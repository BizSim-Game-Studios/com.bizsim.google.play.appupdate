# API Reference

Last reviewed: 2026-04-16

Namespace: `BizSim.Google.Play.AppUpdate`

## AppUpdateController

MonoBehaviour singleton. Entry point for all update operations.

| Member | Type | Description |
|--------|------|-------------|
| `Instance` | `AppUpdateController` | Lazy singleton; creates a DontDestroyOnLoad GameObject |
| `CheckForUpdateAsync(ct)` | `Task<AppUpdateInfo>` | Queries Play Store for update availability |
| `StartFlexibleUpdateAsync(ct)` | `Task<AppUpdateError>` | Starts a background download |
| `StartImmediateUpdateAsync(ct)` | `Task<AppUpdateError>` | Starts a full-screen blocking update |
| `CompleteFlexibleUpdateAsync(ct)` | `Task<AppUpdateError>` | Triggers install after flexible download completes |
| `OnInstallStateChanged` | `event Action<InstallState>` | Fired on each install state transition |
| `SetAnalyticsAdapter(adapter)` | `void` | Injects an analytics adapter or null to disable |
| `SetConsentGate(gate)` | `void` | Injects a GDPR consent gate |
| `SetPolicyEngine(engine)` | `void` | Injects a custom policy engine |
| `GetDiagnosticSnapshot()` | `AppUpdateDiagnosticSnapshot` | Returns current controller state for debugging |

## AppUpdateInfo

Readonly struct returned by `CheckForUpdateAsync`.

| Property | Type | Description |
|----------|------|-------------|
| `UpdateAvailability` | `UpdateAvailability` | Current availability state |
| `UpdatePriority` | `int` | Developer-set priority (0-5) |
| `ClientVersionStalenessDays` | `int?` | Days since update became available |
| `IsFlexibleUpdateAllowed` | `bool` | Whether flexible flow is supported |
| `IsImmediateUpdateAllowed` | `bool` | Whether immediate flow is supported |
| `IsFlexibleUpdateRecommended(days)` | `bool` | Classifier helper for staleness threshold |
| `IsImmediateUpdateRequired(priority)` | `bool` | Classifier helper for priority threshold |

## InstallState

Readonly struct representing an install state transition.

| Property | Type | Description |
|----------|------|-------------|
| `InstallStatus` | `InstallStatus` | Current install status |
| `BytesDownloaded` | `long` | Bytes downloaded so far |
| `TotalBytesToDownload` | `long` | Total download size |
| `InstallErrorCode` | `InstallErrorCode` | Error code if failed |
| `PackageName` | `string` | Package being updated |

## Enums

| Enum | Key Values |
|------|------------|
| `UpdateAvailability` | `Unknown`, `UpdateNotAvailable`, `UpdateAvailable`, `DeveloperTriggeredUpdateInProgress` |
| `InstallStatus` | `Unknown`, `Pending`, `Downloading`, `Downloaded`, `Installing`, `Installed`, `Failed`, `Canceled` |
| `InstallErrorCode` | `NoError`, `ErrorUnknown`, `ErrorApiNotAvailable`, `ErrorInvalidRequest`, `ErrorInstallUnavailable`, `ErrorInstallNotAllowed`, `ErrorDownloadNotPresent`, `ErrorInternalError` |
| `AppUpdateType` | `Flexible`, `Immediate` |

## IAppUpdatePolicyEngine

Custom policy interface for controlling update behavior.

| Method | Description |
|--------|-------------|
| `Evaluate(info, context)` | Returns `PolicyDecision` with flow type and reason |

## AppUpdateSettings

ScriptableObject at `Assets/Resources/BizSim/GooglePlay/AppUpdateSettings.asset`.

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `LogsEnabled` | `bool` | `true` | Master log switch |
| `LogLevel` | `LogLevel` | `Info` | Minimum log severity |
| `UseMockInDevelopmentBuild` | `bool` | `false` | Use mock provider in Development Builds |
| `EnableAnalyticsByDefault` | `bool` | `false` | Auto-enable analytics adapter |
| `EnableKillSwitch` | `bool` | `false` | Suppress all update checks |
| `ImmediatePriorityThreshold` | `int` | `4` | Minimum priority for immediate flow |
| `FlexibleStalenessDaysThreshold` | `int` | `7` | Minimum staleness for flexible flow |

## AppUpdateMockConfig

ScriptableObject for editor testing with 8 preset scenarios.

| Field | Type | Description |
|-------|------|-------------|
| `SimulatedAvailability` | `UpdateAvailability` | Availability to simulate |
| `SimulatedPriority` | `int` | Priority bucket to simulate |
| `SimulatedStaleness` | `int?` | Staleness days to simulate |
| `SimulatedInstallStates` | `InstallState[]` | Sequence of states to emit |
