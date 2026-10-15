using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>기존 가시밭 원본만 해석한다. 공격 분류·방어 규칙이나 별도 환경 원본을 추가하지 않는다.</summary>
    public sealed class ThornMovementTileRule : IMovementTileRule
    {
        public string Key => "thorn";
        public IReadOnlyList<EntityId> TargetIds(MovementTileFact tile, EntityId mover, CombatCommandInput current) => new[] { mover };
        public MovementStepEffects Evaluate(MovementStepInput input, MovementTileFact tile) =>
            throw new NotSupportedException("가시밭은 기존 ThornFields 원본을 사용합니다.");
        public MovementStepEffects EvaluateThorn(MovementStepInput input, ThornFieldCellSnapshot field) =>
            new(damage: new[] { new MovementHealthRequest(input.MoverId, field.ThornDamage) });
    }
}
