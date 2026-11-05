# 전투 골격 구조 원칙 평가 (2026-10-21)

- 작성: View 작업, 2026-10-21. 사용자 요청: "전투 골격 구조 평가 — 1. SSOT 2. SOLID 3. JMH Framework 개발 지침·FW 계약 4. Framework에 있는 기능을 쓰는지, 이미 있는 기능을 새로 만들지 않는지 5. 기능 변경의 독립성 6. 불필요하게 만들거나 돌아가는 구조는 아닌지 — 1~10으로 평가".
- **평가만 한다. 코드 변경 없음.** 처리 방향은 후보이며 결정이 아니다.
- 기준 코드: HEAD `067b53ad`(작업 트리 변경은 FMOD 캐시뿐).
- 2026-10-21 보정: 흐름 지도([전투 골격 흐름](https://claude.ai/artifact/4sFcucyiPtQX5UzpTNxvA7))를 만들며 SK3의 해석 위치가 다섯이 아니라 일곱 파일임을 확인해 고쳤다. 점수는 그대로다.
- 판단 기준: [설계 인계 §4](../handoffs/2026-09-30-combat-view-design-session-handoff.md)(§4.1 SSOT, §4.2 SOLID, §4.3 FW, §4.6 변경 독립성), [프로그래머 가이드](../../../Assets/ProjectL/Script/Systems/Documentation/framework-programmer-guide.md) FW-001~016·FW-101~104, [Framework 개발 지침](../framework/Framework%20Development%20Guidelines.md) §2·§15·§19·§26~28.
- **범위 기준(10-21 지시, 10-22 정정):** 작업 영역은 Combat 씬의 전투 View 구조이고 작성자와 무관하다. Framework·GlobalSystem·Core(계산기)는 주어진 기반으로 두고 연결 줄만 고친다. 아래 §7 개선 묶음 A~D 중 Framework·Core·다른 씬(Stage·Lobby)을 고치는 부분만 빼면, 전투 View 구조 안의 것(호진님 보스 경로·착지 대기 포함)은 다시 쓸 수 있다. 영역별 재분류와 결정은 [문제점 분석](2026-10-21-combat-skeleton-problem-analysis.md) §0~§2·§6.
- 선행 문서: [10-18 구조 원칙 평가](2026-10-18-combat-structure-principles-evaluation.md) §3(골격 "양호", K1~K4). 이 문서는 지금 골격의 절대 판정이다. 10-18 뒤에 들어온 변경(10-16 맵 착지 티켓 — 10-18 평가가 다루지 않음, 10-20 보스 선정리·Run 종료 로비 복귀)이 이번 감점의 대부분이다.

## 0. 범위

- **골격 본체:** `LifeCycleSystem/LifeCycles/Combat/` 18개 파일 1,861줄(10-18에 1,751줄) — 수명 `CombatLifeCycle`, 진입 요청 수명 `CombatEntryService`·`CombatEntryRequest`·맵 표시 소유권(`MapCombatPresentationLease`·`Ticket`), 판단 `CombatPlan`·`CombatStep`·`CombatExitContext`, 사실 읽기 `CombatProgressReader`·`CombatProgressFacts`, View 계약 `ICombatProgressView`, Job 열셋.
- **골격이 기대는 공용 부품:** `JobRegisterSystem`·`JobRunnerSystem`·`JobQueue`, `LifeCycleBase`·`LifeCycleManager`·`LifeCycleSystemGroup`·`LifeCycleSystemFactory`, `RuntimeFaultBoundary`, `GameBootstrap`의 실행·진입 부분.
- **경계만 본다:** View의 `ICombatProgressView` 구현(`CombatViewSystem`), 진입 요청을 읽는 `StageNodeEntryFlow`, 새 Run을 시작하는 다른 루트(Stage·Lobby Plan/Job).
- **제외:** Core 규칙, 전투 View 내부 구조(입력·재생·연출), 씬·프리팹.

## 1. 결론

**판단·읽기·실행의 뼈대는 여전히 좋다. 감점은 10-16~10-20에 덧붙은 경로(보스 선정리, 맵 착지 대기, Run 종료 로비 복귀)가 골격의 한 곳 원칙을 따르지 않고 여러 곳에 퍼진 데서 나온다.**

| # | 기준 | 점수 | 요지 |
|---|---|---|---|
| 1 | SSOT | **7** | 판단 `Decide` 하나·사실 읽기 하나는 그대로. 보스 "정리 먼저, 표시 나중" 규칙이 다섯 곳(SK1), Run 종료 판정 두 방식(SK1), 새 Run 로드아웃 상수 둘·새 Run Job 셋(SK2), 진입 요청 상태를 요청 밖 일곱 파일이 해석(SK3) |
| 2 | SOLID | **7.5** | 수명·진입·판단·읽기·실행 분리와 의존 방향은 양호. 진입 서비스가 맵 표시 소유권까지 맡고(SK4), Bootstrap이 실행 루프 밖 일(타이틀·덱 UI·배경음·개발 노드 탐색)을 함께 가짐(SK8) |
| 3 | Framework 지침·FW 계약 | **8.5** | MUST 위반 없음. Tick 경로 할당 0(공유 빈 목록), 오류 뒤 새 등록 없음, 정렬 동률 = 등록 순서에 기댄 연쇄도 FW-015·§10과 맞음. 공용 JobRegister에 전투 전용 입구(K1)는 그대로 |
| 4 | 있는 기능 사용·재발명 | **7.5** | Framework 부품(LifeCycle·로딩 Node·Job Runner·JobQueue·Builder·PostBind)은 모두 그대로 쓰고 새로 만든 실행기·타이머는 없다. 다만 프로젝트에 이미 있는 것을 다시 만든 곳: 새 Run 시작 Job 셋(SK2), 요청 수명 상태 기계 두 벌(SK3), "대상 있는 등록" 두 방식(SK5) |
| 5 | 기능 변경의 독립성 | **7** | 진행 단계·Core 호출 변경은 `Combat/` 한 폴더·한 Job에서 끝난다. 보스 정책(골격+View), 새 Run 정책(세 루트), 진입 상태(Combat·Stage·Boot), 맵 착지(Stage View·진입 서비스·Job)는 소유자가 여럿이다 |
| 6 | 불필요·우회 구조 | **6.5** | Run 종료 때 새 Run을 두 번 만든다(SK2), 보스 선정리의 Run 종료 판정 결과를 아무도 읽지 않는다(SK1), 맵 착지 확인이 세 층을 거친다(SK4), 쓰이지 않는 열거 값·도달 불가 검사(SK6) |
| | **종합** | **7.3** | 10-18 판정 "양호"(점수 없음)에서 내려감. 묶음 A~D를 모두 반영하면 약 8.7(§7) |

## 2. 현재 구조 지도

```text
GameBootstrap(View/Boot) ─ 런타임 생성·Tick·오류 보고(F1) · 개발 진입: StartNewRun → EnterChapter → RequestEntry
                     │
StageNodeEntryFlow ──┼─► CombatEntryService.RequestEntry(chapter, node[, mapCommand])
(맵 노드 진입)        │      └ JobRegister.RegisterCombatEntry(request) → CombatPlan.RegisterEntry
                     │           = [CombatPrepareJob] → [CombatEntryTransitionJob(맵 착지 대기 = 미완료 반환)]
StageMapViewScope ───┘      맵 착지 소유권: BindMapPresentationOwner → Lease ─ request.Ticket(Pending/Ready/Abandoned)

CombatLifeCycle(트리거) ─ OnEnter: entry.ActivateAccepted() · OnTick: Runner가 비면 Register(Combat, Next) · OnExit: ExitCheck
CombatPlan.Next ─ CombatProgressReader.Read()(Buffer 싱크 5 + ICombatProgressView) → Decide(facts) → Job 목록
  0행  전투 없음 ∧ 보스 완료 확인됨          → ExitBattle(RunOutcome → NewRun → Transition)         ← 10-20 추가
  1~6  완료 있음: 제출 정지 → 표시(보스면 CompletionCheck → EndBattle → RunEnd → Completion) → 확인 → ExitBattle
  7~9  포기: 제출 정지 → 남은 연출 → Abandon
  10~13 명령 · 미리보기 · 자동 진행
CombatViewSystem(View) ─ ICombatProgressView 구현 · TickCore에 보스 표시 유지 분기                       ← 10-20 추가
```

## 3. 지금도 좋은 점

- **판단 원본 하나:** `CombatPlan.Decide`가 사실 묶음만 받는 순수 함수다. 행 순서(승패 > 포기 > 처리)가 한 화면에 있고 시험이 사실을 직접 만들어 넣는다(`CombatPlanDecideTests`).
- **사실 읽기 하나:** `CombatProgressReader`만 Buffer 싱크를 받는다. 사실 묶음은 판단 시점의 `readonly struct` 사본이고 저장하지 않는다. View 상태 다섯은 View가 원본이다(J12).
- **Job = 호출 한 번 + 같은 Execute 안의 확인:** 반환값으로 판단하지 않는다(K14). 자동 진행은 순번·종류·수락 순서로 확인한다(K8).
- **이탈 연쇄는 한 번에 등록:** 정렬 동률이면 등록 순서라는 `JobQueue` 보장(`Sequence` 비교, 가이드 §10)에 기대며, 이는 Framework가 문서로 보장하는 동작이다.
- **Tick 경로 할당 0:** 할 일이 없으면 공유 `NoJobs`를 돌려준다(지침 §19).
- **오류 경계 하나:** 같은 `RuntimeFaultBoundary`를 Bootstrap·LifeCycle·진입 서비스·View가 공유하고, 오류 뒤에는 새 등록·Tick을 하지 않는다. Framework가 정하지 않은 실패 정책(DR-001)을 메우는 것이지 있는 기능의 사본이 아니다.
- **의존 방향:** 골격이 `ICombatProgressView`를 정의하고 View가 구현한다. Plan은 그룹 서비스 계약만 받는다.
- **시험이 계약으로 검증:** 골격 시험 다수가 리플렉션 없이 internal API로 검증한다(10-18 R8의 문제가 골격에는 거의 없다).

## 4. 문제

심각도는 변경 빈도와 잘못 고칠 위험을 함께 본 판단이다. "작성"은 그 코드를 넣은 커밋 작성자이며, 다른 작업자 영역은 CLAUDE.md 규칙대로 보고만 한다.

### SK1 보스 "정리 먼저, 표시 나중" 규칙이 다섯 곳에 — 중간 (SSOT·독립성·우회)

- **작성:** 호진님 `cb76bd2c`(2026-10-20).
- **위치:**
  1. `CombatPlan.Decide` 0행 — 전투 없음 ∧ 보스 완료 ∧ 연결 ∧ 제출 정지 ∧ 연출 없음 ∧ 확인됨([CombatPlan.cs:83](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Combat/Plans/CombatPlan.cs#L83)).
  2. `CombatPlan.PresentCompletion` — 보스면 표시 앞에 CompletionCheck → EndBattle → RunEnd를 끼움([CombatPlan.cs:175](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Combat/Plans/CombatPlan.cs#L175)).
  3. `CombatCompletionCheckJob` — 1~6행이 방금 본 조건 다섯을 다시 검사([CombatExitJobs.cs:103](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Combat/Jobs/CombatExitJobs.cs#L103)).
  4. `CombatRunOutcomeJob` — 0행 조건 다섯을 다시 검사([CombatExitJobs.cs:230](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Combat/Jobs/CombatExitJobs.cs#L230)).
  5. View `CombatViewSystem.TickCore` — "전투가 사라져도 같은 완료를 표시 중이면 화면 유지" 분기, 조건 다섯([CombatViewSystem.cs:289](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/CombatViewSystem.cs#L289)).
- **같은 사실의 두 판정:** "Run이 끝났나"를 `CombatRunEndJob`은 `CompleteChapter` 결과의 `Kind == RunCompleted`로, `CombatRunOutcomeJob`은 `ChapterProgression.Outcome`으로 정한다. 둘 다 Core 값이라 틀리지는 않지만 같은 질문에 답하는 방법이 둘이다.
- **쓰이지 않는 값:** 보스 선정리 연쇄의 `CombatRunEndJob`은 `DecideRunEnd`로 판정을 남기지만, 그 컨텍스트는 연쇄가 끝나면 버려지고 아무도 `EndsRun`·`Destination`을 읽지 않는다. 필요한 것은 `CompleteChapter` 호출이라는 부수 효과뿐이다.
- **왜 문제인가:**
  - 정책 하나("보스는 표시 전에 정리")를 바꾸면 골격 4곳과 View 1곳을 함께 읽고 고친다. 한쪽 조건만 바꾸면 Job의 재검사가 예외로 런타임을 멈춘다(F1).
  - 0행은 "전투 없음 = 할 일 없음"이라는 이전 불변식을 깼다. 이탈 연쇄가 같은 이탈을 두 번 만들지 않는다는 근거(J2 주석)가 이제 "보스 완료가 아니면"이라는 조건에 기댄다.
- **확인 공백:** `CombatPlanDecideTests`에 0행 사례가 없다. 통합 시험은 최종 보스 둘만 있어, 다음 챕터로 이어지는 보스(선정리 뒤 StageSelect)는 시험이 없다. 지금 코드에서 그 경로의 `Completion`은 보존되는 것으로 보이지만(`PublishContinuation`이 지우지 않음) 시험으로 고정돼 있지 않다.
- **방향 후보:**
  - (a) 조건 둘에 이름을 주고(`CombatProgressFacts`의 "보스 선정리 준비"·"선정리된 보스 확인") `Decide`와 두 Job이 같은 식을 부른다. 재검사는 남기되 원본은 하나.
  - (b) 선정리 연쇄는 `CompleteChapter`만 하는 Job으로 줄이고, Run 종료 판정은 이탈 연쇄의 `Outcome` 한 방법으로 모은다(`CombatRunEndJob`의 보스 분기와 `CombatRunOutcomeJob`이 하나가 됨).
  - (c) View 분기는 "같은 완료를 표시 중이면 전투 사라짐을 정리하지 않는다"로 일반화해 보스를 몰라도 되게 한다.
  - 호진님 변경이라 **호진님·사용자 확인 뒤** 정한다.

### SK2 Run 종료 때 새 Run을 두 번 만든다 — 중간 (우회·SSOT·재발명, 알려진 후속)

- **흐름:** Run 종료 → `CombatNewRunJob`이 `StartNewRun(1)` → 로비 → 출발 → `LobbyRunStartJob`이 다시 `StartNewRun(1)`.
- **원인:** 전투 쪽 새 Run은 Run 종료 목적지가 StageSelect였을 때(K21, Stage가 로스터를 보고 `Start`만 거는 경로) 필요했다. 목적지가 로비로 바뀐 뒤(10-16 패배, 10-20 포기·최종 승리)에는 로비 출발이 새 Run을 다시 만든다. 위치 통합은 [로비 계획 LV10](../plans/2026-10-11-lobby-plan-Hojin.md)의 별도 후속으로 기록돼 있다(재구성 설계 J7).
- **같은 사실·같은 기능이 여러 벌:**
  - 새 Run 로드아웃 1: `CombatPlan.NewRunLoadoutKey`([CombatPlan.cs:25](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Combat/Plans/CombatPlan.cs#L25))와 `StagePlan.DefaultLoadoutKey`([StagePlan.cs:15](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Stage/Plans/StagePlan.cs#L15), Lobby도 이것을 씀). 한쪽만 바꾸면 Run 종료 직후와 로비 출발의 로스터가 달라진다.
  - 새 Run 시작 Job 셋: Stage `NewRunJob`, Lobby `LobbyRunStartJob`, `CombatNewRunJob`. 앞뒤 확인이 제각각이다 — 맵 입력 비우기는 Stage·Lobby만, "전투 없음" 확인은 Lobby는 호출 전·Combat은 호출 뒤. 처음 K21은 Stage의 `NewRunJob`을 같이 썼는데 R2에서 전투 전용으로 갈라졌다.
- **방향 후보:**
  - (a) LV10 결정: Run 종료 이탈은 정리·판정·전환만 하고 새 Run은 로비 출발 한 곳에서 만든다(전투 쪽 `CombatNewRunJob`·`NewRunLoadoutKey` 제거). 로비가 출발 전에 새 Run 상태(로스터·재화)를 보여 줘야 하는지 먼저 확인한다.
  - (b) 그대로 두되 로드아웃 상수를 하나로 모은다(최소 정리).
  - 로비(호진님)·Run(천명님) 영역이 걸려 **사용자 결정 필요.**

### SK3 진입 요청 상태의 뜻을 소비자마다 해석 — 낮음~중간 (SSOT·독립성·재발명)

- **상태:** `CombatEntryStatus` 10값 + `IsJobCompleted` + 맵 표시 표 상태 3값.
- **해석하는 곳:** `CombatEntryService.IsFinished`·`ActivateAccepted`·`Shutdown`, `CombatEntryTransitionJob`, `StageNodeEntryFlow.TryConfirmPending`(상태 8개를 분기, [StageNodeEntryFlow.cs:65](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Stage/StageNodeEntryFlow.cs#L65)), `StageLifeCycle`(`TryDeferCompletedCombatReturn`·`ValidateSceneExit`·`IsAcceptedCombatTransition`·`StopAndCancelEntry` 네 곳), `StageMapEntryObserver`(세 곳), `StageMapNodeMovement`, `GameBootstrap.Update`([GameBootstrap.cs:217](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Boot/GameBootstrap.cs#L217)).
- **겹친 검사:** `ActivateAccepted`의 `Result.Accepted`·`IsJobCompleted`는 `Status == Activating`이면 이미 참이다(`BeginActivation`이 보장).
- **같은 상태 기계 두 벌:** `LobbyDepartureRequest`(9값, Created·Queued·Running·Prepared·Rejected·Cancelled·…·Faulted + `IsJobCompleted` + `PublishResult`·`CompleteJob`·`Fail`)가 진입 요청과 같은 모양으로 따로 있다.
- **영향:** 상태를 하나 더하면(10-16에 맵 표시 대기가 그랬다) 일곱 파일을 모두 점검해야 하고, 빠뜨리면 Stage 진입 확인이 멈추거나 오류로 끝난다.
- **방향 후보:** 요청이 이름 있는 질문(`IsSettled`·`IsAwaitingTransition`·`WasAccepted` 등)을 내보내고 소비자는 그것만 읽는다. 공통 요청 수명 부품은 두 번째 복사본이 생긴 지금이 검토 시점이지만, 두 요청의 단계가 조금 달라(Activating·Active vs Transitioning·Completed) 억지로 합치지 않는다.

### SK4 맵 착지 대기가 세 층을 거친다 — 낮음~중간 (S·우회)

- **작성:** 호진님 `edcda135`(2026-10-16).
- **구성:** 진입 서비스가 소유권 `MapCombatPresentationLease`를 발급하고, 요청은 `MapCombatPresentationTicket`을 들고, 표시 완료 확인은 서비스(조건 7개) → 요청(조건 5개) → 표(소유자 같음·Pending) 세 층에서 검사한다. 전환 Job은 착지 전이면 미완료로 돌아가 Runner가 다음 Tick에 다시 부른다([CombatEntryTransitionJob.cs:45](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Combat/Jobs/CombatEntryTransitionJob.cs#L45)).
- **판정:**
  - 소유권(지난 맵 씬의 늦은 완료를 거르는 것) 자체는 필요한 규칙이다.
  - 다만 진입 서비스가 "진입 요청 수명"과 "맵 표시 소유권 관리"를 함께 맡게 됐고(S), `ICombatEntryService`가 `IMapCombatPresentationOwnerSource`를 상속해 진입만 하는 소비자(Bootstrap)도 소유권 발급을 받는다(I).
  - 기다림의 방식이 둘이 됐다. 전투 진행은 "기다림은 Job이 아니라 Plan의 조건"(K1)인데 진입 전환은 Job이 미완료로 Runner를 붙든다. Framework가 허용하는 동작이라 FW 위반은 아니지만, 대기 중에는 같은 Runner의 다른 루트 Job도 줄을 선다.
- **방향 후보:** 표와 소유권을 한 객체로 합치고(요청이 소유권 참조와 "착지 완료" 하나만 가짐), 확인은 요청 한 층에서 한다. 대기를 조건으로 바꾸는 것(착지 완료 뒤에 전환 Job 등록)은 Stage 쪽 흐름과 함께 봐야 하므로 호진님 확인 뒤.

### SK5 "대상 있는 등록"을 두 방식으로 — 낮음 (재발명·O·I, 10-18 K1의 재검토 조건 도달)

- 전투: 공용 `IJobRegisterSystem`에 전용 입구 `RegisterCombatEntry(request)`([JobRegisterSystem.cs:24](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/JobSystem/JobRegister/JobRegisterSystem.cs#L24)).
- 로비: 요청 객체를 Buffer(`LobbyViewReadSink.Current`)에 두고 Plan이 읽는다([LobbyPlan.cs:32](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Lobby/Plans/LobbyPlan.cs#L32)).
- 10-18 K1은 "두 번째 루트가 대상 있는 등록을 요구하면 `Register(root, request)`로 일반화"를 재검토 조건으로 적었다. 두 번째 루트가 생겼지만 다른 방식으로 풀렸다. 실행 요청 객체를 Buffer에 두는 쪽은 "Buffer = Core가 쓰는 진행 상태"(인계 §4.1)와도 결이 다르다.
- **방향 후보:** 일반화하면 공용 파일(JobRegister) 변경이라 민형님 공유가 필요하다. 지금은 두 방식을 기록하고, 세 번째 요구가 생길 때 정리해도 된다(수용 가능).

### SK6 쓰이지 않는 코드·도달하지 않는 검사 — 낮음 (불필요)

- `JobType.AdvanceCombat = 5`: 사용처 0([JobRootType.cs:22](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/JobSystem/JobRegister/JobRootType.cs#L22)). R3에서 전투 처리를 `Next`로 모은 뒤 남은 값이다(`Start`·`NewRun`·`ProcessCommand`·`ProcessPreview`는 다른 루트가 씀).
- `Execute` 안의 `_service == null` 검사 넷(Advance·Command·Preview·Prepare): 생성자가 이미 null을 거절한다. `default(struct)`로만 닿고, 그 경우 다른 필드도 비어 있어 의미 있는 보호가 아니다.
- `CombatLifeCycle.FaultBoundary` 속성: 시험 한 파일만 읽는다.
- **C# 참고(프로젝트 공통 관례):** Job이 가변 `struct`인데 `List<IJobService>`·Runner가 늘 박싱해 보관한다. 할당을 줄이는 효과는 없고, 값으로 복사되면 `IsCompleted`가 사본에만 바뀌는 함정이 있다. 다른 루트도 같은 관례라 전투만의 문제는 아니다(Lobby·Tutorial·Recovery의 새 Job은 `class`).

### SK7 제출 정지를 두 곳이 정한다 — 수용

- View는 종료 결과를 처음 볼 때 스스로 `SuspendSubmissions`를 부르고(`SyncSubmissionState`), Plan도 1·7행에서 제출 정지 Job을 건다. 멱등이고 포기는 Plan 쪽만 닿으므로 의도된 안전망이다(J12·D12). 기록만 한다.

### SK8 Bootstrap의 역할이 넓어졌다 — 낮음~중간 (S, 지침 §26)

- `GameBootstrap` 631줄. 골격이 인수한 역할(B-1~B-8: 런타임 생성·단계·Tick·오류·종료·개발 진입) 밖에 타이틀 UI 연결, 덱 UI 열기, 시작 배경음, 개발 진입 노드 탐색이 붙었다.
- `ResolveDevEntryNode`는 Core 진입 규칙("선택이 없으면 깊이 0에서 이어진 노드만")을 개발용으로 흉내 낸다. 판단은 여전히 Core가 하고 어긋나면 거절 로그만 남으므로 개발 전용으로 수용하되, 규칙 사본이라는 점은 기록한다.
- **방향 후보:** 타이틀·덱 UI·배경음은 로비/UI 쪽 시작 흐름으로, 개발 진입 준비(새 Run·아이템·챕터·노드)는 개발 전용 클래스 하나로 빼면 Bootstrap이 실행 수명만 남는다. 여러 작업자가 고친 파일이라 시점을 맞춘다.

## 5. 기준별 상세와 점수 근거

### 5.1 SSOT — 7

| 사실 | 원본 | 판정 |
|---|---|---|
| 다음 진행 단계 | `CombatPlan.Decide` | 양호 |
| 진행 판단에 쓰는 사실 | `CombatProgressReader`(Buffer·View에서 매번 읽음) | 양호 |
| 이탈 목적지 | `CombatPlan.ContinueDestination`·`RunEndDestination` | 양호 |
| 보스 완료 여부 | `CombatExitContext.RequiresChapterCompletion` 하나 | 양호 |
| 보스 정리·표시 순서와 그 조건 | 골격 4곳 + View 1곳 | **SK1** |
| Run이 끝났나 | Core 값이지만 읽는 방법 둘 | **SK1** |
| 새 Run 로드아웃 | 상수 둘 | **SK2** |
| 진입 요청 단계의 뜻 | 요청 밖 일곱 파일이 각자 해석 | **SK3** |
| 개발 진입 노드 규칙 | Core + Bootstrap 사본(개발 전용) | SK8, 수용 |

### 5.2 SOLID — 7.5

| 원칙 | 판정 | 근거 |
|---|---|---|
| S | 부분 | 수명·진입·판단·읽기·실행이 각 클래스. 진입 서비스에 맵 소유권(SK4), Bootstrap 확장(SK8) |
| O | 수용 | 새 단계 = `CombatStepKind` + `Decide` 한 줄 + `Next` 분기 + Job, 모두 `Combat/` 안. 보스 특례는 0행·표시 연쇄·View에 분기를 더했다(SK1) |
| L | 양호 | Job이 `IsCompleted` 뜻을 지킨다. 진입 전환 Job의 미완료 반환은 Runner 계약("미완료 Job은 다음 Tick에도 실행")이 허용한다 |
| I | 양호(작은 흠) | View 계약 7멤버·읽기 계약 3멤버. 진입 서비스 계약이 소유권 발급을 함께 노출(SK4) |
| D | 양호 | 골격이 계약을 정의하고 View가 구현. View 참조를 Reader(바인딩 때)와 Plan(호출마다 그룹에서) 두 곳이 얻지만 같은 객체다 |

### 5.3 JMH Framework 지침·FW 계약 — 8.5

- **MUST:** FW-001(서비스는 인터페이스로: `ICombatProgressReader`·`ICombatEntryService`) PASS, FW-002(PostBind·Initialize·Shutdown이 필요한 둘은 `AddModule`도) PASS, FW-003(Builder 한 번) PASS, FW-004(Runner·Store 우회 없음) PASS, FW-015(정렬 값 변경 없음, 동률 순서에 기댐 — 문서화된 보장) PASS, FW-016(Bootstrap이 단계 순서·오류 뒤 Tick 중단) PASS.
- **지침:** §15 PostBindService로만 받음, §19 Tick 할당 0, §27 과한 추상화 없음(골격 인터페이스는 View 경계·시험 입구로 실제 쓰임).
- **감점:** 공용 Global 서비스 JobRegister에 전투 전용 입구(K1·SK5), 지침 §26 경향의 Bootstrap(SK8).

### 5.4 Framework 기능 사용·재발명 — 7.5

- **쓰는 것:** LifeCycleBase(로딩 Node로 씬 진입·이탈), LifeCycleManager, JobRegister/JobRunner, JobQueue 순서 보장, Builder·PostBindService, Global Context를 거친 조회(가이드 §8). 자체 스케줄러·타이머·이벤트 버스를 만들지 않았다.
- **Framework에 없는 것을 메운 것(재발명 아님):** 오류 경계(DR-001), 조건 대기(Framework에 조건 대기가 없어 Job 미완료 반환 또는 Plan 조건을 씀).
- **프로젝트에 이미 있던 것을 다시 만든 것:** 새 Run 시작 Job 셋(SK2), 요청 수명 상태 기계 두 벌(SK3), 대상 있는 등록 두 방식(SK5).

### 5.5 기능 변경의 독립성 — 7

| 변경 시나리오 | 고칠 곳 | 판정 |
|---|---|---|
| 판단 행 순서·조건 변경(보스 제외) | `Decide` + `CombatPlanDecideTests` | 좋음 |
| Core 처리 호출의 확인 규칙 변경 | Job 하나 | 좋음 |
| View 상태 하나 추가 | `ICombatProgressView`·Reader·Facts·View 구현 | 보통(계약이라 당연) |
| 보스 정리 순서 변경 | Plan 2곳·Job 2곳·View 1곳 | **나쁨(SK1)** |
| 새 Run 로드아웃·시작 규칙 변경 | Combat·Stage·Lobby의 Plan/Job | **나쁨(SK2)** |
| 진입 요청 단계 추가 | 요청 + 일곱 파일(Combat·Stage·Stage 맵 View·Boot) | 보통~나쁨(SK3) |
| 맵 착지 연출 규칙 변경 | Stage View·진입 서비스·요청·표·전환 Job | 보통(SK4) |

### 5.6 불필요·우회 구조 — 6.5

- 우회: 새 Run 두 번(SK2), 버려지는 Run 종료 판정(SK1), 세 층 착지 확인(SK4).
- 불필요: 쓰이지 않는 `JobType.AdvanceCombat`, 도달하지 않는 null 검사 넷, 시험 전용 속성(SK6), 이미 보장된 활성화 검사(SK3).
- 우회가 아닌 것: Job 안의 재검사 자체는 등록(LifeCycle Tick)과 실행(다음 Runner Tick) 사이에 View 입력 콜백이 끼므로 필요하다(J3). 문제는 재검사가 아니라 그 식이 두 벌이라는 점이다(SK1).

## 6. 10-18 골격 항목의 현재 상태

| 항목 | 10-18 | 지금 | 근거 |
|---|---|---|---|
| K1 공용 Job 등록의 전투 전용 입구 | 낮음·수용 | **재검토 조건 도달** | 로비가 대상 있는 등록을 Buffer로 풀었다(SK5) |
| K2 Run 종료 판정이 Job 안 | 낮음·수용 | 수용, 단 판정 방식 둘 | SK1 |
| K3 임시 값 | 수용 | 일부 변경 | `RunEndDestination` = Lobby로 확정(Title 대기 아님). 새 Run 로드아웃 1은 상수 둘(SK2). "연결 정의 없음 → Run 종료" 임시 규칙은 10-20에 없어짐 |
| K4 골격 ↔ View 경계 | 양호 | 양호, 보스 분기 하나 | View에 보스 순서 지식 하나(SK1) |

## 7. 개선 묶음과 예상 점수

| 묶음 | 내용 | 영역·결정 | 예상(SSOT·SOLID·FW·재발명·독립성·우회 → 종합) |
|---|---|---|---|
| A 작은 정리 | SK6 정리, SK3 이름 있는 질문(진입 요청 쪽만), SK1 (a) 조건 이름 공유 + `Decide` 0행 시험·다음 챕터 보스 통합 시험 | 골격 + 호진님 확인 | 7.5·7.5·8.5·7.5·7.5·7 → **7.6** |
| B 보스 경로 단순화 | SK1 (b)(c): 선정리는 `CompleteChapter`만, Run 종료 판정 한 방법, View 분기 일반화 | 호진님·사용자 | 8·8·8.5·7.5·8·7.5 → **7.9** |
| C 새 Run 한 곳 | SK2 (a) LV10: 이탈은 새 Run을 만들지 않고 로비 출발만 만든다, 로드아웃 상수 하나 | 로비·Run 담당·사용자 | 8.5·8·8.5·8.5·8.5·8 → **8.3** |
| D 착지 대기·Bootstrap | SK4 소유권·표 합치기, SK8 Bootstrap 역할 분리 | 호진님·여러 작업자 | 9·8.5·9·8.5·8.5·8.5 → **8.7** |

- 묶음은 위에서부터 누적이다. A는 동작이 같고, B·C는 흐름이 바뀌므로 설계 문서(재구성 설계 J2·J7)를 먼저 고친다.
- SK5는 세 번째 요구가 생길 때까지 수용해도 점수에 큰 차이가 없다.
- 모든 묶음이 다른 작업자 코드에 닿는다. 착수 전에 해당 작업자와 사용자 결정이 필요하다.
