# 스테이지 입력 사용 안내 — View 작업자용

- 작성일: 2026-09-30
- 작성: View 작업(스테이지 내부 담당)
- 대상: 입력을 받아 화면을 움직이거나 명령을 기록하는 View 작업자
- 근거: [스테이지 내부 입력 설계](../specs/2026-09-30-stage-input-design.md)(규칙 I1~I9, 결정 D1~D7, §11 구현 결과)
- 브랜치: `feature/Yonggeun/Stage`

## 0. 한 줄 요약

**View는 Unity 입력을 직접 읽지 않는다.** `IStageInputReceiver`를 구현해서 수신 슬롯에 붙고(`Bind`), 지금 받을 입력의 종류는 Layer로 고른다(`ChangeLayer`/`ClearLayer`). 키가 무엇인지, 콜백이 언제 오는지는 입력 계층이 처리한다.

View가 쓰는 건 네 가지뿐이다.

| 쓰는 것 | 어디서 받나 | 하는 일 |
|---|---|---|
| `IStageInputReceiver` | View가 직접 구현 | 입력을 받는 메서드 11개 |
| `IStageInputReceiverSlot` | `resolver.GetService(StageInputKey.ReceiverSlot)` | `Bind(this)` / `Unbind(this)` |
| `IInputLayerService` | `resolver.GetService(SystemGroupsService.Global).InputServiceCommand.InputLayerServices` | `ChangeLayer(Layer)` / `ClearLayer()` |
| `StageInputLayers` | 정적 클래스 | `Deployment`, `Combat` |

## 1. 구조

```text
[키 설정]  Assets/ProjectL/Input/PlayerInputActions.inputactions
    ▲ 참조 (View DB: DataBaseSO/ViewGroup/InputSettings/Item/InputSettings_Default)
    │
[Global 입력 계층]  GlobalSystem/InputSystem/UnityBridge/   ← View가 직접 부르지 않음
  UnityInputBridge: 에셋 복제·구독 → InputSystem.Submit(맵, 액션, 단계, 값)
  UnityInputLayerRuntime: 현재 Layer의 ActionMap만 켬
    │
[Framework InputSystem]  Layer가 허용한 맵만 경로표로 전달
    │
[스테이지 입력]  InGameSystem/ViewSystem/StageInput/
  DeploymentInputController / CombatInputController
    │  "무엇을 했나" (포인터 위치, 선택 누름·뗌·취소, 버튼 요청)
    ▼
  StageInputReceiverSlot ◄── Bind / Unbind ── 여러분의 View (IStageInputReceiver)
                                               └ 뜻을 정하고 CombatViewBuffer에 명령 기록
```

- **씬에 `PlayerInput` 컴포넌트는 필요 없다.** `UnityInputBridge`가 그 일을 대신한다.
  - 액션 에셋을 들고 있다.
  - 맵을 켜고 끈다.
  - `actionTriggered` 콜백을 받는다.
- 이름으로 함수를 찾는 SendMessage 방식 대신, Controller가 시작할 때 **함수를 직접 등록**한다. 이름이 틀리면 조용히 무시되지 않고 시작할 때 검증 에러가 난다.

## 2. 입력 한 번의 흐름 — Space로 턴 종료

```text
Space 누름
 → Unity: "Combat/EndTurn" Performed          (Combat 맵이 켜져 있을 때만)
 → UnityInputBridge → InputSystem.Submit("Combat", "EndTurn", Performed, true)
     └ 현재 Layer가 Combat 맵을 허용하는가? 아니면 여기서 버림
 → CombatInputController가 등록한 함수 → 슬롯.EndTurnRequested()
 → 슬롯에 붙은 View.EndTurnRequested()
 → View: CombatViewBuffer에 EndUnitTurn 명령 기록
```

### 2.1 체크포인트 복귀 — R (2026-10-06)

`Combat/Return`은 `<Keyboard>/r`에 연결된다. `CombatInputController` → `StageInputReceiverSlot.RequestReturn()` → `CombatViewSystem.RequestReturn()` 순서로 UI 복귀 버튼과 같은 요청을 보낸다. 이동 모드와 기존 입력 허용 조건을 통과해야 하며, 최신 Core 복귀 미리보기 승인 뒤에만 명령을 제출한다. 비용 환불을 View에서 계산하거나 직접 적용하지 않는다.

### 2.2 현재 전투 이동 조작 (2026-10-06)

1. Q로 경로 지정을 시작한다. 기본 이동 모드라는 이유만으로 경로 수집을 시작하지 않는다.
2. 활성 유닛의 시작 칸(앵커) 또는 상하좌우로 붙은 칸에 커서를 넣는다. 붙은 칸이면 그 칸이 첫 걸음이 된다(대각선 칸은 시작하지 않는다, 2026-10-09). 이때부터 좌클릭을 누르지 않고 마우스 이동으로 경로를 그린다.
3. 끝점에서 좌클릭을 누르면 경로가 고정되고, 같은 경로의 최신 Core 승인 응답을 받은 뒤 실행한다. 버튼을 놓는 것은 이동 확정이 아니다.
4. 우클릭은 지정 중인 경로를 취소한다. 실행한 이동은 R로 별도 복귀를 요청한다. 확정·취소 뒤 새 경로는 Q부터 다시 시작한다.

UI/필드 밖 클릭은 확정하지 않는다. 커서를 이미 지나온 경로 칸에 올리면 그 칸까지 경로가 잘린다(2026-10-09). 벗어났다가 돌아올 때는 기존 경로의 칸(그 칸까지 잘림)에서 이어야 하며 빈 구간을 자동 연결하지 않는다. 경로가 멈춘 동안 HUD는 "끝점에서 이어 그리세요"를, Core가 거절했으면 "이동 불가: 이유"를 보인다. E·턴 전환·포커스 상실·입력 해제는 경로 지정을 끝낸다.

경로 강조(청록)는 Core가 확인한 유지 경로에만 적용한다. 검증 중인 후보는 노랑, 거절된 후보는 빨강으로 별도 표시한다. 거절 시 남은 경로의 어느 칸에 붙은 칸을 가리키면 그 경로 칸까지 잘린 뒤 이어진다(이동 유닛 주변 4칸 포함, 2026-10-09). 먼 칸으로 건너뛰거나 자동 우회하지 않는다. 불가 후보 클릭이나 확정 후 Core 거절은 기존 경로를 보존하고 재지정을 허용한다. 접두 경로가 표시되어 있어도 실행에는 전체 경로의 최신 승인이 필요하다.

## 3. View에서 입력 받기 — 예시

> 아래 코드는 **사용법 예시**다. 실제 전투 View는 아직 설계 전이며, 모듈 등록 위치와 씬 객체 연결은 그 설계에서 정한다.

### 3.1 기본 모양

```csharp
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.View;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageInput;
using ProjectL.Systems.ServiceKeys;

internal sealed class CombatInputView : IStageInputReceiver, ISystemPostBindService, IExportSystemServices
{
    private IStageInputReceiverSlot _slot;
    private IInputLayerService _layers;
    private ICombatViewBufferWriteSink _commands;

    // 1) 서비스 받기: PostBindService에서 한 번
    public void PostBindService(ISystemServiceStoreReader resolver)
    {
        _slot = resolver.GetService(StageInputKey.ReceiverSlot);
        _layers = resolver.GetService(SystemGroupsService.Global).InputServiceCommand.InputLayerServices;
        _commands = resolver.GetService(BufferSystemKey.Buffer).CombatViewWriteSink;
    }

    // 2) 전투 화면에 들어올 때: 붙고, 배치 입력을 켠다
    public void OnCombatEnter()
    {
        _slot.Bind(this);
        _layers.ChangeLayer(StageInputLayers.Deployment);
    }

    // 3) 전투 화면을 나갈 때: 입력을 끄고, 파괴하기 "전에" 떨어진다
    public void OnCombatExit()
    {
        _layers.ClearLayer();
        _slot.Unbind(this);
    }

    public void ExportServices(ISystemWriteSink sink) => sink.AddModule<ISystemModule>(this);

    // ↓ IStageInputReceiver 구현은 3.2
}
```

### 3.2 받는 메서드 구현

```csharp
    private float _lastX, _lastY;

    public void PointerMoved(float screenX, float screenY)
    {
        _lastX = screenX; _lastY = screenY;
        UpdateHover(ScreenToCell(screenX, screenY));        // 호버 표시. 명령은 기록하지 않음
    }

    public void SelectPressed(float screenX, float screenY)
    {
        BeginDragIfUnitUnder(screenX, screenY);             // 드래그 시작 후보
    }

    public void SelectReleased(float screenX, float screenY)
    {
        // 정상 뗌 = 선택 확정 "후보". 지금 상태를 보고 명령으로 만들지 View가 판단한다.
        CellCoord cell = ScreenToCell(screenX, screenY);
        if (_dragging.HasValue && IsInsideField(cell))
            _commands.Submit(CombatViewCommandData.PlaceUnit(_dragging.Value, cell, _rotation));
        EndDrag();
    }

    public void SelectCanceled()
    {
        EndDrag();                                         // 표시만 정리. 명령은 절대 기록하지 않음
    }

    public void Cancel()               => EndDrag();       // 사용자가 취소 키를 누름
    public void EnterCombatRequested() => _commands.Submit(CombatViewCommandData.EnterCombat());
    public void DrawRequested()        => _commands.Submit(CombatViewCommandData.DrawCharacter());
    public void EndTurnRequested()     => _commands.Submit(CombatViewCommandData.EndUnitTurn());
    public void MoveModeRequested()    => _mode = TargetMode.Move;
    public void AttackModeRequested()  => _mode = TargetMode.Attack;
    public void RequestReturn()       { /* 현재 View에서는 Core 복귀 미리보기를 요청한다. */ }
```

### 3.3 배치 → 전투로 Layer 바꾸기

"전투 시작" 입력을 받았다고 바로 Combat Layer로 바꾸지 않는다. **Core가 그 명령을 수락한 결과를 확인한 뒤** 바꾼다(설계 D4). 거절되면(예: 메인 캐릭터 미배치) 배치 입력이 계속 필요하기 때문이다.

```csharp
    private int _enterCombatNumber;

    public void EnterCombatRequested()
    {
        _enterCombatNumber = _commands.Submit(CombatViewCommandData.EnterCombat());
    }

    // Core 결과를 읽는 쪽(전투 View 설계에서 정함)에서
    private void OnCommandResult(int commandNumber, bool accepted)
    {
        if (commandNumber == _enterCombatNumber && accepted)
            _layers.ChangeLayer(StageInputLayers.Combat);
    }
```

> Core 결과(결과 순번, 처리 번호, 사건 목록)는 아직 연결 전이다. `CombatStateBuffer`에 자리만 있다.

### 3.4 UI 버튼

턴 종료 버튼 같은 uGUI 버튼은 Unity EventSystem으로 들어오고, 이 입력 계층을 거치지 않는다. **같은 수신 메서드를 부르면** 키와 버튼이 같은 경로를 탄다(설계 I8).

```csharp
endTurnButton.onClick.AddListener(combatInputView.EndTurnRequested);
```

버튼을 눌렀다고 필드 선택이 자동으로 막히지는 않는다. "UI 위에서는 필드 선택 무시"는 View가 처리한다(전투 View 설계에서 정함).

## 4. 수신 메서드 규칙

| 메서드 | 오는 때 | View가 할 일 |
|---|---|---|
| `PointerMoved(x, y)` | 포인터가 움직일 때(화면 좌표) | 호버·하이라이트. 좌표 → 칸 변환은 View 몫 |
| `SelectPressed(x, y)` | 선택 버튼을 누른 순간(마지막 포인터 좌표) | 배치: 집기 / 전투 이동: 지정 경로 확정 |
| `SelectReleased(x, y)` | **정상적으로 뗀** 순간 | 배치·공격: 확정 후보 / 전투 이동: 무시 |
| `SelectCanceled()` | 선택이 **강제로** 끝남: Layer 전환·`ClearLayer`·종료·`Unbind` | **정리만 한다. 명령 기록 금지** |
| `Cancel()` | 취소 키(우클릭) | 진행 중인 제스처 정리 |
| `EnterCombatRequested()`, `DrawRequested()` | 배치 Layer의 버튼 | 명령 기록 |
| `EndTurnRequested()`, `MoveModeRequested()`, `AttackModeRequested()` | 전투 Layer의 버튼 | 명령 기록 또는 모드 전환 |
| `RequestReturn()` | 전투 Layer의 R / UI 복귀 버튼 | 기존 체크포인트 복귀 요청 경로로 전달 |

보장되는 것:

- **한 번 누르면 `SelectPressed` 한 번**이다. 누른 채 있어도 반복되지 않는다.
- 누름 뒤에는 `SelectReleased`와 `SelectCanceled` 중 **정확히 하나**가 온다.
- **누름을 받은 View만** 그 뗌·취소를 받는다.
  - 누른 채 View가 바뀌면 옛 View가 `SelectCanceled`를 받는다.
  - 새 View에는 옛 뗌이 가지 않는다.
- 포인터 입력이 끊겨도(0, 0)으로 튀지 않는다.

## 5. Layer

| Layer | 켜지는 ActionMap | 쓰는 때 |
|---|---|---|
| (없음) | 없음 | 시작 직후, `ClearLayer()` 뒤 |
| `StageInputLayers.Deployment` | Common + Deployment | 전투 진입 전 배치 |
| `StageInputLayers.Combat` | Common + Combat | 전투 중 |

- 입력을 전부 끌 때는 `ClearLayer()`를 쓴다. `ChangeLayer(default)`는 예외다.
- Layer를 바꾸는 순간 누르던 선택은 `SelectCanceled`로 끝난다.
- 등록되지 않은 Layer나, 이름만 같고 맵 구성이 다른 Layer로는 바꿀 수 없다(예외). 항상 `StageInputLayers`의 값을 쓴다.
- 연출 중이나 적 턴처럼 잠깐 입력을 막을 때도 Layer로 막는다(`ClearLayer` 후 다시 `ChangeLayer`). Controller에 상태 판단을 넣지 않는다.

## 6. 새 입력 추가하기 — 예: 전투 중 T키로 배치 방향 회전

1. **액션 에셋:** `Assets/ProjectL/Input/PlayerInputActions.inputactions`를 연다. `Combat` 맵에 액션 `Rotate`(Button)를 추가하고 `<Keyboard>/t`에 바인딩한다. R은 체크포인트 복귀에 사용 중이다.
2. **이름 상수:** `StageInputNames.Combat`에 추가한다.
   ```csharp
   internal const string Rotate = "Rotate";
   ```
3. **수신 계약:** `IStageInputReceiver`에 메서드를 추가한다.
   ```csharp
   /// <summary>전투 단계: 배치 방향 회전을 요청했습니다.</summary>
   void RotateRequested();
   ```
4. **슬롯 전달:** `StageInputReceiverSlot`에 추가한다.
   ```csharp
   public void RotateRequested() => _receiver?.RotateRequested();
   ```
5. **Controller 바인딩:** `CombatInputController.Configure`에 추가한다.
   ```csharp
   input.Press(StageInputNames.Combat.Rotate, _receiver.RotateRequested);
   ```
6. **View 구현:** `IStageInputReceiver`를 구현한 View와 테스트용 기록 대상(`StageInputTests.RecordingReceiver`)에 메서드를 추가한다.
7. **확인:** 컴파일하고 EditMode `StageInputTests`·`UnityStageInputTests`를 돌린다. 에셋에 액션이 없거나 이름이 다르면 `UnityStageInputTests.ProjectAsset_MatchesStageInputConfiguration`이 실패한다. 실행 중이라면 시작할 때 `Input configuration is invalid`로 멈춘다.

바인딩 종류:

| 메서드 | 받는 입력 | 쓰는 곳 |
|---|---|---|
| `input.Press(name, action)` | 버튼 Performed 한 번 | 단발 요청(턴 종료, 모드) |
| `input.Button(name, bool→)` | 누름 true / 뗌·취소 false | 누르는 동안 유지되는 동작 |
| `input.Float` / `input.Vector2` | 값(Canceled 때 0도 전달) | 축, 스크롤 |
| `input.Raw(name, kind, snapshot→)` | 단계와 값을 그대로 | 특별한 해석이 필요할 때(선택 버튼이 이 방식) |

## 7. 새 화면의 입력 추가하기 — 예: 스테이지 맵

`Configure`는 런타임에 **한 번**이라서, 모든 화면의 Controller는 시작할 때 함께 등록한다(설계 I1).

1. 에셋에 맵이 있는지 확인한다(`StageSelection` 맵은 이미 있다).
2. 이름 상수, Layer(예: Common + StageSelection), 수신 계약·슬롯, Controller를 스테이지 입력과 같은 방식으로 만든다.
3. `Factory/Runtime/RunTimeSystemFactory.cs`에서 Controller 목록과 Layer 목록에 추가한다. 슬롯은 루트에 등록하고, 키는 `ServiceKeys`에 둔다.

   ```csharp
   IInputController[] inputControllers = { deployment, combat, stageSelection };
   GlobalSystemFactory.Create(inputControllers, allLayers).ExportServices(systemBuilderHelper);
   ```

4. 한 `(ActionMap, Action)`은 Controller 하나만 등록할 수 있다. 같은 맵을 두 Controller가 나눠 가지지 않는다.

## 8. 하지 말 것

| 하지 말 것 | 이유 |
|---|---|
| 씬에 `PlayerInput`을 붙이거나 `InputAction`·`Keyboard.current`를 직접 읽기 | 입력 경로가 둘이 되어 Layer 차단이 안 먹는다 |
| Controller를 직접 호출 | Controller는 입력 계층 내부다. View는 수신 메서드로만 받는다 |
| `SelectCanceled`에서 명령 기록 | 강제 종료(화면 전환 등)가 배치·이동으로 확정된다 |
| `Unbind` 없이 View 파괴 | 파괴된 객체가 입력을 받는다. 파괴 **전에** `Unbind` |
| 이미 붙은 상태에서 다시 `Bind` | 예외다(같은 대상이어도). 먼저 `Unbind` |
| `ChangeLayer(default)` | 예외다. `ClearLayer()` |
| Controller에 전투 상태 판단 넣기 | Controller는 "무엇을 했나"만 전한다. 뜻은 View가 정한다 |

## 9. 키 설정 교체와 에러

**키 설정 교체:** `DataBaseSO/ViewGroup/InputSettings/Item/InputSettings_Default`의 **Actions** 칸에 다른 `.inputactions`를 넣는다.
- 코드를 고치지 않아도 된다.
- 이 테이블은 아이템이 정확히 하나여야 한다.
- 새 에셋에도 맵·액션 이름과 종류가 같아야 한다.

| 에러 메시지 | 뜻 | 고칠 곳 |
|---|---|---|
| `Input configuration is invalid:` + 목록 | 에셋과 코드가 안 맞음(맵·액션 없음, 값 종류 다름, 켤 Layer 없음) | 목록에 나온 에셋 액션 또는 `StageInputNames`·Controller |
| `[UnityInputBridge] … is required.` | View DB의 입력 설정이 없음 | `InputSettings_Default`와 ViewGroup 등록 |
| `Input Layer is not registered` / `does not match the registered definition` | 등록 목록에 없는 Layer, 또는 이름만 같은 다른 Layer | `StageInputLayers` 값 사용 |
| `InputSystem is not configured.` | 초기화 전(`InitData` 전)에 `ChangeLayer`/`ClearLayer` 호출 | 호출 시점을 `Initialize` 이후로 |
| `입력 수신 대상이 이미 연결되어 있습니다` | 중복 `Bind` | 앞 View가 `Unbind`했는지 확인 |

## 10. 지금 한계

- **런타임 부트스트랩이 없다.** `FrameworkRuntime`을 만들고 Tick하는 구동기가 없어서, 에디터 재생으로 입력을 확인할 수 없다. 확인은 EditMode 테스트로 한다.
- **실제 장치로 누름·뗌 순서를 검사하는 테스트는 보류했다.** 입력 해석 규칙은 `StageInputTests`(Submit 직접 호출)로, 맵 전환은 `UnityStageInputTests`로 확인한다.
- 프로젝트 설정의 전역 액션(`InputSystem_Actions`, Unity 기본 템플릿)이 재생 때 자동으로 켜진다. 받는 코드가 없어 동작에는 영향이 없다.
- 전투 View(수신 구현, 칸 변환, 드래그, 명령 기록)는 다음 설계 대상이다.

## 11. 파일 위치

| 파일 | 역할 |
|---|---|
| `Assets/ProjectL/Input/PlayerInputActions.inputactions` | 키 설정 |
| `Assets/ProjectL/DataBaseSO/ViewGroup/InputSettings/` | 어떤 키 설정을 쓸지(설정형 DB) |
| `Systems/RuntimeSystem/GlobalSystem/InputSystem/UnityBridge/` | Unity 연결(브리지, Layer 런타임, 검증기, 값 변환) |
| `Systems/RuntimeSystem/GlobalSystem/InputSystem/View/InputSystem.cs` | Framework 라우터(`ChangeLayer`, `ClearLayer`, `Submit`) |
| `Systems/RuntimeSystem/InGameSystem/ViewSystem/StageInput/` | 이름, Layer, 수신 계약, 슬롯, Controller |
| `Systems/ServiceKeys/StageInputServiceKey.cs` | `StageInputKey.ReceiverSlot` |
| `Systems/Factory/Runtime/RunTimeSystemFactory.cs` | Controller·Layer 목록 조립, 슬롯 루트 등록 |
| `Tests/EditMode/Framework/StageInputTests.cs`, `UnityStageInputTests.cs` | 테스트 |
