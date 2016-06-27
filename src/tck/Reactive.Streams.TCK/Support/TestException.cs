using System;

namespace Reactive.Streams.TCK.Support
{
    /// <summary>
    /// Exception used by the TCK to signal failures.
    /// May be thrown or signalled through <see cref="ISubscriber.OnError"/>
    /// </summary>
    public sealed class TestException : Exception
    {
        public TestException() : base("Test Exception: Boom!")
        {
            
        }
    }
}
