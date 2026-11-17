using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Movement;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Tests.Core
{
    public sealed class LavaChainGimmickTests
    {
        internal static readonly StageGimmickId Id = new(1);

        [Test]
        public void WarningAtTwoLocksHeadingAndActivationAtThreeDoesNotReroll()
        {
            var input = World(MovementTestData.SafeRoute(new CellCoord(2, 2), new CellCoord(2, 3)));
            Assert.That(At(input, 1, StageGimmickBoundaryPhase.Warning), Is.Null);
            var warning = At(input, 2, StageGimmickBoundaryPhase.Warning);
            Assert.That(warning, Is.Not.Null);
            var change = warning.Events.OfType<CombatLavaChainEvent>().Single();
            Assert.That(change.Final.IsWarning, Is.True);
            Assert.That(change.Final.NextBoundaryCount, Is.EqualTo(3));
            Assert.That(change.Final.Heading.HasValue, Is.True);
            var after = CombatCommandComposition.Project(warning);
            var result = At(after, 3, StageGimmickBoundaryPhase.Scheduled);
            Assert.That(result.FinalState.EnvironmentProgress.RandomState, Is.EqualTo(after.State.EnvironmentProgress.RandomState));
            Assert.That(result.Events.OfType<CombatLavaChainEvent>().Single().Final.NextBoundaryCount, Is.EqualTo(5));
        }

        [Test]
        public void HeadExtendsInOrderAndPushesPerpendicularlyOnlyOnce()
        {
            var input = Warned(new CellCoord(2, 2));
            var result = At(input, 3, StageGimmickBoundaryPhase.Scheduled);
            var chain = result.Events.OfType<CombatLavaChainEvent>().Single();
            Assert.That(chain.HeadPath, Is.EqualTo(Enumerable.Range(0, 8).Select(x => new CellCoord(x, 2))));
            Assert.That(chain.Step, Is.EqualTo(LavaChainGimmickStep.Retracted));
            var contact = result.Events.OfType<CombatLavaChainContactEvent>().Single(c => c.RecipientId == input.ActorId);
            Assert.That(contact.LandingCell, Is.EqualTo(new CellCoord(2, 1)));
            Assert.That(contact.FinalCell, Is.EqualTo(new CellCoord(2, 1)));
            Assert.That(contact.PathIndex, Is.EqualTo(2));
            Assert.That(contact.Survived, Is.True);
        }

        [Test]
        public void NewWallAfterWarningStopsWithoutAnotherDraw()
        {
            var input = Walls(Warned(new CellCoord(2, 2)), new CellCoord(1, 2));
            var result = At(input, 3, StageGimmickBoundaryPhase.Scheduled);
            Assert.That(result.Events.OfType<CombatLavaChainEvent>().Single().HeadPath, Is.EqualTo(new[] { new CellCoord(0, 2) }));
            Assert.That(result.FinalState.EnvironmentProgress.RandomState, Is.EqualTo(input.State.EnvironmentProgress.RandomState));
            Assert.That(result.Events.OfType<CombatLavaChainContactEvent>(), Is.Empty);
        }

        [Test]
        public void BlockedOuterLandingFallsBackToInnerSide()
        {
            var input = Walls(Warned(new CellCoord(2, 2)), new CellCoord(2, 1));
            var result = At(input, 3, StageGimmickBoundaryPhase.Scheduled);
            Assert.That(result.Events.OfType<CombatLavaChainContactEvent>().Single(c => c.RecipientId == input.ActorId).LandingCell,
                Is.EqualTo(new CellCoord(2, 3)));
        }

        [Test]
        public void NoLegalHeadingCommitsStillActivationWithoutRandomConsumption()
        {
            var input = World(MovementTestData.SafeRoute(new CellCoord(4, 4), new CellCoord(4, 5)));
            input = Walls(input, new CellCoord(0, 1), new CellCoord(1, 1), new CellCoord(1, 2), new CellCoord(1, 3), new CellCoord(0, 3));
            var warning = At(input, 2, StageGimmickBoundaryPhase.Warning);
            Assert.That(warning.Events.OfType<CombatLavaChainEvent>().Single().Final.Heading, Is.Null);
            Assert.That(warning.FinalState.EnvironmentProgress.RandomState, Is.EqualTo(input.State.EnvironmentProgress.RandomState));
            var result = At(CombatCommandComposition.Project(warning), 3, StageGimmickBoundaryPhase.Scheduled);
            Assert.That(result.Events.OfType<CombatLavaChainEvent>().Single().HeadPath.Count, Is.EqualTo(1));
        }

        internal static CombatCommandInput Warned(CellCoord actor) => World(
            MovementTestData.SafeRoute(actor, actor + new CellCoord(0, 1)),
            new LavaChainGimmickState(Id, new CellCoord(0, 2), 3, true, new CellCoord(1, 0)));

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void FailedPushDefeatsOrStopsBeforeReconstructedSurvivor(bool immune, bool reconstruction)
        {
            var input = Warned(new CellCoord(2, 2));
            var actor = input.Characters.Single(c => c.EntityId == input.ActorId);
            if (reconstruction)
            {
                actor = CombatCommandTestData.With(actor, traits: new TraitStatusSnapshot[] { new ReconstructionTraitStatusSnapshot(1, false) });
                input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId == actor.EntityId ? actor : c).ToArray());
            }
            if (immune)
            {
                input = CombatCommandTestData.With(input, profiles: input.Profiles.Select(p =>
                {
                    if (p.Definition.Key != actor.CharacterKey) return p;
                    var s = p.Definition;
                    return new CombatAttackProfile(new UnitSpec(s.Key, s.Role, s.FootprintWidth, s.FootprintHeight,
                        s.FootprintLocalCells.ToArray(), s.MaxHealth, s.MoveMethod, s.AttackRange, s.CategoryKey, s.AttackPower,
                        s.PhysicalDefense, s.Priority, s.Traits.ToArray(), s.OwnerEligibility, s.Occlusion,
                        new UnitBehaviorSpec(true, true, true)), p.Method);
                }).ToArray());
            }
            else input = Walls(input, new CellCoord(2, 1), new CellCoord(2, 3));
            var result = At(input, 3, StageGimmickBoundaryPhase.Scheduled);
            var contact = result.Events.OfType<CombatLavaChainContactEvent>().Single(c => c.RecipientId == actor.EntityId);
            Assert.That(contact.Failure, Is.EqualTo(immune ? LavaChainPushFailure.Immune : LavaChainPushFailure.NoLanding));
            Assert.That(contact.Survived, Is.EqualTo(reconstruction));
            Assert.That(contact.LandingCell, Is.Null);
            Assert.That(result.Events.OfType<CombatLavaChainEvent>().Single().Final.HeadCell,
                Is.EqualTo(reconstruction ? new CellCoord(1, 2) : new CellCoord(7, 2)));
            Assert.That(result.Events.OfType<CombatCharacterEvent>().Count(e => e.Kind == CombatEventKind.Reconstructed),
                Is.EqualTo(reconstruction ? 1 : 0));
        }

        [Test]
        public void EightLegalHeadingIndicesAreUsedExactlyOnceForEightConsecutiveRngResidues()
        {
            var input = World(MovementTestData.SafeRoute(new CellCoord(2, 2), new CellCoord(2, 3)));
            var selected = new CellCoord?[8];
            for (uint seed = 0; seed < 8; seed++)
            {
                uint random = seed;
                selected[seed] = LavaChainGimmickRule.ChooseHeading(input.State, new CellCoord(4, 4),
                    new System.Collections.Generic.HashSet<CellCoord>(), ref random);
            }
            Assert.That(selected, Is.EquivalentTo(new CellCoord?[]
            {
                new CellCoord(1, 0), new CellCoord(1, 1), new CellCoord(0, 1), new CellCoord(-1, 1),
                new CellCoord(-1, 0), new CellCoord(-1, -1), new CellCoord(0, -1), new CellCoord(1, -1)
            }));
        }

        [Test]
        public void ForecastPreservesPathOrderAndInvalidatesAfterNewBlocker()
        {
            var input = Warned(new CellCoord(2, 2));
            var before = StageHazardForecast.ReadSet.Capture(input);
            var changed = Walls(input, new CellCoord(3, 2));
            Assert.That(before.Matches(StageHazardForecast.ReadSet.Capture(changed)), Is.False);
            var facts = StageGimmickComposition.Default.ForecastSteps(changed, 3).Where(f => f.HasValue).Select(f => f.Value).ToArray();
            Assert.That(facts.Select(f => f.Cell), Is.EqualTo(new CellCoord?[] { new CellCoord(1, 2), new CellCoord(2, 2) }));
            Assert.That(changed.State.EnvironmentProgress.RandomState, Is.EqualTo(input.State.EnvironmentProgress.RandomState));
        }

        internal static CombatCommandInput World(CombatCommandInput source, LavaChainGimmickState prepared = null)
        {
            var definitions = new StageGimmickSpec[] { new LavaChainGimmickSpec(new CellCoord(0, 2), 3, 1, 1) };
            var state = source.State.With(gimmicks: new StageGimmickStateSet(new StageGimmickState[]
                { prepared ?? new LavaChainGimmickState(Id, new CellCoord(0, 2), 2) }),
                environmentProgress: new EnvironmentProgressFacts(1, 1, 123));
            var battle = new CombatBattleFacts(8, 42, 2, 2, Array.Empty<CharacterInstanceId>(), 0, gimmicks: definitions);
            return new CombatCommandInput(source.Number, source.Kind, source.ActorId, source.Aim, state, source.Characters,
                source.Profiles, source.Main, source.Owned, source.SoulCounts, source.Souls, source.Artifacts, source.TargetCell, source.Rotation,
                battle, source.ProcessingKind, source.Path, source.MovementContext, source.BattleInstanceId,
                environmentBindings: source.EnvironmentBindings, expectedBattleInstanceId: source.ExpectedBattleInstanceId,
                expectedControllerTurnSequence: source.ExpectedControllerTurnSequence);
        }

        [Test]
        public void SameCharacterCanBeContactedAgainAtALaterHeadStep()
        {
            var input = World(MovementTestData.SafeRoute(new CellCoord(2, 2), new CellCoord(2, 3)),
                new LavaChainGimmickState(Id, CellCoord.Zero, 3, true, new CellCoord(1, 1)));
            var actor = input.Characters.Single(c => c.EntityId == input.ActorId);
            actor = actor.WithPlacement(actor.Area, actor.ReserveSlotIndex, actor.Anchor, actor.Rotation,
                new[] { new CellCoord(2, 2), new CellCoord(3, 2), new CellCoord(4, 2) });
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId == actor.EntityId ? actor : c).ToArray());
            var result = At(input, 3, StageGimmickBoundaryPhase.Scheduled);
            var contacts = result.Events.OfType<CombatLavaChainContactEvent>().Where(c => c.RecipientId == actor.EntityId).ToArray();
            Assert.That(contacts.Select(c => c.PathIndex), Is.EqualTo(new[] { 2, 3 }));
            Assert.That(contacts.Select(c => c.LandingCell), Is.EqualTo(new CellCoord?[] { new CellCoord(1, 3), new CellCoord(0, 4) }));
            Assert.That(result.Events.OfType<CombatLavaChainEvent>().Single().Final.HeadCell, Is.EqualTo(new CellCoord(7, 7)));
            Assert.That(result.Events.OfType<CombatCharacterEvent>().Where(e => e.Kind == CombatEventKind.Moved)
                .All(e => e.Final.OccupiedCells.Count == 3), Is.True);
            var turnEvent = new CombatTurnEvent(CombatEventKind.PlayerTurnEnded, EntityId.Empty, result.Input.State.Turn, result.FinalState.Turn);
            Assert.DoesNotThrow(() => LavaChainGimmickEvidenceValidator.Validate(CombatCommandComposition.WithEvents(result,
                new CombatEvent[] { turnEvent }.Concat(result.Events).ToArray())));
        }

        [Test]
        public void SuccessfulContactWithoutItsActualPushCannotPublishAStoppedHead()
        {
            var input = Warned(new CellCoord(2, 2));
            var result = At(input, 3, StageGimmickBoundaryPhase.Scheduled);
            var turnEvent = new CombatTurnEvent(CombatEventKind.PlayerTurnEnded, EntityId.Empty, result.Input.State.Turn, result.FinalState.Turn);
            var valid = CombatCommandComposition.WithEvents(result, new CombatEvent[] { turnEvent }.Concat(result.Events).ToArray());
            Assert.DoesNotThrow(() => LavaChainGimmickEvidenceValidator.Validate(valid));
            var contact = result.Events.OfType<CombatLavaChainContactEvent>().Single();
            var stopped = new CombatLavaChainContactEvent(contact.GimmickId, contact.PathIndex, contact.ContactCell, contact.RecipientId,
                contact.From, contact.LandingCell, contact.From, LavaChainPushFailure.None, true);
            var advance = result.Events.OfType<CombatLavaChainEvent>().Single();
            var final = new LavaChainGimmickState(Id, new CellCoord(1, 2), advance.Final.NextBoundaryCount);
            var truncated = new CombatLavaChainEvent(advance.Step, advance.Boundary, advance.Expected, final,
                new[] { new CellCoord(0, 2), new CellCoord(1, 2) });
            var forged = CombatCommandComposition.WithEvents(valid, new CombatEvent[] { turnEvent, stopped, truncated })
                .WithFinalState(valid.FinalState.With(gimmicks: valid.FinalState.Gimmicks.Replace(final)));
            Assert.Throws<InvalidOperationException>(() => LavaChainGimmickEvidenceValidator.Validate(forged));
        }

        internal static CombatCommandTransition At(CombatCommandInput input, long count, StageGimmickBoundaryPhase phase)
        {
            var turn = new CombatTurnFacts(StagePhase.Combat, controllerPhase: CombatControllerPhase.StartingEnemy,
                controllerTurnSequence: 1, completedPlayerTurnCount: count / 2, completedControllerTurnCount: count);
            return CombatGimmickCalculator.TryAdvance(CombatCommandTestData.With(input,
                state: input.State.With(turn: turn, phase: CombatFlowPhase.Automatic)),
                new StageGimmickBoundary(Owner.Player, count, phase), Array.Empty<IMovementTileRule>());
        }

        internal static CombatCommandInput Walls(CombatCommandInput input, params CellCoord[] cells)
        {
            var tiles = cells.Select((cell, i) => new StaticTerrainFact(i + 1, cell, StaticTerrainKind.Wall, true,
                new[] { Owner.Player, Owner.Enemy })).ToArray();
            var environment = new MovementEnvironmentFacts(tiles);
            return input.WithWorld(input.State.With(environment: environment, environmentProgress: EnvironmentProgressFacts.Initial(environment, input.State.EnvironmentProgress.RandomState)),
                input.Characters, input.Main, input.SoulCounts, input.Souls,
                new EnvironmentSemanticBindings(tiles.Select(t => t.Id).ToArray(), Array.Empty<long>(), Array.Empty<EntityId>()));
        }
    }
}
