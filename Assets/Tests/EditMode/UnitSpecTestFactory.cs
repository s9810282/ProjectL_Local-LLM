using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Categories;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Tests.Core
{
    internal static class UnitSpecTestFactory
    {
        internal static CategorySpec[] CreateDefaultCategoryCatalog()
        {
            return new[]
            {
                new CategorySpec(CategoryKey.Main,Array.Empty<CategoryRuleSpec>(),3),
                new CategorySpec(CategoryKey.Rider,Array.Empty<CategoryRuleSpec>(),4),
                new CategorySpec(CategoryKey.Heavy,Array.Empty<CategoryRuleSpec>(),3),
                new CategorySpec(CategoryKey.Cannon,new CategoryRuleSpec[]
                {
                    new SafeBombardmentCategoryRuleSpec(),
                },2),
            };
        }

        /// <summary>레벨 성장 설계 §3.1의 상승치(Rider 체+1·공+1, Heavy 체+3, Cannon 공+2)를 가진 필수 Category 목록이다.</summary>
        internal static CategorySpec[] CreateGrowthCategoryCatalog(CategoryLevelGrowth baseGrowth = default)
        {
            return new[]
            {
                new CategorySpec(CategoryKey.Main,Array.Empty<CategoryRuleSpec>(),3,baseGrowth),
                new CategorySpec(CategoryKey.Rider,Array.Empty<CategoryRuleSpec>(),4,new CategoryLevelGrowth(1, 1)),
                new CategorySpec(CategoryKey.Heavy,Array.Empty<CategoryRuleSpec>(),3,new CategoryLevelGrowth(3, 0)),
                new CategorySpec(CategoryKey.Cannon,new CategoryRuleSpec[]
                {
                    new SafeBombardmentCategoryRuleSpec(),
                },2,new CategoryLevelGrowth(0, 2)),
            };
        }

        internal static CategoryMatchupSpec CreateDefaultCategoryMatchups() =>
            CategoryMatchupSpec.CreateDefault();

        /// <summary>지정 방식을 따지지 않는 시험의 기본 지정 방식이다. 직선·단일이라 끌어오기·가시밭·충격 볼트 결합 검사를 통과한다.</summary>
        internal static TargetingMethodSpec DefaultMethod() =>
            new(TargetingShape.CardinalLine, TargetingChoiceMode.Single, TargetingSplash.None, 0);

        /// <summary>유닛마다 <see cref="DefaultMethod"/>를 붙인 공격 프로필이다.</summary>
        internal static CombatAttackProfile[] Profiles(params UnitSpec[] units)
        {
            var profiles = new CombatAttackProfile[units.Length];
            for (int i = 0; i < units.Length; i++)
                profiles[i] = new CombatAttackProfile(units[i], DefaultMethod());
            return profiles;
        }

        /// <summary>목록으로 받은 유닛에 <see cref="DefaultMethod"/>를 붙인다.</summary>
        internal static CombatAttackProfile[] Profiles(IReadOnlyList<UnitSpec> units)
        {
            var copied = new UnitSpec[units.Count];
            for (int i = 0; i < copied.Length; i++)
                copied[i] = units[i];
            return Profiles(copied);
        }

        internal static UnitSpec Create(
            EntityKey key,
            int footprintWidth,
            int footprintHeight,
            CellCoord[] footprintLocalCells,
            int maxHealth,
            MoveMethod moveMethod,
            int attackRange,
            int attackPower,
            int priority = 0,
            UnitOwnerEligibility ownerEligibility = UnitOwnerEligibility.Both)
        {
            return Create(
                key,
                footprintWidth,
                footprintHeight,
                footprintLocalCells,
                maxHealth,
                moveMethod,
                attackRange,
                attackPower,
                priority,
                Array.Empty<TraitSpec>(),
                ownerEligibility: ownerEligibility);
        }

        internal static UnitSpec Create(
            EntityKey key,
            int footprintWidth,
            int footprintHeight,
            CellCoord[] footprintLocalCells,
            int maxHealth,
            MoveMethod moveMethod,
            int attackRange,
            int attackPower,
            int priority,
            IReadOnlyList<TraitSpec> traits,
            UnitRole role = UnitRole.Regular,
            CategoryKey categoryKey = CategoryKey.Main,
            int physicalDefense = 0,
            UnitOwnerEligibility ownerEligibility = UnitOwnerEligibility.Both)
        {

            return new UnitSpec(
                key,
                role,
                footprintWidth,
                footprintHeight,
                footprintLocalCells,
                maxHealth,
                moveMethod,
                attackRange,
                categoryKey,
                attackPower,
                physicalDefense,
                priority,
                traits,
                ownerEligibility,
                behavior: categoryKey == CategoryKey.Boss ? new UnitBehaviorSpec(false, true, false) : null,
                killReward: new UnitKillRewardSpec(0, 10));
        }
    }
}
