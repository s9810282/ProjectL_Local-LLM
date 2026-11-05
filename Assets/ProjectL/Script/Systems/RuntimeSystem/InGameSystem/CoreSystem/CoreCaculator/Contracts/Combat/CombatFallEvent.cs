using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>낭떠러지 추락 개체와 원인 지형을 기록한다. 사망이나 제거 처리는 별도 사실로 전달한다.</summary>
    public sealed class CombatFallEvent : CombatEvent
    {
        public EntityId EntityId { get; }
        public long TerrainSourceId { get; }
        public CellCoord Cell { get; }

        public CombatFallEvent(EntityId entityId, long terrainSourceId, CellCoord cell) : base(CombatEventKind.Fell)
        {
            if (entityId.IsEmpty)
                throw new ArgumentOutOfRangeException(nameof(entityId));
            if (terrainSourceId < 1)
                throw new ArgumentOutOfRangeException(nameof(terrainSourceId));
            EntityId = entityId;
            TerrainSourceId = terrainSourceId;
            Cell = cell;
        }
    }
}
