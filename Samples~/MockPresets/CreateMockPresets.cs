using UnityEditor;
using UnityEngine;
using BizSim.Google.Play.AppUpdate;

namespace BizSim.Google.Play.AppUpdate.Samples.MockPresets
{
    public static class CreateMockPresets
    {
        private const string OUTPUT_DIR = "Assets/BizSim/AppUpdate/MockPresets";

        [MenuItem("Assets/Create/BizSim/Google Play/AppUpdate/Mock Presets", false, 200)]
        public static void CreateAll()
        {
            EnsureFolders();

            Create("Mock_NoUpdate", p =>
            {
                p.SimulatedAvailability = UpdateAvailability.UpdateNotAvailable;
                p.AllowFlexible = false;
                p.AllowImmediate = false;
            });

            Create("Mock_Flexible_Fast", p =>
            {
                p.SimulatedAvailability = UpdateAvailability.UpdateAvailable;
                p.SimulatedAvailableVersionCode = 101;
                p.SimulatedUpdatePriority = 2;
                p.SimulatedClientVersionStalenessDays = 3;
                p.AllowFlexible = true;
                p.AllowImmediate = true;
                p.FlexibleDownloadDurationSeconds = 2f;
            });

            Create("Mock_Flexible_Slow", p =>
            {
                p.SimulatedAvailability = UpdateAvailability.UpdateAvailable;
                p.SimulatedAvailableVersionCode = 101;
                p.SimulatedUpdatePriority = 2;
                p.SimulatedClientVersionStalenessDays = 15;
                p.AllowFlexible = true;
                p.AllowImmediate = true;
                p.FlexibleDownloadDurationSeconds = 8f;
            });

            Create("Mock_Flexible_Failed_Download", p =>
            {
                p.SimulatedAvailability = UpdateAvailability.UpdateAvailable;
                p.SimulatedAvailableVersionCode = 101;
                p.SimulatedUpdatePriority = 2;
                p.SimulatedClientVersionStalenessDays = 7;
                p.AllowFlexible = true;
                p.AllowImmediate = true;
                p.FlexibleDownloadDurationSeconds = 4f;
                p.SimulatedFailureAt = InstallStatus.Downloading;
                p.SimulatedErrorCode = InstallErrorCode.ErrorInternalError;
            });

            Create("Mock_Immediate_High_Priority", p =>
            {
                p.SimulatedAvailability = UpdateAvailability.UpdateAvailable;
                p.SimulatedAvailableVersionCode = 101;
                p.SimulatedUpdatePriority = 5;
                p.SimulatedClientVersionStalenessDays = 1;
                p.AllowFlexible = true;
                p.AllowImmediate = true;
                p.ImmediateFlowDurationSeconds = 1.5f;
            });

            Create("Mock_Immediate_In_Progress", p =>
            {
                p.SimulatedAvailability = UpdateAvailability.DeveloperTriggeredUpdateInProgress;
                p.SimulatedAvailableVersionCode = 101;
                p.SimulatedUpdatePriority = 5;
                p.SimulatedClientVersionStalenessDays = 2;
                p.AllowFlexible = true;
                p.AllowImmediate = true;
                p.ImmediateFlowDurationSeconds = 1.5f;
            });

            Create("Mock_Play_Store_Not_Found", p =>
            {
                p.SimulatedAvailability = UpdateAvailability.Unknown;
                p.AllowFlexible = false;
                p.AllowImmediate = false;
                p.SimulatedErrorCode = InstallErrorCode.ErrorPlayStoreNotFound;
            });

            Create("Mock_Sideloaded_Install", p =>
            {
                p.SimulatedAvailability = UpdateAvailability.UpdateNotAvailable;
                p.AllowFlexible = false;
                p.AllowImmediate = false;
                p.SimulatedErrorCode = InstallErrorCode.ErrorAppNotOwned;
            });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog(
                "BizSim AppUpdate",
                "8 mock presets created in " + OUTPUT_DIR,
                "OK");
        }

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/BizSim"))
                AssetDatabase.CreateFolder("Assets", "BizSim");
            if (!AssetDatabase.IsValidFolder("Assets/BizSim/AppUpdate"))
                AssetDatabase.CreateFolder("Assets/BizSim", "AppUpdate");
            if (!AssetDatabase.IsValidFolder(OUTPUT_DIR))
                AssetDatabase.CreateFolder("Assets/BizSim/AppUpdate", "MockPresets");
        }

        private static void Create(string name, System.Action<AppUpdateMockConfig> configure)
        {
            var asset = ScriptableObject.CreateInstance<AppUpdateMockConfig>();
            configure(asset);
            AssetDatabase.CreateAsset(asset, $"{OUTPUT_DIR}/{name}.asset");
        }
    }
}
