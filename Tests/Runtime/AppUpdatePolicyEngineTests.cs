using NUnit.Framework;
using System;

namespace BizSim.Google.Play.AppUpdate.Tests
{
    [TestFixture]
    public class AppUpdatePolicyEngineTests
    {
        AppUpdatePolicyEngine _engine;
        TestConfigSource _config;

        [SetUp]
        public void Setup()
        {
            _config = new TestConfigSource();
            _engine = new AppUpdatePolicyEngine(
                _config, new AlwaysAllowConsentGate(),
                defaultMinSessions: 3, defaultMinDays: 7,
                defaultImmediatePriorityFloor: 5);
        }

        [Test]
        public void KillSwitch_Off_Blocks()
        {
            _config.RemoteEnabled = false;
            var ctx = MakeContext(sessions: 10, days: 30, priority: 5);
            Assert.AreEqual("killswitch_disabled", _engine.Evaluate(ctx).Reason);
        }

        [Test]
        public void ConsentDenied_Blocks()
        {
            var engine = new AppUpdatePolicyEngine(
                _config, new DenyConsentGate(),
                defaultMinSessions: 0, defaultMinDays: 0);
            var ctx = MakeContext(sessions: 10, days: 30, priority: 3);
            Assert.AreEqual("consent_denied", engine.Evaluate(ctx).Reason);
        }

        [Test]
        public void Offline_Blocks()
        {
            var engine = new AppUpdatePolicyEngine(
                _config, new AlwaysAllowConsentGate(),
                defaultMinSessions: 0, defaultMinDays: 0,
                offlineGuardEnabled: true,
                networkReachabilityProvider: () => false);
            var ctx = MakeContext(sessions: 10, days: 30, priority: 3);
            Assert.AreEqual("offline", engine.Evaluate(ctx).Reason);
        }

        [Test]
        public void FirstRunGrace_Blocks()
        {
            var ctx = MakeContext(sessions: 1, days: 2, priority: 3);
            Assert.AreEqual("first_run_grace", _engine.Evaluate(ctx).Reason);
        }

        [Test]
        public void UpdateNotAvailable_Blocks()
        {
            var ctx = MakeContext(sessions: 10, days: 30, priority: 3,
                availability: UpdateAvailability.UpdateNotAvailable);
            Assert.AreEqual("update_not_available", _engine.Evaluate(ctx).Reason);
        }

        [Test]
        public void DeveloperTriggeredUpdateInProgress_AllowsImmediate()
        {
            var ctx = MakeContext(sessions: 10, days: 30, priority: 3,
                availability: UpdateAvailability.DeveloperTriggeredUpdateInProgress);
            var decision = _engine.Evaluate(ctx);
            Assert.IsTrue(decision.IsAllow);
            Assert.AreEqual(AppUpdateType.Immediate, decision.UpdateType);
        }

        [Test]
        public void Priority5_AllowsImmediate()
        {
            var ctx = MakeContext(sessions: 10, days: 30, priority: 5,
                isImmediateAllowed: true);
            var decision = _engine.Evaluate(ctx);
            Assert.IsTrue(decision.IsAllow);
            Assert.AreEqual(AppUpdateType.Immediate, decision.UpdateType);
        }

        [Test]
        public void Priority3_AllowsFlexible()
        {
            var ctx = MakeContext(sessions: 10, days: 30, priority: 3,
                isFlexibleAllowed: true);
            var decision = _engine.Evaluate(ctx);
            Assert.IsTrue(decision.IsAllow);
            Assert.AreEqual(AppUpdateType.Flexible, decision.UpdateType);
        }

        [Test]
        public void ImmediateNotAllowed_FallsToFlexible()
        {
            var ctx = MakeContext(sessions: 10, days: 30, priority: 5,
                isImmediateAllowed: false, isFlexibleAllowed: true);
            var decision = _engine.Evaluate(ctx);
            Assert.AreEqual(AppUpdateType.Flexible, decision.UpdateType);
        }

        [Test]
        public void NeitherAllowed_Blocks()
        {
            var ctx = MakeContext(sessions: 10, days: 30, priority: 5,
                isImmediateAllowed: false, isFlexibleAllowed: false);
            Assert.AreEqual("update_type_not_allowed", _engine.Evaluate(ctx).Reason);
        }

        [Test]
        public void ConfigOverride_ImmediatePriorityFloor()
        {
            _config.ImmediatePriorityFloor = 3;
            var ctx = MakeContext(sessions: 10, days: 30, priority: 3,
                isImmediateAllowed: true);
            var decision = _engine.Evaluate(ctx);
            Assert.AreEqual(AppUpdateType.Immediate, decision.UpdateType);
        }

        [Test]
        public void NegativeConfigValue_ClampedToNull()
        {
            _config.MinSessionCount = -1;
            var ctx = MakeContext(sessions: 10, days: 30, priority: 3);
            Assert.IsTrue(_engine.Evaluate(ctx).IsAllow);
        }

        static AppUpdatePolicyContext MakeContext(
            int sessions, int days, int priority,
            UpdateAvailability availability = UpdateAvailability.UpdateAvailable,
            bool isFlexibleAllowed = true, bool isImmediateAllowed = true)
        {
            var info = new AppUpdateInfo(
                availability, 100, priority, days,
                InstallStatus.Unknown, 0, 0,
                isFlexibleAllowed, isImmediateAllowed, DateTime.UtcNow);
            return new AppUpdatePolicyContext(sessions, sessions, days, info, "1.0");
        }

        class TestConfigSource : IAppUpdateConfigSource
        {
            public bool RemoteEnabled { get; set; } = true;
            public int? ImmediatePriorityFloor { get; set; }
            public int? FlexibleMinPriority { get; set; }
            public int? FlexibleMinStalenessDays { get; set; }
            public int? MinSessionCount { get; set; }
            public int? MinLaunchCount { get; set; }
            public int? MinDaysSinceInstall { get; set; }
            public int? PerVersionCooldownDays { get; set; }
        }

        class DenyConsentGate : IConsentGate
        {
            public bool IsConsented(AppUpdatePolicyContext context) => false;
        }
    }
}
