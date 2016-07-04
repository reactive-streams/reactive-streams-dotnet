using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using Reactive.Streams.TCK.Support;

namespace Reactive.Streams.TCK
{
    public class TestEnvironment
    {
        public const int TestBufferSize = 16;
        private const string DefaultTimeoutMillisEnv = "DEFAULT_TIMEOUT_MILLIS";
        private const long DefaultTimeoutMillis = 500;
        private const string DefaultNoSignalsTimeoutMillisEnv = "DEFAULT_NO_SIGNALS_TIMEOUT_MILLIS";
        

        /// <summary>
        /// Tests must specify the timeout for expected outcome of asynchronous
        /// interactions. Longer timeout does not invalidate the correctness of
        /// the implementation, but can in some cases result in longer time to
        /// run the tests.
        /// </summary>
        /// <param name="defaultTimeoutMilliseconds">default timeout to be used in all expect* methods</param>
        /// <param name="defaultNoSignalsTimeoutMilliseconds">default timeout to be used when no further signals are expected anymore</param>
        /// <param name="writeLineDebug">if true, signals such as OnNext / Request / OnComplete etc will be printed to standard output. Default: false</param>
        public TestEnvironment(long defaultTimeoutMilliseconds, long defaultNoSignalsTimeoutMilliseconds, bool writeLineDebug = false)
        {
            DefaultTimeoutMilliseconds = defaultTimeoutMilliseconds;
            DefaultNoSignalsTimeoutMilliseconds = defaultNoSignalsTimeoutMilliseconds;
            WriteLineDebug = writeLineDebug;
        }

        /// <summary>
        /// Tests must specify the timeout for expected outcome of asynchronous
        /// interactions. Longer timeout does not invalidate the correctness of
        /// the implementation, but can in some cases result in longer time to
        /// run the tests.
        /// </summary>
        /// <param name="defaultTimeoutMilliseconds">default timeout to be used in all expect* methods</param>
        public TestEnvironment(long defaultTimeoutMilliseconds)
            : this(defaultTimeoutMilliseconds, defaultTimeoutMilliseconds)
        {
        }

        /// <summary>
        /// Tests must specify the timeout for expected outcome of asynchronous
        /// interactions. Longer timeout does not invalidate the correctness of
        /// the implementation, but can in some cases result in longer time to
        /// run the tests.
        /// </summary>
        /// <param name="writeLineDebug">if true, signals such as OnNext / Request / OnComplete etc will be printed to standard output</param>
        public TestEnvironment(bool writeLineDebug)
            : this(
                EnvironmentDefaultTimeoutMilliseconds(),
                EnvironmentDefaultNoSignalsTimeoutMilliseconds(),
                writeLineDebug)
        {
        }

        /// <summary>
        /// Tests must specify the timeout for expected outcome of asynchronous
        /// interactions. Longer timeout does not invalidate the correctness of
        /// the implementation, but can in some cases result in longer time to
        /// run the tests.
        /// </summary>
        public TestEnvironment()
            : this(EnvironmentDefaultTimeoutMilliseconds(), EnvironmentDefaultNoSignalsTimeoutMilliseconds())
        {
        }

        /// <summary>
        /// This timeout is used when waiting for a signal to arrive.
        /// </summary>
        public long DefaultTimeoutMilliseconds { get; }

        /// <summary>
        /// This timeout is used when asserting that no further signals are emitted.
        /// Note that this timeout default
        /// </summary>
        public long DefaultNoSignalsTimeoutMilliseconds { get; }

        /// <summary>
        /// If true, signals such as OnNext / Request / OnComplete etc will be printed to standard output
        /// </summary>
        public bool WriteLineDebug { get; }

        public ConcurrentQueue<Exception> AsyncErrors { get; } = new ConcurrentQueue<Exception>();

        /// <summary>
        /// Tries to parse the env variable DEFAULT_TIMEOUT_MILLIS as long.
        /// </summary>
        /// <exception cref="ArgumentException">When unable to parse the environment variable</exception>
        /// <returns>The value if present OR its default value</returns>
        public static long EnvironmentDefaultTimeoutMilliseconds()
        {
            var environmentMilliseconds = Environment.GetEnvironmentVariable(DefaultTimeoutMillisEnv);
            if (environmentMilliseconds == null)
                return DefaultTimeoutMillis;

            try
            {
                return long.Parse(environmentMilliseconds);
            }
            catch (Exception ex)
            {
                throw new ArgumentException(
                    $"Unable to parse {DefaultTimeoutMillisEnv} environment value {environmentMilliseconds} as long!",
                    ex);
            }
        }

        /// <summary>
        /// Tries to parse the env variable DEFAULT_NO_SIGNALS_TIMEOUT_MILLIS as long.
        /// </summary>
        /// <exception cref="ArgumentException">When unable to parse the environment variable</exception>
        /// <returns>The value if present OR its default value</returns>
        public static long EnvironmentDefaultNoSignalsTimeoutMilliseconds()
        {
            var environmentMilliseconds = Environment.GetEnvironmentVariable(DefaultNoSignalsTimeoutMillisEnv);
            if (environmentMilliseconds == null)
                return EnvironmentDefaultTimeoutMilliseconds();

            try
            {
                return long.Parse(environmentMilliseconds);
            }
            catch (Exception ex)
            {
                throw new ArgumentException(
                    $"Unable to parse {DefaultNoSignalsTimeoutMillisEnv} environment value {environmentMilliseconds} as long!",
                    ex);
            }
        }

        /// <summary>
        /// To flop means to "fail asynchronously", either by onErroring or by failing some TCK check triggered asynchronously.
        /// This method does *NOT* fail the test - it's up to inspections of the error to fail the test if required.
        /// 
        /// Use <see cref="VerifyNoAsyncErrorsNoDelay"/> at the end of your TCK tests to verify there no flops called during it's execution.
        /// To check investigate asyncErrors more closely you can use <see cref="ManualSubscriber{T}.ExpectError{E}()"/> methods or collect the error directly
        /// from the environment using <see cref="DropAsyncError"/>.
        /// 
        ///  To clear asyncErrors you can call <see cref="ClearAsyncErrors"/>
        /// </summary>
        public void Flop(string message)
        {
            try
            {
                Assert.Fail(message);
            }
            catch (Exception ex)
            {
                AsyncErrors.Enqueue(ex);
            }
        }

        /// <summary>
        /// To flop means to "fail asynchronously", either by onErroring or by failing some TCK check triggered asynchronously.
        /// This method does *NOT* fail the test - it's up to inspections of the error to fail the test if required.
        /// 
        /// This overload keeps the passed in throwable as the asyncError, instead of creating an AssertionError for this.
        /// 
        /// Use <see cref="VerifyNoAsyncErrorsNoDelay"/> at the end of your TCK tests to verify there no flops called during it's execution.
        /// To check investigate asyncErrors more closely you can use <see cref="ManualSubscriber{T}.ExpectError{E}()"/> methods or collect the error directly
        /// from the environment using <see cref="DropAsyncError"/>.
        /// 
        /// To clear asyncErrors you can call <see cref="ClearAsyncErrors"/>
        /// </summary>
        public void Flop(Exception exception, string message)
        {
            try
            {
                Assert.Fail(message, exception);
            }
            catch (Exception)
            {
                AsyncErrors.Enqueue(exception);
            }
        }

        /// <summary>
        /// To flop means to "fail asynchronously", either by onErroring or by failing some TCK check triggered asynchronously.
        /// This method does *NOT* fail the test - it's up to inspections of the error to fail the test if required.
        /// 
        /// This overload keeps the passed in throwable as the asyncError, instead of creating an AssertionError for this.
        /// 
        /// Use <see cref="VerifyNoAsyncErrorsNoDelay"/> at the end of your TCK tests to verify there no flops called during it's execution.
        /// To check investigate asyncErrors more closely you can use <see cref="ManualSubscriber{T}.ExpectError{E}()"/> methods or collect the error directly
        /// from the environment using <see cref="DropAsyncError"/>.
        /// 
        /// To clear asyncErrors you can call <see cref="ClearAsyncErrors"/>
        /// </summary>
        public void Flop(Exception exception)
        {
            try
            {
                Assert.Fail(exception.Message, exception);
            }
            catch (Exception)
            {
                AsyncErrors.Enqueue(exception);
            }
        }

        /// <summary>
        /// To flop means to "fail asynchronously", either by onErroring or by failing some TCK check triggered asynchronously.
        /// 
        /// This method DOES fail the test right away (it tries to, by throwing an AssertionException),
        /// in such it is different from <see cref="Flop(string)"/> which only records the error.
        /// 
        /// Use <see cref="VerifyNoAsyncErrorsNoDelay"/> at the end of your TCK tests to verify there no flops called during it's execution.
        /// To check investigate asyncErrors more closely you can use <see cref="ManualSubscriber{T}.ExpectError{E}()"/> methods or collect the error directly
        /// from the environment using <see cref="DropAsyncError"/>.
        /// 
        /// To clear asyncErrors you can call <see cref="ClearAsyncErrors"/>
        /// </summary>
        public T FlopAndFail<T>(string message)
        {
            try
            {
                Assert.Fail(message);
            }
            catch (Exception ex)
            {
                AsyncErrors.Enqueue(ex);
                Assert.Fail(message, ex);
            }

            return default(T); // unreachable, the previous block will always exit by throwing
        }


        public void Subscribe<T>(IPublisher<T> publisher, TestSubscriber<T> subscriber)
            => Subscribe(publisher, subscriber, DefaultTimeoutMilliseconds);

        public void Subscribe<T>(IPublisher<T> publisher, TestSubscriber<T> subscriber, long timeoutMilliseconds)
        {
            publisher.Subscribe(subscriber);
            subscriber.Subscription.ExpectCompletion(timeoutMilliseconds, $"Could not subscribe {subscriber}, to Publisher {publisher}");
            VerifyNoAsyncErrorsNoDelay();
        }

        public ManualSubscriber<T> NewBlackholeSubscriber<T>(IPublisher<T> publisher)
        {
            var subscriber = new BlackholeSubscriberWithSubscriptionSupport<T>(this);
            Subscribe(publisher, subscriber, DefaultTimeoutMilliseconds);
            return subscriber;
        }

        public ManualSubscriber<T> NewManualSubscriber<T>(IPublisher<T> publisher)
            => NewManualSubscriber(publisher, DefaultTimeoutMilliseconds);

        public ManualSubscriber<T> NewManualSubscriber<T>(IPublisher<T> publisher, long timeoutMilliseconds)
        {
            var subscriber = new ManualSubscriberWithSubscriptionSupport<T>(this);
            Subscribe(publisher, subscriber, timeoutMilliseconds);
            return subscriber;
        }

        public void ClearAsyncErrors ()
        {
            Exception tmp;
            while (!AsyncErrors.IsEmpty)
                AsyncErrors.TryDequeue(out tmp);
        }

        public Exception DropAsyncError()
        {
            Exception result;
            AsyncErrors.TryDequeue(out result);
            return result;
        }

        /// <summary>
        /// Waits for <see cref="DefaultTimeoutMilliseconds"/> and then verifies that no asynchronous errors
        /// were signalled pior to, or during that time (by calling <see cref="Flop(string)"/>).
        /// </summary>
        public void VerifyNoAsyncErrors() => VerifyNoAsyncErrors(DefaultNoSignalsTimeoutMilliseconds);

        /// <summary>
        /// This version of <see cref="VerifyNoAsyncErrors()"/> should be used when errors still could be signalled
        /// asynchronously during <see cref="DefaultTimeoutMilliseconds"/> time.
        /// <para />
        ///  It will immediatly check if any async errors were signaled (using <see cref="Flop(string)"/>),
        /// and if no errors encountered wait for another default timeout as the errors may yet be signalled.
        /// The initial check is performed in order to fail-fast in case of an already failed test.
        /// </summary>
        public void VerifyNoAsyncErrors(long delay)
        {
            VerifyNoAsyncErrorsNoDelay();

            Thread.Sleep(TimeSpan.FromMilliseconds(delay));
            VerifyNoAsyncErrorsNoDelay();
        }

        /// <summary>
        /// Verifies that no asynchronous errors were signalled pior to calling this method (by calling <see cref="Flop(string)"/>).
        /// This version of <see cref="VerifyNoAsyncErrors()"/> <b> does not wait before checking for asynchronous errors </b>, and is to be used
        /// for example in tight loops etc.
        /// </summary>
        public void VerifyNoAsyncErrorsNoDelay()
        {
            foreach (var error in AsyncErrors)
            {
                var exception = error as AssertionException;
                if (exception != null)
                    throw exception;

                Assert.Fail($"Async error during test execution: {error.Message}", error);
            }
        }

        /// <summary>
        /// If <see cref="WriteLineDebug"/> is true, print debug message to std out.
        /// </summary>
        public void Debug(string message)
        {
            if(WriteLineDebug)
                Console.WriteLine($"[TCK-DEBUG] {message}");
        }

        /// <summary>
        /// Looks for given <paramref name="method"/> in stack trace.
        /// Can be used to answer questions like "was this method called from OnComplete?". 
        /// </summary>
        /// <param name="method">The method to search for</param>
        /// <returns>The line from the stack trace where the method was found or <see cref="string.Empty"/></returns>
        public string FindCallerMethodInStackTrace(string method)
        {
            var stack = Environment.StackTrace;

            foreach (var line in stack.Split('\n', '\r'))
                if (line.Contains(method))
                    return line;

            return string.Empty;
        }
    }

    /// <summary>
    /// <see cref="ISubscriber{T}"/> implementation which can be steered by test code and asserted on.
    /// </summary>
    public class ManualSubscriber<T> : TestSubscriber<T>
    {
        private readonly Receptacle<T> _received;

        public ManualSubscriber(TestEnvironment environment) : base(environment)
        {
            _received = new Receptacle<T>(environment);
        }

        public override void OnNext(T element)
        {
            try
            {
                _received.Add(element);
            }
            catch (InvalidOperationException ex)
            {
                //error message refinment
                throw new SubscriberBufferOverflowException(
                    $"Received more than bufferSize ({Receptacle<T>.QueueSize}) OnNext signals. The Publisher probably emited more signals than expected!",
                    ex);
            }
        }

        public override void OnComplete() => _received.Complete();

        public virtual void Request(long elements) => Subscription.Value.Request(elements);

        public T RequestNextElement() => RequestNextElement(Environment.DefaultTimeoutMilliseconds);

        public T RequestNextElement(long timeoutMilliseconds)
            => RequestNextElement(timeoutMilliseconds, "Did not receive expected element");

        public T RequestNextElement(string errorMessage)
            => RequestNextElement(Environment.DefaultTimeoutMilliseconds, errorMessage);

        public virtual T RequestNextElement(long timeoutMilliseconds, string errorMessage)
        {
            Request(1);
            return NextElement(timeoutMilliseconds, errorMessage);
        }

        public Option<T> RequestNextElementOrEndOfStream(string errorMessage)
            => RequestNextElementOrEndOfStream(Environment.DefaultTimeoutMilliseconds, errorMessage);

        public Option<T> RequestNextElementOrEndOfStream(long timeoutMilliseconds)
            => RequestNextElementOrEndOfStream(timeoutMilliseconds, "Did not receive expected stream completion");

        public virtual Option<T> RequestNextElementOrEndOfStream(long timeoutMilliseconds, string errorMessage)
        {
            Request(1);
            return NextElementOrEndOfStream(timeoutMilliseconds, errorMessage);
        }

        public void RequestEndOfStream()
            => RequestEndOfStream(Environment.DefaultTimeoutMilliseconds, "Did not receive expected stream completion");

        public void RequestEndOfStream(long timeoutMilliseconds)
            => RequestEndOfStream(timeoutMilliseconds, "Did not receive expected stream completion");

        public void RequestEndOfStream(string errorMessage)
            => RequestEndOfStream(Environment.DefaultTimeoutMilliseconds, errorMessage);

        public virtual void RequestEndOfStream(long timeoutMilliseconds, string errorMessage)
        {
            Request(1);
            ExpectCompletion(timeoutMilliseconds, errorMessage);
        }

        public virtual List<T> RequestNextElements(long elements)
        {
            Request(elements);
            return NextElements(elements, Environment.DefaultTimeoutMilliseconds);
        }

        public virtual List<T> RequestNextElements(long elements, long timeoutMilliseconds)
        {
            Request(elements);
            return NextElements(elements, timeoutMilliseconds, $"Did not receive {elements} expected elements");
        }
        
        public virtual List<T> RequestNextElements(long elements, long timeoutMilliseconds, string errorMessage)
        {
            Request(elements);
            return NextElements(elements, timeoutMilliseconds, errorMessage);
        }

        public T NextElement() => NextElement(Environment.DefaultTimeoutMilliseconds);

        public T NextElement(long timeoutMilliseconds)
            => NextElement(timeoutMilliseconds, "Did not receive expected element");

        public T NextElement(string errorMessage) => NextElement(Environment.DefaultTimeoutMilliseconds, errorMessage);

        public virtual T NextElement(long timeoutMilliseconds, string errorMessage)
            => _received.Next(timeoutMilliseconds, errorMessage);

        public Option<T> NextElementOrEndOfStream() =>
            NextElementOrEndOfStream(Environment.DefaultTimeoutMilliseconds,
                "Did not receive expected stream completion");

        public Option<T> NextElementOrEndOfStream(long timeoutMilliseconds) =>
            NextElementOrEndOfStream(timeoutMilliseconds,
                "Did not receive expected stream completion");

        public Option<T> NextElementOrEndOfStream(string errorMessage)
            => NextElementOrEndOfStream(Environment.DefaultTimeoutMilliseconds, errorMessage);

        public virtual Option<T> NextElementOrEndOfStream(long timeoutMilliseconds, string errorMessage)
            => _received.NextOrEndOfStream(timeoutMilliseconds, errorMessage);
        
        public List<T> NextElements(long elements) =>
            NextElements(elements, Environment.DefaultTimeoutMilliseconds,
                "Did not receive expected element or completion");

        public List<T> NextElements(long elements, string errorMessage)
            => NextElements(elements, Environment.DefaultTimeoutMilliseconds, errorMessage);
        
        public List<T> NextElements(long elements, long timeoutMilliseconds)
            => NextElements(elements, timeoutMilliseconds, "Did not receive expected element or completion");

        public virtual List<T> NextElements(long elements, long timeoutMilliseconds, string errorMessage)
            => _received.NextN(elements, timeoutMilliseconds, errorMessage);

        public void ExpectNext(T expected) => ExpectNext(expected, Environment.DefaultTimeoutMilliseconds);

        public virtual void ExpectNext(T expected, long timeoutMilliseconds)
        {
            var received = NextElement(timeoutMilliseconds, "Did not receive expected element on downstrean");
            if(!received.Equals(expected))
                Environment.Flop($"Expected element {expected} on downstream but received {received}");
        }

        public void ExpectCompletion() => ExpectCompletion(Environment.DefaultTimeoutMilliseconds);

        public void ExpectCompletion(long timeoutMilliseconds)
            => ExpectCompletion(timeoutMilliseconds, "Did not receive expected stream completion");

        public void ExpectCompletion(string errorMessage)
            => ExpectCompletion(Environment.DefaultTimeoutMilliseconds, errorMessage);

        public virtual void ExpectCompletion(long timeoutMilliseconds, string errorMessage)
            => _received.ExpectCompletion(timeoutMilliseconds, errorMessage);

        public void ExpectErrorWithMessage<E>(string requiredMessagePart) where E : Exception
            => ExpectErrorWithMessage<E>(requiredMessagePart, Environment.DefaultTimeoutMilliseconds);

        public virtual void ExpectErrorWithMessage<E>(string requiredMessagePart, long timeoutMilliseconds)
            where E : Exception
        {
            var error = ExpectError<E>(timeoutMilliseconds);
            var message = error.Message;
            if (!message.Contains(requiredMessagePart))
                throw new AssertionException($"Got expected exception {error.GetType().Name} but missing message part {requiredMessagePart}, was {error.Message}");
        }

        public E ExpectError<E>() where E : Exception => ExpectError<E>(Environment.DefaultTimeoutMilliseconds);

        public E ExpectError<E>(long timeoutMilliseconds) where E : Exception
            => ExpectError<E>(timeoutMilliseconds, $"Expected OnError({typeof(E).Name})");

        public E ExpectError<E>(string errorMessage) where E : Exception
            => ExpectError<E>(Environment.DefaultTimeoutMilliseconds, errorMessage);

        public virtual E ExpectError<E>(long timeoutMilliseconds, string errorMessage) where E : Exception
            => _received.ExpectError<E>(timeoutMilliseconds, errorMessage);

        public void ExpectNone() => ExpectNone(Environment.DefaultNoSignalsTimeoutMilliseconds);

        public void ExpectNone(long withinMilliseconds)
            => ExpectNone(withinMilliseconds, "Did not expect an element but got element");

        public void ExpectNone(string errorMessagePrefix)
            => ExpectNone(Environment.DefaultTimeoutMilliseconds, errorMessagePrefix);

        public virtual void ExpectNone(long withinMilliseconds, string errorMessagePrefix)
            => _received.ExpectNone(withinMilliseconds, errorMessagePrefix);
    }

    public class ManualSubscriberWithSubscriptionSupport<T> : ManualSubscriber<T>
    {
        public ManualSubscriberWithSubscriptionSupport(TestEnvironment environment) : base(environment)
        {
        }

        public override void OnNext(T element)
        {
           Environment.Debug($"{this}.OnNext({element})");
            if (Subscription.IsCompleted())
                base.OnNext(element);
            else
                Environment.Flop($"Subscriber.OnNext({element}) called before Subscriber.OnSubscribe()");
        }

        public override void OnComplete()
        {
            Environment.Debug($"{this}.OnComplete()");
            if(Subscription.IsCompleted())
                base.OnComplete();
            else
                Environment.Flop("Subscriber.OnComplete() called before Subscriber.OnSubscribe()");
        }

        public override void OnSubscribe(ISubscription subscription)
        {
            Environment.Debug($"{this}.OnSubscribe({subscription})");
            if(!Subscription.IsCompleted())
                Subscription.Complete(subscription);
            else
                Environment.Flop("Subscriber.OnSubscribe() called on an already-subscribed Subscriber");
        }

        public override void OnError(Exception cause)
        {
            Environment.Debug($"{this}.OnError({cause})");
            if (Subscription.IsCompleted())
                base.OnError(cause);
            else
                Environment.Flop(cause, $"Subscriber.OnError({cause}) called before Subscriber.OnSubscribe()");
        }
    }

    /// <summary>
    /// Similar to <see cref="ManualSubscriberWithSubscriptionSupport{T}"/>
    /// but does not accumulate values signalled via <code>onNext</code>, thus it can not be used to assert
    /// values signalled to this subscriber. Instead it may be used to quickly drain a given publisher.
    /// </summary>
    public class BlackholeSubscriberWithSubscriptionSupport<T> : ManualSubscriberWithSubscriptionSupport<T>
    {
        public BlackholeSubscriberWithSubscriptionSupport(TestEnvironment environment) : base(environment)
        {
        }

        public override void OnNext(T element)
        {
            Environment.Debug($"{this}.OnNext({element})");
            if (!Subscription.IsCompleted())
                Environment.Flop($"Subscriber.OnNext({element}) called before Subscriber.OnSubscribe()");
        }

        public override T NextElement(long timeoutMilliseconds, string errorMessage)
        {
            throw new Exception("Can not expect elements from BlackholeSubscriber, use ManualSubscriber instead!");
        }

        public override List<T> NextElements(long elements, long timeoutMilliseconds, string errorMessage)
        {
            throw new Exception("Can not expect elements from BlackholeSubscriber, use ManualSubscriber instead!");
        }
    }

    public class TestSubscriber<T> : ISubscriber<T>
    {
        public TestSubscriber(TestEnvironment environment)
        {
            Environment = environment;
            Subscription = new Promise<ISubscription>(environment);
        }

        protected TestEnvironment Environment { get; }

        public Promise<ISubscription> Subscription { get; } 

        public virtual void OnNext(T element) => Environment.Flop($"Unexpected Subscriber.OnNext({element})");

        public virtual void OnSubscribe(ISubscription subscription)
            => Environment.Flop($"Unexpected Subsciber.OnSubscribe({subscription})");

        public virtual void OnError(Exception cause) => Environment.Flop(cause, $"Unexpected Subscriber.OnError({cause})");

        public virtual void OnComplete() => Environment.Flop("Unexpected Subsciber.OnComplete()");

        public virtual void Cancel()
        {
            if(Subscription.IsCompleted())
                Subscription.Value.Cancel();
            else
                Environment.Flop("Cannot cancel a subscription before having received it");
        }
    }

    public class ManualPublisher<T> : IPublisher<T>
    {
        private sealed class Subscription : ISubscription
        {
            private readonly ManualPublisher<T> _publisher;

            public Subscription(ManualPublisher<T> publisher)
            {
                _publisher = publisher;
            }

            public void Request(long n) => _publisher.Requests.Add(n);

            public void Cancel() => _publisher.Cancelled.Close();
        }

        public ManualPublisher(TestEnvironment environment)
        {
            Environment = environment;
            Requests = new Receptacle<long>(environment);
            Cancelled = new Latch(environment);
            Subscriber = new Promise<ISubscriber<T>>(environment);
        }

        protected TestEnvironment Environment { get; }

        protected long PendingDemand { get; set; }

        protected Promise<ISubscriber<T>> Subscriber { get; set; }

        protected Receptacle<long> Requests { get; }

        protected Latch Cancelled { get; }

        public void Subscribe(ISubscriber<T> s)
        {
            if (!Subscriber.IsCompleted())
            {
                Subscriber.CompleteImmediatly(s);

                var subscription = new Subscription(this);
                s.OnSubscribe(subscription);
            }
            else
                Environment.Flop("TestPublisher doesn't support more than one Subscriber");
        }

        public void SendNext(T element)
        {
            if(Subscriber.IsCompleted())
                Subscriber.Value.OnNext(element);
            else
                Environment.Flop("Cannot SendNext before having a Subscriber");
        }

        public void SendCompletion()
        {
            if(Subscriber.IsCompleted())
                Subscriber.Value.OnComplete();
            else
                Environment.Flop("Cannot SendCompletion before having a Subscriber");
        }

        public void SendError(Exception cause)
        {
            if(Subscriber.IsCompleted())
                Subscriber.Value.OnError(cause);
            else
                Environment.Flop("Cannot SendError before having a Subscriber");
        }

        public long ExpectRequest() => ExpectRequest(Environment.DefaultTimeoutMilliseconds);

        public long ExpectRequest(long timeoutMilliseconds)
        {
            var requested = Requests.Next(timeoutMilliseconds, "Did not receive expected `Request` call");

            if (requested <= 0)
                return
                    Environment.FlopAndFail<long>(
                        $"Requestes cannot be zero or negative but received Request({requested}).");

            PendingDemand += requested;
            return requested;
        }

        public void ExpectExactRequest(long expected)
            => ExpectExactRequest(expected, Environment.DefaultTimeoutMilliseconds);

        public void ExpectExactRequest(long expected, long timeoutMilliseconds)
        {
            var requested = ExpectRequest(timeoutMilliseconds);
            if(requested != expected)
                Environment.Flop($"Received `Request({requested})` on upstream but expected `Requested({expected})`");

            PendingDemand += requested;
        }

        public void ExpectNoRequest() => ExpectNoRequest(Environment.DefaultTimeoutMilliseconds);

        public void ExpectNoRequest(long timeoutMilliseconds)
            => Requests.ExpectNone(timeoutMilliseconds, "Received an unexpected call to: Request: ");

        public void ExpectCancelling() => ExpectCancelling(Environment.DefaultTimeoutMilliseconds);

        public void ExpectCancelling(long timeoutMilliseconds) =>
            Cancelled.ExpectClose(timeoutMilliseconds,
                "Did not receive expected cancelling on upstream subscription");
    }

    /// <summary>
    /// Simple promise for *one* value which cannot be reset
    /// </summary>
    public class Promise<T>
    {
        private readonly TestEnvironment _environment;
        private readonly BlockingCollection<T> _blockingCollection = new BlockingCollection<T>();
        private Option<T> _value;

        public Promise(TestEnvironment environment)
        {
            _environment = environment;
        }

        public static Promise<T> Completed(TestEnvironment environment, T value)
        {
            var promise = new Promise<T>(environment);
            promise.CompleteImmediatly(value);
            return promise;
        }

        public T Value
        {
            get
            {
                if (IsCompleted())
                    return _value.Value;
                
                _environment.Flop("Cannot access promise value before completion");
                return default(T);
            }
        }

        public bool IsCompleted() => _value.HasValue;

        /// <summary>
        /// Allows using ExpectCompletion to await for completion of the value and complete it _then_
        /// </summary>
        public void Complete(T value) => _blockingCollection.Add(value);

        /// <summary>
        /// Completes the promise right away, it is not possible to ExpectCompletion on a Promise completed this way
        /// </summary>
        public void CompleteImmediatly(T value)
        {
            Complete(value); // complete!
            _value = value; // immediatly!
        }

        public void ExpectCompletion(long timeoutMilliseconds, string errorMessage)
        {
            if (!IsCompleted())
            {
                T value;
                if (!_blockingCollection.TryTake(out value, TimeSpan.FromMilliseconds(timeoutMilliseconds)))
                    _environment.Flop($"{errorMessage} within {timeoutMilliseconds} ms");
                else
                    _value = value;
            }
        }
    }

    /// <summary>
    ///  A "Promise" for multiple values, which also supports "end-of-stream reached"
    /// </summary>
    public class Receptacle<T>
    {
        public const int QueueSize = 2*TestEnvironment.TestBufferSize;
        private readonly TestEnvironment _environment;
        private readonly BlockingCollection<Option<T>> _blockingCollection = new BlockingCollection<Option<T>>();
        private readonly Latch _completedLatch;
        
        public Receptacle(TestEnvironment environment)
        {
            _environment = environment;
            _completedLatch = new Latch(environment);
        }

        public void Add(T value)
        {
            _completedLatch.AssertOpen($"Unexpected element {value} received after stream completed");
            if (_blockingCollection.Count >= QueueSize)
                throw new InvalidOperationException("Queue size exceeded!");
            _blockingCollection.Add(value);
        }

        public void Complete()
        {
            _completedLatch.AssertOpen("Unexpected additional complete signal received!");
            _completedLatch.Close();

            _blockingCollection.Add(Option<T>.None);
        }

        public T Next(long timeoutMilliseconds, string errorMessage)
        {
            Option<T> value;

            if (!_blockingCollection.TryTake(out value, TimeSpan.FromMilliseconds(timeoutMilliseconds)))
                return _environment.FlopAndFail<T>($"{errorMessage} within {timeoutMilliseconds} ms");

            if (value.HasValue)
                return value.Value;

            return _environment.FlopAndFail<T>("Expected element but got end-of-stream");
        }

        public Option<T> NextOrEndOfStream(long timeoutMilliseconds, string errorMessage)
        {
            Option<T> value;

            if (!_blockingCollection.TryTake(out value, TimeSpan.FromMilliseconds(timeoutMilliseconds)))
            {
                _environment.Flop($"{errorMessage} within {timeoutMilliseconds} ms");
                return Option<T>.None;
            }

            return value;
        }

        public List<T> NextN(long elements, long timeoutMilliseconds, string errorMessage)
        {
            var result = new List<T>();
            var remaining = elements;
            var deadline = DateTime.Now.AddMilliseconds(timeoutMilliseconds);
            while (remaining > 0)
            {
                var remainingMilliseconds = (long)(deadline - DateTime.Now).TotalMilliseconds;

                result.Add(Next(remainingMilliseconds, errorMessage));
                remaining--;
            }
            return result;
        }

        public void ExpectCompletion(long timeoutMilliseconds, string errorMessage)
        {
            Option<T> value;

            if (!_blockingCollection.TryTake(out value, TimeSpan.FromMilliseconds(timeoutMilliseconds)))
                _environment.Flop($"{errorMessage} within {timeoutMilliseconds} ms");
            else if (value.HasValue)
                _environment.Flop($"Expected end-of-stream but got element [{value.Value}]");
            //else, ok
        }

        public E ExpectError<E>(long timeoutMilliseconds, string errorMessage) where E : Exception
        {
            Thread.Sleep(TimeSpan.FromMilliseconds(timeoutMilliseconds));

            if (_environment.AsyncErrors.IsEmpty)
                return _environment.FlopAndFail<E>($"{errorMessage} within {timeoutMilliseconds} ms");

            //ok, there was an expected error
            Exception thrown;
            if(!_environment.AsyncErrors.TryDequeue(out thrown))
                throw new Exception("Couldn't dequeue error from the environment");

            var error = thrown as E;
            if (error != null)
                return error;

            return
                _environment.FlopAndFail<E>(
                    $"{errorMessage} within {timeoutMilliseconds} ms; Got {thrown.GetType().Name} but expected {typeof(E).Name}");
        }

        public void ExpectNone(long withinMilliseconds, string errorMessagePrefix)
        {
            Thread.Sleep(TimeSpan.FromMilliseconds(withinMilliseconds));

            Option<T> value;

            if (!_blockingCollection.TryTake(out value))
            {
                //ok
            }
            else if (value.HasValue)
                _environment.Flop($"{errorMessagePrefix} [{value.Value}]");
            else
                _environment.Flop("Expected no element but got end-of-stream");
        }
    }

    /// <summary>
    ///  Like a CountdownEvent, but resettable and with some convenience methods
    /// </summary>
    public class Latch
    {
        private readonly TestEnvironment _environment;
        private CountdownEvent _countdownEvent = new CountdownEvent(1);

        public Latch(TestEnvironment environment)
        {
            _environment = environment;
        }

        public void ReOpen() => _countdownEvent = new CountdownEvent(1);

        public bool IsClosed() => _countdownEvent.IsSet;

        public void Close()
        {
            if(!_countdownEvent.IsSet)
                _countdownEvent.Signal();
        }

        public void AssertClosed(string openErrorMessage)
        {
            if(!IsClosed())
                _environment.Flop(new ExpectedClosedLatchException(openErrorMessage));
        }

        public void AssertOpen(string closedErrorMessage)
        {
            if (IsClosed())
                _environment.Flop(new ExpectedOpenLatchException(closedErrorMessage));
        }

        public void ExpectClose(string notClosedErrorMessage)
            => ExpectClose(_environment.DefaultTimeoutMilliseconds, notClosedErrorMessage);

        public void ExpectClose(long timeoutMilliseconds, string notClosedErrorMessage)
        {
            _countdownEvent.Wait(TimeSpan.FromMilliseconds(timeoutMilliseconds));

            if(_countdownEvent.CurrentCount > 0)
                _environment.Flop($"{notClosedErrorMessage} within {timeoutMilliseconds} ms");
        }

        public class ExpectedOpenLatchException : Exception
        {
            public ExpectedOpenLatchException(string message) : base(message)
            {

            }
        }

        public class ExpectedClosedLatchException : Exception
        {
            public ExpectedClosedLatchException(string message) : base(message)
            {
                
            }
        }
    }
}
