using System;

namespace BizSim.Google.Play.AppUpdate
{
    [Serializable]
    public readonly struct AppUpdateInfo : IEquatable<AppUpdateInfo>
    {
        public readonly UpdateAvailability UpdateAvailability;
        public readonly int AvailableVersionCode;
        public readonly int UpdatePriority;           // 0-5
        public readonly int? ClientVersionStalenessDays;
        public readonly InstallStatus InstallStatus;
        public readonly long BytesDownloaded;
        public readonly long TotalBytesToDownload;
        public readonly bool IsFlexibleAllowed;
        public readonly bool IsImmediateAllowed;
        public readonly DateTime FetchedAtUtc;

        public AppUpdateInfo(
            UpdateAvailability updateAvailability,
            int availableVersionCode,
            int updatePriority,
            int? clientVersionStalenessDays,
            InstallStatus installStatus,
            long bytesDownloaded,
            long totalBytesToDownload,
            bool isFlexibleAllowed,
            bool isImmediateAllowed,
            DateTime fetchedAtUtc)
        {
            UpdateAvailability = updateAvailability;
            AvailableVersionCode = availableVersionCode;
            UpdatePriority = updatePriority;
            ClientVersionStalenessDays = clientVersionStalenessDays;
            InstallStatus = installStatus;
            BytesDownloaded = bytesDownloaded;
            TotalBytesToDownload = totalBytesToDownload;
            IsFlexibleAllowed = isFlexibleAllowed;
            IsImmediateAllowed = isImmediateAllowed;
            FetchedAtUtc = fetchedAtUtc;
        }

        public bool IsUpdateAvailable => UpdateAvailability == UpdateAvailability.UpdateAvailable;

        public bool IsFlexibleUpdateRecommended(int minPriority = 2, int minStalenessDays = 7) =>
            IsUpdateAvailable && IsFlexibleAllowed &&
            (UpdatePriority >= minPriority ||
             (ClientVersionStalenessDays.HasValue && ClientVersionStalenessDays.Value >= minStalenessDays));

        public bool IsImmediateUpdateRequired(int minPriority = 4) =>
            IsUpdateAvailable && IsImmediateAllowed && UpdatePriority >= minPriority;

        public bool Equals(AppUpdateInfo other)
            => UpdateAvailability == other.UpdateAvailability
            && AvailableVersionCode == other.AvailableVersionCode
            && UpdatePriority == other.UpdatePriority
            && ClientVersionStalenessDays == other.ClientVersionStalenessDays
            && InstallStatus == other.InstallStatus
            && BytesDownloaded == other.BytesDownloaded
            && TotalBytesToDownload == other.TotalBytesToDownload
            && IsFlexibleAllowed == other.IsFlexibleAllowed
            && IsImmediateAllowed == other.IsImmediateAllowed
            && FetchedAtUtc == other.FetchedAtUtc;

        public override bool Equals(object obj) => obj is AppUpdateInfo a && Equals(a);

        public override int GetHashCode()
        {
            unchecked
            {
                int h = (int)UpdateAvailability;
                h = (h * 397) ^ AvailableVersionCode;
                h = (h * 397) ^ UpdatePriority;
                h = (h * 397) ^ ClientVersionStalenessDays.GetHashCode();
                h = (h * 397) ^ (int)InstallStatus;
                h = (h * 397) ^ BytesDownloaded.GetHashCode();
                h = (h * 397) ^ TotalBytesToDownload.GetHashCode();
                h = (h * 397) ^ (IsFlexibleAllowed ? 1 : 0);
                h = (h * 397) ^ (IsImmediateAllowed ? 1 : 0);
                h = (h * 397) ^ FetchedAtUtc.GetHashCode();
                return h;
            }
        }

        public static bool operator ==(AppUpdateInfo a, AppUpdateInfo b) => a.Equals(b);
        public static bool operator !=(AppUpdateInfo a, AppUpdateInfo b) => !a.Equals(b);
    }
}
