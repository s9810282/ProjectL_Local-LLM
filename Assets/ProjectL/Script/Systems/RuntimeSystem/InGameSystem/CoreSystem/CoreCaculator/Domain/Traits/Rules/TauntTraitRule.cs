using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules
{
    internal sealed class TauntTraitRule : ICombatTraitRule
    {
        public TraitKey Key => TraitKey.Taunt;

        internal bool Affects(TauntTraitDefinition definition, CharacterCombatFacts source, CharacterCombatFacts actor)
        {
            if (actor.Owner != Owner.Enemy || !new HostilityPolicy().AreHostile(actor.Owner, source.Owner) || source.Health.IsDead || actor.Health.IsDead ||
                source.Area != CharacterRuntimeArea.Field || actor.Area != CharacterRuntimeArea.Field) return false;
            var area = new TraitAreaSpec(TraitAreaFootprint.SquareNeighbors, definition.RadiusCells);
            return source.OccupiedCells.Any(origin => actor.OccupiedCells.Any(cell => TraitAreaRules.Contains(origin, cell, area)));
        }
    }
}
