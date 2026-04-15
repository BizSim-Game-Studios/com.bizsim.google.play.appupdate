# Basic Integration Sample

Three example scripts demonstrating every flow supported by the Google Play In-App Updates bridge:

- **`FlexibleFlowSample.cs`** — background-download flow. Subscribes to `AppUpdateController.Instance.OnInstallStateChanged`, drives a `Slider` with download progress, and reveals a "Restart" `Button` when the install state reaches `Downloaded`. Wire the button's `OnClick` to `FlexibleFlowSample.OnRestartButtonClicked`.
- **`ImmediateFlowSample.cs`** — full-screen blocking flow. Launches the immediate update activity from `Start()` when `IsImmediateUpdateRequired()` is true, and re-checks on every `OnApplicationFocus(true)` so an interrupted `DeveloperTriggeredUpdateInProgress` is resumed automatically (Google's guide insists on this path).
- **`SmartUpdatePolicySample.cs`** — policy wrapper that uses `AppUpdateInfo.IsImmediateUpdateRequired` / `IsFlexibleUpdateRecommended` classifiers to pick a flow based on priority and client staleness, or stay silent when thresholds are not met.

Pick ONE style per GameObject — do not wire more than one sample to the same `AppUpdateController` or they will fight over the in-flight flow.

## Prerequisite: FragmentActivity

The Google Play Core `:app-update:2.1.0` library calls `appUpdateManager.startUpdateFlowForResult(..., activity, ...)` with an `androidx.fragment.app.FragmentActivity`. Classic Unity's `UnityPlayerActivity` is a plain `Activity` — the bridge's fragment shim refuses to run against it and `StartFlexibleUpdateAsync` / `StartImmediateUpdateAsync` will return `ErrorInternalError`.

Two fixes, pick one:

1. **Unity 6 GameActivity (recommended).** `Player Settings → Android → Other Settings → Application Entry Point → GameActivity`. `GameActivity` extends `FragmentActivity` natively — no override required.
2. **Classic Unity override.** Follow `Documentation~/UNITY_ACTIVITY_OVERRIDE.md` in this package — it ships a full `BizSimAppUpdateActivity.java` that subclasses `FragmentActivity` and the `AndroidManifest.xml` override required to register it as the launcher.

The `AppUpdateConfiguration` editor window shows a green compatibility banner when your project is correctly configured.

## First-run scene setup

**This sample does NOT ship a `.unity` scene file.** Unity's binary scene format makes GUIDs project-specific — shipping one from a package causes diff noise and breaks on re-import. Create a scene in your host project on first use:

1. Import this sample: `Window → Package Manager → BizSim Google Play In-App Updates Bridge → Samples → Basic Integration → Import`. Unity materializes the scripts under `Assets/Samples/com.bizsim.google.play.appupdate/<version>/BasicIntegration/`.
2. `File → New Scene → Basic (Built-in)` → save as `Assets/Samples/com.bizsim.google.play.appupdate/<version>/BasicIntegration/BasicIntegration.unity`.
3. Add a persistent GameObject named `[AppUpdateController]`:
   - Attach the `AppUpdateController` component (`Add Component → Scripts → BizSim.Google.Play.AppUpdate → App Update Controller`).
   - In the Inspector, assign its `Mock Config` field to `Mock_Flexible_Fast` (run `Assets → Create → BizSim → Google Play → AppUpdate → Mock Presets` from the `MockPresets` sample first if you have not already — it creates 8 preset assets in `Assets/BizSim/AppUpdate/MockPresets/`).
4. Add a `Canvas` (`GameObject → UI → Canvas`) with `UI Scale Mode = Scale With Screen Size`.
5. Under the Canvas, add:
   - `Slider` named `DownloadProgressBar` (min `0`, max `1`).
   - `Button` named `RestartButton` with text "Restart to install".
   - Optional: `Text` named `StateLog` for a running log of `InstallState` transitions.
6. Add three empty GameObjects and attach one sample component to each:
   - `[FlexibleSample]` → `FlexibleFlowSample`. In the Inspector, drag `DownloadProgressBar` into `_downloadProgressBar` and `RestartButton` into `_restartButton`.
   - `[ImmediateSample]` → `ImmediateFlowSample`.
   - `[SmartSample]` → `SmartUpdatePolicySample`.
   - Disable two of the three GameObjects — only one flow should drive a given session.
7. Wire `RestartButton.OnClick` → `[FlexibleSample] → FlexibleFlowSample.OnRestartButtonClicked`.
8. Save the scene. Press **Play**. The mock provider drives the configured preset and the log messages appear in the Console.

## Integration steps for your own game

1. Install the package via Git URL (see root `README.md` Installation section for the OpenUPM scoped registry + Git URL two-step). Run EDM4U Force Resolve (`Assets → External Dependency Manager → Android Resolver → Force Resolve`).
2. Ensure your Android entry point is a `FragmentActivity` (see Prerequisite above).
3. Drag `AppUpdateController` onto a persistent GameObject in your boot scene — it auto-`DontDestroyOnLoad`s itself.
4. Assign an `AppUpdateMockConfig` asset (from `Samples~/MockPresets` or create your own via `Assets → Create → BizSim → Google Play → AppUpdate → Mock Config`) to the controller's `Mock Config` field. The mock drives Editor play-mode and non-Android builds; on device the real Play Core provider takes over automatically.
5. Pick the flow that fits your game:
   - **Flexible** for non-critical patches where background download is acceptable.
   - **Immediate** for critical / security updates where the user cannot be allowed to continue on the old version.
   - **Smart policy** if you want data-driven selection from `AppUpdateInfo`.
6. Build a signed APK with a new `versionCode`, upload to the Play Console internal test track, and run the 6-step smoke test from the package's `00-scope-and-goals.md` to validate on device.

## Swapping mock presets from the Inspector

With the `AppUpdateController` GameObject selected in the Hierarchy, drag any preset from `Assets/BizSim/AppUpdate/MockPresets/` onto the `Mock Config` field in the Inspector, then press **Play** again. The 8 presets cover no-update, flexible-fast, flexible-slow, flexible-failed-download, immediate-high-priority, immediate-in-progress, play-store-not-found, and sideloaded-install scenarios — see the `MockPresets/README.md` table for details.

## Known limitations

- **Sideloaded builds.** The Play Store returns `ErrorAppNotOwned` on a sideloaded APK — none of the flows work. Deploy via the Play Console internal test track for smoke testing.
- **Play Store unavailable.** Devices without Google Play (Huawei HMS, some Amazon Fire tablets) return `ErrorPlayStoreNotFound`. Gate your update prompts on this error code and fall back to a direct-download flow if you need one.
- **Quota invisibility.** Like In-App Review, Google caps how many times the update flow can be shown per user per day. The cap is opaque — your `StartFlexibleUpdateAsync` call may resolve without the dialog having been shown. Design your UX to tolerate this (no rewards gated on "was the update dialog shown?").
