# 전투 골격 점검 설계 (2026-10-23)

- **상태:** 구현 완료(2026-10-24). 결정은 2026-10-23 사용자가 권장안대로 했다(Q-K1·K2·K4·T1·T2 A, Q-K3·K5·K6 B). Phase K-a~K-e 코드는 Unity 확인 문제 없음(사용자), K-f 문서·공유는 모두 미전달이다. 실행 기록은 [계획](../plans/2026-10-23-combat-skeleton-check-plan.md) "실행 기록"에 있다. 커밋은 사용자가 한다. Task 계획 [skeleton-check-plan](../plans/2026-10-23-combat-skeleton-check-plan.md)은 권장안을 가정해 먼저 썼고, 하위 에이전트 2개로 검증했다. 계획 대조로 바로잡은 것은 §9에 있다(Q-K5 권장 바뀜, Q-K2가 10-22 결정을 뒤집음, K-5 a 범위). Phase마다 새 에이전트로 검토한 뒤 멈춘다.
- **기준:** 작업 트리. C 단계(조정자, 커밋 전)를 포함하고, HEAD는 `4c328d07`이다. 줄 번호는 지금 작업 트리 기준이다.
- **입력:** 2026-10-23 C 단계 뒤 골격 구조 독립 재평가 **7.5/10**(채팅 보고). 기준별 점수는 SSOT 7.5, SOLID 7.5, FW 7.5, 있는 기능 9.5, 독립성 7.0, 불필요/우회 6.25다. 평가 문제 번호(#1~#11, 낮음)는 그 보고의 번호다.
- **약어:**
  - `C/` = `…/LifeCycleSystem/LifeCycles/Combat/`
  - `V/` = `…/InGameSystem/ViewSystem/`
  - `G/` = `…/GlobalSystem/`
  - `T/` = `Assets/Tests/EditMode/Framework/`
- **지킬 것:**
  - MSBuild를 돌리지 않고, 새 `[Test]`를 만들지 않는다(보고에 "컴파일 미확인").
  - 커밋하지 않는다.
  - 범위는 전투 골격·부트·튜토리얼 전투 접점·공용 오류 경계·연결 줄이다. Framework·GlobalSystem은 기능·계약을 바꾸지 않고 **우리 입구·우리 열거 값·우리 연결 줄**만 바꾼다(바꾸면 민형님 공유 문서에 적음).
  - Stage·StageMap·Lobby는 고치지 않는다(사용자 지시가 있을 때만).

## 0. 목표와 범위

**골격(전투 진행 Plan·Job·진입 서비스)이 숨은 가정과 우회 없이 돌게 한다.** 평가의 골격 쪽 중간 넷을 푼다.
1. **실행 순서 가정을 코드로 만든다**(#6 FW, #8 독립성). 단계 Job이 "Runner에서 이 Job 바로 뒤에 이어 실행된다"에 기대지 않게 한다.
2. **공용 등록기에서 전투 전용 입구를 걷는다**(#5 SOLID). 진입도 다른 루트처럼 `Register(루트, 종류)` 하나로 들어온다.
3. **진입 요청의 단계 값을 이름 있는 질문으로 바꾼다**(#10 독립성). 소비자가 단계 조합을 직접 해석하지 않게 한다.
4. **씬 Node 순서를 표 하나에 모은다**(#9 독립성). 순서 사이의 관계를 식으로 적는다.
5. 같은 범위의 낮음을 정리한다(§2 K-5).

**원칙은 동작 동일이다.** 바뀌는 동작은 항목마다 "동작" 줄에 적었다.

**범위 밖(다음 단계 또는 보류, §8):**
- View·조정자 단계: #2 제출 정지 깃발, #3 결과 화면의 Run 결과 해석, #4 조정자 분리, #7 확인창 캐스트, #11 HUD 완료 경로
- H1(#1, 사용자 결정으로 미룸)
- 기반 한계(Runner 완료 신호 없음 등)

## 1. 한눈에

| # | 문제(평가) | 바꾸는 것 | 동작 | 다른 사람 줄·시험 |
|---|---|---|---|---|
| **K-1** | #6 Job 안에서 Runner 재등록, #8 같은 Tick 연속 가정 | `CombatNextJob`이 단계 Job을 Runner에 넣지 않고 자기 `Execute` 안에서 차례로 실행. Plan·연결 줄이 Runner를 받지 않음 | 같음(순서·호출 수) | 연결 줄 1(민형님 공유), 우리 시험 호출 맞춤 |
| **K-2** | #5 공용 등록기에 `RegisterCombatEntry` | 진입은 `Register(Combat, JobType.Entry)`. Plan이 현재 요청을 읽음. 단계 표시(`MarkQueued`·`Fail`)는 진입 서비스가 함 | 같음 | 연결 줄(계약 멤버 삭제·열거 값 하나), 호진님 시험 2곳 |
| **K-3** | #10 다른 영역이 `Status`를 직접 해석 | `CombatEntryRequest`에 이름 있는 질문. 우리 소비자를 옮김(Stage·StageMap은 Q-K3) | 같음 | 호진님 줄(우리 파일 안 2줄 / Q-K3 A면 Stage·StageMap) |
| **K-4** | #9 씬 Node 숫자가 파일 넷에 | 순서 표 `SceneNodeOrder` 하나, 관계는 식으로 | 같음(값 그대로) | 호진님 줄(튜토리얼 상수 2줄) |
| **K-5** | 골격 쪽 낮음 | 판정 함수 모으기·쓰지 않는 멤버·늘 거짓 검사·닿지 않는 분기·주석·Job 의존 좁히기 | 같음 | 우리 시험 맞춤 |

## 2. 항목별 설계

### K-1 단계 Job을 `CombatNextJob` 안에서 바로 실행 (#6·#8)

**지금.**
- `CombatNextJob.Execute`가 `_runner.RegisterRange(CombatPlan.BuildStep(_context))`로 단계 Job을 Runner에 다시 넣는다(`C/Jobs/CombatNextJob.cs:36`).
- 그래서 Plan이 Runner를 받고(`C/Plans/CombatPlan.cs:48-49`), 연결 줄도 Runner를 넘긴다(`G/JobSystem/JobRegister/JobRegisterSystem.cs:71`).
- 판단 때의 사실을 단계 Job이 다시 증명하지 않는 근거는 주석에만 있다(`CombatNextJob.cs:11-14`, `C/Plans/CombatStep.cs:4-8`): "넘긴 Job은 같은 Runner Tick에 이 Job 바로 뒤에 이어 실행되고, 그 사이 다른 모듈은 돌지 않는다".
- 이 근거가 서는 조건은 셋이고, 모두 코드 밖에 있다.
  - Runner가 같은 우선순위를 FIFO로 실행하고 한 Tick 예산(32) 안에서 끝낸다.
  - 다른 루트가 그 사이에 등록하지 않는다.
  - 예산이 정리 연쇄 중간에서 끊기지 않는다.
- View의 F1 분기(`V/Combat/CombatViewSystem.cs:309-321`)도 "정리와 완료 표시가 같은 Runner Tick에 이어진다"에 기댄다.

**바꾼 뒤.** 실행 순간 판단과 그 단계의 실행을 한 `Execute` 안에 둔다. 이어 실행이 코드로 보장된다.

```diff
 internal struct CombatNextJob : IJobService
 {
     private readonly CombatStepContext _context;
-    private readonly IJobRunner _runner;
     …
-    internal CombatNextJob(CombatStepContext context, IJobRunner runner, int priority)
+    internal CombatNextJob(CombatStepContext context, int priority)
     …
     public void Execute()
     {
         if (IsCompleted)
             return;

-        _runner.RegisterRange(CombatPlan.BuildStep(_context));
+        // 판단(BuildStep)과 그 단계의 실행을 한 호출 안에서 한다. 그 사이에 다른 Job·모듈이 돌지 않으므로
+        // 단계 Job은 판단 때의 사실을 다시 증명하지 않는다. 단계 Job은 한 번 실행으로 끝나야 한다(CombatStepKind 계약).
+        List<IJobService> steps = CombatPlan.BuildStep(_context);
+        for (int i = 0; i < steps.Count; i++)
+        {
+            IJobService step = steps[i];
+            step.Execute();
+            if (!step.IsCompleted)
+                throw new InvalidOperationException($"[CombatNextJob] 단계 Job이 한 번 실행으로 끝나지 않았습니다({step.GetType().Name}).");
+        }
         IsCompleted = true;
     }
 }
```

- 단계 Job은 struct다. 목록에는 상자(box)로 들어 있으므로, 같은 참조로 `Execute`와 `IsCompleted`를 읽으면 된다.
- `CombatPlan.Register(service, jobType, lifeCycles)`는 Runner 인자를 지운다. `Next`도 같다.
- 연결 줄은 `CombatPlan.Register(service, jobType, _lifeCycles)`로 바뀐다. `JobRegisterSystem`의 `_runner`는 다른 루트가 계속 쓴다.
- 주석을 고친다.
  - `CombatStep.cs`, `CombatNextJob`, Plan 머리: "같은 Runner Tick" → "한 Job 안".
  - View F1 분기 주석(`CombatViewSystem.cs:309-311`): "정리와 완료 표시는 한 Job 안에서 이어진다"
  - 정지 Job 시점 주석(`:923-924`)
- `CombatAbandonJob`의 `AlreadyCompleted` 갈래(`C/Jobs/CombatAbandonJob.cs:46-49`)는 이제 닿지 않는다. Decide는 완료가 없을 때만 포기를 고르고, 같은 호출 안에서 실행하기 때문이다. 그래서 그 갈래를 "받을 수 없는 결과" 예외(default)에 합친다(K-5와 같이 한다).

**동작:** 같다.
- 단계 Job의 순서, Core 호출 횟수, 예외(→ F1)는 바뀌지 않는다.
- 다른 점은 Runner 예산을 단계 Job 수만큼 쓰지 않는다는 것뿐이다(정리 연쇄 넷이 슬롯 하나).

**시험(우리 줄):**
- `CombatPlanTests:147·160·166`, `CombatProgressJobTests:427·438·450`: `CombatPlan.Register(…, runner)` 호출에서 Runner 인자를 뺀다.
- `CombatProgressTestDriver`: Runner를 넘기는 곳이 있으면 맞춘다.
- `BuildStep_Settle_BuildsTheChainInOrder_AndTheRealRunnerFinishesItInOneTick`은 `BuildStep` 목록을 Runner로 돌리는 시험이다. 확인 내용("정리 연쇄 순서")은 그대로 성립하므로 그대로 둔다(계획에서 다시 확인).

### K-2 진입 등록을 공용 `Register` 경로로 (#5)

**지금.**
- 공용 등록기 계약 `IJobRegisterSystem`에 전투 전용 `RegisterCombatEntry(CombatEntryRequest)`가 있다(`JobRegisterSystem.cs:24`).
- 구현은 Plan을 부른 뒤 요청의 단계를 바꾼다(`:88-106`의 `MarkQueued`, 실패 때 `Fail`).
- 운영에서 부르는 곳은 우리 `CombatEntryService.cs:183` 하나다.
- 시험 가짜 14개가 이 메서드를 구현한다(호진님 13, 우리 1).

**바꾼 뒤.**
- 진입도 다른 루트처럼 `Register(JobRootType.Combat, JobType.Entry)` 하나로 들어온다. 이미 있는 `EntryTransition`과 같은 꼴이다.
  - `JobType.Entry = 9`를 더한다(전투 전용 값, 연결 줄).
  - Plan은 진입 서비스의 현재 요청을 읽어 [준비 → 전환]을 만든다.
- 요청 단계 표시는 그 주인인 진입 서비스가 한다.

```diff
 // G/JobSystem/JobRegister/JobRootType.cs (연결 줄)
     EntryTransition = 8,
+    /// <summary>전투 전용: 진입 서비스의 현재 요청으로 준비·전환을 등록한다.</summary>
+    Entry = 9,

 // G/JobSystem/JobRegister/JobRegisterSystem.cs (연결 줄)
 public interface IJobRegisterSystem : IServiceModule
 {
     void Register(JobRootType rootType, JobType jobType);
-    void RegisterCombatEntry(CombatEntryRequest request);
 }
-public void RegisterCombatEntry(CombatEntryRequest request) { … MarkQueued … Fail … }

 // C/Plans/CombatPlan.cs
+case JobType.Entry:
+    return Entry(service, lifeCycles);
+
+/// <summary>진입 서비스가 막 받은 현재 요청으로 준비 → 전환을 만듭니다(설계 J5). 요청 단계 표시는 진입 서비스가 합니다.</summary>
+private static List<IJobService> Entry(IInGameSystemGroupService service, ILifeCycleSystemGroupService lifeCycles)
+{
+    CombatEntryRequest request = RequireEntry(lifeCycles).Current;
+    if (request == null || !request.IsAwaitingRegistration)
+        throw new InvalidOperationException("CombatPlan: 등록을 기다리는 전투 진입 요청이 없습니다.");
+    return RegisterEntry(service, request, lifeCycles);   // 지금 함수 그대로
+}

 // C/CombatEntryService.cs
 _current = request;
 try
 {
-    _register.RegisterCombatEntry(request);
+    _register.Register(JobRootType.Combat, JobType.Entry);
+    request.MarkQueued();
 }
 catch
 {
     request.Fail();
     throw;
 }
```

- `IsAwaitingRegistration`(= `Created`)은 K-3의 질문이다.
- `MarkQueued`를 부르는 시점이 "Plan이 Job을 만든 뒤, Runner에 넣기 전"에서 "Runner에 넣은 뒤"로 바뀐다. Runner는 Tick에만 실행하므로, 그 사이에 요청을 읽는 곳은 없다. Runner에 넣다가 실패하면 지금은 `Queued → Fail`, 바뀐 뒤는 `Created → Fail`이다. `Fail`은 끝나지 않은 모든 단계에서 받으므로 결과는 같다.
- `MarkQueued`·`Fail`은 internal이다. 같은 어셈블리의 Global이 부르던 것을 우리 진입 서비스가 부르게 된다(요청 단계의 주인이 한 곳이 됨).

**동작:** 같다(Job 목록, 순서, 요청 단계).

**시험(Q-T1·Q-T2):**
- 우리 `CombatProgressTestDriver.cs:62`: 감싼 등록기의 `RegisterCombatEntry` 전달 메서드를 지운다(컴파일).
- 호진님 `CombatEntryIntegrationTests.cs:962`: 감싼 등록기의 전달 메서드 한 줄을 지운다(`_inner.RegisterCombatEntry`가 컴파일되지 않음).
- 호진님 `MapCombatPresentationTests.cs:268-276`: 가짜의 `RegisterCombatEntry` 몸통(`Calls++`·`OnRegister`·`MarkQueued`)을 `Register(Combat, Entry)` 갈래로 옮긴다. 지금 가짜의 `Register`는 `NotSupportedException`이라 실행에서 깨지기 때문이다. `MarkQueued`는 이제 서비스가 하므로 가짜에서 뺀다.
- 그 밖의 호진님 가짜 11개(`=> throw new NotSupportedException()`)는 계약에서 빠진 메서드가 공개 메서드로 남아도 컴파일된다(P5). 그대로 둔다.

### K-3 진입 요청 단계를 이름 있는 질문으로 (#10)

**지금.**
- `CombatEntryRequest.Status`가 public이다. 계약 주석은 "소비자는 단계 값을 해석하는 대신 이름 있는 질문을 읽습니다"(`C/CombatEntryRequest.cs:61-62`)라고 하지만, 질문이 모자라 소비자가 단계 조합을 직접 해석한다.

| 소비자 | 줄 주인 | 지금 해석 |
|---|---|---|
| `V/Boot/GameBootstrap.cs:222·230` | 우리 | `Rejected`, `Active ∨ Cancelled` |
| `C/CombatEntryService.cs:121-122` | 호진님 | `Closed ∨ Cancelled ∨ Rejected`(취소 실패 뒤) |
| `C/CombatEntryService.cs:43·162·207·237` | 우리 | `Active`, `Queued`, `Activating`, "끝남"(`HasExited ∨ (Rejected ∨ Cancelled) ∧ 배출`) |
| `C/Jobs/CombatEntryTransitionJob.cs:38-41` | 우리 | 전이 처리(요청과 같은 골격 안) |
| `LifeCycles/Stage/StageLifeCycle.cs:169-170·183·192` | 호진님 | `Created ∨ Cancelled ∨ Rejected`, `Activating ∧ 배출 ∧ 수락`, `Created ∨ Queued` |
| `LifeCycles/Stage/StageNodeEntryFlow.cs:56·65·71·73-74·77` | 호진님 | `≠ Created`, `Cancelled`, `Closed ∨ Faulted`, `Created ∨ Queued ∨ Running ∨ 미배출`, `Active ∨ Rejected` |
| `V/StageMap/StageMapEntryObserver.cs:42·49·80` | 호진님 | `Prepared ∧ 배출`, `Cancelled`, `Active ∨ Rejected` |
| `V/StageMap/StageMapNodeMovement.cs:59` | 호진님 | `Prepared ∧ 배출 ∧ 수락` |

**바꾼 뒤.** `CombatEntryRequest`에 질문을 더한다(우리 파일). 단계 조합의 뜻은 요청 한 곳에 있다.

| 질문 | 뜻 | 쓰는 곳 |
|---|---|---|
| `IsAwaitingRegistration` | `Created` | K-2 Plan, StageLifeCycle·StageNodeEntryFlow |
| `IsWaitingForJob` | `Created ∨ Queued ∨ Running ∨ !IsJobCompleted` | StageNodeEntryFlow |
| `CanCancelBeforeRun` | `Created ∨ Queued` | StageLifeCycle 취소 |
| `IsPrepared` | `Prepared ∧ IsJobCompleted`(준비 단계는 수락 뒤에만 옴) | StageMapEntryObserver·StageMapNodeMovement |
| `IsActivating` | `Activating ∧ IsJobCompleted` | 진입 서비스 활성화, StageLifeCycle 전환 확인 |
| `IsActive`·`IsCancelled`·`IsRejected` | 단계 하나 | Bootstrap·진입 서비스·StageMap |
| `IsEntryDecided` | `Active ∨ Rejected`(진입 여부가 정해짐) | StageNodeEntryFlow·StageMapEntryObserver |
| `HasEndedUnexpectedly` | `Closed ∨ Faulted` | StageNodeEntryFlow |
| `IsReplaceable` | `HasExited ∨ (Rejected ∨ Cancelled) ∧ IsJobCompleted` | 진입 서비스 `IsFinished`(사본 삭제), Bootstrap 로그 비우기 |

- 이름은 계획에서 소비자와 하나씩 맞춰 정한다.
- 평가의 낮음 "요청 끝남 판정 넷"도 여기서 `IsReplaceable` 하나가 된다(진입 서비스 `IsFinished`·`:121-122`, Bootstrap, `IsInProgress` 삭제).
- **우리 소비자(Bootstrap·진입 서비스)는 이번에 옮긴다.** 전이 Job은 요청과 같은 골격 안의 전이 처리라 단계 값 그대로 둔다.
- `Status`를 internal로 바꿀지는 Stage·StageMap 소비자를 옮기는지에 달렸다(Q-K3).

**동작:** 같다(식만 옮김).

**다른 사람 줄:**
- 우리 파일 안 호진님 줄 `CombatEntryService.cs:121-122`(취소 실패 뒤 판정)를 질문으로 바꾼다.
- Q-K3 A면 Stage·StageMap 호진님 줄 14줄(파일 넷)을 바꾼다.

### K-4 씬 Node 순서를 표 하나로 (#9)

**지금.** Combat 씬의 Loading Node 순서 숫자가 파일 넷에 흩어져 있다. 관계("맵 카메라 수집 전", "View 첫 그리기 전")는 주석과 안내 문서에만 있다.

| 숫자 | 하는 일 | 자리 |
|---|---|---|
| −200 | 씬 UI 다시 연결 | `V/Boot/GameBootstrap.cs:288` |
| −195 | 씬 루트 찾기·배경 세우기(카메라 수집 전) | `V/Combat/CombatViewSystem.cs:78` |
| −190 | 카메라 다시 연결 | `GameBootstrap.cs:290` |
| 0 | LifeCycle Enter·Exit | `LifeCycleSystem/Base/LifeCycleBase.cs:56·58`(기반) |
| 90 | 덱 카드 준비(View 첫 Render 전) | `GameBootstrap.cs:292` |
| 100 / −100 | 전투 View 연결 / 씬 이탈 | `CombatViewSystem.cs:74-75` |
| 110 / −110 | 튜토리얼 연결·해제(View ±10) | `V/Tutorial/TutorialViewSystem.cs:52-53`(호진님) |
| 200 | 씬 화면 열기 | `GameBootstrap.cs:294` |

**바꾼 뒤.** 순서 표 하나(위치 Q-K4). 관계를 식으로 적어, 한 값을 옮기면 기대는 값도 따라 옮겨진다.

```csharp
namespace ProjectL.Systems.RuntimeSystem.Common
{
    /// <summary>
    /// 씬 Loading Node 순서의 원본입니다(Enter는 작은 값부터, Exit도 작은 값부터). 관계는 식으로 적습니다.
    /// LifeCycle(0)은 기반(LifeCycleBase)이 정한 값을 따라 적은 것입니다.
    /// </summary>
    internal static class SceneNodeOrder
    {
        internal const int LifeCycle = 0;

        // Enter
        internal const int BindSceneUI = -200;                    // LifeCycle·View 연결 전에 UI를 다시 잇는다
        internal const int BindCameras = -190;
        internal const int CombatBackdrop = BindCameras - 5;      // 카메라 수집에 맵 카메라가 들어가게
        internal const int CombatViewAttach = LifeCycle + 100;    // LifeCycle 활성화 뒤
        internal const int PrepareCombatDeck = CombatViewAttach - 10;  // View 첫 Render 전
        internal const int TutorialAttach = CombatViewAttach + 10;     // 전투 View 연결 뒤
        internal const int OpenSceneUI = 200;                     // 진입·연결이 끝난 뒤

        // Exit
        internal const int CombatViewRelease = LifeCycle - 100;   // LifeCycle Exit 전
        internal const int TutorialDetach = CombatViewRelease - 10;
    }
}
```

- 값은 지금과 같다(−200, −195, −190, 90, 100, 110, 200, −100, −110).
- 쓰는 곳 셋(Bootstrap 표, 조정자 `Initialize`, 튜토리얼 `Initialize`)이 표를 읽는다.
- 조정자·튜토리얼의 상수는 지운다. 시험이 그 상수를 읽으면 표로 맞춘다(계획에서 목록).
- 씬 연결 안내(`viewDocs/runtime-scene-binding-guide.md` §1.1)의 순서 표는 "원본은 코드 `SceneNodeOrder`"로 바꾼다.

**동작:** 같다(같은 값).

### K-5 골격 쪽 작은 정리 (낮음)

| # | 무엇 | 바꾸는 것 | 시험 |
|---|---|---|---|
| a | 포기 완료 판정 셋(`CombatAbandonJob.cs:42-43`·`CombatExitJobs.cs:79` 우리, `G/UISystem/Screens/Stage/CombatEndUI.cs:150` 현강님) | `CombatPlan.IsAbandonment(completion)` 하나(같은 꼴의 `RequiresChapterCompletion` 옆). **골격 두 곳만**이다. `CombatEndUI`는 View 단계 #3으로 넘긴다(§9) | 없음 |
| b | `CombatAbandonJob`의 닿지 않는 `AlreadyCompleted` 갈래 | K-1 뒤 예외 갈래에 합침 | 그 갈래를 보던 우리 시험이 있으면 지움 |
| c | Job struct의 늘 거짓 null 검사 넷(`CombatCommandJob.cs:38`, `CombatPreviewJob.cs:41`, `CombatAdvanceJob.cs:42`, `CombatPrepareJob.cs:33`) | 생성자가 이미 `ArgumentNullException`이므로 지움 | 없음 |
| d | 쓰지 않는 멤버 `ICombatProgressReader.ActiveCharacterCount`, `CombatEntryRequest.IsInProgress` | 지움(운영 사용 0). `CombatProgressFacts.BattleInstanceId`는 Q-K5 | 우리 `CombatProgressReaderTests.ActiveCharacterCount_ReadsCharacterBuffer` 삭제. 호진님 가짜 셋의 같은 이름 속성은 남아도 컴파일됨 |
| e | `CombatPrepareJob`만 InGame 그룹 전체를 받음(FW-101) | Core 진입 서비스 둘(`BattleEntry`·`StageCommands`)만 받음. 진입 종류에 필요한 것만 null 검사 | 우리 `CombatPlanTests:92·174·175`가 직접 만든다(§9 정정). 생성자 호출만 맞춤 |
| f | `IsAwaitingPlayerCommand` 주석 "Decide가 이때 할 일을 고르지 않습니다"가 틀림(미리보기를 고를 수 있음) | 주석 고침 | 없음 |
| g | Bootstrap의 낡은 거절 힌트("전투 노드인지 확인"), 닿지 않는 `ToLifeCycleKey` Combat 갈래 | 힌트를 지금 동작(비전투 노드는 예외, 개발 진입은 전투 노드로 보정)으로, 갈래는 정리 | 없음 |

## 3. 정할 것

권장은 A다. 단 Q-K3·Q-K5·Q-K6은 B를 권장한다(Q-K5는 계획 대조 뒤 바꿈, §9).

| # | 묻는 것 | A | B | C |
|---|---|---|---|---|
| **Q-K1** | K-1 | **권장:** `CombatNextJob`이 단계 Job을 자기 안에서 차례로 실행. Plan·연결 줄에서 Runner 인자 삭제 | 지금처럼 Runner에 넣되 단계 Job에 우선순위를 줘 앞에 오게 함(Runner 우선순위 기능, 다른 루트가 그 사이에 등록하는 경우는 남음) | 그대로(주석만) |
| **Q-K2** | K-2 | **권장:** `JobType.Entry` + Plan이 현재 요청을 읽음, `RegisterCombatEntry` 삭제(연결 줄 둘, 민형님 공유). 요청 단계 표시는 진입 서비스. **10-22 결정 4-③("`MarkQueued`·`Fail`은 그대로")을 뒤집는다**(§9) | `RegisterCombatEntry`는 두되 `MarkQueued`·`Fail`만 진입 서비스로 옮김(공용 계약의 전투 입구는 남음, 이것도 4-③을 뒤집음) | 그대로(4-③ 유지) |
| **Q-K3** | K-3 범위 | 질문을 더하고 우리 소비자와 **Stage·StageMap 소비자(호진님 줄 14줄, 파일 넷)까지** 옮긴 뒤 `Status`를 internal로. 사용자 지시가 필요하고, 호진님 작업과 겹칠 수 있음 | **권장:** 질문을 더하고 우리 소비자만 옮김. Stage·StageMap에는 같은 질문으로 바꾸자는 제안을 호진님 메모에 적음(`Status`는 public 유지) | 보류 |
| **Q-K4** | K-4 표 위치 | **권장:** `RuntimeSystem/Common/SceneNodeOrder.cs`(오류 경계·`CleanupErrors`와 같은 공용 자리, 우리 파일만 있음, Boot·Combat·Tutorial이 이미 이 네임스페이스를 씀) | `V/Boot/`(Combat·Tutorial이 Boot를 참조하게 됨) | 그대로 |
| **Q-K5** | K-5 범위 | a~g 모두. `CombatProgressFacts.BattleInstanceId`(쓰지 않는 값)도 지움. 호진님 `TutorialPlanTests:177`(순서 인자) 한 줄과 우리 시험 다섯을 고쳐야 함(§9) | **권장(계획 대조 뒤 바꿈):** a~g만, `BattleInstanceId`는 둠(지워도 점수 변화 없음) | 이번엔 안 함 |
| **Q-K6** | 튜토리얼 사실 읽기를 골격 사실 읽기로 합칠지(평가 낮음 "사실 읽는 길 셋", 호진님 파일 `TutorialFactsReader` 전체) | 이번에 합침(튜토리얼 Plan 원천에 골격 사실 읽기를 넘김, 호진님 줄 다수) | **권장:** 보류. SSOT 낮음은 다른 낮음으로도 상한(−1.0)이라 점수 변화가 없고, 바뀌는 호진님 줄이 많음. 다음 단계에서 다시 봄 | — |
| **Q-T1** | 우리 시험 처리 | **권장:** C 단계와 같게(걷은 기능·바뀐 동작은 지우고, 남는 내용은 호출만 맞춤) | 모두 고쳐 남김 | — |
| **Q-T2** | 다른 사람 시험 줄 | **권장:** 고침 허락. 호진님 `CombatEntryIntegrationTests.cs:962`(1줄 삭제), `MapCombatPresentationTests.cs:268-276`(가짜 몸통을 `Register` 갈래로 옮김, SetUp 한 줄), `TutorialViewSystemTests.cs:34-35`(K-4 상수 읽기 두 줄, §9에서 더함) | 고치지 않음 → K-2를 Q-K2 B로 바꾸고, K-4는 조정자에 별칭 상수 둘을 남김 | — |

## 4. 설계에서 정한 것 (확인 대상)

- **P1** 단계 Job은 "한 번 실행으로 끝남"이 계약이다(`CombatStepKind` remarks). K-1 뒤에는 끝나지 않으면 예외(F1)로 드러난다. 지금은 Runner가 다음 Tick에 다시 돌려 조용히 이어졌다.
- **P2** K-2의 `MarkQueued`는 Runner 등록이 성공한 뒤에 부른다. 실패 때 `Fail`은 지금처럼 진입 서비스의 catch가 부른다.
- **P3** K-3의 질문은 요청 파일 한 곳에 두고, 단계 전이 표(`CanMove`)는 바꾸지 않는다. 전이 Job의 `switch (Status)`는 전이 처리라 그대로 둔다.
- **P4** K-4의 LifeCycle 값 0은 기반이 정한 값을 표에 따라 적는다. `LifeCycleBase`(민형님 기반)는 바꾸지 않는다. 기반 값이 바뀌면 표 주석이 틀어진다는 점을 표 주석에 적는다.
- **P5** 계약에서 빠진 멤버를 시험 가짜가 공개 메서드·속성으로 남겨도 컴파일된다. 명시적 구현은 없음을 확인했다. 깨지지 않는 가짜는 손대지 않는다(C 단계 P8과 같음).
- **P6** 민형님 공유: 연결 줄 셋을 바꾼다. 10-10 Job·Plan 공유 문서에 새 절로 적는다.
  - `JobRegisterSystem`의 Combat Plan 줄(Runner 인자 삭제)
  - `IJobRegisterSystem.RegisterCombatEntry` 삭제
  - `JobType.Entry = 9` 추가

## 5. Phase 초안

Phase마다 새 에이전트로 검토하고, 반영한 뒤 멈춰서 보고한다. 컴파일 미확인으로 보고하고, 사용자가 Unity에서 확인한다.

| Phase | 항목 | 바뀌는 파일 | 시험 | 누적 예상 |
|---|---|---|---|---|
| **K-a** | K-1 + K-5 b | `CombatNextJob.cs`, `CombatPlan.cs`, `CombatStep.cs`(주석), `JobRegisterSystem.cs`(연결 줄), `CombatAbandonJob.cs`, `CombatViewSystem.cs`(주석) | `CombatPlanTests`·`CombatProgressJobTests`(호출) | 7.8 |
| **K-b** | K-2 | `JobRootType.cs`·`JobRegisterSystem.cs`(연결 줄), `CombatPlan.cs`, `CombatEntryService.cs`, `CombatEntryRequest.cs`(`IsAwaitingRegistration`) | 우리 `CombatProgressTestDriver`, 호진님 둘(Q-T2) | 7.9 |
| **K-c** | K-3(Q-K3 B) | `CombatEntryRequest.cs`, `CombatEntryService.cs`(호진님 2줄 포함), `GameBootstrap.cs` | 단계 질문을 보던 시험이 있으면 맞춤 | 7.9(B) / 8.0(A) |
| **K-d** | K-4 | 새 `Common/SceneNodeOrder.cs`(+ `.meta`), `GameBootstrap.cs`, `CombatViewSystem.cs`, `TutorialViewSystem.cs`(호진님 2줄) | 상수를 읽는 시험 맞춤 | 8.0 / 8.2 |
| **K-e** | K-5 a·c~g | `CombatPlan.cs`, Job 넷, `CombatProgressReader.cs`·`CombatProgressFacts.cs`, `CombatEntryRequest.cs`, `CombatEndUI.cs`, `GameBootstrap.cs` | `CombatProgressReaderTests` 등 | 8.1 / 8.2 |
| **K-f** | 문서·공유 | 민형님 10-10 공유 새 절, 호진님 메모 새 절(Stage·StageMap 질문 제안, 바꾼 줄·시험), 씬 연결 안내 §1.1, CLAUDE.md | — | — |

## 6. 원칙 점검과 점수

**SSOT.** 진입 단계 조합의 뜻, 요청 "끝남", 포기 완료, 씬 Node 순서가 각각 한 곳이 된다. 그래도 SSOT 낮음은 다른 항목(사실 읽는 길 셋 등)이 남아 상한 안이라 점수는 그대로다.

**SOLID.** 공용 등록기 계약에서 전투 전용 입구가 빠진다(ISP·OCP). 다른 루트가 대상 있는 등록을 원해도 같은 `Register` 꼴을 쓰면 된다. 준비 Job이 필요한 서비스 둘만 받는다.

**FW 계약.** Job·Plan 등록 경로가 "등록기 → Plan → Runner" 하나가 된다. Combat Plan만 Runner를 받던 예외가 없어진다.

**있는 기능 사용.** 새 장치 없음. `Register`·`JobType`의 기존 꼴(`EntryTransition`과 같음)을 쓴다.

**독립성.**
- 단계 연속 실행이 Runner의 FIFO·예산·다른 루트의 관례에 기대지 않는다.
- 씬 Node 순서를 바꾸면 기대는 값이 식으로 따라 움직인다.
- 진입 단계 표를 바꾸면 질문 구현만 바뀐다(Q-K3 A면 Stage까지, B면 우리 쪽까지).

**새로 생기는 문제(낮음).**
- `CombatNextJob`이 판단과 실행을 함께 한다. 단계 Job이 기다리는 Job으로 바뀌면 예외가 된다(P1). 그것이 바라는 동작이다.
- `JobType`에 전투 전용 값이 하나 더 생긴다(연결 줄, `EntryTransition`과 같은 꼴).
- `Common`에 파일이 하나 는다.

**점수(재평가 감점 방식):**

| 기준 | 지금 | 골격 점검 뒤(권장안) | 근거 |
|---|---|---|---|
| SSOT | 7.5 | 7.5 | 중간 둘(#2·#3)은 View 단계. 낮음은 여전히 상한 |
| SOLID | 7.5 | 8.25 | #5 해소. 조정자(#4)·낮음 상한은 남음 |
| FW 계약 | 7.5 | 8.5 | #6 해소, 낮음 하나(그룹 전체 Job) 해소. #7은 View 단계 |
| 있는 기능 사용 | 9.5 | 9.5 | 보류 항목(§8) |
| 독립성 | 7.0 | 8.5 | #8·#9 해소. #10은 B면 남음(A면 9.25) |
| 불필요·우회 | 6.25 | 6.25 | H1·#11과 낮음 상한이 남음(K-5가 줄여도 넷 넘게 남음) |
| **합** | **7.5** | **약 8.0~8.1** | 새 낮음 "`JobType` 전투 값"을 FW에 세면 8.0(§9). Q-K3 A면 약 0.1 더, H1까지 고치면 약 8.3~8.4 |

## 7. 다른 사람 줄과 공유 (Phase K-f, 모두 미전달로 남김)

- **민형님(10-10 Job·Plan 공유 문서 새 절):** 연결 줄 셋(P6). 다른 루트 Plan과 Runner 등록 방식은 그대로다.
- **호진님(10-22 메모 새 절):**
  - 우리 파일 안 호진님 줄: `CombatEntryService.cs:121-122`(K-3), `TutorialViewSystem.cs:52-53`(K-4)
  - 고친 시험 둘(Q-T2)
  - Stage·StageMap의 `Status` 해석을 질문으로 바꾸자는 제안(Q-K3 B, 대응표 §2 K-3)
- **천명님(Core 전달 문서 새 절):** Core 무변경, 요청 없음. 다만 K-5 e가 천명님 줄 `CombatPrepareJob.cs:43`(직행 진입의 Core 호출)을 바꾼다. 부르는 API·인자는 그대로이고, 서비스를 받는 자리만 바뀐다(§9 정정).
- **현강님(10-22 메모 새 절):** Bootstrap 노드 상수가 `SceneNodeOrder`로 옮겨진다는 알림(메모 §2가 그 상수를 알렸음). `CombatEndUI.cs:150`은 이번에 고치지 않고 View 단계 #3에서 다룬다(§9).
- **호진님 추가(§9 정정):** `TutorialViewSystem.cs:203-204`(등록 두 줄), `CombatPrepareJob.cs:45-53`(맵 진입 갈래), 낡게 되는 호진님 문서 두 곳(`viewDocs/stage-selection-input-guide.md:23`, `guides/stage-artifact-shop-sequence-uml.md:159`).

## 8. 이번 단계 밖 (보류, 이유)

- **View·조정자 단계:** #2 제출 정지 깃발을 사실 묶음에 "이탈 시작"으로, #3 결과 화면이 판정된 화면 값만 받기, #4 조정자 분리, #7 확인창 능력 공개 계약, #11 HUD 완료 경로(현강님·다른 사람 시험).
- **H1(#1):** 사용자 결정으로 미룸.
- **있는 기능 낮음 둘:**
  - 개발 진입 노드 해석이 Core 규칙을 복사한다(`ResolveDevEntryNode`). Core `PrepareStageEntry`로 충분한지 조사가 먼저다.
  - 개발 정의 소스를 손으로 바인딩한다. Factory 연결 줄로 모듈 등록이 필요하다.
- **Factory 오류 경계 대체 생성:** 인자 없이 부르는 다른 사람 시험 때문에 남긴 것(10-22 결정).
- **진입 사전 조건(Runner 빔·로딩 아님)을 부르는 쪽이 미리 확인:** 이름 있는 질문을 계약에 더하면, 진입 서비스를 구현하는 가짜(다른 사람 시험)가 깨진다.
- **ExitCheck Job·`CombatLifeCycle`의 구체 의존·Runner 선점 지점:** 기반 한계에 가깝고, 바꿀 때 영향에 비해 점수 변화가 작다.
- **튜토리얼 사실 읽기 합치기:** Q-K6.

## 9. Task 계획 대조로 바로잡은 것 (2026-10-23, 코드 변경 없음)

Task 계획을 권장안 가정으로 쓰면서 코드와 다시 대조했고, 하위 에이전트 2개(코드 사실·호출처 / 설계 일치·동작·규칙)로 검증했다. 자세한 내용은 [계획](../plans/2026-10-23-combat-skeleton-check-plan.md)의 TP와 "검증 기록"에 있다. 설계 본문에서 바뀌는 것만 적는다.

- **Q-K2는 10-22 결정을 뒤집는다(계획 TP-14).**
  - [문제점 분석](2026-10-21-combat-skeleton-problem-analysis.md) §6 결정 4-③(10-22 사용자)은 "`RegisterCombatEntry`의 `MarkQueued`·`Fail`은 그대로"였다.
  - K-2 뒤에는 요청을 차지하는 일을 진입 서비스가 한다(`Register` 바로 뒤 `MarkQueued`).
  - 중복 거절은 두 겹으로 남는다. 진입 서비스가 등록 전에 막고(지금 그대로), Plan이 등록 대기 요청만 받는다.
  - 우리 시험 `CombatPlanTests:48`은 확인 대상이 등록기에서 Plan으로 바뀐다.
- **Q-K5 권장을 A → B로 바꾼다(계획 TP-12).**
  - `BattleInstanceId`는 생성자 둘째 순서 인자다. 호진님 `TutorialPlanTests:177`이 순서대로 넘기므로, 지우면 컴파일이 깨진다.
  - 지워도 점수는 그대로다(낮음 상한).
- **K-5 a는 골격 두 곳만 바꾼다(계획 TP-16).** `CombatEndUI:150`을 고치면 결과 화면(UISystem)이 처음으로 골격 판단 클래스를 참조한다. 이 판정은 View 단계 #3이 결과 화면에서 걷는다.
- **K-5 e의 시험 영향을 고친다.** "직접 만드는 시험 없음"은 틀렸다. 우리 `CombatPlanTests:92·174·175`가 준비 Job을 직접 만든다. 시험 가짜 Core의 `StageCommands`가 null이라, 준비 Job은 진입 종류에 필요한 서비스만 요구한다(계획 TP-10).
- **K-2 시험 영향을 더한다.** 우리 `CombatPlanTests`의 진입 시험 넷이 `RegisterCombatEntry`를 직접 부른다(`:44·48·77·107·130`). 호출만 맞춘다(계획 TP-3).
- **K-3 표를 고친다.**
  - Bootstrap `:230`은 `IsReplaceable`이 아니라 `IsActive ∨ IsCancelled`다(계획 TP-6).
  - Q-K3 B에서는 지금 쓰는 질문 일곱만 더한다. 새로 생기는 질문 `IsQueued`를 포함한다. Stage·StageMap 전용 다섯은 제안으로 둔다(계획 TP-5).
- **K-1 주석 목록을 고친다.**
  - 조정자 `:923-924`는 K-1 뒤에도 맞아서 빠진다(계획 TP-1).
  - `CombatExitJobs.cs:103`·`CombatPreviewJob.cs:22`가 더해진다.
- **K-4 시험을 더한다.** 호진님 `TutorialViewSystemTests:34-35`가 조정자 상수를 읽는다. Q-T2 허락 대상에 넣었고, 허락하지 않으면 별칭 상수 둘을 남긴다.
- **다른 사람 줄을 §7에 더했다.**
  - 천명님 `CombatPrepareJob.cs:43`
  - 호진님 `CombatPrepareJob.cs:45-53`·`TutorialViewSystem.cs:203-204`
- **점수를 고친다.** 새 낮음 "`JobType` 전투 값"을 FW에 세면 K-5 e의 FW 이득이 상쇄된다. 그래서 합은 약 8.0~8.1이다.
- **K-3의 "요청 끝남 판정 넷이 `IsReplaceable` 하나가 된다"(§2 K-3)는 철회한다(K-c 검토).** 뜻이 다른 셋이 남는다.
  - `IsReplaceable`: 다음 요청이 대신해도 되는지(진입 서비스의 사본 `IsFinished`를 대신함)
  - 종료 중 끝난 의미 보존(진입 서비스 `Shutdown`): 닫힘 ∨ 취소 ∨ 거절, 배출 여부는 보지 않음(계획 TP-7)
  - Bootstrap 결과 판정: 거절 / 활성 ∨ 취소(계획 TP-6)
  - K-c는 Q-K3 B라 점수를 바꾸지 않았으므로 점수 계산은 그대로다. `IsInProgress`는 K-5 d에서 지운다.
- **CLAUDE.md 규칙 글:** "작업 방식"의 영역 규칙이 연결 줄 예시로 `RegisterCombatEntry`를 적고 있다. K-2를 하면 고쳐야 하지만, 규칙 글이라 사용자에게 먼저 묻는다(계획 TP-18).
- **K-1은 10-22 개선 설계 2판의 선택을 되돌린다(K-f 문서 정리 중 찾음, 설계 때 놓침).**
  - 10-22 개선 설계 §5.1의 1판은 바로 K-1과 같은 꼴(단계 Job을 `CombatNextJob` 안에서 실행)이었다. 독립 검토가 셋을 지적해 2판에서 "Runner에 넘김"으로 바꿨다.
    - (a) Runner의 연속 실행을 Job 안에서 다시 만든다
    - (b) 두 그룹을 통째로 들고 다닌다
    - (c) 한 번에 안 끝나는 단계 Job의 규칙이 없다
  - K-1 뒤 (b)는 좁은 걸음 재료 `CombatStepContext`가 그대로라 다시 생기지 않는다. (c)는 "끝나지 않으면 예외"(P1)로 정했다.
  - (a)는 남는 맞바꿈이다. 10-23 독립 재평가는 반대쪽(Job 안의 Runner 재등록 = FW 중간, 같은 Tick 가정 = 독립성 중간)을 문제로 봤다. 그래서 두 중간을 푸는 쪽을 골랐다. 이 판단은 다음 독립 평가에서 다시 볼 수 있다.
  - 10-22 설계 §5.1 머리에 "대신함"을 달았다.
- **실행 중 더한 것(계획 기록에 자세히):**
  - K-c: 질문 일곱 중 `IsQueued`는 새로 만든 질문이다(TP-5).
  - K-d 검토로 주석·예외 문구 9곳의 순서 숫자를 `SceneNodeOrder` 이름으로 바꿨다(사용자 결정 A, K-e Ke.12).
  - TP-18(사용자 허락): CLAUDE.md 영역 규칙의 연결 줄 예시를 `RegisterCombatEntry`에서 `JobType.Entry`로 고쳤다.
