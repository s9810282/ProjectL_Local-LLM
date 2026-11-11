# 유지 덱 연결 (2026-10-16)

## 현재 정책 — 배치 전용·자동 등장 (2026-11-07)

아래 버튼 전환·Exclusive 설명은 이전 이력이다. 현재 DeckButton·CloseDeck 오브젝트와 자식·컴포넌트는 보존하고 비활성화만 했다. 코드가 버튼을 자동으로 다시 켜거나 입력을 바인딩하지 않는다. DeckButtonUI는 기존 UIId.Deck 루트와 카드 참조 검증을 유지한다.

최초 진입은 맵 완료→기존 TurnDisplay의 검정 `배치 단계` 안내→네 카드 A안 순차 등장→입력 허용이다. DeckCardList의 Entrance는 Card Seconds 0.28·Stagger Seconds 0.07이며 기존 Fan Layout 최종 배치를 유지한다. 진입 동안 내부 CardFan CanvasGroup으로 클릭·Submit·정보·Hover를 억제한다. 시간 진행은 Combat 진입 Tick 한 곳에서 한다.

UnitCard 정책은 Parallel로 바뀌어 배치 중 필드 정보창과 함께 보인다. 전투·완료에는 목록과 카드 상세를 닫고 이후 배치 복귀에는 자동으로 연다. 국면별 개폐는 CombatDeploymentPresentation이 맡으며 CombatUnitInfo는 정보 표시만 맡는다. 튜토리얼의 진입 완료 대기는 사용자 지시로 별도 작업이다.

코드·씬 연결 완료, **컴파일 미확인**. Unity에서 최초 순서·카드 배치/호버·정보 고정·전투 숨김·배치 복귀를 확인한다.

### Entrance 모드 추가 — D2 (2026-11-07)

Combat 씬 `DeckUI > DeckPannel`의 `Deck Card List > Entrance > Mode`에서 선택한다. `Rise`는 기존 아래에서 한 장씩 상승, `Fan Spread`는 첫 카드 위치를 기준으로 좁게 접힌 네 장의 간격과 회전을 왼쪽부터 펼친다. 프리팹 기본값/직렬화 0은 Rise, 현재 Combat 씬은 FanSpread(1)로 지정했다.

- `Card Seconds` 0.28: 각 카드의 이동·회전 시간. `Stagger Seconds` 0.07: 다음 카드 시작 간격. 두 모드 공통이다.
- `Folded Spacing Ratio` 0.12: 최종 간격/각도 차이 대비 접힌 비율. 작을수록 왼쪽에 모인다.
- `Fan Lift Ratio` 0.12: 카드 높이 대비 시작 시 내려둔 거리.
- `Fan Reveal Seconds` 0.10: 전체 네 장의 짧은 상승·선명화. 펼침과 동시 진행하며 총 시간은 네 장 순차 시간과 이 값 중 큰 값이다.

최종 부채꼴 설정·순서·크기·Hover, 시작 알림·대기·취소 계약은 유지한다. 숨김/취소 뒤 alpha=1과 입력 상태를 복원한다. 새 설정과 자세 계산만 추가했으며 Test 코드·MSBuild·Unity 실행은 하지 않았다. **컴파일 미확인**. 두 모드 비교·최종 자세 동일성·시간 0·취소 후 재진입을 Unity에서 확인한다.

## 이전 연결 기록

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

DeckButtonUI의 Open Button, Close Button, Cards를 연결합니다. DeckCardList의 Initial Instances에 UnitCard_1~4 네 장을 화면 순서대로 연결합니다. 카드의 기본 크기·배율은 프리팹을 사용하고, 위치·회전·호버는 DeckCardList의 Fan Layout 설정이 담당합니다(2026-11-06 변경, 아래 기록).

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

## 부채꼴 배치·호버 (2026-11-06)

- 입력은 사용자가 이번 작업에서 선택한 기존 역할을 유지한다. 본체 클릭·Submit·드래그는 배치 선택, 내부 정보 버튼은 공용 상세 고정·해제다. 다른 카드 Hover로 고정된 상세를 변경하지 않는다. 카드 본체 클릭으로 정보를 고정하는 일반 규약의 이번 작업 예외다.
- DeckCardList가 생성·수명·입력 조립을 소유하고, 직렬화된 내부 클래스 DeckCardFanLayout이 기하 계산·보간·그리기 순서만 담당한다. 설정 변경은 Fan Layout 안에서 끝나고 Buffer·배치 규칙·InputSystem은 수정하지 않는다.
- 네 장은 메인부터 `+22 / +7.33 / -7.33 / -22`도(Unity Z 회전), 중앙이 높은 아치로 배치한다. 기본 카드 폭의 25%를 겹치되 패널 너비가 부족하면 간격을 줄인다. 기본은 중앙 하단이다. Fan Layout의 bool SerializeField `alignBottomLeft`를 체크하면 좌측 하단, 해제하면 중앙 하단으로 패널 X 앵커·Pivot을 맞춘다. Y 앵커와 기존 패널 슬라이드 경로는 유지한다.
- Hover는 카드 높이의 30% 상승, 원래 배율의 1.5배, 회전 0도로 0.16초 동안 보간한다. Time.unscaledDeltaTime을 사용한다. 배치 드래그 중에는 강조·필드 Arrow를 억제하고 드래그 종료 후 실제 Hover에 맞춰 복귀한다.
- Fan Layout의 `yOffset`(기본 0)은 네 UnitCard와 각 카드의 입력 영역에만 더한다. 양수는 위, 음수는 아래로 이동하며 부채꼴·Hover에도 유지된다. DeckCardList 자체의 Y, 닫기 버튼·상세창, 기존 패널 슬라이드 목표값은 변경하지 않는다.
- Scene 또는 Prefab Mode에서 DeckCardList를 선택하면 전용 DeckCardListEditor가 기존 카드에 기본 부채꼴 배치를 미리 적용한다. Fan Layout의 Y Offset·정렬·각도·간격·아치 변경은 재생 없이 Scene 뷰에 반영된다. 런타임과 같은 배치 계산을 사용하며 Undo/Redo·씬 저장·프리팹 변경 기록을 지원한다. 호버 애니메이션은 Play Mode에서 확인한다.
- 편집 미리보기는 카드 RectTransform만 수정하고, 패널의 정렬 이동은 카드 좌표로 환산해 표시한다. 따라서 편집 중 패널 위치·닫기 버튼·Left 복귀 위치를 덮어쓰지 않는다. 재생 시에는 기존 패널 정렬 경로가 적용되므로 카드의 최종 위치는 동일하다. CardFan·CardSlot 생성, 입력 바인딩, UIGroup 멤버 초기화, Buffer 조회, 상세창 연결 검증은 편집 미리보기에서 실행하지 않는다. 원본 에셋은 Prefab Mode로 열어 편집하며 Project 창에서 에셋을 선택한 것만으로 수정하지 않는다.
- 편집 확장의 경계: 부채꼴 기하 변경은 DeckCardFanLayout의 공통 계산에, Undo·Prefab 변경 기록은 DeckCardListEditor에 둔다. DeckCardList의 편집 전용 호출만 둘을 연결하고 Player 빌드에서는 제외한다. 실행 중 카드 입력·서비스 등록·수명 소유권은 기존 경로를 유지한다. 미리보기 후 재생 진입, Y Offset 양수/음수, 좌측/중앙 전환 반복, Undo/Redo, Prefab Mode 저장·재열기를 Unity 확인 항목에 추가한다.
- Awake의 PrepareCards에서 참조·설정을 검증한 뒤 CardFan과 네 CardSlot을 만든다. 기존 UnitCardUI를 슬롯 자식으로 옮기고 기본 크기·배율을 보관한다. 고정 입력 영역과 움직이는 카드의 합집합은 DeckCardHoverBinding의 ICanvasRaycastFilter로 처리한다. 포인터 콜백에서 별도 Raycast나 할당을 하지 않는다.
- 카드 본체와 슬롯 배경 Button 모두 UIInputBinding의 같은 명시적 수신자·슬롯으로 한 번 바인딩한다. 빈 슬롯은 Raycast를 받지 않고, 배경은 본체의 배치 interactability를 따른다. 배치 불가 카드의 정보 Hover는 기존 계약상 허용하므로 DeckCardHoverBinding은 Button.interactable과 독립적이다. 슬롯 배경은 Navigation.None으로 두고, 배경 클릭 수신자는 실제 카드 Button으로 선택을 옮겨 이후 키보드·게임패드 Submit 대상을 맞춘다.
- Hover 슬롯만 CardFan 안에서 가장 앞에 그리며, 해제 시 원래 슬롯 순서로 복원한다. UIGroup의 등록 목록·덱 순서는 바꾸지 않는다. 카드 교체·비움, 패널 숨김·부모 비활성화, 서비스 재바인딩 시 Hover와 보간을 초기화한다. CardFan·CardSlot은 기존 화면 내부 위젯이며 별도 UIId나 서비스 등록을 추가하지 않는다.
- 등록·조회 경로는 기존 DeckButtonUI(UIId.Deck) → DeckCardList(UIId.UnitCard, Parent Deck, Exclusive) → 그룹 멤버 UnitCardUI 네 장이다. CombatCardInfoUI(UIId.CombatCardInfo, Parent UnitCard)와 IUISystemService 개폐 경로도 유지한다. Global 하위 서비스의 루트 Export·직접 조회는 추가하지 않는다.
- 검증 상태: 코드·프리팹 연결 정적 확인만 수행. 프로젝트 지침에 따라 Test 코드 작성·MSBuild·Unity 실행은 하지 않았다. **컴파일 미확인**. Unity에서 네 장 순서·양끝 각도, 경계 Hover 유지와 빠른 전환, 정보 고정 후 다른 카드 Hover, 본체 클릭·Submit·드래그 배치, 드래그 중 억제·복귀, 숨김/재열기·씬 전환 재사용, Align Bottom Left 체크/해제 배치와 화면 비율을 확인한다.
