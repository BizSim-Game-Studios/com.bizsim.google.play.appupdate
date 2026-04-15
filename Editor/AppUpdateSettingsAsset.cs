using System.IO;
using UnityEditor;
using UnityEngine;

namespace BizSim.Google.Play.AppUpdate.Editor
{
    internal static class AppUpdateSettingsAsset
    {
        // Single source of truth for the asset path — see CROSS-INVARIANTS §12.5.
        private static readonly string AssetPath   = AppUpdateSettings.AssetDatabasePath;
        private static readonly string AssetFolder = Path.GetDirectoryName(AssetPath).Replace('\\', '/');

        public static AppUpdateSettings LoadOrCreate()
        {
            var existing = AssetDatabase.LoadAssetAtPath<AppUpdateSettings>(AssetPath);
            if (existing != null) return existing;

            EnsureFolder(AssetFolder);
            var inst = ScriptableObject.CreateInstance<AppUpdateSettings>();
            AssetDatabase.CreateAsset(inst, AssetPath);
            AssetDatabase.SaveAssets();
            return inst;
        }

        public static void Save()
        {
            var asset = AssetDatabase.LoadAssetAtPath<AppUpdateSettings>(AssetPath);
            if (asset == null) return;
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssetIfDirty(asset);
        }

        public static void ResetToDefaults()
        {
            var asset = LoadOrCreate();
            var fresh = ScriptableObject.CreateInstance<AppUpdateSettings>();
            EditorUtility.CopySerialized(fresh, asset);
            Object.DestroyImmediate(fresh);
            Save();
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
