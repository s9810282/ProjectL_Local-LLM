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
    /// <summary>
    /// 연결 한 번의 팝업 부품입니다. 상점·회복·덱 편집·증강 컨트롤러를 같은 입력 세션으로 만들고, 만든 순서대로 목록에 넣어
    /// 시작·Tick·정리·열림/대기 판정을 목록으로 돕니다(스테이지 선택 리팩토링 설계 §2.6 C-01).
    /// 만들기는 Factory 서명이 달라 하나씩 둡니다. 구체 타입 속성은 복원·증강 열기·출전이 씁니다.
    /// </summary>
    internal sealed class StageMapViewPopups
    {
        private readonly ShopPopupViewFactory _shopFactory;
        private readonly RecoveryPopupViewFactory _recoveryFactory;
        private readonly DeckEditPopupViewFactory _deckFactory;
        private readonly TraitGrowthPopupViewFactory _growthFactory;
        private readonly StageMapCanvasView _ui;
        private readonly List<IStageMapViewPopup> _items = new(4);
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

        /// <summary>팝업 하나라도 열려 있습니다.</summary>
        internal bool IsAnyOpen
        {
            get
            {
                for (int i = 0; i < _items.Count; i++)
                    if (_items[i].IsOpen) return true;
                return false;
            }
        }

        /// <summary>팝업 하나라도 제출한 요청의 결과를 기다립니다.</summary>
        internal bool IsAnyWaiting
        {
            get
            {
                for (int i = 0; i < _items.Count; i++)
                    if (_items[i].IsWaiting) return true;
                return false;
            }
        }

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
            if (!Adopt(shop)) return false;
            Shop = shop;
            if (!StartLast()) return false;
            RecoveryPopupController recovery = _recoveryFactory.Create(_ui.Screens.RecoveryScreen, input, host,
                mapChanged, selection);
            if (!Adopt(recovery)) return false;
            Recovery = recovery;
            if (!StartLast()) return false;
            // 덱 편집은 상점·회복과 같은 공통 모달 Layer·입력 세션을 씁니다(계획 RD-0 Q-RD1). 열 수 있는지는 Core가 판단합니다.
            DeckEditPopupController deckEdit = _deckFactory.Create(_ui.Selection.DeckScreen, _ui.Selection.DeckScreen, input,
                host, canOpenDeckEdit);
            if (!Adopt(deckEdit)) return false;
            DeckEdit = deckEdit;
            if (!StartLast()) return false;
            // 증강 선택 화면은 공통 Canvas 아래의 묶음 프리팹이며 공통 모달 Layer·입력 세션을 씁니다.
            TraitGrowthPopupController growth = _growthFactory.Create(_ui.TraitGrowth, input, host);
            if (!Adopt(growth)) return false;
            TraitGrowth = growth;
            return StartLast();
        }

        /// <summary>상점 → 회복 → 덱 편집 → 증강 순으로 진행합니다. 사이에 연결이 끝나면 멈춥니다(시작 때 받은 연결 확인).</summary>
        internal void Tick()
        {
            for (int i = 0; i < _items.Count; i++)
            {
                if (i > 0 && !_canWork()) return;
                _items[i].Tick();
            }
        }

        /// <summary>상점 → 회복 → 덱 편집 → 증강 순으로 정리합니다(옛 Scope 정리 순서).</summary>
        internal void Dispose()
        {
            List<Exception> errors = null;
            for (int i = 0; i < _items.Count; i++)
            {
                IStageMapViewPopup popup = _items[i];
                CleanupErrors.Try(popup.Dispose, ref errors);
            }
            CleanupErrors.ThrowIfAny(errors, "[StageMap] 팝업 정리 중 실패했습니다.");
        }

        /// <summary>만드는 도중 연결이 끝났으면 버리고, 아니면 목록에 넣습니다. 시작은 구체 속성을 게시한 뒤입니다.</summary>
        private bool Adopt(IStageMapViewPopup popup)
        {
            if (!_canWork())
            {
                popup.Dispose();
                return false;
            }
            _items.Add(popup);
            return true;
        }

        private bool StartLast()
        {
            _items[_items.Count - 1].Start();
            return _canWork();
        }
    }
}
