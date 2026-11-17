using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class TraitTrailPublicationTests
    {
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void Compaction_PublishedPathLastsThroughOpposingTurn(int bonus)
        {
            var trait = new GroundCompactionTraitSpec(bonus, opposingTurnEndsToExpire: 1);
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, new HuntersMarkBattleSource(trait));
            var start = battle.Actor.Anchor.Value;
            // 흔적은 지나간 칸에만 생긴다. 출발·도착 칸은 빠진다.
            var passed = new CellCoord(1, 1);
            battle.Submit(battle.PreviewMove(battle.Actor, start, passed, new CellCoord(1, 2)));
            TraitTrailFact[] Active() => battle.Buffers.CombatStateReadSink.Current.CommandState.Environment.Tiles
                .OfType<TraitTrailFact>().Where(t => t.Active && t.Key == TraitKey.GroundCompaction).ToArray();
            var created = Active();
            Assert.That(created.Select(t => t.Cell), Is.EquivalentTo(new[] { passed }));
            Assert.That(created.Select(t => t.MovementRangeBonus), Is.All.EqualTo(bonus));
            battle.Submit(battle.EndPlayerTurn());
            Assert.That(Active().Select(t => t.Id), Is.EquivalentTo(created.Select(t => t.Id)));
            battle.AdvanceToPlayerOrDay();
            Assert.That(Active().Select(t => t.Id), Is.EquivalentTo(created.Select(t => t.Id)));
            battle.Submit(battle.EndPlayerTurn());
            Assert.That(Active(), Is.Empty);
        }

        [Test]
        public void RuntimeTrails_CannotSeedAnotherBattleDefinition()
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, new TrailSource());
            battle.Submit(battle.PreviewMove(battle.Actor, battle.Actor.Anchor.Value, new CellCoord(1, 1), new CellCoord(1, 2)));
            var environment = battle.Buffers.CombatStateReadSink.Current.CommandState.Environment;
            Assert.That(environment.Tiles.Any(t => t.GetType().Name == "TraitTrailFact"), Is.True);
            var basis = new TrailSource().Read(battle.Database, 1, new StageKey(1101));
            Assert.Throws<ArgumentException>(() => new BattleInitialization(basis.Stage, basis.MainAnchor, basis.MainRotation,
                basis.Units, basis.Structures, basis.Categories, basis.AttackProfiles, environment: environment));
        }

        [Test]
        public void ObjectAndTrailIds_ShareOneSpanWithIndependentEntryOrdinals()
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, new TrailSource());
            var actor = battle.Actor;
            var cells = new[] { actor.Anchor.Value, new CellCoord(1, 1), new CellCoord(1, 2) };
            var command = battle.PreviewMove(actor, cells);
            int number = battle.Buffers.CombatViewWriteSink.Submit(command);
            var state = battle.Buffers.CombatStateReadSink.Current.CommandState;
            var input = battle.Publication().Assembler.Assemble(new CombatActionRequest(number, CombatCommandKind.Move, actor.EntityId, default,
                cell: cells.Last(), path: new MovementPath(cells), movementContext: command.MovementContext,
                expectedBattleInstanceId: battle.Buffers.CombatStateReadSink.BattleInstanceId,
                expectedControllerTurnSequence: state.Turn.ControllerTurnSequence));
            var move = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(move.Accepted, Is.True, move.Reason);
            var progress = move.FinalState.EnvironmentProgress;
            // 두 종류가 한 전이에 함께 생성되는 Buffer 계약만 검증하며, 운영 깃발 기능은 켜지 않습니다.
            var created = new FlagSourceFact(progress.NextSourceId, new CellCoord(7, 4), progress.NextObjectEntryOrdinal, 1, true,
                new[] { Owner.Player, Owner.Enemy });
            var environment = new MovementEnvironmentFacts(move.FinalState.Environment.Tiles.Append(created).ToArray());
            var bindings = new EnvironmentSemanticBindings(input.EnvironmentBindings.TerrainSourceIds,
                input.EnvironmentBindings.ObjectSourceIds.Append(created.Id).ToArray(), input.EnvironmentBindings.ObstacleEntityIds);
            var final = move.FinalState.With(environment: environment, environmentProgress: new EnvironmentProgressFacts(
                progress.NextObjectEntryOrdinal + 1, progress.NextSourceId + 1, progress.RandomState));
            var together = new CombatCommandTransition(input, true, "", move.Characters, move.Field, final, move.Outcome,
                consumedSoulIds: move.ConsumedSoulIds, events: move.Events, pathMovement: move.PathMovement,
                environmentBindings: new EnvironmentBindingsChange(input.EnvironmentBindings, bindings), actionUseSettlement: move.ActionUseSettlement);
            battle.Buffers.BattleWriteSink.Apply(MovementUndoPolicy.Reconcile(together));
            Assert.That(battle.Buffers.CombatStateReadSink.Current.CommandState.MovementUndo.Checkpoints, Is.Empty);
            Assert.That(battle.Buffers.CombatStateReadSink.Current.CommandState.EnvironmentProgress.SameValues(final.EnvironmentProgress), Is.True);
            Assert.That(battle.Buffers.CombatStateReadSink.Current.EnvironmentBindings.ObjectSourceIds, Has.Member(created.Id));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void PreviewAndRejectedPublication_DoNotAllocateTrailIds(bool stale)
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, new TrailSource());
            var before = battle.Buffers.CombatStateReadSink.Current.CommandState;
            var actor = battle.Actor;
            var command = battle.PreviewMove(actor, actor.Anchor.Value, new CellCoord(1, 1), new CellCoord(1, 2));
            var afterPreview = battle.Buffers.CombatStateReadSink.Current.CommandState;
            Assert.That(afterPreview.Environment.SameValues(before.Environment), Is.True);
            Assert.That(afterPreview.EnvironmentProgress.SameValues(before.EnvironmentProgress), Is.True);
            Assert.That(battle.Actor.SameValues(actor), Is.True);
            if (stale)
            {
                var services = battle.Publication();
                int number = battle.Buffers.CombatViewWriteSink.Submit(command);
                var input = services.Assembler.Assemble(new CombatActionRequest(number, CombatCommandKind.Move, actor.EntityId, default,
                    cell: new CellCoord(1, 2), path: new MovementPath(new[] { actor.Anchor.Value, new CellCoord(1, 1), new CellCoord(1, 2) }),
                    movementContext: command.MovementContext,
                    expectedBattleInstanceId: battle.Buffers.CombatStateReadSink.BattleInstanceId,
                    expectedControllerTurnSequence: before.Turn.ControllerTurnSequence));
                var progress = input.State.EnvironmentProgress;
                input = input.WithWorld(input.State.With(environmentProgress: new EnvironmentProgressFacts(progress.NextObjectEntryOrdinal,
                    progress.NextSourceId + 1, progress.RandomState)), input.Characters, input.Main, input.SoulCounts, input.Souls, input.EnvironmentBindings);
                var transition = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
                Assert.That(transition.Accepted, Is.True, transition.Reason);
                var snapshot = new CoreIntegrationSnapshot(battle.Buffers);
                Assert.Throws<InvalidOperationException>(() => battle.Buffers.BattleWriteSink.Apply(transition));
                snapshot.AssertUnchanged(battle.Buffers);
            }
            else
            {
                battle.Submit(command);
                var final = battle.Buffers.CombatStateReadSink.Current.CommandState;
                Assert.That(final.Environment.Tiles.Count(t => t.GetType().Name == "TraitTrailFact"), Is.EqualTo(2));
                Assert.That(final.EnvironmentProgress.NextSourceId, Is.EqualTo(before.EnvironmentProgress.NextSourceId + 2));
                Assert.That(final.EnvironmentProgress.NextObjectEntryOrdinal, Is.EqualTo(before.EnvironmentProgress.NextObjectEntryOrdinal));
                Assert.That(final.EnvironmentProgress.RandomState, Is.EqualTo(before.EnvironmentProgress.RandomState));
                var once = new CoreIntegrationSnapshot(battle.Buffers);
                Assert.That(battle.Core.CombatCommands.ProcessNextCommand(), Is.False);
                once.AssertUnchanged(battle.Buffers);
            }
        }

        internal sealed class TrailSource : IBattleInitializationSource
        {
            public BattleInitialization Read(IDataBaseQueryContext database, int chapterKey, StageKey stageKey)
            {
                var basis = new ConfiguredMovementBattle.EncounterSource(new CellCoord(7, 5)).Read(database, chapterKey, stageKey);
                var units = basis.Units.Select(s => new UnitSpec(s.Key, s.Role, s.FootprintWidth, s.FootprintHeight, s.FootprintLocalCells.ToArray(),
                    s.MaxHealth, s.MoveMethod, s.AttackRange, s.CategoryKey, s.AttackPower, s.PhysicalDefense, s.Priority,
                    s.Key == EntityKey.Unit_Cowboy ? new TraitSpec[] { new GroundCompactionTraitSpec(1, 1), new DustScreenTraitSpec(1, 20, 1) } : s.Traits.ToArray(),
                    s.OwnerEligibility, s.Occlusion, s.Behavior, s.KillReward)).ToArray();
                return new BattleInitialization(basis.Stage, basis.MainAnchor, basis.MainRotation, units, basis.Structures, basis.Categories,
                    basis.AttackProfiles.Select(p => new CombatAttackProfile(units.Single(u => u.Key == p.Definition.Key), p.Method, p.OwnedId)).ToArray());
            }
        }
    }
}
