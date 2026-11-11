using System.Linq;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules
{
    internal sealed class DustScreenTraitRule : ICombatTraitRule
    {
        public TraitKey Key => TraitKey.DustScreen;
        internal static IReadOnlyList<TraitTrailFact> RangeSourcesAt(CellCoord cell, MovementEnvironmentFacts environment, Owner queryingSide) =>
            SelectRangeSourcesAt(cell, environment, queryingSide);

        private static IReadOnlyList<TraitTrailFact> SelectRangeSourcesAt(CellCoord cell, MovementEnvironmentFacts environment, Owner? queryingSide) =>
            Select(environment.Tiles.OfType<TraitTrailFact>().Where(t => t.Cell == cell &&
                (!t.ExemptsAlliedAttackRangePenalty || t.CreatingSide != queryingSide)), false);
        internal static IReadOnlyList<TraitTrailFact> DefenseSourcesAt(CharacterCombatFacts recipient, MovementEnvironmentFacts environment) =>
            Select(environment.Tiles.OfType<TraitTrailFact>().Where(t => recipient.OccupiedCells.Contains(t.Cell)), true);
        private static IReadOnlyList<TraitTrailFact> Select(IEnumerable<TraitTrailFact> candidates, bool defense)
        {
            var sources = candidates.Where(t => t.Active && t.Key == TraitKey.DustScreen &&
                (defense ? t.AttackDamageReductionPercent : t.AttackRangeReduction) > 0).ToArray();
            int maximum = sources.Select(t => defense ? t.AttackDamageReductionPercent : t.AttackRangeReduction).DefaultIfEmpty(0).Max();
            return sources.Where(t => (defense ? t.AttackDamageReductionPercent : t.AttackRangeReduction) == maximum).OrderBy(t => t.Id).ToArray();
        }
        internal static int RangeReductionAt(CellCoord cell, MovementEnvironmentFacts environment, Owner queryingSide) =>
            RangeSourcesAt(cell, environment, queryingSide).FirstOrDefault()?.AttackRangeReduction ?? 0;

        internal static int UnfilteredRangeReductionAt(CellCoord cell, MovementEnvironmentFacts environment) =>
            SelectRangeSourcesAt(cell, environment, null).FirstOrDefault()?.AttackRangeReduction ?? 0;

        internal bool Reduces(TraitTrailFact trail, CharacterCombatFacts recipient, CombatDamageKind kind) => trail != null && trail.Active &&
            trail.Key == TraitKey.DustScreen && recipient != null && !recipient.Health.IsDead && recipient.Area == CharacterRuntimeArea.Field &&
            recipient.OccupiedCells.Contains(trail.Cell) &&
            (kind == CombatDamageKind.OrdinaryAttack || kind == CombatDamageKind.ExtraAttack || kind == CombatDamageKind.ReactionAttack);

        internal TraitTrailFact Create(DustScreenTraitDefinition definition, EffectOriginKey origin, Owner side, CellCoord cell, long id) =>
            new(id, cell, origin, side, 0, definition.AttackRangeReduction, definition.AttackDamageReductionPercent,
                definition.OpposingTurnEndsToExpire, true, new[] { Owner.Player, Owner.Enemy },
                definition.ExemptsAlliedAttackRangePenalty);
    }
}
