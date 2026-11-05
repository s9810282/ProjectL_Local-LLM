using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.CameraGroup;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.CameraGroup.CameraCatalogSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.CameraGroup.CameraEffectSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.CameraGroup.UnityCameraCatalogSystem;

namespace ProjectL.Systems.Factory.Runtime.Global
{
    internal static class CameraGroupFactory
    {
        internal static IExportSystemServices Create()
        {
            var builder = new SystemBuilderHelper();
            new UnityCameraCatalogSystem().ExportServices(builder);
            var catalog = new CameraCatalogSystem();
            catalog.ExportServices(builder);
            new CameraEffectSystem(catalog).ExportServices(builder);
            return new CameraGroup(builder.BuildSystemReader());
        }
    }
}
