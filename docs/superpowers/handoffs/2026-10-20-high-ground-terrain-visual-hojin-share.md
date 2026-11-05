# 지형 외형 표 — 바닥 지형 등록·표면 높이 칸 — 호진님 공유 (2026-10-20, 미전달)

근거: [고지대 View 설계](../specs/2026-10-20-combat-high-ground-view-design.md), [계획](../plans/2026-10-20-combat-high-ground-view-plan.md) HG-0~HG-2.

## 1. 표 등록

`DataBaseSO/ViewGroup/TerrainVisual/TerrainVisualDataTable.asset`에 셋을 더했습니다.

- `TerrainVisual_HighGround`
- `TerrainVisual_WeakGround`
- `TerrainVisual_Cliff`

셋 다 GroundShape이고, 지금은 프리팹이 없고 높이도 0입니다. 10-12에 "세 외형은 표에 등록하지 않는다"고 정했던 것을 바꾼 것입니다.

## 2. 새 칸 `surfaceHeight`

`TerrainVisualDataSO`에 칸 `surfaceHeight`(블록 윗면 높이, 필드 로컬 단위, 기본 0)를 더했습니다. Data 생성자가 검사하는 규칙은 셋입니다.

- 값은 유한한 0 이상이어야 합니다.
- 0보다 크면 GroundShape여야 합니다(PlacedPrefab은 0).
- 0보다 크면 블록 프리팹이 있어야 합니다. 블록 없이 유닛만 떠 있는 화면을 막기 위해서입니다.

## 3. 바뀐 표시 규칙

- 지형을 그릴지는 이제 "외형에 프리팹이 있나" 하나로 정합니다. 예전에는 "PlacedPrefab인가"와 고지대·약한 지반·낭떠러지를 고르는 이름 목록, 두 군데로 정했습니다.
- 설치형은 표 검사로 늘 프리팹이 있어 지금과 같습니다.
- GroundShape는 프리팹을 넣으면 그 칸에 블록으로 그려집니다.

## 4. 병합 때 주의

- 표 코드(`TerrainVisualDataSO.cs`)와 표 에셋은 한 커밋입니다.
- `TerrainVisualDatabaseTests`의 실제 카탈로그 시험 이름이 `ActualCatalog_ProvidesPlaceholderCubesTornadoParticlesAndGroundShapeVisuals`로 바뀌었습니다. 세 외형은 미등록이 아니라 등록·GroundShape으로 단언합니다.
- 고지대 블록 프리팹과 높이 값은 Unity에서 민형님 `Resources/Map/Highground.fbx`를 가져온 뒤 넣습니다(계획 HG-1·HG-2 남은 에셋 작업). 그 FBX의 `.meta`가 아직 커밋되지 않았습니다.
