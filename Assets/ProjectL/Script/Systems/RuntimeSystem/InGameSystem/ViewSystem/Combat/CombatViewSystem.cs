using System;
using System.Collections.Generic;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageField.Item;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Current;
using ProjectL.Systems.RuntimeSystem.Common;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Data;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.View;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.LoadingSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.ColorSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.View.AreaOutlineSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageInput;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.Base;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat;
using ProjectL.Systems.ServiceKeys;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat
{
    /// <summary>
    /// 전투 View의 조정자입니다(설계 §4, W2·W16). 씬 연결·첫 그리기와, 결과 하나의 재생 → 표시 반영 → 입력 정리 → 확인 순서를 정합니다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 맡는 것: Framework 수명, 씬 Attach(첫 그리기 포함, 개선 설계 D13)·Detach·오류 정지(<see cref="StopInput"/>),
    /// Tick의 결과 소비 순서(<c>FinalizeResult</c>), 목표 입력 Layer 동기화, 입력 판단용 사실(<see cref="CombatInputFacts"/>) 조립,
    /// 골격 이탈 계약(<see cref="SuspendSubmissions"/>·<see cref="PresentCompletion"/>), 종료·포기 버튼 상태 맞춤(새 결과 관측·이탈 시작 때, D12).
    /// </para>
    /// <para>
    /// 맡기는 것: 재생 수명 <see cref="CombatResultPlayback"/>, 변경 관측 <see cref="CombatViewRefresh"/>, 허용 판단 <see cref="CombatInputPolicy"/>,
    /// 입력 수신 <see cref="CombatInputReceiver"/>(D11), 입력 분배 <see cref="CombatInputRouter"/>, 시작/턴 종료 대기 <see cref="CombatCommandGate"/>,
    /// 배치·공격 <see cref="CombatInteraction"/>, 이동 경로 <see cref="CombatPathInput"/>, 포기 확인창 <see cref="CombatAbandonController"/>,
    /// 개발 Layer 패널 <see cref="CombatDevLayerController"/>, 그리기 <see cref="ICombatBattleDisplay"/>(값만 넘김). 같은 상태를 여기에 복제하지 않습니다.
    /// </para>
    /// <para>
    /// Core 소유 Buffer에는 쓰지 않고, 쓰는 Buffer는 CombatViewBuffer(처리 번호까지 삭제·결과 확인)뿐입니다.
    /// 입력 Buffer 전체 비우기(Clear)는 CombatLifeCycle만 합니다. 타임라인은 아직 Core에 직접 묻습니다(VR6 대기).
    /// </para>
    /// </remarks>
    internal sealed class CombatViewSystem :
        ICombatViewService,
        ISystemInitData<IHasViewQuery>,
        ISystemPostBindService,
        ISystemInitialize,
        ISystemTick,
        ISystemShutdown,
        IExportSystemServices
    {
        // CombatLifeCycle의 Enter(0) 뒤에 연결하고, LifeCycle의 Exit(0) 전에 정리한다(설계 §11).
        internal const int SceneEnterPriority = 100;
        internal const int SceneExitPriority = -100;

        // 타임라인 예측 요청 길이(현강님 HUD 기준). VR6에서 Core가 게시하는 예측 범위 계약으로 대체한다(구조 개선 VR0·VR5).
        private const int MinTimelineEntries = 10;
        private const int TimelineEntriesPerCharacter = 4;

        private readonly RuntimeFaultBoundary _faults;
        private readonly CombatResultPlayback _playback;
        private readonly ICombatPresentation _presentation;
        private CombatPathInput _pathInput;
        private CombatSceneRoot _sceneRoot;
        private long _battleInstance;
        private bool _submissionsSuspended;
        // Attach에서 첫 그리기(Install)가 성공했는지입니다. 입력 허용 판단(facts)의 "최초 표시 뒤"가 이 값이다(개선 설계 D13).
        private bool _battleShown;
        private BattleCompletionChanges _shownCompletion;
        // 공격 모드가 아닌지(경로 입력 쪽인지)입니다. 대상 판정 자체는 CombatInputRouter.ResolvePointerTarget 한 곳이다.
        private bool IsMoveMode => _interaction != null && !_interaction.IsAttackActive;
        private readonly CombatViewRefresh _refresh = new();

        private ICombatStateBufferReadSink _state;
        private ICombatResultReadSink _results;
        private ICharacterBufferReadSink _characters;
        private ICurrentProgressBufferReadSink _current;
        private ICombatViewBufferReadSink _inputRead;
        private ICombatViewBufferWriteSink _inputWrite;
        private IInputLayerService _layers;
        private ILodingRegisterService _loadingRegister;
        private IColorSystemService _colors;
        private IAreaOutlineSystemService _areaOutlines;
        private IUISystemService _ui;

        private StageFieldViewData _fieldConfig;
        private IUnitVisualTableQuery _unitVisuals;
        private IStageViewPropTableQuery _stageProps;
        private ITraitVisualTableQuery _traitVisuals;
        private ICombatProcessingService _combatQueries;

        // 포기 버튼이 보는 골격의 좁은 계약(포기 UI 설계 P2). 확인창 열림은 포기 컨트롤러의 지역 상태다(C6, 개선 설계 D2).
        private ICombatAbandonRequest _abandonRequest;
        private readonly CombatAbandonController _abandon;
        // RecordInputOnly 개발 Layer 패널(W20, 개선 설계 D3).
        private readonly CombatDevLayerController _devLayer;

        private ICombatBattleDisplay _display;
        private CombatInteraction _interaction;
        // 시작/턴 종료 대기의 원본. Attach 때 만들고 결과를 그린 뒤 Resolve, 오류 정지·Detach 때 Clear한다(구조 개선 VR1).
        private CombatCommandGate _gate;
        // 입력 의도별 처리 순서와 대상 선택(구조 개선 VR2). Attach 수명이다.
        private CombatInputRouter _router;
        private readonly Func<CombatInputFacts> _readPathFacts;
        private readonly Func<bool, CombatInputFacts> _readInputFacts;
        // 입력 수신 창구(슬롯 연결·입구 검사, 개선 설계 D11). 서비스를 받을 때 만든다.
        private CombatInputReceiver _receiver;

        internal CombatViewSystem(
            CombatExecutionMode executionMode,
            RuntimeFaultBoundary faultBoundary,
            ICombatPresentation presentation)
        {
            if (!System.Enum.IsDefined(typeof(CombatExecutionMode), executionMode))
                throw new ArgumentOutOfRangeException(nameof(executionMode), executionMode, null);

            ExecutionMode = executionMode;
            _faults = faultBoundary ?? throw new ArgumentNullException(nameof(faultBoundary));
            _presentation = presentation;
            _playback = new CombatResultPlayback(presentation);
            // 경로 입력에 넘기는 사실 공급자도 한 번만 만든다. 경로 판단에는 선행 미처리 명령도 읽는다.
            _readPathFacts = () => ReadFacts(includeUnhandledCommand: true);
            _readInputFacts = includeUnhandledCommand => ReadFacts(includeUnhandledCommand);
            // 버튼 패널은 UI EventSystem 입구라 수명·활성 판단을 매번 이 조정자에게 묻는다.
            _abandon = new CombatAbandonController(executionMode, _faults, () => ReadFacts(),
                () => !_faults.IsFaulted && _entry != null && _entry.IsActive && _display != null,
                CancelInputPresentation);
            _devLayer = new CombatDevLayerController(executionMode, _faults, () => ReadFacts(),
                () => !_faults.IsFaulted && _entry != null && _entry.IsActive && IsInputBound && _interaction != null,
                () => _interaction?.CancelPresentation());
        }

        /// <summary>조립 때 고정한 실행 모드입니다. 개발 Layer 패널(E.4a)이 사용합니다.</summary>
        internal CombatExecutionMode ExecutionMode { get; }

        /// <summary>Combat 씬의 SceneRoot에 연결돼 있는지입니다.</summary>
        public bool IsAttached => _display != null && _playback.IsAttached;
        private ICombatEntryRead _entry;

        #region 시스템 모듈

        /// <summary>View DB 표 네 개(필드 외형 설정, 유닛·구조물 외형, 스테이지 배경 프랍, 특성 외형)를 보관합니다.</summary>
        public void InitData(IHasViewQuery data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            _fieldConfig = data.ViewQuery.StageFieldViewTableQuery.Config
                ?? throw new InvalidOperationException("[CombatViewSystem] StageFieldView 설정이 없습니다.");
            _unitVisuals = data.ViewQuery.UnitVisualTableQuery
                ?? throw new InvalidOperationException("[CombatViewSystem] UnitVisual 표가 없습니다.");
            _stageProps = data.ViewQuery.StageViewPropTableQuery
                ?? throw new InvalidOperationException("[CombatViewSystem] StageViewProp 표가 없습니다.");
            _traitVisuals = data.ViewQuery.TraitVisualTableQuery
                ?? throw new InvalidOperationException("[CombatViewSystem] TraitVisual 표가 없습니다.");
        }

        public void PostBindService(ISystemServiceStoreReader resolver)
        {
            if (resolver == null)
                throw new ArgumentNullException(nameof(resolver));

            // IBufferQuery는 모든 싱크를 노출하므로 이 시스템에 필요한 것만 꺼내 보관한다.
            IBufferQuery buffers = resolver.GetService(BufferSystemKey.Buffer);
            _combatQueries = resolver.GetService(SystemGroupsService.InGame).CoreGroup.Combat;
            var global = resolver.GetService(SystemGroupsService.Global);
            var lifeCycles = resolver.GetService(SystemGroupsService.LifeCycle);
            _ui = global.UIServiceCommand;
            BindServices(
                buffers.CombatStateReadSink,
                buffers.CombatResultReadSink,
                buffers.CharacterReadSink,
                buffers.CurrentProgressReadSink,
                buffers.CombatViewReadSink,
                buffers.CombatViewWriteSink,
                resolver.GetService(StageInputKey.ReceiverSlot),
                global.InputServiceCommand.InputLayerServices,
                global.LodingServiceCommand.LodingRegisterService,
                global.RenderServiceCommand.CoreGroup.ColorSystemServices,
                global.RenderServiceCommand.ViewGroup.AreaOutlineSystemServices,
                lifeCycles.CombatEntry,
                lifeCycles.CombatExit);
        }

        /// <summary>필요한 읽기/쓰기 싱크와 입력·로딩·색·칸 테두리 서비스만 보관합니다(테스트 입구이기도 합니다).</summary>
        internal void BindServices(
            ICombatStateBufferReadSink state,
            ICombatResultReadSink results,
            ICharacterBufferReadSink characters,
            ICurrentProgressBufferReadSink current,
            ICombatViewBufferReadSink inputRead,
            ICombatViewBufferWriteSink inputWrite,
            IStageInputReceiverSlot slot,
            IInputLayerService layers,
            ILodingRegisterService loadingRegister,
            IColorSystemService colors,
            IAreaOutlineSystemService areaOutlines,
            ICombatEntryRead entry,
            ICombatAbandonRequest abandon)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _results = results ?? throw new ArgumentNullException(nameof(results));
            _characters = characters ?? throw new ArgumentNullException(nameof(characters));
            _current = current ?? throw new ArgumentNullException(nameof(current));
            _inputRead = inputRead ?? throw new ArgumentNullException(nameof(inputRead));
            _inputWrite = inputWrite ?? throw new ArgumentNullException(nameof(inputWrite));
            if (slot == null)
                throw new ArgumentNullException(nameof(slot));
            _layers = layers ?? throw new ArgumentNullException(nameof(layers));
            _loadingRegister = loadingRegister ?? throw new ArgumentNullException(nameof(loadingRegister));
            _colors = colors ?? throw new ArgumentNullException(nameof(colors));
            _areaOutlines = areaOutlines ?? throw new ArgumentNullException(nameof(areaOutlines));
            _entry = entry ?? throw new ArgumentNullException(nameof(entry));
            _abandonRequest = abandon ?? throw new ArgumentNullException(nameof(abandon));
            _receiver = new CombatInputReceiver(slot, _faults,
                () => _entry != null && _entry.IsActive && _router != null,
                () => _abandon.IsConfirmOpen,
                (request, x, y) => _router.Route(request, x, y, _characters.Characters));
        }

        /// <summary>Combat 씬 번호에 연결(Enter)·정리(Exit) 처리를 등록합니다(W2).</summary>
        public void Initialize()
        {
            if (_loadingRegister == null)
                throw new InvalidOperationException("[CombatViewSystem] 서비스를 받기 전에 초기화할 수 없습니다.");

            _loadingRegister.RegisterSceneEnterLoaded(SceneIndices.Combat, new LodingNode(SceneEnterPriority, OnCombatSceneEntered));
            _loadingRegister.RegisterSceneExitLoaded(SceneIndices.Combat, new LodingNode(SceneExitPriority, Detach));
        }

        /// <summary>
        /// 전투 확인 → 연출 진행 → 변경 관측 → (새 결과면 제출 상태 맞춤) → 경로 미리보기 → 응답 반영 → 결과 재생·후처리 순서입니다(설계 §4·§5.2).
        /// 첫 그리기는 Attach가 하고(D13), 포기 버튼·종료 판정은 상태가 바뀌는 시점에만 맞춥니다(D12).
        /// 예상한 계약 위반과 연출 실패는 오류 경계에 보고하고, 예상하지 못한 예외는 Bootstrap까지 전파합니다.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (!CanContinue())
                return;

            // 전투가 사라지거나 다른 전투로 바뀌면 표시를 비우고 씬 연결을 끊는다. 같은 씬에 다음 전투를 다시 붙이지 않는다(D9).
            if (!_state.HasBattle || _battleInstance != _state.BattleInstanceId)
            {
                if (!_state.HasBattle)
                    _display.Clear();
                Detach();
                return;
            }

            _presentation.Tick(deltaTime);
            if (!CanContinue())
                return;

            _results.TryGetLatest(out CombatResult latest);
            CombatRefreshSignal signal = _refresh.Observe(
                _state.LatestResultSequence,
                _inputRead.PresentedResultSequence,
                latest,
                _state.AnsweredPreviewNumber);

            if (signal.Violation != null)
            {
                // 최신 결과로 건너뛰거나 확인 번호를 올리지 않는다(W9).
                _faults.Report(new InvalidOperationException($"[CombatViewSystem] {signal.Violation}"));
                return;
            }

            // 새 결과를 처음 관측했다. 종료(Completion·Finished)와 포기 가능 여부는 결과 게시 때만 바뀌므로 여기서 한 번 맞춘다(D12).
            if (signal.UnpresentedResult != null && !_playback.IsBusy)
            {
                SyncSubmissionState();
                if (!CanContinue())
                    return;
            }

            if (_pathInput != null)
            {
                if (_state.TryGetLatestPreview(out var movementPreview))
                    _pathInput.OnPreview(movementPreview);
                _pathInput.RefreshReturnPreview();
            }

            // 미리보기 응답: 현재 요청의 응답일 때만 Interaction이 조준 목록·범위·가능 여부로 쓴다(§21.1).
            // 외부 입력이 아니므로 후처리 차단과 무관하며, 예외는 Tick 경로로 전파한다.
            if (signal.PreviewAnswered)
            {
                _interaction.PreviewAnswered(ReadFacts(followUp: true), _characters.Characters);
                if (!CanContinue())
                    return;
            }

            if (signal.UnpresentedResult != null)
                ConsumeResult(signal.UnpresentedResult);
        }

        /// <summary>씬 연결이 남아 있으면 정리하고, 오류 경계에 맡긴 정리 콜백 참조를 놓습니다. 여러 번 불려도 안전합니다.</summary>
        public void Shutdown()
        {
            try
            {
                Detach();
            }
            finally
            {
                _faults.DisconnectStopInput(StopInput);
            }
        }

        public void ExportServices(ISystemWriteSink sink)
        {
            if (sink == null)
                throw new ArgumentNullException(nameof(sink));

            sink.AddService<ICombatViewService>(this);
            sink.AddModule<ISystemModule>(this);
        }

        #endregion

        #region 씬 연결

        /// <summary>
        /// SceneRoot에 연결합니다. 씬 참조와 View DB를 확인해 표시 구현을 만든 뒤 <see cref="AttachDisplay"/>로 연결합니다.
        /// </summary>
        public void Attach(CombatSceneRoot root)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));
            if (_faults.IsFaulted)
                return;
            if (_fieldConfig == null || _unitVisuals == null || _stageProps == null || _traitVisuals == null)
                throw new InvalidOperationException("[CombatViewSystem] View DB를 받기 전에 씬에 연결할 수 없습니다.");
            if (_colors == null || _areaOutlines == null || _state == null)
                throw new InvalidOperationException("[CombatViewSystem] 서비스를 받기 전에 씬에 연결할 수 없습니다.");
            // 표시 구현(Presenter 설정)을 만들기 전에 확인한다. 실패하면 씬 참조를 건드리지 않는다.
            RequireActiveBattle();

            AttachDisplay(new CombatSceneDisplay(root, _fieldConfig, _unitVisuals, _stageProps, _colors, _traitVisuals,
                _areaOutlines));
            _sceneRoot = root;
            root.FocusLost += FocusLost;
            root.Disabled += OnCombatSceneDisabled;
        }

        /// <summary>
        /// 표시에 연결하고 새 재생 세대를 시작합니다. 입력 연결은 전투 설치 신호로 처음 그린 뒤에 합니다.
        /// 오류 정지 뒤에는 연결하지 않습니다.
        /// </summary>
        internal void AttachDisplay(ICombatBattleDisplay display)
        {
            if (display == null)
                throw new ArgumentNullException(nameof(display));
            if (_display != null)
                throw new InvalidOperationException("[CombatViewSystem] 이미 씬에 연결돼 있습니다. 먼저 Detach해야 합니다.");
            if (_state == null)
                throw new InvalidOperationException("[CombatViewSystem] 서비스를 받기 전에 씬에 연결할 수 없습니다.");
            if (_faults.IsFaulted)
                return;
            RequireActiveBattle();

            _presentation.Attach(display);
            _playback.Attach();
            _refresh.Reset();
            _display = display;
            _submissionsSuspended = false;
            _battleShown = false;
            _shownCompletion = null;
            CompletionDisplay = CombatCompletionDisplay.None;
            IsAbandonRequested = false;
            _battleInstance = _state.BattleInstanceId;
            display.BindReturn(RequestReturn);
            // RecordInputOnly는 포기할 수 없으므로 버튼을 숨긴다(C7). 누를 수 있는지는 Tick에서 맞춘다.
            _abandon.Attach(display, _abandonRequest);
            _gate = new CombatCommandGate(_inputWrite);
            _interaction = new CombatInteraction(_inputWrite, _inputRead, display, display, _gate);
            _router = new CombatInputRouter(_interaction, _gate, _readInputFacts);

            // 개발 Layer 패널은 RecordInputOnly에서만 켜고 연결한다. Live는 끄고 연결하지 않는다(설계 §8.1, W20).
            _devLayer.Attach(display, _layers);

            ShowBattle();
        }

        /// <summary>
        /// 첫 그리기입니다(개선 설계 D13). 정적 필드를 구성하고 동적 값을 처음 적용한 뒤, 그 성공 뒤에만
        /// 경로 입력 생성 → 제출 상태(종료·포기 버튼) → 현재 Core 단계의 Layer로 입력 연결을 합니다(§5.1·§6.1).
        /// Install 안에서 오류 보고·Detach가 일어나면 그 자리에서 멈춥니다. Install 예외는 호출자(로딩 노드 → Bootstrap)로 전파합니다.
        /// </summary>
        private void ShowBattle()
        {
            _display.Install(ReadDisplayValues());
            if (!CanContinue())
                return;

            _battleShown = true;
            _pathInput = new CombatPathInput(_state, _characters, _inputRead, _inputWrite, _display, _display, _readPathFacts);
            _router.SetPathInput(_pathInput);
            SyncSubmissionState();
            if (!CanContinue())
                return;

            SyncInputToCore();
        }

        /// <summary>
        /// 진입 활성과 전투 설치를 확인합니다. Combat 씬 Enter 노드에서 LifeCycle(0)이 준비된 전투를 활성화한 뒤
        /// View(100)가 연결하므로 정상 흐름에서는 항상 참입니다(골격 설계 §6.3, 개선 설계 D13).
        /// </summary>
        private void RequireActiveBattle()
        {
            if (_entry == null || !_entry.IsActive || !_state.HasBattle)
                throw new InvalidOperationException(
                    "[CombatViewSystem] 진입이 활성이고 전투가 설치된 뒤에만 씬에 연결할 수 있습니다.");
        }

        /// <summary>
        /// 연출 취소 → 입력 해제 → 표시 반환·씬 참조 해제 순서로 정리합니다. 하나가 실패해도 나머지를 시도하고 모아서 던집니다.
        /// 명령 Buffer·확인 번호는 바꾸지 않습니다. 연결돼 있지 않으면 아무것도 하지 않습니다.
        /// </summary>
        public void Detach()
        {
            if (_display == null)
                return;

            if (_sceneRoot != null)
            {
                _sceneRoot.FocusLost -= FocusLost;
                _sceneRoot.Disabled -= OnCombatSceneDisabled;
                _sceneRoot = null;
            }

            List<Exception> errors = null;
            AbortPlayback(ref errors);
            StopInteraction(ref errors);
            ReleaseInput(ref errors);

            // 연출이 씬 참조를 놓은 뒤 Presenter가 자기 View를 돌려준다(설계 §6.1). 패널 연결도 Release에서 끊는다.
            ICombatBattleDisplay display = _display;
            _display = null;
            _battleShown = false;
            _shownCompletion = null;
            CompletionDisplay = CombatCompletionDisplay.None;
            IsAbandonRequested = false;
            _abandon.Detach();
            _devLayer.Detach();
            _interaction = null;
            _gate = null;
            _router = null;
            _refresh.Reset();
            try
            {
                _presentation.Detach();
            }
            catch (Exception exception)
            {
                (errors ??= new List<Exception>()).Add(exception);
            }
            try
            {
                display.Release();
            }
            catch (Exception exception)
            {
                (errors ??= new List<Exception>()).Add(exception);
            }

            ThrowIfAny(errors, "[CombatViewSystem] 씬 연결 해제 중 정리에 실패했습니다.");
        }

        /// <summary>
        /// 오류 경계가 부르는 입력 정지입니다(F1, 설계 §10.1). 오류 상태여도 끝까지 정리하며, 정상 종료 때도 쓰입니다.
        /// 재생을 먼저 무효화하고 연출을 취소한 뒤 입력을 해제합니다. 명령 Buffer·확인 번호는 보존합니다.
        /// </summary>
        internal void StopInput()
        {
            List<Exception> errors = null;
            AbortPlayback(ref errors);
            StopInteraction(ref errors);
            ReleaseInput(ref errors);
            // 연결돼 있지 않으면 두 컨트롤러는 아무것도 하지 않는다.
            try
            {
                _devLayer.Hide();
            }
            catch (Exception exception)
            {
                (errors ??= new List<Exception>()).Add(exception);
            }

            try
            {
                _abandon.Stop();
            }
            catch (Exception exception)
            {
                (errors ??= new List<Exception>()).Add(exception);
            }

            ThrowIfAny(errors, "[CombatViewSystem] 입력 정지 중 정리에 실패했습니다.");
        }

        /// <summary>Drag·모드·로컬 대기를 끝내고 고스트·원본 숨김·미리보기를 정리합니다. 명령 Buffer는 보존합니다.</summary>
        private void StopInteraction(ref List<Exception> errors)
        {
            var pathInput = _pathInput;
            _pathInput = null;
            _router?.SetPathInput(null);
            try
            {
                pathInput?.Detach();
            }
            catch (Exception exception)
            {
                (errors ??= new List<Exception>()).Add(exception);
            }

            // 로컬 대기는 표시 정리와 별개로 항상 끝낸다. 정상 종료의 SuspendSubmissions는 여기를 거치지 않으므로 대기를 보존한다.
            _gate?.Clear();
            if (_interaction == null)
                return;

            try
            {
                _interaction.Stop();
            }
            catch (Exception exception)
            {
                (errors ??= new List<Exception>()).Add(exception);
            }
        }

        private void OnCombatSceneEntered()
        {
            if (_faults.IsFaulted)
                return;
            if (!_entry.IsActive)
                throw new InvalidOperationException("[CombatViewSystem] 진입 수락과 LifeCycle 활성화가 필요합니다.");

            // 씬 오브젝트를 찾는 유일한 자리다. Tick·입력 경로에서는 찾지 않는다.
            CombatSceneRoot[] roots = UnityEngine.Object.FindObjectsByType<CombatSceneRoot>(UnityEngine.FindObjectsSortMode.None);
            if (roots.Length != 1)
                throw new InvalidOperationException(
                    $"[CombatViewSystem] Combat 씬에는 CombatSceneRoot가 하나 있어야 합니다. 찾은 수: {roots.Length}");

            Attach(roots[0]);
        }

        /// <summary>Unity의 비활성화 콜백은 Runtime Tick 밖에서 오므로 같은 F1 경계에 정리 실패를 보고합니다.</summary>
        private void OnCombatSceneDisabled()
        {
            try
            {
                Detach();
            }
            catch (Exception exception)
            {
                _faults.Report(exception);
            }
        }

        #endregion

        #region 결과 소비

        private void ConsumeResult(CombatResult result)
        {
            if (!_playback.IsBusy)
            {
                _pathInput?.CancelInputs();
                _playback.Begin(result);
                // Play 안의 재진입(오류·Detach)으로 세대가 바뀌었으면 여기서 멈춘다.
                if (!CanContinue())
                    return;
            }
            else if (_playback.PresentingSequence != result.Sequence)
            {
                _faults.Report(new InvalidOperationException(
                    $"[CombatViewSystem] 재생 중인 결과 {_playback.PresentingSequence}와 확인할 결과 {result.Sequence}가 다릅니다."));
                return;
            }

            if (_playback.TryGetFailure(out Exception failure))
            {
                // 실패 결과는 확인하지 않는다. 정리는 오류 경계가 StopInput으로 한다.
                _faults.Report(new InvalidOperationException(
                    $"[CombatViewSystem] 결과 {result.Sequence}의 연출이 실패했습니다.", failure));
                return;
            }

            // 아직 연출 중이면 다음 Tick에서 같은 결과의 통지를 다시 본다.
            if (_playback.TryBeginFinalization(out CombatPlaybackToken token))
                FinalizeResult(result, token);
        }

        /// <summary>
        /// 완료된 결과의 후처리입니다. 외부 호출에서 돌아올 때마다 오류·토큰을 다시 확인하고,
        /// 모두 성공했을 때만 처리 번호까지 삭제 → 결과 확인 → 재생 상태 해제를 합니다. 거절 결과도 같은 순서로 확인합니다.
        /// </summary>
        private void FinalizeResult(CombatResult result, CombatPlaybackToken token)
        {
            if (!result.Accepted)
                UnityEngine.Debug.Log($"[CombatViewSystem] 결과 {result.Sequence}가 거절됐습니다. 사유 코드: {result.Reason}");

            // 값이 같거나 거절이어도 갱신과 확인은 그대로 한다(설계 §6.1). 전투가 정리됐으면 그릴 것이 없다.
            if (_state.HasBattle)
                _display.Refresh(ReadDisplayValues());
            if (!IsCurrent(token))
                return;

            SyncTimelineUI(result);
            if (!IsCurrent(token))
                return;
            // 그린 뒤에만 일치하는 시작/턴 종료 대기를 해제한다(Kind == Command && 번호 일치, §7.2).
            _gate.Resolve(result);

            // 결과를 그린 뒤 현재 Core 단계로 Layer를 맞춘다. 종료 상태면 Layer를 다시 켜지 않는다(§5.5·§5.6).
            SyncInputToCore();
            if (!IsCurrent(token))
                return;

            // 기존 미리보기를 무효화하고, 유효하게 남은 선택·모드만 새 번호로 재요청한다. 외부 입력 차단을 푸는 것은 아니다(§7.1).
            _interaction.AfterResult(ReadFacts(followUp: true), _characters.Characters);
            if (!IsCurrent(token))
                return;

            // 처리 번호까지만 지우고(K14), 위 단계가 모두 성공한 뒤 마지막으로 확인한다(K1).
            _inputWrite.RemoveCommandsThrough(_state.HandledCommandNumber);
            _inputWrite.AcknowledgePresented(result.Sequence);
            _playback.CompleteFinalization(token);
            if (IsMoveMode)
                _pathInput?.RefreshReturnPreview();
        }

        private void SyncTimelineUI(CombatResult result)
        {
            if (!result.Accepted || _ui == null || !_state.HasBattle)
                return;

            var state = _state.Current;
            // G 입력이 수락되어 Buffer에 밤 전투가 설치된 뒤 Open에서 Image를 생성합니다.
            if (result.CommandKind == CombatCommandKind.EnterCombat &&
                state.CommandState.Turn.StagePhase == StagePhase.Combat && state.Phase != CombatFlowPhase.Finished)
                _ui.OpenUI(UIId.Timeline, true);
            else if (state.Phase == CombatFlowPhase.Finished ||
                (result.CommandKind == CombatCommandKind.EndUnitTurn && state.CommandState.Turn.StagePhase == StagePhase.Deployment))
                _ui.CloseUI(UIId.Timeline);
        }

        #endregion

        #region 표시와 입력 연결

        /// <summary>그릴 값을 Buffer에서 한 번에 읽습니다. 전투가 있을 때만 부릅니다.</summary>
        private CombatDisplayValues ReadDisplayValues()
        {
            // 배경 프랍은 진입 때 Core가 함께 게시한 현재 선택의 스테이지 키로 고른다(InGame DB를 읽지 않음, P6).
            int stageKey = _current.HasSelection ? _current.CurrentStage.StageKey : 0;
            // HUD 골드는 전투 상태의 스테이지 골드(드로우 전용)를 표시 값이 직접 읽는다. Run 재화는 전투 화면에 표시하지 않는다.
            return new CombatDisplayValues(_state.Current, _characters.Characters,
                _combatQueries?.GetTimeline(Math.Max(MinTimelineEntries, _characters.Characters.Count * TimelineEntriesPerCharacter)), stageKey);
        }

        /// <summary>
        /// Policy가 정한 목표 Layer로 입력 연결을 맞춥니다. 목표가 없으면(준비 전·종료·전투 정리) 입력을 해제합니다.
        /// 실제 Layer와 같으면 다시 바꾸지 않습니다.
        /// </summary>
        private void SyncInputToCore()
        {
            if (_submissionsSuspended)
            {
                ReleaseInputOrReport();
                return;
            }
            InputLayer desired = CombatInputPolicy.GetDesiredLayer(ReadFacts());
            if (desired == default)
            {
                CancelInputPresentation();
                ReleaseInputOrReport();
                return;
            }

            _receiver.Bind();

            if (_layers.CurrentLayer != desired)
            {
                // Layer가 바뀌면 이전 단계의 Drag·모드·선택·미리보기를 정리한다. 대기는 보존한다(§5.5).
                CancelInputPresentation();
                _layers.ChangeLayer(desired);
            }
        }

        /// <summary>
        /// 정상 종료의 새 입력 차단입니다(설계 §5.6). 재생·Attach·명령 대기는 보존하고
        /// Drag·모드·고스트·원본 숨김·미리보기를 정리한 뒤 입력 연결을 해제합니다. 멱등적입니다.
        /// </summary>
        public void SuspendSubmissions()
        {
            if (_submissionsSuspended)
                return;
            _submissionsSuspended = true;
            // 이탈 시작(골격 Begin이 이 메서드를 부른다)·종료면 포기를 받을 수 없다. 버튼을 끄고 열린 확인창을 닫는다(C6, D12).
            _abandon.Sync();
            CancelInputPresentation();
            ReleaseInputOrReport();
        }

        public void PresentCompletion(BattleCompletionChanges completion, Action completed)
        {
            if (completed == null)
                throw new ArgumentNullException(nameof(completed));
            ShowCompletion(completion, completed);
        }

        /// <summary>승패를 한 번 표시합니다. 확인되면 <see cref="CompletionDisplay"/>가 Confirmed가 됩니다(재구성 설계 J7·J12).</summary>
        public void PresentCompletion(BattleCompletionChanges completion)
        {
            ShowCompletion(completion, null);
        }

        private void ShowCompletion(BattleCompletionChanges completion, Action completed)
        {
            if (!CanContinue() || HasPendingPresentation || completion == null ||
                !ReferenceEquals(completion, _state.Completion) || _shownCompletion != null)
                throw new InvalidOperationException("Completion 표시는 현재 전투의 일반 결과 확인 뒤 한 번만 가능합니다.");
            _shownCompletion = completion;
            CompletionDisplay = CombatCompletionDisplay.Showing;
            ICombatBattleDisplay display = _display;
            bool confirmed = false;
            display.ShowCompletion(completion, () =>
            {
                if (confirmed || !CanContinue() || !ReferenceEquals(_display, display) ||
                    !ReferenceEquals(_shownCompletion, completion))
                    return;
                confirmed = true;
                CompletionDisplay = CombatCompletionDisplay.Confirmed;
                if (completed == null)
                    return;
                try
                {
                    completed();
                }
                catch (Exception error)
                {
                    _faults.Report(error);
                }
            });
        }

        private void ReleaseInputOrReport()
        {
            List<Exception> errors = null;
            ReleaseInput(ref errors);
            if (errors != null)
                _faults.Report(new AggregateException("[CombatViewSystem] 입력 해제에 실패했습니다.", errors));
        }

        /// <summary>
        /// 수신 창구 Unbind와 ClearLayer를 각각 시도합니다. 연결돼 있지 않거나 Layer가 없으면 건드리지 않습니다.
        /// Unbind 중 슬롯이 보내는 SelectCanceled는 연결된 상태로 받아 Router가 표현을 정리합니다(명령은 기록하지 않음, R2).
        /// </summary>
        private void ReleaseInput(ref List<Exception> errors)
        {
            try
            {
                _receiver?.Unbind();
            }
            catch (Exception exception)
            {
                (errors ??= new List<Exception>()).Add(exception);
            }

            if (_layers == null)
                return;

            try
            {
                if (_layers.CurrentLayer != default)
                    _layers.ClearLayer();
            }
            catch (Exception exception)
            {
                (errors ??= new List<Exception>()).Add(exception);
            }
        }

        private void AbortPlayback(ref List<Exception> errors)
        {
            try
            {
                _playback.Abort();
            }
            catch (Exception exception)
            {
                (errors ??= new List<Exception>()).Add(exception);
            }
        }

        /// <summary>
        /// 입력 판단에 쓸 현재 사실을 한 번 읽습니다. 저장하지 않습니다.
        /// </summary>
        /// <param name="includeUnhandledCommand">EndTurn 입력일 때만 선행 미처리 명령을 조회합니다(설계 §7.1).</param>
        /// <param name="followUp">결과 후처리 안의 내부 재요청 판단입니다. 외부 입력 차단(후처리 중)은 이 판단에 적용하지 않습니다.</param>
        private CombatInputFacts ReadFacts(bool includeUnhandledCommand = false, bool followUp = false)
        {
            bool hasBattle = _state.HasBattle;
            CombatCommandState command = hasBattle ? _state.Current.CommandState : null;
            return new CombatInputFacts(
                _battleShown,
                hasBattle,
                _state.Phase,
                hasBattle ? command.Turn.StagePhase : StagePhase.Deployment,
                _state.Completion != null,
                ExecutionMode,
                _layers.CurrentLayer,
                _state.LatestResultSequence,
                _inputRead.PresentedResultSequence,
                !followUp && _playback.IsFinalizing,
                hasBattle ? command.ActiveActorId : EntityId.Empty,
                includeUnhandledCommand && _inputRead.TryGetNextCommand(_state.HandledCommandNumber, out _),
                ReadCurrentPreviewAnswer(),
                hasPendingCommand: HasPendingCommand,
                isPresenting: IsPresenting(hasBattle),
                isInputAvailable: IsInputBound && !_faults.IsFaulted && _entry != null && _entry.IsActive && _display != null,
                isPathTarget: _interaction != null && CombatInputRouter.ResolvePointerTarget(_layers.CurrentLayer, _interaction.IsAttackActive, ExecutionMode) == CombatInputRouter.PointerTarget.Path);
        }

        /// <summary>결과를 재생·후처리 중이거나 확인하지 않은 결과가 남았는지입니다(Playback busy 또는 S &gt; P).</summary>
        private bool IsPresenting(bool hasBattle)
        {
            return _playback.IsBusy || (hasBattle && _state.LatestResultSequence > _inputRead.PresentedResultSequence);
        }

        /// <summary>입력 Buffer의 현재 요청 번호와 같은 Core 응답만 돌려줍니다(설계 §5.4·§21.1). 없으면 null입니다.</summary>
        private CombatPreviewResult ReadCurrentPreviewAnswer()
        {
            return _inputRead.HasPreview
                && _state.TryGetLatestPreview(out CombatPreviewResult answer)
                && answer.RequestNumber == _inputRead.Preview.Number
                ? answer
                : null;
        }

        /// <summary>
        /// 이 Tick·후처리를 계속해도 되는지입니다. 외부 호출(연출·표시·Layer) 안에서 오류 보고·Detach가 일어나면
        /// 재생 세대가 끊기므로, 외부 호출에서 돌아올 때마다 이것을 다시 봅니다.
        /// </summary>
        private bool CanContinue()
        {
            return !_faults.IsFaulted && _entry != null && _entry.IsActive && _display != null && _playback.IsAttached;
        }

        /// <summary>
        /// 종료(Completion·Finished)면 새 제출을 닫고, 포기 버튼 가능 여부를 맞춥니다(D12).
        /// 첫 그리기와 새 결과를 처음 관측할 때 부릅니다. 이탈 시작은 골격이 <see cref="SuspendSubmissions"/>로 알립니다.
        /// </summary>
        private void SyncSubmissionState()
        {
            // 정상 종료면 새 입력만 닫는다. Attach·재생·명령 대기는 유지하고 결과 소비를 계속한다(§5.6, W15).
            if (CombatInputPolicy.IsTerminal(ReadFacts()))
                SuspendSubmissions();
            if (CanContinue())
                _abandon.Sync();
        }

        private bool IsCurrent(CombatPlaybackToken token)
        {
            return !_faults.IsFaulted && _entry.IsActive && _playback.IsCurrent(token);
        }

        private static void ThrowIfAny(List<Exception> errors, string message)
        {
            if (errors != null)
                throw new AggregateException(message, errors);
        }

        #endregion

        public bool HasPendingPresentation => _display != null && IsPresenting(_state.HasBattle);

        // ICombatProgressView(재구성 설계 J12). Buffer에 없는 View 상태만 내보낸다.
        public bool IsPlaybackBusy => _playback.IsBusy;
        public bool IsSubmissionSuspended => _submissionsSuspended;
        public CombatCompletionDisplay CompletionDisplay { get; private set; }

        /// <summary>사용자가 포기를 확인했는지입니다. 씬 연결·해제 때 false로 돌아갑니다.</summary>
        public bool IsAbandonRequested { get; private set; }

        /// <summary>시작/턴 종료 결과를 기다리는 중인지입니다(읽기 전용, Gate 값).</summary>
        internal bool HasPendingCommand => _gate != null && _gate.HasPending;

        /// <summary>테스트 입구: 입력 수신 창구입니다(개선 설계 D11, Q4). 운영 입력은 슬롯을 거쳐 이 객체로 옵니다.</summary>
        internal CombatInputReceiver Input => _receiver;

        /// <summary>입력 슬롯에 연결돼 있는지입니다(수신 창구의 값).</summary>
        private bool IsInputBound => _receiver != null && _receiver.IsBound;

        // ICombatViewService 입력 멤버(서비스 계약 유지, D10 철회). 씬 포커스·HUD 복귀 버튼이 부르며 수신 창구로 넘긴다.
        public void FocusLost() => _receiver?.FocusLost();
        public void CaptureLost() => FocusLost();
        public void RequestReturn() => _receiver?.RequestReturn();

        /// <summary>
        /// Drag·조준·경로 같은 입력 중 표시를 정리합니다. 대기·명령 Buffer는 보존합니다.
        /// 포기 확인창을 열 때, 정상 종료의 제출 정지, 입력 Layer 해제·변경 때 씁니다.
        /// </summary>
        private void CancelInputPresentation()
        {
            _pathInput?.CancelInputs();
            _interaction?.CancelPresentation();
        }
    }
}
