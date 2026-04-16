# Policy Presets Sample

Pre-configured `IAppUpdateConfigSource` ScriptableObjects for common update policies.

## Presets

| Preset | Strategy |
|--------|----------|
| **PriorityFiveImmediateOnly** | Only priority-5 updates trigger; always immediate. No flexible prompt. |
| **StalenessSevenDayFlexible** | Flexible prompt for any update 7+ days stale. |
| **HybridSessionStaleness** | Flexible after 5 sessions AND 14 days since install. |

## Usage

1. Import this sample via Package Manager.
2. Run **Assets > Create > BizSim > Google Play > AppUpdate > Policy Presets**.
3. Drag a preset onto your bootstrap script or call:

```csharp
var preset = Resources.Load<PolicyPresetConfig>("path/to/preset");
AppUpdateController.Instance.SetConfigSource(preset);
```
