using System;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Global
{
    /// <summary>타이틀 종료 후 공통 장식을 UISystem의 유지 UI로 표시합니다.</summary>
    public sealed class AfterTitleUI : GlobalUIBase
    {
        public AfterTitleUI()
        {
            Policy = UIOpenPolicy.Parallel;
        }

        public override UIId ID => UIId.AfterTitle;

        public override void BindUIService(ISystemServiceStoreReader resolver)
        {
            if (resolver == null) throw new ArgumentNullException(nameof(resolver));
        }

        public override void GetUIData(IServiceModule query)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
        }

        public override void Open() => gameObject.SetActive(true);

        public override void Hide() => gameObject.SetActive(false);
    }
}
