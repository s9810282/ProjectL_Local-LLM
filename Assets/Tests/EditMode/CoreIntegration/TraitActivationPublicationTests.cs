using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class TraitActivationPublicationTests
    {
        [Test]
        public void Roar_BarrelOnly_PublishesDirectOriginBeforeExplosion()
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, new RoarBarrelSource());
            var boss = battle.Character(EntityKey.Unit_Demon);
            battle.Submit(battle.Attack(battle.Actor, boss.Anchor.Value));
            Assert.That(battle.Buffers.CombatResultReadSink.TryGetLatest(out var result), Is.True);
            var roar = result.Events.OfType<CombatEffectsEvent>().SelectMany(e => e.Traits).Single(a => a.TraitKey == TraitKey.Roar);
            var direct = result.Events.OfType<CombatStructureEvent>().First(e => e.Final.Definition.Key == EntityKey.Structure_PowderBarrel);
            var origin = direct.DamageOrigins.Single();
            Assert.That(roar.ResultOrigin.PhysicalCell, Is.EqualTo(boss.Anchor));
            Assert.That(roar.ResultOrigin.CreditedCharacterId, Is.EqualTo(boss.EntityId));
            Assert.That(roar.ResultOrigin.AttackGroup, Is.EqualTo(origin.AttackGroup));
            var explosion = result.Events.OfType<CombatObstacleReactionEvent>().Single().Origin;
            Assert.That(explosion.PhysicalCell, Is.EqualTo(new CellCoord(6, 2)));
            Assert.That(explosion.CreditedCharacterId, Is.EqualTo(boss.EntityId));
            Assert.That(explosion.AttackGroup, Is.Not.EqualTo(origin.AttackGroup));
            Assert.That(direct.Final.Health.IsDead, Is.True);
            Assert.That(result.Events.ToList().FindIndex(e => e is CombatEffectsEvent effects && effects.Traits.Contains(roar)),
                Is.LessThan(result.Events.ToList().FindIndex(e => e is CombatRoarEvent)));
            var once = new CoreIntegrationSnapshot(battle.Buffers);
            Assert.That(battle.Core.CombatCommands.ProcessNextCommand(), Is.False);
            Assert.That(battle.Buffers.CombatResultReadSink.TryGetLatest(out var same), Is.True);
            Assert.That(same, Is.SameAs(result));
            once.AssertUnchanged(battle.Buffers);
        }

        private sealed class RoarBarrelSource : IBattleInitializationSource
        {
            public BattleInitialization Read(IDataBaseQueryContext database, int chapterKey, StageKey stageKey)
            {
                var basis = new ConfiguredMovementBattle.EncounterSource(new CellCoord(6, 3)).Read(database, chapterKey, stageKey);
                var units = basis.Units.Select(s => new UnitSpec(s.Key, s.Role, s.FootprintWidth, s.FootprintHeight,
                    s.FootprintLocalCells.ToArray(), s.Key == EntityKey.Unit_Demon ? 100 : s.MaxHealth, s.MoveMethod,
                    s.Key == EntityKey.Unit_Cowboy ? 10 : s.Key == EntityKey.Unit_Demon ? 1 : s.AttackRange,
                    s.CategoryKey, s.Key == EntityKey.Unit_Demon ? 5 : s.AttackPower, s.PhysicalDefense, s.Priority,
                    s.Key == EntityKey.Unit_Demon ? new TraitSpec[] { new RoarTraitSpec(1) } : Array.Empty<TraitSpec>(),
                    s.OwnerEligibility, s.Occlusion, s.Behavior, s.KillReward)).ToArray();
                var profiles = basis.AttackProfiles.Select(p => new CombatAttackProfile(units.Single(u => u.Key == p.Definition.Key),
                    p.Definition.Key == EntityKey.Unit_Cowboy || p.Definition.Key == EntityKey.Unit_Demon
                        ? new TargetingMethodSpec(TargetingShape.RadialSquare, p.Definition.Key == EntityKey.Unit_Demon
                            ? TargetingChoiceMode.All : TargetingChoiceMode.Single, TargetingSplash.None, 0) : p.Method, p.OwnedId)).ToArray();
                var barrel = new StructureSpec(EntityKey.Structure_PowderBarrel, 1, 1, new[] { CellCoord.Zero }, 1, false, 0);
                return new BattleInitialization(basis.Stage, basis.MainAnchor, basis.MainRotation, units, basis.Structures, basis.Categories,
                    profiles, obstaclePlacements: new[] { new InitialObstaclePlacementSpec(barrel, ObstacleContentKind.PowderBarrel,
                        new CellCoord(6, 2), Rotation4.R0, 7) });
            }
        }

        [Test]
        public void EnterCombat_LateHunterResultRereadDoesNotDrawAgain()
        {
            using var battle = HunterPersistenceTests.LateHunterBattle();
            var hunter = battle.Character(EntityKey.Unit_Sniper);
            battle.Submit(CombatViewCommandData.EnterCombat());
            Assert.That(battle.Buffers.CombatResultReadSink.TryGetLatest(out var result), Is.True);
            var selected = result.Events.OfType<CombatEffectsEvent>().SelectMany(e => e.Traits)
                .Single(t => t.TraitKey == TraitKey.Hunter && t.TraitActivationKind == TraitActivationKind.HunterTargetSelected);
            Assert.That(selected.SourceUnitId, Is.EqualTo(hunter.EntityId));
            var final = battle.Character(EntityKey.Unit_Sniper);
            var status = final.Stats.TraitStatuses.ToArray()
                .OfType<ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits.HunterTraitStatusSnapshot>().Single();
            Assert.That(selected.TargetId, Is.EqualTo(status.TargetId));
            Assert.That(battle.Buffers.CombatStateReadSink.Current.CommandState.EnvironmentProgress.RandomState, Is.EqualTo(1015568748u));
            var before = new CoreIntegrationSnapshot(battle.Buffers);
            for (int i = 0; i < 3; i++)
            {
                Assert.That(battle.Buffers.CombatResultReadSink.TryGetLatest(out var same), Is.True);
                Assert.That(same, Is.SameAs(result));
                Assert.That(battle.Core.CombatCommands.ProcessNextCommand(), Is.False);
            }
            before.AssertUnchanged(battle.Buffers);
        }

        [Test]
        public void AcceptedResult_ContainsOrderedActivationsAndFinalFacts()
        {
            using var battle = Battle();
            var before = battle.Buffers.CombatStateReadSink.Current.CommandState;
            battle.Submit(battle.PreviewMove(battle.Actor, battle.Actor.Anchor.Value, new CellCoord(1, 1), new CellCoord(1, 2)));
            Assert.That(battle.Buffers.CombatResultReadSink.TryGetLatest(out var result), Is.True);
            Assert.That(result.Accepted, Is.True, result.Reason);
            var sources = result.Events.OfType<CombatMovementSourceEvent>().Where(e => e.Expected == null).ToArray();
            // 흔적은 지나간 (1,1)에만 두 종류 생긴다(출발·도착 칸 제외).
            Assert.That(sources.Length, Is.EqualTo(2));
            foreach (var source in sources)
            {
                Assert.That(battle.Buffers.CombatStateReadSink.Current.CommandState.Environment.Tiles.Single(t => t.Id == source.Final.Id).SameValues(source.Final), Is.True);
                int index = result.Events.ToList().IndexOf(source);
                Assert.That(result.Events[index - 1], Is.InstanceOf<CombatEffectsEvent>());
            }
            var final = battle.Buffers.CombatStateReadSink.Current.CommandState;
            Assert.That(final.EnvironmentProgress.NextSourceId, Is.EqualTo(before.EnvironmentProgress.NextSourceId + 2));
            Assert.That(final.EnvironmentProgress.RandomState, Is.EqualTo(before.EnvironmentProgress.RandomState));
            var once = new CoreIntegrationSnapshot(battle.Buffers);
            for (int i = 0; i < 3; i++)
            {
                Assert.That(battle.Buffers.CombatResultReadSink.TryGetLatest(out var same), Is.True);
                Assert.That(same, Is.SameAs(result));
                Assert.That(battle.Core.CombatCommands.ProcessNextCommand(), Is.False);
            }
            once.AssertUnchanged(battle.Buffers);
        }
        [Test]
        public void StalePublication_PublishesNeitherFactsNorActivations()
        {
            using var battle = Battle();
            var actor = battle.Actor;
            var cells = new[] { actor.Anchor.Value, new CellCoord(1, 1), new CellCoord(1, 2) };
            var command = battle.PreviewMove(actor, cells);
            int number = battle.Buffers.CombatViewWriteSink.Submit(command);
            var state = battle.Buffers.CombatStateReadSink.Current.CommandState;
            var input = battle.Publication().Assembler.Assemble(new CombatActionRequest(number, CombatCommandKind.Move, actor.EntityId, default,
                cell: cells.Last(), path: new MovementPath(cells), movementContext: command.MovementContext,
                expectedBattleInstanceId: battle.Buffers.CombatStateReadSink.BattleInstanceId,
                expectedControllerTurnSequence: state.Turn.ControllerTurnSequence));
            var progress = input.State.EnvironmentProgress;
            input = input.WithWorld(input.State.With(environmentProgress: new EnvironmentProgressFacts(progress.NextObjectEntryOrdinal,
                progress.NextSourceId + 1, progress.RandomState)), input.Characters, input.Main, input.SoulCounts, input.Souls, input.EnvironmentBindings);
            var transition = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(transition.Accepted, Is.True, transition.Reason);
            Assert.That(transition.Events.OfType<CombatEffectsEvent>().Any(), Is.True);
            var before = new CoreIntegrationSnapshot(battle.Buffers);
            Assert.Throws<InvalidOperationException>(() => battle.Buffers.BattleWriteSink.Apply(transition));
            before.AssertUnchanged(battle.Buffers);
        }
        [Test]
        public void Preview_DoesNotCommitEventsRngOrCounters()
        {
            using var battle = Battle();
            var before = battle.CaptureGameplay();
            battle.PreviewMove(battle.Actor, battle.Actor.Anchor.Value, new CellCoord(1, 1));
            Assert.That(battle.CaptureGameplay(), Is.EqualTo(before));
        }
        [Test]
        public void TrailResultMismatch_RejectsAllStoresBeforePublishing()
        {
            using var battle = Battle();
            var actor = battle.Actor;
            var cells = new[] { actor.Anchor.Value, new CellCoord(1, 1), new CellCoord(1, 2) };
            var command = battle.PreviewMove(actor, cells);
            int number = battle.Buffers.CombatViewWriteSink.Submit(command);
            var state = battle.Buffers.CombatStateReadSink.Current.CommandState;
            var input = battle.Publication().Assembler.Assemble(new CombatActionRequest(number, CombatCommandKind.Move, actor.EntityId, default,
                cell: cells.Last(), path: new MovementPath(cells), movementContext: command.MovementContext,
                expectedBattleInstanceId: battle.Buffers.CombatStateReadSink.BattleInstanceId,
                expectedControllerTurnSequence: state.Turn.ControllerTurnSequence));
            var transition = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            var omitted = transition.Events.OfType<CombatMovementSourceEvent>().First();
            var invalid = new CombatCommandTransition(transition.Input, transition.Accepted, transition.Reason, transition.Characters,
                transition.Field, transition.FinalState, transition.Outcome, transition.Attack, transition.ConsumedSoulIds, transition.Completion,
                transition.Movement, transition.Events.Where(e => !ReferenceEquals(e, omitted)).ToArray(), transition.PathMovement,
                transition.EnvironmentBindings, transition.BlockedContacts, transition.InventoryChange, transition.RewardChange,
                transition.RewardSettlement, transition.ActionUseSettlement);
            var before = new CoreIntegrationSnapshot(battle.Buffers);
            Assert.Throws<InvalidOperationException>(() => battle.Buffers.BattleWriteSink.Apply(invalid));
            before.AssertUnchanged(battle.Buffers);
        }
        [Test]
        public void EndBattleDisposal_DoesNotFabricateActivationResult()
        {
            using var battle = Battle();
            battle.Submit(battle.PreviewMove(battle.Actor, battle.Actor.Anchor.Value, new CellCoord(1, 1)));
            Assert.That(battle.Buffers.CombatResultReadSink.TryGetLatest(out var before), Is.True);
            int count = battle.Buffers.CombatResultHistoryReadSink.Results.Count;
            battle.Buffers.BattleWriteSink.EndBattle();
            Assert.That(battle.Buffers.CombatStateReadSink.HasBattle, Is.False);
            Assert.That(battle.Buffers.CharacterReadSink.Characters, Is.Empty);
            Assert.That(battle.Buffers.CombatResultReadSink.TryGetLatest(out var after), Is.True);
            Assert.That(after, Is.SameAs(before));
            Assert.That(battle.Buffers.CombatResultHistoryReadSink.Results.Count, Is.EqualTo(count));
        }
        private static ConfiguredMovementBattle Battle() => new(ConfiguredMovementBattle.Config, new TraitTrailPublicationTests.TrailSource());
    }
}
