using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace BizSim.Google.Play.AppUpdate
{
    /// <summary>
    /// Bounded queue of install state snapshots with async iteration. Capacity is constructor-
    /// parameterized and sourced from <see cref="AppUpdateSettings.InstallStateQueueCapacity"/>
    /// by <see cref="AppUpdateController"/> at Awake time per CROSS-INVARIANTS §12.2.
    /// When full, the OLDEST state is dropped (drop-oldest policy) — the latest state is always
    /// the most actionable for a download progress UI.
    /// </summary>
    internal sealed class InstallStateStream
    {
        private readonly int _maxCapacity;
        private readonly Queue<InstallState> _queue = new Queue<InstallState>();
        private readonly object _gate = new object();
        private TaskCompletionSource<bool> _signal = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        public InstallStateStream(int maxCapacity)
        {
            if (maxCapacity < 1)
                throw new ArgumentOutOfRangeException(nameof(maxCapacity), maxCapacity, "maxCapacity must be >= 1");
            _maxCapacity = maxCapacity;
        }

        public void Enqueue(InstallState state)
        {
            TaskCompletionSource<bool> signalToFire = null;
            lock (_gate)
            {
                // Drop-oldest on overflow.
                while (_queue.Count >= _maxCapacity)
                    _queue.Dequeue();
                _queue.Enqueue(state);
                signalToFire = _signal;
            }
            signalToFire.TrySetResult(true);
        }

        public async IAsyncEnumerable<InstallState> ReadAsync(
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            while (!ct.IsCancellationRequested)
            {
                InstallState? next = null;
                Task awaitTask = null;
                lock (_gate)
                {
                    if (_queue.Count > 0)
                    {
                        next = _queue.Dequeue();
                    }
                    else
                    {
                        // Fresh TCS so each await gets a new signal.
                        _signal = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                        awaitTask = _signal.Task;
                    }
                }

                if (next.HasValue)
                {
                    yield return next.Value;
                    continue;
                }

                // No state available — wait for signal OR cancellation.
                using (ct.Register(() => { /* signal resolves via outer loop check */ }))
                {
                    var cancelTask = Task.Delay(Timeout.Infinite, ct);
                    var winner = await Task.WhenAny(awaitTask, cancelTask).ConfigureAwait(false);
                    if (winner == cancelTask) yield break;
                }
            }
        }
    }
}
