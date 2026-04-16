using System;

namespace BizSim.Google.Play.AppUpdate
{
    public readonly struct AppUpdatePolicyContext
    {
        public int SessionCount { get; }
        public int LaunchCount { get; }
        public int DaysSinceInstall { get; }
        public AppUpdateInfo LastUpdateInfo { get; }
        public string AppVersion { get; }

        public AppUpdatePolicyContext(
            int sessionCount, int launchCount, int daysSinceInstall,
            AppUpdateInfo lastUpdateInfo, string appVersion)
        {
            SessionCount = sessionCount;
            LaunchCount = launchCount;
            DaysSinceInstall = daysSinceInstall;
            LastUpdateInfo = lastUpdateInfo;
            AppVersion = appVersion ?? "";
        }
    }
}
