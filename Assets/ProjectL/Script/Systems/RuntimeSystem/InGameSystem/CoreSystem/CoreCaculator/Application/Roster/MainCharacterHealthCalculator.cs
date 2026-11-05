using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Roster
{
    /// <summary>
    /// 전투 밖 메인 체력(지속 현재 체력, 지속 최대 체력 + 영혼 기여분)을 Systems에 공개하는 경계다.
    /// 공식은 갖지 않고 <see cref="CharacterHealthRules"/>에 맡긴다.
    /// </summary>
    public static class MainCharacterHealthCalculator
    {
        public static HealthStateSnapshot Calculate(MainCharacterPersistentState main, int soulCount)
        {
            if (main == null)
                throw new ArgumentNullException(nameof(main));
            if (soulCount < 0)
                throw new ArgumentOutOfRangeException(nameof(soulCount));

            return CharacterHealthRules.PersistentMainHealth(main, soulCount);
        }
    }
}
