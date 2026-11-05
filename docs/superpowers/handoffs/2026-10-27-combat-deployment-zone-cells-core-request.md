# 전투 배치 구역 칸 단위 지정 — Core 요청

- 작성: 2026-10-27, View 작업
- 받는 사람: 천명님(Core)
- 상태: **대신함(2026-10-28 확인).** 천명님이 다른 결정으로 구현했다(`5a1bfbe1`, 2026-10-27). 적 배치 구역은 없애고, 아군 배치 구역만 칸 집합 하나로 두었다. 원본은 천명님 [아군 배치 구역 설계](../specs/2026-10-27-combat-player-deployment-zone-design.md)(PZ1~PZ7)이고, Core 응답은 [Core 전달 문서 §43](2026-09-24-view-core-data-boundary-handoff.md#deployment-zone-cells-request)에 있다. View 대응은 [아군 배치 구역 View 설계](../specs/2026-10-28-combat-player-zone-view-design.md)다. 아래 본문은 요청 당시 기록으로 남긴다.
- 범위: Core·Buffer 계약만 다룬다. 기획자가 칸을 저작하는 자리(어느 SO에 둘지)와 편집 도구, View 표시는 이 요청에 넣지 않는다(2026-10-27 사용자 "View 계획 폐기하고 Core 요청 문서만").
- 기록 위치: [Core 전달 문서 §43](2026-09-24-view-core-data-boundary-handoff.md#deployment-zone-cells-request)
- 관련: 호진님 [진영 칸 설계 초안](../specs/2026-10-27-combat-stage-editor-so-mode-design-Hojin.md#7-보류-배치-진영-칸-단위-dz1dz6)(DZ1~DZ6, 보류). 다른 점은 §6에 적었다.

## 1. 요청 요지

배치 구역을 "아군 = 아래 N줄, 적 = 위 N줄"이 아니라 **칸 단위로 자유롭게** 정할 수 있게 해 달라.

- 기획 요청: 적 구역을 가운데 한 칸만 두거나, 아군·적 구역을 원하는 자리에 둘 수 있어야 한다.
- Core는 구역을 "깊이 두 값"이 아니라 **"진영별 칸 집합"**으로 받아 판정해야 한다. 이 계약 변경이 이번 요청이다.
- 지금 데이터는 모두 깊이 값이다. 그래서 첫 단계에서는 깊이로 구역을 만들어 넘긴다. **바꾼 직후 게임 동작은 지금과 같아야 한다.**
- 칸을 저작하는 자리가 정해지면, 어댑터가 깊이 대신 그 칸을 넘긴다(§5).

## 2. 지금 Core가 구역을 쓰는 곳

| 시점 | 위치 | 하는 일 |
|---|---|---|
| 전투 정의 생성 | `EnemySpawnCalculator.cs:51` | 적 무작위 출현 후보 칸을 위 N줄에서 직접 고름. 판정 함수를 거치지 않음 |
| 전투 준비 | `BattlePreparationCalculator.cs:242` (`Place`) | 초기 적(보스 포함, `:124`)과 적 구조물(본부, `:144`)이 적 구역 안인지 검사. 장애물(`:157`)은 검사하지 않음. 메인 캐릭터는 여기서 검사하지 않음 |
| 배치 국면 (첫 배치, 밤이 끝난 뒤 다시 배치) | `CombatPlacementCalculator.Preview` (`:30`) ← 배치 미리보기·배치 명령 `Place` | 아군·메인 캐릭터를 놓을 칸이 아군 구역인지 판정 |
| 전투 시작 명령 (배치 → 전투) | `CombatTurnCalculator.cs:42` → `RedeployEnemies` (`CombatPlacementCalculator.cs:164`) | 대기로 빠진 적을 원위치로 되돌림. 원위치에 놓을 때도 적 구역 판정을 거침. 원위치가 막히면 적 구역 안에서 가장 가까운 빈칸으로 옮기고, 없으면 `EnemyRedeploymentBlocked`로 거절 |

- 런타임 판정 셋은 모두 `CombatPlacementCalculator.Zones()`(`:80-84`) 하나를 지난다.
- 밤이 끝날 때 적을 대기로 빼는 `RecallForNight`는 구역을 보지 않는다.
- 전투 국면의 이동·공격·넉백·적 AI와 소환수 자리(소환자 주변 8칸)도 구역을 보지 않는다.
- Buffer `CombatStateData`가 깊이 두 값을 내고, View가 그 값으로 구역 색을 칠한다(`FieldHighlightState.cs:146-147`의 `y < 깊이` 계산).

## 3. 요청하는 계약 (제안 — 이름·모양은 Core가 정한다)

### 3.1 구역 값

- **진영별 칸 집합:** 아군 칸, 적 칸. Neutral에는 구역이 없다.
- **만드는 입구 둘(제안):**
  - 칸으로 만들기: 저작 데이터에서 온다.
  - 깊이로 만들기: 지금 방식이다. 지금 데이터·개발 진입·시험이 쓴다.
    - `CombatBattleFacts`에는 판 크기가 없다. 그래서 깊이로 만든 구역은 판정할 때 판 크기를 받아야 한다. 판정은 지금처럼 명령 상태의 `BoardHeight`를 쓴다.
- **묻는 입구 둘(제안):**
  - 칸 하나가 그 진영 구역인가.
  - 그 진영 구역 칸 목록. y → x 순서로 돌려준다(§3.4).
- **규칙 원본은 한 곳이다.** 구역 값 또는 `DeploymentZonePolicy` 중 하나가 판정 규칙을 갖는다.
- **Buffer로 내 달라.** View는 규칙 값을 Buffer로만 받는다(DB 규칙 P6). 지금 View가 깊이로 다시 계산하는 구역을 Buffer의 구역 값에 물어 칠할 수 있어야 한다.

### 3.2 검사 규칙 (2026-10-27 사용자 결정)

- 칸은 판 안에 있어야 한다.
- **겹침을 허용한다.** 한 칸이 아군 구역과 적 구역에 동시에 속할 수 있다. 겹친 칸은 아군 배치 후보이면서 적 출현·재배치 후보다. 같은 칸을 동시에 쓰는 충돌은 지금처럼 점유 검사가 막는다.
- **떨어진 조각(섬 여러 개)을 허용한다.** 한 칸짜리 구역도 된다.
- `StageConfig`는 양쪽 모두 1칸 이상이어야 한다. 지금 "깊이 ≥ 1"과 같은 뜻이다.
- `CombatPreparation`·`CombatBattleFacts`는 지금처럼 빈 구역을 허용한다. 깊이 기본값 0이 빈 구역에 해당한다.
- "깊이 합 < 높이" 검사는 없앤다. 겹침을 허용하므로 대신할 검사가 없다.

### 3.3 바꿀 곳 (Core·Buffer 파일)

| 층 | 파일 | 지금 | 바꿀 것 |
|---|---|---|---|
| 새 값 | (새 파일, Contracts) | — | §3.1의 구역 값 |
| Domain | `Domain/Placement/DeploymentZonePolicy.cs` | 생성자 `(playerDepth, enemyDepth, mapHeight)`, `Y` 비교 | 구역 값을 받아 칸으로 판정. Neutral 거절·null 칸 거절은 그대로 |
| Contracts | `Contracts/Commands/StageConfig.cs` `:17-18`, 생성자 6개, 검사 `:156-165` | 깊이 두 속성, "깊이 합 < 높이" | 깊이 속성 → 구역 값. 검사 → §3.2 |
| Contracts | `Contracts/Combat/BattlePreparation.cs` (`CombatPreparation`) `:22-23`, `:34`, `:53-55`, `:84-85` | 깊이(기본 0), 같은 검사 | 같은 방식. 빈 구역 허용 유지 |
| Contracts | `Contracts/Combat/CombatBattleFacts.cs` `:17-18`, `:29`, `:34`, `:38` | 깊이 | 같은 방식 |
| Application | `Application/Combat/BattlePreparationCalculator.cs` `:184`·`:242-243` | 깊이를 넘겨 정책을 만듦 | 구역 값을 넘김 |
| Application | `Application/Combat/CombatPlacementCalculator.cs` `:80-84` | `Zones()`가 깊이로 정책을 만듦 | 구역 값으로. 런타임 판정 셋이 함께 바뀐다 |
| Application | `Application/Combat/EnemySpawnCalculator.cs` `:51` | 위 N줄을 직접 돎 | 적 구역 칸 목록을 돎 |
| CoreSystem | `CombatSystem/BattleDefinitionAdapter.cs` `:89-90`·`:105-106` | FieldData 깊이를 넘김. `:105-106`은 적 출현 뒤 `StageConfig`를 다시 만듦 | 이번에는 깊이로 만든 구역(동작 같음). `:105-106`은 구역을 그대로 옮김 |
| CoreSystem | `CombatSystem/CombatInputAssembler.cs` `:74-76` | Buffer 깊이를 명령 입력에 넘김 | Buffer 구역을 넘김 |
| Buffer | `BufferSystems/Combat/CombatStateData.cs` `:27-28`, `:51-52`, `:67-68` | 깊이 두 속성 | 구역 값 하나 |

**생성자를 더할 때 주의할 점:** `CombatPreparation`는 깊이 뒤의 인자가 모두 선택 인자다. 구역을 선택 인자로 받는 오버로드를 더하면, 깊이를 생략한 기존 호출이 두 오버로드 사이에서 모호해진다(CS0121). 새 오버로드에서는 구역 인자를 필수로 받거나, 기존 서명과 인자 수·타입이 겹치지 않게 해 달라.

### 3.4 동작이 같아야 하는 조건

- **적 출현 후보 순서는 y → x다.** 지금은 후보를 y → x 순서로 만든 뒤 같은 난수로 섞는다. 칸 목록 순서가 같아야 같은 시드에서 같은 출현 결과가 나온다.
- **적 재배치 후보 정렬은 그대로다:** 거리 → Y → X(`CombatPlacementCalculator.cs:139`).
- **깊이로 만든 구역은 지금 판정과 칸마다 같은 답을 낸다.** 깊이 0이면 빈 구역이다.

## 4. 호환 — 호출처와 시험

깊이 입구(생성자)를 남기면 아래 호출은 **그대로 컴파일되고 동작도 같다.**

| 생성 | 시험 | 운영·개발 |
|---|---|---|
| `new StageConfig(` | 43곳(27개 파일) | 어댑터 2곳, 개발 진입 `DevBattleInitializationSource.cs:121` 1곳 |
| `new CombatPreparation(` | 38곳(31개 파일) | `BattlePreparationCalculator` 1곳 |
| `new CombatBattleFacts(` | 52곳(34개 파일) | `CombatInputAssembler` 1곳 |

**깊이 속성을 읽어 계약을 다시 만드는 곳 10곳(모두 천명님 시험)**은 속성이 바뀌면 호출만 맞추면 된다. 구역 값을 그대로 넘기고, 확인하는 내용은 바꾸지 않는다.

- `CoreIntegration/ImpactBoltBattleSource.cs:51`
- `ExperienceAttributionPathTests.cs:87`
- `Framework/BossReactionPublicationTests.cs:122`
- `Framework/CharacterBufferTestData.cs:82`·`:118`
- `Framework/CombatCommonPublicationTests.cs:268`
- `Framework/CoreStageCombatCycleTests.cs:854`
- `Framework/EnvironmentEntryPublicationTests.cs:230`
- `Framework/RunDeckBattleMembershipTests.cs:185`
- `EnemyRedeploymentResolverTests.cs:56`(정책 생성자)

경로는 모두 `Assets/Tests/EditMode/` 아래다. 깊이 입구까지 없애면 위 133곳과 개발 진입을 모두 고쳐야 한다. 그래서 깊이 입구는 남기기를 권장한다(§6 Q3).

FieldData 깊이를 직접 읽거나 쓰는 시험(`BossStageAuthoringTests.cs:101`, `BattleDefinitionAdapterTests.cs:300-301`)은 DB 쪽 값이라 이 변경과 상관없다.

## 5. 순서

1. **Core 계약 교체(이 요청).** 어댑터는 FieldData 깊이로 구역을 만든다. 동작은 같다. 속성을 바꾸는 순간 사용처가 모두 함께 깨지므로 이 단계는 한 번에 바꿔야 한다. 천명님 시험 10곳의 호출 맞춤도 이 단계에 들어간다.
2. **칸 입력 연결(이 요청 밖).** 칸을 저작하는 자리가 정해지면 `BattleDefinitionAdapter.cs:89-90`이 깊이 대신 그 칸으로 구역을 만든다. 저작 자리·도구·View 표시는 따로 정한다.

## 6. 확인 요청

- **Q1 검사 규칙:** 겹침 허용, 떨어진 조각 허용, `StageConfig`는 양쪽 1칸 이상(§3.2)이 사용자 결정이다. Core 규칙상 문제가 있으면 알려 달라.
- **Q2 작은 적 구역:** 적 구역이 작으면 전투 시작 때 재배치할 칸을 못 찾아 `EnemyRedeploymentBlocked`로 거절될 수 있다. 저작 단계에서 최소 기준을 막아야 하는지, 지금처럼 런타임 거절로 둘지 알려 달라. 기준이 필요하면 그 규칙을 알려 달라.
- **Q3 깊이 입구:** 깊이로 구역을 만드는 생성자를 남겨도 되는가(§4).
- **Q4 안내 문서:** `guides/unit-movement-attack-core-handoff.md` §5-2와 `:422-423`의 배치 구역 설명("아래·위 N줄")을 갱신해 달라.
- **Q5 일정:** 천명님 일정이 맞지 않으면 사용자 지시로 View 작업이 이 Core 변경을 구현할 수 있다. 그 경우 바꾼 줄은 Core 전달 문서 §43에 기록해 넘긴다.

**호진님 진영 칸 설계 초안(DZ1~DZ6, 보류)과 다른 점**

- 겹침: 초안 DZ4는 금지, 이 요청은 허용(사용자 결정).
- 칸 입력 자리: 초안 DZ1은 스테이지 전투 데이터의 칸 목록(비면 필드 깊이)을 둔다. 이 요청은 자리를 정하지 않고 Core 계약만 다룬다.
- 사실 하나: 초안 §1.3은 `CombatPlacementCalculator.cs:164`를 "소환 칸 후보"로 적었다. 실제로는 전투 시작 때의 적 재배치(원위치·가까운 빈칸)다. 소환수 자리는 구역을 보지 않는다.
