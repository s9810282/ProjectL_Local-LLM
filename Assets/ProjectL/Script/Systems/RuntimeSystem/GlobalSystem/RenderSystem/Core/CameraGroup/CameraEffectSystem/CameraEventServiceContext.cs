using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.CameraGroup.CameraCatalogSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UtilitySystem.DelaySystem.View;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.CameraGroup.CameraEffectSystem
{
    public sealed class CameraEventServiceContext
    {
        public ICameraCatalogService Catalog { get; }
        public IDelayService Delay { get; }

        public CameraEventServiceContext(ICameraCatalogService catalog, IDelayService delay)
        {
            Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            Delay = delay ?? throw new ArgumentNullException(nameof(delay));
        }
    }
}
