using ProjectL.Framework.Composition;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRegister;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRunner;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.ServiceKeys;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem
{
    #region 서비스

    public interface IJobSystemServiceContext : IServiceModule
    {
        IJobRunner JobRunner { get; }
        IJobRegisterSystem JobRegister { get; }
    }

    #endregion

    #region 시스템 모듈

    internal sealed class JobSystemGroup :
        SystemGroup, IJobSystemServiceContext,
        ISystemPostBindService, ISystemInitialize, ISystemTick, ISystemShutdown
    {
        public IJobRunner JobRunner { get; }
        public IJobRegisterSystem JobRegister { get; }

        internal JobSystemGroup(ISystemReader systemReader) : base(systemReader)
        {
            JobRunner = systemReader.ServiceStoreReader.GetService(JobSystemServiceKey.JobRunner);
            JobRegister = systemReader.ServiceStoreReader.GetService(JobSystemServiceKey.JobRegister);
        }

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
            sink.AddService<IJobSystemServiceContext>(this);
            sink.AddModule<ISystemModule>(this);
        }
    }

    #endregion
}
