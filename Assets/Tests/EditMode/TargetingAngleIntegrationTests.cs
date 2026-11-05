using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Tests.Core
{
    public sealed class TargetingAngleIntegrationTests
    {
        private static readonly IMovementTileRule[] Rules = Array.Empty<IMovementTileRule>();

        [Test]
        public void RangePreviewExecution_RejectTheSameForbiddenPrimary()
        {
            var input = Input(Method(), AttackAim.ForCell(new CellCoord(4, 3)),
                Target(2, 4, 3), Target(3, 4, 4));
            var range = CombatCommandCalculator.GetAttackRange(input);
            Assert.That(range.RangeCells.ToArray(), Has.No.Member(new CellCoord(4, 3)));
            Assert.That(range.RangeCells.ToArray(), Has.Member(new CellCoord(4, 4)));
            Assert.That(range.AttackAims.ToArray(), Is.EqualTo(new[] { AttackAim.ForCell(new CellCoord(4, 4)) }));
            AssertRejected(input, AttackFailureReason.InvalidPrimaryTarget);
        }

        [Test]
        public void BoundarySplash_ActualDamageSurvivesClipping()
        {
            var input = Input(Method(splash: TargetingSplash.Radial), AttackAim.ForCell(new CellCoord(4, 4)),
                Target(2, 4, 4), Target(3, 5, 4));
            var range = CombatCommandCalculator.GetAttackRange(input);
            Assert.That(range.RangeCells.ToArray(), Has.No.Member(new CellCoord(5, 4)));
            var preview = CombatCommandCalculator.Preview(input, Rules);
            var result = CombatCommandCalculator.Calculate(input, Rules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(preview.Targets.ToArray().Select(t => (t.TargetId, t.Role, t.TargetHealthAfter)),
                Is.EqualTo(new[] { (new EntityId(2), AttackTargetRole.Primary, 80),
                    (new EntityId(3), AttackTargetRole.Additional, 86) }));
            AssertParity(input);
            Assert.That(result.Attack.Value.AdditionalCells.ToArray(), Has.Member(new CellCoord(5, 4)));
            Assert.That(input.Characters.All(c => c.Health.CurrentHealth == 100), Is.True);
        }

        [Test]
        public void BoundarySplash_StillStopsAtWallCells()
        {
            var input = Input(Method(splash: TargetingSplash.Radial), AttackAim.ForCell(new CellCoord(4, 4)),
                Target(2, 4, 4));
            var clear = CombatCommandCalculator.Preview(input, Rules);
            Assert.That(clear.AdditionalCells.ToArray(), Has.Member(new CellCoord(5, 4)));
            input = WallMovementTests.WithWalls(input, WallStateLifecycleTests.Wall(71, new CellCoord(5, 4)));
            var preview = CombatCommandCalculator.Preview(input, Rules);
            Assert.That(preview.CanAttack, Is.True);
            Assert.That(preview.AdditionalCells.ToArray(), Has.Member(new CellCoord(5, 4)));
            Assert.That(preview.AffectedCells.ToArray(), Has.No.Member(new CellCoord(5, 4)));
            Assert.That(CombatCommandCalculator.Calculate(input, Rules).Attack.Value.AffectedCells.ToArray(),
                Has.No.Member(new CellCoord(5, 4)));
            AssertParity(input);
        }

        [Test]
        public void ForbiddenDirection_IsAbsentFromAimsAndRejectedOnSubmit()
        {
            var input = Input(Method(TargetingShape.EightDirectionLine, TargetingChoiceMode.Direction),
                AttackAim.ForDirection(AttackDirection.Left), Target(2, 2, 4), Target(3, 3, 4));
            var range = CombatCommandCalculator.GetAttackRange(input);
            Assert.That(range.AttackAims.ToArray(), Is.EqualTo(new[] { AttackAim.ForDirection(AttackDirection.Up) }));
            AssertRejected(input, AttackFailureReason.InvalidAttackDirection);
            AssertParity(CombatAttackQueries.WithAim(input, AttackAim.ForDirection(AttackDirection.Up)));
        }

        [Test]
        public void RestrictedAll_PreservesEmptyFailure()
        {
            var input = Input(Method(choice: TargetingChoiceMode.All), AttackAim.ForAll(), Target(2, 3, 2));
            Assert.That(CombatCommandCalculator.GetAttackRange(input).AttackAims.Length, Is.Zero);
            AssertRejected(input, AttackFailureReason.UnresolvedEmptyAll);
        }

        [Test]
        public void NoFundingGate_KeepsRestrictedGeometry()
        {
            var input = Input(Method(), AttackAim.ForCell(new CellCoord(4, 4)), Target(2, 4, 4));
            var before = CombatCommandCalculator.GetAttackRange(input);
            input = CombatCommandTestData.With(input, state: input.State.With());
            var after = CombatCommandCalculator.GetAttackRange(input);
            Assert.That(after.RangeCells.ToArray(), Is.EqualTo(before.RangeCells.ToArray()));
            Assert.That(after.AttackAims.ToArray(), Is.EqualTo(before.AttackAims.ToArray()));
            AssertParity(input);
        }

        [Test]
        public void BallistaSideAngle_PreservesPiercingAndAdjacentExclusion()
        {
            var method = Method(TargetingShape.EightDirectionLine, angle: TargetingChoiceAngle.FrontAndSides);
            var input = CombatCommandTestData.Input(method, AttackAim.ForCell(new CellCoord(5, 3)),
                empty: true, category: CategoryKey.Cannon);
            var actor = CombatCommandTestData.With(input.Characters[0], position: new CellCoord(3, 3),
                traits: new TraitStatusSnapshot[] { new PiercingFalloffTraitStatusSnapshot(30) });
            input = CombatCommandTestData.With(input,
                characters: new[] { actor, input.Characters[1], Target(2, 5, 3), Target(3, 6, 3) },
                profiles: input.Profiles.Select(p => p.Definition.Key == actor.CharacterKey
                    ? new CombatAttackProfile(CombatCommandTestData.Spec(actor.CharacterKey, 20, CategoryKey.Cannon,
                        new PiercingFalloffTraitSpec(30, 99)), method) : p).ToArray());
            var preview = CombatCommandCalculator.Preview(input, Rules);
            Assert.That(preview.Targets.ToArray().Select(t => t.ExpectedDamage), Is.EqualTo(new[] { 20, 14 }));
            AssertParity(input);
            Assert.That(CombatCommandCalculator.GetAttackRange(input).RangeCells.ToArray(), Has.No.Member(new CellCoord(3, 1)));
            AssertRejected(CombatAttackQueries.WithAim(input, AttackAim.ForCell(new CellCoord(4, 3))),
                AttackFailureReason.AppointCellRejected);
        }

        [TestCase(1, true)]
        [TestCase(3, false)]
        public void Reaction_UsesAttackersOwnerForAdmission(int targetY, bool allowed)
        {
            var input = EnemyMovementPathTests.Input(actorCell: new CellCoord(2, 2), targetCell: new CellCoord(2, targetY));
            input = WithMethod(input, EntityKey.Unit_Demon, Method());
            var result = CombatAttackCalculator.Reaction(input, Rules, input.Characters[0],
                AttackAim.ForCell(new CellCoord(2, targetY)));
            Assert.That(result != null, Is.EqualTo(allowed));
            if (allowed)
            {
                Assert.That(result.Accepted, Is.True, result.Reason);
                Assert.That(CombatCommandComposition.Project(result).Characters.Single(c => c.EntityId == new EntityId(2))
                    .Health.CurrentHealth, Is.EqualTo(90));
            }
        }

        internal static TargetingMethodSpec Method(TargetingShape shape = TargetingShape.RadialSquare,
            TargetingChoiceMode choice = TargetingChoiceMode.Single, TargetingSplash splash = TargetingSplash.None,
            TargetingChoiceAngle angle = TargetingChoiceAngle.Front) =>
            new TargetingMethodSpec(shape, choice, splash, splash == TargetingSplash.None ? 0 : 70, angle);

        internal static CombatCommandInput WithMethod(CombatCommandInput input, EntityKey key, TargetingMethodSpec method) =>
            CombatCommandTestData.With(input, profiles: input.Profiles.Select(p => p.Definition.Key == key
                ? new CombatAttackProfile(p.Definition, method) : p).ToArray());

        private static CombatCommandInput Input(TargetingMethodSpec method, AttackAim aim, params CharacterCombatFacts[] targets)
        {
            var input = CombatCommandTestData.Input(method, aim, empty: true);
            var actor = CombatCommandTestData.Character(1, EntityKey.Unit_Akimbo, new CellCoord(3, 3), Owner.Player, 20);
            return CombatCommandTestData.With(input, new[] { actor, input.Characters[1] }.Concat(targets).ToArray());
        }

        private static CharacterCombatFacts Target(int id, int x, int y) =>
            CombatCommandTestData.Character(id, EntityKey.Unit_Demon, new CellCoord(x, y), Owner.Enemy);

        private static void AssertRejected(CombatCommandInput input, AttackFailureReason expected)
        {
            var preview = CombatCommandCalculator.Preview(input, Rules);
            var result = CombatCommandCalculator.Calculate(input, Rules);
            Assert.That(preview.FailureReason, Is.EqualTo(expected));
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Reason, Is.EqualTo(expected.ToString()));
            Assert.That(result.Characters.Characters, Is.Empty);
            Assert.That(result.Events.Any(e => e.Kind == CombatEventKind.Attacked), Is.False);
            Assert.That(result.Attack.HasValue && result.Attack.Value.Succeeded, Is.False);
        }

        private static void AssertParity(CombatCommandInput input)
        {
            var preview = CombatCommandCalculator.Preview(input, Rules);
            var result = CombatCommandCalculator.Calculate(input, Rules);
            Assert.That(preview.CanAttack, Is.True, preview.FailureReason.ToString());
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(preview.DesignatedCells.ToArray(), Is.EqualTo(result.Attack.Value.DesignatedCells.ToArray()));
            Assert.That(preview.AdditionalCells.ToArray(), Is.EqualTo(result.Attack.Value.AdditionalCells.ToArray()));
            Assert.That(preview.Targets.ToArray().Select(t => (t.TargetId, t.Role, t.TargetHealthAfter)),
                Is.EqualTo(result.Attack.Value.Hits.ToArray().Select(h => (h.TargetId, h.Role, h.TargetHealthAfter))));
        }
    }
}

