using System;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules
{
    internal sealed class ReconSquadTraitRule : ICombatTraitRule
    {
        public TraitKey Key => TraitKey.ReconSquad;
        internal bool CanApply(CharacterCombatFacts actor, CharacterCombatFacts target, CellCoord cell, int radius) =>
            target.Area == CharacterRuntimeArea.Field && !target.Health.IsDead &&
            new HostilityPolicy().AreHostile(actor.Owner, target.Owner) && target.OccupiedCells.Any(c =>
                c.X == cell.X && Math.Abs((long)c.Y - cell.Y) <= radius ||
                c.Y == cell.Y && Math.Abs((long)c.X - cell.X) <= radius);
    }
}
