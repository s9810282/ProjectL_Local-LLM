using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Framework.Composition;
using ProjectL.Systems.Factory.Runtime.Buffer;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.ServiceKeys;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;

namespace ProjectL.Tests.Framework
{
    public sealed class CharacterBufferStateTests
    {
        [Test]
        public void HeldState_PersistentMainRebuildPreservesSeparateContributionAndSpentCharge()
        {
            var buffers = CreateBuffers();
            var persistent = CharacterBufferTestData.MainState();
            var main = AcquireMain(buffers, persistent);
            var facts = HeldEnvironmentEffects.WithHeldState(CharacterBufferTestData.MainFacts(main.Id, persistent),
                new CharacterHeldEnvironmentState(41, false, 7));
            buffers.BattleWriteSink.PrepareBattle(new BattlePreparation(new[] { facts }, CharacterBufferTestData.Combat()));
            buffers.BattleWriteSink.Apply(new CharacterChanges(Array.Empty<CharacterStateChange>(), Array.Empty<SoulCountChange>(),
                new MainCharacterPersistentChange(persistent, CharacterBufferTestData.MainState(7))));
            var read = buffers.CharacterReadSink.Characters.Single();
            Assert.That(read.Health.CurrentHealth, Is.EqualTo(7));
            Assert.That(read.HeldEnvironment.SameValues(facts.HeldEnvironment), Is.True);
            Assert.That(read.ToFacts().HeldEnvironment.SuppliesAttackBonus, Is.EqualTo(7));
        }

        [Test]
        public void Acquire_IssuesDistinctIds_RejectsSecondMainAndOverflowWithoutWrites()
        {
            var buffer = new CharacterBuffer();
            RosterAcquireResult acquired = buffer.Acquire(new[]
            {
                new RosterAcquireRequest(EntityKey.Unit_MainCharacter, CharacterOwnershipRole.Main),
                new RosterAcquireRequest(EntityKey.Unit_Akimbo, CharacterOwnershipRole.Sub)
            }, CharacterBufferTestData.MainState());

            Assert.That(acquired.Succeeded, Is.True);
            Assert.That(acquired.Acquired.Select(x => x.Id.Value), Is.EqualTo(new[] { 1, 2 }));

            RosterAcquireResult secondMain = buffer.Acquire(new[]
            {
                new RosterAcquireRequest(EntityKey.Unit_MainCharacter, CharacterOwnershipRole.Main)
            }, CharacterBufferTestData.MainState());

            Assert.That(secondMain.FailureReason, Is.EqualTo(RosterAcquireFailureReason.MainAlreadyOwned));
            Assert.That(buffer.Owned.Count, Is.EqualTo(2));

            var overflow = new CharacterBuffer(int.MaxValue);
            RosterAcquireResult rejected = overflow.Acquire(new[]
            {
                new RosterAcquireRequest(EntityKey.Unit_Akimbo, CharacterOwnershipRole.Sub),
                new RosterAcquireRequest(EntityKey.Unit_Sniper, CharacterOwnershipRole.Sub)
            });

            Assert.That(rejected.FailureReason, Is.EqualTo(RosterAcquireFailureReason.IdentityOverflow));
            Assert.That(overflow.Owned, Is.Empty);
            Assert.That(overflow.Acquire(new[]
            {
                new RosterAcquireRequest(EntityKey.Unit_Akimbo, CharacterOwnershipRole.Sub)
            }).Acquired.Single().Id.Value, Is.EqualTo(int.MaxValue));
        }

        [Test]
        public void CharacterData_ReadCannotMutateStoredState_AndDuplicateApplyChangesNothing()
        {
            IBufferQuery buffers = CreateBuffers();
            OwnedCharacterRecord main = AcquireMain(buffers);
            CharacterCombatFacts mainFacts = CharacterBufferTestData.MainFacts(
                main.Id, CharacterBufferTestData.MainState());
            CharacterCombatFacts enemy = CharacterBufferTestData.EnemyFacts(new EntityId(2));
            buffers.BattleWriteSink.PrepareBattle(new BattlePreparation(
                new[] { mainFacts, enemy }, CharacterBufferTestData.Combat()));

            CharacterData enemyRead = buffers.CharacterReadSink.Characters.Single(
                x => x.Owner == Owner.Enemy);
            Assert.Throws<NotSupportedException>(() =>
                ((IList<CellCoord>)enemyRead.OccupiedCells).Clear());
            Assert.Throws<NotSupportedException>(() =>
                ((IList<CharacterData>)buffers.CharacterReadSink.Characters).Clear());

            CharacterCombatFacts expected = enemyRead.ToFacts();
            CharacterCombatFacts damaged = CharacterBufferTestData.WithHealth(expected, 3);
            var duplicate = new CharacterChanges(new[]
            {
                new CharacterStateChange(expected, damaged),
                new CharacterStateChange(expected, CharacterBufferTestData.WithHealth(expected, 1))
            }, Array.Empty<SoulCountChange>());

            Assert.Throws<InvalidOperationException>(() => buffers.BattleWriteSink.Apply(duplicate));
            Assert.That(buffers.CharacterReadSink.Characters.Single(
                x => x.Owner == Owner.Enemy).Health.CurrentHealth, Is.EqualTo(5));
        }

        [Test]
        public void MainRead_UsesOnePersistentHealthAuthority()
        {
            IBufferQuery buffers = CreateBuffers();
            MainCharacterPersistentState initial = CharacterBufferTestData.MainState();
            OwnedCharacterRecord main = AcquireMain(buffers, initial);
            buffers.BattleWriteSink.PrepareBattle(new BattlePreparation(
                new[] { CharacterBufferTestData.MainFacts(main.Id, initial) },
                CharacterBufferTestData.Combat()));

            var damaged = new MainCharacterPersistentState(7, 10, 5, 4, 3, 1);
            buffers.BattleWriteSink.Apply(new CharacterChanges(
                Array.Empty<CharacterStateChange>(),
                Array.Empty<SoulCountChange>(),
                new MainCharacterPersistentChange(initial, damaged)));

            Assert.That(buffers.CharacterReadSink.TryGetMainState(out var persistent), Is.True);
            Assert.That(persistent.CurrentHealth, Is.EqualTo(7));
            Assert.That(buffers.CharacterReadSink.Characters.Single().Health.CurrentHealth,
                Is.EqualTo(7));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Apply_InconsistentMainHealth_RejectsWholeBatch(bool changesPersistentHealth)
        {
            IBufferQuery buffers = CreateBuffers();
            MainCharacterPersistentState initial = CharacterBufferTestData.MainState();
            OwnedCharacterRecord main = AcquireMain(buffers, initial);
            CharacterCombatFacts mainFacts = CharacterBufferTestData.MainFacts(main.Id, initial);
            CharacterCombatFacts enemy = CharacterBufferTestData.EnemyFacts(new EntityId(2));
            buffers.BattleWriteSink.PrepareBattle(new BattlePreparation(
                new[] { mainFacts, enemy }, CharacterBufferTestData.Combat()));

            var changes = new CharacterChanges(new[]
            {
                new CharacterStateChange(enemy, CharacterBufferTestData.WithHealth(enemy, 3)),
                new CharacterStateChange(mainFacts, CharacterBufferTestData.WithHealth(mainFacts, 7))
            }, Array.Empty<SoulCountChange>(), changesPersistentHealth
                ? new MainCharacterPersistentChange(initial, CharacterBufferTestData.MainState(8))
                : null);

            Assert.Throws<InvalidOperationException>(() => buffers.BattleWriteSink.Apply(changes));
            Assert.That(buffers.CharacterReadSink.TryGetMainState(out var persistent), Is.True);
            Assert.That(persistent.CurrentHealth, Is.EqualTo(10));
            Assert.That(buffers.CharacterReadSink.Characters.Select(x => x.Health.CurrentHealth),
                Is.EqualTo(new[] { 10, 5 }));
        }

        [Test]
        public void Apply_ConsistentMainHealth_UpdatesPersistentAndActiveReads()
        {
            IBufferQuery buffers = CreateBuffers();
            MainCharacterPersistentState initial = CharacterBufferTestData.MainState();
            OwnedCharacterRecord main = AcquireMain(buffers, initial);
            CharacterCombatFacts facts = CharacterBufferTestData.MainFacts(main.Id, initial);
            buffers.BattleWriteSink.PrepareBattle(new BattlePreparation(
                new[] { facts }, CharacterBufferTestData.Combat()));

            buffers.BattleWriteSink.Apply(new CharacterChanges(new[]
            {
                new CharacterStateChange(facts, CharacterBufferTestData.WithHealth(facts, 7))
            }, Array.Empty<SoulCountChange>(),
                new MainCharacterPersistentChange(initial, CharacterBufferTestData.MainState(7))));

            Assert.That(buffers.CharacterReadSink.TryGetMainState(out var persistent), Is.True);
            Assert.That(persistent.CurrentHealth, Is.EqualTo(7));
            Assert.That(buffers.CharacterReadSink.Characters.Single().Health.CurrentHealth, Is.EqualTo(7));
        }

        [Test]
        public void Apply_EquivalentReconstructedStatuses_AcceptsExpectedValues()
        {
            IBufferQuery buffers = CreateBuffers();
            CharacterCombatFacts enemy = EnemyWithStatuses();
            buffers.BattleWriteSink.PrepareBattle(new BattlePreparation(
                new[] { enemy }, CharacterBufferTestData.Combat()));

            CharacterCombatFacts expected = EnemyWithStatuses();
            Assert.That(expected.Stats.Category, Is.Not.SameAs(enemy.Stats.Category));
            Assert.That(expected.Stats.TraitStatuses.Span[0],
                Is.Not.SameAs(enemy.Stats.TraitStatuses.Span[0]));

            buffers.BattleWriteSink.Apply(new CharacterChanges(new[]
            {
                new CharacterStateChange(expected, CharacterBufferTestData.WithHealth(expected, 3))
            }, Array.Empty<SoulCountChange>()));

            Assert.That(buffers.CharacterReadSink.Characters.Single().Health.CurrentHealth, Is.EqualTo(3));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Apply_ChangedStatusInExpected_RejectsWithoutWrites(bool changesCategory)
        {
            IBufferQuery buffers = CreateBuffers();
            CharacterCombatFacts enemy = EnemyWithStatuses();
            buffers.BattleWriteSink.PrepareBattle(new BattlePreparation(
                new[] { enemy }, CharacterBufferTestData.Combat()));

            TraitStatusSnapshot[] traits = enemy.Stats.TraitStatuses.ToArray();
            if (changesCategory == false)
                traits[0] = new FocusTraitStatusSnapshot(2, true, false);

            CharacterCombatFacts expected = CharacterBufferTestData.WithStatuses(enemy,
                changesCategory ? CategorySnapshot.CreateHeavy(Rotation4.R0, true, 30)
                    : CategorySnapshot.CreateHeavy(Rotation4.R0, false, 30), traits);

            Assert.Throws<InvalidOperationException>(() => buffers.BattleWriteSink.Apply(
                new CharacterChanges(new[]
                {
                    new CharacterStateChange(expected, CharacterBufferTestData.WithHealth(expected, 3))
                }, Array.Empty<SoulCountChange>())));

            Assert.That(buffers.CharacterReadSink.Characters.Single().Health.CurrentHealth, Is.EqualTo(5));
        }

        private static CharacterCombatFacts EnemyWithStatuses()
        {
            return CharacterBufferTestData.WithStatuses(
                CharacterBufferTestData.EnemyFacts(new EntityId(2)),
                CategorySnapshot.CreateHeavy(Rotation4.R0, false, 30),
                new FocusTraitStatusSnapshot(1, true, false),
                new MoveAttackDiscountTraitStatusSnapshot(false, true),
                new KnockbackTraitStatusSnapshot(2),
                new PiercingFalloffTraitStatusSnapshot(20),
                new ThornFieldTraitStatusSnapshot(2, 3));
        }

        [Test]
        public void MainPersistentValues_SurviveBattlePreparationAndCleanup()
        {
            IBufferQuery buffers = CreateBuffers();
            MainCharacterPersistentState initial = CharacterBufferTestData.MainState();
            OwnedCharacterRecord main = AcquireMain(buffers, initial);
            var changed = new MainCharacterPersistentState(13, 12, 8, 6, 4, 1);

            buffers.CharacterWriteSink.Apply(new CharacterChanges(
                Array.Empty<CharacterStateChange>(),
                new[] { new SoulCountChange(main.Id, 0, 2) },
                new MainCharacterPersistentChange(initial, changed)));
            buffers.BattleWriteSink.PrepareBattle(new BattlePreparation(
                new[] { CharacterBufferTestData.MainFacts(main.Id, changed, 2, 3, 4) },
                CharacterBufferTestData.Combat()));
            buffers.BattleWriteSink.EndBattle();

            Assert.That(buffers.CharacterReadSink.TryGetMainState(out var persistent), Is.True);
            Assert.That(persistent.CurrentHealth, Is.EqualTo(13));
            Assert.That(persistent.PersistentMaxHealth, Is.EqualTo(12));
            Assert.That(persistent.PersistentAttack, Is.EqualTo(8));
            Assert.That(persistent.MaxActionCost, Is.EqualTo(6));
            Assert.That(persistent.MoveRange, Is.EqualTo(4));
            Assert.That(buffers.CharacterReadSink.GetSoulCount(main.Id), Is.EqualTo(2));
        }

        [Test]
        public void BattleCleanup_KeepsOwnedMainAndSouls_RemovesTransientCharactersAndLinks()
        {
            IBufferQuery buffers = CreateBuffers();
            MainCharacterPersistentState mainState = CharacterBufferTestData.MainState();
            RosterAcquireResult roster = buffers.CharacterWriteSink.Acquire(new[]
            {
                new RosterAcquireRequest(EntityKey.Unit_MainCharacter, CharacterOwnershipRole.Main),
                new RosterAcquireRequest(EntityKey.Unit_Akimbo, CharacterOwnershipRole.Sub)
            }, mainState);
            CharacterInstanceId mainId = roster.Acquired[0].Id;
            CharacterInstanceId subId = roster.Acquired[1].Id;
            buffers.CharacterWriteSink.Apply(new CharacterChanges(
                Array.Empty<CharacterStateChange>(),
                new[] { new SoulCountChange(subId, 0, 3) }));

            buffers.BattleWriteSink.PrepareBattle(new BattlePreparation(new[]
            {
                CharacterBufferTestData.MainFacts(mainId, mainState, temporaryAttack: 2),
                CharacterBufferTestData.SubFacts(subId, new EntityId(2), 3),
                CharacterBufferTestData.EnemyFacts(new EntityId(3))
            }, CharacterBufferTestData.Combat(subId)));
            buffers.BattleWriteSink.EndBattle();

            Assert.That(buffers.CharacterReadSink.Characters, Is.Empty);
            Assert.That(buffers.CharacterReadSink.TryGetOwnedInstance(
                new EntityId(2), out _), Is.False);
            Assert.That(buffers.CharacterReadSink.Owned.Count, Is.EqualTo(2));
            Assert.That(buffers.CharacterReadSink.GetSoulCount(subId), Is.EqualTo(3));
            Assert.That(buffers.CharacterReadSink.TryGetMainState(out var persistent), Is.True);
            Assert.That(persistent.CurrentHealth, Is.EqualTo(10));
        }

        [Test]
        public void ClearThenAcquire_NeverReusesIds()
        {
            var buffer = new CharacterBuffer();
            CharacterInstanceId first = buffer.Acquire(new[]
            {
                new RosterAcquireRequest(EntityKey.Unit_Akimbo, CharacterOwnershipRole.Sub)
            }).Acquired.Single().Id;

            ((ICharacterBufferStore)buffer).ClearRoster();

            CharacterInstanceId second = buffer.Acquire(new[]
            {
                new RosterAcquireRequest(EntityKey.Unit_Sniper, CharacterOwnershipRole.Sub)
            }).Acquired.Single().Id;

            Assert.That(second.Value, Is.GreaterThan(first.Value));
        }

        private static OwnedCharacterRecord AcquireMain(IBufferQuery buffers,
            MainCharacterPersistentState state = null)
        {
            return buffers.CharacterWriteSink.Acquire(new[]
            {
                new RosterAcquireRequest(EntityKey.Unit_MainCharacter, CharacterOwnershipRole.Main)
            }, state ?? CharacterBufferTestData.MainState()).Acquired.Single();
        }

        private static IBufferQuery CreateBuffers()
        {
            var builder = new SystemBuilderHelper();
            BufferSystemFactory.Create().ExportServices(builder);
            return builder.BuildSystemReader().ServiceStoreReader.GetService(BufferSystemKey.Buffer);
        }
    }
}
