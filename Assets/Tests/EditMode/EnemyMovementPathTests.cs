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
        public void InstalledAllowanceFive_MovesFiveEdges()
        {
            var input = MovementTestData.WithMovement(Input(category: CategoryKey.Rider,                environment: Maze()),5);
            Assert.That(Find(input, out var path), Is.True);
            Assert.That(path.StepCount, Is.EqualTo(5));
            var result = CombatCommandCalculator.Advance(input, Rules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.PathMovement.DistanceSpent, Is.EqualTo(5));
            Assert.That(MovementTestData.Actor(result).ActionUse, Is.EqualTo(UnitActionUse.Moved));
            Assert.That(result.Attack.HasValue, Is.False);
        }

        [Test]
        public void EnemyMovementUsesInstalledAllowance()
        {
            var input = Input(actorCell: CellCoord.Zero, targetCell: new CellCoord(4, 0));
            Assert.That(Find(input, out var path), Is.True);
            Assert.That(path.StepCount, Is.EqualTo(3));
            var result = CombatCommandCalculator.Advance(input, Rules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.PathMovement, Is.Not.Null);
            Assert.That(result.Attack.HasValue, Is.False);
            Assert.That(MovementTestData.Actor(result).ActionUse, Is.EqualTo(UnitActionUse.Moved));
        }

        [TestCase(CategoryKey.Main, 3)]
        [TestCase(CategoryKey.Rider, 4)]
        public void SafeGoalBeyondAllowance_UsesCategoryPrefix(CategoryKey category, int steps)
        {
            var input = Input(category: category, environment: Maze());
            Assert.That(Find(input, out var path), Is.True);
            Assert.That(path.StepCount, Is.EqualTo(steps));
            Assert.That(path.Cells[1], Is.EqualTo(new CellCoord(1, 2)));
            var result = CombatCommandCalculator.Advance(input, Rules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.PathMovement.DistanceSpent, Is.EqualTo(steps));
            Assert.That(result.Attack.HasValue, Is.False);
            Assert.That(result.Events.Any(e => e.Kind == CombatEventKind.EnemyPhaseEnded), Is.False);
            Assert.That(input.Characters[0].Anchor, Is.EqualTo(new CellCoord(1, 1)));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void HighGroundRemainsTraversableForEnemy(bool visible)
        {
            var input = HighGroundMovementTests.TerrainInput(Input(actorCell: CellCoord.Zero,                targetCell: new CellCoord(4, 0)), HighGroundMovementTests.High(11, CellCoord.Right,
                owner: visible ? Owner.Enemy : Owner.Player));
            Assert.That(Find(input, out var path), Is.True);
            Assert.That(path.Cells, Has.Member(CellCoord.Right));
            var result = CombatCommandCalculator.Advance(input, Rules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.PathMovement, Is.Not.Null);
            Assert.That(result.Attack.HasValue, Is.False);
        }

        [Test]
        public void ZeroDamageCurrentTarget_IsAttackedWithoutPursuit()
        {
            var input = Input(actorCell: CellCoord.Zero, targetCell: CellCoord.Right);
            var actor = input.Characters[0];
            actor = actor.With(stats: actor.Stats.With(attackPower: 0, effectiveAttackPower: 0));
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId == actor.EntityId ? actor : c).ToArray());
            var result = CombatCommandCalculator.Advance(input, Rules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.Attack.HasValue, Is.True);
            Assert.That(result.PathMovement, Is.Null);
            Assert.That(result.Attack.Value.Hits.ToArray().Single().DamageDealt, Is.Zero);
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
            Assert.That(result.Events.Count(e => e.Kind == CombatEventKind.EnemyPhaseEnded), Is.EqualTo(1));
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
        public void InterruptedMove_ConsumesOneAction_AndKeepsEnemyController(
            int damage, bool force, bool block, MovementStopReason reason)
        {
            var input = Input(environment: new MovementEnvironmentFacts(new MovementTileFact[] {
                Mine(1, new CellCoord(2, 1), known: false, damage: damage, force: force, block: block) }));
            var result = CombatCommandCalculator.Advance(input, Rules);
            Assert.That(result.PathMovement, Is.Not.Null);
            Assert.That(result.PathMovement.StopReason, Is.EqualTo(reason));
            Assert.That(result.Attack.HasValue, Is.False);
            Assert.That(result.Events.Count(e => e.Kind == CombatEventKind.Attacked), Is.Zero);
            Assert.That(result.Events.Count(e => e.Kind == CombatEventKind.EnemyPhaseEnded), Is.Zero);
            Assert.That(result.FinalState.Environment.Tiles.Single().Active, Is.False);
        }

        [Test]
        public void HiddenSandstormRejectingPlannedAttack_EndsTurnWithoutAttacking()
        {
            // 적이 모르는 모래폭풍이 실제 사거리를 3에서 2로 줄여, 공개 환경으로 고른 거리 3 조준이 거절된다.
            var input = Input(targetCell: new CellCoord(4, 1));
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId == input.ActorId
                ? CombatCommandTestData.With(c, appointRange: 3) : c).ToArray());
            input = HighGroundMovementTests.TerrainInput(input, new StaticTerrainFact(11, new CellCoord(1, 1),
                StaticTerrainKind.Sandstorm, true, new[] { Owner.Player }));
            var range = CombatCommandCalculator.GetAttackRange(input);
            var policy = new EnemyAiPolicy(
                new ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat.HostilityPolicy(), Rules);
            Assert.That(policy.TrySelectAttackAim(input, range, out var aim), Is.True);
            Assert.That(CombatCommandCalculator.Calculate(CombatAttackQueries.WithAim(input, aim), Rules).Accepted, Is.False);

            var result = CombatCommandCalculator.Advance(input, Rules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.Attack.HasValue, Is.False);
            Assert.That(result.Events.Count(e => e.Kind == CombatEventKind.Attacked), Is.Zero);
            Assert.That(result.Events.Count(e => e.Kind == CombatEventKind.EnemyPhaseEnded), Is.EqualTo(1));
        }

        [Test]
        public void EqualGoals_UseYThenX_AndRoutesUseFixedNeighborOrder()
        {
            var input = Input(actorCell: new CellCoord(0, 1), targetCell: new CellCoord(3, 1),
                environment: new MovementEnvironmentFacts(new MovementTileFact[] { Mine(1, new CellCoord(2, 1)) }));
            Assert.That(Find(input, out var path), Is.True);
            Assert.That(path.Cells, Is.EqualTo(new[] { new CellCoord(0, 1), new CellCoord(1, 1), new CellCoord(1, 0), new CellCoord(2, 0) }));
        }

        private static bool Find(CombatCommandInput input, out MovementPath path) =>
            EnemyMovementPathfinder.TryFind(input, new EntityId(2), out path);

        internal static CombatCommandInput Input(CategoryKey category = CategoryKey.Main,            MovementEnvironmentFacts environment = null,CellCoord? actorCell = null,CellCoord? targetCell = null)
        {
            var source = CombatCommandTestData.Input(empty: true);
            var enemy = CombatCommandTestData.With(CombatCommandTestData.Character(1, EntityKey.Unit_Demon,
                actorCell ?? new CellCoord(1, 1), Owner.Enemy, 10), appointRange: 1,
                category: CategorySnapshot.CreateStateless(category));
            var main = CombatCommandTestData.With(source.Characters[1], position: new CellCoord(4, 4));
            var target = CombatCommandTestData.Character(2, EntityKey.Unit_Akimbo, targetCell ?? new CellCoord(3, 1), Owner.Player);
            var characters = new[] { enemy, target, main };
            var state = new CombatCommandState(5,5,CombatFlowPhase.Automatic,true,                Array.Empty<StructureCombatFacts>(),Array.Empty<ThornFieldCellSnapshot>(),                new CombatTurnFacts(StagePhase.Combat, controllerPhase: CombatControllerPhase.Enemy,
                    controllerTurnSequence: 2, completedPlayerTurnCount: 1),environment);
            var profiles = source.Profiles.Select(p => p.Definition.Key == enemy.CharacterKey
                ? new CombatAttackProfile(CombatCommandTestData.Spec(enemy.CharacterKey, 10, category), p.Method) : p).ToArray();
            return new CombatCommandInput(0, CombatCommandKind.Attack, enemy.EntityId, default, state,
                characters, profiles, source.Main,
                characters.Where(c => c.OwnedId.HasValue).Select(c => new OwnedCharacterRecord(c.OwnedId.Value, c.CharacterKey, c.OwnershipRole.Value)).ToArray(),
                characters.Where(c => c.OwnedId.HasValue).ToDictionary(c => c.OwnedId.Value, _ => 0),
                source.Souls, source.Artifacts, battle: new CombatBattleFacts(10, 1, 0, 0,
                    Array.Empty<CharacterInstanceId>(), 0), processingKind: CombatProcessingKind.Advance);
        }

        internal static MovementEnvironmentFacts Maze() => new(new MovementTileFact[] {
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
