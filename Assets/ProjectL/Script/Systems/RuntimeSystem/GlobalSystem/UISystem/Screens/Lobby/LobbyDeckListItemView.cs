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
        [Tooltip("다음 Run 메인으로 고른 항목에만 켜는 표시입니다. 상세 고정과 따로 보입니다. 항목 루트가 아닌 자식이어야 합니다.")]
        [SerializeField] private GameObject selectedMark;

        private UIInputBinding _binding;

        internal bool CanInteract => actionButton != null && actionButton.IsActive() && actionButton.IsInteractable();

        internal void Validate()
        {
            if (title == null || icon == null || actionButton == null)
                throw new InvalidOperationException("[LobbyDeckListItemView] 이름·아이콘·입력 버튼 참조가 필요합니다.");
            if (!title.transform.IsChildOf(transform) || !icon.transform.IsChildOf(transform) ||
                (actionButton.transform != transform && !actionButton.transform.IsChildOf(transform)))
                throw new InvalidOperationException("[LobbyDeckListItemView] 표시와 입력 참조는 목록 항목 안에 있어야 합니다.");
            if (selectedMark == null)
                throw new InvalidOperationException("[LobbyDeckListItemView] 선택 표시(selectedMark) 참조가 필요합니다.");
            // 항목 루트를 끄면 항목 전체가 사라지므로 자식만 받습니다.
            if (selectedMark == gameObject || !selectedMark.transform.IsChildOf(transform))
                throw new InvalidOperationException("[LobbyDeckListItemView] 선택 표시는 목록 항목 루트가 아닌 자식이어야 합니다.");
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

        /// <summary>다음 Run 메인으로 고른 항목인지 표시합니다. 바뀔 때만 켜고 끕니다.</summary>
        internal void SetSelected(bool selected)
        {
            if (selectedMark.activeSelf != selected)
                selectedMark.SetActive(selected);
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
