using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Tests.Core
{
    public sealed class TornadoRuntimePathTests
    {
        [Test]
        public void ActualTilePushIntoStormInterruptsBeforeNextStep()
        {
            var sources = TornadoGimmickResolutionTests.WallsExcept(6, 6, new CellCoord(3, 0), new CellCoord(3, 1),
                new CellCoord(3, 2), new CellCoord(3, 3), new CellCoord(3, 4), new CellCoord(5, 5))
                .Append((MovementTileFact)new TornadoGimmickResolutionTests.PushTile()).ToArray();
            var input = TornadoGimmickResolutionTests.Input(width: 6, height: 6, center: new CellCoord(1, 1), victim: new CellCoord(3, 0),
                multiple: true, secondCenter: new CellCoord(4, 3), sources: sources);
            input = input.WithIntent(CombatCommandKind.Move, default, new CellCoord(3, 1),
                new MovementPath(new[] { new CellCoord(3, 0), new CellCoord(3, 1) }), null);
            var rules = TornadoEnvironmentTests.Rules.Append(new TornadoGimmickResolutionTests.PushRule()).ToArray();
            var context = MovementContextCalculator.Capture(input, Owner.Player, rules);
            var result = CombatCommandCalculator.Calculate(input.WithIntent(input.Kind, input.Aim, input.Path.Endpoint, input.Path, context), rules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.Events.OfType<CombatCharacterEvent>().Single(c => c.Kind == CombatEventKind.Relocated).TornadoSourceId, Is.EqualTo(32));
            Assert.That(result.Events.OfType<CombatCharacterEvent>().Where(c => c.Kind == CombatEventKind.Moved).Select(c => c.Final.Anchor.Value),
                Is.EqualTo(new[] { new CellCoord(3, 1), new CellCoord(3, 2) }));
            Assert.DoesNotThrow(() => CombatChangeEvidenceReader.ValidateGimmickChanges(result));
        }
        [Test]
        public void ActualKnockbackIntoStormInterruptsItsRemainingPath()
        {
            var input = CombatCommandTestData.Input();
            input = TraitCombatFixtures.Characters(input, input.Characters.Where(c => c.EntityId.Value == 1 || c.EntityId.Value == 2 || c.EntityId.Value == 10).ToArray());
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId == input.ActorId ?
                CombatCommandTestData.With(c, traits: new ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits.TraitStatusSnapshot[] {
                    new ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits.KnockbackTraitStatusSnapshot(3) }) : c).ToArray(),
                profiles: input.Profiles.Select(p => p.Definition.Key != EntityKey.Unit_Akimbo ? p : new CombatAttackProfile(
                    CombatCommandTestData.Spec(EntityKey.Unit_Akimbo, 20, CategoryKey.Main,
                        new ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits.KnockbackTraitSpec(3)), p.Method)).ToArray());
            input = TraitTrailTestData.Environment(input, TornadoEnvironmentTests.Source(cell: new CellCoord(5, 3)));
            var result = CombatCommandCalculator.Calculate(input, TornadoEnvironmentTests.Rules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.Attack.Value.Knockbacks.Single().Path, Is.EqualTo(new[] { new CellCoord(5, 3) }));
            Assert.That(result.Events.OfType<CombatCharacterEvent>().Count(c => c.Kind == CombatEventKind.Relocated), Is.EqualTo(1));
            Assert.DoesNotThrow(() => CombatChangeEvidenceReader.ValidateGimmickChanges(result));
        }

        [Test]
        public void ActualPullIntoStormDoesNotDeliverFollowupAtObsoleteEndpoint()
        {
            var input = PullTestData.Input(extra: 3, distance: 4);
            input = TraitTrailTestData.Environment(input, TornadoEnvironmentTests.Source(cell: new CellCoord(10, 7)));
            var result = PullTestData.Run(input);
            Assert.That(result.Attack.Value.Pull.Path, Is.EqualTo(new[] { new CellCoord(10, 7) }));
            Assert.That(result.Events.OfType<CombatCharacterEvent>().Count(c => c.Kind == CombatEventKind.Relocated), Is.EqualTo(1));
            Assert.That(PullTestData.Final(result).Health.CurrentHealth, Is.EqualTo(100));
            Assert.DoesNotThrow(() => CombatChangeEvidenceReader.ValidateGimmickChanges(result));
        }
        [Test]
        public void FreshPreviewPredictsActualEntryWithoutSpendingStoredRandomness()
        {
            var input = Move(TornadoGimmickResolutionTests.Input());
            uint random = input.State.EnvironmentProgress.RandomState;
            var context = MovementContextCalculator.Capture(input, Owner.Player, TornadoEnvironmentTests.Rules);
            var preview = CombatMovementCalculator.Preview(input, TornadoEnvironmentTests.Rules);
            var actual = CombatCommandCalculator.Calculate(input.WithIntent(input.Kind, input.Aim, input.Path.Endpoint, input.Path, context), TornadoEnvironmentTests.Rules);
            Assert.That(actual.Accepted, Is.True, actual.Reason);
            Assert.That(actual.Events.OfType<CombatCharacterEvent>().Count(e => e.Kind == CombatEventKind.Relocated), Is.EqualTo(1));
            Assert.That(actual.Characters.Characters.Single(c => c.Final.EntityId == input.ActorId).Final.Anchor, Is.EqualTo(preview.ProjectedActor.Anchor));
            Assert.That(input.State.EnvironmentProgress.RandomState, Is.EqualTo(random));
        }

        [Test]
        public void ChangedVisibleAreaInvalidatesPreviewEvenWhenThrowDestinationIsUnchanged()
        {
            var input = Move(TornadoGimmickResolutionTests.Input());
            var context = MovementContextCalculator.Capture(input, Owner.Player, TornadoEnvironmentTests.Rules);
            var before = (TornadoGimmickState)input.State.Gimmicks.Entries.Single();
            var old = before.Members.Single();
            var shifted = new TornadoGimmickState(before.Id, new[] { new TornadoMemberState(old.SourceId, new CellCoord(2, 3),
                old.HorizontalSign, old.Phase, old.NextBoundaryCount, old.LockedDirection) });
            var changed = input.WithWorld(input.State.With(gimmicks: input.State.Gimmicks.Replace(shifted)), input.Characters,
                input.Main, input.SoulCounts, input.Souls, input.EnvironmentBindings);
            Assert.That(MovementContextCalculator.Matches(context, changed, Owner.Player, TornadoEnvironmentTests.Rules), Is.False);
        }

        [Test]
        public void HiddenSourceDoesNotAppearOrThrowInPublicPreview()
        {
            var source = Move(TornadoGimmickResolutionTests.Input());
            var member = new TornadoMemberSpec(31, new[] { Owner.Enemy }, new CellCoord(1, 3));
            var battle = new CombatBattleFacts(4, 1, 2, 0, Array.Empty<ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids.CharacterInstanceId>(), 0,
                gimmicks: new[] { new TornadoGimmickSpec(new[] { member }) });
            var input = new CombatCommandInput(source.Number, source.Kind, source.ActorId, source.Aim, source.State, source.Characters,
                source.Profiles, source.Main, source.Owned, source.SoulCounts, source.Souls, source.Artifacts,
                battle: battle, path: source.Path, battleInstanceId: 1, expectedBattleInstanceId: 1, expectedControllerTurnSequence: 1);
            var context = MovementContextCalculator.Capture(input, Owner.Player, TornadoEnvironmentTests.Rules);
            Assert.That(context.VisibleTornadoAreas, Is.Empty);
            Assert.That(TornadoAreaFact.Read(input.State, battle.GimmickDefinitions, Owner.Player), Is.Empty);
            Assert.That(TornadoAreaFact.Read(input.State, battle.GimmickDefinitions, Owner.Enemy).Count, Is.EqualTo(1));
            var preview = CombatMovementCalculator.Preview(input, TornadoEnvironmentTests.Rules);
            Assert.That(preview.PublicEffects.OfType<CombatCharacterEvent>().Where(e => e.Kind == CombatEventKind.Relocated), Is.Empty);
        }

        [Test]
        public void ChangedOffPathLandingBlockerInvalidatesContextWithoutSpendingStoredRandomness()
        {
            var input = Move(TornadoGimmickResolutionTests.Input());
            var context = MovementContextCalculator.Capture(input, Owner.Player, TornadoEnvironmentTests.Rules);
            var terrain = new MovementEnvironmentFacts(new MovementTileFact[] { new StaticTerrainFact(901,
                CellCoord.Zero, StaticTerrainKind.Wall, true, new[] { Owner.Player, Owner.Enemy }) });
            var changed = input.WithWorld(input.State.With(environment: terrain, environmentProgress: new EnvironmentProgressFacts(1, 902, input.State.EnvironmentProgress.RandomState)), input.Characters, input.Main, input.SoulCounts,
                input.Souls, new EnvironmentSemanticBindings(new long[] { 901 }, Array.Empty<long>(), Array.Empty<EntityId>()));
            Assert.That(MovementContextCalculator.Matches(context, changed, Owner.Player, TornadoEnvironmentTests.Rules), Is.False);
            var result = MovementContextCalculator.RejectStale(changed);
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.FinalState.EnvironmentProgress.SameValues(changed.State.EnvironmentProgress), Is.True);
            Assert.That(result.Events, Is.Empty);
        }
        [Test]
        public void NestedThrowsReturningToFirstTilePushStepStillInterruptTheOldRoute()
        {
            var allowed = new[] { new CellCoord(3, 0), new CellCoord(3, 1), new CellCoord(3, 2),
                new CellCoord(3, 3), new CellCoord(3, 4), new CellCoord(1, 1), new CellCoord(5, 5) };
            var input = TornadoGimmickResolutionTests.Input(width: 6, height: 6, victim: new CellCoord(3, 0),
                sources: TornadoGimmickResolutionTests.WallsExcept(6, 6, allowed)
                    .Append((MovementTileFact)new TornadoGimmickResolutionTests.PushTile()).ToArray());
            var spec = new TornadoGimmickSpec(new[] {
                new TornadoMemberSpec(31, new[] { Owner.Player, Owner.Enemy }, new CellCoord(3, 2), 1, sideLength: 1),
                new TornadoMemberSpec(32, new[] { Owner.Player, Owner.Enemy }, new CellCoord(1, 1), 1, sideLength: 1) });
            var group = new TornadoGimmickState(new StageGimmickId(1), spec.Members.Select(m =>
                new TornadoMemberState(m.SourceId, m.InitialCenter.Value, 1, TornadoGimmickPhase.Cooldown, 9, null)).ToArray());
            var state = input.State.With(gimmicks: new StageGimmickStateSet(new[] { group }),
                environmentProgress: new EnvironmentProgressFacts(1, 901, 4573));
            input = input.WithWorld(state, input.Characters, input.Main, input.SoulCounts, input.Souls, input.EnvironmentBindings);
            var old = input.Battle;
            input = new CombatCommandInput(input.Number, CombatCommandKind.Move, input.ActorId, input.Aim, state, input.Characters,
                input.Profiles, input.Main, input.Owned, input.SoulCounts, input.Souls, input.Artifacts,
                battle: new CombatBattleFacts(old.ReserveCapacity, old.TimelineSeed, old.PlayerZone, old.DrawOrder, old.DrawCursor,
                    gimmicks: new[] { spec }), path: new MovementPath(new[] { new CellCoord(3, 0), new CellCoord(3, 1) }),
                battleInstanceId: 1, expectedBattleInstanceId: 1, expectedControllerTurnSequence: 1, environmentBindings: input.EnvironmentBindings);
            var rules = TornadoEnvironmentTests.Rules.Append(new TornadoGimmickResolutionTests.PushRule()).ToArray();
            var context = MovementContextCalculator.Capture(input, Owner.Player, rules);
            var result = CombatCommandCalculator.Calculate(input.WithIntent(input.Kind, input.Aim, input.Path.Endpoint, input.Path, context), rules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.Events.OfType<CombatCharacterEvent>().Where(e => e.Kind == CombatEventKind.Relocated).Select(e => e.Final.Anchor.Value),
                Is.EqualTo(new[] { new CellCoord(1, 1), new CellCoord(3, 2) }));
            Assert.That(result.Events.OfType<CombatCharacterEvent>().Where(e => e.Kind == CombatEventKind.Moved).Select(e => e.Final.Anchor.Value),
                Is.EqualTo(new[] { new CellCoord(3, 1), new CellCoord(3, 2) }));
            Assert.That(result.Characters.Characters.Single(c => c.Final.EntityId == input.ActorId).Final.Anchor, Is.EqualTo(new CellCoord(3, 2)));
            Assert.DoesNotThrow(() => CombatChangeEvidenceReader.ValidateGimmickChanges(result));
        }

        [Test]
        public void HistoricalPullPathCannotAuthorizeTheSameEntryAndThrowTwice()
        {
            var input = TraitTrailTestData.Environment(PullTestData.Input(extra: 3, distance: 4),
                TornadoEnvironmentTests.Source(cell: new CellCoord(10, 7)));
            var result = PullTestData.Run(input);
            var entry = result.Events.OfType<CombatTornadoEntryEvent>().Single();
            var relocation = result.Events.OfType<CombatCharacterEvent>().Single(e => e.Kind == CombatEventKind.Relocated);
            var events = result.Events.ToList();
            events.InsertRange(events.IndexOf(relocation) + 1, new CombatEvent[] { entry, relocation });
            var forged = new CombatCommandTransition(input, true, "", result.Characters, result.Field, result.FinalState,
                result.Outcome, events: events, attack: result.Attack);
            Assert.Throws<InvalidOperationException>(() => CombatChangeEvidenceReader.ValidateGimmickChanges(forged));
        }

        [TestCase(1)]
        [TestCase(3)]
        public void BothForwardSwapArrivalsContactStormAndCannotBeUndone(int stormX)
        {
            var input = TraitTrailTestData.Environment(PositionSwapMovementTests.Route(),
                TornadoEnvironmentTests.Source(cell: new CellCoord(stormX, 1)));
            var result = MovementUndoTestData.Run(input, TornadoEnvironmentTests.Rules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            var relocated = result.Events.OfType<CombatCharacterEvent>().Single(e => e.Kind == CombatEventKind.Relocated);
            Assert.That(relocated.Final.EntityId, Is.EqualTo(new EntityId(stormX == 1 ? 20 : 1)));
            Assert.That(result.FinalState.MovementUndo.Checkpoints, Is.Empty);
            Assert.DoesNotThrow(() => CombatChangeEvidenceReader.ValidateGimmickChanges(result));
        }
        private static CombatCommandInput Move(CombatCommandInput input) => input.WithIntent(CombatCommandKind.Move, default,
            new CellCoord(2, 3), new MovementPath(new[] { new CellCoord(1, 3), new CellCoord(2, 3) }), null);
    }
}
