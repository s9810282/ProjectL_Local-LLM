using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>
    /// 처치 연쇄 공격(난사)을 정산한다. 이 공격 명령으로 처치된 적 1명당 맵 전체의 랜덤한 적 캐릭터 하나를 추가 공격하고,
    /// 추가 공격으로 처치하면 1회가 더 생긴다. 기본 공격 결과(AttackResult)는 바꾸지 않는다.
    /// 영혼·사망·승패 정산은 하지 않으며 호출자의 최종 게시(Finish)가 투영 상태로 한 번 처리한다.
    /// </summary>
    internal static class KillChainAttackCalculator
    {
        /// <summary>
        /// 강제 도착 처리가 끝난 투영에서 추가 공격을 진행한다. 미리보기에서는 호출하지 않는다.
        /// 시작 전과 매 타격 전에 공격자 생존·필드 존재·전투 종료·후보 유무를 확인하고, 하나라도 어긋나면 남은 횟수를 취소한다.
        /// 타격마다 발동(CombatEffectsEvent) → 보유 방어 소모 → 체력 사건 순서로 기록한다.
        /// </summary>
        /// <param name="damage">기본 공격과 같은 공통 피해(특성 가산·영혼 공격력 포함)다.</param>
        /// <param name="projection">강제 도착을 처리한 투영이다. 승패 판정·전투 난수·현재 환경을 제공하며 사건 목록을 공유한다.</param>
        /// <param name="projected">투영과 같은 캐릭터 사전이다. 추가 공격의 체력·보유 방어 변경을 여기에 반영한다.</param>
        internal static void Resolve(CombatCommandInput input, CombatEffectEngine engine, TargetDamagePipeline pipeline,
            IReadOnlyList<EffectRuleBinding> bindings, int damage, MovementProjection projection,
            Dictionary<EntityId, CharacterCombatFacts> projected, CommandAttackGroups attackGroups,
            Dictionary<EntityId, CellCoord> deathAnchors)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (engine == null) throw new ArgumentNullException(nameof(engine));
            if (pipeline == null) throw new ArgumentNullException(nameof(pipeline));
            if (bindings == null) throw new ArgumentNullException(nameof(bindings));
            if (projection == null) throw new ArgumentNullException(nameof(projection));
            if (projected == null) throw new ArgumentNullException(nameof(projected));
            if (attackGroups == null) throw new ArgumentNullException(nameof(attackGroups));
            if (deathAnchors == null) throw new ArgumentNullException(nameof(deathAnchors));
            if (damage < 0) throw new ArgumentOutOfRangeException(nameof(damage));
            if (!engine.EvaluateKillChainAttack(bindings).HasContribution) return;

            var hostility = new HostilityPolicy();
            Owner owner = projected[input.ActorId].Owner;
            int remaining = input.Characters.Count(c => !c.Health.IsDead && hostility.AreHostile(owner, c.Owner) &&
                projected[c.EntityId].Health.IsDead);
            var events = projection.Events;

            while (remaining > 0)
            {
                var actor = projected[input.ActorId];
                if (actor.Health.IsDead || actor.Area != CharacterRuntimeArea.Field || projection.Terminal) return;
                // 처치 연쇄는 벽과 무관한 적군 전용 추가 공격이다. 최초 공격의 벽 판정을 다시 적용하지 않는다.
                var candidates = projected.Values.Where(c => c.Area == CharacterRuntimeArea.Field && !c.Health.IsDead &&
                    hostility.AreHostile(actor.Owner, c.Owner)).OrderBy(c => c.EntityId).ToArray();
                if (candidates.Length == 0) return;

                var victim = candidates[projection.ChooseRandom(candidates.Length)];
                remaining--;
                var origin = new CombatDamageOrigin(actor.Anchor, actor.EntityId, attackGroups.Next());
                int dealt = CombatDamageCalculator.Attack(input, engine, pipeline, actor, victim, damage,
                    AttackTargetRole.Designated, 100, actor.Anchor, null, projection.Environment).FinalDamage;

                // 최종 피해는 보유 방어 뒤에 정해지므로 발동 사건은 방어 사건 앞 자리에 끼워 넣는다.
                int activationIndex = events.Count;
                dealt = CombatDamageCalculator.ApplyHeldDefense(attackGroups, projected, victim.EntityId, dealt, origin, events);
                events.Insert(activationIndex, new CombatEffectsEvent(
                    new[]
                    {
                        new TraitActivationSnapshot(actor.EntityId, victim.EntityId, TraitKey.KillChainAttack,
                            TraitActivationKind.KillChainAttack, dealt),
                    },
                    Array.Empty<CategoryActivationSnapshot>()));

                var before = projected[victim.EntityId];
                var after = CombatCommandCalculator.Hurt(before, dealt);
                projected[victim.EntityId] = after;
                CombatCommandEvents.Health(events, before, after, new[] { origin });
                if (after.Health.IsDead)
                {
                    deathAnchors[victim.EntityId] = before.Anchor.Value;
                    remaining++;
                }
            }
        }
    }
}
