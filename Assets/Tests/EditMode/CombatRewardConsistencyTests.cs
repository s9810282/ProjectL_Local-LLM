using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Rewards;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Tests.Core
{
    /// <summary>
    /// Buffer 계약 지식 이전 계획 B-5: Buffer에서 옮긴 보상 정산 일관성 검사가 Core 계산 결과를 통과시키고,
    /// 정산 근거가 빠지거나 어긋난 전이를 옛 문구 그대로 거절하는지 확인한다.
    /// 입력 도우미는 CombatKillRewardTests의 Input·WithReward와 같은 값이다.
    /// </summary>
    public sealed class CombatRewardConsistencyTests
    {
        [Test]
        public void KillTransition_ReturnsItsBalanceChange()
        {
            var result = CombatCommandCalculator.Calculate(Input(new UnitKillRewardSpec(10, 10)), Array.Empty<IMovementTileRule>());

            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(CombatRewardConsistency.Validate(result), Is.SameAs(result.RewardChange));
        }

        [Test]
        public void ExcludedReward_ReturnsNullWithoutThrowing()
        {
            var result = CombatCommandCalculator.Calculate(
                Input(UnitKillRewardSpec.Excluded, key: EntityKey.Unit_Ballista, goldBalance: null), Array.Empty<IMovementTileRule>());

            Assert.That(result.RewardChange, Is.Null);
            Assert.That(CombatRewardConsistency.Validate(result), Is.Null);
        }

        [Test]
        public void MissingSettlement_IsRejectedWithOldMessage()
        {
            var input = Input(new UnitKillRewardSpec(10, 10));
            var result = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            var omitted = new CombatCommandTransition(input, true, "", result.Characters, result.Field, result.FinalState,
                result.Outcome, events: result.Events, rewardChange: result.RewardChange);

            Assert.That(Assert.Throws<InvalidOperationException>(() => CombatRewardConsistency.Validate(omitted)).Message,
                Is.EqualTo("보상 효과를 게시하려면 확정된 정산 근거가 필요합니다."));
        }

        [Test]
        public void SettlementKillGoldMismatch_IsRejectedWithOldMessage()
        {
            var input = Input(new UnitKillRewardSpec(10, 10));
            var result = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            var s = result.RewardSettlement;
            var tampered = new CombatCommandTransition(input, true, "", result.Characters, result.Field, result.FinalState,
                result.Outcome, events: result.Events, rewardChange: result.RewardChange,
                rewardSettlement: new CombatRewardSettlement(s.KillCount, s.KillGold + 1, s.ClearGold, s.ClearFame,
                    s.HasClearReward, s.Characters));

            Assert.That(Assert.Throws<InvalidOperationException>(() => CombatRewardConsistency.Validate(tampered)).Message,
                Is.EqualTo("정산 근거의 수락·처치 골드·클리어 여부가 계산 제안과 다릅니다."));
        }

        [Test]
        public void ReceiptRow_SameValuesComparesEveryStoredValue()
        {
            var row = new CharacterRewardReceipt(new CharacterInstanceId(1), EntityKey.Unit_Akimbo, 5, 10, 1, 20, 150, 1, 35, 150);

            Assert.That(row.SameValues(new CharacterRewardReceipt(new CharacterInstanceId(1), EntityKey.Unit_Akimbo, 5, 10, 1, 20, 150, 1, 35, 150)), Is.True);
            Assert.That(row.SameValues(new CharacterRewardReceipt(new CharacterInstanceId(2), EntityKey.Unit_Akimbo, 5, 10, 1, 20, 150, 1, 35, 150)), Is.False);
            Assert.That(row.SameValues(new CharacterRewardReceipt(new CharacterInstanceId(1), EntityKey.Unit_Akimbo, 6, 10, 1, 20, 150, 1, 36, 150)), Is.False);
            Assert.That(row.SameValues(new CharacterRewardReceipt(new CharacterInstanceId(1), EntityKey.Unit_Akimbo, 5, 10, 1, 20, 150, 1, 35, 151)), Is.False);
        }

        [Test]
        public void Victory_ValidatesClearReceiptAndAppliedExperience()
        {
            var result = Victory();
            Assert.That(result.Outcome.Outcome, Is.EqualTo(StageOutcome.Victory));
            Assert.That(result.RewardSettlement.Characters.Count, Is.EqualTo(2));
            Assert.That(result.RewardSettlement.Characters.All(row => row.TotalExperience > 0), Is.True);
            Assert.That(result.Events.OfType<CombatExperienceEvent>().Count(e => e.Kind == CombatEventKind.ExperienceApplied), Is.EqualTo(2));
            Assert.That(result.Completion.Rewards.ClearGold, Is.EqualTo(100));
            Assert.That(result.Completion.Rewards.ClearFame, Is.EqualTo(50));
            Assert.That(CombatRewardConsistency.Validate(result), Is.SameAs(result.RewardChange));
        }

        [TestCase("missing")]
        [TestCase("battle")]
        [TestCase("gold")]
        [TestCase("row")]
        [TestCase("order")]
        public void Victory_InvalidReceipt_IsRejectedWithOldMessage(string failure)
        {
            var result = Victory();
            var r = result.Completion.Rewards;
            var rows = r.Characters.ToArray();
            if (failure == "row")
            {
                var row = rows[0];
                rows[0] = new CharacterRewardReceipt(row.CharacterId, row.CharacterKey, row.KillExperience, row.ClearExperience,
                    row.LevelBefore, row.ExperienceBefore + 1, row.RequiredExperienceBefore,
                    row.LevelAfter, row.ExperienceAfter, row.RequiredExperienceAfter);
            }
            if (failure == "order") Array.Reverse(rows);
            var receipt = failure == "missing" ? null : new BattleRewardReceipt(
                r.BattleInstanceId + (failure == "battle" ? 1 : 0), r.StageDepth, r.StageWidthIndex,
                r.KillGoldEarned + (failure == "gold" ? 1 : 0), r.ClearGold, r.ClearFame, r.ClearParticipantIds, rows);

            Assert.That(Assert.Throws<InvalidOperationException>(() =>
                CombatRewardConsistency.Validate(WithEvidence(result, receipt))).Message,
                Is.EqualTo("완료 영수증의 전투·재화·참여자가 계산 제안과 다릅니다."));
        }

        [TestCase("missing", "정산 내역과 경험치 반영 사건의 수가 다릅니다.")]
        [TestCase("duplicate", "정산 내역과 경험치 반영 사건의 수가 다릅니다.")]
        [TestCase("amount", "정산 내역에 대응하는 경험치 반영 사실이 없습니다.")]
        public void Victory_InvalidAppliedExperience_IsRejectedWithOldMessage(string failure, string message)
        {
            var result = Victory();
            var applied = result.Events.OfType<CombatExperienceEvent>().First(e => e.Kind == CombatEventKind.ExperienceApplied);
            IReadOnlyList<CombatEvent> events = failure == "missing"
                ? result.Events.Where(e => !ReferenceEquals(e, applied)).ToArray()
                : failure == "duplicate" ? result.Events.Append(applied).ToArray()
                : WrongExperienceAmount(result, applied);

            Assert.That(Assert.Throws<InvalidOperationException>(() =>
                CombatRewardConsistency.Validate(WithEvidence(result, result.Completion.Rewards, events))).Message,
                Is.EqualTo(message));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Victory_InvalidBalanceArithmetic_IsRejectedAfterEvidence(bool fame)
        {
            var result = Victory();
            var r = result.RewardChange;
            var wrong = new CombatRewardChange(r.ExpectedGold, r.FinalGold + (fame ? 0 : 1),
                r.ExpectedFame, r.FinalFame + (fame ? 1 : 0));
            Assert.That(Assert.Throws<InvalidOperationException>(() =>
                CombatRewardConsistency.Validate(WithEvidence(result, result.Completion.Rewards, rewardChange: wrong))).Message,
                Is.EqualTo("보상 제안의 원본·수락·획득량이 일치하지 않습니다."));
        }

        [Test]
        public void Victory_ReceiptThenExperienceThenBalance_PreservesFailureOrder()
        {
            var result = Victory();
            var applied = result.Events.OfType<CombatExperienceEvent>().First(e => e.Kind == CombatEventKind.ExperienceApplied);
            var wrongEvents = WrongExperienceAmount(result, applied);
            var r = result.RewardChange;
            var wrongBalance = new CombatRewardChange(r.ExpectedGold, r.FinalGold + 1, r.ExpectedFame, r.FinalFame);

            Assert.That(Assert.Throws<InvalidOperationException>(() =>
                CombatRewardConsistency.Validate(WithEvidence(result, null, wrongEvents, wrongBalance))).Message,
                Is.EqualTo("완료 영수증의 전투·재화·참여자가 계산 제안과 다릅니다."));
            Assert.That(Assert.Throws<InvalidOperationException>(() =>
                CombatRewardConsistency.Validate(WithEvidence(result, result.Completion.Rewards, wrongEvents, wrongBalance))).Message,
                Is.EqualTo("정산 내역에 대응하는 경험치 반영 사실이 없습니다."));
        }

        [Test]
        public void ConfiguredZeroClear_RequiresReceiptWithoutExperienceEvents()
        {
            var result = Victory(zero: true);
            Assert.That(result.RewardSettlement.HasClearReward, Is.True);
            Assert.That(result.RewardSettlement.Characters.Count, Is.EqualTo(2));
            Assert.That(result.RewardSettlement.Characters.All(row => row.TotalExperience == 0), Is.True);
            Assert.That(result.Events.OfType<CombatExperienceEvent>(), Is.Empty);
            Assert.That(CombatRewardConsistency.Validate(result), Is.Null);
            Assert.Throws<InvalidOperationException>(() => CombatRewardConsistency.Validate(WithEvidence(result, null)));
        }

        private static CombatEvent[] WrongExperienceAmount(CombatCommandTransition result, CombatExperienceEvent applied) =>
            result.Events.Select(e => ReferenceEquals(e, applied)
                ? CombatExperienceEvent.Applied(applied.RecipientId, applied.Owned, applied.Amount + 1,
                    applied.LevelsGained, applied.MainGrowth) : e).ToArray();

        private static CombatCommandTransition Victory(bool zero = false)
        {
            var input = Input(zero ? UnitKillRewardSpec.Excluded : new UnitKillRewardSpec(10, 10),
                clearRewards: zero ? new StageClearRewardSpec(0, 0, 0) : new StageClearRewardSpec(100, 50, 100));
            var result = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.Outcome.Outcome, Is.EqualTo(StageOutcome.Victory));
            var receipt = BattleRewardReceiptCalculator.Create(result, 1, 0);
            var progression = ChapterProgressionResult.FinishStage(1, new StageNodeId(1), new StageKey(1), true);
            return result.WithCompletion(new BattleCompletionChanges(result.Characters, result.Outcome, progression, receipt));
        }

        private static CombatCommandTransition WithEvidence(CombatCommandTransition source, BattleRewardReceipt receipt,
            IReadOnlyList<CombatEvent> events = null, CombatRewardChange rewardChange = null)
        {
            var completion = new BattleCompletionChanges(source.Characters, source.Outcome, source.Completion.Progression, receipt);
            return new CombatCommandTransition(source.Input, source.Accepted, source.Reason, source.Characters, source.Field,
                source.FinalState, source.Outcome, source.Attack, source.ConsumedSoulIds, completion, source.Movement,
                events ?? source.Events, source.PathMovement, source.EnvironmentBindings,
                source.BlockedContacts, source.InventoryChange, rewardChange ?? source.RewardChange, source.RewardSettlement, source.ActionUseSettlement);
        }

        private static CombatCommandInput Input(UnitKillRewardSpec reward, int level = 1,
            EntityKey key = EntityKey.Unit_Demon, int? goldBalance = 7, StageClearRewardSpec clearRewards = null)
        {
            var seed = CombatCommandTestData.Input(new TargetingMethodSpec(TargetingShape.RadialSquare,
                TargetingChoiceMode.Single, TargetingSplash.None, 0));
            var characters = seed.Characters.Select(c => c.Owner != Owner.Enemy ? c : new CharacterCombatFacts(
                c.EntityId, null, key, c.Owner, null, c.Area, new HealthStateSnapshot(1, 100), c.Stats,
                c.ReserveSlotIndex, c.Anchor, c.Rotation, c.OccupiedCells, c.ActionUse,
                experience: CharacterExperienceFacts.ForEnemy(level))).ToArray();
            var profiles = seed.Profiles.Select(p => p.Definition.Key != EntityKey.Unit_Demon ? p : new CombatAttackProfile(
                WithReward(p.Definition, key, reward), p.Method)).ToArray();
            var state = seed.State;
            if (clearRewards != null)
            {
                // 실제 공격으로 지정 보스를 처치하고, 진입 때 포착한 두 아군의 클리어 경험치를 정산한다.
                state = new CombatCommandState(state.BoardWidth,state.BoardHeight,state.Phase,                    state.IsNight,state.Structures,state.ThornFields,state.Turn,                    state.Environment,state.EnvironmentProgress,state.WallBlocking,BattleObjective.DesignatedBoss(new EntityId(2)),                    state.Allocation,state.ItemRandomState,new CombatRewardFacts(true, seed.Owned.Select(c => c.Id).ToArray(), 0));
            }
            return new CombatCommandInput(seed.Number, seed.Kind, seed.ActorId, seed.Aim, state, characters, profiles,
                seed.Main, seed.Owned, seed.SoulCounts, seed.Souls, seed.Artifacts,
                battle: new CombatBattleFacts(10, 37, 0, 0, Array.Empty<CharacterInstanceId>(), 0,
                    progression: new LevelProgressionSpec(150, 100, 3, 1, 1), clearRewards: clearRewards),
                battleInstanceId: clearRewards != null ? 1 : seed.BattleInstanceId, goldBalance: goldBalance, fameBalance: 0,
                expectedBattleInstanceId: seed.ExpectedBattleInstanceId, expectedControllerTurnSequence: seed.ExpectedControllerTurnSequence);
        }

        private static UnitSpec WithReward(UnitSpec s, EntityKey key, UnitKillRewardSpec reward) =>
            new(key, s.Role, s.FootprintWidth, s.FootprintHeight, s.FootprintLocalCells.ToArray(), s.MaxHealth,
                s.MoveMethod, s.AttackRange, s.CategoryKey, s.AttackPower, s.PhysicalDefense, s.Priority, s.Traits.ToArray(),
                s.OwnerEligibility, s.Occlusion, s.Behavior, killReward: reward);
    }
}
