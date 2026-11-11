using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem;
using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.ActionButtons;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Hud
{
    /// <summary>
    /// Combat 씬 Canvas의 전투 행동 버튼(공격·턴 종료)입니다(행동 버튼 설계 A1·B1).
    /// 받은 값만 그리고, 누를 수 있는지·입력을 받아들일지는 판단하지 않습니다.
    /// </summary>
    /// <remarks>
    /// 보임과 공격·턴 종료의 누름 가능은 <see cref="CombatActionButtons"/>(Policy·공격 진입 판단)가 정합니다.
    /// 이동 되돌리기는 기존 복귀 슬롯과 버튼을 사용합니다.
    /// 처리는 키 입력과 같은 수신 창구·Router가 맡습니다. 공격 버튼 자체의 이미지·내부 아이콘·텍스트는 공격 상태에 맞춰 전환하고, 나머지 표시는 씬에서 작성합니다.
    /// 버튼 입력은 <see cref="UIInputBinding"/>이 이 패널(<see cref="IUIInputReceiver"/>)에 슬롯으로 전달합니다(Assets/AGENTS.md, 개선 설계 D1).
    /// <c>Button.onClick</c>에는 아무것도 연결하지 않습니다.
    /// </remarks>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display")]
    [DisallowMultipleComponent]
    public sealed class CombatActionPanel : UIBase, IUIInputReceiver
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // 이 기능의 계측 정의입니다. 일반 배포 빌드에서는 선언과 호출을 함께 제외합니다.
        private static readonly Unity.Profiling.ProfilerMarker _profileUIActionPanelClick = new("Combat.Action.UI.ActionPanel.Click");
        private static readonly Unity.Profiling.ProfilerMarker _profileUIActionPanelHoverEnter = new("Combat.Action.UI.ActionPanel.HoverEnter");
        private static readonly Unity.Profiling.ProfilerMarker _profileUIActionPanelHoverExit = new("Combat.Action.UI.ActionPanel.HoverExit");
#endif

        public CombatActionPanel() => Policy = UIOpenPolicy.Parallel;
        public override UIId ID => UIId.CombatActionPanel;

        private const int AttackSlot = 0;
        private const int EndTurnSlot = 1;
        private const int ReturnSlot = 2;

        [Tooltip("공격 모드에 들어가는 버튼입니다. 공격 모드 중에 누르면 모드를 끝냅니다.")]
        [SerializeField] private Button attackButton;
        [Tooltip("공격 모드가 아닐 때 버튼 자체의 Image에 표시할 이미지입니다.")]
        [SerializeField] private Sprite attackButtonSprite;
        [Tooltip("공격 모드일 때 버튼 자체의 Image에 표시할 이미지입니다.")]
        [SerializeField] private Sprite moveButtonSprite;
        [Tooltip("공격·이동 Sprite를 표시할 Atk 버튼의 내부 아이콘 Image입니다. 버튼 자체의 Image와 별도로 연결합니다.")]
        [SerializeField] private Image attackImage;
        [Tooltip("공격 모드가 아닐 때 표시할 공격 버튼 이미지입니다.")]
        [SerializeField] private Sprite attackSprite;
        [Tooltip("공격 모드일 때 표시할 이동 버튼 이미지입니다.")]
        [SerializeField] private Sprite moveSprite;
        [Tooltip("공격 모드가 아니면 '공격', 공격 모드이면 '이동'을 표시할 버튼 텍스트입니다.")]
        [SerializeField] private TMP_Text attackText;
        [Tooltip("플레이어 턴을 끝내는 버튼입니다.")]
        [SerializeField] private Button endTurnButton;
        [Tooltip("선택 유닛의 실행한 이동을 되돌리는 버튼입니다.")]
        [SerializeField] private Button returnButton;
        [Tooltip("공격 모드 동안 켜는 강조 오브젝트입니다. 비워 두면 강조하지 않습니다.")]
        [SerializeField] private GameObject attackActiveIndicator;

        private Action _attack;
        private Action _endTurn;
        private Action _return;
        private bool _isInputAttached;
        private bool _hasShown;
        private CombatActionButtons _shown;

        /// <summary>설명 강조에 사용할 기존 공격 버튼 영역입니다. 버튼 참조의 원본은 이 패널만 소유합니다.</summary>
        internal RectTransform AttackButtonRect => attackButton != null
            ? (RectTransform)attackButton.transform
            : throw new InvalidOperationException($"[UIId.CombatActionPanel] '{name}'의 필수 참조가 없습니다: {nameof(attackButton)}.");

        /// <summary>설명 강조에 사용할 기존 턴 종료 버튼 영역입니다.</summary>
        internal RectTransform EndTurnButtonRect => endTurnButton != null
            ? (RectTransform)endTurnButton.transform
            : throw new InvalidOperationException($"[UIId.CombatActionPanel] '{name}'의 필수 참조가 없습니다: {nameof(endTurnButton)}.");

        protected override void OnUIOpened() => InvalidateShownValues();

        protected override void OnUIHidden() => InvalidateShownValues();

        private void InvalidateShownValues()
        {
            _hasShown = false;
        }

        /// <summary>
        /// 공격·턴 종료·되돌리기 입력을 연결합니다. 모두 null이면 해제만 합니다.
        /// 버튼과 이 패널의 입력 연결(<see cref="UIInputBinding"/>)은 처음 연결할 때 한 번만 만듭니다.
        /// </summary>
        internal void Bind(Action attack, Action endTurn, Action returnToCheckpoint)
        {
            bool releasing = attack == null && endTurn == null && returnToCheckpoint == null;
            if (!releasing && (attack == null || endTurn == null || returnToCheckpoint == null))
                throw new ArgumentException("[CombatActionPanel] 세 입구는 함께 연결하거나 함께 해제해야 합니다.");
            _attack = null;
            _endTurn = null;
            _return = null;

            if (releasing)
                return;

            AttachInputOnce();
            _attack = attack;
            _endTurn = endTurn;
            _return = returnToCheckpoint;
        }

        void IUIInputReceiver.Click(int slot)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var profileUIActionPanelClickSample = _profileUIActionPanelClick.Auto();
#endif

            switch (slot)
            {
                case AttackSlot:
                    _attack?.Invoke();
                    break;
                case EndTurnSlot:
                    _endTurn?.Invoke();
                    break;
                case ReturnSlot:
                    _return?.Invoke();
                    break;
            }
        }

        void IUIInputReceiver.HoverEnter(int slot)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var profileUIActionPanelHoverEnterSample = _profileUIActionPanelHoverEnter.Auto();
#endif

        }

        void IUIInputReceiver.HoverExit(int slot)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var profileUIActionPanelHoverExitSample = _profileUIActionPanelHoverExit.Auto();
#endif

        }

        /// <summary>버튼 보임·누름 가능·공격 이미지·텍스트·강조를 적용합니다. 직전 값과 같으면 다시 쓰지 않습니다.</summary>
        internal void SetAvailable(in CombatActionButtons buttons)
        {
            string missing = FindMissingReference();
            if (missing != null)
                throw new InvalidOperationException($"[CombatActionPanel] '{name}'의 필수 참조가 없습니다: {missing}.");

            if (_hasShown && _shown.Equals(buttons)) return;

            // 개폐는 UISystem으로, 버튼 가용성은 받은 전투 사실로 결정한다.
            // UI 없는 격리 표시 조립만 직접 Open/Hide한다.
            if (UISystemService != null)
            {
                if (buttons.Visible) UISystemService.OpenUI(ID, true);
                else UISystemService.CloseUI(ID);
            }
            else if (buttons.Visible) Open();
            else Hide();
            Apply(attackButton, buttons.Visible, buttons.CanAttack);
            Apply(endTurnButton, buttons.Visible, buttons.CanEndTurn);
            Apply(returnButton, buttons.Visible, buttons.CanUseReturn);
            Sprite buttonSprite = buttons.IsAttackActive ? moveButtonSprite : attackButtonSprite;
            if (attackButton.image.sprite != buttonSprite)
                attackButton.image.sprite = buttonSprite;
            Sprite sprite = buttons.IsAttackActive ? moveSprite : attackSprite;
            if (attackImage.sprite != sprite)
                attackImage.sprite = sprite;
            string text = buttons.IsAttackActive ? "이동" : "공격";
            if (attackText.text != text)
                attackText.text = text;
            if (attackActiveIndicator != null)
            {
                bool indicate = buttons.Visible && buttons.IsAttackActive;
                if (attackActiveIndicator.activeSelf != indicate)
                    attackActiveIndicator.SetActive(indicate);
            }

            _shown = buttons;
            _hasShown = true;
        }

        /// <summary>행동 버튼 표시와 입력에 필요한 참조를 검사합니다.</summary>
        internal string FindMissingReference()
        {
            if (attackButton == null)
                return $"{nameof(CombatActionPanel)}.{nameof(attackButton)}";
            if (attackButton.image == null)
                return $"{nameof(CombatActionPanel)}.{nameof(attackButton)}.image (Button.targetGraphic)";
            if (attackButtonSprite == null)
                return $"{nameof(CombatActionPanel)}.{nameof(attackButtonSprite)}";
            if (moveButtonSprite == null)
                return $"{nameof(CombatActionPanel)}.{nameof(moveButtonSprite)}";
            if (attackImage == null)
                return $"{nameof(CombatActionPanel)}.{nameof(attackImage)}";
            if (attackSprite == null)
                return $"{nameof(CombatActionPanel)}.{nameof(attackSprite)}";
            if (moveSprite == null)
                return $"{nameof(CombatActionPanel)}.{nameof(moveSprite)}";
            if (attackText == null)
                return $"{nameof(CombatActionPanel)}.{nameof(attackText)}";
            if (returnButton == null)
                return $"{nameof(CombatActionPanel)}.{nameof(returnButton)}";
            if (endTurnButton == null)
                return $"{nameof(CombatActionPanel)}.{nameof(endTurnButton)}";
            return null;
        }

        private static void Apply(Button button, bool visible, bool interactable)
        {
            if (button.gameObject.activeSelf != visible)
                button.gameObject.SetActive(visible);
            if (button.interactable != interactable)
                button.interactable = interactable;
        }

        private void AttachInputOnce()
        {
            if (_isInputAttached)
                return;

            UIInputBinding.Attach(attackButton, this, AttackSlot);
            UIInputBinding.Attach(endTurnButton, this, EndTurnSlot);
            if (returnButton != null)
                UIInputBinding.Attach(returnButton, this, ReturnSlot);
            _isInputAttached = true;
        }
    }
}
