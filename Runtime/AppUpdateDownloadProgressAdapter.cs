using System;
using UnityEngine;

namespace BizSim.Google.Play.AppUpdate
{
    /// <summary>
    /// Bridges <see cref="AppUpdateController.OnInstallStateChanged"/> events to an
    /// <see cref="IProgress{Single}"/> consumer with configurable debounce. Useful for
    /// binding download progress to UI sliders or progress bars without flooding the
    /// UI thread on every install-state callback.
    /// <para>
    /// Usage:
    /// <code>
    /// var progress = new AppUpdateDownloadProgressAdapter(
    ///     p => slider.value = p,
    ///     minIntervalSeconds: 0.1f);
    /// AppUpdateController.Instance.OnInstallStateChanged += progress.OnInstallStateChanged;
    /// </code>
    /// </para>
    /// </summary>
    public sealed class AppUpdateDownloadProgressAdapter : IProgress<float>
    {
        readonly Action<float> _handler;
        readonly float _minIntervalSeconds;
        float _lastReportTime;
        float _lastReportedValue;
        bool _completed;

        /// <summary>
        /// Creates a new adapter that debounces progress reports.
        /// </summary>
        /// <param name="handler">Called on the main thread with a 0-1 progress value.</param>
        /// <param name="minIntervalSeconds">Minimum seconds between reports. 0 for no debounce.</param>
        public AppUpdateDownloadProgressAdapter(Action<float> handler, float minIntervalSeconds = 0.1f)
        {
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
            _minIntervalSeconds = Mathf.Max(0f, minIntervalSeconds);
            _lastReportTime = -999f;
            _lastReportedValue = -1f;
        }

        /// <summary>
        /// Feed this into <see cref="AppUpdateController.OnInstallStateChanged"/>.
        /// Reports are debounced except for the terminal states (Downloaded=1.0, Failed, Canceled).
        /// </summary>
        public void OnInstallStateChanged(InstallState state)
        {
            if (_completed) return;

            switch (state.InstallStatus)
            {
                case InstallStatus.Downloading:
                    var progress = state.DownloadProgress;
                    var now = Time.unscaledTime;
                    if (now - _lastReportTime >= _minIntervalSeconds
                        || Math.Abs(progress - _lastReportedValue) > 0.001f)
                    {
                        _lastReportTime = now;
                        _lastReportedValue = progress;
                        SafeReport(progress);
                    }
                    break;

                case InstallStatus.Downloaded:
                    _completed = true;
                    SafeReport(1f);
                    break;

                case InstallStatus.Failed:
                case InstallStatus.Canceled:
                    _completed = true;
                    // Report last known value — don't snap to 0 or 1.
                    break;
            }
        }

        /// <summary>
        /// <see cref="IProgress{T}"/> implementation. Reports the value directly without debounce.
        /// </summary>
        public void Report(float value) => SafeReport(Mathf.Clamp01(value));

        /// <summary>
        /// Resets the adapter so it can be reused for a new download session.
        /// </summary>
        public void Reset()
        {
            _completed = false;
            _lastReportTime = -999f;
            _lastReportedValue = -1f;
        }

        void SafeReport(float value)
        {
            try { _handler(value); }
            catch (Exception ex)
            {
                BizSimLogger.Warning($"AppUpdateDownloadProgressAdapter handler threw: {ex.Message}");
            }
        }
    }
}
