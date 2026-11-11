using System;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Currency;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Current;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Recovery;
using ProjectL.Systems.RuntimeSystem.BufferSystems.ShopView;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.RuntimeSystem.BufferSystems.TraitGrowthView;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.AudioSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Recovery;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.TraitGrowth;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Stage;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap
{
    /// <summary>
    /// 팝업 넷(상점·회복·덱 편집·증강)의 Factory를 만들 때 쓰는 값의 묶음입니다. 씬 조립(<c>StageMapSceneFactory</c>)만 받습니다
    /// (스테이지 선택 리팩토링 설계 §2.9 C-19).
    /// </summary>
    internal sealed class StageMapViewPopupServices
    {
        internal StageMapViewPopupServices(
            IUnitVisualTableQuery unitVisuals, IConsumableItemVisualTableQuery itemVisuals,
            IRecoveryOptionVisualTableQuery recoveryVisuals, IStageOfferTextTableQuery offerTexts,
            ITraitVisualTableQuery traitVisuals,
            ICurrentProgressBufferReadSink current, IStageProgressBufferReadSink stages,
            ICharacterBufferReadSink characters, ICurrencyBufferReadSink currency,
            IShopViewBufferReadSink shopInput, IShopViewBufferWriteSink shopWriter, IStageCommandResultReadSink results,
            IRecoveryStateReadSink recoveryState, IRecoveryRequestStatus recoveryStatus,
            IRecoveryRequestSubmitter recoveryRequests, IRunDeckService deck,
            ITraitGrowthStateReadSink growthOffers, ITraitGrowthResultReadSink growthResults,
            ITraitGrowthViewWriteSink growthWriter,
            ITraitGrowthRequestSubmitter growthRequests, IStageTraitGrowthFacts growthFacts,
            IUISystemService ui, StageMapViewNotice notice, IAudioSystemService audio)
        {
            UnitVisuals = unitVisuals ?? throw new ArgumentNullException(nameof(unitVisuals));
            ItemVisuals = itemVisuals ?? throw new ArgumentNullException(nameof(itemVisuals));
            RecoveryVisuals = recoveryVisuals ?? throw new ArgumentNullException(nameof(recoveryVisuals));
            OfferTexts = offerTexts ?? throw new ArgumentNullException(nameof(offerTexts));
            TraitVisuals = traitVisuals ?? throw new ArgumentNullException(nameof(traitVisuals));
            Current = current ?? throw new ArgumentNullException(nameof(current));
            Stages = stages ?? throw new ArgumentNullException(nameof(stages));
            Characters = characters ?? throw new ArgumentNullException(nameof(characters));
            Currency = currency ?? throw new ArgumentNullException(nameof(currency));
            ShopInput = shopInput ?? throw new ArgumentNullException(nameof(shopInput));
            ShopWriter = shopWriter ?? throw new ArgumentNullException(nameof(shopWriter));
            Results = results ?? throw new ArgumentNullException(nameof(results));
            RecoveryState = recoveryState ?? throw new ArgumentNullException(nameof(recoveryState));
            RecoveryStatus = recoveryStatus ?? throw new ArgumentNullException(nameof(recoveryStatus));
            RecoveryRequests = recoveryRequests ?? throw new ArgumentNullException(nameof(recoveryRequests));
            Deck = deck ?? throw new ArgumentNullException(nameof(deck));
            GrowthOffers = growthOffers ?? throw new ArgumentNullException(nameof(growthOffers));
            GrowthResults = growthResults ?? throw new ArgumentNullException(nameof(growthResults));
            GrowthWriter = growthWriter ?? throw new ArgumentNullException(nameof(growthWriter));
            GrowthRequests = growthRequests ?? throw new ArgumentNullException(nameof(growthRequests));
            GrowthFacts = growthFacts ?? throw new ArgumentNullException(nameof(growthFacts));
            UI = ui ?? throw new ArgumentNullException(nameof(ui));
            Notice = notice ?? throw new ArgumentNullException(nameof(notice));
            Audio = audio ?? throw new ArgumentNullException(nameof(audio));
        }

        internal IUnitVisualTableQuery UnitVisuals { get; }
        internal IConsumableItemVisualTableQuery ItemVisuals { get; }
        internal IRecoveryOptionVisualTableQuery RecoveryVisuals { get; }
        internal IStageOfferTextTableQuery OfferTexts { get; }
        internal ITraitVisualTableQuery TraitVisuals { get; }
        internal ICurrentProgressBufferReadSink Current { get; }
        internal IStageProgressBufferReadSink Stages { get; }
        internal ICharacterBufferReadSink Characters { get; }
        internal ICurrencyBufferReadSink Currency { get; }
        internal IShopViewBufferReadSink ShopInput { get; }
        internal IShopViewBufferWriteSink ShopWriter { get; }
        internal IStageCommandResultReadSink Results { get; }
        internal IRecoveryStateReadSink RecoveryState { get; }
        internal IRecoveryRequestStatus RecoveryStatus { get; }
        internal IRecoveryRequestSubmitter RecoveryRequests { get; }
        internal IRunDeckService Deck { get; }
        // 증강 선택 화면(강화 View 설계 §14)이 쓰는 칸입니다. 결과 확인 쓰기는 화면 Controller만 씁니다.
        internal ITraitGrowthStateReadSink GrowthOffers { get; }
        internal ITraitGrowthResultReadSink GrowthResults { get; }
        internal ITraitGrowthViewWriteSink GrowthWriter { get; }
        internal ITraitGrowthRequestSubmitter GrowthRequests { get; }
        internal IStageTraitGrowthFacts GrowthFacts { get; }
        internal IUISystemService UI { get; }
        /// <summary>덱 편집 안내를 띄우는 공용 안내입니다. 맵 진입 거절과 같은 객체입니다.</summary>
        internal StageMapViewNotice Notice { get; }
        internal IAudioSystemService Audio { get; }
    }
}
