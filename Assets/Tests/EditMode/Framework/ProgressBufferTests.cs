using System;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.ServiceKeys;
using ProjectL.Framework.Composition;
using ProjectL.Systems.Factory.Runtime.Buffer;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Current;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Artifact;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Tests.Framework
{
    public sealed class ProgressBufferTests
    {
        [Test]
        public void ResetRunProgress_ClearsTemporaryDataAndPreservesArtifactsAndMapRegistrations()
        {
            var builder = new SystemBuilderHelper();
            BufferSystemFactory.Create().ExportServices(builder);
            var buffer = builder.BuildSystemReader().ServiceStoreReader.GetService(BufferSystemKey.Buffer);
            var artifact = new ArtifactBufferSnapshot(new ArtifactState(6, 6,
                new[] { new ArtifactItemState(1001, 7, false, CellCoord.Zero,
                    Rotation4.R0, new[] { CellCoord.Zero }) }, Array.Empty<ArtifactItemState>()));
            Assert.That(buffer.ArtifactInitializationWriteSink.TryInitialize(artifact.ToCalculationState()), Is.True);
            artifact = buffer.ArtifactReadSink.Current;
            buffer.CharacterWriteSink.Acquire(new[]
            {
                new RosterAcquireRequest(EntityKey.Unit_Akimbo, CharacterOwnershipRole.Sub)
            });
            buffer.CurrencyWriteSink.SetAmount(75);
            buffer.MapProgressWriteSink.RegisterMap(1);
            buffer.MapProgressWriteSink.RegisterMap(2);
            buffer.MapProgressWriteSink.SetClear(1, true);
            buffer.MapProgressWriteSink.SetClear(2, true);
            buffer.StageProgressWriteSink.RegisterStages(1, new[] {
                new StageProgressData(2, 101, 1, 0, Array.Empty<int>(), true) });
            buffer.StageProgressWriteSink.RegisterStages(2, new[] {
                new StageProgressData(3, 201, 1, 0, Array.Empty<int>(), true) });
            buffer.CurrentProgressWriteSink.SelectMap(2);
            buffer.CurrentProgressWriteSink.SelectStage(new StageNodeId(3), new StageKey(201), "Old stage");
            var currentReader = buffer.CurrentProgressReadSink;
            var artifactReader = buffer.ArtifactReadSink;

            var runStore = (ProjectL.Systems.RuntimeSystem.BufferSystems.Run.IRunProgressBufferStore)buffer.RunProgressReadSink;
            runStore.InstallCompletion(runStore.PrepareCompletion(new
                ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results.RunCompletionReceipt(
                RunOutcome.Victory, 2, new StageNodeId(3), new StageKey(201))));

            buffer.RunProgressWriteSink.ResetRunProgress();

            Assert.That(buffer.RunProgressReadSink.Outcome, Is.EqualTo(RunOutcome.InProgress));
            Assert.That(buffer.RunProgressReadSink.Completion, Is.Null);
            Assert.That(currentReader.HasMap, Is.False);
            Assert.That(currentReader.CurrentMapKey, Is.Zero);
            Assert.That(currentReader.HasSelection, Is.False);
            Assert.That(currentReader.CurrentStage.NodeId, Is.Zero);
            Assert.That(currentReader.CurrentStage.StageKey, Is.Zero);
            Assert.That(currentReader.CurrentStage.StageName, Is.Null);
            Assert.That(buffer.StageProgressReadSink.GetStages(1), Is.Empty);
            Assert.That(buffer.StageProgressReadSink.GetStages(2), Is.Empty);
            Assert.That(buffer.MapProgressReadSink.Maps.Count, Is.EqualTo(2));
            Assert.That(buffer.MapProgressReadSink.GetMap(1).IsClear, Is.False);
            Assert.That(buffer.MapProgressReadSink.GetMap(2).IsClear, Is.False);
            Assert.That(buffer.CharacterReadSink.Characters, Is.Empty);
            Assert.That(buffer.CurrencyReadSink.Amount, Is.Zero);
            Assert.That(artifactReader.Current, Is.SameAs(artifact));
            Assert.That(artifactReader.Current.PoolItems[0].InstanceId, Is.EqualTo(7));

            buffer.RunProgressWriteSink.ResetRunProgress();
            Assert.That(buffer.MapProgressReadSink.Maps.Count, Is.EqualTo(2));
            Assert.That(artifactReader.Current, Is.SameAs(artifact));
            Assert.That(artifactReader.Current.PoolItems.Count, Is.EqualTo(1));
        }

        [Test]
        public void ResetRunProgress_EmptyBufferIsSafeAndDoesNotAffectAnotherGroup()
        {
            var firstBuilder = new SystemBuilderHelper();
            BufferSystemFactory.Create().ExportServices(firstBuilder);
            var first = firstBuilder.BuildSystemReader().ServiceStoreReader.GetService(BufferSystemKey.Buffer);
            var otherBuilder = new SystemBuilderHelper();
            BufferSystemFactory.Create().ExportServices(otherBuilder);
            var other = otherBuilder.BuildSystemReader().ServiceStoreReader.GetService(BufferSystemKey.Buffer);
            other.CurrentProgressWriteSink.SelectMap(2);
            other.CurrencyWriteSink.SetAmount(33);

            Assert.DoesNotThrow(() => first.RunProgressWriteSink.ResetRunProgress());
            Assert.That(first.MapProgressReadSink.Maps, Is.Empty);
            Assert.That(first.CurrentProgressReadSink.HasMap, Is.False);
            Assert.That(other.CurrentProgressReadSink.CurrentMapKey, Is.EqualTo(2));
            Assert.That(other.CurrencyReadSink.Amount, Is.EqualTo(33));
        }

        [Test]
        public void CurrentMapChange_ClearsOnlyCurrentSelection()
        {
            var current = new CurrentProgressBuffer();
            Assert.Throws<InvalidOperationException>(() =>
                current.SelectStage(new StageNodeId(2), new StageKey(101), "Stage"));
            current.SelectMap(1);
            current.SelectStage(new StageNodeId(2), new StageKey(101), "Stage");
            current.SelectMap(1);
            Assert.That(current.HasSelection, Is.True);
            current.SelectMap(2);
            Assert.That(current.CurrentMapKey, Is.EqualTo(2));
            Assert.That(current.HasSelection, Is.False);
        }

        [Test]
        public void Progress_IsolatedByMapAndNode_AllowsRepeatedStageKeys()
        {
            var progress = new StageProgressBuffer();
            progress.RegisterStages(1, new[] {
                new StageProgressData(2, 101, 1, 0, new[] { 3 }),
                new StageProgressData(3, 101, 2, 0, Array.Empty<int>()) });
            progress.RegisterStages(2, new[] {
                new StageProgressData(2, 101, 1, 0, Array.Empty<int>()) });
            progress.SetClear(1, 2, true);
            Assert.That(progress.IsCleared(1, 2), Is.True);
            Assert.That(progress.IsCleared(1, 3), Is.False);
            Assert.That(progress.IsCleared(2, 2), Is.False);
            var cleared = progress.GetStages(1)[0];
            Assert.That(cleared.StageKey, Is.EqualTo(101));
            Assert.That(cleared.Depth, Is.EqualTo(1));
            Assert.That(cleared.WidthIndex, Is.Zero);
            Assert.That(cleared.NextNodeIds, Is.EqualTo(new[] { 3 }));
            progress.SetClear(1, 2, false);
            Assert.That(progress.GetStages(1).Count, Is.EqualTo(2));
            Assert.That(progress.IsCleared(1, 2), Is.False);
            Assert.Throws<NotSupportedException>(() =>
                ((IList<StageProgressData>)progress.GetStages(1)).Clear());
        }

        [Test]
        public void RegisterStages_ValidatesBeforeStoringAndRejectsDuplicateRegistration()
        {
            var progress = new StageProgressBuffer();
            var duplicate = new StageProgressData(3, 102, 1, 0, Array.Empty<int>());
            Assert.Throws<ArgumentException>(() => progress.RegisterStages(1, new[] { duplicate, duplicate }));
            Assert.That(progress.GetStages(1), Is.Empty);
            Assert.Throws<ArgumentException>(() => progress.RegisterStages(1, new[] { default(StageProgressData) }));
            Assert.Throws<ArgumentException>(() => progress.RegisterStages(1, new[] {
                new StageProgressData(1, 0, 0, 0, new[] { 99 }) }));
            progress.RegisterStages(1, new[] { duplicate });
            progress.SetClear(1, 3, true);
            Assert.Throws<InvalidOperationException>(() => progress.RegisterStages(1, new[] { duplicate }));
            Assert.That(progress.IsCleared(1, 3), Is.True);
            Assert.That(progress.GetStages(1).Count, Is.EqualTo(1));
            Assert.Throws<KeyNotFoundException>(() => progress.SetClear(1, 99, true));
            Assert.Throws<KeyNotFoundException>(() => progress.SetClear(2, 3, true));
            progress.Clear();
            Assert.That(progress.GetStages(1), Is.Empty);
        }

        [Test]
        public void RegisteredNodes_CopyInputsAndExposeReadOnlyConnections()
        {
            var connections = new List<int> { 2 };
            var records = new List<StageProgressData> {
                new StageProgressData(1, 0, 0, 0, connections),
                new StageProgressData(2, 101, 1, 0, Array.Empty<int>()) };
            var progress = new StageProgressBuffer();
            progress.RegisterStages(1, records);
            connections.Clear();
            records.Clear();
            Assert.That(progress.GetStages(1).Count, Is.EqualTo(2));
            Assert.That(progress.GetStages(1)[0].NextNodeIds, Is.EqualTo(new[] { 2 }));
            Assert.Throws<NotSupportedException>(() =>
                ((IList<int>)progress.GetStages(1)[0].NextNodeIds).Clear());
        }

        [Test]
        public void Factory_ExportsSeparateCurrentAndStageBuffers()
        {
            var builder = new SystemBuilderHelper();
            BufferSystemFactory.Create().ExportServices(builder);
            var query = builder.BuildSystemReader().ServiceStoreReader.GetService(BufferSystemKey.Buffer);
            query.StageProgressWriteSink.RegisterStages(1, new[] {
                new StageProgressData(2, 101, 1, 0, Array.Empty<int>()) });
            query.StageProgressWriteSink.SetClear(1, 2, true);
            query.CurrentProgressWriteSink.SelectMap(1);
            query.CurrentProgressWriteSink.SelectStage(new StageNodeId(2), new StageKey(101), "Stage");
            query.CurrentProgressWriteSink.Clear();
            Assert.That(query.StageProgressReadSink.IsCleared(1, 2), Is.True);
            Assert.That(query.CurrentProgressReadSink.HasMap, Is.False);
        }

    }
}
