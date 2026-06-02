# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.4.2] - 2026-06-03

### Fixed
- **CS0618 obsolete-symbol warning** in `AppUpdateConfiguration` — the SDK-version label read `PackageVersion.PlayCoreVersion` (an `[Obsolete]` alias); now resolved via a reflection helper preferring the canonical `NativeSdkVersion`, matching the games/review/assetdelivery siblings.
- **Settings-panel edit-discard bug + broken Revert buttons.** `OnGUI` now calls `SerializedObject.Update()` once at frame start and `ApplyModifiedProperties()` once at frame end (per-section `Update()` calls discarded edits mid-frame). Both **Revert** buttons (Logging + Policy Engine sections) now reload the asset from disk (`new SerializedObject(LoadOrCreate())`) instead of re-reading the already-mutated in-memory object, so Revert actually discards unsaved edits (`google-play-bridge-pattern.md` §8).
- **ProGuard validator false-positive.** `AppUpdateProguardValidator` now resolves and scans the package's own shipped `.androidlib` (via `PackageInfo.FindForAssembly`) in addition to the consumer's `Assets/Plugins/Android`. The package's keep rules ship as a Gradle subproject and are never copied into `Assets/Plugins/Android`, so the old consumer-only scan emitted spurious "missing keep rule" warnings or short-circuited on a misleading EDM4U message.

### Changed
- **`AppUpdateSettings` CreateAssetMenu path** unified to `BizSim/Google Play Service/AppUpdate Settings`, matching the games/review/assetdelivery sibling convention. No effect on existing serialized assets.

### Added
- Missing `.meta` files for `PackageVersionSchemaTest` and `PredictiveBackManifestTest`.

## [1.4.1] - 2026-04-17

### Fixed
- **C5.2 compliance (Plan E).** `Runtime/Plugins/Android/BizSimAppUpdate.androidlib/AndroidManifest.xml` now explicitly declares `android:enableOnBackInvokedCallback="true"`. Play Core's flexible + immediate update flows handle predictive-back animations (Android 14+ / API 34+) internally starting with Play Core 2.1.0. Consumer app-level value wins on manifest-merger conflict. Added `PredictiveBackManifestTest` drift guard. See `development-plans/plans/2026-04-17-enterprise-quality-bar/06-conventions/05-predictive-back-audit.md`.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.4.0] - 2026-04-17

### Added
- **K8 PackageVersion schema unification (Plan G).** Three new `public const string` fields on `PackageVersion`: `NativeSdkVersion` (`"2.1.0"`), `NativeSdkLabel` (`"Play Core (app-update)"`), `NativeSdkArtifactCoord` (`"com.google.android.play:app-update:2.1.0"`). See `development-plans/plans/2026-04-17-enterprise-quality-bar/06-conventions/06-package-version-schema.md`.
- `PackageVersionSchemaTest` drift guard.

### Deprecated
- `PackageVersion.PlayCoreVersion` — now an `[Obsolete]` alias of `NativeSdkVersion`. Removed in 2.0.0 per ADR-009.

## [1.3.0] - 2026-04-16

### Added
- ProGuard build-time validator for AppUpdateBridge, AppUpdateResultFragment, InstallStateListenerBridge
- FragmentActivity conditional probe: pass (GameActivity), warn (classic), error (custom non-FragmentActivity)
- Internal `IClock` + `SystemClock` for staleness math testability (S10 security — not public)
- Fragment shim request code tests (0x42F1 flexible, 0x42F2 immediate)
- Policy preset ScriptableObjects: PriorityFiveImmediateOnly, StalenessSevenDayFlexible, HybridSessionStaleness
- `WriteDiagnosticSnapshot(path)` file writer (development builds only, S2 security)
- `Documentation~/UPGRADE-1.x.md` consumer upgrade guide (1.0 → 1.3 path)
- `Documentation~/TELEMETRY-DASHBOARD.md` Firebase dashboard template with 19-event funnel + BigQuery SQL

## [1.2.0] - 2026-04-16

### Added
- `PreloadAppUpdateInfoAsync()` with 15-min TTL cache, invalidates on resume and SetConfigSource
- `IAppUpdateAnalyticsAdapterV2` extended interface (19 new methods: 6 V1-context + 13 new)
- `AppUpdateTelemetryContext` structured context (version, priority, staleness, variant)
- `AppUpdateDownloadProgressAdapter` IProgress<float> debounced UI bridge
- `AppUpdateInstallSourceDetector` with JNI getInstallSourceInfo + session cache
- Per-version cooldown (default 2 days, priority-5 exempt)
- Post-download remind-later with 24h hard cap + auto-complete on pause
- Install-source guard (SkipNonPlayInstalls default ON)
- `FirebaseAppUpdateAnalyticsAdapter` shipped sample (BIZSIM_FIREBASE guarded, 25 methods)
- `BasicIntegrationBootstrap` press-Play scene with flexible/immediate mock paths

## [1.1.0] - 2026-04-16

### Added
- Smart update policy engine consuming priority, staleness, session count, and versionCode via swappable `IAppUpdatePolicyEngine`
- `RecordLaunch()` + first-run grace period (3 sessions / 7 days default)
- `IAppUpdateConfigSource` interface with top-level `RemoteEnabled` kill switch
- `IConsentGate` interface for GDPR / DMA / COPPA region shipments
- `AppUpdateController.GetDiagnosticSnapshot()` for support bundles
- `AppUpdateSessionTracker` with PlayerPrefs-backed session/launch counting
- `DeveloperTriggeredUpdateInProgress` automatic resume on `OnApplicationPause(false)` — fixes stalled immediate updates
- Offline guard, in-flight watchdog (15s default, immediate flow exempt), editor dry-run mode
- `ErrorAppNotOwned` + `ErrorPlayStoreNotFound` dedicated non-retryable telemetry branch
- Editor Configuration tabs: Policy Engine, Remote Config, Consent Gate, Diagnostics
- Build validator warnings for watchdog/timeout mismatch and dry-run in release builds
- Development-build warnings when kill switch or consent gate not wired (S4 security)
- `AppUpdateData_HasNoUserAcceptedField` regression test

## [1.0.1] - 2026-04-15

### Fixed
- Relaxed runtime asmdef `includePlatforms` from `["Android", "Editor"]` to `[]`
  to fix a consumer-side `CS0246: The type or namespace name 'BizSim' could not
  be found` regression that appeared during Addressables content build on Android
  target. The Editor compile pass resolved the auto-reference correctly, but the
  Player script compile pass did not — a known Unity issue when `autoReferenced`
  library assemblies are platform-gated at the asmdef level.

  Runtime platform safety is preserved by the existing `#if UNITY_ANDROID && !UNITY_EDITOR`
  guards around every JNI call site; non-Android builds continue to route through
  `Mock<Api>Provider` per CROSS-PACKAGE-INVARIANTS §4.

  No API surface change. Consumers with existing `using BizSim.Google.Play.AppUpdate;`
  imports require no action — the fix is transparent on the next package install.

## [1.0.0] - 2026-04-15

### Added
- Initial release of the Google Play In-App Updates bridge for Unity.
- `AppUpdateController` singleton with `CheckForUpdateAsync`, `StartFlexibleUpdateAsync`, `StartImmediateUpdateAsync`, `CompleteFlexibleUpdateAsync`.
- Continuous install state stream via `OnInstallStateChanged` event + `ReadInstallStatesAsync(ct)` async iteration.
- C# enums mirroring Google's constants one-to-one (guarded by enum parity tests).
- `AppUpdateInfo` struct with `IsFlexibleUpdateRecommended` / `IsImmediateUpdateRequired` policy classifier helpers.
- Mock provider with 8 ScriptableObject presets.
- Headless fragment shim for activity result handling (requires host Activity extending `FragmentActivity`; Unity 6 `GameActivity` satisfies natively).
- Optional Firebase Analytics adapter guarded by `BIZSIM_FIREBASE`.
- Optional UniTask support guarded by `BIZSIM_UNITASK`.
- `editor.core` integration for Firebase define management.
- `BIZSIM_APPUPDATE_INSTALLED` define auto-registered at editor load.
