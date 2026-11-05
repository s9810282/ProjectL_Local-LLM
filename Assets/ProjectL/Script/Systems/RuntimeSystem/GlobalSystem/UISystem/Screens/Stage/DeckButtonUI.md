# 유지 덱 연결 (2026-10-16)

DeckButtonUI는 계속 활성인 제어 루트에 둡니다. 실제 DeckButton과 DeckPannel은 그 아래의 형제입니다.

```text
GlobalUICanvas
└─ DeckUI (DeckButtonUI)
   ├─ DeckButton (Button, 기본 비활성)
   └─ DeckPannel (DeckCardList, 기본 활성)
      ├─ UnitCard_1 (UnitCardUI, UnitCardView, Button)
      ├─ UnitCard_2 (UnitCardUI, UnitCardView, Button)
      ├─ UnitCard_3 (UnitCardUI, UnitCardView, Button)
      ├─ UnitCard_4 (UnitCardUI, UnitCardView, Button)
      └─ CloseDeck (Button)
```

DeckButtonUI의 Open Button, Close Button, Cards를 연결합니다. DeckCardList의 Initial Instances에 UnitCard_1~4 네 장을 화면 순서대로 연결합니다. 카드의 위치와 크기는 프리팹에서 작성하며 코드가 다시 배치하지 않습니다.

DeckCardList는 UIGroup의 소유권·멤버 바인딩을 사용합니다. 그룹은 UIId.UnitCard, 부모는 UIId.Deck으로 UISystem에 등록하며, 네 UnitCardUI는 그룹 멤버라 개별 등록하지 않습니다. 따라서 카드들의 UIId가 같아도 중복 등록되지 않습니다.

프리팹의 기본 상태는 패널 활성·버튼 비활성입니다. 런타임 표시 상태는 UI 초기화·씬 진입 정책을 따릅니다. CloseDeck은 CloseUI(UnitCard), DeckButton은 OpenChildUI(Deck, UnitCard)를 요청합니다. 그룹의 Open/Hide 콜백에서 DeckButton 표시를 함께 갱신하므로 뒤로가기·부모 닫기·HideAllUI도 같은 상태를 만듭니다. 전체 화면 바깥 클릭 차단 영역은 만들지 않습니다.

CharacterBuffer.Owned에서 Main은 UnitCard_1에, 보유 순서의 서브 세 명은 UnitCard_2~4에 표시합니다. 메인 카드도 클릭·Submit·드래그로 배치를 선택할 수 있습니다. 메인은 기존 MainCharacterPlacement 미리보기와 PlaceMainCharacter 명령을 사용하며, 실제 배치 가능 여부와 잠금은 기존 Core 판정을 따릅니다. 메인 카드의 정보·능력·체력·레벨과 Hover Arrow는 기존 표시 경로를 사용합니다. 배치 뒤에도 카드를 유지합니다. 보유 목록이 비면 기존 인스턴스를 숨기고 다음 목록에서 재사용합니다. 낮에는 카드 본체의 클릭·Submit으로 배치 대상을 선택하고, 필드 클릭으로 기존 배치 명령을 제출합니다. 밤·배치 완료·종료·오류·입력 정지 중에는 본체의 배치 입력만 막으며 정보·능력 버튼은 유지합니다.

2026-10-17 네 장 확장: UnitCard_4를 initialInstances의 마지막 멤버로 연결했다. 위치·크기는 사용자가 직접 조정한다. 런타임·테스트 어셈블리 컴파일 오류 0, diff 형식 검사 통과. 사용자 저장·프리팹 모드 종료 뒤 Unity EditMode 92건 통과: 메인 첫 슬롯·배치 입력 차단·Hover, 네 장 등록·재사용, 기존 서브 배치 입력을 확인했다. 아래 이전 테스트 결과는 네 장 확장 전의 기록이다.

배치 입구는 ICombatViewService.DeckPlacement의 좁은 ICombatDeckPlacementInput 계약입니다. CombatDeckPlacementInput이 씬·입력·확인창 수명과 오류 경계를 검사하고 PlacementDragInteraction에 위임합니다. 배치 미리보기, 명령 번호·제출, 취소, 필드 유닛 우클릭 복귀는 기존 경로를 사용합니다. 패널을 닫으면 덱에서 선택한 배치만 취소합니다. 필드에서 집은 유닛은 패널 닫기로 취소하지 않습니다.

옛 HandPresenter는 씬 연결·공용 표시 계약 호환을 위해 남아 있지만 운영 CombatSceneDisplay에서는 빈 목록과 숨김 상태만 전달하므로 손패 카드를 생성하거나 표시하지 않습니다.

모든 Button 입력은 UIInputBinding의 명시적 수신자·슬롯으로 연결합니다. Inspector UnityEvent와 Button.onClick에는 화면 동작을 연결하지 않습니다.

## Phase 2·3 검증 기록

Unity 6000.0.73f1 EditMode 확장 회귀: 총 402건, 통과 394건, 실패 8건.

- 덱 5건, UnitCard 8건, UIGroup 12건, UIInputBinding 4건 통과.
- CombatInteraction 71건, InputRouter 21건, InputReceiver 6건, CombatViewSystem 80건 통과.
- 전투 진입 71건, 진행 Job 57건, 오류 정리 14건, 보유 능력치 4건, 로비 덱 8건, 로비 출발 4건 통과.
- 잔여 실패: CombatDisplayReuseTests의 행동 버튼 4건(returnText 필드가 주석 처리된 구현과 fixture 불일치), 옛 손패 2건(UnitCardView fixture의 Transform을 RectTransform으로 캐스팅), CombatPlacementFeedbackTests의 고스트 Color 정확 비교 2건. 해당 구현·fixture 파일은 이번 Phase 2·3에서 수정하지 않았다.

프리팹 참조, 카드 배치값 유지, 중복 등록 방지, 씬 재바인딩, UI 클릭 뒤 필드 명령 중복 차단, 덱 선택 취소와 필드 선택 유지, 정지 뒤 늦은 배치 입력의 무효화를 검증했다. 실제 플레이 조작·화면 확인은 2026-10-17 사용자가 완료했다.

## 실행 확인 후 보완 (2026-10-17)

카드 본체에서 왼쪽 버튼 드래그를 시작하면 DeckCardDragBinding이 클릭과 같은 배치 선택 입구로 전달한다. 이동·고스트·필드에서 놓기는 기존 InputSystem 및 PlacementDragInteraction이 처리하며 UI가 배치 명령을 중복 제출하지 않는다. 드래그가 시작된 입력의 클릭 자격을 해제하고, 비활성 배치 버튼·오른쪽 드래그는 무시한다.

2026-10-20 드래그 표시: DeckCardDragBinding은 선택 입력과 별도로 IDeckCardDragReceiver에 드래그 시작·종료를 직접 전달한다. DeckCardList가 자기 RectTransform의 anchoredPosition.y를 185에서 -60으로, 놓으면 185로 각 0.2초 동안 SmoothStep 보간한다. 시간 배율과 무관하게 진행하며, 이동 중 놓으면 현재 위치에서 복귀한다. 카드 비활성화·입력 재바인딩은 드래그 종료로 처리하고 패널 비활성화는 위치를 185로 초기화한다. X값·등록 단위(UIId.UnitCard, 부모 Deck, Parallel)·개폐 경로·서비스 조립/조회 경로는 유지한다. 같은 Binding을 쓰는 가방은 드래그 표시 수신자를 구현하지 않아 덱 이동을 발생시키지 않는다. Unity 재생 확인은 대기 중이다.

드래그 보완 검증: Unity EditMode DeckButtonUITests·CombatInteractionTests 총 80건 통과. EventSystem 드래그 전달, 비활성·오른쪽 입력 차단, 두 번째 필드 누름 없는 놓기와 중복 제출 방지를 확인했다.

StageSelect에 진입할 때마다 DeckPannel을 닫고 DeckButton을 표시한다. Combat 진입은 기존처럼 패널을 연다. 씬별 표시 정책은 GlobalUIPresentation이 소유하며 Bootstrap에서 덱을 중복으로 열지 않는다.

등록 계약 보완: ExportUIService의 등록 생략을 제거하고 UIBase 기본 등록을 사용한다. 유지 루트 Open 콜백은 루트만 표시한다. UISystem은 이 콜백이 끝난 뒤 부모의 열린 상태를 기록하므로, Combat 진입 호출자가 루트를 연 다음 자식 패널을 연다. 씬 재바인딩에서는 일반 자식의 이력을 초기화하고 패널을 닫으며, 이후 StageSelect/Combat 진입 정책으로 표시한다. 카드 Hover·배치 입력과 그룹 멤버 바인딩은 유지한다.

등록 계약 보완 검증: Unity EditMode 197건 통과. 그룹 단독 등록·멤버 중복 등록 방지, 뒤로가기·부모 닫기·HideAllUI, 씬 재바인딩 뒤 명시적 열기, 실제 Canvas의 카드 세 장 재사용, StageSelect/Combat 표시, 카드 패널·Hover·클릭 및 드래그 배치, 전투 진입·오류 정리를 확인했다. 실제 Canvas 테스트는 덱·주인공 표시의 등록 수명만 검증하도록 다른 화면의 서비스 바인딩을 제외한다.

카드 Hover는 DeckCardHoverBinding의 명시적 수신자로 전달한다. 배치가 차단된 카드에서도 Hover는 유지된다. CombatDeckHoverInput은 보유 ID를 CharacterBuffer의 현재 필드 EntityId로 매핑하고, FieldUnitPresenter가 해당 UnitHUD의 Arrow만 표시한다. UnHover, 카드 교체·숨김, 패널 닫기, Combat 해제와 HUD 재사용 시 Arrow를 해제한다. 배치되지 않았거나 사망한 유닛에는 표시하지 않는다.

관련 Unity EditMode 테스트 281건 통과. 마지막 디스크 확인에서 HUD.prefab의 사용자 Arrow가 사라진 것을 확인했고, 사용자 재저장 뒤 Image 참조를 다시 연결했다.

2026-10-17 메인 수동 배치 허용: UnitCard_1도 클릭·Submit·드래그 배치 선택을 허용한다. 필드의 메인은 기존 MainCharacterPlacement/PlaceMainCharacter 경로로 재배치하고, 손패 서브만 PlaceUnit 경로를 사용한다. 메인을 덱에서 선택한 경우에도 필드 출처를 유지해 결과 갱신 시 선택이 취소되지 않게 하며, 덱 닫기·취소는 메인 손패 반환 명령을 만들지 않는다. 기존 Core 배치 규칙은 변경하지 않았다. 런타임·테스트 컴파일 통과, Unity EditMode 91건 통과.
