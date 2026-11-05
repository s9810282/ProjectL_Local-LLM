using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Rewards
{
    /// <summary>기존 승리 계산의 실제 반영 결과를 읽어 게시자가 사용할 영수증을 만든다. 경험치를 다시 적용하지 않는다.</summary>
    public static class BattleRewardReceiptCalculator
    {
        public static BattleRewardReceipt Create(CombatCommandTransition transition, int stageDepth, int stageWidthIndex)
        {
            if (transition == null) throw new ArgumentNullException(nameof(transition));
            var input = transition.Input;
            var settlement = transition.RewardSettlement;
            if (!transition.Accepted || transition.Outcome.Outcome != StageOutcome.Victory ||
                input.Battle?.ClearRewards == null || settlement == null || !settlement.HasClearReward)
                throw new InvalidOperationException("설정된 클리어 보상이 있는 수락된 승리만 영수증을 만들 수 있습니다.");
            return new BattleRewardReceipt(input.BattleInstanceId, stageDepth, stageWidthIndex,
                transition.FinalState.Rewards.KillGoldEarned, settlement.ClearGold, settlement.ClearFame,
                transition.FinalState.Rewards.ClearParticipantIds, settlement.Characters);
        }
    }
}
