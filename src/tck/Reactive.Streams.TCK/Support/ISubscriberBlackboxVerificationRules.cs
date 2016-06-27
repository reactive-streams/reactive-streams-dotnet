namespace Reactive.Streams.TCK.Support
{
    /// <summary>
    /// Internal TCK use only.
    /// Add / Remove tests for SubscriberBlackboxVerificationRules here to make sure that they arre added/removed in the other places. 
    /// </summary>
    internal interface ISubscriberBlackboxVerificationRules
    {
        void Required_spec201_blackbox_mustSignalDemandViaSubscriptionRequest();
        void Untested_spec202_blackbox_shouldAsynchronouslyDispatch();
        void Required_spec203_blackbox_mustNotCallMethodsOnSubscriptionOrPublisherInOnComplete();
        void Required_spec203_blackbox_mustNotCallMethodsOnSubscriptionOrPublisherInOnError();
        void Untested_spec204_blackbox_mustConsiderTheSubscriptionAsCancelledInAfterRecievingOnCompleteOrOnError();
        void Required_spec205_blackbox_mustCallSubscriptionCancelIfItAlreadyHasAnSubscriptionAndReceivesAnotherOnSubscribeSignal();
        void Untested_spec206_blackbox_mustCallSubscriptionCancelIfItIsNoLongerValid();
        void Untested_spec207_blackbox_mustEnsureAllCallsOnItsSubscriptionTakePlaceFromTheSameThreadOrTakeCareOfSynchronization();
        void Untested_spec208_blackbox_mustBePreparedToReceiveOnNextSignalsAfterHavingCalledSubscriptionCancel();
        void Required_spec209_blackbox_mustBePreparedToReceiveAnOnCompleteSignalWithPrecedingRequestCall();
        void Required_spec209_blackbox_mustBePreparedToReceiveAnOnCompleteSignalWithoutPrecedingRequestCall();
        void Required_spec210_blackbox_mustBePreparedToReceiveAnOnErrorSignalWithPrecedingRequestCall();
        void Untested_spec211_blackbox_mustMakeSureThatAllCallsOnItsMethodsHappenBeforeTheProcessingOfTheRespectiveEvents();
        void Untested_spec212_blackbox_mustNotCallOnSubscribeMoreThanOnceBasedOnObjectEquality();
        void Untested_spec213_blackbox_failingOnSignalInvocation();
        void Required_spec213_blackbox_onSubscribe_mustThrowNullPointerExceptionWhenParametersAreNull();
        void Required_spec213_blackbox_onNext_mustThrowNullPointerExceptionWhenParametersAreNull();
        void Required_spec213_blackbox_onError_mustThrowNullPointerExceptionWhenParametersAreNull();
        void Untested_spec301_blackbox_mustNotBeCalledOutsideSubscriberContext();
        void Untested_spec308_blackbox_requestMustRegisterGivenNumberElementsToBeProduced();
        void Untested_spec310_blackbox_requestMaySynchronouslyCallOnNextOnSubscriber();
        void Untested_spec311_blackbox_requestMaySynchronouslyCallOnCompleteOrOnError();
        void Untested_spec314_blackbox_cancelMayCauseThePublisherToShutdownIfNoOtherSubscriptionExists();
        void Untested_spec315_blackbox_cancelMustNotThrowExceptionAndMustSignalOnError();
        void Untested_spec316_blackbox_requestMustNotThrowExceptionAndMustOnErrorTheSubscriber();
    }
}
