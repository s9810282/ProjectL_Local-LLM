using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules;
using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Items;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>
    /// 사망 또는 전투 종료 때 비우는 전투 임시 상태(디버프, 아이템 슬롯, 아이템 효과)의 유일한 규칙이다.
    /// 누구를 비울지는 호출자가 정하고, 무엇을 비울지는 여기서만 정한다. 보유 환경은 별도 규칙을 따른다.
    /// 비울 것이 없으면 같은 참조를 돌려주므로 호출자는 참조 비교로 변경 여부를 판단할 수 있다.
    /// </summary>
    public static class CombatTemporaryStateCleanup
    {
        public static CharacterCombatFacts Clear(CharacterCombatFacts character) =>
            Clear(character, null, ItemEventReason.BattleEnded);

        /// <summary>장착 슬롯이 있으면 지정한 사유로 폐기 사건을 남긴다. 사건 목록이 없으면 사건 없이 비운다.</summary>
        internal static CharacterCombatFacts Clear(CharacterCombatFacts character, List<CombatEvent> events, ItemEventReason reason)
        {
            if (character == null) throw new ArgumentNullException(nameof(character));
            var cleared = ItemLifetimeCalculator.ClearRoundItems(character, events, reason);
            var traitBefore = cleared;
            cleared = RegenerationTurnApplication.Reset(cleared);
            if (cleared.StatusEffects.Entries.Count != 0 || cleared.TraitEffects.Shields.Count != 0 || cleared.TraitEffects.CohesionDefenseBonus != 0)
                cleared = cleared.WithStatusEffects(AppliedStatusRules.ClearForDeathOrBattleEnd(cleared.StatusEffects))
                    .With(traitEffects: CharacterTraitEffects.Empty,
                        stats: cleared.Stats.With(traitAttackBonus: checked(cleared.Stats.TraitAttackBonus - AppliedStatusRules.AttackIncrease(cleared.StatusEffects))));
            var stats = HunterApplication.ClearStage(StationaryFireTraitRule.BeginOwnTurn(AccelerationTraitRule.WithHeld(cleared.Stats, 0)));
            if (!stats.SameValues(cleared.Stats)) cleared = cleared.With(stats: stats);
            var final = cleared.Debuffs.SameValues(CharacterDebuffState.Empty)
                ? cleared
                : CombatDebuffApplication.WithDebuffs(cleared, CharacterDebuffState.Empty);
            if (events != null && !traitBefore.SameValues(final))
            {
                if (traitBefore.TraitEffects.CohesionDefenseBonus != 0)
                    CombatCommandEvents.Trait(events, character.EntityId, TraitKey.Cohesion, TraitActivationKind.DefenseChanged,
                        traitBefore.TraitEffects.CohesionDefenseBonus, new[] { character.EntityId });
                TraitPresenceReconciler.RecordShieldActivations(events, new[] { new CharacterStateChange(traitBefore, final) });
                foreach (var origin in traitBefore.StatusEffects.Entries.OfType<AttackIncreaseStatus>()
                    .Where(g => g.Amount > 0 && g.Origin.Value.Kind == EffectOwnerKind.Trait).Select(g => g.Origin.Value).Distinct().OrderBy(o => o))
                    CombatCommandEvents.Trait(events, new EntityId(origin.OwnerInstanceId), (TraitKey)origin.DefinitionKey,
                        TraitActivationKind.StatusRemoved, 0, new[] { character.EntityId });
                var hunter = traitBefore.Stats.TraitStatuses.ToArray().OfType<HunterTraitStatusSnapshot>().SingleOrDefault();
                if (hunter != null && (hunter.TargetId.HasValue || hunter.SuccessfulRewards != 0))
                    CombatCommandEvents.Trait(events, character.EntityId, TraitKey.Hunter, TraitActivationKind.StatusReset, 0, new[] { character.EntityId });
                if (AccelerationTraitRule.Held(traitBefore.Stats) > 0)
                    CombatCommandEvents.Trait(events, character.EntityId, TraitKey.Acceleration, TraitActivationKind.StatusReset, 0, new[] { character.EntityId });
                if (traitBefore.Stats.TraitStatuses.ToArray().OfType<StationaryFireTraitStatusSnapshot>().Any(s => s.MovedThisOwnTurn))
                    CombatCommandEvents.Trait(events, character.EntityId, TraitKey.StationaryFire, TraitActivationKind.StatusReset, 0, new[] { character.EntityId });
                var origins = traitBefore.Debuffs.ArmorBreak?.Stacks.Where(s => s.ReductionPercent > 0).Select(s => s.Origin).ToList()
                    ?? new List<Contracts.CombatEffects.EffectOriginKey>();
                if (traitBefore.Debuffs.Intimidation?.Origin.HasValue == true) origins.Add(traitBefore.Debuffs.Intimidation.Origin.Value);
                if (traitBefore.Debuffs.Disarm != null) origins.Add(traitBefore.Debuffs.Disarm.Origin);
                if (traitBefore.Debuffs.AnkleBreaker != null) origins.Add(traitBefore.Debuffs.AnkleBreaker.Origin);
                origins.AddRange(traitBefore.StatusEffects.Entries.OfType<WeaknessExposureStatus>().Select(s => s.Origin.Value));
                origins.AddRange(traitBefore.StatusEffects.Entries.OfType<TauntedStatus>().Select(s => s.Origin.Value));
                foreach (var origin in origins.Where(o => o.Kind == EffectOwnerKind.Trait).Distinct().OrderBy(o => o))
                    CombatCommandEvents.Trait(events, new EntityId(origin.OwnerInstanceId), (TraitKey)origin.DefinitionKey,
                        TraitActivationKind.StatusRemoved, 0, new[] { character.EntityId });
                if (traitBefore.Debuffs.HuntersMark != null)
                    CombatCommandEvents.Trait(events, traitBefore.Debuffs.HuntersMark.SourceId, TraitKey.HuntersMark,
                        TraitActivationKind.StatusRemoved, 0, new[] { character.EntityId });
                CombatCommandEvents.TraitState(events, traitBefore, final);
            }
            return final;
        }
    }
}
