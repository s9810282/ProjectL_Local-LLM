using System;
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Items;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
namespace ProjectL.Tests.Core
{
    public sealed class CombatItemEquipmentTests
    {
        internal static readonly ItemDefinition[] Definitions = {
            new ItemDefinition(1, new WhiskeyItemSpec(50, 10)), new ItemDefinition(2, new RoketItemSpec(20, 50)) };

        [Test]
        public void EquipAndUnequip_TransferExactlyOneAndPreserveTargetEffects()
        {
            var equip = ItemEquipmentCalculator.Calculate(Input(CombatCommandKind.EquipItem, countA: 1));
            Assert.That(equip.Accepted, Is.True, equip.Reason);
            Assert.That(equip.InventoryChange.Final.GetCount(1), Is.Zero);
            Assert.That(equip.Characters.Characters.Single().Final.Items.EquippedItemKey, Is.EqualTo(1));
            Assert.That(equip.Characters.Characters.Single().Final.Items.Effects.Count, Is.EqualTo(1));
            var unequip = ItemEquipmentCalculator.Calculate(Input(CombatCommandKind.UnequipItem, equipped: 1));
            Assert.That(unequip.Accepted, Is.True, unequip.Reason);
            Assert.That(unequip.InventoryChange.Final.GetCount(1), Is.EqualTo(1));
            Assert.That(unequip.Characters.Characters.Single().Final.Items.EquippedItemKey, Is.Null);
        }

        [Test]
        public void Replacement_RefundsOldAndSpendsNewAtomically()
        {
            var result = ItemEquipmentCalculator.Calculate(Input(CombatCommandKind.ReplaceItem, key: 2, equipped: 1, countB: 1));
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.InventoryChange.Final.GetCount(1), Is.EqualTo(1));
            Assert.That(result.InventoryChange.Final.GetCount(2), Is.Zero);
            Assert.That(result.Characters.Characters.Single().Final.Items.EquippedItemKey, Is.EqualTo(2));
            var missing = ItemEquipmentCalculator.Calculate(Input(CombatCommandKind.ReplaceItem, key: 2, equipped: 1));
            Assert.That(missing.Accepted, Is.False);
            Assert.That(missing.InventoryChange, Is.Null);
            Assert.That(missing.Characters.Characters, Is.Empty);
        }

        [Test]
        public void SameKeyReplacement_WithNoSpare_IsAcceptedNoOp()
        {
            var input = Input(CombatCommandKind.ReplaceItem, equipped: 1);
            var transition = ItemEquipmentCalculator.Calculate(input);
            Assert.That(transition.Accepted, Is.True, transition.Reason);
            Assert.That(transition.InventoryChange == null || transition.InventoryChange.Expected.SameValues(transition.InventoryChange.Final), Is.True);
            Assert.That(transition.Characters.Characters, Is.Empty);
            Assert.That(input.Characters[0].Items.EquippedItemKey, Is.EqualTo(1));
        }

        [TestCase("combat", "WrongPhase")]
        [TestCase("ended", "StageEnded")]
        [TestCase("enemy", "InvalidItemTarget")]
        [TestCase("hand", "InvalidItemTarget")]
        [TestCase("dead", "InvalidItemTarget")]
        public void Equipment_RejectsIneligibleCharacterOrPhase(string mode, string reason)
        {
            var result = ItemEquipmentCalculator.Calculate(Input(CombatCommandKind.EquipItem, mode: mode, countA: 1));
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Reason, Is.EqualTo(reason));
            Assert.That(result.InventoryChange, Is.Null);
        }

        [Test]
        public void Equipment_RejectsUnknownEmptyOccupiedAndRefundOverflow()
        {
            Assert.That(ItemEquipmentCalculator.Calculate(Input(CombatCommandKind.EquipItem, key: 99)).Reason, Is.EqualTo("UnknownItem"));
            Assert.That(ItemEquipmentCalculator.Calculate(Input(CombatCommandKind.UnequipItem)).Reason, Is.EqualTo("EmptyItemSlot"));
            Assert.That(ItemEquipmentCalculator.Calculate(Input(CombatCommandKind.EquipItem, equipped: 1, countA: 1)).Reason, Is.EqualTo("OccupiedItemSlot"));
            Assert.That(ItemEquipmentCalculator.Calculate(Input(CombatCommandKind.ReplaceItem)).Reason, Is.EqualTo("EmptyItemSlot"));
            var overflow = ItemEquipmentCalculator.Calculate(Input(CombatCommandKind.ReplaceItem, key: 2, equipped: 1, countA: int.MaxValue, countB: 1));
            Assert.That(overflow.Reason, Is.EqualTo("QuantityOverflow"));
            Assert.That(overflow.InventoryChange, Is.Null);
        }

        [Test]
        public void ReturnToHand_RefundsEquipmentInTheSameTransition()
        {
            var input = Input(CombatCommandKind.ReturnUnit, equipped: 1);
            var transition = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(transition.Accepted, Is.True, transition.Reason);
            Assert.That(transition.InventoryChange.Final.GetCount(1), Is.EqualTo(1));
            var final = transition.Characters.Characters.Single().Final;
            Assert.That(final.Area, Is.EqualTo(CharacterRuntimeArea.Hand));
            Assert.That(final.Items.EquippedItemKey, Is.Null);
            Assert.That(transition.Events.OfType<CombatItemEvent>().Single().Reason, Is.EqualTo(ItemEventReason.ReturnedToHand));
        }

        private static CombatCommandInput Input(CombatCommandKind kind, int key = 1, int? equipped = null,
            int countA = 0, int countB = 0, string mode = "")
        {
            var original = CombatCommandTestData.Input();
            var characters = original.Characters.ToArray();
            var actor = mode == "enemy" ? characters[2] : characters[0];
            if (mode == "hand") actor = CombatPlacementCalculator.Location(actor, CharacterRuntimeArea.Hand, 0, null, null);
            if (mode == "dead") actor = CombatCommandCalculator.Hurt(actor, 100);
            actor = ItemCharacterChanges.WithItems(actor, new CharacterItemState(equipped,
                new[] { new ItemTemporaryEffect(4, ItemStatKind.PhysicalDefense, 5) }));
            characters[Array.FindIndex(characters, c => c.EntityId == actor.EntityId)] = actor;
            var state = new CombatCommandState(8,8,mode == "ended" ? CombatFlowPhase.Finished : CombatFlowPhase.PlayerInput,Array.Empty<StructureCombatFacts>(),new CombatTurnFacts(mode == "combat" ? StagePhase.Combat : StagePhase.Deployment,
                    controllerPhase: mode == "combat" ? CombatControllerPhase.Player : CombatControllerPhase.None));
            return new CombatCommandInput(1, kind, actor.EntityId, default, state, characters, original.Profiles,
                original.Main, original.Owned, original.SoulCounts, original.Souls, original.Artifacts,
                battle: new CombatBattleFacts(10, 1, 0, 0, Array.Empty<CharacterInstanceId>(), 0),
                itemKey: kind == CombatCommandKind.UnequipItem ? null : key,
                itemInventory: new ItemInventorySnapshot(new Dictionary<int, int> { [1] = countA, [2] = countB }),
                itemDefinitions: Definitions,
                expectedBattleInstanceId: 0, expectedControllerTurnSequence: (state).Turn.ControllerTurnSequence);
        }
    }
}
