using System;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Configuration;

namespace ProjectL.Systems.Factory.Runtime.InGame
{
    /// <summary>같은 인게임 조립의 그룹과 View 입력 등록을 전달합니다.</summary>
    internal sealed class InGameSystemComposition
    {
        internal IExportSystemServices Group { get; }
        internal InputRegistrationSet Registration { get; }

        internal InGameSystemComposition(IExportSystemServices group, InputRegistrationSet registration)
        {
            Group = group ?? throw new ArgumentNullException(nameof(group));
            Registration = registration ?? throw new ArgumentNullException(nameof(registration));
        }
    }
}
