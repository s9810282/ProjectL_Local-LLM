using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat
{
    public sealed class HostilityPolicy
    {
        public bool AreHostile(Owner attacker, Owner target)
        {
            if (attacker == target)
                return false;

            if (attacker == Owner.Neutral || target == Owner.Neutral)
                return false;

            return true;
        }
    }
}