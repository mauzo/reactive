using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace System.Reactive.Buffering
{
    internal interface IBufferOperator<T> : IProducerConsumerCollection<T>
    {
        protected IProducerConsumerCollection<T> Source { get; }

        bool ICollection.IsSynchronized => false;
        object ICollection.SyncRoot => null;

        new int Count => Source.Count;
        int ICollection.Count => Count;

        new IEnumerator<T> GetEnumerator() => Source.GetEnumerator();
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        T[] IProducerConsumerCollection<T>.ToArray() => this.ToArray();

        void ICollection.CopyTo(Array array, int index) => this.ToArray().CopyTo(array, index);
        void IProducerConsumerCollection<T>.CopyTo(T[] array, int index)
            => this.ToArray().CopyTo(array, index);
    }
}
