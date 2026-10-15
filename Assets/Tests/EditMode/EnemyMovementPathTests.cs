using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Tests.Core
{
    public sealed class EnemyMovementPathTests
    {
        private static readonly IReadOnlyList<IMovementTileRule> Rules = new IMovementTileRule[] { new ThornMovementTileRule(), new MineRule() };

        [Test]
        public void TerrainRoute_PrefersLowerApOverFewerSteps()
        {
            var input = HighGroundMovementTests.TerrainInput(Input(actorCell: new CellCoord(0, 1),
                targetCell: new CellCoord(4, 1), range: 6), HighGroundMovementTests.High(11,
                new CellCoord(1, 1), owner: Owner.Enemy));
            Assert.That(Find(input, out var path), Is.True);
            Assert.That(path.StepCount, Is.EqualTo(4));
            Assert.That(path.Cells, Has.No.Member(new CellCoord(1, 1)));
            var moved = CombatCommandCalculator.Calculate(CombatCommandComposition.Intent(input,
                CombatCommandKind.Move, path: path), Rules);
            Assert.That(moved.Accepted, Is.True, moved.Reason);
            Assert.That(moved.PathMovement.ActionCostSpent, Is.EqualTo(4));
        }

        [TestCase(3, 2, 3)]
        [TestCase(6, 0, 4)]
        public void TerrainRoute_ChoosesLowestApAmongRoutesWithinBothBudgets(int range, int expectedAp, int expectedSteps)
        {
            var input = HighGroundMovementTests.TerrainInput(Input(cost: 0, range: range,
                actorCell: new CellCoord(0, 1), targetCell: new CellCoord(4, 1)),
                HighGroundMovementTests.High(11, new CellCoord(1, 1), owner: Owner.Enemy));
            Assert.That(Find(input, out var path), Is.True);
            Assert.That(path.StepCount, Is.EqualTo(expectedSteps));
            var moved = CombatCommandCalculator.Calculate(CombatCommandComposition.Intent(input,
                CombatCommandKind.Move, path: path), Rules);
            Assert.That(moved.Accepted, Is.True, moved.Reason);
            Assert.That(moved.PathMovement.ActionCostSpent, Is.EqualTo(expectedAp));
        }

        [Test]
        public void TerrainRoute_EqualApPrefersFewerSteps()
        {
            var input = HighGroundMovementTests.TerrainInput(Input(cost: 0,
                actorCell: new CellCoord(0, 1), targetCell: new CellCoord(4, 1)),
                Enumerable.Range(0, 25).Select(i => HighGroundMovementTests.High(11 + i,
                    new CellCoord(i % 5, i / 5), owner: Owner.Enemy)).ToArray());
            Assert.That(Find(input, out var path), Is.True);
            Assert.That(path.Cells, Is.EqualTo(new[] {
                new CellCoord(0, 1), new CellCoord(1, 1), new CellCoord(2, 1), new CellCoord(3, 1) }));
        }

        [TestCase(3, 1, true)]
        [TestCase(2, 1, false)]
        [TestCase(2, 0, true)]
        public void TerrainPrefix_UsesActualEdgeCost(int ap, int cost, bool canMove)
        {
            var input = Input(ap: ap, cost: cost, actorCell: new CellCoord(0, 0), targetCell: new CellCoord(4, 0));
            input = HighGroundMovementTests.TerrainInput(input,
                Enumerable.Range(0, 5).Select(x => HighGroundMovementTests.High(11 + x,
                    new CellCoord(x, 1), owner: Owner.Enemy))
                .Concat(new[] { HighGroundMovementTests.High(20, new CellCoord(1, 0), owner: Owner.Enemy) }).ToArray());
            Assert.That(Find(input, out var path), Is.EqualTo(canMove));
            if (!canMove) return;
            Assert.That(path.StepCount, Is.EqualTo(cost == 0 ? 3 : 1));
            var moved = CombatCommandCalculator.Calculate(CombatCommandComposition.Intent(input,
                CombatCommandKind.Move, path: path), Rules);
            Assert.That(moved.Accepted, Is.True, moved.Reason);
            Assert.That(moved.PathMovement.ActionCostSpent, Is.EqualTo(cost == 0 ? 2 : 3));
        }

        [Test]
        public void SafeDetour_CanInitiallyIncreaseManhattanDistance()
        {
            var input = Input(ap: 2, environment: Maze());
            var result = CombatCommandCalculator.Advance(input, Rules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.PathMovement, Is.Not.Null);
            Assert.That(result.PathMovement.RequestedPath.Cells, Is.EqualTo(new[] {
                new CellCoord(1, 1), new CellCoord(1, 2), new CellCoord(1, 3) }));
            Assert.That(result.PathMovement.EnteredCells, Has.No.Member(new CellCoord(2, 1)));
            Assert.That(result.Events.Count(e => e.Kind == CombatEventKind.TurnEnded), Is.EqualTo(1));
            Assert.That(input.Characters[0].Anchor, Is.EqualTo(new CellCoord(1, 1)));
        }

        [TestCase(1, 1, 1)]
        [TestCase(2, 1, 2)]
        [TestCase(0, 0, 2)]
        public void SafeGoalBeyondBudget_UsesPrefix(int ap, int cost, int expectedSteps)
        {
            var input = Input(ap: ap, cost: cost, range: 2, environment: Maze());
            Assert.That(Find(input, out var path), Is.True);
            Assert.That(path.StepCount, Is.EqualTo(expectedSteps));
            Assert.That(path.Endpoint, Is.EqualTo(new CellCoord(1, 1 + expectedSteps)));
            Assert.That(CombatCommandCalculator.Calculate(CombatCommandComposition.Intent(input,
                CombatCommandKind.Move, path: path), Rules).Accepted, Is.True);
        }

        [Test]
        public void NoSafeApproach_DoesNotMove()
        {
            var input = Input(environment: new MovementEnvironmentFacts(new MovementTileFact[] {
                Mine(1, new CellCoord(2, 1)), Mine(2, new CellCoord(1, 2)),
                Mine(3, new CellCoord(0, 1)), Mine(4, new CellCoord(1, 0)) }));
            Assert.That(Find(input, out _), Is.False);
            var result = CombatCommandCalculator.Advance(input, Rules);
            Assert.That(result.PathMovement, Is.Null);
            Assert.That(result.Attack.HasValue, Is.False);
            Assert.That(result.Events.Select(e => e.Kind), Has.No.Member(CombatEventKind.Moved));
            Assert.That(result.Events.Count(e => e.Kind == CombatEventKind.TurnEnded), Is.EqualTo(1));
        }

        [TestCase(false, true)]
        [TestCase(true, false)]
        public void UnknownOrInactiveMine_IsNotUsedByPlanner(bool known, bool active)
        {
            Assert.That(Find(Input(), out var clear), Is.True);
            var hidden = Input(environment: new MovementEnvironmentFacts(new MovementTileFact[] {
                Mine(1, new CellCoord(2, 1), known: known, active: active) }));
            Assert.That(Find(hidden, out var path), Is.True);
            Assert.That(path.Cells, Is.EqualTo(clear.Cells));
            Assert.That(path.Cells, Does.Contain(new CellCoord(2, 1)));
        }

        [TestCase(200, false, false, MovementStopReason.Dead)]
        [TestCase(0, true, false, MovementStopReason.ForcedRelocation)]
        [TestCase(0, false, true, MovementStopReason.MovementBlocked)]
        public void InterruptedMove_SuppressesFollowingAttack_AndClosesOnlyLivingTurns(
            int damage, bool force, bool block, MovementStopReason reason)
        {
            var input = Input(environment: new MovementEnvironmentFacts(new MovementTileFact[] {
                Mine(1, new CellCoord(2, 1), known: false, damage: damage, force: force, block: block) }));
            var result = CombatCommandCalculator.Advance(input, Rules);
            Assert.That(result.PathMovement, Is.Not.Null);
            Assert.That(result.PathMovement.StopReason, Is.EqualTo(reason));
            Assert.That(result.Attack.HasValue, Is.False);
            Assert.That(result.Events.Count(e => e.Kind == CombatEventKind.Attacked), Is.Zero);
            Assert.That(result.Events.Count(e => e.Kind == CombatEventKind.TurnEnded), Is.EqualTo(damage == 200 ? 0 : 1));
            Assert.That(result.FinalState.Environment.Tiles.Single().Active, Is.False);
        }

        [Test]
        public void EqualGoals_UseYThenX_AndRoutesUseFixedNeighborOrder()
        {
            var input = Input(actorCell: new CellCoord(0, 1), targetCell: new CellCoord(3, 1),
                environment: new MovementEnvironmentFacts(new MovementTileFact[] { Mine(1, new CellCoord(2, 1)) }));
            Assert.That(Find(input, out var path), Is.True);
            Assert.That(path.Cells, Is.EqualTo(new[] { new CellCoord(0, 1), new CellCoord(1, 1), new CellCoord(1, 0), new CellCoord(2, 0) }));
        }

        [TestCase(0)]
        [TestCase(-10)]
        public void PierceBudgetExhaustedByAlly_SearchesForAnActualAttackPosition(int originAttackBonus)
        {
            var input = Input(actorCell: new CellCoord(0, 2), targetCell: new CellCoord(4, 2));
            var actor = input.Characters[0];
            var s = actor.Stats;
            actor = CharacterSettlementCalculator.Copy(actor, actor.Health, new UnitStatsSnapshot(
                s.MoveMethod, s.MoveRange, s.MoveCost, 4, s.AttackPower, s.PhysicalDefense,
                s.MaxActionCost, s.AttackCost, s.RemainingMoveRange, s.CurrentActionCost, s.Priority, s.Delay,
                new TraitStatusSnapshot[] { new PiercingFalloffTraitStatusSnapshot(100) },
                CategorySnapshot.CreateStateless(CategoryKey.Cannon)));
            var ally = CombatCommandTestData.Character(3, EntityKey.Unit_Akimbo, new CellCoord(2, 2), Owner.Enemy);
            var main = input.Characters[2];
            input = CombatCommandTestData.With(input, characters: new[] { actor, input.Characters[1], main, ally },
                profiles: input.Profiles.Select(p => p.Definition.Key == actor.CharacterKey
                    ? new CombatAttackProfile(CombatCommandTestData.Spec(actor.CharacterKey, 10, CategoryKey.Cannon,
                        new PiercingFalloffTraitSpec(100, 99)), new TargetingMethodSpec(TargetingShape.EightDirectionLine,
                            TargetingChoiceMode.Direction, TargetingSplash.None, 0)) : p).ToArray());
            if (originAttackBonus != 0)
            {
                var environment = new MovementEnvironmentFacts(new MovementTileFact[] {
                    new DestinationTileModifierFact(7, actor.Anchor.Value, true, new[] { Owner.Enemy }, originAttackBonus, 0) });
                actor = PositionTileModifierCalculator.Replace(actor,
                    PositionTileModifierCalculator.ResolveAt(actor.Anchor.Value, environment));
                var state = input.State;
                input = CombatCommandTestData.With(input,
                    characters: input.Characters.Select(c => c.EntityId == actor.EntityId ? actor : c).ToArray(),
                    state: new CombatCommandState(state.BoardWidth, state.BoardHeight, state.Phase, state.ActiveActorId,
                        state.IsNight, state.Structures, state.ThornFields, state.Timeline, state.Turn, environment));
            }
            Assert.That(Find(input, out var path), Is.True);
            Assert.That(path.StepCount, Is.GreaterThan(0));
            var result = CombatCommandCalculator.Advance(input, Rules);
            Assert.That(result.PathMovement, Is.Not.Null);
            Assert.That(result.Attack.HasValue, Is.True);
            Assert.That(result.Attack.Value.Hits.ToArray().Any(h => h.TargetId == new EntityId(2) && h.DamageDealt > 0), Is.True);
        }

        [TestCase(1, 30, true)]
        [TestCase(99, 100, true)]
        [TestCase(2, 30, false)]
        public void Pursuit_FinitePierceBudgetAgreesWithPreview(int maxTargetsPerRay, int falloff, bool needsMove)
        {
            var input = Input(actorCell: new CellCoord(0, 2), targetCell: new CellCoord(4, 2));
            var actor = CombatCommandTestData.With(input.Characters[0], appointRange: 4,
                category: CategorySnapshot.CreateStateless(CategoryKey.Cannon),
                traits: new TraitStatusSnapshot[] { new PiercingFalloffTraitStatusSnapshot(falloff) });
            var ally = CombatCommandTestData.Character(3, EntityKey.Unit_Akimbo, new CellCoord(2, 2), Owner.Enemy);
            var definition = UnitSpecTestFactory.Create(actor.CharacterKey, 1, 1, new[] { CellCoord.Zero },
                100, MoveMethod.LiftAndPlace, 6, 1, 4, 10, 5, 2, 100, 10,
                new TraitSpec[] { new PiercingFalloffTraitSpec(falloff, maxTargetsPerRay) }, categoryKey: CategoryKey.Cannon);
            var method = new TargetingMethodSpec(TargetingShape.EightDirectionLine,
                TargetingChoiceMode.Direction, TargetingSplash.None, 0);
            input = CombatCommandTestData.With(input, characters: new[] { actor, input.Characters[1], input.Characters[2], ally },
                profiles: input.Profiles.Select(p => p.Definition.Key == actor.CharacterKey
                    ? new CombatAttackProfile(definition, method) : p).ToArray());

            var aim = AttackAim.ForDirection(AttackDirection.Right);
            var preview = CombatCommandCalculator.Preview(CombatAttackQueries.WithAim(input, aim), Rules);
            Assert.That(preview.CanAttack, Is.True);
            Assert.That(preview.Targets.ToArray().Select(t => t.TargetId), Is.EquivalentTo(needsMove
                ? new[] { new EntityId(3) } : new[] { new EntityId(3), new EntityId(2) }));
            Assert.That(Find(input, out var path), Is.EqualTo(needsMove));
            if (!needsMove)
            {
                Assert.That(preview.Targets.ToArray().Single(t => t.TargetId == new EntityId(2)).TargetHealthAfter, Is.EqualTo(93));
                return;
            }

            // 한 칸 전진하면 아군이 근접 제외 영역에 들어가므로 유한 예산을 적에게 쓸 수 있다.
            Assert.That(path.Cells, Is.EqualTo(new[] { new CellCoord(0, 2), new CellCoord(1, 2) }));
            var move = CombatCommandCalculator.Calculate(CombatCommandComposition.Intent(input, CombatCommandKind.Move, path: path), Rules);
            Assert.That(move.Accepted, Is.True, move.Reason);
            var attack = CombatCommandComposition.Intent(CombatCommandComposition.Project(move), CombatCommandKind.Attack, aim);
            var arrivalPreview = CombatCommandCalculator.Preview(attack, Rules);
            Assert.That(arrivalPreview.CanAttack, Is.True);
            Assert.That(arrivalPreview.Targets.ToArray().Select(t => t.TargetId), Is.EqualTo(new[] { new EntityId(2) }));
            Assert.That(arrivalPreview.Targets.ToArray().Single().TargetHealthAfter, Is.EqualTo(90));
            var result = CombatCommandCalculator.Calculate(attack, Rules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.Attack.Value.Hits.ToArray().Select(h => h.TargetId), Is.EqualTo(new[] { new EntityId(2) }));
            Assert.That(result.Attack.Value.Hits.ToArray().Single().TargetHealthAfter, Is.EqualTo(90));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void PierceGoal_UsesOnlyKnownDestinationAttackBonus(bool known)
        {
            var input = Input(actorCell: new CellCoord(0, 1), targetCell: new CellCoord(4, 1),
                environment: new MovementEnvironmentFacts(new MovementTileFact[] {
                    Mine(1, new CellCoord(2, 1)), Mine(2, new CellCoord(0, 0)), Mine(3, new CellCoord(1, 0)),
                    Mine(4, new CellCoord(0, 2)), Mine(5, new CellCoord(1, 2)),
                    new DestinationTileModifierFact(7, new CellCoord(1, 1), true,
                        known ? new[] { Owner.Enemy } : new[] { Owner.Player }, 9, 0) }));
            var actor = input.Characters[0];
            var s = actor.Stats;
            actor = CharacterSettlementCalculator.Copy(actor, actor.Health, new UnitStatsSnapshot(
                s.MoveMethod, s.MoveRange, s.MoveCost, 4, 1, s.PhysicalDefense,
                s.MaxActionCost, s.AttackCost, s.RemainingMoveRange, s.CurrentActionCost, s.Priority, s.Delay,
                new TraitStatusSnapshot[] { new PiercingFalloffTraitStatusSnapshot(90) }, CategorySnapshot.CreateStateless(CategoryKey.Cannon)));
            var ally = CombatCommandTestData.Character(3, EntityKey.Unit_Akimbo, new CellCoord(3, 1), Owner.Enemy);
            input = CombatCommandTestData.With(input, characters: new[] { actor, input.Characters[1], input.Characters[2], ally },
                profiles: input.Profiles.Select(p => p.Definition.Key == actor.CharacterKey
                    ? new CombatAttackProfile(CombatCommandTestData.Spec(actor.CharacterKey, 1, CategoryKey.Cannon,
                        new PiercingFalloffTraitSpec(90, 99)), new TargetingMethodSpec(TargetingShape.EightDirectionLine,
                            TargetingChoiceMode.Direction, TargetingSplash.None, 0)) : p).ToArray());
            Assert.That(Find(input, out var path), Is.EqualTo(known));
            if (!known) return;
            Assert.That(path.Endpoint, Is.EqualTo(new CellCoord(1, 1)));
            var move = CombatCommandCalculator.Calculate(CombatCommandComposition.Intent(input, CombatCommandKind.Move, path: path), Rules);
            var attack = CombatCommandCalculator.Calculate(CombatCommandComposition.Intent(CombatCommandComposition.Project(move),
                CombatCommandKind.Attack, AttackAim.ForDirection(AttackDirection.Right)), Rules);
            Assert.That(attack.Attack.Value.Hits.ToArray().Any(h => h.TargetId == new EntityId(2) && h.DamageDealt > 0), Is.True);
        }

        private static bool Find(CombatCommandInput input, out MovementPath path)
        {
            return EnemyMovementPathfinder.TryFind(input, new EntityId(2), out path);
        }

        [Test]
        public void ZeroSplashContribution_DoesNotHideADamagingApproach()
        {
            var input = Input();
            var profiles = input.Profiles.Select(p => p.Definition.Key == EntityKey.Unit_Demon ?
                new CombatAttackProfile(p.Definition, new TargetingMethodSpec(TargetingShape.RadialSquare,
                    TargetingChoiceMode.Single, TargetingSplash.Radial, 0)) : p).ToArray();
            input = CombatCommandTestData.With(input, profiles: profiles);
            Assert.That(Find(input, out var path), Is.True);
            Assert.That(path.Endpoint, Is.EqualTo(new CellCoord(2, 1)));
            var result = CombatCommandCalculator.Advance(input, Rules);
            Assert.That(result.Attack.HasValue, Is.True);
            Assert.That(result.Attack.Value.Hits.ToArray().Single(h => h.TargetId == new EntityId(2)).DamageDealt, Is.EqualTo(10));
        }

        internal static CombatCommandInput Input(int ap = 5, int cost = 1, int range = 6,
            MovementEnvironmentFacts environment = null, CellCoord? actorCell = null, CellCoord? targetCell = null)
        {
            var source = CombatCommandTestData.Input(empty: true);
            var enemy = CombatCommandTestData.Character(1, EntityKey.Unit_Demon, actorCell ?? new CellCoord(1, 1), Owner.Enemy, 10);
            var s = enemy.Stats;
            enemy = new CharacterCombatFacts(enemy.EntityId, null, enemy.CharacterKey, enemy.Owner, null, enemy.Area,
                enemy.Health, new UnitStatsSnapshot(s.MoveMethod, range, cost, 1,
                    s.AttackPower, s.PhysicalDefense, 5, s.AttackCost, range, ap, s.Priority, s.Delay,
                    s.TraitStatuses.ToArray(), s.Category), null, enemy.Anchor, enemy.Rotation, enemy.OccupiedCells,
                enemy.Anchor.Value, 0, 0);
            var main = CombatCommandTestData.With(source.Characters[1], position: new CellCoord(4, 4));
            var target = CombatCommandTestData.Character(2, EntityKey.Unit_Akimbo, targetCell ?? new CellCoord(3, 1), Owner.Player);
            var characters = new[] { enemy, target, main };
            var state = new CombatCommandState(5, 5, CombatFlowPhase.Automatic, enemy.EntityId, false,
                Array.Empty<StructureCombatFacts>(), Array.Empty<ThornFieldCellSnapshot>(),
                new[] { CombatTimelineEntrySnapshot.ForUnit(enemy.EntityId, 100, 0, true) },
                new CombatTurnFacts(StagePhase.Combat, participants: new[] { new CombatTurnParticipant(enemy.EntityId, 100, 10) }), environment);
            return new CombatCommandInput(0, CombatCommandKind.Attack, enemy.EntityId, default, state,
                characters, source.Profiles, source.Main,
                characters.Where(c => c.OwnedId.HasValue).Select(c => new OwnedCharacterRecord(c.OwnedId.Value, c.CharacterKey, c.OwnershipRole.Value)).ToArray(),
                characters.Where(c => c.OwnedId.HasValue).ToDictionary(c => c.OwnedId.Value, _ => 0),
                source.Souls, source.Artifacts, battle: new CombatBattleFacts(10, 100, 1, 0, 0,
                    Array.Empty<CharacterInstanceId>(), 0, 20, 10, 2), processingKind: CombatProcessingKind.Advance);
        }

        private static MovementEnvironmentFacts Maze() => new(new MovementTileFact[] {
            Mine(1, new CellCoord(2, 0)), Mine(2, new CellCoord(2, 1)), Mine(3, new CellCoord(2, 2)) });
        private static MineFact Mine(long id, CellCoord cell, bool known = true, int damage = 0, bool force = false, bool block = false, bool active = true) =>
            new(id, cell, active, known ? new[] { Owner.Enemy } : new[] { Owner.Player }, damage, force, block);

        private sealed class MineFact : MovementTileFact
        {
            internal int Damage { get; }
            internal bool Force { get; }
            internal bool Block { get; }
            internal MineFact(long id, CellCoord cell, bool active, IReadOnlyList<Owner> known, int damage, bool force, bool block)
                : base(id, cell, "fixture-enemy-mine", 0, active, known, isMine: true)
            { Damage = damage; Force = force; Block = block; }
            public override bool SameValues(MovementTileFact other) => SameSourceValues(other) &&
                other is MineFact f && Damage == f.Damage && Force == f.Force && Block == f.Block;
            public override MovementTileFact WithKnownTo(IReadOnlyList<Owner> knownTo) => new MineFact(Id, Cell, Active, knownTo, Damage, Force, Block);
        }

        private sealed class MineRule : IMovementTileRule
        {
            public string Key => "fixture-enemy-mine";
            public IReadOnlyList<EntityId> TargetIds(MovementTileFact tile, EntityId mover, CombatCommandInput current) => new[] { mover };
            public MovementStepEffects Evaluate(MovementStepInput input, MovementTileFact tile)
            {
                if (input.Phase != MovementEffectPhase.Hazard) return new MovementStepEffects();
                var mine = (MineFact)tile;
                return new MovementStepEffects(damage: new[] { new MovementHealthRequest(input.MoverId, mine.Damage) },
                    forcedMovement: mine.Force ? new[] { new MovementForcedMoveRequest(input.MoverId, Rotation4.R0, 1) } : null,
                    movementMethods: mine.Block ? new[] { new MovementMethodChange(input.MoverId, MoveMethod.LiftAndPlace, MoveMethod.None) } : null,
                    expectedSource: mine, finalSource: new MineFact(mine.Id, mine.Cell, false, mine.KnownTo, mine.Damage, mine.Force, mine.Block));
            }
        }
    }
}
