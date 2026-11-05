# 전투 행동 버튼 설계 (공격·턴 종료·복귀, 2026-10-13)

- **목표:** 전투 단계의 키 입력 공격(E)·턴 종료(Space)·체크포인트 복귀(R)를 Combat 씬 Canvas 버튼으로도 누를 수 있게 한다. 키와 버튼은 **같은 입구·같은 판단**을 탄다.
- **상태:** A1~A5 추천안 확정(2026-10-13, 사용자). A4는 기존 복귀 버튼 검토(§7) 뒤 **A6 = 복귀 버튼도 `CombatActionPanel`로 재구성**으로 바꿨다(2026-10-13, 사용자 지시). 진행·검증 기록은 View 계획 §7.76.
- **선례:** [포기 버튼 설계](2026-10-07-combat-abandon-ui-design.md)(패널·표시 계약·컨트롤러 분리), [SOLID·SSOT 개선 설계](2026-10-09-combat-view-solid-improvement-design.md) D1(버튼 입력 규칙)·D11(입력 수신 창구)·D12(이벤트 시점 동기화).

## 1. 지금 구조

### 1.1 키 입력 경로

```text
PlayerInputActions (Combat 맵: Space=EndTurn, E=AttackMode, R=Return, Cancel, Select)
  → CombatInputController.Configure              StageInput/CombatInputController.cs
  → StageInputReceiverSlot → CombatInputReceiver  입구 검사(오류·연결·활성·포기 확인창)
  → CombatInputRouter.Route(Request)              처리 순서·대상 선택
       EndTurn    : 경로 취소 → Gate.SubmitPending(EndUnitTurn) → 성공 시 표시 정리
       AttackMode : 경로 취소 → CombatInteraction.AttackModeRequested(facts) → CanEnter면 Enter
       Return     : CombatPathInput.RequestReturn() → Core 복귀 견적 응답 뒤 Submit
```

### 1.2 이미 있는 버튼

| 버튼 | 소유 Presenter | 입구 | 누를 수 있는지 |
|---|---|---|---|
| **복귀(R)** | `CombatHud.returnButton`(슬롯 0) | `ICombatBattleDisplay.BindReturn(RequestReturn)` → `CombatViewSystem.RequestReturn()` → `_receiver.RequestReturn()` | Core 복귀 견적 `ShowReturn(preview).CanReturn` |
| 완료 확인 | `CombatHud.completionButton`(슬롯 1) | `ShowCompletion(completion, confirmed)` | 표시 한 번에 한 번 |
| 포기 | `CombatAbandonPanel`(슬롯 0~2) | `BindAbandon` → `CombatAbandonController` | `CombatInputPolicy.CanRequestAbandon`, D12 시점 `Sync()` |

**되돌리기(R, 체크포인트 복귀)는 이미 HUD 버튼이 있고 Combat.unity에 연결돼 있다**(`CombatHud.returnButton`). 키와 같은 수신 창구로 들어간다. 따라서 새로 만들 것은 **공격·턴 종료 두 개**다(복귀 버튼 위치는 A4).

### 1.3 버튼 입력 규칙(D1, `Assets/AGENTS.md`)

- 버튼 → Presenter는 `UIInputBinding.Attach(button, receiver, slot)` + `IUIInputReceiver.Click(slot)`. `Button.onClick`·Inspector UnityEvent는 쓰지 않는다.
- Presenter → 소유자는 `Action` 입구(`BindXxx`)로 전달한다. 같은 대리자는 한 번만 만든다.
- 검증은 EventSystem 핸들러 전달(`ExecuteEvents`)로 한다.

### 1.4 발견한 위험: 버튼 클릭이 필드 클릭으로도 들어간다

- `Select` 액션(마우스 왼쪽, ReleaseOnly)과 uGUI 버튼 클릭은 **같은 마우스 떼기**로 동시에 일어난다. 두 콜백의 순서는 정해져 있지 않다.
- 경로 입력은 `TryMapPointer`가 UI Raycast(`GraphicRaycaster`)에 걸리면 취소 영역으로 보므로 안전하다.
- **공격 조준은 `CombatPointerMapper.TryPickCell`이 UI 가림을 보지 않는다.** 공격 모드에서 버튼을 누르면 버튼 뒤 칸으로 공격이 기록될 수 있다. 공격 버튼 클릭이 먼저 처리되면, 같은 떼기의 `SelectReleased`가 방금 들어간 공격 모드로 가서 즉시 공격할 수도 있다.
- 지금도 공격 모드에서 HUD 복귀·포기 버튼을 누르면 같은 일이 생길 수 있다(포기는 확인창이 열린 뒤면 입구가 막지만 순서에 따라 다르다). 이번 작업에서 함께 막는다(B5).

## 2. 구조

```text
CombatActionPanel (Combat 씬 Canvas, MonoBehaviour, IUIInputReceiver)    공격·턴 종료 버튼. 받은 값만 그림
   ▲ 표시 의도: BindActions(attack, endTurn) · SetActionsAvailable(in CombatActionButtons)
CombatSceneDisplay (ICombatBattleDisplay : … ICombatActionDisplay)        패널에 전달, 해제 뒤 무시(해제(null)는 항상 전달)
   ▲
CombatViewSystem                                                          버튼 입구 = 수신 창구(키와 같은 Request)
   ├─ 입구: _receiver.AttackModeRequested() / _receiver.EndTurnRequested()
   └─ 가능 여부: CombatActionAvailability.Read(facts, interaction, gate) → 패널(값이 바뀔 때만 씀)
         EndTurn = CombatInputPolicy.CanSubmit(facts(선행 미처리 포함), gate.HasPending, EndUnitTurn)
         Attack  = CombatInteraction.CanEnterAttack(facts)   ← AttackModeRequested가 쓰는 판단 그대로
```

- **B1 표시와 판단 분리:** 패널은 버튼 보임·누름 가능·공격 모드 강조만 그린다. Policy·Buffer·Core를 모른다.
- **B2 입구는 하나(SSOT):** 버튼 클릭은 새 처리 경로를 만들지 않고 `CombatInputReceiver`의 기존 메서드를 부른다. 오류·연결·활성·포기 확인창 검사와 Router의 "경로 취소 → 제출" 순서를 키와 똑같이 탄다. 복귀 버튼이 이미 쓰는 방식이다.
- **B3 가능 조건은 기존 판단을 읽기만 한다:** 턴 종료는 Router가 제출 때 쓰는 `CanSubmit(EndUnitTurn)`과 같은 식·같은 facts(선행 미처리 명령 포함)를 쓴다. 공격은 `AttackTargetingInteraction.CanEnter`를 `CombatInteraction.CanEnterAttack`으로 읽기 전용 노출한다. 버튼 판단이 Router 판단과 갈라지지 않는다. 판단이 거짓이어도 클릭은 Router가 다시 막는다(이중 방어가 아니라 최종 판단은 Router 쪽).
- **B4 보임:** 실제 입력 Layer가 전투(`StageInputLayers.Combat`)이고 제출 정지 전일 때만 보인다. 배치 단계에서는 숨긴다(배치 단계는 손패·시작 버튼 영역). RecordInputOnly도 같은 규칙이다(키도 그 모드에서 동작한다).
- **B5 UI 위 선택 무시:** Router가 포인터 대상이 Attack일 때 `SelectPressed`·`SelectReleased` 위치가 UI(`GraphicRaycaster`) 위면 공격 소유자에게 넘기지 않는다. UI 가림 판정은 `CombatPointerMapper`의 기존 Raycast를 `HitsUi` 하나로 분리해 경로 매핑의 취소 영역과 함께 쓰고, 새 좁은 계약 `ICombatPointerOcclusion.IsPointerOverUi(x, y)`로 노출한다(할당 없음, 기존 `_uiHits` 재사용). Router는 `SetCursorSnap`처럼 Attach 수명의 `SetPointerOcclusion`으로 받는다. `ICombatPointerPicker`·`TryPickCell`은 바꾸지 않는다(배치 Drop·손패 카드 판정에 영향 없음). 경로는 이미 취소 영역 처리를 한다. 비활성 버튼도 Graphic이라 가림으로 잡힌다.
- **B6 갱신 시점(2026-10-13 원안: D12 따름, 매 Tick 아님 — 2026-10-15 SV6에서 Tick 끝 파생으로 바뀜, 아래 갱신 참조):** 한 메서드 `CombatViewSystem.SyncActionButtons()`를 아래에서 부른다.
  - `SyncInputToCore`(Layer 동기화 직후, 첫 표시 포함), 새 결과를 처음 관측할 때(`SyncSubmissionState`), 결과 후처리 끝(`FinalizeResult`: 확인 번호·Gate 해제·`AfterResult` 뒤)
  - `RouteInput`: 수신 창구를 통과한 입력 중 `PointerMoved`가 아닌 것을 Router가 처리한 직후(키·버튼 공통)
  - `CancelInputPresentation`: 포기 확인창 열기 등 입력 밖에서 공격 모드가 끝나는 경로. `SuspendSubmissions`는 이 경로로 숨김이 된다
  - 개발 Layer 패널로 Layer가 바뀐 직후(`CombatDevLayerController`의 선택 인자 `layerChanged`)
  - `StopInput`: 직접 숨김. Detach·전투 정리: 표시 `Release`·`Clear`가 숨기고 연결을 끊는다
  - **2026-10-15 갱신(구조 개선 SV6):** 위 목록 중 `SyncInputToCore`·`SyncSubmissionState`·`FinalizeResult`·`CancelInputPresentation`·개발 Layer `layerChanged` 호출을 없애고, **Tick 끝에서 한 번** 파생한다. `RouteInput` 직후 갱신과 `StopInput` 숨김은 유지하고, `SuspendSubmissions`는 진행 계약의 공개 입구라 호출 시점과 관계없이 직접 숨긴다(운영 경로의 제출 정지 Job은 Global Job Runner Tick에서 View Tick 전에 실행되므로 관측 차이는 없다). 키 입력과 행동 패널 버튼은 수신 창구 → `RouteInput`이라 즉시 갱신된다. 늦을 수 있는 것은 개발 Layer 전환과 포기 확인창 열기뿐이며 그리기 기준 0~1프레임이다. 그 사이 잘못된 기록은 Policy의 Layer 검사와 확인창 열림 중 입구 검사가 막는다. 패널은 같은 값이면 다시 쓰지 않는다.
- **B7 할당:** 입구 대리자는 생성자에서 한 번 만든다. 가능 여부는 값 구조체 하나로 넘기고 패널이 직전 값과 비교한다.

## 3. 결정 필요

| ID | 질문 | 추천 |
|---|---|---|
| A1 | 버튼을 어디에 둘까 | **새 `CombatActionPanel`**(`CombatSceneRoot`가 참조). `CombatHud`는 재화·견적·완료 표시라 행동 입력까지 넣으면 책임이 커진다. 대안: `CombatHud`에 슬롯 2개 추가 |
| A2 | 공격 모드 중 공격 버튼을 다시 누르면 | **공격 모드를 끝낸다(토글)**, 버튼은 공격 모드 동안 눌린 모습. 이를 위해 Router에 `Request.AttackToggle`을 두고 활성이면 `Interaction.Cancel` 경로, 아니면 `AttackMode` 경로를 탄다. E 키는 지금처럼 재진입을 유지한다. 대안: 키와 똑같이(다시 누르면 재진입, 취소는 Esc만) |
| A3 | 누를 수 없을 때 | **보이되 비활성(interactable=false)**. 대안: 항상 활성으로 두고 Router가 조용히 무시(갱신 코드 없음, 피드백 약함) |
| A4 | 복귀 버튼 위치 | ~~소유는 `CombatHud`에 두고 배치만 옮긴다~~ → **§7 검토 뒤 A6으로 대체: 복귀 버튼·견적 문구를 `CombatActionPanel`로 옮긴다(HUD에서 제거).** |
| A5 | 버튼에 단축키 표시 | **씬 문구로만**("공격 (E)"). 키 재지정 기능이 없으므로 코드로 읽지 않는다 |

- **A2 보충(2026-10-28, 사용자 "공격 후에는 공격모드를 자동으로 풀어줘"):** 공격 모드가 낸 공격 명령이 **수락된 결과**를 그리면 결과 후처리에서 공격 모드를 끝낸다. 공격한 유닛은 더 행동할 수 없어서(Core 행동 규칙, 천명님 `8ece0bbc`), 모드를 남기면 다른 유닛 칸 누름이 공격 대상으로 처리돼 "이미 행동" 안내가 떴다. 거절된 공격은 모드를 남겨 다시 고를 수 있다. 구현: `AttackTargetingInteraction`이 제출 번호를 기억하고 `CommandResolved(result)`(입구 `CombatSceneInput.OnResult` → `CombatInteraction.CommandResolved`)에서 수락을 표시, `AfterResult`가 모드를 끝낸다. 옛 규칙("같은 행위자·턴이면 결과 뒤에도 모드 유지, 범위 다시 요청", 전투 View 설계 §5.5)은 공격이 수락되지 않은 결과에만 남는다.

## 4. 바뀌는 파일(추천안 기준)

| 파일 | 변경 |
|---|---|
| `ViewSystem/Combat/Display/CombatActionPanel.cs`(신규) | 공격·턴 종료 버튼(슬롯 0·1), 공격 활성 강조 오브젝트(선택), `SetAvailable` 값 비교, `FindMissingReference` |
| `ViewSystem/Combat/ICombatActionDisplay.cs`(신규) | `BindActions(Action attack, Action endTurn)`, `SetActionsAvailable(in CombatActionButtons)` |
| `ViewSystem/Combat/CombatActionButtons.cs`(신규) | 값 구조체: `Visible`, `CanAttack`, `CanEndTurn`, `IsAttackActive`. `Read(facts, interaction, gate)` 정적 계산(B3) |
| `ICombatBattleDisplay.cs`·`CombatSceneDisplay.cs` | `ICombatActionDisplay` 합성·전달, Release에서 `BindActions(null, null)` |
| `CombatSceneRoot.cs` | `actionPanel` 참조·검증 |
| `CombatInteraction.cs` | `CanEnterAttack(in facts)` 읽기 전용 노출 |
| `CombatInputRouter.cs` | B5 UI 위 선택 무시, A2 채택 시 `AttackToggle` |
| `CombatInputReceiver.cs` | A2 채택 시 `AttackToggleRequested()`(버튼 전용, `IStageInputReceiver` 계약은 바꾸지 않음) |
| `ICombatPointerOcclusion.cs`(신규)·`CombatPointerMapper.cs` | `IsPointerOverUi(x, y)`, 기존 Raycast를 `HitsUi`로 분리해 `TryMapPointer`와 공유 |
| `CombatDevLayerController.cs` | 선택 인자 `layerChanged`(Layer 변경 뒤 버튼 동기화) — 2026-10-15 SV6에서 삭제(Tick 끝 파생) |
| `Display/CombatHud.cs`·`ICombatBattleDisplay.cs` | (A6) 복귀 버튼·문구·`BindReturn`·`ShowReturn` 제거. 복귀 견적은 `CombatSceneDisplay.SetReturnState` → `CombatActionPanel.ShowReturn` |
| `CombatViewSystem.cs` | Attach에서 `BindActions`·`SetPointerOcclusion`, `RouteInput`, B6 시점의 `SyncActionButtons()` 한 곳, StopInput 숨김 |
| 테스트 | `CombatInteractionTests`: `CombatActionButtons.Read` 조건, `CanEnterAttack`, A2 토글·E 키 유지, B5(UI 위 Select 무시·기록 모드 행위자 선택 무시). `CombatViewSystemTests`: Attach 연결·배치 단계 숨김, 기록 모드 전투 Layer에서 버튼 클릭 = 키 기록, 제출 정지·오류 숨김·포기 확인창 중 무시. `FakeDisplay`에 새 계약 구현. `CombatDisplayReuseTests`의 패널 슬롯 검증은 Unity 확인 뒤 추가 후보 |
| 씬(사용자 에디터) | Combat.unity Canvas에 패널·버튼 3개(공격·턴 종료·복귀, 복귀는 기존 HUD 버튼·문구 오브젝트를 옮겨 재사용 가능)와 `CombatSceneRoot.actionPanel` 연결 |

> **주의:** `CombatSceneRoot` 참조 검증에 넣으므로 **씬에 패널을 연결하기 전에는 Combat 진입이 멈춘다**(포기 패널 때와 같다). 코드와 씬 연결을 같은 Phase로 묶는다.

## 5. SOLID·SSOT·FW 점검

- **S:** 패널은 그리기, `CombatActionButtons`는 가능 여부 묶기, Router는 처리 순서, Policy·Interaction은 판단. 조정자(`CombatViewSystem`)는 연결과 시점만 맡는다.
- **O:** 키 입력 경로(`CombatInputController`·`IStageInputReceiver`)는 바꾸지 않는다. 버튼은 기존 수신 창구에 붙는다.
- **I/D:** `CombatSceneDisplay`가 새 좁은 계약 `ICombatActionDisplay`를 구현하고 ViewSystem은 그 계약으로만 부른다(D2·D3과 같은 분리).
- **SSOT:** 턴 종료 가능 = `CanSubmit(EndUnitTurn)` 한 곳, 공격 진입 가능 = `AttackTargetingInteraction.CanEnter` 한 곳, 복귀 가능 = Core 견적 한 곳, UI 가림 = `CombatPointerMapper` Raycast 한 곳. 버튼 쪽에 조건식을 복제하지 않는다.
- **FW:** 새 서비스·모듈 없음. 버튼 입력은 FW UI 규칙(`UIInputBinding`)을 따른다. Core 변경·요청 없음.

## 6. 확인 방법

- EditMode: 위 테스트, 기존 `CombatInputReceiverTests`·`CombatViewSystemTests`·`CombatDisplayReuseTests` 단언 변경 없이 통과.
- Live 재생:
  1. 배치 단계: 공격·턴 종료 버튼 숨김. G로 전투 시작 → 버튼 보임.
  2. 플레이어 턴: 공격 버튼 → 범위 표시, 버튼 뒤 칸으로 공격이 나가지 않음(B5). 다시 누르면 모드 종료(A2).
  3. 턴 종료 버튼 → 대기 중·적 연출 중 비활성 → 다음 플레이어 턴에 다시 활성.
  4. 경로 지정 중 턴 종료 버튼 → 경로 취소 뒤 턴 종료(키와 같음).
  5. 이동 뒤 복귀 버튼 → 기존 견적·복귀 동작 그대로.
  6. 포기 확인창 열린 동안 버튼 클릭 무시, 승패 확정 뒤 버튼 숨김.
  7. RecordInputOnly: 개발 Layer 패널로 전투 Layer → 버튼 보임·턴 종료 기록.

## 7. 기존 복귀 버튼 검토 (A4, 2026-10-13)

사용자 요청으로 `CombatHud.returnButton`의 경로를 끝까지 따라가 검토했다.

### 7.1 지금 동작

```text
HUD 복귀 버튼(슬롯 0) → CombatHud._returnRequested → CombatViewSystem.RequestReturn()
  → CombatInputReceiver.RequestReturn()   ← R 키와 같은 입구(오류·연결·활성·포기 확인창 검사)
  → Router Request.Return → CombatPathInput.RequestReturn()
       허용: AllowsInput(입력 가용·경로 대상·CanEditPath) · PlayerInput · 활성 아군 행위자
       → 복귀 견적 요청 → 응답(CanReturn·context 일치) → ReturnToCheckpoint 제출
누름 가능: CombatPathInput.RefreshReturnPreview(매 Tick 폴링, R6 유지)가 이동 기록이 있는 행위자의 견적을 요청 →
           SetReturnState(preview) → CombatHud.ShowReturn: interactable = preview.CanReturn, 문구 = 환급 비용·거리
```

### 7.2 좋은 점(유지)

- **입구가 키와 같다.** 새 행동 버튼과 같은 B2 방식이라 손댈 필요가 없다.
- **누름 가능을 Core 견적이 정한다.** 복귀 가능 여부·환급은 Core 규칙이라 View가 계산하지 않는다(P6).
- **입력 정리 경로가 견적 표시를 지운다.** 공격 모드 진입·턴 종료·결과 재생 시작·포기 확인창 열기가 모두 `CancelInputs → ClearReturnRequest → SetReturnState(null)`을 거쳐 버튼을 끈다.

### 7.3 문제

| ID | 문제 | 영향 |
|---|---|---|
| R-a | **공격 모드에서 버튼(비활성이어도)을 누르면 버튼 뒤 칸 공격이 기록될 수 있었다.** 복귀 버튼은 공격 모드 진입 때 꺼지지만 Graphic이라 클릭은 그대로 아래로 간다 | 이번 B5로 해결(UI 위 Select를 공격으로 쓰지 않음) |
| R-b | **숨김 규칙이 없다.** 배치 단계·승패 확정 뒤·제출 정지 뒤에도 "복귀"(비활성)로 보인다. HUD `Clear`도 숨기지 않는다 | 새 행동 버튼은 배치 단계·종료 뒤 숨으므로 나란히 두면 복귀만 남는다 |
| R-c | **RecordInputOnly에서는 항상 비활성.** 기록 모드는 경로 대상이 없어 견적을 요청하지 않는다(W20) | 쓸 수 없는 버튼이 보인다. R-b를 해결하면 같이 정리할지 정해야 한다 |
| R-d | 거절 문구가 열거형 이름 그대로다(`복귀 불가: {FailureReason}`) | 요청 조건(이동 기록 있음) 때문에 드물게만 보인다. 문구 표는 별도 작업 후보 |

판정이 두 곳(버튼 = 마지막 Core 견적, 실제 수락 = `AllowsInput`)인 점은 7.2의 정리 경로가 맞춰 주고 있어 이번 검토에서 어긋나는 경우를 찾지 못했다. 근거 없이 바꾸지 않는다.

### 7.4 결정 필요

| ID | 질문 | 추천 |
|---|---|---|
| A6 | 복귀 버튼 보임(R-b·R-c) | **행동 버튼과 같은 보임 규칙을 쓴다.** `CombatSceneDisplay.SetActionsAvailable`이 `buttons.Visible`을 HUD의 새 `SetReturnVisible(bool)`에도 전달한다. 보임 규칙은 `CombatActionButtons.Visible` 한 곳, 누름 가능은 지금처럼 Core 견적. 기록 모드 전투 Layer에서는 보이되 비활성(키 R과 같음). 대안 ①: 복귀 버튼·문구를 `CombatActionPanel`로 옮김(HUD 필드 제거, 씬 참조 재연결 필요, 견적 문구까지 패널 책임이 됨) 대안 ②: 지금처럼 항상 보임 |

**결정(2026-10-13, 사용자): A6 = 대안 ①, 복귀 버튼을 새 버튼처럼 재구성한다.**

- 복귀 버튼(`returnButton`, 슬롯 2)과 견적 문구(`returnText`)는 `CombatActionPanel`이 소유한다. `CombatHud`에서 필드·`BindReturn`·`ShowReturn`·복귀 슬롯을 지웠다(HUD는 골드·이동 견적·완료만).
- 입구: `ICombatActionDisplay.BindActions(attack, endTurn, returnToCheckpoint)` 하나. `ICombatBattleDisplay.BindReturn`은 지웠다. `CombatViewSystem`의 `_onReturnButton`이 R 키와 같은 `CombatInputReceiver.RequestReturn()`으로 넘긴다. `ICombatViewService.RequestReturn`은 D10 결정대로 남긴다.
- 보임: 세 버튼 모두 `CombatActionButtons.Visible`(전투 Layer·입력 연결·제출 정지 전). 배치 단계·종료 뒤·오류 뒤 함께 숨는다(R-b). 기록 모드 전투 Layer에서는 보이되 견적이 없어 비활성이다(R-c, 키 R과 같음).
- 누름 가능: `Visible` **그리고** Core 견적 `CanReturn`. 패널은 두 값을 받은 그대로 조합하며 판단을 더하지 않는다. 견적 경로(`CombatPathInput` → `ICombatMovementFeedback.SetReturnState` → `CombatSceneDisplay` → `CombatActionPanel.ShowReturn`)는 받는 곳만 HUD에서 패널로 바뀌었다.
- 문구·캐시 규칙(같은 견적이면 다시 쓰지 않음, OnEnable/OnDisable에서 무효화, 반복 표시 할당 0)은 HUD의 것을 그대로 옮겼다. R-d(열거형 문구)는 이번 범위 밖.
- **씬 영향:** Combat.unity의 `CombatHud.returnButton`·`returnText` 참조는 쓸모가 없어진다(Unity가 저장할 때 버린다). 같은 버튼·문구 오브젝트를 패널 아래로 옮겨 `CombatActionPanel`에 연결하면 된다.

## 8. 턴 종료 확인창 (2026-11-03 사용자 요청, 컴파일 미확인, 새 시험 없음, Unity 확인 대기)

- **규칙:** 턴 종료(Space·버튼)를 누를 때 필드에 살아 있는 아군 중 행동 사용 상태가 미사용(`UnitActionUse.Available`)인 유닛이 있으면 공용 확인창 "아직 행동하지 않은 유닛이 N명 있습니다. 턴을 종료할까요?"를 띄운다. 확인하면 지금까지와 같은 턴 종료 경로(Router `EndTurn`)로 제출, 취소하면 아무것도 하지 않는다. 이동만 한 유닛(`Moved`)은 세지 않는다(사용자가 기준을 고르지 않아 "아무 행동도 안 함"으로 정함). 상태 이상으로 실제 행동이 없는 유닛까지 거르는 것은 Core 판단이라 하지 않는다.
- **묻지 않는 경우:** 턴 종료를 지금 제출할 수 없을 때(`CanSubmit(EndPlayerTurn)` 거짓 — 지금처럼 Router가 거절 안내), 튜토리얼 입력 제한 중(안내 흐름을 가로막지 않게). 이때는 확인 없이 기존 경로 그대로다.
- **구조:** 배치 시작 확인창과 같은 꼴·같은 공용 확인창(`IUIConfirmationPresentation`, 새 UI 없음). `Input/ActionButtons/CombatEndTurnEntry`(콜백: 종료 가능·확인 여부·제출·입력 정리) + `CombatEndTurnConfirmation`(판단·표시·응답). 연결 입력 묶음 `CombatSceneInput.EndTurn`·`IsConfirmationOpen`·`CloseConfirmations`·`SyncConfirmations`, 조정자는 `EndTurn` 입력을 Router 대신 입구로 넘기고, 모달 판단·닫기·맞춤은 두 확인창을 함께 본다. 입력 생성 재료의 제출 뒤 콜백 이름 `AfterDeploymentSubmit` → `AfterConfirmedSubmit`(두 확인창이 함께 씀). Core 무변경.
- **확인창 모양 조정(2026-11-03 사용자 "예·아니오 버튼 조금 크게, 글 창은 가로로 길게 2줄 나오게, 본문 글 20% 작게", 에셋만, Unity 확인 대기):** 공용 `Notice` 패널(`Prefabs/UI/GlobalUICanvas.prefab`의 `NoticePannel`, 현강님 UI) 값만 바꿈 — 창 800×600 → 1100×340, 확인 영역 y −285 → −155(창 아래 끝에서 같은 거리), Y/N 버튼 80×30 → 104×39·글자 20 → 26, 본문 TMP 자동 크기 24~72 → 19~58, 창 위치 y −170 → −90(조금 더 화면 가운데로). 같은 패널이라 **확인창이 아닌 일반 안내(거절 안내 등)도 같은 모양**이 된다. 코드 무변경.
- **확인창 예·아니오 눌림 연출(2026-11-03 사용자 "마우스 올릴 때 눌리는 연출", 컴파일 미확인, 새 시험 없음, Unity 확인 대기):** 기존 버튼은 모두 Unity 기본 색 변화(거의 안 보임)뿐이고 Y/N 버튼은 전환·배경 그림이 없어 쓸 연출이 없었다. 새 공용 부품 `GlobalSystem/UISystem/Base/UIButtonPressEffect.cs`(+ `.meta`, 현강님 UI 폴더에 새 파일만): 올리면 대상 크기 0.93배, 누르면 0.86배, 0.06초, 시간 배율 무관, 누를 수 없거나 꺼지면 원래 크기. 버튼 자신이 아니라 글자 자식을 줄인다(클릭 영역이 줄면 가장자리에서 들어감·나감 반복). `GlobalUICanvas.prefab` Y·N 버튼에 붙이고 대상 = 각 `Text (TMP)`. 다른 버튼에도 같은 부품을 붙여 쓸 수 있다. 사용자 프리팹 편집에서 켜진 채 저장된 `NoticePannel`은 사용자 지시로 다시 끔(옛 값과 같음).
