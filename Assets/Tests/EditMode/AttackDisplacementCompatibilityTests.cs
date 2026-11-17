using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;

namespace ProjectL.Tests.Core
{
    public sealed class AttackDisplacementCompatibilityTests
    {
        private static readonly TraitSpec[] Displacements =
        {
            new KnockbackTraitSpec(1), new PullTraitSpec(1), new ImpactBoltTraitSpec(1),
            new CrashImpactTraitSpec(1, 10, 10), new DeadHandTraitSpec(100)
        };

        private static IEnumerable<TestCaseData> Conflicts()
        {
            for (int i = 0; i < Displacements.Length; i++)
                for (int j = i + 1; j < Displacements.Length; j++)
                    yield return new TestCaseData(Displacements[i], Displacements[j])
                        .SetName($"Rejects_{Displacements[i].TraitKey}_{Displacements[j].TraitKey}_InBothOrders");
            yield return new TestCaseData(new PiercingFalloffTraitSpec(25, 4), new DeadHandTraitSpec(100))
                .SetName("Rejects_Piercing_DeadHand_InBothOrders");
        }

        [TestCaseSource(nameof(Conflicts))]
        public void ProfileRejectsApprovedExclusions(TraitSpec first, TraitSpec second)
        {
            Assert.Throws<ArgumentException>(() => TraitGrowthFixtures.Profile(CategoryKey.Heavy, first, second));
            Assert.Throws<ArgumentException>(() => TraitGrowthFixtures.Profile(CategoryKey.Heavy, second, first));
        }

        [Test]
        public void CategoryDefaultCanGrantImmunityAlongsideDeadHand()
        {
            var profile = TraitGrowthFixtures.Profile(CategoryKey.Heavy, new DeadHandTraitSpec(100));
            var immunity = Definition(new SteadfastWillTraitSpec());
            var catalog = new TraitGrowthCatalog(TraitGrowthCatalog.Disabled.Categories, new[] { immunity },
                new Dictionary<CategoryKey, TraitKey> { [CategoryKey.Heavy] = immunity.Key });
            Assert.That(OwnedUnitSpecResolver.ResolveBase(profile.Definition, catalog).Traits.ToArray().Select(t => t.TraitKey),
                Is.EquivalentTo(new[] { TraitKey.DeadHand, TraitKey.SteadfastWill }));
        }

        [Test]
        public void CategoryDefaultAndRestoredAcquisitionCannotComposeTwoDisplacements()
        {
            var profile = TraitGrowthFixtures.Profile();
            var nativeDeadHand = TraitGrowthFixtures.Profile(CategoryKey.Heavy, new DeadHandTraitSpec(100));
            var knockback = Definition(new KnockbackTraitSpec(1));
            var crash = Definition(new CrashImpactTraitSpec(1, 10, 10));
            var growth = TraitGrowthFixtures.Catalog(definitions: new[] { knockback, crash });
            var catalog = new TraitGrowthCatalog(growth.Categories, growth.Definitions,
                new Dictionary<CategoryKey, TraitKey> { [CategoryKey.Heavy] = knockback.Key });
            Assert.Throws<ArgumentException>(() => OwnedUnitSpecResolver.ResolveBase(nativeDeadHand.Definition, catalog));
            Assert.That(TraitGrowthRules.Candidates(TraitGrowthFixtures.Owned(profile), profile, catalog), Has.No.Member(crash.Key));
            var record = TraitGrowthFixtures.Owned(profile,
                growth: new OwnedTraitGrowth(new[] { new TraitGrowthSelection(10, crash.Key) }));
            Assert.Throws<ArgumentException>(() => OwnedUnitSpecResolver.Resolve(profile.Definition, record, catalog));
        }

        private static TraitGrowthDefinition Definition(TraitSpec spec) => new TraitGrowthDefinition(spec, null, "", "", "", "");
    }
}
