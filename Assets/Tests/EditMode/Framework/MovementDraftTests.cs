using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageInput;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat;

namespace ProjectL.Tests.Framework
{
    public sealed class MovementDraftTests
    {
        [Test]
        public void Snapshot_ReusesImmutablePath_WhenOnlyPointerValidityChanges()
        {
            var draft = new MovementDraft();
            draft.Begin(new EntityId(1), CellCoord.Zero);
            draft.Accept(CellCoord.Up);
            var path = draft.Snapshot();
            long revision = draft.Revision;
            draft.MarkPointerInvalid();
            Assert.That(draft.Snapshot(), Is.SameAs(path));
            draft.Suspend();
            Assert.That(draft.Snapshot(), Is.SameAs(path));
            draft.Accept(CellCoord.Up);
            draft.RetainPrefix(1);
            Assert.That(draft.Snapshot(), Is.SameAs(path));
            Assert.That(draft.Revision, Is.EqualTo(revision));
            Assert.That(draft.PointerValid, Is.False);
        }

        [Test]
        public void Snapshot_EditsReplaceCache_WithoutMutatingPreviousPaths()
        {
            var draft = new MovementDraft();
            draft.Begin(new EntityId(1), CellCoord.Zero);
            var origin = draft.Snapshot();
            draft.Accept(CellCoord.Up);
            var extended = draft.Snapshot();
            Assert.That(extended, Is.Not.SameAs(origin));
            draft.Accept(CellCoord.Zero);
            Assert.That(draft.Snapshot(), Is.Not.SameAs(origin));
            Assert.That(draft.Snapshot(), Is.Not.SameAs(extended));
            draft.Accept(CellCoord.Up);
            draft.Accept(CellCoord.Up + CellCoord.Up);
            var full = draft.Snapshot();
            draft.RetainPrefix(1);
            Assert.That(draft.Snapshot(), Is.Not.SameAs(full));
            Assert.That(draft.Snapshot().Cells, Is.EqualTo(new[] { CellCoord.Zero, CellCoord.Up }));
            Assert.That(origin.Cells, Is.EqualTo(new[] { CellCoord.Zero }));
            Assert.That(extended.Cells, Is.EqualTo(new[] { CellCoord.Zero, CellCoord.Up }));
            Assert.That(full.StepCount, Is.EqualTo(2));
        }

        [Test]
        public void Snapshot_NewGestureAndCancel_DoNotReusePreviousGesture()
        {
            var draft = new MovementDraft();
            draft.Begin(new EntityId(1), CellCoord.Zero);
            var first = draft.Snapshot();
            draft.Begin(new EntityId(1), CellCoord.Zero);
            var second = draft.Snapshot();
            Assert.That(second, Is.Not.SameAs(first));
            draft.Cancel();
            Assert.Throws<InvalidOperationException>(() => draft.Snapshot());
            draft.Begin(new EntityId(2), CellCoord.Zero);
            Assert.That(draft.Snapshot(), Is.Not.SameAs(second));
            Assert.That(first.Cells, Is.EqualTo(new[] { CellCoord.Zero }));
        }

        [Test]
        public void Snapshot_WarmUnchangedReads_DoNotAllocate()
        {
            var draft = new MovementDraft();
            draft.Begin(new EntityId(1), CellCoord.Zero);
            draft.Accept(CellCoord.Up);
            var expected = draft.Snapshot();
            for (int i = 0; i < 32; i++) draft.Snapshot();
            long before = GC.GetAllocatedBytesForCurrentThread();
            MovementPath last = null;
            for (int i = 0; i < 1000; i++) last = draft.Snapshot();
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(last, Is.SameAs(expected));
            Assert.That(allocated, Is.Zero, "측정 범위는 변경 없는 Snapshot 1000회만 포함한다.");
        }

        [Test]
        public void CanStartAt_AllowsOriginAndOrthogonalNeighborsOnly()
        {
            var o = new CellCoord(3, 3);
            Assert.That(MovementDraft.CanStartAt(o, o), Is.True);
            foreach (var step in new[] { CellCoord.Up, CellCoord.Down, CellCoord.Left, CellCoord.Right })
                Assert.That(MovementDraft.CanStartAt(o, o + step), Is.True, step.ToString());
            Assert.That(MovementDraft.CanStartAt(o, o + CellCoord.Up + CellCoord.Right), Is.False, "대각선");
            Assert.That(MovementDraft.CanStartAt(o, o + CellCoord.Up + CellCoord.Up), Is.False, "두 칸");
        }

        [Test]
        public void BeginAtNeighbor_MakesFirstStepAndRequestsPreview_ButBoundaryDoesNot()
        {
            var r = new Rig();
            r.I.Cancel();
            var edge = new Vector2(r.Up.X + .5f, r.Up.Y + .05f);
            Assert.That(r.I.Begin(r.Actor, r.A, edge, r.Allowed), Is.False, "칸 경계 근처에서는 시작하지 않는다.");

            Assert.That(r.I.Begin(r.Actor, r.A, r.Center(r.Up), r.Allowed), Is.True);
            Assert.That(r.I.Snapshot().Cells, Is.EqualTo(new[] { r.A, r.Up }));
            Assert.That(r.I.RetainedPath.Cells, Is.EqualTo(new[] { r.A }));
            Assert.That(r.I.CandidateCell, Is.EqualTo(r.Up));
            Assert.That(r.B.CombatViewReadSink.HasPreview, Is.True);
            r.I.OnPreview(r.Calculate(), r.Allowed);
            Assert.That(r.I.RetainedPath.Cells, Is.EqualTo(new[] { r.A, r.Up }));
        }

        [Test]
        public void Backtrack_TrimsToAnyEarlierPathCell()
        {
            var d = new MovementDraft();
            var a = CellCoord.Zero; var b = CellCoord.Right; var c = b + CellCoord.Up; var e = c + CellCoord.Up;
            d.Begin(new EntityId(1), a);
            Assert.That(d.Accept(b), Is.EqualTo(DraftEditResult.Added));
            Assert.That(d.Accept(c), Is.EqualTo(DraftEditResult.Added));
            Assert.That(d.Accept(e), Is.EqualTo(DraftEditResult.Added));
            var snapshot = d.Snapshot();
            Assert.That(d.Accept(c), Is.EqualTo(DraftEditResult.Removed), "직전 칸");
            Assert.That(d.Snapshot().Cells, Is.EqualTo(new[] { a, b, c }));
            Assert.That(d.Accept(e), Is.EqualTo(DraftEditResult.Added));
            Assert.That(d.Accept(a), Is.EqualTo(DraftEditResult.Removed), "더 앞의 칸도 그 칸까지 자른다.");
            Assert.That(d.Snapshot().Cells, Is.EqualTo(new[] { a }));
            Assert.That(d.PointerValid, Is.True);
            Assert.That(snapshot.Cells, Is.EqualTo(new[] { a, b, c, e }));
        }

        [Test]
        public void SuspendedDraft_ResumesAtAnyPathCell_ButNotAtGap()
        {
            var d = new MovementDraft();
            var a = CellCoord.Zero; var b = CellCoord.Right; var c = b + CellCoord.Up;
            d.Begin(new EntityId(1), a);
            d.Accept(b); d.Accept(c);
            d.Suspend();
            Assert.That(d.CanResumeAt(c + CellCoord.Up), Is.False, "끝점에 붙은 칸은 허용할 때만");
            Assert.That(d.CanResumeAt(c + CellCoord.Up, true), Is.True);
            Assert.That(d.CanResumeAt(c + CellCoord.Up + CellCoord.Up, true), Is.False, "빈 구간은 잇지 않는다.");
            Assert.That(d.CanResumeAt(a), Is.True);
            d.ResumeAt(a, false);
            Assert.That(d.Snapshot().Cells, Is.EqualTo(new[] { a }));
            Assert.That(d.IsSuspended, Is.False);
        }

        [TestCase(0.5f, 0.5f, 1.5f, 1.5f, 1, 0, 1, 1)]
        [TestCase(1.5f, 1.5f, 0.5f, 0.5f, 0, 1, 0, 0)]
        [TestCase(-0.5f, -0.5f, 0.5f, 0.5f, 0, -1, 0, 0)]
        public void FastDiagonal_RecordsBothOrthogonalCells(float x0, float y0, float x1, float y1,
            int ax, int ay, int bx, int by)
        {
            var crossed = new List<CellCoord>();
            MovementPointerSampler.AppendCrossedCells(new Vector2(x0, y0), new Vector2(x1, y1), crossed);
            Assert.That(crossed, Is.EqualTo(new[] { new CellCoord(ax, ay), new CellCoord(bx, by) }));
        }

        [Test]
        public void LongSample_DoesNotOmitIntermediateCells()
        {
            var crossed = new List<CellCoord>();
            MovementPointerSampler.AppendCrossedCells(new Vector2(.5f, .5f), new Vector2(3.5f, .5f), crossed);
            Assert.That(crossed, Is.EqualTo(new[] { new CellCoord(1, 0), new CellCoord(2, 0), new CellCoord(3, 0) }));
        }

        [Test]
        public void BlockedXCorner_DoesNotTryYFirst()
        {
            var r = new Rig();
            Assert.That(r.A + CellCoord.Right, Is.EqualTo(new CellCoord(2, 1)));
            r.Drag(r.A + CellCoord.Right + CellCoord.Up);
            r.I.OnPreview(r.Calculate(), r.Allowed);
            Assert.That(r.I.Snapshot().Cells, Is.EqualTo(new[] { r.A }));
            Assert.That(r.I.PointerValid, Is.False);
            r.I.Confirm(r.A + CellCoord.Right + CellCoord.Up, false, r.Allowed);
            Assert.That(r.B.CombatViewReadSink.Commands, Is.Empty);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void InvalidConfirm_PreservesPathWithoutSubmitting(int kind)
        {
            var r = new Rig(); r.Drag(r.Up); r.I.OnPreview(r.Calculate(), r.Allowed);
            CellCoord? drop = kind == 0 ? null : kind == 1 ? r.A : kind == 2 ? r.Up : r.Up + CellCoord.Up;
            r.I.Confirm(drop, kind == 2, r.Allowed);
            Assert.That(r.B.CombatViewReadSink.Commands, Is.Empty);
            Assert.That(r.I.IsActive, Is.True);
            Assert.That(r.I.RetainedPath.Cells, Is.EqualTo(new[] { r.A, r.Up }));
            Assert.That(r.I.IsAwaitingPreview, Is.False);
        }

        [Test]
        public void ConfirmBeforePreview_SubmitsExactRouteOnce()
        {
            var r = new Rig(); r.Drag(r.Up);
            var frozen = r.I.Snapshot();
            r.I.Confirm(r.Up, false, r.Allowed);
            r.Drag(r.Up + CellCoord.Right);
            Assert.That(r.B.CombatViewReadSink.Commands, Is.Empty);
            var preview = r.Calculate(); r.I.OnPreview(preview, r.Allowed);
            r.I.OnPreview(preview, r.Allowed); r.I.Confirm(r.Up, false, r.Allowed);
            Assert.That(r.B.CombatViewReadSink.Commands.Count, Is.EqualTo(1));
            var command = r.B.CombatViewReadSink.Commands.Single().Data;
            Assert.That(command.Path.Cells, Is.EqualTo(frozen.Cells));
            Assert.That(command.MovementContext, Is.SameAs(preview.MovementContext));
            Assert.That(r.I.Begin(r.Actor, r.A, r.Center(r.A), r.Allowed), Is.False);
            Assert.That(r.Service.ProcessNextCommand(), Is.True);
            Assert.That(r.B.CharacterReadSink.Characters.Single().Anchor, Is.EqualTo(r.Up));
            Assert.That(r.I.Begin(r.Actor, r.Up, r.Center(r.Up), r.Allowed), Is.False);
            r.B.CombatViewWriteSink.AcknowledgePresented(r.B.CombatStateReadSink.LatestResultSequence);
            Assert.That(r.I.Begin(r.Actor, r.Up, r.Center(r.Up), r.Allowed), Is.True);
        }

        [Test]
        public void OldPreviewAfterCancel_DoesNotSubmit()
        {
            var r = new Rig(); r.Drag(r.Up); var old = r.Calculate();
            r.I.Confirm(r.Up, false, r.Allowed); r.I.Cancel(); r.I.OnPreview(old, r.Allowed);
            Assert.That(r.B.CombatViewReadSink.Commands, Is.Empty);
            Assert.That(r.B.CombatViewReadSink.HasPreview, Is.False);
        }

        [Test]
        public void SupersededResponse_DoesNotValidateNewRevision()
        {
            var r = new Rig(); r.Drag(r.Up); var old = r.Calculate();
            r.Drag(r.Up + CellCoord.Right); r.I.Confirm(r.Up + CellCoord.Right, false, r.Allowed);
            r.I.OnPreview(old, r.Allowed);
            Assert.That(r.B.CombatViewReadSink.Commands, Is.Empty);
            r.I.OnPreview(r.Calculate(), r.Allowed);
            Assert.That(r.B.CombatViewReadSink.Commands.Single().Data.Path.StepCount, Is.EqualTo(2));
        }

        [Test]
        public void InvalidPreview_TrimsToCorePrefix_AndCannotConfirmItAutomatically()
        {
            var r = new Rig(); r.Drag(r.Up);
            r.Drag(r.Up + CellCoord.Right); r.Drag(r.Up + CellCoord.Right + CellCoord.Up);
            // 이동 범위를 넘는 접두 이후는 Core가 거부한다.
            r.Drag(r.Up + CellCoord.Right + CellCoord.Up + CellCoord.Right);
            var candidate = r.I.Snapshot();
            var actual = r.Calculate();
            Assert.That(actual.Move.Value.CanMove, Is.False);
            Assert.That(actual.Move.Value.ValidStepCount, Is.LessThan(candidate.StepCount));
            r.I.OnPreview(actual, r.Allowed);
            Assert.That(r.I.Snapshot().StepCount, Is.EqualTo(actual.Move.Value.ValidStepCount));
            Assert.That(r.I.PointerValid, Is.False);
            r.I.Confirm(r.I.Snapshot().Endpoint, false, r.Allowed);
            Assert.That(r.B.CombatViewReadSink.Commands, Is.Empty);
        }

        [Test]
        public void UIReentry_RequiresEndpointOrPredecessor_AndNeverBridgesGap()
        {
            var r = new Rig(); r.Drag(r.Up);
            r.I.PointerMoved(null, true, r.Allowed);
            r.Drag(r.Up + CellCoord.Right);
            Assert.That(r.I.Snapshot().Cells, Is.EqualTo(new[] { r.A, r.Up }));
            Assert.That(r.I.PointerValid, Is.False);
            r.Drag(r.Up);
            r.Drag(r.Up + CellCoord.Right);
            Assert.That(r.I.Snapshot().Cells, Is.EqualTo(new[] { r.A, r.Up, r.Up + CellCoord.Right }));
        }

        [Test]
        public void BoundaryJitter_DoesNotAppendOrBacktrack_UntilInnerCellEntry()
        {
            var r = new Rig();
            r.I.PointerMoved(new Vector2(r.A.X + .5f, r.A.Y + 1.01f), false, r.Allowed);
            r.I.PointerMoved(new Vector2(r.A.X + .5f, r.A.Y + .99f), false, r.Allowed);
            Assert.That(r.I.Snapshot().StepCount, Is.Zero);
            r.Drag(r.Up);
            r.I.PointerMoved(new Vector2(r.A.X + .5f, r.A.Y + .99f), false, r.Allowed);
            Assert.That(r.I.Snapshot().StepCount, Is.EqualTo(1));
            r.Drag(r.A);
            Assert.That(r.I.Snapshot().StepCount, Is.Zero);
        }

        [Test]
        public void PointerOnCellBoundary_IsPendingNotInvalid_AndCannotConfirm()
        {
            var r = new Rig(); r.Drag(r.Up);
            // 다음 칸 경계를 막 넘은 자리(안쪽 영역 밖). 경계선 위에 커서를 둔 상태다.
            r.I.PointerMoved(new Vector2(r.Up.X + .5f, r.Up.Y + 1.05f), false, r.Allowed);
            Assert.That(r.I.Snapshot().Cells, Is.EqualTo(new[] { r.A, r.Up }));
            Assert.That(r.I.PointerValid, Is.True, "미정 칸은 무효로 보이지 않는다.");
            Assert.That(r.I.CandidateCell, Is.EqualTo(r.Up), "받은 끝점을 그대로 가리킨다.");
            r.I.Confirm(r.Up, false, r.Allowed);
            Assert.That(r.I.IsAwaitingPreview, Is.False, "미정 칸이 남아 있으면 확정하지 않는다.");

            r.Drag(r.Up + CellCoord.Up);
            Assert.That(r.I.Snapshot().Cells, Is.EqualTo(new[] { r.A, r.Up, r.Up + CellCoord.Up }));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void FocusOrCaptureLoss_DiscardsFrozenDraft(bool focus)
        {
            var r = new Rig(); r.Drag(r.Up); var old = r.Calculate();
            r.I.Confirm(r.Up, false, r.Allowed);
            if (focus) r.I.FocusLost(); else r.I.CaptureLost();
            r.I.OnPreview(old, r.Allowed);
            Assert.That(r.B.CombatViewReadSink.Commands, Is.Empty);
            Assert.That(r.I.IsActive, Is.False);
        }

        [Test]
        public void LateResponseAfterDetachAndReattach_DoesNotOwnNewGesture()
        {
            var r = new Rig(); r.Drag(r.Up); var old = r.Calculate();
            r.I.Confirm(r.Up, false, r.Allowed); r.I.Detach(); r.I.Attach();
            r.I.Begin(r.Actor, r.A, r.Center(r.A), r.Allowed); r.Drag(r.Up);
            r.I.Confirm(r.Up, false, r.Allowed); r.I.OnPreview(old, r.Allowed);
            Assert.That(r.B.CombatViewReadSink.Commands, Is.Empty);
            r.I.OnPreview(r.Calculate(), r.Allowed);
            Assert.That(r.B.CombatViewReadSink.Commands.Count, Is.EqualTo(1));
        }

        [Test]
        public void BattleEndsDuringValidation_DoesNotSubmit()
        {
            var r = new Rig(); r.Drag(r.Up); var old = r.Calculate();
            r.I.Confirm(r.Up, false, r.Allowed); r.B.BattleWriteSink.EndBattle(); r.I.OnPreview(old, r.Allowed);
            Assert.That(r.B.CombatViewReadSink.Commands, Is.Empty);
            Assert.That(r.I.IsActive, Is.False);
        }

        [Test]
        public void FrozenGesture_RejectsAnotherBegin_UntilValidationCompletes()
        {
            var r = new Rig(); r.Drag(r.Up); r.I.Confirm(r.Up, false, r.Allowed);
            Assert.That(r.I.Begin(r.Actor, r.A, r.Center(r.A), r.Allowed), Is.False);
            Assert.That(r.I.IsAwaitingPreview, Is.True);
            r.I.OnPreview(r.Calculate(), r.Allowed);
            Assert.That(r.B.CombatViewReadSink.Commands.Single().Data.Path.Endpoint, Is.EqualTo(r.Up));
        }

        [Test]
        public void FastSampleBackToOrigin_TrimsToOrigin_AndDropsOwnedPreview()
        {
            var r = new Rig(); r.Drag(r.Up); r.Drag(r.Up + CellCoord.Right);
            Assert.That(r.B.CombatViewReadSink.HasPreview, Is.True);
            // Y 경계를 먼저 지나 새 칸에 진입하고, 같은 샘플에서 원점을 만나 원점까지 자른다(2026-10-09 전에는 거부했다).
            r.I.PointerMoved(new Vector2(r.A.X + .8f, r.A.Y + .2f), false, r.Allowed);
            Assert.That(r.I.Snapshot().Cells, Is.EqualTo(new[] { r.A }));
            Assert.That(r.I.PointerValid, Is.True);
            Assert.That(r.B.CombatViewReadSink.HasPreview, Is.False, "걸음이 없으면 자기 견적 요청을 거둔다.");
        }

        [Test]
        public void FrozenInvalidPreview_KeepsPrefixAndAllowsAnotherCandidate()
        {
            var r = new Rig(); r.Drag(r.A + CellCoord.Right);
            r.I.Confirm(r.A + CellCoord.Right, false, r.Allowed); r.I.OnPreview(r.Calculate(), r.Allowed);
            Assert.That(r.B.CombatViewReadSink.Commands, Is.Empty);
            Assert.That(r.I.IsActive, Is.True);
            Assert.That(r.I.IsAwaitingPreview, Is.False);
            Assert.That(r.I.RetainedPath.Cells, Is.EqualTo(new[] { r.A }));
            r.Drag(r.Up);
            r.I.Confirm(r.Up, false, r.Allowed); r.I.OnPreview(r.Calculate(), r.Allowed);
            Assert.That(r.B.CombatViewReadSink.Commands.Single().Data.Path.Cells, Is.EqualTo(new[] { r.A, r.Up }));
        }

        [Test]
        public void CoreRejectedContinuation_ResumesAtAdjacentCell_AndRevalidates()
        {
            var r = new Rig(); r.Drag(r.Up); r.Drag(r.Up + CellCoord.Right);
            r.Drag(r.A + CellCoord.Right);
            var invalid = r.Calculate(); r.I.OnPreview(invalid, r.Allowed);
            Assert.That(r.I.Snapshot().Cells, Is.EqualTo(new[] { r.A, r.Up, r.Up + CellCoord.Right }));
            Assert.That(r.I.ValidatedRevision, Is.EqualTo(r.I.DraftRevision));
            r.Drag(r.Up + CellCoord.Right + CellCoord.Up);
            Assert.That(r.I.PointerValid, Is.True);
            Assert.That(r.I.RetainedPath.StepCount, Is.EqualTo(2), "새 후보는 아직 표시 경로에 편입하지 않는다.");
            r.I.Confirm(r.Up + CellCoord.Right + CellCoord.Up, false, r.Allowed); r.I.OnPreview(invalid, r.Allowed);
            Assert.That(r.B.CombatViewReadSink.Commands, Is.Empty);
            r.I.OnPreview(r.Calculate(), r.Allowed);
            Assert.That(r.B.CombatViewReadSink.Commands.Single().Data.Path.StepCount, Is.EqualTo(3));
        }

        [Test]
        public void ReinstalledBattleWithSameActorAndPath_DoesNotAcceptOldPreview()
        {
            var r = new Rig(); r.Drag(r.Up); var old = r.Calculate();
            r.I.Confirm(r.Up, false, r.Allowed);
            var actor = r.B.CharacterReadSink.Characters.Single().ToFacts();
            var state = r.B.CombatStateReadSink.Current.CommandState;
            r.B.BattleWriteSink.EndBattle();
            r.B.BattleWriteSink.PrepareBattle(new BattlePreparation(new[] { actor }, CharacterBufferTestData.Combat(),
                state.Structures, commandState: state));
            Assert.That(r.B.CombatStateReadSink.BattleInstanceId, Is.GreaterThan(old.MovementContext.BattleInstanceId));
            r.I.OnPreview(old, r.Allowed);
            Assert.That(r.B.CombatViewReadSink.Commands, Is.Empty);
            Assert.That(r.I.IsActive, Is.False);
        }

        [Test]
        public void PendingCandidate_IsSeparateFromRetainedPath_AndOldAnswerCannotPromoteIt()
        {
            var r = new Rig();
            var retained = r.I.RetainedPath;
            r.Drag(r.Up);
            var old = r.Calculate();
            Assert.That(r.I.RetainedPath, Is.SameAs(retained));
            Assert.That(r.I.CandidateCell, Is.EqualTo(r.Up));
            r.Drag(r.Up + CellCoord.Right);
            r.I.OnPreview(old, r.Allowed);
            Assert.That(r.I.RetainedPath, Is.SameAs(retained));
            r.I.OnPreview(r.Calculate(), r.Allowed);
            Assert.That(r.I.RetainedPath.Cells, Is.EqualTo(new[] { r.A, r.Up, r.Up + CellCoord.Right }));
            Assert.That(r.B.CombatViewReadSink.Commands, Is.Empty);
        }

        [Test]
        public void RejectedCell_IsNotRequeried_AndFarCellCannotBridgeToAlternative()
        {
            var r = new Rig();
            var blocked = r.A + CellCoord.Right;
            r.Drag(blocked); r.I.OnPreview(r.Calculate(), r.Allowed);
            var retained = r.I.RetainedPath;
            Assert.That(retained.Cells, Is.EqualTo(new[] { r.A }));
            Assert.That(r.I.CandidateCell, Is.EqualTo(blocked));
            for (int i = 0; i < 20; i++)
            {
                r.Drag(blocked);
                r.I.Confirm(blocked, false, r.Allowed);
            }
            Assert.That(r.B.CombatViewReadSink.HasPreview, Is.False);
            Assert.That(r.B.CombatViewReadSink.Commands, Is.Empty);
            r.Drag(r.Up + CellCoord.Right);
            Assert.That(r.I.PointerValid, Is.False);
            Assert.That(r.B.CombatViewReadSink.HasPreview, Is.False);
            r.Drag(r.Up);
            Assert.That(r.I.RetainedPath, Is.SameAs(retained));
            Assert.That(r.B.CombatViewReadSink.Preview.Data.Path.Cells, Is.EqualTo(new[] { r.A, r.Up }));
            r.I.OnPreview(r.Calculate(), r.Allowed);
            r.I.Confirm(r.Up, false, r.Allowed);
            Assert.That(r.B.CombatViewReadSink.Commands.Single().Data.Path.Cells, Is.EqualTo(new[] { r.A, r.Up }));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void UiExitBeforeOrAfterRejection_StillRequiresEndpointReentry(bool beforeResponse)
        {
            var r = new Rig();
            r.Drag(r.A + CellCoord.Right);
            var rejected = r.Calculate();
            if (!beforeResponse) r.I.OnPreview(rejected, r.Allowed);
            r.I.PointerMoved(null, true, r.Allowed);
            if (beforeResponse) r.I.OnPreview(rejected, r.Allowed);
            r.Drag(r.Up);
            Assert.That(r.I.PointerValid, Is.False);
            Assert.That(r.B.CombatViewReadSink.HasPreview, Is.False);
            r.Drag(r.A); r.Drag(r.Up);
            r.I.OnPreview(r.Calculate(), r.Allowed);
            Assert.That(r.I.RetainedPath.Cells, Is.EqualTo(new[] { r.A, r.Up }));
        }

        [Test]
        public void SuspendedDraft_ResumesNextToAnyPathCell_ByTrimmingToIt()
        {
            var d = new MovementDraft();
            var a = CellCoord.Zero; var b = CellCoord.Right; var c = b + CellCoord.Right;
            d.Begin(new EntityId(1), a);
            d.Accept(b); d.Accept(c);
            d.Suspend();
            var besideStart = a + CellCoord.Up;
            Assert.That(d.CanResumeAt(besideStart), Is.False, "붙은 칸은 허용할 때만");
            Assert.That(d.CanResumeAt(besideStart, true), Is.True);
            Assert.That(d.CanResumeAt(new CellCoord(5, 5), true), Is.False, "어느 경로 칸에도 붙지 않은 칸");
            d.ResumeAt(besideStart, true);
            Assert.That(d.Snapshot().Cells, Is.EqualTo(new[] { a, besideStart }));
            Assert.That(d.IsSuspended, Is.False);
            Assert.That(d.PointerValid, Is.True);

            // 잘려 나간 칸(b, c)에만 붙은 칸에서는 다시 잇지 않는다.
            d.Suspend();
            Assert.That(d.CanResumeAt(c + CellCoord.Right, true), Is.False);
        }

        [Test]
        public void OccupiedDestination_ThenCursorBesideMovingUnit_RestartsFromUnit()
        {
            var r = new Rig();
            var a1 = r.A + CellCoord.Up; var a2 = a1 + CellCoord.Up; var occupied = a2 + CellCoord.Up;
            r.Drag(a1); r.Drag(a2); r.Drag(occupied);
            int request = r.B.CombatViewReadSink.Preview.Number;
            // Core는 다른 유닛 칸 앞(2걸음)까지만 유효하다고 답한다.
            r.I.OnPreview(CombatPreviewResult.ForMove(request,
                MovePreview.Failure(MovementFailureReason.DestinationOccupied, 2)), r.Allowed);
            Assert.That(r.I.Snapshot().Cells, Is.EqualTo(new[] { r.A, a1, a2 }));
            Assert.That(r.I.PointerValid, Is.False);

            // 이동 유닛 옆(경로 밖) 칸에 커서를 두면 유닛 칸까지 자르고 그 칸으로 잇는다.
            var side = r.A + CellCoord.Left;
            r.Drag(side);
            Assert.That(r.I.Snapshot().Cells, Is.EqualTo(new[] { r.A, side }));
            Assert.That(r.I.PointerValid, Is.True);
            Assert.That(r.B.CombatViewReadSink.Preview.Data.Path.Cells, Is.EqualTo(new[] { r.A, side }));
        }

        [Test]
        public void PointerCuttingBackToEarlierPathCell_TrimsInsteadOfSuspending()
        {
            var r = new Rig();
            var right = r.A + CellCoord.Right; var corner = right + CellCoord.Up;
            r.Drag(right); r.Drag(corner);
            Assert.That(r.I.Snapshot().Cells, Is.EqualTo(new[] { r.A, right, corner }));
            // 대각선으로 시작 칸까지 돌아오면 경로 밖 칸(r.Up)을 거쳐 시작 칸에 닿는다. 예전에는 여기서 멈췄다.
            r.Drag(r.A);
            Assert.That(r.I.Snapshot().Cells, Is.EqualTo(new[] { r.A }));
            Assert.That(r.I.PointerValid, Is.True);
        }

        [Test]
        public void BacktrackingTrimsRetainedPath_AndNewBranchNeedsFreshApproval()
        {
            var r = new Rig();
            r.Drag(r.Up); r.Drag(r.Up + CellCoord.Right);
            var old = r.Calculate(); r.I.OnPreview(old, r.Allowed);
            r.Drag(r.Up);
            Assert.That(r.I.RetainedPath.Cells, Is.EqualTo(new[] { r.A, r.Up }));
            r.Drag(r.Up + CellCoord.Up);
            var pending = r.I.RetainedPath;
            Assert.That(pending.Cells, Is.EqualTo(new[] { r.A, r.Up }));
            for (int i = 0; i < 20; i++) r.Drag(r.Up + CellCoord.Up);
            Assert.That(r.I.RetainedPath, Is.SameAs(pending));
            r.I.OnPreview(old, r.Allowed);
            Assert.That(r.I.RetainedPath, Is.SameAs(pending));
            r.I.Confirm(r.Up + CellCoord.Up, false, r.Allowed);
            r.I.OnPreview(r.Calculate(), r.Allowed);
            Assert.That(r.B.CombatViewReadSink.Commands.Single().Data.Path.Cells,
                Is.EqualTo(new[] { r.A, r.Up, r.Up + CellCoord.Up }));
        }

        private sealed class Rig
        {
            internal readonly IBufferQuery B;
            internal readonly CombatProcessingService Service;
            internal readonly CombatMovementInteraction I;
            internal readonly EntityId Actor;
            internal readonly CellCoord A;
            internal CellCoord Up => A + CellCoord.Up;
            internal Rig()
            {
                B = CombatPreviewPublicationTests.Create(out Service);
                var actor = B.CharacterReadSink.Characters.Single(); Actor = actor.EntityId; A = actor.Anchor.Value;
                I = new CombatMovementInteraction(B.CombatViewWriteSink, B.CombatViewReadSink, B.CombatStateReadSink, .15f);
                I.Attach(); Assert.That(I.Begin(Actor, A, Center(A), Allowed), Is.True);
            }
            // 호출자(CombatPathInput) 판단의 대역이다. 이 단독 테스트들이 기대하던 옛 PresentationAllowsInput 조건
            // (확인 순번 P ≥ 발행 순번 S, 처리 번호 뒤 미처리 명령 없음)을 호출마다 새로 평가한다(구조 개선 VR1).
            internal bool Allowed => B.CombatViewReadSink.PresentedResultSequence >= B.CombatStateReadSink.LatestResultSequence &&
                !B.CombatViewReadSink.TryGetNextCommand(B.CombatStateReadSink.HandledCommandNumber, out _);
            internal Vector2 Center(CellCoord c) => new Vector2(c.X + .5f, c.Y + .5f);
            internal void Drag(CellCoord c) => I.PointerMoved(Center(c), false, Allowed);
            internal CombatPreviewResult Calculate()
            {
                Assert.That(Service.ProcessLatestPreview(), Is.True);
                Assert.That(B.CombatStateReadSink.TryGetLatestPreview(out var preview), Is.True);
                return preview;
            }
        }
    }
}
