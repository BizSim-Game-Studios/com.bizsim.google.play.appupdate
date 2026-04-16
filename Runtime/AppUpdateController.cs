using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

#pragma warning disable CS4014 // fire-and-forget for CheckStalledImmediateUpdateAsync

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
        /// </summary>
        public void SetConfigSource(IAppUpdateConfigSource source)
        {
            EnsureMainThread();
            _configSource = source ?? throw new ArgumentNullException(nameof(source));
            RebuildPolicyEngine();
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

        // Sentinel-value pattern per CROSS-INVARIANTS §12.2.1.
        public async Task<AppUpdateInfo> CheckForUpdateAsync(CancellationToken ct = default, float timeoutSeconds = -1f)
        {
            EnsureMainThread();

            // T21 offline guard — skip when device is unreachable.
            if (_settings != null && _settings.OfflineGuardEnabled
                && Application.internetReachability == NetworkReachability.NotReachable)
            {
                BizSimLogger.Info("Offline guard: skipping CheckForUpdateAsync — device is not reachable.");
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
            try
            {
                info = await _infoProvider.GetAppUpdateInfoAsync(watchdogCts.Token, resolved);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
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
                SafeInvokeAnalytics(a => a.OnError(err));
                OnError?.Invoke(err);
                throw;
            }

            _lastUpdateInfo = info;
            SafeInvokeAnalytics(a => a.OnUpdateInfoReceived(info));
            OnUpdateInfoReceived?.Invoke(info);

            // T19 policy engine integration — evaluate and act.
            var policyContext = BuildPolicyContext(info);
            var decision = _policyEngine.Evaluate(policyContext);
            BizSimLogger.Verbose($"Policy decision: {decision}");

            if (_settings != null && _settings.DryRunMode && Debug.isDebugBuild)
            {
                BizSimLogger.Info($"DryRunMode: policy returned {decision} — not starting any flow.");
                return info;
            }

            if (decision.IsBlock)
            {
                BizSimLogger.Info($"Policy blocked update: {decision.Reason}");
                return info;
            }

            if (decision.IsAllow)
            {
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
            SafeInvokeAnalytics(a => a.OnFlexibleFlowStarted());
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
                    OnError?.Invoke(timeout);
                    return timeout;
                }

                if (err.HasValue)
                {
                    _lastError = err.Value;
                    SafeInvokeAnalytics(a => a.OnError(err.Value));
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
            SafeInvokeAnalytics(a => a.OnImmediateFlowStarted());
            try
            {
                var resolved = ResolveTimeout(timeoutSeconds);
                var err = await _immediate.StartAsync(options ?? AppUpdateOptions.Immediate(), ct, resolved);
                if (err.HasValue)
                {
                    _lastError = err.Value;
                    SafeInvokeAnalytics(a => a.OnError(err.Value));
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
            SafeInvokeAnalytics(a => a.OnCompleteUpdateInvoked());

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
                OnError?.Invoke(err);
                throw;
            }
        }

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

        private float ResolveTimeout(float caller)
            => caller > 0f
                ? caller
                : (_settings != null ? _settings.DefaultTimeoutSeconds : AppUpdateSettings.DefaultTimeoutSecondsFallback);

        private void HandleState(InstallState s)
        {
            lock (_stateLock) { _lastState = s; }
            _installStateStream.Enqueue(s);
            SafeInvokeAnalytics(a => a.OnInstallStateChanged(s));
            OnInstallStateChanged?.Invoke(s);
        }

        /// <summary>
        /// T18 CRITICAL — DeveloperTriggeredUpdateInProgress resume re-check. When the app comes
        /// back from a paused state (user switched away during immediate update), re-fetch
        /// AppUpdateInfo and auto-resume the immediate flow if stalled.
        /// </summary>
        private void OnApplicationPause(bool paused)
        {
            if (paused) return;
            // Fire-and-forget — CS4014 suppressed at top of file.
            CheckStalledImmediateUpdateAsync();
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
