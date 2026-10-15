using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.CameraGroup;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.CameraGroup.CameraCatalogSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.CameraGroup.CameraEffectSystem;

namespace ProjectL.Systems.Factory.Runtime.Global
{
    internal static class CameraGroupFactory
    {
        internal static IExportSystemServices Create()
        {
            var builder = new SystemBuilderHelper();
            var catalog = new CameraCatalogSystem();
            catalog.ExportServices(builder);
            new CameraEffectSystem(catalog).ExportServices(builder);
            return new CameraGroup(builder.BuildSystemReader());
        }
    }
}
