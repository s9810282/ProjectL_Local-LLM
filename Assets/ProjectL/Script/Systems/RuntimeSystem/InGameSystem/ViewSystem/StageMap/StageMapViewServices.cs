using System;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageMap.Item;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageNode.Item;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Currency;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Current;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Recovery;
using ProjectL.Systems.RuntimeSystem.BufferSystems.ShopView;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.RuntimeSystem.BufferSystems.StageView;
using ProjectL.Systems.RuntimeSystem.BufferSystems.TraitGrowthView;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.AudioSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.View;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.CameraGroup.CameraCatalogSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.ColorSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UtilitySystem.DelaySystem.View;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Recovery;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.TraitGrowth;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageSelectionInput;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Stage;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap
{
    /// <summary>
    /// 스테이지 맵 씬 연결이 쓰는 DB 값과 서비스의 묶음입니다. System이 서비스를 받은 뒤 한 번 만들고, 연결마다 그대로 넘깁니다.
    /// 모든 칸이 운영에 필요하므로 만들 때 한 번 검사하고, 쓰는 곳은 다시 검사하지 않습니다(검사 후속 정리 설계 §12.2 B1·B2).
    /// System만 쓰는 배경음·씬 노드 등록은 넣지 않습니다.
    /// </summary>
    internal sealed class StageMapViewServices
    {
        internal StageMapViewServices(
            StageMapViewData mapData, IStageNodeVisualTableQuery nodeVisuals, IStageMapBackgroundTableQuery backgrounds,
            IUnitVisualTableQuery unitVisuals, IConsumableItemVisualTableQuery itemVisuals,
            IRecoveryOptionVisualTableQuery recoveryVisuals, IStageOfferTextTableQuery offerTexts,
            ICurrentProgressBufferReadSink current, IStageProgressBufferReadSink stages,
            ICharacterBufferReadSink characters, ICurrencyBufferReadSink currency,
            IShopViewBufferReadSink shopInput, IShopViewBufferWriteSink shopWriter,
            IStageViewBufferReadSink requests, IStageViewBufferWriteSink requestWriter,
            IStageCommandResultReadSink results, IStageEntryAttemptRead attempts,
            IMapCombatPresentationOwnerSource presentationOwner,
            IRecoveryStateReadSink recoveryState, IRecoveryRequestStatus recoveryStatus,
            IRecoveryRequestSubmitter recoveryRequests, IRunDeckService deck,
            IStageSelectionReceiverSlot slot, IStageMapInteractionFeedback feedback,
            IUISystemService ui, IInputLayerService layers, IDelayService delay, ICameraCatalogService cameras,
            ITraitVisualTableQuery traitVisuals, ITraitGrowthStateReadSink growthOffers,
            ITraitGrowthResultReadSink growthResults, ITraitGrowthViewReadSink growthInputs,
            ITraitGrowthViewWriteSink growthWriter, ITraitGrowthRequestSubmitter growthRequests,
            IStageTraitGrowthFacts growthFacts, IColorSystemService colors, IAudioSystemService audio)
        {
            MapData = mapData ?? throw new ArgumentNullException(nameof(mapData));
            if (nodeVisuals == null) throw new ArgumentNullException(nameof(nodeVisuals));
            FindVisual = node => nodeVisuals.TryGetNodeVisual(node.StageKey, node.StageType, node.MapRole,
                out StageNodeVisualData visual) ? visual : null;
            Backgrounds = backgrounds ?? throw new ArgumentNullException(nameof(backgrounds));
            UnitVisuals = unitVisuals ?? throw new ArgumentNullException(nameof(unitVisuals));
            ItemVisuals = itemVisuals ?? throw new ArgumentNullException(nameof(itemVisuals));
            RecoveryVisuals = recoveryVisuals ?? throw new ArgumentNullException(nameof(recoveryVisuals));
            OfferTexts = offerTexts ?? throw new ArgumentNullException(nameof(offerTexts));
            Current = current ?? throw new ArgumentNullException(nameof(current));
            Stages = stages ?? throw new ArgumentNullException(nameof(stages));
            Characters = characters ?? throw new ArgumentNullException(nameof(characters));
            Currency = currency ?? throw new ArgumentNullException(nameof(currency));
            ShopInput = shopInput ?? throw new ArgumentNullException(nameof(shopInput));
            ShopWriter = shopWriter ?? throw new ArgumentNullException(nameof(shopWriter));
            Requests = requests ?? throw new ArgumentNullException(nameof(requests));
            RequestWriter = requestWriter ?? throw new ArgumentNullException(nameof(requestWriter));
            Results = results ?? throw new ArgumentNullException(nameof(results));
            Attempts = attempts ?? throw new ArgumentNullException(nameof(attempts));
            PresentationOwner = presentationOwner ?? throw new ArgumentNullException(nameof(presentationOwner));
            RecoveryState = recoveryState ?? throw new ArgumentNullException(nameof(recoveryState));
            RecoveryStatus = recoveryStatus ?? throw new ArgumentNullException(nameof(recoveryStatus));
            RecoveryRequests = recoveryRequests ?? throw new ArgumentNullException(nameof(recoveryRequests));
            Deck = deck ?? throw new ArgumentNullException(nameof(deck));
            Slot = slot ?? throw new ArgumentNullException(nameof(slot));
            Feedback = feedback ?? throw new ArgumentNullException(nameof(feedback));
            UI = ui ?? throw new ArgumentNullException(nameof(ui));
            Layers = layers ?? throw new ArgumentNullException(nameof(layers));
            Delay = delay ?? throw new ArgumentNullException(nameof(delay));
            Cameras = cameras ?? throw new ArgumentNullException(nameof(cameras));
            TraitVisuals = traitVisuals ?? throw new ArgumentNullException(nameof(traitVisuals));
            GrowthOffers = growthOffers ?? throw new ArgumentNullException(nameof(growthOffers));
            GrowthResults = growthResults ?? throw new ArgumentNullException(nameof(growthResults));
            GrowthInputs = growthInputs ?? throw new ArgumentNullException(nameof(growthInputs));
            GrowthWriter = growthWriter ?? throw new ArgumentNullException(nameof(growthWriter));
            GrowthRequests = growthRequests ?? throw new ArgumentNullException(nameof(growthRequests));
            GrowthFacts = growthFacts ?? throw new ArgumentNullException(nameof(growthFacts));
            Colors = colors ?? throw new ArgumentNullException(nameof(colors));
            Audio = audio ?? throw new ArgumentNullException(nameof(audio));
        }

        internal StageMapViewData MapData { get; }
        internal Func<StageProgressData, StageNodeVisualData> FindVisual { get; }
        internal IStageMapBackgroundTableQuery Backgrounds { get; }
        internal IUnitVisualTableQuery UnitVisuals { get; }
        internal IConsumableItemVisualTableQuery ItemVisuals { get; }
        internal IRecoveryOptionVisualTableQuery RecoveryVisuals { get; }
        internal IStageOfferTextTableQuery OfferTexts { get; }
        internal ICurrentProgressBufferReadSink Current { get; }
        internal IStageProgressBufferReadSink Stages { get; }
        internal ICharacterBufferReadSink Characters { get; }
        internal ICurrencyBufferReadSink Currency { get; }
        internal IShopViewBufferReadSink ShopInput { get; }
        internal IShopViewBufferWriteSink ShopWriter { get; }
        internal IStageViewBufferReadSink Requests { get; }
        internal IStageViewBufferWriteSink RequestWriter { get; }
        internal IStageCommandResultReadSink Results { get; }
        internal IStageEntryAttemptRead Attempts { get; }
        internal IMapCombatPresentationOwnerSource PresentationOwner { get; }
        internal IRecoveryStateReadSink RecoveryState { get; }
        internal IRecoveryRequestStatus RecoveryStatus { get; }
        internal IRecoveryRequestSubmitter RecoveryRequests { get; }
        internal IRunDeckService Deck { get; }
        internal IStageSelectionReceiverSlot Slot { get; }
        /// <summary>
        /// 맵 입력 알림을 받는 호버 표시입니다. 이 묶음과 같이 한 번 만들어 연결 사이에도 남고, 호버 상태를 가집니다.
        /// 연결이 끝날 때 맵 입력 정리(<see cref="StageMapInteraction.StopInput"/>)가 호버를 0으로 알려 상태를 비웁니다.
        /// </summary>
        internal IStageMapInteractionFeedback Feedback { get; }
        internal IUISystemService UI { get; }
        internal IInputLayerService Layers { get; }
        internal IDelayService Delay { get; }
        internal ICameraCatalogService Cameras { get; }
        internal IColorSystemService Colors { get; }
        internal IAudioSystemService Audio { get; }

        // 증강 선택 화면(강화 View 설계 §14)이 쓰는 칸입니다. 결과 확인 쓰기는 화면 Controller만 씁니다.
        internal ITraitVisualTableQuery TraitVisuals { get; }
        internal ITraitGrowthStateReadSink GrowthOffers { get; }
        internal ITraitGrowthResultReadSink GrowthResults { get; }
        internal ITraitGrowthViewReadSink GrowthInputs { get; }
        internal ITraitGrowthViewWriteSink GrowthWriter { get; }
        internal ITraitGrowthRequestSubmitter GrowthRequests { get; }
        internal IStageTraitGrowthFacts GrowthFacts { get; }
    }
}
