using System.Reflection;
using UnityEditor;
using UnityEngine;
using BizSim.Google.Play.Editor.Core;

namespace BizSim.Google.Play.AppUpdate.Editor
{
    public sealed class AppUpdateConfiguration : EditorWindow
    {
        private const string REPO_URL = "https://github.com/BizSim-Game-Studios/com.bizsim.google.play.appupdate";
        private const string DOCS_URL = REPO_URL + "/blob/main/Documentation~/DATA_SAFETY.md";
        private const string ACTIVITY_OVERRIDE_URL = REPO_URL + "/blob/main/Documentation~/UNITY_ACTIVITY_OVERRIDE.md";

        private Vector2 _scroll;
        private SerializedObject _settingsSO;
        private bool _policyFoldout = true;
        private bool _remoteConfigFoldout = true;
        private bool _consentFoldout = true;
        private bool _diagnosticsFoldout;

        [MenuItem("BizSim/Google Play/App Update/Configuration", false, 100)]
        public static void ShowWindow()
        {
            var w = GetWindow<AppUpdateConfiguration>("BizSim App Update");
            w.minSize = new Vector2(480, 600);
            w.Show();
        }

        private void OnEnable()
        {
            var settings = AppUpdateSettingsAsset.LoadOrCreate();
            _settingsSO = new SerializedObject(settings);
        }

        private void OnGUI()
        {
            if (_settingsSO == null) OnEnable();
            if (_settingsSO == null) return;

            // Standard Unity SerializedObject pattern: single Update() at frame start,
            // single ApplyModifiedProperties() at frame end. Calling Update() per-section
            // mid-GUI would discard the user's checkbox/slider edits before they propagate
            // to the SerializedObject's backing asset, breaking interactivity entirely.
            _settingsSO.Update();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            DrawHeader();
            EditorGUILayout.Space(8);
            DrawActivityCompatibilityCheck();
            EditorGUILayout.Space(8);
            DrawSettingsSection();
            EditorGUILayout.Space(8);
            DrawPolicyEngineSection();
            EditorGUILayout.Space(8);
            DrawRemoteConfigSection();
            EditorGUILayout.Space(8);
            DrawConsentGateSection();
            EditorGUILayout.Space(8);
            DrawDiagnosticsSection();
            EditorGUILayout.Space(8);
            DrawFirebaseSection();
            EditorGUILayout.Space(8);
            DrawLinksSection();
            EditorGUILayout.EndScrollView();

            // Persist in-memory edits to the asset clone after every frame. Disk save only
            // happens on Apply; until then, edits are kept alive in the SerializedObject's
            // target (the loaded ScriptableObject instance), and Revert can re-read from disk.
            _settingsSO.ApplyModifiedProperties();
        }

        private static string ResolveNativeSdkVersion()
        {
            const BindingFlags F = BindingFlags.Public | BindingFlags.Static;
            var t = typeof(PackageVersion);
            var f = t.GetField("NativeSdkVersion", F)
                 ?? t.GetField("PlayCoreVersion", F);
            return f?.GetRawConstantValue() as string ?? "unknown";
        }

        private void DrawHeader()
        {
            EditorGUILayout.LabelField("BizSim Google Play In-App Updates", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Package version: {PackageVersion.Current}");
            EditorGUILayout.LabelField($"Play Core: {ResolveNativeSdkVersion()}");
            // Hardcoded literal — kept in sync with AAR pin by the workspace `version-drift-check.sh`
            // hook when fragment version is bumped via the google-play-appupdate-bridge skill.
            EditorGUILayout.LabelField("androidx.fragment: 1.8.9 (resolved at build time by EDM4U)");
            EditorGUILayout.LabelField($"Released: {PackageVersion.ReleaseDate}");
        }

        private void DrawActivityCompatibilityCheck()
        {
            EditorGUILayout.LabelField("UnityPlayerActivity Compatibility", EditorStyles.boldLabel);
            bool ok = CompatibilityProbe.HasFragmentActivityOrGameActivity();
            var prev = GUI.color;
            GUI.color = ok ? Color.green : new Color(0.9f, 0.2f, 0.2f);
            EditorGUILayout.LabelField(
                ok ? "\u2713 Host Activity is compatible" : "\u2717 Host Activity may NOT be compatible",
                EditorStyles.boldLabel);
            GUI.color = prev;
            if (!ok)
            {
                EditorGUILayout.HelpBox(
                    "This package requires UnityPlayerActivity to extend FragmentActivity. " +
                    "Either use Unity 6 GameActivity, or subclass UnityPlayerActivity per the override guide.",
                    MessageType.Error);
                if (GUILayout.Button("Open Activity Override Guide"))
                    Application.OpenURL(ACTIVITY_OVERRIDE_URL);
            }
        }

        private void DrawSettingsSection()
        {
            EditorGUILayout.LabelField("Settings (project-wide defaults)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "These values are stored in " + AppUpdateSettings.AssetDatabasePath + " " +
                "and become the controller's defaults at runtime. Per-instance MonoBehaviour fields on " +
                "AppUpdateController still override these for a specific scene.",
                MessageType.Info);

            // _settingsSO already Update()'d once at the top of OnGUI — do NOT call again here.
            EditorGUILayout.PropertyField(_settingsSO.FindProperty("LogsEnabled"));
            EditorGUILayout.PropertyField(_settingsSO.FindProperty("LogLevel"));
            EditorGUILayout.PropertyField(_settingsSO.FindProperty("UseMockInDevelopmentBuild"));
            EditorGUILayout.PropertyField(_settingsSO.FindProperty("EnableAnalyticsByDefault"));
            EditorGUILayout.PropertyField(_settingsSO.FindProperty("InstallStateQueueCapacity"));
            EditorGUILayout.PropertyField(_settingsSO.FindProperty("DefaultTimeoutSeconds"));
            EditorGUILayout.PropertyField(_settingsSO.FindProperty("AutoStartInstallStateListener"));

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Apply"))
                {
                    // Flush pending edits, then write the asset to disk.
                    _settingsSO.ApplyModifiedPropertiesWithoutUndo();
                    AppUpdateSettingsAsset.Save();
                    BizSimLogger.InvalidateCache();
                    _settingsSO.Update();
                }
                if (GUILayout.Button("Revert"))
                {
                    // Discard unsaved in-memory edits by reloading from disk. ApplyModifiedProperties()
                    // runs every frame (mutating the live asset), so Update() alone cannot undo edits —
                    // rebuild the SerializedObject over the disk-loaded asset, mirroring Reset. (§8)
                    _settingsSO = new SerializedObject(AppUpdateSettingsAsset.LoadOrCreate());
                }
                if (GUILayout.Button("Reset to defaults"))
                {
                    AppUpdateSettingsAsset.ResetToDefaults();
                    _settingsSO = new SerializedObject(AppUpdateSettingsAsset.LoadOrCreate());
                    BizSimLogger.InvalidateCache();
                }
            }
        }

        private void DrawPolicyEngineSection()
        {
            _policyFoldout = EditorGUILayout.Foldout(_policyFoldout, "Policy Engine (Wave 1)", true, EditorStyles.foldoutHeader);
            if (!_policyFoldout) return;

            EditorGUI.indentLevel++;
            EditorGUILayout.HelpBox(
                "Controls when and how updates are prompted. The policy engine evaluates session count, " +
                "days since install, update priority, and consent before deciding Flexible vs Immediate flow.",
                MessageType.Info);

            // _settingsSO already Update()'d once at the top of OnGUI — do NOT call again here.
            EditorGUILayout.PropertyField(_settingsSO.FindProperty("ImmediatePriorityFloor"),
                new GUIContent("Immediate Priority Floor", "Priority >= this triggers immediate update (0-5)."));
            EditorGUILayout.PropertyField(_settingsSO.FindProperty("FirstRunGraceSessions"),
                new GUIContent("Grace Period Sessions", "Minimum sessions before first update prompt."));
            EditorGUILayout.PropertyField(_settingsSO.FindProperty("FirstRunGraceDays"),
                new GUIContent("Grace Period Days", "Minimum days since install before first prompt."));
            EditorGUILayout.PropertyField(_settingsSO.FindProperty("WatchdogTimeoutSeconds"),
                new GUIContent("Watchdog Timeout (s)", "Internal timeout for check/flexible flows. Does NOT apply to immediate."));
            EditorGUILayout.PropertyField(_settingsSO.FindProperty("OfflineGuardEnabled"),
                new GUIContent("Offline Guard", "Skip update check when device is offline."));
            EditorGUILayout.PropertyField(_settingsSO.FindProperty("DryRunMode"),
                new GUIContent("Dry Run Mode", "Log policy decisions without invoking provider (dev builds only)."));

            EditorGUILayout.Space(4);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Apply Policy Settings"))
                {
                    _settingsSO.ApplyModifiedPropertiesWithoutUndo();
                    AppUpdateSettingsAsset.Save();
                    _settingsSO.Update();
                }
                if (GUILayout.Button("Revert"))
                {
                    // Reload from disk to discard unsaved edits (see Logging-section Revert).
                    _settingsSO = new SerializedObject(AppUpdateSettingsAsset.LoadOrCreate());
                }
            }
            EditorGUI.indentLevel--;
        }

        private void DrawRemoteConfigSection()
        {
            _remoteConfigFoldout = EditorGUILayout.Foldout(_remoteConfigFoldout, "Remote Config Source", true, EditorStyles.foldoutHeader);
            if (!_remoteConfigFoldout) return;

            EditorGUI.indentLevel++;
            EditorGUILayout.HelpBox(
                "At runtime the controller defaults to StaticAppUpdateConfigSource (always enabled, no remote overrides). " +
                "Call SetConfigSource() from your startup code to wire Firebase Remote Config or a custom backend.",
                MessageType.Info);

            EditorGUILayout.LabelField("Default source:", "StaticAppUpdateConfigSource");
            EditorGUILayout.LabelField("RemoteEnabled:", "true (static default)");

            EditorGUILayout.Space(2);
            EditorGUILayout.HelpBox(
                "In development builds, a warning fires at Awake if no custom IAppUpdateConfigSource is set. " +
                "This means the kill switch is not wired and you cannot disable updates remotely.",
                MessageType.Warning);
            EditorGUI.indentLevel--;
        }

        private void DrawConsentGateSection()
        {
            _consentFoldout = EditorGUILayout.Foldout(_consentFoldout, "Consent Gate", true, EditorStyles.foldoutHeader);
            if (!_consentFoldout) return;

            EditorGUI.indentLevel++;
            EditorGUILayout.HelpBox(
                "At runtime the controller defaults to AlwaysAllowConsentGate (prompts fire without consent check). " +
                "For GDPR/DMA compliance, call SetConsentGate() with your CMP adapter.",
                MessageType.Info);

            EditorGUILayout.LabelField("Default gate:", "AlwaysAllowConsentGate");
            EditorGUILayout.LabelField("IsConsented:", "true (always allows)");

            EditorGUILayout.Space(2);
            EditorGUILayout.HelpBox(
                "In development builds, a warning fires at Awake if no custom IConsentGate is set. " +
                "Wire a consent gate to suppress the warning and ensure GDPR/DMA compliance.",
                MessageType.Warning);
            EditorGUI.indentLevel--;
        }

        private void DrawDiagnosticsSection()
        {
            _diagnosticsFoldout = EditorGUILayout.Foldout(_diagnosticsFoldout, "Diagnostics (runtime snapshot)", true, EditorStyles.foldoutHeader);
            if (!_diagnosticsFoldout) return;

            EditorGUI.indentLevel++;

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "Enter Play Mode to view a live diagnostic snapshot from AppUpdateController.Instance.GetDiagnosticSnapshot().",
                    MessageType.Info);
            }
            else
            {
                var ctrl = AppUpdateController.Instance;
                if (ctrl == null)
                {
                    EditorGUILayout.HelpBox("AppUpdateController.Instance is null.", MessageType.Warning);
                }
                else
                {
                    var snap = ctrl.GetDiagnosticSnapshot();
                    EditorGUILayout.LabelField("Package version:", snap.PackageVersion);
                    EditorGUILayout.LabelField("Timestamp:", snap.Timestamp);
                    EditorGUILayout.LabelField("Session count:", snap.SessionCount.ToString());
                    EditorGUILayout.LabelField("Launch count:", snap.LaunchCount.ToString());
                    EditorGUILayout.LabelField("Days since install:", snap.DaysSinceInstall.ToString());
                    EditorGUILayout.LabelField("Remote enabled:", snap.RemoteEnabled.ToString());
                    EditorGUILayout.LabelField("Offline guard:", snap.OfflineGuardEnabled.ToString());
                    EditorGUILayout.LabelField("Dry run:", snap.DryRunMode.ToString());
                    EditorGUILayout.LabelField("Imm. priority floor:", snap.ImmediatePriorityFloor.ToString());
                    EditorGUILayout.LabelField("Watchdog timeout:", $"{snap.WatchdogTimeoutSeconds}s");
                    EditorGUILayout.LabelField("Last error:", snap.LastErrorCode ?? "(none)");

                    EditorGUILayout.Space(4);
                    if (GUILayout.Button("Copy Snapshot JSON"))
                    {
                        EditorGUIUtility.systemCopyBuffer = snap.ToJson();
                    }
                }
            }
            EditorGUI.indentLevel--;
        }

        private void DrawFirebaseSection()
        {
            EditorGUILayout.LabelField("Firebase Analytics Integration", EditorStyles.boldLabel);
            bool packageInstalled = BizSimDefineManager.IsFirebaseAnalyticsInstalled();
            string version = packageInstalled ? BizSimDefineManager.GetFirebaseAnalyticsVersion() : null;

            using (new EditorGUILayout.HorizontalScope())
            {
                var prev = GUI.color;
                GUI.color = packageInstalled ? Color.green : new Color(1f, 0.5f, 0f);
                EditorGUILayout.LabelField(
                    packageInstalled ? $"\u2713 Installed (v{version})" : "\u2717 Not installed",
                    EditorStyles.boldLabel);
                GUI.color = prev;
            }

            bool definePresent = BizSimDefineManager.IsFirebaseDefinePresentAnywhere();
            EditorGUILayout.LabelField($"BIZSIM_FIREBASE define: {(definePresent ? "active" : "inactive")}");

            string msg = BizSimDefineManager.GetFirebaseStatusMessage(out var msgType);
            EditorGUILayout.HelpBox(msg, msgType);

            using (new EditorGUI.DisabledScope(!packageInstalled || definePresent))
                if (GUILayout.Button("Add BIZSIM_FIREBASE to all platforms"))
                    BizSimDefineManager.AddFirebaseDefineAllPlatforms();
            using (new EditorGUI.DisabledScope(!definePresent))
                if (GUILayout.Button("Remove BIZSIM_FIREBASE from all platforms"))
                    BizSimDefineManager.RemoveFirebaseDefineAllPlatforms();
        }

        private void DrawLinksSection()
        {
            EditorGUILayout.LabelField("Documentation & Support", EditorStyles.boldLabel);
            if (GUILayout.Button("Open GitHub Repository")) Application.OpenURL(REPO_URL);
            if (GUILayout.Button("Open Data Safety Documentation")) Application.OpenURL(DOCS_URL);
            if (GUILayout.Button("Open Activity Override Guide")) Application.OpenURL(ACTIVITY_OVERRIDE_URL);
        }
    }
}
