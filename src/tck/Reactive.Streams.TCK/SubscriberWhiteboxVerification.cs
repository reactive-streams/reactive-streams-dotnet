using System;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using Reactive.Streams.TCK.Support;

namespace Reactive.Streams.TCK
{
    /// <summary>
    /// Provides tests for verifying <see cref="ISubscriber{T}"/> and <see cref="ISubscription"/> specification rules.
    /// </summary>
    public abstract class SubscriberWhiteboxVerification<T> : WithHelperPublisher<T>,
        ISubscriberWhiteboxVerificationRules
    {
        private readonly TestEnvironment _environment;

        protected SubscriberWhiteboxVerification(TestEnvironment environment)
        {
            _environment = environment;
        }

        // USER API

        /// <summary>
        /// This is the main method you must implement in your test incarnation.
        /// It must create a new <see cref="ISubscriber{T}"/> instance to be subjected to the testing logic.
        /// 
        /// In order to be meaningfully testable your Subscriber must inform the given
        /// `WhiteboxSubscriberProbe` of the respective events having been received.
        /// </summary>
        public abstract ISubscriber<T> CreateSubscriber(WhiteboxSubscriberProbe<T> probe);

        ////////////////////// TEST ENV CLEANUP /////////////////////////////////////

        [SetUp]
        public void SetUp() => _environment.ClearAsyncErrors();


        ////////////////////// TEST SETUP VERIFICATION //////////////////////////////
        
        [Test]
        public void Required_exerciseWhiteboxHappyPath()
            => SubscriberTest(stage =>
            {
                stage.Puppet.TriggerRequest(1);
                stage.Puppet.TriggerRequest(1);

                var receivedRequests = stage.ExpectRequest();

                stage.SignalNext();
                stage.Probe.ExpectNext(stage.LastT);

                stage.Puppet.TriggerRequest(1);
                if (receivedRequests == 1)
                    stage.ExpectRequest();

                stage.SignalNext();
                stage.Probe.ExpectNext(stage.LastT);

                stage.Puppet.SignalCancel();
                stage.ExpectCancelling();

                stage.VerifyNoAsyncErrors();
            });

        ////////////////////// SPEC RULE VERIFICATION ///////////////////////////////

        // Verifies rule: https://github.com/reactive-streams/reactive-streams-jvm#2.1
        [Test]
        public void Required_spec201_mustSignalDemandViaSubscriptionRequest()
            => SubscriberTest(stage =>
            {
                stage.Puppet.TriggerRequest(1);
                stage.ExpectRequest();

                stage.SignalNext();
            });

        // Verifies rule: https://github.com/reactive-streams/reactive-streams-jvm#2.2
        [Test]
        public void Untested_spec202_shouldAsynchronouslyDispatch()
            => NotVerified();  // cannot be meaningfully tested, or can it?

        // Verifies rule: https://github.com/reactive-streams/reactive-streams-jvm#2.3
        [Test]
        public void Required_spec203_mustNotCallMethodsOnSubscriptionOrPublisherInOnComplete()
            => SubscriberTestWithoutSetup(stage =>
            {
                var subscription = new Spec203OnCompleteSubscription(this);
                stage.Probe = stage.CreateWhiteboxSubscriberProbe(_environment);
                var subscriber = CreateSubscriber(stage.Probe);

                subscriber.OnSubscribe(subscription);
                subscriber.OnComplete();

                _environment.VerifyNoAsyncErrorsNoDelay();
            });

        private sealed class Spec203OnCompleteSubscription : ISubscription
        {
            private readonly TestEnvironment _environment;

            public Spec203OnCompleteSubscription(SubscriberWhiteboxVerification<T> verification)
            {
                _environment = verification._environment;
            }

            public void Request(long n)
            {
                var stack = new StackTrace();
                var stackFrames = stack.GetFrames();
                if (stackFrames != null && stackFrames.Any(f => f.GetMethod().Name.Equals("OnComplete")))
                    _environment.Flop(
                        "Subscription.Request MUST NOT be called from Subscriber.OnComplete (Rule 2.3)!" +
                        $"Caller: {stack}");
            }

            public void Cancel()
            {
                var stack = new StackTrace();
                var stackFrames = stack.GetFrames();
                if (stackFrames != null && stackFrames.Any(f => f.GetMethod().Name.Equals("OnComplete")))
                    _environment.Flop(
                        "Subscription.Cancel MUST NOT be called from Subscriber.OnComplete (Rule 2.3)!" +
                        $"Caller: {stack}");

            }
        }

        // Verifies rule: https://github.com/reactive-streams/reactive-streams-jvm#2.3
        [Test]
        public void Required_spec203_mustNotCallMethodsOnSubscriptionOrPublisherInOnError()
            => SubscriberTestWithoutSetup(stage =>
            {
                var subscription = new Spec203OnErrorSubscription(this);
                stage.Probe = stage.CreateWhiteboxSubscriberProbe(_environment);
                var subscriber = CreateSubscriber(stage.Probe);

                subscriber.OnSubscribe(subscription);
                subscriber.OnError(new TestException());

                _environment.VerifyNoAsyncErrorsNoDelay();
            });

        private sealed class Spec203OnErrorSubscription : ISubscription
        {
            private readonly TestEnvironment _environment;

            public Spec203OnErrorSubscription(SubscriberWhiteboxVerification<T> verification)
            {
                _environment = verification._environment;
            }

            public void Request(long n)
            {
                var stack = new StackTrace();
                var stackFrames = stack.GetFrames();
                if (stackFrames != null && stackFrames.Any(f => f.GetMethod().Name.Equals("OnError")))
                    _environment.Flop(
                        "Subscription.Request MUST NOT be called from Subscriber.OnError (Rule 2.3)!" +
                        $"Caller: {stack}");
            }

            public void Cancel()
            {
                var stack = new StackTrace();
                var stackFrames = stack.GetFrames();
                if (stackFrames != null && stackFrames.Any(f => f.GetMethod().Name.Equals("OnError")))
                    _environment.Flop(
                        "Subscription.Cancel MUST NOT be called from Subscriber.OnError (Rule 2.3)!" +
                        $"Caller: {stack}");
            }
        }
        
        // Verifies rule: https://github.com/reactive-streams/reactive-streams-jvm#2.4
        [Test]
        public void Untested_spec204_mustConsiderTheSubscriptionAsCancelledInAfterRecievingOnCompleteOrOnError()
            => NotVerified();  // cannot be meaningfully tested, or can it?

        // Verifies rule: https://github.com/reactive-streams/reactive-streams-jvm#2.5
        [Test]
        public void Required_spec205_mustCallSubscriptionCancelIfItAlreadyHasAnSubscriptionAndReceivesAnotherOnSubscribeSignal()
            => SubscriberTest(stage =>
            {
                // try to subscribe another time, if the subscriber calls `probe.RegisterOnSubscribe` the test will fail
                var secondSubscriptionCancelled = new Latch(_environment);
                var subscriber = stage.Sub;
                var subscription = new Spec205Subscription(secondSubscriptionCancelled);

                subscriber.OnSubscribe(subscription);

                secondSubscriptionCancelled.ExpectClose(
                    "Expected 2nd Subscription given to subscriber to be cancelled, but `Subscription.cancel()` was not called");
                _environment.VerifyNoAsyncErrors();
            });

        private sealed class Spec205Subscription : ISubscription
        {
            private readonly Latch _secondSubscriptionCancelled;

            public Spec205Subscription(Latch secondSubscriptionCancelled)
            {
                _secondSubscriptionCancelled = secondSubscriptionCancelled;
            }

            public void Request(long n)
            {
                //ignore
            }

            public void Cancel() => _secondSubscriptionCancelled.Close();

            public override string ToString() => "SecondSubscription(should get cancelled)";
        }

        // Verifies rule: https://github.com/reactive-streams/reactive-streams-jvm#2.6
        [Test]
        public void Untested_spec206_mustCallSubscriptionCancelIfItIsNoLongerValid()
            => NotVerified();  // cannot be meaningfully tested, or can it?

        // Verifies rule: https://github.com/reactive-streams/reactive-streams-jvm#2.7
        [Test]
        public void Untested_spec207_mustEnsureAllCallsOnItsSubscriptionTakePlaceFromTheSameThreadOrTakeCareOfSynchronization()
            => NotVerified();  // cannot be meaningfully tested, or can it?
                               // the same thread part of the clause can be verified but that is not very useful, or is it?

        // Verifies rule: https://github.com/reactive-streams/reactive-streams-jvm#2.8
        [Test]
        public void Required_spec208_mustBePreparedToReceiveOnNextSignalsAfterHavingCalledSubscriptionCancel()
            => SubscriberTest(stage =>
            {
                stage.Puppet.TriggerRequest(1);
                stage.Puppet.SignalCancel();
                stage.SignalNext();
                
                stage.Puppet.TriggerRequest(1);
                stage.Puppet.TriggerRequest(1);

                stage.VerifyNoAsyncErrors();
            });

        // Verifies rule: https://github.com/reactive-streams/reactive-streams-jvm#2.9
        [Test]
        public void Required_spec209_mustBePreparedToReceiveAnOnCompleteSignalWithPrecedingRequestCall()
            => SubscriberTest(stage =>
            {
                stage.Puppet.TriggerRequest(1);
                stage.SendCompletion();
                stage.Probe.ExpectCompletion();

                stage.VerifyNoAsyncErrors();
            });

        // Verifies rule: https://github.com/reactive-streams/reactive-streams-jvm#2.9
        [Test]
        public void Required_spec209_mustBePreparedToReceiveAnOnCompleteSignalWithoutPrecedingRequestCall()
            => SubscriberTest(stage =>
            {
                stage.SendCompletion();
                stage.Probe.ExpectCompletion();

                stage.VerifyNoAsyncErrors();
            });

        // Verifies rule: https://github.com/reactive-streams/reactive-streams-jvm#2.10
        [Test]
        public void Required_spec210_mustBePreparedToReceiveAnOnErrorSignalWithPrecedingRequestCall()
            => SubscriberTest(stage =>
            {
                stage.Puppet.TriggerRequest(1);
                stage.Puppet.TriggerRequest(1);

                var ex = new TestException();
                stage.SendError(ex);
                stage.Probe.ExpectError(ex);

                _environment.VerifyNoAsyncErrorsNoDelay();
            });

        // Verifies rule: https://github.com/reactive-streams/reactive-streams-jvm#2.10
        [Test]
        public void Required_spec210_mustBePreparedToReceiveAnOnErrorSignalWithoutPrecedingRequestCall()
            => SubscriberTest(stage =>
            {
                var ex = new TestException();
                stage.SendError(ex);
                stage.Probe.ExpectError(ex);

                _environment.VerifyNoAsyncErrorsNoDelay();
            });

        // Verifies rule: https://github.com/reactive-streams/reactive-streams-jvm#2.11
        [Test]
        public void Untested_spec211_mustMakeSureThatAllCallsOnItsMethodsHappenBeforeTheProcessingOfTheRespectiveEvents()
            => NotVerified(); // cannot be meaningfully tested, or can it?

        // Verifies rule: https://github.com/reactive-streams/reactive-streams-jvm#2.12
        [Test]
        public void Untested_spec212_mustNotCallOnSubscribeMoreThanOnceBasedOnObjectEquality_specViolation()
            => NotVerified(); // cannot be meaningfully tested, or can it?

        // Verifies rule: https://github.com/reactive-streams/reactive-streams-jvm#2.13
        [Test]
        public void Untested_spec213_failingOnSignalInvocation()
            => NotVerified(); // cannot be meaningfully tested, or can it?

        // Verifies rule: https://github.com/reactive-streams/reactive-streams-jvm#2.13
        [Test]
        public void Required_spec213_onSubscribe_mustThrowNullPointerExceptionWhenParametersAreNull()
            => SubscriberTest(stage =>
            {
                var subscriber = stage.Sub;
                var gotNpe = false;
                try
                {
                    subscriber.OnSubscribe(null);
                }
                catch (ArgumentNullException)
                {
                    gotNpe = true;
                }

                Assert.True(gotNpe, "OnSubscribe(null) did not throw ArgumentNullException");
                _environment.VerifyNoAsyncErrorsNoDelay();
            });

        // Verifies rule: https://github.com/reactive-streams/reactive-streams-jvm#2.13
        [Test]
        public void Required_spec213_onNext_mustThrowNullPointerExceptionWhenParametersAreNull()
            => SubscriberTest(stage =>
            {
                var subscriber = stage.Sub;
                var gotNpe = false;
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
                _environment.VerifyNoAsyncErrorsNoDelay();
            });

        // Verifies rule: https://github.com/reactive-streams/reactive-streams-jvm#2.13
        [Test]
        public void Required_spec213_onError_mustThrowNullPointerExceptionWhenParametersAreNull()
            => SubscriberTest(stage =>
            {
                var subscriber = stage.Sub;
                var gotNpe = false;
                try
                {
                    subscriber.OnError(null);
                }
                catch (ArgumentNullException)
                {
                    gotNpe = true;
                }

                Assert.True(gotNpe, "OnError(null) did not throw ArgumentNullException");
                _environment.VerifyNoAsyncErrorsNoDelay();
            });

        ////////////////////// SUBSCRIPTION SPEC RULE VERIFICATION //////////////////

        // Verifies rule: https://github.com/reactive-streams/reactive-streams-jvm#3.1
        [Test]
        public void Untested_spec301_mustNotBeCalledOutsideSubscriberContext()
            => NotVerified(); // cannot be meaningfully tested, or can it?

        // Verifies rule: https://github.com/reactive-streams/reactive-streams-jvm#3.8
        [Test]
        public void Required_spec308_requestMustRegisterGivenNumberElementsToBeProduced()
            => SubscriberTest(stage =>
            {
                stage.Puppet.TriggerRequest(2);
                stage.Probe.ExpectNext(stage.SignalNext());
                stage.Probe.ExpectNext(stage.SignalNext());

                stage.Probe.ExpectNone();
                stage.Puppet.TriggerRequest(3);

                stage.VerifyNoAsyncErrors();
            });

        // Verifies rule: https://github.com/reactive-streams/reactive-streams-jvm#3.10
        [Test]
        public void Untested_spec310_requestMaySynchronouslyCallOnNextOnSubscriber()
            => NotVerified(); // cannot be meaningfully tested, or can it?

        // Verifies rule: https://github.com/reactive-streams/reactive-streams-jvm#3.11
        [Test]
        public void Untested_spec311_requestMaySynchronouslyCallOnCompleteOrOnError()
            => NotVerified(); // cannot be meaningfully tested, or can it?

        // Verifies rule: https://github.com/reactive-streams/reactive-streams-jvm#3.14
        [Test]
        public void Untested_spec314_cancelMayCauseThePublisherToShutdownIfNoOtherSubscriptionExists()
            => NotVerified(); // cannot be meaningfully tested, or can it?

        // Verifies rule: https://github.com/reactive-streams/reactive-streams-jvm#3.15
        [Test]
        public void Untested_spec315_cancelMustNotThrowExceptionAndMustSignalOnError()
            => NotVerified(); // cannot be meaningfully tested, or can it?

        // Verifies rule: https://github.com/reactive-streams/reactive-streams-jvm#3.16
        [Test]
        public void Untested_spec316_requestMustNotThrowExceptionAndMustOnErrorTheSubscriber()
            => NotVerified(); // cannot be meaningfully tested, or can it?

        /////////////////////// ADDITIONAL "COROLLARY" TESTS ////////////////////////

        /////////////////////// TEST INFRASTRUCTURE /////////////////////////////////

        /// <summary>
        /// Prepares subscriber and publisher pair (by subscribing the first to the latter),
        /// and then hands over the tests <see cref="WhiteboxTestStage{T}"/> over to the test.
        /// 
        /// The test stage is, like in a puppet show, used to orchestrate what each participant should do.
        /// Since this is a whitebox test, this allows the stage to completely control when and how to signal / expect signals.
        /// </summary>
        public void SubscriberTest(Action<WhiteboxTestStage<T>> body)
            => body(new WhiteboxTestStage<T>(_environment, this));

        /// <summary>
        /// Provides a <see cref="WhiteboxTestStage{T}"/> without performing any additional setup,
        /// like the <see cref="SubscriberTest"/> would.
        /// 
        /// Use this method to write tests in which you need full control over when and how the initial subscribe is signalled.
        /// </summary>
        public void SubscriberTestWithoutSetup(Action<WhiteboxTestStage<T>> body)
            => body(new WhiteboxTestStage<T>(_environment, this, false));

        /// <summary>
        /// Test for feature that MAY be implemented. This test will be marked as SKIPPED if it fails.
        /// </summary>
        public void OptionalSubscriberTestWithoutSetup(Action<WhiteboxTestStage<T>> body)
        {
            try
            {
                SubscriberTestWithoutSetup(body);
            }
            catch (Exception)
            {
                NotVerified("Skipped because tested publisher does NOT implement this OPTIONAL requirement.");
            }
        }

        public void NotVerified() => NotVerified("Not verified using this TCK.");

        public void NotVerified(string message) => Assert.Ignore(message);
    }

    public class WhiteboxTestStage<T> : ManualPublisher<T>
    {
        private readonly WithHelperPublisher<T> _verification;
        

        public WhiteboxTestStage(TestEnvironment environment, SubscriberWhiteboxVerification<T> verification, bool runDefaultInit = true) : base(environment)
        {
            _verification = verification;
            if (runDefaultInit)
            {
                Publisher = CreateHelperPublisher(long.MaxValue);
                Tees = Environment.NewManualSubscriber(Publisher);
                Probe = new WhiteboxSubscriberProbe<T>(Environment, Subscriber);
                Subscribe(verification.CreateSubscriber(Probe));
                Probe.Puppet.ExpectCompletion(environment.DefaultTimeoutMilliseconds,
                    $"Subscriber {Sub} did not call `RegisterOnSubscribe`");
            }
        }

        public IPublisher<T> Publisher { get; set; }

        public ManualSubscriber<T> Tees { get; set; } // gives us access to a stream T values

        public WhiteboxSubscriberProbe<T> Probe { get; set; }

        public T LastT { get; private set; }

        public ISubscriber<T> Sub => Subscriber.Value;

        public ISubscriberPuppet Puppet => Probe.SubscriberPuppet;

        public IPublisher<T> CreateHelperPublisher(long elements) => _verification.CreateHelperPublisher(elements);

        public WhiteboxSubscriberProbe<T> CreateWhiteboxSubscriberProbe(TestEnvironment environment)
            => new WhiteboxSubscriberProbe<T>(environment, Subscriber);

        public T SignalNext() => SignalNext(NextT());

        private T SignalNext(T element)
        {
            SendNext(element);
            return element;
        }

        public T NextT()
        {
            LastT = Tees.RequestNextElement();
            return LastT;
        }

        public void VerifyNoAsyncErrors() => Environment.VerifyNoAsyncErrors();

    }

    /// <summary>
    /// This class is intented to be used as <see cref="ISubscriber{T}"/> decorator and should be used in pub.subscriber(...) calls,
    /// in order to allow intercepting calls on the underlying <see cref="ISubscriber{T}"/>.
    /// This delegation allows the proxy to implement <see cref="BlackboxProbe{T}"/> assertions.
    /// </summary>
    public class BlackboxSubscriberProxy<T> : BlackboxProbe<T>, ISubscriber<T>
    {
        public BlackboxSubscriberProxy(TestEnvironment environment, ISubscriber<T> subscriber)
            : base(environment, Promise<ISubscriber<T>>.Completed(environment, subscriber))
        {
        }

        public void OnNext(T element)
        {
            RegisterOnNext(element);
            Sub.OnNext(element);
        }

        public void OnSubscribe(ISubscription subscription) => Sub.OnSubscribe(subscription);

        public void OnError(Exception cause)
        {
            RegisterOnError(cause);
            Sub.OnError(cause);
        }

        public void OnComplete()
        {
            RegisterOnComplete();
            Sub.OnComplete();
        }
    }

    public class BlackboxProbe<T> : ISubscriberProbe<T>
    {
        public BlackboxProbe(TestEnvironment environment, Promise<ISubscriber<T>> subscriber)
        {
            Environment = environment;
            Subscriber = subscriber;
            Elements = new Receptacle<T>(environment);
            Error = new Promise<Exception>(environment);
        }

        protected TestEnvironment Environment { get; }

        protected Promise<ISubscriber<T>> Subscriber { get; }

        protected Receptacle<T> Elements { get; }

        protected Promise<Exception> Error { get; }

        public ISubscriber<T> Sub => Subscriber.Value;

        public void RegisterOnNext(T element) => Elements.Add(element);

        public void RegisterOnComplete()
        {
            try
            {
                Elements.Complete();
            }
            catch (IllegalStateException)
            {
                // "Queue full", onComplete was already called
                Environment.Flop("subscriber.OnComplete was called a second time, which is illegal according to Rule 1.7");
            }
        }

        public void RegisterOnError(Exception ex)
        {
            try
            {
                Error.Complete(ex);
            }
            catch (IllegalStateException)
            {
                // "Queue full", onComplete was already called
                Environment.Flop("subscriber.OnError was called a second time, which is illegal according to Rule 1.7");
            }
        }

        public T ExpectNext() =>
            Elements.Next(Environment.DefaultTimeoutMilliseconds,
                $"Subscriber {Sub} did not call `RegisterOnNext(_)`");

        public void ExpectNext(T expected) => ExpectNext(expected, Environment.DefaultTimeoutMilliseconds);

        public void ExpectNext(T expected, long timeoutMilliseconds)
        {
            var received = Elements.Next(timeoutMilliseconds,
                $"Subscriber {Sub} did not call `RegisterOnNext({expected})`");
            if (!received.Equals(expected))
                Environment.Flop(
                    $"Subscriber {Sub} called `RegisterOnNext({received})` rather than `RegisterOnNext({expected})`");
        }

        public void ExpectCompletion() => ExpectCompletion(Environment.DefaultTimeoutMilliseconds);

        public void ExpectCompletion(long timeoutMilliseconds)
            => ExpectCompletion(timeoutMilliseconds, $"Subscriber {Sub} did not call `RegisterOnComplete()`");

        public void ExpectCompletion(long timeoutMilliseconds, string message)
            => Elements.ExpectCompletion(timeoutMilliseconds, message);

        public E ExpectError<E>() where E : Exception => ExpectError<E>(Environment.DefaultTimeoutMilliseconds);

        public E ExpectError<E>(long timeoutMilliseconds) where E : Exception
        {
            Error.ExpectCompletion(timeoutMilliseconds, $"Subscriber {Sub} did not call `RegisterOnError({typeof(E).Name})`");

            if (Error.Value == null)
                return Environment.FlopAndFail<E>($"Subscriber {Sub} did not call `RegisterOnError({typeof(E).Name})`");
            var value = Error.Value as E;
            if (value != null)
                return value;

            return
                Environment.FlopAndFail<E>(
                    $"Subscriber {Sub} called `RegisterOnError({Error.Value.GetType().Name})` rather than `RegisterOnError({typeof(E).Name})`");
        }

        public void ExpectError(Exception expected) => ExpectError(expected, Environment.DefaultTimeoutMilliseconds);

        public void ExpectError(Exception expected, long timeoutMilliseconds)
        {
            Error.ExpectCompletion(timeoutMilliseconds, $"Subscriber {Sub} did not call `RegisterOnError({expected})`");
            if (Error.Value != expected)
                Environment.Flop(
                    $"Subscriber {Sub} called `RegisterOnError({Error.Value})` rather than `RegisterOnError({expected})`");
        }

        public void ExpectNone() => ExpectNone(Environment.DefaultTimeoutMilliseconds);

        public void ExpectNone(long withinMilliseconds) => Elements.ExpectNone(withinMilliseconds, "Expected nothing");
    }

    public class WhiteboxSubscriberProbe<T> : BlackboxProbe<T>, ISubscriberPuppeteer
    {
        public WhiteboxSubscriberProbe(TestEnvironment environment, Promise<ISubscriber<T>> subscriber)
            : base(environment, subscriber)
        {
            Puppet = new Promise<ISubscriberPuppet>(environment);
        }

        public Promise<ISubscriberPuppet> Puppet { get; }

        public ISubscriberPuppet SubscriberPuppet => Puppet.Value;

        public void RegisterOnSubscribe(ISubscriberPuppet puppet)
        {
            if(!Puppet.IsCompleted())
                Puppet.Complete(puppet);
        }
    }

    public interface ISubscriberPuppeteer
    {
        /// <summary>
        /// Must be called by the test subscriber when it has successfully registered a subscription
        /// inside the `onSubscribe` method.
        /// </summary>
        void RegisterOnSubscribe(ISubscriberPuppet puppet);
    }

    public interface ISubscriberProbe<in T>
    {
        /// <summary>
        /// Must be called by the test subscriber when it has received an`onNext` event.
        /// </summary>
        void RegisterOnNext(T element);

        /// <summary>
        /// Must be called by the test subscriber when it has received an `onComplete` event.
        /// </summary>
        void RegisterOnComplete();

        /// <summary>
        /// Must be called by the test subscriber when it has received an `onError` event.
        /// </summary>
        void RegisterOnError(Exception ex);
    }

    public interface ISubscriberPuppet
    {
        void TriggerRequest(long elements);

        void SignalCancel();
    }
}
