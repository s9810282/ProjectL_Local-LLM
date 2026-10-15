using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.ServiceKeys;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.CameraGroup.CameraCatalogSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.CameraGroup.CameraEffectSystem;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.CameraGroup
{
    public interface ICameraGroupServiceContext : IServiceModule
    {
        ICameraCatalogService CameraCatalogSystem { get; }
        ICameraEffectService CameraEffectSystem { get; }
    }

    /// <summary>서비스 묶음 및 모듈 수명 전달만 담당한다. 카메라 기능은 각 시스템에 있다.</summary>
    internal sealed class CameraGroup : SystemGroup, ICameraGroupServiceContext,
        ISystemBindScene, ISystemPostBindService, ISystemShutdown
    {
        public ICameraCatalogService CameraCatalogSystem { get; }
        public ICameraEffectService CameraEffectSystem { get; }

        internal CameraGroup(ISystemReader reader) : base(reader)
        {
            CameraCatalogSystem = reader.ServiceStoreReader.GetService(CameraSystemServiceKey.Catalog);
            CameraEffectSystem = reader.ServiceStoreReader.GetService(CameraSystemServiceKey.Effect);
        }

        public void BindScene()
        {
            var modules = SystemReader.SystemModuleStoreReader.BindSceneModules;
            for (int i = 0; i < modules.Count; i++) modules[i].BindScene();
        }

        public void PostBindService(ISystemServiceStoreReader resolver)
        {
            var modules = SystemReader.SystemModuleStoreReader.PostBindServiceModules;
            for (int i = 0; i < modules.Count; i++) modules[i].PostBindService(resolver);
        }

        public void Shutdown()
        {
            var modules = SystemReader.SystemModuleStoreReader.ShutdownModules;
            for (int i = modules.Count - 1; i >= 0; i--) modules[i].Shutdown();
        }

        public override void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<ICameraGroupServiceContext>(this);
            sink.AddModule(this);
        }
    }
}
