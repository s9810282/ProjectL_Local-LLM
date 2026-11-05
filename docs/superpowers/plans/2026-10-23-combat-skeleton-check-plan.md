# 전투 골격 점검 Task 계획 (2026-10-23)

- 설계: [skeleton-check-design](../specs/2026-10-23-combat-skeleton-check-design.md)(K-1~K-5).
- **결정(2026-10-23 사용자 "결정은 추천안으로 진행하고, PHASE 끝날 때마다 하위 에이전트로 검토해"):** 권장안대로다.
  - Q-K1·Q-K2·Q-K4·Q-T1·Q-T2는 A
  - Q-K3·Q-K5·Q-K6은 B(Q-K5는 계획 대조 뒤 바뀐 권장, TP-12)
  - Q-K2 A는 10-22 사용자 결정(문제점 분석 §6 결정 4-③ "`RegisterCombatEntry`의 `MarkQueued`·`Fail`은 그대로")을 뒤집는 것이다(TP-14). 이 뒤집기도 권장안에 포함해 받았다.
  - 계획에서 정한 것 TP-1~TP-17은 쓴 그대로다. 그중 K-5 a에서 `CombatEndUI`는 뺀다(TP-16, 설계와 다름).
  - TP-18(CLAUDE.md 영역 규칙 글)은 권장이 "고치기 전에 묻기"이므로 K-f에서 묻는다.
- 기준 코드: 작업 트리. HEAD `4c328d07`에 커밋 전 C 단계가 더해진 상태다. 줄 번호는 지금 작업 트리 기준이다. 앞 Phase가 줄을 바꾸므로 Phase마다 다시 찾는다.
- **2026-10-23 하위 에이전트 2개로 검증했다**(코드 사실·호출처 / 설계 일치·동작·규칙). 반영한 것과 받지 않은 것은 맨 아래 "검증 기록"에 있다.
- **진행 규칙**
  - Phase 하나가 끝나면 멈추고 보고한다. 사용자가 Unity에서 컴파일·EditMode 시험·재생을 확인한 뒤 다음 Phase로 간다(CLAUDE.md).
  - **Phase 코드가 끝나면 보고 전에 새 에이전트(읽기 전용)로 그 Phase 변경을 검토하고, 지적을 반영한 뒤 보고한다**(2026-10-23 사용자 지시). 검토 결과와 반영 내용은 실행 기록에 적는다.
  - 전투 View 작업이라 **MSBuild를 돌리지 않고 새 `[Test]`를 만들지 않는다.** 보고에 "컴파일 미확인"을 적는다.
  - 시험 처리(Q-T1·Q-T2):
    - 걷은 기능이나 바뀐 동작을 보던 우리 시험(사례)은 지운다.
    - 남는 내용을 보는 우리 시험은 호출·입력만 맞춘다.
    - 깨지지 않는 시험은 손대지 않는다(CLAUDE.md "깨지는 곳만 최소로").
    - 고친 시험은 모두 실행 기록과 공유 메모에 적는다.
  - **Q-T2 허락 대상(호진님 시험, 이 셋만):**
    - `T/CombatEntryIntegrationTests.cs:962` 한 줄 삭제(K-b)
    - `T/MapCombatPresentationTests.cs` 가짜 `Registration` 몸통과 SetUp 한 줄(K-b)
    - `T/TutorialViewSystemTests.cs:34-35` 상수 읽기 두 줄(K-d). 허락하지 않으면 TP-9의 별칭으로 간다.
  - 연결 줄(GlobalSystem의 `JobRegisterSystem`·`JobType`)을 바꾸면 Phase K-f에서 민형님 공유 문서에 적는다. 그 밖의 Framework·GlobalSystem 기능·계약은 바꾸지 않는다.
  - 우리 파일 안의 다른 사람 운영 줄을 고치면 Phase K-f 공유 메모에 적는다. 이번에 닿는 줄은 아래와 같다.
    - 호진님: `CombatEntryService.cs:121-122`, `TutorialViewSystem.cs:51-53·203-204`, `CombatPrepareJob.cs:45-53`
    - 천명님: `CombatPrepareJob.cs:43`
  - Stage·StageMap·Lobby 파일은 고치지 않는다(Q-K3 B).
  - 새 파일은 `.meta`와 함께 만든다. GUID는 새 32자리 소문자 16진수다.
- 경로 약어:
  - `C/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Combat/`
  - `V/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/`
  - `G/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/`
  - `R/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/`
  - `T/` = `Assets/Tests/EditMode/Framework/`

## 0. 점수 흐름(설계 §6 감점표, 누적)

| Phase 끝 | 7.5에서 | 바뀌는 것 |
|---|---|---|
| K-a (K-1·K-5 b) | **7.8** | Job 안 Runner 재등록(#6, FW 중간)·같은 Tick 연속 가정(#8, 독립성 중간) 해소 |
| K-b (K-2) | **7.9** | 공용 등록기의 전투 전용 입구(#5, SOLID 중간) 해소 |
| K-c (K-3, Q-K3 B) | **7.9** | 우리 소비자의 단계 해석을 질문으로. #10은 Stage·StageMap 쪽이 남아 그대로 |
| K-d (K-4) | **8.0** | 씬 Node 숫자 흩어짐(#9, 독립성 중간) 해소 |
| K-e (K-5 a·c~g) | **8.0~8.1** | 그룹 전체를 받는 Job(FW 낮음) 해소. 나머지 낮음은 기준 상한 안이라 점수 변화 없음 |

- 계산은 기준 여섯의 평균이다(설계 §6 표). 산술은 7.79 / 7.92 / 7.92 / 8.04 / 8.08이다.
- **K-e의 FW +0.25는 FW 낮음이 상한(−1.0)에 정확히 걸려 있을 때만 생긴다.** 새로 생기는 낮음(설계 §6)을 기준에 나눠 넣으면 아래와 같다.
  - SOLID: `CombatNextJob`이 판단과 실행을 함께 한다. SOLID 낮음은 이미 상한이라 변화 없음.
  - FW: `JobType`에 전투 전용 값이 하나 더 생긴다. 이것을 FW 낮음으로 세면 K-e의 +0.25가 상쇄되어 합이 **8.0**이다.
  - `Common` 파일 하나는 문제로 세지 않는다.
- H1은 미룬다.

## Phase K-a — K-1·K-5 b: 단계 Job을 `CombatNextJob` 안에서 실행

**목표:**
- 실행 순간 판단(`BuildStep`)과 그 단계의 실행을 한 `Execute` 안에 둔다. "판단 뒤 바로 이어 실행"을 Runner의 FIFO·예산에 기대지 않고 코드로 만든다.
- Combat Plan만 Runner를 받던 예외를 없앤다.
- 운영 동작 동일(설계 K-1 "동작", 차이는 아래).

| Task | 파일 | 내용 |
|---|---|---|
| Ka.1 | `C/Jobs/CombatNextJob.cs` | 필드 `_runner`, 생성자 인자 `runner`와 그 null 검사를 지운다. `Execute`는 `CombatPlan.BuildStep(_context)` 목록을 차례로 `Execute`한다. 한 Job이 `IsCompleted`가 아니면 `InvalidOperationException`(Job 형식 이름 포함)을 낸다(설계 K-1 스케치, P1). `using System.Collections.Generic` 추가. 머리 주석(`:7-14`)을 "판단과 그 단계의 실행을 한 Job 안에서 한다"로 고친다 |
| Ka.2 | `C/Plans/CombatPlan.cs` | `Register(service, jobType, lifeCycles)`: `runner` 인자·`<param>`(`:47`)·null 검사(`:53-54`)를 지운다. `Next(service, lifeCycles)`도 같다(`:162-168`). 주석 셋을 고친다. 공유 빈 목록 주석(`:28`)의 "CombatNextJob → Runner.RegisterRange"는 "CombatNextJob의 차례 실행"으로, 머리 remarks(`:19-20`)에 "그 단계도 그 Job 안에서 실행"을 더한다. `Settle` 요약(`:211`)의 "같은 Runner Tick에 이어 실행되고"는 "한 Job 안에서 이어 실행되고"로 바꾼다 |
| Ka.3 | `C/Plans/CombatStep.cs` | `CombatStepKind` remarks(`:4-8`)의 "판단과 같은 Runner Tick에 넘겨 바로 이어 실행되므로" → "판단한 Job 안에서 바로 이어 실행되므로". 한 번 실행으로 끝나지 않으면 `CombatNextJob`이 예외를 낸다는 문장을 더한다(P1) |
| Ka.4 | `C/Jobs/CombatExitJobs.cs:103`, `C/Jobs/CombatPreviewJob.cs:22` | 주석의 "판단과 같은 Runner Tick에 바로 이어 실행되므로" → "판단한 Job 안에서 바로 이어 실행되므로"(설계 목록에 없던 두 곳, 계획 대조에서 찾음) |
| Ka.5 | `G/JobSystem/JobRegister/JobRegisterSystem.cs` (연결 줄) | `:70` 주석을 지우고 `:71`을 `CombatPlan.Register(service, jobType, _lifeCycles)`로. `_runner` 필드는 다른 루트가 쓰므로 그대로 둔다 |
| Ka.6 | `C/Jobs/CombatAbandonJob.cs` (K-5 b) | `AlreadyCompleted` 갈래(`:46-49`)를 지워 `default`(받을 수 없는 결과 예외)로 합친다. 요약 셋째 문장(`:13` "그 사이 실제 승패가 먼저 기록됐으면(AlreadyCompleted) 아무것도 바꾸지 않고, 다음 판단이 그 승패로 이탈합니다")은 갈래를 지우면 틀린 말이 된다. 그래서 "Decide는 완료가 없을 때만 포기를 고르고 같은 Job 안에서 실행하므로, 이미 끝난 전투(AlreadyCompleted)는 받을 수 없는 결과다"로 바꿔 쓴다 |
| Ka.7 | `V/Combat/CombatViewSystem.cs:309` | F1 분기 주석 "정리와 완료 표시는 같은 Runner Tick에 이어지고(CombatPlan.Settle)" → "정리와 완료 표시는 한 Job 안에서 이어지고(`CombatNextJob`이 `CombatPlan.Settle`을 차례로 실행)" |
| Ka.8 | 시험 | 아래 |

**`:923-924` 주석(설계 K-1 목록):** 고치지 않는다. "제출 정지 Job은 Global 그룹의 Job Runner Tick에서 View Tick 전에 실행"은 K-1 뒤에도 맞다. 제출 정지 Job이 `CombatNextJob` 안에서 돌고, 그 Job이 Runner Tick에서 돌기 때문이다. 설계 정정(TP-1)으로 남긴다.

**Ka.8 시험(모두 우리 줄):**
- `T/CombatPlanTests.cs:147·160·166`: `CombatPlan.Register(…, new JobRunnerSystem())`에서 마지막 인자를 뺀다.
- `T/CombatProgressJobTests.cs:427·438·450`: `CombatPlan.Register(…, _rig.Runner)`에서 마지막 인자를 뺀다. 그러면 `Rig.Runner`(`:528`)를 쓰는 곳이 없으므로 그 필드도 지운다.
- `T/CombatProgressJobTests.cs:158` `Abandon_AlreadyCompleted_LeavesTheRealOutcomeForTheNextDecision`: 바뀐 동작(이미 끝남 → 예외)을 보므로 지운다(Q-T1 A).
- 그대로 두는 것:
  - `Abandon_ContradictoryResult_Throws`의 `AlreadyCompleted` 사례(`:169`): 지금은 완료가 없어서 예외고, 바뀐 뒤는 갈래가 없어서 예외다.
  - `BuildStep_Settle_BuildsTheChainInOrder_AndTheRealRunnerFinishesItInOneTick`(`:397`): 깨지지 않으므로 두는 것이 규칙이다(TP-15). 이 시험의 Runner 절반은 이제 운영 길이 아니라, 목록을 차례로 돌리는 도구로 남는다.
  - `CombatProgressIntegrationTests.Automatic_InvalidReadAfterRealPublication_ThrowsInTheJob`(`:201`): 예외 뒤 `Runner.IsIdle == false`를 본다. 남는 Job이 단계 Job에서 `CombatNextJob`으로 바뀌지만 Runner가 비지 않는 것은 같다.
- 시험에서 `CombatNextJob`을 직접 만드는 곳은 없다(검증 둘 다 확인).

**운영 동작이 같은 근거(검증 확인):**
- 전투 Job은 모두 우선순위 0이고, Runner 큐는 같은 우선순위에서 FIFO다(`Framework/Utility/JobQueue.cs:338-362`). 그래서 한 Job 안에서 차례로 실행해도 순서가 같다.
- 다른 루트는 Runner가 빌 때만 등록한다(`TutorialViewSystem.cs:228`, `CombatLifeCycle.cs:54`). 운영 코드에 `Runner.Clear` 호출이 없다.
- 단계 Job 안에서 Runner에 다시 등록하는 경우는 전환(`Change`) 뒤의 `ExitCheck`뿐이다. 그것도 지금처럼 Runner 뒤쪽에 들어간다.
- F1 뒤에는 다시 Tick하지 않는다(`GameBootstrap.cs:212`·`:236-239`).

**동작 차이(적어 둠):**
1. 한 번 실행으로 끝나지 않는 단계 Job은 이제 F1이다. 지금은 Runner가 다음 Tick에 조용히 다시 돌렸다(P1). 지금 단계 Job 아홉(단계 종류 일곱)은 모두 한 번에 끝나거나 예외를 낸다(검증 확인).
2. K-5 b: 완료가 있는 `AlreadyCompleted`가 정상 완료에서 F1로 바뀐다. 운영에서 닿지 않는 경로다.
3. 예외가 난 뒤 Runner에 남는 Job이 실패한 단계 Job에서 `CombatNextJob`으로 바뀐다. 누가 다시 Tick하면, 실패한 단계를 다시 하는 대신 `BuildStep`이 다시 판단한다. F1이 런타임을 멈추므로 운영에서는 다시 돌지 않는다.
4. Runner 예산(Tick당 32)을 단계 Job 수만큼 쓰지 않는다.

## Phase K-b — K-2: 진입 등록을 공용 `Register`로

**목표:**
- 진입도 `Register(JobRootType.Combat, JobType.Entry)` 하나로 들어온다. Plan이 진입 서비스의 현재 요청을 읽는다.
- 요청 단계 표시(`MarkQueued`·`Fail`)는 진입 서비스가 한다.
- 동작 동일(차이는 아래).

| Task | 파일 | 내용 |
|---|---|---|
| Kb.1 | `G/JobSystem/JobRegister/JobRootType.cs` (연결 줄) | `EntryTransition = 8` 뒤에 `Entry = 9`와 요약 주석 한 줄(`EntryTransition`과 같은 꼴) |
| Kb.2 | `G/JobSystem/JobRegister/JobRegisterSystem.cs` (연결 줄) | 계약의 `RegisterCombatEntry`(`:24`)와 구현(`:87-106`)을 지운다. 쓰는 곳이 없어지는 `using …LifeCycles.Combat;`(`:15`)도 지운다 |
| Kb.3 | `C/Plans/CombatPlan.cs` | `Register`의 switch에 `case JobType.Entry: return Entry(service, lifeCycles)`를 더한다. 새 private `Entry(service, lifeCycles)`는 `RequireEntry(lifeCycles).Current`를 읽는다. `null`이거나 `!IsAwaitingRegistration`이면 `InvalidOperationException`을 내고, 아니면 `RegisterEntry(service, request, lifeCycles)`를 돌려준다(설계 K-2 스케치). `RegisterEntry`는 이제 Plan 안에서만 쓰이므로 `private`로 바꾼다(TP-2). 머리 remarks(`:21`)에 "진입은 진입 서비스가 `JobType.Entry`로 건다"를 더한다 |
| Kb.4 | `C/CombatEntryRequest.cs` | 질문 `IsAwaitingRegistration => Status == Created`(요약: "만들어졌고 아직 등록되지 않았는지")를 더한다. 계약 `ICombatEntryService.Current` 주석(`:28`)의 "이탈 확인에 씁니다"를 "진입 등록과 이탈 확인에 씁니다"로 |
| Kb.5 | `C/CombatEntryService.cs:180-189` | `_register.RegisterCombatEntry(request)` → `_register.Register(JobRootType.Combat, JobType.Entry); request.MarkQueued();`(try 안, catch는 지금처럼 `Fail` 뒤 다시 던짐, P2). `_current = request;`(`:180`)는 Plan이 `Current`로 읽으므로 `Register`보다 앞에 있어야 한다. 이 이유를 주석 한 줄로 남긴다 |
| Kb.6 | 시험 | 아래 |

**10-22 결정 4-③과의 관계(TP-14):**
- 그때의 결정은 "`MarkQueued`·`Fail`은 등록기에 그대로"였다. 이유는 둘이었다.
  - 등록이 결과를 돌려주지 않는 Framework에서, 등록 순간 요청을 차지하는 확인 장치다.
  - 옮기면 등록기가 중복 등록을 막지 못하고, 우리 시험 `EntryRegistration_CapturesTarget_RejectsDuplicate_…`의 확인 내용이 바뀐다.
- K-2 뒤에 그 두 가지가 어디로 가는지:
  - 등록 순간 요청을 차지하는 것은 요청의 주인인 진입 서비스가 한다(`Register` 바로 뒤 `MarkQueued`).
  - 중복 거절은 두 겹이다. 진입 서비스는 끝나지 않은 요청이 있으면 등록하기 전에 같은 요청을 돌려주거나 예외를 낸다(`:160-166`, 지금 그대로). Plan은 등록을 기다리는 요청이 아니면 예외를 낸다(`IsAwaitingRegistration`).
  - `CombatPlanTests:48`의 중복 거절 단언은 그대로다. 다만 확인하는 대상이 "등록기의 `MarkQueued`"에서 "Plan의 등록 대기 확인"으로 바뀐다.
- 그래서 Q-K2 A는 10-22 결정을 뒤집는 것이다. 사용자 확인이 필요하다.

**`MarkQueued` 시점(P2):** 지금은 "Plan이 Job을 만든 뒤, Runner에 넣기 전"이고, 바뀐 뒤는 "Runner에 넣은 뒤"다. Runner는 Tick에서만 실행하므로 그 사이에 요청을 읽는 곳은 없다. Runner에 넣다 실패하면 지금은 `Queued → Fail`, 바뀐 뒤는 `Created → Fail`이다. 결과(`Faulted`)는 같다.

**동작 차이(적어 둠):** 등록기를 초기화하기 전에 진입을 등록하면 지금은 `InvalidOperationException`(`JobRegisterSystem.cs:92-93`)이다. 바뀐 뒤는 공용 `Register`의 예외다. 서비스를 받기 전이면 `InvalidOperationException`(`:77-78`), 초기화 전이면 `KeyNotFoundException`(`:80-81`)이다. 어느 쪽이든 진입 서비스 catch가 `Fail`한 뒤 다시 던진다.

**Kb.6 시험 — 우리 줄:**
- `T/CombatProgressTestDriver.cs:62`: `RegistrationLog`의 `RegisterCombatEntry` 전달 메서드를 지운다. 이 도우미로 진입을 등록하는 시험은 없다. 그래서 기록 목록(`Calls`)에 `Entry`가 새로 끼지 않는다(`CombatProgressIntegrationTests:68`의 "모두 Next" 단언 그대로).
- `T/CombatPlanTests.cs`: **설계가 놓친 곳**이다. 진입 등록 시험 넷이 실제 등록기의 `RegisterCombatEntry`를 직접 부른다(`:44·48·77·107·130`). 확인 내용(대상 캡처·중복 거절·준비 → 전환 순서·수락일 때만 전환)은 남으므로 호출만 맞춘다(TP-3).
  - `CombatProgressJobTests.FakeEntry`(`:659`, 우리 줄)를 `private` → `internal`로 바꿔 함께 쓴다.
  - `WithManager()`(`:185`) → `WithEntry(CombatEntryRequest request)`. LifeCycle 그룹에 관리자와 `FakeEntry { Current = request }`를 넣는다.
  - 새 도우미 `RegisterEntry(JobRegisterSystem register, CombatEntryRequest request)`는 진입 서비스와 같은 두 줄이다: `register.Register(JobRootType.Combat, JobType.Entry); request.MarkQueued();`
  - `:44·77·107·130` → `RegisterEntry(register, request)`.
  - **요청을 등록기보다 먼저 만들도록 줄 순서를 바꾼다:** `:42-43`, `:75-76`, `:105-107`(요청을 변수로 뽑음), `:126-127`. 그대로 두면 `WithEntry(request)`가 선언 전 사용(CS0841)이다.
  - `:48` 중복 거절: `Assert.Throws<InvalidOperationException>(() => RegisterEntry(register, request))`. 단언은 그대로다(TP-14).
  - `:126`의 `new TestLifeCycleGroup(manager)` → `new TestLifeCycleGroup(manager, new CombatProgressJobTests.FakeEntry { Current = request })`.

**Kb.6 시험 — 다른 사람(호진님, Q-T2):**
- `T/CombatEntryIntegrationTests.cs:962`: 감싼 등록기 `StagePreparationObserver`의 전달 메서드 한 줄을 지운다. 계약에서 빠진 메서드의 `_inner.RegisterCombatEntry`는 컴파일되지 않는다.
  - 이 시험은 실제 런타임 조립(`LifeCycleSystemFactory`)이다. 진입 서비스가 실제 등록기를 받고, 그 등록기의 LifeCycle 그룹에 같은 진입 서비스가 있다. 그래서 Plan의 `Current` 읽기가 그대로 돈다(검증 확인).
- `T/MapCombatPresentationTests.cs`: 가짜 `Registration`(`:265-276`)을 고친다.
  - `Register`는 `(Combat, Entry)`일 때만 `Calls++`와 `OnRegister?.Invoke(Entry.Current)`를 하고, 그 밖에는 지금처럼 `NotSupportedException`을 낸다.
  - `RegisterCombatEntry` 메서드는 지운다.
  - `MarkQueued`는 이제 서비스가 하므로 가짜에서 뺀다.
  - 가짜에 `internal ICombatEntryService Entry;`를 더하고, SetUp(`:35` 뒤)에 `_register.Entry = _service;` 한 줄을 둔다(TP-4).
  - `OnRegister`를 쓰는 시험 둘(`:106`, `:245`)과 `Calls` 단언(`:51`, `:111`)은 그대로 성립한다(검증 확인).
- 그 밖의 호진님 가짜 11개(`ArtifactTestPipeline.cs:49` 포함)는 `RegisterCombatEntry`가 공개 메서드라 계약에서 빠져도 컴파일된다(P5, 명시적 구현 없음). 손대지 않는다.
  - 등록 길이 `Register`로 바뀌어 이 가짜의 `Register`가 `(Combat, Entry)`를 받을 수 있는지도 봤다. 실제 진입 서비스를 만드는 시험 가운데 가짜 등록기로 `RequestEntry`를 부르는 것은 `MapCombatPresentationTests`뿐이다(검증 확인). 그래서 다른 가짜는 그 호출을 받지 않는다.

## Phase K-c — K-3: 진입 요청 단계를 질문으로 (Q-K3 B: 우리 소비자만)

**목표:**
- 우리 소비자(Bootstrap·진입 서비스)가 단계 값을 직접 해석하지 않게 한다.
- `Status`는 public으로 남는다(Stage·StageMap 소비자).
- 동작 동일(식만 옮김).

**질문 범위(TP-5, 설계 표와 다름 — 확인 필요):**
- 설계 K-3 표는 질문 열 넷을 한꺼번에 더한다.
- Q-K3 B에서는 Stage·StageMap 소비자를 옮기지 않는다. 그래서 그쪽만 쓰는 질문(`IsWaitingForJob`·`CanCancelBeforeRun`·`IsPrepared`·`IsEntryDecided`·`HasEndedUnexpectedly`)을 지금 더하면 쓰는 곳 없는 공개 멤버가 된다. 이것은 K-5 d에서 지우는 것과 같은 낮음이다.
- 그래서 **지금 쓰는 질문만 더하고**, 나머지는 식과 함께 호진님 메모에 제안으로 적는다. 호진님이 옮길 때 같은 파일에 더한다.
- `IsQueued`는 설계 표에 없던 새 질문이다. 진입 서비스 `:162`의 식이 `Queued` 하나라서 더한다.

| Task | 파일 | 내용 |
|---|---|---|
| Kc.1 | `C/CombatEntryRequest.cs` | 질문을 더한다(요약 주석 한 줄씩, `HasExited` 옆). `IsAwaitingRegistration`은 K-b에서 더했다. `IsActive` 요약에는 "요청 단계만 본다. 진입 서비스의 `ICombatEntryRead.IsActive`(준비됨 ∧ 오류 아님 ∧ 활성)와 다르다"를 적는다 |
| Kc.2 | `C/CombatEntryService.cs` | 아래 표대로 바꾼다. `IsFinished`(`:233-239`)는 지우고 `IsReplaceable`로 바꾼다(사본 삭제) |
| Kc.3 | `V/Boot/GameBootstrap.cs:222·230` | `_bootEntry?.Status == Rejected` → `_bootEntry?.IsRejected == true`, `Active ∨ Cancelled` → `_bootEntry != null && (_bootEntry.IsActive \|\| _bootEntry.IsCancelled)` |
| Kc.4 | 시험 | 단계 질문을 보던 시험은 없다(새 멤버라). 바꾸는 시험 없음 |

| 질문 | 식 | 쓰는 곳(바꾸는 줄) |
|---|---|---|
| `IsAwaitingRegistration` | `Created` | K-b Plan `Entry` |
| `IsQueued` | `Queued` | 진입 서비스 `:162`(같은 대상 중복 요청은 같은 요청을 돌려줌) |
| `IsActivating` | `Activating ∧ IsJobCompleted` | 진입 서비스 `:207`(활성화 확인) |
| `IsActive` | `Active` | 진입 서비스 `:43`, Bootstrap `:230` |
| `IsCancelled` | `Cancelled` | 진입 서비스 `:121-122`, Bootstrap `:230` |
| `IsRejected` | `Rejected` | 진입 서비스 `:121-122`, Bootstrap `:222` |
| `IsReplaceable` | `HasExited ∨ (Rejected ∨ Cancelled) ∧ IsJobCompleted` | 진입 서비스 `:160`(사본 `IsFinished` 삭제) |

- **진입 서비스 `:121-122`(호진님 줄):** `_current.Status != Closed && != Cancelled && != Rejected` → `!_current.HasExited && !_current.IsCancelled && !_current.IsRejected`. 같은 식을 질문으로 적은 것이다. 이 셋은 "끝난 의미를 보존할 단계"인데, 이 식을 쓰는 곳은 여기 하나라 새 질문은 만들지 않는다(TP-7).
- **설계 정정(TP-6):** 설계 K-3 표는 `IsReplaceable`이 "Bootstrap 로그 비우기"에도 쓰인다고 했다. Bootstrap `:230`의 식(`Active ∨ Cancelled`)은 `IsReplaceable`과 다르다. 그래서 단계 질문 둘로 쓴다.
- 전이 Job의 `switch (Status)`(`C/Jobs/CombatEntryTransitionJob.cs`)는 그대로 둔다(P3).

## Phase K-d — K-4: 씬 Node 순서 표

**목표:**
- Bootstrap과 Combat 씬 시스템의 Loading Node 순서 숫자를 표 하나에 모은다. 관계는 식으로 적는다.
- 동작 동일(값 그대로).

| Task | 파일 | 내용 |
|---|---|---|
| Kd.1 | 새 `R/Common/SceneNodeOrder.cs` (+ `.meta`, 새 GUID) | 설계 K-4 스케치대로 둔다. `internal static class`, NS `ProjectL.Systems.RuntimeSystem.Common`. 줄 끝은 같은 폴더 파일과 같게 한다(작업 트리 CRLF, 저장소는 LF — `core.autocrlf=true`, BOM 없음). 요약 주석의 범위는 TP-8대로 적는다 |
| Kd.2 | `V/Boot/GameBootstrap.cs:285-294` | 상수 넷(`BindUIPriority`·`BindCamerasPriority`·`PrepareCombatDeckPriority`·`OpenSceneUIPriority`)과 그 머리 주석 두 줄을 지운다. 노드 표(`:307-318`)가 `SceneNodeOrder.BindSceneUI`·`BindCameras`·`PrepareCombatDeck`·`OpenSceneUI`를 읽는다. 각 상수의 요약 주석(무엇 전인지)은 표 파일로 옮긴다. 바뀌는 줄은 모두 우리 줄이다 |
| Kd.3 | `V/Combat/CombatViewSystem.cs:73-78` | 상수 셋(`SceneEnterPriority`·`SceneExitPriority`·`BackdropEnterPriority`)과 주석을 지운다. 등록 셋(`:274-276`)이 `SceneNodeOrder.CombatBackdrop`·`CombatViewAttach`·`CombatViewRelease`를 읽는다 |
| Kd.4 | `V/Tutorial/TutorialViewSystem.cs:51-53·203-204` (호진님 줄) | 상수 둘과 주석을 지운다. 등록 두 줄(`:203-204`)이 `SceneNodeOrder.TutorialAttach`·`TutorialDetach`를 읽는다. 주석 "전투 View가 전투를 붙인 뒤…"는 표의 그 줄로 옮긴다 |
| Kd.5 | 시험 | 아래 |

**표의 범위(TP-8):**
- 표가 갖는 것: Bootstrap 노드(Stage·Lobby·Combat 세 씬 공통)와 Combat 씬 시스템(전투 View·튜토리얼) 노드.
- 표가 갖지 않는 것: Stage·Lobby 시스템의 자기 노드. 이탈 준비 −200, View 연결 100, 카메라 핸들 AfterLoad 100이다. 다른 영역이라 각자 상수로 남는다.
- 표 요약 주석에 이 범위와 "LifeCycle 0은 기반(`LifeCycleBase`) 값을 따라 적은 것"(P4)을 적는다.

**Kd.5 시험:**
- 우리 줄 `T/CombatViewSystemTests.cs:197-199`: 지워지는 상수 읽기만 표로 바꾼다.
  - 197: `{ SceneNodeOrder.CombatBackdrop, SceneNodeOrder.CombatViewAttach }`
  - 198: 왼쪽 값만 `SceneNodeOrder.CombatBackdrop`으로 바꾸고, 오른쪽 리터럴 `-190`은 그대로 둔다. 리터럴까지 표로 바꾸면 표의 식(`BindCameras - 5`) 때문에 늘 참인 단언이 된다.
  - 199: `SceneNodeOrder.CombatViewAttach`. 표 상수끼리의 비교라 확인 가치가 줄어든다. 컴파일 맞춤만 한다.
  - `using ProjectL.Systems.RuntimeSystem.Common`이 없으면 더한다.
- 다른 사람(호진님, Q-T2) `T/TutorialViewSystemTests.cs:34-35`: `CombatViewSystem.SceneEnterPriority` → `SceneNodeOrder.CombatViewAttach`, `SceneExitPriority` → `SceneNodeOrder.CombatViewRelease`(TP-9). 메시지·단언 그대로.
  - Q-T2에서 이 두 줄을 허락하지 않으면, 조정자에 `internal const int SceneEnterPriority = SceneNodeOrder.CombatViewAttach;`처럼 별칭 둘을 남긴다(시험 무변경, 이름이 둘이 되는 낮음).

## Phase K-e — K-5 a·c~g: 골격 쪽 작은 정리

| Task | 항목 | 파일 | 내용 |
|---|---|---|---|
| Ke.1 | a | `C/Plans/CombatPlan.cs` | `RequiresChapterCompletion` 옆에 `internal static bool IsAbandonment(BattleCompletionChanges completion) => completion != null && completion.Outcome.Outcome == StageOutcome.Defeat && completion.Outcome.Reason == StageOutcomeReason.VoluntaryAbandonment;`(요약: 자발적 포기로 끝난 완료) |
| Ke.2 | a | `C/Jobs/CombatAbandonJob.cs:41-44` | `if (!CombatPlan.IsAbandonment(after.Completion)) throw …`. `using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat.Plans;`를 더한다(지금 없음) |
| Ke.3 | a | `C/Jobs/CombatExitJobs.cs:79` | `if (!CombatPlan.IsAbandonment(completion)) RequireFinalResult(…)`. 같은 `using …Combat.Plans;`를 더한다(지금 없음) |
| Ke.4 | c | `C/Jobs/CombatCommandJob.cs:38-39`, `CombatPreviewJob.cs:41-42`, `CombatAdvanceJob.cs:42-43` | 생성자가 이미 `ArgumentNullException`이라 늘 거짓인 null 검사 두 줄씩을 지운다. `CombatPrepareJob.cs:33-34`는 Ke.7에서 함께 |
| Ke.5 | d | `C/CombatProgressReader.cs` | `ICombatProgressReader.ActiveCharacterCount`(`:23-24`)와 구현(`:100-107`)을 지운다. 그러면 쓰는 곳이 없어지는 것도 함께 지운다(TP-11): `_characters` 필드(`:36`), `Bind`의 `characters` 인자(`:59`·`:67`), `PostBindService`의 넘김(`:51`), `using …BufferSystems.Character;`(`:4`) |
| Ke.6 | d | `C/CombatEntryRequest.cs:78-80` | 쓰는 곳 없는 `IsInProgress`를 지운다 |
| Ke.7 | e | `C/Jobs/CombatPrepareJob.cs` | 생성자를 `(IBattleEntryService battleEntry, IStageCommandProcessingService mapCommands, int priority, CombatEntryRequest entry)`로 바꾼다. 이어서 아래를 한다. 다른 사람 줄이 바뀐다(천명님 `:43`, 호진님 `:45-53`) |
| Ke.8 | f | `C/Plans/CombatPlan.cs:149` | `IsAwaitingPlayerCommand` 요약의 "Decide가 이때 할 일을 고르지 않습니다" → "Decide는 이때 명령 처리·자동 진행을 고르지 않습니다(미리보기 응답은 고를 수 있습니다)" |
| Ke.9 | g | `V/Boot/GameBootstrap.cs:223-226` | 낡은 힌트 변수와 주석을 지우고 로그에는 거절 사유 둘만 남긴다(TP-17). 비전투 노드는 지금 거절이 아니라 예외이고, 개발 진입은 `ResolveDevEntryNode`가 전투 노드로 보정한다 |
| Ke.10 | g | `V/Boot/GameBootstrap.cs:656-668` | `ToLifeCycleKey`의 닿지 않는 `Combat` 갈래(우리 줄)를 지운다(부르는 곳 `:560`이 Combat을 먼저 거름). 요약 주석 "Combat은 진입 요청으로만 들어간다"를 단다. Combat이 오면 `default` 예외다 |
| Ke.11 | — | 시험 | 아래 |
| Ke.12 | (K-d 검토, 2026-10-23 사용자 결정 A로 더함) | `V/Combat/CombatViewSystem.cs:82·382·385·386·415·417·655·875`, `V/Combat/Display/Map/StageViewPropPresenter.cs:19`, `Systems/Factory/Runtime/InGame/View/CombatSceneDisplayFactory.cs:32` | 주석·예외 문구의 순서 숫자(−195·−190·0·100)를 `SceneNodeOrder` 이름으로 바꾼다. XML 주석은 using이 있는 파일이면 `<see cref>`, 없으면 `<c>`. 모두 우리 줄이다(blame) |

**Ke.7 준비 Job 자세히:**
- `entry` null 검사 뒤 **진입 종류에 필요한 서비스만** 요구한다. 직행이면 `battleEntry`, 맵이면 `mapCommands`가 null일 때 `ArgumentNullException`이다(TP-10).
- `Execute`의 null 검사(`:33-34`)를 지운다. `:43`·`:45`는 받은 서비스를 부른다.
- using을 맞춘다. `…InGameSystem.CoreSystem.CombatSystem`·`…InGameSystem.CoreSystem.StageCommandSystem`을 더하고, 쓰지 않게 되는 `…InGameSystem`을 지운다.
- Plan `RegisterEntry`(`:41`)는 `service.CoreGroup?.BattleEntry`·`service.CoreGroup?.StageCommands`를 넘긴다. `CombatStepContext.From`(`:74-76`)의 `core?.X`와 같은 꼴이다.

**`CombatEndUI:150`은 이번에 고치지 않는다(TP-16, 설계와 다름 — 확인 필요):**
- 지금 `G/UISystem` 안에는 `LifeCycleSystem`을 참조하는 곳이 없다. 고치면 결과 화면이 처음으로 골격 판단 클래스(`CombatPlan`, internal)를 `using`한다.
- 설계 §8은 "결과 화면이 판정된 값만 받기"(#3)를 View 단계로 미뤘다. 그때 이 줄의 판정은 결과 화면에서 빠진다.
- 그래서 K-5 a는 골격 두 곳만 하나로 모은다. `CombatEndUI:150`은 View 단계 #3에서 다룬다고 현강님 메모에 적는다.

**Q-K5 범위(TP-12, 권장 바꿈 — 확인 필요):**
- 설계 Q-K5 A는 `CombatProgressFacts.BattleInstanceId`도 지운다. 그러나 이 값은 생성자 **둘째 순서 인자**다.
- 호진님 `T/TutorialPlanTests.cs:177`이 `new CombatProgressFacts(true, 1, phase, …)`로 순서대로 넘긴다. 지우면 `1`이 `phase` 자리로 가서 컴파일이 깨진다. 설계 대조 때 놓친 곳이다.
- 남는 낮음은 기준 상한(−1.0) 안이라 지워도 점수가 바뀌지 않는다.
- 그래서 **권장을 B(`BattleInstanceId`는 둠)로 바꾼다.** A를 고르면 호진님 시험 한 줄(`1, ` 삭제)과 우리 시험 다섯도 고친다.
  - 우리 시험 다섯: `CombatPlanDecideTests:137·147`·`CombatProgressJobTests:461`의 이름 인자 삭제, `CombatProgressReaderTests:67`·`CombatEntryIntegrationTests:287`의 단언 삭제

**Ke.11 시험 — 우리 줄:**
- `T/CombatProgressReaderTests.cs:186-190` `ActiveCharacterCount_ReadsCharacterBuffer`: 걷은 기능이라 지운다.
- 같은 파일 `Bind` 호출 일곱 곳을 맞춘다(TP-11).
  - 넷째 인자 `_buffers.CharacterReadSink`를 뺄 곳 여섯: `:47`·`:146`·`:156`·`:158`·`:160`·`:164`
  - 지울 곳 하나: `:162`. 넷째 인자가 `null`인 사례라, 걷은 인자의 검사다. 그 단언 두 줄을 지운다.
- `T/CombatProgressTestDriver.cs:28-29`: `reader.Bind`에서 `buffers.CharacterReadSink`를 뺀다.
- `T/CombatProgressJobTests.cs`: 가짜 `Reader`의 `Characters`(`:545`)와 `ActiveCharacterCount`(`:554`)를 지운다. 계약에서 빠졌다. `Characters`를 읽는 시험은 없다(검증 확인).
- `T/CombatPlanTests.cs:92·174·175`: **설계가 놓친 곳**이다(설계 K-5 e "직접 만드는 시험 없음"은 틀림). 새 준비 Job 생성자에 맞춘다. 확인 내용(Core 예외 보존·null 거절)은 그대로다.
  - `:92` → `new CombatPrepareJob(core, null, 0, request)`(`CombatProbe`가 `IBattleEntryService`를 구현하고, 직행 진입이다)
  - `:174` → `new CombatPrepareJob(null, null, 0, request)`(직행인데 Core 진입 서비스가 없음 → `ArgumentNullException`)
  - `:175` → `new CombatPrepareJob(new CombatProbe(), null, 0, null)`(요청 없음 → `ArgumentNullException`)

**Ke.11 시험 — 다른 사람:** 없다.
- 호진님 가짜 셋(`CombatEntryIntegrationTests.MapMovement.cs:514`, `MapCombatPresentationTests.cs:297`, `StageMapNodeMovementTests.cs:421`)의 `ActiveCharacterCount`는 공개 속성으로 남아도 컴파일된다(P5).
- `CombatProgressReader.Bind`를 부르는 다른 사람 시험은 없다(검증 확인).

## Phase K-f — 문서·공유 (모두 미전달로 작성, 코드 변경 없음)

| Task | 문서 | 내용 |
|---|---|---|
| Kf.1 | `docs/superpowers/handoffs/2026-10-10-combat-job-plan-framework-share-minhyeong.md` 새 §7 | 연결 줄 셋(P6): Combat Plan 줄의 Runner 인자 삭제(K-a), `IJobRegisterSystem.RegisterCombatEntry` 삭제와 쓰지 않게 된 using(K-b), `JobType.Entry = 9`(K-b). 다른 루트 Plan과 Runner 등록 방식은 그대로다. §6.5처럼 "앞 절에서 바뀐 사실"로 §6.1의 "`MarkQueued`도 그대로다"와 §6.2의 "`runner.RegisterRange(...)`로 넘기고"가 낡았다고 적는다 |
| Kf.2 | `docs/superpowers/handoffs/2026-10-22-combat-view-structure-hojin-share.md` 새 §9 | ① 우리 파일 안 호진님 줄: `CombatEntryService.cs:121-122`(K-c), `TutorialViewSystem.cs:51-53·203-204`(K-d), `CombatPrepareJob.cs:45-53`(K-e, 맵 진입 갈래가 받은 서비스를 부름) ② 고친 시험 셋(Q-T2) ③ Stage·StageMap의 `Status` 해석 14줄을 질문으로 바꾸자는 제안(설계 K-3 표의 대응, 더할 질문 다섯과 식) ④ 낡은 호진님 문서 두 곳 알림(고치지 않음): `viewDocs/stage-selection-input-guide.md:23`, `guides/stage-artifact-shop-sequence-uml.md:159` ⑤ 메모 `:34`가 알린 Bootstrap 이름 있는 상수 넷이 `SceneNodeOrder`로 옮겨짐(K-d 검토로 더함) ⑥ 같은 메모(미전달)의 낡은 줄에 "바뀜" 표시: `:14`(단계 Job을 Runner에 넘김 → K-1로 한 Job 안 실행), `:16`·`:70`(`IsInProgress` → K-5 d로 지움, ③의 제안 질문을 씀)(K-e 검토로 더함) |
| Kf.3 | `docs/superpowers/handoffs/2026-10-22-combat-view-structure-hyeongang-share.md` 새 §7 | ① 메모 §2가 알린 Bootstrap 노드 상수와 "원본은 안내 §1.1"이 `SceneNodeOrder` 표로 옮겨짐 ② `CombatEndUI:150`의 포기 판정은 이번에 고치지 않고 View 단계 #3에서 다룸(TP-16) |
| Kf.4 | `docs/superpowers/handoffs/2026-09-24-view-core-data-boundary-handoff.md` 새 §32 | 천명님 줄 `CombatPrepareJob.cs:43`: 준비 Job이 Core 진입 서비스를 직접 받는다. 부르는 Core API·인자·결과 처리는 그대로이고, Core 요청은 없다 |
| Kf.5 | `docs/superpowers/viewDocs/runtime-scene-binding-guide.md` | §1.1 제목의 "(원본)"을 지운다. 표 위 문장(`:75`)과 그 앞 안내(`:70` "§1.1 표가 원본이다")를 "Bootstrap과 Combat 씬 시스템의 숫자 원본은 코드 `SceneNodeOrder`, Stage·Lobby 시스템은 각자 상수"로 고친다. "숫자의 원본" 칸은 아래처럼 바꾼다. Combat·Stage·Lobby가 섞인 Exit −100·Enter 100 줄(`:81`·`:88`)은 Combat과 Stage·Lobby로 나눈다. 조정자 배경 −195 줄, 튜토리얼 두 줄, Bootstrap 네 줄(−200·−190·90·200)은 `SceneNodeOrder.<이름>`으로 바꾼다(K-d 검토로 바로잡음: 다섯 → 넷, −195 줄 추가) |
| Kf.5b | 노드 이름을 적은 다른 문서(K-d 검토로 더함) | 민형님 Factory 공유 `handoffs/2026-10-15-combat-scene-display-factory-framework-share.md:280`의 `CombatViewSystem.BackdropEnterPriority`에 "대신함: `SceneNodeOrder.CombatBackdrop`" 한 줄. 배경 계획 `plans/2026-10-23-combat-backdrop-before-camera-bind-plan.md:100`·CLAUDE.md 배경 줄의 "BK-3 남은 것: `BindCamerasPriority` 주석"은 해소로 적는다(상수가 없어지고 관계가 `CombatBackdrop = BindCameras - 5`로 적힘) |
| Kf.6 | 정책 원본에 "대신함" 표시 | `specs/2026-10-22-combat-view-structure-improvement-design.md` §5.1("판단한 뒤 Runner에 넘긴다")에 K-1이 대신한다고, `specs/2026-10-10-combat-job-plan-restructure-design.md`의 진입 경로(`RegisterCombatEntry`)와 계약 스케치의 `ActiveCharacterCount`(`:252`)에 K-2·K-5 d가 대신한다고 머리 한 줄씩 단다. `handoffs/2026-10-09-combat-skeleton-view-programmer-handoff.md:193`의 Bootstrap 거절 힌트 안내에도 "지움(K-5 g)" 한 줄(K-e 검토로 더함) |
| Kf.7 | 설계 문서 | 상태 줄과 §9 실행 기록. 계획에서 바로잡은 것을 반영한다(TP-1·TP-5·TP-6·TP-10~TP-12·TP-14·TP-16) |
| Kf.8 | `CLAUDE.md` | "현재 상태"에 골격 점검 줄. **"작업 방식"의 영역 규칙(`:13`)이 연결 줄 예시로 `RegisterCombatEntry`를 적고 있다. 규칙 글이므로 고치기 전에 사용자에게 묻는다(TP-18)** |

## 계획에서 정한 것 (사용자 확인 대상)

- **TP-1** `CombatViewSystem.cs:923-924` 주석은 고치지 않는다(K-1 뒤에도 맞음). 설계 K-1의 주석 목록에서 뺀다.
- **TP-2** `CombatPlan.RegisterEntry`를 `private`으로 바꾼다. K-b 뒤 Plan 밖에서 부르는 곳이 없다. 시험도 부르지 않는다.
- **TP-3** `CombatPlanTests` 진입 시험 넷은 다음처럼 호출을 맞춘다.
  - 우리 `CombatProgressJobTests.FakeEntry`를 `internal`로 바꿔 함께 쓴다. 같은 꼴의 가짜를 하나 더 만들지 않는다.
  - 진입 서비스와 같은 두 줄(`Register(Combat, Entry)` → `MarkQueued`)을 도우미로 둔다.
  - 진입 서비스 전체를 조립하는 쪽으로 바꾸지는 않는다. 확인 내용이 등록기·Plan이기 때문이다.
- **TP-4** 호진님 `MapCombatPresentationTests` 가짜는 진입 서비스를 필드로 받아 `Current`를 넘긴다(SetUp 한 줄). `OnRegister`의 서명은 그대로라 그것을 쓰는 시험 둘은 고치지 않는다.
- **TP-5** K-3 질문은 지금 쓰는 일곱만 더한다(설계 표에 없던 `IsQueued` 포함). Stage·StageMap 전용 질문 다섯은 호진님 메모 제안으로 둔다. 설계 K-3 표(열넷을 한꺼번에)와 다르다.
- **TP-6** Bootstrap `:230`은 `IsActive || IsCancelled`다. 설계 표의 "Bootstrap 로그 비우기 = `IsReplaceable`"은 틀렸다.
- **TP-7** 진입 서비스 `:121-122`(호진님 줄)는 새 질문 없이 단계 질문 셋의 조합으로 쓴다(쓰는 곳 하나).
- **TP-8** `SceneNodeOrder`의 범위는 Bootstrap 노드(세 씬)와 Combat 씬 시스템 노드다. Stage·Lobby 시스템 노드는 각자 상수로 남는다.
- **TP-9** 호진님 `TutorialViewSystemTests:34-35` 두 줄을 표로 바꾼다(Q-T2 허락 대상). 허락하지 않으면 조정자 상수 둘을 표 별칭으로 남긴다.
- **TP-10** 준비 Job은 진입 종류에 필요한 Core 서비스만 null 검사한다.
  - 시험 가짜 Core(`CombatPlanTests.FakeCoreService`)가 `StageCommands => null`이라, 둘 다 요구하면 직행 진입 시험이 실행에서 깨진다.
  - Core 서비스를 읽는 때가 실행(Runner Tick)에서 등록(Plan)으로 옮겨진다. 운영 조립에서는 둘 다 늘 있으므로 차이가 없다.
  - 없는 조립에서는 예외가 나는 자리가 `RequestEntry`를 부른 자리로 옮겨진다. 진입 서비스 catch가 `Fail`한 뒤 다시 던지고, 부르는 쪽에서 F1이다. 결과는 같다. Bootstrap 직행 진입이라면 `_bootEntry`가 비어 있는 채로 Update의 catch가 F1을 보고한다.
- **TP-11** `ActiveCharacterCount`를 지우면서 쓸모가 없어지는 `_characters`·`Bind` 인자·using도 지운다. 남기면 쓰지 않는 의존이 null 검사만 받는다. 우리 시험은 `Bind` 호출 여섯 곳의 인자를 빼고, 그 인자의 null 사례 하나를 지우고, 드라이버 하나를 맞춘다.
- **TP-12** Q-K5 권장을 B로 바꾼다(`BattleInstanceId` 유지). 이유는 Phase K-e "Q-K5 범위"에 있다.
- **TP-13** `IsAbandonment`는 Job의 엄격한 꼴(패배 ∧ 자발적 포기)로 하나로 둔다. 사유만 보던 `CombatExitJobs:79`는 Core가 만들지 않는 조합(승리 + 자발적 포기)에서만 결과가 달라진다. `StageOutcome`은 InProgress·Victory·Defeat뿐이다.
- **TP-14** Q-K2 A는 10-22 결정 4-③(`MarkQueued`·`Fail`은 등록기에 그대로)을 뒤집는다. 중복 거절은 진입 서비스(지금 그대로)와 Plan의 등록 대기 확인 두 겹으로 남는다. `CombatPlanTests:48`은 확인 대상이 등록기에서 Plan으로 바뀐다(Phase K-b "10-22 결정 4-③과의 관계").
- **TP-15** `BuildStep_Settle_…AndTheRealRunnerFinishesItInOneTick`은 깨지지 않으므로 그대로 둔다(CLAUDE.md "깨지는 곳만 최소로"). 검증 에이전트 하나는 Runner 부분을 차례 실행으로 바꾸자고 했지만, 깨지지 않는 시험을 고치는 것은 규칙 밖이라 받지 않았다. 이름의 "Runner"가 운영 길과 달라진다는 점만 기록한다.
- **TP-16** `CombatEndUI:150`은 이번에 고치지 않는다. 결과 화면(UISystem)이 처음으로 골격 판단 클래스를 참조하게 되고, View 단계 #3이 이 판정을 결과 화면에서 걷기 때문이다. 설계 K-5 a(셋을 하나로)와 다르다.
- **TP-17** Bootstrap의 낡은 거절 힌트는 고쳐 쓰지 않고 지운다. 설계 K-5 g는 "지금 동작으로 바꿈"이었다. 지금 거절이 나는 사유(챕터 불일치·길 찾기 실패 등)에 붙일 만한 개발 힌트가 없다. 로그에는 Core가 준 사유 둘이 그대로 남는다.
- **TP-18** CLAUDE.md "작업 방식"의 영역 규칙에서 `RegisterCombatEntry` 예시를 고칠지는 사용자에게 묻는다. 규칙 글이라 상태 줄처럼 바로 고치지 않는다.

## 커밋 묶음(사용자가 함)

- K-a~K-e 코드는 한 브랜치에 차례로 쌓인다. Phase마다 커밋하면 리뷰가 쉽다.
- 함께 커밋할 것:
  - 새 파일 `R/Common/SceneNodeOrder.cs` + `.meta`
  - 연결 줄 둘(`JobRootType.cs`·`JobRegisterSystem.cs`)과 그것을 쓰는 `CombatPlan.cs`·`CombatEntryService.cs`. K-b는 이 넷이 한 커밋이어야 컴파일된다.
- C 단계(커밋 전)가 먼저 커밋되면 이 계획의 줄 번호가 그대로다. 함께 커밋해도 된다.

## 검증 기록 (2026-10-23, 하위 에이전트 2개, 읽기 전용)

- **에이전트 A(코드 사실·호출처):** 높음 3·중간 4·낮음 5.
  - 받은 것: `Bind` `:146` 누락과 개수, `CombatPlanTests:42-43·75-76` 줄 순서, using 누락 둘(`CombatAbandonJob`·`CombatPrepareJob`), `Entry(service, lifeCycles)` 서명, `TutorialViewSystem:203-204` 호진님 줄, `CombatAbandonJob:13` 요약 문장과 줄 번호(`:46-49`), Q-K5 A의 우리 시험 하나 더(`CombatEntryIntegrationTests:287`), `:198` 왼쪽 값, 낡을 문서 넷, `CombatProgressReader` using, 가짜 11개 근거 문장.
- **에이전트 B(설계 일치·동작·규칙):** 중간 4·낮음 10.
  - 받은 것: 10-22 결정 4-③ 뒤집기(TP-14), `CombatPrepareJob`의 천명님·호진님 줄과 넘기기, `CombatEndUI` 새 의존(TP-16), 동작 차이 넷, `IsQueued`·`IsActive` 이름, 힌트 삭제를 TP로(TP-17), 리터럴 `-190` 유지, 넘기기·문서 목록, "대신함" 표시, CLAUDE.md 규칙 글(TP-18), 점수의 새 낮음, `CoreGroup?.`와 오류 자리.
- **받지 않은 것:**
  - 두 에이전트 모두 `R/Common/` 파일이 LF라고 했다. 저장소(`i/lf`)는 LF가 맞지만 작업 트리는 CRLF다(`w/crlf`, `core.autocrlf=true`). 그래서 "같은 폴더 파일과 같게"를 유지하고 표기만 고쳤다.
  - `BuildStep_Settle_…` 시험을 차례 실행으로 바꾸자는 제안(TP-15).
- **에이전트가 맞다고 확인한 것:**
  - K-a~K-e 줄 번호(±2 안)
  - 구조체 상자 의미
  - `FakeEntry` 재사용 가능성
  - 부르는 곳 목록 전부(`CombatPlan.Register`·`new CombatNextJob`·`new CombatPrepareJob`·`RegisterEntry`·`RegisterCombatEntry`·`ICombatProgressReader` 구현·노드 상수)
  - K-b 조립에서 같은 LifeCycle 그룹
  - `MapCombatPresentationTests` 단언 성립
  - "확인함" 주장 셋
  - TP-6·TP-10·TP-12 사실
  - 다른 사람 줄 주인

## 실행 기록

(Phase마다 여기에 적는다: 바꾼 것, 계획과 다른 점, 새 에이전트 검토 결과와 반영, 고친 시험, 컴파일 미확인.)

### Phase K-a (K-1·K-5 b) — 2026-10-23, 코드 완료, **컴파일 미확인**(MSBuild 안 돌림, 새 `[Test]` 없음)

**바꾼 것(Ka.1~Ka.7):**
- `C/Jobs/CombatNextJob.cs`: Runner 필드·인자를 지웠다. `BuildStep` 목록을 차례로 `Execute`하고, 끝나지 않은 Job이 있으면 형식 이름을 넣은 `InvalidOperationException`을 낸다. 머리 주석을 "판단과 실행을 한 Job 안에서"로 바꿨다.
- `C/Plans/CombatPlan.cs`: `Register(service, jobType, lifeCycles)`와 `Next(service, lifeCycles)`가 됐다. 머리 remarks·`NoJobs` 주석·`Settle` 요약을 고쳤다.
- `C/Plans/CombatStep.cs`: remarks와 `Settle` 요약을 고쳤다.
- `C/Jobs/CombatAbandonJob.cs`: `AlreadyCompleted` 갈래를 지워 `default` 예외에 합쳤다. 요약 셋째 문장을 바꿔 썼다.
- `C/Jobs/CombatExitJobs.cs:103`, `C/Jobs/CombatPreviewJob.cs:22`: 주석.
- 연결 줄 `G/JobSystem/JobRegister/JobRegisterSystem.cs:70`: `CombatPlan.Register(service, jobType, _lifeCycles)`. 그 위 우리 주석 한 줄을 지웠다.
- `V/Combat/CombatViewSystem.cs:309-311`: F1 분기 주석.
- `:923-925` 주석은 TP-1대로 두었다.

**고친 시험(Ka.8, 모두 우리 줄):**
- `T/CombatPlanTests.cs:147·160·166`: Runner 인자를 뺐다.
- `T/CombatProgressJobTests.cs`:
  - `CombatPlan.Register` 세 곳(`:415·426·438`)에서 Runner 인자를 뺐다.
  - 쓰는 곳이 없어진 `Rig.Runner` 필드를 지웠다.
  - `Abandon_AlreadyCompleted_LeavesTheRealOutcomeForTheNextDecision`을 지웠다(바뀐 동작, Q-T1 A).
- 그대로 둔 것: `BuildStep_Settle_…`(TP-15), `Abandon_ContradictoryResult_Throws(AlreadyCompleted)`(이제 default 갈래로 예외).

**계획과 다른 점:**
- 없다. Ka.4 주석 두 곳에 `<see cref="CombatNextJob"/>`을 더한 것만 계획 글보다 조금 많다.
- 계획 "동작 차이 1"의 "단계 Job 일곱"은 "단계 Job 아홉(단계 종류 일곱)"으로 바로잡았다(검토 지적).

**새 에이전트 검토(읽기 전용, 1개): 높음 0·중간 1·낮음 4. 모두 반영했다.**
- 중간: `CombatStep.cs` `Settle` 요약 "완료 표시를 한 번에 등록합니다"가 낡았다 → "목록을 한 번에 만들고 `CombatNextJob`이 차례로 실행합니다".
- 낮음:
  - `CombatStep.cs` remarks 문장이 두 가지로 읽혔다 → "판단한 뒤 자기 안에서 바로 이어 실행하므로, 기다리는 단계 Job은 허용하지 않습니다".
  - 조정자 `:309-311` 줄바꿈과 "Settle을 실행" 표현 → "Settle이 만든 목록을 차례로 실행".
  - `CombatAbandonJob.cs:13`의 실행 주체 → "`CombatNextJob`이 그 판단과 같은 Job 안에서".
  - 계획의 개수 표현.
- 검토가 확인한 것:
  - 바뀐 서명을 부르는 곳이 모두 맞춰졌다(운영 1·시험 6). 남은 참조는 없다.
  - using과 cref가 모두 풀린다.
  - 상자 동작이 맞다.
  - 운영 동작이 같다(FIFO·우선순위 0·실행 중 다른 등록은 전환 Job뿐·예외는 그대로 F1·`Runner.Clear` 없음). 계획 동작 차이 1~4 밖의 차이는 없다. 옛 `RegisterRange`의 null 항목 검사는 빠졌지만 `BuildStep`이 null을 만들지 않는다.
  - 새로 깨지는 시험이 없다.
  - 바꾼 줄은 모두 우리 줄이다.
- 줄 끝: 새로 쓴 `CombatNextJob.cs`는 다른 파일과 같게 CRLF로 맞췄다. 나머지 파일도 CRLF 그대로다.
- Unity 확인: 컴파일·EditMode 시험 문제 없음(2026-10-23 사용자).

### Phase K-b (K-2) — 2026-10-23, 코드 완료, **컴파일 미확인**(MSBuild 안 돌림, 새 `[Test]` 없음)

**바꾼 것(Kb.1~Kb.5):**
- 연결 줄 `G/JobSystem/JobRegister/JobRootType.cs`: `JobType.Entry = 9`와 요약 한 줄.
- 연결 줄 `G/JobSystem/JobRegister/JobRegisterSystem.cs`: 계약의 `RegisterCombatEntry`와 구현을 지웠다. 쓰지 않게 된 `using …LifeCycles.Combat;`도 지웠다.
- `C/Plans/CombatPlan.cs`:
  - `case JobType.Entry`를 더했다.
  - 새 `Entry(service, lifeCycles)`는 `RequireEntry(lifeCycles).Current`를 읽고, 등록 대기가 아니면 예외를 낸 뒤 `RegisterEntry`를 돌려준다.
  - `RegisterEntry`를 `private`으로 바꿨다(TP-2).
  - 머리 remarks에 `JobType.Entry`를 더했다.
- `C/CombatEntryRequest.cs`: 질문 `IsAwaitingRegistration`(`Created`)을 더했다. `ICombatEntryService.Current` 요약을 고쳤다.
- `C/CombatEntryService.cs`:
  - `_register.Register(JobRootType.Combat, JobType.Entry); request.MarkQueued();`(try 안, catch는 그대로 `Fail` 뒤 다시 던짐)
  - `_current = request;`가 등록보다 앞이어야 하는 이유와 조립 전제를 주석 두 줄로 남겼다.
  - 머리 요약을 고쳤다.

**고친 시험:**
- 우리:
  - `T/CombatProgressTestDriver.cs`: `RegistrationLog`의 전달 메서드를 지웠다.
  - `T/CombatProgressJobTests.cs`: `FakeEntry`를 `private` → `internal`로 바꿨다.
  - `T/CombatPlanTests.cs` 진입 시험 넷:
    - 요청을 등록기보다 먼저 만들었다.
    - `WithManager()` → `WithEntry(request)`(관리자 + `FakeEntry { Current = request }`)
    - 새 도우미 `RegisterEntry(register, request)`(진입 서비스와 같은 두 줄)
    - `RealRunner_…` 시험의 그룹에도 `FakeEntry`를 넣었다.
    - 중복 거절 단언은 그대로이고, 이제 Plan의 `Entry`에서 예외가 난다(TP-14).
- 호진님(Q-T2 허락):
  - `T/CombatEntryIntegrationTests.cs`: `StagePreparationObserver`의 전달 메서드 한 줄을 지웠다.
  - `T/MapCombatPresentationTests.cs`:
    - SetUp에 `_register.Entry = _service;`를 더했다.
    - 가짜 `Registration.Register`가 `(Combat, Entry)`일 때 `Calls++`와 `OnRegister?.Invoke(Entry.Current)`를 한다. 그 밖에는 그대로 `NotSupportedException`이다.
    - 가짜의 `RegisterCombatEntry`·`MarkQueued`를 지웠다.
    - 가짜 몸통 안에 설명 주석 한 줄을 더했다.
- 그대로 둔 것:
  - 다른 가짜 11개(공개 메서드라 컴파일됨, 진입을 요청하지 않음)
  - `Register_OnlyNextExitCheckAndNone_OtherTypesAreRejected`: 이제 `EntryTransition`·`Entry`도 받는데 이름이 그대로라 낡았다. 깨지지 않아 규칙대로 두었다.

**계획보다 조금 더 한 것:** `CombatEntryService.cs` 머리 요약, 등록 앞 주석 둘째 줄(조립 전제), 호진님 가짜 안 설명 주석 한 줄. 공유 메모(Kf.2 ②)에 함께 적는다.

**동작 차이(계획에 더함):**
- 진입 등록은 이제 "진입 서비스가 받은 등록기의 LifeCycle 그룹이 바로 그 서비스를 `CombatEntry`로 가진다"는 조립 전제에 기댄다.
  - 운영 조립(`LifeCycleSystemFactory.cs:50-51`, `JobRegisterSystem.cs:58`)에서는 늘 맞다. `EntryTransition`·`ExitCheck`도 이미 같은 전제다.
  - 그룹이 다른 서비스를 가리키거나 비어 있으면 Plan이 다른 요청을 읽거나 "진입 서비스가 없습니다" 예외를 낸다. 시험 두 파일(`CombatPlanTests`·`MapCombatPresentationTests`)을 고친 이유가 이것이다.
- 초기화 전 등록의 예외 형식(계획 K-b "동작 차이"에 이미 적음).

**새 에이전트 검토(읽기 전용, 1개): 높음 0·중간 0·낮음 6. 모두 반영하거나 기록했다.**
- `ICombatEntryService.Current` 요약 → "진입 서비스가 지금 맡은 요청(등록 직전부터 다음 요청이 대신할 때까지, 끝난 요청도 포함)".
- Plan `Entry` 요약 → "등록 때의 단계 표시(등록됨·등록 실패)".
- 진입 서비스 머리 요약 → "그 등록 결과(등록됨·등록 실패)를 요청에 표시", `<c>` → `<see cref>`.
- 조립 전제를 주석과 위 동작 차이에 적었다.
- 낡은 시험 이름은 기록만 했다.
- 계획보다 더 한 주석은 위에 적었다.
- 검토가 확인한 것:
  - `RegisterCombatEntry`를 부르거나 명시적으로 구현하는 곳이 남지 않았다(운영 코드·`Assets/` 안 문서 포함). 남은 곳은 K-f 대상 문서와 CLAUDE.md다.
  - `FakeEntry` 접근과 `TestLifeCycleGroup` 인자 순서가 맞다. CS0841은 해소됐다.
  - 운영에서 진입 서비스와 등록기 그룹은 같은 인스턴스다. Job 목록·순서·요청 단계가 같다.
  - `MarkQueued` 시점 변화는 관찰되지 않는다(`RequestEntryCore`가 Runner 빔을 요구하고, Runner는 Tick에서만 실행). 예외는 모두 `Fail` 뒤 다시 던진다.
  - `CombatPlanTests:48`은 Plan `Entry`에서 `InvalidOperationException`이 나고, 그 뒤 `:49`도 성립한다. `MapCombatPresentationTests` 단언도 모두 성립한다.
  - 새로 깨지는 시험이 없다. 바꾼 운영 줄은 모두 우리 줄이다. 호진님 시험은 허락 범위 안에서만 바뀌었다.
  - 바꾼 파일 열 개 모두 CRLF다.
- Unity 확인: 컴파일·EditMode 시험 문제 없음(2026-10-23 사용자).

### Phase K-c (K-3, Q-K3 B) — 2026-10-23, 코드 완료, **컴파일 미확인**(MSBuild 안 돌림, 새 `[Test]` 없음)

**바꾼 것(Kc.1~Kc.3):**
- `C/CombatEntryRequest.cs`: 질문 여섯을 `HasExited` 옆에 더했다(TP-5). `IsAwaitingRegistration`은 K-b에서 더했다.
  - `IsQueued`
  - `IsActivating`(`Activating ∧ IsJobCompleted`)
  - `IsActive`(요약에 진입 서비스의 `ICombatEntryRead.IsActive`와 다르다고 적음)
  - `IsCancelled`
  - `IsRejected`
  - `IsReplaceable`(`HasExited ∨ (거절 ∨ 취소) ∧ 배출`)
- `C/CombatEntryService.cs`:
  - `IsActive`(`:44`)
  - `Shutdown` 조건(`:122-123`, 호진님 줄, TP-7)
  - 거듭된 요청 확인(`:161`·`:163`)
  - `ActivateAccepted` 조건(`:212`)
  - 사본 `IsFinished`를 지웠다
  - 이 파일에 `CombatEntryStatus` 해석이 남지 않았다
- `V/Boot/GameBootstrap.cs:222·230`: `_bootEntry != null && _bootEntry.IsRejected`, `_bootEntry != null && (_bootEntry.IsActive || _bootEntry.IsCancelled)`(TP-6). 계획 글의 `?.IsRejected == true`와 꼴만 다르고 결과는 같다. 아래 힌트 문구는 K-e에서 다룬다.
- 전이 Job의 `switch (Status)`는 그대로 두었다(P3).

**고친 시험:** 없다. 새 멤버나 지운 `IsFinished`를 읽는 시험이 없다(검토 확인).

**계획과 다른 점:** 없다. 검토 반영으로 "끝난 요청" 규칙을 말로 적던 두 곳(계약 remarks, 서비스 remarks)이 `IsReplaceable`을 가리키게 했다.

**새 에이전트 검토(읽기 전용, 1개): 높음 0·중간 0·낮음 3. 모두 반영했다.**
- `IsActivating` 요약의 "Combat 전환을 마치고"는 틀렸다. 활성화는 전환 요청 직전에 시작되고 씬 로딩 중 내내 참이다. 그래서 "활성화가 시작돼(Combat 전환 요청 뒤, 씬 로딩 중 포함)…, 배출 조건은 확인을 겹칠 뿐"으로 고쳤다.
- `IsQueued` 요약에 서비스 정책(거듭된 요청)이 들어 있었다 → "등록됐고 아직 실행 전(실행 전 취소 가능)". "끝난 요청" 규칙이 말로 세 번 적혀 있던 것은 `IsReplaceable` cref로 한 곳이 됐다.
- 설계 K-3의 "끝남 판정 넷이 `IsReplaceable` 하나"는 틀렸다. 뜻이 다른 셋이 남으므로 설계 §9에 철회를 적었다. 점수는 Q-K3 B라 그대로다.
- 검토가 확인한 것:
  - 바꾼 식이 모두 같다(null 처리·우선순위·단락 평가, `!IsActivating`의 드모르간·`||` 사슬 위치 포함).
  - 이름 충돌·모호성이 없다(요청은 sealed·인터페이스 없음·확장 메서드 없음, 조정자는 `ICombatEntryRead`로 읽음). cref가 풀린다.
  - 우리 영역에 남은 `Status` 해석은 의도한 전이 Job뿐이다.
  - 시험 영향이 없다.
  - 호진님 줄은 TP-7 허용분(`:121-122`)뿐이고, 호진님 주석 `:120`은 그대로다.
  - Stage·StageMap은 무변경이고 `Status`는 public으로 남았다.
  - CRLF가 일관되고 `git diff --check`도 깨끗하다.
- Unity 확인: 컴파일·EditMode 시험 문제 없음(2026-10-23 사용자).

### Phase K-d (K-4) — 2026-10-23, 코드 완료, **컴파일 미확인**(MSBuild 안 돌림, 새 `[Test]` 없음)

**바꾼 것(Kd.1~Kd.4):**
- 새 `R/Common/SceneNodeOrder.cs`(+ `.meta`, GUID `ec8598060fb048d59aac25d715c4daed`, 저장소에서 유일)
  - `internal static class`이고, 같은 폴더 파일과 같게 CRLF·BOM 없음이다.
  - 기준 값은 숫자다: `LifeCycle` 0, `BindSceneUI` −200, `BindCameras` −190, `OpenSceneUI` 200.
  - 기준에 붙는 값은 식이다: `CombatBackdrop` = `BindCameras` − 5, `CombatViewAttach` = `LifeCycle` + 100, `PrepareCombatDeck` = `CombatViewAttach` − 10, `TutorialAttach` = `CombatViewAttach` + 10, `CombatViewRelease` = `LifeCycle` − 100, `TutorialDetach` = `CombatViewRelease` − 10.
  - 각 값의 이유는 옛 상수의 요약 주석을 옮겨 왔다.
- `V/Boot/GameBootstrap.cs`: 상수 넷과 머리 주석 두 줄을 지웠다. 노드 표 10행이 표를 읽고, 표 위 주석에 "순서 값의 원본은 SceneNodeOrder"를 더했다.
- `V/Combat/CombatViewSystem.cs`: 상수 셋과 주석을 지웠다. `Initialize` 등록 셋이 표를 읽고, 그 위에 원본 주석 한 줄을 달았다.
- `V/Tutorial/TutorialViewSystem.cs`(호진님 줄): 상수 둘과 주석을 지웠다. 등록 두 줄이 표를 읽고, 그 위에 "순서와 그 이유의 원본은 SceneNodeOrder다" 한 줄을 달았다.
- 값은 모두 HEAD와 같다(Bootstrap −200·−190·90·200, Combat −195·100·−100, 튜토리얼 110·−110). 등록 순서와 Enter·Exit 구분도 같다(검토 확인).

**고친 시험:**
- 우리 `T/CombatViewSystemTests.cs:197-199`: 지운 상수 읽기를 표로 바꿨다. `:198` 오른쪽 리터럴 `-190`은 그대로 두었다.
- 호진님(Q-T2 허락) `T/TutorialViewSystemTests.cs:34-35`: `SceneNodeOrder.CombatViewAttach`·`CombatViewRelease`. 메시지·단언은 그대로다. 같은 파일 `:9`의 `using …ViewSystem.Combat;`가 쓰이지 않게 됐지만 숨김 진단이고 허락 밖이라 두었다.

**계획보다 조금 더 한 것:** 호출 자리 주석 세 줄(Bootstrap 표 위, 조정자 `Initialize`, 튜토리얼 등록 위). 튜토리얼 줄은 호진님 함수 안이라 Kf.2 ①에 함께 적는다.

**새 에이전트 검토(읽기 전용, 1개): 높음 0·중간 0·낮음 4·정보 1.**
- 반영:
  - 표 요약이 "관계는 식으로, 한 값을 옮기면 따라 옮겨짐"이라고 넓게 말했다. 기준 값 넷은 숫자이고, 기준끼리의 관계(화면 열기는 View 연결 뒤 등)는 간격으로만 지켜진다. 그래서 그대로 적었다.
  - "Stage·Combat·Lobby 공통" → "세 씬, 덱 카드 준비는 Combat만"
  - Stage·Lobby 자기 노드 목록에 "해제"를 더했다.
  - 튜토리얼 호출 자리의 이유 사본을 지우고 원본 가리킴만 남겼다.
  - K-f 목록 보완: Kf.5에 −195 줄, Bootstrap "다섯" → "넷". Kf.2 ⑤ Bootstrap 상수 이전 알림. Kf.5b(민형님 Factory 공유의 `BackdropEnterPriority`, 배경 계획 BK-3 해소).
- **기록만 한 것(계획 범위 밖):** 주석과 예외 문구에 순서 숫자가 남아 있다. 지금 값으로는 맞지만 표 값을 바꾸면 틀어진다.
  - `CombatViewSystem.cs:82·382·385·415·655·875`
  - 실행 중 예외 문구 `:417` "씬 로드 Node(-195)…"
  - `Display/Map/StageViewPropPresenter.cs:19`
  - `Factory/Runtime/InGame/View/CombatSceneDisplayFactory.cs:32`(Factory 연결 줄 주석)
  - 고칠지는 사용자 결정 대기다.
- 정보: `TutorialViewSystemTests:9` 쓰이지 않는 using(위).
- 검토가 확인한 것:
  - 값·순서가 HEAD와 같고 안내 §1.1의 숫자와도 맞다.
  - 로딩 등록기는 Enter·Exit 모두 오름차순이다.
  - `LifeCycleBase`의 0과 일치한다.
  - "Enter(수락된 진입 활성화) 뒤", "해제 = Detach 뒤 배경 해제" 주석이 맞다.
  - 쓰는 다섯 파일 모두 `using …RuntimeSystem.Common`이 있다. 시험 어셈블리 접근도 된다(InternalsVisibleTo). 같은 이름 형식이 없다.
  - 지운 상수 아홉 개가 `Assets/` 전체에 남아 있지 않다.
  - `.meta` 형식이 같은 폴더 파일과 같다.
  - 다른 사람 줄은 계획에 적힌 곳뿐이다.
  - Stage·Lobby·Framework·`LifeCycleBase`는 무변경이다.
- Unity 확인: 컴파일·EditMode 시험 문제 없음(2026-10-23 사용자). 주석·예외 문구의 순서 숫자는 사용자 결정 A로 K-e의 Ke.12가 맡았다.

### Phase K-e (K-5 a·c~g + Ke.12) — 2026-10-24, 코드 완료, **컴파일 미확인**(MSBuild 안 돌림, 새 `[Test]` 없음)

**바꾼 것:**
- Ke.1 `C/Plans/CombatPlan.cs`: `IsAbandonment(completion)`(패배 ∧ 자발적 포기)을 `RequiresChapterCompletion` 옆에 더했다. 요약에 "결과 화면의 사유 판정 사본은 View 단계"를 적었다.
- Ke.2 `C/Jobs/CombatAbandonJob.cs`: `CombatPlan.IsAbandonment`를 부른다. 쓰지 않게 된 using 둘(Contracts.Combat·Common)을 지우고 `…Combat.Plans`를 더했다.
- Ke.3 `C/Jobs/CombatExitJobs.cs:79`: `if (!CombatPlan.IsAbandonment(completion)) RequireFinalResult(…)`. `using …Combat.Plans;`를 더했다.
- Ke.4 `C/Jobs/CombatCommandJob.cs`·`CombatPreviewJob.cs`·`CombatAdvanceJob.cs`: `Execute`의 늘 거짓인 null 검사를 지웠다.
- Ke.5 `C/CombatProgressReader.cs`: `ICombatProgressReader.ActiveCharacterCount`와 구현, `_characters`, `Bind`의 `characters` 인자, `PostBindService`의 넘김, `using …BufferSystems.Character;`를 지웠다(TP-11).
- Ke.6 `C/CombatEntryRequest.cs`: `IsInProgress`를 지웠다.
- Ke.7 `C/Jobs/CombatPrepareJob.cs`:
  - 새 생성자 `(IBattleEntryService battleEntry, IStageCommandProcessingService mapCommands, int priority, CombatEntryRequest entry)`.
  - 진입 종류에 필요한 서비스만 null 검사한다(TP-10).
  - `Execute`는 받은 서비스를 부르고, 그룹 null 검사는 지웠다.
  - using을 맞췄다(CombatSystem·StageCommandSystem 추가, InGameSystem 삭제).
  - Plan `RegisterEntry`는 `core?.BattleEntry`·`core?.StageCommands`를 넘긴다(`using …InGameSystem.CoreSystem;` 추가).
  - 다른 사람 줄: 천명님 `:43`, 호진님 `:45-53`.
- Ke.8 `CombatPlan.IsAwaitingPlayerCommand` 요약 → "이때 Decide는 명령 처리·자동 진행을 고르지 않습니다(미리보기 응답과, 승패·포기 요청이 있으면 그 단계는 고를 수 있습니다)".
- Ke.9 `V/Boot/GameBootstrap.cs:222-228`: 낡은 힌트를 지웠다. 로그는 Core 사유 둘이다. 이유를 주석 두 줄로 남겼다(TP-17).
- Ke.10 `GameBootstrap.ToLifeCycleKey`: Combat 갈래를 지우고 요약 한 줄을 달았다.
- Ke.12(사용자 결정 A): 순서 숫자를 `SceneNodeOrder` 이름으로 바꿨다. 모두 우리 줄이다.
  - `V/Combat/CombatViewSystem.cs`: 주석 `:82`, 영역 이름 `:382`, 요약 `:385-388`, 주석·예외 문구 `:415-417`, `:655-656`, `:875`
  - `V/Combat/Display/Map/StageViewPropPresenter.cs:19`
  - `Systems/Factory/Runtime/InGame/View/CombatSceneDisplayFactory.cs:32`(Factory 연결 파일의 우리 주석 한 줄)
  - XML 주석은 using이 있는 파일이면 `<see cref>`, 없으면 `<c>`를 썼다.

**고친 시험(모두 우리 줄):**
- `T/CombatProgressReaderTests.cs`:
  - `Bind` 여섯 곳(`:47`·`:146`·`:156`·`:158`·`:160`·`:164`)에서 넷째 인자를 뺐다.
  - `:162` 사례(넷째 인자 null)를 지웠다.
  - `ActiveCharacterCount_ReadsCharacterBuffer`를 지웠다(걷은 기능).
- `T/CombatProgressTestDriver.cs:28-29`: `Bind` 인자.
- `T/CombatProgressJobTests.cs`: 가짜 `Reader`의 `Characters`·`ActiveCharacterCount`를 지웠다.
- `T/CombatPlanTests.cs`:
  - `:92` → `new CombatPrepareJob(core, null, 0, request)`
  - `:176` → `(null, null, 0, request)`
  - `:177` → `(new CombatProbe(), null, 0, null)`
- 호진님 가짜 셋의 `ActiveCharacterCount`는 공개 속성이라 그대로 컴파일된다(P5).

**계획과 다른 점:**
- Ke.8 문구는 계획보다 하나 더 적었다("승패·포기 요청이 있으면 그 단계는 고를 수 있음"). `Decide`(`:107-140`)와 대조하면 더 정확하다.
- Ke.9는 이유 주석 두 줄을 남겼다.
- `CombatEndUI`와 `BattleInstanceId`는 그대로다(TP-16, Q-K5 B).

**TP-13 근거(검토로 보강):** 동작이 달라지는 조합(패배가 아닌 결과 + 자발적 포기)은 계약 형식이 만들 수 없게 막혀 있다. 그래서 실제로 달라지는 경우는 없다.
- `StageOutcomeSnapshot` 생성자는 자발적 포기를 Defeat에서만 받는다(`StageOutcomeSnapshot.cs:21-32`).
- `default`는 InProgress·None이다.
- `BattleCompletionChanges`는 `IsStageEnded`를 요구한다.

**동작 차이(TP-10 그대로):** Core 서비스가 없는 조립에서는 `RequestEntry` 자리에서 `ArgumentNullException("battleEntry"/"mapCommands")`가 난다. 같은 Plan의 다른 "서비스가 없습니다"는 `InvalidOperationException` 꼴이다. 운영 조립에서는 닿지 않고, 시험 `:176`이 이 예외를 단언한다.

**새 에이전트 검토(읽기 전용, 1개): 높음 0·중간 1·낮음 5·정보 1.**
- 중간: 호진님 10-22 메모(미전달) `:16`·`:70`이 지운 `IsInProgress`를 안내한다 → Kf.2 ⑥에 "바뀜" 표시를 더했다(같은 메모 `:14` "단계 Job을 Runner에 넘김"도 함께).
- 낮음:
  - Ke.8 문구 차이 → 위에 기록했다.
  - `IsAbandonment` 요약 → 결과 화면 사본은 View 단계라고 덧붙였다.
  - TP-13 근거 → 위에 기록했다.
  - `StageViewPropPresenter.cs:19` "씬 연결 수명" → "조정자의 씬 수명"(C-c 뒤 용어).
  - 지운 것을 적은 문서 둘(재구성 설계 `:252` `ActiveCharacterCount`, 10-09 인계 `:193` 힌트) → Kf.6에 더했다.
- 정보: 위 동작 차이.
- 검토가 확인한 것:
  - 바뀐 생성자·`Bind`·계약 멤버를 부르는 곳이 모두 맞는다. using이 모두 맞다. cref가 풀린다.
  - `CombatAbandonJob` 판정이 그대로다. 세 Job은 `BuildStep`의 생성자로만 만들어진다.
  - Bootstrap: `devBattleDefinition` 경고 없음, `ToLifeCycleKey` Combat이 닿지 않음, `_bootEntry`는 개발 경로만, 비전투 노드는 예외.
  - 새로 깨지는 시험이 없다.
  - Combat·Boot·Tutorial·Factory 쪽 순서 숫자가 남지 않았다(호진님 `StageLifeCycle`의 −100은 범위 밖).
  - 다른 사람 줄은 계획에 적은 둘뿐이다.
  - `CombatEndUI`·`BattleInstanceId`가 그대로다.
  - CRLF와 `git diff --check`가 깨끗하다.
- Unity 확인: 컴파일·EditMode 시험 문제 없음(2026-10-24 사용자).

### Phase K-f (문서·공유) — 2026-10-24, 문서 완료, 코드 변경 없음, **모두 미전달**

- Kf.1 민형님 `handoffs/2026-10-10-combat-job-plan-framework-share-minhyeong.md` 새 §7:
  - 연결 줄 셋(Combat 줄 Runner 인자 삭제, `RegisterCombatEntry` 삭제와 using, `JobType.Entry = 9`)과 Factory 주석 한 줄
  - Runner 쓰는 법이 바뀐 점
  - 앞 절(§6.1·§6.2·§6.4)의 바뀐 사실
- Kf.2 호진님 `handoffs/2026-10-22-combat-view-structure-hojin-share.md`:
  - 새 §9:
    - §9.1 흐름 변화
    - §9.2 바꾼 줄 넷
    - §9.3 고친 시험 셋과 손대지 않은 가짜
    - §9.4 Stage·StageMap 해석 12곳 → 질문 대응표와 새 질문 다섯의 식
    - §9.5 낡은 문서 둘
    - §9.6 앞 절 바뀐 사실
  - 낡은 줄 셋(`:14`·`:16`·`:70`)에 "바뀜" 표시
- Kf.3 현강님 `handoffs/2026-10-22-combat-view-structure-hyeongang-share.md` 새 §7: Bootstrap 노드 상수가 표로 옮겨짐, `CombatEndUI:150`은 View 단계. 현강님 코드·시험 변경은 없다.
- Kf.4 천명님 `handoffs/2026-09-24-view-core-data-boundary-handoff.md` 새 §32:
  - `CombatPrepareJob:43` 호출 자리
  - 기대는 Core 동작(자발적 포기는 패배일 때만, `AlreadyCompleted`는 닿지 않음)
  - 요청은 없다.
- Kf.5 `viewDocs/runtime-scene-binding-guide.md`:
  - `:70` 안내와 §1.1 제목·머리 문장(숫자 원본 = `SceneNodeOrder`)
  - "숫자의 원본" 칸: Combat 다섯·Bootstrap 넷, 아홉 줄(K-f 검토로 바로잡음) → `SceneNodeOrder.<이름>`과 식
  - 섞인 Exit −100·Enter 100 줄은 Combat과 Stage·Lobby로 나눴다(Stage·Lobby는 각 시스템 `DetachPriority`·`AttachPriority`).
- Kf.5b:
  - `handoffs/2026-10-15-combat-scene-display-factory-framework-share.md:280`에 "대신함: `SceneNodeOrder.CombatBackdrop`"
  - `plans/2026-10-23-combat-backdrop-before-camera-bind-plan.md:100` BK-3 해소
  - CLAUDE.md 배경 줄 BK-3 해소
- Kf.6 "대신함" 표시:
  - `specs/2026-10-22-combat-view-structure-improvement-design.md` §5.1 머리(K-1, 아래 발견 포함)
  - `specs/2026-10-10-combat-job-plan-restructure-design.md` 상태 줄 아래(K-2 진입 경로, K-5 d `ActiveCharacterCount`)
  - `handoffs/2026-10-09-combat-skeleton-view-programmer-handoff.md:193`(Bootstrap 힌트 지움)
- Kf.7 설계: 상태 줄 "구현 완료", §9에 K-1 이력과 실행 중 더한 것.
- Kf.8 CLAUDE.md: 상태 줄, 영역 규칙의 연결 줄 예시 `RegisterCombatEntry` → `Register(Combat, JobType.Entry)`(TP-18, 2026-10-24 사용자 허락).
- **K-f 중 찾은 것:** K-1은 10-22 개선 설계 2판의 "판단 뒤 Runner에 넘김"을 1판 꼴(Job 안 차례 실행)로 되돌린 것이다. 설계 때 이 이력을 놓쳤다.
  - 1판 지적 (b) 그룹 통째: 좁은 걸음 재료가 그대로라 풀려 있다.
  - 1판 지적 (c) 한 번에 안 끝날 때: P1 예외로 풀었다.
  - 1판 지적 (a) "Runner 연속 실행을 Job 안에서 다시 만든다": 남는 맞바꿈이다. 10-23 독립 재평가가 반대쪽을 FW·독립성 중간으로 봐서 이쪽을 골랐다.
  - 설계 §9와 10-22 설계 §5.1에 적었다.
- 줄 끝: 덧붙인 공유 문서·설계는 CRLF로 맞췄다. 섞인 파일은 없다(`2026-10-09-…-handoff.md`는 원래 LF 그대로).
- **새 에이전트 검토(읽기 전용, 1개): 높음 0·중간 6·낮음 10. 모두 반영했다.**
  - 중간:
    - 10-10 재구성 설계 "대신함"의 줄 번호를 "HEAD 기준"으로 밝혔다. 같은 문서의 K-1 대상 줄(`:86`·`:117`)도 더했다.
    - 민형님 §7.3에 낡은 줄 셋(§2 `:22`·`:28`, §6.5 둘째 줄)을 더했다. 상태 줄에 §7을 적었다.
    - 천명님 §32.3으로 §31.1 마지막 행(같은 Runner Tick·예산 단서)을 바뀐 사실로 적었다.
    - 호진님 §9.3의 `:300`을 HEAD `:297`로 고쳤다.
    - 배경 설계 머리에 "대신함(K-4)"을 달았다.
  - 낮음:
    - 호진님 §9.2에 `CombatViewSystem.cs` HEAD 303 행(호진님 주석, K-a가 다시 고침)을 더했다.
    - §9.5에 낡은 호진님 문서 둘을 더했다(튜토리얼 Buffer 계약 설계 `:62`, StageSelect 공유 `:132`).
    - §9.1·§9.6의 "같은 Runner Tick" 문구를 "지금도 한 Tick 안, 없어진 것은 Runner 큐·예산에 기댐"으로 좁혔다.
    - K-e 변경의 날짜를 10-24로 바로잡았다.
    - 민형님 §7.1에 줄 번호 기준과 호진님 가짜 등록기 재작성을 적었다.
    - §9.3에 가짜의 새 필드를 적었다.
    - 현강님 §2 줄에 "바뀜" 표시를 달았다.
    - 안내 갱신 기록에 10-23~24 줄을 더했다.
    - 이 기록의 "일곱 줄"을 "아홉 줄"로 고쳤다.
    - CLAUDE.md·설계 제목의 "코드 변경 없음"·"권장안 가정, 코드 착수 전"을 지금 상태로 고쳤다.
    - 조정자 현황 정리 `:162`에도 "바뀜"을 달았다.
  - 적용 중 일어난 일: 고침 스크립트를 두 번 돌리는 사이 한 파일 쓰기가 잠깐 막혀, 몇 줄이 두 번 들어갔다(민형님 §7.3 세 줄, 천명님 §32.3, 호진님 303 행·문서 두 줄). 같은 내용인지 확인한 뒤 두 번째 것만 지웠고, 각 고침이 한 번씩만 있는 것을 다시 확인했다.
  - 검토가 확인한 것:
    - 주인 판정이 모두 맞다.
    - 새 멤버 이름·값·식이 코드와 맞다.
    - 공유 범위에 빠진 다른 사람 줄이 없다(303 주석 하나는 위에서 더함).
    - 호진님 §9.4 표의 해석과 질문 식이 모두 같은 뜻이다.
    - 안내 §1.1 모든 행의 값·원본이 맞고 빠진 행이 없다.
    - CLAUDE.md 세 곳이 바르게 읽힌다.
    - 줄 끝이 한결같다.
    - K-f 동안 코드 파일 변경이 없다.
