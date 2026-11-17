using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>실제 예비 구역 진입에서 수신 효과와 임시 진행값만 비웁니다. 장비 환급·영구 성장·행동 한도는 기존 소유자가 유지합니다.</summary>
    internal static class CombatReserveReturnCleanup
    {
        /// <param name="keepShieldBudget">배치 단계 손 복귀(`CombatPlacementCalculator.Return`)만 참. 방어막 남은 예산을 지우지 않고 자격만 끈다(`AppliedStatusRules.ClearForReserve`).</param>
        internal static CharacterCombatFacts Clear(CharacterCombatFacts character, List<CombatEvent> events, bool keepShieldBudget = false,
            List<CombatHealthExecution> executions = null, CombatCharacterEvent returned = null)
        {
            if (character == null) throw new ArgumentNullException(nameof(character));
            var statuses = AppliedStatusRules.ClearForReserve(character.StatusEffects, keepShieldBudget);
            var stats = StationaryFireTraitRule.BeginOwnTurn(AccelerationTraitRule.WithHeld(character.Stats, 0));
            // 이미 투영된 영구 기여는 유지하고 이 수신자가 실제 보유한 임시 기여만 뺍니다.
            stats = stats.With(traitAttackBonus: checked(stats.TraitAttackBonus - AppliedStatusRules.AttackIncrease(character.StatusEffects)),
                artifactAttackBonus: checked(stats.ArtifactAttackBonus - character.TemporaryAttackBonus),
                traitStatuses: stats.TraitStatuses.ToArray().Select(s => s switch
                {
                    HunterTraitStatusSnapshot hunter when hunter.TargetId.HasValue => new HunterTraitStatusSnapshot(null, hunter.SuccessfulRewards),
                    RoarTraitStatusSnapshot roar => roar.WithHitCount(0),
                    _ => s
                }).ToArray());
            int maximum = checked(character.Health.MaxHealth - character.TemporaryMaxHealthBonus);
            var cleared = character.With(stats: stats, health: new HealthStateSnapshot(Math.Min(character.Health.CurrentHealth, maximum), maximum),
                temporaryAttackBonus: 0, temporaryMaxHealthBonus: 0,
                items: new CharacterItemState(character.Items.EquippedItemKey, Array.Empty<ItemTemporaryEffect>()),
                heldEnvironment: CharacterHeldEnvironmentState.Empty).WithStatusEffects(statuses);
            cleared = cleared.With(debuffs: CharacterDebuffState.Empty, traitEffects: new CharacterTraitEffects(
                cleared.TraitEffects.AttackGrants, cleared.TraitEffects.Shields, 0));
            cleared = RegenerationTurnApplication.Reset(cleared);
            if (character.SameValues(cleared)) return character;
            if (events == null) return cleared;

            if (!character.HeldEnvironment.SameValues(cleared.HeldEnvironment))
                events.Add(new CombatHeldEnvironmentEvent(character, cleared));
            TraitPresenceReconciler.RecordShieldActivations(events, new[] { new CharacterStateChange(character, cleared) });
            foreach (var origin in character.StatusEffects.Entries.Where(s => s is not ShieldStatus && s.Origin.HasValue &&
                s.Origin.Value.Kind == EffectOwnerKind.Trait).Select(s => s.Origin.Value).Distinct().OrderBy(o => o))
                CombatCommandEvents.Trait(events, new EntityId(origin.OwnerInstanceId), (TraitKey)origin.DefinitionKey,
                    TraitActivationKind.StatusRemoved, 0, new[] { character.EntityId });
            foreach (var before in character.Stats.TraitStatuses.ToArray())
            {
                var after = cleared.Stats.TraitStatuses.ToArray().Single(s => s.Key == before.Key);
                if (!before.SameValues(after))
                    CombatCommandEvents.Trait(events, character.EntityId, before.Key, TraitActivationKind.StatusReset, 0, new[] { character.EntityId });
            }
            if (character.TraitEffects.CohesionDefenseBonus != 0)
                CombatCommandEvents.Trait(events, character.EntityId, TraitKey.Cohesion, TraitActivationKind.DefenseChanged,
                    character.TraitEffects.CohesionDefenseBonus, new[] { character.EntityId });
            // 정규화는 회복·피해가 아닙니다. 완전한 사실을 한 번 게시하고 스테이지 피해 기록은 만들지 않습니다.
            var evidence = CombatCommandEvents.TraitState(events, character, cleared);
            if (executions != null && !character.Health.Equals(cleared.Health))
                CombatHealthRecording.Add(executions, character.EntityId, character.Health, cleared.Health,
                    new CombatHealthSource(CombatHealthOperationKind.Normalization, contextEvent: returned ?? (CombatEvent)evidence),
                    evidence, normalization: character.Health.CurrentHealth - cleared.Health.CurrentHealth);
            return cleared;
        }
    }
}
