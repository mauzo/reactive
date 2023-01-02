using System.Collections.Generic;

namespace System.Reactive.Buffering
{
    public interface IOverflowBufferProvider<T> : IBufferProvider<T>
    {
        IObservable<IObservable<T>> Overflow { get; }
    }
}
