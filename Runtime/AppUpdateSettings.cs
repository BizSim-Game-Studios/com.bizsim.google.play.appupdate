using UnityEngine;

namespace BizSim.Google.Play.AppUpdate
{
    // Stub — full content + decorations land in Phase 5 Step 13.0a per CROSS-INVARIANTS §12.
    public sealed class AppUpdateSettings : ScriptableObject
    {
        // Path constants per CROSS-INVARIANTS §12.5 — keep the two in sync.
        public const string ResourcesLoadKey  = "BizSim/GooglePlay/AppUpdateSettings";
        public const string AssetDatabasePath = "Assets/Resources/" + ResourcesLoadKey + ".asset";

        public bool LogsEnabled = true;
        public BizSimLogger.LogLevel LogLevel = BizSimLogger.LogLevel.Info;
        public bool UseMockInDevelopmentBuild = false;
        public bool EnableAnalyticsByDefault = false;
        public int InstallStateQueueCapacity = 32;
        public float DefaultTimeoutSeconds = 60f;
        public bool AutoStartInstallStateListener = true;
    }
}
