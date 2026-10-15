using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids
{
    public readonly struct StageEntryId : IEquatable<StageEntryId>
    {
        public static readonly StageEntryId Empty = default;

        public Guid Value { get; }
        public bool IsEmpty => Value == Guid.Empty;

        public StageEntryId(Guid value)
        {
            if (value == Guid.Empty)
                throw new ArgumentException("StageEntryId는 Guid.Empty가 될 수 없습니다.", nameof(value));

            Value = value;
        }

        public bool Equals(StageEntryId other)
        {
            return Value.Equals(other.Value);
        }

        public override bool Equals(object obj)
        {
            return obj is StageEntryId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public static bool operator ==(StageEntryId left, StageEntryId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(StageEntryId left, StageEntryId right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
