using System.Collections.Concurrent;

namespace System.Reactive.Buffering
{
    public interface IBufferProvider<TElement>
    {
        IProducerConsumerCollection<TElement> GetBuffer();
    }
}
