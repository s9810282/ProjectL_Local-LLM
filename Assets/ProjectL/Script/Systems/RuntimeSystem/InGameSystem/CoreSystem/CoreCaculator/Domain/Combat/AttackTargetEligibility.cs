using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat
{
    /// <summary>기하와 피해량에 독립적인 공격 대상 자격을 판정한다.</summary>
    internal static class AttackTargetEligibility
    {
        private static readonly HostilityPolicy Hostility = new HostilityPolicy();

        internal static bool CanTargetCharacter(CharacterCombatFacts actor, CharacterCombatFacts target,
            in AttackTargetingPolicy policy)
        {
            if (actor == null) throw new ArgumentNullException(nameof(actor));
            if (target == null) throw new ArgumentNullException(nameof(target));
            return target.EntityId != actor.EntityId && target.Area == CharacterRuntimeArea.Field &&
                !target.Health.IsDead && AllowsOwner(actor.Owner, target.Owner, policy);
        }

        internal static bool CanTargetStructure(CharacterCombatFacts actor, StructureCombatFacts target,
            bool isAttackableObstacle, in AttackTargetingPolicy policy)
        {
            if (actor == null) throw new ArgumentNullException(nameof(actor));
            if (target == null) throw new ArgumentNullException(nameof(target));
            return !target.Health.IsDead && (isAttackableObstacle || AllowsOwner(actor.Owner, target.Owner, policy));
        }

        private static bool AllowsOwner(Owner actor, Owner target, in AttackTargetingPolicy policy)
            => Hostility.AreHostile(actor, target) ||
                (actor == target && policy.AllowsFriendlyTargets) ||
                (target == Owner.Neutral && policy.AllowsNeutralTargets);
    }
}
