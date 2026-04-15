using System;

namespace BizSim.Google.Play.AppUpdate
{
    [Serializable]
    public readonly struct InstallState : IEquatable<InstallState>
    {
        public readonly InstallStatus InstallStatus;
        public readonly long BytesDownloaded;
        public readonly long TotalBytesToDownload;
        public readonly InstallErrorCode InstallErrorCode;
        public readonly DateTime ObservedAtUtc;

        public InstallState(
            InstallStatus installStatus,
            long bytesDownloaded,
            long totalBytesToDownload,
            InstallErrorCode installErrorCode,
            DateTime observedAtUtc)
        {
            InstallStatus = installStatus;
            BytesDownloaded = bytesDownloaded;
            TotalBytesToDownload = totalBytesToDownload;
            InstallErrorCode = installErrorCode;
            ObservedAtUtc = observedAtUtc;
        }

        public float DownloadProgress => TotalBytesToDownload == 0 ? 0f : (float)BytesDownloaded / TotalBytesToDownload;

        public bool IsTerminal =>
            InstallStatus == InstallStatus.Installed ||
            InstallStatus == InstallStatus.Failed ||
            InstallStatus == InstallStatus.Canceled;

        public bool Equals(InstallState other)
            => InstallStatus == other.InstallStatus
            && BytesDownloaded == other.BytesDownloaded
            && TotalBytesToDownload == other.TotalBytesToDownload
            && InstallErrorCode == other.InstallErrorCode
            && ObservedAtUtc == other.ObservedAtUtc;

        public override bool Equals(object obj) => obj is InstallState s && Equals(s);

        public override int GetHashCode()
        {
            unchecked
            {
                int h = (int)InstallStatus;
                h = (h * 397) ^ BytesDownloaded.GetHashCode();
                h = (h * 397) ^ TotalBytesToDownload.GetHashCode();
                h = (h * 397) ^ (int)InstallErrorCode;
                h = (h * 397) ^ ObservedAtUtc.GetHashCode();
                return h;
            }
        }

        public static bool operator ==(InstallState a, InstallState b) => a.Equals(b);
        public static bool operator !=(InstallState a, InstallState b) => !a.Equals(b);
    }
}
