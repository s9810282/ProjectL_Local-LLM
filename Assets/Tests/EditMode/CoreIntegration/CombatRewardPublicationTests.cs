using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class CombatRewardPublicationTests
    {
        [Test]
        public void TwoAcceptedKills_PayImmediatelyOnce_ThenVictoryAndCleanupDoNotRepay()
        {
            using var battle = Create();
            var b = battle.Buffers;
            var actorId = battle.Actor.OwnedId.Value;
            int spent = b.CurrencyReadSink.RunSpent;
            int kills = b.RunProgressReadSink.EnemiesDefeated;
            var first = Attack(battle, EntityKey.Unit_Demon);
            long firstTurn = first.Input.ExpectedControllerTurnSequence;
            int sequence = b.CombatStateReadSink.LatestResultSequence;
            b.BattleWriteSink.Apply(first);
            b.BattleWriteSink.Apply(first);
            Assert.That(b.CurrencyReadSink.Amount, Is.EqualTo(17));
            Assert.That(first.RewardSettlement.KillCount, Is.EqualTo(1));
            Assert.That(b.RunProgressReadSink.EnemiesDefeated, Is.EqualTo(kills + 1));
            Assert.That(b.CharacterReadSink.Owned.Single(o => o.Id == actorId).Experience, Is.Zero);
            Assert.That(battle.Actor.Experience.Earned, Is.EqualTo(10));
            Assert.That(b.CombatStateReadSink.LatestResultSequence, Is.EqualTo(sequence + 1));

            // 같은 유닛의 두 번째 처치는 실제 다음 플레이어 턴에서만 수행한다.
            battle.Submit(battle.EndPlayerTurn());
            battle.AdvanceToPlayerOrDay();
            Assert.That(battle.Actor.ActionUse, Is.EqualTo(UnitActionUse.Available));
            var second = Attack(battle, EntityKey.Unit_Gravekeeper);
            Assert.That(second.Input.ExpectedControllerTurnSequence, Is.GreaterThan(firstTurn));
            Assert.That(second.RewardChange.ExpectedGold, Is.EqualTo(17));
            Assert.That(second.Outcome.Outcome, Is.EqualTo(StageOutcome.Victory));
            sequence = b.CombatStateReadSink.LatestResultSequence;
            Assert.That(battle.Publication().Publisher.Publish(second), Is.True);
            var completed = second.WithCompletion(b.CombatStateReadSink.Completion);
            var once = new CoreIntegrationSnapshot(b);
            b.BattleWriteSink.Apply(completed);
            once.AssertUnchanged(b);
            Assert.That(b.CurrencyReadSink.Amount, Is.EqualTo(37));
            Assert.That(b.CombatStateReadSink.Current.CommandState.Rewards.KillGoldEarned, Is.EqualTo(30));
            Assert.That(b.CharacterReadSink.Owned.Single(o => o.Id == actorId).Experience, Is.EqualTo(30));
            Assert.That(b.CombatStateReadSink.LatestResultSequence, Is.EqualTo(sequence + 1));
            Assert.That(b.BattleWriteSink.CompleteBattle(completed.Completion), Is.False);
            once.AssertUnchanged(b);
            b.BattleWriteSink.EndBattle();
            Assert.That(b.CurrencyReadSink.Amount, Is.EqualTo(37));
            Assert.That(b.CharacterReadSink.Owned.Single(o => o.Id == actorId).Experience, Is.EqualTo(30));
            Assert.That(b.CurrencyReadSink.RunSpent, Is.EqualTo(spent));
            Assert.That(b.RunProgressReadSink.EnemiesDefeated, Is.EqualTo(kills + 2));
        }

        /// <summary>
        /// 유닛별 스테이지 기록 설계 D5~D7: 기록은 명령마다 쌓여 Buffer 조회값에 보이고, 줄어든 기록 게시는 거절되며,
        /// 승리 영수증 행에 복사된 뒤 EndBattle에서 전투 사실과 함께 사라진다(영수증은 남는다).
        /// </summary>
        [Test]
        public void StageRecord_AccumulatesPerCommand_IsCopiedToTheReceipt_AndEndsWithTheBattle()
        {
            // 보상 영수증은 클리어 보상이 있는 승리에서만 만들어진다. 재화가 바뀌지 않게 모든 값을 0으로 둔다.
            using var battle = Create(new StageClearRewardSpec(0, 0, 0));
            var b = battle.Buffers;
            var actorEntity = battle.Actor.EntityId;
            var actorOwned = battle.Actor.OwnedId.Value;
            int demonHealth = battle.Character(EntityKey.Unit_Demon).Health.CurrentHealth;

            b.BattleWriteSink.Apply(Attack(battle, EntityKey.Unit_Demon));
            var afterFirst = battle.Actor.StageRecord;
            Assert.That(afterFirst.Kills, Is.EqualTo(1));
            Assert.That(afterFirst.DamageDealt, Is.EqualTo(demonHealth));
            Assert.That(afterFirst.DistanceMoved, Is.Zero);
            Assert.That(b.CharacterReadSink.Characters.Single(c => c.EntityId == actorEntity).StageRecord.SameValues(afterFirst), Is.True);
            Assert.That(b.CharacterReadSink.Characters.Where(c => c.Owner == Owner.Enemy).All(c => c.StageRecord.IsEmpty), Is.True);

            battle.Submit(battle.EndPlayerTurn());
            battle.AdvanceToPlayerOrDay();
            Assert.That(battle.Actor.StageRecord.IsAtLeast(afterFirst), Is.True);
            var second = Attack(battle, EntityKey.Unit_Gravekeeper);
            Assert.That(second.Outcome.Outcome, Is.EqualTo(StageOutcome.Victory));

            // 명령 게시라도 기록을 줄이는 변경은 거절되고 어떤 저장소도 바뀌지 않는다.
            var lowered = second.Characters.Characters.Select(c => c.Final.EntityId != actorEntity ? c :
                new CharacterStateChange(c.Expected, c.Final.With(stageRecord: CharacterStageRecord.Empty))).ToArray();
            var tampered = WithCharacters(second, new CharacterChanges(lowered, second.Characters.Souls, second.Characters.Main,
                second.Characters.Created, second.Characters.Owned));
            var before = new CoreIntegrationSnapshot(b);
            Assert.That(Assert.Throws<InvalidOperationException>(() => b.BattleWriteSink.Apply(tampered)).Message,
                Does.Contain("스테이지 기록은 줄어들 수 없습니다"));
            before.AssertUnchanged(b);

            Assert.That(battle.Publication().Publisher.Publish(second), Is.True);
            var final = battle.Actor.StageRecord;
            Assert.That(final.Kills, Is.EqualTo(2));
            var row = b.CombatStateReadSink.Completion.Rewards.Characters.Single(r => r.CharacterId == actorOwned);
            Assert.That(row.StageRecord.SameValues(final), Is.True);

            b.BattleWriteSink.EndBattle();
            Assert.That(b.CharacterReadSink.Characters, Is.Empty);
            Assert.That(b.CombatStateReadSink.Completion.Rewards.Characters.Single(r => r.CharacterId == actorOwned)
                .StageRecord.SameValues(final), Is.True);
        }

        [TestCase("staleGold")]
        [TestCase("missingChange")]
        [TestCase("wrongDelta")]
        [TestCase("wrongExpected")]
        [TestCase("fameGrant")]
        [TestCase("missingCompletion")]
        [TestCase("wrongNode")]
        public void InvalidPublication_LeavesEveryStoreUnchanged(string failure)
        {
            using var battle = Create();
            var b = battle.Buffers;
            bool terminal = failure == "missingCompletion" || failure == "wrongNode";
            var valid = Attack(battle, terminal ? EntityKey.Unit_Gravekeeper : EntityKey.Unit_Demon);
            Assert.That(valid.Outcome.IsStageEnded, Is.EqualTo(terminal));
            var candidate = valid;
            var reward = valid.RewardChange;
            string guard;
            switch (failure)
            {
                case "staleGold":
                    b.CurrencyWriteSink.SetAmount(9);
                    guard = "재화 잔액";
                    break;
                case "missingChange":
                    candidate = WithReward(valid, null);
                    guard = "잔액 제안이 없습니다";
                    break;
                case "wrongDelta":
                    candidate = WithReward(valid, new CombatRewardChange(reward.ExpectedGold, reward.FinalGold + 1,
                        reward.ExpectedFame, reward.FinalFame));
                    guard = "보상 제안의 원본·수락·획득량";
                    break;
                case "wrongExpected":
                    candidate = WithReward(valid, new CombatRewardChange(reward.ExpectedGold - 1, reward.FinalGold,
                        reward.ExpectedFame, reward.FinalFame));
                    guard = "보상 제안의 원본·수락·획득량";
                    break;
                case "fameGrant":
                    candidate = WithReward(valid, new CombatRewardChange(reward.ExpectedGold, reward.FinalGold,
                        reward.ExpectedFame, reward.FinalFame + 1));
                    guard = "보상 제안의 원본·수락·획득량";
                    break;
                case "missingCompletion":
                    guard = "완료 제안";
                    break;
                case "wrongNode":
                    candidate = valid.WithCompletion(new BattleCompletionChanges(
                        new SoulActionChanges(valid.Characters, valid.Field, valid.ConsumedSoulIds), valid.Outcome,
                        ChapterProgressionResult.FinishStage(1, new StageNodeId(999), new StageKey(1101), false)));
                    guard = "선택";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(failure));
            }
            var before = new CoreIntegrationSnapshot(b);
            Assert.That(Assert.Throws<InvalidOperationException>(() => b.BattleWriteSink.Apply(candidate)).Message, Does.Contain(guard));
            before.AssertUnchanged(b);

            // 저장소를 되돌리지 않고 최신 잔액으로 같은 행동을 다시 계산한다.
            if (failure == "staleGold")
            {
                var i = valid.Input;
                valid = Calculate(i.WithPersistentFacts(i.Owned, i.ItemInventory, b.CurrencyReadSink.Amount, b.FameReadSink.Amount));
            }
            int gold = b.CurrencyReadSink.Amount;
            int sequence = b.CombatStateReadSink.LatestResultSequence;
            Assert.That(battle.Publication().Publisher.Publish(valid), Is.True);
            Assert.That(b.CurrencyReadSink.Amount, Is.EqualTo(gold + (terminal ? 20 : 10)));
            Assert.That(b.CombatStateReadSink.LatestResultSequence, Is.EqualTo(sequence + 1));
        }

        [Test]
        public void NonrewardCommandWithStaleBalance_IsAlsoRejected()
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config,
                new ConfiguredMovementBattle.EncounterSource(new CellCoord(0, 3)));
            var b = battle.Buffers;
            var input = battle.Publication().Attack(battle.Actor, new CellCoord(0, 3));
            var valid = Calculate(input);
            Assert.That(valid.RewardChange, Is.Null);
            b.CurrencyWriteSink.SetAmount(b.CurrencyReadSink.Amount + 2);
            var before = new CoreIntegrationSnapshot(b);
            Assert.That(Assert.Throws<InvalidOperationException>(() => b.BattleWriteSink.Apply(valid)).Message, Does.Contain("재화 잔액"));
            before.AssertUnchanged(b);
            var current = Calculate(input.WithPersistentFacts(input.Owned, input.ItemInventory, b.CurrencyReadSink.Amount, b.FameReadSink.Amount));
            Assert.That(battle.Publication().Publisher.Publish(current), Is.True);
            Assert.That(battle.Actor.ActionUse, Is.EqualTo(UnitActionUse.Attacked));
        }

        /// <param name="clearRewards">클리어 보상이다. 있으면 승리 완료가 보상 영수증을 갖는다. 기본은 없음(기존 시험의 재화 기대값).</param>
        private static ConfiguredMovementBattle Create(StageClearRewardSpec clearRewards = null)
        {
            var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, new RewardSource(clearRewards));
            battle.Buffers.CurrencyWriteSink.SetAmount(7);
            return battle;
        }

        private static CombatCommandTransition Attack(ConfiguredMovementBattle battle, EntityKey target) =>
            Calculate(battle.Publication().Attack(battle.Actor, battle.Character(target).Anchor.Value));

        private static CombatCommandTransition Calculate(CombatCommandInput input)
        {
            var result = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.ActionUseSettlement, Is.Not.Null);
            return result;
        }

        private static CombatCommandTransition WithReward(CombatCommandTransition t, CombatRewardChange reward) =>
            new(t.Input, t.Accepted, t.Reason, t.Characters, t.Field, t.FinalState, t.Outcome, t.Attack,
                t.ConsumedSoulIds, t.Completion, t.Movement, t.Events, t.PathMovement, t.EnvironmentBindings,
                t.BlockedContacts, t.InventoryChange, reward, t.RewardSettlement, t.ActionUseSettlement);

        private static CombatCommandTransition WithCharacters(CombatCommandTransition t, CharacterChanges characters) =>
            new(t.Input, t.Accepted, t.Reason, characters, t.Field, t.FinalState, t.Outcome, t.Attack,
                t.ConsumedSoulIds, t.Completion, t.Movement, t.Events, t.PathMovement, t.EnvironmentBindings,
                t.BlockedContacts, t.InventoryChange, t.RewardChange, t.RewardSettlement, t.ActionUseSettlement);

        private sealed class RewardSource : IBattleInitializationSource
        {
            private readonly StageClearRewardSpec _clearRewards;

            internal RewardSource(StageClearRewardSpec clearRewards = null) => _clearRewards = clearRewards;

            public BattleInitialization Read(IDataBaseQueryContext database, int chapterKey, StageKey stageKey)
            {
                var source = new ConfiguredMovementBattle.EncounterSource().Read(database, chapterKey, stageKey);
                var baseEnemy = source.Units.Single(u => u.Key == EntityKey.Unit_Demon);
                var units = source.Units.Where(u => u.Key != EntityKey.Unit_Demon).Concat(new[]
                {
                    Enemy(baseEnemy, EntityKey.Unit_Demon, 10), Enemy(baseEnemy, EntityKey.Unit_Gravekeeper, 20)
                }).ToArray();
                var stage = new StageConfig(8, 6, 8, 2, 3, 42, new[]
                {
                    new InitialEnemyPlacementSpec(EntityKey.Unit_Demon, new CellCoord(0, 3), Rotation4.R180),
                    new InitialEnemyPlacementSpec(EntityKey.Unit_Gravekeeper, new CellCoord(1, 3), Rotation4.R180, isVictoryTarget: true)
                }, Array.Empty<CellCoord>(), Array.Empty<StageGimmickSpec>(), Array.Empty<InitialEnemyStructurePlacementSpec>());
                var method = source.AttackProfiles.First().Method;
                return new BattleInitialization(stage, source.MainAnchor, source.MainRotation, units, source.Structures,
                    source.Categories, units.Select(u => new CombatAttackProfile(u, method)).ToArray(),
                    objectiveKind: BattleObjectiveKind.DesignatedBossDefeated, progression: new LevelProgressionSpec(150, 100, 3, 1, 1),
                    clearRewards: _clearRewards);
            }

            // 행동이 하나도 없는 적은 전투 준비가 구성 오류로 거절한다. 기본 공격만 허용하되 공격력 0·이동 없음으로 위치와 체력을 바꾸지 않는다.
            private static UnitSpec Enemy(UnitSpec s, EntityKey key, int reward) => new(key, s.Role,
                s.FootprintWidth, s.FootprintHeight, s.FootprintLocalCells.ToArray(), 1, s.MoveMethod, s.AttackRange,
                s.CategoryKey, 0, 0, s.Priority, s.Traits.ToArray(), s.OwnerEligibility, s.Occlusion,
                new UnitBehaviorSpec(false, true, false), killReward: new UnitKillRewardSpec(reward, reward));
        }
    }
}
