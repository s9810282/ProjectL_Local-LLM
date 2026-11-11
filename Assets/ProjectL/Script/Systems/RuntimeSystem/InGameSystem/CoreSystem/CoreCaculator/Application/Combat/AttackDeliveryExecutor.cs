using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>일반·반응의 전달 순서를 소유한다. 명령 수락과 최종 게시는 호출자가 담당한다.</summary>
    internal static class AttackDeliveryExecutor
    {
        /// <summary>
        /// 행동 사용 정산은 가시 적용 뒤, 피격 체력 고정 전에 동기 호출한다. 반사 사망 때도 정산은 유지한다.
        /// </summary>
        internal static MovementProjection Execute(CommandAttackPlan plan, CommandAttackProjection world,
            CommandAttackLedger ledger, ISet<EntityId> deliveredVictims,
            MovementProjection arrival = null, Action settleActionUse = null, bool deliverDirectDamage = true)
        {
            using var sample = CoreProfiling.Sample(CoreProfileSection.AttackDelivery);
            MovementProjection result = null;
            foreach (var phase in ExecuteSteps(plan, world, ledger, deliveredVictims, value => result = value,
                arrival, settleActionUse, deliverDirectDamage)) { }
            return result;
        }

        internal static IEnumerable<string> ExecuteSteps(CommandAttackPlan plan, CommandAttackProjection world,
            CommandAttackLedger ledger, ISet<EntityId> deliveredVictims, Action<MovementProjection> completed,
            MovementProjection arrival = null, Action settleActionUse = null, bool deliverDirectDamage = true)
        {
            ledger.BeginScope(plan, world);
            var reflection = deliverDirectDamage && plan.DamageKind == CombatDamageKind.OrdinaryAttack
                ? new ThornHeartAttackScope(world.Souls.CompiledLoadout,
                    actualOrdinaryAttack: !plan.PublicPreview, inCombat: plan.Input.State.InCombat)
                : null;
            var aliveBeforeDirect = arrival == null ? null : world.Characters.Values.Where(c => !c.Health.IsDead)
                .Select(c => c.EntityId).ToArray();
            bool reflectedDeath = false;
            if (deliverDirectDamage)
                foreach (var phase in AttackDirectDamageCalculator.ApplySteps(plan, world, ledger, deliveredVictims, reflection,
                    value => reflectedDeath = value))
                    yield return phase;
            if (!deliverDirectDamage) ledger.CompleteDirectStage();
            // 사전 이동 때문에 이미 만든 투영에서도 직접 타격 사망의 영혼은 기존처럼 명령 완료 때 정산합니다.
            if (aliveBeforeDirect != null)
                arrival.DeferDeathsUntilCompletion(aliveBeforeDirect.Where(id => world.Characters[id].Health.IsDead));
            arrival ??= world.CreateArrivalProjection(plan);
            yield return "ArrivalProjection";
            var forcedArrivals = new List<ResolvedAttackDisplacement>();
            if (!reflectedDeath && deliverDirectDamage)
                foreach (var phase in AttackKnockbackCalculator.ResolveSteps(plan, world, ledger, arrival, forcedArrivals))
                    yield return phase;
            yield return "Knockback";
            if (!reflectedDeath && deliverDirectDamage) ThornFieldApplicationCalculator.Apply(plan, world, ledger);
            yield return "ThornField";
            settleActionUse?.Invoke();
            yield return "ActionUse";
            ledger.CaptureHitHealth(world);
            yield return "HitHealth";
            if (!reflectedDeath && deliverDirectDamage)
                foreach (var phase in AttackKnockbackCalculator.ResolveArrivalsSteps(plan, world, arrival, forcedArrivals))
                    yield return phase;
            yield return "ForcedArrivals";
            LavaCannonApplication.Apply(plan, world, plan.CapturedEnemyIds);
            if (!plan.PublicPreview && !reflectedDeath && deliverDirectDamage)
            {
                KillChainAttackCalculator.Resolve(plan.Input, plan.Actor.EntityId, plan.Engine, plan.Pipeline, plan.Bindings,
                    plan.CommonDamage, arrival, world.Characters, world.AttackGroups, world.DeathAnchors,
                    world.HitCause, deliveredVictims, plan, world);
            }
            yield return "ActualKillChain";
            using var observation = arrival.BeginCurrentInput();
            while (!observation.IsComplete)
            {
                observation.Step();
                yield return "ScopeObservation";
            }
            HunterApplication.FinishAction(plan.TraitScope, observation.Result, world.Characters, arrival.ChooseRandom, world.Events, !arrival.Terminal);
            yield return "FinishScope";
            completed(arrival);
        }
    }
}
