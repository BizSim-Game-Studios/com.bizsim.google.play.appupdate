using System.IO;
using UnityEditor;
using UnityEngine;

namespace BizSim.Google.Play.AppUpdate.Editor
{
    internal static class AppUpdateSettingsAsset
    {
        private const string FOLDER = "Assets/Resources/BizSim/GooglePlay";
        private const string PATH   = FOLDER + "/AppUpdateSettings.asset";

        public static AppUpdateSettings LoadOrCreate()
        {
            var existing = AssetDatabase.LoadAssetAtPath<AppUpdateSettings>(PATH);
            if (existing != null) return existing;

            EnsureFolder(FOLDER);
            var inst = ScriptableObject.CreateInstance<AppUpdateSettings>();
            AssetDatabase.CreateAsset(inst, PATH);
            AssetDatabase.SaveAssets();
            return inst;
        }

        public static void Save()
        {
            var asset = AssetDatabase.LoadAssetAtPath<AppUpdateSettings>(PATH);
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
            var parent = Path.GetDirectoryName(path)!.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
