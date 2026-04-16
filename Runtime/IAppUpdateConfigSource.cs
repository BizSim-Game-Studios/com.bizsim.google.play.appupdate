namespace BizSim.Google.Play.AppUpdate
{
    public interface IAppUpdateConfigSource
    {
        bool RemoteEnabled { get; }
        int? ImmediatePriorityFloor { get; }
        int? FlexibleMinPriority { get; }
        int? FlexibleMinStalenessDays { get; }
        int? MinSessionCount { get; }
        int? MinLaunchCount { get; }
        int? MinDaysSinceInstall { get; }
        int? PerVersionCooldownDays { get; }
    }
}
