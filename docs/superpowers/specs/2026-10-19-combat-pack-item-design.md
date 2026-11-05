# 전투 가방·아이템 표시와 장착 설계 (2026-10-19)

- **목표:** 배치 국면에 가방(`PackUI`)에서 보유 아이템과 수량을 보이고, 가방에서 유닛으로 끌어 장착하며, 장착한 아이템을 유닛 HUD 아이콘으로 보인다. Core가 사용(발동)을 판정하면 그 순간 HUD에서 지운다.
- **상태:** 설계 작성(코드 변경 없음). **결정 완료(2026-10-19): Q-P1~Q-P4 모두 A(§6)** — 표시 경유, 사실 기반 가능/불가 강조, 아이콘은 격파 목표 다음, Phase 넷(P-1~P-4). Task 계획: [pack-item-plan](../plans/2026-10-19-combat-pack-item-plan.md).
- **시험 전제:** 개발 시작 아이템([dev-starting-items-design](2026-10-18-dev-starting-items-design.md), I-0~I-2 완료·Unity 확인)으로 재고를 넣는다. Fixture 전투에서도 장착할 수 있다.

## 0. 사용자 요구와 결정 (2026-10-19)

**요구:**
1. 스테이지 진입 시 가방에 지금 보유한 아이템과 개수를 보인다.
2. 유닛에게 장착하면 개수가 줄고, 0 이하면 아이템을 보이지 않는다.
3. 유닛에게 장착한 아이템은 유닛 HUD 버프 아이콘으로 보인다.
4. 가방에서 아이템을 끌어 유닛 위에 놓으면 그 유닛에게 장착한다.
5. 유닛을 우클릭해 손패로 되돌리면 장착이 풀리고 재고가 갱신된다.
6. Core가 아이템 사용을 판정하면 그 유닛 HUD에서 지운다.

**결정:**
1. 이미 장착한 유닛에 놓으면 교체한다(`ReplaceItem`, 이전 아이템은 가방으로).
2. 해제는 HUD 아이템 아이콘 우클릭이다(`UnequipItem`). 메인은 손패로 회수할 수 없어서 필요하다.
3. HUD 제거는 사건 순간이다(`CombatItemEvent`).
4. 아이콘·이름·설명은 새 View 외형 표에 둔다.
5. 가방은 씬에 깔린 고정 칸을 키 순으로 앞에서부터 채운다. **2026-10-19 수정:** 처음 조사에서 "24칸"이라 한 것은 프리팹 GUID 참조 수를 잘못 센 것이었다(실제 1칸). 기획 답변(최대 8칸)에 따라 사용자가 씬에 8칸을 넣었다(세로 레이아웃 그룹 + 크기 맞춤). 8종을 넘으면 넘침 경고다.
6. 적이 장착한 아이템도 보인다. 적 아이콘 해제는 막는다.
7. 다른 유닛에 남는 효과(랜턴·로프) 표시는 이번 범위 밖이다.
8. 라운드가 끝나면 쓰지 않은 장착 아이템을 버리는 Core 규칙은 그대로 둔다.

**원칙(2026-10-19 확인):** 재고는 PackUI가 직접 읽지 않는다. `CombatViewSystem`이 Buffer에서 읽어 넘기고, 수량은 놓는 순간이 아니라 Core 결과가 온 뒤에 바뀐다.

## 1. 지금 구조 (코드 확인, HEAD `8c34b864` + 시작 아이템 작업)

| 대상 | 지금 |
|---|---|
| 재고 원본 | Buffer `ItemInventoryReadSink.Current`(불변 스냅숏, 바뀔 때만 새 객체). View 코드는 아직 읽지 않는다 |
| 재고가 바뀌는 때(전투 안) | 플레이어 명령 결과뿐이다: 장착 -1, 교체 ±1, 해제 +1, 장착 유닛 손패 회수 +1(`CombatPlacementCalculator` → `RefundReturn`). 발동·라운드 끝·사망·전투 끝은 장착 칸만 비운다 |
| 장착 칸 | `CharacterData.Items.EquippedItemKey`(Buffer), 재생 중에는 `CombatShownFacts`의 `CharacterCombatFacts.Items` |
| 장착 명령 | `CombatViewCommandData.EquipItem(id, key)`·`ReplaceItem(id, key)`·`UnequipItem(id)`. Core는 배치 국면·플레이어 입력·필드의 아군 메인/서브만 받는다 |
| **입력 정책** | `CombatInputPolicy.StageOf(CombatViewCommandKind)`가 아이템 명령 셋을 모른다. 지금 그대로 `CanSubmit`을 부르면 `ArgumentOutOfRangeException` |
| 표시 값 | `CombatViewSystem.ReadDisplayValues` → `CombatDisplayValues` → `CombatSceneDisplay.Install`(화면 설치)·`Refresh`(결과 후처리마다, 거절 포함). 골드는 이 길로 `CombatHud`에 간다 |
| 가방 UI | `PackUI`(`UIId.Pack`, 현강님 `aaac7c60`): 토글·펼침(`PackExpansion`)·설명 패널·드래그 사각형(`dragRect`). 표시 여부는 `CombatViewSystem`이 행동 버튼 갱신 때 `Pack.Refresh(facts, suspended)`로 넘긴다(배치 국면에만 보임). 재고는 읽지 않는다. 목록 칸은 `Item.prefab`(Image + Button, 스크립트·수량 글자 없음) 24개가 씬에 미리 깔려 있다 |
| 손패 카드 드래그(본보기) | UI `DeckCardDragBinding.OnBeginDrag` → `ICombatViewService.DeckPlacement.Select(id)`(오류 경계 감쌈, `CombatDeckPlacementInput`) → `PlacementDragInteraction` 드래그 → 포인터 이동·뗌은 입력 시스템 → `CombatInputRouter` → 놓으면 Gate로 명령. 배치 대상은 UI 위에서 뗀 입력을 무시한다(`blockedByUi`) |
| 포인터 대상 판정 | `CombatInputRouter.ResolvePointerTarget(layer, attackActive)` 한 곳. 배치 Layer = Placement |
| 우클릭 | 입력 시스템 "취소"(`<Mouse>/rightButton`) → Router `Cancel` → 배치 국면이면 기억한 포인터 칸의 아군 서브를 손패로 회수(`TryReturnPlacedUnit`). **포인터가 UI 위인지 보지 않는다** |
| UI 클릭 연결 | `UIInputBinding`은 왼쪽 클릭·Submit·호버만 받는다(`IUIInputReceiver.Click/HoverEnter/HoverExit`) |
| 유닛 HUD 상태 줄 | `FieldUnitPresenter.RenderUnitHud`가 사실에서 상태 목록(격파 목표 → 특성)을 만들어 `UnitHUD.SetStatuses`(키·아이콘·설명)에 넘긴다. 아이콘이 없으면 HUD 기본 아이콘. 특성 외형 표는 표시 Factory가 `units.Configure(…, tables.Traits, …)`로 넘긴다 |
| 아이템 사건 | `CombatItemEvent`(아이템 키·소지자·대상·단계·사유·이전/새 장착 키). 사유: 발동·중복 효과·장착·해제·교체·손패 회수·사망·라운드 끝·전투 끝. **재생 경로 표에 없어** 경고 후 건너뛴다(장착할 때마다 경고) |
| 재생 스케줄러 | 사건을 동시 묶음(피해 근원 열쇠)으로 나눈다. 주체가 없거나 근원이 없는 사건은 묶음 밖(-1)이고 **장벽**이다(뒤 사건이 지나가지 못함). 건너뛰는 사건은 장벽이 아니다 |
| 표시 사실 복사 | Core 공개 도우미 `ItemCharacterChanges.WithItems(facts, items)`(`ProjectL.Core.Application`, public)가 장착 칸만 바꾼 사실 사본을 만든다 |
| 외형 표 본보기 | `TraitVisualData`(키·표시 이름·아이콘(비워도 됨)·설명) → 표 → `ViewTableKey` → `IViewQuery` → `ViewDataBaseFactory` → `CombatDisplayTables` |
| 아이템 그림 | 프로젝트에 아이템 아이콘이 없다. InGame SO에는 이름(위스키·로켓·밧줄·랜턴)과 소개글이 있다 |

## 2. 설계

### P1 재고 값 경로 (원칙, Q-P1)

- `CombatViewSystem`이 `ItemInventoryReadSink`를 바인딩 때 받는다. `ReadDisplayValues`가 `CombatDisplayValues.ItemInventory`(스냅숏)를 채운다.
- `CombatSceneDisplay`가 그리기에서 가방 표시 계약 `ICombatPackDisplay.ShowInventory(snapshot)`을 부른다(골드 → `CombatHud`와 같은 자리). PackUI가 이 계약을 구현한다.
- 그리는 때는 이미 있는 두 곳이다: 화면 설치(`Install`), 결과 후처리(`Refresh`, 거절 포함). 전투 안 재고 변화가 모두 명령 결과라 이것으로 충분하다. 매 프레임 읽지 않는다.
- PackUI는 같은 스냅숏 객체면 다시 그리지 않는다(Buffer는 바뀔 때만 새 객체로 바꾼다).
- 표시 여부(`Pack.Refresh(facts, suspended)`, 현강님 연결)는 그대로 둔다. 보이는지는 입력 사실(행동 버튼 시점), 무엇을 담는지는 표시 값(결과 시점)이 정한다.

### P2 가방 칸 그리기

- **칸 부품 `PackItemSlotView`(새, `Item.prefab`에 붙임):** 아이콘 Image·수량 글자·Button·드래그 시작 연결. `Show(visual, count)`/`Hide()`.
- **채우기:** 스냅숏의 `Counts`를 키 순으로 정렬해 앞 칸부터 채우고 남은 칸은 숨긴다. 0개 항목은 스냅숏에 없으므로 따로 거르지 않는다.
- **종류가 칸 수(24)를 넘으면:** 넘친 종류를 그리지 않고 경고 한 번(Debug.LogWarning). 지금은 4종이다. 오류로 멈추지 않는 이유: 표시 한계가 전투 진행을 막으면 안 된다.
- **외형:** 키로 외형 표를 찾는다(P3). 아이콘이 없으면 칸 프리팹의 기본 그림, 이름이 없으면 키 숫자.
- **설명 패널:** 칸에 호버하면 기존 `descriptionPanel`에 이름·설명을 보인다. 패널 글자 칸이 프리팹에 필요하다(§3 에디터 작업).
- **수량 표시 형식:** `×2`. 1개도 `×1`로 보인다.

### P3 아이템 외형 표 `ConsumableItemVisual` (View DB)

- **Data:** `itemKey`(int, InGame 소모 아이템 키와 같음) · `displayName` · `icon`(Sprite, 비워도 됨) · `description`(TextArea). `Validate`: 키 1 이상. `TraitVisualData`와 같은 꼴이다.
- **SO·표 SO·조회:** `ConsumableItemVisualDataSO`·`ConsumableItemVisualDataTableSO`·`ConsumableItemVisualTable : IConsumableItemVisualTableQuery`(`Get`/`TryGet`, 중복 키 거절).
- **등록:** `ViewTableKey.ConsumableItemVisual`, `IViewQuery.ConsumableItemVisualTableQuery`, `ViewDataBaseFactory`, `ViewGroup.asset`, 에셋 `DataBaseSO/ViewGroup/ConsumableItemVisual/`(4개).
- **처음 값:** 이름·설명은 InGame SO 글을 옮기고, 아이콘은 비운다.
  - 이름·설명이 InGame과 겹친다(결정 ④). 천명님(InGame 아이템)·호진님(DB)께 알리고, InGame 쪽 글을 남길지는 그분들이 정한다.
- **키 대조:** `DataBaseAssetTests`에 "View 외형 표의 키 = InGame 소모 아이템 키" 시험을 더한다(시험은 두 그룹을 읽어도 된다).
- **사용처:** `CombatDisplayTables.Items`(필수) → 표시 Factory가 `FieldUnitPresenter.Configure`와 가방 표시에 넘긴다.

### P4 드래그 장착 입력

- **새 입력 기능 `CombatItemDragInteraction`:** `CombatInteraction`의 넷째 기능(배치·공격·경로와 나란히). 드래그 상태(집은 키·포인터)를 소유하고, 명령은 Gate로 제출하며 대기는 기록하지 않는다.
- **입구 `ICombatItemDragInput`(새, `ICombatViewService.ItemDrag`):** `CanSelect(itemKey)`·`Select(itemKey)`·`Cancel()`. 손패 카드의 `CombatDeckPlacementInput`처럼 수명·오류 경계로 감싼다. PackUI 칸의 드래그 시작이 `Select`를 부른다.
- **집을 수 있는 때(`CanSelect`):** `CombatInputPolicy.CanSubmit(facts, pending, EquipItem)` + 가방 표시 재고에 그 키가 1 이상. 배치 드래그 중이면 집지 않는다.
- **포인터 대상:** `ResolvePointerTarget(layer, attackActive, itemDragActive)`에 아이템 드래그 중이면 `PointerTarget.Item`(배치 Layer일 때)을 더한다. 판정식은 지금처럼 한 곳이다.
- **움직이는 동안:**
  - 포인터 칸을 강조한다. 아군 필드 메인·서브가 있으면 가능 색, 없으면 불가 색이다(Q-P2).
  - 사실만 보고 "놓을 대상이 있다"를 알리는 표시이고, 장착 가부는 Core가 판정한다.
  - PackUI `dragRect`가 포인터를 따라간다(표시 계약 `ICombatItemDragFeedback.ShowDrag/MoveDrag/HideDrag`, PackUI 구현).
- **뗄 때:** 포인터 칸의 아군 필드 메인·서브를 찾는다.
  - 장착 칸이 비었으면 `EquipItem(id, key)`, 차 있으면 `ReplaceItem(id, key)`(결정 ①). 같은 키 교체는 Core가 변화 없이 받는다.
  - 대상이 없거나 UI 위에서 떼면 취소한다.
  - 어느 경우든 드래그를 끝낸다. 가방 수량은 결과가 온 뒤 P1로 바뀐다.
- **취소:** 우클릭·Esc(Router `Cancel`)는 아이템 드래그를 먼저 끝낸다. 포커스 상실·선택 취소·Layer 변경·정지는 `CombatInteraction`의 기존 정리 메서드에 아이템 기능을 더한다.
- **배타:**
  - 아이템 드래그를 시작하면 배치 표현(호버·고스트)을 정리한다.
  - 손패 카드를 집으면 아이템 드래그를 끝낸다.
  - 공격 모드는 전투 국면이라 겹치지 않는다.
- **결과 뒤(`AfterResult`):** 배치 국면이 아니거나 그 키의 표시 재고가 0이면 드래그를 끝낸다.
- **정책 보완:** `CombatInputPolicy.StageOf`에 `EquipItem`·`UnequipItem`·`ReplaceItem` → 배치 국면을 더한다.

### P5 유닛 HUD 장착 아이콘

- `FieldUnitPresenter.RenderUnitHud`가 사실의 `Items.EquippedItemKey`가 있으면 상태 하나를 더한다.
  - 키 `"Item"`, 아이콘·설명은 외형 표에서 가져오고, 설명은 "이름 + 설명"이다.
  - 자리는 격파 목표 다음, 특성 앞이다(Q-P3).
- 아군·적 모두 같은 규칙이다(결정 ⑥).
- 재생 중에는 `CombatShownFacts`의 사실, 결과 끝에는 최종 사실을 따른다. 그리는 길이 하나라 따로 맞출 것이 없다.

### P6 HUD 아이콘 우클릭 해제

- **새 공용 UI 연결 `UISecondaryClickBinding` + `IUISecondaryClickReceiver.SecondaryClick(slot)`**(`UISystem/Base`): 오른쪽 클릭만 받는다. 기존 `UIInputBinding`·`IUIInputReceiver`는 바꾸지 않는다. 바꾸면 모든 수신자가 깨지기 때문이다.
- **UnitHUD:** 상태 아이콘에 이 연결을 붙인다. 우클릭하면 주입받은 콜백에 (개체 ID, 상태 키)를 넘긴다. 콜백이 없으면 아무것도 하지 않는다.
- **FieldUnitPresenter:** 표시 Factory가 준 콜백을 HUD에 건다. 상태 키가 `"Item"`일 때만 아이템 해제 입구로 보낸다.
- **해제 입구(`ICombatItemDragInput.RequestUnequip(entityId)` 또는 별도 입구, 계획에서 확정):**
  - 오류 경계 안에서 확인한다: `CanSubmit(UnequipItem)` + 아군 필드 + 장착 칸 있음.
  - 맞으면 `UnequipItem(id)`을 Gate로 제출한다. 적이면 무시한다(결정 ⑥).
- **필드 우클릭 회수와 겹침 막기:** HUD 위 우클릭은 입력 시스템 "취소"로도 들어간다. 기억한 포인터가 UI 위면 Router `Cancel`이 필드 회수(`TryReturnPlacedUnit`)를 하지 않게 한다. 지금도 UI 위 우클릭이 뒤쪽 칸의 서브를 회수할 수 있는 문제이고, 같이 고친다.

### P7 아이템 사건의 사건 순간 표시 (결정 ③)

- **경로:** `CombatEventRoutes.EventTypes`에 `CombatItemEvent`를 더한다(사실 처리기 형식). 경고가 없어진다.
- **사실 반영:** `CombatFactEventHandler.ApplyFacts`에 `CombatItemEvent` 경우를 더한다.
  - 소지자의 보인 사실을 `ItemCharacterChanges.WithItems(holder, new CharacterItemState(e.NewItemKey, holder.Items.Effects))`로 바꾼다.
  - 그 뒤 틀의 다시 그리기가 HUD를 고친다.
  - View는 장착 키를 계산하지 않고 사건 값(`NewItemKey`)을 옮기기만 한다.
- **모든 사유가 같은 규칙이다:** 발동·중복 효과(소모 → 칸 비움), 장착·교체·해제·회수(새 키), 사망·라운드 끝·전투 끝(버림 → 칸 비움, 결정 ⑧). 대상 쪽 남는 효과는 그리지 않는다(결정 ⑦).
- **동시 묶음을 깨지 않게(중요):**
  - 아이템 사건은 지금 주체·근원이 없어 묶음 밖(장벽)이 된다. 그대로 경로 표에 넣으면, 여러 대상 공격에서 A 피격 → A 위스키 발동 → B 피격 순서일 때 B 피격이 A 반응이 끝날 때까지 기다린다(동시 처리 C-2 동작 후퇴).
  - 그래서 두 가지를 정한다.
    - `CombatEventSubjects.TryGetSubject`: `CombatItemEvent` → `HolderId`.
    - `CombatConcurrentGroups`: 아이템 사건은 같은 행동 구간에서 가장 가까운 같은 소지자의 묶음 사건과 같은 묶음에 넣는다(앞을 먼저, 없으면 뒤). 없으면 지금처럼 묶음 밖.
  - 붙일 방향은 단계로 정한다: 피해 앞 단계(로켓)는 뒤 사건을 먼저, 그 밖(위스키·랜턴·사망 버림)은 앞 사건을 먼저 본다.
  - **회복 사건도 함께 붙인다(P-3 구현 때 코드 대조로 보완):** 위스키는 Core가 "피해 체력 사건(근원 있음) → 아이템 사건 → 회복 체력 사건(근원 없음)" 순서로 기록한다(`CombatDamageApplication.Apply`). 회복 사건은 지금도 묶음 밖 장벽이라, 발동 사건 바로 뒤에 오는 같은 소지자의 근원 없는 캐릭터 사건도 같은 묶음에 넣는다.
  - 같은 묶음·같은 주체는 차례로 돌고(소지자 반응 뒤 발동), 다른 주체는 함께 돈다.
  - 근거는 사건 사실(소지자·행동 구간·순서)뿐이다. 칸·이름으로 추측하지 않는다.
- **시험:** 순수 시험으로 지킨다.
  - 다중 피격 사이의 위스키 발동이 다른 대상 피격을 막지 않는다.
  - 로켓(피격 앞)·랜턴(피격 뒤)·밧줄(공격 끝, 묶음 사건 없음 → 묶음 밖)
  - 라운드 끝 버림(묶음 밖)

## 3. 바뀌는 파일

| 영역 | 파일 | 변경 | 소유·공유 |
|---|---|---|---|
| 정책 | `Combat/CombatInputPolicy.cs` | 아이템 명령 셋 → 배치 국면 | View |
| 값 경로 | `CombatViewSystem.cs`·`CombatDisplayValues.cs`·`CombatSceneDisplay.cs` | 재고 스냅숏 전달, 아이템 드래그·해제 입구 | View |
| 입력 | `CombatInteraction.cs`·`CombatInputRouter.cs`·새 `CombatItemDragInteraction.cs`·새 `ICombatItemDragInput.cs`(+ 오류 경계 감싼 구현)·`ICombatViewService.cs`·`PlacementDragInteraction.cs`(기억 포인터 읽기) | P4·P6 | View |
| 조립 | `Factory/Runtime/InGame/View/CombatSceneDisplayFactory.cs`·`CombatSceneInputFactory.cs` | 가방 표시·드래그 표시 계약, 외형 표, HUD 해제 콜백 연결 | 민형님 공유(Factory 공유 문서에 절 추가) |
| 재생 | `CombatEventRoutes.cs`·`CombatFactEventHandler.cs`·`CombatEventSubjects.cs`·`CombatConcurrentGroups.cs` | P7 | View |
| 유닛 HUD | `Display/FieldUnitPresenter.cs` | 아이템 상태·해제 콜백 | View |
| UI | `Screens/Stage/PackUI.cs`·새 `PackItemSlotView.cs`·`Screens/Stage/UnitHUD.cs`·새 `Base/UISecondaryClickBinding.cs` | P2·P6 | 현강님 공유 메모(새로 작성) |
| DB | 새 `Data/View/Table/ConsumableItem/…`(Data·SO·표 SO)·새 `Queries/ViewDataBase/ViewTable/ConsumableItemVisualTable.cs`·`ViewDataBase.cs`(IViewQuery)·`ServiceKeys/DBFactory/Group/ViewTableKey.cs`·`Factory/DBFactory/Group/ViewDataBaseFactory.cs`·`Combat/CombatDisplayTables.cs` | P3 | 민형님(키·Factory)·호진님(DB) 공유 |
| 에셋 | 새 `DataBaseSO/ViewGroup/ConsumableItemVisual/`(표 + 4개), `ViewGroup.asset` | P3. **코드와 한 커밋**(없으면 DB 생성 실패) | 호진님 공유 |
| 프리팹·씬(사용자 에디터) | `Item.prefab`(칸 부품·수량 글자), `Combat.unity`의 PackUI(설명 패널 글자 칸 참조) | P2. 절차는 계획에 적는다 | 현강님 공유 |
| 시험 | 새 순수 시험(드래그 입력·가방 채우기·외형 표·사건 묶음·정책), `DataBaseAssetTests`(키 대조) | | |

Core 파일은 바꾸지 않는다. `ItemCharacterChanges.WithItems`는 호출만 한다.

## 4. 원칙 점검

기준은 [View 설계 세션 인계 §4](../handoffs/2026-09-30-combat-view-design-session-handoff.md)다.

### 4.1 SSOT

| 사실 | 원본 | 이 설계 |
|---|---|---|
| 보유 수량 | Buffer 재고 | 가방은 받은 스냅숏을 그리기만 한다. 사본·예측 차감이 없다 |
| 장착 칸 | Buffer 캐릭터 사실(재생 중 = 보인 사실) | HUD는 사실에서 그린다. 사건 순간 반영도 보인 사실 한 곳을 바꾼다 |
| 장착 가부·교체 규칙 | Core | View는 명령 종류만 고른다(빈 칸 = 장착, 찬 칸 = 교체). 거절은 Core 사유(로그) |
| 아이템 외형·글 | View 외형 표 | InGame 이름·설명과 겹친다(결정 ④, 알림) |
| 아이템 사건이 보이는 길 | 경로 표 | 한 줄 더함 |
| 같은 순간 | Core 피해 근원 + 사건 순서 | 아이템 사건은 같은 소지자의 묶음에 붙는다(P7). 새 추측이 없다 |

### 4.2 SOLID·FW·DB

- **S:**
  - 가방 칸 그리기는 PackUI·칸 부품, 드래그 상태는 입력 기능 하나, 명령 제출은 Gate가 맡는다.
  - HUD 해제는 UI 연결(눌림) → 표시(키 판별) → 입력 입구(정책·제출)로 나눈다.
- **O:**
  - 입력 기능은 `CombatInteraction`에 하나를 더하고, 사건은 경로 표에 한 줄을 더한다.
  - 우클릭은 새 연결로 더하고 기존 `UIInputBinding`은 바꾸지 않는다.
- **I·D:** PackUI는 `ICombatPackDisplay`·`ICombatItemDragFeedback`만 구현하고 Buffer를 모른다. 입력 기능은 Gate·포인터 집기·피드백 계약에만 기댄다.
- **FW:**
  - 새 서비스 등록은 없다(입구는 기존 `ICombatViewService` 멤버).
  - DB 표는 FW 표 규약(`Get`/`TryGet`, `IExportSystemServices`)을 따르고, 등록은 기존 Factory 한 곳이다.
  - UI 입력은 구독이 아니라 명시 연결이다(DR-008 무관).
- **DB P5:** View 표는 InGame과 같은 키(int 아이템 키)로 찾는다. View는 InGame을 읽지 않는다.

### 4.3 기능 변경의 독립성 (기준 §4.6)

| 변경 시나리오 | 수정할 곳 | 검증 |
|---|---|---|
| 가방 칸 모양·수량 글자 형식 | `PackItemSlotView`·PackUI | 가방 채우기 시험 |
| 드래그 규칙(대상 강조·교체 선택) | `CombatItemDragInteraction` | 드래그 입력 시험 |
| 아이템 아이콘·이름 | 외형 표 에셋 | 키 대조 시험 |
| 사건 묶음 규칙 | `CombatConcurrentGroups` | 동시 묶음 시험(기존 + 아이템 사례) |
| 우클릭 연결 | `UISecondaryClickBinding`·UnitHUD | HUD 시험·재생 |

공통으로 남는 경계는 둘이다.
- "표시 값이 언제 그려지나": `CombatViewSystem`의 설치·결과 후처리
- "포인터가 누구에게 가나": Router 판정식

## 5. 범위 밖

- 다른 유닛에 남는 효과(랜턴 방어 -5, 로프 우선순위 -10) 표시(결정 ⑦).
- 거절 안내 UI: 지금처럼 로그만 남긴다(`OccupiedItemSlot`·`ItemUnavailable` 등).
- 발동 연출(이펙트·소리·숫자). 지금은 HUD 아이콘이 사라지는 것과 체력 변화(기존 피해·회복 사건)만 보인다.
- 상점·보상 지급, 운영 시작 아이템(Core 요청 사항).
- 전투 국면의 가방 표시(지금 PackUI 규칙대로 배치 국면에만 보인다).

## 6. 정할 것

**결정(2026-10-19, 사용자): 네 질문 모두 A.**

| 번호 | 질문 | 선택지 | 추천 |
|---|---|---|---|
| Q-P1 | 재고를 가방까지 어느 길로 넘길까? | **A** 표시 경유: `CombatSceneDisplay`가 그리기 때 `ICombatPackDisplay`를 부른다. 표시 Factory에 가방 참조가 더해져 민형님 공유 / **B** 조정자 직접: `CombatViewSystem`이 표시 갱신 뒤 `_sceneRoot.Pack`에 바로 넘긴다. Factory는 그대로 | **A.** 골드·HUD와 같이 "그리기는 표시가" 규칙을 지킨다. B면 조정자가 UI 하나를 더 알게 된다 |
| Q-P2 | 드래그 중 포인터 칸 강조를 어떻게 할까? | **A** 아군 필드 메인·서브 위면 가능 색, 아니면 불가 색(사실만 봄) / **B** 중립 색 하나 | **A.** 놓을 수 있는 곳이 바로 보인다. 장착 가부는 여전히 Core가 판정한다 |
| Q-P3 | HUD 상태 줄에서 아이템 아이콘 자리는? | **A** 격파 목표 다음, 특성 앞 / **B** 맨 끝 | **A.** 바뀔 수 있는 상태(아이템)가 고정 특성보다 먼저 보인다 |
| Q-P4 | 작업을 어떻게 나눌까? | **A** 넷: P-1 외형 표·가방 표시(재고 읽기) → P-2 드래그 장착(정책 포함) → P-3 HUD 아이콘·사건 경로·동시 묶음 → P-4 HUD 우클릭 해제·우클릭 겹침 막기 / **B** 둘: 표시(P-1+P-3) → 입력(P-2+P-4) | **A.** 단계마다 화면에서 바로 확인할 수 있다. 각 단계 뒤 Unity 확인이 쉽다 |

## 7. 확인 방법

- MSBuild 오류·새 경고 0, 관리 실행 기존 시험 상태 변화 0(Phase마다).
- **새 순수 시험:**
  - 정책: 아이템 명령 단계
  - 가방 채우기: 키 순, 0 숨김, 24 초과 경고, 같은 스냅숏 다시 그리지 않음
  - 드래그 입력: 집기 조건, 빈 칸 = 장착, 찬 칸 = 교체, 대상 없음 취소, UI 위 뗌 취소, 배타
  - 사건: 경로·사실 반영·동시 묶음
  - 외형 표 검사
- **Unity:** DB 생성·키 대조, HUD·PackUI 시험, Boot·Combat 진입.
- **재생(사용자, 시작 아이템 `1×2`·`4×1`):**
  1. 진입 시 가방에 위스키 ×2·랜턴 ×1
  2. 위스키를 서브에 끌어 놓음 → 가방 ×1, HUD 아이콘
  3. 다른 아이템을 같은 유닛에 놓음 → 교체(가방 수량 맞음)
  4. 아이콘 우클릭 → 해제(가방 +1)
  5. 장착 유닛 우클릭 회수 → 아이콘 없어짐·가방 +1
  6. 전투에서 발동 → 그 순간 아이콘 사라짐
  7. 라운드 끝 → 쓰지 않은 아이콘 사라짐(가방 그대로)
  8. 메인 장착·해제
