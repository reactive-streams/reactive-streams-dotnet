using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Reactive.Streams.Example.Unicast
{
    /// <summary>
    ///  AsyncSubscriber is an implementation of Reactive Streams `Subscriber`,
    /// it runs asynchronously, requests one element
    /// at a time, and invokes a user-defined method to process each element.
    /// 
    /// NOTE: The code below uses a lot of try-catches to show the reader where exceptions can be expected, and where they are forbidden.
    /// </summary>
    public abstract class AsyncSubscriber<T> : ISubscriber<T>
    {
        // Signal represents the asynchronous protocol between the Publisher and Subscriber
        private interface ISignal { }

        private class OnCompleteSignal : ISignal
        {
            public static OnCompleteSignal Instance { get; } = new OnCompleteSignal();

            private OnCompleteSignal()
            {

            }
        }

        private sealed class OnErrorSignal : ISignal
        {
            public OnErrorSignal(Exception cause)
            {
                Cause = cause;
            }

            public Exception Cause { get; }
        }

        private sealed class OnNextSignal : ISignal
        {
            public OnNextSignal(T next)
            {
                Next = next;
            }

            public T Next { get; }
        }

        private sealed class OnSubscribeSignal : ISignal
        {
            public OnSubscribeSignal(ISubscription subscription)
            {
                Subscritpion = subscription;
            }

            public ISubscription Subscritpion { get; }
        }


        private ISubscription _subscription;  // Obeying rule 3.1, we make this private!
        private bool _done; // It's useful to keep track of whether this Subscriber is done or not
        
        protected AsyncSubscriber()
        {
            _run = () =>
            {
                if (_on.Value) // establishes a happens-before relationship with the end of the previous run
                {
                    try
                    {
                        ISignal signal;
                        if (!_inboundSignals.TryDequeue(out signal))  // We take a signal off the queue
                            return;

                        if (!_done) // If we're done, we shouldn't process any more signals, obeying rule 2.8
                        {
                            // Below we simply unpack the `Signal`s and invoke the corresponding methods
                            var next = signal as OnNextSignal;
                            if (next != null)
                            {
                                HandleOnNext(next.Next);
                                return;
                            }

                            var subscribe = signal as OnSubscribeSignal;
                            if (subscribe != null)
                            {
                                HandleOnSubscribe(subscribe.Subscritpion);
                                return;
                            }

                            var error = signal as OnErrorSignal;
                            if (error != null)  // We are always able to handle OnError, obeying rule 2.10
                            {
                                HandleOnError(error.Cause);
                                return;
                            }

                            if(signal == OnCompleteSignal.Instance)  // We are always able to handle OnComplete, obeying rule 2.9
                                HandleOnComplete();
                        }
                    }
                    finally
                    {
                        _on.Value = false;  // establishes a happens-before relationship with the beginning of the next run
                        if (!_inboundSignals.IsEmpty) // If we still have signals to process
                            TryScheduleToExecute();  // Then we try to schedule ourselves to execute again
                    }
                }
            };
        }

        /// <summary>
        /// Showcases a convenience method to idempotently marking the Subscriber as "done", so we don't want to process more elements
        /// herefor we also need to cancel our `Subscription`.
        /// </summary>
        private void Done()
        {
            //On this line we could add a guard against `!done`, but since rule 3.7 says that `Subscription.cancel()` is idempotent, we don't need to.
            _done = true; // If `whenNext` throws an exception, let's consider ourselves done (not accepting more elements)
            if (_subscription != null) // If we are bailing out before we got a `Subscription` there's little need for cancelling it.
            {
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
                                    " violated the Reactive Streams rule 3.15 by throwing an exception from cancel.",
                                    ex).StackTrace);
                }
            }
        }

        /// <summary>
        /// This method is invoked when the OnNext signals arrive
        /// Returns whether more elements are desired or not, and if no more elements are desired, for convenience.
        /// </summary>
        protected abstract bool WhenNext(T element);

        /// <summary>
        /// This method is invoked when the OnComplete signal arrives
        /// override this method to implement your own custom onComplete logic.
        /// </summary>
        protected virtual void WhenComplete() { }

        /// <summary>
        /// This method is invoked if the OnError signal arrives
        /// override this method to implement your own custom onError logic.
        /// </summary>
        protected virtual void WhenError(Exception cause) { }

        private void HandleOnSubscribe(ISubscription subscription)
        {
            if (subscription == null)
            {
                // Getting a null `Subscription` here is not valid so lets just ignore it.
            }
            else if (_subscription != null)
                // If someone has made a mistake and added this Subscriber multiple times, let's handle it gracefully
            {
                try
                {
                    subscription.Cancel(); // Cancel the additional subscription to follow rule 2.5
                }
                catch (Exception ex)
                {
                    //Subscription.cancel is not allowed to throw an exception, according to rule 3.15
                    System.Diagnostics.Trace.TraceError(
                        new IllegalStateException(
                            _subscription +
                            " violated the Reactive Streams rule 3.15 by throwing an exception from cancel.",
                            ex).StackTrace);
                }
            }
            else
            {
                // We have to assign it locally before we use it, if we want to be a synchronous `Subscriber`
                // Because according to rule 3.10, the Subscription is allowed to call `onNext` synchronously from within `request`
                _subscription = subscription;

                try
                {
                    // If we want elements, according to rule 2.1 we need to call `request`
                    // And, according to rule 3.2 we are allowed to call this synchronously from within the `onSubscribe` method
                    _subscription.Request(1); // Our Subscriber is unbuffered and modest, it requests one element at a time
                }
                catch (Exception ex)
                {
                    // Subscription.request is not allowed to throw according to rule 3.16
                    System.Diagnostics.Trace.TraceError(
                        new IllegalStateException(
                            _subscription +
                            " violated the Reactive Streams rule 3.16 by throwing an exception from request.",
                            ex).StackTrace);
                }
            }
        }

        private void HandleOnNext(T element)
        {
            if (!_done) // If we aren't already done
            {
                if (_subscription == null) // Technically this check is not needed, since we are expecting Publishers to conform to the spec
                {
                    // Check for spec violation of 2.1 and 1.09
                    System.Diagnostics.Trace.TraceError(
                        new IllegalStateException(
                            "Someone violated the Reactive Streams rule 1.09 and 2.1 by signalling OnNext before `Subscription.request`. (no Subscription)")
                            .StackTrace);
                }
                else
                {
                    try
                    {
                        if (WhenNext(element))
                        {
                            try
                            {
                                _subscription.Request(1); // Our Subscriber is unbuffered and modest, it requests one element at a time
                            }
                            catch (Exception ex)
                            {
                                // Subscription.request is not allowed to throw according to rule 3.16
                                System.Diagnostics.Trace.TraceError(
                                    new IllegalStateException(
                                        _subscription +
                                        " violated the Reactive Streams rule 3.16 by throwing an exception from request.",
                                        ex).StackTrace);
                            }
                        }
                        else
                            Done(); // This is legal according to rule 2.6
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
                                    " violated the Reactive Streams rule 2.13 by throwing an exception from onError.",
                                    e).StackTrace);
                        }
                    }
                }
            }
        }

        // Here it is important that we do not violate 2.2 and 2.3 by calling methods on the `Subscription` or `Publisher`
        private void HandleOnComplete()
        {
            if (_subscription == null)
                // Technically this check is not needed, since we are expecting Publishers to conform to the spec
            {
                // Publisher is not allowed to signal onComplete before onSubscribe according to rule 1.09
                System.Diagnostics.Trace.TraceError(
                    new IllegalStateException(
                        "Publisher violated the Reactive Streams rule 1.09 signalling onComplete prior to onSubscribe.")
                        .StackTrace);
            }
            else
            {
                _done = true; // Obey rule 2.4
                WhenComplete();
            }
        }

        // Here it is important that we do not violate 2.2 and 2.3 by calling methods on the `Subscription` or `Publisher`
        private void HandleOnError(Exception cause)
        {
            if (_subscription == null)
            // Technically this check is not needed, since we are expecting Publishers to conform to the spec
            {
                // Publisher is not allowed to signal onError before onSubscribe according to rule 1.09
                System.Diagnostics.Trace.TraceError(
                    new IllegalStateException(
                        "Publisher violated the Reactive Streams rule 1.09 signalling onError prior to onSubscribe.")
                        .StackTrace);
            }
            else
            {
                _done = true; // Obey rule 2.4
                WhenError(cause);
            }
        }

        // We implement the OnX methods on `Subscriber` to send Signals that we will process asycnhronously, but only one at a time
        
        public void OnSubscribe(ISubscription subscription)
        {
            // As per rule 2.13, we need to throw a `ArgumentNullException` if the `Subscription` is `null`
            if (subscription == null)
                throw new ArgumentNullException(nameof(subscription));

            Signal(new OnSubscribeSignal(subscription));
        }

        public void OnNext(T element)
        {
            // As per rule 2.13, we need to throw a `ArgumentNullException` if the `element` is `null`
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            Signal(new OnNextSignal(element));
        }

        public void OnError(Exception cause)
        {
            // As per rule 2.13, we need to throw a `ArgumentNullException` if the `element` is `null`
            if (cause == null)
                throw new ArgumentNullException(nameof(cause));

            Signal(new OnErrorSignal(cause));
        }

        public void OnComplete() => Signal(OnCompleteSignal.Instance);

        /// <summary>
        /// This `ConcurrentQueue` will track signals that are sent to this `Subscriber`, like `OnComplete` and `OnNext` ,
        /// and obeying rule 2.11
        /// </summary>
        private readonly ConcurrentQueue<ISignal> _inboundSignals = new ConcurrentQueue<ISignal>();

        /// <summary>
        /// We are using this `AtomicBoolean` to make sure that this `Subscriber` doesn't run concurrently with itself,
        /// obeying rule 2.7 and 2.11
        /// </summary>
        private readonly AtomicBoolean _on = new AtomicBoolean();

        private readonly Action _run;

        // What `signal` does is that it sends signals to the `Subscription` asynchronously
        private void Signal(ISignal signal)
        {
            if(signal == null)
                throw new ArgumentNullException(nameof(signal));

            _inboundSignals.Enqueue(signal);
            TryScheduleToExecute(); // Then we try to schedule it for execution, if it isn't already
        }

        // This method makes sure that this `Subscriber` is only executing on one Thread at a time
        private void TryScheduleToExecute()
        {
            if (_on.CompareAndSet(false, true))
            {
                try
                {
                    Task.Run(_run);
                }
                catch (Exception) // If we can't run , we need to fail gracefully and not violate rule 2.13
                {
                    if (!_done)
                    {
                        try
                        {
                            Done(); // First of all, this failure is not recoverable, so we need to cancel our subscription
                        }
                        finally
                        {
                            ISignal tmp;
                            while (!_inboundSignals.IsEmpty) // We're not going to need these anymore
                                _inboundSignals.TryDequeue(out tmp);

                            // This subscription is cancelled by now, but letting the Subscriber become schedulable again means
                            // that we can drain the inboundSignals queue if anything arrives after clearing
                            _on.Value = false;
                        }
                    }
                }
            }
        }
    }
    
}
