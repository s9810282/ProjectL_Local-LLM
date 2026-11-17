using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>게시하는 활성 폭탄과 다음 식별자다. 폭발 예약은 포함하지 않는다.</summary>
    public sealed class ThornBombStateSet
    {
        public static ThornBombStateSet Empty { get; } = new ThornBombStateSet(1, Array.Empty<ThornBombSnapshot>());

        public long NextBombId { get; }
        public IReadOnlyList<ThornBombSnapshot> Bombs { get; }

        public ThornBombStateSet(long nextBombId, IReadOnlyList<ThornBombSnapshot> bombs)
        {
            if (nextBombId < 1) throw new ArgumentOutOfRangeException(nameof(nextBombId));
            if (bombs == null) throw new ArgumentNullException(nameof(bombs));

            var copy = bombs.ToArray();
            var ids = new HashSet<long>();
            var targets = new HashSet<EntityId>();
            foreach (var bomb in copy)
            {
                if (bomb == null || bomb.BombId >= nextBombId || !ids.Add(bomb.BombId) || !targets.Add(bomb.TargetId))
                    throw new ArgumentException("활성 폭탄과 대상은 중복될 수 없으며 발급한 폭탄만 저장해야 합니다.", nameof(bombs));
            }

            Array.Sort(copy, (left, right) => left.BombId.CompareTo(right.BombId));
            NextBombId = nextBombId;
            Bombs = Array.AsReadOnly(copy);
        }

        public bool SameValues(ThornBombStateSet other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (other == null || NextBombId != other.NextBombId || Bombs.Count != other.Bombs.Count) return false;
            for (int i = 0; i < Bombs.Count; i++)
                if (!Bombs[i].SameValues(other.Bombs[i])) return false;
            return true;
        }
    }
}
