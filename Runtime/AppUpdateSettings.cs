using UnityEngine;

namespace BizSim.Google.Play.AppUpdate
{
    /// <summary>
    /// Project-wide defaults for <see cref="AppUpdateController"/>. Edit via
    /// <c>BizSim → Google Play → App Update → Configuration</c>, or create the asset manually
    /// with <c>Assets → Create → BizSim → Google Play → AppUpdate Settings</c>. The controller
    /// loads this asset via <c>Resources.Load</c> at <c>Awake</c> and uses the values as defaults
    /// for fields whose per-instance MonoBehaviour value has not been overridden.
    /// </summary>
    /// <remarks>
    /// HARD CONSTRAINT: field defaults on this class are load-bearing. <see cref="BizSimLogger"/>'s
    /// missing-asset fallback path (<c>ScriptableObject.CreateInstance&lt;AppUpdateSettings&gt;()</c>),
    /// the <c>ResetToDefaults</c> test baseline in <c>AppUpdateSettingsAssetTests</c>, and
    /// <c>AppUpdateController.Awake</c>'s fallback branch all inherit these exact values. Changing
    /// a default silently shifts all three with no compile error. Bump the defaults via the
    /// <c>AppUpdateSettings</c> spec in CROSS-INVARIANTS §12 before editing here.
    /// </remarks>
    [CreateAssetMenu(
        menuName = "BizSim/Google Play Service/AppUpdate Settings",
        fileName = "AppUpdateSettings",
        order = 0)]
    public sealed class AppUpdateSettings : ScriptableObject
    {
        // Path constants per CROSS-INVARIANTS §12.5 — keep the two in sync.
        public const string ResourcesLoadKey  = "BizSim/GooglePlay/AppUpdateSettings";
        public const string AssetDatabasePath = "Assets/Resources/" + ResourcesLoadKey + ".asset";

        // Fallback defaults used by the controller when the Settings asset is missing —
        // single source of truth for the "magic numbers" to keep §12 compliance.
        public const int   DefaultInstallStateQueueCapacity = 32;
        public const float DefaultTimeoutSecondsFallback    = 60f;

        [Header("Logging")]
        [Tooltip("Master switch for the BizSimLogger. When false, every log call is a no-op regardless of LogLevel.")]
        public bool LogsEnabled = true;

        [Tooltip("Minimum severity that BizSimLogger forwards to Debug.Log. Gated by LogsEnabled.")]
        public BizSimLogger.LogLevel LogLevel = BizSimLogger.LogLevel.Info;

        [Header("Development")]
        [Tooltip("If true AND the build is DEVELOPMENT_BUILD, the controller swaps the Android provider for MockAppUpdateProvider. Release builds always use the Android provider.")]
        public bool UseMockInDevelopmentBuild = false;

        [Header("Analytics")]
        [Tooltip("If true, the controller auto-wires a Firebase analytics adapter when BIZSIM_FIREBASE is defined. Consumer can always override with SetAnalyticsAdapter.")]
        public bool EnableAnalyticsByDefault = false;

        [Header("Install state stream")]
        [Tooltip("Bounded queue capacity for the InstallStateStream. Oldest state drops on overflow.")]
        [Range(1, 256)]
        public int InstallStateQueueCapacity = DefaultInstallStateQueueCapacity;

        [Tooltip("Default timeout for CheckForUpdateAsync / StartFlexibleUpdateAsync / StartImmediateUpdateAsync when the caller passes -1f (sentinel). Used only as a floor fallback.")]
        [Range(1f, 600f)]
        public float DefaultTimeoutSeconds = DefaultTimeoutSecondsFallback;

        [Tooltip("If true, Awake auto-starts the install state listener. Set to false to start it manually via StartInstallStateListener.")]
        public bool AutoStartInstallStateListener = true;

        [Header("Policy Engine (Wave 1)")]
        [Tooltip("Minimum sessions before first update prompt")]
        public int FirstRunGraceSessions = 3;

        [Tooltip("Minimum days since install before first update prompt")]
        public int FirstRunGraceDays = 7;

        [Tooltip("Priority floor for immediate updates (0-5). Only priority >= this triggers immediate.")]
        [Range(0, 5)]
        public int ImmediatePriorityFloor = 5;

        [Tooltip("Internal watchdog timeout in seconds (3-120). Does NOT apply to immediate flow user interaction.")]
        [Range(3, 120)]
        public int WatchdogTimeoutSeconds = 15;

        [Tooltip("Skip update check when device is offline")]
        public bool OfflineGuardEnabled = true;

        [Tooltip("Log full policy decision tree without invoking provider (dev builds only)")]
        public bool DryRunMode;

        [Header("Preload (Wave 2)")]
        [Tooltip("TTL for the cached AppUpdateInfo from PreloadAppUpdateInfoAsync, in minutes.")]
        [Range(1, 60)]
        public int PreloadCacheTtlMinutes = DefaultPreloadCacheTtlMinutes;

        [Header("Per-Version Cooldown (Wave 2)")]
        [Tooltip("Days to suppress re-prompt for a given version code after the user has seen the prompt. Priority-5 updates are exempt.")]
        [Range(0, 30)]
        public int PerVersionCooldownDays = DefaultPerVersionCooldownDays;

        [Header("Post-Download Remind Later (Wave 2)")]
        [Tooltip("Maximum hours a flexible update can stay in Downloaded state before auto-completing. 0 disables auto-complete.")]
        [Range(0, 168)]
        public int PostDownloadRemindLaterMaxHours = DefaultPostDownloadRemindLaterMaxHours;

        [Header("Install Source (Wave 2)")]
        [Tooltip("Skip update prompts on sideloaded / non-Play-Store installs. Prevents confusing UX on development builds or alternative stores.")]
        public bool SkipNonPlayInstalls = true;

        // Wave 2 fallback defaults.
        public const int DefaultPreloadCacheTtlMinutes       = 15;
        public const int DefaultPerVersionCooldownDays        = 2;
        public const int DefaultPostDownloadRemindLaterMaxHours = 24;
    }
}
