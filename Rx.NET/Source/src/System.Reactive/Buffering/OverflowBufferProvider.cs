using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace System.Reactive.Buffering
{
    internal class OverflowBufferProvider<T> : BufferProvider<T>
    {
        private int _limit;

        private Subject<IObservable<T>> _overflow = new();

        public override IObservable<IObservable<T>> Overflow
            => IfNotDisposed(_overflow).AsObservable();
        
        public OverflowBufferProvider (int limit) => _limit = limit;

        public override IProducerConsumerCollection<T> GetBuffer()
        {
            ThrowIfDisposed();
            var buf = new OverflowBuffer<T>(_limit);
            _overflow.OnNext(buf.Overflow);
            return buf;
        }

        protected override void DisposeManaged()
        {
            _overflow.OnCompleted();
            _overflow.Dispose();
            base.DisposeManaged();
        }
    }
}
