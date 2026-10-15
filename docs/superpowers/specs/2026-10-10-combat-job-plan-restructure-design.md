# 전투 진행 Job·Plan 재구성 설계 (2026-10-10)

- **목표:** 전투 진행의 "다음에 무엇을 할지" 판단을 `CombatPlan` 한 곳으로 모으고, Core 호출을 모두 전투 Job으로 옮긴다. `CombatLifeCycle`은 Job을 거는 **트리거**만 하며 Buffer·Core·View를 모른다. 제작자(민형님)의 원래 계획에 맞춘다.
- **상태:** 설계 확정(2026-10-10, D1~D6 추천안 승인). 코드 변경 없음. Task·실행 기록은 [Task 계획](../plans/2026-10-10-combat-job-plan-restructure-plan.md)에만 적는다.
- **사용자 결정(2026-10-10):**
  - U1. `CombatLifeCycle`은 Job을 실행시키는 트리거 역할만 한다. Buffer 등을 알 필요가 없다(제작자 계획).
  - U2. 전투 진행(진입·처리·이탈)을 Job·Plan으로 푼다. 판단 주인은 `CombatPlan` 하나다.
  - U3. Plan·Job은 Buffer를 직접 읽지 않는다. Core·View 호출은 `IInGameSystemGroupService`로 한다.
  - U4. 씬 전환(`LifeCycleManager.Change`)은 Job 안에서 한다.
  - U5. **B 방식:** `CombatLifeCycle.OnTick`이 "Runner가 비었으면 `Register(Combat, Next)`"만 거는 것을 트리거로 인정한다. Runner를 붙잡는 대기 Job은 두지 않는다.
  - U6. 새 `JobType` 값 추가와 `CombatPlan`에 LifeCycle 그룹을 넘기는 Plan 서명 변경을 허용한다.
  - U7. **사실 묶음 + 순수 Plan(C 방식):** 판단에 필요한 사실은 읽기 객체 하나(`CombatProgressReader`)가 Buffer·View에서 한 번에 읽어 값 묶음(`CombatProgressFacts`)으로 만든다. Plan은 그 묶음만 보고 판단한다(J13). View의 `CombatInputFacts`·`CombatInputPolicy`와 같은 모양이다.
  - U8. **Core 조회 요청은 하지 않는다.** 처음에는 Core 서비스에 진행 조회를 요청하려 했으나, Buffer 값을 그대로 돌려주는 중계 API가 돼 같은 사실을 읽는 길이 둘이 된다. 읽기 객체는 사용하는 쪽(우리 영역)에 둔다.
- **바꾸는 기존 문서:** [골격 설계](2026-09-30-combat-flow-skeleton-design.md)의 §3(구조), §4.1(판단표 원본), §6(LifeCycle 훅), §6.3 6A.0(진입 수명의 소유자), §6.4(이탈 소유자·우회 차단), §6.5 K22의 A1·A2. 이 설계가 확정되면 판단표 원본은 이 문서 J2로 옮긴다. 나머지 계약의 **의미**(K1·K4·K8·K14·K20·K21·K22)는 유지한다(§6).

## 1. 지금 구조

| 문제 | 지금 코드 |
|---|---|
| 판단 주인이 넷 | ① `CombatLifeCycle` 모듈 Tick: 진입 결과 확인 → `Change(Combat)` ② `CombatFlow.Tick`: 명령·미리보기·자동 진행 등록 ③ `CombatLifeCycle.RequestStageReturnAfterCompletion`: 승패 감지 → 이탈 요청 ④ `CombatExitSequence.Tick`: 이탈 단계 진행 → NewRun 등록·`Change` |
| Core 호출 경로가 둘 | Job: `EnterBattle`·`ProcessNextCommand`·`ProcessLatestPreview`·`AdvanceCombat`·`StartNewRun`. **직접 호출**: `EndBattle`(`CombatExitSequence.cs:210`)·`AbandonBattle`(`:250`)·`CompleteChapter`(`:289`) |
| Job 등록 경로가 셋 | `RequestEntry` → `RegisterCombatEntry`, Flow → `Register(Combat, Process…)`, ExitSequence → `Register(Combat, NewRun)` |
| `CombatPlan`은 이름만 Plan | `JobType` → Job 하나의 switch. 진짜 판단은 위 넷에 있다 |
| LifeCycle이 아는 것이 많다 | Buffer 싱크 3개 보관(`CombatViewWrite`·`CombatStateRead`·`CurrentProgressRead`) + 5개 전달, Core(`Combat`·`ChapterProgression`), View(`ViewGroup.Combat`), Runner·Register·Loading·Manager. Flow·ExitSequence를 `OnBindServices`에서 직접 조립(인자 12개) |
| Framework 파일 변경 | `ILifeCycleTransitionGuard`를 민형님 `LifeCycleManager.cs`에 추가(`5078ee0e`) |

## 2. 목표 구조

```text
CombatLifeCycle (트리거)                       아는 것: Runner·Register·CombatEntry·실행 모드·오류 경계
  OnEnter : CombatEntry.ActivateAccepted()     ← 동기(J6)
  OnTick  : 오류·기록 모드가 아니고 Runner가 비었으면 Register(Combat, Next)
  OnExit  : Register(Combat, ExitCheck)
        │
JobRegisterSystem ── CombatPlan.Register(service, jobType, lifeCycles)
        │
CombatPlan
  Register : facts = Reader.Read() → step = Decide(facts) → step에 맞는 Job 목록
  Decide   : 사실 묶음 → 다음 단계 (순수 함수, 판단의 유일한 원본, J2)
        │                                   ▲
        │                         CombatProgressReader (Buffer·View를 읽는 유일한 자리, J13)
        ▼                           Core Buffer · View Buffer · View 상태 → CombatProgressFacts
전투 Job (Core·View 호출 한 번 → Reader로 다시 읽어 결과 확인)
  어긋나면 예외 → GameBootstrap의 RuntimeFaultBoundary(F1)가 멈춘다
```

| 타입 | 소유하는 것 | 소유하지 않는 것 |
|---|---|---|
| `CombatLifeCycle` | 씬 Enter/Tick/Exit에서 무엇을 걸지 | 판단, Buffer·Core·View |
| `CombatEntryService`(신규, J5) | 진입 요청 하나의 수명, `IsActive` | 진입 외의 진행 |
| `CombatProgressReader`(신규, J13) | 사실을 어디서 어떻게 읽는지 | 판단, 쓰기, 저장 |
| `CombatPlan` | 다음 단계 판단(J2), 단계 → Job 구성, 이탈 목적지·새 Run 로드아웃 상수 | 상태 보관, Buffer 타입, 규칙 계산 |
| 전투 Job | 호출 한 번과 그 결과 확인 | 다음 Job 선택 |
| View(`CombatViewSystem`) | 입력 Buffer 기록, 연출, 제출 정지, 완료 표시 상태, 포기 요청 상태 | 진행 판단 |
| Core | 규칙·계산·Buffer 게시 | 진행 순서 |

## 3. 구조 결정

### J1. LifeCycle은 트리거

```csharp
internal sealed class CombatLifeCycle : LifeCycleBase
{
    // 생성자: (CombatExecutionMode, RuntimeFaultBoundary) 그대로
    protected override void OnBindServices(ISystemServiceStoreReader resolver)
    {
        // Global Job 서비스(Runner·Register)와 LifeCycle 그룹의 CombatEntry만 꺼낸다
    }

    protected override void OnEnter() => _entry.ActivateAccepted();

    protected override void OnTick(float deltaTime)
    {
        if (FaultBoundary.IsFaulted || ExecutionMode == CombatExecutionMode.RecordInputOnly)
            return;                                         // 골격 §4.1의 0행·4a행
        if (_runner.IsIdle)
            _register.Register(JobRootType.Combat, JobType.Next);
    }

    protected override void OnExit() => _register.Register(JobRootType.Combat, JobType.ExitCheck);
}
```

- `ISystemTick`(모듈 Tick)·`ICombatEntryService`·`ICombatExitService`·`ILifeCycleTransitionGuard` 구현과 서비스 Export를 없앤다. Flow·ExitSequence 조립도 없어진다.
- Tick당 등록은 많아야 한 번이다. Runner는 다음 Tick에 실행한다(Tick 순서 Buffer → Global → InGame → LifeCycle, 골격 §2). 지금 Flow와 같은 속도다.

### J2. `CombatPlan.Decide` 판단표 (위에서부터 처음 맞는 줄 하나)

`Decide(in CombatProgressFacts facts)`는 사실 묶음만 받아 다음 단계(종류 + 필요한 번호)를 돌려주는 순수 함수다. `Register`가 그 단계를 Job 목록으로 바꾼다.

기호: S = Core 결과 순번, P = View 연출 확인 번호, H = Core 처리 명령 번호. "재생 중" = P < S ∨ View 재생·후처리 중.

| # | 조건 | 단계 → Job |
|---|---|---|
| 0 | 전투 없음(`!HasBattle`) | 없음 |
| 1 | Completion 있음 ∧ 제출 열림 | [제출 정지] |
| 2 | Completion 있음 ∧ 재생 중 | 없음 |
| 3 | Completion 있음 ∧ View 미부착 | 없음(부착 대기) |
| 4 | Completion 있음 ∧ 완료 표시 전 | [완료 표시 시작](J7) |
| 5 | Completion 있음 ∧ 완료 표시 중 | 없음 |
| 6 | Completion 있음 ∧ 완료 확인됨 | [정리 기준 기록] → [EndBattle] → [Run 종료 판정] → [새 Run(조건부)] → [전환](J7) |
| 7 | 포기 요청 ∧ 제출 열림 | [제출 정지] |
| 8 | 포기 요청 ∧ 재생 중 | 없음 |
| 9 | 포기 요청 | [포기](J8) |
| 10 | `PlayerInput` ∧ P ≥ S ∧ H 다음 명령 N 있음 | [명령(N)] |
| 11 | `PlayerInput` ∧ 미리보기 요청 번호 > 응답 번호 | [미리보기(번호)] |
| 12 | P < S | 없음 |
| 13 | `Automatic` | [자동 진행(S)] |
| 14 | 그 밖(`PlayerInput` 대기, `Finished`, `None`) | 없음 |

- 10~14는 지금 골격 §4.1의 5~9행과 같다(K5 명령 우선, 미리보기는 연출 중에도, K1 연출 대기 조건).
- Completion이 포기보다 먼저다(1~6 > 7~9). 포기 요청 뒤 실제 승패가 기록되면 그 승패로 이탈한다(K22 A4).
- 포기 요청이 처리보다 먼저다(7~9 > 10~13). 포기를 받은 뒤에는 아직 등록하지 않은 명령·자동 진행을 등록하지 않는다. 지금 `Flow.Stop()`과 같은 결과다(K22 A5).
- 6행은 기다림이 없어서 한 번에 등록한다. Runner가 같은 Tick에 이어서 실행하고 마지막 전환으로 Combat이 비활성이 되므로 트리거가 저절로 멈춘다. `EndBattle` 뒤에는 0행이 먼저 맞으므로 같은 연쇄가 두 번 생기지 않는다.
- 할 일이 없을 때는 **공유 빈 목록**을 돌려준다(매 Tick 할당하지 않는다). `JobRegisterSystem`은 목록을 바꾸지 않는다.

### J3. 확인은 Job 안에서 (K4 위치 이동)

Job은 Core 처리 호출이 **한 번의 동기 호출**로 끝난다는 계약(골격 §2)을 이용해, 호출 직후 같은 `Execute` 안에서 `Reader.Read()`로 다시 읽어 결과를 확인한다. 지금 Flow의 "기다리는 확인"(`_pending`·`_expectedNumber`)은 없어진다. 처리 호출의 bool 반환은 판단하지 않는다(K14).

| Job | 호출 | 같은 Execute 안의 확인(어긋나면 예외) |
|---|---|---|
| 명령(N) | `ProcessNextCommand()` | H ≥ N |
| 미리보기(번호) | `ProcessLatestPreview()` | 응답 번호 ≥ 번호, **또는** 현재 요청이 더는 그 번호가 아님(View가 비우거나 바꿈) |
| 자동 진행(S) | `AdvanceCombat()` | 골격 §4.3.1 순서: S′ > S → 결과 있음 → 결과 순번 = S′ → `Kind == Advance` → `Accepted` (K8) |
| 포기 | `AbandonBattle()` | J8 |
| 진입 | `EnterBattle()`(기존 `CombatPrepareJob`) | 호출 전 `!HasBattle`, 호출 뒤 기존 `CombatEntryRequest.PublishResult` 대응 확인 |

- 등록(Tick N의 LifeCycle)과 실행(Tick N+1의 Runner) 사이에는 Runtime 모듈이 돌지 않는다. 다만 Unity 입력 콜백은 Tick 밖에서 올 수 있으므로 미리보기의 "요청이 바뀜" 예외를 지금처럼 유지한다.

### J4. 연출 대기는 Plan의 조건 (K1 유지)

"연출이 끝날 때까지 기다리는 Job"을 만들지 않는다. 기다림은 J2의 "없음" 줄이 맡는다. Runner를 여러 Tick 붙잡는 Job이 없으므로 미리보기가 막히지 않고 `IsIdle`의 의미(StageMap 진입 버튼 등)도 그대로다.

### J5. 진입 — `CombatEntryService` 분리 (D3-a 확정)

- 새 클래스 `LifeCycles/Combat/CombatEntryService`가 `ICombatEntryService`를 구현하고, 지금 `CombatLifeCycle`의 진입 부분(`RequestEntry`·`CancelEntry`·`IsActive`·현재 요청 보관)을 가져간다. `LifeCycleSystemFactory`가 만들어 같은 키(`LifeCycleSystemServiceKey.CombatEntry`)로 Export한다. **StageMap(`StageMapEntryFlow`)·`GameBootstrap`의 호출 서명은 바뀌지 않는다.**
- 의존: Runner(`IsIdle`)·Register·Loading·오류 경계·`CombatProgressReader`(활성화 확인용, J6). Buffer를 직접 받지 않는다.
- `RequestEntry`: 오류 없음, 진행 중 요청 없음(대기 중 같은 대상은 같은 요청 반환, 거절·취소된 요청은 교체), Runner Idle, 로딩 아님 → `RegisterCombatEntry(request)`.
- `CombatPlan.RegisterEntry` → **[진입(EnterBattle)] → [진입 전환]**.
  - 진입 Job은 호출 전 `!HasBattle`을 확인한다(옛 K11 3번).
  - 진입 전환 Job: 요청이 `Prepared` ∧ Job 완료면 `BeginActivation()` → `Change(Combat)`. `Rejected`·`Cancelled`면 아무것도 하지 않는다(요청자가 요청 객체에서 결과를 읽는다).
  - 지금 LifeCycle 모듈 Tick의 진입 결과 감시가 없어진다.
- 옛 K11의 1·4번(Flow 정지·Flow 확인 대기)은 Flow가 없어지므로 사라진다.

### J6. 활성화는 `OnEnter`에서 동기로 (K20 유지)

Combat 씬 Enter 노드에서 LifeCycle `OnEnter`(우선순위 0)가 View `OnCombatSceneEntered`(100)보다 먼저 돌고, View는 그때 `IsActive`가 아니면 예외를 던진다(`CombatViewSystem.cs:540`, View 개선 설계 D13). 활성화를 Job으로 옮기면 Runner가 그 뒤에 돌아 View Attach가 실패한다. 그래서 활성화는 Job이 아니라 **`OnEnter` → `CombatEntry.ActivateAccepted()`** 동기 호출로 둔다.

- `ActivateAccepted()`: 현재 요청이 `Activating` ∧ Job 완료 ∧ 결과 수락 ∧ 현재 선택(챕터·노드·스테이지)이 요청 대상과 같을 때 `Activate()`. 아니면 예외. 현재 선택은 `CombatProgressReader`로 읽는다(지금 `OnEnter`의 확인과 같다).
- LifeCycle이 아는 것은 같은 LifeCycle 층의 `ICombatEntryService` 하나다(U1 위반 아님).
- 입력 Buffer 비우기는 J12 D4를 따른다.

### J7. 이탈 연쇄 (K21 유지)

| Job | 하는 일 |
|---|---|
| 제출 정지 | `view.SuspendSubmissions()`(멱등) |
| 완료 표시 시작 | Completion 대상 = 현재 진입(chapter·node·stage) 확인. 일반 승패면 최신 결과가 수락 ∧ 결과 순번 = S ∧ Outcome = Completion.Outcome인지 확인(포기 Completion은 결과를 게시하지 않으므로 건너뜀). 그다음 `view.PresentCompletion(completion)` |
| 정리 기준 기록 | 공유 컨텍스트에 최신 결과 참조·S·Completion 참조·전투 인스턴스를 적는다 |
| EndBattle | `EndBattle()` → `!HasBattle`, 활성 캐릭터 0명, 결과 참조·S·Completion 보존, P ≥ S 확인(지금 `CombatExitSequence`의 확인과 같다) |
| Run 종료 판정 | K21 규칙 그대로: 보스 승리면 `CompleteChapter` 한 번(`RunCompleted` → 종료, `NextChapterSelected` → 계속, `DefinitionUnavailable` → 임시로 종료, 그 밖의 거절 → 예외), 아니면 `ChapterProgression.Outcome != InProgress` |
| 새 Run(조건부) | Run 종료일 때만 `RunStart.StartNewRun(NewRunLoadoutKey)` → `Outcome == InProgress` ∧ `!HasBattle` 확인. 아니면 호출 없이 완료 |
| 전환 | `LifeCycleManager.Change(목적지)` |

- **공유 컨텍스트(`CombatExitContext`):** 6행 연쇄를 만들 때 Plan이 하나 만들어 그 Job들에 넘긴다. 연쇄 하나 동안만 산다. 진입 Job이 `CombatEntryRequest`를 공유하는 것과 같은 방식이다. Plan 자신은 상태를 갖지 않는다.
- **완료 표시 상태는 View가 갖는다:** `None`/`Showing`/`Confirmed`. 새 Attach에서 초기화한다. 확인 콜백 뒤 표시 코드가 예외를 던져도 `EndBattle`은 다음 판단(다음 Tick 이후)에서야 등록되므로 지금 규칙("동기 완료 콜백 뒤 같은 호출 스택에서 EndBattle을 하지 않는다")이 지켜진다.
- **목적지·로드아웃 상수는 `CombatPlan` 한 곳:** 일반 이탈·Run 종료 모두 지금은 StageSelect(`LifeCycleKeys.Stage`), Title이 생기면 Run 종료 목적지만 바꾼다. 새 Run 로드아웃 1(임시). 평가 문서 M6의 흩어진 상수 3개 중 2개가 한 곳으로 모인다.
- 평가 문서 R2("Run 종료 판정이 커질 자리")는 Run 종료 판정 Job이 그 작은 판정 객체 역할을 한다.

### J8. 포기 (K22: A1·A2 변경, A3~A5 유지)

- **A1 변경:** 공개 API `ICombatExitService.RequestAbandon()`·`ICombatAbandonRequest`·`CombatExitStatus`를 없앤다. View 확인창의 확인은 View 자신의 "포기 요청" 상태만 세운다. 판단은 J2의 7~9행이 한다.
- **A2 변경:** `AbandonBattle()`은 포기 Job이 부른다(U2, 모든 Core 호출은 Job).
- 포기 Job: 호출 전 최신 결과 참조·S를 기억 → `AbandonBattle()` →
  - `Abandoned`: Completion이 `Defeat`·`VoluntaryAbandonment` ∧ S·최신 결과 그대로인지 확인. 다음 판단부터 1~6행으로 이탈한다.
  - `AlreadyCompleted`: 확인 없이 완료. 다음 판단이 실제 승패로 1~6행을 탄다(A4).
  - 그 밖: 예외.
- **A3 유지:** 기록 모드에서는 LifeCycle이 `Next`를 걸지 않으므로 포기가 처리되지 않는다. View 버튼은 지금처럼 Live에서만 켠다(`CombatInputPolicy.CanRequestAbandon`).
- 포기 버튼 가능 조건의 `exitIdle` 인자는 View 상태(`!포기 요청 ∧ !제출 정지`)로 바꾼다. 이탈 시작은 지금도 `SuspendSubmissions`로 버튼을 끈다.

### J9. 잘못된 전환 (D1-a 확정)

`ILifeCycleTransitionGuard`는 전투 중 다른 코드가 `Change`를 직접 부르는 것을 **미리 막는다.** B 방식에서 이것을 유지하려면 "지금 허용된 전환"을 LifeCycle이 알아야 한다.

- **D1-a(추천): 없애고 사후 확인.** `LifeCycleManager.cs`의 guard 호출과 인터페이스를 되돌린다(Framework 원래 모양). `OnExit` → `Register(Combat, ExitCheck)` → [ExitCheck]: `HasBattle`이면 예외(F1), 아니면 진입 요청을 닫는다(`Close`). 잘못된 전환을 막지는 못하지만 전환 직후 바로 멈춘다.
- **D1-b: 유지.** 허가 표식을 `CombatEntryService`가 갖고, 전환 Job이 `Change` 직전에 세우고 직후 내린다. `CombatLifeCycle`의 guard는 진입 서비스에 묻기만 한다. 예방은 유지되지만 Framework 파일 변경이 남는다.

### J10. 오류는 예외 하나로 (D2-a 확정)

- Job의 확인이 어긋나면 `InvalidOperationException`을 던진다. Runner는 잡지 않고 `GameBootstrap`이 `RuntimeFaultBoundary.Report` 후 다음 Tick·전환을 멈춘다(골격 §4.5, F1). Job마다 try/catch·`Runner.Clear()`를 두지 않는다.
- **동작 변화:** 지금 Flow는 확인 실패 때 로그만 남기고 진행만 멈춘다(런타임은 살아 있어 포기로 빠져나갈 수 있다). 앞으로는 F1로 런타임 전체가 멈춘다. 확인 실패는 Core 계약 위반(버그)이라 같은 경계로 모으는 쪽을 추천한다.
- Core가 명령을 거절한 것(`Accepted == false`)은 오류가 아니다(R1·R2). 자동 진행 거절은 지금처럼 오류다(K8).
- 취소는 Runner에서 빼지 않는다. Job이 실행 시점에 조건을 보고 할 일이 없으면 호출 없이 완료한다(`CombatEntryRequest.TryStart`, Artifact D8a와 같은 방식). B 방식은 연쇄가 짧고 포기도 남은 처리 뒤에 시작하므로 취소가 필요한 곳은 진입 취소뿐이다.

### J11. 기록 모드·같은 Tick 오류

- 오류 0행·기록 모드 4a행은 J1의 트리거 조건이 된다. 기록 모드는 진입·활성화까지 실제로 하고 그 뒤 처리 Job을 걸지 않는다(지금과 같다). 자동 이탈·포기도 생기지 않는다(지금도 Live 전용).

### J12. View 계약 `ICombatProgressView`

`ICombatExitPresentation`을 대체한다. **Buffer에 없는 View 상태와 View 동작만** 담는다. 명령 번호·미리보기 번호·P는 `CombatViewBuffer`에 있으므로 여기에 두지 않고 읽기 객체가 Buffer에서 읽는다(같은 사실을 읽는 길을 하나로). 사용하는 쪽(`LifeCycles/Combat`)에 정의하고 `ICombatViewService`가 상속한다(지금 `ICombatExitPresentation`과 같은 방향, ISP·DIP).

```csharp
public interface ICombatProgressView
{
    bool IsAttached { get; }
    bool IsPlaybackBusy { get; }                       // 재생·후처리 중(P < S는 Buffer로 따로 본다)
    bool IsSubmissionSuspended { get; }
    void SuspendSubmissions();
    CombatCompletionDisplay CompletionDisplay { get; } // None / Showing / Confirmed
    void PresentCompletion(BattleCompletionChanges completion);
    bool IsAbandonRequested { get; }
}
```

- `PresentCompletion`의 콜백 인자는 없어지고 상태 조회(`CompletionDisplay`)로 바뀐다. 표시 수명·세대 확인은 View 안에 남는다.
- `ViewGroup`이나 `ViewGroup.Combat`이 없으면 읽기 객체가 조립 오류로 예외를 던진다. 테스트는 가짜 View를 넣는다.
- **입력 Buffer 비우기(D4-a 확정):**
  - D4-a(추천): View가 자기 Buffer를 Attach(진입)·Detach(이탈)에서 비운다. 쓰는 주인이 비운다. Attach는 활성화(J6) 뒤, Detach는 `EndBattle` 뒤(전투가 사라지면 Detach, View 개선 설계 R3)라 K14("전체 비우기는 진입·이탈에서만, 처리 전 입력 보존")를 지킨다.
  - D4-b: 활성화·ExitCheck 때 View 서비스의 `ClearInput()`을 부른다.

### J13. 사실 묶음과 읽기 객체 (U7·U8)

```csharp
/// <summary>다음 단계 판단에 쓰는 사실을 한 번에 읽은 값입니다. 저장하지 않습니다.</summary>
public readonly struct CombatProgressFacts   // 생성자는 internal
{
    // Core Buffer: HasBattle, BattleInstanceId, Phase, HandledCommandNumber(H),
    //              AnsweredPreviewNumber, LatestResultSequence(S), Completion, 최신 CombatResult
    // View Buffer: 다음 명령 번호(H 기준), 미리보기 요청 번호(없으면 없음), PresentedResultSequence(P)
    // View 상태 : IsAttached, IsPlaybackBusy, IsSubmissionSuspended, CompletionDisplay, IsAbandonRequested
}

/// <summary>전투 진행 사실을 읽는 유일한 자리입니다.</summary>
public interface ICombatProgressReader : IServiceModule
{
    CombatProgressFacts Read();
    int ActiveCharacterCount { get; }                                       // EndBattle 뒤 확인(J7)
    bool TryReadSelection(out int chapterKey, out StageSelectionData stage); // 활성화 확인(J6)
}

/// <summary>읽기 싱크와 View 계약만 받고 쓰지 않습니다.</summary>
internal sealed class CombatProgressReader : ICombatProgressReader, ISystemPostBindService { … }
```

- **인터페이스를 두는 이유(R1에서 정정):** 처음에는 구현이 하나라 인터페이스 없이 두려 했으나, Framework가 서비스를 인터페이스 타입으로만 등록받는다(FW-001, `SystemBuilderHelper`가 클래스 타입 등록을 거부). LifeCycle 그룹으로 공개하려면 인터페이스가 필요하다. 덕분에 Job 단위 테스트에서 가짜 읽기 객체를 쓸 수 있다.

- 받는 것: `ICombatStateBufferReadSink`·`ICombatResultReadSink`·`ICombatViewBufferReadSink`·`ICharacterBufferReadSink`·`ICurrentProgressBufferReadSink`(필요한 싱크만, ISP)와 `ICombatProgressView`.
- `Read()`는 호출할 때마다 새로 읽는다. 사본·캐시를 두지 않는다. View `ReadFacts()`(`CombatViewSystem.cs:792`)와 같은 규칙이다.
- Plan의 판단(`Decide`)은 사실 묶음만 받는 순수 함수라 테스트에서 Buffer를 조립할 필요가 없다. Job은 호출 뒤 확인에만 `Read()`를 쓴다.
- Core 처리·결과 호출(`EnterBattle`·`Process…`·`AdvanceCombat`·`AbandonBattle`·`EndBattle`·`CompleteChapter`·`Outcome`·`StartNewRun`)은 지금처럼 `IInGameSystemGroupService.CoreGroup`으로 한다. Core에 새로 요청할 것은 없다.
- 읽기 객체를 어디서 만들어 Plan에 넘길지는 D5.

### J14. Plan에 LifeCycle 그룹 전달 (U6)

`JobRegisterSystem.PostBindService`에서 `SystemGroupsService.LifeCycle`을 받아 Combat Plan에 넘긴다. `StagePlan`이 `IBufferQuery`를 받는 것과 같은 방식이다.

```csharp
_plans[JobRootType.Combat] = (service, jobType) => CombatPlan.Register(service, jobType, _lifeCycles);
```

- Plan은 `_lifeCycles.LifeCycleManager`(전환 Job)·`_lifeCycles.CombatEntry`(현재 진입 요청)·읽기 객체(`_lifeCycles.CombatProgress`, `ICombatProgressReader`)를 쓴다. `ICombatEntryService`에 읽기 전용 `Current`를 더한다.
- `LifeCycleSystemGroup`은 생성자에서 서비스를 채우므로 Global의 `PostBindService` 시점에 이미 읽을 수 있다(`LifeCycleSystemGroup.cs:37-43`).
- `RegisterCombatEntry(request)`도 같은 `_lifeCycles`를 넘긴다(진입 전환 Job).

### J15. `JobType` 추가

공용 enum(`JobRootType.cs`)에 `Next = 6`, `ExitCheck = 7`을 더한다. 기존 `Start`·`NewRun`·`ProcessCommand`·`ProcessPreview`·`AdvanceCombat` 값은 공용 파일이라 지우지 않는다. Combat Plan의 switch에서 더 쓰지 않는 값의 정리 범위는 Task 계획에서 정한다(호환 `Start` 경로는 평가 문서 §5의 정리 후보).

## 4. 장면별 흐름

**명령**

```text
① View    : 입력 → CombatViewBuffer 명령 N                       (그대로)
② LifeCycle: Runner 빔 → Register(Combat, Next)
   Plan   : Reader.Read() → Decide: PlayerInput, P ≥ S, H 다음 명령 N → [명령(N)]
③ Job     : ProcessNextCommand() → Reader.Read()로 H ≥ N 확인       (확인이 Job 안으로)
④ Core    : 계산 → Buffer 게시(H, S)                               (그대로)
⑤ View    : 연출 → 처리된 명령 삭제, P = S                         (그대로)
⑥ 다음 Tick: Runner 빔 → Plan이 처음부터 다시 판단(P ≥ S는 Decide 조건)
```

**자동 진행:** 13행 → [자동 진행(S)] → Job 안에서 K8 확인 → 다음 Tick에 12행(연출 대기) → 연출 끝나면 다시 13행.

**승리·패배:** 처리 Job 뒤 Core가 Completion 기록 → 1행 제출 정지 → 2행 연출 대기 → 4행 완료 표시 → 5행 사용자 확인 대기 → 6행 연쇄 → 전환 → Combat 비활성 → `OnExit` → ExitCheck.

**포기:** View 확인창 → 포기 요청 상태 → 7행 제출 정지 → 8행 연출 대기 → 9행 포기 Job → (`Abandoned`) 다음 Tick 1~6행 / (`AlreadyCompleted`) 실제 승패로 1~6행.

**진입:** StageSelect → `RequestEntry` → [진입] → [진입 전환: `Change(Combat)`] → 로딩 → `OnEnter` 동기 활성화 → View Attach → 다음 Tick부터 트리거.

**잘못된 전환(D1-a):** 전투 중 다른 코드가 `Change(Stage)` → Combat 비활성 → `OnExit` → ExitCheck → `HasBattle` → 예외 → F1.

## 5. 파일 변경 예상

경로는 `Assets/ProjectL/Script/Systems/RuntimeSystem/` 기준이다.

| 파일 | 변경 |
|---|---|
| `LifeCycleSystem/LifeCycles/Combat/CombatLifeCycle.cs` | J1로 축소 |
| `…/Combat/CombatEntryService.cs`(신규) | J5·J6 |
| `…/Combat/CombatEntryRequest.cs` | `ICombatEntryService`에 `Current`·`ActivateAccepted` |
| `…/Combat/CombatProgressFacts.cs`·`CombatProgressReader.cs`(신규) | J13 |
| `…/Combat/ICombatProgressView.cs`(신규) | J12 |
| `…/Combat/CombatFlow.cs` | 삭제(판단은 J2, 확인은 J3) |
| `…/Combat/CombatExitSequence.cs` | 삭제(J7·J8). `ICombatExitService`·`ICombatAbandonRequest`·`ICombatExitPresentation`·`CombatExitStatus` 포함 |
| `…/Combat/Plans/CombatPlan.cs` | J2 `Decide`·단계 → Job 구성, `ExitCheck`, 진입 연쇄, 상수 |
| `…/Combat/Plans/CombatExitContext.cs`(신규) | J7 공유 컨텍스트 |
| `…/Combat/Jobs/` | 명령·미리보기·자동 진행 Job에 기대 번호와 확인 추가. 신규: 진입 전환, 제출 정지, 완료 표시 시작, 정리 기준 기록, EndBattle, Run 종료 판정, 새 Run(조건부), 전환, 포기, ExitCheck(묶을 수 있는 것은 Task 계획에서) |
| `LifeCycleSystem/LifeCycleSystemGroup.cs`, `ServiceKeys/…` | `CombatExit` 제거, D5-a면 `CombatProgress` 추가 |
| `Factory/Runtime/LifeCycle/LifeCycleSystemFactory.cs` | `CombatEntryService`(D5-a면 읽기 객체도) 생성·등록 |
| `GlobalSystem/JobSystem/JobRegister/JobRegisterSystem.cs` | J14 (**민형님 파일**) |
| `GlobalSystem/JobSystem/JobRegister/JobRootType.cs` | J15 (**공용**) |
| `LifeCycleSystem/Base/LifeCycleManager.cs` | D1-a면 guard 제거 (**민형님 파일**) |
| `InGameSystem/ViewSystem/Combat/CombatViewSystem.cs`, `ICombatViewService.cs`, `CombatAbandonController.cs`, `CombatInputPolicy.cs` | `ICombatProgressView` 구현, 완료 표시·포기 요청 상태, 입력 비우기(D4) |
| Core | 없음 |
| 테스트 `Tests/EditMode/Framework/` | `CombatFlowTests`(→ `Decide` 판단표 테스트로 재작성), `CombatFlowIntegrationTests`, `CombatEntryIntegrationTests`, `CombatPlanTests`, `CombatViewSystemTests`(이탈·포기 부분), `JobSystemTests`, `FrameworkRuntimeCompositionTests` |

다른 작업자 영향: StageMap(`StageMapEntryFlow`·`StageMapViewSystem`)과 `GameBootstrap`은 `ICombatEntryService` 서명이 유지돼 바뀌지 않는다.

## 6. 기존 결정과의 관계

| 결정 | 이 설계에서 |
|---|---|
| K1 연출 대기는 조건 | **유지.** Flow 조건 → Plan 조건(J4) |
| K2 Flow·Plan·Job은 View 소유 | 유지 |
| K4 되묻지 않기 | **유지, 위치 이동.** Flow의 다음 Tick 확인 → Job 안 즉시 확인(J3) |
| K5 명령 우선 | 유지(J2 10·11행) |
| K8 자동 진행 수락 확인 | 유지(J3) |
| K11 진입 등록 전 조건 | Runner Idle·전투 없음은 유지(J5), Flow 관련 두 조건은 소멸 |
| K14 입력 유지·정상 false 완료 | 유지. "판단은 Flow 확인 조건 한 곳" → "Job 안 확인 한 곳" |
| K16·F1 단일 오류 경계 | 유지, Flow 소프트 정지까지 흡수(J10, D2) |
| K19 판단표 원본 | 골격 §4.1 → 이 문서 J2(확정 시) |
| K20 명시적 진입·한 번의 활성화 | 유지. 진입 결과 감시는 Job, 활성화는 `OnEnter` 동기(J5·J6) |
| K21 Run 종료 → 새 Run → StageSelect | 유지(J7) |
| K22 포기 | A1·A2 변경, A3~A5 유지(J8) |
| 골격 §6.4 우회 차단 | D1 |
| 골격 §3 "진행의 주인은 CombatFlow" | "판단의 주인은 CombatPlan" |

## 7. SSOT·SOLID·FW 점검

**SSOT**

| 사실 | 원본 | 이 설계에서 |
|---|---|---|
| 다음 작업 판단 규칙 | `CombatPlan.Decide` | 네 곳 → 한 곳 |
| 국면·H·S·Completion·결과 | Core Buffer | 읽는 자리는 `CombatProgressReader` 하나. Core에 중계 조회를 만들지 않는다(U8) |
| 명령·미리보기·P | `CombatViewBuffer`(View) | 읽는 자리는 읽기 객체 하나. View 서비스로 다시 노출하지 않는다(J12). 비우기는 View(D4-a) |
| 완료 표시·포기 요청·제출 정지·재생 중 | View | 지금 `CombatExitSequence`의 `Status`·`_confirmed`·`_generation`이 사라진다 |
| 진입 요청 수명 | `CombatEntryService` | LifeCycle에서 분리 |
| 이탈 목적지·새 Run 로드아웃 | `CombatPlan` 상수 | 흩어진 상수 일부 통합 |
| 처리 중 여부 | `IJobRunner.IsIdle` | Flow의 확인 대기 상태가 사라진다 |

**SOLID**

| 원칙 | 점검 |
|---|---|
| S | LifeCycle = 무엇을 걸지, EntryService = 진입 수명, Reader = 읽기, Plan = 판단, Job = 호출+확인, View = 표시 |
| O | 새 진행 = 판단표 한 줄 + Job 하나(+ 사실이 더 필요하면 묶음 필드 하나). 기존 Job은 바뀌지 않는다 |
| L | Job은 `IsCompleted`를 "호출과 확인이 끝났다"는 뜻으로만 쓴다. `ICombatProgressView`의 `SuspendSubmissions`·`PresentCompletion`은 멱등·한 번 규칙을 지킨다 |
| I | 읽기 객체는 필요한 읽기 싱크만 받는다. View는 Buffer에 없는 상태만 `ICombatProgressView`로 노출한다 |
| D | Plan 판단은 값 묶음에만 의존한다. Job은 Core·View 인터페이스와 읽기 객체에 의존한다 |

**Framework 계약**

| 계약 | 점검 |
|---|---|
| FW-001·002 | `CombatEntryService`(D5-a면 읽기 객체도)는 서비스로 Export(생명주기가 필요하면 `AddModule`). `CombatLifeCycle`은 모듈로만 남는다 |
| FW-004 | Runner·Register·Manager는 공개 인터페이스로만 쓴다 |
| 지침 §11 | Plan 등록은 `JobRegisterSystem.Initialize` 그대로. 조립 뒤 구조 변경 없음 |
| 지침 §27·FW-001 | 읽기 객체는 구현이 하나지만 서비스는 인터페이스로만 등록하므로 `ICombatProgressReader`를 둔다(J13). `ICombatProgressView`는 층 경계(LifeCycle ↔ View)의 계약이며 기존 `ICombatExitPresentation`을 대체한다 |
| Job 완료 계약·D7 | Runner 동작·완료 계약을 바꾸지 않는다. `Runner.Clear()`를 쓰지 않는다 |
| 공용 파일 공유 | `JobRootType.cs`·`JobRegisterSystem.cs`·(D1-a면) `LifeCycleManager.cs` 변경은 민형님 공유 문서에 적는다 |

## 8. 결정 (2026-10-10, 모두 추천안으로 확정)

| ID | 질문 | 확정(추천) | 다른 안 |
|---|---|---|---|
| D1 | 잘못된 전환 차단(J9) | **a. guard 제거 + ExitCheck 사후 확인.** Framework 파일이 원래대로 돌아가고 LifeCycle은 트리거만 한다 | b. guard 유지, 허가 표식은 `CombatEntryService` |
| D2 | 확인 실패 처리(J10) | **a. 예외 → F1로 통일** | b. Plan에 정지 상태를 둔다(Plan이 상태를 갖게 됨) |
| D3 | 진입 수명 위치(J5) | **a. `CombatEntryService` 분리** | b. `CombatLifeCycle`에 남기고 본문만 축소 |
| D4 | 입력 Buffer 비우기(J12) | **a. View Attach·Detach** | b. 활성화·ExitCheck가 View 서비스 호출 |
| D5 | 읽기 객체 조립 위치(J13) | **a. `LifeCycleSystemFactory`가 만들어 LifeCycle 그룹(`CombatProgress`)으로 공개.** `PostBindService`에서 싱크·View를 받는다. 민형님 `JobRegisterSystem`은 `_lifeCycles` 한 줄만 늘어난다 | b. `JobRegisterSystem`이 `_buffers`로 직접 만들어 Plan에 넘김(`StagePlan` 선례). Combat 타입 생성이 민형님 파일에 들어간다 |
| D6 | 포기 요청·완료 확인의 위치 | **a. View 상태로 둔다(J12)** | b. 연출 확인 번호 P처럼 `CombatViewBuffer`에 기록한다. View 서비스는 동작 호출(제출 정지·완료 표시)에만 쓰게 된다 |

## 9. Phase 개요 (Task는 [Task 계획](../plans/2026-10-10-combat-job-plan-restructure-plan.md))

| Phase | 내용 | 동작 변화 |
|---|---|---|
| R1 계약 준비 | `ICombatProgressView`(View 구현), `CombatProgressFacts`·`CombatProgressReader`, `JobType`·`JobRegisterSystem` 전달, `CombatEntryService` 분리 | 없음 |
| R2 새 Plan·Job | J2 `Decide`·J3 확인·J7·J8 Job 작성과 단위 테스트. 아직 연결하지 않음 | 없음 |
| R3 전환 | LifeCycle 트리거, View 포기·완료 상태, 옛 Flow·ExitSequence·Exit 서비스 삭제, 통합 테스트 교체 | 있음(J10 등) |
| R4 정리 | D1 처리, 골격 설계·UML·평가 문서 갱신, Framework 공유 문서, CLAUDE.md | — |

Phase마다 멈추고 보고한다. R3 뒤 Unity EditMode와 재생 확인을 한다.

**겹치는 작업:** VR6(타임라인 소비자 교체)은 이 재구성 뒤로 둔다. View 계획 §7.68(SOLID·SSOT 개선 P2~P7)의 Unity 확인을 R3 전에 끝내야 회귀 원인이 섞이지 않는다.

## 10. 확인 방법

| 항목 | 확인 |
|---|---|
| 판단표 | `Decide` 단위 테스트: 사실 묶음만으로 J2 각 행, 행 우선순위(Completion > 포기 > 처리) |
| 읽기 객체 | 각 싱크·View 값이 묶음에 그대로 들어가는지, 사본을 두지 않는지 |
| Job 확인 | 명령 H 미갱신·미리보기 응답 누락(요청 바뀜은 통과)·자동 진행 순번/종류/거절·포기 결과 보존 위반 → 예외 |
| 진입(K20) | 정상 수락·거절·취소·중복 요청, 활성화 한 번, View Attach 때 `IsActive` |
| 승리 복귀(F2) | Completion → 표시 → 확인 → EndBattle → StageSelect, 결과·S 보존 |
| 패배·Run 종료(K21) | 새 Run 준비 → `InProgress` → StageSelect. 보스 승리 `CompleteChapter` 1회 |
| 포기(K22) | 배치 국면 포기, Runner 바쁜 중 포기(처리·표시 뒤 1회), 대기 중 승리 → `AlreadyCompleted`, 기록 모드에서 무시 |
| 입력 대기(VR1) | `CombatCommandGate`의 시작/턴 종료 대기와 이동 허용이 그대로인지 |
| 기록 모드 | 진입·표시·입력 기록만, 처리 Job 없음 |
| 오류 | Job 예외 → F1 1회 보고, 이후 등록·전환 없음. 같은 Tick 앞쪽 오류 뒤 트리거 없음 |
| 잘못된 전환(D1) | a: 전투 중 `Change` → ExitCheck 예외 / b: `Change` 거부 |
