using System;

namespace Reactive.Streams.TCK.Tests.Support
{
    internal sealed class LamdaPublisher<T> : IPublisher<T>
    {
        private readonly Action<ISubscriber<T>> _onSubscribe;

        public LamdaPublisher(Action<ISubscriber<T>> onSubscribe)
        {
            _onSubscribe = onSubscribe;
        }

        public void Subscribe(ISubscriber<T> subscriber) => _onSubscribe(subscriber);
    }
}
