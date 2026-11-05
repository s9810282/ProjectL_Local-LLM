# 전투 진행 Job·Plan 재구성 Task 계획 (2026-10-10)

- **설계:** [전투 진행 Job·Plan 재구성 설계](../specs/2026-10-10-combat-job-plan-restructure-design.md). 결정 U1~U8, J1~J15의 원본은 설계다. 이 문서는 Task와 실행 기록만 갖는다.
- **결정(2026-10-10):** D1~D6을 설계 §8의 추천안으로 확정했다. D1 guard 제거 + ExitCheck, D2 확인 실패는 예외 → F1, D3 `CombatEntryService` 분리, D4 입력 Buffer는 View가 Attach·Detach에서 비움, D5 읽기 객체는 `LifeCycleSystemFactory`가 만들어 LifeCycle 그룹으로 공개, D6 포기 요청·완료 확인은 View 상태.
- **상태:** R1~R3 사용자 커밋 완료(R3 `1e7fdda5`), R3 Unity 확인 완료 통보(2026-10-10), R4 문서 정리 완료. Framework 공유 문서는 작성 완료·미전달.
- **후속 변경(2026-10-16):** 일반 패배만 새 Run 준비 후 로비로 복귀하도록 목적지를 분리했다. 정책 원본은 설계 J7, 검증과 남은 Unity 확인은 [후속 기록](#2026-10-16-일반-패배의-로비-복귀-후속)을 따른다.
- **후속 변경(2026-10-20):** 자발적 포기도, 이어서 최종 승리도 새 Run 준비 후 로비로 복귀한다. Run이 끝나면 언제나 로비다. 기록은 [포기 후속 기록](#2026-10-20-자발적-포기의-로비-복귀-후속).
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
| R3.1 | `LifeCycles/Combat/CombatEntryService.cs`(신규), `CombatEntryRequest.cs`, `LifeCycleSystemFactory.cs`, `LifeCycleSystemGroup.cs` | 설계 J5·J6. `CombatLifeCycle`의 진입 부분을 옮긴다. `ICombatEntryService`에 `ActivateAccepted()` 추가(`Current`는 R2에서 먼저 추가했다). 의존은 Runner·Register·Loading·오류 경계·읽기 객체. `RegisterEntry` 연쇄 = [진입] → [진입 전환]. 진입 Job은 호출 전 `!HasBattle` 확인. `Shutdown`에서 요청 취소/실패(지금 `OnShutdown`과 같음, `AddModule`). 같은 키로 Export하므로 StageMap·GameBootstrap 호출은 그대로 |
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
  4. 일반 패배 → 완료 확인 → 새 Run → 로비(2026-10-16 목적지 변경, 포기는 제외)
  5. 포기: 배치 중 / 전투 중 / 자동 진행 중 → 확인창 → "포기 · 결과 확인" → 새 Run → 로비(2026-10-20 변경, 당시는 StageSelect), 취소하면 계속
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
| 6행 연쇄가 한 Tick에 다 돌지 않으면 중간에 트리거가 다시 걸림 | Runner의 한 Tick 실행 한도는 32개라 4개 연쇄(정리 기준 기록은 EndBattle에 합침)는 한 Tick에 끝난다. 그래도 `EndBattle` 뒤에는 0행(전투 없음)이 먼저 맞아 같은 연쇄를 다시 만들지 않는다. R2.6에서 실제 Runner로 확인 |
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
- 사용자가 R1을 커밋했다(R2 착수 시점 확인).

### R2 새 Plan·Job (2026-10-10, 코드 완료, 연결 안 함·동작 변화 없음)

- **착수:** 사용자 지시("R2 진행해"). LifeCycle은 아직 `Next`·`ExitCheck`를 걸지 않고, 진입 등록(`RegisterEntry`)도 바꾸지 않았다. 새 Job은 테스트에서만 실행된다.
- **변경 파일** (`LC` = `Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Combat/`, `T` = `Assets/Tests/EditMode/Framework/`)

| Task | 파일 | 변경 |
|---|---|---|
| R2.1 | `LC/Plans/CombatStep.cs`(+meta, 신규) | `CombatStepKind`(None·SuspendSubmissions·PresentCompletion·ExitBattle·Abandon·Command·Preview·Advance)과 `CombatStep`(종류 + 번호) |
| R2.1·R2.6 | `LC/Plans/CombatPlan.cs` | `Decide(in CombatProgressFacts)` 순수 함수(설계 J2 0~14행), `Register`에 `Next`(읽기 → 판단 → 단계별 Job, 할 일이 없으면 공유 빈 목록)·`ExitCheck`. 이탈 연쇄 = [EndBattle] → [Run 종료 판정] → [새 Run(조건부)] → [전환]이 `CombatExitContext` 하나를 공유. 목적지 `ContinueDestination`·`RunEndDestination`과 `NewRunLoadoutKey`를 Plan 한 곳에. 옛 case는 그대로 |
| R2.2 | `LC/Jobs/CombatCommandJob.cs`·`CombatPreviewJob.cs`·`CombatAdvanceJob.cs` | 읽기 객체·기대 번호를 받는 생성자 추가. 호출 뒤 같은 `Execute`에서 확인(명령 H ≥ N, 미리보기 응답 ≥ 번호 또는 요청이 바뀜, 자동 진행 §4.3.1 순서). 어긋나면 예외·미완료. 옛 생성자는 확인 없이 그대로(R3에서 제거) |
| R2.3 | `LC/Plans/CombatExitContext.cs`(+meta, 신규) | 이탈 연쇄의 공유 값: 완료, 두 목적지, 기억한 결과·S, Run 종료 판정(한 번만, 판정 전 읽으면 예외) |
| R2.3·R2.5 | `LC/Jobs/CombatExitJobs.cs`(+meta, 신규) | `CombatSuspendJob`, `CombatCompletionJob`(대상·최종 결과 확인 뒤 표시, 포기는 결과 대조 생략), `CombatEndBattleJob`(직전 결과·S 기억 → EndBattle → 정리·보존 확인), `CombatRunEndJob`(K21), `CombatNewRunJob`(Run 종료일 때만), `CombatTransitionJob`, `CombatExitCheckJob`(전투가 남았으면 예외, 아니면 진입 요청 닫기) |
| R2.4 | `LC/Jobs/CombatAbandonJob.cs`(+meta, 신규) | `Abandoned`면 포기 완료·결과·S 보존 확인, `AlreadyCompleted`면 통과, 그 밖 예외 |
| R2.5 | `LC/Jobs/CombatEntryTransitionJob.cs`(+meta, 신규) | 진입 Job 완료 뒤 `Prepared`면 `BeginActivation` → `Change(Combat)`(실패 시 요청 Fault 후 다시 던짐), 거절·취소면 전환 없음 |
| R2.6 | `LC/CombatEntryRequest.cs`, `LC/CombatLifeCycle.cs` | `ICombatEntryService.Current`(읽기 전용) 추가와 구현(`_entry`). R3.1에서 하려던 것을 Plan이 쓰므로 먼저 했다 |
| 테스트 | `T/CombatPlanDecideTests.cs`(+meta, 신규) | 판단표 17건: 전투 없음, 승패 줄(제출 정지·연출·미부착·표시 단계), 승패 > 포기, 포기 줄, 포기 > 처리, 명령 > 미리보기, 연출 중 미리보기, 자동 진행 S, 대기, Finished/None |
| 테스트 | `T/CombatProgressJobTests.cs`(+meta, 신규) | 58건: 처리 Job 확인(옛 `CombatFlowTests`의 확인 테스트를 옮김), 포기 4갈래, 완료 표시·EndBattle·Run 종료 판정·새 Run·전환·ExitCheck·진입 전환, `Register(Next)` 단계별 Job·빈 목록 재사용·이탈 연쇄를 실제 Runner로 한 Tick에 완료·`ExitCheck` 구성·LifeCycle 그룹/View 필수 |

- **계획과 다른 점:**
  - "정리 기준 기록" Job을 따로 두지 않고 `CombatEndBattleJob`에 합쳤다. 같은 연쇄·같은 Tick이라 기준 값이 같다. 설계 J2 6행·J7 표·§5를 고쳤다.
  - 이탈 Job들은 한 파일(`CombatExitJobs.cs`)에 묶었다(`StageJob.cs`에 `NewRunJob`이 함께 있는 선례).
  - `ICombatEntryService.Current`를 R2에서 먼저 추가했다(동작 변화 없음).
- **검증:**
  - MSBuild 오류 0, 변경 파일 새 경고 0(남은 경고는 기존 CS0649·Core 파일 CS8632). after.targets는 Unity가 csproj를 다시 만들어 R1 파일이 이미 들어 있어 `Exclude="@(Compile)"`로 중복을 뺐다.
  - Mono 직접 실행: `CombatPlanDecideTests` 17/17(신규), `CombatProgressJobTests` 58/58(신규), 회귀 `CombatProgressReaderTests` 10/10, `CombatPlanTests` 38/38, `JobSystemTests` 8/8, `CharacterBattlePreparationTests` 12/12, `CombatEntryIntegrationTests` 26/26, `CombatInputPolicyTests` 48/48, `CombatPanelControllerTests` 8/8, `CombatInputRouterTests` 20/20, `CombatViewRefreshTests` 12/12. `CombatFlowTests`·`CombatFlowIntegrationTests`는 기준과 같다(LogAssert 실패만).
  - **Unity EditMode 대기:** 새 두 파일과 전체. 재생 동작 변화 없음(연결 전).
- 커밋·push 없음. Core 파일 변경 없음. Unity가 바꾼 `Assets/Plugins/FMOD/Cache/Editor/FMODStudioCache.asset`은 이번 작업과 무관하다.
- 사용자가 R2를 커밋했다(R3 착수 시점 확인).

### R3 전환 (2026-10-10, 사용자 커밋·Unity 확인 완료 통보, 동작 변화 있음)

- **착수:** 사용자 지시("R3 시작해"). 중간에 사용량 한도로 멈췄다가 이어서 진행했다.
- **변경 파일** (`RS` = `Assets/ProjectL/Script/Systems/RuntimeSystem/`, `LC` = `RS/LifeCycleSystem/LifeCycles/Combat/`, `V` = `RS/InGameSystem/ViewSystem/Combat/`, `T` = `Assets/Tests/EditMode/Framework/`)

| Task | 파일 | 변경 |
|---|---|---|
| R3.1 | `LC/CombatEntryService.cs`(+meta, 신규) | 진입 요청 수명: `RequestEntry`·`CancelEntry`·`IsActive`·`Current`, 끝난 요청(닫힘, 진입 Job이 배출한 거절·취소)은 다음 요청이 대신함. 등록 전 조건은 Runner Idle·로딩 아님·이전 전투 없음(읽기 객체). `ActivateAccepted()`(internal): 수락·전환된 요청이고 현재 선택이 대상과 같을 때만 활성화. `Shutdown`에서 요청 취소/실패 |
| R3.1 | `LC/Plans/CombatPlan.cs` | `RegisterEntry` = [진입] → [진입 전환]. `Register`는 `None`·`Next`·`ExitCheck`만 받고 옛 Combat case(`Start`·`NewRun`·`Process*`·`AdvanceCombat`)는 거부. `RequireManager` 도우미, `Stage.Jobs` using 제거 |
| R3.1 | `Systems/Factory/Runtime/LifeCycle/LifeCycleSystemFactory.cs` | 읽기 객체 → 진입 서비스(같은 오류 경계·읽기 객체) → `CombatLifeCycle(모드, 오류 경계, 진입 서비스)` 순서로 만들고, 진입 서비스는 `ICombatEntryService`로 Export(+모듈) |
| R3.2 | `LC/CombatLifeCycle.cs` | 트리거만: `OnEnter` → `ActivateAccepted()`, `OnTick` → 오류·기록 모드가 아니고 Runner Idle이면 `Register(Combat, Next)`, `OnExit` → 오류가 아니면 `Register(Combat, ExitCheck)`. 서비스 Export·모듈 Tick·guard·Flow/ExitSequence 조립 제거 |
| R3.3 | `RS/LifeCycleSystem/Base/LifeCycleManager.cs`(**민형님 파일**) | `e0b02cc7`(guard를 넣기 전) 내용으로 되돌림. `ILifeCycleTransitionGuard`와 `ExitCore` 분리가 사라짐 |
| R3.4 | `V/CombatAbandonController.cs` | "포기 요청됨"(`IsRequested`)을 소유. 확인 → 요청 상태 + 버튼 끄기, Attach·Detach에서 초기화. 가능 조건 인자는 `!IsRequested && !제출 정지`. 골격 계약(`ICombatAbandonRequest`) 의존 제거 |
| R3.4·R3.5 | `V/CombatViewSystem.cs`, `V/ICombatViewService.cs`, `V/CombatInputPolicy.cs` | `ICombatViewService : ICombatProgressView`만 상속. `BindServices`의 포기 인자 제거, `IsAbandonRequested`는 컨트롤러 값, 콜백형 `PresentCompletion` 제거(확인은 `CompletionDisplay`). D4-a: Attach·Detach에서 `CombatViewWriteSink.Clear()`. `HasPendingPresentation`은 internal로. 주석 갱신 |
| R3.6 | 삭제: `LC/CombatFlow.cs`·`LC/CombatExitSequence.cs`(+meta) | `ICombatExitService`·`ICombatAbandonRequest`·`ICombatExitPresentation`·`CombatExitStatus` 함께 삭제 |
| R3.6 | `Systems/ServiceKeys/SystemGroupsService.cs`(**공용**), `RS/LifeCycleSystem/LifeCycleSystemGroup.cs` | `CombatExit` 키·그룹 속성 제거 |
| R3.6 | `LC/Jobs/CombatPrepareJob.cs`·`CombatCommandJob.cs`·`CombatPreviewJob.cs`·`CombatAdvanceJob.cs` | 확인 없는 옛 생성자와 대상 없는 준비 경로 제거 |
| 주석 | `RS/InGameSystem/ViewSystem/Boot/GameBootstrap.cs`, `LC/CombatEntryRequest.cs` | 옛 주체(Flow·LifeCycle) 이름을 새 주체로 |

**R3.7 테스트**

| 파일 | 처리 |
|---|---|
| `T/CombatProgressTestDriver.cs`(+meta, 신규) | 런타임 없이 진행 구동: `Trigger`(Runner가 비면 `Next`)·`RunJobs`, 실제 읽기 객체, 등록 기록. `TestProgressView`(가짜 `ICombatViewService`), `TestLifeCycleManager`, `TestViewGroup` |
| `T/TestLifeCycleGroup.cs` | 관리자·진입·읽기 객체를 넣을 수 있게, `CombatExit` 제거 |
| `T/CombatProgressIntegrationTests.cs`(+meta, 신규) | 옛 `CombatFlowIntegrationTests`를 구동 도구로 다시 씀(15건). 옛 `CombatFlowTests`의 실제 Runner 2건을 옮김. 소프트 정지 기대는 Job 예외 기대로(D2). 준비는 fixture가 Core `PrepareBattle()` 직접 호출(진입 경로는 진입 통합 테스트가 봄) |
| 삭제: `T/CombatFlowTests.cs`·`T/CombatFlowIntegrationTests.cs`(+meta) | 분기·확인 의미는 R2 테스트와 위 파일로 옮김 |
| `T/CombatEntryIntegrationTests.cs` | 이탈·포기를 결과로 관찰(`ExitStatus`·`RequestAbandon`·`RequestNormalExit` 제거). 가짜 View가 `ICombatProgressView` 상태를 갖고 씬 연결·해제 때 입력을 비움. `RunUntil`·`FrameUntilFault` 도우미. guard 테스트 → "전투 중 직접 전환은 ExitCheck가 잡음", 진입 전환 예외 뒤 Runner에 Job이 남는 것으로 기대 변경, 분리된 View의 확인은 받지 않고 기다림(오류 아님), 포기 완료도 같은 이탈 연쇄 |
| `T/CombatPlanTests.cs` | 진입 연쇄·진입 Job 예외·실제 Runner로 진입 연쇄 1 Tick·옛 타입 거부·생성자 검사로 재구성. 옛 처리/NewRun/대상 없는 준비 테스트와 미사용 가짜 제거 |
| `T/CombatProgressJobTests.cs` | 옛 생성자 테스트 제거, 가짜 LifeCycle 그룹을 `TestLifeCycleGroup`으로 |
| `T/CombatPanelControllerTests.cs`, `T/CombatViewSystemTests.cs` | 포기 대역(`AbandonRequest`·`AbandonProbe`) 제거, 요청 상태·제출 정지로 검증. 완료 확인 테스트는 `CompletionDisplay`로 |
| `T/CombatMovementViewTests.cs`, `T/CoreStageCombatCycleTests.cs` | `CombatFlow` → 구동 도구(실제 `CombatViewSystem`을 읽기 객체의 View로) |
| `T/CharacterBattlePreparationTests.cs` | fixture `Start()`가 Core `PrepareBattle()` 직접 호출, 옛 준비 Job 블록 제거 |
| `T/CombatDisplayReuseTests.cs`, `T/FrameworkRuntimeCompositionTests.cs` | `BindServices`·`CombatLifeCycle` 새 서명 |

- **계획과 다른 점:**
  - `ActivateAccepted()`를 `ICombatEntryService`(공개 계약)에 두지 않고 `CombatEntryService`의 internal 메서드로 했다. 부르는 곳이 `CombatLifeCycle` 하나라 View·StageMap에 활성화를 노출하지 않는다(ISP).
  - 진입 전 `!HasBattle` 확인을 진입 Job 대신 `RequestEntry`(읽기 객체)에 두었다. 지금처럼 요청 때 바로 거부한다(K11).
  - `OnExit`는 이미 오류로 멈춘 런타임에서는 ExitCheck를 걸지 않는다.
  - 기록된 완료는 포기든 승패든 같은 이탈 연쇄를 탄다. 옛 "Core 직접 포기는 자동 이탈 안 함" 테스트를 이 기대로 바꿨다(설계 J2 그대로).
- **검증:**
  - MSBuild 오류 0, 변경 파일 새 경고 0(after.targets에 디스크에 없는 Compile 항목 제거를 더함).
  - Mono 직접 실행: `CombatEntryIntegrationTests` 27/27, `CombatProgressIntegrationTests` 15/15(신규), `CombatProgressJobTests` 57/57, `CombatPlanDecideTests` 17/17, `CombatPlanTests` 16/16, `CombatProgressReaderTests` 10/10, `JobSystemTests` 8/8, `CharacterBattlePreparationTests` 12/12, `CombatPanelControllerTests` 9/9, `CombatInputPolicyTests` 48/48, `CombatInputRouterTests` 20/20, `CombatViewRefreshTests` 12/12. 테스트 어셈블리 전체를 돌려 남은 실패는 ScriptableObject·GameObject·Camera·Audio를 쓰는 클래스뿐임을 확인했다.
  - **최초 검증 당시 Unity EditMode 대기(Mono로 못 돎):** `CombatViewSystemTests`(포기·완료 표시·Attach/Detach 입력 비우기), `CombatMovementViewTests`, `CoreStageCombatCycleTests`, `CombatDisplayReuseTests`, `FrameworkRuntimeCompositionTests`, `ChapterGraphPreparationTests` 등 전체. 이후 사용자 확인은 아래 후속 기록을 따른다.
  - **재생 확인 대상(사용자):** §R3의 "R3 확인 목록" 전체. 이후 사용자 확인은 아래 후속 기록을 따른다.
- 커밋·push 없음. Core 파일 변경 없음.

**R3 후속 검토·사용자 확인(2026-10-10):**

- 사용자가 R3를 `1e7fdda5`로 커밋했다. R4 착수 시 작업 트리는 깨끗했다.
- Codex 검토에서 현재 csproj로 MSBuild 오류 0, 관련 관리 테스트 251/251 통과를 다시 확인했다. 새 테스트 파일의 CS0649 경고 4건(`CombatProgressTestDriver`의 `Busy`·`Abandon`·`Failure`, `CombatProgressIntegrationTests.DefinitionSource.Failure`)이 있어 위 최초 기록의 "변경 파일 새 경고 0"과 구분한다. 검토 시 Unity 배치 실행은 프로젝트가 이미 열려 있다는 오류로 실행되지 않았다.
- 완료 표시 검토에서 같은 표시 객체·Completion으로 재연결한 뒤 이전 확인 콜백을 호출하면 새 표시가 `Showing → Confirmed`로 바뀌는 경우를 재현했다. 일반 씬 연결은 새 표시 객체를 만들지만 재연결 수명에 대한 검토 메모로 남긴다. R4에서는 코드를 수정하거나 이 지적의 해소를 기록하지 않는다.
- 사용자가 "다 확인했고 R4 진행해"라고 통보했다. R3 확인 목록의 Unity EditMode·재생 확인 완료를 사용자 확인으로 인수한다. 상세 실행 수·결과 파일은 별도로 받지 않았으며 Codex가 Unity 검증을 실행한 증거와 구분한다.

### R4 문서 정리 (2026-10-10, 완료)

- **착수:** 사용자 지시("다 확인했고 R4 진행해"). 코드 기준 `1e7fdda5`. R4.1~R4.5 범위의 문서만 변경했다.

| Task | 결과 |
|---|---|
| R4.1 | 골격 설계 머리말에 대체 범위를 표시하고 §4.1의 단일 판단표 원본을 재구성 설계 J2로 이전. 이전 계약·예상 UML은 이력 보존 |
| R4.2 | 10-09 상세 UML·10-07 통합 UML·10-09 평가 머리말에 재구성 이전 스냅샷임을 표시. 평가 R2의 `CombatRunEndJob` 분리와 M6의 Plan 상수 통합·guard 목록 제거를 반영. Title·운영 로드아웃 값 교체는 후속 작업으로 유지 |
| R4.3 | [민형님 공유 문서](../handoffs/2026-10-10-combat-job-plan-framework-share-minhyeong.md) 작성. 공용 enum·JobRegister 전달·guard 복원·키·그룹 변경과 호출자 영향·SSOT·SOLID·FW 점검을 정리. 미전달 |
| R4.4 | [Core 경계 인계 §13.10](../handoffs/2026-09-24-view-core-data-boundary-handoff.md#1310-전투-진행-jobplan-재구성-알림-2026-10-10-호출-위치-이전-완료미전달)에 호출 위치 이전 완료·Core 요청 없음·검증 출처 기록. 타임라인 요청은 별도 유지 |
| R4.5 | View 계획 §7.69·CLAUDE.md 현재 상태·이 문서 상태와 실행 기록 갱신 |

- **검증:** 변경 문서 9개의 로컬 링크 206개를 검사하고 새 링크·앵커 18개의 대상 존재를 확인했다(새 링크 오류 0). Markdown 코드 블록 짝·U+FFFD 없음·`git diff --check`를 확인했다. 문서만 바꾸므로 컴파일·테스트를 재실행하지 않았다. SSOT는 새 설계 J2를 원본으로 연결하고, SOLID·FW 계약 및 실제 공용 변경은 공유 문서에 기록했다.
- **리뷰 지점:** R4 완료로 멈춘다. 이 계획의 다음 Phase는 없다. 민형님 공유 문서 외부 전달, VR6(Core 타임라인 계약 인수 후), Title·운영 로드아웃 선택은 별도 후속 작업이다.
- 커밋·push·외부 발송 없음. 코드·씬·에셋·Core 파일 변경 없음.

### 2026-10-16 일반 패배의 로비 복귀 (후속)

- **사용자 확정:** 일반 패배만 로비로 보낸다. 목적지가 적힌 기존 문서를 먼저 조사·보고한 뒤, 문서와 코드 수정을 지시받았다. 기존 전투 이탈에서의 새 Run 준비는 유지한다.
- **구현:** `CombatPlan.ExitBattle`이 공개 Completion의 `Outcome == Defeat` 및 `Reason != VoluntaryAbandonment`를 확인해 일반 패배의 Run 종료 목적지만 Lobby로 선택한다. 최종 승리·포기의 기본 `RunEndDestination`과 Run 계속 목적지는 Stage다. 완료 확인 → EndBattle → Run 종료 판정 → 새 Run 준비 → 전환의 기존 Job 연쇄·공유 컨텍스트는 유지한다. Core·Framework 기반·UI·Camera·서비스 등록·새 공개 계약 변경은 없다.
- **테스트 변경:** 실제 적 턴의 공격으로 일반 패배를 만드는 `CombatEntryIntegrationTests.Defeat_EndsRun_StartsNewRunThenReturnsToLobby`에서 새 Run 준비와 Lobby LifeCycle 진입을 확인한다. 기존 Run 종료 검증 helper는 요청한 씬 번호도 확인하도록 보강해 최종 승리·정의 없는 챕터 승리·자발적 포기의 StageSelect 목적지를 함께 검증한다.
- **RED:** 변경 전 Systems DLL과 변경한 테스트로 일반 패배 1건을 실행해 요청 씬이 기대 Lobby(1)가 아닌 Stage(2)인 단언 실패를 확인했다. Unity 실행 환경 오류를 RED로 세지 않는다.
- **자동 검증:** Unity가 점유한 작업을 종료하거나 프로젝트 잠금을 지우지 않았다. 현재 C# 프로젝트의 소스·참조·정의와 기존 Unity 의존 DLL을 이용해 별도 폴더에 Systems·Framework 테스트 어셈블리를 컴파일했다(각 오류 0). .NET 10에서 NUnit `[Test]`·`[TestCase]` 메서드를 호출하고 SetUp/TearDown을 적용한 **161/161 통과**다. 대상은 `CombatEntryIntegrationTests`·`CombatProgressJobTests`·`CombatPlanTests`·`CombatPlanDecideTests`이며, 실제 Unity Test Runner 전체 실행과 구분한다. 빌드의 기존 파일 경고는 로그에 남겼으며 신규 경고 수 비교는 수행하지 않았다.
- **남은 확인:** Unity Test Runner EditMode 실행과 실제 Boot → 전투 패배 → 결과 확인 → 로비 화면 표시. 로비에서 다시 출발할 때 LV8A가 준비된 Run에 `StartNewRun(1)`을 다시 호출하는 기존 동작과 생성 위치 통합은 LV10의 별도 후속이다. 포기·최종 승리의 로비 복귀나 LV10 전체 완료로 기록하지 않는다.
- **문서 정합:** 설계 J7에 현재 정책을 두고 골격 설계 §6.4·골격 계획 §6.16·10-07 목적지 메모에는 이력과 후속 링크를 표시했다. 로비 LV10의 과거 전체 복귀안을 이번 사용자 범위로 정정했다. AGENTS·CLAUDE 현재 상태에 설계 원본을 연결했다. 추가 링크·앵커 11개 오류 0, 변경 파일 `git diff --check` 통과.
- **SSOT·SOLID·FW·독립성:** 목적지 선택은 기존 Plan 한 곳, 승패·Run 종료의 사실과 새 Run 준비는 기존 Core 공개 계약, 정리·전환 실행은 기존 Job이 맡는다. 새 규칙 계산·Buffer 원본·캐시·서비스를 추가하지 않는다. 제품 변경은 Plan 한 파일과 관련 테스트 한 파일이다. 다른 작업의 스테이지 맵 아킴보 에셋·표시 테스트는 수정하지 않았다.
- **증거:** `.superpowers/sdd/2026-10-16-defeat-lobby-return/`의 `red-managed.json`, `green-managed.json`, 컴파일 로그·검증용 프로젝트·runner. 서브에이전트를 사용하지 않고 변경 diff와 공개 결과·호출 순서를 자체 검토했다. 커밋·push·외부 발송 없음.

### 2026-10-20 자발적 포기의 로비 복귀 (후속)

- **사용자 결정:** 게임을 패배하거나 유저가 포기한 경우 새 Run으로 시작해 처음 로비로 이동한다. 2026-10-16의 "포기는 StageSelect 유지"를 바꾼다.
- **구현:** `CombatPlan`에 패배 목적지 `DefeatDestination = LifeCycleKeys.Lobby`를 두고, `ExitBattle`이 `Outcome == Defeat`만 보고 고른다(포기 사유 예외를 없앰). `RunEndDestination = Stage`는 최종 승리 목적지로 남는다. 포기는 이미 Core에서 Run을 패배로 끝내고 기존 `CombatNewRunJob`이 `StartNewRun(1)`을 부르므로 새 Run 준비는 바뀌지 않는다. 제품 변경은 `CombatPlan.cs` 한 파일이며 Core·Job·Framework·서비스 등록 변경은 없다.
- **테스트 변경:** `CombatEntryIntegrationTests`의 포기 3건이 Lobby(1)를 기대한다 — `AbandonmentCompletion_IsPresentedWithoutFinalResult_AndEndsRunLikeOtherCompletions`, 이름을 바꾼 `Abandon_InDeployment_RecordsAbandonmentThenStartsNewRunAndReturnsToLobby`(Lobby 씬·LifeCycle 진입까지 확인), `Abandon_WhileCommandIsQueued_ProcessesItFirstThenAbandonsOnce`.
- **남은 확인:** Unity Test Runner EditMode(`CombatEntryIntegrationTests`·`CombatProgressJobTests`·`CombatPlanTests`·`CombatPlanDecideTests`)와 실제 Boot → 전투 포기 → "포기 · 결과 확인" → 로비 표시. 로비에서 다시 출발할 때 LV8A가 `StartNewRun(1)`을 다시 부르는 기존 동작(일반 패배와 같음)은 LV10의 별도 후속이다.
- **담당 공유:** `CombatPlan`은 전투 골격(용근님) 코드이므로 이번 변경을 공유해야 한다(미전달).

### 2026-10-20 최종 승리의 로비 복귀 (후속)

- **사용자 결정:** 최종 승리도 로비로 보낸다(Run 결과 요약 표시를 로비 한 곳에 두기 위해, Run 요약 설계 (Git history: `2026-10-20-run-summary-statistics-design.md`) Q-S3 A).
- **구현:** `CombatPlan.RunEndDestination = LifeCycleKeys.Lobby`. Run이 끝나면 결과와 상관없이 같은 목적지라 패배 전용 `DefeatDestination`과 결과 분기를 지웠다. `CombatExitContext`·Job 연쇄는 그대로다.
- **테스트 변경:** `CombatEntryIntegrationTests`의 Run 종료 도우미가 항상 Lobby(1)를 확인한다(목적지 인자 삭제). 최종 승리 두 시험 이름을 `...ReturnsToLobby`로 바꿨다.
- **남은 확인:** Unity Test Runner(병합 뒤 Framework 시험 상태 때문에 미실행)와 실제 최종 승리 → 로비 재생. 용근님 공유 미전달.
