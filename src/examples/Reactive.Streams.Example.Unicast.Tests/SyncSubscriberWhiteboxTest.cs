using System;
using NUnit.Framework;
using Reactive.Streams.TCK;

namespace Reactive.Streams.Example.Unicast.Tests
{
    [TestFixture]
    public class ValueTypeSyncSubscriberWhiteboxTest : SubscriberWhiteboxVerification<int>
    {
        public ValueTypeSyncSubscriberWhiteboxTest() : base(new TestEnvironment())
        {
        }

        public override int CreateElement(int element) => element;

        public override ISubscriber<int> CreateSubscriber(WhiteboxSubscriberProbe<int> probe) => new Subscriber(probe);

        private sealed class Subscriber : SyncSubscriber<int>
        {
            private readonly WhiteboxSubscriberProbe<int> _probe;

            public Subscriber(WhiteboxSubscriberProbe<int> probe)
            {
                _probe = probe;
            }

            public override void OnSubscribe(ISubscription subscription)
            {
                base.OnSubscribe(subscription);

                _probe.RegisterOnSubscribe(new SubscriberPuppet(subscription));
            }

            private sealed class SubscriberPuppet : ISubscriberPuppet
            {
                private readonly ISubscription _subscription;

                public SubscriberPuppet(ISubscription subscription)
                {
                    _subscription = subscription;
                }

                public void TriggerRequest(long elements) => _subscription.Request(elements);

                public void SignalCancel() => _subscription.Cancel();
            }

            public override void OnNext(int element)
            {
                base.OnNext(element);
                _probe.RegisterOnNext(element);
            }

            protected override bool WhenNext(int element) => true;

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
        }
    }

    [TestFixture]
    public class NullableSyncSubscriberWhiteboxTest : SubscriberWhiteboxVerification<int?>
    {
        public NullableSyncSubscriberWhiteboxTest() : base(new TestEnvironment())
        {
        }

        public override int? CreateElement(int element) => element;

        public override ISubscriber<int?> CreateSubscriber(WhiteboxSubscriberProbe<int?> probe) => new Subscriber(probe);

        private sealed class Subscriber : SyncSubscriber<int?>
        {
            private readonly WhiteboxSubscriberProbe<int?> _probe;

            public Subscriber(WhiteboxSubscriberProbe<int?> probe)
            {
                _probe = probe;
            }

            public override void OnSubscribe(ISubscription subscription)
            {
                base.OnSubscribe(subscription);

                _probe.RegisterOnSubscribe(new SubscriberPuppet(subscription));
            }

            private sealed class SubscriberPuppet : ISubscriberPuppet
            {
                private readonly ISubscription _subscription;

                public SubscriberPuppet(ISubscription subscription)
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

            protected override bool WhenNext(int? element) => true;

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
        }
    }
}
