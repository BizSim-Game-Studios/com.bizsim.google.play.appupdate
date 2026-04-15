#if BIZSIM_UNITASK
using System.Threading;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;

namespace BizSim.Google.Play.AppUpdate.UniTask
{
    /// <summary>
    /// UniTask adapter for <see cref="AppUpdateController"/>. Consumers who pull UniTask into their
    /// project automatically get the <c>*UniTask</c> variants on the controller.
    /// </summary>
    /// <remarks>
    /// Post-freeze r5 audit round-3 B-A4: all UniTask extensions use the <c>-1f</c> sentinel,
    /// matching <see cref="AppUpdateController"/>'s public API per CROSS-INVARIANTS §12.2.1. The
    /// extensions just forward to the controller, which performs sentinel resolution against
    /// <c>AppUpdateSettings.DefaultTimeoutSeconds</c>. Hardcoded 30f / 60f / 120f defaults are
    /// intentionally avoided here — callers who want to override the default pass a positive value.
    /// </remarks>
    public static class AppUpdateUniTaskExtensions
    {
        public static async UniTask<AppUpdateInfo> CheckForUpdateAsyncUniTask(
            this AppUpdateController c,
            CancellationToken ct = default,
            float timeoutSeconds = -1f)
        {
            return await c.CheckForUpdateAsync(ct, timeoutSeconds);
        }

        public static async UniTask<AppUpdateError?> StartFlexibleUpdateAsyncUniTask(
            this AppUpdateController c,
            AppUpdateOptions options = null,
            CancellationToken ct = default,
            float timeoutSeconds = -1f)
        {
            return await c.StartFlexibleUpdateAsync(options, ct, timeoutSeconds);
        }

        public static async UniTask<AppUpdateError?> StartImmediateUpdateAsyncUniTask(
            this AppUpdateController c,
            AppUpdateOptions options = null,
            CancellationToken ct = default,
            float timeoutSeconds = -1f)
        {
            return await c.StartImmediateUpdateAsync(options, ct, timeoutSeconds);
        }

        public static async UniTask CompleteFlexibleUpdateAsyncUniTask(
            this AppUpdateController c,
            CancellationToken ct = default)
        {
            await c.CompleteFlexibleUpdateAsync(ct);
        }

        /// <summary>
        /// Wraps the controller's <c>IAsyncEnumerable&lt;InstallState&gt;</c> stream as a
        /// <see cref="UniTaskAsyncEnumerable{T}"/> so consumers can use UniTask LINQ operators.
        /// </summary>
        public static IUniTaskAsyncEnumerable<InstallState> ReadInstallStatesAsUniTaskAsyncEnumerable(
            this AppUpdateController c,
            CancellationToken ct = default)
        {
            return UniTaskAsyncEnumerable.Create<InstallState>(async (writer, token) =>
            {
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, token);
                await foreach (var state in c.ReadInstallStatesAsync(linkedCts.Token))
                {
                    await writer.YieldAsync(state);
                }
            });
        }
    }
}
#endif
