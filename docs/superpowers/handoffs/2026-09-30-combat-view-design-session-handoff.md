# 전투 View 설계 — 새 세션 인계 문서

> **다음 작업 시작점:** [2026-10-05 골격·View 공동 인계](2026-10-05-combat-view-skeleton-session-handoff.md). 아래는 이전 시점의 결정/작업 이력이다. 현재 코드·완료 범위·대기는 새 인계의 원본 링크를 따른다.

> 컨텍스트가 길어져 **새 세션에서 전투 View 설계를 시작**하기 위한 문서다.
> 새 세션은 **이 문서 → 저장소 루트 `CLAUDE.md` → [Framework 조사 문서](../specs/2026-09-30-combat-framework-survey.md) → [입력 사용 안내](../viewDocs/stage-input-guide.md)** 순서로 읽는다.
> 대화 기록과 Claude 메모리는 원래 PC(Hyeon2-400)에만 있다. 필요한 내용은 이 문서에 모았다.

- 작성일: 2026-09-30
- 작성: View 작업(스테이지 내부 담당)
- 브랜치: `feature/Yonggeun/Stage`
- 직전 인계: [입력 작업 인계](2026-09-30-stage-input-local-session-handoff.md) · [Buffer 작업 인계](2026-09-29-combat-view-buffer-local-session-handoff.md)

## 0. 지금 위치

| 항목 | 상태 |
|---|---|
| 끝난 일 | CombatViewBuffer(입력 Buffer), 스테이지 내부 입력(UnityBridge·Controller·수신 슬롯), 입력 설정 View DB, ViewGroup 테이블 보충, 천명님 `CombatStateBuffer` 병합 |
| 다음 할 일 | **전투 View 설계**(새 설계·계획 문서로 시작). 이 세션 마지막에 정리한 두 판단이 §10에 있다: (1) Core → View Buffer 점검과 보강 제안, (2) 전투 진행 골격(LifeCycle·Flow·Plan·Job)을 지금 만들 수 있는 범위. 새 세션의 첫 작업 후보는 §10.3 |
| 설계 입력 | [Framework 조사 문서](../specs/2026-09-30-combat-framework-survey.md): 구동 방식, 쓸 수 있는 기능, 빠진 것, 결정할 것 |
| 막힌 곳 | 조사 문서 §5의 **결정 6개**가 사용자 답을 기다린다(§6) |
| 검증 | 입력 작업 Phase 1~4 에디터 컴파일 확인(에러 0). EditMode 테스트 실행 결과는 받지 못했다 |

## 1. 작업 방식

규칙 전체는 `CLAUDE.md`에 있다. 아래는 **CLAUDE.md에 없고 원래 PC의 메모리에만 있던 것**이다.

- **사용자는 스테이지 내부 담당이다.**
  - 스테이지 내부는 `CombatLifeCycle`(배치·전투)이다.
  - `StageLifeCycle`은 스테이지 **맵**이며 사용자 범위로 단정하지 않는다.
  - 사용자는 전투 Flow(`CombatLifeCycle`·`CombatPlan`·전투 Job)도 맡는다(Buffer 설계 D6).
- **순서:** 설계 → Task 계획 → Phase 단위 구현. Phase가 끝나면 멈추고 보고한다. 설계와 다른 결정이 필요하면 구현하지 말고 먼저 묻는다.
- **구현 방식:** 새 기능은 기본적으로 **실습 워크시트 artifact**로 준다. 사용자가 **"너가 진행해줘"**라고 하면 직접 구현한다. 최근 작업은 대부분 이렇게 진행했다.
- **Phase 보고 끝에 다음 Phase를 자세히 설명한다**(2026-09-30 요청): 목표, 파일, 핵심 동작과 주의점(공용 파일, 에디터 작업, 결정 필요 여부), 확인 방법.
- **문서 관련 지시**
  - "기존 계획을 날린다"는 참고하지 않는다는 뜻이다. 문서는 지우지 않는다.
  - "파일은 새로 작성"이라고 하면 기존 문서를 고치지 않고 새 파일을 만든다.
- **커밋·push는 사용자가 한다.** 공용 파일(Framework·다른 작업자 영역)을 바꾸면 공유 문서를 새로 쓴다(민형님: Framework, 천명님: Core).
- **검증**
  - 이 PC에는 dotnet이 없다. 컴파일은 Unity 에디터로 확인한다.
  - 순수 C#은 기존 `ProjectL.Tests.Framework`에 EditMode 테스트를 넣는다. **새 테스트 asmdef는 제안하지 않는다.**
  - 가상 장치 입력 테스트는 보류했다(B).
- **조사부터 한다.** View 구조를 제안하기 전에 기존 문서를 먼저 읽는다: `docs/superpowers/viewDocs/`, 옛 재설계 문서(`origin/feature/Yonggeun/Combat:docs/superpowers/viewDocs/stage-redesign/`). 이미 정해진 규칙을 어기는 설계를 내면 되돌리게 된다.

## 2. 프로젝트 구조

```text
Assets/ProjectL/Script/
├─ Framework/            공용 기반(Composition·SystemBase·DataBase·Utility). Systems·Core를 모름
├─ Systems/              ProjectL.Systems 어셈블리 — View 코드도 여기
│  ├─ DataBaseSystem/    Data(SO·Data 타입: InGame/View/Render), Queries(Table 조회)
│  ├─ Factory/           DBFactory, Runtime(Buffer·Global·InGame·LifeCycle 조립)
│  ├─ ServiceKeys/       서비스 키
│  └─ RuntimeSystem/
│     ├─ BufferSystems/  Current·Map·Stage·Artifact·Character·CombatState·Currency·CombatView
│     ├─ GlobalSystem/   Render·Utility·Input(+UnityBridge)·Loding·UI·Job (Audio는 미등록)
│     ├─ InGameSystem/   CoreSystem(StageGenerator·StageServiceManager·ChapterProgression + 옛 CoreCaculator)
│     │                  ViewSystem(InGameViewSystemGroup — 비어 있음, StageInput)
│     └─ LifeCycleSystem/ Lobby·Stage(맵)·Artifact·Combat(스테이지 내부), 각 Plans/·Jobs/
├─ Render/  Tool/Editor/
Assets/ProjectL/DataBaseSO/  InGameGroup(9/12 테이블), ViewGroup(10/10), RenderGroup
Assets/ProjectL/Input/PlayerInputActions.inputactions
Assets/Scenes/Stage.unity   (Build Settings 유일, 전투 객체 없음)
```

## 3. 게임이 도는 흐름

**조립 → 수명**

```text
FrameworkRuntimeFactory.Create()
  루트: 입력 수신 슬롯, Buffer, Global, InGame(Core·View), LifeCycle
  (DB는 DataBaseFactory.Create(카탈로그) → InitData로 주입)
수명: BindScene → InitData(DB, 한 번) → PostBindService → Initialize → Tick → Shutdown(역순)
```

**화면 전환 → 진행**

```text
LifeCycleManager.Change(key) → LodingSystem.Load(sceneIndex) → Enter Node → LifeCycle.OnEnter
(Flow) → JobRegister.Register(JobRootType, JobType)
      → Plan(LifeCycle당 static, JobType별 작업 시나리오) → Job(struct, 한 걸음) → JobRunner(Priority)
```

**데이터**

```text
DB(InGame) ─InitData→ Core 시스템 ─WriteSink→ Buffer ─ReadSink→ View
DB(View)   ─InitData→ View 시스템
View 입력  → CombatViewBuffer ─ReadSink→ Core 측(읽기만)
```

**입력**(자세히는 [사용 안내](../viewDocs/stage-input-guide.md))

```text
PlayerInputActions → UnityInputBridge → InputSystem.Submit → (Layer 허용?) → Controller
 → 수신 슬롯 → View(IStageInputReceiver) → CombatViewBuffer.Submit
```

**지금 비어 있는 것**(조사 문서 §3)

- 부트스트랩: 런타임 생성과 Tick
- 첫 LifeCycle 진입, LifeCycle 인덱스에 맞는 씬(Build Settings에는 0번뿐)
- 로딩 뒤 `BindScene` 재호출
- 전투 Flow와 `CombatPlan` 시나리오
- Core 처리 서비스와 결과·사건 경로(요청 R1~R6, 천명님 Task 4·7)
- 전투 View 모듈과 씬 객체

## 4. 반드시 지킬 설계 원칙

설계와 리뷰에서 **아래를 항목별로 점검한다.** 근거 문서는 JMH [Framework 개발 지침](../framework/Framework%20Development%20Guidelines.md), [프로그래머 가이드](../../../Assets/ProjectL/Script/Systems/Documentation/framework-programmer-guide.md), `CLAUDE.md`다.

### 4.1 SSOT — 한 가지 사실은 한 곳에만

| 사실 | 원본(유일한 곳) | 다른 곳이 하면 안 되는 것 |
|---|---|---|
| 기획 입력 값(유닛 능력치, 필드, 외형 프리팹) | DB SO(InGame/View/Render) | 런타임 상태 저장소로 쓰기(지침 §23 "Query와 Runtime 상태 분리") |
| 런타임 진행·전투 상태(위치, 체력, 턴, 국면, 결과 순번) | **Buffer**(Core 측이 씀) | Core·View가 가변 사본을 따로 들고 동기화하기. View는 Buffer를 읽어 표시하고, 표시용 캐시는 파생값으로만 둔다 |
| 규칙 판정(가능 여부, 비용, 피해, 승패, 미리보기 계산) | **Core** | View가 규칙을 흉내 내 계산하기(P6: 규칙 값은 Buffer로 받는다) |
| 사용자 입력 명령 | `CombatViewBuffer`(번호는 Buffer가 발급) | 다른 경로로 Core를 직접 호출하기 |
| 처리 여부(마지막 처리 번호) | Core 측 `CombatStateBuffer` | 입력 Buffer에 "처리됨" 상태 두기 |
| 키 배치 / 액션 이름 / Layer 구성 | `PlayerInputActions` / `StageInputNames` / `StageInputLayers` | 문자열 리터럴 흩뿌리기(지침 §14 "문자열 기반 조회 최소화") |
| 서비스 키 | `Systems/ServiceKeys/` | 임의 위치에 키 정의 |
| 계산 가능한 값 | 원본 값 | 파생 필드를 따로 저장(P7) |

### 4.2 SOLID — 이 프로젝트에서의 적용

| 원칙 | 이 프로젝트에서 | 예 / 경고 신호 |
|---|---|---|
| **S** 단일 책임 | 지침 §2 역할 분리: System(상태·수명) / Service(외부 API) / Scope(사용 단위 수명) / Handle(경량 접근) / Factory(생성·조립) / Group(도메인 묶음) / Store·Reader / Pool. §26 God Class 방지 | 입력: Controller("무엇을 했나"), 슬롯(연결·선택 소유권), View(뜻), Core(규칙). 옛 교훈: **"`SerializeField`로 Presenter나 이펙트 참조가 붙기 시작하면 Manager가 되는 중이다."** 진행의 주인은 하나(Flow)지만 규칙·표시·연출을 소유하지 않는다 |
| **O** 개방-폐쇄 | 새 기능은 새 클래스·등록으로 더한다: 새 Controller·Layer, 새 Plan 시나리오·Job, 새 테이블 | 기존 switch에 분기를 계속 붙이는 대신 등록 목록에 추가. 단, 공용 enum(`JobType`) 추가는 공용 파일 변경이라 공유한다 |
| **L** 리스코프 치환 | 인터페이스를 구현하면 문서화된 보장을 지킨다 | `IInputLayerRuntime.ClearLayer` 뒤 CurrentLayer는 default. `IStageInputReceiver.SelectCanceled`에서 명령 확정 금지. Job은 `IsCompleted` 의미를 지킨다 |
| **I** 인터페이스 분리 | 필요한 만큼만 받는다: ReadSink/WriteSink 분리, `IHasViewQuery`처럼 좁은 계약 | Core 측에는 ReadSink만, View에는 필요한 WriteSink만. `IBufferQuery`는 모두 노출하므로(D1, 관례로만 보호) 모듈에는 필요한 싱크만 꺼내 보관한다 |
| **D** 의존성 역전 | 구체 타입·씬 검색이 아니라 **서비스 인터페이스**에 의존한다. `PostBindService`로 받는다. 조립은 Factory(루트)가 한다 | 지침 §25 `GetComponent`/`Find` 남용 금지. Core.Application은 Buffer를 모른다. Global 입력은 스테이지 입력 구체 타입을 모른다(루트가 주입) |

**과하게 하지 않는다.** 지침 §27 "과도한 추상화 금지", §28 "추가 전 확인 순서"를 따른다. 쓰임이 하나뿐인 인터페이스나 계층은 만들지 않는다.

### 4.3 Framework 계약 (MUST, 프로그래머 가이드 §6)

- **서비스·등록**
  - FW-001: 서비스는 제공 인터페이스로 등록한다
  - FW-002: 수명 참여는 `AddModule`로 연결한다
  - FW-003: Builder는 한 번 Build하고 닫는다
  - 지침 §11: Build 이후 구조를 바꾸지 않는다
- **경계:** FW-004: Framework/Systems 내부 구현의 상태를 우회해서 바꾸지 않는다
- **입력:** FW-005~007. 바인딩은 Configure 중에만, Configure는 한 번, 입력은 `Submit` 경로로
- **UI:** FW-008·009. 등록 전제조건, 화면 이력 유지
- **렌더:** FW-010~012. 구체 DTO, 대여 자원은 Scope/Handle로 반환, DB 요구조건
- **이벤트·풀·큐·수명:** FW-013~016. 이벤트 타입 계약, Pool 계약, Queue 재정렬, `FrameworkRuntime` 선행 단계
- **이벤트 수명**
  - 지침 §20·21: 구독은 반드시 해제될 수 있어야 한다
  - DR-008: 전역 `SubscribeStage`는 개별 해제가 없다. 씬 단위 구독은 전용 버스 인스턴스 등으로 수명을 책임진다
- **열린 문제(설계 때 확인)**
  - DR-001: 부트스트랩과 씬 재바인딩
  - DR-009: Layer 전환과 Canceled
  - DR-010: UI 재바인딩과 Hide

### 4.4 DB·코딩 규칙

- `CLAUDE.md`의 DB 규칙 P1~P7과 코딩 규칙을 따른다.
  - InGame Data는 순수 C#
  - View는 InGame을 읽지 않는다
  - Allman·`sealed`·한국어 XML doc
  - 예외 규약
- 이름 충돌을 주의한다.
  - `…Data.InGame.Enum` 네임스페이스와 `Enum`
  - `…Table.Input`과 `UnityEngine.Input`(그래서 `InputSettings`로 지음)
  - `…InputSystem.Unity`와 `Unity.*`(그래서 `UnityBridge`로 지음)
  - 우리 `InputSystem`과 `UnityEngine.InputSystem.InputSystem`(별칭을 쓴다)

### 4.5 경계

- **Core 로직은 건드리지 않는다.** 필요한 것은 Core 전달 문서에 요청으로 적는다(`CLAUDE.md`).
- FieldCamera, Architecture는 다른 View 작업자 영역이다.
- Framework 공용 파일(ServiceKeys, Factory, Group, asmdef, `JobType`)은 필요한 줄만 바꾸고 민형님께 공유 문서를 쓴다.

## 5. 설계에 들어갈 때 참고할 기존 결정

| 출처 | 내용 |
|---|---|
| [Buffer 설계](../specs/2026-09-29-combat-view-buffer-design.md) | 번호 발급(V2), Core 측 처리 번호(V7), 모든 결과 확인(V8), 명령 8종·미리보기 6종 |
| [Buffer 설계](../specs/2026-09-29-combat-view-buffer-design.md) §12 | 전투 Flow 후보(참고용, 확정 아님): `OnTick`에서 `IsIdle`일 때 Register, 연출 대기 Job |
| [입력 설계](../specs/2026-09-30-stage-input-design.md) D4 | 배치 → 전투 Layer 전환은 "전투 시작" **수락 결과를 본 뒤**. 전투 → 배치(밤 종료)는 Core 결과 사건(R5)으로 안다 |
| [Core 요청](2026-09-29-combat-view-buffer-core-requests.md) | R1 결과 순번, R2 처리 번호, R3 미리보기 응답 번호, R4 국면, R5 사건 목록, R6 처리 서비스. R1~R4의 읽기 자리는 `CombatStateBuffer`에 생겼지만 값은 아직 움직이지 않는다 |
| 옛 재설계(`origin/feature/Yonggeun/Combat` viewDocs/stage-redesign) | 진행의 주인은 하나(Signal → Advance), 실패한 진행 명령은 다시 묻지 않고 멈춤, 화면 상태는 연출 뒤에 적용, 장면·박자 모델 |
| [대응표](../specs/2026-09-28-stage-view-buffer-mapping.md) | 옛 전투 View 코드 분류(R/C/Q/F/E/V/L). L(View 내부)은 거의 그대로 옮길 수 있다 |

## 6. 결정 대기 (조사 문서 §5)

1. 부트스트랩(확인용 최소 구동기)을 이번에 넣을지
2. 전투 씬 구성: `Stage.unity`를 쓸지 새 씬을 만들지, SceneIndex 맞추기, 씬 객체 연결 방식(로딩 뒤 `BindScene` 재호출 / `OnEnter`에서 찾기)
3. 이번 범위: (a) 입력 → 명령 기록 / (b) + Buffer 상태 표시 / (c) + 연출
4. Core가 준비되기 전 확인 수단: 가짜 상태를 Buffer에 넣기 / 옛 Core 임시 직접 호출(천명님 방향과 어긋남) / 보류
5. 옛 View 코드 가져오기 범위(L부터?)
6. 연출 사건 경로: R5를 기다릴지, View가 전후 상태를 비교해 만들지

새 세션은 이 여섯 가지를 먼저 묻는다. 최소한 1~3번이 정해져야 설계를 시작한다.
다만 §10.3의 1·2번(골격 설계, Core 요청 문서)은 이 결정 없이도 시작할 수 있다. 6번은 §10.1의 제안(결과 기록을 `CombatStateBuffer`에 요청)이 받아들여지면 "R5를 기다린다"로 정리된다.

## 7. 알려진 문제

- `InGameGroup.asset`의 테이블이 9개다(팩토리는 12개 요구). 그래서 `DataBaseAssetTests.Catalog_CreatesAllDataBaseGroups`는 실패한다([DB 스키마 정렬 계획](../plans/2026-09-29-db-schema-core-alignment-plan.md) T11). T12 나머지(UnitVisual 한글 이름·새 유닛 4종 등)도 남아 있다.
- 프로젝트 전역 액션(`InputSystem_Actions`)이 재생 때 자동으로 켜진다(받는 코드 없음).
- `Stage.unity`에 Missing Script 2개가 있다.
- `FMODStudioCache.asset`은 에디터가 자주 바꾼다. 작업과 무관하다.
- 문서로 알릴 것
  - 민형님: 입력 반영 결과(폴더명 `UnityBridge`, `StageInputServiceKey.cs`, 가이드에 `ClearLayer` 반영 요청)
  - 천명님: Core 요청 R1~R6

## 8. 문서 지도

| 문서 | 용도 |
|---|---|
| [Framework 조사](../specs/2026-09-30-combat-framework-survey.md) | **설계 입력.** 구동 방식, 기능 목록, 빠진 것, 결정할 것 |
| [입력 사용 안내](../viewDocs/stage-input-guide.md) | View 작업자용 입력 사용법과 예시 코드 |
| [입력 설계](../specs/2026-09-30-stage-input-design.md) / [계획](../plans/2026-09-30-stage-input-plan.md) | 입력 규칙 I1~I9, 결정 D1~D7, §11 구현 결과 |
| [Buffer 설계](../specs/2026-09-29-combat-view-buffer-design.md) / [계획](../plans/2026-09-29-combat-view-buffer-plan.md) | 입력 Buffer 규칙 V1~V9 |
| [Core 요청](2026-09-29-combat-view-buffer-core-requests.md) | R1~R6 |
| [천명님 캐릭터 Buffer 이전 계획](../plans/2026-09-29-character-buffer-effect-migration-plan.md) | Core 이전 방향(Task 0~2 완료) |
| [대응표](../specs/2026-09-28-stage-view-buffer-mapping.md) | 옛 전투 View 코드 분류 |
| [Framework 개발 지침](../framework/Framework%20Development%20Guidelines.md) | 설계 원칙(§2 역할, §11 Build 후 불변, §20 이벤트, §23 DB vs 런타임, §25~28) |
| [프로그래머 가이드](../../../Assets/ProjectL/Script/Systems/Documentation/framework-programmer-guide.md) | FW-001~016, DR-001~012 |
| `Assets/ProjectL/Script/Systems/Documentation/framework-architecture.md` | 조립·Job·로딩 구조 |

## 9. 새 세션 시작 예시

```text
docs/superpowers/handoffs/2026-09-30-combat-view-design-session-handoff.md 부터 읽어.
§10.3 1번(전투 진행 골격 설계)부터 하자.
```

```text
docs/superpowers/handoffs/2026-09-30-combat-view-design-session-handoff.md 부터 읽어.
§10.1 기준으로 천명님께 보낼 Core 요청 문서(R7~R11) 새로 써줘.
```

```text
docs/superpowers/handoffs/2026-09-30-combat-view-design-session-handoff.md 부터 읽어.
전투 View 설계 시작하자. 결정은 1번 ○, 2번 ○, 3번 ○.
```

## 10. 이 세션 마지막 판단 (2026-09-30)

사용자 질문에 답하며 정리한 내용이다. 아직 사용자 결정 전의 **제안**이며 문서·코드에는 반영하지 않았다.

### 10.1 Core → View Buffer 점검과 보강 제안

**지금 있는 것**(전투에서 Core가 쓰고 View가 읽는 것)

| Buffer | View가 얻는 것 | 값이 채워지나 |
|---|---|---|
| `CharacterBuffer.Characters`(`CharacterData`) | 개체별 종류 키, 진영, 메인/서브, 위치 구분(`CharacterRuntimeArea`: Hand/Field/EnemyWaiting/Excluded), 손패 슬롯, 앵커·방향·점유 칸, 체력, 능력치(`UnitStatsSnapshot`: 이동·공격 범위·비용, 적용 공격력, 방어, 행동력, 우선순위·딜레이, 특성 상태, 카테고리), 이동 정산, 임시 버프 | 저장 구조와 쓰기 입구(`PrepareBattle`/`Apply`)만 있다. 채우는 쪽은 천명님 Task 4~7 |
| `CharacterBuffer` 보유·영혼·메인 지속 상태 | 전투 밖 HUD | 있음 |
| `CombatStateBuffer.Current` | 보드 크기, 손패 용량, 밤 길이, 타임라인 시드, 드로우 순서·커서, 국면(`CombatFlowPhase`) | 준비할 때 설정. 국면은 초기값만 |
| `CombatStateBuffer` 카운터 | R1 결과 순번, R2 처리 번호, R3 미리보기 응답 번호 | 자리만 있다 |
| `CurrencyBuffer` / `ArtifactBuffer` / `CurrentProgressBuffer` | 골드 / 아티팩트 스냅샷 / 현재 스테이지 키·이름(View DB 조회 키) | 있음(Artifact는 쓰는 쪽 없음) |

옛 `StageSnapshot`과 비교하면, 개체 정보는 `CharacterBuffer`가 거의 다 덮는다. 비어 있는 것은 **전투 공통 상태**와 **명령·미리보기 결과 내용**이다.

**비어 있는 것**

| 구분 | 필요한 정보 | 비고 |
|---|---|---|
| 전투 공통 상태 | 배치/전투 단계(`StagePhase`), 준비 인덱스, 메인 배치 잠금, 현재 턴(활성 유닛·리비전·완료 턴 수), 승패(결과·이유), 밤 경과·남음, 구조물(본부)·기믹(레일웨이), 필드 효과(가시밭·영혼), 아티팩트 발동 | 천명님 Task 2 인벤토리에 `CombatStateBuffer`의 "future typed read records"로 예정돼 있다 |
| 계획에 없는 것 | **필드 지형**(막힌 칸·레일 칸). View는 InGame DB를 읽지 않으므로(P6) 전투 준비 때 Core가 담아 줘야 한다 | 요청 필요 |
| 계획에 없는 것 | **수동 드로우 가능 여부·실패 이유·비용**. 규칙 판단이다(SSOT). 남은 장수는 View가 계산하므로 필요 없다 | 요청 필요 |
| 계획에 없는 것 | **타임라인 예고**(앞으로 N칸). 순서는 규칙 계산이다. 참가자 값만 주면 View가 규칙을 흉내 내게 되므로 Core가 계산한 투영값을 내야 한다 | 요청 필요 |
| **가장 큰 공백** | **명령 결과 내용**: 순번, 명령 번호, 수락/거절, 거절 이유, 일어난 일 목록(R5: 이동, 공격, 피격·피해, 사망, 반격, 영혼, 밤 종료·회수, 승리). View가 확인할 때까지 보관 | 카운터만 있다 |
| **가장 큰 공백** | **미리보기 결과 내용**: 요청 번호 + 이동 범위, 이동 예상(도착·비용), 공격 범위, 공격 예상(영향 칸·예상 피해·관통 감쇠), 배치 가능 여부. 최신 하나 | 응답 번호만 있다 |

**제안 구성**(새 Buffer를 늘리지 않는다)

| 어디에 | 무엇을 | 근거 |
|---|---|---|
| `CharacterBuffer` | 추가 요청 없음. 체력 변화 전후 값 같은 연출 데이터는 결과 사건에 싣고 Buffer에 두지 않는다 | SSOT, P7 |
| `CombatStateBuffer` 상태 | 위 "전투 공통 상태" 전부 + 필드 지형 + 드로우 가능 여부 + 타임라인 예고 | 천명님 소유 표에서 이미 전투 공통 상태의 자리로 정해져 있다 |
| `CombatStateBuffer` 안의 **결과 기록**, 별도 읽기 인터페이스(예: `ICombatResultReadSink`) | 결과 목록 + 최신 미리보기 결과 | SSOT: 순번 카운터와 내용을 한 곳에 둔다. SRP·ISP: 상태와 결과는 수명·소비자가 달라 읽기 인터페이스를 나눈다 |

**View가 계산하므로 Buffer에 넣지 않을 것**
- 손패 목록: `Area == Hand` + 슬롯
- 칸 점유: `OccupiedCells`
- 남은 드로우 수: 순서 길이 − 커서
- 메인 ID: `OwnershipRole == Main`
- 하이라이트 같은 표시 상태

이 전부가 **Core 쪽 Buffer**라 View가 만들지 않는다. 천명님께 **새 요청 문서**(새 파일)로 보내는 것을 권했다. R1~R4는 자리가 생겼고, R5·R6은 남아 있다. 추가 번호 제안은 다음과 같다.
- R7: 전투 공통 상태
- R8: 필드 지형
- R9: 드로우 가능 여부
- R10: 타임라인 예고
- R11: 미리보기 결과 내용
- R5는 결과 기록 구조로 구체화

### 10.2 전투 진행 골격 — Buffer를 제외하고 지금 만들 수 있는 범위

대상은 스테이지 **내부**(`CombatLifeCycle`)다. 스테이지 맵(`StageLifeCycle`·`StagePlan`)은 이미 동작하므로 손대지 않는다.

| 층 | 지금 만들 수 있는 것 | Core를 기다리는 것 |
|---|---|---|
| LifeCycle(`CombatLifeCycle`) | `OnEnter`: 입력 Buffer 비우기, 전투 준비 등록. `OnTick`: Flow 한 걸음. `OnExit`: 정리 등록, 입력 해제(`ClearLayer`·`Unbind`는 View 쪽) | 없음 |
| **Flow**(새, 진행의 주인 하나) | Runner가 비어 있을 때 국면을 보고 무엇을 등록할지 분기. 읽을 값은 이미 있다: `CombatStateBuffer.Phase`·`HandledCommandNumber`·`AnsweredPreviewNumber`·`LatestResultSequence`, `CombatViewBuffer`의 명령·미리보기·확인 번호 | 국면 값이 실제로 바뀌는 것 |
| Plan(`CombatPlan`) | JobType별 시나리오: 전투 준비 / 명령 처리 / 미리보기 / 자동 진행 / 전투 종료. `StagePlan`처럼 Buffer를 주입받는다 | 없음 |
| Job | **연출 대기 Job은 완성 가능**(`PresentedResultSequence ≥ LatestResultSequence`면 완료). 명령 처리·미리보기·자동 진행·준비·종료 Job은 **틀만**: 무엇을 읽고 어떤 서비스를 부르나 | 처리 서비스(R6), 전투 준비 데이터 출처(천명님 G7), 결과 기록(R5) |
| JobRunner | **바꿀 것 없음.** Global Runner를 그대로 쓴다 | 전투를 나갈 때 Runner를 비울지(D7) 민형님 확인 |
| 공용 연결 | `JobType`에 전투용 값 추가, `JobRegisterSystem`의 Combat 등록을 Buffer 주입 람다로 | 공용 파일이라 민형님 공유 문서 필요 |

- Core가 비어 있는 동안 Job 몸통은 **조용히 완료하지 않고 "Core 처리 서비스 미연결" 예외**를 던진다(Buffer 설계 D8).
- **확인 방법:** 부트스트랩 없이 EditMode 테스트로 한다. 기존 `ChapterGraphPreparationTests`가 실제 `JobRunnerSystem`으로 `StagePlan`을 돌리는 방식과 같다. 확인할 것:
  - 국면별 등록
  - 같은 명령이 두 번 등록돼도 한 번만 처리되는지
  - 연출 확인 전까지 Runner가 넘어가지 않는지
  - 나갈 때의 정리

  국면·결과 값은 가짜로 넣는다.
- **원칙 점검**
  - Flow는 진행의 주인 하나이며 규칙·표시·연출을 갖지 않는다(SRP). 실패한 진행 명령은 다시 묻지 않고 멈춘다(옛 재설계 교훈).
  - Flow는 상태를 따로 들지 않고 매 Tick Buffer를 읽는다. 처리 중 표시는 Runner의 `IsIdle`로 갈음한다(SSOT).
  - 새 진행은 JobType과 시나리오를 추가해 늘린다(OCP).
  - Job은 인터페이스만 받는다(DIP).
- 이전 Buffer 설계 §12(Flow 후보)와 겹친다. 사용자가 "참고하지 않고 새로" 하기로 했던 내용이므로, 새 설계에서 **다시 검토해 확정**한다.

### 10.3 새 세션의 첫 작업 후보

사용자에게 순서를 묻는다.

1. **전투 진행 골격 설계 문서**(가칭 "전투 진행 골격 설계", 새 파일). 다룰 내용:
   - Flow 분기 표
   - JobType·시나리오 목록
   - Job별 입력·출력·완료 조건
   - 빈칸과 Core 요청의 연결
   - 공용 파일 변경 목록
   - SSOT·SOLID 점검표
2. **Core 요청 문서**(새 파일): §10.1의 R7~R11과 R5 구체화
3. §6의 결정 6개(부트스트랩, 전투 씬, 범위 등)를 받은 뒤 전투 View(표시·입력 수신) 설계

1과 2는 서로 막지 않으므로 함께 진행할 수 있다. 3은 1의 Flow·Job 틀 위에 올라간다.