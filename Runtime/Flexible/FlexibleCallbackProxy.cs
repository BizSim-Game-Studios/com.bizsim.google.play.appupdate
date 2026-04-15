#if UNITY_ANDROID && !UNITY_EDITOR
using System;
using UnityEngine;

namespace BizSim.Google.Play.AppUpdate
{
    /// <summary>
    /// <see cref="AndroidJavaProxy"/> subclass implementing the Java interface
    /// <c>com.bizsim.google.play.appupdate.AppUpdateBridge$IFlowLaunchCallback</c>. Method names
    /// and signatures MUST match the Java interface verbatim (case-sensitive).
    /// </summary>
    /// <remarks>
    /// Used by <see cref="FlexibleUpdateController"/>. Flexible flow resolves the launch TCS on
    /// <c>onLaunched</c> (download proceeds in the background — install-state listener drives
    /// the rest). Activity results still fire via <c>onActivityResult</c> but are treated as
    /// advisory (logged only) because the download continues even if the user backgrounds the
    /// Play dialog.
    /// </remarks>
    internal sealed class FlexibleCallbackProxy : AndroidJavaProxy
    {
        private readonly Action _onLaunched;
        private readonly Action<int, int> _onActivityResult;
        private readonly Action<int, string> _onLaunchError;

        public FlexibleCallbackProxy(
            Action onLaunched,
            Action<int, int> onActivityResult,
            Action<int, string> onLaunchError)
            : base("com.bizsim.google.play.appupdate.AppUpdateBridge$IFlowLaunchCallback")
        {
            _onLaunched = onLaunched;
            _onActivityResult = onActivityResult;
            _onLaunchError = onLaunchError;
        }

        public void onLaunched()
            => UnityMainThreadDispatcher.Enqueue(() => _onLaunched?.Invoke());

        public void onActivityResult(int resultCode, int inAppUpdateResultCode)
            => UnityMainThreadDispatcher.Enqueue(() => _onActivityResult?.Invoke(resultCode, inAppUpdateResultCode));

        public void onLaunchError(int errorCode, string message)
            => UnityMainThreadDispatcher.Enqueue(() => _onLaunchError?.Invoke(errorCode, message ?? ""));
    }
}
#endif
