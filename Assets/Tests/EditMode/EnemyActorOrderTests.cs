using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Tests.Core
{
    /// <summary>
    /// 행위자 순서(설계 6.7을 바꾼 결정 2026-11-10, 계획 R-4): 지금 칸에서 칠 수 있는 행위자 먼저, 같으면 개체 번호. 필드 비용은 보지 않는다.
    /// </summary>
    public sealed class EnemyActorOrderTests
    {
        private static readonly IMovementTileRule[] Rules = EnemyAiBenchmarkFixtures.Rules;

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(31)]
        public void ReadinessFirstOrder(int partition)
        {
            // 11 (0,2), 12 (0,0), 14 (0,4)는 지금 칠 수 없다. 13 (5,2)은 메인 (4,2) 옆이라 지금 칠 수 있다.
            var input = EnemyExchangeModelTests.Arena(allies: new[] { (12, new CellCoord(0, 0)), (13, new CellCoord(5, 2)), (14, new CellCoord(0, 4)) });
            Assert.That(Order(input), Is.EqualTo(new[] { 13, 11, 12, 14 }));

            // 옛 순서(개체 번호)라면 11의 이동이 첫 결정이다. 지금은 칠 수 있는 13이 먼저 친다.
            using var session = new EnemyDecisionSession(input, Rules, EnemyDecisionSettings.Default);
            EnemyUtilityDecisionTests.Drain(session, partition);
            Assert.That(session.Decision.Kind, Is.EqualTo(EnemyDecisionKind.Attack));
            Assert.That(session.Decision.ActorId, Is.EqualTo(new EntityId(13)));
        }

        [Test]
        public void BlockedBackUnitWaitsForFrontUnit()
        {
            // 한 칸 통로: 뒤 11 (0,0), 앞 12 (1,0), 메인 (6,0). 유닛은 서로 지나가지 못한다. 둘 다 지금 칠 수 없어 순서는 번호지만,
            // 막힌 뒤 유닛은 할 수 있는 것이 없어 대기하고 앞 유닛이 먼저 움직인 뒤 뒤 유닛이 따라간다.
            var input = EnemyExchangeModelTests.Arena(width: 7, height: 1, actor: new CellCoord(0, 0), target: new CellCoord(6, 0),
                allies: new[] { (12, new CellCoord(1, 0)) });
            Assert.That(Order(input), Is.EqualTo(new[] { 11, 12 }));
            var moved = new List<int>();
            var current = input;
            for (int decision = 0; decision < 8; decision++)
            {
                var transition = CombatCommandCalculator.AdvanceUtility(current, Rules);
                Assert.That(transition.Accepted, Is.True, transition.Reason);
                if (transition.PathMovement != null) moved.Add(transition.ActionUseSettlement.ActorId.Value);
                current = CombatCommandComposition.Project(transition);
                if (transition.Outcome.IsStageEnded || current.State.Turn.ControllerPhase != CombatControllerPhase.Enemy) break;
            }
            Assert.That(moved.Take(2), Is.EqualTo(new[] { 12, 11 }), "앞 유닛이 먼저, 뒤 유닛이 이어서 움직인다");
        }

        private static int[] Order(CombatCommandInput input)
        {
            var context = new EnemyDecisionContext(input, EnemyDecisionSettings.Default);
            foreach (var _ in context.IndexSteps()) { }
            var hazards = EnemyActionCandidateTests.MakeForecast(input);
            var pending = context.Actors.Values.Select(actor => new EnemyActionCandidates(context, actor, hazards)).ToList();
            try
            {
                foreach (var candidates in pending)
                    for (int i = 0; i < 4000000 && !candidates.IsComplete && !candidates.OrderReady; i++) candidates.Step();
                Assert.That(pending.All(candidates => candidates.OrderReady), Is.True);
                return pending.OrderBy(candidates => candidates.OrderKey).Select(candidates => candidates.Actor.EntityId.Value).ToArray();
            }
            finally
            {
                foreach (var candidates in pending) candidates.Dispose();
            }
        }
    }
}
