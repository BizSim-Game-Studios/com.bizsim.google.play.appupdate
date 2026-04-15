#if UNITY_ANDROID && !UNITY_EDITOR
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace BizSim.Google.Play.AppUpdate
{
    /// <summary>
    /// Android-side provider for the immediate update flow. Delegates to the Java
    /// <c>AppUpdateBridge.startUpdateFlow</c> with <c>AppUpdateType.Immediate</c> and resolves the
    /// TCS when the blocking full-screen activity returns a result.
    /// </summary>
    /// <remarks>
    /// Stateless per-call. In-flight guarding lives on <see cref="AppUpdateController"/>; this
    /// class does not enforce a single concurrent flow.
    /// </remarks>
    internal sealed class ImmediateUpdateController : IImmediateUpdateProvider
    {
        public async Task<AppUpdateError?> StartAsync(AppUpdateOptions opts, CancellationToken ct, float timeoutSeconds)
        {
            if (opts == null) throw new ArgumentNullException(nameof(opts));
            if (opts.AppUpdateType != AppUpdateType.Immediate)
                throw new ArgumentException($"ImmediateUpdateController received AppUpdateOptions with type {opts.AppUpdateType}", nameof(opts));
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
                // Immediate: onLaunched is advisory. The terminal signal is onActivityResult.
                BizSimLogger.Info("Immediate flow launched — awaiting activity result");
            };
            Action<int, int> onActivityResult = (resultCode, inAppCode) =>
            {
                var activity = (ActivityResultCode)resultCode;
                switch (activity)
                {
                    case ActivityResultCode.Ok:
                        tcs.TrySetResult(null);
                        break;
                    case ActivityResultCode.Canceled:
                        tcs.TrySetResult(new AppUpdateError(
                            InstallErrorCode.CancelledByCaller,
                            "User canceled the immediate update dialog",
                            retryable: false,
                            occurredAtUtc: DateTime.UtcNow));
                        break;
                    case ActivityResultCode.InAppUpdateFailed:
                    default:
                        tcs.TrySetResult(new AppUpdateError(
                            InstallErrorCode.ErrorInternalError,
                            $"Immediate flow failed (resultCode={resultCode}, inApp={inAppCode})",
                            retryable: true,
                            occurredAtUtc: DateTime.UtcNow));
                        break;
                }
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

            var proxy = new ImmediateCallbackProxy(onLaunched, onActivityResult, onLaunchError);

            using (ct.Register(() => tcs.TrySetCanceled(ct)))
            {
                try
                {
                    bridge.Call("startUpdateFlow", (int)AppUpdateType.Immediate, opts.AllowAssetPackDeletion, proxy);
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
                        $"StartImmediateUpdateAsync timed out after {timeoutSeconds:F1}s",
                        retryable: true,
                        occurredAtUtc: DateTime.UtcNow);
                    BizSimLogger.Info($"Immediate flow timed out after {timeoutSeconds:F1}s");
                    return err;
                }

                return await tcs.Task.ConfigureAwait(false);
            }
        }

        public async Task<bool> IsImmediateUpdateInProgressAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            // Re-query Play Store for availability. A "developer-triggered update in progress"
            // state means an immediate flow was previously started and the user should be sent
            // back through the blocking UI on app resume. The interface carries no timeout
            // parameter, so the package's built-in fallback constant is used here — NOT a
            // magic number. Consumers who need a tighter budget should call
            // CheckForUpdateAsync(timeout) themselves and inspect the result.
            var infoProvider = new AndroidAppUpdateInfoProvider();
            var info = await infoProvider.GetAppUpdateInfoAsync(
                ct,
                timeoutSeconds: AppUpdateSettings.DefaultTimeoutSecondsFallback).ConfigureAwait(false);
            return info.UpdateAvailability == UpdateAvailability.DeveloperTriggeredUpdateInProgress;
        }
    }
}
#endif
