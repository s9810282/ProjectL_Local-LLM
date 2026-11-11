# 전투 View 조정자 나누기 설계 (CS1~CS6)

- 날짜: 2026-11-05
- 상태: **구현 완료(CS-a~CS-e, 2026-11-05).** Unity 컴파일·재생 문제 없음(사용자, Ua~Ud), 새 시험 없음, 시험 변경 없음. 묶음 끝 검토 컴파일 오류 없음·높음·중간 0, C2-02·C8-01 다 풀림(§13). 설계 결정은 2026-11-05 사용자 "권장대로 하고 Task 계획 써줘"(Q-CS1~Q-CS8 모두 A, P2~P10 그대로), 독립 검토 반영(§12). Task 계획 [coordinator-split-plan](../plans/2026-11-05-combat-view-coordinator-split-plan.md).
- 요청: 사용자 "조정자 나누기 설계부터 진행해". [평가 후속 수정 계획](../plans/2026-11-04-combat-view-evaluation-fixes-plan.md) §6.6 구조 단계의 첫 묶음이다.
- 대상 문제([검증된 문제 목록](2026-11-04-combat-view-evaluation-issues.md)):
  - C2-02 [중간, SOLID] 2단계: 조정자가 서비스 배선·씬 수명·결과 소비·입력 분배·표면 파생을 모두 맡는다.
  - C8-01 [중간, 캡슐화] 2단계: 연결 입력 묶음이 부품을 구체 타입으로 내놓는다.
  - 함께 C8-02 [낮음] 일부: 조정자의 호출자 없는 입구 셋과 선택 인자 둘.
  - 1단계는 3차-a에서 끝났다. 정보창 막기는 `CanInspectOnPress`, 수명 단계 입구는 `TickInteraction`·`BeforeResult`·`AfterResult`·`AfterAcknowledged`로 정리했다.
- 범위: `ViewSystem/Combat/`의 조정자(`CombatViewSystem`)·연결 입력 묶음(`CombatSceneInput`)·입력 사실 읽기·입력 연결, 확인창 둘의 가시성. 골격·Core·Framework·GlobalSystem·Factory는 고치지 않는다(P2).
- 기준 판: HEAD `dfb49b34`. 줄 번호는 이 판 기준이다.
- 규칙: View 작업이라 새 시험을 쓰지 않고 MSBuild도 돌리지 않는다. 지금 `Assets/Tests`에는 조정자·입력 묶음·사실 읽기·연결 상태를 쓰는 시험이 없다(2026-11-05 grep). 그래서 고칠 시험도 없을 것으로 본다.

## 1. 지금 구조 (확인 사실)

### 1.1 조정자가 맡는 것

`CombatViewSystem.cs`는 1186줄이고 필드가 30개다. 일곱 가지를 한 클래스가 맡는다.

| 묶음 | 줄 | 내용 |
|---|---|---|
| ① Framework 수명·서비스 받기 | 165-436 | `InitData`·`PostBindService`·`BindServices`(인자 18, 212-270)·`Initialize`·`ExportServices`·`Shutdown` |
| ② 씬 수명 | 438-482 | `OnCombatSceneLoaded`·`OnCombatSceneEntered`·`ReleaseScene` |
| ③ 연결 수명 | 484-835 | `Attach`·`AttachCreated`·`TakeDisplay`·`ContinueAttach`·`ShowBattle`·`CreateInput`·`Detach`·`StopInput`·`OnCombatSceneDisabled` |
| ④ Tick 순서 | 306-423 | 진입 연출 → 연출 진행 → Layer 연결 → 변경 관측 → 종료 Layer → 가용성·입력 기능 → 결과 소비, Tick 끝 버튼·환경 호버·카메라 |
| ⑤ 결과 소비 | 837-921 | `ConsumeResult`·`FinalizeResult` |
| ⑥ 진행 호스트·수명 판정 | 923-1048 | `ICombatProgressHost` 넷, `IsSceneActive`·`CanContinue`·`IsInputReady`·`IsCurrent`·`IsEntryPresentationPending`·`IsModalOpen`·`IsHoldingResult`·`IsPresenting`·`CanReceiveDeckInput` |
| ⑦ 입력 분배·표면 맞춤 | 1078-1186 | `RouteInput`·`SyncActionAvailability`·`SyncEnvironmentHover`·`SyncActionButtons`·`CancelInputPresentation` |

입력 요청 하나를 더하거나 결과 후처리 단계를 바꾸거나 서비스 하나를 더해도 이 파일을 고친다. 바뀌는 이유가 여럿이다.

### 1.2 재확인 줄

외부 호출(연출·표시·UI·입력 묶음)에서 돌아올 때마다 그 안에서 오류 보고·분리(Detach)가 일어났는지 다시 본다. 지금 그런 줄이 34개다. `CanContinue()`가 28줄(정의 둘 포함), `ReferenceEquals(`가 17줄(연결 비교 16 + 514 표시 비교 1), `IsCurrent(token)`가 5줄이고 한 줄에 둘이 겹친 곳이 있다. 규칙은 넷이다.

| 규칙 | 지금 식 | 쓰는 곳 |
|---|---|---|
| R-A 이어 가도 됨 | `CanContinue() && ReferenceEquals(_connection, c)` | Tick 입력 단계·`RouteInput`·`Sync*`·연결·진행 호스트 |
| R-B 정리 신호 | `ReferenceEquals(_connection, c)`만 | `RouteInput`의 `FocusLost`·`SelectCanceled`. 입력 해제의 Unbind 도중 재생이 이미 끊겨도 지나가야 한다(R2) |
| R-C 후처리 토큰 | `CanContinue() && _playback.IsCurrent(token)` | `FinalizeResult`. 토큰이 맞으면 같은 연결이다. Detach가 표시를 놓기 전에 재생을 먼저 끊기 때문이다(SV2) |
| R-D 입력 준비 | R-A ∧ `IsInputReady` | 정보창 선택 뒤(1120) |

R-A는 줄마다 손으로 두 항을 쓴다. 한 항을 빠뜨리면 분리된 연결에 이어 쓰는 경로가 생긴다(C2-02 실패 시나리오).

### 1.3 입력 묶음 부품 노출 (C8-01)

`CombatSceneInput`은 부품 여섯을 구체 타입 internal 속성으로 낸다: `Gate`·`Interaction`·`Deployment`·`EndTurn`·`ActionAvailability`·`MovementUndo`. 밖에서 부품을 직접 읽는 곳은 다음과 같다.

| 자리 | 읽는 것 |
|---|---|
| `CombatViewSystem.cs:896` | `Gate.Resolve(result)` |
| `CombatViewSystem.cs:1061` | `Gate.HasPending` |
| `CombatViewSystem.cs:1106` | `Deployment.Request()` |
| `CombatViewSystem.cs:1130` | `EndTurn.Request()` |
| `CombatViewSystem.cs:1150-1151` | `ActionAvailability.Sync(…, Interaction.IsInputOwnerActive, …)` |
| `CombatViewSystem.cs:1182-1183` | `Interaction.CanEnterAttack(facts)`·`Interaction.IsAttackActive`·`MovementUndo.CanRequest`·`ActionAvailability.Read(facts)` |
| `CombatInputFactsReader.cs:71·73·77·84` | `Interaction.IsAttackActive`·`HasCurrentAcceptedEmptyMovementRange(actor)`·`IsItemDragActive`·`ResolvePointerTarget(layer)` |

확인창 판정 `HasUnplacedUnits`(`CombatDeploymentConfirmation.cs:42`)와 `CountIdleUnits`(`CombatEndTurnConfirmation.cs:47`)는 자기 클래스 안에서만 쓰는데 internal이다.

### 1.4 호출자 없는 입구 (C8-02 일부)

- `Input` 속성, `RequestReturn()`, `AttachDisplay()`는 저장소에 호출자가 없다(grep).
- `BindServices`의 선택 인자 `loading = null, runDeck = null`은 운영 `PostBindService`가 늘 넘긴다. 이 선택 인자 때문에 null 갈래가 넷 있다.
  - `Initialize`: `_unitPreparationReader != null`
  - `Attach`: `_unitPreparationReader == null`이면 예외
  - `TickCore`: `_loading == null`이면 예외
  - `TakeDisplay`: `_loading == null`이면 예외
- `HasPendingPresentation`(internal)은 조정자 안(`CanShowCompletion`)에서만 쓴다.

### 1.5 다른 작업자 줄 (blame, `dfb49b34`)

| 파일 | 줄 | 작성자 |
|---|---|---|
| `CombatViewSystem.cs` | 1128-1132 턴 종료 갈래, 1175 `SyncConfirmations` | 천명님(`57af3244`) |
| `CombatViewSystem.cs` | 1182-1183 `Surfaces.Sync` 인자 | 호진님(`8339e308`) |
| `CombatViewSystem.cs` | 719 입력 재료 `afterConfirmedSubmit`(CS-0에서 더 찾음) | 천명님(`57af3244`) |
| `CombatViewSystem.cs` | 251 표시 값 읽기 생성(CS-0에서 더 찾음) | 현강님(`0a6fe9c4`) |
| `CombatSceneInput.cs` | 확인창 입구·`EndTurn` 속성·`CloseConfirmations`·`SyncConfirmations` 등 32줄 | 천명님(`57af3244`) |
| `CombatSceneInput.cs` | 115-116 `ClearActionAvailability` | 현강님(`e2cb9912`) |
| `CombatEndTurnConfirmation.cs` | `CountIdleUnits` 포함 96줄 | 천명님 |
| `CombatDeploymentConfirmation.cs` | `HasUnplacedUnits` 포함 111줄 | 호진님(Leehojin 계정) |

`57af3244`는 제목이 "[천명]"이고 Claude 공동 작성이다. 그런데 같은 내용(턴 종료 확인창·토네이도 화살촉·턴 안내·Notice 모양)이 CLAUDE.md 11-03에는 우리 쪽 사용자 요청으로 적혀 있다. 그래서 이 줄을 누구에게 알릴지는 Q-CS8로 묻는다. `CombatSceneInput.cs:5`의 using(현강님 병합 `0a6fe9c4`)은 고치지 않는다.

결과 소비·Tick·재확인 줄과 `CombatInputConnection`·`CombatInputFactsReader`·`CombatViewConnection`은 모두 우리 줄이다.

## 2. 목표와 비목표

목표

- 조정자는 수명(모듈·씬·연결)과 단계 순서만 맡는다.
- 결과 하나의 후처리와 입력 분배·표면 맞춤은 각자 클래스 하나가 맡는다.
- 재확인 규칙(R-A~R-D)은 한 곳에 이름으로 둔다. 옮긴 곳은 그 이름을 부른다.
- 연결 입력 묶음은 부품을 숨기고 묶음 수준 입구만 낸다.
- 동작은 같다. 순서·조건·예외 문구를 그대로 둔다. 바뀌는 것은 §5에 적은 것뿐이다.
- 공개 계약은 그대로다: `ICombatViewService`·`ICombatProgressView`·`ICombatProgressHost`.
- Factory 연결 줄도 그대로다: `InGameViewSystemFactory.cs:68`·`:93`, `CombatSceneInputFactory`의 서명.

비목표

- 연결 수명(Attach~Detach·StopInput)을 옮기지 않는다. 조정자의 본업이다.
- 씬 표시 나누기(C2-03)는 따로 묶음이다(계획 §6.6).
- Tick 단계와 결과 후처리 단계의 순서를 바꾸지 않는다.
- 재확인 지점 수를 줄이지 않는다. 외부 호출 하나마다 하나가 필요하다. 바뀌는 것은 각 지점이 규칙 이름 하나를 부르게 되는 것이다.
- 사실 읽기 횟수를 줄이지 않는다(P4).

이번에 남는 것(검토 반영):

- C2-02가 짚은 "입력 재료 16개를 직접 묶음"(`CreateInput` 703-719)은 조정자에 남는다. 재료 중 다섯이 조정자의 판정 대리자라 묶음으로 옮겨도 넘기는 것이 같기 때문이다.
- C8-01이 짚은 사실 읽기의 `connection.Selection` 따라가기도 남는다. 연결 상태가 가진 좁은 읽기 계약(`ICombatSelectionReader`)이라 부품 노출과는 다르다.
- 둘을 남겨도 두 문제를 "다 풀림"으로 셀지는 묶음 끝 검토(CS-e)에서 정한다(§7).

## 3. 설계

나눈 뒤 모양은 다음과 같다.

```text
CombatViewSystem (조정자: 모듈·씬·연결 수명, Tick 순서, 진행 호스트, 수명 판정)
 ├─ ICombatConnectionGuard ← 조정자가 명시 구현 (CS1)
 ├─ CombatViewBindings     서비스 묶음, PostBindService가 만듦 (CS5)
 ├─ CombatSceneInputDriver 입력 분배·표면 맞춤·Layer 맞춤, 연결을 인자로 받음 (CS3)
 ├─ CombatResultConsumer   결과 하나의 재생 시작·후처리, 연결을 인자로 받음 (CS4)
 └─ CombatViewConnection → CombatSceneInput (부품 숨김, 묶음 입구만, CS2)
                             └─ CombatActionButtonState 행동 버튼에 넘길 값 넷 (CS2)
```

### CS1 연결 재확인 계약 `ICombatConnectionGuard`

새 좁은 계약이다. 조정자가 명시 구현하고, 옮긴 두 클래스는 조정자 전체가 아니라 이것만 받는다.

```csharp
internal interface ICombatConnectionGuard
{
    // R-A: 오류 정지 전 ∧ 진입 활성 ∧ 재생 연결 ∧ 이 연결이 지금 연결.
    bool IsLive(CombatViewConnection connection);
    // R-B: 이 연결이 지금 연결인지(재생·오류와 무관). 입력 해제 중 정리 신호가 씀(R2).
    bool IsConnected(CombatViewConnection connection);
    // R-D: 입력 판단에 쓸 수 있는지(IsInputReady).
    bool IsInputReady { get; }
}
```

조정자 구현은 다음과 같다. 명시 구현이라 조정자 안에서도 쓰도록 같은 이름의 private 도우미 둘을 두고, 명시 구현은 그것을 부른다.

- `IsLive(c) => CanContinue() && ReferenceEquals(_connection, c)`
- `IsConnected(c) => c != null && ReferenceEquals(_connection, c)`
- `IsInputReady`: 지금 속성 그대로

조정자에 남는 R-A 자리(진입 연출 360, 결과 화면 확인 966)는 `IsLive(connection)`을 부른다. 지금 `ReferenceEquals`만 보는 자리(570, 첫 그리기 687)는 `IsConnected(connection)`을 부른다. 687을 `IsLive`로 바꾸면 안 된다. 입력 생성 안에서 오류 보고가 일어나면 입력 정지(StopInput)가 연결을 남긴 채 `CanContinue`만 거짓으로 만든다. 그때 `IsLive`는 첫 그리기 성공 표시와 Layer 맞춤을 건너뛰게 해 동작이 바뀐다.

R-C는 결과 소비 안의 `IsCurrent(connection, token) => guard.IsLive(connection) && playback.IsCurrent(token)` 하나다(P8). 자리는 `V/ICombatConnectionGuard.cs`다(Q-CS7).

대안(Q-CS2 B): 연결 상태가 "끝남" 깃발을 갖고 Detach가 `_connection = null` 자리에서 세운다. 그러면 `connection.IsLive`가 깃발과 조정자가 넘긴 판정 대리자로 답한다.

### CS2 입력 묶음 입구 마무리·부품 숨기기 (C8-01 2단계)

`CombatSceneInput`의 부품 속성 여섯을 private readonly 필드로 내린다. 대신 다음 입구를 더한다.

| 새 입구 | 넘기는 곳 | 쓰는 곳 |
|---|---|---|
| `ResolveCommand(CombatResult)` | `_gate.Resolve` | 결과 소비 |
| `HasPendingCommand` | `_gate.HasPending` | 조정자 `HasPendingCommand` |
| `RequestEnterCombat()` | `_deployment.Request()` | 입력 분배 |
| `RequestEndTurn()` | `_endTurn.Request()` | 입력 분배 |
| `SyncAvailability(in facts, characters, int answeredPreview)` | `_actionAvailability.Sync(facts, characters, _interaction.IsInputOwnerActive, answeredPreview)` | 입력 분배 |
| `ReadActionButtons(in facts)` → `CombatActionButtonState` | 값 넷을 이 순서로 읽음: `_interaction.CanEnterAttack(facts)`·`_interaction.IsAttackActive`·`_movementUndo.CanRequest`·`_actionAvailability.Read(facts)` | 입력 분배가 `Surfaces.Sync`에 넘김 |
| `IsAttackActive`·`IsItemDragActive` | `_interaction.*` | 사실 읽기 |
| `HasCurrentAcceptedEmptyMovementRange(EntityId)`·`ResolvePointerTarget(InputLayer)` | `_interaction.*` | 사실 읽기 |

- 이미 있는 입구는 그대로 둔다: `Route`·`TickCamera`·`SyncEnvironmentHover`·`HasEnvironmentHover`·`TickInteraction`·`BeforeResult`·`OnResult`·`AfterResult`·`AfterAcknowledged`·`CancelPresentation`·`Stop`·`CloseConfirmations`·`SyncConfirmations`·`IsConfirmationOpen`·`ClearActionAvailability`·`DeckPlacement`·`ItemInput`·`CanInspectOnPress`.
- 생성자와 입력 Factory는 그대로다(P5). 부품을 받아 필드에 둘 뿐이다.
- `ReadActionButtons`는 값만 돌려주고 표면을 모른다. 새 읽기 전용 값 `CombatActionButtonState`(공격 진입 가능·공격 중·되돌리기 가능·가용성)를 `Input/ActionButtons/`에 둔다(P6). 입력 분배가 `connection.Surfaces.Sync(facts, s.CanEnterAttack, s.IsAttackActive, s.CanUseReturn, s.Availability)`를 부른다. 평가 순서와 `CombatInputSurfaces.Sync`의 서명(호진님 줄)은 그대로다.
- `CombatInputFactsReader`의 네 줄은 새 입구를 부른다.
- 확인창 판정 둘(`HasUnplacedUnits`·`CountIdleUnits`)을 private으로 내린다.
- 클래스 설명의 "아직 Interaction을 직접 읽습니다" 문장을 지운다.

### CS3 입력 분배·표면 맞춤 `CombatSceneInputDriver` (C2-02)

자리는 `V/Input/CombatSceneInputDriver.cs`다. 서비스 수명 입력 객체라 폴더 배치 안내대로 `Input/` 바로 아래다(입력 연결·입력 재료 옆). 수명은 서비스 수명이다(Q-CS1 A). 조정자가 `Bind`에서 사실 읽기 다음에 만든다(P2). 필드에 연결을 두지 않고 매 호출 연결을 인자로 받는다. 그래서 "처음 잡은 연결 = 지금 연결"을 확인하는 지금 방식이 그대로다.

- 생성 인자: `ICombatConnectionGuard`·`CombatInputFactsReader`·`CombatInputConnection`·`ICombatStateBufferReadSink`·`ICharacterBufferReadSink`
- 응답 단계 사실 공급자(`() => facts.Read(followUp: true)`)는 생성 때 한 번 만든다. 지금 조정자의 `_readFollowUpFacts`를 옮기는 것이다.
- Layer 맞춤(`SyncInputLayer`)도 여기로 온다(P9). 목표 Layer는 정책이 정하고 입력 연결은 적용만 한다는 입력 연결의 지금 설명(`CombatInputConnection.cs:16`, `CombatInputReceiver.cs:13`)이 그대로 맞는다.

| 새 메서드 | 옮겨 오는 것 |
|---|---|
| `Route(connection, request, x, y)` | `RouteInput` 1094-1144 |
| `SyncAvailability(connection)` | `SyncActionAvailability` 1146-1152 |
| `SyncEnvironmentHover(connection)` | 1155-1163 |
| `SyncActionButtons(connection)` | 1170-1184. 끝의 `Surfaces.Sync`는 묶음 `ReadActionButtons` 값으로 부른다 |
| `bool TickInputs(connection, bool previewAnswered)` | `TickCore` 403-416. 가용성 → 입력 기능 → 가용성 순서, 이어 가도 되면 참. 묶음의 `TickInteraction`과 헷갈리지 않게 이름을 다르게 둔다 |
| `TickEnd(connection, float deltaTime)` | `Tick` 314-320. 행동 버튼 → 환경 호버 → 카메라 |
| `bool CanReceiveUiInput()` | `CanReceiveDeckInput`: `guard.IsInputReady && !CombatInputPolicy.IsInputBlocked(facts.Read())` |
| `SyncInputLayer()` | 조정자 `SyncInputLayer`(929-931): `inputConnection.Apply(CombatInputPolicy.GetDesiredLayer(facts.Read()))` |

조정자 쪽 연결은 다음과 같다.

- 입력 연결에 넘기는 분배 대리자: `(r, x, y) => _inputDriver.Route(_connection, r, x, y)`. 서비스를 받을 때 한 번 만든다.
- 입력 재료의 `canReceiveUiInput`은 `_inputDriver.CanReceiveUiInput`이다.
- 입력 재료의 `afterConfirmedSubmit`은 `() => _inputDriver.SyncActionButtons(_connection)`이다.
- 지금도 이 셋은 부르는 순간 `_connection`을 읽는다. 읽는 때가 같다.

`Route` 안의 재확인은 줄마다 다음처럼 바뀐다.

| 지금(줄) | 새 식 |
|---|---|
| 1100 카메라 `CanContinue()` | `guard.IsLive(connection)` |
| 1107 배치 진입 뒤 | `guard.IsLive(connection)` |
| 1115 선택 누름 조건 | `guard.IsLive(connection)` |
| 1120 정보창 선택 뒤 | `!guard.IsLive(connection) \|\| !guard.IsInputReady` |
| 1126 정리 신호 | `!guard.IsConnected(connection) \|\| (!guard.IsLive(connection) && !cleanupRequest)` |
| 1133·1136·1139 | `guard.IsLive(connection)` |
| 1148 가용성 | `!guard.IsLive(connection) \|\| connection.Input == null` |

- 1100·1115는 지금 `CanContinue()`만 본다. 그 앞에 외부 호출이 없어 인자 연결이 지금 연결과 같다. 그래서 같은 연결 항이 더해져도 값이 같다.
- 1126의 새 식은 지금 식 `!Ref || (!CanContinue && !cleanup)`과 연결이 있을 때 같다. `IsConnected`가 참이면 `IsLive`는 `CanContinue`와 같기 때문이다. 다른 점은 두 연결이 모두 null일 때 하나다. 지금은 계속 진행해 null 참조 예외가 나고, 새 식은 멈춘다. 수신 창구가 씬 연결 활성(`IsSceneActive`, 연결 있음 포함)일 때만 넘기므로 닿지 않는 차이다(§5).

재확인을 더하지 않는 곳(검토 반영): `TickEnd`의 세 단계 사이와 `SyncActionButtons`·`SyncEnvironmentHover` 안에는 지금도 재확인이 없다. 지금은 단계마다 `_connection?.Input`을 다시 읽고, 새 판은 인자로 한 번 잡은 연결을 쓴다.

- 단계 중간에 분리가 일어나면 지금은 다음 단계가 null을 보고 건너뛴다. 새 판은 정지된 묶음을 본다.
- 정지된 묶음의 환경 호버·카메라는 null이라 아무것도 하지 않는다(`CombatSceneInput.Stop`). 행동 버튼 맞춤은 `Shown`인 옛 연결의 표면을 다시 쓸 수 있으나, 분리는 그 표면을 이미 숨겼고 표시도 반환했다.
- 그래서 화면 차이는 없다고 본다. 다만 카메라 인자의 사실 읽기가 한 번 더 평가될 수 있다.
- 재확인을 더하지 않는다. 확인창 콜백(`afterConfirmedSubmit`)이 행동 버튼 맞춤을 그 자리에서 부르는 지금 동작에 기대기 때문이다. Phase CS-b의 Unity 확인에서 확인창 수락 직후 버튼을 본다.

### CS4 결과 소비 `CombatResultConsumer` (C2-02)

자리는 `V/CombatResultConsumer.cs`다(재생 수명 `CombatResultPlayback` 옆, Q-CS7). 서비스 수명이고 조정자가 `Bind`에서 만든다.

- 생성 인자: `ICombatConnectionGuard`·`RuntimeFaultBoundary`·`CombatResultPlayback`·`ICombatStateBufferReadSink`·`CombatDisplayValueReader`·`ICombatViewBufferWriteSink`·`ICharacterBufferReadSink`·`CombatInputFactsReader`·`CombatSceneInputDriver`
- `Consume(CombatViewConnection connection, CombatResult result)` 하나가 지금 `ConsumeResult`·`FinalizeResult`를 그대로 옮긴다. 구간 표시 `Combat.Result.Finalize`도 함께 가져간다(P7).
- `_connection.X`는 인자 `connection.X`로 바뀐다.
- `CanContinue() && ReferenceEquals` 자리(845·914)와 `CanContinue()`만 보던 자리(851)는 `guard.IsLive(connection)`가 된다.
- `IsCurrent(token)` 넷(887·892·900·905)은 `IsCurrent(connection, token)`이 된다.
- 결과 후처리 순서는 그대로다.
  1. `Display.Refresh` → `IsCurrent`
  2. `Input.OnResult` → `IsCurrent`
  3. `Input.ResolveCommand` → `driver.SyncInputLayer()` → `IsCurrent`
  4. `Input.AfterResult` → `IsCurrent`
  5. `RemoveCommandsThrough` → `AcknowledgePresented` → `CompleteFinalization`
  6. `driver.SyncAvailability` → `IsLive` → `Input.AfterAcknowledged`

Layer 맞춤은 입력 분배의 `SyncInputLayer()` 하나다(P9). 조정자와 결과 소비가 함께 부른다.

조정자의 `TickCore` 끝은 다음처럼 된다.

```csharp
CombatViewConnection connection = _connection;
if (!_inputDriver.TickInputs(connection, signal.PreviewAnswered))
    return;
if (signal.UnpresentedResult != null)
    _resultConsumer.Consume(connection, signal.UnpresentedResult);
```

대안(Q-CS3 B): 결과 소비를 조정자에 남기고 CS2 입구만 부르게 바꾼다.

### CS5 서비스 묶음 `CombatViewBindings` (C2-02 재료 조립, C8-02 일부)

자리는 `V/CombatViewBindings.cs`다(Q-CS7). 지금 `PostBindService`가 하는 서비스 조회를 한 곳으로 옮기고, 18개 인자를 값 하나로 넘긴다.

순서와 예외는 지금 그대로 둔다(P10, 검토 반영).

1. `static CombatViewBindings From(ISystemServiceStoreReader resolver)`는 지금 `PostBindService`의 인자 평가만 옮긴다.
   - Buffer·그룹 서비스를 꺼내고 `CombatDisplayServices.From(global)`을 만든다.
   - 문구가 있는 `InvalidOperationException` 넷(Tutorial 상태 읽기·Tutorial 요청 쓰기·로딩 상태·확정 덱 읽기)을 지금 문구 그대로 던진다.
   - 묶음은 받은 값을 담기만 하고 다른 검사는 하지 않는다.
2. `Bind(CombatViewBindings bindings)`의 몸체는 지금 `BindServices` 몸체 순서 그대로다.
   - 연결 중이면 거절한다.
   - 사실 읽기·입력 재료·확인창을 비운다. 새로 생기는 입력 분배·결과 소비도 여기서 함께 비운다.
   - `ArgumentNullException(nameof(…))` 검사와 확인창 계약 검사(`textPresentation is IUIConfirmationPresentation`)를 지금 순서·형식대로 한다. `items`는 지금처럼 직접 검사하지 않는다.
   - 파생 객체를 만든다: 표시 값 읽기·준비 대상 읽기·막힘 보고·입력 연결·사실 읽기·입력 분배·결과 소비.
3. 그래서 지금처럼 다음 둘이 지켜진다.
   - 조회 단계에서 실패하면 옛 객체가 남는다.
   - 인자 검사에서 실패하면 옛 사실 읽기·재료가 다시 쓰이지 않는다.
4. 입력 재료(`_inputSources`)를 비우는 줄은 꼭 남아야 한다. 재료의 `canReceiveUiInput`이 그때의 입력 분배에 묶이기 때문이다.

나머지는 다음과 같다.

- 조정자는 `PostBindService(resolver) => Bind(CombatViewBindings.From(resolver))`가 된다.
- 조정자 필드는 서비스 하나하나 대신 묶음 하나와 파생 객체를 갖는다. 30개에서 약 22개로 준다(추정, 계획에서 다시 셈 — 계획 TP-6).
- 조회는 지금도 조정자 안에서 하므로 Factory는 그대로다(P2). 아는 것이 같고 자리만 옮긴다.

### CS6 호출자 없는 입구 걷기 (C8-02 일부)

- `Input`·`RequestReturn()`·`AttachDisplay()`를 지운다.
- `HasPendingPresentation`은 private으로 내린다.
- `loading`·`runDeck`는 지금도 `PostBindService`가 null이면 예외를 던진다. 선택 인자였던 것은 `BindServices`뿐이다. CS5로 그 입구가 없어지므로 §1.4의 null 갈래 넷을 지운다. `IsEntryPresentationPending`의 `_loading?.IsLoading ?? false`는 `_loading.IsLoading`이 된다. 이 식은 연결이 있을 때만 읽으므로 묶음이 늘 있다.
- C8-02의 나머지(`FieldUnitView`·`UnitClipChannel`·`CombatPhaseTransitionSettings.From`)는 다른 파일이라 남는다. 계획 §6.7 남은 낮음에 그대로 둔다.

### 조정자에 남는 것

- 모듈: `InitData`·`PostBindService`·`Bind`·`Initialize`·`ExportServices`·`Shutdown`
- 씬 수명: `OnCombatSceneLoaded`·`OnCombatSceneEntered`·`ReleaseScene`
- 연결 수명: `Attach`·`AttachCreated`·`ReleaseUnowned`·`TakeDisplay`·`Begin/EndConnectionState`·`ContinueAttach`·`ShowBattle`·`CreateInput`·`Detach`·`StopInput`·`OnCombatSceneDisabled`
- Tick 순서: `TickCore`의 진입 연출·연출 진행·Layer 연결·변경 관측·종료 Layer 단계, `_inputDriver.TickInputs`·`_resultConsumer.Consume` 호출, Tick 끝 `_inputDriver.TickEnd`
- 진행 호스트 넷과 `ICombatConnectionGuard` 구현(private 도우미 `IsLive`·`IsConnected`)
- 수명 판정·`ReadFacts`·`SyncInputLayer`(입력 분배에 넘김)·`CancelInputPresentation`(한 줄씩)

크기 추정: 약 1186줄에서 약 870줄로 준다(주석 포함, 필드 약 22). 새 파일은 입력 분배 약 190줄, 결과 소비 약 130줄, 서비스 묶음 약 150줄, 계약 약 30줄, 행동 버튼 값 약 30줄이다. 남는 줄의 대부분은 연결 수명과 그 주석이다. 바뀌는 이유가 수명과 순서 하나로 모이는 것이 목표다. 줄 수가 크게 주는 것은 목표가 아니다.

## 4. 지킬 것 (동작 같음)

1. Tick 단계 순서(§1.1 ④)와 Tick 끝 순서(버튼 → 환경 호버 → 카메라).
2. 결과 후처리 순서와 각 `IsCurrent` 자리(CS4).
3. Detach·StopInput 순서. 연결 비우기(`_connection = null`)는 입력 해제 뒤, `EndConnectionState` 앞이다. 이번에 옮기지 않는다.
4. R2: 입력 해제 중 `FocusLost`·`SelectCanceled`는 같은 연결이면 `CanContinue`가 거짓이어도 지나간다(R-B).
5. `TakeDisplay`의 마지막 문장이 소유권 이전이다. 그대로 둔다.
6. 재확인은 지금 자리마다 하나씩 그대로다. 줄마다 대응은 CS3 표·CS4 목록, 조정자에 남는 자리는 아래 표와 같다.

| 지금 줄 | 새 자리·식 |
|---|---|
| 293·313·325·368·375·399 | 조정자, 그대로(`CanContinue()`) |
| 360 | 조정자, `IsLive(connection)` |
| 408·414·417 | 입력 분배 `TickInputs` 안 `guard.IsLive` + 조정자가 반환값으로 멈춤 |
| 514 | 조정자, 그대로(준비 대리자) |
| 570·687 | 조정자, `IsConnected(connection)`(지금 `ReferenceEquals`만) |
| 682 | 조정자, 그대로 |
| 845·851·914 | 결과 소비 `guard.IsLive` |
| 887·892·900·905 | 결과 소비 `IsCurrent(connection, token)` |
| 956 | 조정자, 그대로 |
| 966 | 조정자, `IsLive(connection) && confirm()` |
| 1100~1148 | 입력 분배(CS3 표) |

7. 공개 계약과 진행 호스트 계약은 그대로다.
8. Factory·입력 Factory 서명·`CombatSceneInput` 생성자는 그대로다.
9. 옮긴 예외 문구는 글자 그대로다. 앞머리 `[CombatViewSystem]`도 그대로다(P3).

## 5. 바뀌는 것 (보이는 동작 없음)

- 같은 연결 항이 더해지는 자리:
  - 결과 소비 851(`_playback.Begin` 뒤)과 `IsCurrent(token)` 넷
  - 입력 분배 1100·1115
  - 앞의 둘은 재생 시작 안에서 분리 뒤 새 연결이 생길 수 없고, 토큰이 맞으면 같은 연결이다(SV2). 뒤의 둘은 그 앞에 외부 호출이 없다. 그래서 값이 같다.
- 정리 신호 자리(1126)에서 두 연결이 모두 null이면 지금은 null 참조 예외, 새 판은 멈춤이다. 수신 창구가 연결 있을 때만 넘겨 닿지 않는다(CS3).
- `TickEnd`·행동 버튼·환경 호버는 인자로 한 번 잡은 연결을 쓴다. 단계 중간 분리 때 정지된 묶음을 보지만 하는 일이 없어 화면 차이는 없다고 본다(CS3 "재확인을 더하지 않는 곳").
- `CanReceiveDeckInput`이 입력 분배의 `CanReceiveUiInput`으로 옮긴다. 식은 같다.
- `BindServices(18 인자)` 대신 `Bind(CombatViewBindings)`다. 순서·예외는 같다(CS5).
- 호출자 없는 입구 셋이 사라진다.
- 바인딩이 실패한 뒤의 필드 상태(계획 TP-6): 지금은 검사 도중 일부 서비스 필드가 새 값이 되고, 새 판은 묶음을 검사가 모두 끝난 뒤 한 번에 넣는다. 바인딩이 실패하면 사실 읽기가 비어 조정자가 쓰이지 않으므로 닿지 않는 차이다.
- 바뀌는 파일:
  - 새 파일 다섯: `ICombatConnectionGuard`·`CombatSceneInputDriver`·`CombatResultConsumer`·`CombatViewBindings`·`CombatActionButtonState`. `.meta`는 Unity가 만든다.
  - 고치는 파일 다섯: `CombatViewSystem`·`CombatSceneInput`·`CombatInputFactsReader`·`CombatDeploymentConfirmation`·`CombatEndTurnConfirmation`
  - `CombatInputConnection`·`CombatInputSurfaces`·입력 Factory는 고치지 않는다.

## 6. Phase(안)

Phase마다 멈추고 보고한다(Q-CS5). 묶음 끝에 검토 에이전트 하나를 돌린다.

| Phase | 내용 | 파일 | Unity 확인 |
|---|---|---|---|
| CS-a | CS2 부품 숨기기 | 새 `CombatActionButtonState`, `CombatSceneInput`·`CombatViewSystem`·`CombatInputFactsReader`·확인창 둘 | 빈 상태 유닛 클릭 → 정보창, 공격 모드·경로·Drag 중 클릭은 정보창 안 열림, 공격 버튼 강조, 배치 진입·턴 종료 확인창, 되돌리기 버튼, 행동 가능 표시, 호버 범위 |
| CS-b | CS1 계약 + CS3 입력 분배(Layer 맞춤 포함) | 새 `ICombatConnectionGuard`·`CombatSceneInputDriver`, `CombatViewSystem` | 키·버튼·포인터 입력 전부, 카메라 이동·줌, 환경 설명, 창 포커스 잃음, 확인창 열린 동안 막힘, 확인창 수락 직후 버튼, 배치 → 전투 Layer 바뀜 |
| CS-c | CS4 결과 소비 | 새 `CombatResultConsumer`, `CombatViewSystem` | 결과 재생·거절 안내, 공격 뒤 공격 모드 자동 해제, 이동 뒤 복귀 견적, 턴 종료 대기 해제, 승패 뒤 입력 닫힘 |
| CS-d | CS5 서비스 묶음 + CS6 걷기 | 새 `CombatViewBindings`, `CombatViewSystem` | 부트 → 전투 진입, 선택 유닛 준비·진입 연출, 씬 이탈·재진입, 오류 정지(F1) |
| CS-e | 문서·공유·묶음 끝 검토 | 폴더 배치 안내·공유 메모·평가 아티팩트 적용 기록 | — |

## 7. 7기준 점검 (설계안)

- **SSOT**
  - 재확인 규칙은 계약 하나에 있다(CS1).
  - 정보창·가용성의 "입력 소유자 활성" 판정은 묶음 안 `IsInputOwnerActive` 하나다.
  - UI 입력 수신 조건과 Layer 맞춤 식은 입력 분배 하나, 받는 서비스 목록은 서비스 묶음 하나다.
- **SOLID**
  - SRP: 조정자는 수명·순서, 입력 분배는 "입력이 오면·Tick 끝 표면", 결과 소비는 "결과 하나의 후처리"를 맡는다. 입력 묶음은 입력 부품의 수명과 입구, 서비스 묶음은 "무엇을 받나"를 맡는다.
  - OCP: 새 입력 요청은 입력 분배만, 새 후처리 단계는 결과 소비만 고친다.
  - DIP: 옮긴 두 클래스는 조정자가 아니라 좁은 계약에 기댄다.
- **FW 계약**
  - 모듈 등록(FW-001/002)은 그대로 조정자 하나다. 새로 내보내는 서비스는 없다.
  - `PostBindService`는 읽기만 한다.
- **독립성**: 입력 쪽 변경과 결과 쪽 변경이 다른 파일에서 일어난다. 다른 작업자 줄은 확인창 입구와 표면 맞춤 몇 줄이다(§10).
- **불필요·우회**
  - 호출자 없는 입구 셋과 null 갈래 넷을 걷는다.
  - 새 계약 하나가 늘지만, 옮긴 클래스에 조정자 전체를 넘기지 않으려는 것이다.
  - 입력 분배와 결과 소비는 단순히 넘기기만 하는 층이 아니다. 단계와 재확인을 실제로 갖는다.
- **C#·Unity**: 새 대리자는 서비스를 받을 때 한 번씩만 만든다. 매 호출 할당은 늘지 않는다. 사실 읽기 횟수는 그대로다(P4).
- **매 프레임 If·Tick**: Tick 경로의 분기 수는 같다. 반환값 검사 하나가 재확인 셋을 대신해 호출 자리에서 보이는 줄이 준다.

예상 점수(같은 감점표, 다 풀린 중간만 뺀 추정):

- C2-02가 풀리면 SOLID 60 → 67.5다. 요청 7기준 75.0 → 76.1.
- C8-01이 풀리면 캡슐화 82.5 → 90이다. 12기준 81.25 → 82.5.
- C8-02는 낮음이고 캡슐화 낮음이 이미 상한이라 점수는 그대로다.
- 위 숫자는 두 문제를 "다 풀림"으로 센 값이다. §2 "이번에 남는 것" 둘 때문에 "일부"로 세면 점수는 그대로(75.0·81.25)다. 판정은 묶음 끝 검토에서 한다.

## 8. 정할 것

**결정(2026-11-05 사용자 "권장대로"): Q-CS1~Q-CS8 모두 A.** 아래는 결정 전 질문 기록이다.

- **Q-CS1 새 두 객체(입력 분배·결과 소비)의 수명**
  - A 서비스 수명이고 연결을 인자로 받는다(권장). 연결마다 만들지 않고, 연결 상태 생성자·첫 그리기 순서를 건드리지 않는다. 필드에 연결을 두지 않아 지금과 같은 "인자 연결 = 지금 연결" 확인을 그대로 쓴다.
  - B 연결 수명이고 연결 상태가 갖는다(`CreateInput`에서 만듦). 인자는 줄지만, 첫 그리기 전과 정리 순서에 새 객체가 끼고 연결 상태가 커진다.
- **Q-CS2 재확인 규칙의 자리**
  - A 조정자가 구현하는 좁은 계약이다(권장, CS1). 수명 판정이 조정자 한 곳에 남고 `TakeDisplay`의 마지막 문장 규칙도 그대로다.
  - B 연결 상태가 끝남 깃발과 판정 대리자를 갖는다. 연결이 스스로 답하지만, Detach의 깃발 세우기 자리와 생성자 인자가 늘어난다.
- **Q-CS3 결과 소비 분리**
  - A 분리한다(권장, CS4).
  - B 조정자에 남기고 CS2 입구만 부른다. 이 경우 C2-02는 "일부"로 남는다.
- **Q-CS4 서비스 묶음·호출자 없는 입구(CS5·CS6)**
  - A 이번에 함께 한다(권장). `BindServices`의 18개 인자와 null 갈래가 없어진다.
  - B 뒤로 미룬다. C2-02의 "재료 조립"과 C8-02 일부가 남는다.
- **Q-CS5 진행 방식**
  - A Phase마다 멈추고 Unity에서 확인한다(권장, CLAUDE.md 방식).
  - B CS-a~CS-d를 한 번에 하고 한 번 확인한다.
- **Q-CS6 착수 시점**
  - A 0단계 확인 뒤에 시작한다(권장). 0단계는 지금 원격 판의 첫 컴파일·재생이다. 아직 한 번도 컴파일하지 않은 판 위에 큰 구조 변경을 얹으면 오류 원인을 가리기 어렵다.
  - B 바로 시작한다.
- **Q-CS7 루트에 새 파일을 둘지**(검토 반영). 폴더 배치 안내는 루트에 새 파일을 두지 않고 예외는 조정자의 씬·연결 수명 객체뿐이며, 그 밖은 "먼저 묻는다"고 한다(안내 §0·§1 5번). 입력 분배·행동 버튼 값은 안내대로 `Input/`·`Input/ActionButtons/`라 묻지 않는다.
  - A 계약·결과 소비·서비스 묶음 셋을 루트에 둔다(권장, 루트 14 → 17). 셋 다 조정자 자신의 수명·순서 조각이다. 결과 소비는 같은 루트의 재생 수명 `CombatResultPlayback` 옆이 자연스럽다.
  - B 계약만 루트에 둔다. 결과 소비는 `Playback/Support/`, 서비스 묶음은 `Input/`(서비스 수명 입력 재료 옆)에 둔다. 루트는 15가 되지만, 결과 소비는 사건 재생 순서가 아니라 결과 하나의 확인 순서라 `Playback/`의 뜻과 조금 어긋나고, 서비스 묶음은 표시 서비스도 담는다.
- **Q-CS8 `57af3244` 줄을 누구에게 알릴지**(검토 반영). 이 커밋은 제목이 "[천명]"이고 Claude 공동 작성인데, 같은 내용이 CLAUDE.md 11-03에는 우리 쪽 사용자 요청으로 적혀 있다.
  - A blame대로 천명님께 공유 메모를 쓴다(권장). 메모는 비용이 작고, 우리 줄이었다 해도 해가 없다.
  - B 우리 줄로 보고 메모를 쓰지 않는다.

## 9. 설계에서 정한 것 (확인 대상)

- 새 파일 자리는 Q-CS7이 정한다. 입력 분배는 `V/Input/`, 행동 버튼 값은 `V/Input/ActionButtons/`다. 네임스페이스는 폴더 경로다. 구현 때 폴더 배치 안내에 줄을 더한다.
- **P2** Factory는 바꾸지 않는다. 새 객체는 조정자가 `Bind`에서 만든다. 입력 연결·막힘 보고와 같은 방식이다. 민형님 공유는 없다.
- **P3** 옮긴 예외 문구는 앞머리 `[CombatViewSystem]`까지 그대로다. 로그에서 찾는 이름을 같게 두려는 것이다.
- **P4** 사실 읽기 횟수와 순서는 바꾸지 않는다. 예를 들어 환경 호버는 지금 사실을 두 번 읽는다(`ReadFacts` + `CanReceiveDeckInput`). 하나로 줄이는 것은 관찰로만 남긴다.
- **P5** `CombatSceneInputFactory`와 `CombatSceneInput` 생성자는 그대로다.
- **P6** 입력 묶음은 행동 버튼에 넘길 값 넷을 읽기 전용 값 `CombatActionButtonState`로 돌려준다. 표면을 모르게 하려는 것이다. `CombatInputSurfaces.Sync`의 서명과 값 평가 순서는 그대로다.
- **P7** 결과 소비가 구간 표시 `Combat.Result.Finalize`를 가져간다.
- **P8** `IsCurrent(token)` 식에 같은 연결 항이 더해진다. 값은 같다(§5).
- **P9** Layer 맞춤은 입력 분배의 `SyncInputLayer()` 한 곳에 둔다. 입력 연결은 적용만 한다는 지금 설명을 그대로 지키려는 것이다.
- **P10** 서비스 묶음은 지금 `PostBindService`의 조회·예외만 옮기고, 검사·비우기는 `Bind`가 지금 `BindServices` 순서대로 한다(CS5).

## 10. 공유 (구현 뒤, 미전달 예정)

- **천명님**(Q-CS8 A일 때)
  - `CombatSceneInput`의 확인창 입구 속성 `Deployment`·`EndTurn`이 private이 된다. 밖에서는 `RequestEnterCombat`·`RequestEndTurn`·`IsConfirmationOpen`·`CloseConfirmations`·`SyncConfirmations`로 쓴다.
  - 조정자의 턴 종료 갈래(1128-1132)와 `SyncConfirmations` 자리(1175)가 입력 분배로 옮긴다.
  - `CountIdleUnits`가 private이 된다.
  - 입력 재료 `afterConfirmedSubmit`(719)가 람다가 된다(CS-0에서 더 찾음).
- **현강님**: `ClearActionAvailability`(116)가 부품 필드를 부르게 바뀐다. 한 줄이고 동작은 같다. 조정자 251 표시 값 읽기 생성이 묶음 값을 읽게 바뀐다(CS-0에서 더 찾음).
- **호진님**
  - 조정자의 `Surfaces.Sync` 두 줄(1182-1183)이 입력 분배로 옮긴다. 넘기는 값은 입력 묶음의 `ReadActionButtons`에서 오고, 인자 순서는 같다.
  - `HasUnplacedUnits`가 private이 된다.
- 민형님·Core 공유는 없다.

## 11. 위험

- 재확인 한 줄을 빠뜨리는 것: 줄마다 대응 표(§4.6, CS3 표)로 구현과 묶음 끝 검토가 대조한다.
- 다른 세션이 같은 파일을 고치고 있으면 충돌이 난다. 착수 전에 `git status`와 원격을 확인한다.
- 아직 컴파일하지 않은 판이다(Q-CS6).

## 12. 독립 검토 기록 (2026-11-05)

새 에이전트 하나가 이 초안과 코드(HEAD `dfb49b34`)를 대조했다. 높음은 없었다. 아래처럼 반영했다.

| 지적 | 등급 | 반영 |
|---|---|---|
| CS1 본문은 687을 `IsLive`로, §4 표는 `IsConnected`로 적어 서로 다름. `IsLive`면 입력 생성 안 오류 보고 때 첫 그리기 성공 표시·Layer 맞춤을 건너뜀 | 중간 | `IsConnected`로 통일, 이유를 CS1에 적음 |
| 서비스 묶음이 조회·검사를 한꺼번에 하면 재바인딩 실패 때 옛 사실 읽기·재료를 다시 쓰지 않는 규칙과 예외 순서가 바뀜 | 중간 | CS5·P10: 묶음은 조회·문구 있는 예외 넷만, 검사·비우기는 `Bind`가 지금 순서대로 |
| 루트에 새 파일 셋은 폴더 배치 안내("먼저 묻는다")와 맞지 않음 | 중간 | P1을 Q-CS7로 바꿈 |
| 입력 연결에 `SyncLayer`를 두면 "목표 Layer는 정책, 입력 연결은 적용만" 설명이 바뀜 | 낮음~중간 | Layer 맞춤을 입력 분배로(P9) |
| Tick 끝·표면 맞춤은 지금도 재확인이 없는데, 인자 연결로 바꾸면 단계 중간 분리 때 정지된 묶음을 봄 | 낮음 | 재확인을 더하지 않음과 근거를 CS3·§5에 적음, CS-b Unity 확인에 확인창 수락 직후 버튼 |
| `IsConnected`의 null 검사는 "같다"가 아니라 닿지 않는 차이 | 낮음 | CS3·§5 문구 고침 |
| 서비스 묶음의 예외 형식·문구(`InvalidOperationException` 넷, `items` 무검사) | 낮음 | CS5에 그대로 둔다고 적음 |
| C2-02 재료 묶기·C8-01 `connection.Selection`이 남음 | 낮음 | §2 "이번에 남는 것", §7 점수 판정은 묶음 끝 검토 |
| 입력 묶음이 표면을 인자로 받으면 UI 표면에 묶임 | 낮음 | 값 넷을 돌려주는 `CombatActionButtonState`(P6) |
| `57af3244`·병합 커밋 작성자 판단은 질문으로 | 낮음 | Q-CS8, `0a6fe9c4` using 줄은 고치지 않아 표에서 뺌 |
| 조정자 안 private 도우미 필요, 입력 분배 `TickInteraction` 이름 겹침, §1.2 수 | 낮음 | CS1 도우미, `TickInputs`로, §1.2 수 고침 |

검토자가 맞다고 확인한 것:

- §1.3의 줄 번호, `BindServices` 인자 18·필드 30, 호출자 없는 입구 셋, `Assets/Tests` 무참조(문자열 포함)
- §1.5 blame
- Factory `:68`·`:93`과 `CreateFactsReader` 서명이 그대로임
- 재확인 식 대응(845·851·914, `IsCurrent`에 같은 연결 항, 1100·1115, 1126)
- R2 보존(수신 창구는 오류 상태면 모두 막아 R-B는 정상 분리·종료 정지에서만 쓰임)
- 생성 순서에 순환이 없음

## 13. 구현 메모 (2026-11-05)

실행 기록은 [계획](../plans/2026-11-05-combat-view-coordinator-split-plan.md) §11이다. 사용자 지시로 CS-c·CS-d는 Ub를 기다리지 않고 이어 했다("CS-d 까지 진행 해"). Unity 확인 Ua~Ud는 모두 문제 없음(사용자)이다.

### 13.1 크기

| | HEAD `dfb49b34` | 지금 | 설계 추정 |
|---|---|---|---|
| 조정자 줄 | 1186 | 941 | 약 870 |
| 조정자 필드(정적 구간 표시 제외) | 29 | 21 | 약 22 |
| 조정자의 `ReferenceEquals(_connection` | 16 | 3(도우미 둘 + 514 표시 일치) | — |
| 조정자의 `CanContinue()` | 28 | 11 | — |

- 새 파일: 입력 분배 189줄, 결과 소비 160줄, 서비스 묶음 164줄, 재확인 계약 32줄, 행동 버튼 값 35줄. 줄이 설계 추정보다 덜 준 것은 남은 연결 수명과 그 주석 때문이다.
- Factory 변경 없음, 고칠 시험 없음(설계 P2 그대로).

### 13.2 설계·계획과 다른 점

- `TickCore`의 지역 이름은 계획의 `connection`이 아니라 `inputConnection` 그대로다. 같은 메서드의 진입 연출 블록에 `connection`이 있어 CS0136 때문이다(계획 §11.3).
- §5는 `CombatInputConnection`을 고치지 않는다고 적었으나 주석 셋을 고쳤다(옮긴 메서드·`Bind`·목표 Layer 주인을 가리키던 문장). 동작은 같다. `CombatSceneInputSources`도 주석 둘을 고쳤다.
- 결과 후처리 끝의 `connection = _connection` 다시 읽기를 없앴다. 앞 셋(Buffer 쓰기·재생 상태)이 분리를 일으키지 않아 같은 연결이고, 분리가 일어나도 바로 뒤 `IsLive`가 멈춘다(계획 §11.4).
- 재바인딩 실패 뒤 입력을 쓰면 옛 판은 `InvalidOperationException`, 새 판은 null 참조 예외다(계획 §11.3·TP-6). 운영은 `PostBindService`를 한 번만 부르고 실패하면 부트가 멈춰 닿지 않는다.

### 13.3 묶음 끝 검토 (Te.4, 새 에이전트 하나)

- 컴파일 오류 없음(정적). 높음·중간 0.
- §4 "지킬 것" 줄마다, 재확인 자리 1100·1107·1115·1120·1126·1133·1136·1139·1148(입력 분배)·845·851·914·`IsCurrent` 넷(결과 소비)·360·966·570·687(조정자)을 대조해 모두 같다고 봤다.
- R2: Detach 순서(`StopInput` → `Release` → `_connection = null`)가 그대로라, 슬롯 해제 때 오는 `SelectCanceled`가 `IsConnected` 갈래로 기존 소유자에 닿는다. 오류 정지 때는 수신 창구가 모두 막는다.
- 결과 후처리 여섯 단계 순서와 단계 사이 토큰 확인이 같다.
- 람다 셋(TP-1·TP-2)은 부를 때 필드를 읽어 옛 입력 분배를 붙잡지 않는다.
- 다른 작업자 줄은 계획 §11.1~§11.5 목록에서 빠진 것이 없다.
- 판정: **C2-02 다 풀림**(결과 소비·입력 분배·표면 맞춤·서비스 조회가 나가고 조정자에는 수명·Tick 순서·진행 호스트·수명 판정만 남음. `CreateInput` 재료 묶기는 조립 자리의 일이라 지적과 다름), **C8-01 다 풀림**(부품 여섯 private, 밖은 묶음 입구만, `connection.Selection`은 묶음 부품이 아닌 좁은 읽기 계약). 평가 아티팩트 '적용 기록' 7차에 해결로 셌다: 요청 7기준 75.0 → 76.1, 12기준 81.25 → 82.5.
- 낮음 다섯
  - L1 낡은 주석: 이번에 바꾼 파일(조정자·연결 입력 묶음·행동 버튼 값·입력 연결·입력 분배)의 것은 계획 TP-9대로 CS-e에서 고쳤다(우리 줄, 주석만). 안 바꾼 파일의 주석 일곱(`CombatInputReceiver:13`·`CombatInputSurfaces:14`·`CombatInteraction:358·366`·`CombatDeploymentEntry:35`·`CombatEndTurnEntry:32`·`CombatViewConnection:26`)은 보고한 뒤 사용자 지시로 고쳤다.
  - L2 조정자 `ReadFacts`의 `followUp` 인자를 부르는 곳이 조정자 안에 없다.
  - L3 재바인딩 실패 뒤 상태(§13.2 넷째). 그대로 둔다.
  - L4 결과 소비의 private `Finalize(…)`가 `object.Finalize`(소멸자)와 이름이 같다(경고 없음).
  - L5 조정자에 쓰지 않는 using 약 10개(17줄은 경계 커밋 `^83fb2767` 줄).
  - 사용자 "권장대로 진행하고 푸쉬해"로 L1 나머지·L2·L4·L5를 반영했다(계획 §11.7, 주석·이름·인자·using, 동작 같음, 컴파일 미확인). L3은 그대로다.

### 13.4 남은 것

- 입력 재료 16개 묶기(`CreateInput`)는 조정자에 남는다(§2). 재료 중 다섯이 조정자의 판정 대리자라 옮겨도 넘기는 것이 같다.
- 재확인은 여전히 외부 호출 자리마다 계약 이름 한 줄로 단다. 재진입(분리) 모델 자체의 성질이다.
- C8-02 나머지(`FieldUnitView`·`UnitClipChannel`·`CombatPhaseTransitionSettings.From`)는 다른 파일이라 남은 낮음으로 둔다.
- 공유 메모 셋(천명님 특성 공유 §8·현강님 §24·호진님 §18)은 미전달이다. 민형님은 Factory 변경이 없어 메모가 없다.

