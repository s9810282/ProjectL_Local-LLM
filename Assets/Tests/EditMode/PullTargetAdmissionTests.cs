using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Tests.Core
{
    public sealed class PullTargetAdmissionTests
    {
        [TestCase(1, false)]
        [TestCase(2, true)]
        public void FriendlyTarget_RequiresActualMovement(int distance, bool accepted)
        {
            var input = PullTestData.Input(distance: distance, owner: Owner.Player);
            Assert.That(CombatCommandCalculator.GetAttackRange(input).AttackAims.ToArray().Contains(input.Aim), Is.EqualTo(accepted));
        }

        [Test]
        public void DiagonalAndSelf_AreRejected()
        {
            var diagonal = PullTestData.Input(ray: new CellCoord(1, 1));
            var self = CombatAttackQueries.WithAim(diagonal,
                ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.AttackAim.ForCell(PullTestData.Origin));
            foreach (var input in new[] { diagonal, self })
                Assert.That(CombatAttackQueries.Selection(input, input.Characters[0], input.Profiles.Last()).FailureReason,
                    Is.Not.EqualTo(AttackFailureReason.None));
        }
    }
}
