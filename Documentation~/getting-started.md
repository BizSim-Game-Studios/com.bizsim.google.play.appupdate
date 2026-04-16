# Getting Started

Last reviewed: 2026-04-16

## Prerequisites

- Unity 6000.0 or later
- Android build target selected in Build Settings
- EDM4U (External Dependency Manager for Unity) installed via OpenUPM scoped registry
- Host Activity must extend `FragmentActivity` (Unity 6 GameActivity satisfies this; classic UnityPlayerActivity requires a subclass override)

## Step 1 — Install the package

Add the OpenUPM scoped registry to your project's `Packages/manifest.json` if not already present:

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

Then add the package dependency:

```json
{
  "dependencies": {
    "com.bizsim.google.play.appupdate": "https://github.com/BizSim-Game-Studios/com.bizsim.google.play.appupdate.git#v1.3.0"
  }
}
```

## Step 2 — Resolve Android dependencies

Run **Assets > External Dependency Manager > Android Resolver > Force Resolve**. This pulls `com.google.android.play:app-update:2.1.0` and `androidx.fragment:fragment:1.8.9` from Google Maven.

## Step 3 — Verify FragmentActivity compatibility

Open **BizSim > Google Play > App Update > Configuration**. The compatibility probe section shows whether your host Activity extends `FragmentActivity`. If using classic `UnityPlayerActivity`, see [UNITY_ACTIVITY_OVERRIDE.md](UNITY_ACTIVITY_OVERRIDE.md).

## Step 4 — Check for an update

Add the following to any MonoBehaviour:

```csharp
using BizSim.Google.Play.AppUpdate;
using UnityEngine;

public class UpdateExample : MonoBehaviour
{
    async void Start()
    {
        var info = await AppUpdateController.Instance.CheckForUpdateAsync();
        if (info.UpdateAvailability == UpdateAvailability.UpdateAvailable)
        {
            if (info.IsImmediateUpdateRequired(priorityThreshold: 4))
                await AppUpdateController.Instance.StartImmediateUpdateAsync();
            else if (info.IsFlexibleUpdateRecommended(stalenessDaysThreshold: 7))
                await AppUpdateController.Instance.StartFlexibleUpdateAsync();
        }
    }
}
```

## Step 5 — Verify in Editor

Enter Play Mode. The mock provider simulates an update-available response by default. Check the Console for `[BizSim.AppUpdate]` log entries.

## Step 6 — Test on a device

Deploy to an Android device. In-app updates only work for apps installed from the Play Store. Use internal test tracks with version code differences to trigger real update flows.

## What to expect

- The immediate flow blocks the user with a full-screen Play Store UI until the update completes.
- The flexible flow downloads in the background. You must call `CompleteFlexibleUpdateAsync()` after observing `InstallStatus.Downloaded` to trigger the install.
- If the user backgrounds the app during an immediate update, re-check on `OnApplicationFocus(true)` and re-launch the flow if `DeveloperTriggeredUpdateInProgress` is reported.
