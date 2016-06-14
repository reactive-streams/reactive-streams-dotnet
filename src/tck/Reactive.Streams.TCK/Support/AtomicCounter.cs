using System.Threading;

namespace Reactive.Streams.TCK.Support
{
    /// <summary>
    /// An atomic 32 bit integer counter.
    /// </summary>
    public class AtomicCounter
    {
        /// <summary>
        /// Creates an instance of an AtomicCounter.
        /// </summary>
        /// <param name="initialValue">The initial value of this counter.</param>
        public AtomicCounter(int initialValue)
        {
            _value = initialValue;
        }

        /// <summary>
        /// Creates an instance of an AtomicCounter with a starting value of -1.
        /// </summary>
        public AtomicCounter()
        {
            _value = -1;
        }

        /// <summary>
        /// The current value of the atomic counter.
        /// </summary>
        private int _value;

        /// <summary>
        /// Retrieves the current value of the counter
        /// </summary>
        public int Current => _value;

        /// <summary>
        /// Increments the counter and returns the next value
        /// </summary>
        public int Next() => Interlocked.Increment(ref _value);

        /// <summary>
        /// Decrements the counter and returns the next value
        /// </summary>
        public int Decrement() => Interlocked.Decrement(ref _value);

        /// <summary>
        /// Atomically increments the counter by one.
        /// </summary>
        /// <returns>The original value.</returns>
        public int GetAndIncrement()
        {
            var nextValue = Next();
            return nextValue - 1;
        }

        /// <summary>
        /// Atomically increments the counter by one.
        /// </summary>
        /// <returns>The new value.</returns>
        public int IncrementAndGet()
        {
            var nextValue = Next();
            return nextValue;
        }

        /// <summary>
        /// Atomically decrements the counter by one.
        /// </summary>
        /// <returns>The original value.</returns>
        public int GetAndDecrement()
        {
            var previousValue = Decrement();
            return previousValue + 1;
        }

        /// <summary>
        /// Atomically decrements the counter by one.
        /// </summary>
        /// <returns>The new value.</returns>
        public int DecrementAndGet() => Decrement();

        /// <summary>
        /// Returns the current value and adds the specified value to the counter.
        /// </summary>
        /// <param name="amount">The amount to add to the counter.</param>
        /// <returns>The original value before additions.</returns>
        public int GetAndAdd(int amount)
        {
            var newValue = Interlocked.Add(ref _value, amount);
            return newValue - amount;
        }


        /// <summary>
        /// Adds the specified value to the counter and returns the new value.
        /// </summary>
        /// <param name="amount">The amount to add to the counter.</param>
        /// <returns>The new value after additions.</returns>
        public int AddAndGet(int amount)
        {
            var newValue = Interlocked.Add(ref _value, amount);
            return newValue;
        }

        /// <summary>
        /// Resets the counter to zero.
        /// </summary>
        public void Reset() => Interlocked.Exchange(ref _value, 0);

        /// <summary>
        /// Returns current counter value and sets a new value on it's place in one operation.
        /// </summary>
        public int GetAndSet(int value) => Interlocked.Exchange(ref _value, value);

        /// <summary>
        /// Compares current counter value with provided <paramref name="expected"/> value,
        /// and sets it to <paramref name="newValue"/> if compared values where equal.
        /// Returns true if replacement has succeed.
        /// </summary>
        public bool CompareAndSet(int expected, int newValue)
            => Interlocked.CompareExchange(ref _value, newValue, expected) != _value;
    }
}
