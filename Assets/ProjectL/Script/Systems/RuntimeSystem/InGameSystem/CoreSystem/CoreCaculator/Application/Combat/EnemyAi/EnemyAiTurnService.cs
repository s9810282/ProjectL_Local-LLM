using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Diagnostics;
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
        /// <summary>
        /// 한 적 턴을 사실 기반으로 계산하고 최종 변경 하나만 반환한다.
        /// 적도 이동 → 공격을 할 수 있으므로, 이동한 적은 다음 계산에서 이 순회가 다시 골라 공격만 시도한다.
        /// </summary>
        internal static CombatCommandTransition Calculate(CombatCommandInput input, IReadOnlyList<IMovementTileRule> rules)
        {
            if (input.ProcessingKind != CombatProcessingKind.Advance || input.State.Phase != CombatFlowPhase.Automatic ||
                input.State.Turn.ControllerPhase != CombatControllerPhase.Enemy)
                throw new InvalidOperationException("자동 적 진영 턴의 입력이 아닙니다.");
            using var sample = CoreProfiling.Sample(CoreProfileSection.EnemyTurn);
            // 대기 반응이 전투를 끝낼 수 있으므로 적 진영 종료·밤 정산보다 먼저 처리한다.
            var pendingRoars = BossRoarCalculator.PendingBosses(input);
            if (pendingRoars.Count != 0)
                return BossRoarCalculator.TryRoar(input, pendingRoars[0], rules);
            var policy = new EnemyAiPolicy(new HostilityPolicy(), rules);
            foreach (var actor in input.Characters.Where(c => c.Owner == Owner.Enemy &&
                c.Area == CharacterRuntimeArea.Field && !c.Health.IsDead && UnitActionUseRules.CanStartAttack(c.ActionUse)).OrderBy(c => c.EntityId))
            {
                var current = CombatCommandComposition.Intent(input.WithActor(actor.EntityId), CombatCommandKind.Attack);
                var range = CombatCommandCalculator.GetAttackRange(current);
                if (policy.TrySelectAttackAim(current, range, out var aim, out var tauntSource))
                {
                    var attack = CombatCommandCalculator.Calculate(CombatCommandComposition.Intent(current, CombatCommandKind.Attack, aim), rules);
                    if (attack.Accepted) return CombatCommandComposition.Merge(input, new[] { RecordTaunt(attack, actor, tauntSource) });
                    // 적이 볼 수 있는 사실로 고른 공격이 숨은 사실 때문에 거절되면 다른 조준을 더 찾지 않고 추격 이동을 시도한다.
                }
                // 이미 이동한 적은 공격만 남는다. 다시 이동하지 않는다.
                if (!UnitActionUseRules.CanStartMove(actor.ActionUse)) continue;
                if (policy.TrySelectPursuitTarget(current, out var target, out var pursuitTaunt) &&
                    EnemyMovementPathfinder.TryFind(current, target, out var path))
                {
                    var movement = CombatCommandCalculator.Calculate(CombatCommandComposition.Intent(current, CombatCommandKind.Move, path: path), rules);
                    // 계산 한 번에 수락된 행동 하나만 돌려준다. 숨은 환경에 의해 거절된 후보는 현재 계산에서만 건너뛴다.
                    if (movement.Accepted) return CombatCommandComposition.Merge(input, new[] { RecordTaunt(movement, actor, pursuitTaunt) });
                }
            }
            return CombatTurnCalculator.EndEnemyPhase(input, rules);
        }

        private static CombatCommandTransition RecordTaunt(CombatCommandTransition transition, CharacterCombatFacts actor, EntityId source)
        {
            if (source.IsEmpty) return transition;
            var events = new List<CombatEvent>();
            CombatCommandEvents.Trait(events, source, TraitKey.Taunt, TraitActivationKind.TauntSelected, 0,
                new[] { actor.EntityId }, actor.OccupiedCells, CombatCommandEvents.FindOrigin(transition.Events, actor.EntityId));
            events.AddRange(transition.Events);
            return CombatCommandComposition.WithEvents(transition, events);
        }
    }
}
