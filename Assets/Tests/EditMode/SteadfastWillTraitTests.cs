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

namespace ProjectL.Tests.Core
{
    public sealed class SteadfastWillTraitTests
    {
        [Test]
        public void Trait_BlocksKnockback()
        {
            var input = Input(pull: false);
            var before = input.Characters.Single(c => c.EntityId == PullTestData.Target);
            var preview = CombatCommandCalculator.Preview(input, Array.Empty<IMovementTileRule>(), out var projection);
            var result = TraitCombatFixtures.Attack(input);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(PullTestData.Final(result).Anchor, Is.EqualTo(before.Anchor));
            Assert.That(PullTestData.Final(result).Health.CurrentHealth, Is.EqualTo(80));
            Assert.That(result.Attack.Value.Knockbacks.Single().Stop, Is.EqualTo(AttackKnockbackStop.Immune));
            Assert.That(preview.Knockbacks.Single().Stop, Is.EqualTo(AttackKnockbackStop.Immune));
            Assert.That(PullTestData.Final(projection).SameValues(PullTestData.Final(result)), Is.True);
            Assert.That(before.Anchor, Is.EqualTo(new CellCoord(9, 7)));
        }

        [Test]
        public void SameTarget_CowboyPullStillWorks()
        {
            var result = PullTestData.Run(Input(pull: true));
            Assert.That(PullTestData.Final(result).Anchor, Is.EqualTo(new CellCoord(8, 7)));
            Assert.That(PullTestData.Final(result).Health.CurrentHealth, Is.EqualTo(80));
            Assert.That(result.Attack.Value.Knockbacks, Is.Empty);
            Assert.That(result.Attack.Value.Pull.FinalAnchor, Is.EqualTo(new CellCoord(8, 7)));
        }

        private static CombatCommandInput Input(bool pull)
        {
            var input = PullTestData.Input(range: pull ? 1 : 3, trait: pull, key: EntityKey.Unit_Cowboy);
            if (!pull)
            {
                input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId == input.ActorId
                    ? c.With(stats: c.Stats.With(traitStatuses: new ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits.TraitStatusSnapshot[]
                    {
                        new ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits.KnockbackTraitStatusSnapshot(1)
                    })) : c).ToArray(), profiles: input.Profiles.Select(p => p.Definition.Key == EntityKey.Unit_Cowboy
                        ? new CombatAttackProfile(CombatCommandTestData.Spec(EntityKey.Unit_Cowboy, 20, CategoryKey.Main,
                            new KnockbackTraitSpec(1)), p.Method) : p).ToArray());
            }
            return TraitCombatFixtures.Equip(input, EntityKey.Unit_Demon, new SteadfastWillTraitSpec());
        }
    }
}
