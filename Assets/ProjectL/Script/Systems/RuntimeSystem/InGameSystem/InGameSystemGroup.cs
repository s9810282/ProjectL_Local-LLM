using ProjectL.Framework.Composition;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.ServiceKeys;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem
{
    /// <summary>인게임 Core와 View를 함께 소유하는 최상위 인게임 그룹입니다.</summary>
    #region 서비스

    public interface IInGameSystemGroupService : IServiceModule
    {
        IInGameCoreSystemGroupService CoreGroup { get; }
        IInGameViewSystemGroupService ViewGroup { get; }
    }

    #endregion

    #region 시스템 모듈

    internal sealed class InGameSystemGroup :
        SystemGroup,
        IInGameSystemGroupService,
        ISystemBindScene,
        ISystemInitData,
        ISystemPostBindService,
        ISystemInitialize,
        ISystemTick,
        ISystemShutdown
    {
        public IInGameCoreSystemGroupService CoreGroup { get; }
        public IInGameViewSystemGroupService ViewGroup { get; }

        internal InGameSystemGroup(ISystemReader systemReader) : base(systemReader)
        {
            CoreGroup = systemReader.ServiceStoreReader.GetService(
                InGameSystemGroupServiceKey.Core);
            ViewGroup = systemReader.ServiceStoreReader.GetService(
                InGameSystemGroupServiceKey.View);
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
            sink.AddService<IInGameSystemGroupService>(this);
            sink.AddModule<ISystemModule>(this);
        }
    }

    #endregion
}
