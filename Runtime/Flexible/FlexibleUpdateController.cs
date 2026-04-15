#if UNITY_ANDROID && !UNITY_EDITOR
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace BizSim.Google.Play.AppUpdate
{
    /// <summary>
    /// Android-side provider for the flexible update flow. Delegates to the Java
    /// <c>AppUpdateBridge.startUpdateFlow</c> with <c>AppUpdateType.Flexible</c> and resolves the
    /// launch TCS when the Play dialog shows. Download progress is exposed separately via the
    /// install-state listener (see <see cref="InstallStateListenerController"/>).
    /// </summary>
    /// <remarks>
    /// Stateless per-call. In-flight guarding lives on <see cref="AppUpdateController"/>; this
    /// class does not enforce a single concurrent flow.
    /// </remarks>
    internal sealed class FlexibleUpdateController : IFlexibleUpdateProvider
    {
        public async Task<AppUpdateError?> StartAsync(AppUpdateOptions opts, CancellationToken ct, float timeoutSeconds)
        {
            if (opts == null) throw new ArgumentNullException(nameof(opts));
            if (opts.AppUpdateType != AppUpdateType.Flexible)
                throw new ArgumentException($"FlexibleUpdateController received AppUpdateOptions with type {opts.AppUpdateType}", nameof(opts));
            ct.ThrowIfCancellationRequested();

            var bridge = AndroidAppUpdateInfoProvider.GetBridge();
            if (bridge == null)
            {
                return new AppUpdateError(
                    InstallErrorCode.BridgeNotInitialized,
                    "AppUpdateBridge failed to initialize",
                    retryable: true,
                    occurredAtUtc: DateTime.UtcNow);
            }

            var tcs = new TaskCompletionSource<AppUpdateError?>(TaskCreationOptions.RunContinuationsAsynchronously);

            Action onLaunched = () =>
            {
                // Flexible: launch success = TCS resolves with null error. Download continues in
                // background and install-state listener fires InstallState events from here on.
                tcs.TrySetResult(null);
            };
            Action<int, int> onActivityResult = (resultCode, inAppCode) =>
            {
                // Advisory: log only. Download proceeds regardless of dialog dismissal.
                var activity = (ActivityResultCode)resultCode;
                BizSimLogger.Info($"Flexible flow activity result: {activity} (inApp={inAppCode})");
            };
            Action<int, string> onLaunchError = (code, message) =>
            {
                var mapped = (InstallErrorCode)code;
                var err = new AppUpdateError(
                    mapped,
                    message ?? "",
                    retryable: AppUpdateError.IsRetryable(mapped),
                    occurredAtUtc: DateTime.UtcNow);
                tcs.TrySetResult(err);
            };

            var proxy = new FlexibleCallbackProxy(onLaunched, onActivityResult, onLaunchError);

            using (ct.Register(() => tcs.TrySetCanceled(ct)))
            {
                try
                {
                    bridge.Call("startUpdateFlow", (int)AppUpdateType.Flexible, opts.AllowAssetPackDeletion, proxy);
                }
                catch (AndroidJavaException aje)
                {
                    var err = new AppUpdateError(
                        InstallErrorCode.ErrorInternalError,
                        $"startUpdateFlow JNI call failed: {aje.Message}",
                        retryable: true,
                        occurredAtUtc: DateTime.UtcNow);
                    tcs.TrySetResult(err);
                }

                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(timeoutSeconds), ct);
                var completed = await Task.WhenAny(tcs.Task, timeoutTask).ConfigureAwait(false);

                if (completed == timeoutTask)
                {
                    ct.ThrowIfCancellationRequested();
                    var err = new AppUpdateError(
                        InstallErrorCode.Timeout,
                        $"StartFlexibleUpdateAsync timed out after {timeoutSeconds:F1}s",
                        retryable: true,
                        occurredAtUtc: DateTime.UtcNow);
                    BizSimLogger.Info($"Flexible flow launch timed out after {timeoutSeconds:F1}s");
                    return err;
                }

                return await tcs.Task.ConfigureAwait(false);
            }
        }

        public async Task CompleteAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            var bridge = AndroidAppUpdateInfoProvider.GetBridge();
            if (bridge == null)
            {
                throw new AppUpdateException(new AppUpdateError(
                    InstallErrorCode.BridgeNotInitialized,
                    "AppUpdateBridge failed to initialize",
                    retryable: true,
                    occurredAtUtc: DateTime.UtcNow));
            }

            var tcs = new TaskCompletionSource<AppUpdateError?>(TaskCreationOptions.RunContinuationsAsynchronously);
            var proxy = new CompleteUpdateCallbackProxy(tcs);

            using (ct.Register(() => tcs.TrySetCanceled(ct)))
            {
                try
                {
                    bridge.Call("completeUpdate", proxy);
                }
                catch (AndroidJavaException aje)
                {
                    throw new AppUpdateException(new AppUpdateError(
                        InstallErrorCode.ErrorInternalError,
                        $"completeUpdate JNI call failed: {aje.Message}",
                        retryable: true,
                        occurredAtUtc: DateTime.UtcNow));
                }

                var result = await tcs.Task.ConfigureAwait(false);
                if (result.HasValue)
                    throw new AppUpdateException(result.Value);

                // Process typically dies here as Play restarts the app to apply the update.
            }
        }
    }
}
#endif
