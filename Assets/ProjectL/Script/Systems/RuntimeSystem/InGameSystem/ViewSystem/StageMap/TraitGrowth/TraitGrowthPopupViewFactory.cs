using System;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.TraitGrowthView;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.AudioSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.TraitGrowth;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Stage;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.TraitGrowth
{
    /// <summary>고정 의존성으로 연결마다 증강 선택 화면 Controller를 만들고 부모 Scope에 소유권을 넘깁니다.</summary>
    internal sealed class TraitGrowthPopupViewFactory
    {
        private readonly ITraitGrowthStateReadSink _offers;
        private readonly ITraitGrowthResultReadSink _results;
        private readonly ITraitGrowthViewWriteSink _writer;
        private readonly ITraitGrowthRequestSubmitter _requests;
        private readonly IStageTraitGrowthFacts _facts;
        private readonly TraitGrowthPopupModelBuilder _models;
        private readonly IUISystemService _ui;
        private readonly RuntimeFaultBoundary _faults;
        private readonly IAudioSystemService _audio;

        internal TraitGrowthPopupViewFactory(ITraitGrowthStateReadSink offers, ITraitGrowthResultReadSink results,
            ITraitGrowthViewWriteSink writer, ITraitGrowthRequestSubmitter requests,
            IStageTraitGrowthFacts facts, ICharacterBufferReadSink characters, IUnitVisualTableQuery units,
            ITraitVisualTableQuery traits, IUISystemService ui, RuntimeFaultBoundary faults, IStageOfferTextTableQuery texts,
            IAudioSystemService audio)
        {
            _offers = offers ?? throw new ArgumentNullException(nameof(offers));
            _results = results ?? throw new ArgumentNullException(nameof(results));
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
            _requests = requests ?? throw new ArgumentNullException(nameof(requests));
            _facts = facts ?? throw new ArgumentNullException(nameof(facts));
            _models = new TraitGrowthPopupModelBuilder(units, traits, characters, texts);
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            _faults = faults ?? throw new ArgumentNullException(nameof(faults));
            _audio = audio ?? throw new ArgumentNullException(nameof(audio));
        }

        internal TraitGrowthPopupController Create(IStageOfferDisplay screen, PopupInputSession input,
            IPopupViewHost host)
        {
            if (_faults.IsFaulted)
                throw new InvalidOperationException("[TraitGrowthPopup] 정지한 런타임에는 팝업을 만들 수 없습니다.");
            return new TraitGrowthPopupController(_offers, _results, _writer, _requests, _facts, _models,
                _ui, screen, input, host, _faults, _audio);
        }
    }
}
