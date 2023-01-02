using System.Collections.Concurrent;

namespace System.Reactive.Buffering
{
    internal class LimitedQueue<T> : ConcurrentQueue<T>, IProducerConsumerCollection<T>
    {
        private int _limit;

        public LimitedQueue (int limit)
            => _limit = limit;

        bool IProducerConsumerCollection<T>.TryAdd(T item)
        {
            /* There is a race condition here, but no way around it
             * without reimplementing ConcurrentQueue. Even if we race
             * the queue is still limited. */
            if (Count >= _limit)
                return false;

            Enqueue(item);
            return true;
        }
    }
}
            
