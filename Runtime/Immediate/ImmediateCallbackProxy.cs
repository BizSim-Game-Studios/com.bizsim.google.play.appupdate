#if UNITY_ANDROID && !UNITY_EDITOR
using System;
using UnityEngine;

namespace BizSim.Google.Play.AppUpdate
{
    /// <summary>
    /// <see cref="AndroidJavaProxy"/> subclass implementing the Java interface
    /// <c>com.bizsim.google.play.appupdate.AppUpdateBridge$IFlowLaunchCallback</c> for the
    /// immediate (blocking full-screen) flow. Method names and signatures MUST match the Java
    /// interface verbatim (case-sensitive).
    /// </summary>
    /// <remarks>
    /// Shape matches <see cref="FlexibleCallbackProxy"/> because both flows use the same Java
    /// interface. The resolution policy differs: immediate resolves its launch TCS on
    /// <c>onActivityResult</c> (the activity result is the terminal signal — the blocking UI
    /// has completed one way or the other), while flexible resolves on <c>onLaunched</c>.
    /// </remarks>
    internal sealed class ImmediateCallbackProxy : AndroidJavaProxy
    {
        private readonly Action _onLaunched;
        private readonly Action<int, int> _onActivityResult;
        private readonly Action<int, string> _onLaunchError;

        public ImmediateCallbackProxy(
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
