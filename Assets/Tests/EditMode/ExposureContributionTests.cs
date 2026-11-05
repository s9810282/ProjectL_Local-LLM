using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;

namespace ProjectL.Tests.Core
{
    public sealed class ExposureContributionTests
    {
        [TestCase(TraitKey.ReconSquad, 10, 110)]
        [TestCase(TraitKey.ReconSquad, 20, 120)]
        [TestCase(TraitKey.HuntersMark, 30, 140)]
        public void SameTraitMax_DifferentTraitsAdd(TraitKey secondTrait, int secondPercent, int damage)
        {
            var statuses = new StatusEffectSet(new AppliedStatus[] {
                new WeaknessExposureStatus(Origin(1, TraitKey.ReconSquad), 10, 2),
                new WeaknessExposureStatus(Origin(2, secondTrait), secondPercent, 2) });
            Assert.That(WeaknessExposureRules.Amplify(100, statuses), Is.EqualTo(damage));
        }

        [Test]
        public void CheckedSumAndAmplificationOverflow_RejectBeforeReturningNewState()
        {
            var before = new StatusEffectSet(new AppliedStatus[] {
                new WeaknessExposureStatus(Origin(1, TraitKey.ReconSquad), int.MaxValue, 1) });
            Assert.Throws<OverflowException>(() => AppliedStatusRules.Apply(before,
                new WeaknessExposureStatus(Origin(2, TraitKey.HuntersMark), 1, 1)));
            Assert.Throws<OverflowException>(() => WeaknessExposureRules.Amplify(int.MaxValue, before));
            Assert.That(before.Entries, Has.Count.EqualTo(1));
        }

        [Test]
        public void MovementContext_UsesCrossTraitTotalEvenWhenGlobalMaximumIsUnchanged()
        {
            var input = CliffFallTests.WithEnvironment(MovementTestData.SafeRoute(CellCoord.Zero, CellCoord.Right),
                MineEnvironmentTests.Mine(cell: CellCoord.Right, damage: 10));
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId == input.ActorId
                ? HuntersMarkAttackTests.Expose(c, 30, 1) : c).ToArray());
            var expected = MovementContextCalculator.Capture(input, Owner.Player, MineEnvironmentTests.Rules);
            var changed = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId == input.ActorId
                ? c.WithStatusEffects(AppliedStatusRules.Apply(c.StatusEffects, new WeaknessExposureStatus(Origin(2, TraitKey.ReconSquad), 10, 1))) : c).ToArray());
            Assert.That(MovementContextCalculator.Matches(expected, changed, Owner.Player, MineEnvironmentTests.Rules), Is.False);
            Assert.That(CombatMovementCalculator.Preview(changed, MineEnvironmentTests.Rules).ProjectedActor.Health.CurrentHealth, Is.EqualTo(86));
        }

        [Test]
        public void TiedMaxima_ReturnAllPositiveSources_WithoutSplittingByHolderOrCallback()
        {
            var entries = new WeaknessExposureStatus[] {
                new(Origin(1, TraitKey.ReconSquad, "first"), 10, 2),
                new(Origin(2, TraitKey.ReconSquad, "second"), 10, 2),
                new(Origin(3, TraitKey.ReconSquad), 5, 2),
                new(Origin(1, TraitKey.HuntersMark), 30, 2),
                new(Origin(4, TraitKey.ReconSquad), 0, 2) };
            var evaluation = WeaknessExposureRules.Evaluate(new StatusEffectSet(entries));
            Assert.That(evaluation.EffectivePercent, Is.EqualTo(40));
            Assert.That(evaluation.UsedSources, Is.EqualTo(new[] { entries[0], entries[1], entries[3] }));
            entries[0] = null;
            Assert.That(evaluation.UsedSources[0].IncreasePercent, Is.EqualTo(10));
            Assert.Throws<NotSupportedException>(() => ((IList<WeaknessExposureStatus>)evaluation.UsedSources).Clear());
            var qualified = new StatusEffectSet(new AppliedStatus[] { evaluation.UsedSources[0],
                new WeaknessExposureStatus(new EffectOriginKey(EffectOwnerKind.Item, 1, (int)TraitKey.ReconSquad, "test"), 7, 1) });
            Assert.That(WeaknessExposureRules.Evaluate(qualified).EffectivePercent, Is.EqualTo(17));
        }

        [Test]
        public void TimedReapply_ReplacesOwnMagnitudeAndDuration_ThenFallsBack()
        {
            var recon = Recon(1, 3, 20);
            var before = new StatusEffectSet(new AppliedStatus[] { recon, new WeaknessExposureStatus(Origin(1, TraitKey.HuntersMark), 30, 3) });
            var captured = StatusEffectReadModel.Capture(Target(before)).OfType<WeaknessExposureEffectSnapshot>().Single();
            var refreshed = WeaknessExposureRules.Apply(before, new WeaknessExposureStatus(Origin(1, TraitKey.HuntersMark, "other-callback"), 10, 1));
            Assert.That(WeaknessExposureRules.EffectivePercent(refreshed), Is.EqualTo(30));
            Assert.That(refreshed.Entries, Has.Count.EqualTo(2));
            Assert.That(((WeaknessExposureStatus)refreshed.Entries[1]).RemainingAffectedSideTurnEnds, Is.EqualTo(1));
            var expired = AppliedStatusRules.EndControllerTurn(refreshed, Owner.Enemy, Owner.Enemy);
            Assert.That(expired.Entries.Single(), Is.SameAs(recon));
            Assert.That(WeaknessExposureRules.EffectivePercent(expired), Is.EqualTo(20));
            Assert.That(captured.EffectiveIncreasePercent, Is.EqualTo(50));
            Assert.That(captured.Sources[1].RemainingAffectedSideTurnEnds, Is.EqualTo(3));
        }

        [Test]
        public void MovementGrants_RemoveOnlyMatchingMove_AndDuplicateContactDoesNotReplace()
        {
            var first = Recon(1, 3, 10);
            var second = Recon(1, 4, 20);
            var other = Recon(2, 5, 15);
            var timed = new WeaknessExposureStatus(Origin(1, TraitKey.HuntersMark), 30, 2);
            var before = WeaknessExposureRules.Apply(new StatusEffectSet(new AppliedStatus[] { first, other, timed }), second);
            Assert.That(WeaknessExposureRules.Apply(before, Recon(1, 3, 99)), Is.SameAs(before));
            var after = WeaknessExposureRules.RemoveMovement(before, second.Movement.Value);
            Assert.That(after.Entries, Is.EqualTo(new AppliedStatus[] { first, other, timed }));
            Assert.That(WeaknessExposureRules.EffectivePercent(after), Is.EqualTo(45));
            Assert.That(WeaknessExposureRules.EffectivePercent(before), Is.EqualTo(50));
            Assert.That(WeaknessExposureRules.RemoveMovement(after, second.Movement.Value), Is.SameAs(after));
            Assert.That(first.SameValues(Recon(1, 4, 10)), Is.False);
            var receipt = new MovementExposureReceipt(first.Movement.Value, new EntityId(2), first);
            Assert.That(receipt.ExpectedStatus, Is.SameAs(first));
            Assert.Throws<ArgumentException>(() => new MovementExposureReceipt(first.Movement.Value, new EntityId(2), second));
        }

        [TestCase(Owner.Player, CharacterRuntimeArea.Hand)]
        [TestCase(Owner.Enemy, CharacterRuntimeArea.EnemyWaiting)]
        public void UntimedRecon_DoesNotTick_AndOnlyRecipientReserveEntryClears(Owner owner, CharacterRuntimeArea reserve)
        {
            var input = CombatCommandTestData.Input();
            var recipient = input.Characters.First(c => c.Owner == owner);
            var status = Recon(1, 3, 10);
            recipient = recipient.WithStatusEffects(new StatusEffectSet(new AppliedStatus[] { status }));
            var projected = input.Characters.ToDictionary(c => c.EntityId);
            projected[recipient.EntityId] = recipient;
            var events = new List<CombatEvent>();
            TraitTurnLifecycle.EndAndRecord(input, projected, owner, events);
            Assert.That(projected[recipient.EntityId].StatusEffects.SameValues(recipient.StatusEffects), Is.True);
            Assert.That(events.OfType<CombatEffectsEvent>().SelectMany(e => e.Traits).Any(t => t.TraitKey == TraitKey.ReconSquad), Is.False);
            var source = StatusEffectReadModel.Capture(recipient).OfType<WeaknessExposureEffectSnapshot>().Single().Sources.Single();
            Assert.That(source.LifetimeKind, Is.EqualTo(StatusEffectLifetimeKind.UntilReserveReturnOrDeath));
            Assert.That(source.RemainingAffectedSideTurnEnds, Is.Null);
            var relocated = CombatPlacementCalculator.Location(recipient, CharacterRuntimeArea.Field, null, CellCoord.Right, recipient.Rotation);
            Assert.That(relocated.StatusEffects.SameValues(recipient.StatusEffects), Is.True);
            var returned = CombatPlacementCalculator.Location(recipient, reserve, owner == Owner.Player ? 0 : (int?)null, null, null);
            Assert.That(returned.StatusEffects.Entries, Is.Empty);
            Assert.That(recipient.StatusEffects.Entries.Single(), Is.SameAs(status));
        }

        [Test]
        public void NumericDamage_RetainsSelectedSourcesEvenWhenShieldAbsorbsAll()
        {
            var input = CombatCommandTestData.Input();
            var first = Recon(1, 3, 10);
            var tied = Recon(3, 4, 10);
            var suppressed = Recon(4, 5, 5);
            var mark = new WeaknessExposureStatus(Origin(1, TraitKey.HuntersMark), 30, 1);
            var target = WeaknessExposureDamageTests.Shield(Target(new StatusEffectSet(new AppliedStatus[] { first, tied, suppressed, mark })), 200);
            var characters = input.Characters.ToDictionary(c => c.EntityId);
            characters[target.EntityId] = target;
            var events = new List<CombatEvent>();
            var damage = CombatDamageApplication.For(input, characters, events);
            var origin = new CombatDamageOrigin(CellCoord.Zero, input.ActorId, 1);
            var result = damage.Apply(target.EntityId, CombatDamageKind.EnvironmentDamage, origin, 100);
            Assert.That(result.DamageBeforeItems, Is.EqualTo(140));
            Assert.That(result.ActualHealthLoss, Is.Zero);
            Assert.That(result.Exposure.UsedSources, Is.EqualTo(new[] { first, tied, mark }));
            Assert.That(result.After.StatusEffects.Entries.OfType<ShieldStatus>().Single().RemainingBudget, Is.EqualTo(60));
            Assert.That(target.StatusEffects.Entries.OfType<ShieldStatus>().Single().RemainingBudget, Is.EqualTo(200));
            Assert.That(damage.Apply(target.EntityId, CombatDamageKind.EnvironmentDamage, origin, 0).Exposure, Is.Null);
            Assert.That(damage.Defeat(target.EntityId, origin).Exposure, Is.Null);
        }

        [TestCase(WeaknessExposureLifetimeKind.AffectedSideTurnEnds, null)]
        [TestCase(WeaknessExposureLifetimeKind.AffectedSideTurnEnds, 0)]
        [TestCase(WeaknessExposureLifetimeKind.UntilReserveReturnOrDeath, 0)]
        [TestCase(WeaknessExposureLifetimeKind.UntilReserveReturnOrDeath, int.MaxValue)]
        public void LifetimeRejectsMissingTimedClockOrUntimedSentinel(WeaknessExposureLifetimeKind kind, int? remaining)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new WeaknessExposureStatus(Origin(1, TraitKey.ReconSquad), 10, kind, remaining));
        }

        [Test]
        public void MovementSourceAndCompatibilityCopies_PreserveCanonicalExposure()
        {
            var status = Recon(1, 3, 10);
            Assert.Throws<ArgumentException>(() => new WeaknessExposureStatus(Origin(2, TraitKey.ReconSquad), 10,
                WeaknessExposureLifetimeKind.UntilReserveReturnOrDeath, movement: status.Movement));
            var legacy = new AttackDecreaseStatus(null, 20);
            var target = Target(new StatusEffectSet(new AppliedStatus[] { status, legacy }));
            var copy = target.With().With(debuffs: target.Debuffs).With(traitEffects: target.TraitEffects);
            Assert.That(copy.SameValues(target), Is.True);
            var removed = WeaknessExposureRules.RemoveMovement(copy.StatusEffects, status.Movement.Value);
            Assert.That(removed.Entries.Single(), Is.SameAs(legacy));
        }

        private static WeaknessExposureStatus Recon(int source, int move, int percent) => new(Origin(source, TraitKey.ReconSquad), percent,
            WeaknessExposureLifetimeKind.UntilReserveReturnOrDeath, movement: new MovementEffectId(1, 1, new EntityId(source), move));
        private static CharacterCombatFacts Target(StatusEffectSet statuses) =>
            CombatCommandTestData.Character(2, EntityKey.Unit_Demon, CellCoord.Zero, Owner.Enemy).WithStatusEffects(statuses);
        private static EffectOriginKey Origin(int source, TraitKey trait, string rule = "main") => new(EffectOwnerKind.Trait, source, (int)trait, rule);
    }
}
