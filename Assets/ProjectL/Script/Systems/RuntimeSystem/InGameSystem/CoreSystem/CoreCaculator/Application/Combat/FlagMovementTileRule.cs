using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>깃발 획득을 판단한다. 이미 깃발을 보유한 도착자는 발동하지 않고 필드 깃발을 남긴다(G5).</summary>
    public sealed class FlagMovementTileRule : IMovementTileRule
    {
        public string Key => FlagSourceFact.Key;
        public IReadOnlyList<EntityId> TargetIds(MovementTileFact tile, EntityId mover, CombatCommandInput current) => new[] { mover };

        public MovementStepEffects Evaluate(MovementStepInput input, MovementTileFact tile)
        {
            var flag = (FlagSourceFact)tile;
            if (input.HeldEnvironment.Flag != null) return new MovementStepEffects();
            var final = HeldEnvironmentEffects.WithFlag(input.HeldEnvironment, HeldFlag.From(flag));
            return new MovementStepEffects(expectedSource: flag, finalSource: flag.WithActive(false),
                heldChanges: new[] { new MovementHeldChange(input.MoverId, input.HeldEnvironment, final) });
        }
    }
}
