using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Movement;

namespace ProjectL.Tests.Core
{
    /// <summary>
    /// 플랫폼 운반 규칙의 단일 원본(설계 2026-11-17 PF-1·PF-2·PF-5·PF-10)을 계산과 근거 검사 양쪽에서 확인한다.
    /// </summary>
    public sealed class PlatformRiderTransportTests
    {
        private static readonly IMovementTileRule[] NoRules = Array.Empty<IMovementTileRule>();

        [Test]
        public void TornadoLandingOnTheSamePlatformKeepsTheRiderCarried()
        {
            var input = SamePlatformTornadoInput();
            var result = Run(input, TornadoEnvironmentTests.Rules);

            var rider = new EntityId(1);
            var relocated = result.Events.OfType<CombatCharacterEvent>()
                .Single(e => e.Kind == CombatEventKind.Relocated && e.Final.EntityId == rider);
            int landingY = relocated.Final.Anchor.Value.Y;
            Assert.That(relocated.Final.Anchor, Is.EqualTo(new CellCoord(2, landingY)));
            Assert.That(landingY, Is.Not.Zero);

            var steps = Motion(result).Steps.Where(s => s.StepKind == PlatformStepKind.Translation).ToArray();
            Assert.That(steps.Length, Is.EqualTo(3));
            Assert.That(steps.Select(s => s.Passengers.Single().EntityId), Is.All.EqualTo(rider));
            Assert.That(steps.Select(s => s.Passengers.Single().TransportEnded), Is.All.False);
            // 다음 칸은 옮겨진 실제 위치에서 출발한다.
            Assert.That(steps[1].Passengers.Single().From, Is.EqualTo(new CellCoord(2, landingY)));
            Assert.That(steps[2].Passengers.Single().To, Is.EqualTo(new CellCoord(4, landingY)));

            var final = CombatCommandComposition.Project(result).Characters.Single(c => c.EntityId == rider);
            Assert.That(final.Health.IsDead, Is.False);
            Assert.That(final.Anchor, Is.EqualTo(new CellCoord(4, landingY)));
            Assert.That(result.Events.OfType<CombatFallEvent>(), Is.Empty);
            Assert.DoesNotThrow(() => CombatChangeEvidenceReader.ReadAll(result));
        }

        [Test]
        public void EvidenceRejectsEndingTransportOfARiderStillOnThePlatform()
        {
            var valid = Run(SamePlatformTornadoInput(), TornadoEnvironmentTests.Rules);
            var motion = Motion(valid);
            var first = motion.Steps[0];
            var forgedStep = new CombatPlatformStepEvent(first.BoundaryCount, first.OrderIndex, first.StepIndex, first.StepKind,
                first.SourceChange, first.Passengers.Select(p => new PlatformPassengerStep(p.EntityId, p.From, p.To, true)).ToArray(),
                first.CompletionEvent);
            var forgedMotion = new CombatPlatformMotionEvent(motion.BoundaryCount, motion.OrderIndex, motion.Expected, motion.Final,
                motion.Steps.Select(s => ReferenceEquals(s, first) ? forgedStep : s).ToArray(), motion.StopReason);
            var events = valid.Events.Select(e => ReferenceEquals(e, first) ? forgedStep : ReferenceEquals(e, motion) ? forgedMotion : e)
                .ToArray();
            Assert.Throws<InvalidOperationException>(() => PlatformMotionEvidenceValidator.Validate(Forge(valid, events)));
        }

        [Test]
        public void EvidenceRejectsAnEndpointArrivalWithoutTurnaround()
        {
            var valid = Run(PlatformMotionTests.Input(EndpointPlatform()), NoRules);
            var motion = Motion(valid);
            Assert.That(motion.Steps.Select(s => s.StepKind),
                Is.EqualTo(new[] { PlatformStepKind.Translation, PlatformStepKind.Turnaround }));
            var turn = motion.Steps[1];
            var arrived = motion.Steps[0].Final;
            var forgedMotion = new CombatPlatformMotionEvent(motion.BoundaryCount, motion.OrderIndex, motion.Expected, arrived,
                new[] { motion.Steps[0] }, motion.StopReason);
            var events = valid.Events.Where(e => !ReferenceEquals(e, turn) && !ReferenceEquals(e, turn.SourceChange))
                .Select(e => ReferenceEquals(e, motion) ? forgedMotion : e).ToArray();
            var environment = new MovementEnvironmentFacts(valid.FinalState.Environment.Tiles
                .Select(t => t.Id == arrived.Id ? arrived : t).ToArray());
            var forged = Forge(valid, events, valid.FinalState.With(environment: environment));
            var exception = Assert.Throws<InvalidOperationException>(() => PlatformMotionEvidenceValidator.Validate(forged));
            StringAssert.Contains("끝점 도착 뒤의 즉시 반전", exception.Message);
        }

        [Test]
        public void EvidenceDoesNotRecheckOccupancyAtTheEndpointTurnaround()
        {
            // 계산기는 이동 칸 앞에서만 점유를 검사하고 끝점 반전은 효과 뒤에 바로 한다. 근거 검사도 반전에서는 점유를 보지 않는다(PF-2).
            var input = PlatformMotionTests.Input(EndpointPlatform());
            var valid = Run(input, NoRules);
            var obstacle = ObstacleReactionTests.Barrel(51, new CellCoord(2, 0), 3, 0);
            var occupied = new CombatCommandInput(input.Number, input.Kind, input.ActorId, input.Aim,
                input.State.With(structures: new[] { obstacle.Structure }), input.Characters, input.Profiles,
                input.Main, input.Owned, input.SoulCounts, input.Souls, input.Artifacts, battle: input.Battle,
                processingKind: input.ProcessingKind, battleInstanceId: input.BattleInstanceId,
                environmentBindings: new EnvironmentSemanticBindings(input.EnvironmentBindings.TerrainSourceIds,
                    input.EnvironmentBindings.ObjectSourceIds, new[] { obstacle.Structure.EntityId }),
                obstacleContents: new[] { obstacle.Profile });
            var forged = new CombatCommandTransition(occupied, true, "", valid.Characters, valid.Field,
                valid.FinalState.With(structures: occupied.State.Structures), valid.Outcome, valid.Attack, valid.ConsumedSoulIds,
                events: valid.Events, healthExecutions: valid.HealthExecutions, healthClaims: valid.HealthClaims);
            Assert.DoesNotThrow(() => PlatformMotionEvidenceValidator.Validate(forged));
        }

        [Test]
        public void TransportEndsOnlyOnDeathLeavingTheFieldOrLeavingTheFootprint()
        {
            var platform = new MovingPlatformFact(new MovingPlatformSpec(100, 3, PlatformAxis.Vertical,
                new CellCoord(1, 0), new CellCoord(5, 0), new CellCoord(2, 0), 1));
            var onOtherCell = CombatCommandTestData.Character(1, EntityKey.Unit_Akimbo, new CellCoord(2, 2), Owner.Player);
            var offPlatform = CombatCommandTestData.Character(1, EntityKey.Unit_Akimbo, new CellCoord(3, 0), Owner.Player);
            var dead = CombatCommandTestData.Character(1, EntityKey.Unit_Akimbo, new CellCoord(2, 1), Owner.Player)
                .WithHealth(new HealthStateSnapshot(0, 100));
            Assert.That(PlatformMotionRules.EndsTransport(onOtherCell, platform), Is.False);
            Assert.That(PlatformMotionRules.EndsTransport(offPlatform, platform), Is.True);
            Assert.That(PlatformMotionRules.EndsTransport(dead, platform), Is.True);
        }

        [TestCase(CombatFlowPhase.Automatic, CombatControllerPhase.StartingEnemy, 1, 0, 1, true)]
        [TestCase(CombatFlowPhase.Automatic, CombatControllerPhase.StartingPlayer, 2, 1, 1, true)]
        [TestCase(CombatFlowPhase.Automatic, CombatControllerPhase.StartingEnemy, 1, 1, 1, false)]
        [TestCase(CombatFlowPhase.Automatic, CombatControllerPhase.StartingEnemy, 1, 0, 0, false)]
        [TestCase(CombatFlowPhase.PlayerInput, CombatControllerPhase.Player, 1, 0, 1, false)]
        [TestCase(CombatFlowPhase.Finished, CombatControllerPhase.StartingEnemy, 1, 0, 1, false)]
        public void PreludeIsDueOnlyBeforeAControllerTurnAfterAnUnprocessedBoundary(CombatFlowPhase phase,
            CombatControllerPhase controller, int completed, int processed, int platforms, bool expected)
        {
            var state = PlatformMotionTests.Input(EndpointPlatform()).State;
            state = state.With(phase: phase, turn: new CombatTurnFacts(StagePhase.Combat, controller, 1, completed,
                completedControllerTurnCount: completed), lastPlatformBoundaryCount: processed);
            Assert.That(PlatformMotionRules.IsPreludeDue(state, platforms), Is.EqualTo(expected));
        }

        [Test]
        public void ObstaclesAreNotAdmittedOntoSupportingPlatformCells()
        {
            var platform = EndpointPlatform();
            var tiles = new MovementTileFact[] { PlatformTestData.Terrain(1, new CellCoord(1, 0)),
                PlatformTestData.Terrain(2, new CellCoord(2, 0)), platform };
            var environment = new MovementEnvironmentFacts(tiles);
            var bound = PlatformTestData.Bind(environment);
            var unbound = new EnvironmentSemanticBindings(new long[] { 1, 2 }, Array.Empty<long>(), Array.Empty<EntityId>());
            Assert.That(BoardSurfaceRules.AdmitsObstacleAt(environment, bound, new[] { new CellCoord(1, 0) }), Is.False);
            Assert.That(BoardSurfaceRules.AdmitsObstacleAt(environment, bound, new[] { new CellCoord(2, 0) }), Is.True);
            Assert.That(BoardSurfaceRules.AdmitsObstacleAt(environment, unbound, new[] { new CellCoord(1, 0) }), Is.True);
        }

        /// <summary>길이 1, 경로 (1,0)–(2,0). 한 칸 이동하면 끝점이라 즉시 반전한다.</summary>
        private static MovingPlatformFact EndpointPlatform() => new MovingPlatformFact(new MovingPlatformSpec(100, 1,
            PlatformAxis.Vertical, new CellCoord(1, 0), new CellCoord(2, 0), new CellCoord(1, 0), 1));

        /// <summary>
        /// 세로 모양 길이 3 플랫폼((1,0)~(1,2))이 +X로 세 칸 간다. 탑승자 1은 (1,0)에서 출발해 첫 칸 (2,0)의 토네이도에 던져진다.
        /// 경로 칸과 메인 칸을 뺀 모든 칸을 벽으로 막아 착지 후보를 같은 플랫폼의 (2,1)·(2,2)로 한정한다.
        /// </summary>
        private static CombatCommandInput SamePlatformTornadoInput()
        {
            var platform = new MovingPlatformFact(new MovingPlatformSpec(100, 3, PlatformAxis.Vertical,
                new CellCoord(1, 0), new CellCoord(5, 0), new CellCoord(1, 0), 1));
            var seed = PlatformMotionTests.Input(platform);
            var rider = CombatCommandTestData.Character(1, EntityKey.Unit_Akimbo, new CellCoord(1, 0), Owner.Player);
            var characters = seed.Characters.Where(c => c.OwnershipRole == CharacterOwnershipRole.Main && c.EntityId != rider.EntityId)
                .Append(rider).ToArray();
            var open = new HashSet<CellCoord>(seed.State.Environment.Tiles.OfType<StaticTerrainFact>().Select(t => t.Cell));
            open.UnionWith(characters.Where(c => c.EntityId != rider.EntityId).Select(c => c.Anchor.Value));
            long nextWall = 200;
            var walls = new List<MovementTileFact>();
            for (int y = 0; y < seed.State.BoardHeight; y++)
                for (int x = 0; x < seed.State.BoardWidth; x++)
                    if (!open.Contains(new CellCoord(x, y)))
                        walls.Add(new StaticTerrainFact(nextWall++, new CellCoord(x, y), StaticTerrainKind.Wall, true,
                            new[] { Owner.Player, Owner.Enemy }));
            var environment = new MovementEnvironmentFacts(seed.State.Environment.Tiles.Concat(walls).ToArray());
            var state = seed.State.With(environment: environment,
                environmentProgress: new EnvironmentProgressFacts(1, nextWall, 42));
            var storm = TornadoGimmickResolutionTests.Input(center: new CellCoord(2, 0), victim: new CellCoord(1, 0));
            return new CombatCommandInput(0, seed.Kind, seed.ActorId, seed.Aim, state.With(gimmicks: storm.State.Gimmicks),
                characters, seed.Profiles, seed.Main, seed.Owned, seed.SoulCounts, seed.Souls, seed.Artifacts,
                battle: storm.Battle, processingKind: CombatProcessingKind.Advance, battleInstanceId: 1,
                environmentBindings: PlatformTestData.Bind(environment));
        }

        private static CombatCommandTransition Forge(CombatCommandTransition valid, IReadOnlyList<CombatEvent> events,
            CombatCommandState finalState = null) =>
            new CombatCommandTransition(valid.Input, true, "", valid.Characters, valid.Field, finalState ?? valid.FinalState,
                valid.Outcome, valid.Attack, valid.ConsumedSoulIds, events: events, healthExecutions: valid.HealthExecutions,
                healthClaims: valid.HealthClaims);

        private static CombatPlatformMotionEvent Motion(CombatCommandTransition result) =>
            result.Events.OfType<CombatPlatformMotionEvent>().Single();

        private static CombatCommandTransition Run(CombatCommandInput input, IReadOnlyList<IMovementTileRule> rules)
        {
            var result = CombatCommandCalculator.Advance(input, rules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            PlatformMotionEvidenceValidator.Validate(result);
            return result;
        }
    }
}
