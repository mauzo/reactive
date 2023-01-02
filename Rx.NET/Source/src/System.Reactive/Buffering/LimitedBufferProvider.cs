using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace System.Reactive.Buffering
{
    internal class LimitedBufferProvider<T> : BufferProvider<T>
    {
        private int _limit;

        public LimitedBufferProvider (int limit)
            => _limit = limit;

        public override IProducerConsumerCollection<T> GetBuffer()
        {
            ThrowIfDisposed();
            return new LimitedQueue<T>(_limit);
        }
    }
}
