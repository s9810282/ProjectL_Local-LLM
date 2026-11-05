using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Item
{
    /// <summary>
    /// 유닛 한 종류의 규칙 데이터입니다. Core가 받는 순수 C# 데이터이며 UnitDataSO가 생성합니다.
    /// 필드는 Core <see cref="UnitSpec"/> 생성자 인자에 맞춥니다. 표시 이름은 ViewGroup의 UnitVisualTable이 가집니다.
    /// </summary>
    /// <remarks>
    /// Spec 변환 규칙: 점유 셀은 가로×세로를 채우고, 이동 방식은 LiftAndPlace, 행동 가격과 이동 허용량은 병과 규칙에서 파생합니다.
    /// 사거리는 <see cref="AttackRange"/>가, 공격 모양·선택·스플래시는 <see cref="TargetingMethodKey"/>가 가리키는 지정 방식 테이블이 가집니다.
    /// </remarks>
    public sealed class UnitData : IData
    {
        public UnitKillRewardSpec KillReward { get; }
        public EntityKey Key { get; }
        public UnitRole Role { get; }
        public UnitOwnerEligibility OwnerEligibility { get; }
        public int FootprintWidth { get; }
        public int FootprintHeight { get; }
        public int MaxHealth { get; }
        public int PhysicalDefense { get; }
        /// <summary>공격 지정 방식 테이블의 키입니다. 공격력이 0인 유닛만 미지정(0)을 허용합니다.</summary>
        public int TargetingMethodKey { get; }
        public int AttackRange { get; }
        public CategoryKey Category { get; }
        public int AttackPower { get; }
        public int Priority { get; }
        public IReadOnlyList<UnitTraitData> Traits { get; }
        public AttackOcclusionSpec Occlusion { get; }
        /// <summary>유닛의 고정 행동 설정입니다. 카테고리와 별개이며 생략하면 일반 유닛 기본값을 사용합니다.</summary>
        public UnitBehaviorSpec Behavior { get; }

        public UnitData(
            EntityKey key,
            UnitRole role,
            UnitOwnerEligibility ownerEligibility,
            int footprintWidth,
            int footprintHeight,
            int maxHealth,
            int physicalDefense,
            int attackRange,
            CategoryKey category,
            int attackPower,
            int priority,
            UnitTraitData[] traits,
            int targetingMethodKey, AttackOcclusionSpec occlusion = null, UnitBehaviorSpec behavior = null,
            UnitKillRewardSpec killReward = null)
        {
            KillReward = killReward;
            Key = key;
            Role = role;
            OwnerEligibility = ownerEligibility;
            FootprintWidth = footprintWidth;
            FootprintHeight = footprintHeight;
            MaxHealth = maxHealth;
            PhysicalDefense = physicalDefense;

            TargetingMethodKey = targetingMethodKey;
            AttackRange = attackRange;
            Category = category;
            AttackPower = attackPower;

            Priority = priority;

            Traits = new ReadOnlyCollection<UnitTraitData>(traits ?? Array.Empty<UnitTraitData>());
            Occlusion = occlusion ?? AttackOcclusionSpec.Default;
            Behavior = behavior ?? UnitBehaviorSpec.Default;
        }

        public void Validate()
        {
            if (Key == EntityKey.None || !System.Enum.IsDefined(typeof(EntityKey), Key))
                throw new InvalidOperationException($"[UnitData] Unit key is invalid. Key: {Key}");
            if (!System.Enum.IsDefined(typeof(UnitRole), Role) ||
                !System.Enum.IsDefined(typeof(UnitOwnerEligibility), OwnerEligibility))
                throw new InvalidOperationException($"[UnitData] Enum value is invalid. Key: {Key}");
            if (Category == CategoryKey.None || !System.Enum.IsDefined(typeof(CategoryKey), Category))
                throw new InvalidOperationException($"[UnitData] Category must be selected. Key: {Key}");
            if (FootprintWidth < 1 || FootprintHeight < 1 || MaxHealth < 1 || Priority < 1)
                throw new InvalidOperationException($"[UnitData] Value must be at least 1. Key: {Key}");
            if (AttackRange < 0 || AttackPower < 0 || PhysicalDefense < 0)
                throw new InvalidOperationException($"[UnitData] Value must not be negative. Key: {Key}");
            if (TargetingMethodKey < 0 || (AttackPower > 0 && TargetingMethodKey == 0))
                throw new InvalidOperationException($"[UnitData] Attacking units require a positive targeting method key. Key: {Key}, Method: {TargetingMethodKey}.");


            ValidateMainCharacter();
            ValidateTraits();
        }


        /// <summary>공통 메인 정의 불변식과 메인의 1x1 크기를 검증한다.</summary>
        private void ValidateMainCharacter()
        {
            PlayerMainDefinitionPolicy.Validate(Key, Role, Category, OwnerEligibility);
            if (Role != UnitRole.MainCharacter) return;
            if (FootprintWidth != 1 || FootprintHeight != 1)
                throw new InvalidOperationException("[UnitData] Main character must have a 1x1 footprint.");
            // 메인 캐릭터도 특성을 가질 수 있다(2026-10-15 사용자 결정). 특성 규칙은 아래 ValidateTraits가 다른 유닛과 같게 검사한다.
        }

        private void ValidateTraits()
        {
            var traitKeys = new HashSet<TraitKey>();
            foreach (UnitTraitData trait in Traits)
            {
                if (trait == null)
                    throw new InvalidOperationException($"[UnitData] Trait is null. Key: {Key}");
                trait.Validate();
                if (!traitKeys.Add(trait.TraitKey))
                    throw new InvalidOperationException($"[UnitData] Duplicate trait {trait.TraitKey}. Key: {Key}");
            }
        }
    }
}
