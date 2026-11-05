using System;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Shop;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Tests.Core
{
    public sealed class ShopProductContractTests
    {
        [Test]
        public void ItemOffer_DoesNotMasqueradeAsUnit()
        {
            var offer = ShopPurchaseSnapshot.ForItem(new ShopOfferId(1), 1, 100, "Whiskey", "Heal");
            Assert.That(offer.Kind, Is.EqualTo(ShopProductKind.ConsumableItem));
            Assert.That(offer.ItemKey, Is.EqualTo(1));
            Assert.That(offer.CharacterKey, Is.EqualTo(EntityKey.None));
            Assert.That(offer.InitialLevel, Is.Zero);
            Assert.That(offer.ItemName, Is.EqualTo("Whiskey"));
            Assert.That(offer.ItemDescription, Is.EqualTo("Heal"));
            var unit = ShopPurchaseSnapshot.ForUnit(new ShopOfferId(2), (EntityKey)1, 600, 3);
            Assert.That(unit.Kind, Is.EqualTo(ShopProductKind.Unit));
            Assert.That(unit.ItemKey, Is.Zero);
            Assert.That(unit.InitialLevel, Is.EqualTo(3));
            Assert.That(unit.ItemName, Is.Empty);
        }

        [TestCase(0)]
        [TestCase(4)]
        public void Factories_RejectOfferOutsideCapacity(int id)
        {
            var offerId = id == 0 ? ShopOfferId.Empty : new ShopOfferId(id);
            Assert.Throws<ArgumentException>(() => ShopPurchaseSnapshot.ForItem(offerId, 1, 100, "Item", ""));
            Assert.Throws<ArgumentException>(() => ShopPurchaseSnapshot.ForUnit(offerId, EntityKey.Unit_Akimbo, 600, 1));
        }

        [Test]
        public void Factories_RejectInvalidBranchValues()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ShopCandidateSpec.ForItem(0, 100, "Item", ""));
            Assert.Throws<ArgumentOutOfRangeException>(() => ShopCandidateSpec.ForItem(1, -1, "Item", ""));
            Assert.Throws<ArgumentException>(() => ShopCandidateSpec.ForItem(1, 100, " ", ""));
            Assert.Throws<ArgumentNullException>(() => ShopCandidateSpec.ForItem(1, 100, "Item", null));
            Assert.Throws<ArgumentException>(() => ShopCandidateSpec.ForUnit(EntityKey.None, 600));
            Assert.Throws<ArgumentOutOfRangeException>(() => ShopPurchaseSnapshot.ForUnit(new ShopOfferId(1), EntityKey.Unit_Akimbo, 600, 0));
        }

        [Test]
        public void UnitCompatibilityConstructors_PreserveExistingValues()
        {
            var candidate = new ShopCandidateSpec(EntityKey.Unit_Akimbo, 5, 7);
            var offer = new ShopPurchaseSnapshot(new ShopOfferId(1), candidate.CharacterKey, candidate.Cost, candidate.InitialLevel);
            Assert.That(candidate.Kind, Is.EqualTo(ShopProductKind.Unit));
            Assert.That(offer.Kind, Is.EqualTo(ShopProductKind.Unit));
            Assert.That(offer.Cost, Is.EqualTo(5));
            Assert.That(offer.InitialLevel, Is.EqualTo(7));
            Assert.That(ShopStageSnapshot.OfferCount, Is.EqualTo(ShopOfferLimits.Maximum));
            Assert.That((int)ShopFailureReason.RunEnded, Is.EqualTo(14));
        }

        [Test]
        public void VisitContext_UsesEveryIdentityComponent()
        {
            var id = new StageEntryId(Guid.NewGuid());
            var context = new ShopVisitContext(1, new StageNodeId(2), new StageKey(3), id);
            Assert.That(context, Is.EqualTo(new ShopVisitContext(1, new StageNodeId(2), new StageKey(3), id)));
            Assert.That(context.GetHashCode(), Is.EqualTo(new ShopVisitContext(1, new StageNodeId(2), new StageKey(3), id).GetHashCode()));
            Assert.That(context, Is.Not.EqualTo(new ShopVisitContext(1, new StageNodeId(2), new StageKey(3), new StageEntryId(Guid.NewGuid()))));
            Assert.That(context, Is.Not.EqualTo(new ShopVisitContext(2, new StageNodeId(2), new StageKey(3), id)));
            Assert.That(context, Is.Not.EqualTo(new ShopVisitContext(1, new StageNodeId(4), new StageKey(3), id)));
            Assert.That(context, Is.Not.EqualTo(new ShopVisitContext(1, new StageNodeId(2), new StageKey(4), id)));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ShopVisitContext(0, new StageNodeId(2), new StageKey(3), id));
            Assert.Throws<ArgumentException>(() => new ShopVisitContext(1, StageNodeId.Empty, new StageKey(3), id));
            Assert.Throws<ArgumentException>(() => new ShopVisitContext(1, new StageNodeId(2), StageKey.Empty, id));
            Assert.Throws<ArgumentException>(() => new ShopVisitContext(1, new StageNodeId(2), new StageKey(3), StageEntryId.Empty));
        }

        [Test]
        public void PurchaseInput_CopiesMutableCollections()
        {
            var owned = new List<OwnedCharacterRecord>
            {
                new OwnedCharacterRecord(new CharacterInstanceId(1), EntityKey.Unit_Akimbo, CharacterOwnershipRole.Sub)
            };
            var items = new List<ItemDefinition> { new ItemDefinition(1, new WhiskeyItemSpec(50, 20)) };
            var input = new ShopPurchaseInput(ShopPurchaseSnapshot.ForItem(new ShopOfferId(1), 1, 100, "Item", ""),
                false, 200, owned, null, null, ItemInventorySnapshot.Empty, items, new Dictionary<EntityKey, CharacterDefinitionFacts>());
            owned.Clear();
            items.Clear();
            Assert.That(input.Owned.Count, Is.EqualTo(1));
            Assert.That(input.Items.Count, Is.EqualTo(1));
            Assert.That(input.Owned, Is.Not.SameAs(owned));
            Assert.That(input.Items, Is.Not.SameAs(items));
            Assert.Throws<ArgumentException>(() => new ShopPurchaseInput(input.Offer, false, 200,
                new OwnedCharacterRecord[] { null }, null, null, ItemInventorySnapshot.Empty, input.Items, input.Definitions));
        }

        [Test]
        public void DecisionsAndResults_HaveExactlyOneSuccessfulPayload()
        {
            var request = new RosterAcquireRequest(EntityKey.Unit_Akimbo, CharacterOwnershipRole.Sub, 3);
            var unitDecision = ShopPurchaseDecision.ForUnit(100, request);
            Assert.That(unitDecision.UnitRequest, Is.SameAs(request));
            Assert.That(unitDecision.ItemChange, Is.Null);
            var change = new ItemInventoryChange(ItemInventorySnapshot.Empty,
                new ItemInventorySnapshot(new Dictionary<int, int> { [1] = 1 }));
            var itemDecision = ShopPurchaseDecision.ForItem(100, change);
            Assert.That(itemDecision.ItemChange, Is.SameAs(change));
            Assert.That(itemDecision.UnitRequest, Is.Null);
            var itemResult = ShopPurchaseResult.ForItem(1);
            Assert.That(itemResult.Succeeded, Is.True);
            Assert.That(itemResult.UnitAcquisition, Is.Null);
            Assert.That(itemResult.ItemKey, Is.EqualTo(1));
            var acquisition = RosterAcquireResult.Success(new[]
            {
                new OwnedCharacterRecord(new CharacterInstanceId(1), EntityKey.Unit_Akimbo, CharacterOwnershipRole.Sub, 3)
            });
            var unitResult = ShopPurchaseResult.ForUnit(acquisition);
            Assert.That(unitResult.UnitAcquisition, Is.SameAs(acquisition));
            Assert.That(unitResult.ItemKey, Is.Zero);
            var failure = ShopPurchaseResult.Failure(ShopFailureReason.AlreadyOwned);
            Assert.That(failure.Succeeded, Is.False);
            Assert.That(failure.Kind, Is.EqualTo(ShopProductKind.None));
            Assert.That(failure.UnitAcquisition, Is.Null);
            Assert.Throws<ArgumentOutOfRangeException>(() => ShopPurchaseDecision.Failure(ShopFailureReason.None));
            Assert.Throws<ArgumentOutOfRangeException>(() => ShopPurchaseResult.Failure((ShopFailureReason)999));
            Assert.Throws<ArgumentException>(() => ShopPurchaseResult.ForUnit(RosterAcquireResult.Success(Array.Empty<OwnedCharacterRecord>())));
            Assert.Throws<ArgumentOutOfRangeException>(() => ShopPurchaseResult.ForItem(0));
        }
    }
}
