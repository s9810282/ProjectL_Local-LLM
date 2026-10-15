using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display
{
    /// <summary>
    /// Combat 씬 Canvas의 포기 버튼과 확인창입니다(포기 UI 설계 C1, 계획 4-V).
    /// 받은 값만 그리고, 누를 수 있는지·포기를 받아들일지는 판단하지 않습니다.
    /// </summary>
    /// <remarks>
    /// 판단은 CombatAbandonController(CombatInputPolicy)가, 포기 순서는 골격이 맡습니다. 같은 값이면 다시 쓰지 않습니다.
    /// 확인창 문구는 씬에서 작성합니다. 확인창 오브젝트는 기본 비활성으로 둡니다.
    /// 버튼 입력은 <see cref="UIInputBinding"/>이 이 패널(<see cref="IUIInputReceiver"/>)에 슬롯으로 전달합니다(Assets/AGENTS.md, 개선 설계 D1).
    /// <c>Button.onClick</c>에는 아무것도 연결하지 않습니다.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class CombatAbandonPanel : MonoBehaviour, IUIInputReceiver
    {
        private const int OpenSlot = 0;
        private const int ConfirmSlot = 1;
        private const int CancelSlot = 2;

        [Tooltip("포기 확인창을 여는 버튼입니다.")]
        [SerializeField] private Button abandonButton;
        [Tooltip("포기 확인창 루트입니다. 기본 비활성으로 둡니다.")]
        [SerializeField] private GameObject confirmDialog;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;

        private Action _open;
        private Action _confirm;
        private Action _cancel;
        private bool _isInputAttached;

        /// <summary>
        /// 세 버튼의 입구를 정합니다. 이전 입구는 먼저 버립니다. 모두 null이면 해제만 합니다.
        /// 버튼과 이 패널의 입력 연결(<see cref="UIInputBinding"/>)은 처음 연결할 때 한 번만 만듭니다.
        /// </summary>
        internal void Bind(Action open, Action confirm, Action cancel)
        {
            Unbind();
            if (open == null && confirm == null && cancel == null)
                return;
            if (open == null || confirm == null || cancel == null)
                throw new ArgumentException("[CombatAbandonPanel] 세 입구는 함께 연결하거나 함께 해제해야 합니다.");

            AttachInputOnce();
            _open = open;
            _confirm = confirm;
            _cancel = cancel;
        }

        void IUIInputReceiver.Click(int slot)
        {
            switch (slot)
            {
                case OpenSlot:
                    _open?.Invoke();
                    break;
                case ConfirmSlot:
                    _confirm?.Invoke();
                    break;
                case CancelSlot:
                    _cancel?.Invoke();
                    break;
            }
        }

        void IUIInputReceiver.HoverEnter(int slot)
        {
        }

        void IUIInputReceiver.HoverExit(int slot)
        {
        }

        /// <summary>버튼을 보일지(<paramref name="visible"/>)와 누를 수 있을지(<paramref name="interactable"/>)를 적용합니다.</summary>
        internal void SetAvailable(bool visible, bool interactable)
        {
            if (abandonButton.gameObject.activeSelf != visible)
                abandonButton.gameObject.SetActive(visible);
            if (abandonButton.interactable != interactable)
                abandonButton.interactable = interactable;
        }

        internal void ShowConfirm()
        {
            if (!confirmDialog.activeSelf)
                confirmDialog.SetActive(true);
        }

        internal void HideConfirm()
        {
            if (confirmDialog != null && confirmDialog.activeSelf)
                confirmDialog.SetActive(false);
        }

        internal string FindMissingReference()
        {
            if (abandonButton == null)
                return $"{nameof(CombatAbandonPanel)}.{nameof(abandonButton)}";
            if (confirmDialog == null)
                return $"{nameof(CombatAbandonPanel)}.{nameof(confirmDialog)}";
            if (confirmButton == null)
                return $"{nameof(CombatAbandonPanel)}.{nameof(confirmButton)}";
            return cancelButton == null ? $"{nameof(CombatAbandonPanel)}.{nameof(cancelButton)}" : null;
        }

        private void Unbind()
        {
            _open = null;
            _confirm = null;
            _cancel = null;
        }

        private void AttachInputOnce()
        {
            if (_isInputAttached)
                return;

            UIInputBinding.Attach(abandonButton, this, OpenSlot);
            UIInputBinding.Attach(confirmButton, this, ConfirmSlot);
            UIInputBinding.Attach(cancelButton, this, CancelSlot);
            _isInputAttached = true;
        }
    }
}
