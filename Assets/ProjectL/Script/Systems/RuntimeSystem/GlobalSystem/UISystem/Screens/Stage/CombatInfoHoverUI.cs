using System;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage
{
    /// <summary>전투 정보 버튼의 호버 동안 설명 패널을 UISystem의 Exclusive 정책으로 표시합니다.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class CombatInfoHoverUI : UIBase, IUIInputReceiver
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // 이 기능의 계측 정의입니다. 일반 배포 빌드에서는 선언과 호출을 함께 제외합니다.
        private static readonly Unity.Profiling.ProfilerMarker _profileUICombatInfoClick = new("Combat.Action.UI.CombatInfo.Click");
        private static readonly Unity.Profiling.ProfilerMarker _profileUICombatInfoHoverEnter = new("Combat.Action.UI.CombatInfo.HoverEnter");
        private static readonly Unity.Profiling.ProfilerMarker _profileUICombatInfoHoverExit = new("Combat.Action.UI.CombatInfo.HoverExit");
#endif

        [SerializeField] private UIId id;
        [SerializeField] private Button hoverButton;
        private IUISystemService _ui;
        private bool _inputBound;

        public override UIId ID => id;
        // 정보창이 Exclusive이므로 설명창은 그 자식이어야 열 때 본문을 닫지 않는다.
        public override UIId ParentID => UIId.CombatUnitInfo;

        public override void ExportUIService(IUIRegistry registry)
        {
            ValidateReferences();
            base.ExportUIService(registry);
        }

        public override void BindUISystemService(IUISystemService service)
        {
            base.BindUISystemService(service);
            _ui = service;
        }

        public override void BindUIService(ISystemServiceStoreReader resolver)
        {
            if (resolver == null) throw new ArgumentNullException(nameof(resolver));
            ValidateReferences();
            // 설명 패널이 버튼의 호버를 가로채지 않게 합니다.
            GetComponent<CanvasGroup>().blocksRaycasts = false;
            if (_inputBound) return;
            UIInputBinding.Attach(hoverButton, this, 0, true);
            _inputBound = true;
        }

        public override void GetUIData(IServiceModule query) { }
        public override void Open() => gameObject.SetActive(true);
        public override void Hide() => gameObject.SetActive(false);

        // 호버 전용입니다. 클릭·Submit은 표시를 고정하지 않습니다.
        void IUIInputReceiver.Click(int slot) {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var profileUICombatInfoClickSample = _profileUICombatInfoClick.Auto();
#endif
 }
        void IUIInputReceiver.HoverEnter(int slot)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var profileUICombatInfoHoverEnterSample = _profileUICombatInfoHoverEnter.Auto();
#endif
            _ui.OpenUI(ID);
        }
        void IUIInputReceiver.HoverExit(int slot)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var profileUICombatInfoHoverExitSample = _profileUICombatInfoHoverExit.Auto();
#endif
            _ui.CloseUI(ID);
        }

        private void ValidateReferences()
        {
            if (id != UIId.CombatCategoryInfo && id != UIId.CombatCostInfo)
                throw new InvalidOperationException($"[CombatInfoHoverUI] {name}: UIId는 CombatCategoryInfo 또는 CombatCostInfo여야 합니다.");
            if (hoverButton == null)
                throw new InvalidOperationException($"[CombatInfoHoverUI] UIId.{ID}: hoverButton 참조가 필요합니다.");
            if (transform == hoverButton.transform || hoverButton.transform.IsChildOf(transform))
                throw new InvalidOperationException($"[CombatInfoHoverUI] UIId.{ID}: hoverButton은 패널 외부에 있어야 합니다.");
            if (GetComponent<CanvasGroup>() == null)
                throw new InvalidOperationException($"[CombatInfoHoverUI] UIId.{ID}: CanvasGroup이 필요합니다.");
        }
    }
}
