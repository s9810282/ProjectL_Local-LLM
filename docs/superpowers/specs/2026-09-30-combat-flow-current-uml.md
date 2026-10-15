# 전투 진행 골격 — 현재 구조 UML

> **과거 스냅샷 / 현재 구조로 사용하지 않음.** 아래 도표는 작성 당시 기록을 보존한다. 현재 골격·View·경로 이동·Return·F1/F2는 [2026-10-05 통합 UML](2026-10-05-combat-current-uml.md)과 [오프라인 HTML](2026-10-05-combat-current-uml.html)을 따른다.

- 작성일: 2026-09-30
- 기준: `feature/Yonggeun/Stage` HEAD `916c948e`에 `1a51234c`를 통합한 작업 트리(병합 커밋 전). Phase 3 골격과 준비 Job의 Core 호출을 반영했다. 사용자 확인: 병합 후 Unity 컴파일 오류 없음, EditMode 전체 통과(2026-09-30).
- 용도: **지금 코드에 실제로 있는 구조**를 그린다. 합의 뒤의 예상 구조(F1~F3, 나머지 Core 서비스 연결)는 [설계 §12](2026-09-30-combat-flow-skeleton-design.md#12-구현-예상-uml)에 있다. 둘을 섞지 않는다.
- 표기: Mermaid UML. `<<struct>>`는 값 타입 Job, 점선 화살표는 "만든다/부른다", 실선 화살표는 필드로 들고 있는 의존이다. **없는 것**은 노트로만 적는다.
- 접근 표기: `+` public, `#` protected, `-` private. `~`는 이 문서에서 C# internal을 나타낸다. 클래스 멤버는 관련 부분만 표시한다.

## 1. 조립과 수명 — 누가 CombatLifeCycle을 만들고 부르나

```mermaid
classDiagram
    direction LR

    class FrameworkRuntimeFactory {
        <<static>>
        +Create() FrameworkRuntime
    }
    class LifeCycleSystemFactory {
        <<static>>
        ~Create() IExportSystemServices
    }
    class LifeCycleSystemGroup {
        +PostBindService(resolver)
        +Initialize()
        +Tick(dt)
        +Shutdown()
    }
    class LifeCycleManager {
        -Dictionary modules
        -ILifeCycleModule _activeModule
        +Change(key)
        +Exit()
        +Tick(dt)
    }
    class ILifeCycleModule {
        <<interface>>
        +Key
        +Enter()
        +Tick(dt)
        +Exit()
    }
    class LifeCycleBase {
        <<abstract>>
        -ILodingSystemService _lodingSystem
        -bool _isActive
        +PostBindService(resolver)
        +Initialize()
        +Enter()
        +Tick(dt)
        +Exit()
        #OnBindServices(resolver)
        #OnEnter()*
        #OnTick(dt)
        #OnExit()*
    }
    class CombatLifeCycle {
        -ICombatViewBufferWriteSink _combatViewWrite
        -CombatFlow _flow
        +Key = Combat
        #SceneIndex = 3
        #OnBindServices(resolver)
        #OnEnter()
        #OnTick(dt)
        #OnExit()
    }
    class ILodingSystemService {
        <<interface>>
        +Load(sceneIndex)
        +Unload()
    }

    FrameworkRuntimeFactory ..> LifeCycleSystemFactory : 루트 조립
    LifeCycleSystemFactory ..> LifeCycleManager : 생성
    LifeCycleSystemFactory ..> CombatLifeCycle : 생성 후 Register
    LifeCycleSystemFactory ..> LifeCycleSystemGroup : 생성
    LifeCycleSystemGroup ..> LifeCycleBase : 수명 단계 전달
    LifeCycleManager o-- ILifeCycleModule : 등록된 구간
    ILifeCycleModule <|.. LifeCycleBase
    LifeCycleBase <|-- CombatLifeCycle
    LifeCycleBase --> ILodingSystemService : Enter는 Load, 완료 Node가 OnEnter
    CombatLifeCycle *-- CombatFlow : 수명 소유

    note for LifeCycleManager "Change(Combat)를 부르는 곳이 아직 없다"
    note for CombatLifeCycle "현재 전투 진입 호출 경로와 Build Settings의 씬 3이 없음. 서비스 바인딩은 진입 전에도 실행됨"
```

- `LifeCycleSystemGroup.PostBindService`는 등록된 모든 구간을 돈다. 그래서 **전투에 들어가지 않아도 `CombatLifeCycle.OnBindServices`는 실행된다.** `FrameworkRuntimeCompositionTests`의 두 테스트가 이 경로를 지난다.
- `OnTick`은 활성 구간에서만 불린다(`LifeCycleBase.Tick`의 `_isActive`).

## 2. Flow·Plan·Job — 진행 한 걸음의 구성

```mermaid
classDiagram
    direction TB

    class CombatFlow {
        <<sealed>>
        -ICombatStateBufferReadSink _state
        -ICombatViewBufferReadSink _input
        -IJobRunner _runner
        -IJobRegisterSystem _register
        -PendingCheck _pending
        -int _expectedNumber
        -bool _isStopped
        ~IsStopped bool
        ~Reset()
        ~Stop()
        ~BeginPreparation()
        ~Tick()
        -TryConfirmPending() bool
        -IsPreviewSettled() bool
        -IsPresentationDone() bool
        -RegisterAndExpect(jobType, check, number)
        -Fail(reason)
        -ClearPending()
    }
    class PendingCheck {
        <<enumeration>>
        None
        Preparation
        Command
        Preview
    }
    class ICombatStateBufferReadSink {
        <<interface>>
        +HasBattle
        +Current
        +Phase
        +LatestResultSequence
        +HandledCommandNumber
        +AnsweredPreviewNumber
    }
    class ICombatViewBufferReadSink {
        <<interface>>
        +Commands
        +TryGetNextCommand(handledThrough, out command)
        +HasPreview
        +Preview
        +PresentedResultSequence
    }
    class IJobRunner {
        <<interface>>
        +IsIdle
        +Register(job)
        +RegisterRange(jobs)
        +Clear()
    }
    class IJobRegisterSystem {
        <<interface>>
        +Register(rootType, jobType)
    }
    class JobRegisterSystem {
        -Dictionary plans
        -IInGameSystemGroupService _inGameService
        +Register(rootType, jobType)
    }
    class JobRunnerSystem {
        -JobQueue _queue
        -IJobService _currentJob
        +Tick(dt)
    }
    class CombatPlan {
        <<static>>
        ~Register(service, jobType) List~IJobService~
    }
    class JobType {
        <<enumeration>>
        None = 0
        Start = 1
        NewRun = 2
        ProcessCommand = 3
        ProcessPreview = 4
        AdvanceCombat = 5
    }
    class IJobService {
        <<interface>>
        +Priority
        +Execute()
    }
    class IJobCompletion {
        <<interface>>
        +IsCompleted
    }
    class CombatPrepareJob {
        <<struct>>
    }
    class CombatCommandJob {
        <<struct>>
    }
    class CombatPreviewJob {
        <<struct>>
    }
    class CombatAdvanceJob {
        <<struct>>
    }
    class IInGameSystemGroupService {
        <<interface>>
        +CoreGroup
        +ViewGroup
    }
    class IInGameCoreSystemGroupService {
        <<interface>>
        +Combat
    }
    class ICombatProcessingService {
        <<interface>>
        +PrepareBattle()
    }

    CombatFlow --> ICombatStateBufferReadSink : 국면과 번호 읽기
    CombatFlow --> ICombatViewBufferReadSink : 명령, 미리보기, 연출 확인 읽기
    CombatFlow --> IJobRunner : IsIdle만 읽음
    CombatFlow --> IJobRegisterSystem : Register(Combat, JobType)
    CombatFlow ..> PendingCheck : 내부 enum 타입
    CombatFlow ..> JobType
    IJobRegisterSystem <|.. JobRegisterSystem
    IJobRunner <|.. JobRunnerSystem
    JobRegisterSystem ..> CombatPlan : JobRootType.Combat
    JobRegisterSystem --> IInGameSystemGroupService : Plan에 서비스 전달
    JobRegisterSystem --> IJobRunner : RegisterRange
    CombatPlan ..> CombatPrepareJob : Start
    CombatPlan ..> CombatCommandJob : ProcessCommand
    CombatPlan ..> CombatPreviewJob : ProcessPreview
    CombatPlan ..> CombatAdvanceJob : AdvanceCombat
    IJobCompletion <|-- IJobService
    IJobService <|.. CombatPrepareJob
    IJobService <|.. CombatCommandJob
    IJobService <|.. CombatPreviewJob
    IJobService <|.. CombatAdvanceJob
    JobRunnerSystem --> IJobService : Execute, IsCompleted
    CombatPrepareJob --> IInGameSystemGroupService
    CombatCommandJob --> IInGameSystemGroupService
    CombatPreviewJob --> IInGameSystemGroupService
    CombatAdvanceJob --> IInGameSystemGroupService
    IInGameSystemGroupService --> IInGameCoreSystemGroupService : CoreGroup
    IInGameCoreSystemGroupService --> ICombatProcessingService : Combat
    CombatPrepareJob ..> ICombatProcessingService : PrepareBattle 한 번 호출

    note for CombatFlow "AdvanceCombat을 등록하는 코드는 아직 없다(K9, F3 대기)"
    note for CombatPlan "None은 빈 목록. 나머지 지원 시나리오는 Priority 0인 새 Job 하나"
    note for CombatPrepareJob "준비 서비스 정상 반환 뒤 완료. 운영 입력 소스 G7은 미연결"
    note for CombatCommandJob "명령, 미리보기, 자동 진행 Job 세 개는 Execute에서 미연결 예외(R6)"
```

- Flow는 `IJobRegisterSystem.Register`를 호출한다. Plan은 Job 목록을 만들어 반환하고, **JobRegisterSystem이 Runner.RegisterRange로 큐에 넣는다.** Plan은 실행이나 큐 등록을 하지 않는다.
- `CombatFlow`는 서비스 모듈이 아니다. `CombatLifeCycle`이 `OnBindServices`에서 만든다.
- Job은 명령 내용을 들고 있지 않다. 서비스가 실행 시점에 Buffer에서 읽을 예정이다.

## 3. Buffer 쪽 — Flow가 읽는 값의 원본

```mermaid
classDiagram
    direction LR

    class IBufferQuery {
        <<interface>>
        +CombatStateReadSink
        +CombatViewReadSink
        +CombatViewWriteSink
    }
    class BufferSystemGroup
    class CombatStateBuffer {
        -CombatStateData _current
        +LatestResultSequence
        +HandledCommandNumber
        +AnsweredPreviewNumber
    }
    class ICombatStateBufferStore {
        <<interface>>
        +PrepareBattle(preparation)
        +InstallBattle(prepared)
        +EndBattle()
    }
    class CombatViewBuffer {
        -List _commands
        -int _lastIssuedNumber
        -CombatViewPreviewRequest _preview
        -int _presentedResultSequence
        -bool _hasPreview
    }
    class ICombatViewBufferWriteSink {
        <<interface>>
        +Submit(command) int
        +SetPreview(preview) int
        +ClearPreview()
        +RemoveCommandsThrough(number)
        +AcknowledgePresented(sequence)
        +Clear()
    }

    IBufferQuery <|.. BufferSystemGroup
    ICombatStateBufferReadSink <|.. CombatStateBuffer
    ICombatStateBufferStore <|.. CombatStateBuffer
    ICombatViewBufferReadSink <|.. CombatViewBuffer
    ICombatViewBufferWriteSink <|.. CombatViewBuffer
    BufferSystemGroup --> ICombatStateBufferReadSink
    BufferSystemGroup --> ICombatStateBufferStore
    BufferSystemGroup --> ICombatViewBufferReadSink
    BufferSystemGroup --> ICombatViewBufferWriteSink
    CombatLifeCycle --> ICombatViewBufferWriteSink : OnEnter, OnExit에서 Clear만
    CombatFlow --> ICombatStateBufferReadSink
    CombatFlow --> ICombatViewBufferReadSink

    class ICombatStateBufferReadSink {
        <<interface>>
    }
    class ICombatViewBufferReadSink {
        <<interface>>
    }
    class CombatLifeCycle
    class CombatFlow

    note for CombatStateBuffer "세 번호의 setter는 private이고 올리는 코드가 없다(R1~R3). 국면도 준비 때 값뿐이다(R4)"
```

- 전투 상태(`CombatStateBuffer`)는 Core 측이 쓴다. 입력(`CombatViewBuffer`)은 View가 쓴다. Flow는 둘 다 **읽기만** 한다.
- LifeCycle은 구간 경계에서 입력 Buffer를 비우는 `Clear`만 쓴다. 번호 카운터와 연출 확인 값은 `Clear`해도 남는다.

## 4. 바인딩 — 전투에 들어가지 않아도 실행되는 부분

```mermaid
sequenceDiagram
    autonumber
    participant Runtime as FrameworkRuntime
    participant Group as LifeCycleSystemGroup
    participant Base as LifeCycleBase
    participant Life as CombatLifeCycle
    participant Store as 서비스 저장소

    Runtime->>Group: PostBindService(resolver)
    Group->>Base: PostBindService(resolver)
    Base->>Store: GetService(Global) → 로딩 서비스
    Base->>Life: OnBindServices(resolver)
    Life->>Store: GetService(BufferSystemKey.Buffer)
    Store-->>Life: IBufferQuery
    Life->>Store: GetService(Global).JobServiceCommand
    Store-->>Life: JobRunner, JobRegister
    Life->>Life: _combatViewWrite = CombatViewWriteSink
    Life->>Life: _flow = new CombatFlow(상태 읽기, 입력 읽기, Runner, Register)
    Note over Store,Life: 키가 없으면 KeyNotFoundException으로 초기화가 멈춘다
```

## 5. 진입 — 지금 코드로 들어온다면 일어나는 일

**지금은 도달하지 않는 경로다.** `Change(Combat)` 호출자와 씬 3이 없다. 코드가 무엇을 하도록 연결됐는지를 보이려고 그린다.

```mermaid
sequenceDiagram
    autonumber
    participant Loding as 로딩 시스템
    participant Life as CombatLifeCycle
    participant Input as CombatViewBuffer
    participant Flow as CombatFlow
    participant Register as JobRegisterSystem
    participant Plan as CombatPlan
    participant Runner as JobRunnerSystem
    participant Job as CombatPrepareJob
    participant Core as CombatProcessingService
    participant Battle as BattleWriteSink

    Loding->>Life: 씬 3 Enter Node → OnEnter()
    Life->>Input: Clear()
    Life->>Flow: Reset()
    Life->>Flow: BeginPreparation()
    Flow->>Flow: 조건 넷 확인(멈춤 아님, Runner Idle, 전투 없음, 기다리는 처리 없음)
    Flow->>Register: Register(Combat, Start)
    Register->>Plan: Register(service, Start)
    Plan-->>Register: [CombatPrepareJob]
    Register->>Runner: RegisterRange
    Flow->>Flow: _pending = Preparation

    Note over Runner,Job: 등록 후 Runner가 실행될 때. OnEnter 등록은 같은 Runtime Tick에서 실행될 수도 있음
    Runner->>Job: Execute()
    Job->>Core: CoreGroup.Combat.PrepareBattle()
    alt 초기화·바인딩·유효한 준비 입력이 제공됨
        Core->>Core: 준비 입력 읽기, Core 계산
        Core->>Battle: PrepareBattle(preparation)
        Battle-->>Core: 상태 설치 완료
        Core-->>Job: 정상 반환
        Job->>Job: IsCompleted = true
        Job-->>Runner: 반환
        Runner->>Runner: 완료 Job 제거
        Note over Flow: 다음 Flow.Tick에서 Runner Idle과 HasBattle 확인
    else 준비 입력이 없거나 준비 실패
        Core-->>Job: InvalidOperationException 등 원래 예외
        Job-->>Runner: 완료 표시 없이 예외 전파
        Note over Runner: 현재 Job 유지. 같은 Runtime을 다시 Tick하면 재실행(F1 미해결)
        Note over Flow: 예외가 전파된 Runtime Tick에서는 뒤쪽 View와 Flow가 호출되지 않음
    end
    Note over Core: 현재 운영 Factory의 입력 소스는 null. G7 예외 경로에 해당
```

## 6. Flow.Tick 한 번의 판단

```mermaid
flowchart TD
    A([Tick]) --> B{멈춤?}
    B -- 예 --> Z([끝])
    B -- 아니오 --> C{Runner Idle?}
    C -- 아니오 --> Z
    C -- 예 --> D{기다리는 처리 있음?}
    D -- 아니오 --> F
    D -- 예 --> E{확인 통과?}
    E -- 아니오 --> E2["LogError 한 번, 멈춤"] --> Z
    E -- 예 --> E3["기다리는 처리 지움"] --> F
    F{HasBattle?}
    F -- 아니오 --> Z
    F -- 예 --> G{Phase = PlayerInput?}
    G -- 아니오 --> Z2(["등록 없음: Automatic(1단계), Finished, None"])
    G -- 예 --> H{"연출 끝 그리고 다음 명령 있음?"}
    H -- 예 --> H2["Register(ProcessCommand), 기대 번호 = 명령 번호"] --> Z
    H -- 아니오 --> I{"새 미리보기 있음?"}
    I -- 예 --> I2["Register(ProcessPreview), 기대 번호 = 요청 번호"] --> Z
    I -- 아니오 --> Z
```

확인 통과 조건:

| 기다리는 처리 | 통과 |
|---|---|
| `Preparation` | `HasBattle == true` |
| `Command` | `HandledCommandNumber ≥ 기대 번호` |
| `Preview` | `AnsweredPreviewNumber ≥ 기대 번호`, 또는 현재 미리보기가 없거나 번호가 바뀜 |

## 7. Flow의 상태

```mermaid
stateDiagram-v2
    state "대기 없음 (None)" as Idle
    state "준비 대기 (Preparation)" as Preparing
    state "명령 대기 (Command)" as Command
    state "미리보기 대기 (Preview)" as Preview
    state "멈춤 (Stopped)" as Stopped

    [*] --> Idle : 생성
    Idle --> Preparing : BeginPreparation
    Idle --> Command : Tick에서 ProcessCommand 등록
    Idle --> Preview : Tick에서 ProcessPreview 등록
    Preparing --> Idle : Runner Idle이고 확인 통과
    Command --> Idle : Runner Idle이고 확인 통과
    Preview --> Idle : Runner Idle이고 확인 통과
    Preparing --> Stopped : 확인 실패, LogError
    Command --> Stopped : 확인 실패, LogError
    Preview --> Stopped : 확인 실패, LogError
    Idle --> Stopped : Stop, 로그 없음
    Preparing --> Stopped : Stop, 대기 기록은 남음
    Command --> Stopped : Stop, 대기 기록은 남음
    Preview --> Stopped : Stop, 대기 기록은 남음
    Stopped --> Idle : Reset
```

- 멈춤에서는 `Tick`이 아무것도 하지 않고, `BeginPreparation`은 예외를 던진다(K11).
- 정상 `Stop`은 대기 기록을 지우지 않는다. `Reset`이 지운다.
- 그림은 주된 진행 경로다. `Reset()`은 멈춤 상태뿐 아니라 준비·명령·미리보기 대기 중에도 확인 기록을 지운다. `Fail()`은 확인 기록을 지우고 정지하며, 정상 `Stop()`과 구분한다.

## 8. 지금 없는 것 (다음 Phase에서 생기는 것)

| 없는 것 | 생기는 Phase | 필요한 외부 조건 |
|---|---|---|
| `AdvanceCombat` 등록과 결과 확인, Flow의 결과 읽기 의존 | 4 | F3 결과 읽기 타입·서비스(천명님) |
| 명령·미리보기·자동 진행 Job의 Core 호출과 처리 번호 갱신 | 5 | R6·R1~R4(천명님). 준비 API와 Job 호출은 반영됨 |
| 운영 전투 준비 입력 소스 | 준비 진입 전 | G7(천명님·진입 경로 협의) |
| 예외 뒤 Runtime 재Tick 중단 | 6 | F1(민형님) |
| 전환 전 새 등록 중단과 기존 Job 종료, 결과·`EndBattle` 수명 | 6 | F2(민형님·천명님·View) |
| `Change(Combat)` 호출자, 전투 씬, 부트스트랩 | 별도 작업 | 인계 §6 결정 1·2 |

## 9. 이탈 — 현재 연결된 훅과 남은 작업

아래는 전투가 활성 상태이고 로딩 종료 콜백이 전달됐다고 가정한 현재 코드 경로다. 정상 이탈의 선행 조건을 보장하는 F2는 아직 없다.

```mermaid
sequenceDiagram
    autonumber
    participant Loading as 로딩 시스템
    participant Base as LifeCycleBase
    participant Life as CombatLifeCycle
    participant Flow as CombatFlow
    participant Input as CombatViewBuffer
    participant Runner as JobRunnerSystem

    Loading->>Base: 등록된 Exit Node 호출
    Base->>Life: OnExit()
    Life->>Flow: Stop()
    Note over Flow: 새 등록 중단. 오류 로그 없음. 이미 등록한 Job은 취소하지 않음
    Life->>Input: Clear()
    Note over Input: 명령과 미리보기 제거. 발급 번호와 연출 확인 값 유지
    Life-->>Base: 반환
    Base->>Base: finally에서 _isActive = false
    Note over Life,Runner: 현재 훅은 Runner Idle을 기다리지 않고 Runner.Clear도 호출하지 않음
    Note over Runner: 이미 등록된 Job은 남을 수 있음. 전환 전 종료 확인은 F2에서 연결할 작업
```

## 10. 코드 대조 위치

- [CombatLifeCycle](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Combat/CombatLifeCycle.cs): Flow 생성, 진입·Tick·이탈 훅
- [CombatFlow](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Combat/CombatFlow.cs): 준비 조건 넷, 등록 뒤 확인 기록, 명령 우선 분기, 정지
- [CombatPlan](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Combat/Plans/CombatPlan.cs): 요청별 Job 구성
- [CombatPrepareJob](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Combat/Jobs/CombatPrepareJob.cs): 준비 서비스 한 번 호출, 정상 반환 뒤 완료
- [CombatProcessingService](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CombatSystem/CombatProcessingService.cs): 준비 입력·계산·Buffer 게시, 운영 입력 G7 검사
- [JobRegisterSystem](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/JobSystem/JobRegister/JobRegisterSystem.cs): Plan 호출 뒤 Runner 등록
- [JobRunnerSystem](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/JobSystem/JobRunner/JobRunnerSystem.cs): Execute·완료 확인·예외 전파
- [LifeCycleBase](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/Base/LifeCycleBase.cs): 로딩 콜백, 활성 상태와 훅 호출
