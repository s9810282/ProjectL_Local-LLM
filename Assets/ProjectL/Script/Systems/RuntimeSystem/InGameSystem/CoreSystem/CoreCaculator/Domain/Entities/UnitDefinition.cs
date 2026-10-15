using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Tiles;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Entities
{
    public sealed class UnitDefinition
    {
        public EntityKey Key { get; }
        public UnitRole Role { get; }
        public TileFootprint Footprint { get; }
        public int MaxHealth { get; }
        public MoveMethod MoveMethod { get; }
        public int MoveRange { get; }
        public int MoveCost { get; }
        public CategoryDefinition Category { get; }
        public CategoryKey CategoryKey => Category.Key;
        /// <summary>공격 사거리다. 모양·선택·스플래시는 지정 방식(TargetingMethodSpec)이 소유한다.</summary>
        public int AttackRange { get; }
        public int AttackPower { get; }
        public int PhysicalDefense { get; }
        public int MaxActionCost { get; }
        public int AttackCost { get; }
        public int ActionPoint => MaxActionCost;
        public int Priority { get; }
        public int Delay { get; }
        public ReadOnlyMemory<TraitDefinition> TraitDefinitions { get; }
        public bool HasTimelineConfiguration { get; }
        public UnitOwnerEligibility OwnerEligibility { get; }
        public AttackOcclusionSpec Occlusion { get; }

        internal UnitDefinition(
            EntityKey key,
            UnitRole role,
            TileFootprint footprint,
            int maxHealth,
            MoveMethod moveMethod,
            int moveRange,
            int moveCost,
            int attackRange,
            CategoryDefinition category,
            int attackPower,
            int maxActionCost,
            int attackCost,
            int physicalDefense,
            int priority,
            int delay,
            TraitDefinition[] traits,
            bool hasTimelineConfiguration,
            UnitOwnerEligibility ownerEligibility = UnitOwnerEligibility.Both,
            AttackOcclusionSpec occlusion = null)
        {
            ValidateCommonValues(
                key,
                role,
                footprint,
                maxHealth,
                moveRange,
                attackRange,
                nameof(attackRange),
                attackPower,
                maxActionCost,
                nameof(maxActionCost));

            if (physicalDefense < 0)
                throw new ArgumentOutOfRangeException(nameof(physicalDefense));

            if (moveMethod != MoveMethod.LiftAndPlace)
                throw new ArgumentOutOfRangeException(nameof(moveMethod), "moveMethod는 LiftAndPlace여야 합니다.");
            if (moveCost <= 0)
                throw new ArgumentOutOfRangeException(nameof(moveCost));
            if (category == null)
                throw new ArgumentNullException(nameof(category));
            if (attackCost <= 0)
                throw new ArgumentOutOfRangeException(nameof(attackCost));
            if (hasTimelineConfiguration && priority <= 0)
                throw new ArgumentOutOfRangeException(nameof(priority));
            if (hasTimelineConfiguration && delay <= 0)
                throw new ArgumentOutOfRangeException(nameof(delay));
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


            Key = key;
            Role = role;
            Footprint = footprint;
            MaxHealth = maxHealth;
            MoveMethod = moveMethod;
            MoveRange = moveRange;
            MoveCost = moveCost;
            AttackRange = attackRange;
            Category = category;
            AttackPower = attackPower;
            PhysicalDefense = physicalDefense;
            MaxActionCost = maxActionCost;
            AttackCost = attackCost;
            Priority = priority;
            Delay = delay;
            HasTimelineConfiguration = hasTimelineConfiguration;
            OwnerEligibility = ownerEligibility;
            Occlusion = occlusion ?? AttackOcclusionSpec.Default;
        }

        private static void ValidateCommonValues(
            EntityKey key,
            UnitRole role,
            TileFootprint footprint,
            int maxHealth,
            int moveRange,
            int attackRange,
            string attackRangeParameterName,
            int attackPower,
            int maxActionCost,
            string maxActionCostParameterName)
        {
            bool isRegularUnitKey =
                key == EntityKey.Unit_Akimbo ||
                key == EntityKey.Unit_ShotGun ||
                key == EntityKey.Unit_Sniper ||
                key == EntityKey.Unit_Demon ||
                key == EntityKey.Unit_Ballista ||
                key == EntityKey.Unit_CactusCannon ||
                key == EntityKey.Unit_Gravekeeper;
            bool isMainCharacterKey = key == EntityKey.Unit_MainCharacter;

            if (isRegularUnitKey == false && isMainCharacterKey == false)
            {
                throw new ArgumentException("키 값이 None이거나 유닛에 알맞지 않는 값입니다.", nameof(key));
            }
            if (Enum.IsDefined(typeof(UnitRole), role) == false)
                throw new ArgumentOutOfRangeException(nameof(role));
            if (isMainCharacterKey != (role == UnitRole.MainCharacter))
                throw new ArgumentException("UnitKey와 UnitRole 조합이 일치하지 않습니다.", nameof(role));

            if (footprint == null)
                throw new ArgumentNullException(nameof(footprint), "footprint값은 null이 될 수 없습니다.");

            if (role == UnitRole.MainCharacter &&
                (footprint.Width != 1 ||
                 footprint.Height != 1 ||
                 footprint.CellCount != 1 ||
                 footprint.GetLocalCell(0) != CellCoord.Zero ||
                 attackRange != 1))
            {
                throw new ArgumentException("메인 캐릭터는 1x1 footprint와 지정 범위 1이 필요합니다.", nameof(role));
            }

            if (maxHealth <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxHealth), "체력은 1 이상이어야 합니다.");
            if (moveRange < 0)
                throw new ArgumentOutOfRangeException(nameof(moveRange), "이동 값은 음수가 될 수 없습니다.");
            if (attackRange < 0)
                throw new ArgumentOutOfRangeException(attackRangeParameterName, "지정 범위는 음수가 될 수 없습니다.");
            if (attackPower < 0)
                throw new ArgumentOutOfRangeException(nameof(attackPower), "공격 데미지는 음수가 될 수 없습니다.");
            if (maxActionCost < 0)
                throw new ArgumentOutOfRangeException(maxActionCostParameterName, "최대 행동 비용은 음수가 될 수 없습니다.");
        }
    }
}
