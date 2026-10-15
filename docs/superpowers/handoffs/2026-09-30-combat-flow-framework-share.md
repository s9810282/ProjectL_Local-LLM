# 전투 진행 골격 — Framework 변경 예정 및 수명 계약 요청

> **최신 공용 변경 인계:** [2026-10-05 공동 Framework 공유](2026-10-05-combat-view-skeleton-framework-share.md). 아래의 예정/미착수/Unity 대기는 당시 기록이며 현재 상태는 공동 인계와 계획을 우선한다. 문서 작성과 외부 전달·합의는 구분한다.

- 작성일: 2026-09-30
- 작성: View 작업(스테이지 내부 Combat 담당)
- 대상: Framework 담당자(민형님), F2의 Core·View 협의
- 기준: `feature/Yonggeun/Stage`, HEAD `3a2b6777`. Phase 0에서 작업 트리까지 확인했다.
- 상태: **전달용 문서 준비 완료. 담당자 전달·답변·합의는 미확인. 코드 변경은 아직 없다.**
- 원본: [설계 4차](../specs/2026-09-30-combat-flow-skeleton-design.md), [구현 계획](../plans/2026-09-30-combat-flow-skeleton-plan.md)
- Core 요청: [Core 전달 문서 §13](2026-09-24-view-core-data-boundary-handoff.md#13-전투-진행-골격-core-서비스결과수명-요청-2026-09-30)

## 1. 공유 목적과 담당 경계

View 작업에서 `CombatLifeCycle`·`CombatFlow`·`CombatPlan`·전투 Job 네 개(준비 포함)를 만든다. Core 담당자는 처리 서비스와 상태·결과 기록을 제공하며, 서비스가 준비되면 Job에서 그 서비스를 호출하는 연결도 View 작업이 한다. 현재 설계의 역할 분담을 전달하는 것이며 상대 담당자의 수락을 받은 상태는 아니다.

공용 변경은 아래 두 파일에 먼저 한정한다. 기존 Global Runner와 Register 계약을 재사용한다. 새로운 Runner나 공용 완료/취소 API를 추가하지 않는다. F1·F2에서 다른 공용 변경이 필요하다고 합의하면 실제 파일·API를 먼저 확정한다.

## 2. Phase 1 공용 변경 예정

| 파일 | 변경 예정 | 보존할 계약 |
|---|---|---|
| `Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/JobSystem/JobRegister/JobRootType.cs` | `ProcessCommand = 3`, `ProcessPreview = 4`, `AdvanceCombat = 5` 추가 | `JobType.None = 0`, `Start = 1`, `NewRun = 2`와 JobRootType 숫자 유지 |
| `Assets/Tests/EditMode/Framework/JobSystemTests.cs` | 일반 등록·서비스 주입·완료 검증의 표본을 Combat에서 Lobby로 교체. NonStagePlans 목록에서 Combat 제외 | Runner 우선순위·미완료 대기·Tick 예산·Clear·잘못된 목록 검증 유지 |

현재 JobSystemTests에서 Combat을 참조하는 테스트는 다음 다섯 개다. 이 중 실제 완료를 전제로 하는 것은 첫째·셋째·다섯째다.

- `Group_ExportsOnlyContext_AndForwardsBindingInitializeTickAndShutdown`
- `Register_SelectsStaticPlan_AndInjectsBoundServiceIntoJob`
- `RepeatedRegistrations_CreateFreshJobs`
- `Register_RequiresBindingAndInitialization_AndRejectsUnknownTypes`
- `NonStagePlans_CanBeRegisteredAndExecutedWithoutChapter`

전투 구성·미연결 예외는 새 `CombatPlanTests`로 검증한다. 단순히 Combat 테스트를 제거해 실패를 숨기지 않는다. 새 테스트도 기존 `Assets/Tests/EditMode/Framework/ProjectL.Tests.Framework.asmdef` 안에 둔다.

Combat 쪽 변경은 `CombatJob.cs`와 `.meta`를 `CombatPrepareJob`으로 옮기고 명령·미리보기·진행 Job을 추가하는 것이다. 기존 GUID `87148ad8eb6ab384ca930f4bda0fc576`은 유지한다. 모든 전투 Job의 Priority는 0이며 준비 선행과 등록 순서는 Flow가 결정한다. Priority로 Core/View 통신 순서를 만들지 않는다.

`JobRegisterSystem.Initialize()`는 이미 CombatPlan을 등록한다. 기존 `Register(JobRootType, JobType)` 서명, Plan의 `List<IJobService>` 반환, 서비스 주입 방식은 변경할 필요가 없다.

## 3. F1 요청 — 실행 예외 뒤 Runtime 재Tick 방지

현재 `JobRunnerSystem.Tick()`은 `Execute()` 예외가 발생하면 현재 Job을 유지한다. `FrameworkRuntime.Tick()`도 예외를 전파한다. 다음 프레임에 같은 Runtime을 Tick하면 같은 Job이 재실행된다. Flow의 Stop은 새 등록만 막으므로 이 문제를 해결하지 못한다.

설계의 권고안은 **실제 Runtime 구동자가 예외를 한 번 보고하고 해당 Runtime의 재Tick·새 입력·정상 전환을 중단하는 것**이다. 종료·복구 정책과 구현 소유자는 Framework 쪽 확인이 필요하다. 예외 Job을 정상 완료로 표시하거나 예외를 삼키는 방식은 사용하지 않는다.

받아야 할 내용:

- 실제 구동자 파일·타입·Tick 진입점과 담당자
- 실패 보고 위치, 입력 차단·정상 전환 중단, 종료·복구 순서
- 실제 구동 경계에서 예외 후 다음 프레임에도 실패 서비스 호출이 1회로 유지되는 검증 결과

현재 Runner를 읽어 확인한 사실과 F1을 구현·검증한 결과는 구분한다. 이 문서 작성은 F1 완료가 아니다.

## 4. F2 요청 — 전환 전에 기존 전투 작업 종료

`OnExit`는 로딩 종료 콜백 안에서 실행된다. 여기서 Stop을 호출해도 이미 등록된 Job은 남는다. 입력을 먼저 지우면 그 Job이 실행할 때 읽어야 할 명령도 사라진다.

권고하는 정상 이탈 순서:

1. 전환 요청 소유자가 Flow의 새 등록과 View의 새 입력 기록을 중단한다. 아직 Change/Exit를 호출하지 않는다.
2. 기존 Job이 읽을 입력을 보존한 채 Runtime Tick을 계속한다. 프레임마다 Runner Idle을 확인한다.
3. 마지막 결과 표시·확인과 Core 정리를 마친다. 정리 Job이 있으면 그것까지 끝낸다.
4. 최종 Idle과 정리 완료를 확인한 뒤 LifeCycleManager.Change/Exit를 호출한다.
5. OnExit에서 Stop과 입력 Clear를 수행한다.

대기 중 실행 예외가 나면 F1로 넘어간다. 치명적 실패·Runtime 전체 종료에 정상 이탈 절차를 강제 적용하지 않는다. Global Runner 전체를 임의로 Clear하지 않는다.

받아야 할 내용:

- 전환 요청 소유자의 실제 파일·타입, Flow 정지 요청의 공개 API와 등록 위치
- 모든 전환 경로가 이 경계를 거치는 방법
- Core·View와 합의한 마지막 결과 확인, `EndBattle` 호출 주체, 정상 종료와 중도 이탈 차이
- 등록 직후 이탈, 대기 중 예외, 재진입, Runtime 종료의 통합 검증 결과

## 5. 먼저 구현할 범위와 바인딩 주의점

Core 수정 없이 Phase 1의 Plan·Job 골격, Phase 2의 Flow 1단계, Phase 3의 LifeCycle 훅을 작성할 수 있다. F3 전에는 Flow가 AdvanceCombat을 등록하지 않으며, 미연결 Job은 명시적인 예외를 던진다. 실제 전투 진입 경로를 활성화하지 않는다.

**전투 진입 경로가 없어도 서비스 바인딩은 실행된다.** CombatLifeCycle은 Factory에 이미 등록돼 있고 `LifeCycleSystemGroup.PostBindService()`는 모든 등록 모듈을 순회한다. Phase 3은 기존 Buffer·Job 서비스만 조회하며 기존 `FrameworkRuntimeCompositionTests`의 실제 바인딩 사례를 회귀 확인한다.

F3는 타입, 읽기 서비스 구현·등록, 실제 결과 기록을 구분해서 인수한다. 타입만 있으면 Flow 단위 검증을 준비할 수 있지만, 필수 생성자 변경과 LifeCycle의 결과 서비스 조회를 생산 코드에 연결하려면 실제 조회 가능한 서비스가 필요하다. 임시 빈 서비스를 등록해서 통과시키지 않는다.

## 6. 협의·전달 기록

| 항목 | 문서 준비 | 담당자 전달 | 답변·합의 | 실제 파일·API | 구현·검증 증거 |
|---|---|---|---|---|---|
| enum 값과 공용 테스트 변경 | 완료 | 미확인 | 미수신 | 예정 파일은 §2 | 코드 미착수 |
| F1 구동 경계 | 완료 | 미확인 | 미수신 | 미정 | 미확인 |
| F2 전환 경계 | 완료 | 미확인 | 미수신 | 미정 | 미확인 |

답변을 받으면 담당자·날짜·실제 경로·멤버·근거를 [계획 §3](../plans/2026-09-30-combat-flow-skeleton-plan.md#3-선행-계약과-인수-조건)에 함께 기록한다. Phase 0에서는 메시지를 발송하거나 외부 합의를 완료로 표시하지 않았다.

## 7. Phase 0 검증 상태

- 사용자 확인(2026-09-30): **현재 Unity 컴파일 오류 없음.** 에이전트가 직접 실행한 결과가 아니다.
- 사용자에게 기준 실행 요청: `JobSystemTests`, `CombatStateBufferTests`, `CombatViewBufferTests`, `FrameworkRuntimeCompositionTests`.
- 위 EditMode 테스트 결과: **미수신·미확인.** 이번 Phase에서 직접 실행하지 않았다.
- 소스·문서 대조 완료. 실제 변경 전 기준과 기존 작업 트리는 [계획 §6.1](../plans/2026-09-30-combat-flow-skeleton-plan.md#61-phase-0-실행-기록-2026-09-30)에 기록한다.

## 2026-10-05 추가 공유 — 정상 F2 전환 전 검사

사용자 지시로 정상 승패의 F2를 선행 구현했다. 계약 원본은 [골격 설계 §6.4](../specs/2026-09-30-combat-flow-skeleton-design.md#64-정상-승패-이탈의-구체-계약-2026-10-05), 실행 상태는 [골격 계획 §6.13](../plans/2026-09-30-combat-flow-skeleton-plan.md#613-phase-6b-정상-f2-선행-구현재화-요청-정정-2026-10-05)이다. 이 문서 앞부분의 Phase 0 미구현 표시는 당시 이력이다.

- `Systems/RuntimeSystem/LifeCycleSystem/Base/LifeCycleManager.cs`에 선택적 내부 `ILifeCycleTransitionGuard` 검사를 추가했다. Change/Exit가 다음 구간/로딩 상태를 변경하기 전에 현재 모듈을 검사한다. Combat만 구현하며 다른 LifeCycle은 기존 방식으로 동작한다.
- Combat의 정상 이탈은 `ILifeCycleSystemGroupService.CombatExit.RequestNormalExit(destination)`을 통한다. Completion 표시와 Core EndBattle 뒤 정리 확인을 마친 시퀀스의 목적지 Change만 허용한다. 직접 Change/Exit로 전투를 빠져나가던 호출자는 이 경계를 사용해야 한다.
- Runtime Shutdown은 F1 정리이므로 정상 이탈 대기를 우회한다. 공용 JobRunner 취소/Clear 계약과 Framework 기반 계층은 바꾸지 않았다.
- 포기 연결과 화면 목적지/실제 요청 호출자는 아직 남았다. Unity 조립·LifeCycle 회귀 검증 대기이며 외부 전달·담당자 합의 완료를 뜻하지 않는다.
