using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules
{
    internal sealed class PositionSwapTraitRule : ICombatTraitRule
    {
        public TraitKey Key => TraitKey.PositionSwap;
        internal static bool AllowsExchange(CharacterCombatFacts actor, CharacterCombatFacts passenger) =>
            actor != null && passenger != null && actor.EntityId != passenger.EntityId && actor.Owner == passenger.Owner &&
            actor.Area == CharacterRuntimeArea.Field && passenger.Area == CharacterRuntimeArea.Field &&
            !actor.Health.IsDead && !passenger.Health.IsDead && actor.Anchor.HasValue && passenger.Anchor.HasValue &&
            actor.Rotation.HasValue && passenger.Rotation.HasValue &&
            actor.OccupiedCells.Count == 1 && actor.OccupiedCells[0] == actor.Anchor.Value &&
            passenger.OccupiedCells.Count == 1 && passenger.OccupiedCells[0] == passenger.Anchor.Value &&
            actor.Stats.TraitStatuses.ToArray().Any(t => t.Key == TraitKey.PositionSwap);
    }
}
