using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.CameraGroup;

namespace ProjectL.Systems.ServiceKeys.Runtime.Global.Render
{
    internal static class RenderSystemGroupServiceKey
    {
        public static readonly SystemGroupKey<ICameraGroupServiceContext> CameraGroup =
            new(nameof(CameraGroup));
    }
}
