# 전투 View 구조 개선 Task 계획 (2026-10-22)

- 설계: [improvement-design](../specs/2026-10-22-combat-view-structure-improvement-design.md)(2판, 독립 검토 반영). 결정은 설계 §11.
- 기준 코드: HEAD `067b53ad`. 작업 트리는 문서·`CLAUDE.md`·FMOD 캐시만 바뀐 상태라 코드 겹침이 없다.
- **진행 규칙**
  - Phase 하나가 끝나면 멈추고 보고한다. 사용자가 Unity에서 컴파일·EditMode 시험·재생을 확인한 뒤 다음 Phase로 간다(CLAUDE.md).
  - 전투 View 작업이라 **MSBuild를 돌리지 않고 새 `[Test]`를 만들지 않는다.** 보고에 "컴파일 미확인"을 적는다.
  - 시험 조정: 컴파일이 깨지면 최소로 맞춘다. 우리 시험이 실행에서 깨지면 확인 내용은 그대로 두고 호출만 맞춘다. **걷는 기능이나 더는 성립하지 않는 동작을 확인하던 우리 시험은 지운다(설계 Q1).** 다른 사람 시험은 고쳐도 된다(설계 Q2). 고친 시험은 모두 실행 기록과 변경 정리에 적는다.
  - 새 파일은 `.meta`와 함께 만든다. 골격 파일은 `LifeCycles/Combat/`의 `Jobs/`·`Plans/`에, 전투 View 파일은 [폴더 배치 안내](../../viewDocs/combat-view-folder-guide.md)대로 둔다.
  - 다른 사람 줄을 고치면 그 줄을 Phase 7 변경 정리에 적는다. 연결 줄(GlobalSystem·Factory)은 민형님 공유 문서에 적는다.
- 경로 약어: `C/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Combat/`, `V/` = `…/RuntimeSystem/InGameSystem/ViewSystem/`, `T/` = `Assets/Tests/EditMode/Framework/`.

## 0. 점수 흐름(설계 §1 감점표, 누적)

| Phase 끝 | 7.7에서 | 바뀌는 것 |
|---|---|---|
| 1 (B) | **8.1** | 보스 특수 경로(P20)·새 Run 두 번(P21)·이탈 완료 추론(P6) 해소, 새 문제 N1 |
| 2 (A+E1) | **8.3** | 판단 재확인(P13)·Core 보존 재증명(P15)·그룹 의존(P26) 해소, 새 문제 N2 |
| 3 (C) | **8.7** | 착지 폴링(P3)·진입 종류 숫자(P22)·요청 상태(P9·P10·P11)·바쁨 예외(P4) |
| 4 (D) | **9.0** | View 계약(P25)·판정 사본(P16)·`IsPlaybackBusy`(P17)·세션 표현(P5) |
| 5 (E) | **9.1** | `AdvanceCombat`(P19)·Factory 대체 생성(P29)·캐스트(P28)·순서 숫자(P24) |
| 6 (F) | **9.3** | 조용한 영구 대기(P30)·완료 정체성(P12)·문구(P23) |

## Phase 1 — 묶음 B: 이탈 순서를 모든 결과에 하나로

**목표:** 모든 결과를 "남은 연출 → 정리(보스면 챕터 완료) → 표시 → 확인 → 떠나기"로 통일한다. 보스 특수 경로와 전투 쪽 새 Run을 없앤다. 나가는 순간 요청을 닫는다.

| Task | 파일 | 내용 |
|---|---|---|
| T1.1 | `C/CombatProgressFacts.cs`, `C/CombatProgressReader.cs` | 사실 묶음 끝에 `RunOutcome runOutcome = RunOutcome.InProgress`(선택 인자, 기존 호출 그대로). 사실 읽기가 Run 진행 읽기 싱크를 받는다(`PostBind`·`Bind`·`Read`) |
| T1.2 | `C/Plans/CombatStep.cs`, `C/Plans/CombatPlan.cs` | 단계 이름 `PresentCompletion` → `Settle`, `ExitBattle` → `Leave`. `Decide` 0행 삭제, 승패 블록을 맨 앞에, `None`이면 `HasBattle ? Settle : Nothing`, 그 뒤 `!HasBattle → Nothing`(설계 §4.4) |
| T1.3 | `C/Plans/CombatPlan.cs` | 이 단계의 `Next`(Phase 2에서 `BuildStep`)에 `Settle`·`Leave` 목록. `PresentCompletion`·`ExitBattle` 함수, `NewRunLoadoutKey`·`ContinueDestination`·`RunEndDestination` 삭제 → `DestinationOf(RunOutcome)` 하나. `RequiresChapterCompletion`은 Plan으로 옮김 |
| T1.4 | `C/Jobs/CombatExitJobs.cs` | `CombatCompletionCheckJob`·`CombatRunOutcomeJob`·`CombatNewRunJob` 삭제. `CombatRunEndJob` → `CombatChapterCompletionJob`(보스 승리일 때만 등록, `CompleteChapter` 한 번, 거절은 예외). `CombatEndBattleJob`은 컨텍스트 대신 기대 결과 순번을 받음. `CombatTransitionJob`은 목적지 키를 받음 |
| T1.5 | `C/Plans/CombatExitContext.cs` (+ `.meta`) | 파일 삭제 |
| T1.6 | `V/Combat/CombatViewSystem.cs`, `C/ICombatProgressView.cs` | 결과를 띄워 두는 조건을 이름 있는 판정 `IsHoldingResult()`로(보스 주석 제거). `PresentCompletion` 설명을 "모든 결과는 정리 뒤 표시"로 |
| T1.7 | `C/CombatLifeCycle.cs`, `C/CombatEntryService.cs` | `OnExit`: 정상 이탈이면 `TryCloseExited()`로 바로 닫고, 전투가 남으면 `ExitCheck`(설계 §4.6) |
| T1.8 | `LifeCycles/Stage/StageLifeCycle.cs`, `IStageDeckEditFacts.cs`, `StageDeckEditWindow.cs` | Stage 보류 장치(`TryDeferCompletedCombatReturn`·`_pendingCombatReturn`·`OnTick`의 보류 갈래) 삭제(다른 영역, 사용자 지시). `IStageDeckEditFacts.HasPendingCombatReturn`과 창의 조건도 함께 삭제(창 줄 주인 확인 후 변경 정리) |
| T1.9 | 시험 | 아래 목록 |

**T1.9 시험 조정**
- 컴파일 맞춤(우리): `CombatProgressReaderTests:47·148·157-165`, `CombatProgressTestDriver:28`(`Bind` 인자). `CombatProgressJobTests`의 `Context(…)` 도우미(`:550`)와 그것을 쓰는 생성 줄(`:242·273·283·306`).
- 동작이 바뀌어 대조해 맞추거나 지울 것(우리):
  - `CombatPlanDecideTests:51·54·61`(단계 이름), 0행 시험과 "전투 없으면 아무것도 안 함" 이름의 시험.
  - `CombatProgressJobTests`: 새 Run Job 시험(`:326·334·351`), `Next_ExitBattle_…`(`:495-509`) 연쇄 모양, `RunEnd` 도우미(`:539`).
  - `CombatEntryIntegrationTests`: 표시 중 `HasBattle` 참 단언(약 `:543·561·578`), `CompletionDisplayException_StopsWithoutEndBattleOrRetry`·`DetachedCompletion_…`(정리 앞뒤가 바뀜), `…StartsNewRunThenReturnsToLobby` 셋(`:637·646·763`).
  - 방침: 걷은 동작을 확인하던 단언은 지우고(Q1), 같은 사건의 새 순서로 성립하는 단언은 그대로 두고 호출만 맞춘다.
- 다른 사람(호진님, Q2): `CombatEntryIntegrationTests.Stage.cs`의 보류 시험 4개(`:434·490·504·523`)와 공용 준비 `BeginCombatReturnAtBudget`(`:546-560`) 삭제. `StageMapNodeMovementBootSmokeTests.Victory.cs`(`:194-265`, 확인 뒤 전환을 단언)는 대조해 필요한 곳만 맞춘다.

**Phase 1 재생 확인(사용자)**
- 일반 승리 → 결과 → 확인 → StageSelect, 패배 → 로비, 포기 → 로비
- 비최종 보스 승리 → StageSelect, 최종 보스 → Run 요약 → 로비
- 결과 화면 동안 밤 진행 `0/0`, 주인공 체력 숫자(설계 Q3 A)
- Stage 복귀가 보류 장치 없이 바로 진입하는지

## Phase 2 — 묶음 A(+E1): 판단을 실행 순간으로

**목표:** 등록 때는 할 일이 있는지만 보고, 실행 순간 판단해 단계 Job을 Runner에 넘긴다. Job은 Core 서비스 하나씩만 받는다.

| Task | 파일 | 내용 |
|---|---|---|
| T2.1 | `C/Plans/CombatStepContext.cs` (새 파일 + `.meta`) | 좁은 걸음 재료: 사실 읽기, `ICombatProgressView`, 진입 서비스, 전환 서비스, Core 서비스(명령·미리보기·자동 진행·완료·챕터 진행). 그룹에서 한 번에 만드는 `From(service, lifeCycles)` |
| T2.2 | `C/Jobs/CombatNextJob.cs` (새 파일 + `.meta`) | struct. `Execute`: `runner.RegisterRange(CombatPlan.BuildStep(context))` 뒤 완료(설계 §5.1). 진행 단계 Job은 한 번 실행으로 끝나야 한다는 규칙을 `CombatStepKind` 설명에 |
| T2.3 | `C/Plans/CombatPlan.cs` | `Register(…, IJobRunner runner)` 필수 인자, `Next` = `HasWork` 거름 → `CombatNextJob`. 옛 `Next` → `internal BuildStep(CombatStepContext)` |
| T2.4 | `GlobalSystem/JobSystem/JobRegister/JobRegisterSystem.cs:70` | **연결 줄:** `CombatPlan.Register(service, jobType, _lifeCycles, _runner)` |
| T2.5 | `C/Jobs/*.cs` | E1: 명령·미리보기·자동 진행·포기·정리·챕터 완료·정지·완료 표시 Job이 그룹 대신 서비스 하나씩 받음 |
| T2.6 | `C/Jobs/CombatExitJobs.cs`, `CombatPreviewJob.cs`, `CombatAbandonJob.cs` | 걷는 확인: `CombatEndBattleJob` 앞쪽 대상 확인, 미리보기 "바뀌었으면 통과". 사후 확인은 다음 판단이 기대는 사실만(설계 §5.2) |
| T2.7 | 시험 | 아래 목록 |
| T2.8 | `docs/superpowers/specs/2026-10-10-combat-job-plan-restructure-design.md` | J1·J3: 판단은 실행 순간, 등록은 할 일 여부만, 등록과 실행 사이에 Render~UI가 돈다 |

**T2.7 시험 조정**
- `CombatProgressJobTests`: `Next_BuildsOneJobForTheDecidedStep`·`Next_ExitBattle_…`는 `BuildStep`을 부르게(허락됨). Job 생성 줄 약 26곳의 인자(그룹 → 서비스), `CombatPlan.Register` 호출(`:517·527·532·537`)에 Runner 인자.
- `CombatPlanTests`: Job 생성자 확인(`:178·181`), `Register` 호출(`:147·159·165`).
- 지움(Q1): `EndBattle_OtherCompletionThanTheExit_ThrowsBeforeCallingCore`, `Preview_AnsweredClearedOrReplacedPasses_…`의 "바뀜" 사례, `EndBattle_BrokenCleanupOrRetention_Throws`의 `resultChanged`·`charactersRemain`·`unpresented` 사례, 포기 Job의 결과 보존 단언.
- "이미 끝났으면 돌아감" 가드는 남기므로 Execute 두 번 시험 6곳은 그대로다.

**Phase 2 재생 확인:** 명령·미리보기·자동 진행·연출 대기가 지금과 같은지, Phase 1의 이탈 경로 다시 한 번.

## Phase 3 — 묶음 C: 진입

**목표:** 착지 대기가 Runner를 붙들지 않게 한다. 진입 종류와 요청 단계에 이름을 붙인다.

| Task | 파일 | 내용 |
|---|---|---|
| T3.0 | (대조만) | 시작 전 다시 대조: 착지 대기 중 Runner가 비었을 때 Stage 흐름·덱 편집 창·튜토리얼이 할 일이 없는지(설계 §6.1) |
| T3.1 | `C/CombatEntryRequest.cs` 외 7곳 | `CombatEntryKind Kind { Direct, Map }`. 번호 0/양수 해석을 이 값으로(설계 §6.2) |
| T3.2 | `C/CombatEntryRequest.cs` | 단계 전이 표 하나. `Close`는 표를 따르고, `Fail`은 종료 전 모든 단계에서 허용. 이름 있는 질문 `IsInProgress`·`IsAwaitingLanding`·`HasExited`, `MarkAwaitingLanding`, 같은 대상 판정 `IsTargetOf` |
| T3.3 | `GlobalSystem/JobSystem/JobRegister/JobRootType.cs` | **연결 줄:** `JobType.EntryTransition = 8` |
| T3.4 | `C/Plans/CombatPlan.cs`, `C/Jobs/CombatEntryTransitionJob.cs`, `C/CombatEntryService.cs` | Plan의 `EntryTransition` 경우, 전환 시도가 `Pending`이면 착지 대기를 남기고 끝남, 착지 게시 때 `Register(Combat, EntryTransition)`, Lease 해제 때 착지 대기면 F1 보고(설계 §6.1) |
| T3.5 | `C/CombatEntryService.cs`, `V/Boot/GameBootstrap.cs` | P4: 바쁨 예외를 계약 주석의 사전 조건으로, Bootstrap은 부르기 전에 확인 |
| T3.6 | `C/CombatEntryService.cs`, `C/MapCombatPresentationTicket.cs`, `C/Jobs/CombatExitJobs.cs` | 같은 대상 확인을 `IsTargetOf`로 |
| T3.7 | 시험 | 우리: `CombatProgressJobTests:400-439`(대기 갈래 → 끝남·착지 대기 표시), `CombatPlanTests`(진입 연쇄), `CombatEntryIntegrationTests`(맵 진입). 호진님(Q2): `MapCombatPresentationTests`, `CombatEntryIntegrationTests.MapMovement.cs`, `StageMapNodeMovementTests` |

**Phase 3 재생 확인:** 맵에서 전투 노드로 이동 → 착지 → 전투 진입, 개발 직행 진입, 진입 취소. 착지 중 다른 입력이 안 먹는지.

## Phase 4 — 묶음 D: View 쪽

| Task | 파일 | 내용 |
|---|---|---|
| T4.1 | `V/Combat/ICombatViewService.cs`, `CombatViewSystem.cs`, `C/CombatProgressReader.cs`, `C/Plans/CombatStepContext.cs` | D1 계약만: `ICombatViewService`가 `ICombatProgressView`를 상속하지 않고 `Progress`를 냄(`=> this`). 골격은 `Combat.Progress`만 받음 |
| T4.2 | `C/Plans/CombatPlan.cs`, `LifeCycles/Tutorial/Plans/TutorialPlan.cs` | D2: `CombatPlan.IsAwaitingPlayerCommand(phase, presented, latest, hasUnhandled)`, 튜토리얼은 그것을 부르고 사본 삭제(호진님 줄) |
| T4.3 | `V/Combat/CombatViewSystem.cs`, `C/Plans/CombatPlan.cs`, `V/Combat/Input/CombatInputPolicy.cs` | D3·2-①(a): View 자체 정지와 Plan 1행은 그대로, 같은 사실("승패 기록됨")이라는 근거와 서로를 가리키는 주석. `IsTerminal`에 같은 뜻 근거 주석 |
| T4.4 | `C/ICombatProgressView.cs`, `C/CombatProgressFacts.cs`, `C/CombatProgressReader.cs`, `CombatViewSystem.cs` | D4: `IsPlaybackBusy` 삭제, `IsPresenting = HasUnpresentedResult`. **사실 묶음 생성자를 위치 인자로 부르는 곳(`CombatProgressReader.cs:77`, `CombatProgressJobTests:559`, `TutorialPlanTests:178`, `MapCombatPresentationTests:295`)은 bool 인자가 밀려 뜻이 바뀌지 않게 먼저 이름 있는 인자로 바꾼다** |
| T4.5 | 시험 | 컴파일 맞춤: `ICombatViewService` 가짜(`CombatEntryIntegrationTests.cs:1023`, `CombatProgressJobTests.cs:700`, `CombatProgressTestDriver.cs:76`, 호진님 `LobbyDepartureIntegrationTests.cs:469`), `isPlaybackBusy:` 인자(`CombatPlanDecideTests:143·152`, `CombatProgressReaderTests:127`, 호진님 `TutorialPlanTests:180`). 튜토리얼 판정 호출(호진님 `TutorialPlanTests`) |

**Phase 4 재생 확인:** 튜토리얼 설명 시작 시점이 지금과 같은지, 전투 진행 전반.

## Phase 5 — 묶음 E: 의존·조립·연결

| Task | 파일 | 내용 |
|---|---|---|
| T5.1 | `GlobalSystem/JobSystem/JobRegister/JobRootType.cs`, `T/CombatPlanTests.cs:154·283` | **연결 줄:** `JobType.AdvanceCombat` 삭제. 시험 두 줄은 같은 번호(`(JobType)5`)로 |
| T5.2 | `Factory/Runtime/LifeCycle/LifeCycleSystemFactory.cs`, `Factory/Runtime/InGame/InGameSystemFactory.cs` | **연결 줄:** 운영 조립 자리(`CreateComposition`·`Compose`)는 경계 필수, 대체 생성은 시험용 입구(`Create`·`CreateWithSources`)로(분석 §6 4번 ②) |
| T5.3 | `V/Boot/GameBootstrap.cs` | 씬별 UI·카메라 연결 노드를 이름 있는 표 하나로, 우선순위 숫자(−200·−190·90·200)를 이름 있는 상수로, 캐스트 둘은 그 안 한 곳에 이유와 함께 |
| T5.4 | `docs/superpowers/viewDocs/runtime-scene-binding-guide.md`, `Assets/ProjectL/Script/Systems/Documentation/framework-programmer-guide.md:466` | 전체 씬 Node 순서 표(LifeCycle 0·View ±100·튜토리얼 ±110·Bootstrap 노드). 프로그래머 가이드 씬 번호를 `SceneIndices`대로(민형님 문서, 사용자 지시) |

## Phase 6 — 묶음 F: 오류·작은 것·문서

| Task | 파일 | 내용 |
|---|---|---|
| T6.1 | `V/Combat/CombatViewSystem.cs`, `CombatSceneRoot.cs` | P30: Combat 활성 중 씬 루트가 꺼지면 F1 보고(런타임 종료·앱 종료 중 제외) |
| T6.2 | `C/Plans/CombatPlan.cs`, `TutorialPlan.cs` | `NoJobs` "바꾸지 않는다" 주석(기반 서명 때문에 수용) |
| T6.3 | `C/CombatCompletionIdentity.cs` (새 파일 + `.meta`) 외 | "같은 전투의 완료인가" 판정 하나, Core와의 약속을 계약 주석에(P12) |
| T6.4 | `V/Boot/GameBootstrap.cs`, `CLAUDE.md` | "Core가 비전투 진입을 거절" → "예외" |
| T6.5 | `runtime-scene-binding-guide.md` | 튜토리얼이 같은 프레임에 전투보다 먼저 Runner를 잡는 순서를 의도된 규약으로 |
| T6.6 | 재구성 설계 J7, `CLAUDE.md` "Run 종료 복귀 정책" 줄 | 새 Run은 로비 출발만, 이탈 순서는 모든 결과가 같음 |

## Phase 7 — 변경 정리 넘기기 (미전달로 작성)

| 받는 사람 | 문서 | 내용 |
|---|---|---|
| 호진님 | 새 `docs/superpowers/handoffs/2026-10-2x-combat-view-structure-hojin-share.md` | 바꾼 호진님 줄(보스 줄·보스 Job, View 결과 유지 분기, Lease·Ticket·진입 전환·준비 Job 맵 분기, Stage 보류 장치, `TutorialPlan` 판정 호출), 고친 시험 목록, 범위 밖 제안(P7·P8·P10 소비자를 이름 있는 질문으로, P27, P29 생성자 셋, Lobby 진입 조건, StageMap 신호, ④ 수락된 전환 중) |
| 현강님 | 새 공유 메모 | `ICombatViewService` 계약 변경, `CombatViewSystem` 줄, 결과 화면 동안의 주인공 체력 표시 |
| 천명님 | Core 전달 문서 새 절(§30) | 우리가 기대는 Core 동작(정리 뒤 완료·결과 순번·인스턴스 유지, 완료 참조 동일성, 보스 승리 Run 종료가 `CompleteChapter` 뒤, 비전투 노드 진입은 예외). 요청 없음. `ICombatViewService`의 천명님 줄 |
| 민형님 | 10-10 Job·Plan 공유 문서 새 절 | 연결 줄: `AdvanceCombat` 삭제, `EntryTransition` 추가, Combat 등록 줄이 Runner를 넘김, Factory 대체 생성 이동. `CombatNextJob` 사용 방식. 가이드 §10 씬 번호 고침 |

## 커밋 묶음(사용자가 함)

- Phase마다 한 묶음을 권장한다.
- 새 파일은 `.meta`와 함께: `Plans/CombatStepContext.cs`·`Jobs/CombatNextJob.cs`(Phase 2), `CombatCompletionIdentity.cs`(Phase 6).
- 지우는 파일도 `.meta`와 함께: `Plans/CombatExitContext.cs`(Phase 1).

## 실행 기록

(Phase마다 여기에 적는다: 바꾼 파일, 계획과 다른 점, 고친·지운 시험, 다른 사람 줄, 컴파일 미확인.)

### Phase 1 (묶음 B) — 2026-10-22, 코드 완료, **컴파일 미확인**(MSBuild 안 돌림, 새 `[Test]` 없음)

**바꾼 파일**

| 파일 | 바뀐 것 |
|---|---|
| `C/CombatProgressFacts.cs` | 끝 선택 인자 `runOutcome`(기본 `InProgress`)·속성 `RunOutcome` |
| `C/CombatProgressReader.cs` | Run 진행 읽기 싱크 `IRunProgressBufferReadSink`를 `PostBind`·`Bind`(View 앞 인자)·`Read`에 |
| `C/Plans/CombatStep.cs` | `PresentCompletion` → `Settle`(2), `ExitBattle` → `Leave`(3), 설명 |
| `C/Plans/CombatPlan.cs` | `Decide` 0행 삭제·승패 블록 맨 앞·`None`이면 `HasBattle ? Settle : Nothing`·그 뒤 `!HasBattle → Nothing`. `Next`에 `Settle`(EndBattle → 보스 승리면 챕터 완료 → 표시)·`Leave`(전환 하나). `RequiresChapterCompletion`·`DestinationOf(RunOutcome)` 추가. `NewRunLoadoutKey`·`ContinueDestination`·`RunEndDestination`·`PresentCompletion`·`ExitBattle` 함수 삭제 |
| `C/Jobs/CombatExitJobs.cs` | `CombatCompletionCheckJob`·`CombatRunOutcomeJob`·`CombatNewRunJob` 삭제. `CombatRunEndJob` → `CombatChapterCompletionJob`(완료를 받음, `CompleteChapter` 한 번, 거절은 예외). `CombatEndBattleJob`은 기대 결과 순번을 받고 정리 전후를 지역 값으로 대조. `CombatTransitionJob`은 목적지 키를 받음. `ExitCheck` 설명 |
| `C/Plans/CombatExitContext.cs` (+ `.meta`) | 삭제(작업 트리에서만, 색인은 건드리지 않음) |
| `C/ICombatProgressView.cs` | `PresentCompletion` 설명 "모든 결과는 정리 뒤" |
| `V/Combat/CombatViewSystem.cs` | 결과 유지 조건을 `IsHoldingResult()` 하나로(보스 주석 제거), `PresentCompletion` 설명 |
| `C/CombatLifeCycle.cs` | `OnExit`: 오류면 끝, `TryCloseExited()`가 거짓일 때만 `ExitCheck` |
| `C/CombatEntryService.cs` | `TryCloseExited()`: 전투가 없으면 현재 요청을 닫고 참 |
| `LifeCycles/Stage/StageLifeCycle.cs` | 보류 장치 삭제: `_pendingCombatReturn`·`TryDeferCompletedCombatReturn`·`OnEnter` 보류 갈래·`OnTick` 보류 갈래·`StopAndCancelEntry`의 비우기·`HasPendingCombatReturn` 사실 |
| `LifeCycles/Stage/IStageDeckEditFacts.cs`, `StageDeckEditWindow.cs` | `HasPendingCombatReturn` 삭제, 창 조건은 `!IsRunnerIdle`만 |

**계획과 다른 점**
1. 이탈 Job 셋의 생성자는 기존처럼 끝에 `priority`를 받는다(설계 코드에는 없음, 모든 전투 Job과 같은 꼴).
2. `CombatEndBattleJob`의 정리 전 확인은 `HasBattle`·`HasCompletion`·순번 셋이다(설계는 순번만 적음). 정리 뒤 보존 확인(전투 없음·활성 캐릭터 0·같은 완료·같은 최신 결과·같은 순번·P ≥ S)은 그대로 두었다. 걷는 것은 Phase 2(P15).
3. `StageLifeCycle.StartStageEntry`는 보류 장치와 함께 생긴 도우미라(`dce453db`) `OnEnter` 안으로 되돌렸다. 보류에만 쓰던 `_combatProgress` 필드·바인딩·종료 비우기도 지웠다.
4. 보스의 완료 대상·최종 결과 확인(`RequireCompletion`)이 이제 정리·챕터 완료 **뒤** 표시 Job에서 돈다(옛 `CombatCompletionCheckJob`이 정리 앞에서 하던 것). 어긋나면 F1로 멈추는 것은 같고, 멈추기 전에 Core 정리가 끝나 있다는 점만 다르다.
5. `Decide`에서 "완료는 있고 전투는 없으며 제출이 열린" 사실은 이제 제출 정지를 고른다(전에는 0행이 아무것도 안 함). 실제 흐름에서는 정리 전에 제출이 이미 닫혀 있어 생기지 않는다.
6. `TryCloseExited`는 사실 묶음 전체(`Read`)를 읽으므로 옛 `ExitCheck`처럼 전투 View 계약이 조립돼 있어야 한다.

**고친·지운 시험**(방침: 걷은 동작 단언은 지우고, 새 순서로 성립하는 것은 호출만)
- `CombatPlanDecideTests`(우리): `NoBattle_DoesNothing_EvenWithCompletionOrAbandonRequest` → `NoBattle_WithoutCompletion_DoesNothing_EvenWithAbandonRequest`(완료 부분 지움, 위 5), 단계 이름 세 곳.
- `CombatProgressJobTests`(우리, 한 시험만 천명님 줄):
  - `EndBattle_RetainsResultAndSequence_AndChecksCleanup` → `EndBattle_KeepsResultAndSequence_AndChecksCleanup`(컨텍스트 보관 단언 둘 지움)
  - `EndBattle_BrokenCleanupOrRetention_Throws` 호출만
  - `EndBattle_OtherCompletionThanTheExit_ThrowsBeforeCallingCore` → `EndBattle_OtherSequenceThanTheDecision_ThrowsBeforeCallingCore`(이탈 대상 식별이 완료 참조에서 결과 순번으로)
  - `RunEnd_BossVictory_AsksChapterCompletionOnce` → `ChapterCompletion_BossVictory_AsksChapterCompletionOnce`(Run 끝 판정 단언 지움)
  - `RunEnd_BossVictoryRejection_Throws` → `ChapterCompletion_Rejection_Throws`(천명님 줄 `b6c8513e`, 호출만)
  - `Transition_GoesToTheDestinationOfTheRunDecision` 호출만(`CombatPlan.DestinationOf`)
  - `Next_BuildsOneJobForTheDecidedStep`의 `"present"` 사례 삭제(정리·표시는 Job 둘 이상)
  - `Next_ExitBattle_BuildsTheChainInOrder_…` → `Next_Settle_BuildsTheChainInOrder_AndTheRealRunnerFinishesItInOneTick`(연쇄 `[EndBattle, Completion]`, 마지막 단언 = 같은 완료 표시)
  - 삭제: `RunEnd_OtherResults_ReadCoreRunOutcome`, `NewRun_OnlyWhenRunEnds_StartsWithTheLoadoutAndChecksTheFreshRun`, `NewRun_RejectedOrNotFresh_Throws`, `ExitContext_RunDecisionIsReadOnlyAfterItIsMadeOnce`, 도우미 `RunEnd`·`Context`
- `CombatProgressReaderTests`·`CombatProgressTestDriver`(우리): `Bind` 인자만(8곳). Run 싱크 null 거절 단언은 더하지 않았다.
- `CombatEntryIntegrationTests`(우리 + 천명님·호진님 줄):
  - `NextEntry_…`: 표시 중 `HasBattle` 참 단언 지움, 메시지·주석 셋
  - `CompletionDisplayException_StopsWithoutEndBattleOrRetry` → `CompletionDisplayException_StopsWithoutRetry`(`HasBattle` 참 지움)
  - `DetachedCompletion_CannotAcknowledgeOrTransition`: `HasBattle` 참 지움
  - 이름에서 새 Run 걷음: `FinalChapterVictory_RunCompleted_ReturnsToLobby`(천명님), `Defeat_EndsRun_ReturnsToLobby`(호진님), `Abandon_InDeployment_RecordsAbandonmentThenReturnsToLobby`(천명님)
  - 도우미 `AssertNewRunThenReturn` → `AssertRunEndThenReturn`(천명님·호진님 줄): 새 Run 상태 단언 일곱 지움(Run 결과 `InProgress`·맵 없음·골드 0·메인 상태·처치 수 0·쓴 골드 0·이번 Run 명성 0). 목적지 로비·오류 없음·Runner 빔·전투 없음·Run 요약 넷은 그대로
  - `Abandon_WhenQueuedAttackWinsFirst_…` 주석만
- `CombatEntryIntegrationTests.Stage.cs`(호진님 `dce453db`, Q2): `Shutdown_DuringPendingCombatReturn_…`, `NormalCombatReturn_FastLoadAfterRunnerBudget_…`, `DeferredCombatReturn_SharedFaultDoesNotRegisterStagePreparation`, `DeferredCombatReturn_ShutdownClearsPendingPreparationWithoutRegisteringJobs`, 도우미 `BeginCombatReturnAtBudget` 삭제
- `StageMapNodeMovementBootSmokeTests.Victory.cs`: 대조만, 고칠 곳 없음(확인 전 Combat 씬 유지·확인 뒤 Stage 복귀는 새 순서에서도 같다)

**다른 사람 줄(Phase 7 변경 정리에 넣을 것)**
- 호진님: `CombatPlan` 보스 줄(0행·`PresentCompletion` 보스 갈래·`ExitBattle` 두 갈래), `CombatCompletionCheckJob`·`CombatRunOutcomeJob`(`cb76bd2c`), `CombatViewSystem` 결과 유지 분기, Stage 보류 장치(`dce453db`), 위 시험 줄
- 천명님: `CombatRunEndJob` 거절 처리(`b6c8513e`), 위 시험 줄
- 덱 편집 창 줄(`38869ec1`)은 우리 줄이다

**보고만(고치지 않음)**
- `StageMapNodeMovementBootSmokeTests.OperationalBoot_RejectShopRecoveryCombatAndAbandonReturn_RespectLanding`(호진님 `70de0066`, 10-16)은 포기 뒤 StageSelect 복귀를 기다린다. 10-20 정책(포기 → 로비, `b6c8513e`)부터 이미 맞지 않던 것이라 이번 변경과 무관하다.
- 결과 화면 동안 전투가 없는 것으로 읽는 곳(설계 §4.7)에 둘을 더 찾았다: `TutorialRequestJob.cs:74`(그때 시작하는 튜토리얼 세션은 전투 인스턴스 0으로 기록), `TutorialViewFocusLocator.cs:304·336`(현재 행위자·적 본부 찾기 실패). 둘 다 결과 화면에서 튜토리얼이 시작되거나 전투 대상을 찾을 때만 해당한다.
- `CLAUDE.md` "Run 종료 복귀 정책" 줄과 재구성 설계 J7은 아직 "새 Run 준비 후 로비"·`CombatPlan.RunEndDestination`이라 적혀 있다. 계획대로 Phase 6(T6.6)에서 고친다.

**커밋 묶음:** 위 코드 파일 열둘 + 시험 여섯, 삭제 `Plans/CombatExitContext.cs`·`.meta`. 새 파일 없음.

### Phase 2 (묶음 A + E1) — 2026-10-22, 코드 완료, **컴파일 미확인**(MSBuild 안 돌림, 새 `[Test]` 없음)

Phase 1 Unity 확인 문제 없음(사용자). Phase 1 변경은 커밋 전인 채 그 위에서 진행했다.

**바꾼 파일**

| 파일 | 바뀐 것 |
|---|---|
| `C/Plans/CombatStepContext.cs` (새 + `.meta`) | 좁은 걸음 재료: 사실 읽기(필수), View 계약·진입 서비스·전환 서비스·Core 서비스 다섯(명령·미리보기·자동 진행·완료·챕터 진행). `From(service, lifeCycles)` |
| `C/Jobs/CombatNextJob.cs` (새 + `.meta`) | struct. `Execute` = `runner.RegisterRange(CombatPlan.BuildStep(context))` 뒤 완료 |
| `C/Plans/CombatPlan.cs` | `Register(…, IJobRunner runner)` 필수 인자. `Next` = 사실을 읽어 `Decide`가 `None`이면 빈 목록, 아니면 `CombatNextJob` 하나. 옛 `Next` 본문 → `internal BuildStep(CombatStepContext)`. `Settle`은 걸음 재료에서 꺼냄. `RequireView` 삭제. 포기 줄 주석에 실행 순간 판단 |
| `C/Plans/CombatStep.cs` | 단계 Job 규칙(한 번 실행으로 끝나거나 예외), 자동 진행 번호는 "판단 때의 S" |
| `C/Jobs/CombatCommandJob.cs`·`CombatPreviewJob.cs`·`CombatAdvanceJob.cs`·`CombatAbandonJob.cs`·`CombatExitJobs.cs` | E1: 그룹 대신 서비스 하나(`ICombatCommandService`·`ICombatPreviewService`·`ICombatAdvanceService`·`IBattleCompletionService`·`IChapterProgressionCoordinator`) |
| 같은 파일들 | T2.6 걷은 확인: 정리 Job의 정리 전 대상 확인, 사후 확인은 전투 없음·S 그대로만(최신 결과 참조·활성 캐릭터 0·P ≥ S 뺌). 미리보기 Job의 "그 사이 요청이 바뀌었으면 통과" 뺌(응답 번호 ≥ 요청 번호만). 포기 Job은 포기 완료 기록만(결과·순번 보존 뺌, 호출 전 읽기 없음) |
| `GlobalSystem/JobSystem/JobRegister/JobRegisterSystem.cs:70` | **연결 줄(우리 줄 `bf7c83da`):** `CombatPlan.Register(service, jobType, _lifeCycles, _runner)` + 주석 한 줄. 민형님 공유 문서는 Phase 7 |
| `docs/superpowers/specs/2026-10-10-combat-job-plan-restructure-design.md` | T2.8: J1 개정 줄(판단은 실행 순간, 등록은 할 일 여부만, 걸음 재료, 포기 순서), J2 머리에 Phase 1로 바뀐 0·4·6행 안내, J3 미리보기 행·"사이에 모듈 안 돎" 정정·사후 확인 범위 |

**계획과 다른 점**
1. **걸음 재료는 사실 읽기만 등록 때 요구하고, 나머지는 그 재료가 필요한 단계를 만들 때 요구한다**(없으면 그때 `InvalidOperationException`). 처음엔 등록 때 모두 요구했는데, 시험 조립(`CombatPlanTests.FakeInGameService`는 View 그룹·챕터 진행이 없고 `CombatProgressTestDriver`는 전환·진입 서비스를 안 넘길 때가 있다)이 단계와 상관없이 멈췄다. 그룹에서 꺼내 쓰던 지금 코드와 같은 시점에 멈추는 것으로 맞췄다.
2. 정리 Job은 기대 결과 순번을 계속 받는다(판단 때 S). 정리 뒤 "S 그대로"를 호출 전 읽기 없이 확인하려고.
3. 명령·미리보기·자동 진행 Job의 `Execute` 안 null 검사는 이름만 바꿔 남겼다(기본값 struct 대비, 계획에 없는 일이라 걷지 않음).
4. `ICombatProgressReader.ActiveCharacterCount`는 정리 Job이 활성 캐릭터 0을 더는 보지 않아 운영 코드에서 쓰는 곳이 없어졌다. 계약·시험 가짜를 바꾸는 일이라 지우지 않고 남겼다(Phase 6 작은 정리 후보).
5. **설계에 없던 동작 차이(10-22 사용자 결정: 받아들임):** 등록 뒤 실행 전 한 프레임 사이에 들어온 포기 확인이 이제 이긴다. 전에는 등록된 명령을 먼저 처리했다(K22 A5). 확인창이 열린 동안 입력이 막혀 있어 실제로는 "제출해 둔 명령이 연출을 기다리다 연출이 끝난 바로 다음 프레임에 포기 확인"에서만 생긴다. 판단표 주석과 재구성 설계 J1 개정 줄에 적었다.

**고친·지운 시험**(모두 우리 시험)
- `CombatProgressJobTests`:
  - Job 생성 인자 그룹 → 서비스(명령 2·미리보기 1·자동 진행 1·포기 4·정리 2·챕터 완료 2), 시험 묶음에 Runner 하나, `Register` 호출 셋에 Runner 인자
  - `Next_BuildsOneJobForTheDecidedStep` → `BuildStep_BuildsOneJobForTheDecidedStep`, `Next_Settle_…` → `BuildStep_Settle_…`(호출을 `BuildStep`으로)
  - `Next_RequiresLifeCycleGroupAndCombatView` → `Next_RequiresLifeCycleGroup_AndBuildStepRequiresCombatView`(View 요구가 등록에서 단계 만들기로 옮겨져 둘째 단언이 `BuildStep`을 부름)
  - `Preview_AnsweredClearedOrReplacedPasses_SameUnansweredRequestThrows` → `Preview_AnsweredPasses_UnansweredRequestThrows`(비움·바뀜 사례 둘 지움)
  - `Abandon_Abandoned_KeepsResultAndSequence_AndRecordsAbandonment` → `Abandon_Abandoned_RecordsAbandonment`, `Abandon_Abandoned_WithChangedResultOrWrongCompletion_Throws` → `Abandon_Abandoned_WithWrongCompletion_Throws`(결과 바뀜 사례 지움)
  - `EndBattle_KeepsResultAndSequence_AndChecksCleanup` → `EndBattle_KeepsSequence_AndChecksCleanup`, `EndBattle_BrokenCleanupOrRetention_Throws` → `EndBattle_BattleLeftBehind_Throws`(`charactersRemain`·`resultChanged`·`unpresented` 지움)
  - 삭제: `EndBattle_OtherSequenceThanTheDecision_ThrowsBeforeCallingCore`(Phase 1의 옛 `…OtherCompletionThanTheExit…`)
- `CombatPlanTests`: `Register` 호출 셋에 Runner, 생성자 확인 넷의 가짜를 `CombatProbe`로
- 계획에 없던 것(위 5와 같은 원인, 실행 순간 판단으로 더는 성립하지 않음, Q1):
  - `CombatEntryIntegrationTests`: `Abandon_WhileCommandIsQueued_ProcessesItFirstThenAbandonsOnce`·`Abandon_WhenQueuedAttackWinsFirst_PresentsVictoryAndKeepsRun` 삭제, 그 시험만 쓰던 `WinBattle`의 콜백 인자 삭제. 승패가 먼저 기록되면 그 승패로 나가는 규칙(K22 A4)은 `Abandon_IsIgnored_AfterARecordedOutcome`·`CombatPlanDecideTests.Completion_WinsOverAbandonRequest`가 계속 본다
  - `CombatProgressIntegrationTests`: `CommandRemovedAfterRegistration_JobThrowsAndKeepsTheJob_WithoutPublishing` 삭제(등록 뒤 명령을 지우면 이제 실행 순간 할 일이 없어 Job을 만들지 않는다)
- 대조만, 고칠 곳 없음: `CombatProgressIntegrationTests`의 나머지(미리보기 교체·비움, 명령 우선, 자동 진행 대기·결과 실패 주입), `CombatMovementViewTests`·`CoreStageCombatCycleTests`의 드라이버 사용(등록 → 같은 Tick에 판단·실행이라 프레임 수가 같다)

**보고만(고치지 않음)**
- `TutorialPlan.cs:18`(호진님) 설명 "`CombatPlan`과 같은 형태 … `Next`는 사실 묶음을 한 번 읽어 판단"은 이제 전투 쪽과 다르다(튜토리얼은 그대로 등록 때 판단). Phase 4의 튜토리얼 판정 정리(T4.2) 때 같이 볼 것.
- 작업 트리의 FMOD 에셋 두 개(`New Fmod Event Catalog.asset`·`FMODStudioCache.asset`) 변경은 에디터가 만든 것으로 이번 범위가 아니다.

**커밋 묶음:** 코드 파일 열 + 새 파일 둘(`Plans/CombatStepContext.cs`·`Jobs/CombatNextJob.cs`, `.meta`와 함께) + 시험 넷 + 재구성 설계 문서. Phase 1 묶음이 커밋 전이면 함께(같은 파일이 겹친다: `CombatPlan.cs`·`CombatStep.cs`·`CombatExitJobs.cs`·시험 셋).

### Phase 3 (묶음 C) — 2026-10-22, 코드 완료, **컴파일 미확인**(MSBuild 안 돌림, 새 `[Test]` 없음)

Phase 2 Unity 확인 문제 없음(사용자). Phase 1·2 변경은 커밋 전인 채 그 위에서 진행했다.

**T3.0 대조 결과(착지 대기 중 Runner가 빌 때):**
- Stage 흐름은 할 일이 없다 — `StageFlow.Tick`이 전투 요청 확인(`StageNodeEntryFlow.TryConfirmPending`)에서 활성·거절 전이면 멈추고, 그 뒤 상점·회복·새 맵 입력을 등록하지 않는다.
- 덱 편집 창은 열리지 않는다 — Runner가 비어도 `HasPendingFlowCheck`(맵 요청 처리 확인 대기)로 `JobPending`.
- 튜토리얼은 Runner가 빌 때마다 `Register(Tutorial, Next)`를 부르지만 그 판단이 할 일을 정한다. StageSelect에서 Runner가 비는 다른 프레임에도 원래 그래 왔으므로 새로 생기는 일은 없다. 그 사이 튜토리얼 Job이 끼어도 착지 게시가 거는 전환은 그 뒤에 줄을 선다(같은 우선순위는 먼저 넣은 것 먼저).

**바꾼 파일**

| 파일 | 바뀐 것 |
|---|---|
| `C/CombatEntryRequest.cs` | 진입 종류 `CombatEntryKind { Direct, Map }`·`Kind`(번호 0/양수 해석을 이 값으로). 단계 전이 표 `CanMove` 하나와 `Move`(등록·실행·수락·거절·취소·활성화 시작·활성·닫힘이 모두 지남). `Close`는 활성 요청만, `Fail`은 끝나지 않은 모든 단계에서 받고 예외를 내지 않음(닫힘·실패면 그대로). 이름 있는 질문 `IsInProgress`·`IsAwaitingLanding`·`HasExited`, 같은 대상 판정 `IsTargetOf(chapter, node)`·`IsTargetOf(chapter, node, stage)`(정수 값 비교), `MarkAwaitingLanding`, `UsesPresentationOwner`. 진입 서비스 계약 설명에 진입 요청의 사전 조건 |
| `C/MapCombatPresentationTicket.cs` | `IsOwnedBy(lease)` |
| `C/CombatEntryService.cs` | 착지 게시(`TryCompleteMapPresentation`)가 착지 대기 요청이면 `Register(Combat, EntryTransition)`. 표시 소유권 해제 때 착지 대기 요청이 그 소유권을 쓰면 `Fail` + 오류 경계 보고(입력 정리 중·서비스 종료 중은 제외). 맵 입력 대조·중복 요청·활성화 확인을 `IsTargetOf`로, `Kind`로 맵 진입 판단, `IsFinished`는 `HasExited` |
| `C/Plans/CombatPlan.cs` | `JobType.EntryTransition` 경우: 현재 요청이 착지 대기면 전환 Job 하나, 아니면 예외 |
| `C/Jobs/CombatEntryTransitionJob.cs` | 착지 전(`Pending`)이면 `MarkAwaitingLanding` 뒤 완료(Runner를 붙들지 않음) |
| `C/Jobs/CombatPrepareJob.cs` | 직행 판단을 `Kind`로 |
| `C/Jobs/CombatExitJobs.cs` | 완료 표시 Job의 대상 확인을 `IsTargetOf`로 |
| `GlobalSystem/JobSystem/JobRegister/JobRootType.cs` | **연결 줄(우리 줄):** `JobType.EntryTransition = 8`. 민형님 공유 문서는 Phase 7 |
| `RuntimeSystem/Common/RuntimeFaultBoundary.cs` | `IsStoppingInput`(공용 오류 경계, 우리 줄): 입력 정리 콜백이 도는 중인지 읽기만 |
| `V/Boot/GameBootstrap.cs` | T3.5: 개발 직행 진입을 `TryRequestDevEntry`로 — Runner가 비고 로딩 중이 아닐 때만 부르고, 아니면 다음 Update에 다시 본다(우리 줄) |

**계획과 다른 점**
1. **입력 정리 중의 소유권 해제는 보고하지 않는다.** 정상 종료 때 Bootstrap이 런타임 종료 전에 입력 정리(`StopInputForShutdown`)를 먼저 돌리는데, 그때 StageMap 표시가 소유권을 놓는다. 설계대로면 착지 이동 중에 앱을 끄면 정상 종료가 오류로 기록된다. 그래서 오류 경계에 `IsStoppingInput`(읽기 전용)을 더하고, 그 동안의 해제는 그 정리가 맡게 했다. 오류 정지 때는 이미 오류 상태라 원래 보고하지 않는다.
2. `Close`를 표에 넣으면서 **닫기는 활성 요청만** 받는다(운영에서는 Combat을 나가는 요청이 늘 활성이다). 우리 시험 둘이 수락 상태 요청을 닫고 있어 요청을 활성까지 진행시키게 맞췄다.
3. `Fail`은 이미 닫힌 요청을 실패로 바꾸지 않는다(전에는 바꿨다). catch에서 원래 예외를 가리지 않게 하는 설계 목적과 같고, 시험이 기대하는 곳은 없다.
4. 같은 대상 판정은 정수 값으로 비교한다. 현재 선택의 스테이지 키는 시작 노드에서 0일 수 있는데 `StageKey`·`StageNodeId` 생성자가 0을 거부해서다.
5. T3.6의 `MapCombatPresentationTicket`에는 대상(챕터·노드·스테이지) 비교가 없어 `IsTargetOf`로 바꿀 곳이 없었다. 소유권 확인만 `IsOwnedBy`로 더했다.
6. 착지 게시 뒤 전환 전 한 프레임 동안에도 `IsAwaitingLanding`이 참이라, 그 사이 소유권을 잃으면 다음 전환 시도가 아니라 잃는 순간 멈춘다(결과는 같은 F1).

**고친 시험**
- `CombatProgressJobTests`(우리): `ExitCheck_BattleLeftBehind_Throws_OtherwiseClosesTheEntry`·`ExitCheck_BuildsTheCheckForTheCurrentEntry`가 쓰는 요청을 `Active()`(수락 → 활성화 시작 → 활성) 도우미로. 확인 내용 그대로
- 호진님(Q2):
  - `MapCombatPresentationTests.Pending_DoesNotActivate` → `Pending_EndsAwaitingLanding_WithoutActivating`(Job 완료 참·착지 대기 참, 전환 없음은 그대로)
  - `CombatEntryIntegrationTests.MapMovement.cs`의 `DetachDuringAcceptedMovement_AbandonsGateWithoutChangingCoreReceipt`: "다음 프레임이 예외" 단언을 "그 자리에서 오류 상태"로
  - 같은 파일 `CurrentLandingReadyPublishFailure_FaultsThroughExistingBoundary` → `CurrentLandingLeaseLoss_FaultsThroughExistingBoundary`: 소유권을 지우는 주입이 이제 그 자리에서 F1이라, "Scope가 아직 일함·착지 게시 실패 예외" 단언 대신 오류 상태·첫 오류 문구("소유권")를 본다. 표시 폐기 기대는 소유권 해제 앞으로 옮김. Core 수락 결과·현재 노드·로드 0은 그대로
- 대조만, 고칠 곳 없음: 맵 진입 통합 시험(`MapEntry_*`, 착지 게시 → 다음 프레임 전환으로 프레임 수 같음), `RealPrepareAndTransition_SameRunnerTick_WaitsForLanding`, `StageLifeCycle_SynchronousExitRequiresOwnedActivatingRequest`, `StageMapNodeMovementTests.DisposedCombatLeaseAtLanding_…`(서비스 초기화 전이라 보고 없음), `StopAndFault_…`(직접 정지하면 이제 소유권 상실 F1도 함께 나지만 단언에 영향 없음), `StageFlowTests`의 진입 단계 직접 진행(표의 전이와 같음)

**다른 사람 줄(Phase 7 변경 정리에 넣을 것)**
- 호진님(`edcda135`): 요청의 맵 표시 상태·착지 게시·표시 연결, 서비스의 착지 게시·소유권 해제, Ticket, 전환 Job의 대기 갈래. `Leehojin` 줄: 맵 입력 대조, 준비 Job의 맵 명령 갈래. 위 시험 셋
- 이름 있는 질문으로 바꿀 수 있는 다른 영역 소비자(고치지 않음, 제안): `StageNodeEntryFlow.cs:56-77`, `StageLifeCycle.cs:169-192`, `StageMapEntryObserver.cs:42-113`, `StageMapNodeMovement.cs:57-59`

**커밋 묶음:** 코드 파일 열 + 시험 셋. 새 파일 없음. Phase 1·2 묶음이 커밋 전이면 함께(`CombatPlan.cs`·`CombatExitJobs.cs`·`CombatEntryService.cs`·`CombatProgressJobTests.cs`가 겹친다).

### Phase 4 (묶음 D) — 2026-10-22, 코드 완료, **컴파일 미확인**(MSBuild 안 돌림, 새 `[Test]` 없음)

Phase 3 Unity 확인 문제 없음(사용자). Phase 1~3 변경은 커밋 전인 채 그 위에서 진행했다.

**D4 전제 대조(코드):** View는 새 결과를 관측했을 때만 재생을 시작하고(`CombatViewSystem.ConsumeResult`), 확인 번호 P를 올리는 것(`AcknowledgePresented`)과 재생 상태를 비우는 것(`CompleteFinalization`)이 같은 후처리 호출 안에서 이어진다. 중단(Abort)은 P를 올리지 않고 상태만 비운다. 그래서 골격이 읽는 어느 시점에서도 "재생 중이면 P < S"이고, 재생 중 칸 없이 `P < S`만으로 연출 대기를 판단할 수 있다.

**바꾼 파일**

| 파일 | 바뀐 것 |
|---|---|
| `V/Combat/ICombatViewService.cs` | D1: `ICombatProgressView`를 상속하지 않고 `ICombatProgressView Progress { get; }`를 냄 |
| `V/Combat/CombatViewSystem.cs` | `ICombatProgressView`를 직접 구현, `Progress => this`, `IsPlaybackBusy` 삭제, `SyncSubmissionState`에 Plan 1행과 같은 사실이라는 근거 주석(D3) |
| `V/Combat/Input/CombatInputPolicy.cs` | `IsTerminal`에 "Finished와 완료는 한 거래로 함께 게시 → 같은 사실, Plan 1행도 같은 사실" 근거 주석(D3·2-①(a)) |
| `C/ICombatProgressView.cs` | D4: `IsPlaybackBusy` 삭제, 묻지 않는 이유를 계약 설명에 |
| `C/CombatProgressFacts.cs` | `isPlaybackBusy`·`IsPlaybackBusy` 삭제, `IsPresenting = HasUnpresentedResult` |
| `C/CombatProgressReader.cs` | View는 `Combat?.Progress`로 받음, 사실 묶음 생성을 이름 있는 인자로 |
| `C/Plans/CombatStepContext.cs` | View는 `Combat?.Progress`로 받음 |
| `C/Plans/CombatPlan.cs` | D2: `IsAwaitingPlayerCommand(phase, presented, latest, hasUnhandled)`(규칙 원본). 1행에 View 자체 정지와 같은 사실이라는 근거 주석(D3) |
| `LifeCycles/Tutorial/Plans/TutorialPlan.cs` | 튜토리얼 설명 시작 조건이 `CombatPlan.IsAwaitingPlayerCommand`를 부름, 튜토리얼 쪽 사본 삭제(호진님 줄) |

**계획과 다른 점**
1. D3은 결정 2-①(a)대로 **주석만** 달았다(코드는 그대로). View는 `Finished ∨ 완료`, Plan은 `완료`로 판단하지만 Core가 둘을 한 거래로 함께 게시하므로 같은 사실이다. 설계 본문의 "같은 판정을 부르게 한다"는 계획(T4.3)의 범위로 줄였다.
2. 위치 인자로 사실 묶음을 만들던 곳 중 `MapCombatPresentationTests:295`는 첫 인자 하나(`new CombatProgressFacts(false)`)뿐이라 그대로 두었다. `TutorialPlanTests:178`은 앞 세 인자만 위치 인자라 밀리지 않는다.

**고친·지운 시험**
- 가짜 View 넷이 골격 계약을 `Progress => this`로 냄, 재생 중 멤버 삭제: `CombatEntryIntegrationTests.PresentationConsumer`·`CombatProgressJobTests.FakeView`·`CombatProgressTestDriver.TestProgressView`(우리), `LobbyDepartureIntegrationTests.CombatViewStub`(호진님)
- 진행 계약을 View 서비스로 읽던 곳을 `.Progress`로: `StageMapNodeMovementBootSmokeTests`·`…Victory`(호진님, `CompletionDisplay` 여섯 곳), `CoreStageCombatCycleTests:246`(우리)
- `CombatProgressJobTests`의 사실 묶음 도우미를 이름 있는 인자로(우리)
- 재생 중 칸을 걷음(Q1, 우리):
  - `CombatProgressReaderTests.Read_CopiesViewState`: 재생 중 설정·단언 둘("재생 중이면 확인하지 않은 결과가 없어도 연출 중") 지움, 가짜의 재생 중 멤버 삭제
  - `CombatPlanDecideTests.Completion_WaitsForPresentationAndAttachedView`: "재생·후처리 중" 사례 지움. `Abandon_SuspendsWaitsForPresentationThenAbandonsOnce`의 재생 중 단언 하나 지움. 도우미 `Playing`·`Ended`의 `busy` 인자 삭제
- `TutorialPlanTests.StartConditions_MatchCombatPlanWaitingForPlayer`(호진님, Q2): 판정 호출을 `CombatPlan.IsAwaitingPlayerCommand`로, 재생 중 반복·인자·설명 줄 삭제

**다른 사람 줄(Phase 7 변경 정리에 넣을 것)**
- 현강님(`9354c522`): `ICombatViewService` 선언 줄. (정정, Phase 7: `CombatViewSystem`의 진행 계약 멤버 줄은 우리 줄 `bf7c83da`였다.)
- 호진님(`89cbc44e`): `TutorialPlan`의 판정 호출·사본 삭제, 위 시험 넷

**남은 것(설계 그대로):** 튜토리얼의 두 번째 사실 읽기(P16 일부, 낮음~중간), `CombatViewSystem` 클래스 분리(Q4, 뒤에 따로).

**커밋 묶음:** 코드 파일 아홉 + 시험 열. 새 파일 없음. Phase 1~3 묶음이 커밋 전이면 함께.

### Phase 5 (묶음 E) — 2026-10-22, 코드·문서 완료, **컴파일 미확인**(MSBuild 안 돌림, 새 `[Test]` 없음)

Phase 4 Unity 확인 문제 없음(사용자). Phase 1~4 변경은 커밋 전인 채 그 위에서 진행했다. E1(Job이 서비스 하나씩)·E2(View를 얻는 곳 둘이 `Combat.Progress`)는 Phase 2·4에서 끝났다.

**바꾼 파일**

| 파일 | 바뀐 것 |
|---|---|
| `GlobalSystem/JobSystem/JobRegister/JobRootType.cs` | **연결 줄(우리 줄):** `JobType.AdvanceCombat = 5` 삭제(운영 사용 0). 다른 값의 번호는 그대로, 빈 번호에 이유 주석 |
| `Factory/Runtime/LifeCycle/LifeCycleSystemFactory.cs` | **연결 줄:** 운영 조립 자리 `CreateComposition`은 오류 경계 필수(`ArgumentNullException`), 기본 경계 대체 생성은 시험용 입구 `Create`로 |
| `Factory/Runtime/InGame/InGameSystemFactory.cs` | **연결 줄:** `Compose`는 경계 필수, `CreateComposition`(운영)은 경계를 선택 인자 대신 반드시 받음, 대체 생성은 시험용 입구 `Create`·`CreateWithSources`가 `TestBoundary()`로 |
| `V/Boot/GameBootstrap.cs` | E4·E5: 씬별 진입 Node를 `BindUILifeCycle`의 이름 있는 표 하나로(등록 순서 그대로), 순서 숫자 넷을 상수(`BindUIPriority` −200·`BindCamerasPriority` −190·`PrepareCombatDeckPriority` 90·`OpenSceneUIPriority` 200), 캐스트 둘을 같은 자리에 이유와 함께(`BindLoadedUI` 삭제) |
| `docs/superpowers/viewDocs/runtime-scene-binding-guide.md` | T5.4: 전체 씬 Node 순서 표 §1.1(원본, Exit −200·−110·−100·0, Enter −200·−190·0·90·100·110·200, AfterLoad 10·100과 각 숫자의 원본 상수), §1 그림은 표를 가리키게. §6 Run 종료 설명을 지금 코드대로(새 Run은 로비 출발만) |
| `Assets/ProjectL/Script/Systems/Documentation/framework-programmer-guide.md:466` | T5.4(민형님 문서, 사용자 지시): 씬 번호를 `SceneIndices`대로(Boot 0·Lobby 1·Stage 2·Combat 3·Artifact 4 예약), 순서 표 위치 안내 |

**계획과 다른 점**
1. `InGameSystemFactory.CreateComposition`도 경계를 선택 인자에서 필수 인자로 바꿨다. 이 자리는 `Compose`로 바로 넘어가므로, 선택 인자로 두면 생략했을 때 늘 예외가 난다. 부르는 곳은 운영 `FrameworkRuntimeFactory` 하나라 시험 영향이 없다. 뿌리 `FrameworkRuntimeFactory`와 `InGameViewSystemFactory`의 대체 생성은 결정대로 그대로다.
2. Bootstrap의 UI 다시 연결은 전에는 부를 때마다 `_ui`를 캐스트했고, 이제 연결할 때 한 번 캐스트해 둔다. 캐스트가 실패하면 첫 씬 진입이 아니라 부트 때 멈춘다(더 이르게 같은 F1). 종료 뒤에는 로딩도 멈추므로 차이가 없다.
3. 씬 연결 안내 §6의 Run 종료 문장도 고쳤다(Phase 1에서 바뀐 사실, 같은 문서를 고치는 김에). 재구성 설계 J7·`CLAUDE.md` 정책 줄은 계획대로 Phase 6(T6.6).

**고친 시험**
- `CombatPlanTests`(천명님 줄 `87348b86`·`6367026e`, Q2): `[TestCase(JobType.AdvanceCombat)]` → `[TestCase((JobType)5)]`, 가짜 기록 키 `Call(JobType.AdvanceCombat)` → `Call((JobType)5)`. 확인 내용 그대로("모르는 종류는 거절", 호출 기록)
- 대조만, 고칠 곳 없음: Factory를 부르는 시험은 모두 `Create`·`CreateWithSources`(시험용 입구)를 쓴다. `ChapterContinuationServiceTests.NamedFactory_PreservesUniqueCreateAndInjectsOneCoordinator`가 세는 `InGameSystemFactory`의 `Create`는 여전히 하나다(`TestBoundary`는 다른 이름)

**다른 사람 줄(Phase 7 변경 정리에 넣을 것)**
- 민형님 영역(연결 줄, 공유 문서): `JobType.AdvanceCombat` 삭제, Factory 두 곳의 경계 필수·대체 생성 이동, 프로그래머 가이드 §10 씬 번호
- 현강님·호진님(`Twinsgm`·`Leehojin`): Bootstrap의 씬별 UI·카메라 연결 줄을 표로 옮김
- 천명님: 위 시험 두 줄

**커밋 묶음:** 코드 파일 넷 + 시험 하나 + 문서 둘. 새 파일 없음. Phase 1~4 묶음이 커밋 전이면 함께(`JobRootType.cs`·`GameBootstrap.cs`가 겹친다).

### Phase 6 (묶음 F) — 2026-10-22, 코드·문서 완료, **컴파일 미확인**(MSBuild 안 돌림, 새 `[Test]` 없음)

Phase 5 Unity 확인 문제 없음(사용자). Phase 1~5 변경은 커밋 전인 채 그 위에서 진행했다.

**바꾼 파일**

| 파일 | 바뀐 것 |
|---|---|
| `V/Combat/CombatViewSystem.cs` | T6.1(P30): 씬 루트 비활성화 콜백에서 Combat이 활성이면(`IsSceneActive`, 입력 정리 중 제외) 원인을 먼저 F1로 보고한 뒤 정리. T6.3: 완료 표시·확인 콜백·결과 유지의 같은 완료 확인 셋을 `CombatCompletionIdentity.IsSame`으로 |
| `C/CombatCompletionIdentity.cs` (새 + `.meta`) | T6.3(P12): "같은 전투의 완료인가" 판정 하나, Core와의 약속(정리·챕터 완료 뒤에도 같은 참조, 다음 준비 때 비움)을 계약 주석에 |
| `C/Plans/CombatPlan.cs`, `LifeCycles/Tutorial/Plans/TutorialPlan.cs` | T6.2(P35): 공유 빈 목록 `NoJobs`는 받는 쪽이 읽기만 한다는 약속과 기반 한계(Plan 서명) 주석 |
| `V/Boot/GameBootstrap.cs` | T6.4(P23): 개발 진입 노드 보정 설명 "Core가 거절" → "거절하지 않고 예외로 멈춤(준비 Job → F1)" |
| `docs/superpowers/viewDocs/runtime-scene-binding-guide.md` | T6.5(P1): §1.1 아래에 같은 프레임 Runner 순서 규약(튜토리얼 InGame Tick이 전투 LifeCycle Tick보다 먼저 잡음) |
| `docs/superpowers/specs/2026-10-10-combat-job-plan-restructure-design.md` | T6.6: J7 머리에 개정 블록(모든 결과 같은 순서, 목적지 `DestinationOf`, 전투 쪽 새 Run 없음, 확인 범위). 옛 표는 이력 |
| `CLAUDE.md` | T6.4: 개발 진입 줄의 "거절" 정정. T6.6: "Run 종료 복귀 정책" 줄(로비 복귀, 새 Run은 로비 출발, 이탈 순서, `DestinationOf`) |

**확인한 것(코드 대조)**
- 정상 이탈은 씬 Exit Node(View −100)에서, 앱·에디터 재생 종료는 `OnApplicationQuit`의 런타임 종료에서 씬 루트 구독을 먼저 푼다. Unity는 `OnApplicationQuit`를 씬 오브젝트의 `OnDisable`보다 먼저 부르므로 정상 경로는 새 보고에 닿지 않는다. Boot 재생 시험도 정리 때 플레이 모드를 꺼서 같은 순서다.
- 시험 중 씬 루트를 지우는 곳은 `CombatViewSystemTests`의 정리(`Dispose`)다. 이제 활성 상태면 보고가 시험 경계의 기록 목록에 쌓이지만, 단언이 끝난 뒤이고 Unity 로그를 남기지 않는다. `CombatMovementViewTests`는 View 종료 뒤 지운다.
- 같은 완료 확인은 골격 쪽에는 이제 남아 있지 않다(Phase 1·2에서 걷었다). `CombatEndUI.cs:101·176`의 참조 비교는 Run 종료 영수증 비교라 다른 사실이다.

**계획과 다른 점**
1. T6.1에서 입력 정리(오류 정지·정상 종료 전) 중의 비활성화는 보고하지 않는다(Phase 3의 소유권 상실 보고와 같은 규칙). 오류 정지 중이면 이미 오류 상태라 원래 보고하지 않는다.
2. F2(닿지 않는 `if (IsCompleted) return;` 가드)는 설계대로 남겼다.

**시험:** 고친 것 없음.

**다른 사람 줄(Phase 7 변경 정리에 넣을 것):** 호진님 `TutorialPlan`의 `NoJobs` 주석, `CombatViewSystem.cs`의 완료 확인 줄 하나(`cb76bd2c`, 결과 유지 판정).

**커밋 묶음:** 코드 파일 넷 + 새 파일 하나(`C/CombatCompletionIdentity.cs`, `.meta`와 함께) + 문서 셋. Phase 1~5 묶음이 커밋 전이면 함께.

### Phase 7 (묶음 G) — 2026-10-22, 넘기기 문서 완료(모두 미전달), 코드 변경 없음

Phase 6 Unity 확인 문제 없음(사용자).

**쓴 문서**

| 받는 사람 | 문서 | 내용 |
|---|---|---|
| 호진님 | 새 [`handoffs/2026-10-22-combat-view-structure-hojin-share.md`](../handoffs/2026-10-22-combat-view-structure-hojin-share.md) | 흐름 변화, 바꾼 호진님 줄(보스 경로·보스 Job 둘·View 결과 유지·완료 표시 설명·진입 요청/서비스/Ticket/전환 Job/준비 Job·튜토리얼 판정·Stage 보류 장치·Bootstrap 노드·InGame Factory 줄), 고친 시험 일곱 파일, 보이는 차이, 범위 밖 제안 일곱(P10·P7④·P8·P27·P29 생성자 셋·Lobby 진입 조건·StageMap 신호) |
| 현강님 | 새 [`handoffs/2026-10-22-combat-view-structure-hyeongang-share.md`](../handoffs/2026-10-22-combat-view-structure-hyeongang-share.md) | `ICombatViewService` 계약(선언 줄), Bootstrap 씬 진입 노드 표, 결과 화면 동안 보이는 차이(`NightCheckerUI`·`MainCharactorUI`·`UnitCardUI`), 씬 루트 비활성화 F1. UI 코드에서 고칠 곳은 없음 |
| 천명님 | [Core 전달 문서](../handoffs/2026-09-24-view-core-data-boundary-handoff.md) 새 절 §30 | 기대는 Core 동작 여섯(정리 뒤 남는 값, 완료 참조 동일성, 보스 Run 종료는 `CompleteChapter` 뒤, 챕터 완료 거절 = 데이터 오류, 승패·완료 한 거래, 비전투 진입은 예외), Core 호출의 변화, 고친 천명님 줄과 시험. 요청 없음 |
| 민형님 | [10-10 Job·Plan 공유 문서](../handoffs/2026-10-10-combat-job-plan-framework-share-minhyeong.md) 새 절 §6(머리말 상태 줄 한 줄) | 연결 줄 넷(`JobType`·JobRegister Combat 줄·Factory 둘), 그대로 둔 것, `CombatNextJob`이 Runner를 쓰는 방식, `IsStoppingInput`, 가이드 §10 고침, 앞 절에서 바뀐 사실 |

**다른 사람 줄을 다시 셈:** 넘기기 전에 작업 트리에서 바뀌거나 지운 줄의 주인을 HEAD 기준 `git blame`으로 다시 셌다(더하기만 한 줄은 주인이 없어 빠짐). Phase 기록과 다른 점:
- Phase 4 기록의 "`CombatViewSystem`의 진행 계약 멤버 줄 = 현강님 줄"은 틀렸다. 그 줄은 우리 줄(`bf7c83da`)이고, `CombatViewSystem`에서 바뀐 다른 사람 줄은 호진님 결과 유지·완료 비교 여섯 줄뿐이다. 현강님 줄은 `ICombatViewService` 선언 한 줄과 Bootstrap 아홉 줄이다. Phase 4 기록에 정정 표시를 달았다.
- 기록에 없던 것: `InGameSystemFactory.cs`의 시험 입구·조립 자리 다섯 줄은 호진님 줄(`55aa9a3a`·`4d6d739d`, Phase 5). Job 넷과 `CombatExitJobs`의 Core 호출 줄은 천명님 줄(`bc8b8103`, Phase 2 E1). `ICombatProgressView`의 완료 표시 설명 한 줄은 호진님 줄(`cb76bd2c`, Phase 1).
- 셈 결과는 각 넘기기 문서에 반영했다.

**남은 것(설계 그대로, 뒤에 따로):** `CombatViewSystem` 클래스 분리(Q4), 튜토리얼의 두 번째 사실 읽기(P16 일부), 운영 코드에서 쓰는 곳이 없어진 `ICombatProgressReader.ActiveCharacterCount`(Phase 2 기록 4, 지우지 않음). 넘기기 문서 전달은 사용자 판단이다.

**커밋 묶음:** 새 문서 둘 + 고친 문서 둘(Core 전달 문서·10-10 민형님 공유) + 이 계획·`CLAUDE.md`. 코드 없음.
