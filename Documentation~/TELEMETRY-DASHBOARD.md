# Telemetry Dashboard Guide

This document maps the `bizsim_appupdate_*` Firebase Analytics events to a recommended dashboard funnel. Use it as a template when building your Firebase or BigQuery analytics dashboard.

## Event catalog

All events use the `bizsim_appupdate_` prefix. Event names are frozen -- renaming breaks existing dashboards and BigQuery exports.

### Core funnel events (V1)

| Event | Fired when | Key parameters |
|-------|-----------|----------------|
| `bizsim_appupdate_info_received` | `CheckForUpdateAsync` returns successfully | `version_code`, `priority`, `staleness_days` |
| `bizsim_appupdate_flexible_started` | `StartFlexibleUpdateAsync` begins | (context params) |
| `bizsim_appupdate_immediate_started` | `StartImmediateUpdateAsync` begins | (context params) |
| `bizsim_appupdate_install_state` | Install state changes (Pending, Downloading, Downloaded, etc.) | `status`, `error_code` |
| `bizsim_appupdate_complete_invoked` | `CompleteFlexibleUpdateAsync` called | (context params) |
| `bizsim_appupdate_error` | Any error from the Play Core API | `error_code`, `error_message`, `retryable` |

### Policy and gate events (V2)

| Event | Fired when | Key parameters |
|-------|-----------|----------------|
| `bizsim_appupdate_policy_evaluated` | Policy engine returns a decision | `decision_type`, `update_type`, `reason` |
| `bizsim_appupdate_killswitch_blocked` | Remote config `RemoteEnabled = false` | (context params) |
| `bizsim_appupdate_consent_blocked` | `IConsentGate.IsConsented` returns false | (context params) |
| `bizsim_appupdate_offline_blocked` | Device is offline and `OfflineGuardEnabled = true` | (context params) |
| `bizsim_appupdate_first_run_grace_blocked` | Session/day count below first-run thresholds | (context params) |
| `bizsim_appupdate_non_retryable_error` | `APP_NOT_OWNED` or `PLAY_STORE_NOT_FOUND` | `error_code`, `error_message` |

### Preload and cache events (V2)

| Event | Fired when | Key parameters |
|-------|-----------|----------------|
| `bizsim_appupdate_preload_started` | `PreloadAppUpdateInfoAsync` begins | (context params) |
| `bizsim_appupdate_preload_succeeded` | Preload completes and caches result | (context params) |
| `bizsim_appupdate_preload_failed` | Preload throws | `error_code`, `error_message` |
| `bizsim_appupdate_cooldown_blocked` | Per-version cooldown suppresses prompt | `version_code`, `priority` |
| `bizsim_appupdate_non_play_install_blocked` | `SkipNonPlayInstalls` gate fires | (context params) |

### Remind-later events (V2)

| Event | Fired when | Key parameters |
|-------|-----------|----------------|
| `bizsim_appupdate_remind_later_started` | User defers flexible update completion | (context params) |
| `bizsim_appupdate_remind_later_auto_completed` | Hard cap expires or app pauses with Downloaded state | (context params) |

## Context parameters

V2 events include an `AppUpdateTelemetryContext` with these parameters:

| Parameter | Type | Description |
|-----------|------|-------------|
| `app_version` | string | `Application.version` |
| `version_code` | int | Available update version code |
| `priority` | int | Google's update priority (0-5) |
| `staleness_days` | int | Days the current version has been stale (-1 = unknown) |
| `trigger` | string | Internal trigger reason (e.g., `auto_check`, `preload`) |
| `sessions` | int | Session count from `AppUpdateSessionTracker` |
| `variant_id` | string | A/B test variant (null if not set) |

## Recommended funnel

The primary conversion funnel for monitoring update adoption:

```
bizsim_appupdate_policy_evaluated (Allow)
    |
    v
bizsim_appupdate_preload_started  (optional, if preload is used)
    |
    v
bizsim_appupdate_flexible_started  OR  bizsim_appupdate_immediate_started
    |
    v
bizsim_appupdate_install_state (status = "Downloaded")
    |
    v
bizsim_appupdate_complete_invoked
```

### Drop-off analysis

Monitor these events to understand why users are not updating:

| Stage | Drop-off event | Action |
|-------|---------------|--------|
| Before prompt | `killswitch_blocked` | Check remote config |
| Before prompt | `consent_blocked` | Review CMP integration |
| Before prompt | `offline_blocked` | Expected for offline users |
| Before prompt | `first_run_grace_blocked` | Expected for new installs |
| Before prompt | `cooldown_blocked` | Expected per-version dedup |
| Before prompt | `non_play_install_blocked` | Expected for sideloads |
| During flow | `error` with `retryable = true` | Transient; monitor rate |
| During flow | `non_retryable_error` | `APP_NOT_OWNED` / `PLAY_STORE_NOT_FOUND` |
| After download | `remind_later_started` | User deferred; cap will auto-complete |

## Firebase dashboard setup

### Custom definitions

Register these custom dimensions in Firebase Console > Analytics > Custom Definitions:

| Dimension | Event parameter | Scope |
|-----------|----------------|-------|
| Update Priority | `priority` | Event |
| Update Type | `update_type` | Event |
| Decision Type | `decision_type` | Event |
| Block Reason | `reason` | Event |
| Staleness Days | `staleness_days` | Event |
| Session Count | `sessions` | Event |

### Suggested explorations

1. **Update adoption rate:** Funnel from `policy_evaluated(Allow)` to `complete_invoked`, grouped by `priority`.

2. **Block reason breakdown:** Count of `*_blocked` events grouped by event name, over time.

3. **Error rate:** Count of `error` events grouped by `error_code`, filtered by `retryable`.

4. **Preload hit rate:** Ratio of `preload_succeeded` to `preload_started`.

5. **Remind-later completion:** Ratio of `remind_later_auto_completed` to `remind_later_started`.

## BigQuery export

If using Firebase BigQuery Export, query the `analytics_YYYYMMDD` table:

```sql
SELECT
  event_name,
  COUNT(*) AS event_count,
  COUNTIF(
    event_name = 'bizsim_appupdate_complete_invoked'
  ) / NULLIF(COUNTIF(
    event_name = 'bizsim_appupdate_policy_evaluated'
    AND (SELECT value.string_value FROM UNNEST(event_params)
         WHERE key = 'decision_type') = 'Allow'
  ), 0) AS conversion_rate
FROM `project.analytics_YYYYMMDD.events_*`
WHERE event_name LIKE 'bizsim_appupdate_%'
  AND _TABLE_SUFFIX BETWEEN '20260401' AND '20260430'
GROUP BY event_name
ORDER BY event_count DESC;
```
