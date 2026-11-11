using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Items;

namespace ProjectL.Tests.Core
{
    public sealed class WeaknessExposureDamageTests
    {
        [TestCase(CombatDamageKind.OrdinaryAttack)]
        [TestCase(CombatDamageKind.ExtraAttack)]
        [TestCase(CombatDamageKind.ReactionAttack)]
        [TestCase(CombatDamageKind.EnvironmentDamage)]
        [TestCase(CombatDamageKind.ReflectedDamage)]
        [TestCase(CombatDamageKind.DamageOverTime)]
        public void NumericReceivingBoundary_AmplifiesOnceBeforeShield(CombatDamageKind kind)
        {
            var input = CombatCommandTestData.Input();
            var target = HuntersMarkAttackTests.Expose(input.Characters.Single(c => c.EntityId.Value == 2)).WithHealth(new HealthStateSnapshot(500, 500));
            target = Shield(target, 50);
            var characters = input.Characters.ToDictionary(c => c.EntityId);
            characters[target.EntityId] = target;
            var events = new List<CombatEvent>();
            var damage = CombatDamageApplication.For(input, characters, events);
            var outcome = damage.Apply(target.EntityId, kind, new CombatDamageOrigin(CellCoord.Zero, input.ActorId, 1), 100);
            Assert.That(outcome.DamageBeforeItems, Is.EqualTo(130));
            Assert.That(outcome.RemainingDamage, Is.EqualTo(80));
            Assert.That(outcome.ActualHealthLoss, Is.EqualTo(80));
            Assert.That(outcome.After.Health.CurrentHealth, Is.EqualTo(420));
            Assert.That(outcome.After.StatusEffects.Entries.OfType<ShieldStatus>().Single().RemainingBudget, Is.Zero);
            Assert.That(HuntersMarkAttackTests.Exposures(outcome.After).Single().IncreasePercent, Is.EqualTo(30));
            Assert.That(damage.CaptureAppliedDamage()[target.EntityId], Is.EqualTo(80));
            Assert.That(target.Health.CurrentHealth, Is.EqualTo(500));
            Assert.That(target.StatusEffects.Entries.OfType<ShieldStatus>().Single().RemainingBudget, Is.EqualTo(50));
        }

        [Test]
        public void BeforeDamageItem_SeesAmplifiedRequestBeforeShield()
        {
            var input = CombatCommandTestData.Input();
            var target = Shield(HuntersMarkAttackTests.Expose(input.Characters.Single(c => c.EntityId.Value == 2))
                .WithHealth(new HealthStateSnapshot(600, 600)), 50)
                .With(items: new CharacterItemState(2, Array.Empty<ItemTemporaryEffect>()));
            var characters = input.Characters.ToDictionary(c => c.EntityId);
            characters[target.EntityId] = target;
            var events = new List<CombatEvent>();
            var damage = new CombatDamageApplication(new[] { new ItemDefinition(2, new RoketItemSpec(20, 50)) }, characters, events, input);
            var outcome = damage.Apply(target.EntityId, CombatDamageKind.OrdinaryAttack, new CombatDamageOrigin(CellCoord.Zero, input.ActorId, 1), 100);
            Assert.That(outcome.ActualHealthLoss, Is.EqualTo(15));
            Assert.That(outcome.After.Health.CurrentHealth, Is.EqualTo(585));
            Assert.That(outcome.After.Items.EquippedItemKey, Is.Null);
            Assert.That(events.OfType<CombatItemEvent>().Single(e => e.Stage == ItemEventStage.BeforeDamage).AppliedMagnitude, Is.EqualTo(65));
        }

        [TestCase(true, false, 80)]
        [TestCase(false, true, 74)]
        [TestCase(true, true, 74)]
        public void Sacrifice_UsesActualRecipientExposureAndGrantsActualRecipient(bool exposeOriginal, bool exposeProtector, int protectorHealth)
        {
            var input = Producer(SacrificeTraitTests.Input());
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c =>
                c.EntityId.Value == 2 && exposeOriginal || c.EntityId.Value == 5 && exposeProtector ? HuntersMarkAttackTests.Expose(c) : c).ToArray());
            var preview = CombatCommandCalculator.Preview(input, Array.Empty<IMovementTileRule>(), out var projected);
            var result = HuntersMarkAttackTests.Run(input);
            Assert.That(preview.CanAttack, Is.True);
            Assert.That(TraitCombatFixtures.Final(result, 2).Health.CurrentHealth, Is.EqualTo(5));
            Assert.That(TraitCombatFixtures.Final(result, 5).Health.CurrentHealth, Is.EqualTo(protectorHealth));
            Assert.That(TraitCombatFixtures.Final(projected, 5).SameValues(TraitCombatFixtures.Final(result, 5)), Is.True);
            // 희생이 대신 맞으면 표식도 실제로 맞은 수호자가 받는다(2026-11-02 결정).
            Assert.That(HuntersMarkAttackTests.Exposures(TraitCombatFixtures.Final(result, 5))
                .Single(s => s.Origin.Value.OwnerInstanceId == input.ActorId.Value).IncreasePercent, Is.EqualTo(30));
            Assert.That(HuntersMarkAttackTests.Exposures(TraitCombatFixtures.Final(result, 2))
                .Any(s => s.Origin.Value.OwnerInstanceId == input.ActorId.Value), Is.False);
        }

        [Test]
        public void Sacrifice_ItemAndShieldSimulationMatchesCommit()
        {
            var input = SacrificeTraitTests.Input();
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId.Value == 5
                ? Shield(HuntersMarkAttackTests.Expose(c), 5).With(items: new CharacterItemState(2, Array.Empty<ItemTemporaryEffect>())) : c).ToArray());
            input = CombatItemDirectDamageTests.Items(input);
            var preview = CombatCommandCalculator.Preview(input, Array.Empty<IMovementTileRule>(), out var projected);
            var result = HuntersMarkAttackTests.Run(input);
            Assert.That(preview.CanAttack, Is.True);
            var protector = TraitCombatFixtures.Final(result, 5);
            Assert.That(protector.Health.CurrentHealth, Is.EqualTo(92));
            Assert.That(protector.Items.EquippedItemKey, Is.Null);
            Assert.That(protector.StatusEffects.Entries.OfType<ShieldStatus>().Single().RemainingBudget, Is.Zero);
            Assert.That(TraitCombatFixtures.Final(projected, 5).SameValues(protector), Is.True);
        }

        [Test]
        public void VisibleMine_UsesSameReceivingBoundaryInPreviewAndCommit()
        {
            var input = CliffFallTests.WithEnvironment(MovementTestData.SafeRoute(CellCoord.Zero, CellCoord.Right),
                MineEnvironmentTests.Mine(cell: CellCoord.Right, damage: 10));
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId == input.ActorId ? HuntersMarkAttackTests.Expose(c) : c).ToArray());
            var preview = CombatMovementCalculator.Preview(input, MineEnvironmentTests.Rules);
            var result = CombatCommandCalculator.Calculate(input, MineEnvironmentTests.Rules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(MovementTestData.Actor(result).Health.CurrentHealth, Is.EqualTo(87));
            Assert.That(input.Characters.Single(c => c.EntityId == input.ActorId).Health.CurrentHealth, Is.EqualTo(100));
        }

        [TestCase(0, 94, 6)]
        [TestCase(4, 98, 2)]
        public void Reflection_AmplifiesItsRecipientWithoutChangingBasisOrRepeating(int shield, int health, int applied)
        {
            var input = ThornHeartTestData.Input(heart: true, hp: 100);
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId == input.ActorId
                ? Shield(HuntersMarkAttackTests.Expose(c, 100), shield) : c).ToArray());
            var result = HuntersMarkAttackTests.Run(input);
            var final = TraitCombatFixtures.Final(result, input.ActorId.Value);
            Assert.That(final.Health.CurrentHealth, Is.EqualTo(health));
            var activation = result.Events.OfType<CombatArtifactEvent>().Single(e => e.Reflection != null).Activations.Single();
            Assert.That(activation.RequestedAmount, Is.EqualTo(6));
            Assert.That(activation.AppliedAmount, Is.EqualTo(applied));
        }

        [TestCase(false, 0, 39, 13, 61)]
        [TestCase(false, 30, 9, 9, 91)]
        [TestCase(true, 0, 39, 13, 61)]
        [TestCase(true, 30, 9, 9, 91)]
        public void SecondaryDamage_UsesReceivingOutcomeForPreviewAndLiveHealth(bool forcedField, int shield,
            int expectedDamage, int secondaryDamage, int health)
        {
            var input = forcedField ? CliffFallTests.Knockback() : CombatCommandTestData.Input(
                new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.Radial, 100));
            if (forcedField)
                input = CombatCommandTestData.With(input, state: CombatCommandEffectTests.WithFields(input,
                    new ThornFieldCellSnapshot(new CellCoord(5, 3), 3, 10)));
            else
            {
                var barrel = ObstacleReactionTests.Barrel(51, new CellCoord(4, 4), 1, 10);
                input = ObstacleReactionTests.Copy(input, structures: new[] { barrel.Structure }, profiles: new[] { barrel.Profile });
            }
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId.Value == 2
                ? Shield(HuntersMarkAttackTests.Expose(c), shield) : c).ToArray());
            var preview = CombatCommandCalculator.Preview(input, Array.Empty<IMovementTileRule>(), out var projected);
            var result = HuntersMarkAttackTests.Run(input);
            Assert.That(preview.CanAttack, Is.True);
            var target = preview.Targets.ToArray().Single(t => t.TargetId.Value == 2);
            Assert.That(TraitCombatFixtures.Final(projected, 2).Health.CurrentHealth, Is.EqualTo(health));
            Assert.That(TraitCombatFixtures.Final(result, 2).Health.CurrentHealth, Is.EqualTo(health));
            Assert.That(target.ExpectedDamage, Is.EqualTo(expectedDamage));
            Assert.That(target.SecondaryDamage, Is.EqualTo(secondaryDamage));
            Assert.That(target.TargetHealthAfter, Is.EqualTo(health));
            Assert.That(input.Characters.Single(c => c.EntityId.Value == 2).Health.CurrentHealth, Is.EqualTo(100));
        }

        [TestCase(0, 30, false)]
        [TestCase(30, 30, true)]
        public void MovementContext_TracksExposureDamageWithoutDependingOnItsDuration(int beforePercent, int afterPercent, bool matches)
        {
            var input = CliffFallTests.WithEnvironment(MovementTestData.SafeRoute(CellCoord.Zero, CellCoord.Right),
                MineEnvironmentTests.Mine(cell: CellCoord.Right, damage: 10));
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId == input.ActorId
                ? HuntersMarkAttackTests.Expose(c, beforePercent, 1) : c).ToArray());
            var expected = MovementContextCalculator.Capture(input, Owner.Player, MineEnvironmentTests.Rules);
            var changed = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId == input.ActorId
                ? HuntersMarkAttackTests.Expose(c, afterPercent, 3) : c).ToArray());
            Assert.That(expected, Is.Not.Null);
            Assert.That(MovementContextCalculator.Matches(expected, changed, Owner.Player, MineEnvironmentTests.Rules), Is.EqualTo(matches));
            Assert.That(CombatMovementCalculator.Preview(input, MineEnvironmentTests.Rules).ProjectedActor.Health.CurrentHealth,
                Is.EqualTo(beforePercent == 0 ? 90 : 87));
            Assert.That(CombatMovementCalculator.Preview(changed, MineEnvironmentTests.Rules).ProjectedActor.Health.CurrentHealth, Is.EqualTo(87));
        }
        internal static CharacterCombatFacts Shield(CharacterCombatFacts target, int budget) =>
            target.WithStatusEffects(new StatusEffectSet(target.StatusEffects.Entries.Append(new ShieldStatus(
                new EffectOriginKey(EffectOwnerKind.Item, 91, 200, "test-shield"), budget, true, 0)).ToArray()));

        private static CombatCommandInput Producer(CombatCommandInput input) => CombatCommandTestData.With(input,
            characters: input.Characters.Select(c => c.EntityId == input.ActorId ? c.With(stats: c.Stats.With(
                traitStatuses: new TraitStatusSnapshot[] { new HuntersMarkTraitStatusSnapshot(30, 1) })) : c).ToArray(),
            profiles: input.Profiles.Select(p => p.Definition.Key == input.Characters.Single(c => c.EntityId == input.ActorId).CharacterKey
                ? new CombatAttackProfile(CombatCommandTestData.Spec(p.Definition.Key, 20, CategoryKey.Main, new HuntersMarkTraitSpec(30, 1)), p.Method) : p).ToArray());
    }
}