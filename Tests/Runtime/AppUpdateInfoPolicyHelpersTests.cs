using System;
using NUnit.Framework;
using BizSim.Google.Play.AppUpdate;

namespace BizSim.Google.Play.AppUpdate.Tests
{
    public class AppUpdateInfoPolicyHelpersTests
    {
        // Columns: availability, priority, staleness, flexAllowed, immAllowed, expectFlex, expectImm
        [TestCase(UpdateAvailability.UpdateNotAvailable, 0, null, false, false, false, false)]
        [TestCase(UpdateAvailability.UpdateAvailable,    0, null, true,  true,  false, false)]
        [TestCase(UpdateAvailability.UpdateAvailable,    2, 0,    true,  true,  true,  false)]
        [TestCase(UpdateAvailability.UpdateAvailable,    1, 10,   true,  true,  true,  false)]
        [TestCase(UpdateAvailability.UpdateAvailable,    4, 3,    true,  true,  true,  true)]
        [TestCase(UpdateAvailability.UpdateAvailable,    5, 0,    false, true,  false, true)]
        [TestCase(UpdateAvailability.UpdateAvailable,    5, 0,    true,  false, true,  false)]
        [TestCase(UpdateAvailability.UpdateAvailable,    2, null, true,  true,  true,  false)]
        public void ClassifierMatrix(
            UpdateAvailability avail, int priority, int? staleness,
            bool flexAllowed, bool immAllowed, bool expectFlex, bool expectImm)
        {
            var info = new AppUpdateInfo(
                avail, 100, priority, staleness,
                InstallStatus.Unknown, 0, 0,
                flexAllowed, immAllowed, DateTime.UtcNow);
            Assert.AreEqual(expectFlex, info.IsFlexibleUpdateRecommended(), "flexible");
            Assert.AreEqual(expectImm,  info.IsImmediateUpdateRequired(),   "immediate");
        }

        [Test]
        public void DownloadProgress_ZeroTotal_ReturnsZero()
        {
            var s = new InstallState(InstallStatus.Pending, 0, 0, InstallErrorCode.NoError, DateTime.UtcNow);
            Assert.AreEqual(0f, s.DownloadProgress);
        }

        [Test]
        public void DownloadProgress_Half_Returns050()
        {
            var s = new InstallState(InstallStatus.Downloading, 500, 1000, InstallErrorCode.NoError, DateTime.UtcNow);
            Assert.AreEqual(0.5f, s.DownloadProgress, 0.001f);
        }

        [Test]
        public void DownloadProgress_Full_ReturnsOne()
        {
            var s = new InstallState(InstallStatus.Downloaded, 1000, 1000, InstallErrorCode.NoError, DateTime.UtcNow);
            Assert.AreEqual(1f, s.DownloadProgress);
        }
    }
}
