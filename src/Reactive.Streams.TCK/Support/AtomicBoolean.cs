using System.Threading;

namespace Reactive.Streams.TCK.Support
{
    /// <summary>
    /// Implementation of the java.concurrent.util.AtomicBoolean type.
    /// 
    /// Uses <see cref="Interlocked.MemoryBarrier"/> internally to enforce ordering of writes
    /// without any explicit locking. .NET's strong memory on write guarantees might already enforce
    /// this ordering, but the addition of the MemoryBarrier guarantees it.
    /// </summary>
    public class AtomicBoolean
    {
        private const int FalseValue = 0;
        private const int TrueValue = 1;

        private int _value;
        /// <summary>
        /// Sets the initial value of this <see cref="AtomicBoolean"/> to <paramref name="initialValue"/>.
        /// </summary>
        public AtomicBoolean(bool initialValue = false)
        {
            _value = initialValue ? TrueValue : FalseValue;
        }

        /// <summary>
        /// The current value of this <see cref="AtomicReference{T}"/>
        /// </summary>
        public bool Value
        {
            get
            {
                Interlocked.MemoryBarrier();
                return _value == TrueValue;
            }
            set
            {
                Interlocked.Exchange(ref _value, value ? TrueValue : FalseValue);
            }
        }

        /// <summary>
        /// If <see cref="Value"/> equals <paramref name="expected"/>, then set the Value to
        /// <paramref name="newValue"/>.
        /// </summary>
        /// <returns><c>true</c> if <paramref name="newValue"/> was set</returns>
        public bool CompareAndSet(bool expected, bool newValue)
        {
            var expectedInt = expected ? TrueValue : FalseValue;
            var newInt = newValue ? TrueValue : FalseValue;
            return Interlocked.CompareExchange(ref _value, newInt, expectedInt) == expectedInt;
        }

        #region Conversion operators

        /// <summary>
        /// Implicit conversion operator = automatically casts the <see cref="AtomicBoolean"/> to a <see cref="bool"/>
        /// </summary>
        public static implicit operator bool(AtomicBoolean boolean) => boolean.Value;

        /// <summary>
        /// Implicit conversion operator = allows us to cast any bool directly into a <see cref="AtomicBoolean"/> instance.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static implicit operator AtomicBoolean(bool value) => new AtomicBoolean(value);

        #endregion
    }
}
