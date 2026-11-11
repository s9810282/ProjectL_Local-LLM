using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Roster
{
    /// <summary>
    /// 경험치 요구량·레벨업 공식의 유일한 위치다.
    /// 요구량(L) = 역할별 레벨당 요구량 × L이고, 진행 경험치는 현재 레벨 안의 값이다. 레벨이 오르면 요구량을 빼고 남은 값을 넘긴다.
    /// 획득을 언제 누구에게 주는지와 능력치 성장은 다루지 않는다.
    /// </summary>
    internal static class ExperienceRules
    {
        /// <summary>레벨 <paramref name="level"/>에서 다음 레벨로 오르는 데 필요한 경험치다. 켜진 설정에서만 의미가 있다.</summary>
        internal static int Required(CharacterOwnershipRole role, int level, LevelProgressionSpec spec)
        {
            if (!spec.IsEnabled)
                throw new InvalidOperationException("꺼진 경험치 설정에는 요구량이 없습니다.");
            if (level < 1)
                throw new ArgumentOutOfRangeException(nameof(level));

            int perLevel = role switch
            {
                CharacterOwnershipRole.Main => spec.MainExperiencePerLevel,
                CharacterOwnershipRole.Sub => spec.SubExperiencePerLevel,
                _ => throw new ArgumentOutOfRangeException(nameof(role))
            };
            return checked(perLevel * level);
        }

        /// <summary>
        /// 보유 기록의 다음 레벨 진행 조회값이다. 꺼진 설정이면 요구량 0의 꺼짐 값이다.
        /// 켜진 설정에서 진행 경험치가 현재 레벨 요구량 이상이면 깨진 기록이므로 예외다(진행값 상한의 유일한 검사).
        /// </summary>
        internal static ExperienceProgressSnapshot Progress(OwnedCharacterRecord record, LevelProgressionSpec spec)
        {
            if (record == null)
                throw new ArgumentNullException(nameof(record));
            if (!spec.IsEnabled)
                return new ExperienceProgressSnapshot(false, record.Experience, 0);
            int required = Required(record.Role, record.Level, spec);
            if (record.Experience >= required)
                throw new InvalidOperationException("보유 기록의 진행 경험치가 현재 레벨 요구량 이상입니다.");
            return new ExperienceProgressSnapshot(true, record.Experience, required);
        }

        /// <summary>
        /// 보유 기록에 경험치를 더한 결과다. 요구량을 채울 때마다 레벨을 1 올리고 남은 값을 넘긴다(여러 단계 상승 가능).
        /// 꺼진 설정이거나 획득량이 0이면 요구량을 계산하지 않고 같은 기록을 돌려준다.
        /// </summary>
        internal static ExperienceGain Gain(OwnedCharacterRecord record, int amount, LevelProgressionSpec spec)
        {
            if (record == null)
                throw new ArgumentNullException(nameof(record));
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount));
            if (!spec.IsEnabled || amount == 0)
                return new ExperienceGain(record, 0);
            int level = record.Level;
            int required = Progress(record, spec).Required;

            int experience = checked(record.Experience + amount);
            while (experience >= required)
            {
                experience -= required;
                level = checked(level + 1);
                required = Required(record.Role, level, spec);
            }

            return new ExperienceGain(new OwnedCharacterRecord(record.Id, record.CharacterKey, record.Role, level, experience,
                record.Growth, record.HunterAttackBonus, record.SpecializationCategory),
                level - record.Level);
        }
    }

    /// <summary>경험치를 더한 결과다. 오른 단계 수는 메인 성장 선택 같은 레벨당 처리가 쓴다.</summary>
    internal readonly struct ExperienceGain
    {
        internal OwnedCharacterRecord Final { get; }
        internal int LevelsGained { get; }

        internal ExperienceGain(OwnedCharacterRecord final, int levelsGained)
        {
            Final = final;
            LevelsGained = levelsGained;
        }
    }
}
