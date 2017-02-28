namespace Reactive.Streams
{
    /// <summary>
    /// <para>
    /// A <see cref="ISubscription"/> represents a one-to-one lifecycle of a <see cref="ISubscriber{T}"/>
    /// subscribing to a <see cref="ISubscriber{T}"/>.
    /// </para>
    /// <para>
    /// It can only be used once by a single <see cref="IPublisher{T}"/>.
    /// </para>
    /// <para>
    /// It is used to both signal desire for data and cancel demand (and allow resource cleanup).
    /// </para>
    /// </summary>
    public interface ISubscription
    {
        /// <summary>
        /// <para>
        /// No events will be sent by a <see cref="IPublisher{T}"/> until demand is signaled via this method.
        /// </para>
        /// <para>
        /// It can be called however often and whenever needed—but the outstanding cumulative demand
        /// must never exceed <see cref="long.MaxValue"/>.
        /// An outstanding cumulative demand of <see cref="long.MaxValue"/> may be treated by the
        /// <see cref="IPublisher{T}"/> as "effectively unbounded".
        /// </para>
        /// <para>
        /// Whatever has been requested can be sent by the <see cref="IPublisher{T}"/> so only signal demand
        /// for what can be safely handled.
        /// </para>
        /// <para>
        /// A <see cref="IPublisher{T}"/> can send less than is requested if the stream ends but
        /// then must emit either <see cref="ISubscriber{T}.OnError"/> or <see cref="ISubscriber{T}.OnComplete"/>.
        /// </para>
        /// </summary>
        /// <param name="n">The strictly positive number of elements to requests to the upstream
        /// <see cref="IPublisher{T}"/></param>
        void Request(long n);

        /// <summary>
        /// <para>
        /// Request the <see cref="IPublisher{T}"/> to stop sending data and clean up resources.
        /// </para>
        /// <para>
        /// Data may still be sent to meet previously signalled demand after calling cancel.
        /// </para>
        /// </summary>
        void Cancel();
    }
}
