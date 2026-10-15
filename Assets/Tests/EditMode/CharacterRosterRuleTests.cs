using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Tests.Core
{
    public sealed class CharacterRosterRuleTests
    {
        [Test]
        public void InitialRoster_PlansOneMainThenThreeSubsInInputOrder()
        {
            var main = new CharacterDefinitionFacts(EntityKey.Unit_MainCharacter,
                UnitRole.MainCharacter, UnitOwnerEligibility.PlayerOnly,
                new MainCharacterPersistentState(10, 10, 5, 4, 3, 1));
            var decision = CharacterRosterRules.PlanInitialRoster(main, new[]
            {
                Sub(EntityKey.Unit_Sniper), Sub(EntityKey.Unit_Akimbo), Sub(EntityKey.Unit_Akimbo)
            }, true);

            Assert.That(decision.Succeeded, Is.True);
            Assert.That(decision.Requests.Select(x => x.CharacterKey), Is.EqualTo(new[]
            {
                EntityKey.Unit_MainCharacter, EntityKey.Unit_Sniper, EntityKey.Unit_Akimbo, EntityKey.Unit_Akimbo
            }));
            Assert.That(decision.Requests.Select(x => x.Role), Is.EqualTo(new[]
            {
                CharacterOwnershipRole.Main, CharacterOwnershipRole.Sub,
                CharacterOwnershipRole.Sub, CharacterOwnershipRole.Sub
            }));
        }

        [TestCase(UnitRole.MainCharacter, UnitOwnerEligibility.PlayerOnly)]
        [TestCase(UnitRole.Regular, UnitOwnerEligibility.EnemyOnly)]
        public void Acquisition_RejectsNonPlayerRegularDefinitions(UnitRole role, UnitOwnerEligibility owner)
        {
            var decision = CharacterRosterRules.PlanAcquireSub(new CharacterDefinitionFacts(
                EntityKey.Unit_Demon, role, owner));
            Assert.That(decision.FailureReason, Is.EqualTo(RosterAcquireFailureReason.InvalidDefinition));
            Assert.That(decision.Requests, Is.Empty);
        }

        [TestCase(0, 0)]
        [TestCase(2, 2)]
        [TestCase(4, 3)]
        public void InitialDrawPlan_TakesAtMostThreeSubs(int subs, int initialCount)
        {
            InitialDrawPlan plan = InitialDrawCalculator.Plan(Candidates(subs), 42);
            Assert.That(plan.Order.Count, Is.EqualTo(subs));
            Assert.That(plan.InitialCount, Is.EqualTo(initialCount));
        }

        [TestCase(4, 5, 3, TestName = "Default_Main1Sub3")]
        [TestCase(3, 5, 2, TestName = "Smaller_Main1Sub2")]
        [TestCase(1, 5, 0, TestName = "MainOnly")]
        [TestCase(6, 2, 2, TestName = "LimitedByDeck")]
        public void InitialDrawPlan_DrawsStartingHandSizeMinusMain(int startingHandSize, int subs, int initialCount)
        {
            // 첫 손패 = 메인 1장 + 서브(startingHandSize - 1)장. 덱이 모자라면 덱 크기까지만 뽑는다.
            InitialDrawPlan plan = InitialDrawCalculator.Plan(Candidates(subs), 42, startingHandSize);
            Assert.That(plan.InitialCount, Is.EqualTo(initialCount));
            Assert.Throws<ArgumentOutOfRangeException>(() => InitialDrawCalculator.Plan(Candidates(subs), 42, 0));
        }

        [Test]
        public void Draw_ShufflesStableIdsDeterministicallyWithoutDuplicates()
        {
            var candidates = Candidates(5);
            var first = InitialDrawCalculator.Plan(candidates, 42);
            var reordered = InitialDrawCalculator.Plan(candidates.Reverse().ToArray(), 42);
            Assert.That(first.Order, Is.EquivalentTo(candidates.Select(x => x.Id)));
            Assert.That(reordered.Order, Is.EqualTo(first.Order));
            Assert.That(first.Order.Distinct().Count(), Is.EqualTo(5));
            Assert.That(candidates.Select(x => x.Id.Value), Is.EqualTo(new[] { 1, 2, 3, 4, 5 }));
        }

        [Test]
        public void Draw_RejectsDuplicateNullAndEmptyIds()
        {
            var candidate = Candidates(1)[0];
            Assert.Throws<ArgumentException>(() => InitialDrawCalculator.Plan(new[] { candidate, candidate }, 42));
            Assert.Throws<ArgumentException>(() => InitialDrawCalculator.Plan(new StageDrawCandidate[] { null }, 42));
            Assert.Throws<ArgumentException>(() => new StageDrawCandidate(default, EntityKey.Unit_Akimbo));
        }

        [Test]
        public void Shop_UsesLegacySeededOfferSelectionAndValidatesUnselectedCandidates()
        {
            var candidates = new[]
            {
                new ShopCandidateSpec(EntityKey.Unit_Akimbo, 5),
                new ShopCandidateSpec(EntityKey.Unit_Sniper, 9)
            };
            Assert.That(CharacterShopRules.TryPlanOffers(candidates,
                new[] { Sub(EntityKey.Unit_Akimbo), Sub(EntityKey.Unit_Sniper) }, 76514027,
                out var offers), Is.True);
            Assert.That(offers.Select(x => x.CharacterKey), Is.EqualTo(new[]
            {
                EntityKey.Unit_Akimbo, EntityKey.Unit_Sniper, EntityKey.Unit_Sniper
            }));

            Assert.That(CharacterShopRules.TryPlanOffers(candidates,
                new[] { Sub(EntityKey.Unit_Akimbo), new CharacterDefinitionFacts(
                    EntityKey.Unit_Sniper, UnitRole.Regular, UnitOwnerEligibility.EnemyOnly) }, 42,
                out var invalid), Is.False);
            Assert.That(invalid, Is.Null);
        }

        private static CharacterDefinitionFacts Sub(EntityKey key) =>
            new(key, UnitRole.Regular, UnitOwnerEligibility.Both);

        private static StageDrawCandidate[] Candidates(int count) =>
            Enumerable.Range(1, count).Select(i => new StageDrawCandidate(
                new CharacterInstanceId(i), EntityKey.Unit_Akimbo)).ToArray();
    }
}
