using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules
{
    internal sealed class CohesionTraitRule : ICombatTraitRule
    {
        public TraitKey Key => TraitKey.Cohesion;
        internal bool Eligible(CohesionTraitDefinition definition, CharacterCombatFacts source, CharacterCombatFacts recipient)
        {
            if (source.EntityId == recipient.EntityId || source.Owner != recipient.Owner || source.Health.IsDead || recipient.Health.IsDead ||
                source.Area != CharacterRuntimeArea.Field || recipient.Area != CharacterRuntimeArea.Field) return false;
            var area = new TraitAreaSpec(TraitAreaFootprint.SquareNeighbors, definition.RadiusCells);
            return source.OccupiedCells.Any(origin => recipient.OccupiedCells.Any(cell => TraitAreaRules.Contains(origin, cell, area)));
        }

        internal int Defense(CohesionTraitDefinition definition, CharacterCombatFacts source, IReadOnlyList<CharacterCombatFacts> characters) =>
            checked(characters.Count(c => Eligible(definition, source, c)) * definition.DefensePerAdjacentAlly);
    }
}
