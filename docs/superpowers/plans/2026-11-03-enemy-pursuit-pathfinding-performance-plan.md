# 적 추적 경로 찾기 성능 Task 계획

- 작성: 2026-11-03, Core(천명)
- 설계: [enemy-pursuit-pathfinding-performance-design](../specs/2026-11-03-enemy-pursuit-pathfinding-performance-design.md) (2판, Q-PF1~Q-PF5 모두 A)
- 원칙: 적 AI의 결과(참·거짓, 경로)는 지금과 같다(예외가 나지 않는 입력 기준). 시험 기대값은 바꾸지 않는다.
- 진행: Phase마다 멈추고 보고한다. 확인은 Unity Test Runner(`ProjectL.Tests.Core`)로 하고, 결과는 `LocalLow/DefaultCompany/Project_L_v2/TestResults.xml`에서 읽는다.

## 0. 계획에서 정한 것

| 번호 | 내용 |
|---|---|
| TP-1 | 기준 구현은 지금 `TryFind`·`CanAttackFrom`을 그대로 옮긴 사본이다. 새 시험 파일 안 `Reference` 클래스에 둔다(설계 Q-PF4 A: 검증 뒤에도 남김). 측정 구간 줄은 뺀다(결과와 무관) |
| TP-2 | 시험 장면은 9×9 보드, 적 1(행위자)·플레이어 대상 1·메인 1. 지정 방식·스플래시·사거리·지형·추가 유닛을 바꿔 가며 만들고, **행위자 출발 칸을 보드의 모든 빈 칸으로 바꿔** 비교한다 |
| TP-3 | PF-a는 `TryFind` 전체만 비교한다(지금 `CanAttackFrom`은 private). PF-c에서 칸 판정을 `internal`로 바꾸고 칸마다 비교를 더한다 |
| TP-4 | 사본이 예외를 던지는 장면은 만들지 않는다(설계 원칙: 예외 없는 입력에서의 동등성) |
| TP-5 | 측정 마커(`CoreProfiling`·`CoreProfilerMarkers`)는 그대로 둔다(Q-PF5 A). 새 함수에도 필요하면 구간을 더한다 |

## 1. Phase

### PF-a 기준 사본·동등성 시험 (코드 변경 없음)

- 새 `Assets/Tests/EditMode/EnemyPursuitEquivalenceTests.cs`
  - `Reference.TryFind`·`Reference.CanAttackFrom`: 지금 코드 사본
  - 장면 조합(설계 §6-2 중 `TryFind`로 드러나는 것): 모양 5 × 선택 3, 스플래시 4, 관통(대상 수 예산 1·무제한), 끌어오기 추가 사거리, 모래폭풍·고지대, 지뢰, 막는 유닛, 사거리 1~3
  - 단언: 출발 칸마다 `EnemyMovementPathfinder.TryFind` == `Reference.TryFind` (참·거짓, 경로 칸 전부)
- 이 Phase에서는 둘이 같은 코드라 당연히 통과한다. **목적은 사본과 장면 생성기가 맞게 도는지 먼저 확인하는 것**이다
- 확인(Ua): Test Runner `ProjectL.Tests.Core` → `EnemyPursuitEquivalenceTests` 전부 통과

### PF-b 공격 규칙 정리 (동작 같음)

- `CombatAttackQueries`
  - 사거리 함수 하나: `Selection`(`:51`)과 `RangeCells`(`:206`)의 식을 모음. `basicOnly` 길은 기본 사거리 그대로
  - 공격자 조건 함수 하나: `Range` 맨 앞(`:140-143`)의 "실패면 자금 부족 외엔 거짓" 규칙
  - `RangeCells`·`Selection`에 "칸 조건은 서로 같아야 한다" 상호 참조 주석
- 확인(Ub): `EnemyPursuitEquivalenceTests`, `EnemyMovementPathTests`, `PiercingShotAiTests`, 공격 범위·지정 시험(`AttackRange*`·`Targeting*`·`Pull*`·`Haboob*`)

### PF-c 후보 거르기·순서·지연 계산 (설계 D1~D3)

- `CombatAttackQueries`에 D1 후보 함수(관통 갈래 먼저, 후보 순서 = `Range` 순서, 겹침 제거)
- `EnemyMovementPathfinder`
  - 공격자 조건을 `TryFind` 시작 때 한 번(실패면 칸 판정 없이 "못 침")
  - 칸 판정: 옮긴 행위자 → 후보 → 없으면 거짓 → 투영 입력 → 후보마다 `DeliversTo`
  - 탐색 문맥: 환경은 `:26` 값 재사용, 프로필·벽·고저는 지연 계산, 대상 칸은 시작 때
  - 칸 판정을 `internal`로(TP-3)
- `ResolveDirect`·`PierceTargetCollector`·후보 함수에 서로를 가리키는 주석(새 전달 방식 추가 때)
- 시험: 칸마다 비교 추가(보드 모든 칸 × 장면), 공격자 조건 실패 장면(무장 해제·행동 사용·여러 칸 캐릭터), 진영 각도, 여러 칸 본부 대상, 관통 세부, 스플래시 가장자리
- 확인(Uc): Ub 목록 + 적 AI 시험 전체(아래 §2)

### PF-d 가까운 거리에서 탐색 멈추기 (설계 D4)

- 첫 목적지 거리 d를 기억하고, 꺼낸 표지의 거리가 d보다 크면 멈춤
- 시험: 같은 거리 목적지 여럿, 최단 목적지 예산 밖, 예산 안·밖 섞임, 시작 칸이 목적지, 목적지 없음, 땅 다지기 보너스
- 확인(Ud): Uc 목록

### PF-e 재측정·문서

- 사용자: 같은 장면 Profiler — `Core.Calc.EnemyPathfind` 시간, `EnemyCanAttackFrom` Calls, 그 아래 `AttackRange` Calls(0이어야 함), GC Alloc
- 설계 §7 다음 후보를 재측정 결과로 정함
- 설계·이 계획에 결과 기록, `ProjectL.Tests.Core` 전체 실행

## 2. 적 AI 회귀 시험 묶음

PF-c부터 Phase마다 `ProjectL.Tests.Core` 전체를 돌린다(적 AI·추적·관통·도발 시험 51개 파일이 여기 있다). 실패가 이 작업 전에도 있던 것인지는 PF-a 실행 결과(기준)와 비교한다.

## 3. 실행 기록

### 3.1 PF-a 완료 (2026-11-03, Ua 통과)

- 새 `Assets/Tests/EditMode/EnemyPursuitReference.cs`: 지금 `TryFind`·`CanAttackFrom` 사본(측정 구간 줄만 뺌), `internal static`
- 새 `Assets/Tests/EditMode/EnemyPursuitEquivalenceTests.cs`: 장면 44개 × 출발 칸((x + y) % 3 == 0인 빈 칸, 장면마다 약 15개)
  - 장면: 모양 5 × (칸 선택 스플래시 4 + 방향 + 전체), 정사각·가로세로 사거리 1·3, 정면 각도(칸), 정면·양옆 각도(부채꼴 방향), 기병 이동력, 관통 무제한·예산 1 + 앞 유닛, 끌어오기 추가 사거리, 막는 유닛, 모래폭풍, 고지대, 지뢰 미로
  - 보드 7×7, 대상 (5,4), 메인 (0,6). 9×9로 하려던 TP-2는 기준 구현이 느려 7×7로 줄임. 출발 칸도 모든 칸 대신 고르게 흩은 칸만
- 코드 변경 없음. 지금은 사본과 실제가 같은 코드라 통과해야 정상
- Ua: Test Runner `ProjectL.Tests.Core` → `EnemyPursuitEquivalenceTests` 44개 통과. 실패하면 장면 생성기·사본 문제이므로 PF-b 전에 고친다
- **Ua 결과(Claude가 Unity 배치 모드로 실행, `-runTests -assemblyNames ProjectL.Tests.Core;ProjectL.Tests.CoreIntegration`):**
  - 1차: 동등성 42/44. 실패 2개(막는 유닛 장면)는 막는 유닛 개체 번호 10이 메인 캐릭터(10)와 겹친 시험 데이터 문제 → 20번대로 고침
  - 2차: 동등성 **44/44 통과**(약 37초). 전체 5,605개 중 실패 33개는 모두 이 작업 전부터 실패하던 것 — 기준선으로 쓴다
    - Core 1: `TraitActivationOutcomeTests.Taunt_OnlyAnAppliedForcedDecisionActivates(True)`
    - CoreIntegration 32: `DeckSlotInfoPanelTests` 13, `DeckMainChangePolicyTests` 8, `DataBaseAssetTests` 7, `RunGoldFacilityTests` 2, `CombatCameraZoomPoseTests` 1, `CombatStructureInspectionTests` 1
  - 배치 실행이 `SCDream7 SDF.asset`(동적 글꼴 아틀라스)을 비워 바꿔서 실행 전 상태로 되돌림. LocalLow `TestResults.xml`도 이 실행 결과로 덮어써짐
  - 이후 Phase도 같은 명령으로 돌리고, 이 33개 밖의 실패가 생기면 이 작업 탓으로 본다

### 3.2 PF-b 완료 (2026-11-03, Ub 통과)

- `CombatAttackQueries`(동작 같음)
  - `SelectionRange(input, actor, profile, environment)`: 지형 보정 기본 사거리 + 끌어오기 추가 사거리. `Selection`과 `RangeCells`(기본만이 아닌 길)가 부름. `RangeCells`의 기본만 길은 지형 보정 기본 사거리 그대로
  - `RangeActor(input, out actor)`: `Actor` 결과에서 자금 부족만 통과. `Range` 맨 앞이 부름(PF-c에서 경로 찾기도 부름)
  - `Selection` 요약 주석·`RangeCells` 주석: 주 칸 검사와 범위 칸 거르기는 하나하나 같아야 함(설계 §4-2)
  - `PullAttackCalculator.cs:45`의 기본 사거리(끌어오기 도착 칸 계산)는 다른 뜻이라 그대로 둠
- Ub(배치 모드, Core·CoreIntegration): 5,605개 중 실패 33개 — PF-a 기준선과 같은 목록(새 실패 0, 통과로 바뀐 것 0). 동등성 44/44. 로그로 `ProjectL.Core.Application.dll` 다시 컴파일 확인
- 배치 실행이 바꾼 `SCDream7 SDF.asset`은 다시 되돌림

### 3.3 PF-c 완료 (2026-11-03, Uc 통과)

- `CombatAttackQueries`
  - `AimsThatCanHit(input, actor, profile, targetCells, environment)`: 설계 D1 표대로 후보를 만들고 `AttackSelectionResolver.TryResolve`(모양·각도·사거리)로 거름. 관통 갈래 먼저, 칸 조준 Y·X 순서, 겹침 제거
  - 방향 조준 목록 `DirectionAims`(열거 값 순서, None 제외) 하나를 `Range`와 함께 씀
- `EnemyMovementPathfinder`
  - 칸 판정을 탐색 한 번 동안 쓰는 `AttackReach`(private)로 옮김: 옮긴 행위자 → (처음 한 번: 투영 → 프로필 → `RangeActor`) → 후보 → 없으면 거짓 → 투영 → 공통 피해 → 후보마다 `DeliversTo`
  - 환경은 `TryFind`가 만든 값을 그대로, 벽·고저는 처음 필요할 때 한 번
  - `Range` 호출 없음
  - 시험 입구 `CanAttackFrom(input, actor, cell, targetId)`(internal, TP-3)
- 주석: `CombatAttackDelivery.ResolveDirect`·`PierceTargetCollector.CollectSingle`에 후보 규칙이 쓰는 성질 한 줄씩
- 시험 `EnemyPursuitEquivalenceTests`
  - 칸마다 비교 `CanAttackFrom_MatchesReference_OnEveryFreeCell` 추가(장면마다 빈 칸 전부)
  - 장면 44 → 51: 보드 가장자리 대상(주변 스플래시·관통), 관통 사거리 1 먼 대상, 2×2 본부 대상(정사각·십자 스플래시), 공격 사용함(공격자 조건 실패), 이동 사용함
  - 시험 이름은 `{m}(장면)`
  - 넣지 않은 것: 무장 해제 상태(상태 효과 만들기가 커서 공격 사용함으로 같은 공격자 조건 길을 덮음), 필드의 여러 칸 캐릭터(같은 길)
- Uc(배치 모드): 5,663개 중 실패 33개 — PF-b와 같은 목록(새 실패 0). 동등성 **102/102**(51 장면 × 2 시험), 약 26초(PF-a 44개 약 37초보다 짧음 — 기준 사본 쪽 시간이 대부분)
- 배치 실행이 바꾼 `SCDream7 SDF.asset`은 다시 되돌림

### 3.4 PF-d 완료 (2026-11-03, Ud 통과)

- `EnemyMovementPathfinder.TryFind`: 첫 목적지 거리 `goalDistance`를 기억하고, 꺼낸 표지 거리가 그보다 크면 멈춤(같은 거리 층은 끝까지 봄)
- 시험 장면 51 → 54: 대상 둘레 8칸을 유닛으로 막은 경우 셋(정사각 사거리 1 = 목적지 없음, 정사각 사거리 2·가로세로 사거리 3 = 둘레 밖에서만 침)
  - 같은 거리 목적지 여럿·최단 목적지가 예산 밖(앞부분 경로)·시작 칸이 목적지는 기존 장면의 흩은 출발 칸이 덮음(이동력 3, 7×7)
  - 넣지 않은 것: 땅 다지기 보너스(흔적 환경 만들기가 큼. 예산 판정은 출발 칸만 보므로 층 멈춤 근거와 무관)
- Ud(배치 모드): 5,669개 중 실패 33개 — PF-c와 같은 목록(새 실패 0). 동등성 **108/108**
- 배치 실행이 바꾼 `SCDream7 SDF.asset`은 다시 되돌림
- 남은 것: PF-e 재측정(사용자 Profiler)

### 3.5 PF-e 재측정 (2026-11-03, 사용자)

- 사용자 보고: "일단 프레임 보호는 돼. 이제 적 턴일 때 300이 보통이고 200 이하로 잘 안 내려간다"
  - 작업 전: 적 하나의 추적 경로 찾기가 한 프레임 약 240ms(약 4FPS 순간 정지, 평균 표시 24FPS)
  - 마커별 수치(`EnemyPathfind` 시간·`EnemyCanAttackFrom` Calls·GC Alloc)는 받지 않음
- 설계 §7 다음 후보(적 행동마다 모든 적 재계산, 공격 범위 자체 비용, 도발 출처마다 경로 찾기)는 지금 적 턴이 200FPS 아래로 잘 안 내려가므로 보류. 다시 끊기면 같은 마커로 재서 정한다
