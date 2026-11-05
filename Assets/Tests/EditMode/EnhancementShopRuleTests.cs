using System;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Shop;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Shop;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Tests.Core
{
    public sealed class EnhancementShopRuleTests
    {
        [TestCase(CharacterOwnershipRole.Sub)]
        [TestCase(CharacterOwnershipRole.Main)]
        public void LegacyEnhancement_IsUnavailableRegardlessOfTargetRole(CharacterOwnershipRole role)
        {
            var record = new OwnedCharacterRecord(new CharacterInstanceId(1), EntityKey.Unit_Akimbo, role);
            var offer = ShopPurchaseSnapshot.ForEnhancement(new ShopOfferId(1), record.Id, record.CharacterKey,
                new OwnedEnhancement(EnhancementKey.ChangeRider, 1), 700, "Legacy", "Legacy");
            var decision = ShopPurchaseRules.Plan(new ShopPurchaseInput(offer, false, 700, new[] { record }, null,
                new MainCharacterPersistentState(10, 10, 5), ItemInventorySnapshot.Empty, Array.Empty<ItemDefinition>(),
                new Dictionary<EntityKey, CharacterDefinitionFacts>()));

            Assert.That(decision.FailureReason, Is.EqualTo(ShopFailureReason.SubTraitPurchaseUnavailable));
            Assert.That(decision.UnitRequest, Is.Null);
            Assert.That(decision.ItemChange, Is.Null);
            Assert.That(record.Growth.IsEmpty, Is.True);
        }
    }
}
