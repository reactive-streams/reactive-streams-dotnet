/***************************************************
* Licensed under MIT No Attribution (SPDX: MIT-0) *
***************************************************/
using System;
using Xunit;
using Xunit.Abstractions;
using Reactive.Streams.TCK;

namespace Reactive.Streams.Example.Unicast.Tests
{
    public class SyncSubscriberTest : SubscriberBlackboxVerification<int?>
    {
        private readonly ITestOutputHelper _output;

        public SyncSubscriberTest(ITestOutputHelper output) : base(new TestEnvironment(output))
        {
            _output = output;
        }

        public override int? CreateElement(int element) => element;

        public override ISubscriber<int?> CreateSubscriber() => new Subscriber(_output);

        private sealed class Subscriber : SyncSubscriber<int?>
        {
            private readonly ITestOutputHelper _output;
            private long _acc;

            public Subscriber(ITestOutputHelper output)
            {
                _output = output;
            }

            protected override bool WhenNext(int? element)
            {
                _acc += element.Value;
                return true;
            }

            public override void OnComplete() => _output?.WriteLine("Accumulated: " + _acc);
        }
    }
}
