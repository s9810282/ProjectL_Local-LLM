using ProjectL.Framework.Composition;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.ColorSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.EffectSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.LightSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.MeshSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.View.OutlineSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.View.AreaOutlineSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.RenderFeatureSystem;
using ProjectL.Framework.SystemBase;

namespace ProjectL.Systems.Factory.Runtime.Global
{
    #region 시스템 모듈

    public static class RenderSystemFactory
    {
        public static IExportSystemServices Create() 
        {
            SystemBuilderHelper systemBuilderHelper = new();
            var runtimeRoot = new RenderRuntimeRoot();

            CameraGroupFactory.Create().ExportServices(systemBuilderHelper);

            // 서비스 및 모듈 등록단계
            var effectSystem = new EffectSystem(runtimeRoot);
            effectSystem.ExportServices(systemBuilderHelper);

            var MeshSystem = new MeshSystem();
            MeshSystem.ExportServices(systemBuilderHelper);

            var ColorSystem = new ColorSystem();
            ColorSystem.ExportServices(systemBuilderHelper);

            var LightSystem = new LightSystem(runtimeRoot);
            LightSystem.ExportServices(systemBuilderHelper);

            var outlineSystem = new OutlineSystem(runtimeRoot);
            outlineSystem.ExportServices(systemBuilderHelper);

            var areaOutlineSystem = new AreaOutlineSystem(runtimeRoot);
            areaOutlineSystem.ExportServices(systemBuilderHelper);

            var renderFeatureSystem = new RenderFeatureSystem();
            renderFeatureSystem.ExportServices(systemBuilderHelper);

            RenderSystemGroup renderSystemGroup = new RenderSystemGroup(systemBuilderHelper.BuildSystemReader(), runtimeRoot);

            return renderSystemGroup;
        }
    }

    #endregion
}
