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

        [MenuItem("BizSim/Google Play/App Update/Configuration", false, 100)]
        public static void ShowWindow()
        {
            var w = GetWindow<AppUpdateConfiguration>("BizSim App Update");
            w.minSize = new Vector2(480, 500);
            w.Show();
        }

        private void OnEnable()
        {
            var settings = AppUpdateSettingsAsset.LoadOrCreate();
            _settingsSO = new SerializedObject(settings);
        }

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            DrawHeader();
            EditorGUILayout.Space(8);
            DrawActivityCompatibilityCheck();
            EditorGUILayout.Space(8);
            DrawSettingsSection();
            EditorGUILayout.Space(8);
            DrawFirebaseSection();
            EditorGUILayout.Space(8);
            DrawLinksSection();
            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            EditorGUILayout.LabelField("BizSim Google Play In-App Updates", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Package version: {PackageVersion.Current}");
            EditorGUILayout.LabelField($"Play Core: {PackageVersion.PlayCoreVersion}");
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

            if (_settingsSO == null) OnEnable();
            _settingsSO.Update();

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
                    _settingsSO.ApplyModifiedProperties();
                    AppUpdateSettingsAsset.Save();
                    BizSimLogger.InvalidateCache();
                }
                if (GUILayout.Button("Revert"))
                {
                    _settingsSO.Update();
                }
                if (GUILayout.Button("Reset to defaults"))
                {
                    AppUpdateSettingsAsset.ResetToDefaults();
                    _settingsSO = new SerializedObject(AppUpdateSettingsAsset.LoadOrCreate());
                    BizSimLogger.InvalidateCache();
                }
            }
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
