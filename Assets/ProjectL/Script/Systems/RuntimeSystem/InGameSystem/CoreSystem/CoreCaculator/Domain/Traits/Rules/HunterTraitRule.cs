using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules
{
    internal sealed class HunterTraitRule : ICombatTraitRule
    {
        public TraitKey Key => TraitKey.Hunter;
        internal IReadOnlyList<EntityId> Highest(CharacterCombatFacts owner, IEnumerable<CharacterCombatFacts> characters)
        {
            var candidates = characters.Where(c => c.Area == CharacterRuntimeArea.Field && !c.Health.IsDead &&
                new HostilityPolicy().AreHostile(owner.Owner, c.Owner)).OrderBy(c => c.EntityId).ToArray();
            if (candidates.Length == 0) return System.Array.Empty<EntityId>();
            int highest = candidates.Max(c => c.Health.CurrentHealth);
            return candidates.Where(c => c.Health.CurrentHealth == highest).Select(c => c.EntityId).ToArray();
        }
        internal OwnedCharacterRecord Award(OwnedCharacterRecord source, int amount) => new(source.Id, source.CharacterKey,
            source.Role, source.Level, source.Experience, source.Growth, checked(source.HunterAttackBonus + amount),
            source.SpecializationCategory);
    }
}
