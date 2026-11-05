# 전투 배경 맵을 카메라 연결 전에 세우기 — Task 계획

- 날짜: 2026-10-23
- 설계: [backdrop-before-camera-bind-design](../specs/2026-10-23-combat-backdrop-before-camera-bind-design.md)(BD1~BD6)
- 상태: **BK-1·BK-2 완료(2026-10-23, Stage, 컴파일·Unity 미확인). 남은 것: 사용자 확인·커밋 → Refector-Stage 병합 → BK-3.** Q-B4·Q-B5 결정(Stage에서 먼저 구현 → 사용자가 Refector-Stage에 Stage 병합 → 조정 → 조정자 수정 C1).
- 방식: 상시 지시대로 **MSBuild·새 `[Test]` 없음.** 깨지는 기존 시험만 최소로 맞추고, 보고에 "컴파일 미확인"을 적는다. Phase마다 멈추고 보고한다. 커밋은 사용자.

## 0. 착수 조건

- 브랜치 `feature/Yonggeun/Stage`(지금 `e706b65`). 작업 트리의 Unity 재저장 파일 넷(`StageViewProp_*.asset` 셋·표 에셋, 설계와 무관)은 건드리지 않는다.
- Refector-Stage 스태시(`stash@{0}`)는 손대지 않는다.

## BK-1 코드 (Stage)

줄 번호는 Stage 판 기준이다.

### T1 배경 Presenter: 카메라는 읽기만 (`Display/Map/StageViewPropPresenter.cs`)

- 생성자 `(IStageViewPropTableQuery props, Transform parent)`. 창구 인자와 `_camera` 필드를 뺀다.
- `internal IReadOnlyList<CombatMapCamera> MapCameras` — 세운 맵의 카메라 사실. 맵이 없으면 빈 목록.
- `Show`(42): 세운 직후 `CombatMapCameras.Read(_instance.transform, _mapCameras)`. 실패하면 지금처럼 `Release` 뒤 다시 던진다(같은 키로 다시 세울 때 다시 읽음).
- `Release`(72): 창구 `DetachMap` 대신 `_mapCameras.Clear()`. 인스턴스 파괴는 그대로.
- `AttachCameras`(94) 삭제. 클래스 문서의 K1 설명을 "읽어서 낸다, 창구 연결은 Factory"로 고친다.

### T2 Factory (`Factory/Runtime/InGame/View/CombatSceneDisplayFactory.cs`)

- 새 입구 `internal static StageViewPropPresenter CreateBackdrop(CombatSceneRoot root, CombatDisplayTables tables)`.
  - `root`·`tables`·`root.Field` null 검사 뒤 `new StageViewPropPresenter(tables.StageProps, root.Field.CellsRoot)`.
  - 씬 전체 참조 검사(`ValidateReferences`)는 하지 않는다. 지금처럼 100번 `Create`가 한다.
- `Create(root, tables, services, StageViewPropPresenter backdrop = null)`: 창구를 만든 직후(86) `if (backdrop != null) camera.AttachMap(backdrop.MapCameras)`.
  - 89번 줄의 배경 생성을 뺀다.
  - 표시 생성자에 `props` 대신 `camera`를 넘긴다.
  - 85번 주석(K1)을 고친다.
- 표시를 조립하는 기존 시험 6개 파일은 선택 인자라 그대로다(설계 F12).

### T3 표시 (`Display/CombatSceneDisplay.cs`)

- 필드·생성자 인자 `StageViewPropPresenter props` → `CombatCameraWindow camera`(필드 71, 인자 108, 대입 144).
- `Install`(176)의 `_props.Show(values.StageKey)` 삭제.
- `Clear`(254)의 `_props.Release` 삭제. 배경은 남긴다(Q-B2 A). 창구는 뒤따르는 `Detach` → `Release`가 비운다.
- `Release`(271)의 `_props.Release`를 `_camera.DetachMap`으로. 위치는 같은 자리(필드 해제 직전)다.
- `CombatDisplayValues.StageKey`는 그대로 둔다. 오브젝트·지형 외형 회전(`Refresh` 211·212)이 계속 쓴다.

### T4 조정자 (`CombatViewSystem.cs`)

- 상수 `internal const int BackdropEnterPriority = -195;`(71 근처). 주석: "카메라 다시 연결(-190, `GameBootstrap`)보다 앞. 순서 원본은 씬 연결 안내."
- 필드 `_loadedRoot`(-195에서 찾은 루트), `_backdrop`.
- `Initialize`(256): `RegisterSceneEnterLoaded(SceneIndices.Combat, new LodingNode(BackdropEnterPriority, OnCombatSceneLoaded))`를 100번 등록 앞에.
- 새 `OnCombatSceneLoaded`:
  1. 오류 상태면 끝.
  2. 루트 찾기(지금 `OnCombatSceneEntered` 671의 찾기 코드를 옮김, 정확히 하나)를 `_loadedRoot`에 보관.
  3. `_backdrop = CombatSceneDisplayFactory.CreateBackdrop(_loadedRoot, _displayTables)`.
  4. `_backdrop.Show(_current.HasSelection ? _current.CurrentStage.StageKey : 0)`.
  - 진입 상태는 보지 않는다(Q-B1 A).
- `OnCombatSceneEntered`(671): 찾지 않고 `_loadedRoot`로 `Attach`. 없으면 `InvalidOperationException`("-195 씬 로드 Node가 먼저 돌아야 합니다").
- `Attach(root)`: Factory `Create`에 `backdrop: _backdrop`. 단 `ReferenceEquals(root, _loadedRoot)`일 때만 넘긴다. 시험이 다른 루트로 직접 붙일 때 남의 배경을 쓰지 않게 하려는 것이다.
- `Detach`(577): 표시가 없을 때 일찍 돌아가는 길(592)과 끝 길 모두에서, 마지막에 `CleanupErrors.Try(ReleaseBackdrop, ref errors)`.
  - `ReleaseBackdrop`은 `_backdrop?.Release()` 뒤 `_backdrop = null`, `_loadedRoot = null`이다.
  - 순서는 표시 정리 → 배경이다(BD4).
- `ReadDisplayValues`의 스테이지 키 주석(P6)은 그대로다. -195도 같은 원본(`_current`)을 읽는다는 한 줄을 더한다.

### T5 깨지는 시험 (`Assets/Tests/EditMode/Framework/CombatMapCameraTests.cs`, 우리 파일)

- `PropPresenter_AttachesMapCamerasOnShow_KeepsThemOnTheSameKey_AndDetachesOnRelease`(80)
  - 창구 없이 생성한다.
  - 단언을 `presenter.MapCameras.Count`로 바꾼다: 세우면 1, 같은 키면 1, 해제하면 0.
  - 이름은 `PropPresenter_ReadsMapCamerasOnShow_KeepsThemOnTheSameKey_AndClearsOnRelease`.
- `PropPresenter_CameraReadFailure_RemovesTheProp_AndRetriesOnTheSameKey`(103)
  - 창구 없이 생성한다.
  - `window.CanZoom` 단언을 `MapCameras.Count == 0`으로 바꾼다.
- 남는 동작(읽기·같은 키 유지·해제·실패 시 치움)을 계속 보는 시험이라 지우지 않고 고친다. 창구 연결은 Factory로 옮겨 가 이 파일에서 보지 않는다.
- `MapSoDatabaseAssemblyTests`(민형님)는 두 인자 생성이라 그대로다.

### BK-1 보고에 적을 것

- 컴파일 미확인, 새 시험 없음.
- Unity 확인 목록(설계 §5): 컴파일·EditMode, 진입 직후 배경에 낮/밤 전환·줌·흔들림, 이탈 뒤 배경·라이트맵 정리, 민형님 증상.

## BK-2 문서 (Stage)

- [씬 연결 안내](../../viewDocs/runtime-scene-binding-guide.md) §1 블록:
  - `priority -195: CombatViewSystem이 씬 루트를 찾고 배경 맵을 세움 (Combat만, 카메라 다시 연결보다 먼저)` 한 줄을 넣는다.
  - 100번 줄의 Combat 설명을 "-195에서 찾은 루트로 Attach"로 고친다.
  - §5 "하지 말 것"의 "View 시스템은 씬 로드 Enter 처리(100)에서 찾는다"에 Combat -195 예외를 적는다.
- [카메라 조각 설계](../specs/2026-10-19-combat-camera-pieces-design.md) K1 머리에 개정 표시: "2026-10-23: 맵 카메라 사실은 배경 Presenter가 읽고, 창구 연결은 Factory가 표시를 만들 때(배경 설계 BD3)".
- 민형님 [Factory 공유 문서](../handoffs/2026-10-15-combat-scene-display-factory-framework-share.md)에 새 절(미전달):
  - `CreateBackdrop` 입구, `Create`의 끝 선택 인자.
  - 맵이 -195에 생겨 -190 카메라 수집에 들어감.
  - ViewStage 표·`MapAssembler`·카메라 시스템은 무변경.
  - 실제로 본 증상을 알려 달라는 질문.
- CLAUDE.md는 Stage에서 고치지 않는다. Refector-Stage 스태시의 `CLAUDE.md` 변경과 겹치므로 BK-3에서 적는다.

## BK-3 병합 뒤 조정 (Refector-Stage, 사용자 병합 뒤)

- 사용자가 스태시를 되살리고 Stage를 병합한 뒤 충돌을 확인한다.
  - 예상 충돌: `CombatViewSystem`, 씬 연결 안내 §1.
    - `CombatViewSystem`에서 Refector-Stage가 바꾼 곳(`OnCombatSceneDisabled` F1·`IsHoldingResult`·`Progress`)은 BK-1이 바꾼 줄과 겹치지 않는다.
    - 겹칠 수 있는 줄: `Initialize`의 Node 등록, `Shutdown => ReleaseScene`.
- 안내 §1.1 표(원본)에 줄 하나: `Enter | -195 | Combat | CombatViewSystem | 씬 루트 찾기·배경 맵 세우기 | CombatViewSystem.BackdropEnterPriority`. 100번 줄 설명을 고친다.
- `GameBootstrap.BindCamerasPriority` 주석에 "Combat 배경(-195)이 이보다 앞" 한 줄(선택). **(2026-10-23 해소: 상수가 없어지고 순서 표에 `SceneNodeOrder.CombatBackdrop = BindCameras - 5`로 관계가 적힘, 골격 점검 K-4)**
- CLAUDE.md 현재 상태에 한 줄.
- 그 뒤 조정자 수정(C1) 설계로 넘어간다(Q-B5). C1은 이 Node를 씬 연결 묶음에 넣는다.

## 실행 기록

### BK-1 코드 (2026-10-23, Stage `e706b65` 위, 컴파일 미확인, 새 시험 없음)

사용자 "Q-B1~Q-B3 모두 A, Stage에서 BK-1 진행".

| Task | 한 것 |
|---|---|
| T1 | `StageViewPropPresenter`: 생성자 `(표, 부모)`, `MapCameras` 읽기 전용 목록. `Show`가 세운 직후 `CombatMapCameras.Read`(실패면 치우고 다시 던짐). `Release`는 목록을 비우고 인스턴스를 없앰. 창구 필드·`AttachCameras` 삭제 |
| T2 | Factory `CreateBackdrop(root, tables)`(루트·표·필드 null 검사). `Create(…, backdrop = null)`이 창구를 만든 직후 `AttachMap(backdrop.MapCameras)`. 배경 생성 줄 삭제, 표시에 창구를 넘김 |
| T3 | `CombatSceneDisplay`: `_props` → `_camera`(`CombatCameraWindow`). `Install`의 `Show` 삭제. `Clear`의 배경 해제 삭제. `Release`의 배경 해제 자리에 `_camera.DetachMap` |
| T4 | `CombatViewSystem`: `BackdropEnterPriority = -195`, `_loadedRoot`·`_backdrop`. `Initialize`가 -195 `OnCombatSceneLoaded`를 100번 앞에 등록. -195는 오류 상태면 끝, 표·서비스 확인, 남은 배경 정리, 루트 찾기(옮김), 배경 생성·`Show(CurrentStageKey)`. 100번 `OnCombatSceneEntered`는 보관한 루트로 `Attach`(없으면 예외). `Attach`는 같은 루트일 때만 배경을 Factory에 넘김. 스테이지 키 식을 `CurrentStageKey` 하나로 묶음(`ReadDisplayValues`와 공유) |
| T5 | `CombatMapCameraTests`의 Presenter 시험 둘: 창구 없이 생성하고 `MapCameras.Count`로 단언(첫 시험 이름 `PropPresenter_ReadsMapCamerasOnShow_KeepsThemOnTheSameKey_AndClearsOnRelease`) |
| 주석 | `CombatCameraWindow` 클래스 문서의 K1 설명 |

**계획과 다른 점**

1. 배경 해제를 `Detach`가 아니라 새 `ReleaseScene`에 두었다. `ReleaseScene`은 `Detach` → 배경 해제·루트 보관 비움이고, Exit -100 Node와 `Shutdown`이 부른다.
   - 이유: `Detach`는 전투 중간에도 불린다(`Clear` 뒤, 씬 루트 꺼짐). 그래서 `Detach`에 넣으면 Q-B2 A(씬 이탈까지 배경 유지)가 깨진다.
   - 설계 BD4 표를 고쳤다.
2. 스테이지 키 식을 `CurrentStageKey` 하나로 묶었다. -195와 표시 값이 같은 식을 두 번 쓰지 않게 하려는 것이다.

**실행 때 실패가 예상되던 시험 — 사용자 지시로 고침(아래 "BK-1 이어서")**

- `CombatViewSystemTests.Initialize_RegistersCombatSceneNodes_AroundLifeCyclePriority`(우리, `466f8c62`) 192·195줄이 `Loading.Enter.Single()`을 불렀다. Combat Enter Node가 둘(-195·100)이 되어 실패할 것이었다.
- 같은 파일 `ExitNode_DetachesTheScene`(213)은 Exit Node가 하나 그대로라 통과할 것이다.

**작업 트리의 다른 변경(이번 작업 아님, 건드리지 않음)**

Unity가 다시 저장한 파일들이다.
- `StageViewProp_*.asset` 13개·표 에셋: `m_Name`이 파일 이름으로 바뀌고 `map: {fileID: 0}` 줄이 붙음.
- `Combat.unity`: `CombatEndUI.ending` 빈 칸, 앵커 값 하나.
- `FMODStudioCache.asset`.

**Unity 확인 목록**

1. 컴파일, EditMode. 위 실패 하나가 예상된다.
2. 전투 진입 직후 배경 위에서 낮/밤 화면 전환, 포효 줌, 피격·기차 흔들림이 되는지.
3. 패배·포기·승리로 이탈한 뒤 배경과 라이트맵이 남지 않는지.
4. 개발 직행 진입(Play From Boot)도 배경이 서는지.
5. 민형님이 본 증상이 사라졌는지.

### BK-1 이어서 (2026-10-23, 사용자 "고쳐주고 안 쓰는 테이블들도 치우자", 컴파일 미확인)

**노드 등록 시험**
- `Initialize_RegistersCombatSceneNodes_AroundLifeCyclePriority`를 Enter Node 둘에 맞췄다.
  - 모두 Combat 씬인지 본다.
  - 우선순위가 등록 순서대로 `[BackdropEnterPriority, SceneEnterPriority]`인지 본다.
  - 배경은 -190보다 앞인지, 연결은 0보다 뒤인지 본다.
  - Exit 단언은 그대로다.

**안 쓰는 표 정리(민형님 ViewStage Phase 1이 카탈로그에서 뺀 것)**
- 사용자가 지시한 삭제다(CLAUDE.md "참조 끊긴 에셋은 보고만"의 예외). `.meta`와 함께 지웠고, Git 인덱스에는 올리지 않았다.
- 지우기 전에 GUID로 다른 참조가 없는지 확인했다. 표 SO 스크립트의 GUID는 지운 환경 표 에셋만 썼다. `StageViewPropDataTableSO`의 GUID를 쓰는 에셋은 없다. 옛 표 에셋은 이미 `ViewStageDataTableSO`로 바뀌어 있다.

| 지운 것 | 이유 |
|---|---|
| `DataBaseSO/ViewGroup/StageEnvironmentVisual/`(표 1 + 항목 3 + 폴더, 각 `.meta`) | 카탈로그(`ViewGroup.asset`)에서 빠졌고 다른 참조 없음. 회전 값은 모두 비어 있었다 |
| `Data/View/Table/StageEnvironment/StageEnvironmentVisualDataTableSO.cs`(+ `.meta`) | Factory가 더 쓰지 않음. 쓰던 곳은 위 에셋과 아래 시험 하나 |
| `Data/View/Table/StageViewProp/StageViewPropDataTableSO.cs`(+ `.meta`) | 참조 0 |

- 지운 스크립트를 쓰던 시험 `TerrainOperationalDisplayTests.TemporaryCatalog`(우리, `7ec2b981`)도 고쳤다.
  - 스테이지 1101의 지형 회전을 넣는 곳을 `ViewStageDataTableSO`·`ViewStageDataSO`로 옮겼다. 직렬화 칸 이름 `terrainRotations`·`sourceId`·`rotation`은 같다.
  - `using` 둘을 바꿨다.
  - 앞서 보고한 "회전 주입이 안 일어나 회전 단언(166줄)이 실패할 것"도 이것으로 풀린다.

**남긴 것(민형님 ViewStage Phase 2·소비자 전환 몫)**
- 레거시 항목 SO `StageViewPropDataSO`와 `StageEnvironmentVisualDataSO`를 남겼다. 시험(`MapSoDatabaseAssemblyTests`·`EnvironmentStageAuthoringTests`)이 쓴다.
- 호환 표의 옛 생성자 둘과 호환 조회 두 클래스도 남겼다.
- `gameObject`·`map` "둘 중 하나만" 검사는 이번에 하지 않았다(요청 범위 밖).

**보고만**
- `EnvironmentAuthoringAssetTests.StageRotationTemplate_LoadsWithoutInventedPositionsOrSourceIds`(101줄)는 `StageEnvironmentVisual_Template.asset`을 읽는다. 이 파일은 병합 전(`067b53ad`)에도 없었다. 그래서 이번 삭제와 상관없이 이미 실패하던 시험이다. 142줄 "카탈로그에 Template 없음" 단언은 그대로 통과한다.
- BK-2의 민형님 공유 절에 이 정리 목록을 함께 적는다.

### BK-2 문서 (2026-10-23, Stage, 코드 변경 없음)

| 문서 | 고친 것 |
|---|---|
| [씬 연결 안내](../viewDocs/runtime-scene-binding-guide.md) §1 | Enter 목록에 `-195 CombatViewSystem이 SceneRoot를 찾고 배경 맵을 세움`. 100번 줄에 "Combat은 -195에서 찾은 것을 씀", Exit -100 줄에 "Combat은 배경 맵도 없앰". 블록 아래에 -195에 세우는 이유 한 단락 |
| 같은 안내 §5 "하지 말 것" | `BindScene` 줄에 Combat -195 예외(찾는 곳은 여전히 한 곳) |
| [카메라 조각 설계](../specs/2026-10-19-combat-camera-pieces-design.md) K1 | 머리에 2026-10-23 개정 인용 블록, "수명" 줄은 옛 판 취소선 + 지금 규칙 |
| 같은 설계 §3.3 | 표시는 프랍을 세우거나 없애지 않고 `Release`에서 `DetachMap`만, 프랍은 `ReleaseScene`. "사건 정리 → 카메라 사실 비움 → 카메라 제거" 순서는 그대로 |
| 민형님 [Factory 공유](../handoffs/2026-10-15-combat-scene-display-factory-framework-share.md) §22(미전달) | 생성 시점 표, Factory 입구 변경, 바꾸지 않은 것, 안 쓰는 표 정리 목록·남겨 둔 레거시, 확인 부탁 셋(실제 증상, 맵 SO 카메라 이름, `gameObject`·`map` 둘 다 찬 경우) |

- 이 문서들은 Stage와 Refector-Stage에서 내용이 같았다(2026-10-23 대조). 그래서 병합 때 이 줄들이 충돌할 곳은 씬 연결 안내 §1 블록뿐이다. Refector-Stage는 §1.1 표가 더 있다.
- CLAUDE.md는 BK-3에서 적는다(계획 그대로).
- **BK-1·BK-2 끝.** 남은 것:
  - 사용자 Unity 확인(BK-1 확인 목록)·커밋
  - Refector-Stage 병합 뒤 BK-3
