# 공격 궤적 — 필드 설정 표 변경 공유 (호진님, 2026-10-19, 미전달)

전투 공격 모드에서 호버한 칸까지 궤적 점선을 그리는 작업([설계](../specs/2026-10-19-combat-attack-trajectory-design.md), [계획](../plans/2026-10-19-combat-attack-trajectory-plan.md) TJ-2)으로 View DB 필드 설정 표가 바뀌었습니다.

## 바뀐 것

1. **`StageFieldViewData`(`DataBaseSystem/Data/View/Table/StageField/Item/StageFieldViewDataSO.cs`)에 새 묶음 "Attack Trajectory"**

   | 칸 | 뜻 | 검사 |
   |---|---|---|
   | `trajectoryMaterial` | 궤적 점선 머티리얼(AreaOutline 셰이더) | **필수** — 없으면 DB 생성(부트)이 실패합니다 |
   | `trajectoryThickness` | 점선 폭(월드 길이, 기본 0.08) | 양수 |
   | `trajectoryDotSpacing` | 점 간격(월드 길이, 기본 0.3) | 양수 |
   | `trajectoryDotFill` | 간격 한 칸에서 점이 차지하는 비율(기본 0.45) | (0, 1] |
   | `trajectoryFlowSpeed` | 점이 목표 쪽으로 흐르는 속도(기본 0.6, 0이면 멈춤) | 0 이상 |
   | `trajectoryBlockedMarker` | 벽에 막힌 자리에 띄우는 X 표지 프리팹 | **선택** — 비우면 표지 없이 궤적만 끊김, 넣으면 Renderer 필요 |

   검사는 표시 모드(CellPrefab·GridOutline)와 상관없이 돕니다.

2. **에셋 `DataBaseSO/ViewGroup/StageFieldView/Item/StageFieldView_Default.asset`**에 위 값과 재질 참조를 넣었습니다. X 표지는 아직 비어 있습니다(아트가 생기면 넣어 주세요).

3. **새 재질 `Assets/ProjectL/Prefabs/Combat/Materials/AttackTrajectory.mat`** — 민형님 `AreaOutline.mat`을 복사해 이름·점선 값만 바꿨습니다. 원본은 그대로입니다. 점선 값(`_DashCount = 1` 등)은 코드가 렌더러별로 덮어쓰므로 재질 값을 바꿔도 화면은 DB 값을 따릅니다.

4. **색은 DB가 아니라 `FieldView` 씬 값**(`trajectoryColor`·`trajectoryBlockedColor`)입니다. 다른 필드 강조 색과 같은 자리입니다.

## 함께 커밋해야 하는 것

`StageFieldViewDataSO.cs`, `StageFieldView_Default.asset`, `AttackTrajectory.mat`(+ `.meta`)는 **한 커밋**이어야 합니다. 코드만 들어가면 재질이 비어 DB 생성이 실패합니다.

## 시험

- `ViewDataTableTests`: 설정 도우미 둘에 궤적 재질을 넣었고, 궤적 값 검사 시험 `StageFieldView_AttackTrajectoryValuesAreChecked`를 더했습니다.
- 시험이 `new StageFieldViewData()`로 만든 맨 설정은 DB 표를 거치지 않으므로 재질이 없어도 됩니다. 그때 필드는 궤적을 만들지 않습니다.

## 추가 — 공격 조준 호버 색 분리 (2026-10-19)

공격 조준 호버의 칸 색을 배치·아이템 Drag·이동 끝점 호버와 나눴습니다. DB 표는 바뀌지 않았습니다.

- **`FieldView` 씬 값 셋 추가:** `attackHoverColor`(판단 전)·`attackHoverValidColor`(공격 가능)·`attackHoverInvalidColor`(공격 불가). 기본값은 지금 호버 색 셋과 같아 화면은 그대로입니다. GridOutline 모드의 영향 칸 채우기와 막힌 벽 층도 이 공격 색을 따릅니다.
- **호진님 시험 파일 한 줄:** 표시 계약 `ICombatInteractionFeedback.SetHoverCell`에 셋째 인자 `CombatHoverKind kind`가 생겨, `HojinCombatAttackDragTests`의 가짜 표시 `SetHoverCell` 서명만 맞췄습니다(본문·단언은 그대로).

## 추가 — 이동 경로선 값 나눔 (2026-10-29, 미전달)

필드 설정 표(`StageFieldViewDataSO.cs`)에서 이동 경로선이 칸 테두리 값을 함께 쓰던 것을 나눴습니다. 근거는 [격자 아웃라인 설계](../specs/2026-10-09-field-grid-outline-mode-design.md) 머리말 2026-10-29 줄입니다.

- **새 칸 둘(머리말 "Movement Path"):** `movementPathThickness`(0.06)·`movementPathYOffset`(0.02). 읽기 `MovementPathThickness`·`MovementPathYOffset`, 검사는 GridOutline 모드에서만(두께 양수·높이 0 이상).
- **그대로인 것:** `outlineThickness`·`outlineYOffset`은 칸 테두리·칸 채우기만 씁니다. 필드 이름·순서는 바꾸지 않았고 머리말·툴팁만 기능별(공통·Grid Decal·Cell Outline·Movement Path)로 나눴습니다. `outlineCellSpacing`은 이름 그대로입니다(SO 모드 도구가 문자열로 읽음).
- **에셋:** `StageFieldView_Default.asset`에 새 두 칸을 옛 값과 같게 적었습니다(화면 같음). 코드와 한 커밋입니다.
- **병합 때 주의:** 같은 "Grid Outline" 묶음에 줄을 더하셨다면 머리말 줄에서 충돌할 수 있습니다. 두 쪽 줄을 모두 남기면 됩니다.
- **부탁:** 따로 하실 일은 없습니다.

## 추가 — 이동 경로 화살표 칸 넷 (2026-10-29, 미전달)

이동 경로를 선 대신 **띠 + 꺾인 칸의 직각 이음 + 끝 화살촉 하나**로 그리면서, 필드 설정 표(`StageFieldViewDataSO.cs`) "Movement Path" 묶음에 칸 넷을 더했습니다. 설계는 [이동 경로 화살표](../specs/2026-10-29-combat-movement-path-arrow-design.md)(MA1~MA7), 실행 기록은 [계획](../plans/2026-10-29-combat-movement-path-arrow-plan.md) §5입니다. 컴파일 미확인입니다.

- **새 칸 넷(`movementPathYOffset` 바로 뒤, 이 순서):**
  - `movementPathMaterial`(Material): 띠와 화살촉이 함께 쓰는 재질입니다. 색은 Combat 씬 `FieldView.movementDraftColor`가 덮어씁니다.
  - `movementPathArrowLength`(기본 0.5): 화살촉 꼭지에서 밑변까지 길이입니다(필드 로컬 단위 = 칸 간격과 같은 단위).
  - `movementPathArrowWidth`(기본 0.8): 화살촉 밑변 너비입니다.
  - `movementPathArrowTipOffset`(기본 0): 꼭지 자리입니다. 0이면 도착 칸 가운데, 양수면 진행 방향 앞, 음수면 뒤입니다.
  - 읽기 `MovementPathMaterial`·`MovementPathArrowLength`·`MovementPathArrowWidth`·`MovementPathArrowTipOffset`.
- **검사(GridOutline 모드에서만, 새 함수 `ValidateMovementPath()` 하나):** 지난 절의 두께·높이 검사도 이 함수로 옮겼습니다. 더한 조건은 다음과 같습니다.
  - 재질이 있어야 합니다.
  - 0 < 길이 ≤ 칸 간격입니다(한 칸보다 길면 밑변이 앞 칸의 꺾임과 겹침).
  - 너비 ≥ 띠 두께 × 2입니다(화살촉이 띠보다 넓어야 화살표로 보임).
  - |꼭지 자리| ≤ 칸 간격(가로·세로 중 작은 값) / 2입니다(꼭지가 도착 칸 안).
  - 길이 / 2 − 꼭지 자리 < 칸 간격입니다(띠 끝 = 화살촉 가운데라서, 한 걸음 경로에도 띠가 남음).
  - 재질의 `_BaseColor` 속성은 DB가 아니라 필드를 구성할 때 표시 쪽이 검사합니다.
- **에셋:** `StageFieldView_Default.asset`에 경로 전용 재질 `Prefabs/Combat/Materials/MovementPath.mat`(선 재질 `LineShader.mat`을 복제, Render DB `LineColor`와 따로 고칠 수 있음)과 0.5·0.8·0을 적었습니다. 재질 에셋도 함께 커밋해야 합니다. 코드와 한 커밋입니다. 코드만 들어가고 재질이 비면 GridOutline 검사가 부트 DB 생성을 막습니다. GridOutline 칸을 가진 에셋은 이 기본 에셋 하나뿐입니다.
- **값은 언제 반영되나:** 필드 표시를 만들 때 한 번 읽으므로 다음 Combat 진입부터입니다(지금 다른 칸과 같음).
- **SO 모드 도구:** 필드 이름을 문자열로 읽는 곳에 이 넷은 없습니다(경로 값은 도구가 보지 않음).
- **부탁:** 따로 하실 일은 없습니다. 같은 묶음에 줄을 더하셨다면 병합 때 두 쪽 줄을 모두 남기면 됩니다.

## 추가 — 겹친 채우기 축소 비율 칸 하나 (2026-10-29, 미전달)

한 칸에 채우기가 겹치면(예: 공격 범위 칸 안의 영향 칸, 이동 범위 칸 위의 호버) 위 채우기를 한 단계씩 작게 칸 안쪽에 그리게 했습니다. 필드 설정 표(`StageFieldViewDataSO.cs`) "Cell Outline" 묶음에 칸 하나를 더했습니다. 설계는 [겹친 강조 안쪽 축소](../specs/2026-10-29-combat-overlap-inset-enemy-move-range-design.md)(OI1~OI5), 실행 기록은 [계획](../plans/2026-10-29-combat-overlap-inset-enemy-move-range-plan.md) §7입니다. 컴파일 미확인입니다.

- **새 칸(`highlightFillMaterial` 바로 뒤):** `overlapInsetScale`(float, 기본 0.7, 인스펙터 범위 0.05~0.95). 아래부터 k번째 채우기 = 칸 크기 × 0.7^k입니다. 읽기 `OverlapInsetScale`.
- **검사(GridOutline 모드에서만, `ValidateGridOutline` 안):** 0보다 크고 1보다 작아야 합니다.
- **에셋:** `StageFieldView_Default.asset`에 0.7을 적었습니다. 코드와 한 커밋입니다.
- **값은 언제 반영되나:** 다음 Combat 진입부터입니다(지금 다른 칸과 같음).
- **SO 모드 도구:** 필드 이름을 문자열로 읽는 곳에 이 칸은 없습니다.
- **부탁:** 따로 하실 일은 없습니다.

## 추가 — 겹친 칸 둥근 판·둥근 테두리 칸 둘, 채우기 재질 바뀜 (2026-10-29, 미전달)

겹친 채우기를 안쪽으로 줄인 판(같은 층 칸끼리는 한 덩어리)으로 그리고, 칸 테두리 모서리를 둥글게 깎을 수 있게 했습니다. 필드 설정 표(`StageFieldViewDataSO.cs`) "Cell Outline" 묶음에 칸 둘을 더했습니다. 설계는 [겹친 강조 2단계](../specs/2026-10-29-combat-overlap-rounded-inset-design.md)(§8 3판·§9 4판), 실행 기록은 [계획](../plans/2026-10-29-combat-overlap-rounded-inset-plan.md) §7입니다. 컴파일 미확인입니다.

- **새 칸 둘:**
  - `outlineCornerRadius`(float, 기본 0, 범위 0~1, `outlineDashFill` 바로 뒤): 칸 테두리 덩어리의 바깥으로 튀어나온 모서리 반지름입니다(칸 짧은 반변 대비 비율). 안으로 꺾인 모서리는 늘 직각입니다. 0이면 지금처럼 모두 직각이고, 레일·배치 구역 테두리도 같이 둥글어집니다. 읽기 `OutlineCornerRadius`.
  - `overlapCornerRadius`(float, 기본 0.35, 범위 0~1, `overlapInsetScale` 바로 뒤): 겹친 칸 안쪽 판과 그 둘레 테두리의 튀어나온 모서리 반지름입니다(판 짧은 반변 대비 비율). 읽기 `OverlapCornerRadius`.
  - 범위 상수 한 쌍 `MinCornerRadiusRatio`·`MaxCornerRadiusRatio`(0·1)를 두 칸이 함께 씁니다.
- **검사(GridOutline 모드에서만, `ValidateGridOutline`):** 두 칸 모두 유한하고 [0, 1]이어야 합니다. 앞 절의 `overlapInsetScale` 검사는 그 뒤 인스펙터 범위와 같은 상수 [0.05, 0.95]로 바뀌었습니다(앞 절의 "0보다 크고 1보다 작아야"는 낡은 문장입니다).
- **채우기 재질 바뀜:** `highlightFillMaterial`은 새 칸 채우기 셰이더 `ProjectL/Combat/CellFill`(`Prefabs/Combat/Shaders/CombatCellFill.shader`)의 재질이어야 합니다. 기본 에셋은 새 `Prefabs/Combat/Materials/CellFill.mat`을 가리킵니다(옛 `AreaOutline.mat`에서 바뀜, 그 재질은 테두리·궤적이 계속 씀). 재질의 셰이더 값(`_GridCorner` 등 열하나)은 DB 검사가 아니라 필드를 구성할 때 채우기 부품이 검사합니다. 그래서 옛 재질이 남은 에셋은 부트가 아니라 Combat 필드 구성 때 멈춥니다(이동 경로 재질과 같은 시점). 툴팁에 셰이더 이름을 적었습니다.
- **채우기 알파:** 칸 채우기 알파는 강조 색 알파 × `FieldView.fillAlpha`(Combat 씬 값)입니다. 알파 1인 색은 지금과 같습니다.
- **에셋:** `StageFieldView_Default.asset`에 0·0.35와 새 재질을 적었습니다. 셰이더·`CellFill.mat`(+ `.meta`, 새 폴더 `Shaders.meta`)과 코드는 한 커밋입니다.
- 중간에 넣었던 `overlapEdgeAlpha`(안쪽 판 가장자리선 알파)는 커밋 전에 지웠습니다.
- **이동 경로 높이 검사 하나 더함:** `movementPathYOffset`은 `outlineYOffset` 이상이어야 합니다(`ValidateMovementPath`). 경로는 깊이를 쓰는 불투명 띠라, 표시기가 모든 칸 테두리보다 한 단계 위에 놓아 반투명 채우기·테두리가 경로·화살촉 위에 덧칠되지 않게 하는데, 그 바탕 높이가 테두리보다 낮으면 다시 덧칠되기 때문입니다. 기본 에셋은 둘 다 0.02라 그대로 통과합니다.
- **툴팁:** `overlapInsetScale`에 0.95 근처에서 테두리 띠가 겹칠 수 있다는 한 줄, `movementPathYOffset`에 위 규칙.
- **값은 언제 반영되나:** 다음 Combat 진입부터입니다(지금 다른 칸과 같음).
- **SO 모드 도구:** 필드 이름을 문자열로 읽는 곳에 이 칸들은 없습니다.
- **부탁:** 따로 하실 일은 없습니다. 같은 묶음에 줄을 더하셨다면 병합 때 두 쪽 줄을 모두 남기면 됩니다.

## 추가 — 칸 테두리 방식 칸 둘 (2026-10-29, 미전달)

지금 덩어리 테두리는 그대로 두고, 칸마다 네 변을 모두 그리는 방식을 고를 수 있게 했습니다. 필드 설정 표(`StageFieldViewDataSO.cs`) "Cell Outline" 묶음에 칸 둘을 더했습니다(`outlineDashFill` 바로 뒤). 설계 [겹친 강조 2단계](../specs/2026-10-29-combat-overlap-rounded-inset-design.md) §10, 실행 기록 [계획](../plans/2026-10-29-combat-overlap-rounded-inset-plan.md) §7.7. 컴파일 미확인입니다.

- **새 칸 둘:**
  - `cellOutlineStyle`(새 열거 `FieldCellOutlineStyle`, 기본 `Merged` = 지금): `EachCell`이면 칸마다 네 변을 그립니다. 칸 크기 1·모서리 직각이면 맞닿은 변은 한 번만, 아니면 칸마다 따로 그리고, 겹친 칸 판·테두리도 칸마다 따로입니다. 레일·배치 구역·범위 층·호버 테두리가 모두 따릅니다. 읽기 `CellOutlineStyle`.
  - `cellOutlineScale`(float, 기본 1, 범위 0.05~1): EachCell에서 겹치지 않은 칸의 테두리·채우기 크기(칸 대비 비율)입니다. 1보다 작으면 칸마다 가운데로 줄인 네모이고, 겹친 칸은 이 크기 × `overlapInsetScale`^k입니다. Merged에서는 쓰지 않습니다. 읽기 `CellOutlineScale`.
- **둥근 네모:** 밑 층은 `outlineCornerRadius`, 겹친 층은 `overlapCornerRadius`(둘 다 원래 칸, 툴팁만 고침). EachCell에서는 칸마다 네 모서리가 둥글고 밑 층 채우기도 같은 모양입니다.
- **검사(GridOutline, `ValidateGridOutline`):** 열거 값이 정의돼 있어야 하고, 크기는 유한한 [0.05, 1]입니다.
- **에셋:** `StageFieldView_Default.asset`에 `cellOutlineStyle: 0`·`cellOutlineScale: 1`(화면 같음). 코드와 한 커밋입니다.
- **값은 언제 반영되나:** 다음 Combat 진입부터입니다. 이번부터 `outlineCornerRadius`를 바꿔도 다음 진입 때 표시기를 다시 만듭니다(이전에는 처음 값이 남음).
- **부탁:** 따로 하실 일은 없습니다.

## 추가 — 필드 색이 View DB로 (2026-11-14, 미전달)

전투 필드 색 30개와 채우기 불투명도를 `FieldView` 씬 값에서 필드 설정 표(`StageFieldViewDataSO.cs`)의 새 묶음 "Colors"로 옮겼습니다. 설계 [필드 색 DB](../specs/2026-11-14-combat-field-colors-db-design.md), 실행 기록 [계획](../plans/2026-11-14-combat-field-colors-db-plan.md) §7. 컴파일 미확인입니다.

- **새 칸:** `colors`(새 직렬화 클래스 `FieldColorsData`, `neutralGhostTint` 바로 뒤). 칸 강조 층 색·`fillAlpha`·공격 궤적 색·넉백/끌어오기 화살표 색이 들어 있습니다. 읽기 `Colors`. 색마다 툴팁에 "무엇 / 쓰이는 곳 / 그리기"를 적었습니다.
- **호진님 줄을 옮겼습니다:** 낙석 경고 색 `rockfallWarningColor`(낙석 RF-b `678dbafb7`, `FieldView` 씬 값)가 이 묶음으로 갔습니다. 값·뜻은 그대로입니다(느낌표 이미지 색). `FieldHighlightLayers.cs`의 `FieldHighlightColor.RockfallWarning` 문서 한 줄("씬 값" → "View DB 값")도 고쳤습니다. 낙석 계획 문서(RP-16의 "씬 값 `FieldView.rockfallWarningColor`")는 고치지 않았습니다.
- **검사:** `Validate` 끝에서 묶음을 검사합니다(색은 성분이 유한하고 알파 [0, 1], `fillAlpha` [0, 1]). 묶음이 비면 예외입니다.
- **에셋:** `StageFieldView_Default.asset`에 `colors:` 31줄(옮기기 전 `Combat.unity` 값과 같아 화면 같음). 코드와 한 커밋입니다. 새 필드 설정 에셋을 만들면 칸 기본값도 그 씬 값입니다.
- **값은 언제 반영되나:** 색과 `fillAlpha`는 에디터에서 고치면 전투 중에도 바로 반영됩니다(에디터 전용 신호, 빌드에는 없음). 모양 값은 지금처럼 다음 Combat 진입부터입니다.
- **주의:** 플레이 중에 고친 색은 플레이를 끝내도 에셋에 남습니다(SO라서). 튜닝한 뒤 커밋 전에 확인해 주세요.
- **SO 모드 도구:** 필드 설정 표에서 이름으로 읽는 칸(표시 방식·칸 간격·유닛 크기)에 색은 없습니다.
- **부탁:** 낙석 경고 색을 바꾸실 때는 이제 `StageFieldView_Default`의 Colors 묶음에서 고치시면 됩니다. 그 밖에 따로 하실 일은 없습니다.
