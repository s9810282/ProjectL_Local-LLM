# 전투 골격·View — 현재 구현 UML

- 기준: 2026-10-07, `feature/Yonggeun/Stage`의 `8408a787`. 구현된 코드 관계만 그린다.
- [오프라인 HTML 도표](2026-10-07-combat-view-skeleton-current-uml.html)에서 주요 관계와 흐름을 볼 수 있다.
- 정책 원본: [골격 설계](2026-09-30-combat-flow-skeleton-design.md) §4.1·§6.3~6.5, [View 설계](2026-09-30-combat-view-design.md). 이 문서는 코드 구조의 스냅샷이다.

표기: `*--`는 객체 생성·수명 소유, `o--`는 참조 보관, `..>`는 호출·등록 의존, `<|..`는 인터페이스 구현이다. 씬 Presenter 참조는 GameObject 생성 소유를 뜻하지 않는다.

## 1. 골격과 외부 경계

```mermaid
classDiagram
    class CombatLifeCycle {
        +RequestEntry(chapterKey, nodeId)
        +CancelEntry(request)
        +RequestNormalExit(destination)
        +RequestAbandon()
        +IsActive
        -OnTick(deltaTime)
    }
    class CombatEntryRequest {
        +Status
        +Result
        +IsJobCompleted
    }
    class CombatFlow {
        -PendingCheck _pending
        -bool _isStopped
        +ActivatePreparedBattle(entry)
        +Tick()
        +Stop()
    }
    class CombatExitSequence {
        +Status
        +Request(destination)
        +RequestAbandon(destination)
        +Tick()
        +ValidateTransition(destination)
    }
    class ICombatEntryService {
        <<interface>>
        +RequestEntry(chapterKey, nodeId)
    }
    class ICombatExitService {
        <<interface>>
        +RequestNormalExit(destination)
    }
    class ICombatAbandonRequest {
        <<interface>>
        +RequestAbandon()
    }
    class ICombatExitPresentation {
        <<interface>>
        +SuspendSubmissions()
        +HasPendingPresentation
        +PresentCompletion(completion, confirmed)
    }
    class ICombatProcessingService {
        <<interface>>
        +AbandonBattle()
        +EndBattle()
    }
    ICombatEntryService <|.. CombatLifeCycle
    ICombatExitService <|.. CombatLifeCycle
    ICombatAbandonRequest <|-- ICombatExitService
    CombatLifeCycle *-- CombatEntryRequest
    CombatLifeCycle *-- CombatFlow
    CombatLifeCycle *-- CombatExitSequence
    CombatExitSequence ..> ICombatExitPresentation : drain / completion
    CombatExitSequence ..> ICombatProcessingService : abandon / cleanup
    CombatExitSequence ..> LifeCycleManager : authorized Change
    LifeCycleManager ..> CombatLifeCycle : active Tick / transition guard
    CombatLifeCycle o-- RuntimeFaultBoundary
    CombatFlow o-- RuntimeFaultBoundary
    CombatExitSequence o-- RuntimeFaultBoundary
```

`CombatLifeCycle`의 활성 Tick 순서는 **Flow → Completion 감지 및 이탈 요청 → ExitSequence**다. 비활성 `ISystemTick`은 진입 Job의 결과를 확인한다. Flow는 버퍼의 국면·번호를 매번 읽고 다음 Job **하나**만 등록한다. ExitSequence는 결과 표시가 끝나고 사용자가 Completion을 확인한 다음 Core 정리·새 Run 준비·씬 전환을 순서대로 실행한다.

## 2. Plan·Job·Buffer

```mermaid
classDiagram
    CombatLifeCycle ..> IJobRegisterSystem : RegisterCombatEntry
    CombatFlow ..> IJobRegisterSystem : Register one job
    IJobRegisterSystem <|.. JobRegisterSystem
    JobRegisterSystem ..> CombatPlan : RegisterEntry / Register
    JobRegisterSystem ..> IJobRunner : RegisterRange
    CombatPlan ..> CombatPrepareJob : entry / Start
    CombatPlan ..> CombatCommandJob : ProcessCommand
    CombatPlan ..> CombatPreviewJob : ProcessPreview
    CombatPlan ..> CombatAdvanceJob : AdvanceCombat
    CombatPlan ..> NewRunJob : NewRun
    IJobRunner ..> CombatPrepareJob : Execute
    IJobRunner ..> CombatCommandJob : Execute
    IJobRunner ..> CombatPreviewJob : Execute
    IJobRunner ..> CombatAdvanceJob : Execute
    IJobRunner ..> NewRunJob : Execute
    CombatPrepareJob ..> ICombatProcessingService : EnterBattle
    CombatCommandJob ..> ICombatProcessingService : ProcessNextCommand
    CombatPreviewJob ..> ICombatProcessingService : ProcessLatestPreview
    CombatAdvanceJob ..> ICombatProcessingService : AdvanceCombat
    NewRunJob ..> IRunStartService : StartNewRun
    CombatFlow ..> ICombatStateBufferReadSink : phase / handled number
    CombatFlow ..> ICombatResultReadSink : latest result
    CombatFlow ..> ICombatViewBufferReadSink : command / preview / presented number
```

`CombatPlan`은 JobType별 Job 목록을 조립한다. `CombatFlow`가 등록 조건과 순서를 정하므로 Priority가 전투의 시나리오 순서를 정하지 않는다. Core 처리 결과는 Buffer로 돌아오고 Flow가 처리 번호·결과를 확인한다. `RecordInputOnly`는 준비 확인 후 처리 Job을 등록하지 않는다.

## 3. View 내부 책임

```mermaid
classDiagram
    class CombatViewSystem {
        +Attach(sceneRoot)
        +Tick(deltaTime)
        +Detach()
        +SuspendSubmissions()
        +PresentCompletion(completion, confirmed)
        -Receive(request, x, y)
    }
    class ICombatViewService {
        <<interface>>
        +Attach(sceneRoot)
        +Detach()
    }
    class ICombatBattleDisplay {
        <<interface>>
        +Install(values)
        +Refresh(values)
        +ShowCompletion(completion, confirmed)
        +Release()
    }
    ICombatExitPresentation <|-- ICombatViewService
    ICombatViewService <|.. CombatViewSystem
    CombatViewSystem *-- CombatViewRefresh : signal observation
    CombatViewSystem *-- CombatResultPlayback : result lifetime / finalization
    CombatViewSystem *-- CombatInputRouter : input dispatch
    CombatViewSystem *-- CombatCommandGate : pending command owner
    CombatViewSystem *-- CombatInteraction : placement / attack
    CombatViewSystem *-- CombatPathInput : movement / return
    CombatViewSystem o-- ICombatBattleDisplay : scene attachment
    CombatViewSystem o-- ICombatEntryRead : active check
    CombatViewSystem o-- ICombatAbandonRequest : confirmed abandon
    CombatInputRouter ..> CombatInteraction : pointer target
    CombatInputRouter ..> CombatPathInput : path / return
    CombatInputRouter ..> CombatCommandGate : enter / draw / end turn
    CombatInteraction *-- PlacementDragInteraction
    CombatInteraction *-- AttackTargetingInteraction
    PlacementDragInteraction ..> CombatCommandGate : place / return-to-hand
    AttackTargetingInteraction ..> CombatCommandGate : attack
    CombatPathInput *-- CombatMovementInteraction
    CombatCommandGate ..> CombatInputPolicy : CanSubmit
    CombatResultPlayback o-- ICombatPresentation
    ICombatPresentation <|.. OrderedCombatPresentation
    OrderedCombatPresentation *-- CombatMovementPlayback
    ICombatBattleDisplay <|.. CombatSceneDisplay
    CombatSceneDisplay *-- CombatPointerMapper : screen mapping
    CombatSceneDisplay *-- CombatSceneEventPresenter : events / interpolation
    CombatSceneDisplay o-- FieldView
    CombatSceneDisplay o-- FieldUnitPresenter
    CombatSceneDisplay o-- HandPresenter
    CombatSceneDisplay o-- CombatHud
    CombatSceneDisplay o-- CombatAbandonPanel
```

`CombatViewSystem`은 씬 연결, Buffer 관측, 결과 후처리, 입력 차단을 조정한다. Router는 입력 대상만 고르고, Gate는 시작·턴 종료 대기 번호를 소유한다. 배치·공격·경로 입력은 각각의 객체가 수명과 미리보기를 관리한다. `CombatSceneDisplay`는 Buffer를 직접 읽지 않고 View가 전달한 표시값과 View DB를 사용한다. HUD의 재화는 `CombatDisplayValues.StageGold → CombatStateData.StageGold`에서 읽는다.

## 4. 입력부터 결과 표시까지

```mermaid
sequenceDiagram
    actor Player
    participant V as CombatViewSystem
    participant R as CombatInputRouter
    participant G as CombatCommandGate / Interaction / PathInput
    participant B as CombatViewBuffer
    participant F as CombatFlow
    participant J as Plan / Runner / Core
    participant P as CombatResultPlayback
    participant D as SceneDisplay / Presentation
    Player->>V: input
    V->>R: Route(current facts)
    R->>G: dispatch by intent / pointer target
    G->>B: Submit command or preview
    F->>B: read pending input
    F->>J: register one processing Job
    J->>B: publish state / result / preview answer
    F->>F: confirm expected number or result
    V->>P: Begin(unpresented result)
    P->>D: play ordered events
    D-->>P: events complete
    V->>D: Refresh(current values)
    V->>G: Resolve matching pending command
    V->>B: RemoveCommandsThrough(handled)
    V->>B: AcknowledgePresented(sequence)
```

결과 연출이나 후처리가 실패하면 View는 확인 번호를 올리지 않는다. Flow는 확인되지 않은 최신 결과를 덮을 상태 변경 Job을 등록하지 않는다. 입력 Buffer는 View가 쓰고 Core가 읽는다. 상태·결과 Buffer는 Core가 쓰고 View·Flow가 읽는다.

## 5. 승패·포기 이탈

```mermaid
sequenceDiagram
    actor Player
    participant V as CombatViewSystem
    participant L as CombatLifeCycle
    participant X as CombatExitSequence
    participant C as Core / ChapterProgression
    participant M as LifeCycleManager
    alt Core가 승리 또는 패배 Completion 기록
        L->>X: RequestNormalExit(Stage)
    else 플레이어가 포기 확인
        Player->>V: confirm abandon
        V->>L: ICombatAbandonRequest.RequestAbandon()
        L->>X: RequestAbandon(Stage)
    end
    X->>L: Stop Flow
    X->>V: SuspendSubmissions
    Note over X,V: wait Runner idle and all results presented
    opt 포기 요청
        X->>C: AbandonBattle()
        C-->>X: abandonment result / Completion
    end
    X->>V: PresentCompletion(completion, confirmed)
    V-->>Player: show outcome
    Player->>V: confirm
    V-->>X: confirmed callback
    Note over X,C: later Tick
    X->>C: EndBattle()
    X->>X: verify cleanup and retained result
    opt Run ended
        X->>C: CompleteChapter when final victory
        X->>C: NewRunJob via Register
        C-->>X: new Run ready
    end
    X->>M: authorized Change(Stage)
```

현재 `RunEndDestination`도 `Stage`다. 이는 **임시 목적지**이며 Title 화면 도입 시 바꾸도록 코드에 표시돼 있다. 일반 승리·패배·최종 승리의 Completion은 모두 정상 이탈 요청 대상이다. 포기는 Completion이 없을 때만 받을 수 있으며 Core의 `AbandonBattle()`가 결과를 확정한다. View는 재화·승패를 계산하거나 초기화하지 않는다.

```mermaid
stateDiagram-v2
    [*] --> Idle
    Idle --> Draining: normal exit / abandon accepted
    Draining --> Abandoning: abandon after drain
    Abandoning --> Presenting: Core outcome confirmed
    Draining --> Presenting: normal final result confirmed
    Presenting --> Ending: user confirmed / later Tick
    Ending --> PreparingNewRun: EndBattle / Run ended
    Ending --> Transitioning: EndBattle / Run continues
    PreparingNewRun --> Transitioning: NewRunJob complete
    Draining --> Failed: exception
    Abandoning --> Failed: exception
    Presenting --> Failed: exception
    Ending --> Failed: exception
    PreparingNewRun --> Failed: exception
    Transitioning --> Failed: change failure
```

공유 `RuntimeFaultBoundary`가 보고된 오류를 보존하고 다음 Runtime Tick을 막는다. `ExitSequence`의 `Failed`는 그 시퀀스 자체에서 잡은 예외의 상태이며, 외부 오류가 생겼다고 반드시 `Failed`로 바뀌는 것은 아니다.

## 코드 위치

| 구역 | 구현 |
|---|---|
| 골격 | [CombatLifeCycle](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Combat/CombatLifeCycle.cs), [CombatFlow](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Combat/CombatFlow.cs), [CombatExitSequence](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Combat/CombatExitSequence.cs), [CombatPlan](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Combat/Plans/CombatPlan.cs) |
| View 조정·입력 | [CombatViewSystem](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/CombatViewSystem.cs), [CombatInputRouter](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/CombatInputRouter.cs), [CombatCommandGate](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/CombatCommandGate.cs), [CombatInteraction](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/CombatInteraction.cs), [CombatPathInput](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/CombatPathInput.cs) |
| View 표시 | [CombatResultPlayback](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/CombatResultPlayback.cs), [OrderedCombatPresentation](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/OrderedCombatPresentation.cs), [CombatSceneDisplay](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/CombatSceneDisplay.cs) |
