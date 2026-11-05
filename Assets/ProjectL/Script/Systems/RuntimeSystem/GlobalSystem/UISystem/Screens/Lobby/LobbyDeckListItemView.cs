using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Lobby
{
    /// <summary>로비 덱 목록 항목의 표현과 직접 입력 바인딩입니다.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class LobbyDeckListItemView : MonoBehaviour
    {
        [SerializeField] private TMP_Text title;
        [SerializeField] private Image icon;
        [SerializeField] private Button actionButton;

        private UIInputBinding _binding;

        internal bool CanInteract => actionButton != null && actionButton.IsActive() && actionButton.IsInteractable();

        internal void Validate()
        {
            if (title == null || icon == null || actionButton == null)
                throw new InvalidOperationException("[LobbyDeckListItemView] 이름·아이콘·입력 버튼 참조가 필요합니다.");
            if (!title.transform.IsChildOf(transform) || !icon.transform.IsChildOf(transform) ||
                (actionButton.transform != transform && !actionButton.transform.IsChildOf(transform)))
                throw new InvalidOperationException("[LobbyDeckListItemView] 표시와 입력 참조는 목록 항목 안에 있어야 합니다.");
        }

        internal void Bind(IUIInputReceiver receiver, int slot)
        {
            _binding = UIInputBinding.Attach(actionButton, receiver, slot);
        }

        internal void SetInputEnabled(bool enabled)
        {
            if (actionButton != null)
                actionButton.interactable = enabled;
            if (!enabled)
                _binding?.OnHoverExit();
        }

        internal void Render(LobbyDeckCardContent content)
        {
            if (content == null)
                throw new ArgumentNullException(nameof(content));
            // 구성은 목록 원본을 씬 연결 때 한 번 검사했고 복제본은 같은 구성입니다(N-22).
            string name = string.IsNullOrWhiteSpace(content.DisplayName) ? content.Key.ToString() : content.DisplayName;
            if (title.text != name)
                title.text = name;
            if (icon.sprite != content.Icon)
                icon.sprite = content.Icon;
            icon.enabled = content.Icon != null;
        }
    }
}
