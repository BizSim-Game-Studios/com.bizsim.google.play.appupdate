# Data Safety Disclosure — BizSim Google Play In-App Updates Bridge

This document is the source-of-truth for the package's Google Play Data Safety form answers. Consumers must fill out their own Play Console Data Safety form based on their full app's data practices; the entries below cover ONLY what this package adds.

## Data collected

**None.** The package does not collect user-identifying data. The Google Play In-App Updates flow is rendered by the Play Store app (the flexible download progress notification and the full-screen immediate update activity both live in the Play Store process); no update payload, user identifier, or device identifier ever passes through this package's code or JNI bridge.

## Data transmitted to Google

The Google Play Core library (`com.google.android.play:app-update:2.1.0`) handles all communication with the Play Store from its own process. This package's JNI bridge marshals only the opaque `AppUpdateInfo` handle returned by `AppUpdateManager.getAppUpdateInfo()`, the flow-type selector (`FLEXIBLE` / `IMMEDIATE`), and the `InstallState` transitions reported by the install state listener — no user-identifying data crosses the JNI boundary.

## Data transmitted to Firebase (if enabled)

The optional `IAppUpdateAnalyticsAdapter` + its default `FirebaseAppUpdateAnalyticsAdapter` implementation (guarded by the `BIZSIM_FIREBASE` scripting define) log **technical events only**:

- `bizsim_appupdate_check_requested` — fires when `CheckForUpdateAsync()` is called
- `bizsim_appupdate_info_received` — parameters: `availability`, `priority_bucket` (0-5), `staleness_bucket` (never / 0-7d / 8-30d / 30+d / unknown), `allow_flexible` (0/1), `allow_immediate` (0/1)
- `bizsim_appupdate_flexible_started` / `bizsim_appupdate_immediate_started` — parameters: `priority_bucket`
- `bizsim_appupdate_install_state` — parameters: `install_status` (int), `download_progress_bucket` (0 / 0-25 / 25-50 / 50-75 / 75-99 / 100)
- `bizsim_appupdate_flow_completed` — parameters: `flow` (flexible/immediate), `elapsed_ms`
- `bizsim_appupdate_error` — parameters: `code` (int), `retryable` (0/1)

**No user identity, device identifier, ad ID, package name, or `versionCode` is transmitted.** The `code` parameter is the Google Play `InstallErrorCode` int constant, not a user-facing string. Staleness and priority are reported as bucketed values to prevent fingerprinting via long tail values.

Consumers who enable Firebase must complete their own Play Console Data Safety form covering Firebase's data collection — this disclosure covers only what THIS package adds on top.

## Data persisted locally

**NONE.** Unlike the sibling `com.bizsim.google.play.review` package (which stores a single `LastPromptTicks` timestamp for its 90-day cooldown), the AppUpdate package does not persist any state. `AppUpdateInfo` is fetched fresh on every `CheckForUpdateAsync()` call; install state is observed via live listener events and never cached. There are no `PlayerPrefs` keys, no files under `Application.persistentDataPath`, and no `EncryptedPlayerPrefs` entries owned by this package.

The Google Play Store app itself maintains its own cache of the download (that is how the flexible download survives an app restart), but that cache lives in the Play Store's own sandboxed storage and is not accessible to this package.

## User controls

- **No local state to clear.** Because the package persists nothing, there is no `Clear*ForTesting()` escape hatch — there is nothing to clear. To abort an in-flight flexible download, the user can cancel it from the Play Store notification directly.
- **Analytics opt-out:** consumers who want to disable the adapter call `AppUpdateController.Instance.SetAnalyticsAdapter(null)` (or never call `SetAnalyticsAdapter` — the default is no adapter).
- **Full package opt-out:** removing the package from `Packages/manifest.json` leaves no residual state behind.

## Play Console Data Safety form answers

When filling out the [Data Safety form](https://support.google.com/googleplay/android-developer/answer/10787469):

- **Does your app collect or share any of the required user data types?** — Not from this package alone. Answer based on your full app including Firebase/other SDKs.
- **Data types collected by this package:** None.
- **Data shared with third parties by this package:** None (Google Play Core talks to Google Play directly, not through your app).
- **Is the data encrypted in transit?** — N/A (this package doesn't transmit data).
- **Can users request their data be deleted?** — N/A (this package doesn't persist data).

## References

- Package source: <https://github.com/BizSim-Game-Studios/com.bizsim.google.play.appupdate>
- Google In-App Updates API: <https://developer.android.com/guide/playcore/in-app-updates>
- Play Console Data Safety: <https://support.google.com/googleplay/android-developer/answer/10787469>
- CROSS-PACKAGE-INVARIANTS.md §10 (shared template source)
