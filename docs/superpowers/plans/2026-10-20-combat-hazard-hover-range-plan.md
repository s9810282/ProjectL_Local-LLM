# 전투 오브젝트·장애물 호버 위험 범위 Task 계획 (2026-10-20)

상태: **HZ-a·HZ-b 코드 완료(2026-10-20, 사용자 "Phase 모두 다 차례대로 진행해"), MSBuild 오류 0·바꾼 파일 새 경고 0, Unity Test Runner·재생 확인 대기. 기록 §5.** 설계는 [유닛 호버 공격 범위 설계 §8](../specs/2026-10-19-combat-unit-hover-attack-range-design.md)(HZ1~HZ7). 결정 Q-H1~Q-H4 모두 A(2026-10-20 사용자, "Core 공개 입구는 내가 지시하는 거야"). 계획에서 정한 것 P1~P8(§0.2)은 위 지시로 쓴 그대로 정했다(P3 보라 포함).

## 0. 시작 전

### 0.1 결정과 착수 조건

| 항목 | 내용 |
|---|---|
| 결정 | Q-H1 A(Core 공개 입구 `CombatHazardAreas`, 사용자 지시 Core 변경) · Q-H2 A(새 층 `HazardRange` + `FieldView.hazardRangeColor`) · Q-H3 A(화약통 자신의 8칸만) · Q-H4 A(평가 L1·L2는 이번에 안 함) |
| 작업 트리 | 계획 작성 때 코드 변경 없음(FMOD 캐시·글꼴 에셋 변경은 에디터가 바꾼 것, 이 작업과 무관). 호버 U-1·U-2·적/아군 색은 커밋됨 |
| 다른 작업자 파일 | `CombatEnvironmentText.cs`는 현강님 코드(`63a1c23e` 등) → 공유 도우미 호출로 바꾸고 현강님 메모(T12). `ObjectViewSet`·`TerrainViewSet`은 사용자 작성·현강님 수정 → 같은 메모. Factory·서비스 키·DB 변경 없음(호버 생성자가 상태 Buffer를 이미 받음) |
| 검증 | 사용자 지시대로 전체 관리 실행은 하지 않는다. Phase마다 바뀐 파일 MSBuild 오류 0·새 경고 0(사용자가 건너뛰라고 하면 건너뜀), Unity Test Runner·재생은 사용자 |

### 0.2 계획에서 정한 것 (사용자 확인 대상)

| P | 정한 것 | 이유 |
|---|---|---|
| P1 | Core 입구는 **호출자 목록에 덧붙이는** 꼴: `MineCells(origin, boardWidth, boardHeight, ICollection<CellCoord> into)`·`PowderBarrelCells(anchor, boardWidth, boardHeight, ICollection<CellCoord> into)`. 비우지 않고 덧붙인다(한 칸 여러 원본의 합집합). 옛 internal 함수 둘은 새 목록을 만들어 입구를 부르는 한 줄 위임 | View 호버가 칸이 바뀔 때마다 할당하지 않게(입력 규칙 F10). 옛 함수의 반환 꼴·검사 순서·칸 순서는 그대로라 Core 호출처·시험 무수정 |
| P2 | 색조 `CombatRangeTone`(`Ally`·`Enemy`·`Hazard`)는 입력 쪽 새 열거형. 표시 계약 멤버를 `ShowHoverRange(cells, tone)`·`ClearHoverRange()`로 바꾼다(계약 이름 `ICombatUnitRangeFeedback`·클래스 이름 `CombatUnitRangeHover`는 그대로 — 설계 HZ4, 폴더 정리 때 이름 바꾸기 후보) | 서명이 어차피 바뀌고, 공격 모드의 `ShowRangeCells`와 이름이 헷갈리지 않게. 파일 이름 바꾸기는 `.meta`·Factory·시험이 따라 바뀌어 범위를 넓힌다 |
| P3 | 위험 범위 기본 색은 **보라** `(0.75, 0.45, 1, 1)`. 설계 Q-H2 표의 "기본 노랑"을 바꾼다 | 노랑 계열은 호버(`hoverColor`·`attackHoverColor` 연노랑)와 행위자(`activeActorColor`)가 이미 쓴다. 주황·빨강은 영향·적 범위가 쓴다. 씬에서 조정 가능 |
| P4 | "플레이어에게 알려진 원본" 판단의 사본 셋(`CombatEnvironmentText`·`ObjectViewSet`·`TerrainViewSet`)을 공유 도우미 `CombatEnvironmentFacts.IsKnownToPlayer` 하나로 바꾼다(동작 동일). 장애물 분류·프로필 찾기는 환경 설명과 찾기 규칙이 `TryGetObstacle`을 같이 쓴다 | 새 도우미만 두면 같은 규칙이 넷이 된다. 바꾸는 곳은 각 파일의 private 함수 하나씩이라 구조 정리 범위가 작다(Q-H4 A의 "구조 정리 분리"는 L1·L2 몫) |
| P5 | 찾기 규칙 `CombatHazardLookup.Collect`는 환경 설명 `Build`처럼 **사실 조각을 받는다**(환경·분류·구조물·장애물 프로필·보드 크기·칸·목록). 호버는 `CombatStateData`에서 조각을 꺼내 넘긴다 | 시험이 `CombatStateData` 없이 사실만 만들어 확인할 수 있다(환경 호버 시험 선례). 구조물 사실은 `CombatStateData`로 만들기 어렵다 |
| P6 | 지뢰 조건 = 원본 ID가 `ObjectSourceIds`에 있고 `MineSourceFact`이며 활성이고 플레이어에게 알려짐(오브젝트 표시·환경 설명과 같음). 화약통 조건 = 살아 있는 구조물이 칸을 점유하고 `ObstacleEntityIds`에 있으며 프로필이 `PowderBarrel`. 피해 칸은 구조물 **기준 칸(Anchor)** 둘레(Core `ResolveExplosions`가 Anchor를 씀 — 여러 칸 화약통의 다른 칸에 호버해도 같은 8칸) | Core 규칙을 그대로 따른다 |
| P7 | 같은 위험 칸에서 포인터만 움직이면 다시 찾지 않는다(그린 위험 칸 기억). 결과 재생 전 `Suspend`가 기억을 지우므로 터진 지뢰·부서진 화약통은 다시 집을 때 새 사실로 판단한다 | 포인터 콜백은 칸 비교만. 사실 변화는 결과 재생으로만 온다 |
| P8 | 새 강조 층 `HazardRange = 1 << 9`, 색 종류 `HazardRange = 15`, 그리기 순서·채우기 우선순위는 `EnemyAttackRange` 바로 위, 행동 중 층에 포함(유닛 호버 범위와 같음) | 호버 범위 셋은 함께 보이지 않으므로 서로의 순서는 의미가 없고, 유닛 호버 범위와 같은 자리에 둔다 |

### 0.3 바뀌는 파일 한눈에

| Phase | 새 파일(+ `.meta`) | 고치는 파일 |
|---|---|---|
| HZ-a (Core) | `Application/Combat/CombatHazardAreas.cs`, `Tests/EditMode/CombatHazardAreasTests.cs` | `MineMovementTileRule.cs`, `ObstacleReactionCalculator.cs`(옛 함수 위임) |
| HZ-b (View) | `ViewSystem/Combat/Display/CombatEnvironmentFacts.cs`, `ViewSystem/Combat/CombatHazardLookup.cs`, `ViewSystem/Combat/CombatRangeTone.cs`, `Tests/EditMode/Framework/CombatHazardLookupTests.cs` | `CombatEnvironmentText.cs`·`Display/ObjectViewSet.cs`·`Display/TerrainViewSet.cs`(P4), `ICombatUnitRangeFeedback.cs`, `CombatAttackFeedback.cs`, `CombatSceneDisplay.cs`, `CombatUnitRangeHover.cs`, `Display/FieldHighlightLayers.cs`, `Display/FieldHighlightState.cs`, `Display/FieldHighlightPalette.cs`, `Display/FieldView.cs`, `Display/FieldOutlineRenderer.cs` |
| HZ-b 기존 시험 | — | `CombatUnitRangeHoverTests.cs`(가짜 표시 서명·적 색 시험), `CombatViewSystemTests.cs`(`FakeDisplay` 두 멤버), `FieldOutlineRendererTests.cs`(팔레트 생성자·슬롯 수 11→12·22→24·`FillNames`), `CombatObjectiveDisplayTests.cs`·`CombatDisplayReuseTests.cs`(팔레트 생성자) |

## 1. Phase HZ-a — Core 공개 입구 (동작 동일)

### T1. `CombatHazardAreas` 만들기

- 위치: `CoreSystem/CoreCaculator/Application/Combat/CombatHazardAreas.cs`, 네임스페이스 `...CoreCaculator.Application.Combat`, `public static class`.
- `public static void MineCells(CellCoord origin, int boardWidth, int boardHeight, ICollection<CellCoord> into)` — 중심 포함 3×3, 보드 밖 제외, `dy`·`dx` 순서는 지금 `MineMovementTileRule.Cells`와 같다.
- `public static void PowderBarrelCells(CellCoord anchor, int boardWidth, int boardHeight, ICollection<CellCoord> into)` — 중심 제외 둘레 8칸, 순서는 지금 `BarrelCells`와 같다.
- 검사: 보드 크기 1 미만·원점이 보드 밖이면 `ArgumentOutOfRangeException`(지금과 같은 순서 — 너비·높이 → 원점), `into`가 null이면 `ArgumentNullException`. 목록은 비우지 않고 덧붙인다(P1). 요약 주석에 "두 규칙(지뢰 진입 피해·화약통 폭발)이 쓰는 유일한 피해 칸 모양이며 View 호버가 같이 부른다"를 적는다.
- `.meta`는 새 GUID.

### T2. 옛 함수 위임

- `MineMovementTileRule.Cells`: `var cells = new List<CellCoord>(9); CombatHazardAreas.MineCells(origin, width, height, cells); return cells.AsReadOnly();`
- `ObstacleReactionCalculator.BarrelCells`: 같은 꼴, 용량 8.
- 두 함수의 접근자·서명·반환 꼴은 그대로(호출처 `TargetIds`·`ResolveExplosions`와 시험 `MineEnvironmentTests`·`ObstacleReactionTests` 무수정).

### T3. Core 시험 `CombatHazardAreasTests` (`ProjectL.Tests.Core`)

| 사례 | 확인 |
|---|---|
| 지뢰 가운데·모서리·1×1 보드 | 9칸·4칸·1칸, 순서는 옛 함수와 같음 |
| 화약통 가운데·모서리·1×1 | 8칸(중심 없음)·3칸·0칸 |
| 덧붙이기 | 미리 든 칸을 지우지 않고 뒤에 붙임, 지뢰 둘을 같은 목록에 부르면 둘 다 들어감 |
| 잘못된 값 | 너비·높이 0, 원점 보드 밖 → `ArgumentOutOfRangeException`, 목록 null → `ArgumentNullException` |
| 옛 함수와 같음 | `MineMovementTileRule.Cells`·`ObstacleReactionCalculator.BarrelCells`가 입구와 같은 순서·같은 칸 |

### T4. 기록

- 데이터 경계 전달 문서(`handoffs/2026-09-24-view-core-data-boundary-handoff.md`) **§5.47** "환경 위험 피해 칸 공개 입구(2026-10-20, View 작업에서 사용자 지시로 구현, 미전달)": 무엇을(새 공개 정적 클래스, 옛 internal 두 함수는 위임), 왜(View 호버가 Core 규칙 사본 없이 같은 칸을 그림), 동작 변화 없음, 시험.

### HZ-a 확인

- MSBuild: Core 어셈블리·Core 시험 어셈블리 오류 0·바꾼 파일 새 경고 0.
- 사용자 Unity: `CombatHazardAreasTests`(새), `MineEnvironmentTests`, `ObstacleReactionTests`(바뀐 것 없음, 회귀).
- 화면 변화 없음. 여기서 멈추고 보고한다.

## 2. Phase HZ-b — View 찾기·표시·호버 연결

### T5. 공유 도우미 `CombatEnvironmentFacts` (P4)

- 위치: `ViewSystem/Combat/Display/CombatEnvironmentFacts.cs`, `internal static class`, 네임스페이스 `...ViewSystem.Combat.Display`.
- `internal static bool IsKnownToPlayer(MovementTileFact source)` — `KnownTo`에 `Owner.Player`가 있으면 true(지금 세 사본과 같은 식).
- `internal static bool TryGetObstacle(EnvironmentSemanticBindings bindings, IReadOnlyList<ObstacleContentProfile> obstacles, StructureCombatFacts structure, out ObstacleContentProfile profile)` — 구조물 ID가 `ObstacleEntityIds`에 있고 같은 ID의 프로필이 있으면 true(지금 `CombatEnvironmentText.Build`의 안쪽 두 반복과 같음). 할당 없음.
- 바꾸는 곳: `CombatEnvironmentText`(private `IsKnownToPlayer` 삭제·장애물 반복을 `TryGetObstacle`로), `ObjectViewSet`·`TerrainViewSet`(private `IsKnownToPlayer` 삭제). 동작 동일.

### T6. 찾기 규칙 `CombatHazardLookup` (P5·P6)

- 위치: `ViewSystem/Combat/CombatHazardLookup.cs`, `internal static class`.
- `internal static bool Collect(MovementEnvironmentFacts environment, EnvironmentSemanticBindings bindings, IReadOnlyList<StructureCombatFacts> structures, IReadOnlyList<ObstacleContentProfile> obstacles, int boardWidth, int boardHeight, CellCoord cell, List<CellCoord> into)`.
  - `into`를 비운 뒤, 칸의 지뢰마다 `CombatHazardAreas.MineCells`, 칸을 점유한 화약통마다 `CombatHazardAreas.PowderBarrelCells(structure.Anchor, …)`를 덧붙인다. 위험이 하나라도 있으면 true.
  - 분류와 형식이 다르면(`ObjectSourceIds`에 있는데 `ObjectSourceFact`가 아님) 환경 설명과 같이 `InvalidOperationException`.
  - 할당 없음(호출자 목록 재사용). 인자 null은 `ArgumentNullException`.
- 요약 주석: 무엇을 위험으로 보는지 표(설계 §8.1)는 설계가 원본이고 여기는 그 조건만 적는다.

### T7. 색조와 표시 계약 (P2)

- 새 `ViewSystem/Combat/CombatRangeTone.cs`: `internal enum CombatRangeTone { Ally = 1, Enemy, Hazard }`(0은 두지 않아 기본값으로 그리지 않게).
- `ICombatUnitRangeFeedback`: `ShowHoverRange(IReadOnlyList<CellCoord> cells, CombatRangeTone tone)`·`ClearHoverRange()`. 요약 주석을 "유닛·위험 호버 범위"로.
- `CombatAttackFeedback`: 색조 → 층(`Ally` → `AttackRange`, `Enemy` → `EnemyAttackRange`, `Hazard` → `HazardRange`, 그 밖 → `ArgumentOutOfRangeException`), 지금처럼 지난번 층이 다르면 먼저 지움. 필드 `_unitRangeLayer` → `_hoverRangeLayer`.
- `CombatSceneDisplay`: 위임 두 멤버 이름·서명(해제 뒤 무시 그대로).

### T8. 강조 층 `HazardRange` (Q-H2 A, P3·P8)

- `FieldHighlightLayers.cs`: `FieldHighlightLayer.HazardRange = 1 << 9`(요약: 호버한 지뢰·화약통의 피해 칸), `FieldHighlightColor.HazardRange = 15`, `DrawOrder`의 `EnemyAttackRange` 다음, `ColorOf` 갈래.
- `FieldHighlightState.ActingLayers`에 추가.
- `FieldHighlightPalette`: 생성자 인자 `hazardRange`를 `enemyAttackRange` 다음에, 속성 `HazardRange`, `TryGet` 갈래.
- `FieldView`: "입력·미리보기 층 색" 묶음의 `enemyAttackRangeColor` 다음에 `[Tooltip] hazardRangeColor = new(0.75f, 0.45f, 1f, 1f)`, 팔레트 생성에 넘김. 씬 값은 처음엔 코드 기본값(직렬화 칸이 새로 생김).
- `FieldOutlineRenderer`: `FillKind.HazardRange`(`EnemyAttackRange` 다음), `TryGetFillKind`에서 `EnemyAttackRange` 위 갈래, `FillColor` 갈래, 요약 주석의 우선순위 문장.

### T9. 호버 소유자 `CombatUnitRangeHover` (HZ4, P7)

- 필드: `_wantedEnemy` → `_wantedTone`(`Ally`·`Enemy`), 새 `_hazardCells`(재사용 목록), `_hazardCell`·`_showingHazard`(그린 위험 칸 기억).
- `Pick`: 정책·UI 가림·칸 집기 다음 —
  1. 유닛이 있으면 지금 길. 위험을 그리고 있었으면 먼저 `Release`.
  2. 유닛이 없고 `_showingHazard && cell == _hazardCell`이면 아무것도 하지 않음.
  3. 아니면 `_state.HasBattle`일 때 `CombatHazardLookup.Collect(state 조각…, cell, _hazardCells)`. 있으면 `Release`(유닛 요청·표시 내려놓기) 뒤 `ShowHoverRange(_hazardCells, Hazard)`, 기억. 없으면 `Release`.
- `Release`·`Hide`: 위험 기억도 지운다. `Poll`·`PreviewAnswered`·`TryRequest`는 유닛만(위험은 요청 없음).
- 요약 주석: 유닛 우선, 위험은 Core 공개 입구로 바로 그림, 슬롯을 쓰지 않음.

### T10. 시험

| 파일 | 사례 |
|---|---|
| 새 `CombatHazardLookupTests`(순수) | 알려진 지뢰 = 중심 포함 3×3 / 모르는 지뢰·비활성 지뢰·분류 밖 원본 = 없음 / 화약통 = 기준 칸 둘레 8칸, 여러 칸 화약통의 다른 칸도 같은 8칸 / 죽은 화약통·보급품·분류 밖 구조물 = 없음 / 같은 칸 지뢰 둘 = 둘 다 / 빈 칸 = false이고 목록이 비워짐 / 분류·형식 불일치 = 예외 |
| `CombatUnitRangeHoverTests`(고침 + 추가) | 가짜 표시를 새 서명으로, 적 색 시험은 색조 단언으로. 추가: 지뢰 칸 호버 = 요청 없이 바로 `Hazard`(미리보기 슬롯 그대로) / 같은 칸에서 움직이면 다시 그리지 않음 / 유닛 칸 → 지뢰 칸 = 유닛 요청 내려놓고 위험 / 지뢰 칸 → 빈 칸 = 지움 / `Suspend` 뒤 지뢰가 비활성이 된 사실로 다시 집으면 안 그림. 상태는 실제 Buffer의 다른 값은 그대로 두고 `Current`만 지뢰 상태로 바꾸는 시험 전용 상태 읽기(환경 호버 시험의 `State` 꼴) |
| `CombatAttackFeedbackTests`(추가) | 색조별 층, 색조를 바꾸면 앞 층을 지움, 지울 때 그린 층만 |
| `FieldOutlineRendererTests`(고침 + 추가) | 팔레트 생성자, 슬롯 수 11 → 12·22 → 24, `FillNames`에 `FieldFill_HazardRange`. 추가: 위험 범위 채우기·테두리 색(적 공격 범위 시험 꼴) |
| `CombatObjectiveDisplayTests`·`CombatDisplayReuseTests` | 팔레트 생성자 인자 하나 |
| `CombatViewSystemTests.FakeDisplay` | 두 멤버 이름·서명 |
| `CombatEnvironmentHoverTests`·`ObjectViewSetTests`·`TerrainViewSetTests` | 무수정(P4 동작 동일 확인용 회귀) |

### T11. 문서

- 설계 §8에 실행 기록 링크, `viewDocs/stage-input-guide.md` §2.2 7번에 "지뢰·화약통 칸은 피해 칸(FieldView Hazard Range Color)" 한 줄.
- 폴더 정리 계획 §3 대응표: `Input/Interaction/`에 `CombatHazardLookup`·`CombatRangeTone`, `Display/` 해당 행에 `CombatEnvironmentFacts`(폴더 정리 결정 때 확인).

### T12. 공유 메모

- 현강님 새 메모 `handoffs/2026-10-20-environment-facts-helper-hyeongang-share.md`(미전달): `CombatEnvironmentText`·`ObjectViewSet`·`TerrainViewSet`의 공개 판단이 `CombatEnvironmentFacts`로 옮겨졌고 장애물 프로필 찾기가 `TryGetObstacle`을 쓴다(문구·동작 같음), 호버 범위 색 칸 하나(`FieldView.hazardRangeColor`).
- 민형님 Factory 공유는 없음(Factory 무변경).

### HZ-b 확인

- MSBuild: View(`ProjectL.Systems`)·Framework 시험 어셈블리 오류 0·바꾼 파일 새 경고 0.
- 사용자 Unity Test Runner: T10 표의 파일 전부(회귀 셋 포함).
- 사용자 재생: 지뢰·화약통이 있는 스테이지에서 (1) 지뢰 칸 호버 → 보라 3×3, (2) 화약통 호버 → 둘레 8칸, (3) 보급품·깃발·배지·빈 칸 → 없음, (4) 유닛 호버는 지금처럼(아군 파랑·적 주황빛 빨강), (5) Drag·공격 모드·결과 재생 중 숨고 끝나면 다시, (6) 지뢰가 터진 뒤 그 칸 호버 → 없음, (7) 환경 설명 글은 지금과 같음.
- 여기서 멈추고 보고한다. 보고 끝에 원칙 7기준 자체 점검(설계 §8.3 예상 8.3과 비교).

## 3. 위험·주의

| # | 내용 | 대응 |
|---|---|---|
| W1 | `FieldView`에 직렬화 칸이 새로 생겨 `Combat.unity`가 다음 저장 때 바뀐다 | 커밋 때 씬 변경을 같이 확인(값은 기본 보라) |
| W2 | 팔레트 생성자 인자 순서 실수(Color 여러 개가 같은 형식) | 생성 세 곳이 모두 위치 인자라, 순서는 시험 단언(위험 범위 채우기·테두리 색 시험)으로 잡는다 |
| W3 | Core 파일 변경 | 사용자 지시(2026-10-20), 데이터 경계 §5.47. 옛 함수 위임이라 Core 동작 같음 |
| W4 | 지뢰가 플레이어에게 안 알려졌는데 범위가 보이면 정보 누설 | P6 조건 + 찾기 시험(모르는 지뢰 = 없음) |

## 4. 커밋 묶음 (사용자)

- HZ-a: `CombatHazardAreas.cs`·`CombatHazardAreasTests.cs`(+ `.meta`), `MineMovementTileRule.cs`, `ObstacleReactionCalculator.cs`, 데이터 경계 문서.
- HZ-b: 새 파일 넷(+ `.meta`), 고친 코드·시험, `Combat.unity`(새 색 칸이 저장됐으면), 문서·메모.
- FMOD 캐시·글꼴 에셋 변경은 이 작업이 아니다.

## 5. 실행 기록

### 5.1 HZ-a·HZ-b (2026-10-20)

사용자 지시 "Phase 모두 다 차례대로 진행해"로 HZ-a → HZ-b를 이어서 했다(P1~P8은 쓴 그대로, Phase 사이에 멈추지 않음). 기준 HEAD `eb797d9f`, 작업 시작 때 이 작업과 겹치는 커밋 전 변경 없음.

**HZ-a (Core, 사용자 지시 Core 변경 — 데이터 경계 §5.47):** 새 `Application/Combat/CombatHazardAreas.cs`(`MineCells`·`PowderBarrelCells`, 목록에 덧붙임, 검사 너비 → 높이 → 원점 → 목록), `MineMovementTileRule.Cells`·`ObstacleReactionCalculator.BarrelCells`는 새 목록을 만들어 입구를 부르는 위임. 새 시험 `Tests/EditMode/CombatHazardAreasTests.cs` 5개(모양 둘·덧붙이기·잘못된 값·옛 함수와 같은 칸·순서). 기존 Core 시험 무수정.

**HZ-b (View):**

| Task | 결과 |
|---|---|
| T5 | 새 `Display/CombatEnvironmentFacts.cs`(`IsKnownToPlayer`·`TryGetObstacle`, 인덱스 반복이라 할당 없음). 사본 셋(`CombatEnvironmentText`·`ObjectViewSet`·`TerrainViewSet`)의 private 함수를 지우고 도우미 호출, 환경 설명의 장애물 반복 두 개는 `TryGetObstacle` 한 줄 |
| T6 | 새 `CombatHazardLookup.cs`(`Collect`, 분류·형식 불일치는 환경 설명과 같은 예외) |
| T7 | 새 `CombatRangeTone.cs`(`Ally = 1`·`Enemy`·`Hazard`), 계약 `ShowHoverRange`·`ClearHoverRange`, `CombatAttackFeedback.LayerOf`(그 밖 색조는 예외)·`_hoverRangeLayer`, `CombatSceneDisplay` 위임 |
| T8 | `HazardRange = 1 << 9`·색 종류 15·그리기 순서는 적 범위 다음·행동 중 층·팔레트 인자(적 범위 다음)·`FieldView.hazardRangeColor`·아웃라인 채우기 종류(적 범위 다음, 채우기 우선은 적 범위 위) |
| T9 | `CombatUnitRangeHover`: `_wantedEnemy` → `_wantedTone`, `PickHazard`(같은 칸이면 그대로 → 아니면 `Release` 뒤 찾기·그리기), `Release`가 위험 기억도 지움 |
| T10 | 아래 시험 표 |
| T11·T12 | 설계 §8 결정 기록, 입력 안내 §2.2 7번, 폴더 정리 대응표 두 줄, 현강님 메모 `handoffs/2026-10-20-environment-facts-helper-hyeongang-share.md`(미전달), 민형님 공유 없음(Factory 무변경) |

**계획과 다른 점(작은 것, 모두 동작에 영향 없음):**

1. Core 입구 인자 이름은 `origin, width, height`(계획은 `anchor`·`boardWidth`). 옛 함수와 예외 인자 이름(`ParamName`)을 똑같이 두려고.
2. `FieldView.hazardRangeColor`는 인스펙터에서 `enemyAttackRangeColor` **위**에 뒀다(계획은 "다음"). 그 묶음 머리말이 "위 칸이 이김"이고 위험 범위가 그리기 순서상 적 범위 위라서.
3. 같은 칸 기억(P7)을 "위험이 없는 빈 칸"까지 넓혔다(`_hazardChecked`). 빈 칸 위에서 포인터만 움직일 때도 다시 찾지 않는다.
4. MSBuild는 Phase마다가 아니라 두 Phase를 마친 뒤 한 번 돌렸다(Phase를 이어서 하라는 지시).
5. `TryGetObstacle`은 장애물 목록이 null이면 분류와 상관없이 `ArgumentNullException`(옛 환경 설명은 분류된 구조물이 있을 때만 NullReference). Buffer의 목록은 늘 비어 있지 않은 배열이라 실제 경로 변화는 없다.

**시험:**

| 파일 | 새로 | 고침 |
|---|---|---|
| `CombatHazardAreasTests`(Core, 새) | 5 | — |
| `CombatHazardLookupTests`(Framework, 새, 순수) | 8개 메서드(사례 13) — 알려진 지뢰, 지뢰 안 보이는 넷(모름·비활성·분류 밖·다른 칸), 깃발, 같은 칸 지뢰 둘, 화약통 기준 칸 둘레(여러 칸 화약통 두 칸 모두), 구조물 안 보이는 셋(죽음·분류 밖·보급품), 분류·형식 불일치 예외, null 인자 | — |
| `CombatUnitRangeHoverTests` | 3 — 지뢰 칸 즉시 위험 색·슬롯 그대로·같은 칸 다시 안 그림 / 유닛 → 지뢰 → 빈 칸 → 유닛 / 내려놓은 뒤 비활성 지뢰는 안 그림 | 가짜 표시 서명, 적 색 시험을 색조 단언으로, Kit 상태 읽기를 `StateOverride`(실제 Buffer + `Current`만 바꿈)로 |
| `CombatAttackFeedbackTests` | 1(사례 2, CellPrefab·GridOutline) — 색조별 층, 바꾸면 앞 층 지움, 지울 때 그린 층만, 기본값 색조 예외 | — |
| `FieldOutlineRendererTests` | 1 — 위험 범위 채우기·테두리 색 | 팔레트 생성자, 슬롯 수 11 → 12·22 → 24, `FillNames` |
| `CombatObjectiveDisplayTests`·`CombatDisplayReuseTests` | — | 팔레트 생성자 인자 하나 |
| `CombatViewSystemTests.FakeDisplay` | — | 두 멤버 이름·서명 |

**검증:** VS 2022 MSBuild로 `ProjectL.Tests.Framework`(View·Core 포함)·`ProjectL.Tests.Core` 빌드, 새 파일은 스크래치 targets로 넣음. 오류 0, 바꾼 파일 새 경고 0(`CombatViewSystemTests.cs:1713` CS0649 하나는 원래 있던 것). 산출 DLL에 새 형식 넷·새 시험 둘이 들어간 것 확인. 사용자 지시대로 관리 실행·Unity 시험은 돌리지 않았다.

**사용자 확인 대기:** Unity 컴파일·Test Runner(위 표의 파일 + 회귀 `MineEnvironmentTests`·`ObstacleReactionTests`·`CombatEnvironmentHoverTests`·`ObjectViewSetTests`·`TerrainViewSetTests`), 재생 확인 §2 HZ-b 확인 (1)~(7). `Combat.unity`는 다음 저장 때 `hazardRangeColor` 칸이 들어간다.

**원칙 7기준 자체 점검(설계 §8.3 예상 8.3, 4기준):**

| 기준 | 점수 | 근거 |
|---|---|---|
| SSOT | 9 | 피해 칸 모양 Core 한 곳(옛 두 함수는 위임), "알려진 원본" 사본 셋 → 하나, 색조 → 층 대응은 `LayerOf` 한 곳 |
| SOLID | 7 | 호버 소유자가 출처 둘을 고름(찾기 규칙은 따로). 이름 `CombatUnitRangeHover`·`ICombatUnitRangeFeedback`이 이제 위험도 맡아 뜻이 좁다(낮음, 폴더 정리 때 이름 바꾸기 후보) |
| FW 지침·계약 | 9 | 서비스·키·Factory 무변경, Core 입구는 순수 정적 함수, Core 변경은 사용자 지시·기록 |
| 변경 독립성 | 8 | 위험 종류 추가 = Core 입구 함수 + 찾기 한 갈래. 층 추가는 여전히 층 순서·채우기 순서 두 표(L3) |
| 캡슐화 | 8 | 호버 상태는 모두 private, 시험은 공개 멤버와 표시 기록만 봄 |
| C#/Unity 권장 구조 | 8 | 포인터 길에 할당 없음(목록 재사용, 인덱스 반복). 칸이 바뀔 때마다 환경 원본·구조물을 한 번씩 훑음(작은 n) |
| 무의미한 If/Tick | 8 | 새 Tick 없음, 같은 칸 비교 한 번으로 끝냄. 위험 쪽 `Poll`은 할 일이 없어 그대로 지나감 |

평균 약 8.1/10. 남는 것: L1·L2(설계 §7.6, Q-H4 A로 이번 범위 밖), 이름 좁음(위 SOLID 줄).

**커밋 묶음(사용자):** §4 그대로. 새 파일 여섯(+ `.meta`): `CombatHazardAreas.cs`, `CombatHazardAreasTests.cs`, `CombatHazardLookup.cs`, `CombatRangeTone.cs`, `Display/CombatEnvironmentFacts.cs`, `CombatHazardLookupTests.cs`. Core 두 파일과 데이터 경계 문서는 HZ-a, 나머지는 HZ-b로 나눌 수 있고 한 커밋도 된다(HZ-b가 HZ-a 입구를 부르므로 HZ-b만 따로는 안 됨).
