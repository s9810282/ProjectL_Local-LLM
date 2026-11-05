using System;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.LobbyVisual.Item;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.LobbyInput;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Lobby
{
    /// <summary>
    /// 같은 시설의 누름·놓음과 이동 완료 후 한 번의 실행 의도, 지금 누르면 고를 시설의 호버 강조를 소유합니다.
    /// 말을 옮기는 일(도착 자세·이동 시작·소리)은 <see cref="LobbyPieceMover"/>가 맡습니다(로비 리팩토링 2차 설계 N-55).
    /// 막힘(시설 팝업·출발 처리·끝난 연결)은 저장하지 않고 쓰는 순간 Scope 판정에 묻습니다(검사 후속 정리 설계 §12.2 B4).
    /// </summary>
    internal sealed class LobbyInteraction : ILobbyInputReceiver, IDisposable
    {
        private LobbyFacilityPicker _picker;
        private ScreenUIPointerBlocker _blocker;
        private LobbyPieceMover _mover;
        private ILobbyFacilityActions _actions;
        private Func<bool> _isBlocked;
        private LobbyFacilityHighlight _highlight;
        private Func<bool> _isLobbyLayer;
        private LobbyFacilityView _pressed;
        private LobbyFacilityView _destination;
        private bool _hasDestination;
        private bool _disposed;
        private int _generation;
        private float _pointerX;
        private float _pointerY;
        private bool _hasPointer;
        // 호버는 포인터가 움직였거나 고를 수 있는지가 바뀐 Tick에만 다시 고릅니다.
        private bool _pointerMoved;
        private bool _couldHover;

        /// <param name="isBlocked">시설 고르기·도착 실행을 막는지 묻는 판정입니다.</param>
        /// <param name="highlight">호버한 시설의 안내 글자를 빛나게 하는 부품입니다. 이 입력이 정리합니다.</param>
        /// <param name="isLobbyLayer">지금 입력 Layer가 로비인지입니다. 설정 화면 등이 Layer를 가져가면 호버를 끕니다.</param>
        internal LobbyInteraction(LobbyFacilityPicker picker, ScreenUIPointerBlocker blocker,
            LobbyPieceMover mover, ILobbyFacilityActions actions, Func<bool> isBlocked,
            LobbyFacilityHighlight highlight, Func<bool> isLobbyLayer)
        {
            _picker = picker ?? throw new ArgumentNullException(nameof(picker));
            _blocker = blocker ?? throw new ArgumentNullException(nameof(blocker));
            _mover = mover ?? throw new ArgumentNullException(nameof(mover));
            _actions = actions ?? throw new ArgumentNullException(nameof(actions));
            _isBlocked = isBlocked ?? throw new ArgumentNullException(nameof(isBlocked));
            _highlight = highlight ?? throw new ArgumentNullException(nameof(highlight));
            _isLobbyLayer = isLobbyLayer ?? throw new ArgumentNullException(nameof(isLobbyLayer));
        }

        public void OnPointer(float x, float y)
        {
            if (_disposed) return;
            _pointerX = x;
            _pointerY = y;
            _hasPointer = true;
            _pointerMoved = true;
        }

        public void OnSelectPressed()
        {
            _pressed = null;
            if (!CanSelect() || _blocker.IsBlocked(_pointerX, _pointerY)) return;
            if (_picker.TryPick(_pointerX, _pointerY, out LobbyFacilityView facility))
                _pressed = facility;
        }

        public void OnSelectReleased()
        {
            LobbyFacilityView pressed = _pressed;
            _pressed = null;
            if (pressed == null || !CanSelect() || _blocker.IsBlocked(_pointerX, _pointerY) ||
                !_picker.TryPick(_pointerX, _pointerY, out LobbyFacilityView released) || released != pressed) return;

            int generation = ++_generation;
            _destination = released;
            _hasDestination = true;
            try
            {
                _mover.MoveTo(released.Kind);
            }
            catch
            {
                if (_generation == generation) Cancel();
                throw;
            }
        }

        public void OnSelectCanceled() => _pressed = null;

        internal void Tick()
        {
            if (_disposed) return;
            UpdateHover();
            if (!_hasDestination) return;
            // 가는 도중에 막히면(시설 팝업·출발 처리) 그 이동을 버립니다. 막힌 동안은 새 고르기도 시작하지 않습니다.
            if (_isBlocked())
            {
                Cancel();
                return;
            }
            // 설정창 등이 입력 Layer를 가져간 동안에는 도착 완료를 소비하지 않고 기다립니다. Layer가 돌아오면 그 Tick에 실행합니다
            // (로비 리팩토링 2차 설계 M-02, Q-R2-1 A).
            if (!_isLobbyLayer()) return;
            if (!_mover.TryConsumeCompletion()) return;
            LobbyFacilityView destination = _destination;
            ILobbyFacilityActions actions = _actions;
            // 외부 실행 콜백보다 먼저 의도를 지워 재진입 Tick이 같은 완료를 실행하지 않습니다.
            _hasDestination = false;
            _destination = null;
            if (destination == null || !destination.isActiveAndEnabled) return;
            // 막힘·로비 Layer·팝업 열 수 있음은 실행 표의 판정 한 번이 봅니다. 앞의 막힘 검사 뒤에 콜백이 없어 다시 보지 않습니다(N-24).
            actions.TryExecute(destination.Kind);
        }

        internal void Cancel()
        {
            ++_generation;
            _pressed = null;
            _destination = null;
            _hasDestination = false;
            _mover?.Cancel();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            try
            {
                Cancel();
            }
            finally
            {
                try
                {
                    _mover?.Dispose();
                }
                finally
                {
                    _highlight?.Dispose();
                    _highlight = null;
                    _isLobbyLayer = null;
                    _mover = null;
                    _picker = null;
                    _blocker = null;
                    _actions = null;
                    _isBlocked = null;
                }
            }
        }

        /// <summary>
        /// 지금 누르면 고를 시설만 빛납니다. 이동 중·막힘·로비 밖 Layer·UI 위면 끕니다(로비 설계 §4.2·§16).
        /// 다시 고르는 것은 포인터가 움직였거나 고를 수 있는지가 바뀐 때뿐입니다.
        /// </summary>
        private void UpdateHover()
        {
            bool canHover = CanSelect() && _isLobbyLayer();
            if (canHover == _couldHover && !_pointerMoved) return;
            _couldHover = canHover;
            _pointerMoved = false;
            LobbyFacilityView hovered = null;
            if (canHover && !_blocker.IsBlocked(_pointerX, _pointerY))
                _picker.TryPick(_pointerX, _pointerY, out hovered);
            _highlight.Show(hovered);
        }

        private bool CanSelect() => !_disposed && _hasPointer && !_hasDestination && !_mover.IsMoving && !_isBlocked();
    }
}
