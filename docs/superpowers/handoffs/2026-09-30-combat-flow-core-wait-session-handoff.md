# 전투 진행 골격 — Core 대기 시점 세션 인계

> **과거 기록 안내(2026-10-02):** 아래는 Phase 0~3 당시의 인계다. 현재는 Phase 4 완료, `83f485d1`의 Core Task 7 인수와 Phase 5 Job 연결·테스트 작성을 마쳤으며 실행 검증이 남아 있다. 최신 상태는 [골격 계획 §0.1·§6.8](../plans/2026-09-30-combat-flow-skeleton-plan.md), [설계 7차](../specs/2026-09-30-combat-flow-skeleton-design.md), [인수 안내](../plans/2026-09-30-combat-flow-core-intake-guide.md)를 먼저 읽는다. 이 문서의 과거 테스트 결과를 현재 검증으로 재사용하지 않는다.

> 전투 진행 골격의 **외부 조건 없이 가능한 범위(Phase 0~3)와 Core 병합·답변 정리까지** 마친 시점의 인계다. 이전 [중간 인계](2026-09-30-combat-flow-local-session-handoff.md) 뒤에 일어난 일(Core 병합, Core 답변, 문서 정리)을 포함한다.
> 새 세션은 **이 문서 → 저장소 루트 `CLAUDE.md` → [Core 인계물 인수 가이드](../plans/2026-09-30-combat-flow-core-intake-guide.md) → [구현 계획](../plans/2026-09-30-combat-flow-skeleton-plan.md)** 순서로 읽는다. 설계는 [설계 5차](../specs/2026-09-30-combat-flow-skeleton-design.md), 현재 코드 구조는 [현재 구조 UML](../specs/2026-09-30-combat-flow-current-uml.md)에 있다.

- 작성일: 2026-09-30
- 작성: View 작업(스테이지 내부 담당, 로컬 세션)
- 대상 브랜치: `feature/Yonggeun/Stage`
- 상태: **Phase 0~3 완료. Phase 5 중 준비 Job 연결만 병합으로 선반영. Phase 4는 Core Task 4A(F3 최소 결과 타입) 대기.** Core는 Task 4A 작업 중이라고 알려 왔다.

## 1. 한눈에 보기

| 항목 | 상태 |
|---|---|
| 끝난 코드 | `JobType` 3개, `CombatPlan`과 Job 네 개, `CombatFlow` 1단계, `CombatLifeCycle` 훅 1단계, 준비 Job의 `CoreGroup.Combat.PrepareBattle()` 연결 |
| 아직 틀만 있는 것 | 명령·미리보기·진행 Job 세 개(미연결 예외), Flow의 `Automatic` 처리(등록 없이 대기), 이탈(미완성) |
| 실행 경로 | 부트스트랩·`Change(Combat)` 호출자·씬 인덱스 3이 없어 **실행 중 전투 구간에 들어오지 않는다.** 들어오더라도 운영 준비 입력(G7)이 없어 준비에서 예외로 멈춘다 |
| 검증 | Phase 1·2·3과 병합 뒤마다 사용자가 에디터 컴파일(오류 없음)과 EditMode(모두 통과)를 확인. 병합 뒤 `JobSystemTests` 가짜 제거 재실행도 문제없음(사용자 보고) |
| Core와의 합의 | [Core 답변](2026-09-30-combat-core-response-to-view.md)으로 **K2(Flow·Plan·Job은 View) 확정**, 병합 방식 승인, 게임 규칙 확정(아래 §4.3) |
| 막힌 곳 | Phase 4: Core Task 4A-1(타입)·4A-3(실제 읽기 등록). Phase 5: Core Task 7(처리 API, 2026-10-01 Core 답변). Phase 6: F1·F2. Phase 4는 Task 4A로 반영 완료(계획 §6.6) |

## 2. 작업 방식

규칙 전체는 `CLAUDE.md`에 있다. 이 작업에서 확인한 것만 적는다.

- Phase마다 멈추고 보고한다. 보고 끝에 **다음 Phase의 목표·파일·핵심 동작·주의점·확인 방법**을 적는다.
- 새 Phase는 워크시트로 할지 직접 할지 묻는다. 이 작업은 Phase 1~3을 모두 **"너가 진행해줘"**로 맡겼다.
- Claude는 Unity를 실행할 수 없다. 컴파일·테스트는 **실행할 테스트 이름을 적어 사용자에게 요청**하고, 받은 결과를 계획 §6에 "사용자 확인/보고"로 적는다.
- 설계·계획과 다른 판단이 필요하면 구현은 설계대로 두고 먼저 묻는다(예: K11 네 번째 조건).
- 한 사실은 한 곳에만. 설계·계획·인계의 표현이 어긋나면 같이 고친다. 코드를 바꾸면 관련 문서의 "현재 상태" 문장도 함께 확인한다.
- 병합·커밋·push·외부 전달은 사용자가 한다. 이 세션에서도 사용자가 Core 병합을 직접 했고, 다른 세션이 병합 충돌 해결과 문서 기록을 도왔다.
- Core 브랜치를 볼 때는 먼저 `git fetch`로 새 커밋을 확인한다. 이번에도 작업 중에 Core 커밋이 두 번 올라왔다(`1a51234c`, `c89caecc`).

## 3. 커밋 이력과 커밋 전 변경

| 커밋 | 내용 |
|---|---|
| `3a46b623` | 설계·계획, Core 전달 문서 §13, Framework 공유 문서(Phase 0) |
| `d1dcb92f` | JobType·Plan·Job 골격, `JobSystemTests` 표본 교체, `CombatPlanTests`(Phase 1) |
| `23934177` | `CombatFlow`, `CombatFlowTests`(Phase 2) |
| `4d0c63ea` | K11 네 번째 조건, `CombatLifeCycle` 훅(Phase 3), `.meta` 두 개, 중간 인계, `CLAUDE.md` |
| `916c948e` | 요청 총정리, 현재 구조 UML, 문서 갱신 |
| `02b9529a` | Core `1a51234c` 병합: 준비 Job 연결, `CombatPlanTests` 준비 검증, Core 테스트의 Job 이름 변경 |

**아직 커밋하지 않은 변경**(사용자가 일부를 스테이징해 둔 상태):

| 파일 | 내용 |
|---|---|
| `Assets/Tests/EditMode/Framework/JobSystemTests.cs` | 병합으로 들어온 쓰지 않는 준비 가짜 제거(Core 답변 §2-5). 재실행 문제없음(사용자 보고) |
| `docs/superpowers/handoffs/2026-09-30-combat-core-response-to-view.md` | Core 답변 원문. **Core `c89caecc` 판으로 교체했다**(Core가 같은 경로에 커밋했으므로 병합 충돌을 피하려고). 스테이징된 옛 판과 다르므로 다시 스테이징해야 한다 |
| 설계 5차, 계획, 요청 총정리, 중간 인계 | 병합·답변 반영 |
| `docs/superpowers/plans/2026-09-30-combat-flow-core-intake-guide.md` | 새 문서: Core 인계물 인수 가이드 |
| 이 문서 | 새 인계 |

`FMODStudioCache.asset` 변경은 에디터가 만든 것이라 작업과 무관하다.

## 4. 만든 것과 정해진 것

### 4.1 코드

경로는 `Assets/ProjectL/Script/Systems/RuntimeSystem/` 기준이다.

| 파일 | 역할 |
|---|---|
| `GlobalSystem/JobSystem/JobRegister/JobRootType.cs` (공용) | `ProcessCommand = 3`, `ProcessPreview = 4`, `AdvanceCombat = 5` |
| `LifeCycleSystem/LifeCycles/Combat/Plans/CombatPlan.cs` | JobType마다 Job 하나, Priority 0 |
| `…/Combat/Jobs/CombatPrepareJob.cs` | `CoreGroup.Combat.PrepareBattle()` 한 번, 정상 반환 뒤 완료(GUID `87148ad8…` 유지) |
| `…/Combat/Jobs/CombatCommandJob.cs` · `CombatPreviewJob.cs` · `CombatAdvanceJob.cs` | 미연결 예외. 서비스가 오면 호출 한 줄로 교체 |
| `…/Combat/CombatFlow.cs` | 진행의 주인 하나. 1단계(F3 없음) |
| `…/Combat/CombatLifeCycle.cs` | 훅 네 개. 1단계(이탈 미완성) |
| `Assets/Tests/EditMode/Framework/CombatPlanTests.cs` · `CombatFlowTests.cs` | Plan·Job, Flow 검증. `CombatPlanTests`의 `FakeCoreService`가 테스트에서 유일한 CoreGroup 가짜다 |

Flow의 판단 순서, 상태, 훅 동작은 [현재 구조 UML](../specs/2026-09-30-combat-flow-current-uml.md) §6·§7과 [설계](../specs/2026-09-30-combat-flow-skeleton-design.md) §4에 있다.

### 4.2 결정(설계 §9)

| 번호 | 결정 |
|---|---|
| K1 | 연출 대기는 Job이 아니라 Flow 조건 |
| K2·K12 | Flow·Plan·Job·JobType은 View, 처리 서비스·규칙·상태/결과 기록은 Core. **Core 답변으로 확정.** 병합은 View 구조로 |
| K3 | 새 JobType 이름 `ProcessPreview`(4), `AdvanceCombat`(5) |
| K4·K8 | 되묻지 않기. 자동 진행 성공은 순번이 아니라 F3 결과로 판단 |
| K5 | 연출이 끝났으면 명령이 미리보기보다 먼저 |
| K6·K7 | 예외 재실행 방지(F1), 전환 전 Job 종료(F2)는 권고안, 합의 대기 |
| K9 | F3 전에는 `Automatic`에서 등록하지 않고 기다림 |
| K10 | LifeCycle 훅은 도달 불가 상태에서 먼저 연결 |
| K11 | `BeginPreparation` 조건 넷(멈추지 않음, Runner Idle, 전투 없음, 기다리는 처리 없음), 위반 시 예외 |
| K13 | 자발적 포기·로비 복귀는 패배. 이탈 순서에서 Core 패배 기록을 정리·전환보다 먼저 |

### 4.3 Core 답변으로 확정된 게임 규칙

- 서브는 다음 전투에 영혼을 포함한 최대 HP로 시작한다(이전 피해·사망 초기화). 메인은 HP와 지속 수치를 유지한다.
- 자발적 포기·로비 복귀는 패배다. View가 Buffer를 비우고 장면만 바꿔 패배를 건너뛰면 안 된다.
- 타임라인 예고는 기본 10칸이며 개수를 조절할 수 있어야 한다.
- 게임 튕김 복구는 방향만 나왔고 확정되지 않았다.

## 5. 알아 둘 것

- **Core 다음 작업은 Task 4A다**(천명님 계획, `c89caecc`). Step 1 타입, Step 2 호출 의미 합의, Step 3 실제 읽기 등록, Step 4 보고. Core가 넘기는 API는 **천명님 계획 §4.1 표**에 기록하기로 했다.
- **Step 1만 와서는 Phase 4 코드를 커밋할 수 없다.** Flow 생성자에 결과 읽기를 필수로 더하면 `CombatLifeCycle`이 넘길 실제 서비스(Step 3)가 필요하다. Step 1 때는 초안을 준비하고, Step 3과 함께 반영한다([인수 가이드 §2.1](../plans/2026-09-30-combat-flow-core-intake-guide.md)).
- **`OnBindServices`는 전투에 들어가지 않아도 실행된다.** `FrameworkRuntimeCompositionTests`의 `ChapterCoordinator_…` 두 테스트가 이 바인딩을 지난다.
- **Runner는 예외가 난 Job을 붙잡는다.** 다음 Tick에 같은 Job을 다시 실행한다. Flow의 `Stop`으로는 막을 수 없다(F1).
- **Core가 CoreGroup에 멤버를 더하면 `CombatPlanTests`의 `FakeCoreService`가 컴파일되지 않는다.** 그 멤버를 넣으면 된다.
- **Tick 순서는 Runner → View → Flow다.** Flow가 N 프레임에 등록한 Job은 N+1 프레임에 실행된다.
- **View가 지켜야 할 것**(전투 View 설계로 넘길 것, 계획 7.4)
  - 상태 변경 결과를 확인하면 기존 미리보기를 무효화하고, 필요하면 `SetPreview`로 새 번호를 받는다.
  - 거절·연출 없는 결과도 `AcknowledgePresented`로 확인한다. 안 하면 Flow가 연출 대기에서 멈춘다.
  - 배치 → 전투 입력 Layer 전환은 `EnterCombat` 수락 결과를 본 뒤 View가 한다.
  - 자발적 포기는 Core 패배 기록을 거친다.

## 6. 외부 대기

| 받을 것 | 누구 | 우리 작업 | 인수 가이드 |
|---|---|---|---|
| Task 4A-1 F3 최소 결과 타입 | 천명님(작업 중) | Phase 4 초안 | §2.1 |
| Task 4A-2 명령·미리보기·진행 호출 의미, 국면 대응 | 천명님 | Flow 확인 조건 대조, Phase 5.0 | §2.2 |
| Task 4A-3 실제 읽기 서비스 등록 | 천명님 | Phase 4 반영 + 4.4 바인딩 | §2.3 |
| Task 7 처리 API·결과 발행(Task 5만으로는 시작하지 않음) | 천명님 | Phase 5 | §2.4 |
| Task 7 상세 사건·미리보기·드로우·타임라인 | 천명님 | 전투 View | §2.5 |
| G7 운영 준비 입력(일정 미정) | 천명님 | 실제 진입(계획 밖) | §2.6 |
| F2 Core 몫: 패배 기록 호출 수단, `EndBattle` 호출자 | 천명님 | Phase 6 | §2.7 |
| F1, F2 Framework 몫 | 민형님 | Phase 6 | §2.7 |

**천명님께 아직 전하지 않은 것**(Core 답변 뒤에 생긴 요청):

1. 자발적 포기(패배)를 무엇으로 부르는가: 명령 종류 추가 / 별도 서비스
2. CoreGroup에 멤버를 더할 때 `CombatPlanTests`의 `FakeCoreService`도 같은 커밋에서 맞추거나 알려 달라
3. 병합 테스트 결과 공유(Core 답변 §2-7이 요청): 계획 §6.5의 검증 표 + `JobSystemTests` 재실행 결과(문제없음)

## 7. 다음 시작점

1. **정리(사용자):** §3의 미커밋 변경 커밋(답변 문서는 다시 스테이징) → §6의 전하지 않은 세 가지를 천명님께 전달.
2. **Core 인계물이 오면:** [인수 가이드](../plans/2026-09-30-combat-flow-core-intake-guide.md) §1의 공통 절차 → 해당 절(§2.x) → 계획의 해당 Phase.
3. **기다리는 동안: 전투 View 설계.** [전투 View 설계 세션 인계](2026-09-30-combat-view-design-session-handoff.md)의 작업 방식·설계 원칙(§4)을 따르고, 결정 대기 6개(§6) 중 1~3번(부트스트랩, 전투 씬, 범위)부터 묻는다. 먼저 `docs/superpowers/viewDocs/`와 옛 재설계 문서를 읽는다. 설계하면서 준비 인덱스·리비전의 쓰임새를 정리해 Core에 보낸다(Core 답변 §6).

## 8. 문서 지도

| 문서 | 용도 |
|---|---|
| [Core 인계물 인수 가이드](../plans/2026-09-30-combat-flow-core-intake-guide.md) | **Core 작업이 오면 먼저 볼 문서.** 인계물별 확인 항목과 이어지는 Phase·Task·파일 |
| [구현 계획](../plans/2026-09-30-combat-flow-skeleton-plan.md) | Phase 0~7 Task, §3.1 인수 표, §6 실행 기록(§6.5 병합) |
| [설계 5차](../specs/2026-09-30-combat-flow-skeleton-design.md) | Flow 분기(§4), Plan·Job(§5), LifeCycle·이탈(§6), 결정 K1~K13(§9), 합의 뒤 예상 UML(§12) |
| [현재 구조 UML](../specs/2026-09-30-combat-flow-current-uml.md) | Phase 0~3 시점 코드의 실제 구조(병합으로 준비 Job 연결 반영) |
| [Core 답변](2026-09-30-combat-core-response-to-view.md) | 천명님 답변 원문(`c89caecc` 판) |
| [전투 Core 요청 총정리](2026-09-30-combat-core-requests-consolidated.md) | 보낸 요청 원문과 §8 답변 기록 |
| [Framework 공유 문서](2026-09-30-combat-flow-framework-share.md) | 공용 변경, F1·F2 요청(민형님 답변 대기) |
| 천명님 [Core 이전 계획](../plans/2026-09-29-character-buffer-effect-migration-plan.md) | §4.1 Core API 기록 표, Task 4A~8 |
| [중간 인계](2026-09-30-combat-flow-local-session-handoff.md) | Phase 0~3 시점 인계(이 문서가 이어받음) |
| [전투 View 설계 세션 인계](2026-09-30-combat-view-design-session-handoff.md) | 전투 View 설계의 출발점: 원칙(§4), 결정 대기(§6), Buffer 점검(§10.1) |

## 9. 새 세션 시작 예시

```text
docs/superpowers/handoffs/2026-09-30-combat-flow-core-wait-session-handoff.md 부터 읽어.
천명님이 Task 4A Step 1(F3 타입)을 올렸어. 인수 가이드 §2.1대로 확인하고 Phase 4 초안 준비하자.
```

```text
docs/superpowers/handoffs/2026-09-30-combat-flow-core-wait-session-handoff.md 부터 읽어.
Core 기다리는 동안 전투 View 설계 시작하자. 인계 §7-3대로 결정부터 물어봐.
```
