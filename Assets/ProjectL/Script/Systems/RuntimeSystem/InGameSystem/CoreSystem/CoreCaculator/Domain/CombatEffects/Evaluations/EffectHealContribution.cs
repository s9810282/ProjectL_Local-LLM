using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Evaluations
{
    internal readonly struct EffectHealContribution
    {
        internal EffectOriginKey Origin { get; }
        internal EntityId TargetId { get; }
        internal long RequestedAmount { get; }

        internal EffectHealContribution(
            EffectOriginKey origin,
            EntityId targetId,
            long requestedAmount)
        {
            if (origin.IsValid == false) throw new ArgumentException(nameof(origin));
            if (targetId.IsEmpty) throw new ArgumentException(nameof(targetId));
            if (requestedAmount <= 0) throw new ArgumentOutOfRangeException(nameof(requestedAmount));

            Origin = origin;
            TargetId = targetId;
            RequestedAmount = requestedAmount;
        }
    }
}
