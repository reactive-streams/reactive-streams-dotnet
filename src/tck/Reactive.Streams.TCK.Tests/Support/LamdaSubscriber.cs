using System;
using System.Runtime.CompilerServices;

namespace Reactive.Streams.TCK.Tests.Support
{
    internal sealed class LamdaSubscriber<T> : ISubscriber<T>
    {
        private readonly Action<T> _onNext;
        private readonly Action<ISubscription> _onSubscribe;
        private readonly Action<Exception> _onError;
        private readonly Action _onComplete;

        public LamdaSubscriber(Action<T> onNext = null, Action<ISubscription> onSubscribe = null,
            Action<Exception> onError = null, Action onComplete = null)
        {
            _onNext = onNext ?? (_ => { });
            _onSubscribe = onSubscribe ?? (_ => { });
            _onError = onError ?? (_ => { });
            _onComplete = onComplete ?? (() => { });
        }

        public void OnNext(T element) => _onNext(element);

        public void OnSubscribe(ISubscription subscription) => _onSubscribe(subscription);

        // Make sure we see the method in the stack trace in release mode 
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void OnError(Exception cause) => _onError(cause);

        // Make sure we see the method in the stack trace in release mode 
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void OnComplete() => _onComplete();
    }
}
