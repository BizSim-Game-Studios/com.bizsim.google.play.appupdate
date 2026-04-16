using System;
using UnityEngine;
using UnityEngine.UI;
using BizSim.Google.Play.AppUpdate;

namespace BizSim.Google.Play.AppUpdate.Samples.BasicIntegration
{
    /// <summary>
    /// One-script bootstrap that demonstrates the full Wave 1 + Wave 2 app-update flow:
    /// <list type="number">
    /// <item><description><see cref="AppUpdateController.Instance.RecordLaunch"/> on cold start.</description></item>
    /// <item><description><see cref="AppUpdateController.Instance.CheckForUpdateAsync"/> — the policy engine decides.</description></item>
    /// <item><description>If flexible: drives a progress bar + "remind later" → auto-complete after cap.</description></item>
    /// <item><description>If immediate: the Play Store full-screen dialog handles everything.</description></item>
    /// <item><description>Mock mode in Editor: uses <see cref="AppUpdateMockConfig"/> for testable flows.</description></item>
    /// </list>
    /// </summary>
    public class BasicIntegrationBootstrap : MonoBehaviour
    {
        [Header("UI References (optional — leave null for headless demo)")]
        [SerializeField] private Slider _downloadProgressBar;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _remindLaterButton;
        [SerializeField] private Text _statusLabel;

        [Header("Remind Later")]
        [Tooltip("How many minutes to defer the update install when the user taps Remind Later.")]
        [SerializeField] private int _remindLaterMinutes = 30;

        private AppUpdateDownloadProgressAdapter _progressAdapter;

        private async void Start()
        {
            // Hide buttons until they're needed.
            if (_restartButton != null) _restartButton.gameObject.SetActive(false);
            if (_remindLaterButton != null) _remindLaterButton.gameObject.SetActive(false);
            if (_downloadProgressBar != null) _downloadProgressBar.value = 0f;

            var ctrl = AppUpdateController.Instance;
            if (ctrl == null)
            {
                SetStatus("AppUpdateController not available (not in play mode?).");
                return;
            }

            // Wire events.
            ctrl.OnInstallStateChanged += OnInstallStateChanged;
            ctrl.OnError += OnError;

            // Wave 2: progress adapter with debounce.
            _progressAdapter = new AppUpdateDownloadProgressAdapter(
                p =>
                {
                    if (_downloadProgressBar != null) _downloadProgressBar.value = p;
                },
                minIntervalSeconds: 0.1f);
            ctrl.OnInstallStateChanged += _progressAdapter.OnInstallStateChanged;

            // Step 1: Record launch for policy engine.
            ctrl.RecordLaunch();
            SetStatus("Launch recorded. Checking for update...");

            // Step 2: Check for update — the policy engine decides the flow.
            try
            {
                var info = await ctrl.CheckForUpdateAsync();
                if (!info.IsUpdateAvailable)
                {
                    SetStatus("No update available.");
                }
                else
                {
                    SetStatus($"Update v{info.AvailableVersionCode} (priority {info.UpdatePriority}) — policy engine handled flow.");
                }
            }
            catch (InvalidOperationException ex)
            {
                // Expected for offline, non-Play-install, or cooldown blocks.
                SetStatus($"Update check blocked: {ex.Message}");
            }
            catch (Exception ex)
            {
                SetStatus($"Update check failed: {ex.Message}");
                Debug.LogError($"[BasicIntegrationBootstrap] {ex}");
            }
        }

        private void OnDestroy()
        {
            var ctrl = AppUpdateController.Instance;
            if (ctrl == null) return;
            ctrl.OnInstallStateChanged -= OnInstallStateChanged;
            ctrl.OnError -= OnError;
            if (_progressAdapter != null)
                ctrl.OnInstallStateChanged -= _progressAdapter.OnInstallStateChanged;
        }

        private void OnInstallStateChanged(InstallState state)
        {
            switch (state.InstallStatus)
            {
                case InstallStatus.Downloading:
                    SetStatus($"Downloading: {state.DownloadProgress:P0}");
                    break;

                case InstallStatus.Downloaded:
                    SetStatus("Download complete. Restart or remind later.");
                    if (_restartButton != null) _restartButton.gameObject.SetActive(true);
                    if (_remindLaterButton != null) _remindLaterButton.gameObject.SetActive(true);
                    break;

                case InstallStatus.Installing:
                    SetStatus("Installing...");
                    break;

                case InstallStatus.Installed:
                    SetStatus("Installed.");
                    break;

                case InstallStatus.Failed:
                    SetStatus($"Install failed: {state.InstallErrorCode}");
                    break;

                case InstallStatus.Canceled:
                    SetStatus("Update canceled by user.");
                    break;
            }
        }

        private void OnError(AppUpdateError err)
        {
            SetStatus($"Error: {err.Code} — {err.Message}");
            Debug.LogError($"[BasicIntegrationBootstrap] {err.Code}: {err.Message}");
        }

        /// <summary>
        /// Wire to RestartButton.OnClick in the Inspector.
        /// </summary>
        public async void OnRestartClicked()
        {
            SetStatus("Completing update (app will restart)...");
            try
            {
                await AppUpdateController.Instance.CompleteFlexibleUpdateAsync();
            }
            catch (Exception ex)
            {
                SetStatus($"Complete failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Wire to RemindLaterButton.OnClick in the Inspector.
        /// Uses the Wave 2 <see cref="AppUpdateController.CompleteFlexibleUpdateAsync(TimeSpan, CancellationToken)"/>
        /// overload with the configured delay, capped by <see cref="AppUpdateSettings.PostDownloadRemindLaterMaxHours"/>.
        /// </summary>
        public async void OnRemindLaterClicked()
        {
            if (_restartButton != null) _restartButton.gameObject.SetActive(false);
            if (_remindLaterButton != null) _remindLaterButton.gameObject.SetActive(false);

            var delay = TimeSpan.FromMinutes(_remindLaterMinutes);
            SetStatus($"Remind later: will auto-complete in {_remindLaterMinutes} min.");

            try
            {
                await AppUpdateController.Instance.CompleteFlexibleUpdateAsync(delay);
            }
            catch (Exception ex)
            {
                SetStatus($"Remind-later failed: {ex.Message}");
            }
        }

        private void SetStatus(string msg)
        {
            if (_statusLabel != null)
                _statusLabel.text = msg;
            Debug.Log($"[BasicIntegrationBootstrap] {msg}");
        }
    }
}
