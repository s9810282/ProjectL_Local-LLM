using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Tests.Core
{
    public sealed class EnemyAssessmentVisibilityReuseTests
    {
        private static readonly Owner[] Public = { Owner.Player, Owner.Enemy };
        private static readonly MethodInfo VisibleSteps = typeof(MovementEnvironmentFacts)
            .GetMethod("VisibleSteps", BindingFlags.Instance | BindingFlags.NonPublic);

        [Test]
        public void SameSourceAndOwnerMatchesFreshPublicProjection()
        {
            var context = Context();
            var source = context.Input.State.Environment;
            string before = EnemyActionAssessmentTests.Encode(source);
            var fresh = Fresh(source, Owner.Enemy);
            var initial = Read(context, source, Owner.Enemy);
            var repeated = Read(context, source, Owner.Enemy);

            Assert.That(initial.result.SameValues(fresh.result), Is.True);
            Assert.That(repeated.result, Is.SameAs(initial.result));
            Assert.That(repeated.steps, Is.EqualTo(1).And.LessThan(initial.steps));
            Assert.That(initial.result.Tiles.Select(tile => tile.Id), Is.EqualTo(new long[] { 91, 72, 54 }));
            Assert.That(initial.result.Tiles[0].KnownTo, Is.EqualTo(Public));
            Assert.That(initial.result.Tiles[1].KnownTo, Is.EqualTo(new[] { Owner.Enemy }));
            Assert.That(EnemyActionAssessmentTests.Encode(source), Is.EqualTo(before));
        }

        [Test]
        public void DifferentSourceOrOwnerRebuilds()
        {
            var context = Context();
            var source = context.Input.State.Environment;
            var distinctSource = new MovementEnvironmentFacts(source.Tiles);
            Assert.That(distinctSource.SameValues(source), Is.True);
            var first = Read(context, source, Owner.Enemy).result;
            var newSource = Read(context, distinctSource, Owner.Enemy).result;
            var newObserver = Read(context, distinctSource, Owner.Player).result;
            var originalAgain = Read(context, source, Owner.Enemy).result;

            Assert.That(newSource, Is.Not.SameAs(first));
            Assert.That(newSource.SameValues(first), Is.True);
            Assert.That(newObserver, Is.Not.SameAs(newSource));
            Assert.That(newObserver.SameValues(Fresh(distinctSource, Owner.Player).result), Is.True);
            Assert.That(newObserver.Tiles.Select(tile => tile.Id), Is.EqualTo(new long[] { 91, 63, 54 }));
            Assert.That(newObserver.Tiles[1].KnownTo, Is.EqualTo(new[] { Owner.Player }));
            Assert.That(originalAgain, Is.Not.SameAs(first));
            Assert.That(originalAgain.SameValues(first), Is.True);
        }

        [Test]
        public void PublicAndObserverOnlyProjectionsStayDistinct()
        {
            var context = Context();
            var source = context.Input.State.Environment;
            var observed = Geometry(context);
            var assessment = Read(context, source, Owner.Enemy).result;

            Assert.That(observed.Environment.SameValues(source.VisibleTo(Owner.Enemy)), Is.True);
            Assert.That(assessment.SameValues(Fresh(source, Owner.Enemy).result), Is.True);
            Assert.That(assessment.Tiles.Select(tile => tile.Id), Is.EqualTo(observed.Environment.Tiles.Select(tile => tile.Id)));
            Assert.That(observed.Environment.Tiles[0].KnownTo, Is.EqualTo(new[] { Owner.Enemy }));
            Assert.That(assessment.Tiles[0].KnownTo, Is.EqualTo(Public));
            Assert.That(source.Tiles[0].KnownTo, Is.EqualTo(Public));
            Assert.That(Geometry(context), Is.SameAs(observed));
            Assert.That(Read(context, source, Owner.Enemy).result, Is.SameAs(assessment));
        }

        [Test]
        public void FollowupUsesItsActualEnvironmentWithParentBudgetContext()
        {
            var world = EnemyFollowupEstimateTests.World(targetCell: new CellCoord(5, 2), range: 2);
            var endpoint = new CellCoord(3, 2);
            var input = HighGroundMovementTests.TerrainInput(world,
                new StaticTerrainFact(71, endpoint, StaticTerrainKind.Sandstorm, true, Public));
            var budget = new EnemyDecisionContext(input, new EnemyDecisionSettings(actorPreviewLimit: 2, decisionPreviewLimit: 2));
            var route = new MovementPath(new[] { new CellCoord(0, 2), new CellCoord(1, 2), new CellCoord(2, 2), endpoint });
            string before = EnemyActionAssessmentTests.Encode(input);
            Assert.That(CombatActionAssessment.TryAssess(input, EnemyDecision.Move(input.ActorId, route, default),
                EnemyAiBenchmarkFixtures.Rules, budget, out var facts, out var moved), Is.True);
            Assert.That(facts.IsPrecise, Is.True);
            Assert.That(moved, Is.Not.Null);
            Assert.That(budget.PreviewCount, Is.EqualTo(1));
            Assert.That(EnemyFollowupEstimateTests.Followup(moved, budget, preciseMove: true), Is.Zero);
            Assert.That(budget.PreviewCount, Is.EqualTo(1));

            // 후속 공격 입력은 이동 투영에서 바뀐 환경을 읽되, 원래 결정의 미리보기 예산을 사용한다.
            var changed = HighGroundMovementTests.TerrainInput(moved);
            Assert.That(changed.State.Environment, Is.Not.SameAs(budget.Input.State.Environment));
            Assert.That(changed.State.Environment.Tiles, Is.Empty);
            long fresh = EnemyFollowupEstimateTests.Followup(changed,
                new EnemyDecisionContext(changed, budget.Settings), preciseMove: true);
            Assert.That(fresh, Is.EqualTo(400));
            Assert.That(EnemyFollowupEstimateTests.Followup(changed, budget, preciseMove: true), Is.EqualTo(fresh));
            Assert.That(budget.PreviewCount, Is.EqualTo(2));
            Assert.That(EnemyActionAssessmentTests.Encode(input), Is.EqualTo(before));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void IncompleteVisibilityEnumerationIsNotPublished(bool afterInnerCallback)
        {
            var context = Context();
            var source = context.Input.State.Environment;
            var previous = Read(context, source, Owner.Enemy).result;
            var nextSource = new MovementEnvironmentFacts(source.Tiles);
            var fresh = Fresh(nextSource, Owner.Enemy);
            int stop = afterInnerCallback ? fresh.steps : 1;
            bool completed = false;
            using (var cursor = context.AssessmentVisibilitySteps(nextSource, Owner.Enemy, _ => completed = true).GetEnumerator())
            {
                // 마지막 하위 단계에서는 이미 결과가 전달됐지만 하위 열거가 끝나지 않았다.
                for (int step = 0; step < stop; step++) Assert.That(cursor.MoveNext(), Is.True);
                Assert.That(completed, Is.False);
            }

            Assert.That(Read(context, source, Owner.Enemy).result, Is.SameAs(previous));
            var rebuilt = Read(context, nextSource, Owner.Enemy);
            Assert.That(rebuilt.steps, Is.EqualTo(fresh.steps));
            Assert.That(rebuilt.result.SameValues(fresh.result), Is.True);
            Assert.That(Read(context, nextSource, Owner.Enemy).result, Is.SameAs(rebuilt.result));
        }

        [Test]
        public void FaultedProjectionKeepsLastCompletedEntry()
        {
            var context = Context();
            var source = context.Input.State.Environment;
            var previous = Read(context, source, Owner.Enemy).result;
            Assert.Throws<ArgumentOutOfRangeException>(() => Read(context, source, Owner.Neutral));
            Assert.That(Read(context, source, Owner.Enemy).result, Is.SameAs(previous));
        }

        private static EnemyDecisionContext Context()
        {
            var input = HighGroundMovementTests.TerrainInput(EnemyFollowupEstimateTests.World(),
                new StaticTerrainFact(91, new CellCoord(1, 0), StaticTerrainKind.HighGround, true, Public),
                new StaticTerrainFact(72, new CellCoord(2, 0), StaticTerrainKind.Sandstorm, true, new[] { Owner.Enemy }),
                new StaticTerrainFact(63, new CellCoord(3, 0), StaticTerrainKind.HighGround, true, new[] { Owner.Player }),
                new StaticTerrainFact(54, new CellCoord(4, 0), StaticTerrainKind.Sandstorm, false, Public));
            return new EnemyDecisionContext(input, EnemyDecisionSettings.Default);
        }

        private static (MovementEnvironmentFacts result, int steps) Fresh(MovementEnvironmentFacts source, Owner observer)
        {
            MovementEnvironmentFacts result = null;
            var callback = (Action<MovementEnvironmentFacts>)(value => result = value);
            // Contracts의 내부 열거를 직접 기준으로 삼아 완료 콜백 이후의 중단 지점을 확인한다.
            var phases = ((IEnumerable<string>)VisibleSteps.Invoke(source, new object[] { observer, callback, true })).ToArray();
            Assert.That(phases.Last(), Is.EqualTo("EnvironmentVisibleComplete"));
            return (result, phases.Length);
        }

        private static (MovementEnvironmentFacts result, int steps) Read(
            EnemyDecisionContext context, MovementEnvironmentFacts source, Owner observer)
        {
            MovementEnvironmentFacts result = null;
            int steps = context.AssessmentVisibilitySteps(source, observer, value => result = value).Count();
            return (result, steps);
        }

        private static EnemyDecisionContext.ObservedGeometry Geometry(EnemyDecisionContext context)
        {
            EnemyDecisionContext.ObservedGeometry geometry = null;
            foreach (var _ in context.GeometrySteps(value => geometry = value)) { }
            return geometry;
        }
    }
}
