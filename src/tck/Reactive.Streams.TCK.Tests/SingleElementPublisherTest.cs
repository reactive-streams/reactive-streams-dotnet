using System.Linq;
using Xunit;
using Xunit.Abstractions;
using Reactive.Streams.Example.Unicast;

namespace Reactive.Streams.TCK.Tests
{
    public class SingleElementPublisherTest : PublisherVerification<int>
    {
        public SingleElementPublisherTest() : base(new TestEnvironment())
        {
            
        }

        public override IPublisher<int> CreatePublisher(long elements)
            => new AsyncIterablePublisher<int>(Enumerable.Repeat(1, 1));

        public override IPublisher<int> CreateFailedPublisher() => null;

        public override long MaxElementsFromPublisher { get; } = 1;
    }
}
