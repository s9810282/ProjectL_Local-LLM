using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids
{
    /// <summary>전투와 챕터가 바뀌어도 유지되는 보유 카드 한 장의 식별자다.</summary>
    public readonly struct CharacterInstanceId : IEquatable<CharacterInstanceId>, IComparable<CharacterInstanceId>
    {
        public int Value { get; }
        public static readonly CharacterInstanceId Empty = default;
        public bool IsEmpty => Value == 0;

        public CharacterInstanceId(int value)
        {
            if (value <= 0)
                throw new ArgumentOutOfRangeException(nameof(value));
            Value = value;
        }

        public bool Equals(CharacterInstanceId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is CharacterInstanceId other && Equals(other);
        public override int GetHashCode() => Value;
        public int CompareTo(CharacterInstanceId other) => Value.CompareTo(other.Value);
        public override string ToString() => Value.ToString();
        public static bool operator ==(CharacterInstanceId left, CharacterInstanceId right) => left.Equals(right);
        public static bool operator !=(CharacterInstanceId left, CharacterInstanceId right) => !left.Equals(right);
    }
}
