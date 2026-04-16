# Upgrade Guide: com.bizsim.google.play.appupdate 1.x

This guide covers upgrading between minor versions within the 1.x release line. Each section lists the API additions, configuration changes, and migration steps for a specific version jump.

---

## 1.0.0 to 1.0.1

**Type:** Patch (bug fix only)

### What changed

- Runtime asmdef `includePlatforms` relaxed from `["Android", "Editor"]` to `[]` to fix a consumer-side `CS0246` regression during Addressables content builds.

### Migration steps

None. The fix is transparent — update the package reference and rebuild.

---

## 1.0.x to 1.1.0

**Type:** Minor (new features, no breaking changes)

### New APIs

| API | Purpose |
|-----|---------|
| `AppUpdateController.SetPolicyEngine(IAppUpdatePolicyEngine)` | Replace the default policy engine |
| `AppUpdateController.SetConfigSource(IAppUpdateConfigSource)` | Wire a remote-config backend (Firebase RC, etc.) |
| `AppUpdateController.SetConsentGate(IConsentGate)` | Block prompts until GDPR/DMA consent obtained |
| `AppUpdateController.RecordLaunch()` | Feed the first-run grace period |
| `AppUpdateController.RecordSession()` | Feed the session counter |
| `AppUpdateController.GetDiagnosticSnapshot()` | Returns `AppUpdateDiagnosticSnapshot` for support bundles |

### New configuration fields (AppUpdateSettings)

| Field | Default | Notes |
|-------|---------|-------|
| `FirstRunGraceSessions` | 3 | Sessions before first prompt |
| `FirstRunGraceDays` | 7 | Days since install before first prompt |
| `ImmediatePriorityFloor` | 5 | Priority threshold for immediate flow |
| `WatchdogTimeoutSeconds` | 15 | Internal watchdog (immediate flow exempt) |
| `OfflineGuardEnabled` | true | Skip check when offline |
| `DryRunMode` | false | Log decisions without invoking provider (dev builds) |

### Migration steps

1. **Easy path (no overrides):** Update the package reference. The default policy engine, config source, and consent gate are backward-compatible — `CheckForUpdateAsync` works identically to 1.0.x when no overrides are set.

2. **Recommended:** Add `RecordLaunch()` to your splash scene and `RecordSession()` at meaningful session boundaries. This activates the first-run grace period.

3. **Advanced (Firebase wiring):** Implement `IAppUpdateConfigSource` backed by Firebase Remote Config. Call `SetConfigSource(mySource)` early in startup. This enables the kill switch and remote priority overrides.

4. **GDPR/DMA regions:** Implement `IConsentGate` backed by your CMP SDK. Call `SetConsentGate(myGate)` before the first `CheckForUpdateAsync`.

---

## 1.1.x to 1.2.0

**Type:** Minor (new features, no breaking changes)

### New APIs

| API | Purpose |
|-----|---------|
| `AppUpdateController.PreloadAppUpdateInfoAsync(ct)` | Pre-fetch and cache AppUpdateInfo (15-min TTL) |
| `AppUpdateController.InvalidatePreloadCache()` | Force re-fetch on next call |
| `AppUpdateController.CompleteFlexibleUpdateAsync(TimeSpan, ct)` | Remind-later with hard cap |
| `AppUpdateController.StartInstallStateListener()` | Manual listener start (when `AutoStartInstallStateListener = false`) |
| `IAppUpdateAnalyticsAdapterV2` | Extended analytics interface (19 new methods) |
| `AppUpdateTelemetryContext` | Structured context for analytics events |
| `AppUpdateDownloadProgressAdapter` | `IProgress<float>` debounced UI bridge |
| `AppUpdateInstallSourceDetector` | Sideload detection |

### New configuration fields (AppUpdateSettings)

| Field | Default | Notes |
|-------|---------|-------|
| `PreloadCacheTtlMinutes` | 15 | TTL for cached AppUpdateInfo |
| `PerVersionCooldownDays` | 2 | Days before re-prompting same version |
| `PostDownloadRemindLaterMaxHours` | 24 | Hard cap for remind-later timer |
| `SkipNonPlayInstalls` | true | Skip prompts on sideloaded installs |

### Migration steps

1. **Easy path:** Update the package reference. All new features have safe defaults — existing code works without changes.

2. **Preload optimization:** Call `PreloadAppUpdateInfoAsync()` during splash screen loading. The cached result is used by the next `CheckForUpdateAsync` within the TTL window.

3. **Analytics upgrade:** If you implemented `IAppUpdateAnalyticsAdapter`, consider upgrading to `IAppUpdateAnalyticsAdapterV2` for the 13 new event methods and structured `AppUpdateTelemetryContext`. The adapter pattern is backward-compatible — `IAppUpdateAnalyticsAdapterV2 extends IAppUpdateAnalyticsAdapter`.

4. **Remind-later UX:** After the user sees a "Downloaded" notification, offer a "Remind me later" button that calls `CompleteFlexibleUpdateAsync(TimeSpan.FromHours(4))`. The 24-hour hard cap ensures the update installs within a day.

5. **Download progress UI:** Wrap `AppUpdateDownloadProgressAdapter` with a UI slider. It debounces install state updates to configurable intervals.

### Sample: FirebaseAdapter

The new `Samples~/FirebaseAdapter` sample ships a reference `IAppUpdateAnalyticsAdapterV2` implementation that logs all 25 events to Firebase Analytics. Import it via Package Manager > Samples.

---

## General upgrade notes

- **AppUpdateSettings asset:** After upgrading, open **BizSim > Google Play > App Update > Configuration** to see new fields. New fields use safe defaults, but review them to ensure they match your product requirements.

- **EDM4U resolution:** After any upgrade, run **Assets > External Dependency Manager > Android Resolver > Force Resolve** to ensure Maven dependencies are in sync.

- **Build validator warnings:** The build validator checks new conditions on each version. Review build warnings after upgrading — they are advisory but highlight real configuration issues.
