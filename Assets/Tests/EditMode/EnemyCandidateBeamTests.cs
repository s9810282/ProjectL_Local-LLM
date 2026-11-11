using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Tests.Core
{
    /// <summary>
    /// 이동 빔 선택(설계 6.6, 계획 R-4): 저가 점수, 예약(도발 경로 / 위험 피하기 → 최선 공격 위치 → 최선 진척), K·R 한도.
    /// 기본 판은 <see cref="EnemyExchangeModelTests.Arena"/>: 적 (0,2), 메인 (4,2) 사거리 3. 메인 옆 공격 위치는 위협을 받는다.
    /// </summary>
    public sealed class EnemyCandidateBeamTests
    {
        private static readonly IMovementTileRule[] Rules = EnemyAiBenchmarkFixtures.Rules;
        private static readonly CellCoord Origin = new CellCoord(0, 2);
        private static readonly CellCoord AttackCell = new CellCoord(3, 2);

        [TestCase(4)]
        [TestCase(1)]
        public void AttackPositionSurvivesPruning(int limit)
        {
            // 위협이 없는 칸이 많아도(이동 5) 위협을 받는 공격 위치가 빔에 남는다. 옛 순위(위협 먼저)는 K = 4에서 모두 잘랐다.
            var input = EnemyExchangeModelTests.Arena(moveRange: 5);
            using var candidates = Candidates(input, Settings(limit));
            var field = candidates.Field;
            Assert.That(candidates.Moves.Count, Is.EqualTo(limit));
            Assert.That(candidates.Moves.Any(move => field.IsAttackPosition(move.Path.Endpoint)), Is.True);
            Assert.That(field.IsAttackPosition(candidates.Reserved[0]), Is.True, "위험 피하기가 없으면 첫 예약은 최선 공격 위치");
        }

        [Test]
        public void ReservationsCountInsideBeam()
        {
            // 제자리와 공격 위치 (3,2)가 낙석 칸이다: 위험 피하기 → 최선 공격 위치(= 최선 진척, 한 자리) 순서로 예약한다.
            var input = EnemyExchangeModelTests.Arena(rockfall: new[] { Origin, AttackCell });
            using (var candidates = Candidates(input, Settings(3)))
            {
                var reserved = candidates.Reserved;
                Assert.That(reserved.Count, Is.EqualTo(2), string.Join(" ", reserved));
                Assert.That(reserved[0], Is.Not.EqualTo(AttackCell).And.Not.EqualTo(Origin), "위험 피하기");
                Assert.That(reserved[1], Is.EqualTo(AttackCell), "최선 공격 위치");
                Assert.That(candidates.Moves.Count, Is.EqualTo(3));
                Assert.That(candidates.Moves.Select(move => move.Path.Endpoint), Is.SupersetOf(reserved));
            }

            // 공격 위치에 닿지 못하면 최선 진척 칸을 예약한다(대상이 멀다).
            var far = EnemyExchangeModelTests.Arena(width: 12, target: new CellCoord(11, 2));
            using (var candidates = Candidates(far, Settings(3)))
            {
                Assert.That(candidates.Reserved, Is.EqualTo(new[] { new CellCoord(3, 2) }));
                Assert.That(candidates.Moves.Select(move => move.Path.Endpoint), Does.Contain(new CellCoord(3, 2)));
            }

            // 도발 의무: 빔에는 도발 경로만 있고, 그 자리는 예약으로 센다.
            using (var candidates = Candidates(Taunted(), Settings(3)))
            {
                Assert.That(candidates.RequiredTauntSource.IsEmpty, Is.False);
                Assert.That(candidates.Moves.Count, Is.EqualTo(1));
                Assert.That(candidates.Moves[0].TauntSourceId, Is.EqualTo(candidates.RequiredTauntSource));
                Assert.That(candidates.Reserved, Is.EqualTo(new[] { candidates.Moves[0].Path.Endpoint }));
            }
        }

        [Test]
        public void ReservationOverflowFollowsPriority()
        {
            var input = EnemyExchangeModelTests.Arena(rockfall: new[] { Origin, AttackCell });
            CellCoord escape;
            using (var one = Candidates(input, Settings(1)))
            {
                Assert.That(one.Moves.Count, Is.EqualTo(1));
                escape = one.Moves[0].Path.Endpoint;
                Assert.That(one.Reserved, Is.EqualTo(new[] { escape }), "K = 1이면 가장 높은 우선순위(위험 피하기)만 남는다");
                Assert.That(escape, Is.Not.EqualTo(AttackCell));
            }
            using (var two = Candidates(input, Settings(2)))
            {
                Assert.That(two.Reserved, Is.EqualTo(new[] { escape, AttackCell }));
                Assert.That(two.Moves.Select(move => move.Path.Endpoint), Is.EquivalentTo(new[] { escape, AttackCell }));
            }
            using (var six = Candidates(input, EnemyDecisionSettings.Default))
            {
                // 공격 위치가 최선 진척이기도 하다: 두 번 예약된 칸은 한 자리다.
                Assert.That(six.Reserved, Is.EqualTo(new[] { escape, AttackCell }));
                Assert.That(six.Moves.Select(move => move.Path.Endpoint).Distinct().Count(), Is.EqualTo(six.Moves.Count));
            }
            using (var taunted = Candidates(Taunted(), Settings(1)))
            {
                // 의무가 있으면 도발 경로 자리는 내주지 않고 다른 예약은 더하지 않는다.
                Assert.That(taunted.Moves.Single().TauntSourceId, Is.EqualTo(taunted.RequiredTauntSource));
                Assert.That(taunted.Reserved.Count, Is.EqualTo(1));
            }
        }

        [Test]
        public void LimitsHonoured()
        {
            Assert.That(EnemyDecisionSettings.Default.MoveCandidateLimit, Is.EqualTo(6));
            Assert.That(EnemyDecisionSettings.Default.RefinedMoveLimit, Is.EqualTo(2));
            var input = EnemyExchangeModelTests.Arena(moveRange: 5);
            foreach (int limit in new[] { 3, 6, 8 })
            {
                using var candidates = Candidates(input, limit == 6 ? EnemyDecisionSettings.Default : Settings(limit));
                Assert.That(candidates.Moves.Count, Is.EqualTo(limit), $"K = {limit}(옛 상한 4 없음)");
            }
            foreach (int refined in new[] { 1, 2, 3 })
            {
                var diagnostics = new EnemyDecisionDiagnostics();
                using var session = new EnemyDecisionSession(input, Rules, new EnemyDecisionSettings(moveCandidateLimit: 6,
                    refinedMoveLimit: refined), diagnostics);
                EnemyUtilityDecisionTests.Drain(session);
                Assert.That(diagnostics.RefinedMoves, Is.EqualTo(refined), $"R = {refined}(옛 상한 2 없음)");
            }
        }

        [Test]
        public void PruningUsesScorer()
        {
            // 예약이 아닌 자리는 저가 점수 순서(같으면 걸음 수·y·x)로 채운다. 열린 판이라 걸음 수 = 맨해튼 거리다.
            var input = EnemyExchangeModelTests.Arena(moveRange: 5);
            using var candidates = Candidates(input, EnemyDecisionSettings.Default);
            var reserved = new HashSet<CellCoord>(candidates.Reserved);
            var expected = candidates.CheapScores.Where(entry => !reserved.Contains(entry.cell))
                .OrderByDescending(entry => entry.score).ThenBy(entry => Manhattan(entry.cell, Origin))
                .ThenBy(entry => entry.cell.Y).ThenBy(entry => entry.cell.X)
                .Take(candidates.Moves.Count - reserved.Count).Select(entry => entry.cell).ToArray();
            var filled = candidates.Moves.Select(move => move.Path.Endpoint).Where(cell => !reserved.Contains(cell)).ToArray();
            Assert.That(filled, Is.EquivalentTo(expected));
            // 빔 안 순서도 저가 점수 순서다(결정 세션이 후속 공격을 더해 다시 줄 세운다).
            var score = candidates.CheapScores.ToDictionary(entry => entry.cell, entry => entry.score);
            var ordered = candidates.Moves.Select(move => score[move.Path.Endpoint]).ToArray();
            Assert.That(ordered, Is.Ordered.Descending);
            Assert.That(EnemyDecisionSession.HoldReason(false, false, true, 0, candidates.Field, Origin, new List<EnemyDecisionSession.Option>()),
                Is.EqualTo(EnemyHoldReason.Pruned));
        }

        private static EnemyDecisionSettings Settings(int moveLimit) =>
            new EnemyDecisionSettings(moveCandidateLimit: moveLimit, refinedMoveLimit: Math.Min(2, moveLimit));

        private static EnemyActionCandidates Candidates(CombatCommandInput input, EnemyDecisionSettings settings)
        {
            var context = new EnemyDecisionContext(input, settings);
            foreach (var _ in context.IndexSteps()) { }
            var candidates = new EnemyActionCandidates(context, context.Units[input.ActorId], EnemyActionCandidateTests.MakeForecast(input));
            for (int i = 0; i < 4000000 && !candidates.IsComplete; i++) candidates.Step();
            Assert.That(candidates.IsComplete, Is.True);
            return candidates;
        }

        // 도발하는 카우보이가 (6,2)에 있다. 지금은 칠 수 없어 도발 원본으로 가는 경로가 의무다.
        private static CombatCommandInput Taunted()
        {
            var input = EnemyEngagementFieldTests.WithUnit(EnemyExchangeModelTests.Arena(),
                CombatCommandTestData.Character(2, EntityKey.Unit_Cowboy, new CellCoord(6, 2), Owner.Player));
            return TraitCombatFixtures.Equip(input, EntityKey.Unit_Cowboy, new TauntTraitSpec(8));
        }

        private static int Manhattan(CellCoord a, CellCoord b) => Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
    }
}
