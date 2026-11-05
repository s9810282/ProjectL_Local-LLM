using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Entities;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Roster
{
    /// <summary>
    /// 정의·레벨과 보유 원본으로 계산한 배치 전 기준 수치다. 한 호출의 파생 결과이며 저장 원본이 아니다.
    /// 전투 생성과 보유 능력치 조회가 이 값을 각자의 형식으로 옮겨 담기만 한다. 위치·환경·전투 임시 효과는 포함하지 않는다.
    /// </summary>
    internal sealed class CharacterBaselineStats
    {
        /// <summary>영혼 반영 전 공격력이다. 메인은 지속 공격력, 서브·비보유 개체는 레벨로 성장한 정의 공격력이다.</summary>
        internal int BaseAttackPower { get; }
        internal int SoulAttackBonus { get; }
        internal int TraitAttackBonus { get; }
        internal int EffectiveAttackPower { get; }
        /// <summary>영혼 반영 전 최대 체력이다. 메인은 지속 최대 체력, 서브·비보유 개체는 레벨로 성장한 정의 최대 체력이다.</summary>
        internal int BaseMaxHealth { get; }
        internal int SoulMaxHealthBonus { get; }
        internal HealthStateSnapshot Health { get; }
        internal int MaxHealth => Health.MaxHealth;
        /// <summary>메인은 지속 현재 체력, 서브·비보유 개체는 최대 체력이다.</summary>
        internal int InitialCurrentHealth => Health.CurrentHealth;
        internal MoveMethod MoveMethod { get; }
        internal int MoveRange { get; }
        internal int AttackRange { get; }
        internal int PhysicalDefense { get; }
        internal int Priority { get; }

        /// <summary>
        /// <see cref="CharacterBaselineStatsCalculator"/>만 호출한다. 합산은 계산기와 <see cref="CharacterHealthRules"/>가 하고
        /// 여기서는 합의 불변식만 확인한다.
        /// </summary>
        internal CharacterBaselineStats(UnitDefinition definition,int baseAttackPower,int soulAttackBonus,            int effectiveAttackPower,int baseMaxHealth,int soulMaxHealthBonus,HealthStateSnapshot health,            int moveRange,int physicalDefense,int traitAttackBonus = 0)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));
            if (physicalDefense < 0)
                throw new ArgumentOutOfRangeException(nameof(physicalDefense));
            if (baseAttackPower < 0)
                throw new ArgumentOutOfRangeException(nameof(baseAttackPower));
            if (soulAttackBonus < 0)
                throw new ArgumentOutOfRangeException(nameof(soulAttackBonus));
            if (soulMaxHealthBonus < 0)
                throw new ArgumentOutOfRangeException(nameof(soulMaxHealthBonus));
            if (traitAttackBonus < 0) throw new ArgumentOutOfRangeException(nameof(traitAttackBonus));
            if (effectiveAttackPower != checked(baseAttackPower + soulAttackBonus + traitAttackBonus))
                throw new ArgumentException("최종 공격력은 기준 공격력과 영혼·특성 기여분의 합이어야 합니다.",
                    nameof(effectiveAttackPower));
            if (health.MaxHealth != checked(baseMaxHealth + soulMaxHealthBonus))
                throw new ArgumentException("최대 체력은 영혼 반영 전 최대 체력과 영혼 기여분의 합이어야 합니다.",
                    nameof(health));
            if (moveRange < 0)
                throw new ArgumentOutOfRangeException(nameof(moveRange));

            BaseAttackPower = baseAttackPower;
            SoulAttackBonus = soulAttackBonus;
            TraitAttackBonus = traitAttackBonus;
            EffectiveAttackPower = effectiveAttackPower;
            BaseMaxHealth = baseMaxHealth;
            SoulMaxHealthBonus = soulMaxHealthBonus;
            Health = health;
            MoveMethod = definition.MoveMethod;
            MoveRange = moveRange;

            AttackRange = definition.AttackRange;
            PhysicalDefense = physicalDefense;


            Priority = definition.Priority;

        }
    }
}
