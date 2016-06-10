using System;
using System.Collections;
using System.Collections.Generic;
using Reactive.Streams.Example.Unicast;
using Reactive.Streams.Utils;

namespace Reactive.Streams.TCK.Support
{
    public class HelperPublisher<T> : AsyncIterablePublisher<T>
    {
        public HelperPublisher(int from, int to, Func<int, T> create)
            : base(new HelperPublisherEnumerable(from, to, create))
        {
        }

        private sealed class HelperPublisherEnumerable : IEnumerable<T>
        {
            private readonly HelperPublisherEnumerator _enumerator;

            public HelperPublisherEnumerable(int from, int to, Func<int, T> create)
            {
                _enumerator = new HelperPublisherEnumerator(from, to, create);
            }

            public IEnumerator<T> GetEnumerator() => _enumerator;

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class HelperPublisherEnumerator : IEnumerator<T>
        {
            private readonly int _to;
            private readonly Func<int, T> _create;
            private int _at;

            public HelperPublisherEnumerator(int from, int to, Func<int, T> create)
            {
                if(from > to)
                    throw new ArgumentException("from must be equal or greater than to!");

                _at = from;
                _to = to;
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
                    if (_at >= _to)
                        return false;

                    Current = _create(_at++);
                    return true;
                }
                catch (Exception ex)
                {
                    throw new IllegalStateException($"Failed to create element for id {_at - 1}!", ex);
                }
            }

            public void Reset()
            {
                throw new NotSupportedException();
            }

        }
    }
}
