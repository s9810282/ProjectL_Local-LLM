using System;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Tests.Core
{
    public sealed class CharacterRosterContractTests
    {
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

        [TestCase(-1, 10, 2, 3, 4, 1)]
        [TestCase(0, 0, 2, 3, 4, 1)]
        [TestCase(0, -1, 2, 3, 4, 1)]
        [TestCase(0, 10, -1, 3, 4, 1)]
        [TestCase(0, 10, 2, -1, 4, 1)]
        [TestCase(0, 10, 2, 3, -1, 1)]
        [TestCase(0, 10, 2, 3, 4, 0)]
        [TestCase(0, 10, 2, 3, 4, -1)]
        public void MainState_RejectsInvalidPersistentValues(int health, int max, int attack,
            int action, int range, int moveCost)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new MainCharacterPersistentState(health, max, attack, action, range, moveCost));
        }

        [Test]
        public void MainState_PreservesHealthAboveBaseMaxUntilEffectiveMaxValidation()
        {
            var state = new MainCharacterPersistentState(12, 10, 2, 3, 4, 1);
            Assert.That(state.CurrentHealth, Is.EqualTo(12));
            Assert.That(state.PersistentMaxHealth, Is.EqualTo(10));
            Assert.That(state.PersistentAttack, Is.EqualTo(2));
            Assert.That(state.MaxActionCost, Is.EqualTo(3));
            Assert.That(state.MoveRange, Is.EqualTo(4));
            Assert.That(state.MoveCost, Is.EqualTo(1));
            Assert.DoesNotThrow(() => new MainCharacterPersistentState(0, 1, 0, 0, 0, 1));
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
        [TestCase("second-main")]
        public void AcquireResult_RejectsMalformedIssuedRoster(string invalid)
        {
            OwnedCharacterRecord[] cards = invalid switch
            {
                "null-list" => null,
                "null-record" => new OwnedCharacterRecord[] { null },
                "duplicate-id" => new[] { Card(1), Card(1) },
                "second-main" => new[] { Card(1, CharacterOwnershipRole.Main), Card(2, CharacterOwnershipRole.Main) },
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
                if (reason == RosterAcquireFailureReason.None) continue;
                var result = RosterAcquireResult.Failure(reason);
                Assert.That(result.Succeeded, Is.False);
                Assert.That(result.FailureReason, Is.EqualTo(reason));
                Assert.That(result.Acquired, Is.Empty);
            }
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
    }
}
