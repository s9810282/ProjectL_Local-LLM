using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Tests.Core
{
    public sealed class EnemyEngagementProgressTests
    {
        private static readonly IMovementTileRule[] Rules = EnemyAiBenchmarkFixtures.Rules;

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(31)]
        public void SafeRailPrefixReceivesProgressAndBeatsHold(int partition)
        {
            var input = World(railX: 4);
            string before = EnemyActionAssessmentTests.Encode(input);
            using var candidates = Candidates(input);
            Drain(candidates, partition);
            var actor = input.Characters.Single(unit => unit.EntityId == input.ActorId);
            var one = EnemyDecision.Move(actor.EntityId, new MovementPath(new[] { actor.Anchor.Value, new CellCoord(2, 2) }), default);
            var two = EnemyDecision.Move(actor.EntityId, new MovementPath(new[] { actor.Anchor.Value, new CellCoord(2, 2), new CellCoord(3, 2) }), default);
            using var decision = new EnemyDecisionSession(input, Rules, EnemyDecisionSettings.Default);
            Drain(decision, partition);
            Assert.That(decision.Decision.Kind, Is.EqualTo(EnemyDecisionKind.Move));
            Assert.That(decision.Decision.Path.Endpoint.X, Is.LessThan(4));
            Assert.That(candidates.Progress(one), Is.EqualTo(1));
            Assert.That(candidates.Progress(two), Is.EqualTo(2));
            Assert.That(candidates.Moves.Any(move => move.Path.Endpoint == two.Path.Endpoint), Is.True);
            var forecast = EnemyActionCandidateTests.MakeForecast(input);
            Assert.That(forecast.Evaluate(actor, Array.Empty<CellCoord>(), new CellCoord(4, 2)).KnownHpLoss, Is.EqualTo(100));
            var actual = CombatCommandCalculator.AdvancePrepared(input, decision.Decision, Rules);
            Assert.That(actual.Accepted, Is.True, actual.Reason);
            Assert.That(actual.PathMovement, Is.Not.Null);
            Assert.That(EnemyActionAssessmentTests.Encode(input), Is.EqualTo(before));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void FixedIntentExecutionIsUnchanged(bool rail)
        {
            var input = World(railX: rail ? 4 : -1);
            string before = EnemyActionAssessmentTests.Encode(input);
            var move = EnemyDecision.Move(input.ActorId, new MovementPath(new[] { new CellCoord(1, 2), new CellCoord(2, 2), new CellCoord(3, 2) }), default);
            var actual = CombatCommandCalculator.AdvancePrepared(input, move, Rules);
            Assert.That(actual.Accepted, Is.True, actual.Reason);
            CombatCommandInput projected = null;
            foreach (var _ in CombatCommandComposition.ProjectSteps(actual, value => projected = value)) { }
            using var sha = SHA256.Create();
            var snapshot = EnemyActionAssessmentTests.Encode(new object[] { projected, actual.Events, actual.Outcome });
            TestContext.WriteLine("fixed-intent=" + BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(snapshot))));
            Assert.That(EnemyActionAssessmentTests.Encode(input), Is.EqualTo(before));
        }

        [Test]
        public void WallDetourReducesRouteDistance()
        {
            var input = World(width: 7, height: 7, actorCell: new CellCoord(2, 2), targetCell: new CellCoord(5, 2));
            input = CliffFallTests.WithEnvironment(input, Enumerable.Range(0, 5)
                .Select(y => (MovementTileFact)WallStateLifecycleTests.Wall(100 + y, new CellCoord(3, y))).ToArray());
            var map = Map(input);
            var next = new CellCoord(2, 3);
            Assert.That(map.Progress(next), Is.EqualTo(1));
            Assert.That(CombatMovementCalculator.Distance(next, new CellCoord(5, 2)), Is.GreaterThan(3));
            using var decision = new EnemyDecisionSession(input, Rules, EnemyDecisionSettings.Default);
            Drain(decision);
            Assert.That(decision.Decision.Kind, Is.EqualTo(EnemyDecisionKind.Move));
            Assert.That(map.Progress(decision.Decision.Path.Endpoint), Is.Positive);
            var actual = CombatCommandCalculator.AdvancePrepared(input, decision.Decision, Rules);
            Assert.That(actual.Accepted, Is.True, actual.Reason);
        }

        [Test]
        public void EquivalentRoutesShareOnePotential()
        {
            var input = World(width: 9, height: 7, actorCell: new CellCoord(2, 2), targetCell: new CellCoord(7, 5));
            var map = Map(input);
            Assert.That(map.Progress(new CellCoord(3, 2)), Is.EqualTo(1));
            Assert.That(map.Progress(new CellCoord(2, 3)), Is.EqualTo(1));
            Assert.That(map.Progress(new CellCoord(3, 1)), Is.Zero);
            Assert.That(map.Progress(new CellCoord(1, 2)), Is.EqualTo(-1));
            var cycle = new[] { new CellCoord(2, 2), new CellCoord(3, 2), new CellCoord(3, 3), new CellCoord(2, 3), new CellCoord(2, 2) };
            int change = 0;
            for (int i = 1; i < cycle.Length; i++)
            {
                Assert.That(map.TryGetCost(cycle[i - 1], out int from), Is.True);
                Assert.That(map.TryGetCost(cycle[i], out int to), Is.True);
                change += from - to;
            }
            Assert.That(change, Is.Zero);
        }

        // 공격 모양별 목표 칸 대조는 EnemyEngagementFieldTests.GoalsMatchAttackReach로 옮겼다(R-2, 끌어오기·관통 사례 추가).

        [TestCase(false)]
        [TestCase(true)]
        public void OcclusionAndDestinationModifiersUseExistingReach(bool modifier)
        {
            var input = World(width: 7, height: 7, actorCell: new CellCoord(1, 2), targetCell: new CellCoord(5, 2), range: 2);
            input = modifier
                ? HighGroundMovementTests.TerrainInput(input, HighGroundMovementTests.High(71, new CellCoord(2, 2), owner: Owner.Enemy))
                : CliffFallTests.WithEnvironment(input, Enumerable.Range(1, 3)
                    .Select(y => (MovementTileFact)WallStateLifecycleTests.Wall(100 + y, new CellCoord(3, y))).ToArray());
            AssertGoalsMatchReference(input, new EntityId(1), 7, 7);
        }

        [Test]
        public void AttackCandidateDoesNotSuppressProgressMap()
        {
            var input = World(actorCell: new CellCoord(6, 2));
            using var candidates = Candidates(input);
            Drain(candidates);
            Assert.That(candidates.Attacks, Is.Not.Empty);
            Assert.That(candidates.EngagementExpandedNodes, Is.Positive);
            Assert.That(candidates.Attacks.Count, Is.LessThanOrEqualTo(4));
            var map = Map(input);
            foreach (var move in candidates.Moves)
                Assert.That(candidates.Progress(move), Is.EqualTo(map.Progress(move.Path.Endpoint)));
        }

        [Test]
        public void AlreadyAttackCapableAnchorsHaveNoApproachReward()
        {
            var map = Map(World(actorCell: new CellCoord(6, 2)));
            Assert.That(map.Progress(new CellCoord(6, 1)), Is.Zero);
            Assert.That(map.Progress(new CellCoord(5, 2)), Is.EqualTo(-1));
        }

        [Test]
        public void TauntOverrideChangesTheSharedMapTarget()
        {
            var source = World();
            var nearby = CombatCommandTestData.Character(2, EntityKey.Unit_Cowboy, new CellCoord(1, 4), Owner.Player, 0);
            source = CombatCommandTestData.With(source, characters: source.Characters.Append(nearby).ToArray(),
                profiles: source.Profiles.Append(Profile(nearby.CharacterKey, 0, 1)).ToArray());
            source = MineEnvironmentTests.Copy(source);
            var east = EnemyDecision.Move(source.ActorId, new MovementPath(new[] { new CellCoord(1, 2), new CellCoord(2, 2) }), default);
            var north = EnemyDecision.Move(source.ActorId, new MovementPath(new[] { new CellCoord(1, 2), new CellCoord(1, 3) }), default);
            using var nearest = Candidates(source);
            Drain(nearest);
            Assert.That(nearest.Progress(east), Is.Zero);
            Assert.That(nearest.Progress(north), Is.EqualTo(1));
            var taunted = TraitCombatFixtures.Equip(source, EntityKey.Unit_MainCharacter, new TauntTraitSpec(10, 10));
            using var mandatory = Candidates(taunted);
            Drain(mandatory);
            Assert.That(mandatory.RequiredTauntSource, Is.EqualTo(new EntityId(1)));
            Assert.That(mandatory.Progress(east), Is.EqualTo(1));
            Assert.That(mandatory.Progress(north), Is.Zero);
        }

        [Test]
        public void CandidateAndDecisionPartitionsAreIdentical()
        {
            string expected = null;
            foreach (int partition in new[] { 1, 2, 31 })
            {
                var input = World(railX: 4);
                using var candidates = Candidates(input);
                Drain(candidates, partition);
                using var decision = new EnemyDecisionSession(input, Rules, EnemyDecisionSettings.Default);
                Drain(decision, partition);
                string actual = EnemyActionAssessmentTests.Encode(new object[] { candidates.Attacks, candidates.Moves,
                    candidates.Moves.Select(candidates.Progress).ToArray(), decision.Decision });
                expected ??= actual;
                Assert.That(actual, Is.EqualTo(expected));
                Assert.That(decision.Diagnostics.PreviewCalls, Is.LessThanOrEqualTo(8));
                Assert.That(decision.Diagnostics.GeneratedMoves, Is.LessThanOrEqualTo(EnemyDecisionSettings.Default.MoveCandidateLimit), "R-4: K = 6");
                Assert.That(decision.Diagnostics.RefinedMoves, Is.LessThanOrEqualTo(EnemyDecisionSettings.Default.RefinedMoveLimit));
                TestContext.WriteLine($"partition={partition}; mapNodes={candidates.EngagementExpandedNodes}; work={decision.Diagnostics.WorkUnits}; previews={decision.Diagnostics.PreviewCalls}");
            }
        }

        [Test]
        public void MissingTargetAndUnreachableGoalDoNotInventProgress()
        {
            var input = World(width: 7, height: 1, actorCell: CellCoord.Zero, targetCell: new CellCoord(6, 0));
            var missing = Map(input, new EntityId(999));
            Assert.That(missing.TryGetCost(CellCoord.Zero, out _), Is.False);
            Assert.That(missing.Progress(CellCoord.Right), Is.Zero);
            var blocked = CliffFallTests.WithEnvironment(input, WallStateLifecycleTests.Wall(71, new CellCoord(3, 0)));
            var map = Map(blocked);
            Assert.That(map.TryGetCost(CellCoord.Zero, out _), Is.False);
            Assert.That(map.Progress(CellCoord.Right), Is.Zero);
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(unit => unit.Owner == Owner.Player
                ? unit.WithPlacement(CharacterRuntimeArea.Hand, 0, null, null, Array.Empty<CellCoord>()) : unit).ToArray());
            using var candidates = Candidates(input);
            Drain(candidates);
            Assert.That(candidates.EngagementExpandedNodes, Is.Zero);
            foreach (var move in candidates.Moves) Assert.That(candidates.Progress(move), Is.Zero);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void MineTransitAndGoalEligibilityRemainOwned(bool leap)
        {
            var input = World(width: 6, height: 1, actorCell: CellCoord.Zero, targetCell: new CellCoord(5, 0));
            input = CliffFallTests.WithEnvironment(input, new MineSourceFact(71, new CellCoord(2, 0), 1, 10, true, new[] { Owner.Enemy }),
                new MineSourceFact(72, new CellCoord(4, 0), 2, 10, true, new[] { Owner.Enemy }));
            if (leap) input = TraitCombatFixtures.Equip(input, EntityKey.Unit_Demon, new LeapTraitSpec());
            var map = Map(input);
            // 마지막 공격 위치도 지뢰이면 도약으로 통과할 수 있어도 목표가 될 수 없다.
            Assert.That(map.TryGetCost(CellCoord.Zero, out _), Is.False);
            input = CliffFallTests.WithEnvironment(input, new MineSourceFact(71, new CellCoord(2, 0), 1, 10, true, new[] { Owner.Enemy }));
            map = Map(input);
            Assert.That(map.TryGetCost(CellCoord.Zero, out _), Is.EqualTo(leap));
            if (leap) Assert.That(map.Progress(new CellCoord(3, 0)), Is.EqualTo(3));
        }

        [Test]
        public void CollapseGoalAndLivingOccupancyAreNotInventedLandings()
        {
            var input = World(width: 6, height: 1, actorCell: CellCoord.Zero, targetCell: new CellCoord(5, 0));
            input = CliffFallTests.WithEnvironment(input, new StaticTerrainFact(71, new CellCoord(4, 0), StaticTerrainKind.WeakGround,
                true, new[] { Owner.Player, Owner.Enemy }, entryThreshold: 2, entryCount: 1));
            Assert.That(Map(input).TryGetCost(CellCoord.Zero, out _), Is.False);
            var clear = World(width: 6, height: 1, actorCell: CellCoord.Zero, targetCell: new CellCoord(5, 0));
            var occupant = Character(12, EntityKey.Unit_Demon, Owner.Enemy, new CellCoord(2, 0), 20, 1);
            var occupied = CombatCommandTestData.With(clear, characters: clear.Characters.Concat(new[] { occupant }).ToArray());
            // 아군 칸은 여러 턴 추정에서 비용을 더해 지나가지만(R-2) 공격 위치·착지 칸이 되지는 않는다.
            var map = Map(occupied);
            Assert.That(map.IsAttackPosition(new CellCoord(2, 0)), Is.False);
            Assert.That(map.TryGetCost(new CellCoord(2, 0), out int through), Is.True);
            Assert.That(through, Is.EqualTo(2));
            Assert.That(map.TryGetCost(CellCoord.Zero, out int origin), Is.True);
            Assert.That(origin, Is.EqualTo(4 + EnemyAiTuning.AllySoftCost));
            Assert.That(Map(clear).Progress(new CellCoord(2, 0)), Is.EqualTo(2));
        }

        [Test]
        public void LeavingMineAtOriginDoesNotMakeReentryTraversable()
        {
            var input = World(width: 7, height: 1, actorCell: new CellCoord(2, 0), targetCell: new CellCoord(6, 0));
            input = CliffFallTests.WithEnvironment(input, new MineSourceFact(71, new CellCoord(2, 0), 1, 10, true, new[] { Owner.Enemy }));
            var map = Map(input);
            Assert.That(map.TryGetCost(new CellCoord(2, 0), out int origin), Is.True);
            Assert.That(origin, Is.EqualTo(3));
            Assert.That(map.TryGetCost(new CellCoord(1, 0), out _), Is.False);
            Assert.That(map.Progress(new CellCoord(1, 0)), Is.Zero);
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(31)]
        public void MapCapsAndPartitionsAgree(int partition)
        {
            const int limit = EnemyEngagementField.CellLimit;
            // 대상 칸은 이동할 수 없으므로 폭 limit+1 한 줄은 이동 가능 칸이 정확히 limit개다.
            var full = World(width: limit + 1, height: 1, actorCell: CellCoord.Zero, targetCell: new CellCoord(limit, 0));
            var map = Map(full, partition: partition);
            Assert.That(map.IsTruncated, Is.False);
            Assert.That(map.ExpandedNodes, Is.EqualTo(limit * 2));
            Assert.That(map.TryGetCost(CellCoord.Zero, out int distance), Is.True);
            Assert.That(distance, Is.EqualTo(limit - 1));
            Assert.That(map.Progress(CellCoord.Right), Is.EqualTo(1));
            var over = World(width: limit + 2, height: 1, actorCell: CellCoord.Zero, targetCell: new CellCoord(limit + 1, 0));
            map = Map(over, partition: partition);
            Assert.That(map.IsTruncated, Is.True);
            Assert.That(map.ExpandedNodes, Is.LessThanOrEqualTo(limit));
            Assert.That(map.TryGetCost(CellCoord.Zero, out _), Is.False);
            Assert.That(map.Progress(CellCoord.Right), Is.Zero);
            using var candidates = Candidates(over);
            Drain(candidates, partition);
            Assert.That(candidates.EngagementTruncated, Is.True);
            Assert.That(candidates.EngagementExpandedNodes, Is.LessThanOrEqualTo(limit));
            // 지도를 버린 뒤에는 대표 추격 경로 끝 칸만 그 걸음 수를 받는다. 부분 지도 거리는 쓰지 않는다.
            Assert.That(candidates.Moves.Count(move => candidates.Progress(move) != 0), Is.LessThanOrEqualTo(1));
            foreach (var move in candidates.Moves)
                Assert.That(candidates.Progress(move), Is.Zero.Or.EqualTo(move.Path.StepCount));
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(31)]
        public void TruncatedMapFallsBackToRepresentativePursuitProgress(int partition)
        {
            // 20×15 = 300칸이라 거리 지도가 상한(289칸)에 걸린다. 적은 이번 턴에 공격할 수 없다.
            var input = World(width: 20, height: 15, actorCell: new CellCoord(1, 7), targetCell: new CellCoord(18, 7));
            string before = EnemyActionAssessmentTests.Encode(input);
            using var candidates = Candidates(input);
            Drain(candidates, partition);
            Assert.That(candidates.EngagementTruncated, Is.True);
            Assert.That(candidates.Attacks, Is.Empty);
            var rewarded = candidates.Moves.Where(move => candidates.Progress(move) != 0).ToArray();
            Assert.That(rewarded, Has.Length.EqualTo(1));
            Assert.That(candidates.Progress(rewarded[0]), Is.EqualTo(rewarded[0].Path.StepCount));

            using var decision = new EnemyDecisionSession(input, Rules, EnemyDecisionSettings.Default);
            Drain(decision, partition);
            Assert.That(decision.Decision.Kind, Is.EqualTo(EnemyDecisionKind.Move));
            var target = new CellCoord(18, 7);
            Assert.That(CombatMovementCalculator.Distance(decision.Decision.Path.Endpoint, target),
                Is.LessThan(CombatMovementCalculator.Distance(new CellCoord(1, 7), target)));
            var actual = CombatCommandCalculator.AdvancePrepared(input, decision.Decision, Rules);
            Assert.That(actual.Accepted, Is.True, actual.Reason);
            Assert.That(EnemyActionAssessmentTests.Encode(input), Is.EqualTo(before));
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(31)]
        public void LargestPlannedFieldBuildsTheCompleteMap(int partition)
        {
            // 예비 최대 필드 17×17의 빈 판이다. 상한 안이라 대체 점수가 아니라 완성된 지도 거리로 점수를 준다.
            var target = new CellCoord(15, 8);
            var input = World(width: 17, height: 17, actorCell: new CellCoord(1, 8), targetCell: target);
            var map = Map(input, partition: partition);
            Assert.That(map.IsTruncated, Is.False);
            Assert.That(map.TryGetCost(new CellCoord(1, 8), out int origin), Is.True);
            Assert.That(origin, Is.EqualTo(13));
            Assert.That(map.TryGetCost(new CellCoord(16, 16), out _), Is.True, "판 끝 칸까지 지도가 덮어야 한다.");
            using var candidates = Candidates(input);
            Drain(candidates, partition);
            Assert.That(candidates.EngagementTruncated, Is.False);
            Assert.That(candidates.Moves, Is.Not.Empty);
            foreach (var move in candidates.Moves)
                Assert.That(candidates.Progress(move), Is.EqualTo(map.Progress(move.Path.Endpoint)));
            // 대표 추격 끝 칸이 아닌 짧은 접근도 점수를 받는다.
            Assert.That(candidates.Moves.Count(move => candidates.Progress(move) > 0), Is.GreaterThan(1));

            using var decision = new EnemyDecisionSession(input, Rules, EnemyDecisionSettings.Default);
            Drain(decision, partition);
            Assert.That(decision.Decision.Kind, Is.EqualTo(EnemyDecisionKind.Move));
            Assert.That(map.Progress(decision.Decision.Path.Endpoint), Is.Positive);
            var actual = CombatCommandCalculator.AdvancePrepared(input, decision.Decision, Rules);
            Assert.That(actual.Accepted, Is.True, actual.Reason);
        }

        // 옛 단일 대상 거리 지도의 시험을 교전 필드로 옮겼다(R-2). 한 대상으로 제한하면 옛 지도와 같은 거리다.
        private static EnemyEngagementField Map(CombatCommandInput input, EntityId? target = null, int partition = 1) =>
            EnemyEngagementFieldTests.Build(input, new[] { target ?? new EntityId(1) }, partition);

        private static void AssertGoalsMatchReference(CombatCommandInput input, EntityId target, int width, int height) =>
            EnemyEngagementFieldTests.AssertGoalsMatchReference(input, target, width, height);

        private static EnemyActionCandidates Candidates(CombatCommandInput input) =>
            new EnemyActionCandidates(new EnemyDecisionContext(input, EnemyDecisionSettings.Default),
                input.Characters.Single(unit => unit.EntityId == input.ActorId), EnemyActionCandidateTests.MakeForecast(input));

        private static void Drain(EnemyActionCandidates work, int partition = 1)
        {
            int steps = 0;
            while (!work.IsComplete && steps < 2000000)
                for (int i = 0; i < partition && !work.IsComplete; i++, steps++) work.Step();
            Assert.That(work.IsComplete, Is.True);
        }

        private static void Drain(EnemyDecisionSession work, int partition = 1)
        {
            int steps = 0;
            while (!work.IsReady && steps < 2000000)
                for (int i = 0; i < partition && !work.IsReady; i++, steps++) work.Step();
            Assert.That(work.IsReady, Is.True);
        }

        private static CombatCommandInput World(int width = 9, int height = 5, CellCoord? actorCell = null,
            CellCoord? targetCell = null, int railX = -1, int range = 1)
        {
            var enemy = Character(11, EntityKey.Unit_Demon, Owner.Enemy, actorCell ?? new CellCoord(1, 2), 20, range);
            var main = Character(1, EntityKey.Unit_MainCharacter, Owner.Player, targetCell ?? new CellCoord(7, 2), 0, 3);
            var units = new[] { main, enemy };
            var profiles = new[] { Profile(enemy.CharacterKey, 20, range), Profile(main.CharacterKey, 0, 3) };
            var definitions = railX < 0 ? Array.Empty<StageGimmickSpec>() : new StageGimmickSpec[] { new RailwayGimmickSpec(2, 3) };
            var rails = railX < 0 ? Array.Empty<CellCoord>() : Enumerable.Range(0, height).Select(y => new CellCoord(railX, y)).ToArray();
            var state = new CombatCommandState(width, height, CombatFlowPhase.Automatic, Array.Empty<StructureCombatFacts>(),
                new CombatTurnFacts(StagePhase.Combat, controllerPhase: CombatControllerPhase.Enemy,
                    controllerTurnSequence: 2, completedPlayerTurnCount: 1, completedControllerTurnCount: 1));
            state = StageGimmickComposition.Initialize(state, definitions);
            var owned = new[] { new OwnedCharacterRecord(main.OwnedId.Value, main.CharacterKey, main.OwnershipRole.Value) };
            return new CombatCommandInput(0, CombatCommandKind.CompleteEnemyTurn, enemy.EntityId, default, state, units, profiles,
                new MainCharacterPersistentState(100, 100, 1), owned, owned.ToDictionary(o => o.Id, _ => 0), SoulFieldSnapshot.Empty,
                new ArtifactLoadoutSnapshot(0, Array.Empty<ArtifactPlacedEffectSnapshot>()),
                battle: new CombatBattleFacts(8, 1, 0, 0, Array.Empty<CharacterInstanceId>(), 0, rails, definitions),
                processingKind: CombatProcessingKind.Advance, battleInstanceId: 1, expectedBattleInstanceId: 1, expectedControllerTurnSequence: 2);
        }

        private static CharacterCombatFacts Character(int id, EntityKey key, Owner owner, CellCoord cell, int attack, int range)
        {
            CharacterInstanceId? owned = owner == Owner.Player ? new CharacterInstanceId(id) : null;
            return new CharacterCombatFacts(new EntityId(id), owned, key, owner,
                owned.HasValue ? CharacterOwnershipRole.Main : (CharacterOwnershipRole?)null,
                CharacterRuntimeArea.Field, new HealthStateSnapshot(100, 100),
                new UnitStatsSnapshot(MoveMethod.LiftAndPlace, 3, range, attack, 0, 100, Array.Empty<TraitStatusSnapshot>(), CategorySnapshot.CreateStateless(CategoryKey.Main)),
                null, cell, Rotation4.R0, new[] { cell }, UnitActionUse.Available);
        }

        private static CombatAttackProfile Profile(EntityKey key, int attack, int range) =>
            new CombatAttackProfile(new UnitSpec(key, UnitRole.Regular, 1, 1, new[] { CellCoord.Zero }, 100, MoveMethod.LiftAndPlace,
                range, CategoryKey.Main, attack, 0, 100, Array.Empty<TraitSpec>()),
                new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0));
    }
}
