using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Rewards
{
    /// <summary>확정된 새 사망과 기존 귀속 규칙을 골드·경험치가 함께 사용한다.</summary>
    internal static class ConfirmedKillCalculator
    {
        internal static IReadOnlyList<CreditedKill> Collect(CombatCommandInput input,
            IReadOnlyDictionary<EntityId, CharacterCombatFacts> projected, IReadOnlyList<CombatEvent> events,
            CombatCommandState sourceState)
        {
            var kills = projected.Values.Where(c => c.Owner == Owner.Enemy && c.Health.IsDead).ToArray();
            if (kills.Length == 0)
                return Array.Empty<CreditedKill>();
            var before = input.Characters.ToDictionary(c => c.EntityId);
            kills = kills.Where(c => !before.TryGetValue(c.EntityId, out var previous) || !previous.Health.IsDead)
                .OrderBy(c => c.EntityId).ToArray();
            if (kills.Length == 0)
                return Array.Empty<CreditedKill>();

            var deaths = events.OfType<CombatCharacterEvent>()
                .Where(e => e.Kind == CombatEventKind.Died && !e.Expected.Health.IsDead).ToArray();
            var reflections = CombatDamageCredit.Reflections(events);
            var creditedKills = new List<CreditedKill>();
            foreach (var victim in kills)
            {
                var victimDeaths = deaths.Where(e => e.Final.EntityId == victim.EntityId).ToArray();
                // 사망 하나에 사망 사건이 정확히 하나다. 다르면 새 사망 경로가 사건 규약을 어긴 것이다(설계 §3.2).
                if (victimDeaths.Length != 1)
                    throw new InvalidOperationException(
                        $"처치된 적 {victim.EntityId}의 사망 사건이 {victimDeaths.Length}개입니다. 사망 경로는 사망 사건을 정확히 하나 남겨야 합니다.");
                EntityId? recipient = CombatDamageCredit.Recipient(input, projected, victimDeaths[0].DamageOrigins, reflections);
                if (!recipient.HasValue)
                    continue;

                var profile = CombatCommandCalculator.RequiredProfile(input, victim);
                var reward = profile?.Definition.KillReward ?? throw new InvalidOperationException(
                    $"처치된 적 {victim.EntityId}의 보상 정의가 없습니다.");
                if (!reward.IsEligible) continue;
                bool reinforced = sourceState.Reinforcements.Contains(victim.EntityId);
                creditedKills.Add(new CreditedKill(victim.EntityId, recipient.Value,
                    projected[recipient.Value].OwnedId.Value, reinforced ? 0 : reward.Gold, reinforced ? 0 : reward.Experience));
            }
            return creditedKills.AsReadOnly();
        }
    }

    internal readonly struct CreditedKill
    {
        internal EntityId VictimId { get; }
        internal EntityId RecipientId { get; }
        internal CharacterInstanceId OwnedId { get; }
        internal int Gold { get; }
        internal int Experience { get; }

        internal CreditedKill(EntityId victimId, EntityId recipientId, CharacterInstanceId ownedId, int gold, int experience)
        {
            VictimId = victimId;
            RecipientId = recipientId;
            OwnedId = ownedId;
            Gold = gold;
            Experience = experience;
        }
    }
}
