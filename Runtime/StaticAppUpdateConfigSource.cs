namespace BizSim.Google.Play.AppUpdate
{
    public sealed class StaticAppUpdateConfigSource : IAppUpdateConfigSource
    {
        public bool RemoteEnabled => true;
        public int? ImmediatePriorityFloor => null;
        public int? FlexibleMinPriority => null;
        public int? FlexibleMinStalenessDays => null;
        public int? MinSessionCount => null;
        public int? MinLaunchCount => null;
        public int? MinDaysSinceInstall => null;
        public int? PerVersionCooldownDays => null;
    }
}
