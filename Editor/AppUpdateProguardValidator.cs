using System.Collections.Generic;
using System.IO;
using UnityEditor.PackageManager;
using UnityEngine;

namespace BizSim.Google.Play.AppUpdate.Editor
{
    /// <summary>
    /// Validates that the JNI-bridge keep rules required by this package are reachable to
    /// Gradle/R8 at Android build time. Scans <c>.pro</c> files in two locations:
    /// <list type="number">
    ///   <item>The package's own <c>Runtime/Plugins/Android/BizSimAppUpdate.androidlib</c>
    ///         (primary — Unity includes this as a Gradle subproject automatically; the
    ///         <c>consumer-rules.pro</c> declared in <c>build.gradle</c> propagates to the
    ///         host app).</item>
    ///   <item>The consumer's <c>Assets/Plugins/Android/</c> tree (secondary — detects user
    ///         overrides or supplemental rules).</item>
    /// </list>
    /// Missing rules cause build-time warnings (advisory only — never fails the build).
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
        /// Validates that ProGuard configuration reachable to Gradle/R8 keeps the required
        /// classes. Returns a list of human-readable warning messages (empty if all rules
        /// are present). Scans the package's own <c>.androidlib</c> first, then the
        /// consumer's <c>Assets/Plugins/Android</c> tree.
        /// </summary>
        /// <param name="extraScanPath">
        /// Optional extra root path to scan for <c>.pro</c> files (used by tests). When null,
        /// defaults to <c>Assets/Plugins/Android</c> in addition to the package's
        /// <c>.androidlib</c> path resolved at runtime.
        /// </param>
        public static List<string> Validate(string extraScanPath = null)
        {
            var warnings = new List<string>();

            // Primary source: the package's shipped .androidlib. Resolved via the Package
            // Manager so it works whether the package is consumed via Git URL, file: path,
            // or local embedded copy. Unity bundles every *.androidlib under Packages/**/
            // Runtime/Plugins/Android/ as a Gradle subproject at Android build time, and the
            // consumer-rules.pro declared in build.gradle propagates to the host app — no
            // EDM4U resolution required for the package's own keep rules.
            var packageProPath = ResolvePackageAndroidLibPath();

            // Secondary source: the consumer's Assets/Plugins/Android tree. Detects user
            // overrides and supplemental rules.
            var consumerProPath = extraScanPath ?? Path.Combine(Application.dataPath, "Plugins", "Android");

            var allProContent = CollectProguardContent(packageProPath) +
                                CollectProguardContent(consumerProPath);

            if (string.IsNullOrEmpty(allProContent))
            {
                // The package's shipped .androidlib is missing AND the consumer has no
                // ProGuard rules — the package is likely corrupted on disk.
                warnings.Add(
                    "No .pro files found in the package's BizSimAppUpdate.androidlib " +
                    $"(expected at '{packageProPath ?? "<unresolved>"}') nor under " +
                    $"'{consumerProPath}'. The package may be corrupted — reinstall it " +
                    "via the Package Manager and verify that " +
                    "Runtime/Plugins/Android/BizSimAppUpdate.androidlib/proguard-rules.pro " +
                    "is present.");
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
            if (string.IsNullOrEmpty(rootPath) || !Directory.Exists(rootPath)) return string.Empty;

            var proFiles = Directory.GetFiles(rootPath, "*.pro", SearchOption.AllDirectories);
            if (proFiles.Length == 0) return string.Empty;

            var sb = new System.Text.StringBuilder();
            foreach (var f in proFiles)
            {
                try { sb.Append(File.ReadAllText(f)); sb.Append('\n'); }
                catch { /* skip unreadable files */ }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Resolves the on-disk path to this package's shipped <c>.androidlib</c> directory.
        /// Uses <see cref="PackageInfo.FindForAssembly"/> so the lookup works for Git URL,
        /// file: path, embedded, and local-tarball install modes. Returns <c>null</c> if the
        /// package metadata cannot be resolved (e.g. running outside Unity in a test runner).
        /// </summary>
        private static string ResolvePackageAndroidLibPath()
        {
            var info = PackageInfo.FindForAssembly(typeof(AppUpdateProguardValidator).Assembly);
            if (info == null || string.IsNullOrEmpty(info.resolvedPath)) return null;

            return Path.Combine(
                info.resolvedPath,
                "Runtime", "Plugins", "Android", "BizSimAppUpdate.androidlib");
        }
    }
}
