# Architecture

Last reviewed: 2026-04-16

## Overview

The AppUpdate package follows the canonical BizSim Google Play bridge pattern with an
additional fragment shim layer for `onActivityResult` interception. A Java bridge on the
Android side, a C# provider abstraction on the Unity side, and a MonoBehaviour singleton
controller that selects the provider at compile time.

## Component diagram

```
AppUpdateController (MonoBehaviour singleton)
    |
    +-- IAppUpdateInfoProvider (compile-time selection)
    |       |
    |       +-- AndroidAppUpdateInfoProvider (#if UNITY_ANDROID && !UNITY_EDITOR)
    |       |       |
    |       |       +-- UpdateInfoCallbackProxy (AndroidJavaProxy)
    |       |       +-- CompleteUpdateCallbackProxy (AndroidJavaProxy)
    |       |       |       |
    |       |       |       +-- UnityMainThreadDispatcher.Enqueue()
    |       |       |
    |       |       +-- AppUpdateBridge.java (JNI entry point)
    |       |               |
    |       |               +-- AppUpdateManager (Play Core SDK)
    |       |               +-- BizSimAppUpdateFragment (headless fragment shim)
    |       |                       |
    |       |                       +-- onActivityResult() interception
    |       |
    |       +-- MockAppUpdateInfoProvider (Editor + non-Android)
    |               |
    |               +-- AppUpdateMockConfig (ScriptableObject)
    |
    +-- IAppUpdatePolicyEngine (smart policy evaluation)
    |       |
    |       +-- AppUpdatePolicyEngine (default implementation)
    |
    +-- IConsentGate (GDPR consent check)
    |
    +-- IAppUpdateAnalyticsAdapterV2 (optional telemetry)
    |
    +-- AppUpdateInstallSourceDetector (install source verification)
    |
    +-- AppUpdateSessionTracker (session counting)
```

## Thread model

All public methods on `AppUpdateController` enforce main-thread execution via
`EnsureMainThread()`. Calling from a background thread throws `InvalidOperationException`.

On the Android side, `AppUpdateBridge.java` posts all `AppUpdateManager` calls to the main
`Handler` (UI thread). Callbacks from Play Core arrive on the main thread and are forwarded
to C# via callback proxies, which use `UnityMainThreadDispatcher.Enqueue()` to marshal back
to Unity's main thread.

## Fragment shim

The `BizSimAppUpdateFragment` is a headless (no UI) Android Fragment that intercepts
`onActivityResult` from the Play Store update UI. It is dynamically attached to the host
Activity's `FragmentManager` when the first update flow is started.

This design requires the host Activity to extend `FragmentActivity`. Unity 6's `GameActivity`
satisfies this. Classic `UnityPlayerActivity` extends `Activity` directly and must be
subclassed.

## Dual-flow model

- **Flexible flow:** `StartFlexibleUpdateAsync()` initiates a background download. Install
  state changes are emitted via `OnInstallStateChanged`. After `InstallStatus.Downloaded`,
  the consumer must call `CompleteFlexibleUpdateAsync()` to trigger the install.
- **Immediate flow:** `StartImmediateUpdateAsync()` shows a full-screen blocking UI. The
  user cannot back out until the update completes. If the user backgrounds the app
  mid-update, the next `OnApplicationFocus(true)` must re-check and re-launch the flow if
  `DeveloperTriggeredUpdateInProgress` is reported.

## Provider selection

Provider selection happens at compile time:

- `#if UNITY_ANDROID && !UNITY_EDITOR` selects `AndroidAppUpdateInfoProvider`
- All other configurations select `MockAppUpdateInfoProvider`
- In Development Builds, `AppUpdateSettings.UseMockInDevelopmentBuild` can override to mock

## Data flow

1. Consumer calls `AppUpdateController.Instance.CheckForUpdateAsync()`
2. Policy engine evaluates the result; returns a `PolicyDecision` with flow recommendation
3. Consumer starts the appropriate flow
4. Fragment shim attaches to the Activity and intercepts `onActivityResult`
5. Install state transitions are streamed via `OnInstallStateChanged`
6. Analytics adapter is notified at each stage
