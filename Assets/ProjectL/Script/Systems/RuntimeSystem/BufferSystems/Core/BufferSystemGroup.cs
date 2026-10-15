using ProjectL.Systems.RuntimeSystem.BufferSystems.StageView;
using ProjectL.Systems.RuntimeSystem.BufferSystems.ArtifactView;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.ServiceKeys;
using ProjectL.Framework.Composition;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Artifact;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Currency;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Current;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Map;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Run;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;


namespace ProjectL.Systems.RuntimeSystem.BufferSystems.Core
{
    #region 서비스

    public interface IRunProgressBufferWriteSink : IServiceModule
    {
        /// <summary>
        /// 새 Run을 위해 임시 진행 데이터를 비우고 맵 등록과 영구 Artifact 보유 기록은 유지한다.
        /// 로스터와 Run 재화도 비우므로 이것만으로는 플레이 가능한 Run이 아니다. 새 Run 시작은 PublishNewRun을 쓴다.
        /// </summary>
        void ResetRunProgress();

        /// <summary>
        /// 이전 Run을 비우고 새 Run의 로스터와 시작 Run 재화를 한 번에 설치한다. 준비가 실패하면 아무것도 바꾸지 않는다.
        /// 같은 제안을 다시 게시해도 매번 비운 뒤 설치하므로 중복 지급되지 않는다. 챕터 진입은 호출하는 흐름이 이어서 한다.
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

    public interface ICharacterPurchaseBufferWriteSink : IServiceModule
    {
        CharacterPurchaseResult PublishPurchase(StageNodeId nodeId, StageKey stageKey,
            ShopOfferId offerId, int expectedBalance, CharacterPurchaseDecision decision);
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

    internal interface IBufferQuery : IServiceModule
    {
        IChapterContinuationBufferWriteSink ChapterContinuationWriteSink { get; }
        IChapterProgressBufferWriteSink ChapterProgressWriteSink { get; }
        IRunProgressBufferReadSink RunProgressReadSink { get; }
        IRecoveryStageBufferWriteSink RecoveryStageWriteSink { get; }
        IShopStageBufferWriteSink ShopStageWriteSink { get; }
        ICharacterPurchaseBufferWriteSink CharacterPurchaseWriteSink { get; }
        IRunProgressBufferWriteSink RunProgressWriteSink { get; }
        IBattleBufferWriteSink BattleWriteSink { get; }
        ICurrentProgressBufferReadSink CurrentProgressReadSink { get; }
        ICurrentProgressBufferWriteSink CurrentProgressWriteSink { get; }
        IMapProgressBufferReadSink MapProgressReadSink { get; }
        IMapProgressBufferWriteSink MapProgressWriteSink { get; }
        IArtifactBufferReadSink ArtifactReadSink { get; }
        IArtifactInitializationWriteSink ArtifactInitializationWriteSink { get; }
        IArtifactEditWriteSink ArtifactEditWriteSink { get; }
        IArtifactPreviewWriteSink ArtifactPreviewWriteSink { get; }
        IArtifactEditResultReadSink ArtifactEditResultReadSink { get; }
        IArtifactPreviewReadSink ArtifactPreviewReadSink { get; }
        ICharacterBufferReadSink CharacterReadSink { get; }
        ICharacterBufferWriteSink CharacterWriteSink { get; }
        ICombatStateBufferReadSink CombatStateReadSink { get; }
        ICombatResultReadSink CombatResultReadSink { get; }
        ICombatResultHistoryReadSink CombatResultHistoryReadSink { get; }
        ICurrencyBufferReadSink CurrencyReadSink { get; }
        ICurrencyBufferWriteSink CurrencyWriteSink { get; }
        IStageProgressBufferReadSink StageProgressReadSink { get; }
        IStageProgressBufferWriteSink StageProgressWriteSink { get; }
        ICombatViewBufferReadSink CombatViewReadSink { get; }
        ICombatViewBufferWriteSink CombatViewWriteSink { get; }
        IStageViewBufferReadSink StageViewReadSink { get; }
        IStageViewBufferWriteSink StageViewWriteSink { get; }
        IArtifactViewBufferReadSink ArtifactViewReadSink { get; }
        IArtifactViewBufferWriteSink ArtifactViewWriteSink { get; }
    }

    #endregion

    #region 시스템 모듈

    /// <summary>
    /// 하위 Buffer 모듈을 묶는 Group이다. 초기화 데이터를 하위 모듈에 전달하고, 하위 Buffer 서비스와
    /// 저장소 간 거래(전투·진행·비전투 스테이지)를 IBufferQuery 하나로 노출한다.
    /// 거래 본문은 이 클래스에 두지 않는다. 새 거래는 맞는 거래 클래스에 넣거나 새 클래스로 나눈다.
    /// </summary>
    internal sealed class BufferSystemGroup : SystemGroup, IBufferQuery, ISystemInitData<IDataBaseQueryContext>
    {
        private readonly BattleTransactions _battle;
        private readonly ProgressTransactions _progress;
        private readonly StageServiceTransactions _stageServices;

        public IRunProgressBufferReadSink RunProgressReadSink { get; }
        public IChapterContinuationBufferWriteSink ChapterContinuationWriteSink => _progress;
        public IChapterProgressBufferWriteSink ChapterProgressWriteSink => _progress;
        public IRunProgressBufferWriteSink RunProgressWriteSink => _progress;
        public IRecoveryStageBufferWriteSink RecoveryStageWriteSink => _stageServices;
        public IShopStageBufferWriteSink ShopStageWriteSink => _stageServices;
        public ICharacterPurchaseBufferWriteSink CharacterPurchaseWriteSink => _stageServices;
        public IBattleBufferWriteSink BattleWriteSink => _battle;
        public ICurrentProgressBufferReadSink CurrentProgressReadSink { get; }
        public ICurrentProgressBufferWriteSink CurrentProgressWriteSink { get; }
        public IMapProgressBufferReadSink MapProgressReadSink { get; }
        public IMapProgressBufferWriteSink MapProgressWriteSink { get; }
        public IArtifactBufferReadSink ArtifactReadSink { get; }
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

        public BufferSystemGroup(ISystemReader systemReader) : base(systemReader)
        {
            var services = systemReader.ServiceStoreReader;
            RunProgressReadSink = services.GetService(RunProgressBufferKey.Read);
            CurrentProgressReadSink = services.GetService(CurrentProgressBufferKey.Read);
            CurrentProgressWriteSink = services.GetService(CurrentProgressBufferKey.Write);
            MapProgressReadSink = services.GetService(MapProgressBufferKey.Read);
            MapProgressWriteSink = services.GetService(MapProgressBufferKey.Write);
            ArtifactReadSink = services.GetService(ArtifactBufferKey.Read);
            ArtifactInitializationWriteSink = services.GetService(ArtifactBufferKey.Initialize);
            ArtifactEditWriteSink = services.GetService(ArtifactBufferKey.Edit);
            ArtifactPreviewWriteSink = services.GetService(ArtifactBufferKey.Preview);
            ArtifactEditResultReadSink = services.GetService(ArtifactBufferKey.EditResults);
            ArtifactPreviewReadSink = services.GetService(ArtifactBufferKey.Previews);
            CharacterReadSink = services.GetService(CharacterBufferKey.Read);
            CharacterWriteSink = services.GetService(CharacterBufferKey.Write);
            CombatStateReadSink = services.GetService(CombatStateBufferKey.Read);
            CombatResultReadSink = services.GetService(CombatStateBufferKey.Results);
            CombatResultHistoryReadSink = services.GetService(CombatStateBufferKey.History);
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

            // 저장소는 거래 클래스에만 넘기고 Group은 보관하지 않는다.
            var runStore = services.GetService(RunProgressBufferKey.Store);
            var currentStore = services.GetService(CurrentProgressBufferKey.Store);
            var mapStore = services.GetService(MapProgressBufferKey.Store);
            var characterStore = services.GetService(CharacterBufferKey.Store);
            var combatStore = services.GetService(CombatStateBufferKey.Store);
            var currencyStore = services.GetService(CurrencyBufferKey.Store);
            var stageStore = services.GetService(StageProgressBufferKey.Store);

            var guards = new BufferGuards(RunProgressReadSink, CurrentProgressReadSink, CombatStateReadSink);
            var completion = new BattleCompletionTransaction(guards, CombatStateReadSink, StageProgressReadSink,
                MapProgressReadSink, combatStore, stageStore, mapStore, runStore);
            _battle = new BattleTransactions(guards, completion, CharacterReadSink, CharacterWriteSink,
                CombatStateReadSink, CombatViewReadSink, ArtifactReadSink, StageProgressReadSink,
                characterStore, combatStore, currentStore);
            _progress = new ProgressTransactions(guards, _battle, CurrentProgressReadSink, CurrentProgressWriteSink,
                MapProgressReadSink, MapProgressWriteSink, StageProgressReadSink, StageProgressWriteSink,
                CombatStateReadSink, CurrencyWriteSink, characterStore, currentStore, stageStore, runStore);
            _stageServices = new StageServiceTransactions(guards, CurrentProgressReadSink, CombatStateReadSink,
                MapProgressReadSink, StageProgressReadSink, CharacterReadSink, CharacterWriteSink,
                characterStore, currencyStore, currentStore);
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
