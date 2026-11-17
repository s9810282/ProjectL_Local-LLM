using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Framework.Composition;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class EnemyAssessmentBoundaryPublicationTests
    {
        public enum Scenario
        {
            RemoteEffects,
            LeapMine,
            ForecastBind,
        }

        private static readonly CellCoord MineCell = new(0, 3);
        private static readonly CellCoord WarnedCell = new(7, 3);
        private static readonly CellCoord EscapeCell = new(7, 4);
        private static readonly IMovementTileRule[] Rules =
        {
            new SheriffBadgeMovementTileRule(), new MineMovementTileRule(), new FlagMovementTileRule()
        };

        [TestCase(Scenario.RemoteEffects, 1)]
        [TestCase(Scenario.RemoteEffects, 2)]
        [TestCase(Scenario.RemoteEffects, 31)]
        [TestCase(Scenario.LeapMine, 1)]
        [TestCase(Scenario.LeapMine, 2)]
        [TestCase(Scenario.LeapMine, 31)]
        [TestCase(Scenario.ForecastBind, 1)]
        [TestCase(Scenario.ForecastBind, 2)]
        [TestCase(Scenario.ForecastBind, 31)]
        public void CorrectedUtilityDecisionPublishesTheSameFactsAndEvents(Scenario scenario, int partition)
        {
            using var prepared = Create(scenario);
            using var synchronous = Create(scenario);
            var actor = prepared.Character(EntityKey.Unit_Demon);
            var input = synchronous.Publication().EnemyStep();
            if (scenario == Scenario.RemoteEffects)
            {
                Assert.That(prepared.Main.Area, Is.EqualTo(CharacterRuntimeArea.Field));
                Assert.That(prepared.Main.Stats.TraitStatuses.ToArray().Select(status => status.Key),
                    Is.EquivalentTo(new[] { TraitKey.Hunter, TraitKey.Bleeding, TraitKey.Disarm }));
            }
            if (scenario == Scenario.ForecastBind)
            {
                var warning = (RockfallGimmickState)input.State.Gimmicks.Get(new StageGimmickId(1));
                Assert.That(warning.WarnedCells, Does.Contain(WarnedCell));
                Assert.That(warning.WarnedCells, Has.No.Member(EscapeCell));
                var hold = CombatCommandCalculator.AdvancePrepared(input, EnemyDecision.EndEnemyPhase(), Rules);
                Assert.That(hold.Accepted, Is.True, hold.Reason);
                var held = hold.Characters.Characters.Single(change => change.Final.EntityId == actor.EntityId).Final;
                Assert.That(held.Health, Is.EqualTo(actor.Health));
                Assert.That(HasBind(held), Is.True);
            }

            var result = PublishBoth(prepared, synchronous, partition);
            Assert.That(result.PathMovement, Is.Not.Null, scenario.ToString());
            Assert.That(result.ActionUseSettlement?.ActorId, Is.EqualTo(actor.EntityId));
            var moved = prepared.Character(EntityKey.Unit_Demon);
            Assert.That(moved.Anchor, Is.EqualTo(result.PathMovement.FinalStop));
            Assert.That(moved.ActionUse, Is.EqualTo(UnitActionUse.Moved));

            if (scenario == Scenario.RemoteEffects)
            {
                var target = prepared.Character(EntityKey.Unit_DynamiteHammer).Anchor.Value;
                Assert.That(Distance(moved.Anchor.Value, target), Is.LessThan(Distance(actor.Anchor.Value, target)));
            }
            else if (scenario == Scenario.LeapMine)
            {
                Assert.That(result.PathMovement.EnteredCells.Take(result.PathMovement.DistanceSpent - 1), Does.Contain(MineCell));
                Assert.That(result.Events.OfType<CombatObjectActivationEvent>(), Is.Empty);
                Assert.That(result.FinalState.Environment.Tiles.OfType<MineSourceFact>().Single().Active, Is.True);
                Assert.That(moved.Health, Is.EqualTo(actor.Health));
            }
            else
            {
                Assert.That(moved.Anchor, Is.EqualTo(EscapeCell));
                var boundary = PublishBoth(prepared, synchronous, partition);
                Assert.That(boundary.Events.OfType<CombatRockfallEvent>().Any(e => e.Step == RockfallGimmickStep.Impact), Is.True);
                var escaped = prepared.Character(EntityKey.Unit_Demon);
                Assert.That(escaped.Health, Is.EqualTo(actor.Health));
                Assert.That(HasBind(escaped), Is.False);
            }
        }

        private static bool HasBind(CharacterCombatFacts unit) =>
            unit.StatusEffects.Entries.Any(status => status.Key == StatusEffectKey.Bind);

        private static int Distance(CellCoord a, CellCoord b) => Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);

        private static CombatCommandTransition PublishBoth(ConfiguredMovementBattle prepared, ConfiguredMovementBattle synchronous,
            int partition)
        {
            var buffers = prepared.Buffers;
            var direct = synchronous.Buffers;
            var publication = prepared.Publication();
            var directPublication = synchronous.Publication();
            int frame = 0;
            long ticks = 0;
            var service = new CombatAdvanceService(publication.Assembler, publication.Publisher, CombatMovementRules.Validated(Rules),
                () => frame, () => ++ticks, timestampFrequency: 1000L * partition, usePreparation: true);
            var builder = new SystemBuilderHelper();
            builder.AddService<IBufferQuery>(buffers);
            service.PostBindService(builder.BuildSystemReader().ServiceStoreReader);
            try
            {
                if (buffers.CombatViewReadSink.Commands.Count == 0)
                {
                    buffers.CombatViewWriteSink.Submit(prepared.EndPlayerTurn());
                    direct.CombatViewWriteSink.Submit(synchronous.EndPlayerTurn());
                }
                EnemyAdvancePreparationTests.AssertBattleValues(buffers, direct);
                var pending = buffers.CombatViewReadSink.Commands.ToArray();
                Assert.That(pending, Is.Not.Empty);
                var snapshot = prepared.CaptureStores();
                var directSnapshot = synchronous.CaptureStores();
                int sequence = buffers.CombatStateReadSink.LatestResultSequence;
                int handled = buffers.CombatStateReadSink.HandledCommandNumber;

                for (; frame < 100_000 && service.ReadPreparationState() != AdvancePreparationState.Ready; frame++)
                {
                    long version = service.PreparationWorkVersion;
                    service.PrepareAdvanceSlice();
                    Assert.That(service.PreparationWorkVersion - version, Is.LessThanOrEqualTo(partition));
                }
                Assert.That(service.ReadPreparationState(), Is.EqualTo(AdvancePreparationState.Ready));
                Assert.That(service.InputCaptureCount, Is.EqualTo(1));
                Assert.That(prepared.CaptureStores(), Is.EqualTo(snapshot));

                var expected = CombatCommandCalculator.AdvanceUtility(directPublication.EnemyStep(), Rules);
                Assert.That(expected.Accepted, Is.True, expected.Reason);
                Assert.That(synchronous.CaptureStores(), Is.EqualTo(directSnapshot));
                Assert.That(directPublication.Publisher.Publish(expected), Is.True);
                Assert.That(service.AdvancePreparedCombat(), Is.EqualTo(PreparedAdvanceResult.Published));
                Assert.That(buffers.CombatStateReadSink.LatestResultSequence, Is.EqualTo(sequence + 1));
                Assert.That(buffers.CombatStateReadSink.HandledCommandNumber, Is.EqualTo(handled));
                Assert.That(buffers.CombatViewReadSink.Commands, Is.EqualTo(pending));
                EnemyAdvancePreparationTests.AssertBattleValues(buffers, direct);
                Assert.That(buffers.CombatResultReadSink.TryGetLatest(out var actualResult), Is.True);
                Assert.That(direct.CombatResultReadSink.TryGetLatest(out var expectedResult), Is.True);
                EnemyAdvancePreparationTests.AssertValues(actualResult, expectedResult, "utility publication");
                return expected;
            }
            finally
            {
                service.CancelPreparation();
            }
        }

        private static ConfiguredMovementBattle Create(Scenario scenario)
        {
            var config = ConfiguredMovementBattle.Config;
            var database = new CoreIntegrationTestData(config);
            var traits = scenario == Scenario.LeapMine ? new UnitTraitData[] { new LeapTraitData() } : Array.Empty<UnitTraitData>();
            database.SetUnit(new UnitData(EntityKey.Unit_Demon, UnitRole.Regular, UnitOwnerEligibility.EnemyOnly,
                1, 1, scenario == Scenario.LeapMine ? 1000 : 100, 0, 1, CategoryKey.Main, 5, 1, traits, 1,
                behavior: new UnitBehaviorSpec(true, scenario != Scenario.ForecastBind, false), killReward: UnitKillRewardSpec.Excluded));
            if (scenario == Scenario.RemoteEffects)
                database.SetUnit(new UnitData(EntityKey.Unit_Akimbo, UnitRole.MainCharacter, UnitOwnerEligibility.PlayerOnly,
                    1, 1, 10, 0, 1, CategoryKey.Main, 5, 1,
                    new UnitTraitData[] { new HunterTraitData(2, 3), new BleedingTraitData(2), new DisarmTraitData(1) },
                    1, killReward: UnitKillRewardSpec.Excluded));
            var battle = new ConfiguredMovementBattle(config, new Encounter(scenario), database: database, deploySniper: false);
            try
            {
                battle.Submit(battle.EndPlayerTurn());
                Assert.That(battle.Core.CombatAdvance.AdvanceCombat(), Is.True);
                Assert.That(battle.Buffers.CombatStateReadSink.Current.CommandState.Turn.ControllerPhase, Is.EqualTo(CombatControllerPhase.Enemy));
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
            private readonly Scenario _scenario;
            internal Encounter(Scenario scenario) => _scenario = scenario;

            public BattleInitialization Read(IDataBaseQueryContext database, int chapterKey, StageKey stageKey)
            {
                var enemy = _scenario switch
                {
                    Scenario.LeapMine => new CellCoord(0, 4),
                    Scenario.ForecastBind => WarnedCell,
                    _ => new CellCoord(7, 5),
                };
                var basis = new ConfiguredMovementBattle.EncounterSource(enemy).Read(database, chapterKey, stageKey);
                if (_scenario == Scenario.RemoteEffects) return basis;

                var wallCells = _scenario == Scenario.LeapMine
                    ? (from x in Enumerable.Range(1, 3) from y in Enumerable.Range(2, 4)
                       where x != 3 || y != 5
                       select new CellCoord(x, y)).ToArray()
                    : new[] { new CellCoord(6, 2), new CellCoord(6, 3), new CellCoord(6, 4), new CellCoord(6, 5),
                        new CellCoord(7, 2), new CellCoord(7, 5) };
                var visibleTo = new[] { Owner.Player, Owner.Enemy };
                var walls = wallCells.Select((cell, index) => new StaticTerrainFact(100 + index, cell,
                    StaticTerrainKind.Wall, true, visibleTo)).ToArray();
                var tiles = walls.Cast<MovementTileFact>();
                var mineIds = Array.Empty<long>();
                var gimmicks = Array.Empty<StageGimmickSpec>();
                if (_scenario == Scenario.LeapMine)
                {
                    tiles = tiles.Append(new MineSourceFact(80, MineCell, 1, 10, true, visibleTo));
                    mineIds = new long[] { 80 };
                }
                else
                {
                    // 경고는 가능한 마스크를 모두 뽑는다. 출발 칸은 포함되며 위쪽의 유일한 탈출 칸은 벽 또는 경계로 제외된다.
                    gimmicks = new StageGimmickSpec[] { new RockfallGimmickSpec(1, 1, 64, 64,
                        maximumHealthLossPercent: 0, footprintOffsets: new[] { CellCoord.Zero, new CellCoord(0, 2) }) };
                }
                var stage = basis.Stage;
                var configured = new StageConfig(stage.MapWidth, stage.MapHeight, stage.ReserveCapacity, stage.PlayerZone,
                    stage.TimelineSeed, stage.InitialEnemyPlacements.ToArray(), stage.RailCells.ToArray(), gimmicks,
                    stage.InitialEnemyStructurePlacements.ToArray());
                return new BattleInitialization(configured, basis.MainAnchor, basis.MainRotation, basis.Units, basis.Structures,
                    basis.Categories, basis.AttackProfiles, new MovementEnvironmentFacts(tiles.ToArray()),
                    new EnvironmentSemanticBindings(walls.Select(wall => wall.Id).ToArray(), mineIds, Array.Empty<EntityId>()),
                    clearRewards: basis.ClearRewards);
            }
        }
    }
}
