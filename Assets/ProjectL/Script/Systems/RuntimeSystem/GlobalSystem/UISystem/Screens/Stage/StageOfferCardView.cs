using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage
{
    /// <summary>전달받은 카드 표시값을 그리고 입력 슬롯을 연결하는 재사용 표시 부품입니다.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class StageOfferCardView : UIWidgetBase
    {
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text primaryText;
        [Tooltip("추가 설명을 표시하는 카드에서만 연결합니다. 설명 영역이 없는 이벤트 카드는 비워 둡니다.")]
        [SerializeField] private TMP_Text secondaryText;
        [Tooltip("설명 영역에 스크롤을 사용하는 카드에서 연결합니다. Content는 secondaryText, Viewport는 그 부모입니다.")]
        [SerializeField] private UnityEngine.UI.ScrollRect descriptionScroll;
        [Tooltip("유닛 상품의 상단 오른쪽 레벨입니다. 유닛 상품을 그리는 카드에 연결합니다.")]
        [SerializeField] private TMP_Text unitLevelText;
        [Tooltip("유닛 상품의 초상화 아래 가운데 보유 수입니다. 유닛 상품을 그리는 카드에 연결합니다.")]
        [SerializeField] private TMP_Text unitOwnedText;
        [SerializeField] private TMP_Text buttonText;
        [SerializeField] private Button actionButton;
        [Tooltip("버튼 외에 완료 표시가 필요한 카드에서만 연결합니다.")]
        [SerializeField] private GameObject completedMarker;
        [Tooltip("증강 종류(신규 특성·특성 강화) 글입니다. 값이 비면 꺼집니다.")]
        [SerializeField] private TMP_Text kindText;
        [Tooltip("가격 옆 그림입니다. 가격이 비면 가격 글과 함께 꺼집니다.")]
        [SerializeField] private GameObject priceIcon;

        private UIInputBinding _binding;
        private bool _canSubmit;

        internal bool IsButtonEnabled { get; private set; }
        internal Button ActionButton => actionButton;
        internal void Bind(IUIInputReceiver receiver, int slot)
        {
            _binding = UIInputBinding.Attach(actionButton, receiver, slot);
        }

        internal void Validate()
        {
            if (icon == null || titleText == null || primaryText == null || secondaryText == null ||
                buttonText == null || actionButton == null || kindText == null || priceIcon == null)
                throw new InvalidOperationException("[StageOfferCardView] Icon, texts, kind text, price icon and action button are required.");
            if (descriptionScroll != null &&
                (descriptionScroll.content != secondaryText.rectTransform || descriptionScroll.viewport == null ||
                 secondaryText.rectTransform.parent != descriptionScroll.viewport ||
                 descriptionScroll.horizontal || !descriptionScroll.vertical ||
                 descriptionScroll.movementType != UnityEngine.UI.ScrollRect.MovementType.Clamped))
                throw new InvalidOperationException($"[StageOfferCardView] '{name}'의 descriptionScroll에는 설명 Content와 부모 Viewport, Clamped 세로 스크롤 설정이 필요합니다.");
        }

        public void Render(StageOfferCardContent content)
        {
            if (content.UnitLevelText.Length > 0 && unitLevelText == null)
                throw new InvalidOperationException($"[StageOfferCardView] '{name}'에 유닛 레벨을 그릴 {nameof(unitLevelText)} 참조가 필요합니다.");
            if (content.UnitOwnedText.Length > 0 && unitOwnedText == null)
                throw new InvalidOperationException($"[StageOfferCardView] '{name}'에 보유 수를 그릴 {nameof(unitOwnedText)} 참조가 필요합니다.");
            IsButtonEnabled = content.IsButtonEnabled;
            if (icon != null)
            {
                icon.sprite = content.Icon;
                icon.enabled = content.Icon != null;
            }
            SetText(titleText, content.Title);
            SetText(primaryText, content.PrimaryText);
            RenderDescription(content.SecondaryText);
            SetOptionalText(unitLevelText, content.UnitLevelText);
            SetOptionalText(unitOwnedText, content.UnitOwnedText);
            // 값이 없는 칸은 끕니다. 증강은 가격이 없고, 상점·회복은 종류 글이 없습니다.
            bool hasPrice = content.PrimaryText.Length > 0;
            primaryText.gameObject.SetActive(hasPrice);
            priceIcon.SetActive(hasPrice);
            SetText(kindText, content.KindText);
            kindText.gameObject.SetActive(content.KindText.Length > 0);
            SetText(buttonText, content.ButtonText);
            if (completedMarker != null) completedMarker.SetActive(content.IsCompleted);
            RefreshInteractability();
        }

        public void SetCanSubmit(bool canSubmit)
        {
            _canSubmit = canSubmit;
            RefreshInteractability();
        }

        private void RenderDescription(string value)
        {
            SetText(secondaryText, value);
            if (descriptionScroll == null) return;

            // 위쪽에 고정한 Content를 처음으로 돌립니다. 글 높이는 ContentSizeFitter가 계산하고,
            // Viewport보다 짧은 글은 ScrollRect의 Clamped 설정으로 움직이지 않습니다.
            descriptionScroll.StopMovement();
            descriptionScroll.content.anchoredPosition = Vector2.zero;
            descriptionScroll.gameObject.SetActive(value.Length > 0);
        }

        private void RefreshInteractability()
        {
            bool enabled = _canSubmit && IsButtonEnabled;
            if (actionButton != null) actionButton.interactable = enabled;
            if (!enabled) _binding?.OnHoverExit();
        }

        private static void SetText(TMP_Text text, string value)
        {
            if (text != null) text.text = value ?? string.Empty;
        }

        // 유닛 상품 외에는 쓰지 않는 칸이며, 다른 종류의 카드로 다시 그릴 때 함께 숨깁니다.
        private static void SetOptionalText(TMP_Text text, string value)
        {
            if (text == null) return;
            text.text = value;
            text.gameObject.SetActive(value.Length > 0);
        }
    }
}
