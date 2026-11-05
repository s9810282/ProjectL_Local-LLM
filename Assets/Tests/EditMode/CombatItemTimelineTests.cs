using System;
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Items;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;

namespace ProjectL.Tests.Core
{
    public sealed class CombatItemTimelineTests
    {
        [TestCase(8)]
        [TestCase(20)]
        [TestCase(30)]
        public void RetainedRopeMarker_DoesNotModifyPublishedPriority(int priority)
        {
            var before = CombatItemLanternTests.Stats(CombatCommandTestData.Input().Characters[0], priority: priority);
            var marked = Mark(before);
            Assert.That(CombatEffectiveStatsCalculator.Priority(marked), Is.EqualTo(priority));
            Assert.That(marked.Items.Effects.Single().Magnitude, Is.EqualTo(10));
            Assert.That(marked.ActionUse, Is.EqualTo(before.ActionUse));
            var cleared = ItemCharacterChanges.WithItems(marked, CharacterItemState.Empty);
            Assert.That(CombatEffectiveStatsCalculator.Priority(cleared), Is.EqualTo(priority));
        }

        [Test]
        public void RetainedMarker_DoesNotAdvanceControllerOrChangeCommandState()
        {
            var input = IntimidationScheduleTests.Scheduled(priority: 20);
            var before = input.Characters.Single(c => c.EntityId.Value == 2);
            var result = IntimidationScheduleTests.Finish(input, Mark(before));
            Assert.That(result.FinalState.SameValues(input.State), Is.True);
            Assert.That(result.Events.OfType<CombatTurnEvent>(), Is.Empty);
            Assert.That(before.Items.Effects, Is.Empty);
        }

        [Test]
        public void RopeIsUnavailable_WhileOtherItemRulesRemainAvailable()
        {
            var rules = ItemRuleRegistry.CreateDefault();
            Assert.That(rules.IsAvailable(new RopeItemSpec(10)), Is.False);
            Assert.That(rules.GetUnavailableReason(new RopeItemSpec(10)), Is.EqualTo("DeferredUnitTurnTiming"));
            Assert.That(rules.IsAvailable(new WhiskeyItemSpec(50, 10)), Is.True);
            Assert.That(rules.IsAvailable(new RoketItemSpec(20, 50)), Is.True);
        }

        internal static CharacterCombatFacts Mark(CharacterCombatFacts c) => ItemCharacterChanges.WithItems(c,
            new CharacterItemState(c.Items.EquippedItemKey, c.Items.Effects.Append(new ItemTemporaryEffect(3, ItemStatKind.Priority, 10, 1)).ToArray()));
    }
}
