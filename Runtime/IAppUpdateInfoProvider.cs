using System.Threading;
using System.Threading.Tasks;

namespace BizSim.Google.Play.AppUpdate
{
    /// <summary>
    /// Provider that queries the Play Store for update availability. The returned
    /// <see cref="AppUpdateInfo"/> is a snapshot at the time of the call; callers should
    /// re-query on resume.
    /// </summary>
    public interface IAppUpdateInfoProvider
    {
        /// <summary>
        /// Callers MUST pass a positive, resolved timeoutSeconds — the controller does sentinel
        /// resolution against <c>AppUpdateSettings.DefaultTimeoutSeconds</c> BEFORE delegating here.
        /// Providers do NOT consult the Settings asset themselves (single source of truth).
        /// </summary>
        Task<AppUpdateInfo> GetAppUpdateInfoAsync(CancellationToken ct, float timeoutSeconds);
    }
}
