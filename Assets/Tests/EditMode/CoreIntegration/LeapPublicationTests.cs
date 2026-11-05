using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
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
    public sealed class LeapPublicationTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void OccupiedTornado_PublishesOnlyVoluntaryPrefixOnce(bool hidden)
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, new LeapSource(true, hidden));
            var actor = battle.Actor;
            var crossed = battle.Character(EntityKey.Unit_DynamiteHammer);
            var before = battle.Buffers.CombatStateReadSink.Current.CommandState;
            var cells = new[] { actor.Anchor.Value, new CellCoord(1, 1), new CellCoord(2, 1), crossed.Anchor.Value, new CellCoord(3, 0) };
            var gameplay = battle.CaptureGameplay();
            var command = battle.PreviewMove(actor, cells);
            Assert.That(battle.CaptureGameplay(), Is.EqualTo(gameplay));
            battle.Submit(command);
            Assert.That(battle.Buffers.CombatResultReadSink.TryGetLatest(out var result), Is.True);
            Assert.That(result.PathMovement.DistanceSpent, Is.EqualTo(3));
            var relocated = result.Events.OfType<CombatCharacterEvent>().Single(e => e.Kind == CombatEventKind.Relocated);
            Assert.That(relocated.MovementCause.Agency, Is.EqualTo(CombatMovementAgency.Forced));
            Assert.That(relocated.MovementCause.TraitKey, Is.Null);
            Assert.That(result.Events.OfType<CombatCharacterEvent>().Where(e => e.Kind == CombatEventKind.Moved)
                .All(e => e.MovementCause == null), Is.True);
            Assert.That(result.PathMovement.EnteredCells, Is.EqualTo(cells.Skip(1).Take(3)));
            Assert.That(result.ActionUseSettlement.ActorId, Is.EqualTo(actor.EntityId));
            Assert.That(result.ActionUseSettlement.FinalActionUse, Is.EqualTo(UnitActionUse.Moved));
            Assert.That(battle.Character(crossed.CharacterKey).SameValues(crossed), Is.True);
            var trails = battle.Buffers.CombatStateReadSink.Current.CommandState.Environment.Tiles.OfType<TraitTrailFact>().ToArray();
            // 토네이도가 cells[3]에서 옮겨 이동이 끝났으므로 지나간 칸은 cells[1]·cells[2]뿐이다.
            Assert.That(trails.Select(t => t.Cell), Is.EquivalentTo(cells.Skip(1).Take(2)));
            Assert.That(battle.Actor.Stats.TraitStatuses.ToArray()
                .OfType<ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits.AccelerationTraitStatusSnapshot>()
                .Single().HeldAttackBonus, Is.EqualTo(6));
            var once = new CoreIntegrationSnapshot(battle.Buffers);
            var finalActor = battle.Actor;
            var finalState = battle.Buffers.CombatStateReadSink.Current.CommandState;
            Assert.That(battle.Core.CombatCommands.ProcessNextCommand(), Is.False);
            once.AssertUnchanged(battle.Buffers);
            battle.Buffers.CombatViewWriteSink.Submit(command);
            Assert.That(battle.Core.CombatCommands.ProcessNextCommand(), Is.True);
            Assert.That(battle.Buffers.CombatResultReadSink.TryGetLatest(out var duplicate), Is.True);
            Assert.That(duplicate.Accepted, Is.False);
            Assert.That(battle.Actor.SameValues(finalActor), Is.True);
            Assert.That(battle.Buffers.CombatStateReadSink.Current.CommandState.SameValues(finalState), Is.True);
            Assert.That(battle.Buffers.CombatStateReadSink.Current.CommandState.Environment.Tiles.OfType<TraitTrailFact>().Count(), Is.EqualTo(2));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CrossedUnitIsUnchanged_AndRejectedPublicationMovesNothing(bool stale)
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, new LeapSource());
            var actor = battle.Actor;
            var crossed = battle.Character(EntityKey.Unit_DynamiteHammer);
            var before = battle.Buffers.CombatStateReadSink.Current.CommandState;
            var cells = new[] { actor.Anchor.Value, new CellCoord(1, 1), new CellCoord(2, 1), crossed.Anchor.Value, new CellCoord(3, 0) };
            var command = battle.PreviewMove(actor, cells);
            Assert.That(battle.Actor.SameValues(actor), Is.True);
            Assert.That(battle.Buffers.CombatStateReadSink.Current.CommandState.Environment.SameValues(before.Environment), Is.True);
            if (stale)
            {
                int number = battle.Buffers.CombatViewWriteSink.Submit(command);
                var input = battle.Publication().Assembler.Assemble(new CombatActionRequest(number, CombatCommandKind.Move, actor.EntityId, default,
                    cell: cells.Last(), path: new MovementPath(cells), movementContext: command.MovementContext,
                    expectedBattleInstanceId: battle.Buffers.CombatStateReadSink.BattleInstanceId,
                    expectedControllerTurnSequence: before.Turn.ControllerTurnSequence));
                var transition = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
                Assert.That(transition.Accepted, Is.True, transition.Reason);
                battle.Buffers.BattleWriteSink.Apply(CharacterSettlementCalculator.SetHealth(actor, null, actor.Health.CurrentHealth - 1));
                var changed = new CoreIntegrationSnapshot(battle.Buffers);
                Assert.Throws<InvalidOperationException>(() => battle.Buffers.BattleWriteSink.Apply(transition));
                changed.AssertUnchanged(battle.Buffers);
            }
            else
            {
                battle.Submit(command);
                Assert.That(battle.Actor.Anchor, Is.EqualTo(cells.Last()));
                Assert.That(battle.Character(crossed.CharacterKey).SameValues(crossed), Is.True);
                var final = battle.Buffers.CombatStateReadSink.Current.CommandState;
                // 흔적은 지나간 세 칸에만(출발·도착 제외) 생긴다.
                Assert.That(final.Environment.Tiles.Count(t => t.GetType().Name == "TraitTrailFact"), Is.EqualTo(3));
                Assert.That(final.EnvironmentProgress.NextSourceId, Is.EqualTo(before.EnvironmentProgress.NextSourceId + 3));
            }
        }

        private sealed class LeapSource : IBattleInitializationSource
        {
            private readonly bool _tornado;
            private readonly bool _hidden;
            internal LeapSource(bool tornado = false, bool hidden = false) { _tornado = tornado; _hidden = hidden; }
            public BattleInitialization Read(IDataBaseQueryContext database, int chapterKey, StageKey stageKey)
            {
                var basis = new ConfiguredMovementBattle.EncounterSource(new CellCoord(7, 5)).Read(database, chapterKey, stageKey);
                var units = basis.Units.Select(s => new UnitSpec(s.Key, s.Role, s.FootprintWidth, s.FootprintHeight, s.FootprintLocalCells.ToArray(),
                    s.MaxHealth, s.MoveMethod, s.AttackRange, s.CategoryKey, s.AttackPower, s.PhysicalDefense, s.Priority,
                    s.Key == EntityKey.Unit_Cowboy ? new TraitSpec[] { new LeapTraitSpec(), new GroundCompactionTraitSpec(1, 1), new AccelerationTraitSpec(2) }
                        .Concat(_tornado ? new TraitSpec[] { new SoloActionTraitSpec(10, 1, 0, 1) } : Array.Empty<TraitSpec>()).ToArray() : s.Traits.ToArray(),
                    s.OwnerEligibility, s.Occlusion, s.Behavior, s.KillReward)).ToArray();
                var oldStage = basis.Stage;
                var stage = new StageConfig(oldStage.MapWidth, oldStage.MapHeight, oldStage.ReserveCapacity, oldStage.PlayerZone,
                    oldStage.TimelineSeed, oldStage.InitialEnemyPlacements.ToArray(), oldStage.RailCells.ToArray(),
                    _tornado ? new StageGimmickSpec[] { new TornadoGimmickSpec(new[] { new TornadoMemberSpec(31,
                        _hidden ? new[] { Owner.Enemy } : new[] { Owner.Player, Owner.Enemy }, new CellCoord(2, 0), 1, sideLength: 1) }) }
                        : oldStage.StageGimmicks.ToArray(), oldStage.InitialEnemyStructurePlacements.ToArray());
                return new BattleInitialization(stage, basis.MainAnchor, basis.MainRotation, units, basis.Structures, basis.Categories,
                    basis.AttackProfiles.Select(p => new CombatAttackProfile(units.Single(u => u.Key == p.Definition.Key), p.Method, p.OwnedId)).ToArray(),
                    environment: basis.Environment, environmentBindings: basis.EnvironmentBindings);
            }
        }
    }
}
