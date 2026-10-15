using System;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectL.Framework.Composition;
using ProjectL.Systems.Factory.Runtime.Buffer;
using ProjectL.Systems.Factory.Runtime.InGame;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.ServiceKeys;

namespace ProjectL.Tests.Framework
{
    public sealed partial class FrameworkRuntimeCompositionTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void Factory_ExportsRecoveryAndPropagatesDependencies(bool bindFirst)
        {
            var runtime = CreateRecoveryRuntime();
            try
            {
                var database = new RecoveryStageTestDatabase();
                if (bindFirst) { runtime.PostBindService(); runtime.InitData(database); }
                else { runtime.InitData(database); runtime.PostBindService(); }
                runtime.Initialize();
                var inGame = runtime.GetService(SystemGroupsService.InGame);
                var core = inGame.CoreGroup;
                var buffers = runtime.GetService(BufferSystemKey.Buffer);
                Assert.That(core.Recovery, Is.Not.Null);
                Assert.That(runtime.GetService(SystemGroupsService.InGame).CoreGroup.Recovery,
                    Is.SameAs(core.Recovery));
                Assert.Throws<KeyNotFoundException>(() => runtime.GetService(InGameSystemGroupServiceKey.Core));
                Assert.Throws<KeyNotFoundException>(() => runtime.GetService(InGameSystemServiceKey.Recovery));
                Assert.Throws<KeyNotFoundException>(() => runtime.GetService(InGameSystemServiceKey.Combat));
                Assert.That(core.Characters.InitializeRoster(1).Succeeded, Is.True);
                buffers.CharacterReadSink.TryGetMainState(out var main);
                buffers.CharacterWriteSink.Apply(new CharacterChanges(Array.Empty<CharacterStateChange>(),
                    Array.Empty<SoulCountChange>(), new MainCharacterPersistentChange(main, CharacterBufferTestData.MainState(7))));
                buffers.CurrencyWriteSink.SetAmount(8);
                RegisterRecoveryGraph(runtime);
                Assert.That(core.ChapterProgression.EnterChapter(1).Accepted, Is.True);
                Assert.That(core.Recovery.EnterRecovery(1, new StageNodeId(2)).Accepted, Is.True);
                Assert.That(database.OptionReads, Is.EqualTo(new[] { 2, 1 }));
                Assert.That(core.Recovery.Recover(1, new StageNodeId(2), new RecoveryOptionId(2)), Is.True);
                buffers.CharacterReadSink.TryGetMainState(out var healed);
                Assert.That(healed.CurrentHealth, Is.EqualTo(9));
                Assert.That(buffers.CurrencyReadSink.Amount, Is.EqualTo(5));
                Assert.That(core.Recovery.CompleteRecovery(1, new StageNodeId(2)).Accepted, Is.True);
                Assert.That(buffers.StageProgressReadSink.IsCleared(1, 2), Is.True);
            }
            finally { runtime.Shutdown(); }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Factory_RecoveryMissingLifecycleDependencyFailsWithoutPublication(bool missingData)
        {
            var runtime = CreateRecoveryRuntime();
            try
            {
                var database = new RecoveryStageTestDatabase();
                if (missingData) runtime.PostBindService(); else runtime.InitData(database);
                var buffers = runtime.GetService(BufferSystemKey.Buffer);
                if (missingData) buffers.MapProgressWriteSink.RegisterMap(1);
                RegisterRecoveryGraph(runtime);
                buffers.CurrentProgressWriteSink.SelectMap(1);
                var before = new RecoveryBufferSnapshot(buffers);
                var recovery = runtime.GetService(SystemGroupsService.InGame).CoreGroup.Recovery;
                var exception = Assert.Throws<InvalidOperationException>(
                    () => recovery.EnterRecovery(1, new StageNodeId(2)));
                Assert.That(exception.Message, Does.Contain("DB 초기화와 서비스 바인딩"));
                Assert.That(database.OptionReads, Is.Empty);
                before.AssertUnchanged(buffers);
            }
            finally { runtime.Shutdown(); }
        }

        private static FrameworkRuntime CreateRecoveryRuntime()
        {
            // 실제 InGame 팩토리의 Core/View 계층을 유지하고 무관한 Global 렌더 조회는 제외한다.
            var builder = new SystemBuilderHelper();
            BufferSystemFactory.Create().ExportServices(builder);
            var inGameBuilder = new SystemBuilderHelper();
            ProjectL.Systems.Factory.Runtime.InGame.Core.InGameCoreSystemFactory.Create().ExportServices(inGameBuilder);
            new ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.InGameViewSystemGroup(
                new SystemBuilderHelper().BuildSystemReader(), null).ExportServices(inGameBuilder);
            new ProjectL.Systems.RuntimeSystem.InGameSystem.InGameSystemGroup(inGameBuilder.BuildSystemReader()).ExportServices(builder);
            return new FrameworkRuntime(builder.BuildSystemReader());
        }

        private static void RegisterRecoveryGraph(FrameworkRuntime runtime)
            => runtime.GetService(BufferSystemKey.Buffer).StageProgressWriteSink.RegisterStages(1, new[] {
                new StageProgressData(1, 0, 0, 0, new[] { 2 }),
                new StageProgressData(2, 20, 1, 0, Array.Empty<int>(), false, StageType.Event, StageMapRole.Regular) });
    }
}
