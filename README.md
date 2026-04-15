# BizSim Google Play In-App Updates Bridge

[![Unity 6000.0+](https://img.shields.io/badge/Unity-6000.0%2B-black?logo=unity)](https://unity.com/releases/unity-6)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE.md)
[![Version 1.0.0](https://img.shields.io/badge/Version-1.0.0-blue)](CHANGELOG.md)

A Unity bridge for the [Google Play In-App Updates API](https://developer.android.com/guide/playcore/in-app-updates) (`com.google.android.play:app-update:2.1.0`). Supports both **flexible** (background download, user keeps playing) and **immediate** (full-screen blocking) update flows, exposes a continuous install state stream, and ships with a mock provider so you can iterate in the Editor without a Play Store install.

> ⚠️ **Unofficial.** This package is maintained by BizSim Game Studios. It is not an official Google product and is not affiliated with Google.

## Features

- **Java-to-C# bridge** for `com.google.android.play:app-update:2.1.0`
- **Dual-flow architecture** — flexible (background download) + immediate (full-screen blocking)
- **Install state stream** — `OnInstallStateChanged` event plus `ReadInstallStatesAsync(ct)` async iteration
- **`AppUpdateInfo` classifier helpers** — `IsFlexibleUpdateRecommended`, `IsImmediateUpdateRequired`
- **Mock provider** with 8 ScriptableObject presets covering both flows + resume-in-progress
- **Headless fragment shim** for `onActivityResult` interception (requires a `FragmentActivity` host)
- **Optional Firebase Analytics adapter** guarded by `BIZSIM_FIREBASE`
- **Optional UniTask support** guarded by `BIZSIM_UNITASK`
- **Editor integration** via `editor.core` with the `BIZSIM_APPUPDATE_INSTALLED` define auto-registered at editor load

## Installation

This package depends on Google's [External Dependency Manager for Unity (EDM4U)](https://github.com/googlesamples/unity-jar-resolver), which is published to the OpenUPM scoped registry. Add EDM4U's registry to your project's `Packages/manifest.json` once, then add this package as a Git URL — UPM will auto-install EDM4U on first import.

**Step 1 — Add the OpenUPM scoped registry (one-time per project):**

```json
{
  "scopedRegistries": [
    {
      "name": "package.openupm.com",
      "url": "https://package.openupm.com",
      "scopes": [
        "com.google.external-dependency-manager"
      ]
    }
  ]
}
```

If you already have other OpenUPM-distributed packages, you may already have this registry — just add `com.google.external-dependency-manager` to the existing `scopes` array.

**Step 2 — Install this package via Git URL:**

```json
{
  "dependencies": {
    "com.bizsim.google.play.appupdate": "https://github.com/BizSim-Game-Studios/com.bizsim.google.play.appupdate.git#v1.0.0"
  }
}
```

After the package imports, EDM4U is automatically resolved by UPM — no manual `.unitypackage` import required. EDM4U then resolves the Android Maven dependencies declared in `Editor/Dependencies.xml` (`com.google.android.play:app-update:2.1.0` + `androidx.fragment:fragment:1.8.9`) at the next Android build, or immediately via `Assets → External Dependency Manager → Android Resolver → Force Resolve`.

## Known integration steps

**UnityPlayerActivity must extend `FragmentActivity`.** This package uses a headless
fragment shim to intercept `onActivityResult` for the update flow. Classic Unity's
`UnityPlayerActivity` extends `Activity` directly, which will NOT work. Options:

1. **Unity 6 `GameActivity`** (recommended) — already extends `FragmentActivity`. Zero setup.
2. **Classic `UnityPlayerActivity`** — requires a subclass override. See
   [Documentation~/UNITY_ACTIVITY_OVERRIDE.md](Documentation~/UNITY_ACTIVITY_OVERRIDE.md).

The `AppUpdateConfiguration` editor window runs a compatibility probe and shows a red banner if neither option is detected.

## Quick Start

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

## Dual-flow semantics

The package exposes both flows but does **not** auto-choose. The consumer picks via the `AppUpdateInfo` classifier helpers:

- **Flexible** — background download, user keeps playing the game, then is prompted to install at a moment of the consumer's choosing. Good for small, low-priority updates that fix non-blocking bugs or add content.
- **Immediate** — full-screen blocking UI driven by Play Store. The user cannot back out until the install completes (or they uninstall the game). Reserved for critical or security updates.

**Interrupted immediate flows.** If the user backgrounds the app mid-immediate-update, the next launch must re-check via `CheckForUpdateAsync` and re-launch the flow if `UpdateAvailability.DeveloperTriggeredUpdateInProgress` is reported. The `ImmediateFlowSample` shows the recommended `OnApplicationFocus` pattern.

## Requirements

- **Unity 6000.0+**
- **Android** target platform
- **Host Activity must extend `FragmentActivity`.** Unity 6 `GameActivity` satisfies this natively; classic `UnityPlayerActivity` requires a subclass override per `Documentation~/UNITY_ACTIVITY_OVERRIDE.md`.
- **EDM4U** (auto-resolved via OpenUPM scoped registry — see Installation)
- **Google Play In-App Updates library** `2.1.0` and **`androidx.fragment:fragment:1.8.9`** (resolved automatically via `Editor/Dependencies.xml`)

## Google Play Data Safety

**No data collected by this package.** Play Core handles all communication with the Play Store directly; this bridge only relays method calls and result codes. Unlike the Review package's local cooldown cache, AppUpdate has no PlayerPrefs persistence whatsoever — every `CheckForUpdateAsync` call queries Play Store fresh.

When filling out your app's [Play Store Data Safety form](https://support.google.com/googleplay/android-developer/answer/10787469), this package does not require any new declarations. Full input text lives in [`Documentation~/DATA_SAFETY.md`](Documentation~/DATA_SAFETY.md).

## License

Copyright (c) 2026 BizSim Game Studios.

Released under the [MIT License](LICENSE.md).

## Third-Party Licenses

| Library | Version | License |
|---------|---------|---------|
| `com.google.android.play:app-update` | 2.1.0 | Apache 2.0 |
| `androidx.fragment:fragment` | 1.8.9 | Apache 2.0 |

Full attribution text in [NOTICES.md](NOTICES.md).
