# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
