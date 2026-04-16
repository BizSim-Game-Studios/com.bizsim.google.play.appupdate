using System;
using UnityEngine;

namespace BizSim.Google.Play.AppUpdate
{
    /// <summary>
    /// Tracks session count, launch count, and install date via PlayerPrefs.
    /// Used by the policy engine to enforce first-run grace periods.
    /// </summary>
    public sealed class AppUpdateSessionTracker
    {
        const string Prefix = "bizsim_appupdate_";
        const string SessionCountKey = Prefix + "session_count";
        const string LaunchCountKey = Prefix + "launch_count";
        const string InstallDateKey = Prefix + "install_date";

        readonly Func<DateTime> _utcNowProvider;

        public AppUpdateSessionTracker() : this(null) { }

        internal AppUpdateSessionTracker(Func<DateTime> utcNowProvider)
        {
            _utcNowProvider = utcNowProvider ?? (() => DateTime.UtcNow);
            EnsureInstallDate();
        }

        public int SessionCount => PlayerPrefs.GetInt(SessionCountKey, 0);
        public int LaunchCount => PlayerPrefs.GetInt(LaunchCountKey, 0);

        public int DaysSinceInstall
        {
            get
            {
                var installTicks = long.Parse(
                    PlayerPrefs.GetString(InstallDateKey, _utcNowProvider().Ticks.ToString()));
                var installDate = new DateTime(installTicks, DateTimeKind.Utc);
                return (int)(_utcNowProvider() - installDate).TotalDays;
            }
        }

        public void RecordSession()
        {
            PlayerPrefs.SetInt(SessionCountKey, SessionCount + 1);
            PlayerPrefs.Save();
        }

        public void RecordLaunch()
        {
            PlayerPrefs.SetInt(LaunchCountKey, LaunchCount + 1);
            PlayerPrefs.Save();
        }

        public bool IsInFirstRunGrace(int minSessions, int minDays)
            => SessionCount < minSessions || DaysSinceInstall < minDays;

        void EnsureInstallDate()
        {
            if (!PlayerPrefs.HasKey(InstallDateKey))
            {
                PlayerPrefs.SetString(InstallDateKey, _utcNowProvider().Ticks.ToString());
                PlayerPrefs.Save();
            }
        }

        /// <summary>
        /// Clears all tracked data. For testing only.
        /// </summary>
        internal void ClearForTesting()
        {
            PlayerPrefs.DeleteKey(SessionCountKey);
            PlayerPrefs.DeleteKey(LaunchCountKey);
            PlayerPrefs.DeleteKey(InstallDateKey);
            PlayerPrefs.Save();
        }
    }
}
