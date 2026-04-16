# Troubleshooting

Last reviewed: 2026-04-16

## 1. IllegalStateException: FragmentManager has not been attached to a host

**Problem:** Starting an update flow crashes with a FragmentManager error on Android.

**Cause:** The host Activity does not extend `FragmentActivity`. The headless fragment shim cannot attach.

**Fix:** If using Unity 6 GameActivity, this should not happen (it extends FragmentActivity natively). If using classic `UnityPlayerActivity`, create a subclass that extends `FragmentActivity`. See [UNITY_ACTIVITY_OVERRIDE.md](UNITY_ACTIVITY_OVERRIDE.md).

## 2. Update check returns UpdateNotAvailable on a test device

**Problem:** `CheckForUpdateAsync` always reports `UpdateNotAvailable` even though a newer version exists.

**Cause:** In-app updates only work for apps installed from the Play Store. Sideloaded APKs bypass the update check. The test track version code must be strictly greater than the installed version.

**Fix:** Upload the new version to an internal test track. Install the older version from the Play Store. Ensure the new version has a higher `versionCode`. Wait a few minutes for the Play Store to propagate the update.

## 3. InvalidOperationException: Must be called on the main thread

**Problem:** Calling `AppUpdateController` methods from a background thread throws.

**Cause:** All public methods enforce main-thread execution via `EnsureMainThread()`.

**Fix:** Ensure all calls happen on Unity's main thread. Use `UnityMainThreadDispatcher.Enqueue()` to marshal back if triggered from a callback on a background thread.

## 4. Flexible download completes but install never starts

**Problem:** `OnInstallStateChanged` reports `Downloaded` but the update is never installed.

**Cause:** The consumer must explicitly call `CompleteFlexibleUpdateAsync()` after observing `InstallStatus.Downloaded`. The package does not auto-install.

**Fix:** Subscribe to `OnInstallStateChanged`, check for `InstallStatus.Downloaded`, and call `AppUpdateController.Instance.CompleteFlexibleUpdateAsync()` at an appropriate moment (e.g., after the user confirms or between levels).

## 5. Immediate update restarts but never completes

**Problem:** The user backgrounds the app during an immediate update. On re-launch, the update appears stuck.

**Cause:** When the user backgrounds the app mid-immediate-update, the flow is interrupted. The app must re-check on resume and re-launch the flow.

**Fix:** In `OnApplicationFocus(true)`, call `CheckForUpdateAsync()`. If the result reports `UpdateAvailability.DeveloperTriggeredUpdateInProgress`, call `StartImmediateUpdateAsync()` again. See the `ImmediateFlowSample` in `Samples~/BasicIntegration`.

## 6. EDM4U fails to resolve dependencies

**Problem:** Android build fails with missing dependency errors for `app-update` or `fragment`.

**Cause:** EDM4U has not resolved the Maven dependencies, or the OpenUPM scoped registry is missing.

**Fix:** Run **Assets > External Dependency Manager > Android Resolver > Force Resolve**. Verify that `Packages/manifest.json` contains the OpenUPM scoped registry with `com.google.external-dependency-manager` in its scopes.

## 7. Mock provider does not emit install state transitions

**Problem:** In Editor play mode, `OnInstallStateChanged` never fires.

**Cause:** The mock config may have an empty `SimulatedInstallStates` array.

**Fix:** Assign a mock config with populated install state sequences. Use one of the 8 preset configs from `Samples~/MockPresets`, or create a custom one via **Assets > Create > BizSim > AppUpdate Mock Config**.
