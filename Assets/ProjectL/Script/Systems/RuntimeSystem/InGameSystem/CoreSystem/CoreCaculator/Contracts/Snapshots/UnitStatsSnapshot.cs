using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots
{
    /// <summary>
    /// View가 Unit의 이동, 공격, 행동 자원 및 Timeline 수치를 읽는 복사본이다.
    /// </summary>
    public readonly struct UnitStatsSnapshot
    {
        public MoveMethod MoveMethod { get; }
        public int MoveRange { get; }
        public int MoveCost { get; }
        public int AppointRange { get; }
        public int AttackRange => AppointRange;
        public int AttackPower { get; }
        /// <summary>Buffer 경로에서는 보유 카드 영혼 개수로 계산한다. 기존 Stage 경로는 이전 완료 전까지 별도다.</summary>
        public int SoulAttackBonus { get; }
        /// <summary>Buffer 경로에서는 보유 카드 영혼 개수로 계산한 최대 체력 보너스다.</summary>
        public int SoulMaxHealthBonus { get; }
        /// <summary>현재 Stage의 Artifact 사실 집합만 반영한 공격력 보너스다.</summary>
        public int ArtifactAttackBonus { get; }
        /// <summary>현재 위치에서만 적용하는 부호 있는 공격력 기여분이다.</summary>
        public int PositionAttackBonus { get; }
        /// <summary>기본 공격력과 Artifact·위치 보너스의 합이며 순간 Trait 조건은 포함하지 않는다.</summary>
        public int EffectiveAttackPower { get; }
        public int PhysicalDefense { get; }
        public int MaxActionCost { get; }
        public int AttackCost { get; }
        public int ActionPoint => MaxActionCost;
        public int RemainingMoveRange { get; }
        public int CurrentActionCost { get; }
        public int Priority { get; }
        public int Delay { get; }
        public ReadOnlyMemory<TraitStatusSnapshot> TraitStatuses { get; }
        public CategorySnapshot Category { get; }

        public UnitStatsSnapshot(
            MoveMethod moveMethod,
            int moveRange,
            int moveCost,
            int appointRange,
            int attackPower,
            int physicalDefense,
            int maxActionCost,
            int attackCost,
            int remainingMoveRange,
            int currentActionCost,
            int priority,
            int delay,
            TraitStatusSnapshot[] traitStatuses,
            CategorySnapshot category,
            int artifactAttackBonus = 0,
            int? effectiveAttackPower = null,
            int soulAttackBonus = 0,
            int soulMaxHealthBonus = 0,
            int positionAttackBonus = 0)
        {
            if (Enum.IsDefined(typeof(MoveMethod), moveMethod) == false)
                throw new ArgumentOutOfRangeException(nameof(moveMethod));
            if (moveRange < 0)
                throw new ArgumentOutOfRangeException(nameof(moveRange));
            if (moveCost < 0)
                throw new ArgumentOutOfRangeException(nameof(moveCost));
            if (appointRange < 0)
                throw new ArgumentOutOfRangeException(nameof(appointRange));
            if (attackPower < 0)
                throw new ArgumentOutOfRangeException(nameof(attackPower));
            if (artifactAttackBonus < 0)
                throw new ArgumentOutOfRangeException(nameof(artifactAttackBonus));
            if (soulAttackBonus < 0 || soulAttackBonus > artifactAttackBonus)
                throw new ArgumentOutOfRangeException(nameof(soulAttackBonus));
            if (soulMaxHealthBonus < 0)
                throw new ArgumentOutOfRangeException(nameof(soulMaxHealthBonus));
            int resolvedEffectiveAttackPower = effectiveAttackPower ??
                checked(attackPower + artifactAttackBonus + positionAttackBonus);
            if (resolvedEffectiveAttackPower < 0 ||
                resolvedEffectiveAttackPower != checked(attackPower + artifactAttackBonus + positionAttackBonus))
                throw new ArgumentException(
                    "Effective attack power must be nonnegative and equal base plus Artifact and position bonuses.",
                    nameof(effectiveAttackPower));
            if (physicalDefense < 0)
                throw new ArgumentOutOfRangeException(nameof(physicalDefense));
            if (maxActionCost < 0)
                throw new ArgumentOutOfRangeException(nameof(maxActionCost));
            if (attackCost < 0)
                throw new ArgumentOutOfRangeException(nameof(attackCost));
            if (remainingMoveRange < 0 || remainingMoveRange > moveRange)
                throw new ArgumentOutOfRangeException(nameof(remainingMoveRange));
            if (currentActionCost < 0 || currentActionCost > maxActionCost)
                throw new ArgumentOutOfRangeException(nameof(currentActionCost));
            if (priority < 0)
                throw new ArgumentOutOfRangeException(nameof(priority));
            if (delay < 0)
                throw new ArgumentOutOfRangeException(nameof(delay));
            if (traitStatuses == null)
                throw new ArgumentNullException(nameof(traitStatuses));
            if (category == null)
                throw new ArgumentNullException(nameof(category));

            TraitStatusSnapshot[] copiedTraitStatuses =
                new TraitStatusSnapshot[traitStatuses.Length];
            for (int i = 0; i < traitStatuses.Length; i++)
            {
                if (traitStatuses[i] == null)
                    throw new ArgumentException(
                        "Trait Status 목록에는 null 항목을 넣을 수 없습니다.",
                        nameof(traitStatuses));

                copiedTraitStatuses[i] = traitStatuses[i];
            }

            MoveMethod = moveMethod;
            MoveRange = moveRange;
            MoveCost = moveCost;
            AppointRange = appointRange;
            AttackPower = attackPower;
            SoulAttackBonus = soulAttackBonus;
            SoulMaxHealthBonus = soulMaxHealthBonus;
            ArtifactAttackBonus = artifactAttackBonus;
            PositionAttackBonus = positionAttackBonus;
            EffectiveAttackPower = resolvedEffectiveAttackPower;
            PhysicalDefense = physicalDefense;
            MaxActionCost = maxActionCost;
            AttackCost = attackCost;
            RemainingMoveRange = remainingMoveRange;
            CurrentActionCost = currentActionCost;
            Priority = priority;
            Delay = delay;
            TraitStatuses = copiedTraitStatuses;
            Category = category;
        }
    }
}
