using System;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat;
using UnityEngine;

namespace ProjectL.Tests.Framework
{
    /// <summary>경로 입력 정리 중 Return 표시 실패가 Draft와 입력 수명 정리를 막지 않는지 검증합니다.</summary>
    public sealed class CombatPathInputCleanupTests
    {
        [Test]
        public void ReturnCleanupFailure_StillClearsDraft_AndIgnoresLateRelease()
        {
            var buffers = CombatPreviewPublicationTests.Create(out _);
            var display = new CleanupDisplay();
            var pathInput = new CombatPathInput(buffers.CombatStateReadSink, buffers.CharacterReadSink,
                buffers.CombatViewReadSink, buffers.CombatViewWriteSink, display, display, () => PathInputTestFacts.Read(buffers, true));
            var anchor = buffers.CharacterReadSink.Characters[0].Anchor.Value;
            pathInput.BeginPlanning();
            pathInput.PointerMoved(anchor.X + .5f, anchor.Y + .5f);
            pathInput.PointerMoved(anchor.X + .5f, anchor.Y + 1.5f);
            Assert.That(display.HasDraft, Is.True);
            display.FailReturn = true;

            Assert.Throws<AggregateException>(pathInput.Detach);

            Assert.That(display.HasDraft, Is.False);
            Assert.That(display.DraftClears, Is.EqualTo(1));
            Assert.That(buffers.CombatViewReadSink.HasPreview, Is.False);
            Assert.DoesNotThrow(() => pathInput.SelectReleased(anchor.X + .5f, anchor.Y + 1.5f));
            Assert.DoesNotThrow(pathInput.Detach);
            Assert.That(buffers.CombatViewReadSink.Commands, Is.Empty);
            Assert.That(buffers.CombatViewReadSink.PresentedResultSequence, Is.Zero);
        }

        private sealed class CleanupDisplay : CombatViewSystemTests.FakeDisplay
        {
            internal bool HasDraft, FailReturn;
            internal int DraftClears;
            public override bool TryMapPointer(Vector2 screen, out Vector2 grid, out bool cancel)
            {
                grid = screen;
                cancel = false;
                return true;
            }
            public override void ShowDraft(MovementPath path, MovePreview? preview, bool valid, CellCoord? candidate = null) => HasDraft = true;
            public override void ClearDraft() { HasDraft = false; DraftClears++; }
            public override void SetReturnState(CheckpointReturnPreview preview)
            {
                if (FailReturn) throw new InvalidOperationException("Return UI 정리 실패");
            }
        }
    }
}
