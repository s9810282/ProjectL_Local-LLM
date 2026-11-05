using System;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.ServiceKeys.Runtime;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.AudioSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Global;
using UnityEngine;
using UnityEngine.EventSystems;
using Button = UnityEngine.UI.Button;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Boot
{
    /// <summary>최초 구간 진입의 소유자에게 게임 시작 의도만 전달합니다.</summary>
    public interface IGameStartReceiver
    {
        bool TryRequestGameStart();
    }

    /// <summary>Boot에서 게임 시작 버튼을 표시하고 명시적인 수신자에게 입력을 전달합니다.</summary>
    [DisallowMultipleComponent]
    public sealed class TitleUI : UIBase, IUIInputReceiver
    {
        [SerializeField] private Button startButton;
        [SerializeField] private Button quitButton;
        private IOptionGameExit _gameExit = new OptionGameExit();
        private bool _quitRequested;
        private IGameStartReceiver _receiver;
        private IAudioSystemService _audio;
        private bool _isOpen;
        private bool _canStart;
        private bool _inputBound;

        public override UIId ID => UIId.Title;

        internal void BindStart(IGameStartReceiver receiver)
        {
            if (receiver == null) throw new ArgumentNullException(nameof(receiver));
            ValidateBindings();
            if (_receiver != null && !ReferenceEquals(_receiver, receiver))
                throw new InvalidOperationException("[TitleUI] 게임 시작 수신자가 이미 연결되어 있습니다.");
            _receiver = receiver;
            if (!_inputBound)
            {
                UIInputBinding.Attach(startButton, this, 0);
                UIInputBinding.Attach(quitButton, this, 1);
                _inputBound = true;
            }
            SetCanStart(false);
        }

        internal void UnbindStart(IGameStartReceiver receiver)
        {
            if (!ReferenceEquals(_receiver, receiver)) return;
            SetCanStart(false);
            _receiver = null;
        }

        internal void SetCanStart(bool canStart)
        {
            _canStart = canStart;
            if (startButton != null) startButton.interactable = _canStart && _isOpen && _receiver != null;
        }

        public override void BindUIService(ISystemServiceStoreReader resolver)
        {
            if (resolver == null) throw new ArgumentNullException(nameof(resolver));
            ValidateBindings();
            _audio = resolver.GetService(SystemGroupsService.Global).AudioServiceCommand;
        }

        public override void GetUIData(IServiceModule query) { }

        public override void Open()
        {
            ValidateBindings();
            if (_receiver == null)
                throw new InvalidOperationException("[TitleUI] UIId.Title의 게임 시작 수신자가 필요합니다.");
            _isOpen = true;
            gameObject.SetActive(true);
            SetCanStart(_canStart);
            if (startButton.interactable && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(startButton.gameObject);
        }

        public override void Hide()
        {
            _isOpen = false;
            SetCanStart(false);
            EventSystem events = EventSystem.current;
            if (events != null && events.currentSelectedGameObject != null &&
                events.currentSelectedGameObject.transform.IsChildOf(transform))
                events.SetSelectedGameObject(null);
            gameObject.SetActive(false);
        }

        void IUIInputReceiver.Click(int slot)
        {
            if (slot == 1)
            {
                if (!_isOpen || !isActiveAndEnabled || _quitRequested) return;
                _quitRequested = true;
                quitButton.interactable = false;
                SetCanStart(false);
                _gameExit.Quit();
                return;
            }
            if (slot != 0 || !_isOpen || !_canStart || !isActiveAndEnabled || _receiver == null) return;
            if (_receiver.TryRequestGameStart()) _audio?.Play(AmbgmEvent.GameStart);
            SetCanStart(false);
        }

        void IUIInputReceiver.HoverEnter(int slot) { }
        void IUIInputReceiver.HoverExit(int slot) { }

        private void ValidateBindings()
        {
            if (startButton == null || !startButton.transform.IsChildOf(transform))
                throw new InvalidOperationException("[TitleUI] UIId.Title 내부의 startButton 참조가 필요합니다.");
            if (quitButton == null || quitButton == startButton || !quitButton.transform.IsChildOf(transform))
                throw new InvalidOperationException("[TitleUI] UIId.Title 내부의 별도 quitButton 참조가 필요합니다.");
        }
    }
}
