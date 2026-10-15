using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem
{
    #region 서비스

    public interface IInGameViewSystemGroupService : IServiceModule
    {
        ICombatViewService Combat { get; }
    }

    #endregion

    #region 시스템 모듈

    internal sealed class InGameViewSystemGroup :
        SystemGroup,
        IInGameViewSystemGroupService,
        ISystemBindScene,
        ISystemInitData,
        ISystemPostBindService,
        ISystemInitialize,
        ISystemTick,
        ISystemShutdown
    {
        public ICombatViewService Combat { get; }
        public InGameViewSystemGroup(ISystemReader systemReader, ICombatViewService combat) : base(systemReader)
        {
            Combat = combat;
        }

        public void BindScene()
        {
            foreach (var module in SystemReader.SystemModuleStoreReader.BindSceneModules)
                module.BindScene();
        }

        public void InitData(IServiceModule query)
        {
            foreach (var module in SystemReader.SystemModuleStoreReader.InitDataModules)
                module.InitData(query);
        }

        public void PostBindService(ISystemServiceStoreReader resolver)
        {
            foreach (var module in SystemReader.SystemModuleStoreReader.PostBindServiceModules)
                module.PostBindService(resolver);
        }

        public void Initialize()
        {
            foreach (var module in SystemReader.SystemModuleStoreReader.InitializeModules)
                module.Initialize();
        }

        public void Tick(float deltaTime)
        {
            foreach (var module in SystemReader.SystemModuleStoreReader.TickModules)
                module.Tick(deltaTime);
        }

        public void Shutdown()
        {
            var modules = SystemReader.SystemModuleStoreReader.ShutdownModules;
            for (int i = modules.Count - 1; i >= 0; i--)
                modules[i].Shutdown();
        }

        public override void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<IInGameViewSystemGroupService>(this);
            sink.AddModule<ISystemModule>(this);
        }
    }

    #endregion
}
