using UnityEngine;

namespace BizSim.Google.Play.AppUpdate
{
    /// <summary>
    /// Per-package logger. Reads its config from an <see cref="AppUpdateSettings"/> asset at first call
    /// and caches the result. Falls back to compile-time defaults + a one-shot warning if the asset
    /// is missing.
    /// </summary>
    /// <remarks>
    /// THREAD SAFETY: All public methods are main-thread-only. The underlying <c>Resources.Load</c>
    /// and <c>ScriptableObject.CreateInstance</c> calls are main-thread-only in Unity, and callers
    /// (the controller and friends) marshal through <c>UnityMainThreadDispatcher</c> before invoking
    /// logger methods. Calling <c>BizSimLogger.*</c> from a background thread is undefined behavior.
    /// </remarks>
    public static class BizSimLogger
    {
        // Per-package PREFIX — never changes, never read from asset.
        // Required convention per CROSS-INVARIANTS §12.3: "[BizSim.AppUpdate] " with trailing space.
        public const string Prefix = "[BizSim.AppUpdate] ";

        public enum LogLevel { Verbose = 0, Info = 1, Warning = 2, Error = 3, Silent = 4 }

        private static AppUpdateSettings _cachedSettings;
        private static bool _loggedFallbackWarning;

        private static AppUpdateSettings Settings
        {
            get
            {
                if (_cachedSettings != null) return _cachedSettings;
                _cachedSettings = Resources.Load<AppUpdateSettings>(AppUpdateSettings.ResourcesLoadKey);
                if (_cachedSettings != null) return _cachedSettings;

                // Asset missing. Warn once per session, then fall back to compile-time defaults.
                if (!_loggedFallbackWarning)
                {
                    Debug.LogWarning(Prefix +
                        "Settings asset not found at Resources/BizSim/GooglePlay/AppUpdateSettings.asset — " +
                        "falling back to compile-time defaults. Open BizSim → Google Play → App Update → " +
                        "Configuration to create the asset.");
                    _loggedFallbackWarning = true;
                }
                _cachedSettings = ScriptableObject.CreateInstance<AppUpdateSettings>();
                return _cachedSettings;
            }
        }

        public static void Verbose(string msg) { if (Should(LogLevel.Verbose)) Debug.Log(Prefix + msg); }
        public static void Info   (string msg) { if (Should(LogLevel.Info))    Debug.Log(Prefix + msg); }
        public static void Warning(string msg) { if (Should(LogLevel.Warning)) Debug.LogWarning(Prefix + msg); }
        public static void Error  (string msg) { if (Should(LogLevel.Error))   Debug.LogError(Prefix + msg); }

        // Master-switch (LogsEnabled) is checked FIRST — it overrides LogLevel.
        // When LogsEnabled == false, every call is a no-op regardless of severity.
        private static bool Should(LogLevel level)
        {
            var s = Settings;
            if (!s.LogsEnabled) return false;
            return (int)level >= (int)s.LogLevel;
        }

#if UNITY_EDITOR
        public static void InvalidateCache()
        {
            _cachedSettings = null;
            _loggedFallbackWarning = false;
        }
#endif
    }
}
