using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.ServiceKeys;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.LoadingSystem
{
    public interface ILodingSystemServiceContext : IServiceModule
    {
        ILodingRegisterService LodingRegisterService { get; }
        ILodingRegisterReader LodingRegisterReader { get; }
        ILodingSystemService LodingService { get; }
    }

    internal sealed class LodingSystemGroup :
        SystemGroup,
        ILodingSystemServiceContext,

        ISystemPostBindService,
        ISystemInitialize,
        ISystemTick,
        ISystemShutdown
    {
        #region 서비스

        public ILodingRegisterService LodingRegisterService { get; }
        public ILodingRegisterReader LodingRegisterReader { get; }
        public ILodingSystemService LodingService { get; }

        #endregion

        #region 시스템 구성

        public LodingSystemGroup(ISystemReader systemReader) : base(systemReader)
        {
            LodingRegisterService = systemReader.ServiceStoreReader.GetService(
                LodingSystemServiceKey.Register);
            LodingRegisterReader = systemReader.ServiceStoreReader.GetService(
                LodingSystemServiceKey.Reader);
            LodingService = systemReader.ServiceStoreReader.GetService(
                LodingSystemServiceKey.Loding);
        }

        #endregion

        #region 시스템 모듈

        public void PostBindService(ISystemServiceStoreReader resolver)
        {
            var modules = SystemReader.SystemModuleStoreReader.PostBindServiceModules;
            for (int i = 0; i < modules.Count; i++)
                modules[i].PostBindService(resolver);
        }

        public void Initialize()
        {
            var modules = SystemReader.SystemModuleStoreReader.InitializeModules;
            for (int i = 0; i < modules.Count; i++)
                modules[i].Initialize();
        }

        public void Tick(float deltaTime)
        {
            var modules = SystemReader.SystemModuleStoreReader.TickModules;
            for (int i = 0; i < modules.Count; i++)
                modules[i].Tick(deltaTime);
        }

        public void Shutdown()
        {
            var modules = SystemReader.SystemModuleStoreReader.ShutdownModules;
            for (int i = modules.Count - 1; i >= 0; i--)
                modules[i].Shutdown();
        }

        public override void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<ILodingSystemServiceContext>(this);
            sink.AddModule<ISystemModule>(this);
        }

        #endregion
    }
}
