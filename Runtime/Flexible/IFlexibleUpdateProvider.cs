using System.Threading;
using System.Threading.Tasks;

namespace BizSim.Google.Play.AppUpdate
{
    /// <summary>
    /// Provider for the flexible (background-download) update flow.
    /// </summary>
    public interface IFlexibleUpdateProvider
    {
        Task<AppUpdateError?> StartAsync(AppUpdateOptions opts, CancellationToken ct, float timeoutSeconds);

        /// <summary>
        /// Finalizes the flexible flow after <see cref="InstallStatus.Downloaded"/> has been observed.
        /// The controller enforces the state precondition; providers MAY assume it holds.
        /// Process may die after this call.
        /// </summary>
        Task CompleteAsync(CancellationToken ct);
    }
}
