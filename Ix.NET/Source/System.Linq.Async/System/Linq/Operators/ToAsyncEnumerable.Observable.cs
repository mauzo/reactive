// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT License.
// See the LICENSE file in the project root for more information. 

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace System.Linq
{
    public static partial class AsyncEnumerable
    {
        /// <summary>
        /// Converts an observable sequence to an async-enumerable sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <param name="source">Observable sequence to convert to an async-enumerable sequence.</param>
        /// <param name="buffers">Buffer provider to use to buffer the Observable.</param>
        /// <returns>The async-enumerable sequence whose elements are pulled from the given observable sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
        public static IAsyncEnumerable<TSource> ToAsyncEnumerable<TSource>(this IObservable<TSource> source, Func<IProducerConsumerCollection<TSource>> buffers)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));

            return new ObservableAsyncEnumerable<TSource>(source, buffers);
        }

        /// <summary>
        /// Converts an observable sequence to an async-enumerable sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <param name="source">Observable sequence to convert to an async-enumerable sequence.</param>
        /// <returns>The async-enumerable sequence whose elements are pulled from the given observable sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
        public static IAsyncEnumerable<TSource> ToAsyncEnumerable<TSource>(this IObservable<TSource> source)
            => source.ToAsyncEnumerable(() => new ConcurrentQueue<TSource>());
        
        private sealed class ObservableAsyncEnumerable<TSource> : AsyncIterator<TSource>, IObserver<TSource>
        {
            private readonly IObservable<TSource> _source;
            private readonly Func<IProducerConsumerCollection<TSource>> _buffers;

            private static int _next_id = 0;
            private readonly int _id = _next_id++;
            private IProducerConsumerCollection<TSource>? _values;
            private Exception? _error;
            private bool _completed;
            private TaskCompletionSource<bool>? _signal;
            private IDisposable? _subscription;
            private CancellationTokenRegistration _ctr;

            public ObservableAsyncEnumerable(IObservable<TSource> source, Func<IProducerConsumerCollection<TSource>> buffers)
            {
                _source = source;
                _buffers = buffers;
            }

            public override AsyncIteratorBase<TSource> Clone() => new ObservableAsyncEnumerable<TSource>(_source, _buffers);

            public override ValueTask DisposeAsync()
            {
                Dispose();

                return base.DisposeAsync();
            }

            private void Log (string msg) => Debug.WriteLine($"OAE [{_id}]: {msg}");

            protected override async ValueTask<bool> MoveNextCore()
            {
                //
                // REVIEW: How often should we check? At the very least, we want to prevent
                //         subscribing if cancellation is requested. A case may be made to
                //         check for each iteration, namely because this operator is a bridge
                //         with another interface. However, we also wire up cancellation to
                //         the observable subscription, so there's redundancy here.
                //
                _cancellationToken.ThrowIfCancellationRequested();

                switch (_state)
                {
                    case AsyncIteratorState.Allocated:
                        //
                        // NB: Breaking change to align with lazy nature of async iterators.
                        //
                        //     In previous implementations, the Subscribe call happened during
                        //     the call to GetAsyncEnumerator.
                        //
                        // REVIEW: Confirm this design point. This implementation is compatible
                        //         with an async iterator using "yield return", e.g. subscribing
                        //         to the observable sequence and yielding values out of a local
                        //         queue filled by observer callbacks. However, it departs from
                        //         the dual treatment of Subscribe/GetEnumerator.
                        //

                        Log("Create buffer");
                        _values = _buffers();
                        _subscription = _source.Subscribe(this);
                        _ctr = _cancellationToken.Register(OnCanceled, state: null);
                        _state = AsyncIteratorState.Iterating;
                        goto case AsyncIteratorState.Iterating;

                    case AsyncIteratorState.Iterating:
                        while (true)
                        {
                            var completed = Volatile.Read(ref _completed);

                            if (_values!.TryTake(out _current!))
                            {
                                Log($"pulled {_current}");
                                return true;
                            }
                            else if (completed)
                            {
                                var error = _error;

                                if (error != null)
                                {
                                    throw error;
                                }

                                return false;
                            }

                            await Resume().ConfigureAwait(false);
                            Volatile.Write(ref _signal, null);
                        }
                }

                await DisposeAsync().ConfigureAwait(false);
                return false;
            }

            public void OnCompleted()
            {
                Volatile.Write(ref _completed, true);

                DisposeSubscription();
                OnNotification();
            }

            public void OnError(Exception error)
            {
                _error = error;
                Volatile.Write(ref _completed, true);

                DisposeSubscription();
                OnNotification();
            }

            public void OnNext(TSource value)
            {
                Log($"pushing {value}");
                _values?.TryAdd(value);

                OnNotification();
            }

            private void OnNotification()
            {
                while (true)
                {
                    var signal = Volatile.Read(ref _signal);

                    if (signal == TaskExt.True)
                    {
                        return;
                    }

                    if (signal != null)
                    {
                        signal.TrySetResult(true);
                        return;
                    }

                    if (Interlocked.CompareExchange(ref _signal, TaskExt.True, null) == null)
                    {
                        return;
                    }
                }
            }

            private void Dispose()
            {
                _ctr.Dispose();
                DisposeSubscription();

                _values = null;
                _error = null;
            }

            private void DisposeSubscription() => Interlocked.Exchange(ref _subscription, null)?.Dispose();

            private void OnCanceled(object? state)
            {
                var cancelledTcs = default(TaskCompletionSource<bool>);

                Dispose();

                while (true)
                {
                    var signal = Volatile.Read(ref _signal);

                    if (signal != null)
                    {
                        if (signal.TrySetCanceled(_cancellationToken))
                            return;
                    }

                    if (cancelledTcs == null)
                    {
                        cancelledTcs = new TaskCompletionSource<bool>();
                        cancelledTcs.TrySetCanceled(_cancellationToken);
                    }

                    if (Interlocked.CompareExchange(ref _signal, cancelledTcs, signal) == signal)
                        return;
                }
            }

            private Task Resume()
            {
                TaskCompletionSource<bool>? newSignal = null;

                while (true)
                {
                    var signal = Volatile.Read(ref _signal);

                    if (signal != null)
                    {
                        return signal.Task;
                    }

                    newSignal ??= new TaskCompletionSource<bool>();

                    if (Interlocked.CompareExchange(ref _signal, newSignal, null) == null)
                    {
                        return newSignal.Task;
                    }
                }
            }
        }
    }
}
