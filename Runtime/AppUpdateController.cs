using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

#pragma warning disable CS4014 // fire-and-forget for CheckStalledImmediateUpdateAsync and RemindLaterAutoCompleteAsync

namespace BizSim.Google.Play.AppUpdate
{
    /// <summary>
    /// Top-level singleton for the app-update flows (flexible + immediate). Consumer code calls
    /// <c>AppUpdateController.Instance.CheckForUpdateAsync()</c> or subscribes to
    /// <see cref="OnInstallStateChanged"/>/<see cref="OnError"/>. All public methods are
    /// main-thread only; calling from a background thread throws <see cref="InvalidOperationException"/>.
    /// </summary>
    public sealed class AppUpdateController : MonoBehaviour
    {
        private static AppUpdateController _instance;
        public static AppUpdateController Instance
        {
            get
            {
                if (_instance != null) return _instance;
                if (!Application.isPlaying) return null;
                var go = new GameObject("[AppUpdateController]");
                _instance = go.AddComponent<AppUpdateController>();
                DontDestroyOnLoad(go);
                return _instance;
            }
        }

        [SerializeField] private AppUpdateMockConfig _mockConfig;

        private AppUpdateSettings _settings;
        private IAppUpdateInfoProvider   _infoProvider;
        private IFlexibleUpdateProvider  _flexible;
        private IImmediateUpdateProvider _immediate;
        private IInstallStateListener    _stateListener;
        private IAppUpdateAnalyticsAdapter _analytics;
        private InstallStateStream _installStateStream;
        private int _mainThreadId;

        // Wave 1 — policy engine, config source, consent gate, session tracker.
        private IAppUpdatePolicyEngine _policyEngine;
        private IAppUpdateConfigSource _configSource;
        private IConsentGate _consentGate;
        private AppUpdateSessionTracker _sessionTracker;

        // In-flight flags — flexible and immediate are independent; CheckForUpdate has no guard.
        private bool _flexibleInFlight;
        private bool _immediateInFlight;

        // Wave 1 — last known info + error for diagnostics.
        private AppUpdateInfo? _lastUpdateInfo;
        private AppUpdateError? _lastError;

        // Wave 1 — watchdog CancellationTokenSource (immediate flow exempt per H4).
        private CancellationTokenSource _watchdogCts;

        // Wave 2 — preload cache.
        private AppUpdateInfo? _preloadedInfo;
        private DateTime _preloadedAtUtc;

        // Wave 2 — remind-later CTS for auto-complete timer.
        private CancellationTokenSource _remindLaterCts;

        // Thread-safe last-state cache — lock required because install state listener may fire
        // on background thread before UnityMainThreadDispatcher marshals.
        private readonly object _stateLock = new object();
        private InstallState? _lastState;
        public InstallState? LastObservedState { get { lock (_stateLock) { return _lastState; } } }

        public event Action<AppUpdateInfo>  OnUpdateInfoReceived;
        public event Action<InstallState>   OnInstallStateChanged;
        public event Action<AppUpdateError> OnError;

        private void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            _mainThreadId = Thread.CurrentThread.ManagedThreadId;

            // Post-r5 per CROSS-INVARIANTS §12: load project-wide Settings asset.
            _settings = Resources.Load<AppUpdateSettings>(AppUpdateSettings.ResourcesLoadKey);
            int  queueCap          = _settings != null
                ? _settings.InstallStateQueueCapacity
                : AppUpdateSettings.DefaultInstallStateQueueCapacity;
            bool autoStartListener = _settings == null || _settings.AutoStartInstallStateListener;
            _installStateStream    = new InstallStateStream(maxCapacity: queueCap);

#if UNITY_ANDROID && !UNITY_EDITOR
  #if DEVELOPMENT_BUILD
            if (_settings != null && _settings.UseMockInDevelopmentBuild)
            {
                var mock = new MockAppUpdateProvider(_mockConfig);
                _infoProvider  = mock;
                _flexible      = mock;
                _immediate     = mock;
                _stateListener = mock;
                BizSimLogger.Info("Development build: using MockAppUpdateProvider per AppUpdateSettings.UseMockInDevelopmentBuild");
            }
            else
  #endif
            {
                _infoProvider  = new AndroidAppUpdateInfoProvider();
                _flexible      = new FlexibleUpdateController();
                _immediate     = new ImmediateUpdateController();
                _stateListener = new InstallStateListenerController();
            }
#else
            var mock = new MockAppUpdateProvider(_mockConfig);
            _infoProvider  = mock;
            _flexible      = mock;
            _immediate     = mock;
            _stateListener = mock;
#endif

            _stateListener.OnStateUpdate += HandleState;
            if (autoStartListener)
                _stateListener.StartListening();

            // Wave 1 defaults — consumers override via SetPolicyEngine/SetConfigSource/SetConsentGate.
            _configSource = new StaticAppUpdateConfigSource();
            _consentGate = new AlwaysAllowConsentGate();
            _sessionTracker = new AppUpdateSessionTracker();
            _policyEngine = new AppUpdatePolicyEngine(
                _configSource,
                _consentGate,
                defaultMinSessions: _settings != null ? _settings.FirstRunGraceSessions : 3,
                defaultMinDays: _settings != null ? _settings.FirstRunGraceDays : 7,
                defaultImmediatePriorityFloor: _settings != null ? _settings.ImmediatePriorityFloor : 5,
                offlineGuardEnabled: _settings != null ? _settings.OfflineGuardEnabled : true);

            // S4 security: warn when kill switch and consent gate are defaults in dev builds.
            if (Debug.isDebugBuild)
            {
                if (_configSource is StaticAppUpdateConfigSource)
                    BizSimLogger.Warning("No custom IAppUpdateConfigSource set — kill switch not wired.");
                if (_consentGate is AlwaysAllowConsentGate)
                    BizSimLogger.Warning("No custom IConsentGate set — prompts fire without consent. " +
                        "For GDPR/DMA, call SetConsentGate().");
            }
        }

        /// <summary>
        /// Public accessor for consumers who set <c>AutoStartInstallStateListener = false</c>
        /// and need to start the listener explicitly later. Idempotent (listener implementations
        /// guard via a static flag).
        /// </summary>
        public void StartInstallStateListener()
        {
            EnsureMainThread();
            _stateListener.StartListening();
        }

        /// <summary>
        /// Replace the default policy engine. The new engine takes effect on the next
        /// <see cref="CheckForUpdateAsync"/> call. Pass a custom implementation to inject
        /// remote-config-driven rules, A/B test gates, or per-version cooldowns.
        /// </summary>
        public void SetPolicyEngine(IAppUpdatePolicyEngine engine)
        {
            EnsureMainThread();
            _policyEngine = engine ?? throw new ArgumentNullException(nameof(engine));
        }

        /// <summary>
        /// Replace the default config source (which always returns <c>RemoteEnabled = true</c>
        /// with all-null overrides). Call this early in your startup to wire a Firebase Remote
        /// Config or similar backend. The engine is reconstructed with the new source.
        /// Also invalidates the preload cache (Wave 2).
        /// </summary>
        public void SetConfigSource(IAppUpdateConfigSource source)
        {
            EnsureMainThread();
            _configSource = source ?? throw new ArgumentNullException(nameof(source));
            RebuildPolicyEngine();
            InvalidatePreloadCache();
        }

        /// <summary>
        /// Replace the default consent gate (which always returns <c>true</c>). Wire a CMP
        /// adapter here to block update prompts until the user has given consent.
        /// </summary>
        public void SetConsentGate(IConsentGate gate)
        {
            EnsureMainThread();
            _consentGate = gate ?? throw new ArgumentNullException(nameof(gate));
            RebuildPolicyEngine();
        }

        /// <summary>
        /// Record an app launch. Call once per cold start (e.g. in your splash scene's Awake).
        /// Feeds the policy engine's first-run grace period logic.
        /// </summary>
        public void RecordLaunch()
        {
            EnsureMainThread();
            _sessionTracker?.RecordLaunch();
        }

        /// <summary>
        /// Record a gameplay session. Call at the end of a meaningful session (e.g. after a
        /// match completes). Feeds the policy engine's first-run grace period logic.
        /// </summary>
        public void RecordSession()
        {
            EnsureMainThread();
            _sessionTracker?.RecordSession();
        }

        /// <summary>
        /// Writes a JSON diagnostic snapshot to disk. Only available in development builds and
        /// the editor — release builds return <c>false</c> immediately. Callers should wrap in
        /// a try/catch for I/O exceptions (invalid path, read-only filesystem, etc.).
        /// </summary>
        /// <param name="path">Absolute file path to write the JSON to.</param>
        /// <returns><c>true</c> if the snapshot was written; <c>false</c> in release builds.</returns>
        public bool WriteDiagnosticSnapshot(string path)
        {
            EnsureMainThread();
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            var snapshot = GetDiagnosticSnapshot();
            System.IO.File.WriteAllText(path, snapshot.ToJson());
            BizSimLogger.Info($"Diagnostic snapshot written to {path}");
            return true;
#else
            BizSimLogger.Warning("WriteDiagnosticSnapshot disabled in release builds");
            return false;
#endif
        }

        /// <summary>
        /// Returns a serializable snapshot of the current app-update subsystem state.
        /// Intended for diagnostics dashboards, support tickets, and debug logging.
        /// </summary>
        public AppUpdateDiagnosticSnapshot GetDiagnosticSnapshot()
        {
            EnsureMainThread();
            var snapshot = new AppUpdateDiagnosticSnapshot
            {
                PackageVersion = PackageVersion.Current,
                Timestamp = DateTime.UtcNow.ToString("O"),
                SessionCount = _sessionTracker?.SessionCount ?? 0,
                LaunchCount = _sessionTracker?.LaunchCount ?? 0,
                DaysSinceInstall = _sessionTracker?.DaysSinceInstall ?? 0,
                RemoteEnabled = _configSource?.RemoteEnabled ?? true,
                CooldownActive = false,
                OfflineGuardEnabled = _settings != null ? _settings.OfflineGuardEnabled : true,
                DryRunMode = _settings != null && _settings.DryRunMode,
                PolicyDecisionRaw = null,
                LastErrorCode = _lastError?.Code.ToString(),
                LastUpdateInfoJson = _lastUpdateInfo.HasValue
                    ? FormatUpdateInfoForDiagnostics(_lastUpdateInfo.Value)
                    : null,
                LastInstallStateJson = _lastState.HasValue
                    ? FormatInstallStateForDiagnostics(_lastState.Value)
                    : null,
                ImmediatePriorityFloor = _settings != null ? _settings.ImmediatePriorityFloor : 5,
                WatchdogTimeoutSeconds = _settings != null ? _settings.WatchdogTimeoutSeconds : 15,
            };
            return snapshot;
        }

        // ---------------------------------------------------------------
        // Wave 2 — PreloadAppUpdateInfoAsync (Task 34)
        // ---------------------------------------------------------------

        /// <summary>
        /// Pre-fetches <see cref="AppUpdateInfo"/> and caches it for the TTL configured in
        /// <see cref="AppUpdateSettings.PreloadCacheTtlMinutes"/> (default 15 min). Subsequent
        /// calls within the TTL return the cached result. The cache is invalidated on
        /// <c>OnApplicationPause(false)</c> and <see cref="SetConfigSource"/>.
        /// </summary>
        public async Task<AppUpdateInfo> PreloadAppUpdateInfoAsync(CancellationToken ct = default)
        {
            EnsureMainThread();

            // Return cached if still valid.
            if (_preloadedInfo.HasValue)
            {
                int ttlMinutes = _settings != null
                    ? _settings.PreloadCacheTtlMinutes
                    : AppUpdateSettings.DefaultPreloadCacheTtlMinutes;
                if ((DateTime.UtcNow - _preloadedAtUtc).TotalMinutes < ttlMinutes)
                {
                    BizSimLogger.Verbose("PreloadAppUpdateInfoAsync: returning cached info.");
                    return _preloadedInfo.Value;
                }
            }

            var ctx = BuildTelemetryContext("preload");
            SafeInvokeV2(v2 => v2.OnPreloadStarted(ctx));

            var resolved = ResolveTimeout(-1f);
            int watchdogMs = (_settings != null ? _settings.WatchdogTimeoutSeconds : 15) * 1000;
            using var watchdogCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            watchdogCts.CancelAfter(watchdogMs);

            AppUpdateInfo info;
            try
            {
                info = await _infoProvider.GetAppUpdateInfoAsync(watchdogCts.Token, resolved);
            }
            catch (Exception ex)
            {
                var code = ClassifyExceptionErrorCode(ex);
                var retryable = AppUpdateError.IsRetryable(code);
                var err = new AppUpdateError(code, ex.Message, retryable, DateTime.UtcNow);
                _lastError = err;
                SafeInvokeV2(v2 => v2.OnPreloadFailed(err, ctx));
                throw;
            }

            _preloadedInfo = info;
            _preloadedAtUtc = DateTime.UtcNow;
            _lastUpdateInfo = info;

            SafeInvokeV2(v2 => v2.OnPreloadSucceeded(ctx));
            BizSimLogger.Info("PreloadAppUpdateInfoAsync: info cached.");
            return info;
        }

        /// <summary>
        /// Invalidates the preload cache so the next <see cref="PreloadAppUpdateInfoAsync"/>
        /// or <see cref="CheckForUpdateAsync"/> call will fetch fresh data.
        /// </summary>
        public void InvalidatePreloadCache()
        {
            _preloadedInfo = null;
            BizSimLogger.Verbose("Preload cache invalidated.");
        }

        // ---------------------------------------------------------------
        // Wave 2 — Per-version cooldown helpers (Task 35)
        // ---------------------------------------------------------------

        const string CooldownKeyPrefix = "bizsim_appupdate_cooldown_";

        /// <summary>
        /// Returns true if the given version code is in cooldown (the user was already
        /// prompted within <see cref="AppUpdateSettings.PerVersionCooldownDays"/> days).
        /// Priority-5 updates are EXEMPT from cooldown and always return false.
        /// </summary>
        bool IsVersionInCooldown(int versionCode, int updatePriority)
        {
            // Priority 5 is always exempt from cooldown.
            if (updatePriority >= 5) return false;

            int cooldownDays = _settings != null
                ? _settings.PerVersionCooldownDays
                : AppUpdateSettings.DefaultPerVersionCooldownDays;
            if (cooldownDays <= 0) return false;

            var key = CooldownKeyPrefix + versionCode;
            var stored = PlayerPrefs.GetString(key, "");
            if (string.IsNullOrEmpty(stored)) return false;

            if (long.TryParse(stored, out var ticks))
            {
                var cooldownStart = new DateTime(ticks, DateTimeKind.Utc);
                return (DateTime.UtcNow - cooldownStart).TotalDays < cooldownDays;
            }

            return false;
        }

        void RecordCooldownForVersion(int versionCode)
        {
            var key = CooldownKeyPrefix + versionCode;
            PlayerPrefs.SetString(key, DateTime.UtcNow.Ticks.ToString());
            PlayerPrefs.Save();
        }

        // ---------------------------------------------------------------
        // Core check flow
        // ---------------------------------------------------------------

        // Sentinel-value pattern per CROSS-INVARIANTS §12.2.1.
        public async Task<AppUpdateInfo> CheckForUpdateAsync(CancellationToken ct = default, float timeoutSeconds = -1f)
        {
            EnsureMainThread();

            // Wave 2 — install-source guard (Task 40).
            if (_settings != null && _settings.SkipNonPlayInstalls
                && !AppUpdateInstallSourceDetector.IsPlayStoreInstall())
            {
                BizSimLogger.Info($"Install-source guard: installer is '{AppUpdateInstallSourceDetector.GetInstallerPackageName()}', not Play Store — skipping.");
                var ctx = BuildTelemetryContext("install_source_blocked");
                SafeInvokeV2(v2 => v2.OnNonPlayInstallBlocked(ctx));
                throw new InvalidOperationException(
                    "CheckForUpdateAsync skipped: app not installed from Play Store and SkipNonPlayInstalls is true.");
            }

            // T21 offline guard — skip when device is unreachable.
            if (_settings != null && _settings.OfflineGuardEnabled
                && Application.internetReachability == NetworkReachability.NotReachable)
            {
                BizSimLogger.Info("Offline guard: skipping CheckForUpdateAsync — device is not reachable.");
                var offlineCtx = BuildTelemetryContext("offline_blocked");
                SafeInvokeV2(v2 => v2.OnOfflineBlocked(offlineCtx));
                SafeInvokeAnalytics(a => a.OnError(new AppUpdateError(
                    InstallErrorCode.ErrorApiNotAvailable, "offline_blocked", false, DateTime.UtcNow)));
                throw new InvalidOperationException(
                    "CheckForUpdateAsync skipped: device is offline and OfflineGuardEnabled is true.");
            }

            var resolved = ResolveTimeout(timeoutSeconds);

            // T20 watchdog timeout — wraps CheckForUpdateAsync (immediate exempt per H4).
            int watchdogMs = (_settings != null ? _settings.WatchdogTimeoutSeconds : 15) * 1000;
            using var watchdogCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            watchdogCts.CancelAfter(watchdogMs);

            AppUpdateInfo info;

            // Wave 2 — try preload cache first.
            if (_preloadedInfo.HasValue)
            {
                int ttlMinutes = _settings != null
                    ? _settings.PreloadCacheTtlMinutes
                    : AppUpdateSettings.DefaultPreloadCacheTtlMinutes;
                if ((DateTime.UtcNow - _preloadedAtUtc).TotalMinutes < ttlMinutes)
                {
                    info = _preloadedInfo.Value;
                    BizSimLogger.Verbose("CheckForUpdateAsync: using preloaded info.");
                    _preloadedInfo = null; // consume once
                }
                else
                {
                    _preloadedInfo = null; // expired
                    info = await FetchInfoWithWatchdog(watchdogCts, ct, resolved, watchdogMs);
                }
            }
            else
            {
                info = await FetchInfoWithWatchdog(watchdogCts, ct, resolved, watchdogMs);
            }

            _lastUpdateInfo = info;
            var telemetryCtx = BuildTelemetryContext("auto_check", info);
            SafeInvokeAnalytics(a => a.OnUpdateInfoReceived(info));
            SafeInvokeV2(v2 => v2.OnUpdateInfoReceived(info, telemetryCtx));
            OnUpdateInfoReceived?.Invoke(info);

            // Wave 2 — per-version cooldown (Task 35).
            if (info.IsUpdateAvailable && IsVersionInCooldown(info.AvailableVersionCode, info.UpdatePriority))
            {
                BizSimLogger.Info($"Per-version cooldown active for version {info.AvailableVersionCode} — skipping prompt.");
                SafeInvokeV2(v2 => v2.OnPerVersionCooldownBlocked(telemetryCtx));
                return info;
            }

            // T19 policy engine integration — evaluate and act.
            var policyContext = BuildPolicyContext(info);
            var decision = _policyEngine.Evaluate(policyContext);
            BizSimLogger.Verbose($"Policy decision: {decision}");
            SafeInvokeV2(v2 => v2.OnPolicyEvaluated(decision, telemetryCtx));

            if (_settings != null && _settings.DryRunMode && Debug.isDebugBuild)
            {
                BizSimLogger.Info($"DryRunMode: policy returned {decision} — not starting any flow.");
                return info;
            }

            if (decision.IsBlock)
            {
                BizSimLogger.Info($"Policy blocked update: {decision.Reason}");
                FireV2BlockedEvent(decision, telemetryCtx);
                return info;
            }

            if (decision.IsAllow)
            {
                // Record that we prompted for this version (cooldown starts now).
                RecordCooldownForVersion(info.AvailableVersionCode);

                if (decision.UpdateType == AppUpdateType.Immediate)
                {
                    BizSimLogger.Info("Policy allows immediate update — starting immediate flow.");
                    await StartImmediateUpdateAsync(AppUpdateOptions.Immediate(), ct, timeoutSeconds);
                }
                else
                {
                    BizSimLogger.Info("Policy allows flexible update — starting flexible flow.");
                    await StartFlexibleUpdateAsync(AppUpdateOptions.Flexible(), ct, timeoutSeconds);
                }
            }

            return info;
        }

        public async Task<AppUpdateError?> StartFlexibleUpdateAsync(
            AppUpdateOptions options = null, CancellationToken ct = default, float timeoutSeconds = -1f)
        {
            EnsureMainThread();
            if (_flexibleInFlight) throw new InvalidOperationException("Flexible update already in progress");
            _flexibleInFlight = true;
            var telemetryCtx = BuildTelemetryContext("flexible_start");
            SafeInvokeAnalytics(a => a.OnFlexibleFlowStarted());
            SafeInvokeV2(v2 => v2.OnFlexibleFlowStarted(telemetryCtx));
            try
            {
                var resolved = ResolveTimeout(timeoutSeconds);

                // T20 watchdog — wraps flexible flow.
                int watchdogMs = (_settings != null ? _settings.WatchdogTimeoutSeconds : 15) * 1000;
                using var watchdogCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                watchdogCts.CancelAfter(watchdogMs);

                AppUpdateError? err;
                try
                {
                    err = await _flexible.StartAsync(options ?? AppUpdateOptions.Flexible(), watchdogCts.Token, resolved);
                }
                catch (OperationCanceledException) when (!ct.IsCancellationRequested)
                {
                    var timeout = new AppUpdateError(
                        InstallErrorCode.Timeout,
                        $"StartFlexibleUpdateAsync watchdog timeout ({watchdogMs}ms)",
                        true, DateTime.UtcNow);
                    _lastError = timeout;
                    SafeInvokeAnalytics(a => a.OnError(timeout));
                    SafeInvokeV2(v2 => v2.OnError(timeout, telemetryCtx));
                    OnError?.Invoke(timeout);
                    return timeout;
                }

                if (err.HasValue)
                {
                    _lastError = err.Value;
                    SafeInvokeAnalytics(a => a.OnError(err.Value));
                    SafeInvokeV2(v2 => v2.OnError(err.Value, telemetryCtx));
                    OnError?.Invoke(err.Value);
                }
                return err;
            }
            finally
            {
                _flexibleInFlight = false;
            }
        }

        // H4 CRITICAL: watchdog does NOT wrap immediate flow — the full-screen Play Store dialog
        // may take 30+ seconds for user interaction. Fragment shim's onActivityResult handles timeout.
        public async Task<AppUpdateError?> StartImmediateUpdateAsync(
            AppUpdateOptions options = null, CancellationToken ct = default, float timeoutSeconds = -1f)
        {
            EnsureMainThread();
            if (_immediateInFlight) throw new InvalidOperationException("Immediate update already in progress");
            _immediateInFlight = true;
            var telemetryCtx = BuildTelemetryContext("immediate_start");
            SafeInvokeAnalytics(a => a.OnImmediateFlowStarted());
            SafeInvokeV2(v2 => v2.OnImmediateFlowStarted(telemetryCtx));
            try
            {
                var resolved = ResolveTimeout(timeoutSeconds);
                var err = await _immediate.StartAsync(options ?? AppUpdateOptions.Immediate(), ct, resolved);
                if (err.HasValue)
                {
                    _lastError = err.Value;
                    SafeInvokeAnalytics(a => a.OnError(err.Value));
                    SafeInvokeV2(v2 => v2.OnError(err.Value, telemetryCtx));
                    OnError?.Invoke(err.Value);
                }
                return err;
            }
            finally
            {
                _immediateInFlight = false;
            }
        }

        // Precondition guard: CompleteFlexibleUpdate requires the stream to have reached Downloaded.
        public async Task CompleteFlexibleUpdateAsync(CancellationToken ct = default)
        {
            EnsureMainThread();
            var last = LastObservedState;
            if (last == null || last.Value.InstallStatus != InstallStatus.Downloaded)
            {
                throw new InvalidOperationException(
                    $"CompleteFlexibleUpdate requires install state == Downloaded. " +
                    $"Current: {(last?.InstallStatus.ToString() ?? "null")}. " +
                    $"Subscribe to OnInstallStateChanged or ReadInstallStatesAsync to observe the Downloaded state first.");
            }
            var telemetryCtx = BuildTelemetryContext("complete_update");
            SafeInvokeAnalytics(a => a.OnCompleteUpdateInvoked());
            SafeInvokeV2(v2 => v2.OnCompleteUpdateInvoked(telemetryCtx));

            CancelRemindLater();

            // T20 watchdog — wraps CompleteFlexibleUpdateAsync.
            int watchdogMs = (_settings != null ? _settings.WatchdogTimeoutSeconds : 15) * 1000;
            using var watchdogCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            watchdogCts.CancelAfter(watchdogMs);

            try
            {
                await _flexible.CompleteAsync(watchdogCts.Token);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                var err = new AppUpdateError(
                    InstallErrorCode.Timeout,
                    $"CompleteFlexibleUpdateAsync watchdog timeout ({watchdogMs}ms)",
                    true, DateTime.UtcNow);
                _lastError = err;
                SafeInvokeAnalytics(a => a.OnError(err));
                SafeInvokeV2(v2 => v2.OnError(err, telemetryCtx));
                OnError?.Invoke(err);
                throw;
            }
        }

        // ---------------------------------------------------------------
        // Wave 2 — Remind-later with 24h hard cap (Task 37)
        // ---------------------------------------------------------------

        /// <summary>
        /// Defers <see cref="CompleteFlexibleUpdateAsync"/> by <paramref name="delay"/>, capped at
        /// <see cref="AppUpdateSettings.PostDownloadRemindLaterMaxHours"/> (default 24h). When the
        /// cap expires, the update auto-completes. Only callable when the last observed install
        /// state is <see cref="InstallStatus.Downloaded"/>.
        /// </summary>
        /// <param name="delay">How long to wait before auto-completing. Capped at the Settings max.</param>
        /// <param name="ct">Cancellation token that cancels the remind-later timer (does NOT auto-complete).</param>
        public async Task CompleteFlexibleUpdateAsync(TimeSpan delay, CancellationToken ct = default)
        {
            EnsureMainThread();

            var last = LastObservedState;
            if (last == null || last.Value.InstallStatus != InstallStatus.Downloaded)
            {
                throw new InvalidOperationException(
                    $"CompleteFlexibleUpdateAsync(delay) requires install state == Downloaded. " +
                    $"Current: {(last?.InstallStatus.ToString() ?? "null")}.");
            }

            int maxHours = _settings != null
                ? _settings.PostDownloadRemindLaterMaxHours
                : AppUpdateSettings.DefaultPostDownloadRemindLaterMaxHours;

            // If maxHours is 0, auto-complete is disabled — just complete immediately.
            if (maxHours <= 0)
            {
                BizSimLogger.Info("Remind-later: max hours is 0, completing immediately.");
                await CompleteFlexibleUpdateAsync(ct);
                return;
            }

            var maxDelay = TimeSpan.FromHours(maxHours);
            var effectiveDelay = delay > maxDelay ? maxDelay : delay;
            if (effectiveDelay <= TimeSpan.Zero)
            {
                BizSimLogger.Info("Remind-later: delay is zero or negative, completing immediately.");
                await CompleteFlexibleUpdateAsync(ct);
                return;
            }

            var telemetryCtx = BuildTelemetryContext("remind_later");
            SafeInvokeV2(v2 => v2.OnRemindLaterStarted(telemetryCtx));
            BizSimLogger.Info($"Remind-later: will auto-complete in {effectiveDelay.TotalMinutes:F0} min (cap: {maxHours}h).");

            CancelRemindLater();
            _remindLaterCts = CancellationTokenSource.CreateLinkedTokenSource(ct);

            // Fire-and-forget the auto-complete timer.
            RemindLaterAutoCompleteAsync(effectiveDelay, _remindLaterCts.Token);
        }

        private async Task RemindLaterAutoCompleteAsync(TimeSpan delay, CancellationToken ct)
        {
            try
            {
                await Task.Delay(delay, ct);
            }
            catch (OperationCanceledException)
            {
                BizSimLogger.Verbose("Remind-later timer cancelled.");
                return;
            }

            // Timer expired — auto-complete if still Downloaded.
            try
            {
                var last = LastObservedState;
                if (last != null && last.Value.InstallStatus == InstallStatus.Downloaded)
                {
                    BizSimLogger.Info("Remind-later cap expired — auto-completing flexible update.");
                    var ctx = BuildTelemetryContext("remind_later_auto");
                    SafeInvokeV2(v2 => v2.OnRemindLaterAutoCompleted(ctx));
                    await _flexible.CompleteAsync(CancellationToken.None);
                }
                else
                {
                    BizSimLogger.Verbose("Remind-later timer expired but install state is no longer Downloaded — skipping auto-complete.");
                }
            }
            catch (Exception ex)
            {
                BizSimLogger.Warning($"Remind-later auto-complete failed: {ex.Message}");
            }
        }

        private void CancelRemindLater()
        {
            if (_remindLaterCts != null)
            {
                _remindLaterCts.Cancel();
                _remindLaterCts.Dispose();
                _remindLaterCts = null;
            }
        }

        // ---------------------------------------------------------------
        // Existing public API
        // ---------------------------------------------------------------

        public IAsyncEnumerable<InstallState> ReadInstallStatesAsync(CancellationToken ct = default)
        {
            EnsureMainThread();
            return _installStateStream.ReadAsync(ct);
        }

        public void SetAnalyticsAdapter(IAppUpdateAnalyticsAdapter adapter)
        {
            EnsureMainThread();
            _analytics = adapter;
        }

        // ---------------------------------------------------------------
        // Private helpers
        // ---------------------------------------------------------------

        private float ResolveTimeout(float caller)
            => caller > 0f
                ? caller
                : (_settings != null ? _settings.DefaultTimeoutSeconds : AppUpdateSettings.DefaultTimeoutSecondsFallback);

        private async Task<AppUpdateInfo> FetchInfoWithWatchdog(
            CancellationTokenSource watchdogCts, CancellationToken callerCt,
            float resolved, int watchdogMs)
        {
            try
            {
                return await _infoProvider.GetAppUpdateInfoAsync(watchdogCts.Token, resolved);
            }
            catch (OperationCanceledException) when (!callerCt.IsCancellationRequested)
            {
                var err = new AppUpdateError(
                    InstallErrorCode.Timeout,
                    $"CheckForUpdateAsync watchdog timeout ({watchdogMs}ms)",
                    true, DateTime.UtcNow);
                _lastError = err;
                SafeInvokeAnalytics(a => a.OnError(err));
                OnError?.Invoke(err);
                throw;
            }
            catch (Exception ex)
            {
                // T23 non-retryable error telemetry — dedicated branch for APP_NOT_OWNED / PLAY_STORE_NOT_FOUND.
                var code = ClassifyExceptionErrorCode(ex);
                var retryable = AppUpdateError.IsRetryable(code);
                var err = new AppUpdateError(code, ex.Message, retryable, DateTime.UtcNow);
                _lastError = err;
                var ctx = BuildTelemetryContext("check_error");
                SafeInvokeAnalytics(a => a.OnError(err));
                SafeInvokeV2(v2 => v2.OnError(err, ctx));
                if (!retryable)
                    SafeInvokeV2(v2 => v2.OnNonRetryableError(err, ctx));
                OnError?.Invoke(err);
                throw;
            }
        }

        private void HandleState(InstallState s)
        {
            lock (_stateLock) { _lastState = s; }
            _installStateStream.Enqueue(s);
            var ctx = BuildTelemetryContext("state_change");
            SafeInvokeAnalytics(a => a.OnInstallStateChanged(s));
            SafeInvokeV2(v2 => v2.OnInstallStateChanged(s, ctx));
            OnInstallStateChanged?.Invoke(s);
        }

        /// <summary>
        /// T18 CRITICAL — DeveloperTriggeredUpdateInProgress resume re-check. When the app comes
        /// back from a paused state (user switched away during immediate update), re-fetch
        /// AppUpdateInfo and auto-resume the immediate flow if stalled.
        /// Also invalidates the preload cache on resume (Wave 2).
        /// </summary>
        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                // Auto-complete on pause if remind-later is active and Downloaded.
                var last = LastObservedState;
                if (_remindLaterCts != null && last != null
                    && last.Value.InstallStatus == InstallStatus.Downloaded)
                {
                    BizSimLogger.Info("App paused with remind-later active and Downloaded — auto-completing.");
                    CancelRemindLater();
                    // Fire-and-forget auto-complete.
                    AutoCompleteOnPause();
                }
                return;
            }

            // Resume: invalidate preload cache + check for stalled immediate.
            InvalidatePreloadCache();
            CheckStalledImmediateUpdateAsync();
        }

        private async void AutoCompleteOnPause()
        {
            try
            {
                var ctx = BuildTelemetryContext("remind_later_pause");
                SafeInvokeV2(v2 => v2.OnRemindLaterAutoCompleted(ctx));
                await _flexible.CompleteAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                BizSimLogger.Warning($"Auto-complete on pause failed: {ex.Message}");
            }
        }

        private async Task CheckStalledImmediateUpdateAsync()
        {
            try
            {
                var timeout = _settings != null ? _settings.DefaultTimeoutSeconds : AppUpdateSettings.DefaultTimeoutSecondsFallback;
                var info = await _infoProvider.GetAppUpdateInfoAsync(CancellationToken.None, timeout);
                if (info.UpdateAvailability == UpdateAvailability.DeveloperTriggeredUpdateInProgress)
                {
                    BizSimLogger.Info("Stalled immediate update detected on resume — auto-resuming.");
                    await _immediate.StartAsync(AppUpdateOptions.Immediate(), CancellationToken.None, timeout);
                }
            }
            catch (Exception ex)
            {
                BizSimLogger.Warning($"Resume re-check failed: {ex.Message}");
            }
        }

        private void SafeInvokeAnalytics(Action<IAppUpdateAnalyticsAdapter> call)
        {
            var a = _analytics;
            if (a == null) return;
            try { call(a); }
            catch (Exception ex) { BizSimLogger.Warning($"analytics adapter threw: {ex.Message}"); }
        }

        /// <summary>
        /// Wave 2 — V2 adapter pattern: <c>if (_analytics is IAppUpdateAnalyticsAdapterV2 v2)</c>
        /// wrapped in try/catch per bridge pattern §6.
        /// </summary>
        private void SafeInvokeV2(Action<IAppUpdateAnalyticsAdapterV2> call)
        {
            var a = _analytics;
            if (a is IAppUpdateAnalyticsAdapterV2 v2)
            {
                try { call(v2); }
                catch (Exception ex) { BizSimLogger.Warning($"V2 analytics adapter threw: {ex.Message}"); }
            }
        }

        /// <summary>
        /// Fires the appropriate V2 blocked event based on the policy decision reason.
        /// </summary>
        private void FireV2BlockedEvent(PolicyDecision decision, AppUpdateTelemetryContext ctx)
        {
            switch (decision.Reason)
            {
                case "killswitch_disabled":
                    SafeInvokeV2(v2 => v2.OnKillSwitchBlocked(ctx));
                    break;
                case "consent_denied":
                    SafeInvokeV2(v2 => v2.OnConsentBlocked(ctx));
                    break;
                case "offline":
                    SafeInvokeV2(v2 => v2.OnOfflineBlocked(ctx));
                    break;
                case "first_run_grace":
                    SafeInvokeV2(v2 => v2.OnFirstRunGraceBlocked(ctx));
                    break;
                default:
                    // Other block reasons don't have dedicated V2 events.
                    break;
            }
        }

        private void RebuildPolicyEngine()
        {
            _policyEngine = new AppUpdatePolicyEngine(
                _configSource,
                _consentGate,
                defaultMinSessions: _settings != null ? _settings.FirstRunGraceSessions : 3,
                defaultMinDays: _settings != null ? _settings.FirstRunGraceDays : 7,
                defaultImmediatePriorityFloor: _settings != null ? _settings.ImmediatePriorityFloor : 5,
                offlineGuardEnabled: _settings != null ? _settings.OfflineGuardEnabled : true);
        }

        private AppUpdatePolicyContext BuildPolicyContext(AppUpdateInfo info)
        {
            return new AppUpdatePolicyContext(
                sessionCount: _sessionTracker?.SessionCount ?? 0,
                launchCount: _sessionTracker?.LaunchCount ?? 0,
                daysSinceInstall: _sessionTracker?.DaysSinceInstall ?? 0,
                lastUpdateInfo: info,
                appVersion: Application.version);
        }

        /// <summary>
        /// Build a telemetry context from current state. Used at every V2 event emission.
        /// </summary>
        private AppUpdateTelemetryContext BuildTelemetryContext(string triggerReason, AppUpdateInfo? info = null)
        {
            var updateInfo = info ?? _lastUpdateInfo;
            return new AppUpdateTelemetryContext(
                appVersion: Application.version,
                availableVersionCode: updateInfo?.AvailableVersionCode ?? 0,
                updatePriority: updateInfo?.UpdatePriority ?? 0,
                stalenessDays: updateInfo?.ClientVersionStalenessDays ?? -1,
                triggerReason: triggerReason,
                sessionCount: _sessionTracker?.SessionCount ?? 0,
                variantId: null);
        }

        /// <summary>
        /// T23 — classify exception into error codes. APP_NOT_OWNED and PLAY_STORE_NOT_FOUND are
        /// non-retryable; INTERNAL_ERROR stays retryable.
        /// </summary>
        private static InstallErrorCode ClassifyExceptionErrorCode(Exception ex)
        {
            var msg = ex.Message ?? "";
            if (msg.Contains("APP_NOT_OWNED") || msg.Contains("-10"))
                return InstallErrorCode.ErrorAppNotOwned;
            if (msg.Contains("PLAY_STORE_NOT_FOUND") || msg.Contains("-9"))
                return InstallErrorCode.ErrorPlayStoreNotFound;
            return InstallErrorCode.ErrorInternalError;
        }

        private static string FormatUpdateInfoForDiagnostics(AppUpdateInfo info)
        {
            return $"{{\"availability\":\"{info.UpdateAvailability}\",\"versionCode\":{info.AvailableVersionCode}," +
                   $"\"priority\":{info.UpdatePriority},\"stalenessDays\":{info.ClientVersionStalenessDays?.ToString() ?? "null"}," +
                   $"\"flexAllowed\":{(info.IsFlexibleAllowed ? "true" : "false")}," +
                   $"\"immAllowed\":{(info.IsImmediateAllowed ? "true" : "false")}}}";
        }

        private static string FormatInstallStateForDiagnostics(InstallState state)
        {
            return $"{{\"status\":\"{state.InstallStatus}\",\"error\":\"{state.InstallErrorCode}\"," +
                   $"\"downloaded\":{state.BytesDownloaded},\"total\":{state.TotalBytesToDownload}}}";
        }

        private void OnApplicationQuit()
        {
            try { _stateListener?.StopListening(); }
            catch (Exception ex) { BizSimLogger.Warning($"stateListener.StopListening threw on quit: {ex.Message}"); }
        }

        private void OnDestroy()
        {
            if (_stateListener != null) _stateListener.OnStateUpdate -= HandleState;
            _watchdogCts?.Dispose();
            CancelRemindLater();
            if (_instance == this) _instance = null;
        }

        private void EnsureMainThread([CallerMemberName] string caller = null)
        {
            if (Thread.CurrentThread.ManagedThreadId != _mainThreadId)
            {
                throw new InvalidOperationException(
                    $"AppUpdateController.{caller} must be called from the Unity main thread " +
                    $"(was called from thread id {Thread.CurrentThread.ManagedThreadId}).");
            }
        }
    }
}
