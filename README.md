# BizSim Google Play In-App Updates Bridge

[![Unity 6000.0+](https://img.shields.io/badge/Unity-6000.0%2B-blue.svg)](https://unity.com)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE.md)
[![Version](https://img.shields.io/badge/Version-1.4.3-orange.svg)](CHANGELOG.md)

Unity bridge for the [Google Play In-App Updates API](https://developer.android.com/guide/playcore/in-app-updates) (v2.1.0).
Supports both **flexible** (background download, user keeps playing) and **immediate** (full-screen blocking) update flows, exposes a continuous install state stream, and ships with a mock provider so you can iterate in the Editor without a Play Store install.

> **⚠️ Unofficial package.** This is a community-built Unity bridge for the Google Play In-App Updates API. It is **not** an official Google product.

## Features

- **Java-to-C# Bridge** — Play Core `AppUpdateManager` wrapped in a main-thread-safe singleton
- **Dual-flow architecture** — flexible (background download) + immediate (full-screen blocking)
- **Install state stream** — `OnInstallStateChanged` event plus `ReadInstallStatesAsync(ct)` async iteration
- **`AppUpdateInfo` classifier helpers** — `IsFlexibleUpdateRecommended`, `IsImmediateUpdateRequired`
- **Mock provider** — 8 ScriptableObject presets covering both flows + resume-in-progress scenarios for editor + non-Android builds
- **Headless fragment shim** for `onActivityResult` interception (requires a `FragmentActivity` host)
- **Analytics adapter** — Optional `IAppUpdateAnalyticsAdapter` with a Firebase implementation guarded by `BIZSIM_FIREBASE`
- **UniTask support** — Optional extension assembly guarded by `BIZSIM_UNITASK`
- **Editor integration** — Auto-registered `BIZSIM_APPUPDATE_INSTALLED` define via `editor.core`

## Installation

This package depends on Unity's [External Dependency Manager (EDM)](https://docs.unity3d.com/Packages/com.unity.external-dependency-manager@2.1/manual/index.html) (`com.unity.external-dependency-manager`, Unity 2022.3+). It is declared in `package.json`, so the Unity Package Manager installs it from the Unity Registry together with this package; no scoped registry is needed. EDM reads this package's `Editor/Dependencies.xml`.

If the project still uses Google's EDM4U (for example through another SDK), Unity asks which manager stays active (`Assets → External Dependency Manager`). Keep EDM4U installed until every SDK that depends on it has migrated.

**Install this package via Git URL:**

```json
{
  "dependencies": {
    "com.bizsim.google.play.appupdate": "https://github.com/BizSim-Game-Studios/com.bizsim.google.play.appupdate.git#v1.4.3"
  }
}
```

After the package imports, EDM is automatically installed by UPM — no manual `.unitypackage` import required. EDM then resolves the Android Maven dependencies declared in `Editor/Dependencies.xml` (`com.google.android.play:app-update:2.1.0` + `androidx.fragment:fragment:1.8.9`) at the next Android build, or immediately via `Assets → External Dependency Manager → Android Resolver → Force Resolve`.

**FragmentActivity requirement.** This package uses a headless fragment shim to intercept `onActivityResult` for the update flow. Your host Activity must extend `FragmentActivity`. Unity 6 `GameActivity` satisfies this natively (recommended — zero setup). If you are using classic `UnityPlayerActivity`, a subclass override is required; see [Documentation~/UNITY_ACTIVITY_OVERRIDE.md](Documentation~/UNITY_ACTIVITY_OVERRIDE.md). The `AppUpdateConfiguration` editor window runs a compatibility probe and shows a red banner if neither option is detected.

## Quick Start

1. Add `AppUpdateController` to a persistent GameObject (or access the `Instance` singleton).

2. Check for an update and start the appropriate flow:
   ```csharp
   using BizSim.Google.Play.AppUpdate;

   // 1. Check for update
   var info = await AppUpdateController.Instance.CheckForUpdateAsync();
   if (info.UpdateAvailability != UpdateAvailability.UpdateAvailable) return;

   // 2. Decide which flow (consumer policy)
   if (info.IsImmediateUpdateRequired(priorityThreshold: 4))
   {
       await AppUpdateController.Instance.StartImmediateUpdateAsync();
   }
   else if (info.IsFlexibleUpdateRecommended(stalenessDaysThreshold: 7))
   {
       var err = await AppUpdateController.Instance.StartFlexibleUpdateAsync();
       // Subscribe to OnInstallStateChanged to observe download progress.
       // Call CompleteFlexibleUpdateAsync() after observing InstallStatus.Downloaded.
   }
   ```

3. For interrupted immediate flows, re-check `UpdateAvailability.DeveloperTriggeredUpdateInProgress` on the next launch and re-launch the flow. The `ImmediateFlowSample` shows the recommended `OnApplicationFocus` pattern.

## Dual-flow semantics

The package exposes both flows but does **not** auto-choose. The consumer picks via the `AppUpdateInfo` classifier helpers:

- **Flexible** — background download, user keeps playing the game, then is prompted to install at a moment of the consumer's choosing. Good for small, low-priority updates that fix non-blocking bugs or add content.
- **Immediate** — full-screen blocking UI driven by Play Store. The user cannot back out until the install completes (or they uninstall the game). Reserved for critical or security updates.

**Interrupted immediate flows.** If the user backgrounds the app mid-immediate-update, the next launch must re-check via `CheckForUpdateAsync` and re-launch the flow if `UpdateAvailability.DeveloperTriggeredUpdateInProgress` is reported. The `ImmediateFlowSample` shows the recommended `OnApplicationFocus` pattern.

## Requirements

- Unity 6000.0 or later
- Android target platform
- **Host Activity must extend `FragmentActivity`** — Unity 6 `GameActivity` satisfies this natively; classic `UnityPlayerActivity` requires a subclass override per `Documentation~/UNITY_ACTIVITY_OVERRIDE.md`
- **[External Dependency Manager](https://docs.unity3d.com/Packages/com.unity.external-dependency-manager@2.1/manual/index.html)** (`com.unity.external-dependency-manager`) — installed automatically by UPM (see Installation)
- Google Play In-App Updates library 2.1.0 and `androidx.fragment:fragment:1.8.9` (resolved automatically via `Editor/Dependencies.xml`)

## Google Play Data Safety

### Data Collected

This package does **not** collect any user data. Play Core handles all communication with the Play Store directly; this bridge only relays method calls and result codes. Every `CheckForUpdateAsync` call queries the Play Store fresh — `AppUpdateInfo` is never cached. The package does persist a small amount of **local, non-identifying policy state** (session/launch counts, install date, and per-version prompt cooldowns) via `PlayerPrefs` to drive its update-prompt policy engine; this data is device-local and never transmitted. See [`Documentation~/DATA_SAFETY.md`](Documentation~/DATA_SAFETY.md) for the full disclosure.

### Data NOT Collected or Shared

- **No personal data** is collected by this package
- **No data is shared** with third parties
- **No network calls** are made by this package (Play Core handles all IPC to Google Play)

### Play Console Data Safety Form

When filling out the [Data Safety form](https://support.google.com/googleplay/android-developer/answer/10787469) in Google Play Console:

1. **Data types**: None collected by this package
2. **Collection purpose**: N/A
3. **Shared with third parties**: No

Full input text for the Data Safety form lives in [`Documentation~/DATA_SAFETY.md`](Documentation~/DATA_SAFETY.md).

## License

This package's C# and Java source code is licensed under the [MIT License](LICENSE.md) — Copyright (c) 2026 BizSim Game Studios.

## Third-Party Licenses

This package does **not** bundle any Google SDK binaries. The native Android dependencies are resolved at build time by [Unity's External Dependency Manager](https://docs.unity3d.com/Packages/com.unity.external-dependency-manager@2.1/manual/index.html) from the Google Maven repository (`maven.google.com`):

| Dependency | Version | License |
|-----------|---------|---------|
| `com.google.android.play:app-update` | 2.1.0 | [Apache License 2.0](https://www.apache.org/licenses/LICENSE-2.0) |
| `androidx.fragment:fragment` | 1.8.9 | [Apache License 2.0](https://www.apache.org/licenses/LICENSE-2.0) |

For full third-party license details, see [NOTICES.md](NOTICES.md).
