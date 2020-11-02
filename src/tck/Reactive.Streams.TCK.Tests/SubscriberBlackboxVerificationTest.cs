/***************************************************
 * Licensed under MIT No Attribution (SPDX: MIT-0) *
 ***************************************************/
using System;
using NUnit.Framework;
using Reactive.Streams.TCK.Tests.Support;

namespace Reactive.Streams.TCK.Tests
{
    /// <summary>
    /// Validates that the TCK's <see cref="SubscriberBlackboxVerification{T}"/> fails with nice human readable errors.
    /// >Important: Please note that all Publishers implemented in this file are *wrong*!
    /// </summary>
    [TestFixture]
    public class SubscriberBlackboxVerificationTest : TCKVerificationSupport
    {
        [Test]
        public void Required_spec201_blackbox_mustSignalDemandViaSubscriptionRequest_shouldFailBy_notGettingRequestCall()
            => RequireTestFailure(
                () => NoopSubscriberVerification().Required_spec201_blackbox_mustSignalDemandViaSubscriptionRequest(),
                "Did not receive expected `Request` call within");

        [Test]
        public void Required_spec201_blackbox_mustSignalDemandViaSubscriptionRequest_shouldPass()
            => SimpleSubscriberVerification().Required_spec201_blackbox_mustSignalDemandViaSubscriptionRequest();

        [Test]
        public void Required_spec203_blackbox_mustNotCallMethodsOnSubscriptionOrPublisherInOnComplete_shouldFail_dueToCallingRequest()
        {
            ISubscription subscription = null;
            var subscriber = new LamdaSubscriber<int?>(onSubscribe: sub => subscription = sub,
                onComplete: () => subscription.Request(1));
            var verification = CustomSubscriberVerification(subscriber);
            RequireTestFailure(() => verification.Required_spec203_blackbox_mustNotCallMethodsOnSubscriptionOrPublisherInOnComplete(),
                "Subscription.Request MUST NOT be called from Subscriber.OnComplete (Rule 2.3)!");
        }

        [Test]
        public void Required_spec203_blackbox_mustNotCallMethodsOnSubscriptionOrPublisherInOnComplete_shouldFail_dueToCallingCancel()
        {
            ISubscription subscription = null;
            var subscriber = new LamdaSubscriber<int?>(onSubscribe: sub => subscription = sub,
                onComplete: () => subscription.Cancel());
            var verification = CustomSubscriberVerification(subscriber);
            RequireTestFailure(() => verification.Required_spec203_blackbox_mustNotCallMethodsOnSubscriptionOrPublisherInOnComplete(),
                "Subscription.Cancel MUST NOT be called from Subscriber.OnComplete (Rule 2.3)!");
        }

        [Test]
        public void Required_spec203_blackbox_mustNotCallMethodsOnSubscriptionOrPublisherInOnError_shouldFail_dueToCallingRequest()
        {
            ISubscription subscription = null;
            var subscriber = new LamdaSubscriber<int?>(onSubscribe: sub => subscription = sub,
                onError: _ => subscription.Request(1));
            var verification = CustomSubscriberVerification(subscriber);
            RequireTestFailure(() => verification.Required_spec203_blackbox_mustNotCallMethodsOnSubscriptionOrPublisherInOnError(),
                "Subscription.Request MUST NOT be called from Subscriber.OnError (Rule 2.3)!");
        }

        [Test]
        public void Required_spec203_blackbox_mustNotCallMethodsOnSubscriptionOrPublisherInOnError_shouldFail_dueToCallingCancel()
        {
            ISubscription subscription = null;
            var subscriber = new LamdaSubscriber<int?>(onSubscribe: sub => subscription = sub,
                onError: _ => subscription.Cancel());
            var verification = CustomSubscriberVerification(subscriber);
            RequireTestFailure(() => verification.Required_spec203_blackbox_mustNotCallMethodsOnSubscriptionOrPublisherInOnError(),
                "Subscription.Cancel MUST NOT be called from Subscriber.OnError (Rule 2.3)!");
        }

        [Test]
        public void Required_spec205_blackbox_mustCallSubscriptionCancelIfItAlreadyHasAnSubscriptionAndReceivesAnotherOnSubscribeSignal_shouldFail()
        {
            ISubscription subscription = null;
            var subscriber = new LamdaSubscriber<int?>(onSubscribe: sub =>
            {
                subscription = sub;
                sub.Request(1); // this is wrong, as one should always check if should accept or reject the subscription
            });
            var verification = CustomSubscriberVerification(subscriber);
            RequireTestFailure(() => verification.Required_spec205_blackbox_mustCallSubscriptionCancelIfItAlreadyHasAnSubscriptionAndReceivesAnotherOnSubscribeSignal(),
                "illegally called `Subscription.Request(1)`");
        }

        [Test]
        public void Required_spec209_blackbox_mustBePreparedToReceiveAnOnCompleteSignalWithPrecedingRequestCall_shouldFail()
            => RequireTestFailure(
                () => CustomSubscriberVerification(new LamdaSubscriber<int?>()).Required_spec209_blackbox_mustBePreparedToReceiveAnOnCompleteSignalWithPrecedingRequestCall(),
                "did not call `RegisterOnComplete()`");

        [Test]
        public void Required_spec209_blackbox_mustBePreparedToReceiveAnOnCompleteSignalWithoutPrecedingRequestCall_shouldPass_withNoopSubscriber() 
            => CustomSubscriberVerification(new LamdaSubscriber<int?>())
                    .Required_spec209_blackbox_mustBePreparedToReceiveAnOnCompleteSignalWithoutPrecedingRequestCall();

        [Test]
        public void Required_spec210_blackbox_mustBePreparedToReceiveAnOnErrorSignalWithPrecedingRequestCall_shouldFail()
        {
            var subscriber = new LamdaSubscriber<int?>(onError: cause =>
            {
                // this is wrong in many ways (incl. spec violation), but aims to simulate user code which "blows up" when handling the onError signal
                throw new Exception("Wrong, don't do this!", cause); // don't do this
            });
            var verification = CustomSubscriberVerification(subscriber);
            RequireTestFailure(() => verification.Required_spec210_blackbox_mustBePreparedToReceiveAnOnErrorSignalWithPrecedingRequestCall(),
                "Test Exception: Boom!"); // checks that the expected exception was delivered to onError, we don't expect anyone to implement onError so weirdly
        }

        [Test]
        public void Required_spec213_blackbox_mustThrowNullPointerExceptionWhenParametersAreNull_mustFailOnIgnoredNull_onSubscribe()
            => RequireTestFailure(
                () => CustomSubscriberVerification(new LamdaSubscriber<int?>()).Required_spec213_blackbox_onSubscribe_mustThrowNullPointerExceptionWhenParametersAreNull(),
                "OnSubscribe(null) did not throw ArgumentNullException");

        [Test]
        public void Required_spec213_blackbox_mustThrowNullPointerExceptionWhenParametersAreNull_mustFailOnIgnoredNull_onNext()
            => RequireTestFailure(
                () => CustomSubscriberVerification(new LamdaSubscriber<int?>()).Required_spec213_blackbox_onNext_mustThrowNullPointerExceptionWhenParametersAreNull(),
                "OnNext(null) did not throw ArgumentNullException");

        [Test]
        public void Required_spec213_blackbox_mustThrowNullPointerExceptionWhenParametersAreNull_mustIgnoreSpecForValueType_onNext()
            => RequireTestSkip(
                () => SimpleSubscriberVerification().Required_spec213_blackbox_onNext_mustThrowNullPointerExceptionWhenParametersAreNull(),
                "Can't verify behavior for value types");

        [Test]
        public void Required_spec213_blackbox_mustThrowNullPointerExceptionWhenParametersAreNull_mustFailOnIgnoredNull_onError()
            => RequireTestFailure(
                () => CustomSubscriberVerification(new LamdaSubscriber<int?>()).Required_spec213_blackbox_onError_mustThrowNullPointerExceptionWhenParametersAreNull(),
                "OnError(null) did not throw ArgumentNullException");


        // FAILING IMPLEMENTATIONS //

        /// <summary>
        /// Verification using a Subscriber that doesn't do anything on any of the callbacks
        /// </summary>
        private SubscriberBlackboxVerification<int> NoopSubscriberVerification()
            => new NoopBlackboxVerification(new TestEnvironment());

        [TestFixture(Ignore = "Helper verification for single test")]
        private sealed class NoopBlackboxVerification : SubscriberBlackboxVerification<int>
        {
            //Requirement for NUnit even if the tests are ignored
            public NoopBlackboxVerification() : base(new TestEnvironment())
            {

            }

            public NoopBlackboxVerification(TestEnvironment environment) : base(environment)
            {
            }

            public override int CreateElement(int element) => element;

            public override ISubscriber<int> CreateSubscriber() => new LamdaSubscriber<int>();
        }

        /// <summary>
        /// Verification using a Subscriber that only calls 'Requests(1)' on 'OnSubscribe' and 'OnNext'
        /// </summary>
        private SubscriberBlackboxVerification<int> SimpleSubscriberVerification()
            => new SimpleBlackboxVerification(new TestEnvironment());

        [TestFixture(Ignore = "Helper verification for single test")]
        private sealed class SimpleBlackboxVerification : SubscriberBlackboxVerification<int>
        {
            //Requirement for NUnit even if the tests are ignored
            public SimpleBlackboxVerification() : base(new TestEnvironment())
            {

            }

            public SimpleBlackboxVerification(TestEnvironment environment) : base(environment)
            {
            }

            public override int CreateElement(int element) => element;

            public override ISubscriber<int> CreateSubscriber()
            {
                ISubscription sub = null;
                return new LamdaSubscriber<int>(
                    onSubscribe: subscription =>
                    {
                        sub = subscription;
                        sub.Request(1);
                    },
                    onNext: _ => sub.Request(1));
            }
        }

        /// <summary>
        /// Custom Verification using given Subscriber
        /// </summary>
        private SubscriberBlackboxVerification<int?> CustomSubscriberVerification(ISubscriber<int?> subscriber)
            => new CustomBlackboxVerification(new TestEnvironment(), subscriber);

        [TestFixture(Ignore = "Helper verification for single test")]
        private sealed class CustomBlackboxVerification : SubscriberBlackboxVerification<int?>
        {
            private readonly ISubscriber<int?> _subscriber;

            //Requirement for NUnit even if the tests are ignored
            public CustomBlackboxVerification() : base(new TestEnvironment())
            {

            }

            public CustomBlackboxVerification(TestEnvironment environment, ISubscriber<int?> subscriber) : base(environment)
            {
                _subscriber = subscriber;
            }

            public override int? CreateElement(int element) => element;

            public override ISubscriber<int?> CreateSubscriber() => _subscriber;
        }
    }
}
