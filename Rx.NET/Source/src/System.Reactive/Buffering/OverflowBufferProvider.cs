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
        private BufferProvider<T> _source;

        private Subject<IObservable<T>> _overflow = new();

        public override IObservable<IObservable<T>> Overflow
            => IfNotDisposed(_overflow).AsObservable();
        
        public OverflowBufferProvider (BufferProvider<T> source)
            => _source = source;

        public override IProducerConsumerCollection<T> GetBuffer()
        {
            ThrowIfDisposed();
            var queue = _source.GetBuffer();
            var buf = new OverflowBuffer<T>(queue);
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
