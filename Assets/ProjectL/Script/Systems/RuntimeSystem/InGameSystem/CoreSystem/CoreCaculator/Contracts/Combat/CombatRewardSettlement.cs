using System;
using System.Collections.Generic;
using System.Linq;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>한 계산 제안에서 확정한 처치 수·재화 획득량과 승리 경험치 반영 내역이다. 현재 잔액을 소유하지 않는다.</summary>
    public sealed class CombatRewardSettlement
    {
        /// <summary>이 제안에서 확정한 처치 수다. 처치 골드·경험치와 같은 확정 처치 목록을 센다.</summary>
        public int KillCount { get; }
        public int KillGold { get; }
        public int ClearGold { get; }
        public int ClearFame { get; }
        public bool HasClearReward { get; }
        public IReadOnlyList<CharacterRewardReceipt> Characters { get; }
        public int GoldAwarded => checked(KillGold + ClearGold);

        public CombatRewardSettlement(int killCount, int killGold, int clearGold, int clearFame, bool hasClearReward,
            IReadOnlyList<CharacterRewardReceipt> characters)
        {
            if (killCount < 0)
                throw new ArgumentOutOfRangeException(nameof(killCount));
            if (killGold < 0 || clearGold < 0 || clearFame < 0)
                throw new ArgumentOutOfRangeException(nameof(killGold));
            if (killCount == 0 && killGold != 0)
                throw new ArgumentException("처치가 없는 정산에는 처치 골드를 넣을 수 없습니다.", nameof(killGold));
            _ = checked(killGold + clearGold);
            Characters = Array.AsReadOnly((characters ?? throw new ArgumentNullException(nameof(characters))).ToArray());
            if (Characters.Any(c => c == null) || Characters.Select(c => c.CharacterId).Distinct().Count() != Characters.Count)
                throw new ArgumentException("정산 캐릭터 목록에는 null이나 중복 보유 ID를 넣을 수 없습니다.", nameof(characters));
            if (!hasClearReward && (clearGold != 0 || clearFame != 0 || Characters.Any(c => c.ClearExperience != 0)))
                throw new ArgumentException("클리어 보상이 없는 정산에는 클리어 획득량을 넣을 수 없습니다.");
            KillCount = killCount;
            KillGold = killGold;
            ClearGold = clearGold;
            ClearFame = clearFame;
            HasClearReward = hasClearReward;
        }
    }
}
