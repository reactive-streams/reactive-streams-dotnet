namespace Reactive.Streams.TCK.Support
{
    /// <summary>
    /// Internal TCK use only.
    /// Add / Remove tests for SubscriberWhiteboxVerificationRules here to make sure that they arre added/removed in the other places. 
    /// </summary>
    internal interface ISubscriberWhiteboxVerificationRules
    {
        void Required_exerciseWhiteboxHappyPath();
        void Required_spec201_mustSignalDemandViaSubscriptionRequest();
        void Untested_spec202_shouldAsynchronouslyDispatch();
        void Required_spec203_mustNotCallMethodsOnSubscriptionOrPublisherInOnComplete();
        void Required_spec203_mustNotCallMethodsOnSubscriptionOrPublisherInOnError();
        void Untested_spec204_mustConsiderTheSubscriptionAsCancelledInAfterRecievingOnCompleteOrOnError();
        void Required_spec205_mustCallSubscriptionCancelIfItAlreadyHasAnSubscriptionAndReceivesAnotherOnSubscribeSignal();
        void Untested_spec206_mustCallSubscriptionCancelIfItIsNoLongerValid();
        void Untested_spec207_mustEnsureAllCallsOnItsSubscriptionTakePlaceFromTheSameThreadOrTakeCareOfSynchronization();
        void Required_spec208_mustBePreparedToReceiveOnNextSignalsAfterHavingCalledSubscriptionCancel();
        void Required_spec209_mustBePreparedToReceiveAnOnCompleteSignalWithPrecedingRequestCall();
        void Required_spec209_mustBePreparedToReceiveAnOnCompleteSignalWithoutPrecedingRequestCall();
        void Required_spec210_mustBePreparedToReceiveAnOnErrorSignalWithPrecedingRequestCall();
        void Required_spec210_mustBePreparedToReceiveAnOnErrorSignalWithoutPrecedingRequestCall();
        void Untested_spec211_mustMakeSureThatAllCallsOnItsMethodsHappenBeforeTheProcessingOfTheRespectiveEvents();
        void Untested_spec212_mustNotCallOnSubscribeMoreThanOnceBasedOnObjectEquality_specViolation();
        void Untested_spec213_failingOnSignalInvocation();
        void Required_spec213_onSubscribe_mustThrowNullPointerExceptionWhenParametersAreNull();
        void Required_spec213_onNext_mustThrowNullPointerExceptionWhenParametersAreNull();
        void Required_spec213_onError_mustThrowNullPointerExceptionWhenParametersAreNull();
        void Untested_spec301_mustNotBeCalledOutsideSubscriberContext();
        void Required_spec308_requestMustRegisterGivenNumberElementsToBeProduced();
        void Untested_spec310_requestMaySynchronouslyCallOnNextOnSubscriber();
        void Untested_spec311_requestMaySynchronouslyCallOnCompleteOrOnError();
        void Untested_spec314_cancelMayCauseThePublisherToShutdownIfNoOtherSubscriptionExists();
        void Untested_spec315_cancelMustNotThrowExceptionAndMustSignalOnError();
        void Untested_spec316_requestMustNotThrowExceptionAndMustOnErrorTheSubscriber();
    }
}
