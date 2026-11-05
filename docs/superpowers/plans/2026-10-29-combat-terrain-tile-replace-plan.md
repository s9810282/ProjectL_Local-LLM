# 전투 지형 외형 — 타일 위 / 바닥 타일 교체 Task 계획

- 날짜: 2026-10-29
- 설계: [terrain-tile-replace-design](../specs/2026-10-29-combat-terrain-tile-replace-design.md)(TR1~TR7, P1~P7, 결정 Q-TR1~Q-TR4)
- 기준 판: HEAD `3be9ad77`
- 상태: P8~P14 그대로 확정(2026-10-29 사용자). TR-a 완료(§6.1, Ua 문제 없음), TR-b 완료(§6.2, Ub 문제 없음), TR-c 완료(§6.3, Uc 문제 없음), TR-c2 붕괴 떨림·가라앉기 코드·에셋 완료(§6.4, 컴파일 미확인, Uc2 대기). 다음 TR-d.
- 진행 규칙
  - Phase마다 멈추고 보고한다.
  - View 작업이라 새 시험을 쓰지 않는다. 전투 View라 MSBuild도 돌리지 않는다. 보고에는 "컴파일 미확인"을 적는다.
  - 검토 에이전트는 묶음 끝(TR-d)에 한 번만 돌린다.
  - 커밋은 사용자가 한다.

## 0. 계획에서 정한 것 (확인 대상)

- **P8** 생성자의 "표면 높이가 있으면 프리팹 필수" 검사를 지운다. 표의 "타일 위는 프리팹 필수" 하나가 맡는다(타일 위만 높이로 올리므로 같은 뜻).
- **P9** TR-a에서는 바닥 교체 종류를 높이 표에 넣지 않는다(TR-b부터 `Replace`). 그래서 TR-a 동안 바닥 교체 에셋에 음수 높이를 넣어도 멈추지 않는다.
- **P10** 한 칸의 교체 원본이 여럿이면, 첫 교체는 타일 바탕을 덮고 그다음부터는 낮은 값이 남는다(설계 P4의 "낮은 값"은 교체끼리의 비교다. 타일 바탕보다 높은 교체도 타일 바탕을 덮어야 해서 다시 채우기마다 "이 칸을 교체했나" 표시를 둔다).
- **P11** 교체 인스턴스는 쓰는 동안 끈 타일의 부모(`MapTileRoot`) 아래로 옮기고, 반환 때 지형 루트(`FieldTerrains`)로 되돌린다.
  - 설계 TR3은 "타일의 월드 크기를 축별로 나눠 넣기"였다. 부모를 옮기면 크기 변환이 필요 없고, 배경 배율이 고르지 않거나 90° 돌아 있어도 타일과 똑같이 놓인다.
  - 배경이 먼저 없어지면 인스턴스도 함께 파괴된다. 풀은 파괴된 항목을 건너뛴다(지금 `Rent`의 null 검사).
- **P12** 타일 칸에서 외형 보정 위치·크기·회전은 타일 루트 기준이다(교체 프리팹을 타일처럼 만들기 때문). 외형 회전(스테이지 회전·이웃 모양)도 타일 루트의 Y축으로 돈다. 타일 없는 칸은 지금처럼 필드 기준이다.
- **P13** 타일 켜짐은 바뀔 때만 `SetActive`를 부른다. 끌 칸 표시는 `Render`마다 다시 채우는 배열이다(할당 없음).
- **P14** 타일 나눔(한 타일에 칸 둘 이상)은 높이 표를 만들 때 사실로 돌려주고, 경고는 배경 표시가 남긴다(지금 `fieldExceedsTiles` 경고와 같은 자리·꼴).

## 1. Phase TR-a 값 (화면 변화 없음)

| Task | 파일 | 내용 |
|---|---|---|
| Ta.1 | `DataBaseSystem/Data/View/Table/Terrain/Item/TerrainVisualMode.cs` | `PlacedPrefab` → `OnTile`(0), `GroundShape` → `ReplaceTile`(1), 문서 주석 |
| Ta.2 | `TerrainVisualDataSO.cs`(Data) | 표면 높이 검사를 방식별로 바꾼다(타일 위: 유한한 0 이상, 바닥 교체: 유한). "높이 있으면 GroundShape"·"높이 있으면 프리팹"(P8) 삭제. "토네이도는 타일 위만" 추가. `SurfaceHeight` 문서에 방식별 뜻 |
| Ta.3 | `TerrainVisualDataSO.cs`(SO) | `visualMode` 기본값 `OnTile`(기본 종류가 고지대), 툴팁 둘(방식·표면 높이) |
| Ta.4 | `Queries/ViewDataBase/ViewTable/TerrainVisualTable.cs` | 검사 `OnTile && Prefab == null`, 문구·요약 |
| Ta.5 | `ViewSystem/Combat/Display/Map/FieldTerrainPresenter.cs` | `RaiseSurface`는 타일 위 종류만 올린다(P9). `CanDisplay`·클래스 주석의 "설치형·바닥 변경" 문구 |
| Ta.6 | `DataBaseSO/ViewGroup/TerrainVisual/Item/TerrainVisual_HighGround.asset` | `visualMode: 1` → `0` |

- 그대로인 것: 약한 지반·낭떠러지 에셋(이미 1), 높이 값.
- 이 에셋 파일에는 에디터가 더한 `tornadoPresentation: {fileID: 0}` 한 줄이 작업 트리에 이미 있다(이번 작업 아님, 함께 커밋해도 무해).
- **Ua(사용자 Unity 확인):** 컴파일, Play From Boot로 Test 진입. 고지대 블록·약한 지반이 지금처럼 보이는지, DB 생성 오류가 없는지 본다.

## 2. Phase TR-b 높이 (바탕 바꾸기)

| Task | 파일 | 내용 |
|---|---|---|
| Tb.1 | `Display/Field/FieldSurfaceHeights.cs` | 타일 바탕 `_tileBase`(`ClearBase`·`SetBase`가 씀)와 이번 바탕 `_base`로 나눈다. `BeginRefill`·`Reset`이 타일 바탕을 이번 바탕으로 복사한다. 새 `Replace(cell, height)`(유한 검사, P10 교체 표시 배열, 칸 높이 = 이번 바탕 + 지형). `EndRefill`은 칸 높이와 지형 높이를 모두 비교한다(지형 이전 값 배열 하나 추가). 클래스·`BaseHeightAt` 문서 |
| Tb.2 | `FieldTerrainPresenter.cs` | `RaiseSurface`가 방식으로 갈린다: 타일 위 → `Raise`, 바닥 교체 → `Replace` |

- 화면 변화: 약한 지반 칸의 칸 높이가 그 칸 타일 윗면 대신 약한 지반 표면 높이(지금 0)가 된다. Test에서 그 칸 타일이 −0.1이면 유닛이 0.1 올라간다.
- 격자(칸 테두리·채우기·경로선·호버·데칼·포인터)는 지형 높이만 따르므로 바뀌지 않는다.
- **Ub:** 약한 지반 표면 높이를 몇 가지로 바꿔 보며 그 칸 유닛이 그 높이에 서는지, 격자는 그대로인지 본다. 낭떠러지 표면 높이를 음수로 두고 넉백으로 떨어뜨리면 걸음이 구멍 바닥 높이로 끝난 뒤 추락 연출이 이어지는지 본다.

## 3. Phase TR-c 타일 교체

| Task | 파일 | 내용 |
|---|---|---|
| Tc.1 | `Display/Map/StageMapTiles.cs` | `Build`가 만든 타일을 맵 칸별(`Transform[columns, rows]`)로 함께 돌려준다 |
| Tc.2 | `Display/Map/CombatMapTileSurface.cs` | 칸별 타일 `Transform`도 든다(같은 `TryTileAt` 반복에서). 새 읽기 `TryGetTile(cell, out Transform)`(없거나 파괴됐으면 false). `Build`가 타일 나눔 사실을 돌려준다(P14). 문서 "칸별 바닥 타일(윗면 높이·타일 오브젝트)" |
| Tc.3 | `Display/Map/StageViewPropPresenter.cs` | 타일 배열을 표에 넘기고, 나눔이면 경고 한 번 |
| Tc.4 | `Display/Map/FieldTileReplacement.cs`(새, + `.meta`) | `Bind(tiles)`(바뀌면 옛 표의 타일을 먼저 켬), `BeginMarks`·`Mark(cell)`·`Apply`(바뀐 타일만 `SetActive`, P13), `Restore`(끈 타일 모두 켬), `TryGetTile` |
| Tc.5 | `FieldTerrainPresenter.cs` | 부품을 갖고 `BindTiles(tiles)` 입구. 이웃 표 방문에서 바닥 교체 칸도 표시 → `Apply`. `Apply`·자세: 바닥 교체이고 타일이 있으면 타일 부모 아래 타일 자세(P11·P12), 없으면 칸 가운데 바닥면 + 보정(설계 P7). `Return`은 지형 루트로 되돌림. `ShowChanged` 조건(설계 TR6). `Clear`·`Release`가 타일을 켬 |
| Tc.6 | `Display/CombatSceneDisplay.cs` | `Install`의 `SetGroundBase` 다음 줄에 `_terrain.BindTiles(_tileSurface)` |
| Tc.7 | `TerrainVisual_Cliff.asset` | `prefab`을 비운다(Q-TR2 B) |

- 지우지 않는 것: `Prefabs/Default/Environment/Terrain_Cliff.prefab`, `Environment/Cliff/CliffSurface.mat`. Tc.7 뒤 참조가 없어진다(CLAUDE.md 규칙대로 보고만).
- 약한 지반 교체 프리팹은 사용자가 타일 프리팹처럼 새로 만든다. 지금 `Terrain_WeakGround.prefab`(칸 바탕용 사각형)은 타일 피벗 자리에 놓이므로 타일 칸에서 엉뚱한 높이에 보일 수 있다.
- **Uc:**
  1. Test 진입 때 약한 지반 칸 (6,3)의 타일이 꺼지고 교체 프리팹이 그 자리에 놓인다.
  2. 붕괴하면 약한 지반 외형이 바로 빠지고 구멍이 남는다.
  3. 결과 끝에 구멍 칸 높이가 낭떠러지 값으로 바뀐다.
  4. 전투를 나가면(또는 표시 해제 뒤) 타일이 모두 다시 켜진다.
  5. 고지대·벽·토네이도 자리는 그대로다.

## 4. Phase TR-d 문서·공유·묶음 끝 검토

| Task | 문서 | 내용 |
|---|---|---|
| Td.1 | [PlacedPrefab 지형 안내](../viewDocs/placed-prefab-terrain-guide.md) | 방식 두 값·표면 높이 뜻·바닥 교체 프리팹 만드는 법 |
| Td.2 | [폴더 배치 안내](../viewDocs/combat-view-folder-guide.md) | `Display/Map/FieldTileReplacement` 줄, 칸 높이 규칙 줄(바탕 바꾸기) |
| Td.3 | [SO 모드 3D 사본 공유](../handoffs/2026-10-27-combat-stage-editor-so-mode-view-share.md) §5 | 호진님 도구가 표면 높이를 모두 "올림"으로 읽어 바닥 교체 높이를 잘못 그림, 타일 교체도 모름(미전달) |
| Td.4 | 설계 §8 구현 메모 | 계획과 다른 점, 남은 것 |
| Td.5 | 검토 에이전트 하나 | 7기준, 묶음 전체 |

## 5. 커밋 묶음 (사용자)

- 코드와 에셋은 한 커밋이다. 에셋만이면 옛 코드가 "높이 있는 PlacedPrefab"을 거절해 DB 생성이 멈춘다. 코드만이면 고지대가 바닥 교체로 읽혀 블록 높이를 잃는다(TR-a는 올리지 않고, TR-b부터는 0.45가 새 바탕이 됨).
- 새 파일 `FieldTileReplacement.cs` + `.meta`.

## 6. 실행 기록

### 6.1 TR-a 코드·에셋 완료 (2026-10-29, 사용자 "그대로 TR-a", 컴파일 미확인, 새 시험 없음)

P8~P14는 그대로 확정했다(사용자).

| Task | 한 것 |
|---|---|
| Ta.1 | `TerrainVisualMode` 값 이름 `OnTile`(0)·`ReplaceTile`(1), 방식별 표면 높이 뜻을 문서 주석에 |
| Ta.2 | `TerrainVisualData` 생성자: 표면 높이는 유한 검사 하나 + "타일 위면 0 이상", "토네이도는 타일 위만" 추가. "높이 있으면 GroundShape"·"높이 있으면 프리팹"(P8) 삭제. `SurfaceHeight` 문서에 방식별 뜻 |
| Ta.3 | SO 기본 방식 `OnTile`, 툴팁 둘. 표면 높이 칸의 `Min(0f)`를 뗐다(바닥 교체는 음수가 됨, 타일 위 음수는 생성자가 거절) |
| Ta.4 | 표 검사 `OnTile && Prefab == null`, 문구·요약 |
| Ta.5 | `FieldTerrainPresenter.RaiseSurface`는 타일 위 종류만 올림(P9). 클래스·`SyncSurface`·`CanDisplay` 주석, `TerrainViewSet` 주석 둘의 "설치형·바닥 변경" 문구 |
| Ta.6 | `TerrainVisual_HighGround.asset` `visualMode: 1` → `0`(LF 그대로) |

- 계획과 다른 점: `TerrainViewSet.cs` 주석 두 줄도 새 이름으로 고쳤다(계획 표에 없던 파일, 동작 무변경). 표면 높이 칸의 `Min(0f)` 삭제도 계획 표에 없었다.
- 옛 이름(`PlacedPrefab`·`GroundShape`)이 남은 `.cs`는 없다. 문서(PlacedPrefab 지형 안내 등)는 TR-d에서 고친다.
- 화면 변화: 없어야 한다. 약한 지반·낭떠러지는 표면 높이 0이라 전에도 표를 바꾸지 않았다.
- 같은 작업 트리에 다른 작업(GridOutline 이동 경로선 값 나눔: `StageFieldViewDataSO.cs`·`FieldOutlineRenderer.cs`·`StageFieldView_Default.asset`·문서 둘)과 에디터 변경(FMOD 캐시·글꼴 아틀라스)이 섞여 있다. 이번 작업 파일이 아니다.
- **Ua(사용자):** 컴파일, Play From Boot로 Test 진입 → DB 생성 오류 없음, 고지대 블록 높이·약한 지반 외형이 지금처럼 보임. 인스펙터에서 고지대 방식이 `OnTile`, 약한 지반·낭떠러지가 `ReplaceTile`로 보임.
- **Ua 문제 없음(2026-10-29 사용자).**

### 6.2 TR-b 코드 완료 (2026-10-29, 사용자 "Ua 문제 없음, TR-b", 컴파일 미확인, 새 시험 없음)

| Task | 한 것 |
|---|---|
| Tb.1 | `FieldSurfaceHeights`: 타일 바탕 `_tileBase`(`ClearBase`·`SetBase`가 씀)와 이번 바탕 `_base`, 교체 표시 `_replaced`, 지형 이전 값 `_previousTerrain`. 새 `Replace(cell, height)`(유한 검사, 첫 교체는 타일 바탕을 덮고 그다음은 낮은 값 — P10, 칸 높이 = 이번 바탕 + 지형, 격자 높이 무변경). `Reset`·`BeginRefill`은 공통 `StartFromTiles`(이번 바탕·칸 높이 = 타일 바탕, 지형·교체 표시 비움). `EndRefill`·`Reset`은 칸 높이와 지형 높이를 모두 비교. `BaseHeightAt`은 이번 바탕. 클래스·필드·`Raise` 문서 |
| Tb.2 | `FieldTerrainPresenter`: `RaiseSurface` → `FillSurface`(위임 필드 `_fillSurface`), 바닥 교체 → `Replace`, 타일 위 → `Raise`. `SyncSurface`·예외 정리·`LocalPosition` 주석 |

- 계획과 다른 점: `FieldGridMapper.CellBaseToLocal` 문서 주석(바탕 = 바닥 교체면 그 지형 높이)도 고쳤다(계획 표에 없던 파일, 동작 무변경).
- `Replace`가 바탕과 같은 값이면 교체 표시만 남기고 `Version`을 올리지 않는다(다시 채우기 밖에서 불려도 헛갱신 없음).
- 바탕을 읽는 곳은 둘이다: `FieldGridMapper.CellBaseToLocal`(지형 외형 자리)·`FieldTerrainPresenter.TornadoBaseHeight`. 둘 다 이번 바탕을 읽어, 바닥 교체 칸 위의 타일 위 외형은 바뀐 바탕에 놓인다. 바닥 교체 외형 자신도 TR-c 전까지는 자기 바탕(= 자기 표면 높이)에 놓인다.
- 바탕 반영 시점: `SetGroundBase`(설치)는 타일 바탕만 바꾸고, 이번 바탕·칸 높이는 다음 다시 채우기부터 바뀐다(전과 같음 — 설치와 첫 Refresh 사이에 바탕을 읽는 곳이 없음을 확인).
- 남은 것: 약한 지반과 낭떠러지의 표면 높이를 다르게 두면, 붕괴 순간 `ShowChanged`가 "높이 다름" 조건으로 외형 바꾸기를 결과 끝 `Render`로 미룬다(지금 값은 둘 다 0이라 바로 바뀜). 바닥 교체끼리는 바로 바꾸는 조건은 TR-c Tc.5(설계 TR6).
- **Ub(사용자):**
  1. Test 진입 — 약한 지반 칸 (6,3)의 유닛이 약한 지반 표면 높이(지금 0)에 선다. 그 칸 타일이 −0.1이면 0.1 올라간다.
  2. 약한 지반 `surfaceHeight`를 몇 가지(예: 0.2, −0.3)로 바꿔 다음 진입에서 그 칸 유닛·고스트가 그 높이를 따르는지 본다.
  3. 격자(칸 테두리·채우기·경로선·호버·포인터)는 그 칸에서 높이가 바뀌지 않는다.
  4. (선택) 낭떠러지 `surfaceHeight`를 음수로 두고 약한 지반을 무너뜨린 뒤, 넉백으로 그 칸에 밀린 유닛의 걸음이 구멍 바닥 높이에서 끝나고 추락 연출이 이어지는지 본다. 약한 지반과 높이가 달라 붕괴 순간 외형 바꾸기는 결과 끝까지 미뤄진다(위 "남은 것").
- **Ub 문제 없음(2026-10-29 사용자).**

### 6.3 TR-c 코드·에셋 완료 (2026-10-29, 사용자 "Ub 문제 없음, TR-c", 컴파일 미확인, 새 시험 없음)

| Task | 한 것 |
|---|---|
| Tc.1 | `StageMapTiles.Build(backdrop, tiles, out Transform[,] placed)` — 만든 타일을 타일 맵 칸별로 돌려줌. 문서에 "켜짐은 지형 표시가 맡음" |
| Tc.2 | `CombatMapTileSurface`: 칸별 타일 `_tiles`(높이와 같은 `TryTileAt` 반복에서), `TryGetTile(cell, out Transform)`(맵 밖·파괴면 false), `Build`에 `placed`·`out bool sharesTiles`(만든 타일 수 검사 포함). 문서 "칸별 바닥 타일(윗면 높이·타일 오브젝트)" |
| Tc.3 | `StageViewPropPresenter.ShowTiles`: 타일 배열을 표에 넘기고, 나눔이면 경고 한 번(`fieldExceedsTiles` 경고 옆) |
| Tc.4 | 새 `Display/Map/FieldTileReplacement.cs`(+ `.meta` GUID `6bdb2d91…`): `Bind`(다른 표면 옛 표 타일 먼저 켬)·`TryGetTile`·`BeginMarks`·`Mark`·`Apply`·`Restore`. 자기가 끈 타일만 기억하고 다시 켬(원래 꺼져 있던 타일은 건드리지 않음). 나눔 타일은 한 칸만 표시돼도 끔 |
| Tc.5 | `FieldTerrainPresenter`: 부품 `_tiles`·입구 `BindTiles`. `FillNeighbors` → `MarkShownCells`·`MarkNeighbor` → `MarkShown`(이웃 표 + 바닥 교체 칸 표시, 프리팹 유무 무관) → `Render`가 `_tiles.Apply()` 뒤 그리기. `Instance.ReplacesTile`. `Apply`가 `PlaceOnTile`(바닥 교체 + 타일 있음: 타일 부모 아래 `tile.localPosition + 보정`, 회전 `YawOf(외형 회전) × 프리팹 기본 회전`, 크기 = 프리팹 × 외형 배율 — P11·P12)·`PlaceInField`(그 밖: 지형 루트 아래 지금처럼)로 갈림. `LocalPosition`은 바닥 교체면 칸 가운데 바닥면(설계 P7). `Return`이 지형 루트로 되돌림. `ShowChanged` 조건(이웃 모양·방식 다름·타일 위인데 높이 다름이면 Render로 미룸, 바닥 교체끼리는 바로). `Clear`가 끝에 타일을 켬(finally), `Release`가 표를 놓음(`Bind(null)`) |
| Tc.6 | `CombatSceneDisplay.Install`: `SetGroundBase` 다음 줄 `_terrain.BindTiles(_tileSurface)` |
| Tc.7 | `TerrainVisual_Cliff.asset` `prefab: {fileID: 0}`(LF 그대로) |

- 계획과 다른 점:
  - `FieldGridMapper.YawOf(Rotation4)`(정적)를 새로 두고 `RotationToWorld`도 이것을 부른다. 타일 루트 기준 회전(P12)이 같은 식을 쓰게 해 90° 식을 한 곳에 둔다(계획 표에 없던 파일).
  - 이웃 표 채우기 이름이 `MarkShownCells`·`MarkShown`으로 바뀌었다(같은 방문에서 끌 칸도 표시).
- 그대로인 것: 사건 시점 `ShowAt`(토네이도 재배치)은 타일 자세를 모른다. 바닥 교체 외형을 옮기는 사건이 없어 그대로 두었다.
- 참조가 없어진 에셋(지우지 않음, CLAUDE.md 규칙 — 보고만): `Prefabs/Default/Environment/Terrain_Cliff.prefab`. `Environment/Cliff/CliffSurface.mat`은 이 프리팹만 참조한다.
- 약한 지반 교체 프리팹: 지금 `Terrain_WeakGround.prefab`(칸 바탕용 사각형)은 타일 피벗 자리(`tile.localPosition`)에 놓인다. 타일 프리팹과 같은 크기·피벗으로 새로 만들고 윗면을 약한 지반 `surfaceHeight`와 같게 해야 한다(사용자).
- **Uc(사용자):**
  1. Test 진입 → 약한 지반 칸 (6,3)의 타일(`MapTileRoot/Tile_x_y`)이 꺼지고, 약한 지반 외형이 `MapTileRoot` 아래 그 타일 자리에 놓인다.
  2. 약한 지반이 무너지면 외형이 바로 빠지고 구멍이 남는다(낭떠러지 프리팹 없음). 결과 끝에 그 칸 높이가 낭떠러지 값이 된다.
  3. 전투를 나가거나 표시를 해제하면 꺼진 타일이 다시 켜지고, 교체 외형은 `FieldTerrains` 아래로 돌아간다.
  4. 고지대·벽·토네이도·모래폭풍 자리와 격자(칸 테두리·채우기·경로선·호버)는 그대로다.
  5. 타일 하나에 칸이 여럿 놓이는 데이터면 콘솔에 경고 한 줄이 나온다(Test는 한 장 = 한 칸이라 나오지 않아야 함).
  6. 타일이 없는 배경 스테이지(A·B·C·보스)는 변화 없음(교체 지형 배치 없음).
- **Uc 문제 없음(2026-10-29 사용자, 낭떠러지로 바뀌는 것까지 확인).** 이어서 사용자 요청: "바뀔 때 단순히 SetActive를 끄는 게 아니라 해당 타일이 떨리면서 아래로 내려갔으면" → 설계 TR8, Phase TR-c2를 TR-d 앞에 둔다.

### 6.4 TR-c2 붕괴 떨림·가라앉기 코드·에셋 완료 (2026-10-29, 사용자 "권장안대로" = Q-CL1 A·Q-CL2 A, 컴파일 미확인, 새 시험 없음)

| 파일 | 한 것 |
|---|---|
| `CombatPresentationSettingsDataSO.cs` | 새 묶음 "약한 지반 붕괴 연출 (타일 교체 설계 TR8)" — `tileCollapseShakeSeconds` 0.35·`tileCollapseShakeStrength` 0.04·`tileCollapseShakeFrequency` 25·`tileCollapseSinkSeconds` 0.5·`tileCollapseSinkDepth` 1.5, 읽기 다섯, 조건 함수 `ValidateTileCollapseAmount`(유한한 0 이상), 검사는 파일 규칙대로 `Validate()` 맨 뒤 |
| `CombatPresentationSettings_Default.asset` | 같은 값 다섯 줄(`fallEndScale` 다음, LF 그대로). 그 뒤 사용자가 떨림 세기를 0.1로 바꿨다(TR-d 검토 때 확인) |
| 새 `Presentation/Environment/CombatTileCollapseSettings.cs`(+ `.meta`) | 값 객체 `From(settings)`, `TotalSeconds` |
| 새 `Presentation/Environment/CombatTileCollapse.cs`(+ `.meta`) | 시작 월드 위치에서 떨림(필드 가로 두 축 사인, 둘째 축 빠르기 ×1.37) → 가라앉기(필드 위쪽 반대), `IsShaking`·`IsDone`·`Finish`. 외형 끄기·반환은 부르는 쪽 |
| `Presentation/Pieces/CombatSinkPiece.cs` | 가라앉기 곡선을 정적 `SinkDistance` 하나로 꺼냄(유닛 추락과 붕괴가 같이 씀, 동작 같음) |
| `Display/Map/TerrainViewSet.cs` | `TryReplace`에 선택 인자 `leave`(빠지는 옛 View를 반환 대신 넘김), `ShowOne`·`Remove`도 같은 인자 |
| `Display/Map/FieldTerrainPresenter.cs` | 생성자 넷째 인자 `CombatTileCollapseSettings`, 무너짐 목록 `_collapses`(열쇠·외형·연출), `Leave`(보이는 바닥 교체 외형이면 무너짐 시작, 그 밖·시간 0·Mapper 없음은 바로 반환), `ShowChanged`가 `leave`를 넘김, `IsAppearancePlaying`(토네이도 또는 무너짐 남음)·새 `IsAppearanceBlockingStart`(토네이도 또는 떨리는 중)·`TickAppearance`(다 무너지면 반환)·`CancelAppearance`·`Clear`(남은 무너짐 바로 반환) |
| `Playback/Players/CombatTornadoAppearancePlayer.cs` → `CombatFieldAppearancePlayer.cs`(`git mv`, `.meta` GUID 그대로) | 생성자에 `blocksStart`, `FindBlock`은 그것으로·완료와 결과 끝 반영은 `isPlaying`으로 |
| `Playback/Players/CombatStartQuery.cs` | 막는 이유 `TornadoAppearance` → `FieldAppearance` |
| `Playback/CombatFieldEventPorts.cs` | 칸 이름 `TickAppearance`·`AppearancePlaying`·`CancelAppearance`(옛 토네이도 이름), 새 칸 `AppearanceBlocksStart` |
| `Playback/CombatSceneEventPresenter.cs` | 재생기 생성 한 줄 |
| `Factory/.../CombatSceneDisplayFactory.cs`(민형님 영역, 연결 줄) | 지형 표시 생성에 `CombatTileCollapseSettings.From(presentation)`, 필드 창구 이름 있는 인자 넷(옛 셋 + `appearanceBlocksStart`), using 한 줄 — 민형님 공유는 TR-d |

- 흐름: 붕괴 사건 → `ShowChanged` → `TryReplace(…, leave)`가 약한 지반 외형을 연결에서 빼고 `Leave`로 넘김 → 떨리는 동안 다음 사건(그 칸 유닛 추락)은 시작을 기다림 → 떨림이 끝나면 외형이 가라앉고 유닛 추락이 함께 진행 → 다 가라앉으면 외형을 끄고 풀에 반환 → 결과 끝 반영은 그 뒤.
- 흔들고 가라앉는 축은 `ShowChanged` 순간의 필드 축(Mapper)에서 한 번 구한다. 외형 부모(타일 루트·지형 루트)와 상관없이 월드 위치로 움직인다.
- 계획과 다른 점: 이 Phase 자체가 TR-c 확인 뒤 사용자 추가 요청이다(설계 TR8). 거리 단위는 설계 초안의 "필드 단위"가 아니라 유닛 추락과 같은 월드 단위로 했다(설계 TR8에 고침).
- 같은 작업 트리에서 다른 세션이 `CombatSceneDisplay`·Factory·`FieldView`·`CombatDisplayServices`의 점선 서비스 인자를 걷고 있다(이번 작업 아님). 내 줄과 겹치지 않고 그대로 들어갔다. 사용자 쪽 변경(Test 더미 타일 프리팹 둘 삭제, 약한 지반 InGame·외형 에셋)도 작업 트리에 있다.
- **Uc2(사용자):**
  1. Test에서 약한 지반을 무너뜨리면 그 칸 외형이 0.35초 떨리고, 그동안 그 칸 유닛은 서 있다.
  2. 떨림이 끝나면 외형이 아래로 가라앉고(점점 빨라짐), 유닛 추락(가라앉기·작아지기)이 함께 진행된다.
  3. 다 가라앉으면 외형이 사라지고 구멍이 남는다. 결과 끝 다시 그리기는 그 뒤에 온다.
  4. 결과를 건너뛰면(또는 전투를 나가면) 무너지는 외형이 바로 사라지고, 타일이 다시 켜진다(전투를 나간 경우).
  5. 토네이도 생성·이동은 지금과 같다.
  6. 설정 표의 다섯 값을 바꾸면 다음 전투 진입부터 반영된다.
- **Uc2 문제 없음(2026-10-29 사용자).**

### 6.5 TR-d 문서·공유·묶음 끝 검토 (2026-10-29, 사용자 "Uc2 문제 없음, TR-d", 코드 변경 없음)

| Task | 한 것 |
|---|---|
| Td.1 | [PlacedPrefab 지형 안내](../viewDocs/placed-prefab-terrain-guide.md) — 외형 표 줄(Visual Mode 두 값·Surface Height), "PlacedPrefab" 문구, 바닥 지형 단락을 §2.2로 넘김, 새 §2.2 "표시 방식 — 타일 위 / 바닥 타일 교체"(방식 표·토네이도는 OnTile·바닥 교체 프리팹 만드는 법·타일 끄는 조건·그리드 무관·붕괴 연출 값·지금 값·한계 셋) |
| Td.2 | [폴더 배치 안내](../viewDocs/combat-view-folder-guide.md) — 머리 갱신 줄, 폴더 나무에 `Presentation/Environment/`, 2단 표 `Map/`(`FieldTileReplacement`)·`Players/`(`CombatFieldAppearancePlayer`)·새 `Environment/` 줄(토네이도 연출이 원래 있었고 표에 빠져 있었음), §2 칸 위 높이 규칙에 바탕 바꾸기(`Replace`·`Raise`) |
| Td.3 | 공유(모두 미전달): 민형님 [Factory 공유](../handoffs/2026-10-15-combat-scene-display-factory-framework-share.md) §39(이동 경로 화살표 세션이 §38을 먼저 써서 39), 호진님 [설정 표 메모](../handoffs/2026-10-18-combat-presentation-settings-table-hojin-share.md) §11(설정 묶음·호진님 토네이도 재생기·창구 이름·지형 외형 방식·토네이도 OnTile 검사), [SO 모드 3D 사본 공유](../handoffs/2026-10-27-combat-stage-editor-so-mode-view-share.md) §6(도구 높이 사본 `RaiseSurfaces`가 음수·프리팹 없는 바닥 교체를 못 그림) |
| Td.4 | 설계 §8 구현 메모 |

- 공유 문서 셋은 CRLF 그대로 끝에 붙였다(임시 파일 `unix2dos` 뒤 덧붙임).
- Td.5 묶음 끝 검토(새 에이전트 하나, 읽기 전용): **약 9.46**(SSOT 8.5·SOLID 9.25·FW 10·독립성 9.5·캡슐화 9.75·C#/Unity 9.75·If/Tick 9.5, 높음 0·중간 2·낮음 9), 권장 반영 뒤 예상 약 9.9(M1·M2만이면 약 9.68). 설계 예상 8.8보다 높다. 중간 이상과 L8은 코드로 다시 확인했다(맞음).
  - **M1(결함, 잠복)** 바닥 교체 외형의 타일 칸 회전(`PlaceOnTile`의 `YawOf(look.Rotation)`)이 타일 루트 축 기준인데, 외형 회전(이웃 모양·스테이지 회전)은 필드 기준 사실이다. 타일 맞춤은 배경을 가장 가까운 90° 단위로만 맞추므로(`CombatMapTileAlignment.YawCorrection`) 타일 루트가 필드와 90° 단위로 어긋나면 회전이 틀린 쪽을 본다. 지금 약한 지반·낭떠러지는 모양 없음·R0이라 안 드러남. 권장: 회전만 필드 기준(`Inverse(부모 회전) × RotationToWorld(외형 회전) × 프리팹 회전`, P12 일부 바뀜) 또는 "바닥 교체는 이웃 모양 불가"(P5 바뀜).
  - **M2(구조)** `FieldTerrainPresenter`(473 → 613줄)에 타일 자리 배치·무너짐 수명이 더해짐. 권장: 무너짐 목록·진행·떨림 집계·끝내기를 `Presentation/Environment/`의 작은 집합으로 뺌.
  - L1 `ShowAt`(토네이도 재배치)이 바닥 교체 외형을 받으면 타일 루트 아래인 채 엉뚱한 자리로 감(지금 안 닿음), `LocalPosition` 주석 부정확. L2 방식 검사가 생성자·표 두 곳(P8의 결과) → 프리팹 필수도 생성자로. L3 문서 불일치(떨림 세기 에셋 0.1 — 이번에 문서 고침, `localOffset` 툴팁, `TryReplace` 요약). L4 바닥 교체로 바탕이 바뀌면 토네이도가 축소·재확대 연출을 다시 함(지금 높이 0이라 안 드러남). L5 필드 창구 위치 인자 13개(이번에도 천명님 시험 한 줄이 따라 바뀜, 기존 꼴). L6 지형 인스턴스가 배경 계층에 들어감(P11의 결과, 해제 순서는 안전). L7 떨림 → 가라앉기 넘어갈 때 최대 떨림 세기만큼 한 프레임 튐. L8 높이 버전이 칸 높이만 바뀌어도 올라 격자 전체를 다시 그림(버전을 쓰는 곳은 격자 그리기뿐 — 확인). L9 `Leave`의 `_mapper == null` 갈래는 닿지 않음.
  - 확인 필요(감점 없음): 한 명령에서 약한 지반 여럿이 무너지면 떨림이 차례로 이어지고 다른 사건도 기다림. 낭떠러지에 프리팹을 다시 넣으면 떨리는 옛 외형과 새 외형이 같은 자리에 겹침.
  - 반영은 §6.6에 적었다(사용자 "권장안대로", L5만 그대로).
- 검토 중 확인한 것: 천명님 `PullAndMarkPresentationTests.cs:427` 위치 인자 줄에 `() => false` 하나가 작업 트리에서 이미 더해져 있었다(필드 창구 칸 하나 늘어난 직접 결과, 이 세션이 고친 것 아님). Core 전달 문서 §50에 적었다(미전달).
- 문서를 쓰며 찾은 것(보고만, 지우지 않음): 약한 지반 외형이 사용자 새 프리팹 `Prefabs/Stage/MapProp/Test/WeekGround.prefab`으로 바뀌어, 옛 `Prefabs/Default/Environment/Terrain_WeakGround.prefab`(+ `WeakGround/WeakGroundSurface.mat`·`WeakGroundCrack.png`)도 참조가 없다. Test 타일 프리팹은 사용자가 `dummy_background_tile*.prefab` → `Tile1`·`Tile2.prefab`으로 바꿔 작업 트리에 삭제 + 새 파일로 보인다(함께 커밋해야 타일 세트 참조가 산다).

### 6.6 검토 권장 반영 + 유닛 추락 연출 (2026-10-29, 사용자 "커밋 했고 지금 권장안대로 반영하는데, 지금 유닛 추락 시 떨어지는 연출도 추가해주샘")

- 기준 판은 사용자 커밋 `73ed12be`(TR-a~TR-d)다. 컴파일은 확인하지 않았다. 새 시험은 없고, 시험도 바꾸지 않았다(바뀐 생성자·함수를 부르는 시험이 없음 — 확인).

| 항목 | 한 것 |
|---|---|
| M1 | 바닥 교체 외형의 타일 칸 회전을 필드 기준으로 바꿨다. `PlaceOnTile`은 `Inverse(부모 회전) × RotationToWorld(외형 회전) × 프리팹 회전`이다. 위치·배율은 타일 루트 기준 그대로다(P12에서 회전만 바뀜). `FieldGridMapper.YawOf`를 지우고 90° 식은 `RotationToWorld` 한 곳에 둔다. |
| M2 | 새 `Presentation/Environment/CombatTileCollapseSet.cs`(+ `.meta`)를 만들었다. 무너짐 목록·진행·떨림 집계·끝내기를 맡는다. `FieldTerrainPresenter`는 집합 하나를 갖고 묻기만 한다(안쪽 `Collapse` 클래스·`FinishCollapses`·설정 필드 삭제, 613 → 586줄). `CombatTileCollapse.Finish`도 지웠다(끝 알림은 집합이 부름). |
| L1 | `ShowAt`이 바닥 교체 외형을 받으면 예외를 낸다(토네이도 재배치는 타일 위 외형만 받음). `LocalPosition` 주석을 고쳤다. |
| L2 | "타일 위는 프리팹 필수" 검사를 외형 Data 생성자로 옮겼다. 방식 규칙은 이제 한 곳이고, 표 검사는 지웠다. |
| L3 | `localOffset` 툴팁에 타일 칸에서 어느 축이 쓰이는지 적었다. `TryReplace` 요약에 `leave`를 적었다. |
| L4 | 토네이도 바탕 높이를 타일 바탕(새 `FieldSurfaceHeights.TileBaseHeightAt`)에서 읽는다. 바닥 교체가 토네이도 축소·재확대를 다시 일으키지 않는다. |
| L6 | `StageMapTiles` 머리 주석에 두 가지를 적었다. 교체 외형이 타일 루트 아래 있다는 것과 해제 순서다. |
| L7 | 떨림 세기를 떨림 시간 끝에 0이 되도록 줄인다. 가라앉기로 넘어갈 때 튐이 없다. |
| L8 | 높이 표 `Version`은 덧그리기(지형) 높이만 센다. `Replace`는 버전을 올리지 않고, 다시 채우기 비교도 지형만 본다(`_previous` 삭제). |
| L9 | `Leave`의 `_mapper == null`은 조용히 돌아가지 않고 계약 위반 예외를 낸다. |
| L5 | 그대로 둔다(필드 창구 위치 인자, 기존 꼴). |

**유닛 추락 연출(설계 TR2의 "이 뒤 예정", 사용자 고름 "크게 떨어지며 끝에만 작아짐"):**

- 설정 표 "낭떠러지 추락 연출" 묶음을 고쳤다.
  - 기본값: `fallSeconds` 0.5 → 0.6, `fallDepth` 0.6 → 3.
  - 새 칸 `fallShrinkStart`(0.5): 전체 시간 가운데 작아지기 시작하는 지점이다. 0 이상 1 미만이고, 조건 함수는 `ValidateFallShrinkStart`다.
  - `fallEndScale` 0.05는 그대로다. 기본 에셋도 같은 값으로 바꿨다.
- 값 객체 `CombatFallMotionSettings`에 `ShrinkStart`·`ShrinkDelay`·`ShrinkSeconds`를 넣었다. `Instant`는 (0, 0, 1, 0)이다.
- 조립 `ComposeFall`:
  - 가라앉기는 전체 시간·깊이로 그대로다(가속).
  - 크기는 `ShrinkDelay` 뒤 시작해, 남은 시간 동안 1 → 끝 크기로 줄어든다. 둘은 같은 순간 끝난다.
  - 사망 소리·사망 사실 반영 시점은 그대로다.
  - `fallShrinkStart`가 0이면 옛 모양(처음부터 작아짐)이다.
- 칸 낭떠러지(프리팹 비움 = 빈 구멍)와 약한 지반 붕괴(가라앉기와 유닛 추락이 함께, Q-CL1 A) 둘 다 이 조립을 쓴다.
- 공유: 호진님 설정 표 메모 §12(미전달).

**Unity 확인(Ud):**

1. 약한 지반 붕괴: 떨림이 점점 약해지다 튐 없이 가라앉기로 이어진다. 그 칸 유닛은 함께 떨어진다.
2. 넉백·끌어오기·붕괴로 떨어지는 유닛은 0.6초 동안 3만큼 아래로 빨라지며 떨어진다. 절반이 지나서부터 작아져 끝에 사라지고, 사망 소리가 난다.
3. 토네이도 생성·이동·축소/확대가 지금과 같다. 근처 약한 지반이 무너져도 토네이도가 다시 줄었다 커지지 않는다.
4. 바닥 교체 외형의 방향·자리가 지금과 같다(약한 지반·낭떠러지는 R0).
5. 결과를 건너뛰거나 전투를 나가면 무너지는 외형이 바로 사라지고 타일이 켜진다.
6. 설정 표의 추락 값 넷·붕괴 값 다섯을 바꾸면 다음 전투 진입부터 반영된다.
