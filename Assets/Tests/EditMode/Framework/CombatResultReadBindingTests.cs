using System.Linq;
using NUnit.Framework;
using ProjectL.Framework.Composition;
using ProjectL.Systems.Factory.Runtime.Buffer;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.ServiceKeys;

namespace ProjectL.Tests.Framework
{
    public sealed class CombatResultReadBindingTests
    {
        [Test]
        public void LeafExport_ResultReadKeyResolvesTheActualCombatStoreBeforeBattle()
        {
            var store = new CombatStateBuffer();
            var builder = new SystemBuilderHelper();
            store.ExportServices(builder);
            var services = builder.BuildSystemReader().ServiceStoreReader;

            ICombatResultReadSink results = services.GetService(CombatStateBufferKey.Results);

            Assert.That(results, Is.SameAs(services.GetService(CombatStateBufferKey.Read)));
            Assert.That(results, Is.SameAs(services.GetService(CombatStateBufferKey.Store)));
            Assert.That(results.TryGetLatest(out var result), Is.False);
            Assert.That(result, Is.Null);
        }

        [Test]
        public void FactoryGroup_PreparationAndViewAcknowledgementDoNotFabricateProcessingResults()
        {
            var builder = new SystemBuilderHelper();
            BufferSystemFactory.Create().ExportServices(builder);
            IBufferQuery buffers = builder.BuildSystemReader().ServiceStoreReader
                .GetService(BufferSystemKey.Buffer);
            ICombatResultReadSink results = buffers.CombatResultReadSink;

            Assert.That(results, Is.SameAs(buffers.CombatStateReadSink));
            Assert.That(results.TryGetLatest(out var beforeBattle), Is.False);
            Assert.That(beforeBattle, Is.Null);

            var main = buffers.CharacterWriteSink.Acquire(new[]
            {
                new RosterAcquireRequest(EntityKey.Unit_MainCharacter, CharacterOwnershipRole.Main)
            }, CharacterBufferTestData.MainState()).Acquired.Single();
            buffers.BattleWriteSink.PrepareBattle(new BattlePreparation(
                new[] { CharacterBufferTestData.MainFacts(main.Id, CharacterBufferTestData.MainState()) },
                CharacterBufferTestData.Combat()));
            Assert.That(buffers.CombatStateReadSink.HasBattle, Is.True);
            Assert.That(results.TryGetLatest(out var afterPreparation), Is.False);
            Assert.That(afterPreparation, Is.Null);

            buffers.CombatViewWriteSink.AcknowledgePresented(0);
            buffers.CombatViewWriteSink.Clear();
            buffers.BattleWriteSink.EndBattle();

            Assert.That(results.TryGetLatest(out var afterEnd), Is.False);
            Assert.That(afterEnd, Is.Null);
            Assert.That(buffers.CombatStateReadSink.LatestResultSequence, Is.Zero);
            Assert.That(buffers.CombatStateReadSink.HandledCommandNumber, Is.Zero);
            Assert.That(buffers.CombatStateReadSink.AnsweredPreviewNumber, Is.Zero);
        }
    }
}
