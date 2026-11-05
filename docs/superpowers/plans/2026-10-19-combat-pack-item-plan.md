# 전투 가방·아이템 계획

- 설계: [pack-item-design](../specs/2026-10-19-combat-pack-item-design.md)(P1~P7). 결정 Q-P1~Q-P4 모두 A(2026-10-19).
- 작성: 2026-10-19(사용자 지시 "Q-P1~Q-P4 모두 A로 정하고 Task 계획 써줘").
- 경로 약어:
  - `C/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/`
  - `D/` = `C/Display/`
  - `U/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/UISystem/`
  - `V/` = `Assets/ProjectL/Script/Systems/DataBaseSystem/`
  - `F/` = `Assets/ProjectL/Script/Systems/Factory/`
  - `T/` = `Assets/Tests/EditMode/Framework/`
- 위치는 메서드 이름으로 적는다.
- **사용자 결정(2026-10-19, P-1 착수 때):** "MSBuild·관리 실행·Unity 확인 목록은 일단 건너뛰고 빠르게 작업 진행". §1의 Phase마다 확인을 이번에는 하지 않는다. 시험 코드는 쓰되 돌리지 않는다. 컴파일·시험 결과는 Unity에서 처음 확인된다.

## 0. 착수 조건과 기준선 (P-0)

1. **기준 코드:** 시작 아이템 I-0~I-2가 들어간 지금 작업 트리(Unity 확인 완료). 사용자가 커밋해도 코드가 같으면 기준선은 그대로다.
2. **관리 실행 기준선:** 시작 아이템 계획 §5.4의 I-2 최종 실행(5,445개: PASS 3,033 · NATIVE 2,271 · FAIL 92 · UNSUPPORTED 49)을 쓴다. 착수 전 `git status`로 코드가 바뀌었는지 보고, 바뀌었으면 다시 돌린다.
3. **도구:** 이 세션 스크래치의 `tool/`(Runner·build·runeach)를 쓴다. 다음 세션이면 시작 아이템 계획 §5.1 방법으로 다시 복사한다.
4. **csproj:** 새 파일은 Unity가 csproj를 다시 만들기 전까지 extra.targets로 더한다. `Assembly-CSharp-Editor`는 바뀌지 않는다(이번 작업에 에디터 코드가 없음).

## 1. 공통 규칙

- **새 코드:** 경로·네임스페이스는 폴더를 따르고, Allman, `sealed`·`internal` 기본, 한국어 XML doc이다. `.meta`는 32자리 GUID로 새로 만든다.
- **Core 파일은 바꾸지 않는다.** `ItemCharacterChanges.WithItems`·`CharacterItemState`는 호출만 한다.
- **기존 시험 호환:**
  - `CombatViewSystem` 시험 생성자·`FieldUnitPresenter.Configure`·`CombatInteraction`·`CombatSceneDisplay`에 더하는 인자는 끝에 선택 인자로 둔다.
  - `CombatDisplayTables`는 필수 인자로 더하고 직접 만드는 시험 두 파일(`CombatSceneEventPresenterTests`·`TerrainOperationalDisplayTests`)을 고친다.
- **프리팹·씬은 사용자가 에디터에서 연결한다.** 절차는 Phase마다 적는다. 코드는 연결이 빠졌을 때 무엇이 빠졌는지 알리는 예외로 멈춘다(지금 PackUI 방식).
- **공유:** 현강님(UI·프리팹), 민형님(Factory·ServiceKeys), 호진님(View DB 표·에셋)께 Phase마다 해당 절을 쓴다. 문서는 §6에 있다.
- **Phase마다 확인:**
  - MSBuild 오류 0, 새 경고 0
  - 관리 실행 기존 시험 상태 변화 0(바꾼 시험은 이유를 적음), 새 순수 시험 PASS
  - Unity Test Runner 확인 목록과 사용자 재생 확인 목록
- Phase마다 멈추고 보고한다.

## 2. P-1 — 외형 표와 가방 표시 (요구 1·2의 표시, 설계 P1·P2·P3)

### T1.1 View 외형 표 (새)

- **`V/Data/View/Table/ConsumableItem/Item/ConsumableItemVisualDataSO.cs`:**
  - `ConsumableItemVisualData`: `itemKey`(int) · `displayName` · `icon`(Sprite, 비워도 됨) · `description`(TextArea)
  - `DisplayName`은 비면 키 숫자, `Validate`는 키 1 이상
  - SO는 `GetData()`가 Data를 돌려준다. `TraitVisualData`와 같은 꼴이다.
- **`V/Data/View/Table/ConsumableItem/ConsumableItemVisualDataTableSO.cs`:** 표 SO.
- **`V/Queries/ViewDataBase/ViewTable/ConsumableItemVisualTable.cs`:** `IConsumableItemVisualTableQuery`(`Get(int)`·`TryGet(int, out data)`). 생성자에서 항목마다 `Validate`, 중복 키 거절. `IExportSystemServices`.
- **등록:**
  - `ServiceKeys/DBFactory/Group/ViewTableKey.cs`의 `ConsumableItemVisual`
  - `V/Queries/ViewDataBase/ViewDataBase.cs`의 `IViewQuery.ConsumableItemVisualTableQuery`와 대입
  - `F/DBFactory/Group/ViewDataBaseFactory.cs` 등록

### T1.2 외형 에셋 (새)

- **`DataBaseSO/ViewGroup/ConsumableItemVisual/`:** `ConsumableItemVisualDataTable.asset` + `Item/Whiskey·Roket·Rope·Lantern.asset`.
  - 이름·설명은 InGame SO에서 옮긴다: 위스키·로켓·밧줄·랜턴과 소개글. 아이콘은 비운다.
- `ViewGroup.asset`의 표 목록에 더한다. **코드와 한 커밋**(없으면 DB 생성 실패).

### T1.3 전투 표시 표 묶음

- `C/CombatDisplayTables.cs`에 `Items`(필수)를 더하고 `From`에서 채운다.
- 직접 만드는 시험 두 파일을 고친다.

### T1.4 재고 값 경로 (설계 P1, Q-P1 A)

- **`C/CombatViewSystem.cs`:**
  - `IItemInventoryReadSink _items`. 운영 바인딩에서 Buffer의 것을 받고, 시험 생성자에는 끝에 선택 인자로 둔다(null이면 빈 재고).
  - `ReadDisplayValues`가 재고를 넘긴다.
- **`C/CombatDisplayValues.cs`:** 선택 인자 `itemInventory`와 `ItemInventory`(없으면 `ItemInventorySnapshot.Empty`).
- **새 계약 `C/ICombatPackDisplay.cs`:** `ShowInventory(ItemInventorySnapshot)`. 드래그 표시 메서드는 P-2에서 더한다.
- **`C/CombatSceneDisplay.cs`:** 생성자 끝에 선택 인자 `ICombatPackDisplay pack`. 그리기(설치·갱신 공통)에서 `_hud.Render` 다음에 `_pack?.ShowInventory(values.ItemInventory)`.
- **`F/Runtime/InGame/View/CombatSceneDisplayFactory.cs`:** `root.Pack`에 외형 표를 넘기고(`ConfigureItems(tables.Items)`) 표시에 가방을 넘긴다.

### T1.5 가방 칸 채우기

- **새 순수 부품 `U/Screens/Stage/PackInventoryLayout.cs`:** `Build(ItemInventorySnapshot, int capacity)` → 키 순 (키, 수량) 목록 + 넘친 종류 수. Unity를 쓰지 않는다.
- **새 칸 부품 `U/Screens/Stage/PackItemSlotView.cs`(MonoBehaviour, `Item.prefab`에 붙임):** `icon`(Image)·`countText`(TMP_Text)·`button`, `Show(Sprite, int count)`/`Hide()`. 아이콘이 없으면 칸의 처음 그림을 쓴다.
- **`U/Screens/Stage/PackUI.cs`:**
  - `ICombatPackDisplay` 구현, `ConfigureItems(IConsumableItemVisualTableQuery)`.
  - 칸 목록은 바인딩 때 `expansion` 안의 `PackItemSlotView`를 순서대로 모은다(0개면 참조 누락 예외).
  - `ShowInventory`:
    - 같은 스냅숏 객체면 건너뛴다.
    - `PackInventoryLayout`으로 칸을 채우고 남은 칸은 숨긴다.
    - 넘치면 경고 한 번 남긴다.
  - 수량 형식은 `×n`.
  - 칸 호버(`UIInputBinding` hover, 칸 슬롯 번호는 1부터, 0은 토글)에 설명 패널의 이름·설명을 보인다. 설명 글자 칸 `descriptionText`(TMP_Text)를 직렬화 필드로 더한다.
  - 칸 클릭은 P-2 전까지 아무것도 하지 않는다.

### T1.6 에디터 작업 (사용자)

1. `Item.prefab`에 `PackItemSlotView`를 붙이고, 수량 글자(TMP) 자식을 만들어 `icon`·`countText`·`button`을 연결한다.
2. `Combat.unity`의 Pack에서 설명 패널 안 TMP를 `descriptionText`에 연결한다. 칸 24개는 프리팹 인스턴스라 1번이 그대로 들어간다.

### T1.7 시험

- **새 순수 시험:**
  - `PackInventoryLayoutTests`(키 순·빈 재고·넘침 수)
  - `ConsumableItemVisualTableTests`(Get/TryGet·중복 키·키 0 거절·이름 비면 키 숫자)
  - `CombatDisplayValues` 재고 기본값
- **Unity:** `DataBaseAssetTests`에 "View 외형 키 = InGame 소모 아이템 키" 시험을 더한다(두 그룹을 읽음).
- **관리 실행:** 기존 상태 변화 0. `CombatDisplayTables`를 만드는 두 시험은 인자만 바뀐다.

### P-1 확인

- **Unity:** DB 생성(부트 성공), `DataBaseAssetTests`, Combat 진입.
- **재생:** 시작 아이템 `1×2`·`4×1` → 가방에 위스키 ×2 · 랜턴 ×1, 호버 설명. 시작 아이템을 비우면 빈 가방.
- **공유:** 현강님 메모 §1(PackUI·칸 부품·프리팹), 민형님 Factory 공유 문서 §13(외형 표 키·View DB Factory·표시 Factory), 호진님 메모(View DB 표·에셋, InGame 글과 겹침 — 천명님께도 전달 요청).

## 3. P-2 — 드래그 장착 (요구 2·4, 설계 P4, 결정 ①, Q-P2 A)

### T2.1 입력 정책

- `C/CombatInputPolicy.cs`의 `StageOf(CombatViewCommandKind)`에 `EquipItem`·`UnequipItem`·`ReplaceItem` → 배치 국면을 더한다.
- `CombatInputPolicyTests.StageOf_MapsEveryCommand`에 세 경우를 더한다.

### T2.2 입력 기능 `C/CombatItemDragInteraction.cs` (새)

- **의존:** `ICombatPointerPicker`(칸 집기)·`ICombatInteractionFeedback`(호버 칸)·`ICombatItemDragFeedback`(드래그 그림)·`CombatCommandGate`·오디오(선택).
- **상태:** `_dragging`, `_itemKey`.
- **`Select(facts, key, stock)`:**
  - `CanSelect` = `CanSubmit(facts, gate.HasPending, EquipItem)` && `stock > 0`
  - 시작하면 드래그 그림을 보인다.
- **`PointerMoved(facts, characters, x, y)`:** 칸 강조(Q-P2 A)와 드래그 그림 이동.
  - 아군 필드 메인·서브(살아 있음)가 있으면 `Valid`, 아니면 `Invalid`.
- **`SelectReleased(facts, characters, x, y)`:** 대상 유닛을 찾는다.
  - 장착 칸이 있으면 `ReplaceItem(id, key)`, 없으면 `EquipItem(id, key)`를 `gate.TrySubmit`한다.
  - 대상이 없으면 아무것도 제출하지 않는다.
  - 어느 경우든 드래그를 끝낸다(호버·그림 정리).
- **`Cancel()`·`CancelPresentation()`·`AfterResult(facts, stock)`:** 배치 국면이 아니거나 재고가 0이면 끝낸다.
- 대상 찾기는 기존 `CombatCharacterQueries.FindPlayerAt`을 쓴다.

### T2.3 기능 묶음·Router

- **`C/CombatInteraction.cs`:**
  - 생성자 끝에 선택 인자 `CombatItemDragInteraction item`, `DragKind.Item`
  - `PointerTarget.Item`의 이동·뗌 위임
  - 정리 메서드(`SelectCanceled`·`Cancel`·`CancelDragOrAttack`·`CancelPresentation`·`CancelInputs`·`Stop`·`AfterResult`)에 아이템 기능을 더한다.
  - `SelectDeckUnit`은 아이템 드래그를 먼저 끝내고, `SelectItem`은 배치 표현을 먼저 정리한다.
  - `IsCursorSnapMode`에 아이템 드래그를 더한다(배치 드래그와 같음).
- **`C/CombatInputRouter.cs`:**
  - `ResolvePointerTarget(layer, attackActive, itemDragActive)`: 배치 Layer + 아이템 드래그면 `Item`. 이 판정식을 부르는 곳(경로 입력 입구 등)도 고친다.
  - `Cancel`은 아이템 드래그 중이면 그것만 끝낸다(회수로 가지 않음).
  - `Item` 대상의 뗌이 UI 위면 드래그를 취소한다.
- **`F/Runtime/InGame/View/CombatSceneInputFactory.cs`:** 아이템 기능을 만들어 묶음에 넣는다.

### T2.4 입구와 드래그 그림

- **새 `C/ICombatItemInput.cs`(public):** `CanSelect(int key)`·`Select(int key)`·`Cancel()`. 해제(`RequestUnequip`)는 P-4에서 더한다.
- **새 `C/CombatItemInput.cs`:** 수명·오류 경계 감쌈(`CombatDeckPlacementInput`과 같음). 재고는 `_items.Current.GetCount(key)`로 읽는다.
- **`ICombatViewService.ItemInput`:** `CombatViewSystem.CreateInput`이 만들고, 정리에서 `Release`한다.
- **드래그 그림 계약 `ICombatItemDragFeedback`:** `ShowItemDrag(key, x, y)`·`MoveItemDrag(x, y)`·`HideItemDrag()`.
  - `ICombatBattleDisplay`가 상속하고, `CombatSceneDisplay`가 가방에 넘긴다.
  - `ICombatPackDisplay`에 같은 세 메서드를 더하고 PackUI가 구현한다(`dragRect`를 포인터 위치로 옮기고 아이콘을 보임).

### T2.5 PackUI 드래그 시작

- 칸마다 `DeckCardDragBinding`을 붙여 드래그 시작을 `Click(slot)`으로 받는다(손패 카드와 같음).
- `Click(slot ≥ 1)`이면 `_combat.ItemInput.Select(그 칸의 키)`. 클릭으로 집고 다음 필드 클릭으로 놓는 것도 손패와 같이 된다.
- 칸 버튼의 눌림 가능 여부는 `CanSelect`로 맞춘다(그리기 때).
- `Hide`·`OnDisable`이면 `ItemInput.Cancel()`.

### T2.6 시험

- **새 순수 시험 `CombatItemDragInteractionTests`:**
  - 집기 조건(국면·대기·재고)
  - 빈 칸 = 장착, 찬 칸 = 교체, 같은 키 교체도 교체 명령
  - 대상 없음·적·손패·사망 = 제출 없음
  - 취소·결과 뒤 종료, 강조 색
- **Router·묶음 시험(`CombatInputRouterTests`·`CombatInteractionTests`):** 대상 판정, 취소 우선, UI 위 뗌, 손패 선택과의 배타.
- 정책 시험.

### P-2 확인

- **재생:**
  1. 위스키를 서브에 끌어 놓음 → 가방 ×1(결과 뒤)
  2. 같은 유닛에 랜턴 → 교체(위스키 ×2로 돌아옴, 랜턴 0 → 숨김)
  3. 빈 칸·적 위에 놓음 → 변화 없음
  4. 메인 장착
  5. 우클릭·Esc로 드래그 취소
- P-2에서는 HUD 아이콘이 아직 없다. 장착 여부는 가방 수량과 로그로 본다. 아이템 사건 경고는 P-3에서 없어진다.
- **공유:** 현강님 메모 §2(칸 드래그·`dragRect`), 민형님 Factory 공유 문서 §13 보완(입력 Factory).

## 4. P-3 — HUD 아이콘과 사건 순간 표시 (요구 3·6, 설계 P5·P7, 결정 ③⑥⑧, Q-P3 A)

### T3.1 HUD 상태

- `D/FieldUnitPresenter.cs`: `Configure` 끝에 선택 인자 `IConsumableItemVisualTableQuery itemVisuals`.
- `RenderUnitHud`가 격파 목표 다음에 `EquippedItemKey`가 있으면 상태 `"Item"`(외형 아이콘, 설명 = 이름 + 줄바꿈 + 설명)을 더한다. 아군·적 모두.
- 표시 Factory가 `tables.Items`를 넘긴다.

### T3.2 사건 경로와 사실 반영

- `C/CombatEventRoutes.cs`: `EventTypes`에 `CombatItemEvent`.
- `C/CombatFactEventHandler.cs`: `CombatItemEvent`이면 보인 사실에서 소지자를 찾는다(없으면 건너뜀).
  - `ItemCharacterChanges.WithItems(holder, new CharacterItemState(e.NewItemKey, holder.Items.Effects))`를 `ApplyCharacter`한다.

### T3.3 동시 묶음 (설계 P7)

- `C/CombatEventSubjects.cs`: `TryGetSubject`에서 `CombatItemEvent` → `HolderId`.
- `C/CombatConcurrentGroups.cs`: 아이템 사건은 같은 행동 구간에서 가장 가까운 같은 소지자의 묶음 사건과 같은 묶음에 넣는다(앞을 먼저, 없으면 뒤). 없으면 묶음 밖.
  - 넉백 경로 끊김 판정(`SplitInterruptedPaths`)보다 앞에 정한다. 같은 묶음 사건은 경로를 끊지 않는다.
- 클래스 remarks에 규칙을 적는다.

### T3.4 시험

- **바꾸는 시험(사용자 작성분):**
  - `CombatEventRoutesTests`: 사건 틀 형식 수 19 → 20
  - `CombatEventHandlerTableTests.EveryEventRouteType_HasAHandler_AndOtherTypesUseNone`: `CombatItemEvent`가 사실 처리기
- **새 순수 시험:**
  - `CombatConcurrentGroupsTests`: 다중 피격 사이 위스키는 다른 대상 피격과 같은 묶음, 로켓(피격 앞)은 뒤 묶음, 밧줄(공격 끝·같은 소지자 묶음 없음)은 묶음 밖, 라운드 끝 버림은 묶음 밖
  - 사실 처리기의 장착 키 반영(발동 → 빈 칸, 장착 → 키)
- **Unity 시험:**
  - `OrderedCombatSchedulingTests`·순차 재생 시험에 "A 피격 → A 위스키 → B 피격"의 B가 A 반응을 기다리지 않는 사례
  - `FieldUnitPresenter` HUD 상태 사례

### P-3 확인

- **재생:**
  1. 장착 → HUD 아이콘(호버 설명은 HUD 설명 패널이 비어 있어 안 나올 수 있음, 현강님 10-15 메모)
  2. 전투에서 발동(위스키: 체력 50% 이하 피격, 랜턴: 공격받음) → 그 순간 아이콘 사라짐
  3. 라운드 끝 → 쓰지 않은 아이콘 사라짐
  4. 회수 → 아이콘 사라짐
  5. 아이템 사건 경고가 콘솔에 없음
- **적 아이콘:** 지금 적에게 아이템을 준 스테이지가 없으므로 시험으로만 본다.

## 5. P-4 — HUD 아이콘 우클릭 해제와 우클릭 겹침 막기 (설계 P6, 결정 ②⑥)

### T4.1 우클릭 연결 (새, 공용 UI)

- `U/Base/UISecondaryClickBinding.cs` + `IUISecondaryClickReceiver.SecondaryClick(int slot)`.
- `IPointerClickHandler`로 오른쪽 버튼만 받는다. 기존 `UIInputBinding`·`IUIInputReceiver`는 바꾸지 않는다.

### T4.2 UnitHUD

- **`UnitHUDStatus`:** 끝에 선택 인자 `bool hasSecondaryAction`을 더한다.
- **`U/Screens/Stage/UnitHUD.cs`:**
  - 상태 아이콘에 우클릭 연결을 붙인다.
  - `SetStatusSecondaryHandler(Action<EntityId, string>)`.
  - 우클릭하면 (개체 ID, 상태 키)를 넘긴다.
- **입력을 받는 조건:**
  - 지금은 설명 가능할 때만 아이콘이 입력을 받는다(HUD 프리팹의 설명 패널이 비어 있어 지금은 전부 꺼짐).
  - 이것을 "설명 가능 또는 보조 동작 있음"으로 바꾼다. 버튼 눌림(설명 고정)은 지금처럼 설명 가능할 때만이다.

### T4.3 해제 입구와 연결

- **`ICombatItemInput.RequestUnequip(EntityId)`:**
  - 오류 경계 안에서 확인한다: `CanSubmit(UnequipItem)` + 아군 필드 + 장착 칸 있음.
  - 맞으면 `gate.TrySubmit(UnequipItem(id))`, 아니면 무시한다.
- **연결:**
  - `FieldUnitPresenter`가 HUD에 핸들러를 건다. 상태 키 `"Item"`이고 아군일 때만 보조 동작 표시를 켜서, 적은 입력을 받지 않는다(결정 ⑥).
  - 핸들러는 `CombatViewSystem.CreateInput`이 입구를 만든 뒤 표시에 넘긴다(`BindItemUnequip`). 정리에서 푼다.
  - 표시가 입력보다 먼저 만들어지기 때문이다.

### T4.4 우클릭 겹침 막기

- `C/PlacementDragInteraction.cs`: 기억한 포인터 읽기 `TryGetLastPointer(out x, out y)`.
- `C/CombatInputRouter.cs`의 `Cancel`: 기억한 포인터가 UI 위면(`_occlusion.IsPointerOverUi`) `TryReturnPlacedUnit`을 건너뛰고 일반 취소만 한다.

### T4.5 시험

- **순수 시험:**
  - `RequestUnequip` 조건(국면·아군·칸·대기)
  - Router `Cancel`의 UI 위 회수 건너뜀(가짜 가림)
  - UnitHUD 핸들러 전달은 Unity 시험(`UnitHUDTests`)

### P-4 확인

- **재생:**
  1. 장착 아이콘 우클릭 → 해제·가방 +1
  2. 메인 아이콘 우클릭 → 해제(메인은 손패로 가지 않음)
  3. HUD 위 우클릭이 뒤쪽 칸의 서브를 회수하지 않음
  4. 필드의 유닛 우클릭 회수는 그대로
  5. 적 아이콘 우클릭 → 무시
- **공유:** 현강님 메모 §3(우클릭 연결·UnitHUD 입력 조건).

## 6. 공유 문서

| 대상 | 문서 | 내용 |
|---|---|---|
| 현강님 | 새 `docs/superpowers/handoffs/2026-10-19-pack-item-hyeongang-share.md` | §1 PackUI 표시 계약·칸 부품·프리팹 연결 / §2 칸 드래그·`dragRect` / §3 `UISecondaryClickBinding`·UnitHUD 입력 조건. HUD 설명 패널이 아직 비어 있다는 10-15 메모도 다시 알림 |
| 민형님 | `handoffs/2026-10-15-combat-scene-display-factory-framework-share.md` §13 | `ViewTableKey.ConsumableItemVisual`·`ViewDataBaseFactory` 등록·표시/입력 Factory 변경 |
| 호진님(+ 천명님) | 새 `docs/superpowers/handoffs/2026-10-19-consumable-item-visual-table-hojin-share.md` | View 외형 표·에셋·`ViewGroup.asset`(코드와 한 커밋). InGame `ConsumableItemData` 이름·설명과 글이 겹친다(InGame 쪽을 남길지 결정 요청) |

## 7. 실행 기록

### 7.1 P-1 외형 표와 가방 표시 (2026-10-19, 코드 완료, 확인 건너뜀)

- **확인 상태:** 사용자 결정(머리말)으로 MSBuild·관리 실행·Unity 목록을 하지 않았다. 컴파일도 확인하지 않은 상태다.
- **새 코드:**
  - `V/Data/View/Table/ConsumableItem/Item/ConsumableItemVisualDataSO.cs`(Data + SO)
  - `V/Data/View/Table/ConsumableItem/ConsumableItemVisualDataTableSO.cs`
  - `V/Queries/ViewDataBase/ViewTable/ConsumableItemVisualTable.cs`
  - `C/ICombatPackDisplay.cs`(`ConfigureItems`·`ShowInventory`)
  - `U/Screens/Stage/PackInventoryLayout.cs`, `U/Screens/Stage/PackItemSlotView.cs`
- **바꾼 코드:**
  - DB: `ViewTableKey`(`ConsumableItemVisual`), `ViewDataBase`(`IViewQuery`), `ViewDataBaseFactory`
  - 값 경로: `CombatDisplayTables`(`Items` 필수), `CombatDisplayValues`(`ItemInventory`), `CombatViewSystem`(`BindServices` 끝 선택 인자 `items`, 운영은 `ItemInventoryReadSink`), `CombatSceneDisplay`(생성자 끝 선택 인자 `pack`, 그리기에서 `ShowInventory`), `CombatSceneDisplayFactory`(가방에 외형 표, 표시에 가방)
  - UI: `PackUI`(`ICombatPackDisplay` 명시 구현, 칸 모으기·채우기·호버 설명, 필수 참조 `descriptionText` 추가)
- **새 에셋:** `DataBaseSO/ViewGroup/ConsumableItemVisual/`(표 1 + 아이템 4, 이름·설명은 InGame SO에서 옮김, 아이콘 없음), 폴더 `.meta` 넷, `ViewGroup.asset` 한 줄.
- **시험:**
  - 새 `PackInventoryLayoutTests`(순수)·`ConsumableItemVisualTableTests`(SO 생성이라 Unity), `DataBaseAssetTests.ConsumableItemVisuals_MatchInGameItemKeys`
  - 바꾼 시험: `CombatDisplayTables`를 만드는 2파일(인자 추가), `IViewQuery` 가짜 7파일(멤버 추가)
- **계획과 다른 점:**
  - `CombatDisplayValues` 기본값 시험은 쓰지 않았다(필드 하나라 생략).
  - 가방 재고를 다시 그리면 열려 있던 칸 설명을 닫는다(칸 내용이 바뀌었을 수 있음).
  - 표시 Factory는 `root.Pack`을 Unity null 비교로 거른 뒤 넘긴다(인터페이스 `?.`가 Unity의 빈 객체를 거르지 못함).
- **공유:** 민형님 Factory 공유 문서 §13, 현강님 새 메모 `handoffs/2026-10-19-pack-item-hyeongang-share.md` §1, 호진님(+ 천명님) 새 메모 `handoffs/2026-10-19-consumable-item-visual-table-hojin-share.md`. 모두 미전달이다.
- **에디터 작업(사용자, T1.6):**
  1. `Item.prefab`에 `PackItemSlotView`를 붙이고, 수량 TMP 자식을 만들어 `icon`·`countText`·`button`을 연결한다.
  2. `Combat.unity`의 Pack에서 설명 패널 안 TMP를 `descriptionText`에 연결한다.
  - 연결 전에는 Combat 진입이 PackUI 참조 누락 예외로 멈춘다.
- **커밋 때:** 새 `.cs` 6개(+ `.meta`), 시험 2개(+ `.meta`), 에셋 폴더 `ConsumableItemVisual/`(+ `.meta`)와 `ViewGroup.asset`, 코드 폴더 `ConsumableItem/`(+ `.meta`)를 한 커밋으로 넣는다.
- **Unity 확인 중 발견(2026-10-19, 사용자):** 키 1~4를 넣었는데 키 1만 보였다.
  - 원인: 씬 가방 목록의 Item 칸이 1개뿐이었다. 처음 조사의 "24칸"은 프리팹 GUID 참조 수를 인스턴스 수로 잘못 센 것이었다.
  - 기획 답변(최대 8칸)에 따라 사용자가 씬에 8칸을 넣었다(세로 레이아웃 그룹 + 크기 맞춤). 결정 ⑤를 "고정 8칸"으로 고쳤다(설계 §0).

### 7.2 P-2 드래그 장착 (2026-10-19, 코드 완료, 확인 건너뜀)

- **확인 상태:** P-1과 같이 MSBuild·관리 실행·Unity 목록을 하지 않았다. 컴파일 미확인이다.
- **새 코드:**
  - `C/CombatItemDragInteraction.cs`(집기 조건·이동 강조·놓기 = 빈 칸 장착 / 찬 칸 교체·취소·결과 뒤 종료)
  - `C/ICombatItemDragFeedback.cs`(끄는 그림 계약)
  - `C/ICombatItemInput.cs`(public 입구), `C/CombatItemInput.cs`(수명·오류 경계 감쌈)
- **바꾼 코드:**
  - 정책: `CombatInputPolicy.StageOf`에 아이템 명령 셋 → 배치 국면
  - 묶음: `CombatInteraction`(끝 선택 인자 `item`, `DragKind.Item`, `IsItemDragActive`, `CanSelectItem`·`SelectItem`·`CancelItemSelection`, 손패 선택과 배타, 정리 메서드에 아이템 더함, 커서 스냅)
  - Router: `PointerTarget.Item`, `ResolvePointerTarget` 셋째 인자(기본 false), `Cancel`은 아이템 드래그만 끝냄, UI 위 뗌은 취소
  - 조립: `CombatSceneInputFactory`(`itemStock` 인자, 아이템 기능 생성), `CombatViewSystem`(`ItemInput`·`ReadItemStock`·생성과 해제)
  - 표시: `ICombatBattleDisplay`가 `ICombatItemDragFeedback` 상속, `CombatSceneDisplay`가 가방에 넘김, `ICombatPackDisplay`에 드래그 세 메서드
  - 서비스: `ICombatViewService.ItemInput`
  - UI: `PackUI`(칸에 `DeckCardDragBinding`, `Click(slot ≥ 1)` → `ItemInput.Select`, 숨김·꺼짐 때 `Cancel`, `dragRect`가 포인터를 따라감·`raycastTarget` 끔·아이콘)
- **시험:**
  - 새 `CombatItemDragInteractionTests`(순수: 집기 조건, 장착·교체 선택, 대상 없음, 강조 색, 취소, 결과 뒤, 포인터 대상)
  - `CombatInputPolicyTests.StageOf_MapsEveryCommand` 세 경우 추가
  - 바꾼 시험 가짜: `CombatViewSystemTests.FakeDisplay`(드래그 세 메서드), `ICombatViewService` 가짜 4곳(`ItemInput => null`)
- **계획과 다른 점:**
  - 칸 버튼의 눌림 가능 여부를 `CanSelect`로 맞추는 것은 하지 않았다. 집을 수 없으면 `Select`가 아무것도 하지 않는다. 가방은 배치 국면에만 보인다.
  - 드래그 효과음은 넣지 않았다.
- **공유:** 현강님 메모 §2, 민형님 Factory 공유 문서 §14(미전달).
- **사용자 확인에서 발견(2026-10-19):** 끄는 아이템이 마우스를 따라가지 않았다.
  - 원인: 씬의 `DragRect`는 RectTransform 하나뿐(Image·자식 없음, 1920×900)인 현강님 객체라, 그것을 켜고 옮겨도 보이는 것이 없었다.
  - 사용자 결정(B): 따라다닐 아이콘 Image를 사용자가 만들고 PackUI 새 필드 `dragIcon`(Image, 필수, Pack 안·펼침 목록 밖)에 연결한다.
  - 코드는 `dragIcon`을 켜고 끄며 포인터로 옮긴다(맨 위 형제로, 입력은 받지 않게, 아이템 아이콘이 없으면 넣어 둔 그림). `DragRect`는 현강님 원래 동작(가방 정리 때 끄기)으로 되돌렸다.
- **에디터 작업(사용자):**
  1. Pack 아래(펼침 목록 밖)에 Image를 만든다. 크기는 칸 아이콘 정도로 하고 Pivot은 가운데로 둔다. 꺼 둬도 된다.
  2. PackUI의 `dragIcon`에 연결한다.
  - 연결 전에는 Combat 진입이 PackUI 참조 누락 예외로 멈춘다.
- **커밋 때:** 새 `.cs` 4개·시험 1개를 `.meta`와 함께 넣는다.

### 7.3 P-3 HUD 아이콘과 사건 순간 표시 (2026-10-19, 코드 완료, 확인 건너뜀)

- **확인 상태:** P-1·P-2와 같이 MSBuild·관리 실행·Unity 목록을 하지 않았다. 컴파일 미확인이다.
- **바꾼 코드:**
  - HUD 상태: `D/FieldUnitPresenter.cs`(`Configure` 끝 선택 인자 `itemVisuals`, `RenderUnitHud`가 격파 목표 다음·특성 앞에 상태 `"Item"`을 더함, 아군·적 모두, 외형이 없으면 아이콘 없이 키 글자). 표시 Factory가 `tables.Items`를 넘긴다.
  - 사건 경로: `C/CombatEventRoutes.cs`의 사건 틀 형식에 `CombatItemEvent`(19 → 20). 장착 때 남던 "경로 표에 없음" 경고가 사라진다.
  - 사실 반영: `C/CombatFactEventHandler.cs`가 아이템 사건이면 보인 사실의 소지자 장착 칸을 사건의 새 장착 키로 바꾼다(`ItemCharacterChanges.WithItems`, 남는 효과는 그대로). 소지자가 보인 사실에 없거나 이미 같은 값이면 건너뛴다.
  - 주체: `C/CombatEventSubjects.cs`의 `TryGetSubject`에서 아이템 사건 → 소지자.
  - 동시 묶음: `C/CombatConcurrentGroups.cs`가 사건마다 행동 구간 번호를 기억하고, 묶음 밖 아이템 사건을 같은 구간에서 가장 가까운 같은 소지자의 묶음 사건과 같은 묶음에 둔다(`AttachItemEvents`, 넉백 경로 끊김 판정 앞).
- **계획과 다른 점(설계 P7은 고쳐 둠):**
  - 찾는 방향: 계획은 "앞을 먼저, 없으면 뒤"였다. 로켓은 Core가 피해 앞(`BeforeDamage`)에 기록하므로 앞을 먼저 보면 다른 묶음에 붙을 수 있어, 피해 앞 단계는 뒤를 먼저, 그 밖은 앞을 먼저 보게 했다.
  - 회복 사건: Core가 위스키 발동 바로 뒤에 소지자 회복 체력 사건(근원 없음, `CombatDamageApplication.Apply`)을 낸다. 이것도 묶음 밖 장벽이라 다른 대상 피격을 막으므로, 발동 바로 뒤의 같은 소지자·근원 없는 캐릭터 사건을 같은 묶음에 둔다. 계획에는 없던 규칙이다.
- **시험:**
  - 바꾼 시험(사용자 작성분): `CombatEventRoutesTests`(목록에 `CombatItemEvent`, 수 20), `CombatEventHandlerTableTests.EveryEventRouteType_HasAHandler_AndOtherTypesUseNone`(아이템 사건 = 사실 처리기). 둘 다 경로 표에 넣은 결과를 따른 것이다.
  - 새 순수 시험: `CombatConcurrentGroupsTests` 넷 — 다중 피격 사이 위스키·회복이 소지자 묶음에 들어가 다른 대상 피격을 막지 않음, 로켓은 뒤 피격 묶음, 공격 끝(밧줄)·라운드 끝 버림은 묶음 밖, 다른 행동 구간에는 붙지 않음.
  - 새 Unity 시험: `CombatFactEventHandlerTests`(보인 사실이 `FieldUnitPresenter`를 요구) — 장착 → 키, 발동 → 빈 칸, 교체 → 새 키·효과 유지, 모르는 소지자 건너뜀.
  - 하지 않은 것: 계획 T3.4의 Unity 시험 둘(순차 재생 "A 피격 → A 위스키 → B 피격", `FieldUnitPresenter` HUD 상태)은 쓰지 않았다. 앞은 묶음 순수 시험이, 뒤는 재생 확인이 대신한다.
- **공유:** 민형님 Factory 공유 문서 §15(미전달). 현강님 파일은 바꾸지 않았다. HUD 아이콘 호버 설명은 `HUD.prefab`의 설명 패널이 비어 있어 아직 안 나온다(10-15 메모, 현강님 메모 §3에서 다시 알림 예정).
- **커밋 때:** 새 시험 `CombatFactEventHandlerTests.cs`를 `.meta`와 함께 넣는다.

### 7.4 P-4 HUD 아이콘 우클릭 해제와 우클릭 겹침 막기 (2026-10-19, 코드 완료, 확인 건너뜀)

- **확인 상태:** 앞 Phase와 같이 MSBuild·관리 실행·Unity 목록을 하지 않았다. 컴파일 미확인이다.
- **새 코드:** `U/Base/UISecondaryClickBinding.cs`(+ `IUISecondaryClickReceiver`, 같은 파일). 오른쪽 클릭만 받고 Button 눌림 가능 여부는 보지 않는다.
- **바꾼 코드:**
  - `U/Screens/Stage/UnitHUD.cs`(현강님 파일):
    - 상태 아이콘에 우클릭 연결을 붙이고 `SetStatusSecondaryHandler(Action<EntityId, string>)`를 더했다.
    - 아이콘이 입력을 받는 조건은 "설명 가능 또는 보조 동작 있음"이다. 버튼 눌림·호버는 설명 가능할 때만이다.
    - `UnitHUDStatus`에 끝 선택 인자 `hasSecondaryAction`을 더했다.
  - `D/FieldUnitPresenter.cs`:
    - HUD를 만들 때 처리기를 건다. 아이템 상태는 **아군일 때만** 보조 동작을 켠다(결정 ⑥).
    - `BindItemUnequip(Action<EntityId>)`를 더했다. 상태 키 `"Item"`이면 해제 입구로 보낸다.
  - 표시 계약: `ICombatBattleDisplay.BindItemUnequip`, `CombatSceneDisplay`가 유닛 표시에 넘긴다(해제 뒤에는 풀기만 받음).
  - 입구:
    - `ICombatItemInput.RequestUnequip(EntityId)`. `CombatItemInput`이 캐릭터 읽기를 더 받아 오류 경계 안에서 `CombatInteraction.RequestItemUnequip`을 부른다.
    - `CombatItemDragInteraction.RequestUnequip`는 해제 명령 기록 가능 + 필드의 살아 있는 아군 메인·서브 + 장착 칸 있음을 본다. 맞으면 `UnequipItem`을 제출한다.
  - 연결: `CombatViewSystem.CreateInput`이 입구를 만든 뒤 `_display.BindItemUnequip(_itemInput.RequestUnequip)`를 건다. `StopInteraction`이 null로 푼다.
  - 겹침 막기:
    - `PlacementDragInteraction.TryGetLastPointer`, `CombatInteraction.TryGetLastPlacementPointer`를 더했다.
    - Router `Cancel`은 기억한 포인터가 UI 위면 `TryReturnPlacedUnit`을 건너뛰고 일반 취소만 한다.
- **계획과 다른 점:**
  - 보조 동작 표시는 입구가 걸렸는지와 관계없이 아군 아이템이면 켠다. 걸리기 전이나 푼 뒤의 우클릭은 아무것도 하지 않는다. 입력이 첫 그리기 뒤에 만들어져, 걸림 여부로 정하면 다음 그리기까지 아이콘이 입력을 받지 않기 때문이다.
  - 우클릭 효과음은 넣지 않았다.
- **끄는 중의 우클릭:** 해제는 드래그 중인지 보지 않는다. 같은 우클릭이 입력 시스템 취소로도 들어와 드래그를 끝내는데, 두 입력의 순서가 정해져 있지 않아 드래그 여부로 막으면 결과가 순서에 따라 달라지기 때문이다. 아이콘 위 우클릭은 언제나 "드래그 취소 + 해제"다.
- **시험:**
  - 순수 시험:
    - `CombatItemDragInteractionTests` 둘: 아군 장착 유닛만 해제(빈 칸·적·빈 ID·모르는 유닛은 무시, 메인 해제), 전투 국면에는 무시.
    - `CombatInteractionTests.RightClick_OverUi_DoesNotReturnTheUnitBehind`: UI 위 우클릭은 회수하지 않고, 필드 위 우클릭은 회수한다.
    - `CombatViewSystemTests.ItemUnequipHook_IsBoundAfterInput_AndUnboundOnStop`
  - Unity 시험: `UnitHUDTests.Status_SecondaryAction_OpensInputWithoutDescription_AndRightClickPassesUnitAndKey`
  - 바꾼 시험 가짜: `CombatViewSystemTests.FakeDisplay`에 `BindItemUnequip`(건 값 기록).
- **확인된 사실:** 씬의 `InputSystemUIInputModule`(Boot·Combat 등)에는 기본 동작 에셋의 `RightClick` 동작이 연결돼 있어 UI가 오른쪽 클릭 이벤트를 받는다.
- **공유:** 현강님 메모 §3(우클릭 연결·UnitHUD 입력 조건·HUD 설명 패널 다시 알림). Factory 파일은 바꾸지 않았다.
- **커밋 때:** 새 `UISecondaryClickBinding.cs`를 `.meta`와 함께 넣는다.

### 7.5 Unity 확인 (2026-10-19)

- 사용자가 Unity에서 P-1~P-4를 한꺼번에 컴파일하고 테스트·재생을 확인했다. 문제 없음.
- MSBuild·관리 실행 비교는 사용자 결정대로 하지 않았다(머리말). 이 Unity 확인이 P-1~P-4의 확인을 대신한다.

### 7.6 `0d0adcfd` 병합 (2026-10-19, 커밋 안 함)

- **들어온 것:** `origin/feature/Hojin/StageSelcet`의 `0d0adcfd`(천명님 혼합 상점·보상 정산, 현강님 UI 리소스·Hover 설명, 호진님 튜토리얼 문서·카우보이·쉴더 SO).
- **겹친 표:** 천명님이 상점 작업 `a5133b32`에서 같은 이름·같은 경로의 `ConsumableItemVisual` 표를 GUID만 다르게 만들었다(키·아이콘만, 아이콘 비어 있음).
  - 사용자 결정: 우리 것을 남긴다(코드·GUID·에셋 4개).
  - 천명님 쪽 아이템 에셋 4개(`Lantern`·`Roket`·`Rope`·`Whiskey.asset`)와 표 에셋은 지웠다. 그 스크립트·에셋 GUID를 참조하는 곳은 남지 않았다.
  - 상점 코드(`ShopPopupViewFactory`·`StageMapViewSystem`)는 `Get`·`TryGet`·`Icon`만 써서 그대로 동작한다. 상점 아이콘은 이제 우리 에셋의 아이콘이다.
- **중복 정리:**
  - `ViewTableKey`·`ViewDataBase`·`ViewDataBaseFactory`·`ViewGroup.asset`: 양쪽이 같은 항목을 더해 두 번씩 들어갔다. 저쪽 변경이 이 중복뿐이라 우리 판으로 맞췄다.
  - 시험 가짜 6곳(`ConsumableItemVisualTableQuery => null` 두 번): 우리 판으로 맞췄다.
  - `ViewPopupCompositionTests`: 저쪽 상점 시험은 살리고 우리 가짜 줄은 지웠다. 저쪽 가짜에 `GetAll`을 더했다.
- **`Combat.unity`:** 충돌 두 곳.
  - 우리가 더한 `Item.prefab` 칸 하나(`&1075721316`)는 남겼다. 현강님이 지운 `Text (TMP)` 객체(`&1081254748`~`51`, 우리는 손대지 않음)는 지웠다.
  - 다른 한 곳은 앵커 위치 소수점 차이라 우리 값을 썼다.
  - 병합 뒤 씬 안의 지역 참조가 모두 있는 객체를 가리키는 것을 확인했다.
- **확인 상태:** 컴파일·Unity 확인 전이다. 커밋은 사용자가 한다.
- **보고(지우지 않음):** 호진님 `UnitVisual_Cowboy`·`UnitVisual_Shielder` 에셋은 `UnitVisualDataTable`에 들어 있지 않아 어디서도 참조되지 않는다.
