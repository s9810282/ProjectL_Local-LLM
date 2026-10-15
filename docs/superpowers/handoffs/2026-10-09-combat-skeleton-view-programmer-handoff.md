# 전투 골격·View 프로그래머 인계 (2026-10-09)

전투 골격과 전투 View 코드를 **직접 고치거나 기능을 더할 사람**을 위한 안내다. 구조 그림은 [상세 UML](../specs/2026-10-09-combat-skeleton-view-detailed-uml.md), 판정과 개선 후보는 [구조 평가](../specs/2026-10-09-combat-skeleton-view-evaluation.md)에 있다. 이 문서는 "무엇을 지키고, 어디를 고치고, 어떻게 확인하는가"만 다룬다.

- **기준 코드:** `feature/Yonggeun/Stage` HEAD `88c845d0`(2026-10-09).
- **경로 약어:** `LC/` = `LifeCycleSystem/LifeCycles/Combat/`, `V/` = `InGameSystem/ViewSystem/Combat/`, `VD/` = `V/Display/`, `BUF/` = `BufferSystems/`. 모두 `Assets/ProjectL/Script/Systems/RuntimeSystem/` 아래다.

---

## 0. 먼저 읽을 것 (순서대로, 1시간 안)

1. 이 문서 §1~§3 (개념·규칙·파일 지도)
2. [상세 UML](../specs/2026-10-09-combat-skeleton-view-detailed-uml.md) §0 한 화면 요약, §3 프레임 순서, §5.4 명령 왕복
3. 코드 세 개를 위에서 아래로: `LC/CombatFlow.cs`(282줄) → `V/CombatViewSystem.cs`의 `Tick`·`ConsumeResult`·`FinalizeResult`(`:224-602`) → `V/CombatInputPolicy.cs`(182줄)
4. 입력을 건드린다면 [스테이지 입력 안내](../viewDocs/stage-input-guide.md), 씬에 붙는 시스템을 만든다면 [씬 연결 안내](../viewDocs/runtime-scene-binding-guide.md), 배치 Drag 표시를 건드린다면 [배치 Drag 규칙](../viewDocs/placement-drag-interaction.md)

정책의 원본은 [골격 설계](../specs/2026-09-30-combat-flow-skeleton-design.md) §4.1·§6.3~6.5와 [View 설계](../specs/2026-09-30-combat-view-design.md)다. 작업 이력과 남은 일의 원본은 [View 계획](../plans/2026-10-01-combat-view-plan.md) §7.56 이후다.

---

## 1. 10분 개념

### 1.1 누가 무엇을 하나

| 주체 | 하는 일 | 하지 않는 일 |
|---|---|---|
| **Core**(`CoreSystem/`) | 규칙 계산, 상태·결과를 Buffer에 게시 | 화면, 입력, 언제 처리할지 |
| **Flow**(`LC/CombatFlow`) | Buffer를 보고 다음 Job **하나**를 등록, 결과 확인 | 계산, 표시, Buffer 쓰기 |
| **LifeCycle**(`LC/CombatLifeCycle`) | 진입 요청 수명, 씬 들어가고 나가기, Flow·Exit 호출 | 진행 판단 |
| **ExitSequence**(`LC/CombatExitSequence`) | 승패·포기 뒤 이탈 순서 | 승패·재화 계산 |
| **View**(`V/`) | 입력을 해석해 **입력 Buffer에 기록**, 결과를 연출하고 **확인 번호(P)를 올림** | 규칙 판정, Core 상태 쓰기 |

### 1.2 용어

| 용어 | 뜻 |
|---|---|
| **N / H / A / S / P** | 명령·미리보기 번호(View 발급) / Core가 처리한 마지막 명령 번호 / Core가 응답한 마지막 미리보기 번호 / Core 결과 순번 / View가 확인한 결과 순번 |
| **미리보기(Preview)** | "이렇게 하면 어떻게 되나"를 Core에 묻는 요청. 상태를 바꾸지 않는다. Buffer에 **최신 하나**만 있다 |
| **명령(Command)** | 실제로 상태를 바꾸는 요청. 번호 순서대로 쌓이고 Core가 하나씩 처리한다 |
| **Gate** | `CombatCommandGate`. 명령 제출 직전 마지막 검사와 시작/턴 종료 **대기** 하나를 소유 |
| **Facts** | `CombatInputFacts`. 입력 판단 한 번에 쓰는 현재 사실 묶음. **저장하지 않고 매번 새로 읽는다** |
| **Policy** | `CombatInputPolicy`. 입력 허용 조건의 유일한 곳(무상태) |
| **Router** | `CombatInputRouter`. 입력을 누구에게 보낼지 정하는 곳 |
| **Playback** | `CombatResultPlayback`. 결과 하나의 재생 수명(세대·토큰) |
| **Layer** | 입력 맵 묶음. `Deployment`(배치), `Combat`(전투), 없음(입력 끔) |
| **Attach / Detach** | View가 Combat 씬의 `CombatSceneRoot`에 연결·해제 |
| **Completion** | Core가 기록한 전투 승패. 생기면 새 입력을 닫고 이탈을 시작한다 |
| **F1 / F2** | F1 = 오류 정지(`RuntimeFaultBoundary`), F2 = 정상 이탈(`CombatExitSequence`) |
| **Live / RecordInputOnly** | 실행 모드. Live는 실제 처리, RecordInputOnly는 개발 진단용(처리 Job을 등록하지 않고 입력 기록만 확인) |

### 1.3 한 바퀴

```text
입력 → Router → (Placement / Attack / Path) → Policy 검사 → CombatViewBuffer에 기록
     → Flow가 다음 프레임 Job 등록 → Runner 실행 → Core가 Buffer에 결과 게시
     → View가 결과 연출 → 표시 갱신 → 대기 해제 → Layer 맞춤 → 재요청 → 명령 삭제 → P = S
     → Flow가 확인하고 다음 Job 등록
```

---

## 2. 반드시 지킬 규칙

| # | 규칙 | 이유 | 어기면 |
|---|---|---|---|
| G1 | View는 **`CombatViewBuffer`에만 쓴다.** Core Buffer(`CombatStateBuffer` 등)에 쓰지 않는다 | Core 상태의 원본은 하나 | 상태가 두 군데서 바뀌어 재현 불가 버그 |
| G2 | View는 **규칙을 계산하지 않는다.** 가능 여부·비용·범위는 Core 미리보기 응답으로 받는다 | 규칙의 원본은 Core(P6) | Core와 화면이 다른 판정 |
| G3 | **새 Core 호출을 View에서 직접 하지 않는다.** 필요한 값은 Buffer로 받도록 Core에 요청한다 | 경계(B7 `GetTimeline`은 정리 대상인 예외다) | 경계 위반, Core 변경에 View가 깨짐 |
| G4 | 입력 허용 조건은 **`CombatInputPolicy`에만** 둔다. 호출하는 쪽에서 같은 조건을 다시 쓰지 않는다 | SSOT | 조건이 어긋나 어떤 경로만 통과 |
| G5 | Facts는 **저장하지 않는다.** 외부 표시 콜백을 부른 뒤에는 **다시 읽는다** | 콜백이 상태를 바꿀 수 있다 | 낡은 사실로 제출 |
| G6 | 미리보기를 지울 때는 **Buffer의 현재 요청 번호가 내 번호일 때만** 지운다(`ClearOwnedPreview` 패턴) | 미리보기 슬롯은 하나를 여러 소유자가 쓴다 | 남의 요청을 지워 응답이 사라짐 |
| G7 | 응답은 **내가 받은 번호와 같을 때만** 반영한다 | 늦은 응답 무시 | 이전 칸의 판정이 새 칸에 표시 |
| G8 | P(`AcknowledgePresented`)는 **`FinalizeResult` 끝에서만** 올린다 | Flow가 P를 보고 다음 Job을 등록 | 확인 전 결과가 덮이거나 Flow가 앞질러 감 |
| G9 | Unity 콜백(입력·버튼·포커스) 입구는 **try/catch로 `_faults.Report`** 한다 | Tick 밖 예외는 Bootstrap이 못 잡는다 | 오류 후에도 런타임이 계속 돌아 상태 꼬임 |
| G10 | 콜백 안에서는 **다시 검사**한다(오류·활성·연결·Policy) | 버튼 콜백과 Tick의 선후가 정해져 있지 않다 | 이탈 중 포기, 해제 뒤 표시 호출 |
| G11 | 늦게 올 수 있는 완료 통지는 **세대·토큰으로** 막는다 | Abort·재연결 뒤 옛 통지 | 다른 결과를 확인 처리 |
| G12 | 씬 오브젝트는 **`OnCombatSceneEntered`에서 한 번만** 찾는다. `Find`·`GetComponent`를 Tick·입력 경로에 쓰지 않는다 | FW 지침 §25 | 성능·수명 문제 |
| G13 | 정리(Detach·Release·Cancel)는 **여러 번 불려도 안전**하고, 하나가 실패해도 나머지를 시도한 뒤 모아서 던진다 | 오류 정지 중에도 끝까지 정리 | 자원·구독 누수 |
| G14 | Flow는 **Tick당 Job 하나**, Runner가 일하는 동안은 아무것도 안 한다 | 순서 보장의 근간 | 결과 순번 건너뜀(View 위반 보고) |
| G15 | Combat에서 나가는 전환은 **`CombatExit` 서비스로만** 한다. `LifeCycleManager.Change`를 직접 부르지 않는다 | guard가 거부한다 | `InvalidOperationException` |
| G16 | **Core 파일은 고치지 않는다.** 필요한 변경은 [Core 전달 문서](2026-09-24-view-core-data-boundary-handoff.md)에 요청으로 적는다 | 작업자 경계(CLAUDE.md) | 병합 충돌, 소유권 혼란 |
| G17 | 공용 파일(`ServiceKeys`, Factory, Group, asmdef, `JobType`, Buffer 명령 종류)은 **필요한 줄만** 바꾸고 공유 문서를 쓴다 | 여러 작업자가 쓴다 | 다른 브랜치 깨짐 |

---

## 3. 파일 지도 — 이걸 하려면 여기를 본다

| 하려는 일 | 먼저 볼 파일 |
|---|---|
| 전투가 왜 진행을 멈췄나 | `LC/CombatFlow.cs` `TryConfirmPending`(로그 `[CombatFlow] ... 전투 진행을 멈춥니다`) |
| 진입이 안 된다 | `LC/CombatLifeCycle.cs` `RequestEntry`·`ISystemTick.Tick`·`OnEnter`, `LC/Jobs/CombatPrepareJob.cs` |
| 입력이 안 먹힌다 | `V/CombatViewSystem.cs` `Receive`(`:855`) → `V/CombatInputRouter.cs` → 소유 입력 객체 → `V/CombatInputPolicy.cs` |
| 어떤 키가 어떤 요청인지 | `ViewSystem/StageInput/*InputController.cs`, `StageInputNames.cs`, `Assets/ProjectL/Input/PlayerInputActions.inputactions` |
| 배치 Drag | `V/PlacementDragInteraction.cs`, 표시는 `VD/PlacementGhost.cs`·`VD/HandPresenter.cs` |
| 공격 조준 | `V/AttackTargetingInteraction.cs` |
| 이동 경로·복귀 | `V/CombatPathInput.cs`, `V/CombatMovementInteraction.cs`, `ViewSystem/StageInput/MovementDraft.cs` |
| 결과 연출 | `V/OrderedCombatPresentation.cs`, `V/CombatSceneEventPresenter.cs`, `VD/FieldUnitPresenter.cs` |
| 필드 칸 색 | `VD/FieldView.cs`, `VD/FieldHighlightLayers.cs`(우선순위) |
| HUD | `VD/CombatHud.cs`, 유닛 위 HUD는 `VD/FieldUnitPresenter.cs` `RenderUnitHud` |
| 포기 버튼 | `V/CombatViewSystem.cs` `:884-970`, `VD/CombatAbandonPanel.cs`, `V/CombatInputPolicy.CanRequestAbandon` |
| 승패 뒤 이탈 | `LC/CombatExitSequence.cs` |
| 씬 참조 | `V/CombatSceneRoot.cs`(누락 검사 `FindMissingReference`) |
| 조립·시작 설정 | `ViewSystem/Boot/GameBootstrap.cs`, `Factory/Runtime/RunTimeSystemFactory.cs`, `Tool/Editor/PlayFromBootToggle.cs` |
| 오류 정지 | `Common/RuntimeFaultBoundary.cs`, `V/CombatViewSystem.StopInput` |

---

## 4. 작업 레시피

각 레시피는 **순서대로** 한다. "Core"라고 적힌 단계는 직접 하지 않고 Core 작업자에게 요청한다.

### R1. 새 전투 명령 추가 (예: 스킬 사용)

1. **Core(요청):** 명령 처리, 결과 게시, 필요하면 미리보기 응답. 새 `CombatViewCommandKind` 값과 `CombatViewCommandData` 생성 함수를 정한다(`BUF/CombatView/CombatViewCommand.cs`, 공용 파일).
2. `V/CombatInputPolicy.cs` `StageOf(CombatViewCommandKind)`에 단계를 추가한다. 빠뜨리면 `ArgumentOutOfRangeException`이다.
3. 키가 필요하면 R5대로 입력을 추가한다(`IStageInputReceiver` 메서드 → `CombatViewSystem` 구현 → `CombatInputRouter.Request` 값).
4. `CombatInputRouter.Route`에 분기를 넣는다. **순서**(무엇을 먼저 취소하고 언제 사실을 다시 읽는지)를 기존 분기와 맞춘다.
5. 제출 경로를 고른다.
   - Core 미리보기의 context가 필요 없으면 **Gate** `TrySubmit`(대기가 필요하면 `SubmitPending` — 단, `PendingInputCommand`는 시작/턴 종료만 받으므로 대기 종류를 늘리려면 설계부터 한다).
   - 이동처럼 Core가 준 context를 함께 보내야 하면 직접 `Submit`하되, 입구에서 Policy로 허용을 확인한다(`CombatPathInput.AllowsInput` 참고).
6. 입력 상태(모드·대상 선택)가 있으면 **새 소유 객체**를 만들고 `CombatInteraction`처럼 조합한다. `CombatViewSystem`에 상태를 두지 않는다.
7. 소유 객체에 `CancelPresentation`·`AfterResult`·`Stop`을 구현하고 `CombatInteraction`의 같은 이름 메서드에서 부른다(Layer 전환·결과 후처리·오류 정지 때 정리되게).
8. RecordInputOnly에서 어떻게 할지 정한다(처리 Job이 없어 미리보기 응답이 오지 않는다).
9. 테스트: `CombatInputPolicyTests`(허용 조건), `CombatInputRouterTests`(순서), 새 소유 객체 테스트, `CombatViewSystemTests`(통합).

### R2. 새 미리보기 추가

1. **Core(요청):** 응답 형식(`CombatPreviewResult`의 새 필드).
2. `BUF/CombatView/CombatViewPreview.cs`에 종류·생성 함수(공용 파일), `CombatInputPolicy.StageOf(CombatViewPreviewKind)`에 단계.
3. 소유 객체에서 `SetPreview`가 돌려준 번호를 기억하고, `PreviewAnswered`에서 **번호가 같을 때만** 반영한다(G7). 지울 때는 G6.
4. `CombatInteraction.PreviewAnswered`(또는 경로 입력이면 `CombatViewSystem.Tick`의 `_pathInput.OnPreview`)에서 새 소유자에게 전달한다. 이미 있는 소유자가 `true`를 돌려주면 그다음으로 넘기지 않는 구조다.
5. 결과 후처리(`AfterResult`)에서 이전 응답을 버리고 필요하면 **새 번호로** 다시 요청한다.

### R3. 비이동 연출 추가 (공격 이펙트·피격·사망)

1. `V/CombatSceneEventPresenter.BeginEvent`에서 해당 사건 종류일 때 연출을 시작하고, **연출이 끝난 뒤** `completed()`를 부른다. 지금은 즉시 부른다.
2. 연출 중 취소(`CancelPresentation`)가 오면 `_presentationVersion`이 바뀐다. 연출 완료 콜백에서 **시작 때의 버전과 같을 때만** `completed()`를 부른다(기존 코드가 그 패턴이다).
3. 연출이 빌린 자원(이펙트 풀·트윈)은 취소와 완료 모두에서 **통지 전에** 반환한다.
4. 유닛 본인의 모션은 이벤트 방송이 아니라 `FieldUnitPresenter`의 소유자 API로 간다([배치 Drag 규칙](../viewDocs/placement-drag-interaction.md)의 원칙).
5. 새 사건 종류면 `CanPresent` 목록에 넣는다. 빠지면 `OrderedCombatPresentation.Play`가 `NotSupportedException`을 던진다.
6. **바꾸지 않는 것:** `CombatResultPlayback`, `CombatViewSystem.ConsumeResult`/`FinalizeResult`, Flow. 순서·확인은 그대로 동작한다.
7. 테스트: `CombatSceneEventPresenterTests`, `CombatPresentationCleanupTests`(취소·늦은 완료).

### R4. 새 HUD·표시 요소 추가

1. Presenter(MonoBehaviour)를 만든다. **Buffer·Core를 모르고 값만 받는다.** `FindMissingReference()`를 둔다.
2. `V/CombatSceneRoot.cs`에 `[SerializeField]` 필드·접근자를 추가하고 `FindMissingReference`에 넣는다.
3. `V/CombatSceneDisplay.cs`: 생성자에서 꺼내고, `Install`/`Refresh`/`Clear`/`Release`에서 호출한다. `Release`는 `RunAll`에 넣는다.
4. ViewSystem이 직접 부를 메서드가 필요할 때만 `V/ICombatBattleDisplay.cs`에 추가한다. 추가하면 테스트의 가짜 표시(`CombatViewSystemTests`의 `FakeDisplay` 등)도 고쳐야 한다.
5. 그릴 값이 Buffer에 이미 있으면 `CombatDisplayValues`에 **계산 속성**으로 둔다(P7, `StageGold => State.StageGold` 참고). 같은 값을 따로 받지 않는다.
6. 같은 값이면 다시 쓰지 않는다(`CombatHud.Render`의 캐시 패턴).
7. **Combat.unity 씬 작업:** 새 참조를 연결하지 않으면 `ValidateReferences`가 예외를 던져 **Combat 진입이 멈춘다.** 코드 병합과 씬 연결을 같은 작업으로 묶는다.

### R5. 새 입력 키 추가

[스테이지 입력 안내 §6](../viewDocs/stage-input-guide.md)을 따른다. 요약:

1. `Assets/ProjectL/Input/PlayerInputActions.inputactions`에 Action 추가(이름은 글자까지 일치).
2. `StageInputNames`에 상수, 해당 Controller(`DeploymentInputController`·`CombatInputController`)의 `Configure`에 `input.Press(...)`.
3. `IStageInputReceiver`에 메서드 → `StageInputReceiverSlot`에 전달 → `CombatViewSystem`에서 `Receive(CombatInputRouter.Request.새값, ...)`.
4. `CombatInputRouter.Request`에 값, `Route`에 분기.
5. `IStageInputReceiver`를 구현하는 다른 클래스(테스트 가짜 포함)도 컴파일이 깨지므로 같이 고친다.

### R6. Run 종료 목적지를 Title로 바꾸기

1. Title LifeCycle·씬이 생기면 `LifeCycleKeys`·`SceneIndices`·Build Settings에 추가하고 `LifeCycleSystemFactory`에 등록한다.
2. `LC/CombatLifeCycle.cs:112` `RunEndDestination`을 바꾼다.
3. `LC/CombatExitSequence.cs:300` `ValidateDestination`의 허용 목록에 새 키를 넣는다. 빠지면 `ArgumentException`이다.
4. Title에서 새 게임을 시작한다면 `LC/Plans/CombatPlan.cs:19` `NewRunLoadoutKey`(임시 1)와 ExitSequence의 "Run 종료 → NewRun Job" 단계를 다시 설계한다(새 Run을 Title이 시작할지, 이탈이 시작할지).
5. 테스트: `CombatEntryIntegrationTests`(K21·K22 경로).

### R7. 이동·복귀 입력 고치기

- 가장 복잡한 곳이다. 먼저 [상세 UML §5.6·§6.5](../specs/2026-10-09-combat-skeleton-view-detailed-uml.md)를 보고 상태 전이를 확인한다.
- 허용 판단은 `CombatPathInput.AllowsInput()` 한 곳(입력 가용 ∧ 경로 대상 ∧ `CanEditPath`)이다. `CombatMovementInteraction`에는 `allowed`를 인자로 넘긴다. 여기에 허용 조건을 새로 쓰지 않는다.
- 제출 전 Core context의 전투·행위자·관찰자·의도·경로 일치 검사(`OnPreview`)를 지우지 않는다.
- 테스트: `MovementDraftTests`, `CombatMovementFeedbackTests`, `CombatMovementViewTests`, `CombatPathInputCleanupTests`.

### R8. 골격 순서를 바꿔야 할 때

- Flow의 분기 순서 원본은 [골격 설계 §4.1](../specs/2026-09-30-combat-flow-skeleton-design.md)의 표다. **표를 먼저 고치고** 코드를 맞춘다.
- 이탈 순서의 원본은 골격 설계 §6.4(정상)·§6.5(포기)다.
- 테스트: `CombatFlowTests`, `CombatFlowIntegrationTests`, `CombatPlanTests`, `CombatEntryIntegrationTests`.

---

## 5. 디버깅 안내

| 증상 | 먼저 의심할 곳 | 확인 방법 |
|---|---|---|
| Combat 진입 직후 멈춤, 로그 `[CombatSceneRoot] 씬 참조가 비어 있습니다: X` | Combat.unity의 `CombatSceneRoot` 참조 누락 | Inspector에서 X 연결. 지금 `abandonPanel`이 흔한 원인(CLAUDE.md 경고) |
| 로그 `Combat 씬에는 CombatSceneRoot가 하나 있어야 합니다. 찾은 수: N` | 씬에 Root가 0개 또는 2개 이상 | Hierarchy 검색 |
| 진입 거절 로그(`전투 진입 거절: ...`) | 노드가 전투 노드가 아님, 로스터 없음 | DB 정의(`Database`)면 개발 노드 번호가 그 맵 그래프의 전투 노드인지 확인 |
| `[CombatFlow] ... 전투 진행을 멈춥니다` | Core가 기대한 번호·결과를 남기지 않음 | 메시지의 종류(명령·미리보기·자동 진행)와 번호를 보고 Core 처리 확인. 예외가 아니라 Flow만 멈춘다 |
| `[CombatViewSystem] 확인하지 않은 결과를 건너뛰었습니다` | Core가 한 Job에서 결과를 두 번 게시했거나, View 확인 전 상태 변경 Job이 돌았음 | Flow의 `IsPresentationDone` 조건, Core 게시 횟수 |
| 입력이 전혀 안 먹힘 | 입력 Layer 미연결(첫 표시 전), 대기 중(시작/턴 종료), 종료 상태, 포기 확인창 열림 | `ReadFacts` 값: `IsBattleDisplayed`·`HasPendingCommand`·`HasCompletion`·`CurrentLayer`. `Receive` 첫 줄의 조건 |
| 명령은 기록되는데 처리 안 됨 | RecordInputOnly 실행 모드, 연출이 안 끝남(`P < S`) | Play From Boot 설정 창의 실행 모드, `PresentedResultSequence` vs `LatestResultSequence` |
| 연출이 끝나지 않음 | `BeginEvent`의 `completed` 미호출, 이동 연출 `IsMovementComplete` | `OrderedCombatPresentation._index`, `CombatSceneEventPresenter._moving` |
| 미리보기 색이 안 바뀜 | 다른 소유자가 미리보기를 덮었다(슬롯 하나), 응답 번호 불일치 | Buffer의 `Preview.Number`와 소유자의 요청 번호 |
| 포기 버튼이 회색 | 종료·이탈 중·RecordInputOnly·첫 표시 전 | `CombatInputPolicy.CanRequestAbandon` |
| 승패 뒤 StageSelect로 안 감 | Completion 확인 버튼 미연결, 이탈 단계 `Failed` | `ExitStatus`, HUD `completionButton` 참조 |
| 오류 한 번 뒤 모든 게 멈춤 | **정상 동작(F1).** 최초 오류만 로그에 남는다 | 콘솔의 **첫** 예외를 본다. 재생을 다시 시작해야 한다 |
| Run 종료 뒤 전환 직전 필드가 잠깐 비어 보임 | EndBattle 뒤 View가 스스로 Detach(상세 UML §5.9) | 의도 여부 확인 대기 |

**재생 설정:** 메뉴 `ProjectL > Play From Boot`를 켜면 어느 씬에서 Play해도 Boot부터 시작한다. `ProjectL > Play From Boot 설정...`에서 시작 구간·실행 모드·개발 진입·전투 정의(Fixture/Database)를 이 PC에서만 바꾼다. 켜져 있으면 콘솔에 덮어쓰기 로그가 한 줄 남는다.

---

## 6. 테스트 지도

테스트는 `Assets/Tests/EditMode/Framework/`에 있다. Unity Test Runner(EditMode)로 실행한다.

| 바꾼 곳 | 돌릴 테스트 |
|---|---|
| `CombatFlow`·`CombatPlan`·Job | `CombatFlowTests`, `CombatFlowIntegrationTests`, `CombatPlanTests` |
| 진입·이탈·포기(골격) | `CombatEntryIntegrationTests`, `StageMapCombatEntryBootSmokeTests` |
| 오류 경계·Bootstrap | `RuntimeFaultBoundaryTests`, `GameBootstrapFaultBoundaryTests` |
| `CombatViewSystem` | `CombatViewSystemTests`, `CombatViewRefreshTests`, `CombatResultPlaybackTests` |
| Policy·Router·Gate | `CombatInputPolicyTests`, `CombatInputRouterTests`, `CombatCommandGateTests` |
| 배치·공격 입력 | `CombatInteractionTests` |
| 이동 입력 | `MovementDraftTests`, `CombatMovementFeedbackTests`, `CombatMovementViewTests`, `CombatPathInputCleanupTests` |
| 표시·Presenter | `CombatDisplayReuseTests`, `CombatSceneEventPresenterTests`, `CombatPointerMappingTests`, `CombatPresentationCleanupTests` |
| Buffer | `CombatViewBufferTests`, `CombatStateBufferTests`, `CombatResultReadBindingTests` |
| DB 에셋 값 | `DataBaseAssetTests` |

새 `.cs`가 Unity csproj에 아직 없을 때 컴파일만 먼저 확인하려면 VS 2022 MSBuild에 `CustomAfterMicrosoftCommonTargets`로 새 파일을 Compile에 더하는 방법을 쓴다(CLAUDE.md).

---

## 7. Unity 확인 체크리스트

변경 뒤 재생으로 확인할 최소 경로다.

- [ ] Live, 개발 진입(Fixture): Combat 시작 → 배치(손패 카드 Drag·필드 복귀) → 드로우 → G로 전투 시작 → Q 이동·R 복귀 → E 공격 → Space 턴 종료 → 적 자동 진행
- [ ] Live, 승리·패배 → "결과 확인" → StageSelect(패배·최종 승리는 새 Run)
- [ ] Live, 배치·전투·자동 진행 중 포기 → 확인창(열린 동안 Q·E·Space 무시) → 확인 → "포기 · 결과 확인" → StageSelect·새 로스터. 취소하면 계속
- [ ] Live, 개발 진입(Database): 운영 DB 정의로 진입(노드가 전투 노드일 것)
- [ ] RecordInputOnly: 포기 버튼 없음, 개발 Layer 패널로 배치/전투 Layer 전환, 클릭으로 기록용 공격 행위자 선택
- [ ] StageSelect에서 노드 클릭 → 전투 진입(S2, 다른 작업자 경로)
- [ ] 콘솔에 오류 0, 경고는 기존 것만

---

## 8. 경계와 담당

| 영역 | 담당 | 이쪽에서 할 수 있는 것 |
|---|---|---|
| Core 규칙·Buffer 게시(`CoreSystem/`) | 천명님 | 요청 문서 작성만(G16) |
| Framework 공용(ServiceKeys·Factory·Group·asmdef·`JobType`) | 민형님 | 필요한 줄만 + 공유 문서(G17) |
| UnitCard·UnitHUD·타임라인 HUD 기준 | 현강님 | 요청·협의 |
| UI 화면(`GlobalSystem/UISystem/Screens/Stage/`: TimelineUI·CombatLogUI) | 담당 확인 필요 | 고치기 전에 담당자 확인 |
| StageSelect 노드 진입(`ViewSystem/StageMap/`, S2) | 다른 작업자 | 진입 API(`CombatEntry.RequestEntry`)는 그대로 제공 |
| FieldCamera, Architecture | 다른 View 작업자 | 건드리지 않음 |
| 전투 골격(`LC/`)·전투 View(`V/`) | 이 문서의 대상 | 설계 → 계획 → Phase 구현 |

---

## 9. 남은 일과 열린 문제

| # | 내용 | 상태 | 근거 |
|---|---|---|---|
| O1 | 턴 순서 출처 이중화: HUD는 `GetTimeline()`, `TimelineUI`는 `CommandState.Timeline` | Core에 VR0 계약 일치 확인 필요 → 확인되면 VR6 착수 | 평가 E1 |
| O2 | EndBattle 정리 조건(캐릭터 0명 등)이 문서화되지 않은 Core 계약 | Core 문서에 명시 요청 | 평가 E2 |
| O3 | Run 종료 목적지 `Stage`(임시), 새 Run 로드아웃 `1`(임시) | Title·로드아웃 선택 대기 | R6 |
| O4 | S3 챕터 계속: Core 연결 정의 원본 없음 → 임시로 Run 종료 | Core | View 계획 §7.56 |
| O5 | 테스트 전용 운영 코드(`BeginPreparation` 경로, `FieldUnitPresenter.Render`) | 정리 후보 | 평가 E6·M5 |
| O6 | `CombatViewSystem` 1009줄 — 포기 UI 분리 후보 | 다음 큰 수정 때 | 평가 R1 |
| O7 | 성능 미측정(유휴·Drag·자동 진행 연속) | X4 측정 계획 | 평가 §6 |
| O8 | 미전달 공유 메모(VR0 타임라인·이동 형식·UnitCard 분리·S4 목적지) | 사용자 발송 대기 | View 계획 §7.56 |
| O9 | Run 종료 뒤 NewRun 대기 동안 빈 필드가 보이는지 | Unity 확인 필요 | 상세 UML §5.9 |
