using NUnit.Framework;

namespace BizSim.Google.Play.AppUpdate.Tests
{
    [TestFixture]
    public class PolicyDecisionTests
    {
        [Test]
        public void AllowFlexible_CarriesType()
        {
            var d = PolicyDecision.Allow(AppUpdateType.Flexible);
            Assert.IsTrue(d.IsAllow);
            Assert.AreEqual(AppUpdateType.Flexible, d.UpdateType);
        }

        [Test]
        public void AllowImmediate_CarriesType()
        {
            var d = PolicyDecision.Allow(AppUpdateType.Immediate);
            Assert.AreEqual(AppUpdateType.Immediate, d.UpdateType);
        }

        [Test]
        public void Block_CarriesReason()
        {
            var d = PolicyDecision.Block("killswitch_disabled");
            Assert.IsTrue(d.IsBlock);
            Assert.AreEqual("killswitch_disabled", d.Reason);
        }

        [Test]
        public void Defer_CarriesMinDelay()
        {
            var d = PolicyDecision.Defer(System.TimeSpan.FromMinutes(5));
            Assert.IsTrue(d.IsDefer);
            Assert.AreEqual(5, d.MinDelay.TotalMinutes, 0.01);
        }
    }
}
