using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reactive.Streams.TCK.Support
{
    /// <summary>
    /// Thrown when an assertion failed.
    /// </summary>
    [Serializable]
    public class AssertionException : Exception
    {
        /// <param name="message">The error message that explains 
        /// the reason for the exception</param>
        public AssertionException(string message) : base(message)
        { }

        /// <param name="message">The error message that explains 
        /// the reason for the exception</param>
        /// <param name="inner">The exception that caused the 
        /// current exception</param>
        public AssertionException(string message, Exception inner) :
            base(message, inner)
        { }

#if SERIALIZATION
        /// <summary>
        /// Serialization Constructor
        /// </summary>
        protected AssertionException(System.Runtime.Serialization.SerializationInfo info, 
            System.Runtime.Serialization.StreamingContext context) : base(info,context)
        {}
#endif

        /*
        /// <summary>
        /// Gets the ResultState provided by this exception
        /// </summary>
        public override ResultState ResultState
        {
            get { return ResultState.Failure; }
        }
        */
    }
}
