#if UNITY_ANDROID && !UNITY_EDITOR
using System;
using System.Threading.Tasks;
using UnityEngine;

namespace BizSim.Google.Play.AppUpdate
{
    /// <summary>
    /// <see cref="AndroidJavaProxy"/> subclass implementing the Java interface
    /// <c>com.bizsim.google.play.appupdate.AppUpdateBridge$ICompleteCallback</c>. Used by
    /// <see cref="FlexibleUpdateController"/> when finalizing a downloaded flexible update.
    /// </summary>
    /// <remarks>
    /// After <c>completeUpdate</c> returns successfully, the host process usually terminates as
    /// Play restarts the app to apply the update. Any continuation awaiting the resolved TCS may
    /// never run — consumer code must not rely on post-complete logic.
    /// </remarks>
    internal sealed class CompleteUpdateCallbackProxy : AndroidJavaProxy
    {
        private readonly TaskCompletionSource<AppUpdateError?> _tcs;

        public CompleteUpdateCallbackProxy(TaskCompletionSource<AppUpdateError?> tcs)
            : base("com.bizsim.google.play.appupdate.AppUpdateBridge$ICompleteCallback")
        {
            _tcs = tcs ?? throw new ArgumentNullException(nameof(tcs));
        }

        public void onCompleteInvoked()
        {
            UnityMainThreadDispatcher.Enqueue(() => _tcs.TrySetResult(null));
        }

        public void onCompleteError(int errorCode, string message)
        {
            UnityMainThreadDispatcher.Enqueue(() =>
            {
                var code = (InstallErrorCode)errorCode;
                var err = new AppUpdateError(
                    code: code,
                    message: message ?? "",
                    retryable: AppUpdateError.IsRetryable(code),
                    occurredAtUtc: DateTime.UtcNow);
                _tcs.TrySetResult(err);
            });
        }
    }
}
#endif
