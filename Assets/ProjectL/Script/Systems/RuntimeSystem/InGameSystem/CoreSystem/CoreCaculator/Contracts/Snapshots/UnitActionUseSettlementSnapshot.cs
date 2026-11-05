using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots
{
    /// <summary>일반 행동 한 번의 행위자와 원본·최종 행동 사용 사실이다.</summary>
    public sealed class UnitActionUseSettlementSnapshot
    {
        public EntityId ActorId { get; }
        public UnitActionUse BeforeActionUse { get; }
        public UnitActionUse FinalActionUse { get; }

        public UnitActionUseSettlementSnapshot(EntityId actorId, UnitActionUse beforeActionUse, UnitActionUse finalActionUse)
        {
            if (actorId.IsEmpty) throw new ArgumentException("행동 정산에는 행위자가 필요합니다.", nameof(actorId));
            if (!Enum.IsDefined(typeof(UnitActionUse), beforeActionUse) || !Enum.IsDefined(typeof(UnitActionUse), finalActionUse))
                throw new ArgumentException("행동 사용 정산 값이 잘못되었습니다.");
            ActorId = actorId;
            BeforeActionUse = beforeActionUse;
            FinalActionUse = finalActionUse;
        }
    }
}
