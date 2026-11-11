using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Combat;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Terrain.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Terrain.Enum;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class TornadoRuntimeIntegrationTests
    {
        [Test]
        public void NonDefaultSoFlowsThroughDatabaseAdapterAndNormalServices()
        {
            using var fixture = new CoreAuthoringFixture();
            var so = fixture.Create<TerrainDefinitionDataSO>();
            CoreAuthoringFixture.Set(so, "kind", TerrainContentKind.Tornado);
            CoreAuthoringFixture.Set(so, "firstActivationAfterUnitTurns", 3L);
            CoreAuthoringFixture.Set(so, "repeatIntervalUnitTurns", 4);
            CoreAuthoringFixture.Set(so, "tornadoWarningLeadControllerTurns", 2);
            var spec = new TornadoGimmickSpec(new[] { ((TerrainDefinitionData)so.GetData()).CreateTornadoMember(901,
                new[] { Owner.Player, Owner.Enemy }, new CellCoord(1, 3)) });
            var db = new CoreIntegrationTestData(ConfiguredMovementBattle.Config);
            db.SetStage(new StageRootData(1101, 1, StageMapRole.Regular, new CombatStageDetail(42, "field", 1, 0, 1,
                Array.Empty<EntityPlacement>(), Array.Empty<EntityKey>(), Array.Empty<EntityPlacement>(), EntityKey.Structure_Headquarters,
                CellCoord.Zero, Rotation4.R0, new CellCoord(3, 5), Rotation4.R180, null, tornado: spec)));
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, database: db);
            Assert.That(battle.Buffers.CombatStateReadSink.Current.ReadTornadoAreas(Owner.Player).Single().Cells.Count, Is.EqualTo(9));
            for (int count = 1; count <= 7; count++)
            {
                if (count % 2 == 1) RockfallPublicationTests.EndPlayer(battle);
                else battle.AdvanceToPlayerOrDay();
                var member = Storm(battle).Members.Single();
                if (count == 1 || count == 5) Assert.That(member.Phase, Is.EqualTo(TornadoGimmickPhase.Warning));
                if (count == 3 || count == 7) Assert.That(member.Phase, Is.EqualTo(TornadoGimmickPhase.Cooldown));
            }
        }

        [Test]
        public void LeadEqualToRepeatPublishesDestinationThroughPlayerTurnsAndTravelArrivesThere()
        {
            using var fixture = new CoreAuthoringFixture();
            var so = fixture.Create<TerrainDefinitionDataSO>();
            CoreAuthoringFixture.Set(so, "kind", TerrainContentKind.Tornado);
            CoreAuthoringFixture.Set(so, "firstActivationAfterUnitTurns", 2L);
            CoreAuthoringFixture.Set(so, "repeatIntervalUnitTurns", 2);
            CoreAuthoringFixture.Set(so, "tornadoWarningLeadControllerTurns", 2);
            var spec = new TornadoGimmickSpec(new[] { ((TerrainDefinitionData)so.GetData()).CreateTornadoMember(901,
                new[] { Owner.Player, Owner.Enemy }, new CellCoord(1, 3)) });
            var db = new CoreIntegrationTestData(ConfiguredMovementBattle.Config);
            db.SetStage(new StageRootData(1101, 1, StageMapRole.Regular, new CombatStageDetail(42, "field", 1, 0, 1,
                Array.Empty<EntityPlacement>(), Array.Empty<EntityKey>(), Array.Empty<EntityPlacement>(), EntityKey.Structure_Headquarters,
                CellCoord.Zero, Rotation4.R0, new CellCoord(3, 5), Rotation4.R180, null, tornado: spec)));
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, database: db);

            // 경고 선행 = 반복 간격이면 첫 아군 턴부터 도착 위치가 보인다.
            var announced = PlayerArea(battle);
            AssertDestinationMatchesTravel(battle, announced);
            RockfallPublicationTests.EndPlayer(battle);
            Assert.That(PlayerArea(battle).Destination, Is.EqualTo(announced.Destination), "적 턴에도 같은 도착 위치를 유지해야 합니다.");

            // 적 턴이 끝나면 알린 자리로 이동하고, 다음 아군 턴의 도착 위치가 바로 다시 나온다.
            battle.AdvanceToPlayerOrDay();
            var next = PlayerArea(battle);
            Assert.That(next.Center, Is.EqualTo(announced.Destination.Value));
            AssertDestinationMatchesTravel(battle, next);
        }

        private static TornadoAreaFact PlayerArea(ConfiguredMovementBattle battle) =>
            battle.Buffers.CombatStateReadSink.Current.ReadTornadoAreas(Owner.Player).Single();

        private static void AssertDestinationMatchesTravel(ConfiguredMovementBattle battle, TornadoAreaFact area)
        {
            var state = battle.Buffers.CombatStateReadSink.Current.CommandState;
            Assert.That(area.Phase, Is.EqualTo(TornadoGimmickPhase.Warning));
            var direction = Storm(battle).Members.Single().LockedDirection.Value;
            var path = TornadoGeometry.Travel(area.Center, area.SideLength, 3, state.BoardWidth, state.BoardHeight, direction);
            Assert.That(area.Destination, Is.EqualTo(path[path.Count - 1]));
            Assert.That(area.DestinationCells, Is.EqualTo(TornadoGeometry.Footprint(area.Destination.Value, area.SideLength)));
        }

        [Test]
        public void ProductionPreparationAndBothSideEndsRunWarningTravelCycles()
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, new Source());
            Assert.That(battle.Buffers.CombatStateReadSink.Current.Environment.Tiles.OfType<TornadoSourceFact>(), Is.Empty);
            Assert.That(Storm(battle).Members.Single().Center, Is.EqualTo(new CellCoord(1, 3)));
            for (int count = 1; count <= 4; count++)
            {
                if (count % 2 == 1) RockfallPublicationTests.EndPlayer(battle);
                else battle.AdvanceToPlayerOrDay();
                var state = battle.Buffers.CombatStateReadSink.Current.CommandState;
                Assert.That(state.Turn.CompletedControllerTurnCount, Is.EqualTo(count));
                Assert.That(Storm(battle).Members[0].Phase, Is.EqualTo(count % 2 == 1 ? TornadoGimmickPhase.Warning : TornadoGimmickPhase.Cooldown));
                Assert.That(Storm(battle).NextBoundaryCount, Is.EqualTo(count + 1));
            }
        }

        [Test]
        public void DeploymentPlacementAndCombatActivationAreInertButLaterActualEntryThrows()
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, new Source(center: new CellCoord(1, 1)), enterCombat: false);
            var state = battle.Buffers.CombatStateReadSink.Current.CommandState;
            battle.Submit(CombatViewCommandData.ReturnUnit(battle.Actor.EntityId));
            battle.Submit(CombatViewCommandData.PlaceUnit(battle.Actor.EntityId, new CellCoord(0, 1), Rotation4.R0));
            Assert.That(battle.Buffers.CombatStateReadSink.Current.CommandState.Gimmicks.SameValues(state.Gimmicks), Is.True);
            Assert.That(battle.Buffers.CombatStateReadSink.Current.CommandState.EnvironmentProgress.SameValues(state.EnvironmentProgress), Is.True);
            battle.Submit(CombatViewCommandData.EnterCombat());
            Assert.That(battle.Actor.Anchor, Is.EqualTo(new CellCoord(0, 1)));
            Assert.That(battle.Core.CombatAdvance.AdvanceCombat(), Is.True);
            var move = battle.PreviewMove(battle.Actor, new CellCoord(0, 1), new CellCoord(1, 1));
            battle.Submit(move);
            Assert.That(battle.Buffers.CombatResultReadSink.TryGetLatest(out var result), Is.True);
            Assert.That(result.Events.OfType<CombatCharacterEvent>().Count(e => e.Kind == CombatEventKind.Relocated), Is.EqualTo(1));
            Assert.That(Storm(battle).SameValues(state.Gimmicks.Get(Storm(battle).Id)), Is.True);
        }

        [Test]
        public void OffPathLandingOccupancyChangeRejectsOldCommandThroughNormalService()
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, new Source(center: new CellCoord(1, 1)));
            var command = battle.PreviewMove(battle.Actor, new CellCoord(0, 1), new CellCoord(1, 1));
            var unit = battle.Character(EntityKey.Unit_Sniper);
            var changed = unit.WithPlacement(unit.Area, unit.ReserveSlotIndex, new CellCoord(6, 5), unit.Rotation, new[] { new CellCoord(6, 5) });
            battle.Buffers.BattleWriteSink.Apply(new CharacterChanges(new[] { new CharacterStateChange(unit, changed) }, Array.Empty<SoulCountChange>()));
            var before = battle.Buffers.CombatStateReadSink.Current.CommandState;
            var actor = battle.Actor;
            battle.Buffers.CombatViewWriteSink.Submit(command);
            Assert.That(battle.Core.CombatCommands.ProcessNextCommand(), Is.True);
            Assert.That(battle.Buffers.CombatResultReadSink.TryGetLatest(out var result), Is.True);
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Reason, Is.EqualTo("StaleContext"));
            Assert.That(result.Events, Is.Empty);
            Assert.That(battle.Actor.SameValues(actor), Is.True);
            Assert.That(battle.Buffers.CombatStateReadSink.Current.CommandState.EnvironmentProgress.SameValues(before.EnvironmentProgress), Is.True);
            Assert.That(battle.Core.CombatCommands.ProcessNextCommand(), Is.False);
        }

        [Test]
        public void ReusedPullEntryEvidenceFailsBeforeInstallingAnyStore()
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, new PullStormSource());
            var target = battle.Character(EntityKey.Unit_Demon);
            var input = battle.Publication().Attack(battle.Actor, target.Anchor.Value);
            var result = ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.CombatCommandCalculator.Calculate(
                input, Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            var entry = result.Events.OfType<CombatTornadoEntryEvent>().Single();
            var relocation = result.Events.OfType<CombatCharacterEvent>().Single(e => e.Kind == CombatEventKind.Relocated);
            var events = result.Events.ToList();
            events.InsertRange(events.IndexOf(relocation) + 1, new CombatEvent[] { entry, relocation });
            var forged = new CombatCommandTransition(input, true, "", result.Characters, result.Field, result.FinalState,
                result.Outcome, attack: result.Attack, events: events, rewardChange: result.RewardChange,
                rewardSettlement: result.RewardSettlement, actionUseSettlement: result.ActionUseSettlement);
            var before = battle.CaptureStores();
            var state = battle.Buffers.CombatStateReadSink.Current.CommandState;
            Assert.Throws<InvalidOperationException>(() => battle.Buffers.BattleWriteSink.Apply(forged));
            Assert.That(battle.CaptureStores(), Is.EqualTo(before));
            Assert.That(battle.Buffers.CombatStateReadSink.Current.CommandState.SameValues(state), Is.True);
            battle.Buffers.BattleWriteSink.Apply(result);
            Assert.That(battle.Character(EntityKey.Unit_Demon).Anchor, Is.Not.EqualTo(target.Anchor));
        }
        [Test]
        public void LethalScheduledEnemyEndPublishesItsIncrementAndOneOutcome()
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, new FatalLandingSource());
            RockfallPublicationTests.EndPlayer(battle);
            Assert.That(battle.Buffers.CombatStateReadSink.Current.CommandState.Turn.CompletedControllerTurnCount, Is.EqualTo(1));
            battle.AdvanceToPlayerOrDay();
            var state = battle.Buffers.CombatStateReadSink.Current.CommandState;
            Assert.That(state.Turn.CompletedControllerTurnCount, Is.EqualTo(2));
            Assert.That(state.Phase, Is.EqualTo(CombatFlowPhase.Finished));
            Assert.That(battle.Buffers.CombatResultReadSink.TryGetLatest(out var result), Is.True);
            var events = result.Events.ToList();
            Assert.That(events.Count(e => e.Kind == CombatEventKind.EnemyPhaseEnded), Is.EqualTo(1));
            Assert.That(events.FindIndex(e => e.Kind == CombatEventKind.EnemyPhaseEnded),
                Is.LessThan(events.FindIndex(e => e is CombatTornadoGimmickEvent)));
            Assert.That(events.OfType<CombatOutcomeEvent>().Count(e => e.Outcome.IsStageEnded), Is.EqualTo(1));
            Assert.That(events.OfType<CombatTornadoCenterEvent>().Select(e => e.Center), Is.EqualTo(new[] { new CellCoord(1, 1) }));
            Assert.That(battle.Buffers.CombatStateReadSink.Current.Gimmicks.Entries.OfType<TornadoGimmickState>().Single().Members.Single().NextBoundaryCount,
                Is.EqualTo(2));
        }
        internal static TornadoGimmickState Storm(ConfiguredMovementBattle battle) =>
            battle.Buffers.CombatStateReadSink.Current.Gimmicks.Entries.OfType<TornadoGimmickState>().Single();

        private sealed class FatalLandingSource : IBattleInitializationSource
        {
            public BattleInitialization Read(IDataBaseQueryContext database, int chapterKey, StageKey stageKey)
            {
                var d = new Source(center: new CellCoord(1, 1), enemy: false).Read(database, chapterKey, stageKey);
                var safe = new[] { CellCoord.Zero, new CellCoord(0, 1), new CellCoord(2, 0), new CellCoord(4, 0), new CellCoord(3, 5) };
                var mines = Enumerable.Range(0, d.Stage.MapHeight).SelectMany(y => Enumerable.Range(0, d.Stage.MapWidth).Select(x => new CellCoord(x, y)))
                    .Where(c => !safe.Contains(c)).Select((c, i) => new MineSourceFact(1000 + i, c, i + 1, 1000, true,
                        new[] { Owner.Player, Owner.Enemy })).ToArray();
                return new BattleInitialization(d.Stage, d.MainAnchor, d.MainRotation, d.Units, d.Structures,
                    d.Categories, d.AttackProfiles, new MovementEnvironmentFacts(mines),
                    new EnvironmentSemanticBindings(Array.Empty<long>(), mines.Select(m => m.Id).ToArray(), Array.Empty<EntityId>()),
                    d.ObstaclePlacements, d.WallBlocking, d.ObjectiveKind, d.ItemDefinitions, d.Progression,
                    clearRewards: d.ClearRewards, traitGrowth: d.TraitGrowth);
            }
        }
        private sealed class PullStormSource : IBattleInitializationSource
        {
            public BattleInitialization Read(IDataBaseQueryContext database, int chapterKey, StageKey stageKey)
            {
                var d = new PullBattleSource(new ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits.PullTraitSpec(1))
                    .Read(database, chapterKey, stageKey);
                var s = d.Stage;
                var stage = new StageConfig(s.MapWidth, s.MapHeight, s.ReserveCapacity, s.PlayerZone, s.TimelineSeed,
                    s.InitialEnemyPlacements.ToArray(), s.RailCells.ToArray(), new StageGimmickSpec[] {
                        new TornadoGimmickSpec(new[] { new TornadoMemberSpec(901, new[] { Owner.Player, Owner.Enemy },
                            new CellCoord(0, 2), 1, sideLength: 1) }) }, s.InitialEnemyStructurePlacements.ToArray());
                return new BattleInitialization(stage, d.MainAnchor, d.MainRotation, d.Units, d.Structures,
                    d.Categories, d.AttackProfiles, d.Environment, d.EnvironmentBindings, d.ObstaclePlacements,
                    d.WallBlocking, d.ObjectiveKind, d.ItemDefinitions, d.Progression, clearRewards: d.ClearRewards, traitGrowth: d.TraitGrowth);
            }
        }
        internal sealed class Source : IBattleInitializationSource
        {
            private readonly CellCoord _center;
            private readonly bool _enemy;
            internal Source(CellCoord? center = null, bool enemy = true) { _center = center ?? new CellCoord(1, 3); _enemy = enemy; }
            public BattleInitialization Read(IDataBaseQueryContext database, int chapterKey, StageKey stageKey)
            {
                var d = new ConfiguredMovementBattle.EncounterSource(_enemy ? new CellCoord(7, 4) : (CellCoord?)null).Read(database, chapterKey, stageKey);
                var s = d.Stage;
                var stage = new StageConfig(s.MapWidth, s.MapHeight, s.ReserveCapacity, s.PlayerZone, s.TimelineSeed,
                    s.InitialEnemyPlacements.ToArray(), s.RailCells.ToArray(), new StageGimmickSpec[] {
                        new TornadoGimmickSpec(new[] { new TornadoMemberSpec(901, new[] { Owner.Player, Owner.Enemy }, _center) }) },
                    s.InitialEnemyStructurePlacements.ToArray());
                return new BattleInitialization(stage, d.MainAnchor, d.MainRotation, d.Units, d.Structures,
                    d.Categories, d.AttackProfiles, d.Environment, d.EnvironmentBindings, d.ObstaclePlacements,
                    d.WallBlocking, d.ObjectiveKind, d.ItemDefinitions, d.Progression, clearRewards: d.ClearRewards, traitGrowth: d.TraitGrowth);
            }
        }
    }
}
