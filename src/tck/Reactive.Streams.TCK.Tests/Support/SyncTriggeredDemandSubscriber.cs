/***************************************************
 * Licensed under MIT No Attribution (SPDX: MIT-0) *
 ***************************************************/
using System;
using Reactive.Streams.TCK.Support;

namespace Reactive.Streams.TCK.Tests.Support
{
    /// <summary>
    /// SyncTriggeredDemandSubscriber is an implementation of Reactive Streams `Subscriber`,
    /// it runs synchronously (on the Publisher's thread) and requests demand triggered from
    /// "the outside" using its `triggerDemand` method and from "the inside" using the return
    /// value of its user-defined `whenNext` method which is invoked to process each element.
    /// 
    /// NOTE: The code below uses a lot of try-catches to show the reader where exceptions can be expected, and where they are forbidden.
    /// </summary>
    public abstract class SyncTriggeredDemandSubscriber<T> : ISubscriber<T>
    {
        private ISubscription _subscription; // Obeying rule 3.1, we make this private!
        private bool _done;

        public virtual void OnSubscribe(ISubscription subscription)
        {
            // As per rule 2.13, we need to throw a `ArgumentNullException` if the `Subscription` is `null`
            if(subscription == null)
                throw new ArgumentNullException(nameof(subscription));

            if (_subscription != null)
                // If someone has made a mistake and added this Subscriber multiple times, let's handle it gracefully
            {
                try
                {
                    subscription.Cancel(); // Cancel the additional subscription
                }
                catch (Exception ex)
                {
                    //Subscription.cancel is not allowed to throw an exception, according to rule 3.15
                    System.Diagnostics.Trace.TraceError(
                        new IllegalStateException(
                            subscription +
                            " violated the Reactive Streams rule 3.15 by throwing an exception from cancel.", ex)
                            .StackTrace);
                }
            }
            else
                // We have to assign it locally before we use it, if we want to be a synchronous `Subscriber`
                // Because according to rule 3.10, the Subscription is allowed to call `onNext` synchronously from within `request`
                _subscription = subscription;
        }

        /// <summary>
        /// Requests the provided number of elements from the `Subscription` of this `Subscriber`.
        /// NOTE: This makes no attempt at thread safety so only invoke it once from the outside to initiate the demand.
        /// </summary>
        /// <returns>`true` if successful and `false` if not (either due to no `Subscription` or due to exceptions thrown)</returns>
        public virtual bool TriggerDemand(long n)
        {
            if (_subscription == null)
                return false;
            try
            {
                _subscription.Request(n);
            }
            catch (Exception ex)
            {
                // Subscription.request is not allowed to throw according to rule 3.16
                System.Diagnostics.Trace.TraceError(
                    new IllegalStateException(
                        _subscription +
                        " violated the Reactive Streams rule 3.15 by throwing an exception from request.", ex)
                        .StackTrace);
                return false;
            }

            return true;
        }

        public virtual void OnNext(T element)
        {
            if (_subscription == null)
                // Technically this check is not needed, since we are expecting Publishers to conform to the spec
                System.Diagnostics.Trace.TraceError(Environment.StackTrace,
                    new IllegalStateException(
                        "Publisher violated the Reactive Streams rule 1.09 signalling onNext prior to onSubscribe."));
            else
            {
                // As per rule 2.13, we need to throw a `ArgumentNullException` if the `element` is `null`
                if(element == null)
                    throw new ArgumentNullException(nameof(element));

                if (!_done) // If we aren't already done
                {
                    try
                    {
                        var need = Foreach(element);
                        if (need > 0)
                            TriggerDemand(need);
                        else if (need == 0)
                        {

                        }
                        else
                            Done();
                    }
                    catch (Exception ex)
                    {
                        Done();
                        try
                        {
                            OnError(ex);
                        }
                        catch (Exception e)
                        {
                            //Subscriber.onError is not allowed to throw an exception, according to rule 2.13
                            System.Diagnostics.Trace.TraceError(
                                new IllegalStateException(
                                    this +
                                    " violated the Reactive Streams rule 2.13 by throwing an exception from onError.", e)
                                    .StackTrace);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Showcases a convenience method to idempotently marking the Subscriber as "done", so we don't want to process more elements
        /// herefor we also need to cancel our `Subscription`.
        /// </summary>
        private void Done()
        {
            //On this line we could add a guard against `!done`, but since rule 3.7 says that `Subscription.cancel()` is idempotent, we don't need to.
            _done = true;  // If we `whenNext` throws an exception, let's consider ourselves done (not accepting more elements)
            try
            {
                _subscription.Cancel(); // Cancel the subscription
            }
            catch (Exception ex)
            {
                //Subscription.cancel is not allowed to throw an exception, according to rule 3.15
                System.Diagnostics.Trace.TraceError(
                    new IllegalStateException(
                        _subscription +
                        " violated the Reactive Streams rule 3.15 by throwing an exception from cancel.", ex)
                        .StackTrace);
            }
        }

        /// <summary>
        /// This method is left as an exercise to the reader/extension point
        /// Don't forget to call `TriggerDemand` at the end if you are interested in more data,
        /// a return value of lower than 0 indicates that the subscription should be cancelled,
        /// a value of 0 indicates that there is no current need,
        /// a value of greater than 0 indicates the current need.
        /// </summary>
        protected abstract long Foreach(T element);

        public virtual void OnError(Exception cause)
        {
            if (_subscription == null)
                // Technically this check is not needed, since we are expecting Publishers to conform to the spec
                System.Diagnostics.Trace.TraceError(Environment.StackTrace,
                    new IllegalStateException(
                        "Publisher violated the Reactive Streams rule 1.09 signalling onError prior to onSubscribe."));
            else
            {
                // As per rule 2.13, we need to throw a `ArgumentNullException` if the `Throwable` is `null`
                if(cause == null)
                    throw new ArgumentNullException(nameof(cause));

                // Here we are not allowed to call any methods on the `Subscription` or the `Publisher`, as per rule 2.3
                // And anyway, the `Subscription` is considered to be cancelled if this method gets called, as per rule 2.4
            }
        }

        public virtual void OnComplete()
        {
            if (_subscription == null)
                // Technically this check is not needed, since we are expecting Publishers to conform to the spec
                System.Diagnostics.Trace.TraceError(Environment.StackTrace,
                    new IllegalStateException(
                        "Publisher violated the Reactive Streams rule 1.09 signalling onError prior to onSubscribe."));
            else
            {
                // Here we are not allowed to call any methods on the `Subscription` or the `Publisher`, as per rule 2.3
                // And anyway, the `Subscription` is considered to be cancelled if this method gets called, as per rule 2.4
            }
        }
    }
}
