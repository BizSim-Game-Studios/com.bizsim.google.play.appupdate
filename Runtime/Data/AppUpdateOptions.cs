namespace BizSim.Google.Play.AppUpdate
{
    public sealed class AppUpdateOptions
    {
        public AppUpdateType AppUpdateType { get; }
        public bool AllowAssetPackDeletion { get; private set; }

        private AppUpdateOptions(AppUpdateType type) { AppUpdateType = type; }

        public static AppUpdateOptions Flexible() => new AppUpdateOptions(AppUpdateType.Flexible);
        public static AppUpdateOptions Immediate() => new AppUpdateOptions(AppUpdateType.Immediate);

        public AppUpdateOptions WithAllowAssetPackDeletion(bool allow)
        {
            AllowAssetPackDeletion = allow;
            return this;
        }
    }
}
