# 전투 View 폴더 정리 Task 계획 (M)

- 작성일: 2026-10-19. 갱신: 2026-10-21. 상태: **2026-10-21 완료 — 코드 이동(커밋 `81354f33`, Unity 확인 문제 없음)·M-4 문서(§9.2).**
- 요청: "combat 쪽 스크립트가 하나도 정리가 안되어있어서 폴더들 만들어서 정리" → 분류(대화) → "네임스페이스를 폴더 경로에 맞춰 바꾸는 방향으로 이동 계획". 2026-10-21 다시 요청 "기준 및 역할에 따라서 분류해줄래" → 대응표를 지금 파일에 맞춤(§0.3).
- 분류 출처: [전투 View 구조 안내](https://claude.ai/artifact/CCPMThCg8ciN3z1VhhzAfz)(2026-10-18)의 파트·대단위. 그 뒤 생긴 카메라·가방 파일을 끼워 넣었다.
- 기준: 처음 HEAD `a0f9cd95`(194개). **2026-10-21 HEAD `fc392a72`(작업 트리 깨끗)에서 `C/` 아래 `.cs` 241개(루트 148, `Display/` 93)**, `.meta` 짝 빠짐 0.
- 경로 약칭: `C/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/`, `T/` = `Assets/Tests/EditMode/Framework/`, `F/` = `Assets/ProjectL/Script/Systems/Factory/Runtime/InGame/View/`.
- 네임스페이스 접두 `NS` = `ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat`.

**범위:** 파일 이동(`.meta` 함께, GUID 유지), `namespace` 줄, `using` 줄, MonoBehaviour `MovedFrom`, 완전 이름 참조 두 곳.
**범위 밖:** 동작 변경, 클래스·멤버 이름 변경, 파일 삭제(참조 없는 `CombatAbandonPanel` 포함), `F/`·시험 폴더 구조, 옛 설계·계획·인계 문서의 경로(이력이라 고치지 않음).

## 0. 착수 조건과 공통 규칙

### 0.1 착수 조건

1. **카메라 K-1 마무리·커밋.** 지금 `C/`에 커밋 전 변경이 있다(수정 8개, 추적 안 된 새 파일 5개 + `.meta`). 그중 `Display/FieldOutlineRenderer.cs`의 "데칼 상시표기로 변경 10-19" 5줄은 카메라 작업이 아니므로, 커밋할지 따로 둘지 사용자가 정한다.
2. **같은 파일을 고치는 작업이 진행 중이지 않을 것.** 카메라 K-2·K-3, 클립 AC-1~AC-4는 조각·실행·사건 Presenter 파일을 고친다. 순서는 Q-M2에서 정한다.
3. **다른 작업자 사전 공지.** 공지 메모(M-0.3)를 전달하고, 그분들이 Combat 파일·Combat을 쓰는 파일을 고친 브랜치를 언제 병합할지 맞춘다(Q-M3).
4. **작업 트리가 깨끗할 것**(커밋 직후). 되돌릴 일이 생기면 그 커밋으로 돌아가면 된다.

2026-10-21 확인: 1·4는 풀렸다(`fc392a72`, 작업 트리에는 에디터가 바꾼 `FMODStudioCache.asset`뿐). 2·3은 착수 때 다시 확인한다.

### 0.2 Phase마다 지키는 것

- **동작 변경 없음.** 바뀌는 줄은 `namespace`, `using`, `MovedFrom`, §4.3의 두 곳뿐이다.
- **MSBuild 오류·새 경고 0.** **(2026-10-21: Q-M5에 따름)** Unity csproj의 Compile 목록은 옛 경로를 가리키므로, `extra.targets`에서 옮긴 파일의 옛 경로를 `Compile Remove`하고 새 경로를 `Include`한다(빌드 확인 메모의 방법).
- **관리 실행 기존 시험 상태 변화 0.** **(2026-10-21: 하지 않음 — 2026-10-20 지시, Q-M5 A도 MSBuild만)** M-0 기준선과 메서드 이름 단위로 비교한다. 시험 이름·네임스페이스는 바뀌지 않으므로 이름으로 맞출 수 있다.
- **정적 검사(§5) 모두 통과.**
- 검토 에이전트 1개.
- Unity 확인은 Q-M4대로. 커밋은 사용자가 한다. 이동과 네임스페이스 변경은 같은 커밋에 넣어도 파일 내용 변화가 작아 git이 이름 변경으로 잡는다.
- 보고 끝에 다음 Phase를 자세히 적는다.

### 0.3 2026-10-21 갱신 (그 사이 바뀐 사실)

- **새 파일 18개**를 §3에 더했다(행 끝 "10-21 추가"). 위치 근거는 그 행의 괄호.
- **MonoBehaviour 16개**(M6): 새 `CombatRewardSlot`(종료 화면 칸, `CombatEndUI`가 씀)·`RailwayTrainBodyBinding`(열차 Body 연결점).
- **Combat 밖 새 사용처:** UI `CombatEndUI`(`CombatRewardSlot` → `Display/Hud/`), 튜토리얼 View 셋(호진님 — `TutorialViewFocusLocator`가 `FieldUnitView`·`CombatUnitInfo`를 써서 M-2에서 `using` 둘이 붙음, `TutorialViewSystem`·`TutorialViewUIDisplay`는 루트 형식만 써서 그대로). 시험 165개 파일이 Combat `using`을 가진다.
- **M5 다시 확인:** Combat 형식 266개(파일 241개), 밖 선언과 이름 겹침 0. 폴더 이름과 같은 형식은 시험의 private 중첩 클래스 `Display`·`Effects`·`Feedback`뿐이라 영향 없음(`using`은 형식만 들여오고 네임스페이스 이름은 들여오지 않음).
- **§4.3 위치:** `CombatViewSystem.cs:998` → `:1056`. 시험 `ViewDataTableTests.cs:184-185`는 그대로.
- **검증 방법 충돌:** §0.2는 MSBuild·관리 실행을 쓰지만, 2026-10-20 사용자 지시는 "전투 View 작업은 MSBuild를 돌리지 않는다"다. 정리 작업에 이 지시를 그대로 적용할지는 Q-M5.

## 1. 정한 것

| 번호 | 내용 |
|---|---|
| M1 | **1단 폴더 = 파트, 2단 폴더 = 대단위.** 루트 = 01 조정자·씬 연결·결과 수명, `Input` = 02, `Display` = 03, `Playback` = 04, `Presentation` = 05. |
| M2 | **네임스페이스 = `NS` + 폴더 경로**(`/` → `.`). 프로젝트 코딩 규칙 그대로다. |
| M3 | **루트 12개는 옮기지 않는다.** 폴더 밖에서 가장 많이 쓰는 `CombatViewSystem`·`ICombatViewService`·`CombatSceneRoot`의 네임스페이스가 그대로 남는다. 루트 형식은 바깥 네임스페이스라 모든 하위 네임스페이스에서 `using` 없이 보인다. 그래서 `OptionUI`·`InGameViewSystemGroup`은 바뀌지 않는다. |
| M4 | **폴더 이름은 다른 형식 이름과 겹치지 않게 한다.** 네임스페이스가 같은 이름의 형식을 가리기 때문이다(`Enum` 문제와 같음). 피한 이름: `Camera`(UnityEngine, `CombatPointerMapper`가 씀) → `Cameras`, `Path`(System.IO) → `MovePath`, `Common`(`ViewSystem.Common` 등 네임스페이스 넷) → `Support`, `Terrain`·`Screen`·`Motion`·`Environment`(Unity·System 형식) → `Map`·`Hud`·`Movement`. 같은 이름의 네임스페이스가 이미 있는 `Input`(`ViewSystem.Input`, 파일 1개)·`Contracts`(Core)·`Field`·`Map`·`Unit`·`Movement`(DB·Core)는 Combat 코드가 그 네임스페이스를 `using`하지 않고, 이름 일부만 붙인 참조(`Unit.`·`Path.`)도 모두 클래스 멤버라 그대로 쓴다. |
| M5 | **`using`을 더해도 모호해지지 않는다.** Combat 최상위 형식 217개(파일 194개, 한 파일에 여럿인 것 포함)의 이름이 Combat 밖 어떤 선언과도 겹치지 않는다(2026-10-19 확인, 2026-10-21 형식 266개·파일 241개로 다시 확인 — §0.3). 그래서 형식이 바깥 네임스페이스에서 `using`으로 바뀌어도 CS0104가 생기지 않는다. |
| M6 | **MonoBehaviour 16개에 `MovedFrom`을 붙인다**(2026-10-19 14개 + 10-21 `CombatRewardSlot`·`RailwayTrainBodyBinding`)(CLAUDE.md 규칙). 모두 `Display/`에 있고 M-2에서 옮긴다. 형식은 저장소의 기존 쓰임과 같게 `[UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "NS.Display")]`. 씬·프리팹은 스크립트 GUID로 찾으므로 `.meta`를 함께 옮기면 연결은 그대로다. 루트의 `CombatSceneRoot`는 옮기지 않는다. |
| M7 | **새 폴더의 `.meta`는 Unity가 만든 것을 커밋한다**(손으로 만들지 않는다). |

## 2. 폴더 구조

```text
C/                         NS                                01 조정자·씬 연결·결과 수명 (12)
├─ Input/                  NS.Input                          02 수신·분배·허용 판단·Gate (7)
│  ├─ Interaction/         NS.Input.Interaction              배치·공격·아이템 끌기·호버 (11)
│  ├─ MovePath/            NS.Input.MovePath                 이동 경로·복귀 (6)
│  ├─ UiEntry/             NS.Input.UiEntry                  덱·가방 UI가 부르는 입구 (6)
│  ├─ Assist/              NS.Input.Assist                   커서 스냅·환경 설명·거절 안내 (4)
│  ├─ ActionButtons/       NS.Input.ActionButtons            행동 버튼·포기·시작 확인 (5)
│  └─ Contracts/           NS.Input.Contracts                입력 → 표시 계약 (12)
├─ Display/                NS.Display                        03 표시 입구 (3)
│  ├─ Feedback/            NS.Display.Feedback               입력 중 표시·궤적 구성 (6)
│  ├─ Field/               NS.Display.Field                  격자·칸·강조·높이 표·궤적 그리기 (18)
│  ├─ Unit/                NS.Display.Unit                   유닛·유닛 HUD·표시 창구·클립 채널 (25)
│  ├─ Map/                 NS.Display.Map                    지형·오브젝트·레일·열차 칸·가시밭·프랍 (15)
│  └─ Hud/                 NS.Display.Hud                    화면 UI·종료 화면 칸 (8)
├─ Playback/               NS.Playback                       04 사건 Presenter·재생 조정·단계 전환 (4)
│  ├─ Ordering/            NS.Playback.Ordering              순차 재생·동시 묶음·경로 표 (8)
│  ├─ Handlers/            NS.Playback.Handlers              사건 처리기 (8)
│  ├─ Players/             NS.Playback.Players               시작 조건·실행 목록·재생기 (12)
│  └─ Support/             NS.Playback.Support               대기·소리·강조·보인 사실·연출 값 (9)
└─ Presentation/           (파일 없음)                        05
   ├─ Pieces/              NS.Presentation.Pieces            조각·조립·클립 재생 (23)
   ├─ Attack/              NS.Presentation.Attack            공격 재생·애니메이션·투사체·발사 기하·Gate (19)
   ├─ Movement/            NS.Presentation.Movement          걸음 레인·접촉·몸·높이 식 (9)
   ├─ Reaction/            NS.Presentation.Reaction          피격 반응·맞은 방향 (3)
   ├─ Railway/             NS.Presentation.Railway           기차·기차 카메라 (4)
   ├─ Cameras/             NS.Presentation.Cameras           카메라 창구 (3)
   └─ Effects/             NS.Presentation.Effects           이펙트 창구 (1)
```

합계(2026-10-21): 루트 12 · Input 51 · Display 75 · Playback 41 · Presentation 62 = 241. (10-19 처음 판: 12 · 38 · 61 · 40 · 43 = 194)

## 3. 대응표 (이동의 원본)

`*` = 지금 `C/Display/`에 있는 파일, 표시가 없으면 지금 `C/` 루트에 있는 파일이다. 파일 하나에 형식이 여럿이면 모두 같은 네임스페이스로 간다.

| 새 폴더 | 파일 |
|---|---|
| `C/` (그대로) | CombatViewSystem, CombatSceneRoot, ICombatViewService, CombatDisplayTables, CombatDisplayServices, CombatDisplayValues, CombatResultPlayback, CombatPlaybackToken, CombatPresentationOutcome, ICombatPresentation, CombatViewRefresh, CombatRefreshSignal |
| `Input/` | CombatInputReceiver, CombatInputRouter, CombatInputPolicy, CombatInputFacts, CombatCommandGate, PendingInputCommand, CombatCharacterQueries |
| `Input/Interaction/` | CombatInteraction, PlacementDragInteraction, AttackTargetingInteraction, CombatAttackDragGesture, CombatHoverTracker, CombatHoverState, CombatItemDragInteraction, CombatUnitRangeHover, CombatPreviewSlotRule(2026-10-19 호버 공격 범위 U-2에서 추가), CombatHazardLookup·CombatRangeTone(2026-10-20 호버 위험 범위에서 추가) |
| `Input/MovePath/` | CombatPathInput, CombatMovementInteraction, CombatMovementRangeRequest, CombatReturnRequest, CombatMovementRangeTracker, CombatReturnEstimateTracker(2026-10-19 경로 입력 분리에서 추가) |
| `Input/UiEntry/` | CombatDeckPlacementInput, ICombatDeckPlacementInput, CombatDeckHoverInput, ICombatDeckHoverInput, CombatItemInput, ICombatItemInput |
| `Input/Assist/` | CombatCursorSnap, CombatEnvironmentHover, CombatEnvironmentText, CombatActionNotice(10-21 추가 — 같은 파일의 `ICombatActionRejectionSink` 포함. 입력이 넘긴 Core 거절 사실을 안내 문구로 바꿈, 현강님) |
| `Input/ActionButtons/` | CombatActionButtons, ICombatActionDisplay, CombatAbandonController, ICombatAbandonDisplay, CombatDeploymentConfirmation(10-21 추가 — 미배치 개체가 있을 때의 시작 확인, 포기 확인 `CombatAbandonController`와 같은 성격, 현강님) |
| `Input/Contracts/` | ICombatPointerPicker, ICombatPointerMapping, ICombatPointerOcclusion, ICombatCursorDevice, ICombatInteractionFeedback, ICombatMovementFeedback, ICombatItemDragFeedback, ICombatUnitRangeFeedback(2026-10-19 호버 공격 범위 적·아군 색에서 추가), ICombatMovementRangeFeedback, ICombatReturnFeedback(2026-10-19 경로 입력 분리에서 추가), CombatHoverKind(10-21 추가 — `ICombatInteractionFeedback.SetHoverCell`의 인자), ICombatAttackTrajectoryFeedback(10-21 추가 — 공격 조준 → 궤적 표시 의도) |
| `Display/` | CombatSceneDisplay, ICombatBattleDisplay, ICombatPackDisplay |
| `Display/Feedback/` | CombatPlacementFeedback, CombatAttackFeedback, CombatMovementPlanningPresenter, CombatPointerMapper, PlacementGhost*, CombatAttackTrajectory*(10-21 추가 — 궤적 표시 의도를 경로로 바꿈, `CombatAttackFeedback`이 부름) |
| `Display/Field/` | FieldView*, FieldGridMapper*, FieldGridDecal*, FieldCellPool*, FieldCellHandle*, FieldCellFill*, FieldCellSpacing*, FieldCellColorRenderer*, FieldOutlineRenderer*, FieldHighlightLayers*, FieldHighlightPalette*, FieldHighlightState*, IFieldHighlightRenderer*, IFieldHighlightStateReader*, FieldSurfaceHeights*(10-21 추가 — 칸 높이 표, `FieldGridMapper`가 가짐), FieldTrajectoryLine*·FieldTrajectorySegments*·ICombatTrajectoryCanvas*(10-21 추가 — 궤적 그리기, `FieldView`가 캔버스, `FieldCellFill` 선례) |
| `Display/Unit/` | FieldUnitPresenter*, FieldUnitView*, EntityViewSet*, UnitModelAppearance*, CombatUnitPresentationWindow*, CombatUnitHudFeedback*, CombatObjectiveDisplay*, UnitHUDLayer*, IUnitHUDAnchor*, BallistaWeaponView*, CatusCannonWeaponView*, ICombatUnitTargets*, ICombatUnitPresentationTarget*, ICombatUnitHudTarget*, ICombatUnitAppearance*, ICombatUnitFlash*, ICombatUnitScale*, ICombatUnitPose*, ICombatAttackUnitPose*, ICombatUnitPlacement*, ICombatUnitDeathHold*, UnitPresentationLeaseSlot*·ICombatUnitClip*·UnitClipChannel*(클립 AC-2, 2026-10-19 추가), ICombatUnitPlacementReader*(10-21 추가 — `FieldUnitPresenter`가 구현하는 위치 읽기) |
| `Display/Map/` | FieldTerrainPresenter*, TerrainViewSet*, FieldObjectPresenter*, ObjectViewSet*, FieldRailwayPresenter*, RailwayTrackLayout*, RailwayTrainVisualBinding*, FieldThornPresenter*, StageViewPropPresenter*, CombatEnvironmentEffectPresenter*, EnvironmentVisualKinds*, CombatExplosionRules*, CombatEnvironmentFacts*(2026-10-20 호버 위험 범위에서 추가), RailwayTrainBodyBinding*·RailwayTrainCars*(10-21 추가 — 열차 칸, `RailwayTrainVisualBinding` 옆) |
| `Display/Hud/` | CombatHud*, CombatActionPanel*, CombatAbandonPanel*, CombatUnitInfo*, HandPresenter*, HandCardHoverView*, CombatRewardSlot*·CombatRewardExperienceAnimation*(10-21 추가 — 종료 화면 `CombatEndUI`의 칸·경험치 게이지, 현강님) |
| `Playback/` | CombatSceneEventPresenter, CombatSceneEventPlayback, CombatPhaseTransitionPresenter, CombatPhaseTransitionSettings |
| `Playback/Ordering/` | OrderedCombatPresentation, ICombatEventPlayback, CombatEventRoutes, CombatConcurrentGroups, CombatEventSubjects, CombatActionBoundary, CombatMovementPlayback, ICombatMovementPlaybackPresenter |
| `Playback/Handlers/` | CombatEventHandler, CombatEventHandlerTable, CombatAttackEventHandler, CombatCharacterEventHandler, CombatFactEventHandler, CombatRoarEventHandler, CombatSummonEventHandler, CombatTurnEventHandler |
| `Playback/Players/` | ICombatPresentationPlayer, CombatPresentationPlayers, CombatStartQuery, CombatPlaybackFrame, CombatSubjectBlocking, ICombatWaitSlot, ICombatSourceLifetime, ICombatSourceOverlap, CombatAttackPlayer, CombatStepPlayer, CombatReactionPlayer, CombatPieceRunPlayer |
| `Playback/Support/` | CombatEventWait, CombatEventAudio, CombatEventSounds, CombatPlaybackHighlight, CombatShownFacts, CombatMovementPresentation, CombatEnemyPresentationPacing, CombatAbilityMotionSettings, CombatCameraMotionSettings(10-21 추가 — `CombatAbilityMotionSettings`와 같은 성격의 연출 값, M-4.3이 미리 정한 자리) |
| `Presentation/Pieces/` | ICombatPresentationPiece, CombatComposition, CombatPieceStart, CombatPieceContext, CombatPieceRun, CombatPieceRunLeases(대여 분리 L, 2026-10-19 추가), CombatReactionPiece, CombatFlashPiece, CombatScalePiece, CombatRisePiece, CombatFacingAttackPiece, CombatHighlightClearPiece, CombatEventSoundPiece, CombatCameraShakePiece, CombatCameraZoomPiece, CombatClipEventCursor(클립 AC-1, 2026-10-19 추가 — AC-2 이후의 클립 재생 단위·조각도 이 줄), CombatClipPlayback(AC-2), CombatClipPiece(AC-3), CombatFaceTargetPiece·CombatReleasePiece·CombatClipAttack(AC-4, 클립 공격 조립), CombatFirePiece(AC-5b), CombatEffectPiece(클립 이펙트 EF-2, 2026-10-19 추가) |
| `Presentation/Attack/` | CombatAttackPlayback, CombatProjectileFlights·ICombatShot·CombatHitMoment(클립 AC-5b, 2026-10-19 추가), CombatAttackPresenter, ICombatAttackAnimation, CombatHitGate, CombatHitFollowUps, CombatAttackMotion*, CombatAttackTarget*, CombatAttackFacing*(클립 AC-4, 2026-10-19 추가), CombatMeleeAttackAnimation*, CombatLinearAttackAnimation*, CombatMortarAnimation*, CombatBallistaAnimation*, CombatProjectilePool*, CombatProjectileLease*, CombatProjectileContact*(투사체 접촉 이펙트 IE-b, 2026-10-19 추가), CombatShotPath*(10-21 추가 — 실제 발사와 궤적이 같이 쓰는 발사 기하, `CombatAttackTarget` 옆) |
| `Presentation/Movement/` | CombatUnitMotionPlayback, CombatUnitMotionSet, CombatStepContacts, CombatStepContactGate, CombatBody*, CombatBodyPlacement*, CombatBodyShapeCache*, CombatContactGeometry*, CombatStepHeight*(10-21 추가 — 걸음·넉백 보간의 높이 식) |
| `Presentation/Reaction/` | CombatUnitReactionPresenter*, CombatUnitReactionSet*, CombatHitDirection*(피격 이펙트 HE-b, 2026-10-20 추가 — 반응 밀림과 피격 이펙트 회전이 같이 쓰는 맞은 방향, 폴더 정리 결정 때 확인) |
| `Presentation/Railway/` | CombatRailwayContactPlayback, RailwayContactSchedule*, RailwayTravelPath*, CombatTrainCamera(10-21 추가 — 기차 전용 재생이 부르는 카메라 연출. 카메라 창구의 사용자라 `Cameras/`가 아니라 기차 쪽) |
| `Presentation/Cameras/` | CombatCameraWindow*, CombatMapCameras*, ICombatCameraZoom* |
| `Presentation/Effects/` | CombatEffectWindow*(클립 이펙트 EF-2, 2026-10-19 추가 — 카메라 창구와 같은 성격의 연출 출력 창구라 새 폴더로 제안, 폴더 정리 결정 때 함께 확인) |

**대응표를 바꿀 때:** 착수 전에 카메라·클립 작업이 새 파일을 만들면, 이 표에 행을 더한 뒤 이동한다(§6 M-4.2의 배치 규칙을 따름).

## 4. 이동 방법

### 4.1 도구

스크래치패드에 이동 도구를 만든다(이 PC의 `python`은 Store 스텁이라 bash + perl). 입력은 §3 표를 옮긴 대응 파일 하나이고, Phase마다 옮길 폴더 묶음을 인자로 받는다.

1. **이동 전 기록.** 옮길 파일마다 `.meta`의 `guid`를 적어 둔다.
2. **이동.** `.cs`와 `.cs.meta`를 새 폴더로 `mv`한다. `git mv`는 인덱스를 바꾸므로 쓰지 않는다(커밋 준비는 사용자 몫).
3. **`namespace` 줄.** 파일마다 `namespace` 줄이 정확히 하나인지 확인한 뒤 새 네임스페이스로 바꾼다. 하나가 아니면 멈춘다.
4. **`using` 정리.** 대상은 저장소의 모든 `.cs` 중 Combat 네임스페이스 안에 있거나, Combat 계열 `using`이 있거나, 옮긴 형식의 이름이 나오는 파일이다.
   - 파일에 나오는 옮긴 형식 이름(단어 경계, 주석의 `cref` 포함)으로 필요한 네임스페이스를 모은다.
   - 파일 자신의 네임스페이스와 그 바깥 네임스페이스(예: `NS.Display.Unit` 파일에게 `NS.Display`·`NS`)는 빼고, 없는 것만 `using` 블록 끝에 더한다.
   - Combat 계열 `using`(`NS`·`NS.*`) 중 더 이상 필요 없는 것은 지운다. 다른 `using`은 건드리지 않는다.
   - 줄 끝·BOM은 파일마다 지금 상태를 그대로 둔다(Combat 파일은 git에는 모두 LF·BOM 없음, 작업 트리는 `core.autocrlf=true`라 대부분 CRLF — 2026-10-21 확인. 시험 파일은 파일별로 확인).
5. **정적 검사(§5).**

### 4.2 Phase마다 영향 범위 (2026-10-19 기준, 상한)

형식 이름이 주석에만 나와도 세었으므로 실제 수정은 이보다 적다. **2026-10-21 파일 241개 기준으로는 M-0.1에서 다시 센다**(밖 파일에 UI `CombatEndUI`·튜토리얼 `TutorialViewFocusLocator`가 더해짐, §0.3).

| 옮기는 묶음 | 형식 수 | 그 형식을 쓰는 Combat 안 다른 파일 | 밖 파일 |
|---|---|---|---|
| Presentation | 52 | 23 | 32: 시험 31, `F/CombatSceneDisplayFactory` |
| Playback | 47 | 17 | 47: 시험 45, `F/CombatSceneDisplayFactory`·`F/InGameViewSystemFactory` |
| Display | 65 | 48 | 66: 시험 62, `F/CombatSceneDisplayFactory`·`F/CombatSceneInputFactory`·`F/CombatUnitHUDFactory`, UI `PackUI` |
| Input | 40 | 15 | 39: 시험 35, `F/CombatSceneInputFactory`, UI `DeckCardList`·`BattleChangerUI`·`PackUI` |

### 4.3 손으로 고칠 곳

| 위치 | 지금 | 고침 | Phase |
|---|---|---|---|
| `C/CombatViewSystem.cs:1056`(10-19에는 998) | `_display is Display.ICombatUnitInspection inspection` (부분 이름) | 형식이 `Display/Hud/CombatUnitInfo.cs`로 가므로 `using NS.Display.Hud;` + 이름만 | M-2 |
| `T/ViewDataTableTests.cs:184-185` | `AddComponent<NS.Display.RailwayTrainVisualBinding>()` (완전 이름) | `NS.Display.Map.RailwayTrainVisualBinding` | M-2 |

그 밖에 확인한 것: 문자열 안 네임스페이스 0곳, `using` 별칭·`using static` 0곳, `[SerializeReference]` 0곳, 씬·프리팹·에셋 안 Combat 네임스페이스 문자열 0곳.

## 5. 정적 검사 (Phase 끝마다)

| 검사 | 기준 |
|---|---|
| 네임스페이스 = 폴더 | `C/` 아래 모든 `.cs`의 `namespace` 줄이 `NS` + 폴더 경로와 같다. 아직 옮기지 않은 묶음은 옛 값(`NS`·`NS.Display`)이다 |
| GUID 유지 | 옮긴 파일의 `.meta` `guid`가 이동 전 기록과 같다. `.meta` 없는 `.cs`, `.cs` 없는 `.meta`가 0개 |
| 대응표 일치 | 옮긴 묶음의 파일이 §3 표의 폴더에만 있고, 옛 위치에 남은 것이 0개 |
| `using` 유효 | 저장소 전체에서 Combat 계열 `using`이 있는 네임스페이스만 가리킨다 |
| 형식 이름 겹침 | M5 검사를 다시 돌려 0건(병합으로 새 선언이 생겼을 수 있음) |
| 범위 | `git diff`(이름 변경 감지 켬)에서 바뀐 줄이 `namespace`·`using`·`MovedFrom`·§4.3뿐이다 |

## 6. Phase

### M-0 기준선·공지 (코드 변경 없음)

| Task | 내용 |
|---|---|
| M-0.1 | §0.1 착수 조건 확인. HEAD 기록. §5 형식 이름 겹침 검사와 §4.3 위치를 그 HEAD에서 다시 확인한다. 카메라·클립 작업이 만든 새 파일이 있으면 §3에 행을 더한다 |
| M-0.2 | MSBuild 기준선(오류·경고 목록) — Q-M5 A일 때만. 관리 실행 기준선은 2026-10-21부터 잡지 않는다 |
| M-0.3 | **사전 공지 메모** `docs/superpowers/handoffs/2026-10-19-combat-folder-restructure-share.md`: 언제·무엇을 옮기는지, §2 옛 → 새 네임스페이스 표, 병합 뒤 할 일(`using` 추가), 작업자별 영향 파일(아래). 사용자가 전달한다 |

작업자별 영향 파일(git 작성자 기준):

- JMH(민형님): `CombatPhaseTransitionPresenter`·`CombatPhaseTransitionSettings`(→ `Playback/`), `F/`의 Factory 4개.
- Twinsgm(단독 작성 10개): 덱 입력 4개(→ `Input/UiEntry/`), `CombatEnvironmentHover`·`CombatEnvironmentText`(→ `Input/Assist/`), `CombatUnitInfo`(→ `Display/Hud/`), `EnvironmentVisualKinds`(→ `Display/Map/`), `IUnitHUDAnchor`·`UnitHUDLayer`(→ `Display/Unit/`). UI `DeckCardList`·`BattleChangerUI`·`PackUI`의 `using`. 2026-10-21 추가: `CombatActionNotice`(→ `Input/Assist/`), `CombatDeploymentConfirmation`(→ `Input/ActionButtons/`), `CombatRewardSlot`·`CombatRewardExperienceAnimation`(→ `Display/Hud/`, `CombatRewardSlot`은 MonoBehaviour), UI `CombatEndUI`의 `using`.
- Leehojin: `CombatAttackDragGesture`(→ `Input/Interaction/`). 2026-10-21 추가(CCRC_Hojin): 튜토리얼 `TutorialViewFocusLocator`의 `using`(M-2에서 `NS.Display.Unit`·`NS.Display.Hud`).
- 여러 작업자 시험 파일(시험 폴더는 그대로, `using`만).

### M-1 Presentation·Playback (103개, MonoBehaviour 없음 — 10-19에는 83개)

| Task | 내용 |
|---|---|
| M-1.1 | 이동 도구 작성(§4.1). 대응 파일은 §3 표에서 만든다 |
| M-1.2 | `Presentation/` 62개 이동. 그중 25개(`*`)는 `NS.Display`에서 나오므로 `Display` 형식을 쓰면 `using NS.Display;`가 붙는다(M-2에서 다시 정리) |
| M-1.3 | `Playback/` 41개 이동 |
| M-1.4 | `using` 정리(§4.2 앞 두 행) |
| M-1.5 | 검증: §0.2·§5. 검토 에이전트 1개 |

Unity 확인: 컴파일(Editor.log), EditMode 전체, Combat 진입 뒤 공격·포효·소환·피격·사망·기차 재생.

### M-2 Display (75개, MonoBehaviour 16개 — 10-19에는 61개·14개)

| Task | 내용 |
|---|---|
| M-2.1 | `Display/` 하위 폴더로 75개 이동(루트에서 7개, `Display/`에서 68개) |
| M-2.2 | MonoBehaviour 16개에 `MovedFrom`(M6): BallistaWeaponView, CatusCannonWeaponView, CombatAbandonPanel, CombatActionPanel, CombatHud, CombatRewardSlot, CombatUnitInfo, FieldUnitPresenter, FieldUnitView, FieldView, HandCardHoverView, HandPresenter, PlacementGhost, RailwayTrainBodyBinding, RailwayTrainVisualBinding, UnitHUDLayer |
| M-2.3 | §4.3 두 곳 |
| M-2.4 | `using` 정리(§4.2 Display 행). M-1에서 붙인 `using NS.Display;` 중 필요 없어진 것도 지운다 |
| M-2.5 | 검증: §0.2·§5. 검토 에이전트 1개 |

Unity 확인: 컴파일, EditMode 전체, **Missing Script 없음** — `Combat.unity`, `HUD.prefab`, 유닛 프리팹(`FieldUnitView`·무기 View), 기차 프리팹(`RailwayTrainVisualBinding`·Head·Body `RailwayTrainBodyBinding`), 배치 고스트, 종료 화면(`CombatRewardSlot`). 콘솔에 "The referenced script is missing"가 없어야 한다. Combat 진입과 손패·HUD·행동 버튼 표시.

### M-3 Input (51개 — 10-19에는 38개)

| Task | 내용 |
|---|---|
| M-3.1 | `Input/` 하위 폴더로 51개 이동(모두 루트에서) |
| M-3.2 | `using` 정리(§4.2 Input 행, UI 세 파일 포함) |
| M-3.3 | 검증: §0.2·§5. 검토 에이전트 1개 |

Unity 확인: 컴파일, EditMode 전체, 배치 드래그·덱 카드 배치·공격 조준·경로 지정·복귀·가방 아이템 장착·HUD 우클릭 해제·포기(Option UI)·밤 진입 배치 확인·비용 부족 안내.

### M-4 문서·배치 규칙

| Task | 내용 |
|---|---|
| M-4.1 | [구조 안내 아티팩트](https://claude.ai/artifact/CCPMThCg8ciN3z1VhhzAfz) 갱신: 폴더·네임스페이스 절, 파일 색인에 폴더 열, "`Combat/`는 평면이다" 문장 교체 |
| M-4.2 | **배치 안내** 새 `docs/superpowers/viewDocs/combat-view-folder-guide.md`: 폴더마다 무엇을 두는지(§2 설명), 새 파일을 어디에 둘지 판단 순서, 폴더 이름에 쓰지 않는 이름(M4). 새 파일 위치의 원본은 이 안내 하나다 |
| M-4.3 | (2026-10-21: 카메라 K-2·K-3·클립 AC-1~AC-6은 끝나 고칠 착수 전 계획이 없다. 착수 때 진행 중인 계획이 있으면 그 계획의 `C/` 경로만 고친다.) |
| M-4.4 | 완료 공지: M-0.3 메모에 완료 HEAD와 최종 표를 더한다 |
| M-4.5 | CLAUDE.md 현재 상태 갱신 |

## 7. 위험과 대응

| 위험 | 대응 |
|---|---|
| 다른 브랜치가 옛 네임스페이스로 새 코드를 만든 채 병합 | 컴파일 오류로 바로 드러나고 `using` 추가로 고친다. 사전 공지(M-0.3)에 표를 둔다 |
| 다른 브랜치가 옮긴 파일의 내용을 고친 채 병합 | 바뀐 줄이 적어 git 이름 변경 감지로 내용이 따라온다. 병합 뒤 §5 "네임스페이스 = 폴더" 검사로 확인한다 |
| 씬·프리팹 Missing Script | `.meta`를 함께 옮겨 GUID를 유지한다(§5 GUID 검사). M-2 Unity 확인 목록 |
| Unity csproj가 옛 경로 | 에디터가 포커스를 받으면 다시 만든다. 그 전의 IDE 오류는 무시해도 된다. MSBuild는 `extra.targets`로 맞춘다 |
| 이동 중 다른 세션이 같은 파일 편집 | §0.1 조건 2. Phase 진행 중에는 Combat 파일을 고치는 다른 작업을 열지 않는다 |

## 8. 정할 것

**결정(2026-10-21, 사용자):** Q-M1 **B**(한 번에 241개), Q-M2 A(지금 착수), Q-M3 A(다른 작업자 파일도 옮기고 공지), Q-M4 A(Phase마다 — Q-M1 B라 코드 Phase는 하나), Q-M5 A(이번만 MSBuild).

| 번호 | 질문 | 선택지 |
|---|---|---|
| Q-M1 | Phase 단위 | **A(권장)** §6처럼 코드 세 Phase(M-1~M-3). Phase마다 컴파일되는 중간 상태라 문제의 범위가 좁고, 커밋도 나뉘어 이름 변경을 따라가기 쉽다. / B 한 번에 241개 |
| Q-M2 | 다른 작업과의 순서 | (10-19 문항: 카메라·클립 먼저냐 정리 먼저냐 — 두 작업이 끝나 낡음.) 2026-10-21: **A(권장)** 지금 착수(작업 트리 깨끗, 다음 작업 고지대 HG-4b 등의 새 파일이 처음부터 제 폴더에 생김). 그동안 Combat 파일을 고치는 다른 작업은 열지 않는다. / B 다른 작업(HG-4a 재생 확인 결과 반영, HG-4b) 뒤 |
| Q-M3 | 다른 작업자 파일 | **A(권장)** 함께 옮기고 사전 공지(M-0.3)·완료 공지(M-4.4)를 전달한다. / B 다른 작업자 파일은 제자리(대응표와 규칙에 예외가 생김) |
| Q-M4 | Unity 확인 시점 | **A(권장)** Phase마다(MovedFrom·씬 연결이 있는 M-2를 따로 보기 위해). / B M-3 뒤 한 번에(MSBuild·관리 실행·정적 검사는 Phase마다 한다) |
| Q-M5 | 검증 방법(2026-10-21 추가, §0.3) | **A(권장)** 이 정리 작업만 2026-10-20 지시의 예외로 MSBuild를 Phase마다 돌린다(오류 0 확인만, 관리 실행·새 시험 없음). 파일 241개의 `using`을 기계로 바꾸므로 컴파일 확인 없이 넘기면 Unity에서 오류를 한꺼번에 받는다. / B 지시 그대로 MSBuild 없음 — 정적 검사(§5)만 하고 "컴파일 미확인"으로 보고, 컴파일은 사용자가 Unity에서 Phase마다 확인 |

## 9. 실행 기록

### 9.1 2026-10-21 코드 이동 (Q-M1 B: M-1~M-3을 한 번에)

- **기준:** HEAD `fc392a72`, 작업 트리 깨끗(에디터가 바꾼 `FMODStudioCache.asset`만).
- **기준선(M-0.2, Q-M5 A):** `ProjectL.Tests.Framework.csproj`(`ProjectL.Systems` 포함) MSBuild 오류 0·경고 18종. Combat 형식을 쓰는 파일은 `ProjectL.Systems` 13개·`ProjectL.Tests.Framework` 167개뿐이라 이 빌드 하나로 덮는다. Unity csproj의 Compile 목록은 옛 경로라, 스크래치 `after.targets`가 `ProjectL.Systems`의 Combat Compile을 지우고 `Combat\**\*.cs`로 다시 넣는다.
- **도구(§4.1):** 스크래치 `restructure.pl`(대응표 → 이동·`namespace`·`using`·완전 이름·`MovedFrom`)·`check.pl`(§5). `using`은 코드와 `cref`에 나오는 형식 이름으로 다시 계산한다. 멤버 접근(앞에 `.`)과 메서드 호출(뒤에 `(`, 앞에 `new` 없음)은 형식 참조로 세지 않는다(시험 도우미 `FieldView()` 때문에 생긴 불필요한 `using`을 걸러냄). `cref`에만 나오는 형식도 문서 링크가 풀리게 `using`을 붙였다(파일 넷: `CombatHoverKind`·`ICombatItemDragFeedback`·`ICombatPointerOcclusion`·`ICombatSourceOverlap`, 이 넷은 처음 생긴 `using` 블록 뒤 빈 줄 하나).
- **결과:** 이동 229개(+ `.meta`, 루트 12개는 그대로), `namespace` 229줄, `using` 바뀐 파일 296개(추가 667·삭제 214), `MovedFrom` 16개, 완전 이름 2곳(`ViewDataTableTests:184-185`), 부분 이름 1곳(`CombatViewSystem.cs:1056`). Combat 밖에서 바뀐 것: Factory 넷, UI 넷(`PackUI`·`CombatEndUI`·`DeckCardList`·`BattleChangerUI`), 튜토리얼 `TutorialViewFocusLocator`, 시험 154개(`using`만).
- **검증:** 정적 검사(§5) 모두 통과(`.cs` 241·`.meta` 241, 네임스페이스 26개 = 폴더, GUID 241개 같음, 옛 위치 0, `using` 722개 모두 있는 네임스페이스). HEAD와 비교한 바뀐 줄은 `using`·`namespace`·`MovedFrom`·빈 줄 넷·§4.3의 세 줄뿐. MSBuild 오류 0, 경고는 기준선과 같은 18종(파일 이름·코드·문구로 비교, 줄 번호는 `using` 때문에 바뀜). 빌드한 `ProjectL.Systems.dll`에 새 네임스페이스가 들어 있음을 확인. 관리 실행·새 시험은 하지 않았다(2026-10-20 지시). 검토 에이전트는 돌리지 않았다 — 위 기계 검사가 같은 범위를 본다.
- **줄 끝:** git에는 모두 LF이고 작업 트리는 `core.autocrlf=true`라 파일마다 지금 줄 끝(Combat `.cs` 기준 CRLF 229개·LF 12개)을 그대로 두었다.
- **계획과 다른 점·발견:** Combat 안에 현강님 문서 `.md` 셋(`CombatDeploymentConfirmation.md`, `Display/CombatUnitInfo.md`, `Display/UnitHUD-screen-layer.md`)이 있다. 계획 범위(`.cs`) 밖이었으나 사용자 지시("md 파일도 코드 따라 같이 옮겨")로 설명하는 코드 옆 `Input/ActionButtons/`·`Display/Hud/`·`Display/Unit/`로 옮겼다(`.meta` 함께, GUID 같음, 내용 무수정 — 안에 경로 없음). 이 문서를 가리키던 다른 분 문서 링크 둘(호진님 튜토리얼 설계 512줄, 현강님 기록 데이터 경계 인계 1213줄)은 고치지 않고 공유 메모 §4에 적었다.
- **공유:** [전체 공유 메모](../handoffs/2026-10-21-combat-folder-restructure-share.md)(M-0.3 사전 공지와 M-4.4 완료 공지를 하나로, 미전달). 다른 작업자가 만든 파일(처음 추가한 사람 기준) 중 옮긴 것은 23개(현강님 18·민형님 2·천명님 2·호진님 1, 메모 §3.1), `using`만 바뀐 그분들 파일은 메모 §3.2.
- **Unity 확인 목록(M-1~M-3 합침):** 컴파일(Editor.log), EditMode 전체, **Missing Script 없음** — `Combat.unity`, `HUD.prefab`, 유닛 프리팹(`FieldUnitView`·무기 View), 기차 프리팹(Head `RailwayTrainVisualBinding`·Body `RailwayTrainBodyBinding`), 배치 고스트, 종료 화면(`CombatRewardSlot`). Combat 진입 뒤 배치 드래그·덱 카드 배치·공격 조준·경로 지정·복귀·가방 장착·HUD 우클릭 해제·포기, 공격·포효·소환·피격·사망·기차 재생, 종료 화면.
- **커밋 때:** 새 폴더 25개의 `.meta`는 Unity가 만든 것을 함께 커밋한다(M7). (2026-10-21 Unity가 이미 만들었고, Editor.log에 이동 뒤 "Tundra build success"가 있다 — 시험 결과는 아님.) 이동 229개는 `.cs`·`.meta`가 옛 경로 삭제 + 새 경로 추가로 보이고, 스테이지하면 git이 이름 변경으로 잡는다.
- **다음:** M-4 — 배치 안내 `viewDocs/combat-view-folder-guide.md`(M-4.2), 구조 안내 아티팩트 갱신(M-4.1), CLAUDE.md(M-4.5는 이번에 한 줄 적음). M-4.3은 고칠 착수 전 계획이 없다.

### 9.2 2026-10-21 Unity 확인·M-4

- **Unity 확인 문제 없음**(사용자, 9.1 확인 목록). 사용자 커밋 `81354f33`, 그 뒤 호진님 브랜치 병합 `538a19ef`. 병합이 바꾼 Combat 사용 파일 셋(`CombatViewSystem`·`InGameViewSystemFactory`·`CombatEndUI`)은 새 Combat 형식을 쓰지 않아 `using` 추가가 필요 없고, 정적 검사(§5)는 병합 뒤에도 통과.
- **M-4.2 배치 안내** [combat-view-folder-guide.md](../viewDocs/combat-view-folder-guide.md) 새로 씀: 폴더 트리와 네임스페이스, 판단 순서(연출 부품 → 결과 재생 → 표시 → 입력 → 루트는 묻기), 2단 폴더 표(예 파일 76개는 모두 그 폴더에 있음을 확인), 헷갈리는 경우(계약은 다루는 기능 폴더·입력이 표시에 요청하는 계약만 `Input/Contracts/`, 실제와 미리보기가 같이 쓰는 식은 실제 쪽, 연출 값은 `Playback/Support/`, 창구를 쓰는 특정 연출은 그 연출 폴더, `.md`는 코드 옆), 파일·하위 폴더를 만들 때(M4 이름 규칙). **새 파일 위치의 원본은 이 안내 하나이고, §3 표는 10-21 이동 기록으로 고정한다.**
- **M-4.1 구조 안내 아티팩트** 2판(같은 주소): 파일 경로 375곳·표 안 폴더 칸 29곳·색인 폴더 칸 184곳을 새 위치로, 파트 02~05 소제목에 폴더 표시, §3 폴더 절을 새 구조와 배치 안내 링크로("평면" 문장 교체), 머리말에 "역할 설명은 10-18 기준, 위치만 10-21" 표시. 색인에 10-18 뒤 생긴 Combat 파일 57개를 더함(대단위 = 같은 폴더의 것, "10-18 뒤 추가" 표시, 파트 문서 설명은 없음) — 색인 합계 246(Combat 241 + Factory 5).
- **M-4.3:** 고칠 착수 전 계획 없음(카메라·클립 끝남).
- **M-4.4:** [공유 메모](../handoffs/2026-10-21-combat-folder-restructure-share.md)에 완료 커밋·배치 안내 링크(사전·완료 공지를 하나로 했으므로 같은 메모, 미전달).
- **M-4.5:** CLAUDE.md 현재 상태·기준 문서 표.
