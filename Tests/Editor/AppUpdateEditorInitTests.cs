using NUnit.Framework;
using BizSim.Google.Play.Editor.Core;

namespace BizSim.Google.Play.AppUpdate.EditorTests
{
    public class AppUpdateEditorInitTests
    {
        [Test]
        public void InitializeOnLoad_RegistersAppUpdateInstalledDefine()
        {
            foreach (var platform in BizSimDefineManager.GetRelevantPlatforms())
            {
                Assert.IsTrue(
                    BizSimDefineManager.IsDefinePresent("BIZSIM_APPUPDATE_INSTALLED", platform),
                    $"Expected BIZSIM_APPUPDATE_INSTALLED on {platform}");
            }
        }
    }
}
