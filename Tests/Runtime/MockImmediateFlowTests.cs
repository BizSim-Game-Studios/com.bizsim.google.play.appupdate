using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using BizSim.Google.Play.AppUpdate;

namespace BizSim.Google.Play.AppUpdate.Tests
{
    public class MockImmediateFlowTests
    {
        private static AppUpdateMockConfig MakeConfig(bool allowImm = true, float duration = 0.2f, InstallErrorCode error = InstallErrorCode.NoError)
        {
            var c = ScriptableObject.CreateInstance<AppUpdateMockConfig>();
            c.SimulatedAvailability = UpdateAvailability.UpdateAvailable;
            c.AllowFlexible = true;
            c.AllowImmediate = allowImm;
            c.ImmediateFlowDurationSeconds = duration;
            c.SimulatedErrorCode = error;
            return c;
        }

        [Test]
        public async Task Immediate_Resolves_AfterConfiguredDuration()
        {
            var cfg = MakeConfig();
            var p = new MockAppUpdateProvider(cfg);
            var err = await ((IImmediateUpdateProvider)p).StartAsync(AppUpdateOptions.Immediate(), default, 10f);
            Assert.IsNull(err);
        }

        [Test]
        public async Task Immediate_ReturnsNotAllowed_WhenDisabled()
        {
            var cfg = MakeConfig(allowImm: false);
            var p = new MockAppUpdateProvider(cfg);
            var err = await ((IImmediateUpdateProvider)p).StartAsync(AppUpdateOptions.Immediate(), default, 10f);
            Assert.NotNull(err);
            Assert.AreEqual(InstallErrorCode.ErrorInstallNotAllowed, err.Value.Code);
        }

        [Test]
        public async Task Immediate_ReturnsError_WhenConfigured()
        {
            var cfg = MakeConfig(error: InstallErrorCode.ErrorPlayStoreNotFound);
            var p = new MockAppUpdateProvider(cfg);
            var err = await ((IImmediateUpdateProvider)p).StartAsync(AppUpdateOptions.Immediate(), default, 10f);
            Assert.NotNull(err);
            Assert.AreEqual(InstallErrorCode.ErrorPlayStoreNotFound, err.Value.Code);
        }

        [Test]
        public async Task IsImmediateUpdateInProgress_ReturnsTrue_WhenAvailabilityIsInProgress()
        {
            var cfg = MakeConfig();
            cfg.SimulatedAvailability = UpdateAvailability.DeveloperTriggeredUpdateInProgress;
            var p = new MockAppUpdateProvider(cfg);
            bool inProgress = await ((IImmediateUpdateProvider)p).IsImmediateUpdateInProgressAsync(default);
            Assert.IsTrue(inProgress);
        }
    }
}
