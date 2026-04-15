# Mock Presets Sample

Editor-only menu action that materializes 8 `AppUpdateMockConfig` asset scenarios into your host project, covering every major lifecycle branch the Google Play In-App Updates bridge has to handle.

## Usage

1. Import this sample from the Package Manager (`Window → Package Manager → BizSim Google Play In-App Updates Bridge → Samples → Mock Presets → Import`).
2. Run **`Assets → Create → BizSim → Google Play → AppUpdate → Mock Presets`** from the top menu.
3. Eight preset assets appear in `Assets/BizSim/AppUpdate/MockPresets/`.
4. Drag any preset onto `AppUpdateController._mockConfig` in the Inspector to switch scenarios, then press **Play**.

## Preset list

| Name | Availability | Priority | Staleness | AllowFlex | AllowImm | FlexDuration | FailureAt | Error | Purpose |
|---|---|---|---|---|---|---|---|---|---|
| `Mock_NoUpdate` | UpdateNotAvailable | 0 | — | false | false | — | — | — | Happy-path "already on latest" branch |
| `Mock_Flexible_Fast` | UpdateAvailable | 2 | 3 | true | true | 2s | — | — | Quick flexible flow — validate download progress UI + restart button reveal |
| `Mock_Flexible_Slow` | UpdateAvailable | 2 | 15 | true | true | 8s | — | — | Long flexible flow — test slow-network UX and long-lived `InstallStateListener` lifetime |
| `Mock_Flexible_Failed_Download` | UpdateAvailable | 2 | 7 | true | true | 4s | Downloading | ErrorInternalError | Mid-download failure — test error surfacing + retry UX |
| `Mock_Immediate_High_Priority` | UpdateAvailable | 5 | 1 | true | true | — | — | — | Critical update — validate `IsImmediateUpdateRequired(minPriority: 4)` branch |
| `Mock_Immediate_In_Progress` | DeveloperTriggeredUpdateInProgress | 5 | 2 | true | true | — | — | — | Resume-on-focus path — validate the `OnApplicationFocus(true)` re-check in `ImmediateFlowSample` |
| `Mock_Play_Store_Not_Found` | Unknown | 0 | — | false | false | — | — | ErrorPlayStoreNotFound | Device without Google Play (Huawei HMS, some Amazon Fire tablets) |
| `Mock_Sideloaded_Install` | UpdateNotAvailable | 0 | — | false | false | — | — | ErrorAppNotOwned | Sideloaded APK — Play Store refuses the flow |

## Why a script instead of shipped `.asset` files?

`ScriptableObject` asset GUIDs are project-specific and cause diff noise when shipped in a package's `Samples~` folder. A menu-action generator lets each consumer materialize fresh presets in their own project without GUID collisions. The resulting `.asset` files live under `Assets/BizSim/AppUpdate/MockPresets/` and are committed with the consumer's game project, not this package.
