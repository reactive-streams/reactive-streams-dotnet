using System;
using System.Collections.Generic;
using System.Threading;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;
using Reactive.Streams.TCK.Support;

namespace Reactive.Streams.TCK
{
    public abstract class PublisherVerification<T> : IPublisherVerificationRules
    {
        private const string PublisherReferenceGcTimeoutMillisecondsEnvironment = "PUBLISHER_REFERENCE_GC_TIMEOUT_MILLIS";
        private const long DefaultPublisherReferenceGcTimeoutMilliseconds = 300;


        private readonly TestEnvironment _environment;

        /// <summary>
        /// The amount of time after which a cancelled Subscriber reference should be dropped.
        /// See Rule 3.13 for details.
        /// </summary>
        private readonly long _publisherReferenceGcTimeoutMillis;

        /// <summary>
        /// Constructs a new verification class using the given environment and configuration.
        /// </summary>
        /// <param name="environment">The environment configuration</param>
        /// <param name="publisherReferenceGcTimeoutMillis">Used to determine after how much time a reference to a Subscriber should be already dropped by the Publisher.</param>
        protected PublisherVerification(TestEnvironment environment, long publisherReferenceGcTimeoutMillis)
        {
            _environment = environment;
            _publisherReferenceGcTimeoutMillis = publisherReferenceGcTimeoutMillis;
            SetUp();
        }

        /// <summary>
        /// Constructs a new verification class using the given environment and configuration.
        /// </summary>
        /// <param name="environment">The environment configuration</param>
        protected PublisherVerification(TestEnvironment environment)
            : this(environment, EnvironmentPublisherReferenceGcTimeoutMilliseconds())
        {
        }

        /// <summary>
        /// Tries to parse the environment variable `PUBLISHER_REFERENCE_GC_TIMEOUT_MILLIS` as long and returns the value if present,
        /// OR its default value <see cref="DefaultPublisherReferenceGcTimeoutMilliseconds"/>).
        /// 
        /// This value is used to determine after how much time a reference to a Subscriber should be already dropped by the Publisher.
        /// </summary>
        public static long EnvironmentPublisherReferenceGcTimeoutMilliseconds()
        {
            var environmentMilliseconds =
                Environment.GetEnvironmentVariable(PublisherReferenceGcTimeoutMillisecondsEnvironment);
            if (environmentMilliseconds == null)
                return DefaultPublisherReferenceGcTimeoutMilliseconds;
            try
            {
                return long.Parse(environmentMilliseconds);
            }
            catch (Exception ex)
            {
                throw new ArgumentException(
                    $"Unable to parse {PublisherReferenceGcTimeoutMillisecondsEnvironment} environment value {environmentMilliseconds} as long",
                    ex);
            }
        }

        /// <summary>
        /// This is the main method you must implement in your test incarnation.
        /// It must create a Publisher for a stream with exactly the given number of elements.
        /// If <paramref name="elements"/> is <see cref="long.MaxValue"/> the produced stream must be infinite.
        /// </summary>
        public abstract IPublisher<T> CreatePublisher(long elements);

        /// <summary>
        /// By implementing this method, additional TCK tests concerning a "failed" publishers will be run.
        /// 
        /// The expected behaviour of the <see cref="IPublisher{T}"/> returned by this method is hand out a subscription,
        /// followed by signalling <see cref="ISubscriber{T}.OnError"/> on it, as specified by Rule 1.9.
        /// 
        /// If you ignore these additional tests, return `null` from this method.
        /// </summary>
        /// <returns></returns>
        public abstract IPublisher<T> CreateFailedPublisher();

        /// <summary>
        /// Override and return lower value if your Publisher is only able to produce a known number of elements.
        /// For example, if it is designed to return at-most-one element, return `1` from this method.
        /// 
        /// Defaults to <see cref="long.MaxValue"/> - 1, meaning that the Publisher can be produce a huge but NOT an unbounded number of eleme
        /// 
        /// To mark your Publisher will *never* signal an <see cref="ISubscriber{T}.OnComplete"/> override this method and return <see cref="long.MaxValue"/>,
        /// which will result in *skipping all tests which require an onComplete to be triggered* (!).
        /// </summary>
        public virtual long MaxElementsFromPublisher { get; } = long.MaxValue - 1;

        /// <summary>
        /// Override and return `true` in order to skip executing tests marked as `Stochastic`.
        /// Such tests MAY sometimes fail even though the implementation
        /// </summary>
        public virtual bool SkipStochasticTests { get; } = false;

        /// <summary>
        /// In order to verify rule 3.3 of the reactive streams spec, this number will be used to check if a
        /// <see cref="ISubscription"/> actually solves the "unbounded recursion" problem by not allowing the number of
        /// recursive calls to exceed the number returned by this property.
        /// 
        /// See https://github.com/reactive-streams/reactive-streams-jvm#3.3
        /// <see cref="Required_spec303_mustNotAllowUnboundedRecursion"/>
        /// </summary>
        public virtual long BoundedDepthOfOnNextAndRequestRecursion { get; } = 1;

        ////////////////////// TEST ENV CLEANUP /////////////////////////////////////

        public void SetUp() => _environment.ClearAsyncErrors();

        ////////////////////// TEST SETUP VERIFICATION //////////////////////////////

        [SkippableFact]
        public void Required_createPublisher1MustProduceAStreamOfExactly1Element()
        {
            Func<IPublisher<T>, ManualSubscriber<T>, Option<T>> requestNextElementOrEndOfStream =
                (publisher, subscriber) =>
                    subscriber.RequestNextElementOrEndOfStream(
                        $"Timeout while waiting for next element from Publisher {publisher}");

            ActivePublisherTest(1, true, publisher =>
            {
                var subscriber = _environment.NewManualSubscriber(publisher);
                Assert.True(requestNextElementOrEndOfStream(publisher, subscriber).HasValue, $"Publisher {publisher} produced no elements");
                subscriber.RequestEndOfStream();
            });
        }

        [SkippableFact]
        public void Required_createPublisher3MustProduceAStreamOfExactly3Elements()
        {
            Func<IPublisher<T>, ManualSubscriber<T>, Option<T>> requestNextElementOrEndOfStream =
                (publisher, subscriber) =>
                    subscriber.RequestNextElementOrEndOfStream(
                        $"Timeout while waiting for next element from Publisher {publisher}");

            ActivePublisherTest(3, true, publisher =>
            {
                var subscriber = _environment.NewManualSubscriber(publisher);
                Assert.True(requestNextElementOrEndOfStream(publisher, subscriber).HasValue, $"Publisher {publisher} produced no elements");
                Assert.True(requestNextElementOrEndOfStream(publisher, subscriber).HasValue, $"Publisher {publisher} produced only 1 element");
                Assert.True(requestNextElementOrEndOfStream(publisher, subscriber).HasValue, $"Publisher {publisher} produced only 3 element");
                subscriber.RequestEndOfStream();
            });
        }

        [SkippableFact]
        public void Required_validate_maxElementsFromPublisher()
            => Assert.True(MaxElementsFromPublisher >= 0, "maxElementsFromPublisher MUST return a number >= 0");

        [SkippableFact]
        public void Required_validate_boundedDepthOfOnNextAndRequestRecursion()
            => Assert.True(BoundedDepthOfOnNextAndRequestRecursion >= 1, "boundedDepthOfOnNextAndRequestRecursion must return a number >= 1");


        ////////////////////// SPEC RULE VERIFICATION ///////////////////////////////

        // Verifies rule: https://github.com/reactive-streams/reactive-streams-jvm#1.1
        [SkippableFact]
        public void Required_spec101_subscriptionRequestMustResultInTheCorrectNumberOfProducedElements()
            => ActivePublisherTest(5, false, publisher =>
            {
                var subscriber = _environment.NewManualSubscriber(publisher);
                try
                {
                    subscriber.ExpectNone($"Publisher {publisher} produced value before the first `Request`: ");
                    subscriber.Request(1);
                    subscriber.NextElement($"Publisher {publisher} produced no element after first `Request`");
                    subscriber.ExpectNone($"Publisher {publisher} produced unrequested: ");

                    subscriber.Request(1);
                    subscriber.Request(2);
                    subscriber.NextElements(3, _environment.DefaultTimeoutMilliseconds,
                        $"Publisher {publisher} produced less than 3 elements after two respective `Request` calls");

                    subscriber.ExpectNone($"Publisher {publisher} produced unrequested ");
                }
                finally
                {
                    subscriber.Cancel();
                }
            });

        // Verifies rule: https://github.com/reactive-streams/reactive-streams-jvm#1.2
        [SkippableFact]
        public void Required_spec102_maySignalLessThanRequestedAndTerminateSubscription()
        {
            const int elements = 3;
            const int requested = 10;

            ActivePublisherTest(elements, true, publisher =>
            {
                var subscriber = _environment.NewManualSubscriber(publisher);
                subscriber.Request(requested);
                subscriber.NextElements(elements);
                subscriber.ExpectCompletion();
            });
        }

        // Verifies rule: https://github.com/reactive-streams/reactive-streams-jvm#1.3
        [SkippableFact]
        public void Stochastic_spec103_mustSignalOnMethodsSequentially()
        {
            const int iterations = 100;
            const int elements = 10;

            StochasticTest(iterations, i =>
            {
                ActivePublisherTest(elements, true, publisher =>
                {
                    var completionLatch = new Latch(_environment);
                    var gotElements = new AtomicCounter(0);

                    publisher.Subscribe(new Spec103Subscriber(_environment, elements, completionLatch, gotElements));

                    completionLatch.ExpectClose(elements * _environment.DefaultTimeoutMilliseconds,
                        $"Failed in iteration {i} of {iterations}. Expected completion signal after signalling {elements} elements (signalled {gotElements.Current}), yet did not receive it");
                });
            });
        }

        private sealed class Spec103Subscriber : ISubscriber<T>
        {
            /// <summary>
            /// Concept wise very similar to a <see cref="Latch"/>, serves to protect
            /// a critical section from concurrent access, with the added benefit of Thread tracking and same-thread-access awareness.
            /// 
            /// Since a <i>Synchronous</i> Publisher may choose to synchronously (using the same <see cref="Thread"/>) call
            /// <see cref="ISubscriber{T}.OnNext(T)"/> directly from either <see cref="IPublisher{T}.Subscribe(ISubscriber{T})"/> or <see cref="ISubscription.Request"/> a plain Latch is not enough
            /// to verify concurrent access safety - one needs to track if the caller is not still using the calling thread
            /// to enter subsequent critical sections ("nesting" them effectively).
            /// </summary>
            private sealed class ConcurrentAccessBarrier
            {
                private readonly Spec103Subscriber _subscriber;
                private readonly AtomicReference<Thread> _currentlySignallingThread = new AtomicReference<Thread>(null);
                private string _previousSignal;

                public ConcurrentAccessBarrier(Spec103Subscriber subscriber)
                {
                    _subscriber = subscriber;
                }

                public void EnterSignal(string signalName)
                {
                    if (!_currentlySignallingThread.CompareAndSet(null, Thread.CurrentThread) && !IsSynchronousSignal())
                    {
                        _subscriber._environment.Flop(
                            "Illegal concurrent access detected (entering critical section)!" +
                            $"{Thread.CurrentThread.Name} emited {signalName} signal, before {_currentlySignallingThread.Value.Name} finished its {_previousSignal} signal");
                    }

                    _previousSignal = signalName;
                }

                public void LeaveSignal(string signalName)
                {
                    _currentlySignallingThread.GetAndSet(null);
                    _previousSignal = signalName;
                }

                private bool IsSynchronousSignal() =>
                    _previousSignal != null &&
                    Thread.CurrentThread.ManagedThreadId == _currentlySignallingThread.Value.ManagedThreadId;
            }

            private readonly TestEnvironment _environment;
            private readonly int _elements;
            private readonly Latch _completionLatch;
            private readonly AtomicCounter _gotElements;
            private readonly ConcurrentAccessBarrier _concurrentAccessBarrier;
            private ISubscription _subscription;

            public Spec103Subscriber(TestEnvironment environment, int elements, Latch completionLatch, AtomicCounter gotElements)
            {
                _environment = environment;
                _elements = elements;
                _completionLatch = completionLatch;
                _gotElements = gotElements;
                _concurrentAccessBarrier = new ConcurrentAccessBarrier(this);
            }

            public void OnNext(T element)
            {
                var signal = $"OnNext({element})";
                _concurrentAccessBarrier.EnterSignal(signal);
                if (_gotElements.IncrementAndGet() <= _elements) // requesting one more than we know are in the stream (some Publishers need this)
                    _subscription.Request(1);

                _concurrentAccessBarrier.LeaveSignal(signal);
            }

            public void OnSubscribe(ISubscription s)
            {
                var signal = "OnSubscribe()";
                _concurrentAccessBarrier.EnterSignal(signal);

                _subscription = s;
                _subscription.Request(1);

                _concurrentAccessBarrier.LeaveSignal(signal);
            }

            public void OnError(Exception cause)
            {
                var signal = $"OnError({cause.Message})";
                _concurrentAccessBarrier.EnterSignal(signal);

                //ignore value

                _concurrentAccessBarrier.LeaveSignal(signal);
            }

            public void OnComplete()
            {
                var signal = "OnComplete()";
                _concurrentAccessBarrier.EnterSignal(signal);

                //entering for completeness

                _concurrentAccessBarrier.LeaveSignal(signal);
                _completionLatch.Close();
            }
        }

        // Verifies rule: https://github.com/reactive-streams/reactive-streams-jvm#1.4
        [SkippableFact]
        public void Optional_spec104_mustSignalOnErrorWhenFails()
        {
            try
            {
                WhenHasErrorPublisherTest(publisher =>
                {
                    var onErrorLatch = new Latch(_environment);
                    var onSubscribeLatch = new Latch(_environment);

                    publisher.Subscribe(new Spec104Subscriber(_environment, onSubscribeLatch, onErrorLatch, publisher));

                    onSubscribeLatch.ExpectClose("Should have received OnSubscribe");
                    onErrorLatch.ExpectClose(
                        $"Error-state Publisher {publisher} did not call `OnError` on new Subscriber");

                    _environment.VerifyNoAsyncErrors();
                });
            }
            catch (SkipException)
            {
                throw;
            }
            catch (Exception ex)
            {
                // we also want to catch AssertionErrors and anything the publisher may have thrown inside subscribe
                // which was wrong of him - he should have signalled on error using onError
                throw new SystemException($"Publisher threw exception ({ex.Message}) instead of signalling error via onError!");
            }
        }

        private sealed class Spec104Subscriber : TestSubscriber<T>
        {
            private readonly Latch _onSubscribeLatch;
            private readonly Latch _onErrorLatch;
            private readonly IPublisher<T> _publisher;

            public Spec104Subscriber(TestEnvironment environment, Latch onSubscribeLatch, Latch onErrorLatch,
                IPublisher<T> publisher) : base(environment)
            {
                _onSubscribeLatch = onSubscribeLatch;
                _onErrorLatch = onErrorLatch;
                _publisher = publisher;
            }

            public override void OnSubscribe(ISubscription subscription)
            {
                _onSubscribeLatch.AssertOpen("Only one onSubscribe call expected");
                _onSubscribeLatch.Close();
            }

            public override void OnError(Exception cause)
            {
                _onSubscribeLatch.AssertClosed("onSubscribe should be called prior to onError always");
                _onErrorLatch.AssertOpen($"Error-state Publisher {_publisher} called `onError` twice on new Subscriber");
                _onErrorLatch.Close();
            }
        }

        // Verifies rule: https://github.com/reactive-streams/reactive-streams-jvm#1.5
        [SkippableFact]
        public void Required_spec105_mustSignalOnCompleteWhenFiniteStreamTerminates()
            => ActivePublisherTest(3, true, publisher =>
            {
                var sub = _environment.NewManualSubscriber(publisher);
                sub.RequestNextElement();
                sub.RequestNextElement();
                sub.RequestNextElement();
                sub.RequestEndOfStream();
                sub.ExpectNone();
            });

        // Verifies rule: https://github.com/reactive-streams/reactive-streams-jvm#1.5
        [SkippableFact]
        public void Optional_spec105_emptyStreamMustTerminateBySignallingOnComplete()
            => OptionalActivePublisherTest(0, true, publisher =>
            {
                var subscriber = _environment.NewManualSubscriber(publisher);
                subscriber.Request(1);
                subscriber.ExpectCompletion();
                subscriber.ExpectNone();
            });

        // Verifies rule: https://github.com/reactive-streams/reactive-streams-jvm#1.6
        [SkippableFact]
        public void Untested_spec106_mustConsiderSubscriptionCancelledAfterOnErrorOrOnCompleteHasBeenCalled()
            => NotVerified(); // not really testable without more control over the Publisher

        // Verifies rule: https://github.com/reactive-streams/reactive-streams-jvm#1.7
        [SkippableFact]
        public void Required_spec107_mustNotEmitFurtherSignalsOnceOnCompleteHasBeenSignalled()
            => ActivePublisherTest(1, true, publisher =>
            {
                var subscriber = _environment.NewManualSubscriber(publisher);
                subscriber.Request(10);
                subscriber.NextElement();
                subscriber.ExpectCompletion();

                subscriber.Request(10);
                subscriber.ExpectNone();
            });

        // Verifies rule: https://github.com/reactive-streams/reactive-streams-jvm#1.7
        [SkippableFact]
        public void Untested_spec107_mustNotEmitFurtherSignalsOnceOnErrorHasBeenSignalled()
            => NotVerified(); // can we meaningfully test this, without more control over the publisher?

        // Verifies rule: https://github.com/reactive-streams/reactive-streams-jvm#1.8
        [SkippableFact]
        public void Untested_spec108_possiblyCanceledSubscriptionShouldNotReceiveOnErrorOrOnCompleteSignals()
            => NotVerified(); // can we meaningfully test this?

        // Verifies rule: https://github.com/reactive-streams/reactive-streams-jvm#1.9
        [SkippableFact]
        public void Untested_spec109_subscribeShouldNotThrowNonFatalThrowable()
            => NotVerified(); // can we meaningfully test this?

        // Verifies rule: https://github.com/reactive-streams/reactive-streams-jvm#1.9
        [SkippableFact]
        public void Required_spec109_subscribeThrowNPEOnNullSubscriber()
            => ActivePublisherTest(0, false, publisher =>
            {
                try
                {
                    publisher.Subscribe(null);
                    _environment.Flop(
                        "Publisher did not throw a ArgumentNullException when given a null Subscribe in subscribe");
                }
                catch (ArgumentNullException)
                {
                    // valid behaviour
                }
                _environment.VerifyNoAsyncErrorsNoDelay();
            });


        // Verifies rule: https://github.com/reactive-streams/reactive-streams-jvm#1.9
        [SkippableFact]
        public void Required_spec109_mustIssueOnSubscribeForNonNullSubscriber()
            => ActivePublisherTest(0, false, publisher =>
            {
                var onSubscriberLatch = new Latch(_environment);
                var subscriber = new Spec109Subscriber(onSubscriberLatch);
                try
                {
                    publisher.Subscribe(subscriber);
                    onSubscriberLatch.ExpectClose("Should have received OnSubscribe");
                    _environment.VerifyNoAsyncErrorsNoDelay();
                }
                finally
                {
                    subscriber.Cancel();
                }
            });

        private class Spec109Subscriber : ISubscriber<T>
        {
            private readonly Latch _onSubscriberLatch;

            ISubscription upstream;

            public Spec109Subscriber(Latch onSubscriberLatch)
            {
                _onSubscriberLatch = onSubscriberLatch;
            }

            public void Cancel()
            {
                Interlocked.Exchange(ref upstream, null)?.Cancel();
            }

            public void OnNext(T element)
                => _onSubscriberLatch.AssertClosed("OnSubscribe should be called prior to OnNext always");

            public void OnSubscribe(ISubscription subscription)
            {
                Interlocked.Exchange(ref upstream, subscription);
                _onSubscriberLatch.AssertOpen("Only one OnSubscribe call expected");
                _onSubscriberLatch.Close();
            }

            public void OnError(Exception cause)
                => _onSubscriberLatch.AssertClosed("OnSubscribe should be called prior to OnError always");

            public void OnComplete()
                => _onSubscriberLatch.AssertClosed("OnSubscribe should be called prior to OnComplete always");
        }

        // Verifies rule: https://github.com/reactive-streams/reactive-streams-jvm#1.9
        [SkippableFact]
        public void Required_spec109_mayRejectCallsToSubscribeIfPublisherIsUnableOrUnwillingToServeThemRejectionMustTriggerOnErrorAfterOnSubscribe()
            => WhenHasErrorPublisherTest(publisher =>
            {
                var onErrorLatch = new Latch(_environment);
                var onSubscribeLatch = new Latch(_environment);

                publisher.Subscribe(new Spec109ManualSubscriber(_environment, onErrorLatch, onSubscribeLatch));

                onSubscribeLatch.ExpectClose("Should have received OnSubscribe");
                onErrorLatch.ExpectClose("Should have received OnError");

                _environment.VerifyNoAsyncErrorsNoDelay();
            });

        private class Spec109ManualSubscriber : ManualSubscriberWithSubscriptionSupport<T>
        {
            private readonly Latch _onErrorLatch;
            private readonly Latch _onSubscribeLatch;

            public Spec109ManualSubscriber(TestEnvironment environment, Latch onErrorLatch, Latch onSubscribeLatch)
                : base(environment)
            {
                _onErrorLatch = onErrorLatch;
                _onSubscribeLatch = onSubscribeLatch;
            }

            public override void OnError(Exception cause)
            {
                _onSubscribeLatch.AssertClosed("onSubscribe should be called prior to onError always");
                _onErrorLatch.AssertOpen("Only one onError call expected");
                _onErrorLatch.Close();
            }

            public override void OnSubscribe(ISubscription subscription)
            {
                _onSubscribeLatch.AssertOpen("Only one onSubscribe call expected");
                _onSubscribeLatch.Close();
            }
        }

        // Verifies rule: https://github.com/reactive-streams/reactive-streams-jvm#1.10
        [SkippableFact]
        public void Untested_spec110_rejectASubscriptionRequestIfTheSameSubscriberSubscribesTwice()
            => NotVerified(); // can we meaningfully test this?

        // Verifies rule: https://github.com/reactive-streams/reactive-streams-jvm#1.11
        [SkippableFact]
        public void Optional_spec111_maySupportMultiSubscribe()
            => OptionalActivePublisherTest(1, false, publisher =>
            {
                var sub1 = _environment.NewManualSubscriber(publisher);
                var sub2 = _environment.NewManualSubscriber(publisher);
                try
                {
                    _environment.VerifyNoAsyncErrors();
                }
                finally
                {
                    try
                    {
                        sub1.Cancel();
                    }
                    finally
                    {
                        sub2.Cancel();
                    }
                }
            });

        // Verifies rule: https://github.com/reactive-streams/reactive-streams-jvm#1.11
        [SkippableFact]
        public void Optional_spec111_multicast_mustProduceTheSameElementsInTheSameSequenceToAllOfItsSubscribersWhenRequestingOneByOne()
            => OptionalActivePublisherTest(5, true, publisher =>
            {
                var sub1 = _environment.NewManualSubscriber(publisher);
                var sub2 = _environment.NewManualSubscriber(publisher);
                var sub3 = _environment.NewManualSubscriber(publisher);

                sub1.Request(1);
                var x1 = sub1.NextElement($"Publisher {publisher} did not produce the requested 1 element on 1st subscriber");

                sub2.Request(2);
                var y1 = sub2.NextElements(2, $"Publisher {publisher} did not produce the requested 2 elements on 2nd subscriber");

                sub1.Request(1);
                var x2 = sub1.NextElement($"Publisher {publisher} did not produce the requested 1 element on 1st subscriber");

                sub3.Request(3);
                var z1 = sub3.NextElements(3, $"Publisher {publisher} did not produce the requested 3 elements on 3nd subscriber");

                sub3.Request(1);
                var z2 = sub3.NextElement($"Publisher {publisher} did not produce the requested 1 element on 3nd subscriber");

                sub3.Request(1);
                var z3 = sub3.NextElement($"Publisher {publisher} did not produce the requested 3 element on 3nd subscriber");
                sub3.RequestEndOfStream($"Publisher {publisher} did not complete the stream as expected on 3rd subscriber");

                sub2.Request(3);
                var y2 = sub2.NextElements(3, $"Publisher {publisher} did not produce the requested 3 elements on 2nd subscriber");
                sub2.RequestEndOfStream($"Publisher {publisher} did not complete the stream as expected on 2nd subscriber");

                sub1.Request(2);
                var x3 = sub1.NextElements(2, $"Publisher {publisher} did not produce the requested 2 elements on 1st subscriber");

                sub1.Request(1);
                var x4 = sub1.NextElement($"Publisher {publisher} did not produce the requested 1 element on 1st subscriber");
                sub1.RequestEndOfStream($"Publisher {publisher} did not complete the stream as expected on 1st subscriber");

                var r = new List<T> { x1, x2 };
                r.AddRange(x3);
                r.Add(x4);

                var check1 = y1;
                check1.AddRange(y2);

                //noinspection unchecked
                var check2 = z1;
                check2.Add(z2);
                check2.Add(z3);

                r.Should().BeEquivalentTo(check1, $"Publisher {publisher} did not produce the same element sequence for subscribers 1 and 2");
                r.Should().BeEquivalentTo(check2, $"Publisher {publisher} did not produce the same element sequence for subscribers 1 and 3");
            });


        // Verifies rule: https://github.com/reactive-streams/reactive-streams-jvm#1.11
        [SkippableFact]
        public void Optional_spec111_multicast_mustProduceTheSameElementsInTheSameSequenceToAllOfItsSubscribersWhenRequestingManyUpfront()
            => OptionalActivePublisherTest(3, false, publisher =>
            {
                var sub1 = _environment.NewManualSubscriber(publisher);
                var sub2 = _environment.NewManualSubscriber(publisher);
                var sub3 = _environment.NewManualSubscriber(publisher);

                // if the publisher must touch it's source to notice it's been drained, the OnComplete won't come until we ask for more than it actually contains...
                // edgy edge case?
                sub1.Request(4);
                sub2.Request(4);
                sub3.Request(4);

                var received1 = sub1.NextElements(3);
                var received2 = sub2.NextElements(3);
                var received3 = sub3.NextElements(3);

                // NOTE: can't check completion, the Publisher may not be able to signal it
                //       a similar test *with* completion checking is implemented

                received1.Should().BeEquivalentTo(received2,
                    "Expected elements to be signaled in the same sequence to 1st and 2nd subscribers");
                received2.Should().BeEquivalentTo(received3,
                    "Expected elements to be signaled in the same sequence to 2st and 3nd subscribers");
            });

        // Verifies rule: https://github.com/reactive-streams/reactive-streams-jvm#1.11
        [SkippableFact]
        public void Optional_spec111_multicast_mustProduceTheSameElementsInTheSameSequenceToAllOfItsSubscribersWhenRequestingManyUpfrontAndCompleteAsExpected()
            => OptionalActivePublisherTest(3, true, publisher =>
            {
                var sub1 = _environment.NewManualSubscriber(publisher);
                var sub2 = _environment.NewManualSubscriber(publisher);
                var sub3 = _environment.NewManualSubscriber(publisher);

                // if the publisher must touch it's source to notice it's been drained, the OnComplete won't come until we ask for more than it actually contains...
                // edgy edge case?
                sub1.Request(4);
                sub2.Request(4);
                sub3.Request(4);

                var received1 = sub1.NextElements(3);
                var received2 = sub2.NextElements(3);
                var received3 = sub3.NextElements(3);

                sub1.ExpectCompletion();
                sub2.ExpectCompletion();
                sub3.ExpectCompletion();

                received1.Should().BeEquivalentTo(received2,
                    "Expected elements to be signaled in the same sequence to 1st and 2nd subscribers");
                received2.Should().BeEquivalentTo(received3,
                    "Expected elements to be signaled in the same sequence to 2st and 3nd subscribers");
            });

        ///////////////////// SUBSCRIPTION TESTS //////////////////////////////////

        // Verifies rule: https://github.com/reactive-streams/reactive-streams-jvm#3.2
        [SkippableFact]
        public void Required_spec302_mustAllowSynchronousRequestCallsFromOnNextAndOnSubscribe()
            => ActivePublisherTest(6, false, publisher =>
            {
                _environment.Subscribe(publisher, new Spec302Subscriber(_environment));
                _environment.VerifyNoAsyncErrors();
            });

        private class Spec302Subscriber : ManualSubscriberWithSubscriptionSupport<T>
        {
            public Spec302Subscriber(TestEnvironment environment) : base(environment)
            {
            }

            public override void OnSubscribe(ISubscription subscription)
            {
                Subscription.CompleteImmediatly(subscription);

                subscription.Request(1);
                subscription.Request(1);
                subscription.Request(1);
            }

            public override void OnNext(T element)
            {
                var subscription = Subscription.Value;
                subscription.Request(1);
            }
        }

        // Verifies rule: https://github.com/reactive-streams/reactive-streams-jvm#3.3
        [SkippableFact]
        public void Required_spec303_mustNotAllowUnboundedRecursion()
        {
            var oneMoreThanBoundedLimit = BoundedDepthOfOnNextAndRequestRecursion + 1;

            ActivePublisherTest(oneMoreThanBoundedLimit, false, publisher =>
            {
                var stackDephtCounter = new ThreadLocal<long>(() => 0);
                var runCompleted = new Latch(_environment);
                var subscriber = new Spec303Subscriber(_environment, stackDephtCounter, runCompleted,
                    BoundedDepthOfOnNextAndRequestRecursion);

                try
                {
                    _environment.Subscribe(publisher, subscriber);

                    subscriber.Request(1); // kick-off the `request -> onNext -> request -> onNext -> ...`

                    var msg = "Unable to validate call stack depth safety, " +
                              $"awaited at-most {oneMoreThanBoundedLimit} signals (`MaxOnNextSignalsInRecursionTest`) or completion";

                    runCompleted.ExpectClose(_environment.DefaultTimeoutMilliseconds, msg);
                    _environment.VerifyNoAsyncErrorsNoDelay();
                }
                finally
                {
                    // since the request/onNext recursive calls may keep the publisher running "forever",
                    // we MUST cancel it manually before exiting this test case
                    subscriber.Cancel();
                }
            });
        }

        private class Spec303Subscriber : ManualSubscriberWithSubscriptionSupport<T>
        {
            private readonly ThreadLocal<long> _stackDephtCounter;
            private readonly Latch _runCompleted;
            private readonly long _boundedDepthOfOnNextAndRequestRecursion;

            // counts the number of signals received, used to break out from possibly infinite request/onNext loops
            private long _signalsReceived;

            public Spec303Subscriber(TestEnvironment environment, ThreadLocal<long> stackDephtCounter,
                Latch runCompleted, long boundedDepthOfOnNextAndRequestRecursion) : base(environment)
            {
                _stackDephtCounter = stackDephtCounter;
                _runCompleted = runCompleted;
                _boundedDepthOfOnNextAndRequestRecursion = boundedDepthOfOnNextAndRequestRecursion;
            }

            public override void OnNext(T element)
            {
                // NOT calling super.onNext as this test only cares about stack depths, not the actual values of elements
                // which also simplifies this test as we do not have to drain the test buffer, which would otherwise be in danger of overflowing

                _signalsReceived++;
                _stackDephtCounter.Value++;
                Environment.Debug($"{this}(recursion depth: {_stackDephtCounter.Value})::OnNext({element})");

                var callsUntilNow = _stackDephtCounter.Value;
                if (callsUntilNow > _boundedDepthOfOnNextAndRequestRecursion)
                {
                    Environment.Flop(
                        $"Got {callsUntilNow} OnNext calls within thread: {Thread.CurrentThread}, yet expected recursive bound was {_boundedDepthOfOnNextAndRequestRecursion}");

                    // stop the recursive call chain
                    _runCompleted.Close();
                    return;
                }
                if (_signalsReceived >= _boundedDepthOfOnNextAndRequestRecursion + 1)
                {
                    // since max number of signals reached, and recursion depth not exceeded, we judge this as a success and
                    // stop the recursive call chain
                    _runCompleted.Close();
                    return;
                }

                // request more right away, the Publisher must break the recursion
                Subscription.Value.Request(1);

                _stackDephtCounter.Value--;
            }

            public override void OnComplete()
            {
                base.OnComplete();
                _runCompleted.Close();
            }

            public override void OnError(Exception cause)
            {
                base.OnError(cause);
                _runCompleted.Close();
            }
        }

        // Verifies rule: https://github.com/reactive-streams/reactive-streams-jvm#3.4
        [SkippableFact]
        public void Untested_spec304_requestShouldNotPerformHeavyComputations()
            => NotVerified();  // cannot be meaningfully tested, or can it?

        // Verifies rule: https://github.com/reactive-streams/reactive-streams-jvm#3.5
        [SkippableFact]
        public void Untested_spec305_cancelMustNotSynchronouslyPerformHeavyCompuatation()
            => NotVerified();  // cannot be meaningfully tested, or can it?

        // Verifies rule: https://github.com/reactive-streams/reactive-streams-jvm#3.6
        [SkippableFact]
        public void Required_spec306_afterSubscriptionIsCancelledRequestMustBeNops()
            => ActivePublisherTest(3, false, publisher =>
            {
                var subscriber = new Spec306Subscriber(_environment);
                _environment.Subscribe(publisher, subscriber);

                subscriber.Cancel();
                subscriber.Request(1);
                subscriber.Request(1);
                subscriber.Request(1);

                subscriber.ExpectNone();
                _environment.VerifyNoAsyncErrorsNoDelay();
            });

        private sealed class Spec306Subscriber : ManualSubscriberWithSubscriptionSupport<T>
        {
            public Spec306Subscriber(TestEnvironment environment) : base(environment)
            {
            }

            // override because by default a ManualSubscriber will drop the
            // subscription once it's cancelled (as expected).
            // In this test however it must keep the cancelled Subscription and keep issuing `request(long)` to it.
            public override void Cancel()
            {
                if (Subscription.IsCompleted())
                    Subscription.Value.Cancel();
                else
                    Environment.Flop("Cannot cancel a subscription before having received it");
            }
        }

        // Verifies rule: https://github.com/reactive-streams/reactive-streams-jvm#3.7
        [SkippableFact]
        public void Required_spec307_afterSubscriptionIsCancelledAdditionalCancelationsMustBeNops()
            => ActivePublisherTest(1, false, publisher =>
            {
                var subscriber = _environment.NewManualSubscriber(publisher);

                // leak the Subscription
                var subscription = subscriber.Subscription.Value;

                subscription.Cancel();
                subscription.Cancel();
                subscription.Cancel();

                subscriber.ExpectNone();
                _environment.VerifyNoAsyncErrorsNoDelay();
            });

        // Verifies rule: https://github.com/reactive-streams/reactive-streams-jvm#3.9
        [SkippableFact]
        public void Required_spec309_requestZeroMustSignalIllegalArgumentException()
            => ActivePublisherTest(10, false, publisher =>
            {
                var subscriber = _environment.NewManualSubscriber(publisher);
                subscriber.Request(0);
                subscriber.ExpectErrorWithMessage<ArgumentException>("3.9");  // we do require implementations to mention the rule number at the very least
            });

        // Verifies rule: https://github.com/reactive-streams/reactive-streams-jvm#3.9
        [SkippableFact]
        public void Required_spec309_requestNegativeNumberMustSignalIllegalArgumentException()
            => ActivePublisherTest(10, false, publisher =>
            {
                var subscriber = _environment.NewManualSubscriber(publisher);
                var random = new Random();
                subscriber.Request(-random.Next(int.MaxValue));
                subscriber.ExpectErrorWithMessage<ArgumentException>("3.9");  // we do require implementations to mention the rule number at the very least
            });

        // Verifies rule: https://github.com/reactive-streams/reactive-streams-jvm#3.12
        [SkippableFact]
        public void Required_spec312_cancelMustMakeThePublisherToEventuallyStopSignaling()
        {
            // the publisher is able to signal more elements than the subscriber will be requesting in total
            const int publisherElements = 20;
            const int demand1 = 10;
            const int demand2 = 5;
            const int totalDemand = demand1 + demand2;

            ActivePublisherTest(publisherElements, false, publisher =>
            {
                var subscriber = _environment.NewManualSubscriber(publisher);

                subscriber.Request(demand1);
                subscriber.Request(demand2);

                /*
                  NOTE: The order of the nextElement/cancel calls below is very important (!)
                  If this ordering was reversed, given an asynchronous publisher,
                  the following scenario would be *legal* and would break this test:
                  > AsyncPublisher receives request(10) - it does not emit data right away, it's asynchronous
                  > AsyncPublisher receives request(5) - demand is now 15
                  ! AsyncPublisher didn't emit any onNext yet (!)
                  > AsyncPublisher receives cancel() - handles it right away, by "stopping itself" for example
                  ! cancel was handled hefore the AsyncPublisher ever got the chance to emit data
                  ! the subscriber ends up never receiving even one element - the test is stuck (and fails, even on valid Publisher)
                  Which is why we must first expect an element, and then cancel, once the producing is "running".                 
                 */

                subscriber.NextElement();
                subscriber.Cancel();

                var onNextSignalled = 1;
                bool stillbeeingSignalled;
                do
                {
                    //put asyncError if onNext signal received
                    subscriber.ExpectNone();
                    var error = _environment.DropAsyncError();

                    if (error == null)
                        stillbeeingSignalled = false;
                    else
                    {
                        onNextSignalled++;
                        stillbeeingSignalled = true;
                    }

                    // if the Publisher tries to emit more elements than was requested (and/or ignores cancellation) this will throw
                    Assert.True(onNextSignalled <= totalDemand,
                        $"Publisher signalled {onNextSignalled} elements, which is more than the signalled demand: {totalDemand}");
                } while (stillbeeingSignalled);
            });

            _environment.VerifyNoAsyncErrorsNoDelay();
        }

        // Verifies rule: https://github.com/reactive-streams/reactive-streams-jvm#3.13
        [SkippableFact]
        public void Required_spec313_cancelMustMakeThePublisherEventuallyDropAllReferencesToTheSubscriber()
        {
            Func<IPublisher<T>, WeakReference<ManualSubscriber<T>>> run = publisher =>
            {
                var subscriber = _environment.NewManualSubscriber(publisher);
                var reference = new WeakReference<ManualSubscriber<T>>(subscriber);

                subscriber.Request(1);
                subscriber.NextElement();
                subscriber.Cancel();

                return reference;
            };

            ActivePublisherTest(3, false, publisher =>
            {
                var reference = run(publisher);

                // cancel may be run asynchronously so we add a sleep before running the GC
                // to "resolve" the race
                Thread.Sleep(TimeSpan.FromMilliseconds(_publisherReferenceGcTimeoutMillis));
                GC.Collect();

                ManualSubscriber<T> tmp;
                if (reference.TryGetTarget(out tmp))
                    _environment.Flop($"Publisher {publisher} did not drop reference to test subscriber after subscription cancellation");

                _environment.VerifyNoAsyncErrorsNoDelay();
            });
        }

        // Verifies rule: https://github.com/reactive-streams/reactive-streams-jvm#3.17
        [SkippableFact]
        public void Required_spec317_mustSupportAPendingElementCountUpToLongMaxValue()
        {
            const int totalElements = 3;

            ActivePublisherTest(totalElements, true, publisher =>
            {
                var subscriber = _environment.NewManualSubscriber(publisher);
                subscriber.Request(long.MaxValue);

                subscriber.NextElements(totalElements);
                subscriber.ExpectCompletion();

                _environment.VerifyNoAsyncErrorsNoDelay();
            });
        }

        // Verifies rule: https://github.com/reactive-streams/reactive-streams-jvm#3.17
        [SkippableFact]
        public void Required_spec317_mustSupportACumulativePendingElementCountUpToLongMaxValue()
        {
            const int totalElements = 3;

            ActivePublisherTest(totalElements, true, publisher =>
            {
                var subscriber = _environment.NewManualSubscriber(publisher);
                subscriber.Request(long.MaxValue / 2); // pending = Long.MAX_VALUE / 2
                subscriber.Request(long.MaxValue / 2); // pending = Long.MAX_VALUE - 1
                subscriber.Request(1); // pending = Long.MAX_VALUE

                subscriber.NextElements(totalElements);
                subscriber.ExpectCompletion();

                try
                {
                    _environment.VerifyNoAsyncErrorsNoDelay();
                }
                finally
                {
                    subscriber.Cancel();
                }
            });
        }

        // Verifies rule: https://github.com/reactive-streams/reactive-streams-jvm#3.17
        [SkippableFact]
        public void Required_spec317_mustNotSignalOnErrorWhenPendingAboveLongMaxValue()
            => ActivePublisherTest(int.MaxValue, false, publisher =>
            {
                var subscriber = new Spec317Subscriber(_environment);
                _environment.Subscribe(publisher, subscriber);

                // eventually triggers `OnNext`, which will then trigger up to `callsCounter` times `request(long.MaxValue - 1)`
                // we're pretty sure to overflow from those
                subscriber.Request(1);

                // no onError should be signalled
                try
                {
                    _environment.VerifyNoAsyncErrors();
                }
                finally
                {
                    subscriber.Cancel();
                }
            });

        private sealed class Spec317Subscriber : BlackholeSubscriberWithSubscriptionSupport<T>
        {
            // arbitrarily set limit on nuber of request calls signalled, we expect overflow after already 2 calls,
            // so 10 is relatively high and safe even if arbitrarily chosen
            private int _callsCounter = 10;

            public Spec317Subscriber(TestEnvironment environment) : base(environment)
            {
            }

            public override void OnNext(T element)
            {
                Environment.Debug($"{this}.OnNext({element})");
                if (Subscription.IsCompleted())
                {
                    if (_callsCounter > 0)
                    {
                        Subscription.Value.Request(long.MaxValue - 1);
                        _callsCounter--;
                    }
                    else
                        Subscription.Value.Cancel();
                }
                else
                    Environment.Flop($"Subscriber.OnNext({element}) called before Subscriber.OnSubscribe");
            }
        }

        ///////////////////// ADDITIONAL "COROLLARY" TESTS ////////////////////////

        ///////////////////// TEST INFRASTRUCTURE /////////////////////////////////

        /// <summary>
        /// Test for feature that SHOULD/MUST be implemented, using a live publisher.
        /// </summary>
        /// <param name="elements">the number of elements the Publisher under test  must be able to emit to run this test</param>
        /// <param name="completionSignalRequired"> true if an onComplete signal is required by this test to run.
        /// If the tested Publisher is unable to signal completion, tests requireing onComplete signals will be skipped.
        /// To signal if your Publisher is able to signal completion see <see cref="MaxElementsFromPublisher"/>.</param>
        /// <param name="run">The actual test to run</param>
        public void ActivePublisherTest(long elements, bool completionSignalRequired, Action<IPublisher<T>> run)
        {
            if (elements > MaxElementsFromPublisher)
                TckAssert.Skip($"Uable to run this test as required elements nr : {elements} is higher than supported by given producer {MaxElementsFromPublisher}");
            if (completionSignalRequired && MaxElementsFromPublisher == long.MaxValue)
                TckAssert.Skip("Unable to run this test, as it requires an onComplete signal, which this Publisher is unable to provide (as signalled by returning long.MaxValue from `MaxElementsFromPublisher");

            var publisher = CreatePublisher(elements);
            run(publisher);
            _environment.VerifyNoAsyncErrorsNoDelay();
        }

        /// <summary>
        /// Test for feature that MAY be implemented. This test will be marked as SKIPPED if it fails.
        /// </summary>
        /// <param name="elements">the number of elements the Publisher under test  must be able to emit to run this test</param>
        /// <param name="completionSignalRequired">true if an onComplete signal is required by this test to run.
        /// If the tested Publisher is unable to signal completion, tests requireing onComplete signals will be skipped.
        /// To signal if your Publisher is able to signal completion see <see cref="MaxElementsFromPublisher"/>.</param>
        /// <param name="run">The actual test to run</param>
        public void OptionalActivePublisherTest(long elements, bool completionSignalRequired, Action<IPublisher<T>> run)
        {
            if (elements > MaxElementsFromPublisher)
                TckAssert.Skip($"Uable to run this test as required elements nr : {elements} is higher than supported by given producer {MaxElementsFromPublisher}");
            if (completionSignalRequired && MaxElementsFromPublisher == long.MaxValue)
                TckAssert.Skip("Unable to run this test, as it requires an onComplete signal, which this Publisher is unable to provide (as signalled by returning long.MaxValue from `MaxElementsFromPublisher");

            var publisher = CreatePublisher(elements);
            var skipMessage = "Skipped because tested publisher does NOT implement this OPTIONAL requirement.";

            try
            {
                PotentiallyPendingTest(publisher, run);
            }
            catch (AssertionException ex)
            {
                NotVerified(skipMessage + "Reason for skipping was: " + ex.Message);
            }
            /*
            catch (Exception)
            {
                NotVerified(skipMessage);
            }
            */
        }

        public const string SkippingNoErrorPublisherAvailable =
            @"Skipping because no error state Publisher provided, and the test requires it. " +
            "Please implement PublisherVerification#createFailedPublisher to run this test.";

        public const string SkippingOptionalTestFailed =
            "Skipping, because provided Publisher does not pass this *additional* verification";

        /// <summary>
        /// Additional test for Publisher in error state
        /// </summary>
        public void WhenHasErrorPublisherTest(Action<IPublisher<T>> run)
            => PotentiallyPendingTest(CreateFailedPublisher(), run, SkippingNoErrorPublisherAvailable);

        public void PotentiallyPendingTest(IPublisher<T> publisher, Action<IPublisher<T>> run)
            => PotentiallyPendingTest(publisher, run, SkippingOptionalTestFailed);

        public void PotentiallyPendingTest(IPublisher<T> publisher, Action<IPublisher<T>> run, string message)
        {
            if (publisher != null)
                run(publisher);
            else
                TckAssert.Skip(message);
        }

        /// <summary>
        /// Executes a given test body <paramref name="n"/> times.
        /// All the test runs must pass in order for the stochastic test to pass.
        /// </summary>
        public void StochasticTest(int n, Action<int> body)
        {
            if (SkipStochasticTests)
                TckAssert.Skip("Skipping @Stochastic test because `SkipStochasticTests` returned `true`!");

            for (var i = 0; i < n; i++)
                body(i);
        }

        public void NotVerified() => NotVerified("Not verified by this TCK.");

        public void NotVerified(string message) => TckAssert.Skip(message);

        /// <summary>
        /// Return this value from <see cref="MaxElementsFromPublisher"/> to mark that the given <see cref="IPublisher{T}"/>,
        /// is not able to signal completion. For example it is strictly a time-bound or unbounded source of data.
        /// 
        /// Returning this value from <see cref="MaxElementsFromPublisher"/> will result in skipping all TCK tests which require onComplete signals!
        /// </summary>
        public long PublisherUnableToSignalOnComplete { get; } = long.MaxValue;
    }
}
