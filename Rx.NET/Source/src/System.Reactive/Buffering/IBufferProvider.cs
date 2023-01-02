using System.Collections.Concurrent;

namespace System.Reactive.Buffering
{
    public interface IBufferProvider<TElement> : IDisposable
    {
        IProducerConsumerCollection<TElement> GetBuffer();
    }
}
