using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;

namespace ProjectL.Tests.Core
{
    public sealed class NewTraitRankTests
    {
        [Test]
        public void RampageNIsSingleSource()
        {
            var rank2 = new TraitRankValues(TraitKey.Rampage, 1, 5);
            var rank3 = new TraitRankValues(TraitKey.Rampage, 2, 7);
            foreach (int basis in new[] { 10, 20 })
            {
                var normal = new RampageTraitSpec(basis, 0, 1);
                var definition = new TraitGrowthDefinition(normal, null, "", "", "", "", rank2, rank3);
                var resolved = Enumerable.Range(1, 3)
                    .Select(rank => (RampageTraitSpec)Resolve(definition, rank)).ToArray();

                Assert.That(resolved.Select(spec => spec.BaseAttackBonus + spec.RankAttackBonus),
                    Is.EqualTo(new[] { basis, basis + 5, basis + 7 }));
                Assert.That(resolved.Select(spec => spec.MovementBonus), Is.EqualTo(new[] { 1, 1, 2 }));
            }
        }

        [Test]
        public void BleedingLevelValuesAreOneTwoThree()
        {
            var normal = new BleedingTraitSpec(1);
            var definition = new TraitGrowthDefinition(normal, null, "", "", "", "",
                new TraitRankValues(TraitKey.Bleeding, 2), new TraitRankValues(TraitKey.Bleeding, 3));
            var origin = new EffectOriginKey(EffectOwnerKind.Trait, 1, (int)TraitKey.Bleeding, "bleeding");
            var state = StatusEffectSet.Empty;

            for (int rank = 1; rank <= 3; rank++)
            {
                var resolved = (BleedingTraitSpec)Resolve(definition, rank);
                state = PeriodicStatusRules.ApplyBleeding(state, origin, resolved.DamagePerStack);
            }

            Assert.That(state.Entries.OfType<BleedingStatus>().Single().Stacks.Select(stack => stack.Damage),
                Is.EqualTo(new[] { 1, 2, 3 }));
        }

        [Test]
        public void InvalidRankValuesFailBeforeResolution()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new TraitRankValues(TraitKey.Bleeding, 0));
            var normal = new RampageTraitSpec(int.MaxValue, 0, 1);
            Assert.Throws<OverflowException>(() => new TraitGrowthDefinition(normal, null, "", "", "", "",
                new TraitRankValues(TraitKey.Rampage, 1, 5), new TraitRankValues(TraitKey.Rampage, 2, 7)));
        }

        private static TraitSpec Resolve(TraitGrowthDefinition definition, int rank)
        {
            var basis = CombatCommandTestData.Spec(EntityKey.Unit_Cowboy, 10, CategoryKey.Rider, definition.Normal);
            var catalog = new TraitGrowthCatalog(TraitGrowthCatalog.Disabled.Categories, new[] { definition });
            var growth = OwnedTraitGrowth.Empty;
            for (int i = 1; i < rank; i++)
                growth = growth.WithPurchase(definition.Key);
            var owned = new OwnedCharacterRecord(new CharacterInstanceId(1), basis.Key, CharacterOwnershipRole.Sub, growth: growth);
            return OwnedUnitSpecResolver.Resolve(basis, owned, catalog).Traits.ToArray().Single();
        }
    }
}
