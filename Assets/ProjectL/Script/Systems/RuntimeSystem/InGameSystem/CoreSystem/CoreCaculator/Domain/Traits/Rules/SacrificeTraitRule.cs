using System;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules
{
    internal sealed class SacrificeTraitRule : ICombatTraitRule
    {
        public TraitKey Key => TraitKey.Sacrifice;
        internal bool Eligible(SacrificeTraitDefinition definition, CharacterCombatFacts actor, CharacterCombatFacts source,
            CharacterCombatFacts recipient, CombatControllerPhase controller)
        {
            if (controller != CombatControllerPhase.Enemy || source.EntityId == recipient.EntityId || source.Owner != recipient.Owner ||
                !new HostilityPolicy().AreHostile(actor.Owner, recipient.Owner) || source.Health.IsDead || recipient.Health.IsDead ||
                source.Area != CharacterRuntimeArea.Field || recipient.Area != CharacterRuntimeArea.Field) return false;
            var area = new TraitAreaSpec(TraitAreaFootprint.SquareNeighbors, definition.RadiusCells);
            return source.OccupiedCells.Any(origin => recipient.OccupiedCells.Any(cell => TraitAreaRules.Contains(origin, cell, area)));
        }

        internal static int UsedAt(long lastSequence, int used, long currentSequence)
        {
            if (currentSequence < lastSequence) throw new InvalidOperationException("희생의 진영 순서가 역행했습니다.");
            return lastSequence == currentSequence ? used : 0;
        }

        internal static long Distance(CharacterCombatFacts source, CharacterCombatFacts recipient) =>
            source.OccupiedCells.Min(a => recipient.OccupiedCells.Min(b => Math.Abs((long)a.X - b.X) + Math.Abs((long)a.Y - b.Y)));
    }
}
