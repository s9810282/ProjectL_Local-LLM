using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Rewards;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Experience
{
    /// <summary>
    /// 명령 하나의 스테이지 기록 정산이다. 피해 경계가 모은 피해 항목, 행동자의 자발 경로 칸 수, 귀속 처치를
    /// 보유 아군의 전투 사실에 한 번 더한다(유닛별 스테이지 기록 설계 D3~D5). Finish가 처치 수집 직후, 경험치 정산 전에 부른다.
    /// 피해량·사망·이동 판정은 하지 않고 이미 만들어진 값만 읽는다. Buffer를 모른다.
    /// </summary>
    internal static class CombatStageRecordSettlement
    {
        /// <param name="tally">이번 명령의 피해 항목·이동 칸 수다. 피해와 이동이 없는 정산(포효 초기화 단계)은 null이다.</param>
        internal static void Record(CombatCommandInput input, Dictionary<EntityId, CharacterCombatFacts> projected,
            IReadOnlyList<CombatEvent> events, IReadOnlyList<CreditedKill> kills, CombatStageTally tally)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (projected == null) throw new ArgumentNullException(nameof(projected));
            if (events == null) throw new ArgumentNullException(nameof(events));
            if (kills == null) throw new ArgumentNullException(nameof(kills));

            var added = new Dictionary<EntityId, CharacterStageRecord>();
            if (tally != null)
            {
                IReadOnlyList<ThornHeartReflectionContext> reflections = null;
                foreach (var entry in tally.Damage)
                {
                    if (!projected.TryGetValue(entry.RecipientId, out var recipient)) continue;
                    if (IsOwnedAlly(recipient))
                        Add(added, entry.RecipientId, damageTaken: entry.HealthLoss);
                    // 넣은 피해는 적이 잃은 HP만 센다(설계 Q-S2). 귀속은 처치와 같은 규칙이다(D4).
                    if (recipient.Owner != Owner.Enemy) continue;
                    reflections ??= CombatDamageCredit.Reflections(events);
                    if (CombatDamageCredit.Recipient(input, projected, entry.Origins, reflections) is EntityId dealer)
                        Add(added, dealer, damageDealt: entry.HealthLoss);
                }
                if (tally.ActorDistance > 0 && projected.TryGetValue(tally.ActorId, out var actor) && IsOwnedAlly(actor))
                    Add(added, tally.ActorId, distanceMoved: tally.ActorDistance);
            }
            foreach (var kill in kills)
                Add(added, kill.RecipientId, kills: 1);

            // 개체마다 한 번만 사실을 다시 만든다(계획 TP-4). 증분 묶음도 기록 계약이라 넘침은 예외다.
            foreach (var pair in added.OrderBy(p => p.Key))
            {
                var facts = projected[pair.Key];
                var increment = pair.Value;
                projected[pair.Key] = facts.With(stageRecord: facts.StageRecord.Add(increment.DamageTaken,
                    increment.DamageDealt, increment.DistanceMoved, increment.Kills));
            }
        }

        private static bool IsOwnedAlly(CharacterCombatFacts facts) => facts.Owner == Owner.Player && facts.OwnedId.HasValue;

        private static void Add(Dictionary<EntityId, CharacterStageRecord> added, EntityId id,
            int damageTaken = 0, int damageDealt = 0, int distanceMoved = 0, int kills = 0)
        {
            var current = added.TryGetValue(id, out var record) ? record : CharacterStageRecord.Empty;
            added[id] = current.Add(damageTaken, damageDealt, distanceMoved, kills);
        }
    }

    /// <summary>명령 하나 동안 모은 스테이지 기록 재료다. 피해 항목(적용 순서)과 행동자의 자발 경로 칸 수를 가진다.</summary>
    internal sealed class CombatStageTally
    {
        internal IReadOnlyList<CombatStageDamageEntry> Damage { get; }
        internal EntityId ActorId { get; }
        internal int ActorDistance { get; }

        internal CombatStageTally(IReadOnlyList<CombatStageDamageEntry> damage, EntityId actorId, int actorDistance)
        {
            if (actorDistance < 0) throw new ArgumentOutOfRangeException(nameof(actorDistance));
            Damage = damage ?? throw new ArgumentNullException(nameof(damage));
            ActorId = actorId;
            ActorDistance = actorDistance;
        }
    }
}
