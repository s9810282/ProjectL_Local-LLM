# 옛 Stage 전투 View → 새 구조(Buffer) 대응표

> 옛 Stage 전투 씬 코드가 Core와 주고받던 것을 새 Framework/Systems 구조의 어느 통로로 옮겨야 하는지 정리한다.
> 설계 문서가 아니라 **설계의 입력**이다. 결정은 하지 않고, 결정해야 할 것을 §5에 모은다.

- 작성일: 2026-09-28
- 작성: View 작업
- 옛 코드 기준: `origin/develop-4week` `ae07af37`(2026-09-22). develop에서는 `a18d86d`(JMH 폴더 정리)에서 삭제됨
- 새 구조 기준: `feature/Yonggeun/DB_SO` `f860909`(develop `8a5e703` + DB-SO 재구성)
- 범위: 옛 `Assets/Scripts/ProjectL/Unity`의 전투 씬 부분(Runtime/Stage, View/Combat·Field·Animation·PlacementPreview·HandQueue·Stage·Effects, View/UI의 전투 UI, Setup/Stage, Adapter/Stage·Coordinate·Unit·Structure). 스테이지 맵·상점·회복·아티팩트·영속 HUD는 §6에 간단히만 적는다.

## 0. 이름 대응 (먼저 읽기)

| 옛 이름 | 새 구조 | 비고 |
|---|---|---|
| Stage 씬 (전투) | **`CombatLifeCycle`** (SceneIndex 3, 임시) | 새 구조의 `StageLifeCycle`은 **스테이지 맵**이다. 이름이 겹치므로 주의 |
| StageSelect 씬 (맵) | `StageLifeCycle` (SceneIndex 1, 임시) | |
| `StageSessionHost` (Core 세션 보유) | 없음. Core는 Systems의 Core 시스템이 되고 View는 Buffer로만 본다 | |
| 옛 Framework의 `ISceneViewModule`·`ISystemTick` | `InGameViewSystemFactory` → `InGameViewSystemGroup`의 모듈 | 그룹은 비어 있고 모든 단계를 자식에게 전달한다 |

Build Settings에는 현재 `Assets/Scenes/Stage.unity` 하나만 있다. SceneIndex 0~3은 LifeCycle 코드의 임시값이다.

## 1. 분류 기준

옛 코드가 Core와 주고받은 것은 아래 일곱 갈래로 나뉜다.

| 코드 | 분류 | 새 통로 | 지금 있나 |
|---|---|---|---|
| **R** | 상태 읽기 | Buffer ReadSink | 전투용 없음 (B2 필요) |
| **C** | 명령 (View의 의도) | View → Systems 코디네이터 → Core | 없음 |
| **Q** | 규칙 조회·미리보기 (가정 입력에 대한 계산) | **Buffer로 표현하기 어려움.** §5-3 | 없음 |
| **F** | 진행 주도 (턴 넘기기, 적 턴 실행) | Core/Flow/Job 쪽 | Job·Flow 골격만 있음 |
| **E** | 연출 사건 (무엇이 어떤 순서로 일어났나) | Global `EventSystem`(`PublishDomain`/`PublishPresentation`) 후보 | 서비스는 있음, 전투 사건 정의 없음 |
| **V** | 외형 데이터 | ViewGroup DB | 일부 있음 (DB-SO 재구성) |
| **S** | 입력 조립 (SO → Core 입력) | Core가 InGameGroup을 `InitData`로 직접 읽음 | InGameGroup 있음, Core 연결 없음 |

이 밖에 Core와 관계없는 **View 내부**(L)와 **조립·수명**(A)이 있다.

## 2. 흐름 비교

```text
[옛]
StageSceneInstaller ─► StageEntryDataBuilder(SO→Spec) ─► StageSessionBuilder ─► Core StageSession
                                                                                   ▲   │
View 입력 ─► StageSessionHost.TryMove/TryAttack/Preview… ──────────────────────────┘   │ 결과 객체 + StageSnapshot
CombatFlowController(View Tick) ─► AdvanceTimeline / RunActiveEnemyTurn ───────────────┤
                                                                                       ▼
                        SnapshotChanged 이벤트 ─► 필드·손패·UI 갱신
                        결과 → Domain 이벤트 발행 ─► 애니메이션·데미지 텍스트·로그

[새] (문서 기준 방향. 굵게 표시한 곳이 아직 없음)
InGameGroup DB ─InitData─► Core 시스템 ─WriteSink─► Buffer ─ReadSink─► View
View 의도 ─► **코디네이터** ─► Core 계산 ─► Buffer 기록 (+ **연출 사건 발행**)
턴 진행 ─► **Flow / Job** (LifeCycle.OnEnter에서 시작)
```

## 3. 대응표

### 3.1 R — Buffer로 옮길 상태 (옛 `StageSnapshot`)

현재 Buffer 중 쓸 수 있는 것은 `CurrencyBuffer.Amount` 하나뿐이다. `CharacterBuffer`·`ArtifactBuffer`의 Data는 필드가 없는 빈 클래스다. 나머지는 **전투 Buffer(B2)** 를 새로 정의해야 한다.

| 옛 필드 | 내용 | 옛 소비자 | 새 위치 후보 |
|---|---|---|---|
| `MapWidth`, `MapHeight` | 필드 크기 | FieldRoot, FieldGridMapper, HandFieldAnchor | B2 |
| `Entities[]` — Id, Key, Kind, Owner, Area, ReserveSlotIndex, CombatAnchor, CombatRotation, OccupiedCells | 유닛·구조물 위치와 소속 | FieldUnitPresenter, HandPresenter, CardHandPresenter, UnitHUD | B2 (핵심) |
| `Entities[].Health` (Current/Max/IsDead) | 체력 | UnitHUD, HealthView | B2 |
| `Entities[].UnitStats` — 이동·공격 범위, 공격력, 행동력, 우선순위·딜레이, TraitStatuses, Category | 유닛 정보 패널 | UnitInfoUI(.Abilities/.Buffs/_InfoPannel), CombatAttackTargeting | B2 (범위 결정 필요: 전부 넣으면 커진다) |
| `ReserveSlots[]` | 손패 슬롯 | HandPresenter, CardHandPresenter, StageCardRecall | B2 |
| `CurrentPhase`, `PreparationIndex` | 준비/전투 단계 | StagePlayerInput, CombatFlowController | B2 |
| `CombatTurn` — ActiveUnitId, TimelineRevision, CompletedUnitTurnCount | 현재 턴 | ActiveTurnSpotlight, TimelineUnitHighlight, CombatFlowController | B2 |
| `Night` — 길이, 현재, 남은 시간 | 밤 진행 | DayNightTransitionController, StageDayNightLight | B2 |
| `Outcome` | 승패, 종료 여부 | StageResultFlow, CombatFlowController | B2 |
| `MainCharacterId`, `MainCharacterPlacementLocked` | 메인 캐릭터 | InitialMainCharacterPlacementPresenter, StagePendingPlacementInput | B2 |
| `Draw` — 남은 수, 수동 드로우 가능 여부·실패 이유 | 드로우 | StageDrawStatusUI, UnitDrawButton, DeckPannel | B2 또는 CharacterBuffer 확장 |
| `Economy.Gold`, `ManualDrawGoldCost` | 재화 | CurrencyView, StageDrawStatusUI | Gold → `CurrencyBuffer.Amount`, 드로우 비용은 B2 |
| `ThornFields[]` | 가시밭 셀 | ThornFieldPresenter | B2 |
| `ArtifactActivations[]` | 아티팩트 발동 | (아티팩트 View) | ArtifactBuffer 또는 E |
| 타임라인 목록 (`GetTimeline(n)`의 결과) | 앞으로의 행동 순서 | Timeline, TimelineUnitHighlight | B2 (몇 칸까지 미리 쓸지 정해야 함) 또는 Q |

### 3.2 C — 명령 (View의 의도)

옛 코드는 모두 `StageSessionHost`를 직접 불렀다. 새 구조에서는 전부 **코디네이터(없음)** 로 가야 한다.

| 옛 명령 | 옛 호출자 | 비고 |
|---|---|---|
| `TryPlaceOnCombat(entity, cell)` | CardPlacementDrag, PlacementInteractionPresenter | 손패 → 필드 배치 |
| `TryReturnToHand(entity)` | PlacementInteractionPresenter, StageCardRecall | 필드 → 손패 |
| `TryPlaceInitialMainCharacter(cell, rot)` | InitialMainCharacterPlacementPresenter | 메인 캐릭터 최초 배치 |
| `TryEnterCombat()` | StagePlayerInput | 준비 → 전투 |
| `TryMove(unit, cell)` | CombatMoveTargeting | |
| `TryAttack(unit, target)` / `TryAttack(unit, aim)` | CombatAttackTargeting | 대상 지정 / 셀 조준(선인장 등) |
| `EndUnitTurn()` | CombatFlowController | 플레이어 턴 종료 |
| `TryDrawCharacter()` | UnitDrawButton, StagePlayerInput | 수동 드로우 |
| `RequestContinue` / `RequestNewRun` / `RetryStageCompletion` | StageResultPresenter | 결과 화면. Run 쪽 명령 |

명령 결과로 옛 View가 받던 것: 성공 여부와 실패 이유(`CommandRejectedEvent`로 UI 표시), 갱신된 Snapshot. 새 구조에서는 성공한 결과는 Buffer, 실패 이유는 E 통로가 된다.

### 3.3 Q — 규칙 조회·미리보기

"만약 여기로 옮기면?"처럼 **가정한 입력**에 대해 Core 규칙을 계산해 달라는 요청이다. 결과가 저장된 상태가 아니라서 Buffer에 그대로 넣기 어렵다. 옛 View는 드래그·호버마다 바로 호출했다.

| 옛 조회 | 옛 호출자 | 쓰임 |
|---|---|---|
| `PreviewCombatPlacement(entity, cell)` | CardPlacementDrag, PlacementInteractionPresenter (→ FieldPlacementMap이 전체 셀 계산) | 배치 가능 셀 색칠, 고스트 색 |
| `PreviewInitialMainCharacterPlacement(cell, rot)` | InitialMainCharacterPlacementPresenter | 메인 캐릭터 배치 가능 여부 |
| `GetMovementRange(unit)` | CombatMoveTargeting | 이동 가능 셀 |
| `PreviewMove(unit, cell)` | CombatMoveTargeting | 이동 비용·도착 셀 |
| `GetAttackRange(unit)` | CombatAttackTargeting | 공격 가능 셀 (행동력 부족 시 제한) |
| `PreviewAttack(unit, target/aim)` | CombatAttackTargeting | 영향 셀, 예상 피해, 관통 감쇠 |
| `GetTimeline(n)` | Timeline, CombatDebugOverlay | 앞으로 n칸 행동 순서 |
| `TryGetPlayerCombatUnitAt(cell)`, `TryGetEntitySnapshot(id)` | PlacementInteractionPresenter, StagePlayerInput | 클릭한 셀의 유닛 찾기 → **R에서 계산 가능**(Buffer의 엔티티 목록으로 대체) |

### 3.4 F — 진행 주도

| 옛 코드 | 하던 일 | 새 구조에서 |
|---|---|---|
| `CombatFlowController` (View Tick) | Snapshot을 보고 다음 행동을 정함: 플레이어 턴이면 대기, 적 턴이면 지연 후 `RunActiveEnemyTurn`, 턴 끝나면 `AdvanceTimeline` | Core/Flow가 맡아야 함. `CombatLifeCycle.OnEnter`의 "CombatFlow 시작"(TODO) |
| `TurnAuthority` | 지금 누가 진행 권한(바통)을 쥐었나: 플레이어 / 적 / 기믹 / 연출 | Flow가 판단. View는 "플레이어 입력 가능" 여부만 알면 됨 → R 또는 E |
| `CombatPresentationGate` | 연출이 끝날 때까지 진행을 붙잡음 | **새 구조에 대응 없음.** Flow가 View 연출 완료를 기다릴 방법이 필요(§5-4) |
| 적 턴 전 지연, 적 이동 후 대기 | 연출 템포 | 연출 쪽 값이지만 진행을 멈추므로 §5-4와 같이 결정 |

### 3.5 E — 연출 사건

옛 `StageSessionHost`가 명령 결과를 보고 Domain 사건을 발행했고(**Snapshot 먼저 발행 뒤 사건**), View 연출이 이를 구독했다. Buffer에는 최종 상태만 남으므로, 이 사건들은 별도 통로가 필요하다. 새 구조의 Global `EventSystem`에 `PublishDomain`/`PublishPresentation`이 이미 있다.

| 옛 사건 (출처 결과 필드) | 구독자 |
|---|---|
| `CombatEnteredEvent` / `CombatEntryRejectedEvent` | CombatHudPresenter |
| `UnitTurnStartedEvent` / `UnitTurnEndedEvent` | ActiveTurnSpotlight, CombatAnimationPresenter, CombatLog |
| `UnitMovedEvent` (MovementResult: From/To, 거리) | CombatAnimationPresenter(점프 이동), CombatLog |
| `UnitAttackedEvent` (AttackResult: 방향, AffectedCells, TargetCell) | CombatAnimationPresenter(박치기), BulletPresenter(발사체), CombatEffectPresenter, CombatLog |
| `UnitDamagedEvent` (AttackResult.Hits) | DamageTextPresenter, CombatAnimationPresenter(피격), CombatLog |
| `UnitDiedEvent` | CombatAnimationPresenter(사망 모션), StageDeadViewRelease |
| `StageGimmickActivatedEvent` (레일웨이) | RailwayPresentationScope, TrainTraversalView |
| `TraitActivatedEvent` / 카테고리 발동 | CombatEffectPresenter (넉백 싱크 포함) |
| `CombatFinishedEvent` | CombatHudPresenter, 결과 흐름 |
| `CommandRejectedEvent` (View 쪽 사건) | CombatHudPresenter |
| 드로우 결과 (CharacterDrawResult: 뽑은 키, 슬롯) | 손패 연출 |

주의: 옛 연출은 **사건 순서와 타격 시점**에 의존한다(`AttackImpactReactions`가 공격 사건의 피격·사망 반응을 타격 순간까지 미룸). 새 통로도 한 명령 안의 사건 순서를 보장해야 한다.

### 3.6 V — ViewGroup으로 가는 외형 데이터

| 옛 출처 | 새 위치 | 상태 |
|---|---|---|
| UnitVisualCatalog (유닛 프리팹) | ViewGroup `UnitVisualTable` | 있음 |
| FieldPrefab + StageSceneViewConfig (셀 프리팹, 고스트 색·크기) | ViewGroup `StageFieldViewTable` | 있음 |
| `StagePresentationData` (배경·소품) | ViewGroup `StageViewPropTable` | 테이블만 있음(에셋 0) |
| 셀 간격 X·Y, 배경 소품 위치·회전 offset (지난주 추가) | ViewGroup `StageFieldView` 또는 `StageViewProp` 확장 | **없음** — DB-SO 재구성 때 옛 에셋 기준이라 빠짐 |
| 유닛 초상화·특성 아이콘 (UnitInfoUI, 타임라인, 로그) | UnitVisual 확장 또는 별도 테이블 | 없음 |
| 무기·발사체 프리팹(Bullet, Ballista, Mortar), 사망·피격 모션 값 | 프리팹 직렬화 필드로 둘지 DB로 올릴지 결정 | 없음 |
| 데미지 텍스트, 낮/밤 조명 설정 | 씬·프리팹 설정 유지 가능 | – |

### 3.7 S — Core 쪽으로 넘어가는 입력 조립

View가 더 이상 하지 않는다. Core 시스템이 InGameGroup을 `InitData`로 읽는다.

| 옛 파일 | 하던 일 | 새 대응 |
|---|---|---|
| StageEntryDataBuilder, StageEntryPreparation, StageEntryResolver | SO → Core 입력 | Core가 `StageTable`/`FieldTable` 등 조회 |
| EnemySpawnPlanner | EnemyLoadoutSO → 초기 적 배치 | `EnemyLoadoutTable` |
| UnitSpecAdapter, StructureSpecAdapter, PlayerLoadoutAdapter | SO → UnitSpec/StructureSpec | `UnitTable`/`StructureTable` → Core. **UnitData에 Role·Category·방어력 등이 없음**(SO 검토 ①) |
| StageSessionBuilder | 세션 생성 | Core 시스템 |
| StageDatabaseSource, StageLoadoutDatabaseFactory, RunEconomyDatabaseFactory | DB 소스 보유 | `DataBaseFactory` + `InitData` |
| StageCompletionHandoff | 종료 결과 → Run | 코디네이터가 `StageProgress`/`MapProgress` Buffer에 클리어 기록 |

### 3.8 A — 조립·수명

| 옛 파일 | 새 대응 |
|---|---|
| StageSceneInstaller, StageSceneModuleFactory, StageRuntimeFactory, StageCombatInstaller | `InGameViewSystemFactory`에서 View 시스템 생성·등록 |
| StageSceneModule, StageViewModule, StageRuntime, StageCombatRuntime, StageSceneScope, StageRelease | `InGameViewSystemGroup`의 모듈 수명 + `CombatLifeCycle.OnEnter/OnExit` |
| StageTickSequence | 그룹의 Tick 전달 순서 |
| StageSnapshotBinding, StageUISnapshotFeed, StageUIBinding, StageUIContext | Buffer 읽기 + 갱신 시점(§5-2) |
| StageExecutionBoundary | 명령·Tick 실패 시 정지. 새 구조에서 누가 맡을지 미정 |
| StageInputBinding, StageInputBlocks, StagePlayerInput, StagePendingPlacementInput | Global `InputSystem`(Configure/Submit, Layer) |
| 씬 참조 연결 (SerializeField 앵커) | `ISystemBindScene.BindScene` |
| StageResultFlow, StageResultPresenter, StageResultUI | UISystem 화면 + Run 명령(C) |
| CombatEventRelay | EventSystem 구독(E) |
| StageActionTrace, StageInputTrace, CombatDebugOverlay, StageDrawDebugHUD | 디버그. 옮길지 결정 |

### 3.9 L — View 내부 (Core와 무관, 그대로 옮길 수 있음)

입력 데이터 출처만 R/E/V로 바꾸면 된다.

- **필드:** FieldCellPool, FieldCellHandle(ColorScope), FieldCellPicker, FieldCellSpacing(Policy), FieldFrame, FieldGridMapper(좌표 변환), FieldPlacementHighlight, CombatCellHighlight. FieldCamera* 는 다른 작업자 영역
- **배치:** PlacementGhost*(Factory/Pool/Presenter/Instance), CombatUnitDragPresenter, HandDropZone, HandFieldAnchor, HandRoot, HandUnitPicker/View, CardHandItem, UIPointerHitTest
- **연출:** UnitProceduralMotion, AnimationNotify, AttackImpactReactions, UnitHitPresenter, Bullet/BulletPool/BulletPresenter, Ballista/MortarWeapon, DamageTextView, ActiveTurnSpotlight, StageDayNightLight, StageSceneryView, TrainTraversalView
- **UI 표시부:** UnitHUD, UnitInfoUI 계열, Timeline 표시, CombatLog, DeckPannel, StageDrawStatusUI

## 4. 파일별 요약

| 폴더 | R | C | Q | F | E | V | S | A | L |
|---|---|---|---|---|---|---|---|---|---|
| Runtime/Stage | SessionHost | SessionHost, CombatCommands | SessionHost | CombatFlowController, TurnAuthority, PresentationGate | Events/*, CombatEventRelay | – | SessionBuilder, CompletionHandoff | 대부분 | CardRecall(제스처) |
| View/Combat | – | Move/AttackTargeting | Move/AttackTargeting | – | ThornFieldPresenter | – | – | – | Highlight, Spotlight, DragPresenter |
| View/Field | FieldUnitPresenter | – | FieldPlacementMap | – | – | FieldRoot(셀 프리팹) | – | FieldRuntime | 나머지 |
| View/Animation | – | – | – | – | CombatAnimationPresenter, BulletPresenter | 무기 프리팹 | – | – | 나머지 |
| View/PlacementPreview | – | Initial/PlacementPresenter | 같음 | – | – | – | – | – | Ghost* |
| View/HandQueue | HandPresenter | – | – | – | – | – | – | – | 나머지 |
| View/Stage | Night | – | – | – | Railway, DayNight | Scenery | – | – | TrainTraversal, Light |
| View/UI (전투) | CardHand, UnitHUD, UnitInfo, Timeline, DrawStatus | CardPlacementDrag, DrawButton, ResultUI | Timeline | – | DamageText, CombatLog | 아이콘 | – | – | View 부분 |
| Setup/Stage | – | – | – | – | – | – | 전부 | StageSceneInstaller | – |
| Adapter | – | – | – | – | StageResultEventPublisher | – | Unit/Structure/Loadout | – | FieldGridMapper |

## 5. 결정해야 할 것 (Core·JMH와 협의)

1. **전투 Buffer(B2) 범위.** §3.1의 필드 중 무엇을 넣나. 특히 `UnitStats` 전체, 타임라인 몇 칸, 드로우 상태를 CharacterBuffer에 둘지. Buffer 파일은 JMH 소유다.
2. **갱신 시점.** Buffer에는 변경 알림이 없다. 옛 코드는 `SnapshotChanged`로 갱신했다. 매 Tick 비교 / 명령 완료 알림 / EventSystem 사건 중 무엇으로 다시 그리나.
3. **미리보기(Q) 통로.** 드래그·호버마다 필요한 규칙 계산을 어떻게 받나.
   - (a) Core가 매 상태마다 미리 계산해 Buffer에 쓴다: 활성 유닛의 이동·공격 범위, 손패 유닛별 배치 가능 셀. 호버별 예상 피해는 어렵다.
   - (b) 읽기 전용 Core 조회 서비스를 둔다: "View는 Core에 데이터를 넣지 않는다" 원칙과 부딪히는지 확인 필요.
   - (c) 섞는다: 범위는 (a), 가정 입력 계산은 (b).
4. **연출과 진행의 동기화.** 옛 `CombatPresentationGate`는 View가 연출이 끝날 때까지 진행을 붙잡았다. 새 구조에서 Flow/Job이 진행을 주도하면, View 연출 완료를 어떻게 기다리나. (Job의 `IsCompleted`로 연출 완료를 기다리는 Job을 넣는 방법 등)
5. **연출 사건 통로.** EventSystem을 쓴다면 사건 정의(Domain/Presentation)를 누가 어디에 두고, 누가 발행하나(코디네이터). 한 명령 안의 순서와 "상태 먼저, 사건 나중" 규칙을 지킬 수 있나.
6. **명령 통로.** View의 의도를 받는 코디네이터의 위치와 인터페이스. 실패 이유를 View가 받는 방법.
7. **지난주 추가한 외형 값**(셀 간격, 소품 offset, 아이콘, 초상화)을 ViewGroup 어디에 둘지. 이건 View 작업 범위라 우리가 정하면 된다.
8. **부트스트랩.** Runtime을 생성·Tick하는 호출자가 없어서 새 구조로는 에디터 실행 확인이 안 된다(JMH 문서 DR-001).

## 6. 추가 발견: DB-SO 이전이 옛 develop 데이터 기준이다

DB-SO 재구성은 develop `d852362`의 옛 에셋 55개를 기준으로 했다. 그런데 실제로 플레이하던 `develop-4week`의 SO 데이터는 그보다 훨씬 새롭다(대응표 작업 중 발견, 2026-09-28).

| 비교 | 결과 |
|---|---|
| 에셋 수 | develop 55개 / develop-4week 109개 → **develop-4week에만 54개** (Ballista·Gravekeeper·CactusCannon·MainCharacter 유닛, CategoryCatalog, TraitDescriptions, StageEconomy, 상점·회복 스테이지, 무기·발사체·모션 설정, RailwayVisualConfig, NewStage/NewArtifact 테이블 등) |
| 공통 에셋 | 33개 내용이 다름 |
| 예: Akimbo | `_role`, `_category`, `_physicalDefense`, 캐논 범위 필드 추가, 공격력 1 → 6, 표시 이름 한글 |
| 예: Stage_Nomal_A | `_executionKind`, `_economy`, `_nightLength`, 배경 소품·offset 추가, **플레이어 본부 앵커 → 메인 캐릭터 앵커**, 적 본부 (3,5) → (4,7), 필드 변경 |
| 예: Chapter1_Enemy_1 | 적 구성에 유닛 2종 추가, 시드 변경 |

→ 새 InGameGroup/ViewGroup 스키마와 값이 develop-4week 기준으로 뒤처져 있다. SO 검토 ①(UnitData에 Role·Category·방어력 없음)도 같은 원인이다. 이 차이를 어떻게 반영할지(스키마 확장 + 값 재이전)는 별도로 정해야 한다.

## 7. 범위 밖 (간단히)

| 옛 폴더 | 새 대응 |
|---|---|
| Runtime/StageSelect, View/StageMap, Adapter/StageSelect | `StageLifeCycle` + StageProgress/CurrentProgress Buffer(있음) + ViewGroup StageMapView·StageNodeVisual(있음). 가장 먼저 옮길 수 있음 |
| Runtime/Shop·Recovery, View/Shop·Recovery | 대응 LifeCycle 없음(Lobby/Stage/Artifact/Combat 넷뿐) |
| Runtime/Artifact, View/Artifact | `ArtifactLifeCycle`, ArtifactBuffer(빈 껍데기) |
| Runtime/IngameHUD, View/IngameHUD | 영속 HUD. CurrencyBuffer + 체력 값 출처 필요 |
| Adapter/Input | Global InputSystem |
