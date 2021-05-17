/***************************************************
* Licensed under MIT No Attribution (SPDX: MIT-0) *
***************************************************/
using System;
using Xunit;
using Xunit.Abstractions;
using Reactive.Streams.TCK.Tests.Support;

namespace Reactive.Streams.TCK.Tests
{
    public class SyncTriggeredDemandSubscriberWhiteboxTest : SubscriberWhiteboxVerification<int?>
    {
        public SyncTriggeredDemandSubscriberWhiteboxTest(ITestOutputHelper output) : base(new TestEnvironment(output))
        {
        }

        public override int? CreateElement(int element) => element;

        public override ISubscriber<int?> CreateSubscriber(WhiteboxSubscriberProbe<int?> probe) => new Subscriber(probe);

        private sealed class Subscriber : SyncTriggeredDemandSubscriber<int?>
        {
            private readonly WhiteboxSubscriberProbe<int?> _probe;

            public Subscriber(WhiteboxSubscriberProbe<int?> probe)
            {
                _probe = probe;
            }

            public override void OnSubscribe(ISubscription subscription)
            {
                base.OnSubscribe(subscription);

                _probe.RegisterOnSubscribe(new Puppet(subscription));
            }

            private sealed class Puppet : ISubscriberPuppet
            {
                private readonly ISubscription _subscription;

                public Puppet(ISubscription subscription)
                {
                    _subscription = subscription;
                }

                public void TriggerRequest(long elements) => _subscription.Request(elements);

                public void SignalCancel() => _subscription.Cancel();
            }

            public override void OnNext(int? element)
            {
                base.OnNext(element);
                _probe.RegisterOnNext(element);
            }

            public override void OnError(Exception cause)
            {
                base.OnError(cause);
                _probe.RegisterOnError(cause);
            }

            public override void OnComplete()
            {
                base.OnComplete();
                _probe.RegisterOnComplete();
            }

            protected override long Foreach(int? element) => 1;
        }
    }
}
