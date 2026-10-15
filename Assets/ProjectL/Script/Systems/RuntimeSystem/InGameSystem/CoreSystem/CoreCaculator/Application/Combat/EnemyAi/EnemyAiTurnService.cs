using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyAi
{

    internal sealed class EnemyAiTurnService
    {
        /// <summary>한 적 턴을 사실 기반으로 계산하고 최종 변경 하나만 반환한다.</summary>
        internal static CombatCommandTransition Calculate(CombatCommandInput input, IReadOnlyList<IMovementTileRule> rules)
        {
            if (input.ProcessingKind != CombatProcessingKind.Advance || input.State.Phase != CombatFlowPhase.Automatic ||
                !input.Characters.Any(c => c.EntityId == input.ActorId && c.Owner == Owner.Enemy))
                throw new InvalidOperationException("자동 적 턴의 입력이 아닙니다.");
            var policy = new EnemyAiPolicy(new HostilityPolicy(), rules);
            var steps = new List<CombatCommandTransition>();
            var current = CombatCommandComposition.Intent(input, CombatCommandKind.Attack);
            bool interrupted = false;
            var range = CombatCommandCalculator.GetAttackRange(current);
            if (!policy.TrySelectAttackAim(current, range, out var aim))
            {
                if (policy.TrySelectPursuitTarget(current, out var target) &&
                    EnemyMovementPathfinder.TryFind(current, target, out var path))
                {
                    var movement = CombatCommandCalculator.Calculate(CombatCommandComposition.Intent(current, CombatCommandKind.Move, path: path), rules);
                    steps.Add(movement); current = CombatCommandComposition.Project(movement);
                    interrupted = movement.PathMovement.StopReason != MovementStopReason.Completed;
                    range = CombatCommandCalculator.GetAttackRange(CombatCommandComposition.Intent(current, CombatCommandKind.Attack));
                }
            }
            if (!interrupted && current.State.Phase != CombatFlowPhase.Finished &&
                policy.TrySelectAttackAim(CombatCommandComposition.Intent(current, CombatCommandKind.Attack), range, out aim))
            {
                var attack = CombatCommandCalculator.Calculate(CombatCommandComposition.Intent(current, CombatCommandKind.Attack, aim), rules);
                steps.Add(attack); current = CombatCommandComposition.Project(attack);
            }
            if (current.State.Phase != CombatFlowPhase.Finished && current.State.ActiveActorId == input.ActorId &&
                current.Characters.Any(c => c.EntityId == input.ActorId && c.Area == CharacterRuntimeArea.Field && !c.Health.IsDead))
                steps.Add(CombatTurnCalculator.EndTurn(CombatCommandComposition.Intent(current, CombatCommandKind.EndUnitTurn), rules));
            return CombatCommandComposition.Merge(input, steps);
        }
    }
}
