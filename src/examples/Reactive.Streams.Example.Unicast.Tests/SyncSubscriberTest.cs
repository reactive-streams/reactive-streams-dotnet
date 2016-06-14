using System;
using NUnit.Framework;
using Reactive.Streams.TCK;

namespace Reactive.Streams.Example.Unicast.Tests
{
    [TestFixture]
    public class SyncSubscriberTest : SubscriberBlackboxVerification<int?>
    {
        public SyncSubscriberTest() : base(new TestEnvironment())
        {
        }

        public override int? CreateElement(int element) => element;

        public override ISubscriber<int?> CreateSubscriber() => new Subscriber();

        private sealed class Subscriber : SyncSubscriber<int?>
        {
            private long _acc;

            protected override bool WhenNext(int? element)
            {
                _acc += element.Value;
                return true;
            }

            public override void OnComplete() => Console.WriteLine("Accumulated: " + _acc);
        }
    }
}
