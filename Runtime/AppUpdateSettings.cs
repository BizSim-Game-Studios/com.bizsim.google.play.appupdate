using UnityEngine;

namespace BizSim.Google.Play.AppUpdate
{
    // Stub — full content + decorations land in Phase 5 Step 13.0a per CROSS-INVARIANTS §12.
    public sealed class AppUpdateSettings : ScriptableObject
    {
        // Path constants per CROSS-INVARIANTS §12.5 — keep the two in sync.
        public const string ResourcesLoadKey  = "BizSim/GooglePlay/AppUpdateSettings";
        public const string AssetDatabasePath = "Assets/Resources/" + ResourcesLoadKey + ".asset";

        // Fallback defaults used by the controller when the Settings asset is missing —
        // single source of truth for the "magic numbers" to keep §12 compliance.
        public const int   DefaultInstallStateQueueCapacity = 32;
        public const float DefaultTimeoutSecondsFallback    = 60f;

        public bool LogsEnabled = true;
        public BizSimLogger.LogLevel LogLevel = BizSimLogger.LogLevel.Info;
        public bool UseMockInDevelopmentBuild = false;
        public bool EnableAnalyticsByDefault = false;
        public int InstallStateQueueCapacity = DefaultInstallStateQueueCapacity;
        public float DefaultTimeoutSeconds = DefaultTimeoutSecondsFallback;
        public bool AutoStartInstallStateListener = true;
    }
}
