using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids
{
    public readonly struct RunId : IEquatable<RunId>
    {
        public static readonly RunId Empty = default;
        public Guid Value { get; }
        public bool IsEmpty => Value == Guid.Empty;

        public RunId(Guid value)
        {
            if (value == Guid.Empty)
                throw new ArgumentException("RunId는 비어 있을 수 없습니다.", nameof(value));
            Value = value;
        }

        public bool Equals(RunId other) => Value.Equals(other.Value);
        public override bool Equals(object obj) => obj is RunId other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public static bool operator ==(RunId left, RunId right) => left.Equals(right);
        public static bool operator !=(RunId left, RunId right) => !left.Equals(right);
        public override string ToString() => Value.ToString();
    }
}
