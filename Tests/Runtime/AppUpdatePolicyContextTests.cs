using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace BizSim.Google.Play.AppUpdate.Tests
{
    [TestFixture]
    public class AppUpdatePolicyContextTests
    {
        [Test]
        public void Constructor_SetsAllFields()
        {
            var info = new AppUpdateInfo(
                UpdateAvailability.UpdateAvailable, 42, 3, 7,
                InstallStatus.Unknown, 0, 0, true, true, DateTime.UtcNow);
            var ctx = new AppUpdatePolicyContext(
                sessionCount: 5, launchCount: 12, daysSinceInstall: 14,
                lastUpdateInfo: info, appVersion: "1.2.3");

            Assert.AreEqual(5, ctx.SessionCount);
            Assert.AreEqual(42, ctx.LastUpdateInfo.AvailableVersionCode);
            Assert.AreEqual(3, ctx.LastUpdateInfo.UpdatePriority);
            Assert.IsTrue(ctx.LastUpdateInfo.IsFlexibleAllowed);
        }
    }
}
