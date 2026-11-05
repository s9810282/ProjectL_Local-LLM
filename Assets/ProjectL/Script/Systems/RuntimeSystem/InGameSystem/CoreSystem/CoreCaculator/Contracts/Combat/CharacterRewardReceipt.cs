using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>승리 정산에서 실제 반영한 보유 캐릭터별 경험치와 레벨의 앞뒤 값이다.</summary>
    public sealed class CharacterRewardReceipt
    {
        public CharacterInstanceId CharacterId { get; }
        public EntityKey CharacterKey { get; }
        public int KillExperience { get; }
        public int ClearExperience { get; }
        public int TotalExperience { get; }
        public int LevelBefore { get; }
        public int ExperienceBefore { get; }
        public int RequiredExperienceBefore { get; }
        public int LevelAfter { get; }
        public int ExperienceAfter { get; }
        public int RequiredExperienceAfter { get; }
        /// <summary>정산 순간 그 보유 아군의 스테이지 기록 사본이다. 전투 사실이 사라진 뒤 결과 화면이 읽는다.</summary>
        public CharacterStageRecord StageRecord { get; }

        /// <param name="stageRecord">
        /// 정산 순간의 전투 사실 기록이다. null이면 <see cref="CharacterStageRecord.Empty"/>다.
        /// 영수증을 만드는 정산은 늘 값을 넘기고, 보상 일관성 대조가 최종 전투 사실과 같은지 확인한다.
        /// </param>
        public CharacterRewardReceipt(CharacterInstanceId characterId, EntityKey characterKey, int killExperience,
            int clearExperience, int levelBefore, int experienceBefore, int requiredExperienceBefore,
            int levelAfter, int experienceAfter, int requiredExperienceAfter, CharacterStageRecord stageRecord = null)
        {
            if (characterId.IsEmpty || characterKey == EntityKey.None)
                throw new ArgumentException("보상 대상의 보유 ID와 정의 키가 필요합니다.");
            if (killExperience < 0 || clearExperience < 0 || levelBefore < 1 || levelAfter < levelBefore ||
                experienceBefore < 0 || experienceAfter < 0 || requiredExperienceBefore < 0 || requiredExperienceAfter < 0)
                throw new ArgumentOutOfRangeException(nameof(killExperience));
            TotalExperience = checked(killExperience + clearExperience);
            // 요구량 0은 진행 설정이 꺼진 경우만 표현한다.
            if (requiredExperienceBefore == 0 || requiredExperienceAfter == 0)
            {
                if (requiredExperienceBefore != 0 || requiredExperienceAfter != 0 || TotalExperience != 0 ||
                    levelBefore != levelAfter || experienceBefore != experienceAfter)
                    throw new ArgumentException("꺼진 경험치 설정의 영수증에 진행 변경을 넣을 수 없습니다.");
            }
            else if (experienceBefore >= requiredExperienceBefore || experienceAfter >= requiredExperienceAfter)
                throw new ArgumentException("진행 경험치는 해당 레벨 요구량보다 작아야 합니다.");
            CharacterId = characterId;
            CharacterKey = characterKey;
            KillExperience = killExperience;
            ClearExperience = clearExperience;
            LevelBefore = levelBefore;
            ExperienceBefore = experienceBefore;
            RequiredExperienceBefore = requiredExperienceBefore;
            LevelAfter = levelAfter;
            ExperienceAfter = experienceAfter;
            RequiredExperienceAfter = requiredExperienceAfter;
            StageRecord = stageRecord ?? CharacterStageRecord.Empty;
        }

        // ⚠ 필드를 추가·삭제할 때 SameValues와 ContractShapeGuardTests 목록을 함께 고친다.
        /// <summary>영수증 행의 값이 모두 같은지다. 합계(TotalExperience)는 처치·클리어 경험치에서 파생되므로 따로 비교하지 않는다.</summary>
        public bool SameValues(CharacterRewardReceipt other) =>
            CharacterId == other.CharacterId && CharacterKey == other.CharacterKey &&
            KillExperience == other.KillExperience && ClearExperience == other.ClearExperience &&
            LevelBefore == other.LevelBefore && ExperienceBefore == other.ExperienceBefore &&
            RequiredExperienceBefore == other.RequiredExperienceBefore && LevelAfter == other.LevelAfter &&
            ExperienceAfter == other.ExperienceAfter && RequiredExperienceAfter == other.RequiredExperienceAfter &&
            StageRecord.SameValues(other.StageRecord);
    }
}
