using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Reactive.Streams.TCK;

namespace Reactive.Streams.Example.Unicast.Tests
{
    [TestFixture]
    public class UnboundedIntegerIncrementPublisherTest : PublisherVerification<int?>
    {
        public UnboundedIntegerIncrementPublisherTest() : base(new TestEnvironment())
        {
        }

        public override IPublisher<int?> CreatePublisher(long elements) => new InfiniteIncrementNumberPublisher();


        public override IPublisher<int?> CreateFailedPublisher() => new FailedPublisher();

        private sealed class FailedPublisher : AsyncIterablePublisher<int?>
        {
            public FailedPublisher() : base(new FailedEnumerable())
            {
            }

            private sealed class FailedEnumerable : IEnumerable<int?>
            {
                public IEnumerator<int?> GetEnumerator()
                {
                    throw new Exception("Error state signal!");
                }

                IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            }
        }

        public override long MaxElementsFromPublisher => PublisherUnableToSignalOnComplete;
    }
}
