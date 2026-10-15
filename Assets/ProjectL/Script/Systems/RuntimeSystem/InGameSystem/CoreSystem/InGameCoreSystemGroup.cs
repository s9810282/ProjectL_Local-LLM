using ProjectL.Framework.Composition;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.StageSystem;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.ServiceKeys;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.ShopSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.ArtifactSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.RecoverySystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.RunSystem;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem
{
    /// <summary>InGame/Core 아래의 규칙·데이터 처리 시스템 그룹입니다.</summary>
    #region 서비스

    public interface IInGameCoreSystemGroupService : IServiceModule
    {
        IStageGeneratorSystem StageGeneratorSystem { get; }
        IStageServiceManager StageServiceManager { get; }
        IChapterProgressionCoordinator ChapterProgression { get; }
        ICharacterRosterService Characters { get; }
        ICharacterShopService Shop { get; }
        IRecoveryStageService Recovery { get; }
        ICombatProcessingService Combat { get; }
        IArtifactEditingService Artifacts { get; }
        /// <summary>새 Run 시작(이전 Run 정리·시작 로스터·시작 Run 재화)이다.</summary>
        IRunStartService RunStart { get; }
    }

    #endregion

    #region 시스템 모듈

    internal sealed class InGameCoreSystemGroup :
        SystemGroup,
        IInGameCoreSystemGroupService,
        ISystemInitData,
        ISystemPostBindService,
        ISystemShutdown
    {
        public IStageGeneratorSystem StageGeneratorSystem { get; }
        public IStageServiceManager StageServiceManager { get; }
        public IChapterProgressionCoordinator ChapterProgression { get; }
        public ICharacterRosterService Characters { get; }
        public ICharacterShopService Shop { get; }
        public IRecoveryStageService Recovery { get; }
        public ICombatProcessingService Combat { get; }
        public IArtifactEditingService Artifacts { get; }
        public IRunStartService RunStart { get; }

        internal InGameCoreSystemGroup(ISystemReader systemReader) : base(systemReader)
        {
            StageGeneratorSystem = systemReader.ServiceStoreReader.GetService(
                InGameSystemServiceKey.StageGenerator);
            StageServiceManager = systemReader.ServiceStoreReader.GetService(
                InGameSystemServiceKey.StageServiceManager);
            ChapterProgression = systemReader.ServiceStoreReader.GetService(
                InGameSystemServiceKey.ChapterProgression);
            Characters = systemReader.ServiceStoreReader.GetService(InGameSystemServiceKey.Characters);
            Shop = systemReader.ServiceStoreReader.GetService(InGameSystemServiceKey.Shop);
            Recovery = systemReader.ServiceStoreReader.GetService(InGameSystemServiceKey.Recovery);
            Combat = systemReader.ServiceStoreReader.GetService(InGameSystemServiceKey.Combat);
            Artifacts = systemReader.ServiceStoreReader.GetService(InGameSystemServiceKey.Artifacts);
            RunStart = systemReader.ServiceStoreReader.GetService(InGameSystemServiceKey.RunStart);
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

        public void Shutdown()
        {
            var modules = SystemReader.SystemModuleStoreReader.ShutdownModules;
            for (int i = modules.Count - 1; i >= 0; i--)
                modules[i].Shutdown();
        }

        public override void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<IInGameCoreSystemGroupService>(this);
            sink.AddModule<ISystemModule>(this);
        }
    }

    #endregion
}
