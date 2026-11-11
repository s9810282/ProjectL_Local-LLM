# 전투 맵 바닥 타일 맞춤 Task 계획

> **2026-11-10 후속:** 배경·타일 공통 자동 배율은 [공통 계획 CS0~CS4](2026-11-10-combat-map-common-scale-plan.md)에서 Combat·CombatMapAuthoring을 함께 다룬다. 아래 완료/검증 이력은 유지한다. 새 작업은 기존 표시 결과 보존을 먼저 검증하며, 설계 갱신만으로 타일 배치·배율을 변경하지 않는다.

- 날짜: 2026-10-28
- 설계: [map-tile-alignment-design](../specs/2026-10-28-combat-map-tile-alignment-design.md)(TA1~TA5, Q-TA1~Q-TA6 모두 A — 2026-10-28 사용자 "모두 A")
- 상태: P1~P14 그대로 진행(2026-10-28 사용자 "7기준에 문제가 없다면 진행해", 점검 §1.1). TA-a 진행

## 0. 진행 규칙과 착수 조건

- Phase마다 멈추고 보고한다. 계획과 다른 결정이 필요하면 구현하지 않고 먼저 묻는다.
- View 작업이라 시험 코드는 쓰지 않고, 전투 View라 MSBuild도 돌리지 않는다. 보고에는 "컴파일 미확인"을 적는다. 컴파일·재생 확인은 사용자가 Unity에서 한다.
- 검토 에이전트는 묶음 끝(TA-c)에 한 번만 돌린다.
- 착수 조건 없음. 지금 작업 트리는 원점 맞춤을 롤백한 상태(타일 생성만 있음)다.
- 다른 세션의 소리 작업 변경(`CombatDisplayTables.cs`·소리 처리기 등)이 같은 트리에 있다. 이 계획은 그 파일을 고치지 않는다.

## 1. 계획에서 정한 것 (확인 대상)

| 번호 | 정한 것 | 이유 |
|---|---|---|
| P1 | (개정 3) 타일 바닥 높이 = `anchorHeight`(타일 루트 단위). 타일 루트·배경 높이는 같은 `positionOffset.y` | 타일 루트가 배경과 같은 보정을 받는다. |
| P2 | **(개정 4, 10-28 사용자 "타일 맵 칸 좌표") 기준점 `anchorTile`(정수) = 게임 (0,0) 칸이 놓일 타일 맵 칸, 화면(필드) 기준 좌하단 타일이 (0,0), 배경 회전과 무관. 격자 가운데 = 타일 루트 원점(아래 개정 3 문장의 "기준점 → 위치 변환"은 대신함).** (개정 3, 10-28 사용자 지시 다섯) 기준점 = 타일 루트 원점에서 본 격자 가운데, 칸 단위 `anchorTile` + 타일 바닥 높이 `anchorHeight`. 기준점 → 위치 변환은 데이터 격자 모서리 한 줄(`CenterOf`·`GridCenter`가 함께 씀). 옛 루트 값 셋은 사라짐 | 사용자 "위치만 남겨"·"칸 단위 + 높이"·"배경 자식으로 넣지마"·"MapTileRoot에도 똑같이 offset". 프리팹은 고치지 않는다. |
| P3 | (개정 3) 회전 보정 피벗은 타일 격자 가운데, 축은 칸 루트 위 방향. 배경과 타일 루트를 같은 각으로 돌리고 같은 거리로 옮긴다 | 둘이 같은 보정으로 시작해 같은 연산을 받으므로 늘 겹친다. |
| P4 | 허용 오차: 기울기 1°, 크기 정수배 판정은 `|크기 / 칸 간격 − 가장 가까운 정수| ≤ 0.001`. 남은 각은 0이어도 그대로 돌린다(갈래 없음) | 에셋 값의 부동소수 오차(−88.55° 같은 값은 보정 대상)를 받아들이되 잘못된 크기는 거절한다. |
| P5 | 타일 방식인데 −195에 전투가 없으면(보드 크기를 못 읽으면) 진입 오류(`InvalidOperationException` → F1) | 운영 흐름에서는 전투 준비가 씬 전환보다 먼저다. 반 칸 판단 없이 놓으면 조용히 어긋난다. |
| P6 | 필드가 타일 맵보다 크면 `Debug.LogWarning` 한 줄(세울 때마다 한 번) | Q-TA5 A. |
| P7 | (개정 3) `StageMapTilePresenter` → 정적 `StageMapTiles.Build(cellsRoot, tiles)`(GUID 유지). 타일 루트는 칸 루트 아래 배경과 형제이고, 보정은 배경 표시의 `ApplyOffsets` 하나가 배경과 똑같이 넣으며, 참조를 들고 배경과 함께 없앤다 | 타일 루트가 배경 자식이 아니어서 수명을 배경 표시가 맡는다. |
| P8 | 타일 방식에서도 `positionOffset`·`rotationOffset`·`scaleOffset`을 먼저 적용하고, 그 뒤 코드가 회전 보정·수평 이동을 덮는다. 결과적으로 `positionOffset`의 x·z는 쓰이지 않는다(툴팁에 적음). | 높이(y)·화면 방향(회전)·배율은 데이터 값 그대로 쓴다. |
| P9 | 보드 크기 읽기는 `CombatDisplayValueReader.TryReadBoardSize(out Vector2Int size)` 하나(전투가 없으면 false). 배경 표시는 `Vector2Int?`로 받는다 | 표시 값을 Buffer에서 읽는 곳이 이미 이 클래스다. 있음·가로·세로를 따로 넘기지 않는다. |
| P10 | 설정 → 칸 간격 변환은 `FieldCellSpacing.FromGridOutline(StageFieldViewData)` 하나(GridOutline이 아니면 예외). `FieldOutlineRenderer`도 이것으로 간격을 만든다. | 같은 값을 두 곳이 따로 바꾸지 않게(SSOT). 칸 프리팹 모드는 Q-TA6 A로 받지 않는다. |
| P11 | **(개정 4로 쓰지 않음 — 기준 타일 모서리 = 게임 (0,0) 칸 모서리로 옮김)** 반 칸 방향(Q-TA4 A): 축마다 `(k × 타일 수 − 필드 칸 수)`가 홀수면 타일 격자 중앙을 칸 루트의 +½칸 자리에 둔다 | 남는 반 칸이 필드 (0,0)의 반대쪽(+X·+Z)에 생긴다. |
| P12 | **(개정 4) `Test.asset`의 사용자 값 (4, 4)·−1.3은 그대로 둔다. 10-28 스크린샷 자리는 (3, 3)이다(§6.5).** (개정 3) `Test.asset` 기준점은 사용자가 넣은 값((4, 4)·1.3)을 그대로 둔다. 예전에 맞춰 둔 구멍 자리와 같게 하려면 (−1, 0)·−2(타일 루트가 배경 보정을 받아 TA-b 1판 계산과 같음) | 사용자 값이 이미 있어 덮어쓰지 않는다. |
| P13 | 필드 격자가 칸 루트의 어디에 놓이는지(가운데) 규칙을 `FieldGridMapper` 정적 함수 `CornerLocal(columns, rows, cellWidth, cellLength)` 하나로 내보내고, Mapper 자신과 맞춤 계산이 함께 부른다(동작 무변경) | 맞춤 계산이 "필드 가운데 = 칸 루트 원점"을 따로 가정하면 사본이 생긴다(7기준 점검). `FieldGridDecal`의 같은 가정 사본은 이번 변경 전부터 있던 것이라 기록만 한다. |
| P14 | 타일 격자 가운데는 `CombatMapTilesData.GridCenter`(`CenterOf`와 같은 모서리 계산에서 나옴)로 내고, 맞춤 계산은 그것을 읽는다 | 맞춤 계산이 "격자 가운데 = 타일 루트 원점"을 따로 가정하지 않게(7기준 점검). |

### 1.1 7기준 점검 (2026-10-28, 사용자 "7기준에 문제가 없다면 진행해")

계획 1판을 7기준으로 다시 보고 아래를 고쳤다. 고친 뒤 남은 문제는 없다고 보고 TA-a를 진행한다.

| 기준 | 찾은 것 | 고친 것 |
|---|---|---|
| SSOT | 맞춤 계산이 필드 가운데 자리와 타일 격자 가운데 자리를 따로 가정 | P13·P14 |
| SOLID | 배경 표시 `Show`에 있음·가로·세로 원시값 셋 | P9 `Vector2Int?` 하나 |
| 무의미한 If | 남은 각이 아주 작으면 돌리지 않는 갈래(돌려도 결과가 같음) | P4에서 갈래 삭제 |
| FW·독립성·캡슐화·C#/Unity | 씬 Node 순서·Tick 없음, 다른 배경 방식·필드 동작 무변경, 기준점은 값 한 곳(P2 개정), 수명은 Transform 부모 | 없음 |

기록만: `FieldGridDecal`이 필드 가운데를 따로 가정한다(모서리 `−TotalWidth/2`·마스크 중심 = 칸 루트 위치). 이번 변경 전부터 있던 사본이고 필드 동작을 바꾸지 않으려고 이번에는 그대로 둔다.

## 2. TA-a 기준점과 데이터 (TA1·TA2)

| Task | 파일 | 내용 |
|---|---|---|
| a.1 | `CombatMapTilesData.cs` | (개정) 옛 루트 값 셋 대신 기준점 칸 단위 자리 `anchorTile`·높이 `anchorHeight`(P2). 데이터 속성 `AnchorTile`·`AnchorHeight`와 배경 기준 위치 변환 `AnchorPosition`. `CenterOf`를 모서리 하나에서 계산하게 바꾸고 같은 모서리에서 `GridCenter`를 낸다(P14, 값은 같음). |
| a.2 | `StageViewPropDataSO.cs` | `positionOffset`(높이만 씀)·`rotationOffset`(카메라와 함께 돌고 90° 단위로 맞춰짐)·`mapTiles`(기준점은 여기 배경 기준) 툴팁. |
| a.3 | `StageMapTilePresenter.cs` | 속성 이름만 맞춘다. 배경 아래로 옮기는 일은 TA-b. |

화면 변화 없음. 사용자 에디터 작업 없음(1판의 U-a는 개정으로 없어짐).

## 3. TA-b 맞춤 (TA3·TA4)

| Task | 파일 | 내용 |
|---|---|---|
| b.1 | `Display/Field/FieldCellSpacing.cs`·`FieldOutlineRenderer.cs` | `FromGridOutline(config)`(P10). 렌더러 생성자가 이것으로 간격을 한 번 만들고 `MeasureSpacing`이 그 값을 돌려준다. `CanReuse`는 그대로. |
| b.1b | `Display/Field/FieldGridMapper.cs` | 정적 `CornerLocal(columns, rows, cellWidth, cellLength)`(P13). 생성자·칸 변환이 그것을 쓰고 값은 같다. |
| b.2 | `Display/CombatDisplayValueReader.cs` | `TryReadBoardSize`(P9). |
| b.3 | 새 `Display/Map/CombatMapTileAlignment.cs`(+ `.meta`) | 순수 계산(정적). 입력: 칸 루트 기준 타일 격자 중앙·가로/세로 한 칸 벡터·칸 수, 필드 칸 수, 칸 간격. 출력: 돌릴 각(남은 각), 놓을 중앙(반 칸 포함), 필드가 더 큰지. 기울기·정수배 검사 예외(P4). 축 대응(90°·270°면 가로 칸 수가 필드 Z축)·반 칸 규칙(설계 §3, P11)은 여기 한 곳. |
| b.4 | `Display/Map/StageMapTilePresenter.cs` → `StageMapTiles.cs`(GUID 유지) | 정적 `Build(Transform backdrop, CombatMapTilesData tiles)`: 배경 인스턴스 아래 기준점 값 자리에 `MapTileRoot`와 칸마다 프리팹(P7). |
| b.5 | `Display/Map/StageViewPropPresenter.cs` | 생성자에 필드 설정(`StageFieldViewData`), `Show(int stageKey, Vector2Int? boardSize)`. 타일 방식 순서: 배경 세우기(오프셋 적용) → 타일(배경 아래) → 칸 간격(`FromGridOutline`, 칸 프리팹 모드면 예외) → 보드 크기 없으면 예외(P5) → 정렬 계산 → 격자 중앙 기준으로 회전 → 수평 이동 → 필드가 크면 경고(P6) → 카메라 읽기. 다른 방식은 지금과 같다. 타일 필드·`_tiles` 수명 삭제. |
| b.6 | `CombatLoadedScene.cs` | `Load`가 `values.TryReadBoardSize`로 읽어 `Show`에 넘긴다. |
| b.7 | `Factory/Runtime/InGame/View/CombatSceneDisplayFactory.cs` | `CreateBackdrop`이 `tables.Field`를 넘긴다(한 줄). |
| b.8 | `DataBaseSO/ViewGroup/StageViewProp/Item/Test.asset` | 기준점 `anchorTile: {x: -1, y: 0}`·`anchorHeight: -2`를 넣고 옛 `rootPosition`·`rootRotation`·`rootScale` 키 삭제(P12). |

화면 변화: Test 스테이지(필드 9 × 9, 타일 16 × 16)는 타일 격자 중앙이 필드 중앙에서 +½칸(+X·+Z) 옮겨지고, 칸 경계와 타일 경계가 겹친다. 다른 배경 방식은 그대로다.

### U-b 사용자 Unity 확인 (TA-b 뒤, 설계 §7)

1. 컴파일 오류 없음.
2. Test 스테이지: 칸 경계 = 타일 경계, 중앙 반 칸 옮김, 타일이 구멍 안.
3. 필드 (0,0) 칸이 화면 좌하단(R4).
4. 배경 `rotationOffset`을 −88.55°로 바꿔도 −90°와 같은 화면.
5. 배경 `positionOffset` x·z를 바꿔도 맵 자리가 같고, y는 따라간다.
6. 필드 크기를 짝수로 바꾼 경우(가능하면): 중앙 정확히, 경계 겹침.
7. 다른 배경 방식 스테이지(A·B·C·보스): 화면 그대로.
8. 높이(타일 윗면 대 필드 평면)는 `anchorPosition.y`로 맞춘다(범위 밖, 맞지 않으면 보고만).

## 4. TA-c 문서·공유·검토

- 설계 §9 구현 메모(계획과 다르게 한 것).
- 민형님 Factory 공유 문서(`handoffs/2026-10-15-combat-scene-display-factory-framework-share.md`)에 `CreateBackdrop` 한 줄 절(미전달). → **했음(2026-10-28):** 타일 높이 TH-e에서 표시 생성 줄과 함께 §37에 적었다.
- CLAUDE.md 현재 상태 줄.
- 묶음 끝 검토 에이전트 하나(7기준), 보고 뒤 반영은 사용자 결정.

## 5. 커밋 묶음 (사용자가 커밋)

- 한 커밋: 앞 단계 타일 묶음(새 `.cs` 넷 + `StageMapTiles.cs`, 에셋) + 이 계획의 새 `.cs` 하나(`CombatMapTileAlignment`, + `.meta`) + 바뀐 코드 + `Test.asset`.
- 프리팹은 고치지 않는다(P2 개정).

## 6. 실행 기록

### 6.1 TA-a 코드 완료 (2026-10-28, 사용자 "7기준에 문제가 없다면 진행해" → P1~P14, 컴파일 미확인, 새 시험 없음, 시험 변경 없음)

- 1판: 프리팹 기준점 컴포넌트 `CombatMapTileAnchor`(+ `.meta`)와 DB 검사를 넣었다.
- **개정(같은 날, 사용자 "MapTileAnchor를 배경에 넣지 말고 ViewStageDataSO에서 anchor 위치 넣는 방향으로 진행해"):**
  - `CombatMapTileAnchor.cs`·`.meta`(커밋 전 새 파일)를 지우고 DB 검사 줄을 걷었다.
  - `CombatMapTilesData.cs`: 옛 루트 값 셋 → 기준점 값 `anchorPosition`·`anchorRotation`·`anchorScale`(배경 기준, `FormerlySerializedAs("rootPosition")`·`("offset")`·`("rootRotation")`·`("rootScale")`), 데이터 속성 `AnchorPosition`·`AnchorRotation`·`AnchorScale`, 검사 문구, `CenterOf`를 모서리 하나(`Corner`)에서 계산, 같은 모서리에서 `GridCenter`(P14). 칸 자리 값은 전과 같다.
  - `StageViewPropDataSO.cs`: 툴팁 셋(`positionOffset`은 높이만, `rotationOffset`은 카메라와 함께 돌고 90° 단위로 맞춰짐, `mapTiles`에 기준점).
  - `StageMapTilePresenter.cs`: 속성 이름만(아직 칸 루트 아래).
- 계획과 다른 점: Q-TA1이 A → C(사용자 지시)로 바뀌어 P2·P7·P12와 a.1~a.3을 고쳤다. 루트 값과 기준점 값을 합친 것은 그 결과로 정한 것이다(SSOT, 위치·회전·크기 설정은 그대로).
- Phase 사이의 어긋남: TA-b 전까지 타일은 아직 칸 루트 아래에 놓이고, `Test.asset` 값도 옛 칸 루트 기준 그대로라(이름 바꿈으로 이어 받음) 화면은 지금과 같다.
- 사용자 작업: 없음.
- **개정 2(같은 날, 사용자 "위치만 남겨"):** 기준점 회전·크기 칸(`anchorRotation`·`anchorScale`)과 데이터 속성·검사를 지웠다. 타일 루트는 위치만 받고 회전·크기는 배경 것을 따른다(`StageMapTilePresenter`도 위치만 넣음). `Test.asset`의 옛 `rootRotation`·`rootScale` 키는 읽히지 않으며 TA-b(b.8)에서 지운다. 옛 회전 −90°·배율 0.5가 칸 루트 아래에서 빠져 TA-b 전까지 Test 타일이 돌지 않고 2배 크기로 보일 수 있다(TA-b에서 배경 아래로 옮기면 배경 회전·배율을 따름).
- **개정 3(같은 날, 사용자 "칸 단위 + 높이")** — 아래 한 줄, 그 뒤 TA-b는 §6.2: 기준점 `anchorPosition`(Vector3)을 칸 단위 자리 `anchorTile`(Vector2)과 높이 `anchorHeight`(float)로 나눴다. 배경 기준 위치는 데이터 `AnchorPosition`(칸 × `tileSize` + 높이) 한 곳에서 계산하고, `StageMapTilePresenter`는 그것을 읽는다. 형식이 달라 `FormerlySerializedAs`는 쓰지 않았다(옛 값은 이어지지 않음, `using UnityEngine.Serialization` 삭제). `Test.asset` 값은 TA-b(b.8)에서 (−1, 0)·−2로 넣는다.

### 6.2 TA-b 코드 완료 (2026-10-28, 사용자 "TA-b 진행", 컴파일 미확인, 새 시험 없음, 시험 변경 없음)

- b.1 `FieldCellSpacing.FromGridOutline(config)`(GridOutline이 아니면 예외), `FieldOutlineRenderer`는 생성자에서 이것으로 간격을 한 번 만들고(`_configuredSpacing`) `MeasureSpacing`이 돌려준다. `CanReuse`는 그대로.
- b.1b `FieldGridMapper.CornerLocal(columns, rows, cellWidth, cellLength)` 정적 함수, 생성자가 `_corner`로 받아 칸 변환 셋이 쓴다. 계산 순서가 같아 값은 비트 단위로 같다.
- b.2 `CombatDisplayValueReader.TryReadBoardSize(out Vector2Int)`.
- b.3 새 `Display/Map/CombatMapTileAlignment.cs`(+ `.meta`, GUID `6e56c435…`): `RequireLevel`(기울기 1°), `YawCorrection`(가장 가까운 90° 단위까지 남은 각, `RotateAround`에 그대로), `TargetCenter`(축 대응·축 평행 1°·정수배 0.001·짝·홀 반 칸·필드가 더 큰지).
- b.4 `StageMapTilePresenter.cs` → `StageMapTiles.cs`(GUID `8cdae8f5…` 유지): 정적 `Build(backdrop, tiles)`가 배경 인스턴스 아래 `AnchorPosition`에 타일 루트를 만들고 돌려준다.
- b.5 `StageViewPropPresenter`: 생성자 셋째 인자 필드 설정, `Show(stageKey, Vector2Int? boardSize)`, 타일 방식이면 `AlignToField`(보드 없으면 예외 → 칸 간격 → 기울기 → 남은 각만큼 격자 가운데 기준 회전 → 수평 이동 → 넘치면 경고) 뒤에 카메라를 읽는다. 타일 수명 필드를 걷었다(배경과 함께 없어짐).
- b.6 `CombatLoadedScene.Load`가 보드 크기를 읽어 넘긴다. b.7 `CombatSceneDisplayFactory.CreateBackdrop`이 `tables.Field`를 넘긴다(연결 줄, 민형님 공유는 TA-c).
- **b.8 하지 않음:** `Test.asset`에 이미 사용자가 넣은 기준점 값 `anchorTile: {x: 4, y: 4}`·`anchorHeight: 1.3`이 있어 덮어쓰지 않았다. 옛 루트 키도 이미 없다. 이 값은 TA-b부터 배경 기준(배경 회전 −90°·배율 0.5를 따름)으로 읽힌다. 앞서 맞춰 두었던 구멍 자리를 그대로 쓰려면 (−1, 0)·−2다(P12 계산).
- 계획과 다른 점: b.8(위). 그 밖에 없음.

### 6.3 TA-b 개정 2: 타일 루트는 칸 루트 아래 필드 기준 (2026-10-28, 사용자 "MapTileRoot 배경 자식으로 넣지마" → "칸 루트 아래 독립(필드 기준)", 컴파일 미확인, 새 시험 없음)

- 구조: `CellsRoot` 아래에 `MapTileRoot`(회전·배율 없음)와 배경이 형제. 타일은 필드에 맞추고, 배경은 "타일 격자 가운데 − `anchorTile × tileSize`"에 놓여 타일을 따라간다. 배경 회전만 90° 단위로 맞춘다(설계 TA1~TA3 개정 2).
- `CombatMapTilesData`: 기준점 뜻을 바꿈(`anchorTile` = 배경 원점에서 본 타일 격자 가운데, 필드 축 칸 단위 / `anchorHeight` = 타일 루트 높이, 필드 단위), `AnchorPosition` → `AnchorOffset`(가로만, 칸 × 타일 크기), `tileSize`는 필드 단위.
- `CombatMapTileAlignment`: `TargetCenter(tileSize, columns, rows, board, spacing, out exceeds)`로 줄임(타일 축이 늘 필드 축이라 축 대응·평행 검사 삭제). `RequireLevel`·`YawCorrection`은 배경 축에 씀.
- `StageMapTiles.Build(cellsRoot, tiles, localPosition)`이 타일 루트 `GameObject`를 돌려줌.
- `StageViewPropPresenter`: `_tileRoot`를 들고 `Release`에서 함께 없앰, `ShowTiles`(타일 자리 계산 → 타일 세우기 → 배경 기울기 검사·회전 보정 → 배경 가로 자리 → 넘치면 경고).
- Test: 사용자 값 `anchorTile` (4, 4)·`anchorHeight` 1.3 그대로. 이제 타일 크기가 2(배경 배율 안 따름)라 16 × 16 타일은 32 단위, 배경 구멍은 16 단위(배율 0.5)로 크기가 다르다 — 맞추는 방법은 사용자 결정(P12).

### 6.4 TA-b 개정 3: 타일 루트에 배경과 같은 보정 (2026-10-28, 사용자 "MapTileRoot에도 똑같이 offset 넣으라고" → "하던거 해라", 컴파일 미확인, 새 시험 없음)

- 구조: `CellsRoot` 아래 배경과 `MapTileRoot` 형제, 둘 다 `positionOffset`·`rotationOffset`·`scaleOffset`(배율은 배경 = 프리팹 크기 × 보정, 타일 루트 = 1 × 보정). 같은 보정을 넣는 규칙은 `StageViewPropPresenter.ApplyOffsets` 하나.
- `CombatMapTilesData`: 기준점 뜻 = 타일 루트 원점에서 본 격자 가운데(칸 단위)·타일 바닥 높이, 격자 모서리 `Corner`에 기준점을 넣어 `CenterOf`·`GridCenter`가 함께 씀(칸 높이도 `AnchorHeight`), `AnchorOffset` 삭제.
- `CombatMapTileAlignment`: 회전·배율이 걸린 걸음 벡터를 받는 `TargetCenter`로 되돌림(축 대응·평행 검사 포함), `RequireLevel`·`YawCorrection`은 타일 루트 축에 씀.
- `StageMapTiles.Build(cellsRoot, tiles)`: 루트를 만들고 칸을 채움(보정은 부르는 쪽).
- `StageViewPropPresenter.ShowTiles(data, tiles, …)`: 타일 루트 세우기·같은 보정 → 기울기 검사 → 남은 각만큼 격자 가운데 기준으로 둘을 똑같이 회전 → 둘을 똑같이 수평 이동 → 넘치면 경고 → (Show로 돌아가) 카메라 읽기.
- Test: 사용자 값 (4, 4)·1.3 그대로(P12).

### 6.5 TA-b 개정 4: 기준점 = 게임 (0,0) 칸이 놓일 타일 칸 (2026-10-28, 사용자 스크린샷 "Anchor Tile 0, 0일 때 이런 느낌" → 선택 "타일 맵 칸 좌표", 컴파일 미확인, 새 시험 없음)

- 뜻: `anchorTile`(`Vector2` → `Vector2Int`, 이름·직렬화 키 그대로) = 게임 그리드 (0,0) 칸이 놓일 타일 맵 칸. 화면(필드) 기준 좌하단(필드 −X·−Z) 타일이 (0,0), x = 필드 X·y = 필드 Z 방향 타일 수. 배경 회전과 무관하고, 타일 루트 순서(`KindAt`·종류 목록)와는 회전만큼 다를 수 있다(종류 목록 툴팁의 "필드 −Z 쪽 줄"도 "타일 루트 −Z 쪽 줄(회전 전)"로 바로잡음 — 개정 3부터 틀린 문장이었음).
- `CombatMapTilesData`: 격자 모서리 `Corner`에서 기준점을 뺌(격자 가운데 = 루트 원점, 높이 = `AnchorHeight`). 0 이상만 DB에서 검사한다.
- `CombatMapTileAlignment.TargetCenter`·`HalfCellShift`(짝·홀 반 칸) → `AnchorShift(gridCenter, columnStep, rowStep, columns, rows, anchorTile, board, spacing, out exceeds)`: 축 대응·평행·정수배 검사는 그대로, 기준 타일이 화면 기준 타일 맵 안인지 검사, 타일 맵 좌하단 자리 = 게임 (0,0) 칸 좌하단 − 기준점 × 타일 한 장, 옮길 양을 돌려줌. 넘침은 칸 단위 정수 비교(+X·+Z 쪽만).
- `StageViewPropPresenter.ShowTiles`: 옮길 양을 바로 받아 둘에 더함, 경고 문구에 기준 타일.
- 확인 계산(Test, 필드 9 × 9·타일 16 × 16·크기 2 × 배율 0.5 = 칸 1·회전 −90°·위치 (0.4, −0.3, 0)): 남은 각 0, 격자 가운데 (0.4, 0) → 기준점 (3, 3)이면 타일 맵 좌하단 = (−4.5 − 3, −4.5 − 3) = (−7.5, −7.5), 옮길 양 (0.1, 0.5), 배경·`MapTileRoot` 위치 (0.5, −0.3, 0.5) = 10-28 스크린샷(개정 3·기준점 0)과 같은 자리. 지금 사용자 값 (4, 4)면 둘 다 (−0.5, −0.3, −0.5)로 타일 한 장 좌하단 쪽.
- `Test.asset`은 고치지 않았다(P12).

### 6.6 구멍 자리 칸 `tileOffset` 되살림 (2026-10-28, 사용자 "MapTile이 배경 구멍이랑 딱 맞게 세팅되는 구석이 없는데, MapTileOffset 따로 있어야 하지 않나", 컴파일 미확인, 새 시험 없음)

- 원인: 개정 3까지 `anchorTile`이 "배경 원점에서 본 구멍 가운데(칸 단위)"였는데, 개정 4(§6.5)가 그 뜻을 "게임 (0,0)이 놓일 타일 칸"으로 바꾸며 구멍 자리를 적을 칸이 없어졌다(격자 가운데 = 배경 원점으로 고정). 개정 4의 빠뜨림이다.
- `CombatMapTilesInput.tileOffset`(Vector2, 칸 단위, x = 타일 루트 X·y = 타일 루트 Z, 배경 보정 전, 소수 가능, 유한 값 검사) → `CombatMapTilesData.TileOffset`. 격자 모서리 `Corner`가 `(TileOffset − 칸 수/2) × TileSize`라 `CenterOf`·`GridCenter`가 함께 따른다. 맞춤 계산·배경 표시는 `GridCenter`를 재므로 코드 변경 없음.
- `Test.asset`: `tileOffset: {x: -1, y: 0}`. 근거 둘 — FBX 구멍이 파일 x −1400~1800(가운데 +200 = 타일 한 장)·y −1600~1600(가운데 0)이고 Unity 가져오기가 X를 뒤집음, 그리고 P12에서 예전에 사용자가 맞춘 자리를 배경 기준으로 바꾼 값 (−1, 0)과 같음. 사용자 값 `anchorTile` (3, 3)·`anchorHeight` −1.3은 그대로.
- 그 뒤 디스크의 `Test.asset` `tileOffset`이 (0, 0)으로 바뀌어 있었다(에디터가 컴파일 직후 메모리 판을 저장했거나 사용자가 바꿈). 되돌리지 않았다. 구멍에 맞추려면 계산값 (−1, 0)을 넣어 본다(축 방향은 Unity에서 확인).

### 6.7 개정 5: 타일 루트는 배경 자식 + Offset x·y·z (2026-10-28, 사용자 "MapTile 배경 하위로 넣고 Offset 넣는걸로 하자 x,y,z. 그 후에 조정하는걸로", 컴파일 미확인, 새 시험 없음)

- 구조: `MapTileRoot`는 배경 인스턴스 아래 자식, 배경 로컬 `offset`에 회전 없이·배율 1로 놓는다(배경 회전·배율을 부모로 받음). 맞춤(기울기 검사 → 남은 각 회전 → 기준점 이동)은 배경만 돌리고 옮기고, 타일은 따라온다. 배경 표시의 타일 루트 필드 `_tileRoot`·보정 공유 함수 `ApplyOffsets`는 걷었다(배경 보정은 `Show` 안 세 줄, 해제는 배경째).
- 데이터: `CombatMapTilesInput.offset`(Vector3, 유한 값 검사) → `CombatMapTilesData.Offset`. §6.6의 `tileOffset`(칸 단위)·`anchorHeight`는 지웠다(형식이 달라 `FormerlySerializedAs`는 안 씀). 격자 모서리는 루트 원점 가운데·높이 0이고, 자리·높이는 루트 위치가 정한다. `anchorTile`(게임 (0,0)이 놓일 타일 칸)은 그대로.
- `StageMapTiles.Build(backdrop, tiles)`가 루트를 배경 아래 `Offset` 자리에 만든다.
- `Test.asset`: 지금 값을 같은 자리로 옮겨 `offset: {x: 0, y: -1.3, z: 0}`(옛 `tileOffset` (0, 0)·`anchorHeight` −1.3, 배경 프리팹 루트 배율이 1이라 단위가 같음), `anchorTile` (3, 3) 그대로. 구멍에 맞추는 값은 사용자가 조정(FBX 계산으로는 x 쪽 타일 한 장 = 배경 로컬 −2 근처).

### 6.8 타일 종류별 윗면 높이 (2026-10-28, 사용자 "CombatMapTile Set SO에 높이값 추가, 타일마다 높이가 다름. 추후 이 값이 인게임까지 전달되어야 함", 컴파일 미확인, 새 시험 없음)

- 타일 세트 SO 항목에 `surfaceHeight`(타일 피벗(바닥)에서 윗면까지, 타일 루트 단위 = `tileSize`와 같은 단위, `Min(0)`, 유한 값·0 이상 검사 — 지형 외형 `surfaceHeight`와 같은 규칙).
- 종류 하나의 외형 값 새 `CombatMapTileVisual`(프리팹·윗면 높이, + `.meta`). 세트는 `CreateVisuals()`로 종류 → 외형 표를 만들고(옛 `CreatePrefabs`), 배치 데이터는 `VisualOf(kind)` 하나로 찾는다(옛 `PrefabOf`). `StageMapTiles`는 `.Prefab`을 읽는다. 높이는 지금 값만 보관하고 쓰는 곳은 없다.
- 임시 세트 에셋은 고치지 않았다(두 종류 모두 0으로 읽힘, 인스펙터에서 넣음).
- **정함(옛 "열린 것", 2026-10-28 사용자 "화면 표시") → [타일 높이 설계](../specs/2026-10-28-combat-map-tile-height-design.md)·[계획](2026-10-28-combat-map-tile-height-plan.md)(TH-a~TH-e):** 높이는 화면 표시용이다. 원본은 View DB 그대로이고 Core·Buffer는 바꾸지 않았다. 뜻도 바뀌었다 — 필드 바닥면 기준 위아래(필드 단위, 음수 = 들어간 타일), `Min(0)` 걷음. 칸 높이 표의 바탕 층으로 들어가 유닛·강조·지형 외형 등이 타일 윗면을 따른다. 아래는 정하기 전 기록이다.
- **(정하기 전) 열린 것 — "인게임까지 전달":** 지금 값은 View DB(ViewGroup)에 있다. 프로젝트 규칙상 View는 Core에 값을 넣지 않고 InGame은 View DB를 읽지 않으므로, 이 높이가 Core 규칙(이동·사거리·고지대 등)에 쓰이려면 원본을 InGame 데이터로 옮기고 View는 Buffer나 같은 키로 받는 쪽으로 다시 설계해야 한다. 그때 타일 칸 ↔ 필드 칸 대응(`anchorTile`·배경 회전·타일 한 장 = 칸 k개)도 Core가 알아야 하는 사실이 된다. "인게임"이 런타임 화면(유닛이 타일 윗면에 서기 등 View 쪽)이면 지금 자리 그대로 쓴다. 정할 때 설계를 따로 쓴다.
