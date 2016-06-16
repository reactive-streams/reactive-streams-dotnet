using Reactive.Streams.TCK.Support;

namespace Reactive.Streams.TCK
{
    /// <summary>
    /// Type which is able to create elements based on a seed id value.
    /// <para/>
    /// Simplest implementations will simply return the incoming id as the element.
    /// </summary>
    /// <typeparam name="T">type of element to be delivered to the Subscriber</typeparam>
    public abstract class WithHelperPublisher<T>
    {
        /// <summary>
        /// Implement this method to match your expected element type.
        /// In case of implementing a simple Subscriber which is able to consume any kind of element simply return the
        /// incoming <paramref name="element"/>.
        /// <para>
        /// Sometimes the Subscriber may be limited in what type of element it is able to consume, this you may have to implement
        /// this method such that the emitted element matches the Subscribers requirements. Simplest implementations would be
        /// to simply pass in the <paramref name="element"/> as payload of your custom element, such as appending it to a String or other identifier.
        /// </para>
        /// Warning: This method may be called concurrently by the helper publisher, thus it should be implemented in a
        /// thread-safe manner.
        /// </summary>
        /// <returns>element of the matching type <see cref="T"/> that will be delivered to the tested Subscriber</returns>
        public abstract T CreateElement(int element);

        /// <summary>
        /// Helper method required for creating the Publisher to which the tested Subscriber will be subscribed and tested against.
        /// <para>
        /// By default an asynchronously signalling Publisher is provided, which will use <see cref="CreateElement"/>
        /// to generate elements type your Subscriber is able to consume.
        /// </para>
        /// Sometimes you may want to implement your own custom custom helper Publisher - to validate behaviour of a Subscriber
        /// when facing a synchronous Publisher for example.If you do, it MUST emit the exact number of elements asked for
        /// (via the <paramref name="elements"/> parameter) and MUST also must treat the following numbers of elements in these specific ways:
        /// 
        /// <para>
        /// If <paramref name="elements"/> is <see cref="long.MaxValue"/> the produced stream must be infinite.
        /// </para>
        ///  
        /// <para>
        /// If <paramref name="elements"/> is 0 the <see cref="IPublisher{T}"/> should signal <see cref="ISubscriber.OnComplete"/> immediatly.
        /// In other words, it should represent a "completed stream".
        /// </para>
        /// </summary>
        /// <param name="elements"></param>
        /// <returns></returns>
        public virtual IPublisher<T> CreateHelperPublisher(long elements)
            => elements > int.MaxValue
                ? (IPublisher<T>) new InfiniteHelperPublisher<T>(CreateElement)
                : new HelperPublisher<T>(0, (int) elements, CreateElement);
    }
}
