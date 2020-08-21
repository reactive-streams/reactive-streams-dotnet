using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Reactive.Streams.TCK.Support;
using Reactive.Streams.TCK.Tests.Support;

namespace Reactive.Streams.TCK.Tests
{
    /// <summary>
    /// Validates that the TCK's <see cref="PublisherVerification{T}"/> fails with nice human readable errors.
    /// >Important: Please note that all Publishers implemented in this file are *wrong*!
    /// </summary>
    public class PublisherVerificationTest : TCKVerificationSupport
    {
        [SkippableFact]
        public void Required_spec101_subscriptionRequestMustResultInTheCorrectNumberOfProducedElements_shouldFailBy_ExpectingOnError()
            => RequireTestFailure(() => NoopPublisherVerification().Required_spec101_subscriptionRequestMustResultInTheCorrectNumberOfProducedElements(),
                    "produced no element after first");

        [SkippableFact]
        public void Required_spec102_maySignalLessThanRequestedAndTerminateSubscription_shouldFailBy_notReceivingAnyElement()
            => RequireTestFailure(() => NoopPublisherVerification().Required_spec102_maySignalLessThanRequestedAndTerminateSubscription(),
                    "Did not receive expected element");

        [SkippableFact]
        public void Required_spec102_maySignalLessThanRequestedAndTerminateSubscription_shouldFailBy_receivingTooManyElements()
            => RequireTestFailure(() => DemandIgnoringSynchronousPublisherVerification().Required_spec102_maySignalLessThanRequestedAndTerminateSubscription(),
                    "Expected end-of-stream but got element [3]");

        [SkippableFact]
        public void Stochastic_spec103_mustSignalOnMethodsSequentially_shouldFailBy_concurrentlyAccessingOnNext()
        {
            var verification = CustomPublisherVerification(new ConcurrentAccessPublisher());
            RequireTestFailure(() => verification.Stochastic_spec103_mustSignalOnMethodsSequentially(),
                "Illegal concurrent access detected");
        }

        /// <summary>
        /// highly specialised threadpool driven publisher which aims to FORCE concurrent access,
        /// so that we can confirm the test is able to catch this.
        /// </summary>
        private sealed class ConcurrentAccessPublisher : IPublisher<int>
        {
            // this is an arbitrary number, we just need "many threads" to try to force an concurrent access scenario
            private const int MaxSignallingThreads = 100;
            private readonly CancellationTokenSource _source;
            private readonly CancellationToken _token;
            private readonly AtomicBoolean _concurrentAccessCaused = new AtomicBoolean();
            private readonly AtomicCounter _startedSignallingThreads = new AtomicCounter();

            public ConcurrentAccessPublisher()
            {
                _source = new CancellationTokenSource();
                _token = _source.Token;
            }

            public void Subscribe(ISubscriber<int> subscriber)
                => subscriber.OnSubscribe(new LamdaSubscription(onRequest: n =>
                {
                    Action signalling = () =>
                    {
                        for (var i = 0L; i < n; i++)
                        {
                            try
                            {
                                // shutdown cleanly in when the task is shutting down
                                if (_token.IsCancellationRequested)
                                    return;

                                subscriber.OnNext((int)i);
                            }
                            catch (Exception ex)
                            {
                                // signal others to shut down
                                _source.Cancel();

                                if (ex is Latch.ExpectedOpenLatchException)
                                {
                                    if (!_concurrentAccessCaused.CompareAndSet(false, true))
                                        throw new Exception("Concurrent access detected", ex);
                                    // error signalled once already, stop more errors from propagating
                                    return;
                                }
                                else
                                    throw;
                            }
                        }
                    };

                    // must be guarded like this in case a Subscriber triggers request() synchronously from it's onNext()
                    while (_startedSignallingThreads.GetAndIncrement() < MaxSignallingThreads && !_token.IsCancellationRequested)
                        new Thread(() => signalling()).Start();
                }));
        }

        [SkippableFact]
        public void Stochastic_spec103_mustSignalOnMethodsSequentially_shouldPass_forSynchronousPublisher()
        {
            var publisher = new LamdaPublisher<int>(onSubscribe: subscriber =>
            {
                var element = 0;

                subscriber.OnSubscribe(new LamdaSubscription(onRequest: n =>
                {
                    for (var i = 0; i < n; i++)
                        subscriber.OnNext(element++);

                    subscriber.OnComplete();
                }));
            });
            CustomPublisherVerification(publisher).Stochastic_spec103_mustSignalOnMethodsSequentially();
        }

        [SkippableFact]
        public void Optional_spec104_mustSignalOnErrorWhenFails_shouldFail()
        {
            var publisher = new LamdaPublisher<int>(onSubscribe: subscriber =>
            {
                throw new Exception("It is not valid to throw here!");
            });
            var verification = CustomPublisherVerification(null, publisher);
            RequireTestFailure(() => verification.Optional_spec104_mustSignalOnErrorWhenFails(),
                "Publisher threw exception (It is not valid to throw here!) instead of signalling error via onError!");
        }

        [SkippableFact]
        public void Optional_spec104_mustSignalOnErrorWhenFails_shouldBeSkippedWhenNoErrorPublisherGiven()
            => RequireTestSkip(() => NoopPublisherVerification().Optional_spec104_mustSignalOnErrorWhenFails(),
                    PublisherVerification<int>.SkippingNoErrorPublisherAvailable);

        [SkippableFact]
        public void Required_spec105_mustSignalOnCompleteWhenFiniteStreamTerminates_shouldFail()
        {
            var publisher = new LamdaPublisher<int>(onSubscribe: subscriber =>
            {
                var signal = 0;

                subscriber.OnSubscribe(new LamdaSubscription(onRequest: n =>
                {
                    for (var i = 0; i < n; i++)
                        subscriber.OnNext(signal++);

                    // intentional omission of onComplete
                }));
            });
            var verification = CustomPublisherVerification(publisher);

            RequireTestFailure(() => verification.Required_spec105_mustSignalOnCompleteWhenFiniteStreamTerminates(),
                messagePart: "Expected end-of-stream but got element [3]");
        }

        [SkippableFact]
        public void Optional_spec105_emptyStreamMustTerminateBySignallingOnComplete_shouldNotAllowEagerOnComplete()
        {
            var publisher = new LamdaPublisher<int>(onSubscribe: subscriber => subscriber.OnComplete());
            var verification = new Spec105Verification(NewTestEnvironment(), publisher);
            RequireTestFailure(() => verification.Optional_spec105_emptyStreamMustTerminateBySignallingOnComplete(),
                "Subscriber.OnComplete() called before Subscriber.OnSubscribe");
        }

        [TestFixture(Ignore = "Helper for single test")]
        private sealed class Spec105Verification : PublisherVerification<int>
        {
            private readonly IPublisher<int> _publisher;

            /// <summary>
            /// We need this constructor for NUnit even if the fixture is ignored 
            /// </summary>
            public Spec105Verification() : base(NewTestEnvironment())
            {

            }

            public Spec105Verification(TestEnvironment environment, IPublisher<int> publisher) : base(environment)
            {
                _publisher = publisher;
            }

            public override IPublisher<int> CreatePublisher(long elements) => _publisher;

            public override IPublisher<int> CreateFailedPublisher() => null;

            public override long MaxElementsFromPublisher { get; } = 0; // it is an "empty" Publisher
        }

        [SkippableFact]
        public void Required_spec107_mustNotEmitFurtherSignalsOnceOnCompleteHasBeenSignalled_shouldFailForNotCompletingPublisher()
        {
            var cancellation = new CancellationTokenSource();

            try
            {
                var verification = DemandIgnoringAsynchronousPublisherVerification(cancellation.Token);
                RequireTestFailure(
                    () => verification.Required_spec107_mustNotEmitFurtherSignalsOnceOnCompleteHasBeenSignalled(),
                    "Expected end-of-stream but got element [" /* element which should not have been signalled */);
            }
            finally
            {
                cancellation.Cancel();
            }
        }

        [SkippableFact]
        public void Required_spec107_mustNotEmitFurtherSignalsOnceOnCompleteHasBeenSignalled_shouldFailForPublisherWhichCompletesButKeepsServingData()
        {
            var publisher = new LamdaPublisher<int>(onSubscribe: subscriber =>
            {
                var completed = false;

                subscriber.OnSubscribe(new LamdaSubscription(onRequest: n =>
                {
                    // emit one element
                    subscriber.OnNext(0);

                    // and "complete"
                    // but keep signalling data if more demand comes in anyway!
                    if (!completed)
                    {
                        subscriber.OnComplete();
                        completed = true;
                    }
                }));
            });
            var verification = CustomPublisherVerification(publisher);
            RequireTestFailure(() => verification.Required_spec107_mustNotEmitFurtherSignalsOnceOnCompleteHasBeenSignalled(),
                "Unexpected element 0 received after stream completed");
        }

        [SkippableFact]
        public void Required_spec109_subscribeThrowNPEOnNullSubscriber_shouldFailIfDoesntThrowNPE()
        {
            var publisher = new LamdaPublisher<int>(onSubscribe: subscriber => { });
            var verification = CustomPublisherVerification(publisher);
            RequireTestFailure(() => verification.Required_spec109_subscribeThrowNPEOnNullSubscriber(),
                "Publisher did not throw a ArgumentNullException when given a null Subscribe in subscribe");
        }

        [SkippableFact]
        public void Required_spec109_mayRejectCallsToSubscribeIfPublisherIsUnableOrUnwillingToServeThemRejectionMustTriggerOnErrorAfterOnSubscribe_actuallyPass()
        {
            var publisher = new LamdaPublisher<int>(onSubscribe: subscriber =>
            {
                subscriber.OnSubscribe(new LamdaSubscription());
                subscriber.OnError(new Exception("Sorry, I'm busy now. Call me later."));
            });
            CustomPublisherVerification(null, publisher)
                .Required_spec109_mayRejectCallsToSubscribeIfPublisherIsUnableOrUnwillingToServeThemRejectionMustTriggerOnErrorAfterOnSubscribe();
        }

        [SkippableFact]
        public void Required_spec109_mustIssueOnSubscribeForNonNullSubscriber_mustFailIfOnCompleteHappensFirst()
        {
            var publisher = new LamdaPublisher<int>(onSubscribe: subscriber => subscriber.OnComplete());
            var verification = CustomPublisherVerification(publisher);
            RequireTestFailure(() => verification.Required_spec109_mustIssueOnSubscribeForNonNullSubscriber(),
                "OnSubscribe should be called prior to OnComplete always");
        }

        [SkippableFact]
        public void Required_spec109_mustIssueOnSubscribeForNonNullSubscriber_mustFailIfOnNextHappensFirst()
        {
            var publisher = new LamdaPublisher<int>(onSubscribe: subscriber => subscriber.OnNext(1337));
            var verification = CustomPublisherVerification(publisher);
            RequireTestFailure(() => verification.Required_spec109_mustIssueOnSubscribeForNonNullSubscriber(),
                "OnSubscribe should be called prior to OnNext always");
        }

        [SkippableFact]
        public void Required_spec109_mustIssueOnSubscribeForNonNullSubscriber_mustFailIfOnErrorHappensFirst()
        {
            var publisher = new LamdaPublisher<int>(onSubscribe: subscriber => subscriber.OnError(new TestException()));
            var verification = CustomPublisherVerification(publisher);
            RequireTestFailure(() => verification.Required_spec109_mustIssueOnSubscribeForNonNullSubscriber(),
                "OnSubscribe should be called prior to OnError always");
        }

        [SkippableFact]
        public void Required_spec109_mayRejectCallsToSubscribeIfPublisherIsUnableOrUnwillingToServeThemRejectionMustTriggerOnErrorAfterOnSubscribe_shouldFail()
        {
            var publisher = new LamdaPublisher<int>(onSubscribe: subscriber => subscriber.OnSubscribe(new LamdaSubscription()));
            var verification = CustomPublisherVerification(null, publisher);
            RequireTestFailure(() => verification.Required_spec109_mayRejectCallsToSubscribeIfPublisherIsUnableOrUnwillingToServeThemRejectionMustTriggerOnErrorAfterOnSubscribe(),
                "Should have received OnError");
        }

        [SkippableFact]
        public void Required_spec109_mayRejectCallsToSubscribeIfPublisherIsUnableOrUnwillingToServeThemRejectionMustTriggerOnErrorAfterOnSubscribe_beSkippedForNoGivenErrorPublisher()
        {
            RequireTestSkip(() => NoopPublisherVerification().Required_spec109_mayRejectCallsToSubscribeIfPublisherIsUnableOrUnwillingToServeThemRejectionMustTriggerOnErrorAfterOnSubscribe(),
                PublisherVerification<int>.SkippingNoErrorPublisherAvailable);
        }

        [SkippableFact]
        public void Untested_spec110_rejectASubscriptionRequestIfTheSameSubscriberSubscribesTwice_shouldFailBy_skippingSinceOptional()
        {
            RequireTestFailure(() => NoopPublisherVerification().Untested_spec110_rejectASubscriptionRequestIfTheSameSubscriberSubscribesTwice(),
                "Not verified by this TCK.");
        }

        [SkippableFact]
        public void Optional_spec111_maySupportMultiSubscribe_shouldFailBy_actuallyPass()
            => NoopPublisherVerification().Optional_spec111_maySupportMultiSubscribe();

        [SkippableFact]
        public void Optional_spec111_multicast_mustProduceTheSameElementsInTheSameSequenceToAllOfItsSubscribersWhenRequestingManyUpfront_shouldFailBy_expectingOnError()
        {
            var random = new Random();
            var publisher = new LamdaPublisher<int>(onSubscribe: subscriber =>
            {
                subscriber.OnSubscribe(new LamdaSubscription(onRequest: n =>
                {
                    for (var i = 0; i < n; i++)
                        subscriber.OnNext(random.Next());
                }));
            });
            var verification = CustomPublisherVerification(publisher);
            RequireTestFailure(() => verification.Optional_spec111_multicast_mustProduceTheSameElementsInTheSameSequenceToAllOfItsSubscribersWhenRequestingManyUpfront(),
                "Expected elements to be signaled in the same sequence to 1st and 2nd subscribers");
        }

        [SkippableFact]
        public void Required_spec302_mustAllowSynchronousRequestCallsFromOnNextAndOnSubscribe_shouldFailBy_reportingAsyncError()
            => RequireTestFailure(() => OnErroringPublisherVerification().Required_spec302_mustAllowSynchronousRequestCallsFromOnNextAndOnSubscribe(),
                "Async error during test execution: Test Exception: Boom!");

        [SkippableFact]
        public void Required_spec303_mustNotAllowUnboundedRecursion_shouldFailBy_informingAboutTooDeepStack()
        {
            var publisher = new LamdaPublisher<int>(onSubscribe: subscriber =>
            {
                subscriber.OnSubscribe(new LamdaSubscription(onRequest: n =>
                {
                    subscriber.OnNext(0); // naive reccursive call, would explode with StackOverflowException
                }));
            });
            var verification = CustomPublisherVerification(publisher);
            RequireTestFailure(() => verification.Required_spec303_mustNotAllowUnboundedRecursion(),
              /*Got 2 onNext calls within thread: ... */ "yet expected recursive bound was 1");
        }

        [SkippableFact]
        public void Required_spec306_afterSubscriptionIsCancelledRequestMustBeNops_shouldFailBy_unexpectedElement()
            => RequireTestFailure(() => DemandIgnoringSynchronousPublisherVerification().Required_spec306_afterSubscriptionIsCancelledRequestMustBeNops(),
                "Did not expect an element but got element [0]");

        [SkippableFact]
        public void Required_spec307_afterSubscriptionIsCancelledAdditionalCancelationsMustBeNops_shouldPass()
            => DemandIgnoringSynchronousPublisherVerification().Required_spec307_afterSubscriptionIsCancelledAdditionalCancelationsMustBeNops();

        [SkippableFact]
        public void Required_spec307_afterSubscriptionIsCancelledAdditionalCancelationsMustBeNops_shouldFailBy_unexpectedErrorInCancelling()
        {
            var publisher = new LamdaPublisher<int>(onSubscribe: subscriber =>
            {
                subscriber.OnSubscribe(new LamdaSubscription(onCancel: () =>
                {
                    subscriber.OnError(new TestException());// illegal error signalling!
                }));
            });
            var verification = CustomPublisherVerification(publisher);
            RequireTestFailure(() => verification.Required_spec307_afterSubscriptionIsCancelledAdditionalCancelationsMustBeNops(),
               "Async error during test execution: Test Exception: Boom!");
        }

        [SkippableFact]
        public void Required_spec309_requestZeroMustSignalIllegalArgumentException_shouldFailBy_expectingOnError()
            => RequireTestFailure(() => NoopPublisherVerification().Required_spec309_requestZeroMustSignalIllegalArgumentException(),
                "Expected OnError");

        [SkippableFact]
        public void Required_spec309_requestNegativeNumberMustSignalIllegalArgumentException_shouldFailBy_expectingOnError()
            => RequireTestFailure(() => NoopPublisherVerification().Required_spec309_requestNegativeNumberMustSignalIllegalArgumentException(),
                "Expected OnError");

        [SkippableFact]
        public void Required_spec312_cancelMustMakeThePublisherToEventuallyStopSignaling_shouldFailBy_havingEmitedMoreThanRequested()
        {
            var cancellation = new CancellationTokenSource();

            try
            {
                var verification = DemandIgnoringAsynchronousPublisherVerification(cancellation.Token);
                RequireTestFailure(
                    () => verification.Required_spec312_cancelMustMakeThePublisherToEventuallyStopSignaling(),
                    /*Publisher signalled [...] */ ", which is more than the signalled demand: ");
            }
            finally
            {
                cancellation.Cancel();
            }
        }

        [SkippableFact]
        public void Required_spec313_cancelMustMakeThePublisherEventuallyDropAllReferencesToTheSubscriber_shouldFailBy_keepingTheReferenceLongerThanNeeded()
        {
            ISubscriber<int> sub;
            var publisher = new LamdaPublisher<int>(onSubscribe: subscriber =>
            {
                sub = subscriber;// keep the reference

                subscriber.OnSubscribe(new LamdaSubscription(onRequest: n =>
                {
                    for (var i = 0; i < n; i++)
                        subscriber.OnNext((int)n);
                }, onCancel: () =>
                {
                    // noop, we still keep the reference!
                }));
            });
            var verification = CustomPublisherVerification(publisher);
            RequireTestFailure(() => verification.Required_spec313_cancelMustMakeThePublisherEventuallyDropAllReferencesToTheSubscriber(),
               "did not drop reference to test subscriber after subscription cancellation");
        }

        [SkippableFact]
        public void Required_spec317_mustSupportAPendingElementCountUpToLongMaxValue_shouldFail_onAsynchDemandIgnoringPublisher()
        {
            var cancellation = new CancellationTokenSource();

            try
            {
                var verification = DemandIgnoringAsynchronousPublisherVerification(cancellation.Token);
                RequireTestFailure(
                    () => verification.Required_spec317_mustSupportAPendingElementCountUpToLongMaxValue(),
                    "Expected end-of-stream but got");
            }
            finally
            {
                cancellation.Cancel();
            }
        }

        [SkippableFact]
        public void Required_spec317_mustSupportAPendingElementCountUpToLongMaxValue_shouldFail_onSynchDemandIgnoringPublisher()
            => RequireTestFailure(() => DemandIgnoringSynchronousPublisherVerification().Required_spec317_mustSupportAPendingElementCountUpToLongMaxValue(),
                "Received more than bufferSize (32) OnNext signals. The Publisher probably emited more signals than expected!");

        [SkippableFact]
        public void Required_spec317_mustNotSignalOnErrorWhenPendingAboveLongMaxValue_shouldFail_onSynchOverflowingPublisher()
        {
            var demand = 0L;
            var publisher = new LamdaPublisher<int>(onSubscribe: subscriber =>
            {
                subscriber.OnSubscribe(new LamdaSubscription(onRequest: n =>
                {
                    // it does not protect from demand overflow!
                    demand += n;
                    if (demand < 0)
                        // overflow
                        subscriber.OnError(new IllegalStateException("Illegally signalling OnError (violates rule 3.17)")); // Illegally signal error
                    else
                        subscriber.OnNext(0);
                }));
            });
            var verification = CustomPublisherVerification(publisher);
            RequireTestFailure(() => verification.Required_spec317_mustNotSignalOnErrorWhenPendingAboveLongMaxValue(),
               "Async error during test execution: Illegally signalling OnError (violates rule 3.17)");
        }

        [SkippableFact]
        public void Required_spec317_mustSupportACumulativePendingElementCountUpToLongMaxValue_shouldFailWhenErrorSignalledOnceMaxValueReached()
        {
            var demand = 0L;
            var publisher = new LamdaPublisher<int>(onSubscribe: subscriber =>
            {
                subscriber.OnSubscribe(new LamdaSubscription(onRequest: n =>
                {
                    demand += n;

                    // this is a mistake, it should still be able to accumulate such demand
                    if (demand == long.MaxValue)
                        subscriber.OnError(new IllegalStateException("Illegally signalling onError too soon! Cumulative demand equal to long.MaxValue is legal."));

                    subscriber.OnNext(0);
                }));
            });
            var verification = CustomPublisherVerification(publisher);
            RequireTestFailure(() => verification.Required_spec317_mustSupportACumulativePendingElementCountUpToLongMaxValue(),
               "Async error during test execution: Illegally signalling onError too soon!");
        }

        [SkippableFact]
        public void Required_spec317_mustNotSignalOnErrorWhenPendingAboveLongMaxValue_forSynchronousPublisher()
        {
            var sent = new AtomicCounter(0);

            var publisher = new LamdaPublisher<int>(onSubscribe: subscriber =>
            {
                var started = false;
                var cancelled = false;

                subscriber.OnSubscribe(new LamdaSubscription(onRequest: n =>
                {
                    if (!started)
                    {
                        started = true;
                        while (!cancelled)
                            subscriber.OnNext(sent.GetAndIncrement());
                    }
                }, onCancel: () => cancelled = true));
            });
            var verification = CustomPublisherVerification(publisher);
            verification.Required_spec317_mustNotSignalOnErrorWhenPendingAboveLongMaxValue();

            // 11 due to the implementation of this particular TCK test (see impl)
            Assert.AreEqual(11, sent.Current);
        }

        // FAILING IMPLEMENTATIONS //

        /// <summary>
        /// Verification using a Publisher that never publishes any element.
        /// Skips the error state publisher tests.
        /// </summary>
        private PublisherVerification<int> NoopPublisherVerification()
        {
            var publisher = new LamdaPublisher<int>(onSubscribe: subscriber =>
            {
                subscriber.OnSubscribe(new LamdaSubscription());
            });

            return new SimpleVerification(NewTestEnvironment(), publisher);
        }

        /// <summary>
        /// Verification using a Publisher that never publishes any element
        /// </summary>
        private PublisherVerification<int> OnErroringPublisherVerification()
        {
            var publisher = new LamdaPublisher<int>(onSubscribe: subscriber =>
            {
                subscriber.OnSubscribe(new LamdaSubscription(onRequest: n =>
                {
                    subscriber.OnError(new TestException());
                }));
            });

            return new SimpleVerification(NewTestEnvironment(), publisher);
        }

        /// <summary>
        /// Custom Verification using given Publishers
        /// </summary>
        private PublisherVerification<int> CustomPublisherVerification(IPublisher<int> publisher)
            => new SimpleVerification(new TestEnvironment(), publisher);

        /// <summary>
        /// Custom Verification using given Publishers
        /// </summary>
        private PublisherVerification<int> CustomPublisherVerification(IPublisher<int> publisher,
            IPublisher<int> errorPublisher)
            => new SimpleVerification(new TestEnvironment(), publisher, errorPublisher);

        /// <summary>
        /// Verification using a Publisher that publishes elements even with no demand available
        /// </summary>
        private PublisherVerification<int> DemandIgnoringSynchronousPublisherVerification()
        {
            var publisher = new LamdaPublisher<int>(onSubscribe: subscriber =>
            {
                subscriber.OnSubscribe(new LamdaSubscription(onRequest: n =>
                {
                    for (var i = 0L; i <= n; i++)
                        // one too much
                        subscriber.OnNext((int)i);
                }));
            });
            return new SimpleVerification(new TestEnvironment(), publisher);
        }

        /// <summary>
        /// Verification using a Publisher that publishes elements even with no demand available, from multiple threads (!).
        /// 
        /// Please note that exceptions thrown from onNext *will be swallowed* – reason being this verification is used to check
        /// very specific things about error reporting - from the "TCK Tests", we do not have any assertions on thrown exceptions.
        /// </summary>
        private PublisherVerification<int> DemandIgnoringAsynchronousPublisherVerification(CancellationToken token)
            => DemandIgnoringAsynchronousPublisherVerification(true, token);

        /// <summary>
        /// Verification using a Publisher that publishes elements even with no demand available, from multiple threads (!).
        /// </summary>
        private PublisherVerification<int> DemandIgnoringAsynchronousPublisherVerification(bool swallowOnNextExceptions, CancellationToken token)
            => new SimpleVerification(new TestEnvironment(), new DemandIgnoringAsyncPublisher(swallowOnNextExceptions, token));

        private sealed class DemandIgnoringAsyncPublisher : IPublisher<int>
        {
            private readonly bool _swallowOnNextExceptions;
            private readonly CancellationToken _token;
            private readonly AtomicCounter _startedSignallingThreads = new AtomicCounter(0);
            private const int MaxSignallingThreads = 2;

            private readonly AtomicBoolean _concurrentAccessCaused = new AtomicBoolean();

            public DemandIgnoringAsyncPublisher(bool swallowOnNextExceptions, CancellationToken token)
            {
                _swallowOnNextExceptions = swallowOnNextExceptions;
                _token = token;
            }

            public void Subscribe(ISubscriber<int> subscriber)
                => subscriber.OnSubscribe(new LamdaSubscription(onRequest: n =>
                {
                    Action signalling = () =>
                    {
                        for (var i = 0L; i <= n; i++)
                        {
                            if (_token.IsCancellationRequested)
                                break;

                            // one signal too much
                            try
                            {
                                var signal = i;
                                Task.Run(() =>
                                {
                                    try
                                    {
                                        subscriber.OnNext((int)signal);
                                    }
                                    catch (Exception ex)
                                    {
                                        if (!_swallowOnNextExceptions)
                                            throw new Exception("onNext threw an exception!", ex);
                                        else
                                        {
                                            // yes, swallow the exception, we're not asserting and they'd just end up being logged (stdout),
                                            // which we do not need in this specific PublisherVerificationTest
                                        }
                                    }
                                });
                            }
                            catch (Exception ex)
                            {
                                if (ex is Latch.ExpectedOpenLatchException)
                                {
                                    if (_concurrentAccessCaused.CompareAndSet(false, true))
                                        throw new Exception("Concurrent access detected", ex);
                                }
                                else
                                {
                                    if (!_concurrentAccessCaused.Value)
                                        throw;
                                }
                            }
                        }
                    };

                    // must be guarded like this in case a Subscriber triggers request() synchronously from it's onNext()
                    while (_startedSignallingThreads.GetAndAdd(1) < MaxSignallingThreads)
                        Task.Run(signalling, _token);
                }));
        }

        [TestFixture(Ignore = "Helper for single test")]
        private sealed class SimpleVerification : PublisherVerification<int>
        {
            private readonly IPublisher<int> _publisher;
            private readonly IPublisher<int> _failedPublisher;

            /// <summary>
            /// We need this constructor for NUnit even if the fixture is ignored 
            /// </summary>
            public SimpleVerification() : base(NewTestEnvironment()) { }

            public SimpleVerification(TestEnvironment environment, IPublisher<int> publisher, IPublisher<int> failedPublisher = null) : base(environment)
            {
                _publisher = publisher;
                _failedPublisher = failedPublisher;
            }

            public override IPublisher<int> CreatePublisher(long elements) => _publisher;

            public override IPublisher<int> CreateFailedPublisher() => _failedPublisher;
        }

        private static TestEnvironment NewTestEnvironment() => new TestEnvironment();
    }
}
