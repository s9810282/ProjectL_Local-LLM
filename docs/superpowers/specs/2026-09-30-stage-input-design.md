# 스테이지 내부 입력 설계 — Framework InputSystem의 Unity 연결과 전투 Controller

- 작성일: 2026-09-30
- 작성: View 작업(스테이지 내부 담당)
- 상태: **구현 완료(계획 Phase 0~4, 2026-09-30).**
  - Phase 3·4의 컴파일과 EditMode 테스트 결과는 아직 받지 않았다.
  - 가상 장치 테스트는 보류했다(B).
  - 결과와 달라진 점은 §11에 있다.
  - 설계 본문(2차, 구조 검토 반영)은 바꾸지 않았다.
- 위치: 스테이지 내부 View 전체 작업의 첫 단계. 이전 전투 Flow 후보(Buffer 설계 §12)는 **참고하지 않고** 새로 짠다.

## 0. 요약

- Framework의 `InputSystem`은 받은 입력을 Layer와 경로표에 따라 핸들러로 나눠 주는 **중간 단계만** 있다. 장치에서 입력을 모아 넣는 쪽과 Layer를 저장하는 쪽이 없고, 호출하는 코드도 없다.
- 이번 설계는 두 층을 만든다.
  1. **Unity 연결 계층(Global):** `InputActionAsset`에서 입력을 모아 `Submit`으로 넘기고, Layer에 맞춰 ActionMap을 켜고 끄고, 모든 Controller로 `Configure`를 한 번 호출한다.
  2. **스테이지 내부 Controller:** 배치와 전투의 ActionMap을 맡는다. 입력을 "무엇을 했나"(포인터 위치, 선택 누름·뗌, 취소, 턴 종료, 모드 전환) 수준으로 바꿔 **입력 수신 슬롯**에 넘긴다.
- 뜻을 정하는 건 View다. "그 칸에 무엇이 있나, 이게 배치인가 이동인가"는 수신 슬롯에 붙은 전투 View가 판단하고, `CombatViewBuffer`에 명령으로 기록한다. 이 View는 다음 설계 대상이다.
- 옛 `develop-4week`의 `Unity/Adapter/Input/`에서 검증된 부분(수집, 값 변환, Layer 전환, 누름·뗌 판정)을 가져온다. 다만 씬마다 설치하던 구조는 버린다. 새 InputSystem은 **런타임에 한 번만 Configure**하기 때문이다.
- 전체 입력 해제는 새 `ClearLayer()` 계약으로 표현한다. 정상적인 선택 완료와 강제 취소를 구분하고, 슬롯은 누름을 받은 View만 그 선택의 완료·취소를 받도록 관리한다.
- Controller 목록과 Layer 목록은 모두 루트 조립 지점에서 전달한다. 실제 입력 설정 누락은 초기화 실패로 처리하고, 입력이 필요 없는 테스트는 입력 그룹을 조립하지 않는다.

## 1. 범위

| 포함 | 제외 |
|---|---|
| Unity 연결 계층: 입력 수집, Layer 저장·ActionMap 전환, 설정 조립과 한 번 Configure | 포인터 → 논리 칸 변환, 드래그·호버 표시, `CombatViewBuffer` 기록: 전투 View 설계 |
| 액션 에셋: 옛 `PlayerInputActions.inputactions` 이식 | 다른 화면 Controller(StageSelection, Artifact, Settings). 에셋에 맵은 옮기지만 Controller는 나중에 만든다 |
| 스테이지 내부 Controller 두 개(배치, 전투)와 Layer 정의 | 런타임 부트스트랩(Runtime 생성·Tick). 이번에는 컴파일과 테스트로만 확인한다 |
| 입력 수신 슬롯(View 연결과 진행 중인 선택의 소유권) | uGUI 버튼 연결과 UI 위 포인터 차단. UI 의미 명령은 같은 수신 인터페이스를 사용한다(§5 I8) |

## 2. 지금 Framework InputSystem (확인한 사실)

```text
[없음: 장치 수집] ─► IInputSystemService.Submit(ActionMap, Action, Phase, 값, 시간)
                       │ 현재 Layer가 ActionMap을 허용하나?  (IInputLayerRuntime — 구현 없음)
                       ▼
                  InputRouteTable ─► InputController.Configure에서 등록한 핸들러
```

| 대상 | 계약 |
|---|---|
| `IInputSystemService.Configure(layerRuntime, configuration)` | **한 번만** 가능하다. 두 번째 호출이나 Shutdown 뒤 호출은 예외다 |
| `InputConfiguration.Create(controllers)` | Controller가 하나 이상 있어야 하고, 각 Controller는 바인딩이 하나 이상 있어야 한다. `(ActionMap, Action)` 중복은 예외다 |
| `InputBindings` | `Press`(Performed만), `Button`(Performed 값 / Canceled면 false), `Float`, `Vector2`, `Raw` |
| `InputLayer` | 이름과 허용 ActionMap 목록. 같은지는 이름으로만 비교한다 |
| 비활성 Layer | 현재 `ChangeLayer(default)`와 맵이 0개인 `InputLayer` 생성은 모두 예외다. 기존 API로 비활성 상태에 돌아갈 수 없으므로 §4.1의 `ClearLayer`를 추가해야 한다 |
| `Submit` | Layer가 허용하지 않는 ActionMap은 버린다. 값 종류가 등록과 다르면 예외다. Shutdown 뒤에 오는 입력은 조용히 무시한다 |
| 호출자 | **없음.** 가이드도 "기본 장치 수집·Layer 저장까지 자동 제공되는 것은 아니다"라고 적는다 |
| 어셈블리 | `ProjectL.Systems.asmdef`가 **`Unity.InputSystem`을 참조하지 않는다** |
| 형식 지정 초기화 | `ISystemInitData<T>`의 기본 전달은 `(T)service` 강제 형변환이다. 다른 타입의 가짜 컨텍스트는 본문에 들어오기 전에 실패한다 |
| 실제 View DB | `ViewGroup.asset`에는 테이블이 6개이고 팩토리는 9개를 요구한다. Trait·RecoveryOptionVisual·Railway 등록 누락은 실제 DB 조립의 선행 장애다 |

## 3. 옛 코드에서 가져올 것과 버릴 것

출처: `origin/develop-4week`의 `Assets/Scripts/ProjectL/Unity/Adapter/Input/`

| 옛 코드 | 처리 | 이유 |
|---|---|---|
| `UnityInputRuntime` | **가져온다** | 에셋 복제, `actionTriggered` 구독, 바인딩별 값 종류 캐시, `Submit` 호출. 끌 때는 ActionMap을 먼저 꺼서 마지막 Canceled까지 전달한 뒤 구독을 푼다 |
| `InputActionValueReader` | **가져온다** | Unity Phase·값 → `InputPhase`/`InputValueData`. Canceled면 0/false로 읽는다 |
| `InputLayerRegistry` / `InputLayerState` | **수정해서 가져온다** | Layer별 맵 해석·전환 중 요청 보류·Suspend/Resume을 유지한다. 옛 코드의 유효한 초기 Layer 전제를 없애고, 비활성 시작·`ClearLayer`를 지원한다 |
| `InputConfigurationValidator` | **수정해서 가져온다** | 맵·액션·값 종류·도달 가능성을 검사한다. 유효한 초기 Layer 필수 검사는 제거한다. 등록 Layer는 여전히 유효해야 한다 |
| `CombatInput`의 선택 판정 | **수정해서 가져온다** | `Raw` 버튼 값 변화로 중복을 제거하되, `Canceled`는 정상 뗌과 구분한다. 선택 액션은 기존 `Press(behavior=1)` 계약을 유지한다(§4.3) |
| `PlayerInputActions.inputactions` | **가져온다** | 맵: Common, Settings, StageSelection, Deployment, Combat |
| `PlayerInputActions.cs`(생성된 1,355줄 래퍼) | 버린다 | 이름을 얻는 데만 썼다. 문자열 상수 클래스로 대신한다(D3) |
| `InputInstaller`, `InputFeatureCatalog`, `InputSetupBuilder`의 씬 대상 연결 | 버린다 | 씬마다 설치하던 구조다. 새 구조는 한 번 Configure + 수신 슬롯이다 |
| Settings·StageSelection·Artifact Controller | 이번엔 제외 | 범위 밖 |

## 4. 구조

```text
┌ Global / InputSystem ───────────────────────────────────────────────┐
│ InputSystem (기존)                                                   │
│   ▲ Submit                         ▲ Configure(한 번)                 │
│ UnityInputBridge (신규)            │                                  │
│   - InputActionAsset 복제·구독      │                                  │
│   - 값·Phase 변환                   │                                  │
│   - UnityInputLayerRuntime : IInputLayerRuntime (신규)                │
│       Layer → ActionMap 켜기/끄기                                     │
│   - 시작 때 설정 검증 → Configure ──┘                                 │
└──────────────────────────────────────────────────────────────────────┘
          ▲ Controller 목록 + Layer 목록 (루트 조립 시점에 전달, D2)
┌ 스테이지 내부 입력 ─────────────────────────────────────────────────┐
│ DeploymentInputController  (ActionMap "Deployment")                  │
│ CombatInputController      (ActionMap "Combat")                      │
│        │  무엇을 했나: 포인터 위치 / 선택 누름·뗌 / 취소 / 턴 종료 …   │
│        ▼                                                              │
│ StageInputReceiverSlot ── Bind(receiver) / Unbind(receiver)          │
│        │  수신 대상 + 진행 중인 선택의 소유자                           │
└────────┼──────────────────────────────────────────────────────────────┘
         ▼
   전투 View (다음 설계) : 포인터 → 논리 칸, 뜻 판단 → CombatViewBuffer.Submit / SetPreview
```

### 4.1 Layer

| Layer | 허용 ActionMap | 쓰는 때 |
|---|---|---|
| `StageInputLayers.Deployment` | Common, Deployment | 전투 진입 전 배치 |
| `StageInputLayers.Combat` | Common, Combat | 전투 중 |
| (없음) | — | 시작 직후와 `ClearLayer()` 완료 뒤. `CurrentLayer == default`이며 모든 맵이 꺼진다 |

- `IInputLayerService`와 `IInputLayerRuntime`에 `void ClearLayer()`를 추가한다. `InputSystem.ClearLayer()`는 Configure·Shutdown 여부를 기존 `ChangeLayer`와 동일하게 검사하고 런타임에 위임한다.
- `ChangeLayer(default)`와 빈 맵을 가진 이름 있는 Layer는 계속 거부한다. 비활성 상태를 가짜 이름이나 가짜 ActionMap으로 등록하지 않는다. `InputLayer` 자체의 유효성 규칙은 바꾸지 않는다.
- 런타임 생성 직후 `CurrentLayer`는 `default`다. 내부 맵 조회에서 이 상태는 빈 배열로 처리한다. 이 상태에서 `Resume`, `Suspend`, 반복 `ClearLayer`가 예외를 내면 안 된다.
- `ChangeLayer`와 `ClearLayer`는 같은 전환 절차를 쓴다. **이전 Layer를 유지한 채 빠지는 맵을 Disable → CurrentLayer 교체 → 새 맵 Enable** 순서다. 유지되는 Common 맵은 재활성화하지 않는다.
- 전환 중 콜백에서 들어온 변경·해제 요청은 보류하고 마지막 요청을 전환 완료 뒤 적용한다. `default`도 해제 요청 값이므로 요청 유무는 별도 bool로 구분한다. `Suspend` 중에는 보류 요청을 반영해도 맵을 다시 켜지 않는다.

### 4.2 조립과 등록 목록

- `FrameworkRuntimeFactory`에서 슬롯, 두 Controller, `StageInputLayers.Deployment`·`Combat` 목록을 만든다.
- `GlobalSystemFactory.Create(controllers, layers)` → `InputSystemFactory.Create(controllers, layers)` → 브리지로 전달한다. 매개변수는 각각 `IReadOnlyList<IInputController>`, `IReadOnlyList<InputLayer>`다.
- 브리지는 목록을 복사해 보관한다. `InputConfiguration.Create`는 `params IInputController[]`이므로 복사한 배열을 사용한다. `InputConfiguration`은 계속 라우팅 바인딩만 가지며 Layer 목록을 추론하지 않는다.
- Global 연결 계층은 `StageInputLayers`나 전투 View를 직접 참조하지 않는다. null·빈 목록·null Controller·Layer 이름 중복·정의 불일치·존재하지 않는 맵·어느 Layer에서도 켤 수 없는 Controller 맵은 검증 오류다. 등록 후 ChangeLayer에서도 이름만 같고 맵 구성이 다른 정의를 거부한다.
- 등록된 Layer가 허용하는 맵에 Controller가 없는 것은 허용한다. 이번 Common.OpenSettings는 설정 화면 Controller가 범위 밖이므로 처리되지 않는다.

### 4.3 수신 인터페이스와 선택 액션 계약 (Controller → View)

옛 `ICombatTargetingInputTarget` 등을 새 구조에 맞게 줄이고 강제 취소를 별도 메서드로 둔다.

```csharp
public interface IStageInputReceiver
{
    void PointerMoved(float screenX, float screenY);
    void SelectPressed(float screenX, float screenY);     // 누른 순간. 드래그 시작이 될 수 있다
    void SelectReleased(float screenX, float screenY);    // 정상 뗌. 선택 확정 후보
    void SelectCanceled();                              // 제스처 정리만. 명령 확정 금지
    void Cancel();                                      // 사용자가 취소 키를 누름
    // 배치
    void EnterCombatRequested();
    void DrawRequested();
    // 전투
    void EndTurnRequested();
    void MoveModeRequested();
    void AttackModeRequested();
}
```

- 좌표는 **화면 좌표 그대로** 넘긴다. 칸으로 바꾸는 건 카메라와 필드를 가진 View가 한다.
- D5에 따라 배치·전투가 같은 수신 인터페이스를 사용한다. 메서드는 10개다.
- `SelectReleased`도 자동으로 명령을 실행한다는 뜻은 아니다. View가 현재 상태와 유효한 선택인지 판단한 뒤 기록한다. `SelectCanceled`는 드래그·선택 표시를 정리하며 배치·이동 등의 확정 명령을 기록하지 않는다.

**선택 액션은 특정 Interaction 계약을 갖는다.** `Deployment.Press`와 `Combat.Select`는 기존 에셋의 Button + `Press(behavior=1)`(ReleaseOnly), `initialStateCheck=false`를 유지한다. 개별 바인딩에 이 계약을 바꾸는 Interaction을 추가하지 않는다. 일반 Button 설정으로 바꿔도 같은 결과를 낸다고 가정하지 않는다. Phase 1에서 에셋을 확인하고 Phase 4에서 실제 에셋 회귀 테스트로 고정한다.

| 입력 | Controller 처리 |
|---|---|
| `Started/Performed`에서 버튼 값이 false → true | 누름 상태를 먼저 기록한 뒤 `SelectPressed` |
| `Performed`에서 true → false | 누름 상태를 먼저 해제한 뒤 `SelectReleased` |
| `Canceled` | 누름 상태를 먼저 해제하고, 진행 중인 누름이 있었다면 `SelectCanceled`. `SelectReleased`로 바꾸지 않는다 |
| 같은 버튼 값 반복 또는 누르지 않은 상태의 false | 무시한다 |
| 포인터 `Performed` | 마지막 좌표 갱신 후 `PointerMoved` |
| 포인터 `Canceled` | 무시한다. Reader가 만든 `(0, 0)`으로 마지막 좌표나 호버를 덮어쓰지 않는다 |

따라서 선택과 포인터 모두 `Raw`로 구독한다. 기존 `Vector2` 편의 바인딩은 Canceled도 전달하므로 여기에는 사용하지 않는다. ReleaseOnly의 정상 뗌은 Performed 뒤 Canceled가 이어질 수 있으므로, Performed에서 누름 상태를 먼저 지워 추가 취소가 전달되지 않게 한다. Layer 해제·전환·Shutdown 및 장치 리셋 등으로 발생한 Canceled는 선택 정리만 수행한다. 일반 Button에서 정상 뗌이 Canceled인 동작은 이번 선택 계약에 포함하지 않는다.

### 4.4 수신 슬롯과 선택의 소유권

- `IStageInputReceiverSlot : IServiceModule`은 `Bind(receiver)`와 `Unbind(receiver)`를 제공한다. 구현은 Controller에 `IStageInputReceiver`로 전달한다.
- null Bind는 `ArgumentNullException`, 이미 연결된 상태의 Bind는 같은 대상이어도 `InvalidOperationException`이다. 다른 대상의 Unbind는 무시한다.
- 슬롯은 현재 수신 대상과 **SelectPressed를 전달한 선택 소유자**를 별도로 기억한다. 누름 콜백 전에 소유자를 기록하여 그 콜백에서 Unbind해도 취소할 수 있게 한다. 슬롯이 비어 있으면 누름을 전달하지 않으며 소유자도 만들지 않는다.
- `SelectReleased`·`SelectCanceled`는 현재 연결의 선택 소유자에게만 전달한다. 전달 전에 소유자를 지운다. 누름 없는 뗌·취소는 버린다.
- 일치하는 Unbind는 연결과 소유자를 먼저 지운 뒤, 진행 중인 선택이 있었다면 옛 수신자에게 `SelectCanceled`를 한 번 보낸다. 재진입 콜백이 새로 연결한 대상의 상태를 그 뒤 덮어쓰지 않는다. View는 객체를 파괴하기 **전에** Unbind한다.
- Unbind만으로 Controller의 물리 버튼 상태를 false로 되돌리지 않는다. 아직 누른 버튼의 반복 true를 새 View의 누름으로 잘못 해석하지 않도록, 실제 뗌·Canceled까지 중복 제거 상태를 유지한다. 이때 슬롯의 소유자는 이미 없으므로 옛 뗌은 새 View에 전달되지 않는다.
- `Cancel`, 턴 종료 등 단발 요청은 현재 수신자에게 전달한다. 사용자의 취소 키와 `SelectCanceled`의 역할은 다르며, 취소 키를 받은 View도 진행 중인 제스처를 정리한다.

## 5. 규칙

| 번호 | 규칙 |
|---|---|
| **I1** | **Configure는 런타임에 한 번이다.** 스테이지 내부 Controller도 시작할 때 다른 화면 Controller와 함께 등록한다. 씬이 바뀐다고 다시 Configure하지 않는다 |
| **I2** | **Controller는 씬을 모른다.** 씬 객체, 카메라, Buffer를 참조하지 않는다. "무엇을 했나"만 수신 슬롯에 넘긴다. 순수 C#이라 테스트할 수 있다 |
| **I3** | **수신 슬롯은 연결과 선택 소유권을 함께 관리한다.** Unbind는 진행 중인 선택을 취소하며 옛 누름의 뗌을 새 수신자에게 보내지 않는다(§4.4) |
| **I4** | **입력을 막는 건 Layer다.** 전체 차단은 `ClearLayer`로 한다. View도 상태에 따라 수신을 무시할 수 있지만 Controller에는 전투 상태 판단을 넣지 않는다 |
| **I5** | **선택은 ReleaseOnly 계약과 값 변화로 처리한다.** 중복 값은 버리고 정상 뗌과 Canceled를 구분한다. 강제 취소로 명령을 확정하지 않는다(§4.3) |
| **I6** | **실행 시 Unity 입력 타입은 연결 계층에 둔다.** 예외는 View DB의 `InputSettingsData.Actions : InputActionAsset` 참조와 검증용 EditMode 테스트다. Controller·슬롯·Layer·수신 인터페이스에는 Unity 타입이 없다 |
| **I7** | **끌 때 마지막 Canceled까지 전달한다.** 구독과 입력 라우터가 살아 있는 동안 맵을 끄고, 그다음 구독 해제·에셋 파괴·라우터 Shutdown 순으로 정리한다. 전달 의미는 제스처 취소다 |
| **I8** | **UI 의미 명령도 같은 수신 인터페이스를 부른다.** 턴 종료 버튼과 키 입력이 같은 `EndTurnRequested`를 사용한다. UI가 클릭을 받았다고 필드 입력이 자동 차단되지는 않으므로 포인터 차단은 후속 View 설계의 책임이다 |
| **I9** | **실제 입력 설정 누락은 초기화 오류다.** 테스트 편의를 위해 런타임의 설정 오류를 숨기지 않는다. 입력 없는 테스트는 입력 그룹을 생략한다 |

## 6. 수명

| 단계 | Unity 연결 계층 | 스테이지 내부 |
|---|---|---|
| 조립(Factory) | 브리지 생성. Controller·Layer 목록을 받는다(D2) | Controller와 수신 슬롯 생성. 슬롯은 루트 서비스로 노출한다 |
| `InitData` (한 번) | 액션 에셋을 받는다(D1). 설정을 검증하고 Layer 런타임을 만든 뒤 **Configure** | — |
| `Initialize` | 구독 후 Resume. Layer가 없으므로 맵은 계속 꺼져 있다 | — |
| 전투 진입 | — | 전투 View가 슬롯에 `Bind`. Layer를 Deployment로 바꾼다(D4) |
| 배치 → 전투 | — | Layer를 Combat으로 바꾼다(D4) |
| 전투 종료·View 교체 | `ClearLayer` 또는 다음 화면 Layer 적용 | 파괴 전에 `Unbind(receiver)`하여 선택 소유권 해제·제스처 취소. 새 View는 Bind 후 Layer 활성화 |
| `Shutdown` | 브리지 Suspend → 구독 해제 → 복제 에셋 파괴 → InputSystem.Shutdown | View는 자기 Shutdown에서 파괴 전에 Unbind. 이미 비어 있는 슬롯은 최종 Canceled를 버린다 |

`Configure`가 한 번뿐이라 `BindScene`(씬마다 호출)에서 하면 안 된다. `InitData`는 런타임에 한 번만 불리므로 여기에 둔다. `BindScene → InitData → PostBindService → Initialize`는 연결 코드가 선택할 순서이며 Framework가 앞의 세 단계 사이 순서까지 강제하는 것은 아니다. 브리지 Configure는 PostBindService에 의존하지 않도록 생성자에서 InputSystem을 받는다.

### 6.1 입력 설정이 없을 때

- 브리지는 필요한 최소 계약인 `ISystemInitData<IHasViewQuery>`를 사용한다. 정상 조립은 실제 DB 컨텍스트를 전달한다. ViewQuery, 입력 테이블, Config, Actions가 없거나 검증에 실패하면 명확한 예외로 초기화를 중단한다.
- 테이블 자체가 없으면 DB 팩토리에서 먼저 실패한다. 잘못된 컨텍스트 타입은 형식 지정 초기화의 전달에서 실패한다. 둘 다 "입력이 조용히 꺼진 성공 상태"로 바꾸지 않는다.
- 입력 없는 Core·Job 테스트는 해당 그룹만 조립한다. Global/입력 그룹 초기화를 검증하는 테스트는 유효한 `IHasViewQuery`와 입력 설정을 제공한다. 이를 위해 운영 팩토리에 무입력 플래그나 빈 Controller 허용을 추가하지 않는다.
- InitData의 복제·검증 도중 실패하면 복제 에셋을 정리하고 원본은 건드리지 않는다. 실패한 런타임은 종료하고 새로 조립한다. 같은 InputSystem의 재Configure로 복구하지 않는다.

### 6.2 등록·종료 순서

- InputSystemFactory의 모듈 등록 순서는 **InputSystem → UnityInputBridge**다. InputSystemGroup은 역순 Shutdown하므로 브리지가 먼저 맵을 끄고 마지막 Canceled를 라우터에 전달할 수 있다.
- 브리지는 InitData 전·실패 후 Shutdown과 반복 Shutdown에도 안전해야 한다. Suspend 중 콜백을 막는 플래그를 먼저 세우거나 구독을 먼저 풀지 않는다. 자원 해제는 finally로 보장한다.
- 루트는 InGame을 Global보다 먼저 종료한다. 후속 View는 Unbind를 자기 종료의 앞부분에서 수행해 이미 파괴된 객체가 최종 입력을 받지 않게 한다.

## 7. 파일

| 파일 | 내용 | 영역 |
|---|---|---|
| `Assets/ProjectL/Input/PlayerInputActions.inputactions` | 옛 에셋 이식. C# 래퍼 생성은 끄고 선택 액션의 ReleaseOnly 계약을 확인한다 | 신규 에셋 |
| `GlobalSystem/InputSystem/Unity/UnityInputBridge.cs` | 수집, 구독, Configure | 신규(Global) |
| `GlobalSystem/InputSystem/Unity/UnityInputLayerRuntime.cs` | `IInputLayerRuntime`, ActionMap 전환 | 신규(Global) |
| `GlobalSystem/InputSystem/Unity/InputActionValueReader.cs` | 값·Phase 변환 | 신규(Global) |
| `GlobalSystem/InputSystem/Unity/InputConfigurationValidator.cs` | 에셋·설정 검증 | 신규(Global) |
| `…/StageInput/StageInputNames.cs` | ActionMap·Action 이름 상수 | 신규(스테이지 내부, 위치는 D6) |
| `…/StageInput/StageInputLayers.cs` | Layer 정의 | 신규 |
| `…/StageInput/DeploymentInputController.cs`, `CombatInputController.cs` | Controller | 신규 |
| `…/StageInput/IStageInputReceiver.cs`, `StageInputReceiverSlot.cs` | 수신 인터페이스와 슬롯 | 신규 |
| `GlobalSystem/InputSystem/View/InputSystem.cs` | 두 Layer 인터페이스와 InputSystem에 ClearLayer 추가. InputLayer 데이터 규칙은 유지 | **공용**, Phase 2 |
| `ProjectL.Systems.asmdef` | Unity.InputSystem 참조를 DB 코드 작성 전에 추가 | **공용**, Phase 3 |
| `InputSystemGroup.cs` | InitData·Initialize 전달 추가, 역순 Shutdown 유지 | **공용**, Phase 4 |
| `Factory/Runtime/Global/InputSystemFactory.cs`, `GlobalSystemFactory.cs` | Controller·Layer 목록 전달, 등록 순서 보장 | **공용**, Phase 4 |
| `Factory/Runtime/RunTimeSystemFactory.cs`, `ServiceKeys/…` | 슬롯 생성·루트 등록, 두 목록 조립 | **공용**, Phase 4 |
| ViewGroup 입력 설정 테이블·아이템 및 DB 키·쿼리·팩토리 | 액션 에셋 참조 | DB-SO 규칙을 따름, Phase 3 |
| `StageInputTests.cs`, `UnityStageInputTests.cs`, 기존 DB 테스트 | 순수 입력 계약, 실제 Unity 연결, 설정 검증 | EditMode |
| `Assets/Tests/EditMode/Framework/ProjectL.Tests.Framework.asmdef` | Unity 테스트에서 직접 사용하는 Unity.InputSystem 참조 | 테스트 공용, Phase 3 |

## 8. 검증

- **순수 계약(EditMode, `StageInputTests`)**: 실제 InputSystem + 가짜 Layer 런타임으로 라우팅, ClearLayer, 정상 누름·뗌, Canceled의 별도 취소, 포인터 취소 무시, 슬롯의 연결·선택 소유권을 검사한다.
- **Unity 연결(EditMode, `UnityStageInputTests`)**: 실제 InputActionAsset과 가상 Mouse를 사용한다. 프로젝트 에셋의 ReleaseOnly 설정 확인, 초기 비활성 Resume, 누른 채 Layer 전환·ClearLayer·Shutdown, 공통 맵 유지, 전환 중 재진입 요청, 초기화 실패 시 자원 정리를 확인한다. 테스트가 만든 장치·구독·복제 에셋은 매번 정리한다.
- **DB**: 설정 아이템 0개·2개·Actions null 거부와 유효한 1개 허용, 입력 테이블 누락·잘못된 초기화 컨텍스트의 실패를 검사한다.
- **조립**: 실제 InputSystemFactory의 수명 전달과 브리지 선행 Shutdown을 검사한다. 임시 테스트 데이터로 하는 계약 검증과 실제 카탈로그 검증은 별도로 보고한다.
- **기존 DB 선행 장애**: ViewGroup의 누락 3개는 이번 입력 작업에서 임의로 채우지 않는다. 해결 전 실제 카탈로그 검증·전체 런타임 초기화는 미완료다. 단위 테스트 통과를 실제 카탈로그 통과로 보고하지 않는다.
- Unity 연결 계층도 컴파일만으로 완료하지 않는다. 런타임 부트스트랩을 이용한 재생 검증은 별도 후속 작업이다.

## 9. 결정

| 번호 | 질문 | 결정 및 검토 반영 |
|---|---|---|
| **D1** | 브리지가 `InputActionAsset`을 **어디서** 받나 | **ViewGroup DB 입력 설정 테이블**에서 InitData 때 받는다. IHasViewQuery를 사용하며 설정 누락은 오류다. DB의 에셋 참조는 Unity 타입 경계의 명시적 예외다 |
| **D2** | Controller·Layer 목록을 **누가 모아** 넘기나 | FrameworkRuntimeFactory가 두 목록을 만들어 Global → Input 팩토리로 넘긴다. Global은 스테이지 입력의 구체 타입을 조회하지 않는다 |
| **D3** | 액션 이름과 에셋 위치 | 생성 래퍼 대신 **문자열 상수 클래스**를 두고, 검증기가 시작할 때 에셋과 대조한다. 에셋은 `Assets/ProjectL/Input/`에 둔다 |
| **D4** | 배치 ↔ 전투 Layer를 **무엇을 보고** 바꾸나 | **결정(2026-09-30, 사용자): Core에 단계 값을 요청하지 않는다.** 배치 → 전투는 사용자 입력(`EnterCombatRequested` → `EnterCombat` 명령)으로 시작하므로 View가 시점을 안다. Core가 거절할 수 있으므로(예: 메인 캐릭터 미배치) Layer는 입력 시점이 아니라 **그 명령이 수락된 결과를 확인한 뒤** 바꾼다. 반대 방향(밤이 끝나 배치로 돌아가는 경우)은 Core가 진행하므로 결과의 사건 목록(R5)으로 안다. 호출 자리는 전투 View 설계에서 정한다 |
| **D5** | 수신 인터페이스를 하나로 둘지, 배치·전투로 나눌지 | 처음에는 **하나**(`IStageInputReceiver`)로 둔다. 전투 View가 둘 다 받고, 배치와 전투는 같은 필드·포인터를 공유한다 |
| **D6** | 스테이지 내부 입력 파일 위치 | `RuntimeSystem/InGameSystem/ViewSystem/StageInput/`. InGame View 그룹 아래에 모은다 |
| **D7** | Unity 연결 계층의 Global 배치를 민형님께 먼저 알릴지 | 설계가 정해지면 공유 문서를 먼저 보내고 구현한다(Buffer 때와 같다) |

2026-09-30 구조 검토 반영: 비활성화는 ClearLayer, 선택은 ReleaseOnly + 별도 SelectCanceled, 슬롯은 선택 소유권 관리, 조립은 Controller·Layer 동시 전달, 어셈블리 참조는 Phase 3으로 이동, 실제 설정 누락은 실패 처리로 정리했다. D4의 "Core 단계 값을 별도로 요청하지 않는다"는 결정은 유지한다.

## 10. 범위 밖과 다음 단계

- **전투 View 설계(다음):** IStageInputReceiver 구현, 포인터 → 칸 변환, 드래그·호버, UI 위 포인터 차단, CombatViewBuffer 기록, Layer 전환 호출 자리. SelectCanceled의 무확정 정리와 파괴 전 Unbind 규칙을 따른다
- 전투 Flow와 결과 연출: 그 뒤
- 다른 화면 Controller: 해당 화면 작업 때
- 부트스트랩: Framework 쪽 과제. 생기면 재생 확인을 추가한다
- 실제 DB의 기존 테이블 누락 3개 보충은 별도 과제이며, 실제 카탈로그를 사용하는 초기화 검증의 선행 조건이다

## 11. 구현 결과 (2026-09-30)

계획: [스테이지 내부 입력 구현 계획](../plans/2026-09-30-stage-input-plan.md). Phase 1~4는 사용자 지시("너가 진행해줘")로 Claude가 직접 구현했다.

### 11.1 만든 파일

| Phase | 파일 | 내용 |
|---|---|---|
| 0 | `docs/superpowers/handoffs/2026-09-30-stage-input-framework-share.md` | 민형님 공유(공용 파일 변경 목록, 계약) |
| 1 | `Assets/ProjectL/Input/PlayerInputActions.inputactions` | develop-4week 원본과 바이트 단위로 같다. 옛 `.meta`(C# 래퍼 생성 켜짐)는 가져오지 않았다 |
| 1 | `StageInput/StageInputNames.cs`, `StageInputLayers.cs` | 이름 상수, Layer 두 개와 목록 `All` |
| 2 | `View/InputSystem.cs` (공용) | `IInputLayerRuntime`·`IInputLayerService`·`InputSystem`에 `ClearLayer()` |
| 2 | `StageInput/IStageInputReceiver.cs`, `StageInputReceiverSlot.cs` | 수신 계약(메서드 10개), 슬롯(`IStageInputReceiverSlot`, 선택 소유권) |
| 2 | `StageInput/DeploymentInputController.cs`, `CombatInputController.cs`, `StageSelectInput.cs` | Controller 두 개와 공용 포인터·선택 해석 |
| 3 | `ProjectL.Systems.asmdef`, `ProjectL.Tests.Framework.asmdef` (공용) | `Unity.InputSystem` 참조 |
| 3 | `Data/View/Table/InputSettings/…`, `Queries/…/InputSettingsTable.cs`, 키·`IViewQuery`·팩토리 | 입력 설정 View 테이블(설정형) |
| 3 | `DataBaseSO/ViewGroup/InputSettings/…asset` | 테이블과 아이템 `InputSettings_Default`(Actions → `PlayerInputActions`). ViewGroup 등록 |
| 4 | `GlobalSystem/InputSystem/UnityBridge/` 4개 | `UnityInputBridge`, `UnityInputLayerRuntime`, `InputConfigurationValidator`, `InputActionValueReader` |
| 4 | `InputSystemGroup.cs`, `InputSystemFactory.cs`, `GlobalSystemFactory.cs`, `RunTimeSystemFactory.cs` (공용) | InitData·Initialize 전달. `Create(controllers, layers)`. InputSystem → 브리지 등록 순서. 루트에 슬롯 등록 |
| 4 | `ServiceKeys/StageInputServiceKey.cs` | `StageInputKey.ReceiverSlot` |
| 테스트 | `StageInputTests.cs`(18건), `UnityStageInputTests.cs`(14건), `ViewDataTableTests.cs`(+3건) | 순수 계약, 장치 없는 Unity 연결, 입력 설정 테이블 |

### 11.2 설계·계획과 달라진 점

| 항목 | 내용 | 이유 |
|---|---|---|
| `StageSelectInput.cs` 추가 | 두 Controller의 포인터·선택 해석을 한 내부 도우미로 모았다 | 같은 규칙의 중복 제거. 파일 1개 증가 |
| 누른 상태에서 Started(false) | §4.3 표에 없는 조합이다. **취소(`SelectCanceled`)로 처리**했다 | 확정 명령이 잘못 나가지 않는 쪽 |
| DB 폴더명 `Table/InputSettings/` | 계획의 `Table/Input/` 대신 | `…View.Table.Input` 네임스페이스가 `UnityEngine.Input`을 가리는 충돌 회피 |
| 연결 계층 폴더명 `UnityBridge/` | 계획의 `Unity/` 대신(사용자 결정) | `…InputSystem.Unity` 네임스페이스가 `Unity.*`를 가리는 충돌 회피 |
| 입력 설정 에셋 | 에디터 작업(3.6) 대신 Claude가 YAML로 직접 작성(사용자 요청). 액션 참조 fileID `-944628639613478452` | — |
| 가상 장치 테스트 | 4.9 중 가상 Mouse가 필요한 항목은 **보류(사용자 결정 B)** | EditMode에서 실제 입력 혼입·갱신 시점 문제. `InputTestFixture`는 manifest `testables` 변경이 필요 |
| 기존 테스트 | 수정하지 않았다 | Global·Input 팩토리를 직접 부르는 테스트가 없고, 루트 조립 테스트는 `InitData`를 부르지 않는다 |

### 11.3 함께 한 작업 (다른 계획 소속)

- 사용자 요청으로 ViewGroup에 빠져 있던 `TraitVisual`(5)·`RecoveryOptionVisual`(3)·`RailwayVisual`(1) 테이블과 아이템을 채웠다.
- ViewGroup 테이블은 10개로 팩토리 요구 수와 같다.
- 이 작업은 [DB 스키마 정렬 계획](../plans/2026-09-29-db-schema-core-alignment-plan.md) Phase C T12의 일부이며, 그 계획의 실행 기록에 적었다.

### 11.4 검증 상태

| 항목 | 상태 |
|---|---|
| 에디터 컴파일 | Phase 1~4 모두 사용자 확인(에러 0, 2026-09-30) |
| `StageInputTests`, `UnityStageInputTests`, `ViewDataTableTests` | **실행 결과를 받지 않았다** |
| `DataBaseAssetTests` | 실패가 예상된다. InGameGroup 테이블 3개 누락(DB 스키마 정렬 계획 T11)이 남아 있다. 입력 작업의 결함이 아니다 |
| 재생 확인 | 부트스트랩이 없어 하지 않았다 |

### 11.5 남은 일

- **전투 View 설계(다음)**: §10 참고
- 가상 장치 테스트(보류 B): 부트스트랩이 생긴 뒤 에디터에서 누름·뗌, 누른 채 Layer 전환, 종료 시 취소를 직접 확인한다
- 런타임 부트스트랩(Framework 쪽 과제)
- 실제 카탈로그 통과: DB 스키마 정렬 계획 T11(InGame 테이블 3개)과 T12 나머지
- 프로젝트 전역 액션(`ProjectSettings`의 `InputSystem_Actions`)이 재생 때 자동으로 켜진다. 비우거나 `PlayerInputActions`로 바꿀지 정한다
- Framework 문서(`framework-programmer-guide.md`)의 Input 계약에는 `ClearLayer`가 아직 없다. 민형님 문서라 공유만 했다
- 민형님께는 Phase 0 공유 문서만 보냈다. 반영 결과(폴더명 `UnityBridge`, 추가 파일)는 필요하면 후속 문서로 알린다
