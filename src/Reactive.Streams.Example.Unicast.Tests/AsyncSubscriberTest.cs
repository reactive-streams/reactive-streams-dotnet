using System;
using System.Threading;
using NUnit.Framework;
using Reactive.Streams.TCK;
using Reactive.Streams.TCK.Support;

namespace Reactive.Streams.Example.Unicast.Tests
{
    [TestFixture]
    public class AsyncSubscriberTest : SubscriberBlackboxVerification<int?>
    {
        public AsyncSubscriberTest() : base(new TestEnvironment())
        {
        }

        public override int? CreateElement(int element) => element;

        public override ISubscriber<int?> CreateSubscriber() => new Suscriber();

        private sealed class Suscriber : AsyncSubscriber<int?>
        {
            protected override bool WhenNext(int? element) => true;
        }

        [Test]
        public void TestAccumulation()
        {
            var i = new AtomicCounterLong(0);
            var latch = new CountdownEvent(1);
            var subscriber = new AccSubscriber(i, latch);
            new NumberIterablePublisher(0,10).Subscribe(subscriber);
            latch.Wait(TimeSpan.FromMilliseconds(Environment.DefaultTimeoutMilliseconds*10));
            Assert.AreEqual(45, i.Current);
        }

        private sealed class AccSubscriber : AsyncSubscriber<int?>
        {
            private readonly AtomicCounterLong _counter;
            private readonly CountdownEvent _latch;
            private long _acc;

            public AccSubscriber(AtomicCounterLong counter, CountdownEvent latch)
            {
                _counter = counter;
                _latch = latch;
            }

            protected override bool WhenNext(int? element)
            {
                // no need for null check, OnNext handles this case
                _acc += element.Value;
                return true;
            }

            protected override void WhenComplete()
            {
                _counter.GetAndAdd(_acc);
                _latch.Signal();
            }
        }
    }
}
