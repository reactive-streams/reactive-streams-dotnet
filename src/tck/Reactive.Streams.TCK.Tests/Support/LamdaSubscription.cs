/***************************************************
 * Licensed under MIT No Attribution (SPDX: MIT-0) *
 ***************************************************/
using System;

namespace Reactive.Streams.TCK.Tests.Support
{
    internal sealed class LamdaSubscription : ISubscription
    {
        private readonly Action<long> _onRequest;
        private readonly Action _onCancel;

        public LamdaSubscription(Action<long> onRequest = null, Action onCancel = null)
        {
            _onRequest = onRequest;
            _onCancel = onCancel;
        }

        public void Request(long n) => _onRequest?.Invoke(n);

        public void Cancel() => _onCancel?.Invoke();
    }
}