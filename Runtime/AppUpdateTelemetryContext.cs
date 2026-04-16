using System;

namespace BizSim.Google.Play.AppUpdate
{
    /// <summary>
    /// Immutable context bag passed to every <see cref="IAppUpdateAnalyticsAdapterV2"/> event.
    /// Captures the state at the moment the event fires so analytics adapters can enrich
    /// their telemetry without coupling to controller internals.
    /// </summary>
    [Serializable]
    public readonly struct AppUpdateTelemetryContext : IEquatable<AppUpdateTelemetryContext>
    {
        /// <summary>Current app version string (<c>Application.version</c>).</summary>
        public readonly string AppVersion;

        /// <summary>Version code of the available update (from <see cref="AppUpdateInfo.AvailableVersionCode"/>).</summary>
        public readonly int AvailableVersionCode;

        /// <summary>Google-assigned update priority 0-5.</summary>
        public readonly int UpdatePriority;

        /// <summary>Days since the update was published, or -1 if unknown.</summary>
        public readonly int StalenessDays;

        /// <summary>Why this event was triggered (e.g. "auto_check", "resume", "manual").</summary>
        public readonly string TriggerReason;

        /// <summary>Session count from the session tracker at the time of the event.</summary>
        public readonly int SessionCount;

        /// <summary>
        /// Opaque string for A/B test variant identification. Null when not running an experiment.
        /// </summary>
        public readonly string VariantId;

        public AppUpdateTelemetryContext(
            string appVersion,
            int availableVersionCode,
            int updatePriority,
            int stalenessDays,
            string triggerReason,
            int sessionCount,
            string variantId)
        {
            AppVersion = appVersion ?? "";
            AvailableVersionCode = availableVersionCode;
            UpdatePriority = updatePriority;
            StalenessDays = stalenessDays;
            TriggerReason = triggerReason ?? "";
            SessionCount = sessionCount;
            VariantId = variantId;
        }

        public bool Equals(AppUpdateTelemetryContext other)
            => string.Equals(AppVersion, other.AppVersion, StringComparison.Ordinal)
            && AvailableVersionCode == other.AvailableVersionCode
            && UpdatePriority == other.UpdatePriority
            && StalenessDays == other.StalenessDays
            && string.Equals(TriggerReason, other.TriggerReason, StringComparison.Ordinal)
            && SessionCount == other.SessionCount
            && string.Equals(VariantId, other.VariantId, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is AppUpdateTelemetryContext c && Equals(c);

        public override int GetHashCode()
        {
            unchecked
            {
                int h = AppVersion?.GetHashCode() ?? 0;
                h = (h * 397) ^ AvailableVersionCode;
                h = (h * 397) ^ UpdatePriority;
                h = (h * 397) ^ StalenessDays;
                h = (h * 397) ^ (TriggerReason?.GetHashCode() ?? 0);
                h = (h * 397) ^ SessionCount;
                h = (h * 397) ^ (VariantId?.GetHashCode() ?? 0);
                return h;
            }
        }

        public static bool operator ==(AppUpdateTelemetryContext a, AppUpdateTelemetryContext b) => a.Equals(b);
        public static bool operator !=(AppUpdateTelemetryContext a, AppUpdateTelemetryContext b) => !a.Equals(b);

        public override string ToString()
            => $"TelemetryContext(v{AppVersion}, vCode={AvailableVersionCode}, pri={UpdatePriority}, " +
               $"stale={StalenessDays}d, trigger={TriggerReason}, sessions={SessionCount}, " +
               $"variant={VariantId ?? "null"})";
    }
}
