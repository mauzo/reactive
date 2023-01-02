using System.Collections.Concurrent;
using System.Diagnostics;

namespace System.Linq.Buffering
{
    public static class BufferProvider
    {
        private class InfiniteBufferProvider<T> : IBufferProvider<T>
        {
            public IProducerConsumerCollection<T> GetBuffer()
            {
                Debug.WriteLine($"IBP: Creating new CQueue");
                return new ConcurrentQueue<T>();
            }
        }

        /* These objects have no state; it would be reasonable to cache
         * them. */
        public static IBufferProvider<T> Infinite<T>()
            => new InfiniteBufferProvider<T>();
    }
}
