using System;
using System.Collections;
using System.Collections.Generic;
using Reactive.Streams.Example.Unicast;
using Reactive.Streams.Utils;

namespace Reactive.Streams.TCK.Support
{
    public class InfiniteHelperPublisher<T> : AsyncIterablePublisher<T>
    {
        public InfiniteHelperPublisher(Func<int, T> create) : base(new InfiniteHelperEnumerable(create))
        {
        }

        private sealed class InfiniteHelperEnumerable : IEnumerable<T>
        {
            private readonly InfiniteHelperEnumerator _enumerator;

            public InfiniteHelperEnumerable(Func<int, T> create)
            {
                _enumerator = new InfiniteHelperEnumerator(create);
            }

            public IEnumerator<T> GetEnumerator() => _enumerator;

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class InfiniteHelperEnumerator : IEnumerator<T>
        {
            private readonly Func<int, T> _create;
            private int _at;

            public InfiniteHelperEnumerator(Func<int, T> create)
            {
                _create = create;
            }

            public T Current { get; private set; }

            object IEnumerator.Current => Current;

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                try
                {
                    Current = _create(_at++); // Wraps around on overflow
                    return true;
                }
                catch (Exception ex)
                {
                    throw new IllegalStateException($"Failed to create element in {GetType().Name} for id {_at - 1}!",
                        ex);
                }
            }

            public void Reset()
            {
                throw new NotSupportedException();
            }

        }
    }
}
