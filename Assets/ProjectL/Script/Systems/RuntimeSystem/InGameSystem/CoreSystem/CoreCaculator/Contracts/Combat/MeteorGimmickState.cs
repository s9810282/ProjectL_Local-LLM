using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    public sealed class MeteorFootprint
    {
        public long ImpactBoundaryCount { get; }
        public int Ordinal { get; }
        public IReadOnlyList<CellCoord> Cells { get; }

        public MeteorFootprint(long impactBoundaryCount, int ordinal, IReadOnlyList<CellCoord> cells)
        {
            if (impactBoundaryCount < 1) throw new ArgumentOutOfRangeException(nameof(impactBoundaryCount));
            if (ordinal < 1) throw new ArgumentOutOfRangeException(nameof(ordinal));
            ImpactBoundaryCount = impactBoundaryCount;
            Ordinal = ordinal;
            Cells = StageGimmickState.CopyCells(cells, false);
        }

        public EffectOriginKey FireOrigin(StageGimmickId id) => new EffectOriginKey(EffectOwnerKind.StageGimmick,
            id.Value, (int)StageGimmickKey.Meteor, FormattableString.Invariant($"meteor:{ImpactBoundaryCount}:{Ordinal}"));

        public bool SameValues(MeteorFootprint other) => other != null && ImpactBoundaryCount == other.ImpactBoundaryCount &&
            Ordinal == other.Ordinal && Cells.SequenceEqual(other.Cells);

        internal bool Fits(int side, int width, int height)
        {
            var first = Cells[0];
            return Cells.Count == (long)side * side && (long)first.X + side <= width && (long)first.Y + side <= height &&
                Cells.All(cell => cell.X >= first.X && cell.X < (long)first.X + side && cell.Y < (long)first.Y + side);
        }
    }

    public sealed class MeteorFlameArea
    {
        public MeteorFootprint Footprint { get; }
        public long ExpiresAtBoundaryCount { get; }

        public MeteorFlameArea(MeteorFootprint footprint, long expiresAtBoundaryCount)
        {
            Footprint = footprint ?? throw new ArgumentNullException(nameof(footprint));
            if (expiresAtBoundaryCount <= footprint.ImpactBoundaryCount) throw new ArgumentOutOfRangeException(nameof(expiresAtBoundaryCount));
            ExpiresAtBoundaryCount = expiresAtBoundaryCount;
        }

        public bool SameValues(MeteorFlameArea other) => other != null && ExpiresAtBoundaryCount == other.ExpiresAtBoundaryCount &&
            Footprint.SameValues(other.Footprint);
    }

    public sealed class MeteorGimmickState : StageGimmickState
    {
        public IReadOnlyList<MeteorFootprint> Warnings { get; }
        public IReadOnlyList<MeteorFlameArea> Flames { get; }

        public MeteorGimmickState(StageGimmickId id, long nextBoundaryCount, IReadOnlyList<MeteorFootprint> warnings,
            IReadOnlyList<MeteorFlameArea> flames) : base(id, StageGimmickKey.Meteor, nextBoundaryCount)
        {
            var warned = (warnings ?? throw new ArgumentNullException(nameof(warnings))).ToArray();
            var active = (flames ?? throw new ArgumentNullException(nameof(flames))).ToArray();
            if (warned.Any(w => w == null || w.ImpactBoundaryCount != nextBoundaryCount) ||
                !warned.Select(w => w.Ordinal).SequenceEqual(Enumerable.Range(1, warned.Length)) ||
                warned.SelectMany(w => w.Cells).GroupBy(cell => cell).Any(g => g.Count() > 1))
                throw new ArgumentException("같은 충돌에 예약한 운석 경고는 순서대로 서로 다른 칸을 차지해야 합니다.", nameof(warnings));
            if (active.Any(f => f == null || f.Footprint.ImpactBoundaryCount >= nextBoundaryCount) ||
                active.GroupBy(f => (f.Footprint.ImpactBoundaryCount, f.Footprint.Ordinal)).Any(g => g.Count() > 1))
                throw new ArgumentException("불길에는 이전 충돌의 유일한 운석 출처가 필요합니다.", nameof(flames));
            Warnings = Array.AsReadOnly(warned);
            Flames = Array.AsReadOnly(active.OrderBy(f => f.Footprint.ImpactBoundaryCount).ThenBy(f => f.Footprint.Ordinal).ToArray());
        }

        /// <summary>겹친 영역에서는 가장 최근 충돌, 같은 충돌에서는 가장 앞선 운석을 출처로 쓴다.</summary>
        public MeteorFlameArea FlameAt(IReadOnlyList<CellCoord> cells) => Flames
            .Where(f => f.Footprint.Cells.Any(cells.Contains))
            .OrderByDescending(f => f.Footprint.ImpactBoundaryCount).ThenBy(f => f.Footprint.Ordinal).FirstOrDefault();

        internal void ValidateDefinition(MeteorGimmickSpec spec, int width, int height)
        {
            if (Warnings.Count != 0 && (Warnings.Count < spec.MinimumMeteorCount || Warnings.Count > spec.MaximumMeteorCount) ||
                !Warnings.Concat(Flames.Select(f => f.Footprint)).All(f => f.Ordinal <= spec.MaximumMeteorCount &&
                    f.Fits(spec.FootprintSideLength, width, height)) ||
                Flames.Any(f => f.ExpiresAtBoundaryCount - f.Footprint.ImpactBoundaryCount != spec.FlameControllerTurns))
                throw new InvalidOperationException("운석 경고·불길의 상태가 정의 또는 보드 범위와 다릅니다.");
        }

        public override bool SameValues(StageGimmickState other) => other is MeteorGimmickState state && Id == state.Id &&
            NextBoundaryCount == state.NextBoundaryCount && Warnings.Count == state.Warnings.Count && Flames.Count == state.Flames.Count &&
            Warnings.Zip(state.Warnings, (x, y) => x.SameValues(y)).All(equal => equal) &&
            Flames.Zip(state.Flames, (x, y) => x.SameValues(y)).All(equal => equal);
    }
}
