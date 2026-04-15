using System;
using UnityEngine;

namespace BizSim.Google.Play.AppUpdate
{
    /// <summary>
    /// Hidden MonoBehaviour used by <see cref="MockAppUpdateProvider"/> to drive the flexible
    /// flow state machine on Unity's Update tick. Created on-demand, hidden from the hierarchy,
    /// destroys itself when the sequence terminates.
    /// </summary>
    internal sealed class MockInstallStateReplayer : MonoBehaviour
    {
        private AppUpdateMockConfig _config;
        private Action<InstallState> _onState;
        private Action<AppUpdateError> _onError;
        private Action _onComplete;
        private float _startedAt;
        private InstallStatus _currentStatus;
        private bool _active;

        public static MockInstallStateReplayer Spawn(
            AppUpdateMockConfig config,
            Action<InstallState> onState,
            Action<AppUpdateError> onError,
            Action onComplete)
        {
            var go = new GameObject("[MockInstallStateReplayer]");
            go.hideFlags = HideFlags.HideAndDontSave;
            var r = go.AddComponent<MockInstallStateReplayer>();
            r._config = config;
            r._onState = onState;
            r._onError = onError;
            r._onComplete = onComplete;
            r._startedAt = Time.realtimeSinceStartup;
            r._currentStatus = InstallStatus.Pending;
            r._active = true;
            r.Emit(InstallStatus.Pending, 0, config.FlexibleDownloadDurationSeconds > 0f ? 1_000_000L : 0L);
            return r;
        }

        private void Update()
        {
            if (!_active) return;
            float elapsed = Time.realtimeSinceStartup - _startedAt;
            float duration = Mathf.Max(0.1f, _config.FlexibleDownloadDurationSeconds);
            float progress = Mathf.Clamp01(elapsed / duration);

            // Transition: Pending -> Downloading -> Downloaded
            if (_currentStatus == InstallStatus.Pending && elapsed > 0.05f)
            {
                _currentStatus = InstallStatus.Downloading;
                CheckFailureAndEmit(InstallStatus.Downloading, 0, 1_000_000L);
                if (!_active) return;
            }
            if (_currentStatus == InstallStatus.Downloading)
            {
                long downloaded = (long)(progress * 1_000_000L);
                CheckFailureAndEmit(InstallStatus.Downloading, downloaded, 1_000_000L);
                if (!_active) return;
                if (progress >= 1f)
                {
                    _currentStatus = InstallStatus.Downloaded;
                    CheckFailureAndEmit(InstallStatus.Downloaded, 1_000_000L, 1_000_000L);
                    if (!_active) return;
                    _onComplete?.Invoke();
                    TerminateAndDestroy();
                }
            }
        }

        private void CheckFailureAndEmit(InstallStatus status, long downloaded, long total)
        {
            // If the config says to fail at this status, emit a Failed state + error and stop.
            if (_config.SimulatedFailureAt == status && _config.SimulatedErrorCode != InstallErrorCode.NoError)
            {
                var failState = new InstallState(
                    InstallStatus.Failed, downloaded, total, _config.SimulatedErrorCode, DateTime.UtcNow);
                _onState?.Invoke(failState);
                var err = new AppUpdateError(
                    _config.SimulatedErrorCode,
                    $"MockInstallStateReplayer simulated failure at {status}",
                    retryable: AppUpdateError.IsRetryable(_config.SimulatedErrorCode),
                    occurredAtUtc: DateTime.UtcNow);
                _onError?.Invoke(err);
                TerminateAndDestroy();
                return;
            }
            Emit(status, downloaded, total);
        }

        private void Emit(InstallStatus status, long downloaded, long total)
        {
            _onState?.Invoke(new InstallState(status, downloaded, total, InstallErrorCode.NoError, DateTime.UtcNow));
        }

        private void TerminateAndDestroy()
        {
            _active = false;
            if (this != null && gameObject != null) Destroy(gameObject);
        }
    }
}
