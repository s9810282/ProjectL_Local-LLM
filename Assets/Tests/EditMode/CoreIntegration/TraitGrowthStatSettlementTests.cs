using System;
using System.Linq;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.TraitGrowth.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Combat;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using NUnit.Framework;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Category.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class TraitGrowthStatSettlementTests
    {
        [Test]
        public void RankedDefault_RemainsInstanceLocalAcrossRecallAndClearsOnNewRun()
        {
            var key = TraitKey.ReconSquad;
            var config = new CategoryConfigData(3, 5, 3, 2, 0, defaultTraits:
                new System.Collections.Generic.Dictionary<CategoryKey, TraitKey> { [CategoryKey.Rider] = key });
            var database = new CoreIntegrationTestData(config);
            var tables = new TraitGrowthTestRuntime.GrowthTables(CategoryKey.Rider, false);
            tables.ReplaceCategory(new CategoryTraitProgressionData(CategoryKey.Rider,
                Array.Empty<TraitGrowthStepSpec>(), Array.Empty<TraitKey>()));
            tables.ReplaceDefinition(TraitKey.Vanguard, new TraitGrowthDefinitionData(new ReconSquadTraitData(10, 1),
                null, "", "", "", "", new TraitRankValues(key, 15), new TraitRankValues(key, 20)));
            database.CategoryTraitProgressions = tables;
            database.TraitGrowthDefinitions = tables;
            database.SetStage(new StageRootData(1101, 1, StageMapRole.Regular, new CombatStageDetail(42, "field", 1, 0, 1,
                Array.Empty<EntityPlacement>(), Array.Empty<EntityKey>(), Array.Empty<EntityPlacement>(), EntityKey.Structure_Headquarters,
                CellCoord.Zero, Rotation4.R0, new CellCoord(7, 5), Rotation4.R180, null)));
            using var battle = new ConfiguredMovementBattle(config, database: database, configureOwned: buffers => SeedPurchase(buffers, key));
            var id = battle.Actor.OwnedId.Value;
            Assert.That(battle.OwnedStats.TraitRanks[key], Is.EqualTo(2));
            Assert.That(battle.OwnedStats.ReconSquadDescription.IncreasePercent, Is.EqualTo(15));
            int Increase(CharacterCombatFacts actor) =>
                actor.Stats.TraitStatuses.ToArray().OfType<ReconSquadTraitStatusSnapshot>().Single().IncreasePercent;
            Assert.That(Increase(battle.Actor), Is.EqualTo(15));
            Assert.That(Increase(battle.Character(EntityKey.Unit_DynamiteHammer)), Is.EqualTo(10));
            for (int i = 0; i < 8; i++)
            {
                battle.Submit(battle.EndPlayerTurn());
                battle.AdvanceToPlayerOrDay();
            }
            Assert.That(battle.Actor.Area, Is.EqualTo(CharacterRuntimeArea.Hand));
            Assert.That(Increase(battle.Actor), Is.EqualTo(15));
            battle.Core.BattleCompletion.AbandonBattle();
            battle.Core.BattleCompletion.EndBattle();
            Assert.That(battle.Buffers.CharacterReadSink.Owned.Single(o => o.Id == id).Growth.Purchases[key], Is.EqualTo(1));
            Assert.That(battle.Core.RunStart.StartNewRun(1).Succeeded, Is.True);
            Assert.That(battle.Buffers.CharacterReadSink.Owned.All(o => o.Growth.Purchases.Count == 0), Is.True);
        }
        internal static void SeedPurchase(ProjectL.Systems.RuntimeSystem.BufferSystems.Core.IBufferQuery buffers, TraitKey key)
        {
            // 구매 정산은 후속 단계다. 이 픽스처는 전투 진입 전 보유 이력만 설치한다.
            var roster = buffers.CharacterReadSink.Owned.ToList();
            var card = roster.Single(o => o.CharacterKey == EntityKey.Unit_Cowboy);
            long revision = buffers.CharacterReadSink.RosterRevision;
            ((ICharacterBufferStore)buffers.CharacterReadSink).InstallGrowth(new PreparedTraitGrowthChange(
                roster.IndexOf(card), card.WithGrowth(card.Growth.WithPurchase(key)), revision + 1, revision));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void VictoryGrowth_ChoiceFailureRetryAndAcknowledgementNeverReplayStatSettlement(bool main)
        {
            using var disabled = Create(main, false);
            using var enabled = Create(main, true);
            CompleteVictory(disabled);
            CompleteVictory(enabled);

            var buffers = enabled.Buffers;
            var records = buffers.CharacterReadSink.Owned.ToArray();
            var disabledRecords = disabled.Buffers.CharacterReadSink.Owned.ToArray();
            var mainId = records.Single(r => r.Role == CharacterOwnershipRole.Main).Id;
            Assert.That(buffers.CharacterReadSink.TryGetMainState(mainId, out var settledMain), Is.True);
            Assert.That(disabled.Buffers.CharacterReadSink.TryGetMainState(mainId, out var disabledMain), Is.True);
            Assert.That(settledMain.SameValues(disabledMain), Is.True);
            Assert.That(records.Select(r => (r.Id, r.Level, r.Experience)),
                Is.EqualTo(disabledRecords.Select(r => (r.Id, r.Level, r.Experience))));
            Assert.That(records.All(r => r.Level >= 3), Is.True, "실제 승리 정산이 여러 레벨을 한 번에 올려야 한다.");
            Assert.That(disabled.Core.Characters.ReadTraitGrowthProgress().Status,
                Is.EqualTo(TraitGrowthProgressStatus.ReadyToContinue));
            Assert.That(enabled.Core.Characters.ReadTraitGrowthProgress().Status,
                Is.EqualTo(TraitGrowthProgressStatus.ChoiceRequired));

            var baselineStats = enabled.Core.CharacterStats.ReadOwned();
            var target = records.Single(r => r.CharacterKey == (main ? EntityKey.Unit_Akimbo : EntityKey.Unit_Cowboy));
            var offer = enabled.Core.Characters.PrepareNextTraitGrowthOffer().ActiveOffer;
            Assert.That(offer.OwnedId, Is.EqualTo(target.Id));
            long revision = buffers.CharacterReadSink.RosterRevision;
            // 현재 병과에서 획득 가능하지만 고정 제안에는 없었던 키를 제출한다.
            var validKeys = new[] { TraitKey.SoloAction, TraitKey.HuntersMark, TraitKey.Vanguard, TraitKey.TrackingShot };
            var notOffered = validKeys.Single(k => !offer.Choices.Any(c => c.TraitKey == k));
            var failure = Publish(enabled, Request(offer, notOffered));
            Assert.That(failure.Result.FailureReason, Is.EqualTo(TraitGrowthFailureReason.NotCandidate));
            Assert.That(buffers.CharacterReadSink.RosterRevision, Is.EqualTo(revision));
            AssertSettlementPreserved();
            Assert.That(buffers.TraitGrowthViewWriteSink.AcknowledgeResult(offer.RunId, failure.Number), Is.True);
            AssertSettlementPreserved();
            Assert.That(enabled.Core.Characters.PrepareNextTraitGrowthOffer().ActiveOffer, Is.SameAs(offer));

            var acquired = Publish(enabled, Request(offer, offer.Choices[0].TraitKey));
            Assert.That(acquired.Result.Succeeded, Is.True);
            Assert.That(buffers.CharacterReadSink.RosterRevision, Is.EqualTo(revision + 1));
            Assert.That(enabled.Core.Characters.ReadTraitGrowthProgress().Status,
                Is.EqualTo(TraitGrowthProgressStatus.AwaitingAcknowledgement));
            AssertSettlementPreserved();
            Assert.That(buffers.TraitGrowthViewWriteSink.AcknowledgeResult(offer.RunId, acquired.Number), Is.True);
            AssertSettlementPreserved();

            var enhancement = enabled.Core.Characters.PrepareNextTraitGrowthOffer().ActiveOffer;
            Assert.That(enhancement.OwnedId, Is.EqualTo(target.Id));
            var enhanced = Publish(enabled, Request(enhancement, TraitKey.Knockback));
            Assert.That(enhanced.Result.Succeeded, Is.True);
            Assert.That(buffers.CharacterReadSink.RosterRevision, Is.EqualTo(revision + 2));
            Assert.That(enhanced.Result.Final.Growth.Selections.Count, Is.EqualTo(2));
            AssertSettlementPreserved();
            Assert.That(buffers.TraitGrowthViewWriteSink.AcknowledgeResult(enhancement.RunId, enhanced.Number), Is.True);
            AssertSettlementPreserved();
            // 같은 결과를 다시 확인해도 상태를 바꾸지 않고 성공한다.
            Assert.That(buffers.TraitGrowthViewWriteSink.AcknowledgeResult(enhancement.RunId, enhanced.Number), Is.True);
            AssertSettlementPreserved();

            void AssertSettlementPreserved()
            {
                var current = buffers.CharacterReadSink.Owned;
                Assert.That(current.Select(r => (r.Id, r.CharacterKey, r.Role, r.Level, r.Experience)),
                    Is.EqualTo(records.Select(r => (r.Id, r.CharacterKey, r.Role, r.Level, r.Experience))));
                Assert.That(buffers.CharacterReadSink.TryGetMainState(mainId, out var currentMain), Is.True);
                Assert.That(currentMain, Is.SameAs(settledMain));
                var stats = enabled.Core.CharacterStats.ReadOwned();
                Assert.That(stats.Items.Select(s => (s.OwnedId, s.Stats.AttackPower, s.Stats.MaxHealth, s.Stats.PhysicalDefense)),
                    Is.EqualTo(baselineStats.Items.Select(s => (s.OwnedId, s.Stats.AttackPower, s.Stats.MaxHealth, s.Stats.PhysicalDefense))));
            }
        }

        private static ConfiguredMovementBattle Create(bool main, bool enabled)
        {
            var config = new CategoryConfigData(3,5,3,2,0,                levelProgression: new LevelProgressionSpec(150, 100, 3, 1, 1));
            var database = new CoreIntegrationTestData(config);
            var category = main ? CategoryKey.Main : CategoryKey.Rider;
            var tables = new TraitGrowthTestRuntime.GrowthTables(category, false);
            if (enabled)
            {
                database.CategoryTraitProgressions = tables;
                database.TraitGrowthDefinitions = tables;
            }
            foreach (var unit in database.GetAllUnits().Where(u => u.Category == category &&
                u.OwnerEligibility != UnitOwnerEligibility.EnemyOnly).ToArray())
            {
                database.SetUnit(new UnitData(unit.Key, unit.Role, unit.OwnerEligibility, unit.FootprintWidth,
                    unit.FootprintHeight, unit.MaxHealth, unit.PhysicalDefense, unit.AttackRange, unit.Category,
                    unit.AttackPower, unit.Priority, new UnitTraitData[] { new KnockbackTraitData(1) },
                    targetingMethodKey: unit.TargetingMethodKey, occlusion: unit.Occlusion,
                    behavior: unit.Behavior, killReward: unit.KillReward));
            }
            return new ConfiguredMovementBattle(config, new VictorySource(), database: database);
        }

        private static void CompleteVictory(ConfiguredMovementBattle battle)
        {
            battle.Submit(battle.Attack(battle.Actor, new CellCoord(1, 3)));
            Assert.That(battle.Buffers.CombatStateReadSink.Completion, Is.Not.Null);
            Assert.That(battle.Buffers.CombatStateReadSink.Completion.Outcome.Outcome, Is.EqualTo(StageOutcome.Victory));
            battle.Buffers.BattleWriteSink.EndBattle();
            Assert.That(battle.Buffers.RunProgressReadSink.Outcome, Is.EqualTo(RunOutcome.InProgress));
        }

        private static TraitGrowthChoiceRequest Request(TraitGrowthOffer offer, TraitKey key) =>
            new(offer.RunId, offer.OfferId, offer.OwnedId, offer.StepId, key);

        private static TraitGrowthProcessedResult Publish(ConfiguredMovementBattle battle, TraitGrowthChoiceRequest request)
        {
            var buffers = battle.Buffers;
            int number = buffers.TraitGrowthViewWriteSink.Submit(request);
            var operation = ((CharacterRosterService)battle.Core.Characters).PrepareTraitGrowthChoice(request);
            var result = operation.Decision.Succeeded
                ? TraitGrowthResult.Success(operation.Decision.Change.Final)
                : TraitGrowthResult.Failure(operation.Decision.FailureReason);
            var receipt = new TraitGrowthProcessedResult(operation.RunId, number, request, result);
            var publication = buffers.TraitGrowthOperations.PrepareChoicePublication(operation.RunId,
                operation.Revision, operation.Offer, operation.Decision.Change, receipt);
            buffers.TraitGrowthOperations.InstallChoicePublication(publication);
            return receipt;
        }

        private sealed class VictorySource : IBattleInitializationSource
        {
            public BattleInitialization Read(IDataBaseQueryContext database, int chapterKey, StageKey stageKey)
            {
                var basis = new ConfiguredMovementBattle.EncounterSource(terminalHeadquarters: true)
                    .Read(database, chapterKey, stageKey);
                return new BattleInitialization(basis.Stage, basis.MainAnchor, basis.MainRotation, basis.Units,
                    basis.Structures, basis.Categories, basis.AttackProfiles, progression: database.InGameQuery.CategoryConfigTableQuery.Config.LevelProgression,
                    clearRewards: new StageClearRewardSpec(0, 0, 950));
            }
        }
    }
}
