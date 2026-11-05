using System;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class MainSpecializationStoreTests
    {
        [TestCase(CategoryKey.Rider)]
        [TestCase(CategoryKey.Heavy)]
        [TestCase(CategoryKey.Cannon)]
        public void TypedPreparationChangesOnlyClassAndOneRevision(CategoryKey category)
        {
            var buffer = CreateBuffer();
            var before = buffer.Owned[0];
            buffer.TryGetMainState(before.Id, out var main);
            var other = buffer.Owned[1];
            var store = (ICharacterBufferStore)buffer;
            var change = Change(before, category);
            var prepared = store.PrepareMainSpecialization(change);
            Assert.That(buffer.RosterRevision, Is.EqualTo(1));
            Assert.That(buffer.Owned[0], Is.SameAs(before));
            store.InstallMainSpecialization(prepared);
            Assert.That(buffer.RosterRevision, Is.EqualTo(2));
            Assert.That(buffer.Owned[0].SameValues(change.Final), Is.True);
            Assert.That(buffer.Owned[1], Is.SameAs(other));
            Assert.That(buffer.GetSoulCount(before.Id), Is.Zero);
            buffer.TryGetMainState(before.Id, out var afterMain);
            Assert.That(afterMain, Is.SameAs(main));
            Assert.That((afterMain.CurrentHealth, afterMain.PersistentMaxHealth, afterMain.PersistentAttack, afterMain.PersistentDefense),
                Is.EqualTo((7, 13, 11, 5)));
            Assert.That(buffer.Characters, Is.Empty);
            Assert.Throws<InvalidOperationException>(() => store.InstallMainSpecialization(prepared));
            Assert.That(buffer.RosterRevision, Is.EqualTo(2));
        }

        [Test]
        public void StaleExpectedAndRevisionRejectWithoutMutation()
        {
            var buffer = CreateBuffer();
            var store = (ICharacterBufferStore)buffer;
            var before = buffer.Owned[0];
            var stale = new OwnedCharacterRecord(before.Id, before.CharacterKey, before.Role, experience: 1);
            Assert.Throws<InvalidOperationException>(() => store.PrepareMainSpecialization(Change(stale, CategoryKey.Rider)));
            Assert.That(buffer.Owned[0], Is.SameAs(before));
            var prepared = store.PrepareMainSpecialization(Change(before, CategoryKey.Cannon));
            buffer.Acquire(new[] { new RosterAcquireRequest(EntityKey.Unit_Ballista, CharacterOwnershipRole.Sub) });
            long revision = buffer.RosterRevision;
            Assert.Throws<InvalidOperationException>(() => store.InstallMainSpecialization(prepared));
            Assert.That(buffer.RosterRevision, Is.EqualTo(revision));
            Assert.That(buffer.Owned[0], Is.SameAs(before));
        }

        [Test]
        public void ForeignOrInvalidPreparationRejectsWithoutMutation()
        {
            var buffer = CreateBuffer();
            var foreign = CreateBuffer();
            var store = (ICharacterBufferStore)buffer;
            var otherStore = (ICharacterBufferStore)foreign;
            var prepared = store.PrepareMainSpecialization(Change(buffer.Owned[0], CategoryKey.Heavy));
            Assert.Throws<InvalidOperationException>(() => otherStore.InstallMainSpecialization(prepared));
            Assert.Throws<ArgumentNullException>(() => store.PrepareMainSpecialization(null));
            Assert.Throws<ArgumentNullException>(() => store.InstallMainSpecialization(null));
            var sub = buffer.Owned[1];
            Assert.Throws<ArgumentException>(() => new MainSpecializationChange(sub,
                new OwnedCharacterRecord(sub.Id, sub.CharacterKey, CharacterOwnershipRole.Main, specializationCategory: CategoryKey.Heavy)));
            Assert.That((buffer.RosterRevision, foreign.RosterRevision), Is.EqualTo((1L, 1L)));
            Assert.That(foreign.Owned[0].SpecializationCategory, Is.Null);
            store.InstallMainSpecialization(prepared);
            Assert.That(buffer.Owned[0].SpecializationCategory, Is.EqualTo(CategoryKey.Heavy));
        }

        [Test]
        public void RevisionOverflowDoesNotMutate()
        {
            var buffer = CreateBuffer(long.MaxValue - 1);
            var before = buffer.Owned[0];
            Assert.Throws<OverflowException>(() => ((ICharacterBufferStore)buffer).PrepareMainSpecialization(Change(before, CategoryKey.Rider)));
            Assert.That(buffer.RosterRevision, Is.EqualTo(long.MaxValue));
            Assert.That(buffer.Owned[0], Is.SameAs(before));
        }

        [Test]
        public void FreshRunIsUnspecialized()
        {
            var buffer = CreateBuffer();
            var store = (ICharacterBufferStore)buffer;
            var before = buffer.Owned[0];
            store.InstallMainSpecialization(store.PrepareMainSpecialization(Change(before, CategoryKey.Rider)));
            store.ClearRoster(store.PrepareClearRoster());
            var next = store.PrepareInitialRoster(new[] { MainRequest() });
            store.InstallAcquire(next);
            Assert.That(buffer.Owned.Count, Is.EqualTo(1));
            Assert.That(buffer.Owned[0].Id, Is.Not.EqualTo(before.Id));
            Assert.That(buffer.Owned[0].SpecializationCategory, Is.Null);
        }

        private static CharacterBuffer CreateBuffer(long revision = 0)
        {
            var buffer = new CharacterBuffer(1, revision);
            Assert.That(buffer.Acquire(new[] { MainRequest(),
                new RosterAcquireRequest(EntityKey.Unit_Shielder, CharacterOwnershipRole.Sub) }).Succeeded, Is.True);
            return buffer;
        }

        private static RosterAcquireRequest MainRequest() => new(EntityKey.Unit_Cowboy,
            CharacterOwnershipRole.Main, initialMain: new MainCharacterPersistentState(7, 13, 11, 5));

        private static MainSpecializationChange Change(OwnedCharacterRecord before, CategoryKey category) => new(before,
            new OwnedCharacterRecord(before.Id, before.CharacterKey, before.Role, before.Level, before.Experience,
                before.Growth, before.HunterAttackBonus, category));
    }
}
