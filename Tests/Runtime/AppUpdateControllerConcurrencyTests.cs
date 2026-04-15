using System;
using NUnit.Framework;
using UnityEngine;
using BizSim.Google.Play.AppUpdate;

namespace BizSim.Google.Play.AppUpdate.Tests
{
    public class AppUpdateControllerConcurrencyTests
    {
        private GameObject _go;
        private AppUpdateController _controller;
        private AppUpdateMockConfig _mockConfig;

        [SetUp]
        public void SetUp()
        {
            _mockConfig = ScriptableObject.CreateInstance<AppUpdateMockConfig>();
            _mockConfig.AllowFlexible = true;
            _mockConfig.AllowImmediate = true;
            _mockConfig.FlexibleDownloadDurationSeconds = 10f;
            _mockConfig.ImmediateFlowDurationSeconds = 10f;

            _go = new GameObject("[test-appupdate-concurrency]");
            _controller = _go.AddComponent<AppUpdateController>();
            var _ = AppUpdateController.Instance;

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
        public void Flexible_SecondCallWhileInFlight_Throws()
        {
            _ = _controller.StartFlexibleUpdateAsync();
            Assert.Throws<InvalidOperationException>(
                () => { var _ = _controller.StartFlexibleUpdateAsync(); });
        }

        [Test]
        public void Immediate_SecondCallWhileInFlight_Throws()
        {
            _ = _controller.StartImmediateUpdateAsync();
            Assert.Throws<InvalidOperationException>(
                () => { var _ = _controller.StartImmediateUpdateAsync(); });
        }

        [Test]
        public void FlexibleAndImmediate_Independent_BothAllowed()
        {
            _ = _controller.StartFlexibleUpdateAsync();
            Assert.DoesNotThrow(() => { var _ = _controller.StartImmediateUpdateAsync(); });
        }
    }
}
