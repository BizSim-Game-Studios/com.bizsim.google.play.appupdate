using System.IO;
using UnityEditor;
using UnityEngine;

namespace BizSim.Google.Play.AppUpdate.Editor
{
    /// <summary>
    /// Detects whether the host project's Unity Android activity is compatible with this package's
    /// fragment shim. Returns a three-state result:
    /// <list type="bullet">
    ///   <item><see cref="FragmentActivityState.GameActivity"/> — Unity 6 GameActivity; FragmentActivity-compatible natively.</item>
    ///   <item><see cref="FragmentActivityState.FragmentActivityOverride"/> — custom activity extending FragmentActivity found.</item>
    ///   <item><see cref="FragmentActivityState.ClassicUnityPlayerActivity"/> — no override detected; Unity's default UnityPlayerActivity does NOT extend FragmentActivity.</item>
    ///   <item><see cref="FragmentActivityState.CustomNonFragmentActivity"/> — custom activity found but it does NOT extend FragmentActivity.</item>
    /// </list>
    /// </summary>
    internal static class CompatibilityProbe
    {
        internal enum FragmentActivityState
        {
            /// <summary>Unity 6 GameActivity detected — inherits FragmentActivity natively.</summary>
            GameActivity,

            /// <summary>A custom .java file under Assets/Plugins/Android/ extends FragmentActivity.</summary>
            FragmentActivityOverride,

            /// <summary>No custom activity override detected — Unity's default UnityPlayerActivity (does NOT extend FragmentActivity).</summary>
            ClassicUnityPlayerActivity,

            /// <summary>A custom activity .java exists but does NOT extend FragmentActivity.</summary>
            CustomNonFragmentActivity,
        }

        /// <summary>
        /// Backward-compat API. Returns true when the fragment shim can work (GameActivity or
        /// FragmentActivity override detected).
        /// </summary>
        public static bool HasFragmentActivityOrGameActivity()
        {
            var state = Probe();
            return state == FragmentActivityState.GameActivity ||
                   state == FragmentActivityState.FragmentActivityOverride;
        }

        /// <summary>
        /// Three-state probe for the build validator. Returns the most specific state detected.
        /// </summary>
        public static FragmentActivityState Probe()
        {
            // (1) GameActivity check — Unity 6 Player Settings
            if (IsGameActivityConfigured())
                return FragmentActivityState.GameActivity;

            // (2) Scan Assets/Plugins/Android/ for custom activity .java files.
            var androidDir = Path.Combine(Application.dataPath, "Plugins", "Android");
            if (!Directory.Exists(androidDir))
                return FragmentActivityState.ClassicUnityPlayerActivity;

            var javaFiles = Directory.GetFiles(androidDir, "*.java", SearchOption.AllDirectories);
            bool hasCustomActivity = false;

            foreach (var file in javaFiles)
            {
                string content;
                try { content = File.ReadAllText(file); }
                catch { continue; }

                // Does this file define a class that extends Activity (or a subclass)?
                bool isActivityClass = content.Contains("extends Activity") ||
                                       content.Contains("extends UnityPlayerActivity") ||
                                       content.Contains("extends FragmentActivity") ||
                                       content.Contains("extends AppCompatActivity") ||
                                       content.Contains("extends NativeActivity");

                if (!isActivityClass) continue;

                hasCustomActivity = true;

                // Is it FragmentActivity-compatible?
                if (content.Contains("extends FragmentActivity") ||
                    content.Contains("extends AppCompatActivity") ||
                    content.Contains("androidx.fragment.app.FragmentActivity"))
                    return FragmentActivityState.FragmentActivityOverride;
            }

            return hasCustomActivity
                ? FragmentActivityState.CustomNonFragmentActivity
                : FragmentActivityState.ClassicUnityPlayerActivity;
        }

        private static bool IsGameActivityConfigured()
        {
            try
            {
                var psAndroid = typeof(PlayerSettings.Android);
                var prop = psAndroid.GetProperty("applicationEntry");
                if (prop != null)
                {
                    var val = prop.GetValue(null)?.ToString() ?? "";
                    if (val.Contains("GameActivity")) return true;
                }
            }
            catch { /* Unity version without GameActivity support — fall through */ }

            return false;
        }
    }
}
