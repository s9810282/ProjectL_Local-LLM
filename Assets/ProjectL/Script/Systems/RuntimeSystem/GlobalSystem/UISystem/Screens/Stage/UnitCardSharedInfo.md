# UnitCard 공용 상세 패널 설계·Task (2026-11-02)

## 설계

사용자가 추가한 `Prefabs/UI/ScrollContentFrame.prefab`을 Combat 씬 Canvas에 한 번 배치한다. 기본은 비활성이다. 카드별 InfoPannel·AbilityPannel과 그 직렬화 필드·개폐·특성 입력 구현을 제거하고, 기존 InfoButton이 공용 상세 패널로 직접 요청한다. 카드 본체의 배치 선택·드래그와 필드 호버 입력은 유지한다. 손패 상세 입력 추가는 이번 범위가 아니다.

- 등록: 공용 루트 `CombatCardInfoUI : UIBase`, `UIId.CombatCardInfo = 47`, ParentID = UnitCard, Exclusive. 부모 UnitCard 목록과 Deck 조상은 보존되며 다른 가지의 Exclusive 이력은 닫힌다. 기본 비활성도 UISystem.BindScene에서 등록한다. DeckSlotInfo/DeckEdit 등록은 변경하지 않는다.
- 입력: InfoButton → UIInputBinding/IUIInputReceiver → 명시적인 IUnitCardInfoReceiver → 공용 패널. 클릭/Submit만 열며 동일 대상 재클릭은 닫고 다른 대상 클릭은 전환한다. 사용자 후속 지시로 호버 개폐·미리보기는 제공하지 않는다. CloseButton도 같은 입력 계약을 쓴다. Button.onClick/Inspector 이벤트/입력 이벤트 구독은 추가하지 않는다.
- 표시: UnitCardUI가 기존 Buffer/보유 능력치/View DB 조회에서 표시값을 만든다. 기본 수치·EXP 대기·2/3특성 영역은 DeckSlotInfo의 기존 내부 표시 위젯을 공유한다. DeckEditCardModel을 전투 모델로 사용하거나 규칙을 View에서 재계산하지 않는다.
- 수명: 씬의 직렬화 참조를 DeckCardList가 검증·연결한다. 개폐는 바인딩된 IUISystemService를 거치고 실제 활성 변경은 UIBase Open/Hide만 수행한다. 대상 변경/삭제/카드 숨김/목록 닫힘/씬 해제 시 선택을 해제한다. 열기·닫기는 기존 선택 포커스를 보관·복원한다. 재사용 시 이전 대상의 데이터가 남지 않는다.
- 필수 참조는 등록/바인딩 시 상태 변경 전에 검증하고 UIId·필드·객체를 포함한 구성 예외를 던진다. 상세 입력을 제공하지 않는 손패/로비 카드의 InfoButton 생략 또는 입력 비활성은 명시적인 선택 구성이다.

## 규약·독립성 검토

- SSOT: 능력치/특성은 기존 Buffer, 공용 상세의 대상/고정 상태는 공용 패널 한 곳이다. 카드의 갱신 번호는 표시 무효화 캐시다.
- SOLID: 조회(UnitCardUI), 값 표시(UnitCardView·기존 상세 위젯), 선택/개폐(공용 UI), 조립(DeckCardList·프리팹/씬)을 구분한다(S). 공통 표시값 계약으로 두 화면에서 표시 위젯을 공유한다(O). UIBase Open/Hide와 UIGroup 멤버 계약을 유지한다(L). 상세 요청은 좁은 명시적 수신자만 받는다(I/D).
- 대표 변경: 카드 외형 수정은 UnitCardView/카드 프리팹, 상세 배치 수정은 상세 위젯/ScrollContentFrame, 의존성 연결 수정은 DeckCardList/씬에서 한다. 두 화면이 공유하는 수치/특성 표시 계약 변경은 DeckEdit와 Combat 양쪽 확인이 필요하다. 배치/공격/필드/카메라/Core 기능 구현은 건드리지 않는다.
- 서비스 생성: 기존 GlobalSystemFactory → UISystem 생성/Global 내부 Builder 등록 → GlobalSystemGroup.UIServiceCommand 보관 → GlobalSystemGroup만 상위 Export → 루트 Builder. 화면은 UISystem.BindScene의 BindUISystemService로 받는다. 새 서비스·모듈·루트 재등록·루트 직접 조회가 없으며 FW-001/002/003의 구조는 유지한다. 새 화면은 FW-008/009의 등록·부모·이력 계약을 따른다. InputSystem action map/계층 계약은 변경하지 않는다.

## Task 계획 — Phase 1 (일괄 연결 후 리뷰)

1. 공용 표시값 계약과 DeckSlotInfoStats의 공통 표시 경로를 준비한다. 기존 DeckEdit의 결과를 유지한다.
2. CombatCardInfoUI와 명시적인 상세 요청 계약을 추가하고, UnitCardUI 조회·갱신과 InfoButton 입력을 연결한다.
3. UnitCardView의 내부 패널·특성 입력·구독 API를 제거하고 생산 호출자를 맞춘다. 기존 시험은 컴파일이 깨지는 호출만 최소 변경한다. 새 시험은 작성하지 않는다.
4. UnitCard 프리팹의 InfoPannel/AbilityPannel 하위 객체와 직렬화 참조를 제거한다. ScrollContentFrame 루트에 공용 UI를 연결하고 중복 DeckSlotInfoUI와 함께 복사된 덱 편집 목록/출전 UI는 이 공용 프리팹에서 정리한다. Combat Canvas에 기본 비활성으로 배치해 DeckCardList에 연결한다.
5. 참조/GUID/fileID/잔여 호출/변경 범위를 정적으로 확인하고 기록한다. Phase 종료 후 멈추고 보고한다.

## 사용자 Unity 확인

컴파일 미확인. Test 코드 작성·실행, MSBuild를 수행하지 않는다. Unity에서 초기 비활성, InfoButton EventSystem 클릭/Submit, 동일 대상 재클릭 닫기/다른 대상 전환, 호버로 열리지 않음·열린 대상이 호버로 바뀌지 않음, Close/뒤로가기/목록 닫기/국면 전환, 재사용/데이터 갱신, 0/1/2/3/4/다수 특성(Phase 2에서 상한 제거), 기존 배치 선택/드래그·손패·로비 상세·DeckEdit 수치 표시를 확인한다.

## 실행 기록

- Phase 1 구현 완료. 공용 UI=47/부모 UnitCard/Exclusive와 씬 참조를 연결하고 기본 비활성으로 배치했다. ScrollContentFit은 직계 부모 Combat Canvas를 viewport로 사용한다. 실제 배치 크기·가림은 Unity에서 확인한다.
- 내부 패널과 직렬화 참조/입력/PanelOpening 구독/이전 API를 제거하고 손패·로비 생산 호출을 새 정보 입력 비활성 API에 맞췄다. UnitCardUI는 Buffer 변경 시 상세 표시 캐시를 갱신하고 공용 패널이 변경 번호를 보고 내용을 갱신한다.
- 사용자 후속 요청을 반영해 클릭/Submit만 허용했다. 중간에 검토한 호버 미리보기와 그 CanvasGroup은 최종 코드/프리팹에 남기지 않았다.
- 카드·공용 프리팹·Combat 씬에서 fileID 중복과 끊어진 로컬 참조가 없음을 정적으로 확인했다. 기존 시험 코드에서 삭제 API의 직접 호출은 발견되지 않아 시험 파일을 수정하지 않았다. 새 시험/빌드/Unity 재생은 수행하지 않았다. 컴파일 미확인.
- DeckUI에 남았던 삭제 패널의 중첩 프리팹 override 55건도 제거했다. UnitCard를 참조하는 에셋의 source fileID, DeckUI를 포함한 네 에셋의 로컬 참조/중복, 변경 C#의 UTF-8/LF와 git diff --check를 확인했다.
- 작업 중 별도로 변경된 DeckEditUI.prefab의 위치 값은 수정/복구하지 않았다. Core·FieldCamera·Architecture·구형 SO를 수정하지 않았고 커밋·push하지 않았다. Phase 1 종료 후 사용자 리뷰에서 멈춘다.

## Phase 2 — 특성 개수 제한 해제 (2026-11-02)

### 설계

사용자 지시로 특성 개수 상한을 제거한다. 0~2개는 기존 2칸 영역, 3개 이상은 3칸 영역을 쓴다. 4번째부터는 3번 슬롯 전체를 같은 부모·위치에 복제하고, 후속 배치 지시인 "3번 슬롯 뒤에 숨기기"에 따라 3번 슬롯보다 먼저 그려지도록 sibling 순서를 둔다. 복제 슬롯에도 각 특성의 아이콘·이름·설명을 모두 적용한다. 표시상 앞에 남는 슬롯은 기존 3번이다.

복제본은 영역이 소유하는 재사용 목록에 보관한다. 최대 필요 수가 늘 때만 추가하고, 줄어든 뒤 남는 복제본은 내용을 비우고 비활성화한다. 패널 숨김에서는 보관하며 같은 패널을 다시 사용할 때 재사용한다. 부모 씬/패널 파괴가 하위 복제본도 해제한다. 새 UI 등록·서비스·입력은 만들지 않는다.

SSOT는 기존 특성 표시값이며 개수 정책/영역 선택은 DeckSlotInfoAbilities, 복제본 수명은 DeckSlotAbilityArea, 복제와 내부 참조 대응은 DeckSlotAbilityContent가 맡는다(S). 입력 데이터 길이에 맞춰 확장하고 UIBase/내부 위젯 계약을 유지한다(O/L). 기존 좁은 표시값·참조만 사용하며 Core/규칙 조회/새 서비스 의존성은 없다(I/D). UIId·부모·Exclusive·클릭 전용·Factory/Group/Global Context 경로와 FW 등록 계약은 Phase 1 그대로다.

특성 개수/재사용 변경은 공용 특성 위젯에 한정하고, 수치·EXP·입력·씬 조립은 수정하지 않는다. 공용 위젯을 사용하는 Combat과 DeckEdit 양쪽이 영향 범위이며, 각각의 선택/개폐 기능 구현을 읽거나 바꾸지 않는다.

### Task — 단일 Phase 후 리뷰

1. 3개 상한 예외를 제거하고 3개 이상을 기존 3칸 영역으로 전달한다.
2. 3번 슬롯 복제·자식 인덱스에 따른 명시적 내부 참조 대응·뒤쪽 sibling 배치·필요 수만 추가·초과분 비우기/숨기기/재사용을 구현한다.
3. 기존 호출·직렬화 참조·변경 범위·문서의 현재 상한 안내를 정적으로 확인하고 완료 보고 후 멈춘다. Test 코드·MSBuild·Unity 실행은 수행하지 않는다.

### 사용자 Unity 확인

컴파일 미확인. Combat/DeckEdit 각각 특성 0/1/2/3/4/다수, 다수→3→2→다수, 재열기/다른 카드 전환에서 원래 3번이 복제본 앞에 남고 모든 복제본의 내용·비활성·재사용이 맞는지 확인한다. 클릭 전용·Close·부모 닫기 동작도 유지되는지 확인한다.

### 실행 기록

- Phase 2 구현 완료. 상한 예외를 제거했고 3개 이상은 기존 3칸 영역을 사용한다. 4번째부터 3번 슬롯의 전체 계층을 복제하고 원본보다 앞선 sibling index에 두어 원본이 앞에 그려지게 했다. 내부 참조는 원본과 복제본의 자식 인덱스로 대응한다.
- 생성 수는 지금까지 필요했던 최대 추가 수만 유지한다. 다시 열기/동일 수 갱신은 재사용하고, 감소한 초과 슬롯은 아이콘·이름·설명을 비운 뒤 숨긴다. 씬/패널 부모 파괴가 복제본도 해제한다. 프리팹·UI 등록·선택·클릭 전용 입력·Core는 변경하지 않았다.
- 정적 호출/개수 제한 잔여·UTF-8/LF·git diff --check를 확인했다. 복제 원본 Ability.prefab에는 독립 UIBase가 없어 추가 UI 등록도 필요하지 않다. Test 코드 작성/실행·MSBuild·Unity 실행은 수행하지 않았다. 컴파일 미확인. Phase 2 종료 후 리뷰에서 멈춘다.
