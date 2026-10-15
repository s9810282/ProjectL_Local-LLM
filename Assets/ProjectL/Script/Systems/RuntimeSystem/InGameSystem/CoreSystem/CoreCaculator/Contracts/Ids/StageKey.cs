using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids
{
    /// <summary>
    /// 어떤 실제 Stage데이터인지 확인용
    /// </summary>
    public readonly struct StageKey : IEquatable<StageKey>
    {
        public int Value { get; }
        public static readonly StageKey Empty = default;
        public bool IsEmpty => Value == 0;
        public StageKey(int value) 
        {
            if (value <= 0)
                throw new ArgumentOutOfRangeException(nameof(value), "StageKey는 1 이상의 값이어야 합니다.");

            Value = value; 
        }

        public bool Equals(StageKey other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is StageKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value;
        }

        public static bool operator ==(StageKey left, StageKey right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(StageKey left, StageKey right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
