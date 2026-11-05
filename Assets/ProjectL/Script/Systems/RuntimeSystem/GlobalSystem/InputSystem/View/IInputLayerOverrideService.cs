using System;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Data;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.View
{
    /// <summary>화면 입력을 잠시 독점합니다. 해제하면 그동안 요청된 마지막 Layer로 복귀합니다.</summary>
    public interface IInputLayerOverrideService : IServiceModule
    {
        IDisposable OverrideLayer(InputLayer layer);
    }
}
