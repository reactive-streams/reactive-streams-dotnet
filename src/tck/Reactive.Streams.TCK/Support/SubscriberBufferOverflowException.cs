/***************************************************
 * Licensed under MIT No Attribution (SPDX: MIT-0) *
 ***************************************************/
using System;

namespace Reactive.Streams.TCK.Support
{
    public class SubscriberBufferOverflowException : Exception
    {
        public SubscriberBufferOverflowException(string message) : base(message)
        {
            
        }

        public SubscriberBufferOverflowException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}