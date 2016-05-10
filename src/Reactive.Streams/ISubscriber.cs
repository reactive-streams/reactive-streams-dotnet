using System;

namespace Reactive.Streams
{
    /// <summary>
    /// <para>
    /// Will receive call to <see cref="OnSubscribe"/> once after passing an instance of
    /// <see cref="ISubscriber"/> to <see cref="IPublisher.Subscribe"/>.
    /// </para>
    /// <para>
    /// No further notifications will be received until <see cref="ISubscription.Request"/> is called.
    /// </para>
    /// <para>After signaling demand:</para>
    /// <para>1. One or more invocations of <see cref="OnNext"/> up to the maximum number defined by
    /// <see cref="ISubscription.Request"/></para>
    /// <para>2. Single invocation of <see cref="OnError"/> or <see cref="OnComplete"/> which signals
    /// a terminal state after which no further events will be sent.</para>
    /// <para>
    /// Demand can be signaled via <see cref="ISubscription.Request"/> whenever the
    /// <see cref="ISubscriber"/> instance is capable of handling more.</para>
    /// </summary>
    public interface ISubscriber
    {
        /// <summary>
        /// <para>
        /// Invoked after calling <see cref="IPublisher.Subscribe"/>.
        /// </para>
        /// <para>
        /// No data will start flowing until <see cref="ISubscription.Request"/> is invoked.
        /// </para>
        /// <para>
        /// It is the responsibility of this <see cref="ISubscriber"/> instance to call
        /// <see cref="ISubscription.Request"/> whenever more data is wanted.
        /// </para>
        /// <para>
        /// The <see cref="IPublisher"/> will send notifications only in response to
        /// <see cref="ISubscription.Request"/>.
        /// </para>
        /// </summary>
        /// <param name="subscription"><see cref="ISubscription"/> that allows requesting data
        /// via <see cref="ISubscription.Request"/></param>
        void OnSubscribe(ISubscription subscription);

        /// <summary>
        /// Data notification sent by the <see cref="IPublisher"/> in response to requests to
        /// <see cref="ISubscription.Request"/>.
        /// </summary>
        /// <param name="element">The element signaled</param>
        void OnNext(object element);

        /// <summary>
        /// <para>
        /// Failed terminal state.
        /// </para>
        /// <para>
        /// No further events will be sent even if <see cref="ISubscription.Request"/> is
        /// invoked again.
        /// </para>
        /// </summary>
        /// <param name="cause">The exception signaled</param>
        void OnError(Exception cause);

        /// <summary>
        /// <para>
        /// Successful terminal state.
        /// </para>
        /// <para>
        /// No further events will be sent even if <see cref="ISubscription.Request"/> is
        /// invoked again.
        /// </para>
        /// </summary>
        void OnComplete();
    }

    /// <summary>
    /// <para>
    /// Will receive call to <see cref="ISubscriber.OnSubscribe"/> once after passing an instance of
    /// <see cref="ISubscriber{T}"/> to <see cref="IPublisher.Subscribe"/>.
    /// </para>
    /// <para>
    /// No further notifications will be received until <see cref="ISubscription.Request"/> is called.
    /// </para>
    /// <para>After signaling demand:</para>
    /// <para>1. One or more invocations of <see cref="OnNext"/> up to the maximum number defined by
    /// <see cref="ISubscription.Request"/></para>
    /// <para>2. Single invocation of <see cref="ISubscriber.OnError"/> or
    /// <see cref="ISubscriber.OnComplete"/> which signals a terminal state after which no further
    /// events will be sent.</para>
    /// <para>
    /// Demand can be signaled via <see cref="ISubscription.Request"/> whenever the
    /// <see cref="ISubscriber{T}"/> instance is capable of handling more.</para>
    /// </summary>
    /// <typeparam name="T">The type of element signaled.</typeparam>
    public interface ISubscriber<in T> : ISubscriber
    {
        /// <summary>
        /// Data notification sent by the <see cref="IPublisher"/> in response to requests to
        /// <see cref="ISubscription.Request"/>.
        /// </summary>
        /// <param name="element">The element signaled</param>
        void OnNext(T element);
    }
}