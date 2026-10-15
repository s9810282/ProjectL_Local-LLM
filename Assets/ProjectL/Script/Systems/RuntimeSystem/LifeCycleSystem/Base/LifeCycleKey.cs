using System;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.Base
{
    /// <summary>Framework가 기본 제공하는 큰 게임 진행 구간 키입니다.</summary>
    public static class LifeCycleKeys
    {
        public static readonly LifeCycleKey Lobby = new(nameof(Lobby));
        public static readonly LifeCycleKey Stage = new(nameof(Stage));
        public static readonly LifeCycleKey Artifact = new(nameof(Artifact));
        public static readonly LifeCycleKey Combat = new(nameof(Combat));
    }

    /// <summary>게임 진행 LifeCycle을 식별하는 명시적 키입니다.</summary>
    public readonly struct LifeCycleKey : IEquatable<LifeCycleKey>
    {
        public string Name { get; }

        public LifeCycleKey(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("LifeCycle key is required.", nameof(name));

            Name = name;
        }

        public bool Equals(LifeCycleKey other)
        {
            return string.Equals(Name, other.Name, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is LifeCycleKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Name ?? string.Empty);
        }

        public override string ToString()
        {
            return Name ?? string.Empty;
        }

        public static bool operator ==(LifeCycleKey left, LifeCycleKey right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(LifeCycleKey left, LifeCycleKey right)
        {
            return !left.Equals(right);
        }
    }
}
