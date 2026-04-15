#if UNITY_ANDROID && !UNITY_EDITOR
using System;
using UnityEngine;

namespace BizSim.Google.Play.AppUpdate
{
    /// <summary>
    /// Android-side implementation of <see cref="IInstallStateListener"/>. Registers a single
    /// <see cref="InstallStateListenerProxy"/> with the Java bridge via
    /// <c>AppUpdateBridge.setInstallStateCallback</c>, which internally calls
    /// <c>InstallStateListenerBridge.setCallback</c> + <c>register</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="StartListening"/> is guarded by a static <c>_started</c> flag because
    /// <see cref="AppUpdateController"/> survives domain reloads via <c>DontDestroyOnLoad</c>,
    /// but the Unity Editor still wipes static state on domain reload. The flag prevents a
    /// second <c>register</c> call from stacking listeners if <see cref="StartListening"/> is
    /// ever invoked twice.
    /// </para>
    /// <para>
    /// Per Task 8 R2, the proxy itself marshals every <c>onStateUpdate</c> callback through
    /// <see cref="UnityMainThreadDispatcher"/> — this class does not need extra marshaling.
    /// </para>
    /// </remarks>
    internal sealed class InstallStateListenerController : IInstallStateListener
    {
        private static bool _started;
        private InstallStateListenerProxy _proxy;

        public event Action<InstallState> OnStateUpdate;

        public void StartListening()
        {
            if (_started)
            {
                BizSimLogger.Info("InstallStateListener already started — ignoring duplicate StartListening call");
                return;
            }

            var bridge = AndroidAppUpdateInfoProvider.GetBridge();
            if (bridge == null)
            {
                BizSimLogger.Error("Cannot start InstallStateListener — AppUpdateBridge failed to initialize");
                return;
            }

            _proxy = new InstallStateListenerProxy(RaiseStateUpdate);
            try
            {
                bridge.Call("setInstallStateCallback", _proxy);
                _started = true;
            }
            catch (AndroidJavaException aje)
            {
                BizSimLogger.Error($"setInstallStateCallback JNI call failed: {aje.Message}");
                _proxy = null;
            }
        }

        public void StopListening()
        {
            if (!_started) return;

            var bridge = AndroidAppUpdateInfoProvider.GetBridge();
            if (bridge == null)
            {
                BizSimLogger.Warning("Cannot stop InstallStateListener — bridge is null");
                _started = false;
                _proxy = null;
                return;
            }

            try
            {
                bridge.Call("unregisterInstallStateListener");
            }
            catch (AndroidJavaException aje)
            {
                BizSimLogger.Warning($"unregisterInstallStateListener JNI call failed: {aje.Message}");
            }
            finally
            {
                _started = false;
                _proxy = null;
            }
        }

        private void RaiseStateUpdate(InstallState state)
        {
            // Already marshaled to main thread by InstallStateListenerProxy.
            OnStateUpdate?.Invoke(state);
        }
    }
}
#endif
