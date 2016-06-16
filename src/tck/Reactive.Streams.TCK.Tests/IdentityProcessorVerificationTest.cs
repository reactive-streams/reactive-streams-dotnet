using System;
using NUnit.Framework;
using Reactive.Streams.TCK.Tests.Support;

namespace Reactive.Streams.TCK.Tests
{
    public class IdentityProcessorVerificationTest : TCKVerificationSupport
    {
        private static readonly long DefaultTimeoutMilliseconds =
            TestEnvironment.EnvironmentDefaultTimeoutMilliseconds();
        private static readonly long DefaultNoSignalsTimeoutMilliseconds =
            TestEnvironment.EnvironmentDefaultNoSignalsTimeoutMilliseconds();

        [Test]
        public void Required_spec104_mustCallOnErrorOnAllItsSubscribersIfItEncountersANonRecoverableError_shouldBeIgnored()
        {
            RequireTestSkip(() =>
            {
                new Spec104IgnoreVerification(NewTestEnvironment())
                    .Required_spec104_mustCallOnErrorOnAllItsSubscribersIfItEncountersANonRecoverableError();
            }, "The Publisher under test only supports 1 subscribers, while this test requires at least 2 to run");
        }

        [TestFixture(Ignore = "Helper verification for single test")]
        private sealed class Spec104WaitingVerification : IdentityProcessorVerification<int>
        {
            /// <summary>
            /// We need this constructor for NUnit even if the fixture is ignored 
            /// </summary>
            public Spec104WaitingVerification() : base(new TestEnvironment())
            {

            }

            private sealed class Processor : IProcessor<int, int>
            {
                private sealed class Subscription : ISubscription
                {
                    private readonly ISubscriber<int> _subscriber;

                    public Subscription(ISubscriber<int> subscriber)
                    {
                        _subscriber = subscriber;
                    }

                    public void Request(long n) => _subscriber.OnNext(0);

                    public void Cancel()
                    {
                    }
                }

                public void OnNext(int element)
                {
                    // noop
                }

                public void OnSubscribe(ISubscription subscription) => subscription.Request(1);

                public void OnError(Exception cause)
                {
                    // noop
                }

                public void OnComplete()
                {
                    // noop
                }

                public void Subscribe(ISubscriber<int> subscriber)
                    => subscriber.OnSubscribe(new Subscription(subscriber));
            }

            private sealed class Publisher : IPublisher<int>
            {
                public void Subscribe(ISubscriber<int> subscriber)
                {
                    subscriber.OnSubscribe(new LamdaSubscription(onRequest: _ =>
                    {
                        for (var i = 0; i < 10; i++)
                            subscriber.OnNext(i);
                    }));
                }
            }

            public Spec104WaitingVerification(TestEnvironment environment, long publisherReferenceGcTimeoutMillis)
                : base(environment, publisherReferenceGcTimeoutMillis)
            {
            }


            public override int CreateElement(int element) => element;

            public override IProcessor<int, int> CreateIdentityProcessor(int bufferSize) => new Processor();

            public override IPublisher<int> CreateHelperPublisher(long elements) => new Publisher();

            public override IPublisher<int> CreateFailedPublisher() => null;
        }

        [Test]
        public void Required_spec104_mustCallOnErrorOnAllItsSubscribersIfItEncountersANonRecoverableError_shouldFailWhileWaitingForOnError()
        {
            RequireTestFailure(() =>
            {
                new Spec104WaitingVerification(NewTestEnvironment(), DefaultTimeoutMilliseconds)
                    .Required_spec104_mustCallOnErrorOnAllItsSubscribersIfItEncountersANonRecoverableError();
            }, "Did not receive expected error on downstream within " + DefaultTimeoutMilliseconds);
        }

        [TestFixture(Ignore = "Helper verification for single test")]
        private sealed class Spec104IgnoreVerification : IdentityProcessorVerification<int>
        {
            /// <summary>
            /// We need this constructor for NUnit even if the fixture is ignored 
            /// </summary>
            public Spec104IgnoreVerification() : base(new TestEnvironment())
            {

            }

            public Spec104IgnoreVerification(TestEnvironment environment) : base(environment)
            {
            }

            public override int CreateElement(int element) => element;

            public override IProcessor<int, int> CreateIdentityProcessor(int bufferSize) => new NoopProcessor();

            public override IPublisher<int> CreateFailedPublisher() => null;

            public override IPublisher<int> CreateHelperPublisher(long elements) => null;

            // can only support 1 subscribe => unable to run this test
            public override long MaxSupportedSubscribers { get; } = 1;
        }

        private static TestEnvironment NewTestEnvironment()
            => new TestEnvironment(DefaultTimeoutMilliseconds, DefaultNoSignalsTimeoutMilliseconds);


        // FAILING IMPLEMENTATIONS //

        private sealed class NoopProcessor : IProcessor<int, int>
        {
            public void OnNext(int element)
            {
                // noop
            }

            public void OnSubscribe(ISubscription subscription)
            {
                // noop
            }

            public void OnError(Exception cause)
            {
                // noop
            }

            public void OnComplete()
            {
                // noop
            }

            public void Subscribe(ISubscriber<int> subscriber)
            {
                // noop
            }
        }
    }
}
