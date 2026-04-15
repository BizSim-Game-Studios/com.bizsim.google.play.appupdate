using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using BizSim.Google.Play.AppUpdate;
using BizSim.Google.Play.AppUpdate.Editor;

namespace BizSim.Google.Play.AppUpdate.EditorTests
{
    public class AppUpdateSettingsAssetTests
    {
        private const string PATH = "Assets/Resources/BizSim/GooglePlay/AppUpdateSettings.asset";

        [TearDown]
        public void TearDown()
        {
            if (AssetDatabase.LoadAssetAtPath<AppUpdateSettings>(PATH) != null)
                AssetDatabase.DeleteAsset(PATH);
        }

        [Test]
        public void LoadOrCreate_AutoCreatesAssetAndFolders()
        {
            // Ensure clean state
            if (AssetDatabase.LoadAssetAtPath<AppUpdateSettings>(PATH) != null)
                AssetDatabase.DeleteAsset(PATH);

            var asset = AppUpdateSettingsAsset.LoadOrCreate();
            Assert.NotNull(asset);
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<AppUpdateSettings>(PATH));
            Assert.IsTrue(AssetDatabase.IsValidFolder("Assets/Resources/BizSim/GooglePlay"));
        }

        [Test]
        public void Save_RoundTripsLogLevel()
        {
            var asset = AppUpdateSettingsAsset.LoadOrCreate();
            asset.LogLevel = BizSimLogger.LogLevel.Error;
            AppUpdateSettingsAsset.Save();

            AssetDatabase.Refresh();
            var reloaded = AssetDatabase.LoadAssetAtPath<AppUpdateSettings>(PATH);
            Assert.AreEqual(BizSimLogger.LogLevel.Error, reloaded.LogLevel);
        }

        [Test]
        public void ResetToDefaults_RestoresOriginalValues()
        {
            var asset = AppUpdateSettingsAsset.LoadOrCreate();
            asset.LogsEnabled = false;
            asset.LogLevel = BizSimLogger.LogLevel.Error;
            asset.UseMockInDevelopmentBuild = true;
            asset.EnableAnalyticsByDefault = true;
            asset.InstallStateQueueCapacity = 16;
            asset.DefaultTimeoutSeconds = 120f;
            asset.AutoStartInstallStateListener = false;
            AppUpdateSettingsAsset.Save();

            AppUpdateSettingsAsset.ResetToDefaults();

            var reloaded = AssetDatabase.LoadAssetAtPath<AppUpdateSettings>(PATH);
            var fresh = ScriptableObject.CreateInstance<AppUpdateSettings>();
            Assert.AreEqual(fresh.LogsEnabled, reloaded.LogsEnabled);
            Assert.AreEqual(fresh.LogLevel, reloaded.LogLevel);
            Assert.AreEqual(fresh.UseMockInDevelopmentBuild, reloaded.UseMockInDevelopmentBuild);
            Assert.AreEqual(fresh.EnableAnalyticsByDefault, reloaded.EnableAnalyticsByDefault);
            Assert.AreEqual(fresh.InstallStateQueueCapacity, reloaded.InstallStateQueueCapacity);
            Assert.AreEqual(fresh.DefaultTimeoutSeconds, reloaded.DefaultTimeoutSeconds);
            Assert.AreEqual(fresh.AutoStartInstallStateListener, reloaded.AutoStartInstallStateListener);
            Object.DestroyImmediate(fresh);
        }
    }
}
