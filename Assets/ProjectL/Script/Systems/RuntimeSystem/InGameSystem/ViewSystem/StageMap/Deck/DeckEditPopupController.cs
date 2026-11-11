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
    internal sealed class DeckEditPopupController : IPopupViewController, IDeckEditInput, IDeckCombatInput, IStageMapViewPopup
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
        private readonly StageMapViewNotice _notice;
        // 마지막으로 띄운 상태 안내입니다. 같은 이유를 그릴 때마다 다시 띄우지 않습니다. 정상 상태가 되면 비웁니다.
        private string _lastNotice;

        /// <param name="canOpen">다른 팝업·노드 이동처럼 열기를 막는 맵 쪽 조건입니다. 편집 가능 여부는 Core가 판단합니다.</param>
        /// <param name="departureScreen">출전 버튼·확인창 표시 계약입니다. 출전은 늘 이 화면으로 하므로 필수입니다.</param>
        internal DeckEditPopupController(IRunDeckService deck, IUISystemService ui, IDeckEditDisplay screen,
            IDeckCombatDisplay departureScreen, PopupInputSession input, IPopupViewHost host, Func<bool> canOpen,
            RuntimeFaultBoundary faults, StageMapViewNotice notice)
        {
            _deck = deck ?? throw new ArgumentNullException(nameof(deck));
            _screen = screen ?? throw new ArgumentNullException(nameof(screen));
            _canOpen = canOpen ?? throw new ArgumentNullException(nameof(canOpen));
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            _departureScreen = departureScreen ?? throw new ArgumentNullException(nameof(departureScreen));
            _notice = notice ?? throw new ArgumentNullException(nameof(notice));
            _lifetime = new StagePopupViewLifetime("DeckEditPopup", ui, new DeckEditPopupViewScreen(screen), input,
                this, host, faults);
        }

        internal bool IsOpen => _lifetime.IsOpen;
        bool IStageMapViewPopup.IsOpen => IsOpen;
        // 덱 편집은 Core 결과를 기다리지 않습니다(편집 평가 응답은 열린 동안의 일입니다).
        bool IStageMapViewPopup.IsWaiting => false;
        void IStageMapViewPopup.Start() => Start();
        void IStageMapViewPopup.Tick() => Tick();

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
            _lastNotice = null;
            _draft.Open(_deck.Read());
            // 출전 창이 아니거나 기준 덱이 없으면 지난 출전의 Run을 남기지 않습니다(RebaseDraft가 늘 이 값과 견줌).
            _departureRun = _depart != null && _draft.Basis != null ? _draft.Basis.RunId : default;
            EvaluateDraft();
            if (!_shown || !_lifetime.IsCurrent) return;
            _screen.BindInput(this);
            _departureScreen.BindDeparture(this);
            Render();
        }

        void IPopupViewController.Hide(bool explicitClose)
        {
            _shown = false;
            _lastNotice = null;
            Action cancelled = _cancelDeparture;
            _depart = null;
            _cancelDeparture = null;
            _draft.Clear();
            cancelled?.Invoke();
            if (!_lifetime.IsCurrent) return;
            _screen.BindInput(null);
            _departureScreen.BindDeparture(null);
            _screen.Clear();
        }

        void IDeckEditInput.PlaceOwned(CharacterInstanceId ownedId, int slotIndex) => Act(() =>
        {
            DeckEditRequest request = _draft.CreatePlacementRequest(ownedId, slotIndex);
            if (request == null) return;
            _draft.ApplyPlacementEvaluation(_deck.Evaluate(request));
            // 거절된 놓기는 덱 상태를 바꾸지 않고 이유만 한 번 알립니다(평가 C-08).
            DeckFailureReason rejected = _draft.TakePlacementRejection();
            if (rejected != DeckFailureReason.None) _notice.Show(DeckEditText.Failure(rejected));
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
                _departureScreen.BindDeparture(null);
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
            DeckEditDisplayModel model = DeckEditModelBuilder.Build(_draft);
            _screen.Render(model);
            _departureScreen.RenderDeparture(_depart != null, CanDepart);
            ShowStateNotice(model.FitsLayout ? DeckEditText.Notice(_draft) : DeckEditText.LayoutMismatch);
        }

        /// <summary>상태 안내는 이유가 바뀔 때만 한 번 띄웁니다. 정상 상태가 되면 기억을 비워 다음 같은 이유를 다시 알립니다.</summary>
        private void ShowStateNotice(string text)
        {
            if (text == null)
            {
                _lastNotice = null;
                return;
            }
            if (text == _lastNotice || !_shown || !_lifetime.CanWork) return;
            _lastNotice = text;
            _notice.Show(text);
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
