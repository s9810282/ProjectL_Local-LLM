using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Unit.Item;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Currency;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Current;
using ProjectL.Systems.RuntimeSystem.BufferSystems.ShopView;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.AudioSystem;
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
        private readonly IAudioSystemService _audio;

        internal ShopPopupViewFactory(ICurrentProgressBufferReadSink current, IStageProgressBufferReadSink stages,
            ICharacterBufferReadSink characters, ICurrencyBufferReadSink currency,
            IShopViewBufferReadSink input, IShopViewBufferWriteSink writer, IStageCommandResultReadSink results,
            IUISystemService ui, IUnitVisualTableQuery visuals, RuntimeFaultBoundary faults,
            IConsumableItemVisualTableQuery itemVisuals, IStageOfferTextTableQuery texts, ITraitVisualTableQuery traitVisuals,
            IAudioSystemService audio)
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
            _audio = audio ?? throw new ArgumentNullException(nameof(audio));
        }

        internal ShopPopupController Create(IStageOfferDisplay screen, PopupInputSession input,
            IPopupViewHost host, Action mapChanged, StageMapSelectionPresentation selection)
        {
            if (_faults.IsFaulted)
                throw new InvalidOperationException("[ShopPopup] 정지한 런타임에는 팝업을 만들 수 없습니다.");
            var models = new ShopPopupModelBuilder(_current, _currency, _characters, _texts, new IShopOfferContentBuilder[]
            {
                new UnitShopOfferContentBuilder(_characters, FindName, FindIcon),
                new ItemShopOfferContentBuilder(FindItemIcon)
            }, new TrainingCardContentBuilder(FindName, FindIcon, _traitVisuals));
            return new ShopPopupController(models, _current, _stages, _input, _writer, _results,
                _ui, screen, input, host, mapChanged, Report, _faults, _texts, _audio,
                (selection ?? throw new ArgumentNullException(nameof(selection))).Observe(new StageOfferPopupViewScreen(screen)));
        }

        // 외형 행 누락 정책은 팝업마다 같게 "예외"입니다. 유닛·아이템 외형은 전투에서도 필수라 조용히 키 숫자로 대신하지 않습니다(평가 N-23).
        private UnitVisualData UnitVisualOf(EntityKey key) => _visuals.TryGetUnitVisual(key, out UnitVisualData visual)
            ? visual
            : throw new KeyNotFoundException($"[ShopPopup] 유닛 외형이 등록되지 않았습니다: {key}");
        private string FindName(EntityKey key) => UnitVisualOf(key).DisplayName;
        private Sprite FindIcon(EntityKey key) => UnitVisualOf(key).InfoIcon;
        private Sprite FindItemIcon(int key) => _itemVisuals.TryGet(key, out var visual)
            ? visual.Icon
            : throw new KeyNotFoundException($"[ShopPopup] 아이템 외형이 등록되지 않았습니다: {key}");
        private static void Report(string message) => Debug.Log($"[StageMap] {message}");
    }
}
