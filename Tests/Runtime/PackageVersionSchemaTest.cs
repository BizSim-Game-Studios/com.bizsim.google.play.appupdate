using NUnit.Framework;
using BizSim.Google.Play.AppUpdate;

namespace BizSim.Google.Play.AppUpdate.Tests
{
    /// <summary>K8 PackageVersion schema drift guard (Plan G).</summary>
    public class PackageVersionSchemaTest
    {
        [Test]
        public void NativeSdkFields_ArePopulated()
        {
            Assert.IsFalse(string.IsNullOrEmpty(PackageVersion.NativeSdkVersion));
            Assert.IsFalse(string.IsNullOrEmpty(PackageVersion.NativeSdkLabel));
            Assert.IsFalse(string.IsNullOrEmpty(PackageVersion.NativeSdkArtifactCoord));
        }

        [Test]
        public void NativeSdkArtifactCoord_EndsWithVersion()
        {
            Assert.IsTrue(PackageVersion.NativeSdkArtifactCoord.EndsWith(":" + PackageVersion.NativeSdkVersion));
        }

        [Test]
        public void NativeSdkFields_MatchExpectedAppUpdateValues()
        {
            Assert.AreEqual("2.1.0", PackageVersion.NativeSdkVersion);
            Assert.AreEqual("Play Core (app-update)", PackageVersion.NativeSdkLabel);
            Assert.AreEqual("com.google.android.play:app-update:2.1.0", PackageVersion.NativeSdkArtifactCoord);
        }

#pragma warning disable CS0618
        [Test]
        public void LegacyAlias_ResolvesToSameValue()
        {
            Assert.AreEqual(PackageVersion.NativeSdkVersion, PackageVersion.PlayCoreVersion);
        }
#pragma warning restore CS0618
    }
}
