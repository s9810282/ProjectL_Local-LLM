using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>낙하의 물리 출처·영향 칸과 수신자를 고정한다. 잔해 칸은 별도 지형 사건으로 기록한다.</summary>
    public sealed class CombatPillarFallEvent : CombatEvent
    {
        public CombatObstacleDurabilityEvent Destruction { get; }
        public CombatObstacleReactionEvent Reaction { get; }
        public EntityId PillarId => Destruction.ObstacleId;
        public CellCoord PillarCell => Destruction.Expected.Anchor;
        public CombatDamageOrigin Origin => Destruction.Origin;
        public int FallLength { get; }
        public CellCoord Direction { get; }
        public IReadOnlyList<CellCoord> ImpactCells { get; }
        public IReadOnlyList<EntityId> CrushedUnitIds { get; }
        public IReadOnlyList<EntityId> DestroyedObstacleIds { get; }

        public CombatPillarFallEvent(CombatObstacleDurabilityEvent destruction, CombatObstacleReactionEvent reaction,
            int fallLength, CellCoord direction, IReadOnlyList<CellCoord> impactCells, IReadOnlyList<EntityId> crushedUnitIds,
            IReadOnlyList<EntityId> destroyedObstacleIds) : base(CombatEventKind.PillarFell)
        {
            Destruction = destruction ?? throw new ArgumentNullException(nameof(destruction));
            Reaction = reaction ?? throw new ArgumentNullException(nameof(reaction));
            if (destruction.Expected.Definition.Key != EntityKey.Structure_Pillar || !destruction.Final.IsDestroyed ||
                destruction.Cause == ObstacleDestructionCause.Fall || destruction.Origin?.PhysicalCell == null ||
                reaction.ObstacleId != destruction.ObstacleId || reaction.ContentKind != ObstacleContentKind.Pillar ||
                reaction.Reaction != ObstacleReactionKind.Destroyed || reaction.Origin.PhysicalCell != PillarCell)
                throw new ArgumentException("파괴된 기둥과 같은 낙하 반응 원본이 필요합니다.");
            if (fallLength < 1) throw new ArgumentOutOfRangeException(nameof(fallLength));
            if (direction == CellCoord.Zero || direction.X < -1 || direction.X > 1 || direction.Y < -1 || direction.Y > 1)
                throw new ArgumentException("낙하 방향은 여덟 방향 중 하나여야 합니다.", nameof(direction));
            FallLength = fallLength;
            Direction = direction;
            var cells = (impactCells ?? throw new ArgumentNullException(nameof(impactCells))).ToArray();
            if (cells.Length > fallLength || cells.Distinct().Count() != cells.Length || cells.Contains(PillarCell))
                throw new ArgumentException("낙하 영향 칸은 원래 칸을 제외한 중복 없는 경로여야 합니다.", nameof(impactCells));
            ImpactCells = Array.AsReadOnly(cells);
            CrushedUnitIds = Array.AsReadOnly(EnvironmentSemanticBindings.CopyEntityIds(crushedUnitIds, nameof(crushedUnitIds)));
            DestroyedObstacleIds = Array.AsReadOnly(EnvironmentSemanticBindings.CopyEntityIds(destroyedObstacleIds, nameof(destroyedObstacleIds)));
            if (CrushedUnitIds.Intersect(DestroyedObstacleIds).Any() || DestroyedObstacleIds.Contains(PillarId))
                throw new ArgumentException("낙하 수신자는 유닛과 장애물이 겹치지 않아야 합니다.");
        }
    }

    /// <summary>낙하에 귀속된 벽 생성 또는 같은 ID의 낭떠러지 메우기만 표현한다.</summary>
    public sealed class CombatPillarTerrainEvent : CombatEvent
    {
        public CombatPillarFallEvent Fall { get; }
        public StaticTerrainFact Expected { get; }
        public StaticTerrainFact Final { get; }

        public CombatPillarTerrainEvent(CombatPillarFallEvent fall, StaticTerrainFact expected, StaticTerrainFact final)
            : base(CombatEventKind.PillarTerrainChanged)
        {
            Fall = fall ?? throw new ArgumentNullException(nameof(fall));
            Final = final ?? throw new ArgumentNullException(nameof(final));
            Expected = expected;
            if (!fall.ImpactCells.Contains(final.Cell) ||
                (expected == null
                    ? final.Kind != StaticTerrainKind.Wall || !final.Active ||
                        !final.KnownTo.Contains(Owner.Player) || !final.KnownTo.Contains(Owner.Enemy)
                    : !expected.IsPillarFillSuccessor(final)))
                throw new ArgumentException("기둥 지형 변경은 영향 칸의 공개 벽 생성 또는 낭떠러지 메우기여야 합니다.");
        }
    }
}
