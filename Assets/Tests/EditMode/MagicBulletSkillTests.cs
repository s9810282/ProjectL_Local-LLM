using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.PlayerSkills;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Tests.Core
{
    public sealed class MagicBulletSkillTests
    {
        [Test]
        public void SameMultiCellStructureAcrossDifferentSegmentsConsumesOnlyOneEncounter()
        {
            var anchor = new CellCoord(4, 3);
            var offsets = new[] { CellCoord.Zero, new CellCoord(0, 2) };
            var structure = new StructureCombatFacts(new EntityId(51),
                new StructureSpec(EntityKey.Structure_Headquarters, 1, 3, offsets, 1000, false, 0),
                new HealthStateSnapshot(1000, 1000), anchor, Rotation4.R0, offsets.Select(c => anchor + c).ToArray(), Owner.Enemy);
            var input = MagicBulletTestData.Input(new[] { new CellCoord(5, 3), new CellCoord(5, 5), new CellCoord(4, 5) },
                PlayerSkillUpgradeKey.Distortion, targets: new[] { Target(2, 3, 5) }, structures: new[] { structure }, cap: 2);
            var attack = PowderBlossomFixture.Attacks(PowderBlossomFixture.Calculate(input)).Single();
            Assert.That(attack.Hits.Single(h => h.TargetId.Value == 51).Contacts.Count, Is.EqualTo(1));
            Assert.That(attack.DamageByRecipient[new EntityId(2)].DamageBeforeMatchup, Is.EqualTo(75));
            Assert.That(attack.ProjectilePath.Last(), Is.EqualTo(new CellCoord(3, 5)));
            Assert.That(attack.ImpactCells[0], Is.EqualTo(new CellCoord(4, 3)));
            Assert.That(attack.ImpactCells[1], Is.EqualTo(new CellCoord(3, 5)));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void FalloffAndBudgetContinueAcrossBendsAndSurvivingStructure(bool distortion)
        {
            var points = distortion ? new[] { new CellCoord(3, 1), new CellCoord(3, 3), new CellCoord(4, 3) }
                : new[] { new CellCoord(3, 1), new CellCoord(3, 3) };
            var fourth = distortion ? new CellCoord(4, 3) : new CellCoord(3, 4);
            var fifth = distortion ? new CellCoord(5, 3) : new CellCoord(3, 5);
            var structure = PowderBlossomFixture.Structure(51, EntityKey.Structure_Headquarters, 3, 1, 1000, Owner.Enemy, width: 2);
            var input = MagicBulletTestData.Input(points, distortion ? PlayerSkillUpgradeKey.Distortion : PlayerSkillUpgradeKey.None,
                center: new CellCoord(1, 1), targets: new[] { Target(2, 2, 1, Owner.Player), Target(3, 3, 2),
                    Target(4, fourth.X, fourth.Y), Target(5, fifth.X, fifth.Y) }, structures: new[] { structure });
            var result = PowderBlossomFixture.Calculate(input);
            var attack = PowderBlossomFixture.Attacks(result).Single();
            Assert.That(new[] { 2, 51, 3, 4 }.Select(id => attack.DamageByRecipient[new EntityId(id)].DamageBeforeMatchup),
                Is.EqualTo(new[] { 100, 75, 50, 25 }));
            Assert.That(attack.Hits.Select(h => h.TargetId.Value), Has.No.Member(5));
            Assert.That(attack.ProjectilePath.Last(), Is.EqualTo(fourth));
            Assert.That(attack.Hits.Single(h => h.TargetId.Value == 51).Contacts.Count, Is.EqualTo(1));
            Assert.That(attack.IncomingDirections[2], Is.EqualTo(new CellCoord(0, 1)));
            Assert.That(result.FinalState.PlayerSkill.RemainingUses, Is.EqualTo(1));
            Assert.That(result.ActionUseSettlement, Is.Null);
        }

        [TestCase(1, 25, 99, 3)]
        [TestCase(100, 25, 2, 2)]
        public void ExistingRoundedZeroAndConfiguredBudgetStopTheProjectile(int damage, int reduction, int cap, int hits)
        {
            var input = MagicBulletTestData.Input(new[] { new CellCoord(3, 1), new CellCoord(3, 2) },
                center: new CellCoord(1, 1), attack: damage, reduction: reduction, cap: cap,
                targets: new[] { Target(2, 2, 1), Target(3, 3, 1), Target(4, 3, 2), Target(5, 3, 3) });
            Assert.That(PowderBlossomFixture.Attacks(PowderBlossomFixture.Calculate(input)).Single().Hits.Count, Is.EqualTo(hits));
        }

        [Test]
        public void ExplosionKillsLaterTargetBeforeItsEncounterWithoutSpendingItsBudget()
        {
            var barrel = PowderBlossomFixture.Structure(51, EntityKey.Structure_PowderBarrel, 3, 1, 1, Owner.Neutral);
            var fragile = Target(2, 3, 2).WithHealth(new HealthStateSnapshot(5, 1000));
            var input = MagicBulletTestData.Input(new[] { new CellCoord(3, 1), new CellCoord(3, 3) }, center: new CellCoord(1, 1), cap: 2,
                targets: new[] { fragile, Target(3, 3, 3) }, structures: new[] { barrel,
                    PowderBlossomFixture.Structure(50, EntityKey.Structure_Headquarters, 7, 7, 1000, Owner.Enemy) },
                obstacles: new[] { new ObstacleContentProfile(barrel.EntityId, ObstacleContentKind.PowderBarrel, 10) });
            var attack = PowderBlossomFixture.Attacks(PowderBlossomFixture.Calculate(input)).Single();
            Assert.That(attack.Hits.Select(h => h.TargetId.Value), Has.No.Member(2));
            Assert.That(attack.DamageByRecipient[new EntityId(3)].DamageBeforeMatchup, Is.EqualTo(75));
            Assert.That(attack.ProjectilePath.Last(), Is.EqualTo(new CellCoord(3, 3)));
        }

        [Test]
        public void EmptyCastConsumesOneUseAndIncompleteCastRejects()
        {
            var input = MagicBulletTestData.Input(new[] { new CellCoord(5, 3), new CellCoord(5, 4) });
            Assert.That(PowderBlossomFixture.Attacks(PowderBlossomFixture.Calculate(input)).Single().Hits, Is.Empty);
            var incomplete = MagicBulletTestData.Copy(input, request: new PlayerSkillRequest(new[] { new CellCoord(5, 3) }, input.PlayerSkillSource));
            var rejected = CombatCommandCalculator.Calculate(incomplete, Array.Empty<IMovementTileRule>());
            Assert.That(rejected.Accepted, Is.False);
            Assert.That(rejected.Reason, Is.EqualTo(nameof(PlayerSkillFailureReason.WrongPointCount)));
            Assert.That(rejected.FinalState.PlayerSkill.RemainingUses, Is.EqualTo(2));
        }

        internal static CharacterCombatFacts Target(int id, int x, int y, Owner owner = Owner.Enemy) =>
            CombatCommandTestData.Character(id, EntityKey.Unit_Demon, new CellCoord(x, y), owner)
                .WithHealth(new HealthStateSnapshot(1000, 1000));
    }
}
