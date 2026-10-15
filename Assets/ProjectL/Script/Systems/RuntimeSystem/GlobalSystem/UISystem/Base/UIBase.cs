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

        public void BindUISystemService(IUISystemService service)
        {
            uiSystemService = service ?? throw new System.ArgumentNullException(nameof(service));
        }

        public abstract UIId ID { get; }
        public virtual UIId ParentID => UIId.None;
        public abstract void BindUIService(ISystemServiceStoreReader resolver);
        public virtual void ExportUIService(IUIRegistry registry)
        {
            if (registry == null) throw new System.ArgumentNullException(nameof(registry));
            registry.AddUI(ID, this, ParentID);
        }
        public abstract void GetUIData(IServiceModule query);
        public abstract void Hide();
        public abstract void Open();
    }

    #endregion
}
