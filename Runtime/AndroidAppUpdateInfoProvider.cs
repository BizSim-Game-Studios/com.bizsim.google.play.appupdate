#if UNITY_ANDROID && !UNITY_EDITOR
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace BizSim.Google.Play.AppUpdate
{
    /// <summary>
    /// Android JNI provider that queries Play Store for update availability by calling
    /// <c>AppUpdateBridge.requestAppUpdateInfo</c>. Compiled only under
    /// <c>UNITY_ANDROID &amp;&amp; !UNITY_EDITOR</c>.
    /// </summary>
    /// <remarks>
    /// Stateless per-call: every invocation creates its own TCS and proxy. The shared
    /// <see cref="AndroidJavaObject"/> bridge is cached statically (see <see cref="GetBridge"/>)
    /// per the Task 8 R1 finding — <c>AppUpdateBridge.init</c> is called exactly once per
    /// bridge lifetime so the Java singleton does not hold a stale Activity reference.
    /// </remarks>
    internal sealed class AndroidAppUpdateInfoProvider : IAppUpdateInfoProvider
    {
        // Shared bridge instance — lazy-initialized once, reused for the lifetime of the process.
        // Per R1, we never re-init on Activity recreation; the Java side is a synchronized singleton
        // that captures the Activity on first init.
        private static AndroidJavaObject _bridge;
        private static readonly object _bridgeLock = new object();

        internal static AndroidJavaObject GetBridge()
        {
            if (_bridge != null) return _bridge;
            lock (_bridgeLock)
            {
                if (_bridge != null) return _bridge;
                try
                {
                    using (var bridgeClass = new AndroidJavaClass("com.bizsim.google.play.appupdate.AppUpdateBridge"))
                    using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                    using (var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                    {
                        if (currentActivity == null)
                        {
                            BizSimLogger.Error("UnityPlayer.currentActivity is null — cannot create AppUpdateBridge");
                            return null;
                        }
                        // Per R3: Java init may throw IllegalStateException if the fragment shim cannot
                        // attach to the host Activity (not a FragmentActivity, or mid-recreation race).
                        // AndroidJavaException is the C# surface for that.
                        _bridge = bridgeClass.CallStatic<AndroidJavaObject>("init", currentActivity);
                    }
                }
                catch (AndroidJavaException aje)
                {
                    BizSimLogger.Error($"AppUpdateBridge.init failed: {aje.Message}");
                    _bridge = null;
                }
                catch (Exception ex)
                {
                    BizSimLogger.Error($"AppUpdateBridge.init failed (unexpected): {ex.Message}");
                    _bridge = null;
                }
                return _bridge;
            }
        }

        public async Task<AppUpdateInfo> GetAppUpdateInfoAsync(CancellationToken ct, float timeoutSeconds)
        {
            ct.ThrowIfCancellationRequested();

            var bridge = GetBridge();
            if (bridge == null)
            {
                var err = new AppUpdateError(
                    InstallErrorCode.BridgeNotInitialized,
                    "AppUpdateBridge failed to initialize (see previous log for details)",
                    retryable: true,
                    occurredAtUtc: DateTime.UtcNow);
                throw new AppUpdateException(err);
            }

            var tcs = new TaskCompletionSource<AppUpdateInfo>(TaskCreationOptions.RunContinuationsAsynchronously);
            var proxy = new UpdateInfoCallbackProxy(tcs);

            using (ct.Register(() => tcs.TrySetCanceled(ct)))
            {
                try
                {
                    bridge.Call("requestAppUpdateInfo", proxy);
                }
                catch (AndroidJavaException aje)
                {
                    var err = new AppUpdateError(
                        InstallErrorCode.ErrorInternalError,
                        $"requestAppUpdateInfo JNI call failed: {aje.Message}",
                        retryable: true,
                        occurredAtUtc: DateTime.UtcNow);
                    tcs.TrySetException(new AppUpdateException(err));
                }

                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(timeoutSeconds), ct);
                var completed = await Task.WhenAny(tcs.Task, timeoutTask).ConfigureAwait(false);

                if (completed == timeoutTask)
                {
                    ct.ThrowIfCancellationRequested();
                    var err = new AppUpdateError(
                        InstallErrorCode.Timeout,
                        $"GetAppUpdateInfoAsync timed out after {timeoutSeconds:F1}s",
                        retryable: true,
                        occurredAtUtc: DateTime.UtcNow);
                    BizSimLogger.Info($"GetAppUpdateInfo timed out after {timeoutSeconds:F1}s — late callbacks will be ignored");
                    tcs.TrySetException(new AppUpdateException(err));
                    throw new AppUpdateException(err);
                }

                return await tcs.Task.ConfigureAwait(false);
            }
        }
    }
}
#endif
