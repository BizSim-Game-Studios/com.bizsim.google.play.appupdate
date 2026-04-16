using NUnit.Framework;

namespace BizSim.Google.Play.AppUpdate.Tests
{
    [TestFixture]
    public class StaticAppUpdateConfigSourceTests
    {
        [Test]
        public void RemoteEnabled_DefaultsToTrue()
        {
            var src = new StaticAppUpdateConfigSource();
            Assert.IsTrue(src.RemoteEnabled);
        }

        [Test]
        public void AllThresholds_ReturnNull()
        {
            var src = new StaticAppUpdateConfigSource();
            Assert.IsNull(src.ImmediatePriorityFloor);
            Assert.IsNull(src.MinSessionCount);
            Assert.IsNull(src.PerVersionCooldownDays);
        }
    }
}
