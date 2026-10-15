# 스테이지 내부 입력 — Framework InputSystem 연결과 공용 파일 변경 공유

- 작성일: 2026-09-30
- 작성: View 작업(스테이지 내부 담당)
- 전달 대상: Framework 작업자(민형님)
- 목적: Framework `InputSystem` 위에 Unity 입력 연결 계층과 스테이지 내부 입력을 만들면서 바뀌는 공용 파일과 계약을 **코드 전에** 공유한다.
- 상태: 변경 예정. 코드는 아직 바꾸지 않았다.
  - `IInputLayerService`/`IInputLayerRuntime`에 `ClearLayer()`를 추가하는 것은 사용자를 통해 확인받았다(2026-09-30).
  - 나머지 항목도 의견이 있으면 해당 Phase 전에 반영한다.

관련 문서

- [스테이지 내부 입력 설계](../specs/2026-09-30-stage-input-design.md)
- [구현 계획](../plans/2026-09-30-stage-input-plan.md)
- 가이드와 맞춘 부분: [framework-programmer-guide.md](../../../Assets/ProjectL/Script/Systems/Documentation/framework-programmer-guide.md)의 Input 계약(Configure 한 번, Controller 확장, `IInputLayerRuntime` 주입)

## 1. 무엇을 만드나

지금 `InputSystem`에는 입력을 Layer와 경로표로 나눠 주는 가운데 단계만 있다. 장치에서 입력을 모으는 쪽, Layer를 저장하는 쪽, `Configure`·`Submit`을 부르는 쪽이 없다(가이드: "기본 장치 수집·Layer 저장까지 자동 제공되는 것은 아니다"). 이번에 두 층을 만든다.

```text
┌ Global / InputSystem ─────────────────────────────────────────────┐
│ InputSystem (기존)  ◄─ Submit ─  UnityInputBridge (신규)            │
│                     ◄─ Configure(한 번)                             │
│                       UnityInputLayerRuntime : IInputLayerRuntime  │
│                       (Layer → ActionMap 켜기·끄기)                 │
└────────────────────────────────────────────────────────────────────┘
        ▲ Controller 목록 + Layer 목록 (루트 조립에서 전달)
┌ InGame/ViewSystem/StageInput (신규) ───────────────────────────────┐
│ DeploymentInputController, CombatInputController (InputController) │
│   → StageInputReceiverSlot → (다음 작업) 전투 View                   │
└────────────────────────────────────────────────────────────────────┘
```

- 새 Unity 연결 파일은 `Systems/RuntimeSystem/GlobalSystem/InputSystem/Unity/` 아래에만 둔다.
  - `UnityInputBridge`
  - `UnityInputLayerRuntime`
  - `InputActionValueReader`
  - `InputConfigurationValidator`
- 옛 `develop-4week`의 `Unity/Adapter/Input/` 구현을 새 구조에 맞춰 옮긴다.
- 액션 에셋은 옛 `PlayerInputActions.inputactions`를 `Assets/ProjectL/Input/`로 이식한다. C# 래퍼 생성은 끄고 이름 상수 클래스를 쓴다.

## 2. 공용 파일 변경

| 파일 | 변경 | Phase |
|---|---|---|
| `GlobalSystem/InputSystem/View/InputSystem.cs` | `IInputLayerService`·`IInputLayerRuntime`·`InputSystem`에 `void ClearLayer()` 추가. `InputSystem.ClearLayer()`는 `ChangeLayer`와 같은 수명 검사(Configure 전·Shutdown 후 거부)를 하고 런타임에 위임한다. **`InputLayer` 자체의 규칙은 바꾸지 않는다**(`ChangeLayer(default)`, 맵 0개 Layer는 계속 예외) | 2 |
| `Systems/ProjectL.Systems.asmdef` | `"Unity.InputSystem"` 참조 추가 | 3 |
| `Assets/Tests/EditMode/Framework/ProjectL.Tests.Framework.asmdef` | 같은 참조 추가(Unity 입력 타입을 직접 쓰는 테스트용). 새 테스트 어셈블리는 만들지 않는다 | 3 |
| ViewGroup DB: `ViewTableKey`, `IViewQuery`/`ViewDataBase`, `ViewDataBaseFactory` | 입력 설정 테이블 한 개 추가. `RailwayVisual`처럼 아이템 정확히 1개인 설정형이다 | 3 |
| `GlobalSystem/InputSystem/InputSystemGroup.cs` | `ISystemInitData`, `ISystemInitialize`를 구현해 하위 모듈에 넘긴다(`JobSystemGroup`과 같은 방식). Shutdown의 역순 전달은 그대로 | 4 |
| `Factory/Runtime/Global/InputSystemFactory.cs` | `Create(IReadOnlyList<IInputController> controllers, IReadOnlyList<InputLayer> layers)`. **InputSystem → UnityInputBridge 순서로 등록**한다 | 4 |
| `Factory/Runtime/Global/GlobalSystemFactory.cs` | `Create(controllers, layers)`로 받아 `InputSystemFactory`에 넘긴다 | 4 |
| `Factory/Runtime/RunTimeSystemFactory.cs` | 스테이지 입력(슬롯, Controller 두 개, Layer 두 개)을 만든다. 슬롯은 루트 서비스로 등록하고, 두 목록은 Global에 넘긴다. `FrameworkRuntimeFactory.Create()`의 시그니처는 그대로다 | 4 |
| `ServiceKeys/…` | 수신 슬롯 키 한 개(`StageInputKey.ReceiverSlot`) | 4 |

## 3. 지켜야 할 계약

**Configure는 한 번, 위치는 `InitData`**

- 브리지는 `ISystemInitData<IHasViewQuery>`에서 입력 설정 테이블의 `InputActionAsset`을 받는다.
- 그다음 에셋 복제 → 설정 검증 → Layer 런타임 생성 → `Configure` 순서다.
- `BindScene`은 씬마다 불리므로 쓰지 않는다.
- `InputSystem`은 브리지 생성자에서 받는다. `PostBindService` 순서에 기대지 않기 위해서다.

**Controller·Layer 목록은 루트가 넘긴다**

- `InputConfiguration`은 바인딩만 가지고 있어서, Common을 함께 켜는 Layer 구성을 추론할 수 없다.
- 그래서 `FrameworkRuntimeFactory`가 두 목록을 만들어 Global → Input 팩토리로 넘긴다. Global은 스테이지 입력의 구체 타입을 조회하지 않는다.
- 브리지는 시작할 때 목록을 검증한다.
  - 거부: null, 빈 목록, Layer 이름 중복, 에셋에 없는 맵·액션, 값 종류 불일치, 어느 Layer에서도 켤 수 없는 Controller 맵
  - 허용: 등록된 Layer에 Controller가 없는 맵. 예를 들어 `Common.OpenSettings`는 설정 화면 Controller가 범위 밖이라 이번엔 처리되지 않는다.

**비활성 시작과 `ClearLayer`**

- Layer 런타임은 Layer가 없는 상태(`CurrentLayer == default`, 모든 맵 꺼짐)로 시작한다.
- 전체 입력 해제는 `ClearLayer()`로 한다. 가짜 이름이나 가짜 맵을 가진 "비활성 Layer"는 등록하지 않는다.
- Layer 전환 순서
  1. 이전 Layer를 유지한 채, 빠지는 맵을 끈다
  2. `CurrentLayer`를 바꾼다
  3. 새 맵을 켠다

  Common처럼 계속 켜져 있는 맵은 다시 켜지 않는다. 전환 중 콜백에서 들어온 요청은 전환이 끝난 뒤 마지막 것만 적용한다.

**선택 입력과 취소**

- `Deployment.Press`·`Combat.Select`는 기존 에셋의 `Press(behavior=1)`(ReleaseOnly), `initialStateCheck=false`를 유지한다.
- Controller는 정상 뗌(Performed)과 강제 취소(Canceled)를 나눈다. 맵을 끌 때, Layer를 바꿀 때, Shutdown할 때 오는 Canceled는 **선택을 확정하지 않고 정리만** 한다.
- 수신 슬롯은 누름을 받은 View를 기억한다. View가 바뀌어도 옛 누름의 뗌이 새 View로 넘어가지 않는다.

**종료 순서**

- `InputSystemGroup`은 등록 역순으로 Shutdown한다. 그래서 브리지가 InputSystem보다 먼저 종료된다.
- 브리지 종료 순서: 맵 끄기 → 마지막 Canceled를 라우터에 전달 → 구독 해제 → 복제 에셋 파괴
- InitData 전 Shutdown과 반복 Shutdown도 안전하게 만든다.

**입력 설정이 없으면 실패**

- 정상 조립에서 ViewQuery·입력 테이블·설정·에셋 중 하나라도 없으면 초기화 예외로 멈춘다. 조용히 입력을 끈 채 성공하지 않는다.
- 입력이 필요 없는 테스트는 입력 그룹을 조립하지 않는다. 운영 팩토리에 "무입력" 분기나 빈 Controller 허용을 넣지 않는다.

## 4. 영향 확인

- `FrameworkRuntimeFactory.Create()` 시그니처는 그대로다.
- 기존 `FrameworkRuntimeCompositionTests`는 `InitData`를 부르지 않으므로 필수 입력 설정 정책의 영향을 받지 않는다.
- 브리지가 InitData 전 Shutdown에 안전해야 한다는 조건은 이 테스트들 때문에도 필요하다.
- `GlobalSystemFactory`·`InputSystemFactory`를 직접 부르는 곳은 현재 운영 코드의 `RunTimeSystemFactory`·`GlobalSystemFactory`뿐이다.

## 5. 알려 드릴 기존 문제 (이번 작업에서 고치지 않음)

- **실제 `ViewGroup.asset`의 테이블이 6개뿐이다.** 팩토리는 9개를 요구하며 Trait·RecoveryOptionVisual·Railway가 빠져 있다.
  - 그래서 실제 카탈로그로 DB를 만드는 경로는 입력 작업과 상관없이 먼저 실패할 가능성이 높다.
  - 입력 설정 테이블 에셋을 추가해도 이 문제는 남는다. 별도 과제로 남긴다.
- **런타임 부트스트랩이 없다.** 그래서 이번 작업은 컴파일과 EditMode 테스트로 확인하고, 재생 확인은 부트스트랩이 생긴 뒤에 한다.
