using System;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Categories;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Categories.Definitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Categories.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Categories.States;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Contexts;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Evaluations;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Transitions;

namespace ProjectL.Tests.Core
{
    /// <summary>
    /// Task 7: Heavy의 전방 확장 보호 geometry, 독립 방어 Facing, 피격 경감 기여를 검증한다.
    /// 피해 단계 통합 순서는 Task 10b에서 함께 확인한다.
    /// </summary>
    public sealed class HeavyDirectionalArmorTests
    {
        private static readonly EntityId Unit = new EntityId(1);

        private static EffectOriginKey HeavyOrigin =>
            CategoryEffectOrigin.Create(Unit, CategoryKey.Heavy, CategoryRuleKey.DirectionalArmor);

        // 폭 2d-1: d=1은 폭 1, d=2는 폭 3, d=3은 폭 5.
        // R0(+Y) 기준 보호 셀
        [TestCase(0, 1, true)]   // d=1 중앙
        [TestCase(-1, 1, false)] // d=1의 옆은 제외
        [TestCase(1, 1, false)]
        [TestCase(-1, 2, true)]  // d=2 폭 3
        [TestCase(0, 2, true)]
        [TestCase(1, 2, true)]
        [TestCase(-2, 2, false)] // 같은 깊이의 바깥 한 칸은 제외
        [TestCase(-2, 3, true)]  // d=3 폭 5
        [TestCase(2, 3, true)]
        [TestCase(-3, 3, false)]
        [TestCase(0, 0, false)]  // 원점 제외
        [TestCase(1, 0, false)]  // 옆 제외
        [TestCase(0, -1, false)] // 뒤 제외
        public void Coverage_AtR0_MatchesWideningFan(int localX, int localY, bool expected)
        {
            var defenderAnchor = new CellCoord(5, 5);
            var origin = new CellCoord(5 + localX, 5 + localY);

            bool result = DirectionalArmorCoverageCalculator.IsProtected(
                defenderAnchor, Rotation4.R0, origin);

            Assert.That(result, Is.EqualTo(expected));
        }

        // 같은 식이 네 Facing 모두에 적용된다. 각 Facing의 정면 d=1 셀만 확인한다.
        [TestCase(Rotation4.R0, 0, 1)]
        [TestCase(Rotation4.R90, 1, 0)]
        [TestCase(Rotation4.R180, 0, -1)]
        [TestCase(Rotation4.R270, -1, 0)]
        public void Coverage_AppliesSameFormulaToAllFacings(Rotation4 facing, int dx, int dy)
        {
            var defenderAnchor = new CellCoord(5, 5);

            Assert.That(
                DirectionalArmorCoverageCalculator.IsProtected(
                    defenderAnchor, facing, new CellCoord(5 + dx, 5 + dy)),
                Is.True);

            // 반대편은 보호되지 않는다.
            Assert.That(
                DirectionalArmorCoverageCalculator.IsProtected(
                    defenderAnchor, facing, new CellCoord(5 - dx, 5 - dy)),
                Is.False);
        }

        // [사용자 확정] 경감률 50%. 피격 5는 경감 2(floor), 최종 3.
        [TestCase(5, 50, 2)]
        [TestCase(0, 50, 0)]
        [TestCase(1, 50, 0)]
        [TestCase(2, 50, 1)]
        [TestCase(5, 100, 5)]
        [TestCase(5, 1, 0)]
        public void IncomingRule_ReducesByConfiguredPercentWithFloor(
            int incomingDamage, int reductionPercent, int expectedReduction)
        {
            var rule = CreateRule(reductionPercent);
            var input = new EffectRuleInput(
                HeavyOrigin, new DirectionalArmorEffectStateSnapshot(Rotation4.R0, false));

            EffectIncomingDamageContribution contribution = rule.EvaluateIncomingDamage(
                input,
                new IncomingDamageEffectContext(
                    incomingDamage,
                    defenderAnchor: new CellCoord(5, 5),
                    defenseFacing: Rotation4.R0,
                    impactOrigin: new CellCoord(5, 6)));

            Assert.That(contribution.Origin, Is.EqualTo(HeavyOrigin));
            Assert.That(contribution.FlatReduction, Is.EqualTo(expectedReduction));
        }

        // 원점이 보호 영역 밖이거나 없으면 경감하지 않는다.
        [Test]
        public void IncomingRule_NoReductionWhenOriginIsOutsideOrAbsent()
        {
            var rule = CreateRule(50);
            var input = new EffectRuleInput(
                HeavyOrigin, new DirectionalArmorEffectStateSnapshot(Rotation4.R0, false));
            var anchor = new CellCoord(5, 5);

            // 뒤쪽에서 온 피해
            Assert.That(
                rule.EvaluateIncomingDamage(input,
                    new IncomingDamageEffectContext(5, anchor, Rotation4.R0, new CellCoord(5, 4)))
                    .FlatReduction,
                Is.Zero);

            // 원점 없음
            Assert.That(
                rule.EvaluateIncomingDamage(input,
                    new IncomingDamageEffectContext(5, anchor, Rotation4.R0, null)).FlatReduction,
                Is.Zero);

            // 원점이 방어자 자신과 같음
            Assert.That(
                rule.EvaluateIncomingDamage(input,
                    new IncomingDamageEffectContext(5, anchor, Rotation4.R0, anchor)).FlatReduction,
                Is.Zero);
        }

        // [사용자 확정] 이동 방향이 Facing보다 우선한다. 이동했으면 이동 방향을 쓴다.
        [Test]
        public void Facing_MovementDirectionWins()
        {
            var rule = CreateRule(50);
            var input = new EffectRuleInput(
                HeavyOrigin, new DirectionalArmorEffectStateSnapshot(Rotation4.R0, false));

            EffectRuleResult result = rule.PlanMoveSucceeded(
                input, new MoveSucceededEffectContext(2, true, 2, Rotation4.R90));

            Assert.That(result.Change, Is.Not.Null);
            var finalState = (DirectionalArmorEffectStateSnapshot)result.Change.Value.FinalState;
            Assert.That(finalState.DefenseFacing, Is.EqualTo(Rotation4.R90));
            Assert.That(finalState.MovedThisTurn, Is.True);
        }

        // [사용자 확정] 이동한 턴의 공격은 이동 Facing을 보존하고, 무이동 공격은 공격 방향을 채택한다.
        [TestCase(true, Rotation4.R0)]   // 이동했으면 기존 Facing(R0) 유지
        [TestCase(false, Rotation4.R180)] // 무이동이면 공격 방향(R180) 채택
        public void Facing_AttackUpdatesOnlyWhenUnitDidNotMove(bool movedThisTurn, Rotation4 expected)
        {
            var rule = CreateRule(50);
            var input = new EffectRuleInput(
                HeavyOrigin, new DirectionalArmorEffectStateSnapshot(Rotation4.R0, movedThisTurn));

            EffectRuleResult result = rule.PlanAttackFacingUpdate(input, Rotation4.R180);

            Rotation4 actual = result.Change.HasValue
                ? ((DirectionalArmorEffectStateSnapshot)result.Change.Value.FinalState).DefenseFacing
                : Rotation4.R0;
            Assert.That(actual, Is.EqualTo(expected));
        }

        // 턴 시작은 Facing을 유지하고 MovedThisTurn만 초기화한다.
        [Test]
        public void TurnStart_KeepsFacingAndClearsMovedFlag()
        {
            var rule = CreateRule(50);
            var input = new EffectRuleInput(
                HeavyOrigin, new DirectionalArmorEffectStateSnapshot(Rotation4.R90, true));

            EffectRuleResult result = rule.PlanTurnStarted(input);

            Assert.That(result.Change, Is.Not.Null);
            var finalState = (DirectionalArmorEffectStateSnapshot)result.Change.Value.FinalState;
            Assert.That(finalState.DefenseFacing, Is.EqualTo(Rotation4.R90), "Facing은 유지된다.");
            Assert.That(finalState.MovedThisTurn, Is.False);
        }

        [Test]
        public void NegativeReductionContribution_IsRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new EffectIncomingDamageContribution(HeavyOrigin, -1));
        }

        private static DirectionalArmorCategoryRule CreateRule(int reductionPercent)
        {
            return new DirectionalArmorCategoryRule(
                new DirectionalArmorCategoryRuleDefinition(reductionPercent, Rotation4.R0));
        }
    }
}
