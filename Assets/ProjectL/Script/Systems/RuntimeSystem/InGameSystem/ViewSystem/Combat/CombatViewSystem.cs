using ProjectL.Systems.ServiceKeys.Runtime;
using System;
using Unity.Profiling;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem;
using System.Collections.Generic;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Currency;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Current;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Run;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Tutorial;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Data;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.View;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.LoadingSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageInput;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.Base;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Hud;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Map;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.ActionButtons;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Assist;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Interaction;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.UiEntry;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Selection;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Entry;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat
{
    /// <summary>
    /// 전투 View의 조정자입니다(설계 §4, W2·W16). 씬 연결·첫 그리기와, 결과 하나의 재생 → 표시 반영 → 입력 정리 → 확인 순서를 정합니다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 맡는 것: Framework 수명, 씬 Attach(첫 그리기 포함, 개선 설계 D13)·Detach·오류 정지(<see cref="StopInput"/>),
    /// Tick의 결과 소비 순서(<c>FinalizeResult</c>), 목표 입력 Layer 동기화, 입력 판단용 사실(<see cref="CombatInputFacts"/>) 읽기 입구(<c>ReadFacts</c>),
    /// 진행 사실(<see cref="CombatViewProgress"/>)이 알리는 화면·입력 정리(호스트 <see cref="ICombatProgressHost"/>), 종료 결과를 처음 볼 때 입력 해제(D12, C 단계 C-4).
    /// </para>
    /// <para>
    /// 맡기는 것: 재생 수명 <see cref="CombatResultPlayback"/>, 변경 관측 <see cref="CombatViewRefresh"/>, 허용 판단 <see cref="CombatInputPolicy"/>,
    /// 입력 연결 <see cref="CombatInputConnection"/>(수신 창구 <see cref="CombatInputReceiver"/>·Layer·행동 버튼 입구, 3라운드 W11-1),
    /// 불러온 씬 <see cref="CombatLoadedScene"/>(씬 루트·배경, 3라운드 W11-3),
    /// 연결 입력 묶음 <see cref="CombatSceneInput"/>(입력 분배 <see cref="CombatInputRouter"/>·시작/턴 종료 대기 <see cref="CombatCommandGate"/>·
    /// 입력 기능 묶음 <see cref="CombatInteraction"/>·덱/아이템 입구·환경 호버·거절 안내·배치 진입·턴 종료 확인·행동 가용성·카메라 조작·이동 되돌리기,
    /// View 2라운드 W6-1·3라운드 W11-2),
    /// 씬 연결 한 번의 상태 <see cref="CombatViewConnection"/>(표시·입력 표면 <see cref="CombatInputSurfaces"/>·연결 입력 묶음·정보창·선택 읽기·덱 호버·루트 구독·
    /// 진입 연출·선택 유닛 준비, 3라운드 W11-4),
    /// 표시 값 읽기 <see cref="CombatDisplayValueReader"/>(재화·재고·현재 진행, W6-3), 입력 사실 읽기 <see cref="CombatInputFactsReader"/>,
    /// 튜토리얼 막힘 보고 <see cref="CombatBlockedInputReport"/>(W6-4),
    /// 표시·배경과 연결 입력 묶음의 생성 계약 <see cref="ICombatSceneFactory"/>·<see cref="ICombatSceneInputFactory"/>(생성자로 받음, W6-6),
    /// 진행 사실 <see cref="CombatViewProgress"/>(제출 정지·완료 표시·포기 요청, C 단계 C-2), 포기 요청 입구 <see cref="CombatAbandonController"/>,
    /// 그리기 <see cref="ICombatBattleDisplay"/>(값만 넘김). 같은 상태를 여기에 복제하지 않습니다.
    /// </para>
    /// <para>
    /// Core 소유 Buffer에는 쓰지 않습니다. CombatViewBuffer(처리 번호까지 삭제·결과 확인)에만 쓰고, Tutorial 요청 Buffer(막힌 입력 안내)는 막힘 보고 객체가 씁니다.
    /// 입력 Buffer 전체 비우기(Clear)는 씬 연결·해제 때 이 조정자가 합니다(쓰는 주인이 비움, 재구성 설계 D4-a).
    /// 그릴 값도 표시 값 읽기가 Buffer에서만 읽고 Core를 호출하지 않습니다.
    /// </para>
    /// </remarks>
    internal sealed class CombatViewSystem :
        ICombatViewService,
        ICombatProgressHost,
        ISystemInitData<IHasViewQuery>,
        ISystemPostBindService,
        ISystemInitialize,
        ISystemTick,
        ISystemShutdown,
        IExportSystemServices
    {
        private readonly RuntimeFaultBoundary _faults;
        private static readonly ProfilerMarker FinalizeMarker = new("Combat.Result.Finalize");
        private readonly CombatResultPlayback _playback;
        private readonly ICombatPresentation _presentation;
        // 표시·배경과 연결 입력 묶음의 생성 계약입니다(View 2라운드 W6-6). 정적 Factory를 직접 부르지 않습니다.
        private readonly ICombatSceneFactory _sceneFactory;
        private readonly ICombatSceneInputFactory _inputFactory;
        // 씬 로드(SceneNodeOrder.CombatBackdrop)에서 찾은 루트와 그때 세운 배경입니다(View 3라운드 W11-3). 씬 수명(씬 로드 ~ 씬 이탈 ReleaseScene)이며,
        // 표시 연결 뒤에만 생기는 연결 상태의 씬 루트 구독과 다른 사실입니다(배경 설계 BD2·BD3).
        private readonly CombatLoadedScene _scene;
        // 씬 연결 한 번의 상태(표시·입력 표면·연결 입력 묶음·정보창·선택 읽기·덱 호버·루트 구독·첫 그리기·전투 번호·진입 연출·선택 유닛 준비, View 3라운드 W11-4).
        // 표시를 인수하는 순간(TakeDisplay의 마지막 문장) 만들고 Detach가 정리 순서대로 비운 뒤 버린다. 연결이 없으면 null이다.
        private CombatViewConnection _connection;
        private readonly CombatViewRefresh _refresh = new();

        private ICombatStateBufferReadSink _state;
        private ICombatResultReadSink _results;
        private ICharacterBufferReadSink _characters;
        // 그릴 값(재화·재고·현재 진행)을 Buffer에서 읽는 곳입니다. 서비스를 받을 때 만듭니다(View 2라운드 W6-3).
        private CombatDisplayValueReader _displayValues;
        private CombatUnitPreparationReader _unitPreparationReader;
        private ICombatViewBufferReadSink _inputRead;
        private ICombatViewBufferWriteSink _inputWrite;
        // 튜토리얼의 전투 입력 제한 읽기와 막힌 입력 알림입니다. 서비스를 받을 때 만듭니다(View 2라운드 W6-4).
        private CombatBlockedInputReport _blockedInput;
        private ILodingRegisterService _loadingRegister;
        private ILodingSystemService _loading;

        // 전투 표시가 쓰는 View DB·Global 서비스 묶음입니다. 이 조정자는 넘기기만 하고 개별 표·서비스를 알지 않습니다(리팩토링 설계 D4).
        // 예외 하나: 입력 생성 재료에 효과음 서비스(Audio)를 꺼내 넣습니다(View 2라운드 Vf2.5, 3라운드 W12-1).
        private CombatDisplayTables _displayTables;
        private CombatDisplayServices _displayServices;

        // 전투 진행 골격이 읽는 View 쪽 진행 사실의 원본(제출 정지·완료 표시·포기 요청, C 단계 C-2). 조정자는 묻고 시키기만 한다.
        private readonly CombatViewProgress _progress;

        // Option 등 다른 UI가 확인한 포기 요청의 입구(C 단계 C-1). 요청 깃발은 진행 사실이 갖고 전투 Plan이 읽는다(재구성 설계 J8).
        private readonly CombatAbandonController _abandon;

        // 손패 카드 호버·덱 배치·가방 아이템 입구입니다(현강님 UI가 읽음). 연결 상태가 갖고, 연결이 없거나 정지 뒤에는 null입니다.
        public ICombatDeckHoverInput DeckHover => IsEntryPresentationPending ? null : _connection?.DeckHover;
        public ICombatDeckPlacementInput DeckPlacement => _connection?.Input?.DeckPlacement;
        public ICombatItemInput ItemInput => _connection?.Input?.ItemInput;
        // 거절 안내·환경 호버·배치 확인창이 쓰는 텍스트 표시입니다. 입력 생성 재료에 넣습니다.
        private IUITextPresentation _textPresentation;
        private IUIConfirmationPresentation _confirmationPresentation;
        // 미리보기 응답 단계의 사실 공급자입니다. 입력 기능 묶음이 감시 단계 뒤에 부릅니다(View 2라운드 W5-3).
        private readonly Func<CombatInputFacts> _readFollowUpFacts;
        // 연결 입력 묶음의 서비스 수명 재료입니다(View 3라운드 W12-1). 첫 입력 생성 때 한 번 만듭니다(표 = InitData, 나머지 = BindServices 뒤).
        private CombatSceneInputSources _inputSources;
        // 입력 연결(수신 창구·Layer·행동 버튼 입구, View 3라운드 W11-1). 서비스 수명이며 서비스를 받을 때 만든다(계획 TP-22).
        private CombatInputConnection _inputConnection;
        // 서비스 바인딩 하나의 사실 읽기입니다. 현재 씬 연결은 매 호출 공급자로 읽습니다.
        private CombatInputFactsReader _inputFactsReader;

        /// <param name="sceneFactory">표시·배경 생성 계약입니다. 운영 구현은 <c>CombatSceneFactory.Instance</c>입니다.</param>
        /// <param name="inputFactory">연결 입력 묶음 생성 계약입니다. 운영 구현은 <c>CombatSceneFactory.Instance</c>입니다.</param>
        internal CombatViewSystem(
            RuntimeFaultBoundary faultBoundary,
            ICombatPresentation presentation,
            ICombatSceneFactory sceneFactory,
            ICombatSceneInputFactory inputFactory)
        {
            _faults = faultBoundary ?? throw new ArgumentNullException(nameof(faultBoundary));
            _presentation = presentation;
            _playback = new CombatResultPlayback(presentation);
            _sceneFactory = sceneFactory ?? throw new ArgumentNullException(nameof(sceneFactory));
            _inputFactory = inputFactory ?? throw new ArgumentNullException(nameof(inputFactory));
            _scene = new CombatLoadedScene(_sceneFactory);
            // 응답 단계의 사실 공급자는 한 번만 만든다. 입력 객체가 쓰는 공급자는 입력 생성 재료가 한 번 갖는다(CreateInput).
            _readFollowUpFacts = () => ReadFacts(followUp: true);
            // 진행 사실은 생성자에서 만든다. 골격 사실 읽기가 서비스 연결(PostBind) 때 Progress를 잡아 두기 때문이다.
            _progress = new CombatViewProgress(this);
            // 포기 요청은 UI 입구라 수명·활성 판단을 매번 이 조정자에게 묻는다.
            // 수명 판단은 이름 있는 판정식(IsSceneActive)만 넘긴다(구조 개선 SV2·E5). 입력 정리는 진행 사실이 호스트(OnInputClosed)로 부른다.
            _abandon = new CombatAbandonController(_faults, () => ReadFacts(),
                () => IsSceneActive,
                _progress);
        }

        /// <summary>Combat 씬의 SceneRoot에 연결돼 있는지입니다.</summary>
        public bool IsAttached => _connection != null && _playback.IsAttached;
        private ICombatEntryRead _entry;

        #region 시스템 모듈

        /// <summary>전투 표시에 필요한 View DB 조회를 묶어 보관합니다. 어떤 표가 필요한지는 <see cref="CombatDisplayTables"/>가 정합니다.</summary>
        public void InitData(IHasViewQuery data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            _displayTables = CombatDisplayTables.From(data.ViewQuery);
            // 입력 생성 재료가 표를 갖고 있으므로 다음 입력 생성 때 다시 묶는다(View 3라운드 W12-1).
            _inputSources = null;
        }

        public void PostBindService(ISystemServiceStoreReader resolver)
        {
            if (resolver == null)
                throw new ArgumentNullException(nameof(resolver));

            // IBufferQuery는 모든 싱크를 노출하므로 이 시스템에 필요한 것만 꺼내 보관한다.
            IBufferQuery buffers = resolver.GetService(BufferSystemKey.Buffer);
            var inGame = resolver.GetService(SystemGroupsService.InGame);
            var global = resolver.GetService(SystemGroupsService.Global);
            var lifeCycles = resolver.GetService(SystemGroupsService.LifeCycle);
            // 확인창 기능도 BindServices에서 검사하여 운영/시험의 전체 조립이 같은 필수 계약을 따릅니다.
            BindServices(
                buffers.CombatStateReadSink,
                buffers.CombatResultReadSink,
                buffers.CharacterReadSink,
                buffers.CurrentProgressReadSink,
                buffers.CurrencyReadSink,
                buffers.CombatViewReadSink,
                buffers.CombatViewWriteSink,
                inGame.ViewGroup.Input.Stage,
                global.InputServiceCommand.InputLayerServices,
                global.LodingServiceCommand.LodingRegisterService,
                lifeCycles.CombatEntry,
                // 표시가 쓰는 Render·Audio·UI 서비스는 묶음이 고른다(구조 개선 SV5·E16).
                CombatDisplayServices.From(global),
                global.UITextPresentation,
                buffers.ItemInventoryReadSink,
                buffers.TutorialStateReadSink ?? throw new InvalidOperationException("[CombatViewSystem] Tutorial 상태 읽기 Sink가 필요합니다."),
                buffers.TutorialRequestWriteSink ?? throw new InvalidOperationException("[CombatViewSystem] Tutorial 요청 쓰기 Sink가 필요합니다."),
                global.LodingServiceCommand.LodingService ?? throw new InvalidOperationException("[CombatViewSystem] 로딩 완료 상태 서비스가 필요합니다."),
                buffers.RunDeckReadSink ?? throw new InvalidOperationException("[CombatViewSystem] 확정 RunDeck 읽기가 필요합니다."));
        }

        /// <summary>필요한 싱크와 입력·로딩 서비스, 표시 서비스 묶음을 보관합니다. 로딩 상태는 진입 연출 없는 시험 조립에서만 생략할 수 있습니다.</summary>
        internal void BindServices(
            ICombatStateBufferReadSink state,
            ICombatResultReadSink results,
            ICharacterBufferReadSink characters,
            ICurrentProgressBufferReadSink current,
            ICurrencyBufferReadSink currency,
            ICombatViewBufferReadSink inputRead,
            ICombatViewBufferWriteSink inputWrite,
            IStageInputReceiverSlot slot,
            IInputLayerService layers,
            ILodingRegisterService loadingRegister,
            ICombatEntryRead entry,
            CombatDisplayServices displayServices,
            IUITextPresentation textPresentation,
            IItemInventoryReadSink items,
            ITutorialStateBufferReadSink tutorialState,
            ITutorialRequestBufferWriteSink tutorialRequests,
            ILodingSystemService loading = null, IRunDeckReadSink runDeck = null)
        {
            // 재바인딩이 실패해도 이전 서비스의 사실/입력 재료를 다시 쓰지 않습니다.
            _inputFactsReader = null;
            _inputSources = null;
            _confirmationPresentation = null;
            _loading = loading;
            // 운영 입구와 같은 인자를 모두 필수로 받습니다(View 2라운드 W6-7). 시험 조립은 가짜를 넘깁니다.
            _blockedInput = new CombatBlockedInputReport(tutorialState, tutorialRequests);

            _state = state ?? throw new ArgumentNullException(nameof(state));
            _results = results ?? throw new ArgumentNullException(nameof(results));
            _characters = characters ?? throw new ArgumentNullException(nameof(characters));
            // 씬을 만들지 않는 격리 조립만 생략할 수 있습니다. 운영 PostBindService는 필수로 전달합니다.
            _unitPreparationReader = runDeck != null ? new CombatUnitPreparationReader(runDeck, characters) : null;
            if (current == null)
                throw new ArgumentNullException(nameof(current));
            if (currency == null)
                throw new ArgumentNullException(nameof(currency));
            _displayValues = new CombatDisplayValueReader(_state, _characters, currency, current, items);
            _inputRead = inputRead ?? throw new ArgumentNullException(nameof(inputRead));
            _inputWrite = inputWrite ?? throw new ArgumentNullException(nameof(inputWrite));
            if (slot == null)
                throw new ArgumentNullException(nameof(slot));
            if (layers == null)
                throw new ArgumentNullException(nameof(layers));
            _loadingRegister = loadingRegister ?? throw new ArgumentNullException(nameof(loadingRegister));
            _displayServices = displayServices ?? throw new ArgumentNullException(nameof(displayServices));
            _textPresentation = textPresentation ?? throw new ArgumentNullException(nameof(textPresentation));
            _confirmationPresentation = textPresentation is IUIConfirmationPresentation confirmation
                ? confirmation
                : throw new InvalidOperationException("[CombatViewSystem] 텍스트 표시 서비스가 확인창(IUIConfirmationPresentation)을 제공해야 합니다.");
            _entry = entry ?? throw new ArgumentNullException(nameof(entry));
            // 수신 창구는 오류·연결·확인창을 자기 안에서 먼저 검사한다(D11). 여기서는 씬 연결 활성만 묻는다.
            // 인자 검사가 모두 끝난 뒤 만든다. 중간 인자가 없어 실패하면 입력 연결을 남기지 않는다(옛 수신 창구 자리).
            _inputConnection = new CombatInputConnection(slot, layers, _faults,
                () => IsSceneActive,
                () => IsModalOpen,
                RouteInput,
                CancelInputPresentation);
            _inputFactsReader = _inputFactory.CreateFactsReader(_state, _inputRead, _inputConnection, _playback, _progress,
                _blockedInput, () => _connection, () => IsInputReady, () => HasPendingCommand, () => IsModalOpen)
                ?? throw new InvalidOperationException("[CombatViewSystem] 입력 사실 읽기 조립 결과가 필요합니다.");
        }

        /// <summary>Combat 씬 번호에 배경 세우기·연결(Enter)과 정리(Exit) 처리를 등록합니다(W2, 배경 설계 BD1).</summary>
        public void Initialize()
        {
            if (_loadingRegister == null)
                throw new InvalidOperationException("[CombatViewSystem] 서비스를 받기 전에 초기화할 수 없습니다.");

            // 순서 값과 그 관계(카메라 수집 전 배경, LifeCycle 뒤 연결·앞 해제)의 원본은 SceneNodeOrder다.
            _loadingRegister.RegisterSceneEnterLoaded(SceneIndices.Combat, new LodingNode(SceneNodeOrder.CombatBackdrop, OnCombatSceneLoaded));
            _loadingRegister.RegisterSceneEnterLoaded(SceneIndices.Combat, new LodingNode(SceneNodeOrder.CombatViewAttach, OnCombatSceneEntered));
            _loadingRegister.RegisterSceneExitLoaded(SceneIndices.Combat, new LodingNode(SceneNodeOrder.CombatViewRelease, ReleaseScene));
            if (_unitPreparationReader != null)
                _loadingRegister.RegisterScenePreparation(SceneIndices.Combat, new LodingPreparationNode(GetScenePreparation));
        }

        private ILodingPreparation GetScenePreparation()
        {
            if (!CanContinue() || !_connection.Shown || _connection.BattleInstance != _state.BattleInstanceId ||
                _connection.Preparation == null)
                throw new InvalidOperationException("[CombatViewSystem] 설치된 현재 연결의 선택 유닛 준비가 필요합니다.");
            return _connection.Preparation;
        }

        /// <summary>
        /// 전투 확인 → 연출 진행 → 변경 관측 → (종료 결과면 입력 해제) → 가용성 조회 → 경로 미리보기 → 응답 반영 → 결과 재생·후처리 순서입니다.
        /// 첫 그리기는 Attach가 하고(D13), 첫 그리기가 성공하기 전에는 전투 확인 뒤 바로 멈춥니다(SV1).
        /// 종료(완료 있음)는 첫 그리기·새 결과를 처음 관측할 때·결과 후처리의 Layer 맞춤에서 봅니다(D12). 새 제출 정지는 전투 Plan이 정합니다(C 단계 설계 C-4).
        /// 행동 버튼은 Tick 끝에서 한 번 파생합니다(SV6).
        /// 예상한 계약 위반과 연출 실패는 오류 경계에 보고하고, 예상하지 못한 예외는 Bootstrap까지 전파합니다.
        /// </summary>
        public void Tick(float deltaTime)
        {
            TickCore(deltaTime);
            // 행동 버튼(공격·턴 종료)은 여러 입력 경로(키·UI 버튼·개발 Layer·결과 후처리)에서 바뀌는 View 상태라
            // 갱신 지점을 따로 두지 않고 Tick 끝에서 현재 사실로 한 번 파생한다. 패널은 같은 값이면 다시 쓰지 않는다(구조 개선 SV6, D12 적용 범위 보완).
            // 입력 닫힘(제출 정지·포기 요청)은 호스트(OnInputClosed)가 그 자리에서 숨기고, 여기서는 같은 사실로 숨김이 유지된다.
            // 오류 정지·해제 뒤에는 정지(StopInput)·해제가 이미 숨겼으므로 쓰지 않는다.
            if (CanContinue() && _state.HasBattle)
            {
                SyncActionButtons();
                SyncEnvironmentHover();
                // 카메라 조작은 매 프레임 한 번 진행한다. 움직일지는 입력 사실로 정책이 정한다(카메라 조작 설계 CC6).
                // 시점이 바뀌면 묶음이 마지막 포인터로 칸을 다시 집는다(평가 C10-01).
                _connection?.Input?.TickCamera(ReadFacts(), deltaTime, _characters.Characters);
            }
        }

        private void TickCore(float deltaTime)
        {
            if (!CanContinue())
                return;

            // 모든 결과는 정리 뒤 표시한다. 이 전투의 완료를 보이는 동안 마지막 화면만 유지한다.
            // 전투 갱신·연출·입력은 진행하지 않으며, 확인 뒤 실제 씬 이탈이 연결과 표시를 정리한다.
            if (IsHoldingResult())
                return;

            // 그 밖에 전투가 사라지거나 다른 전투로 바뀌는 것은 정상 흐름에 없다. 정리와 완료 표시는 한 Job 안에서 이어지고
            // (CombatNextJob이 CombatPlan.Settle이 만든 목록을 차례로 실행), 같은 씬에 다음 전투를 다시 붙이지 않는다(D9).
            // 조용히 끊으면 골격이 "연결 안 됨"으로 오류 없이 멈추므로, 원인을 먼저 F1로 보고한 뒤 표시를 비우고 씬 연결을 끊는다(C 단계 설계 C-5).
            if (!_state.HasBattle || _connection.BattleInstance != _state.BattleInstanceId)
            {
                _faults.Report(new InvalidOperationException(_state.HasBattle
                    ? "[CombatViewSystem] 연결한 전투가 아닌 다른 전투가 설치됐습니다."
                    : "[CombatViewSystem] 완료를 보이기 전에 전투가 정리됐습니다."));
                if (!_state.HasBattle)
                    _connection.Display.Clear();
                Detach();
                return;
            }

            // 첫 그리기(Install)가 성공하기 전에는 연출·응답·결과를 진행하지 않는다.
            // 설치에 실패한 표시는 반쯤 설치된 상태라 재생·후처리를 시도하지 않고 Detach의 반환을 기다린다(구조 개선 SV1·E23).
            if (!_connection.Shown)
                return;

            if (IsEntryPresentationPending)
            {
                if (_loading == null)
                    throw new InvalidOperationException("[CombatViewSystem] 보드 진입 연출에는 로딩 완료 상태 서비스가 필요합니다.");
                CombatViewConnection connection = _connection;
                // 준비는 LoadingSystem이 진행합니다. 보드 연출의 로딩 대기와 상호 대기하지 않습니다.
                if (connection.IsPreparationPending) return;
                connection.EntryPresentation?.Tick(deltaTime, _loading.IsLoading);
                if (!CanContinue() || !ReferenceEquals(connection, _connection))
                    return;
                if (!IsEntryPresentationPending)
                    SyncInputLayer();
                return;
            }

            _presentation.Tick(deltaTime);
            if (!CanContinue())
                return;

            // 보드 연출이 없는 씬도 준비/Reveal 대기가 끝나면 기존 입력 사실에 따라 연결합니다.
            if (!IsInputBound && CombatInputPolicy.GetDesiredLayer(ReadFacts()) != default)
            {
                SyncInputLayer();
                if (!CanContinue()) return;
            }

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

            // 새 결과가 승패를 기록했으면 입력 연결만 바로 놓는다(목표 Layer 없음 → 표시 취소·해제). 명령·자동 진행의 승패는 결과와 함께
            // 게시되므로 새 결과를 처음 볼 때 한 번 본다(D12). 결과 없이 생기는 포기 완료는 전투 Plan의 포기 줄이 그 전에 제출을 이미 정지했다.
            // 새 제출 정지는 전투 Plan이 정한다(C 단계 설계 C-4).
            // 종료가 아닌 결과에서 SyncInputLayer를 부르면 재생 전에 Layer가 바뀌므로 종료일 때만 부른다.
            if (signal.UnpresentedResult != null && !_playback.IsBusy && CombatInputPolicy.IsTerminal(ReadFacts()))
            {
                SyncInputLayer();
                if (!CanContinue())
                    return;
            }

            // 입력 기능 묶음의 Tick 입구 하나(View 2라운드 W5-3): 감시 단계(경로의 최신 응답·복귀 견적, 그 뒤 호버 공격 범위) 뒤,
            // 현재 요청의 새 응답이면 그때 응답 사실을 읽어 자기 요청인 소유자가 쓴다(§21.1).
            // 외부 입력이 아니므로 후처리 차단과 무관하며, 예외는 Tick 경로로 전파한다.
            CombatViewConnection inputConnection = _connection;
            SyncActionAvailability(inputConnection);
            if (!CanContinue() || !ReferenceEquals(_connection, inputConnection))
                return;
            // 가용성 응답 소비/슬롯 변경 뒤 현재 Facts를 새로 읽습니다.
            inputConnection.Input.TickInteraction(ReadFacts(), _characters.Characters,
                _state.TryGetLatestPreview(out var latestPreview) ? latestPreview : null,
                signal.PreviewAnswered, _readFollowUpFacts);
            if (!CanContinue() || !ReferenceEquals(_connection, inputConnection))
                return;
            SyncActionAvailability(inputConnection);
            if (!CanContinue() || !ReferenceEquals(_connection, inputConnection))
                return;

            if (signal.UnpresentedResult != null)
                ConsumeResult(signal.UnpresentedResult);
        }

        /// <summary>씬 연결을 정리합니다. 공용 입력 콜백의 연결·해제는 View 그룹이 소유합니다(P3.9).</summary>
        public void Shutdown() => ReleaseScene();

        public void ExportServices(ISystemWriteSink sink)
        {
            if (sink == null)
                throw new ArgumentNullException(nameof(sink));

            sink.AddService<ICombatViewService>(this);
            sink.AddModule<ISystemModule>(this);
        }

        #endregion

        #region 씬 수명(씬 로드 CombatBackdrop ~ 씬 이탈 ReleaseScene)

        /// <summary>
        /// Combat 씬 로드 직후(<see cref="SceneNodeOrder.CombatBackdrop"/>) 씬 루트를 찾고 배경 맵을 세웁니다.
        /// 카메라 다시 연결(<see cref="SceneNodeOrder.BindCameras"/>)이 맵 카메라도 수집하게 하려는 순서입니다(배경 설계 BD1·BD2).
        /// 진입 활성은 보지 않습니다. 이 시점의 진입은 활성화 중이고, 정당성은 LifeCycle 활성화(<see cref="SceneNodeOrder.LifeCycle"/>)와
        /// 연결(<see cref="SceneNodeOrder.CombatViewAttach"/>)이 검사합니다(Q-B1 A).
        /// 선택이 없거나 키에 맞는 배경이 없으면 배경을 두지 않습니다.
        /// </summary>
        private void OnCombatSceneLoaded()
        {
            if (_faults.IsFaulted)
                return;
            if (_displayTables == null || _displayValues == null)
                throw new InvalidOperationException("[CombatViewSystem] View DB·서비스를 받기 전에 씬을 연결할 수 없습니다.");
            // 앞 씬 수명의 배경 놓기 → 루트 찾기 → 배경 세우기는 불러온 씬이 합니다(View 3라운드 W11-3).
            _scene.Load(_displayTables, _displayValues);
        }

        private void OnCombatSceneEntered()
        {
            if (_faults.IsFaulted)
                return;
            if (!_entry.IsActive)
                throw new InvalidOperationException("[CombatViewSystem] 진입 수락과 LifeCycle 활성화가 필요합니다.");
            // 루트는 씬 로드(SceneNodeOrder.CombatBackdrop)에서 찾아 두었다(배경 설계 BD2).
            if (_scene.Root == null)
                throw new InvalidOperationException("[CombatViewSystem] 씬 로드 Node(SceneNodeOrder.CombatBackdrop)가 Combat 씬 루트를 먼저 찾아야 합니다.");

            Attach(_scene.Root);
        }

        /// <summary>
        /// 씬 수명의 끝입니다. 표시 연결을 놓은 뒤(<see cref="Detach"/>, 카메라 창구가 맵 카메라를 비움) 배경을 없애고 씬 로드 때 찾은 루트를 놓습니다.
        /// Combat Exit Node와 런타임 종료가 부릅니다. 하나가 실패해도 나머지를 시도하고 모아서 던집니다. 여러 번 불려도 안전합니다(배경 설계 BD4).
        /// </summary>
        private void ReleaseScene()
        {
            List<Exception> errors = null;
            CleanupErrors.Try(Detach, ref errors);
            CleanupErrors.Try(_scene.Release, ref errors);
            CleanupErrors.ThrowIfAny(errors, "[CombatViewSystem] Combat 씬 연결 수명 정리 중 실패했습니다.");
        }

        #endregion

        #region 연결 수명(표시 연결 TakeDisplay ~ 해제 Detach)

        /// <summary>
        /// SceneRoot에 연결합니다. 사전 검사 뒤 Factory가 조립한 표시를 <see cref="AttachCreated"/>로 넘겨받습니다.
        /// </summary>
        internal void Attach(CombatSceneRoot root)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));
            if (_faults.IsFaulted)
                return;
            if (_displayTables == null)
                throw new InvalidOperationException("[CombatViewSystem] View DB를 받기 전에 씬에 연결할 수 없습니다.");
            if (_displayServices == null || _state == null)
                throw new InvalidOperationException("[CombatViewSystem] 서비스를 받기 전에 씬에 연결할 수 없습니다.");
            // 표시 구현(Presenter 설정)을 만들기 전에 확인한다. 실패하면 씬 참조를 건드리지 않는다.
            RequireActiveBattle();
            ThrowIfDisplayAttached();

            // 씬 로드 때 같은 루트에 세운 배경만 넘긴다. 다른 루트로 직접 붙이는 조립(시험)은 남의 배경 카메라를 쓰지 않는다.
            StageViewPropPresenter backdrop = _scene.BackdropFor(root);
            if (_unitPreparationReader == null)
                throw new InvalidOperationException("[CombatViewSystem] 실제 씬 연결에는 확정 덱의 준비 대상 읽기가 필요합니다.");
            CombatUnitPreparationInput preparationInput = _unitPreparationReader.Read(_state.BattleInstanceId);
            // 씬 표시는 정보창 선택 창구도 겸한다. 표시 계약에 넣지 않고 여기서 따로 넘긴다(View 단계 설계 P6, 형식 검사 없음).
            CombatSceneDisplay created = _sceneFactory.CreateDisplay(root, _displayTables, _displayServices, backdrop);
            CombatUnitPreparation preparation;
            try
            {
                preparation = created.CreateUnitPreparation(preparationInput, () =>
                    CanContinue() && ReferenceEquals(_connection.Display, created) && _state.HasBattle &&
                    _state.BattleInstanceId == preparationInput.BattleInstance && _unitPreparationReader.IsCurrent(preparationInput));
            }
            catch (Exception error)
            {
                ReleaseUnowned(created, error);
                throw;
            }
            AttachCreated(created, root, created, created.EntryPresentation, preparation);
        }

        /// <summary>
        /// Factory가 막 만든 표시의 소유권을 넘겨받습니다(구조 개선 SV1). 소유권은 <see cref="TakeDisplay"/>가
        /// 연결 상태(<c>_connection</c>)를 만드는 순간(그 메서드의 마지막 문장) 넘어오며, 그 뒤의 실패(첫 그리기 등)는 Detach가 반환합니다.
        /// 그 전에 실패하거나 오류 상태로 인수하지 않고 돌아오면 이 자리에서 연출 연결을 끊고 표시를 반환합니다.
        /// 씬 루트 이벤트는 인수한 표시가 아직 연결돼 있을 때만 구독합니다.
        /// </summary>
        /// <remarks>
        /// 반환은 살아 있는 다른 표시가 없을 때만 합니다. 같은 씬 객체(유닛·고스트·필드)를 공유하므로
        /// 연결된 표시가 있는데 새 표시를 반환하면 그 표시의 대여 자원을 빼앗게 됩니다(호출자가 중복 연결을 먼저 거절합니다).
        /// 재생 연결(<c>_playback.Attach</c>)은 되돌리지 않습니다. 다음 연결이 Abort를 거쳐 새 세대를 시작합니다.
        /// </remarks>
        /// <param name="root">구독할 씬 루트입니다. 씬 없이 조립하는 테스트는 null을 넘깁니다.</param>
        /// <param name="inspection">필수 선택 누름/읽기 계약입니다. 시험도 명시적으로 제공합니다.</param>
        internal void AttachCreated(ICombatBattleDisplay created, CombatSceneRoot root, ICombatUnitInspection inspection,
            ICombatEntryPresentation entryPresentation = null, ILodingPreparation preparation = null)
        {
            if (created == null)
                throw new ArgumentNullException(nameof(created));

            bool owned;
            ICombatSelectionReader selection;
            try
            {
                owned = TakeDisplay(created, inspection, out selection, entryPresentation, preparation);
            }
            catch (Exception error) when (_connection == null)
            {
                // 인수 전 실패이고 살아 있는 다른 표시도 없다(중복 연결 거절은 연결 상태가 있어 여기로 오지 않는다).
                ReleaseUnowned(created, error);
                throw;
            }

            if (!owned)
            {
                // 오류 상태라 인수하지 않고 정상 반환했다. 주인이 없는 표시를 남기지 않는다.
                ReleaseUnowned(created, null);
                return;
            }

            // 표시를 인수한 뒤에만 둔다. 첫 그리기 안의 재진입 Detach가 연결 상태와 함께 버린다.
            CombatViewConnection connection = _connection;
            connection.BeginUse(inspection, selection,
                root != null && root.FieldUnits != null ? new CombatDeckHoverInput(_characters, root.FieldUnits, _faults) : null);
            ContinueAttach();
            // 첫 그리기 안의 재진입 Detach로 이미 놓았으면 구독하지 않는다.
            if (root == null || !ReferenceEquals(_connection, connection))
                return;
            // 입력 표면의 BattleChanger 입력 연결 → 포커스 상실·비활성화 구독 순서입니다(연결 상태가 함).
            connection.BindRoot(root, _inputConnection.Receiver, FocusLost, OnCombatSceneDisabled);
        }

        /// <summary>
        /// 인수하지 못한 표시를 D9 순서(연출 연결 끊기 → 표시 반환)로 정리합니다. 하나가 실패해도 나머지를 시도합니다.
        /// 정리 실패는 원인 예외가 있으면 원인을 맨 앞에 두고 함께, 없으면 단독으로 던집니다. 모두 성공하면 아무것도 던지지 않습니다.
        /// </summary>
        private void ReleaseUnowned(ICombatBattleDisplay display, Exception cause)
        {
            List<Exception> errors = null;
            // 연출이 이미 새 표시를 들고 있을 수 있다(연출 연결 뒤 실패). 연결 전이면 Detach는 아무것도 하지 않는다.
            CleanupErrors.Try(_presentation.Detach, ref errors);
            CleanupErrors.Try(display.Release, ref errors);
            if (errors == null)
                return;
            if (cause != null)
                errors.Insert(0, cause);
            throw new AggregateException("[CombatViewSystem] 인수하지 못한 표시를 정리하는 중 실패했습니다.", errors);
        }

        /// <summary>
        /// 표시에 연결하고 새 재생 세대를 시작합니다. 입력 연결은 첫 그리기 뒤에 합니다.
        /// 오류 정지 뒤에는 연결하지 않습니다(표시를 인수하지 않고 돌아옵니다). 테스트는 이 입구로 가짜 표시를 붙입니다.
        /// </summary>
        internal void AttachDisplay(ICombatBattleDisplay display, ICombatUnitInspection inspection)
        {
            if (TakeDisplay(display, inspection, out ICombatSelectionReader selection))
            {
                _connection.BeginUse(inspection, selection, null);
                ContinueAttach();
            }
        }

        /// <summary>
        /// 사전 검사 → 입력 Buffer 비우기 → 연출·재생 연결 → 이 연결 수명의 상태 초기화 → 연결 상태 만들기(소유권 이전) 순서입니다.
        /// 연결 상태 대입이 마지막 문장이라, 이 메서드가 예외로 끝나면 언제나 인수 전입니다. 오류 상태면 인수하지 않고 false를 돌려줍니다.
        /// </summary>
        private bool TakeDisplay(ICombatBattleDisplay display, ICombatUnitInspection inspection, out ICombatSelectionReader selection,
            ICombatEntryPresentation entryPresentation = null, ILodingPreparation preparation = null)
        {
            selection = null;
            if (display == null)
                throw new ArgumentNullException(nameof(display));
            ThrowIfDisplayAttached();
            if (_state == null)
                throw new InvalidOperationException("[CombatViewSystem] 서비스를 받기 전에 씬에 연결할 수 없습니다.");
            if (_faults.IsFaulted)
                return false;
            RequireActiveBattle();

            if ((entryPresentation != null || preparation != null) && _loading == null)
                throw new InvalidOperationException("[CombatViewSystem] 진입 연출을 연결하려면 로딩 완료 상태 서비스가 필요합니다.");

            if (inspection == null)
                throw new ArgumentNullException(nameof(inspection));
            selection = inspection.Selection ?? throw new InvalidOperationException("[CombatViewSystem] 표시의 선택 읽기 계약이 필요합니다.");

            // 새 전투 수명의 시작이다. 지난 전투·로딩 중에 남은 명령·미리보기를 버린다.
            // 번호 카운터와 연출 확인 번호(P)는 Buffer가 유지한다(재구성 설계 D4-a, K14).
            _inputWrite.Clear();
            _presentation.Attach(display.EventPlayback);
            _playback.Attach();
            BeginConnectionState();
            // 연결 상태(표시·입력 표면·전투 번호)를 만든다. 생성은 예외가 없어 이 대입이 마지막 실패 지점이자 소유권 이전이다(계획 TP-23).
            _connection = new CombatViewConnection(display, _state.BattleInstanceId, entryPresentation, preparation);
            return true;
        }

        /// <summary>
        /// 연결 시작의 상태입니다. 관측·진행 사실(제출 정지·완료 표시·포기 요청)을 새로 시작합니다. 첫 그리기·전투 번호는 새 연결 상태가 갖습니다.
        /// 입력 Buffer는 연출 연결 전에 이미 비웠습니다(연결이 실패해도 지난 전투의 명령은 버려진 채로 남게).
        /// </summary>
        private void BeginConnectionState()
        {
            _refresh.Reset();
            _progress.BeginConnection();
        }

        /// <summary>
        /// 연결 끝의 상태입니다. 시작과 같은 것을 되돌리되 제출 정지는 남깁니다(연결 사이에는 마지막 값 유지, C 단계 설계 P2).
        /// 첫 그리기·전투 번호는 연결 상태와 함께 이미 버렸습니다.
        /// </summary>
        private void EndConnectionState()
        {
            _progress.EndConnection();
            _refresh.Reset();
            _inputWrite.Clear();
        }

        /// <summary>인수한 표시에 버튼·패널을 연결하고 첫 그리기를 합니다. 여기서의 실패는 Detach가 표시를 반환합니다.</summary>
        private void ContinueAttach()
        {
            ICombatBattleDisplay display = _connection.Display;
            // 행동 버튼(공격·턴 종료·복귀)은 숨긴 채 연결하고, 보임·누름 가능은 입력 Layer를 맞춘 뒤(SyncInputLayer) Tick 끝에서 맞춘다(행동 버튼 설계 B6, SV6).
            display.BindActions(_inputConnection.AttackPressed, _inputConnection.EndTurnPressed, _inputConnection.ReturnPressed);
            display.SetActionsAvailable(default);

            ShowBattle();
        }

        /// <summary>
        /// 첫 그리기입니다(개선 설계 D13). 정적 필드를 구성하고 동적 값을 처음 적용한 뒤, 그 성공 뒤에만
        /// 입력 객체 생성 → 첫 그리기 성공 표시 → 현재 Core 단계의 Layer로 입력 연결을 합니다(§5.1·§6.1, SV4). 끝난 전투면 목표 Layer가 없어 잇지 않습니다.
        /// Install 안에서 오류 보고·Detach가 일어나면 그 자리에서 멈춥니다. Install 예외는 호출자(로딩 노드 → Bootstrap)로 전파합니다.
        /// </summary>
        private void ShowBattle()
        {
            CombatViewConnection connection = _connection;
            connection.Display.Install(_displayValues.Read());
            if (!CanContinue())
                return;

            CreateInput();
            // 입력 생성 안의 재진입 Detach로 이 연결이 이미 끝났으면 여기서 멈춘다(끝난 연결에 첫 그리기 성공을 세우지 않음).
            if (!ReferenceEquals(_connection, connection))
                return;
            // 입력 객체가 모두 생긴 뒤에만 세운다. 생성 중 예외가 나면 Tick이 결과·응답을 소비하지 않는다(SV1·SV4).
            connection.MarkShown();
            SyncInputLayer();
        }

        /// <summary>
        /// 이 씬 연결 수명의 입력 객체(연결 입력 묶음)를 첫 그리기 성공 뒤 한 번에 만듭니다(구조 개선 SV4, View 2라운드 W6-1).
        /// 입력 연결(Bind)보다 먼저입니다. 객체 여덟의 생성과 연결은 입력 Factory가 합니다.
        /// </summary>
        private void CreateInput()
        {
            CombatViewConnection connection = _connection;
            // 재료는 서비스 수명이라 첫 입력 생성 때 한 번 묶는다(View 3라운드 W12-1). 경로 판단에는 선행 미처리 명령도 읽는다.
            // 같은 형식의 인자(사실 공급자 둘, 판정 둘, 알림 둘)가 이어져 있어 이름으로 넘긴다(구조 원칙 평가 R5).
            _inputSources ??= new CombatSceneInputSources(
                tables: _displayTables,
                state: _state,
                characters: _characters,
                inputRead: _inputRead,
                inputWrite: _inputWrite,
                readPathFacts: () => ReadFacts(includeUnhandledCommand: true),
                readInputFacts: includeUnhandledCommand => ReadFacts(includeUnhandledCommand),
                canReceiveUiInput: CanReceiveDeckInput,
                reportBlockedInput: _blockedInput.Report,
                faults: _faults,
                audio: _displayServices.Audio,
                itemStock: _displayValues.ItemStock,
                text: _textPresentation,
                confirmation: _confirmationPresentation,
                isInputReady: () => IsInputReady,
                afterConfirmedSubmit: SyncActionButtons);
            // 유닛 HUD 아이템 아이콘 우클릭 해제는 묶음 생성이 모두 끝난 뒤 연결 상태가 건다(가방 설계 P6, 계획 검증 L11). 정리는 연결 상태의 입력 정지.
            connection.AttachInput(_inputFactory.CreateSceneInput(connection.Display, _inputSources));
        }

        /// <summary>입력 정지 뒤에도 남는 Display의 소유권을 확인해 중복 생성·연결을 거절합니다.</summary>
        private void ThrowIfDisplayAttached()
        {
            if (_connection != null)
                throw new InvalidOperationException("[CombatViewSystem] 이미 씬에 연결돼 있습니다. 먼저 Detach해야 합니다.");
        }

        /// <summary>
        /// 진입 활성과 전투 설치를 확인합니다. Combat 씬 Enter 노드에서 LifeCycle(<see cref="SceneNodeOrder.LifeCycle"/>)이 준비된 전투를
        /// 활성화한 뒤 View(<see cref="SceneNodeOrder.CombatViewAttach"/>)가 연결하므로 정상 흐름에서는 항상 참입니다(골격 설계 §6.3, 개선 설계 D13).
        /// </summary>
        private void RequireActiveBattle()
        {
            if (_entry == null || !_entry.IsActive || !_state.HasBattle)
                throw new InvalidOperationException(
                    "[CombatViewSystem] 진입이 활성이고 전투가 설치된 뒤에만 씬에 연결할 수 있습니다.");
        }

        /// <summary>
        /// 연출 취소 → 입력 해제 → 표시 반환·씬 참조 해제 순서로 정리합니다. 하나가 실패해도 나머지를 시도하고 모아서 던집니다.
        /// 전투 수명의 끝이라 남은 명령·미리보기를 비웁니다. 번호 카운터·확인 번호(P)는 Buffer가 유지합니다(재구성 설계 D4-a).
        /// 연결돼 있지 않으면 아무것도 하지 않습니다. 배치 확인창과 씬 루트 구독은 연결 상태가 있을 때만 생깁니다(View 3라운드 W11-4).
        /// 씬 내용물인 배경은 놓지 않습니다. 씬 이탈·종료의 <see cref="ReleaseScene"/>이 놓습니다(배경 설계 BD4, Q-B2 A).
        /// </summary>
        /// <remarks>
        /// 단계마다 연결 상태를 다시 읽습니다. 정리 콜백 안의 재진입 Detach가 연결을 먼저 끝낸 경우 옛 조정자(단계마다 필드를 다시 읽음)와 같게 행동하려는 것입니다.
        /// </remarks>
        internal void Detach()
        {
            if (_connection == null)
                return;

            List<Exception> errors = null;
            if (_connection.Preparation != null)
                CleanupErrors.Try(_connection.Preparation.Cancel, ref errors);
            if (_connection.Input != null)
                CleanupErrors.Try(_connection.Input.CloseConfirmations, ref errors);
            // 구독 해제는 정리의 맨 앞에서 한다(구조 개선 SV1, 지침 §20·21). 입력 표면 → 루트 구독 순서이고, 루트가 없으면 연결 상태가 건너뛴다.
            _connection?.UnbindRoot();

            if (_connection == null)
            {
                // 확인창 닫기 안의 재진입 Detach가 이미 정리를 끝냈다. 닫기 실패만 남는다.
                CleanupErrors.ThrowIfAny(errors, "[CombatViewSystem] 확인창 해제 중 정리에 실패했습니다.");
                return;
            }

            if (_connection?.EntryPresentation != null)
                CleanupErrors.Try(_connection.EntryPresentation.Cancel, ref errors);
            AbortPlayback(ref errors);
            _connection?.StopInput(ref errors);
            _inputConnection?.Release(ref errors);

            // 연출이 씬 참조를 놓은 뒤 Presenter가 자기 View를 돌려준다(설계 §6.1). 패널 연결도 Release에서 끊는다.
            // 연결 상태는 입력 해제(Unbind 중 SelectCanceled 정리) 뒤에 놓는다(2라운드 계획 검증 H2).
            CombatViewConnection connection = _connection;
            _connection = null;
            // 진행 사실·관측·입력 Buffer를 되돌린다. 입력 Buffer 비우기(외부 쓰기)를 연결 상태 비우기보다 앞으로 옮기지 않으려고 그 뒤에 둔다.
            EndConnectionState();
            CleanupErrors.Try(_presentation.Detach, ref errors);
            CleanupErrors.Try(connection.Display.Release, ref errors);

            CleanupErrors.ThrowIfAny(errors, "[CombatViewSystem] 씬 연결 해제 중 정리에 실패했습니다.");
        }

        /// <summary>
        /// 오류 경계가 부르는 입력 정지입니다(F1, 설계 §10.1). 오류 상태여도 끝까지 정리하며, 정상 종료 때도 쓰입니다.
        /// 재생을 먼저 무효화하고 연출을 취소한 뒤 입력을 해제합니다. 명령 Buffer·확인 번호는 보존합니다.
        /// </summary>
        internal void StopInput()
        {
            List<Exception> errors = null;
            if (_connection?.Preparation != null)
                CleanupErrors.Try(_connection.Preparation.Cancel, ref errors);
            if (_connection?.Input != null)
                CleanupErrors.Try(_connection.Input.CloseConfirmations, ref errors);
            if (_connection?.EntryPresentation != null)
                CleanupErrors.Try(_connection.EntryPresentation.Cancel, ref errors);
            AbortPlayback(ref errors);
            _connection?.StopInput(ref errors);
            _inputConnection?.Release(ref errors);
            if (_connection != null)
                CleanupErrors.Try(_connection.Surfaces.Hide, ref errors);

            CleanupErrors.ThrowIfAny(errors, "[CombatViewSystem] 입력 정지 중 정리에 실패했습니다.");
        }

        /// <summary>
        /// Unity의 비활성화 콜백은 Runtime Tick 밖에서 오므로 같은 F1 경계에 보고합니다.
        /// 정상 이탈(씬 Exit Node)과 런타임 종료(앱 종료 때 Bootstrap이 먼저 부름)는 그 전에 이 구독을 풉니다(<see cref="Detach"/>).
        /// 여기까지 왔는데 Combat이 활성이면 씬 루트가 이탈 밖에서 꺼진 것입니다. 같은 씬에 다시 붙지 않으므로(D9) 전투 진행이
        /// 부착 대기·연출 대기에서 오류 없이 멈춥니다. 그래서 조용히 분리하지 않고 원인을 먼저 F1로 보고한 뒤 정리합니다(개선 설계 §7.3 F1).
        /// 입력 정리(오류 정지·정상 종료 전) 중에 꺼진 것은 그 정리가 맡습니다.
        /// </summary>
        private void OnCombatSceneDisabled()
        {
            if (IsSceneActive && !_faults.IsStoppingInput)
                _faults.Report(new InvalidOperationException(
                    "[CombatViewSystem] Combat이 활성인 동안 씬 루트가 꺼졌습니다. 다시 연결할 수 없어 전투 진행이 멈춥니다."));
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
                CombatViewConnection connection = _connection;
                connection.Input.ClearActionAvailability();
                if (!CanContinue() || !ReferenceEquals(_connection, connection))
                    return;
                // 경로 입력 취소 → 공격 상세 프리뷰 정리(입력 기능 묶음이 순서를 정한다, SV4).
                _connection.Input.BeforeResult();
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
            using var sample = FinalizeMarker.Auto();
            if (!result.Accepted)
                UnityEngine.Debug.Log($"[CombatViewSystem] 결과 {result.Sequence}가 거절됐습니다. 사유 코드: {result.Reason}");

            // 값이 같거나 거절이어도 갱신과 확인은 그대로 한다(설계 §6.1). 전투가 정리됐으면 그릴 것이 없다.
            if (_state.HasBattle)
                _connection.Display.Refresh(_displayValues.Read());
            if (!IsCurrent(token))
                return;

            // 최종 거절 사실을 표시한 뒤에도 같은 재생 수명인지 확인합니다.
            _connection.Input.OnResult(result);
            if (!IsCurrent(token))
                return;

            // 그린 뒤에만 일치하는 시작/턴 종료 대기를 해제한다(Kind == Command && 번호 일치, §7.2).
            _connection.Input.Gate.Resolve(result);

            // 결과를 그린 뒤 현재 Core 단계로 Layer를 맞춘다. 종료 상태면 Layer를 다시 켜지 않는다(§5.5·§5.6).
            SyncInputLayer();
            if (!IsCurrent(token))
                return;

            // 기존 미리보기를 무효화하고, 유효하게 남은 선택·모드만 새 번호로 재요청한다. 외부 입력 차단을 푸는 것은 아니다(§7.1).
            _connection.Input.AfterResult(ReadFacts(followUp: true), _characters.Characters);
            if (!IsCurrent(token))
                return;

            // 처리 번호까지만 지우고(K14), 위 단계가 모두 성공한 뒤 마지막으로 확인한다(K1).
            _inputWrite.RemoveCommandsThrough(_state.HandledCommandNumber);
            _inputWrite.AcknowledgePresented(result.Sequence);
            _playback.CompleteFinalization(token);
            CombatViewConnection connection = _connection;
            SyncActionAvailability(connection);
            if (!CanContinue() || !ReferenceEquals(_connection, connection))
                return;
            // 확인 뒤 경로 입력이 복귀 견적을 맞춘다. 경로 대상이 아니면 경로 입력이 스스로 거른다. Tick도 같은 호출을 조건 없이 한다.
            connection.Input.AfterAcknowledged();
            // 확인 번호(P)·대기 해제·후처리 끝이 모두 반영된 행동 버튼은 이 Tick 끝에서 맞춘다(B6, SV6).
        }

        #endregion

        #region 표시와 입력 연결

        /// <summary>
        /// Policy가 정한 목표 Layer로 입력 연결을 맞춥니다. 목표가 없으면(준비 전·종료·입력 닫힘·전투 정리) 입력을 해제합니다.
        /// 실제 Layer와 같으면 다시 바꾸지 않습니다.
        /// </summary>
        private void SyncInputLayer() =>
            // 목표는 정책 한 곳이 정하고, 슬롯 연결·Layer 변경·해제는 입력 연결이 합니다(View 3라운드 W11-1).
            _inputConnection.Apply(CombatInputPolicy.GetDesiredLayer(ReadFacts()));

        /// <summary>
        /// 새 입력 차단입니다(설계 §5.6, View 단계 설계 W2). 진행 사실이 제출 정지나 포기 요청을 세운 직후 부릅니다(둘이 차례로 서면 두 번).
        /// 재생·Attach·명령 대기는 보존하고 Drag·모드·고스트·원본 숨김·미리보기를 정리한 뒤 입력 연결을 해제합니다. 두 번 불러도 결과가 같습니다.
        /// </summary>
        void ICombatProgressHost.OnInputClosed()
        {
            _connection?.Input?.CloseConfirmations();
            CancelInputPresentation();
            _inputConnection?.ReleaseOrReport();
            // 제출 정지는 골격 Job이, 포기 요청은 UI 입구가 부르므로 호출 시점과 관계없이 행동 버튼을 즉시 숨긴다. 제출 정지의 현재 운영 경로(전투 Plan의 제출 정지 Job은
            // Global 그룹의 Job Runner Tick에서 View Tick 전에 실행)에서는 Tick 끝 파생과 관측 차이가 없다(구조 개선 SV6).
            _connection?.Surfaces.Hide();
        }

        bool ICombatProgressHost.IsAttached => IsAttached;

        bool ICombatProgressHost.IsEntryPresentationPending => IsEntryPresentationPending;

        /// <summary>
        /// 이 완료를 지금 보여도 되는지입니다: 씬 활성·재생 연결, 재생·후처리 중이거나 확인하지 않은 결과 없음, Core가 기록한 완료와 같음.
        /// "한 번만"은 진행 사실이 봅니다.
        /// </summary>
        bool ICombatProgressHost.CanShowCompletion(BattleCompletionChanges completion) =>
            CanContinue() && !IsEntryPresentationPending && !HasPendingPresentation && CombatCompletionIdentity.IsSame(completion, _state.Completion);

        /// <summary>
        /// 결과 화면을 엽니다. 확인은 이 연결의 표시에서 온 것일 때만 진행 사실에 넘기고, 진행 사실이 Confirmed로 바꿨을 때만 참을 돌려줍니다.
        /// 참이면 표시가 완료 뒤 배경음으로 바꿉니다(View 2라운드 W6-5). 지난 수명의 확인은 무시합니다(재구성 설계 J7·J12).
        /// </summary>
        void ICombatProgressHost.ShowCompletion(BattleCompletionChanges completion, Func<bool> confirm)
        {
            CombatViewConnection connection = _connection;
            // 오류 정지·지난 연결의 늦은 확인은 무시한다(연결 하나에 표시 하나). 같은 완료·표시 중인지는 진행 사실(confirm)이 본다.
            connection.Display.ShowCompletion(completion, () => CanContinue() && ReferenceEquals(_connection, connection) && confirm());
        }

        private void AbortPlayback(ref List<Exception> errors)
        {
            CleanupErrors.Try(_playback.Abort, ref errors);
        }

        /// <summary>
        /// 입력 판단에 쓸 현재 사실을 한 번 읽습니다. 저장하지 않습니다.
        /// </summary>
        /// <param name="includeUnhandledCommand">EndTurn·경로 편집·필드 선택 판단에서 선행 미처리 명령을 조회합니다.</param>
        /// <param name="followUp">결과 후처리 안의 내부 재요청 판단입니다. 외부 입력 차단(후처리 중)은 이 판단에 적용하지 않습니다.</param>
        private CombatInputFacts ReadFacts(bool includeUnhandledCommand = false, bool followUp = false)
        {
            if (_inputFactsReader == null)
                throw new InvalidOperationException("[CombatViewSystem] 서비스를 바인딩한 뒤 입력 사실을 읽어야 합니다.");
            return _inputFactsReader.Read(includeUnhandledCommand, followUp);
        }

        /// <summary>
        /// 정리된 이 전투의 완료를 보이는 중인지입니다(개선 설계 §4.5). 제출을 닫고 같은 완료를 표시한 뒤 Core가 전투를 정리했으면 참이며,
        /// 이 동안 Tick은 마지막 화면만 유지합니다. 확인 뒤 씬 이탈이 연결과 표시를 정리합니다.
        /// </summary>
        private bool IsHoldingResult()
        {
            return !_state.HasBattle && _connection.BattleInstance == _state.BattleInstanceId &&
                _progress.IsShowingCompletionOf(_state.Completion);
        }

        /// <summary>
        /// 확인하지 않은 결과가 남았는지(P &lt; S)입니다. 재생·후처리는 이때만 하므로 "재생 중"도 이것과 같고(진행 계약 remarks), 골격과 같은 식입니다(C 단계 설계 C-3).
        /// 정리 뒤에도 Core가 결과 순번을 남기므로 완료 표시 앞의 "남은 재생" 검사에도 그대로 씁니다.
        /// </summary>
        private bool IsPresenting =>
            CombatProgressFacts.IsUnpresented(_inputRead.PresentedResultSequence, _state.LatestResultSequence);

        /// <summary>
        /// 씬에 연결된 활성 전투인지입니다: 오류 정지 전 ∧ 진입 활성 ∧ 표시 연결(구조 개선 SV2·E5).
        /// 수명 판정식의 공통 바탕이며, 재생 쪽(<see cref="CanContinue"/>)과 입력 쪽(<see cref="IsInputReady"/>)이 이 위에 각자 조건을 더합니다.
        /// 포기 요청·행동 버튼처럼 UI에서 오는 입구와 입력 수신 창구가 이것으로 수명을 판단합니다.
        /// </summary>
        /// <remarks>
        /// 재생 연결은 보지 않습니다. 입력 정지·Detach는 재생을 먼저 끊고(Abort) 그 뒤 슬롯을 놓으며,
        /// 그 사이 슬롯이 보내는 SelectCanceled 정리를 수신 창구가 받아야 하기 때문입니다(R2).
        /// </remarks>
        private bool IsSceneActive => !_faults.IsFaulted && _entry != null && _entry.IsActive && _connection != null;

        /// <summary>
        /// 입력 판단에 쓸 수 있는지입니다: 씬 연결 활성 ∧ 입력 슬롯 연결 ∧ 진입 연출 완료. facts의 입력 가용성과 개발 Layer 패널이 씁니다.
        /// 입력 쪽 판정은 재생 쪽(<see cref="CanContinue"/>)의 하위가 아니라 같은 바탕 위의 형제이며, SV2 이전 식의 의미를 그대로 보존합니다.
        /// 슬롯 연결은 연결 입력 묶음(<see cref="CombatSceneInput"/>)이 있을 때만 성립합니다(Bind는 <see cref="SyncInputLayer"/> 한 곳).
        /// </summary>
        private bool IsInputReady => IsSceneActive && IsInputBound && !IsEntryPresentationPending;

        private bool IsEntryPresentationPending => _connection != null &&
            (_connection.IsPreparationPending ||
             (_connection.Preparation != null && (_loading?.IsLoading ?? false)) ||
             (_connection.EntryPresentation?.IsPending ?? false));

        // 덱·가방 UI 입구의 수신 조건입니다. 막힘 판단은 Policy 한 곳이고, 입력이 준비됐을 때만 사실을 읽습니다.
        private bool CanReceiveDeckInput() => IsInputReady && !CombatInputPolicy.IsInputBlocked(ReadFacts());

        /// <summary>
        /// 전투 입력을 막는 확인창이 열려 있는지입니다. 그런 확인창은 배치 확인창과 턴 종료 확인창입니다(C 단계 C-1).
        /// 두 확인창은 연결 입력 묶음(배치 진입·턴 종료)이 가져서, 연결이 없으면 거짓입니다(View 3라운드 설계 P23).
        /// </summary>
        private bool IsModalOpen => _connection?.Input?.IsConfirmationOpen ?? false;

        /// <summary>
        /// 이 Tick·후처리를 계속해도 되는지입니다: 씬 연결 활성 ∧ 재생 연결. 외부 호출(연출·표시·Layer) 안에서 오류 보고·Detach가 일어나면
        /// 재생 세대가 끊기므로, 외부 호출에서 돌아올 때마다 이것을 다시 봅니다.
        /// </summary>
        private bool CanContinue() => IsSceneActive && _playback.IsAttached;

        /// <summary>
        /// 후처리를 이어 가도 되는지입니다: <see cref="CanContinue"/> ∧ 재생 토큰 일치.
        /// 토큰 일치는 재생 연결을 포함하고, Detach는 표시를 놓기 전에 재생을 먼저 끊으므로 표시 연결 검사가 더해져도 의미가 같습니다(SV2).
        /// AttachDisplay에서 재생 연결이 표시 저장보다 먼저지만 연결은 Abort를 거치므로 그 사이 유효한 토큰은 없고, 진입은 BindServices에서만 정해집니다.
        /// </summary>
        private bool IsCurrent(CombatPlaybackToken token) => CanContinue() && _playback.IsCurrent(token);

        #endregion

        /// <summary>재생·후처리 중이거나 확인하지 않은 결과가 남았는지(P &lt; S)입니다. 연결돼 있지 않으면 false입니다.</summary>
        internal bool HasPendingPresentation => _connection != null && IsPresenting;

        // 전투 진행 골격이 보는 계약(재구성 설계 J12). Buffer에 없는 View 상태만 내보내고, 원본은 진행 사실 클래스다.
        // 골격은 Progress로만 받는다(개선 설계 §7.1 D1, C 단계 C-2).
        public ICombatProgressView Progress => _progress;

        /// <summary>Option UI에서 확인한 포기를 기존 요청 처리로 전달합니다.</summary>
        public bool TryRequestAbandon() => _abandon.TryRequestAbandon();

        /// <summary>시작/턴 종료 결과를 기다리는 중인지입니다(읽기 전용, Gate 값).</summary>
        internal bool HasPendingCommand => _connection?.Input != null && _connection.Input.Gate.HasPending;

        /// <summary>테스트 입구: 입력 수신 창구입니다(개선 설계 D11, Q4). 운영 입력은 슬롯을 거쳐 이 객체로 옵니다.</summary>
        internal CombatInputReceiver Input => _inputConnection?.Receiver;

        /// <summary>입력 슬롯에 연결돼 있는지입니다(수신 창구의 값).</summary>
        private bool IsInputBound => _inputConnection != null && _inputConnection.IsBound;

        // 씬 루트 포커스 상실 이벤트가 부르며 입력 연결로 넘긴다(AttachCreated에서 구독, Detach에서 해제).
        internal void FocusLost() => _inputConnection?.FocusLost();

        // 시험 입구입니다. 운영 복귀는 복귀 키(입력 슬롯)와 복귀 버튼(입력 연결의 ReturnPressed)이 수신 창구로 바로 들어간다(C 단계 설계 C-7).
        internal void RequestReturn() => _inputConnection?.Receiver.RequestReturn();

        /// <summary>
        /// Drag·조준·경로 같은 입력 중 표시를 정리합니다. 대기·명령 Buffer는 보존합니다.
        /// 포기 요청을 받을 때, 정상 종료의 제출 정지, 입력 Layer 해제·변경 때 씁니다.
        /// </summary>
        private void CancelInputPresentation()
        {
            _connection?.Input?.CancelPresentation();
            // 공격 모드가 끝났을 수 있는 버튼 강조는 Tick 끝에서 맞춘다(Layer 바뀜 등 입력 밖의 정리 경로, SV6). 입력 닫힘 경로는 호스트가 버튼을 바로 숨긴다.
        }

        /// <summary>
        /// 수신 창구가 입구 검사를 통과한 입력을 Router로 넘깁니다. 포인터 이동이 아니면 처리 뒤 행동 버튼을 맞춥니다
        /// (공격 모드 진입·취소, 제출·대기 시작, B6). 키와 버튼이 같은 경로입니다.
        /// </summary>
        /// <remarks>
        /// 수신 창구는 슬롯에 연결돼 있을 때만 이것을 부르고, 슬롯 연결(Bind)은 연결 입력 묶음을 만든 뒤 <see cref="SyncInputLayer"/> 한 곳에서만 합니다.
        /// 그래서 여기서 묶음은 항상 있습니다. 이 불변식이 깨지면 수신 창구가 예외를 오류 경계에 보고합니다.
        /// 배치 확인 갈래·정보창 선택·표면 맞춤은 조정자의 사실(확인창·표시·재생 중)을 써서 여기 남고, Router·환경 호버는 묶음이 맡습니다(계획 TP-7).
        /// </remarks>
        private void RouteInput(CombatInputRouter.Request request, float screenX, float screenY)
        {
            CombatViewConnection connection = _connection;
            // 카메라 입력은 상태만 저장한다. 선택·가용성·행동 버튼과 무관하므로 묶음에 바로 넘긴다(카메라 조작 설계 CC2, 계획 P5).
            if (CombatInputRouter.IsCameraRequest(request))
            {
                if (CanContinue())
                    connection.Input.Route(request, screenX, screenY, _characters.Characters);
                return;
            }
            if (request == CombatInputRouter.Request.EnterCombat)
            {
                connection.Input.Deployment.Request();
                if (CanContinue() && ReferenceEquals(_connection, connection))
                    SyncActionButtons();
                return;
            }
            // 전경 입력 소유자(Drag·공격 모드·경로 지정)가 포인터를 쓰는 동안은 기존 선택을 유지합니다. 그 판정은 묶음 입구 하나이고(평가 C2-02·C8-01),
            // 그 밖의 선택 자격은 Policy 한 곳에서 판단합니다.
            if (request == CombatInputRouter.Request.SelectPressed
                && connection.Input.CanInspectOnPress
                && CanContinue()
                && CombatInputPolicy.CanSelectUnit(ReadFacts(includeUnhandledCommand: true)))
            {
                connection.Inspection.InspectUnit(screenX, screenY);
                // 정보창의 외부 UI 콜백 뒤에는 지난 입력을 새 연결에 보내지 않습니다.
                if (!CanContinue() || !ReferenceEquals(_connection, connection) || !IsInputReady)
                    return;
            }
            SyncActionAvailability(connection);
            // Abort 뒤 Unbind가 보내는 정리 신호는 기존 소유자에게 끝까지 전달합니다(R2).
            bool cleanupRequest = request == CombatInputRouter.Request.FocusLost || request == CombatInputRouter.Request.SelectCanceled;
            if (!ReferenceEquals(_connection, connection) || (!CanContinue() && !cleanupRequest))
                return;
            // 턴 종료는 확인창 입구를 먼저 지난다. 확인이 필요 없으면 입구가 지금과 같은 Router 경로로 바로 넘긴다.
            if (request == CombatInputRouter.Request.EndTurn)
                connection.Input.EndTurn.Request();
            else
                connection.Input.Route(request, screenX, screenY, _characters.Characters);
            if (!CanContinue() || !ReferenceEquals(_connection, connection))
                return;
            SyncActionAvailability(connection);
            if (!CanContinue() || !ReferenceEquals(_connection, connection))
                return;
            SyncEnvironmentHover();
            if (!CanContinue() || !ReferenceEquals(_connection, connection))
                return;
            if (request != CombatInputRouter.Request.PointerMoved)
                SyncActionButtons();
        }

        /// <summary>조회 수명만 맞춥니다. Tracker가 Facts를 다시 읽지 않도록 현재 키를 값으로 넘깁니다.</summary>
        private void SyncActionAvailability(CombatViewConnection connection)
        {
            if (!CanContinue() || !ReferenceEquals(_connection, connection) || connection.Input == null)
                return;
            connection.Input.ActionAvailability.Sync(ReadFacts(includeUnhandledCommand: true), _characters.Characters,
                connection.Input.Interaction.IsInputOwnerActive, _state.AnsweredPreviewNumber);
        }

        /// <summary>표시가 끝난 결과만 설명합니다. 같은 칸·결과 번호의 문구는 다시 생성하지 않습니다.</summary>
        private void SyncEnvironmentHover()
        {
            CombatSceneInput input = _connection?.Input;
            if (input == null || !input.HasEnvironmentHover) return;
            CombatInputFacts facts = ReadFacts();
            bool allowed = CanReceiveDeckInput() && CombatInputPolicy.CanShowEnvironmentHover(facts)
                && facts.PointerTarget != CombatInputRouter.PointerTarget.None;
            input.SyncEnvironmentHover(facts.HasBattle ? _state.Current : null, facts.LatestResultSequence, allowed);
        }

        /// <summary>
        /// 행동 버튼(공격·턴 종료)을 현재 사실로 맞춥니다(행동 버튼 설계 B3·B6). 판단은 Policy와 공격 진입 판단을 읽기만 하고,
        /// 패널은 값이 바뀔 때만 씁니다. Tick 끝과 입력 직후(<see cref="RouteInput"/>)에 부릅니다(구조 개선 SV6). 정지·입력 닫힘·해제의 숨김은 각 경로가 즉시 합니다.
        /// 입력 객체가 없으면(첫 그리기 전·해제 뒤) 아무것도 하지 않습니다.
        /// </summary>
        private void SyncActionButtons()
        {
            CombatViewConnection connection = _connection;
            if (connection?.Input == null)
                return;
            connection.Input.SyncConfirmations();
            if (!connection.Shown)
                return;

            // 턴 종료 판단에는 선행 미처리 명령까지 읽는다(Router의 EndTurn과 같은 사실).
            CombatInputFacts facts = ReadFacts(includeUnhandledCommand: true);
            // 세 표면이 같은 막힘 판단 하나를 쓴다(View 단계 설계 W2). 맞춤은 입력 표면이 한다(View 2라운드 W6-2).
            connection.Surfaces.Sync(facts, connection.Input.Interaction.CanEnterAttack(facts), connection.Input.Interaction.IsAttackActive,
                connection.Input.MovementUndo.CanRequest, connection.Input.ActionAvailability.Read(facts));
        }
    }
}
