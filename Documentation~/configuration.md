# Configuration

Last reviewed: 2026-04-16

## AppUpdateSettings asset

The project-wide defaults are stored in a ScriptableObject at:

```
Assets/Resources/BizSim/GooglePlay/AppUpdateSettings.asset
```

This asset is auto-created by `AppUpdateSettingsAsset.LoadOrCreate()` the first time you open
the Configuration window. The controller reads it at `Awake()` via `Resources.Load`.

### Fields

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `LogsEnabled` | `bool` | `true` | Master switch for all `[BizSim.AppUpdate]` log output |
| `LogLevel` | `LogLevel` | `Info` | Minimum severity: Verbose, Info, Warning, Error, Silent |
| `UseMockInDevelopmentBuild` | `bool` | `false` | When true, Development Builds use the mock provider |
| `EnableAnalyticsByDefault` | `bool` | `false` | Auto-registers the default analytics adapter at startup |
| `EnableKillSwitch` | `bool` | `false` | When true, all update checks are silently suppressed |
| `ImmediatePriorityThreshold` | `int` | `4` | Minimum priority bucket (0-5) for immediate flow |
| `FlexibleStalenessDaysThreshold` | `int` | `7` | Minimum staleness days for flexible flow recommendation |

### Per-instance overrides

`AppUpdateController` has matching `[SerializeField]` fields. When a MonoBehaviour field
has a non-default value, it overrides the asset value for that instance.

## Editor Configuration window

Open via **BizSim > Google Play > App Update > Configuration**.

### Sections

1. **Package Info** — displays current package version, Play Core version, and EDM4U status.
2. **Compatibility Probe** — checks whether the host Activity extends `FragmentActivity`.
   Shows a green checkmark for Unity 6 GameActivity or a red banner with a link to
   `UNITY_ACTIVITY_OVERRIDE.md` if the classic `UnityPlayerActivity` is detected.
3. **Settings** — draws the `AppUpdateSettings` asset with full `SerializedObject` editing.
   - **Apply** — saves changes to disk and calls `BizSimLogger.InvalidateCache()`.
   - **Revert** — discards unsaved changes.
   - **Reset to defaults** — restores all fields to their default values.
4. **Policy Engine** — shows the current policy engine configuration and allows testing
   with simulated `AppUpdateInfo` values.
5. **Quick Actions** — buttons for Force Resolve and Open Samples.

### Log level changes

After clicking Apply, log level changes take effect immediately without a domain reload.
The Configuration window calls `BizSimLogger.InvalidateCache()` which clears the cached
settings reference inside `BizSimLogger`.

## Fragment shim requirement

The AppUpdate package uses a headless fragment (`BizSimAppUpdateFragment`) to intercept
`onActivityResult` callbacks from the Play Store update UI. This fragment must be attached
to a `FragmentActivity` host.

Unity 6's `GameActivity` extends `FragmentActivity` natively. Classic `UnityPlayerActivity`
extends `Activity` directly and requires a subclass override. See
[UNITY_ACTIVITY_OVERRIDE.md](UNITY_ACTIVITY_OVERRIDE.md) for instructions.
