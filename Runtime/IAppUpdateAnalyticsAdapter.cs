namespace BizSim.Google.Play.AppUpdate
{
    /// <summary>
    /// Optional telemetry adapter. Consumer plugs this into the controller via
    /// <c>SetAnalyticsAdapter</c>. All methods are fired on the Unity main thread.
    /// Implementations MUST NOT throw — the controller wraps calls in try/catch but
    /// a repeated throw pollutes the Unity console with warnings.
    /// </summary>
    public interface IAppUpdateAnalyticsAdapter
    {
        void OnUpdateInfoReceived(AppUpdateInfo info);
        void OnFlexibleFlowStarted();
        void OnImmediateFlowStarted();
        void OnInstallStateChanged(InstallState state);
        void OnCompleteUpdateInvoked();
        void OnError(AppUpdateError error);
    }
}
