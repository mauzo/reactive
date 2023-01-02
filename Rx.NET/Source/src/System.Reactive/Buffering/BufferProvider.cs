using System.Collections.Concurrent;
using System.Diagnostics;

namespace System.Reactive.Buffering
{
    public static class BufferProvider
    {
        public static Func<IProducerConsumerCollection<T>> Infinite<T>()
            => () => new ConcurrentQueue<T>();

        public static IOverflowBufferProvider<T> Limit<T>(int limit)
            => new OverflowBufferProvider<T>(limit);
    }
}
