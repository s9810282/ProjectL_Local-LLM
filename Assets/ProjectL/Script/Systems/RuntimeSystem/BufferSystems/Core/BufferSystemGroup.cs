using ProjectL.Systems.RuntimeSystem.BufferSystems.TraitGrowthView;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Deck;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Shop;
using ProjectL.Systems.ServiceKeys.Runtime.Buffer;
using ProjectL.Systems.RuntimeSystem.BufferSystems.StageView;
using ProjectL.Systems.RuntimeSystem.BufferSystems.ArtifactView;
using ProjectL.Systems.RuntimeSystem.BufferSystems.ShopView;
using ProjectL.Systems.RuntimeSystem.BufferSystems.RecoveryView;
using ProjectL.Systems.RuntimeSystem.BufferSystems.LobbyView;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Framework.Composition;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Artifact;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Currency;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Fame;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Current;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Map;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Run;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Tutorial;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;


namespace ProjectL.Systems.RuntimeSystem.BufferSystems.Core
{
    #region 서비스

    public interface IRunDeckWriteSink : IServiceModule
    {
        DeckFailureReason TryPublish(DeckEditRequest expected, IReadOnlyList<CharacterInstanceId> canonicalIds);
    }

    public interface IRunProgressBufferWriteSink : IServiceModule
    {
        /// <summary>
        /// 새 Run을 위해 임시 진행 데이터를 비우고 맵 등록과 영구 Artifact 보유 기록은 유지한다.
        /// 명성 잔액과 마지막 Run 요약(<c>LastSummary</c>)도 유지하고, 이번 Run 합계(쓴 골드·획득 명성·처치 수)는 0으로 돌린다.
        /// 로스터와 Run 재화도 비우므로 이것만으로는 플레이 가능한 Run이 아니다. 새 Run 시작은 PublishNewRun을 쓴다.
        /// </summary>
        void ResetRunProgress();

        /// <summary>
        /// 이전 Run을 비우고 새 Run의 로스터와 시작 Run 재화를 한 번에 설치한다. 준비가 실패하면 아무것도 바꾸지 않는다.
        /// 이미 설치된 Run ID의 재게시는 거절한다. 다음 Run은 Core가 새 ID로 준비하며 챕터 진입은 호출하는 흐름이 이어서 한다.
        /// </summary>
        RosterAcquireResult PublishNewRun(NewRunPreparation preparation);
    }

    public interface IBattleBufferWriteSink : IServiceModule
    {
        void PrepareBattle(BattlePreparation preparation);
        void PrepareBattle(BattlePreparation preparation, BattleEntrySelection selection);
        void Apply(CharacterChanges changes);
        void Apply(SoulActionChanges changes);
        void Apply(CombatCommandTransition transition);
        void PublishPreview(CombatPreviewResult preview);
        bool CompleteBattle(BattleCompletionChanges changes);
        void EndBattle();
    }

    /// <summary>회복 스테이지의 진입과 선택지 소비를 게시한다.</summary>
    public interface IRecoveryStageBufferWriteSink : IServiceModule
    {
        void PrepareRecovery(RecoveryEntryPreparation preparation);
        bool PublishRecovery(StageNodeId nodeId, StageKey stageKey, CharacterRecoveryDecision decision);
    }

    public interface IShopPurchaseBufferWriteSink : IServiceModule
    {
        ShopPurchaseResult PublishPurchase(ShopPurchasePreparation preparation);
        ShopPurchaseResult PublishTraining(TrainingPurchasePreparation preparation);
    }

    public interface IShopStageBufferWriteSink : IServiceModule
    {
        void PrepareShop(ShopEntryPreparation preparation);
    }

    public interface IChapterContinuationBufferWriteSink : IServiceModule
    {
        void PublishContinuation(ChapterContinuationPreparation preparation);
    }

    /// <summary>챕터 진입과 비전투 노드 완료를 원본 비교 뒤 설치한다. 진입 가능·완료 가능 판정은 Core가 소유한다.</summary>
    public interface IChapterProgressBufferWriteSink : IServiceModule
    {
        void PublishChapterEntry(ChapterEntryPreparation preparation);
        void PublishStageClear(int chapterKey, StageNodeId nodeId, StageKey stageKey);
    }

    /// <summary>진행 중인 Run에 재고 지급을 기대값 비교 뒤 설치한다. 지급 가능 판정과 수량 계산은 Core가 소유한다.</summary>
    public interface IItemInventoryBufferWriteSink : IServiceModule
    {
        void PublishGrant(ItemInventoryChange change);
    }

    internal interface IBufferQuery : IServiceModule
    {
        IMainSpecializationTransaction MainSpecializationOperations { get; }
        ITraitGrowthStateReadSink TraitGrowthStateReadSink
        {
            get;
        }
        ITraitGrowthResultReadSink TraitGrowthResultReadSink
        {
            get;
        }
        ITraitGrowthViewReadSink TraitGrowthViewReadSink
        {
            get;
        }
        ITraitGrowthViewWriteSink TraitGrowthViewWriteSink
        {
            get;
        }
        ITraitGrowthTransaction TraitGrowthOperations
        {
            get;
        }
        ProjectL.Systems.RuntimeSystem.BufferSystems.Recovery.IRecoveryResultReadSink RecoveryResultReadSink
        {
            get;
        }
        IRecoveryOperationTransaction RecoveryOperations
        {
            get;
        }
        IStageCommandResultReadSink StageCommandResults
        {
            get;
        }
        IStageCommandResultStore StageCommandResultStore
        {
            get;
        }
        IChapterContinuationBufferWriteSink ChapterContinuationWriteSink
        {
            get;
        }
        IChapterProgressBufferWriteSink ChapterProgressWriteSink
        {
            get;
        }
        IItemInventoryReadSink ItemInventoryReadSink
        {
            get;
        }
        IItemInventoryBufferWriteSink ItemInventoryWriteSink
        {
            get;
        }
        IRunProgressBufferReadSink RunProgressReadSink
        {
            get;
        }
        IRunDeckReadSink RunDeckReadSink
        {
            get;
        }
        ICharacterStatsReadSink CharacterStatsReadSink
        {
            get;
        }
        ICharacterStatsRequestSink CharacterStatsRequestSink
        {
            get;
        }
        ICharacterStatsStore CharacterStatsStore
        {
            get;
        }
        IRunDeckWriteSink RunDeckWriteSink
        {
            get;
        }
        IRecoveryStageBufferWriteSink RecoveryStageWriteSink
        {
            get;
        }
        IShopStageBufferWriteSink ShopStageWriteSink
        {
            get;
        }
        IShopPurchaseBufferWriteSink ShopPurchaseWriteSink
        {
            get;
        }
        IRunProgressBufferWriteSink RunProgressWriteSink
        {
            get;
        }
        IBattleBufferWriteSink BattleWriteSink
        {
            get;
        }
        ICurrentProgressBufferReadSink CurrentProgressReadSink
        {
            get;
        }
        ICurrentProgressBufferWriteSink CurrentProgressWriteSink
        {
            get;
        }
        IMapProgressBufferReadSink MapProgressReadSink
        {
            get;
        }
        IMapProgressBufferWriteSink MapProgressWriteSink
        {
            get;
        }
        IArtifactBufferReadSink ArtifactReadSink
        {
            get;
        }
        IArtifactCommandResultReadSink ArtifactCommandResults
        {
            get;
        }
        IArtifactCommandResultStore ArtifactCommandResultStore
        {
            get;
        }
        IArtifactInitializationWriteSink ArtifactInitializationWriteSink
        {
            get;
        }
        IArtifactEditWriteSink ArtifactEditWriteSink
        {
            get;
        }
        IArtifactPreviewWriteSink ArtifactPreviewWriteSink
        {
            get;
        }
        IArtifactEditResultReadSink ArtifactEditResultReadSink
        {
            get;
        }
        IArtifactPreviewReadSink ArtifactPreviewReadSink
        {
            get;
        }
        ICharacterBufferReadSink CharacterReadSink
        {
            get;
        }
        ICharacterBufferWriteSink CharacterWriteSink
        {
            get;
        }
        ICombatStateBufferReadSink CombatStateReadSink
        {
            get;
        }
        ICombatResultReadSink CombatResultReadSink
        {
            get;
        }
        ICombatResultHistoryReadSink CombatResultHistoryReadSink
        {
            get;
        }
        IFameBufferReadSink FameReadSink
        {
            get;
        }
        ICurrencyBufferReadSink CurrencyReadSink
        {
            get;
        }
        ICurrencyBufferWriteSink CurrencyWriteSink
        {
            get;
        }
        IStageProgressBufferReadSink StageProgressReadSink
        {
            get;
        }
        IStageProgressBufferWriteSink StageProgressWriteSink
        {
            get;
        }
        ICombatViewBufferReadSink CombatViewReadSink
        {
            get;
        }
        ICombatViewBufferWriteSink CombatViewWriteSink
        {
            get;
        }
        IStageViewBufferReadSink StageViewReadSink
        {
            get;
        }
        IStageViewBufferWriteSink StageViewWriteSink
        {
            get;
        }
        IArtifactViewBufferReadSink ArtifactViewReadSink
        {
            get;
        }
        IArtifactViewBufferWriteSink ArtifactViewWriteSink
        {
            get;
        }
        IShopViewBufferReadSink ShopViewReadSink
        {
            get;
        }
        IShopViewBufferWriteSink ShopViewWriteSink
        {
            get;
        }
        IRecoveryViewBufferReadSink RecoveryViewReadSink
        {
            get;
        }
        IRecoveryViewBufferWriteSink RecoveryViewWriteSink
        {
            get;
        }
        ILobbyViewBufferReadSink LobbyViewReadSink
        {
            get;
        }
        ILobbyViewBufferWriteSink LobbyViewWriteSink
        {
            get;
        }
        ITutorialRequestBufferWriteSink TutorialRequestWriteSink
        {
            get;
        }
        ITutorialResultBufferReadSink TutorialResultReadSink
        {
            get;
        }
        ITutorialStateBufferReadSink TutorialStateReadSink
        {
            get;
        }
        ITutorialBufferStore TutorialStore
        {
            get;
        }
    }

    #endregion

    #region 시스템 모듈

    /// <summary>
    /// 하위 Buffer 모듈을 묶는 Group이다. 초기화 데이터를 하위 모듈에 전달하고, 하위 Buffer 서비스와
    /// 저장소 간 거래(전투·진행·비전투 스테이지)를 IBufferQuery 하나로 노출한다.
    /// 거래 본문은 이 클래스에 두지 않는다. 새 거래는 맞는 거래 클래스에 넣거나 새 클래스로 나눈다.
    /// </summary>
    internal sealed class BufferSystemGroup : SystemGroup, IBufferQuery, ISystemInitData<IDataBaseQueryContext>
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        , IDevelopmentBufferProvider
#endif
    {
        public ITraitGrowthStateReadSink TraitGrowthStateReadSink { get; }
        public ITraitGrowthResultReadSink TraitGrowthResultReadSink { get; }
        public ITraitGrowthViewReadSink TraitGrowthViewReadSink { get; }
        public ITraitGrowthViewWriteSink TraitGrowthViewWriteSink { get; }
        public ITraitGrowthTransaction TraitGrowthOperations { get; }
        public IMainSpecializationTransaction MainSpecializationOperations { get; }
        public IStageCommandResultReadSink StageCommandResults { get; }
        public IStageCommandResultStore StageCommandResultStore { get; }
        private readonly BattleTransactions _battle;
        private readonly ProgressTransactions _progress;
        private readonly StageServiceTransactions _stageServices;
        private readonly ShopTransactions _shop;
        private readonly ItemInventoryTransactions _items;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public IDevelopmentBufferWriteSink DevelopmentWriteSink { get; }
#endif

        public IItemInventoryReadSink ItemInventoryReadSink { get; }
        public IItemInventoryBufferWriteSink ItemInventoryWriteSink => _items;
        public IRunProgressBufferReadSink RunProgressReadSink { get; }
        public IRunDeckReadSink RunDeckReadSink { get; }
        public ICharacterStatsReadSink CharacterStatsReadSink { get; }
        public ICharacterStatsRequestSink CharacterStatsRequestSink { get; }
        public ICharacterStatsStore CharacterStatsStore { get; }
        public IRunDeckWriteSink RunDeckWriteSink { get; }
        public IChapterContinuationBufferWriteSink ChapterContinuationWriteSink => _progress;
        public IChapterProgressBufferWriteSink ChapterProgressWriteSink => _progress;
        public IRunProgressBufferWriteSink RunProgressWriteSink => _progress;
        public IRecoveryStageBufferWriteSink RecoveryStageWriteSink => _stageServices;
        public ProjectL.Systems.RuntimeSystem.BufferSystems.Recovery.IRecoveryResultReadSink RecoveryResultReadSink { get; }
        public IRecoveryOperationTransaction RecoveryOperations => _stageServices;
        public IShopStageBufferWriteSink ShopStageWriteSink => _shop;
        public IShopPurchaseBufferWriteSink ShopPurchaseWriteSink => _shop;
        public IBattleBufferWriteSink BattleWriteSink => _battle;
        public ICurrentProgressBufferReadSink CurrentProgressReadSink { get; }
        public ICurrentProgressBufferWriteSink CurrentProgressWriteSink { get; }
        public IMapProgressBufferReadSink MapProgressReadSink { get; }
        public IMapProgressBufferWriteSink MapProgressWriteSink { get; }
        public IArtifactBufferReadSink ArtifactReadSink { get; }
        public IArtifactCommandResultReadSink ArtifactCommandResults { get; }
        public IArtifactCommandResultStore ArtifactCommandResultStore { get; }
        public IArtifactInitializationWriteSink ArtifactInitializationWriteSink { get; }
        public IArtifactEditWriteSink ArtifactEditWriteSink { get; }
        public IArtifactPreviewWriteSink ArtifactPreviewWriteSink { get; }
        public IArtifactEditResultReadSink ArtifactEditResultReadSink { get; }
        public IArtifactPreviewReadSink ArtifactPreviewReadSink { get; }
        public ICharacterBufferReadSink CharacterReadSink { get; }
        public ICharacterBufferWriteSink CharacterWriteSink { get; }
        public ICombatStateBufferReadSink CombatStateReadSink { get; }
        public ICombatResultReadSink CombatResultReadSink { get; }
        public ICombatResultHistoryReadSink CombatResultHistoryReadSink { get; }
        public IFameBufferReadSink FameReadSink { get; }
        public ICurrencyBufferReadSink CurrencyReadSink { get; }
        public ICurrencyBufferWriteSink CurrencyWriteSink { get; }
        public IStageProgressBufferReadSink StageProgressReadSink { get; }
        public IStageProgressBufferWriteSink StageProgressWriteSink { get; }
        public ICombatViewBufferReadSink CombatViewReadSink { get; }
        public ICombatViewBufferWriteSink CombatViewWriteSink { get; }
        public IStageViewBufferReadSink StageViewReadSink { get; }
        public IStageViewBufferWriteSink StageViewWriteSink { get; }
        public IArtifactViewBufferReadSink ArtifactViewReadSink { get; }
        public IArtifactViewBufferWriteSink ArtifactViewWriteSink { get; }
        public IShopViewBufferReadSink ShopViewReadSink { get; }
        public IShopViewBufferWriteSink ShopViewWriteSink { get; }
        public IRecoveryViewBufferReadSink RecoveryViewReadSink { get; }
        public IRecoveryViewBufferWriteSink RecoveryViewWriteSink { get; }
        public ILobbyViewBufferReadSink LobbyViewReadSink { get; }
        public ILobbyViewBufferWriteSink LobbyViewWriteSink { get; }
        public ITutorialRequestBufferWriteSink TutorialRequestWriteSink { get; }
        public ITutorialResultBufferReadSink TutorialResultReadSink { get; }
        public ITutorialStateBufferReadSink TutorialStateReadSink { get; }
        public ITutorialBufferStore TutorialStore { get; }

        public BufferSystemGroup(ISystemReader systemReader) : base(systemReader)
        {
            var services = systemReader.ServiceStoreReader;
            StageCommandResults = services.GetService(CurrentProgressBufferKey.CommandResults);
            StageCommandResultStore = services.GetService(CurrentProgressBufferKey.CommandResultStore);
            RunProgressReadSink = services.GetService(RunProgressBufferKey.Read);
            RunDeckReadSink = services.GetService(RunDeckBufferKey.Read);
            ItemInventoryReadSink = services.GetService(ItemInventoryBufferKey.Read);
            CurrentProgressReadSink = services.GetService(CurrentProgressBufferKey.Read);
            CurrentProgressWriteSink = services.GetService(CurrentProgressBufferKey.Write);
            MapProgressReadSink = services.GetService(MapProgressBufferKey.Read);
            MapProgressWriteSink = services.GetService(MapProgressBufferKey.Write);
            ArtifactReadSink = services.GetService(ArtifactBufferKey.Read);
            ArtifactCommandResults = services.GetService(ArtifactBufferKey.CommandResults);
            ArtifactCommandResultStore = services.GetService(ArtifactBufferKey.CommandStore);
            ArtifactInitializationWriteSink = services.GetService(ArtifactBufferKey.Initialize);
            ArtifactEditWriteSink = services.GetService(ArtifactBufferKey.Edit);
            ArtifactPreviewWriteSink = services.GetService(ArtifactBufferKey.Preview);
            ArtifactEditResultReadSink = services.GetService(ArtifactBufferKey.EditResults);
            ArtifactPreviewReadSink = services.GetService(ArtifactBufferKey.Previews);
            CharacterReadSink = services.GetService(CharacterBufferKey.Read);
            CharacterWriteSink = services.GetService(CharacterBufferKey.Write);
            CombatStateReadSink = services.GetService(CombatStateBufferKey.Read);
            CharacterStatsReadSink = services.GetService(CharacterBufferKey.StatsRead);
            CharacterStatsRequestSink = services.GetService(CharacterBufferKey.StatsRequest);
            CharacterStatsStore = services.GetService(CharacterBufferKey.StatsStore);
            ((CharacterStatsBuffer)CharacterStatsReadSink).BindSources(RunProgressReadSink, CharacterReadSink, CombatStateReadSink);
            CombatResultReadSink = services.GetService(CombatStateBufferKey.Results);
            CombatResultHistoryReadSink = services.GetService(CombatStateBufferKey.History);
            FameReadSink = services.GetService(FameBufferKey.Read);
            CurrencyReadSink = services.GetService(CurrencyBufferKey.Read);
            CurrencyWriteSink = services.GetService(CurrencyBufferKey.Write);
            StageProgressReadSink = services.GetService(StageProgressBufferKey.Read);
            StageProgressWriteSink = services.GetService(StageProgressBufferKey.Write);
            CombatViewReadSink = services.GetService(CombatViewBufferKey.Read);
            CombatViewWriteSink = services.GetService(CombatViewBufferKey.Write);
            StageViewReadSink = services.GetService(StageViewBufferKey.Read);
            StageViewWriteSink = services.GetService(StageViewBufferKey.Write);
            ArtifactViewReadSink = services.GetService(ArtifactViewBufferKey.Read);
            ArtifactViewWriteSink = services.GetService(ArtifactViewBufferKey.Write);
            ShopViewReadSink = services.GetService(ShopViewBufferKey.Read);
            ShopViewWriteSink = services.GetService(ShopViewBufferKey.Write);
            RecoveryResultReadSink = services.GetService(RecoveryResultBufferKey.Read);
            RecoveryViewReadSink = services.GetService(RecoveryViewBufferKey.Read);
            RecoveryViewWriteSink = services.GetService(RecoveryViewBufferKey.Write);
            LobbyViewReadSink = services.GetService(LobbyViewBufferKey.Read);
            LobbyViewWriteSink = services.GetService(LobbyViewBufferKey.Write);
            TutorialRequestWriteSink = services.GetService(TutorialBufferKey.RequestWrite);
            TutorialResultReadSink = services.GetService(TutorialBufferKey.ResultRead);
            TutorialStateReadSink = services.GetService(TutorialBufferKey.StateRead);
            TutorialStore = services.GetService(TutorialBufferKey.Store);

            // 저장소는 거래 클래스에만 넘기고 Group은 보관하지 않는다.
            var runStore = services.GetService(RunProgressBufferKey.Store);
            var currentStore = services.GetService(CurrentProgressBufferKey.Store);
            var mapStore = services.GetService(MapProgressBufferKey.Store);
            var characterStore = services.GetService(CharacterBufferKey.Store);
            var combatStore = services.GetService(CombatStateBufferKey.Store);
            var currencyStore = services.GetService(CurrencyBufferKey.Store);
            var stageStore = services.GetService(StageProgressBufferKey.Store);
            var itemStore = services.GetService(ItemInventoryBufferKey.Store);

            var deckStore = services.GetService(RunDeckBufferKey.Store);
            RunDeckWriteSink = new RunDeckTransactions(RunProgressReadSink, CharacterReadSink, RunDeckReadSink, deckStore, CombatStateReadSink);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            DevelopmentWriteSink = new DevelopmentTransactions(RunProgressReadSink, CurrentProgressReadSink,
                CombatStateReadSink, CurrencyReadSink, currencyStore, MapProgressReadSink, StageProgressReadSink,
                stageStore, currentStore);
#endif
            TraitGrowthStateReadSink = services.GetService(CharacterBufferKey.GrowthState);
            TraitGrowthResultReadSink = services.GetService(CharacterBufferKey.GrowthResults);
            TraitGrowthViewReadSink = services.GetService(TraitGrowthViewBufferKey.Read);
            TraitGrowthViewWriteSink = services.GetService(TraitGrowthViewBufferKey.Write);
            TraitGrowthOperations = new TraitGrowthTransactions(RunProgressReadSink, CombatStateReadSink, CharacterReadSink,
                characterStore, TraitGrowthStateReadSink, TraitGrowthResultReadSink, services.GetService(CharacterBufferKey.GrowthStore),
                TraitGrowthViewReadSink, services.GetService(TraitGrowthViewBufferKey.Store));
            MainSpecializationOperations = new MainSpecializationTransactions(this, characterStore);
            var guards = new BufferGuards(RunProgressReadSink, CurrentProgressReadSink, CombatStateReadSink);
            _items = new ItemInventoryTransactions(guards, itemStore);
            var completion = new BattleCompletionTransaction(guards, CombatStateReadSink, StageProgressReadSink,
                MapProgressReadSink, combatStore, stageStore, mapStore, runStore, CurrencyReadSink, FameReadSink, TraitGrowthOperations);
            _battle = new BattleTransactions(guards, completion, CharacterReadSink,
                CombatStateReadSink, CombatViewReadSink, ArtifactReadSink, StageProgressReadSink,
                characterStore, combatStore, currentStore, itemStore, CurrencyReadSink, currencyStore, FameReadSink, services.GetService(FameBufferKey.Store), RunProgressReadSink, runStore, RunDeckReadSink);
            _progress = new ProgressTransactions(guards, _battle, CurrentProgressReadSink, CurrentProgressWriteSink,
                MapProgressReadSink, MapProgressWriteSink, StageProgressReadSink, StageProgressWriteSink,
                CombatStateReadSink, CurrencyReadSink, CurrencyWriteSink, FameReadSink, services.GetService(FameBufferKey.Store), characterStore, currentStore, stageStore, runStore, combatStore, itemStore, deckStore, TraitGrowthOperations);
            _shop = new ShopTransactions(guards, CurrentProgressReadSink, CombatStateReadSink,
                MapProgressReadSink, StageProgressReadSink, CharacterReadSink, characterStore,
                currencyStore, currentStore, itemStore, CurrencyReadSink);
            _stageServices = new StageServiceTransactions(guards, CurrentProgressReadSink, CombatStateReadSink,
                MapProgressReadSink, StageProgressReadSink, CharacterReadSink,
                characterStore, currencyStore, currentStore, CurrencyReadSink, stageStore, _progress,
                RecoveryResultReadSink, services.GetService(RecoveryResultBufferKey.Store), RecoveryViewReadSink);
        }

        public override void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<IBufferQuery>(this);
            sink.AddModule<ISystemModule>(this);
        }

        public void InitData(IDataBaseQueryContext data)
        {
            foreach (var module in SystemReader.SystemModuleStoreReader.InitDataModules)
                module.InitData(data);
        }
    }

    #endregion
}
