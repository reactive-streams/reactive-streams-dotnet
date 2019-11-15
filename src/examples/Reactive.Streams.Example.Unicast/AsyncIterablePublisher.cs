using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Reactive.Streams.Example.Unicast
{
    public class AsyncIterablePublisher<T> : IPublisher<T>
    {
        // These represent the protocol of the `AsyncIterablePublishers` SubscriptionImpls
        private interface ISignal { }

        private class CancelSignal : ISignal
        {
            public static CancelSignal Instance { get; } = new CancelSignal();

            private CancelSignal()
            {

            }
        }

        private class SubscribeSignal : ISignal
        {
            public static SubscribeSignal Instance { get; } = new SubscribeSignal();

            private SubscribeSignal()
            {

            }
        }

        private class SendSignal : ISignal
        {
            public static SendSignal Instance { get; } = new SendSignal();

            private SendSignal()
            {

            }
        }

        private class RequestSignal : ISignal
        {
            public RequestSignal(long n)
            {
                N = n;
            }

            public long N { get; }
        }

        private const int DefaultBatchsize = 1024;

        private readonly IEnumerable<T> _elements; // This is our data source / generator
        private readonly int _batchSize; // In general one should be nice nad not hog a thread for too long, this is the cap for that, in elements

        public AsyncIterablePublisher(IEnumerable<T> elements) : this(elements, DefaultBatchsize)
        {
        }

        private AsyncIterablePublisher(IEnumerable<T> elements, int batchSize)
        {
            if (elements == null)
                throw new ArgumentNullException(nameof(elements));
            if (batchSize < 1)
                throw new ArgumentNullException(nameof(batchSize), "batchSize must be greater than zero!");

            _elements = elements;
            _batchSize = batchSize;
        }


        public void Subscribe(ISubscriber<T> subscriber)
        {
            // As per rule 1.11, we have decided to support multiple subscribers in a unicast configuration
            // for this `Publisher` implementation.
            // As per 2.13, this method must return normally (i.e. not throw)
            new SubscriptionImplementation(_elements, _batchSize, subscriber).Init();
        }

        // This is our implementation of the Reactive Streams `Subscription`,
        // which represents the association between a `Publisher` and a `Subscriber`.
        private class SubscriptionImplementation : ISubscription
        {
            private readonly IEnumerable<T> _elements;
            private readonly int _batchSize;
            private readonly ISubscriber<T> _subscriber; // We need a reference to the `Subscriber` so we can talk to it
            private bool _cancelled;  // This flag will track whether this `Subscription` is to be considered cancelled or not
            private long _demand; // Here we track the current demand, i.e. what has been requested but not yet delivered
            private IEnumerator<T> _enumerator; // This is our cursor into the data stream, which we will send to the `Subscriber`

            // This `ConcurrentQueue` will track signals that are sent to this `Subscription`, like `request` and `cancel`
            private readonly ConcurrentQueue<ISignal> _inboundSignals = new ConcurrentQueue<ISignal>();

            // We are using this `AtomicBoolean` to make sure that this `Subscription` doesn't run concurrently with itself,
            // which would violate rule 1.3 among others (no concurrent notifications).
            private readonly AtomicBoolean _on = new AtomicBoolean();

            // This is the main "event loop" if you so will
            private readonly Action _run;

            public SubscriptionImplementation(IEnumerable<T> elements, int batchSize, ISubscriber<T> subscriber)
            {
                // As per rule 1.09, we need to throw a `ArgumentNullException` if the `Subscriber` is `null`
                if (subscriber == null)
                    throw new ArgumentNullException(nameof(subscriber));

                _elements = elements;
                _batchSize = batchSize;
                _subscriber = subscriber;

                // This is the main "event loop" if you so will
                _run = () =>
                {
                    if (_on.Value) // establishes a happens-before relationship with the end of the previous run
                    {
                        try
                        {
                            ISignal signal;
                            if (_inboundSignals.TryDequeue(out signal) && !_cancelled) // to make sure that we follow rule 1.8, 3.6 and 3.7
                            {
                                // Below we simply unpack the `Signal`s and invoke the corresponding method
                                if (signal is RequestSignal)
                                    DoRequest(((RequestSignal)signal).N);
                                else if (signal == SendSignal.Instance)
                                    DoSend();
                                else if (signal == CancelSignal.Instance)
                                    DoCancel();
                                else if (signal == SubscribeSignal.Instance)
                                    DoSubscribe();
                            }
                        }
                        finally
                        {
                            _on.Value = false;  // establishes a happens-before relationship with the beginning of the next run
                            if (!_inboundSignals.IsEmpty) // If we still have signals to process
                                TryScheduleToExecute();
                        }
                    }
                };
            }

            private void DoRequest(long n)
            {
                if (n < 1)
                    TerminateDueTo(new ArgumentException(_subscriber + " violated the Reactive Streams rule 3.9 by requesting a non-positive number of elements"));
                else if (_demand + n < 1)
                {
                    // As governed by rule 3.17, when demand overflows `long.MaxValue` we treat the signalled demand as "effectively unbounded"
                    _demand = long.MaxValue;
                    // Here we protect from the overflow and treat it as "effectively unbounded"
                    DoSend(); // Then we proceed with sending data downstream
                }
                else
                {
                    _demand += n; // Here we record the downstream demand
                    DoSend(); // Then we can proceed with sending data downstream
                }
            }

            // This handles cancellation requests, and is idempotent, thread-safe and not synchronously performing heavy computations as specified in rule 3.5
            private void DoCancel() => _cancelled = true;

            // Instead of executing `subscriber.onSubscribe` synchronously from within `Publisher.subscribe`
            // we execute it asynchronously, this is to avoid executing the user code on the calling thread.
            // It also makes it easier to follow rule 1.9
            private void DoSubscribe()
            {
                try
                {
                    _enumerator = _elements.GetEnumerator();
                }
                catch (Exception ex)
                {
                    // We need to make sure we signal onSubscribe before onError, obeying rule 1.9
                    _subscriber.OnSubscribe(new DummySubscription());
                    TerminateDueTo(ex); // Here we send onError, obeying rule 1.09
                }

                if (!_cancelled)
                {
                    // Deal with setting up the subscription with the subscriber
                    try
                    {
                        _subscriber.OnSubscribe(this);
                    }
                    catch (Exception ex) // Due diligence to obey 2.13
                    {
                        TerminateDueTo(new IllegalStateException(_subscriber + " violated the Reactive Streams rule 2.13 by throwing an exception from onSubscribe.", ex));
                    }

                    // Deal with already complete iterators promptly
                    bool hasElements;
                    try
                    {
                        hasElements = _enumerator.MoveNext();
                    }
                    catch (Exception ex)
                    {
                        TerminateDueTo(ex); // If MoveNext throws, there's something wrong and we need to signal onError as per 1.2, 1.4, 
                        throw;
                    }

                    // If we don't have anything to deliver, we're already done, so lets do the right thing and
                    // not wait for demand to deliver `onComplete` as per rule 1.2 and 1.3
                    if (!hasElements)
                    {
                        try
                        {
                            DoCancel();  // Rule 1.6 says we need to consider the `Subscription` cancelled when `onComplete` is signalled
                            _subscriber.OnComplete();
                        }
                        catch (Exception ex)
                        {
                            // As per rule 2.13, `onComplete` is not allowed to throw exceptions, so we do what we can, and log this.
                            System.Diagnostics.Trace.TraceError(
                                new IllegalStateException(
                                    _subscriber +
                                    " violated the Reactive Streams rule 2.13 by throwing an exception from onComplete.",
                                    ex).StackTrace);
                        }
                    }
                }
            }


            private class DummySubscription : ISubscription
            {
                public void Request(long n) { }

                public void Cancel() { }
            }


            // This is our behavior for producing elements downstream
            private void DoSend()
            {
                try
                {
                    // In order to play nice we will only send at-most `batchSize` before
                    // rescheduing ourselves and relinquishing the current thread.

                    var leftInBatch = _batchSize;

                    do
                    {
                        T next;
                        bool hasNext;
                        try
                        {
                            next = _enumerator.Current;
                            // We have already checked `MoveNext` when subscribing, so we can fall back to testing -after- `Current` is called.
                            hasNext = _enumerator.MoveNext(); // Need to keep track of End-of-Stream
                        }
                        catch (Exception ex)
                        {
                            TerminateDueTo(ex);
                            // If `Current` or `MoveNext` throws (they can, since it is user-provided), we need to treat the stream as errored as per rule 1.4
                            return;
                        }

                        _subscriber.OnNext(next); // Then we signal the next element downstream to the `Subscriber`
                        if (!hasNext) // If we are at End-of-Stream
                        {
                            DoCancel(); // We need to consider this `Subscription` as cancelled as per rule 1.6
                            _subscriber.OnComplete(); // Then we signal `onComplete` as per rule 1.2 and 1.5
                        }
                    } while (!_cancelled && --leftInBatch > 0 && --_demand > 0);

                    if (!_cancelled && _demand > 0)
                        // If the `Subscription` is still alive and well, and we have demand to satisfy, we signal ourselves to send more data
                        Signal(SendSignal.Instance);
                }
                catch (Exception ex)
                {
                    // We can only get here if `onNext` or `onComplete` threw, and they are not allowed to according to 2.13, so we can only cancel and log here.
                    DoCancel();
                    System.Diagnostics.Trace.TraceError(
                               new IllegalStateException(
                                   _subscriber +
                                   " violated the Reactive Streams rule 2.13 by throwing an exception from onNext or onComplete.",
                                   ex).StackTrace);
                }
            }

            // This is a helper method to ensure that we always `cancel` when we signal `onError` as per rule 1.6
            private void TerminateDueTo(Exception exception)
            {
                _cancelled = true; // When we signal onError, the subscription must be considered as cancelled, as per rule 1.6
                try
                {
                    _subscriber.OnError(exception); // Then we signal the error downstream, to the `Subscriber`
                }
                catch (Exception ex)
                {
                    // If `onError` throws an exception, this is a spec violation according to rule 1.9, and all we can do is to log it.
                    System.Diagnostics.Trace.TraceError(
                               new IllegalStateException(
                                   _subscriber +
                                   " violated the Reactive Streams rule 2.13 by throwing an exception from onError.",
                                   ex).StackTrace);
                }
            }

            // What `Signal` does is that it sends signals to the `Subscription` asynchronously
            private void Signal(ISignal signal)
            {
                _inboundSignals.Enqueue(signal);
                TryScheduleToExecute(); // Then we try to schedule it for execution, if it isn't already
            }

            // This method makes sure that this `Subscription` is only running on one Thread at a time,
            // this is important to make sure that we follow rule 1.3
            private void TryScheduleToExecute()
            {
                if (_on.CompareAndSet(false, true))
                {
                    try
                    {
                        Task.Run(_run);
                    }
                    catch (Exception ex)  // If we can't run, we need to fail gracefully
                    {
                        if (!_cancelled)
                        {
                            DoCancel();  // First of all, this failure is not recoverable, so we need to follow rule 1.4 and 1.6
                            try
                            {
                                TerminateDueTo(new IllegalStateException("Publisher terminated due to unavailable Executor.", ex));
                            }
                            finally
                            {
                                ISignal tmp;
                                while (!_inboundSignals.IsEmpty)
                                    _inboundSignals.TryDequeue(out tmp); // We're not going to need these anymore

                                // This subscription is cancelled by now, but letting it become schedulable again means
                                // that we can drain the inboundSignals queue if anything arrives after clearing
                                _on.Value = false;
                            }
                        }
                    }
                }
            }

            // Our implementation of `Subscription.cancel` sends a signal to the Subscription that the `Subscriber` is not interested in any more elements
            public void Cancel() => Signal(CancelSignal.Instance);

            // The reason for the `Init` method is that we want to ensure the `SubscriptionImpl`
            // is completely constructed before it is exposed to the thread pool, therefor this
            // method is only intended to be invoked once, and immediately after the constructor has
            // finished.
            public void Init() => Signal(SubscribeSignal.Instance);

            // Our implementation of `Subscription.request` sends a signal to the Subscription that more elements are in demand
            public void Request(long n) => Signal(new RequestSignal(n));
        }
    }



}
