using UnityEngine;
using UnityEngine.UI;
using BizSim.Google.Play.AppUpdate;

namespace BizSim.Google.Play.AppUpdate.Samples.BasicIntegration
{
    /// <summary>
    /// Demonstrates the flexible update flow: check for update, start download in the
    /// background, surface download progress via an <see cref="AppUpdateController.OnInstallStateChanged"/>
    /// subscription, and call <see cref="AppUpdateController.CompleteFlexibleUpdateAsync"/>
    /// when the install state reaches <see cref="InstallStatus.Downloaded"/>.
    ///
    /// Wire <see cref="OnRestartButtonClicked"/> to the Button's OnClick event in the Inspector.
    /// </summary>
    public class FlexibleFlowSample : MonoBehaviour
    {
        [SerializeField] private Slider _downloadProgressBar;
        [SerializeField] private Button _restartButton;

        private async void Start()
        {
            if (_restartButton != null) _restartButton.gameObject.SetActive(false);

            var ctrl = AppUpdateController.Instance;
            ctrl.OnInstallStateChanged += OnState;
            ctrl.OnError += OnError;

            AppUpdateInfo info;
            try
            {
                info = await ctrl.CheckForUpdateAsync();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[FlexibleFlowSample] CheckForUpdate failed: {ex.Message}");
                return;
            }

            if (!info.IsFlexibleUpdateRecommended())
            {
                Debug.Log("[FlexibleFlowSample] Flexible update not recommended for current AppUpdateInfo.");
                return;
            }

            var err = await ctrl.StartFlexibleUpdateAsync();
            if (err.HasValue)
                Debug.LogWarning($"[FlexibleFlowSample] Flexible flow failed: {err.Value.Code}");
        }

        private void OnDestroy()
        {
            var ctrl = AppUpdateController.Instance;
            if (ctrl == null) return;
            ctrl.OnInstallStateChanged -= OnState;
            ctrl.OnError -= OnError;
        }

        private void OnState(InstallState s)
        {
            switch (s.InstallStatus)
            {
                case InstallStatus.Downloading:
                    if (_downloadProgressBar != null)
                        _downloadProgressBar.value = s.DownloadProgress;
                    break;
                case InstallStatus.Downloaded:
                    Debug.Log("[FlexibleFlowSample] Download complete — restart required.");
                    if (_restartButton != null)
                        _restartButton.gameObject.SetActive(true);
                    break;
                case InstallStatus.Failed:
                    Debug.LogError($"[FlexibleFlowSample] Install failed: {s.InstallErrorCode}");
                    break;
            }
        }

        private void OnError(AppUpdateError err)
        {
            Debug.LogError($"[FlexibleFlowSample] Controller error: {err.Code} — {err.Message}");
        }

        /// <summary>
        /// Hook this up to the restart Button's OnClick in the Inspector. The process
        /// usually dies inside <see cref="AppUpdateController.CompleteFlexibleUpdateAsync"/>,
        /// so any code below the await may never run.
        /// </summary>
        public async void OnRestartButtonClicked()
        {
            await AppUpdateController.Instance.CompleteFlexibleUpdateAsync();
            // Process usually dies here. Any code below may never run.
        }
    }
}
