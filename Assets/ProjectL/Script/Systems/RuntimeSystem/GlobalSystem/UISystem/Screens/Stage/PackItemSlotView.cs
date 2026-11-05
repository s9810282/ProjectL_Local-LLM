using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage
{
    /// <summary>
    /// 가방 칸 하나의 표시입니다(가방 설계 P2). 아이콘과 수량만 그리며 재고·장착을 소유하지 않습니다.
    /// </summary>
    /// <remarks><c>Item.prefab</c>에 붙입니다. 아이콘이 없는 아이템은 칸 프리팹의 처음 그림을 씁니다.</remarks>
    [DisallowMultipleComponent]
    public sealed class PackItemSlotView : UIWidgetBase
    {
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text countText;
        [SerializeField] private Button button;

        private Sprite _defaultIcon;
        private bool _hasDefaultIcon;

        /// <summary>지금 보이는 아이템 키입니다. 숨긴 칸은 0입니다.</summary>
        internal int ItemKey { get; private set; }

        internal Button Button => button;

        internal void Show(int itemKey, Sprite sprite, int count)
        {
            ValidateReferences();
            if (!_hasDefaultIcon)
            {
                _defaultIcon = icon.sprite;
                _hasDefaultIcon = true;
            }
            ItemKey = itemKey;
            icon.sprite = sprite != null ? sprite : _defaultIcon;
            countText.text = "×" + count;
            Open();
        }

        public override void Hide()
        {
            ItemKey = 0;
            base.Hide();
        }

        internal void ValidateReferences()
        {
            if (icon == null || countText == null || button == null)
                throw new InvalidOperationException($"[PackItemSlotView] {name}의 icon, countText, button 참조가 필요합니다.");
        }
    }
}
