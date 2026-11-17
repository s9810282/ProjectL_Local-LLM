using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster
{
    /// <summary>
    /// 보유 카드의 레벨·진행 경험치에 대한 변경 제안이며 저장 상태를 직접 바꾸지 않는다.
    /// 식별(ID·정의 키·역할)은 바꿀 수 없고 (레벨, 진행 경험치)는 줄어들지 않는다. 레벨업 판단은 Core 규칙이 한다.
    /// </summary>
    public sealed class OwnedCharacterChange
    {
        public OwnedCharacterRecord Expected { get; }
        public OwnedCharacterRecord Final { get; }

        public OwnedCharacterChange(OwnedCharacterRecord expected, OwnedCharacterRecord final)
        {
            Expected = expected ?? throw new ArgumentNullException(nameof(expected));
            Final = final ?? throw new ArgumentNullException(nameof(final));

            if (expected.Id != final.Id || expected.CharacterKey != final.CharacterKey || expected.Role != final.Role)
                throw new ArgumentException("보유 기록 변경은 ID·정의 키·역할을 바꿀 수 없습니다.", nameof(final));
            if (expected.SpecializationCategory != final.SpecializationCategory)
                throw new ArgumentException("일반 보유 변경은 전직 병과를 바꿀 수 없습니다.", nameof(final));
            if (expected.PlayerSkillUpgrade != final.PlayerSkillUpgrade)
                throw new ArgumentException("일반 보유 변경은 스킬 강화를 바꿀 수 없습니다.", nameof(final));
            if (!expected.SameGrowth(final))
                throw new ArgumentException("경험치 변경은 특성 성장을 바꿀 수 없습니다.", nameof(final));
            if (final.HunterAttackBonus < expected.HunterAttackBonus)
                throw new ArgumentException("Run 특성 기여분은 줄어들 수 없습니다.", nameof(final));
            if (final.Level < expected.Level || final.Level == expected.Level && final.Experience < expected.Experience)
                throw new ArgumentException("보유 기록의 레벨과 진행 경험치는 줄어들 수 없습니다.", nameof(final));
        }
    }
}
