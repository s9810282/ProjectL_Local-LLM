using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.ArtifactPopup;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Artifact;

namespace ProjectL.Tests.Framework
{
    public sealed class ArtifactInteractionTests
    {
        [Test]
        public void PoolDrag_RotatesAndPlacesDefinition_Once()
        {
            var input = new ArtifactInteraction();
            input.BeginPool(0);
            input.SetCandidate(new CellCoord(2, 1), true);
            input.Rotate(1);
            Assert.That(input.TryTakePreview(out var preview), Is.True);
            Assert.That(preview.Kind, Is.EqualTo(ArtifactPreviewKind.Place));
            Assert.That(preview.Rotation, Is.EqualTo(Rotation4.R90));
            var edit = input.Release(true, false);
            Assert.That(edit.ArtifactId, Is.Zero);
            Assert.That(edit.Anchor, Is.EqualTo(new CellCoord(2, 1)));
            Assert.That(edit.Rotation, Is.EqualTo(Rotation4.R90));
            Assert.That(input.Release(true, false), Is.Null);
        }

        [Test]
        public void PlacedDrag_PreservesGrabOffset_AndMovesInstance()
        {
            var input = new ArtifactInteraction();
            input.BeginPlaced(1001, 0, new CellCoord(1, 1), Rotation4.R90, new CellCoord(2, 1));
            input.SetCandidate(new CellCoord(3, 2), true);
            input.Rotate(-1);
            var edit = input.Release(true, false);
            Assert.That(edit.Kind, Is.EqualTo(ArtifactEditKind.Move));
            Assert.That(edit.InstanceId, Is.Zero);
            Assert.That(edit.Anchor, Is.EqualTo(new CellCoord(2, 2)));
            Assert.That(edit.Rotation, Is.EqualTo(Rotation4.R0));
        }

        [Test]
        public void SameCellAndRotation_DoesNotCreateRepeatedPreview()
        {
            var input = new ArtifactInteraction();
            input.BeginPool(5);
            input.SetCandidate(CellCoord.Zero, true);
            Assert.That(input.TryTakePreview(out _), Is.True);
            for (int i = 0; i < 100; i++)
            {
                input.SetCandidate(CellCoord.Zero, true);
                Assert.That(input.TryTakePreview(out _), Is.False);
            }
            input.Rotate(-1);
            Assert.That(input.TryTakePreview(out var rotated), Is.True);
            Assert.That(rotated.Rotation, Is.EqualTo(Rotation4.R270));
            input.InvalidateCandidate();
            Assert.That(input.TryTakePreview(out _), Is.True, "보드 상태가 바뀌면 같은 후보도 다시 검증합니다.");
        }

        [Test]
        public void SteadyDrag_DoesNotAllocateRequests_ForUnchangedCell()
        {
            var input = new ArtifactInteraction();
            input.BeginPool(1);
            input.SetCandidate(CellCoord.Zero, true);
            input.TryTakePreview(out _);
            for (int i = 0; i < 10; i++) { input.SetCandidate(CellCoord.Zero, true); input.TryTakePreview(out _); }
            long before = System.GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1000; i++) { input.SetCandidate(CellCoord.Zero, true); input.TryTakePreview(out _); }
            Assert.That(System.GC.GetAllocatedBytesForCurrentThread() - before, Is.Zero);
        }

        [Test]
        public void ReturnArea_OnlyReturnsPlacedInstance_AndCancelNeverReturns()
        {
            var input = new ArtifactInteraction();
            input.BeginPool(5);
            Assert.That(input.Release(false, true), Is.Null);
            input.BeginPlaced(5, 8, CellCoord.Zero, Rotation4.R0, CellCoord.Zero);
            Assert.That(input.Release(false, true).Kind, Is.EqualTo(ArtifactEditKind.Return));
            input.BeginPlaced(5, 8, CellCoord.Zero, Rotation4.R0, CellCoord.Zero);
            input.Cancel();
            Assert.That(input.Release(false, true), Is.Null);
            input.BeginPlaced(5, 8, CellCoord.Zero, Rotation4.R0, CellCoord.Zero);
            Assert.That(input.Release(false, false), Is.Null, "버튼·화면 밖에서는 반환하지 않습니다.");
        }

        [Test]
        public void LeavingAndReenteringBoard_InvalidatesAndRequestsFreshPreview()
        {
            var input = new ArtifactInteraction();
            input.BeginPool(1);
            input.SetCandidate(CellCoord.Zero, true);
            input.TryTakePreview(out var first);
            input.SetCandidate(CellCoord.Zero, false);
            Assert.That(input.TryTakePreview(out _), Is.False);
            input.SetCandidate(CellCoord.Zero, true);
            Assert.That(input.TryTakePreview(out var second), Is.True);
            Assert.That(second, Is.Not.SameAs(first));
            input.Cancel();
            Assert.That(input.TryTakePreview(out _), Is.False);
        }
    }
}
