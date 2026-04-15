using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using BizSim.Google.Play.AppUpdate;

namespace BizSim.Google.Play.AppUpdate.Tests
{
    public class InstallStateStreamTests
    {
        private static InstallState S(InstallStatus status, long b = 0) =>
            new InstallState(status, b, 100, InstallErrorCode.NoError, DateTime.UtcNow);

        [Test]
        public async Task Enqueue_Single_Yields()
        {
            var stream = new InstallStateStream(maxCapacity: 8);
            stream.Enqueue(S(InstallStatus.Pending));
            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromSeconds(1));
            await foreach (var s in stream.ReadAsync(cts.Token))
            {
                Assert.AreEqual(InstallStatus.Pending, s.InstallStatus);
                return; // Consume one and exit.
            }
            Assert.Fail("stream yielded no items");
        }

        [Test]
        public async Task DropOldest_OnOverflow()
        {
            var stream = new InstallStateStream(maxCapacity: 8);
            // Enqueue 16 — first 8 should be dropped.
            for (long i = 0; i < 16; i++)
                stream.Enqueue(S(InstallStatus.Downloading, i));

            var collected = new List<long>();
            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromSeconds(1));
            try
            {
                await foreach (var s in stream.ReadAsync(cts.Token))
                {
                    collected.Add(s.BytesDownloaded);
                    if (collected.Count >= 8) break;
                }
            }
            catch (OperationCanceledException) { /* allowed */ }

            Assert.AreEqual(8, collected.Count);
            // The oldest 8 (bytesDownloaded 0..7) were dropped; remaining are 8..15.
            Assert.AreEqual(8, collected[0]);
            Assert.AreEqual(15, collected[7]);
        }

        [Test]
        public async Task Cancellation_ExitsIterator()
        {
            var stream = new InstallStateStream(maxCapacity: 8);
            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromMilliseconds(100));
            int count = 0;
            try
            {
                await foreach (var _ in stream.ReadAsync(cts.Token))
                {
                    count++;
                }
            }
            catch (OperationCanceledException) { /* allowed */ }
            Assert.AreEqual(0, count, "no items should yield before cancellation");
        }
    }
}
