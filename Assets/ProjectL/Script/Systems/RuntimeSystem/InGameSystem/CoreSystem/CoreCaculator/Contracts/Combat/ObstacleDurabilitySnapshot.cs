using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>장애물 파괴까지 필요한 타수와 현재 남은 타수다.</summary>
    public sealed class ObstacleDurabilitySnapshot : IEquatable<ObstacleDurabilitySnapshot>
    {
        public int RemainingHits { get; }
        public int RequiredHits { get; }
        public bool IsDestroyed => RemainingHits == 0;

        public ObstacleDurabilitySnapshot(int remainingHits, int requiredHits)
        {
            if (requiredHits < 1) throw new ArgumentOutOfRangeException(nameof(requiredHits));
            if (remainingHits < 0 || remainingHits > requiredHits)
                throw new ArgumentOutOfRangeException(nameof(remainingHits));
            RemainingHits = remainingHits;
            RequiredHits = requiredHits;
        }

        public bool Equals(ObstacleDurabilitySnapshot other) => other != null &&
            RemainingHits == other.RemainingHits && RequiredHits == other.RequiredHits;
        public override bool Equals(object obj) => Equals(obj as ObstacleDurabilitySnapshot);
        public override int GetHashCode() => HashCode.Combine(RemainingHits, RequiredHits);
    }
}
