using System;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.LoadingSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.Completion;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.Deck;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.Entry;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.Recovery;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.Shop;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.TraitGrowth;
using UnityEngine;

namespace ProjectL.Systems.Factory.Runtime.InGame.View
{
    /// <summary>
    /// 스테이지 맵 씬 연결의 조립입니다. 팝업 묶음(<see cref="StageMapViewPopupServices"/>)으로 팝업 Factory 넷(상점·회복·덱 편집·증강)을 만들고 Scope에 넘깁니다.
    /// 상태가 없고 <see cref="Instance"/> 하나만 씁니다.
    /// </summary>
    internal sealed class StageMapSceneFactory : IStageMapSceneFactory
    {
        internal static readonly StageMapSceneFactory Instance = new();

        private StageMapSceneFactory()
        {
        }

        public StageMapViewScope Create(StageMapSceneRoot root, Camera output, StageMapViewServices services,
            RuntimeFaultBoundary faults)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (faults == null) throw new ArgumentNullException(nameof(faults));
            StageMapViewPopupServices p = services.Popups;
            var shop = new ShopPopupViewFactory(p.Current, p.Stages, p.Characters, p.Currency, p.ShopInput, p.ShopWriter,
                p.Results, p.UI, p.UnitVisuals, faults, p.ItemVisuals, p.OfferTexts, p.TraitVisuals, p.Audio);
            var recovery = new RecoveryPopupViewFactory(p.Current, p.RecoveryState, p.RecoveryStatus, p.RecoveryRequests,
                p.UI, p.RecoveryVisuals, p.OfferTexts, faults, p.Audio);
            var deck = new DeckEditPopupViewFactory(p.Deck, p.UI, faults, p.Notice);
            var growth = new TraitGrowthPopupViewFactory(p.GrowthOffers, p.GrowthResults, p.GrowthWriter, p.GrowthRequests,
                p.GrowthFacts, p.Characters, p.UnitVisuals, p.TraitVisuals, p.UI, faults, p.OfferTexts, p.Audio);
            var popups = new StageMapViewPopups(shop, recovery, deck, growth, root.UI);
            var drops = new StageNodeDropPresentation(services.MapData.NodeDrop, services.NodeDropEffects, services.NodeDropAudio);
            var intro = new StageMapIntroPresentation(services.MapData.MapIntro, services.IntroHistory,
                drops, new StageLinkDrawPresentation(), new StageMapCameraTour());
            var policy = new StageMapIntroInteractionPolicy(intro);
            var focus = new StageMapCameraFocus(services.MapData.CameraFocus);
            return new StageMapViewScope(root, output, services, popups, intro, policy, faults, focus, this);
        }

        public ILodingPreparation CreatePreparation(StageMapViewScope scope, RuntimeFaultBoundary faults)
            => new StageMapLoadingPreparation(scope, faults);

        public StageNodeClearPresentation CreateCompletion(StageMapViewScene scene, StageMapViewCompletionServices services,
            StageMapCameraFocus focus, Func<bool> canWork, Func<StageMapMovementContext> captureContext,
            Func<StageMapMovementContext, bool> isContextCurrent, Action clearPointer)
        {
            if (scene == null) throw new ArgumentNullException(nameof(scene));
            if (services == null) throw new ArgumentNullException(nameof(services));
            var jump = new StageMapViewMarkerJump(scene.Display, scene.Root.MapRoot, services.MapData,
                services.Delay, services.Audio, focus, "StageNodeClearPresentation");
            return new StageNodeClearPresentation(services, scene.Display, jump, canWork, captureContext,
                isContextCurrent, clearPointer);
        }
    }
}
