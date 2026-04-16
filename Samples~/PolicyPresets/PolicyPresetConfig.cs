using UnityEngine;

namespace BizSim.Google.Play.AppUpdate.Samples.PolicyPresets
{
    /// <summary>
    /// ScriptableObject-backed <see cref="IAppUpdateConfigSource"/> for quick policy engine
    /// configuration. Drop one onto your bootstrap script and call
    /// <c>AppUpdateController.Instance.SetConfigSource(preset)</c> to apply.
    /// </summary>
    [CreateAssetMenu(
        menuName = "BizSim/Google Play/AppUpdate/Policy Preset Config",
        fileName = "PolicyPresetConfig")]
    public sealed class PolicyPresetConfig : ScriptableObject, IAppUpdateConfigSource
    {
        [Header("Kill Switch")]
        [Tooltip("Master switch — false disables all update prompts.")]
        [SerializeField] private bool _remoteEnabled = true;

        [Header("Immediate Flow")]
        [Tooltip("Priority floor for immediate updates (0-5). null = use engine default.")]
        [SerializeField] private int _immediatePriorityFloor = -1;

        [Header("Flexible Flow")]
        [Tooltip("Minimum priority for flexible updates. -1 = null (use engine default).")]
        [SerializeField] private int _flexibleMinPriority = -1;

        [Tooltip("Minimum staleness days before flexible prompt. -1 = null (use engine default).")]
        [SerializeField] private int _flexibleMinStalenessDays = -1;

        [Header("Session / Launch Gates")]
        [Tooltip("Minimum session count before prompting. -1 = null (use engine default).")]
        [SerializeField] private int _minSessionCount = -1;

        [Tooltip("Minimum launch count before prompting. -1 = null (use engine default).")]
        [SerializeField] private int _minLaunchCount = -1;

        [Tooltip("Minimum days since install before prompting. -1 = null (use engine default).")]
        [SerializeField] private int _minDaysSinceInstall = -1;

        [Header("Cooldown")]
        [Tooltip("Per-version cooldown days. -1 = null (use engine default).")]
        [SerializeField] private int _perVersionCooldownDays = -1;

        // IAppUpdateConfigSource — -1 encodes null (nullable int from inspector).
        public bool RemoteEnabled => _remoteEnabled;
        public int? ImmediatePriorityFloor => _immediatePriorityFloor >= 0 ? _immediatePriorityFloor : null;
        public int? FlexibleMinPriority => _flexibleMinPriority >= 0 ? _flexibleMinPriority : null;
        public int? FlexibleMinStalenessDays => _flexibleMinStalenessDays >= 0 ? _flexibleMinStalenessDays : null;
        public int? MinSessionCount => _minSessionCount >= 0 ? _minSessionCount : null;
        public int? MinLaunchCount => _minLaunchCount >= 0 ? _minLaunchCount : null;
        public int? MinDaysSinceInstall => _minDaysSinceInstall >= 0 ? _minDaysSinceInstall : null;
        public int? PerVersionCooldownDays => _perVersionCooldownDays >= 0 ? _perVersionCooldownDays : null;
    }
}
