using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids
{
    /// <summary>
    /// 이번 맵에 생성된 몇 번째 Node인지 확인용
    /// </summary>
    public readonly struct StageNodeId : IEquatable<StageNodeId>
    {
        public int Value { get; }
        public static readonly StageNodeId Empty = default;
        public bool IsEmpty => Value == 0;

        public StageNodeId(int value)
        {
            if (value <= 0)
                throw new ArgumentOutOfRangeException(nameof(value), "StageKey는 1 이상의 값이어야 합니다.");

            Value = value;
        }

        public bool Equals(StageNodeId other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is StageNodeId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value;
        }

        public static bool operator ==(StageNodeId left, StageNodeId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(StageNodeId left, StageNodeId right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
