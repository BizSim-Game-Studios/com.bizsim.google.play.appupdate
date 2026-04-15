using UnityEngine;

namespace BizSim.Google.Play.AppUpdate
{
    /// <summary>
    /// Editor/non-Android mock configuration. Ships as ScriptableObject assets in
    /// <c>Samples~/MockPresets</c>. Drop one onto <c>AppUpdateController._mockConfig</c>
    /// (Inspector) to pick a scenario.
    /// </summary>
    [CreateAssetMenu(
        menuName = "BizSim/Google Play/AppUpdate/Mock Config",
        fileName = "AppUpdateMockConfig")]
    public sealed class AppUpdateMockConfig : ScriptableObject
    {
        [Header("Availability")]
        [Tooltip("What the mock reports when CheckForUpdateAsync is called.")]
        public UpdateAvailability SimulatedAvailability = UpdateAvailability.UpdateAvailable;

        [Tooltip("versionCode the mock claims is available.")]
        public int SimulatedAvailableVersionCode = 101;

        [Tooltip("0-5 priority, matches Google's updatePriority field.")]
        [Range(0, 5)]
        public int SimulatedUpdatePriority = 2;

        [Tooltip("Days the current client version has been stale. -1 means null (unknown).")]
        public int SimulatedClientVersionStalenessDays = -1;

        [Header("Allowed flows")]
        public bool AllowFlexible = true;
        public bool AllowImmediate = true;

        [Header("Flow timings")]
        [Tooltip("How long the flexible flow takes from Pending to Downloaded, in seconds.")]
        [Range(0.1f, 60f)]
        public float FlexibleDownloadDurationSeconds = 3f;

        [Tooltip("How long the immediate flow blocks before resolving, in seconds.")]
        [Range(0.1f, 60f)]
        public float ImmediateFlowDurationSeconds = 1.5f;

        [Header("Error simulation")]
        [Tooltip("If non-NoError, the flow fails with this code.")]
        public InstallErrorCode SimulatedErrorCode = InstallErrorCode.NoError;

        [Tooltip("Stop the flexible state machine at this status and fire the error. Unknown = don't fail.")]
        public InstallStatus SimulatedFailureAt = InstallStatus.Unknown;
    }
}
