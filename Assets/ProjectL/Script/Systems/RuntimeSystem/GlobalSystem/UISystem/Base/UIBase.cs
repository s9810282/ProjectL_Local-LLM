using ProjectL.Systems.RuntimeSystem.GlobalSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem;
using ProjectL.Framework.SystemBase;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base
{
    #region 서비스

    public interface IUIService 
    {
        void BindUISystemService(IUISystemService service);
        public void ExportUIService(IUIRegistry registry);

        void GetUIData(IServiceModule query);
          
        public void BindUIService(ISystemServiceStoreReader resolver);

        public void Open();

        public void Hide();
    }

    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem", sourceAssembly: "ProjectL.Systems", sourceClassName: "UIBase")]
    public abstract class UIBase : MonoBehaviour, IUIService
    {
        private IUISystemService uiSystemService;
        private bool _presentationKnown;
        private bool _presentationOpen;

        protected IUISystemService UISystemService => uiSystemService;

        protected UIBase()
        {
            Policy = UIOpenPolicy.Exclusive;
        }

        public UIOpenPolicy Policy { get; protected set; }

        /// <summary>명시적으로 연 뒤 마지막 일반 UI가 닫히면 복귀할 씬 기본 화면입니다.</summary>
        public virtual bool RestoreWhenHistoryEmpty => false;

        // 그룹 멤버는 UIId로 개별 등록하지 않고 소유 그룹이 바인딩과 표시를 맡는다.
        internal UIGroup OwnerGroup { get; set; }
        internal bool IsRuntimeRegistered { get; set; }
        // 반복 프리팹 안의 독립 패널은 소유자가 선택한 인스턴스만 런타임 등록한다.
        internal virtual bool RegisterWithScene => true;

        public virtual void BindUISystemService(IUISystemService service)
        {
            uiSystemService = service ?? throw new System.ArgumentNullException(nameof(service));
        }

        public abstract UIId ID { get; }
        public virtual UIId ParentID => UIId.None;
        public virtual void BindUIService(ISystemServiceStoreReader resolver)
        {
            if (resolver == null) throw new System.ArgumentNullException(nameof(resolver));
        }
        public virtual void ExportUIService(IUIRegistry registry)
        {
            if (registry == null) throw new System.ArgumentNullException(nameof(registry));
            registry.AddUI(ID, this, ParentID);
        }
        public virtual void GetUIData(IServiceModule query)
        {
            if (query == null) throw new System.ArgumentNullException(nameof(query));
        }

        public virtual void Open()
        {
            gameObject.SetActive(true);
            NotifyOpened();
        }

        public virtual void Hide()
        {
            NotifyHidden();
            gameObject.SetActive(false);
        }

        // 부모 비활성화·씬 해제도 동일한 정리 훅을 거친다. 명시적 Open/Hide와 중복 실행하지 않는다.
        private void OnEnable() => NotifyOpened();
        private void OnDisable() => NotifyHidden();
        private void NotifyOpened()
        {
            if (_presentationKnown && _presentationOpen) return;
            _presentationKnown = _presentationOpen = true;
            OnUIOpened();
        }
        private void NotifyHidden()
        {
            if (_presentationKnown && !_presentationOpen) return;
            _presentationKnown = true;
            _presentationOpen = false;
            OnUIHidden();
        }
        protected virtual void OnUIOpened() { }
        protected virtual void OnUIHidden() { }
    }

    #endregion
}
