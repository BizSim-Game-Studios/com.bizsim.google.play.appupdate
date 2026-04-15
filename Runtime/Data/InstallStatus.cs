namespace BizSim.Google.Play.AppUpdate
{
    public enum InstallStatus
    {
        Unknown     = 0,
        Pending     = 1,
        Downloading = 2,
        Installing  = 3,
        Installed   = 4,
        Failed      = 5,
        Canceled    = 6,
        Downloaded  = 11,  // Note: values 7-10 are reserved by Google, do not add.
    }
}
