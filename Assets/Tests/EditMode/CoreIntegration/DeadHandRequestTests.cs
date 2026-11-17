using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.TraitGrowth.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Targeting.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.InGameTable;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Categories;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class DeadHandRequestTests
    {
        [TestCase(AttackSweepSide.Left, 0)]
        [TestCase(AttackSweepSide.Right, 3)]
        [TestCase(null, 3)]
        public void QueuedPreviewAndCommandResolveTheSameRelativeSide(AttackSweepSide? side, int finalX)
        {
            using var battle = CreateBattle();
            var before = battle.CaptureGameplay();
            var buffers = battle.Buffers;
            int request = buffers.CombatViewWriteSink.SetPreview(Preview(battle, side));
            Assert.That(battle.Core.CombatPreview.ProcessLatestPreview(), Is.True);
            Assert.That(buffers.CombatStateReadSink.TryGetLatestPreview(out var preview), Is.True);
            Assert.That(preview.RequestNumber, Is.EqualTo(request));
            var projected = preview.Attack.Value.Sweep;
            Assert.That(projected.Side, Is.EqualTo(side ?? AttackSweepSide.Right));
            Assert.That(projected.Targets.Single().FinalAnchor, Is.EqualTo(new CellCoord(finalX, 3)));
            Assert.That(battle.CaptureGameplay(), Is.EqualTo(before));

            battle.Submit(Command(battle, side));
            Assert.That(buffers.CombatResultReadSink.TryGetLatest(out var result), Is.True);
            var installed = result.Attack.Value.Sweep;
            Assert.That(installed.Side, Is.EqualTo(projected.Side));
            Assert.That(installed.Targets.Single().EnteredCells, Is.EqualTo(projected.Targets.Single().EnteredCells));
            Assert.That(battle.Character(EntityKey.Unit_Demon).Anchor, Is.EqualTo(projected.Targets.Single().FinalAnchor));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ChangingOrClearingRequestDuringSweepDiscardsProjection(bool clear)
        {
            using var battle = CreateBattle(Mine());
            var buffers = battle.Buffers;
            var service = new CombatPreviewService(battle.Publication().Assembler, new[] { new DuringMineEntry(step =>
            {
                Assert.That(step.Anchor, Is.EqualTo(MineCell));
                if (clear) buffers.CombatViewWriteSink.ClearPreview();
                else buffers.CombatViewWriteSink.SetPreview(Preview(battle, AttackSweepSide.Left));
            }) });
            service.PostBindService(Reader(battle));
            int previousAnswer = buffers.CombatStateReadSink.AnsweredPreviewNumber;
            var before = battle.CaptureGameplay();
            buffers.CombatViewWriteSink.SetPreview(Preview(battle));
            Assert.That(service.ProcessLatestPreview(), Is.False);
            Assert.That(buffers.CombatStateReadSink.AnsweredPreviewNumber, Is.EqualTo(previousAnswer));
            Assert.That(battle.CaptureGameplay(), Is.EqualTo(before));
            Assert.That(buffers.CombatViewReadSink.HasPreview, Is.EqualTo(!clear));
            if (!clear)
            {
                Assert.That(battle.Core.CombatPreview.ProcessLatestPreview(), Is.True);
                Assert.That(buffers.CombatStateReadSink.TryGetLatestPreview(out var response), Is.True);
                Assert.That(response.Attack.Value.Sweep.Side, Is.EqualTo(AttackSweepSide.Left));
            }
        }

        internal static readonly CellCoord MineCell = new(2, 3);
        private static readonly AttackAim Aim = AttackAim.ForDirection(AttackDirection.Up);

        internal static MineSourceFact Mine(int damage = 2) =>
            new(21, MineCell, 1, damage, true, new[] { Owner.Player, Owner.Enemy });

        internal static CombatViewPreviewData Preview(ConfiguredMovementBattle battle, AttackSweepSide? side = null) =>
            CombatViewPreviewData.Attack(battle.Actor.EntityId, Aim, side);

        internal static CombatViewCommandData Command(ConfiguredMovementBattle battle, AttackSweepSide? side = null)
        {
            var state = battle.Buffers.CombatStateReadSink;
            return CombatViewCommandData.Attack(battle.Actor.EntityId, Aim, state.BattleInstanceId,
                state.Current.CommandState.Turn.ControllerTurnSequence, side);
        }

        internal static ISystemServiceStoreReader Reader(ConfiguredMovementBattle battle)
        {
            var builder = new SystemBuilderHelper();
            builder.AddService<IBufferQuery>(battle.Buffers);
            return builder.BuildSystemReader().ServiceStoreReader;
        }

        internal static ConfiguredMovementBattle CreateBattle(MineSourceFact mine = null, bool terminal = false,
            bool rankTwo = false)
        {
            var config = ConfiguredMovementBattle.Config;
            var database = new CoreIntegrationTestData(config);
            database.SetTargetingMethod(new TargetingMethodData(2, TargetingShape.ForwardRectangle,
                TargetingChoiceMode.Direction, TargetingSplash.None, 0, width: 3));
            database.SetUnit(new UnitData(EntityKey.Unit_Cowboy, UnitRole.Regular, UnitOwnerEligibility.PlayerOnly,
                1, 1, 10, 0, 2, CategoryKey.Rider, 5, 1, new UnitTraitData[] { new DeadHandTraitData(100) },
                targetingMethodKey: 2, killReward: UnitKillRewardSpec.Excluded));
            if (rankTwo)
            {
                var tables = new RankTables();
                database.TraitGrowthDefinitions = tables;
                database.CategoryTraitProgressions = tables;
            }
            var battle = new ConfiguredMovementBattle(config, new Encounter(mine, terminal), database: database,
                configureOwned: rankTwo ? buffers => TraitGrowthStatSettlementTests.SeedPurchase(buffers, TraitKey.DeadHand) : null);
            try
            {
                if (!terminal) battle.Move(battle.Actor, new CellCoord(1, 1));
                return battle;
            }
            catch
            {
                battle.Dispose();
                throw;
            }
        }

        private sealed class Encounter : IBattleInitializationSource
        {
            private readonly MineSourceFact _mine;
            private readonly bool _terminal;

            internal Encounter(MineSourceFact mine, bool terminal)
            {
                _mine = mine;
                _terminal = terminal;
            }

            public BattleInitialization Read(IDataBaseQueryContext database, int chapterKey, StageKey stageKey)
            {
                var enemy = _terminal ? new CellCoord(0, 2) : new CellCoord(1, 3);
                var source = new ConfiguredMovementBattle.EncounterSource(enemy, _terminal).Read(database, chapterKey, stageKey);
                var tables = (IHasTraitGrowthTables)database.InGameQuery;
                var growth = TraitGrowthDefinitionAdapter.Build(tables.CategoryTraitProgressions, tables.TraitGrowthDefinitions);
                return new BattleInitialization(source.Stage, source.MainAnchor, source.MainRotation, source.Units,
                    source.Structures, source.Categories, source.AttackProfiles,
                    _mine == null ? source.Environment : new MovementEnvironmentFacts(new MovementTileFact[] { _mine }),
                    _mine == null ? source.EnvironmentBindings :
                        new EnvironmentSemanticBindings(Array.Empty<long>(), new[] { _mine.Id }, Array.Empty<EntityId>()),
                    clearRewards: source.ClearRewards, traitGrowth: growth);
            }
        }

        private sealed class RankTables : ICategoryTraitProgressionTableQuery, ITraitGrowthDefinitionTableQuery
        {
            private readonly TraitGrowthDefinitionData _definition = new(
                new DeadHandTraitData(100), null, "", "", "", "", rank2: new TraitRankValues(TraitKey.DeadHand, 140));
            private readonly CategoryTraitProgressionData[] _categories = TraitGrowthCatalog.Disabled.Categories
                .Select(c => new CategoryTraitProgressionData(c.Category, Array.Empty<TraitGrowthStepSpec>(), Array.Empty<TraitKey>()))
                .ToArray();

            CategoryTraitProgressionData ICategoryTraitProgressionTableQuery.Get(CategoryKey key) =>
                _categories.Single(c => c.Category == key);

            bool ICategoryTraitProgressionTableQuery.TryGet(CategoryKey key, out CategoryTraitProgressionData data)
            {
                data = _categories.SingleOrDefault(c => c.Category == key);
                return data != null;
            }

            IReadOnlyList<CategoryTraitProgressionData> ICategoryTraitProgressionTableQuery.GetAll() => _categories;

            TraitGrowthDefinitionData ITraitGrowthDefinitionTableQuery.Get(TraitKey key) =>
                key == _definition.Key ? _definition : throw new KeyNotFoundException();

            bool ITraitGrowthDefinitionTableQuery.TryGet(TraitKey key, out TraitGrowthDefinitionData data)
            {
                data = key == _definition.Key ? _definition : null;
                return data != null;
            }

            IReadOnlyList<TraitGrowthDefinitionData> ITraitGrowthDefinitionTableQuery.GetAll() => new[] { _definition };
        }

        internal sealed class DuringMineEntry : IMovementTileRule
        {
            private readonly MineMovementTileRule _rule = new();
            private readonly Action<MovementStepInput> _callback;

            internal DuringMineEntry(Action<MovementStepInput> callback) => _callback = callback;

            public string Key => _rule.Key;

            public IReadOnlyList<EntityId> TargetIds(MovementTileFact tile, EntityId mover, CombatCommandInput current) =>
                _rule.TargetIds(tile, mover, current);

            public MovementStepEffects Evaluate(MovementStepInput input, MovementTileFact tile)
            {
                _callback(input);
                return _rule.Evaluate(input, tile);
            }
        }

    }
}
