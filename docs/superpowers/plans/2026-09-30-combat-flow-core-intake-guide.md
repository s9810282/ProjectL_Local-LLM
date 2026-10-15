# 전투 진행 골격 — Core 인계물 인수 가이드

- 작성일: 2026-09-30 (현재 인수 기준 갱신 2026-10-03)
- 작성: View 작업(스테이지 내부 담당)
- 용도: **Core 작업이 넘어왔을 때 무엇을 확인하고, 우리 계획의 어느 Phase·Task로 이어지는지** 한곳에서 보기 위한 문서. 새 세션이 이 문서만 보고도 인수 작업을 시작할 수 있게 쓴다.
- 기준: `e0b02cc7` 병합본. 6A/B3 및 View 잔여 검증 완료 확인 뒤 **골격 6B의 F1 검증·보완**을 진행한다. 최신 실행 상태는 골격 계획 §6, View 계획 §7이 원본이며 현재 순서는 골격 계획 §0.1을 따른다. Core 요청은 [2026-10-05 Core 요청사항](../handoffs/2026-10-05-combat-core-requests-phase6b.md)에 정리했다. 아래 §2.8의 6A 착수 안내는 당시 이력이다.
- 함께 볼 문서
  - 우리 [구현 계획](2026-09-30-combat-flow-skeleton-plan.md): Phase 4~7의 Task 목록과 §3.1 인수 표
  - 우리 [설계 8차](../specs/2026-09-30-combat-flow-skeleton-design.md): 진입 계약(§6.3), Flow 분기(§4), Completion 기반 이탈(§6.1·§6.2)
  - [Core 이전 계획](2026-09-29-character-buffer-effect-migration-plan.md) §4.1, [Task 7 계약](2026-10-01-combat-buffer-command-task7-contract.md): 제공 API 기록. 현재 실행 순서·검증 상태는 우리 계획 §0.1·§6.8
  - [Core 답변](../handoffs/2026-09-30-combat-core-response-to-view.md)

## 0. 한눈에

```text
Core 인계물                              우리 작업
─────────────────────────────────────    ───────────────────────────────
Task 4A    F3 타입·등록·호출 의미       ─► 인수 완료, Phase 4 완료
Task 7     실제 처리 API·결과 발행      ─► 인수·Job 연결 완료, Phase 5 완료
Task 7     상세 사건·미리보기·조회      ─► 인수 완료, 표시 범위·읽기 경계는 별도 설계
81c8c38    명시적 전투 진입·공동 게시    ─► 골격 6A + View B3 (다음 작업)
T1/G7      운영 DB 준비                ─► 미완, fixture 기반 Phase 5·개발 Live와 분리
View B1·C·E 단일 F1                    ─► 골격 6B에서 인수, View E/E2 검증과 연결
F2 Core·Framework·View 호출/표시 합의   ─► 골격 6B 이탈·재진입 구현
```

§2.1~§2.4는 기존 인수 이력과 Phase 5 연결 근거다. 다시 기다리거나 재구현하지 않는다. 새 작업은 **§2.8 → 골격 계획 6A.0**에서 시작한다. 계획의 구체적인 Task와 완료 기준은 골격 계획을 단일 원본으로 사용한다.

## 1. 인계물이 왔을 때 공통 절차

1. **가져와 확인한다.**
   - `git fetch origin` 뒤 `git log --oneline HEAD..origin/feature/Cheon/refactor/core-system`으로 새 커밋을 본다.
   - 천명님 계획 §4.1 표에서 실제 파일·네임스페이스·어셈블리·멤버·조회 경로·반환 의미를 읽는다. 표에 없으면 코드에서 직접 확인하고, 추측으로 채우지 않는다.
2. **기록한다.** 우리 계획 §3.1 인수 표와 [요청 총정리](../handoffs/2026-09-30-combat-core-requests-consolidated.md) §8에 담당·날짜·실제 경로·멤버를 적는다.
3. **병합할지 사용자에게 묻는다.** 병합은 사용자가 결정한다. 병합 때 주의할 점은 §4에 있다.
4. **해당 Phase를 시작한다.** 새 Phase는 워크시트로 할지 직접 할지 묻는다. Phase가 끝나면 멈추고 보고한다.
5. **Unity 검증을 사용자에게 요청한다.** 실행할 테스트 이름을 적어 요청하고, 받은 결과를 계획 §6에 "사용자 확인/보고"로 적는다.

## 2. 인계물별 확인과 우리 작업

### 2.1 Task 4A-1 — F3 최소 결과 타입 → Phase 4 초안

**Core가 줄 것(천명님 계획 Task 4A):** `Core/Contracts/Combat/`의 결과 값 타입과 처리 종류, `Buffers/Combat/CombatStateBuffer.cs` 옆의 결과 읽기 인터페이스(가칭 `ICombatResultReadSink`).

**받으면 확인할 것**

| 확인 | 왜 | 어긋나면 |
|---|---|---|
| "결과 없음"을 수락·거절과 구분해서 조회할 수 있나 | 전투 전 바인딩과 누락 판정 | Core에 확인 |
| 결과에 순번·처리 종류(명령/진행)·명령 번호(진행이면 0)·수락/거절·이유가 있나 | Flow가 "S 이후의 진행 결과가 수락인가"를 판단 | Core에 확인 |
| 등록 때 기록한 순번 S보다 **뒤의 진행 결과**를 찾을 수 있나(최신 하나인지, 순번 조회인지) | 설계 §4.3 `AdvanceCombat` 확인 | 최신 하나뿐이면 "최신 결과의 순번 > S이고 종류 = 진행"으로 판정. 판정 규칙이 바뀌면 설계를 먼저 고치고 사용자에게 묻는다 |
| 최신 결과가 View 확인 뒤에도 남는가 | Tick 순서가 Runner → View → Flow | Core에 확인(Core 답변 §5가 보장하기로 함) |
| 값 타입이 불변이고 Unity 타입이 없는가 | P1, Core 계약 | Core에 확인 |

**우리 작업(계획 Phase 4)**

| Task | 파일 | 변경 |
|---|---|---|
| 4.0 | 계획 §3.1 | 실제 타입 경로·멤버 기록 |
| 4.1 | `LifeCycles/Combat/CombatFlow.cs` | 생성자에 결과 읽기 추가(null 거부). `PendingCheck`에 `Advance` 추가 |
| 4.2 | 같은 파일 | `Tick`의 "Automatic이면 기다린다"를 "순번 S 기록 + `Register(Combat, AdvanceCombat)`"로 교체 |
| 4.3 | 같은 파일 | `TryConfirmPending`에 `Advance` 판정: S 이후 진행 결과가 수락이면 통과, 거절·누락·종류 불일치·순번 미갱신이면 `Fail` |
| 4.5 | `Tests/EditMode/Framework/CombatFlowTests.cs` | 가짜 결과 읽기 추가. `Automatic_FirstStage_…` 테스트를 새 기대로 교체. 자동 진행 등록·결과·거절 뒤 재등록 없음·결과 조회 수명 시나리오 추가 |

**주의 — 코드 반영 시점:** Flow 생성자에 필수 인자를 더하면 `CombatLifeCycle.OnBindServices`가 그 인자를 넘겨야 컴파일된다. 실제 서비스(Step 3)가 없으면 넘길 것이 없고, 빈 가짜 서비스를 생산 코드에 두는 것은 금지다(계획 §1, Core 답변 §5).

- 그래서 **Step 1만 왔을 때는 Flow·테스트 변경을 초안(워크시트나 로컬 변경)으로 준비하고, 커밋 반영은 Step 3과 함께 한다.**
- 초안 단계에서도 가칭 이름을 우리 쪽에 선언하지 않는다. Core가 준 실제 타입만 쓴다.

### 2.2 Task 4A-2 — 호출 의미·수명 합의 → Flow 확인 조건 대조

**Core가 줄 것:** 명령·미리보기·진행의 멤버 이름과 반환, "명령 없음"의 의미, 국면 전환, 결과 교체·확인 정책, 번호 범위. Core는 이 단계에서 실행 가능한 빈 메서드를 만들지 않는다.

**받으면 우리 Flow와 대조할 것**

| Core 계약 | Flow의 현재 동작(설계 §4.1·§4.3) | 맞지 않으면 |
|---|---|---|
| 명령 없음일 때 반환 | Flow는 명령이 있을 때만 등록한다. 처리 뒤 처리 번호가 기대 번호 이상이 아니면 **멈춘다** | 멈춤이 맞는지, 조건을 바꿀지 사용자에게 묻는다 |
| 미리보기가 바뀌거나 비워졌을 때 | 응답 번호가 기대 이상이거나, 현재 요청이 없거나 번호가 다르면 통과 | 같음 |
| 국면 전환(배치, 전투 개시, 적 선공, 기믹, 밤, 승패) | `PlayerInput`에서만 명령·미리보기, `Automatic`에서 진행, `Finished`·`None`은 등록 없음 | 국면 값이 Flow 표와 어긋나면 설계 §4.1을 먼저 고친다 |
| 번호 | Flow는 `≥`·`>`로만 비교. 입력 번호 건너뜀 허용 | 정확히 +1을 요구하는 계약이면 Core에 확인 |
| 결과 교체·확인 정책 | View가 확인해도 최신 결과는 Flow가 읽을 수 있어야 함 | Core에 확인 |

이 단계에서 Flow 코드는 바꾸지 않는다. 대조 결과를 계획 §3.1과 Phase 5.0에 적는다. Core 답변에서 "준비 인덱스·리비전의 쓰임새"와 "막힌 칸 출처"도 이 단계에서 묻기로 했다. 쓰임새는 전투 View 설계에서 정리해 보낸다.

### 2.3 Task 4A-3 — 실제 읽기 서비스 등록 → Phase 4 반영 + 4.4

**Core가 줄 것:** 실제 `CombatStateBuffer` 저장소를 보는 읽기 서비스와 조회 경로. 천명님 계획은 기존 Buffer 조립 경로(`ServiceKeys/DataBaseGroupServiceKey.cs`의 키, Buffer 그룹)를 쓰기로 했다. 전투 전에는 "결과 없음"을 돌려준다.

**받으면 확인할 것**

- 조회 경로: `IBufferQuery`의 새 속성인지, 별도 키인지
- 전투 전 `PostBindService`에서 조회가 성공하고 "결과 없음"을 돌려주는지
- 상태 Buffer와 같은 저장소를 보는지

**우리 작업**

| Task | 파일 | 변경 |
|---|---|---|
| 4.4 | `LifeCycles/Combat/CombatLifeCycle.cs` | `OnBindServices`에서 결과 읽기를 꺼내 Flow에 넘긴다 |
| 4.1~4.3·4.5 반영 | §2.1의 초안 | 이 시점에 함께 커밋 |
| 검증 | — | Unity 컴파일, `CombatFlowTests`, **`FrameworkRuntimeCompositionTests`**(전투 전 실제 바인딩), `CombatPlanTests`, `JobSystemTests` |

### 2.4 Task 7 — 실제 처리·결과 발행 → Phase 5

> 2026-10-02 인수: `83f485d1`에 실제 처리·기록 구현이 포함됐다. Phase 5에서 세 Job 연결·통합 테스트 작성을 완료했으며 실행 검증은 골격 계획 §6.8에 남아 있다. 운영 준비 T1/G7은 이 단계의 선행 조건이 아니다.

**인수한 계약:** `IInGameCoreSystemGroupService.Combat`의 `ICombatProcessingService`.

| 멤버 | 정상 반환과 확인 |
|---|---|
| `PrepareBattle()` | Phase 5 당시 연결. 현재는 확정된 선택의 호환용이며 새 진입은 §2.8의 `EnterBattle`로 이전 |
| `ProcessNextCommand()` | 처리할 명령 없으면 false. true에는 규칙 거절도 포함. Flow는 처리 번호, View는 결과 수락/거절을 읽음 |
| `ProcessLatestPreview()` | 전투/새 요청 없음 또는 이미 응답한 요청이면 false. 응답 번호 갱신, 일반 결과 발행 없음. 요청 교체·취소는 Flow가 허용 |
| `AdvanceCombat()` | 전투 없음/Automatic 아님이면 false. 처리 시 결과 하나. Flow는 새 순번·Kind·Accepted 확인 |

세 bool은 Job 완료 여부가 아니다. 정상 반환이면 true/false 모두 Job 완료, 예외면 완료하지 않고 전파한다. 명령·자동 진행의 예상 기록이 없으면 Flow가 한 번 정지하며 재시도하지 않는다. K14 입력 유지를 보장한 정상 경로와 고의 위반 테스트를 구분한다.

**우리 작업(계획 Phase 5)**

| Task | 파일 | 변경 |
|---|---|---|
| 5.0 | 계획 §3.1 | 실제 멤버·반환 의미 기록 |
| 5.1 | `Jobs/CombatCommandJob.cs`·`CombatPreviewJob.cs`·`CombatAdvanceJob.cs` | 미연결 예외를 서비스 호출 한 줄 + `IsCompleted = true`로 교체. `CombatPrepareJob`과 같은 모양 |
| 5.2 | `Tests/EditMode/Framework/CombatPlanTests.cs` | 세 Job의 미연결 기대를 호출·완료·예외 보존 검증으로 교체 |
| 5.3 | `Tests/EditMode/Framework/CombatFlowIntegrationTests.cs`(새 파일) | 실제 Register → Plan → Runner → Core → Buffer 경로. 명령 수락·거절, 미리보기 교체·해제, 자동 진행 수락·거절, 번호 미갱신, 최신 결과 보존 |
| 5.4·5.5 | — | 결과 보존·예외 전파 확인, Unity 검증 |

**주의**

- Core가 `IInGameCoreSystemGroupService`에 멤버를 더하면 `CombatPlanTests`의 `FakeCoreService`가 컴파일되지 않는다. 그 멤버를 넣으면 된다(쓰지 않으면 `null`, 검증에 쓰면 호출 기록 가짜).
- 세 서비스가 모두 도착해 Phase 5에서 함께 연결했다. 아래 검증·범위 조건은 그대로 유지한다.
- 계산 입력은 기존 `IBattleInitializationSource`·DB query fixture로 준다. 테스트 행위에 맞는 공격 프로필·경제 정의·로스터·진행·재화 전제를 명시한다. 실제 Core가 기록하게 하며 옛 Core 직접 호출이나 번호만 올리는 가짜 성공을 통합 증거로 쓰지 않는다.

### 2.5 Task 7 — 상세 사건·미리보기·표시 값 → 전투 View

사건·미리보기 응답 및 `GetDrawAvailability`·`GetTimeline` 조회는 도착했다. **Flow의 책임은 늘리지 않는다.** View의 현재 범위 (b)도 유지한다. 사건별 실제 연출·타임라인·규칙 기반 HUD를 추가할 때 별도 표시 설계와 읽기 경계를 정한다. Core 조회 API의 존재만으로 View의 직접 서비스 조회를 승인하지 않는다.

### 2.6 Task 8·G7 — 생산 바인딩과 실제 진입

기본 `DatabaseBattleInitializationSource`의 생산 등록과 A2의 지정 방식·유닛 참조/query는 제공됐다. Core의 공격 프로필·전체 준비 정의 조립/반환은 아직 연결되지 않아 운영 준비가 막혀 있다. 실제 데이터 인수는 정상 로스터·챕터·진입 호출 경로에서 검증하며 개발 토글만 끄는 것으로 완료되지 않는다. 개발 Live는 명시적 fixture로 검증할 수 있다. 안전한 이탈·재진입에는 F2가 추가로 필요하다.

### 2.7 F1·F2 → Phase 6

| 받을 것 | 누구 | 우리 작업 |
|---|---|---|
| F1: Tick·입력·전환·새 등록을 차단하는 단일 경계 | View B·C·E 구현, Framework 공유 | 6.0a·6.3에서 같은 구현과 증거 인수. 중복 구현 없음 |
| F2 Framework 몫: 전환 요청 소유자, Flow 정지 요청 API, 기존 Job 종료 확인 | 민형님 | 6.0·6.2 이탈 연결 |
| F2 Core 몫: Core가 현재 ArtifactLoadout을 조립하는 포기 호출 경계 | 천명님 | 현재 `AbandonBattle(ArtifactLoadoutSnapshot)`는 있음. View가 입력을 조립하지 않도록 실제 API 확정 요청 |
| F2 결과 수명·전환 | Core·Framework·View | 남은 CombatResult 확인 → 필요 시 포기 정산 → Completion 표시 완료 → EndBattle → 전환 |

**현재 계약에 따른 요청 초안:** Core가 현재 장비/로드아웃 입력을 읽어 포기 정산까지 수행하는 공개 경계를 제공하고 반환·Completion 보존을 확정해 달라고 요청한다. 인자 없는 서비스가 가능한지부터 협의하며 View에서 규칙 입력을 조립하는 우회는 하지 않는다. 현재 AbandonBattle은 Completion을 기록하고 새 CombatResult·순번·Finished 전환을 만들지 않는다. 따라서 새 결과를 기다리지 않고 Completion 표시 완료를 별도로 확인한다. 정상 완료의 Completion은 포기로 덮어쓰지 않는다. 반복 이탈은 정산·표시·EndBattle을 중복 실행하지 않는다. 실제 API·전환 소유자·표시 완료 경로는 **합의 전**이며 메시지는 발송하지 않았다.

### 2.8 `81c8c38` — 전투 진입 계약 → 골격 6A + View B3

**다른 골격 세션에서 바로 읽을 곳:** [골격 계획 Phase 6A](2026-09-30-combat-flow-skeleton-plan.md#phase-6a--명시적-전투-진입-계약-이전-다음-작업), [골격 설계 §6.3](../specs/2026-09-30-combat-flow-skeleton-design.md#63-명시적-전투-진입-계약-k20), [Core 원본 인계 V1~V5](../handoffs/2026-10-03-stage-entry-battle-preparation-handoff.md#gate-v-view-and-flow-migration-handoff-not-core-implementation).

| 인수 항목 | 사용할 계약 / 우리 작업 |
|---|---|
| 진입 API | `CoreGroup.Combat.EnterBattle(chapterKey, targetNodeId)`가 StageKey를 진행 상태에서 결정. 호출 전 로스터·챕터 준비 필요 |
| 수락 | 반환 전에 Current 선택·Character·Combat 게시. Job 완료·씬 활성화는 실행 계층 책임 |
| 거절 / 예외 | 정상 거절도 대응 요청에 결과를 저장한 뒤 Job 완료. Runner Idle 뒤 수신자가 결과 소비, 활성화/자동 재시도 없음. 예외는 기존 F1로 전파 |
| 준비 소스 | `Read(database, chapterKey, stageKey)`. 서명 호환은 병합 때 반영했으나 fixture는 실제 DB 조립 성공을 뜻하지 않음 |
| 이전 대상 | `JobRegisterSystem`, `CombatPlan`, `CombatPrepareJob`, `CombatFlow`, `CombatLifeCycle`; View B3의 Bootstrap·개발 모드와 함께 연결 |
| 기존 코드와 충돌 | Bootstrap의 Combat `ConfirmStageEntry`, `OnEnter → BeginPreparation`, HasBattle만의 성공 판정을 그대로 유지하지 않음 |
| 기록 모드 | 기존 로스터만 준비하는 경로는 새 Core의 선택 전제에 맞지 않음. B3에서 두 모드 모두 동일한 유효 진입 경로를 사용하고 수락 후 처리 등록만 구분 |
| 완료 기준 | 실제 Register/Plan/Runner/Job/Core/Buffer/Flow 경로로 첫/다음 대상·거절·예외·중복·취소/교체·한 번의 활성화 검증. 운영 이탈·DB는 별도 |

6A 구현 계약·호출 예시는 골격 설계 §6.3의 6A.0 확정 항목과 골격 계획 §6.10, B3 기록은 View 계획 §7.17을 따른다. 구현 세션이 달라도 대상/결과 저장소나 실행기를 따로 만들지 않는다. 준비 중 Combat이 비활성이면 해당 OnTick에 의존하지 않는 기존 실행 수명을 사용해야 한다. StageLifeCycle 전체 개편·회복/상점 진입은 이번 계약 이전 범위가 아니다.

이후 **6B F1 인수 + View E/E2 검증 → View A2/Core DB 연결과 F2 → 실제 DB·씬 인수 → 골격 7/View F**로 이어간다. A2와 F2는 서로의 전체 완료를 기다리는 관계가 아니다. `AbandonBattle(loadout)`의 Core 소유 입력 조립, 전환 소유자, Completion 표시 완료 계약은 여전히 별도다.

## 3. 하지 않는 것

- 이미 인수한 `ICombatResultReadSink`와 실제 처리 API를 쓴다. 미확정 F2 경계를 예상 이름으로 먼저 구현하지 않는다.
- 생산 코드에 빈 결과 서비스나 가짜 Core 서비스를 두지 않는다. 가짜는 테스트 파일 안에만 둔다.
- 결과 순번이 올랐다는 이유만으로 자동 진행을 성공으로 판단하지 않는다.
- Core 코드(처리 서비스, Buffer, Contracts)를 고치지 않는다. 필요하면 요청한다.
- 인계물이 오지 않은 Task를 완료로 표시하지 않는다. "타입 인수"와 "생산 등록"은 따로 표시한다.

## 4. 병합할 때 주의

| 파일 | 상황 | 처리 |
|---|---|---|
| `docs/superpowers/handoffs/2026-09-30-combat-core-response-to-view.md` | 우리와 Core가 같은 경로에 각자 추가했다. 우리 사본은 Core `c89caecc` 판으로 맞춰 두었다 | 내용이 같으면 충돌 없이 합쳐진다. 다르면 Core 판을 따른다 |
| `Tests/EditMode/Framework/CombatPlanTests.cs` | Core가 CoreGroup에 멤버를 더하면 컴파일 오류 | `FakeCoreService`에 멤버 추가(§2.4) |
| `Tests/EditMode/Framework/JobSystemTests.cs` | 병합 뒤 Core 준비 가짜를 제거했다(`CoreGroup => null`) | Core 쪽에서 이 파일을 다시 고쳤으면 Lobby 표본 유지 기준으로 합친다 |
| `Tests/EditMode/Framework/CharacterBattlePreparationTests.cs` | Core 테스트. `CombatPrepareJob`을 직접 쓴다 | Core가 `CombatJob`으로 되돌려 쓰지 않았는지 확인 |
| `LifeCycles/Combat/**`, `JobRootType.cs` | 우리 소유(K2) | Core가 고쳤다면 사용자에게 알리고 합친다 |

병합 뒤에는 최소한 `CombatPlanTests`, `CombatFlowTests`, `JobSystemTests`, `CharacterBattlePreparationTests`, `FrameworkRuntimeCompositionTests`와 Core가 새로 넣은 테스트를 함께 돌려 달라고 요청한다.
