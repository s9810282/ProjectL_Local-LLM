using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots
{
    /// <summary>
    /// 보유 유닛의 배치 전 능력치다. 영혼과 메인 지속 성장이 반영돼 있고 위치·환경·전투 임시 효과는 없다.
    /// 메인 <see cref="CurrentHealth"/>는 지속 체력, 서브는 다음 전투를 시작할 때의 체력이다. 전투 잔여 체력 조회에 쓰지 않는다.
    /// </summary>
    public sealed class CharacterBaselineStatsSnapshot
    {
        public int CurrentHealth { get; }
        public int MaxHealth { get; }
        /// <summary>영혼 기여분을 포함한 기준 공격력이다. View에서 영혼을 다시 더하지 않는다.</summary>
        public int AttackPower { get; }
        public int PhysicalDefense { get; }
        public MoveMethod MoveMethod { get; }
        public int MoveRange { get; }
        public int AttackRange { get; }
        public int Priority { get; }
        public CategoryKey CategoryKey { get; }
        /// <summary>보유 Main의 추가 병과다. CategoryKey의 정체성 의미는 유지한다.</summary>
        public CategoryKey? SpecializationCategory { get; }
        public CategoryKey CombatCategoryKey => SpecializationCategory ?? CategoryKey;
        /// <summary>정의의 정적 특성 키다. 명칭·아이콘·설명문은 View DB가 담당한다.</summary>
        public ReadOnlyMemory<TraitKey> TraitKeys { get; }

        public CharacterBaselineStatsSnapshot(int currentHealth,int maxHealth,int attackPower,            int physicalDefense,MoveMethod moveMethod,int moveRange,int attackRange,int priority,CategoryKey categoryKey,            IReadOnlyList<TraitKey> traitKeys, CategoryKey? specializationCategory = null)
        {
            if (maxHealth < 1)
                throw new ArgumentOutOfRangeException(nameof(maxHealth));
            if (currentHealth < 0 || currentHealth > maxHealth)
                throw new ArgumentOutOfRangeException(nameof(currentHealth));
            if (attackPower < 0)
                throw new ArgumentOutOfRangeException(nameof(attackPower));
            if (physicalDefense < 0)
                throw new ArgumentOutOfRangeException(nameof(physicalDefense));
            if (Enum.IsDefined(typeof(MoveMethod), moveMethod) == false)
                throw new ArgumentOutOfRangeException(nameof(moveMethod));
            if (moveRange < 0)
                throw new ArgumentOutOfRangeException(nameof(moveRange));
            if (attackRange < 0)
                throw new ArgumentOutOfRangeException(nameof(attackRange));
            if (priority < 0)
                throw new ArgumentOutOfRangeException(nameof(priority));
            if (categoryKey == CategoryKey.None || Enum.IsDefined(typeof(CategoryKey), categoryKey) == false)
                throw new ArgumentOutOfRangeException(nameof(categoryKey));
            if (traitKeys == null)
                throw new ArgumentNullException(nameof(traitKeys));
            if (specializationCategory.HasValue && (categoryKey != CategoryKey.Main ||
                specializationCategory != CategoryKey.Rider && specializationCategory != CategoryKey.Heavy && specializationCategory != CategoryKey.Cannon))
                throw new ArgumentException("Main만 Rider/Heavy/Cannon 전직 병과를 가질 수 있습니다.", nameof(specializationCategory));

            var copiedTraitKeys = new TraitKey[traitKeys.Count];
            for (int i = 0; i < copiedTraitKeys.Length; i++)
            {
                TraitKey key = traitKeys[i];
                if (key == TraitKey.None || Enum.IsDefined(typeof(TraitKey), key) == false)
                    throw new ArgumentException("특성 키는 정의된 값이어야 합니다.", nameof(traitKeys));
                copiedTraitKeys[i] = key;
            }

            CurrentHealth = currentHealth;
            MaxHealth = maxHealth;
            AttackPower = attackPower;
            PhysicalDefense = physicalDefense;
            MoveMethod = moveMethod;
            MoveRange = moveRange;

            AttackRange = attackRange;


            Priority = priority;

            CategoryKey = categoryKey;
            SpecializationCategory = specializationCategory;
            TraitKeys = copiedTraitKeys;
        }
    }
}
