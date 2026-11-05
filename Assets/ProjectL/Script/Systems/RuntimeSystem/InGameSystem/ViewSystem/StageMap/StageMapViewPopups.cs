using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.Deck;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.Recovery;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.Shop;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.TraitGrowth;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap
{
    /// <summary>연결 한 번의 팝업 부품입니다. 상점·회복·덱 편집·증강 컨트롤러를 같은 입력 세션으로 만들고 Tick·정리를 묶습니다.</summary>
    internal sealed class StageMapViewPopups
    {
        private readonly ShopPopupViewFactory _shopFactory;
        private readonly RecoveryPopupViewFactory _recoveryFactory;
        private readonly DeckEditPopupViewFactory _deckFactory;
        private readonly TraitGrowthPopupViewFactory _growthFactory;
        private readonly StageMapCanvasView _ui;
        private Func<bool> _canWork;

        internal StageMapViewPopups(ShopPopupViewFactory shop, RecoveryPopupViewFactory recovery,
            DeckEditPopupViewFactory deck, TraitGrowthPopupViewFactory growth, StageMapCanvasView ui)
        {
            _shopFactory = shop ?? throw new ArgumentNullException(nameof(shop));
            _recoveryFactory = recovery ?? throw new ArgumentNullException(nameof(recovery));
            _deckFactory = deck ?? throw new ArgumentNullException(nameof(deck));
            _growthFactory = growth ?? throw new ArgumentNullException(nameof(growth));
            _ui = ui != null ? ui : throw new ArgumentNullException(nameof(ui));
        }

        internal ShopPopupController Shop { get; private set; }
        internal RecoveryPopupController Recovery { get; private set; }
        internal DeckEditPopupController DeckEdit { get; private set; }
        internal TraitGrowthPopupController TraitGrowth { get; private set; }

        /// <summary>
        /// 상점 → 회복 → 덱 편집 → 증강 순으로 만들고 시작합니다. 만든 것은 시작 전에 게시해 이미 열린 화면 인수보다 먼저 부모가 알게 합니다.
        /// 도중에 연결이 끝나면 false입니다(만든 것은 <see cref="Dispose"/>가 정리합니다).
        /// </summary>
        /// <param name="canOpenDeckEdit">덱 편집을 겹쳐 열지 않는 맵 쪽 조건입니다(덱 편집 쪽 몫, 설계 Q-R3).</param>
        internal bool Start(IPopupViewHost host, PopupInputSession input, Action mapChanged,
            StageMapSelectionPresentation selection, Func<bool> canOpenDeckEdit, Func<bool> canWork)
        {
            _canWork = canWork ?? throw new ArgumentNullException(nameof(canWork));
            ShopPopupController shop = _shopFactory.Create(_ui.Screens.ShopScreen, input, host, mapChanged, selection);
            if (!canWork())
            {
                shop.Dispose();
                return false;
            }
            Shop = shop;
            shop.Start();
            if (!canWork()) return false;
            RecoveryPopupController recovery = _recoveryFactory.Create(_ui.Screens.RecoveryScreen, input, host,
                mapChanged, selection);
            if (!canWork())
            {
                recovery.Dispose();
                return false;
            }
            Recovery = recovery;
            recovery.Start();
            if (!canWork()) return false;
            // 덱 편집은 상점·회복과 같은 공통 모달 Layer·입력 세션을 씁니다(계획 RD-0 Q-RD1). 열 수 있는지는 Core가 판단합니다.
            DeckEditPopupController deckEdit = _deckFactory.Create(_ui.Selection.DeckScreen, input, host, canOpenDeckEdit);
            if (!canWork())
            {
                deckEdit.Dispose();
                return false;
            }
            DeckEdit = deckEdit;
            deckEdit.Start();
            if (!canWork()) return false;
            // 증강 선택 화면은 공통 Canvas 아래의 묶음 프리팹이며 공통 모달 Layer·입력 세션을 씁니다.
            TraitGrowthPopupController growth = _growthFactory.Create(_ui.TraitGrowth, input, host);
            if (!canWork())
            {
                growth.Dispose();
                return false;
            }
            TraitGrowth = growth;
            growth.Start();
            return canWork();
        }

        /// <summary>상점 → 회복 → 덱 편집 → 증강 순으로 진행합니다. 사이에 연결이 끝나면 멈춥니다(시작 때 받은 연결 확인).</summary>
        internal void Tick()
        {
            Shop.Tick();
            if (!_canWork()) return;
            Recovery.Tick();
            if (!_canWork()) return;
            DeckEdit.Tick();
            if (!_canWork()) return;
            TraitGrowth.Tick();
        }

        /// <summary>상점 → 회복 → 덱 편집 → 증강 순으로 정리합니다(옛 Scope 정리 순서).</summary>
        internal void Dispose()
        {
            List<Exception> errors = null;
            CleanupErrors.Try(() => Shop?.Dispose(), ref errors);
            CleanupErrors.Try(() => Recovery?.Dispose(), ref errors);
            CleanupErrors.Try(() => DeckEdit?.Dispose(), ref errors);
            CleanupErrors.Try(() => TraitGrowth?.Dispose(), ref errors);
            CleanupErrors.ThrowIfAny(errors, "[StageMap] 팝업 정리 중 실패했습니다.");
        }
    }
}
