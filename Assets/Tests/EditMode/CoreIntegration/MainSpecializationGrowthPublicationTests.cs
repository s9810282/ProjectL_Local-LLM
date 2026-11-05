using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.TraitGrowth.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.InGameTable;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Run;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class MainSpecializationGrowthPublicationTests
    {
        [TestCase(CategoryKey.Rider)]
        [TestCase(CategoryKey.Heavy)]
        [TestCase(CategoryKey.Cannon)]
        public void ActualChoiceAndAcknowledgementKeepClassAndPublishItToNextBattle(CategoryKey category)
        {
            using var game = new TraitGrowthTestRuntime(main: true, enabled: false, initialLevel: 3);
            var tables = new Tables(category);
            game.Database.CategoryTraitProgressions = tables;
            game.Database.TraitGrowthDefinitions = tables;
            ((IRunProgressBufferStore)game.Buffers.RunProgressReadSink).InstallRunId(new RunId(new Guid("11111111-2222-3333-4444-555555555555")));
            CoreIntegrationTestData.InstallDeck(game.Buffers);
            var initial = game.Current;
            var store = (ICharacterBufferStore)game.Buffers.CharacterReadSink;
            var history = new OwnedCharacterRecord(initial.Id, initial.CharacterKey, initial.Role, initial.Level, initial.Experience,
                new OwnedTraitGrowth(new[] { new TraitGrowthSelection(10, TraitKey.SoloAction) }), initial.HunterAttackBonus);
            store.InstallGrowth(store.PrepareGrowth(new TraitGrowthChange(initial, history)));
            MainSpecializationBattleTests.Seed(game.Buffers, category);
            var original = game.Current;
            game.Buffers.CharacterReadSink.TryGetMainState(original.Id, out var main);
            var offer = game.Open();
            Assert.That(offer.StepId, Is.EqualTo(20));
            Assert.That(offer.Choices.Count, Is.EqualTo(3));
            var frozenKeys = offer.Choices.Select(c => c.TraitKey).ToArray();
            var chosen = offer.Choices.First(c => c.TraitKey == TraitKey.HuntersMark || c.TraitKey == TraitKey.Vanguard).TraitKey;
            long revision = game.Revision;
            int number = game.Buffers.TraitGrowthViewWriteSink.Submit(game.Request(offer, chosen));
            Assert.That(game.Core.TraitGrowthCommands.ProcessNextTraitGrowthCommand(number), Is.True);
            Assert.That(game.Buffers.TraitGrowthResultReadSink.LatestResult.Result.Succeeded, Is.True);
            Assert.That(game.Current.SpecializationCategory, Is.EqualTo(category));
            Assert.That(game.Current.Growth.Selections.Select(s => s.StepId), Is.EqualTo(new[] { 10, 20 }));
            Assert.That(game.Current.Growth.Selections[0].TraitKey, Is.EqualTo(TraitKey.SoloAction));
            Assert.That(game.Revision, Is.EqualTo(revision + 1));
            Assert.That(game.Core.TraitGrowthCommands.ProcessNextTraitGrowthCommand(number), Is.False);
            Assert.That(game.Core.Characters.ReadTraitGrowthProgress().Status, Is.EqualTo(TraitGrowthProgressStatus.AwaitingAcknowledgement));
            Assert.That(offer.Choices.Select(c => c.TraitKey), Is.EqualTo(frozenKeys));
            Assert.That(game.Buffers.TraitGrowthViewWriteSink.AcknowledgeResult(offer.RunId, number), Is.True);
            var enhance = game.Open();
            Assert.That(enhance.StepId, Is.EqualTo(30));
            Assert.That(enhance.Choices.Select(c => c.TraitKey), Has.Member(chosen));
            number = game.Buffers.TraitGrowthViewWriteSink.Submit(game.Request(enhance, chosen));
            Assert.That(game.Core.TraitGrowthCommands.ProcessNextTraitGrowthCommand(number), Is.True);
            Assert.That(game.Buffers.TraitGrowthResultReadSink.LatestResult.Result.Succeeded, Is.True);
            Assert.That(game.Buffers.TraitGrowthViewWriteSink.AcknowledgeResult(enhance.RunId, number), Is.True);
            Assert.That(game.Core.Characters.ReadTraitGrowthProgress().Status, Is.EqualTo(TraitGrowthProgressStatus.ReadyToContinue));
            Assert.That(game.Current.SpecializationCategory, Is.EqualTo(category));
            Assert.That((game.Current.Level, game.Current.Experience, game.Current.HunterAttackBonus),
                Is.EqualTo((original.Level, original.Experience, original.HunterAttackBonus)));
            game.Buffers.CharacterReadSink.TryGetMainState(original.Id, out var after);
            Assert.That(after.SameValues(main), Is.True);
            var ownedStats = game.Core.CharacterStats.ReadOwned().Items.Single(c => c.OwnedId == original.Id);
            Assert.That(ownedStats.Stats.TraitKeys.ToArray(), Has.Member(chosen));
            Assert.That(ownedStats.Stats.CombatCategoryKey, Is.EqualTo(category));
            game.EnterBattle();
            var battle = game.Buffers.CharacterReadSink.Characters.Single(c => c.ToFacts().OwnedId == original.Id).ToFacts();
            Assert.That(battle.Stats.SpecializationCategory, Is.EqualTo(category));
            Assert.That(battle.Stats.TraitStatuses.ToArray().Select(t => t.Key), Has.Member(chosen));
            Assert.That(battle.Stats.MoveRange, Is.EqualTo(ownedStats.Stats.MoveRange));
        }

        private sealed class Tables : ICategoryTraitProgressionTableQuery, ITraitGrowthDefinitionTableQuery
        {
            private readonly CategoryTraitProgressionData[] _categories;
            private readonly TraitGrowthDefinitionData[] _definitions =
            {
                new(new KnockbackTraitData(1), new KnockbackTraitData(3), "knock", "normal", "knock+", "enhanced"),
                new(new SoloActionTraitData(1, 1, 0, 1), new SoloActionTraitData(3, 1, 0, 1), "solo", "normal", "solo+", "enhanced"),
                new(new HuntersMarkTraitData(20, 1), new HuntersMarkTraitData(40, 1), "mark", "normal", "mark+", "enhanced"),
                new(new VanguardTraitData(2, 1), new VanguardTraitData(4, 2), "vanguard", "normal", "vanguard+", "enhanced"),
                new(new TrackingShotTraitData(50, 10), null, "tracking", "normal", "", ""),
                new(new GroundCompactionTraitData(1, 1), null, "ground", "normal", "", ""),
                new(new RegenerationTraitData(10, 2), new RegenerationTraitData(20, 3), "regen", "normal", "regen+", "enhanced")
            };

            internal Tables(CategoryKey chosen)
            {
                var steps = new[] { new TraitGrowthStepSpec(10, 2, TraitGrowthKind.Acquire),
                    new TraitGrowthStepSpec(20, 3, TraitGrowthKind.Acquire), new TraitGrowthStepSpec(30, 3, TraitGrowthKind.Enhance) };
                _categories = new[] { CategoryKey.Main, CategoryKey.Rider, CategoryKey.Heavy, CategoryKey.Cannon }.Select(c =>
                    new CategoryTraitProgressionData(c, c == CategoryKey.Main ? steps : Array.Empty<TraitGrowthStepSpec>(),
                        c == CategoryKey.Main ? new[] { TraitKey.Knockback, TraitKey.SoloAction, TraitKey.TrackingShot, TraitKey.GroundCompaction, TraitKey.Regeneration }
                            : c == chosen ? new[] { TraitKey.SoloAction, TraitKey.HuntersMark, TraitKey.Vanguard } : Array.Empty<TraitKey>())).ToArray();
            }

            CategoryTraitProgressionData ICategoryTraitProgressionTableQuery.Get(CategoryKey key) => _categories.Single(c => c.Category == key);
            bool ICategoryTraitProgressionTableQuery.TryGet(CategoryKey key, out CategoryTraitProgressionData data)
            {
                data = _categories.SingleOrDefault(c => c.Category == key);
                return data != null;
            }
            IReadOnlyList<CategoryTraitProgressionData> ICategoryTraitProgressionTableQuery.GetAll() => _categories;
            TraitGrowthDefinitionData ITraitGrowthDefinitionTableQuery.Get(TraitKey key) => _definitions.Single(d => d.Key == key);
            bool ITraitGrowthDefinitionTableQuery.TryGet(TraitKey key, out TraitGrowthDefinitionData data)
            {
                data = _definitions.SingleOrDefault(d => d.Key == key);
                return data != null;
            }
            IReadOnlyList<TraitGrowthDefinitionData> ITraitGrowthDefinitionTableQuery.GetAll() => _definitions;
        }
    }
}
