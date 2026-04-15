using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

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

        // In-flight flags — flexible and immediate are independent; CheckForUpdate has no guard.
        private bool _flexibleInFlight;
        private bool _immediateInFlight;

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

        // Sentinel-value pattern per CROSS-INVARIANTS §12.2.1.
        public async Task<AppUpdateInfo> CheckForUpdateAsync(CancellationToken ct = default, float timeoutSeconds = -1f)
        {
            EnsureMainThread();
            var resolved = ResolveTimeout(timeoutSeconds);
            AppUpdateInfo info;
            try
            {
                info = await _infoProvider.GetAppUpdateInfoAsync(ct, resolved);
            }
            catch (Exception ex)
            {
                SafeInvokeAnalytics(a => a.OnError(new AppUpdateError(
                    InstallErrorCode.ErrorInternalError, ex.Message, true, DateTime.UtcNow)));
                throw;
            }
            SafeInvokeAnalytics(a => a.OnUpdateInfoReceived(info));
            OnUpdateInfoReceived?.Invoke(info);
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
                var err = await _flexible.StartAsync(options ?? AppUpdateOptions.Flexible(), ct, resolved);
                if (err.HasValue)
                {
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
            await _flexible.CompleteAsync(ct);
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

        private void SafeInvokeAnalytics(Action<IAppUpdateAnalyticsAdapter> call)
        {
            var a = _analytics;
            if (a == null) return;
            try { call(a); }
            catch (Exception ex) { BizSimLogger.Warning($"analytics adapter threw: {ex.Message}"); }
        }

        private void OnApplicationQuit()
        {
            try { _stateListener?.StopListening(); }
            catch (Exception ex) { BizSimLogger.Warning($"stateListener.StopListening threw on quit: {ex.Message}"); }
        }

        private void OnDestroy()
        {
            if (_stateListener != null) _stateListener.OnStateUpdate -= HandleState;
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
