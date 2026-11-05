using System;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.Deck;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.Recovery;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.Shop;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.TraitGrowth;
using UnityEngine;

namespace ProjectL.Systems.Factory.Runtime.InGame.View
{
    /// <summary>
    /// 스테이지 맵 씬 연결의 조립입니다. 서비스 묶음으로 팝업 Factory 넷(상점·회복·덱 편집·증강)을 만들고 Scope에 넘깁니다.
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
            var shop = new ShopPopupViewFactory(services.Current, services.Stages, services.Characters,
                services.Currency, services.ShopInput, services.ShopWriter, services.Results, services.UI,
                services.UnitVisuals, faults, services.ItemVisuals, services.OfferTexts, services.TraitVisuals);
            var recovery = new RecoveryPopupViewFactory(services.Current, services.RecoveryState,
                services.RecoveryStatus, services.RecoveryRequests, services.UI, services.RecoveryVisuals, services.OfferTexts, faults);
            var deck = new DeckEditPopupViewFactory(services.Deck, services.UI, faults);
            var growth = new TraitGrowthPopupViewFactory(services.GrowthOffers, services.GrowthResults,
                services.GrowthInputs, services.GrowthWriter, services.GrowthRequests, services.GrowthFacts,
                services.Characters, services.UnitVisuals, services.TraitVisuals, services.UI, faults, services.OfferTexts);
            var popups = new StageMapViewPopups(shop, recovery, deck, growth, root.UI);
            return new StageMapViewScope(root, output, services, popups, faults);
        }
    }
}
