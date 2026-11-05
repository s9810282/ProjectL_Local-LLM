using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Categories;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Tiles;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Entities
{
    public sealed class UnitDefinition
    {
        public UnitKillRewardSpec KillReward { get; }
        public EntityKey Key { get; }
        public UnitRole Role { get; }
        public TileFootprint Footprint { get; }
        public int MaxHealth { get; }
        public MoveMethod MoveMethod { get; }
        public int MoveRange => CombatCategory.MovementAllowance;
        public CategoryDefinition Category { get; }
        public CategoryKey CategoryKey => Category.Key;
        public CategoryDefinition CombatCategory { get; }
        public CategoryKey CombatCategoryKey => CombatCategory.Key;
        /// <summary>정체성과 다른 전투 병과만 전직 선택으로 공개한다.</summary>
        public CategoryKey? SpecializationCategory => CombatCategoryKey == CategoryKey ? (CategoryKey?)null : CombatCategoryKey;
        /// <summary>공격 사거리다. 모양·선택·스플래시는 지정 방식(TargetingMethodSpec)이 소유한다.</summary>
        public int AttackRange { get; }
        public int AttackPower { get; }
        public int PhysicalDefense { get; }
        public int Priority { get; }
        public ReadOnlyMemory<TraitDefinition> TraitDefinitions { get; }
        public UnitOwnerEligibility OwnerEligibility { get; }
        public AttackOcclusionSpec Occlusion { get; }

        internal UnitDefinition(
            EntityKey key,
            UnitRole role,
            TileFootprint footprint,
            int maxHealth,
            MoveMethod moveMethod,

            int attackRange,
            CategoryDefinition category,
            int attackPower,

            int physicalDefense,
            int priority,

            TraitDefinition[] traits,
            UnitOwnerEligibility ownerEligibility = UnitOwnerEligibility.Both,
            AttackOcclusionSpec occlusion = null,
            UnitKillRewardSpec killReward = null,
            CategoryDefinition combatCategory = null)
        {
            ValidateCommonValues(
                key,
                role,
                footprint,
                maxHealth,
                attackRange,
                nameof(attackRange),
                attackPower);

            if (physicalDefense < 0)
                throw new ArgumentOutOfRangeException(nameof(physicalDefense));
            if (priority < 0)
                throw new ArgumentOutOfRangeException(nameof(priority));

            if (moveMethod != MoveMethod.LiftAndPlace)
                throw new ArgumentOutOfRangeException(nameof(moveMethod), "moveMethod는 LiftAndPlace여야 합니다.");
            if (category == null)
                throw new ArgumentNullException(nameof(category));
            PlayerMainDefinitionPolicy.Validate(key, role, category.Key, ownerEligibility);
            if (combatCategory != null && (role != UnitRole.MainCharacter || category.Key != CategoryKey.Main ||
                ownerEligibility != UnitOwnerEligibility.PlayerOnly ||
                combatCategory.Key != CategoryKey.Rider && combatCategory.Key != CategoryKey.Heavy && combatCategory.Key != CategoryKey.Cannon))
                throw new ArgumentException("플레이어 메인만 Rider·Heavy·Cannon 전투 병과를 사용할 수 있습니다.", nameof(combatCategory));
            if (traits == null)
                throw new ArgumentNullException(nameof(traits));
            if (Enum.IsDefined(typeof(UnitOwnerEligibility), ownerEligibility) == false)
                throw new ArgumentOutOfRangeException(nameof(ownerEligibility));

            for (int i = 0; i < traits.Length; i++)
            {
                if (traits[i] == null)
                    throw new ArgumentNullException(nameof(traits));
            }

            TraitDefinition[] copiedTraits = new TraitDefinition[traits.Length];
            Array.Copy(traits, copiedTraits, traits.Length);
            Array.Sort(copiedTraits, (left, right) => {
                int keyComparison = left.TraitKey.CompareTo(right.TraitKey);
                return keyComparison;
            });

            TraitDefinitions = copiedTraits;


            KillReward = killReward;
            Key = key;
            Role = role;
            Footprint = footprint;
            MaxHealth = maxHealth;
            MoveMethod = moveMethod;

            AttackRange = attackRange;
            Category = category;
            CombatCategory = combatCategory ?? category;
            AttackPower = attackPower;
            PhysicalDefense = physicalDefense;

            Priority = priority;

            OwnerEligibility = ownerEligibility;
            Occlusion = occlusion ?? AttackOcclusionSpec.Default;
        }


        private static void ValidateCommonValues(
            EntityKey key,
            UnitRole role,
            TileFootprint footprint,
            int maxHealth,

            int attackRange,
            string attackRangeParameterName,
            int attackPower)
        {
            // 정의된 키의 Unit_ 이름 규칙으로 분류하여 새 유닛마다 허용 목록을 수정하지 않습니다.
            string keyName = Enum.GetName(typeof(EntityKey), key);
            bool isUnitKey = keyName != null && keyName.StartsWith("Unit_", StringComparison.Ordinal);

            if (isUnitKey == false)
            {
                throw new ArgumentException("키 값이 None이거나 유닛에 알맞지 않는 값입니다.", nameof(key));
            }
            if (Enum.IsDefined(typeof(UnitRole), role) == false)
                throw new ArgumentOutOfRangeException(nameof(role));

            if (footprint == null)
                throw new ArgumentNullException(nameof(footprint), "footprint값은 null이 될 수 없습니다.");

            if (role == UnitRole.MainCharacter &&
                (footprint.Width != 1 ||
                 footprint.Height != 1 ||
                 footprint.CellCount != 1 ||
                 footprint.GetLocalCell(0) != CellCoord.Zero))
            {
                throw new ArgumentException("메인 캐릭터는 1x1 footprint가 필요합니다.", nameof(role));
            }

            if (maxHealth <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxHealth), "체력은 1 이상이어야 합니다.");
            if (attackRange < 0)
                throw new ArgumentOutOfRangeException(attackRangeParameterName, "지정 범위는 음수가 될 수 없습니다.");
            if (attackPower < 0)
                throw new ArgumentOutOfRangeException(nameof(attackPower), "공격 데미지는 음수가 될 수 없습니다.");
        }
    }
}
