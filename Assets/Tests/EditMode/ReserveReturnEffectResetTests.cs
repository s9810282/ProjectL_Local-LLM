using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Tests.Core
{
    public sealed class ReserveReturnEffectResetTests
    {
        [TestCase(CharacterRuntimeArea.Hand)]
        [TestCase(CharacterRuntimeArea.EnemyWaiting)]
        public void HandAndEnemyWaiting_ClearTemporaryEffects_KeepEquipmentAndHistory(CharacterRuntimeArea area)
        {
            var before = Dirty(area == CharacterRuntimeArea.Hand ? Owner.Player : Owner.Enemy);
            var after = CombatPlacementCalculator.Location(before, area, area == CharacterRuntimeArea.Hand ? 0 : null, null, null);
            Assert.That(after.StatusEffects.Entries.All(s => s is ShieldStatus), Is.True);
            var shield = after.StatusEffects.Entries.OfType<ShieldStatus>().Single();
            Assert.That((shield.RemainingBudget, shield.Eligible, shield.LastResetControllerTurnCount), Is.EqualTo((0, false, 4L)));
            Assert.That(after.Stats.TraitStatuses.ToArray().OfType<AccelerationTraitStatusSnapshot>().Single().HeldAttackBonus, Is.Zero);
            Assert.That(after.Stats.TraitStatuses.ToArray().OfType<RegenerationTraitStatusSnapshot>().Single().CompletedEligibleTurnEnds, Is.Zero);
            Assert.That(after.Stats.TraitStatuses.ToArray().OfType<StationaryFireTraitStatusSnapshot>().Single().MovedThisOwnTurn, Is.False);
            var hunter = after.Stats.TraitStatuses.ToArray().OfType<HunterTraitStatusSnapshot>().Single();
            Assert.That(hunter.TargetId, Is.Null);
            Assert.That(hunter.SuccessfulRewards, Is.EqualTo(2));
            Assert.That(after.Stats.TraitStatuses.ToArray().OfType<SacrificeTraitStatusSnapshot>().Single().SameValues(
                before.Stats.TraitStatuses.ToArray().OfType<SacrificeTraitStatusSnapshot>().Single()), Is.True);
            Assert.That(after.Stats.TraitAttackBonus, Is.EqualTo(5));
            Assert.That(after.Items.EquippedItemKey, Is.EqualTo(1));
            Assert.That(after.Items.Effects, Is.Empty);
            Assert.That(after.HeldEnvironment.SameValues(CharacterHeldEnvironmentState.Empty), Is.True);
            Assert.That(after.StageRecord.SameValues(before.StageRecord), Is.True);
            Assert.That(after.Experience.SameValues(before.Experience), Is.True);
            Assert.That(before.StatusEffects.Entries, Has.Count.EqualTo(7));
        }

        [TestCase(0)]
        [TestCase(7)]
        public void SameTurnRedeploy_DoesNotRefreshActionsTriggersOrShields(int remaining)
        {
            var input = TraitCombatFixtures.Equip(MovementUndoTestData.Route(), EntityKey.Unit_Akimbo,
                new SoloActionTraitSpec(10, 1, 0, 2), new SacrificeTraitSpec(1, 1));
            var actor = input.Characters.Single(c => c.EntityId == input.ActorId).With(actionUse: UnitActionUse.Attacked,
                stats: input.Characters.Single(c => c.EntityId == input.ActorId).Stats.With(traitStatuses: new TraitStatusSnapshot[] {
                    new SoloActionTraitStatusSnapshot(), new SacrificeTraitStatusSnapshot(1, 1) }));
            actor = actor.WithStatusEffects(new StatusEffectSet(new AppliedStatus[] {
                new ShieldStatus(new EffectOriginKey(EffectOwnerKind.Trait, actor.EntityId.Value, (int)TraitKey.SoloAction, "main"), remaining, true, 0) }));
            var reserved = CombatPlacementCalculator.Location(actor, CharacterRuntimeArea.Hand, 0, null, null);
            var deployed = CombatPlacementCalculator.Location(reserved, CharacterRuntimeArea.Field, null, actor.Anchor, actor.Rotation);
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId == actor.EntityId ? deployed : c).ToArray());
            var projected = input.Characters.ToDictionary(c => c.EntityId);
            TraitPresenceReconciler.Apply(input, projected);
            var after = projected[actor.EntityId];
            Assert.That(after.TraitEffects.ActiveShieldTotal, Is.Zero);
            Assert.That(after.ActionUse, Is.Not.EqualTo(UnitActionUse.Available));
            Assert.That(after.Stats.TraitStatuses.ToArray().OfType<SacrificeTraitStatusSnapshot>().Single().ActivationsThisEnemyTurn, Is.EqualTo(1));
        }

        [Test]
        public void MaxHpNormalization_CreatesNoHealOrDamageCredit_AndPreservesSoulGrowth()
        {
            var original = Dirty(Owner.Player);
            var before = new CharacterCombatFacts(original.EntityId, original.OwnedId, original.CharacterKey, original.Owner,
                original.OwnershipRole, original.Area, new HealthStateSnapshot(110, 110),
                original.Stats.With(artifactAttackBonus: 7, soulAttackBonus: 2), original.ReserveSlotIndex, original.Anchor,
                original.Rotation, original.OccupiedCells, original.ActionUse, temporaryAttackBonus: 5, temporaryMaxHealthBonus: 10,
                heldEnvironment: original.HeldEnvironment, items: original.Items, stageRecord: original.StageRecord,
                statusEffects: original.StatusEffects);
            before = PositionTileModifierCalculator.Replace(before, new PositionTileModifier(before.Anchor.Value, new[] { 51L }, 2, 5));
            var after = CombatPlacementCalculator.Location(before, CharacterRuntimeArea.Hand, 0, null, null);
            Assert.That((after.Health.CurrentHealth, after.Health.MaxHealth), Is.EqualTo((100, 100)));
            Assert.That((after.TemporaryAttackBonus, after.TemporaryMaxHealthBonus), Is.EqualTo((0, 0)));
            Assert.That((after.Stats.SoulAttackBonus, after.Stats.ArtifactAttackBonus), Is.EqualTo((2, 2)));
            Assert.That(after.StageRecord.SameValues(before.StageRecord), Is.True);
        }

        internal static CharacterCombatFacts Dirty(Owner owner)
        {
            var actor = CombatCommandTestData.Character(20, EntityKey.Unit_Demon, new CellCoord(2, 1), owner,
                actionUse: UnitActionUse.Attacked);
            var statuses = new StatusEffectSet(new AppliedStatus[] {
                new AttackIncreaseStatus(Origin(TraitKey.Vanguard), 4, 2),
                new AttackDecreaseStatus(Origin(TraitKey.Intimidation), 20),
                new DefenseDecreaseStatus(Origin(TraitKey.ArmorBreaker), 30),
                new ActionRestrictionStatus(StatusEffectKey.Disarm, Origin(TraitKey.Disarm), 2),
                new ActionRestrictionStatus(StatusEffectKey.Bind, Origin(TraitKey.AnkleBreaker), 2),
                new WeaknessExposureStatus(Origin(TraitKey.ReconSquad), 10, WeaknessExposureLifetimeKind.UntilReserveReturnOrDeath),
                new ShieldStatus(Origin(TraitKey.Cohesion), 7, true, 4) });
            return actor.With(stats: actor.Stats.With(traitAttackBonus: 12, traitStatuses: new TraitStatusSnapshot[] {
                    new AccelerationTraitStatusSnapshot(3), new RegenerationTraitStatusSnapshot(10, 2, 1),
                    new StationaryFireTraitStatusSnapshot(true), new HunterTraitStatusSnapshot(new EntityId(99), 2),
                    new SacrificeTraitStatusSnapshot(3, 1) }), heldEnvironment: new CharacterHeldEnvironmentState(41, true, 7),
                items: new CharacterItemState(1, new[] { new ItemTemporaryEffect(4, ItemStatKind.PhysicalDefense, 5, 0) }))
                .WithStatusEffects(statuses);
        }

        private static EffectOriginKey Origin(TraitKey key) => new(EffectOwnerKind.Trait, 90, (int)key, "main");
    }
}
