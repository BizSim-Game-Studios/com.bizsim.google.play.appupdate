namespace BizSim.Google.Play.AppUpdate
{
    /// <summary>
    /// Extended analytics interface (Wave 2). Adds context-carrying overloads of V1 events
    /// plus 13 new events for policy decisions, preload, cooldown, install-source, and
    /// remind-later flows.
    /// <para>
    /// IL2CPP SAFETY: This is a standalone interface extending <see cref="IAppUpdateAnalyticsAdapter"/>,
    /// NOT default interface methods. Default interface methods are not reliably supported by
    /// Unity's IL2CPP scripting backend.
    /// </para>
    /// <para>
    /// The controller checks <c>if (_analytics is IAppUpdateAnalyticsAdapterV2 v2)</c> at each
    /// decision point. Consumers that implement only <see cref="IAppUpdateAnalyticsAdapter"/>
    /// continue to work unchanged — V2 events simply do not fire.
    /// </para>
    /// </summary>
    public interface IAppUpdateAnalyticsAdapterV2 : IAppUpdateAnalyticsAdapter
    {
        // ---------------------------------------------------------------
        // Context-carrying overloads of V1 events (6 methods)
        // ---------------------------------------------------------------
        void OnUpdateInfoReceived(AppUpdateInfo info, AppUpdateTelemetryContext ctx);
        void OnFlexibleFlowStarted(AppUpdateTelemetryContext ctx);
        void OnImmediateFlowStarted(AppUpdateTelemetryContext ctx);
        void OnInstallStateChanged(InstallState state, AppUpdateTelemetryContext ctx);
        void OnCompleteUpdateInvoked(AppUpdateTelemetryContext ctx);
        void OnError(AppUpdateError error, AppUpdateTelemetryContext ctx);

        // ---------------------------------------------------------------
        // New V2 events (13 methods)
        // ---------------------------------------------------------------
        void OnPolicyEvaluated(PolicyDecision decision, AppUpdateTelemetryContext ctx);
        void OnKillSwitchBlocked(AppUpdateTelemetryContext ctx);
        void OnConsentBlocked(AppUpdateTelemetryContext ctx);
        void OnOfflineBlocked(AppUpdateTelemetryContext ctx);
        void OnFirstRunGraceBlocked(AppUpdateTelemetryContext ctx);
        void OnNonRetryableError(AppUpdateError error, AppUpdateTelemetryContext ctx);
        void OnPreloadStarted(AppUpdateTelemetryContext ctx);
        void OnPreloadSucceeded(AppUpdateTelemetryContext ctx);
        void OnPreloadFailed(AppUpdateError error, AppUpdateTelemetryContext ctx);
        void OnPerVersionCooldownBlocked(AppUpdateTelemetryContext ctx);
        void OnNonPlayInstallBlocked(AppUpdateTelemetryContext ctx);
        void OnRemindLaterStarted(AppUpdateTelemetryContext ctx);
        void OnRemindLaterAutoCompleted(AppUpdateTelemetryContext ctx);
    }
}
