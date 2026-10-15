# 전투 진행 골격 — 로컬 세션 중간 인계 문서

> **다음 작업 시작점:** [2026-10-05 골격·View 공동 인계](2026-10-05-combat-view-skeleton-session-handoff.md). 아래는 이전 시점의 결정/작업 이력이다. 현재 코드·완료 범위·대기는 새 인계의 원본 링크를 따른다.

> 전투 진행 골격(`CombatLifeCycle`·`CombatFlow`·`CombatPlan`·전투 Job)의 **외부 조건 없이 가능한 범위(Phase 0~3)**를 마친 시점의 중간 인계다.
> **`1a51234c` 병합 통합으로 준비 Job만 Core 서비스에 연결했다.** 운영 준비 입력(G7)과 나머지 Job 세 개는 미연결이고, Flow는 자동 진행을 등록하지 않으며, 이탈은 미완성이다. 게임에서 전투가 도는 상태가 아니다. 병합 검증·커밋 상태는 [계획 §6.5](../plans/2026-09-30-combat-flow-skeleton-plan.md#65-1a51234c-병합-통합-기록-2026-09-30)를 본다.
> 새 세션은 **이 문서 → 저장소 루트 `CLAUDE.md` → [구현 계획](../plans/2026-09-30-combat-flow-skeleton-plan.md) → [설계 4차](../specs/2026-09-30-combat-flow-skeleton-design.md)** 순서로 읽는다.

- 작성일: 2026-09-30
- 작성: View 작업(스테이지 내부 담당, 로컬 세션)
- 대상 브랜치: `feature/Yonggeun/Stage`
- 상태: **구현 계획 Phase 0~3 완료, Phase 4는 F3 타입 대기.** 각 Phase는 사용자가 에디터 컴파일(오류 없음)과 EditMode 테스트(모두 통과)를 확인했다.
- 직전 인계: [전투 View 설계 세션 인계](2026-09-30-combat-view-design-session-handoff.md)(이 작업의 출발점, §10.3-1)

## 1. 한눈에 보기

| 항목 | 상태 |
|---|---|
| 끝난 일 | JobType 3개 추가, Plan·Job 네 개 틀, `CombatFlow` 1단계, `CombatLifeCycle` 훅 1단계, 전달 문서 두 개 |
| 동작 범위 | EditMode 테스트로만 확인. 부트스트랩·`LifeCycleManager.Change` 호출자·씬 인덱스 3이 없어 **실행 중 이 구간에 들어오지 않는다** |
| 막힌 곳 | Phase 4: F3 결과 읽기 타입(천명님). Phase 5: Core 처리 서비스 R6·R1~R4 기록. Phase 6: F1(예외 뒤 Tick 중단)·F2(전환 전 Job 종료) |
| 전달 | 요청 문서는 준비 완료. **담당자 전달·답변은 미확인**(사용자가 보낸다). Core 요청의 현재 기준은 [전투 Core 요청 총정리](2026-09-30-combat-core-requests-consolidated.md) |
| Core 새 커밋 | 사용자 지시로 `1a51234c`(Task 3·4) 병합 충돌을 해결하고 준비 서비스 호출을 `CombatPrepareJob`에 연결했다. K2·Plan 네 분기를 유지했다. 병합 커밋 `02b9529a`. **[Core 답변](2026-09-30-combat-core-response-to-view.md)으로 K2와 병합 방식이 확정됐다.** 자발적 포기 = 패배 규칙도 확정됐고, F3는 Core가 가장 먼저 넘길 예정이다 |
| 커밋 전 확인 | Phase 3·K11·누락 `.meta` 두 개는 `4d0c63ea`에 포함됐다. 이번 병합 커밋은 사용자 작업이며 검증 현황은 계획 §6.5 참고 |

## 2. 작업 방식

규칙 전체는 `CLAUDE.md`에 있다. 이번 작업에서 확인한 것만 적는다.

- Phase마다 멈추고 보고한다. 보고 끝에 **다음 Phase의 목표·파일·핵심 동작·주의점·확인 방법**을 적는다.
- 새 Phase는 워크시트로 할지 직접 할지 묻는다. 이번 작업은 Phase 1~3을 모두 **"너가 진행해줘"**로 맡겼다.
- Claude는 Unity를 실행할 수 없다. 컴파일·테스트는 사용자에게 **실행할 테스트 이름을 적어 요청**하고, 받은 결과를 계획 §6에 "사용자 확인/보고"로 기록한다.
- 설계·계획과 다른 판단이 필요하면 구현은 설계대로 두고 먼저 묻는다(예: K11 네 번째 조건은 물어서 추가했다).
- 한 사실은 한 곳에만(SSOT): 설계·계획의 표현이 어긋나면 같이 고친다.

## 3. 커밋 이력

| 커밋 | 내용 |
|---|---|
| `3a46b623` | 설계·구현 계획, Core 전달 문서 §13, Framework 공유 문서(Phase 0) |
| `d1dcb92f` | JobType·Plan·Job 골격, `JobSystemTests` 표본 교체, `CombatPlanTests`(Phase 1) |
| `23934177` | `CombatFlow`, `CombatFlowTests`, 계획 기록(Phase 2) |
| `4d0c63ea` | K11 네 번째 조건, `CombatLifeCycle` Phase 3, 설계·계획·인계, `CombatFlow.cs.meta`·`CombatFlowTests.cs.meta` |
| `916c948e` | 현재 구조 UML·Core 요청 총정리 등 문서 갱신 |
| **병합 커밋 전** | `1a51234c` 통합: 준비 Job 호출 연결, 충돌 해결, 테스트·문서 갱신(계획 §6.5) |

`FMODStudioCache.asset` 변경은 에디터가 만든 것이라 작업과 무관하다.

## 4. 만든 것

경로는 `Assets/ProjectL/Script/Systems/RuntimeSystem/` 기준이다.

| 파일 | 역할 |
|---|---|
| `GlobalSystem/JobSystem/JobRegister/JobRootType.cs` (공용) | `ProcessCommand = 3`, `ProcessPreview = 4`, `AdvanceCombat = 5` |
| `LifeCycleSystem/LifeCycles/Combat/Plans/CombatPlan.cs` | Start·ProcessCommand·ProcessPreview·AdvanceCombat → Job 하나, Priority 0 |
| `…/Combat/Jobs/CombatPrepareJob.cs`(옛 `CombatJob`, GUID 유지) · `CombatCommandJob.cs` · `CombatPreviewJob.cs` · `CombatAdvanceJob.cs` | 준비는 `CoreGroup.Combat.PrepareBattle()` 호출 후 완료. 나머지 세 개는 "…서비스가 연결되지 않았습니다(R6)." 예외 |
| `…/Combat/CombatFlow.cs` | 진행의 주인 하나. 1단계(F3 없음) |
| `…/Combat/CombatLifeCycle.cs` | 훅 네 개. 1단계(이탈 미완성) |
| `Assets/Tests/EditMode/Framework/CombatPlanTests.cs` · `CombatFlowTests.cs` | Plan·Job 구성·준비 호출·예외, Flow 등록·확인 검증. 병합 후 재검증 상태는 계획 §6.5 |
| `Assets/Tests/EditMode/Framework/JobSystemTests.cs` (공용) | 일반 Job 시스템 표본을 Combat → Lobby로 교체 |

### 4.1 CombatFlow가 하는 일

```text
Tick: 정지 → Runner 바쁨 → 직전 처리 확인 → 전투 없음
      → PlayerInput + 연출 끝 + 명령 있음 → ProcessCommand
      → PlayerInput + 새 미리보기          → ProcessPreview
      → 그 밖(연출 중, Automatic, Finished, None) → 등록 없음
```

- 자기 상태는 "기다리는 처리(없음/준비/명령/미리보기) + 기대 번호 하나 + 정지"뿐이다. 국면·번호는 매 Tick Buffer에서 읽는다.
- **되묻지 않기:** 처리가 정상 반환했는데 기대 번호가 오르지 않으면 `[CombatFlow] …` 오류를 한 번 남기고 멈춘다. 미리보기는 View가 요청을 비우거나 바꿨으면 통과다.
- `BeginPreparation()` 조건 넷(K11): 멈추지 않음, Runner Idle, 전투 없음, 기다리는 처리 없음. 어기면 `InvalidOperationException`.
- `Stop()`은 새 등록만 막고 로그를 남기지 않는다. 멈춤은 `Reset()`으로만 풀린다.

### 4.2 CombatLifeCycle 훅

| 훅 | 동작 |
|---|---|
| `OnBindServices` | `BufferSystemKey.Buffer`와 Global `JobServiceCommand`로 Flow 생성. 입력 쓰기는 `CombatViewWriteSink`만 보관 |
| `OnEnter` | 입력 `Clear` → `Reset` → `BeginPreparation` |
| `OnTick` | `Flow.Tick()` |
| `OnExit` | `Stop` → 입력 `Clear`. Runner는 비우지 않음(D7). **이탈 미완성** 주석 |

### 4.3 결정 요약 (설계 §9)

| 번호 | 결정 |
|---|---|
| K1 | 연출 대기는 Job이 아니라 Flow 조건. `IsCompleted`는 "서비스 호출이 돌아왔다"는 뜻으로만 쓴다(Framework에 `Complete()` 함수는 없다) |
| K2 | Flow·Plan·Job 전부(`Start` 포함)와 `JobType`은 View 작업 소유. Core는 처리 서비스만 제공 |
| K3 | 새 JobType 이름 `ProcessPreview`(4), `AdvanceCombat`(5) |
| K4 | 되묻지 않기를 골격에 넣는다 |
| K5 | 연출이 끝났으면 명령이 미리보기보다 먼저 |
| K6·K7 | 예외 재실행 방지(F1)와 전환 전 Job 종료(F2)는 권고안만 있고 합의 대기 |
| K8 | 결과 순번만으로 자동 진행 성공을 판단하지 않는다(F3 필요) |
| K9 | F3 전 1단계 Flow는 `Automatic`에서 등록하지 않고 기다린다 |
| K10 | LifeCycle 훅은 도달 불가 상태에서 먼저 연결한다 |
| K11 | `BeginPreparation` 조건 넷, 위반 시 예외 |

## 5. 알아 둘 것

- **준비 API 연결만으로 실제 진입을 켤 수는 없다.** 준비 Job은 Core를 호출하지만 운영 입력 소스(G7)가 없어 운영 조립에서는 예외가 난다. 명령·미리보기·진행 Job은 미연결 예외다. F1과 준비 입력 등 진입 조건을 먼저 연결해야 한다.
- **Runner는 예외가 난 Job을 붙잡는다.** 다음 Tick에 같은 Job을 다시 실행한다. Flow의 `Stop`으로는 막을 수 없다(F1). `CombatPlanTests.RealRunner_…`가 이 현재 동작을 기록한다.
- **`OnBindServices`는 전투에 들어가지 않아도 실행된다.** `FrameworkRuntimeCompositionTests`의 `ChapterCoordinator_…` 두 테스트가 실제 runtime으로 이 바인딩을 지난다. Flow 생성자에 인자를 더하면(Phase 4의 결과 읽기) 바인딩할 실제 서비스가 없을 때 이 테스트가 깨진다.
- **Tick 순서는 Runner → View → Flow다.** Flow가 N 프레임에 등록한 Job은 N+1 프레임에 실행된다.
- **View가 지켜야 할 것**(전투 View 설계로 넘길 것, 계획 7.4)
  - 상태 변경 결과를 확인하면 기존 미리보기를 무효화한다. 여전히 필요하면 `SetPreview`로 새 번호를 받고, 아니면 `ClearPreview`한다. 변화 없는 매 프레임 재요청은 하지 않는다.
  - 거절·연출 없는 결과도 `AcknowledgePresented`로 확인한다. 안 하면 Flow가 연출 대기에서 멈춘다.
  - 배치 → 전투 입력 Layer 전환은 `EnterCombat` 수락 결과를 본 뒤 View가 한다. Flow는 Layer를 건드리지 않는다.

## 6. 외부 대기

> 요청 현황과 남은 요청의 기준은 [전투 Core 요청 총정리](2026-09-30-combat-core-requests-consolidated.md)다. 준비 서비스와 Job 호출은 연결됐으며 운영 입력(G7)은 대기 중이다. 사용자 지시로 K2를 유지해 병합했으며 담당자 간 소유 경계 합의는 별도로 확인한다.

| 조건 | 받아야 할 것 | 누구 | 필요한 Phase | 요청 위치 |
|---|---|---|---|---|
| F3 타입 | 결과 순번·처리 종류·수락/거절·이유를 읽는 인터페이스와 값 타입 | 천명님 | **4** | Core 전달 §13.3 |
| F3 서비스 등록 | 실제 읽기 서비스 인스턴스와 조회 경로 | 천명님 | 4.4 | Core 전달 §13.3 |
| R6·R1~R4 | 준비·명령·미리보기·진행 서비스의 실제 API와 CoreGroup 노출, 번호·국면 갱신 | 천명님 | 5 | Core 전달 §13.2 |
| F3 기록·보존 | 결과 기록과 View 확인 뒤에도 최신 결과 조회 | 천명님 | 5 | Core 전달 §13.3 |
| F1 | 예외 뒤 같은 Runtime을 다시 Tick하지 않는 구동 경계 | 민형님 | 6 | Framework 공유 §3 |
| F2 | 전환 요청 소유자, Flow 정지 요청 API, 결과 수명·`EndBattle` 호출자 | 민형님·천명님·View | 6 | Framework 공유 §4, Core 전달 §13.4 |

답을 받으면 [계획 §3.1](../plans/2026-09-30-combat-flow-skeleton-plan.md) 표에 담당자·날짜·실제 파일·멤버를 적는다. 추측으로 Core 인터페이스를 만들지 않는다(가칭 `ICombatResultReadSink`를 우리 쪽에 선언하지 않는다).

## 7. 다음 시작점 (우선순위 순)

1. **병합 검증·커밋:** 계획 §6.5의 Unity 검증 결과를 확인하고 `1a51234c` 병합을 커밋한다(사용자). Phase 3와 `.meta` 두 개는 이미 커밋됐다.
2. **전달:** [전투 Core 요청 총정리](2026-09-30-combat-core-requests-consolidated.md)(소유 경계 K2 확인, merge 방식, 남은 요청)와 [Framework 공유 문서](2026-09-30-combat-flow-framework-share.md)를 담당자에게 보낸다(사용자). 보냈다는 사실과 답변을 계획 §3.1에 기록한다.
3. **F3 타입이 오면 Phase 4:** Flow에 결과 읽기 추가, Tick 8행을 `AdvanceCombat` 등록으로, 수락일 때만 계속. 순서와 테스트는 계획 Phase 4에 있다.
4. **기다리는 동안 할 수 있는 것:** 전투 View 설계([전투 View 설계 인계](2026-09-30-combat-view-design-session-handoff.md) §10.3-3). 먼저 그 문서 §6의 결정 6개(부트스트랩, 전투 씬, 범위 등) 중 1~3번을 사용자에게 묻는다. 이 골격의 Flow·Job 틀과 §5의 View 조건 위에 올린다.
5. 이후 Phase 5(Core 서비스 연결) → Phase 6(F1·F2 통합) → Phase 7(최종 인계).

## 8. 문서 지도

| 문서 | 용도 |
|---|---|
| [구현 계획](../plans/2026-09-30-combat-flow-skeleton-plan.md) | Phase 0~7 Task, §3 외부 조건, §6 Phase별 실행 기록 |
| [설계 4차](../specs/2026-09-30-combat-flow-skeleton-design.md) | Flow 분기(§4), Plan·Job(§5), LifeCycle(§6), 결정(§9), 합의 뒤 예상 UML(§12) |
| [현재 구조 UML](../specs/2026-09-30-combat-flow-current-uml.md) | Phase 0~3 시점 코드의 실제 구조: 조립·수명, Flow·Plan·Job, Buffer, 바인딩·진입 시퀀스, Tick 판단, Flow 상태 |
| [전투 Core 요청 총정리](2026-09-30-combat-core-requests-consolidated.md) | **Core 요청의 현재 기준.** K2 소유 경계, `1a51234c` merge 충돌 해결안, R1~R11·F2·F3 현황과 남은 요청, 답변 표 |
| [Core 답변](2026-09-30-combat-core-response-to-view.md) | 천명님 답변 원문(2026-09-30): K2 확정, 병합 방식, 게임 규칙, F3 4단계 제공 계획, 후속 API 계약 |
| [Core 전달 문서 §13](2026-09-24-view-core-data-boundary-handoff.md) | Phase 0 때의 소유 경계·처리 서비스·F3·F2 요청(총정리가 대신함) |
| [Framework 공유 문서](2026-09-30-combat-flow-framework-share.md) | 공용 변경, F1·F2 요청과 답변 표 |
| [전투 View 설계 세션 인계](2026-09-30-combat-view-design-session-handoff.md) | 작업 방식, SSOT·SOLID·FW 기준(§4), 결정 대기 6개(§6), Buffer 점검(§10.1) |
| [CombatViewBuffer 설계](../specs/2026-09-29-combat-view-buffer-design.md) | 입력 Buffer 규칙 V1~V9 |

## 9. 새 세션 시작 예시

```text
docs/superpowers/handoffs/2026-09-30-combat-flow-local-session-handoff.md 부터 읽어.
천명님이 F3 타입을 줬어(파일: …). Phase 4 진행하자.
```

```text
docs/superpowers/handoffs/2026-09-30-combat-flow-local-session-handoff.md 부터 읽어.
F3 기다리는 동안 전투 View 설계 시작하자. 인계 §7-4대로 결정부터 물어봐.
```
