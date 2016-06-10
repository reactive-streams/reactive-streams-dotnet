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