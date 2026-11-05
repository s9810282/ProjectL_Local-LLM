using System;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Recovery.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Shop;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.InGameTable;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.RuntimeSystem.BufferSystems.StageView;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class TraitGrowthStageGateTests
    {
        private static readonly StageNodeId Target = new(2);
        private static void PrepareMap(TraitGrowthTestRuntime game, StageType type = StageType.Combat)
        {
            game.Buffers.StageProgressWriteSink.RegisterStages(1, new[] {
                new StageProgressData(1, 0, 0, 0, new[] { 2 }),
                new StageProgressData(2, 1101, 1, 0, Array.Empty<int>(), stageType: type) });
            Assert.That(game.Core.ChapterProgression.EnterChapter(1).Accepted, Is.True);
        }
        private static void Pending(ChapterProgressionResult result)
        {
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.FailureReason, Is.EqualTo(ChapterProgressionFailureReason.PendingTraitGrowth));
        }
        private static void AllPathsBlocked(TraitGrowthTestRuntime game)
        {
            Pending(game.Core.ChapterProgression.PrepareStageEntry(1, Target));
            Pending(game.Core.ChapterProgression.ConfirmStageEntry(1, Target));
            Pending(((ChapterProgressionCoordinator)game.Core.ChapterProgression).RevalidateStageEntry(1, Target));
            Pending(game.Core.BattleEntry.EnterBattle(1, Target));
            Pending(game.Core.Shop.EnterShop(1, Target, 17));
            Pending(game.Core.Recovery.EnterRecovery(1, Target));
            Assert.That(game.Buffers.CurrentProgressReadSink.HasSelection, Is.False);
            Assert.That(game.Buffers.CombatStateReadSink.HasBattle, Is.False);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void HighLevelAcquisition_BlocksAllNodeEntryPaths(bool main)
        {
            using var game = new TraitGrowthTestRuntime(main, lastOnly: true);
            PrepareMap(game);
            var before = game.Current;
            Assert.That(game.Buffers.TraitGrowthStateReadSink.ActiveOffer, Is.Null);
            AllPathsBlocked(game);
            var offer = game.Open();
            int number = game.Buffers.TraitGrowthViewWriteSink.Submit(game.Request(offer));
            Assert.That(TraitGrowthCommandTests.Process(game, number), Is.True);
            Assert.That(game.Buffers.TraitGrowthStateReadSink.ActiveOffer, Is.Null);
            Assert.That(game.Core.Characters.ReadTraitGrowthProgress().Status, Is.EqualTo(TraitGrowthProgressStatus.AwaitingAcknowledgement));
            AllPathsBlocked(game);
            Assert.That((game.Current.Level, game.Current.Experience), Is.EqualTo((before.Level, before.Experience)));
            Assert.That(game.Buffers.TraitGrowthViewWriteSink.AcknowledgeResult(offer.RunId, number), Is.True);
            Assert.That(game.Core.ChapterProgression.PrepareStageEntry(1, Target).Accepted, Is.True);
            Assert.That(game.Core.ChapterProgression.ConfirmStageEntry(1, Target).Accepted, Is.True);
        }

        [TestCase(StageType.Shop)]
        [TestCase(StageType.Event)]
        public void BufferedNodeEntry_CannotBypassPendingChoice(StageType type)
        {
            using var game = new TraitGrowthTestRuntime();
            game.Database.SetStage(type == StageType.Event ? ChapterDistributionTestData.RecoveryStage(1101, 1)
                : new StageRootData(1101, 1, StageMapRole.Regular, new ShopStageDetail(new[] { EntityKey.Unit_Ballista }, Array.Empty<int>(), 5)));
            game.Database.RecoveryOptions = new RecoveryTable();
            PrepareMap(game, type);
            int number = game.Buffers.StageViewWriteSink.Submit(StageViewCommandData.SelectStageNode(1, Target));
            Assert.That(game.Core.StageCommands.ProcessNextNodeCommand(number, () => 17), Is.True);
            Assert.That(game.Buffers.StageCommandResults.TryGetMapResult(out var result), Is.True);
            Pending(result.Result);
            Assert.That(game.Buffers.CurrentProgressReadSink.HasSelection, Is.False);
        }

        [Test]
        public void EmptyMainConfiguration_DoesNotBlockOrdinaryAdmission()
        {
            using var game = new TraitGrowthTestRuntime(main: true, enabled: false);
            PrepareMap(game);
            Assert.That(game.Core.Characters.ReadTraitGrowthProgress().Status, Is.EqualTo(TraitGrowthProgressStatus.ReadyToContinue));
            Assert.That(game.Core.ChapterProgression.PrepareStageEntry(1, Target).Accepted, Is.True);
            Assert.That(game.Core.BattleEntry.EnterBattle(1, Target).Accepted, Is.True);
            Assert.Throws<InvalidOperationException>(() => game.Core.BattleEntry.EnterBattle(1, Target));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void DefinitionRead_LevelMutationCannotBypassEntryGate(bool battle)
        {
            using var game = new TraitGrowthTestRuntime(initialLevel: 1, observeBattleDefinition: battle);
            PrepareMap(game);
            if (battle)
                game.AfterBattleDefinitionRead = () => game.SetTargetLevel(2);
            else
                game.Database.BeforeStageRead = _ =>
            {
                game.Database.BeforeStageRead = null;
                game.SetTargetLevel(2);
            };
            Pending(battle ? game.Core.BattleEntry.EnterBattle(1, Target) : game.Core.ChapterProgression.ConfirmStageEntry(1, Target));
            Assert.That(game.Buffers.CurrentProgressReadSink.HasSelection, Is.False);
            Assert.That(game.Buffers.CombatStateReadSink.HasBattle, Is.False);
        }

        [Test]
        public void ReadyToContinue_RetainsOtherAdmissionFailures()
        {
            using var game = new TraitGrowthTestRuntime(enabled: false);
            PrepareMap(game);
            Assert.That(game.Core.ChapterProgression.PrepareStageEntry(1, new StageNodeId(99)).Accepted, Is.False);
            game.EndRun();
            Assert.That(game.Core.ChapterProgression.PrepareStageEntry(1, Target).FailureReason, Is.EqualTo(ChapterProgressionFailureReason.RunEnded));
            Assert.That(game.Core.BattleEntry.EnterBattle(1, Target).FailureReason, Is.EqualTo(ChapterProgressionFailureReason.RunEnded));
        }

        [Test]
        public void GrowthRead_SourceMutationCannotReturnCachedReady()
        {
            using var game = new TraitGrowthTestRuntime(initialLevel: 1);
            PrepareMap(game);
            game.Tables.BeforeRead = () =>
            {
                game.Tables.BeforeRead = null;
                game.SetTargetLevel(2);
            };
            Pending(game.Core.ChapterProgression.ConfirmStageEntry(1, Target));
            Assert.That(game.Buffers.CurrentProgressReadSink.HasSelection, Is.False);
        }

        [Test]
        public void FrozenTargetUnsupportedCategoryChange_ReportsDiagnostic()
        {
            using var game = new TraitGrowthTestRuntime();
            var offer = game.Open();
            // 런타임 정의 불변 계약을 어긴 입력을 진단하며 다른 보유 대상으로 바꾸지 않는다.
            game.Database.SetUnit(new ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Item.UnitData(EntityKey.Unit_ShotGun,
                UnitRole.Regular, UnitOwnerEligibility.PlayerOnly, 1, 1, 10, 0, 2, CategoryKey.Heavy, 5, 1,
                Array.Empty<ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait.UnitTraitData>(), targetingMethodKey: 1));
            Assert.Throws<InvalidOperationException>(() => game.Core.Characters.ReadTraitGrowthProgress());
            Assert.That(game.Buffers.TraitGrowthStateReadSink.ActiveOffer, Is.SameAs(offer));
        }

        [Test]
        public void PendingGrowth_DoesNotGateFinalChapterBookkeeping()
        {
            using var game = new TraitGrowthTestRuntime(initialLevel: 1);
            var original = game.Database.GetStage(1101);
            game.Database.SetStage(new StageRootData(1101, 1, StageMapRole.Boss, original.StageData));
            game.Buffers.StageProgressWriteSink.RegisterStages(1, new[] {
                new StageProgressData(1, 0, 0, 0, new[] { 2 }),
                new StageProgressData(2, 1101, 1, 0, Array.Empty<int>(), stageType: StageType.Combat, mapRole: StageMapRole.Boss) });
            Assert.That(game.Core.ChapterProgression.EnterChapter(1).Accepted, Is.True);
            Assert.That(game.Core.BattleEntry.EnterBattle(1, Target).Accepted, Is.True);
            var completion = new BattleCompletionChanges(new CharacterChanges(Array.Empty<CharacterStateChange>(), Array.Empty<SoulCountChange>()),
                new StageOutcomeSnapshot(StageOutcome.Victory, StageOutcomeReason.EnemyHeadquartersDestroyed),
                ChapterProgressionResult.FinishStage(1, Target, new StageKey(1101), true));
            Assert.That(game.Buffers.BattleWriteSink.CompleteBattle(completion), Is.True);
            game.Buffers.BattleWriteSink.EndBattle();
            game.SetTargetLevel(2);
            Assert.That(game.Core.Characters.ReadTraitGrowthProgress().Status, Is.EqualTo(TraitGrowthProgressStatus.ChoiceRequired));
            var decision = game.Core.ChapterProgression.CompleteChapter(1, Target);
            Assert.That(decision.Accepted, Is.True, decision.FailureReason.ToString());
            Assert.That(game.Core.ChapterProgression.Outcome, Is.EqualTo(RunOutcome.Victory));
            Assert.That(game.Core.Characters.ReadTraitGrowthProgress().Status, Is.EqualTo(TraitGrowthProgressStatus.Unavailable));
        }

        private sealed class RecoveryTable : IRecoveryOptionTableQuery
        {
            private readonly RecoveryOptionData _option = new(1, 0, RecoveryHealMode.Full, 0);
            public RecoveryOptionData GetRecoveryOption(int key) => _option;
            public bool TryGetRecoveryOption(int key, out RecoveryOptionData data) { data = key == 1 ? _option : null; return data != null; }
            public IReadOnlyCollection<RecoveryOptionData> GetAllRecoveryOptions() => new[] { _option };
        }
    }
}
