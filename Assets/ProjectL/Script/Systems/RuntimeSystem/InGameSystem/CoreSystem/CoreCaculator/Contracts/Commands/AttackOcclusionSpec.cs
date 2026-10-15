using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands
{
    public enum AttackWallDelivery { RequireClear = 1, IgnoreWalls = 2 }
    public enum AttackWallPropagation { FromImpact = 1, IgnoreWalls = 2 }

    /// <summary>공격 전달과 파급의 벽 통과 정책이다. 진영 자격과 셀 점유는 변경하지 않는다.</summary>
    public sealed class AttackOcclusionSpec
    {
        public static AttackOcclusionSpec Default { get; } = new AttackOcclusionSpec(AttackWallDelivery.RequireClear, AttackWallPropagation.FromImpact);
        public AttackWallDelivery Delivery { get; }
        public AttackWallPropagation Propagation { get; }

        public AttackOcclusionSpec(AttackWallDelivery delivery, AttackWallPropagation propagation)
        {
            if (!Enum.IsDefined(typeof(AttackWallDelivery), delivery)) throw new ArgumentOutOfRangeException(nameof(delivery));
            if (!Enum.IsDefined(typeof(AttackWallPropagation), propagation)) throw new ArgumentOutOfRangeException(nameof(propagation));
            Delivery = delivery;
            Propagation = propagation;
        }
    }
}
