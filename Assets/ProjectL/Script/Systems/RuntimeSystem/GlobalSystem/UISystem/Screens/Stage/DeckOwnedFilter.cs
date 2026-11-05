using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage
{
    /// <summary>보유 목록의 표시 필터와 버튼 외형만 소유합니다. 편성 규칙은 변경하지 않습니다.</summary>
    [Serializable]
    internal sealed class DeckOwnedFilter
    {
        internal const int Count = 4;

        [Tooltip("OwnedLabel 아래 All·Heavy·Rider·Cannon 순서의 버튼입니다.")]
        [SerializeField] private Button[] buttons;
        [SerializeField] private Sprite selectedSprite;
        [SerializeField] private Sprite unselectedSprite;

        private int _selectedIndex;

        internal void Validate(Transform panel)
        {
            if (buttons == null || buttons.Length != Count || selectedSprite == null || unselectedSprite == null)
                throw new InvalidOperationException("[DeckEditUI] UIId.DeckEdit: ownedFilter에 All·Heavy·Rider·Cannon 버튼과 선택·미선택 Sprite가 필요합니다.");
            for (int i = 0; i < buttons.Length; i++)
            {
                Button button = buttons[i];
                if (button == null || !button.transform.IsChildOf(panel) || button.image == null ||
                    button.targetGraphic != button.image || button.image.gameObject != button.gameObject)
                    throw new InvalidOperationException($"[DeckEditUI] UIId.DeckEdit: ownedFilter.buttons[{i}]는 Panel 내부의 Image를 가진 Button이어야 합니다.");
                if (i > 0 && button.transform.parent != buttons[0].transform.parent)
                    throw new InvalidOperationException($"[DeckEditUI] UIId.DeckEdit: ownedFilter.buttons[{i}]는 All과 같은 OwnedLabel 아래에 있어야 합니다.");
                for (int previous = 0; previous < i; previous++)
                    if (buttons[previous] == button)
                        throw new InvalidOperationException($"[DeckEditUI] UIId.DeckEdit: ownedFilter.buttons[{previous}]와 [{i}]가 같은 버튼입니다.");
            }
        }

        internal void Bind(IUIInputReceiver receiver, int firstSlot)
        {
            for (int i = 0; i < buttons.Length; i++)
                UIInputBinding.Attach(buttons[i], receiver, firstSlot - i);
            Reset();
        }

        internal void Reset()
        {
            _selectedIndex = 0;
            RefreshButtons();
        }

        internal bool Select(int index)
        {
            if (index < 0 || index >= Count) throw new ArgumentOutOfRangeException(nameof(index));
            if (_selectedIndex == index) return false;
            _selectedIndex = index;
            RefreshButtons();
            return true;
        }

        internal bool Includes(CategoryKey category) => _selectedIndex switch
        {
            0 => true,
            1 => category == CategoryKey.Heavy,
            2 => category == CategoryKey.Rider,
            3 => category == CategoryKey.Cannon,
            _ => throw new InvalidOperationException("[DeckEditUI] UIId.DeckEdit: 보유 목록의 필터 상태가 잘못되었습니다.")
        };

        private void RefreshButtons()
        {
            for (int i = 0; i < buttons.Length; i++)
                buttons[i].image.sprite = i == _selectedIndex ? selectedSprite : unselectedSprite;
        }
    }
}
