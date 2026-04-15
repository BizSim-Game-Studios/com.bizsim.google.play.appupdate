using System.IO;
using UnityEditor;
using UnityEngine;

namespace BizSim.Google.Play.AppUpdate.Editor
{
    /// <summary>
    /// Detects whether the host project's Unity Android activity is compatible with this package's
    /// fragment shim. Returns true if ANY of:
    ///   (a) a <c>.java</c> file under <c>Assets/Plugins/Android/</c> contains <c>extends FragmentActivity</c>
    ///       or <c>androidx.fragment.app.FragmentActivity</c> import
    ///   (b) Unity 6's GameActivity is configured as the Android application entry point
    ///       (detected via reflection on <c>PlayerSettings.Android.applicationEntry</c>)
    /// </summary>
    internal static class CompatibilityProbe
    {
        public static bool HasFragmentActivityOrGameActivity()
        {
            // (b) GameActivity check — Unity 6 Player Settings
            // Reflection-based because PlayerSettings.Android.applicationEntry may not
            // exist on older Unity versions. If absent, we skip this check.
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

            // (a) Assets/Plugins/Android/*.java scan
            var androidDir = Path.Combine(Application.dataPath, "Plugins", "Android");
            if (!Directory.Exists(androidDir)) return false;

            var javaFiles = Directory.GetFiles(androidDir, "*.java", SearchOption.AllDirectories);
            foreach (var file in javaFiles)
            {
                string content;
                try { content = File.ReadAllText(file); }
                catch { continue; }

                if (content.Contains("extends FragmentActivity") ||
                    content.Contains("androidx.fragment.app.FragmentActivity"))
                    return true;
            }

            return false;
        }
    }
}
