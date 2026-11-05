using System.Linq;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules
{
    internal sealed class GroundCompactionTraitRule : ICombatTraitRule
    {
        public TraitKey Key => TraitKey.GroundCompaction;
        internal static IReadOnlyList<TraitTrailFact> SourcesAt(CellCoord cell, MovementEnvironmentFacts environment)
        {
            var sources = environment.Tiles.OfType<TraitTrailFact>().Where(t => t.Active && t.Key == TraitKey.GroundCompaction &&
                t.Cell == cell && t.MovementRangeBonus > 0).ToArray();
            int maximum = sources.Select(t => t.MovementRangeBonus).DefaultIfEmpty(0).Max();
            return sources.Where(t => t.MovementRangeBonus == maximum).OrderBy(t => t.Id).ToArray();
        }
        internal static int MovementBonusAt(CellCoord cell, MovementEnvironmentFacts environment) =>
            SourcesAt(cell, environment).FirstOrDefault()?.MovementRangeBonus ?? 0;

        internal TraitTrailFact Create(GroundCompactionTraitDefinition definition, EffectOriginKey origin, Owner side, CellCoord cell,
            long id, long turnSequence) => new(id, cell, origin, side, definition.MovementRangeBonus, 0, 0,
                definition.OpposingTurnEndsToExpire, true, new[] { Owner.Player, Owner.Enemy }, turnSequence);
    }
}
