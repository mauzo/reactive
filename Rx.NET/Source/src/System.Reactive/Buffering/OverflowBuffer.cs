using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;

namespace System.Reactive.Buffering
{
    internal class OverflowBuffer<T> : ConcurrentQueue<T>, IProducerConsumerCollection<T>, IDisposable
    {
        private int _limit;

        private static int _next_id = 0;
        private readonly int _id = _next_id++;

        private int _disposed = 0;
        private Subject<T> _overflow = new();

        private bool IsDisposed => Interlocked.CompareExchange(ref _disposed, 0, 0) == 1;

        public IObservable<T> Overflow => _overflow.AsObservable();

        private void Log(string msg) => Debug.WriteLine($"OB [{_id}]: {msg}");

        public OverflowBuffer (int limit)
        {
            Log("new");
            _limit = limit;
        }

        bool IProducerConsumerCollection<T>.TryAdd (T item)
        {
            if (IsDisposed) return false;

            /* We rely on the Rx protocol to ensure there is no race
             * here. If we do race the buffer still will not grow
             * infinitely, so it's not a huge problem. */
            if (Count < _limit) {
                Log($"enqueue {item}, count {Count}");
                Enqueue(item);
                return true;
            }
            
            /* We have 'successfully' queued this item by passing it
             * to the overflow handler. */
            Log($"overflow {item}");
            _overflow.OnNext(item);
            return true;
        }

        bool IProducerConsumerCollection<T>.TryTake (out T item)
        {
            if (IsDisposed) {
                item = default(T);
                return false;
            }

            var ok = TryDequeue(out item);
            Log($"dequeue {item}");
            return ok;
        }

        public void Dispose ()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 1)
                return;

            Log($"dispose");
            while (TryDequeue(out T _)) { }
            _overflow?.OnCompleted();
            _overflow = null;
        }
    }
}
