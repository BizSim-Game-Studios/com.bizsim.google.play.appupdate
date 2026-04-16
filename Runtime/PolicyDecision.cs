using System;

namespace BizSim.Google.Play.AppUpdate
{
    public readonly struct PolicyDecision
    {
        public enum Kind { Allow, Block, Defer }

        public Kind Type { get; }
        public AppUpdateType UpdateType { get; }
        public string Reason { get; }
        public TimeSpan MinDelay { get; }

        public bool IsAllow => Type == Kind.Allow;
        public bool IsBlock => Type == Kind.Block;
        public bool IsDefer => Type == Kind.Defer;

        PolicyDecision(Kind type, AppUpdateType updateType, string reason, TimeSpan minDelay)
        {
            Type = type;
            UpdateType = updateType;
            Reason = reason;
            MinDelay = minDelay;
        }

        public static PolicyDecision Allow(AppUpdateType updateType) =>
            new(Kind.Allow, updateType, null, TimeSpan.Zero);

        public static PolicyDecision Block(string reason) =>
            new(Kind.Block, default, reason ?? "unspecified", TimeSpan.Zero);

        public static PolicyDecision Defer(TimeSpan minDelay) =>
            new(Kind.Defer, default, null, minDelay);

        public override string ToString() => Type switch
        {
            Kind.Allow => $"Allow({UpdateType})",
            Kind.Block => $"Block({Reason})",
            Kind.Defer => $"Defer({MinDelay.TotalSeconds:F0}s)",
            _ => "Unknown"
        };
    }
}
