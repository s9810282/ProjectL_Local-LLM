using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Movement;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>
    /// 재구성 중인 유닛의 자기 진영 턴 시작 처리입니다. 진영 턴 시작에서 이동 투영을 만든 뒤, 지속 피해 앞에서 한 번 부릅니다(설계 P3).
    /// 남은 횟수를 1 줄이고, 0이 되면 상태를 지워 그 턴에 바로 행동하게 합니다. 부활한 유닛은 일반 유닛으로서 이어지는 턴 시작 효과를 받습니다.
    /// Lv3는 부활 순간 주변 8칸의 캐릭터를 바깥으로 한 칸 밉니다(설계 P13).
    /// </summary>
    internal static class ReconstructionTurnApplication
    {
        internal static void BeginSide(MovementProjection projection, Owner side, IReadOnlyList<IMovementTileRule> rules)
        {
            if (projection == null) throw new ArgumentNullException(nameof(projection));
            if (rules == null) throw new ArgumentNullException(nameof(rules));
            if (side != Owner.Player && side != Owner.Enemy) throw new ArgumentOutOfRangeException(nameof(side));
            var batch = projection.Damage.Characters.Values.Where(c => c.Owner == side && c.Area == CharacterRuntimeArea.Field &&
                !c.Health.IsDead && ReconstructionTraitRule.IsReconstructing(c)).OrderBy(c => c.EntityId).Select(c => c.EntityId).ToArray();
            foreach (var id in batch)
            {
                // 앞선 부활 넉백으로 승패가 정해졌거나 이 유닛이 죽었으면 남은 처리를 하지 않는다.
                if (projection.Terminal) break;
                var before = projection.Character(id);
                if (before.Health.IsDead || before.Area != CharacterRuntimeArea.Field) continue;
                var after = ReconstructionTraitRule.BeginOwnTurn(before, out bool revived);
                if (revived)
                {
                    var activation = new List<CombatEvent>(1);
                    CombatCommandEvents.Trait(activation, id, TraitKey.Reconstruction, TraitActivationKind.ReconstructionEnded, 0, new[] { id });
                    projection.Events.Add(activation[0]);
                }
                projection.MergeCharacter(new CharacterStateChange(before, after));
                CombatCommandEvents.TraitState(projection.Events, before, after);
                var status = ReconstructionTraitRule.Status(after);
                if (revived && status != null && EnemyDeathTraitLevels.ReconstructionPushesOnRevive(status.Level))
                    PushNeighbors(projection, after, rules);
            }
        }

        /// <summary>
        /// 부활 순간 주변 8칸의 한 칸 캐릭터를 EntityId 순으로 바깥 8방향 한 칸 밉니다. 면역은 공격 넉백과 같은 판정이고,
        /// 칸 효과는 이동 투영 강제 이동 입구가 정산합니다. 장애물·구조물·여러 칸 유닛은 밀지 않고, 승패가 정해지면 멈춥니다.
        /// </summary>
        private static void PushNeighbors(MovementProjection projection, CharacterCombatFacts revived, IReadOnlyList<IMovementTileRule> rules)
        {
            var center = revived.Anchor.Value;
            var area = new HashSet<CellCoord>();
            CombatHazardAreas.SurroundingCells(center, projection.Input.State.BoardWidth, projection.Input.State.BoardHeight, area);
            var targets = projection.Damage.Characters.Values.Where(c => c.EntityId != revived.EntityId &&
                    c.Area == CharacterRuntimeArea.Field && !c.Health.IsDead && c.Anchor.HasValue &&
                    c.OccupiedCells.Count == 1 && area.Contains(c.Anchor.Value))
                .OrderBy(c => c.EntityId).Select(c => c.EntityId).ToArray();
            if (targets.Length == 0) return;
            var cause = new CombatMovementCause(CombatMovementAgency.Forced, TraitKey.Reconstruction);
            int activationIndex = projection.Events.Count;
            var activations = new List<CombatEvent>();
            var pushed = new List<EntityId>();
            var pushedCells = new List<CellCoord>();
            foreach (var id in targets)
            {
                if (projection.Terminal) break;
                var victim = projection.Character(id);
                if (victim.Health.IsDead || victim.Area != CharacterRuntimeArea.Field || !victim.Anchor.HasValue) continue;
                var input = projection.CurrentInput();
                if (KnockbackImmunityCheck.IsImmune(input, victim, out bool preventedBySteadfastWill))
                {
                    if (preventedBySteadfastWill)
                        CombatCommandEvents.Trait(activations, id, TraitKey.SteadfastWill, TraitActivationKind.KnockbackPrevented, 1,
                            new[] { id }, victim.OccupiedCells);
                    continue;
                }
                var start = victim.Anchor.Value;
                var step = ForcedDisplacementGeometry.ResolveBranchStep(center, start);
                ForcedDisplacementPath path = null;
                foreach (var _ in MovementEnvironmentCalculator.ResolveForcedStepsSteps(projection, input, id, step, 1,
                    knockbackTerrain: true, cause, rules, value => path = value)) { }
                if (path.Steps.Count == 0) continue;
                pushed.Add(id);
                pushedCells.Add(start);
                pushedCells.AddRange(path.Steps);
            }
            // 실제로 밀린 대상만 싣는다. 판독기는 공격 경로가 없으면 대상마다 실제 이동을 요구한다.
            if (pushed.Count > 0)
                CombatCommandEvents.Trait(activations, revived.EntityId, TraitKey.Reconstruction, TraitActivationKind.KnockbackApplied, 1,
                    pushed, pushedCells.Distinct().ToArray());
            projection.Events.InsertRange(activationIndex, activations);
        }
    }
}
