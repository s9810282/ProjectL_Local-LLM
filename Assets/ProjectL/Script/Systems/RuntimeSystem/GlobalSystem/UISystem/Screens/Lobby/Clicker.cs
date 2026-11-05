using System;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Lobby
{
    /// <summary>로비 시설 안내를 표시하며 다른 일반 UI 종료 후 기본 화면으로 복귀합니다.</summary>
    [DisallowMultipleComponent]
    public sealed class Clicker : UIBase
    {
        public override UIId ID => UIId.Clicker;
        public override bool RestoreWhenHistoryEmpty => true;

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
