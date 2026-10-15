using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.CameraGroup;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.ColorSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.EffectSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.LightSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.MeshSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.RenderFeatureSystem;
using ProjectL.Systems.ServiceKeys;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core
{
    /// <summary>
    /// View 계층이 사용하는 공용 렌더 인프라 서비스입니다.
    /// </summary>
    public interface IRenderCoreServiceContext : IServiceModule
    {
        ICameraGroupServiceContext CameraGroup { get; }
        IEffectSystemServices EffectSystemServices { get; }
        IMeshSystemService MeshSystemServices { get; }
        IColorSystemService ColorSystemServices { get; }
        ILightSystemService LightSystemServices { get; }
        IRenderFeatureSystemService RenderFeatureSystemServices { get; }
    }

    internal sealed class RenderCoreGroup : IRenderCoreServiceContext
    {
        public ICameraGroupServiceContext CameraGroup { get; }
        public IEffectSystemServices EffectSystemServices { get; }
        public IMeshSystemService MeshSystemServices { get; }
        public IColorSystemService ColorSystemServices { get; }
        public ILightSystemService LightSystemServices { get; }
        public IRenderFeatureSystemService RenderFeatureSystemServices { get; }

        public RenderCoreGroup(ISystemReader systemReader)
        {
            CameraGroup = systemReader.ServiceStoreReader.GetService(RenderSystemServiceKey.CameraGroup);
            EffectSystemServices = systemReader.ServiceStoreReader.GetService(RenderSystemServiceKey.ParticleSystem);
            MeshSystemServices = systemReader.ServiceStoreReader.GetService(RenderSystemServiceKey.MeshSystem);
            ColorSystemServices = systemReader.ServiceStoreReader.GetService(RenderSystemServiceKey.ColorSystem);
            LightSystemServices = systemReader.ServiceStoreReader.GetService(RenderSystemServiceKey.LightSystem);
            RenderFeatureSystemServices = systemReader.ServiceStoreReader.GetService(RenderSystemServiceKey.RenderFeatureSystem);
        }
    }
}
