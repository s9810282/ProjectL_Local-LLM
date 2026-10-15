using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>배지 획득을 판단한다. 중복 배지도 필드에서는 소모하지만 보호를 더하지 않는다(G3).</summary>
    public sealed class SheriffBadgeMovementTileRule : IMovementTileRule
    {
        public string Key => SheriffBadgeSourceFact.Key;
        public IReadOnlyList<EntityId> TargetIds(MovementTileFact tile, EntityId mover, CombatCommandInput current) => new[] { mover };

        public MovementStepEffects Evaluate(MovementStepInput input, MovementTileFact tile)
        {
            var badge = (SheriffBadgeSourceFact)tile;
            var final = HeldEnvironmentEffects.WithBadge(input.HeldEnvironment, badge.Id);
            return new MovementStepEffects(expectedSource: badge, finalSource: badge.WithActive(false),
                heldChanges: ReferenceEquals(final, input.HeldEnvironment) ? Array.Empty<MovementHeldChange>()
                    : new[] { new MovementHeldChange(input.MoverId, input.HeldEnvironment, final) });
        }
    }
}
