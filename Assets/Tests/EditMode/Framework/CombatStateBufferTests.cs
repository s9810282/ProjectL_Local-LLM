using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Framework.Composition;
using ProjectL.Systems.Factory.Runtime.Buffer;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.ServiceKeys;

namespace ProjectL.Tests.Framework
{
    public sealed class CombatStateBufferTests
    {
        [Test]
        public void BattleAndChapterEntry_PreserveStageAndMapClearRecords()
        {
            IBufferQuery buffers = CreateBuffers();
            OwnedCharacterRecord main = AcquireMain(buffers);
            buffers.MapProgressWriteSink.RegisterMap(7);
            buffers.MapProgressWriteSink.SetClear(7, true);
            buffers.StageProgressWriteSink.RegisterStages(7, new[]
            {
                new StageProgressData(2, 100, 1, 0, Array.Empty<int>(), true)
            });

            buffers.BattleWriteSink.PrepareBattle(new BattlePreparation(
                new[] { CharacterBufferTestData.MainFacts(
                    main.Id, CharacterBufferTestData.MainState()) },
                CharacterBufferTestData.Combat()));

            Assert.That(buffers.CombatStateReadSink.HasBattle, Is.True);
            Assert.That(buffers.CombatStateReadSink.Current.BoardWidth, Is.EqualTo(8));
            Assert.That(buffers.CombatStateReadSink.Phase, Is.EqualTo(CombatFlowPhase.PlayerInput));
            Assert.That(buffers.CombatStateReadSink.LatestResultSequence, Is.Zero);
            Assert.That(buffers.CombatStateReadSink.HandledCommandNumber, Is.Zero);
            Assert.That(buffers.CombatStateReadSink.AnsweredPreviewNumber, Is.Zero);

            buffers.BattleWriteSink.EndBattle();

            Assert.That(buffers.CombatStateReadSink.HasBattle, Is.False);
            Assert.That(buffers.MapProgressReadSink.GetMap(7).IsClear, Is.True);
            Assert.That(buffers.StageProgressReadSink.IsCleared(7, 2), Is.True);
            Assert.That(buffers.CharacterReadSink.Owned.Single().Id, Is.EqualTo(main.Id));
        }

        [Test]
        public void PrepareBattle_InvalidDrawOrder_InstallsNeitherCharacterNorCombatState()
        {
            IBufferQuery buffers = CreateBuffers();
            OwnedCharacterRecord main = AcquireMain(buffers);
            var preparation = new BattlePreparation(
                new[] { CharacterBufferTestData.MainFacts(
                    main.Id, CharacterBufferTestData.MainState()) },
                CharacterBufferTestData.Combat(new CharacterInstanceId(999)));

            Assert.Throws<InvalidOperationException>(() =>
                buffers.BattleWriteSink.PrepareBattle(preparation));
            Assert.That(buffers.CombatStateReadSink.HasBattle, Is.False);
            Assert.That(buffers.CharacterReadSink.Characters, Is.Empty);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void PrepareBattle_DefaultCharacterSnapshot_PreservesStoresAndAllowsRetry(bool invalidHealth)
        {
            IBufferQuery buffers = CreateBuffers();
            OwnedCharacterRecord main = AcquireMain(buffers);
            CharacterCombatFacts mainFacts = CharacterBufferTestData.MainFacts(
                main.Id, CharacterBufferTestData.MainState());
            CharacterCombatFacts enemy = CharacterBufferTestData.EnemyFacts(new EntityId(2));
            CharacterCombatFacts invalidEnemy = invalidHealth
                ? CharacterBufferTestData.WithSnapshot(enemy, health: default(HealthStateSnapshot))
                : CharacterBufferTestData.WithSnapshot(enemy, stats: default(UnitStatsSnapshot));

            Assert.Throws<InvalidOperationException>(() => buffers.BattleWriteSink.PrepareBattle(
                new BattlePreparation(new[] { mainFacts, invalidEnemy }, CharacterBufferTestData.Combat())));

            Assert.That(buffers.CombatStateReadSink.HasBattle, Is.False);
            Assert.That(buffers.CharacterReadSink.Characters, Is.Empty);
            Assert.That(buffers.CharacterReadSink.TryGetOwnedInstance(mainFacts.EntityId, out _), Is.False);
            Assert.That(buffers.CharacterReadSink.Owned.Single().Id, Is.EqualTo(main.Id));
            Assert.That(buffers.CharacterReadSink.TryGetMainState(out var persistent), Is.True);
            Assert.That(persistent.CurrentHealth, Is.EqualTo(10));

            buffers.BattleWriteSink.PrepareBattle(new BattlePreparation(
                new[] { mainFacts, enemy }, CharacterBufferTestData.Combat()));

            Assert.That(buffers.CombatStateReadSink.HasBattle, Is.True);
            Assert.That(buffers.CharacterReadSink.Characters.Count, Is.EqualTo(2));
        }

        private static OwnedCharacterRecord AcquireMain(IBufferQuery buffers)
        {
            return buffers.CharacterWriteSink.Acquire(new[]
            {
                new RosterAcquireRequest(EntityKey.Unit_MainCharacter, CharacterOwnershipRole.Main)
            }, CharacterBufferTestData.MainState()).Acquired.Single();
        }

        private static IBufferQuery CreateBuffers()
        {
            var builder = new SystemBuilderHelper();
            BufferSystemFactory.Create().ExportServices(builder);
            return builder.BuildSystemReader().ServiceStoreReader.GetService(BufferSystemKey.Buffer);
        }
    }
}
