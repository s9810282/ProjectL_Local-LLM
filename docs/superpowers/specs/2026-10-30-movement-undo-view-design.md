# 이동 되돌리기 View 연결 설계

- 작성: 2026-10-30 / 상태: 2026-10-31 사용자 연속 진행 지시로 코드 연결·사용자 씬 연결 및 되돌리기 기본 동작 확인 완료.
- 사용자 범위: 현재 구조 확인 후 설계. 버튼·키 바인딩은 기존 것 재사용.
- 계약 원본: [Core 인계](../handoffs/2026-10-30-movement-undo-core-view-handoff.md). 과거 Core 계획을 현재 계약 대신 사용하지 않는다.
- 구조 기준: [공동 설계 기준 §4](../handoffs/2026-09-30-combat-view-design-session-handoff.md), 특히 §4.6; Framework architecture/programmer guide 및 Assets/AGENTS.md.

## 1. 현재 구조 확인

아래 경로는 `Assets/ProjectL/Script/Systems/` 기준이다.

| 경계 | 현재 코드와 빈 연결 |
|---|---|
| 키 | `ViewSystem/StageInput/CombatInputController`가 Return 등록을 생략. `StageInputKeyBindings`·`StageInputNames.Combat.Return`·PlayerInputActions의 Return 액션은 유지. 기본 키 R, 사용자 재바인딩 원본 유지 |
| 버튼 | `Combat/Display/Hud/CombatActionPanel`의 returnButton과 ReturnSlot=2 유지. Combat.unity에 참조 있음(Undo). UIInputBinding 연결은 있지만 Click(2)는 무시하고 SetAvailable에서 항상 숨김 |
| 수신 | `CombatInputConnection.ReturnPressed` → `CombatInputReceiver.RequestReturn` → Router.Request.Return까지 연결. `CombatViewSystem`의 BindActions도 연결. Router는 Return을 무시 |
| 가능 여부 | `CombatActionAvailabilityTracker`가 선택 아군의 요청/응답 수명을 소유. 키는 battle/turn/result/actor. QueryRevision/SourceRevision 검사는 없음 |
| 입력 사실 | `CombatInputFactsReader`가 Buffer 및 현재 연결의 값을 읽어 Facts 생성. QueryRevision은 아직 전달하지 않음 |
| 표시 투영 | `CombatInputSurfaces` → `CombatActionButtons.Read` → 패널. CanUseReturn 생성자 인자를 무시하고 false 고정 |
| 정책 | `CombatInputPolicy.StageOf`에 UndoMove가 없어 그대로 호출하면 예외. 튜토리얼 MoveOnly는 UndoMove를 허용하지 않음 |
| 명령 대기 | `CombatCommandGate.SubmitPending`이 제출 번호를 저장, 결과 표시 후 Resolve에서 일치 명령 번호의 수락/거절 모두 해제. 재생/확인은 기존 CombatResultPlayback 소유 |
| 옛 경로 복귀 | `CombatInteraction.RequestReturn`이 빈 `CombatPathInput.RequestReturn`으로 위임. 새 UndoMove는 이동 context/견적을 쓰지 않으므로 이 경로를 살리지 않음 |
| 재생 | EventTypeTable이 필드→필드 Returned를 Return으로 분류. OrderedCombatPresentation → CombatMovementPlayback → CombatMovementPresentation.ShowReturn이 회전 정리·Final 사실 적용·Render. 역방향 경로 재생 없음 |

## 2. 제안하는 사용자 동작

1. 전투 단계의 선택 아군에 대해 현재 가용성 응답이 준비되면 Undo 버튼을 활성화한다. 선택 없음·불가·조회 중은 버튼을 보이되 비활성화한다(기존 패널 Visible 정책 유지).
2. 버튼 또는 기존 Return 키(R 기본값)는 같은 수신 창구와 같은 실행 함수를 호출한다. 키 이름/설정 저장 키/슬롯 번호/입력 에셋을 바꾸지 않는다.
3. 공격 조준·경로 작성·아이템 Drag 등 `Interaction.IsInputOwnerActive`가 참이면 비활성화하고 키도 무시한다. 되돌리기를 위해 진행 중 입력을 자동 취소하지 않는다. 사용자가 취소를 끝내면 다시 조회한다.
4. 누른 순간 선택한 유닛의 현재 Ready 응답으로 UndoMove를 한 번 제출한다. 대기·연출·미표시 결과·적 턴·확인창·입력 정지·포기·전투 완료에는 제출하지 않는다.
5. 성공하면 원래 칸과 방향을 즉시 표시한다. 선택은 유효한 한 유지하며, 결과 후 기존 갱신으로 이동 가능 상태와 범위를 다시 조회한다. 되돌아가는 걷기·경로·추가 효과는 만들지 않는다.
6. 거절은 기존 명령 결과 소비를 따른다. 화면을 낙관적으로 옮기거나 자동 재제출하지 않는다. 이후 최신 조회로 버튼을 맞춘다.
7. 튜토리얼 설명/MoveOnly 제한은 현재 정책대로 되돌리기를 차단한다. RecordInputOnly는 Core의 실행된 이동 기록/Ready 응답이 없으므로 사용할 수 없다.
8. 원래 칸 클릭, 우클릭 재해석, 새 단축키, 목적지 강조/새 안내 UI는 이번 범위에 넣지 않는다. 배치 우클릭 ReturnUnit은 유지한다.

## 3. 책임과 데이터 흐름

### D1. 조회 수명은 기존 Tracker 한 곳

Facts에 `QueryRevision`(long)을 추가하고 FactsReader가 `CombatStateReadSink.QueryRevision`을 읽는다. Tracker 키에 이를 포함하고 기존 battle/turn/result/actor 키도 유지한다. revision 0은 사용 불가다.

응답 수락 때 자기 요청 번호/슬롯/actor/battle/turn과 `answer.SourceRevision == 현재 QueryRevision > 0`을 확인한다. Read 시에도 같은 키와 SourceRevision을 검사하여 Sync 이전 입력에도 오래된 Ready를 돌려주지 않는다. revision 변경은 기존 결과 순번이 같아도 Unknown→재조회로 간다.

응답을 받아 Ready가 된 뒤 다른 미리보기가 슬롯을 써도 키가 같으면 캐시는 유지한다. 미응답 요청 선점/선택 변경/정지는 기존 ClearPreview(자기 번호)로만 정리한다. A→B→A에서도 이전 요청의 늦은 응답은 새 요청과 일치하지 않아 버린다. 불가 응답으로 Core 체크포인트가 사라졌다고 추정하지 않는다.

### D2. 기능은 작은 연결 수명 객체에 응집

`Combat/Input/Interaction/CombatMovementUndoInput`(internal sealed, 새 순수 C# 협력 객체)을 둔다. 필요한 의존성만 받는다: 기존 Tracker, CommandGate, 최신 Facts 공급자, 현재 입력 기능 활성 여부 공급자. 가능 여부 읽기와 요청 제출을 소유한다. 토큰·유닛별 체크포인트·pending·게임 상태는 저장하지 않는다.

- `CanRequest`와 `Request`는 같은 자격 판정 함수를 사용한다.
- 자격: 입력 연결 유효, 전경 기능 없음, 미처리 명령 없음, Policy.CanSubmit(UndoMove), Tracker.Read의 현재 응답, MovementUndo.CanUndo 및 양수 token.
- Request는 입력 시작 시 현재 Facts(includeUnhandledCommand: true)를 읽어 검증하고 `UndoMove(answer.ActorId, answer.BattleInstanceId, answer.ControllerTurnSequence, answer.MovementUndo.MoveCommandNumber)`를 만들어 Gate.SubmitPending에 보낸다.
- 검증과 제출 사이 표시 콜백/모드 정리를 호출하지 않는다. Core가 실행 시 실제 상태를 다시 검증한다.
- 제출 후에는 기존 pending이 재진입을 막는다. 기존 조정 순서에서 가용성 무효화·버튼 갱신을 수행한다. 별도 완료/실패/재시도 상태 머신은 없다.

### D3. 조립과 라우팅

기존 `CombatSceneInputFactory`가 Tracker/Gate와 기능 객체를 조립하고 연결 입력 묶음이 보관한다. Router는 Return을 이 기능에 전달하는 한 갈래만 추가한다. InputSurfaces에는 같은 기능이 계산한 bool을 전달하고 CombatActionButtons가 CanUseReturn으로 투영한다. 버튼과 키가 서로 다른 가능 여부 식을 갖지 않는다.

기능 구현을 Router·CombatViewSystem·CombatPathInput에 분산하지 않는다. 옛 무동작 RequestReturn 경로의 실제 호출자를 확인하고, 새 기능과 혼동되는 주석/사용되지 않는 내부 위임만 정리한다. 공개 IStageInputReceiver.RequestReturn은 유지한다. 새 서비스/Job/Flow/Framework 등록은 필요 없다.

### D4. UI/정책

StageOf(UndoMove)=Combat을 추가한다. 튜토리얼 허용 목록은 확장하지 않는다. Router의 막힌 실제 입력 안내는 기존 reportBlockedInput 규칙을 따르며 조회/버튼 상태 계산은 안내하지 않는다.

CombatActionPanel은 기존 UIInputBinding(슬롯 2)을 통해 `_return`을 호출하고 다른 버튼처럼 Visible/CanUseReturn을 적용한다. returnButton을 필수 참조로 검사한다(운영 씬 참조 확인됨). UISystem의 기존 개폐와 Bind(null,null,null) 해제는 유지한다. Button.onClick/Inspector 이벤트/새 구독은 사용하지 않는다. 버튼 문구·외형·키 안내는 기존 에셋을 재사용한다.

### D5. 재생/수명

권위는 ordered Returned 사건이다. `UndoMovement` 메타데이터로 위치·행동·거리 값을 두 번 적용하지 않는다. 기존 ShowReturn의 Final 사실 적용 및 결과 끝 전체 Refresh를 사용한다. 결과 표시 완료 전 acknowledgment/pending 해제를 앞당기지 않는다.

Stop/Detach는 기존 연결 수명에서 Tracker 요청과 Gate를 정리한다. 이미 제출한 명령을 삭제/취소하거나 Core 이동 기록을 지우지 않는다. 실패/Cancel/Release와 재진입에서 이전 UI 콜백이 새 연결/새 선택을 제출하지 않는 기존 연결 검사를 보존한다.

## 4. SSOT·SOLID·FW·독립성 점검

| 기준 | 적용 |
|---|---|
| SSOT | 자격·token·원래 칸은 Core 응답, 최신성은 Buffer revision, 선택은 SelectionState, 조회는 Tracker, pending은 Gate, 표시 완료는 ResultPlayback. 추가 원본 없음 |
| SRP | UndoInput은 되돌리기 의도, Tracker는 공유 조회 수명, Policy는 공통 입력 제한, UI는 받은 값 표시, Factory는 생성/연결 |
| OCP | 기존 Return 입구와 Returned 재생 확장 지점 사용. 공격/이동 규칙·전투 Flow의 분기 추가 없음 |
| LSP | IStageInputReceiver와 UIBase의 기존 해제/입력 계약 유지. 기본값 Facts revision=0은 안전한 사용 불가이며 정상 운영 응답은 실제 revision 필요 |
| ISP | 되돌리기에 필요한 좁은 값/협력 객체만 전달. 전체 CoreGroup/서비스 resolver 전달 없음 |
| DIP | Buffer 포트·Facts 공급자·기존 Gate/Tracker 협력 사용. 구체 내부 협력 클래스를 위한 일회용 서비스 인터페이스는 추가하지 않음 |
| FW/JMH | 서비스/모듈/Builder 추가 없음(FW-001~003 영향 없음). 기존 Factory→연결 입력→수신/표시 경로 사용. InputSystem이 키·Layer, UISystem/UIInputBinding이 버튼 수명 소유. Core 직접 호출/Buffer 우회 쓰기 없음 |

§4.6 변경 시나리오 비교: Router에 조회·제출을 모두 넣으면 되돌리기 조건 변경 때 공격/포인터 라우팅 전체를 읽게 된다. 제안 구조에서는 조건 변경은 UndoInput과 그 시험, 버튼 외형 변경은 패널/에셋, 의존성 변경은 InputFactory에 모인다. 공통 revision 계약 변경은 Facts/Tracker와 기존 이동·공격 소비자 회귀가 필요하다. 이 공유 영향은 숨기지 않는다. 새 기능 때문에 FieldCamera/Architecture/Core 계산기를 수정하지 않는다.

## 5. 검증 경계와 리뷰

문서 작성 단계에는 실행 테스트를 주장하지 않는다. Core 인계의 기존 통과 수치는 이번 View 인수 증거가 아니다. 상세 Task와 Phase 종료 조건은 [구현 계획](../plans/2026-10-30-movement-undo-view-plan.md).

설계 제안의 주요 리뷰 결정: 전경 입력 중 비활성화, 불가 시 숨김 대신 비활성화, 즉시 복귀, MoveOnly에서 차단, returnButton 필수화. 변경 요청이 있으면 문서를 먼저 수정한다. 10-31 사용자 지시로 이번에는 Phase를 연속 진행했으며, 씬 편집·에디터 테스트는 사용자 담당으로 남겼다.

## 6. 2026-10-31 구현 대조

D1~D5의 입력/표시 연결을 반영했다. PendingInputCommand의 허용 종류에도 UndoMove를 추가했다. 씬/프리팹/입력 에셋과 Core 코드는 변경하지 않았고 새 Runtime 서비스도 등록하지 않았다. 2026-10-31 사용자가 씬 연결 및 되돌리기 정상 작동을 확인했다. UV3 기본 기능 인수는 사용자 확인 완료이며 자동 시험·세부 경계 사례는 별도 미확인이다. 자세한 실행 범위·정적 컴파일·사용자 확인 항목은 [계획 작성 기록](../plans/2026-10-30-movement-undo-view-plan.md#작성-기록)을 따른다.
