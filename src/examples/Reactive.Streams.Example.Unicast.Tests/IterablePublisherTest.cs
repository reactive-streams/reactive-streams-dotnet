using System;
using System.Collections;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;
using Reactive.Streams.TCK;

namespace Reactive.Streams.Example.Unicast.Tests
{
    [TestFixture]
    public class IterablePublisherTest : PublisherVerification<int?>
    {
        public IterablePublisherTest() : base(new TestEnvironment())
        {
        }

        public override IPublisher<int?> CreatePublisher(long elements)
        {
            Assert.True(elements <= MaxElementsFromPublisher);
            //Assert.LessOrEqual(elements, MaxElementsFromPublisher);
            return new NumberIterablePublisher(0, (int)elements);
        }

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

        public override long MaxElementsFromPublisher { get; } = int.MaxValue;
    }
}
