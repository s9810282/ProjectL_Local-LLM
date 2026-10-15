using ProjectL.Systems.RuntimeSystem.GlobalSystem.AudioSystem;
using ProjectL.Framework.Composition;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.LoadingSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UtilitySystem;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.ServiceKeys;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem
{
    #region 서비스

    public interface IGlobalSystemGroupService : IServiceModule
    {
        IInputSystemServiceContext InputServiceCommand { get; }
        IRenderServiceContext RenderServiceCommand { get; }
        IUISystemService UIServiceCommand { get; }
        IUtilitySystemServiceContext UtilityServiceCommand { get; }
        ILodingSystemServiceContext LodingServiceCommand { get; }
        IJobSystemServiceContext JobServiceCommand { get; }
        IAudioSystemService AudioServiceCommand { get; }
    }

    #endregion

    #region 시스템 모듈

    internal sealed class GlobalSystemGroup :
        SystemGroup,
        IGlobalSystemGroupService,
        ISystemBindScene,
        ISystemInitData,
        ISystemPostBindService,
        ISystemInitialize,
        ISystemTick,
        ISystemShutdown
    {
        public IInputSystemServiceContext InputServiceCommand { get; }
        public IRenderServiceContext RenderServiceCommand { get; }
        public IUISystemService UIServiceCommand { get; }
        public IUtilitySystemServiceContext UtilityServiceCommand { get; }
        public ILodingSystemServiceContext LodingServiceCommand { get; }
        public IJobSystemServiceContext JobServiceCommand { get; }
        public IAudioSystemService AudioServiceCommand { get; }

        public GlobalSystemGroup(ISystemReader systemReader) : base(systemReader)
        {
            InputServiceCommand = systemReader.ServiceStoreReader.GetService(
                SystemGroupsService.Input);
            RenderServiceCommand = systemReader.ServiceStoreReader.GetService(
                SystemGroupsService.Render);
            UIServiceCommand = systemReader.ServiceStoreReader.GetService(
                SystemGroupsService.UI);
            UtilityServiceCommand = systemReader.ServiceStoreReader.GetService(
                SystemGroupsService.Utility);
            LodingServiceCommand = systemReader.ServiceStoreReader.GetService(
                SystemGroupsService.Loding);
            JobServiceCommand = systemReader.ServiceStoreReader.GetService(SystemGroupsService.Job);
            AudioServiceCommand = systemReader.ServiceStoreReader.GetService(AudioSystemServiceKey.Audio);
        }

        public void BindScene()
        {
            var modules = SystemReader.SystemModuleStoreReader.BindSceneModules;
            for (int i = 0; i < modules.Count; i++) modules[i].BindScene();
        }

        public void InitData(IServiceModule service)
        {
            var modules = SystemReader.SystemModuleStoreReader.InitDataModules;
            for (int i = 0; i < modules.Count; i++) modules[i].InitData(service);
        }

        public void PostBindService(ISystemServiceStoreReader systemServiceResolver)
        {
            var modules = SystemReader.SystemModuleStoreReader.PostBindServiceModules;
            for (int i = 0; i < modules.Count; i++) modules[i].PostBindService(systemServiceResolver);
        }

        public void Initialize()
        {
            var modules = SystemReader.SystemModuleStoreReader.InitializeModules;
            for (int i = 0; i < modules.Count; i++) modules[i].Initialize();
        }

        public void Tick(float deltaTime)
        {
            var modules = SystemReader.SystemModuleStoreReader.TickModules;
            for (int i = 0; i < modules.Count; i++) modules[i].Tick(deltaTime);
        }

        public void Shutdown()
        {
            var modules = SystemReader.SystemModuleStoreReader.ShutdownModules;
            for (int i = modules.Count - 1; i >= 0; i--) modules[i].Shutdown();
        }

        public override void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<IGlobalSystemGroupService>(this);
            sink.AddModule<ISystemModule>(this);
        }
    }

    #endregion
}
