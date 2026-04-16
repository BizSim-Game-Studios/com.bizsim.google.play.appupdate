using System;
using NUnit.Framework;
using UnityEngine;

namespace BizSim.Google.Play.AppUpdate.Tests
{
    [TestFixture]
    public class AppUpdateSessionTrackerTests
    {
        AppUpdateSessionTracker _tracker;
        DateTime _fakeNow;

        [SetUp]
        public void Setup()
        {
            _fakeNow = new DateTime(2026, 4, 16, 0, 0, 0, DateTimeKind.Utc);
            // Clear any leftover state from previous test runs
            PlayerPrefs.DeleteKey("bizsim_appupdate_session_count");
            PlayerPrefs.DeleteKey("bizsim_appupdate_launch_count");
            PlayerPrefs.DeleteKey("bizsim_appupdate_install_date");
            PlayerPrefs.Save();
            _tracker = new AppUpdateSessionTracker(() => _fakeNow);
        }

        [TearDown]
        public void TearDown()
        {
            _tracker.ClearForTesting();
        }

        [Test]
        public void RecordSession_Increments()
        {
            Assert.AreEqual(0, _tracker.SessionCount);
            _tracker.RecordSession();
            Assert.AreEqual(1, _tracker.SessionCount);
            _tracker.RecordSession();
            Assert.AreEqual(2, _tracker.SessionCount);
        }

        [Test]
        public void RecordLaunch_Increments()
        {
            Assert.AreEqual(0, _tracker.LaunchCount);
            _tracker.RecordLaunch();
            Assert.AreEqual(1, _tracker.LaunchCount);
            _tracker.RecordLaunch();
            Assert.AreEqual(2, _tracker.LaunchCount);
        }

        [Test]
        public void DaysSinceInstall_ReturnsZero_OnFirstRun()
        {
            Assert.AreEqual(0, _tracker.DaysSinceInstall);
        }

        [Test]
        public void DaysSinceInstall_ReflectsElapsedDays()
        {
            // Advance the fake clock by 10 days
            _fakeNow = _fakeNow.AddDays(10);
            Assert.AreEqual(10, _tracker.DaysSinceInstall);
        }

        [Test]
        public void IsInFirstRunGrace_True_WhenBelowThresholds()
        {
            Assert.IsTrue(_tracker.IsInFirstRunGrace(minSessions: 3, minDays: 7));
        }

        [Test]
        public void IsInFirstRunGrace_False_WhenAboveThresholds()
        {
            _tracker.RecordSession();
            _tracker.RecordSession();
            _tracker.RecordSession();
            _fakeNow = _fakeNow.AddDays(7);
            Assert.IsFalse(_tracker.IsInFirstRunGrace(minSessions: 3, minDays: 7));
        }
    }
}
