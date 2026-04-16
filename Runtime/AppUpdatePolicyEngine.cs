using System;
using UnityEngine;

namespace BizSim.Google.Play.AppUpdate
{
    public sealed class AppUpdatePolicyEngine : IAppUpdatePolicyEngine
    {
        readonly IAppUpdateConfigSource _config;
        readonly IConsentGate _consentGate;
        readonly int _defaultMinSessions;
        readonly int _defaultMinDays;
        readonly int _defaultImmediatePriorityFloor;
        readonly bool _offlineGuardEnabled;
        readonly Func<bool> _networkReachabilityProvider;

        public AppUpdatePolicyEngine(
            IAppUpdateConfigSource config,
            IConsentGate consentGate,
            int defaultMinSessions = 3,
            int defaultMinDays = 7,
            int defaultImmediatePriorityFloor = 5,
            bool offlineGuardEnabled = true,
            Func<bool> networkReachabilityProvider = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _consentGate = consentGate ?? throw new ArgumentNullException(nameof(consentGate));
            _defaultMinSessions = defaultMinSessions;
            _defaultMinDays = defaultMinDays;
            _defaultImmediatePriorityFloor = defaultImmediatePriorityFloor;
            _offlineGuardEnabled = offlineGuardEnabled;
            _networkReachabilityProvider = networkReachabilityProvider
                ?? (() => Application.internetReachability != NetworkReachability.NotReachable);
        }

        public PolicyDecision Evaluate(AppUpdatePolicyContext context)
        {
            if (!_config.RemoteEnabled)
                return PolicyDecision.Block("killswitch_disabled");

            if (!_consentGate.IsConsented(context))
                return PolicyDecision.Block("consent_denied");

            if (_offlineGuardEnabled && !_networkReachabilityProvider())
                return PolicyDecision.Block("offline");

            if (context.SessionCount < _defaultMinSessions
                || context.DaysSinceInstall < _defaultMinDays)
                return PolicyDecision.Block("first_run_grace");

            var info = context.LastUpdateInfo;

            if (info.UpdateAvailability == UpdateAvailability.DeveloperTriggeredUpdateInProgress)
                return PolicyDecision.Allow(AppUpdateType.Immediate);

            if (info.UpdateAvailability != UpdateAvailability.UpdateAvailable)
                return PolicyDecision.Block("update_not_available");

            var minSessions = ClampNonNegative(_config.MinSessionCount, "MinSessionCount");
            if (minSessions.HasValue && context.SessionCount < minSessions.Value)
                return PolicyDecision.Block("min_sessions_not_met");

            var minLaunches = ClampNonNegative(_config.MinLaunchCount, "MinLaunchCount");
            if (minLaunches.HasValue && context.LaunchCount < minLaunches.Value)
                return PolicyDecision.Block("min_launches_not_met");

            int immFloor = ClampNonNegative(_config.ImmediatePriorityFloor, "ImmediatePriorityFloor")
                ?? _defaultImmediatePriorityFloor;
            if (info.UpdatePriority >= immFloor && info.IsImmediateAllowed)
                return PolicyDecision.Allow(AppUpdateType.Immediate);

            if (info.IsFlexibleAllowed)
                return PolicyDecision.Allow(AppUpdateType.Flexible);

            if (info.IsImmediateAllowed)
                return PolicyDecision.Allow(AppUpdateType.Immediate);

            return PolicyDecision.Block("update_type_not_allowed");
        }

        static int? ClampNonNegative(int? value, string fieldName)
        {
            if (!value.HasValue) return null;
            if (value.Value < 0)
            {
                BizSimLogger.Warning(
                    $"IAppUpdateConfigSource.{fieldName} returned {value.Value} (negative). " +
                    "Treating as null (skip). Check your Remote Config setup.");
                return null;
            }
            return value;
        }
    }
}
