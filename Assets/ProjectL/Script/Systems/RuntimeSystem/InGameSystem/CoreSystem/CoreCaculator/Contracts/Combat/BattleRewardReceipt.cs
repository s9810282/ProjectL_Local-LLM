using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>한 전투 승리에 대해 게시한 보상과 완료 노드 좌표의 불변 영수증이다.</summary>
    public sealed class BattleRewardReceipt
    {
        public long BattleInstanceId { get; }
        public int StageDepth { get; }
        public int StageWidthIndex { get; }
        public int KillGoldEarned { get; }
        public int ClearGold { get; }
        public int TotalGoldEarned { get; }
        public int ClearFame { get; }
        public IReadOnlyList<CharacterInstanceId> ClearParticipantIds { get; }
        public IReadOnlyList<CharacterRewardReceipt> Characters { get; }

        public BattleRewardReceipt(long battleInstanceId, int stageDepth, int stageWidthIndex, int killGoldEarned,
            int clearGold, int clearFame, IReadOnlyList<CharacterInstanceId> clearParticipantIds,
            IReadOnlyList<CharacterRewardReceipt> characters)
        {
            if (battleInstanceId < 1) throw new ArgumentOutOfRangeException(nameof(battleInstanceId));
            if (stageDepth < 1 || stageWidthIndex < 0 || stageWidthIndex == int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(stageDepth));
            if (killGoldEarned < 0 || clearGold < 0 || clearFame < 0)
                throw new ArgumentOutOfRangeException(nameof(killGoldEarned));
            ClearParticipantIds = Array.AsReadOnly((clearParticipantIds ?? throw new ArgumentNullException(nameof(clearParticipantIds))).ToArray());
            Characters = Array.AsReadOnly((characters ?? throw new ArgumentNullException(nameof(characters))).ToArray());
            if (ClearParticipantIds.Any(id => id.IsEmpty) || ClearParticipantIds.Distinct().Count() != ClearParticipantIds.Count ||
                Characters.Any(c => c == null) || Characters.Select(c => c.CharacterId).Distinct().Count() != Characters.Count ||
                ClearParticipantIds.Any(id => !Characters.Any(c => c.CharacterId == id)) ||
                Characters.Any(c => c.ClearExperience > 0 && !ClearParticipantIds.Contains(c.CharacterId)))
                throw new ArgumentException("보상 영수증의 참여자와 캐릭터 목록이 일치하지 않습니다.");
            BattleInstanceId = battleInstanceId;
            StageDepth = stageDepth;
            StageWidthIndex = stageWidthIndex;
            KillGoldEarned = killGoldEarned;
            ClearGold = clearGold;
            TotalGoldEarned = checked(killGoldEarned + clearGold);
            ClearFame = clearFame;
        }
    }
}
