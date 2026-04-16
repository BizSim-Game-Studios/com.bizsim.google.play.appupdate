using UnityEditor;
using UnityEngine;
using BizSim.Google.Play.AppUpdate;

namespace BizSim.Google.Play.AppUpdate.Samples.PolicyPresets
{
    /// <summary>
    /// Editor menu action that materializes three policy preset ScriptableObjects:
    /// <list type="number">
    ///   <item><b>PriorityFiveImmediateOnly</b> — immediate-only for priority 5; no flexible prompt.</item>
    ///   <item><b>StalenessSevenDayFlexible</b> — flexible prompt after 7 days staleness.</item>
    ///   <item><b>HybridSessionStaleness</b> — flexible after 5 sessions + 14 days since install.</item>
    /// </list>
    /// </summary>
    public static class CreatePolicyPresets
    {
        private const string OUTPUT_DIR = "Assets/BizSim/AppUpdate/PolicyPresets";

        [MenuItem("Assets/Create/BizSim/Google Play/AppUpdate/Policy Presets", false, 201)]
        public static void CreateAll()
        {
            EnsureFolders();

            // 1. PriorityFiveImmediateOnly — only priority-5 updates trigger, always immediate.
            //    No flexible prompt at all (flexibleMinPriority set impossibly high).
            Create("Policy_PriorityFiveImmediateOnly", p =>
            {
                SetField(p, "_remoteEnabled", true);
                SetField(p, "_immediatePriorityFloor", 5);
                SetField(p, "_flexibleMinPriority", 99); // Impossible — suppresses flexible
                SetField(p, "_flexibleMinStalenessDays", -1);
                SetField(p, "_minSessionCount", -1);
                SetField(p, "_minLaunchCount", -1);
                SetField(p, "_minDaysSinceInstall", -1);
                SetField(p, "_perVersionCooldownDays", 7);
            });

            // 2. StalenessSevenDayFlexible — flexible prompt for any update that's 7+ days stale.
            Create("Policy_StalenessSevenDayFlexible", p =>
            {
                SetField(p, "_remoteEnabled", true);
                SetField(p, "_immediatePriorityFloor", 5);
                SetField(p, "_flexibleMinPriority", -1);
                SetField(p, "_flexibleMinStalenessDays", 7);
                SetField(p, "_minSessionCount", -1);
                SetField(p, "_minLaunchCount", -1);
                SetField(p, "_minDaysSinceInstall", -1);
                SetField(p, "_perVersionCooldownDays", 2);
            });

            // 3. HybridSessionStaleness — flexible after 5 sessions AND 14 days since install.
            Create("Policy_HybridSessionStaleness", p =>
            {
                SetField(p, "_remoteEnabled", true);
                SetField(p, "_immediatePriorityFloor", 5);
                SetField(p, "_flexibleMinPriority", -1);
                SetField(p, "_flexibleMinStalenessDays", -1);
                SetField(p, "_minSessionCount", 5);
                SetField(p, "_minLaunchCount", -1);
                SetField(p, "_minDaysSinceInstall", 14);
                SetField(p, "_perVersionCooldownDays", 3);
            });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog(
                "BizSim AppUpdate",
                "3 policy presets created in " + OUTPUT_DIR,
                "OK");
        }

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/BizSim"))
                AssetDatabase.CreateFolder("Assets", "BizSim");
            if (!AssetDatabase.IsValidFolder("Assets/BizSim/AppUpdate"))
                AssetDatabase.CreateFolder("Assets/BizSim", "AppUpdate");
            if (!AssetDatabase.IsValidFolder(OUTPUT_DIR))
                AssetDatabase.CreateFolder("Assets/BizSim/AppUpdate", "PolicyPresets");
        }

        private static void Create(string name, System.Action<PolicyPresetConfig> configure)
        {
            var asset = ScriptableObject.CreateInstance<PolicyPresetConfig>();
            configure(asset);
            AssetDatabase.CreateAsset(asset, $"{OUTPUT_DIR}/{name}.asset");
        }

        /// <summary>
        /// Sets a serialized field on a PolicyPresetConfig instance via SerializedObject.
        /// This is needed because the fields are private [SerializeField] and cannot be set directly.
        /// </summary>
        private static void SetField(PolicyPresetConfig target, string fieldName, object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop == null) return;

            switch (value)
            {
                case bool b:
                    prop.boolValue = b;
                    break;
                case int i:
                    prop.intValue = i;
                    break;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
