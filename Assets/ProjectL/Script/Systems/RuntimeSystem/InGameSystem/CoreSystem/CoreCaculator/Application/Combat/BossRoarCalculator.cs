using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>
    /// 원래 명령 정산 뒤의 보스 포효 반응이다(D8, D11, G6). 대기 여부는 저장하지 않고 현재 사실에서 계산한다.
    /// 포효는 AP·현재 행동자·밤 게이지·완료 턴 수를 바꾸지 않으며, 결과는 <see cref="CombatRoarEvent"/>로만 낸다.
    /// </summary>
    internal static class BossRoarCalculator
    {
        /// <summary>캐릭터 정의의 포효다. 없으면 null이다.</summary>
        internal static RoarTraitSpec Roar(CombatCommandInput input, CharacterCombatFacts character) =>
            CombatCommandCalculator.RequiredProfile(input, character)?.Definition.Traits.ToArray()
                .OfType<RoarTraitSpec>().SingleOrDefault();

        /// <summary>
        /// 대기 포효 보스(D11, G6-4·G6-6): 밤 + 전투 국면 + 전투 미종료 + 살아 있는 Field 캐릭터 + 포효 카운터 &gt;= 임계값. EntityId 순이다.
        /// </summary>
        internal static IReadOnlyList<EntityId> PendingBosses(CombatCommandInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (!input.State.IsNight || input.State.Phase == CombatFlowPhase.Finished ||
                input.State.Turn.StagePhase != StagePhase.Combat) return Array.Empty<EntityId>();
            return input.Characters.Where(c => !c.Health.IsDead && c.Area == CharacterRuntimeArea.Field && !CombatActionAdmission.IsAttackRestricted(c))
                .Where(c => Roar(input, c) is RoarTraitSpec roar && RoarHitCounter.HitCount(c) >= roar.HitThreshold)
                .Select(c => c.EntityId).OrderBy(id => id).ToArray();
        }

        /// <summary>
        /// 대기 포효를 원래 명령 결과 뒤에 붙인다. 거절·종료면 그대로 돌려준다. 한 호출 안에서 보스당 최대 1회 포효하고(G6-3),
        /// 남은 대기는 다음 반응 경계가 처리한다. 반응 단계의 Attack은 비어 있으므로 Merge 결과의 Attack은 원래 명령 것이다(D8).
        /// </summary>
        internal static CombatCommandTransition ApplyPendingReactions(CombatCommandTransition transition,
            IReadOnlyList<IMovementTileRule> rules)
        {
            if (transition == null) throw new ArgumentNullException(nameof(transition));
            if (!transition.Accepted || transition.FinalState.Phase == CombatFlowPhase.Finished) return transition;
            // 포효 정의가 없는 전투는 투영을 만들지 않고 그대로 돌려준다.
            if (!transition.Input.Profiles.Any(p => p.Definition.Traits.ToArray().OfType<RoarTraitSpec>().Any())) return transition;
            var steps = new List<CombatCommandTransition> { transition };
            var current = CombatCommandComposition.Project(transition);
            var roared = transition.Events.OfType<CombatRoarEvent>().Select(e => e.BossId).ToHashSet();
            while (current.State.Phase != CombatFlowPhase.Finished)
            {
                var next = PendingBosses(current).FirstOrDefault(id => !roared.Contains(id));
                if (next.IsEmpty) break;
                roared.Add(next);
                var roar = TryRoar(current, next, rules);
                steps.Add(roar);
                current = CombatCommandComposition.Project(roar);
            }
            return steps.Count == 1 ? transition : CombatCommandComposition.Merge(transition.Input, steps);
        }

        /// <summary>
        /// 대기 중인 보스 하나가 포효한다. 피해 계산 전에 카운터를 0으로 되돌리고(G6-2, G6-5),
        /// 보스 프로필의 전체 지정(All)으로 반응 공격을 한다. 넉백은 보스의 Knockback Trait 기여를 쓴다(D4).
        /// </summary>
        internal static CombatCommandTransition TryRoar(CombatCommandInput input, EntityId bossId, IReadOnlyList<IMovementTileRule> rules)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (!PendingBosses(input).Contains(bossId)) throw new InvalidOperationException("포효 대기 중인 보스가 아닙니다.");
            var boss = input.Characters.Single(c => c.EntityId == bossId);
            var profile = CombatCommandCalculator.RequiredProfile(input, boss);
            if (profile.Method.ChoiceMode != TargetingChoiceMode.All)
                throw new InvalidOperationException("포효 보스의 공격 프로필은 전체 지정(All)이어야 합니다.");

            // 1단계: 포효 카운터만 초기화한다. 일반 행동 사용과 공용 자금은 유지한다.
            var projected = input.Characters.ToDictionary(c => c.EntityId);
            projected[bossId] = RoarHitCounter.Reset(boss);
            CombatCommandTransition Reset(params CombatEvent[] events) => CombatCommandCalculator.Finish(input,
                new Dictionary<EntityId, CharacterCombatFacts>(projected), input.SoulCounts.ToDictionary(p => p.Key, p => p.Value),
                input.Souls, input.State.ThornFields.ToDictionary(f => f.Cell), input.State.Structures.ToArray(), new List<long>(),
                new Dictionary<EntityId, CellCoord>(), new SoulEffectCalculator(input.Artifacts), null, input.State,
                events: events.ToList());
            var reset = Reset();
            var afterReset = CombatCommandComposition.Project(reset);

            // 2단계: 반응 공격. 범위 안에 대상이 없으면 포효 사실만 남긴다.
            var attack = CombatAttackCalculator.Reaction(afterReset, rules,
                afterReset.Characters.Single(c => c.EntityId == bossId), AttackAim.ForAll(), out var emptyArea);
            // 대상이 없어도 포효 범위 칸은 표시용으로 남긴다(View 요청, 2026-10-15 사용자 지시).
            var occurrence = attack == null ? new CombatRoarEvent(bossId, null, emptyArea)
                : attack.Events.OfType<CombatRoarEvent>().Single(e => e.BossId == bossId);
            var ordered = new List<CombatEvent>();
            var origin = attack == null ? null : CombatCommandEvents.FindOrigin(attack.Events, bossId);
            var targets = occurrence.Attack.HasValue ? occurrence.Attack.Value.Hits.ToArray().Select(h => h.TargetId).Distinct().ToArray()
                : Array.Empty<EntityId>();
            CombatCommandEvents.Trait(ordered, bossId, TraitKey.Roar, TraitActivationKind.RoarTriggered,
                occurrence.AreaCells.Count, targets, occurrence.AreaCells, origin);
            CombatCommandEvents.TraitState(ordered, boss, projected[bossId], origin);
            ordered.AddRange(reset.Events);
            if (attack == null) ordered.Add(occurrence);
            var recorded = CombatCommandComposition.WithEvents(reset, ordered);
            return attack == null ? recorded : CombatCommandComposition.Merge(input, new[] { recorded, attack });
        }
    }
}
