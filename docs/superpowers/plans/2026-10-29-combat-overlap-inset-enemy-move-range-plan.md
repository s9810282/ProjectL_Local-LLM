# 겹친 강조의 안쪽 축소 · 유닛 호버 이동 범위 Task 계획 (2026-10-29)

설계 [overlap-inset-enemy-move-range-design](../specs/2026-10-29-combat-overlap-inset-enemy-move-range-design.md)(OI1~OI5·EM1~EM2, 결정 §5)을 Phase로 나눕니다.

## 0. 기준

- 결정(10-29 사용자):
  - Q-EM1 A: 이동 칸 Core 조회는 이 세션이 구현합니다.
  - Q-EM2 A: 기존 유닛 공격 범위 응답에 이동 칸을 더합니다.
  - P4 바뀜: 아군 호버도 이동 + 공격을 그립니다.
  - P1~P3·P5~P7은 그대로이고, P8은 확인 대상입니다.
- 진행 규칙:
  - Phase가 끝나면 멈추고 보고합니다.
  - 새 시험은 쓰지 않고 MSBuild도 돌리지 않습니다. 보고에는 "컴파일 미확인"을 적습니다.
  - 기존 시험은 컴파일이 깨지는 곳만 맞춥니다.
  - 검토 에이전트는 묶음 끝에 한 번만 돌립니다(문서 Phase).
- 작업 트리: `Combat.unity`·FMOD 캐시·`CLAUDE.md`·문서 둘이 바뀐 채로 있습니다(우리 작업 아님, 손대지 않음). `FieldView` 새 색 칸은 씬을 다음에 저장할 때 기본값으로 생깁니다.
- 시험 영향(10-29 grep):
  - `Assets/Tests`에서 `ShowHoverRange`·`FieldHighlightPalette`·`FieldCellFill`·`FieldSortingOrders`를 부르는 곳은 없습니다.
  - 우리 `UnitAttackRangeQueryTests`가 `UnitAttackRangeResult.Success`·`Failure`를 부릅니다. 끝 선택 인자로 더해 그대로 컴파일되게 합니다(TP-5).

## 1. Phase

| Phase | 내용 | 화면 변화 |
|---|---|---|
| OI-a | 겹침 깊이 규칙·깊이별 채우기·값·정렬 순서 | 겹친 칸의 위 채우기가 안쪽으로 작아짐 |
| EM-a | Core 유닛 공격 범위 조회에 이동 칸 | 없음(View가 아직 안 읽음) |
| EM-b | 호버 이동 범위(아군·적) | 호버하면 이동 범위 + 그 위 공격 범위 |
| EM-c | 문서·공유·묶음 끝 검토 | 없음 |

OI-a와 EM-a는 파일이 겹치지 않습니다. EM-b는 EM-a 뒤입니다.

## 2. OI-a 겹침 축소

| Task | 내용 | 파일 |
|---|---|---|
| Ta.1 | 값 칸 `overlapInsetScale`(기본 0.7, 툴팁 "겹친 칸에서 위 채우기를 한 단계마다 줄이는 비율"), 읽기 속성, 검사(GridOutline만, 0 초과 1 미만) | `StageFieldViewDataSO.cs` |
| Ta.2 | 기본 에셋에 0.7 | `StageFieldView_Default.asset` |
| Ta.3 | 정렬 순서 `Fill(depth) = CellFill + depth`, `OutlineBase` 0 → 16, 주석(채우기는 깊이마다 하나씩 위) | `FieldSortingOrders.cs` |
| Ta.4 | 채우기 객체가 정렬 순서를 생성 인자로 받음(지금 상수 대신) | `FieldCellFill.cs` |
| Ta.5 | 새 부품 `FieldCellFillStack`(+ `.meta`): 채우기 종류(배치 구역 → 채우는 층 → 호버), 칸마다 아래부터 깊이를 매기는 규칙, 종류 × 깊이 객체(깊이 1 이상은 처음 쓸 때 만듦), 깊이별 크기·색·그린 값 기억·해제 | `Display/Field/FieldCellFillStack.cs` |
| Ta.6 | 렌더러는 채우기를 부품에 맡김: `CreateFills`·`TryGetFill`·`FillColor`·`ApplyFill`·`FillSlot`을 부품으로 옮기고, `Refresh`·`Build`·`Release`·`ForgetDrawn`은 부품을 부름, `CanReuse`에 비율 비교 한 줄 | `FieldOutlineRenderer.cs` |
| Ta.7 | 구성 때 검사: 가장 깊은 채우기 정렬 순서 < 테두리 시작 | `FieldCellFillStack.cs` |

부품의 꼴(TP-1):

```text
FieldCellFillStack(cellsRoot, material, meshes, colors, scale)
  Build()                              // 깊이 0 객체만 만듦
  Apply(acting, state, mapper, palette, spacing, yOffset)
  Forget() / Release()
```

- 깊이 규칙(OI1): 칸 하나에서 아래부터 셉니다. 배치 구역(배치 드래그 중이고 구역 칸) → 채우는 층(`DrawOrder` 낮은 것부터, 그 칸에 있는 것만) → 호버(호버 칸) 순서이고, k번째는 칸 크기 × scale^k입니다.
- 색은 지금 `FillColor` 그대로입니다(층은 `FillColorOf`, 구역은 구역 색, 호버는 호버 상태·종류 색, 알파는 공용 채우기 알파).
- 높이는 지금 `ApplyFill` 그대로입니다(`CellOverlayToWorld`, 지형 높이만).

Ua(Unity 확인):

1. 컴파일 오류 없음.
2. 겹치지 않은 칸의 채우기가 지금과 같습니다(이동 범위만, 공격 범위만).
3. 공격 모드에서 대상에 호버하면 영향 칸이 공격 범위 칸 안쪽에 작게 채워집니다.
4. 공격 범위·이동 범위 칸 위 호버는 호버 채우기가 안쪽에 작게 그려집니다.
5. 배치 드래그 중 배치 구역 위 호버는 호버가 안쪽입니다.
6. 넉백 도착 칸이 공격 범위 안이면 안쪽에 그려집니다.
7. 테두리는 지금과 같습니다(P1).
8. 비율을 0.5로 바꾸면 다음 Combat 진입부터 더 작아집니다.

## 3. EM-a Core 이동 칸 (사용자 지시 Core 변경)

| Task | 내용 | 파일 |
|---|---|---|
| Tb.1 | 결과에 `MoveCells`(복사본, 읽기 전용), `Success(unitId, rangeCells, moveCells = null)`·`Failure(reason, unitId, moveCells = null)`(null이면 빈 목록), 요약 주석 | `Contracts/Results/UnitAttackRangeResult.cs` |
| Tb.2 | `Range`의 너비 우선 탐색을 `Reach(input, actor, environment, allowance)` 하나로 꺼내 `Range`가 부름(결과 같음) | `Application/Combat/CombatMovementCalculator.cs` |
| Tb.3 | 새 `UnitMoveCells(input, unit, environment)`: 필드에 살아 있음·자리 정상·보드 안·들어 옮기는 이동·자발 이동 가능·이동력 있음이면 `Reach`, 아니면 빈 목록. 차례·비용·행동 사용은 보지 않음. Y·X 순서 | 같은 파일 |
| Tb.4 | `UnitRange`가 유닛을 찾은 뒤 플레이어 시점 지형으로 이동 칸을 먼저 구하고, 공격 실패·성공 결과 모두에 담음(P8). 전투 끝·유닛 없음은 지금처럼 빈 실패 | `Application/Combat/CombatAttackQueries.cs` |
| Tb.5 | 기록: Core 전달 문서 §5 다음 번호(지금 끝 §5.58)에 바꾼 줄·이유·시험 영향(없음) | `handoffs/2026-09-24-view-core-data-boundary-handoff.md` |

Ua2(Unity 확인): 컴파일 오류 없음. 화면 변화 없음(이동 모드 범위·공격 범위 그대로).

## 4. EM-b 호버 이동 범위

| Task | 내용 | 파일 |
|---|---|---|
| Tc.1 | 층 `AllyHoverMoveRange = 1 << 14`·`EnemyMoveRange = 1 << 15`, 색 종류 둘, 표 두 줄(끌어오기 범위 뒤·공격 범위 앞, 채움, 평소에는 안 그림), 비트를 다 썼다는 주석 | `FieldHighlightLayers.cs` |
| Tc.2 | 팔레트 칸 둘·생성자 인자 둘·`TryGet` 갈래 | `FieldHighlightPalette.cs` |
| Tc.3 | 씬 색 칸 둘 `allyHoverMoveRangeColor`·`enemyMoveRangeColor`(기본 = 지금 `moveRangeColor` 값, "입력·미리보기 층 색" 묶음), `CreatePalette` | `FieldView.cs` |
| Tc.4 | 표시 의도 `ShowHoverRange(attackCells, moveCells, tone)`, 요약 주석 | `ICombatUnitRangeFeedback.cs` |
| Tc.5 | 표시: 색조 → (공격 층, 이동 층), 위험은 이동 층 없음(이동 칸이 오면 예외), 그린 층 둘을 기억해 바뀌면 먼저 지움, `ClearHoverRange`는 둘 다 | `CombatAttackFeedback.cs`, `CombatSceneDisplay.cs` |
| Tc.6 | 호버: 응답의 이동 칸(아군·적)과 성공일 때 공격 칸을 넘김, 둘 다 비면 지움, 위험은 이동 칸 없이 | `CombatUnitRangeHover.cs` |

Ub(Unity 확인):

1. 컴파일 오류 없음.
2. 적에 호버하면 이동 범위(파랑)와 그 위 공격 범위가 보이고, 겹친 칸은 공격이 안쪽입니다.
3. 아군에 호버해도 같습니다(아군 공격 범위 색).
4. 스스로 움직이지 않는 유닛(이동력 0 등)은 공격 범위만 보입니다.
5. 보스 스테이지(여러 칸 유닛)에서는 이동 범위만 보입니다(P8, 공격 범위 조회가 실패해서).
6. 이동 모드에서 다른 아군·적에 호버하면 아군 이동 범위 → 호버 이동 → 호버 공격 순서로 안쪽에 그려집니다.
7. 지뢰·화약통 호버는 지금과 같습니다(이동 범위 없음).
8. 공격 모드·Drag 중에는 호버 범위가 그려지지 않습니다(지금과 같음).
9. CellPrefab 모드에서는 맨 위 색만 칠해집니다.

## 5. EM-c 문서·공유·검토

| Task | 내용 |
|---|---|
| Td.1 | 호진님 필드 설정 표 메모에 새 절: `overlapInsetScale`(View DB 칸·기본 에셋, 미전달) |
| Td.2 | 폴더 배치 안내 `Display/Field/` 줄에 `FieldCellFillStack` |
| Td.3 | 격자 아웃라인 설계 머리말 한 줄(채우기 깊이 규칙, 원본 = 이 설계 OI1) |
| Td.4 | 설계 §7 구현 메모 |
| Td.5 | 묶음 끝 검토 에이전트 하나(7기준), 결과 보고 |

공유 대상:

- 민형님은 없습니다. Factory는 무변경이고, GlobalSystem은 읽기만 합니다.
- 현강님은 없습니다. UI 코드는 무변경입니다.
- 천명님은 Core 전달 문서 §5(Tb.5)입니다.

## 6. 계획에서 정한 것 (확인 대상)

| 번호 | 내용 |
|---|---|
| TP-1 | 채우기 쌓기는 새 부품 `FieldCellFillStack` 하나가 맡습니다. 렌더러는 "언제 채우나(행동 중)"만 정합니다. 렌더러(779줄)에 깊이 규칙까지 더하지 않으려는 것입니다. |
| TP-2 | 채우기 정렬 순서는 깊이마다 −1부터 하나씩 올리고, 테두리 시작을 16으로 올립니다. 종류는 열하나라 가장 깊은 채우기가 9입니다. |
| TP-3 | 깊이 1 이상 객체는 처음 필요할 때 만들어 해제까지 둡니다. |
| TP-4 | 비율은 렌더러를 만들 때 한 번 읽고 `CanReuse`가 비교합니다. |
| TP-5 | Core 결과는 끝 선택 인자로 더해 우리 `UnitAttackRangeQueryTests`를 고치지 않습니다. |
| TP-6 | 이동 칸 탐색은 `Range`와 같은 함수 하나를 씁니다. 조건 함수 `UnitMoveCells`는 이동 쪽 파일에 둡니다. |
| TP-7 | 층 비트 둘(14·15)로 ushort를 다 씁니다. 바탕 형식은 지금 넓히지 않습니다. |
| TP-8 | 호버 이동 색은 아군·적 모두 새 씬 칸입니다. 아군 호버 공격처럼 기존 색(이동 범위)을 재사용하지 않는 이유는, 이동 모드의 아군 이동 범위와 겹쳐 구분이 안 되기 때문입니다. |
| TP-9 | 이동 모드에서 지금 행위자에 호버하면 그 유닛 이동 범위가 이미 보이는 이동 범위 위에 한 겹 더(안쪽) 그려집니다. 특별 규칙은 두지 않습니다. |

## 7. 실행 기록

사용자 "OI-a 진행" → 진행 중 "EM-C 까지 진행하고 마지막 검토진행"(10-29). TP-1~TP-9 그대로. 모두 컴파일 미확인, 새 시험 없음, 시험 변경 없음.

### 7.1 OI-a 겹침 축소

- Ta.1·Ta.2: `StageFieldViewDataSO.cs` "Cell Outline" 묶음에 `overlapInsetScale`(기본 0.7, 인스펙터 범위 0.05~0.95)·읽기 `OverlapInsetScale`·검사(`ValidateGridOutline` 안, (0, 1)). 기본 에셋 `StageFieldView_Default.asset`에 0.7(이 칸을 가진 에셋은 이것 하나).
- Ta.3: `FieldSortingOrders.Fill(depth) = CellFill + depth`, `OutlineBase` 0 → 16. 옛 주석의 "이동 경로선(DrawPointLine) 정렬 순서 0" 줄은 경로가 불투명 부품으로 바뀌어 고침.
- Ta.4: `FieldCellFill` 생성자 끝 인자 `sortingOrder`.
- Ta.5·Ta.7: 새 `Display/Field/FieldCellFillStack.cs`(+ `.meta`). 종류(배치 구역 → 채우는 층 → 호버) × 깊이별 채우기, 깊이 0은 생성 때 만들고 구성은 `Build`, 깊이 1 이상은 `Apply` 안에서 처음 쓸 때 만들어 구성한 뒤 목록에 넣음, `Release`는 깊이 1 이상을 목록에서 뺌. 구성 때 "가장 깊은 채우기 < 테두리 시작" 검사. 비율 (0, 1) 인자 검사(시험이 직접 만든 설정용).
- Ta.6: 렌더러의 `CreateFills`·`HoverFill`·`ApplyFills`·`TryGetFill`·`FillColor`·`ApplyFill`·`FillSlot`·`PlayerZoneFill`·`_fillMaterial`·`_fillCenters` 삭제, `_fills`는 부품(머티리얼 없으면 null), `CanReuse`는 `_fills?.Uses(config) ?? config.HighlightFillMaterial == null`(비율도 비교, TP-4).
- 계획과 다른 점: 없음.

### 7.2 EM-a Core 이동 칸 (사용자 지시 Core 변경)

- Tb.1: `UnitAttackRangeResult.MoveCells`, `Success(…, moveCells = null)`·`Failure(…, moveCells = null)`. 우리 `UnitAttackRangeQueryTests`는 그대로 컴파일됨(TP-5).
- Tb.2·Tb.3: `CombatMovementCalculator.Reach`(private, `Range`와 공유, `Range` 결과 같음)·`UnitMoveCells`(internal).
- Tb.4: `CombatAttackQueries.UnitRange`가 플레이어 시점 지형으로 이동 칸을 먼저 구해 실패·성공 결과에 담음(P8).
- Tb.5: Core 전달 문서 §5.59(미전달).

### 7.3 EM-b 호버 이동 범위

- Tc.1: 층 `AllyHoverMoveRange = 1 << 14`·`EnemyMoveRange = 1 << 15`, 색 종류 `AllyHoverMoveRange = 17`·`EnemyMoveRange = 18`, 표 두 줄(끌어오기 범위 뒤·공격 범위 앞, 채움, 평소 안 그림).
- Tc.2·Tc.3: 팔레트 칸·생성자 인자 둘(`knockbackDestination` 뒤), `FieldView` 씬 칸 `enemyMoveRangeColor`·`allyHoverMoveRangeColor`(기본 = `moveRangeColor` 코드 기본값 (0.45, 0.85, 1, 1), "입력·미리보기 층 색" 묶음의 공격 범위와 이동 범위 사이). `Combat.unity`는 다음 저장 때 칸이 생김(씬 파일 손대지 않음).
- Tc.4~Tc.6: 계약 `ShowHoverRange(attackCells, moveCells, tone)`, 표시가 그린 층 둘을 기억해 바뀐 층만 먼저 지움, 위험 색조에 이동 칸이 오면 예외, 호버는 이동 칸(아군·적)·성공일 때 공격 칸을 넘기고 둘 다 비면 지움.
- 계획과 다른 점: 없음.

### 7.4 EM-c 문서·공유

- 호진님 필드 설정 표 메모 새 절(`handoffs/2026-10-19-attack-trajectory-field-view-hojin-share.md`, 미전달).
- 폴더 배치 안내 `Display/Field/` 줄, 격자 아웃라인 설계 머리말 한 줄, 호버 공격 범위 설계 머리말 한 줄, Core 전달 문서 §5.59.
- 민형님·현강님 공유는 없음(Factory·UI 무변경, GlobalSystem은 읽기만).

### 7.5 Unity 확인

Ua(§2)·Ub(§4) 목록 그대로. Ua2(§3)는 컴파일만.

### 7.6 묶음 끝 검토 (10-29, 새 에이전트 하나, 읽기 전용) — 약 9.75, 권장대로 반영(§7.7)

- 컴파일 위험은 없음(바뀐 서명마다 부르는 곳을 Assets 전체에서 대조, 시험 가짜 없음, `UnitAttackRangeQueryTests`는 그대로 컴파일).
- 정확성 문제도 없음: 깊이 상한 10 → 정렬 9 < 16, 테두리 16~34 < 궤적 100, 해제·다시 구성, 층 묶음 지우기, CellPrefab 모드, `Range` 결과 같음.
- 점수: SSOT 9.25 · SOLID 9.75 · FW 10 · 독립성 9.75 · 캡슐화 10 · C#/Unity 10 · If/Tick 9.5. 높음 0, 중간 0, 낮음 7.
- 낮음:
  - L1 그린 값 기억이 두 벌(`FieldCellFillStack.Depth`·렌더러 `Slot`).
  - L2 비율 조건 사본 셋, 경계도 다름(인스펙터 0.05~0.95 / 검사 (0, 1) / 부품 (0, 1)).
  - L3 `Reach` 주석 "유일한 탐색"이 틀림. 적 AI(`EnemyMovementPathfinder:24`)는 아는 지뢰 칸을 빼고 실제 지형으로 찾는다(코드로 확인). 적 호버 이동 칸은 규칙상 닿는 칸이라 AI가 안 가는 칸도 보인다.
  - L4 테두리 시작 16으로, 정렬 순서 0인 투명 물체(고스트·이펙트)보다 깊이 1 이상 채우기와 테두리가 늘 뒤에 그려진다.
  - L5 호버 답 하나에 다시 그리기 세 번(지우기·공격·이동).
  - L6 공격 조준 칸이 공격 범위 → 영향(공격 호버 색) → 호버(같은 색) 세 겹이 된다(코드로 확인, 예전엔 한 겹).
  - L7 `Apply` 인자 여섯이 매번 같다.
- 확인 사항(감점 없음):
  - 아군 호버는 이번 턴에 이미 움직인 유닛에도 이동 칸을 보인다(행동 사용을 안 봄).
  - `Combat.unity`의 이동 범위 색은 초록(0.11, 1, 0.20)인데, 새 두 칸은 씬에 값이 없어 코드 기본값 파랑으로 나온다(코드로 확인).

### 7.7 검토 권장 반영 (10-29, 사용자 "권장대로 반영", 컴파일 미확인, 새 시험 없음, 시험 변경 없음)

- L1: 새 `Display/Field/FieldDrawnCells.cs`(+ `.meta`) — 마지막으로 그린 칸 목록·색(`SameAs`·`Remember`·`Forget`, 빈 목록은 색과 상관없이 같음). 렌더러 `Slot`과 채우기 부품 `Depth`가 `Drawn`으로 함께 씀(두 사본 삭제).
- L2: 비율 범위 원본은 `StageFieldViewData`의 상수 둘(`MinOverlapInsetScale` 0.05·`MaxOverlapInsetScale` 0.95). 인스펙터 `Range`와 검사가 함께 씀(검사 경계가 (0, 1) → [0.05, 0.95]로 인스펙터와 같아짐). 부품 안의 검사는 뺌. 툴팁의 "0보다 크고 1보다 작아야" → "범위는 슬라이더와 같습니다". 기본 에셋 0.7은 범위 안이라 에셋 변경 없음.
- L3: Core `CombatMovementCalculator.Reach` 주석만 — "유일한 탐색" → "행동 범위와 유닛 이동 칸 조회가 함께 쓴다", 적 AI 길 찾기는 따로이고 유닛 이동 칸에는 적이 실제로 고르지 않는 칸이 있을 수 있다. Core 전달 문서 §5.59 덧붙임 한 줄(미전달). 동작 무변경.
- L4: `FieldSortingOrders` 머리 주석에 규칙 — 이 표에 없는 투명 물체(고스트·이펙트)는 정렬 순서 0, 깊이 1 채우기는 그와 거리로 순서, 깊이 2 이상 채우기·모든 테두리는 늘 그 뒤. 칸 표시 위에 보여야 하는 물체는 이 표보다 큰 값을 준다. 코드 무변경.
- L6 A: 채우기 부품이 칸을 쌓기 전에 종류별 색을 정하고(`_kindColors`), 호버 칸 색이 그 칸 바로 아래 채우기 색과 같으면 호버 깊이를 넣지 않음. 공격 조준 칸은 공격 범위 → 영향 두 겹(예전처럼 영향 색 한 겹이 위)이 됨. 다른 종류끼리 같은 색이어도 깊이는 그대로(호버만).
- L7: 채우기 부품 생성자는 `(cellsRoot, config, palette, meshes, colors)` — 머티리얼·비율·판 높이(`OutlineYOffset`)를 설정에서 읽음. `Build(mapper, state, spacing)`가 그릴 필드를 받고 `Apply(acting)` 하나만 남음, `Release`가 참조를 비움. 판 높이 규칙(칸 테두리 높이 = 레일 높이) 설명은 렌더러 `Refresh` 주석에서 부품 머리 주석으로 옮김.
- 그대로: L5(호버 답 하나에 다시 그리기 셋), 확인 사항 둘(아군 이미 움직인 유닛의 이동 칸, 씬 색 칸 둘은 사용자가 정함).
- 문서: 폴더 배치 안내 `Display/Field/` 줄에 `FieldDrawnCells`, 설계 §7 한 줄.
- 계획과 다른 점: 없음.
- 커밋: 새 `FieldDrawnCells.cs`(+ `.meta`)는 `FieldCellFillStack.cs`·`FieldOutlineRenderer.cs`와 한 커밋(없으면 두 파일이 컴파일되지 않음).

Ua 덧붙임(반영분):

9. 공격 모드에서 대상 칸에 호버하면 그 칸은 공격 범위 위에 영향 색 한 겹만 안쪽에 보입니다(같은 색 사각형이 두 번 줄어들지 않음).
10. 공격 범위 칸이 아닌 이동 범위·배치 구역 위 호버는 지금처럼 호버가 안쪽에 그려집니다.
11. 배치 드래그 고스트·이펙트 위에 칸 테두리·안쪽 채우기가 덧칠돼 보이는지 봅니다(L4). 거슬리면 그 물체의 정렬 순서를 정할 일입니다.
12. 인스펙터에서 `overlapInsetScale` 슬라이더 범위가 0.05~0.95로 그대로입니다.
