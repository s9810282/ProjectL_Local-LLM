using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.StageGimmicks
{
    public sealed class RailwayGimmickActivationSnapshot : StageGimmickActivationSnapshot
    {
        public ReadOnlyMemory<CellCoord> AffectedRailCells { get; }
        public ReadOnlyMemory<EntityId> KilledUnitIds { get; }

        public RailwayGimmickActivationSnapshot(StageGimmickId gimmickId, CellCoord[] affectedRailCells, EntityId[] killedUnitIds) :
            base(gimmickId, StageGimmickKey.Railway)
        {
            if (affectedRailCells == null)
                throw new ArgumentNullException(nameof(affectedRailCells));
            if (killedUnitIds == null)
                throw new ArgumentNullException(nameof(killedUnitIds));

            CellCoord[] copiedCells = new CellCoord[affectedRailCells.Length];
            Array.Copy(affectedRailCells, copiedCells, affectedRailCells.Length);

            EntityId[] copiedIds = new EntityId[killedUnitIds.Length];
            Array.Copy(killedUnitIds, copiedIds, killedUnitIds.Length);

            AffectedRailCells = copiedCells;
            KilledUnitIds = copiedIds;
        }
    }
}
