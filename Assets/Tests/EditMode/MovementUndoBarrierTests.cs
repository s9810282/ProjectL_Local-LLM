using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;

namespace ProjectL.Tests.Core
{
    public sealed class MovementUndoBarrierTests
    {
        [TestCase(99)]
        [TestCase(100)]
        [TestCase(110)]
        public void NativeVictoryHealingThenEndCapHasSeparateOrderedFacts(int beforeHealth)
        {
            var input = MovementUndoTestData.WithEnemy(CombatCommandTestData.Input(empty: true));
            var tile = new DestinationTileModifierFact(1, new CellCoord(7, 7), true, new[] { Owner.Player }, 0, 20);
            input = WithTile(input, tile);
            var characters = input.Characters.Select(c => c.EntityId.Value == 10
                ? PositionTileModifierCalculator.Replace(c, PositionTileModifierCalculator.ResolveAt(tile.Cell, input.State.Environment))
                    .WithHealth(new HealthStateSnapshot(beforeHealth, 120))
                : c.EntityId.Value == 2 ? c.WithHealth(new HealthStateSnapshot(1, 100)) : c).ToArray();
            var s = input.State;
            var state = new CombatCommandState(s.BoardWidth, s.BoardHeight, s.Phase, s.Structures, s.Turn, s.Environment, s.EnvironmentProgress, s.WallBlocking, BattleObjective.DesignatedBoss(new EntityId(2)),
                s.Allocation, s.ItemRandomState, s.Rewards, s.TraitTurn, s.MovementUndo);
            var effect = new ArtifactEffectData {
                entryId = "heal", trigger = ArtifactTriggerType.StageCleared,
                source = new ArtifactSourceData { faction = ArtifactFactionFilter.Any, category = CategoryKey.None },
                condition = new ArtifactConditionData { type = ArtifactConditionType.None, comparison = ArtifactComparisonType.LessThan },
                target = new ArtifactTargetData { type = ArtifactTargetType.MainCharacter, faction = ArtifactFactionFilter.Ally, category = CategoryKey.None },
                location = new ArtifactLocationData { type = ArtifactLocationType.None },
                effect = new ArtifactEffectPayloadData { type = ArtifactEffectType.Heal },
                value = new ArtifactValueData { type = ArtifactValueType.Flat, basis = ArtifactValueBasis.None, amount = 3f } };
            input = CombatCommandTestData.With(input, characters: characters, state: state,
                main: input.Main.WithCurrentHealth(beforeHealth), artifacts: new ArtifactLoadoutSnapshot(1,
                    new[] { new ArtifactPlacedEffectSnapshot(7, 2, effect) }));
            var result = MovementUndoTestData.Run(MovementUndoTestData.Command(input, CombatCommandKind.Attack,
                aim: AttackAim.ForCell(new CellCoord(2, 2))));
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.Outcome.Outcome, Is.EqualTo(StageOutcome.Victory));
            Assert.That(result.Characters.Main.Final.CurrentHealth, Is.EqualTo(100));
            var health = result.Events.OfType<CombatCharacterEvent>().Where(e => e.Final.EntityId.Value == 10 &&
                e.Kind is CombatEventKind.HealthChanged or CombatEventKind.HealthNormalized).ToArray();
            Assert.That(health.Length, Is.EqualTo(2));
            Assert.That(health[0].Expected.Health.CurrentHealth, Is.EqualTo(beforeHealth));
            Assert.That(health[0].Final.Health.CurrentHealth, Is.EqualTo(beforeHealth + 3));
            Assert.That(health[1].Kind, Is.EqualTo(CombatEventKind.HealthNormalized));
            Assert.That(health[1].Expected.Health.CurrentHealth, Is.EqualTo(beforeHealth + 3));
            Assert.That(health[1].Final.Health.CurrentHealth, Is.EqualTo(100));
            Assert.That(result.Events.OfType<CombatArtifactEvent>().Single().Activations.Single().AppliedAmount, Is.EqualTo(3));
            Assert.That(input.Main.CurrentHealth, Is.EqualTo(beforeHealth));
        }

        [Test]
        public void TerminalOutcomeClearsOtherActorsCheckpoint()
        {
            var input = MovementUndoTestData.Route();
            var b = CombatCommandTestData.Character(20, EntityKey.Unit_Akimbo, new CellCoord(1, 4), Owner.Player);
            input = TraitCombatFixtures.Characters(input, input.Characters.Append(b).ToArray());
            input = WithTile(input, new EffectTile(1, new CellCoord(2, 1), "terminal", new EntityId(10)));
            var movedB = MovementUndoTestData.Run(MovementUndoTestData.Command(input, CombatCommandKind.Move, actor: b.EntityId,
                path: new MovementPath(new[] { new CellCoord(1, 4), new CellCoord(2, 4) })), new EffectRule());
            Assert.That(movedB.FinalState.MovementUndo.TryGet(b.EntityId, out _), Is.True);
            var result = MovementUndoTestData.Run(MovementUndoTestData.Command(MovementUndoTestData.After(movedB),
                CombatCommandKind.Move, actor: input.ActorId, path: input.Path), new EffectRule());
            Assert.That(result.Outcome.IsStageEnded, Is.True);
            Assert.That(result.FinalState.MovementUndo.Checkpoints, Is.Empty);
        }

        [TestCase(TraitKey.Acceleration)]
        [TestCase(TraitKey.StationaryFire)]
        [TestCase(TraitKey.DustScreen)]
        [TestCase(TraitKey.GroundCompaction)]
        public void NativeSupportedMovementTraitEffectCanBeUndoneBeforeUse(TraitKey key)
        {
            TraitSpec spec = key switch {
                TraitKey.Acceleration => new AccelerationTraitSpec(2),
                TraitKey.StationaryFire => new StationaryFireTraitSpec(100),
                TraitKey.DustScreen => new DustScreenTraitSpec(1, 20, 1),
                _ => new GroundCompactionTraitSpec(1, 1) };
            var input = TraitCombatFixtures.Equip(MovementUndoTestData.Route(), EntityKey.Unit_Akimbo, spec);
            var result = MovementUndoTestData.Run(input);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.Events.OfType<CombatEffectsEvent>(), Is.Not.Empty);
            Assert.That(result.FinalState.MovementUndo.Checkpoints, Has.Count.EqualTo(1));
            var undo = MovementUndoTestData.Run(MovementUndoTestData.Undo(result));
            Assert.That(undo.Accepted, Is.True, undo.Reason);
            Assert.That(MovementTestData.Actor(undo).SameValues(input.Characters.Single(c => c.EntityId == input.ActorId)), Is.True);
        }

        [TestCase("damage")]
        [TestCase("heal")]
        [TestCase("relocate")]
        [TestCase("death")]
        [TestCase("terminal")]
        [TestCase("net-zero-debuff")]
        [TestCase("net-zero-method")]
        public void NativeEffectProducerCommitsMoveEvenWhenEndpointValuesRecover(string effect)
        {
            var input = MovementUndoTestData.Route();
            if (effect == "heal") input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId == input.ActorId
                ? c.WithHealth(new HealthStateSnapshot(90, 100)) : c).ToArray());
            input = WithTile(input, new EffectTile(1, new CellCoord(2, 1), effect, effect == "terminal" ? new EntityId(10) : input.ActorId));
            var result = MovementUndoTestData.Run(input, new EffectRule());
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.FinalState.MovementUndo.Checkpoints, Is.Empty);
            if (effect.StartsWith("net-zero", StringComparison.Ordinal))
            {
                var before = input.Characters.Single(c => c.EntityId == input.ActorId);
                var after = MovementTestData.Actor(result);
                Assert.That(after.Debuffs.SameValues(before.Debuffs), Is.True);
                Assert.That(after.Stats.MoveMethod, Is.EqualTo(before.Stats.MoveMethod));
                Assert.That(result.Events.OfType<CombatTraitStateEvent>().Count(), Is.EqualTo(2));
            }
        }

        [Test]
        public void SameActorChangedThenRestoredStillExpiresExistingCheckpoint()
        {
            var b = CombatCommandTestData.Character(20, EntityKey.Unit_Akimbo, new CellCoord(1, 4), Owner.Player);
            var input = MovementUndoTestData.Route();
            input = TraitCombatFixtures.Characters(input, input.Characters.Append(b).ToArray());
            input = WithTile(input, new EffectTile(1, new CellCoord(2, 1), "net-zero-debuff", b.EntityId));
            var bMove = MovementUndoTestData.Run(MovementUndoTestData.Command(input, CombatCommandKind.Move, actor: b.EntityId,
                path: new MovementPath(new[] { new CellCoord(1, 4), new CellCoord(2, 4) })), new EffectRule());
            var current = MovementUndoTestData.After(bMove);
            Assert.That(current.State.MovementUndo.TryGet(b.EntityId, out var checkpoint), Is.True);
            var aMove = MovementUndoTestData.Run(MovementUndoTestData.Command(current, CombatCommandKind.Move,
                actor: input.ActorId, path: input.Path), new EffectRule());
            var finalB = MovementUndoTestData.After(aMove).Characters.Single(c => c.EntityId == b.EntityId);
            Assert.That(finalB.SameValues(checkpoint.ExpectedActorAfterMove), Is.True);
            Assert.That(aMove.FinalState.MovementUndo.TryGet(b.EntityId, out _), Is.False);
        }

        [Test]
        public void SoulCollectionCommitsMovement()
        {
            var input = MovementUndoTestData.Route();
            var soul = new SoulFieldFacts(1, new EffectOriginKey(EffectOwnerKind.Trait, 2, (int)TraitKey.CallOfBlood, "main"), new CellCoord(4, 1), 2, 1);
            input = CombatCommandTestData.With(input, souls: new SoulFieldSnapshot(2, new[] { soul }));
            var result = MovementUndoTestData.Run(input);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.ConsumedSoulIds, Is.EqualTo(new long[] { 1 }));
            Assert.That(result.FinalState.MovementUndo.Checkpoints, Is.Empty);
        }

        [TestCase(1)]
        [TestCase(3)]
        public void WeakGroundEntryAndCollapseCommitMovement(int threshold)
        {
            var input = MovementUndoTestData.Route();
            var source = new StaticTerrainFact(1, new CellCoord(2, 1), StaticTerrainKind.WeakGround, true,
                new[] { Owner.Player, Owner.Enemy }, threshold);
            input = WithTile(input, source);
            input = input.WithWorld(input.State, input.Characters, input.Main, input.SoulCounts, input.Souls,
                new EnvironmentSemanticBindings(new long[] { 1 }, Array.Empty<long>(), Array.Empty<EntityId>()));
            var result = MovementUndoTestData.Run(input);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.Events.OfType<CombatMovementSourceEvent>(), Is.Not.Empty);
            Assert.That(result.FinalState.MovementUndo.Checkpoints, Is.Empty);
        }

        [Test]
        public void PendingRoarSettlesBeforeCheckpointCreation()
        {
            var input = MovementTestData.Route(BossRoarTests.Input(5), new CellCoord(4, 3), new CellCoord(5, 3));
            var result = MovementUndoTestData.Run(input);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.Events.OfType<CombatRoarEvent>(), Is.Not.Empty);
            Assert.That(result.FinalState.MovementUndo.Checkpoints, Is.Empty);
        }

        [Test]
        public void UndoRejectsPendingReactionWithoutExecutingIt()
        {
            var input = MovementTestData.Route(BossRoarTests.Input(0), new CellCoord(4, 3), new CellCoord(5, 3));
            var moved = MovementUndoTestData.Run(input);
            var current = MovementUndoTestData.After(moved);
            Assert.That(current.State.MovementUndo.TryGet(input.ActorId, out _), Is.True);
            current = CombatCommandTestData.With(current, characters: current.Characters.Select(c => c.EntityId == BossRoarTests.Boss
                ? c.With(stats: c.Stats.With(traitStatuses: c.Stats.TraitStatuses.ToArray().Select(t => t.Key == TraitKey.Roar
                    ? (TraitStatusSnapshot)new RoarTraitStatusSnapshot(5, 5) : t).ToArray())) : c).ToArray());
            var result = MovementUndoTestData.Run(MovementUndoTestData.Command(current, CombatCommandKind.UndoMove, moved.Input.Number));
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Reason, Is.EqualTo(MovementUndoFailureReason.PendingReaction.ToString()));
            Assert.That(result.Events, Is.Empty);
            Assert.That(result.Characters.Characters, Is.Empty);
        }

        [Test]
        public void NetZeroPositionEffectOnAnotherActorCannotRetainItsCheckpoint()
        {
            var input = MovementUndoTestData.Route();
            var b = CombatCommandTestData.Character(20, EntityKey.Unit_Akimbo, new CellCoord(1, 4), Owner.Player);
            input = TraitCombatFixtures.Characters(input, input.Characters.Append(b).ToArray());
            var inactive = new DestinationTileModifierFact(1, new CellCoord(2, 4), false, Array.Empty<Owner>(), 5, 0);
            input = WithTile(input, inactive);
            var bMove = MovementUndoTestData.Run(MovementUndoTestData.Command(input, CombatCommandKind.Move, actor: b.EntityId,
                path: new MovementPath(new[] { new CellCoord(1, 4), new CellCoord(2, 4) })));
            var current = MovementUndoTestData.Command(MovementUndoTestData.After(bMove), CombatCommandKind.Move, actor: input.ActorId, path: input.Path);
            var projection = new MovementProjection(current);
            projection.BeginOrdinaryMove(); projection.Enter(new CellCoord(2, 1)); projection.Stop(MovementStopReason.NextStepInvalid);
            var active = new DestinationTileModifierFact(1, inactive.Cell, true, Array.Empty<Owner>(), 5, 0);
            projection.ChangeSource(inactive, new MovementStepEffects(expectedSource: inactive, finalSource: active));
            projection.ReconcilePresence(new[] { inactive.Cell });
            projection.ChangeSource(active, new MovementStepEffects(expectedSource: active, finalSource: inactive));
            projection.ReconcilePresence(new[] { inactive.Cell });
            var result = MovementUndoPolicy.Reconcile(projection.Transition());
            Assert.That(result.FinalState.Environment.SameValues(current.State.Environment), Is.True);
            Assert.That(result.FinalState.MovementUndo.TryGet(b.EntityId, out _), Is.False);
        }

        private static CombatCommandInput WithTile(CombatCommandInput input, MovementTileFact source)
        {
            var environment = new MovementEnvironmentFacts(new[] { source });
            return CombatCommandTestData.With(input, state: input.State.With(environment: environment, environmentProgress: EnvironmentProgressFacts.Initial(environment)));
        }

        private sealed class EffectTile : MovementTileFact
        {
            internal string Effect { get; }
            internal EntityId Target { get; }
            internal EffectTile(long id, CellCoord cell, string effect, EntityId target, IReadOnlyList<Owner> known = null)
                : base(id, cell, "undo-barrier-fixture", 0, true, known ?? new[] { Owner.Player, Owner.Enemy }) { Effect = effect; Target = target; }
            public override bool SameValues(MovementTileFact other) => SameSourceValues(other) && other is EffectTile t && Effect == t.Effect && Target == t.Target;
            public override MovementTileFact WithKnownTo(IReadOnlyList<Owner> knownTo) => new EffectTile(Id, Cell, Effect, Target, knownTo);
        }

        private sealed class EffectRule : IMovementTileRule
        {
            public string Key => "undo-barrier-fixture";
            public IReadOnlyList<EntityId> TargetIds(MovementTileFact tile, EntityId mover, CombatCommandInput current) => new[] { ((EffectTile)tile).Target, mover }.Distinct().ToArray();
            public MovementStepEffects Evaluate(MovementStepInput input, MovementTileFact tile)
            {
                var source = (EffectTile)tile;
                bool hazard = input.Phase == MovementEffectPhase.Hazard;
                var debuff = new CharacterDebuffState(disarm: new DisarmState(new EffectOriginKey(EffectOwnerKind.Trait, 1, (int)TraitKey.Disarm, "main"), 1));
                return new MovementStepEffects(
                    damage: hazard && source.Effect is "damage" or "death" or "terminal"
                        ? new[] { new MovementHealthRequest(source.Target, source.Effect == "damage" ? 5 : 100) } : null,
                    healing: !hazard && source.Effect == "heal" ? new[] { new MovementHealthRequest(source.Target, 5) } : null,
                    forcedMovement: hazard && source.Effect == "relocate" ? new[] { new MovementForcedMoveRequest(source.Target, Rotation4.R0, 1) } : null,
                    debuffChanges: hazard && source.Effect == "net-zero-debuff" ? new[] {
                        new MovementDebuffChange(source.Target, CharacterDebuffState.Empty, debuff), new MovementDebuffChange(source.Target, debuff, CharacterDebuffState.Empty) } : null,
                    movementMethods: hazard && source.Effect == "net-zero-method" ? new[] {
                        new MovementMethodChange(source.Target, MoveMethod.LiftAndPlace, MoveMethod.None), new MovementMethodChange(source.Target, MoveMethod.None, MoveMethod.LiftAndPlace) } : null);
            }
        }
    }
}
