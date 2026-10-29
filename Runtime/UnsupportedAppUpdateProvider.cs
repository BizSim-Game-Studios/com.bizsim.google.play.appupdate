using System;
using System.Threading;
using System.Threading.Tasks;

namespace BizSim.Google.Play.AppUpdate
{
    /// <summary>
    /// Provider for players with no Play in-app update API (iOS, standalone, WebGL). Reports no update,
    /// never raises install state, and fails start requests with <see cref="InstallErrorCode.ErrorApiNotAvailable"/>.
    /// </summary>
    internal sealed class UnsupportedAppUpdateProvider :
        IAppUpdateInfoProvider, IFlexibleUpdateProvider, IImmediateUpdateProvider, IInstallStateListener
    {
        public bool IsSupported => false;

        public event Action<InstallState> OnStateUpdate { add { } remove { } }

        public void StartListening() { }
        public void StopListening() { }

        public Task<AppUpdateInfo> GetAppUpdateInfoAsync(CancellationToken ct, float timeoutSeconds)
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(new AppUpdateInfo(
                updateAvailability: UpdateAvailability.UpdateNotAvailable,
                availableVersionCode: 0,
                updatePriority: 0,
                clientVersionStalenessDays: null,
                installStatus: InstallStatus.Unknown,
                bytesDownloaded: 0,
                totalBytesToDownload: 0,
                isFlexibleAllowed: false,
                isImmediateAllowed: false,
                fetchedAtUtc: DateTime.UtcNow));
        }

        public Task<AppUpdateError?> StartAsync(AppUpdateOptions opts, CancellationToken ct, float timeoutSeconds)
            => Task.FromResult<AppUpdateError?>(new AppUpdateError(InstallErrorCode.ErrorApiNotAvailable,
                "In-app updates are not available on this platform.", retryable: false, occurredAtUtc: DateTime.UtcNow));

        public Task CompleteAsync(CancellationToken ct) => Task.CompletedTask;

        public Task<bool> IsImmediateUpdateInProgressAsync(CancellationToken ct) => Task.FromResult(false);
    }
}
