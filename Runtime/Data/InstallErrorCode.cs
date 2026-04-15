namespace BizSim.Google.Play.AppUpdate
{
    public enum InstallErrorCode
    {
        NoError                 = 0,
        ErrorUnknown            = -2,
        ErrorApiNotAvailable    = -3,
        ErrorInvalidRequest     = -4,
        ErrorInstallUnavailable = -5,
        ErrorInstallNotAllowed  = -6,
        ErrorDownloadNotPresent = -7,
        ErrorInstallInProgress  = -8,
        ErrorPlayStoreNotFound  = -9,
        ErrorAppNotOwned        = -10,
        ErrorInternalError      = -100,

        // BizSim extensions — stay below -200.
        BridgeNotInitialized = -200,
        Timeout              = -201,
        CancelledByCaller    = -202,
        EditorMockError      = -204,
        FlowLaunchFailed     = -205,
    }
}
