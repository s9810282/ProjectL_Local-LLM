using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules
{
    internal sealed class SoloActionTraitRule : ICombatTraitRule
    {
        public TraitKey Key => TraitKey.SoloAction;
        internal bool Eligible(SoloActionTraitDefinition definition, CharacterCombatFacts source, CharacterCombatFacts recipient,
            IReadOnlyList<CharacterCombatFacts> characters)
        {
            if (source.EntityId != recipient.EntityId || source.Health.IsDead || source.Area != CharacterRuntimeArea.Field) return false;
            var area = new TraitAreaSpec(TraitAreaFootprint.SquareNeighbors, definition.RadiusCells);
            int occupied = characters.Where(c => c.EntityId != source.EntityId && !c.Health.IsDead && c.Area == CharacterRuntimeArea.Field)
                .SelectMany(c => c.OccupiedCells).Where(cell => source.OccupiedCells.Any(origin => TraitAreaRules.Contains(origin, cell, area))).Distinct().Count();
            return occupied == definition.RequiredOccupiedNeighborCount;
        }
    }
}
