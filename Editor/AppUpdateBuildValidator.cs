using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BizSim.Google.Play.AppUpdate.Editor
{
    /// <summary>
    /// Advisory-only pre-build checks for <c>com.bizsim.google.play.appupdate</c>. Fires a
    /// <see cref="Debug.LogWarning"/> for each missing prerequisite and NEVER throws or fails the
    /// build. The four checks (EDM4U present, Settings asset present, minSdk ≥ 21, FragmentActivity
    /// compatibility) are the highest-value early-warning signals for appupdate consumers.
    /// </summary>
    internal sealed class AppUpdateBuildValidator : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.Android) return;

            if (!HasEdm4U())
            {
                Debug.LogWarning(BizSimLogger.Prefix +
                    "EDM4U not detected at build time even though this package declares " +
                    "'com.google.external-dependency-manager:1.2.187' as a UPM dependency. " +
                    "This usually means the host project is missing the OpenUPM scoped registry entry. " +
                    "Add the registry to Packages/manifest.json (see this package's README.md Installation section) " +
                    "and re-import the package, then run Android Resolver → Force Resolve to pull " +
                    "'com.google.android.play:app-update:2.1.0' and 'androidx.fragment:fragment:1.8.9'.");
            }

            // Settings asset existence check — CROSS-INVARIANTS §13 risk #5 mitigation.
            // Note: uses Debug.LogWarning directly rather than BizSimLogger.Warning because we want
            // this build-time message to fire even when the consumer set LogsEnabled = false in the
            // (potentially missing) Settings asset.
            if (AssetDatabase.LoadAssetAtPath<AppUpdateSettings>(AppUpdateSettings.AssetDatabasePath) == null)
            {
                Debug.LogWarning(BizSimLogger.Prefix +
                    "AppUpdateSettings.asset not found at " + AppUpdateSettings.AssetDatabasePath + ". " +
                    "AppUpdateController will fall back to compile-time defaults at runtime " +
                    "(InstallStateQueueCapacity=" + AppUpdateSettings.DefaultInstallStateQueueCapacity +
                    ", DefaultTimeoutSeconds=" + AppUpdateSettings.DefaultTimeoutSecondsFallback +
                    ", AutoStartInstallStateListener=true), and the logger will print a one-shot " +
                    "fallback warning on first call. Open BizSim → Google Play → App Update → " +
                    "Configuration to create the asset.");
            }

            var minSdk = (int)PlayerSettings.Android.minSdkVersion;
            if (minSdk < 21)
            {
                Debug.LogWarning(BizSimLogger.Prefix +
                    $"Player Settings minSdk is {minSdk}; Play Core In-App Updates requires API 21+. " +
                    "Raise Player Settings → Android → Minimum API Level to Android 5.0 (API 21) or higher.");
            }

            // Compatibility probe: the fragment shim requires FragmentActivity host.
            if (!CompatibilityProbe.HasFragmentActivityOrGameActivity())
            {
                Debug.LogWarning(BizSimLogger.Prefix +
                    "No FragmentActivity override or GameActivity detected in " +
                    "Assets/Plugins/Android/. The fragment shim will throw ClassCastException at runtime. " +
                    "See Documentation~/UNITY_ACTIVITY_OVERRIDE.md.");
            }
        }

        private static bool HasEdm4U()
        {
            // Reflect on GooglePlayServices.PlayServicesResolver to avoid a hard dep.
            var type = Type.GetType("GooglePlayServices.PlayServicesResolver, Google.JarResolver");
            return type != null;
        }
    }
}
