using System;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Configuration;

namespace ProjectL.Systems.Factory.Runtime.InGame.View
{
    /// <summary>같은 View 조립에서 생성한 그룹과 입력 등록을 부모에게 전달합니다.</summary>
    internal sealed class InGameViewSystemComposition
    {
        internal IExportSystemServices Group { get; }
        internal InputRegistrationSet Registration { get; }

        internal InGameViewSystemComposition(IExportSystemServices group, InputRegistrationSet registration)
        {
            Group = group ?? throw new ArgumentNullException(nameof(group));
            Registration = registration ?? throw new ArgumentNullException(nameof(registration));
        }
    }
}
