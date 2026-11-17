using System;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Rewards;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>공통 경계가 이미 확정한 사망 사건의 특성 보상을 한 번씩 소비합니다.</summary>
    internal static class TraitKillSettlement
    {
        internal static void Apply(CombatCommandInput input, CombatDamageApplication damage, int eventStart)
        {
            var deaths = damage.Events.Skip(eventStart).OfType<CombatCharacterEvent>()
                .Where(e => e.Kind == CombatEventKind.Died && !e.Expected.Health.IsDead).ToArray();
            if (deaths.Length == 0) return;
            var reflections = CombatDamageCredit.Reflections(damage.Events);
            foreach (var death in deaths)
            {
                if (!damage.SettledTraitDeaths.Add(death.Final.EntityId)) continue;
                var credited = CombatDamageCredit.CharacterRecipient(input, damage.Characters, death.DamageOrigins, reflections);
                // 회복을 포함한 다른 처치 보상 전에 자격을 읽고 한 번 더합니다. 이후 체력 조정이 임계치를 넘으면 즉시 비웁니다.
                // 광폭화는 귀속 유닛 자신의 보너스라, 귀속 유닛이 죽어 있으면(예: 자폭으로 주변을 죽인 유닛) 더하지 않는다(적 전용 특성 설계 P7).
                // 사기 진작·피의 부름처럼 아군에게 주는 보상은 죽은 귀속 유닛도 그대로 낸다(반사로 서로 죽인 경우의 기존 동작).
                if (credited.HasValue && damage.Characters.TryGetValue(credited.Value, out var rampaging) && !rampaging.Health.IsDead &&
                    new HostilityPolicy().AreHostile(rampaging.Owner, death.Expected.Owner))
                {
                    int hunter = rampaging.OwnedId.HasValue ? damage.OwnedRecord(rampaging.OwnedId.Value).HunterAttackBonus : 0;
                    var rewarded = RampageApplication.AddKill(rampaging, death.Expected.Stats.AttackPower, hunter);
                    if (!ReferenceEquals(rewarded, rampaging))
                    {
                        damage.Characters[rampaging.EntityId] = rewarded;
                        var rewardOrigin = death.DamageOrigins.FirstOrDefault(o => o.CreditedCharacterId == credited) ?? death.DamageOrigins.FirstOrDefault();
                        Activation(damage, rampaging.EntityId, rampaging.EntityId, TraitKey.Rampage, TraitActivationKind.AttackGranted,
                            death.Expected.Stats.AttackPower, rewardOrigin);
                        CombatCommandEvents.TraitState(damage.Events, rampaging, rewarded, rewardOrigin);
                    }
                }
                HunterApplication.TargetDied(input, damage, death, credited);
                if (!credited.HasValue || !damage.Characters.TryGetValue(credited.Value, out var source) ||
                    !new HostilityPolicy().AreHostile(source.Owner, death.Expected.Owner)) continue;
                if (!source.Stats.TraitStatuses.ToArray().Any(t => t.Key == TraitKey.CallOfBlood || t.Key == TraitKey.MoraleBoost)) continue;
                var origin = death.DamageOrigins.FirstOrDefault(o => o.CreditedCharacterId == credited) ?? death.DamageOrigins.FirstOrDefault();
                var profile = CombatCommandCalculator.RequiredProfile(input, source, damage.ProfileReads);
                foreach (var binding in CombatCommandEffects.Capture(source, profile))
                {
                    var bound = new EffectRuleInput(binding.Origin, binding.ExpectedState);
                    if (binding.Rule is CallOfBloodEffectRule blood)
                    {
                        var before = damage.Characters[source.EntityId];
                        var after = CharacterSettlementCalculator.Recover(before, blood.Evaluate(bound, death.Expected.Health.MaxHealth), out int actual);
                        if (actual == 0) continue;
                        damage.Characters[source.EntityId] = after;
                        damage.RecordRecovery(source.EntityId, actual);
                        var occurrence = Activation(damage, source.EntityId, source.EntityId, TraitKey.CallOfBlood,
                            TraitActivationKind.HealthRecovered, actual, origin);
                        var evidence = CombatCommandEvents.Health(damage.Events, before, after);
                        var healthSource = new CombatHealthSource(CombatHealthOperationKind.EffectRecovery, binding.Origin, contextEvent: occurrence);
                        var step = CombatHealthRecording.Add(damage.HealthExecutions, source.EntityId, before.Health, after.Health,
                            healthSource, evidence, recovery: actual);
                        damage.HealthClaims.Add(new CombatHealthClaim(CombatHealthClaimKind.TraitRecovery, occurrence, 0, -1,
                            source.EntityId, new[] { step }));
                    }
                    else if (binding.Rule is MoraleBoostEffectRule morale)
                    {
                        var grant = morale.Evaluate(bound, death.Expected.Stats.AttackPower);
                        var recipients = damage.Characters.Values.Where(c => c.Owner == source.Owner &&
                            c.Area == CharacterRuntimeArea.Field && !c.Health.IsDead).OrderBy(c => c.EntityId).ToArray();
                        if (grant.Amount > 0 && recipients.Length > 0)
                            CombatCommandEvents.Trait(damage.Events, source.EntityId, TraitKey.MoraleBoost, TraitActivationKind.AttackGranted,
                                grant.Amount, recipients.Select(c => c.EntityId).ToArray(), origin: origin);
                        foreach (var recipient in recipients)
                        {
                            int hunter = recipient.OwnedId.HasValue ? damage.OwnedRecord(recipient.OwnedId.Value).HunterAttackBonus : 0;
                            damage.Characters[recipient.EntityId] = CombatEffectiveStatsCalculator.GrantTraitAttack(recipient, grant.ToStatus(), hunter);
                            CombatCommandEvents.TraitState(damage.Events, recipient, damage.Characters[recipient.EntityId], origin);
                        }
                    }
                }
            }
        }

        private static CombatEffectsEvent Activation(CombatDamageApplication damage, Contracts.Ids.EntityId source,
            Contracts.Ids.EntityId target, TraitKey key, TraitActivationKind kind, int amount, CombatDamageOrigin origin)
        {
            var occurrence = new CombatEffectsEvent(new[] { new TraitActivationSnapshot(source, target, key, kind, amount, resultOrigin: origin) },
                Array.Empty<CategoryActivationSnapshot>());
            damage.Events.Add(occurrence);
            return occurrence;
        }
    }
}
