using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results
{
    public readonly struct PiercingShotEncounter
    {
        public EntityId TargetId { get; }
        public int EncounterIndex { get; }
        public CellCoord Cell { get; }
        public int RemainingPercent { get; }

        public PiercingShotEncounter(EntityId targetId, int encounterIndex, CellCoord cell, int remainingPercent)
        {
            if (targetId.IsEmpty) throw new ArgumentException("피격 대상 ID는 비어 있을 수 없습니다.", nameof(targetId));
            if (encounterIndex < 0) throw new ArgumentOutOfRangeException(nameof(encounterIndex));
            if (cell.X < 0 || cell.Y < 0) throw new ArgumentOutOfRangeException(nameof(cell));
            if (remainingPercent < 1 || remainingPercent > 100)
                throw new ArgumentOutOfRangeException(nameof(remainingPercent));
            TargetId = targetId;
            EncounterIndex = encounterIndex;
            Cell = cell;
            RemainingPercent = remainingPercent;
        }
    }
}
