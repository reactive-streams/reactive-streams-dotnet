/***************************************************
 * Licensed under MIT No Attribution (SPDX: MIT-0) *
 ***************************************************/
using System;
using NUnit.Framework;
using Reactive.Streams.TCK.Support;

namespace Reactive.Streams.TCK.Tests.Support
{
    /// <summary>
    /// Provides assertions to validate the TCK tests themselves,
    /// with the goal of guaranteeing proper error messages when an implementation does not pass a given TCK test.
    /// 
    /// "Quis custodiet ipsos custodes?" -- Iuvenalis
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public class TCKVerificationSupport
    {
        // INTERNAL ASSERTION METHODS //

        /// <summary>
        /// Runs given code block and expects it to fail with an "Expected onError" failure.
        /// Use this method to validate that TCK tests fail with meaningful errors instead of NullPointerExceptions etc.
        /// </summary>
        /// <param name="throwingRun">encapsulates test case which we expect to fail</param>
        /// <param name="messagePart">the exception failing the test (inside the run parameter) must contain this message part in one of it's causes</param>
        public void RequireTestFailure(Action throwingRun, string messagePart)
        {
            try
            {
                throwingRun();
            }
            catch (Exception ex)
            {
                if (FindDeepErrorMessage(ex, messagePart))
                    return;

                throw new Exception($"Expected TCK to fail with '... {messagePart} ...', " +
                                    $"yet `{ex.GetType().Name}({ex.Message})` was thrown and test would fail with not useful error message!", ex);
            }

            throw new Exception($"Expected TCK to fail with '... {messagePart} ...', " +
                                "yet no exception was thrown and test would pass unexpectedly!");
        }

        /// <summary>
        ///  Runs given code block and expects it fail with an <see cref="IgnoreException"/>
        /// </summary>
        /// <param name="throwingRun">encapsulates test case which we expect to be skipped</param>
        /// <param name="messagePart">the exception failing the test (inside the run parameter) must contain this message part in one of it's causes</param>
        public void RequireTestSkip(Action throwingRun, string messagePart)
        {
            try
            {
                throwingRun();
            }
            catch (IgnoreException ignore)
            {
                if(ignore.Message.Contains(messagePart))
                    return;

                throw new Exception($"Expected TCK to skip this test with '... {messagePart} ...', " +
                                    $"yet it skipped with ({ignore.Message}) instead!", ignore);
            }
            catch (Exception ex)
            {
                throw new Exception(
                    $"Expected TCK to skip this test, yet it threw {ex.GetType().Name}({ex.Message}) instead!", ex);
            }

            throw new Exception($"Expected TCK to fail with '... {messagePart} ...', " +
                                "yet no exception was thrown and test would pass unexpectedly!");
        }

        /// <summary>
        /// This publisher does NOT fulfil all Publisher spec requirements.
        /// It's just the bare minimum to enable this test to fail the Subscriber tests.
        /// </summary>
        public IPublisher<int> NewSimpleIntsPublisher(long maxElementsToEmit)
            => new SimpleIntsPublisher(maxElementsToEmit);

        private sealed class SimpleIntsPublisher : IPublisher<int>
        {
            private sealed class SimpleIntsSubscribtion : ISubscription
            {
                private readonly SimpleIntsPublisher _publisher;
                private readonly AtomicCounterLong _nums = new AtomicCounterLong();
                private readonly AtomicBoolean _active = new AtomicBoolean(true);

                public SimpleIntsSubscribtion(SimpleIntsPublisher publisher)
                {
                    _publisher = publisher;
                }

                public void Request(long n)
                {
                    var thisDemand = n;
                    while (_active.Value && thisDemand > 0 && _nums.Current < _publisher._maxElementsToEmit)
                    {
                        _publisher._subscriber.OnNext((int)_nums.GetAndIncrement());
                        thisDemand--;
                    }

                    if (_nums.Current == _publisher._maxElementsToEmit)
                        _publisher._subscriber.OnComplete();
                }

                public void Cancel() => _active.Value = false;
            }

            private ISubscriber<int> _subscriber;
            private readonly long _maxElementsToEmit;

            public SimpleIntsPublisher(long maxElementsToEmit)
            {
                _maxElementsToEmit = maxElementsToEmit;
            }

            public void Subscribe(ISubscriber<int> subscriber)
            {
                _subscriber = subscriber;
                subscriber.OnSubscribe(new SimpleIntsSubscribtion(this));
            }
        }

        /// <summary>
        /// Looks for expected error message prefix inside of causes of thrown throwable.
        /// </summary>
        /// <returns>true if one of the causes indeed contains expected error, false otherwise</returns>
        public bool FindDeepErrorMessage(Exception exception, string messagePart)
            => FindDeepErrorMessage(exception, messagePart, 5);

        private bool FindDeepErrorMessage(Exception exception, string messagePart, int depth)
        {
            if (exception is NullReferenceException)
            {
                Assert.Fail($"{typeof(NullReferenceException).Name} was thrown, definitely not a helpful error!",
                    exception);
            }
            if (exception == null || depth == 0)
                return false;

            var message = exception.Message;
            return message.Contains(messagePart) ||
                   FindDeepErrorMessage(exception.InnerException, messagePart, depth - 1);
        }
    }
}
