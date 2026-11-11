using System;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Current;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Recovery;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.AudioSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Recovery;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.Recovery
{
    /// <summary>회복 팝업의 연결별 구성을 생성합니다.</summary>
    internal sealed class RecoveryPopupViewFactory
    {
        private readonly ICurrentProgressBufferReadSink _current;
        private readonly IRecoveryStateReadSink _state;
        private readonly IRecoveryRequestStatus _status;
        private readonly IRecoveryRequestSubmitter _requests;
        private readonly IUISystemService _ui;
        private readonly IRecoveryOptionVisualTableQuery _visuals;
        private readonly IStageOfferTextTableQuery _texts;
        private readonly RuntimeFaultBoundary _faults;
        private readonly IAudioSystemService _audio;

        internal RecoveryPopupViewFactory(ICurrentProgressBufferReadSink current, IRecoveryStateReadSink state,
            IRecoveryRequestStatus status, IRecoveryRequestSubmitter requests, IUISystemService ui,
            IRecoveryOptionVisualTableQuery visuals, IStageOfferTextTableQuery texts, RuntimeFaultBoundary faults,
            IAudioSystemService audio)
        {
            _current = current ?? throw new ArgumentNullException(nameof(current));
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _status = status ?? throw new ArgumentNullException(nameof(status));
            _requests = requests ?? throw new ArgumentNullException(nameof(requests));
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            _visuals = visuals ?? throw new ArgumentNullException(nameof(visuals));
            _texts = texts ?? throw new ArgumentNullException(nameof(texts));
            _faults = faults ?? throw new ArgumentNullException(nameof(faults));
            _audio = audio ?? throw new ArgumentNullException(nameof(audio));
        }
        internal RecoveryPopupController Create(IStageOfferDisplay screen, PopupInputSession input,
            IPopupViewHost host, Action mapChanged, StageMapSelectionPresentation selection)
        {
            if (_faults.IsFaulted)
                throw new InvalidOperationException("[RecoveryPopup] 정지한 런타임에는 팝업을 만들 수 없습니다.");
            return new RecoveryPopupController(_current, _state, _status, _requests, _ui, screen, input, host,
                mapChanged, Report, _faults, new RecoveryPopupModelBuilder(_visuals, _texts), _texts, _audio,
                (selection ?? throw new ArgumentNullException(nameof(selection))).Observe(new StageOfferPopupViewScreen(screen)));
        }

        private static void Report(string message) => UnityEngine.Debug.Log($"[StageMap] {message}");
    }
}
