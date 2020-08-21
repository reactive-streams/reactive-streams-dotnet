using Xunit;
using Xunit.Abstractions;
using Reactive.Streams.TCK.Tests.Support;

namespace Reactive.Streams.TCK.Tests
{
    public class SyncTriggeredDemandSubscriberTest : SubscriberBlackboxVerification<int?>
    {
        public SyncTriggeredDemandSubscriberTest(ITestOutputHelper output) : base(new TestEnvironment(output))
        {
        }

        public override void TriggerRequest(ISubscriber<int?> subscriber)
            => ((SyncTriggeredDemandSubscriber<int?>) subscriber).TriggerDemand(1);
        

        public override ISubscriber<int?> CreateSubscriber() => new Subscriber();

        private sealed class Subscriber : SyncTriggeredDemandSubscriber<int?>
        {
            private long _acc;

            protected override long Foreach(int? element)
            {
                _acc += element.Value;
                return 1;
            }

            public override void OnComplete()
            {
            }
        }

        public override int? CreateElement(int element) => element;
    }
}
