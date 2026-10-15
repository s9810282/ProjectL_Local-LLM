# 스테이지 내부 입력 구현 계획

- 작성일: 2026-09-30
- 설계: [스테이지 내부 입력 설계](../specs/2026-09-30-stage-input-design.md)
- 상태: **수정안(2차, 2026-09-30 구조 검토 반영).** 기존 D1~D7을 유지한다. 비활성 Layer, 강제 취소, 선택 소유권, Layer 목록 전달, 의존성 순서, 설정 누락 정책을 보완했다. 문서 수정만 완료했으며 구현은 아직 시작하지 않았다.
- 진행 방식: Phase가 끝날 때마다 멈추고 보고한다. 실습 워크시트로 할지, 직접 구현할지는 Phase를 시작할 때 정한다.

## 전역 제약

- 순수 C# 파일(Controller, 수신 슬롯, 이름 상수, Layer)에는 `using UnityEngine`을 쓰지 않는다(설계 I2·I6).
- 실행 시 Unity 입력 타입(`InputActionAsset`, `InputAction`, `Vector2`)은 `GlobalSystem/InputSystem/Unity/`에 둔다. 명시적 예외는 View DB의 `InputSettingsData.Actions : InputActionAsset` 참조와 EditMode 테스트다. Controller·슬롯·Layer·수신 계약은 순수 C#이다.
- 전체 비활성화는 `ClearLayer()`를 사용한다. `ChangeLayer(default)`나 빈 맵을 가진 이름 있는 Layer는 허용하지 않는다.
- 선택 액션은 Button + `Press(behavior=1)`(ReleaseOnly), `initialStateCheck=false`로 고정한다. 정상 뗌은 `SelectReleased`, 강제 취소는 `SelectCanceled`로 구분한다. "Phase/Interaction과 무관하게 false는 모두 뗌"이라는 옛 규칙은 사용하지 않는다.
- 운영 입력 설정이 없으면 초기화 실패로 처리한다. 입력 없는 테스트는 해당 그룹을 조립하지 않는다. 테스트 때문에 운영 초기화를 조용히 건너뛰는 분기를 넣지 않는다.
- 옛 코드 출처는 `origin/develop-4week:Assets/Scripts/ProjectL/Unity/Adapter/Input/`다.
  - 가져올 때 네임스페이스와 Framework 타입 이름(`ProjectL.Framework.Systems.InputSystem` → `ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.*`)을 새 구조에 맞춘다.
  - 옛 기능 중 이번 범위 밖(StageSelection·Artifact·Settings 보완 코드)은 가져오지 않는다.
- 코드 형식은 주변 코드를 따른다: `#region 서비스` / `#region 시스템 모듈`, `sealed`, Allman 중괄호, 한국어 XML doc.
- Framework 공용 파일은 필요한 줄만 바꾸고, Phase 0 공유 문서에 적은 범위를 넘지 않는다.
- `.cs`와 에셋의 `.meta`는 에디터가 만든다. 커밋할 때 함께 넣는다.

## 확인한 사실 (계획에 영향)

| 사실 | 영향 |
|---|---|
| `ProjectL.Systems.asmdef`에 `Unity.InputSystem` 참조가 없다 | **Phase 3.0에서 DB 코드를 쓰기 전에** 참조를 추가한다. Unity 타입을 직접 쓰는 테스트 asmdef에도 추가한다 |
| `InputSystemGroup`은 `ISystemShutdown`만 구현한다. `InitData`·`Initialize`를 하위 모듈에 넘기지 않는다 | 두 수명을 하위 모듈에 전달한다. Shutdown은 기존 역순을 유지한다 |
| `InitData`는 한 번이며 Initialize 전에 완료해야 한다. BindScene·InitData·PostBindService 사이 상대 순서는 Framework가 강제하지 않는다 | 브리지는 생성자에서 라우터를 받고 InitData에서 Configure한다. PostBindService 순서에 의존하지 않는다 |
| `ChangeLayer(default)`와 빈 맵 Layer 생성은 거부된다 | Phase 2에서 서비스·런타임 계약에 ClearLayer를 추가한다. 초기 비활성 처리는 옛 구현을 수정해야 한다 |
| 옛 선택 액션은 ReleaseOnly다. 맵 Disable도 Canceled를 일으키며, 기존 Vector2 편의 바인딩은 Canceled 좌표도 전달한다 | 선택·포인터는 Raw로 받아 정상 뗌/강제 취소를 구분하고 포인터 Canceled의 (0, 0)은 버린다 |
| 형식 지정 `ISystemInitData<T>`는 `(T)service`로 전달한다 | 브리지는 IHasViewQuery를 요구한다. 다른 컨텍스트를 조용히 허용하지 않는다 |
| View 쪽 설정형 테이블 선례: `RailwayVisual`(아이템 정확히 1개, `Config` 속성, `Validate`) | 입력 설정 테이블도 같은 모양으로 만든다 |
| 실제 `ViewGroup.asset`에 테이블이 **6개**만 있다. 팩토리는 9개를 요구하며 Trait·RecoveryOptionVisual·Railway가 없다 | 실제 DB 조립의 기존 선행 장애다. 입력 테이블 추가로 해결되지 않는다. Phase 3에서 실행 결과를 확인·보고하며, 별도 보충 전 실제 카탈로그 및 전체 런타임 초기화는 검증 미완료로 남긴다 |
| 루트 서비스는 `resolver.GetService(key)`로 조회된다 | 수신 슬롯은 루트에 직접 등록하면 된다. InGame 팩토리의 시그니처를 바꾸지 않아도 된다(D2) |
| `InputConfiguration`은 바인딩만 가지며 Controller는 ActionMap 하나만 노출한다 | Common을 포함한 Layer 구성을 추론할 수 없다. 루트가 Controller·Layer 목록을 함께 전달한다 |

## Phase 0 — Framework 공유 (코드 전)

| Task | 내용 | 완료 조건 |
|---|---|---|
| 0.1 ✅ | 작성 완료(2026-09-30). `ClearLayer` 추가는 사용자를 통해 민형님 확인. 새 문서 `docs/superpowers/handoffs/2026-09-30-stage-input-framework-share.md`: Global 연결 계층 위치, 아래 공용 변경 목록, InitData의 Configure 한 번, Controller·Layer 전달 경로, ClearLayer 추가, 취소·선택 소유권, 필수 DB 정책, 브리지 선행 Shutdown | 사용자에게 전달(보내는 건 사용자) |

공용 파일 변경 목록 (Phase 2·3·4)

| 파일 | 변경 |
|---|---|
| `GlobalSystem/InputSystem/View/InputSystem.cs` | IInputLayerService·IInputLayerRuntime·InputSystem에 ClearLayer 추가(Phase 2). InputLayer 자체는 변경하지 않는다 |
| `Systems/ProjectL.Systems.asmdef` | Unity.InputSystem 참조 추가(Phase 3.0) |
| `Assets/Tests/EditMode/Framework/ProjectL.Tests.Framework.asmdef` | DB·Unity 연결 테스트에서 직접 쓰는 Unity.InputSystem 참조 추가(Phase 3.0) |
| `GlobalSystem/InputSystem/InputSystemGroup.cs` | `ISystemInitData`, `ISystemInitialize` 구현을 추가해 하위 모듈에 넘긴다 |
| `Factory/Runtime/Global/InputSystemFactory.cs` | `Create(IReadOnlyList<IInputController> controllers, IReadOnlyList<InputLayer> layers)`. InputSystem 다음에 브리지를 등록한다 |
| `Factory/Runtime/Global/GlobalSystemFactory.cs` | Create(controllers, layers)로 받아 InputSystemFactory에 넘긴다 |
| `Factory/Runtime/RunTimeSystemFactory.cs` | 슬롯·Controller·Layer 목록을 만들고, 슬롯을 루트에 등록하고, 두 목록을 Global에 넘긴다 |
| `ServiceKeys/…` | 수신 슬롯 키 한 개 |
| DB: `ViewTableKey`, `IViewQuery`/`ViewDataBase`, `ViewDataBaseFactory` | 입력 설정 테이블 한 개 추가(Phase 3) |

**보고 지점:** 공유 문서.

## Phase 1 — 액션 에셋과 이름

| Task | 파일 | 내용 | 완료 조건 |
|---|---|---|---|
| 1.1 | `Assets/ProjectL/Input/PlayerInputActions.inputactions` | 옛 에셋을 git show로 가져오되 옛 .meta는 가져오지 않는다. Generate C# Class가 꺼져 있는지 확인한다. Deployment.Press·Combat.Select의 Button, Press(behavior=1), initialStateCheck=false, 개별 바인딩 Interaction 없음도 확인한다 | import 에러 없음, 선택 계약 일치 |
| 1.2 | `InGameSystem/ViewSystem/StageInput/StageInputNames.cs` | ActionMap·Action 이름 상수. `Deployment`: PointerPosition·Press·Cancel·EnterCombat·DrawCharacter / `Combat`: PointerPosition·Select·Cancel·EndTurn·MoveMode·AttackMode / `Common`: OpenSettings | 에셋의 이름과 글자까지 같음 |
| 1.3 | `…/StageInput/StageInputLayers.cs` | Deployment(Common, Deployment), Combat(Common, Combat) 및 루트가 전달할 읽기 전용 목록. 비활성 Layer를 별도 등록하지 않는다 | 컴파일 |

**진행(2026-09-30):**
- 1.1~1.3 파일을 작성했다. 에셋은 `develop-4week` 원본과 바이트 단위로 같다.
- 옛 `.meta`(`generateWrapperCode: 1`)는 가져오지 않았다.
- 에디터 import와 Inspector 확인은 사용자가 한다.

**보고 지점:** 에셋의 맵·액션·선택 Interaction 설정과 코드 정의를 대조한 표. Common.OpenSettings는 이번 범위에 수신 Controller가 없어 처리되지 않음을 기록한다.

## Phase 2 — 수신 인터페이스와 Controller (순수 C#)

| Task | 파일 | 내용 | 완료 조건 |
|---|---|---|---|
| 2.0 | `GlobalSystem/InputSystem/View/InputSystem.cs` | 두 Layer 계약과 InputSystem에 ClearLayer 추가. Configure 전·Shutdown 후에는 ChangeLayer와 같은 수명 검사. 기존 런타임 구현·가짜 구현이 있으면 새 메서드를 맞춘다 | 컴파일, 기존 ChangeLayer(default) 거부 유지 |
| 2.1 | `…/StageInput/IStageInputReceiver.cs` | 설계 §4.3의 메서드 10개. SelectCanceled는 제스처 정리만 하며 선택을 확정하지 않는다 | 컴파일 |
| 2.2 | `…/StageInput/StageInputReceiverSlot.cs` | 서비스 계약 Bind/Unbind와 수신 전달 구현. 누름 콜백 전에 선택 소유자를 기록한다. 완료·취소·일치하는 Unbind는 소유자를 먼저 지우고 전달한다. Unbind는 연결도 먼저 지운 뒤 옛 소유자에게 SelectCanceled 한 번. null/중복 Bind 거부, 다른 대상 Unbind 무시 | 설계 §4.4·I3 |
| 2.3 | `…/StageInput/DeploymentInputController.cs` | 포인터는 Raw Vector2의 Performed만 반영. Press는 Raw 버튼으로 누름 중복 제거, Performed의 true→false만 정상 뗌, Canceled는 별도 취소. 내부 상태를 콜백 전에 갱신한다. Cancel·EnterCombat·DrawCharacter는 Press | 설계 §4.3·I5 |
| 2.4 | `…/StageInput/CombatInputController.cs` | 2.3과 같은 규칙. Select, Cancel, EndTurn, MoveMode, AttackMode 바인딩 | 설계 §4.3·I5 |
| 2.5 | `Assets/Tests/EditMode/Framework/StageInputTests.cs` | 실제 `InputSystem`에 가짜 `IInputLayerRuntime`을 주고 `InputConfiguration.Create(두 Controller)`로 Configure한다. 기록용 수신 대상으로 아래 목록을 확인한다 | EditMode 통과 |

2.5에서 확인할 항목(두 Controller에 공통 적용)

- 초기 비활성 상태와 ClearLayer 이후에는 입력이 버려지고, 유효 Layer를 다시 설정하면 전달된다. ChangeLayer(default)는 여전히 예외다.
- Deployment Layer는 Combat 입력을 차단한다. ClearLayer의 Configure 전·Shutdown 후 호출도 거부된다.
- 포인터 이동 뒤 Started(true) → Performed(false)는 같은 좌표의 누름·정상 뗌 한 쌍이다. true 반복은 누름 한 번이며, 누름 없는 false는 전달하지 않는다.
- 누른 뒤 Canceled(false)는 SelectCanceled 한 번, SelectReleased 0번이다. 정상 뗌 뒤 Canceled가 추가되어도 중복 완료·취소가 없다.
- 포인터 Canceled가 (0, 0)이어도 마지막 좌표와 PointerMoved 호출 수를 바꾸지 않는다.
- 슬롯이 비어 있으면 입력을 버린다. null/중복 Bind는 예외이며 다른 수신자의 Unbind는 무시한다.
- A에서 누름 → A Unbind → B Bind → 뗌: A에 취소 한 번, B에 뗌 0번. 같은 A 객체를 다시 Bind해도 옛 뗌은 버린다.
- 빈 슬롯에서 누름 → B Bind → 반복 true → 뗌: B에 누름·뗌 0번. 다음 새 누름·뗌은 정상 전달된다.
- 누름 콜백 안에서 Unbind하면 취소 한 번으로 정리된다. Unbind나 뗌/취소 콜백에서 재진입해 연결을 바꿔도 새 연결·선택 소유권을 덮어쓰지 않는다.
- Cancel·EnterCombat·DrawCharacter·EndTurn·MoveMode·AttackMode는 각 Performed에서 해당 요청만 보낸다.

**진행(2026-09-30):**
- 2.0~2.5를 작성했다.
- 계획 외 파일이 하나 있다: `StageSelectInput.cs`. 두 Controller가 함께 쓰는 포인터·선택 해석을 한곳에 모은 내부 도우미다.
- 테스트는 12개 메서드(TestCase 포함 18건)이며, EditMode 실행은 사용자가 한다.

**보고 지점:** 테스트 결과와 수신 인터페이스 모양.

## Phase 3 — 입력 설정 View 테이블 (D1)

`RailwayVisual`의 구조를 그대로 따른다.

| Task | 파일 | 내용 |
|---|---|---|
| 3.0 | `Systems/ProjectL.Systems.asmdef`, `Assets/Tests/EditMode/Framework/ProjectL.Tests.Framework.asmdef` | Unity.InputSystem 참조를 먼저 추가한다. Phase 4까지 미루면 아래 DB 코드가 컴파일되지 않는다 |
| 3.1 | `DataBaseSystem/Data/View/Table/Input/Item/InputSettingsDataSO.cs` | InputSettingsData : IData의 InputActionAsset Actions와 Validate, InputSettingsDataSO : DatabaseItem. DB의 직렬화 에셋 참조는 Unity 타입 경계의 예외다(P5) |
| 3.2 | `…/Table/Input/InputSettingsDataTableSO.cs` | `[CreateAssetMenu(… "ProjectL/Database/View/InputSettingsDataTable")]` |
| 3.3 | `Queries/ViewDataBase/ViewTable/InputSettingsTable.cs` | `IInputSettingsTableQuery { InputSettingsData Config }`. 아이템이 정확히 1개인지와 `Validate`(에셋 null 거절)를 확인한다 |
| 3.4 | `ViewTableKey`, `IViewQuery`/`ViewDataBase`, `ViewDataBaseFactory` | 입력 테이블 등록·조회 추가. 기존 IViewQuery 가짜 구현이 있으면 새 속성을 맞춘다 |
| 3.5 | `Assets/Tests/EditMode/Framework/ViewDataTableTests.cs` | 아이템 0개·2개·Actions null 거부, 유효한 1개 허용. 실패 원인이 개수인지 에셋 null인지 구분되게 유효한 테스트 에셋을 준비한다 |
| 3.6 | **에디터 작업(사용자)** | 아래 목록 |

3.6 에디터 작업

- `Assets/ProjectL/DataBaseSO/ViewGroup/InputSettings/`에 테이블 에셋과 아이템 에셋을 만든다.
- 아이템에 `PlayerInputActions`를 연결하고, 테이블에 아이템을 넣고, `ViewGroup.asset`의 테이블 목록에 추가한다.
- DataBaseAssetTests를 실행해 결과를 확인한다. 기존 Trait·RecoveryOptionVisual·Railway 누락은 별도 문제로 보고하고 이번 범위에서 임의로 고치지 않는다.
- 기존 누락이 남아 있으면 **실제 카탈로그 통과·전체 런타임 초기화는 미완료**로 표시한다. 후속 Phase의 독립된 테스트 데이터 검증은 진행할 수 있으나 이를 카탈로그 성공으로 대신하지 않는다.

**진행(2026-09-30):**
- 3.0~3.5를 작성했다.
- 계획과 다른 점이 둘 있다.
  - 폴더·네임스페이스를 `Table/Input/`이 아니라 **`Table/InputSettings/`**로 했다. `…View.Table.Input` 네임스페이스가 `UnityEngine.Input`을 가리는 이름 충돌을 피하기 위해서다(CLAUDE.md의 `Enum` 충돌과 같은 종류).
  - 테스트 도우미 이름은 `InputSettingsItem`으로 했다. `UnityEngine.InputSystem.InputSettings` 클래스와 헷갈리지 않게 하기 위해서다.
- 3.6 에디터 작업과 테스트 실행은 사용자가 한다.
- 3.6 에셋은 사용자 요청("예시로 하나 만들어 보기")에 따라 Claude가 YAML로 직접 작성했다.
  - `DataBaseSO/ViewGroup/InputSettings/InputSettingsDataTable.asset`
  - `…/Item/InputSettings_Default.asset`: Actions → `PlayerInputActions`, fileID `-944628639613478452`
  - `ViewGroup.asset`에 등록해 테이블이 7개가 됐다.
  - 에디터 Inspector 확인과 테스트 실행은 사용자가 한다.

**보고 지점:** 테스트 결과와 에디터 작업 결과.

## Phase 4 — Unity 연결 계층과 조립

| Task | 파일 | 내용 |
|---|---|---|
| 4.1 | 조립 계약 확인 | 두 목록 전달, 초기 비활성, ReleaseOnly, 필수 입력 설정, 모듈 등록·종료 순서가 Phase 0 공유 내용 및 설계와 같은지 확인한다. 어셈블리 참조는 Phase 3.0에서 이미 추가한다 |
| 4.2 | `GlobalSystem/InputSystem/Unity/InputActionValueReader.cs` | 옛 코드를 가져온다(값 종류 판정, Phase 변환, Canceled면 0/false) |
| 4.3 | `…/Unity/UnityInputLayerRuntime.cs` | 옛 Registry·State를 합치되 default의 내부 맵 조회는 빈 배열로 처리한다. ChangeLayer·ClearLayer, 비활성 Resume/Suspend, 공통 맵 유지, 전환 중 마지막 요청 보류를 구현한다. 맵 Disable 콜백 동안 이전 CurrentLayer를 유지한다 |
| 4.4 | `…/Unity/InputConfigurationValidator.cs` | 옛 검증을 수정한다. 등록 Layer·바인딩·맵/액션·값 종류·사용 가능한 바인딩·도달 가능성을 검사한다. 초기 Layer가 유효해야 한다는 옛 인자·검사는 제거한다. 비활성 시작과 유효한 등록 목록을 구분한다 |
| 4.5 | `…/Unity/UnityInputBridge.cs` | 생성자에서 라우터와 Controller·Layer 목록을 받아 복사한다. ISystemInitData<IHasViewQuery>에서 필수 설정을 조회하고 복제 → 검증 → 값 종류 캐시·Layer 런타임 생성 → Configure 한 번. Initialize는 구독 후 Resume. Shutdown은 Suspend → 구독 해제 → 복제 에셋 파괴. 초기화 실패·초기화 전 종료·반복 종료도 자원을 남기지 않는다 |
| 4.6 | `GlobalSystem/InputSystem/InputSystemGroup.cs` | `ISystemInitData`, `ISystemInitialize` 넘기기 추가(`JobSystemGroup`과 같은 방식) |
| 4.7 | `Factory/Runtime/Global/InputSystemFactory.cs`, `GlobalSystemFactory.cs` | Create(controllers, layers)로 변경. InputSystem → 브리지 순서로 등록하여 역순 종료 시 마지막 Canceled가 라우터에 도달하게 한다 |
| 4.8 | `Factory/Runtime/RunTimeSystemFactory.cs`, `ServiceKeys` | 슬롯·두 Controller·두 Layer 목록 생성. StageInputKey.ReceiverSlot로 슬롯을 루트에 등록하고 두 목록을 Global에 넘긴다 |
| 4.9 | `Assets/Tests/EditMode/Framework/UnityStageInputTests.cs` 및 관련 기존 테스트 | 실제 에셋·가상 장치로 아래 Unity 연결 및 조립 계약을 검사한다. 기존 팩토리 호출부의 시그니처와 가짜 Layer 런타임의 ClearLayer도 맞춘다 |

- **입력 설정 누락 정책은 설계 §6.1을 따른다.** 정상 입력 그룹은 IHasViewQuery와 유효한 입력 설정을 요구한다. 다른 컨텍스트, ViewQuery/Config/Actions null은 실패다. DB 테이블 누락은 DB 생성 단계에서 실패한다.
- 입력 없는 테스트는 필요한 Core·Job 그룹만 조립한다. Global/입력 초기화 테스트에는 유효한 테스트 DB를 제공한다. 무입력 플래그·빈 Controller 허용·예외 삼키기로 운영 계약을 약하게 만들지 않는다.
- Unity 연결 계층도 **컴파일 + EditMode 테스트**로 확인한다. 부트스트랩을 사용하는 재생 확인은 후속 과제다.

4.9에서 확인할 항목

- 프로젝트의 실제 액션 에셋에서 선택 버튼의 ReleaseOnly·initialStateCheck=false 및 바인딩 Interaction 계약을 검사한다. 복제 에셋과 가상 Mouse로 정상 누름·뗌이 한 쌍만 전달되는지 확인한다.
- Layer가 없는 상태에서 Configure → Initialize/Resume해도 예외가 없고 모든 맵이 꺼져 있다. Layer 설정 → ClearLayer 반복 → 재설정이 동작한다.
- 누른 채 Deployment → Combat 또는 ClearLayer를 호출하면 이전 수신자에 SelectCanceled 한 번, SelectReleased 0번이다. 포인터 취소가 (0, 0) 이동으로 전달되지 않는다.
- 버튼을 계속 누른 상태로 다른 Layer를 켜도 새 선택을 시작하지 않는다. 물리적으로 뗀 뒤 다시 누르면 새 선택이 정상 동작한다.
- 두 Layer에 공통인 Common 맵은 전환 때 Disable/Enable하지 않는다. 취소 콜백 안의 ChangeLayer/ClearLayer 요청은 전환 뒤 마지막 요청을 적용한다.
- 실제 InputSystemFactory로 조립한 그룹을 누른 상태에서 Shutdown하면 취소가 라우터 종료 전에 전달된다. 반복 Shutdown은 중복 취소를 만들지 않고 구독·복제 에셋을 남기지 않는다. 비활성 상태에서 바로 Shutdown해도 안전하다.
- 검증 실패 시 복제 에셋을 정리하며 원본을 변경하지 않는다. InitData 전 Shutdown, 잘못된 컨텍스트·설정 누락도 각각 기대한 종료/실패를 보인다.
- null·빈 등록 목록·null Controller·Layer 중복·잘못된 맵·액션·값 종류·도달 불가능한 Controller 맵은 거부한다. 등록된 이름과 맵 정의가 다른 ChangeLayer도 거부한다. InputSystem 재Configure 거부 계약은 유지한다.
- 테스트가 만든 장치·구독·에셋을 TearDown/finally에서 정리한다. 테스트에 Unity.InputSystem의 장치 생성·이벤트 큐·Update API를 사용하며, 실제 프로젝트 에셋은 복제해서 쓴다.

**진행(2026-09-30):**
- 폴더는 사용자 결정으로 `GlobalSystem/InputSystem/UnityBridge/`로 했다. 네임스페이스는 `…InputSystem.UnityBridge`다.
- 4.2~4.8을 작성했다. 수신 슬롯 키는 새 파일 `ServiceKeys/StageInputServiceKey.cs`에 두었다.
- 4.9는 **장치 없이 검사할 수 있는 항목만** `UnityStageInputTests`(14건)로 작성했다.
  - 실제 에셋의 ReleaseOnly 계약
  - 설정 검증
  - Layer 전환·ClearLayer·Suspend
  - Common 유지: `InputSystem.onActionChange`로 관찰
  - 전환 중 보류 요청
  - 브리지 수명·실패 정리·잘못된 컨텍스트
  - 팩토리 수명 전달
- **가상 Mouse가 필요한 항목은 보류했다**(사용자 결정 대기).
  - 누름·뗌 한 쌍
  - 누른 채 Layer 전환·ClearLayer·Shutdown 시 취소
  - 누른 채 켠 Layer에서 새 선택이 시작되지 않는지
  - 브리지 선행 종료의 Canceled 전달
- 기존 테스트 수정은 필요하지 않았다. Global·Input 팩토리를 직접 부르는 테스트가 없다.

**보고 지점:** 공용 파일 diff, 컴파일·순수 계약·Unity 연결·조립 테스트 결과, 기존 테스트 수정 목록. 실제 카탈로그의 선행 장애는 별도 상태로 표시한다.

## Phase 5 — 기록

| Task | 내용 |
|---|---|
| 5.1 ✅ | (2026-09-30 설계 §11로 작성. 가상 장치 테스트는 사용자 결정 B로 보류) 설계 문서에 구현 결과 절(파일, 계약 변경, 테스트 결과, 실제 카탈로그 선행 장애, 재생 검증 등 남은 일). 테스트 미실행/실패와 범위 완료를 구분해 기록한다 |
| 5.2 ✅ | (2026-09-30 권장안 A: 새 인계 문서 `handoffs/2026-09-30-stage-input-local-session-handoff.md`와 CLAUDE.md "최신 작업" 교체. 사용자 요청으로 View 작업자용 `viewDocs/stage-input-guide.md`도 작성) 인계 문서 갱신 여부는 사용자에게 묻는다 |

## 이 계획에 없는 것

- 전투 View: IStageInputReceiver 구현, 포인터 → 칸, 드래그·호버, UI 위 포인터 차단, CombatViewBuffer 기록, Layer 전환 호출. 후속 구현도 SelectCanceled의 무확정 정리·파괴 전 Unbind 규칙을 따라야 한다(D4 유지)
- 다른 화면 Controller(StageSelection, Artifact, Settings)
- 런타임 부트스트랩과 재생 확인
- ViewGroup.asset의 기존 누락 테이블 3개 보충. 별도 해결 전 실제 카탈로그와 전체 런타임 초기화 완료를 선언하지 않는다

## 예상 규모

| Phase | 새 파일 | 주요 수정 범위 | 에디터 작업 |
|---|---|---|---|
| 0 | 1 (문서) | 0 | — |
| 1 | 3 (에셋 1, 코드 2) | 0 | 에셋 확인 |
| 2 | 5 (테스트 1) | InputSystem.cs의 ClearLayer 계약·구현, 필요 시 기존 가짜 런타임 | EditMode |
| 3 | 4 | 운영/테스트 asmdef, DB 키·쿼리·팩토리, DB 테스트 | 테이블·아이템 에셋, ViewGroup 등록, EditMode |
| 4 | 5 (Unity 테스트 1) | InputSystemGroup, 팩토리 3개, 서비스 키(신규 파일 가능), 기존 호출부·테스트 | EditMode |
| 5 | 0 | 1~2 (문서) | — |

## 검토 지적과 반영 위치

| 지적 | 설계 | 구현·검증 |
|---|---|---|
| 비활성 Layer가 기존 API·옛 초기화와 충돌 | §4.1 ClearLayer | 2.0, 2.5, 4.3~4.5, 4.9 |
| 강제 Canceled가 선택 확정으로 전달됨 | §4.3 ReleaseOnly·SelectCanceled, 포인터 취소 무시 | 1.1, 2.1~2.5, 4.9 |
| View 교체 시 옛 누름이 새 View로 넘어감 | §4.4 선택 소유권 | 2.2~2.5 |
| Layer 목록 전달 경로 누락 | §4.2 조립 | 1.3, 4.5, 4.7~4.9 |
| Phase 3의 어셈블리 참조 누락·Unity 경계 모순 | I6, 파일 목록 | 3.0~3.5 |
| 입력 설정 누락 시 동작과 형식 지정 초기화 충돌 | §6.1 필수 설정·테스트 분리 | 3.3~3.6, 4.5, 4.9 |
