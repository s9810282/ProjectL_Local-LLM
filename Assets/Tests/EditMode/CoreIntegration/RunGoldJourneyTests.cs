using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Recovery.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Combat;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Enhancement;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Event;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Shop;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.TraitGrowth.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.InGameTable;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Current;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.BufferSystems.ShopView;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Deck;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Shop;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class RunGoldJourneyTests
    {
        [Test]
        public void GrowthRestRecruitTrainingSkipAndBattle_PreserveRunStateUntilFreshRun()
        {
            using var game = new TraitGrowthTestRuntime(initialLevel: 1, deckEditAvailability: new DeckWindow());
            var core = game.Core;
            var buffers = game.Buffers;
            Assert.That(core.RunStart.StartNewRun(1).Succeeded, Is.True);
            var acquired = core.Characters.AcquireSub(EntityKey.Unit_ShotGun, 1);
            Assert.That(acquired.Succeeded, Is.True);
            var targetId = acquired.Acquired.Single().Id;
            var runId = buffers.RunProgressReadSink.RunId;
            var mainId = buffers.CharacterReadSink.Owned.Single(o => o.Role == CharacterOwnershipRole.Main).Id;
            var originalDeck = new[] { mainId, targetId }.Concat(buffers.CharacterReadSink.Owned
                .Where(o => o.Id != mainId && o.Id != targetId).Take(2).Select(o => o.Id)).ToArray();
            SelectDeck(originalDeck);
            game.Tables.ReplaceCategory(new CategoryTraitProgressionData(CategoryKey.Cannon,
                new[] { new TraitGrowthStepSpec(10, 2, TraitGrowthKind.Acquire), new TraitGrowthStepSpec(30, 3, TraitGrowthKind.Acquire) },
                new[] { TraitKey.ReconSquad, TraitKey.SoloAction }));
            game.Tables.ReplaceDefinition(TraitKey.Vanguard, new TraitGrowthDefinitionData(new ReconSquadTraitData(10, 1),
                null, "recon", "", "", "", new TraitRankValues(TraitKey.ReconSquad, 15), new TraitRankValues(TraitKey.ReconSquad, 20)));

            // 레벨 도달만 기존 fixture로 준비하고, 각 선택은 실제 입력·처리·확인 경계를 통과시킨다.
            SetLevel(2);
            Choose(TraitKey.ReconSquad, 10, 2);
            SetLevel(3);
            Choose(TraitKey.SoloAction, 30, 1);
            var selections = Current().Growth.Selections.ToArray();
            Assert.That(selections.Select(s => s.StepId), Is.EqualTo(new[] { 10, 30 }));
            Assert.That(Current().Growth.Purchases, Is.Empty);
            Assert.That(core.Characters.ReadTraitGrowthProgress().Status, Is.EqualTo(TraitGrowthProgressStatus.ReadyToContinue));

            buffers.CurrencyWriteSink.SetAmount(1000);
            var startingSpent = buffers.CurrencyReadSink.RunSpent;
            var originalMain = ReadMain();
            var combat = new StageRootData(1101, 1, StageMapRole.Regular, new CombatStageDetail(42, "field", 1, 0, 1,
                Array.Empty<EntityPlacement>(), Array.Empty<EntityKey>(), Array.Empty<EntityPlacement>(), EntityKey.Structure_Headquarters,
                CellCoord.Zero, Rotation4.R0, new CellCoord(7, 5), Rotation4.R180, null));
            game.Database.RecoveryOptions = new RecoveryTable();
            game.Database.SetStage(new StageRootData(1301, 1, StageMapRole.Regular, new EventStageDetail(new[] { 2, 4 })));
            buffers.StageProgressWriteSink.RegisterStages(1, new[]
            {
                new StageProgressData(1, 0, 0, 0, new[] { 2 }),
                new StageProgressData(2, 1301, 1, 0, new[] { 3 }, stageType: StageType.Event),
                new StageProgressData(3, 1201, 2, 0, new[] { 4 }, stageType: StageType.Shop),
                new StageProgressData(4, 1202, 3, 0, new[] { 5 }, stageType: StageType.Shop),
                new StageProgressData(5, 1202, 4, 0, new[] { 6 }, stageType: StageType.Shop),
                new StageProgressData(6, 1101, 5, 0, Array.Empty<int>())
            });
            Assert.That(core.ChapterProgression.EnterChapter(1).Accepted, Is.True);
            Assert.That(core.Recovery.EnterRecovery(1, new StageNodeId(2)).Accepted, Is.True);
            var rest = buffers.CurrentProgressReadSink.Recovery.Context;
            Recover(RecoveryCommandData.Select(rest, new RecoveryOptionId(4)));
            Assert.That(ReadMain().CurrentHealth, Is.EqualTo(originalMain.CurrentHealth));
            Assert.That(ReadMain().PersistentMaxHealth, Is.EqualTo(originalMain.PersistentMaxHealth + 10));
            Recover(RecoveryCommandData.Complete(rest));
            Assert.That(buffers.CurrencyReadSink.Amount, Is.EqualTo(990));

            game.Database.SetStage(new StageRootData(1201, 1, StageMapRole.Regular,
                new ShopStageDetail(new[] { EntityKey.Unit_Ballista }, Array.Empty<int>())));
            var beforeRecruit = buffers.CharacterReadSink.Owned.Select(o => o.Id).ToArray();
            Assert.That(core.Shop.EnterShop(1, new StageNodeId(3), 17).Accepted, Is.True);
            var recruit = Shop(ShopViewCommandData.Purchase(Context(3, 1201), buffers.CurrentProgressReadSink.Shop.Offers.Single().OfferId));
            Assert.That(recruit.Accepted, Is.True);
            Assert.That(recruit.CompletionResult.Accepted, Is.True);
            var recruited = buffers.CharacterReadSink.Owned.Single(o => !beforeRecruit.Contains(o.Id));
            Assert.That(recruited.CharacterKey, Is.EqualTo(EntityKey.Unit_Ballista));
            Assert.That(buffers.CurrencyReadSink.Amount, Is.EqualTo(390));
            SelectDeck(originalDeck.Where(id => id != targetId).Append(recruited.Id).ToArray());

            game.Database.SetStage(new StageRootData(1202, 1, StageMapRole.Regular, new EnhancementStageDetail(new TrainingPrices(10, 20))));
            Assert.That(core.Shop.EnterShop(1, new StageNodeId(4), 17).Accepted, Is.True);
            Assert.That(core.Deck.Read().Snapshot.Deck.OrderedIds, Has.No.Member(targetId));
            var choice = buffers.CurrentProgressReadSink.Shop.TrainingChoices.Single(c => c.OwnedId == targetId && c.TraitKey == TraitKey.ReconSquad);
            Assert.That((choice.CurrentRank, choice.NextRank, choice.Price, choice.CanPurchase), Is.EqualTo((1, 2, 10, true)));
            var trained = Shop(ShopViewCommandData.PurchaseTraining(Context(4, 1202), choice.OwnedId, choice.TraitKey, choice.CurrentRank));
            Assert.That(trained.Accepted, Is.True);
            Assert.That(trained.CompletionResult.Accepted, Is.True);
            Assert.That((trained.PurchaseResult.Training.OwnedId, trained.PurchaseResult.Training.PreviousRank,
                trained.PurchaseResult.Training.FinalRank, trained.PurchaseResult.Training.Price), Is.EqualTo((targetId, 1, 2, 10)));
            Assert.That(Current().Growth.Selections, Is.EqualTo(selections));
            Assert.That(Current().Growth.Purchases[TraitKey.ReconSquad], Is.EqualTo(1));
            Assert.That(buffers.CharacterReadSink.Owned.Single(o => o.Id == recruited.Id), Is.SameAs(recruited));
            Assert.That(PublishedStats().Items.Single(o => o.OwnedId == targetId).ReconSquadDescription.IncreasePercent, Is.EqualTo(15));

            Assert.That(core.Shop.EnterShop(1, new StageNodeId(5), 17).Accepted, Is.True);
            Assert.That(buffers.CurrentProgressReadSink.Shop.TrainingChoices.Any(c => c.OwnedId == targetId && c.CanPurchase), Is.True);
            var beforeSkip = Current();
            Assert.That(Shop(ShopViewCommandData.Complete(Context(5, 1202))).CompletionResult.Accepted, Is.True);
            Assert.That(Current(), Is.SameAs(beforeSkip));
            Assert.That(buffers.CurrencyReadSink.Amount, Is.EqualTo(380));
            Assert.That(buffers.CurrencyReadSink.RunSpent, Is.EqualTo(startingSpent + 620));
            Assert.That(buffers.RunProgressReadSink.RunId, Is.EqualTo(runId));
            SelectDeck(originalDeck);

            var owned = PublishedStats().Items.Single(o => o.OwnedId == targetId);
            Assert.That(owned.TraitRanks[TraitKey.ReconSquad], Is.EqualTo(2));
            Assert.That(owned.TraitRanks[TraitKey.SoloAction], Is.EqualTo(1));
            Assert.That(PublishedStats().Items.Single(o => o.OwnedId == mainId).Stats.MaxHealth,
                Is.EqualTo(originalMain.PersistentMaxHealth + 10));

            game.Database.SetStage(combat);
            Assert.That(core.BattleEntry.EnterBattle(1, new StageNodeId(6)).Accepted, Is.True);
            Assert.That(PublishedStats().Status, Is.EqualTo(CharacterStatsQueryStatus.BattleActive));
            var actor = buffers.CharacterReadSink.Characters.Single(c => c.OwnedId == targetId).ToFacts();
            Assert.That(actor.Stats.TraitStatuses.ToArray().OfType<ReconSquadTraitStatusSnapshot>().Single().IncreasePercent, Is.EqualTo(15));
            Assert.That(buffers.CurrencyReadSink.Amount, Is.EqualTo(380));
            core.BattleCompletion.AbandonBattle();
            core.BattleCompletion.EndBattle();
            Assert.That(Current().Growth.Selections, Is.EqualTo(selections));
            Assert.That(Current().Growth.Purchases[TraitKey.ReconSquad], Is.EqualTo(1));
            Assert.That(core.RunStart.StartNewRun(1).Succeeded, Is.True);
            Assert.That(buffers.RunProgressReadSink.RunId, Is.Not.EqualTo(runId));
            Assert.That(buffers.CurrencyReadSink.RunSpent, Is.Zero);
            Assert.That(buffers.CharacterReadSink.Owned.All(o => o.Level == 1 && o.Growth.Selections.Count == 0 && o.Growth.Purchases.Count == 0), Is.True);
            Assert.That(buffers.CharacterReadSink.Owned.Select(o => o.CharacterKey), Has.No.Member(EntityKey.Unit_Ballista));
            Assert.That(ReadMain().PersistentMaxHealth, Is.EqualTo(originalMain.PersistentMaxHealth));
            Assert.That(buffers.CurrencyReadSink.Amount, Is.EqualTo(game.Database.GetPlayerLoadout(1).StartingRunCurrency));
            var resetStats = PublishedStats();
            Assert.That(resetStats.Status, Is.EqualTo(CharacterStatsQueryStatus.Ready));
            Assert.That(resetStats.Items.All(o => o.TraitRanks.Values.All(rank => rank == 1)), Is.True);
            Assert.That(resetStats.Items.Select(o => o.OwnedId), Has.No.Member(targetId));

            CharacterStatsQueryResult PublishedStats()
            {
                buffers.CharacterStatsRequestSink.RequestRefresh();
                ((ISystemTick)core).Tick(0);
                return buffers.CharacterStatsReadSink.ReadOwned();
            }
            OwnedCharacterRecord Current() => buffers.CharacterReadSink.Owned.Single(o => o.Id == targetId);
            void SetLevel(int level)
            {
                var before = Current();
                var final = new OwnedCharacterRecord(before.Id, before.CharacterKey, before.Role, level, before.Experience, before.Growth);
                buffers.RunDeckReadSink.TryGetCurrent(out var deck);
                var store = (ICharacterBufferStore)buffers.CharacterReadSink;
                store.InstallChanges(store.PrepareCommandChanges(new CharacterChanges(Array.Empty<CharacterStateChange>(),
                    Array.Empty<SoulCountChange>(), owned: new[] { new OwnedCharacterChange(before, final) }), deck));
            }
            void Choose(TraitKey key, int step, int count)
            {
                var offer = game.Open();
                Assert.That((offer.OwnedId, offer.StepId, offer.Choices.Count), Is.EqualTo((targetId, step, count)));
                int number = buffers.TraitGrowthViewWriteSink.Submit(game.Request(offer, key));
                Assert.That(core.TraitGrowthCommands.ProcessNextTraitGrowthCommand(number), Is.True);
                Assert.That(buffers.TraitGrowthResultReadSink.LatestResult.Result.Succeeded, Is.True);
                Assert.That(buffers.TraitGrowthViewWriteSink.AcknowledgeResult(offer.RunId, number), Is.True);
            }
            ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster.MainCharacterPersistentState ReadMain()
            {
                var id = buffers.CharacterReadSink.Owned.Single(o => o.Role == CharacterOwnershipRole.Main).Id;
                Assert.That(buffers.CharacterReadSink.TryGetMainState(id, out var value), Is.True);
                return value;
            }
            void Recover(RecoveryCommandData command)
            {
                int number = buffers.RecoveryViewWriteSink.Submit(command);
                Assert.That(core.RecoveryCommands.ProcessNextRecoveryCommand(number), Is.True);
                Assert.That(buffers.RecoveryResultReadSink.TryGetLatest(out var result), Is.True);
                Assert.That(result.Result.Succeeded, Is.True);
                buffers.RecoveryViewWriteSink.AcknowledgeResult(number);
                Assert.That(buffers.RecoveryViewWriteSink.RemoveCommand(number), Is.True);
            }
            ShopVisitContext Context(int node, int stage) => new(1, new StageNodeId(node), new StageKey(stage), buffers.CurrentProgressReadSink.Shop.EntryId);
            ShopCommandResult Shop(ShopViewCommandData command)
            {
                int number = buffers.ShopViewWriteSink.Submit(command);
                Assert.That(core.StageCommands.ProcessNextShopCommand(number), Is.True);
                Assert.That(buffers.StageCommandResults.TryGetShopResult(out var result), Is.True);
                Assert.That(result.RequestNumber, Is.EqualTo(number));
                buffers.ShopViewWriteSink.RemoveCommandsThrough(number);
                return result;
            }
            void SelectDeck(CharacterInstanceId[] ids)
            {
                var snapshot = core.Deck.Read().Snapshot;
                var result = core.Deck.Confirm(new DeckEditRequest(snapshot.RunId, snapshot.RosterRevision, snapshot.DeckRevision, ids));
                Assert.That(result.Accepted, Is.True, result.Reason.ToString());
            }
        }

        private sealed class DeckWindow : IDeckEditAvailabilityRead
        {
            public DeckEditAvailability ReadDeckEditAvailability() => new(DeckUnavailableReason.None);
        }

        private sealed class RecoveryTable : IRecoveryOptionTableQuery
        {
            private readonly RecoveryOptionData[] _rows =
            {
                new(2, 5, RecoveryHealMode.PercentOfMax, 30),
                new(4, 10, RecoveryHealMode.Flat, 0, RecoveryEffectKind.MaxHealthGrowth, 10)
            };
            public RecoveryOptionData GetRecoveryOption(int key) => _rows.Single(r => r.OptionId == key);
            public bool TryGetRecoveryOption(int key, out RecoveryOptionData data)
            {
                data = _rows.SingleOrDefault(r => r.OptionId == key);
                return data != null;
            }
            public IReadOnlyCollection<RecoveryOptionData> GetAllRecoveryOptions() => _rows;
        }
    }
}
