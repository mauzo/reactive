using System.Collections.Concurrent;
using System.Diagnostics;

namespace System.Reactive.Buffering
{
    public static class BufferProvider
    {
        public static Func<IProducerConsumerCollection<T>> Infinite<T>()
            => () => new ConcurrentQueue<T>();

        public static BufferProvider<T> LimitByCount<T>(int limit)
            => new OverflowBufferProvider<T>(limit);
    }
}
