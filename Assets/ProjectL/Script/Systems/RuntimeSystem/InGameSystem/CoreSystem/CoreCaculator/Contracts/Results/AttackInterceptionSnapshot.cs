using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results
{
    public sealed class AttackInterceptionSnapshot
    {
        public EntityId GuardianId { get; }
        public EntityId ProtectedId { get; }
        public EntityId ActualRecipientId { get; }
        public int DeliveryIndex { get; }
        public AttackInterceptionSnapshot(EntityId guardianId, EntityId protectedId, EntityId actualRecipientId, int deliveryIndex)
        {
            if (guardianId.IsEmpty || protectedId.IsEmpty || actualRecipientId.IsEmpty || guardianId == protectedId)
                throw new ArgumentException("가로채기에는 서로 다른 보호자와 보호 대상이 필요합니다.");
            if (deliveryIndex < 0) throw new ArgumentOutOfRangeException(nameof(deliveryIndex));
            GuardianId = guardianId; ProtectedId = protectedId; ActualRecipientId = actualRecipientId; DeliveryIndex = deliveryIndex;
        }
    }
}
