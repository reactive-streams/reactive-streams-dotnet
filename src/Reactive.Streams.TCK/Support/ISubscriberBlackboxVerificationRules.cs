namespace Reactive.Streams.TCK.Support
{
    /// <summary>
    /// Internal TCK use only.
    /// Add / Remove tests for SubscriberBlackboxVerificationRules here to make sure that they arre added/removed in the other places. 
    /// </summary>
    internal interface ISubscriberBlackboxVerificationRules
    {
        void required_spec201_blackbox_mustSignalDemandViaSubscriptionRequest();
        void untested_spec202_blackbox_shouldAsynchronouslyDispatch();
        void required_spec203_blackbox_mustNotCallMethodsOnSubscriptionOrPublisherInOnComplete();
        void required_spec203_blackbox_mustNotCallMethodsOnSubscriptionOrPublisherInOnError();
        void untested_spec204_blackbox_mustConsiderTheSubscriptionAsCancelledInAfterRecievingOnCompleteOrOnError();
        void required_spec205_blackbox_mustCallSubscriptionCancelIfItAlreadyHasAnSubscriptionAndReceivesAnotherOnSubscribeSignal();
        void untested_spec206_blackbox_mustCallSubscriptionCancelIfItIsNoLongerValid();
        void untested_spec207_blackbox_mustEnsureAllCallsOnItsSubscriptionTakePlaceFromTheSameThreadOrTakeCareOfSynchronization();
        void untested_spec208_blackbox_mustBePreparedToReceiveOnNextSignalsAfterHavingCalledSubscriptionCancel();
        void required_spec209_blackbox_mustBePreparedToReceiveAnOnCompleteSignalWithPrecedingRequestCall();
        void required_spec209_blackbox_mustBePreparedToReceiveAnOnCompleteSignalWithoutPrecedingRequestCall();
        void required_spec210_blackbox_mustBePreparedToReceiveAnOnErrorSignalWithPrecedingRequestCall();
        void untested_spec211_blackbox_mustMakeSureThatAllCallsOnItsMethodsHappenBeforeTheProcessingOfTheRespectiveEvents();
        void untested_spec212_blackbox_mustNotCallOnSubscribeMoreThanOnceBasedOnObjectEquality();
        void untested_spec213_blackbox_failingOnSignalInvocation();
        void required_spec213_blackbox_onSubscribe_mustThrowNullPointerExceptionWhenParametersAreNull();
        void required_spec213_blackbox_onNext_mustThrowNullPointerExceptionWhenParametersAreNull();
        void required_spec213_blackbox_onError_mustThrowNullPointerExceptionWhenParametersAreNull();
        void untested_spec301_blackbox_mustNotBeCalledOutsideSubscriberContext();
        void untested_spec308_blackbox_requestMustRegisterGivenNumberElementsToBeProduced();
        void untested_spec310_blackbox_requestMaySynchronouslyCallOnNextOnSubscriber();
        void untested_spec311_blackbox_requestMaySynchronouslyCallOnCompleteOrOnError();
        void untested_spec314_blackbox_cancelMayCauseThePublisherToShutdownIfNoOtherSubscriptionExists();
        void untested_spec315_blackbox_cancelMustNotThrowExceptionAndMustSignalOnError();
        void untested_spec316_blackbox_requestMustNotThrowExceptionAndMustOnErrorTheSubscriber();
    }
}
