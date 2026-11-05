using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.CameraGroup.CameraCatalogSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.CameraGroup.CameraEffectSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.CameraGroup.UnityCameraCatalogSystem;

namespace ProjectL.Systems.ServiceKeys.Runtime.Global.Render
{
    internal static class CameraSystemServiceKey
    {
        public static readonly SystemGroupKey<ICameraCatalogService> Catalog = new(nameof(Catalog));

        public static readonly SystemGroupKey<IUnityCameraCatalogService> UnityCatalog = new(nameof(UnityCatalog));

        public static readonly SystemGroupKey<ICameraEffectService> Effect = new(nameof(Effect));
    }
}
