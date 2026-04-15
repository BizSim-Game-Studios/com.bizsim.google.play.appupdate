using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using BizSim.Google.Play.AppUpdate;

namespace BizSim.Google.Play.AppUpdate.Tests
{
    public class MockFlexibleFlowTests
    {
        private static AppUpdateMockConfig MakeConfig(
            bool allowFlex = true,
            float duration = 0.3f,
            InstallErrorCode error = InstallErrorCode.NoError,
            InstallStatus failAt = InstallStatus.Unknown)
        {
            var c = ScriptableObject.CreateInstance<AppUpdateMockConfig>();
            c.SimulatedAvailability = UpdateAvailability.UpdateAvailable;
            c.AllowFlexible = allowFlex;
            c.AllowImmediate = true;
            c.FlexibleDownloadDurationSeconds = duration;
            c.SimulatedErrorCode = error;
            c.SimulatedFailureAt = failAt;
            return c;
        }

        [UnityTest]
        public IEnumerator FlexibleFlow_EmitsPendingDownloadingDownloaded_OnSuccess()
        {
            var cfg = MakeConfig();
            var p = new MockAppUpdateProvider(cfg);
            var observed = new List<InstallStatus>();
            p.OnStateUpdate += s => observed.Add(s.InstallStatus);

            var task = ((IFlexibleUpdateProvider)p).StartAsync(AppUpdateOptions.Flexible(), default, 10f);
            yield return new WaitForSeconds(0.7f);
            Assert.IsTrue(task.IsCompleted, "flexible flow should complete within 0.7s");
            Assert.IsNull(task.Result, "no error expected on success path");
            CollectionAssert.Contains(observed, InstallStatus.Pending);
            CollectionAssert.Contains(observed, InstallStatus.Downloading);
            CollectionAssert.Contains(observed, InstallStatus.Downloaded);
        }

        [UnityTest]
        public IEnumerator FlexibleFlow_FailsAtDownloading_WhenConfigured()
        {
            var cfg = MakeConfig(error: InstallErrorCode.ErrorInternalError, failAt: InstallStatus.Downloading);
            var p = new MockAppUpdateProvider(cfg);
            var observed = new List<InstallStatus>();
            p.OnStateUpdate += s => observed.Add(s.InstallStatus);

            var task = ((IFlexibleUpdateProvider)p).StartAsync(AppUpdateOptions.Flexible(), default, 10f);
            yield return new WaitForSeconds(0.7f);
            Assert.IsTrue(task.IsCompleted);
            Assert.NotNull(task.Result);
            Assert.AreEqual(InstallErrorCode.ErrorInternalError, task.Result.Value.Code);
            CollectionAssert.Contains(observed, InstallStatus.Failed);
            CollectionAssert.DoesNotContain(observed, InstallStatus.Downloaded);
        }

        [UnityTest]
        public IEnumerator FlexibleFlow_ReturnsNotAllowed_WhenAllowFlexibleFalse()
        {
            var cfg = MakeConfig(allowFlex: false);
            var p = new MockAppUpdateProvider(cfg);
            var task = ((IFlexibleUpdateProvider)p).StartAsync(AppUpdateOptions.Flexible(), default, 10f);
            yield return null;
            Assert.IsTrue(task.IsCompleted);
            Assert.NotNull(task.Result);
            Assert.AreEqual(InstallErrorCode.ErrorInstallNotAllowed, task.Result.Value.Code);
        }
    }
}
