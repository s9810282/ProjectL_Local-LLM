using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Evaluations;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Movement;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>
    /// 작렬 같은 넉백 변형의 부딪힘을 처리한다(설계 2026-11-10 CI2·CI3·CI6). 넉백 이동은 공통 넉백 계산기가 끝낸 뒤 부른다.
    /// 무엇이 막았는지는 강제 이동 진입 판정(<see cref="AttackForcedMovement.FindBlocker"/>), 피해 식은
    /// <see cref="CrashImpactCollisionRules"/>, 보호막·받는 피해 감소·사망은 공통 피해 입구가 맡는다.
    /// </summary>
    internal static class CrashImpactCollision
    {
        /// <summary>
        /// 막혀 멈춘 캐릭터 넉백이고 부딪힘 값이 있으면 부딪힘 사실을, 아니면 null을 돌려준다.
        /// 완주·중단(낙하·토네이도·전투 종료)·면역은 부딪힘이 아니다. 경로가 막힌 직후라 점유는 진입 판정 때와 같다.
        /// </summary>
        internal static AttackKnockbackCollision Find(CommandAttackPlan plan, CommandAttackProjection world, MovementProjection arrival,
            EffectKnockbackEvaluation knockback, EntityId flyerId, CellCoord start, CellCoord step, ForcedDisplacementPath path)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (arrival == null) throw new ArgumentNullException(nameof(arrival));
            if (knockback == null) throw new ArgumentNullException(nameof(knockback));
            if (path == null) throw new ArgumentNullException(nameof(path));
            if (!knockback.Collision.HasValue || path.Stop != ForcedDisplacementStop.Blocked) return null;
            var from = path.Steps.Count == 0 ? start : path.Steps[path.Steps.Count - 1];
            var blocked = ForcedDisplacementGeometry.ResolveDestinationAnchor(from, step, 1);
            var blocker = AttackForcedMovement.FindBlocker(plan.Input, arrival.Environment, arrival.Bindings,
                world.Characters.Values, world.Structures.Values, flyerId, from, blocked);
            if (blocker.IsNone)
                throw new InvalidOperationException("막혀 멈춘 넉백인데 다음 칸을 막은 것이 없습니다.");
            return new AttackKnockbackCollision(blocker.Kind, blocked, blocker.EntityId);
        }

        /// <summary>
        /// 부딪힘 발동 사건을 남기고, 날아간 대상 → (캐릭터에 부딪혔으면) 부딪힌 유닛 순서로 부딪힘 피해를 준다.
        /// 피해는 환경 피해 입구를 지나며 근원은 막은 칸·공격자·새 공격 그룹이다. 도중에 전투가 끝나면 남은 피해를 멈춘다.
        /// </summary>
        internal static IEnumerable<string> ResolveSteps(CommandAttackPlan plan, CommandAttackProjection world, CommandAttackLedger ledger,
            MovementProjection arrival, EffectKnockbackEvaluation knockback, EntityId flyerId, AttackKnockbackCollision collision)
        {
            if (collision == null) yield break;
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (ledger == null) throw new ArgumentNullException(nameof(ledger));
            if (arrival == null) throw new ArgumentNullException(nameof(arrival));
            var settings = knockback?.Collision ?? throw new InvalidOperationException("부딪힘 값이 없는 넉백에는 부딪힘 피해가 없습니다.");
            var source = knockback.CollisionOrigin.Value;
            var flyer = world.Characters[flyerId];
            int baseDamage = CrashImpactCollisionRules.BaseDamage(flyer.Health.MaxHealth,
                CrashImpactCollisionRules.PercentFor(settings, collision.Kind));
            var origin = new CombatDamageOrigin(collision.Cell, plan.Actor.EntityId, world.AttackGroups.Next());
            var recipients = collision.Kind == AttackKnockbackCollisionKind.Character
                ? new[] { flyerId, collision.EntityId.Value }
                : new[] { flyerId };
            // View 신호: 부딪힘 순간을 피해 사건보다 앞에 둔다. 칸은 막은 칸, 선택 칸은 날아간 대상이 멈춘 칸이다.
            var occurrence = CombatCommandEvents.Trait(world.Events, new EntityId(source.OwnerInstanceId), (TraitKey)source.DefinitionKey,
                TraitActivationKind.CrashImpactCollided, baseDamage, recipients, new[] { collision.Cell }, origin,
                selectedCell: flyer.Anchor);
            yield return "CrashImpactSignal";
            foreach (var id in recipients)
            {
                if (arrival.Terminal) yield break;
                var victim = world.Characters[id];
                if (victim.Health.IsDead) continue;
                int defense = CombatEffectiveStatsCalculator.PhysicalDefense(plan.Input, victim, world.Characters.Values.ToArray());
                int amount = CrashImpactCollisionRules.AfterDefense(baseDamage, defense);
                if (amount == 0) continue;
                RoarHitCounter.Record(world.Characters, id, world.HitCause, plan.Input.State.InCombat);
                int remaining = amount;
                foreach (var phase in world.Damage.ApplySteps(id, CombatDamageKind.EnvironmentDamage, origin, amount,
                    value => remaining = value.RemainingDamage, heldDefenseGroups: world.AttackGroups, effectOrigin: source, contextEvent: occurrence))
                    yield return phase;
                ledger.AddDamage(id, remaining);
                if (id != flyerId) ledger.AddCollisionVictim(id);
                if (world.Characters[id].Health.IsDead) world.DeathAnchors[id] = victim.Anchor.Value;
                yield return "CrashImpactDamage";
            }
        }
    }
}
