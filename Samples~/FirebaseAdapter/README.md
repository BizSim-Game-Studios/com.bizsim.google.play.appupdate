# Firebase Analytics Adapter Sample

Reference implementation of `IAppUpdateAnalyticsAdapterV2` that logs all 25 app-update events to Firebase Analytics.

## Event inventory

| Method | Firebase event name | Category |
|--------|---------------------|----------|
| `OnUpdateInfoReceived(info)` | `bizsim_appupdate_info_received` | V1 |
| `OnFlexibleFlowStarted()` | `bizsim_appupdate_flexible_started` | V1 |
| `OnImmediateFlowStarted()` | `bizsim_appupdate_immediate_started` | V1 |
| `OnInstallStateChanged(state)` | `bizsim_appupdate_install_state` | V1 |
| `OnCompleteUpdateInvoked()` | `bizsim_appupdate_complete_invoked` | V1 |
| `OnError(error)` | `bizsim_appupdate_error` | V1 |
| `OnUpdateInfoReceived(info, ctx)` | `bizsim_appupdate_info_received` | V1+ctx |
| `OnFlexibleFlowStarted(ctx)` | `bizsim_appupdate_flexible_started` | V1+ctx |
| `OnImmediateFlowStarted(ctx)` | `bizsim_appupdate_immediate_started` | V1+ctx |
| `OnInstallStateChanged(state, ctx)` | `bizsim_appupdate_install_state` | V1+ctx |
| `OnCompleteUpdateInvoked(ctx)` | `bizsim_appupdate_complete_invoked` | V1+ctx |
| `OnError(error, ctx)` | `bizsim_appupdate_error` | V1+ctx |
| `OnPolicyEvaluated(decision, ctx)` | `bizsim_appupdate_policy_evaluated` | V2 |
| `OnKillSwitchBlocked(ctx)` | `bizsim_appupdate_killswitch_blocked` | V2 |
| `OnConsentBlocked(ctx)` | `bizsim_appupdate_consent_blocked` | V2 |
| `OnOfflineBlocked(ctx)` | `bizsim_appupdate_offline_blocked` | V2 |
| `OnFirstRunGraceBlocked(ctx)` | `bizsim_appupdate_first_run_grace_blocked` | V2 |
| `OnNonRetryableError(error, ctx)` | `bizsim_appupdate_non_retryable_error` | V2 |
| `OnPreloadStarted(ctx)` | `bizsim_appupdate_preload_started` | V2 |
| `OnPreloadSucceeded(ctx)` | `bizsim_appupdate_preload_succeeded` | V2 |
| `OnPreloadFailed(error, ctx)` | `bizsim_appupdate_preload_failed` | V2 |
| `OnPerVersionCooldownBlocked(ctx)` | `bizsim_appupdate_cooldown_blocked` | V2 |
| `OnNonPlayInstallBlocked(ctx)` | `bizsim_appupdate_non_play_install_blocked` | V2 |
| `OnRemindLaterStarted(ctx)` | `bizsim_appupdate_remind_later_started` | V2 |
| `OnRemindLaterAutoCompleted(ctx)` | `bizsim_appupdate_remind_later_auto_completed` | V2 |

## DO NOT RENAME event constants

Event name constants (the `bizsim_appupdate_*` strings) are **load-bearing for cross-session analytics funnels**. Renaming a constant breaks every existing dashboard, BigQuery export, and Looker Studio report that references the old name. If a rename is truly needed, ship a migration window where BOTH old and new names fire for N releases, then deprecate the old name.

## Policy note (P3)

Event names deliberately avoid "update_accepted" or "update_rejected" phrasing. The Play In-App Updates API is quota-invisible -- the controller cannot distinguish "user saw the dialog and dismissed it" from "Google suppressed the dialog silently". Using acceptance/rejection language would create a misleading funnel.

## Prerequisites

- Firebase Unity SDK installed (specifically `com.google.firebase.analytics`). The asmdef's `versionDefines` auto-sets `BIZSIM_FIREBASE` when the package is present.
- Without Firebase, the class compiles to empty stubs -- zero runtime cost.

## Usage

```csharp
AppUpdateController.Instance.SetAnalyticsAdapter(
    new FirebaseAppUpdateAnalyticsAdapter());
```
