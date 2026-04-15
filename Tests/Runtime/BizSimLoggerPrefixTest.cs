using NUnit.Framework;
using BizSim.Google.Play.AppUpdate;

namespace BizSim.Google.Play.AppUpdate.Tests
{
    public class BizSimLoggerPrefixTest
    {
        [Test]
        public void Prefix_IsExactlyBizSimAppUpdate()
        {
            Assert.AreEqual("[BizSim.AppUpdate] ", BizSimLogger.Prefix,
                "Per CROSS-PACKAGE-INVARIANTS.md §12.3, the per-package log prefix is a hard convention. Do not change.");
        }
    }
}
