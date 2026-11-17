using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.ThornBombs;
using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>일반·반응·스킬 공격의 전달과 공격 완료 정산 순서를 소유한다. 명령 수락과 최종 게시는 호출자가 담당한다.</summary>
    internal static class AttackDeliveryExecutor
    {
        /// <summary>
        /// 행동 사용 정산은 넉백 뒤, 피격 체력 고정 전에 동기 호출한다. 반사 사망 때도 정산은 유지한다.
        /// </summary>
        internal static MovementProjection Execute(CommandAttackPlan plan, CommandAttackProjection world,
            CommandAttackLedger ledger, ISet<EntityId> deliveredVictims,
            MovementProjection arrival = null, Action settleActionUse = null, bool attackStarted = true,
            Action<MovementProjection> beforeKillChain = null)
        {
            using var sample = CoreProfiling.Sample(CoreProfileSection.AttackDelivery);
            MovementProjection result = null;
            foreach (var phase in ExecuteSteps(plan, world, ledger, deliveredVictims, value => result = value,
                arrival, settleActionUse, attackStarted, beforeKillChain)) { }
            return result;
        }

        internal static IEnumerable<string> ExecuteSteps(CommandAttackPlan plan, CommandAttackProjection world,
            CommandAttackLedger ledger, ISet<EntityId> deliveredVictims, Action<MovementProjection> completed,
            MovementProjection arrival = null, Action settleActionUse = null, bool attackStarted = true,
            Action<MovementProjection> beforeKillChain = null)
        {
            ledger.BeginScope(plan, world);
            var bombAttachment = attackStarted ? ThornBombAttackApplication.CaptureAttachment(plan, world) : null;
            var reflection = attackStarted && (plan.DamageKind == CombatDamageKind.OrdinaryAttack || plan.DamageKind == CombatDamageKind.PlayerSkillAttack)
                ? new ThornHeartAttackScope(world.Souls.CompiledLoadout,
                    actualOrdinaryAttack: plan.CompleteConsequences, inCombat: plan.Input.State.InCombat,
                    triggeringAttackGroup: plan.AttackGroup)
                : null;
            var aliveBeforeDirect = arrival == null ? null : world.Characters.Values.Where(c => !c.Health.IsDead)
                .Select(c => c.EntityId).ToArray();
            bool reflectedDeath = false;
            if (attackStarted)
                foreach (var phase in AttackDirectDamageCalculator.ApplySteps(plan, world, ledger, deliveredVictims, reflection,
                    value => reflectedDeath = value))
                    yield return phase;
            if (!attackStarted) ledger.CompleteDirectStage();
            // 사전 이동 때문에 이미 만든 투영에서도 직접 타격 사망의 영혼은 기존처럼 명령 완료 때 정산합니다.
            if (aliveBeforeDirect != null)
                arrival.DeferDeathsUntilCompletion(aliveBeforeDirect.Where(id => world.Characters[id].Health.IsDead));
            arrival ??= world.CreateArrivalProjection(plan);
            yield return "ArrivalProjection";
            var forcedArrivals = new List<ResolvedAttackDisplacement>();
            if (!reflectedDeath && attackStarted)
                foreach (var phase in AttackKnockbackCalculator.ResolveSteps(plan, world, ledger, arrival, forcedArrivals))
                    yield return phase;
            yield return "Knockback";
            settleActionUse?.Invoke();
            yield return "ActionUse";
            ledger.CaptureHitHealth(world);
            yield return "HitHealth";
            if (!reflectedDeath && attackStarted)
                foreach (var phase in AttackKnockbackCalculator.ResolveArrivalsSteps(plan, world, arrival, forcedArrivals))
                    yield return phase;
            yield return "ForcedArrivals";
            foreach (var phase in ThornBombAttackApplication.CompleteAttachmentSteps(plan, arrival, bombAttachment, world.ThornBombs, plan.Rules))
                yield return phase;
            if (attackStarted)
            {
                IReadOnlyList<EntityId> capturedEnemies = plan.CapturedEnemyIds;
                if (plan.ProjectileTargetBudget.HasValue)
                {
                    // 투사체가 실제로 만난 원래 대상만 범위 효과를 받는다. 중단 지점 뒤 후보는 제외한다.
                    var encountered = ledger.HitIds.SelectMany(id => ledger.Contacts(id))
                        .Select(contact => contact.OriginalTargetId).ToHashSet();
                    capturedEnemies = capturedEnemies.Where(encountered.Contains).ToArray();
                }
                LavaCannonApplication.Apply(plan, world, capturedEnemies);
            }
            if (attackStarted) beforeKillChain?.Invoke(arrival);
            if (plan.CompleteConsequences && !reflectedDeath && attackStarted)
            {
                // 폭탄 연쇄로만 생긴 사망은 원래 공격의 처치로 세지 않는다. 초기 근거 식은 처치 연쇄 한 곳에 둔다.
                KillChainAttackCalculator.Resolve(plan.Input, plan.Actor.EntityId, plan.Engine, plan.Pipeline, plan.Bindings,
                    plan.CommonDamage, arrival, world.Characters, world.AttackGroups, world.DeathAnchors,
                    world.HitCause, deliveredVictims, plan, world,
                    initialKilledEnemyIds: plan.DamageKind == CombatDamageKind.PlayerSkillAttack ? ledger.DirectKilledEnemyIds : null,
                    excludedKilledEnemyIds: world.ThornBombs.KilledIds);
            }
            yield return "ActualKillChain";
            using var observation = arrival.BeginCurrentInput();
            while (!observation.IsComplete)
            {
                observation.Step();
                yield return "ScopeObservation";
            }
            if (attackStarted)
                HunterApplication.FinishAction(plan.TraitScope, observation.Result, world.Characters, arrival.ChooseRandom, world.Events, !arrival.Terminal);
            yield return "FinishScope";
            // 일반 행동 사용 여부와 무관하게 실제 공격마다 위협 부여와 공격 완료 수명을 정산한다.
            if (attackStarted)
                CombatDebuffApplication.CompleteAttack(plan.Input, plan.OriginalActor, deliveredVictims, world.Characters, world.Events,
                    new CombatDamageOrigin(plan.Actor.Anchor, plan.Actor.EntityId, plan.AttackGroup));
            yield return "CompleteDebuff";
            completed(arrival);
        }
    }
}
