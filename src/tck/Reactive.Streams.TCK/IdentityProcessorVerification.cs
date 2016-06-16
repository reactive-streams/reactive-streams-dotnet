using System;
using System.Collections.Generic;
using NUnit.Framework;
using Reactive.Streams.TCK.Support;

namespace Reactive.Streams.TCK
{
    public abstract class IdentityProcessorVerification<T> : WithHelperPublisher<T>,
        ISubscriberWhiteboxVerificationRules, IPublisherVerificationRules
    {
        private readonly TestEnvironment _environment;

        ////////////////////// DELEGATED TO SPECS //////////////////////

        // for delegating tests
        private readonly SubscriberWhiteboxVerification<T> _subscriberVerification;

        // for delegating tests
        private readonly PublisherVerification<T> _publisherVerification;

        ////////////////// END OF DELEGATED TO SPECS //////////////////

        /// <summary>
        /// number of elements the processor under test must be able ot buffer,
        /// without dropping elements. Defaults to <see cref="TestEnvironment.TestBufferSize"/>.
        /// </summary>
        private readonly int _processorBufferSize;

        /// <summary>
        /// Test class must specify the expected time it takes for the publisher to
        /// shut itself down when the the last downstream <see cref="ISubscription"/> is cancelled.
        /// 
        /// The processor will be required to be able to buffer <see cref="TestEnvironment.TestBufferSize"/> elements.
        /// </summary>
        protected IdentityProcessorVerification(TestEnvironment environment)
            : this(environment, PublisherVerification<T>.EnvironmentPublisherReferenceGcTimeoutMilliseconds())
        {
        }

        /// <summary>
        /// Test class must specify the expected time it takes for the publisher to
        /// shut itself down when the the last downstream <see cref="ISubscription"/> is cancelled.
        /// 
        /// The processor will be required to be able to buffer <see cref="TestEnvironment.TestBufferSize"/> elements.
        /// </summary>
        /// <param name="environment">The test environment</param>
        /// <param name="publisherReferenceGcTimeoutMillis"> used to determine after how much time a reference to a Subscriber should be already dropped by the Publisher.</param>
        /// <param name="processorBufferSize"> number of elements the processor is required to be able to buffer. Default <see cref=" TestEnvironment.TestBufferSize"/></param>
        protected IdentityProcessorVerification(TestEnvironment environment, long publisherReferenceGcTimeoutMillis, int processorBufferSize = TestEnvironment.TestBufferSize)
        {
            _environment = environment;
            _processorBufferSize = processorBufferSize;
            _subscriberVerification = new IdentifierWhiteboxVerification(this);
            _publisherVerification = new IdentifierPublisherVerification(this, publisherReferenceGcTimeoutMillis);
        }

        private sealed class IdentifierPublisherVerification : PublisherVerification<T>
        {
            private readonly IdentityProcessorVerification<T> _processor;

            public IdentifierPublisherVerification(IdentityProcessorVerification<T> processor,
                long publisherReferenceGcTimeoutMillis)
                : base(processor._environment, publisherReferenceGcTimeoutMillis)
            {
                _processor = processor;
                MaxElementsFromPublisher = _processor.MaxElementsFromPublisher;
                BoundedDepthOfOnNextAndRequestRecursion = _processor.BoundedDepthOfOnNextAndRequestRecursion;
                SkipStochasticTests = _processor.SkipStochasticTests;
            }

            public override IPublisher<T> CreatePublisher(long elements) => _processor.CreatePublisher(elements);

            public override IPublisher<T> CreateFailedPublisher() => _processor.CreateFailedPublisher();

            public override long MaxElementsFromPublisher { get; }

            public override long BoundedDepthOfOnNextAndRequestRecursion { get; }

            public override bool SkipStochasticTests { get; }
        }

        private sealed class IdentifierWhiteboxVerification : SubscriberWhiteboxVerification<T>
        {
            private readonly IdentityProcessorVerification<T> _processor;

            public IdentifierWhiteboxVerification(IdentityProcessorVerification<T> processor) : base(processor._environment)
            {
                _processor = processor;
            }

            public override T CreateElement(int element) => _processor.CreateElement(element);

            public override ISubscriber<T> CreateSubscriber(WhiteboxSubscriberProbe<T> probe)
                => _processor.CreateSubscriber(probe);

            public override IPublisher<T> CreateHelperPublisher(long elements) => _processor.CreateHelperPublisher(elements);
        }

        /// <summary>
        /// This is the main method you must implement in your test incarnation.
        /// It must create a Publisher, which simply forwards all stream elements from its upstream
        /// to its downstream. It must be able to internally buffer the given number of elements.
        /// </summary>
        /// <param name="bufferSize">number of elements the processor is required to be able to buffer.</param>
        public abstract IProcessor<T, T> CreateIdentityProcessor(int bufferSize);

        /// <summary>
        /// By implementing this method, additional TCK tests concerning a "failed" publishers will be run.
        /// 
        /// The expected behaviour of the <see cref="IPublisher{T}"/> returned by this method is hand out a subscription,
        /// ollowed by signalling OnError on it, as specified by Rule 1.9.
        /// 
        /// If you ignore these additional tests, return null from this method.
        /// </summary>
        /// <returns></returns>
        public abstract IPublisher<T> CreateFailedPublisher();

        /// <summary>
        /// Override and return lower value if your Publisher is only able to produce a known number of elements.
        /// For example, if it is designed to return at-most-one element, return 1 from this method.
        /// 
        /// Defaults to <see cref="long.MaxValue"/> - 1, meaning that the Publisher can be produce a huge but NOT an unbounded number of elements.
        /// 
        /// To mark your Publisher will *never* signal an OnComplete override this method and return <see cref="long.MaxValue"/>,
        /// which will result in *skipping all tests which require an onComplete to be triggered* (!).
        /// </summary>
        public virtual long MaxElementsFromPublisher { get; } = long.MaxValue - 1;

        /// <summary>
        /// In order to verify rule 3.3 of the reactive streams spec, this number will be used to check if a
        /// {@code Subscription} actually solves the "unbounded recursion" problem by not allowing the number of
        /// recursive calls to exceed the number returned by this method.
        /// 
        /// <para/>
        /// see: https://github.com/reactive-streams/reactive-streams-jvm#3.3 reactive streams spec, rule 3.3
        /// see: <see cref="PublisherVerification{T}.Required_spec303_mustNotAllowUnboundedRecursion"/>
        /// </summary>
        public virtual long BoundedDepthOfOnNextAndRequestRecursion { get; } = 1;

        /// <summary>
        /// Override and return true in order to skip executing tests marked as Stochastic.
        /// Such tests MAY sometimes fail even though the impl
        /// </summary>
        public virtual bool SkipStochasticTests { get; } = false;

        /// <summary>
        /// Describes the tested implementation in terms of how many subscribers they can support.
        /// Some tests require the <see cref="IPublisher{T}"/> under test to support multiple Subscribers,
        /// yet the spec does not require all publishers to be able to do so, thus – if an implementation
        /// supports only a limited number of subscribers (e.g. only 1 subscriber, also known as "no fanout")
        /// you MUST return that number from this method by overriding it.
        /// </summary>
        public virtual long MaxSupportedSubscribers { get; } = long.MaxValue;

        ////////////////////// TEST ENV CLEANUP /////////////////////////////////////

        [SetUp]
        public void SetUp()
        {
            _publisherVerification.SetUp();
            _subscriberVerification.SetUp();
        }

        ////////////////////// PUBLISHER RULES VERIFICATION ///////////////////////////

        public IPublisher<T> CreatePublisher(long elements)
        {
            var processor = CreateIdentityProcessor(_processorBufferSize);
            var publisher = CreateHelperPublisher(elements);
            publisher.Subscribe(processor);
            return processor; // we run the PublisherVerification against this
        }

        [Test]
        public void Required_validate_maxElementsFromPublisher()
            => _publisherVerification.Required_validate_maxElementsFromPublisher();

        [Test]
        public void Required_validate_boundedDepthOfOnNextAndRequestRecursion()
            => _publisherVerification.Required_validate_boundedDepthOfOnNextAndRequestRecursion();

        /////////////////////// DELEGATED TESTS, A PROCESSOR "IS A" PUBLISHER //////////////////////
        // Verifies rule: https://github.com/reactive-streams/reactive-streams-jvm#4.1


        [Test]
        public void Required_createPublisher1MustProduceAStreamOfExactly1Element()
            => _publisherVerification.Required_createPublisher1MustProduceAStreamOfExactly1Element();

        [Test]
        public void Required_createPublisher3MustProduceAStreamOfExactly3Elements()
            => _publisherVerification.Required_createPublisher3MustProduceAStreamOfExactly3Elements();

        [Test]
        public void Required_spec101_subscriptionRequestMustResultInTheCorrectNumberOfProducedElements()
            => _publisherVerification.Required_spec101_subscriptionRequestMustResultInTheCorrectNumberOfProducedElements();

        [Test]
        public void Required_spec102_maySignalLessThanRequestedAndTerminateSubscription()
            => _publisherVerification.Required_spec102_maySignalLessThanRequestedAndTerminateSubscription();

        [Test]
        public void Stochastic_spec103_mustSignalOnMethodsSequentially()
            => _publisherVerification.Stochastic_spec103_mustSignalOnMethodsSequentially();

        [Test]
        public void Optional_spec104_mustSignalOnErrorWhenFails()
            => _publisherVerification.Optional_spec104_mustSignalOnErrorWhenFails();

        [Test]
        public void Required_spec105_mustSignalOnCompleteWhenFiniteStreamTerminates()
            => _publisherVerification.Required_spec105_mustSignalOnCompleteWhenFiniteStreamTerminates();

        [Test]
        public void Optional_spec105_emptyStreamMustTerminateBySignallingOnComplete()
            => _publisherVerification.Optional_spec105_emptyStreamMustTerminateBySignallingOnComplete();

        [Test]
        public void Untested_spec106_mustConsiderSubscriptionCancelledAfterOnErrorOrOnCompleteHasBeenCalled()
            => _publisherVerification.Untested_spec106_mustConsiderSubscriptionCancelledAfterOnErrorOrOnCompleteHasBeenCalled();

        [Test]
        public void Required_spec107_mustNotEmitFurtherSignalsOnceOnCompleteHasBeenSignalled()
            => _publisherVerification.Required_spec107_mustNotEmitFurtherSignalsOnceOnCompleteHasBeenSignalled();

        [Test]
        public void Untested_spec107_mustNotEmitFurtherSignalsOnceOnErrorHasBeenSignalled()
            => _publisherVerification.Untested_spec107_mustNotEmitFurtherSignalsOnceOnErrorHasBeenSignalled();

        [Test]
        public void Untested_spec108_possiblyCanceledSubscriptionShouldNotReceiveOnErrorOrOnCompleteSignals()
            => _publisherVerification.Untested_spec108_possiblyCanceledSubscriptionShouldNotReceiveOnErrorOrOnCompleteSignals();

        [Test]
        public void Required_spec109_mustIssueOnSubscribeForNonNullSubscriber()
            => _publisherVerification.Required_spec109_mustIssueOnSubscribeForNonNullSubscriber();

        [Test]
        public void Untested_spec109_subscribeShouldNotThrowNonFatalThrowable()
            => _publisherVerification.Untested_spec109_subscribeShouldNotThrowNonFatalThrowable();

        [Test]
        public void Required_spec109_subscribeThrowNPEOnNullSubscriber()
            => _publisherVerification.Required_spec109_subscribeThrowNPEOnNullSubscriber();

        [Test]
        public void Required_spec109_mayRejectCallsToSubscribeIfPublisherIsUnableOrUnwillingToServeThemRejectionMustTriggerOnErrorAfterOnSubscribe()
            => _publisherVerification.Required_spec109_mayRejectCallsToSubscribeIfPublisherIsUnableOrUnwillingToServeThemRejectionMustTriggerOnErrorAfterOnSubscribe();

        [Test]
        public void Untested_spec110_rejectASubscriptionRequestIfTheSameSubscriberSubscribesTwice()
            => _publisherVerification.Untested_spec110_rejectASubscriptionRequestIfTheSameSubscriberSubscribesTwice();

        [Test]
        public void Optional_spec111_maySupportMultiSubscribe()
            => _publisherVerification.Optional_spec111_maySupportMultiSubscribe();

        [Test]
        public void Optional_spec111_multicast_mustProduceTheSameElementsInTheSameSequenceToAllOfItsSubscribersWhenRequestingOneByOne()
            => _publisherVerification.Optional_spec111_multicast_mustProduceTheSameElementsInTheSameSequenceToAllOfItsSubscribersWhenRequestingOneByOne();

        [Test]
        public void Optional_spec111_multicast_mustProduceTheSameElementsInTheSameSequenceToAllOfItsSubscribersWhenRequestingManyUpfront()
            => _publisherVerification.Optional_spec111_multicast_mustProduceTheSameElementsInTheSameSequenceToAllOfItsSubscribersWhenRequestingManyUpfront();

        [Test]
        public void Optional_spec111_multicast_mustProduceTheSameElementsInTheSameSequenceToAllOfItsSubscribersWhenRequestingManyUpfrontAndCompleteAsExpected()
            => _publisherVerification.Optional_spec111_multicast_mustProduceTheSameElementsInTheSameSequenceToAllOfItsSubscribersWhenRequestingManyUpfrontAndCompleteAsExpected();

        [Test]
        public void Required_spec302_mustAllowSynchronousRequestCallsFromOnNextAndOnSubscribe()
            => _publisherVerification.Required_spec302_mustAllowSynchronousRequestCallsFromOnNextAndOnSubscribe();

        [Test]
        public void Required_spec303_mustNotAllowUnboundedRecursion()
            => _publisherVerification.Required_spec303_mustNotAllowUnboundedRecursion();

        [Test]
        public void Untested_spec304_requestShouldNotPerformHeavyComputations()
            => _publisherVerification.Untested_spec304_requestShouldNotPerformHeavyComputations();

        [Test]
        public void Untested_spec305_cancelMustNotSynchronouslyPerformHeavyCompuatation()
            => _publisherVerification.Untested_spec305_cancelMustNotSynchronouslyPerformHeavyCompuatation();

        [Test]
        public void Required_spec306_afterSubscriptionIsCancelledRequestMustBeNops()
            => _publisherVerification.Required_spec306_afterSubscriptionIsCancelledRequestMustBeNops();

        [Test]
        public void Required_spec307_afterSubscriptionIsCancelledAdditionalCancelationsMustBeNops()
            => _publisherVerification.Required_spec307_afterSubscriptionIsCancelledAdditionalCancelationsMustBeNops();

        [Test]
        public void Required_spec309_requestZeroMustSignalIllegalArgumentException()
            => _publisherVerification.Required_spec309_requestZeroMustSignalIllegalArgumentException();

        [Test]
        public void Required_spec309_requestNegativeNumberMustSignalIllegalArgumentException()
            => _publisherVerification.Required_spec309_requestNegativeNumberMustSignalIllegalArgumentException();

        [Test]
        public void Required_spec312_cancelMustMakeThePublisherToEventuallyStopSignaling()
            => _publisherVerification.Required_spec312_cancelMustMakeThePublisherToEventuallyStopSignaling();

        [Test]
        public void Required_spec313_cancelMustMakeThePublisherEventuallyDropAllReferencesToTheSubscriber()
            => _publisherVerification.Required_spec313_cancelMustMakeThePublisherEventuallyDropAllReferencesToTheSubscriber();

        [Test]
        public void Required_spec317_mustSupportAPendingElementCountUpToLongMaxValue()
            => _publisherVerification.Required_spec317_mustSupportAPendingElementCountUpToLongMaxValue();

        [Test]
        public void Required_spec317_mustSupportACumulativePendingElementCountUpToLongMaxValue()
            => _publisherVerification.Required_spec317_mustSupportACumulativePendingElementCountUpToLongMaxValue();

        [Test]
        public void Required_spec317_mustNotSignalOnErrorWhenPendingAboveLongMaxValue()
            => _publisherVerification.Required_spec317_mustNotSignalOnErrorWhenPendingAboveLongMaxValue();


        // Verifies rule: https://github.com/reactive-streams/reactive-streams-jvm#1.4
        // for multiple subscribers
        [Test]
        public void Required_spec104_mustCallOnErrorOnAllItsSubscribersIfItEncountersANonRecoverableError()
            => OptionalMultipleSubscribersTest(2, setup =>
            {
                var sub1 = new ManualSubscriberWithErrorCollection<T>(_environment);
                _environment.Subscribe(setup.Processor, sub1);

                var sub2 = new ManualSubscriberWithErrorCollection<T>(_environment);
                _environment.Subscribe(setup.Processor, sub2);

                sub1.Request(1);
                setup.ExpectRequest();
                var x = setup.SendNextTFromUpstream();
                setup.ExpectNextElement(sub1, x);
                sub1.Request(1);

                // sub1 has received one element, and has one demand pending
                // sub2 has not yet requested anything

                var ex = new TestException();
                setup.SendError(ex);
                sub1.ExpectError(ex);
                sub2.ExpectError(ex);

                _environment.VerifyNoAsyncErrorsNoDelay();
            });

        ////////////////////// SUBSCRIBER RULES VERIFICATION ///////////////////////////
        // Verifies rule: https://github.com/reactive-streams/reactive-streams-jvm#4.1

        // A Processor
        //   must obey all Subscriber rules on its consuming side
        public ISubscriber<T> CreateSubscriber(WhiteboxSubscriberProbe<T> probe)
        {
            var processor = CreateIdentityProcessor(_processorBufferSize);
            processor.Subscribe(new ProcessorSubscriber(_environment, probe));
            return processor; // we run the SubscriberVerification against this
        }

        private sealed class ProcessorSubscriber : ISubscriber<T>
        {
            private sealed class Subscription : ISubscriberPuppet
            {
                private readonly ISubscription _subscription;

                public Subscription(ISubscription subscription)
                {
                    _subscription = subscription;
                }

                public void TriggerRequest(long elements) => _subscription.Request(elements);

                public void SignalCancel() => _subscription.Cancel();
            }

            private readonly TestEnvironment _environment;
            private readonly WhiteboxSubscriberProbe<T> _probe;
            private readonly Promise<ISubscription> _subscription;
            public ProcessorSubscriber(TestEnvironment environment, WhiteboxSubscriberProbe<T> probe)
            {
                _environment = environment;
                _probe = probe;
                _subscription = new Promise<ISubscription>(environment);
            }

            public void OnNext(T element)
            {
                _environment.Debug($"WhiteboxSubscriber.OnNext({element})");
                _probe.RegisterOnNext(element);
            }

            public void OnSubscribe(ISubscription subscription)
            {
                _environment.Debug($"WhiteboxSubscriber.OnSubscribe({subscription})");
                if(_subscription.IsCompleted())
                    subscription.Cancel(); // the Probe must also pass subscriber verification

                _probe.RegisterOnSubscribe(new Subscription(subscription));
            }

            public void OnError(Exception cause)
            {
                _environment.Debug($"WhiteboxSubscriber.OnError({cause})");
                _probe.RegisterOnError(cause);
            }

            public void OnComplete()
            {
                _environment.Debug("WhiteboxSubscriber.OnComplete()");
                _probe.RegisterOnComplete();
            }
        }

        ////////////////////// OTHER RULE VERIFICATION ///////////////////////////

        // A Processor
        //   must immediately pass on `onError` events received from its upstream to its downstream
        [Test]
        public void MustImmediatelyPassOnOnErrorEventsReceivedFromItsUpstreamToItsDownstream()
        {
            var setup = new TestSetup(_environment, _processorBufferSize, 1, this);
            var subscription = new ManualSubscriberWithErrorCollection<T>(_environment);
            _environment.Subscribe(setup.Processor, subscription);

            var ex = new TestException();
            setup.SendError(ex);
            subscription.ExpectError(ex); // "immediately", i.e. without a preceding request

            _environment.VerifyNoAsyncErrorsNoDelay();
        }

        /////////////////////// DELEGATED TESTS, A PROCESSOR "IS A" SUBSCRIBER //////////////////////
        // Verifies rule: https://github.com/reactive-streams/reactive-streams-jvm#4.1

        [Test]
        public void Required_exerciseWhiteboxHappyPath()
           => _subscriberVerification.Required_exerciseWhiteboxHappyPath();
        [Test]
        public void Required_spec201_mustSignalDemandViaSubscriptionRequest()
            => _subscriberVerification.Required_spec201_mustSignalDemandViaSubscriptionRequest();
        [Test]
        public void Untested_spec202_shouldAsynchronouslyDispatch()
            => _subscriberVerification.Untested_spec202_shouldAsynchronouslyDispatch();
        [Test]
        public void Required_spec203_mustNotCallMethodsOnSubscriptionOrPublisherInOnComplete()
            => _subscriberVerification.Required_spec203_mustNotCallMethodsOnSubscriptionOrPublisherInOnComplete();
        [Test]
        public void Required_spec203_mustNotCallMethodsOnSubscriptionOrPublisherInOnError()
            => _subscriberVerification.Required_spec203_mustNotCallMethodsOnSubscriptionOrPublisherInOnError();
        [Test]
        public void Untested_spec204_mustConsiderTheSubscriptionAsCancelledInAfterRecievingOnCompleteOrOnError()
            => _subscriberVerification.Untested_spec204_mustConsiderTheSubscriptionAsCancelledInAfterRecievingOnCompleteOrOnError();
        [Test]
        public void Required_spec205_mustCallSubscriptionCancelIfItAlreadyHasAnSubscriptionAndReceivesAnotherOnSubscribeSignal()
            => _subscriberVerification.Required_spec205_mustCallSubscriptionCancelIfItAlreadyHasAnSubscriptionAndReceivesAnotherOnSubscribeSignal();
        [Test]
        public void Untested_spec206_mustCallSubscriptionCancelIfItIsNoLongerValid()
            => _subscriberVerification.Untested_spec206_mustCallSubscriptionCancelIfItIsNoLongerValid();
        [Test]
        public void Untested_spec207_mustEnsureAllCallsOnItsSubscriptionTakePlaceFromTheSameThreadOrTakeCareOfSynchronization()
            => _subscriberVerification.Untested_spec207_mustEnsureAllCallsOnItsSubscriptionTakePlaceFromTheSameThreadOrTakeCareOfSynchronization();
        [Test]
        public void Required_spec208_mustBePreparedToReceiveOnNextSignalsAfterHavingCalledSubscriptionCancel()
            => _subscriberVerification.Required_spec208_mustBePreparedToReceiveOnNextSignalsAfterHavingCalledSubscriptionCancel();
        [Test]
        public void Required_spec209_mustBePreparedToReceiveAnOnCompleteSignalWithPrecedingRequestCall()
            => _subscriberVerification.Required_spec209_mustBePreparedToReceiveAnOnCompleteSignalWithPrecedingRequestCall();
        [Test]
        public void Required_spec209_mustBePreparedToReceiveAnOnCompleteSignalWithoutPrecedingRequestCall()
            => _subscriberVerification.Required_spec209_mustBePreparedToReceiveAnOnCompleteSignalWithoutPrecedingRequestCall();
        [Test]
        public void Required_spec210_mustBePreparedToReceiveAnOnErrorSignalWithPrecedingRequestCall()
            => _subscriberVerification.Required_spec210_mustBePreparedToReceiveAnOnErrorSignalWithPrecedingRequestCall();
        [Test]
        public void Required_spec210_mustBePreparedToReceiveAnOnErrorSignalWithoutPrecedingRequestCall()
            => _subscriberVerification.Required_spec210_mustBePreparedToReceiveAnOnErrorSignalWithoutPrecedingRequestCall();
        [Test]
        public void Untested_spec211_mustMakeSureThatAllCallsOnItsMethodsHappenBeforeTheProcessingOfTheRespectiveEvents()
            => _subscriberVerification.Untested_spec211_mustMakeSureThatAllCallsOnItsMethodsHappenBeforeTheProcessingOfTheRespectiveEvents();
        [Test]
        public void Untested_spec212_mustNotCallOnSubscribeMoreThanOnceBasedOnObjectEquality_specViolation()
            => _subscriberVerification.Untested_spec212_mustNotCallOnSubscribeMoreThanOnceBasedOnObjectEquality_specViolation();
        [Test]
        public void Untested_spec213_failingOnSignalInvocation()
            => _subscriberVerification.Untested_spec213_failingOnSignalInvocation();
        [Test]
        public void Required_spec213_onSubscribe_mustThrowNullPointerExceptionWhenParametersAreNull()
            => _subscriberVerification.Required_spec213_onSubscribe_mustThrowNullPointerExceptionWhenParametersAreNull();
        [Test]
        public void Required_spec213_onNext_mustThrowNullPointerExceptionWhenParametersAreNull()
            => _subscriberVerification.Required_spec213_onNext_mustThrowNullPointerExceptionWhenParametersAreNull();
        [Test]
        public void Required_spec213_onError_mustThrowNullPointerExceptionWhenParametersAreNull()
            => _subscriberVerification.Required_spec213_onError_mustThrowNullPointerExceptionWhenParametersAreNull();
        [Test]
        public void Untested_spec301_mustNotBeCalledOutsideSubscriberContext()
            => _subscriberVerification.Untested_spec301_mustNotBeCalledOutsideSubscriberContext();
        [Test]
        public void Required_spec308_requestMustRegisterGivenNumberElementsToBeProduced()
            => _subscriberVerification.Required_spec308_requestMustRegisterGivenNumberElementsToBeProduced();
        [Test]
        public void Untested_spec310_requestMaySynchronouslyCallOnNextOnSubscriber()
            => _subscriberVerification.Untested_spec310_requestMaySynchronouslyCallOnNextOnSubscriber();
        [Test]
        public void Untested_spec311_requestMaySynchronouslyCallOnCompleteOrOnError()
            => _subscriberVerification.Untested_spec311_requestMaySynchronouslyCallOnCompleteOrOnError();
        [Test]
        public void Untested_spec314_cancelMayCauseThePublisherToShutdownIfNoOtherSubscriptionExists()
            => _subscriberVerification.Untested_spec314_cancelMayCauseThePublisherToShutdownIfNoOtherSubscriptionExists();
        [Test]
        public void Untested_spec315_cancelMustNotThrowExceptionAndMustSignalOnError()
            => _subscriberVerification.Untested_spec315_cancelMustNotThrowExceptionAndMustSignalOnError();
        [Test]
        public void Untested_spec316_requestMustNotThrowExceptionAndMustOnErrorTheSubscriber()
            => _subscriberVerification.Untested_spec316_requestMustNotThrowExceptionAndMustOnErrorTheSubscriber();

        /////////////////////// ADDITIONAL "COROLLARY" TESTS //////////////////////

        // A Processor
        //   must trigger `requestFromUpstream` for elements that have been requested 'long ago'
        [Test]
        public void Required_mustRequestFromUpstreamForElementsThatHaveBeenRequestedLongAgo()
        {
            OptionalMultipleSubscribersTest(2, setup =>
            {
                var sub1 = setup.NewSubscriber();
                sub1.Request(20);

                var totalRequests = setup.ExpectRequest();
                var x = setup.SendNextTFromUpstream();
                setup.ExpectNextElement(sub1, x);

                if (totalRequests == 1)
                    totalRequests += setup.ExpectRequest();

                var y = setup.SendNextTFromUpstream();
                setup.ExpectNextElement(sub1, y);

                if (totalRequests == 1)
                    totalRequests += setup.ExpectRequest();

                var sub2 = setup.NewSubscriber();

                // sub1 now has 18 pending
                // sub2 has 0 pending

                var z = setup.SendNextTFromUpstream();
                setup.ExpectNextElement(sub1, z);
                sub2.ExpectNone();// since sub2 hasn't requested anything yet

                sub2.Request(1);
                setup.ExpectNextElement(sub2, z);

                if (totalRequests == 3)
                    setup.ExpectRequest();

                // to avoid error messages during test harness shutdown
                setup.SendCompletion();
                sub1.ExpectCompletion(_environment.DefaultTimeoutMilliseconds);
                sub2.ExpectCompletion(_environment.DefaultTimeoutMilliseconds);

                _environment.VerifyNoAsyncErrorsNoDelay();
            });
        }

        /////////////////////// TEST INFRASTRUCTURE //////////////////////

        public void NotVerified() => _publisherVerification.NotVerified();

        public void NotVerified(string message) => _publisherVerification.NotVerified(message);

        /// <summary>
        /// Test for feature that REQUIRES multiple subscribers to be supported by Publisher.
        /// </summary>
        public void OptionalMultipleSubscribersTest(long requiredSubscribersSupport, Action<TestSetup> body)
        {
            if (requiredSubscribersSupport > MaxSupportedSubscribers)
                NotVerified(
                    $"The Publisher under test only supports {MaxSupportedSubscribers} subscribers, while this test requires at least {requiredSubscribersSupport} to run.");
            else
                body(new TestSetup(_environment, _processorBufferSize, requiredSubscribersSupport, this));
        }

        public class TestSetup : ManualPublisher<T>
        {
            private readonly HashSet<T> _seenTees = new HashSet<T>();

            public TestSetup(TestEnvironment environment, int testBufferSize, long requiredSubscribersSupport,
                IdentityProcessorVerification<T> verification) : base(environment)
            {
                TestBufferSize = testBufferSize;
                RequiredSubscribersSupport = requiredSubscribersSupport;

                Tees = Environment.NewManualSubscriber(verification.CreateHelperPublisher(long.MaxValue));
                Processor = verification.CreateIdentityProcessor(testBufferSize);
                Subscribe(Processor);
            }

            public IProcessor<T, T> Processor { get; }

            public ManualSubscriber<T> Tees { get; }

            public int TestBufferSize { get; }

            public long RequiredSubscribersSupport { get; }

            public ManualSubscriber<T> NewSubscriber() => Environment.NewManualSubscriber(Processor);

            public T NextT()
            {
                var t = Tees.RequestNextElement();
                if(_seenTees.Contains(t))
                    Environment.Flop($"Helper publisher illegally produced the same element {t} twice");
                _seenTees.Add(t);
                return t;
            }

            public void ExpectNextElement(ManualSubscriber<T> subscriber, T expected)
            {
                var element = subscriber.NextElement($"timeout while awaiting {expected}");
                if(!element.Equals(expected))
                    Environment.Flop($"Received `OnNext({element})` on downstream but expected `OnNext({expected})");
            }

            public T SendNextTFromUpstream()
            {
                var x = NextT();
                SendNext(x);
                return x;
            }
        }

        public class ManualSubscriberWithErrorCollection<A> : ManualSubscriberWithSubscriptionSupport<A>
        {
            private readonly Promise<Exception> _error;

            public ManualSubscriberWithErrorCollection(TestEnvironment environment) : base(environment)
            {
                _error = new Promise<Exception>(environment);
            }

            public override void OnError(Exception cause) => _error.Complete(cause);

            public void ExpectError(Exception cause) => ExpectError(cause, Environment.DefaultTimeoutMilliseconds);

            public void ExpectError(Exception cause, long timeoutMilliseconds)
            {
                _error.ExpectCompletion(timeoutMilliseconds, "Did not receive expected error on downstream");
                if (cause.Equals(_error.Value))
                    Environment.Flop($"Expected error {cause} but got {_error}");
            }
        }
    }
}
