using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands
{
    /// <summary>
    /// View가 Unit ScriptableObject 값을 Core에 전달할 때 사용하는 Unit 설정이다.
    /// </summary>
    public sealed class UnitSpec
    {
        public UnitKillRewardSpec KillReward { get; }
        public EntityKey Key { get; }
        public UnitRole Role { get; }
        public int FootprintWidth { get; }
        public int FootprintHeight { get; }
        public ReadOnlyMemory<CellCoord> FootprintLocalCells { get; }
        public int MaxHealth { get; }
        public MoveMethod MoveMethod { get; }
        public CategoryKey CategoryKey { get; }
        /// <summary>보유 기록에서 합성한 메인의 추가 병과다. 원본 식별 병과는 바꾸지 않는다.</summary>
        public CategoryKey? SpecializationCategory { get; }
        public CategoryKey CombatCategoryKey => SpecializationCategory ?? CategoryKey;
        /// <summary>공격 사거리다. 모양·선택·스플래시는 유닛이 참조하는 지정 방식(TargetingMethodSpec)이 소유한다.</summary>
        public int AttackRange { get; }
        public int AttackPower { get; }
        public int PhysicalDefense { get; }
        public int Priority { get; }
        public ReadOnlyMemory<TraitSpec> Traits { get; }
        public UnitOwnerEligibility OwnerEligibility { get; }
        public AttackOcclusionSpec Occlusion { get; }
        public UnitBehaviorSpec Behavior { get; }

        public UnitSpec(
            EntityKey key,
            UnitRole role,
            int footprintWidth,
            int footprintHeight,
            CellCoord[] footprintLocalCells,
            int maxHealth,
            MoveMethod moveMethod,

            int attackRange,
            CategoryKey categoryKey,
            int attackPower,

            int physicalDefense,
            int priority,

            IReadOnlyList<TraitSpec> traits,
            UnitOwnerEligibility ownerEligibility = UnitOwnerEligibility.Both,
            AttackOcclusionSpec occlusion = null,
            UnitBehaviorSpec behavior = null,
            UnitKillRewardSpec killReward = null,
            CategoryKey? specializationCategory = null)
        {
            if (key == EntityKey.None)
                throw new ArgumentException("엔티티 키는 None이 될 수 없습니다.", nameof(key));
            if (Enum.IsDefined(typeof(UnitRole), role) == false)
                throw new ArgumentOutOfRangeException(nameof(role));
            if (footprintWidth <= 0)
                throw new ArgumentOutOfRangeException(nameof(footprintWidth));
            if (footprintHeight <= 0)
                throw new ArgumentOutOfRangeException(nameof(footprintHeight));
            if (footprintLocalCells == null || footprintLocalCells.Length == 0)
                throw new ArgumentException("footprint의 cell은 null일 수 없습니다.", nameof(footprintLocalCells));
            if (maxHealth <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxHealth));
            if (moveMethod != MoveMethod.LiftAndPlace)
                throw new ArgumentOutOfRangeException(nameof(moveMethod), "moveMethod는 LiftAndPlace여야 합니다.");
            if (attackRange < 0)
                throw new ArgumentOutOfRangeException(nameof(attackRange));

            if (Enum.IsDefined(typeof(CategoryKey), categoryKey) == false ||
                categoryKey == CategoryKey.None)
            {
                throw new ArgumentOutOfRangeException(nameof(categoryKey));
            }

            if (attackPower < 0)
                throw new ArgumentOutOfRangeException(nameof(attackPower));
            if (physicalDefense < 0)
                throw new ArgumentOutOfRangeException(nameof(physicalDefense));
            if (priority < 0)
                throw new ArgumentOutOfRangeException(nameof(priority));
            if (traits == null)
                throw new ArgumentNullException(nameof(traits));
            if (Enum.IsDefined(typeof(UnitOwnerEligibility), ownerEligibility) == false)
                throw new ArgumentOutOfRangeException(nameof(ownerEligibility));
            if (specializationCategory.HasValue)
            {
                if (role != UnitRole.MainCharacter || categoryKey != CategoryKey.Main ||
                    ownerEligibility != UnitOwnerEligibility.PlayerOnly ||
                    specializationCategory != CategoryKey.Rider && specializationCategory != CategoryKey.Heavy &&
                    specializationCategory != CategoryKey.Cannon)
                    throw new ArgumentException("플레이어 메인만 Rider·Heavy·Cannon 전투 병과를 선택할 수 있습니다.", nameof(specializationCategory));
                PlayerMainDefinitionPolicy.Validate(key, role, categoryKey, ownerEligibility);
            }

            CellCoord[] copiedCells = new CellCoord[footprintLocalCells.Length];
            Array.Copy(footprintLocalCells, copiedCells, footprintLocalCells.Length);

            TraitSpec[] copiedTraits = new TraitSpec[traits.Count];
            HashSet<TraitKey> traitKeys = new HashSet<TraitKey>();
            for (int i = 0; i < traits.Count; i++)
            {
                TraitSpec trait = traits[i];
                if (trait == null)
                {
                    throw new ArgumentException(
                        "Trait 목록에는 null 항목을 넣을 수 없습니다.",
                        nameof(traits));
                }

                if (traitKeys.Add(trait.TraitKey) == false)
                {
                    throw new ArgumentException(
                        "같은 TraitKey를 두 번 넣을 수 없습니다.",
                        nameof(traits));
                }

                copiedTraits[i] = trait;
            }

            KillReward = killReward;
            Key = key;
            Role = role;
            FootprintWidth = footprintWidth;
            FootprintHeight = footprintHeight;
            FootprintLocalCells = copiedCells;
            MaxHealth = maxHealth;
            MoveMethod = moveMethod;

            AttackRange = attackRange;
            CategoryKey = categoryKey;
            SpecializationCategory = specializationCategory;
            AttackPower = attackPower;
            PhysicalDefense = physicalDefense;

            Priority = priority;

            Traits = copiedTraits;
            OwnerEligibility = ownerEligibility;
            Occlusion = occlusion ?? AttackOcclusionSpec.Default;
            Behavior = behavior ?? UnitBehaviorSpec.Default;
            if (categoryKey == CategoryKey.Boss && Behavior.CanMoveVoluntarily)
                throw new ArgumentException("Boss 자발 이동 허용량은 미정이므로 이동 비활성 정의만 허용합니다.", nameof(behavior));
        }

    }
}
