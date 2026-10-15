using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids
{
    public readonly struct EntityId : IEquatable<EntityId>, IComparable<EntityId>
    {
        public int Value { get; }
        public static readonly EntityId Empty = default;
        public bool IsEmpty => Value == 0;
        /// <summary>
        /// 엔티티의 아이디 생성자
        /// </summary>
        /// <param name="value">1 이상의 값만 입력, 인덱스</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public EntityId(int value)
        {
            if (value <= 0)
                throw new ArgumentOutOfRangeException(nameof(value), "EntityId는 1 이상의 값이어야 합니다.");

            Value = value;
        }

        public bool Equals(EntityId other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is EntityId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value;
        }

        public override string ToString()
        {
            return Value.ToString();
        }

        public int CompareTo(EntityId other)
        {
            return Value.CompareTo(other.Value);
        }

        public static bool operator ==(EntityId left, EntityId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(EntityId left, EntityId right)
        {
            return !left.Equals(right);
        }
    }
}