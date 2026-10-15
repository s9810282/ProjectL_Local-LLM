# Stage Outcome and Night Cycle — Tasks 3–8 학습형 구현 가이드

> 과거 기록 (2026-10-06 정리): 아래 내용은 Framework/Buffer 전환 전 구현이며 현재 구현 지침이 아닙니다. 삭제된 계획·설계 원문은 Git 커밋 `606fdf4c`에서 확인할 수 있습니다. 현재 구조와 API는 [Framework 개발 가이드](../../../Assets/ProjectL/Script/Systems/Documentation/framework-programmer-guide.md)와 실제 코드를 기준으로 확인합니다.

## 이 문서의 목적

이 문서는 **2026-09-03-stage-outcome-night-cycle.md**의 Task 3부터 Task 8까지를 직접 구현하기 위한 학습형 가이드다. 원본 Plan의 Task 순서, 아키텍처, 타입 이름, 공개 함수 이름, 책임 경계와 원자성 규칙을 그대로 따른다.

정답 구현 전체를 제공하지 않는다. 대신 다음 내용을 제공한다.

- 어느 파일의 어느 위치를 수정하는지
- 새 타입의 선언 형태와 핵심 필드
- 각 함수 내부에서 지켜야 할 처리 순서
- Plan 단계에 미리 담아야 하는 값
- Commit 전에 끝내야 하는 검증
- 다음 Task에서 해당 코드가 연결되는 지점
- 구현 후 확인해야 할 핵심 동작

이 문서에는 RED 테스트 작성·실행 단계가 없다. 각 Task 마지막의 완료 체크리스트는 구현 후 무엇이 검증되어야 하는지만 설명한다.

## 시작 전 전제

Task 1과 Task 2가 먼저 완료되어 있어야 한다.

- **StageConfig.NightLength**
- **StageConfig.InitialEnemyStructurePlacements**
- **StructureState**, **StructureCatalog**, **StructureFactory**
- **PlayerHeadquartersPlacementService**
- **StageSessionFactory.Create(config, unitCatalog, structureCatalog)**
- **StageSession.TryPlaceInitialPlayerHeadquarters(...)**

다음 규칙은 Task 3부터 Task 8까지 계속 유지한다.

1. **CombatTimelineState.CurrentTime**과 밤 시간은 서로 다른 값이다.
2. 상태 변경은 가능한 한 **현재 상태 확인 → 대상 수집 → Plan 생성 → 전체 검증 → Commit** 순서로 처리한다.
3. Preview와 실패 Result는 상태를 변경하지 않는다.
4. Stage가 끝난 뒤에는 Query는 허용하지만 Command는 무변경 실패한다.
5. Structure는 낮 전환 때 회수하지 않는다.
6. 생산 예약 Unit은 Stage Entity나 Player Reserve에 없으므로 생존 병력으로 세지 않는다.

---

# Task 3 — Stage Outcome 상태, 판정기와 Snapshot

## 이번 Task의 책임

Task 3에서는 승패의 원본 상태를 Domain에 만들고, Stage 수준 판정기를 Application에 둔다.

책임을 다음처럼 나눈다.

~~~text
CombatEntityState / StructureState
→ 체력과 사망 여부만 소유

StageOutcomeEvaluator
→ Stage 전체 Entity와 Player Reserve를 읽고 승패 후보 판단

StageOutcomeState
→ 한 번만 최종 결과 Commit

StageSnapshotBuilder
→ Domain 상태를 공개 StageOutcomeSnapshot으로 복사
~~~

**UnitAttackService** 안에서 승패를 결정하지 않는다. 공격 서비스는 피해와 사망 처리까지만 담당하고, Task 6에서 **StageCombatFacade**가 공격 Commit 직후 Evaluator를 호출한다.

## Step 1 — StageOutcome.cs

### Step 1-1 — 공개 enum 선언

파일:

**Assets/Scripts/ProjectL/Core/Contracts/Common/StageOutcome.cs**

타입 선언:

**public enum**

추가 위치:

새 파일의 **ProjectL.Core.Contracts.Common** namespace 안.

역할:

Stage가 진행 중인지, 승리인지, 패배인지 View와 Application이 공통으로 읽는 최상위 결과다.

힌트 코드:

~~~csharp
public enum StageOutcome
{
    InProgress = 0,
    Victory = 1,
    Defeat = 2,
}
~~~

주의할 점:

- **InProgress**를 0으로 둔다. 기본값이 곧 정상적인 초기 상태가 된다.
- 원인은 이 enum에 넣지 않는다. Outcome과 Reason을 분리해야 View가 결과 종류와 세부 원인을 독립적으로 처리할 수 있다.

## Step 2 — StageOutcomeReason.cs

### Step 2-1 — 공개 enum 선언

파일:

**Assets/Scripts/ProjectL/Core/Contracts/Common/StageOutcomeReason.cs**

타입 선언:

**public enum**

추가 위치:

새 파일의 **ProjectL.Core.Contracts.Common** namespace 안.

힌트 코드:

~~~csharp
public enum StageOutcomeReason
{
    None = 0,
    EnemyHeadquartersDestroyed = 1,
    PlayerHeadquartersDestroyed = 2,
    NoPlayerUnitsRemaining = 3,
}
~~~

유효 조합은 다음 세 종류뿐이다.

~~~text
InProgress + None
Victory    + EnemyHeadquartersDestroyed
Defeat     + PlayerHeadquartersDestroyed 또는 NoPlayerUnitsRemaining
~~~

다른 조합을 허용하면 Victory인데 Player HQ 파괴가 원인인 모순된 Snapshot을 만들 수 있다.

## Step 3 — StageOutcomeSnapshot.cs

### Step 3-1 — View용 불변 Snapshot 선언

파일:

**Assets/Scripts/ProjectL/Core/Contracts/Snapshots/StageOutcomeSnapshot.cs**

타입 선언:

**public readonly struct**

추가 위치:

새 파일의 **ProjectL.Core.Contracts.Snapshots** namespace 안.

역할:

View가 Domain의 **StageOutcomeState**를 직접 참조하지 않고 결과를 읽는 공개 복사본이다.

힌트 코드:

~~~csharp
public readonly struct StageOutcomeSnapshot
{
    public StageOutcome Outcome { get; }
    public StageOutcomeReason Reason { get; }
    public bool IsStageEnded => Outcome != StageOutcome.InProgress;

    public StageOutcomeSnapshot(
        StageOutcome outcome,
        StageOutcomeReason reason)
    {
        // 1. enum 값 유효성 확인
        // 2. Outcome과 Reason 조합 확인
        // 3. 프로퍼티 대입
    }
}
~~~

구현 흐름:

~~~text
Outcome enum 유효성 확인
→ Reason enum 유효성 확인
→ 허용된 Outcome/Reason 조합인지 확인
→ 값 저장
~~~

주의할 점:

- **IsStageEnded**는 저장 필드가 아니라 Outcome에서 계산하는 파생 값이다.
- Snapshot 자체도 잘못된 조합을 거부하면 공개 경계에서 모순된 결과가 만들어지는 것을 막을 수 있다.

## Step 4 — StageOutcomeState.cs

### Step 4-1 — Stage 결과 원본 상태 선언

파일:

**Assets/Scripts/ProjectL/Core/Domain/Phase/StageOutcomeState.cs**

타입 선언:

**internal sealed class**

추가 위치:

새 파일의 **ProjectL.Core.Domain.Phase** namespace 안.

역할:

한 Stage의 권위 있는 결과를 소유한다. Evaluator는 판단만 하고 실제 최종 상태 반영은 이 객체가 담당한다.

힌트 코드:

~~~csharp
internal sealed class StageOutcomeState
{
    internal StageOutcome Outcome { get; private set; }
    internal StageOutcomeReason Reason { get; private set; }
    internal bool IsStageEnded => Outcome != StageOutcome.InProgress;

    internal StageOutcomeState()
    {
        Outcome = StageOutcome.InProgress;
        Reason = StageOutcomeReason.None;
    }

    internal void Conclude(
        StageOutcome outcome,
        StageOutcomeReason reason)
    {
        // 아래 Step 4-2의 순서를 구현한다.
    }
}
~~~

### Step 4-2 — Conclude를 단방향 전이로 구현

추가 위치:

**StageOutcomeState.cs의 생성자 아래**.

구현 흐름:

~~~text
이미 IsStageEnded인지 확인
→ InProgress를 최종 결과로 넣으려는지 거부
→ Outcome/Reason enum 유효성 확인
→ Victory/Defeat와 Reason 조합 검증
→ Outcome과 Reason을 한 번에 반영
~~~

핵심 조건 힌트:

~~~csharp
if (IsStageEnded)
    throw new InvalidOperationException(...);

bool validVictory =
    outcome == StageOutcome.Victory &&
    reason == StageOutcomeReason.EnemyHeadquartersDestroyed;

bool validDefeat =
    outcome == StageOutcome.Defeat &&
    (reason == StageOutcomeReason.PlayerHeadquartersDestroyed ||
     reason == StageOutcomeReason.NoPlayerUnitsRemaining);

if (!validVictory && !validDefeat)
    throw new ArgumentException(...);
~~~

주의할 점:

- 이미 결론 난 상태에 같은 결과를 다시 넣는 것도 허용하지 않는다. 최종 결과는 정확히 한 번 Commit된다.
- 검증이 모두 끝난 뒤 두 프로퍼티를 반영한다. 한 프로퍼티만 먼저 바꾸면 예외 시 부분 상태가 남을 수 있다.

## Step 5 — StageOutcomeEvaluator.cs

### Step 5-1 — 판정기 의존성 구성

파일:

**Assets/Scripts/ProjectL/Core/Application/StageRun/StageOutcomeEvaluator.cs**

타입 선언:

**internal sealed class**

필드 영역에 필요한 의존성:

~~~csharp
private readonly StageOutcomeState _state;
private readonly IReadOnlyDictionary<EntityId, CombatEntityState> _entities;
private readonly ReserveSlotState _playerReserveSlots;
~~~

역할:

Entity 체력과 배치 상태를 읽어 어떤 Outcome을 확정할지 판단한다. 결과의 실제 Commit은 **StageOutcomeState.Conclude**에 맡긴다.

생성자에서는 세 의존성의 null을 거부한다.

### Step 5-2 — EvaluateAndCommit 구현

추가 위치:

**StageOutcomeEvaluator.cs의 생성자 아래**.

함수 시그니처:

~~~csharp
internal void EvaluateAndCommit()
~~~

판정 순서는 반드시 고정한다.

~~~text
이미 Stage가 끝났는지 확인
→ Player HQ 사망 여부
→ Enemy HQ 사망 여부
→ 실제 Player Unit 생존 여부
→ 해당 결과가 있을 때 StageOutcomeState.Conclude
~~~

핵심 순회 힌트:

~~~csharp
foreach (CombatEntityState entity in _entities.Values)
{
    // Headquarters는 Owner별로 찾고 IsDead를 확인한다.
    // Player Unit은 UnitState, Owner.Player, !IsDead 조건으로 찾는다.
    // Reserve Unit이면 실제 Player Reserve 점유와 일치하는지도 확인한다.
}
~~~

병력 전멸 판정에서 살아 있는 Player Unit으로 인정하는 대상:

- Combat에 살아서 존재하는 Player **UnitState**
- Player **ReserveSlotState**의 자기 슬롯을 실제로 점유한 살아 있는 Player **UnitState**

제외 대상:

- Structure와 Headquarters
- 죽은 Unit
- Enemy Unit과 Enemy Reserve
- 생산 예약처럼 Stage Entity에 생성되지 않은 데이터

주의할 점:

- HQ가 정확히 하나인지 검사하는 책임은 Evaluator에 없다. 그 불변식은 Task 6의 **TryEnterCombat**에서 검증한다.
- Enemy 일반 Structure가 죽어도 Victory가 아니다. Key/Kind가 Headquarters인지 확인해야 한다.
- 이미 종료된 상태에서 다시 평가하면 아무것도 변경하지 않고 반환한다.
- 동시 HQ 파괴 우선순위를 새로 설계하지 않는다. 원본 Plan대로 Player HQ → Enemy HQ → 병력 전멸 순서를 유지한다.

## Step 6 — StageSnapshot.cs

### Step 6-1 — Outcome 프로퍼티와 생성자 인자 추가

파일:

**Assets/Scripts/ProjectL/Core/Contracts/Snapshots/StageSnapshot.cs**

추가 위치:

- **Outcome** 프로퍼티는 기존 **CombatTurn** 프로퍼티 근처.
- 새 생성자 인자는 기존 Snapshot 데이터 인자 뒤.
- 구버전 호환 생성자는 클래스 아래쪽에 유지.

힌트 코드:

~~~csharp
public StageOutcomeSnapshot Outcome { get; }

public StageSnapshot(
    int mapWidth,
    int mapHeight,
    StagePhase currentPhase,
    EntitySnapshot[] entities,
    ReserveSlotSnapshot[] reserveSlots,
    CombatTurnSnapshot combatTurnSnapshot,
    StageOutcomeSnapshot outcome)
{
    // 기존 검증과 방어적 복사 유지
    Outcome = outcome;
}
~~~

기존 생성자 호환 흐름:

~~~text
기존 인자만 받는 overload
→ 새 주 생성자 호출
→ StageOutcomeSnapshot(InProgress, None)을 명시적으로 전달
~~~

임의의 숨은 Runtime 기본 상태를 만들지 말고, 호환 overload 안에서만 명시적으로 초기 Snapshot을 구성한다.

## Step 7 — StageSnapshotBuilder.cs

### Step 7-1 — Outcome 상태 주입

파일:

**Assets/Scripts/ProjectL/Core/Application/StageRun/StageSnapshotBuilder.cs**

추가 위치:

- 클래스 필드 영역에 **StageOutcomeState** 필드 추가
- 기존 생성자의 마지막 의존성 근처에 인자 추가
- 생성자에서 null 검증 후 저장

힌트 코드:

~~~csharp
private readonly StageOutcomeState _outcomeState;
~~~

### Step 7-2 — Build에서 Outcome Snapshot 생성

추가 위치:

**Build() 안에서 CombatTurnSnapshot을 만든 뒤, new StageSnapshot(...) 호출 직전**.

힌트 코드:

~~~csharp
StageOutcomeSnapshot outcomeSnapshot =
    new StageOutcomeSnapshot(
        _outcomeState.Outcome,
        _outcomeState.Reason);
~~~

그다음 **StageSnapshot**의 새 생성자 인자로 전달한다. SnapshotBuilder는 판정하지 않고 현재 상태를 복사만 해야 한다.

## Step 8 — StageSessionFactory.cs

### Step 8-1 — Outcome 상태와 판정기 조립

파일:

**Assets/Scripts/ProjectL/Core/Application/StageRun/StageSessionFactory.cs**

추가 위치:

- **StagePhaseState**를 생성하는 Stage 기본 상태 영역 근처에 **StageOutcomeState** 생성
- Entity dictionary와 Player Reserve가 준비된 뒤 **StageOutcomeEvaluator** 생성
- **StageSnapshotBuilder** 생성자 호출에 Outcome 상태 전달

힌트 흐름:

~~~csharp
StageOutcomeState outcomeState = new StageOutcomeState();

// map, reserveSlot, entities가 준비된 뒤
StageOutcomeEvaluator outcomeEvaluator =
    new StageOutcomeEvaluator(
        outcomeState,
        entities,
        reserveSlot);

// SnapshotBuilder에는 outcomeState를 전달
~~~

Task 3에서는 상태와 판정기를 조립하지만, 공격·기믹·Combat 진입 뒤 실제 호출 연결은 Task 6에서 한다.

## 전체 흐름

~~~text
StructureState / UnitState / Player Reserve
→ StageOutcomeEvaluator.EvaluateAndCommit()
→ StageOutcomeState.Conclude(...)
→ StageSnapshotBuilder.Build()
→ StageOutcomeSnapshot
→ StageSnapshot.Outcome
→ View
~~~

## 완료 체크리스트

- [ ] 초기 상태가 **InProgress / None**이다.
- [ ] Outcome과 Reason의 잘못된 조합을 만들 수 없다.
- [ ] 한 번 Victory/Defeat가 확정되면 다시 바뀌지 않는다.
- [ ] Player HQ 사망이 **Defeat / PlayerHeadquartersDestroyed**가 된다.
- [ ] Enemy HQ 사망이 **Victory / EnemyHeadquartersDestroyed**가 된다.
- [ ] Enemy 일반 Structure 사망은 결과를 끝내지 않는다.
- [ ] Combat과 Player Reserve에 살아 있는 Player Unit이 모두 없을 때만 병력 전멸이다.
- [ ] 죽은 Unit, Enemy Reserve, 생산 예약은 Player 생존 병력으로 세지 않는다.
- [ ] StageSnapshot이 Domain State가 아닌 불변 Outcome Snapshot을 제공한다.

---

# Task 4 — 독립적인 밤 시간 상태와 Snapshot

## 이번 Task의 책임

밤 시간은 Timeline Scheduler 시간과 분리한다.

~~~text
CombatTimelineState.CurrentTime
= Unit 행동 순서를 계산하는 Scheduler 시간

StageNightState.CurrentNightTime
= 이번 밤에 실제로 종료된 Unit들의 raw Delay 누적값
~~~

**StageNightState**가 원본 상태를 소유하고, **StageCombatFacade.EndUnitTurn()**이 Task 6에서 Plan과 Commit을 조정한다. Task 4에서는 순수 상태와 Snapshot까지만 준비한다.

## Step 1 — StageNightSnapshot.cs

### Step 1-1 — View용 밤 상태 선언

파일:

**Assets/Scripts/ProjectL/Core/Contracts/Snapshots/StageNightSnapshot.cs**

타입 선언:

**public readonly struct**

추가 위치:

새 파일의 **ProjectL.Core.Contracts.Snapshots** namespace 안.

힌트 코드:

~~~csharp
public readonly struct StageNightSnapshot
{
    public long NightLength { get; }
    public long CurrentNightTime { get; }
    public long RemainingNightTime { get; }

    public StageNightSnapshot(
        long nightLength,
        long currentNightTime)
    {
        // 양수 NightLength와 음수가 아닌 CurrentNightTime 검증
        // RemainingNightTime은 저장 입력으로 받지 않고 계산
    }
}
~~~

Remaining 값은 다음 규칙으로 만든다.

~~~csharp
RemainingNightTime =
    Math.Max(0L, nightLength - currentNightTime);
~~~

주의할 점:

- **CurrentNightTime**이 NightLength를 초과할 수 있다. 마지막 Unit의 Delay를 잘라서 더하지 않기 때문이다.
- Remaining만 0으로 clamp하고 Current 값은 실제 누적값을 보존한다.
- 비율, 색상, 게이지 표시 정책은 Core 계약에 넣지 않는다.

## Step 2 — StageNightState.cs

### Step 2-1 — NightAdvancePlan 선언

파일:

**Assets/Scripts/ProjectL/Core/Domain/Phase/StageNightState.cs**

타입 선언:

**internal readonly struct**

추가 위치:

같은 파일의 **StageNightState** 선언 위 또는 아래. 공개 Contracts로 이동시키지 않는다.

역할:

Delay를 더한 결과를 실제 상태에 반영하기 전에 완성된 값으로 보관한다. Task 6에서 Timeline/Trait/회수 Plan이 모두 유효한 경우에만 이 Plan을 Commit한다.

힌트 코드:

~~~csharp
internal readonly struct NightAdvancePlan
{
    internal long ExpectedNightTime { get; }
    internal long FinalNightTime { get; }
    internal bool EndsNight { get; }

    internal NightAdvancePlan(
        long expectedNightTime,
        long finalNightTime,
        bool endsNight)
    {
        // 생성 시 값 저장
    }
}
~~~

Plan에 필요한 핵심 정보:

- 계획을 만들 당시의 CurrentNightTime
- raw Delay를 더한 최종 시간
- 최종 시간이 NightLength 이상인지

### Step 2-2 — StageNightState 필드와 생성자

타입 선언:

**internal sealed class**

필드/프로퍼티 영역:

~~~csharp
internal long NightLength { get; }
internal long CurrentNightTime { get; private set; }
~~~

생성자 흐름:

~~~text
NightLength > 0 확인
→ NightLength 저장
→ CurrentNightTime = 0
~~~

이 상태는 Phase를 직접 알지 않는다. 어느 시점에 증가하거나 초기화하는지는 Task 6의 Application 조정자가 결정한다.

### Step 2-3 — PlanAdvance 구현

추가 위치:

**StageNightState 생성자 아래**.

함수 시그니처:

~~~csharp
internal NightAdvancePlan PlanAdvance(int delay)
~~~

구현 흐름:

~~~text
delay가 양수인지 확인
→ expected = CurrentNightTime
→ checked(expected + delay)로 final 계산
→ final >= NightLength로 EndsNight 계산
→ NightAdvancePlan 반환
~~~

힌트 코드:

~~~csharp
long finalNightTime =
    checked(CurrentNightTime + delay);

return new NightAdvancePlan(
    CurrentNightTime,
    finalNightTime,
    finalNightTime >= NightLength);
~~~

주의할 점:

- 이 함수는 Query 성격의 Plan 생성 함수다. **CurrentNightTime을 변경하면 안 된다.**
- Delay는 **UnitState.Delay** 원본값이다. 이동 횟수, 공격 횟수, ActionCost를 사용하지 않는다.
- overflow가 발생해도 상태는 그대로여야 한다.

### Step 2-4 — CommitAdvance 구현

함수 시그니처:

~~~csharp
internal void CommitAdvance(NightAdvancePlan plan)
~~~

구현 흐름:

~~~text
plan.ExpectedNightTime과 현재 CurrentNightTime 비교
→ 다르면 stale Plan 예외
→ FinalNightTime이 음수가 아닌지 확인
→ CurrentNightTime에 FinalNightTime 대입
~~~

Commit에서 Delay를 다시 더하지 않는다. Plan의 최종값을 대입해야 정확히 한 번만 누적된다.

### Step 2-5 — ResetForCombat 구현

함수 시그니처:

~~~csharp
internal void ResetForCombat()
~~~

역할:

다음 밤 Combat 진입이 모든 검증을 통과한 뒤 CurrentNightTime을 0으로 초기화한다.

주의할 점:

- 낮으로 전환될 때는 초기화하지 않는다. 낮 Snapshot에는 NightLength에 도달하거나 초과한 시간이 그대로 보여야 한다.
- 이전 밤의 초과분을 다음 밤으로 이월하지 않는다.
- Task 6의 **TryEnterCombat** Commit 구간에서만 호출한다.

## Step 3 — StageSnapshot.cs

### Step 3-1 — Night Snapshot 통합

파일:

**Assets/Scripts/ProjectL/Core/Contracts/Snapshots/StageSnapshot.cs**

추가 위치:

- **Outcome** 프로퍼티 근처에 **Night** 프로퍼티 추가
- Task 3에서 확장한 주 생성자의 마지막 인자 영역에 Night Snapshot 추가

힌트 코드:

~~~csharp
public StageNightSnapshot Night { get; }

public StageSnapshot(
    // 기존 인자
    CombatTurnSnapshot combatTurnSnapshot,
    StageOutcomeSnapshot outcome,
    StageNightSnapshot night)
{
    // 기존 검증과 방어적 복사
    Outcome = outcome;
    Night = night;
}
~~~

기존 생성자 overload를 유지해야 한다면 호환용 Outcome과 Night 값을 명시해서 주 생성자로 연결한다. 실제 **StageSessionFactory** 경로는 반드시 **StageConfig.NightLength**로 만든 값을 사용해야 하며 호환 기본값에 기대면 안 된다.

## Step 4 — StageSnapshotBuilder.cs

### Step 4-1 — StageNightState 주입

파일:

**Assets/Scripts/ProjectL/Core/Application/StageRun/StageSnapshotBuilder.cs**

추가 위치:

- 클래스 필드 영역
- 기존 생성자 인자와 null 검증 영역

힌트 코드:

~~~csharp
private readonly StageNightState _nightState;
~~~

### Step 4-2 — Build에서 Night Snapshot 생성

추가 위치:

**Build()의 StageSnapshot 생성 직전**.

~~~csharp
StageNightSnapshot nightSnapshot =
    new StageNightSnapshot(
        _nightState.NightLength,
        _nightState.CurrentNightTime);
~~~

Task 3의 Outcome Snapshot과 함께 새 **StageSnapshot** 생성자에 전달한다.

SnapshotBuilder는 Remaining 값을 별도로 계산하거나 Stage 전환을 판단하지 않는다. 계산 규칙은 **StageNightSnapshot**, 전환 판단은 Task 6의 **StageCombatFacade** 책임이다.

## Step 5 — StageSessionFactory.cs

### Step 5-1 — StageNightState 생성과 공유

파일:

**Assets/Scripts/ProjectL/Core/Application/StageRun/StageSessionFactory.cs**

추가 위치:

**StagePhaseState**, **StageOutcomeState**를 만드는 Stage 기본 상태 영역.

힌트 코드:

~~~csharp
StageNightState nightState =
    new StageNightState(config.NightLength);
~~~

연결 흐름:

~~~text
StageConfig.NightLength
→ StageNightState 생성
→ StageSnapshotBuilder 생성자에 전달
→ Task 6에서 StageSession/StageCombatFacade에도 같은 인스턴스 전달
~~~

같은 Stage에 NightState를 둘 이상 만들면 Snapshot이 보는 시간과 EndUnitTurn이 수정하는 시간이 달라진다. Factory에서 한 번 만들고 공유한다.

## 전체 흐름

~~~text
StageConfig.NightLength
→ StageSessionFactory
→ StageNightState
   ├─ PlanAdvance(UnitState.Delay)
   ├─ CommitAdvance(plan)
   └─ ResetForCombat()
→ StageSnapshotBuilder
→ StageNightSnapshot
→ StageSnapshot.Night
→ View의 선택적 게이지/숫자 표시
~~~

## 완료 체크리스트

- [ ] NightLength가 0 이하이면 상태를 만들 수 없다.
- [ ] PlanAdvance가 상태를 변경하지 않는다.
- [ ] raw Delay가 checked 연산으로 정확히 한 번 더해진다.
- [ ] overflow와 stale Plan이 현재 시간을 변경하지 않는다.
- [ ] 경계와 같은 값 및 초과값 모두 EndsNight가 true다.
- [ ] RemainingNightTime은 음수가 되지 않는다.
- [ ] 낮 Snapshot이 도달·초과한 CurrentNightTime을 보존한다.
- [ ] ResetForCombat만 다음 밤 시작 시 시간을 0으로 만든다.
- [ ] CombatTimelineState.CurrentTime은 수정하지 않는다.

---

# Task 5 — 양 진영 Unit 회수와 Enemy 최초 위치 복귀

## 이번 Task의 책임

밤이 끝날 때 살아 있는 Player/Enemy Unit을 전장에서 회수하고, 다음 Combat 진입 때 살아 있는 Enemy Unit만 StageConfig에 기록된 최초 위치와 회전으로 되돌린다.

여기서 중요한 경계는 다음과 같다.

- **StagePhaseTransitionService**가 대상 수집, 목적지 계산, 전체 검증을 담당한다.
- **StagePhaseTransitionPlan**과 **EnemyRedeploymentPlan**은 Commit에 필요한 기대 상태와 최종 상태를 미리 보관한다.
- Plan 생성 중에는 어떤 Entity, Map, Reserve도 변경하지 않는다.
- Commit은 Plan이 만들어진 뒤 상태가 바뀌지 않았는지 재검증한 다음 한 번에 적용한다.
- Structure는 회수 대상이 아니다. Player HQ, Enemy HQ, Enemy 일반 건물은 Map 위치와 체력을 그대로 유지한다.
- 죽은 Unit은 되살리거나 다시 배치하지 않는다.
- Enemy의 내부 ReserveSlotIndex는 최초 입력 인덱스지만, Player용 **ReserveSlotState**를 점유하지 않는다.

이 Task에서는 자동 전환 호출까지 넣지 않는다. Task 6에서 **EndUnitTurn()**과 **TryEnterCombat()**이 이 Service의 Plan/Commit API를 호출한다.

## Step 1 — StagePhaseTransitionPlan.cs

### Step 1-1 — 낮 전환용 불변 Plan 선언

파일:

**Assets/Scripts/ProjectL/Core/Application/Deployment/StagePhaseTransitionPlan.cs**

타입 선언:

**internal sealed class**

추가 위치:

새 파일의 namespace 내부.

역할:

**PlanEnterDeployment()**가 검증을 마친 뒤 반환하는 Commit 전용 자료다. 살아 있고 현재 Combat Map에 배치된 Unit 각각에 대해 최소한 다음 정보를 보관해야 한다.

- 대상 Entity 또는 EntityId
- Plan 생성 당시 기대한 Combat 위치와 점유 Cell
- Owner
- 회수 후 사용할 ReserveSlotIndex
- Player Unit이라면 실제 Player Reserve에서 배정받은 슬롯
- Enemy Unit이라면 최초 Stage 입력 인덱스를 사용한 내부 Reserve 위치
- stale 여부를 확인하는 데 필요한 Phase, active turn, Timeline 기대 상태

Plan을 공개 Contract로 만들지 않는다. View가 회수 세부 절차를 알아야 할 이유가 없고, Commit 외부에서 Plan을 조작하면 원자성 전제가 깨지기 때문이다.

힌트 코드:

~~~csharp
internal sealed class StagePhaseTransitionPlan
{
    internal StagePhase ExpectedPhase { get; }

    // 내부에는 Unit별 기대 Combat 위치/점유 Cell과
    // 최종 Reserve 위치를 사전 계산한 컬렉션을 보관한다.
}
~~~

전체 구현을 그대로 옮기기보다, 현재 **EntityLocation**과 Map API가 Commit 시 무엇을 요구하는지 보고 entry에 필요한 기대값과 최종값을 정한다. 핵심은 Commit 중 목적지를 다시 계산하지 않는 것이다.

주의할 점:

- Plan에 **StructureState**를 넣지 않는다.
- 죽은 Unit과 이미 Reserve에 있는 Unit도 넣지 않는다.
- Player와 Enemy의 Reserve 의미를 섞지 않는다.
- mutable List를 그대로 외부에 노출하지 않는다.

## Step 2 — EnemyRedeploymentPlan.cs

### Step 2-1 — Enemy 원위치 복귀용 불변 Plan 선언

파일:

**Assets/Scripts/ProjectL/Core/Application/Deployment/EnemyRedeploymentPlan.cs**

타입 선언:

**internal sealed class**

추가 위치:

새 파일의 namespace 내부.

역할:

다음 Combat에 참가할 살아 있는 Enemy Reserve Unit과 각 Unit의 최초 Anchor/Rotation을 미리 확정한다. Task 6의 Timeline 초기화도 이 Plan을 읽어 “재배치가 끝난 뒤 배치 상태가 될 Enemy”를 참가자에 포함해야 한다.

각 항목에 필요한 정보:

- Enemy EntityId 또는 UnitState 참조
- 현재 기대 Reserve 위치와 최초 입력 인덱스
- 최종 Anchor와 Rotation
- 최종 Footprint Cell 집합
- Timeline 참가자 예상 집합을 만들 때 필요한 Unit 정보

힌트 코드:

~~~csharp
internal sealed class EnemyRedeploymentPlan
{
    // 내부에는 Enemy별 기대 Reserve 위치, 최초 Anchor/Rotation,
    // 최종 점유 Cell과 Timeline 투영 정보를 보관한다.
}
~~~

Plan은 “어디에 놓을지”만 담는 명령 목록이 아니다. 현재 상태가 여전히 Plan 생성 당시와 같은지 Commit 전에 확인할 수 있는 기대값도 함께 담아야 한다.

주의할 점:

- 최초 위치는 현재 위치에서 역산하지 않는다. Factory가 저장한 최초 입력을 사용한다.
- 죽은 Enemy는 Entries에 포함하지 않는다.
- Enemy Reserve는 Player Reserve UI 슬롯이 아니다.
- 최종 Cell 집합은 Plan 단계에서 계산해 충돌을 모두 확인한다.

## Step 3 — StagePhaseTransitionService.cs

### Step 3-1 — Service 의존성과 소유 경계 구성

파일:

**Assets/Scripts/ProjectL/Core/Application/Deployment/StagePhaseTransitionService.cs**

타입 선언:

**internal sealed class**

추가 위치:

새 파일의 클래스 필드 및 생성자 영역.

역할:

회수와 재배치의 정책 판단을 한곳에 모은다. 이 Service는 상태를 소유하지 않고, Factory가 만든 기존 상태 객체와 Entity lookup을 참조한다.

생성자에서 받을 대상:

- 전체 Entity lookup
- **ReserveSlotState**
- **StageMapState**
- **StagePhaseState**
- active turn 및 Timeline 일관성을 확인할 기존 상태/서비스
- EntityId별 Enemy 최초 Anchor/Rotation lookup
- EntityId별 Enemy 최초 입력 인덱스 lookup

힌트 코드:

~~~csharp
internal sealed class StagePhaseTransitionService
{
    private readonly IReadOnlyDictionary<EntityId, CombatEntityState> _entities;
    private readonly ReserveSlotState _playerReserve;
    private readonly StageMapState _map;
    private readonly StagePhaseState _phase;
    private readonly IReadOnlyDictionary<EntityId, InitialEnemyPlacementSpec> _enemyOrigins;
    private readonly IReadOnlyDictionary<EntityId, int> _enemyInputIndices;

    internal StagePhaseTransitionService(/* 기존 상태와 lookup */)
    {
        // 같은 StageSession이 사용하는 인스턴스를 그대로 보관한다.
    }
}
~~~

실제 타입 이름과 접근 방식은 현재 Core 코드에 맞춘다. 별도 public 저장소나 새 공개 DTO를 만들지 않는다.

### Step 3-2 — PlanEnterDeployment 구현

추가 위치:

생성자 아래 public이 아닌 Application API 영역.

함수 시그니처:

~~~csharp
internal StagePhaseTransitionPlan PlanEnterDeployment();
~~~

구현 흐름:

~~~text
현재 Combat Phase 확인
→ active turn과 Timeline의 종료 가능 상태 확인
→ 전체 Entity에서 살아 있고 배치된 UnitState만 수집
→ 각 Unit의 Map 위치와 실제 점유가 정확히 일치하는지 확인
→ Player/Enemy로 분리
→ Player Reserve의 빈 슬롯을 낮은 인덱스부터 수집
→ Player Unit 수만큼 슬롯이 있는지 확인
→ Enemy마다 최초 입력 인덱스 lookup이 있는지 확인
→ 모든 목적 Reserve 위치가 중복되지 않는지 확인
→ 기대 현재 상태와 최종 Reserve 위치를 담은 Plan 생성
~~~

핵심 필터의 힌트:

~~~csharp
foreach (CombatEntityState entity in _entities.Values)
{
    if (entity is not UnitState unit)
        continue;
    if (unit.IsDead || !unit.IsDeployed)
        continue;

    // 현재 Map 점유 검증 후 Owner별 목적 Reserve 위치를 계획한다.
}
~~~

Player 슬롯 배정은 Entity 열거 순서에 우연히 기대지 말고, 기존 프로젝트의 안정적인 Unit 순서와 “가장 낮은 빈 슬롯부터” 규칙을 함께 적용한다. Enemy는 Player 슬롯 여유와 무관하게 각자의 최초 입력 인덱스를 내부 ReserveSlotIndex로 사용한다.

Plan 생성이 실패하면 Entity 위치, Map 점유, Player Reserve, Phase, Timeline은 모두 그대로여야 한다.

### Step 3-3 — CommitEnterDeployment 구현

함수 시그니처:

~~~csharp
internal void CommitEnterDeployment(StagePhaseTransitionPlan plan);
~~~

구현 흐름:

~~~text
plan null 확인
→ 현재 Phase/active turn/Timeline이 Plan 기대값과 같은지 확인
→ 모든 대상 Unit 위치와 Map 점유가 기대값과 같은지 재검증
→ 모든 Player 목적 슬롯이 아직 비어 있는지 재검증
→ 재검증 전체 성공
→ 모든 대상의 Map 점유 해제
→ Player Unit을 실제 Player Reserve 슬롯에 기록
→ Enemy Unit 위치를 내부 Reserve 위치로 변경
~~~

검증과 변경을 Unit마다 섞으면 앞 Unit은 회수됐는데 뒤 Unit에서 실패할 수 있다. 반드시 전체 재검증 루프를 끝낸 다음 별도의 적용 루프를 시작한다.

### Step 3-4 — PlanRedeployEnemies 구현

함수 시그니처:

~~~csharp
internal EnemyRedeploymentPlan PlanRedeployEnemies();
~~~

구현 흐름:

~~~text
현재 Deployment Phase 확인
→ 전체 Entity에서 살아 있는 Enemy Unit 중 Reserve 상태만 수집
→ 각 Unit의 내부 ReserveSlotIndex가 최초 입력 인덱스와 맞는지 확인
→ EntityId별 최초 Anchor/Rotation 조회
→ Footprint의 최종 Cell 계산
→ Map 범위와 진영 배치 규칙 확인
→ 현재 Map의 모든 점유와 충돌하지 않는지 확인
→ 재배치 대상끼리 최종 Cell이 겹치지 않는지 확인
→ 기대 Reserve 위치와 완성된 최종 배치 정보를 Plan에 저장
~~~

모든 Enemy 목적지를 먼저 계산하고 하나의 임시 Cell 집합으로 대상끼리의 중복도 검사한다. 첫 Enemy를 실제로 배치하면서 둘째 Enemy의 충돌을 찾는 방식은 사용하지 않는다.

### Step 3-5 — CommitRedeployEnemies 구현

함수 시그니처:

~~~csharp
internal void CommitRedeployEnemies(EnemyRedeploymentPlan plan);
~~~

구현 흐름:

~~~text
plan null 확인
→ 모든 Enemy가 아직 Plan의 기대 Reserve 위치에 있는지 확인
→ 각 목적 Cell의 현재 Map 점유를 다시 확인
→ 재배치 대상끼리 여전히 충돌하지 않는지 확인
→ 재검증 전체 성공
→ Plan에 저장된 Cell을 Map에 점유
→ Unit 위치를 최초 Anchor/Rotation의 Combat 위치로 변경
~~~

죽은 Enemy는 처음부터 Plan에 없으므로 Commit에서도 손대지 않는다. 회수 뒤 죽은 Unit이 생기는 별도 흐름이 있다면 stale 검증이 Plan을 거부해야 한다.

## Step 4 — StageSessionFactory.cs

### Step 4-1 — 최초 Enemy 위치 lookup 기록

파일:

**Assets/Scripts/ProjectL/Core/Application/StageRun/StageSessionFactory.cs**

추가 위치:

Task 2에서 초기 Enemy Unit을 생성하고 배치하는 기존 루프 주변.

구현 흐름:

~~~text
StageConfig.InitialEnemyPlacements를 입력 순서대로 순회
→ Enemy Unit 생성 및 EntityId 확보
→ 기존 초기 배치 수행
→ EntityId → InitialEnemyPlacementSpec 저장
→ EntityId → 입력 인덱스 저장
→ 조립 마지막에 두 lookup을 읽기 전용 참조로 Service에 전달
~~~

힌트 코드:

~~~csharp
var enemyOrigins =
    new Dictionary<EntityId, InitialEnemyPlacementSpec>();
var enemyInputIndices =
    new Dictionary<EntityId, int>();

for (int index = 0; index < placements.Length; index++)
{
    // 기존 생성/배치
    enemyOrigins.Add(enemyId, placements.Span[index]);
    enemyInputIndices.Add(enemyId, index);
}
~~~

lookup은 Stage 수명 동안 바뀌지 않는 최초 정보다. 이후 Enemy가 이동했다고 갱신하지 않는다.

### Step 4-2 — StagePhaseTransitionService 조립

추가 위치:

Map, Reserve, Phase, Timeline과 Entity lookup 생성이 끝난 뒤, **StageSession** 생성 직전.

힌트 코드:

~~~csharp
StagePhaseTransitionService phaseTransition =
    new StagePhaseTransitionService(
        /* entities, player reserve, map, phase,
           turn/timeline 상태, immutable enemy lookup */);
~~~

Task 3의 OutcomeState/Evaluator, Task 4의 NightState와 마찬가지로 Stage당 하나만 만들고 Session에 전달한다.

## Step 5 — StageSession.cs

### Step 5-1 — 전환 Service 보관

파일:

**Assets/Scripts/ProjectL/Core/Application/StageRun/StageSession.cs**

추가 위치:

클래스 필드 영역과 생성자 의존성 목록.

힌트 코드:

~~~csharp
private readonly StagePhaseTransitionService _phaseTransition;

internal StageSession(
    /* 기존 의존성 */,
    StagePhaseTransitionService phaseTransition)
{
    _phaseTransition = phaseTransition
        ?? throw new ArgumentNullException(nameof(phaseTransition));
}
~~~

이 단계에서는 임의의 새 공개 전환 메서드를 만들지 않는다. Task 6에서 기존 **TryEnterCombat()**, **EndUnitTurn()** 흐름 안에 Service를 연결한다.

## 전체 흐름

~~~text
StageSessionFactory
├─ Enemy 생성 시 EntityId → 최초 배치 Spec 저장
├─ EntityId → 최초 입력 인덱스 저장
└─ StagePhaseTransitionService 생성

밤 종료 예정
→ PlanEnterDeployment
→ 모든 Unit/Map/Reserve/Timeline 검증
→ CommitEnterDeployment
→ 살아 있는 양 진영 Unit만 Reserve로 이동
→ Structure는 Map에 유지

다음 Combat 진입
→ PlanRedeployEnemies
→ 모든 최초 위치와 충돌 검증
→ CommitRedeployEnemies
→ 살아 있는 Enemy Unit만 최초 Anchor/Rotation으로 복귀
~~~

## 완료 체크리스트

- [ ] 살아 있고 배치된 Player/Enemy Unit만 회수 Plan에 들어간다.
- [ ] Structure와 죽은 Unit은 회수 Plan에 들어가지 않는다.
- [ ] Player Unit은 가장 낮은 실제 빈 Player Reserve 슬롯부터 배정된다.
- [ ] Enemy Unit은 최초 입력 인덱스를 내부 ReserveSlotIndex로 사용한다.
- [ ] Enemy가 Player ReserveSlotState를 점유하지 않는다.
- [ ] 최초 Enemy Anchor/Rotation lookup이 Factory에서 한 번 만들어지고 이후 갱신되지 않는다.
- [ ] 살아 있는 Enemy Reserve Unit만 재배치 Plan에 들어간다.
- [ ] 목적 Cell 하나라도 막혔거나 중복되면 Commit 전에 실패한다.
- [ ] Plan 실패와 stale Plan 거부 시 일부 Unit만 이동하는 부분 변경이 없다.
- [ ] Commit은 전체 재검증 뒤 별도 적용 단계로 실행된다.
- [ ] Player HQ, Enemy HQ, Enemy 일반 건물은 회수되지 않는다.
- [ ] StageSession이 Factory가 만든 동일한 전환 Service를 보관한다.

---

# Task 6 — Combat 진입, 승패 판정, 밤 종료의 공개 흐름 통합

## 이번 Task의 책임

Tasks 2~5에서 만든 기능을 실제 Stage 진행 경계에 연결한다. 이 Task부터 View는 실패 가능한 Combat 진입을 Result로 받고, 밤 종료는 Unit 턴 종료에 의해 자동으로 일어나며, Stage가 끝난 뒤 변경 Command는 모두 무변경 실패한다.

핵심 원자성 규칙은 다음과 같다.

~~~text
현재 상태 확인
→ 필요한 모든 Plan 생성
→ 모든 Plan과 Commit 전제 재검증
→ 실패 가능한 계산이 더는 남지 않았는지 확인
→ Commit만 순서대로 실행
→ 최종 Snapshot 반환
~~~

특히 Combat 진입에서 Enemy 재배치를 먼저 Commit한 뒤 Timeline 초기화가 실패하면 안 된다. **TimelineInitializationPlan**은 참가자 구성, 정렬, 중복 검사, 설정 검사, Revision checked 계산, 최종 Dictionary 할당까지 미리 끝낸다. 검증이 끝난 뒤의 Timeline 적용은 완성된 상태를 대입하는 것뿐이며, 같은 단일 스레드 호출 중 외부 변경이 끼어들지 않는다는 불변식 아래 실패할 수 없어야 한다.

## Step 1 — CombatEntryFailureReason.cs

### Step 1-1 — Combat 진입 실패 enum 선언

파일:

**Assets/Scripts/ProjectL/Core/Contracts/Results/CombatEntryFailureReason.cs**

타입 선언:

**public enum**

추가 위치:

새 파일의 Contracts.Results namespace 내부.

역할:

**TryEnterCombat()**이 예외 대신 공개적으로 설명할 수 있는 실패 이유를 정의한다. 원본 Plan에서 반드시 구분해야 하는 사유는 다음과 같다.

- 성공 상태인 **None**
- 이미 Stage가 끝난 **StageEnded**
- Deployment가 아닌 **WrongPhase**
- 진영별 살아 있는 배치 HQ 수가 정확히 하나가 아닌 **InvalidHeadquarters**
- Timeline 참가자 초기화 Plan을 만들 수 없는 **InvalidTimelineConfiguration**

힌트 코드:

~~~csharp
public enum CombatEntryFailureReason
{
    None = 0,
    StageEnded,
    WrongPhase,
    InvalidHeadquarters,
    InvalidTimelineConfiguration,
}
~~~

프로젝트의 다른 FailureReason 값 명명과 순서를 따른다. Unit Catalog 검증에서 Timeline 참가자 설정을 완성할 수 없는 경우도 **InvalidTimelineConfiguration** 경계에서 상태 변경 없이 반환한다. 특히 HQ 불변식 실패와 Timeline 설정 실패는 서로 다른 Result여야 한다.

## Step 2 — CombatEntryResult.cs

### Step 2-1 — 실패 가능한 Combat 진입 Result 선언

파일:

**Assets/Scripts/ProjectL/Core/Contracts/Results/CombatEntryResult.cs**

타입 선언:

**public readonly struct**

추가 위치:

새 파일의 Contracts.Results namespace 내부.

필드/프로퍼티:

~~~csharp
public readonly struct CombatEntryResult
{
    public bool Succeeded { get; }
    public CombatEntryFailureReason FailureReason { get; }
    public StageSnapshot CurrentStage { get; }

    // private 생성자
    // public static Success(...)
    // public static Failure(...)
}
~~~

구성 규칙:

- Success는 **FailureReason.None**과 non-null Snapshot만 허용한다.
- Failure는 **None**이 아닌 정의된 enum과 non-null Snapshot만 허용한다.
- 실패 Snapshot은 변경 전 현재 Stage를 보여준다.
- Result 안에 Exception이나 내부 Plan을 노출하지 않는다.

## Step 3 — AttackFailureReason.cs

### Step 3-1 — StageEnded 추가

파일:

**Assets/Scripts/ProjectL/Core/Contracts/Results/AttackFailureReason.cs**

추가 위치:

기존 **public enum AttackFailureReason** 값 영역.

힌트 코드:

~~~csharp
StageEnded,
~~~

**TryAttack()**의 첫 검사에서 사용한다. 공격자/대상/Phase 검증보다 종결 상태가 우선한다.

## Step 4 — MovementFailureReason.cs

### Step 4-1 — StageEnded 추가

파일:

**Assets/Scripts/ProjectL/Core/Contracts/Results/MovementFailureReason.cs**

추가 위치:

기존 **public enum MovementFailureReason** 값 영역.

**TryMove()**가 Stage 종료 뒤 Map, 위치, 이동 자원을 전혀 바꾸지 않고 이 값을 반환하도록 Task 후반의 Facade에서 연결한다.

## Step 5 — CombatTurnFailureReason.cs

### Step 5-1 — StageEnded 추가

파일:

**Assets/Scripts/ProjectL/Core/Contracts/Results/CombatTurnFailureReason.cs**

추가 위치:

기존 **public enum CombatTurnFailureReason** 값 영역.

**EndUnitTurn()**에서 WrongPhase나 NoActiveUnit보다 먼저 반환한다. Stage를 끝낸 공격 뒤 밤 시간 누적이나 낮 전환이 추가로 일어나지 않게 하는 계약이다.

## Step 6 — CombatTimelineAdvanceFailureReason.cs

### Step 6-1 — StageEnded 추가

파일:

**Assets/Scripts/ProjectL/Core/Contracts/Results/CombatTimelineAdvanceFailureReason.cs**

추가 위치:

기존 **public enum CombatTimelineAdvanceFailureReason** 값 영역.

**AdvanceTimeline()**이 종결 뒤 새 Unit 턴 또는 Gimmick을 시작하지 않게 한다.

## Step 7 — EnemyAiTurnFailureReason.cs

### Step 7-1 — StageEnded 추가

파일:

**Assets/Scripts/ProjectL/Core/Contracts/Results/EnemyAiTurnFailureReason.cs**

추가 위치:

기존 **public enum EnemyAiTurnFailureReason** 값 영역.

이미 끝난 Stage에서 AI 실행을 새로 요청한 경우 사용하는 명시적 거부 사유다.

## Step 8 — EnemyAiTurnResult.cs

### Step 8-1 — Rejected가 StageEnded를 허용하도록 확장

파일:

**Assets/Scripts/ProjectL/Core/Contracts/Results/EnemyAiTurnResult.cs**

추가 위치:

기존 **Rejected(...)** factory의 failureReason 검증 영역.

현재 NoActiveUnit과 ActiveUnitNotEnemy만 허용하는 조건에 **StageEnded**를 추가한다. StageEnded는 actor를 선택하기 전에 검사하므로 UnitId는 null이어야 한다.

구현 흐름:

~~~text
허용된 거부 사유인지 검사
→ 사유별 UnitId 유무 규칙 검사
→ Snapshot null 검사
→ 실패 Result 생성
~~~

### Step 8-2 — CompletedByStageEnd 추가

추가 위치:

기존 **Completed(...)** factory 근처.

함수 시그니처:

~~~csharp
public static EnemyAiTurnResult CompletedByStageEnd(
    EntityId actorId,
    MovementResult? movement,
    AttackResult? attack,
    StageSnapshot currentStage);
~~~

원본 Plan의 의미상 Result는 다음 상태다.

~~~text
Succeeded = true
FailureReason = None
UnitId = actorId
Movement/Attack = 실제 수행 결과
EndTurn = null
CurrentStage = terminal Snapshot
~~~

AI가 자신의 행동을 정상적으로 수행했고 그 행동으로 Stage가 끝난 것이므로 실패가 아니다. 다만 **EndUnitTurn()**은 호출하지 않았기 때문에 EndTurn은 반드시 null이다.

## Step 9 — HeadquartersPlacementFailureReason.cs

### Step 9-1 — StageEnded 명시적 실패 확인

파일:

**Assets/Scripts/ProjectL/Core/Contracts/Results/HeadquartersPlacementFailureReason.cs**

추가 위치:

기존 **public enum HeadquartersPlacementFailureReason** 값 영역.

힌트 코드:

~~~csharp
StageEnded,
~~~

이미 값이 추가되어 있다면 중복 선언하지 말고 Task 6 연결만 확인한다. **TryPlaceInitialPlayerHeadquarters()**는 Stage 종료 뒤 이 사유를 반환한다. 이 경계는 상세 실패 enum이 없는 경계가 아니다.

## Step 10 — CombatTimelineState.cs

### Step 10-1 — 완성된 초기 상태를 적용하는 Domain 경계 추가

파일:

**Assets/Scripts/ProjectL/Core/Domain/Combat/CombatTimelineState.cs**

추가 위치:

기존 **Initialize(...)** 근처의 초기화/상태 적용 영역.

역할:

기존 Initialize는 검증, 참가자 생성, 정렬, Dictionary 할당, checked Revision 증가를 한 메서드에서 수행한다. Combat 진입 원자성을 위해 이 실패 가능 작업을 Application의 Plan 단계로 옮기고, Domain에는 사전 완성된 상태를 적용하는 메서드를 둔다.

원본 Plan은 이 Domain 적용 메서드의 이름을 별도로 고정하지 않는다. 새 public API를 만들지 말고, 기존 **Initialize(...)** 주변에 사전 완성 상태를 받는 internal 적용 경계를 둔다.

힌트 형태:

~~~text
internal 반환형 void인 적용 메서드
입력:
  Dictionary<EntityId, CombatTimelineParticipantState> preparedParticipants
  long finalRevision

본문:
  Plan이 소유한 완성 Dictionary 대입
  CurrentTime = 0
  CompletedUnitTurnCount = 0
  IsInitialized = true
  Revision = finalRevision
~~~

이 메서드는 Commit 시 다음 일을 해서는 안 된다.

- Entity lookup
- 참가자 추가 루프 중 중복 검사
- Priority/Delay 유효성 검사
- 정렬
- 새 Dictionary 할당
- checked Revision 계산

모두 **PlanInitialize()**에서 끝나 있어야 한다. 적용 메서드에 방어 검증을 다시 넣어 Commit 실패 지점을 만드는 방식은 피한다.

## Step 11 — CombatTimelineService.cs

### Step 11-1 — TimelineInitializationPlan 선언

파일:

**Assets/Scripts/ProjectL/Core/Application/Combat/CombatTimelineService.cs**

타입 선언:

**internal sealed class**

추가 위치:

같은 파일의 namespace 내부, **CombatTimelineService** 선언 앞이나 기존 Timeline Plan 타입 배치 규칙과 같은 위치.

선언 형태:

~~~csharp
internal sealed class TimelineInitializationPlan
{
    internal long ExpectedRevision { get; }
    internal long FinalRevision { get; }
    internal Dictionary<EntityId, CombatTimelineParticipantState>
        PreparedParticipants { get; }

    internal TimelineInitializationPlan(
        long expectedRevision,
        long finalRevision,
        Dictionary<EntityId, CombatTimelineParticipantState>
            preparedParticipants);
}
~~~

**PreparedParticipants**는 단순 후보 목록이 아니라 정렬과 검증을 끝내고 즉시 적용 가능한 최종 상태다. Commit 전에 다시 만들어서는 안 된다.

### Step 11-2 — PlanInitialize 구현

추가 위치:

기존 **Initialize(...)**를 대체하거나 그 근처의 초기화 API 영역.

함수 시그니처:

~~~csharp
internal TimelineInitializationPlan PlanInitialize(
    IReadOnlyDictionary<EntityId, CombatEntityState> entities,
    EnemyRedeploymentPlan enemyRedeploymentPlan);
~~~

구현 흐름:

~~~text
입력 null 확인
→ 현재 Timeline Revision을 ExpectedRevision으로 캡처
→ 현재 살아 있고 Combat에 배치된 Unit 수집
→ EnemyRedeploymentPlan의 재배치 예정 Unit 추가
→ null/중복 EntityId 검사
→ 각 Unit의 생존 및 최종 배치 예상 상태 검사
→ Priority/Delay 설정 검사
→ EntityId 기준 정렬
→ CombatTimelineParticipantState 전부 생성
→ 최종 Dictionary 전부 생성
→ checked(ExpectedRevision + 1)로 FinalRevision 계산
→ 완성된 Plan 반환
~~~

재배치 예정 Enemy는 아직 실제 EntityLocation이 Reserve다. 따라서 현재 **IsDeployed**만 보고 제외하지 말고 **EnemyRedeploymentPlan**에 들어 있다는 사실로 Commit 후 배치 상태를 투영한다. 반대로 Plan에 없는 Reserve Unit은 참가자가 아니다.

### Step 11-3 — ValidateInitialization 구현

함수 시그니처:

~~~csharp
internal void ValidateInitialization(
    TimelineInitializationPlan plan);
~~~

구현 흐름:

~~~text
plan null 확인
→ 현재 Timeline Revision == plan.ExpectedRevision 확인
→ Plan의 완성 상태가 해당 Plan 인스턴스 그대로 유지되는지 확인
→ 적용 가능 상태임을 확정
~~~

가장 중요한 검사는 Revision이다. 이 검증은 반드시 **CommitRedeployEnemies()**보다 먼저 실행한다.

### Step 11-4 — CommitInitialize 구현

함수 시그니처:

~~~csharp
internal void CommitInitialize(
    TimelineInitializationPlan plan);
~~~

구현 흐름:

~~~text
사전 검증된 Plan 수신
→ PreparedParticipants와 FinalRevision을 Domain 적용 메서드에 전달
~~~

**TryEnterCombat()**의 검증 구간이 끝난 뒤 같은 단일 스레드 호출 안에서 즉시 실행된다는 전제를 문서 주석으로 남긴다. 이 메서드는 다시 조회·정렬·할당·checked 연산하지 않아야 하며, 그 결과 Enemy 재배치 뒤 Timeline 적용 실패라는 부분 Commit 경로가 사라진다.


## Step 12 — StageSession.cs

### Step 12-1 — Task 3~5 의존성 보관

파일:

**Assets/Scripts/ProjectL/Core/Application/StageRun/StageSession.cs**

추가 위치:

클래스 필드 영역과 internal 생성자 인자/대입 영역.

추가할 연결:

~~~csharp
private readonly StageOutcomeState _outcomeState;
private readonly StageOutcomeEvaluator _outcomeEvaluator;
private readonly StageNightState _nightState;
private readonly StagePhaseTransitionService _phaseTransition;
~~~

Factory에서 만들어 **StageSnapshotBuilder**, **StageCombatFacade**와 공유하는 바로 그 인스턴스를 받는다. 각 클래스가 OutcomeState나 NightState를 새로 만들면 판정과 Snapshot이 서로 다른 상태를 보게 된다.

### Step 12-2 — TryEnterCombat 구현

추가 위치:

기존 **EnterCombat()** 근처의 public Stage 흐름 API 영역.

함수 시그니처:

~~~csharp
public CombatEntryResult TryEnterCombat();
~~~

구현 흐름:

~~~text
1. StageOutcomeState.IsStageEnded 확인
2. 현재 Phase가 Deployment인지 확인
3. 살아 있고 Combat에 배치된 Player HQ 수 계산
4. 살아 있고 Combat에 배치된 Enemy HQ 수 계산
5. 양쪽 모두 정확히 하나인지 확인
6. Combat 후보 Unit의 Catalog/정적 설정 검증
7. StagePhaseTransitionService.PlanRedeployEnemies 생성
8. 현재 배치 Unit + 재배치 예정 Enemy를 사용해 TimelineInitializationPlan 생성
9. Enemy 재배치 Plan의 현재 상태 재검증
10. Timeline ValidateInitialization으로 Revision 포함 사전 검증
11. Night reset, Phase 전이, Player HQ placement lock의 전제 확인
12. 모든 검증 성공 후 Enemy 재배치 Commit
13. Timeline CommitInitialize
14. Night ResetForCombat
15. Phase를 Combat으로 Commit
16. Player HQ placement 잠금
17. StageOutcomeEvaluator.EvaluateAndCommit
18. CombatEntryResult.Success와 최신 Snapshot 반환
~~~

초기 가드의 힌트:

~~~csharp
if (_outcomeState.IsStageEnded)
    return CombatEntryResult.Failure(
        CombatEntryFailureReason.StageEnded,
        _snapshotBuilder.Build());

if (_phaseState.Current != StagePhase.Deployment)
    return CombatEntryResult.Failure(
        CombatEntryFailureReason.WrongPhase,
        _snapshotBuilder.Build());
~~~

HQ 집계 시 조건을 일부만 보지 않는다.

~~~text
EntityKind == Headquarters
AND Owner == 해당 진영
AND IsDead == false
AND Area == Combat
AND CombatAnchor가 존재
~~~

일반 Structure는 HQ 개수에 포함하지 않는다. 각 진영의 살아 있는 배치 HQ가 0개 또는 2개 이상이면 **InvalidHeadquarters**이며 어떤 상태도 바꾸지 않는다.

Timeline Plan 생성에서 설정 오류나 overflow가 발생하면 이를 공개 **InvalidTimelineConfiguration** 실패로 변환한다. 이 시점에는 Enemy가 아직 Reserve, Timeline Revision과 Night/Phase/HQ lock도 이전 상태여야 한다.

Commit 순서의 핵심:

~~~csharp
// 여기 위에서 모든 실패 가능한 검증이 끝나야 한다.
_phaseTransition.CommitRedeployEnemies(enemyPlan);
_timelineService.CommitInitialize(timelinePlan); // 사전 완성 상태 대입만
_nightState.ResetForCombat();
// phase Commit
// player HQ placement lock
_outcomeEvaluator.EvaluateAndCommit();
~~~

Player Combat Unit 최소 수 검사는 넣지 않는다. Player Unit이 Combat과 Player Reserve 양쪽에 모두 없다면 Combat 진입 자체는 성공하고, 마지막 판정 단계에서 **NoPlayerUnitsRemaining** Defeat가 즉시 확정된다. Player Unit이 Reserve에 하나라도 살아 있으면 패배가 아니다.

### Step 12-3 — EnterCombat 호환 wrapper 유지

기존 **EnterCombat()**을 삭제하지 않는다.

~~~csharp
public StageSnapshot EnterCombat()
{
    CombatEntryResult result = TryEnterCombat();
    if (result.Succeeded == false)
        throw new InvalidOperationException(
            result.FailureReason.ToString());

    return result.CurrentStage;
}
~~~

새 View는 실패 이유가 필요한 경우 Try API를 사용하고, 기존 호출자는 wrapper를 통해 호환된다.

### Step 12-4 — Stage 변경 API의 terminal guard 연결

추가 위치:

각 기존 public 변경 함수의 가장 첫 상태 판정 위치.

대상과 반환 규칙:

- **TryPlaceInitialPlayerHeadquarters()** → **HeadquartersPlacementFailureReason.StageEnded**
- **TryPlaceOnCombat()**, **TryReturnToReserve()**처럼 **DeploymentResult**만 반환하는 경계 → false와 변경 없는 Snapshot
- Combat 진입 → **CombatEntryFailureReason.StageEnded**

Preview, GetSnapshot, TryGetEntitySnapshot 같은 읽기 전용 Query는 허용한다. Query까지 막으면 최종 결과 화면과 연출이 Snapshot을 읽지 못한다.

StageEnded를 PlacementLocked나 WrongPhase보다 뒤에서 검사하지 않는다. Stage가 끝났다면 최종 실패 이유는 항상 StageEnded여야 한다.

## Step 13 — StageCombatFacade.cs

### Step 13-1 — Outcome/Night/전환 의존성 연결

파일:

**Assets/Scripts/ProjectL/Core/Application/StageRun/StageCombatFacade.cs**

추가 위치:

클래스 필드와 internal 생성자.

힌트 코드:

~~~csharp
private readonly StageOutcomeState _outcomeState;
private readonly StageOutcomeEvaluator _outcomeEvaluator;
private readonly StageNightState _nightState;
private readonly StagePhaseTransitionService _phaseTransition;
~~~

Factory가 조립한 같은 Stage 범위 인스턴스를 전달받는다. 이 Facade는 공격, Timeline, 턴 종료라는 서로 다른 Service를 Stage 규칙으로 묶는 Application 경계다.

### Step 13-2 — 변경 Command에 StageEnded guard 추가

추가 위치:

다음 변경 함수의 기존 Phase/Entity 검사보다 앞.

- **RunActiveEnemyTurn()**
- **AdvanceTimeline()**
- **EndUnitTurn()**
- **TryMove()**
- **TryAttack()**
- Unit/Gimmick 상태를 실제 변경하는 다른 공개 Command

각 함수는 자기 Result의 StageEnded failure를 사용한다. 상세 실패 enum이 있는 경계에서는 일반 false로 뭉개지 않는다.

읽기 전용인 **GetMovementRange()**, **PreviewMove()**, **GetAttackRange()**, **PreviewAttack()**, **GetTimeline()**은 최종 표시를 위해 계속 조회 가능하다. 단, 기존 Phase 검증 규칙은 유지한다.

### Step 13-3 — TryAttack 성공 직후 Outcome 판정

추가 위치:

기존 **TryAttack()**의 공격 및 Timeline 제거 Commit이 모두 끝난 직후, 최종 Snapshot을 만들기 전.

구현 흐름:

~~~text
StageEnded 사전 검사
→ AttackResolutionPlan 생성
→ TimelineRemovalPlan 생성/검증
→ 공격 Commit
→ pending movement 및 Timeline 제거 Commit
→ StageOutcomeEvaluator.EvaluateAndCommit
→ 판정이 반영된 Snapshot 생성
→ AttackResult.Success 반환
~~~

피해 계산기인 **UnitAttackService** 안에서 Stage 결과를 정하지 않는다. 같은 공격 서비스가 Stage 밖에서 재사용될 수 있고, HQ/Player Reserve 같은 Stage 전체 정보도 소유하지 않기 때문이다.

Enemy HQ가 죽으면 이 함수가 반환하는 **AttackResult.CurrentStage**에 즉시 Victory가 보여야 한다. Player HQ가 죽으면 즉시 Defeat다.

### Step 13-4 — Gimmick 활성화 직후 Outcome 판정

추가 위치:

기존 **AdvanceTimeline()**에서 due Gimmick을 실제로 Activate한 직후.

구현 흐름:

~~~text
Gimmick 활성화 Commit
→ StageOutcomeEvaluator.EvaluateAndCommit
→ 종결 여부가 반영된 Snapshot 생성
→ CombatTimelineAdvanceResult 반환
~~~

Gimmick이 Unit이나 HQ에 피해를 줄 수 있으므로 공격과 같은 판정 경계를 사용한다. 미래에 다른 사망 Command가 생기면 그 Commit 직후에도 같은 원칙을 적용한다.

### Step 13-5 — EndUnitTurn을 Plan-검증-Commit으로 확장

추가 위치:

기존 **EndUnitTurn()** 본문.

구현 흐름:

~~~text
StageEnded 확인
→ Combat Phase와 active Unit 확인
→ active Unit/Timeline 일관성 확인
→ UnitDefinition 조회
→ TraitActionPlan 생성
→ TimelineEndPlan 생성
→ NightAdvancePlan을 unit.Delay로 생성
→ NightAdvancePlan.EndsNight이면 PlanEnterDeployment 생성
→ Timeline/Trait/Night/회수 Plan의 모든 전제 검증
→ unit pending movement Commit
→ TimelineEndPlan Commit
→ Trait 전이 Commit
→ CombatTurnState.End
→ NightAdvancePlan Commit
→ EndsNight이면 StagePhaseTransitionPlan Commit
→ 최신 Snapshot으로 CombatTurnResult.Success 반환
~~~

핵심 힌트:

~~~csharp
NightAdvancePlan nightPlan =
    _nightState.PlanAdvance(unit.Delay);

StagePhaseTransitionPlan deploymentPlan = null;
if (nightPlan.EndsNight)
    deploymentPlan = _phaseTransition.PlanEnterDeployment();

// 위 Plan들과 현재 상태를 모두 검증한 뒤에만 Commit 시작
~~~

회수 Plan은 active turn 종료와 Timeline 종료 상태까지 예상해 검증해야 한다. 실제 Commit 도중 새 검증 실패가 생기지 않도록 Plan이 필요한 최종값을 미리 가져야 한다.

낮 전환 후 Snapshot 규칙:

- CurrentPhase는 Deployment다.
- CurrentNightTime은 NightLength에 도달하거나 초과한 값 그대로다.
- 살아 있는 양 진영 Unit은 Reserve다.
- 모든 Structure는 원래 Map 위치에 있다.
- Night time은 다음 **TryEnterCombat()** 성공 때만 0으로 초기화된다.

공격이 Stage를 끝냈다면 호출자가 **EndUnitTurn()**을 이어서 실행하지 않는다. 혹시 호출해도 첫 StageEnded guard로 실패하므로 Delay가 더해지지 않는다.

## Step 14 — EnemyAiTurnService.cs

### Step 14-1 — 실행 시작 시 terminal 거부

파일:

**Assets/Scripts/ProjectL/Core/Application/Combat/EnemyAi/EnemyAiTurnService.cs**

추가 위치:

**Run(...)**의 null 검사 직후, active Unit 조회 전.

힌트 코드:

~~~csharp
if (initialStage.Outcome.IsStageEnded)
{
    return EnemyAiTurnResult.Rejected(
        EnemyAiTurnFailureReason.StageEnded,
        null,
        initialStage);
}
~~~

종결 상태는 NoActiveUnit보다 우선한다.

### Step 14-2 — 이동·공격 뒤 terminal이면 턴 종료 생략

추가 위치:

이동 또는 공격 Result를 받은 각 분기에서 **CompleteByEndingTurn(...)**를 호출하기 전.

구현 흐름:

~~~text
이동 또는 공격 수행
→ Result.CurrentStage.Outcome.IsStageEnded 확인
→ true이면 CompletedByStageEnd 반환
→ false이면 기존 CompleteByEndingTurn 흐름 계속
~~~

공격 분기의 힌트:

~~~csharp
attack = combat.TryAttack(activeId, targetId);

if (attack.Value.CurrentStage.Outcome.IsStageEnded)
{
    return EnemyAiTurnResult.CompletedByStageEnd(
        activeId,
        movement,
        attack,
        attack.Value.CurrentStage);
}

return CompleteByEndingTurn(
    combat, activeId, movement, attack);
~~~

실패한 공격 Result도 CurrentStage를 갖기 때문에 성공 여부와 종결 여부를 혼동하지 않는다. Stage 종결은 실제 Commit 결과 Snapshot으로 판단한다.

## 전체 흐름

~~~text
Deployment의 StageSession.TryEnterCombat
→ terminal/Phase/HQ/Catalog 검증
→ EnemyRedeploymentPlan
→ TimelineInitializationPlan
→ 두 Plan + Revision + 나머지 전제 전체 검증
→ Enemy 재배치 Commit
→ 완성 Timeline 상태 적용
→ Night 0 초기화
→ Combat Phase
→ Player HQ 배치 잠금
→ Outcome 판정
→ CombatEntryResult

Combat의 StageCombatFacade.EndUnitTurn
→ Trait + Timeline + Night Plan
→ 경계 도달 시 회수 Plan까지 생성/검증
→ 전부 Commit
→ 양 진영 Unit 회수 및 Deployment 자동 전환

공격/Gimmick Commit
→ StageOutcomeEvaluator
→ Victory/Defeat 잠금
→ 이후 모든 변경 Command는 StageEnded 무변경 실패
~~~

## 완료 체크리스트

- [ ] TryEnterCombat과 기존 EnterCombat wrapper가 함께 존재한다.
- [ ] terminal 검사가 Phase, 배치 잠금, active Unit 검사보다 우선한다.
- [ ] 진영별 살아 있는 배치 HQ가 정확히 하나일 때만 Combat 진입을 계속한다.
- [ ] Player Combat Unit이 0명이어도 Combat 진입 자체는 허용된다.
- [ ] Player Combat/Reserve Unit이 모두 없으면 Outcome evaluator가 즉시 Defeat를 확정한다.
- [ ] EnemyRedeploymentPlan과 TimelineInitializationPlan을 상태 변경 전에 모두 만든다.
- [ ] Timeline의 중복/설정/정렬/Dictionary 생성/Revision checked 계산이 Plan 단계에서 끝난다.
- [ ] ValidateInitialization이 Enemy 재배치 Commit 전에 현재 Revision을 확인한다.
- [ ] CommitInitialize와 Domain 적용 메서드에 실패 가능한 조회·할당·checked 연산이 남지 않는다.
- [ ] Timeline Plan 실패 시 Enemy 위치, Timeline, Night, Phase, HQ lock이 모두 그대로다.
- [ ] Combat 진입 Commit 순서가 Enemy 재배치 → Timeline → Night reset → Phase → HQ lock이다.
- [ ] TryAttack과 Gimmick Commit 직후 Outcome을 판정한다.
- [ ] 성공한 EndUnitTurn만 raw Unit.Delay를 밤 시간에 정확히 한 번 더한다.
- [ ] 밤 경계 턴은 Trait/Timeline/Night/회수 Plan을 모두 검증한 뒤 Commit한다.
- [ ] 낮 Snapshot은 도달·초과한 밤 시간을 유지하고 Structure를 회수하지 않는다.
- [ ] HQ 배치 종료 후 호출은 HeadquartersPlacementFailureReason.StageEnded를 반환한다.
- [ ] 상세 failure enum이 없는 DeploymentResult만 false와 변경 없는 Snapshot을 반환한다.
- [ ] Enemy AI의 종결 행동은 EndTurn이 null인 성공 Result를 반환한다.
- [ ] 종결 공격 뒤 EndUnitTurn이 호출되지 않아 밤 시간이 증가하지 않는다.
- [ ] 읽기 전용 Query는 Stage 종료 후에도 사용할 수 있다.

---

# Task 7 — Timeline 밤 종료 예측

## 이번 Task의 책임

기존 Timeline의 Unit/Gimmick 순서를 유지하면서, 각 예상 Unit 턴 뒤 밤 시간이 얼마가 되는지와 그 턴이 밤을 끝내는지를 공개 Snapshot에 표시한다. 공간이 있으면 별도 **NightEnd** Entry도 추가한다.

이 기능은 순수 Query다. 예측 중 **StageNightState.CurrentNightTime**, **CombatTimelineState.CurrentTime**, Revision, 참가자 상태를 변경하지 않는다.

두 시간의 의미를 분리한다.

~~~text
CombatTimelineState.CurrentTime
→ Scheduler가 Unit 행동 순서를 계산하는 시간

StageNightState.CurrentNightTime
→ 실제 완료된 Unit 턴의 raw Delay 누적값
~~~

밤 종료 예측은 두 번째 값에 Unit.Delay를 더하지만, Entry의 기존 scheduled time과 정렬 규칙은 그대로 둔다.

## Step 1 — CombatTimelineEntryKind.cs

### Step 1-1 — NightEnd 종류 추가

파일:

**Assets/Scripts/ProjectL/Core/Contracts/Common/CombatTimelineEntryKind.cs**

타입 선언:

기존 **public enum** 수정.

추가 위치:

기존 StageGimmick 뒤.

~~~csharp
public enum CombatTimelineEntryKind
{
    None = 0,
    UnitTurn = 1,
    StageGimmick = 2,
    NightEnd = 3,
}
~~~

기존 숫자를 바꾸지 않는다. NightEnd는 실행 가능한 Entity가 아니라 예측 경계 표시다.

## Step 2 — CombatTimelineEntrySnapshot.cs

### Step 2-1 — Unit 턴 뒤 예상 밤 정보 추가

파일:

**Assets/Scripts/ProjectL/Core/Contracts/Snapshots/CombatTimelineEntrySnapshot.cs**

추가 위치:

기존 public 프로퍼티 영역과 private 생성자 인자/대입 영역.

~~~csharp
public long? ProjectedNightTimeAfterTurn { get; }
public bool EndsNightAfterTurn { get; }
~~~

값 규칙:

- 실제 Stage Forecast의 UnitTurn에는 누적 예상 시간을 넣는다.
- 해당 값이 NightLength 이상이면 EndsNightAfterTurn은 true다.
- StageGimmick과 NightEnd에는 Unit 행동 뒤 시간이라는 의미가 없으므로 nullable 값을 비운다.
- NightEnd 자신은 “앞 Unit 뒤 밤 종료”를 나타내므로 EndsNightAfterTurn을 Unit처럼 사용하지 않는다.

### Step 2-2 — 기존 ForUnit 호환 overload 유지

추가 위치:

기존 **ForUnit(...)** factory 영역.

기존 호출부나 계약 테스트가 사용하는 overload는 삭제하지 않는다. 새 overload가 예상 값을 받게 하고, 기존 overload는 의미 있는 기본값으로 새 overload를 호출하게 구성한다.

힌트 형태:

~~~csharp
public static CombatTimelineEntrySnapshot ForUnit(
    /* 기존 인자 */,
    long projectedNightTimeAfterTurn,
    bool endsNightAfterTurn)
{
    // 기존 검증 + 새 값 검증 + Snapshot 생성
}

public static CombatTimelineEntrySnapshot ForUnit(
    /* 기존 인자 */)
{
    // 호환을 위해 기존 의미를 보존해 위 overload로 위임
}
~~~

예상 시간이 음수인 조합이나 Unit이 아닌 Kind에 Unit 전용 값이 들어가는 조합을 만들지 않는다.

### Step 2-3 — ForNightEnd factory 추가

함수 시그니처:

~~~csharp
public static CombatTimelineEntrySnapshot ForNightEnd(
    long scheduledTime,
    long nightLength);
~~~

역할:

View가 Timeline 마지막에 밤 종료 경계를 별도 항목으로 표시하고 싶을 때 사용할 Snapshot을 만든다. EntityId나 Gimmick 식별자는 비워 두고, 기존 Entry가 요구하는 공통 시간 필드만 유효하게 채운다.

검증 흐름:

~~~text
scheduledTime 유효성 확인
→ nightLength가 양수인지 확인
→ Kind = NightEnd
→ Entity/Gimmick 전용 필드는 비움
→ 불변 Snapshot 반환
~~~

## Step 3 — CombatTimelineService.cs

### Step 3-1 — GetTimeline에 Entity lookup과 NightState 연결

파일:

**Assets/Scripts/ProjectL/Core/Application/Combat/CombatTimelineService.cs**

추가 위치:

필드/생성자와 기존 **GetTimeline(...)** 영역.

필요 연결:

- EntityId로 **UnitState.Delay**를 조회할 Stage Entity lookup
- 현재 밤 시작값과 NightLength를 제공할 **StageNightState**

힌트 형태:

~~~csharp
internal CombatTimelineResult GetTimeline(
    EntityId activeUnitId,
    int maxEntries)
{
    CombatTimelineForecastEntry[] unitEntries =
        _scheduler.BuildForecast(
            _timelineState,
            activeUnitId,
            maxEntries);

    CombatTimelineEntrySnapshot[] combined =
        _stageGimmickTimelineService.BuildCombinedForecast(
            _timelineState.CompletedUnitTurnCount,
            _timelineState.CurrentTime,
            unitEntries,
            maxEntries,
            _entities,
            _nightState);

    return CombatTimelineResult.Success(
        Revision,
        _timelineState.CurrentTime,
        combined);
}
~~~

정확한 인자 순서는 기존 코드 스타일을 따르되 정보 흐름은 바꾸지 않는다. 이 메서드가 직접 밤 종료 병합 루프를 새로 만들지 않고 기존 **StageGimmickTimelineService**의 병합 책임을 확장한다.

## Step 4 — StageGimmickTimelineService.cs

### Step 4-1 — BuildCombinedForecast 입력 확장

파일:

**Assets/Scripts/ProjectL/Core/Application/StageGimmicks/StageGimmickTimelineService.cs**

추가 위치:

기존 **BuildCombinedForecast(...)** 시그니처와 본문.

추가 입력의 역할:

- Entity lookup: Unit Forecast Entry의 EntityId로 실제 살아 있는 UnitState와 raw Delay를 찾는다.
- StageNightState: 예상 누적 시작값과 NightLength를 제공한다.

기존 Unit/Gimmick 병합 알고리즘을 교체하지 않는다. 기존 순서를 결정하는 지점에 밤 누적과 중단 조건을 끼워 넣는다.

### Step 4-2 — 예상 밤 시간 누적 루프 구현

구현 흐름:

~~~text
projectedNightTime = StageNightState.CurrentNightTime
→ 기존 방식으로 다음 Unit/Gimmick 후보 비교
→ Unit Entry를 채택할 때 EntityId로 살아 있는 Combat Unit 조회
→ checked(projectedNightTime + unit.Delay)
→ ProjectedNightTimeAfterTurn에 결과 기록
→ 결과 >= NightLength인지 판정
→ false면 기존 병합 계속
→ true면 Unit Entry의 EndsNightAfterTurn = true
→ 출력 공간이 남으면 NightEnd Entry 추가
→ 이후 Unit/Gimmick 예측을 모두 중단
~~~

핵심 루프 힌트:

~~~csharp
long projectedNightTime = nightState.CurrentNightTime;

// 기존 merge loop 안에서 Unit Entry를 실제로 선택한 시점
UnitState unit = GetRequiredLivingCombatUnit(
    entities,
    unitForecast.EntityId);

projectedNightTime = checked(
    projectedNightTime + unit.Delay);

bool endsNight =
    projectedNightTime >= nightState.NightLength;

// 새 값이 든 Unit Snapshot을 먼저 추가

if (endsNight)
{
    if (results.Count < maxEntries)
    {
        results.Add(
            CombatTimelineEntrySnapshot.ForNightEnd(
                /* 경계가 놓일 scheduled time */,
                nightState.NightLength));
    }

    break;
}
~~~

active Unit도 Forecast에 포함되는 기존 구조라면 동일하게 Delay를 누적한다. active Unit이라는 이유로 밤 시간 계산에서 빼지 않는다.

### Step 4-3 — NightEnd 우선순위 처리

같은 Unit 턴 완료로 Gimmick이 due가 되더라도, 그 Unit이 NightLength에 도달하면 NightEnd를 먼저 확정하고 해당 Gimmick을 반환하지 않는다.

~~~text
경계 Unit 추가
→ 밤 종료 판정
→ NightEnd를 넣을 공간 확인
→ Forecast 중단
→ Gimmick due 계산/추가로 진행하지 않음
~~~

NightEnd Entry를 넣을 공간이 없더라도 경계 Unit의 **EndsNightAfterTurn**은 true로 유지한다. 예를 들어 maxEntries가 1이면 Unit 하나만 반환되더라도 View는 그 턴 뒤 낮이 된다는 사실을 알 수 있다.

반대로 maxEntries 범위 안에서 누적 시간이 경계에 도달하지 않는다면 NightEnd를 마지막 칸에 억지로 넣지 않는다. “언젠가 밤이 끝난다”가 아니라 현재 Forecast 범위에 실제 경계가 들어올 때만 표시한다.

### Step 4-4 — Query 무변경 보장

BuildCombinedForecast는 지역 변수와 새 Snapshot 배열만 만든다. 다음 상태를 수정하지 않는다.

- StageNightState.CurrentNightTime
- CombatTimelineState.CurrentTime
- CompletedUnitTurnCount
- Timeline Revision/참가자
- UnitState.Delay 및 위치

밤 예상 더하기에서 overflow가 나면 실제 상태는 당연히 그대로여야 한다.

## Step 5 — StageCombatFacade.cs

### Step 5-1 — GetTimeline 연결 갱신

파일:

**Assets/Scripts/ProjectL/Core/Application/StageRun/StageCombatFacade.cs**

추가 위치:

기존 **GetTimeline(int maxEntries)** 본문.

구현 흐름:

~~~text
maxEntries 검증
→ 기존 Combat Phase 검증
→ active UnitId 결정
→ CombatTimelineService.GetTimeline 호출
→ 밤 예상 정보가 포함된 CombatTimelineResult 반환
~~~

StageCombatFacade가 별도로 Delay를 누적하거나 Entry를 수정하지 않는다. Entity lookup과 NightState를 Service 생성자에서 이미 주입했다면 호출 시그니처는 기존처럼 간단하게 유지할 수 있다.

## 전체 흐름

~~~text
StageCombatFacade.GetTimeline(maxEntries)
→ CombatTimelineService
→ Scheduler의 기존 Unit Forecast
→ StageGimmickTimelineService.BuildCombinedForecast
   ├─ 기존 Unit/Gimmick 정렬 유지
   ├─ Entity lookup에서 각 Unit.Delay 조회
   ├─ CurrentNightTime부터 checked 누적
   ├─ Unit Entry에 예상 시간/종료 Flag 기록
   └─ 경계면 선택적 NightEnd 추가 후 중단
→ CombatTimelineResult
→ View가 Unit Flag 또는 NightEnd Entry를 선택적으로 표시
~~~

## 완료 체크리스트

- [ ] CombatTimelineEntryKind의 기존 숫자는 유지되고 NightEnd는 3이다.
- [ ] CombatTimelineEntrySnapshot에 ProjectedNightTimeAfterTurn과 EndsNightAfterTurn이 있다.
- [ ] 기존 ForUnit overload가 유지된다.
- [ ] 실제 Stage Forecast의 모든 Unit Entry가 raw Delay 기반 예상 시간을 가진다.
- [ ] active Unit의 Delay도 예상 누적에 포함된다.
- [ ] 누적은 StageNightState.CurrentNightTime에서 시작하고 checked 연산을 사용한다.
- [ ] 경계 Unit은 EndsNightAfterTurn이 true다.
- [ ] 공간이 있으면 경계 Unit 바로 뒤에 NightEnd가 추가된다.
- [ ] NightEnd가 들어갈 공간이 없어도 Unit의 종료 Flag는 유지된다.
- [ ] 경계가 maxEntries 밖이면 NightEnd를 억지로 표시하지 않는다.
- [ ] 경계 Unit으로 due가 된 Gimmick과 이후 모든 Entry는 반환하지 않는다.
- [ ] 경계 전까지 기존 Unit/Gimmick 순서와 tie-break가 바뀌지 않는다.
- [ ] Forecast Query가 Night, Timeline, Unit 상태를 변경하지 않는다.
- [ ] CombatTimelineState.CurrentTime은 밤 누적에 사용하거나 수정하지 않는다.

---

# Task 8 — Enemy AI Unit 우선, Headquarters fallback

## 이번 Task의 책임

Enemy AI가 공격과 추적 대상을 고를 때 다음 두 모드 중 정확히 하나만 사용하게 한다.

~~~text
살아 있는 hostile Combat Unit이 하나라도 있음
→ Unit만 공격/추적 후보
→ HQ는 더 가깝거나 사거리 안이어도 무시

살아 있는 hostile Combat Unit이 하나도 없음
→ Headquarters만 공격/추적 후보
~~~

여기서 Player Reserve Unit은 Combat Unit이 아니다. Player Unit이 Reserve에만 남아 있다면 Enemy는 전장에 있는 Player HQ를 노린다. 생산 예약은 애초에 Stage Entity/Reserve 생존 판정 경계에 들어오지 않는다.

기존 Manhattan 거리, EntityId 대상 tie-break, 이동 목적지 tie-break는 그대로 유지한다. 이 Task는 후보 **종류를 선택하는 앞단 필터**만 확장한다.

## Step 1 — EnemyAiPolicy.cs

### Step 1-1 — hostile Combat Unit 존재 확인 helper 추가

파일:

**Assets/Scripts/ProjectL/Core/Application/Combat/EnemyAi/EnemyAiPolicy.cs**

추가 위치:

클래스 하단의 기존 private 조회/필터 helper 영역.

함수 시그니처:

~~~csharp
private static bool HasLivingHostileCombatUnit(
    StageSnapshot stage,
    Owner actorOwner);
~~~

판정 조건:

~~~text
snapshot.Owner != actorOwner
AND snapshot.Kind == EntityKind.Unit
AND snapshot.Area == EntityArea.Combat
AND snapshot.IsDead == false
AND snapshot.CombatAnchor가 존재
~~~

힌트 루프:

~~~csharp
foreach (EntitySnapshot entity in stage.Entities.Span)
{
    if (entity.Owner == actorOwner)
        continue;
    if (entity.Kind != EntityKind.Unit)
        continue;
    if (entity.Area != EntityArea.Combat || entity.IsDead)
        continue;
    if (!entity.CombatAnchor.HasValue)
        continue;

    return true;
}

return false;
~~~

Reserve의 살아 있는 Player Unit은 이 함수의 결과를 true로 만들지 않는다. HQ 역시 Unit 존재 여부를 묻는 함수이므로 포함하지 않는다.

### Step 1-2 — 후보 Kind 선택 helper 추가

추가 위치:

Step 1-1과 같은 private helper 영역.

함수 시그니처:

~~~csharp
private static bool IsAllowedTargetKind(
    EntitySnapshot target,
    bool targetUnits);
~~~

힌트 코드:

~~~csharp
return targetUnits
    ? target.Kind == EntityKind.Unit
    : target.Kind == EntityKind.Headquarters;
~~~

일반 **EntityKind.Structure**는 fallback 공격 대상이 아니다. HQ Key를 가진 건물은 Task 2에서 **EntityKind.Headquarters**로 강제되므로 여기서는 Key를 다시 해석하지 않는다.

### Step 1-3 — 일반 living Combat Entity 조회 helper 추가

추가 위치:

기존 **TryFindCombatUnit(...)** 근처.

함수 시그니처:

~~~csharp
private static bool TryFindLivingCombatEntity(
    StageSnapshot stage,
    EntityId id,
    out EntitySnapshot entity);
~~~

구현 흐름:

~~~text
Stage Entities에서 id 일치 항목 탐색
→ Area == Combat 확인
→ IsDead == false 확인
→ CombatAnchor 존재 확인
→ Kind와 무관하게 entity 반환
→ 없으면 null/false
~~~

Actor 조회에는 이 helper를 사용하지 않는다. 행동자는 계속 Unit이어야 하므로 기존 Unit 전용 조회/검증을 유지한다. 이 helper는 추적 대상이 Unit 또는 HQ가 될 수 있도록 이동 목적지 계산 쪽에서만 사용한다.

### Step 1-4 — TrySelectAttackTarget에 대상 클래스 선택 연결

추가 위치:

기존 **TrySelectAttackTarget(...)** 본문의 actor 검증 직후, 후보 순회 전.

구현 흐름:

~~~text
actor를 기존 Unit 전용 helper로 조회
→ targetUnits = HasLivingHostileCombatUnit(stage, actor.Owner) 한 번 계산
→ 기존 Entity 후보 순회
→ 적대/생존/Combat 조건 확인
→ IsAllowedTargetKind(candidate, targetUnits) 확인
→ 기존 공격 가능 Cell/거리 조건 확인
→ 기존 Manhattan 거리 및 EntityId tie-break로 최종 선택
~~~

힌트 코드:

~~~csharp
bool targetUnits =
    HasLivingHostileCombatUnit(stage, actor.Owner);

foreach (EntitySnapshot candidate in stage.Entities.Span)
{
    // 기존 hostile/living/combat 검증
    if (!IsAllowedTargetKind(candidate, targetUnits))
        continue;

    // 기존 사거리, 거리, EntityId tie-break 유지
}
~~~

**targetUnits**는 후보마다 다시 계산하지 않는다. 후보 집합을 순회하는 동안 어떤 후보를 보느냐에 따라 모드가 바뀌면 Unit-only/HQ-only 보장이 깨진다.

중요한 사례:

- Player Unit이 전장에 있지만 사거리 밖이고 HQ가 사거리 안이어도 공격 대상을 찾지 못한 것으로 반환한다.
- Player Unit이 하나도 전장에 없을 때만 HQ가 사거리 안이면 선택한다.

### Step 1-5 — TrySelectPursuitTarget에 같은 정책 연결

추가 위치:

기존 **TrySelectPursuitTarget(...)** 본문의 actor 검증 뒤.

구현 흐름:

~~~text
actor Unit 조회
→ hostile Combat Unit 존재 여부 한 번 계산
→ Unit-only 또는 HQ-only 후보 필터 적용
→ 기존 Manhattan 거리 비교
→ 기존 EntityId tie-break 적용
→ pursuitTargetId 반환
~~~

공격과 추적이 서로 다른 대상 클래스 규칙을 쓰지 않게 동일한 두 helper를 호출한다. 공격에서 Unit을 못 때렸다고 곧바로 HQ를 추적하는 두 번째 fallback을 추가하지 않는다. Combat Unit이 존재하는 동안 추적도 Unit만 대상으로 한다.

### Step 1-6 — TrySelectMovementDestination의 target 조회 확장

추가 위치:

기존 **TrySelectMovementDestination(...)**에서 pursuitTargetId로 대상을 찾는 부분.

변경 방향:

~~~text
actor 조회 → 기존 Unit 전용 helper 유지
target 조회 → TryFindLivingCombatEntity 사용
목적지 후보 계산 → 기존 로직 유지
목적지 거리/Cell tie-break → 기존 로직 유지
~~~

기존 target lookup이 Unit만 허용하면 HQ pursuitTargetId를 넘긴 순간 실패한다. 조회 범위만 일반 living Combat Entity로 넓히고, 이동 가능한 Cell 계산이나 점유 규칙은 바꾸지 않는다.

## Step 2 — EnemyAiTurnService.cs

### Step 2-1 — 시작 Snapshot이 terminal이면 즉시 거부

파일:

**Assets/Scripts/ProjectL/Core/Application/Combat/EnemyAi/EnemyAiTurnService.cs**

추가 위치:

Task 6에서 설명한 **Run(...)**의 null 검사 직후.

~~~csharp
if (initialStage.Outcome.IsStageEnded)
{
    return EnemyAiTurnResult.Rejected(
        EnemyAiTurnFailureReason.StageEnded,
        null,
        initialStage);
}
~~~

Task 8 구현 시 이 가드를 새로 중복 추가하지 말고 Task 6에서 넣은 흐름을 유지한다.

### Step 2-2 — 이동 뒤 terminal 확인

추가 위치:

**movement = combat.TryMove(...)** 성공 결과를 받은 직후, 후속 공격 범위를 계산하기 전.

구현 흐름:

~~~text
TryMove 결과 수신
→ 실패면 기존 종료 흐름
→ 성공 Snapshot의 Outcome 확인
→ terminal이면 CompletedByStageEnd 반환
→ 진행 중이면 post-move 공격 탐색 계속
~~~

현재 이동 자체가 Stage를 끝내는 규칙이 없더라도, 모든 변경 Command 뒤 terminal Snapshot을 존중하는 구조를 유지하면 이후 이동 트리거나 Trait이 추가돼도 EndTurn이 잘못 실행되지 않는다.

### Step 2-3 — 공격 뒤 terminal 확인

추가 위치:

초기 공격 분기와 이동 후 공격 분기에서 **TryAttack(...)** 직후.

힌트 흐름:

~~~csharp
attack = combat.TryAttack(activeId, targetId);

if (attack.Value.CurrentStage.Outcome.IsStageEnded)
{
    return EnemyAiTurnResult.CompletedByStageEnd(
        activeId,
        movement,
        attack,
        attack.Value.CurrentStage);
}

return CompleteByEndingTurn(
    combat, activeId, movement, attack);
~~~

HQ 파괴로 Defeat가 된 경우 이 분기로 끝난다. **EndUnitTurn()**을 호출하지 않으므로 공격자 Delay가 CurrentNightTime에 추가되지 않고, 우연히 낮 전환이 일어나지도 않는다.

## 전체 흐름

~~~text
EnemyAiTurnService.Run
→ terminal 사전 검사
→ EnemyAiPolicy actor Unit 확인
→ HasLivingHostileCombatUnit 한 번 계산
   ├─ true  → 공격/추적 후보를 Unit으로만 제한
   └─ false → 공격/추적 후보를 Headquarters로만 제한
→ 기존 거리와 EntityId tie-break로 대상 선택
→ HQ 추적 시 일반 living Combat Entity 조회
→ 기존 목적지 tie-break로 이동
→ 공격
→ CurrentStage.Outcome 확인
   ├─ 진행 중 → 기존 EndUnitTurn
   └─ 종결    → CompletedByStageEnd, EndTurn 생략
~~~

## 완료 체크리스트

- [ ] HasLivingHostileCombatUnit은 살아 있는 적대 Combat Unit만 센다.
- [ ] Reserve Player Unit은 Unit 우선 모드를 켜지 않는다.
- [ ] Combat Player Unit이 하나라도 있으면 HQ가 더 가까워도 후보에서 제외된다.
- [ ] Combat Player Unit이 사거리 밖이고 HQ만 사거리 안이면 공격 대상을 찾지 않는다.
- [ ] Combat Player Unit이 없을 때 공격과 추적 모두 Headquarters만 본다.
- [ ] 일반 Structure는 HQ fallback 후보가 아니다.
- [ ] actor 조회는 계속 Unit 전용이다.
- [ ] movement target 조회만 일반 living Combat Entity를 허용한다.
- [ ] Manhattan 거리와 EntityId 대상 tie-break가 유지된다.
- [ ] 이동 목적지의 기존 tie-break가 유지된다.
- [ ] 공격과 추적에서 hostile Unit 존재 여부를 각각 한 번만 계산한다.
- [ ] Enemy가 실제로 HQ 방향으로 이동하고 사거리 안에서 HQ를 공격할 수 있다.
- [ ] Player HQ 파괴 Snapshot이 즉시 Defeat를 담는다.
- [ ] HQ를 파괴한 AI Result는 성공이며 EndTurn이 null이다.
- [ ] 종결 공격 뒤 밤 시간이 증가하거나 Deployment로 전환되지 않는다.
- [ ] 이미 끝난 Stage의 AI 실행은 StageEnded로 무변경 거부된다.

---
