using ProjectL.Systems.ServiceKeys.Runtime.InGame.Core;
using ProjectL.Framework.Composition;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.ItemSystem;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.StageSystem;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.ShopSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.ArtifactSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.RecoverySystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.RunSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.StageCommandSystem;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Recovery;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.Development;
#endif

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem
{
    /// <summary>InGame/Core 아래의 규칙·데이터 처리 시스템 그룹입니다.</summary>
    #region 서비스

    public interface IInGameCoreSystemGroupService : IServiceModule
    {
        IRecoveryCommandProcessingService RecoveryCommands { get; }
        IRecoveryStateReadSink RecoveryState { get; }
        IStageCommandProcessingService StageCommands { get; }
        IStageGeneratorSystem StageGeneratorSystem { get; }
        IStageServiceManager StageServiceManager { get; }
        IChapterProgressionCoordinator ChapterProgression { get; }
        IItemInventoryService Items { get; }
        ICharacterRosterService Characters { get; }
        ITraitGrowthCommandProcessingService TraitGrowthCommands { get; }
        /// <summary>현재 Run 보유 유닛의 전투 외 능력치 조회다. 매 프레임 호출하지 않는다.</summary>
        ICharacterStatsReadSink CharacterStats { get; }
        IRunDeckService Deck { get; }
        IShopService Shop { get; }
        IRecoveryStageService Recovery { get; }
        /// <summary>전투 스테이지 진입 판단과 준비 게시다.</summary>
        IBattleEntryService BattleEntry { get; }
        /// <summary>View가 제출한 다음 전투 명령 처리다.</summary>
        ICombatCommandService CombatCommands { get; }
        /// <summary>자동 진행 국면 처리다.</summary>
        ICombatAdvanceService CombatAdvance { get; }
        /// <summary>최신 미리보기 요청 응답이다.</summary>
        ICombatPreviewService CombatPreview { get; }
        /// <summary>현재 전투의 턴 순서 조회다. 상태를 바꾸지 않는다.</summary>
        /// <summary>전투 포기와 완료 후 정리다.</summary>
        IBattleCompletionService BattleCompletion { get; }
        IArtifactEditingService Artifacts { get; }
        IArtifactCommandProcessingService ArtifactCommands { get; }
        /// <summary>새 Run 시작(이전 Run 정리·시작 로스터·시작 Run 재화)이다.</summary>
        IRunStartService RunStart { get; }
        IResultQueryService Results { get; }
    }

    #endregion

    #region 시스템 모듈

    internal sealed class InGameCoreSystemGroup :
        SystemGroup,
        IInGameCoreSystemGroupService,
        ISystemInitData,
        ISystemPostBindService,
        ISystemTick,
        ISystemShutdown
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        , IDevelopmentStateProvider
#endif
    {
        public IStageCommandProcessingService StageCommands { get; }
        public IStageGeneratorSystem StageGeneratorSystem { get; }
        public IStageServiceManager StageServiceManager { get; }
        public IChapterProgressionCoordinator ChapterProgression { get; }
        public IItemInventoryService Items { get; }
        public ICharacterRosterService Characters { get; }
        public ITraitGrowthCommandProcessingService TraitGrowthCommands { get; }
        public ICharacterStatsReadSink CharacterStats { get; }
        public IRunDeckService Deck { get; }
        public IShopService Shop { get; }
        public IRecoveryStageService Recovery { get; }
        public IRecoveryCommandProcessingService RecoveryCommands { get; }
        public IRecoveryStateReadSink RecoveryState { get; }
        public IBattleEntryService BattleEntry { get; }
        public ICombatCommandService CombatCommands { get; }
        public ICombatAdvanceService CombatAdvance { get; }
        public ICombatPreviewService CombatPreview { get; }
        public IBattleCompletionService BattleCompletion { get; }
        public IArtifactEditingService Artifacts { get; }
        public IArtifactCommandProcessingService ArtifactCommands { get; }
        public IRunStartService RunStart { get; }
        public IResultQueryService Results { get; }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public IDevelopmentStateCommands DevelopmentState { get; }
#endif

        internal InGameCoreSystemGroup(ISystemReader systemReader
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            , IDevelopmentStateCommands developmentState = null
#endif
            ) : base(systemReader)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            DevelopmentState = developmentState;
#endif
            StageCommands = systemReader.ServiceStoreReader.GetService(InGameCoreSystemServiceKey.StageCommands);
            StageGeneratorSystem = systemReader.ServiceStoreReader.GetService(
                InGameCoreSystemServiceKey.StageGenerator);
            StageServiceManager = systemReader.ServiceStoreReader.GetService(
                InGameCoreSystemServiceKey.StageServiceManager);
            ChapterProgression = systemReader.ServiceStoreReader.GetService(
                InGameCoreSystemServiceKey.ChapterProgression);
            Items = systemReader.ServiceStoreReader.GetService(InGameCoreSystemServiceKey.Items);
            Characters = systemReader.ServiceStoreReader.GetService(InGameCoreSystemServiceKey.Characters);
            TraitGrowthCommands = systemReader.ServiceStoreReader.GetService(InGameCoreSystemServiceKey.TraitGrowthCommands);
            CharacterStats = systemReader.ServiceStoreReader.GetService(InGameCoreSystemServiceKey.CharacterStats);
            Deck = systemReader.ServiceStoreReader.GetService(InGameCoreSystemServiceKey.Deck);
            Shop = systemReader.ServiceStoreReader.GetService(InGameCoreSystemServiceKey.Shop);
            Recovery = systemReader.ServiceStoreReader.GetService(InGameCoreSystemServiceKey.Recovery);
            RecoveryState = systemReader.ServiceStoreReader.GetService(InGameCoreSystemServiceKey.RecoveryState);
            RecoveryCommands = systemReader.ServiceStoreReader.GetService(InGameCoreSystemServiceKey.RecoveryCommands);
            BattleEntry = systemReader.ServiceStoreReader.GetService(InGameCoreSystemServiceKey.BattleEntry);
            CombatCommands = systemReader.ServiceStoreReader.GetService(InGameCoreSystemServiceKey.CombatCommands);
            CombatAdvance = systemReader.ServiceStoreReader.GetService(InGameCoreSystemServiceKey.CombatAdvance);
            CombatPreview = systemReader.ServiceStoreReader.GetService(InGameCoreSystemServiceKey.CombatPreview);
            BattleCompletion = systemReader.ServiceStoreReader.GetService(InGameCoreSystemServiceKey.BattleCompletion);
            Artifacts = systemReader.ServiceStoreReader.GetService(InGameCoreSystemServiceKey.Artifacts);
            ArtifactCommands = systemReader.ServiceStoreReader.GetService(InGameCoreSystemServiceKey.ArtifactCommands);
            RunStart = systemReader.ServiceStoreReader.GetService(InGameCoreSystemServiceKey.RunStart);
            Results = systemReader.ServiceStoreReader.GetService(InGameCoreSystemServiceKey.Results);
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

        public void Tick(float deltaTime)
        {
            foreach (var module in SystemReader.SystemModuleStoreReader.TickModules)
                module.Tick(deltaTime);
        }

        public override void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<IInGameCoreSystemGroupService>(this);
            sink.AddModule<ISystemModule>(this);
        }
    }

    #endregion
}
