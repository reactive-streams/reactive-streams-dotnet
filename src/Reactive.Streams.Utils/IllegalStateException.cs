using System;

namespace Reactive.Streams.Utils
{
    public class IllegalStateException : Exception
    {
        public IllegalStateException(string message, Exception innerException) : base(message, innerException)
        {

        }

        public IllegalStateException(string message): base(message)
        {
        }
    }
}