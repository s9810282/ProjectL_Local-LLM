# 소모 아이템 View 외형 표 — 호진님 공유 (2026-10-19, 미전달, 천명님께도 전달 부탁)

- 설계: [pack-item-design](../specs/2026-10-19-combat-pack-item-design.md) P3, 계획: [pack-item-plan](../plans/2026-10-19-combat-pack-item-plan.md) P-1.

## 바뀐 것

- **새 View 표 `ConsumableItemVisual`:** 키는 InGame 소모 아이템 키(int)와 같습니다. 필드는 `itemKey`·`displayName`·`icon`(비워도 됨)·`description`입니다. 특성 외형(`TraitVisual`)과 같은 꼴입니다.
  - 코드: `DataBaseSystem/Data/View/Table/ConsumableItem/`(Data·SO·표 SO), `Queries/ViewDataBase/ViewTable/ConsumableItemVisualTable.cs`
  - 등록: `ViewTableKey.ConsumableItemVisual`, `IViewQuery.ConsumableItemVisualTableQuery`, `ViewDataBaseFactory`
- **에셋:** `DataBaseSO/ViewGroup/ConsumableItemVisual/`(표 + 아이템 4개), `ViewGroup.asset`의 표 목록. **코드와 한 커밋**입니다(표가 없으면 DB 생성 실패).
- **시험:** `DataBaseAssetTests.ConsumableItemVisuals_MatchInGameItemKeys`(View 외형 키 = InGame 아이템 키), `ConsumableItemVisualTableTests`.

## 확인 부탁

- **글이 두 곳에 있습니다.** 이름(위스키·로켓·밧줄·랜턴)과 설명은 InGame `ConsumableItemDataSO`의 `displayName`·`description`을 그대로 옮겼습니다.
  - View는 InGame을 읽지 않는 규칙이라, 화면 글은 View 표에 둡니다.
  - 지금 InGame 쪽 이름·설명을 읽는 코드는 없습니다(Core는 키·가격·효과만 씀).
  - InGame 쪽 글을 남길지(예: 상점·기획 확인용) 지울지는 천명님·호진님이 정해 주세요. 남기면 두 곳을 같이 고쳐야 합니다.
- **아이템을 더할 때:** InGame 소모 아이템과 View 외형 항목을 같은 키로 함께 만들어야 합니다. 키가 어긋나면 위 시험이 잡습니다.

## 병합 뒤 (2026-10-19, `0d0adcfd` 병합)

- 천명님 상점 작업 `a5133b32`에도 같은 이름·같은 경로의 `ConsumableItemVisual` 표가 있었습니다(GUID 다름, 키·아이콘만). 병합에서는 가방 쪽 표 하나만 남겼습니다(사용자 결정).
  - 남은 표: 필드 `itemKey`·`displayName`·`icon`·`description`, 조회 `Get`·`TryGet`·`GetAll`. 상점이 쓰는 `Get`·`TryGet`·`Icon`은 그대로라 상점 코드는 바꾸지 않았습니다.
  - 지운 것: 천명님 쪽 아이템 에셋 4개(`Lantern`·`Roket`·`Rope`·`Whiskey.asset`, 아이콘 비어 있음)와 표 에셋. 에셋은 `ConsumableItemVisual_<이름>.asset` 4개(아이콘·이름·설명 있음)입니다.
- 천명님께: 앞으로 이 표에 칸을 더할 일이 있으면 이 표를 쓰시면 됩니다. 같은 표를 두 번 만들지 않게 알려 드립니다.
