#if UNITY_ANDROID && !UNITY_EDITOR
using System;
using UnityEngine;

namespace BizSim.Google.Play.AppUpdate
{
    /// <summary>
    /// <see cref="AndroidJavaProxy"/> subclass implementing the Java interface
    /// <c>com.bizsim.google.play.appupdate.AppUpdateBridge$IInstallStateCallback</c>. Method
    /// names and signatures MUST match the Java interface verbatim (case-sensitive).
    /// </summary>
    /// <remarks>
    /// CRITICAL (Task 8 R2): <see cref="InstallStateListenerBridge"/> on the Java side invokes
    /// this callback directly from Play Core's background worker thread. Unlike the other
    /// bridge callbacks, Java does NOT wrap it in <c>mainHandler.post</c>. Therefore every
    /// <c>onStateUpdate</c> invocation MUST marshal via <see cref="UnityMainThreadDispatcher"/>
    /// before touching any Unity-owned state — otherwise IL2CPP crashes when the handler lambda
    /// hits the controller's event or the install-state stream from a worker thread.
    /// </remarks>
    internal sealed class InstallStateListenerProxy : AndroidJavaProxy
    {
        private readonly Action<InstallState> _onStateUpdate;

        public InstallStateListenerProxy(Action<InstallState> onStateUpdate)
            : base("com.bizsim.google.play.appupdate.AppUpdateBridge$IInstallStateCallback")
        {
            _onStateUpdate = onStateUpdate ?? throw new ArgumentNullException(nameof(onStateUpdate));
        }

        // First line MUST marshal to main thread. Construction of the struct happens inside the
        // dispatched lambda so nothing touches Unity state on the worker thread.
        public void onStateUpdate(int installStatus, long bytesDownloaded, long totalBytesToDownload, int installErrorCode)
        {
            UnityMainThreadDispatcher.Enqueue(() =>
            {
                var state = new InstallState(
                    installStatus: (InstallStatus)installStatus,
                    bytesDownloaded: bytesDownloaded,
                    totalBytesToDownload: totalBytesToDownload,
                    installErrorCode: (InstallErrorCode)installErrorCode,
                    observedAtUtc: DateTime.UtcNow);
                try
                {
                    _onStateUpdate.Invoke(state);
                }
                catch (Exception ex)
                {
                    BizSimLogger.Error($"InstallState handler threw: {ex.Message}");
                }
            });
        }
    }
}
#endif
