using System.Collections.Concurrent;

namespace System.Linq.Buffering
{
    public interface IBufferProvider<TElement>
    {
        IProducerConsumerCollection<TElement> GetBuffer();
    }
}
