namespace Reactive.Streams.TCK.Support
{
    /// <summary>
    /// Internal TCK use only.
    /// Add / Remove tests for PublisherVerification here to make sure that they arre added/removed in the other places. 
    /// </summary>
    internal interface IPublisherVerificationRules
    {
        void required_validate_maxElementsFromPublisher();
        void required_validate_boundedDepthOfOnNextAndRequestRecursion();
        void required_createPublisher1MustProduceAStreamOfExactly1Element();
        void required_createPublisher3MustProduceAStreamOfExactly3Elements();
        void required_spec101_subscriptionRequestMustResultInTheCorrectNumberOfProducedElements();
        void required_spec102_maySignalLessThanRequestedAndTerminateSubscription();
        void stochastic_spec103_mustSignalOnMethodsSequentially();
        void optional_spec104_mustSignalOnErrorWhenFails();
        void required_spec105_mustSignalOnCompleteWhenFiniteStreamTerminates();
        void optional_spec105_emptyStreamMustTerminateBySignallingOnComplete();
        void untested_spec106_mustConsiderSubscriptionCancelledAfterOnErrorOrOnCompleteHasBeenCalled();
        void required_spec107_mustNotEmitFurtherSignalsOnceOnCompleteHasBeenSignalled();
        void untested_spec107_mustNotEmitFurtherSignalsOnceOnErrorHasBeenSignalled();
        void untested_spec108_possiblyCanceledSubscriptionShouldNotReceiveOnErrorOrOnCompleteSignals();
        void required_spec109_mustIssueOnSubscribeForNonNullSubscriber();
        void untested_spec109_subscribeShouldNotThrowNonFatalThrowable();
        void required_spec109_subscribeThrowNPEOnNullSubscriber();
        void required_spec109_mayRejectCallsToSubscribeIfPublisherIsUnableOrUnwillingToServeThemRejectionMustTriggerOnErrorAfterOnSubscribe();
        void untested_spec110_rejectASubscriptionRequestIfTheSameSubscriberSubscribesTwice();
        void optional_spec111_maySupportMultiSubscribe();
        void optional_spec111_multicast_mustProduceTheSameElementsInTheSameSequenceToAllOfItsSubscribersWhenRequestingOneByOne();
        void optional_spec111_multicast_mustProduceTheSameElementsInTheSameSequenceToAllOfItsSubscribersWhenRequestingManyUpfront();
        void optional_spec111_multicast_mustProduceTheSameElementsInTheSameSequenceToAllOfItsSubscribersWhenRequestingManyUpfrontAndCompleteAsExpected();
        void required_spec302_mustAllowSynchronousRequestCallsFromOnNextAndOnSubscribe();
        void required_spec303_mustNotAllowUnboundedRecursion();
        void untested_spec304_requestShouldNotPerformHeavyComputations();
        void untested_spec305_cancelMustNotSynchronouslyPerformHeavyCompuatation();
        void required_spec306_afterSubscriptionIsCancelledRequestMustBeNops();
        void required_spec307_afterSubscriptionIsCancelledAdditionalCancelationsMustBeNops();
        void required_spec309_requestZeroMustSignalIllegalArgumentException();
        void required_spec309_requestNegativeNumberMustSignalIllegalArgumentException();
        void required_spec312_cancelMustMakeThePublisherToEventuallyStopSignaling();
        void required_spec313_cancelMustMakeThePublisherEventuallyDropAllReferencesToTheSubscriber();
        void required_spec317_mustSupportAPendingElementCountUpToLongMaxValue();
        void required_spec317_mustSupportACumulativePendingElementCountUpToLongMaxValue();
        void required_spec317_mustNotSignalOnErrorWhenPendingAboveLongMaxValue();
    }
}
