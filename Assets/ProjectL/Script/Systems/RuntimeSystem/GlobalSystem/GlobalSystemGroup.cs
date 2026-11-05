using ProjectL.Systems.ServiceKeys.Runtime.Global;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.AudioSystem;
using ProjectL.Framework.Composition;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.View;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.LoadingSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UtilitySystem;
using ProjectL.Framework.SystemBase;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem
{
    #region 서비스

    public interface IGlobalSystemGroupService : IServiceModule
    {
        IInputSystemServiceContext InputServiceCommand { get; }
        IOptionCloseRequestService OptionCloseRequests { get; }
        IRenderServiceContext RenderServiceCommand { get; }
        IUISystemService UIServiceCommand { get; }
        IGlobalUIPresentation GlobalUIPresentation { get; }
        IUITextPresentation UITextPresentation { get; }
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
        public IOptionCloseRequestService OptionCloseRequests { get; }
        public IRenderServiceContext RenderServiceCommand { get; }
        public IUISystemService UIServiceCommand { get; }
        public IGlobalUIPresentation GlobalUIPresentation { get; }
        public IUITextPresentation UITextPresentation { get; }
        public IUtilitySystemServiceContext UtilityServiceCommand { get; }
        public ILodingSystemServiceContext LodingServiceCommand { get; }
        public IJobSystemServiceContext JobServiceCommand { get; }
        public IAudioSystemService AudioServiceCommand { get; }

        public GlobalSystemGroup(ISystemReader systemReader) : base(systemReader)
        {
            InputServiceCommand = systemReader.ServiceStoreReader.GetService(
                GlobalSystemGroupServiceKey.Input);
            OptionCloseRequests = systemReader.ServiceStoreReader.GetService(InputSystemServiceKey.OptionClose);
            RenderServiceCommand = systemReader.ServiceStoreReader.GetService(
                GlobalSystemGroupServiceKey.Render);
            UIServiceCommand = systemReader.ServiceStoreReader.GetService(
                GlobalSystemServiceKey.UI);
            GlobalUIPresentation = systemReader.ServiceStoreReader.GetService(GlobalSystemServiceKey.GlobalUIPresentation);
            UITextPresentation = systemReader.ServiceStoreReader.GetService(GlobalSystemServiceKey.UITextPresentation);
            UtilityServiceCommand = systemReader.ServiceStoreReader.GetService(
                GlobalSystemGroupServiceKey.Utility);
            LodingServiceCommand = systemReader.ServiceStoreReader.GetService(
                GlobalSystemGroupServiceKey.Loding);
            JobServiceCommand = systemReader.ServiceStoreReader.GetService(GlobalSystemGroupServiceKey.Job);
            AudioServiceCommand = systemReader.ServiceStoreReader.GetService(GlobalSystemServiceKey.Audio);
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
