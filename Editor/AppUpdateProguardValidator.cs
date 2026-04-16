using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace BizSim.Google.Play.AppUpdate.Editor
{
    /// <summary>
    /// Validates that the consumer's ProGuard/R8 configuration contains the required keep rules for
    /// this package's JNI bridge classes. Scans <c>.pro</c> files under
    /// <c>Assets/Plugins/Android/</c> for the critical keep-rule patterns. Missing rules cause
    /// build-time warnings (advisory only — never fails the build).
    /// </summary>
    internal static class AppUpdateProguardValidator
    {
        /// <summary>
        /// Fully-qualified class names that MUST be kept in ProGuard/R8 to prevent JNI bridge
        /// crashes at runtime. The list mirrors <c>proguard-rules.pro</c> in the
        /// <c>BizSimAppUpdate.androidlib</c> subproject.
        /// </summary>
        internal static readonly string[] RequiredKeepClasses = new[]
        {
            "com.bizsim.google.play.appupdate.AppUpdateBridge",
            "com.bizsim.google.play.appupdate.AppUpdateResultFragment",
            "com.bizsim.google.play.appupdate.InstallStateListenerBridge",
        };

        /// <summary>
        /// The nested callback interfaces inside AppUpdateBridge that are implemented in C# via
        /// AndroidJavaProxy. Without keep rules, R8 strips them and onActivityResult / onStateUpdate
        /// calls crash with NoSuchMethodError.
        /// </summary>
        internal static readonly string[] RequiredKeepNestedInterfaces = new[]
        {
            "com.bizsim.google.play.appupdate.AppUpdateBridge$IUpdateInfoCallback",
            "com.bizsim.google.play.appupdate.AppUpdateBridge$IFlowLaunchCallback",
            "com.bizsim.google.play.appupdate.AppUpdateBridge$IInstallStateCallback",
            "com.bizsim.google.play.appupdate.AppUpdateBridge$ICompleteCallback",
        };

        /// <summary>
        /// Validates that the consumer project's ProGuard configuration keeps the required classes.
        /// Returns a list of human-readable warning messages (empty if all rules are present).
        /// </summary>
        /// <param name="pluginsAndroidPath">
        /// Root path to scan for <c>.pro</c> files. Defaults to <c>Assets/Plugins/Android</c>.
        /// </param>
        public static List<string> Validate(string pluginsAndroidPath = null)
        {
            var warnings = new List<string>();
            pluginsAndroidPath ??= Path.Combine(Application.dataPath, "Plugins", "Android");

            // Collect all .pro file contents. The .androidlib shipped by this package should
            // already contain the rules, but consumer projects may override or strip ProGuard
            // configs. We scan all .pro files to find at least one match per required class.
            var allProContent = CollectProguardContent(pluginsAndroidPath);

            if (string.IsNullOrEmpty(allProContent))
            {
                // No .pro files at all — the .androidlib ships its own, so this is unexpected
                // but not necessarily broken (EDM4U may not have resolved yet).
                warnings.Add(
                    "No .pro files found under " + pluginsAndroidPath + ". " +
                    "This is expected if EDM4U has not resolved yet. After running " +
                    "Android Resolver > Force Resolve, re-check that proguard-rules.pro " +
                    "exists in BizSimAppUpdate.androidlib.");
                return warnings;
            }

            foreach (var cls in RequiredKeepClasses)
            {
                if (!allProContent.Contains(cls))
                {
                    warnings.Add(
                        $"ProGuard keep rule missing for '{cls}'. " +
                        "Without this rule, R8 may strip the JNI bridge class and cause " +
                        "NoClassDefFoundError at runtime. Add '-keep class " + cls + " { *; }' " +
                        "to your ProGuard configuration.");
                }
            }

            // Check for the wildcard nested-class rule: AppUpdateBridge$*
            // This is the shipped pattern. If it's present, all nested interfaces are covered.
            bool hasWildcardNested = allProContent.Contains(
                "com.bizsim.google.play.appupdate.AppUpdateBridge$*");

            if (!hasWildcardNested)
            {
                // Fall back to checking each nested interface individually.
                foreach (var iface in RequiredKeepNestedInterfaces)
                {
                    if (!allProContent.Contains(iface))
                    {
                        warnings.Add(
                            $"ProGuard keep rule missing for nested interface '{iface}'. " +
                            "Without this rule, R8 strips the callback interface and " +
                            "AndroidJavaProxy calls fail with NoSuchMethodError. " +
                            "Add '-keep class " + iface + " { *; }' or use the wildcard " +
                            "'-keep class com.bizsim.google.play.appupdate.AppUpdateBridge$* { *; }'.");
                    }
                }
            }

            return warnings;
        }

        private static string CollectProguardContent(string rootPath)
        {
            if (!Directory.Exists(rootPath)) return null;

            var proFiles = Directory.GetFiles(rootPath, "*.pro", SearchOption.AllDirectories);
            if (proFiles.Length == 0) return null;

            var sb = new System.Text.StringBuilder();
            foreach (var f in proFiles)
            {
                try { sb.Append(File.ReadAllText(f)); }
                catch { /* skip unreadable files */ }
            }
            return sb.ToString();
        }
    }
}
