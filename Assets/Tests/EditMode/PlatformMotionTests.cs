using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;

namespace ProjectL.Tests.Core
{
    public sealed class PlatformMotionTests
    {
        private static readonly IMovementTileRule[] NoRules = Array.Empty<IMovementTileRule>();

        [TestCase(false)]
        [TestCase(true)]
        public void ExceptionalObstacleOccupancyKeepsPlatformSupportedUntilItLeaves(bool pillar)
        {
            // 정상 넉백은 탑승을 거부한다. 이미 점유한 예외 상태에서도 지지면을 보존한다.
            var obstacle = pillar ? PillarTestData.Pillar(cell: new CellCoord(5, 3), hits: 3)
                : ObstacleReactionTests.Barrel(51, new CellCoord(5, 3), 3, 0);
            var platform = new MovingPlatformFact(new MovingPlatformSpec(100, 1, PlatformAxis.Horizontal,
                new CellCoord(5, 3), new CellCoord(6, 3), new CellCoord(5, 3), 1));
            var seed = Input(platform);
            var input = WithObstacles(CombatCommandTestData.With(seed,
                characters: seed.Characters.Where(c => c.EntityId.Value != 2 && c.EntityId.Value != 4).ToArray()), obstacle.Structure, obstacle.Profile);
            var result = Run(input);
            Assert.That(Motion(result).StopReason, Is.EqualTo(PlatformMotionStopReason.NontransportableOccupant));
            Assert.That(Motion(result).Steps, Is.Empty);
            Assert.That(Motion(result).Final.SameValues(platform), Is.True);
            Assert.That(result.FinalState.Structures.Single().Anchor, Is.EqualTo(platform.Cell));
            Assert.That(result.FinalState.Structures.Single().ObstacleDurability, Is.EqualTo(input.State.Structures.Single().ObstacleDurability));
            Assert.That(result.Events.OfType<CombatFallEvent>(), Is.Empty);
            Assert.That(result.FinalState.LastPlatformBoundaryCount, Is.EqualTo(1));

            var next = CombatCommandComposition.Project(result);
            var current = next.State.Structures.Single();
            var movedOff = new StructureCombatFacts(current.EntityId, current.Definition, current.ObstacleDurability,
                new CellCoord(4, 3), current.Rotation, new[] { new CellCoord(4, 3) }, current.Owner);
            next = WithObstacles(next.WithWorld(next.State.With(turn: new CombatTurnFacts(StagePhase.Combat,
                CombatControllerPhase.StartingPlayer, 2, 1, completedControllerTurnCount: 2)), next.Characters,
                next.Main, next.SoulCounts, next.Souls, next.EnvironmentBindings), movedOff, obstacle.Profile);
            Assert.That(Motion(Run(next)).Final.Cell, Is.EqualTo(new CellCoord(6, 3)));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void OnlyLiveObstaclesBlockAnyOccupiedPlatformCell(bool destroyed)
        {
            var input = Input(Platform(100, 2, 1, 4, 1));
            var obstacle = ObstacleReactionTests.Barrel(51, new CellCoord(1, 1), 3, 0);
            var structure = destroyed ? obstacle.Structure.WithObstacleDurability(new ObstacleDurabilitySnapshot(0, 3)) : obstacle.Structure;
            var result = Run(WithObstacles(input, structure, obstacle.Profile));
            Assert.That(Motion(result).Final.Cell.X, Is.EqualTo(destroyed ? 3 : 1));
            Assert.That(Motion(result).StopReason, Is.EqualTo(destroyed
                ? PlatformMotionStopReason.BudgetConsumed : PlatformMotionStopReason.NontransportableOccupant));
        }

        [Test]
        public void EarlierPlatformDestructionReleasesTheNextPlatformsObstacleOccupancy()
        {
            var input = Input(new[] { Platform(100, 2, 1, 4, 1), Platform(101, 1, 3, 6, 3) });
            var rider = CombatCommandTestData.With(CombatCommandTestData.Character(2, EntityKey.Unit_Gravekeeper,
                new CellCoord(1, 0), Owner.Enemy, attack: 20), hp: 1,
                traits: new TraitStatusSnapshot[] { new SelfDestructTraitStatusSnapshot(1) });
            input = Riders(input, rider);
            input = CombatCommandTestData.With(input, profiles: input.Profiles.Append(new CombatAttackProfile(
                CombatCommandTestData.Spec(EntityKey.Unit_Gravekeeper, 20, CategoryKey.Main, new SelfDestructTraitSpec(1)),
                input.Profiles[0].Method)).ToArray(),
                state: EnvironmentDamageTests.WithDamageTiles(input.State, (new CellCoord(2, 0), 2)));
            var obstacle = ObstacleReactionTests.Barrel(51, new CellCoord(3, 0), 1, 0);
            var result = Run(WithObstacles(input, obstacle.Structure, obstacle.Profile), new IMovementTileRule[] { new EnvironmentDamageTests.DamageRule() });
            Assert.That(result.FinalState.Structures.Single().IsDestroyed, Is.True);
            Assert.That(result.Events.OfType<CombatPlatformMotionEvent>().Last().Final.Cell.X, Is.EqualTo(4));
            CombatChangeEvidenceReader.ReadAll(result);
        }

        [Test]
        public void EvidenceRejectsMovingAwayFromLiveObstacle()
        {
            var input = Input(Platform(100, 1, 1, 4, 1));
            var valid = Run(input);
            var obstacle = ObstacleReactionTests.Barrel(51, new CellCoord(1, 0), 3, 0);
            var occupied = WithObstacles(input, obstacle.Structure, obstacle.Profile);
            var forged = new CombatCommandTransition(occupied, true, "", valid.Characters, valid.Field,
                valid.FinalState.With(structures: occupied.State.Structures), valid.Outcome, valid.Attack, valid.ConsumedSoulIds,
                events: valid.Events, healthExecutions: valid.HealthExecutions, healthClaims: valid.HealthClaims);
            Assert.Throws<InvalidOperationException>(() => PlatformMotionEvidenceValidator.Validate(forged));
        }

        [Test]
        public void EvidenceRejectsWrongStopReasonForExceptionalObstacleOccupancy()
        {
            var obstacle = ObstacleReactionTests.Barrel(51, new CellCoord(1, 0), 3, 0);
            var valid = Run(WithObstacles(Input(Platform(100, 1, 1, 4, 1)), obstacle.Structure, obstacle.Profile));
            var motion = Motion(valid);
            var events = valid.Events.Select(e => ReferenceEquals(e, motion)
                ? new CombatPlatformMotionEvent(motion.BoundaryCount, motion.OrderIndex, motion.Expected, motion.Final,
                    motion.Steps, PlatformMotionStopReason.Blocked) : e).ToArray();
            var forged = new CombatCommandTransition(valid.Input, true, "", valid.Characters, valid.Field,
                valid.FinalState, valid.Outcome, valid.Attack, valid.ConsumedSoulIds, events: events,
                healthExecutions: valid.HealthExecutions, healthClaims: valid.HealthClaims);
            Assert.Throws<InvalidOperationException>(() => PlatformMotionEvidenceValidator.Validate(forged));
        }

        private static CombatCommandInput WithObstacles(CombatCommandInput input, StructureCombatFacts obstacle, ObstacleContentProfile profile) =>
            new CombatCommandInput(input.Number, input.Kind, input.ActorId, input.Aim,
                input.State.With(structures: new[] { obstacle }), input.Characters, input.Profiles,
                input.Main, input.Owned, input.SoulCounts, input.Souls, input.Artifacts, battle: input.Battle,
                processingKind: input.ProcessingKind, battleInstanceId: input.BattleInstanceId,
                environmentBindings: new EnvironmentSemanticBindings(input.EnvironmentBindings.TerrainSourceIds,
                    input.EnvironmentBindings.ObjectSourceIds, new[] { obstacle.EntityId }), obstacleContents: new[] { profile });

        [Test]
        public void FirstEnemyTurnMovesBeforeTurnStart()
        {
            var input = Input(ReboundPlatform());
            var result = CombatCommandCalculator.Advance(input, NoRules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.FinalState.Turn.ControllerPhase, Is.EqualTo(CombatControllerPhase.StartingEnemy));
            Assert.That(result.FinalState.Turn.ControllerTurnSequence, Is.EqualTo(1));
            Assert.That(result.Events.Any(e => e.Kind == CombatEventKind.ControllerTurnStarted), Is.False);
            Assert.That(result.FinalState.Environment.Tiles.OfType<MovingPlatformFact>().Single().Cell, Is.EqualTo(new CellCoord(2, 0)));
        }

        [Test]
        public void ThreeOneReboundConsumesRemainingTwo()
        {
            var result = CombatCommandCalculator.Advance(Input(ReboundPlatform()), NoRules);
            var platform = result.FinalState.Environment.Tiles.OfType<MovingPlatformFact>().Single();
            Assert.That(platform.Cell, Is.EqualTo(new CellCoord(2, 0)));
            Assert.That(platform.TravelSign, Is.EqualTo(-1));
            Assert.That(Motion(result).Steps.Where(s => s.StepKind == PlatformStepKind.Translation).Select(s => s.Final.Cell.X),
                Is.EqualTo(new[] { 4, 3, 2 }));
            Assert.That(result.FinalState.LastPlatformBoundaryCount, Is.EqualTo(1));
            Assert.That(result.FinalState.EnvironmentProgress.RandomState, Is.EqualTo(42));
            PlatformMotionEvidenceValidator.Validate(result);
        }

        [Test]
        public void PreludeDoesNotRefreshActionsOrRunStartEffects()
        {
            var input = Input(ReboundPlatform());
            var reconstructing = CombatCommandTestData.With(ReconstructionTraitTests.Holding(1, used: true),
                position: new CellCoord(5, 5), actionUse: UnitActionUse.Attacked).WithStatusEffects(new StatusEffectSet(new AppliedStatus[]
                { new ReconstructionStatus(new EffectOriginKey(EffectOwnerKind.Trait, 2, (int)TraitKey.Reconstruction, "main"), 1) }));
            input = CombatCommandTestData.With(input,
                characters: input.Characters.Select(c => c.EntityId.Value == 2 ? reconstructing : c.EntityId.Value == 3
                    ? c.WithStatusEffects(PeriodicStatusTurnTests.Both()) : c).ToArray(),
                profiles: input.Profiles.Append(new CombatAttackProfile(CombatCommandTestData.Spec(EntityKey.Unit_Gravekeeper,
                    1, CategoryKey.Main, new ReconstructionTraitSpec(1)), input.Profiles[0].Method)).ToArray());
            var result = CombatCommandCalculator.Advance(input, NoRules);
            var final = CombatCommandComposition.Project(result);
            Assert.That(final.Characters.Select(c => c.ActionUse), Is.All.EqualTo(UnitActionUse.Attacked));
            Assert.That(result.FinalState.Turn.ControllerPhase, Is.EqualTo(CombatControllerPhase.StartingEnemy));
            Assert.That(ReconstructionTraitTests.Reconstruction(final.Characters.Single(c => c.EntityId.Value == 2)).RemainingOwnTurnStarts, Is.EqualTo(1));
            Assert.That(final.Characters.Single(c => c.EntityId.Value == 3).Health.CurrentHealth, Is.EqualTo(100));
            var started = CombatCommandComposition.Project(CombatCommandCalculator.Advance(final, NoRules));
            Assert.That(ReconstructionTraitTests.Reconstruction(started.Characters.Single(c => c.EntityId.Value == 2)), Is.Null);
            Assert.That(started.Characters.Single(c => c.EntityId.Value == 3).Health.CurrentHealth, Is.EqualTo(94));
        }

        [Test]
        public void FirstPlayerTurnSkipsPlatformPrelude()
        {
            var input = Input(ReboundPlatform(), completed: 0, phase: CombatControllerPhase.StartingPlayer);
            var result = CombatCommandCalculator.Advance(input, NoRules);
            Assert.That(result.FinalState.Turn.ControllerPhase, Is.EqualTo(CombatControllerPhase.Player));
            Assert.That(result.Events.Count(e => e.Kind == CombatEventKind.ControllerTurnStarted), Is.EqualTo(1));
            Assert.That(result.FinalState.Environment.Tiles.OfType<MovingPlatformFact>().Single().Cell, Is.EqualTo(new CellCoord(3, 0)));
        }

        [Test]
        public void ExactEndpointStoresInwardSign()
        {
            var result = Run(Input(Platform(100, 1, 3, 4, 3)));
            Assert.That(Motion(result).Final.TravelSign, Is.EqualTo(-1));
            Assert.That(Motion(result).Steps.Select(s => s.StepKind), Is.EqualTo(new[] { PlatformStepKind.Translation, PlatformStepKind.Turnaround }));
        }

        [Test]
        public void BothSidesBlockedStillMarksBoundaryAndNextAdvanceStartsTurn()
        {
            var input = Input(Platform(100, 1, 1, 3, 2));
            input = Fill(input, new CellCoord(1, 0), new CellCoord(3, 0));
            var prelude = Run(input);
            Assert.That(Motion(prelude).Steps, Is.Empty);
            Assert.That(Motion(prelude).Final.TravelSign, Is.EqualTo(1));
            Assert.That(Motion(prelude).StopReason, Is.EqualTo(PlatformMotionStopReason.Blocked));
            var next = CombatCommandCalculator.Advance(CombatCommandComposition.Project(prelude), NoRules);
            Assert.That(next.FinalState.Turn.ControllerTurnSequence, Is.EqualTo(2));
            Assert.That(next.Events.OfType<CombatPlatformMotionEvent>(), Is.Empty);
        }

        [Test]
        public void LongerPlatformsMoveFirstAndTiesUseSeededRandom()
        {
            var input = Input(new[] { Platform(101, 1, 1, 4, 1), Platform(102, 1, 1, 4, 1, y: 1),
                Platform(100, 2, 1, 4, 1, y: 2) });
            var first = Run(input);
            var repeat = Run(input);
            Assert.That(first.Events.OfType<CombatPlatformMotionEvent>().First().PlatformSourceId, Is.EqualTo(100));
            var boundary = first.Events.OfType<CombatPlatformBoundaryEvent>().Single();
            Assert.That(boundary.OrderingDraws.Count, Is.EqualTo(1));
            Assert.That(boundary.OrderingDraws[0].ChoiceCount, Is.EqualTo(2));
            Assert.That(first.FinalState.EnvironmentProgress.RandomState, Is.Not.EqualTo(42));
            Assert.That(boundary.OrderedPlatformIds, Is.EqualTo(repeat.Events.OfType<CombatPlatformBoundaryEvent>().Single().OrderedPlatformIds));
            Assert.That(input.State.EnvironmentProgress.RandomState, Is.EqualTo(42));
        }

        [Test]
        public void ForgedRandomDrawCannotChangeFinalStateOrSelection()
        {
            var result = Run(Input(new[] { Platform(100, 1, 1, 4, 1), Platform(101, 1, 1, 4, 1, y: 1) }));
            var draw = result.Events.OfType<CombatEnvironmentRandomEvent>().Single();
            Assert.Throws<ArgumentException>(() => new CombatEnvironmentRandomEvent(draw.ExpectedState,
                draw.FinalState + 1, draw.ChoiceCount, draw.ChoiceIndex));
            Assert.Throws<ArgumentException>(() => new CombatEnvironmentRandomEvent(draw.ExpectedState,
                draw.FinalState, draw.ChoiceCount, 1 - draw.ChoiceIndex));
        }

        [Test]
        public void SharedRouteUsesEarlierPlatformsFinalOccupancy()
        {
            var input = Input(new[] { Platform(100, 2, 0, 6, 1, axis: PlatformAxis.Horizontal),
                Platform(101, 1, 0, 6, 6, sign: -1) });
            var result = Run(input);
            Assert.That(result.FinalState.Environment.Tiles.OfType<MovingPlatformFact>().OrderBy(p => p.Id).Select(p => p.Cell.X),
                Is.EqualTo(new[] { 3, 5 }));
        }

        [Test]
        public void CrossedRoutesUseCurrentFullFootprint()
        {
            var horizontal = Platform(100, 2, 1, 4, 1, y: 2);
            var vertical = new MovingPlatformFact(new MovingPlatformSpec(101, 1, PlatformAxis.Horizontal,
                new CellCoord(3, 1), new CellCoord(3, 5), new CellCoord(3, 1), 1));
            var result = Run(Input(new[] { horizontal, vertical }));
            var second = result.Events.OfType<CombatPlatformMotionEvent>().Last();
            Assert.That(second.Steps, Is.Empty);
            Assert.That(second.StopReason, Is.EqualTo(PlatformMotionStopReason.Blocked));
        }

        [Test]
        public void FilledCliffAndNonPassengerDestinationCauseReversal()
        {
            foreach (bool filled in new[] { true, false })
            {
                var input = Input(Platform(100, 2, 0, 5, 2));
                input = filled ? Fill(input, new CellCoord(3, 1)) : Riders(input,
                    CombatCommandTestData.Character(2, EntityKey.Unit_Demon, new CellCoord(3, 1), Owner.Enemy));
                var result = Run(input);
                Assert.That(Motion(result).Final.Cell, Is.EqualTo(new CellCoord(0, 0)));
            }
        }

        [TestCase(2)]
        [TestCase(3)]
        public void PassengersMoveAtomicallyBeforeEffectsInStableIdOrder(int count)
        {
            var input = Input(Platform(100, count, 1, 5, 1));
            var riders = Enumerable.Range(1, count).Select(id => CombatCommandTestData.Character(id,
                EntityKey.Unit_Demon, new CellCoord(1, id - 1), Owner.Enemy, actionUse: UnitActionUse.Attacked)).ToArray();
            input = Riders(input, riders);
            var observations = new List<EntityId>();
            var probe = new ProbeRule((current, mover) =>
            {
                observations.Add(mover);
                Assert.That(current.Characters.Where(c => c.EntityId.Value <= count).Select(c => c.Anchor.Value.X),
                    Is.All.EqualTo(2));
                Assert.That(current.State.Environment.Tiles.OfType<MovingPlatformFact>().Single().Cell.X, Is.EqualTo(2));
            });
            input = Tiles(input, Enumerable.Range(0, count).Select(i => (MovementTileFact)new ProbeTile(200 + i, new CellCoord(2, i))).ToArray());
            var result = Run(input, new IMovementTileRule[] { probe });
            var final = CombatCommandComposition.Project(result);
            Assert.That(final.Characters.Where(c => c.EntityId.Value <= count).Select(c => c.ActionUse), Is.All.EqualTo(UnitActionUse.Attacked));
            Assert.That(observations.Distinct(), Is.EqualTo(riders.Select(c => c.EntityId)));
            Assert.That(Motion(result).Steps.Where(s => s.StepKind == PlatformStepKind.Translation).All(s => s.Passengers.Count == count), Is.True);
        }

        [Test]
        public void RuntimeBossOccupancyKeepsPlatformStationaryAndSupported()
        {
            var boss = CombatCommandTestData.With(CombatCommandTestData.Character(2, EntityKey.Unit_Demon, new CellCoord(1, 0), Owner.Enemy),
                category: CategorySnapshot.CreateStateless(CategoryKey.Boss));
            var input = Riders(Input(Platform(100, 2, 1, 5, 1)), boss);
            var blocked = Run(input);
            Assert.That(Motion(blocked).Steps, Is.Empty);
            Assert.That(Motion(blocked).StopReason, Is.EqualTo(PlatformMotionStopReason.NontransportableOccupant));
            Assert.That(Motion(blocked).Final.SameValues(Motion(blocked).Expected), Is.True);
            Assert.That(CombatCommandComposition.Project(blocked).Characters.Single(c => c.EntityId == boss.EntityId).Health.IsDead, Is.False);
            var moved = Run(Riders(Input(Platform(100, 2, 1, 5, 1), completed: 2), CombatCommandTestData.With(boss, position: new CellCoord(6, 6))));
            Assert.That(Motion(moved).Final.Cell.X, Is.EqualTo(3));
        }

        [Test]
        public void NetZeroJourneyRetainsBothTranslations()
        {
            var result = Run(Input(Platform(100, 2, 1, 2, 1)));
            Assert.That(Motion(result).Final.Cell, Is.EqualTo(Motion(result).Expected.Cell));
            Assert.That(Motion(result).Steps.Count(s => s.StepKind == PlatformStepKind.Translation), Is.EqualTo(2));
        }

        [Test]
        public void PreparedAndImmediateAdvanceMatchAndNeverRequestEnemyDecision()
        {
            var input = Input(ReboundPlatform());
            Assert.That(CombatCommandCalculator.RequiresEnemyDecision(input), Is.False);
            var immediate = Run(input);
            var prepared = CombatCommandCalculator.AdvancePrepared(input, null, NoRules);
            Assert.That(prepared.FinalState.SameValues(immediate.FinalState), Is.True);
            Assert.That(Motion(prepared).Steps.Select(s => s.Final.Cell), Is.EqualTo(Motion(immediate).Steps.Select(s => s.Final.Cell)));
            PlatformMotionEvidenceValidator.Validate(prepared);
        }

        [Test]
        public void TerminalPreludeKeepsAllPassengerPositionsAndNeverBeginsTurn()
        {
            var input = Input(Platform(100, 2, 1, 5, 1));
            var main = CombatCommandTestData.With(input.Characters.Single(c => c.EntityId == new EntityId(10)), position: new CellCoord(1, 0));
            input = Riders(input, main, CombatCommandTestData.Character(11, EntityKey.Unit_Demon, new CellCoord(1, 1), Owner.Enemy));
            input = CombatCommandTestData.With(input, state: EnvironmentDamageTests.WithDamageTiles(input.State, (new CellCoord(2, 0), 200)));
            var result = Run(input, new IMovementTileRule[] { new EnvironmentDamageTests.DamageRule() });
            Assert.That(result.Outcome.IsStageEnded, Is.True);
            Assert.That(Motion(result).Final.Cell.X, Is.EqualTo(2));
            Assert.That(CombatCommandComposition.Project(result).Characters.Single(c => c.EntityId == new EntityId(11)).Anchor, Is.EqualTo(new CellCoord(2, 1)));
            Assert.That(result.Events.Any(e => e.Kind == CombatEventKind.ControllerTurnStarted), Is.False);
        }

        [Test]
        public void PreludeFearDoesNotReceiveEndCleanupCompensation()
        {
            var input = Input(Platform(100, 1, 1, 4, 1), phase: CombatControllerPhase.StartingPlayer);
            var fear = CombatCommandTestData.With(CombatCommandTestData.Character(2, EntityKey.Unit_Gravekeeper, new CellCoord(1, 0), Owner.Enemy),
                hp: 1, traits: new TraitStatusSnapshot[] { new FearTraitStatusSnapshot() });
            input = Riders(input, fear, CombatCommandTestData.Character(3, EntityKey.Unit_Demon, new CellCoord(2, 1), Owner.Enemy));
            input = CombatCommandTestData.With(input, profiles: input.Profiles.Append(new CombatAttackProfile(
                CombatCommandTestData.Spec(EntityKey.Unit_Gravekeeper, 1, CategoryKey.Main, new FearTraitSpec()), input.Profiles[0].Method)).ToArray(),
                state: EnvironmentDamageTests.WithDamageTiles(input.State, (new CellCoord(2, 0), 2)));
            var result = Run(input, new IMovementTileRule[] { new EnvironmentDamageTests.DamageRule() });
            var affected = CombatCommandComposition.Project(result).Characters.Single(c => c.EntityId == new EntityId(3));
            Assert.That(affected.StatusEffects.Entries.OfType<ActionRestrictionStatus>().Select(s => s.RemainingAffectedSideTurnEnds), Is.EqualTo(new[] { 1, 1 }));
            Assert.That(ActionRestrictionTiming.Duration(1, Owner.Enemy, input.State.Turn), Is.EqualTo(2));
            var normal = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId == fear.EntityId
                ? CombatCommandTestData.With(c, position: new CellCoord(2, 0)) : c).ToArray());
            var ending = MovementProjection.ForArrival(normal, normal.Characters.ToDictionary(c => c.EntityId), new List<CombatEvent>());
            MovementEnvironmentCalculator.ResolveEntry(ending, new CellCoord(2, 0), new IMovementTileRule[] { new EnvironmentDamageTests.DamageRule() }, fear.EntityId);
            foreach (var _ in DeathTraitApplication.ResolveSteps(ending)) { }
            Assert.That(ending.Character(new EntityId(3)).StatusEffects.Entries.OfType<ActionRestrictionStatus>()
                .Select(s => s.RemainingAffectedSideTurnEnds), Is.EqualTo(new[] { 2, 2 }));
        }

        [Test]
        public void AttackOnlyDepartureModifierRetainsCharacterEvidence()
        {
            var input = Input(Platform(100, 1, 1, 4, 1));
            input = Tiles(input, new DestinationTileModifierFact(200, new CellCoord(1, 0), true,
                new[] { Owner.Player, Owner.Enemy }, 3, 0));
            var rider = CombatCommandTestData.Character(2, EntityKey.Unit_Demon, new CellCoord(1, 0), Owner.Enemy);
            rider = PositionTileModifierCalculator.Replace(rider, PositionTileModifierCalculator.ResolveAt(rider.Anchor.Value, input.State.Environment));
            var result = Run(Riders(input, rider));
            Assert.That(CombatCommandComposition.Project(result).Characters.Single(c => c.EntityId == rider.EntityId).Stats.PositionAttackBonus, Is.Zero);
        }

        [Test]
        public void TornadoDisplacementEndsTransportAndPreservesRandomEvidence()
        {
            var input = Riders(Input(Platform(100, 3, 1, 5, 1)),
                CombatCommandTestData.Character(1, EntityKey.Unit_Akimbo, new CellCoord(1, 0), Owner.Player));
            var storm = TornadoGimmickResolutionTests.Input(center: new CellCoord(2, 0), victim: new CellCoord(1, 0));
            input = new CombatCommandInput(0, input.Kind, input.ActorId, input.Aim, input.State.With(gimmicks: storm.State.Gimmicks),
                input.Characters, input.Profiles, input.Main, input.Owned, input.SoulCounts, input.Souls, input.Artifacts,
                battle: storm.Battle, processingKind: CombatProcessingKind.Advance, battleInstanceId: 1,
                environmentBindings: input.EnvironmentBindings);
            var result = Run(input, TornadoEnvironmentTests.Rules);
            var steps = Motion(result).Steps.Where(s => s.StepKind == PlatformStepKind.Translation).ToArray();
            Assert.That(result.Events.OfType<CombatCharacterEvent>().Any(e => e.Kind == CombatEventKind.Relocated), Is.True);
            Assert.That(steps[0].Passengers.Single().TransportEnded, Is.True);
            Assert.That(steps.Skip(1).SelectMany(s => s.Passengers), Is.Empty);
        }

        [Test]
        public void NoPlatformsStartsNormallyAndBoundaryCopiesAreAuthoritative()
        {
            var input = Input(Array.Empty<MovingPlatformFact>());
            var result = CombatCommandCalculator.Advance(input, NoRules);
            Assert.That(result.Events.Count(e => e.Kind == CombatEventKind.ControllerTurnStarted), Is.EqualTo(1));
            var state = Input(ReboundPlatform()).State;
            Assert.That(state.SameValues(state.With(lastPlatformBoundaryCount: 1)), Is.False);
            Assert.That(state.With(lastPlatformBoundaryCount: 1).With().LastPlatformBoundaryCount, Is.EqualTo(1));
        }

        private static CombatPlatformMotionEvent Motion(CombatCommandTransition result) => result.Events.OfType<CombatPlatformMotionEvent>().Single();

        private static CombatCommandTransition Run(CombatCommandInput input, IReadOnlyList<IMovementTileRule> rules = null)
        {
            var result = CombatCommandCalculator.Advance(input, rules ?? NoRules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            PlatformMotionEvidenceValidator.Validate(result);
            return result;
        }

        private static MovingPlatformFact Platform(long id, int length, int start, int end, int anchor,
            int y = 0, int sign = 1, PlatformAxis axis = PlatformAxis.Vertical) => new MovingPlatformFact(
                new MovingPlatformSpec(id, length, axis, new CellCoord(start, y), new CellCoord(end, y), new CellCoord(anchor, y), sign));

        private static CombatCommandInput Riders(CombatCommandInput input, params CharacterCombatFacts[] riders) =>
            CombatCommandTestData.With(input, characters: input.Characters.Where(c => c.OwnershipRole == CharacterOwnershipRole.Main &&
                riders.All(r => r.EntityId != c.EntityId)).Concat(riders).ToArray());

        private static CombatCommandInput Fill(CombatCommandInput input, params CellCoord[] cells)
        {
            var environment = new MovementEnvironmentFacts(input.State.Environment.Tiles.Select(t =>
                t is StaticTerrainFact terrain && cells.Contains(t.Cell) ? terrain.FillCliff() : t).ToArray());
            return CombatCommandTestData.With(input, state: input.State.With(environment: environment));
        }

        private static CombatCommandInput Tiles(CombatCommandInput input, params MovementTileFact[] tiles)
        {
            var environment = new MovementEnvironmentFacts(input.State.Environment.Tiles.Concat(tiles).ToArray());
            return CombatCommandTestData.With(input, state: input.State.With(environment: environment,
                environmentProgress: new EnvironmentProgressFacts(1, environment.Tiles.Max(t => t.Id) + 1, 42)));
        }

        private sealed class ProbeTile : MovementTileFact
        {
            internal ProbeTile(long id, CellCoord cell) : base(id, cell, "platform-probe", 0, true, new[] { Owner.Player, Owner.Enemy }) { }
            public override bool SameValues(MovementTileFact other) => SameSourceValues(other);
            public override MovementTileFact WithKnownTo(IReadOnlyList<Owner> knownTo) => new ProbeTile(Id, Cell);
        }

        private sealed class ProbeRule : IMovementTileRule
        {
            private readonly Action<CombatCommandInput, EntityId> _observe;
            internal ProbeRule(Action<CombatCommandInput, EntityId> observe) { _observe = observe; }
            public string Key => "platform-probe";
            public IReadOnlyList<EntityId> TargetIds(MovementTileFact tile, EntityId mover, CombatCommandInput current)
            {
                _observe(current, mover);
                return new[] { mover };
            }
            public MovementStepEffects Evaluate(MovementStepInput input, MovementTileFact tile) => new MovementStepEffects();
        }

        internal static MovingPlatformFact ReboundPlatform() => new MovingPlatformFact(new MovingPlatformSpec(100, 3,
            PlatformAxis.Vertical, new CellCoord(1, 0), new CellCoord(4, 0), new CellCoord(3, 0), 1));

        internal static CombatCommandInput Input(MovingPlatformFact platform, long completed = 1,
            CombatControllerPhase phase = CombatControllerPhase.StartingEnemy) =>
            Input(new[] { platform }, completed, phase);

        internal static CombatCommandInput Input(IReadOnlyList<MovingPlatformFact> platforms, long completed = 1,
            CombatControllerPhase phase = CombatControllerPhase.StartingEnemy)
        {
            var seed = CombatCommandTestData.Input();
            var cells = new HashSet<CellCoord>();
            foreach (var platform in platforms)
                for (int y = Math.Min(platform.Spec.RouteStart.Y, platform.Spec.RouteEnd.Y);
                    y <= Math.Max(platform.Spec.RouteStart.Y, platform.Spec.RouteEnd.Y); y++)
                    for (int x = Math.Min(platform.Spec.RouteStart.X, platform.Spec.RouteEnd.X);
                        x <= Math.Max(platform.Spec.RouteStart.X, platform.Spec.RouteEnd.X); x++)
                        cells.UnionWith(platform.Spec.FootprintAt(new CellCoord(x, y)));
            var tiles = cells.OrderBy(c => c.Y).ThenBy(c => c.X)
                .Select((cell, index) => (MovementTileFact)PlatformTestData.Terrain(index + 1, cell))
                .Concat(platforms).ToArray();
            var environment = new MovementEnvironmentFacts(tiles);
            var turn = new CombatTurnFacts(StagePhase.Combat, phase, 1, completed > 0 ? 1 : 0,
                completedControllerTurnCount: completed);
            var state = seed.State.With(phase: CombatFlowPhase.Automatic, turn: turn, environment: environment,
                environmentProgress: new EnvironmentProgressFacts(1, tiles.Select(t => t.Id).DefaultIfEmpty(0).Max() + 1, 42));
            return new CombatCommandInput(0, CombatCommandKind.StartControllerTurn, EntityId.Empty, default, state,
                seed.Characters.Select(c => c.With(actionUse: UnitActionUse.Attacked)).ToArray(),
                seed.Profiles, seed.Main, seed.Owned, seed.SoulCounts, seed.Souls, seed.Artifacts,
                processingKind: CombatProcessingKind.Advance, battleInstanceId: 1,
                environmentBindings: new EnvironmentSemanticBindings(tiles.Select(t => t.Id).ToArray(),
                    Array.Empty<long>(), Array.Empty<EntityId>()));
        }
    }
}
