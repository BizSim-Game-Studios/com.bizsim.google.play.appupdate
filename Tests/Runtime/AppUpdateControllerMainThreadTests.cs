using System;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using BizSim.Google.Play.AppUpdate;

namespace BizSim.Google.Play.AppUpdate.Tests
{
    public class AppUpdateControllerMainThreadTests
    {
        private GameObject _go;
        private AppUpdateController _controller;
        private AppUpdateMockConfig _mockConfig;

        [SetUp]
        public void SetUp()
        {
            _mockConfig = ScriptableObject.CreateInstance<AppUpdateMockConfig>();
            _mockConfig.AllowFlexible = true;
            _mockConfig.FlexibleDownloadDurationSeconds = 10f;  // long so the test doesn't race

            _go = new GameObject("[test-appupdate-controller]");
            _controller = _go.AddComponent<AppUpdateController>();
            var _ = AppUpdateController.Instance;  // force main-thread wire-up

            var field = typeof(AppUpdateController).GetField("_mockConfig",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(_controller, _mockConfig);
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) UnityEngine.Object.DestroyImmediate(_go);
            _go = null;
            _controller = null;
            if (_mockConfig != null) UnityEngine.Object.DestroyImmediate(_mockConfig);
            _mockConfig = null;
        }

        [Test]
        public async Task CheckForUpdateAsync_FromBackgroundThread_ThrowsInvalidOperation()
        {
            var captured = _controller;
            var thrown = await Task.Run(() =>
            {
                try { _ = captured.CheckForUpdateAsync(); return (Exception)null; }
                catch (Exception ex) { return ex; }
            });
            Assert.IsInstanceOf<InvalidOperationException>(thrown);
            StringAssert.Contains("main thread", thrown.Message);
        }

        [Test]
        public async Task StartFlexibleUpdateAsync_FromBackgroundThread_ThrowsInvalidOperation()
        {
            var captured = _controller;
            var thrown = await Task.Run(() =>
            {
                try { _ = captured.StartFlexibleUpdateAsync(); return (Exception)null; }
                catch (Exception ex) { return ex; }
            });
            Assert.IsInstanceOf<InvalidOperationException>(thrown);
            StringAssert.Contains("main thread", thrown.Message);
        }
    }
}
