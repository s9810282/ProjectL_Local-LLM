using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Categories;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Categories.States;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Entities;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.States;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>
    /// 정의와 보유·배치 사실을 초기 전투 값으로 계산하며 런타임 개체를 만들지 않는다.
    /// 기준 수치는 개체 종류마다 한 출처에서 받는다: 보유 카드는 보유 기록(레벨)·메인 지속 상태·영혼, 적은 배치가 정한 레벨.
    /// </summary>
    internal static class BattleCharacterCalculator
    {
        /// <summary>보유 플레이어 카드(메인·서브)의 초기 전투 값이다.</summary>
        internal static CharacterCombatFacts CreateOwned(EntityId id, UnitDefinition definition,
            OwnedCharacterRecord owned, MainCharacterPersistentState main, int soulCount, int? slot,
            CellCoord? anchor, Rotation4? rotation, CellCoord[] cells, TraitRegistry traits)
        {
            CharacterBaselineStats baseline = CharacterBaselineStatsCalculator.ForOwned(definition, owned, main, soulCount);

            return Assemble(id, definition, Owner.Player, owned, baseline, slot, anchor, rotation, cells, traits, null,
                CharacterExperienceFacts.AllyDefault);
        }

        /// <summary>보유 기록이 없는 적(초기 배치·출현·소환)의 초기 전투 값이다. 기준 수치는 주어진 레벨로 성장한 정의 값이다.</summary>
        internal static CharacterCombatFacts CreateEnemy(EntityId id, UnitDefinition definition, int level,
            CellCoord anchor, Rotation4 rotation, CellCoord[] cells, TraitRegistry traits,
            CharacterItemState items = null)
        {
            CharacterBaselineStats baseline = CharacterBaselineStatsCalculator.ForUnowned(definition, level);
            return Assemble(id, definition, Owner.Enemy, null, baseline, null, anchor, rotation, cells, traits, items,
                CharacterExperienceFacts.ForEnemy(level));
        }

        private static CharacterCombatFacts Assemble(EntityId id, UnitDefinition definition, Owner owner,
            OwnedCharacterRecord owned, CharacterBaselineStats baseline, int? slot, CellCoord? anchor,
            Rotation4? rotation, CellCoord[] cells, TraitRegistry traits, CharacterItemState items,
            CharacterExperienceFacts experience)
        {
            var traitStates = new List<TraitRuntimeState>();
            foreach (var trait in definition.TraitDefinitions.Span)
                traitStates.Add(traits.GetRequiredRegistration(trait.TraitKey)
                    .TraitRuntimeStateFactory.BuildRuntimeState(trait));

            var categoryStates = new List<CategoryRuntimeState>();
            foreach (var rule in definition.CombatCategory.RuleDefinitions.Span)
            {
                CategoryRuntimeState state = CategoryRuleFactories.CreateInitialState(rule);
                if (state != null)
                    categoryStates.Add(state);
            }

            var statuses = new TraitEffectProvider(traits).CreateStatuses(
                definition, new UnitTraitStateSet(traitStates.ToArray()));
            var category = new CategorySnapshotBuilder().CreateSnapshot(
                definition.CombatCategory, new UnitCategoryState(categoryStates.ToArray()));

            // 기준 수치는 공통 계산 결과를 옮겨 담기만 한다. 초기 Artifact 보너스는 영혼 기여분뿐이다.
            var stats = new UnitStatsSnapshot(baseline.MoveMethod,baseline.MoveRange,                baseline.AttackRange,baseline.BaseAttackPower,baseline.PhysicalDefense,baseline.Priority,                statuses,category,artifactAttackBonus: baseline.SoulAttackBonus,                effectiveAttackPower: baseline.EffectiveAttackPower,                soulAttackBonus: baseline.SoulAttackBonus,soulMaxHealthBonus: baseline.SoulMaxHealthBonus,                traitAttackBonus: baseline.TraitAttackBonus, specializationCategory: definition.SpecializationCategory);

            return new CharacterCombatFacts(id, owned?.Id, definition.Key, owner, owned?.Role,
                slot.HasValue ? CharacterRuntimeArea.Hand : CharacterRuntimeArea.Field,
                baseline.Health, stats, slot, anchor, rotation, cells, items: items, experience: experience);
        }
    }
}
