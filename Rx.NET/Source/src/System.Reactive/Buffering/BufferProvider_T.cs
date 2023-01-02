using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace System.Reactive.Buffering
{
    public class BufferProvider<T> : IDisposable
    {
        private int _limit;

        private int _disposed = 0;
        private Subject<IObservable<T>> _overflow = new();

        private bool IsDisposed => Interlocked.CompareExchange(ref _disposed, 0, 0) == 1;

        public static implicit operator Func<IProducerConsumerCollection<T>>(BufferProvider<T> value)
            => value.GetBuffer;

        public IObservable<IObservable<T>> Overflow => _overflow?.AsObservable();
        
        public BufferProvider (int limit) => _limit = limit;

        public IProducerConsumerCollection<T> GetBuffer()
        {
            if (IsDisposed)
                throw new ObjectDisposedException("buffer provider");

            var buf = new OverflowBuffer<T>(_limit);
            _overflow.OnNext(buf.Overflow);
            return buf;
        }

        public void Dispose ()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 1)
                return;
            Debug.WriteLine("OBP dispose");
            _overflow.OnCompleted();
            _overflow = null;
        }
    }
}
