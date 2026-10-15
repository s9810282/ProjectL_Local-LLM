# 전투 진행 Job·Plan 재구성 Task 계획 (2026-10-10)

- **설계:** [전투 진행 Job·Plan 재구성 설계](../specs/2026-10-10-combat-job-plan-restructure-design.md). 결정 U1~U8, J1~J15의 원본은 설계다. 이 문서는 Task와 실행 기록만 갖는다.
- **결정(2026-10-10):** D1~D6을 설계 §8의 추천안으로 확정했다. D1 guard 제거 + ExitCheck, D2 확인 실패는 예외 → F1, D3 `CombatEntryService` 분리, D4 입력 Buffer는 View가 Attach·Detach에서 비움, D5 읽기 객체는 `LifeCycleSystemFactory`가 만들어 LifeCycle 그룹으로 공개, D6 포기 요청·완료 확인은 View 상태.
- **상태:** R1 코드 완료·Unity 확인 대기(실행 기록 참고). R2 착수 전.
- **작업 방식:**
  - Phase마다 멈추고 보고한다. 커밋·push는 사용자가 한다.
  - 착수 방식(실습 워크시트로 받을지, Claude가 구현할지)은 Phase 시작 때 사용자에게 확인한다.
  - Core 파일은 바꾸지 않는다. 민형님 파일(`JobRegisterSystem.cs`·`JobRootType.cs`·`LifeCycleManager.cs`·`ServiceKeys`)은 필요한 줄만 바꾸고 R4에서 공유 문서를 쓴다.
  - 테스트는 기존 `Assets/Tests/EditMode/Framework/`에 더한다. 새 asmdef를 만들지 않는다.
  - 새 `.cs`는 `.meta`와 함께 다룬다. 지우는 파일은 `.meta`도 지운다.
  - 컴파일 확인은 VS 2022 MSBuild로 한다(새 파일은 `CustomAfterMicrosoftCommonTargets`로 더함, 산출물은 스크래치패드). GameObject·씬 테스트와 재생은 사용자가 Unity에서 확인한다.

## 0. 착수 전 조건

| # | 조건 | 이유 |
|---|---|---|
| 0.1 | View 계획 §7.68(SOLID·SSOT 개선 P2~P7)의 Unity 확인 완료 | 같은 파일(`CombatViewSystem`·`CombatAbandonController`)을 바꾸므로 회귀 원인이 섞이지 않게 한다 |
| 0.2 | 착수 시점의 EditMode 전체 결과 기록(통과 수·실패 목록) | 재구성 전후 비교 기준 |
| 0.3 | 착수 시점 커밋 해시 기록 | 실행 기록의 기준 |

## R1 — 계약 준비 (동작 변화 없음)

새 계약을 더하기만 한다. 옛 Flow·ExitSequence 경로는 그대로 동작한다.

| Task | 파일 | 할 일 | 확인 |
|---|---|---|---|
| R1.1 | `GlobalSystem/JobSystem/JobRegister/JobRootType.cs` | `JobType.Next = 6`, `JobType.ExitCheck = 7` 추가(설계 J15). 기존 값은 그대로 | 컴파일 |
| R1.2 | `LifeCycles/Combat/ICombatProgressView.cs`(신규), `CombatCompletionDisplay` enum | 설계 J12의 계약 정의. 기존 `ICombatExitPresentation`은 R3까지 함께 둔다 | 컴파일 |
| R1.3 | `ViewSystem/Combat/CombatViewSystem.cs`, `ICombatViewService.cs` | `ICombatViewService`가 `ICombatProgressView`도 상속. 구현: `IsPlaybackBusy`(= `_playback.IsBusy`), `IsSubmissionSuspended`(= `_submissionsSuspended`), `CompletionDisplay`(옛 `PresentCompletion` 경로에서 `Showing`·`Confirmed`로 바꿈, Attach에서 `None`), 콜백 없는 `PresentCompletion(completion)`(옛 메서드와 같은 검사, 확인되면 `Confirmed`), `IsAbandonRequested`(Attach에서 false. R1에서는 아직 아무도 세우지 않음) | `CombatViewSystemTests`에 상태 전이 테스트: Attach → None, 표시 → Showing, 확인 → Confirmed, 새 Attach → None. 기존 테스트 통과 |
| R1.4 | `LifeCycles/Combat/CombatProgressFacts.cs`·`CombatProgressReader.cs`(신규) | 설계 J13. `ICombatProgressReader`(public, 서비스는 인터페이스로 등록 FW-001) + `CombatProgressReader`(internal sealed). `PostBindService`에서 Combat 상태·결과·입력 읽기 싱크, 캐릭터·현재 진행 읽기 싱크, `InGame.ViewGroup.Combat`을 받는다. `Read()`·`ActiveCharacterCount`·`TryReadSelection`. 사본·캐시 없음 | 새 `CombatProgressReaderTests`: 각 싱크·View 값이 묶음에 그대로 들어감, 다음 명령 번호는 H 기준 `TryGetNextCommand`, 미리보기 없음 처리, View 없음은 예외 |
| R1.5 | `ServiceKeys/SystemGroupsService.cs`, `LifeCycleSystem/LifeCycleSystemGroup.cs`, `Factory/Runtime/LifeCycle/LifeCycleSystemFactory.cs` | `LifeCycleSystemServiceKey.CombatProgress` 추가, 그룹 속성 `CombatProgress`, Factory가 읽기 객체를 만들어 Export(서비스 + `PostBindService` 모듈) | `FrameworkRuntimeCompositionTests`에 "그룹에서 읽기 객체를 얻고 바인딩이 끝난다" 추가 |
| R1.6 | `GlobalSystem/JobSystem/JobRegister/JobRegisterSystem.cs`, `LifeCycles/Combat/Plans/CombatPlan.cs` | `PostBindService`에서 `SystemGroupsService.LifeCycle`을 받아 보관. Combat Plan 등록을 `(service, jobType) => CombatPlan.Register(service, jobType, _lifeCycles)`로, `RegisterCombatEntry`도 `_lifeCycles` 전달(설계 J14). `CombatPlan`의 기존 case 동작은 그대로 | `JobSystemTests`·`CombatPlanTests`의 `Register` 호출을 새 서명으로 맞춤. LifeCycle 그룹 없이 `JobRegisterSystem`을 조립하던 테스트가 있으면 가짜 그룹을 넣는다 |

**R1 완료 조건:** MSBuild 오류 0·새 경고 0, 관리 실행 가능한 테스트 통과, 사용자 EditMode 전체가 0.2 기준과 같음. 재생 동작 변화 없음.

## R2 — 새 Plan·Job (연결하지 않음)

새 판단·Job을 만들고 단위 테스트로 검증한다. LifeCycle은 아직 `Next`를 걸지 않으므로 동작 변화가 없다.

| Task | 파일 | 할 일 | 확인 |
|---|---|---|---|
| R2.1 | `Plans/CombatStep.cs`(신규), `Plans/CombatPlan.cs` | `CombatStep`(단계 종류 + 번호) 값과 `CombatPlan.Decide(in CombatProgressFacts)` 순수 함수. 설계 J2 판단표 0~14행 | 새 `CombatPlanDecideTests`: 각 행, 우선순위(Completion > 포기 > 처리, 명령 > 미리보기), 미리보기는 연출 중에도, 미부착이면 완료 표시 대기, 포기 Completion 구분. 골격 `CombatFlowTests`의 분기 테스트(명령 우선·연속 미리보기·Automatic 대기·Finished/None)를 같은 의미로 옮긴다 |
| R2.2 | `Jobs/CombatCommandJob.cs`·`CombatPreviewJob.cs`·`CombatAdvanceJob.cs` | 기대 번호와 읽기 객체를 받는 생성자 추가. 호출 뒤 같은 `Execute`에서 설계 J3 확인, 어긋나면 `InvalidOperationException`. 옛 생성자(확인 없음)는 옛 Flow 경로용으로 R3까지 둔다 | `CombatPlanTests` 확장: 명령 H 미갱신 → 예외·미완료, 거절도 H 이동이면 완료, 미리보기 응답 누락 → 예외, 요청 비움·교체 → 완료, 자동 진행 순번 유지·결과 없음·순번 불일치·종류 다름·거절 → 예외(옛 `CombatFlowTests`의 확인 테스트를 옮김) |
| R2.3 | `Plans/CombatExitContext.cs`(신규), `Jobs/` 이탈 Job(신규) | 설계 J7: 제출 정지, 완료 표시 시작(대상·최종 결과 확인 포함), 정리 기준 기록, EndBattle(정리·보존 확인), Run 종료 판정(K21 규칙), 새 Run(조건부, `NewRunJob`과 같은 거절 처리), 전환. 묶을 수 있는 것은 묶는다 | 새 `CombatExitJobTests`: 각 Job의 호출 1회·확인 위반 예외, Run 종료 판정 4갈래(`RunCompleted`·`NextChapterSelected`·`DefinitionUnavailable`·그 밖 거절), 새 Run은 Run 종료일 때만 |
| R2.4 | `Jobs/CombatAbandonJob.cs`(신규) | 설계 J8: 호출 전 결과·S 기억 → `AbandonBattle()` → `Abandoned` 보존 확인 / `AlreadyCompleted` 통과 / 그 밖 예외 | 위 테스트 파일에 포기 결과 4갈래 |
| R2.5 | `Jobs/CombatEntryTransitionJob.cs`·`CombatExitCheckJob.cs`(신규) | 설계 J5 진입 전환(`Prepared` ∧ Job 완료면 `BeginActivation` → `Change(Combat)`, 거절·취소면 호출 없음), J9 ExitCheck(`HasBattle`이면 예외, 아니면 진입 요청 닫기) | 진입 전환 3갈래, ExitCheck 2갈래 |
| R2.6 | `Plans/CombatPlan.cs` | `Register(…, Next)`: `Read()` → `Decide` → 단계별 Job 목록(할 일이 없으면 공유 빈 목록). `Register(…, ExitCheck)`. 6행 연쇄는 `CombatExitContext` 하나를 공유. 목적지·새 Run 로드아웃 상수를 Plan 한 곳으로(설계 J7) | `CombatPlanTests`: `Next`가 단계별로 맞는 Job 순서를 만든다, 빈 목록은 같은 인스턴스, 실제 Runner로 6행 연쇄가 한 Tick에 끝난다 |

**R2 완료 조건:** 새 테스트 통과, 기존 테스트 변화 없음, 재생 동작 변화 없음.

## R3 — 전환 (동작 변화 있음)

옛 경로를 새 경로로 바꾸고 옛 코드를 지운다. 한 Phase로 묶되 Task 순서대로 컴파일을 확인하며 진행한다.

| Task | 파일 | 할 일 |
|---|---|---|
| R3.1 | `LifeCycles/Combat/CombatEntryService.cs`(신규), `CombatEntryRequest.cs`, `LifeCycleSystemFactory.cs`, `LifeCycleSystemGroup.cs` | 설계 J5·J6. `CombatLifeCycle`의 진입 부분을 옮긴다. `ICombatEntryService`에 `Current`·`ActivateAccepted()` 추가. 의존은 Runner·Register·Loading·오류 경계·읽기 객체. `RegisterEntry` 연쇄 = [진입] → [진입 전환]. 진입 Job은 호출 전 `!HasBattle` 확인. `Shutdown`에서 요청 취소/실패(지금 `OnShutdown`과 같음, `AddModule`). 같은 키로 Export하므로 StageMap·GameBootstrap 호출은 그대로 |
| R3.2 | `LifeCycles/Combat/CombatLifeCycle.cs` | 설계 J1로 축소. `ISystemTick`·서비스 Export·guard·Flow·ExitSequence 조립 제거. `OnEnter` → `ActivateAccepted()`, `OnTick` → 오류·기록 모드가 아니고 Runner Idle이면 `Register(Combat, Next)`, `OnExit` → `Register(Combat, ExitCheck)` |
| R3.3 | `LifeCycleSystem/Base/LifeCycleManager.cs` | D1-a: `ILifeCycleTransitionGuard`와 `Change`·`Exit`의 검사 호출 제거(`5078ee0e` 이전 모양) |
| R3.4 | `ViewSystem/Combat/CombatAbandonController.cs`, `CombatInputPolicy.cs`, `CombatViewSystem.cs` | 설계 J8: 확인창의 확인은 `IsAbandonRequested = true`만 세운다. `CanRequestAbandon(facts, exitIdle)`의 인자는 `!IsAbandonRequested && !IsSubmissionSuspended`. `BindServices`의 `ICombatAbandonRequest` 인자 제거. D4-a: Attach·Detach에서 `CombatViewWriteSink.Clear()`(P·번호 카운터는 Buffer가 유지) |
| R3.5 | `ICombatViewService.cs`, `CombatViewSystem.cs` | `ICombatExitPresentation` 상속과 콜백형 `PresentCompletion` 제거 |
| R3.6 | 삭제: `CombatFlow.cs`·`CombatExitSequence.cs`(+meta). `ServiceKeys`·`LifeCycleSystemGroup`의 `CombatExit`. 확인 없는 옛 Job 생성자. `CombatPlan`의 옛 Combat case(`Start`·`ProcessCommand`·`ProcessPreview`·`AdvanceCombat`·`NewRun`)와 `CombatPrepareJob`의 대상 없는 호환 경로(호출자 `Flow.BeginPreparation`이 사라짐) | 공용 enum 값은 지우지 않는다(설계 J15) |
| R3.7 | 테스트 | 아래 표 |
| R3.8 | 확인 | MSBuild 오류 0·새 경고 0, 관리 실행, 사용자 EditMode 전체·재생 확인(§확인 목록) |

**R3.7 테스트 이전**

| 파일 | 처리 |
|---|---|
| `CombatFlowTests.cs` | 삭제. 분기·확인 의미는 R2.1·R2.2로 옮겼다. 실제 Runner 두 건(`RealRunner_…`)은 아래 구동 도우미로 다시 쓴다 |
| 구동 도우미(신규, 테스트 폴더) | 런타임 없이 쓰는 테스트용: "Runner가 비었으면 `Register(Combat, Next)` → Runner Tick"을 반복. `CombatFlowIntegrationTests`·`CombatMovementViewTests`·`CoreStageCombatCycleTests`의 `new CombatFlow(…)`·`flow.Tick()`을 대체 |
| `CombatFlowIntegrationTests.cs` | 도우미로 다시 쓴다. 준비 등록(옛 `Start`) 테스트는 진입 경로 테스트(`CombatEntryIntegrationTests`)로 옮기거나 삭제. "명령 제거 뒤 Flow 정지" 같은 소프트 정지 기대는 예외 기대로 바꾼다(D2) |
| `CombatMovementViewTests.cs`, `CoreStageCombatCycleTests.cs` | `CombatFlow` 생성 3곳·1곳을 도우미로 |
| `CombatEntryIntegrationTests.cs` | `ExitStatus`·`RequestAbandon`·`RequestNormalExit` 관찰을 결과 관찰(Completion·전환·새 Run·Core 호출 횟수)로. 포기는 View 가짜의 `IsAbandonRequested`로 시작. `ActiveBattle_RejectsNormalExitAndManagerBypass…`는 "전투 중 직접 `Change` → ExitCheck 예외"로 바꾼다(D1-a). `PresentationConsumer` 가짜는 `ICombatProgressView`로 |
| `CombatPanelControllerTests.cs`, `CombatViewSystemTests.cs` | `ICombatAbandonRequest` 가짜(`AbandonProbe`) 제거. 확인 → `IsAbandonRequested`, 이탈 진행 → `SuspendSubmissions`로 버튼 비활성 |
| `CombatInputPolicyTests.cs` | `exitIdle` 인자 의미 갱신(이름만 바뀌면 이름만) |
| `CombatPlanTests.cs` | 옛 case 테스트 정리, R2 테스트 유지 |
| `FrameworkRuntimeCompositionTests.cs` | `CombatLifeCycle` 생성 확인 유지, `CombatEntry`가 `CombatEntryService`인지, `CombatExit` 키가 없는지 |

**R3 확인 목록(사용자 Unity)**

- EditMode 전체: 0.2 기준 대비 새 실패 없음(옮기거나 지운 테스트는 목록으로 보고).
- Live 재생(Play From Boot, fixture·Database 각각 가능하면):
  1. StageSelect 노드 → Combat 진입(한 번 활성화, View Attach 정상)
  2. 배치 → 전투 시작 → 이동·공격 명령, 미리보기 호버, 턴 종료 → 적 자동 진행
  3. 승리 → 완료 확인 → StageSelect
  4. 패배 → 완료 확인 → 새 Run → StageSelect
  5. 포기: 배치 중 / 전투 중 / 자동 진행 중 → 확인창 → "포기 · 결과 확인" → 새 Run → StageSelect, 취소하면 계속
  6. 다시 진입해 한 판 더(재진입)
- RecordInputOnly 재생: 진입·표시·입력 기록만, 처리 없음, 포기 버튼 없음.

## R4 — 정리

| Task | 할 일 |
|---|---|
| R4.1 | [골격 설계](../specs/2026-09-30-combat-flow-skeleton-design.md) 머리말에 "§3·§4.1·§6·§6.3 6A.0·§6.4·§6.5 A1·A2는 재구성 설계로 대체" 표시, 판단표 원본 이전(K19) |
| R4.2 | UML·평가 문서 머리말 주의 문구: [상세 UML](../specs/2026-10-09-combat-skeleton-view-detailed-uml.md), [통합 UML](../specs/2026-10-07-combat-view-skeleton-current-uml.md), [골격·View 평가](../specs/2026-10-09-combat-skeleton-view-evaluation.md)(R2·M6 처리 상태) |
| R4.3 | 민형님 공유 문서(신규, `handoffs/`): `JobType` 값 2개, `JobRegisterSystem`의 LifeCycle 그룹 전달, `LifeCycleManager` guard 제거(우리가 넣었던 것을 되돌림), `ServiceKeys`·LifeCycle 그룹의 `CombatExit` 제거·`CombatProgress` 추가, 판단은 `CombatPlan`·LifeCycle은 트리거라는 구조 요약 |
| R4.4 | [Core 경계 인계](../handoffs/2026-09-24-view-core-data-boundary-handoff.md) §13.10 상태 갱신(호출 위치 이동 완료) |
| R4.5 | View 계획 §7.69 상태, `CLAUDE.md` 현재 상태, 이 문서 실행 기록 |

## 위험과 대응

| 위험 | 대응 |
|---|---|
| `JobRegisterSystem`이 LifeCycle 그룹을 요구하게 돼, 그룹 없이 조립하던 테스트가 깨진다 | R1.6에서 해당 테스트를 찾아 가짜 그룹을 넣는다. 운영 조립은 루트에 LifeCycle 그룹이 항상 있다 |
| 활성화 순서(설계 J6): 활성화가 늦으면 View Attach가 예외 | 활성화는 `OnEnter` 동기 호출로 둔다. R3 재생 1번과 `CombatEntryIntegrationTests`의 Attach 시점 `IsActive`로 확인 |
| 확인 실패가 소프트 정지에서 런타임 정지로 바뀐다(D2) | 의도된 변화. R3 보고에 바뀐 테스트 기대를 목록으로 남긴다 |
| 6행 연쇄가 한 Tick에 다 돌지 않으면 중간에 트리거가 다시 걸림 | Runner의 한 Tick 실행 한도는 32개라 5개 연쇄는 한 Tick에 끝난다. 그래도 `EndBattle` 뒤에는 0행(전투 없음)이 먼저 맞아 같은 연쇄를 다시 만들지 않는다. R2.6에서 실제 Runner로 확인 |
| 같은 Tick 앞쪽 콜백 오류 뒤 트리거 | `OnTick`이 `IsFaulted`를 먼저 본다(골격 §4.1 0행 유지) |
| View·골격 파일 동시 편집 | 착수 전 조건 0.1. R3 동안 다른 View 작업은 같은 파일을 피한다 |

## 실행 기록

### R1 계약 준비 (2026-10-10, 코드 완료·Unity 확인 대기, 동작 변화 없음)

- **착수:** 사용자 지시("너가 구현하고 R1 시작해"). 기준 커밋 `9e407bc8`. 착수 전 조건 0.1(§7.68 P2~P7 Unity 확인)은 확인하지 않고 진행했다. 0.2의 Unity EditMode 전체 기준은 없고, 아래 Mono 직접 실행 결과를 기준으로 삼았다.
- **변경 파일** (`RS` = `Assets/ProjectL/Script/Systems/RuntimeSystem/`, `LC` = `RS/LifeCycleSystem/LifeCycles/Combat/`, `T` = `Assets/Tests/EditMode/Framework/`)

| Task | 파일 | 변경 |
|---|---|---|
| R1.1 | `RS/GlobalSystem/JobSystem/JobRegister/JobRootType.cs`(**공용**) | `JobType.Next = 6`, `ExitCheck = 7` |
| R1.2 | `LC/ICombatProgressView.cs`(+meta, 신규) | `ICombatProgressView`, `CombatCompletionDisplay { None, Showing, Confirmed }` |
| R1.3 | `RS/InGameSystem/ViewSystem/Combat/ICombatViewService.cs` | `ICombatProgressView` 상속 추가(`ICombatExitPresentation`은 R3까지 유지) |
| R1.3 | `RS/InGameSystem/ViewSystem/Combat/CombatViewSystem.cs` | `IsPlaybackBusy`·`IsSubmissionSuspended`·`CompletionDisplay`·`IsAbandonRequested`. 콜백 없는 `PresentCompletion(completion)` 추가, 옛 콜백형과 같은 `ShowCompletion`을 쓴다(표시 → Showing, 확인 → Confirmed). Attach·Detach에서 상태 초기화. `IsAbandonRequested`는 아직 세우는 곳이 없다(R3) |
| R1.4 | `LC/CombatProgressFacts.cs`·`LC/CombatProgressReader.cs`(+meta, 신규) | 사실 묶음(public struct, internal 생성자), `ICombatProgressReader`(public) + `CombatProgressReader`(internal sealed, `PostBindService`에서 싱크 5개·`InGame.ViewGroup.Combat`, View 없음은 `Read` 때 예외) |
| R1.5 | `Systems/ServiceKeys/SystemGroupsService.cs`(**공용**), `RS/LifeCycleSystem/LifeCycleSystemGroup.cs`, `Systems/Factory/Runtime/LifeCycle/LifeCycleSystemFactory.cs` | 키 `LifeCycleSystemServiceKey.CombatProgress`, 그룹 속성 `CombatProgress`, Factory가 읽기 객체를 서비스 + 모듈로 등록 |
| R1.6 | `RS/GlobalSystem/JobSystem/JobRegister/JobRegisterSystem.cs`(**민형님 파일**), `LC/Plans/CombatPlan.cs` | JobRegister가 `PostBindService`에서 LifeCycle 그룹을 받아 Combat Plan에 넘김(`Register`·`RegisterEntry`). Plan의 새 인자는 아직 쓰지 않는다(R2·R3) |
| 테스트 | `T/TestLifeCycleGroup.cs`(+meta, 신규) | LifeCycle 그룹 없이 JobRegister를 조립하던 테스트용 빈 그룹. `ChapterGraphPreparationTests`·`CombatFlowIntegrationTests`·`CombatPlanTests`·`JobSystemTests`·`CharacterBattlePreparationTests` 조립에 추가 |
| 테스트 | `T/CombatProgressReaderTests.cs`(+meta, 신규) | 10건: Core 상태·결과 복사, 결과 없음, H 기준 다음 명령, 미리보기·P, View 상태, 매번 새로 읽음, 바인딩·View 필수, null 인자, 현재 선택, 캐릭터 수 |
| 테스트 | `T/CombatViewSystemTests.cs` | 완료 표시 상태 전이(None → Showing → Confirmed, 두 번 표시 거부, 재연결 시 초기화) 1건 |
| 테스트 | `T/CombatEntryIntegrationTests.cs` | 실제 런타임 바인딩으로 읽기 객체가 활성 전투·View·현재 선택을 읽는지 1건. 가짜 View에 `ICombatProgressView` 멤버 추가 |
| 테스트 | `T/FrameworkRuntimeCompositionTests.cs` | LifeCycle 그룹이 읽기 객체를 공개하고 바인딩 전에는 읽지 않는지 1건 |
| 테스트 | `T/CombatPlanTests.cs` | `CombatPlan.Register` 호출 14곳에 새 인자(null) |

- **계획과 다른 점(설계 J13 정정):** 읽기 객체를 인터페이스 없는 클래스로 등록했더니 `SystemBuilderHelper`가 "Service must be registered by interface type"으로 거부했다(FW-001). `ICombatProgressReader`를 두고 클래스는 internal로 바꿨다. 설계 J13·§7 표를 고쳤다.
- **검증:**
  - MSBuild `ProjectL.Tests.Framework`(새 파일은 `CustomAfterMicrosoftCommonTargets`) 오류 0. 경고는 손대지 않은 파일의 기존 CS0649뿐이고 변경 파일 새 경고 0.
  - Mono 직접 실행(기준 → 변경 후): `CombatProgressReaderTests` 10/10(신규), `CombatPlanTests` 38/38 → 38/38, `JobSystemTests` 8/8 → 8/8, `CharacterBattlePreparationTests` 12/12, `CombatEntryIntegrationTests` 25/25 → 26/26, `CombatInputPolicyTests` 48/48, `CombatPanelControllerTests` 8/8, `FrameworkRuntimeCompositionTests` 9/15 → 10/16(신규 통과).
  - Mono에서 못 도는 테스트(기준과 같음): `CombatFlowTests` 17건·`CombatFlowIntegrationTests` 5건(LogAssert), `FrameworkRuntimeCompositionTests` 6건(Camera 타입), `ChapterGraphPreparationTests`(ScriptableObject 생성), `CombatViewSystemTests`(GameObject).
  - **Unity EditMode 대기:** 전체 실행. 특히 `CombatViewSystemTests`(신규 1건 포함), `ChapterGraphPreparationTests`, `FrameworkRuntimeCompositionTests`, `CombatFlowTests`, `CombatFlowIntegrationTests`. 재생 동작은 바뀌지 않아야 한다(Combat 진입·승리·패배·포기가 전과 같음).
- 커밋·push 없음. Core 파일 변경 없음.
