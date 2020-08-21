using System.Linq;
using Xunit;
using Xunit.Abstractions;
using Reactive.Streams.Example.Unicast;

namespace Reactive.Streams.TCK.Tests
{
    public class EmptyLazyPublisherTest : PublisherVerification<int>
    {
        public EmptyLazyPublisherTest(ITestOutputHelper output) : base(new TestEnvironment(output))
        {
            
        }

        public override IPublisher<int> CreatePublisher(long elements)
            => new AsyncIterablePublisher<int>(Enumerable.Empty<int>());

        public override IPublisher<int> CreateFailedPublisher() => null;

        public override long MaxElementsFromPublisher { get; } = 0;
    }
}
