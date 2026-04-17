using System.IO;
using NUnit.Framework;

namespace BizSim.Google.Play.AppUpdate.Tests
{
    /// <summary>
    /// C5.2 drift guard (Plan E). For appupdate, expected value is "true" —
    /// Play Core's flexible + immediate update flows render system-managed
    /// Activities that handle predictive-back internally (Play Core 2.1.0+).
    /// </summary>
    public class PredictiveBackManifestTest
    {
        private const string ManifestPath =
            "Packages/com.bizsim.google.play.appupdate/Runtime/Plugins/Android/BizSimAppUpdate.androidlib/AndroidManifest.xml";

        private const string FallbackPath =
            "Runtime/Plugins/Android/BizSimAppUpdate.androidlib/AndroidManifest.xml";

        private static string ReadManifest()
        {
            if (File.Exists(ManifestPath)) return File.ReadAllText(ManifestPath);
            if (File.Exists(FallbackPath)) return File.ReadAllText(FallbackPath);
            Assert.Inconclusive("Manifest not found at " + ManifestPath + " or " + FallbackPath);
            return null;
        }

        [Test]
        public void Manifest_DeclaresPredictiveBackCallback_True()
        {
            var xml = ReadManifest();
            Assert.IsTrue(xml.Contains("enableOnBackInvokedCallback=\"true\""),
                "Per C5.2, appupdate's .androidlib manifest must declare " +
                "android:enableOnBackInvokedCallback=\"true\".");
        }
    }
}
