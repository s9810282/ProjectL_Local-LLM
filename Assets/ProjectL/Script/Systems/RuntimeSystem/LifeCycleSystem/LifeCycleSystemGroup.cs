using ProjectL.Framework.Composition;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.Base;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat;
using ProjectL.Systems.ServiceKeys;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem
{
    #region 서비스

    /// <summary>게임 실행 순서에 반응하는 시스템을 담는 전용 그룹입니다.</summary>
    public interface ILifeCycleSystemGroupService : IServiceModule
    {
        ILifeCycleManager LifeCycleManager { get; }
        ICombatEntryService CombatEntry { get; }
        ICombatExitService CombatExit { get; }

        /// <summary>전투 진행 판단에 쓰는 사실을 읽는 유일한 자리입니다(재구성 설계 J13·D5).</summary>
        ICombatProgressReader CombatProgress { get; }
    }

    #endregion

    #region 시스템 모듈

    internal sealed class LifeCycleSystemGroup :
        SystemGroup,
        ILifeCycleSystemGroupService,
        ISystemBindScene,
        ISystemInitData,
        ISystemPostBindService,
        ISystemInitialize,
        ISystemTick,
        ISystemShutdown
    {
        public ILifeCycleManager LifeCycleManager { get; }
        public ICombatEntryService CombatEntry { get; }
        public ICombatExitService CombatExit { get; }
        public ICombatProgressReader CombatProgress { get; }

        internal LifeCycleSystemGroup(ISystemReader systemReader) : base(systemReader)
        {
            LifeCycleManager = systemReader.ServiceStoreReader.GetService(
                LifeCycleSystemServiceKey.Manager);
            CombatEntry = systemReader.ServiceStoreReader.GetService(LifeCycleSystemServiceKey.CombatEntry);
            CombatExit = systemReader.ServiceStoreReader.GetService(LifeCycleSystemServiceKey.CombatExit);
            CombatProgress = systemReader.ServiceStoreReader.GetService(LifeCycleSystemServiceKey.CombatProgress);
        }

        public void BindScene()
        {
            foreach (var module in SystemReader.SystemModuleStoreReader.BindSceneModules)
                module.BindScene();
        }

        public void InitData(IServiceModule data)
        {
            foreach (var module in SystemReader.SystemModuleStoreReader.InitDataModules)
                module.InitData(data);
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
            sink.AddService<ILifeCycleSystemGroupService>(this);
            sink.AddModule<ISystemModule>(this);
        }
    }
    #endregion
}
