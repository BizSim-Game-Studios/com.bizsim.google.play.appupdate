using NUnit.Framework;
using BizSim.Google.Play.AppUpdate;

namespace BizSim.Google.Play.AppUpdate.Tests
{
    public class AppUpdateOptionsBuilderTests
    {
        [Test]
        public void Flexible_FactoryReturnsFlexibleType()
        {
            var o = AppUpdateOptions.Flexible();
            Assert.AreEqual(AppUpdateType.Flexible, o.AppUpdateType);
            Assert.IsFalse(o.AllowAssetPackDeletion);
        }

        [Test]
        public void Immediate_FactoryReturnsImmediateType()
        {
            var o = AppUpdateOptions.Immediate();
            Assert.AreEqual(AppUpdateType.Immediate, o.AppUpdateType);
            Assert.IsFalse(o.AllowAssetPackDeletion);
        }

        [Test]
        public void WithAllowAssetPackDeletion_SetsFlagAndReturnsSameInstance()
        {
            var o = AppUpdateOptions.Flexible();
            var result = o.WithAllowAssetPackDeletion(true);
            Assert.AreSame(o, result);
            Assert.IsTrue(o.AllowAssetPackDeletion);
        }
    }
}
