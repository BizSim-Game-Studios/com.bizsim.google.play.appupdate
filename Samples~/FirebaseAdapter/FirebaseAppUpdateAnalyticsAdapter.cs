// ReSharper disable InconsistentNaming
#if BIZSIM_FIREBASE
using Firebase.Analytics;
#endif
using BizSim.Google.Play.AppUpdate;

namespace BizSim.Google.Play.AppUpdate.Samples.FirebaseAdapter
{
    /// <summary>
    /// Reference implementation of <see cref="IAppUpdateAnalyticsAdapterV2"/> that logs every
    /// event to Firebase Analytics. All 25 methods (6 V1 + 6 V1-context + 13 V2) are implemented.
    ///
    /// <para><b>DO NOT RENAME</b> event name constants — they are load-bearing for cross-session
    /// analytics funnels. Renaming a constant breaks every existing dashboard and BigQuery export
    /// that references the old name. If a rename is truly needed, ship a migration window where
    /// BOTH old and new names fire for N releases, then deprecate the old name.</para>
    ///
    /// <para><b>POLICY NOTE (P3):</b> Event names deliberately avoid "update_accepted" or
    /// "update_rejected" phrasing. The Play In-App Updates API is quota-invisible — the controller
    /// cannot distinguish "user saw the dialog and dismissed it" from "Google suppressed the dialog
    /// silently". Using acceptance/rejection language would create a misleading funnel.</para>
    ///
    /// <para>All method bodies are gated with <c>#if BIZSIM_FIREBASE</c>. When Firebase Analytics
    /// is not installed, the class compiles to empty stubs — no runtime cost.</para>
    /// </summary>
    public sealed class FirebaseAppUpdateAnalyticsAdapter : IAppUpdateAnalyticsAdapterV2
    {
        // ---------------------------------------------------------------
        // Event name constants — DO NOT RENAME (see class summary)
        // ---------------------------------------------------------------
        const string EventPrefix = "bizsim_appupdate_";

        const string EventInfoReceived       = EventPrefix + "info_received";
        const string EventFlexibleStarted    = EventPrefix + "flexible_started";
        const string EventImmediateStarted   = EventPrefix + "immediate_started";
        const string EventInstallState       = EventPrefix + "install_state";
        const string EventCompleteInvoked    = EventPrefix + "complete_invoked";
        const string EventError              = EventPrefix + "error";

        const string EventPolicyEvaluated    = EventPrefix + "policy_evaluated";
        const string EventKillSwitchBlocked  = EventPrefix + "killswitch_blocked";
        const string EventConsentBlocked     = EventPrefix + "consent_blocked";
        const string EventOfflineBlocked     = EventPrefix + "offline_blocked";
        const string EventFirstRunBlocked    = EventPrefix + "first_run_grace_blocked";
        const string EventNonRetryableError  = EventPrefix + "non_retryable_error";
        const string EventPreloadStarted     = EventPrefix + "preload_started";
        const string EventPreloadSucceeded   = EventPrefix + "preload_succeeded";
        const string EventPreloadFailed      = EventPrefix + "preload_failed";
        const string EventCooldownBlocked    = EventPrefix + "cooldown_blocked";
        const string EventNonPlayBlocked     = EventPrefix + "non_play_install_blocked";
        const string EventRemindLaterStarted = EventPrefix + "remind_later_started";
        const string EventRemindLaterAuto    = EventPrefix + "remind_later_auto_completed";

        // ---------------------------------------------------------------
        // Parameter name constants
        // ---------------------------------------------------------------
        const string ParamVersionCode   = "version_code";
        const string ParamPriority      = "priority";
        const string ParamStalenessDays = "staleness_days";
        const string ParamStatus        = "status";
        const string ParamErrorCode     = "error_code";
        const string ParamErrorMessage  = "error_message";
        const string ParamRetryable     = "retryable";
        const string ParamDecisionType  = "decision_type";
        const string ParamUpdateType    = "update_type";
        const string ParamReason        = "reason";
        const string ParamTrigger       = "trigger";
        const string ParamSessions      = "sessions";
        const string ParamVariantId     = "variant_id";
        const string ParamAppVersion    = "app_version";

        // ===============================================================
        // V1 methods (6) — IAppUpdateAnalyticsAdapter
        // ===============================================================

        public void OnUpdateInfoReceived(AppUpdateInfo info)
        {
#if BIZSIM_FIREBASE
            FirebaseAnalytics.LogEvent(EventInfoReceived,
                new Parameter(ParamVersionCode, info.AvailableVersionCode),
                new Parameter(ParamPriority, info.UpdatePriority),
                new Parameter(ParamStalenessDays, info.ClientVersionStalenessDays ?? -1));
#endif
        }

        public void OnFlexibleFlowStarted()
        {
#if BIZSIM_FIREBASE
            FirebaseAnalytics.LogEvent(EventFlexibleStarted);
#endif
        }

        public void OnImmediateFlowStarted()
        {
#if BIZSIM_FIREBASE
            FirebaseAnalytics.LogEvent(EventImmediateStarted);
#endif
        }

        public void OnInstallStateChanged(InstallState state)
        {
#if BIZSIM_FIREBASE
            FirebaseAnalytics.LogEvent(EventInstallState,
                new Parameter(ParamStatus, state.InstallStatus.ToString()),
                new Parameter(ParamErrorCode, state.InstallErrorCode.ToString()));
#endif
        }

        public void OnCompleteUpdateInvoked()
        {
#if BIZSIM_FIREBASE
            FirebaseAnalytics.LogEvent(EventCompleteInvoked);
#endif
        }

        public void OnError(AppUpdateError error)
        {
#if BIZSIM_FIREBASE
            FirebaseAnalytics.LogEvent(EventError,
                new Parameter(ParamErrorCode, error.Code.ToString()),
                new Parameter(ParamErrorMessage, Truncate(error.Message, 100)),
                new Parameter(ParamRetryable, error.Retryable ? "true" : "false"));
#endif
        }

        // ===============================================================
        // V1 context overloads (6) — IAppUpdateAnalyticsAdapterV2
        // ===============================================================

        public void OnUpdateInfoReceived(AppUpdateInfo info, AppUpdateTelemetryContext ctx)
        {
#if BIZSIM_FIREBASE
            FirebaseAnalytics.LogEvent(EventInfoReceived,
                new Parameter(ParamVersionCode, info.AvailableVersionCode),
                new Parameter(ParamPriority, info.UpdatePriority),
                new Parameter(ParamStalenessDays, info.ClientVersionStalenessDays ?? -1),
                new Parameter(ParamTrigger, ctx.TriggerReason),
                new Parameter(ParamSessions, ctx.SessionCount),
                new Parameter(ParamAppVersion, ctx.AppVersion));
#endif
        }

        public void OnFlexibleFlowStarted(AppUpdateTelemetryContext ctx)
        {
#if BIZSIM_FIREBASE
            FirebaseAnalytics.LogEvent(EventFlexibleStarted, BuildCtxParams(ctx));
#endif
        }

        public void OnImmediateFlowStarted(AppUpdateTelemetryContext ctx)
        {
#if BIZSIM_FIREBASE
            FirebaseAnalytics.LogEvent(EventImmediateStarted, BuildCtxParams(ctx));
#endif
        }

        public void OnInstallStateChanged(InstallState state, AppUpdateTelemetryContext ctx)
        {
#if BIZSIM_FIREBASE
            FirebaseAnalytics.LogEvent(EventInstallState,
                new Parameter(ParamStatus, state.InstallStatus.ToString()),
                new Parameter(ParamErrorCode, state.InstallErrorCode.ToString()),
                new Parameter(ParamTrigger, ctx.TriggerReason),
                new Parameter(ParamSessions, ctx.SessionCount));
#endif
        }

        public void OnCompleteUpdateInvoked(AppUpdateTelemetryContext ctx)
        {
#if BIZSIM_FIREBASE
            FirebaseAnalytics.LogEvent(EventCompleteInvoked, BuildCtxParams(ctx));
#endif
        }

        public void OnError(AppUpdateError error, AppUpdateTelemetryContext ctx)
        {
#if BIZSIM_FIREBASE
            FirebaseAnalytics.LogEvent(EventError,
                new Parameter(ParamErrorCode, error.Code.ToString()),
                new Parameter(ParamErrorMessage, Truncate(error.Message, 100)),
                new Parameter(ParamRetryable, error.Retryable ? "true" : "false"),
                new Parameter(ParamTrigger, ctx.TriggerReason),
                new Parameter(ParamSessions, ctx.SessionCount));
#endif
        }

        // ===============================================================
        // New V2 events (13) — IAppUpdateAnalyticsAdapterV2
        // ===============================================================

        public void OnPolicyEvaluated(PolicyDecision decision, AppUpdateTelemetryContext ctx)
        {
#if BIZSIM_FIREBASE
            FirebaseAnalytics.LogEvent(EventPolicyEvaluated,
                new Parameter(ParamDecisionType, decision.Type.ToString()),
                new Parameter(ParamUpdateType, decision.UpdateType.ToString()),
                new Parameter(ParamReason, decision.Reason ?? ""),
                new Parameter(ParamTrigger, ctx.TriggerReason),
                new Parameter(ParamSessions, ctx.SessionCount));
#endif
        }

        public void OnKillSwitchBlocked(AppUpdateTelemetryContext ctx)
        {
#if BIZSIM_FIREBASE
            FirebaseAnalytics.LogEvent(EventKillSwitchBlocked, BuildCtxParams(ctx));
#endif
        }

        public void OnConsentBlocked(AppUpdateTelemetryContext ctx)
        {
#if BIZSIM_FIREBASE
            FirebaseAnalytics.LogEvent(EventConsentBlocked, BuildCtxParams(ctx));
#endif
        }

        public void OnOfflineBlocked(AppUpdateTelemetryContext ctx)
        {
#if BIZSIM_FIREBASE
            FirebaseAnalytics.LogEvent(EventOfflineBlocked, BuildCtxParams(ctx));
#endif
        }

        public void OnFirstRunGraceBlocked(AppUpdateTelemetryContext ctx)
        {
#if BIZSIM_FIREBASE
            FirebaseAnalytics.LogEvent(EventFirstRunBlocked, BuildCtxParams(ctx));
#endif
        }

        public void OnNonRetryableError(AppUpdateError error, AppUpdateTelemetryContext ctx)
        {
#if BIZSIM_FIREBASE
            FirebaseAnalytics.LogEvent(EventNonRetryableError,
                new Parameter(ParamErrorCode, error.Code.ToString()),
                new Parameter(ParamErrorMessage, Truncate(error.Message, 100)),
                new Parameter(ParamTrigger, ctx.TriggerReason));
#endif
        }

        public void OnPreloadStarted(AppUpdateTelemetryContext ctx)
        {
#if BIZSIM_FIREBASE
            FirebaseAnalytics.LogEvent(EventPreloadStarted, BuildCtxParams(ctx));
#endif
        }

        public void OnPreloadSucceeded(AppUpdateTelemetryContext ctx)
        {
#if BIZSIM_FIREBASE
            FirebaseAnalytics.LogEvent(EventPreloadSucceeded, BuildCtxParams(ctx));
#endif
        }

        public void OnPreloadFailed(AppUpdateError error, AppUpdateTelemetryContext ctx)
        {
#if BIZSIM_FIREBASE
            FirebaseAnalytics.LogEvent(EventPreloadFailed,
                new Parameter(ParamErrorCode, error.Code.ToString()),
                new Parameter(ParamErrorMessage, Truncate(error.Message, 100)),
                new Parameter(ParamTrigger, ctx.TriggerReason));
#endif
        }

        public void OnPerVersionCooldownBlocked(AppUpdateTelemetryContext ctx)
        {
#if BIZSIM_FIREBASE
            FirebaseAnalytics.LogEvent(EventCooldownBlocked,
                new Parameter(ParamVersionCode, ctx.AvailableVersionCode),
                new Parameter(ParamPriority, ctx.UpdatePriority),
                new Parameter(ParamTrigger, ctx.TriggerReason),
                new Parameter(ParamSessions, ctx.SessionCount));
#endif
        }

        public void OnNonPlayInstallBlocked(AppUpdateTelemetryContext ctx)
        {
#if BIZSIM_FIREBASE
            FirebaseAnalytics.LogEvent(EventNonPlayBlocked, BuildCtxParams(ctx));
#endif
        }

        public void OnRemindLaterStarted(AppUpdateTelemetryContext ctx)
        {
#if BIZSIM_FIREBASE
            FirebaseAnalytics.LogEvent(EventRemindLaterStarted, BuildCtxParams(ctx));
#endif
        }

        public void OnRemindLaterAutoCompleted(AppUpdateTelemetryContext ctx)
        {
#if BIZSIM_FIREBASE
            FirebaseAnalytics.LogEvent(EventRemindLaterAuto, BuildCtxParams(ctx));
#endif
        }

        // ===============================================================
        // Helpers
        // ===============================================================

#if BIZSIM_FIREBASE
        static Parameter[] BuildCtxParams(AppUpdateTelemetryContext ctx)
        {
            return new[]
            {
                new Parameter(ParamAppVersion, ctx.AppVersion),
                new Parameter(ParamVersionCode, ctx.AvailableVersionCode),
                new Parameter(ParamPriority, ctx.UpdatePriority),
                new Parameter(ParamStalenessDays, ctx.StalenessDays),
                new Parameter(ParamTrigger, ctx.TriggerReason),
                new Parameter(ParamSessions, ctx.SessionCount),
                new Parameter(ParamVariantId, ctx.VariantId ?? ""),
            };
        }
#endif

        static string Truncate(string s, int maxLength)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Length <= maxLength ? s : s.Substring(0, maxLength);
        }
    }
}
