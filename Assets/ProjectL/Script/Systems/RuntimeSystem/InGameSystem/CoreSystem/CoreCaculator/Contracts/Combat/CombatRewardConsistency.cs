using System;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>
    /// 명령 전이 안의 보상 사실(정산·완료 영수증·경험치 반영 사건·잔액 제안)이 서로 맞는지 확인한다.
    /// Buffer 거래가 저장소 잔액 대조 바로 뒤에 부른다. 옛 Buffer 검사(BattleTransactions.PrepareRewards 등)를 순서·문구 그대로 옮겼다.
    /// 계약이지만 예외는 InvalidOperationException이다. 게시 동작(예외 형식·문구)을 바꾸지 않기 위해서다(설계 BD6).
    /// </summary>
    public static class CombatRewardConsistency
    {
        /// <returns>재화·명성 잔액을 바꿔야 하면 그 잔액 제안, 바꿀 것이 없으면 null.</returns>
        public static CombatRewardChange Validate(CombatCommandTransition transition)
        {
            if (transition == null) throw new ArgumentNullException(nameof(transition));
            var input = transition.Input;
            var change = transition.RewardChange;
            var settlement = transition.RewardSettlement;
            bool requiresClear = transition.Accepted && transition.Outcome.Outcome == StageOutcome.Victory &&
                input.Battle?.ClearRewards != null;
            int earned = checked(transition.FinalState.Rewards.KillGoldEarned - input.State.Rewards.KillGoldEarned);
            if (settlement == null)
            {
                if (earned != 0 || change != null || requiresClear || transition.Completion?.Rewards != null ||
                    transition.Events.OfType<CombatExperienceEvent>().Any(e => e.Kind == CombatEventKind.ExperienceApplied))
                    throw new InvalidOperationException("보상 효과를 게시하려면 확정된 정산 근거가 필요합니다.");
                return null;
            }
            if (!transition.Accepted || earned != settlement.KillGold || requiresClear != settlement.HasClearReward)
                throw new InvalidOperationException("정산 근거의 수락·처치 골드·클리어 여부가 계산 제안과 다릅니다.");
            ValidateReceipt(transition, settlement);
            ValidateExperienceSettlement(transition, settlement);
            int gold = settlement.GoldAwarded;
            int fame = settlement.ClearFame;
            if (change == null)
            {
                if (gold != 0 || fame != 0) throw new InvalidOperationException("보상에 대응하는 잔액 제안이 없습니다.");
                return null;
            }
            if (!transition.Accepted || !input.GoldBalance.HasValue || change.ExpectedGold != input.GoldBalance.Value ||
                change.ExpectedFame != (input.FameBalance ?? 0) || earned < 0 ||
                checked(change.FinalGold - change.ExpectedGold) != gold || checked(change.FinalFame - change.ExpectedFame) != fame ||
                fame != 0 && !input.FameBalance.HasValue)
                throw new InvalidOperationException("보상 제안의 원본·수락·획득량이 일치하지 않습니다.");
            return change;
        }

        private static void ValidateReceipt(CombatCommandTransition transition, CombatRewardSettlement settlement)
        {
            var receipt = transition.Completion?.Rewards;
            if (!settlement.HasClearReward)
            {
                if (receipt != null) throw new InvalidOperationException("클리어 정산이 없는 거래에 보상 영수증이 있습니다.");
                return;
            }
            var input = transition.Input;
            var participants = transition.FinalState.Rewards.ClearParticipantIds;
            if (receipt == null || receipt.BattleInstanceId != input.BattleInstanceId ||
                receipt.KillGoldEarned != transition.FinalState.Rewards.KillGoldEarned ||
                receipt.ClearGold != settlement.ClearGold || receipt.ClearFame != settlement.ClearFame ||
                !receipt.ClearParticipantIds.SequenceEqual(participants) || receipt.Characters.Count != settlement.Characters.Count ||
                receipt.Characters.Where((row, index) => !row.SameValues(settlement.Characters[index])).Any())
                throw new InvalidOperationException("완료 영수증의 전투·재화·참여자가 계산 제안과 다릅니다.");
        }

        private static void ValidateExperienceSettlement(CombatCommandTransition transition, CombatRewardSettlement settlement)
        {
            var input = transition.Input;
            var facts = input.Characters.ToDictionary(c => c.EntityId);
            foreach (var change in transition.Characters.Characters) facts[change.Final.EntityId] = change.Final;
            foreach (var created in transition.Characters.Created) facts.Add(created.EntityId, created);
            var applied = transition.Events.OfType<CombatExperienceEvent>()
                .Where(e => e.Kind == CombatEventKind.ExperienceApplied).ToArray();
            if (applied.Length != settlement.Characters.Count(c => c.TotalExperience > 0))
                throw new InvalidOperationException("정산 내역과 경험치 반영 사건의 수가 다릅니다.");
            foreach (var entry in settlement.Characters)
            {
                var recipients = facts.Values.Where(c => c.OwnedId == entry.CharacterId && c.Owner == Owner.Player).ToArray();
                var before = input.Owned.SingleOrDefault(o => o.Id == entry.CharacterId);
                if (before == null || recipients.Length != 1)
                    throw new InvalidOperationException("정산 대상의 보유 기록과 아군 전투 개체가 일치하지 않습니다.");
                var after = transition.Characters.Owned.SingleOrDefault(c => c.Expected.Id == before.Id)?.Final ?? before;
                if (entry.CharacterKey != before.CharacterKey || recipients[0].CharacterKey != before.CharacterKey ||
                    entry.LevelBefore != before.Level || entry.ExperienceBefore != before.Experience ||
                    entry.LevelAfter != after.Level || entry.ExperienceAfter != after.Experience)
                    throw new InvalidOperationException("정산 경험치의 앞뒤 값이 실제 보유 변경과 다릅니다.");
                if (!entry.StageRecord.SameValues(recipients[0].StageRecord))
                    throw new InvalidOperationException("정산 내역의 스테이지 기록이 최종 전투 사실과 다릅니다.");
                var matches = applied.Where(e => e.RecipientId == recipients[0].EntityId).ToArray();
                if (entry.TotalExperience == 0)
                {
                    if (entry.LevelBefore != entry.LevelAfter || entry.ExperienceBefore != entry.ExperienceAfter || matches.Length != 0)
                        throw new InvalidOperationException("획득 경험치가 없는 내역에 보유 진행 변경 또는 반영 사건이 있습니다.");
                }
                else if (matches.Length != 1 || matches[0].Amount != entry.TotalExperience ||
                    !matches[0].Owned.Expected.SameValues(before) || !matches[0].Owned.Final.SameValues(after))
                    throw new InvalidOperationException("정산 내역에 대응하는 경험치 반영 사실이 없습니다.");
            }
        }
    }
}
