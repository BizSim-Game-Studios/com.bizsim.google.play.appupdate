using UnityEngine;
using BizSim.Google.Play.AppUpdate;

namespace BizSim.Google.Play.AppUpdate.Samples.BasicIntegration
{
    /// <summary>
    /// Demonstrates the immediate update flow: check for update, launch the blocking
    /// full-screen update activity when required, and re-check on every application
    /// focus regain to resume an interrupted flow.
    ///
    /// Google's In-App Updates guide is explicit about the <see cref="OnApplicationFocus"/>
    /// re-check path: if the user backgrounded the immediate flow mid-download, resuming
    /// the app must re-launch it — otherwise the user is left in a broken state where the
    /// Play Store believes an update is in progress but the host activity is idle.
    /// </summary>
    public class ImmediateFlowSample : MonoBehaviour
    {
        private bool _started;

        private async void Start()
        {
            if (_started) return;
            _started = true;

            AppUpdateInfo info;
            try
            {
                info = await AppUpdateController.Instance.CheckForUpdateAsync();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ImmediateFlowSample] CheckForUpdate failed: {ex.Message}");
                return;
            }

            if (info.IsImmediateUpdateRequired())
            {
                var err = await AppUpdateController.Instance.StartImmediateUpdateAsync();
                if (err.HasValue)
                    Debug.LogWarning($"[ImmediateFlowSample] Immediate flow failed: {err.Value.Code}");
                // If we resume here without a process restart, the flow failed or was canceled.
            }
        }

        private async void OnApplicationFocus(bool focused)
        {
            if (!focused) return;

            // Re-check on every focus regain — Google's guide insists on this.
            AppUpdateInfo info;
            try
            {
                info = await AppUpdateController.Instance.CheckForUpdateAsync();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ImmediateFlowSample] CheckForUpdate on resume failed: {ex.Message}");
                return;
            }

            if (info.UpdateAvailability == UpdateAvailability.DeveloperTriggeredUpdateInProgress)
            {
                // Mandatory resume re-launch per Google's In-App Updates guide.
                var err = await AppUpdateController.Instance.StartImmediateUpdateAsync();
                if (err.HasValue)
                    Debug.LogWarning($"[ImmediateFlowSample] Resume immediate failed: {err.Value.Code}");
            }
        }
    }
}
