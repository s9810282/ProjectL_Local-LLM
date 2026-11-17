using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Evaluations;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>
    /// 사망 특성(자폭·공포) 실행자입니다. 명령 완료 정산에서 한 번 불리며, 이번 전이에서 새로 죽은 보유자를 사실에서 찾아 실행합니다.
    /// 무엇을 할지는 능력 <see cref="IEffectDeathTraitRule"/>의 기여가 정하고, 이 실행자는 특성 키를 모릅니다(처치 연쇄와 같은 두 층).
    /// </summary>
    /// <remarks>
    /// 대상: 투영 입력 때 살아 있었고 지금 죽었으며, 같은 투영에서 낙사하지 않은 캐릭터 중 사망 특성 기여가 있는 유닛입니다.
    /// 한 바퀴는 EntityId 순이고, 실행이 새 사망을 만들면 다음 바퀴에서 다시 찾습니다. 승패가 정해지면 멈춥니다.
    /// </remarks>
    internal static class DeathTraitApplication
    {
        internal static IEnumerable<string> ResolveSteps(MovementProjection projection)
        {
            if (projection == null) throw new ArgumentNullException(nameof(projection));
            var engine = new CombatEffectEngine();
            var handled = projection.SettledDeathEffects;
            while (!projection.Terminal)
            {
                var round = FindNewDeaths(projection, engine, handled);
                if (round.Count == 0) yield break;
                foreach (var death in round)
                {
                    if (projection.Terminal) yield break;
                    handled.Add(death.UnitId);
                    // 한 유닛 안에서는 자폭 → 공포 순서다(설계 P8). 자폭으로 죽은 대상은 살아 있지 않아 공포 대상에서 빠진다.
                    if (death.Evaluation.SelfDestructPercent > 0)
                    {
                        foreach (var phase in ExplodeSteps(projection, death)) yield return phase;
                        yield return "DeathTraitSelfDestruct";
                    }
                    if (death.Evaluation.Fear)
                    {
                        ApplyFear(projection, death);
                        yield return "DeathTraitFear";
                    }
                }
            }
        }

        private static List<DeathTraitTrigger> FindNewDeaths(MovementProjection projection, CombatEffectEngine engine, ISet<EntityId> handled)
        {
            var fallen = projection.Events.OfType<CombatFallEvent>().Select(e => e.EntityId).ToHashSet();
            var round = new List<DeathTraitTrigger>();
            foreach (var original in projection.Input.Characters.OrderBy(c => c.EntityId))
            {
                if (original.Health.IsDead || handled.Contains(original.EntityId) || fallen.Contains(original.EntityId)) continue;
                if (!projection.Damage.Characters.TryGetValue(original.EntityId, out var current) || !current.Health.IsDead) continue;
                var before = CommandDeathSettlement.PreDeathFact(projection.Input, projection.Events, original.EntityId);
                // 사망 특성 기여는 지금 특성 효과 규칙만 낸다. 특성이 없으면 바인딩을 만들지 않는다(병과 규칙이 사망 기여를 내게 되면 이 거름을 걷는다).
                if (before.Stats.TraitStatuses.Length == 0)
                {
                    handled.Add(original.EntityId);
                    continue;
                }
                var profile = CombatCommandCalculator.RequiredProfile(projection.Input, before, projection.ProfileReads);
                var evaluation = engine.EvaluateDeathTraits(CombatCommandEffects.Capture(before, profile));
                if (!evaluation.HasContribution)
                {
                    handled.Add(original.EntityId);
                    continue;
                }
                CellCoord? cell = projection.DeathAnchors.TryGetValue(original.EntityId, out var anchor) ? anchor : before.Anchor;
                if (!cell.HasValue)
                {
                    handled.Add(original.EntityId);
                    continue;
                }
                round.Add(new DeathTraitTrigger(original.EntityId, before, cell.Value, evaluation));
            }
            return round;
        }

        /// <summary>
        /// 사망 칸 주변 8칸의 살아 있는 캐릭터·구조물에게 폭발 피해를 준다. 화약통 폭발과 같은 환경 피해 입구를 써서
        /// 방어 없이 엄폐·피해 감소·보호막이 적용되고, 장애물이면 장애물 피해와 화약통 연쇄가 이어진다. 귀속은 자폭한 유닛이다.
        /// </summary>
        private static IEnumerable<string> ExplodeSteps(MovementProjection projection, DeathTraitTrigger death)
        {
            var area = Surrounding(projection, death.Cell);
            int amount = SelfDestructTraitRule.Damage(CombatEffectiveStatsCalculator.AttackPower(death.BeforeDeath),
                death.Evaluation.SelfDestructPercent);
            var origin = new CombatDamageOrigin(death.Cell, death.UnitId, projection.NextAttackGroup());
            var characters = projection.Damage.Characters.Values.Where(c => c.Area == CharacterRuntimeArea.Field && !c.Health.IsDead &&
                c.OccupiedCells.Any(area.Contains)).Select(c => c.EntityId);
            var structures = projection.CurrentStructures.Where(s => !s.Health.IsDead && s.OccupiedCells.Any(area.Contains))
                .Select(s => s.EntityId);
            var targets = characters.Concat(structures).Distinct().OrderBy(id => id).ToArray();
            int activationIndex = projection.Events.Count;
            var affected = new List<EntityId>();
            foreach (var targetId in targets)
            {
                var characterBefore = projection.Damage.Characters.GetValueOrDefault(targetId);
                var structureBefore = characterBefore == null ? projection.Structure(targetId) : null;
                foreach (var phase in projection.ChangeHealthSteps(new MovementHealthRequest(targetId, amount, origin), false,
                    effectOrigin: death.Evaluation.SelfDestructOrigin))
                    yield return phase;
                bool changed = characterBefore != null
                    ? !characterBefore.SameValues(projection.Character(targetId))
                    : structureBefore != null && StructureChanged(structureBefore, projection.Structure(targetId));
                if (changed) affected.Add(targetId);
            }
            // 폭발은 맞은 대상이 없어도 기록한다(연출). 대상 목록에는 실제로 바뀐 개체만 싣는다.
            var events = new List<CombatEvent>(1);
            CombatCommandEvents.Trait(events, death.UnitId, TraitKey.SelfDestruct, TraitActivationKind.SelfDestructExploded, amount,
                affected, area, origin, selectedCell: death.Cell);
            projection.Events.Insert(activationIndex, events[0]);
        }

        /// <summary>장애물 체력은 호환용 0/1이라 파괴하지 않은 타격을 놓친다. 장애물은 남은 타격 수로 비교한다(검토 OB-10).</summary>
        private static bool StructureChanged(StructureCombatFacts before, StructureCombatFacts after) =>
            before.ObstacleDurability != null
                ? !before.ObstacleDurability.Equals(after.ObstacleDurability)
                : after.Health.CurrentHealth != before.Health.CurrentHealth;

        private static List<CellCoord> Surrounding(MovementProjection projection, CellCoord center)
        {
            var area = new List<CellCoord>(8);
            CombatHazardAreas.SurroundingCells(center, projection.Input.State.BoardWidth, projection.Input.State.BoardHeight, area);
            return area;
        }

        private static void ApplyFear(MovementProjection projection, DeathTraitTrigger death)
        {
            var area = Surrounding(projection, death.Cell);
            var targets = projection.Damage.Characters.Values.Where(c => c.Area == CharacterRuntimeArea.Field && !c.Health.IsDead &&
                c.OccupiedCells.Any(area.Contains)).OrderBy(c => c.EntityId).Select(c => c.EntityId).ToArray();
            var origin = death.Evaluation.FearOrigin.Value;
            int activationIndex = projection.Events.Count;
            var affected = new List<EntityId>();
            foreach (var targetId in targets)
            {
                var target = projection.Character(targetId);
                int duration = ActionRestrictionTiming.Duration(FearTraitRule.BaseAffectedSideTurns, target.Owner, projection.PendingEndCleanupSide);
                bool disarmed = projection.ApplyStatus(targetId, new ActionRestrictionStatus(StatusEffectKey.Disarm, origin, duration));
                bool bound = projection.ApplyStatus(targetId, new ActionRestrictionStatus(StatusEffectKey.Bind, origin, duration));
                if (disarmed || bound) affected.Add(targetId);
            }
            // 공포가 실제로 건 대상만 발동 사건에 싣는다. 더 긴 제한이 이미 있어 아무것도 바뀌지 않았으면 발동 사건을 내지 않는다.
            if (affected.Count == 0) return;
            var events = new List<CombatEvent>(1);
            CombatCommandEvents.Trait(events, death.UnitId, TraitKey.Fear, TraitActivationKind.StatusApplied, affected.Count,
                affected, new[] { death.Cell });
            projection.Events.Insert(activationIndex, events[0]);
        }

        private sealed class DeathTraitTrigger
        {
            internal EntityId UnitId { get; }
            internal CharacterCombatFacts BeforeDeath { get; }
            internal CellCoord Cell { get; }
            internal EffectDeathTraitEvaluation Evaluation { get; }

            internal DeathTraitTrigger(EntityId unitId, CharacterCombatFacts beforeDeath, CellCoord cell, EffectDeathTraitEvaluation evaluation)
            {
                UnitId = unitId;
                BeforeDeath = beforeDeath;
                Cell = cell;
                Evaluation = evaluation;
            }
        }
    }
}
