using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NUnit.Framework;
using ProjectL.Framework.Composition;
using ProjectL.Systems.Factory.Runtime.Buffer;
using ProjectL.Systems.ServiceKeys;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Tests.Framework
{
    public sealed class ArtifactBufferTests
    {
        [Test]
        public void Snapshot_CopiesSourceArraysAndCells()
        {
            var cells = new[] { new CellCoord(0, 0) };
            var items = new[] { new ArtifactItemState(1001, 7, true,
                new CellCoord(2, 3), Rotation4.R0, cells) };
            var snapshot = new ArtifactBufferSnapshot(new ArtifactState(6, 6,
                Array.Empty<ArtifactItemState>(), items));
            cells[0] = new CellCoord(9, 9);
            items[0] = default;
            Assert.That(snapshot.PlacedItems[0].InstanceId, Is.EqualTo(7));
            Assert.That(snapshot.PlacedItems[0].Cells[0].X, Is.EqualTo(0));
        }

        [Test]
        public void Snapshot_RejectsInvalidBoardAndNullState()
        {
            Assert.Throws<ArgumentNullException>(() => new ArtifactBufferSnapshot(null));
            Assert.Throws<ArgumentException>(() => new ArtifactBufferSnapshot(
                new ArtifactState(0, 6, null, null)));
        }

        [Test]
        public void TryInitialize_GrantsOnceAndPreservesOutputsOnRepeat()
        {
            var buffer = new ArtifactBuffer();
            ArtifactState source = State();
            Assert.That(buffer.TryInitialize(source), Is.True);
            Assert.That(buffer.LastEditResult, Is.Null);
            Assert.That(buffer.LastPreview, Is.Null);
            ArtifactBufferSnapshot current = buffer.Current;
            Assert.That(current.PoolItems[0].InstanceId, Is.Zero);
            buffer.PublishEdit(Rejected());
            ArtifactPlacementPreview preview = Preview();
            buffer.PublishPreview(preview);

            Assert.That(buffer.TryInitialize(new ArtifactState(9, 9, null, null)), Is.False);
            Assert.That(buffer.Current, Is.SameAs(current));
            Assert.That(buffer.LastEditResult.Value.FailureReason, Is.EqualTo(ArtifactActionFailureReason.CellOccupied));
            Assert.That(buffer.LastPreview, Is.SameAs(preview));
            ArrayOf(source.PoolItems)[0] = default;
            ArrayOf(source.PlacedItems.Span[0].Cells)[0] = new CellCoord(99, 99);
            Assert.That(current.PoolItems[0].ArtifactId, Is.EqualTo(100));
            Assert.That(current.PlacedItems[0].Cells[0], Is.EqualTo(new CellCoord(1, 2)));
        }

        [Test]
        public void ToCalculationState_CopiesEveryFieldAndNestedCell()
        {
            ArtifactState source = State();
            var snapshot = new ArtifactBufferSnapshot(source);
            ArtifactState first = snapshot.ToCalculationState();
            ArtifactState second = snapshot.ToCalculationState();
            AssertState(first, source);
            AssertState(second, source);
            AssertState(new ArtifactBufferSnapshot(first).ToCalculationState(), source);

            ArrayOf(source.PoolItems.Span[0].Cells)[0] = new CellCoord(10, 10);
            ArrayOf(source.PlacedItems)[0] = default;
            ArrayOf(first.PlacedItems.Span[0].Cells)[0] = new CellCoord(20, 20);
            ArrayOf(first.PoolItems)[0] = default;
            Assert.That(snapshot.PoolItems[0].Cells[0], Is.EqualTo(CellCoord.Zero));
            Assert.That(snapshot.PlacedItems[0].Cells[0], Is.EqualTo(new CellCoord(1, 2)));
            Assert.That(second.PoolItems.Span[0].InstanceId, Is.Zero);
            Assert.That(second.PoolItems.Span[0].Cells.Span[0], Is.EqualTo(CellCoord.Zero));
            Assert.That(second.PlacedItems.Span[0].Cells.Span[0], Is.EqualTo(new CellCoord(1, 2)));
            Assert.That(first.PlacedItems.Span[0].ArtifactId, Is.EqualTo(200));
        }

        [Test]
        public void EmptySnapshot_RejectsCalculationConversion()
        {
            Assert.Throws<InvalidOperationException>(() => ArtifactBufferSnapshot.Empty.ToCalculationState());
        }

        [TestCase(false)]
        [TestCase(true)]
        public void PublishAcceptedEdit_StoresMatchingStateResultAndClearsPreview(bool noOp)
        {
            var buffer = InitializedBuffer();
            ArtifactBufferSnapshot previous = buffer.Current;
            buffer.PublishPreview(Preview());
            ArtifactState state = noOp ? previous.ToCalculationState() : new ArtifactState(4, 4, null,
                new[] { Item(100, 0, true, new CellCoord(2, 1), Rotation4.R180, new CellCoord(2, 1)) });
            var proposal = new ArtifactEditProposal(ArtifactActionResult.Success(100, 0), state);
            buffer.PublishEdit(proposal);

            AssertState(buffer.Current.ToCalculationState(), state);
            Assert.That(buffer.LastEditResult.Value.Succeeded, Is.True);
            Assert.That(buffer.LastEditResult.Value.ArtifactId, Is.EqualTo(100));
            Assert.That(buffer.LastEditResult.Value.InstanceId, Is.Zero);
            Assert.That(buffer.LastPreview, Is.Null);
            Assert.That(previous.PoolItems.Count, Is.EqualTo(2));
            ArrayOf(proposal.State.PlacedItems.Span[0].Cells)[0] = new CellCoord(90, 90);
            ArrayOf(proposal.State.PoolItems).AsSpan().Clear();
            AssertState(buffer.Current.ToCalculationState(), state);
        }

        [Test]
        public void PublishRejectedEdit_PreservesCurrentAndClearsPreview()
        {
            var buffer = InitializedBuffer();
            ArtifactBufferSnapshot previous = buffer.Current;
            buffer.PublishPreview(Preview());
            buffer.PublishEdit(Rejected());
            Assert.That(buffer.Current, Is.SameAs(previous));
            Assert.That(buffer.LastEditResult.Value.Succeeded, Is.False);
            Assert.That(buffer.LastEditResult.Value.FailureReason, Is.EqualTo(ArtifactActionFailureReason.CellOccupied));
            Assert.That(buffer.LastPreview, Is.Null);
        }

        [Test]
        public void PublishPreview_ChangesOnlyPreview()
        {
            var buffer = InitializedBuffer();
            buffer.PublishEdit(Rejected());
            ArtifactBufferSnapshot previous = buffer.Current;
            ArtifactActionResult result = buffer.LastEditResult.Value;
            buffer.PublishPreview(Preview());
            var missing = new ArtifactPlacementPreview(null, new CellCoord(3, 2), ArtifactActionFailureReason.InstanceNotFound);
            buffer.PublishPreview(missing);
            Assert.That(buffer.LastPreview, Is.SameAs(missing));
            Assert.That(buffer.Current, Is.SameAs(previous));
            Assert.That(buffer.LastEditResult.Value, Is.EqualTo(result));
        }

        [TestCaseSource(nameof(InvalidStates))]
        public void InvalidInitialization_PreservesCurrentAndOutputs(ArtifactState state)
        {
            var buffer = new ArtifactBuffer();
            Assert.Throws<ArgumentException>(() => buffer.TryInitialize(state));
            Assert.That(buffer.Current, Is.SameAs(ArtifactBufferSnapshot.Empty));
            Assert.That(buffer.LastEditResult, Is.Null);
            Assert.That(buffer.LastPreview, Is.Null);
        }

        [Test]
        public void InvalidAcceptedPublication_PreservesAllFields()
        {
            var buffer = InitializedBuffer();
            buffer.PublishEdit(Rejected());
            ArtifactPlacementPreview preview = Preview();
            buffer.PublishPreview(preview);
            ArtifactBufferSnapshot previous = buffer.Current;
            var proposal = new ArtifactEditProposal(ArtifactActionResult.Success(), State());
            // 전달 상태의 공개 메모리를 통해 손상된 제안도 게시 전에 구조 검증한다.
            CellCoord[] cells = ArrayOf(proposal.State.PlacedItems.Span[0].Cells);
            cells[1] = cells[0];
            Assert.Throws<ArgumentException>(() => buffer.PublishEdit(proposal));
            Assert.That(buffer.Current, Is.SameAs(previous));
            Assert.That(buffer.LastEditResult.Value.FailureReason, Is.EqualTo(ArtifactActionFailureReason.CellOccupied));
            Assert.That(buffer.LastPreview, Is.SameAs(preview));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void NullPublication_PreservesAllFields(bool initialized)
        {
            var buffer = initialized ? InitializedBuffer() : new ArtifactBuffer();
            if (initialized) { buffer.PublishEdit(Rejected()); buffer.PublishPreview(Preview()); }
            ArtifactBufferSnapshot previous = buffer.Current;
            ArtifactActionResult? result = buffer.LastEditResult;
            ArtifactPlacementPreview preview = buffer.LastPreview;
            Assert.Throws<ArgumentNullException>(() => buffer.TryInitialize(null));
            Assert.Throws<ArgumentNullException>(() => buffer.PublishEdit(null));
            Assert.Throws<ArgumentNullException>(() => buffer.PublishPreview(null));
            Assert.That(buffer.Current, Is.SameAs(previous));
            Assert.That(buffer.LastEditResult, Is.EqualTo(result));
            Assert.That(buffer.LastPreview, Is.SameAs(preview));
        }

        [Test]
        public void UninitializedPublication_Throws()
        {
            var buffer = new ArtifactBuffer();
            Assert.Throws<InvalidOperationException>(() => buffer.PublishEdit(Rejected()));
            Assert.Throws<InvalidOperationException>(() => buffer.PublishEdit(new ArtifactEditProposal(ArtifactActionResult.Success(), State())));
            Assert.Throws<InvalidOperationException>(() => buffer.PublishPreview(Preview()));
            Assert.That(buffer.Current.IsInitialized, Is.False);
            Assert.That(buffer.LastEditResult, Is.Null);
            Assert.That(buffer.LastPreview, Is.Null);
        }

        [Test]
        public void FactoryExportsNarrowArtifactPermissionsOnOneBuffer()
        {
            var builder = new SystemBuilderHelper();
            BufferSystemFactory.Create().ExportServices(builder);
            var query = builder.BuildSystemReader().ServiceStoreReader.GetService(BufferSystemKey.Buffer);
            Assert.That(query.ArtifactInitializationWriteSink, Is.SameAs(query.ArtifactReadSink));
            Assert.That(query.ArtifactEditWriteSink, Is.SameAs(query.ArtifactReadSink));
            Assert.That(query.ArtifactPreviewWriteSink, Is.SameAs(query.ArtifactReadSink));
            Assert.That(query.ArtifactEditResultReadSink, Is.SameAs(query.ArtifactReadSink));
            Assert.That(query.ArtifactPreviewReadSink, Is.SameAs(query.ArtifactReadSink));
            Assert.That(query.ArtifactInitializationWriteSink.TryInitialize(State()), Is.True);
            ArtifactPlacementPreview preview = Preview();
            query.ArtifactPreviewWriteSink.PublishPreview(preview);
            Assert.That(query.ArtifactPreviewReadSink.LastPreview, Is.SameAs(preview));
            query.ArtifactEditWriteSink.PublishEdit(Rejected());
            Assert.That(query.ArtifactReadSink.Current.PoolItems[0].InstanceId, Is.Zero);
            Assert.That(query.ArtifactEditResultReadSink.LastEditResult.Value.Succeeded, Is.False);
            Assert.That(query.ArtifactPreviewReadSink.LastPreview, Is.Null);
            query.RunProgressWriteSink.ResetRunProgress();
            Assert.That(query.ArtifactReadSink.Current.IsInitialized, Is.True);
            Assert.That(query.ArtifactEditResultReadSink.LastEditResult.Value.Succeeded, Is.False);
        }

        [Test]
        public void Factory_DoesNotExposeLegacyReplacementOrResetAuthority()
        {
            var builder = new SystemBuilderHelper();
            BufferSystemFactory.Create().ExportServices(builder);
            var query = builder.BuildSystemReader().ServiceStoreReader.GetService(BufferSystemKey.Buffer);
            Type bufferType = query.ArtifactReadSink.GetType();
            // 읽기 권한을 받아도 구형 전체 교체나 초기화 해제 권한으로 우회할 수 없어야 한다.
            Assert.That(bufferType.GetInterface("IArtifactBufferWriteSink"), Is.Null);
            Assert.That(query.GetType().GetProperty("ArtifactWriteSink"), Is.Null);
            Assert.That(typeof(ArtifactBufferKey).GetField("Write"), Is.Null);
            Assert.That(bufferType.GetMethod("SetSnapshot"), Is.Null);
            Assert.That(bufferType.GetMethod("Clear"), Is.Null);
        }

        [Test]
        public void Snapshot_DoesNotRecalculatePlacementRules()
        {
            var outside = new CellCoord(20, -3);
            var state = new ArtifactState(1, 1, null, new[] {
                Item(100, 0, true, outside, Rotation4.R0, outside),
                Item(100, 1, true, outside, Rotation4.R0, outside) });
            var buffer = new ArtifactBuffer();
            Assert.That(buffer.TryInitialize(state), Is.True);
            buffer.PublishEdit(new ArtifactEditProposal(ArtifactActionResult.Success(), state));
            AssertState(buffer.Current.ToCalculationState(), state);
        }

        private static IEnumerable<TestCaseData> InvalidStates()
        {
            yield return Invalid("Width", new ArtifactState(0, 4, null, null));
            yield return Invalid("Height", new ArtifactState(4, -1, null, null));
            yield return Invalid("ArtifactId", Pool(Item(-1, 0, false, CellCoord.Zero, Rotation4.R0, CellCoord.Zero)));
            yield return Invalid("InstanceId", Pool(Item(100, -1, false, CellCoord.Zero, Rotation4.R0, CellCoord.Zero)));
            yield return Invalid("DuplicatePoolId", Pool(Item(100, 0, false, CellCoord.Zero, Rotation4.R0, CellCoord.Zero), Item(200, 0, false, CellCoord.Zero, Rotation4.R0, CellCoord.Zero)));
            yield return Invalid("DuplicatePlacedId", new ArtifactState(4, 4, null, new[] { Item(100, 0, true, CellCoord.Zero, Rotation4.R0, CellCoord.Zero), Item(200, 0, true, CellCoord.Zero, Rotation4.R0, CellCoord.Zero) }));
            yield return Invalid("DuplicateAcrossLists", new ArtifactState(4, 4, Pool(Item(100, 0, false, CellCoord.Zero, Rotation4.R0, CellCoord.Zero)).PoolItems.ToArray(), new[] { Item(200, 0, true, CellCoord.Zero, Rotation4.R0, CellCoord.Zero) }));
            yield return Invalid("PoolMembership", Pool(Item(100, 0, true, CellCoord.Zero, Rotation4.R0, CellCoord.Zero)));
            yield return Invalid("PlacedMembership", new ArtifactState(4, 4, null, new[] { Item(100, 0, false, CellCoord.Zero, Rotation4.R0, CellCoord.Zero) }));
            yield return Invalid("Rotation", new ArtifactState(4, 4, null, new[] { Item(100, 0, true, CellCoord.Zero, (Rotation4)99, CellCoord.Zero) }));
            yield return Invalid("PoolAnchor", Pool(Item(100, 0, false, new CellCoord(1, 0), Rotation4.R0, CellCoord.Zero)));
            yield return Invalid("PoolRotation", Pool(Item(100, 0, false, CellCoord.Zero, Rotation4.R90, CellCoord.Zero)));
            yield return Invalid("EmptyCells", Pool(Item(100, 0, false, CellCoord.Zero, Rotation4.R0)));
            yield return Invalid("DuplicateCells", Pool(Item(100, 0, false, CellCoord.Zero, Rotation4.R0, CellCoord.Zero, CellCoord.Zero)));
        }

        private static TestCaseData Invalid(string name, ArtifactState state) => new TestCaseData(state).SetName("InvalidInitialization_PreservesCurrentAndOutputs_" + name);
        private static ArtifactItemState Item(int artifactId, int instanceId, bool placed, CellCoord anchor, Rotation4 rotation, params CellCoord[] cells) => new(artifactId, instanceId, placed, anchor, rotation, cells);
        private static ArtifactState Pool(params ArtifactItemState[] items) => new(4, 4, items, null);
        private static ArtifactState State() => new(4, 4, new[] {
            Item(100, 0, false, CellCoord.Zero, Rotation4.R0, CellCoord.Zero),
            Item(101, 1, false, CellCoord.Zero, Rotation4.R0, CellCoord.Zero, new CellCoord(1, 0)) }, new[] {
            Item(200, 3, true, new CellCoord(1, 2), Rotation4.R90, new CellCoord(1, 2), new CellCoord(1, 3)),
            Item(201, 4, true, new CellCoord(3, 0), Rotation4.R270, new CellCoord(3, 0)) });
        private static ArtifactBuffer InitializedBuffer()
        {
            var buffer = new ArtifactBuffer();
            buffer.TryInitialize(State());
            return buffer;
        }
        private static ArtifactEditProposal Rejected() => new(ArtifactActionResult.Failure(ArtifactActionFailureReason.CellOccupied), null);
        private static ArtifactPlacementPreview Preview() => new(new ArtifactShapeSnapshot(1, 1, new[] { CellCoord.Zero }), new CellCoord(2, 1), ArtifactActionFailureReason.None);
        private static T[] ArrayOf<T>(ReadOnlyMemory<T> memory)
        {
            Assert.That(MemoryMarshal.TryGetArray(memory, out ArraySegment<T> segment), Is.True);
            return segment.Array;
        }
        private static void AssertState(ArtifactState actual, ArtifactState expected)
        {
            Assert.That(actual.BoardWidth, Is.EqualTo(expected.BoardWidth));
            Assert.That(actual.BoardHeight, Is.EqualTo(expected.BoardHeight));
            AssertItems(actual.PoolItems, expected.PoolItems);
            AssertItems(actual.PlacedItems, expected.PlacedItems);
        }
        private static void AssertItems(ReadOnlyMemory<ArtifactItemState> actual, ReadOnlyMemory<ArtifactItemState> expected)
        {
            Assert.That(actual.Length, Is.EqualTo(expected.Length));
            for (int i = 0; i < actual.Length; i++)
            {
                ArtifactItemState a = actual.Span[i], e = expected.Span[i];
                Assert.That(a.ArtifactId, Is.EqualTo(e.ArtifactId));
                Assert.That(a.InstanceId, Is.EqualTo(e.InstanceId));
                Assert.That(a.IsPlaced, Is.EqualTo(e.IsPlaced));
                Assert.That(a.Anchor, Is.EqualTo(e.Anchor));
                Assert.That(a.Rotation, Is.EqualTo(e.Rotation));
                Assert.That(a.Cells.ToArray(), Is.EqualTo(e.Cells.ToArray()));
            }
        }
    }
}
