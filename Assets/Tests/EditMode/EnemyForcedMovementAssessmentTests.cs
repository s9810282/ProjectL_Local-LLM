using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Tests.Core
{
    /// <summary>
    /// 적 AI 평가의 행위자 강제 이동(넉백 계열·당기기, 사용자 결정 2026-11-10). 강제 이동 자체는 정밀 미리보기가 실제 공격 계산으로 세고,
    /// 밀려날 수 있는 칸의 도착 원본만 따로 본다. 넉백을 가진 적 종류가 공격을 아예 못 하던 문제(묘지기: 아군 전원 피해 상한)의 회귀를 막는다.
    /// 고정물: 행위자(1,3)가 (4,3)을 방사 스플래시로 쳐서 (4,3)·(4,4)·(5,3)의 적을 맞힌다. 넉백 1칸이면 닿는 칸은 그 둘레다.
    /// </summary>
    public sealed class EnemyForcedMovementAssessmentTests
    {
        // 맞는 대상 (5,3)에서 넉백 1칸 안이다. 폭발 칸(둘레 3×3)에 아래 아군 (7,4)가 들어가고, 메인 (7,7)은 들어가지 않는다.
        private static readonly CellCoord MineInReach = new CellCoord(6, 3);
        private static readonly CellCoord MineOutOfReach = new CellCoord(0, 7);
        private static readonly CellCoord AllyNearMine = new CellCoord(7, 4);

        [TestCase("Knockback")]
        [TestCase("ImpactBolt")]
        public void OwnForcedMovementAttackIsAssessedByPreview(string source)
        {
            var input = source == "Knockback" ? Knockback() : ImpactBoltTests.Input();
            var facts = Assess(input, Array.Empty<IMovementTileRule>(), out int previews);
            Assert.That(facts.IsPrecise, Is.True);
            Assert.That(facts.Gaps & ~CombatAssessmentGap.HiddenEnvironment, Is.EqualTo(CombatAssessmentGap.None));
            Assert.That(facts.OpponentHpLossPermille, Is.Positive);
            Assert.That(facts.PossibleAlliedHpLossPermille, Is.EqualTo(facts.AlliedHpLossPermille));
            Assert.That(EnemyUtilityScorer.AttackBenefit(facts), Is.Positive);
            Assert.That(previews, Is.EqualTo(1));
        }

        [Test]
        public void ArrivalSourceOutsideForcedReachDoesNotBlockPreview()
        {
            var input = CliffFallTests.WithEnvironment(WithAlly(Knockback()), Mine(MineOutOfReach));
            var facts = Assess(input, MineEnvironmentTests.Rules, out int previews);
            Assert.That(facts.IsPrecise, Is.True);
            Assert.That(facts.Gaps.HasFlag(CombatAssessmentGap.UnsupportedEffect), Is.False);
            Assert.That(facts.PossibleAlliedHpLossPermille, Is.EqualTo(facts.AlliedHpLossPermille));
            Assert.That(previews, Is.EqualTo(1));
        }

        [Test]
        public void MineInsideForcedReachChargesOnlyBlastAllies()
        {
            var input = CliffFallTests.WithEnvironment(WithAlly(Knockback()), Mine(MineInReach));
            var facts = Assess(input, MineEnvironmentTests.Rules, out int previews);
            Assert.That(facts.IsPrecise, Is.False);
            Assert.That(facts.Gaps.HasFlag(CombatAssessmentGap.UnsupportedEffect), Is.True);
            // 폭발 칸 안의 아군 하나(체력 가득 = 1000)만 센다. 폭발 칸 밖의 메인은 넣지 않는다(이전에는 아군 전원).
            Assert.That(facts.PossibleAlliedHpLossPermille, Is.EqualTo(1000));
            Assert.That(previews, Is.Zero);
        }

        private static CombatCommandInput Knockback() =>
            CombatCommandEffectTests.ActorEffect(new KnockbackTraitSpec(1), new KnockbackTraitStatusSnapshot(1));

        // 행위자와 같은 진영(플레이어) 유닛 하나를 지뢰 옆에 둔다. 넉백 프로필을 가진 키와 겹치지 않게 데몬 키를 쓴다.
        private static CombatCommandInput WithAlly(CombatCommandInput input) => TraitCombatFixtures.Characters(input,
            input.Characters.Append(CombatCommandTestData.Character(20, EntityKey.Unit_Demon, AllyNearMine, Owner.Player)).ToArray());

        private static MineSourceFact Mine(CellCoord cell) => new MineSourceFact(71, cell, 1, 10, true, new[] { Owner.Player });

        private static EnemyScoreFacts Assess(CombatCommandInput input, IReadOnlyList<IMovementTileRule> rules, out int previews)
        {
            var context = new EnemyDecisionContext(input, EnemyDecisionSettings.Default);
            Assert.That(CombatActionAssessment.TryAssess(input, EnemyDecision.Attack(input.ActorId, input.Aim, default), rules, context,
                out var facts, out _), Is.True);
            previews = context.PreviewCount;
            return facts;
        }
    }
}
