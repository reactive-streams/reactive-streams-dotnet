﻿/***************************************************
 * Licensed under MIT No Attribution (SPDX: MIT-0) *
 ***************************************************/
using System.Collections.Generic;

namespace Reactive.Streams.TCK.Support
{
    /// <summary>
    /// Allows tracking of whether a value has be initialized (even with the default value) for both
    /// reference and value types.
    /// Useful where distinguishing between null (or zero, or false) and unitialized is significant.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public struct Option<T>
    {
        public static readonly Option<T> None = new Option<T>();

        public Option(T value)
        {
            Value = value;
            HasValue = true;
        }

        public bool HasValue { get; }

        public T Value { get; }

        public static implicit operator Option<T>(T value) => new Option<T>(value);

        public bool Equals(Option<T> other)
            => HasValue == other.HasValue && EqualityComparer<T>.Default.Equals(Value, other.Value);

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            return obj is Option<T> && Equals((Option<T>)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (EqualityComparer<T>.Default.GetHashCode(Value) * 397) ^ HasValue.GetHashCode();
            }
        }

        public override string ToString() => HasValue ? $"Some<{Value}>" : "None";
    }
}