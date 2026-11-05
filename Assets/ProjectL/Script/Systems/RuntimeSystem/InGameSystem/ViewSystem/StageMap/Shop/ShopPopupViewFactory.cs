using System;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Unit.Item;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Currency;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Current;
using ProjectL.Systems.RuntimeSystem.BufferSystems.ShopView;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Common;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.Shop
{
    /// <summary>고정 의존성으로 연결마다 상점 팝업을 만들고 부모 Scope에 소유권을 넘깁니다.</summary>
    internal sealed class ShopPopupViewFactory
    {
        private readonly ICurrentProgressBufferReadSink _current;
        private readonly IStageProgressBufferReadSink _stages;
        private readonly ICharacterBufferReadSink _characters;
        private readonly ICurrencyBufferReadSink _currency;
        private readonly IShopViewBufferReadSink _input;
        private readonly IShopViewBufferWriteSink _writer;
        private readonly IStageCommandResultReadSink _results;
        private readonly IUISystemService _ui;
        private readonly IUnitVisualTableQuery _visuals;
        private readonly IConsumableItemVisualTableQuery _itemVisuals;
        private readonly IStageOfferTextTableQuery _texts;
        private readonly ITraitVisualTableQuery _traitVisuals;
        private readonly RuntimeFaultBoundary _faults;

        internal ShopPopupViewFactory(ICurrentProgressBufferReadSink current, IStageProgressBufferReadSink stages,
            ICharacterBufferReadSink characters, ICurrencyBufferReadSink currency,
            IShopViewBufferReadSink input, IShopViewBufferWriteSink writer, IStageCommandResultReadSink results,
            IUISystemService ui, IUnitVisualTableQuery visuals, RuntimeFaultBoundary faults,
            IConsumableItemVisualTableQuery itemVisuals, IStageOfferTextTableQuery texts, ITraitVisualTableQuery traitVisuals)
        {
            _current = current ?? throw new ArgumentNullException(nameof(current));
            _stages = stages ?? throw new ArgumentNullException(nameof(stages));
            _characters = characters ?? throw new ArgumentNullException(nameof(characters));
            _currency = currency ?? throw new ArgumentNullException(nameof(currency));
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
            _results = results ?? throw new ArgumentNullException(nameof(results));
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            _visuals = visuals ?? throw new ArgumentNullException(nameof(visuals));
            _itemVisuals = itemVisuals ?? throw new ArgumentNullException(nameof(itemVisuals));
            _texts = texts ?? throw new ArgumentNullException(nameof(texts));
            _traitVisuals = traitVisuals ?? throw new ArgumentNullException(nameof(traitVisuals));
            _faults = faults ?? throw new ArgumentNullException(nameof(faults));
        }

        internal ShopPopupController Create(IStageOfferDisplay screen, PopupInputSession input,
            IPopupViewHost host, Action mapChanged, StageMapSelectionPresentation selection)
        {
            if (_faults.IsFaulted)
                throw new InvalidOperationException("[ShopPopup] 정지한 런타임에는 팝업을 만들 수 없습니다.");
            var models = new ShopPopupModelBuilder(_current, _currency, _characters, _texts, new IShopOfferContentBuilder[]
            {
                new UnitShopOfferContentBuilder(_characters, FindName, FindIcon),
                new ItemShopOfferContentBuilder(FindItemIcon),
                new EnhancementShopOfferContentBuilder(FindName, FindIcon)
            }, new TrainingCardContentBuilder(FindName, FindIcon, _traitVisuals));
            return new ShopPopupController(models, _current, _stages, _input, _writer, _results,
                _ui, screen, input, host, mapChanged, Report, _faults, _texts,
                (selection ?? throw new ArgumentNullException(nameof(selection))).Observe(new StageOfferPopupViewScreen(screen)));
        }

        private string FindName(EntityKey key) =>
            _visuals.TryGetUnitVisual(key, out UnitVisualData visual) ? visual.DisplayName : null;
        private Sprite FindIcon(EntityKey key) =>
            _visuals.TryGetUnitVisual(key, out UnitVisualData visual) ? visual.InfoIcon : null;
        private Sprite FindItemIcon(int key) =>
            _itemVisuals.TryGet(key, out var visual) ? visual.Icon : null;
        private static void Report(string message) => Debug.Log($"[StageMap] {message}");
    }
}
