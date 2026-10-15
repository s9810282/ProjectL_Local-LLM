# 현재 Framework 기준 게임 구동 방식과 전투 구현 기능 정리

- 작성일: 2026-09-30
- 작성: View 작업(스테이지 내부 담당)
- 용도: **전투 View 설계의 입력.** 결정을 담지 않는다. 결정할 거리는 §5에 모은다.
- 기준: `feature/Yonggeun/Stage` 작업 트리(입력 작업 Phase 0~5, 천명님 `1f833e1` 병합 포함)
- 함께 볼 문서
  - [옛 Stage View → Buffer 대응표](2026-09-28-stage-view-buffer-mapping.md): 옛 전투 View 코드의 분류
  - Framework 구조: `Assets/ProjectL/Script/Systems/Documentation/framework-architecture.md`
  - [입력 사용 안내](../viewDocs/stage-input-guide.md)

## 0. 한눈에

| 층 | 있는 것 | 비어 있는 것 |
|---|---|---|
| **구동** | 조립(Factory), 수명 단계(InitData → PostBindService → Initialize → Tick), LifeCycle·로딩·Job 골격 | **부트스트랩**(런타임을 만들고 Tick하는 쪽)과 **첫 LifeCycle 진입**. 그래서 지금은 게임이 스스로 돌지 않는다 |
| **규칙(Core)** | 옛 `CoreCaculator`에 전투·Run 규칙이 거의 다 있다(Core 테스트 1080건 통과) | 새 구조와 **연결되지 않았다.** 천명님이 Buffer 기반으로 이전 중이다(Task 0~2 완료, 3~8 남음) |
| **데이터** | DB 3그룹(InGame·View·Render), Buffer 8종 | InGame 테이블 3개 에셋 누락, View 값 일부는 임시 |
| **표현(View)** | 입력(UnityBridge·Controller·슬롯), 전투 입력 Buffer, Render·UI·Utility 서비스 | 전투 View 모듈 전부, 씬의 전투 객체, 결과를 받아 연출하는 경로 |

한 줄로 말하면, **골격과 입구는 있지만 게임을 굴리는 손잡이(부트스트랩·Flow)와 규칙 연결(Core 처리 서비스)이 없다.** 전투 View는 입력과 명령 기록까지 준비됐고, 결과를 받는 쪽은 Core 쪽을 기다린다.

## 1. 게임이 돌아가는 방식 (Framework 기준)

### 1.1 조립 — 누가 무엇을 만드나

`FrameworkRuntimeFactory.Create()`(`Factory/Runtime/RunTimeSystemFactory.cs`)가 한 번에 조립한다.

```text
루트
├─ StageInputReceiverSlot            (입력 수신 슬롯, 루트 서비스)
├─ BufferSystemGroup                 Current·Map·Stage·Artifact·Character·CombatState·Currency·CombatView
├─ GlobalSystemGroup
│  ├─ Render   Effect·Mesh·Color·Light·Outline
│  ├─ Utility  Delay·EventSystem
│  ├─ Input    InputSystem + UnityInputBridge (Controller·Layer 목록을 루트에서 받음)
│  ├─ Loding   씬 로딩과 Node 실행
│  ├─ UI       UISystem (씬의 UIBase를 찾아 등록)
│  └─ Job      JobRegister(Plan → Job) + JobRunner
├─ InGameSystemGroup
│  ├─ Core     StageGenerator · StageServiceManager · ChapterProgressionCoordinator
│  └─ View     (비어 있음)
└─ LifeCycleSystemGroup  LifeCycleManager + Lobby(0)·Stage(1)·Artifact(2)·Combat(3)
```

DB는 런타임 밖에서 `DataBaseFactory.Create(카탈로그)`로 만들고, `InitData`로 넣는다.

### 1.2 수명 — 단계와 호출자

| 단계 | 하는 일 | 호출자 |
|---|---|---|
| `BindScene` | 씬 객체 찾기(UI는 `FindObjectsByType<UIBase>`) | **없음** |
| `InitData(DB)` | 테이블 조회 준비, 맵 등록, 입력 Configure(한 번) | **없음** |
| `PostBindService` | 서로의 서비스 받기(Buffer, Global, 슬롯 등) | **없음** |
| `Initialize` | 시작 준비(LifeCycle은 로딩 Node 등록, 입력은 구독) | **없음** |
| `Tick(dt)` | 로딩 진행, Job 실행, 활성 LifeCycle의 `OnTick` | **없음** |
| `Shutdown` | 역순 정리 | **없음** |

- **부트스트랩이 없다.** `FrameworkRuntimeFactory.Create`를 부르고 위 단계를 차례로 호출하며 매 프레임 `Tick`하는 MonoBehaviour가 저장소에 없다(메모리 기록: 09-22 재구성 뒤 호출자 없음).
- `FrameworkRuntime`은 단계 순서만 검사한다(`InitData`·`PostBindService` 뒤에야 `Initialize`). `BindScene`·`InitData`·`PostBindService` 사이의 순서는 강제하지 않는다.

### 1.3 화면 전환 — LifeCycle과 로딩

```text
LifeCycleManager.Change(key)          ← 부르는 곳: 없음
 → LifeCycleBase.Enter()
 → LodingSystem.Load(sceneIndex)
     BeforeLoad Node → 현재 씬 Exit Node(OnExit) → LoadSceneAsync
     … Tick마다 진행 확인 …
     완료 → 대상 씬 Enter Node(OnEnter) → AfterLoad Node
```

- LifeCycle 네 개의 SceneIndex는 임시값이다: Lobby 0, Stage 1(스테이지 **맵**), Artifact 2, Combat 3(스테이지 **내부**).
- Build Settings에는 `Stage.unity` **하나(인덱스 0)**뿐이다. 1~3 인덱스의 씬이 없다.
- **씬을 불러온 뒤 `BindScene`을 다시 부르지 않는다.** UISystem처럼 `BindScene`에서 씬 객체를 찾는 시스템은 새 씬의 객체를 알지 못한다.
- `CombatLifeCycle.OnEnter/OnExit`는 `TODO: CombatFlow 시작/종료`만 있다. `OnTick`도 비어 있다.

### 1.4 진행 명령 — LifeCycle · Plan · Job

```text
LifeCycle (진행 구간)     Lobby · Stage(맵) · Artifact · Combat(스테이지 내부)
   │  각 LifeCycle 폴더에 Plans/ 와 Jobs/ 가 있다
   ▼
(Flow)                    무엇을 언제 Register할지 정하는 쪽 ← 없음
   │  JobRegisterSystem.Register(JobRootType, JobType)   ← enum 두 개만 받는다
   ▼
Plan (LifeCycle당 static 클래스 하나, JobRootType으로 선택)
   │  JobType별 "작업 시나리오": 어떤 Job을 어떤 Priority로 묶을지, 서비스를 Job에 주입
   ▼
Job (struct, 한 걸음)     Execute(), IsCompleted
   ▼
JobRunner (Global 하나)   Priority 순 실행, 미완료 Job은 다음 Tick에 다시, IsIdle
```

| Plan | JobType 시나리오 | 상태 |
|---|---|---|
| `StagePlan` | `NewRun`: [Run 초기화(0), 챕터 진입(1)] / `Start`: [챕터 진입(0)] | **동작한다.** 람다로 `IBufferQuery`도 주입받는다 |
| `CombatPlan` | `Start`: [빈 Job] | 골격 |
| `LobbyPlan`, `ArtifactPlan` | `Start`: [빈 Job] | 골격 |

- **Plan은 "무엇을 할지"(시나리오), Job은 "한 걸음을 어떻게 할지"다.** 전투 진행은 `CombatPlan`의 JobType 시나리오 목록으로 표현하게 된다. 예: 전투 준비, 명령 처리, 자동 진행, 전투 종료.
- 명령 내용은 `Register`로 넘길 수 없다. 그래서 Job이 Execute할 때 Buffer에서 읽는다(`CombatViewBuffer`를 만든 이유).
- `Register`를 부르는 **Flow가 없다**(Framework 문서에도 "Flow 클래스 없음"). 원래 View 입력 → Flow → Register로 그려져 있다.
- `JobType`은 모든 Plan이 함께 쓰는 enum이다(`None`/`Start`/`NewRun`). 전투용 값을 더하면 공용 파일 변경이다.
- 용어 주의: 여기의 Plan은 **작업 구성 Plan**이다. Core의 `CombatEffectPlan`·`RailwayActivationPlan` 같은 **효과 변경 Plan**(계산 결과 값)과 다르다.

### 1.5 데이터가 흐르는 방향

```text
DB(InGame) ──InitData──► Core 시스템 ──WriteSink──► Buffer ──ReadSink──► View
DB(View)   ──InitData──► View 시스템                 ▲
View 입력 ─────────────► CombatViewBuffer ─ReadSink─┘(Core 측이 읽기만)
```

- View는 InGame DB를 읽지 않는다. 규칙 값은 Buffer로 받는다.
- 지금 실제로 움직이는 흐름은 **스테이지 맵 진행**뿐이다: `StagePlan` → `ChapterProgressionCoordinator` → Current·Map·Stage Buffer.

## 2. 전투 구현에 쓸 수 있는 기능

### 2.1 규칙 — 옛 Core `CoreCaculator` (Unity 참조 없음)

`StageSessionFactory.Create(…)`가 전투 하나의 `StageServices`를 만든다. 공개 진입점은 다음과 같다.

| 서비스 | 진입점 |
|---|---|
| `Deployment` | 메인 캐릭터 최초 배치(미리보기·실행), 유닛 배치(미리보기·실행), 손패로 회수(슬롯 지정·자동), 본부 배치(미리보기·실행) |
| `Flow` | `TryEnterCombat`(준비 → 전투) |
| `Combat` | 이동 범위·미리보기·실행, 공격 범위·미리보기·실행(대상 지정·셀 조준), 턴 종료, 타임라인 진행, 적 AI 턴 실행, 타임라인 조회 |
| `Draw` | 수동 드로우, 드로우 상태 |
| `Gold` · `ArtifactLifecycle` | 전투 중 재화, 아티팩트 발동 |
| `Query` | `StageSnapshot` 전체, 개체 하나 조회 |

- `StageSnapshot`이 담는 것: 맵 크기, 단계(배치/전투), 개체 목록(위치·체력·능력치·특성), 손패 슬롯, 현재 턴, 승패, 밤 진행, 메인 캐릭터, 드로우, 재화, 가시밭, 아티팩트 발동
- 결과 타입: 명령마다 성공/실패 이유와 세부 결과가 있다. 예: `AttackResult`(피격 목록), `MovementResult`, `EnemyAiTurnResult`, `CombatTimelineAdvanceResult`
- `Contracts/Events` 폴더는 비어 있다. 옛 연출 사건은 View 쪽 `StageSessionHost`가 결과에서 만들어 썼다.
- 규칙 범위: 특성(집중·이동 후 할인·관통·가시밭·넉백), 카테고리 상성, 효과 엔진, 기믹(레일웨이), 영혼·아티팩트, 밤/낮, 승패
- Run 규칙(`Application/Run`): 스테이지 진입·완료, 상점 구매, 회복 선택, 지갑, 덱

**상태:**
- 이 Core를 Systems 쪽에서 부르는 코드는 없다. 천명님 계획 §1에서도 확인됐다.
- 천명님이 "상태는 Buffer, Core는 명령마다 계산"으로 옮기는 중이다([캐릭터 Buffer 이전 계획](../plans/2026-09-29-character-buffer-effect-migration-plan.md)).
  - Task 0~2 완료: 캐릭터 보유 기록, `CharacterBuffer`, `CombatStateBuffer` 골격
  - Task 3~8 남음: 드로우·상점, 전투 준비, 영혼, 효과, 전투 상태 기록, View 연결

### 2.2 규칙 — Systems 쪽 Core 시스템 (새 구조, 연결됨)

| 시스템 | 하는 일 |
|---|---|
| `StageGeneratorSystem` | DB로 챕터 노드 그래프 생성 → `StageProgressBuffer` |
| `StageServiceManager` | 스테이지 키로 데이터 조회·검증(실행 처리는 비어 있음) |
| `ChapterProgressionCoordinator` | 챕터 진입, 스테이지 진입 준비·확정, 스테이지 결과 반영(클리어 기록) |

전투 자체를 처리하는 Systems 서비스(명령 처리, 결과 기록)는 **아직 없다**(요청 R6, 천명님 Task 4·7).

### 2.3 Buffer

| Buffer | 쓰는 쪽 | 전투에서의 쓰임 |
|---|---|---|
| `CurrentProgress` | Core | 현재 챕터·스테이지(스테이지 이름 포함) |
| `MapProgress` · `StageProgress` | Core | 맵·노드 클리어(전투 결과 반영 대상) |
| `Character` | Core | 보유 캐릭터, 영혼 수, 메인 지속 상태, **전투 중 캐릭터 상태**(위치·체력 등, 천명님 이전 중) |
| `CombatState` | Core | 전투 전체 상태(보드 크기, 손패 용량, 밤 길이, 드로우 순서) + **R1~R4 읽기 자리**(결과 순번, 처리 번호, 미리보기 응답 번호, 국면). 값을 올리는 쪽은 아직 없음 |
| `Artifact` | Core | 아티팩트 보드 스냅샷 |
| `Currency` | Core | 재화 |
| `CombatView` | **View** | 입력 명령, 미리보기 요청, 연출 확인 |

`BattleWriteSink`(PrepareBattle / Apply / EndBattle)가 전투 준비·종료의 쓰기 입구다.

### 2.4 DB

| 그룹 | 전투에 쓰이는 테이블 | 에셋 상태 |
|---|---|---|
| InGame | Stage(Root + CombatStageDetail: 필드, 적 구성, 밤 길이, 경제, 레일웨이), Field, Unit, Structure, EnemyLoadout, PlayerLoadout, UnitRoster, CategoryConfig, StageEconomy, ArtifactDefinition, RecoveryOption | 12개 중 **9개만 그룹에 있음**. CategoryConfig·StageEconomy·RecoveryOption 에셋 없음(DB 스키마 정렬 계획 T11) |
| View | StageFieldView(셀 프리팹·간격·고스트), UnitVisual(유닛 프리팹·이름), StageViewProp, StageMapView, StageNodeVisual, TraitVisual, RecoveryOptionVisual, RailwayVisual, ArtifactView, InputSettings | 10개 모두 등록. 새 유닛 4종 외형, 한글 이름, 레일웨이 프리팹은 **임시**(T12 나머지) |
| Render | Effect, Material, Light | `EffectType`은 `Empty`·`TestFire` 둘뿐 |

그래서 실제 카탈로그로 DB 전체를 만드는 경로는 아직 실패한다.

### 2.5 View가 쓸 Global 서비스

| 서비스 | 제공 | 전투 View에서의 쓰임 |
|---|---|---|
| **Input** (이번 작업) | 수신 슬롯, `ChangeLayer`/`ClearLayer` | 클릭·키 → 명령. [사용 안내](../viewDocs/stage-input-guide.md) |
| Render / Effect | `GetEffectHandle()` → `EffectScope`(풀) | 타격·발사 이펙트 |
| Render / Mesh | `CreateMeshScop()` | 범위 표시용 동적 메시 등 |
| Render / Color | `CreateColorScop(Renderer)` | 셀·유닛 색 덮어쓰기(하이라이트) |
| Render / Outline | `CreateOutlineScop(OutlineData)` | 선택·호버 외곽선 |
| Render / Light | Scope, 프리셋 핸들 | 낮/밤 조명 |
| Utility / Delay | `AddDelayEvent` | 시간 지연 |
| Utility / EventSystem | `SubscribeStage`, `PublishDomain`, `PublishPresentation` | 사건 전달. **구독 해제 수단이 없다**(서비스가 `void`를 돌려줌). 씬마다 쓰면 구독이 쌓인다. 옛 코드는 Stage 소유 `StageEventBus` 인스턴스로 피했다 |
| UI | `OpenUI`/`CloseUI`/`HideAllUI`, `UIBase`(ID: Setting·Loading·Stage·StageSelect·Architecture·StageResult) | 전투 HUD·결과 화면. 화면은 `LoadingUI`·`StageUI`만 구현 |
| Loding | 씬 로딩, 단계별 Node 등록 | 전투 씬 진입·종료 훅 |
| Job | Plan·Job 실행 | 전투 진행(Flow) |
| Audio | `AudioSystem` 코드는 있으나 **Global에 등록되지 않음** | 효과음(오디오 담당자 영역) |

### 2.6 씬 `Assets/Scenes/Stage.unity`

- 있는 것: 카메라, 조명, URP Volume, EventSystem + `InputSystemUIInputModule`, UI 캔버스(버튼 1, 이미지 3, TMP 2), FMOD 오디오
- 없는 것: 필드·유닛·손패 등 전투 View 객체. 옛 스크립트 2개는 Missing Script로 남아 있다.
- 옛 전투 View 코드(`develop-4week`의 `Assets/Scripts/ProjectL/Unity/`)는 이 브랜치에 없다. 무엇을 옮길지는 [대응표](2026-09-28-stage-view-buffer-mapping.md) §3의 분류를 따른다.
  - R: 상태 읽기, C: 명령, Q: 미리보기, F: 진행, E: 연출 사건, V: 외형 데이터, L: View 내부

## 3. 전투 한 번을 돌리려면 필요한 것 — 현재 대비

```text
① 런타임 구동(부트스트랩)                       없음
② Combat LifeCycle 진입 → 전투 씬 로딩            LifeCycle은 있음 / 호출자 없음 / 씬 인덱스 없음
③ 씬 객체 연결(BindScene)                       로딩 뒤 재호출 없음
④ 전투 준비(DB → BattlePreparation → Buffer)    쓰기 입구만 있음(PrepareBattle) / 준비 Job 없음
⑤ 전투 Flow(입력 대기 ↔ 자동 진행 ↔ 종료)        없음(CombatLifeCycle TODO)
⑥ 입력 → 명령                                    있음(입력 작업 + CombatViewBuffer)
⑦ 명령 → Core 처리 → 상태·결과 기록               없음(R6, 천명님 Task 4·7)
⑧ 결과 → 화면 갱신 → 연출 → 확인                   Buffer 자리만 있음(R1~R5) / View 없음
⑨ 전투 종료 → 스테이지 결과 반영 → 맵으로 복귀     ResolveStage는 있음 / 호출 Job 없음
```

View 담당 범위는 ⑥, ⑧(표시·연출), ③의 전투 쪽, 그리고 D6에 따라 ⑤다. ①·②는 Framework 쪽이고, ④·⑦·⑨는 Core 쪽과 맞물린다.

## 4. 사실 확인 메모

- `ISystemInitData<T>`는 `(T)service`로 강제 형변환한다. 다른 타입의 컨텍스트는 본문 전에 실패한다.
- 모듈은 구현한 수명 인터페이스로 분류된다. 그룹은 자기가 넘겨주는 수명만 하위에 전달한다. 예: 입력 그룹은 이번에 InitData·Initialize 전달을 추가했다.
- Global 등록 순서: Render → Utility → Input → Loding → UI → Job. 종료는 역순이다. 루트는 LifeCycle → InGame → Global → Buffer 순으로 종료한다.
- `LodingSystem.Load`는 같은 씬 인덱스면 로딩 없이 Exit → Enter만 실행한다.

## 5. 전투 View 설계 전에 정할 것

1. **부트스트랩을 이번에 넣을지.**
   - 넣으면 재생해서 확인할 수 있다(입력 재생 확인도 여기서 풀린다).
   - Framework 쪽 영역이라 민형님과 겹친다. "확인용 최소 구동기"로 한정할지 정한다.
2. **전투 씬 구성.**
   - `Stage.unity`를 전투 씬으로 쓸지, 새 씬을 만들지
   - LifeCycle SceneIndex를 Build Settings와 어떻게 맞출지
   - 씬 객체(카메라, 필드 루트, 캔버스)를 View 모듈에 연결하는 방법: `BindScene`을 로딩 뒤 다시 부르게 할지, `CombatLifeCycle.OnEnter`에서 찾을지
3. **이번 범위.**
   - (a) 입력 → 명령 기록까지. 표시는 최소한
   - (b) 여기에 Buffer 상태 표시(필드·유닛·손패)까지
   - (c) 연출까지

   Core 결과 경로(⑦·⑧)가 없어서 (b)·(c)는 Core가 쓴 값이 있어야 확인할 수 있다.
4. **Core가 준비되기 전까지 무엇으로 확인할지.**
   - 테스트용 가짜 상태를 Buffer에 직접 넣는다
   - 옛 Core `StageServices`를 임시로 직접 부른다. 천명님 이전 방향과 어긋나고 나중에 걷어내야 한다
   - 보류한다
5. **옛 View 코드 가져오기 범위.** 대응표 §3.9의 L(View 내부: 셀 풀, 좌표 변환, 고스트, 하이라이트 등)부터 가져올지
6. **연출 사건 경로.** Core 결과의 사건 목록(R5)을 기다릴지, 옛 방식처럼 View가 전후 상태를 비교해 만들지. 전역 EventSystem은 구독 해제가 없다는 점도 고려한다
