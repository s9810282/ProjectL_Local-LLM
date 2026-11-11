using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Items;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;

namespace ProjectL.Tests.Core
{
    public sealed class CombatItemCopyTests
    {
        private static CharacterCombatFacts Equipped()
        {
            var c = CombatCommandTestData.Input().Characters[0];
            return ItemCharacterChanges.WithItems(c, new CharacterItemState(1,
                new[] { new ItemTemporaryEffect(3, ItemStatKind.Priority, 10) }));
        }

        [Test]
        public void Move_PreservesEquipmentAndTargetEffects()
        {
            var before = Equipped();
            var after = CombatMovementCalculator.MoveTo(before, new CellCoord(2, 3));
            Assert.That(after.Anchor, Is.EqualTo(new CellCoord(2, 3)));
            Assert.That(after.Items.SameValues(before.Items), Is.True);
        }

        [Test]
        public void UnrelatedHealthHeldTraitAndPositionCopies_PreserveItems()
        {
            var before = Equipped();
            var healed = CharacterSettlementCalculator.SetHealth(before, null, 50).Characters.Single().Final;
            Assert.That(healed.Health.CurrentHealth, Is.EqualTo(50));
            var held = HeldEnvironmentEffects.GrantSupplies(before, 2);
            Assert.That(held.HeldEnvironment.SuppliesAttackBonus, Is.EqualTo(2));
            var debuffed = CombatDebuffApplication.WithDebuffs(before, new CharacterDebuffState());
            var position = PositionTileModifierCalculator.Replace(before,
                new PositionTileModifier(before.Anchor, new long[] { 1 }, 2, 3));
            Assert.That(position.Health.MaxHealth, Is.EqualTo(103));
            foreach (var c in new[] { healed, held, debuffed, position,
                CombatCommandCalculator.Hurt(before, 1), before.With(actionUse: UnitActionUse.Available) })
                Assert.That(c.Items.SameValues(before.Items), Is.True);
        }

        [Test]
        public void SoulCollection_PreservesItemsWhileGrowingHealth()
        {
            var before = Equipped();
            var owned = new OwnedCharacterRecord(before.OwnedId.Value, before.CharacterKey, before.OwnershipRole.Value);
            var field = new SoulFieldSnapshot(2, new[] { new SoulFieldFacts(1, new ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects.EffectOriginKey(ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects.EffectOwnerKind.Trait, 1, 1, "Test"), before.Anchor.Value, 1, 1) });
            var changes = new SoulEffectCalculator(new ArtifactLoadoutSnapshot(0, Array.Empty<ArtifactPlacedEffectSnapshot>()))
                .Collect(new SoulCollectorInput(before, owned, 0, 100), field, true);
            var after = changes.Characters.Characters.Single().Final;
            Assert.That(after.Health.MaxHealth, Is.EqualTo(101));
            Assert.That(after.Items.SameValues(before.Items), Is.True);
        }

        [Test]
        public void StateCopies_PreserveIndependentItemRandomStateAndCompareIt()
        {
            var s = CombatCommandTestData.Input().State;
            var seeded = new CombatCommandState(s.BoardWidth,s.BoardHeight,s.Phase,s.Structures,s.ThornFields,s.Turn,s.Environment,                s.EnvironmentProgress,s.WallBlocking,s.Objective,s.Allocation,itemRandomState: 123);
            Assert.That(seeded.SameValues(s), Is.False);
            var changed = CombatTurnCalculator.State(seeded, seeded.Turn, seeded.Phase);
            Assert.That(changed.ItemRandomState, Is.EqualTo(123));
        }
    }
}
