# 전투 스테이지 편집 SO 모드 — 기믹 시작·초안 보호·UX 인계와 View 경계 (2026-11-09 갱신, 미전달)

- 작성자: 호진(이호진)
- 최신 상태(2026-11-09): **UXR-d 문서·정적 대조 반영, 컴파일 미확인·최신 Unity 확인/Phase 인수 대기·커밋 전·공유 미전달**입니다. 현행 화면과 HAT/GP/DP/UXR 인계는 §15, 기능별 확인은 [계획 §20.9](../plans/2026-10-27-combat-stage-editor-so-mode-plan-Hojin.md#209-uxr-d-문서기능-대조-기록-2026-11-09)를 따릅니다. D-01 본부 삭제 안내는 후속 승인으로 수정 반영했습니다(계획 §20.10, 컴파일·Unity 미확인). 아래 날짜별 기록의 자동 시작/구형 초안 재읽기/형식 번호는 당시 이력입니다.
- 터널 폭발 후속(2026-11-02): RTE-a 스테이지별 선택과 전투 표시 연결 반영(§14), 컴파일·Unity 미확인, 고지대 B1 보류. 공유 미전달입니다.
- 모양 편집 후속(2026-11-02): GSE-a 코드 반영(§13), 컴파일·Unity 미확인, 고지대 B1 보류. 실제 전투의 위치/연출/규칙 변경은 없으며 공유 미전달입니다.
- 최신 상태(2026-11-02): UX-c 및 HBE-a 하붑 설정 코드·안내 문서 반영, 컴파일·Unity 미확인, 고지대 B1 보류. 공유 미전달. 공개 표시 후속은 §11, 하붑은 §12, 타일/토네이도 식의 최신 사본은 §10과 토네이도 절을 우선합니다.

전투 스테이지 편집 도구 SO 모드의 **3D 보기**(SO-i)는 게임 전투 View의 배치 식을 **도구 안에 사본으로** 갖고 있습니다(추가 설계 Q-AD1 A: 게임 코드에 `InternalsVisibleTo`를 더하지 않는 대신 사본). 도구는 에디터 전용이라 빌드에는 들어가지 않습니다.

**아래 게임 식을 바꾸면 도구 사본도 같이 맞춰 주세요.** 맞추지 않으면 도구 3D에서 유닛·지형이 게임과 다른 자리·방향·크기로 보입니다. 게임 동작에는 영향이 없습니다.

## 1. 사본 목록

| 게임 원본 (`ViewSystem/Combat/…`) | 도구 사본 (`Tool/Editor/…`) | 식 |
|---|---|---|
| `FieldGridMapper.CellCenterToLocal` | `CombatStageSoSpace.CellCenter` | 필드 가운데가 원점, `((x+.5)·칸 가로 − 전체 가로/2, 0, (y+.5)·칸 세로 − 전체 세로/2)` |
| `FieldGridMapper.RotationToWorld` | `CombatStageSoSpace.Facing` | 회전 값 × 90°(Y축), R0 = 필드 +Z |
| `FieldGridMapper.WorldToGrid` | `CombatStageSoSpace.CellAt`·`ClampedCellAt` | 필드 로컬 → 칸(바닥 높이 0 평면에서 고름) |
| `FieldUnitPresenter.CenterOf` + `FieldGridMapper.SurfaceHeightOf` | `CombatStageSoSpace.StandingPoint` | 점유 칸 중심 평균 + 점유 칸 표면 높이 중 최댓값(첫 칸에서 시작, 2026-10-29) |
| `FieldUnitPresenter.EnemyFacing`·`ModelRotation`·`CreateView` | `CombatStageSoSpace.EnemyFacing`, `CombatStageSoPreview`(유닛) | 적은 반 바퀴, 배율 = 프리팹 배율 × `fieldUnitScale` |
| `FieldTerrainPresenter.FillSurface` + `FieldSurfaceHeights.Replace`·`Raise`·`BaseHeightAt`·`OverlayHeightAt` | `CombatStageSoSpace.RaiseSurfaces`·`BaseAt`·`OverlayAt`·`SurfaceAt` | 바탕(타일 윗면, 바닥 교체 칸은 외형 높이·한 칸 여럿이면 낮은 값·프리팹 없어도) + 타일 위 지형 높이(최댓값). 덧그리기 `Corners`는 지형 높이만(2026-10-29 맞춤, §5·§6) |
| `FieldTerrainPresenter.LocalPosition`·`FieldObjectPresenter.LocalPosition` | `CombatStageSoPreview.RebuildPlacements`(지형·오브젝트) | 타일 위 지형 = 칸 바탕 + `localOffset`, 오브젝트 = 칸 표면 + `localOffset`. 외형 회전 × 프리팹 회전, 프리팹 배율 × 외형 `localScale` |
| `FieldTerrainPresenter.PlaceOnTile`·`PlaceInField` | `CombatStageSoPreview.PlaceReplacement` | 바닥 교체 외형: 타일이 있으면 타일 부모 아래 타일 위치 + 오프셋, 회전 = 부모 회전의 역 × 필드 회전 × 프리팹 회전 / 없으면 칸 가운데 바닥면 + 오프셋. 프리팹 없으면 빈 구멍(2026-10-29) |
| `FieldTileReplacement.Apply`·`Restore` | `CombatStageSoPreview.ApplyHiddenTiles` | 자기가 숨긴 타일만 다시 켬, 원래 꺼진 타일은 건드리지 않음, 나눠 쓰는 타일은 교체 지형이 남는 동안 숨김(2026-10-29) |
| `StageViewPropPresenter.Show`·`StageViewPropData.GameObject`·`Map` | `CombatStageSoPreview.RebuildMap`, `CombatStageSoBackground` | 배경 방식대로 맵 프리팹/맵 SO 프리팹을 위치·회전·배율 보정으로 놓음(2026-10-29) |
| `StageMapTiles.Build` + `StageViewPropPresenter.ShowTiles` | `CombatStageSoPreview.BuildTiles` | MapTileRoot(배경 자식, `offset`)·칸마다 타일 → 남은 각만큼 배경 회전 → 기준 타일에 맞춰 배경 이동 → 필드 칸 → 타일 표(2026-10-29). 칸 회전 = `Euler(0, r × 90, 0)` × 프리팹 회전(2026-10-29 칸별 회전, §8) |
| `CombatMapTileAlignment.RequireLevel`·`YawCorrection`·`AnchorShift`·`CellMultiple` | `CombatStageSoTileFit`(`Of`·`YawCorrection`·`AnchorShift`) | 맞춤 검사(기울기·정수배·기준 타일 범위)와 회전·이동. 화면 ↔ 배열 방향은 회전·배율 부호로 `columnsAlongX`와 같은 비교(2026-10-29) |
| `CombatMapTileSurface.Build` | `CombatStageSoTileFit.TryTileOfCell`, `CombatStageSoTileScene.BaseHeightAt` | 칸 가운데가 놓인 타일 = 기준 타일 + 칸 번호 ÷ 타일 한 장의 칸 수(맞춤이 성립할 때 같음) (2026-10-29) |
| `FieldCellSpacing.FromGridOutline` | `CombatStageSoTileFit.Of`(GridOutline 전제) | GridOutline이 아니면 타일 맞춤을 계산하지 않고 "진입 때 멈춤"으로 알림(2026-10-29) |
| 칸 크기(필드 보기 `GridOutline`의 `outlineCellSpacing`) | `CombatStageSoAssets.ReadFieldView` | `CellPrefab` 모드는 1×1로 근사하고 안내를 띄움 |
| `StageViewPropPresenter.RockfallTunnelObjectName`·`FindRockfallTunnel` | `CombatStageSoAssets.RockfallTunnelObjectName`·`RockfallTunnelCount` | 오브젝트 이름 `RockfallTunnelBoom`, 비활성 포함 이름 일치. 이름을 바꾸면 도구 사본도 함께 바꿔야 경고가 맞음(2026-11-02) |

`CombatStageSoSpace`는 `CombatStageSoVisuals.cs`, `CombatStageSoTileFit`·`CombatStageSoTileScene`·`CombatStageSoBackground`는 `CombatStageSoTiles.cs` 파일 안에 있습니다.

**2026-10-29 도구가 §5·§6을 맞췄습니다(SO 모드 UT-a~UT-c, [SO 모드 설계 §12](../specs/2026-10-27-combat-stage-editor-so-mode-design-Hojin.md#12-병합-최신화-배경-방식바닥-타일-ut1ut10)).** 위 표의 2026-10-29 줄이 새로 생긴 사본입니다. 당시 남은 차이는 공개 대상·토네이도·고지대였으며, 공개 대상·토네이도는 FU-b에서 코드 반영했습니다(§9). 고지대 B1은 보류입니다.

## 2. 사본이 아닌 것 (게임과 다를 수 있음)

- 맵 SO는 `MapAssembler.Instantiate`를 쓰지 않고 프리팹만 놓습니다. `MapAssembler`가 에디터 전역 `LightmapSettings`를 바꾸기 때문입니다. 그래서 조명은 게임과 다릅니다.
- 레일·기차 외형, 강조 층·아웃라인, HUD는 그리지 않습니다. 칸 위 표시(아군 구역·범위·상자·고스트)는 도구가 화면 좌표로 덧그립니다.
- 아군 구역 색은 도구가 스테이지 칸 목록(비면 필드 아군 줄 수)을 Core `PlayerDeploymentZone`으로 해석해 칠합니다. 게임 View `FieldHighlightState.SetStaticZones`도 같은 Core 값으로 칠해 결과가 같습니다(§7, 2026-10-29 병합 뒤).

## 3. 도구가 이름으로 읽는 View DB 필드

도구는 View DB SO를 SerializedObject로 **필드 이름**으로 읽습니다(필드 이름은 `CombatStageSoAssets.cs` 상수에만 있음). 아래 필드 이름이 바뀌면 도구 상수도 바꿔야 합니다.

- 유닛 외형: `unitVisualData.key`·`.displayName`·`.prefab`
- 지형·오브젝트 외형: `kind`·`displayName`·`prefab`·`localOffset`·`localScale`(지형은 `surfaceHeight`, `visualMode`(열거 **이름** `ReplaceTile`이면 바닥 교체, 2026-10-29)와 이웃 모양 칸 `oneSidePrefab`·`twoSidePrefab`·`twoSideOppositePrefab`·`threeSidePrefab`·`fourSidePrefab`도)
- 필드 보기: `stageFieldViewData.fieldDisplayMode`·`.outlineCellSpacing`·`.fieldUnitScale`
- View Stage Data·카메라 설정 참조(SO-a~SO-d)
- View Stage Data 배경(2026-10-29, 읽고 씀): `stageViewPropData.mode`(열거 **이름** `Prefab`·`MapDefinition`·`PrefabWithTiles`로 뜻을 봄), `.mapTiles`의 `tileSet`·`columns`·`rows`·`tileSize`·`offset`·`anchorTile`·`tiles`(종류는 열거 값)·`rotations`(2026-10-29 칸별 회전, `Rotation4` 값, §8)
- 타일 세트 `CombatMapTileSetSO`(2026-10-29, 읽기만, 형식 이름으로 찾음): `tiles`의 `kind`·`prefab`·`surfaceHeight`. 종류 열거 `CombatMapTileKind`의 이름이 도구에 그대로 보입니다.

## 4. 규칙 사본: 이웃 모양 지형의 외형 회전 (병합 대응 A1, 2026-10-27 추가)

고지대처럼 이웃 모양을 쓰는 지형에 스테이지 외형 회전 항목이 있으면(R0이어도) 전투 화면이 예외로 멈춥니다. 도구는 그런 항목을 만들지 못하게 하고, 남은 항목은 저장 검사에서 막습니다([SO 모드 설계 §10](../specs/2026-10-27-combat-stage-editor-so-mode-design-Hojin.md#10-병합-대응-a1a2-mg1mg9) MG1~MG4). 아래 게임 규칙이 바뀌면 도구 사본도 맞춰 주세요. 맞추지 않으면 도구가 덜 막거나 더 막을 뿐, 게임 동작은 그대로입니다.

| 게임 원본 | 도구 사본 (`Tool/Editor/…`) | 규칙 |
|---|---|---|
| `TerrainEdgeShapePrefabs.HasAny` → `TerrainVisualData.UsesEdgeShapes` | `CombatStageSoAssets.EdgeShapeTerrainKinds`·`UsesEdgeShapes` | 지형 외형의 모양 칸 다섯 중 하나라도 차 있으면 이웃 모양 지형 |
| `CombatTerrainLook.Resolve`(`TryGetTerrainRotation`이 참이면 예외) | `CombatStageSoValidation.CheckPairs`, 패널 `DrawSoViewRotation`, 정의 변경 `DrawSoMarker` | 이웃 모양 지형의 원본에는 외형 회전 항목을 두지 않음 |

- 도구는 외형 표를 형식으로 찾은 에셋 중 종류별 이름순 첫 것으로 읽습니다. 게임은 등록된 외형 표를 씁니다. 지금 지형 외형은 종류마다 하나라 결과가 같습니다.
- 고지대 이웃 모양(끝·모서리)은 보류입니다. 토네이도 반 칸·높이는 FU-b 사본에 반영했습니다(§9).

## 5. 게임 식이 바뀜: 바닥 타일 맞춤·높이 (2026-10-28, 전투 View → 호진님, 미전달)

> **2026-10-29 해소(도구 쪽):** 아래 §5.2 표의 사본은 모두 맞췄고(§1 표의 2026-10-29 줄), §5.3의 `mode`·`mapTiles`는 도구가 읽고 쓰고 패널·'타일' 보기·2D 띠로 보입니다. 패널 문구 "맵 SO가 있으면…"은 지웠습니다. 순서는 §5.4 권장대로 ① 방식 → ② 높이 → ③ 타일·바탕 높이(사본)였습니다. 덧그리기는 지형 높이만 씁니다.

전투 맵 바닥 타일 작업으로 위 §1의 게임 식 몇 개가 바뀌었습니다. 설계는 [타일 맞춤](../specs/2026-10-28-combat-map-tile-alignment-design.md)(TA1~TA5)과 [타일 높이](../specs/2026-10-28-combat-map-tile-height-design.md)(TH1~TH6), 기록은 [타일 높이 계획](../plans/2026-10-28-combat-map-tile-height-plan.md) §8입니다. **도구 사본은 고치지 않았습니다(다른 작업자 영역, 계획 P14).** 맞출지·언제 맞출지는 도구 담당이 정합니다. 맞추지 않아도 게임 동작은 그대로이고, 도구 3D만 게임과 다르게 보입니다.

### 5.1 게임에서 바뀐 것

- **배경 방식:** 프랍에 방식 열거 `StageViewPropMode`(`mode`)가 생겼습니다 — `Prefab = 0`(기본, `gameObject`), `MapDefinition = 1`(`map`의 프리팹), `PrefabWithTiles = 2`(`gameObject` + 바닥 타일 `mapTiles`). 게임은 방식만 보고 고릅니다. 방식이 아닌 칸에 남은 값은 읽지 않습니다.
- **타일 방식:** 배경 인스턴스 아래 `MapTileRoot`를 `mapTiles.offset` 자리에 만들어 칸 단위 타일을 놓고, 배경만 90° 단위 남은 각만큼 돌리고 옮겨 `anchorTile` 타일이 게임 (0,0) 칸에 오게 합니다(`CombatMapTileAlignment`). 지금 이 방식은 Test 스테이지 하나입니다.
- **칸 높이 = 타일 윗면 + 지형:** 필드 높이 표 `FieldSurfaceHeights`에 바탕 층이 생겼습니다. 바탕은 그 칸 타일의 윗면 높이(타일 세트 `surfaceHeight`, 필드 단위, **음수 = 바닥보다 들어간 타일**)이고, 지형 외형 높이는 바탕 위에 더합니다(`Raise` = max(지금, 바탕 + 지형 높이)). 타일 방식이 아니면 바탕은 0이라 옛 값과 같습니다. 2026-10-29부터 바닥 교체 지형(약한 지반·낭떠러지)은 바탕 자체를 그 높이로 바꿉니다(§6).
- **놓는 자리:** 타일 위 지형 외형은 칸 바탕(타일 윗면, 바닥 교체 칸은 그 높이 — §6, `CellBaseToLocal`), 오브젝트·가시밭은 칸 표면(바탕 + 지형, `CellSurfaceToLocal`)입니다. 오브젝트는 **타일이 없어도** 고지대 칸에서는 블록 윗면에 놓입니다(전에는 바닥면).
- **유닛 높이:** `FieldGridMapper.SurfaceHeightOf`가 0이 아니라 첫 점유 칸 높이에서 시작해 최댓값을 고릅니다. 들어간 칸만 점유한 유닛이 바닥면에 뜨지 않게 하려는 고침입니다.
- **격자(GridOutline)·호버·포인터:** 칸 테두리·채우기·경로선·호버·데칼과 포인터는 타일 높이를 보지 않고 지형이 올린 높이만 따릅니다(`OverlayHeightAt` = 지형 높이). 유닛·고스트·지형·오브젝트는 타일 높이를 따릅니다.

### 5.2 어긋나는 도구 사본

| 게임 원본 | 도구 사본 (`Tool/Editor/…`) | 도구가 게임과 다르게 보이는 것 |
|---|---|---|
| `FieldSurfaceHeights`(바탕 + `Raise`), `CombatMapTileSurface` | `CombatStageSoSpace.RaiseSurfaces`·`SurfaceAt` | 타일 높이를 모름. 타일 스테이지에서 칸 높이가 지형 높이만으로(타일 칸은 0으로) 보임 |
| `FieldGridMapper.SurfaceHeightOf`(→ `FieldUnitPresenter.CenterOf`) | `CombatStageSoSpace.StandingPoint` | 시작 값이 0이라 들어간 칸 위 유닛이 바닥면에 뜸(음수 높이가 있을 때만) |
| `FieldTerrainPresenter.LocalPosition`(칸 바탕)·`FieldObjectPresenter.LocalPosition`(칸 표면) | `CombatStageSoPreview`(지형·오브젝트) | 둘 다 칸 중심 높이 0. 고지대 칸 위 오브젝트가 블록 속에 묻혀 보이고, 타일 스테이지의 지형이 타일 윗면을 따르지 않음 |
| `StageViewPropPresenter.Show`·`ShowTiles` | `CombatStageSoPreview`(맵) | 방식을 보지 않고 `map`이 있으면 맵 SO, 없으면 `gameObject`를 놓음. 타일·`MapTileRoot`·배경 회전 보정을 그리지 않음 |
| `FieldSurfaceHeights.OverlayHeightAt`(격자·호버·포인터 = 지형 높이만) | `CombatStageSoSpace.Corners`(한 칸 덧그리기 높이)·`CellAt`(바닥 평면) | 다르지 않음 — 도구도 지형 높이만 쓰므로 같게 보임(도구에 타일 높이를 넣게 되면 덧그리기는 지형 높이만 쓰게 둘 것) |

### 5.3 §3 이름 읽기에 빠진 것

- 도구는 `stageViewPropData.gameObject`·`.map`을 이름으로 읽고 쓰지만, 새 칸 `mode`·`mapTiles`는 읽지 않습니다. 그래서 방식을 표시하지 않고, 타일 방식 스테이지를 도구로 저장해도 이 두 칸은 그대로 남습니다(지워지지는 않음).
- 패널 안내 문구 "맵 SO가 있으면 맵 SO의 프리팹을 씁니다."(`CombatLayoutWindow.SoMode.cs` 맵 SO 줄)는 이제 게임 규칙과 다릅니다. 게임은 `MapDefinition` 방식일 때만 맵 SO를 씁니다.
- 저장 검사(복제본 `GetData()`)는 게임과 같은 방식별 검사를 거칩니다(`StageViewPropData.Validate()`: `Prefab`·`PrefabWithTiles`는 `gameObject` 필수, `MapDefinition`은 `map` 필수). 도구가 방식을 모르는 채 프리팹 칸을 비우면 그 검사에서 막힙니다.

### 5.4 권장 (도구 담당이 정함)

- 사본을 맞춘다면 순서는 ① 방식 읽기·표시(맵 SO / 프리팹 고르기를 게임과 같게) → ② 오브젝트 높이를 칸 표면으로(타일 없이도 고지대에서 다름) → ③ 타일·바탕 높이(맞춤 계산 `CombatMapTileAlignment`·높이 표 `CombatMapTileSurface`를 사본으로 둘지, 타일 방식 스테이지는 3D에서 타일 없이 보인다고 안내만 할지) 순이 영향이 큽니다.
- 이 메모를 쓰며 바꾼 호진님 줄은 없습니다.

## 6. 게임 식이 바뀜: 지형 외형 표시 방식·바닥 타일 교체 (2026-10-29, 전투 View → 호진님, 미전달)

> **2026-10-29 해소(도구 쪽):** 도구가 `visualMode`를 읽어 바닥 교체는 "바탕 = 값"(프리팹 없어도, 음수 그대로), 타일 위는 올림으로 둡니다. 바닥 교체 외형은 타일 자리에 놓고 그 타일을 숨기며(자기가 숨긴 것만 복원), 프리팹 없는 낭떠러지는 빈 구멍입니다. 당시 차이였던 공개 대상과 토네이도 바탕은 FU-b에서 코드 반영했습니다(§9, Unity 미확인).

[타일 교체 설계](../specs/2026-10-29-combat-terrain-tile-replace-design.md) TR1~TR8, [지형 외형 안내](../viewDocs/placed-prefab-terrain-guide.md) §2.2.

### 6.1 게임에서 바뀐 것

- 지형 외형 SO의 Visual Mode는 `OnTile`(0, 옛 PlacedPrefab: 효과·벽·고지대)·`ReplaceTile`(1, 옛 GroundShape: 약한 지반·낭떠러지) 두 값입니다.
- `surfaceHeight` 뜻: 타일 위는 바탕(바닥 타일 윗면) 위로 올리는 높이(0 이상), 바닥 교체는 그 칸의 새 바탕 높이(필드 바닥면 기준, 음수 가능)입니다. 높이 표 `FieldSurfaceHeights`가 바닥 교체는 `Replace`(한 칸에 여럿이면 낮은 값), 타일 위는 `Raise`(가장 높은 값)로 받습니다(`FieldTerrainPresenter.FillSurface`).
- 바닥 교체 외형은 프리팹이 없어도 바탕을 바꿉니다(낭떠러지는 지금 프리팹이 없음 = 빈 구멍).
- 놓는 자리: 타일 위 외형은 칸의 이번 바탕(타일 윗면, 바닥 교체 칸은 그 높이, `CellBaseToLocal`) 위입니다. 바닥 교체 외형은 타일이 있는 칸이면 그 칸 타일을 끄고 타일 자리에, 타일이 없는 칸이면 칸 가운데 바닥면(높이 0) + 외형 오프셋입니다.
- 토네이도 외형만 바닥 교체를 보지 않고, 덮는 칸들의 타일 바탕(`FieldSurfaceHeights.TileBaseHeightAt`) 중 가장 높은 값에 놓습니다(바탕이 결과마다 바뀌어 자리 옮김 연출을 다시 하지 않게).
- 그리드(덧그리기)는 지형이 올린 높이만 봅니다. 바닥 교체 높이는 그리드에 들어가지 않습니다.
- 지금 에셋 값: 약한 지반(프리팹 있음)·낭떠러지(프리팹 없음)는 바닥 교체·높이 0, 고지대는 타일 위·0.45입니다.

### 6.2 어긋나는 도구 사본

| 게임 | 도구 사본 | 다른 점 |
|---|---|---|
| `FieldTerrainPresenter.FillSurface` + `FieldSurfaceHeights.Replace`·`Raise`(방식별) | `CombatStageSoSpace.RaiseSurfaces`(모두 올림, 프리팹 없음·0 이하 건너뜀, 가장 높은 값) | 바닥 교체의 음수 높이를 0으로 그림(게임은 그만큼 내려감), 프리팹 없는 바닥 교체(낭떠러지)의 높이를 무시함. 양수 높이는 도구에 타일이 없어(§5) 결과가 같음. 지금 에셋은 바닥 교체 높이가 모두 0이라 타일 없는 스테이지에서는 차이가 보이지 않음 |
| 바닥 교체 외형은 타일 칸에서 타일 자리 | `CombatStageSoPreview` 지형 외형 = 칸 가운데 + 오프셋 | 도구는 타일을 그리지 않으므로(§5) 게임의 "타일 없는 칸"과 같음. 타일 방식 스테이지에서만 다르게 보임 |

- 도구는 `visualMode`를 읽지 않습니다. 사본을 맞춘다면 방식을 읽어 바닥 교체는 "바탕 = 값"으로, 타일 위는 지금처럼 올림으로 두면 됩니다.
- 이 메모를 쓰며 바꾼 호진님 줄은 없습니다.

## 7. 아군 배치 구역 칸 (2026-10-28 추가, 2026-10-29 해소, 병합 전 §5·§6)

천명님 `5a1bfbe1f`로 적 배치 구역이 없어지고, 아군 배치 구역을 스테이지 칸 목록(`CombatStageData.playerDeploymentCells`, 비면 필드 아군 줄 수)으로 정하게 되었습니다. SO 모드 도구는 이 칸 목록을 칠하는 '아군 구역' 도구를 더했습니다([SO 모드 설계 §11](../specs/2026-10-27-combat-stage-editor-so-mode-design-Hojin.md#11-아군-배치-구역-칸-az1az9)).

- **지금은 게임 화면과 도구가 같은 Core 값으로 칠합니다.** 게임은 `CombatSceneDisplay`가 `state.PlayerZone`을 넘기고 `FieldHighlightState.SetStaticZones(PlayerDeploymentZone, …)`가 `PlayerDeploymentZone.Contains`로 칠합니다(전투 View PV-a, 2026-10-29 병합으로 들어옴). 도구는 `CombatStageSoAssets`가 같은 `PlayerDeploymentZone`(`Rows`·`Of`)으로 구역 값을 만듭니다.
- 그 전(10-28)에는 게임 View가 Buffer의 옛 속성 `PlayerDeploymentDepth`(칸 구역이면 0)로 칠해, 칸 목록 스테이지에서 아군 색과 "놓을 수 있는 칸" 표시가 나오지 않았습니다. 그동안 도구는 '아군 배치 구역' 묶음에 그 안내 줄을 띄웠습니다.
- 10-29에 도구 안내 줄(`CombatLayoutWindow.SoMode.cs` `DrawSoPlayerZone`)을 지우고, 사용법 §5.8 "게임 화면" 문단과 문제 표 줄도 고쳤습니다(SO 모드 계획 §7.3). 이 절에 남은 일은 없습니다.

## 8. 게임 파일 변경: 바닥 타일 칸별 회전 (2026-10-29, 호진 → 용근님, 미전달)

SO 모드에 바닥 타일을 칸마다 돌리는 기능을 넣으면서, 회전을 게임 데이터에 담고 게임이 놓을 때 돌리도록 **용근님 파일 두 개를 고쳤습니다**(사용자 결정 Q-TT1 A, [SO 모드 설계 §13](../specs/2026-10-27-combat-stage-editor-so-mode-design-Hojin.md#13-바닥-타일-칸별-회전-tt1tt9)). 컴파일·Unity 확인은 사용자가 했습니다(Ua).

| 파일 | 바뀐 것 |
|---|---|
| `DataBaseSystem/Data/View/Table/StageViewProp/Item/CombatMapTilesData.cs` | 입력 `CombatMapTilesInput`에 `tiles` 다음 칸 `Rotation4[] rotations`(툴팁 포함). 순서는 `tiles`와 같고 **비우면 모두 R0**. `CreateData`: "모두 비면 null"에 회전 길이, 새 검사 함수 `CreateRotations` — 길이가 0도 가로 × 세로도 아니면 예외, 정의 안 된 값 예외, `tileSize` 가로 ≠ 세로(`Mathf.Approximately`)인데 R90·R270이면 칸 좌표를 담은 예외. 불변 값 `CombatMapTilesData` 생성자 끝 인자 `rotations`(null·길이 검사)와 `RotationAt(column, row)` |
| `ViewSystem/Combat/Display/Map/StageMapTiles.cs` | `Build`가 타일을 만든 뒤 `localRotation = Euler(0, RotationAt × 90, 0) × 프리팹 localRotation` 한 줄(피벗 중심, 타일 루트 위에서 본 시계 방향) |

- **지금 에셋은 바뀌지 않습니다.** `rotations` 칸이 YAML에 없으면 빈 목록이라 모두 R0이고 화면이 같습니다.
- 지형 표시·높이 표(`CombatMapTileSurface`)·타일 맞춤·바닥 교체(`PlaceOnTile`·`FieldTileReplacement`)는 바꾸지 않았습니다. 이들은 타일의 위치·부모만 읽어 회전과 무관합니다. 바닥 교체 외형은 지금처럼 필드 기준 회전이라, 돌린 타일 칸이 무너져도 교체 외형은 돌지 않습니다.
- 생성자를 부르는 곳은 `CreateData` 하나이고, 시험 코드는 이 두 형식을 쓰지 않습니다.
- 도구 사본: 3D `BuildTiles`가 같은 회전 한 줄을 갖습니다(§1 표). 이 식을 바꾸시면 도구도 맞춰 주세요.
- 피벗이 타일 가운데가 아닌 프리팹은 돌리면 칸에서 벗어납니다. 타일 프리팹을 만드실 때 피벗을 가운데(바닥)로 두세요.

## 9. FU-b 표시 사본 보완 (2026-10-31, 미전달)

| 원본 | 도구 사본·범위 |
|---|---|
| `FieldTerrainPresenter.LocalPosition`·`TornadoBaseHeight` | `CombatStageSoSpace.TornadoPosition` → Preview. 반 칸 이동 + 판 안 2×2 원래 타일 바탕 최대 높이. 교체 지형 높이는 제외, 음수 유지, 배율 중복 적용 없음 |
| `CombatEnvironmentFacts.IsKnownToPlayer`·`IsShown`(켜짐·공개, 2026-11-04 평가 C1-05에서 `TerrainViewSet.IsShownFact`를 대신함)·`ObjectViewSet.Render` | `CombatStageSoVisuals.IsShown`. Owner 열거 순번을 `Names.owner`의 이름으로 해석. Player 기준에서 지형·오브젝트 필터, 빈 공개 목록은 숨김 |
| 표시 지형 기반 표면·타일 교체 | 같은 필터를 `RaiseSurfaces`와 Preview에 전달. 모드를 배치 캐시 키에 포함하고 자신이 숨긴 타일만 복원 |

전체 배치가 기본이며 Player 기준은 선택입니다. 2D는 전체 편집, Player 기준 3D의 화면 편집만 표시 대상으로 한정합니다. 숨은 배치도 충돌 판정에는 포함합니다. 고지대 이웃 모양 B1은 보류이며 기존 높이·외형·Core 규칙은 유지합니다. 런타임·Core 수정은 없습니다. 코드 반영과 사용자 검증은 별개이며 컴파일·Unity Ub 확인은 미완료입니다.

## 10. 타일 후보·Random 회전 (2026-11-01, 미전달)

- 원본/Task: [설계](../specs/2026-11-01-combat-map-tile-variants-random-rotation-design.md), [계획](../plans/2026-11-01-combat-map-tile-variants-random-rotation-plan.md). 앞 절은 당시 이력이며, 현재 타일 입력은 이 절을 따른다.
- `CombatMapTileSetSO.tiles[].prefab` 하나가 **`prefabs[]`**로 바뀐다. 종류당 한 줄과 공유 `surfaceHeight`는 유지한다. 기존 TileSet 참조는 후보 한 개로 이전했다. `CombatStageSoAssets.ReadTileSet`도 새 후보 배열을 읽는다.
- `mapTiles.rotations`는 View 전용 **`CombatMapTileRotation : byte`**(R0=0/R90=1/R180=2/R270=3/Random=4)다. 기존 0~3과 빈 목록의 R0는 유지하며 Core Rotation4·지형/오브젝트 회전은 바꾸지 않는다. 작업본은 int 값 4를 그대로 저장한다.
- 선택 계산은 **Tool.Runtime/MapTileRandomSelection** 한 곳이다. 런타임은 `StageMapTiles.Build`의 생성 시드, 도구는 창의 미리보기 시드로 같은 함수를 쓴다. 후보 하나는 그대로, 여러 후보는 칸마다 고른다. Random 각은 정사각 네 각/직사각 0·180도다. 후보/회전 추첨은 별도 경로이며 전투/전역 난수를 사용하지 않는다.
- 도구의 `CombatStageSoTileLayout.ResolvedRotationAt` → 타일 보기/칸 정보/Preview가 같은 실제 예시 각을 쓴다. Random을 4 modulo로 0도 처리하지 않는다. `?` 표식·툴팁·칸 정보가 Random을 구분하며, R/Shift+R은 Random을 유지한다. `랜덤 다시 뽑기`는 시드/캐시만 바꾸고 SO·작업본·Undo에 결과를 쓰지 않는다.
- 높이 표·칸 중심 대응·바닥 교체/복구·배경/표시 해제 순서는 기존 책임을 유지한다. 후보는 같은 크기·피벗·높이로 제작한다.
- 검증·리뷰 결과와 실제 Unity 확인 범위는 위 계획의 실행 기록/최종 검토 문서를 따른다. 이 메모를 외부에 발송한 것은 아니다.

## 11. UX-c 숨김 진단·복구 (2026-11-02, 미전달)

- 공개 규칙 원본은 기존 `CombatStageSoVisuals.TryIsShown`입니다. SoUx가 Player 공개 진단을 보기와 무관하게 읽어 정상 비공개/판정 오류를 따로 세고, 무작위 시작은 고정 위치 없음으로 구분합니다. 전체/2D 전환만으로 오류 원인을 없애지 않습니다. 실제 화면 필터·충돌 판정·공개 목록은 기존 계약 그대로입니다.
- SoWorkspace는 속성 영역에 안내를 조립하고 SoMode는 기존 맵/붙여넣기에 연결합니다. `CombatStageSoIssue`의 대상 정보와 Root/개정을 사용해 고정 오류의 기존 위치/판 밖 편집으로 가며, 무작위 시작은 기존 패널을 엽니다. 위치 없는 일반 오류는 가짜 좌표를 만들지 않습니다.
- 전체 보기와 2D 복구는 선택·클립보드·3D 시점을 유지하고 미확정 끌기를 취소합니다. 기존 Preview 기준 키와 RaiseSurfaces가 높이·외형·타일 숨김을 갱신합니다. 붙여넣기는 전환 뒤 사용자 재실행만 받습니다. 전체 3D 표시 실패 때도 기존 Workspace 오류 경계로 편집 패널을 유지합니다.
- 표시 진단은 저장 검사 결과에 합산하지 않습니다. 작업본 수정/Undo는 재검사 필요, 보기 전환은 검사/Dirty를 유지합니다. 검사 통과와 저장 완료·실제 전투 진입 성공을 구분합니다. 사용법 §5.7·§8.2와 계획 §12.9에 동선/한계를 기록했습니다.
- 런타임 View/Core 및 Visuals/Preview 식은 이번에 변경하지 않았습니다. 고지대 B1 보류, **컴파일·Unity Uc 미확인, 커밋 전, 공유 미전달**입니다. 기존 확인을 새 UX 확인으로 확대하지 않습니다.

## 토네이도 사본 (2026-11-01)

`CombatStageSoSpace.TornadoPosition`과 `CombatStageSoPreview`의 토네이도 자리·배율은 이제 `FieldTornadoPresenter.BodyPosition`·`Create`의 사본입니다(중심 기준 정사각형, 비율 = 판정 칸 수 ÷ 프리팹 기준 칸 수). 옛 반 칸·2×2 설명은 낡았습니다.

## 12. HBE-a 하붑 입력 연결 (2026-11-02, 미전달)

- Tool에서 기존 CombatStageData.haboob의 사용 여부·이동 주기·경고 시간·영역 상대 좌표를 읽고 편집/저장할 수 있게 연결했습니다. 위치는 **스테이지 설정 → 기믹 → 하붑**입니다.
- 값 규칙은 공개 HaboobGimmickSpec, 실제 초기 위치/재배치는 기존 Core가 소유합니다. Tool의 값 검사가 필드 안 위치 선정 성공을 보장하지 않습니다. Core·HaboobVisual·FieldHaboobPresenter·이펙트/사운드·Buffer와 Preview 식은 이번에 변경하지 않았습니다.
- 작업본 형식은 4→5입니다. 적용 전 미저장 편집을 저장하고 구형 초안/열린 작업본은 기존 거절/에셋 다시 읽기 정책을 따릅니다. 하붑을 꺼도 주기/모양은 저장되고, 배치 비우기/스테이지 복제에서도 틀의 설정을 보존합니다.
- 검사 오류는 “하붑 값:”으로 표시하고 원본 대신 검사 복제본에서만 비활성화하여 다른 검사와 함께 보고합니다. 설정 오류에 고정 위치 버튼을 만들지 않으며, 편집 툴의 3D 구름·다음 위치 미리보기·새 경고 이펙트는 없습니다.
- 새 SoHaboob은 UI, Document는 값, Assets는 SO 입출력, Validation은 공개 규칙 호출, SoMode/Workspace는 조립을 맡습니다. 기존 Undo·초안·Dirty·검사 수명/복제 경로를 유지합니다.
- **고지대 B1 보류, 컴파일·Unity 미확인, 커밋 전, 공유 미전달.** 실행 기록은 계획 §15.4이며 기존 View/UX 확인을 이번 확인으로 확대하지 않습니다.

## 13. GSE-a 낙석·하붑 모양 입력 (2026-11-02, 미전달)

- Tool 기믹 패널의 상대 칸 목록에 클릭/드래그 격자를 더했습니다. 낙석은 돌 하나의 모양, 하붑은 구름 영역의 모양이며 실제 맵의 고정 위치를 지정하지 않습니다. 배치 Sidebar 바로가기와 세부/고급 좌표 편집을 제공합니다.
- 확정된 획만 기존 Document 목록에 SoChange로 반영하고 Undo·초안·Dirty·재검사를 재사용합니다. 미완료 획은 취소/포커스 상실/전환/해제에서 버립니다. 보기만 해서는 값/순서를 바꾸지 않으며 기본 모양은 Core 공개 Spec을 사용합니다.
- Core·DB SO·FieldHaboobPresenter·낙석 View·Buffer·자동 위치·경고 이펙트·3D Preview 식·고지대는 변경하지 않았습니다. 새 격자는 기존 EditorWindow 내부 기능입니다. 검사/저장/복제 경로와 작업본 형식 5를 유지합니다.
- 실행 원본은 계획 §16.3, 사용자 Unity 확인은 §16.2입니다. **컴파일·Unity 미확인, 공유 미전달, 커밋 전**이며 GSE의 확인을 앞선 HBE/FU 확인으로 대체하지 않습니다.

## 14. RTE-a 스테이지별 터널 폭발 (2026-11-02, 미전달)

- **낙석 → 세부 설정 → 터널 폭발 사용**은 스테이지별 StageViewPropData에 저장합니다. 기존 에셋은 켜짐입니다. Tool의 저장·검사·Undo·초안·스테이지 복제/배치 비우기에 연결했고, 영역/타일 클립보드와 모양 초기화에는 넣지 않았습니다. 작업본 형식은 5→6입니다.
- 같은 View SO 조회 → StageViewPropPresenter 캐시 → 실제 표시/특성 미리보기 Factory의 필수 bool → FieldRockfallPresenter 순서입니다. `TryGetTunnelPose`의 정상 false는 터널 없음뿐 아니라 명시적으로 폭발을 끈 경우도 포함합니다. 꺼짐이면 누락 로그도 남기지 않습니다.
- EventHandler를 바꾸지 않고 기존 분기로 터널 WarningCues만 생략합니다. 경고 칸·느낌표·경고음과 능선/낙하/착지·Core 규칙은 유지합니다. 터널/능선 오브젝트 수 검사는 그대로여서 같은 이름 중복 오류는 꺼짐으로 우회되지 않습니다.
- 배경 Release는 표시 옵션을 기본값으로 되돌립니다. 터널과 자식 능선을 삭제/비활성화하지 않고, 공용 RockfallVisual·프리팹·운영 SO·하붑·고지대는 수정하지 않았습니다.
- 계약 원본은 편집 설계 §24, 실행·확인은 계획 §17입니다. **컴파일·Unity 미확인, 커밋 전, 공유 미전달, 고지대 B1 계속 보류.**

## 15. 현행 기믹·UX와 View 경계 (2026-11-09, 미전달)

- UXR 화면은 배치/바닥 타일/스테이지 설정, 설정의 **기믹**에서 레일·낙석·하붑을 편집합니다. 상단 **사용법**의 여섯 주제에서 기능 위치·상세 문서·기존 검사/공개 진단/초안 보호에 접근합니다. 진입점과 정적 대조 원본은 계획 §20.9이며 전체 구현 책임은 [민형님 공유 §10](2026-10-27-combat-stage-editor-so-mode-minhyeong-share.md#10-현행-화면수명과-uxr-d-대조-2026-11-09-미전달)에 있습니다.
- HAT/GP에서 하붑은 **지정한 시작 위치의 구름 영역을 2D·3D 편집 맵에 표시**하고, 클릭/드롭으로 이동합니다. 이는 실제 이펙트/다음 이동 위치 예측이 아닙니다. 클릭은 구름 가운데 근처, X/Y는 모양 왼쪽 아래 기준점이며 이후 이동은 무작위입니다. 시작 비우기/배치 없는 구름 칸 Delete는 시작 지정만 끄고, 배치가 있으면 배치를 먼저 지웁니다. 판 밖 시작은 기존 검사에서 막습니다.
- 낙석 도구는 사용 여부만 켜고 실제 낙하 위치는 자동입니다. 터널 폭발 옵션은 기존 RTE의 경고/낙하/착지 보존 계약을 따릅니다. 모양 격자는 상대 영역만 편집하며 실제 전투 시뮬레이터가 아닙니다. 기믹 설정은 스테이지 복제/배치 비우기에 보존되고 배치·타일 클립보드에는 넣지 않습니다.
- 현행 작업본 형식은 7입니다. DP-a는 구형·손상·Root 누락 초안을 자동 버리지 않고 PC 로컬 보호 원문으로 보관합니다. 내보내기/복원/폐기는 도구 경계이며 SO 자동 저장·형식 자동 변환·게임용 내보내기가 아닙니다. 게임 View에는 새 초안/보관 상태를 전달하지 않습니다.
- UXR/TXT/DP는 Preview·Visuals 배치 식이나 전투 연출을 새로 구현하지 않습니다. 입력은 SoMode/SoTiles/모양 편집기, 화면 조립은 Workspace, 도움말은 Help, 검사/공개 진단은 SoUx, 원문 보관은 DraftStore/SoDrafts가 소유합니다. PreviewRenderUtility와 공유 복제본의 기존 해제 경로, 게임 서비스·Buffer 계약은 유지합니다.
- **고지대 B1 이웃 모양 개선은 보류**입니다. 기존 높이·외형 회전 제한·Core 규칙은 유지합니다. 이번 UXR-d에서 게임 코드·운영 에셋·초안을 변경하거나 전투/저장 검증을 실행하지 않았습니다.
- 기능 진입점 누락은 정적 대조에서 발견하지 않았습니다. D-01은 후속 승인으로 본부 포함·아군 구역 제외 안내와 상자 삭제 주석을 수정했습니다(계획 §20.10). 삭제 로직·View/Core 계약은 유지했습니다. 최신 HBE/GSE/RTE/HAT/GP·DP·TXT·UXR의 Unity 확인은 계획 §20.9에서 별도 대기입니다. 과거 Stage_Test2 일부 보기 확인을 저장·복제·Undo·전투 연출 확인으로 간주하지 않습니다. **공유 미전달**입니다.
