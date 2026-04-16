using System;
using UnityEngine;

namespace BizSim.Google.Play.AppUpdate
{
    /// <summary>
    /// Detects the installer package name for the current app. On Android, calls
    /// <c>PackageManager.getInstallSourceInfo()</c> (API 30+) with a
    /// <c>getInstallerPackageName()</c> fallback (API &lt; 30). Result is cached
    /// for the session lifetime.
    /// <para>
    /// On non-Android platforms and in the Editor, always returns
    /// <see cref="PlayStoreInstallerPackage"/> to allow flows to proceed.
    /// </para>
    /// </summary>
    public static class AppUpdateInstallSourceDetector
    {
        /// <summary>The Google Play Store installer package name.</summary>
        public const string PlayStoreInstallerPackage = "com.android.vending";

        private static string _cachedInstaller;
        private static bool _cached;

        /// <summary>
        /// Returns the installer package name, or <c>"unknown"</c> if detection fails.
        /// On Editor / non-Android platforms, returns <see cref="PlayStoreInstallerPackage"/>.
        /// Result is cached after first call.
        /// </summary>
        public static string GetInstallerPackageName()
        {
            if (_cached) return _cachedInstaller;

            _cachedInstaller = DetectInstaller();
            _cached = true;

            BizSimLogger.Verbose($"Install source detected: {_cachedInstaller}");
            return _cachedInstaller;
        }

        /// <summary>
        /// Returns <c>true</c> if the app was installed from the Google Play Store.
        /// </summary>
        public static bool IsPlayStoreInstall()
            => string.Equals(GetInstallerPackageName(), PlayStoreInstallerPackage, StringComparison.Ordinal);

        /// <summary>
        /// Clears the cached result. For testing only.
        /// </summary>
        internal static void ClearCacheForTesting()
        {
            _cachedInstaller = null;
            _cached = false;
        }

        static string DetectInstaller()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                using var context = activity.Call<AndroidJavaObject>("getApplicationContext");
                using var pm = context.Call<AndroidJavaObject>("getPackageManager");
                var packageName = context.Call<string>("getPackageName");

                // API 30+ path: getInstallSourceInfo(packageName).getInstallingPackageName()
                if (GetApiLevel() >= 30)
                {
                    try
                    {
                        using var sourceInfo = pm.Call<AndroidJavaObject>("getInstallSourceInfo", packageName);
                        if (sourceInfo != null)
                        {
                            var installer = sourceInfo.Call<string>("getInstallingPackageName");
                            if (!string.IsNullOrEmpty(installer))
                                return installer;
                        }
                    }
                    catch (Exception ex)
                    {
                        BizSimLogger.Verbose($"getInstallSourceInfo failed, falling back: {ex.Message}");
                    }
                }

                // Fallback: getInstallerPackageName (deprecated but available on all API levels)
                var fallback = pm.Call<string>("getInstallerPackageName", packageName);
                return string.IsNullOrEmpty(fallback) ? "unknown" : fallback;
            }
            catch (Exception ex)
            {
                BizSimLogger.Warning($"Install source detection failed: {ex.Message}");
                return "unknown";
            }
#else
            // In Editor and non-Android builds, assume Play Store to allow flows to proceed.
            return PlayStoreInstallerPackage;
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        static int GetApiLevel()
        {
            try
            {
                using var version = new AndroidJavaClass("android.os.Build$VERSION");
                return version.GetStatic<int>("SDK_INT");
            }
            catch
            {
                return 0;
            }
        }
#endif
    }
}
