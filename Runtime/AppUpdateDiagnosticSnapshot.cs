using System;
using UnityEngine;

namespace BizSim.Google.Play.AppUpdate
{
    /// <summary>
    /// Serializable snapshot of the entire app-update subsystem state at a point in time.
    /// Designed for diagnostics dashboards, support tickets, and debug logging.
    /// </summary>
    [Serializable]
    public sealed class AppUpdateDiagnosticSnapshot
    {
        public int SchemaVersion = 1;
        public string PackageVersion;
        public string Timestamp;
        public int SessionCount;
        public int LaunchCount;
        public int DaysSinceInstall;
        public bool RemoteEnabled;
        public bool CooldownActive;
        public bool OfflineGuardEnabled;
        public bool DryRunMode;
        public string PolicyDecisionRaw;
        public string LastErrorCode;
        public string LastUpdateInfoJson;
        public string LastInstallStateJson;
        public int ImmediatePriorityFloor;
        public int WatchdogTimeoutSeconds;

        public string ToJson() => JsonUtility.ToJson(this, true);

        public static AppUpdateDiagnosticSnapshot FromJson(string json)
            => JsonUtility.FromJson<AppUpdateDiagnosticSnapshot>(json);
    }
}
