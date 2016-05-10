namespace Reactive.Streams
{
    /// <summary>
    /// <para>
    /// A <see cref="IPublisher"/> is a provider of a potentially unbounded number of sequenced elements,
    /// publishing them according to the demand received from its <see cref="ISubscriber"/>.
    /// </para>
    /// <para>
    /// A <see cref="IPublisher"/> can serve multiple <see cref="ISubscriber"/>s subscribed dynamically
    /// at various points in time.
    /// </para>
    /// </summary>
    public interface IPublisher
    {
        /// <summary>
        /// <para>
        /// Request <see cref="IPublisher"/> to start streaming data.
        /// </para>
        /// <para>
        /// This is a "factory method" and can be called multiple times, each time starting a new
        /// <see cref="ISubscription"/>.
        /// </para>
        /// <para>
        /// Each <see cref="ISubscription"/> will work for only a single <see cref="ISubscriber"/>.
        /// </para>
        /// <para>
        /// A <see cref="ISubscriber"/> should only subscribe once to a single
        /// <see cref="IPublisher"/>.
        /// </para>
        /// <para>
        /// If the <see cref="IPublisher"/> rejects the subscription attempt or otherwise fails
        /// it will signal the error via <see cref="ISubscriber.OnError"/>.
        /// </para>
        /// </summary>
        /// <param name="subscriber">The <see cref="ISubscriber"/> that will consume signals
        /// from this <see cref="IPublisher"/></param>
        void Subscribe(ISubscriber subscriber);
    }

    /// <summary>
    /// <para>
    /// A <see cref="IPublisher{T}"/> is a provider of a potentially unbounded number of sequenced elements,
    /// publishing them according to the demand received from its <see cref="ISubscriber{T}"/>.
    /// </para>
    /// <para>
    /// A <see cref="IPublisher{T}"/> can serve multiple <see cref="ISubscriber{T}"/>s subscribed dynamically
    /// at various points in time.
    /// </para>
    /// </summary>
    /// <typeparam name="T">The type of element signaled.</typeparam>
    public interface IPublisher<out T> : IPublisher
    {
        /// <summary>
        /// <para>
        /// Request <see cref="IPublisher{T}"/> to start streaming data.
        /// </para>
        /// <para>
        /// This is a "factory method" and can be called multiple times, each time starting a new
        /// <see cref="ISubscription"/>.
        /// </para>
        /// <para>
        /// Each <see cref="ISubscription"/> will work for only a single <see cref="ISubscriber{T}"/>.
        /// </para>
        /// <para>
        /// A <see cref="ISubscriber{T}"/> should only subscribe once to a single
        /// <see cref="IPublisher{T}"/>.
        /// </para>
        /// <para>
        /// If the <see cref="IPublisher{T}"/> rejects the subscription attempt or otherwise fails
        /// it will signal the error via <see cref="ISubscriber.OnError"/>.
        /// </para>
        /// </summary>
        /// <param name="subscriber">The <see cref="ISubscriber{T}"/> that will consume signals
        /// from this <see cref="IPublisher{T}"/></param>
        void Subscribe(ISubscriber<T> subscriber);
    }
}