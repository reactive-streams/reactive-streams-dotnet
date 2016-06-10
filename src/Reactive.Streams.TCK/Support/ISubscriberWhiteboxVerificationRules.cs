namespace Reactive.Streams.TCK.Support
{
    /// <summary>
    /// Internal TCK use only.
    /// Add / Remove tests for SubscriberWhiteboxVerificationRules here to make sure that they arre added/removed in the other places. 
    /// </summary>
    internal interface ISubscriberWhiteboxVerificationRules
    {
        void required_exerciseWhiteboxHappyPath();
        void required_spec201_mustSignalDemandViaSubscriptionRequest();
        void untested_spec202_shouldAsynchronouslyDispatch();
        void required_spec203_mustNotCallMethodsOnSubscriptionOrPublisherInOnComplete();
        void required_spec203_mustNotCallMethodsOnSubscriptionOrPublisherInOnError();
        void untested_spec204_mustConsiderTheSubscriptionAsCancelledInAfterRecievingOnCompleteOrOnError();
        void required_spec205_mustCallSubscriptionCancelIfItAlreadyHasAnSubscriptionAndReceivesAnotherOnSubscribeSignal();
        void untested_spec206_mustCallSubscriptionCancelIfItIsNoLongerValid();
        void untested_spec207_mustEnsureAllCallsOnItsSubscriptionTakePlaceFromTheSameThreadOrTakeCareOfSynchronization();
        void required_spec208_mustBePreparedToReceiveOnNextSignalsAfterHavingCalledSubscriptionCancel();
        void required_spec209_mustBePreparedToReceiveAnOnCompleteSignalWithPrecedingRequestCall();
        void required_spec209_mustBePreparedToReceiveAnOnCompleteSignalWithoutPrecedingRequestCall();
        void required_spec210_mustBePreparedToReceiveAnOnErrorSignalWithPrecedingRequestCall();
        void required_spec210_mustBePreparedToReceiveAnOnErrorSignalWithoutPrecedingRequestCall();
        void untested_spec211_mustMakeSureThatAllCallsOnItsMethodsHappenBeforeTheProcessingOfTheRespectiveEvents();
        void untested_spec212_mustNotCallOnSubscribeMoreThanOnceBasedOnObjectEquality_specViolation();
        void untested_spec213_failingOnSignalInvocation();
        void required_spec213_onSubscribe_mustThrowNullPointerExceptionWhenParametersAreNull();
        void required_spec213_onNext_mustThrowNullPointerExceptionWhenParametersAreNull();
        void required_spec213_onError_mustThrowNullPointerExceptionWhenParametersAreNull();
        void untested_spec301_mustNotBeCalledOutsideSubscriberContext();
        void required_spec308_requestMustRegisterGivenNumberElementsToBeProduced();
        void untested_spec310_requestMaySynchronouslyCallOnNextOnSubscriber();
        void untested_spec311_requestMaySynchronouslyCallOnCompleteOrOnError();
        void untested_spec314_cancelMayCauseThePublisherToShutdownIfNoOtherSubscriptionExists();
        void untested_spec315_cancelMustNotThrowExceptionAndMustSignalOnError();
        void untested_spec316_requestMustNotThrowExceptionAndMustOnErrorTheSubscriber();
    }
}
