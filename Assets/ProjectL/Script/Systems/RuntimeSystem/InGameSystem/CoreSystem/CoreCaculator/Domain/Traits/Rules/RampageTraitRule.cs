using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules
{
    internal sealed class RampageTraitRule : ICombatTraitRule
    {
        public TraitKey Key => TraitKey.Rampage;
        internal static RampageTraitStatusSnapshot Status(CharacterCombatFacts character) =>
            character.Stats.TraitStatuses.ToArray().OfType<RampageTraitStatusSnapshot>().SingleOrDefault();
        internal static bool IsActive(CharacterCombatFacts character) => Status(character) != null &&
            character.Area == CharacterRuntimeArea.Field && !character.Health.IsDead &&
            100L * character.Health.CurrentHealth < 40L * character.Health.MaxHealth;

        internal static int AttackBonus(CharacterCombatFacts character)
        {
            if (!IsActive(character)) return 0;
            var status = Status(character);
            return checked(status.BaseAttackBonus + status.RankAttackBonus + status.KillAttackBonus);
        }

        internal static int MovementBonus(CharacterCombatFacts character) => IsActive(character) ? Status(character).MovementBonus : 0;
    }
}
