# 전투 View 아군 배치 구역 — Core `PlayerZone`으로 옮기기 설계 초안

- 작성: 2026-10-28, View 작업
- 상태: **결정 완료(2026-10-28 사용자).** Q-PV4는 C("레벨 툴은 그냥 두고")이고, Q-PV1·Q-PV2·Q-PV3·Q-PV5는 A, P1~P7은 그대로다("권장대로"). 코드·에셋은 아직 바꾸지 않았다. Task 계획은 [player-zone-view-plan](../plans/2026-10-28-combat-player-zone-view-plan.md)이다.
- 요청(사용자): Core 배치 구역 변경을 확인한 뒤 "설계 써줘". View 구역 표시를 Buffer `PlayerZone`으로 옮긴다.
- 바탕: 천명님 커밋 `5a1bfbe1`(2026-10-27).
  - 규칙 원본: [아군 배치 구역 설계](2026-10-27-combat-player-deployment-zone-design.md) §2. 계약은 PZ1~PZ6이고, 우리 몫은 PZ7이다.
  - Core 응답: [Core 전달 문서 §43](../handoffs/2026-09-24-view-core-data-boundary-handoff.md#deployment-zone-cells-request).
- 대신하는 것: 우리 [배치 구역 칸 Core 요청](../handoffs/2026-10-27-combat-deployment-zone-cells-core-request.md)은 Core가 다른 결정(적 구역 없음, 아군 칸 집합 하나)으로 구현해서 끝났다. 이 설계는 그 결과에 View·개발 진입·편집 도구(선택)를 맞춘다.

## 1. 지금 구조 (코드로 확인한 사실, HEAD `5fe2ce1b`)

### 1.1 Core가 주는 것

- 값은 `CombatStateData.PlayerZone` 하나다. 타입은 `PlayerDeploymentZone`(어셈블리 `ProjectL.Core.Contracts`, 순수 C#)이다.
  - 판정 재료는 `Contains(cell)`이다.
  - 모양은 둘이다. 줄 구역 `Rows(n)`(y < n)과 칸 구역 `Of(칸)`이다.
- 옛 속성은 전환 입구로 남아 있다(PZ6). `PlayerDeploymentDepth`는 줄 수이고, 칸 구역이면 0이다. `EnemyDeploymentDepth`는 늘 0이다. View가 옮기면 천명님이 지운다(PZ-d).
- 구역은 전투 준비 때 정해지고 전투 동안 바뀌지 않는다. `CombatStateData`의 두 생성자(`:55`·`:70`)가 그대로 옮긴다.
- 규칙: 아군은 구역 안에만 놓인다. 적은 구역 밖이면 어디든 놓인다. 적 구역은 없다.
- 입력 자리: 스테이지 SO `CombatStageData.playerDeploymentCells`. 비면 필드의 아군 줄 수를 쓴다. 대체 규칙은 `BattleDefinitionAdapter` 한 곳에 있다.

### 1.2 View 표시 흐름

- `CombatSceneDisplay.cs:199`가 설치 때 한 번 `_field.ApplyStaticHighlights(state.PlayerDeploymentDepth, state.EnemyDeploymentDepth, state.RailCells)`를 부른다.
- `FieldView.ApplyStaticHighlights`(`:162`)는 `FieldHighlightState.SetStaticZones`(`:128-159`)를 부른다. 판정 순서는 이렇다.
  1. 레일이면 `Rail`
  2. `y < 아군 줄 수`면 `PlayerZone`
  3. `y >= 세로 − 적 줄 수`면 `EnemyZone`
  4. 그 밖은 `None`
- 2·3은 **Core 줄 규칙의 View 사본**이다.
- 칠하는 곳:
  - GridOutline 모드(지금 운영 값, `StageFieldView_Default.asset:16` = 1)
    - 배치 드래그 중에만 아군 구역 테두리(`FieldOutlineRenderer.cs:408-417`)와 채우기(`:564`)를 그린다.
    - 적 구역은 원래 그리지 않는다.
  - CellPrefab 모드: 정적 구역 색(아군·적·레일)을 칸 색으로 칠한다(`FieldCellColorRenderer.cs:158-160`).
- 색: `FieldView` 씬 값 `playerZoneColor`·`enemyZoneColor`·`railColor`(`:36-43`) → `FieldHighlightPalette`(`:12-13`·`:111-120`).
- 구역을 쓰는 다른 View 코드는 없다. 놓을 수 있는지는 Core 미리보기 응답이 정한다.

### 1.3 Core 변경 뒤 지금 화면

| 상황 | 화면 |
|---|---|
| 지금 스테이지(칸 목록이 비어 아래 N줄) | 아군 표시는 맞다. 적 구역 색은 사라졌다(CellPrefab 모드만 해당, GridOutline은 원래 없음) |
| 칸 구역 스테이지(`playerDeploymentCells`를 채움) | `PlayerDeploymentDepth`가 0이라 **아군 구역 테두리·채우기가 나오지 않는다.** Core는 칸대로 받아서 "보이는 구역"과 "놓이는 구역"이 어긋난다 |

지금 운영 스테이지는 모두 칸 목록이 비어 있어 아직 드러나지 않았다.

### 1.4 개발 진입(Fixture)

- `DevBattleInitializationSource.cs:37-61`: 기본 8×7, 아군·적 줄 수 2/2.
  - 검사: 둘 다 1 이상, 합 < 세로, 가로 × 적 줄 수 ≥ 2.
- `:115-127`:
  - 데몬은 `((w−1)/2, h − 적 줄 수)`에 놓는다.
  - 본부는 `적 줄 수 > 1 ? (w/2, h−1) : ((데몬.x+1)%w, h−1)`에 놓는다.
  - 옛 `StageConfig` 줄 수 생성자를 쓴다.
- 설정 자리 넷:
  - `GameBootstrap.cs:121-122`(씬 값), `:436-437`(생성), `:652-658`(덮어쓰기 적용·로그)
  - `GameBootstrapEditorOverrides.cs:207-217`(EditorPrefs)
  - `Assets/ProjectL/Editor/Boot/PlayFromBootSettingsWindow.cs:84-85·127-128·151-152·197-198`
- 적 줄 수는 이제 Core 규칙에 쓰이지 않는다. 데몬·본부 위치 계산에만 쓰인다.

### 1.5 SO 모드 편집 도구(`Tool/Editor`, 호진님 영역)

- 구역 색·이름은 줄 규칙 사본이다. 판정은 아군 / 적 / 중립이다(`CombatLayoutWindow.SoMode.cs:2066-2070`).
  - 같은 함수를 2D·3D 덧그리기(`:754`·`:1014`)와 선택 칸 정보(`:1486`)가 쓴다.
- 진영 줄 수 슬라이더(아군·적)가 있다(`:1733-1740`). 필드가 공유면 전용 복제 뒤 바꾼다.
  - "합 < 세로" 사본: `CombatStageSoField.MaxDepthBeside`, `EnemyStartRow`(`CombatStageSoAssets.cs:812-817`).
- 경고(`:1743-1749`) "적 진영 밖 고정 적 → Core가 멈춤"은 **지금 규칙과 반대다.**
  - 위 2줄 밖의 적은 이제 정상이다.
  - Core가 멈추는 것은 아군 구역 안의 적이다(천명님 Q-PZ3 A).
- `playerDeploymentCells`를 읽고 쓰는 곳이 없다. 칸을 넣는 방법은 기본 인스펙터의 `CellInput` 배열뿐이다.
- asmdef가 `ProjectL.Core.Contracts`·`ProjectL.Core.Domain`을 참조한다. 그래서 `PlayerDeploymentZone`과 `DeploymentZonePolicy`를 직접 부를 수 있다.
- 새 필드 만들기(`:1955-1970`, `CombatStageSoCreation.cs:124`)도 적 줄 수를 받는다. `FieldData.Validate`가 1 이상을 요구하고, Core는 읽지 않는다.
- DT 모드·엑셀(`CombatLayoutWindow.cs:165-166`, `CombatLayoutDocument.cs:134`, `CombatLayoutWorkbook.cs:16`)은 민형님 영역이라 이 설계 밖이다. 도구들은 Core 속성을 읽지 않고 자기 값과 필드 SO 이름만 쓴다.

### 1.6 시험

- 구역을 쓰는 View 시험은 남아 있지 않다(Assets/Tests 삭제분).
- 천명님 `CategoryDefinitionMigrationTests.cs:36`이 `new DevBattleInitializationSource()`를 기본 인자로 부른다. PV4 뒤에도 그대로 컴파일된다.
- 옛 깊이 속성을 쓰는 시험 10줄은 천명님 Core 시험이다(PZ-d 몫).

## 2. 규칙 (원본은 천명님 설계 §2, View는 따른다)

- 아군 배치 구역 칸 = `CombatStateData.PlayerZone.Contains(cell)`가 참인 칸이다. View는 줄 수나 칸 목록을 해석하지 않는다.
- 적 배치 구역은 없다. View는 적 구역을 표시하지 않는다.
- 정적 구역 우선순위: 레일 > 아군 구역 > 없음. 지금과 같고 적 구역만 빠진다.
- 표시 시점은 그대로다. GridOutline은 배치 드래그 중에만, CellPrefab은 늘 보인다.

## 3. 설계

### PV1 정적 구역 판정은 Core 값에 묻는다

- `FieldHighlightState.SetStaticZones(PlayerDeploymentZone playerZone, IReadOnlyList<CellCoord> railCells)`로 바꾼다.
  - 칸마다 `playerZone.Contains(cell)`로 판정한다.
  - 줄 비교 사본(`:146-147`)은 지운다.
- `FieldView.ApplyStaticHighlights(PlayerDeploymentZone playerZone, IReadOnlyList<CellCoord> railCells)`로 바꾼다. null이면 `ArgumentNullException`이다.
- 상태 클래스는 Unity 타입을 쓰지 않는 규칙을 지킨다. `PlayerDeploymentZone`은 순수 C#이고, 같은 파일이 이미 같은 어셈블리의 `CellCoord`를 쓴다.
- 판 밖 칸은 Core가 이미 거절한다(`CombatPreparation`의 `FitsBoard`). View는 판 안 칸만 돈다.

### PV2 적 구역 표시 걷기

- `FieldStaticZone.EnemyZone`을 지운다. `Rail = 3`은 그대로 둔다(P6, 직렬화되지 않는 internal enum이라 바꿀 이유가 없다).
- `FieldHighlightPalette`에서 지운다: 생성자 인자 `enemyZone`, 속성 `EnemyZone`, `TryGetZone`의 갈래.
- `FieldView.enemyZoneColor`와 `CreatePalette`의 인자를 지운다.
  - `Combat.unity`에 남는 직렬화 값은 다음 씬 저장 때 Unity가 버린다. Missing Script는 생기지 않는다.
- `playerZoneColor` 툴팁은 뜻이 같아 그대로 둔다.

### PV3 표시 입구

- `CombatSceneDisplay.cs:199`를 `_field.ApplyStaticHighlights(state.PlayerZone, state.RailCells)`로 바꾼다.
- 구역이 전투 중 바뀌지 않으므로 "설치 때 한 번"은 그대로다. Tick이나 Refresh를 더하지 않는다.
- 이 뒤로 View는 옛 깊이 속성을 읽지 않는다.

### PV4 개발 진입 Fixture

> **2026-10-28 사용자 지시로 Play From Boot의 Fixture 모드 자체를 지웠다(계획 §7.3 PV-b2).** 개발 진입은 Database 정의 하나다. 아래의 적 줄 수 걷기는 PV-b에서 했고, Fixture 필드 설정은 PV-b2에서 함께 사라졌다. Fixture 클래스는 천명님 시험 `CategoryDefinitionMigrationTests`용으로만 남는다(사용자 결정).

- `StageConfig` 새 생성자에 `PlayerDeploymentZone.Rows(아군 줄 수)`를 넘긴다.
- 적 줄 수 설정을 걷는다(Q-PV3). 지우는 것:
  - 생성자 인자와 상수 `DefaultEnemyDeploymentDepth`
  - `GameBootstrap` 씬 값, 덮어쓰기 적용, 로그
  - `GameBootstrapEditorOverrides.DevEnemyDeploymentDepth`
  - 설정 창의 칸과 검사
- 데몬·본부 위치(P1): 지역 값 `top = Math.Min(2, h − 아군 줄 수)`(아군 구역 위쪽 남은 줄, 최대 2)로 옛 식을 그대로 쓴다.
  - 기본 8×7·아군 2에서는 `top` = 2라 자리가 지금과 같다(데몬 (3,5), 본부 (4,6)).
- 검사: 가로·세로·아군 줄 수 ≥ 1, 아군 줄 수 < 세로, 가로 × (세로 − 아군 줄 수) ≥ 2.
  - 설정 창의 빨간 표시도 같은 식을 쓴다. 사본이므로 원본(Fixture 생성자) 자리를 주석으로 단다.
- 남는 직렬화 값(`Boot.unity`의 `devEnemyDeploymentDepth`)과 EditorPrefs 키는 쓰지 않고 그대로 둔다(P2).
- Database 정의(운영 DB 경로)는 바꿀 것이 없다. 어댑터가 이미 구역을 만든다.

### PV5 SO 모드 편집 도구 (Q-PV4 C로 이번 범위에서 뺌)

**2026-10-28 사용자 "레벨 툴은 그냥 두고".** 도구는 고치지 않는다. 아래는 PV-d에서 호진님께 넘길 할 일 목록이다. 칸 입력 화면은 PV7(Q-PV5)에서 따로 정한다.

목표: 기획자가 맵 위에서 아군 배치 칸을 드래그로 칠한다(처음 요청). 지금 도구의 틀린 구역 표시와 경고도 바로잡는다.

- **구역 읽기는 한 곳이다.** 작업본에 칸 목록이 있으면 `PlayerDeploymentZone.Of(칸)`, 비면 `Rows(필드 아군 줄 수)`다.
  - 이 대체 규칙은 `BattleDefinitionAdapter`의 사본이다. 도구는 Systems를 참조하지 않아서다. 원본 자리를 주석으로 단다.
  - 판정은 Core 값의 `Contains`를 부르므로, 줄 규칙 사본은 없어진다.
- **작업본**: `CombatStageSoDocument`에 `playerZoneCells`(`List<Vector2Int>`)를 더한다.
  - 읽기·쓰기는 `railCells`와 같은 길이다(`CombatStageSoAssets.cs:561`·`:655`). SO 필드 이름은 `playerDeploymentCells`다.
  - 놓인 물건이 아니라 칸 표시라서 `Placements()`와 겹침 검사에 끼지 않는다.
- **맵 색·이름**: "아군 배치 구역" / 나머지(지금 중립 색, 이름 "적·중립")다. 적 구역 색은 지운다. 2D·3D 덧그리기와 선택 칸 정보가 같은 함수를 쓴다.
- **"아군 구역" 붓**: 칸 칠하기·지우기, Shift+끌기 사각형(지금 칠하기 도구와 같은 길), Undo 한 번.
  - 칸 목록이 빈 채로 처음 칠하면, 지금 보이는 줄 구역 칸을 칸 목록으로 펼친 뒤 칠한다(P3). 보이는 구역을 그대로 이어서 고치게 하려는 것이다.
  - "줄 구역으로 되돌리기" 버튼은 칸 목록을 비운다(P3).
- **진영 줄 수**: 적 슬라이더를 지운다.
  - 아군 슬라이더는 "칸 목록이 비었을 때 쓰는 값"으로 남기고, 칸 목록이 있으면 끈다(P4).
  - 최댓값은 세로 − 1이다. `MaxDepthBeside`·`EnemyStartRow`는 지운다.
- **경고 교체**(저장은 막지 않는다, P5 — SO 모드 계획 P1과 같음):
  - 아군 구역 안에 고정 적 유닛·적 구조물·본부가 있으면 "전투 진입 때 Core가 멈춥니다". 판정은 `DeploymentZonePolicy.CanDeploy(Owner.Enemy, 칸)`을 직접 부른다.
  - 아군 구역 밖 칸이 하나도 없으면 경고한다. 적 출현과 재배치가 불가능해진다.
  - 중복 칸과 판 밖 칸은 저장 검사(복제본 `GetData` → `CombatStageData` 저작 검사)가 이미 잡는다.
- **새 필드 만들기**: 적 줄 수 칸을 빼고 1로 쓴다. 필드 검사(1 이상)만 만족하면 되고, Core는 읽지 않는다.
- 범위 밖: 좌우 뒤집기·대칭 복사(SO-g)에 구역을 넣는 것, DT 모드·엑셀.

### PV7 칸 입력 인스펙터 (Q-PV5로 정함)

처음 요청한 "Column×Row 사각형을 드래그로 칠하기"를 SO 인스펙터에 둔다. 다만 칠하는 SO는 `FieldDataSO`가 아니라 `CombatStageData`다.

- **`CombatStageData`인 이유:** 천명님 결정 ②A로 칸 목록이 스테이지 SO에 있다(`playerDeploymentCells`).
  - 필드 SO는 여러 스테이지가 함께 쓴다. 지금도 `Combat_Normal_B`·`Combat_Normal_C`가 `Field_13x13_P3E9` 하나를 쓴다.
  - 필드에 칸을 두면 두 스테이지 구역이 함께 바뀐다.
  - `FieldDataSO`에 두려면 DB 입력 자리와 어댑터 대체 규칙(Core 쪽 `BattleDefinitionAdapter`)을 바꿔야 한다. 그래서 이 설계에서는 하지 않는다.
- **자리:** 이미 있는 커스텀 인스펙터 `Systems/DataBaseSystem/Editor/CombatStageDataEditor.cs`(우리 파일, 10-12)에 "아군 배치 구역" 묶음을 더한다. 새 파일은 없다.
  - `playerDeploymentCells` 기본 배열 표시는 그 묶음으로 바꾼다(`DrawPropertiesExcluding` 목록에 더함).
- **격자 크기:** 연결된 `fieldDefinition`의 가로·세로에서 읽는다. 필드가 없으면 격자 대신 안내 문구만 보인다.
- **칠하기:**
  - 칸 사각형을 클릭하거나 끌면 칠해지고, 칠한 칸에서 시작해 끌면 지워진다.
  - 한 번 끌기가 Undo 한 번이다.
  - 위가 큰 y로 그린다(맵과 같은 방향).
- **보이는 구역:** 칸 목록이 비었으면 필드 아군 줄 수의 줄 구역을 옅게 그리고 "필드 줄 수 사용 중"이라고 적는다.
  - 처음 칠하면 그 줄 구역을 칸 목록으로 펼친 뒤 칠한다(P3).
  - "줄 구역으로 되돌리기" 버튼은 칸 목록을 비운다(P3).
- **판정·검사:** 구역은 `PlayerDeploymentZone.Rows/Of`로 만들고 `Contains`로 칠한다. 대체 규칙(비면 줄 수)은 어댑터 사본이라 원본 자리를 주석으로 단다. 중복·판 밖 검사는 지금처럼 `CombatStageData` 저작 검사가 부트 DB 생성 때 한다.
  - 인스펙터는 판 밖 칸이 남아 있으면 경고만 한다. 필드 크기를 줄인 경우가 그렇다.
- **다른 배치 표시(선택, P7):** 고정 적·적 구조물·본부 칸을 글자로 겹쳐 보이고, 아군 구역 안이면 빨간 테두리를 그린다(Core가 진입 때 멈추는 경우).
- 다중 선택에서는 격자를 그리지 않는다. 지금 인스펙터는 다중 편집을 허용하지만, 칸 칠하기는 스테이지 하나에만 뜻이 있다.

### PV6 전환 입구 걷기(PZ-d) 알림

- PV1~PV4가 끝나면 우리 코드는 옛 깊이 속성과 줄 수 생성자를 쓰지 않는다.
- 남는 사용처는 천명님 Core 시험 10줄과 Core 안쪽뿐이다.
- 이 사실을 Core 전달 문서 §43 아래 "View 대응"에 적어 PZ-d를 걸 수 있게 한다.

## 4. 바꾸지 않는 것

- Core·Buffer·DB(`CombatStageData`·`FieldData`)·Factory.
- 배치 가능 판정(Core 미리보기), 배치 드래그 고스트, 표시 시점(GridOutline은 드래그 중에만).
- 레일 표시, 정적 구역 밖의 강조 층.
- DT 모드·엑셀(민형님).

## 5. 보이는 동작 변화

| 상황 | 지금 | 바뀐 뒤 |
|---|---|---|
| 줄 구역 스테이지의 아군 표시 | 아래 N줄 | 같음 |
| 칸 구역 스테이지의 아군 표시 | 나오지 않음 | 칠한 칸 |
| 적 구역 색(CellPrefab 모드) | 이미 사라짐(값 0) | 코드도 없음 |
| 개발 진입 기본 8×7 | 데몬 (3,5)·본부 (4,6) | 같음 |
| Play From Boot 설정 창 | 적군 배치 깊이 칸 | 없음 |
| SO 모드 도구 | 위 2줄이 "적 배치 구역", 반대로 된 경고 | 그대로(Q-PV4 C, 호진님 메모) |
| `CombatStageData` 인스펙터(PV7을 하면) | 칸 목록 배열 | Column×Row 격자를 드래그로 칠함 |

## 6. 시험·검증

- 새 시험은 쓰지 않는다(View 작업 지시). MSBuild도 돌리지 않는다(전투 View 지시). 보고에는 "컴파일 미확인"을 적는다.
- 기존 시험은 바뀌지 않을 것으로 본다(§1.6).
- Unity 확인(사용자):
  1. 지금 스테이지에서 배치 드래그 때 아래 2줄 테두리·채우기가 나온다.
  2. `Test` 스테이지 `playerDeploymentCells`에 칸 몇 개를 넣고(배열 또는 PV7 격자) 배치 드래그 → 그 칸만 칠해지고 그 칸에만 놓인다.
  3. 개발 진입 Fixture로 기본값 진입.
  4. Play From Boot 설정 창 표시와 잘못된 값 표시.
  5. (PV7) 격자 칠하기·지우기·끌기·Undo·"줄 구역으로 되돌리기", 필드 없는 스테이지 안내, 다중 선택 때 격자 없음.

## 7. Phase (안)

1. **PV-a View 표시:** PV1~PV3.
2. **PV-b 개발 진입:** PV4.
3. **PV-c 칸 입력 인스펙터:** PV7(Q-PV5가 A일 때). SO 모드 도구는 고치지 않는다(Q-PV4 C).
4. **PV-d 문서·공유:**
   - 천명님: PZ-d를 걸 수 있다는 알림(Core 전달 문서 §43).
   - 호진님: 도구 할 일 목록 메모(PV5 내용 — 반대로 된 경고, 적 구역 색, 칸 목록 읽기·쓰기).
   - 새 파일은 없을 것으로 봐서 폴더 배치 안내는 바꾸지 않는다.

검토 에이전트는 묶음 끝에 한 번 돌린다.

## 8. 원칙 점검 (7기준, View 구역 부분, 예상)

"지금"은 Core 변경 뒤 상태 기준이다(칸 구역이면 틀린 표시).

| 기준 | 지금 | 바뀐 뒤 | 이유 |
|---|---|---|---|
| SSOT | 5 | 9 | View의 줄 규칙 사본이 없어지고 Core 값 하나에 묻는다. 남는 것은 PV7 인스펙터의 대체 규칙 한 줄(원본 주석)과, 고치지 않는 SO 모드 도구의 줄 규칙 사본(Q-PV4 C, 호진님 메모)이다 |
| SOLID | 8 | 9 | 상태는 판정 재료만 받는다. 적 구역 갈래를 걷는다 |
| FW 계약 | 9.5 | 9.5 | Framework와 무관하다 |
| 변경 독립성 | 6 | 9 | 구역 모양이 바뀌어도 View는 그대로다 |
| 캡슐화 | 7 | 9 | View가 줄 수·칸 목록 모양을 모른다 |
| C#/Unity 권장 구조 | 8.5 | 9 | 남는 직렬화 값 둘(씬·Boot)은 Unity가 버린다 |
| 무의미한 If/Tick | 9 | 9.5 | 설치 때 한 번, 갈래 하나가 줄어든다 |
| **합** | **약 7.6** | **약 9.1** | |

## 9. 정할 것 (권장안 먼저)

### Q-PV1 View에 구역을 어떤 모양으로 넘기나

- **A(권장) `PlayerDeploymentZone`을 그대로 넘긴다.** 판정 원본이 Core 값 하나로 남고, View는 구역 모양을 모른다.
- B 칸 목록으로 펼쳐 넘긴다. 줄 구역이면 Display가 줄을 칸으로 펼쳐야 해서 규칙 사본이 다시 생긴다.
- C `Func<CellCoord, bool>`을 넘긴다. 사본은 없지만 상태 클래스 서명만 흐려지고 얻는 것이 없다.

### Q-PV2 적 구역 표시

- **A(권장) 걷는다.** 규칙에 없는 구역이다.
- B "아군 구역 밖 = 적이 놓일 수 있는 칸"을 CellPrefab 모드에서 옅게 칠한다. 운영은 GridOutline이라 보이지 않고, 플레이어 행동과도 상관없다.

### Q-PV3 개발 진입의 적 줄 수 설정

- **A(권장) 걷는다.** 위치는 아군 줄 수로 계산하고, 기본값 자리는 같다.
- B "적 시작 줄"로 이름만 바꿔 위치 조절용으로 남긴다. 쓸 일이 거의 없는 설정 넷이 남는다.

### Q-PV4 SO 모드 도구(호진님 영역)를 이번에 고치나 — **결정: C(2026-10-28 사용자 "레벨 툴은 그냥 두고")**

- A(처음 권장) 이번 작업에 넣는다. 사용자 지시로 이 세션이 고치고 호진님께 메모를 넘긴다.
  - 처음 요청이 "드래그로 칠하기"다.
  - 지금 도구가 반대로 된 경고를 낸다.
  - 칸 입력 화면이 있어야 칸 구역을 실제로 쓸 수 있다.
- B 색·경고만 고치고 붓은 호진님께 넘긴다. 틀린 안내는 바로잡지만 칸 입력은 인스펙터 배열뿐이다.
- C 고치지 않고 호진님 메모만 쓴다. 영역 규칙에 가장 맞지만, 그동안 도구가 틀린 경고를 낸다.

### Q-PV5 칸 입력 화면 (Q-PV4 C 뒤 다시 씀, 2026-10-28)

도구를 고치지 않으므로 처음 안의 "SO 모드 붓"은 빠졌다.

- **A(권장) `CombatStageData` 인스펙터에 Column×Row 격자를 두고 드래그로 칠한다(PV7).**
  - 처음 말씀하신 "사각형을 드래그로 칠하기"와 같은 모양이다.
  - SO만 `FieldDataSO`가 아니라 `CombatStageData`다. 칸 목록이 스테이지 SO에 있기 때문이다(PV7 첫 항목).
  - 이미 있는 우리 커스텀 인스펙터에 묶음 하나를 더하는 것이라 새 파일이 없다.
- B 칸 입력 화면을 만들지 않는다. 칸은 기본 인스펙터의 `CellInput` 배열로만 넣고, 칠하기는 나중에 호진님 도구가 맡는다.
- C `FieldDataSO`에 격자를 둔다. 칸 목록 자리를 필드로 옮겨야 해서 DB·Core 어댑터 변경(천명님 결정 ②A 뒤집기)이 필요하고, 필드를 함께 쓰는 스테이지들 구역이 같이 바뀐다.

### 설계에서 정한 것 (확인 대상)

| 번호 | 내용 |
|---|---|
| P1 | Fixture 데몬·본부 위치는 `top = min(2, 세로 − 아군 줄 수)`로 옛 식을 쓴다. 기본값 자리는 같다 |
| P2 | 남는 직렬화 값(`Boot.unity`)과 EditorPrefs 키는 지우는 코드를 넣지 않는다 |
| P3 | (PV7) 칸 목록이 빈 채로 처음 칠하면 줄 구역을 칸 목록으로 펼친다. "줄 구역으로 되돌리기" = 칸 목록 비우기 |
| P4 | (PV5, 호진님 메모용) 아군 줄 수 슬라이더는 대체값으로 남기고, 칸 목록이 있으면 끈다 |
| P5 | (PV5, 호진님 메모용) 경고는 저장을 막지 않는다 |
| P6 | `FieldStaticZone`의 `Rail = 3`은 번호를 그대로 둔다 |
| P7 | (PV7) 격자에 고정 적·적 구조물·본부 칸을 겹쳐 보이고, 아군 구역 안이면 빨갛게 표시한다(선택, 빼도 됨) |

## 10. 구현 메모 (2026-10-28, PV-a·PV-b·PV-b2·PV-c 코드 완료)

계획에서 달라진 점과 구현으로 확인한 사실이다. 원본 기록은 [계획 §7](../plans/2026-10-28-combat-player-zone-view-plan.md#7-실행-기록)이다.

- **PV-a(View 표시):** 설계대로다. `FieldHighlightState.SetStaticZones`는 구역 null 검사를 레일 표시보다 먼저 해서 표시 버퍼가 더러워지지 않게 했다. Ua 문제 없음(사용자).
- **PV-b(개발 진입)·PV-b2(Fixture 모드 삭제):**
  - PV-b 뒤 사용자 지시로 Play From Boot의 Fixture 모드 자체를 지웠다(계획 밖, 계획 §7.3). 개발 진입은 Database 정의 하나다.
  - 설정 창에는 읽기 줄 "Battle Definition: Database"만 남는다.
  - Fixture 클래스는 천명님 `CategoryDefinitionMigrationTests`용으로만 남겼다(사용자 결정, Core 전달 문서 §43에 메모).
  - 그래서 PV4의 Fixture 필드 설정 정리(TP-1 설정 창 검사 원본 호출 포함)는 대부분 사라졌다.
  - Ub·Ub2 문제 없음(사용자).
- **PV-c(인스펙터 격자):**
  - 계획에 없던 것: 마지막 한 칸은 지우지 않는다. 칸 목록이 비면 줄 구역으로 돌아가 버려서다. 비우는 것은 버튼으로만 한다.
  - 계획에 없던 것: 필드가 없는 스테이지에서는 칸 배열 자체가 보이지 않는다.
  - 사용자 추가 요청 둘(계획 §7.4.1·§7.4.2):
    - 적 말고 지형·장애물·오브젝트도 보인다(앵커 칸만, 크기·회전은 보지 않음, 구역 안 오브젝트는 노랑 테두리·경고).
    - 글자 대신 색 표기와 범례다. 칸 바탕은 구역 색, 가운데 네모는 배치 색, 범례는 있는 종류만 보인다.
  - 막힌 칸(벽·낭떠러지)을 따로 칠하지는 않았다. 어떤 지형이 막는지는 Core `BlocksVoluntaryEntry` 규칙이라 사본을 두지 않았다.
  - Uc 1~9 문제 없음(사용자). 색 표기(Uc 10)는 확인 대기다.
- **SO 모드 도구(PV5):** 고치지 않았다(Q-PV4 C). 할 일 목록은 [호진님 메모](../handoffs/2026-10-28-player-zone-so-mode-hojin-share.md)다(미전달).
- **PZ-d:** View·개발 진입은 옛 깊이 속성과 줄 수 생성자를 쓰지 않는다. 천명님이 전환 입구를 지워도 된다(Core 전달 문서 §43에 적음).
