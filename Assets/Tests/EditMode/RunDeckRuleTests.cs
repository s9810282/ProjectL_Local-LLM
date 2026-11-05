using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Deck;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Tests.Core
{
    public sealed class RunDeckRuleTests
    {
        private static CharacterInstanceId Id(int value) => new CharacterInstanceId(value);
        private static CharacterInstanceId[] Ids(params int[] values) => values.Select(Id).ToArray();

        private static Dictionary<EntityKey, CharacterDefinitionFacts> Definitions() => new Dictionary<EntityKey, CharacterDefinitionFacts>
        {
            [EntityKey.Unit_Akimbo] = Facts(EntityKey.Unit_Akimbo, UnitRole.MainCharacter, CategoryKey.Main),
            [EntityKey.Unit_Cowboy] = Facts(EntityKey.Unit_Cowboy, UnitRole.MainCharacter, CategoryKey.Main),
            [EntityKey.Unit_CactusCannon] = Facts(EntityKey.Unit_CactusCannon, UnitRole.Regular, CategoryKey.Cannon),
            [EntityKey.Unit_ShotGun] = Facts(EntityKey.Unit_ShotGun, UnitRole.Regular, CategoryKey.Heavy),
            [EntityKey.Unit_Ballista] = Facts(EntityKey.Unit_Ballista, UnitRole.Regular, CategoryKey.Cannon),
            [EntityKey.Unit_Shielder] = Facts(EntityKey.Unit_Shielder, UnitRole.Regular, CategoryKey.Heavy),
        };

        private static CharacterDefinitionFacts Facts(EntityKey key, UnitRole role, CategoryKey category) =>
            new CharacterDefinitionFacts(key, role, UnitOwnerEligibility.PlayerOnly, category: category);

        private static OwnedCharacterRecord[] Owned() => new[]
        {
            new OwnedCharacterRecord(Id(1), EntityKey.Unit_Akimbo, CharacterOwnershipRole.Main),
            new OwnedCharacterRecord(Id(2), EntityKey.Unit_CactusCannon, CharacterOwnershipRole.Sub),
            new OwnedCharacterRecord(Id(3), EntityKey.Unit_ShotGun, CharacterOwnershipRole.Sub),
            new OwnedCharacterRecord(Id(4), EntityKey.Unit_Ballista, CharacterOwnershipRole.Sub),
            new OwnedCharacterRecord(Id(5), EntityKey.Unit_Shielder, CharacterOwnershipRole.Sub),
            // 두 번째 Main은 운영 역할을 바꾸지 않는 테스트 전용 정의다.
            new OwnedCharacterRecord(Id(6), EntityKey.Unit_Cowboy, CharacterOwnershipRole.Main),
        };

        [Test]
        public void Evaluate_NormalizesMainAndPreservesSubOrder_Idempotently()
        {
            var requested = Ids(2, 1, 3);
            var result = DeckRules.Evaluate(requested, Owned(), Definitions());
            Assert.That(result.Accepted, Is.True);
            Assert.That(result.CanonicalIds, Is.EqualTo(Ids(1, 2, 3)));
            Assert.That(requested, Is.EqualTo(Ids(2, 1, 3)));
            Assert.That(DeckRules.Evaluate(result.CanonicalIds, Owned(), Definitions()).CanonicalIds,
                Is.EqualTo(result.CanonicalIds));
        }

        [TestCase(new[] { 1 })]
        [TestCase(new[] { 2, 3, 4, 1 })]
        public void Evaluate_AcceptsOneThroughFourCards(int[] values)
        {
            var result = DeckRules.Evaluate(Ids(values), Owned(), Definitions());
            Assert.That(result.Accepted, Is.True);
            Assert.That(result.CanonicalIds[0], Is.EqualTo(Id(1)));
        }

        [TestCase(new int[0], DeckFailureReason.EmptyDeck)]
        [TestCase(new[] { 2 }, DeckFailureReason.MissingMain)]
        [TestCase(new[] { 1, 6 }, DeckFailureReason.MultipleMains)]
        [TestCase(new[] { 1, 2, 3, 4, 5 }, DeckFailureReason.TooManyCards)]
        [TestCase(new[] { 1, 99 }, DeckFailureReason.UnknownOwnedId)]
        [TestCase(new[] { 1, 1 }, DeckFailureReason.DuplicateOwnedId)]
        public void Evaluate_RejectsInvalidSelection(int[] values, DeckFailureReason reason)
        {
            var result = DeckRules.Evaluate(Ids(values), Owned(), Definitions());
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Reason, Is.EqualTo(reason));
            Assert.That(result.CanonicalIds, Is.Null);
        }

        [TestCase("Missing")]
        [TestCase("Enemy")]
        [TestCase("RoleMismatch")]
        [TestCase("Retired")]
        public void Evaluate_RejectsUnsupportedDefinitionOrRole(string kind)
        {
            var definitions = Definitions();
            var owned = Owned();
            if (kind == "Missing") definitions.Remove(EntityKey.Unit_Akimbo);
            if (kind == "Enemy") definitions[EntityKey.Unit_Akimbo] = new CharacterDefinitionFacts(
                EntityKey.Unit_Akimbo, UnitRole.MainCharacter, UnitOwnerEligibility.EnemyOnly, category: CategoryKey.Main);
            if (kind == "RoleMismatch") owned[0] = new OwnedCharacterRecord(Id(1), EntityKey.Unit_Akimbo, CharacterOwnershipRole.Sub);
            if (kind == "Retired")
            {
                definitions[EntityKey.Unit_MainCharacter] = Facts(EntityKey.Unit_MainCharacter, UnitRole.MainCharacter, CategoryKey.Main);
                owned[0] = new OwnedCharacterRecord(Id(1), EntityKey.Unit_MainCharacter, CharacterOwnershipRole.Main);
            }
            Assert.That(DeckRules.Evaluate(Ids(1), owned, definitions).Reason, Is.EqualTo(DeckFailureReason.UnsupportedUnit));
        }

        private static RosterAcquireRequest[] Grants() => new[]
        {
            new RosterAcquireRequest(EntityKey.Unit_Akimbo, CharacterOwnershipRole.Main),
            new RosterAcquireRequest(EntityKey.Unit_CactusCannon, CharacterOwnershipRole.Sub),
            new RosterAcquireRequest(EntityKey.Unit_ShotGun, CharacterOwnershipRole.Sub),
            new RosterAcquireRequest(EntityKey.Unit_Cowboy, CharacterOwnershipRole.Main),
        };

        [Test]
        public void Initial_NormalizesGrantIndicesWithoutAllocatingOwnedIds()
        {
            var requested = new[] { 1, 0, 2 };
            var result = DeckRules.EvaluateInitial(requested, Grants(), Definitions());
            Assert.That(result.Reason, Is.EqualTo(DeckFailureReason.None));
            Assert.That(result.CanonicalGrantIndices, Is.EqualTo(new[] { 0, 1, 2 }));
            Assert.That(requested, Is.EqualTo(new[] { 1, 0, 2 }));
            Assert.That(DeckRules.EvaluateInitial(result.CanonicalGrantIndices, Grants(), Definitions()).CanonicalGrantIndices,
                Is.EqualTo(result.CanonicalGrantIndices));
        }

        [TestCase(new int[0], DeckFailureReason.EmptyDeck)]
        [TestCase(new[] { 1 }, DeckFailureReason.MissingMain)]
        [TestCase(new[] { 0, 3 }, DeckFailureReason.MultipleMains)]
        [TestCase(new[] { 0, 1, 2, 3, 4 }, DeckFailureReason.TooManyCards)]
        [TestCase(new[] { 0, 99 }, DeckFailureReason.UnknownOwnedId)]
        [TestCase(new[] { 0, -1 }, DeckFailureReason.UnknownOwnedId)]
        [TestCase(new[] { 0, 0 }, DeckFailureReason.DuplicateOwnedId)]
        public void Initial_UsesSameRejections(int[] indices, DeckFailureReason reason)
        {
            var result = DeckRules.EvaluateInitial(indices, Grants(), Definitions());
            Assert.That(result.Reason, Is.EqualTo(reason));
            Assert.That(result.CanonicalGrantIndices, Is.Null);
        }

        [Test]
        public void Initial_InvalidGrantRejectsWholeSelection()
        {
            var grants = Grants();
            grants[1] = new RosterAcquireRequest(EntityKey.Unit_CactusCannon, CharacterOwnershipRole.Main);
            var result = DeckRules.EvaluateInitial(new[] { 0, 1, 2 }, grants, Definitions());
            Assert.That(result.Reason, Is.EqualTo(DeckFailureReason.UnsupportedUnit));
            Assert.That(result.CanonicalGrantIndices, Is.Null);
            Assert.That(grants.Length, Is.EqualTo(4));
        }
    }
}
