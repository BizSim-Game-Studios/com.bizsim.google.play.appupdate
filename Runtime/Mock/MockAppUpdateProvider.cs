using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace BizSim.Google.Play.AppUpdate
{
    /// <summary>
    /// Editor + non-Android mock provider. Implements all four provider contracts so a single
    /// instance can be injected where the Android controllers would be. Uses
    /// <see cref="MockInstallStateReplayer"/> to drive the flexible flow state machine on the
    /// Unity Update tick.
    /// </summary>
    public sealed class MockAppUpdateProvider :
        IAppUpdateInfoProvider,
        IFlexibleUpdateProvider,
        IImmediateUpdateProvider,
        IInstallStateListener
    {
        private readonly AppUpdateMockConfig _cfg;
        private MockInstallStateReplayer _currentReplayer;

        public event Action<InstallState> OnStateUpdate;

        public MockAppUpdateProvider(AppUpdateMockConfig cfg)
        {
            _cfg = cfg != null ? cfg : ScriptableObject.CreateInstance<AppUpdateMockConfig>();
        }

        // ----- IInstallStateListener -----

        public void StartListening() { /* mock has no persistent listener state */ }
        public void StopListening()
        {
            if (_currentReplayer != null && _currentReplayer.gameObject != null)
            {
                UnityEngine.Object.Destroy(_currentReplayer.gameObject);
                _currentReplayer = null;
            }
        }

        // ----- IAppUpdateInfoProvider -----

        public Task<AppUpdateInfo> GetAppUpdateInfoAsync(CancellationToken ct, float timeoutSeconds)
        {
            ct.ThrowIfCancellationRequested();
            int? staleness = _cfg.SimulatedClientVersionStalenessDays < 0
                ? (int?)null
                : _cfg.SimulatedClientVersionStalenessDays;
            var info = new AppUpdateInfo(
                updateAvailability: _cfg.SimulatedAvailability,
                availableVersionCode: _cfg.SimulatedAvailableVersionCode,
                updatePriority: _cfg.SimulatedUpdatePriority,
                clientVersionStalenessDays: staleness,
                installStatus: InstallStatus.Unknown,
                bytesDownloaded: 0,
                totalBytesToDownload: 0,
                isFlexibleAllowed: _cfg.AllowFlexible,
                isImmediateAllowed: _cfg.AllowImmediate,
                fetchedAtUtc: DateTime.UtcNow);
            return Task.FromResult(info);
        }

        // ----- IFlexibleUpdateProvider -----

        public async Task<AppUpdateError?> StartAsync(AppUpdateOptions opts, CancellationToken ct, float timeoutSeconds)
        {
            if (!_cfg.AllowFlexible)
            {
                return new AppUpdateError(
                    InstallErrorCode.ErrorInstallNotAllowed,
                    "MockAppUpdateProvider: AllowFlexible=false",
                    retryable: false,
                    occurredAtUtc: DateTime.UtcNow);
            }
            ct.ThrowIfCancellationRequested();

            var completion = new TaskCompletionSource<AppUpdateError?>(TaskCreationOptions.RunContinuationsAsynchronously);
            Action<InstallState> stateHandler = s => OnStateUpdate?.Invoke(s);
            Action<AppUpdateError> errorHandler = e => completion.TrySetResult(e);
            Action completeHandler = () => completion.TrySetResult(null);

            _currentReplayer = MockInstallStateReplayer.Spawn(_cfg, stateHandler, errorHandler, completeHandler);

            using (ct.Register(() => completion.TrySetCanceled(ct)))
            {
                return await completion.Task;
            }
        }

        public Task CompleteAsync(CancellationToken ct)
        {
            // Mock "restart" — in the editor we just log and resolve.
            BizSimLogger.Info("MockAppUpdateProvider: CompleteFlexibleUpdate invoked; in a real build Play Store would take over and terminate the process here.");
            return Task.CompletedTask;
        }

        // ----- IImmediateUpdateProvider -----

        Task<AppUpdateError?> IImmediateUpdateProvider.StartAsync(AppUpdateOptions opts, CancellationToken ct, float timeoutSeconds)
            => StartImmediateAsync(opts, ct, timeoutSeconds);

        private async Task<AppUpdateError?> StartImmediateAsync(AppUpdateOptions opts, CancellationToken ct, float timeoutSeconds)
        {
            if (!_cfg.AllowImmediate)
            {
                return new AppUpdateError(
                    InstallErrorCode.ErrorInstallNotAllowed,
                    "MockAppUpdateProvider: AllowImmediate=false",
                    retryable: false,
                    occurredAtUtc: DateTime.UtcNow);
            }
            float delay = Mathf.Max(0.1f, _cfg.ImmediateFlowDurationSeconds);
            await Task.Delay(TimeSpan.FromSeconds(delay), ct);
            if (_cfg.SimulatedErrorCode != InstallErrorCode.NoError)
            {
                return new AppUpdateError(
                    _cfg.SimulatedErrorCode,
                    $"MockAppUpdateProvider simulated immediate-flow error: {_cfg.SimulatedErrorCode}",
                    retryable: AppUpdateError.IsRetryable(_cfg.SimulatedErrorCode),
                    occurredAtUtc: DateTime.UtcNow);
            }
            return null;
        }

        public Task<bool> IsImmediateUpdateInProgressAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(_cfg.SimulatedAvailability == UpdateAvailability.DeveloperTriggeredUpdateInProgress);
        }
    }
}
