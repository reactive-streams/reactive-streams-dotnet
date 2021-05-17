using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Reactive.Streams.TCK.Support
{
    public static class TckAssert
    {
        #region Fail

        /// <summary>
        /// Throws an <see cref="AssertionException"/> with the message and arguments
        /// that are passed in. This is used by the other Assert functions.
        /// </summary>
        /// <param name="message">The message to initialize the <see cref="AssertionException"/> with.</param>
        /// <param name="args">Arguments to be used in formatting the message</param>
        public static void Fail(string message, params object[] args)
        {
            if (message == null) message = string.Empty;
            else if (args != null && args.Length > 0)
                message = string.Format(message, args);

            throw new AssertionException(message);
            //ReportFailure(message);
        }

        /// <summary>
        /// Throws an <see cref="AssertionException"/> with the message that is
        /// passed in. This is used by the other Assert functions.
        /// </summary>
        /// <param name="message">The message to initialize the <see cref="AssertionException"/> with.</param>
        public static void Fail(string message)
        {
            Fail(message, null);
        }

        /// <summary>
        /// Throws an <see cref="AssertionException"/>.
        /// This is used by the other Assert functions.
        /// </summary>
        public static void Fail()
        {
            Fail(string.Empty, null);
        }

        #endregion

        #region Skip

        /// <summary>
        /// Throws an <see cref="SkipException"/> with the message and arguments
        /// that are passed in.  This causes the test to be reported as ignored.
        /// </summary>
        /// <param name="message">The message to initialize the <see cref="AssertionException"/> with.</param>
        /// <param name="args">Arguments to be used in formatting the message</param>
        public static void Skip(string message, params object[] args)
        {
            if (message == null) message = string.Empty;
            else if (args != null && args.Length > 0)
                message = string.Format(message, args);

            /*
            // If we are in a multiple assert block, this is an error
            if (TestExecutionContext.CurrentContext.MultipleAssertLevel > 0)
                throw new Exception("Assert.Ignore may not be used in a multiple assertion block.");
            */

            throw new SkipException(message);
        }

        /// <summary>
        /// Throws an <see cref="SkipException"/> with the message that is
        /// passed in. This causes the test to be reported as ignored.
        /// </summary>
        /// <param name="message">The message to initialize the <see cref="AssertionException"/> with.</param>
        public static void Skip(string message)
        {
            Skip(message, null);
        }

        /// <summary>
        /// Throws an <see cref="SkipException"/>.
        /// This causes the test to be reported as ignored.
        /// </summary>
        public static void Skip()
        {
            Skip(string.Empty, null);
        }

        #endregion
    }
}
