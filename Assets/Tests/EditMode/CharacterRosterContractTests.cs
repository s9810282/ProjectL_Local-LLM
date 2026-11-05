using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using System;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Deck;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Tests.Core
{
    public sealed class CharacterRosterContractTests
    {
        [TestCase(3)]
        [TestCase(int.MaxValue)]
        public void ShopLevelContracts_DefaultToOneAndPreserveExplicitLevel(int level)
        {
            Assert.That(new ShopCandidateSpec(EntityKey.Unit_Akimbo, 5).InitialLevel, Is.EqualTo(1));
            Assert.That(new ShopPurchaseSnapshot(new ShopOfferId(1), EntityKey.Unit_Akimbo, 5).InitialLevel,
                Is.EqualTo(1));
            Assert.That(new ShopCandidateSpec(EntityKey.Unit_Akimbo, 5, level).InitialLevel, Is.EqualTo(level));
            Assert.That(new ShopPurchaseSnapshot(new ShopOfferId(1), EntityKey.Unit_Akimbo, 5, level).InitialLevel,
                Is.EqualTo(level));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void ShopLevelContracts_RejectNonpositiveLevels(int level)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ShopCandidateSpec(EntityKey.Unit_Akimbo, 5, level));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new ShopPurchaseSnapshot(new ShopOfferId(1), EntityKey.Unit_Akimbo, 5, level));
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(int.MinValue)]
        public void Identity_RejectsNonpositiveValues(int value)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new CharacterInstanceId(value));
        }

        [Test]
        public void Identity_PreservesEqualityOrderingAndEmptyWithoutEntityConversion()
        {
            var first = new CharacterInstanceId(1);
            var same = new CharacterInstanceId(1);
            var last = new CharacterInstanceId(int.MaxValue);
            Assert.That(default(CharacterInstanceId), Is.EqualTo(CharacterInstanceId.Empty));
            Assert.That(CharacterInstanceId.Empty.IsEmpty, Is.True);
            Assert.That(first.IsEmpty, Is.False);
            Assert.That(first == same, Is.True);
            Assert.That(first != last, Is.True);
            Assert.That(first.Equals((object)same), Is.True);
            Assert.That(first.Equals((object)new EntityId(1)), Is.False);
            Assert.That(first.GetHashCode(), Is.EqualTo(same.GetHashCode()));
            Assert.That(first.CompareTo(last), Is.LessThan(0));
            Assert.That(last.Value, Is.EqualTo(int.MaxValue));
            Assert.That(first.ToString(), Is.EqualTo("1"));
            foreach (var method in typeof(CharacterInstanceId).GetMethods())
                Assert.That(method.Name, Is.Not.EqualTo("op_Implicit").And.Not.EqualTo("op_Explicit"));
        }

        [TestCase("empty-id")]
        [TestCase("empty-key")]
        [TestCase("invalid-role")]
        public void OwnedRecord_RejectsMalformedIdentity(string invalid)
        {
            Assert.That(() => new OwnedCharacterRecord(
                invalid == "empty-id" ? CharacterInstanceId.Empty : new CharacterInstanceId(1),
                invalid == "empty-key" ? EntityKey.None : EntityKey.Unit_Akimbo,
                invalid == "invalid-role" ? (CharacterOwnershipRole)99 : CharacterOwnershipRole.Sub),
                Throws.InstanceOf<ArgumentException>());
        }

        [Test]
        public void SameKindCards_RetainDistinctOwnedIdentity()
        {
            var first = Card(1);
            var second = Card(2);
            Assert.That(first.CharacterKey, Is.EqualTo(second.CharacterKey));
            Assert.That(first.Id, Is.Not.EqualTo(second.Id));
            Assert.That(first.Role, Is.EqualTo(CharacterOwnershipRole.Sub));
        }

        [TestCase(3)]
        [TestCase(int.MaxValue)]
        public void LevelContracts_DefaultToOneAndPreserveExplicitSubLevel(int level)
        {
            var id = new CharacterInstanceId(1);
            Assert.That(new OwnedCharacterRecord(id, EntityKey.Unit_Akimbo,
                CharacterOwnershipRole.Sub).Level, Is.EqualTo(1));
            Assert.That(new RosterAcquireRequest(EntityKey.Unit_Akimbo,
                CharacterOwnershipRole.Sub).InitialLevel, Is.EqualTo(1));
            Assert.That(new OwnedCharacterRecord(id, EntityKey.Unit_Akimbo,
                CharacterOwnershipRole.Sub, level).Level, Is.EqualTo(level));
            Assert.That(new RosterAcquireRequest(EntityKey.Unit_Akimbo,
                CharacterOwnershipRole.Sub, level).InitialLevel, Is.EqualTo(level));
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(int.MinValue)]
        public void LevelContracts_RejectNonpositiveLevels(int level)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new OwnedCharacterRecord(
                new CharacterInstanceId(1), EntityKey.Unit_Akimbo, CharacterOwnershipRole.Sub, level));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RosterAcquireRequest(
                EntityKey.Unit_Akimbo, CharacterOwnershipRole.Sub, level));
        }

        [Test]
        public void LevelContracts_MainStartsAtOneButRecordCanHoldLaterLevels()
        {
            var id = new CharacterInstanceId(1);
            Assert.That(new OwnedCharacterRecord(id, EntityKey.Unit_MainCharacter,
                CharacterOwnershipRole.Main).Level, Is.EqualTo(1));
            Assert.That(new RosterAcquireRequest(EntityKey.Unit_MainCharacter,
                CharacterOwnershipRole.Main).InitialLevel, Is.EqualTo(1));
            Assert.That(new RosterAcquireRequest(EntityKey.Unit_MainCharacter,
                CharacterOwnershipRole.Main, 1).InitialLevel, Is.EqualTo(1));
            // 메인도 레벨업한다(레벨 성장 설계 G2). 보유 기록은 높은 레벨을 담고, 획득은 1에서만 시작한다.
            Assert.That(new OwnedCharacterRecord(id, EntityKey.Unit_MainCharacter,
                CharacterOwnershipRole.Main, 3).Level, Is.EqualTo(3));
            Assert.Throws<ArgumentException>(() => new RosterAcquireRequest(
                EntityKey.Unit_MainCharacter, CharacterOwnershipRole.Main, 3));
        }

        [TestCase(-1, 0)]
        [TestCase(0, -1)]
        [TestCase(2, 1)]
        public void SoulChange_RejectsNegativeOrDecreasingCounts(int expected, int final)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new SoulCountChange(new CharacterInstanceId(1), expected, final));
        }

        [Test]
        public void SoulChange_RequiresOwnedIdAndAllowsUnchangedOrMaximumCount()
        {
            Assert.Throws<ArgumentException>(() => new SoulCountChange(CharacterInstanceId.Empty, 0, 1));
            var unchanged = new SoulCountChange(new CharacterInstanceId(1), 3, 3);
            var maximum = new SoulCountChange(new CharacterInstanceId(2), 0, int.MaxValue);
            Assert.That(unchanged.ExpectedCount, Is.EqualTo(unchanged.FinalCount));
            Assert.That(maximum.Id, Is.EqualTo(new CharacterInstanceId(2)));
            Assert.That(maximum.FinalCount, Is.EqualTo(int.MaxValue));
        }

        [Test]
        public void OwnedRecord_ExperienceDefaultsToZero_RejectsNegative_AndSameValuesComparesAllFields()
        {
            var record = new OwnedCharacterRecord(new CharacterInstanceId(2), EntityKey.Unit_Akimbo,
                CharacterOwnershipRole.Sub, 2, 30);

            Assert.That(new OwnedCharacterRecord(new CharacterInstanceId(2), EntityKey.Unit_Akimbo,
                CharacterOwnershipRole.Sub).Experience, Is.Zero);
            Assert.That(record.Experience, Is.EqualTo(30));
            Assert.Throws<ArgumentOutOfRangeException>(() => new OwnedCharacterRecord(new CharacterInstanceId(2),
                EntityKey.Unit_Akimbo, CharacterOwnershipRole.Sub, 1, -1));
            Assert.That(record.SameValues(new OwnedCharacterRecord(new CharacterInstanceId(2), EntityKey.Unit_Akimbo,
                CharacterOwnershipRole.Sub, 2, 30)), Is.True);
            Assert.That(record.SameValues(new OwnedCharacterRecord(new CharacterInstanceId(2), EntityKey.Unit_Akimbo,
                CharacterOwnershipRole.Sub, 2, 31)), Is.False);
            Assert.That(record.SameValues(new OwnedCharacterRecord(new CharacterInstanceId(2), EntityKey.Unit_Akimbo,
                CharacterOwnershipRole.Sub, 3, 30)), Is.False);
            Assert.That(record.SameValues(null), Is.False);
        }

        [TestCase(2, 30, 2, 30)]
        [TestCase(2, 30, 2, 31)]
        [TestCase(2, 30, 3, 0)]
        public void OwnedChange_AllowsSameOrHigherLevelProgress(int level, int experience, int finalLevel, int finalExperience)
        {
            var change = new OwnedCharacterChange(Sub(level, experience), Sub(finalLevel, finalExperience));

            Assert.That((change.Final.Level, change.Final.Experience), Is.EqualTo((finalLevel, finalExperience)));
        }

        [Test]
        public void OwnedChange_RejectsIdentityChangeOrDecreasingProgress()
        {
            var expected = Sub(2, 30);

            Assert.Throws<ArgumentNullException>(() => new OwnedCharacterChange(null, expected));
            Assert.Throws<ArgumentNullException>(() => new OwnedCharacterChange(expected, null));
            Assert.Throws<ArgumentException>(() => new OwnedCharacterChange(expected, new OwnedCharacterRecord(
                new CharacterInstanceId(3), EntityKey.Unit_Akimbo, CharacterOwnershipRole.Sub, 2, 30)));
            Assert.Throws<ArgumentException>(() => new OwnedCharacterChange(expected, new OwnedCharacterRecord(
                new CharacterInstanceId(2), EntityKey.Unit_Sniper, CharacterOwnershipRole.Sub, 2, 30)));
            Assert.Throws<ArgumentException>(() => new OwnedCharacterChange(expected, new OwnedCharacterRecord(
                new CharacterInstanceId(2), EntityKey.Unit_Akimbo, CharacterOwnershipRole.Main, 2, 30)));
            Assert.Throws<ArgumentException>(() => new OwnedCharacterChange(expected, Sub(2, 29)));
            Assert.Throws<ArgumentException>(() => new OwnedCharacterChange(expected, Sub(1, 90)));
        }

        [Test]
        public void CharacterChanges_OwnedDefaultsEmpty_RejectsNullOrDuplicateIds()
        {
            var change = new OwnedCharacterChange(Sub(1, 0), Sub(1, 10));
            var empty = new CharacterChanges(Array.Empty<CharacterStateChange>(), Array.Empty<SoulCountChange>());

            Assert.That(empty.Owned, Is.Empty);
            Assert.That(new CharacterChanges(Array.Empty<CharacterStateChange>(), Array.Empty<SoulCountChange>(),
                owned: new[] { change }).Owned, Is.EqualTo(new[] { change }));
            Assert.Throws<ArgumentException>(() => new CharacterChanges(Array.Empty<CharacterStateChange>(),
                Array.Empty<SoulCountChange>(), owned: new OwnedCharacterChange[] { null }));
            Assert.Throws<ArgumentException>(() => new CharacterChanges(Array.Empty<CharacterStateChange>(),
                Array.Empty<SoulCountChange>(), owned: new[] { change, new OwnedCharacterChange(Sub(1, 0), Sub(1, 20)) }));
        }

        [TestCase(-1, 10, 2, 0)]
        [TestCase(0, 0, 2, 0)]
        [TestCase(0, -1, 2, 0)]
        [TestCase(0, 10, -1, 0)]
        [TestCase(0, 10, 2, -1)]
        public void MainState_RejectsInvalidPersistentValues(int health, int max, int attack, int defense)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new MainCharacterPersistentState(health, max, attack, defense));
        }

        [Test]
        public void MainState_PreservesHealthAboveBaseMaxUntilEffectiveMaxValidation()
        {
            var state = new MainCharacterPersistentState(12, 10, 2);
            Assert.That(state.CurrentHealth, Is.EqualTo(12));
            Assert.That(state.PersistentMaxHealth, Is.EqualTo(10));
            Assert.That(state.PersistentAttack, Is.EqualTo(2));
            Assert.That(state.PersistentDefense, Is.Zero);
            Assert.DoesNotThrow(() => new MainCharacterPersistentState(0, 1, 0));
        }

        [Test]
        public void AcquireRequest_ValidatesShapeWithoutIssuingIdentity()
        {
            Assert.Throws<ArgumentException>(() =>
                new RosterAcquireRequest(EntityKey.None, CharacterOwnershipRole.Sub));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new RosterAcquireRequest(EntityKey.Unit_Akimbo, (CharacterOwnershipRole)99));
            var request = new RosterAcquireRequest(EntityKey.Unit_MainCharacter, CharacterOwnershipRole.Main);
            Assert.That(request.CharacterKey, Is.EqualTo(EntityKey.Unit_MainCharacter));
            Assert.That(request.Role, Is.EqualTo(CharacterOwnershipRole.Main));
        }

        [TestCase("null-list")]
        [TestCase("null-record")]
        [TestCase("duplicate-id")]
        public void AcquireResult_RejectsMalformedIssuedRoster(string invalid)
        {
            OwnedCharacterRecord[] cards = invalid switch
            {
                "null-list" => null,
                "null-record" => new OwnedCharacterRecord[] { null },
                "duplicate-id" => new[] { Card(1), Card(1) },
                _ => throw new ArgumentOutOfRangeException(nameof(invalid))
            };
            Assert.That(() => RosterAcquireResult.Success(cards), Throws.InstanceOf<ArgumentException>());
        }

        [Test]
        public void AcquireResult_FreezesSourceAndExposesNoWritableCollection()
        {
            var original = Card(1);
            var source = new[] { original, Card(2) };
            var result = RosterAcquireResult.Success(source);
            source[0] = Card(3);
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.FailureReason, Is.EqualTo(RosterAcquireFailureReason.None));
            Assert.That(result.Acquired.Count, Is.EqualTo(2));
            Assert.That(result.Acquired[0], Is.SameAs(original));
            Assert.That(result.Acquired, Is.Not.InstanceOf<OwnedCharacterRecord[]>());
            Assert.Throws<NotSupportedException>(() => ((IList<OwnedCharacterRecord>)result.Acquired)[0] = Card(4));
        }

        [Test]
        public void AcquireResult_FailureContainsNoIssuedRecords()
        {
            foreach (RosterAcquireFailureReason reason in Enum.GetValues(typeof(RosterAcquireFailureReason)))
            {
                if (reason == RosterAcquireFailureReason.None || reason == RosterAcquireFailureReason.InvalidInitialDeck) continue;
                var result = RosterAcquireResult.Failure(reason);
                Assert.That(result.Succeeded, Is.False);
                Assert.That(result.FailureReason, Is.EqualTo(reason));
                Assert.That(result.InitialDeckFailure, Is.EqualTo(DeckFailureReason.None));
                Assert.That(result.Acquired, Is.Empty);
            }
            var deck = RosterAcquireResult.InvalidInitialDeck(DeckFailureReason.EmptyDeck);
            Assert.That(deck.FailureReason, Is.EqualTo(RosterAcquireFailureReason.InvalidInitialDeck));
            Assert.That(deck.InitialDeckFailure, Is.EqualTo(DeckFailureReason.EmptyDeck));
            Assert.That(deck.Acquired, Is.Empty);
            // 초기 덱 실패는 구체 사유 없이 만들 수 없다.
            Assert.Throws<ArgumentOutOfRangeException>(() => RosterAcquireResult.Failure(RosterAcquireFailureReason.InvalidInitialDeck));
            Assert.Throws<ArgumentOutOfRangeException>(() => RosterAcquireResult.InvalidInitialDeck(DeckFailureReason.None));
            Assert.Throws<ArgumentOutOfRangeException>(() => RosterAcquireResult.Failure(RosterAcquireFailureReason.None));
            Assert.Throws<ArgumentOutOfRangeException>(() => RosterAcquireResult.Failure((RosterAcquireFailureReason)99));
        }

        [Test]
        public void DrawCandidate_PreservesOwnedIdentityAndRejectsEmptyValues()
        {
            var id = new CharacterInstanceId(7);
            var candidate = new StageDrawCandidate(id, EntityKey.Unit_Akimbo);
            Assert.That(candidate.Id, Is.EqualTo(id));
            Assert.That(candidate.CharacterKey, Is.EqualTo(EntityKey.Unit_Akimbo));
            Assert.Throws<ArgumentException>(() => new StageDrawCandidate(CharacterInstanceId.Empty, candidate.CharacterKey));
            Assert.Throws<ArgumentException>(() => new StageDrawCandidate(id, EntityKey.None));
        }

        private static OwnedCharacterRecord Card(int id, CharacterOwnershipRole role = CharacterOwnershipRole.Sub) =>
            new OwnedCharacterRecord(new CharacterInstanceId(id),
                role == CharacterOwnershipRole.Main ? EntityKey.Unit_MainCharacter : EntityKey.Unit_Akimbo, role);

        private static OwnedCharacterRecord Sub(int level, int experience) => new(new CharacterInstanceId(2),
            EntityKey.Unit_Akimbo, CharacterOwnershipRole.Sub, level, experience);
    }
}
