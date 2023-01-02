using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;

namespace System.Reactive.Buffering
{
    internal class OverflowBuffer<T> : IBufferOperator<T>, IDisposable
    {
        private IProducerConsumerCollection<T> _queue;
        IProducerConsumerCollection<T> IBufferOperator<T>.Source => _queue;

        private static int _next_id = 0;
        private readonly int _id = _next_id++;

        private int _disposed = 0;
        private Subject<T> _overflow = new();

        private bool IsDisposed => Interlocked.CompareExchange(ref _disposed, 0, 0) == 1;

        public IObservable<T> Overflow => IfNotDisposed(_overflow).AsObservable();

        private void Log(string msg) => Debug.WriteLine($"OB [{_id}]: {msg}");

        public OverflowBuffer (IProducerConsumerCollection<T> queue)
        {
            Log("new");
            _queue = queue;
        }

        public bool TryAdd (T item)
        {
            if (IsDisposed) return false;

            /* XXX There is a race here: we can become disposed at this
             * point and the queued item will not be unqueued until we
             * are GCd. We may also end up passing the item to a
             * disposed Subject. We can only avoid this by locking. */

            if (_queue.TryAdd(item))
                return true;
            
            /* We have 'successfully' queued this item by passing it
             * to the overflow handler. */
            Log($"overflow {item}");
            _overflow.OnNext(item);
            return true;
        }

        public bool TryTake (out T item)
        {
            if (IsDisposed) {
                item = default(T);
                return false;
            }
            return _queue.TryTake(out item);
        }

        public void Dispose ()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 1)
                return;

            Log($"dispose");
            while (TryTake(out T _)) { }
            _overflow.OnCompleted();
            _overflow.Dispose();
        }

        private TValue IfNotDisposed<TValue> (TValue value)
        {
            if (IsDisposed)
                throw new ObjectDisposedException("OverflowBuffer");
            return value;
        }
    }
}
