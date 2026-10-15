using System;
using System.Reflection;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat;
using UnityEngine;

namespace ProjectL.Tests.Framework
{
    /// <summary>반복 표시는 생략해도 검증·포인터 유효성·취소/재진입·명령 제출은 유지합니다.</summary>
    public sealed class CombatMovementFeedbackTests
    {
        [Test]
        public void PointerAndClickWithoutQ_DoNotStartPath()
        {
            var rig = new Rig();
            rig.Move(rig.Origin);
            rig.Input.SelectPressed(rig.Origin.X + .5f, rig.Origin.Y + .5f);
            rig.Move(rig.Up);
            rig.Input.SelectReleased(rig.Up.X + .5f, rig.Up.Y + .5f);
            Assert.That(rig.Display.Path, Is.Null);
            Assert.That(rig.B.CombatViewReadSink.HasPreview, Is.False);
            Assert.That(rig.B.CombatViewReadSink.Commands, Is.Empty);
        }

        [Test]
        public void Q_WaitsForStartCell_ThenCollectsWithoutHoldingButton()
        {
            var rig = new Rig();
            rig.Input.BeginPlanning();
            // 행위자 칸이나 상하좌우로 붙은 칸이 아니면 시작하지 않는다.
            var far = rig.Up + CellCoord.Up;
            rig.Move(far);
            rig.Input.SelectPressed(far.X + .5f, far.Y + .5f);
            rig.Display.CancelArea = true;
            rig.Move(rig.Origin);
            Assert.That(rig.Display.Path, Is.Null);
            Assert.That(rig.B.CombatViewReadSink.HasPreview, Is.False);
            rig.Display.CancelArea = false;
            rig.Move(rig.Origin);
            rig.Move(rig.Up);
            Assert.That(rig.Display.Path.Cells, Is.EqualTo(new[] { rig.Origin }));
            Assert.That(rig.Display.Candidate, Is.EqualTo(rig.Up));
            Assert.That(rig.B.CombatViewReadSink.HasPreview, Is.True);
            rig.Input.OnPreview(rig.Calculate());
            Assert.That(rig.Display.Path.Cells, Is.EqualTo(new[] { rig.Origin, rig.Up }));
            Assert.That(rig.B.CombatViewReadSink.Commands, Is.Empty);
        }

        [Test]
        public void Q_CanStartAtOrthogonalNeighbor_ButNotDiagonal()
        {
            var rig = new Rig();
            rig.Input.BeginPlanning();
            rig.Move(rig.Up + CellCoord.Right);
            Assert.That(rig.Display.Path, Is.Null, "대각선 칸에서는 시작하지 않는다.");
            Assert.That(rig.B.CombatViewReadSink.HasPreview, Is.False);

            rig.Move(rig.Up);
            Assert.That(rig.Display.Path.Cells, Is.EqualTo(new[] { rig.Origin }), "Core 승인 전에는 행위자 칸만 확정 경로다.");
            Assert.That(rig.Display.Candidate, Is.EqualTo(rig.Up));
            Assert.That(rig.B.CombatViewReadSink.HasPreview, Is.True);
            rig.Input.OnPreview(rig.Calculate());
            Assert.That(rig.Display.Path.Cells, Is.EqualTo(new[] { rig.Origin, rig.Up }));

            rig.Input.SelectPressed(rig.Up.X + .5f, rig.Up.Y + .5f);
            Assert.That(rig.B.CombatViewReadSink.Commands.Count, Is.EqualTo(1));
            Assert.That(rig.B.CombatViewReadSink.Commands[0].Data.Path.Cells, Is.EqualTo(new[] { rig.Origin, rig.Up }));
        }

        [Test]
        public void PathPlanning_IsShownFromQ_UntilCancelOrSubmit()
        {
            var rig = new Rig();
            rig.Input.BeginPlanning();
            Assert.That(rig.Display.Planning, Is.True, "시작 칸을 고르기 전부터 경로 지정 중이다.");
            rig.Input.Cancel();
            Assert.That(rig.Display.Planning, Is.False);

            rig.Start();
            Assert.That(rig.Display.Planning, Is.True);
            int calls = rig.Display.PlanningCalls;
            rig.Move(rig.Up);
            rig.Input.OnPreview(rig.Calculate());
            Assert.That(rig.Display.PlanningCalls, Is.EqualTo(calls), "값이 그대로면 다시 알리지 않는다.");
            rig.Input.SelectPressed(rig.Up.X + .5f, rig.Up.Y + .5f);
            Assert.That(rig.B.CombatViewReadSink.Commands.Count, Is.EqualTo(1));
            Assert.That(rig.Display.Planning, Is.False, "제출 뒤에는 경로 지정이 끝난다.");

            var detached = new Rig();
            detached.Input.BeginPlanning();
            Assert.That(detached.Display.Planning, Is.True);
            detached.Input.Detach();
            Assert.That(detached.Display.Planning, Is.False, "이탈하면 끈다.");
        }

        [Test]
        public void ReleaseDoesNotSubmit_ClickFreezesAndSubmitsOnce_ThenRequiresQ()
        {
            var rig = new Rig(); rig.Start(); rig.Move(rig.Up);
            rig.Input.SelectReleased(rig.Up.X + .5f, rig.Up.Y + .5f);
            rig.Input.OnPreview(rig.Calculate());
            Assert.That(rig.B.CombatViewReadSink.Commands, Is.Empty);
            rig.Input.SelectPressed(rig.Up.X + .5f, rig.Up.Y + .5f);
            rig.Input.SelectReleased(rig.Up.X + .5f, rig.Up.Y + .5f);
            rig.Input.SelectPressed(rig.Up.X + .5f, rig.Up.Y + .5f);
            Assert.That(rig.B.CombatViewReadSink.Commands.Count, Is.EqualTo(1));
            Assert.That(rig.Service.ProcessNextCommand(), Is.True);
            rig.B.CombatViewWriteSink.RemoveCommandsThrough(rig.B.CombatStateReadSink.HandledCommandNumber);
            rig.B.CombatViewWriteSink.AcknowledgePresented(rig.B.CombatStateReadSink.LatestResultSequence);
            rig.Move(rig.Up); rig.Move(rig.Origin);
            rig.Input.SelectPressed(rig.Origin.X + .5f, rig.Origin.Y + .5f);
            Assert.That(rig.Display.Path, Is.Null);
            Assert.That(rig.B.CombatViewReadSink.Commands, Is.Empty);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void UiOrOutsideClick_DoesNotConfirm_AndCanResumeAtEndpoint(bool outside)
        {
            var rig = new Rig(); rig.Start(); rig.Move(rig.Up);
            rig.Input.OnPreview(rig.Calculate());
            rig.Display.CancelArea = !outside;
            rig.Input.SelectPressed(outside ? -1f : rig.Up.X + .5f, rig.Up.Y + .5f);
            Assert.That(rig.B.CombatViewReadSink.Commands, Is.Empty);
            Assert.That(rig.Display.Path, Is.Not.Null);
            Assert.That(rig.Display.PointerValid, Is.False);
            rig.Display.CancelArea = false;
            rig.Move(rig.Up);
            rig.Input.SelectPressed(rig.Up.X + .5f, rig.Up.Y + .5f);
            Assert.That(rig.B.CombatViewReadSink.Commands.Count, Is.EqualTo(1));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CancelWhileWaitingOrFrozen_DropsLateInputAndRequiresQ(bool frozen)
        {
            var rig = new Rig();
            rig.Input.BeginPlanning();
            CombatPreviewResult preview = null;
            if (frozen)
            {
                rig.Move(rig.Origin); rig.Move(rig.Up);
                preview = rig.Calculate();
                rig.Input.SelectPressed(rig.Up.X + .5f, rig.Up.Y + .5f);
            }
            rig.Input.Cancel();
            if (preview != null) rig.Input.OnPreview(preview);
            rig.Move(rig.Origin); rig.Move(rig.Up);
            rig.Input.SelectPressed(rig.Up.X + .5f, rig.Up.Y + .5f);
            Assert.That(rig.Display.Path, Is.Null);
            Assert.That(rig.B.CombatViewReadSink.Commands, Is.Empty);
            rig.Start(); rig.Move(rig.Up);
            Assert.That(rig.Display.Path, Is.Not.Null);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void InputBlockedWhileWaitingOrEditing_CancelsWithoutAutomaticRearm(bool editing)
        {
            var rig = new Rig();
            rig.Input.BeginPlanning();
            if (editing) { rig.Move(rig.Origin); rig.Move(rig.Up); }
            rig.AllowsInput = false;
            rig.Input.RefreshReturnPreview();
            rig.AllowsInput = true;
            rig.Move(rig.Origin); rig.Move(rig.Up);
            Assert.That(rig.Display.Path, Is.Null);
            Assert.That(rig.B.CombatViewReadSink.HasPreview, Is.False);
            Assert.That(rig.B.CombatViewReadSink.Commands, Is.Empty);
        }

        [Test]
        public void SameAcceptedPreview_SkipsDisplay_ButValidityChangesStillRender()
        {
            var rig = new Rig();
            rig.Start(); rig.Move(rig.Up);
            var preview = rig.Calculate(); rig.Input.OnPreview(preview);
            int calls = rig.Display.Shows;
            var path = rig.Display.Path;
            for (int i = 0; i < 20; i++) rig.Input.OnPreview(preview);
            Assert.That(rig.Display.Shows, Is.EqualTo(calls));
            rig.Display.CancelArea = true;
            rig.Move(rig.Up);
            Assert.That(rig.Display.Shows, Is.EqualTo(calls + 1));
            Assert.That(rig.Display.PointerValid, Is.False);
            Assert.That(rig.Display.Path, Is.SameAs(path));
            rig.Display.CancelArea = false;
            rig.Move(rig.Up);
            Assert.That(rig.Display.PointerValid, Is.True);
            Assert.That(rig.Display.Shows, Is.EqualTo(calls + 2));
            Assert.That(rig.B.CombatViewReadSink.Commands, Is.Empty);
        }

        [Test]
        public void InputDisabledWithCachedPreview_ClearsDraftBeforeDeduplication()
        {
            var rig = new Rig(); rig.Start(); rig.Move(rig.Up);
            var preview = rig.Calculate(); rig.Input.OnPreview(preview);
            int shows = rig.Display.Shows;
            rig.AllowsInput = false;
            rig.Input.OnPreview(preview);
            rig.Input.OnPreview(preview);
            Assert.That(rig.Display.Shows, Is.EqualTo(shows));
            Assert.That(rig.Display.Path, Is.Null, "공격 모드 등으로 입력이 닫히면 같은 응답이어도 표시를 정리한다.");
            Assert.That(rig.Display.Clears, Is.EqualTo(1));
            Assert.That(rig.B.CombatViewReadSink.Commands, Is.Empty);
        }

        [Test]
        public void UnacceptedOldResponse_DoesNotReplaceDisplayCache()
        {
            var rig = new Rig(); rig.Start(); rig.Move(rig.Up);
            var old = rig.Calculate(); rig.Input.OnPreview(old);
            rig.Move(rig.Up + CellCoord.Right);
            var current = rig.Calculate(); rig.Input.OnPreview(current);
            int calls = rig.Display.Shows;
            rig.Input.OnPreview(old);
            rig.Input.OnPreview(current);
            Assert.That(rig.Display.Shows, Is.EqualTo(calls));
            Assert.That(rig.Display.Path.StepCount, Is.EqualTo(2));
        }

        [Test]
        public void SamePathNewQuote_Renders_AndInvalidContextStillCancels()
        {
            var rig = new Rig(); rig.Start(); rig.Move(rig.Up);
            var original = rig.Calculate(); rig.Input.OnPreview(original);
            int calls = rig.Display.Shows;
            MovePreview move = original.Move.Value;
            MovePreview changed = MovePreview.Success(move.PathMovement, move.ProjectedTraitStatuses.ToArray(),
                move.ProjectedCategory, move.ProjectedActor, move.PublicEffects, move.QuotedActionCost + 1, move.CanReturn);
            rig.Input.OnPreview(CombatPreviewResult.ForMove(original.RequestNumber, changed, original.MovementContext));
            Assert.That(rig.Display.Shows, Is.EqualTo(calls + 1));
            Assert.That(rig.Display.Preview.Value.QuotedActionCost, Is.EqualTo(move.QuotedActionCost + 1));
            rig.Input.OnPreview(CombatPreviewResult.ForMove(original.RequestNumber, changed));
            Assert.That(rig.Display.Path, Is.Null, "표시값이 같아도 context 없는 응답을 검증해야 한다.");
            Assert.That(rig.Display.Clears, Is.EqualTo(1));
            Assert.That(rig.B.CombatViewReadSink.Commands, Is.Empty);
        }

        [Test]
        public void RejectedPreview_TrimsPath_AndRendersInvalidPrefix()
        {
            var rig = new Rig(); rig.Start(); rig.Move(rig.Up);
            int request = rig.B.CombatViewReadSink.Preview.Number;
            rig.Input.OnPreview(CombatPreviewResult.ForMove(request,
                MovePreview.Failure(MovementFailureReason.MoveRangeExceeded, 0)));
            Assert.That(rig.Display.Path.StepCount, Is.Zero);
            Assert.That(rig.Display.PointerValid, Is.False);
            Assert.That(rig.Display.Preview.Value.CanMove, Is.False);
            Assert.That(rig.B.CombatViewReadSink.Commands, Is.Empty);
        }

        [Test]
        public void CancelAndRestartSamePath_RendersAgain_AndOldPreviewCannotSubmit()
        {
            var rig = new Rig(); rig.Start(); rig.Move(rig.Up);
            var old = rig.Calculate(); rig.Input.OnPreview(old);
            var first = rig.Display.Path;
            rig.Input.CancelInputs();
            rig.Input.OnPreview(old);
            Assert.That(rig.Display.Path, Is.Null);
            rig.Start(); rig.Move(rig.Up);
            Assert.That(rig.Display.Path, Is.Not.SameAs(first));
            rig.Input.SelectPressed(rig.Up.X + .5f, rig.Up.Y + .5f);
            rig.Input.OnPreview(old);
            Assert.That(rig.B.CombatViewReadSink.Commands, Is.Empty);
            rig.Input.OnPreview(rig.Calculate());
            Assert.That(rig.B.CombatViewReadSink.Commands.Count, Is.EqualTo(1));
        }

        [Test]
        public void FailedDisplay_IsRetriedForSameValues()
        {
            var rig = new Rig(); rig.Start();
            rig.Display.OnShow = () => throw new InvalidOperationException("display failed");
            Assert.Throws<InvalidOperationException>(() => rig.Move(rig.Up));
            rig.Display.OnShow = null;
            int calls = rig.Display.Shows;
            rig.Move(rig.Up);
            Assert.That(rig.Display.Shows, Is.EqualTo(calls + 1));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CancelOrDetachDuringDisplay_DoesNotRestoreOldCache(bool detach)
        {
            var rig = new Rig(); rig.Start();
            rig.Display.OnShow = () =>
            {
                rig.Display.OnShow = null;
                if (detach) rig.Input.Detach(); else rig.Input.CancelInputs();
            };
            rig.Move(rig.Up);
            Assert.That(rig.Display.Path, Is.Null);
            Assert.That(typeof(CombatPathInput).GetField("_shownPath", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(rig.Input), Is.Null, "재진입 정리 뒤 이전 표시를 유효 캐시로 되살리지 않는다.");
            Assert.That(rig.B.CombatViewReadSink.Commands, Is.Empty);
        }

        [Test]
        public void NewGestureDuringDisplay_KeepsInnerDisplayAsLastApplied()
        {
            var rig = new Rig(); rig.Start();
            rig.Display.OnShow = () =>
            {
                rig.Display.OnShow = null;
                rig.Input.CancelInputs(); rig.Start(); rig.Move(rig.Up + CellCoord.Right);
            };
            rig.Move(rig.Up);
            int calls = rig.Display.Shows;
            rig.Move(rig.Up + CellCoord.Right);
            Assert.That(rig.Display.Shows, Is.EqualTo(calls));
            Assert.That(rig.Display.Path.Endpoint, Is.EqualTo(rig.Origin));
            Assert.That(rig.Display.Candidate, Is.EqualTo(rig.Up + CellCoord.Right));
        }

        [Test]
        public void DuplicateReturnPreview_SubmitsOnlyOnce()
        {
            var rig = new Rig(); rig.Start(); rig.Move(rig.Up);
            rig.Input.SelectPressed(rig.Up.X + .5f, rig.Up.Y + .5f);
            rig.Input.OnPreview(rig.Calculate());
            Assert.That(rig.Service.ProcessNextCommand(), Is.True);
            rig.B.CombatViewWriteSink.RemoveCommandsThrough(rig.B.CombatStateReadSink.HandledCommandNumber);
            rig.B.CombatViewWriteSink.AcknowledgePresented(rig.B.CombatStateReadSink.LatestResultSequence);
            rig.Input.RequestReturn();
            var returned = rig.Calculate();
            rig.Input.OnPreview(returned); rig.Input.OnPreview(returned);
            Assert.That(rig.B.CombatViewReadSink.Commands.Count, Is.EqualTo(1));
        }

        [Test]
        public void BlockedCandidate_OnlyChangesMarker_AndAlternativeRefreshesDespiteSameRetainedPath()
        {
            var rig = new Rig(); rig.Start();
            rig.Move(rig.Origin + CellCoord.Right);
            Assert.That(rig.Display.Path.Cells, Is.EqualTo(new[] { rig.Origin }));
            Assert.That(rig.Display.Candidate, Is.EqualTo(rig.Origin + CellCoord.Right));
            Assert.That(rig.Display.Preview, Is.Null);
            rig.Input.OnPreview(rig.Calculate());
            Assert.That(rig.Display.Path.Cells, Is.EqualTo(new[] { rig.Origin }));
            Assert.That(rig.Display.PointerValid, Is.False);
            Assert.That(rig.Display.Preview.Value.CanMove, Is.False);
            int shows = rig.Display.Shows;
            rig.Move(rig.Up);
            Assert.That(rig.Display.Shows, Is.EqualTo(shows + 1));
            Assert.That(rig.Display.Candidate, Is.EqualTo(rig.Up));
            Assert.That(rig.Display.Path.Cells, Is.EqualTo(new[] { rig.Origin }));
            Assert.That(rig.Display.Preview, Is.Null);
            rig.Input.OnPreview(rig.Calculate());
            Assert.That(rig.Display.Path.Cells, Is.EqualTo(new[] { rig.Origin, rig.Up }));
        }

        internal sealed class Rig
        {
            internal readonly IBufferQuery B;
            internal readonly CombatProcessingService Service;
            internal readonly Probe Display = new();
            internal readonly CombatPathInput Input;
            internal readonly CellCoord Origin;
            internal bool AllowsInput = true;
            internal CellCoord Up => Origin + CellCoord.Up;
            internal Rig()
            {
                B = CombatPreviewPublicationTests.Create(out Service);
                Origin = B.CharacterReadSink.Characters[0].Anchor.Value;
                Input = new CombatPathInput(B.CombatStateReadSink, B.CharacterReadSink, B.CombatViewReadSink,
                    B.CombatViewWriteSink, Display, Display, () => PathInputTestFacts.Read(B, AllowsInput));
            }
            internal void Start()
            {
                Input.BeginPlanning();
                Move(Origin);
            }
            internal void Move(CellCoord cell) => Input.PointerMoved(cell.X + .5f, cell.Y + .5f);
            internal CombatPreviewResult Calculate()
            {
                Assert.That(Service.ProcessLatestPreview(), Is.True);
                Assert.That(B.CombatStateReadSink.TryGetLatestPreview(out var preview), Is.True);
                return preview;
            }
        }

        internal sealed class Probe : ICombatMovementFeedback, ICombatPointerMapping
        {
            internal int Shows, Clears;
            internal bool CancelArea, PointerValid;
            internal MovementPath Path;
            internal CellCoord? Candidate;
            internal MovePreview? Preview;
            internal Action OnShow;
            public float InnerCellInset => .15f;
            public void SetReturnState(CheckpointReturnPreview preview) { }
            public bool TryMapPointer(Vector2 screen, out Vector2 grid, out bool cancel)
            { grid = screen; cancel = CancelArea; return true; }
            public void ShowDraft(MovementPath path, MovePreview? preview, bool valid, CellCoord? candidate = null)
            {
                Shows++; Path = path; Preview = preview; PointerValid = valid; Candidate = candidate;
                OnShow?.Invoke();
            }
            public void ClearDraft() { Clears++; Path = null; }
            internal bool Planning;
            internal int PlanningCalls;
            public void SetPathPlanning(bool planning) { Planning = planning; PlanningCalls++; }
        }
    }
}
