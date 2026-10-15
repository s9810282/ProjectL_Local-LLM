using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;

namespace ProjectL.Systems
{
    public class TestGlobalUI : GlobalUIBase
    {
        public override UIId ID => UIId.TestGlobal;

        private void Start() => Open();

        public override void BindUIService(ISystemServiceStoreReader resolver) { }

        public override void GetUIData(IServiceModule query) { }

        public override void Open() => gameObject.SetActive(true);

        public override void Hide() => gameObject.SetActive(false);
    }
}
