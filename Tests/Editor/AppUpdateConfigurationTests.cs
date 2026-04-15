using NUnit.Framework;
using UnityEditor;
using BizSim.Google.Play.AppUpdate.Editor;

namespace BizSim.Google.Play.AppUpdate.EditorTests
{
    public class AppUpdateConfigurationTests
    {
        [Test]
        public void AppUpdateConfiguration_OpensWithoutException()
        {
            var window = EditorWindow.GetWindow<AppUpdateConfiguration>();
            Assert.NotNull(window);
            window.Close();
        }

        [Test]
        public void CompatibilityProbe_RunsWithoutException()
        {
            // On a test project without any FragmentActivity override or GameActivity entry,
            // the probe should return false but MUST NOT throw.
            bool result = false;
            Assert.DoesNotThrow(() =>
            {
                result = CompatibilityProbe.HasFragmentActivityOrGameActivity();
            });
            // We don't assert a specific value — the host test project may have GameActivity
            // configured or not. What matters is that the call is safe.
            _ = result;
        }
    }
}
