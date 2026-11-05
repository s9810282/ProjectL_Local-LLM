using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Entities;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Roster
{
    /// <summary>
    /// 정의·레벨·메인 지속 상태·영혼 수로 배치 전 기준 수치를 계산한다. 전투 생성과 보유 능력치 조회가 공유한다.
    /// 기준 공격력·최대 체력의 출처를 고른다: 메인은 지속 상태(저장된 성장 결과), 서브·적은 레벨 파생 값(<see cref="UnitLevelGrowthRules"/>).
    /// 체력은 <see cref="CharacterHealthRules"/>에 맡기고, 역할·소유 권한 검사는 호출자가 따로 한다.
    /// Buffer·DB·Unity를 참조하지 않는다.
    /// </summary>
    internal static class CharacterBaselineStatsCalculator
    {
        /// <summary>
        /// 보유 개체의 기준 수치다. 메인이면 지속 상태가 필요하고, 메인이 아니면 지속 상태를 받지 않는다.
        /// 서브는 보유 기록의 레벨로 성장한 값을 쓴다. 메인의 성장 결과는 지속 상태가 원본이므로 레벨 공식을 적용하지 않는다.
        /// </summary>
        internal static CharacterBaselineStats ForOwned(UnitDefinition definition, OwnedCharacterRecord owned,
            MainCharacterPersistentState main, int soulCount)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));
            if (owned == null)
                throw new ArgumentNullException(nameof(owned));
            if (soulCount < 0)
                throw new ArgumentOutOfRangeException(nameof(soulCount));

            bool isMain = owned.Role == CharacterOwnershipRole.Main;
            if (isMain && main == null)
                throw new InvalidOperationException("메인의 기준 수치에는 메인 지속 상태가 필요합니다.");
            if (!isMain && main != null)
                throw new InvalidOperationException("메인이 아닌 개체에는 메인 지속 상태를 넣을 수 없습니다.");

            if (isMain)
                return Create(definition,main.PersistentAttack,main.PersistentMaxHealth,soulCount,                    CharacterHealthRules.PersistentMainHealth(main, soulCount),                    definition.MoveRange,main.PersistentDefense,owned.HunterAttackBonus);

            return Leveled(definition, owned.Level, soulCount, owned.HunterAttackBonus);
        }

        /// <summary>보유 기록이 없는 적 개체(배치·출현·소환)의 기준 수치다. 주어진 레벨로 성장한 값을 쓰고 영혼 기여분은 0이다.</summary>
        internal static CharacterBaselineStats ForUnowned(UnitDefinition definition, int level)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            return Leveled(definition, level, 0);
        }

        private static CharacterBaselineStats Leveled(UnitDefinition definition, int level, int soulCount, int traitAttackBonus = 0)
        {
            int maxHealth = UnitLevelGrowthRules.MaxHealth(definition, level);
            return Create(definition,UnitLevelGrowthRules.AttackPower(definition, level),maxHealth,soulCount,                CharacterHealthRules.Health(maxHealth, soulCount, null),                definition.MoveRange,definition.PhysicalDefense,traitAttackBonus);
        }

        private static CharacterBaselineStats Create(UnitDefinition definition,int baseAttackPower,            int baseMaxHealth,int soulCount,HealthStateSnapshot health,int moveRange,            int physicalDefense,int traitAttackBonus = 0)
        {
            int soulAttackBonus = checked(soulCount * SoulGrowthRules.AttackBonusPerSoul);
            return new CharacterBaselineStats(definition,baseAttackPower,soulAttackBonus,                checked(baseAttackPower + soulAttackBonus + traitAttackBonus),baseMaxHealth,                CharacterHealthRules.SoulMaxHealthBonus(soulCount),health,moveRange,physicalDefense,traitAttackBonus);
        }
    }
}
