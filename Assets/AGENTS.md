# UI input conventions

- In Deck and future Hand unit lists, clicking a unit pins its info card. Hovering any other unit must not switch or unpin it. Clicking another unit may switch the pinned selection; clicking the pinned unit again closes it. Hover previews resume after the pin is cleared.

- For new or modified UI, prefer Unity pointer handler interfaces (`IPointerClickHandler`, `IPointerEnterHandler`, `IPointerExitHandler`) and `ISubmitHandler` with direct calls to an explicit receiver interface.
- Reuse `UIInputBinding` and `IUIInputReceiver` in `ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/UISystem/Base` for Button inputs. Bind the receiver and slot once; do not connect screen behavior through `Button.onClick.AddListener`, delegate closures, or Inspector UnityEvent listeners.
- Keep Button visuals, interactability, left-click behavior, and keyboard/gamepad Submit support. Hover must be cleared once on exit, disable, rebind, or loss of interactability. Avoid per-control raycasts and allocations in pointer callbacks.
- InputSystem owns action maps, shortcuts, and input layers. UI views own presentation and receive explicit input calls. Do not introduce event subscriptions for UI input without a concrete need.
- When verifying input, use EventSystem handler dispatch rather than invoking Button.onClick directly.

# 서비스 조립·UI 등록 규약 검사

- 규약 검사는 개별 클래스만 보지 않고 **Factory 생성·등록 → Group의 보관·Export → 상위/루트 Builder → 소비자의 조회 경로**를 함께 추적한다. 같은 Builder의 동일 인터페이스 중복 등록(FW-001)과, 서로 다른 Builder 사이에서 하위 서비스를 루트에 다시 노출하는 구조 문제를 구분한다. 실제 중복 등록이 없다는 이유만으로 계층 검사를 통과시키지 않는다.
- Global 하위 UI·Render·Input·Utility·Loading·Job 등의 서비스는 기존 Global Context 경로로 제공하고 조회한다. 조회 편의를 위해 `GlobalSystemGroup.ExportServices`에서 하위 서비스를 루트에 별도로 재등록하거나 소비자가 루트 resolver로 직접 조회하는 우회를 만들지 않는다. 필요한 접근 계약은 해당 Group/Context에 추가한다. 별도 루트 등록이 정의된 계약은 그 근거와 적용 범위를 확인한다.
- 서비스 검사 결과에는 생성 주체, 내부 등록 위치, 상위 Export 위치, 소비자 조회 위치를 기록한다. `UITextPresentation`처럼 내부 등록은 정상이어도 상위 재등록과 소비자 직접 조회가 프로젝트의 계층 규약을 위반할 수 있음을 확인한다. FW 필수 계약 위반과 프로젝트 구조 규약·권장사항 위반을 구분해 보고한다.
- **새 화면·팝업·패널·독립적으로 열고 닫는 HUD는 `UIBase` 기반으로 UISystem에 등록하는 것을 기본으로 한다.** UI 이력·등록·수명 연결을 피하려고 독립 MonoBehaviour, 별도 표시 서비스 또는 외부의 직접 `SetActive` 제어로 대체하지 않는다. 먼저 `UIOpenPolicy`, 부모/자식 UI, `GlobalUIBase`, `UIGroup` 등 기존 확장 지점으로 요구사항을 표현한다.
- 유지 UI, 뒤로가기 제외, 동시 표시, 반복 생성 등의 요구만으로 UISystem 등록을 배제하지 않는다. 기존 계약이 해당 요구를 지원하는지 먼저 확인하고, 등록하지 않는 설계가 필요하면 구체적인 제약과 수명 소유자를 설계·리뷰에 기록한다. 이미 등록된 화면 내부의 슬롯·아이콘·바·애니메이션 등 세부 위젯을 각각 UIBase로 만들거나 중복 등록할 필요는 없다.
- UI 설계·리뷰에는 **등록 단위, UIId·부모·Policy, 생성/등록/바인딩 시점, 열기·닫기 호출 경로, 재사용·씬 전환·해제 수명**을 확인한다. 등록된 UI의 외부 개폐는 IUISystemService를 거치고, 실제 활성 상태 변경은 해당 UI의 Open/Hide 구현이 담당한다. 위 기준은 새 UI와 수정 범위에 적용하며, 사용자 요청 없이 기존 UI 전체를 일괄 재구성하지 않는다.
- UI 연결·참조·등록·계층 검증에서 필수 의존성 누락이나 잘못된 구성을 발견하면 로그 후 return/false/null로 성공처럼 계속하지 않고 원인과 대상(UIId·필드·객체)을 명시한 예외를 throw한다. 인자 오류는 ArgumentNullException/ArgumentException, 구성·수명 상태 오류는 InvalidOperationException을 사용한다. 검증은 가능한 한 상태 변경 전에 수행한다. 명시적인 선택 기능, 바인딩 전 대기, 비활성 입력 무시, 정상적인 조회 실패와 반복 해제는 오류 검증과 구분하며, 이 규칙 때문에 일괄 예외 처리하지 않는다. 정상 경로로 취급하는 근거를 계약·주석·리뷰에 남긴다.
