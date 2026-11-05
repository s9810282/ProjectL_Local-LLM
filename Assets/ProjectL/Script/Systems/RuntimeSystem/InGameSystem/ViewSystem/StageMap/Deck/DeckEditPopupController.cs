using System;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Deck;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.Deck
{
    /// <summary>
    /// 덱 편집 팝업 한 번의 수명입니다. Core 덱 서비스(Read·Evaluate·Confirm)를 부르는 곳은 이 클래스 하나입니다.
    /// 수락된 확정은 다시 부르지 않습니다(런 덱 Core → View 인계 §3). 변경 알림이 없으므로 열 때·다시 평가·평가/확정 결과에서만 다시 읽습니다.
    /// </summary>
    internal sealed class DeckEditPopupController : IPopupViewController, IDeckEditInput, IDeckCombatInput, IDisposable
    {
        private readonly IRunDeckService _deck;
        private readonly IDeckEditDisplay _screen;
        private readonly Func<bool> _canOpen;
        private readonly StagePopupViewLifetime _lifetime;
        private readonly DeckEditDraft _draft = new();
        private bool _shown;
        private readonly IUISystemService _ui;
        private readonly IDeckCombatDisplay _departureScreen;
        private Action _depart;
        private Action _cancelDeparture;
        private bool _submitting;
        private RunId _departureRun;

        /// <param name="canOpen">다른 팝업·노드 이동처럼 열기를 막는 맵 쪽 조건입니다. 편집 가능 여부는 Core가 판단합니다.</param>
        internal DeckEditPopupController(IRunDeckService deck, IUISystemService ui, IDeckEditDisplay screen,
            PopupInputSession input, IPopupViewHost host, Func<bool> canOpen, RuntimeFaultBoundary faults)
        {
            _deck = deck ?? throw new ArgumentNullException(nameof(deck));
            _screen = screen ?? throw new ArgumentNullException(nameof(screen));
            _canOpen = canOpen ?? throw new ArgumentNullException(nameof(canOpen));
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            _departureScreen = screen as IDeckCombatDisplay;
            _lifetime = new StagePopupViewLifetime("DeckEditPopup", ui, new DeckEditPopupViewScreen(screen), input,
                this, host, faults);
        }

        internal bool IsOpen => _lifetime.IsOpen;

        internal void Start() => _lifetime.Start(() => _screen.BindOpenRequest(RequestOpen), Dispose);

        internal void Tick() => _lifetime.Tick();

        internal void CloseCombat()
        {
            if (_depart != null && _lifetime.IsActive) _lifetime.Close(false);
        }

        /// <summary>착지 후 출전용으로 엽니다. 실제 진행 확정은 기존 명령 제출 경로가 담당합니다.</summary>
        internal bool OpenForCombat(Action depart, Action cancelled)
        {
            if (depart == null) throw new ArgumentNullException(nameof(depart));
            if (cancelled == null) throw new ArgumentNullException(nameof(cancelled));
            if (!_lifetime.CanOpen || IsOpen) return false;
            if (_departureScreen == null)
                throw new InvalidOperationException("[DeckEditPopup] 출전 표시 계약이 필요합니다.");
            _depart = depart;
            _cancelDeparture = cancelled;
            _lifetime.Open();
            if (IsOpen) return true;
            _depart = null;
            _cancelDeparture = null;
            return false;
        }

        void IPopupViewController.Show()
        {
            if (!_lifetime.IsActive) return;
            _shown = true;
            _draft.Open(_deck.Read());
            if (_depart != null && _draft.Basis != null) _departureRun = _draft.Basis.RunId;
            EvaluateDraft();
            if (!_shown || !_lifetime.IsCurrent) return;
            _screen.BindInput(this);
            _departureScreen?.BindDeparture(this);
            Render();
        }

        void IPopupViewController.Hide(bool explicitClose)
        {
            _shown = false;
            Action cancelled = _cancelDeparture;
            _depart = null;
            _cancelDeparture = null;
            _draft.Clear();
            cancelled?.Invoke();
            if (!_lifetime.IsCurrent) return;
            _screen.BindInput(null);
            _departureScreen?.BindDeparture(null);
            _screen.Clear();
        }

        void IDeckEditInput.PlaceOwned(CharacterInstanceId ownedId, int slotIndex) => Act(() =>
        {
            DeckEditRequest request = _draft.CreatePlacementRequest(ownedId, slotIndex);
            if (request != null) _draft.ApplyEvaluation(_deck.Evaluate(request));
        }, false);
        void IDeckEditInput.RemoveSlot(int index) => Act(() => _draft.RemoveSlot(index), true);
        void IDeckEditInput.Reevaluate() => Act(RebaseDraft, true);
        void IDeckEditInput.Confirm() => Act(() => ConfirmDraft(), false);

        void IDeckEditInput.Close() => Guard(() =>
        {
            if (_shown && _lifetime.IsActive) _lifetime.Close(false);
        });

        public void Dispose()
        {
            if (_lifetime.IsDisposed) return;
            _shown = false;
            _depart = null;
            _cancelDeparture = null;
            // 공유 화면의 열기 요청·수신자는 수명 부품이 현재 연결일 때만 끊습니다.
            _lifetime.Dispose(_draft.Clear, () =>
            {
                _screen.BindOpenRequest(null);
                _screen.BindInput(null);
                _departureScreen?.BindDeparture(null);
            });
        }

        private void RequestOpen() => Guard(() =>
        {
            if (!_lifetime.CanOpen || !_canOpen()) return;
            _lifetime.Open();
        });

        private void Act(Action change, bool evaluate) => Guard(() =>
        {
            if (!_shown || !_lifetime.CanWork) return;
            change();
            if (evaluate) EvaluateDraft();
            if (_shown && _lifetime.CanWork) Render();
        });

        private void EvaluateDraft()
        {
            DeckEditRequest request = _draft.CreateRequest();
            if (request == null) return;
            _draft.ApplyEvaluation(_deck.Evaluate(request));
        }

        private void RebaseDraft()
        {
            DeckReadResult read = _deck.Read();
            if (_depart != null && read.Snapshot != null && read.Snapshot.RunId != _departureRun)
            {
                _lifetime.Close(false);
                return;
            }
            _draft.Rebase(read);
        }

        private bool ConfirmDraft()
        {
            if (!_draft.CanConfirm) return false;
            DeckEditRequest request = _draft.CreateRequest();
            DeckEditResult result = _deck.Confirm(request);
            if (!result.Accepted)
            {
                _draft.ApplyRejectedConfirm(result);
                return false;
            }
            // 확정은 이미 일어났습니다. 아래 다시 읽기가 실패하거나 StateChanged여도 Confirm을 다시 부르지 않습니다.
            _draft.Open(_deck.Read());
            EvaluateDraft();
            _draft.MarkCommitted();
            return _draft.State == DeckEditDraftState.Valid;
        }

        private void Render()
        {
            _screen.Render(DeckEditModelBuilder.Build(_draft));
            _departureScreen?.RenderDeparture(_depart != null, CanDepart);
        }

        private bool CanDepart => _shown && _lifetime.CanWork && !_submitting && _depart != null &&
            _draft.State == DeckEditDraftState.Valid && _draft.Availability?.CanEdit == true;

        void IDeckCombatInput.StartCombat() => Guard(() =>
        {
            if (!CanDepart || _departureScreen.IsDepartureConfirmationOpen) return;
            EvaluateDraft();
            if (!_shown || !_lifetime.CanWork) return;
            Render();
            if (!CanDepart) return;
            if (_draft.Candidates.Count < _departureScreen.DeckSlotCount) _ui.OpenUI(UIId.DeckCombatConfirm);
            else Depart(_draft.HasChanges);
        });

        void IDeckCombatInput.AcceptDeparture() => Guard(() =>
        {
            if (!CanDepart || !_departureScreen.IsDepartureConfirmationOpen) return;
            _ui.CloseUI(UIId.DeckCombatConfirm);
            Depart(_draft.HasChanges);
        });

        void IDeckCombatInput.CancelDepartureConfirmation() => Guard(() =>
        {
            if (_shown && _lifetime.CanWork) _ui.CloseUI(UIId.DeckCombatConfirm);
        });

        private void Depart(bool applyChanges)
        {
            _submitting = true;
            try
            {
                if (applyChanges && !ConfirmDraft())
                {
                    Render();
                    return;
                }
                Action depart = _depart;
                // 닫힘은 취소로 되돌리지 않습니다. 먼저 수신자를 끊고 기존 명령을 한 번 제출합니다.
                _depart = null;
                _cancelDeparture = null;
                _lifetime.CloseThen(false, depart);
            }
            finally
            {
                _submitting = false;
                if (_shown && _lifetime.CanWork) Render();
            }
        }

        private void Guard(Action action)
        {
            try
            {
                action();
            }
            catch (Exception error)
            {
                _lifetime.Report(error);
            }
        }
    }
}
