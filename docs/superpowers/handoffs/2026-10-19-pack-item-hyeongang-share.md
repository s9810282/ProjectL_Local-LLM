# 전투 가방(PackUI) 아이템 표시 — 현강님 공유 (2026-10-19, 미전달)

- 설계: [pack-item-design](../specs/2026-10-19-combat-pack-item-design.md), 계획: [pack-item-plan](../plans/2026-10-19-combat-pack-item-plan.md).
- 현강님이 만든 `PackUI`(`aaac7c60`)에 재고 표시를 붙였습니다. 펼침·토글·표시 여부(`Refresh(facts, suspended)`, 배치 국면에만 보임)는 그대로입니다.

## 1. P-1 재고 표시 (2026-10-19)

- **값을 받는 길:**
  - 가방은 Buffer를 읽지 않습니다. `CombatViewSystem`이 재고를 읽고, 전투 표시(`CombatSceneDisplay`)가 그리기 때 넘깁니다.
  - 넘기는 계약은 `ICombatPackDisplay.ShowInventory(snapshot)`입니다. 넘기는 때는 화면 설치·결과 후처리 두 번입니다.
  - `PackUI`가 이 계약을 명시적으로 구현합니다. 같은 스냅숏 객체면 다시 그리지 않습니다.
- **칸 부품 `PackItemSlotView`(새):** `Item.prefab`에 붙입니다. 필드는 `icon`(Image)·`countText`(TMP_Text)·`button`입니다.
  - 보유 종류를 아이템 키 순으로 앞 칸부터 채우고 남은 칸은 끕니다. 수량은 `×n`입니다.
  - 아이콘이 없는 아이템은 칸의 처음 그림을 씁니다. 지금 아이템 아이콘은 없습니다.
  - 칸 수보다 종류가 많으면 경고 한 번만 남기고 넘친 종류는 보이지 않습니다.
- **칸 모으기:** `PackUI`가 `expansion` 아래의 `PackItemSlotView`를 계층 순서대로 모읍니다. 0개면 참조 누락 예외입니다.
- **설명:**
  - 칸에 호버하면 기존 `descriptionPanel`을 켜고, 새 필드 `descriptionText`(패널 안의 TMP)에 이름·설명을 씁니다.
  - 입력 슬롯은 0이 토글, 1부터가 칸입니다.
- **필수 참조가 늘었습니다:** `descriptionText`(설명 패널 안이어야 함)와 칸 부품입니다. 연결 전에는 Combat 진입이 참조 누락 예외로 멈춥니다.
- **에디터 연결(사용자가 함, 계획 T1.6):**
  1. `Item.prefab`에 `PackItemSlotView`를 붙이고 수량 TMP 자식을 만듭니다.
  2. `Combat.unity`의 Pack에서 설명 TMP를 `descriptionText`에 연결합니다.
- **아이템 외형:** 새 View 표 `ConsumableItemVisual`(아이콘·이름·설명). 아이콘이 생기면 표 에셋의 `icon`에 넣으면 됩니다.
- **다음(P-2):** 칸 드래그 시작을 장착 입력으로 넘기고, `dragRect`가 포인터를 따라가게 합니다.

## 2. P-2 드래그 장착 (2026-10-19)

- **칸 수:** 기획 답변대로 씬에 8칸(세로 레이아웃 그룹 + 크기 맞춤)을 넣었습니다. 8종을 넘으면 넘침 경고 한 번입니다.
- **칸 드래그 시작:** 칸마다 `DeckCardDragBinding`을 붙여(손패 카드와 같은 부품) 드래그 시작을 `Click(slot)`으로 받습니다.
  - `PackUI`는 그 칸의 아이템 키로 `ICombatViewService.ItemInput.Select(key)`를 부릅니다. 집을 수 있는지(배치 국면·대기 없음·재고)는 전투 입력이 판단합니다.
  - 칸을 한 번 눌러 집고 필드에서 다시 눌러 놓는 것도 됩니다(손패와 같음).
- **이동·놓기:** 포인터는 입력 시스템 → 전투 입력(`CombatItemDragInteraction`)이 받습니다.
  - 아군 필드 메인·서브 위에서 놓으면, 장착 칸이 비었으면 장착, 차 있으면 교체를 제출합니다.
  - 가방 수량은 결과가 온 뒤 바뀝니다.
  - UI 위나 빈 칸에서 놓으면 아무것도 하지 않습니다. 우클릭·Esc는 드래그 취소입니다.
- **따라다니는 아이콘 `dragIcon`(새 필수 필드, Image):** 드래그 중 포인터를 따라갑니다(`ICombatPackDisplay.ShowItemDrag/MoveItemDrag/HideItemDrag`).
  - 용근이 Pack 아래(펼침 목록 밖)에 Image를 만들어 연결했습니다. Pivot은 가운데입니다.
  - 처음 보일 때 아이콘과 그 아래 그래픽의 `raycastTarget`을 끕니다. 포인터 바로 밑에 있어서, 켜져 있으면 필드에서 놓아도 "UI 위에서 뗌"으로 취소되기 때문입니다.
  - 아이템 아이콘을 넣고, 아이콘이 없으면 넣어 둔 그림을 둡니다.
- **`DragRect`는 건드리지 않았습니다.** 처음에는 이것을 아이콘으로 옮기려 했지만, Image가 없는 1920×900 영역이라 되돌렸습니다. 지금처럼 가방 정리 때 끄기만 합니다. 이 영역의 용도(놓는 영역 등)를 알려 주시면 맞추겠습니다.
- 가방이 닫히거나 꺼지면 집은 아이템을 내려놓습니다.

## 3. P-3·P-4 유닛 HUD 아이템 아이콘과 우클릭 해제 (2026-10-19)

- **아이템 아이콘(P-3):** 장착한 유닛의 HUD에 상태 `"Item"`이 격파 목표 다음·특성 앞에 뜹니다. 아군·적 모두 그립니다. `UnitHUD`는 바꾸지 않았고 전투 표시(`FieldUnitPresenter`)가 상태 목록에 더합니다.
  - 위스키 발동·라운드 끝·회수 때 그 사건 순간에 사라집니다.
- **새 공용 UI 부품 `UISystem/Base/UISecondaryClickBinding.cs`(+ 수신자 계약 `IUISecondaryClickReceiver.SecondaryClick(slot)`):** 오른쪽 클릭만 받습니다.
  - `UIInputBinding`·`IUIInputReceiver`는 바꾸지 않았습니다. 바꾸면 모든 수신자가 영향을 받기 때문입니다.
  - 같은 오브젝트에 `UIInputBinding`과 함께 붙어도 서로의 버튼을 받지 않습니다. Button의 눌림 가능 여부는 보지 않습니다.
- **`UnitHUD`에서 바꾼 것(현강님 파일):**
  - 상태 아이콘을 만들 때 `UISecondaryClickBinding`도 붙입니다.
  - 새 `SetStatusSecondaryHandler(Action<EntityId, string>)`: 우클릭하면 (이 HUD의 개체 ID, 상태 Key)를 넘깁니다. 처리기가 없으면 무시합니다.
  - `UnitHUDStatus`에 끝 선택 인자 `hasSecondaryAction`(기본 false)이 생겼습니다. 기존 호출은 그대로입니다.
  - 아이콘이 입력을 받는 조건이 "설명 가능"에서 "설명 가능 **또는** 우클릭 동작 있음"으로 넓어졌습니다. 왼쪽 클릭 고정(설명 핀)과 호버는 지금처럼 설명 가능할 때만입니다.
  - 지금 우클릭 동작이 있는 것은 **아군** 아이템 아이콘뿐입니다. 적 아이콘은 입력을 받지 않습니다.
- **우클릭 해제:** 아군 아이템 아이콘을 우클릭하면 전투 입력이 배치 국면·필드의 아군 메인·서브·장착 칸 있음을 확인하고 해제를 제출합니다. 가방 수량은 결과 뒤 +1입니다. 메인은 손패로 돌아가지 않으므로 이것이 메인의 유일한 해제 방법입니다.
- **우클릭 겹침:** HUD 같은 UI 위에서 우클릭하면 이제 뒤쪽 칸의 서브를 손패로 회수하지 않습니다(전투 입력 쪽 수정). 필드 위 우클릭 회수는 그대로입니다.
- **다시 알림:** `HUD.prefab`의 `descriptionPanel`·`descriptionText`가 아직 비어 있어 아이템·포효 카운터·격파 목표 설명이 화면에 안 나옵니다(10-15 메모). 연결하면 아이템 아이콘 호버에 이름·설명이 나옵니다.
- **알아 두실 점:** 아군 아이템 아이콘은 이제 Raycast 대상이라, 그 아이콘 위에서의 왼쪽 클릭은 뒤쪽 필드로 가지 않습니다(설명 가능한 아이콘과 같음).

## 4. 추가(2026-10-20): 밤 진입 배치 확인 시험 가짜 — 컴파일 맞춤

- `CombatDeploymentConfirmationTests`의 시험 가짜 `CharacterSource`가 천명님 `16333690`에서 바뀐 `ICharacterBufferReadSink`(새 `RosterRevision`, `TryGetMainState`가 `CharacterInstanceId`를 받는 서명으로 바뀜)를 따라가지 못해 시험 어셈블리 전체가 컴파일되지 않아, 사용자 지시로 그 두 멤버만 맞췄습니다(배치 확인은 `Owned`·`Characters`만 읽어 다른 안 쓰는 멤버처럼 `NotSupportedException`, 시험 본문·단언은 그대로).
