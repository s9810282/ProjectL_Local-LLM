using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using ProjectL.Framework.SystemBase;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Loading
{
    /// <summary>
    /// 씬 전환 동안만 표시되는 UI입니다.
    /// UISystem의 씬 탐색 과정에서 일반 UIBase와 동일하게 자동 등록됩니다.
    /// </summary>
    public sealed class LoadingUI : UIBase
    {
        public override UIId ID => UIId.Loading;

        private void Awake()
        {
            // 씬 전환 중에도 로딩 UI가 파괴되지 않아야 완료 시 안전하게 닫을 수 있습니다.
            DontDestroyOnLoad(gameObject);
            gameObject.SetActive(false);
        }

        public override void ExportUIService(IUIRegistry registry)
        {
            if (registry == null)
                throw new ArgumentNullException(nameof(registry));

            registry.AddUI(ID, this);
        }

        public override void BindUIService(ISystemServiceStoreReader resolver)
        {
            if (resolver == null)
                throw new ArgumentNullException(nameof(resolver));
        }

        public override void GetUIData(IServiceModule query)
        {
            if (query == null)
                throw new ArgumentNullException(nameof(query));
        }

        public override void Open()
        {
            gameObject.SetActive(true);
        }

        public override void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
