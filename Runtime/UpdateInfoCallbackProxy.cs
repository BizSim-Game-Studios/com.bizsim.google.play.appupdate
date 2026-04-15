#if UNITY_ANDROID && !UNITY_EDITOR
using System;
using System.Threading.Tasks;
using UnityEngine;

namespace BizSim.Google.Play.AppUpdate
{
    /// <summary>
    /// <see cref="AndroidJavaProxy"/> subclass implementing the Java interface
    /// <c>com.bizsim.google.play.appupdate.AppUpdateBridge$IUpdateInfoCallback</c>. Method names
    /// and signatures MUST match the Java interface verbatim (case-sensitive). Any drift causes
    /// <c>NoSuchMethodError</c> at runtime.
    /// </summary>
    /// <remarks>
    /// Callbacks marshal through <see cref="UnityMainThreadDispatcher"/> so the TCS resolution
    /// happens on the Unity main thread. The Java signature carries <c>stalenessPresent</c> as a
    /// separate boolean flag so we can convey <c>null</c> for <c>ClientVersionStalenessDays</c>
    /// without ambiguous sentinel encoding.
    /// </remarks>
    internal sealed class UpdateInfoCallbackProxy : AndroidJavaProxy
    {
        private readonly TaskCompletionSource<AppUpdateInfo> _tcs;

        public UpdateInfoCallbackProxy(TaskCompletionSource<AppUpdateInfo> tcs)
            : base("com.bizsim.google.play.appupdate.AppUpdateBridge$IUpdateInfoCallback")
        {
            _tcs = tcs ?? throw new ArgumentNullException(nameof(tcs));
        }

        // Signature must match Java IUpdateInfoCallback.onInfoReceived exactly.
        public void onInfoReceived(
            int updateAvailability,
            int availableVersionCode,
            int updatePriority,
            int clientVersionStalenessDays,
            bool stalenessPresent,
            int installStatus,
            long bytesDownloaded,
            long totalBytesToDownload,
            bool flexibleAllowed,
            bool immediateAllowed)
        {
            UnityMainThreadDispatcher.Enqueue(() =>
            {
                try
                {
                    int? staleness = stalenessPresent ? (int?)clientVersionStalenessDays : null;
                    var info = new AppUpdateInfo(
                        updateAvailability: (UpdateAvailability)updateAvailability,
                        availableVersionCode: availableVersionCode,
                        updatePriority: updatePriority,
                        clientVersionStalenessDays: staleness,
                        installStatus: (InstallStatus)installStatus,
                        bytesDownloaded: bytesDownloaded,
                        totalBytesToDownload: totalBytesToDownload,
                        isFlexibleAllowed: flexibleAllowed,
                        isImmediateAllowed: immediateAllowed,
                        fetchedAtUtc: DateTime.UtcNow);
                    _tcs.TrySetResult(info);
                }
                catch (Exception ex)
                {
                    _tcs.TrySetException(ex);
                }
            });
        }

        public void onInfoError(int errorCode, string message)
        {
            UnityMainThreadDispatcher.Enqueue(() =>
            {
                var err = new AppUpdateError(
                    code: (InstallErrorCode)errorCode,
                    message: message ?? "",
                    retryable: AppUpdateError.IsRetryable((InstallErrorCode)errorCode),
                    occurredAtUtc: DateTime.UtcNow);
                _tcs.TrySetException(new AppUpdateException(err));
            });
        }
    }
}
#endif
