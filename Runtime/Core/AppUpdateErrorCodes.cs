namespace BizSim.Google.Play.AppUpdate
{
    /// <summary>
    /// BizSim-specific extension constants for <see cref="InstallErrorCode"/>. Stay below -200 to
    /// avoid collision with Google's int space.
    /// </summary>
    internal static class AppUpdateErrorCodes
    {
        public const int BridgeNotInitialized = -200;
        public const int Timeout              = -201;
        public const int CancelledByCaller    = -202;
        public const int EditorMockError      = -204;
        public const int FlowLaunchFailed     = -205;
    }
}
