using System.Threading;
using System.Threading.Tasks;

namespace BizSim.Google.Play.AppUpdate
{
    /// <summary>
    /// Provider for the immediate (blocking full-screen) update flow.
    /// </summary>
    public interface IImmediateUpdateProvider
    {
        Task<AppUpdateError?> StartAsync(AppUpdateOptions opts, CancellationToken ct, float timeoutSeconds);

        /// <summary>
        /// Returns true if an immediate update was previously started and is still in progress
        /// (detected by <c>UpdateAvailability.DeveloperTriggeredUpdateInProgress</c>). Consumers
        /// call this on <c>OnApplicationFocus</c> to re-launch the flow.
        /// </summary>
        Task<bool> IsImmediateUpdateInProgressAsync(CancellationToken ct);
    }
}
