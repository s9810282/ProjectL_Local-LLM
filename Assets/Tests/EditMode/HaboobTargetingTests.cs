using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;

namespace ProjectL.Tests.Core
{
    public sealed class HaboobTargetingTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void CrossBoundaryPrimaryIsRejectedIncludingEmptyCell(bool insideActor)
        {
            var source = CombatCommandTestData.Input();
            var cell = insideActor ? source.Characters[0].Anchor.Value : new CellCoord(4, 3);
            var input = WithArea(source, cell);
            var preview = CombatCommandCalculator.Preview(input, Array.Empty<IMovementTileRule>());
            Assert.That(preview.FailureReason.ToString(), Is.EqualTo("HaboobDesignationBlocked"));
            Assert.That(CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>()).Accepted, Is.False);
            var empty = CombatAttackQueries.WithAim(input, AttackAim.ForCell(insideActor ? new CellCoord(2, 3) : cell));
            if (!insideActor) empty = CombatCommandTestData.With(empty, characters: empty.Characters.Where(c => c.EntityId.Value != 2).ToArray());
            Assert.That(CombatCommandCalculator.Preview(empty, Array.Empty<IMovementTileRule>()).FailureReason.ToString(), Is.EqualTo("HaboobDesignationBlocked"));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void SameMembershipAndInterveningCloudAllowPrimary(bool bothInside)
        {
            var source = CombatCommandTestData.Input();
            var input = WithArea(source, bothInside ? new[] { source.Characters[0].Anchor.Value, new CellCoord(4, 3) } : new[] { new CellCoord(2, 3) });
            Assert.That(CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>()).Accepted, Is.True);
        }

        [Test]
        public void ValidPrimarySplashMayHitAcrossCloudBoundary()
        {
            var input = WithArea(CombatCommandTestData.Input(), new CellCoord(4, 4));
            var preview = CombatCommandCalculator.Preview(input, Array.Empty<IMovementTileRule>());
            var result = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.Attack.Value.Hits.ToArray().Select(h => h.TargetId), Does.Contain(new EntityId(3)));
            foreach (var victim in preview.Targets.ToArray())
                Assert.That(CombatCommandComposition.Project(result).Characters.Single(c => c.EntityId == victim.TargetId).Health.CurrentHealth,
                    Is.EqualTo(victim.TargetHealthAfter));
        }

        [TestCase(TargetingChoiceMode.Direction)]
        [TestCase(TargetingChoiceMode.All)]
        public void AiCanChooseLegalIndirectHitButNotForbiddenPrimary(TargetingChoiceMode mode)
        {
            var method = new TargetingMethodSpec(mode == TargetingChoiceMode.Direction ? TargetingShape.EightDirectionLine : TargetingShape.RadialSquare,
                mode, TargetingSplash.None, 0);
            var input = WithArea(CombatCommandTestData.Input(method), new CellCoord(4, 3), new CellCoord(4, 4), new CellCoord(5, 3));
            var range = CombatCommandCalculator.GetAttackRange(input);
            Assert.That(new EnemyAiPolicy(new HostilityPolicy(), Array.Empty<IMovementTileRule>()).TrySelectAttackAim(input, range, out var aim), Is.True);
            var result = CombatCommandCalculator.Calculate(CombatAttackQueries.WithAim(input, aim), Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.Attack.Value.Hits.Length, Is.GreaterThan(0));
            Assert.That(range.RangeCells.ToArray(), Does.Contain(new CellCoord(4, 3)));
            var direct = WithArea(CombatCommandTestData.Input(new TargetingMethodSpec(TargetingShape.RadialSquare,
                TargetingChoiceMode.Single, TargetingSplash.None, 0)), new CellCoord(4, 3), new CellCoord(4, 4), new CellCoord(5, 3));
            Assert.That(new EnemyAiPolicy(new HostilityPolicy(), Array.Empty<IMovementTileRule>()).TrySelectAttackAim(direct,
                CombatCommandCalculator.GetAttackRange(direct), out _), Is.False);
        }

        [Test]
        public void DisplayedPrimaryCellsMatchExecutableAims()
        {
            var input = WithArea(CombatCommandTestData.Input(category: CategoryKey.Cannon), new CellCoord(4, 3));
            var range = CombatCommandCalculator.GetAttackRange(input);
            Assert.That(range.RangeCells.ToArray().Contains(new CellCoord(4, 3)), Is.False);
            Assert.That(CombatCommandCalculator.GetUnitAttackRange(input).RangeCells.ToArray().Contains(new CellCoord(4, 3)), Is.False);
            Assert.That(range.AttackAims.ToArray().All(a => a.Cell != new CellCoord(4, 3)), Is.True);
            Assert.That(range.RangeCells.ToArray(), Is.EquivalentTo(range.AttackAims.ToArray().Select(a => a.Cell.Value)));
            foreach (var aim in range.AttackAims.ToArray())
            {
                var command = CombatAttackQueries.WithAim(input, aim);
                Assert.That(CombatCommandCalculator.Preview(command, Array.Empty<IMovementTileRule>()).CanAttack, Is.True);
                Assert.That(CombatCommandCalculator.Calculate(command, Array.Empty<IMovementTileRule>()).Accepted, Is.True);
            }
        }

        [Test]
        public void EmptyPrimaryThatOtherwiseIsLegalStillCannotCrossCloud()
        {
            var input = CombatCommandTestData.Input(empty: true, category: CategoryKey.Cannon);
            Assert.That(CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>()).Accepted, Is.True);
            var blocked = WithArea(input, input.Aim.Cell.Value);
            Assert.That(CombatCommandCalculator.Preview(blocked, Array.Empty<IMovementTileRule>()).FailureReason.ToString(),
                Is.EqualTo("HaboobDesignationBlocked"));
        }

        internal static CombatCommandInput WithArea(CombatCommandInput input, params CellCoord[] cells) => CombatCommandTestData.With(input,
            state: input.State.With(gimmicks: new StageGimmickStateSet(new StageGimmickState[] {
                new HaboobGimmickState(new StageGimmickId(1), HaboobGimmickPhase.Active, 2, cells) })));
    }
}
