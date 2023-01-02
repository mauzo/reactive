using System.Collections.Concurrent;
using System.Reactive.Linq;
using System.Threading;

/* IDisposable implementation warnings */
#pragma warning disable CA1063, CA1816

namespace System.Reactive.Buffering
{
    public abstract class BufferProvider<T> : IDisposable
    {
        private int _disposed = 0;

        /* I'm not entirely sure this cmpxchg is necessary; I don't know
         * if a plain read can anticipate an Interlocked.Exchange on
         * another thread. */
        public bool IsDisposed => Interlocked.CompareExchange(ref _disposed, 0, 0) == 1;

        public virtual IObservable<IObservable<T>> Overflow => Observable.Empty<IObservable<T>>();

        public Func<IProducerConsumerCollection<T>> ToFunc() => GetBuffer;
        public static implicit operator Func<IProducerConsumerCollection<T>>? (BufferProvider<T> value)
            => value?.ToFunc();

        public abstract IProducerConsumerCollection<T> GetBuffer();

        public void Dispose ()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 1)
                return;
            DisposeManaged();
        }

        protected virtual void DisposeManaged () { }

        protected void ThrowIfDisposed ()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(GetType().Name);
        }

        protected TValue IfNotDisposed<TValue> (TValue value)
        {
            ThrowIfDisposed();
            return value;
        }
    }
}

