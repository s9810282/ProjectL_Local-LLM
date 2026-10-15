using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids
{
    public readonly struct RecoveryOptionId : IEquatable<RecoveryOptionId>
    {
        public int Value { get; }
        public static readonly RecoveryOptionId Empty = default;
        public bool IsEmpty => Value == 0;

        public RecoveryOptionId(int value)
        {
            if (value <= 0)
                throw new ArgumentOutOfRangeException(nameof(value), "RecoveryOptionId must be positive.");

            Value = value;
        }

        public bool Equals(RecoveryOptionId other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is RecoveryOptionId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value;
        }

        public static bool operator ==(RecoveryOptionId left, RecoveryOptionId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(RecoveryOptionId left, RecoveryOptionId right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
