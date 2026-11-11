# 전투 View 조정자 나누기 Task 계획 (CS-0~CS-e)

- 날짜: 2026-11-05
- 상태: **CS-0~CS-e 모두 완료(2026-11-05).** Unity 컴파일·재생 문제 없음(사용자 "Ub, Uc, Ud 문제 없음, CS-e 진행해", Ua도 문제 없음), 새 시험 없음, 시험 변경 없음. 묶음 끝 검토 컴파일 오류 없음·높음·중간 0, C2-02·C8-01 다 풀림(§11.6, 평가 아티팩트 7차). 낮음 넷(L1 나머지·L2·L4·L5)은 사용자 "권장대로 진행하고 푸쉬해"로 반영했고(§11.7, 컴파일 미확인), 사용자 지시로 Claude가 커밋·push했다. 남은 것은 공유 메모 셋 전달(사용자)이다. CS-c·CS-d는 사용자 지시로 Ub를 기다리지 않고 이어 했다(Phase마다 멈추는 규칙의 예외).
- 설계: [coordinator-split-design](../specs/2026-11-05-combat-view-coordinator-split-design.md)(CS1~CS6). 줄 번호는 HEAD `dfb49b34` 기준이다.
- 결정(2026-11-05 사용자 "권장대로 하고 Task 계획 써줘"): Q-CS1~Q-CS8 모두 A, 설계 P2~P10 그대로.
  - Q-CS1 A: 입력 분배·결과 소비는 서비스 수명이고 연결을 인자로 받는다.
  - Q-CS2 A: 재확인 규칙은 조정자가 구현하는 좁은 계약 `ICombatConnectionGuard`에 둔다.
  - Q-CS3 A: 결과 소비를 나눈다.
  - Q-CS4 A: 서비스 묶음과 호출자 없는 입구 걷기를 이번에 함께 한다.
  - Q-CS5 A: Phase마다 멈추고 Unity에서 확인한다.
  - Q-CS6 A: 0단계 확인 뒤에 착수한다.
  - Q-CS7 A: 계약·결과 소비·서비스 묶음은 루트에 둔다(루트 14 → 17).
  - Q-CS8 A: `57af3244` 줄은 천명님께 공유 메모를 쓴다.
- 대상 문제: C2-02(중간, SOLID) 2단계, C8-01(중간, 캡슐화) 2단계, C8-02(낮음) 일부.
- 규칙:
  - View 작업이라 새 시험을 쓰지 않고 MSBuild도 돌리지 않는다. 보고에는 "컴파일 미확인"을 적는다.
  - 동작은 같아야 한다. 설계 §4(지킬 것)와 §5(바뀌는 것)를 벗어나면 구현하지 않고 먼저 묻는다.
  - Phase마다 멈추고 보고한다. 묶음 끝(CS-e)에 검토 에이전트 하나를 돌린다.
  - 커밋·push는 사용자가 한다.

## 0. 계획에서 정한 것 (사용자 확인 대상)

설계에 없던 세부를 계획에서 정했다. 다르게 하고 싶은 것이 있으면 착수 전에 알려 주면 된다.

| 번호 | 정한 것 | 이유 |
|---|---|---|
| TP-1 | 입력 재료의 `canReceiveUiInput`·`afterConfirmedSubmit`는 람다로 넘긴다. `() => _inputDriver.CanReceiveUiInput()`, `() => _inputDriver.SyncActionButtons(_connection)` | 재료는 첫 입력 생성 때 한 번 만들어 들고 있다. 메서드 그룹으로 넘기면 그때의 입력 분배 객체에 묶인다. 람다는 부를 때 필드를 읽어 지금과 같다(지금도 조정자 메서드가 부를 때 `_connection`을 읽음). 재바인딩 때 `_inputSources = null`도 그대로 둔다. |
| TP-2 | 입력 연결에 넘기는 분배 대리자는 `(r, x, y) => _inputDriver.Route(_connection, r, x, y)`이다. 바인딩 때 한 번 만든다 | 입력 연결이 입력 분배보다 먼저 생긴다. 람다라 순서 문제가 없다. |
| TP-3 | 옛 `BindServices`(internal, 인자 18)는 없애고 private `Bind(CombatViewBindings)` 하나로 둔다 | 호출자가 `PostBindService` 하나뿐이다(시험은 이미 지워짐). |
| TP-4 | 조정자는 `ICombatConnectionGuard`를 명시 구현하고, 같은 이름의 private 도우미 `IsLive(connection)`·`IsConnected(connection)`를 둔다 | 조정자 안의 재확인 자리도 같은 이름을 부르게 하려는 것이다(설계 CS1). |
| TP-5 | 행동 버튼 값 `CombatActionButtonState`는 readonly struct다. 생성자 인자 순서가 지금 평가 순서(공격 진입 가능 → 공격 중 → 되돌리기 가능 → 가용성)와 같다 | 매 Tick·입력마다 만들므로 할당을 늘리지 않으려는 것이다. 순서는 `MovementUndo.CanRequest`가 사실을 다시 읽어서 지킨다. |
| TP-6 | 조정자는 서비스 하나하나 대신 묶음 `_bindings` 하나를 갖는다. `_bindings`는 `Bind`의 검사가 모두 끝난 뒤 한 번에 넣는다 | 설계 CS5의 필드 줄이기(30 → 약 22)다. 지금은 검사 도중 일부 필드가 새 값이 되지만, 바인딩이 실패하면 사실 읽기가 비어 조정자가 쓰이지 않는다. 그래서 그 차이는 닿지 않는다(설계 §5에 더함). |
| TP-7 | `_confirmationPresentation`(확인창 계약)은 묶음에 넣지 않고, `Bind`가 형식 검사 뒤 파생 필드로 둔다 | 검사 자리·예외 문구를 `Bind`에 그대로 두려는 것이다(P10). |
| TP-8 | 다른 internal 입구(`FocusLost`·`AttachCreated`의 선택 인자·`root == null` 갈래)는 이번에 손대지 않는다 | 설계 CS6 범위 밖이다. |
| TP-9 | Phase마다 그 Phase가 바꾼 클래스의 클래스 설명(summary·remarks)을 같이 고친다 | 설명이 옮긴 책임을 계속 가리키지 않게 하려는 것이다. |
| TP-10 | CS-b에서 결과 소비 코드(아직 조정자 안)의 재확인도 `IsLive`·`IsConnected`로 먼저 바꾼다 | 그러면 CS-c는 순수한 옮기기가 된다. |

## 1. CS-0 착수 전 확인

- [x] **0단계 결과를 받는다(Q-CS6 A).** — 문제 없음(2026-11-05 사용자). 지금 원격 판의 첫 Unity 컴파일·재생이다.
- [x] `git status`와 `git fetch origin feature/Yonggeun/Refector-Stage`로 다른 세션·원격 변경을 본다. 고칠 파일 일곱(§9)이 바뀌었으면 줄 번호를 다시 잡는다.
- [x] 고칠 줄의 작성자를 `git blame`으로 다시 센다(설계 §1.5). 바뀌었으면 §8 공유 목록을 고친다.
- [x] `Assets/Tests`에서 바꿀 멤버 이름을 grep한다(`CombatViewSystem`·`CombatSceneInput`·`CombatInputFactsReader`·`HasUnplacedUnits`·`CountIdleUnits`·`BindServices`). 설계 때는 0건이었다. 생기면 멈추고 묻는다.

## 2. CS-a 입력 묶음 부품 숨기기 (설계 CS2)

화면 변화가 없다. 부품을 직접 부르던 자리를 묶음 입구로 바꾼다.

- [x] **Ta.1 새 `Input/ActionButtons/CombatActionButtonState.cs`**
  - internal readonly struct. 읽기 속성 넷: `CanEnterAttack`·`IsAttackActive`·`CanUseReturn`·`Availability`(`CombatActionAvailability`, null 허용).
  - 생성자 인자 순서는 TP-5대로다.
  - `.meta`는 Unity가 만든다.
- [x] **Ta.2 `Input/CombatSceneInput.cs`**
  - 부품 속성 여섯(68·71·80·83·111·113)을 private readonly 필드로 바꾼다: `_gate`·`_interaction`·`_deployment`·`_endTurn`·`_actionAvailability`·`_movementUndo`. 생성자 대입 줄은 이름만 바꾼다.
  - 안에서 부품을 쓰는 곳을 새 필드 이름으로 바꾼다. 자리는 77·86·95·97·106·107·116·142·158·182·192·195·204·209·212·219·228·240·242다.
  - 새 입구를 더한다(설계 CS2 표).
    - `ResolveCommand(CombatResult result) => _gate.Resolve(result)`
    - `HasPendingCommand => _gate.HasPending`
    - `RequestEnterCombat() => _deployment.Request()`
    - `RequestEndTurn() => _endTurn.Request()`
    - `SyncAvailability(in CombatInputFacts facts, IReadOnlyList<CharacterData> characters, int answeredPreview) => _actionAvailability.Sync(facts, characters, _interaction.IsInputOwnerActive, answeredPreview)`
    - `ReadActionButtons(in CombatInputFacts facts) => new CombatActionButtonState(_interaction.CanEnterAttack(facts), _interaction.IsAttackActive, _movementUndo.CanRequest, _actionAvailability.Read(facts))`
    - `IsAttackActive`·`IsItemDragActive`·`HasCurrentAcceptedEmptyMovementRange(EntityId)`·`ResolvePointerTarget(InputLayer)`: `_interaction`에 그대로 넘긴다.
  - 클래스 remarks: `<see cref="Deployment"/>`·`<see cref="EndTurn"/>`·`<see cref="Interaction"/>`을 지금 뜻에 맞는 말로 바꾼다. "행동 버튼·가용성 맞춤과 입력 사실 읽기는 아직 Interaction을 직접 읽습니다" 문장을 지운다.
- [x] **Ta.3 `CombatViewSystem.cs`** (바꿀 줄 여섯)
  - 896 `_connection.Input.Gate.Resolve(result)` → `_connection.Input.ResolveCommand(result)`
  - 1061 `_connection.Input.Gate.HasPending` → `_connection.Input.HasPendingCommand`
  - 1106 `connection.Input.Deployment.Request()` → `connection.Input.RequestEnterCombat()`
  - 1130 `connection.Input.EndTurn.Request()` → `connection.Input.RequestEndTurn()`
  - 1150-1151 → `connection.Input.SyncAvailability(ReadFacts(includeUnhandledCommand: true), _characters.Characters, _state.AnsweredPreviewNumber)`
    - `IsInputOwnerActive`는 묶음 안에서 `AnsweredPreviewNumber` 다음에 읽힌다. 둘 다 순수 읽기라 결과가 같다(설계 §12 검토자 확인).
  - 1182-1183 → `CombatActionButtonState buttons = connection.Input.ReadActionButtons(facts);` 다음 줄 `connection.Surfaces.Sync(facts, buttons.CanEnterAttack, buttons.IsAttackActive, buttons.CanUseReturn, buttons.Availability);`
- [x] **Ta.4 `Input/CombatInputFactsReader.cs`** 71·73·77·84
  - `input.Interaction.IsAttackActive` → `input.IsAttackActive`
  - `input.Interaction.HasCurrentAcceptedEmptyMovementRange(selectedActor)` → `input.HasCurrentAcceptedEmptyMovementRange(selectedActor)`
  - `input.Interaction.IsItemDragActive` → `input.IsItemDragActive`
  - `input.Interaction.ResolvePointerTarget(layer)` → `input.ResolvePointerTarget(layer)`
- [x] **Ta.5 확인창 둘**
  - `Input/ActionButtons/CombatDeploymentConfirmation.cs:42` `internal bool HasUnplacedUnits()` → private
  - `Input/ActionButtons/CombatEndTurnConfirmation.cs:47` `internal int CountIdleUnits()` → private
  - 둘 다 자기 클래스 안에서만 쓴다(92·68).
- [x] **Ta.6 확인 grep**
  - `\.Input\.(Gate|Interaction|Deployment|EndTurn|ActionAvailability|MovementUndo)\b`와 `input\.Interaction\.`가 0건이어야 한다.
  - `CombatSceneInput.cs`에 `internal .*(Gate|Interaction|Deployment|EndTurn|ActionAvailability|MovementUndo) \{` 속성이 남지 않아야 한다.
- [x] **Ta.7 보고** 바꾼 파일, 다른 작업자 줄, "컴파일 미확인", Unity 확인 Ua(§7)를 보고하고 멈춘다.

## 3. CS-b 재확인 계약 + 입력 분배 (설계 CS1·CS3)

- [x] **Tb.1 새 `V/ICombatConnectionGuard.cs`**
  - 네임스페이스는 `...ViewSystem.Combat`이고 internal interface다.
  - 멤버 셋: `bool IsLive(CombatViewConnection connection)`, `bool IsConnected(CombatViewConnection connection)`, `bool IsInputReady { get; }`.
  - 문서 주석에 R-A·R-B·R-D 뜻과 R2(입력 해제 중 정리 신호)를 적는다(설계 §1.2).
- [x] **Tb.2 조정자가 계약을 구현한다**
  - 클래스 선언에 `ICombatConnectionGuard`를 더한다.
  - private 도우미를 둔다.
    - `IsLive(c) => CanContinue() && ReferenceEquals(_connection, c)`
    - `IsConnected(c) => c != null && ReferenceEquals(_connection, c)`
  - 명시 구현 셋은 도우미와 `IsInputReady`를 부른다.
- [x] **Tb.3 새 `V/Input/CombatSceneInputDriver.cs`** (internal sealed)
  - 생성자: `(ICombatConnectionGuard guard, CombatInputFactsReader facts, CombatInputConnection inputConnection, ICombatStateBufferReadSink state, ICharacterBufferReadSink characters)`. 모두 null이면 `ArgumentNullException`.
  - 응답 단계 사실 공급자 `_readFollowUpFacts = () => _facts.Read(followUp: true)`를 생성자에서 한 번 만든다.
  - 메서드와 옮겨 오는 곳(본문은 그대로 옮기고 이름만 바꾼다).

    | 새 메서드 | 옮겨 오는 곳 | 바뀌는 것 |
    |---|---|---|
    | `Route(CombatViewConnection connection, Request request, float x, float y)` | `RouteInput` 1094-1144 | 첫 줄 `connection = _connection`이 인자가 된다. 재확인은 설계 CS3 표대로 |
    | `SyncAvailability(CombatViewConnection connection)` | `SyncActionAvailability` 1146-1152 | 첫 줄이 `!_guard.IsLive(connection) \|\| connection.Input == null`이 된다 |
    | `SyncEnvironmentHover(CombatViewConnection connection)` | 1155-1163 | `_connection?.Input` → `connection?.Input`, `CanReceiveDeckInput()` → `CanReceiveUiInput()` |
    | `SyncActionButtons(CombatViewConnection connection)` | 1170-1184 | `_connection` → 인자. 끝은 CS-a의 `ReadActionButtons` 두 줄이다 |
    | `bool TickInputs(CombatViewConnection connection, bool previewAnswered)` | `TickCore` 403-416 | 재확인 셋이 `return false`가 되고, 끝에 `return _guard.IsLive(connection)`. 응답 인자 평가 순서는 그대로(사실 → 캐릭터 → 최신 미리보기 → 응답 여부 → 응답 공급자) |
    | `TickEnd(CombatViewConnection connection, float deltaTime)` | `Tick` 315-319 | 버튼 → 환경 호버 → `connection.Input?.TickCamera(_facts.Read(), deltaTime, _characters.Characters)`. 단계 사이 재확인은 더하지 않는다(설계 CS3 "재확인을 더하지 않는 곳") |
    | `bool CanReceiveUiInput()` | `CanReceiveDeckInput` 1027 | `_guard.IsInputReady && !CombatInputPolicy.IsInputBlocked(_facts.Read())` |
    | `SyncInputLayer()` | `SyncInputLayer` 929-931 | `_inputConnection.Apply(CombatInputPolicy.GetDesiredLayer(_facts.Read()))` |

- [x] **Tb.4 조정자 연결**
  - `BindServices`:
    - 맨 앞에서 비우는 줄에 `_inputDriver = null`을 더한다.
    - 입력 연결 생성 인자 `RouteInput` → TP-2의 람다.
    - 사실 읽기 생성 다음에 `_inputDriver = new CombatSceneInputDriver(this, _inputFactsReader, _inputConnection, _state, _characters)`를 둔다.
  - `Tick` 끝 314-320 → `if (CanContinue() && _state.HasBattle) _inputDriver.TickEnd(_connection, deltaTime);`
  - `TickCore` 403-416을 다음으로 바꾼다.
    - `CombatViewConnection connection = _connection;`
    - `if (!_inputDriver.TickInputs(connection, signal.PreviewAnswered)) return;`
    - 418-419의 `ConsumeResult(signal.UnpresentedResult)`는 CS-b에서 그대로 둔다.
  - `SyncInputLayer()` 본문 → `_inputDriver.SyncInputLayer();`
  - `CreateInput`의 재료 두 칸은 TP-1의 람다로 바꾼다.
  - `FinalizeResult` 913의 `SyncActionAvailability(connection)` → `_inputDriver.SyncAvailability(connection)`
  - 지운다: `RouteInput`·`SyncActionAvailability`·`SyncEnvironmentHover`·`SyncActionButtons`·`CanReceiveDeckInput`·필드 `_readFollowUpFacts`와 생성자 대입(138-158 안).
  - 조정자에 남는 재확인을 이름으로 바꾼다(TP-10, 설계 §4 표).
    - 360·845·914 `!CanContinue() || !ReferenceEquals(…)` → `!IsLive(connection)`
    - 851 `!CanContinue()`(`_playback.Begin` 뒤) → `!IsLive(connection)`
    - 966 `CanContinue() && ReferenceEquals(_connection, connection)` → `IsLive(connection)`
    - 570·687 `ReferenceEquals(_connection, connection)` → `IsConnected(connection)`
    - 514(`_connection.Display`와 비교)·293·313·325·368·375·399·682·956은 그대로다.
  - 클래스 설명(41-68)에서 입력 분배·표면 맞춤을 "맡는 것"에서 "맡기는 것"으로 옮긴다(TP-9).
- [x] **Tb.5 확인 grep**
  - `CombatViewSystem.cs`에 `RouteInput`·`SyncActionAvailability`·`SyncActionButtons()`·`CanReceiveDeckInput`·`_readFollowUpFacts`가 0건이어야 한다.
  - `ReferenceEquals(_connection`은 도우미 둘에만 남아야 한다.
- [x] **Tb.6 보고** 설계 CS3 표의 줄마다 대응을 적고 멈춘다. Unity 확인은 Ub(§7)다.

## 4. CS-c 결과 소비 (설계 CS4)

- [x] **Tc.1 새 `V/CombatResultConsumer.cs`** (internal sealed)
  - 생성자: `(ICombatConnectionGuard guard, RuntimeFaultBoundary faults, CombatResultPlayback playback, ICombatStateBufferReadSink state, CombatDisplayValueReader displayValues, ICombatViewBufferWriteSink inputWrite, ICharacterBufferReadSink characters, CombatInputFactsReader facts, CombatSceneInputDriver driver)`. 모두 null이면 `ArgumentNullException`.
  - 구간 표시 `private static readonly ProfilerMarker FinalizeMarker = new("Combat.Result.Finalize")`를 옮긴다(P7).
  - `Consume(CombatViewConnection connection, CombatResult result)`: `ConsumeResult` 839-876을 옮긴다. `_connection` → 인자다.
  - private `Finalize(CombatViewConnection connection, CombatResult result, CombatPlaybackToken token)`: `FinalizeResult` 878-919를 옮긴다.
    - `IsCurrent(token)` 넷 → `IsCurrent(connection, token)`
    - `SyncInputLayer()` → `_driver.SyncInputLayer()`
    - `ReadFacts(followUp: true)` → `_facts.Read(followUp: true)`
    - 908의 `connection = _connection` 다시 읽기는 인자 그대로 쓴다. 905의 토큰 확인 뒤 Buffer 쓰기 둘·`CompleteFinalization`은 분리를 일으키지 않아 같은 연결이다.
  - private `IsCurrent(CombatViewConnection connection, CombatPlaybackToken token) => _guard.IsLive(connection) && _playback.IsCurrent(token)`
  - 예외 문구 앞머리는 `[CombatViewSystem]` 그대로다(P3).
- [x] **Tc.2 조정자 연결**
  - `BindServices`:
    - 비우는 줄에 `_resultConsumer = null`을 더한다.
    - 입력 분배 생성 다음에 `_resultConsumer = new CombatResultConsumer(this, _faults, _playback, _state, _displayValues, _inputWrite, _characters, _inputFactsReader, _inputDriver)`를 둔다.
  - `TickCore` 418-419 → `_resultConsumer.Consume(connection, signal.UnpresentedResult);`
  - 지운다: `ConsumeResult`·`FinalizeResult`·`FinalizeMarker`·`IsCurrent(CombatPlaybackToken)`·`#region 결과 소비`. `using Unity.Profiling`이 더 쓰이지 않으면 지운다.
  - 클래스 설명에서 결과 후처리를 "맡기는 것"으로 옮긴다.
- [x] **Tc.3 확인 grep**
  - `CombatViewSystem.cs`에 `ConsumeResult`·`FinalizeResult`·`IsCurrent(token)`·`ProfilerMarker`가 0건이어야 한다.
  - 결과 소비에 `_connection`이 0건이어야 한다.
- [x] **Tc.4 보고** 결과 후처리 단계 여섯(설계 CS4)과 재확인 자리를 대조해 적고 멈춘다. Unity 확인은 Uc(§7)다.

## 5. CS-d 서비스 묶음 + 호출자 없는 입구 (설계 CS5·CS6)

- [x] **Td.1 새 `V/CombatViewBindings.cs`** (internal sealed)
  - 읽기 속성 열여덟을 둔다: `State`·`Results`·`Characters`·`Current`·`Currency`·`InputRead`·`InputWrite`·`InputSlot`·`InputLayers`·`LoadingRegister`·`Entry`·`DisplayServices`·`TextPresentation`·`Items`·`TutorialState`·`TutorialRequests`·`Loading`·`RunDeck`.
  - 생성자는 private이고 받은 값을 담기만 한다.
  - `static CombatViewBindings From(ISystemServiceStoreReader resolver)`
    - 지금 `PostBindService` 186-208의 조회를 옮긴다.
    - 인자 평가 순서를 그대로 지킨다. 특히 `CombatDisplayServices.From(global)`은 Tutorial 검사보다 앞이다.
    - 문구 있는 `InvalidOperationException` 넷(Tutorial 상태 읽기·Tutorial 요청 쓰기·로딩 상태·확정 덱 읽기)은 글자 그대로 둔다.
    - 그 밖의 검사는 하지 않는다(P10).
- [x] **Td.2 조정자**
  - `PostBindService`: resolver null 검사 다음에 `Bind(CombatViewBindings.From(resolver));`
  - `BindServices` → private `Bind(CombatViewBindings bindings)`(TP-3). 몸체 순서는 지금과 같다.
    1. 연결 중이면 거절한다.
    2. 사실 읽기·재료·확인창·입력 분배·결과 소비를 비운다.
    3. 막힘 보고를 만든다(지금 240 자리). 지금 238의 `_loading` 대입은 없어진다(필드를 지움).
    4. `ArgumentNullException(nameof(…))` 검사를 지금 순서·인자 이름대로 한다(`state`·`results`·`characters`·`current`·`currency`·`inputRead`·`inputWrite`·`slot`·`layers`·`loadingRegister`·`displayServices`·`textPresentation`·`entry`). 이름은 `nameof(bindings.State)` 꼴이 아니라 지금 문자열(`"state"` 등)을 그대로 쓴다.
    5. 표시 값 읽기·준비 대상 읽기를 지금 자리에서 만든다. 준비 대상 읽기는 늘 만든다(`runDeck` 필수).
    6. 확인창 형식 검사를 하고 `_confirmationPresentation`을 둔다(TP-7).
    7. `_bindings = bindings`(TP-6).
    8. 입력 연결 → 사실 읽기 → 입력 분배 → 결과 소비를 만든다.
  - 서비스 필드 열 개를 지우고 `_bindings.X`로 읽는다(TP-6): `_state`·`_results`·`_characters`·`_inputRead`·`_inputWrite`·`_loadingRegister`·`_loading`·`_displayServices`·`_textPresentation`·`_entry`.
  - "서비스 받기 전" 검사를 `_bindings == null`로 바꾼다.
    - `Initialize` 280(`_loadingRegister == null`)
    - `Attach` 497(`_displayServices == null || _state == null`)
    - `TakeDisplay` 616(`_state == null`)
    - `RequireActiveBattle` 737(`_entry == null`)
    - `IsSceneActive` 1012(`_entry != null`)
  - null 갈래 넷을 지운다(설계 §1.4).
    - `Initialize` 287 `if (_unitPreparationReader != null)`
    - `Attach` 500-501
    - `TickCore` 354-355
    - `TakeDisplay` 623-624
    - `IsEntryPresentationPending` 1023 `_loading?.IsLoading ?? false` → `_bindings.Loading.IsLoading`
  - 호출자 없는 입구를 걷는다: `Input`(1067)·`RequestReturn()`(1073)·`AttachDisplay()`(597-604)를 지우고, `HasPendingPresentation`(1052)은 private으로 내린다.
  - 클래스 설명과 지운 멤버를 가리키던 주석을 고친다(CS-0 grep, §11.1).
    - `BindServices`: 조정자 188·211, `Input/CombatSceneInputSources.cs:16`, `Input/CombatInputConnection.cs:15`
    - `AttachDisplay`: 조정자 1044
    - `CombatViewSystem.Input`: `Input/CombatInputConnection.cs:50`
- [x] **Td.3 확인 grep**
  - `CombatViewSystem.cs`에 `BindServices`·`AttachDisplay`·`RequestReturn`이 0건이어야 한다. 다른 클래스의 같은 이름(`TutorialViewSystem.BindServices`·수신 창구와 슬롯의 `RequestReturn` 등)은 그대로다.
  - 위 주석 넷에 옛 이름이 남지 않아야 한다.
  - `CombatViewSystem.cs`에 지운 필드 이름 열 개가 0건이어야 한다.
- [x] **Td.4 보고** 필드 수, 줄 수, "컴파일 미확인"을 적고 멈춘다. Unity 확인은 Ud(§7)다.

## 6. CS-e 문서·공유·묶음 끝 검토

- [x] **Te.1 폴더 배치 안내** `docs/superpowers/viewDocs/combat-view-folder-guide.md`
  - §1.1 루트 줄(83)에 `ICombatConnectionGuard`·`CombatResultConsumer`·`CombatViewBindings`를 더한다(루트 14 → 17, Q-CS7 A).
  - `Input/` 줄(84)에 `CombatSceneInputDriver`를 더한다.
  - `ActionButtons/` 줄에 `CombatActionButtonState`를 더한다.
  - §1 판단 순서 5번(75)의 "루트 14개"를 17로 고친다.
  - §2 "조정자의 수명 하나" 행(120)에 서비스 수명의 결과 소비·서비스 묶음은 루트라는 예외를 더한다. 근거는 Q-CS7 A다.
  - §2에 규칙 한 줄을 더한다: 조정자 밖 클래스의 외부 호출 뒤 재확인은 `ICombatConnectionGuard` 이름(`IsLive`·`IsConnected`)으로만 한다.
- [x] **Te.2 공유 메모(모두 미전달)**
  - 천명님 특성 공유 `handoffs/2026-10-30-combat-trait-system-view-cheon-share.md` §8(Q-CS8 A):
    - `CombatSceneInput` 확인창 입구 속성이 private이 된다.
    - 턴 종료 갈래(옛 1128-1132)와 `SyncConfirmations` 자리(옛 1175)가 입력 분배로 옮긴다.
    - `CountIdleUnits`가 private이 된다.
    - 입력 재료의 `afterConfirmedSubmit: SyncActionButtons`(조정자 719)가 람다가 된다(TP-1, CS-b).
    - 입력 재료의 `afterConfirmedSubmit` 인자 설명(`CombatSceneInputSources.cs:33`, CS-0 때 빠뜨림)이 "조정자의 입구" → "입력 분배 `SyncActionButtons`"로 바뀐다(CS-b, 주석만).
    - `57af3244` 줄의 작성자 확인도 묻는다.
  - 현강님 메모 `handoffs/2026-10-22-combat-view-structure-hyeongang-share.md` §24:
    - `CombatSceneInput.ClearActionAvailability`(116) 한 줄이 바뀐다(CS-a).
    - 조정자 251 `_displayValues = new CombatDisplayValueReader(…)`가 묶음 값을 읽게 바뀐다(CS-d, `0a6fe9c4`).
  - 호진님 메모 `handoffs/2026-10-22-combat-view-structure-hojin-share.md` §18:
    - `Surfaces.Sync` 두 줄(옛 1182-1183)이 입력 분배로 옮긴다. 값은 `ReadActionButtons`에서 온다.
    - `HasUnplacedUnits`가 private이 된다.
  - 민형님: Factory가 바뀌지 않았는지 `git diff --stat -- Assets/ProjectL/Script/Systems/Factory`로 확인한다. 0이면 메모가 없다.
- [x] **Te.2b 입력 안내** `docs/superpowers/viewDocs/stage-input-guide.md:136`의 "AttachCreated와 AttachDisplay 모두" 문장을 `AttachCreated` 하나로 고친다(CS-d가 `AttachDisplay`를 지움). 같은 문서 `:339`의 "조정자 RouteInput"을 입력 분배 `CombatSceneInputDriver.Route`로 고친다(CS-b가 옮김).
- [x] **Te.3 설계 §13 구현 메모** 계획과 다른 점, 남은 것, 줄 수·필드 수를 적는다.
- [x] **Te.4 묶음 끝 검토** 새 에이전트 하나가 HEAD와 바뀐 판을 대조한다.
  - 정적 컴파일 오류를 본다.
  - 설계 §4 대응 표의 줄마다 동작이 같은지 본다.
  - R2 경로(정상 분리·종료 정지 중 정리 신호)를 본다.
  - 결과 후처리 순서를 본다.
  - 다른 작업자 줄 목록을 본다.
  - 높음·중간은 반영하고, 낮음은 권장안과 함께 보고한다.
- [x] **Te.5 평가 아티팩트 '적용 기록' 7차**(CLAUDE.md 규칙)
  - `applied.json`에 C2-02·C8-01·C8-02 항목과 바꾼 파일·남은 단계를 더하고 페이지를 다시 만든다.
  - C2-02·C8-01을 `done`으로 셀지 `part`로 셀지는 Te.4 검토 결과로 정한다(설계 §2 "이번에 남는 것", §7).
- [x] **Te.6 문서** 후속 수정 계획 §6.6 표, CLAUDE.md 현재 상태, 이 계획 §11 실행 기록을 고친다.

## 7. Unity 확인 (사용자)

모든 Phase에 공통으로 확인한다: 컴파일 오류 없음, Combat 진입, 콘솔 예외 없음.

| Phase | 번호 | 확인 |
|---|---|---|
| CS-a | Ua-1 | 빈 상태에서 아군·적 유닛을 누르면 정보창이 열린다 |
| | Ua-2 | 공격 모드·경로 지정·배치 Drag·아이템 Drag 중에 누르면 정보창이 끼어들지 않는다 |
| | Ua-3 | 공격 버튼 강조가 공격 모드와 함께 켜지고 꺼진다. 공격할 수 없으면 버튼이 막힌다 |
| | Ua-4 | 배치 진입 확인창(덜 놓았을 때)·턴 종료 확인창(행동 안 한 아군이 있을 때)이 지금처럼 뜨고, 닫으면 입력이 돌아온다 |
| | Ua-5 | 이동 뒤 되돌리기 버튼이 켜지고 누르면 되돌아간다 |
| | Ua-6 | 선택 유닛의 행동 가능 표시와 유닛 호버 범위가 지금과 같다 |
| | Ua-7 | 결과를 보내면 시작·턴 종료 대기가 풀린다(턴 종료 → 적 턴 → 내 턴) |
| CS-b | Ub-1 | 키(E·턴 종료·되돌리기)와 화면 버튼 입력이 모두 지금처럼 동작한다 |
| | Ub-2 | 카메라 이동(WASD·화살표·가장자리)·휠 줌이 되고, 카메라를 옮긴 뒤 커서 아래 칸이 바로 바뀐다 |
| | Ub-3 | 환경 칸(지형·오브젝트) 호버 설명이 뜨고 사라진다 |
| | Ub-4 | 경로 Drag 중 창 포커스를 잃으면 Drag가 정리된다 |
| | Ub-5 | 확인창이 열린 동안 칸 입력이 막히고, 확인창 수락 직후 행동 버튼이 바로 맞춰진다 |
| | Ub-6 | 배치 → 전투 진입 때 입력 Layer가 바뀌고, 승패 결과 뒤 입력이 닫힌다 |
| CS-c | Uc-1 | 공격·이동 결과가 재생되고 끝난 뒤 다음 입력을 받는다 |
| | Uc-2 | 거절되는 명령(예: 막힌 칸 공격)에서 거절 안내가 뜨고 입력이 돌아온다 |
| | Uc-3 | 공격 뒤 공격 모드가 자동으로 풀린다 |
| | Uc-4 | 이동 뒤 되돌리기 견적·버튼이 맞다 |
| | Uc-5 | 턴 종료 → 적 턴 자동 진행 → 내 턴 복귀가 막히지 않는다 |
| | Uc-6 | 승리·패배 결과 화면까지 간다 |
| CS-d | Ud-1 | Boot → 로비·StageSelect → 전투 진입, Play From Boot 개발 진입 둘 다 된다 |
| | Ud-2 | 선택 유닛 준비·보드 진입 연출이 끝난 뒤 입력이 열린다 |
| | Ud-3 | 전투에서 나가 StageSelect로 갔다가 다시 전투에 들어간다 |
| | Ud-4 | 포기(Option) → 결과 화면 → 로비 복귀가 된다 |

## 8. 공유 (구현 뒤, 미전달 예정)

공유 대상과 메모 자리는 §6 Te.2와 같다. 착수 전 blame 결과(CS-0)로 고친다.

## 9. 바뀌는 파일과 커밋 묶음 (사용자)

| Phase | 새 파일(+ `.meta`, Unity가 만듦) | 고치는 파일 |
|---|---|---|
| CS-a | `Input/ActionButtons/CombatActionButtonState.cs` | `Input/CombatSceneInput.cs`·`CombatViewSystem.cs`·`Input/CombatInputFactsReader.cs`·`Input/ActionButtons/CombatDeploymentConfirmation.cs`·`Input/ActionButtons/CombatEndTurnConfirmation.cs` |
| CS-b | `ICombatConnectionGuard.cs`·`Input/CombatSceneInputDriver.cs` | `CombatViewSystem.cs` |
| CS-c | `CombatResultConsumer.cs` | `CombatViewSystem.cs` |
| CS-d | `CombatViewBindings.cs` | `CombatViewSystem.cs`, 주석만 `Input/CombatSceneInputSources.cs`·`Input/CombatInputConnection.cs` |
| CS-e | — | 문서(폴더 안내·공유 셋·설계·이 계획·후속 계획·CLAUDE.md) |

- 경로는 `ViewSystem/Combat/` 기준이다. Factory 파일은 고치지 않는다.
- Phase마다 한 커밋으로 나눠도 되고 묶음 하나로 해도 된다. 새 `.cs`는 Unity가 만든 `.meta`와 같은 커밋에 넣는다.

## 10. 위험과 되돌리기

- 재확인 한 줄을 빠뜨리는 것: Phase마다 Tx 확인 grep과 설계 §4 표로 대조한다. 묶음 끝 검토(Te.4)가 다시 대조한다.
- 같은 파일을 다른 세션이 고치는 것: CS-0에서 본다. 겹치면 멈추고 묻는다.
- Phase를 되돌릴 때: 그 Phase의 새 파일과 `CombatViewSystem.cs` 변경을 함께 되돌린다. CS-c·CS-d는 CS-b의 입력 분배에 기대므로 CS-b만 되돌릴 수는 없다.

## 11. 실행 기록

### 11.1 CS-0 착수 전 확인 (2026-11-05)

사용자 "그대로, CS-0 진행해" = TP-1~TP-10 그대로. 코드 변경은 없다.

- 0단계(지금 원격 판의 첫 Unity 컴파일·재생): **문제 없음(2026-11-05 사용자 "0단계 문제 없음, CS-a 진행해").** 그래서 CS-a를 시작했다(Q-CS6 A).
- 원격
  - `origin/feature/Yonggeun/Refector-Stage`는 HEAD `dfb49b34`와 같다(앞섬·뒤처짐 0).
  - `origin/develop`은 HEAD보다 13커밋 앞서 있다(호진님 로비 검수·챕터 2, 민형님 디졸브·사망 처리·용암·넥서스·메테오).
  - develop이 바꾼 전투 View 파일은 일곱이다: `Display/Unit/`의 `EntityCartoonAppearance`(새)·`FieldUnitPresenter`·`FieldUnitView`·`ICombatUnitPose`·`UnitModelAppearance`, `Presentation/Reaction/CombatUnitReactionPresenter`, Factory `LobbySceneFactory`.
  - 이 계획이 고칠 파일 일곱과는 겹치지 않는다. develop 병합은 이 작업과 따로 정한다.
- 작업 트리: 문서 넷만 바뀌어 있다(설계·이 계획·CLAUDE.md·후속 계획). 코드 변경은 없다.
- blame(HEAD `dfb49b34`, 파일별 다른 작업자 줄)

  | 파일 | 다른 작업자 줄 | 이 계획에서 바뀌나 |
  |---|---|---|
  | `CombatViewSystem.cs` | 천명님 14(`57af3244`: 719 `afterConfirmedSubmit`, 760·769·801·939 확인창 닫기, 1030-1033 `IsModalOpen`, 1128-1132 턴 종료 갈래, 1175 `SyncConfirmations`) | 719(CS-b 람다), 1128-1132·1175(CS-b 옮김). 나머지는 그대로 |
  | | 호진님 4(17·18 using, 1182-1183 `Surfaces.Sync`) | 1182-1183(CS-a 바꿈, CS-b 옮김) |
  | | 현강님 1(251 표시 값 읽기 생성) | 251(CS-d) |
  | `CombatSceneInput.cs` | 천명님 32(확인창 입구 등), 현강님 3(5 using·115-116) | 확인창 속성·116(CS-a). using은 그대로 |
  | `CombatEndTurnConfirmation.cs` | 천명님 96 | `CountIdleUnits`(CS-a) |
  | `CombatDeploymentConfirmation.cs` | 호진님(Leehojin) 111 | `HasUnplacedUnits`(CS-a) |
  | `CombatInputFactsReader.cs`·`CombatInputConnection.cs`·`CombatSceneInputSources.cs` | 없음(우리 줄). **CS-b 때 바로잡음:** `CombatSceneInputSources.cs:33`(인자 설명 주석)은 천명님 `57af3244` 줄이다 | `:33` 주석(CS-b) |

  - 설계 §1.5에서 빠졌던 것 둘을 더 찾았다: 천명님 719(입력 재료 `afterConfirmedSubmit`)와 현강님 251(표시 값 읽기 생성). 공유 목록(§6 Te.2)에 더했다.
  - 처음 계획에 적었던 주석 파일 둘(`CombatSceneInputFactory`·`CombatDisplayValueReader`)에는 `BindServices` 언급이 없었다. 실제 자리(`CombatSceneInputSources.cs:16`·`CombatInputConnection.cs:15`·`:50`, 조정자 1044)로 Td.2·§9를 고쳤다. 그래서 Factory 파일은 이 계획에서 고치지 않는다.
- 시험: `Assets/Tests`에서 `CombatViewSystem`·`CombatSceneInput`·`CombatInputFactsReader`·`HasUnplacedUnits`·`CountIdleUnits`·`BindServices`를 grep했다. 0건이라 고칠 시험이 없다.
- 문서: `viewDocs/stage-input-guide.md:136`이 `AttachDisplay`를 말해 CS-e에 Te.2b를 더했다. 10-14 작업표 둘은 옛 기록이라 고치지 않는다.

### 11.2 CS-a 입력 묶음 부품 숨기기 (2026-11-05)

사용자 "0단계 문제 없음, CS-a 진행해". 컴파일 미확인(MSBuild를 돌리지 않음), 새 시험 없음, 시험 변경 없음. 화면 변화는 없다.

- 착수 전: 원격 `origin/feature/Yonggeun/Refector-Stage`는 HEAD `dfb49b34`와 같았고, 고칠 파일에 다른 세션 변경이 없었다. 줄 번호는 계획 그대로였다.
- Ta.1 새 `Input/ActionButtons/CombatActionButtonState.cs`(internal readonly struct, 속성 넷, 생성자 순서 = TP-5). `.meta`는 Unity가 만든다.
- Ta.2 `Input/CombatSceneInput.cs`
  - 부품 속성 여섯을 private readonly 필드 `_gate`·`_interaction`·`_deployment`·`_endTurn`·`_actionAvailability`·`_movementUndo`로 바꿨다. 속성 설명은 필드 주석으로 옮겼다.
  - 새 입구: `HasPendingCommand`·`ResolveCommand`·`IsAttackActive`·`IsItemDragActive`·`HasCurrentAcceptedEmptyMovementRange`·`ResolvePointerTarget`·`RequestEnterCombat`·`RequestEndTurn`·`SyncAvailability`·`ReadActionButtons`. 모두 부품에 그대로 넘긴다.
  - using 둘을 더했다(`GlobalSystem.InputSystem.Data`의 `InputLayer`, `Contracts.Ids`의 `EntityId`). 두 이름공간에는 이 파일이 쓰는 이름과 겹치는 형식이 없다.
  - 클래스 remarks의 `<see cref="Deployment"/>`·`<see cref="EndTurn"/>`·`<see cref="Interaction"/>`을 새 입구로 바꾸고, "아직 Interaction을 직접 읽는다" 문장을 "부품은 밖에 내지 않는다"로 바꿨다(TP-9).
- Ta.3 `CombatViewSystem.cs` 여섯 자리(896·1061·1106·1130·1150-1151·1182-1183)를 계획대로 바꿨다. 클래스 설명은 묶음이 가진 것을 나열할 뿐이라 이번에 고칠 문장이 없다.
- Ta.4 `Input/CombatInputFactsReader.cs` 넷(71·73·77·84)을 묶음 입구로 바꿨다.
- Ta.5 `CombatDeploymentConfirmation.HasUnplacedUnits`·`CombatEndTurnConfirmation.CountIdleUnits`를 private으로 바꿨다. 다른 파일에서 부르는 곳은 없었다.
- Ta.6 확인 grep
  - `\.Input\.(Gate|Interaction|Deployment|EndTurn|ActionAvailability|MovementUndo)\b`(using·namespace 줄 제외): 0건.
  - `input\.Interaction\.`: 전투 View는 0건이다. 남은 둘(`LobbyViewScope.cs:86`·`StageMapViewScope.cs:275`의 `_input.Interaction`)은 다른 형식이라 이 작업과 무관하다.
  - `CombatSceneInput.cs`의 부품 속성: 0건.
  - 남은 cref(`CombatCommandGate.HasPending`·`CombatInteraction.IsInputOwnerActive`·`CombatInteraction.ResolvePointerTarget`)는 판정 원본을 가리키므로 그대로 둔다.
- 동작 대조
  - 가용성 맞춤: 옛 인자 평가 순서는 사실 읽기 → 캐릭터 → `IsInputOwnerActive` → `AnsweredPreviewNumber`였다. 이제 `IsInputOwnerActive`가 묶음 안에서 마지막에 읽힌다. 둘 다 상태를 바꾸지 않는 읽기라 결과가 같다(설계 §12).
  - 행동 버튼: 공격 진입 가능 → 공격 중 → 되돌리기 가능 → 가용성 순서가 그대로다. 되돌리기 가능이 사실을 다시 읽는 것도 같다.
  - 나머지는 이름만 바꿨다.
- 다른 작업자 줄(HEAD `dfb49b34` blame, 바뀐 줄만)
  - 천명님 `57af3244`: `CombatSceneInput.cs` 9줄(28 remarks, 57 생성자 대입, 82-83 `EndTurn` 속성, 86 `IsConfirmationOpen`, 95·97 `CloseConfirmations`, 106-107 `SyncConfirmations`. 86 이후는 이름만 바뀜), `CombatEndTurnConfirmation.cs:47`(`CountIdleUnits` 접근자), `CombatViewSystem.cs:1130`(`RequestEndTurn`).
  - 현강님 `e2cb9912`: `CombatSceneInput.cs:116`(`ClearActionAvailability`, 이름만).
  - 호진님: `CombatViewSystem.cs:1182-1183`(`8339e308`, CCRC_Hojin, 행동 버튼 표면 맞춤), `CombatDeploymentConfirmation.cs:42`(경계 커밋 `^83fb2767`, Leehojin — 작성자 확인은 Q-N11).
  - 모두 계획 §11.1 공유 목록 안에 있다. 공유 메모는 CS-e에서 쓴다.
- Unity 확인: Ua-1~Ua-7(§7). 새 파일 `CombatActionButtonState.cs`는 Unity가 만든 `.meta`와 같은 커밋에 넣는다.

### 11.3 CS-b 재확인 계약 + 입력 분배 (2026-11-05)

사용자 "Ua 문제 없음, CS-b 진행해". 컴파일 미확인(MSBuild를 돌리지 않음), 새 시험 없음, 시험 변경 없음. 화면 변화는 없어야 한다.

- 착수 전: 원격 `origin/feature/Yonggeun/Refector-Stage`는 HEAD `dfb49b34`와 같았다. 작업 트리는 CS-a 변경과 문서 넷 그대로였다.
- Tb.1 새 `V/ICombatConnectionGuard.cs`(internal interface, `IsLive`·`IsConnected`·`IsInputReady`). 문서 주석에 R-A·R-B·R-D와 R2를 적었다. `.meta`는 Unity가 만든다.
- Tb.2 조정자가 계약을 명시 구현한다. private 도우미 `IsLive(c) => CanContinue() && ReferenceEquals(_connection, c)`·`IsConnected(c) => c != null && ReferenceEquals(_connection, c)`를 `IsCurrent` 뒤에 두고, 명시 구현 셋은 도우미와 `IsInputReady`를 부른다.
- Tb.3 새 `V/Input/CombatSceneInputDriver.cs`(internal sealed, 생성 인자 다섯·null 검사, 응답 단계 사실 공급자를 생성자에서 한 번).
  - `Route`·`SyncAvailability`·`SyncEnvironmentHover`·`SyncActionButtons`·`TickInputs`·`TickEnd`·`CanReceiveUiInput`·`SyncInputLayer`. 본문은 옮겼고, `ReadFacts(…)` → `_facts.Read(…)`, 조정자의 재확인 → 계약 호출만 바뀌었다.
  - `Route`의 재확인은 설계 CS3 표대로다(카메라 1100·배치 진입 뒤 1107·선택 누름 1115 → `IsLive`, 정보창 뒤 1120 → `!IsLive || !IsInputReady`, 정리 신호 1126 → `!IsConnected || (!IsLive && !cleanup)`, 1133·1136·1139 → `IsLive`, 가용성 1148 → `!IsLive || Input == null`).
  - `TickEnd`의 세 단계 사이에는 재확인을 더하지 않았다(설계 CS3 "재확인을 더하지 않는 곳").
- Tb.4 조정자 연결
  - `BindServices`: 맨 앞에서 `_inputDriver = null`, 입력 연결의 분배 인자 = `(request, screenX, screenY) => _inputDriver.Route(_connection, request, screenX, screenY)`(TP-2), 사실 읽기 다음에 `_inputDriver = new CombatSceneInputDriver(this, …)`.
  - `Tick` 끝 = `_inputDriver.TickEnd(_connection, deltaTime)`. `TickCore` 입력 단계 = `if (!_inputDriver.TickInputs(inputConnection, signal.PreviewAnswered)) return;`.
    - 계획은 지역 이름을 `connection`으로 적었으나 `inputConnection`을 그대로 썼다. 같은 메서드 안 진입 연출 블록에 `connection` 지역이 있어 C# 규칙상 바깥에 같은 이름을 둘 수 없다(CS0136). 동작은 같다.
  - `SyncInputLayer()` = `_inputDriver.SyncInputLayer()`, 입력 재료 두 칸 = 람다(TP-1), `FinalizeResult`의 가용성 = `_inputDriver.SyncAvailability(connection)`.
  - 지움: `RouteInput`·`SyncActionAvailability`·`SyncEnvironmentHover`·`SyncActionButtons`·`CanReceiveDeckInput`·`_readFollowUpFacts`(필드·생성자 대입).
  - 재확인 이름 바꿈(TP-10): 진입 연출 360·결과 소비 845·`_playback.Begin` 뒤 851·`FinalizeResult` 914·결과 화면 확인 966 → `IsLive(connection)`, 루트 구독 570·첫 그리기 687 → `IsConnected(connection)`. 514·293·313·325·368·375·399·682·956은 그대로다.
  - 클래스 설명: "맡는 것"의 "목표 입력 Layer 동기화" → "목표 입력 Layer를 맞출 때", 재확인 규칙 구현을 더함. "맡기는 것"에 입력 분배 `CombatSceneInputDriver`를 더함(TP-9).
- 주석만 고친 다른 파일 셋(옮긴 메서드를 조정자 것으로 가리키던 것): `Input/CombatSceneInputSources.cs:33`(`afterConfirmedSubmit` 인자 설명, 천명님 줄)·`Input/CombatSceneInput.cs:161`(`Route` 설명)·`Input/CombatInputConnection.cs:26`(`route` 인자 설명). 입력 안내 문서 `stage-input-guide.md:339`의 "조정자 RouteInput"은 CS-e Te.2b에 더했다.
- Tb.5 확인 grep
  - `CombatViewSystem.cs`의 `RouteInput`·`SyncActionAvailability`·`SyncActionButtons()`·`CanReceiveDeckInput`·`_readFollowUpFacts`: 0건.
  - `ReferenceEquals(_connection`: 도우미 둘(1040·1046)에만 남는다. 514는 `ReferenceEquals(_connection.Display, created)`라 계획대로 그대로다.
- 동작 대조
  - 옮긴 메서드 다섯은 본문이 같다. `_facts`는 조정자의 `_inputFactsReader`와 같은 객체이고, 계약 `guard`는 조정자 자신이다.
  - 새로 연결 항이 붙은 자리(1100·1115·851)는 그 앞에 바깥 호출이 없거나(1100·1115), Detach가 연결을 비우기 전에 재생을 먼저 끊어(851, SV2) 값이 같다.
  - 1126은 연결이 있으면 옛 식과 같다. 다른 점은 두 연결이 모두 null일 때 하나다. 옛 식은 진행해 null 참조 예외가 나고 새 식은 멈춘다. 수신 창구가 씬 연결 활성(연결 있음 포함)일 때만 넘기므로 닿지 않는다(설계 §5).
  - `Tick` 끝은 옛 판이 단계마다 `_connection`을 다시 읽고 새 판은 한 번 잡은 연결을 쓴다. 단계 사이에 분리가 일어나면 정지된 묶음을 보지만, 정지된 묶음의 환경 호버·카메라는 null이고 표면은 분리가 이미 숨겼다(설계 CS3). 카메라 인자의 사실 읽기가 한 번 더 평가될 수 있는 것만 다르다.
  - `TickInputs`의 응답 인자 평가 순서(사실 → 캐릭터 → 최신 미리보기 → 응답 여부 → 응답 공급자)는 그대로다.
  - 재바인딩이 실패한 상태에서 입력을 쓰면, 옛 판은 사실 읽기가 "서비스를 바인딩한 뒤" `InvalidOperationException`을 던졌고 새 판은 `_inputDriver`가 null이라 `NullReferenceException`이 난다. 바인딩 실패는 Framework 바인딩 단계에서 멈추므로 닿지 않는 차이로 본다(TP-6과 같은 성격, CS-d가 `Bind`로 묶을 때 다시 본다).
- 조정자 줄 수: 1186(HEAD, CS-a 뒤도 같음) → 1091. Factory 파일 변경 없음(`git diff --stat -- Assets/ProjectL/Script/Systems/Factory` 0).
- 다른 작업자 줄(HEAD `dfb49b34` blame, 바뀌거나 옮긴 줄만)
  - 천명님 `57af3244`: 조정자 719(`afterConfirmedSubmit` → 람다), 1128-1132(턴 종료 갈래 → 입력 분배 `Route`), 1175(`SyncConfirmations` → 입력 분배 `SyncActionButtons`), `CombatSceneInputSources.cs:33`(주석). 33은 CS-0 표에서 빠졌던 줄이라 Te.2에 더했다.
  - 호진님 `8339e308`(CCRC_Hojin): 조정자 1182-1183(`Surfaces.Sync` → 입력 분배 `SyncActionButtons`).
  - 그 밖의 옮긴 줄은 모두 우리 줄이다.
- Unity 확인: Ub-1~Ub-6(§7). 새 파일 둘(`ICombatConnectionGuard.cs`·`Input/CombatSceneInputDriver.cs`)은 Unity가 만든 `.meta`와 같은 커밋에 넣는다.

### 11.4 CS-c 결과 소비 (2026-11-05)

사용자 "CS-d 까지 진행 해"(Ub를 기다리지 않음). 컴파일 미확인(MSBuild를 돌리지 않음), 새 시험 없음, 시험 변경 없음. 화면 변화는 없어야 한다.

- Tc.1 새 `V/CombatResultConsumer.cs`(internal sealed, 생성 인자 아홉·null 검사, 구간 표시 `Combat.Result.Finalize` 옮김).
  - `Consume(connection, result)` = 옛 `ConsumeResult`, private `Finalize(connection, result, token)` = 옛 `FinalizeResult`. `_connection.X` → `connection.X`, `SyncInputLayer()` → `_driver.SyncInputLayer()`, `ReadFacts(followUp: true)` → `_facts.Read(followUp: true)`.
  - 재확인: 845·851·914 → `_guard.IsLive(connection)`(CS-b에서 이미 이름을 바꾼 것), `IsCurrent(token)` 넷 → `IsCurrent(connection, token) = _guard.IsLive(connection) && _playback.IsCurrent(token)`(R-C, P8).
  - 옛 908의 `connection = _connection` 다시 읽기는 없앴다. 그 앞 셋(`RemoveCommandsThrough`·`AcknowledgePresented`·`CompleteFinalization`)은 분리를 일으키지 않아 같은 연결이다(주석으로 적음).
  - 예외 문구·로그 문구 앞머리는 `[CombatViewSystem]` 그대로다(P3).
- Tc.2 조정자: 비우는 줄에 `_resultConsumer = null`, 입력 분배 다음에 결과 소비 생성, `TickCore` 끝 = `_resultConsumer.Consume(inputConnection, signal.UnpresentedResult)`. 지움: `ConsumeResult`·`FinalizeResult`·`FinalizeMarker`·`IsCurrent(CombatPlaybackToken)`(문서 주석의 `AttachDisplay` 문장 포함)·`#region 결과 소비`·`using Unity.Profiling`. 클래스 설명: "맡는 것"의 결과 소비 순서 → Tick 단계 순서, "맡기는 것"에 결과 소비(TP-9).
- Tc.3 확인 grep: `CombatViewSystem.cs`의 `ConsumeResult`·`FinalizeResult`·`IsCurrent(token)`·`ProfilerMarker`·`CombatPlaybackToken` 0건, `CombatResultConsumer.cs`의 `_connection` 0건.
- 동작 대조(결과 후처리 단계 여섯, 설계 CS4)
  1. `Display.Refresh` → `IsCurrent`: 그대로(`_connection.Display` → `connection.Display`, 들어올 때 `IsLive`가 참이라 같은 객체).
  2. `Input.OnResult` → `IsCurrent`: 그대로.
  3. `Input.ResolveCommand` → `_driver.SyncInputLayer()` → `IsCurrent`: 그대로.
  4. `Input.AfterResult(_facts.Read(followUp: true), …)` → `IsCurrent`: 그대로.
  5. `RemoveCommandsThrough` → `AcknowledgePresented` → `CompleteFinalization`: 그대로.
  6. `_driver.SyncAvailability(connection)` → `IsLive(connection)` → `Input.AfterAcknowledged`: 그대로.
  - `IsCurrent`에 연결 항이 더해졌다. 토큰이 맞으면 같은 연결이다. Detach는 연결을 비우기 전에 재생을 먼저 끊는다(SV2). 그래서 값이 같다.
  - 조정자가 넘기는 연결은 `TickInputs`가 참을 돌려준 연결이라 그때 `_connection`과 같다. 재생 중(`IsBusy`) 갈래도 들어올 때 같은 연결이다.
- 다른 작업자 줄: 없음(옮긴 줄은 모두 우리 줄).
- 조정자 줄 수: 1091 → 1002.

### 11.5 CS-d 서비스 묶음 + 호출자 없는 입구 (2026-11-05)

같은 지시로 CS-c 뒤에 이어 했다. 컴파일 미확인, 새 시험 없음, 시험 변경 없음. 화면 변화는 없어야 한다.

- Td.1 새 `V/CombatViewBindings.cs`(internal sealed, 읽기 속성 열여덟, private 생성자는 담기만).
  - `static From(resolver)`가 옛 `PostBindService`의 조회를 옮겼다. 생성자 인자 순서가 옛 `BindServices` 인자 순서와 같아 평가 순서(표시 서비스 묶음 → 텍스트 표시 → 재고 → Tutorial 둘 → 로딩 상태 → 확정 덱)가 그대로다.
  - 문구 있는 `InvalidOperationException` 넷은 글자 그대로다. 그 밖의 검사는 없다(P10). resolver null 검사는 옛 자리(조정자 `PostBindService`)에 그대로 둔다.
- Td.2 조정자
  - `PostBindService` = resolver null 검사 → `Bind(CombatViewBindings.From(resolver))`.
  - `BindServices`(internal, 인자 18) → private `Bind(CombatViewBindings bindings)`(TP-3). 순서: 연결 중 거절 → 사실 읽기·입력 분배·결과 소비·재료·확인창 비우기 → 막힘 보고 → `ArgumentNullException` 검사(인자 이름은 옛 문자열 `"state"`·`"results"`·`"characters"`·`"current"`·`"currency"`·`"inputRead"`·`"inputWrite"`·`"slot"`·`"layers"`·`"loadingRegister"`·`"displayServices"`·`"textPresentation"`·`"entry"`, 자리도 옛 순서)·준비 대상 읽기·표시 값 읽기 → 확인창 계약 검사 → `_bindings = bindings`(TP-6) → 입력 연결 → 사실 읽기 → 입력 분배 → 결과 소비.
  - 옛 `_loading = loading` 대입은 필드와 함께 없어졌다. 준비 대상 읽기는 늘 만든다(확정 덱은 묶음이 늘 채움).
  - 서비스 필드 열 개(`_state`·`_results`·`_characters`·`_inputRead`·`_inputWrite`·`_loadingRegister`·`_loading`·`_displayServices`·`_textPresentation`·`_entry`)를 지우고 `_bindings.X`로 읽는다. `_confirmationPresentation`은 파생 필드로 남긴다(TP-7).
  - "서비스 받기 전" 검사 다섯을 `_bindings == null`로: `Initialize`·`Attach`·`TakeDisplay`·`RequireActiveBattle`·`IsSceneActive`.
  - null 갈래 넷을 지움: `Initialize`의 `if (_unitPreparationReader != null)`(준비 등록은 늘 함), `Attach`의 준비 대상 읽기 없음 예외, `TickCore`의 로딩 서비스 없음 예외, `TakeDisplay`의 로딩 서비스 없음 예외. `IsEntryPresentationPending`은 `_bindings.Loading.IsLoading`.
  - 호출자 없는 입구 걷기: `Input` 속성·`RequestReturn()`·`AttachDisplay()` 지움, `HasPendingPresentation` → private.
  - 주석: 조정자 `_inputSources` 줄(`BindServices` → `Bind`), `PostBindService`·`Bind` 설명, 필드 주석 둘(표시 표·확인창), 클래스 설명 "맡기는 것"에 서비스 조회 `CombatViewBindings`. 다른 파일 셋: `Input/CombatSceneInputSources.cs:16`·`Input/CombatInputConnection.cs:15`(`BindServices` → `Bind`), `Input/CombatInputConnection.cs:50`(지운 `CombatViewSystem.Input`을 가리키던 문장 → 루트 구독 때 넘긴다는 문장).
- Td.3 확인 grep: `CombatViewSystem.cs`의 `BindServices`·`AttachDisplay`·`RequestReturn` 0건, 지운 필드 이름 열 개 0건, 주석 셋에 옛 이름 0건. 수신 창구·슬롯의 `RequestReturn`과 `ScreenTransitionSystem`의 다른 `BindServices`는 그대로다. 옛 기록 작업표 둘(`viewDocs/2026-10-14-combat-scene-display-sd-*-worksheet.md`)의 `AttachDisplay`는 기록이라 고치지 않는다. `stage-input-guide.md:136`·`:339`는 CS-e Te.2b다.
- 동작 대조
  - 운영 경로(`PostBindService`)의 조회·검사·생성 순서와 예외 문구·인자 이름은 같다.
  - 다른 점은 둘이다. 둘 다 운영에서 닿지 않는다.
    - 바인딩이 도중에 실패한 뒤의 필드 상태(TP-6, 설계 §5): 옛 판은 검사 도중 일부 서비스 필드가 새 값이 됐다. 새 판은 `_bindings`를 검사가 모두 끝난 뒤 한 번에 넣어 옛 묶음이 남는다. 실패하면 사실 읽기·입력 분배·결과 소비가 비어 조정자가 쓰이지 않는다.
    - 확정 덱·로딩 서비스가 없는 경우의 null 갈래 넷: 옛 판도 운영 입구(`PostBindService`)가 null이면 예외를 던졌고, 선택 인자로 생략하던 호출자는 없었다(설계 §1.4).
  - `IsEntryPresentationPending`·`IsSceneActive`·`IsPresenting`이 `_bindings`를 읽는 자리는 연결이 있을 때나 바인딩 뒤에만 닿는다.
- 필드: 29(HEAD, 정적 구간 표시 제외) → 21. 조정자 줄 수: 1002 → 941(HEAD 1186). 설계 추정 약 870줄·필드 약 22와 비교하면 줄은 덜 줄었다(남은 대부분은 연결 수명과 그 주석).
- 다른 작업자 줄(HEAD blame): 현강님 `0a6fe9c4` 조정자 251(`_displayValues = new CombatDisplayValueReader(…)` → 묶음 값을 읽게). 계획 §11.1 목록 그대로다. 그 밖에 바뀐 줄은 모두 우리 줄이다.
- Factory 파일 변경 없음(`git diff --stat -- Assets/ProjectL/Script/Systems/Factory` 0).
- 새 파일(Unity가 `.meta`를 만듦): `CombatResultConsumer.cs`(CS-c)·`CombatViewBindings.cs`(CS-d).
- Unity 확인: Ub-1~Ub-6·Uc-1~Uc-6·Ud-1~Ud-4(§7)를 한 번에 본다.

### 11.6 CS-e 문서·공유·묶음 끝 검토 (2026-11-05)

사용자 "Ub, Uc, Ud 문제 없음, CS-e 진행해". 코드는 주석만 고쳤다(아래 Te.4 반영). 새 시험 없음, 시험 변경 없음.

- 착수 전: 원격 `origin/feature/Yonggeun/Refector-Stage`는 HEAD `dfb49b34`와 같았다. Factory diff 0.
- 아티팩트: 사용자 요청("CS-e 전에 내가 볼 수 있게 아티팩트에 변경사항 반영해줘")으로 CS-e 전에 7차를 먼저 올렸다(세 항목 모두 '일부', 점수 그대로). Te.4 판정 뒤 C2-02·C8-01을 '해결'로 바꿨다(Te.5).
  - 페이지 만드는 스크립트 두 곳을 고쳤다. 같은 문제가 여러 차수에서 '일부'면 상태 표시가 마지막 차수를 따른다. '일부만 풀린 문제의 남은 단계' 목록은 마지막 차수 것만 보인다. 7차를 넣기 전 판으로 다시 만들면 바이트까지 같았다.
- Te.1 폴더 배치 안내: 갱신 줄, 판단 순서 5번(루트 14 → 17), §1.1 루트·`Input/`·`ActionButtons/` 줄, §2 수명 객체 행에 예외(결과 소비·서비스 묶음은 루트, Q-CS7 A, 연결은 인자로), §2 새 행(조정자 밖 재확인은 `ICombatConnectionGuard` 이름으로만).
- Te.2 공유 메모(모두 미전달): 천명님 특성 공유 §8(확인창·턴 종료 줄, `57af3244` 작성자 질문), 현강님 메모 §24(116·251), 호진님 메모 §18(행동 버튼 표면 1182-1183, `HasUnplacedUnits`). 줄은 blame으로 다시 확인했다. 민형님은 없음.
- Te.2b 입력 안내 `stage-input-guide.md`: 136(`AttachDisplay` 문장 → `AttachCreated` 하나), 339 카메라 흐름(조정자 `RouteInput` → 입력 분배 `Route`, Tick 끝 → 입력 분배 `TickEnd`).
- Te.3 설계 §13 구현 메모(크기, 다른 점, 검토, 남은 것).
- Te.4 묶음 끝 검토(새 에이전트 하나, HEAD와 작업 트리 대조, 읽기만 함)
  - 컴파일 오류 없음(정적), 높음·중간 0. 설계 §4 표·재확인 자리·R2·결과 후처리 순서·람다·재바인딩을 대조해 모두 같다고 봤다. 다른 작업자 줄 목록에서 빠진 것 없음.
  - 판정: C2-02·C8-01 다 풀림(근거는 설계 §13.3).
  - 낮음 다섯 중 L1(낡은 주석)의 이번에 바꾼 파일 몫은 TP-9대로 고쳤다. 모두 우리 줄이고 주석만이다.
    - `CombatViewSystem.cs`: 클래스 summary(결과 순서 → 수명·Tick 순서, 결과는 결과 소비), "입력 분배 `CombatInputRouter`" → "요청 해석"(입력 분배는 `CombatSceneInputDriver`만 가리키게), Buffer 쓰기 문장(삭제·확인은 결과 소비), `CreateInput`·`IsInputReady` 설명의 "Bind" → "슬롯 연결"(새 `Bind` 메서드와 헷갈림).
    - `Input/CombatSceneInput.cs`: remarks(부르는 쪽 = 입력 분배·결과 소비·사실 읽기), `SyncAvailability`·`SyncEnvironmentHover`·`HasEnvironmentHover` 설명의 "조정자" → "입력 분배".
    - `Input/ActionButtons/CombatActionButtonState.cs`: 넘기는 쪽 = 입력 분배.
    - `Input/CombatInputConnection.cs` 16: 목표 Layer를 정하는 쪽 = 입력 분배.
    - `Input/CombatSceneInputDriver.cs` 53: "슬롯 연결(Bind)" → "슬롯 연결".
  - 나머지 낮음은 고치지 않고 권장과 함께 보고했다. 사용자가 권장대로 하라고 해서 §11.7에서 반영했다(L3만 그대로).

    | 번호 | 내용 | 권장 |
    |---|---|---|
    | L1 나머지 | 안 바꾼 파일의 낡은 "조정자" 주석 일곱: `CombatInputReceiver:13`·`CombatInputSurfaces:14`·`CombatInteraction:358·366`·`CombatDeploymentEntry:35`·`CombatEndTurnEntry:32`·`CombatViewConnection:26` | 고친다(주석만). blame: `CombatEndTurnEntry:32`는 천명님 `57af3244`(공유 메모 §8에 한 줄), `CombatInputReceiver:13`은 경계 커밋 `^83fb2767`(Leehojin, 작성자 확인은 Q-N11), 나머지 다섯은 우리 줄 |
    | L2 | 조정자 `ReadFacts`의 `followUp` 인자를 부르는 곳이 조정자 안에 없다 | 인자를 걷는다(private, 동작 같음) |
    | L3 | 재바인딩 실패 뒤 상태(§11.3·TP-6과 같음) | 그대로 둔다. 고치려면 `OnCombatSceneEntered`의 `_bindings.Entry` 읽기도 함께 막아야 한다 |
    | L4 | 결과 소비의 private `Finalize(…)`가 `object.Finalize`와 이름이 같다 | 옛 이름 `FinalizeResult`로 되돌린다(private, 영향 없음) |
    | L5 | 조정자에 쓰지 않는 using 약 10개(CS-d 뒤) | 지운다. 17줄은 경계 커밋 `^83fb2767`(Leehojin) 줄이라 작성자 확인 뒤 넘긴다(Q-N11) |

- Te.5 평가 아티팩트 '적용 기록' 7차: C2-02·C8-01 해결, C8-02 일부. 요청 7기준 75.0 → 76.1, 12기준 81.25 → 82.5, 중간 해결 13 → 15(일부 9 → 7, 남음 7).
- Te.6 후속 계획 §6.6 표·§6.7 점수 줄, CLAUDE.md 현재 상태, 이 계획 상태 줄·§11.6.
- 커밋 묶음(사용자): 새 `.cs` 다섯(Unity가 만든 `.meta`와 함께), 고친 코드 일곱, 문서(설계·이 계획·CLAUDE.md·후속 계획·폴더 배치 안내·입력 안내·공유 메모 셋).

### 11.7 묶음 끝 검토 낮음 반영·커밋 (2026-11-05)

사용자 "권장대로 진행하고 푸쉬해". 컴파일 미확인(MSBuild를 돌리지 않음), 새 시험 없음, 시험 변경 없음. 동작은 같다.

- L1 나머지(주석만): `Input/CombatInputReceiver.cs` 13(Layer 변경 주인 = 입력 분배·입력 연결), `Input/ActionButtons/CombatInputSurfaces.cs` 14(먼저 부르는 쪽 = 입력 분배), `Input/Interaction/CombatInteraction.cs` 358·366(매 Tick 부르는 쪽·응답 공급자 만든 쪽 = 입력 분배), `Input/ActionButtons/CombatDeploymentEntry.cs` 35·`CombatEndTurnEntry.cs` 32(`afterSubmit` = 입력 분배 `SyncActionButtons`), `CombatViewConnection.cs` 26(입력 분배·결과 소비는 연결을 인자로 받음).
- L2: 조정자 `ReadFacts`의 `followUp` 인자를 걷었다(private, 부르는 곳 없음). 내부 재요청 판단은 입력 분배·결과 소비가 사실 읽기를 직접 부른다고 remarks에 적었다.
- L4: 결과 소비의 private `Finalize` → `FinalizeResult`(옛 조정자 이름).
- L5: 조정자의 쓰지 않는 using 16개를 지웠다(`ServiceKeys.Runtime`, Buffer 여덟, `InputSystem.Data`·`View`, `Contracts.Common`·`Ids`, `StageInput`, `Display.Hud`).
  - 컴파일을 돌리지 않아 저장소 전체 대조로 확인했다. 각 이름공간에 선언된 형식(중첩 포함)이 조정자 본문(주석·문자열 제외)에 나오지 않는다. 확장 메서드를 가진 정적 클래스도 없다. 이름공간 여럿을 담은 파일도 없다.
  - `Input.Interaction`·`Input.ActionButtons`는 본문이 쓰지 않지만 문서 주석 cref(`CombatInteraction`·`CombatInputSurfaces`)가 써서 남겼다.
- L3: 그대로 둔다.
- 다른 작업자 줄: `CombatEndTurnEntry.cs:32`는 천명님 `57af3244`(특성 공유 §8에 한 줄 더함). `CombatInputReceiver.cs:13`과 조정자 17줄 using(`InputSystem.Data`)은 경계 커밋 `^83fb2767`(Leehojin)이다. 작성자 확인 뒤 넘긴다(Q-N11, 호진님 메모 §18에 적음). 나머지는 우리 줄이다.
- `.meta`: 새 `.cs` 다섯의 `.meta`를 Unity 기본 형식(MonoImporter, 새 GUID)으로 만들어 함께 커밋했다. 4차 `CombatBgmPolicy.cs.meta`와 같은 방식이다. 로컬 Unity가 이미 다른 GUID의 `.meta`를 만들었으면, 받기 전에 그 다섯을 지우고 받으면 된다(형식 참조가 없는 일반 C# 클래스라 GUID를 쓰는 곳이 없다).

