using System;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.AudioSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Configuration;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.View;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using ProjectL.Systems.ServiceKeys.Runtime;
using ProjectL.Systems.ServiceKeys.Runtime.Global;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.Base;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Stage;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Global
{
    /// <summary>옵션의 첫 화면을 표시하고 입력 독점과 오디오 일시정지를 표시 수명에 맞춰 해제합니다.</summary>
    [DisallowMultipleComponent]
    public sealed class OptionUI : UIBase, IUIInputReceiver, IOptionInputReceiver, IConfirmReceiver
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // 이 기능의 계측 정의입니다. 일반 배포 빌드에서는 선언과 호출을 함께 제외합니다.
        private static readonly Unity.Profiling.ProfilerMarker _profileUIOptionClick = new("Combat.Action.UI.Option.Click");
        private static readonly Unity.Profiling.ProfilerMarker _profileUIOptionOpen = new("Combat.Action.UI.Option.Open");
        private static readonly Unity.Profiling.ProfilerMarker _profileUIOptionClose = new("Combat.Action.UI.Option.Close");
        private static readonly Unity.Profiling.ProfilerMarker _profileUIOptionConfirmOpen = new("Combat.Action.UI.Option.ConfirmOpen");
#endif

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // 이 기능의 계측 정의입니다. 일반 배포 빌드에서는 선언과 호출을 함께 제외합니다.
        private static readonly Unity.Profiling.ProfilerMarker _profileUIAbandonAccept = new("Combat.Action.UI.Abandon.Accept");
        private static readonly Unity.Profiling.ProfilerMarker _profileUIAbandonDecline = new("Combat.Action.UI.Abandon.Decline");
#endif

        private const int OpenSlot = 0;
        private const int CloseSlot = 1;
        private const int AbandonSlot = 3;
        private const int ExitSlot = 4;
        private const int GamePlaySlot = 5;
        private const int SoundSlot = 6;

        [SerializeField] private GameObject panel;
        [SerializeField] private Button optionButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private ConfirmUI confirm;
        [SerializeField] private Button runAbandonButton;
        [SerializeField] private Button exitButton;
        [SerializeField] private Button gamePlayButton;
        [SerializeField] private Button soundButton;
        [SerializeField] private KeySettingUI keySetting;
        [SerializeField] private SoundSettingUI soundSetting;

        private IUISystemService _ui;
        private IInputLayerOverrideService _input;
        private IAudioSystemService _audio;
        private IOptionCloseRequestService _closeRequests;
        private IKeyBindingService _keys;
        private ICombatAbandonRequestService _combatAbandon;
        private IStageRunAbandonRequestService _stageAbandon;
        private ILifeCycleManager _lifeCycleManager;
        private IOptionGameExit _gameExit = new OptionGameExit();
        private bool _quitAfterSettings;
        private bool _quitRequested;
        private IDisposable _layerScope;
        private GameObject _blocker;
        private Transform _canvasChild;
        private GameObject _previousSelection;
        private int _previousSiblingIndex;
        private bool _wasAudioPaused;
        private bool _restoreAudio;
        private float _previousTimeScale;
        private bool _restoreTimeScale;
        private bool _wasButtonInteractable;
        private bool _buttonsBound;
        private bool _isOpen;

        public override UIId ID => UIId.Setting;
        public bool IsOpen => _isOpen;

        private void Awake()
        {
            Policy = UIOpenPolicy.Parallel;
            BindButtons();
            if (panel != null) panel.SetActive(false);
        }

        private void BindButtons()
        {
            if (_buttonsBound) return;
            if (panel == null || panel == gameObject || !panel.transform.IsChildOf(transform)
                || optionButton == null || closeButton == null)
                throw new InvalidOperationException("[OptionUI] 자식 Panel과 Option·Close Button을 연결해야 합니다.");
            UIInputBinding.Attach(optionButton, this, OpenSlot);
            UIInputBinding.Attach(closeButton, this, CloseSlot);
            UIInputBinding.Attach(runAbandonButton, this, AbandonSlot);
            UIInputBinding.Attach(exitButton, this, ExitSlot);
            UIInputBinding.Attach(gamePlayButton, this, GamePlaySlot);
            UIInputBinding.Attach(soundButton, this, SoundSlot);
            if (confirm == null || !confirm.transform.IsChildOf(panel.transform)
                || runAbandonButton == null || exitButton == null)
                throw new InvalidOperationException("[OptionUI] Panel 아래의 ConfirmUI와 RunAbandon·Exit Button을 연결해야 합니다.");
            confirm.BindReceiver(this);
            if (gamePlayButton == null || soundButton == null || keySetting == null || soundSetting == null
                || !keySetting.transform.IsChildOf(panel.transform) || !soundSetting.transform.IsChildOf(panel.transform))
                throw new InvalidOperationException("[OptionUI] GamePlay·Sound Button과 Panel 아래의 설정 화면을 연결해야 합니다.");
            if (panel.GetComponent<UIPointerClickBoundary>() == null)
                panel.AddComponent<UIPointerClickBoundary>();
            keySetting.BindCloseReceiver(this);
            _buttonsBound = true;
        }

        public override void BindUISystemService(IUISystemService service)
        {
            base.BindUISystemService(service);
            _ui = service;
            Policy = UIOpenPolicy.Parallel;
            BindButtons();
        }

        public override void GetUIData(IServiceModule query)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
        }

        public override void BindUIService(ISystemServiceStoreReader resolver)
        {
            if (resolver == null) throw new ArgumentNullException(nameof(resolver));
            var global = resolver.GetService(SystemGroupsService.Global);
            _input = global.InputServiceCommand.InputLayerOverrides;
            _keys = global.InputServiceCommand.KeyBindings;
            _audio = global.AudioServiceCommand;
            _closeRequests = global.OptionCloseRequests;
            _combatAbandon = resolver.GetService(SystemGroupsService.InGame).ViewGroup.Combat;
            var lifeCycles = resolver.GetService(SystemGroupsService.LifeCycle);
            _lifeCycleManager = lifeCycles.LifeCycleManager;
            _stageAbandon = lifeCycles.StageRunAbandon;
            if (isActiveAndEnabled) _closeRequests.Bind(this);
        }

        public override void Open()
        {
            if (_isOpen) return;
            _quitRequested = false;
            BindButtons();
            if (_input == null || _audio == null || _keys == null)
                throw new InvalidOperationException("[OptionUI] 입력과 오디오 서비스가 연결되어야 합니다.");

            _wasAudioPaused = _audio.IsGamePaused;
            _wasButtonInteractable = optionButton.interactable;
            _isOpen = true;
            try
            {
                _keys.BeginEdit();
                soundSetting.BeginEdit();
                _layerScope = _input.OverrideLayer(GlobalInputNames.Settings);
                _previousTimeScale = Time.timeScale;
                _restoreTimeScale = true;
                Time.timeScale = 0f;
                _restoreAudio = true;
                _audio.SetGamePaused(true);
                ShowBlocker();
                optionButton.interactable = false;
                panel.SetActive(true);
                if (EventSystem.current != null)
                {
                    _previousSelection = EventSystem.current.currentSelectedGameObject;
                    EventSystem.current.SetSelectedGameObject(closeButton.gameObject);
                }
            }
            catch
            {
                Hide();
                throw;
            }
        }

        public override void Hide()
        {
            _quitAfterSettings = false;
            bool wasOpen = _isOpen;
            var events = EventSystem.current;
            bool restoreSelection = wasOpen && events != null && events.currentSelectedGameObject != null
                && panel != null && events.currentSelectedGameObject.transform.IsChildOf(panel.transform);
            _isOpen = false;
            if (confirm != null) confirm.Hide();
            if (keySetting != null) keySetting.Hide();
            _keys?.CancelEdit();
            if (soundSetting != null) soundSetting.Hide();
            if (soundSetting != null) soundSetting.CancelEdit();
            if (panel != null) panel.SetActive(false);
            if (_blocker != null) _blocker.SetActive(false);
            if (wasOpen)
            {
                if (optionButton != null) optionButton.interactable = _wasButtonInteractable;
                if (_canvasChild != null) _canvasChild.SetSiblingIndex(_previousSiblingIndex);
                if (restoreSelection)
                    events.SetSelectedGameObject(_previousSelection != null && _previousSelection.activeInHierarchy
                        ? _previousSelection : null);
                _previousSelection = null;
            }

            IDisposable scope = _layerScope;
            _layerScope = null;
            try
            {
                scope?.Dispose();
            }
            finally
            {
                try
                {
                    if (_restoreAudio)
                    {
                        _restoreAudio = false;
                        _audio.SetGamePaused(_wasAudioPaused);
                    }
                }
                finally
                {
                    if (_restoreTimeScale)
                    {
                        _restoreTimeScale = false;
                        Time.timeScale = _previousTimeScale;
                    }
                }
            }
        }

        private void ShowBlocker()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) throw new InvalidOperationException("[OptionUI] 부모 Canvas가 필요합니다.");
            if (_blocker == null)
            {
                _blocker = new GameObject("OptionInputBlocker", typeof(RectTransform), typeof(Image), typeof(UIPointerClickBoundary));
                _blocker.layer = gameObject.layer;
                var rect = (RectTransform)_blocker.transform;
                rect.SetParent(canvas.transform, false);
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                _blocker.GetComponent<Image>().color = Color.clear;
            }

            _canvasChild = transform;
            while (_canvasChild.parent != canvas.transform)
                _canvasChild = _canvasChild.parent;
            _previousSiblingIndex = _canvasChild.GetSiblingIndex();
            _blocker.transform.SetAsLastSibling();
            _canvasChild.SetAsLastSibling();
            _blocker.SetActive(true);
        }

        void IUIInputReceiver.Click(int slot)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var profileUIOptionClickSample = _profileUIOptionClick.Auto();
#endif

            if (_ui == null) return;
            if (slot == OpenSlot) RequestOpen();
            else if (slot == CloseSlot) RequestClose();
            else if (_isOpen && slot == AbandonSlot) RequestRunAbandonConfirmation();
            else if (_isOpen && slot == ExitSlot) ShowConfirm(ConfirmAction.QuitGame);
            else if (_isOpen && !confirm.IsOpen && slot == GamePlaySlot)
            {
                _ui.CloseUI(soundSetting.ID);
                _ui.OpenChildUI(ID, keySetting.ID);
            }
            else if (_isOpen && !confirm.IsOpen && slot == SoundSlot)
            {
                _ui.CloseUI(keySetting.ID);
                _ui.OpenChildUI(ID, soundSetting.ID);
            }
        }

        public void RequestOpen()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var profileUIOptionOpenSample = _profileUIOptionOpen.Auto();
#endif

            if (_isOpen || _ui == null) return;
            // 부모가 열린 것으로 등록된 뒤 기본 자식을 연다.
            _ui.OpenUI(ID);
            try { _ui.OpenChildUI(ID, keySetting.ID); }
            catch { _ui.CloseUI(ID); throw; }
        }

        public void RequestClose()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var profileUIOptionCloseSample = _profileUIOptionClose.Auto();
#endif

            if (!_isOpen || _ui == null) return;
            if (confirm.IsOpen)
            {
                _ui.CloseUI(UIId.Confirm);
                // 종료를 이미 확인했다면 저장 확인의 ESC는 저장 취소로 처리하고 종료한다.
                if (_quitAfterSettings)
                    ((IConfirmReceiver)this).Respond(ConfirmAction.SaveSettings, false);
                return;
            }

            if (!_keys.HasChanges && !soundSetting.HasChanges)
            {
                _ui.CloseUI(ID);
                return;
            }
            ShowConfirm(ConfirmAction.SaveSettings);
        }

        private void RequestRunAbandonConfirmation()
        {
            if (_lifeCycleManager.CurrentKey == LifeCycleKeys.Stage)
            {
                if (_stageAbandon.CanRequestAbandon)
                    ShowConfirm(ConfirmAction.AbandonStageRun);
            }
            else
            {
                ShowConfirm(ConfirmAction.AbandonRun);
            }
        }

        private void ShowConfirm(ConfirmAction action)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var profileUIOptionConfirmOpenSample = _profileUIOptionConfirmOpen.Auto();
#endif

            keySetting.CancelWaiting();
            confirm.Show(action);
        }

        void IConfirmReceiver.Respond(ConfirmAction action, bool accepted)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var profileAbandon = action == ConfirmAction.AbandonRun ? (accepted ? _profileUIAbandonAccept : _profileUIAbandonDecline).Auto() : default;
#endif

            if (!_isOpen) return;
            if (accepted)
            {
                switch (action)
                {
                    case ConfirmAction.SaveSettings:
                        _keys.SaveEdit();
                        soundSetting.SaveEdit();
                        break;
                    case ConfirmAction.AbandonRun:
                        // Y 확인 뒤에만 요청한다. 수락되면 일시정지를 풀어 기존 전투 종료 흐름을 진행한다.
                        if (_combatAbandon != null && _combatAbandon.TryRequestAbandon())
                            _ui.CloseUI(ID);
                        break;
                    case ConfirmAction.AbandonStageRun:
                        if (_stageAbandon.TryRequestAbandon())
                            _ui.CloseUI(ID);
                        break;
                    case ConfirmAction.QuitGame:
                        if (_keys.HasChanges || soundSetting.HasChanges)
                        {
                            _quitAfterSettings = true;
                            try { ShowConfirm(ConfirmAction.SaveSettings); }
                            catch { _quitAfterSettings = false; throw; }
                        }
                        else
                        {
                            QuitGame();
                        }
                        break;
                    case ConfirmAction.SaveProgress:
                        // 진행상황 저장 흐름이 없으므로 호출하지 않는다.
                        break;
                }
            }

            // 저장 Y/N 모두 옵션을 닫는다. 종료 확인에서 이어진 저장이면 최종 종료까지 진행한다.
            if (action == ConfirmAction.SaveSettings)
            {
                if (!accepted) _keys.CancelEdit();
                if (_quitAfterSettings) QuitGame();
                else _ui.CloseUI(ID);
            }
        }

        private void QuitGame()
        {
            if (_quitRequested) return;
            _quitRequested = true;
            // 종료 전에 기존 Hide 경로로 입력·오디오·시간과 취소할 편집을 정리한다.
            _ui.CloseUI(ID);
            _gameExit.Quit();
        }

        void IUIInputReceiver.HoverEnter(int slot) { }
        void IUIInputReceiver.HoverExit(int slot) { }

        private void OnEnable() => _closeRequests?.Bind(this);

        private void OnDisable()
        {
            _closeRequests?.Unbind(this);
            Hide();
        }

        private void OnDestroy()
        {
            Hide();
            if (_blocker == null) return;
            if (Application.isPlaying) Destroy(_blocker);
            else DestroyImmediate(_blocker);
        }
    }
}
