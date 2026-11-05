# 전투 View 아군 배치 구역 — Task 계획

- 작성: 2026-10-28, View 작업
- 설계: [player-zone-view-design](../specs/2026-10-28-combat-player-zone-view-design.md)(PV1~PV7)
- 결정(2026-10-28 사용자):
  - Q-PV4는 C(레벨 툴은 그대로)다.
  - Q-PV1·Q-PV2·Q-PV3·Q-PV5는 A, P1~P7은 그대로다("권장대로").
- 상태: **계획. 코드 착수 전.** 계획에서 정한 것 TP-1~TP-8(§1)은 사용자 확인 대상이다.

## 0. 진행 규칙과 착수 조건

- Phase마다 멈추고 보고한다. 보고에는 "컴파일 미확인"을 적고, 다음 Phase를 자세히 설명한다.
- 새 시험은 쓰지 않는다(View 작업 지시). MSBuild도 돌리지 않는다(전투 View 지시). 컴파일과 재생 확인은 사용자가 Unity에서 한다.
- 기존 시험은 컴파일이 깨지는 곳만 최소로 맞춘다. 지금 예상으로는 맞출 곳이 없다(설계 §1.6).
- 검토 에이전트는 묶음 끝(PV-d)에 한 번만 돌린다.
- 커밋과 push는 사용자가 한다.
- **착수 조건:** 작업 트리에 다른 세션(소리 창구 2차 ES)의 변경이 있다.
  - 지금 겹치는 파일은 없다(2026-10-28 `git status` 대조). 고칠 파일은 §2·§3·§4 표에 있다.
  - Phase를 시작할 때마다 같은 파일에 커밋 안 된 다른 변경이 없는지 다시 본다.

## 1. 계획에서 정한 것 (확인 대상)

| 번호 | 내용 | 이유 |
|---|---|---|
| TP-1 | Play From Boot 설정 창의 필드 값 검사는 사본을 두지 않는다. Fixture의 규칙 함수 하나를 `GameBootstrapEditorOverrides`(public, 에디터 전용)를 거쳐 부른다 | 지금은 같은 검사가 Fixture 생성자와 설정 창(`PlayFromBootSettingsWindow.cs:197-198`) 두 곳에 있다. 설정 창은 Assembly-CSharp-Editor라 internal인 Fixture를 직접 부를 수 없다 |
| TP-2 | `FieldView.ApplyStaticHighlights`는 `PlayerDeploymentZone`을 받는다(Q-PV1 A). Display·FieldView·상태 클래스 모두 같은 값을 넘긴다 | 판정 원본을 Core 값 하나로 둔다 |
| TP-3 | (P7) 인스펙터 격자에 겹쳐 보이는 적 배치는 **앵커 칸만**이다. 유닛·구조물 크기와 회전은 보지 않는다. 그 사실을 격자 아래 한 줄로 적는다 | 크기·회전 칸 계산은 Core 규칙이고 DB SO 안에 사본을 두고 싶지 않다. 정확한 검사는 전투 진입 때 Core가 한다 |
| TP-4 | 격자 칸 크기는 인스펙터 너비에 맞춘다(최대 22px, 최소 8px). 위가 큰 y로 그린다 | 15×15가 기본 인스펙터 너비에 들어가고, 맵·SO 모드와 방향이 같다 |
| TP-5 | 끌기 한 번(누름~뗌) = Undo 한 번 | 칸마다 Undo가 쌓이지 않게 한다 |
| TP-6 | 칸 목록은 쓸 때마다 y → x로 정렬해 저장한다 | Core도 같은 순서로 정렬하고(`PlayerDeploymentZone.Of`), 에셋 차이가 안정된다 |
| TP-7 | 필드 크기·아군 줄 수는 `FieldDataSO`를 `SerializedObject`로 읽는다(`columnCount`·`rowCount`·`playerDeploymentDepth`) | `GetData()`는 잘못된 값이면 예외를 던져 인스펙터가 멈출 수 있다 |
| TP-8 | 칸 목록에 중복 칸이 있으면(에셋을 손으로 고친 경우) 격자는 그대로 그리고 빨간 안내를 띄운다. 칠하면 정렬·중복 제거된 목록으로 다시 쓴다 | `PlayerDeploymentZone.Of`가 중복에 예외를 던지므로, 인스펙터는 칸 집합을 직접 만들어 판정한다. 부트 DB 생성은 지금처럼 중복을 거절한다 |

## 2. PV-a View 표시 (PV1~PV3)

| Task | 파일 | 할 일 |
|---|---|---|
| a1 | `ViewSystem/Combat/Display/Field/FieldHighlightState.cs` | `FieldStaticZone.EnemyZone`을 지운다(`Rail = 3` 유지, P6). `SetStaticZones(PlayerDeploymentZone playerZone, IReadOnlyList<CellCoord> railCells)`로 바꾸고 null 검사를 둘 다 한다. 칸마다 `playerZone.Contains(new CellCoord(x, y))`로 판정한다. 줄 비교 `:146-147`를 지우고 문서 주석 `:125-127`을 "레일 > 아군 배치 구역(Core 값으로 판정)"으로 고친다 |
| a2 | `…/Field/FieldHighlightPalette.cs` | 생성자 인자 `enemyZone`, 속성 `EnemyZone`, `TryGetZone`의 `EnemyZone` 갈래를 지운다 |
| a3 | `…/Field/FieldView.cs` | `enemyZoneColor` 필드와 툴팁(`:39-40`)을 지우고, `CreatePalette`(`:448`)의 인자를 지운다. `ApplyStaticHighlights`(`:159-170`) 서명을 TP-2대로 바꾸고 null 검사를 더한다 |
| a4 | `ViewSystem/Combat/Display/CombatSceneDisplay.cs:199` | `_field.ApplyStaticHighlights(state.PlayerZone, state.RailCells)`로 바꾼다. 필요하면 using을 더한다 |
| a5 | 같은 폴더 | "적 구역"이 남은 주석과 툴팁을 찾아 고친다(`grep "적 구역\|EnemyZone"`). `playerZoneColor` 툴팁은 그대로 둔다 |

- 바뀌는 화면: 칸 구역 스테이지에서 칠한 칸이 아군 구역으로 보인다. 줄 구역 스테이지는 같다.
- **Ua 확인(사용자):**
  1. 컴파일.
  2. 지금 스테이지(예: `Combat_Normal_A`)에서 배치 드래그 때 아래 줄 구역 테두리·채우기가 지금과 같다.
  3. `Test.asset` `playerDeploymentCells`에 칸 몇 개를 배열로 넣고 진입한다 → 배치 드래그 때 그 칸만 칠해지고, 그 칸에만 놓인다. 확인 뒤 값을 되돌린다.
  4. 레일 스테이지에서 레일 칸 표시가 그대로다.

## 3. PV-b 개발 진입 (PV4)

> **2026-10-28 사용자 지시로 Play From Boot의 Fixture 모드를 지웠다(PV-b2, 실행 기록 §7.3).** 아래 b2~b4의 Fixture 필드 설정은 그 뒤 모두 사라졌다. b1의 Fixture 클래스는 천명님 시험용으로만 남는다.

| Task | 파일 | 할 일 |
|---|---|---|
| b1 | `ViewSystem/Dev/DevBattleInitializationSource.cs` | 생성자 인자 `enemyDeploymentDepth`, 상수 `DefaultEnemyDeploymentDepth`, 필드 `_enemyDeploymentDepth`를 지운다. 규칙 함수 `internal static string FieldError(int mapWidth, int mapHeight, int playerDeploymentDepth)`(맞으면 null)를 더한다. 조건: 셋 다 1 이상, 아군 줄 수 < 세로, 가로 × (세로 − 아군 줄 수) ≥ 2. 생성자는 이 함수를 불러 `ArgumentException`을 던진다. 위치는 `top = Math.Min(2, h − p)`로 옛 식(`:116-119`)의 적 줄 수 자리에 넣는다(P1). `StageConfig`는 `PlayerDeploymentZone.Rows(p)`를 받는 생성자로 바꾼다. 문서 주석의 "각 배치 구역 안" 문구를 "아군 구역 밖 위쪽"으로 고친다 |
| b2 | `ViewSystem/Boot/GameBootstrap.cs` | 씬 값 `devEnemyDeploymentDepth`(`:122`), 생성 인자(`:436-437`), 덮어쓰기 적용(`:653`), 로그 구절(`:658`)을 지운다. 로그는 "배치 깊이 아군 N"만 남긴다 |
| b3 | `ViewSystem/Boot/GameBootstrapEditorOverrides.cs` | `DevEnemyDeploymentDepth`(`:213-217`)를 지운다. `public static string DevFieldError(int width, int height, int playerDepth)`를 더하고 b1 함수를 부른다(TP-1) |
| b4 | `Assets/ProjectL/Editor/Boot/PlayFromBootSettingsWindow.cs` | 적 줄 수 지역 값(`:85`)·입력 칸(`:128`)·저장(`:152`)을 지운다. 잘못된 값 판정(`:197-198`)은 `DevFieldError(...) != null`로 바꾸고, 안내 문구에 그 오류 문구를 그대로 보인다 |
| b5 | 호출처 | `DevBattleInitializationSource(` 호출을 다시 찾는다. 지금은 GameBootstrap 한 곳과 천명님 `CategoryDefinitionMigrationTests.cs:36`(기본 인자)뿐이다. 시험은 바꾸지 않는다 |

- 남는 것: `Boot.unity`의 `devEnemyDeploymentDepth` 직렬화 값과 EditorPrefs 키. 쓰지 않고 그대로 둔다(P2). `Boot.unity`는 지금 다른 변경이 있어 이 Phase에서 열거나 저장하지 않는다.
- **Ub 확인(사용자):**
  1. 컴파일.
  2. Play From Boot에서 Combat 직접 시작 + Fixture 기본값(8×7, 아군 2) → 데몬 (3,5), 본부 (4,6), 아군 구역은 아래 2줄이다.
  3. 설정 창에 "적군 배치 깊이" 칸이 없다. 아군 줄 수를 세로와 같게 넣으면 빨간 안내가 나온다.
  4. 세로 3·아군 2 같은 작은 값으로 진입 → 데몬·본부가 맨 위 줄에 놓인다.

## 4. PV-c 칸 입력 인스펙터 (PV7)

| Task | 파일 | 할 일 |
|---|---|---|
| c1 | `Systems/DataBaseSystem/Editor/CombatStageDataEditor.cs` | `DrawPropertiesExcluding` 목록에 `playerDeploymentCells`를 더하고, 그 뒤(Legacy 접기 앞)에 "아군 배치 구역" 묶음을 그린다. 다중 선택이면 묶음에 "스테이지 하나를 고르면 격자를 그립니다"만 적는다 |
| c2 | 같은 파일 | 필드 읽기(TP-7). `fieldDefinition`이 비면 "Field Definition을 연결하면 격자가 보입니다" 안내만 보인다 |
| c3 | 같은 파일 | 보이는 구역: 칸 목록이 있으면 그 칸 집합(TP-8), 비면 `PlayerDeploymentZone.Rows(필드 아군 줄 수)`의 `Contains`로 정한다. 대체 규칙 줄에 "원본: `BattleDefinitionAdapter`" 주석을 단다. 머리 문구는 "필드 줄 수 사용 중 (아래 N줄)" 또는 "칸 지정 N칸"이다 |
| c4 | 같은 파일 | 격자 그리기와 입력(TP-4·TP-5·TP-6). 누른 칸이 구역 안이면 지우기, 밖이면 칠하기로 정한 뒤 끄는 동안 지나는 칸에 같은 동작을 한다. 칸 목록이 빈 채로 처음 칠하면 지금 줄 구역 칸을 먼저 목록에 펼친다(P3). "줄 구역으로 되돌리기" 버튼은 목록을 비운다(목록이 있을 때만 켬). 판 밖 칸이 남아 있으면 개수와 함께 경고한다(지우기 버튼은 두지 않음, 설계 PV7) |
| c5 | 같은 파일 | (P7, TP-3) 고정 적 유닛(`initialEnemyPlacements`, 정의가 있는 것)·적 구조물(`initialEnemyStructurePlacements`)·본부(`headquarters`가 있을 때 `enemyHeadquartersAnchor`)의 앵커 칸에 글자 "적"·"구"·"본"을 겹친다. 아군 구역 안이면 그 칸을 빨간 테두리로 그리고 "아군 구역 안의 적 배치 N개 — 전투 진입 때 Core가 멈춥니다"를 경고한다 |

- 데이터 코드(`CombatStageData.cs`)는 바꾸지 않는다. 인스펙터는 `SerializedProperty`로만 읽고 쓴다.
- **Uc 확인(사용자):**
  1. 컴파일.
  2. `Combat_Normal_A` 인스펙터에서 15×15 격자가 보이고, 아래 줄 구역(필드 아군 줄 수)이 옅게 칠해져 있다.
  3. 칸을 칠하면 줄 구역이 칸 목록으로 펼쳐진 채 칠한 칸이 더해진다.
  4. 끌어 지우기, Ctrl+Z 한 번에 끌기 한 번이 되돌아간다.
  5. "줄 구역으로 되돌리기"를 누르면 칸 목록이 빈다.
  6. 적 앵커가 구역 안이면 빨간 표시와 경고가 나온다.
  7. 필드 없는 스테이지와 다중 선택 때 안내가 나온다.
  8. 칠한 스테이지로 진입하면 Ua 3과 같다. 확인 뒤 에셋을 되돌릴지 정한다.

## 5. PV-d 문서·공유·검토

| Task | 할 일 |
|---|---|
| d1 | 묶음 끝 검토 에이전트 하나. PV-a~PV-c 변경 전체를 7기준으로 매기고 결함 후보를 낸다. 반영은 사용자와 정한다 |
| d2 | Core 전달 문서 §43 "View 대응"에 적는다. View·개발 진입은 옛 깊이 속성과 줄 수 생성자를 쓰지 않으므로 PZ-d(전환 입구 걷기)를 걸 수 있다는 것과, 남는 사용처(천명님 Core 시험 10줄)다 |
| d3 | 호진님 메모 새 파일 `handoffs/2026-10-28-player-zone-so-mode-hojin-share.md`(미전달). 내용은 설계 PV5 할 일 목록(반대로 된 경고, 적 구역 색·슬라이더, 칸 목록 읽기·쓰기, 새 필드 만들기의 적 줄 수)과, `CombatStageData` 인스펙터 격자가 같은 칸을 쓴다는 사실이다 |
| d4 | 설계 §10 구현 메모(계획과 달라진 점), 이 계획의 실행 기록 §7, CLAUDE.md 현재 상태 |

## 6. 커밋 묶음 (사용자가 커밋)

- 새 코드 파일은 없다. 새 문서는 이 계획·설계·호진님 메모다.
- PV-a(View 넷), PV-b(개발 진입 넷), PV-c(인스펙터 하나)는 Phase마다 따로 커밋해도 된다. 서로 컴파일 의존이 없다.
- `Boot.unity`·`Combat.unity`에 남는 옛 직렬화 값은 이번 커밋에 넣지 않는다. 씬을 저장할 일이 생기면 그때 함께 빠진다.

## 7. 실행 기록

### 7.1 PV-a 코드 완료 (2026-10-28, 사용자 "그대로, PV-a 진행" → TP-1~TP-8 그대로, 컴파일 미확인, 새 시험 없음, 시험 변경 없음)

- a1 `FieldHighlightState`:
  - `FieldStaticZone.EnemyZone`을 지웠다(`Rail = 3` 유지).
  - `SetStaticZones(PlayerDeploymentZone, rails)`가 칸마다 `Contains`로 판정한다. 구역 null 검사는 레일 표시보다 먼저 해서 표시 버퍼가 더러워지지 않게 했다.
- a2 `FieldHighlightPalette`: 적 구역 인자·속성·갈래를 지웠다.
- a3 `FieldView`: `enemyZoneColor`(필드·툴팁)를 지웠다. `ApplyStaticHighlights(PlayerDeploymentZone, rails)`로 바꾸고 null 검사를 더했다.
- a4 `CombatSceneDisplay.cs:199`: `state.PlayerZone`을 넘긴다. using은 이미 있다(`Contracts.Common`).
- a5 대조:
  - View에서 옛 깊이 속성과 적 구역을 읽는 곳은 없다. `Dev/`는 PV-b에서 다룬다.
  - 남은 "정적 구역" 주석은 아군 구역·레일을 뜻해 그대로 두었다.
  - 시험에서 옛 깊이 속성을 쓰는 곳은 천명님 Core 시험뿐이고 View 서명과 무관하다.
- 계획과 다른 점은 없다.
- `Combat.unity`에 남는 `enemyZoneColor` 직렬화 값은 씬을 다음에 저장할 때 빠진다(P2와 같은 취급).
- 같은 시각 작업 트리에 다른 세션의 소리 작업 변경이 있다. 이번 Phase 파일 넷과는 겹치지 않는다.
- Ua 확인 문제 없음(2026-10-28 사용자).

### 7.2 PV-b 코드 완료 (2026-10-28, 사용자 "Ua 문제 없음, PV-b", 컴파일 미확인, 새 시험 없음, 시험 변경 없음)

- b1 `DevBattleInitializationSource`:
  - 적 줄 수 인자·상수·필드를 지웠다.
  - 규칙 함수 `FieldError(w, h, p)`를 더했고, 생성자는 그 문구에 `[DevBattle]`을 붙여 `ArgumentException`을 던진다.
  - 위치는 `top = Math.Min(2, h − p)`로 옛 식을 그대로 쓴다. `StageConfig`는 `PlayerDeploymentZone.Rows(p)` 생성자를 쓴다.
- b2 `GameBootstrap`: 씬 값·생성 인자·덮어쓰기 적용·로그의 적 줄 수를 지웠다. 로그는 "아군 배치 줄 수 N"이다.
  - 계획에 없던 한 줄: `devPlayerDeploymentDepth`에 툴팁(아군 구역 뜻, 적·본부 위치)을 더했다.
- b3 `GameBootstrapEditorOverrides`: `DevEnemyDeploymentDepth`를 지우고 `DevFieldError`(Fixture 함수로 넘김)를 더했다.
- b4 `PlayFromBootSettingsWindow`:
  - 적군 칸·지역 값·저장을 지웠고, 칸 이름은 "아군 배치 줄 수"다.
  - 잘못된 값이면 `DevFieldError` 문구를 빨간 안내로 그대로 보인다. 맞으면 지금처럼 안내 문구다.
- b5 대조: 생성자 호출은 GameBootstrap 한 곳과 천명님 `CategoryDefinitionMigrationTests.cs:36`(기본 인자)뿐이다. 시험은 바꾸지 않았다.
- 남는 것(P2): `Boot.unity`의 `devEnemyDeploymentDepth` 직렬화 값과 EditorPrefs 키 `ProjectL.BootOverride.DevEnemyDeploymentDepth`. `Boot.unity`는 다른 변경이 있어 열지 않았다.
- 검사 규칙의 차이: 옛 검사는 "두 깊이 합 < 세로", "가로 × 적 줄 수 ≥ 2"였다. 이제는 "아군 줄 수 < 세로", "가로 × (세로 − 아군 줄 수) ≥ 2"다. 기본값과 옛 값으로 통과하던 조합은 모두 통과한다.
- Ub 확인 문제 없음(2026-10-28 사용자).

### 7.3 PV-b2 Play From Boot Fixture 모드 삭제 (2026-10-28, 계획 밖 사용자 지시, 컴파일 미확인, 새 시험 없음, 시험 변경 없음)

- 지시(사용자): PV-c를 시작하려다 멈추고 "그냥 Play From Boot에 Fixture 모드 삭제하자" → "fixture 모드만 삭제하고 DataBase 모드는 남겨달라는건데, 천명님 시험용으로 클래스 자체는 남겨도됨".
- PV-b(Fixture 필드 설정 정리)는 이것으로 대부분 대신한다. Fixture 클래스 안의 새 생성자·위치 식만 남는다.
- 바꾼 것:
  - `GameBootstrap`:
    - 지운 것: 열거 `DevBattleDefinition`, 씬 값 `devBattleDefinition`, 개발 전투 필드 씬 값 셋, Fixture 갈래와 경고 셋.
    - Database 조건(보스·지정·지정 배치·덮어쓰기)은 조건 없이 늘 그 길이다.
    - 덮어쓰기 검사·로그에서 전투 정의와 필드를 뺐다. 클래스 문서와 `useDevProgress` 툴팁에 "전투 정의는 운영과 같은 DB 정의"를 적었다.
  - `GameBootstrapEditorOverrides`: `DevBattleDefinition`·`DevMapWidth`·`DevMapHeight`·`DevPlayerDeploymentDepth`·`DevFieldError`를 지웠다. 문서에는 읽지 않고 남는 EditorPrefs 키를 적었다.
  - `PlayFromBootSettingsWindow`:
    - 지운 것: "Battle Definition" 선택, "개발 전투 필드" 묶음, Fixture 안내 문구.
    - Database 모드가 남은 것을 보이도록 읽기 줄 "Battle Definition: Database (운영 DB 정의)"를 둔다.
    - 개발 진입 안내 문구는 노드 자동 보정(`ResolveDevEntryNode`)에 맞게 고쳤다. 옛 문구 "거절됩니다"는 10-20부터 틀린 설명이었다.
  - `DevBattleInitializationSource`(클래스는 남김, 사용자 결정):
    - 문서에 "천명님 `CategoryDefinitionMigrationTests`만 쓴다"를 적었다.
    - 외부 사용처가 없어진 기본값 상수 셋과 `FieldError`는 private으로 바꿨다.
  - 안내 문서 둘(`viewDocs/placed-prefab-terrain-guide.md:82`, `railway-view-guide.md:11`): Fixture 문장을 지웠다.
- 시험: 천명님 `CategoryDefinitionMigrationTests.cs:36`은 기본 생성자로 클래스를 그대로 쓴다. 바꾸지 않았다.
- 남는 것: `Boot.unity`의 `devBattleDefinition`·`devMap*`·`devPlayerDeploymentDepth`·`devEnemyDeploymentDepth` 직렬화 값과 EditorPrefs 키. 읽지 않으며 지우지 않았다(P2와 같은 취급). `Boot.unity`는 다른 변경이 있어 열지 않았다.
- 보이는 변화:
  - Play From Boot 덮어쓰기를 켜고 전투 정의를 고른 적이 없던 PC(저장값 0 = Fixture)는 이제 Database로 진입한다.
  - `Boot.unity`는 `useDevProgress: 0`이라 씬 기본 동작은 그대로다.
- 확인(사용자, Ub2):
  1. 컴파일.
  2. Play From Boot 설정 창에 Battle Definition 선택·개발 전투 필드가 없고 읽기 줄만 있다.
  3. Combat 직접 시작 + 개발 진입으로 DB 스테이지에 들어간다(노드가 비전투면 콘솔에 보정 한 줄).
  4. 보스·지정 스테이지·지정 스테이지 데이터 진입이 지금처럼 된다.
- Ub2 확인 문제 없음(2026-10-28 사용자).

### 7.4 PV-c 코드 완료 (2026-10-28, 사용자 "Ub2 문제 없음, PV-c", 컴파일 미확인, 새 시험 없음, 시험 변경 없음)

- 파일 하나: `Systems/DataBaseSystem/Editor/CombatStageDataEditor.cs`. 새 파일 없음, `CombatStageData.cs` 무변경.
- c1: `DrawPropertiesExcluding`에 `playerDeploymentCells`를 더하고, Legacy 접기 앞에 "아군 배치 구역" 묶음을 둔다. 다중 선택이면 안내만 보인다.
- c2: 필드 SO를 `SerializedObject`로 읽는다(`columnCount`·`rowCount`·`playerDeploymentDepth`, TP-7). 참조가 바뀌면 새로 만들고, `OnDisable`에서 버린다. 필드가 없으면 안내만 보인다.
- c3: 보이는 구역은 Core 값으로 판정한다. 칸이 있으면 `PlayerDeploymentZone.Of(중복 없는 칸)`, 비면 `Rows(필드 아군 줄 수)`의 `Contains`다. 대체 규칙 줄에 원본 `BattleDefinitionAdapter` 주석을 달았다. 판 밖 개수도 Core `CellsOutside`로 센다.
- c4: 격자·입력.
  - 칸 크기는 너비에 맞춰 8~22px이고, 위가 큰 y로 그린다(TP-4).
  - 누른 칸이 구역 밖이면 칠하기, 안이면 지우기다. 끄는 동안 지나는 칸에 같은 동작을 한다.
  - 끌기 한 번은 `Undo.CollapseUndoOperations`로 Undo 한 번이다(TP-5). 쓸 때는 y → x로 정렬해 쓴다(TP-6).
  - 빈 목록에서 처음 칠하면 줄 구역을 먼저 펼친다(P3). "줄 구역으로 되돌리기" 버튼은 목록이 있을 때만 켜진다.
  - 중복은 빨간 안내를 띄우고, 한 번 칠하면 정리돼 저장된다(TP-8). 판 밖 칸은 경고만 하고 지우지 않는다.
- c5: 고정 적 유닛·적 구조물(정의가 있는 것)·본부(정의가 있을 때)의 앵커에 "적·구·본"을 겹친다. 칸이 12px보다 작으면 글자는 생략한다. 아군 구역 안이면 빨간 테두리를 그리고 개수를 경고한다(TP-3, P7).
- 계획에 없던 것 셋:
  1. **마지막 한 칸은 지우지 않는다.** 칸 목록이 비면 필드 줄 수 구역으로 돌아가 버려서다. 비우는 것은 버튼으로만 한다. 이 사실은 격자 아래 한 줄로 알린다.
  2. 필드가 연결되지 않은 스테이지에서는 칸 배열 자체가 인스펙터에 보이지 않는다(격자가 대신하므로). 저작 검사도 필드가 있어야 돈다(`CombatStageData.CreateStageData`).
  3. 글자 표시는 칸 크기 12px 이상일 때만이다(작은 칸에서 글자가 넘쳐서).
- 확인은 §4 Uc 8항목이다.

#### 7.4.1 PV-c 추가: 지형·장애물·오브젝트 표시 (2026-10-28, 사용자 "권장안대로 더해", 컴파일 미확인)

- 질문(사용자): "적 말고 터레인, 장애물, 오브젝트까지 보여주기엔 좀 그러려나" → 권장안 "보이기만, 앵커 칸만, 글자 하나, 편집 없음"으로 결정했다.
- 이유(Core 배치 규칙 `CombatPlacementCalculator.Preview`·`Place`로 확인):
  - 벽·낭떠러지·장애물·적·구조물 칸에는 아군을 놓을 수 없다.
  - 오브젝트는 배치를 막지 않고 놓는 순간 발동한다(`ResolveObjectsAt`).
- 바꾼 것(같은 파일):
  - 표시 모음 `CollectMarks`: 적·구·본(층 Enemy), 장애물 "장"(`environment.obstaclePlacements` 앵커), 오브젝트 "오"(`environment.objectPlacements` 칸), 지형(`environment.terrainPlacements` 칸)을 모은다.
  - 지형 글자는 정의 SO `Kind`로 정한다: 고·모·벽·약·낭·토·보.
  - 무작위 최초 위치 토네이도와 옛 호환 지형 입력은 뺐다.
  - 한 칸에 여럿이면 적 > 장 > 오 > 지형 순으로 글자 하나만 보인다(열거 `MarkLayer`).
  - 구역 안의 적은 빨강, 구역 안의 오브젝트(적이 없는 칸)는 노랑 테두리로 그린다. 경고 "아군 배치 구역 안에 오브젝트 n개 — 배치하는 순간 발동"을 더했다.
  - 격자 아래 설명을 글자 범례·우선순위·"앵커만"·"놓을 수 없는 칸(정확한 판정은 Core)"으로 고쳤다.
- 하지 않은 것:
  - 막힌 칸을 따로 칠하는 것. 어떤 지형이 막는지는 Core `BlocksVoluntaryEntry` 규칙이라 사본을 두지 않았다.
  - 장애물 크기·회전, 레일 표시.
- 확인 추가(Uc 9): 지형·장애물·오브젝트가 있는 스테이지(`Test` 등)에서 글자가 맞는 칸에 보이는지, 오브젝트를 구역 안에 칠하면 노랑 테두리와 경고가 나오는지.
- Uc(1~9) 확인 문제 없음(2026-10-28 사용자).

#### 7.4.2 PV-c 추가: 글자 대신 색 표기와 범례 (2026-10-28, 사용자 "이 안대로 바꿔", 컴파일 미확인)

- 질문(사용자): "검토 전에 저거 글자가 아니라 색으로 표기하고 색이 뭐 뜻하는지 되나?"
  - 돌던 묶음 끝 검토는 코드가 바뀔 예정이라 멈췄다. 바꾼 뒤 다시 돌린다.
- 바꾼 것(같은 파일):
  - 칸 바탕은 지금처럼 구역 색이다. 가운데 네모(칸의 55%, `MarkFraction`)는 가장 높은 층 배치의 색이다.
  - 배치 종류 열거 `GridMark` 열둘이 있고, 이름·색·층은 `InfoOf` 한 곳이다(인스펙터 전용 값):
    - 적 유닛 빨강·적 구조물 자주·본부 분홍
    - 장애물 갈색·오브젝트 노랑
    - 벽 회색·낭떠러지 검정·고지대 황토·약한 지반 주황·모래폭풍 모래색·토네이도 하늘색·위치 보정 초록
  - 범례 `DrawLegend`:
    - 격자 바로 아래에 "색 칸 + 이름"을 한 줄에 들어가는 만큼씩 나눠 그린다.
    - 구역 색 둘(지금 모양에 따라 "칸 지정 구역" 또는 "필드 줄 수 구역", 그리고 "구역 밖")과, 이 스테이지에 실제로 있는 배치 종류만 보인다.
  - 글자 그리기, 글자 스타일, 12px 제한은 걷었다. 색은 8px에서도 보인다.
  - 한 칸 하나·적 > 장애물 > 오브젝트 > 지형 우선순위, 경고 테두리(빨강 적·노랑 오브젝트), 앵커만 보는 규칙은 그대로다.
- 확인(Uc 10): 범례가 실제 있는 종류만 보이는지, 가운데 네모 색이 범례와 맞는지, 구역 안/밖이 바탕색으로 구별되는지.

### 7.5 PV-d 묶음 끝 검토·문서 (2026-10-28, 사용자 "Uc 문제 없음, PV-d")

- 문서:
  - 호진님 메모 새 파일 [player-zone-so-mode-hojin-share](../handoffs/2026-10-28-player-zone-so-mode-hojin-share.md)(미전달).
  - 설계 §10 구현 메모.
  - Core 전달 문서 §43의 "View 대응"·천명님 메모는 PV-b2 때 적었다.
- 검토 에이전트 하나(첫 실행은 색 표기 변경 전이라 멈췄고, 바꾼 뒤 다시 돌림, 읽기 전용).
  - 점수: SSOT 8.3 · SOLID 7.4 · FW 9.5 · 독립성 8.4 · 캡슐화 8.0 · C#/Unity 8.0 · If/Tick 8.6 = **평균 8.3**(설계 예상 9.1).
  - 높음 0, 중간 1, 낮음 12, 정보 1.
  - 색 표기 변경(§7.4.2)에서 컴파일 오류 후보는 없다고 봤다.
- 중간
  - M1 판 밖 칸 경고가 "다시 칠하세요"라고 하는데 칠해도 판 밖 칸이 남는다(`ApplyStroke`가 기존 목록에서 시작). 고치는 길은 "줄 구역으로 되돌리기"뿐이다.
- 낮음
  - L2 줄 구역 펼치기 반복문이 `Rows.Contains` 사본(원본 주석 없음).
  - L3 끄는 도중 인스펙터가 사라지면(`OnDisable`) Undo 묶음·`hotControl`을 정리하지 않는다.
  - L4 `GUI.enabled`를 보지 않아 읽기 전용 에셋도 칠해질 수 있다(추측).
  - L5 빠르게 끌면 칸을 건너뛴다.
  - L6 다시 그릴 때마다 할당(15×15에서는 문제없음).
  - L7 범례 순서 원본이 `LegendOrder`·`InfoOf` 둘, 판 밖 배치도 범례에 들어감.
  - L8 색 대비(낭떠러지 대 구역 밖 바탕, 토네이도 대 칸 지정 파랑, 노랑~갈색 계열 다섯, 오브젝트 색 = 구역 안 오브젝트 테두리 색).
  - L9 필드 이름 문자열이 바뀌면 `FindProperty` null → 인스펙터 전체 NRE.
  - L10 `playerDeploymentCells`의 머리말·툴팁이 사라지고 묶음이 Railway 뒤로 옮겨짐.
  - L11 PV-a null 검사 두 겹.
  - L12 `GameBootstrapEditorOverrides.cs:6`의 쓰지 않는 using(코드로 확인: `DevStartingItem`은 Boot 이름 공간).
  - L13 `GameBootstrap.CreateDevBattleSource` 같은 조건 반복.
- 정보
  - I14 EditorPrefs에 옛 Fixture 값(0)이 남은 PC는, 전투 스테이지가 Selected(키 0)나 PlacedStage(데이터 없음)였다면 예전에는 Fixture로 들어갔고 이제는 시작에서 예외로 멈춘다. 설정 창 Warning이 알린다.
- 반영 여부는 §7.6(사용자 "권장대로 반영").

### 7.6 검토 권장 반영 (2026-10-28, 사용자 "권장대로 반영", 컴파일 미확인, 새 시험 없음, 시험 변경 없음)

- M1 (b안): 칠할 때 판 밖 칸을 함께 버린다(중복 정리와 같은 방식). 판정은 Core `CellsOutside`. 경고 문구는 "다음에 격자를 칠하면 판 밖 칸은 지워집니다"로 바꿨다.
- L2: 줄 구역 대체 규칙을 `FallbackZone(rowDepth)` 한 함수로 모았다. 원본은 `BattleDefinitionAdapter`이고, 사본이라는 주석을 달았다. 펼치기는 그 값의 `Contains`로 판정한다.
- L3: 끌기 상태 `_strokeControl`과 `EndStroke()`를 두었다. 뗄 때와 `OnDisable`에서 `hotControl`을 놓고 Undo 묶음을 접는다.
- L4: `GUI.enabled`가 꺼져 있으면(읽기 전용) 누름을 받지 않는다.
- L5: 끌기에서 건너뛴 칸은 지난 칸에서 직선(브레젠햄, `CellsAfter`)으로 이어 칠한다.
- L7: 범례 순서는 열거 값을 층(`InfoOf`) 내림차순 → 선언 순서로 정렬해 만든다(손으로 쓴 배열 삭제). 범례에는 판 안에 놓인 종류만 넣는다(`_presentMarks`, 격자 그리기가 채움).
- L8: 색을 바꿨다.
  - 장애물 갈색, 오브젝트 흰색(노랑 경고 테두리와 갈림), 벽 회색, 고지대 올리브, 모래폭풍 모래색, 위치 보정 청록.
  - 배치 네모와 범례 배치 색 앞에 옅은 1px 테두리를 둘렀다(검정 낭떠러지가 구역 밖 바탕에 묻히지 않게).
  - 구역 색 범례에는 테두리를 두르지 않는다(격자의 구역 칸과 같게).
- L9: `FindProperty` 결과가 null이면 멈추지 않는다.
  - 칸 목록·필드 참조·필드 SO 값을 찾지 못하면 안내 상자를 보이고 격자를 그리지 않는다.
  - 배치 목록을 찾지 못하면 그 목록만 건너뛰고 "일부 배치가 보이지 않음" 경고를 보인다(`_marksIncomplete`).
- L10: 묶음 머리 툴팁을 데이터 칸의 `[Tooltip]`(`SerializedProperty.tooltip`)에서 읽는다. 위치(Railway 뒤)는 그대로 둔다.
- L12: `GameBootstrapEditorOverrides.cs`의 쓰지 않는 `using …ViewSystem.Dev`를 지웠다.
- L13: `GameBootstrap.CreateDevBattleSource`는 개발 진입이 꺼졌거나 전투 시작이 아니면 먼저 null을 돌려준다. 보스·지정 조건은 스테이지 값 비교 하나씩이다(동작 같음).
- 그대로 둔 것: L6(다시 그릴 때 할당, 15×15에서 문제없음), L11(PV-a null 검사 두 겹).
- I14는 기록만 한다. 옛 Fixture 값이 EditorPrefs에 남은 PC에서 전투 스테이지가 Selected(키 0)나 PlacedStage(데이터 없음)이면 시작에서 예외로 멈춘다. 설정 창 Warning이 알린다.
- Unity 확인(Uc 10과 함께):
  1. 판 밖 칸이 든 목록(필드를 줄여 만듦)에서 한 칸 칠하면 판 밖 칸과 경고가 사라진다.
  2. 빠르게 대각선으로 끌어도 칸이 끊기지 않는다. 끌기 한 번은 Undo 한 번이다.
  3. 읽기 전용 인스펙터(예: 버전 관리 잠금, 패키지 안 에셋)에서는 칠해지지 않는다.
  4. 머리 "아군 배치 구역"에 마우스를 올리면 데이터 칸 툴팁이 나온다.
  5. 낭떠러지·벽이 구역 밖 회색 바탕에서도 테두리로 구분된다. 오브젝트는 흰 네모이고, 구역 안이면 노랑 테두리다.
  6. 판 밖 앵커만 있는 종류는 범례에 나오지 않는다.
