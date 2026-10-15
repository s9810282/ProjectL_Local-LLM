using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Categories;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Traits;
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
    /// <summary>정의와 보유 사실을 초기 전투 값으로 계산하며 런타임 개체를 만들지 않는다.</summary>
    internal static class BattleCharacterCalculator
    {
        internal static CharacterCombatFacts Create(EntityId id, UnitDefinition definition,
            Owner owner, OwnedCharacterRecord owned, MainCharacterPersistentState main,
            int soulCount, int? slot, CellCoord? anchor, Rotation4? rotation,
            CellCoord[] cells, TraitRegistry traits)
        {
            if (soulCount < 0)
                throw new ArgumentOutOfRangeException(nameof(soulCount));
            if (owner == Owner.Player && definition.TraitDefinitions.Length > 2)
                throw new InvalidOperationException("플레이어 특성은 최대 두 개입니다.");

            var traitStates = new List<TraitRuntimeState>();
            foreach (var trait in definition.TraitDefinitions.Span)
                traitStates.Add(traits.GetRequiredRegistration(trait.TraitKey)
                    .TraitRuntimeStateFactory.BuildRuntimeState(trait));

            var categoryStates = new List<CategoryRuntimeState>();
            foreach (var rule in definition.Category.RuleDefinitions.Span)
            {
                CategoryRuntimeState state = CategoryRuleFactories.CreateInitialState(rule);
                if (state != null)
                    categoryStates.Add(state);
            }

            var statuses = new TraitEffectProvider(traits).CreateStatuses(
                definition, new UnitTraitStateSet(traitStates.ToArray()));
            var category = new CategorySnapshotBuilder().CreateSnapshot(
                definition.Category, new UnitCategoryState(categoryStates.ToArray()));

            bool isMain = owned?.Role == CharacterOwnershipRole.Main;
            int maxHealth = checked((isMain ? main.PersistentMaxHealth : definition.MaxHealth) + soulCount);
            int attack = isMain ? main.PersistentAttack : definition.AttackPower;
            int moveRange = isMain ? main.MoveRange : definition.MoveRange;
            int moveCost = isMain ? main.MoveCost : definition.MoveCost;
            int actionCost = isMain ? main.MaxActionCost : definition.MaxActionCost;
            var stats = new UnitStatsSnapshot(definition.MoveMethod, moveRange, moveCost,
                definition.AttackRange, attack, definition.PhysicalDefense,
                actionCost, definition.AttackCost, 0, 0, definition.Priority, definition.Delay,
                statuses, category, artifactAttackBonus: soulCount,
                soulAttackBonus: soulCount, soulMaxHealthBonus: soulCount);

            return new CharacterCombatFacts(id, owned?.Id, definition.Key, owner, owned?.Role,
                slot.HasValue ? CharacterRuntimeArea.Hand : CharacterRuntimeArea.Field,
                new HealthStateSnapshot(isMain ? main.CurrentHealth : maxHealth, maxHealth),
                stats, slot, anchor, rotation, cells, anchor ?? CellCoord.Zero, 0, 0);
        }
    }
}
