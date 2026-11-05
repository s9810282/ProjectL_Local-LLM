using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items
{
    public sealed class ItemAttackCandidate
    {
        public EntityId TargetId { get; }
        public CellCoord AnchorAtDelivery { get; }

        public ItemAttackCandidate(EntityId targetId, CellCoord anchorAtDelivery)
        {
            if (targetId.IsEmpty) throw new ArgumentException("피격 대상 ID가 필요합니다.", nameof(targetId));
            TargetId = targetId;
            AnchorAtDelivery = anchorAtDelivery;
        }
    }
}
