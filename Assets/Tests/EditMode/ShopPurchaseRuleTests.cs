using System;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Shop;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Shop;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Tests.Core
{
    public sealed class ShopPurchaseRuleTests
    {
        [TestCase(0)]
        [TestCase(2)]
        public void Rope_GrantsOneAtFrozenPrice(int beforeCount)
        {
            var inventory = new ItemInventorySnapshot(new Dictionary<int, int> { [3] = beforeCount, [4] = 7 });
            var input = Input(ShopPurchaseSnapshot.ForItem(new ShopOfferId(1), 3, 200, "Rope", ""), 250, inventory: inventory);
            var decision = ShopPurchaseRules.Plan(input);
            Assert.That(decision.Succeeded, Is.True);
            Assert.That(decision.FinalBalance, Is.EqualTo(50));
            Assert.That(decision.ItemChange.Final.GetCount(3), Is.EqualTo(beforeCount + 1));
            Assert.That(decision.ItemChange.Final.GetCount(4), Is.EqualTo(7));
            Assert.That(decision.ItemChange.Expected, Is.SameAs(inventory));
            Assert.That(inventory.GetCount(3), Is.EqualTo(beforeCount));
            Assert.That(decision.UnitRequest, Is.Null);
        }

        [Test]
        public void Unit_UsesFrozenPriceAndLevel_WithoutMutatingRoster()
        {
            var decision = ShopPurchaseRules.Plan(Input(
                ShopPurchaseSnapshot.ForUnit(new ShopOfferId(1), EntityKey.Unit_Sniper, 600, 3), 700));
            Assert.That(decision.Succeeded, Is.True);
            Assert.That(decision.FinalBalance, Is.EqualTo(100));
            Assert.That(decision.UnitRequest.CharacterKey, Is.EqualTo(EntityKey.Unit_Sniper));
            Assert.That(decision.UnitRequest.InitialLevel, Is.EqualTo(3));
            Assert.That(decision.UnitRequest.Role, Is.EqualTo(CharacterOwnershipRole.Sub));
            Assert.That(decision.ItemChange, Is.Null);
        }

        /// <summary>중복 서브 금지 설계 U1·U5: 카테고리 칸이 남아 있어도 이미 가진 서브는 다시 살 수 없다.</summary>
        [Test]
        public void OwnedSub_BelowCategoryLimitIsRejected()
        {
            var owned = new[] { new OwnedCharacterRecord(new CharacterInstanceId(99), EntityKey.Unit_Sniper, CharacterOwnershipRole.Sub, 8) };
            var decision = ShopPurchaseRules.Plan(Input(
                ShopPurchaseSnapshot.ForUnit(new ShopOfferId(1), EntityKey.Unit_Sniper, 600, 3), 700, owned: owned));
            AssertFailure(decision, ShopFailureReason.OwnershipLimitReached);
            Assert.That(owned[0].Level, Is.EqualTo(8));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void UsedVisit_RejectsAnyProduct(bool item)
        {
            var offer = item ? ShopPurchaseSnapshot.ForItem(new ShopOfferId(2), 3, 200, "Rope", "")
                : ShopPurchaseSnapshot.ForUnit(new ShopOfferId(2), EntityKey.Unit_Sniper, 600, 1);
            AssertFailure(ShopPurchaseRules.Plan(Input(offer, 1000, used: true)), ShopFailureReason.PurchaseAlreadyUsed);
        }

        [Test]
        public void InsufficientFunds_AndOverflowLeaveInventoryUntouched()
        {
            var offer = ShopPurchaseSnapshot.ForItem(new ShopOfferId(1), 3, 200, "Rope", "");
            AssertFailure(ShopPurchaseRules.Plan(Input(offer, 199)), ShopFailureReason.InsufficientFunds);
            var inventory = new ItemInventorySnapshot(new Dictionary<int, int> { [3] = int.MaxValue });
            AssertFailure(ShopPurchaseRules.Plan(Input(offer, 200, inventory: inventory)), ShopFailureReason.ItemQuantityOverflow);
            Assert.That(inventory.GetCount(3), Is.EqualTo(int.MaxValue));
        }

        [Test]
        public void MissingOfferOrDefinition_IsRejected()
        {
            AssertFailure(ShopPurchaseRules.Plan(Input(null, 1000)), ShopFailureReason.UnknownOffer);
            var offer = ShopPurchaseSnapshot.ForItem(new ShopOfferId(1), 42, 100, "Unknown", "");
            AssertFailure(ShopPurchaseRules.Plan(Input(offer, 1000)), ShopFailureReason.InvalidItem);
            var unitOffer = ShopPurchaseSnapshot.ForUnit(new ShopOfferId(1), EntityKey.Unit_Sniper, 600, 3);
            var wrongUnit = new CharacterDefinitionFacts(EntityKey.Unit_Akimbo, UnitRole.Regular, UnitOwnerEligibility.Both);
            AssertFailure(ShopPurchaseRules.Plan(Input(unitOffer, 1000, unit: wrongUnit)), ShopFailureReason.InvalidCharacter);
        }

        [Test]
        public void WhiskeyAtNinetyNineGold_DoesNotProduceAGrant()
        {
            var offer = ShopPurchaseSnapshot.ForItem(new ShopOfferId(1), 1, 100, "Whiskey", "Heal");
            var input = new ShopPurchaseInput(offer, false, 99, Array.Empty<OwnedCharacterRecord>(), null,
                new MainCharacterPersistentState(10, 10, 5),
                ItemInventorySnapshot.Empty, new[] { new ItemDefinition(1, new WhiskeyItemSpec(50, 20)) }, new Dictionary<EntityKey, CharacterDefinitionFacts>());
            AssertFailure(ShopPurchaseRules.Plan(input), ShopFailureReason.InsufficientFunds);
            Assert.That(input.Inventory.GetCount(1), Is.Zero);
        }

        [Test]
        public void MissingUnitDefinition_IsAnOrdinaryRejection()
        {
            var normal = Input(ShopPurchaseSnapshot.ForUnit(new ShopOfferId(1), EntityKey.Unit_Sniper, 600, 3), 700);
            var input = new ShopPurchaseInput(normal.Offer, false, 700, normal.Owned, null, normal.Main,
                normal.Inventory, normal.Items, new Dictionary<EntityKey, CharacterDefinitionFacts>());
            AssertFailure(ShopPurchaseRules.Plan(input), ShopFailureReason.InvalidCharacter);
        }

        [TestCase(UnitRole.Regular, UnitOwnerEligibility.EnemyOnly)]
        [TestCase(UnitRole.MainCharacter, UnitOwnerEligibility.PlayerOnly)]
        public void InvalidUnitEligibility_IsRejected(UnitRole role, UnitOwnerEligibility eligibility)
        {
            var offer = ShopPurchaseSnapshot.ForUnit(new ShopOfferId(1), EntityKey.Unit_Sniper, 600, 1);
            AssertFailure(ShopPurchaseRules.Plan(Input(offer, 1000,
                unit: new CharacterDefinitionFacts(EntityKey.Unit_Sniper, role, eligibility))), ShopFailureReason.InvalidCharacter);
        }

        [TestCase(0)]
        [TestCase(1)]
        public void MissingOrDeadMain_CannotPurchase(int variant)
        {
            var normal = Input(ShopPurchaseSnapshot.ForItem(new ShopOfferId(1), 3, 200, "Rope", ""), 1000);
            var input = new ShopPurchaseInput(normal.Offer, false, normal.Balance, normal.Owned, normal.Unit,
                variant == 0 ? null : new MainCharacterPersistentState(0, 10, 5),
                normal.Inventory, normal.Items, normal.Definitions);
            AssertFailure(ShopPurchaseRules.Plan(input), ShopFailureReason.WrongPhase);
        }

        [Test]
        public void FreeItem_CanBeGrantedWithZeroBalance()
        {
            var decision = ShopPurchaseRules.Plan(Input(ShopPurchaseSnapshot.ForItem(new ShopOfferId(1), 3, 0, "Rope", ""), 0));
            Assert.That(decision.Succeeded, Is.True);
            Assert.That(decision.FinalBalance, Is.Zero);
            Assert.That(decision.ItemChange.Final.GetCount(3), Is.EqualTo(1));
        }

        private static ShopPurchaseInput Input(ShopPurchaseSnapshot offer, int balance, bool used = false,
            IReadOnlyList<OwnedCharacterRecord> owned = null, ItemInventorySnapshot inventory = null, CharacterDefinitionFacts unit = null)
        {
            return new ShopPurchaseInput(offer, used, balance, owned ?? Array.Empty<OwnedCharacterRecord>(),
                unit ?? new CharacterDefinitionFacts(EntityKey.Unit_Sniper, UnitRole.Regular, UnitOwnerEligibility.Both, category: CategoryKey.Cannon),
                new MainCharacterPersistentState(10, 10, 5),
                inventory ?? ItemInventorySnapshot.Empty,
                new[] { new ItemDefinition(3, new RopeItemSpec(5)) },
                new Dictionary<EntityKey, CharacterDefinitionFacts> {
                    [EntityKey.Unit_Sniper] = new CharacterDefinitionFacts(EntityKey.Unit_Sniper, UnitRole.Regular,
                        UnitOwnerEligibility.Both, category: CategoryKey.Cannon) });
        }

        private static void AssertFailure(ShopPurchaseDecision decision, ShopFailureReason reason)
        {
            Assert.That(decision.Succeeded, Is.False);
            Assert.That(decision.FailureReason, Is.EqualTo(reason));
            Assert.That(decision.UnitRequest, Is.Null);
            Assert.That(decision.ItemChange, Is.Null);
        }
    }
}
