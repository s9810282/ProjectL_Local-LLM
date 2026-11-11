using System;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageInput;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage
{
    /// <summary>배치 단계의 전투 진입 버튼입니다. 국면·대기 사실은 전투 View가 전달합니다.</summary>
    [DisallowMultipleComponent]
    public sealed class BattleChangerUI : UIBase, IUIInputReceiver
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // 이 기능의 계측 정의입니다. 일반 배포 빌드에서는 선언과 호출을 함께 제외합니다.
        private static readonly Unity.Profiling.ProfilerMarker _profileUIPhaseButtonClick = new("Combat.Action.UI.PhaseButton.Click");
        private static readonly Unity.Profiling.ProfilerMarker _profileUIPhaseButtonHoverEnter = new("Combat.Action.UI.PhaseButton.HoverEnter");
        private static readonly Unity.Profiling.ProfilerMarker _profileUIPhaseButtonHoverExit = new("Combat.Action.UI.PhaseButton.HoverExit");
#endif

        [SerializeField] private Button button;
        private IUISystemService _ui;
        private IStageInputReceiver _input;
        private bool _inputAttached;
        private bool _shown;

        public BattleChangerUI() => Policy = UIOpenPolicy.Parallel;
        public override UIId ID => UIId.BattleChanger;
        /// <summary>튜토리얼이 실제 버튼의 가장자리를 표시할 때 읽는 영역입니다.</summary>
        internal RectTransform ButtonRect => button != null
            ? (RectTransform)button.transform
            : throw new InvalidOperationException("[BattleChangerUI] UIId.BattleChanger의 button 참조가 없습니다.");

        public override void BindUISystemService(IUISystemService service)
        {
            base.BindUISystemService(service);
            _ui = service;
        }

        public override void BindUIService(ISystemServiceStoreReader resolver)
        {
            if (resolver == null) throw new ArgumentNullException(nameof(resolver));
            ValidateReferences();
            if (!_inputAttached)
            {
                UIInputBinding.Attach(button, this, 0);
                _inputAttached = true;
            }
        }

        public override void GetUIData(IServiceModule query) { }

        internal void BindInput(IStageInputReceiver input)
        {
            _input = input ?? throw new ArgumentNullException(nameof(input));
        }

        internal void Refresh(in CombatInputFacts facts, bool suspended)
        {
            bool visible = !suspended && facts.IsBattleDisplayed && facts.HasBattle
                && !facts.HasCompletion && facts.StagePhase == StagePhase.Deployment
                && !facts.HasPendingCommand && !facts.IsPresenting && !facts.IsFinalizing;
            ValidateReferences();
            button.interactable = visible && CombatInputPolicy.CanSubmit(
                facts, facts.HasPendingCommand, CombatViewCommandKind.EnterCombat);
            SetVisible(visible);
        }

        internal void UnbindInput()
        {
            SetVisible(false);
            _input = null;
        }

        private void SetVisible(bool visible)
        {
            if (_shown == visible) return;
            if (_ui == null)
                throw new InvalidOperationException("[BattleChangerUI] UIId.BattleChanger의 UISystem 연결이 필요합니다.");
            if (visible) _ui.OpenUI(ID, true);
            else _ui.CloseUI(ID);
        }

        public override void Open()
        {
            ValidateReferences();
            if (_input == null)
                throw new InvalidOperationException("[BattleChangerUI] UIId.BattleChanger의 전투 입력 연결이 필요합니다.");
            _shown = true;
            gameObject.SetActive(true);
        }

        public override void Hide()
        {
            _shown = false;
            gameObject.SetActive(false);
        }

        void IUIInputReceiver.Click(int slot)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var profileUIPhaseButtonClickSample = _profileUIPhaseButtonClick.Auto();
#endif

            if (slot != 0 || !isActiveAndEnabled || !button.IsInteractable()) return;
            IStageInputReceiver input = _input
                ?? throw new InvalidOperationException("[BattleChangerUI] UIId.BattleChanger의 전투 입력이 해제되었습니다.");
            SetVisible(false);
            input.EnterCombatRequested();
        }

        void IUIInputReceiver.HoverEnter(int slot) {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var profileUIPhaseButtonHoverEnterSample = _profileUIPhaseButtonHoverEnter.Auto();
#endif
 }
        void IUIInputReceiver.HoverExit(int slot) {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var profileUIPhaseButtonHoverExitSample = _profileUIPhaseButtonHoverExit.Auto();
#endif
 }

        private void ValidateReferences()
        {
            if (button == null)
                throw new InvalidOperationException("[BattleChangerUI] UIId.BattleChanger의 button 참조가 필요합니다.");
        }
    }
}
