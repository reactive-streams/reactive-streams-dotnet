/***************************************************
 * Licensed under MIT No Attribution (SPDX: MIT-0) *
 ***************************************************/
using System.Linq;
using NUnit.Framework;
using Reactive.Streams.Example.Unicast;

namespace Reactive.Streams.TCK.Tests
{
    [TestFixture]
    public class EmptyLazyPublisherTest : PublisherVerification<int>
    {
        public EmptyLazyPublisherTest() : base(new TestEnvironment())
        {
            
        }

        public override IPublisher<int> CreatePublisher(long elements)
            => new AsyncIterablePublisher<int>(Enumerable.Empty<int>());

        public override IPublisher<int> CreateFailedPublisher() => null;

        public override long MaxElementsFromPublisher { get; } = 0;
    }
}
