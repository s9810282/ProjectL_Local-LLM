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

        private static IReadOnlyList<TraitTrailFact> SelectRangeSourcesAt(CellCoord cell, MovementEnvironmentFacts environment, Owner? queryingSide)
        {
            List<TraitTrailFact> found = null;
            var tiles = environment.Tiles;
            for (int i = 0; i < tiles.Count; i++)
                if (tiles[i] is TraitTrailFact t && t.Cell == cell &&
                    (!t.ExemptsAlliedAttackRangePenalty || t.CreatingSide != queryingSide))
                    (found ??= new List<TraitTrailFact>()).Add(t);
            return Select(found, false);
        }
        internal static IReadOnlyList<TraitTrailFact> DefenseSourcesAt(CharacterCombatFacts recipient, MovementEnvironmentFacts environment)
        {
            List<TraitTrailFact> found = null;
            var tiles = environment.Tiles;
            var cells = recipient.OccupiedCells;
            for (int i = 0; i < tiles.Count; i++)
            {
                if (!(tiles[i] is TraitTrailFact t)) continue;
                for (int c = 0; c < cells.Count; c++)
                    if (cells[c] == t.Cell) { (found ??= new List<TraitTrailFact>()).Add(t); break; }
            }
            return Select(found, true);
        }
        private static IReadOnlyList<TraitTrailFact> Select(List<TraitTrailFact> candidates, bool defense)
        {
            if (candidates == null) return System.Array.Empty<TraitTrailFact>();
            int maximum = 0;
            bool any = false;
            foreach (var t in candidates)
            {
                int value = defense ? t.AttackDamageReductionPercent : t.AttackRangeReduction;
                if (!t.Active || t.Key != TraitKey.DustScreen || value <= 0) continue;
                if (!any || value > maximum) maximum = value;
                any = true;
            }
            if (!any) return System.Array.Empty<TraitTrailFact>();
            var result = new List<TraitTrailFact>();
            foreach (var t in candidates)
                if (t.Active && t.Key == TraitKey.DustScreen && (defense ? t.AttackDamageReductionPercent : t.AttackRangeReduction) == maximum)
                    result.Add(t);
            result.Sort((a, b) => a.Id.CompareTo(b.Id));
            return result.ToArray();
        }
        internal static int RangeReductionAt(CellCoord cell, MovementEnvironmentFacts environment, Owner queryingSide) =>
            RangeSourcesAt(cell, environment, queryingSide).FirstOrDefault()?.AttackRangeReduction ?? 0;

        internal static int UnfilteredRangeReductionAt(CellCoord cell, MovementEnvironmentFacts environment) =>
            SelectRangeSourcesAt(cell, environment, null).FirstOrDefault()?.AttackRangeReduction ?? 0;

        internal bool Reduces(TraitTrailFact trail, CharacterCombatFacts recipient, CombatDamageKind kind) => trail != null && trail.Active &&
            trail.Key == TraitKey.DustScreen && recipient != null && !recipient.Health.IsDead && recipient.Area == CharacterRuntimeArea.Field &&
            recipient.OccupiedCells.Contains(trail.Cell) &&
            CombatDamageKinds.IsAttack(kind);

        internal TraitTrailFact Create(DustScreenTraitDefinition definition, EffectOriginKey origin, Owner side, CellCoord cell, long id) =>
            new(id, cell, origin, side, 0, definition.AttackRangeReduction, definition.AttackDamageReductionPercent,
                definition.OpposingTurnEndsToExpire, true, new[] { Owner.Player, Owner.Enemy },
                definition.ExemptsAlliedAttackRangePenalty);
    }
}
