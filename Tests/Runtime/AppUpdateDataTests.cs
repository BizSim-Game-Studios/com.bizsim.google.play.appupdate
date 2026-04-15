using System;
using NUnit.Framework;
using BizSim.Google.Play.AppUpdate;

namespace BizSim.Google.Play.AppUpdate.Tests
{
    public class AppUpdateDataTests
    {
        [TestCase(InstallErrorCode.ErrorInternalError, true)]
        [TestCase(InstallErrorCode.ErrorUnknown, true)]
        [TestCase(InstallErrorCode.Timeout, true)]
        [TestCase(InstallErrorCode.BridgeNotInitialized, true)]
        [TestCase(InstallErrorCode.ErrorPlayStoreNotFound, false)]
        [TestCase(InstallErrorCode.ErrorAppNotOwned, false)]
        [TestCase(InstallErrorCode.ErrorInvalidRequest, false)]
        [TestCase(InstallErrorCode.CancelledByCaller, false)]
        public void AppUpdateError_IsRetryable_MatchesPolicy(InstallErrorCode code, bool expected)
            => Assert.AreEqual(expected, AppUpdateError.IsRetryable(code));

        [Test]
        public void InstallState_IsTerminal_ReturnsTrueForInstalled()
        {
            var s = new InstallState(InstallStatus.Installed, 0, 0, InstallErrorCode.NoError, DateTime.UtcNow);
            Assert.IsTrue(s.IsTerminal);
        }

        [Test]
        public void InstallState_IsTerminal_ReturnsTrueForFailed()
        {
            var s = new InstallState(InstallStatus.Failed, 0, 0, InstallErrorCode.ErrorInternalError, DateTime.UtcNow);
            Assert.IsTrue(s.IsTerminal);
        }

        [Test]
        public void InstallState_IsTerminal_ReturnsFalseForDownloading()
        {
            var s = new InstallState(InstallStatus.Downloading, 500, 1000, InstallErrorCode.NoError, DateTime.UtcNow);
            Assert.IsFalse(s.IsTerminal);
        }
    }
}
