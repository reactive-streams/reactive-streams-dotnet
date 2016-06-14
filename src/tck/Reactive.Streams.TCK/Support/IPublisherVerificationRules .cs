namespace Reactive.Streams.TCK.Support
{
    /// <summary>
    /// Internal TCK use only.
    /// Add / Remove tests for PublisherVerification here to make sure that they arre added/removed in the other places. 
    /// </summary>
    internal interface IPublisherVerificationRules
    {
        void Required_validate_maxElementsFromPublisher();
        void Required_validate_boundedDepthOfOnNextAndRequestRecursion();
        void Required_createPublisher1MustProduceAStreamOfExactly1Element();
        void Required_createPublisher3MustProduceAStreamOfExactly3Elements();
        void Required_spec101_subscriptionRequestMustResultInTheCorrectNumberOfProducedElements();
        void Required_spec102_maySignalLessThanRequestedAndTerminateSubscription();
        void Stochastic_spec103_mustSignalOnMethodsSequentially();
        void Optional_spec104_mustSignalOnErrorWhenFails();
        void Required_spec105_mustSignalOnCompleteWhenFiniteStreamTerminates();
        void Optional_spec105_emptyStreamMustTerminateBySignallingOnComplete();
        void Untested_spec106_mustConsiderSubscriptionCancelledAfterOnErrorOrOnCompleteHasBeenCalled();
        void Required_spec107_mustNotEmitFurtherSignalsOnceOnCompleteHasBeenSignalled();
        void Untested_spec107_mustNotEmitFurtherSignalsOnceOnErrorHasBeenSignalled();
        void Untested_spec108_possiblyCanceledSubscriptionShouldNotReceiveOnErrorOrOnCompleteSignals();
        void Required_spec109_mustIssueOnSubscribeForNonNullSubscriber();
        void Untested_spec109_subscribeShouldNotThrowNonFatalThrowable();
        void Required_spec109_subscribeThrowNPEOnNullSubscriber();
        void Required_spec109_mayRejectCallsToSubscribeIfPublisherIsUnableOrUnwillingToServeThemRejectionMustTriggerOnErrorAfterOnSubscribe();
        void Untested_spec110_rejectASubscriptionRequestIfTheSameSubscriberSubscribesTwice();
        void Optional_spec111_maySupportMultiSubscribe();
        void Optional_spec111_multicast_mustProduceTheSameElementsInTheSameSequenceToAllOfItsSubscribersWhenRequestingOneByOne();
        void Optional_spec111_multicast_mustProduceTheSameElementsInTheSameSequenceToAllOfItsSubscribersWhenRequestingManyUpfront();
        void Optional_spec111_multicast_mustProduceTheSameElementsInTheSameSequenceToAllOfItsSubscribersWhenRequestingManyUpfrontAndCompleteAsExpected();
        void Required_spec302_mustAllowSynchronousRequestCallsFromOnNextAndOnSubscribe();
        void Required_spec303_mustNotAllowUnboundedRecursion();
        void Untested_spec304_requestShouldNotPerformHeavyComputations();
        void Untested_spec305_cancelMustNotSynchronouslyPerformHeavyCompuatation();
        void Required_spec306_afterSubscriptionIsCancelledRequestMustBeNops();
        void Required_spec307_afterSubscriptionIsCancelledAdditionalCancelationsMustBeNops();
        void Required_spec309_requestZeroMustSignalIllegalArgumentException();
        void Required_spec309_requestNegativeNumberMustSignalIllegalArgumentException();
        void Required_spec312_cancelMustMakeThePublisherToEventuallyStopSignaling();
        void Required_spec313_cancelMustMakeThePublisherEventuallyDropAllReferencesToTheSubscriber();
        void Required_spec317_mustSupportAPendingElementCountUpToLongMaxValue();
        void Required_spec317_mustSupportACumulativePendingElementCountUpToLongMaxValue();
        void Required_spec317_mustNotSignalOnErrorWhenPendingAboveLongMaxValue();
    }
}
