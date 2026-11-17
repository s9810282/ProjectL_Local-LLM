using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Tests.Core
{
    public sealed class PillarPlatformPreviewTests
    {
        private static readonly IMovementTileRule[] NoRules = Array.Empty<IMovementTileRule>();

        [Test]
        public void AttackPreviewPublishesTheProjectedPillarFall()
        {
            var passenger = new CellCoord(5, 3);
            var wall = new CellCoord(6, 3);
            var input = PillarTestData.Input(PlatformTestData.Terrain(71, passenger),
                PlatformTestData.Platform(72, passenger, new CellCoord(5, 6)),
                PlatformTestData.Terrain(73, wall, StaticTerrainKind.Wall));
            var before = EnemyActionAssessmentTests.Encode(input);
            var preview = CombatCommandCalculator.Preview(input, NoRules);
            var attack = PillarTestData.Attack(input);
            var fall = attack.Events.OfType<CombatPillarFallEvent>().Single();
            var forecast = preview.PillarFalls.Single();
            Assert.That(preview.CanAttack, Is.True);
            Assert.That(attack.Attack.Value.Hits.Span[0].DamageDealt, Is.Zero);
            Assert.That(forecast.ImpactCells, Is.EqualTo(fall.ImpactCells));
            Assert.That(forecast.ImpactCells, Has.Member(passenger).And.Member(wall));
            Assert.That(forecast.CrushedUnitIds, Is.EqualTo(fall.CrushedUnitIds).And.Not.Empty);
            Assert.That(forecast.TerrainChanges.Select(change => change.Cell), Is.EqualTo(new[] { new CellCoord(7, 3) }));
            Assert.That(EnemyActionAssessmentTests.Encode(input), Is.EqualTo(before));
        }

        [Test]
        public void NonDestroyingContactDoesNotPredictAFall()
        {
            var input = ObstacleReactionTests.Barrels(PillarTestData.Pillar(hits: 2));
            Assert.That(CombatCommandCalculator.Preview(input, NoRules).PillarFalls, Is.Empty);
        }

        [Test]
        public void HiddenSourceAllocationDoesNotChangePublicPillarForecast()
        {
            var input = PillarTestData.Input();
            var hiddenMine = MineEnvironmentTests.Mine(800, hidden: true, cell: new CellCoord(0, 7));
            var environment = new MovementEnvironmentFacts(new[] { hiddenMine });
            var hidden = input.WithWorld(input.State.With(environment: environment,
                environmentProgress: EnvironmentProgressFacts.Initial(environment)), input.Characters, input.Main,
                input.SoulCounts, input.Souls, new EnvironmentSemanticBindings(Array.Empty<long>(),
                    new[] { hiddenMine.Id }, input.EnvironmentBindings.ObstacleEntityIds));

            var expected = CombatCommandCalculator.Preview(input, NoRules).PillarFalls.Single();
            var result = CombatCommandCalculator.Preview(hidden, NoRules).PillarFalls.Single();
            Assert.That(EnemyActionAssessmentTests.Encode(result.TerrainChanges),
                Is.EqualTo(EnemyActionAssessmentTests.Encode(expected.TerrainChanges)));
        }

        [Test]
        public void ChainReactionsFollowTheProjectedCause()
        {
            var first = ObstacleReactionTests.Barrel(52, new CellCoord(6, 3), 3, 7);
            var second = ObstacleReactionTests.Barrel(53, new CellCoord(6, 4), 3, 7);
            var input = ObstacleReactionTests.Barrels(PillarTestData.Pillar(), first, second);
            var execution = PillarTestData.Attack(input);
            var preview = CombatCommandCalculator.Preview(input, NoRules).PillarFalls.Single();
            var reactions = execution.Events.OfType<CombatObstacleReactionEvent>()
                .Where(e => e.ObstacleId != preview.PillarId).Select(e => e.ObstacleId);
            Assert.That(preview.ChainReactions.Select(e => e.ObstacleId), Is.EqualTo(reactions));
            Assert.That(preview.ChainReactions.Select(e => e.ObstacleId),
                Is.EqualTo(new[] { first.Structure.EntityId, second.Structure.EntityId }));
        }

        [Test]
        public void AiKeepsCollateralRiskForAReachablePillar()
        {
            var input = PillarTestData.Input();
            var score = EnemyActionAssessmentTests.Assess(input, NoRules, out _);
            Assert.That(score.IsPrecise, Is.False);
            Assert.That(score.PossibleAlliedHpLossPermille, Is.GreaterThan(0));
        }
        [Test]
        public void AiBoundsPillarCrushBeyondTheMineBlast()
        {
            var seed = MineEnvironmentTests.Input(MineEnvironmentTests.Mine());
            var pillar = PillarTestData.Pillar(cell: new CellCoord(3, 2));
            var ally = CombatCommandTestData.Character(8, EntityKey.Unit_Akimbo, new CellCoord(4, 2), Owner.Player);
            var input = MineEnvironmentTests.Copy(seed, seed.Characters.Append(ally).ToArray(),
                new[] { pillar.Structure }, new[] { pillar.Profile });
            var execution = CombatCommandCalculator.Calculate(input, MineEnvironmentTests.Rules);
            Assert.That(execution.Events.OfType<CombatPillarFallEvent>().Single().CrushedUnitIds, Has.Member(ally.EntityId));
            var score = EnemyActionAssessmentTests.Assess(input, MineEnvironmentTests.Rules, out _);
            Assert.That(score.PossibleAlliedHpLossPermille, Is.GreaterThanOrEqualTo(2000));
        }

        [Test]
        public void RepeatedForecastMatchesSeededExecutionWithoutConsumingRandomness()
        {
            var other = new MovingPlatformFact(new MovingPlatformSpec(101, 3, PlatformAxis.Horizontal,
                new CellCoord(0, 5), new CellCoord(0, 7), new CellCoord(0, 5), 1));
            var input = PlatformMotionTests.Input(new[] { PlatformMotionTests.ReboundPlatform(), other });
            var before = EnemyActionAssessmentTests.Encode(input);
            var first = PlatformForecastCalculator.Forecast(input, 42, NoRules);
            var second = PlatformForecastCalculator.Forecast(input, 42, NoRules);
            var execution = CombatCommandCalculator.Advance(input, NoRules);
            var motions = execution.Events.OfType<CombatPlatformMotionEvent>().ToArray();
            Assert.That(first.Coverage, Is.EqualTo(PlatformForecastCoverage.KnownInformationConditional));
            Assert.That(first.ProjectedBoundaryCount, Is.EqualTo(1));
            Assert.That(first.Paths.Select(path => path.PlatformSourceId), Is.EqualTo(motions.Select(m => m.PlatformSourceId)));
            Assert.That(second.Paths.Select(path => path.PlatformSourceId), Is.EqualTo(first.Paths.Select(path => path.PlatformSourceId)));
            for (int i = 0; i < motions.Length; i++)
            {
                Assert.That(first.Paths[i].Steps.Select(step => (step.Kind, step.To, step.TravelSign)),
                    Is.EqualTo(motions[i].Steps.Select(step => (step.StepKind, step.Final.Cell, step.Final.TravelSign))));
            }
            Assert.That(first.Source.Matches(input.BattleInstanceId, 43, input.State), Is.False);
            Assert.That(execution.FinalState.EnvironmentProgress.RandomState, Is.Not.EqualTo(input.State.EnvironmentProgress.RandomState));
            Assert.That(EnemyActionAssessmentTests.Encode(input), Is.EqualTo(before));
        }

        [Test]
        public void NoPlatformReturnsEmptyExactAnswer()
        {
            var input = PlatformMotionTests.Input(Array.Empty<MovingPlatformFact>());
            var forecast = PlatformForecastCalculator.Forecast(input, 42, NoRules);
            Assert.That(forecast.Paths, Is.Empty);
            Assert.That(forecast.ProjectedBoundaryCount, Is.Null);
            Assert.That(forecast.Coverage, Is.EqualTo(PlatformForecastCoverage.Exact));
        }

        [TestCase(Owner.Player, true)]
        [TestCase(Owner.Enemy, false)]
        public void ForecastUsesThePlayersGroupedTornadoVisibility(Owner visibility, bool visibleEjection)
        {
            var start = new CellCoord(1, 0);
            var center = new CellCoord(2, 0);
            var platform = new MovingPlatformFact(new MovingPlatformSpec(100, 3, PlatformAxis.Vertical,
                start, new CellCoord(5, 0), start, 1));
            var input = PlatformMotionTests.Input(platform);
            input = CombatCommandTestData.With(input, characters: input.Characters
                .Where(c => c.EntityId.Value != 1 && c.OwnershipRole == CharacterOwnershipRole.Main)
                .Append(CombatCommandTestData.Character(1, EntityKey.Unit_Akimbo, start, Owner.Player)).ToArray());
            var storm = TornadoGimmickResolutionTests.Input(center: center, victim: start);
            var spec = new TornadoGimmickSpec(new[] { new TornadoMemberSpec(31, new[] { visibility }, center, 1) });
            var battle = new CombatBattleFacts(4, 1, 8, 8, Array.Empty<CharacterInstanceId>(), 0, gimmicks: new[] { spec });
            var current = new CombatCommandInput(0, input.Kind, input.ActorId, input.Aim, input.State.With(gimmicks: storm.State.Gimmicks),
                input.Characters, input.Profiles, input.Main, input.Owned, input.SoulCounts, input.Souls, input.Artifacts,
                battle: battle, processingKind: CombatProcessingKind.Advance, battleInstanceId: 1,
                environmentBindings: input.EnvironmentBindings);

            var forecast = PlatformForecastCalculator.Forecast(current, 42, TornadoEnvironmentTests.Rules);
            Assert.That(forecast.Paths.Single().Steps.First().Passengers.Single().TransportEnded, Is.EqualTo(visibleEjection));
            var execution = CombatCommandCalculator.Advance(current, TornadoEnvironmentTests.Rules);
            Assert.That(execution.Events.OfType<CombatPlatformMotionEvent>().Single().Steps.First()
                .Passengers.Single().TransportEnded, Is.True);
        }
    }
}
