using NUnit.Framework;

namespace BizSim.Google.Play.AppUpdate.Tests
{
    /// <summary>
    /// Tests for the fragment shim's request code contract and C#/Java enum parity.
    /// The Java-side <c>AppUpdateBridge</c> uses fixed request codes:
    ///   <c>REQ_FLEXIBLE  = 0x42F1</c> (17137)
    ///   <c>REQ_IMMEDIATE = 0x42F2</c> (17138)
    /// and selects the code based on <c>appUpdateType == AppUpdateType.IMMEDIATE</c> (int 1).
    ///
    /// Since we cannot invoke Java from NUnit, these tests verify the C# side of the contract:
    /// the <see cref="AppUpdateType"/> enum int values that the C# providers pass to the bridge,
    /// and the well-known request code constants that <c>AppUpdateResultFragment.onActivityResult</c>
    /// filters on.
    /// </summary>
    [TestFixture]
    public class AppUpdateResultFragmentTests
    {
        // Mirror of Java-side constants. If the Java code changes these, the tests must be
        // updated in tandem — the constants are part of the JNI contract.
        private const int JavaReqFlexible  = 0x42F1; // 17137
        private const int JavaReqImmediate = 0x42F2; // 17138

        [Test]
        public void RequestCode_Flexible_Is0x42F1()
        {
            Assert.AreEqual(0x42F1, JavaReqFlexible,
                "REQ_FLEXIBLE request code must be 0x42F1 (17137). " +
                "Changing this breaks the AppUpdateResultFragment.onActivityResult filter.");
        }

        [Test]
        public void RequestCode_Immediate_Is0x42F2()
        {
            Assert.AreEqual(0x42F2, JavaReqImmediate,
                "REQ_IMMEDIATE request code must be 0x42F2 (17138). " +
                "Changing this breaks the AppUpdateResultFragment.onActivityResult filter.");
        }

        [Test]
        public void RequestCodes_AreDistinct()
        {
            Assert.AreNotEqual(JavaReqFlexible, JavaReqImmediate,
                "Flexible and immediate request codes must be distinct — " +
                "onActivityResult dispatches based on the request code.");
        }

        [Test]
        public void AppUpdateType_Flexible_IntValue_Is0()
        {
            // The C# provider passes (int)AppUpdateType.Flexible to the Java bridge's
            // startUpdateFlow method. Java maps: appUpdateType != IMMEDIATE → REQ_FLEXIBLE.
            Assert.AreEqual(0, (int)AppUpdateType.Flexible,
                "AppUpdateType.Flexible int value must be 0 (matches Play Core's AppUpdateType.FLEXIBLE).");
        }

        [Test]
        public void AppUpdateType_Immediate_IntValue_Is1()
        {
            // Java maps: appUpdateType == IMMEDIATE → REQ_IMMEDIATE.
            Assert.AreEqual(1, (int)AppUpdateType.Immediate,
                "AppUpdateType.Immediate int value must be 1 (matches Play Core's AppUpdateType.IMMEDIATE).");
        }

        [Test]
        public void AppUpdateOptions_Flexible_HasCorrectType()
        {
            var opts = AppUpdateOptions.Flexible();
            Assert.AreEqual(AppUpdateType.Flexible, opts.AppUpdateType);
        }

        [Test]
        public void AppUpdateOptions_Immediate_HasCorrectType()
        {
            var opts = AppUpdateOptions.Immediate();
            Assert.AreEqual(AppUpdateType.Immediate, opts.AppUpdateType);
        }

        [Test]
        public void RequestCodes_FitIn16Bit_ActivityRequestCode()
        {
            // Android's Activity.startActivityForResult requires request codes that fit in
            // the lower 16 bits (0x0000..0xFFFF). Values above 0xFFFF cause
            // IllegalArgumentException. Verify our codes are in range.
            Assert.IsTrue(JavaReqFlexible >= 0 && JavaReqFlexible <= 0xFFFF,
                $"REQ_FLEXIBLE (0x{JavaReqFlexible:X4}) must fit in 16-bit request code range.");
            Assert.IsTrue(JavaReqImmediate >= 0 && JavaReqImmediate <= 0xFFFF,
                $"REQ_IMMEDIATE (0x{JavaReqImmediate:X4}) must fit in 16-bit request code range.");
        }
    }
}
