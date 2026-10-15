using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids
{
    public readonly struct ShopOfferId : IEquatable<ShopOfferId>
    {
        public int Value { get; }
        public static readonly ShopOfferId Empty = default;
        public bool IsEmpty => Value == 0;

        public ShopOfferId(int value)
        {
            if (value <= 0)
                throw new ArgumentOutOfRangeException(nameof(value), "ShopOfferId must be positive.");

            Value = value;
        }

        public bool Equals(ShopOfferId other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is ShopOfferId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value;
        }

        public static bool operator ==(ShopOfferId left, ShopOfferId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ShopOfferId left, ShopOfferId right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
