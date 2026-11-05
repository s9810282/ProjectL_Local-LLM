# 아군 배치 구역 칸 단위 지정 + 적 배치 구역 제한 삭제 — 설계 초안

- 작성: 2026-10-27, Core 작업(천명)
- 상태: **결정 완료(2026-10-27 사용자: Q-PZ1 B, Q-PZ2~Q-PZ4 A, "Framework 시험은 건들지 마, Core·CoreIntegration 시험만").** 코드 착수 전. Task 계획은 [player-deployment-zone-plan](../plans/2026-10-27-combat-player-deployment-zone-plan.md).
- 요청(사용자): "아군 스폰 지역과 적군 스폰 지역이 줄로 되어 있는데 자유자재로 바꾸고 싶다. 적군 스폰 지역 제한은 삭제(전 지역 스폰 가능), 아군 스폰 지역만 제한을 둔다."
- 사용자 결정(2026-10-27):
  - ① **B** — 적이 놓이는 칸 = 판 전체에서 아군 구역을 뺀 나머지. 가중치 출현·고정 배치·전투 시작 재배치가 모두 같은 규칙이다.
  - ② **A** — 아군 칸 목록은 스테이지 데이터(`CombatStageData`)에 둔다. 비어 있으면 필드의 아군 줄 수로 대신한다.
  - ③ **A** — 아군 줄 수는 "칸 목록이 비었을 때 대신 쓰는 값"으로 남긴다. 적 줄 수는 읽는 곳을 없애고, 필드와 에셋 값은 그대로 둔다.
- 이 설계가 대신하는 것:
  - [배치 구역 칸 Core 요청](../handoffs/2026-10-27-combat-deployment-zone-cells-core-request.md)(View 쪽에서 온 요청)의 "진영별 칸 집합" 계약. 이번 결정으로 적 구역이 없어져 아군 칸 집합 하나가 된다.
  - 호진님 [진영 칸 설계 초안](2026-10-27-combat-stage-editor-so-mode-design-Hojin.md#7-보류-배치-진영-칸-단위-dz1dz6)(DZ1~DZ6, 보류)의 적 진영 부분. 아군 칸 목록을 스테이지 데이터에 두는 DZ1은 이 설계의 PZ5와 같다.

## 1. 지금 구조(코드로 확인한 사실)

### 1.1 값의 자리와 흐름

```text
FieldDataSO(아군 줄 수, 적 줄 수, 둘 다 ≥ 1)
  → FieldData → BattleDefinitionAdapter(:89-90 정의 생성, :105-106 적 출현 뒤 다시 생성)
  → StageConfig(검사: 두 값 > 0, 합 < 판 높이)
  → CombatPreparation(검사: 두 값 ≥ 0, 합 < 판 높이) → Buffer CombatStateData
  → CombatInputAssembler(:74-76) → CombatBattleFacts(두 값 ≥ 0) → 명령 계산
  → View CombatSceneDisplay(:199) → FieldView → FieldHighlightState(:146-147, 줄 비교 사본)
```

### 1.2 판정이 쓰이는 곳

| 시점 | 위치 | 아군 | 적 |
|---|---|---|---|
| 전투 준비 | `BattlePreparationCalculator.cs:242` `Place` | — | 고정 적 유닛(`:124`)·적 구조물·본부(`:144`)가 적 줄 밖이면 예외 |
| 전투 정의 생성 | `EnemySpawnCalculator.cs:51` | — | 가중치 출현 후보를 위 N줄에서 **직접** 고름(판정 함수를 거치지 않음) |
| 배치 국면 | `CombatPlacementCalculator.cs:30` `Preview` ← `Place` | 아군·메인 캐릭터를 놓을 칸이 아군 줄 안이어야 함 | — |
| 전투 시작(배치 → 전투) | `CombatPlacementCalculator.cs:160-164` `RedeployEnemies.Available` | — | 원위치·대체 칸이 적 줄 안이어야 함. 없으면 `EnemyRedeploymentBlocked` |

- 런타임 판정(배치 국면·전투 시작)은 `CombatPlacementCalculator.Zones()`(`:80-84`) 하나를 지난다.
- 판정 규칙은 `Domain/Placement/DeploymentZonePolicy.cs`에 있다. 아군은 `Y < 아군 줄 수`, 적은 `Y >= 높이 - 적 줄 수`, 중립은 늘 거절한다.
- **구역을 보지 않는 것:** 장애물(중립, `:157`의 `requireDeploymentZone: false`), 소환수 자리(소환자 주변 8칸), 밤 끝 회수(`RecallForNight`), 전투 국면의 이동·공격·넉백·적 AI.

### 1.3 Core 밖에서 줄 수를 쓰는 곳

| 영역 | 위치 | 쓰임 |
|---|---|---|
| View(용근님) | `FieldHighlightState.SetStaticZones:128-147`, `FieldView.ApplyStaticHighlights:162`, `CombatSceneDisplay.cs:199` | 아군 구역 색, 배치 국면 "놓을 수 있는 칸" 채우기, 적 구역 색 |
| 개발 진입(용근님) | `DevBattleInitializationSource.cs:37-61·116-121`, `GameBootstrap.cs:121-122·437·652-658`, `GameBootstrapEditorOverrides.cs:207-216` | Fixture 판 크기·줄 수, 데몬·본부 위치를 적 줄로 계산 |
| 도구 | SO 모드 `CombatLayoutWindow.SoMode.cs:1561-1612`(줄 수 슬라이더)·`:1921-1924`(`ZoneColor`·`ZoneName`), DT 모드 `CombatLayoutWorkbook.cs`·`전투스테이지 편집/model.js` | 맵 색, 줄 수 조절 |
| DB 툴팁 | `CombatStageData.cs:105·114` | "Player 배치 구역(맵 아래쪽)", "Enemy 배치 구역(맵 위쪽) 안이어야" |

### 1.4 시험 어셈블리별 사용 수

| 찾은 것 | Core | CoreIntegration | Framework(용근님) |
|---|---|---|---|
| `new StageConfig(` | 24곳(12개 파일) | 5곳 | 15곳(11개 파일) |
| `new CombatPreparation(` | 0 | 1곳 | 38곳(31개 파일) |
| `new CombatBattleFacts(` | 46곳(29개 파일) | 0 | 7곳(6개 파일) |
| `…DeploymentDepth` 속성 읽기 | 6곳(3개 파일) | 4곳 | 16곳(9개 파일) |

Framework 시험 어셈블리는 용근님이 계속 고치는 코드다(최근 `c40fd37b`). Core 작업에서는 고치지 않는다. 그래서 이 어셈블리의 컴파일을 깨는지가 API 모양(Q-PZ1)을 정한다.

## 2. 규칙 (정책 원본)

| 대상 | 놓을 수 있는 칸 |
|---|---|
| 아군·메인 캐릭터(배치 국면) | **아군 구역 칸**이면서, 지금처럼 판 안·자발 진입 가능(벽·낭떠러지 아님)·다른 유닛과 살아 있는 구조물이 없는 칸 |
| 고정 적 유닛, 적 구조물·본부(전투 준비) | **판 안이면서 아군 구역이 아닌 칸**. 점유 겹침 검사는 그대로 |
| 가중치 적 출현 후보 | **아군 구역이 아닌 칸**. 예약 칸(구조물·장애물)과 진입 불가 지형을 빼는 것은 그대로 |
| 전투 시작 적 재배치(원위치·대체 칸) | **아군 구역이 아닌 칸**. 나머지 조건과 대체 칸 정렬(거리 → Y → X)은 그대로 |
| 장애물·소환수·전투 국면 | 바뀌지 않음(구역을 보지 않음) |

- **아군 구역:** 칸 집합 하나다. 1칸 이상이고, 모든 칸이 판 안이며, 같은 칸이 두 번 들어가지 않는다. 모양은 자유롭다(떨어진 조각이 여럿이어도 되고, 한 칸도 된다).
- **적 구역은 없다.** "적 줄 수", 그리고 "두 줄 수의 합 < 판 높이" 검사는 없앤다.
- 이 표가 규칙의 원본이다. 코드의 원본은 `DeploymentZonePolicy` 한 곳이다(PZ2).

## 3. 설계

### PZ1 Core 구역 값 `PlayerDeploymentZone`

- 자리: `CoreCaculator/Contracts/Common/PlayerDeploymentZone.cs`(새 파일, 순수 C#, sealed 불변 값).
- 모양은 둘 중 하나다. 판 너비 없이 판정할 수 있게 하려는 것이다(`CombatBattleFacts`는 판 크기를 모른다).
  - **줄 구역** `Rows(int depth)`: "아래부터 depth줄"(`y < depth`). 어댑터의 대체 규칙(PZ5)과, 줄 수를 받는 옛 생성자(PZ6)가 만든다.
  - **칸 구역** `Of(IEnumerable<CellCoord> cells)`: 저작한 칸 목록. 만들 때 **y → x 순서로 정렬해 복사**하고, 중복은 거절한다.
- 멤버:
  - `bool Contains(CellCoord cell)` — 판정의 유일한 재료다.
  - `bool IsEmpty` — 줄 수 0, 또는 칸 0개.
  - `IReadOnlyList<CellCoord> CellsOutside(int width, int height)` — 칸 구역에서 판 밖 칸. 줄 구역이면 빈 목록이다. DB 저작 검사가 오류 문구에 좌표를 넣을 때 쓴다.
  - `bool FitsBoard(int width, int height)` — 줄 구역은 `depth ≤ height`, 칸 구역은 `CellsOutside`가 비었는지 본다. "판 안" 규칙의 원본은 이 둘이다.
  - `int? RowDepth` — 줄 구역이면 줄 수, 칸 구역이면 null. 옛 속성(PZ6)만 읽는다.
  - `IReadOnlyList<CellCoord> ListedCells` — 칸 구역의 칸(y → x). 줄 구역이면 빈 목록이다.
  - `static PlayerDeploymentZone Empty` — `Rows(0)`.
- "판 안" 검사는 판 크기를 아는 `StageConfig`·`CombatPreparation` 생성자가 한다.
- 빈 구역은 값으로는 만들 수 있다(`CombatPreparation`·`CombatBattleFacts` 기본값 자리). "1칸 이상" 검사는 `StageConfig`가 한다. 지금 "줄 수 ≥ 1"과 같은 자리다.

### PZ2 판정 원본 `DeploymentZonePolicy`

- 생성자: `DeploymentZonePolicy(PlayerDeploymentZone zone)`. 판 높이 인자는 없어진다.
- `CanDeploy(owner, cells)`:
  - Player: 모든 칸이 `zone.Contains`.
  - Enemy: **어느 칸도** `zone.Contains`가 아님. "판 안" 검사는 부르는 쪽이 지금처럼 한다(`Place`의 범위 검사, `IsInside`).
  - Neutral: 거절(그대로). null 칸: 거절(그대로).
- 부르는 곳 넷이 모두 이 함수를 쓴다. `EnemySpawnCalculator`의 줄 반복문(§1.2 둘째 줄)도 이 함수를 부르게 바꿔 사본을 없앤다(PZ3).

### PZ3 적 출현 후보

- `EnemySpawnCalculator.cs:51-58`: 반복 범위를 `y = 0 .. MapHeight-1`, `x = 0 .. MapWidth-1`(y → x)로 넓히고, 조건에 `policy.CanDeploy(Owner.Enemy, cell)`을 더한다. 예약 칸·진입 불가 지형을 거르는 조건은 그대로 둔다.
- 후보를 도는 순서는 y → x 그대로다. 다만 후보 집합이 넓어지므로 **같은 시드여도 출현 자리가 바뀐다**(§5, Q-PZ4).
- "빈 칸이 부족합니다" 예외는 그대로다. 후보가 넓어져서 지금보다 드물게 난다.

### PZ4 전달 경로

| 층 | 파일 | 지금 | 바꿀 것 |
|---|---|---|---|
| Contracts | `Commands/StageConfig.cs` | 줄 수 두 속성, 생성자 6개, 검사 `:156-165` | `PlayerDeploymentZone PlayerZone` 하나. 검사는 "1칸 이상·모든 칸이 판 안"으로 바꾸고, 적 줄 수 검사와 합 검사는 없앤다 |
| Contracts | `Combat/BattlePreparation.cs`(`CombatPreparation`) `:22-23·34·53-55·84-85` | 줄 수(기본 0), 합 검사 | `PlayerZone`(기본 빈 구역), "모든 칸이 판 안" 검사 |
| Contracts | `Combat/CombatBattleFacts.cs` `:17-18·29·34·38` | 줄 수 | `PlayerZone`(판 크기는 모름, 칸 값 그대로) |
| Application | `BattlePreparationCalculator.cs:184·242-243` | 줄 수를 넘겨 정책을 만듦 | 구역을 넘김 |
| Application | `CombatPlacementCalculator.cs:80-84` | `Zones()`가 줄 수 + `BoardHeight` | `new DeploymentZonePolicy(battle.PlayerZone)` |
| Application | `EnemySpawnCalculator.cs:51` | 위 N줄을 직접 돎 | PZ3 |
| CoreSystem | `BattleDefinitionAdapter.cs:89-90·105-106` | FieldData 줄 수 | PZ5 규칙으로 구역을 만들어 넘김. `:105-106`은 구역을 그대로 옮김 |
| CoreSystem | `CombatInputAssembler.cs:74-76` | Buffer 줄 수 | Buffer 구역 |
| Buffer | `CombatStateData.cs:27-28·51-52·67-68` | 줄 수 두 속성 | `PlayerZone` 하나(View는 이 칸으로 칠함, DB 규칙 P6) |

### PZ5 칸 입력 자리(DB)와 대체 규칙

- `CombatStageData`(SO)에 `[SerializeField] CellInput[] playerDeploymentCells`를 더한다. 헤더는 "Deployment"이고, 툴팁은 "비우면 필드의 아군 배치 줄 수(아래부터)를 씁니다"다.
- `CombatStageDetail`(순수 Data)에 `IReadOnlyList<CellCoord> PlayerDeploymentCells`를 더한다. 생성자 끝 선택 인자, 기본값은 빈 목록이다(P1·P4).
- 저작 검사: `CreateStageData`가 필드를 아는 자리(`:178-182`)에서 칸이 판 안인지, 중복이 없는지 본다. 위반하면 부트 DB 생성 때 멈춘다. 지금 환경 검사와 같은 방식이다.
- **대체 규칙의 원본은 어댑터 한 곳이다:** `BattleDefinitionAdapter`가 칸 목록이 있으면 그 칸으로, 비었으면 `PlayerDeploymentZone.Rows(field.PlayerDeploymentDepth)`로 구역을 만든다.
- `FieldData`·`FieldDataSO`는 바꾸지 않는다(③A). 적 줄 수 필드와 그 검사(≥ 1)는 남지만 Core는 읽지 않는다. 필드 이름 옆에 "Core가 읽지 않음" 툴팁 한 줄을 더하는 것은 DB 담당과 정한다(Q-PZ2).
- 툴팁 `CombatStageData.cs:105·114`를 §2 규칙대로 고친다("아군 배치 구역 안", "아군 배치 구역 밖").

### PZ6 API 모양 — 줄 수 생성자·속성을 어떻게 할지

**결정: B 전환 입구를 둔다(2026-10-27 사용자).**

이유: Unity는 어느 한 어셈블리라도 컴파일 오류가 나면 전체가 멈춘다. 그런데 Framework 시험(고치지 않음, 사용자 지시)·View·개발 진입이 줄 수 생성자와 속성을 쓴다(§1.4, Framework 시험만 최대 76곳). 이 이름을 지우면 Core 시험도 돌릴 수 없다. 처음 권장한 A(이름 삭제)는 이 조건에서 쓸 수 없다.

- **Core 로직은 `PlayerZone`만 읽는다.** 판정·출현·재배치·준비 계산은 줄 수 속성을 읽지 않는다.
- **새 생성자:** 줄 수 두 인자 자리에 `PlayerDeploymentZone playerZone` 하나를 받는다. 어댑터와 앞으로 새로 쓰는 코드는 이것을 쓴다.
- **옛 생성자(전환 입구):** 그대로 남긴다. 아군 줄 수로 `Rows(playerDepth)`를 만들고, 적 줄 수 인자는 **받기만 하고 쓰지 않는다.** 옛 검사 "적 줄 수 > 0"과 "합 < 높이"는 없앤다.
- **옛 속성(전환 입구):**
  - `PlayerDeploymentDepth` = `PlayerZone.RowDepth ?? 0`. 따로 저장하지 않고 구역에서 계산한다(P7).
  - `EnemyDeploymentDepth` = 늘 0. 적 구역이 없다는 사실을 그대로 드러낸다.
  - 옛 속성을 읽어 계약을 다시 만드는 시험은 줄 구역이면 같은 구역으로 돌아온다. 칸 구역이면 아군 줄 수 0이 되어 실행에서 깨진다. 지금 에셋에는 칸 구역이 없고, Core·CoreIntegration 시험 중 이 경로는 줄 구역만 쓴다(계획에서 확인).
- 문서 주석에 "전환 입구 — PZ-d에서 지움, Core 로직은 쓰지 않음"을 단다. `[Obsolete]`는 붙이지 않는다. 바깥 수십 곳에 경고가 쏟아져 다른 경고를 가리기 때문이다.
- **PZ-d:** View·개발 진입이 새 값으로 옮긴 뒤 옛 입구를 지운다. Framework 시험을 계속 안 쓰면 그때 그 어셈블리를 어떻게 할지 함께 정한다.

### PZ7 Core 밖 영향 (다른 작업자 영역, 이 설계에서는 고치지 않음)

| 영역 | 해야 할 것 | 담당 |
|---|---|---|
| View 구역 색 | `SetStaticZones`가 줄 비교 대신 Buffer `PlayerZone.Contains`로 칠한다. `FieldStaticZone.EnemyZone`과 적 구역 색을 걷는다(적 구역이 없음) | 용근님 |
| 개발 진입 | Fixture가 `Rows(w, 아군 줄 수)`를 넘긴다. 적 줄 수 설정(씬 값·EditorPrefs·설정 창)은 데몬·본부 위치 계산에만 남기거나 걷는다 | 용근님 |
| SO 모드 도구 | 적 줄 수 슬라이더를 걷는다. 맵 색과 구역 이름은 "아군 구역 / 나머지"로 바꾼다. 아군 칸 붓은 호진님 초안 DZ5에서 아군 붓만 남긴 형태다 | 도구 담당(SO 모드는 용근·호진님, 창은 민형님) |
| DT 모드·엑셀 도구 | 줄 수 표시를 정리한다 | 민형님 |
| 안내 문서 | `guides/unit-movement-attack-core-handoff.md` §5-2와 `:422-423`의 "아래·위 N줄" 설명 | 이쪽(Core) |

B로 정해서 위 작업은 Core 커밋과 묶이지 않는다. Core만 들어가도 컴파일되고 게임 규칙은 바뀐다.

View를 고치기 전 화면은 이렇다(§5).
- 지금 스테이지(줄 구역): 아군 색과 놓을 수 있는 칸 표시는 맞다. 적 구역 색은 사라진다(`EnemyDeploymentDepth` = 0).
- 칸 구역 스테이지: 아군 줄 수가 0으로 읽혀 **아군 색과 놓을 수 있는 칸 표시가 나오지 않는다.** 그래서 칸 구역 스테이지를 실제로 쓰기 전에 View 수정이 필요하다.

## 4. 바꾸지 않는 것

- 아군 배치의 나머지 조건(국면·메인 잠금·진입 불가 지형·점유), 메인 캐릭터 규칙.
- 소환수 자리(소환자 주변 8칸). 지금도 구역을 보지 않는다. 이번에 아군 구역을 빼는 조건도 더하지 않는다. 소환은 배치가 끝난 뒤라 아군 배치를 막지 않는다.
- 밤 끝 회수, 전투 국면 규칙, 장애물 배치.
- 재배치 대체 칸 정렬(거리 → Y → X), 출현 후보 순서(y → x)와 섞는 방식.
- `FieldData`·`FieldDataSO`의 값과 검사(③A).

## 5. 보이는 동작 변화

| 상황 | 지금 | 바뀐 뒤 |
|---|---|---|
| 지금 스테이지의 아군 배치(칸 목록 비어 있음) | 아래 2줄 | **같음**(대체 규칙) |
| 가중치 적 출현 | 위 2줄에서 추첨 | 아래 2줄을 뺀 전체에서 추첨. **아군 구역 바로 앞 줄에도 나온다.** 같은 시드여도 자리가 달라진다 |
| 고정 적·본부 | 위 2줄 밖이면 예외 | 아군 구역 안일 때만 예외. 지금 에셋은 모두 통과한다(모두 위 줄) |
| 전투 시작 적 재배치 | 원위치가 막히면 위 2줄 안의 가까운 빈칸, 없으면 거절 | 아군 구역 밖의 가까운 빈칸. 거절이 드물어진다 |
| View 적 구역 색 | 위 2줄 | 사라진다(옛 속성 `EnemyDeploymentDepth` = 0). View 코드는 그대로다 |
| View 아군 색·놓을 수 있는 칸(칸 구역 스테이지) | — | View가 옮길 때까지 **나오지 않는다**(PZ7) |

## 6. 시험 영향 (Core·CoreIntegration만)

기대가 바뀌는 시험(모두 천명 작성)은 **확인하는 내용은 그대로 두고 설정만 맞춘다.**

| 시험 | 지금 기대가 기대는 것 | 맞춤 |
|---|---|---|
| `EnemySpawnCalculationTests.Cliffs_AreExcludedBeforeCountingCapacity`(8·9) | 8×6에서 적 구역 = y4~5 | 아군 줄 수를 4로 → 적 칸 = y4~5. 판·낭떠러지·수는 그대로 |
| `EnemySpawnCalculationTests.Obstacles_ReserveRotatedFootprintBeforeCountingCapacity`(14·15) | 같음(적 칸 16) | 같은 맞춤 |
| `CombatCommonCommandTests.EnemyRedeployment_ReservesOriginsBeforeFallback_AndRejectsFullZone`(blocked) | 3×8, 적 구역 = y7 한 줄 | `CombatBattleFacts`의 아군 줄 수를 7로 → 적 칸 = y7 |
| `WallMovementTests.EnemyRedeployment_ExcludesWallsAndRejectsWhenNoCellRemains` | 8×8, 적 구역 = y7 | 같은 맞춤(2 → 7). 외부 검토로 추가(계획 §3) |
| `WeakGroundMovementTests.EnemyRedeployment_ExcludesCliffsAndDoesNotInventAnAvailableCell` | 같음 | 같은 맞춤(2 → 7) |

- 그대로 통과할 것으로 보는 시험:
  - `CombatRewardParticipationTests.RejectedRedeployment_DoesNotInstallProposedCapture`: 1×2 판이라 적 칸이 (0,1) 하나뿐이고, 그 칸을 본부가 차지한다.
  - `EnemyRedeploymentResolverTests.PaddedSingleCell…:56`: (0,4)는 아군 구역 밖이다. 다만 정책 생성자 호출은 바뀐다.
- 새 시험: 계획 §4(Core 단위 시험 + CoreIntegration 전달 경로 시험).
- 운영 DB로 출현 결과(자리)를 단언하는 Core·CoreIntegration 시험은 grep으로 찾지 못했다. 계획 단계에서 전수 확인한다.
- Framework 시험: 고치지도 돌리지도 않는다(사용자 지시). 옛 입구로 컴파일만 유지한다(PZ6).
- 옛 생성자를 쓰는 Core·CoreIntegration 시험 호출(최대 86곳)은 B라서 고치지 않는다. 새 생성자로 옮기는 것은 PZ-d에서 한다.

## 7. Phase (안)

1. **PZ-a Core 구역 값·판정·적 제한 삭제:** PZ1~PZ4. 어댑터는 아직 `Rows(필드 아군 줄 수)`만 넘긴다. 이 단계가 끝나면 아군 배치는 같고, 적 쪽은 §5대로 바뀐다. 시험 맞춤(§6)도 여기 들어간다.
2. **PZ-b DB 칸 목록:** PZ5(SO 칸·Data·저작 검사·어댑터 대체 규칙·툴팁).
3. **PZ-c 문서·공유:** Core 안내 문서, Core 전달 문서 §43에 응답, 용근·호진·민형님 공유 메모(PZ7).
4. **PZ-d 전환 입구 걷기(나중, 이번 계획 밖):** View·개발 진입이 옮긴 뒤 줄 수 생성자·속성을 지운다.

Phase마다 멈추고 보고한다. 컴파일·실행 확인은 사용자가 Unity Test Runner로 한다.

## 8. 원칙 점검

- **SSOT:**
  - 판정 규칙은 `DeploymentZonePolicy` 한 곳이다. 출현 반복문 사본은 없앤다.
  - 대체 규칙(빈 목록 → 줄 수)은 어댑터 한 곳이다.
  - View의 줄 비교 사본은 Buffer 구역으로 바뀐다(PZ7). 도구는 Core를 부를 수 없어 대체 규칙 사본 하나가 남는다. 원본 자리를 주석으로 단다.
- **SRP:** 저작 검사는 DB, 구역 결정은 어댑터, 판정은 정책, 칠하기는 View가 맡는다.
- **변경 독립성:** 칸 목록이 비면 아군 쪽은 지금과 같다. 에셋을 옮길 필요가 없다. 적 쪽 변화는 사용자가 의도한 것이다(①B).
- **변경 영향:** 옛 입구로 바깥 컴파일은 지킨다. 기대가 바뀌는 시험은 §6의 다섯이다. 남는 비용은 전환 입구 자체다. 받기만 하는 인자와 계산 속성이 PZ-d까지 남는다.
- **FW:** Framework·GlobalSystem 변경 없음.

## 9. 결정 (2026-10-27 사용자)

| 번호 | 질문 | 결정 |
|---|---|---|
| Q-PZ1 | API 모양 | **B 전환 입구**(PZ6). 처음 권장은 A였으나 Framework 시험·View 컴파일을 지켜야 해서 바꿈 |
| Q-PZ2 | DB 칸 목록(PZ5)을 누가 넣나 | **A 이 Core 작업에 포함**(`CombatStageData`·`CombatStageDetail`, DB 담당에게 공유) |
| Q-PZ3 | 고정 적·적 구조물이 아군 구역 안에 있으면 | **A 지금처럼 전투 진입 때 예외**(①B와 같은 규칙) |
| Q-PZ4 | 가중치 출현이 아군 구역 바로 앞 줄까지 나오는 것 | **A 그대로 받아들임**(출현 자리 조절은 아군 구역 모양으로) |

## 10. 구현 메모 (2026-10-27, PZ-a·PZ-b·PZ-c 완료, Core·CoreIntegration 시험 통과)

계획에서 달라진 점과 구현으로 확인한 사실이다. 원본 기록은 [계획 §8](../plans/2026-10-27-combat-player-deployment-zone-plan.md).

- **구역 값:** `PlayerDeploymentZone`에 `CellsOutside`를 더했다. 외부 검토 반영으로, DB 오류 문구에 좌표를 넣으려는 것이다. "판 안" 규칙은 `FitsBoard`와 이 함수 한 곳에만 있다.
- **옛 생성자 예외 형식:** `CombatBattleFacts` 옛 생성자는 음수 줄 수일 때 예전과 같은 `ArgumentException`을 던진다.
- **정책:** null 칸 검사를 지웠다. `CellCoord`가 구조체라 늘 거짓이던 검사다.
- **기대가 바뀐 시험:** 셋이 아니라 다섯이었다. 벽·약한 지반 재배치 시험 둘은 외부 검토가 찾았다.
- **CoreIntegration 전달 경로 시험:** 진입은 `ConfiguredMovementBattle`을 쓰지 않고 같은 순서로 따로 거친다. 그 틀은 정해진 칸에 배치까지 마쳐서 칸 구역과 맞지 않는다.
- **저작 검사 문구:** 스테이지 키 대신 에셋 이름을 쓴다. 키는 `StageRootDataSO`에 있다.
- **시험 결과:** Core 3749/3749 통과, CoreIntegration 169/170 통과.
  - 실패 하나(`BossStagePreparationTests`)는 이 작업 전부터 있던 데이터 문제다. 재령님 `ba9b05fe`가 Root 표에서 보스 스테이지 2101을 뺐다.
- **남은 것:**
  - View·도구 쪽 옮기기(PZ7)는 공유 메모로 넘겼다.
  - 전환 입구 걷기(PZ-d)는 View가 옮긴 뒤에 한다.
