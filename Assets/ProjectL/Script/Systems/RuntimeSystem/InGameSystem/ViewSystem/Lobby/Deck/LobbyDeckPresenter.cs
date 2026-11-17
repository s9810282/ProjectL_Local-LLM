using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Lobby;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Lobby.Deck
{
    /// <summary>
    /// 메인 후보 카드의 호버·상세 고정과 "다음 Run 메인" 선택 버튼을 관리합니다. 카드 클릭은 상세 고정만 하고 선택을 바꾸지 않으며,
    /// 선택은 버튼으로만 바꿉니다(로비 메인 선택 설계 LM3). 선택 값의 원본은 <see cref="ILobbyMainSelectionWriter"/>이고 여기는 보관하지 않습니다.
    /// </summary>
    internal sealed class LobbyDeckPresenter : IUIInputReceiver
    {
        private readonly ILobbyDeckDisplay _view;
        private readonly ILobbyMainSelectionWriter _selection;
        // 후보 카드 목록 모델입니다. 연결마다 한 번 만들어 팝업을 열 때마다 다시 만들지 않습니다.
        private readonly LobbyDeckDisplayModel _candidates;
        private readonly Action _requestClose;
        private LobbyDeckDisplayModel _model;
        private int _hoveredSlot = -1;
        private int _pinnedSlot = -1;
        private int _version;
        private bool _inputEnabled;

        /// <param name="cards">후보 카드입니다. 선택 후보와 같은 순서·같은 키여야 합니다.</param>
        internal LobbyDeckPresenter(ILobbyDeckDisplay view, ILobbyMainSelectionWriter selection,
            IReadOnlyList<LobbyDeckCardContent> cards, Action requestClose)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _selection = selection ?? throw new ArgumentNullException(nameof(selection));
            if (cards == null) throw new ArgumentNullException(nameof(cards));
            _requestClose = requestClose ?? throw new ArgumentNullException(nameof(requestClose));
            IReadOnlyList<EntityKey> candidates = selection.Candidates;
            if (cards.Count != candidates.Count)
                throw new InvalidOperationException(
                    $"[Lobby] 메인 후보 카드 수({cards.Count})가 후보 수({candidates.Count})와 다릅니다.");
            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i] == null || cards[i].Key != candidates[i])
                    throw new InvalidOperationException($"[Lobby] 메인 후보 카드 {i}번이 후보 {candidates[i]}와 다릅니다.");
            }
            _candidates = new LobbyDeckDisplayModel(cards);
        }

        private bool CanInput => _inputEnabled && _model != null && _view.IsShown;

        /// <summary>
        /// 선택 버튼을 눌러도 되는지의 유일한 판정입니다. 버튼을 그릴 때와 선택 클릭을 받은 순간 둘 다 이것을 봅니다.
        /// 입력 가능 ∧ 고정한 카드 있음 ∧ 그 카드가 지금 선택과 다름 ∧ 출발이 붙잡고 있지 않음입니다.
        /// </summary>
        private bool CanSelect => CanInput && IsCardSlot(_pinnedSlot) &&
            _model.Cards[_pinnedSlot].Key != _selection.Selected && !_selection.IsLocked;

        /// <summary>후보 카드를 보이고 지금 선택을 표시합니다. 팝업을 열 때마다 부르며, 선택 값은 다시 만들지 않고 읽기만 합니다.</summary>
        internal void ShowCandidates()
        {
            int version = _version + 1;
            SetModel(_candidates);
            // 목록을 바꾸는 동안 다른 호출이 들어왔으면 그 호출의 상태를 덮지 않습니다.
            if (version != _version)
                return;
            _view.ShowSelected(SlotOf(_selection.Selected));
            RefreshSelect();
        }

        private void SetModel(LobbyDeckDisplayModel model)
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
            // 입력을 켜거나 끌 때마다 선택 버튼을 다시 판정합니다(끄면 늘 거짓).
            RefreshSelect();
        }

        internal void Clear()
        {
            ++_version;
            _inputEnabled = false;
            _model = null;
            _hoveredSlot = -1;
            _pinnedSlot = -1;
            _view.ShowDetails(null);
            _view.SetSelectEnabled(false);
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
            if (slot == LobbyDeckUI.SelectSlot)
            {
                SelectPinned();
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
            RefreshSelect();
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

        /// <summary>고정한 카드를 다음 Run 메인으로 고릅니다. 버튼이 켜져 있었더라도 클릭 순간 판정을 다시 봅니다.</summary>
        private void SelectPinned()
        {
            if (!CanSelect)
            {
                RefreshSelect();
                return;
            }
            int slot = _pinnedSlot;
            _selection.Select(_model.Cards[slot].Key);
            _view.ShowSelected(slot);
            RefreshSelect();
        }

        private void RefreshSelect() => _view.SetSelectEnabled(CanSelect);

        /// <summary>지금 목록에서 키의 칸입니다. 선택은 늘 후보 안이라 후보 목록에서 못 찾으면 구성 오류입니다.</summary>
        private int SlotOf(EntityKey key)
        {
            IReadOnlyList<LobbyDeckCardContent> cards = _model.Cards;
            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i].Key == key) return i;
            }
            throw new InvalidOperationException($"[Lobby] 다음 Run 메인({key})이 후보 카드에 없습니다.");
        }
    }
}
