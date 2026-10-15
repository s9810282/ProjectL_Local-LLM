using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids
{
    public readonly struct StageGimmickId : IEquatable<StageGimmickId>
    {
        public int Value { get; }
        public static readonly StageGimmickId Empty = default;
        public bool IsEmpty => Value == 0;

        public StageGimmickId(int value)
        {
            if (value <= 0)
                throw new ArgumentOutOfRangeException(nameof(value), "StageGimmickId는 1 이상의 값이어야 합니다.");

            Value = value;
        }

        public bool Equals(StageGimmickId other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is StageGimmickId other && Equals(other);
        }
        public override int GetHashCode()
        {
            return Value;
        }
        public override string ToString()
        {
            return Value.ToString();
        }

        public static bool operator ==(StageGimmickId left, StageGimmickId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(StageGimmickId left, StageGimmickId right)
        {
            return !left.Equals(right);
        }
    }
}