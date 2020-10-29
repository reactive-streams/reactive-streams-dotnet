/***************************************************
 * Licensed under MIT No Attribution (SPDX: MIT-0) *
 ***************************************************/
using System;

namespace Reactive.Streams.TCK.Support
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