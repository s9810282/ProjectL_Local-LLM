using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Diagnostics;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Tests.Core
{
    public sealed class EnemyThreatFieldTests
    {
        private static readonly CellCoord Origin = new CellCoord(1, 3);
        private static readonly CellCoord Endpoint = new CellCoord(4, 3);

        [Test]
        public void ThreatFieldReusesGeometry()
        {
            var input = InputWithDistantMain(new TargetingMethodSpec(TargetingShape.EightDirectionLine,
                TargetingChoiceMode.Single, TargetingSplash.None, 0));
            var offLine = new CellCoord(4, 5);
            var target = Target(input);
            Assert.That(CombatAttackQueries.Selection(input, input.Characters[0], input.Profiles[0],
                AttackAim.ForCell(Endpoint)).FailureReason, Is.EqualTo(AttackFailureReason.None));
            var field = Build(input);
            Assert.That(field.Evaluate(target, Endpoint).EstimatedLossPermille, Is.EqualTo(200));
            Assert.That(field.Evaluate(target, offLine).EstimatedLossPermille, Is.Zero);
            Assert.That(field.Evaluate(target, new CellCoord(3, 3)).EstimatedLossPermille, Is.EqualTo(200));
        }

        [Test]
        public void NoFullPreviewInThreatConstruction()
        {
            var input = CombatCommandTestData.Input();
            var profiler = new ProjectionCounter();
            CoreProfiling.Install(profiler);
            try
            {
                var context = new EnemyDecisionContext(input, EnemyDecisionSettings.Default);
                using var builder = new EnemyThreatField.Builder(context);
                var field = Finish(builder);
                Assert.That(field.Evaluate(Target(input), Endpoint).EstimatedLossPermille, Is.Positive);
                Assert.That(field.CoveredCellCount, Is.LessThanOrEqualTo(input.State.BoardWidth * input.State.BoardHeight));
                Assert.That(builder.WorkVersion, Is.GreaterThan(field.CoveredCellCount));
                Assert.That(context.PreviewCount, Is.Zero);
                Assert.That(profiler.AttackProjections, Is.Zero);
            }
            finally
            {
                CoreProfiling.Install(null);
            }
        }

        [Test]
        public void HiddenEnemyUnknownEnvironmentDoesNotLeak()
        {
            var input = InputWithDistantMain(new TargetingMethodSpec(TargetingShape.RadialSquare,
                TargetingChoiceMode.Single, TargetingSplash.None, 0));
            var wall = WallStateLifecycleTests.Wall(71, new CellCoord(2, 3));
            var playerOnlyHighGround = new StaticTerrainFact(72, Origin, StaticTerrainKind.HighGround, true,
                new[] { Owner.Player });
            var wallOnly = WallMovementTests.WithWalls(input, wall);
            var hidden = WallMovementTests.WithWalls(input, wall, playerOnlyHighGround);
            var visible = WallMovementTests.WithWalls(input, wall, new StaticTerrainFact(72, Origin,
                StaticTerrainKind.HighGround, true, new[] { Owner.Player, Owner.Enemy }));
            var first = Build(wallOnly).Evaluate(Target(input), Endpoint);
            var second = Build(hidden).Evaluate(Target(input), Endpoint);
            Assert.That(second.EstimatedLossPermille, Is.EqualTo(first.EstimatedLossPermille).And.Zero);
            Assert.That(second.Gaps, Is.EqualTo(first.Gaps));
            Assert.That(second.Gaps.HasFlag(CombatAssessmentGap.HiddenEnvironment), Is.True);
            Assert.That(Build(visible).Evaluate(Target(input), Endpoint).EstimatedLossPermille, Is.Positive);
        }

        [Test]
        public void WallBlocksKnownThreat()
        {
            var input = InputWithDistantMain(new TargetingMethodSpec(TargetingShape.RadialSquare,
                TargetingChoiceMode.Single, TargetingSplash.None, 0));
            var wall = WallStateLifecycleTests.Wall(71, new CellCoord(2, 3));
            Assert.That(Build(input).Evaluate(Target(input), Endpoint).EstimatedLossPermille, Is.Positive);
            Assert.That(Build(WallMovementTests.WithWalls(input, wall)).Evaluate(Target(input), Endpoint)
                .EstimatedLossPermille, Is.Zero);
        }

        [Test]
        public void PotentialThreatIsNotCertainDamage()
        {
            var input = EnemyControllerPhaseTests.Input(CombatCommandTestData.Input(), CombatControllerPhase.Enemy);
            input = CombatCommandTestData.With(input, input.Characters.Select(unit => unit.Owner == Owner.Player
                ? CombatCommandTestData.With(unit, actionUse: UnitActionUse.Attacked,
                    position: unit.EntityId.Value == 10 ? new CellCoord(4, 1) : Origin)
                : unit).ToArray());
            var target = Target(input);
            var exposure = Build(input).Evaluate(target, Endpoint);
            Assert.That(exposure.EstimatedLossPermille, Is.EqualTo(200));
            Assert.That(exposure.Gaps.HasFlag(CombatAssessmentGap.FuturePlayerChoice), Is.True);
            Assert.That(target.Health.CurrentHealth, Is.EqualTo(100));
        }

        [Test]
        public void DisarmedPlayerDoesNotContributeThreat()
        {
            var input = CombatCommandTestData.Input();
            var restriction = new StatusEffectSet(new AppliedStatus[] { new ActionRestrictionStatus(StatusEffectKey.Disarm,
                new EffectOriginKey(EffectOwnerKind.Item, 1, 999, "threat-test"), 2) });
            input = CombatCommandTestData.With(input, input.Characters.Select(unit => unit.Owner == Owner.Player
                ? unit.WithStatusEffects(restriction) : unit).ToArray());
            Assert.That(Build(input).Evaluate(Target(input), Endpoint).EstimatedLossPermille, Is.Zero);
        }

        private static CharacterCombatFacts Target(CombatCommandInput input) =>
            input.Characters.Single(unit => unit.EntityId == new EntityId(2));

        private static CombatCommandInput InputWithDistantMain(TargetingMethodSpec method)
        {
            var input = CombatCommandTestData.Input(method);
            return CombatCommandTestData.With(input, input.Characters.Select(unit => unit.EntityId.Value == 10
                ? CombatCommandTestData.With(unit, appointRange: 1) : unit).ToArray());
        }

        private static EnemyThreatField Build(CombatCommandInput input)
        {
            using var builder = new EnemyThreatField.Builder(new EnemyDecisionContext(input, EnemyDecisionSettings.Default));
            return Finish(builder);
        }

        private static EnemyThreatField Finish(EnemyThreatField.Builder builder)
        {
            int remaining = 100_000;
            while (!builder.IsComplete && remaining-- > 0) builder.Step();
            Assert.That(builder.IsComplete, Is.True);
            return builder.Build();
        }

        private sealed class ProjectionCounter : ICoreProfiler
        {
            internal int AttackProjections { get; private set; }

            public void Begin(CoreProfileSection section)
            {
                if (section == CoreProfileSection.AttackProjectionSetup) AttackProjections++;
            }

            public void End(CoreProfileSection section) { }
        }
    }
}
