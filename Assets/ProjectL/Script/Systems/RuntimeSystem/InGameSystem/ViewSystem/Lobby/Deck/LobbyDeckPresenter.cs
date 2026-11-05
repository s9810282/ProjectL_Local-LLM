using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Lobby;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Lobby.Deck
{
    /// <summary>표시 모델의 카드 호버와 고정 슬롯을 관리합니다. 선택은 덱 확정이 아닙니다.</summary>
    internal sealed class LobbyDeckPresenter : IUIInputReceiver
    {
        private readonly ILobbyDeckDisplay _view;
        private readonly Action _requestClose;
        private LobbyDeckDisplayModel _model;
        private int _hoveredSlot = -1;
        private int _pinnedSlot = -1;
        private int _version;
        private bool _inputEnabled;

        internal LobbyDeckPresenter(ILobbyDeckDisplay view, Action requestClose)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _requestClose = requestClose ?? throw new ArgumentNullException(nameof(requestClose));
        }

        private bool CanInput => _inputEnabled && _model != null && _view.IsShown;

        internal void SetModel(LobbyDeckDisplayModel model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));
            int version = ++_version;
            bool restoreInput = CanInput;
            _inputEnabled = false;
            _hoveredSlot = -1;
            _pinnedSlot = -1;
            _model = model;
            _view.ShowDetails(null);
            _view.SetModel(model);
            if (version == _version && _view.IsShown)
                _inputEnabled = restoreInput;
        }

        internal void SetInputEnabled(bool enabled)
        {
            ++_version;
            _inputEnabled = enabled && _view.IsShown;
            if (!_inputEnabled)
            {
                _hoveredSlot = -1;
                _pinnedSlot = -1;
                _view.ShowDetails(null);
            }
        }

        internal void Clear()
        {
            ++_version;
            _inputEnabled = false;
            _model = null;
            _hoveredSlot = -1;
            _pinnedSlot = -1;
            _view.ShowDetails(null);
        }

        public void Click(int slot)
        {
            if (!CanInput)
                return;
            if (slot == LobbyDeckUI.CloseSlot)
            {
                SetInputEnabled(false);
                _requestClose();
                return;
            }
            if (!IsCardSlot(slot))
                return;
            if (_pinnedSlot == slot)
            {
                _pinnedSlot = -1;
                _hoveredSlot = -1;
                _view.ShowDetails(null);
            }
            else
            {
                _pinnedSlot = slot;
                _view.ShowDetails(_model.Cards[slot]);
            }
        }

        public void HoverEnter(int slot)
        {
            if (!CanInput || !IsCardSlot(slot))
                return;
            _hoveredSlot = slot;
            if (_pinnedSlot < 0)
                _view.ShowDetails(_model.Cards[slot]);
        }

        public void HoverExit(int slot)
        {
            if (!CanInput || !IsCardSlot(slot) || _hoveredSlot != slot)
                return;
            _hoveredSlot = -1;
            if (_pinnedSlot < 0)
                _view.ShowDetails(null);
        }

        private bool IsCardSlot(int slot) => _model != null && slot >= 0 && slot < _model.Cards.Count;
    }
}
