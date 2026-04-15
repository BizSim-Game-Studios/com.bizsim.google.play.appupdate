using NUnit.Framework;
using BizSim.Google.Play.AppUpdate;

namespace BizSim.Google.Play.AppUpdate.Tests
{
    public class AppUpdateEnumParityTests
    {
        [TestCase(0, (int)AppUpdateType.Flexible)]
        [TestCase(1, (int)AppUpdateType.Immediate)]
        public void AppUpdateType_MatchesGoogle(int expected, int actual) => Assert.AreEqual(expected, actual);

        [TestCase(0, (int)UpdateAvailability.Unknown)]
        [TestCase(1, (int)UpdateAvailability.UpdateNotAvailable)]
        [TestCase(2, (int)UpdateAvailability.UpdateAvailable)]
        [TestCase(3, (int)UpdateAvailability.DeveloperTriggeredUpdateInProgress)]
        public void UpdateAvailability_MatchesGoogle(int expected, int actual) => Assert.AreEqual(expected, actual);

        [TestCase(0, (int)InstallStatus.Unknown)]
        [TestCase(1, (int)InstallStatus.Pending)]
        [TestCase(2, (int)InstallStatus.Downloading)]
        [TestCase(3, (int)InstallStatus.Installing)]
        [TestCase(4, (int)InstallStatus.Installed)]
        [TestCase(5, (int)InstallStatus.Failed)]
        [TestCase(6, (int)InstallStatus.Canceled)]
        [TestCase(11, (int)InstallStatus.Downloaded)]
        public void InstallStatus_MatchesGoogle(int expected, int actual) => Assert.AreEqual(expected, actual);

        [TestCase(0, (int)InstallErrorCode.NoError)]
        [TestCase(-2, (int)InstallErrorCode.ErrorUnknown)]
        [TestCase(-3, (int)InstallErrorCode.ErrorApiNotAvailable)]
        [TestCase(-4, (int)InstallErrorCode.ErrorInvalidRequest)]
        [TestCase(-5, (int)InstallErrorCode.ErrorInstallUnavailable)]
        [TestCase(-6, (int)InstallErrorCode.ErrorInstallNotAllowed)]
        [TestCase(-7, (int)InstallErrorCode.ErrorDownloadNotPresent)]
        [TestCase(-8, (int)InstallErrorCode.ErrorInstallInProgress)]
        [TestCase(-9, (int)InstallErrorCode.ErrorPlayStoreNotFound)]
        [TestCase(-10, (int)InstallErrorCode.ErrorAppNotOwned)]
        [TestCase(-100, (int)InstallErrorCode.ErrorInternalError)]
        public void InstallErrorCode_MatchesGoogle(int expected, int actual) => Assert.AreEqual(expected, actual);

        [TestCase(-1, (int)ActivityResultCode.Ok)]
        [TestCase(0, (int)ActivityResultCode.Canceled)]
        [TestCase(1, (int)ActivityResultCode.InAppUpdateFailed)]
        public void ActivityResultCode_MatchesAndroid(int expected, int actual) => Assert.AreEqual(expected, actual);
    }
}
