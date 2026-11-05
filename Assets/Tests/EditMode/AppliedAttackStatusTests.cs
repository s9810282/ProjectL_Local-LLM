using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;

namespace ProjectL.Tests.Core
{
    public sealed class AppliedAttackStatusTests
    {
        private static readonly EffectOriginKey Origin = Source(TraitKey.Vanguard);

        [TestCase(Owner.Player)]
        [TestCase(Owner.Enemy)]
        public void RepeatedSameSourceGrants_AddAndExpireAtEveryControllerEnd(Owner endedSide)
        {
            var statuses = AppliedStatusRules.Apply(StatusEffectSet.Empty, new AttackIncreaseStatus(Origin, 3, 1));
            statuses = AppliedStatusRules.Apply(statuses, new AttackIncreaseStatus(Origin, 4, 2));
            Assert.That(statuses.Entries.Count, Is.EqualTo(2));
            Assert.That(AppliedStatusRules.AttackIncrease(statuses), Is.EqualTo(7));

            var first = AppliedStatusRules.EndControllerTurn(statuses, Owner.Player, endedSide);
            Assert.That(AppliedStatusRules.AttackIncrease(first), Is.EqualTo(4));
            Assert.That(((AttackIncreaseStatus)first.Entries.Single()).RemainingControllerTurnEnds, Is.EqualTo(1));
            var second = AppliedStatusRules.EndControllerTurn(first, Owner.Player, endedSide);
            Assert.That(AppliedStatusRules.AttackIncrease(second), Is.Zero);
            Assert.That(second.Entries, Is.Empty);
            Assert.That(AppliedStatusRules.AttackIncrease(statuses), Is.EqualTo(7));
        }

        [TestCase(TraitKey.CleanSweep)]
        [TestCase(TraitKey.MoraleBoost)]
        [TestCase(TraitKey.SteadfastWill)]
        public void AnotherProducer_UsesTheSameAggregationAndIndependentLifetime(TraitKey producer)
        {
            var statuses = AppliedStatusRules.Apply(StatusEffectSet.Empty, new AttackIncreaseStatus(Origin, 3, 1));
            statuses = AppliedStatusRules.Apply(statuses, new AttackIncreaseStatus(Source(producer), 5, 3));
            var first = AppliedStatusRules.EndControllerTurn(statuses, Owner.Enemy, Owner.Player);
            Assert.That(AppliedStatusRules.AttackIncrease(first), Is.EqualTo(5));
            Assert.That(first.Entries.Single().Origin, Is.EqualTo(Source(producer)));
            Assert.That(((AttackIncreaseStatus)first.Entries.Single()).RemainingControllerTurnEnds, Is.EqualTo(2));
        }

        [Test]
        public void ActualTurnBoundary_ConsumesOnceAndPreservesHeldAndPermanentContributions()
        {
            var input = TraitCombatFixtures.Equip(CombatCommandTestData.Input(), EntityKey.Unit_MainCharacter, new AccelerationTraitSpec(1));
            input = input.WithPersistentFacts(input.Owned.Select(o => o.Role == CharacterOwnershipRole.Main
                ? new OwnedCharacterRecord(o.Id, o.CharacterKey, o.Role, hunterAttackBonus: 7) : o).ToArray(),
                input.ItemInventory, input.GoldBalance, input.FameBalance);
            var main = input.Characters.Single(c => c.OwnershipRole == CharacterOwnershipRole.Main);
            var statuses = new StatusEffectSet(new AppliedStatus[] {
                new AttackIncreaseStatus(Origin, 3, 1), new AttackIncreaseStatus(Origin, 4, 2),
                new AttackIncreaseStatus(Source(TraitKey.MoraleBoost), 5, 3) });
            main = CombatEffectiveStatsCalculator.ProjectTraitAttack(main.WithStatusEffects(statuses)
                .With(stats: main.Stats.With(traitStatuses: new TraitStatusSnapshot[] { new AccelerationTraitStatusSnapshot(5) })), 7);
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId == main.EntityId ? main : c).ToArray());
            Assert.That(main.Stats.TraitAttackBonus, Is.EqualTo(24));
            var transition = CombatTurnCalculator.EndPlayerTurn(CombatCommandTestData.FreshCommand(input, CombatCommandKind.EndPlayerTurn), Array.Empty<IMovementTileRule>());
            Assert.That(transition.Accepted, Is.True, transition.Reason);
            var after = TraitCombatFixtures.Final(transition, main.EntityId.Value);
            Assert.That(AppliedStatusRules.AttackIncrease(after.StatusEffects), Is.EqualTo(9));
            Assert.That(after.Stats.TraitAttackBonus, Is.EqualTo(21));
            Assert.That(after.Stats.TraitStatuses.ToArray().OfType<AccelerationTraitStatusSnapshot>().Single().HeldAttackBonus, Is.EqualTo(5));
            Assert.That(input.Owned.Single(o => o.Id == main.OwnedId).HunterAttackBonus, Is.EqualTo(7));
            Assert.That(main.StatusEffects.SameValues(statuses), Is.True);

            var cleared = CombatTemporaryStateCleanup.Clear(after);
            Assert.That(cleared.StatusEffects.Entries, Is.Empty);
            Assert.That(cleared.Stats.TraitAttackBonus, Is.EqualTo(7));
        }

        [Test]
        public void Cleanup_RemovesCanonicalStateThatLegacyDtoCannotRepresent()
        {
            var before = CombatCommandTestData.Character(1, EntityKey.Unit_Akimbo, CellCoord.Zero, Owner.Player)
                .WithStatusEffects(new StatusEffectSet(new AppliedStatus[] { new ProbeStatus() }));
            var after = CombatTemporaryStateCleanup.Clear(before);
            Assert.That(after.StatusEffects.Entries, Is.Empty);
            Assert.That(before.StatusEffects.Entries.Count, Is.EqualTo(1));
            Assert.That(CombatTemporaryStateCleanup.Clear(after), Is.SameAs(after));
        }

        [Test]
        public void Apply_RejectsInvalidOrOverflowingRequestsWithoutChangingInput()
        {
            var current = new StatusEffectSet(new AppliedStatus[] { new AttackIncreaseStatus(Origin, int.MaxValue, 1) });
            Assert.Throws<OverflowException>(() => AppliedStatusRules.Apply(current, new AttackIncreaseStatus(Origin, 1, 1)));
            Assert.That(AppliedStatusRules.AttackIncrease(current), Is.EqualTo(int.MaxValue));
            Assert.Throws<ArgumentNullException>(() => AppliedStatusRules.Apply(null, new AttackIncreaseStatus(Origin, 1, 1)));
            Assert.Throws<ArgumentNullException>(() => AppliedStatusRules.Apply(current, null));
            Assert.Throws<ArgumentException>(() => AppliedStatusRules.Apply(current, new ProbeStatus()));
            Assert.Throws<ArgumentOutOfRangeException>(() => AppliedStatusRules.EndControllerTurn(current, Owner.Player, (Owner)99));
        }

        private static EffectOriginKey Source(TraitKey key) => new(EffectOwnerKind.Trait, 1, (int)key, "grant");

        private sealed class ProbeStatus : AppliedStatus
        {
            public ProbeStatus() : base(StatusEffectKey.DefenseIncrease, null) { }
            public override StatusEffectLifetimeKind Lifetime => StatusEffectLifetimeKind.BattleEnd;
            public override bool SameValues(AppliedStatus other) => other is ProbeStatus;
        }
    }
}
