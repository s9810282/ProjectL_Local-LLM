using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Framework.Composition;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Category.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Economy.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Field.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Loadout.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Map.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Combat;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Item;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.InGameTable;
using ProjectL.Systems.DataBaseSystem.Queries.RenderDataBase;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase;
using ProjectL.Systems.Factory.Runtime.Buffer;
using ProjectL.Systems.Factory.Runtime.InGame.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Current;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.ServiceKeys;

namespace ProjectL.Tests.Framework
{
    public sealed class EnvironmentEntryPublicationTests
    {
        [Test]
        public void Tornado_RealFactoryPreparesAndPublishesThirdTurnWithoutProductionAuthoring()
        {
            using var f = new Fixture();
            f.Source.Override = key => {
                var seed = f.Source.Definition(key);
                var source = new TornadoSourceFact(99, new CellCoord(2, 0), new[] { Owner.Player, Owner.Enemy });
                return new BattleInitialization(seed.Stage, seed.MainAnchor, seed.MainRotation, seed.Units,
                    seed.Structures, seed.Categories, seed.AttackProfiles, seed.Economy,
                    new MovementEnvironmentFacts(seed.Environment.Tiles.Append(source).ToArray()),
                    new EnvironmentSemanticBindings(seed.EnvironmentBindings.TerrainSourceIds.Append(99L).ToArray(),
                        seed.EnvironmentBindings.ObjectSourceIds, seed.EnvironmentBindings.ObstacleEntityIds));
            };
            f.EnterTarget();
            Assert.That(f.B.CombatStateReadSink.Current.Environment.Tiles.OfType<TornadoSourceFact>().Single().NextDueCompletedTurn, Is.EqualTo(3));
            f.StartTurn();
            for (int turn = 1; turn <= 3; turn++)
            {
                f.Command(CombatViewCommandData.EndUnitTurn());
                Assert.That(f.B.CombatStateReadSink.Current.CommandState.Turn.CompletedUnitTurnCount, Is.EqualTo(turn));
                if (turn < 3)
                {
                    Assert.That(f.Core.Combat.AdvanceCombat(), Is.True);
                    f.B.CombatViewWriteSink.AcknowledgePresented(f.Latest.Sequence);
                }
            }
            Assert.That(f.Latest.Events.OfType<CombatTornadoEvent>().Single().Expected.Id, Is.EqualTo(99));
            Assert.That(f.B.CombatStateReadSink.Current.Environment.Tiles.OfType<TornadoSourceFact>().Single().NextDueCompletedTurn, Is.EqualTo(6));
        }

        [Test]
        public void Flag_RealFactoryPreviewAndPublication_UpdateCharacterParticipantAndForecastTogether()
        {
            using var f = new Fixture();
            var cell = new CellCoord(2, 0);
            f.Source.Override = key => {
                var seed = f.Source.Definition(key);
                var flag = new FlagSourceFact(99, cell, 1, 7, true, new[] { Owner.Player, Owner.Enemy });
                return new BattleInitialization(seed.Stage, seed.MainAnchor, seed.MainRotation, seed.Units,
                    seed.Structures, seed.Categories, seed.AttackProfiles, seed.Economy,
                    new MovementEnvironmentFacts(seed.Environment.Tiles.Append(flag).ToArray()),
                    new EnvironmentSemanticBindings(seed.EnvironmentBindings.TerrainSourceIds, new[] { 99L }, seed.EnvironmentBindings.ObstacleEntityIds));
            };
            f.EnterTarget();
            f.StartTurn();
            var before = f.Main;
            var progress = f.B.CombatStateReadSink.Current.CommandState.EnvironmentProgress;
            var turn = f.B.CombatStateReadSink.Current.CommandState.Turn.Participants.Single(p => p.EntityId == before.EntityId);
            var path = new MovementPath(new[] { before.Anchor.Value, cell });
            var preview = f.Preview(CombatViewPreviewData.Move(before.EntityId, path));
            Assert.That(HeldEnvironmentEffects.EffectivePriority(preview.Move.Value.ProjectedActor), Is.EqualTo(before.Stats.Priority + 7));
            Assert.That(f.Main.HeldEnvironment.Flag, Is.Null);
            Assert.That(f.B.CombatStateReadSink.Current.CommandState.EnvironmentProgress, Is.SameAs(progress));
            f.Command(CombatViewCommandData.Move(before.EntityId, path, preview.MovementContext));
            Assert.That(HeldEnvironmentEffects.EffectivePriority(f.Main), Is.EqualTo(before.Stats.Priority + 7));
            Assert.That(f.Main.Stats.Priority, Is.EqualTo(before.Stats.Priority));
            var participant = f.B.CombatStateReadSink.Current.CommandState.Turn.Participants.Single(p => p.EntityId == before.EntityId);
            Assert.That(participant.Priority, Is.EqualTo(before.Stats.Priority + 7));
            Assert.That(participant.NextActionTime, Is.EqualTo(turn.NextActionTime));
            Assert.That(f.B.CombatStateReadSink.Current.Environment.Tiles.Single(t => t.Id == 99).Active, Is.False);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Mine_RealFactoryPreviewPublicationAndSelectedConsumptionAgree(bool hidden)
        {
            using var f = new Fixture();
            var cell = new CellCoord(2, 0);
            f.Source.Override = key => {
                var seed = f.Source.Definition(key);
                var mine = new MineSourceFact(99, cell, 1, 1, true, new[] { hidden ? Owner.Enemy : Owner.Player });
                return new BattleInitialization(seed.Stage, seed.MainAnchor, seed.MainRotation, seed.Units,
                    seed.Structures, seed.Categories, seed.AttackProfiles, seed.Economy,
                    new MovementEnvironmentFacts(seed.Environment.Tiles.Append(mine).ToArray()),
                    new EnvironmentSemanticBindings(seed.EnvironmentBindings.TerrainSourceIds, new[] { 99L }, seed.EnvironmentBindings.ObstacleEntityIds));
            };
            f.EnterTarget();
            f.StartTurn();
            var before = f.Main;
            var path = new MovementPath(new[] { before.Anchor.Value, cell });
            var preview = f.Preview(CombatViewPreviewData.Move(before.EntityId, path));
            Assert.That(preview.Move.Value.CanMove, Is.True);
            Assert.That(preview.Move.Value.ProjectedActor.Health.CurrentHealth, Is.EqualTo(before.Health.CurrentHealth - (hidden ? 0 : 1)));
            Assert.That(f.Main.Health.CurrentHealth, Is.EqualTo(before.Health.CurrentHealth));
            f.Command(CombatViewCommandData.Move(before.EntityId, path, preview.MovementContext));
            Assert.That(f.Main.Health.CurrentHealth, Is.EqualTo(before.Health.CurrentHealth - 1));
            Assert.That(f.B.CombatStateReadSink.Current.Environment.Tiles.Single(t => t.Id == 99).Active, Is.False);
            Assert.That(f.Latest.Events.OfType<CombatObjectActivationEvent>().Single().Source.Id, Is.EqualTo(99));
            Assert.That(f.B.CombatStateReadSink.Current.CommandState.EnvironmentProgress.NextObjectEntryOrdinal, Is.EqualTo(2));
        }

        [Test]
        public void Badge_RealFactoryEntryMovePreviewAndAtomicPublicationConsumeSelectedSources()
        {
            using var f = new Fixture();
            var cell = new CellCoord(2, 0);
            f.Source.Override = key => {
                var seed = f.Source.Definition(key);
                var objects = new MovementTileFact[] {
                    new SheriffBadgeSourceFact(99, cell, 1, true, new[] { Owner.Player }),
                    new SheriffBadgeSourceFact(1, cell, 2, true, new[] { Owner.Player }) };
                var environment = new MovementEnvironmentFacts(seed.Environment.Tiles.Concat(objects).ToArray());
                var bindings = new EnvironmentSemanticBindings(seed.EnvironmentBindings.TerrainSourceIds,
                    new[] { 99L, 1L }, seed.EnvironmentBindings.ObstacleEntityIds);
                return new BattleInitialization(seed.Stage, seed.MainAnchor, seed.MainRotation, seed.Units,
                    seed.Structures, seed.Categories, seed.AttackProfiles, seed.Economy, environment, bindings);
            };
            f.EnterTarget();
            f.StartTurn();
            var before = f.Main;
            var path = new MovementPath(new[] { before.Anchor.Value, cell });
            var preview = f.Preview(CombatViewPreviewData.Move(before.EntityId, path));
            Assert.That(preview.Move.Value.CanMove, Is.True);
            Assert.That(f.Main.HeldEnvironment.BadgeAvailable, Is.False);
            f.Command(CombatViewCommandData.Move(before.EntityId, path, preview.MovementContext));
            Assert.That(f.Main.HeldEnvironment.BadgeSourceId, Is.EqualTo(99));
            Assert.That(f.Main.HeldEnvironment.BadgeAvailable, Is.True);
            Assert.That(f.B.CombatStateReadSink.Current.Environment.Tiles.Where(t => t is ObjectSourceFact).All(t => !t.Active), Is.True);
            Assert.That(f.Latest.Events.OfType<CombatObjectActivationEvent>().Select(e => e.Source.Id), Is.EqualTo(new[] { 99L, 1L }));
            Assert.That(f.B.CombatStateReadSink.Current.CommandState.EnvironmentProgress.NextObjectEntryOrdinal, Is.EqualTo(3));
        }
        [Test]
        public void Barrel_RealCoreEntryCapturePreviewAndPublicationAgree()
        {
            using var f = new Fixture();
            var placement = new InitialObstaclePlacementSpec(new StructureSpec(EntityKey.Structure_PowderBarrel,
                1, 1, new[] { CellCoord.Zero }, 1, false, 0), ObstacleContentKind.PowderBarrel, new CellCoord(2, 0), Rotation4.R0, 1);
            f.Source.Override = key => {
                var seed = f.Source.Definition(key);
                return new BattleInitialization(seed.Stage, seed.MainAnchor, seed.MainRotation, seed.Units,
                    seed.Structures, seed.Categories, seed.AttackProfiles, seed.Economy, seed.Environment,
                    seed.EnvironmentBindings, new[] { placement });
            };
            f.EnterTarget();
            var profile = f.B.CombatStateReadSink.Current.ObstacleContents.Single();
            Assert.That(profile.EntityId, Is.Not.EqualTo(f.Main.EntityId));
            f.StartTurn();
            var before = f.Main;
            var range = f.Preview(CombatViewPreviewData.AttackRange(before.EntityId));
            Assert.That(range.Range.Value.InRangeTargetIds.ToArray(), Does.Contain(profile.EntityId));
            var preview = f.Preview(CombatViewPreviewData.Attack(before.EntityId, AttackAim.ForCell(placement.Anchor)));
            Assert.That(preview.Attack.Value.CanAttack, Is.True);
            f.Command(CombatViewCommandData.Attack(before.EntityId, AttackAim.ForCell(placement.Anchor)));
            Assert.That(f.Main.Health.CurrentHealth, Is.EqualTo(before.Health.CurrentHealth - 1));
            Assert.That(f.B.CombatStateReadSink.Current.Structures.Single(s => s.EntityId == profile.EntityId).Health.IsDead, Is.True);
            Assert.That(f.Latest.Events.OfType<CombatObstacleReactionEvent>().Single().ObstacleId, Is.EqualTo(profile.EntityId));
            Assert.That(f.B.CombatStateReadSink.Current.ObstacleContents.Single(), Is.SameAs(profile));
        }

        private static readonly CellCoord MainCell = new(1, 0);
        private static readonly CellCoord HeadquartersCell = new(1, 3);

        [Test]
        public void Entry_InstallsSelectedLayoutBindingsAndPreparedContributions()
        {
            using var f = new Fixture();
            f.PreviousSelection(false);
            var persistent = f.Persistent;
            var artifact = f.B.ArtifactReadSink.Current;
            Assert.That(f.Core.Combat.EnterBattle(1, new StageNodeId(3)).Accepted, Is.True);
            Assert.That(f.Source.Reads, Is.EqualTo(new[] { (1, 1102) }));
            Assert.That(f.B.CurrentProgressReadSink.CurrentStage.NodeId, Is.EqualTo(3));
            Assert.That(f.B.CurrentProgressReadSink.CurrentStage.StageKey, Is.EqualTo(1102));
            Assert.That(f.B.CurrentProgressReadSink.Shop, Is.Null);
            var battle = f.B.CombatStateReadSink.Current;
            var source = (DestinationTileModifierFact)battle.Environment.Tiles.Single();
            Assert.That(source.Id, Is.EqualTo(11));
            Assert.That(source.Cell, Is.EqualTo(MainCell));
            Assert.That(source.AttackBonus, Is.EqualTo(2));
            Assert.That(source.MaxHealthBonus, Is.EqualTo(3));
            Assert.That(source.KnownTo, Is.EqualTo(new[] { Owner.Player }));
            Assert.That(source.Active, Is.True);
            Assert.That(battle.EnvironmentBindings.TerrainSourceIds, Is.EqualTo(new[] { 11L }));
            Assert.That(battle.EnvironmentBindings, Is.SameAs(f.Source.Last.EnvironmentBindings));
            AssertPresence(f.Main, 12, 18, 2, new[] { 11L });
            Assert.That(f.Main.EntityId, Is.EqualTo(new EntityId(1)));
            Assert.That(battle.NextEntityId, Is.EqualTo(3));
            Assert.That(f.B.CombatStateReadSink.BattleInstanceId, Is.EqualTo(1));
            Assert.That(f.Persistent, Is.SameAs(persistent));
            Assert.That(f.B.CharacterReadSink.GetSoulCount(f.MainId), Is.EqualTo(3));
            Assert.That(f.B.CurrencyReadSink.Amount, Is.EqualTo(91));
            Assert.That(f.B.ArtifactReadSink.Current, Is.SameAs(artifact));
            var previous = f.Source.Definition(1101);
            Assert.That(previous.Environment.Tiles.Single().Cell, Is.EqualTo(new CellCoord(3, 2)));
            Assert.That(previous.Stage.MapWidth, Is.EqualTo(battle.BoardWidth));
        }

        [TestCase("Bounds", false, false)]
        [TestCase("Bounds", true, true)]
        [TestCase("Composition", false, false)]
        [TestCase("Composition", false, true)]
        [TestCase("Composition", true, false)]
        [TestCase("Composition", true, true)]
        [TestCase("Clamp", false, false)]
        [TestCase("Clamp", false, true)]
        [TestCase("Clamp", true, false)]
        [TestCase("Clamp", true, true)]
        public void Entry_InvalidEnvironmentPreservesAllStores(string failure, bool recovery, bool classified)
        {
            using var f = new Fixture(soulCount: failure == "Clamp" ? 0 : 3);
            f.SeedHistory();
            f.PreviousSelection(recovery);
            if (failure == "Clamp") f.SetMain(12);
            var before = new RecoveryBufferSnapshot(f.B);
            var instance = f.B.CombatStateReadSink.BattleInstanceId;
            var expected = f.Calculate(f.Source.Definition(1102));
            f.Source.Override = key => WithEnvironment(f.Source.Definition(key),
                failure == "Composition" ? Environment(Tile(11, new CellCoord(3, 2)), Tile(12, new CellCoord(3, 2))) :
                failure == "Bounds" ? Environment(Tile(11, new CellCoord(4, 0))) :
                Environment(Tile(11, MainCell, 0, -3)),
                classified ? Bindings(failure == "Composition" ? new[] { 11L, 12L } : new[] { 11L }) : EnvironmentSemanticBindings.Empty);

            if (failure == "Bounds")
                Assert.Throws<ArgumentOutOfRangeException>(() => f.Core.Combat.EnterBattle(1, new StageNodeId(3)));
            else
            {
                var error = Assert.Throws<NotSupportedException>(() => f.Core.Combat.EnterBattle(1, new StageNodeId(3)));
                Assert.That(error.Message, Does.Contain(failure == "Composition" ? "중첩 목적지 보정" : "메인 현재 HP"));
            }
            before.AssertUnchanged(f.B);
            Assert.That(f.B.CombatStateReadSink.BattleInstanceId, Is.EqualTo(instance));
            f.Source.Override = null;
            Assert.That(f.Core.Combat.EnterBattle(1, new StageNodeId(3)).Accepted, Is.True);
            Assert.That(f.B.CombatStateReadSink.BattleInstanceId, Is.EqualTo(instance + 1));
            Assert.That(f.B.CombatStateReadSink.Current.NextEntityId, Is.EqualTo(expected.NextEntityId));
            Assert.That(f.B.CharacterReadSink.Characters.Select(c => c.EntityId), Is.EqualTo(expected.Characters.Select(c => c.EntityId)));
            Assert.That(f.B.CombatStateReadSink.Current.Structures.Select(s => s.EntityId), Is.EqualTo(expected.Structures.Select(s => s.EntityId)));
            Assert.That(f.Source.Reads.TakeLast(2), Is.EqualTo(new[] { (1, 1102), (1, 1102) }));
            Assert.That(f.B.CombatStateReadSink.Completion, Is.Null);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Publication_DanglingBindingRejectsBeforeAnyInstall(bool recovery)
        {
            using var f = new Fixture();
            f.SeedHistory();
            f.PreviousSelection(recovery);
            var valid = f.Calculate(f.Source.Definition(1102));
            var c = valid.Combat;
            var combat = new CombatPreparation(c.BoardWidth, c.BoardHeight, c.ReserveCapacity, c.NightLength,
                c.TimelineSeed, c.InitialPhase, c.DrawOrder, c.DrawCursor, c.PlayerDeploymentDepth, c.EnemyDeploymentDepth,
                c.RailCells.ToArray(), c.StageGimmicks.ToArray(), c.ManualDrawGoldCost, c.Environment, Bindings(999));
            var invalid = new BattlePreparation(valid.Characters, combat, valid.Structures, valid.NextEntityId, valid.CommandState);
            var snapshot = new RecoveryBufferSnapshot(f.B);
            var instance = f.B.CombatStateReadSink.BattleInstanceId;
            Assert.Throws<ArgumentException>(() => f.B.BattleWriteSink.PrepareBattle(invalid, f.Selection()));
            snapshot.AssertUnchanged(f.B);
            Assert.That(f.B.CombatStateReadSink.BattleInstanceId, Is.EqualTo(instance));
            f.B.BattleWriteSink.PrepareBattle(valid, f.Selection());
            Assert.That(f.B.CombatStateReadSink.BattleInstanceId, Is.EqualTo(instance + 1));
            Assert.That(f.B.CombatStateReadSink.Current.NextEntityId, Is.EqualTo(valid.NextEntityId));
            Assert.That(f.Main.EntityId, Is.EqualTo(new EntityId(1)));
        }

        [Test]
        public void Metadata_SurvivesAcceptedCommandWithoutDuplicatingRuntimeState()
        {
            using var f = new Fixture();
            f.EnterTarget();
            var initial = f.B.CombatStateReadSink.Current;
            var bindings = initial.EnvironmentBindings;
            f.Command(CombatViewCommandData.EnterCombat());
            var current = f.B.CombatStateReadSink.Current;
            Assert.That(current, Is.Not.SameAs(initial));
            Assert.That(current.EnvironmentBindings, Is.SameAs(bindings));
            Assert.That(current.EnvironmentBindings.TerrainSourceIds, Is.EqualTo(new[] { 11L }));
            Assert.That(current.Environment, Is.SameAs(current.CommandState.Environment));
            Assert.That(current.Environment.SameValues(initial.Environment), Is.True);
            var cell = CellEnvironmentQuery.Read(current.CommandState, f.B.CharacterReadSink.Characters.Select(c => c.ToFacts()).ToArray(),
                current.EnvironmentBindings, MainCell);
            Assert.That(cell.TerrainSourceIds, Is.EqualTo(new[] { 11L }));
            Assert.That(cell.CharacterIds, Is.EqualTo(new[] { f.Main.EntityId }));
        }

        [Test]
        public void EndBattle_ClearsLocalEnvironmentPreservingRunState()
        {
            using var f = new Fixture();
            f.EnterTarget();
            var persistent = f.Persistent;
            var owned = f.B.CharacterReadSink.Owned.ToArray();
            var artifact = f.B.ArtifactReadSink.Current;
            f.WinAndEnd();
            Assert.That(f.B.CombatStateReadSink.HasBattle, Is.False);
            Assert.That(f.B.CombatStateReadSink.Current, Is.Null);
            Assert.That(f.B.CharacterReadSink.Characters, Is.Empty);
            Assert.That(f.B.CharacterReadSink.Owned, Is.EqualTo(owned));
            Assert.That(f.Persistent.CurrentHealth, Is.EqualTo(persistent.CurrentHealth));
            Assert.That(f.Persistent.PersistentMaxHealth, Is.EqualTo(12));
            Assert.That(f.B.CharacterReadSink.GetSoulCount(f.MainId), Is.EqualTo(3));
            Assert.That(f.B.ArtifactReadSink.Current, Is.SameAs(artifact));
            Assert.That(f.B.CurrencyReadSink.Amount, Is.EqualTo(91));
            Assert.That(f.B.StageProgressReadSink.IsCleared(1, 3), Is.True);
            Assert.That(f.B.CurrentProgressReadSink.CurrentStage.StageKey, Is.EqualTo(1102));
            Assert.That(f.B.CombatStateReadSink.Completion.Outcome.Outcome, Is.EqualTo(StageOutcome.Victory));
            Assert.That(f.B.CombatResultReadSink.TryGetLatest(out var retained), Is.True);
            Assert.That(retained.Accepted, Is.True);
            Assert.That(f.B.CombatViewReadSink.PresentedResultSequence, Is.EqualTo(retained.Sequence));
        }

        [Test]
        public void InitialPresence_MoveAndReturnReconcilesOnlyPositionContribution()
        {
            using var f = new Fixture();
            f.EnterTarget();
            AssertPresence(f.Main, 12, 18, 2, new[] { 11L });
            f.StartTurn();
            var before = f.Main;
            var bindings = f.B.CombatStateReadSink.Current.EnvironmentBindings;
            var environment = f.B.CombatStateReadSink.Current.Environment;
            Assert.That(before.Anchor, Is.EqualTo(MainCell));
            Assert.That(before.MovementCheckpointAnchor, Is.EqualTo(MainCell));
            Assert.That(before.PendingMoveDistance, Is.Zero);
            Assert.That(before.PendingMoveActionCost, Is.Zero);
            Assert.That(before.AttackRefundEligibleMoveCost, Is.Zero);
            Assert.That(before.Stats.MoveCost, Is.GreaterThan(0));
            var path = new MovementPath(new[] { MainCell, new CellCoord(2, 0) });
            var preview = f.Preview(CombatViewPreviewData.Move(before.EntityId, path));
            Assert.That(preview.Move.Value.CanMove, Is.True, preview.Move.Value.FailureReason.ToString());
            f.Command(CombatViewCommandData.Move(before.EntityId, path, preview.MovementContext));
            Assert.That(f.Latest.PathMovement.ReturnBarrierOccurred, Is.False);
            var away = f.Main;
            AssertPresence(away, 10, 15, 0, Array.Empty<long>());
            Assert.That(away.Anchor, Is.EqualTo(path.Endpoint));
            Assert.That(away.Stats.CurrentActionCost, Is.EqualTo(before.Stats.CurrentActionCost - before.Stats.MoveCost));
            Assert.That(away.Stats.RemainingMoveRange, Is.EqualTo(before.Stats.RemainingMoveRange - 1));
            Assert.That(away.PendingMoveDistance, Is.EqualTo(before.PendingMoveDistance + 1));
            Assert.That(away.PendingMoveActionCost, Is.EqualTo(before.PendingMoveActionCost + before.Stats.MoveCost));
            Assert.That(away.AttackRefundEligibleMoveCost, Is.EqualTo(before.AttackRefundEligibleMoveCost + before.Stats.MoveCost));
            Assert.That(away.CommittedMoveDistance, Is.EqualTo(before.CommittedMoveDistance));
            Assert.That(away.MovementCheckpointAnchor, Is.EqualTo(before.MovementCheckpointAnchor));
            AssertUnrelated(before, away);
            f.AssertEnvironment(environment, bindings);
            var returning = f.Preview(CombatViewPreviewData.ReturnToCheckpoint(before.EntityId));
            Assert.That(returning.CheckpointReturn.CanReturn, Is.True);
            f.Command(CombatViewCommandData.ReturnToCheckpoint(before.EntityId, returning.MovementContext));
            var back = f.Main;
            AssertPresence(back, 12, 18, 2, new[] { 11L });
            Assert.That(back.Anchor, Is.EqualTo(MainCell));
            Assert.That(back.Stats.CurrentActionCost, Is.EqualTo(before.Stats.CurrentActionCost));
            Assert.That(back.Stats.RemainingMoveRange, Is.EqualTo(before.Stats.RemainingMoveRange));
            Assert.That(back.PendingMoveDistance, Is.Zero);
            Assert.That(back.PendingMoveActionCost, Is.Zero);
            Assert.That(back.CommittedMoveDistance, Is.EqualTo(before.CommittedMoveDistance));
            Assert.That(back.AttackRefundEligibleMoveCost, Is.EqualTo(before.AttackRefundEligibleMoveCost));
            Assert.That(back.MovementCheckpointAnchor, Is.EqualTo(before.MovementCheckpointAnchor));
            AssertUnrelated(before, back);
            f.AssertEnvironment(environment, bindings);
        }

        [TestCase(StaticTerrainKind.HighGround, 2)]
        [TestCase(StaticTerrainKind.Sandstorm, 0)]
        public void StaticTerrain_PreparationAndCommandPublicationPreserveFacts(StaticTerrainKind kind, int surcharge)
        {
            using var f = new Fixture();
            var destination = new CellCoord(2, 0);
            f.Source.Override = key => {
                var converted = StageEnvironmentAdapter.Read(4, 5, Array.Empty<TerrainPresencePlacementData>(),
                    new[] { new StaticTerrainPlacementData(71, destination, kind, new[] { Owner.Player }) });
                return WithEnvironment(f.Source.Definition(key), converted.Environment, converted.Bindings);
            };
            f.EnterTarget();
            var initial = f.B.CombatStateReadSink.Current;
            Assert.That(initial.Environment.Tiles.Single(), Is.TypeOf<StaticTerrainFact>());
            Assert.That(initial.EnvironmentBindings.TerrainSourceIds, Is.EqualTo(new[] { 71L }));
            Assert.That(f.Main.PositionModifier.SourceIds, Is.Empty);
            f.StartTurn();
            var before = f.Main;
            var path = new MovementPath(new[] { MainCell, destination });
            var preview = f.Preview(CombatViewPreviewData.Move(before.EntityId, path));
            Assert.That(preview.Move.Value.CanMove, Is.True);
            f.Command(CombatViewCommandData.Move(before.EntityId, path, preview.MovementContext));
            Assert.That(f.Main.Anchor, Is.EqualTo(destination));
            Assert.That(f.Main.Stats.CurrentActionCost, Is.EqualTo(before.Stats.CurrentActionCost - before.Stats.MoveCost - surcharge));
            Assert.That(f.Main.Stats.AppointRange, Is.EqualTo(before.Stats.AppointRange));
            Assert.That(f.B.CombatStateReadSink.Current.Environment.SameValues(initial.Environment), Is.True);
            var returning = f.Preview(CombatViewPreviewData.ReturnToCheckpoint(before.EntityId));
            f.Command(CombatViewCommandData.ReturnToCheckpoint(before.EntityId, returning.MovementContext));
            Assert.That(f.Main.Anchor, Is.EqualTo(MainCell));
            Assert.That(f.Main.Stats.CurrentActionCost, Is.EqualTo(before.Stats.CurrentActionCost));
            Assert.That(f.B.CombatStateReadSink.Current.EnvironmentBindings.TerrainSourceIds, Is.EqualTo(new[] { 71L }));
        }

        [Test]
        public void DefaultSource_PreparesBattleFromDatabase_WithEnvironmentAndProfiles()
        {
            // Buffer에 연결되지 않은 공급원은 보유 로스터를 읽을 수 없으므로 G7로 거부한다.
            var source = new DatabaseBattleInitializationSource();
            Assert.That(Assert.Throws<InvalidOperationException>(() => source.Read(new Database(), 1, new StageKey(1102))).Message, Does.Contain("G7"));
            using var f = new Fixture(defaultSource: true);
            f.PreviousSelection(false);

            Assert.That(f.Core.Combat.EnterBattle(1, new StageNodeId(3)).Accepted, Is.True);

            // 운영 DB 경로만으로 전투가 설치되고, 프로필을 붙이면서 지형 환경이 빠지지 않는다.
            Assert.That(f.B.CombatStateReadSink.HasBattle, Is.True);
            Assert.That(f.B.CombatStateReadSink.Current.EnvironmentBindings.TerrainSourceIds, Is.EqualTo(new[] { 11L }));
            Assert.That(f.B.CurrencyReadSink.Amount, Is.EqualTo(91));
        }

        private static void AssertPresence(CharacterCombatFacts actor, int attack, int maxHealth, int position, long[] sources)
        {
            Assert.That(actor.Stats.EffectiveAttackPower, Is.EqualTo(attack));
            Assert.That(actor.Stats.PositionAttackBonus, Is.EqualTo(position));
            Assert.That(actor.Health.MaxHealth, Is.EqualTo(maxHealth));
            Assert.That(actor.Health.CurrentHealth, Is.EqualTo(4));
            Assert.That(actor.PositionModifier.SourceIds, Is.EqualTo(sources));
        }

        private static void AssertUnrelated(CharacterCombatFacts before, CharacterCombatFacts after)
        {
            Assert.That(after.EntityId, Is.EqualTo(before.EntityId));
            Assert.That(after.OwnedId, Is.EqualTo(before.OwnedId));
            Assert.That(after.CharacterKey, Is.EqualTo(before.CharacterKey));
            Assert.That(after.Owner, Is.EqualTo(before.Owner));
            Assert.That(after.OwnershipRole, Is.EqualTo(before.OwnershipRole));
            Assert.That(after.Area, Is.EqualTo(before.Area));
            Assert.That(after.Rotation, Is.EqualTo(before.Rotation));
            Assert.That(after.TemporaryAttackBonus, Is.EqualTo(before.TemporaryAttackBonus));
            Assert.That(after.TemporaryMaxHealthBonus, Is.EqualTo(before.TemporaryMaxHealthBonus));
            Assert.That(after.Stats.AttackPower, Is.EqualTo(before.Stats.AttackPower));
            Assert.That(after.Stats.ArtifactAttackBonus, Is.EqualTo(before.Stats.ArtifactAttackBonus));
            Assert.That(after.Stats.SoulAttackBonus, Is.EqualTo(before.Stats.SoulAttackBonus));
            Assert.That(after.Stats.SoulMaxHealthBonus, Is.EqualTo(before.Stats.SoulMaxHealthBonus));
            Assert.That(after.Stats.PhysicalDefense, Is.EqualTo(before.Stats.PhysicalDefense));
            Assert.That(after.Stats.MoveMethod, Is.EqualTo(before.Stats.MoveMethod));
            Assert.That(after.Stats.MoveCost, Is.EqualTo(before.Stats.MoveCost));
            Assert.That(after.Stats.MoveRange, Is.EqualTo(before.Stats.MoveRange));
            Assert.That(after.Stats.MaxActionCost, Is.EqualTo(before.Stats.MaxActionCost));
            Assert.That(after.Stats.AttackCost, Is.EqualTo(before.Stats.AttackCost));
            Assert.That(after.Stats.TraitStatuses.ToArray(), Is.EqualTo(before.Stats.TraitStatuses.ToArray()));
            Assert.That(after.Stats.Category, Is.SameAs(before.Stats.Category));
        }

        private static DestinationTileModifierFact Tile(long id, CellCoord cell, int attack = 2, int maxHealth = 3) =>
            new(id, cell, true, new[] { Owner.Player }, attack, maxHealth);
        private static MovementEnvironmentFacts Environment(params MovementTileFact[] tiles) => new(tiles);
        private static EnvironmentSemanticBindings Bindings(params long[] ids) => new(ids, Array.Empty<long>(), Array.Empty<EntityId>());
        private static BattleInitialization WithEnvironment(BattleInitialization original, MovementEnvironmentFacts environment, EnvironmentSemanticBindings bindings) =>
            new(original.Stage, original.MainAnchor, original.MainRotation, original.Units, original.Structures,
                original.Categories, original.AttackProfiles, original.Economy, environment, bindings);

        private sealed class Fixture : IDisposable
        {
            internal readonly Source Source = new();
            internal readonly FrameworkRuntime Runtime;
            internal readonly IBufferQuery B;
            internal readonly IInGameCoreSystemGroupService Core;
            internal readonly CharacterInstanceId MainId;
            internal CharacterCombatFacts Main => B.CharacterReadSink.Characters.Single(c => c.OwnershipRole == CharacterOwnershipRole.Main).ToFacts();
            internal MainCharacterPersistentState Persistent => B.CharacterReadSink.TryGetMainState(out var main) ? main : throw new InvalidOperationException();
            internal CombatResult Latest => B.CombatResultReadSink.TryGetLatest(out var result) ? result : throw new InvalidOperationException();

            internal Fixture(bool defaultSource = false, int soulCount = 3)
            {
                var builder = new SystemBuilderHelper();
                BufferSystemFactory.Create().ExportServices(builder);
                InGameCoreSystemFactory.Create(defaultSource ? null : Source).ExportServices(builder);
                Runtime = new FrameworkRuntime(builder.BuildSystemReader());
                Runtime.InitData(new Database());
                Runtime.PostBindService();
                B = Runtime.GetService(BufferSystemKey.Buffer);
                Core = Runtime.GetService(InGameSystemGroupServiceKey.Core);
                MainId = B.CharacterWriteSink.Acquire(new[] { new RosterAcquireRequest(EntityKey.Unit_MainCharacter, CharacterOwnershipRole.Main) },
                    new MainCharacterPersistentState(4, 12, 7, 6, 5, 1)).Acquired.Single().Id;
                if (soulCount > 0)
                    B.CharacterWriteSink.Apply(new CharacterChanges(Array.Empty<CharacterStateChange>(), new[] { new SoulCountChange(MainId, 0, soulCount) }));
                B.StageProgressWriteSink.RegisterStages(1, new[]
                {
                    new StageProgressData(1, 0, 0, 0, new[] { 2 }),
                    new StageProgressData(2, 1101, 1, 0, new[] { 3 }, stageType: StageType.Combat),
                    new StageProgressData(3, 1102, 2, 0, Array.Empty<int>(), stageType: StageType.Combat)
                });
                Assert.That(Core.ChapterProgression.EnterChapter(1).Accepted, Is.True);
                B.CurrencyWriteSink.SetAmount(91);
                B.ArtifactInitializationWriteSink.TryInitialize(new ArtifactState(4, 4,
                    new[] { new ArtifactItemState(100, 0, false, default, default, new[] { CellCoord.Zero }) }, Array.Empty<ArtifactItemState>()));
            }

            internal void SetMain(int hp)
            {
                B.CharacterWriteSink.Apply(new CharacterChanges(Array.Empty<CharacterStateChange>(),
                    Array.Empty<SoulCountChange>(),
                    new MainCharacterPersistentChange(Persistent, new MainCharacterPersistentState(hp, 12, 7, 6, 5, 1))));
            }

            internal void PreviousSelection(bool recovery)
            {
                B.StageProgressWriteSink.SetClear(1, 2, true);
                B.CurrentProgressWriteSink.SelectStage(new StageNodeId(2), new StageKey(1101), "Previous");
                if (recovery)
                    B.CurrentProgressWriteSink.SetRecoveryOptions(new[] { new RecoveryOptionSpec(new RecoveryOptionId(1), 5, new RecoveryHealSpec(RecoveryHealMode.Flat, 3)) });
                else
                    B.CurrentProgressWriteSink.SetShopOffers(Enumerable.Range(1, 3).Select(i => new ShopPurchaseSnapshot(new ShopOfferId(i), EntityKey.Unit_Akimbo, 10)).ToArray());
            }

            internal void EnterTarget() { PreviousSelection(false); Assert.That(Core.Combat.EnterBattle(1, new StageNodeId(3)).Accepted, Is.True); }
            internal BattleEntrySelection Selection() => new(1, B.CurrentProgressReadSink.HasSelection, B.CurrentProgressReadSink.CurrentStage,
                new StageNodeId(3), new StageKey(1102), "Stage 1102");
            internal BattlePreparation Calculate(BattleInitialization initialization) => BattlePreparationCalculator.Prepare(initialization,
                B.CharacterReadSink.Owned, Persistent, B.CharacterReadSink.Owned.ToDictionary(o => o.Id, o => B.CharacterReadSink.GetSoulCount(o.Id)),
                Core.Characters.PlanInitialDraw(initialization.Stage.TimelineSeed), Array.Empty<CharacterCombatFacts>());

            internal void Command(CombatViewCommandData data)
            {
                int number = B.CombatViewWriteSink.Submit(data);
                Assert.That(Core.Combat.ProcessNextCommand(), Is.True);
                Assert.That(Latest.CommandNumber, Is.EqualTo(number));
                Assert.That(Latest.Accepted, Is.True, Latest.Reason);
                Acknowledge();
            }
            private void Acknowledge()
            {
                B.CombatViewWriteSink.AcknowledgePresented(Latest.Sequence);
                B.CombatViewWriteSink.RemoveCommandsThrough(B.CombatStateReadSink.HandledCommandNumber);
                B.CombatViewWriteSink.ClearPreview();
            }
            internal CombatPreviewResult Preview(CombatViewPreviewData data)
            {
                int number = B.CombatViewWriteSink.SetPreview(data);
                Assert.That(Core.Combat.ProcessLatestPreview(), Is.True);
                Assert.That(B.CombatStateReadSink.TryGetLatestPreview(out var preview), Is.True);
                Assert.That(preview.RequestNumber, Is.EqualTo(number));
                return preview;
            }
            internal void StartTurn()
            {
                Command(CombatViewCommandData.EnterCombat());
                for (int i = 0; i < 8 && B.CombatStateReadSink.Phase == CombatFlowPhase.Automatic; i++)
                { Assert.That(Core.Combat.AdvanceCombat(), Is.True); Acknowledge(); }
                Assert.That(B.CombatStateReadSink.Phase, Is.EqualTo(CombatFlowPhase.PlayerInput));
                Assert.That(B.CombatStateReadSink.Current.CommandState.ActiveActorId, Is.EqualTo(Main.EntityId));
            }
            internal void WinAndEnd()
            {
                StartTurn();
                var path = new MovementPath(new[] { MainCell, new CellCoord(1, 1), new CellCoord(1, 2) });
                var preview = Preview(CombatViewPreviewData.Move(Main.EntityId, path));
                Command(CombatViewCommandData.Move(Main.EntityId, path, preview.MovementContext));
                Command(CombatViewCommandData.Attack(Main.EntityId, AttackAim.ForCell(HeadquartersCell)));
                Assert.That(B.CombatStateReadSink.Completion.Outcome.Outcome, Is.EqualTo(StageOutcome.Victory));
                Core.Combat.EndBattle();
            }
            internal void SeedHistory()
            {
                Assert.That(Core.Combat.EnterBattle(1, new StageNodeId(2)).Accepted, Is.True);
                WinAndEnd();
                // 준비 실패가 기존 응답을 지우지 않는지 검사하기 위한 저장소 경계 입력이다.
                int request = B.CombatViewWriteSink.SetPreview(CombatViewPreviewData.AttackRange(new EntityId(1)));
                ((ICombatStateBufferStore)B.CombatStateReadSink).InstallPreview(CombatPreviewResult.ForAttackRange(request,
                    AttackRangeResult.Failure(AttackFailureReason.InvalidAttackAim, new EntityId(1))));
                Assert.That(B.CombatStateReadSink.Completion, Is.Not.Null);
                Assert.That(B.CombatStateReadSink.LatestResultSequence, Is.GreaterThan(0));
            }
            internal void AssertEnvironment(MovementEnvironmentFacts environment, EnvironmentSemanticBindings bindings)
            {
                var battle = B.CombatStateReadSink.Current;
                Assert.That(battle.Environment.SameValues(environment), Is.True);
                Assert.That(battle.EnvironmentBindings, Is.SameAs(bindings));
                Assert.That(battle.EnvironmentBindings.TerrainSourceIds, Is.EqualTo(new[] { 11L }));
                Assert.That(Persistent.CurrentHealth, Is.EqualTo(4));
                Assert.That(Persistent.PersistentMaxHealth, Is.EqualTo(12));
                Assert.That(Persistent.PersistentAttack, Is.EqualTo(7));
                Assert.That(B.CharacterReadSink.GetSoulCount(MainId), Is.EqualTo(3));
            }
            public void Dispose() => Runtime.Shutdown();
        }

        private sealed class Source : IBattleInitializationSource
        {
            internal readonly List<(int chapter, int stage)> Reads = new();
            internal Func<int, BattleInitialization> Override;
            internal BattleInitialization Last;
            public BattleInitialization Read(IDataBaseQueryContext database, int chapterKey, StageKey stageKey)
            { Reads.Add((chapterKey, stageKey.Value)); return Last = Override?.Invoke(stageKey.Value) ?? Definition(stageKey.Value); }
            internal BattleInitialization Definition(int stageKey)
            {
                var converted = StageEnvironmentAdapter.Read(4, 5, Database.Detail(stageKey).TerrainPresencePlacements);
                var seed = CharacterBattlePreparationTests.Initialization();
                var stage = new StageConfig(4, 5, 8, 1, 2, 42, Array.Empty<InitialEnemyPlacementSpec>(), Array.Empty<CellCoord>(),
                    Array.Empty<ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks.StageGimmickSpec>(), 100,
                    new[] { new InitialEnemyStructurePlacementSpec(EntityKey.Structure_Headquarters, HeadquartersCell, Rotation4.R0) });
                var structures = new[] { new StructureSpec(EntityKey.Structure_Headquarters, 1, 1, new[] { CellCoord.Zero }, 1, false, 0) };
                var profiles = seed.Units.Select(unit => new CombatAttackProfile(unit,
                    new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0))).ToArray();
                return new BattleInitialization(stage, MainCell, Rotation4.R0, seed.Units, structures, seed.Categories, profiles,
                    new StageEconomySpec(0, 2), converted.Environment, converted.Bindings);
            }
        }

        // 실제 Factory와 조회 계약을 사용하되 운영 콘텐츠와 SO를 수정하지 않는다.
        private sealed class Database : IDataBaseQueryContext, IInGameQuery, IStageTableQuery, IStageMapTableQuery,
            IFieldTableQuery, IUnitRosterTableQuery, IStageEconomyTableQuery, ICategoryConfigTableQuery
        {
            private readonly CharacterRosterDatabase _roster = new();
            private readonly FieldData _field = new("shared-field", 4, 5, 8, 1, 2);
            private readonly StageMapData _map = new(1, 3, 1, 0, StageConnectionAlgorithm.Nearest, 1, 1, 42, false);
            private readonly UnitRosterData _units = new(1, new[] { EntityKey.Unit_Akimbo });
            private readonly StageEconomyData _economy = new(1, 0, 2);
            internal static CombatStageDetail Detail(int key) => new(42, "shared-field", 1, 0, 1, 100,
                Array.Empty<EntityPlacement>(), Array.Empty<EntityKey>(), Array.Empty<EntityPlacement>(), EntityKey.None,
                MainCell, Rotation4.R0, HeadquartersCell, Rotation4.R0, null,
                new[] { new TerrainPresencePlacementData(key == 1102 ? 11 : 42, key == 1102 ? MainCell : new CellCoord(3, 2),
                    new TerrainPresenceDefinitionData(2, 3), new[] { Owner.Player }) });
            public StageRootData GetStage(int key) => key == 1101 || key == 1102 ? new StageRootData(key, 1, StageMapRole.Regular, Detail(key)) : throw new KeyNotFoundException();
            public IReadOnlyList<StageRootData> GetStages(int key) => key == 1 ? new[] { GetStage(1101), GetStage(1102) } : Array.Empty<StageRootData>();
            public StageMapData GetStageMap(int key) => key == 1 ? _map : throw new KeyNotFoundException();
            public IReadOnlyCollection<StageMapData> GetAllStageMaps() => new[] { _map };
            public FieldData GetField(string key) => key == _field.FieldId ? _field : throw new KeyNotFoundException();
            public bool TryGetField(string key, out FieldData value) { value = key == _field.FieldId ? _field : null; return value != null; }
            public IReadOnlyCollection<FieldData> GetAllFields() => new[] { _field };
            public UnitRosterData GetUnitRoster(int key) => key == 1 ? _units : throw new KeyNotFoundException();
            public bool TryGetUnitRoster(int key, out UnitRosterData value) { value = key == 1 ? _units : null; return value != null; }
            public IReadOnlyCollection<UnitRosterData> GetAllUnitRosters() => new[] { _units };
            public StageEconomyData GetStageEconomy(int key) => key == 1 ? _economy : throw new KeyNotFoundException();
            public bool TryGetStageEconomy(int key, out StageEconomyData value) { value = key == 1 ? _economy : null; return value != null; }
            public IReadOnlyCollection<StageEconomyData> GetAllStageEconomies() => new[] { _economy };
            public CategoryConfigData Config => new(37, Rotation4.R0);
            public IInGameQuery InGameQuery => this;
            public IStageTableQuery StageTableQuery => this;
            public IStageMapTableQuery StageMapTableQuery => this;
            public IFieldTableQuery FieldTableQuery => this;
            public IUnitRosterTableQuery UnitRosterTableQuery => this;
            public IStageEconomyTableQuery StageEconomyTableQuery => this;
            public ICategoryConfigTableQuery CategoryConfigTableQuery => this;
            public IUnitTableQuery UnitTableQuery => _roster;
            public IPlayerLoadoutTableQuery PlayerLoadoutTableQuery => _roster;
            public IArtifactDefinitionTableQuery ArtifactDefinitionTableQuery => _roster.ArtifactDefinitionTableQuery;
            public IStructureTableQuery StructureTableQuery => throw new NotSupportedException();
            public IEnemyLoadoutTableQuery EnemyLoadoutTableQuery => throw new NotSupportedException();
            public ITargetingMethodTableQuery TargetingMethodTableQuery => _roster.TargetingMethodTableQuery;
            public IRecoveryOptionTableQuery RecoveryOptionTableQuery => throw new NotSupportedException();
            public IRenderQuery RenderQuery => throw new NotSupportedException();
            public IViewQuery ViewQuery => throw new NotSupportedException();
        }
    }
}
