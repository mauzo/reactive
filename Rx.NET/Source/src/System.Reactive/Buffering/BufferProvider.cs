using System.Collections.Concurrent;
using System.Diagnostics;

namespace System.Reactive.Buffering
{
    public static class BufferProvider
    {
        public static Func<IProducerConsumerCollection<T>> Infinite<T>()
            => () => new ConcurrentQueue<T>();

        public static BufferProvider<T> Limit<T>(int limit)
            => new BufferProvider<T>(limit);
    }
}
