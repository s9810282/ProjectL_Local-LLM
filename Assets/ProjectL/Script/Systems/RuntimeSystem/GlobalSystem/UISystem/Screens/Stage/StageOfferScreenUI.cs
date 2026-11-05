using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Common.ScrollPanel;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage
{
    /// <summary>
    /// 공통 레이아웃을 쓰는 등록 화면의 기반입니다. 열림 상태·알림·잠금을 맡고, 그리기와 입력 전달은 자식 레이아웃에 맡깁니다.
    /// 하위는 ID·부모·정책만 답합니다. 규칙·결과 판정은 소유하지 않습니다.
    /// </summary>
    /// <remarks>
    /// 레이아웃은 씬·프리팹에 미리 놓인 자식입니다. 닫을 때 루트는 그대로 두고 레이아웃만 끄므로 <see cref="UIBase"/>의 Open/Hide를 부르지 않고,
    /// 열림 상태의 원본은 <see cref="IsOpen"/> 하나입니다. 씬에서 루트를 꺼 둬도 열 때 켭니다.
    /// </remarks>
    public abstract class StageOfferScreenUI : UIBase, IStageOfferDisplay, IScrollPopupDisplay
    {
        [Tooltip("자식으로 놓은 프리팹의 레이아웃입니다. 이벤트 스테이지는 StageOfferUI, 증강은 TraitUI입니다.")]
        [SerializeField] private StageOfferLayoutView layout;

        private bool _isOpen;
        private bool _canSubmit;
        private bool _hasRendered;

        public bool IsOpen => _isOpen;
        public ScrollPanelPresentation ScrollPresentation
        {
            get
            {
                ScrollPanelPresentation presentation = Layout.ScrollPresentation;
                if (RequiresScrollPresentation && presentation == null)
                    throw new InvalidOperationException($"[{GetType().Name}] '{name}'의 layout에 scrollPresentation 참조가 필요합니다.");
                return presentation;
            }
        }
        /// <summary>팝업 수명 소유자에게 열림 상태 변화를 동기로 알립니다. 입력 사건이 아닙니다.</summary>
        public event Action<bool> OpenStateChanged;

        /// <summary>그릴 때 첫 카드에 초점을 둘지입니다. 나가기가 없어 카드로만 진행하는 화면이 켭니다.</summary>
        protected virtual bool FocusFirstCardOnRender => false;

        /// <summary>두루마리를 사용하는 화면만 필수로 요구합니다.</summary>
        protected virtual bool RequiresScrollPresentation => false;

        private StageOfferLayoutView Layout => layout != null ? layout :
            throw new InvalidOperationException($"[{GetType().Name}] '{name}'의 layout 참조가 필요합니다.");

        // 하위가 Awake를 새로 선언하면 숨김 경고가 나도록 protected로 둡니다(기반 초기화를 빠뜨리지 않게).
        protected void Awake()
        {
            if (layout != null) layout.Prepare(this);
        }

        public override void Open()
        {
            _ = ScrollPresentation;
            // 루트가 꺼진 채 로드되면 Awake가 돌지 않았으므로 켠 뒤 준비를 확인합니다. 준비는 한 번만 일합니다.
            gameObject.SetActive(true);
            Layout.Prepare(this);
            bool wasOpen = _isOpen;
            _isOpen = true;
            Layout.SetVisible(true);
            RefreshInteractability();
            if (!wasOpen) NotifyOpenStateChanged(true);
        }

        public override void Hide()
        {
            bool wasOpen = _isOpen;
            _isOpen = false;
            try
            {
                RefreshInteractability();
                if (wasOpen) NotifyOpenStateChanged(false);
            }
            finally
            {
                // 닫힘 알림 안에서 다시 열린 최신 표시를 이전 Hide가 덮지 않습니다.
                if (!_isOpen && layout != null) layout.SetVisible(false);
            }
        }

        public void BindInput(IStageOfferInput input)
        {
            // 씬 정리 중에는 레이아웃이 먼저 없어질 수 있습니다. 해제는 그대로 끝납니다.
            if (layout != null) layout.BindInput(input);
            RefreshInteractability();
        }

        public void Render(StageOfferPopupModel model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            _hasRendered = true;
            Layout.Render(model);
            RefreshInteractability();
            if (_isOpen && FocusFirstCardOnRender) Layout.FocusFirstCard();
        }

        public void ShowError(string message) => Layout.ShowError(message);

        public void SetCanSubmit(bool canSubmit)
        {
            _canSubmit = canSubmit;
            RefreshInteractability();
        }

        internal void Validate()
        {
            StageOfferLayoutView checkedLayout = Layout;
            if (checkedLayout.gameObject == gameObject || !checkedLayout.transform.IsChildOf(transform))
                throw new InvalidOperationException($"[{GetType().Name}] '{name}'의 layout은 화면의 별도 자식이어야 합니다.");
            _ = ScrollPresentation;
            checkedLayout.Validate();
        }

        private void RefreshInteractability()
        {
            if (layout != null) layout.SetInteractable(_isOpen && _canSubmit && _hasRendered);
        }

        private void NotifyOpenStateChanged(bool open)
        {
            Action<bool> changed = OpenStateChanged;
            if (changed == null) return;
            List<Exception> failures = null;
            foreach (Action<bool> observer in changed.GetInvocationList())
            {
                try { observer(open); }
                catch (Exception error) { (failures ??= new()).Add(error); }
            }
            if (failures != null)
                throw new AggregateException($"[{GetType().Name}] 화면 상태 알림 중 실패했습니다.", failures);
        }
    }
}
