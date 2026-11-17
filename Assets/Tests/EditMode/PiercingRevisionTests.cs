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
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;

namespace ProjectL.Tests.Core
{
    public sealed class PiercingRevisionTests
    {
        [TestCase(100, 4)]
        [TestCase(1, 3)]
        public void AlliedAndEnemyHitsShareFalloffAndCapPastAim(int power, int count)
        {
            var input = Arrange(4, power, Ally(3, 2), PiercingShotTestData.Character(4, 4),
                Ally(5, 5), PiercingShotTestData.Character(6, 6), PiercingShotTestData.Character(7, 7));

            var result = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            var preview = CombatCommandCalculator.Preview(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(preview.CanAttack, Is.True);
            var attack = result.Attack.Value;
            Assert.That(attack.PrimaryTargetId.Value, Is.EqualTo(4));
            Assert.That(attack.PiercingShot.StopReason, Is.EqualTo(PiercingShotStopReason.TargetBudget));
            var encounters = attack.PiercingShot.Encounters.ToArray();
            Assert.That(encounters.Select(e => e.TargetId.Value), Is.EqualTo(new[] { 3, 4, 5, 6 }.Take(count)));
            Assert.That(preview.PiercingShot.Encounters.ToArray().Select(e => e.TargetId.Value),
                Is.EqualTo(encounters.Select(e => e.TargetId.Value)));
            Assert.That(encounters.Select(e => e.RemainingPercent), Is.EqualTo(new[] { 100, 75, 50, 25 }.Take(count)));
            Assert.That(attack.Hits.ToArray().Select(h => h.DamageDealt),
                Is.EqualTo(power == 100 ? new[] { 100, 75, 50, 25 } : new[] { 1, 1, 1 }));
        }

        [Test]
        public void BudgetCanBeExhaustedBeforeSelectedEnemy()
        {
            var input = Arrange(6, 40, Ally(3, 2), PiercingShotTestData.Character(4, 3),
                Ally(5, 4), PiercingShotTestData.Character(6, 5), PiercingShotTestData.Character(7, 6));
            var result = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.Attack.Value.PrimaryTargetId.Value, Is.EqualTo(7));
            Assert.That(result.Attack.Value.Hits.ToArray().Select(h => h.TargetId.Value), Is.EqualTo(new[] { 3, 4, 5, 6 }));
            Assert.That(result.Attack.Value.Hits.ToArray().All(h => h.Role == AttackTargetRole.Effect), Is.True);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void FriendlyAndEmptyCellsRemainInvalidAims(bool friendly)
        {
            var input = Arrange(4, 100, Ally(3, 2), PiercingShotTestData.Character(4, 6));
            var aim = AttackAim.ForCell(new CellCoord(friendly ? 2 : 4, 3));
            Assert.That(CombatCommandCalculator.GetAttackRange(input).AttackAims.ToArray(), Has.No.Member(aim));
            Assert.That(CombatCommandCalculator.Calculate(CombatAttackQueries.WithAim(input, aim),
                Array.Empty<IMovementTileRule>()).Accepted, Is.False);
        }

        private static CombatCommandInput Arrange(int aimX, int power, params CharacterCombatFacts[] targets)
        {
            var input = PiercingShotTestData.Input(damage: power, appointRange: aimX - 1);
            var profiles = input.Profiles.ToArray();
            profiles[0] = new CombatAttackProfile(CombatCommandTestData.Spec(EntityKey.Unit_Akimbo, power, CategoryKey.Main,
                new PiercingFalloffTraitSpec(25, 4)), profiles[0].Method);
            return CombatAttackQueries.WithAim(CombatCommandTestData.With(input, profiles: profiles,
                characters: input.Characters.Take(2).Concat(targets).ToArray()), AttackAim.ForCell(new CellCoord(aimX, 3)));
        }

        private static CharacterCombatFacts Ally(int id, int x) =>
            CombatCommandTestData.Character(id, EntityKey.Unit_Demon, new CellCoord(x, 3), Owner.Player);
    }
}
