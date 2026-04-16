using NUnit.Framework;

namespace BizSim.Google.Play.AppUpdate.Tests
{
    [TestFixture]
    public class AppUpdateDiagnosticSnapshotTests
    {
        [Test]
        public void SchemaVersion_DefaultsToOne()
        {
            var snap = new AppUpdateDiagnosticSnapshot();
            Assert.AreEqual(1, snap.SchemaVersion);
        }

        [Test]
        public void JsonRoundTrip_PreservesFields()
        {
            var snap = new AppUpdateDiagnosticSnapshot
            {
                SchemaVersion = 1,
                PackageVersion = "1.0.1",
                Timestamp = "2026-04-16T00:00:00Z",
                SessionCount = 5,
                LaunchCount = 12,
                DaysSinceInstall = 14,
                RemoteEnabled = true,
                CooldownActive = false,
                OfflineGuardEnabled = true,
                DryRunMode = false,
                PolicyDecisionRaw = "Allow(Flexible)",
                LastErrorCode = "",
                LastUpdateInfoJson = "{\"priority\":3}",
                LastInstallStateJson = "{\"status\":0}",
                ImmediatePriorityFloor = 5,
                WatchdogTimeoutSeconds = 15
            };

            var json = snap.ToJson();
            var restored = AppUpdateDiagnosticSnapshot.FromJson(json);

            Assert.AreEqual(1, restored.SchemaVersion);
            Assert.AreEqual("1.0.1", restored.PackageVersion);
            Assert.AreEqual(5, restored.SessionCount);
            Assert.AreEqual(12, restored.LaunchCount);
            Assert.AreEqual(14, restored.DaysSinceInstall);
            Assert.IsTrue(restored.RemoteEnabled);
            Assert.IsFalse(restored.CooldownActive);
            Assert.IsTrue(restored.OfflineGuardEnabled);
            Assert.IsFalse(restored.DryRunMode);
            Assert.AreEqual("Allow(Flexible)", restored.PolicyDecisionRaw);
            Assert.AreEqual("", restored.LastErrorCode);
            Assert.AreEqual("{\"priority\":3}", restored.LastUpdateInfoJson);
            Assert.AreEqual("{\"status\":0}", restored.LastInstallStateJson);
            Assert.AreEqual(5, restored.ImmediatePriorityFloor);
            Assert.AreEqual(15, restored.WatchdogTimeoutSeconds);
        }
    }
}
