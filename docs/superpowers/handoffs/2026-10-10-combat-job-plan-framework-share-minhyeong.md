# 전투 진행 Job·Plan 재구성 — 민형님 공유 (2026-10-10)

- **받는 사람:** 민형님(Framework)
- **보내는 쪽:** View 작업(전투 골격 담당)
- **상태:** 작성 완료·미전달. 담당자 수신·회신은 미확인. 2026-10-22 구조 개선의 연결 줄은 §6, 2026-10-23~24 골격 점검의 연결 줄은 §7(모두 미전달).
- **코드 기준:** R1 `bf7c83da` · R2 `41be3a4d` · R3 `1e7fdda5`. 커밋은 사용자가 수행했다.
- **원본:** [재구성 설계](../specs/2026-10-10-combat-job-plan-restructure-design.md), [Task·검증 기록](../plans/2026-10-10-combat-job-plan-restructure-plan.md).

## 1. 변경 결과

`CombatLifeCycle`은 씬 Enter·Tick·Exit의 트리거만 맡는다. Live에서 오류가 없고 Runner가 비었으면 `Register(Combat, Next)`를 한 번 건다. 다음 작업의 판단은 `CombatPlan.Decide` 한 곳이며, 사실은 `CombatProgressReader`가 Buffer·View에서 새로 읽어 `CombatProgressFacts`로 전달한다. Core 호출과 호출 직후 확인은 전투 Job이 수행한다.

진입 수명은 `CombatEntryService`로 분리했다. 씬 Enter에서는 활성화를 동기로 수행해 LifeCycle(우선순위 0) → View Attach(100) 순서를 유지한다. 연출 대기는 Plan이 빈 Job 목록을 돌려주는 조건으로 처리한다. 기록 모드는 진입·활성화 뒤 `Next`를 등록하지 않는다.

## 2. 공용 파일 변경

경로는 `Assets/ProjectL/Script/Systems/` 기준이다.

| 파일 | 변경과 소비자 영향 |
|---|---|
| `RuntimeSystem/GlobalSystem/JobSystem/JobRegister/JobRootType.cs` | `JobType.Next = 6`, `ExitCheck = 7` 추가. 기존 enum 값 유지. Combat Plan은 `None`·`Next`·`ExitCheck`만 받으며 옛 `Start`·`NewRun`·`ProcessCommand`·`ProcessPreview`·`AdvanceCombat` 등록은 거부한다 |
| `RuntimeSystem/GlobalSystem/JobSystem/JobRegister/JobRegisterSystem.cs` | `PostBindService`에서 `SystemGroupsService.LifeCycle`을 받아 Combat Plan의 `Register`·`RegisterEntry`에 전달. 공개 등록 메서드 서명과 다른 루트의 Plan 등록은 유지. 테스트의 단독 조립에도 LifeCycle 그룹이 필요하다 |
| `RuntimeSystem/LifeCycleSystem/Base/LifeCycleManager.cs` | 우리 쪽이 추가했던 `ILifeCycleTransitionGuard`와 검사 호출·`ExitCore` 분리를 제거해 guard 도입 전 모양으로 복원. 잘못된 전환은 사전에 거부하지 않고 Combat Exit 훅이 등록한 `CombatExitCheckJob`에서 남은 전투를 검출해 F1으로 정지한다 |
| `ServiceKeys/SystemGroupsService.cs` | `LifeCycleSystemServiceKey.CombatProgress` 추가, `CombatExit` 제거. 기존 `CombatEntry` 키 유지 |
| `RuntimeSystem/LifeCycleSystem/LifeCycleSystemGroup.cs` | `ICombatProgressReader CombatProgress` 공개, `CombatExit` 속성 제거. `CombatEntry`의 제공자는 분리된 진입 서비스 |
| `Factory/Runtime/LifeCycle/LifeCycleSystemFactory.cs` | 읽기 객체·진입 서비스를 생성하고 제공 인터페이스로 등록. 읽기 객체의 서비스 바인딩, 진입 서비스의 바인딩·초기화·종료는 모듈 등록으로 연결 |

`CombatPlan.Register(service, jobType, lifeCycles)`와 `RegisterEntry(service, request, lifeCycles)`는 전환 관리자·현재 진입·읽기 객체를 LifeCycle 그룹에서 얻는다. JobRunner의 실행·우선순위·완료 계약은 유지하며 전투 진행에서 `Runner.Clear()`를 사용하지 않는다.

## 3. 호출자와 동작 차이

- StageMap·GameBootstrap의 `ICombatEntryService.RequestEntry`·`CancelEntry`·`IsActive` 호출 계약은 유지한다. `Current`는 읽기 전용으로 제공하며 활성화 메서드는 진입 서비스의 internal 메서드다.
- `CombatFlow`·`CombatExitSequence`, `ICombatExitService`·`ICombatAbandonRequest`·`ICombatExitPresentation`·`CombatExitStatus`를 제거했다. 옛 포기·정상 이탈 API를 새 코드에서 호출하지 않는다.
- 포기 확인은 View 요청 상태를 세우고, Plan이 남은 처리·연출 뒤 포기 Job을 선택한다. 실제 Completion이 먼저 기록되면 그 승패를 우선한다.
- 완료 표시는 View의 `None`/`Showing`/`Confirmed` 상태로 확인한다. 확인 뒤 다음 판단에서 EndBattle → Run 종료 판정 → 필요 시 새 Run → 전환 Job 연쇄를 실행한다.
- 잘못된 처리 확인은 기존 Flow의 로그·진행 정지 대신 예외 → 단일 `RuntimeFaultBoundary`(F1)로 모인다. 정상 명령 거절과 정상 `false` 반환은 오류로 바꾸지 않는다.
- 입력 전체 비우기는 View Attach·Detach에서 한다. 명령 번호 카운터·연출 확인 번호(P)는 Buffer가 유지한다.
- 목적지와 새 Run 로드아웃은 `CombatPlan` 한 곳이다. 일반 복귀·Run 종료 모두 현재 StageSelect, 새 Run 로드아웃은 임시 1이며 운영 값 교체는 후속 작업이다.

## 4. SSOT·SOLID·Framework 계약

판단 원본은 설계 J2와 `CombatPlan.Decide`, 런타임 사실 원본은 Buffer·View이며 Reader는 캐시를 두지 않는다. LifeCycle(트리거)·EntryService(진입 수명)·Plan(판단)·Job(호출·확인)·View(입력·표시)의 책임을 분리했다. Plan·Job은 Buffer 싱크를 직접 받지 않고 인터페이스를 통해 호출·조회한다.

FW-001에 따라 제공 인터페이스로 서비스를 등록하고, FW-002에 따라 수명 참여 객체를 모듈로도 등록했다. Builder는 한 번 Build하고 조립 뒤 등록 구조를 바꾸지 않는다(FW-003·지침 §11). Runner·Register·Manager는 공개 서비스 경로로 사용한다(FW-004). Framework 기반·Core 계산 코드 변경은 없다.

## 5. 검증과 전달

R3에서 MSBuild 오류 0, 관련 관리 테스트 251/251 통과를 확인했다. 이후 2026-10-10 사용자가 "다 확인했고 R4 진행해"로 Unity 확인 완료를 통보했다. 상세 Unity 결과 파일·실행 수는 별도로 받지 않았으며, R4는 문서 정리다. 구체적인 검토 메모와 검증 출처는 Task 실행 기록에 보관한다.

이 문서를 외부로 발송하지 않았다. 전달 후 담당자 답변이 오면 변경 영향만 원본 설계·계획에 반영한다.

## 6. 2026-10-22 전투 View 구조 개선 — 연결 줄 추가 공유 (미전달)

근거: [개선 설계 2판](../specs/2026-10-22-combat-view-structure-improvement-design.md)(§5·§6.1·§7.2), [Task 계획·실행 기록](../plans/2026-10-22-combat-view-structure-improvement-plan.md)(Phase 2·3·5). 코드는 작업 트리에 있고 커밋 전이다(커밋은 사용자). Phase 1~6 모두 Unity 컴파일·EditMode 시험·재생 문제 없음(2026-10-22 사용자 확인).

Framework 기반(Runner·JobRegister 공개 서명·LifeCycleManager·LifeCycle Base)의 기능·동작·계약은 바꾸지 않았다. 아래는 전투 구조가 그것을 쓰기 위한 연결 줄이다.

### 6.1 바꾼 연결 줄

경로는 `Assets/ProjectL/Script/Systems/` 기준이다.

| 파일 | 바뀐 것 | 소비자 영향 |
|---|---|---|
| `RuntimeSystem/GlobalSystem/JobSystem/JobRegister/JobRootType.cs` | `JobType.AdvanceCombat = 5` 삭제(운영 사용 0, 번호 5는 비우고 이유 주석). `JobType.EntryTransition = 8` 추가(전투 전용: 착지를 기다리던 현재 진입 요청의 전환을 다시 시도). 다른 값의 번호는 그대로 | Combat Plan이 받는 종류는 `None`·`Next`·`ExitCheck`·`EntryTransition`. 다른 루트 Plan은 영향 없음. 시험 두 줄(천명님 줄 포함)은 `(JobType)5`로 맞춤 |
| `RuntimeSystem/GlobalSystem/JobSystem/JobRegister/JobRegisterSystem.cs:70` | Combat 줄이 Runner도 넘김: `CombatPlan.Register(service, jobType, _lifeCycles, _runner)`(필수 인자) + 주석 한 줄 | `IJobRegisterSystem` 공개 서명·다른 루트 줄은 그대로라 시험 가짜 영향 없음 |
| `Factory/Runtime/LifeCycle/LifeCycleSystemFactory.cs` | 운영 조립 자리 `CreateComposition(faultBoundary)`는 오류 경계를 반드시 받음(null이면 `ArgumentNullException`). 기본 경계 대체 생성은 시험용 입구 `Create(faultBoundary = null)`만 | 운영 `FrameworkRuntimeFactory`는 이미 공용 경계를 넘김. 시험은 모두 `Create`를 씀 |
| `Factory/Runtime/InGame/InGameSystemFactory.cs`(이 줄들은 호진님 줄 `55aa9a3a`·`4d6d739d`) | `Compose`는 경계 필수. `CreateComposition(battleSource, faultBoundary, …)`은 두 인자를 선택 인자 대신 반드시 받음(바로 `Compose`로 가서 생략하면 늘 예외이므로). 시험용 입구 `Create`·`CreateWithSources`가 경계를 생략하면 `TestBoundary()` | `CreateComposition`을 부르는 곳은 운영 `FrameworkRuntimeFactory` 하나. `ChapterContinuationServiceTests`가 세는 `Create`는 여전히 하나 |

**그대로 둔 것(결정):** 뿌리 `FrameworkRuntimeFactory`(`Factory/Runtime/RunTimeSystemFactory.cs:28`)와 `InGameViewSystemFactory.cs:47`의 대체 생성. 인자 없이 부르는 시험(민형님 `TestRuner.cs:91`, 호진님 시험 등)이 있어서다. `MarkQueued`도 그대로다.

### 6.2 Runner를 이렇게 쓴다 — `CombatNextJob`

- `Register(Combat, Next)`: Plan이 사실을 읽어 할 일이 없으면 빈 목록, 있으면 `CombatNextJob` 하나를 돌려준다(등록 때는 할 일 여부만 봄).
- `CombatNextJob.Execute`: 실행 순간 다시 판단해 단계 Job 목록을 `runner.RegisterRange(...)`로 넘기고 바로 완료한다. struct다(Framework 관례).
- 기대는 Runner 동작(바꾸지 않음): `Execute` 안에서 등록한 Job은 같은 Tick의 남은 예산(32개) 안에서 돈다, 작은 Priority 먼저·같은 Priority는 먼저 넣은 것 먼저. 그래서 판단과 단계 Job이 한 Tick에 이어지고, 그 사이에 다른 모듈의 Tick이 끼지 않는다. 단계 Job은 한 번 실행으로 끝나거나 예외를 낸다(규칙은 `CombatStepKind` 설명).
- 착지 대기: 진입 전환 Job이 Runner를 여러 Tick 붙들지 않는다. 착지 전이면 요청에 착지 대기를 남기고 끝나며, 착지 게시가 `Register(Combat, EntryTransition)`으로 전환을 다시 건다.
- 정상 이탈: `CombatLifeCycle.OnExit`가 그 자리에서 진입 요청을 닫는다. `ExitCheck`는 전투가 남은 채 나갔을 때만 등록된다.

### 6.3 공용 오류 경계 (우리 줄, 참고)

`RuntimeSystem/Common/RuntimeFaultBoundary.cs`에 읽기 전용 `IsStoppingInput`(입력 정리 콜백이 도는 중인지)을 더했다. Factory 조립은 바뀌지 않는다. 착지 대기 중 표시 소유권 상실과 Combat 활성 중 씬 루트 비활성화를 F1로 보고할 때, 정상 종료 전 입력 정리 중의 것은 빼는 데 쓴다.

### 6.4 민형님 문서를 고친 것 (사용자 지시)

`Systems/Documentation/framework-programmer-guide.md` §10: 씬 번호를 코드(`SceneIndices`)대로 Boot 0(LifeCycle 없음)·Lobby 1·Stage 2·Combat 3·Artifact 4(예약)로 고치고, 씬별 Node priority 전체 표의 위치(`docs/superpowers/viewDocs/runtime-scene-binding-guide.md` §1.1)를 적었다.

### 6.5 이 문서 앞 절에서 바뀐 사실

- §2 `JobRootType` 행: `AdvanceCombat` 값이 없어졌고 Combat Plan이 `EntryTransition`을 더 받는다.
- §2 `JobRegisterSystem` 행: Combat 줄은 LifeCycle 그룹과 함께 Runner도 넘긴다.
- §3 "확인 뒤 다음 판단에서 EndBattle → Run 종료 판정 → 필요 시 새 Run → 전환": 이제 모든 결과가 [정리(EndBattle) → 보스 승리면 챕터 완료 → 표시] → 확인 → 전환이다. 전투 쪽 새 Run은 없다.
- §3 "목적지와 새 Run 로드아웃은 `CombatPlan` 한 곳 … 현재 StageSelect, 로드아웃 임시 1": 목적지는 `CombatPlan.DestinationOf`(Run이 이어지면 StageSelect, 끝났으면 Lobby), 새 Run 로드아웃 상수는 지웠다.

## 7. 2026-10-23 전투 골격 점검 — 연결 줄 추가 공유 (미전달)

근거: [골격 점검 설계](../specs/2026-10-23-combat-skeleton-check-design.md)(K-1·K-2·K-4·K-5), [Task 계획·실행 기록](../plans/2026-10-23-combat-skeleton-check-plan.md)(Phase K-a·K-b·K-e). 코드는 작업 트리에 있고 커밋 전이다(커밋은 사용자). Phase마다 Unity 컴파일·EditMode 시험 문제 없음(사용자 확인).

Framework 기반(Runner·JobRegister의 `Register` 공개 서명·LifeCycleManager·LifeCycle Base)의 기능·동작·계약은 바꾸지 않았다. 아래는 전투 구조가 그것을 쓰기 위한 연결 줄이다.

### 7.1 바꾼 연결 줄

경로는 `Assets/ProjectL/Script/Systems/` 기준이고, 줄 번호는 작업 트리 기준이다(HEAD에서 Combat 줄은 `:71`).

| 파일 | 바뀐 것 | 소비자 영향 |
|---|---|---|
| `RuntimeSystem/GlobalSystem/JobSystem/JobRegister/JobRegisterSystem.cs:70` | Combat 줄이 Runner를 넘기지 않는다: `CombatPlan.Register(service, jobType, _lifeCycles)`. 그 위 주석 한 줄을 지웠다(K-1) | `_runner` 필드와 다른 루트 줄은 그대로다 |
| 같은 파일 | 계약 `IJobRegisterSystem`에서 전투 전용 `RegisterCombatEntry(CombatEntryRequest)`와 그 구현을 지웠다. 쓰지 않게 된 `using …LifeCycles.Combat;`도 지웠다(K-2) | 계약은 `Register(JobRootType, JobType)` 하나가 됐다. 시험 가짜 11개(호진님)는 같은 이름 공개 메서드가 남아도 컴파일된다(명시적 구현 없음). 전달만 하던 감싼 등록기 둘(우리 드라이버, 호진님 `CombatEntryIntegrationTests`)은 그 줄을 지웠다. 진입 서비스와 직접 엮인 호진님 가짜 등록기(`MapCombatPresentationTests`)는 `Register(Combat, Entry)` 갈래로 다시 썼다(사용자 허락) |
| `RuntimeSystem/GlobalSystem/JobSystem/JobRegister/JobRootType.cs` | `JobType.Entry = 9` 추가(전투 전용: 진입 서비스가 막 받은 현재 요청으로 준비·전환을 등록). 다른 값 번호는 그대로(K-2) | Combat Plan이 받는 종류는 `None`·`Next`·`ExitCheck`·`EntryTransition`·`Entry`. 다른 루트 Plan은 영향 없음 |
| `Factory/Runtime/InGame/View/CombatSceneDisplayFactory.cs:32` | 우리 주석 한 줄의 순서 숫자 `-195`를 `SceneNodeOrder.CombatBackdrop` 이름으로(K-5 Ke.12, 주석만) | 없음 |

### 7.2 Runner를 이렇게 쓴다 — 바뀐 점

- `CombatNextJob.Execute`는 실행 순간 판단한 단계 Job 목록을 Runner에 다시 넣지 않고, **자기 안에서 차례로 실행**한다. 한 번 실행으로 끝나지 않은 단계 Job이 있으면 예외(F1)다.
- 그래서 "Execute 안에서 등록한 Job이 같은 Tick의 남은 예산 안에서 이어 돈다"는 Runner 관례에 더는 기대지 않는다. 단계 Job이 Runner 예산을 쓰지 않는다.
- 진입은 진입 서비스가 `Register(JobRootType.Combat, JobType.Entry)`로 건다. Plan이 LifeCycle 그룹의 진입 서비스 `Current`를 읽어 [준비 → 전환]을 만든다. 등록됨 표시(`MarkQueued`)·실패 표시(`Fail`)는 진입 서비스가 한다(조립 전제: 등록기의 LifeCycle 그룹이 그 진입 서비스를 가진다 — `EntryTransition`·`ExitCheck`도 같음).

### 7.3 이 문서 앞 절에서 바뀐 사실

- §2 `JobRegisterSystem` 행(`:22`): "Combat Plan의 `Register`·`RegisterEntry`에 전달", "공개 등록 메서드 서명 유지" — 계약에서 `RegisterCombatEntry`가 빠졌고 `CombatPlan.RegisterEntry`는 Plan 안의 private가 됐다. 진입은 `Register(Combat, Entry)` 하나로 들어온다.
- §2 끝 문단(`:28`)의 `RegisterEntry(service, request, lifeCycles)`: 바깥 입구가 아니다(위와 같음).
- §6.5 둘째 줄 "Combat 줄은 LifeCycle 그룹과 함께 Runner도 넘긴다": 이제 Runner를 넘기지 않는다(K-1).
- §6.1 `JobRegisterSystem.cs:70` 행: Combat 줄은 이제 Runner를 넘기지 않는다(`CombatPlan.Register(service, jobType, _lifeCycles)`).
- §6.1 "그대로 둔 것"의 "`MarkQueued`도 그대로다": `MarkQueued`·`Fail`은 등록기에서 진입 서비스로 옮겨졌다(10-22 결정 4-③을 사용자 결정으로 뒤집음).
- §6.2 "단계 Job 목록을 `runner.RegisterRange(...)`로 넘기고 바로 완료한다"와 "기대는 Runner 동작": 위 §7.2로 바뀌었다.
- §6.4의 "씬별 Node priority 전체 표의 위치": 숫자의 원본은 코드 `RuntimeSystem/Common/SceneNodeOrder.cs`(Bootstrap·Combat 씬 시스템 노드)가 됐고, 안내 §1.1은 그것을 가리킨다. Factory 공유 문서(`2026-10-15-combat-scene-display-factory-framework-share.md`)의 `CombatViewSystem.BackdropEnterPriority`도 `SceneNodeOrder.CombatBackdrop`으로 바뀌었다.
