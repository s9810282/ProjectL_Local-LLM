using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Evaluations;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>한 명령에서 포착한 쓸기 입력입니다. 이동 중 대상 선정을 다시 하지 않습니다.</summary>
    internal sealed class DeadHandAttackEvaluation
    {
        private readonly CombatCommandInput _input;
        internal EffectAttackSweepContribution Contribution { get; }
        internal Rotation4 Facing { get; }
        internal AttackSweepSide Side { get; }
        internal IReadOnlyList<CellCoord> AreaCells { get; }
        internal IReadOnlyList<EntityId> TargetIds { get; }

        internal DeadHandAttackEvaluation(CombatCommandInput input, EffectAttackSweepContribution contribution,
            Rotation4 facing, AttackSweepSide side, IEnumerable<CellCoord> areaCells, IEnumerable<EntityId> targetIds)
        {
            _input = input ?? throw new ArgumentNullException(nameof(input));
            Contribution = contribution;
            Facing = facing;
            Side = side;
            AreaCells = Array.AsReadOnly(areaCells.Distinct().OrderBy(c => c.Y).ThenBy(c => c.X).ToArray());
            TargetIds = Array.AsReadOnly(targetIds.Distinct().OrderBy(id => id).ToArray());
        }

        internal void RequireSource(CombatCommandInput input)
        {
            if (!ReferenceEquals(_input, input))
                throw new InvalidOperationException("쓸기 준비는 포착한 명령 입력에서만 실행할 수 있습니다.");
        }
    }
}
