# 전투 지형 외형 크기를 칸 크기에 맞추기 — 설계 3판

- 작성: 2026-11-17 (사용자 "Terrain local size가 맵 사이즈를 안 따라가지? → 칸 크기 따라가게 설계 써줘")
- 2판: 2026-11-17 사용자 "ViewStageDataSO랑 같게 → 같은 규칙으로 고쳐". 1판의 새 기준 값(`terrainReferenceCellSize` 1.5)을 버리고 배경 맵의 자동 배율 규칙을 그대로 쓴다(**Q-CS1 정함**).
- 3판(2026-11-17)에서 사용자가 **Q-CS3~Q-CS6 모두 A**로 확정했다. 현재 설정에서 적용 전후 화면이 같아야 한다. 코드·에셋 이전 및 Unity 확인은 구현 단계에 진행한다.
- 범위: 전투 View 지형 외형(`FieldTerrainPresenter`·`FieldTornadoPresenter`)과 칸 높이 표, 배율 규칙 파일, 지형 외형 에셋 값, SO 모드 도구의 3D 사본

## 1. 지금 사실 (코드 대조)

| 무엇 | 지금 | 자리 |
|---|---|---|
| 배경 맵(타일 있음) 크기 | **자동**: `칸 간격 ÷ TileLength(2)`를 XYZ 같게. 정사각 칸·프리팹 루트 크기 1이 아니면 예외 | `StageViewPropPresenter.cs:102-106` → `Tool/Runtime/CombatMapAuthoringMetrics.Scale` (`af24adb57`, 11-10) |
| 배경 맵(타일 없음) 크기 | 손으로 `scaleOffset` | `StageViewPropPresenter.cs:109` |
| 지형 크기 | `프리팹 루트 localScale × SO localScale`. **칸 크기는 곱하지 않음** | `FieldTerrainPresenter.cs:49` |
| 지형 위치 | 타일 위 지형은 칸 바탕 위치 + SO `localOffset`(필드 로컬 단위) | `FieldTerrainPresenter.LocalPosition` |
| 칸 간격 | GridOutline = `StageFieldView_Default.outlineCellSpacing` (지금 1.5 × 1.5) | `FieldCellSpacing.FromGridOutline` |
| 지형 올림 높이 | 타일 위 지형은 SO `surfaceHeight`만큼 칸을 올림(필드 로컬 단위) | `FieldTerrainPresenter.FillSurface` → `FieldSurfaceHeights.Raise` |
| 토네이도 | `프리팹 × SO × (덮는 칸 수 / 기준 칸 수)`. 칸 수는 따르지만 칸 크기는 안 따름 | `FieldTornadoPresenter.cs:305-317` |
| 바닥 교체(ReplaceTile) | 타일이 있으면 같은 부모 아래 놓고, 없으면 필드 루트 아래 칸 가운데 놓음 | `FieldTerrainPresenter.Apply` → `PlaceOnTile` 또는 `PlaceInField` |
| 도구 사본 | SO 모드 3D가 같은 크기 식을 따로 가짐 | `Tool/Editor/CombatStageSoPreview.cs:290·320`, 높이 `CombatStageSoVisuals.cs:142-144` |

현재 칸의 자동 배율 `1.5 ÷ 2 = 0.75`는 벽 SO에 저장된 `localScale`과 같다(`2103abe20`, 11-02).

## 2. 목표

배경 맵과 **같은 규칙 하나**로 지형 외형의 크기·보정 위치·올림 높이가 칸 간격을 따라간다. 프리팹 제작 규격은 "한 칸 = 2"로 배경·타일·지형이 같다.

## 3. 설계

### CS1 배율 규칙은 `CombatMapAuthoringMetrics` 한 곳 (정함)

- 같은 파일에 배율 하나를 내는 함수를 더하고(`CellScale(cellWidth, cellLength, source)` → `cellWidth / TileLength`, 정사각 칸 검사 포함), 지금 `Scale`은 그것을 불러 `Vector3.one * 배율`을 돌려준다(배경 동작 같음, 루트 크기 1 검사는 `Scale`에만 남김 — 배경 전용 조건).
- 지형은 `CellScale`을 쓰고 지형 프리팹 루트 크기는 지금처럼 곱한다.
- 이 파일은 `Tool/Runtime`이고 Systems가 이미 참조한다(`StageViewPropPresenter`). 작성자는 용근(우리 줄).
- 1판의 View DB 새 칸은 만들지 않는다.

### CS2 배율 모양 (배경과 같음)

XYZ 같은 값 하나. 칸이 정사각이 아니면 배경과 똑같이 예외다. 지형은 90° 단위로 돌아가서 축마다 다른 값은 돌린 칸에서 축이 바뀌므로 맞지 않다. 1판 Q-CS2는 배경 규칙에 따라 A로 정해진다.

### CS3 무엇이 배율을 따르나

| 값 | 따름 | 이유 |
|---|---|---|
| 타일 위(OnTile) 지형 크기 | 예 | 목표 |
| 이웃 모양 프리팹(고지대·벽 Center·OneSide…) | 예 | 같은 Instance 식 |
| OnTile의 SO `localOffset` | 예 | 규격 칸에서 맞춘 보정 |
| OnTile의 SO `surfaceHeight`(칸 올림 높이) | 예 (Q-CS3 A) | 블록 윗면과 유닛이 서는 높이가 같은 비율로 올라감 |
| 토네이도 몸 크기·보정 | 예 | 칸 수 비율에 칸 크기 배율을 더 곱함 |
| 바닥 교체(ReplaceTile) | 기존 배치 유지 | 타일이 있으면 부모 배율을 상속한다. 타일이 없는 칸에서는 기존 필드 크기·보정을 쓴다. |
| 오브젝트·가시밭·흔적·유닛 | 기존 외형 유지 (Q-CS4 A) | 이번 적용 대상은 지형·토네이도 |

OnTile의 SO `localScale`·`localOffset`·`surfaceHeight`는 **한 칸 = 2일 때의 값**이 된다. 툴팁과 지형 외형 안내 §2에 적용 모드를 함께 적는다.

`FillSurface`는 OnTile의 올림 높이를 `Raise(cell, surfaceHeight × 배율)`로 기록한다. 유닛·고스트·경로선·포인터·격자 덧그리기는 기존 높이 표를 읽는다. 타일 바탕 높이와 바닥 교체의 `Replace` 값은 기존 필드 단위를 쓴다.

### CS4 에셋 값 다시 적기

**현재 화면 보존은 완료 조건이다.** 현재 `StageFieldView_Default`의 칸 간격은 `1.5 × 1.5`다. 새로 배율을 받는 SO 값마다 `이전 SO 값 ÷ 0.75f`를 저장한다. 게임과 SO 모드 3D에서 다시 `0.75f`를 곱하면 기존 크기·보정 위치·올림 높이가 복원된다. 프리팹 루트 배율, 이웃 모양별 회전, 토네이도의 기준 칸 수와 연출 설정은 기존 값을 사용한다.

| 지형 외형 SO | `localScale` 이전 → 저장값 (XYZ 각 축) | `surfaceHeight` 이전 → 저장값 |
|---|---|---|
| `TerrainVisual_Wall` | 0.75 → 1 | 0 → 0 |
| `TerrainVisual_HighGround` | 1 → 1.3333334 | 0.45 → 0.59999996 (약 0.6) |
| `TerrainVisual_Sandstorm` | 1 → 1.3333334 | 0 → 0 |
| `TerrainVisual_PresenceModifier` | 1 → 1.3333334 | 0 → 0 |
| `TerrainVisual_Tornado` | 1 → 1.3333334 | 0 → 0 |

표의 저장값은 기존 직렬화 값을 float로 읽어 나눈 결과다. 다섯 SO의 `localOffset`은 모두 (0, 0, 0)이므로 환산 후에도 0이다. `TerrainVisual_WeakGround`와 `TerrainVisual_Cliff`는 ReplaceTile이므로 기존 값을 쓴다. 구현 시 게임·미리보기의 배율 적용과 SO 환산을 함께 반영한다.

### CS5 코드 자리 (예상)

- 규칙: `Tool/Runtime/CombatMapAuthoringMetrics.cs`에 `CellScale` 하나, `Scale`이 그것을 부름.
- View: `FieldTerrainPresenter`(Instance 크기·보정에 배율, `FillSurface` 높이), `FieldTornadoPresenter`(ratio에 배율). 배율은 매퍼의 칸 간격으로 구한다(두 표시 모드 모두 같은 식). 매퍼에 칸 너비·길이 읽기 둘이 필요할 수 있다(`_cellWidth`·`_cellLength`는 지금 private).
- 연결: 표시는 `SyncSurface`·`Render`에서 이미 매퍼를 받으므로 **Factory 변경 없음 예상**(민형님 공유 없음).
- 에셋: 지형 외형 SO 다섯의 저장값은 CS4 표를 따른다.
- 도구(Q-CS6): SO 모드 3D 사본 셋에 `CombatMapAuthoringMetrics.CellScale`을 직접 부름(도구도 이 파일을 이미 씀 — `CombatMapScaleEditor.cs:17`). 사본 식이 아니라 같은 함수라 1판보다 SSOT가 좋다. 호진님 공유 메모 한 절.
- 툴팁: `TerrainVisualDataSO`에 CS3의 모드별 단위를 표시한다.
- 새 파일 없음. 새 시험 없음(View 작업 지시), 기존 시험은 컴파일이 깨지는 곳만.

## 4. 7기준 예상

| 기준 | 점수 | 메모 |
|---|---|---|
| SSOT | 9.5 | 제작 규격·배율 규칙 한 곳(배경·타일·지형·도구가 같은 함수), 높이는 표 한 곳 |
| SOLID | 9 | 규격은 규칙 파일, 표시는 받기만 |
| FW | 9.5 | Framework 무변경 |
| 독립성 | 8.5 | 오브젝트·유닛은 아직 안 따라가 표시 간 비율이 어긋날 수 있음(Q-CS4) |
| 캡슐화 | 9 | 매퍼 읽기 둘이 늘어남 |
| C#/Unity 권장 | 9 | 균일 배율로 회전 축 문제 회피 |
| 무의미한 If/Tick | 9.5 | 구성 때 한 번 계산 |

예상 약 9.2 (1판 9.1 → 기준 값 두 벌이 없어져 SSOT +0.5).

## 5. 확정한 결정 (2026-11-17)

- **Q-CS1·Q-CS2** 배경 맵의 제작 규격과 정사각 칸의 XYZ 균일 배율을 공유한다(CS1·CS2).
- **Q-CS3 A** 타일 위 지형의 올림 높이에도 칸 배율을 곱한다(CS3).
- **Q-CS4 A** 이번에는 지형과 토네이도에 적용한다. 오브젝트·가시밭·흔적은 후속 범위다.
- **Q-CS5 A** CS4대로 값을 환산한다. 현재 설정에서 화면 차이가 보이면 환산값과 적용 경로를 바로잡고 다시 확인한다.
- **Q-CS6 A** SO 모드 3D에도 동일한 배율 함수를 적용하고 호진님 공유 메모에 기록한다.

## 6. Phase (안)

- CS-a 규칙: `CellScale` 추가·`Scale`이 부름(배경 동작 같음, 화면 변화 없음).
- CS-b 표시·도구·에셋: 런타임과 SO 모드 3D의 크기·보정·높이 계산, 토네이도, 매퍼 읽기, CS4의 SO 다섯·툴팁을 함께 반영한다. 같은 SO를 읽는 두 화면 모두 이 Phase의 리뷰 대상이다.
- CS-c 문서·공유: 지형 외형 안내와 호진님 공유 메모를 갱신하고 Unity 확인 결과를 기록한다.

### Unity 확인 (예정)

1. **구현 전** 현재 GridOutline 1.5에서 같은 스테이지·배치·카메라로 Combat과 SO 모드 3D의 기준 화면을 각각 남긴다. 한 화면에 없는 지형은 해당 지형이 있는 배치에서 기록한다.
2. **구현 후 1.5**에서 각 화면을 자신의 기준 화면과 비교한다. 대상 다섯 종류의 크기·위치, 벽·고지대 이웃 모양의 접점, 고지대 윗면과 유닛 발·격자의 높이, 같은 점유 칸 수와 연출 시점의 토네이도 몸체가 일치해야 한다. 약한 지반과 낭떠러지의 타일 교체 모습도 확인한다.
3. **칸 간격 2.0**에서 타일 위 지형의 크기·보정·올림 높이가 1.5일 때의 `2 / 1.5`배인지, 고지대 위 유닛이 여전히 윗면에 서는지 확인한다. 바닥 교체는 타일이 있는 칸과 없는 칸을 나눠 CS3의 배치를 확인한다. 현재 설정의 화면 보존 판정은 2번에서 한다.
4. GridOutline에서는 게임과 SO 모드 3D가 같은 칸 간격으로 계산하는지 확인한다. CellPrefab에서 도구의 기존 1×1 근사는 게임 실측값과 다를 수 있다(`CombatStageSoAssets.ReadFieldView`). 정사각이 아닌 칸 간격의 자동 배율 검사는 배경과 같은 오류를 보고해야 한다.
