using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.PlayerSkills;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.PlayerSkills;

namespace ProjectL.Tests.Core
{
    public sealed class MagicBulletParityTests
    {
        [TestCase(PlayerSkillUpgradeKey.None)]
        [TestCase(PlayerSkillUpgradeKey.Distortion)]
        [TestCase(PlayerSkillUpgradeKey.Judgment)]
        public void RepeatedLocalCalculationsAndCommandExecutionAgreeWithoutInputMutation(PlayerSkillUpgradeKey upgrade)
        {
            var points = upgrade == PlayerSkillUpgradeKey.Distortion
                ? new[] { new CellCoord(5, 3), new CellCoord(5, 4), new CellCoord(4, 4) }
                : new[] { new CellCoord(5, 3), new CellCoord(5, 4) };
            var input = MagicBulletTestData.Input(points, upgrade, targets: new[] { MagicBulletSkillTests.Target(2, 5, 4) });
            var first = PlayerSkillCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(first.Accepted, Is.True, first.Reason);
            var second = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(second.Events.Select(e => e.Kind), Is.EqualTo(first.Events.Select(e => e.Kind)));
            Assert.That(second.Characters.Characters.Select(c => (c.Final.EntityId, c.Final.Health.CurrentHealth, c.Final.Anchor)),
                Is.EqualTo(first.Characters.Characters.Select(c => (c.Final.EntityId, c.Final.Health.CurrentHealth, c.Final.Anchor))));
            Assert.That(input.State.PlayerSkill.RemainingUses, Is.EqualTo(2));
            Assert.That(input.Characters.Single(c => c.EntityId.Value == 2).Health.CurrentHealth, Is.EqualTo(1000));
            CombatHealthEvidenceValidator.Validate(first);
            CombatHealthEvidenceValidator.Validate(second);
        }
    }
}
