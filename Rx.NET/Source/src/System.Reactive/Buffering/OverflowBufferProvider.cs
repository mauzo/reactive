using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace System.Reactive.Buffering
{
    internal class OverflowBufferProvider<T> : IOverflowBufferProvider<T>
    {
        private int _limit;

        private Subject<IObservable<T>> _overflow = new();

        public IObservable<IObservable<T>> Overflow => _overflow.AsObservable();
        
        public OverflowBufferProvider (int limit) => _limit = limit;

        public IProducerConsumerCollection<T> GetBuffer()
        {
            var buf = new OverflowBuffer(_limit);
            _overflow.OnNext(buf.Overflow);
            return buf;
        }

        private class OverflowBuffer : ConcurrentQueue<T>, IProducerConsumerCollection<T>
        {
            private int _limit;

            private static int _next_id = 0;
            private readonly int _id = _next_id++;
            private Subject<T> _overflow = new();

            public IObservable<T> Overflow => _overflow.AsObservable();

            private void Log(string msg) => Debug.WriteLine($"OB [{_id}]: {msg}");

            public OverflowBuffer (int limit)
            {
                Log("new");
                _limit = limit;
            }

            bool IProducerConsumerCollection<T>.TryAdd (T item)
            {
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
                var ok = TryDequeue(out item);
                Log($"dequeue {item}");
                return ok;
            }
        }
    }
}
