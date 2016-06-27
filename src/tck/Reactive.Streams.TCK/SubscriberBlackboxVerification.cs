using System;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using Reactive.Streams.TCK.Support;

namespace Reactive.Streams.TCK
{
    /// <summary>
    /// Provides tests for verifying <see cref="ISubscriber{T}"/> and <see cref="ISubscription"/>
    /// specification rules, without any modifications to the tested implementation (also known as "Black Box" testing).
    /// 
    /// This verification is NOT able to check many of the rules of the spec, and if you want more
    /// verification of your implementation you'll have to implement <see cref="SubscriberWhiteboxVerification{T}"/>
    /// instead.
    /// </summary>
    public abstract class SubscriberBlackboxVerification<T> : WithHelperPublisher<T>,
        ISubscriberBlackboxVerificationRules
    {
        protected readonly TestEnvironment Environment;

        protected SubscriberBlackboxVerification(TestEnvironment environment)
        {
            Environment = environment;
        }

        // USER API

        /// <summary>
        /// This is the main method you must implement in your test incarnation.
        /// It must create a new <see cref="ISubscriber{T}"/> instance to be subjected to the testing logic.        
        /// </summary>
        public abstract ISubscriber<T> CreateSubscriber();

        /// <summary>
        /// Override this method if the Subscriber implementation you are verifying
        /// needs an external signal before it signals demand to its Publisher.
        /// 
        /// By default this method does nothing.
        /// </summary>
        public virtual void TriggerRequest(ISubscriber<T> subscriber)
        {
        }

        ////////////////////// TEST ENV CLEANUP /////////////////////////////////////

        [SetUp]
        public void Setup() => Environment.ClearAsyncErrors();

        ////////////////////// SPEC RULE VERIFICATION ///////////////////////////////

        // Verifies rule: https://github.com/reactive-streams/reactive-streams-jvm#2.1
        [Test]
        public void Required_spec201_blackbox_mustSignalDemandViaSubscriptionRequest()
            => BlackboxSubscriberTest(stage =>
            {
                TriggerRequest(stage.SubscriberProxy.Sub);
                var n = stage.ExpectRequest(); // assuming subscriber wants to consume elements...

                // should cope with up to requested number of elements
                for (var i = 0; i < n; i++)
                    stage.SignalNext();
            });

        // Verifies rule: https://github.com/reactive-streams/reactive-streams-jvm#2.2
        [Test]
        public void Untested_spec202_blackbox_shouldAsynchronouslyDispatch()
            => NotVerified(); // cannot be meaningfully tested, or can it?

        // Verifies rule: https://github.com/reactive-streams/reactive-streams-jvm#2.3
        [Test]
        public void Required_spec203_blackbox_mustNotCallMethodsOnSubscriptionOrPublisherInOnComplete()
            => BlackboxSubscriberWithoutSetupTest(stage =>
            {
                var subscriber = CreateSubscriber();
                var subscription = new Spec203Subscription(Environment, "OnComplete");
                subscriber.OnSubscribe(subscription);
                subscriber.OnComplete();

                Environment.VerifyNoAsyncErrorsNoDelay();
            });

        // Verifies rule: https://github.com/reactive-streams/reactive-streams-jvm#2.3
        [Test]
        public void Required_spec203_blackbox_mustNotCallMethodsOnSubscriptionOrPublisherInOnError()
            => BlackboxSubscriberWithoutSetupTest(stage =>
            {
                var subscriber = CreateSubscriber();
                var subscription = new Spec203Subscription(Environment, "OnError");
                subscriber.OnSubscribe(subscription);
                subscriber.OnError(new TestException());

                Environment.VerifyNoAsyncErrorsNoDelay();
            });

        private sealed class Spec203Subscription : ISubscription
        {
            private readonly TestEnvironment _environment;
            private readonly string _method;

            public Spec203Subscription(TestEnvironment environment, string method)
            {
                _environment = environment;
                _method = method;
            }

            public void Request(long n)
            {
                var stack = new StackTrace();
                var stackFrames = stack.GetFrames();
                if (stackFrames != null && stackFrames.Any(f => f.GetMethod().Name.Equals(_method)))
                    _environment.Flop($"Subscription.Request MUST NOT be called from Subscriber.{_method} (Rule 2.3)!" +
                                      $"Caller: {stack}");
            }

            public void Cancel()
            {
                var stack = new StackTrace();
                var stackFrames = stack.GetFrames();
                if (stackFrames != null && stackFrames.Any(f => f.GetMethod().Name.Equals(_method)))
                    _environment.Flop($"Subscription.Cancel MUST NOT be called from Subscriber.{_method} (Rule 2.3)!" +
                                      $"Caller: {stack}");
            }
        }

        // Verifies rule: https://github.com/reactive-streams/reactive-streams-jvm#2.4
        [Test]
        public void Untested_spec204_blackbox_mustConsiderTheSubscriptionAsCancelledInAfterRecievingOnCompleteOrOnError()
            => NotVerified(); // cannot be meaningfully tested, or can it?

        // Verifies rule: https://github.com/reactive-streams/reactive-streams-jvm#2.5
        [Test]
        public void
            Required_spec205_blackbox_mustCallSubscriptionCancelIfItAlreadyHasAnSubscriptionAndReceivesAnotherOnSubscribeSignal
            ()
        {
            var stage = new BlackBoxTestStage<T>(Environment, this);
            // try to subscribe another time, if the subscriber calls `probe.RegisterOnSubscribe` the test will fail
            var secondSubscriptionCancelled = new Latch(Environment);
            stage.Sub.OnSubscribe(new Spec205Subscription(Environment, secondSubscriptionCancelled, stage.Sub));
            secondSubscriptionCancelled.ExpectClose(
                "Expected SecondSubscription given to subscriber to be cancelled, but `Subscription.cancel()` was not called.");
            Environment.VerifyNoAsyncErrorsNoDelay();
        }

        private sealed class Spec205Subscription : ISubscription
        {
            private readonly TestEnvironment _environment;
            private readonly Latch _secondSubscriptionCancelled;
            private readonly ISubscriber<T> _subscriber;

            public Spec205Subscription(TestEnvironment environment, Latch secondSubscriptionCancelled,
                ISubscriber<T> subscriber)
            {
                _environment = environment;
                _secondSubscriptionCancelled = secondSubscriptionCancelled;
                _subscriber = subscriber;
            }

            public void Request(long n)
                => _environment.Flop($"Subscriber {_subscriber} illegally called `Subscription.Request({n})`!");

            public void Cancel() => _secondSubscriptionCancelled.Close();

            public override string ToString() => "SecondSubscription(should get cancelled)";
        }

        // Verifies rule: https://github.com/reactive-streams/reactive-streams-jvm#2.6
        [Test]
        public void Untested_spec206_blackbox_mustCallSubscriptionCancelIfItIsNoLongerValid()
            => NotVerified(); // cannot be meaningfully tested, or can it?

        // Verifies rule: https://github.com/reactive-streams/reactive-streams-jvm#2.7
        [Test]
        public void
            Untested_spec207_blackbox_mustEnsureAllCallsOnItsSubscriptionTakePlaceFromTheSameThreadOrTakeCareOfSynchronization
            ()
            => NotVerified(); // cannot be meaningfully tested, or can it?
        // the same thread part of the clause can be verified but that is not very useful, or is it?

        // Verifies rule: https://github.com/reactive-streams/reactive-streams-jvm#2.8
        [Test]
        public void Untested_spec208_blackbox_mustBePreparedToReceiveOnNextSignalsAfterHavingCalledSubscriptionCancel()
            => NotVerified(); // cannot be meaningfully tested, or can it?

        // Verifies rule: https://github.com/reactive-streams/reactive-streams-jvm#2.9
        [Test]
        public void Required_spec209_blackbox_mustBePreparedToReceiveAnOnCompleteSignalWithPrecedingRequestCall()
            => BlackboxSubscriberWithoutSetupTest(stage =>
            {
                var publisher = new Spec209WithPublisher();
                var subscriber = CreateSubscriber();
                var probe = stage.CreateBlackboxSubscriberProxy(Environment, subscriber);

                publisher.Subscribe(probe);
                TriggerRequest(subscriber);
                probe.ExpectCompletion();
                probe.ExpectNone();

                Environment.VerifyNoAsyncErrorsNoDelay();
            });

        private sealed class Spec209WithPublisher : IPublisher<T>
        {
            private sealed class Subscription : ISubscription
            {
                private readonly Spec209WithPublisher _publisher;
                private bool _completed;

                public Subscription(Spec209WithPublisher publisher)
                {
                    _publisher = publisher;
                }

                public void Request(long n)
                {
                    if (!_completed)
                    {
                        _completed = true;
                        _publisher._subscriber.OnComplete();
                            // Publisher now realises that it is in fact already completed
                    }
                }

                public void Cancel()
                {
                    // noop, ignore
                }
            }

            private ISubscriber<T> _subscriber;

            public void Subscribe(ISubscriber<T> subscriber)
            {
                _subscriber = subscriber;
                subscriber.OnSubscribe(new Subscription(this));
            }
        }

        // Verifies rule: https://github.com/reactive-streams/reactive-streams-jvm#2.9
        [Test]
        public void Required_spec209_blackbox_mustBePreparedToReceiveAnOnCompleteSignalWithoutPrecedingRequestCall()
            => BlackboxSubscriberWithoutSetupTest(stage =>
            {
                var publisher = new Spec209WithoutPublisher();
                var subscriber = CreateSubscriber();
                var probe = stage.CreateBlackboxSubscriberProxy(Environment, subscriber);

                publisher.Subscribe(probe);
                probe.ExpectCompletion();

                Environment.VerifyNoAsyncErrorsNoDelay();
            });

        private sealed class Spec209WithoutPublisher : IPublisher<T>
        {
            public void Subscribe(ISubscriber<T> subscriber) => subscriber.OnComplete();
        }

        // Verifies rule: https://github.com/reactive-streams/reactive-streams-jvm#2.10
        [Test]
        public void Required_spec210_blackbox_mustBePreparedToReceiveAnOnErrorSignalWithPrecedingRequestCall()
            => BlackboxSubscriberTest(stage =>
            {
                stage.Sub.OnError(new TestException());
                stage.SubscriberProxy.ExpectError<TestException>();
            });

        // Verifies rule: https://github.com/reactive-streams/reactive-streams-jvm#2.11
        [Test]
        public void
            Untested_spec211_blackbox_mustMakeSureThatAllCallsOnItsMethodsHappenBeforeTheProcessingOfTheRespectiveEvents
            ()
            => NotVerified(); // cannot be meaningfully tested, or can it?

        // Verifies rule: https://github.com/reactive-streams/reactive-streams-jvm#2.12
        [Test]
        public void Untested_spec212_blackbox_mustNotCallOnSubscribeMoreThanOnceBasedOnObjectEquality()
            => NotVerified(); // cannot be meaningfully tested, or can it?

        // Verifies rule: https://github.com/reactive-streams/reactive-streams-jvm#2.13
        [Test]
        public void Untested_spec213_blackbox_failingOnSignalInvocation()
            => NotVerified(); // cannot be meaningfully tested, or can it?

        // Verifies rule: https://github.com/reactive-streams/reactive-streams-jvm#2.13
        [Test]
        public void Required_spec213_blackbox_onSubscribe_mustThrowNullPointerExceptionWhenParametersAreNull()
            => BlackboxSubscriberWithoutSetupTest(stage =>
            {
                var subscriber = CreateSubscriber();
                var gotNpe = false;
                subscriber.OnSubscribe(new Spec213DummySubscription());

                try
                {
                    subscriber.OnSubscribe(null);
                }
                catch (ArgumentNullException)
                {
                    gotNpe = true;
                }

                Assert.True(gotNpe, "OnSubscribe(null) did not throw ArgumentNullException");
                Environment.VerifyNoAsyncErrorsNoDelay();
            });

        // Verifies rule: https://github.com/reactive-streams/reactive-streams-jvm#2.13
        [Test]
        public void Required_spec213_blackbox_onNext_mustThrowNullPointerExceptionWhenParametersAreNull()
            => BlackboxSubscriberWithoutSetupTest(stage =>
            {
                var subscriber = CreateSubscriber();
                var gotNpe = false;
                subscriber.OnSubscribe(new Spec213DummySubscription());

                try
                {
                    // we can't use null here because we can't enforce a constsraint which supports Nullable<T>
                    // default(T) will return null for all reference types as well as Nullable<T>
                    subscriber.OnNext(default(T));
                }
                catch (ArgumentNullException)
                {
                    gotNpe = true;
                }

                Assert.True(gotNpe, "OnNext(null) did not throw ArgumentNullException");
                Environment.VerifyNoAsyncErrorsNoDelay();
            });

        // Verifies rule: https://github.com/reactive-streams/reactive-streams-jvm#2.13
        [Test]
        public void Required_spec213_blackbox_onError_mustThrowNullPointerExceptionWhenParametersAreNull()
            => BlackboxSubscriberWithoutSetupTest(stage =>
            {
                var subscriber = CreateSubscriber();
                var gotNpe = false;
                subscriber.OnSubscribe(new Spec213DummySubscription());

                try
                {
                    subscriber.OnError(null);
                }
                catch (ArgumentNullException)
                {
                    gotNpe = true;
                }

                Assert.True(gotNpe, "OnError(null) did not throw ArgumentNullException");
                Environment.VerifyNoAsyncErrorsNoDelay();
            });

        private sealed class Spec213DummySubscription : ISubscription
        {
            public void Request(long n)
            {

            }

            public void Cancel()
            {
            }
        }

        ////////////////////// SUBSCRIPTION SPEC RULE VERIFICATION //////////////////

        // Verifies rule: https://github.com/reactive-streams/reactive-streams-jvm#3.1
        [Test]
        public void Untested_spec301_blackbox_mustNotBeCalledOutsideSubscriberContext()
            => NotVerified(); // cannot be meaningfully tested, or can it?

        // Verifies rule: https://github.com/reactive-streams/reactive-streams-jvm#3.8
        [Test]
        public void Untested_spec308_blackbox_requestMustRegisterGivenNumberElementsToBeProduced()
            => NotVerified(); // cannot be meaningfully tested, or can it?

        // Verifies rule: https://github.com/reactive-streams/reactive-streams-jvm#3.10
        [Test]
        public void Untested_spec310_blackbox_requestMaySynchronouslyCallOnNextOnSubscriber()
            => NotVerified(); // cannot be meaningfully tested, or can it?

        // Verifies rule: https://github.com/reactive-streams/reactive-streams-jvm#3.11
        [Test]
        public void Untested_spec311_blackbox_requestMaySynchronouslyCallOnCompleteOrOnError()
            => NotVerified(); // cannot be meaningfully tested, or can it?

        // Verifies rule: https://github.com/reactive-streams/reactive-streams-jvm#3.14
        [Test]
        public void Untested_spec314_blackbox_cancelMayCauseThePublisherToShutdownIfNoOtherSubscriptionExists()
            => NotVerified(); // cannot be meaningfully tested, or can it?

        // Verifies rule: https://github.com/reactive-streams/reactive-streams-jvm#3.15
        [Test]
        public void Untested_spec315_blackbox_cancelMustNotThrowExceptionAndMustSignalOnError()
            => NotVerified(); // cannot be meaningfully tested, or can it?

        // Verifies rule: https://github.com/reactive-streams/reactive-streams-jvm#3.16
        [Test]
        public void Untested_spec316_blackbox_requestMustNotThrowExceptionAndMustOnErrorTheSubscriber()
            => NotVerified(); // cannot be meaningfully tested, or can it?

        /////////////////////// ADDITIONAL "COROLLARY" TESTS ////////////////////////

        /////////////////////// TEST INFRASTRUCTURE /////////////////////////////////

        public void BlackboxSubscriberTest(Action<BlackBoxTestStage<T>> body)
            => body(new BlackBoxTestStage<T>(Environment, this));

        public void BlackboxSubscriberWithoutSetupTest(Action<BlackBoxTestStage<T>> body)
            => body(new BlackBoxTestStage<T>(Environment, this, false));

        public void NotVerified() => NotVerified("Not verified using this TCK.");

        public void NotVerified(string message) => Assert.Ignore(message);
    }

    public class BlackBoxTestStage<T> : ManualPublisher<T>
    {
        private readonly WithHelperPublisher<T> _verification;
        
        public BlackBoxTestStage(TestEnvironment environment, SubscriberBlackboxVerification<T> verification, bool runDefaultInit = true) : base(environment)
        {
            _verification = verification;
            if (runDefaultInit)
            {
                Publisher = CreateHelperPublisher(long.MaxValue);
                Tees = Environment.NewManualSubscriber(Publisher);
                var subscriber = verification.CreateSubscriber();
                SubscriberProxy = CreateBlackboxSubscriberProxy(Environment, subscriber);
                Subscribe(SubscriberProxy);
            }
        }

        public IPublisher<T> Publisher { get; set; }

        public ManualSubscriber<T> Tees { get; set; } // gives us access to a stream T values
        
        public T LastT { get; private set; }

        /// <summary>
        /// Proxy for the <see cref="Sub"/> Subscriber, providing certain assertions on methods being called on the Subscriber.
        /// </summary>
        public BlackboxSubscriberProxy<T> SubscriberProxy { get; set; }

        public ISubscriber<T> Sub => Subscriber.Value;

        public IPublisher<T> CreateHelperPublisher(long elements) => _verification.CreateHelperPublisher(elements);

        public BlackboxSubscriberProxy<T> CreateBlackboxSubscriberProxy(TestEnvironment environment, ISubscriber<T> subscriber)
            => new BlackboxSubscriberProxy<T>(environment, subscriber);

        public T SignalNext()
        {
            var element = NextT();
            SendNext(element);
            return element;
        }

        public T NextT()
        {
            LastT = Tees.RequestNextElement();
            return LastT;
        }
    }
}
