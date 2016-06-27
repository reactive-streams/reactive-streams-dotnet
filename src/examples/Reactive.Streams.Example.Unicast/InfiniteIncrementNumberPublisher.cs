using System.Collections;
using System.Collections.Generic;

namespace Reactive.Streams.Example.Unicast
{
    public class InfiniteIncrementNumberPublisher : AsyncIterablePublisher<int?>
    {
        public InfiniteIncrementNumberPublisher() : base(new InfiniteEnumerable())
        {

        }

        private sealed class InfiniteEnumerable : IEnumerable<int?>
        {
            public IEnumerator<int?> GetEnumerator() => new InfiniteEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class InfiniteEnumerator : IEnumerator<int?>
        {
            private int _at;

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                _at++;
                return true;
            }

            public void Reset() => _at = 0;

            public int? Current => _at;

            object IEnumerator.Current => Current;
        }
    }
}
