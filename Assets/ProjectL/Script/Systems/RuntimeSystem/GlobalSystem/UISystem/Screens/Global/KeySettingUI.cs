using System;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.View;
using ProjectL.Systems.ServiceKeys.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Global
{
    /// <summary>키 변경 입력을 서비스에 요청하고 현재 키와 입력 안내를 표시합니다.</summary>
    public sealed class KeySettingUI : UIBase, IUIInputReceiver, IKeyBindingReceiver
    {
        private const string IdleGuide = "변경할 키를 클릭";

        [Serializable]
        private sealed class KeyButton
        {
            [SerializeField] private KeyBindingAction action;
            [SerializeField] private Button button;
            [SerializeField] private TMP_Text text;
            internal KeyBindingAction Action => action;
            internal Button Button => button;
            internal TMP_Text Text => text;
        }

        [SerializeField] private TMP_Text guide;
        [SerializeField] private KeyButton[] keyButtons = Array.Empty<KeyButton>();
        private IKeyBindingService _keys;
        private IOptionInputReceiver _closeReceiver;
        private int _pendingSlot = -1;
        private bool _buttonsBound;

        public override UIId ID => UIId.KeySetting;
        public override UIId ParentID => UIId.Setting;

        internal void BindCloseReceiver(IOptionInputReceiver receiver)
        {
            _closeReceiver = receiver ?? throw new ArgumentNullException(nameof(receiver));
        }

        public override void BindUISystemService(IUISystemService service)
        {
            base.BindUISystemService(service);
            Policy = UIOpenPolicy.Parallel;
        }

        public override void BindUIService(ISystemServiceStoreReader resolver)
        {
            if (resolver == null) throw new ArgumentNullException(nameof(resolver));
            CancelWaiting();
            _keys = resolver.GetService(SystemGroupsService.Global).InputServiceCommand.KeyBindings;
            BindButtons();
        }

        public override void GetUIData(IServiceModule query)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
        }

        private void BindButtons()
        {
            if (_buttonsBound) return;
            // 7 = 전투 다섯(시작·턴 종료·공격·되돌리기·스킬) + 아티팩트 회전 둘. 프리팹 줄 수와 한 커밋으로 바꿉니다(전투 스킬 View 계획 TP-16).
            if (guide == null || keyButtons.Length != 7)
                throw new InvalidOperationException("[KeySettingUI] 안내와 키 버튼 7개를 연결해야 합니다.");
            for (int i = 0; i < keyButtons.Length; i++)
            {
                KeyButton row = keyButtons[i];
                if (row == null || row.Button == null || row.Text == null || !System.Enum.IsDefined(typeof(KeyBindingAction), row.Action))
                    throw new InvalidOperationException("[KeySettingUI] 액션·Button·Text를 연결해야 합니다.");
                for (int j = 0; j < i; j++)
                    if (keyButtons[j].Action == row.Action || keyButtons[j].Button == row.Button)
                        throw new InvalidOperationException("[KeySettingUI] 키 버튼이 중복됩니다.");
                UIInputBinding.Attach(row.Button, this, i);
            }
            _buttonsBound = true;
        }

        public override void Open()
        {
            if (_keys == null) throw new InvalidOperationException("[KeySettingUI] 입력 서비스를 연결해야 합니다.");
            BindButtons();
            CancelWaiting();
            RefreshKeys();
            guide.text = IdleGuide;
            gameObject.SetActive(true);
        }

        private void RefreshKeys()
        {
            foreach (KeyButton row in keyButtons) row.Text.text = _keys.GetKeyText(row.Action);
        }

        void IUIInputReceiver.Click(int slot)
        {
            if (!isActiveAndEnabled || _keys == null || slot < 0 || slot >= keyButtons.Length) return;
            CancelWaiting();
            _keys.BeginCapture(keyButtons[slot].Action, this);
            _pendingSlot = slot;
            keyButtons[slot].Text.text = string.Empty;
            guide.text = "변경할 키를 누르세요.";
            // Submit으로 시작해도 방금 누른 키가 다른 버튼 동작까지 실행하지 않게 선택을 비웁니다.
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }

        void IKeyBindingReceiver.KeyBindingReceived(KeyBindingResult result)
        {
            if (_pendingSlot < 0) return;
            if (result == KeyBindingResult.CloseRequested)
            {
                CancelWaiting();
                _closeReceiver?.RequestClose();
                return;
            }
            if (result == KeyBindingResult.Forbidden) guide.text = "지정 불가한 키 입력입니다.";
            else if (result == KeyBindingResult.Duplicate) guide.text = "키를 중복 할당할 수 없습니다.";
            else
            {
                KeyButton row = keyButtons[_pendingSlot];
                row.Text.text = _keys.GetKeyText(row.Action);
                _pendingSlot = -1;
                guide.text = IdleGuide;
            }
        }

        internal void CancelWaiting()
        {
            if (_pendingSlot < 0) return;
            _keys.CancelCapture();
            KeyButton row = keyButtons[_pendingSlot];
            row.Text.text = _keys.GetKeyText(row.Action);
            _pendingSlot = -1;
            guide.text = IdleGuide;
        }

        public override void Hide()
        {
            CancelWaiting();
            gameObject.SetActive(false);
        }

        private void OnDisable() => CancelWaiting();
        void IUIInputReceiver.HoverEnter(int slot) { }
        void IUIInputReceiver.HoverExit(int slot) { }
    }
}
