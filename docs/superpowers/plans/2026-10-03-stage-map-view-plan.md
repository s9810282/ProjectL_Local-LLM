# 스테이지 선택 View 계획

- 작성일: 2026-10-03
- 작성: View 작업(스테이지 선택 담당)
- 브랜치: `feature/Hojin/Stage_Shop_Event`
- 근거: [런타임과 씬 연결 안내](../viewDocs/runtime-scene-binding-guide.md), [Framework 프로그래머 가이드](../../../Assets/ProjectL/Script/Systems/Documentation/framework-programmer-guide.md) FW-001~003·016·101, [DB-SO 설계](../specs/2026-09-24-db-so-ingame-view-group-design.md) §6.3
- 진행·검증 상태는 이 문서 §7 한 곳에만 적는다.

## 1. 전체 순서와 현재 상태

| 순서 | 내용 | 상태 |
|---|---|---|
| 1 | 맵 데이터 읽기 (`StageMapViewReader`) | 읽기 완료. 데이터 대기 처리는 3에서 완성 |
| 2 | 맵 표시 (`StageMapViewDisplay`·`StageNodeView`·`StageLinkView`) | 코드 완료. Unity 테스트·프리팹 재저장 확인 필요(§7) |
| 3 | 화면 수명 연결 (+ T7 맵 준비) | 완료 (2026-10-04, §7) |
| 4 | 선택 입력 연결 (StageSelection Layer, 노드 클릭, 취소. 강조 색은 보류) | 완료 (2026-10-04, §8) |
| 5 | 선택 요청 기록 (`StageViewWriteSink.Submit`) | 완료 (2026-10-04, §8) |
| 6 | Core와 계약을 맞춘 뒤 진행 연결 (Flow·Plan·Job) | 진행 중. 전투 노드 진입 요청 연결 코드 완료(2026-10-06, §7. Play·Unity 테스트 미검증, 운영 진입은 T1/G7 대기). `StageLifeCycle`의 Flow 연결, 상점·이벤트 노드는 미연결. 전제 조건은 §5 |

## 2. 3번 결정

- **D1 — 범위 (b안):** 3번은 View 연결만 한다. 맵 데이터를 준비하는 Flow(`StageLifeCycle.OnEnter` → `StagePlan`)는 연결하지 않는다. 동작은 EditMode 테스트에서 가짜 Buffer 데이터로 검증한다. 3번이 끝나도 실제 Play에서는 맵이 대기 상태로 남는 것이 정상이며, 화면 확인은 6번에서 한다.
  - **2026-10-04 변경(사용자 결정):** 화면 확인을 위해 맵 준비만 3번에 넣는다(T7). `StageLifeCycle.OnEnter`가 기존 `Register(JobRootType.Stage, JobType.Start)`를 한 번 등록하고, 기존 `StageJob`이 `EnterChapter`로 맵을 준비한다. `StageMapFlow`·노드 진입·`EnterBattle`은 6번 범위로 남는다. 같은 챕터 재진입은 그래프가 있으면 다시 생성하지 않고 수락된다(`ChapterProgressionCoordinator.EnterChapter`).
- **D2 — 카메라:** Systems에 카메라 서비스가 없고 FieldCamera는 다른 작업자 영역이다. 3번은 씬에 배치한 카메라를 그대로 쓰고 코드는 카메라에 관여하지 않는다. 피킹에 Camera가 필요한 4번에서 Framework 담당자의 제공 방식을 확인한다.
- **D3 — 다시 그리는 시점:** Attach마다 데이터가 준비되면 한 번 그린다. 클리어 상태는 전투 씬에서 바뀌고 복귀 때 씬이 다시 로드되므로 충분하다. 스테이지 맵 씬 안에서 챕터가 바뀌는 경우는 6번에서 다룬다.
- **D4 — 서비스 노출:** `StageMapViewSystem`은 모듈로만 등록한다(`AddModule`). 다른 시스템이 호출해야 하는 4·5번에서 `IStageMapView : IServiceModule`을 추가한다(FW-001).

## 3. 3번 구조

```text
[Boot에서 한 번 생성]                               [StageSelect 씬에 미리 배치]
InGameViewSystemFactory                             StageMapSceneRoot (MonoBehaviour)
  └─ StageMapViewSystem (일반 C#)   ── Attach 때 한 번 찾음 ──►  └─ mapRoot (Transform)
       ├─ StageMapViewReader   (Buffer 읽기, 기존)
       └─ StageMapViewDisplay  (생성·정리, 기존)
```

| 구성 | 위치 | 책임 | 하지 않는 일 |
|---|---|---|---|
| `StageMapSceneRoot` | `ViewSystem/StageMap/StageMapSceneRoot.cs` | `mapRoot` 참조 보관 | 동작, 런타임·서비스 찾기 |
| `StageMapViewSystem` | `ViewSystem/StageMap/StageMapViewSystem.cs` | 언제 그리고 정리할지 판단, 씬 연결 | Core 소유 Buffer 쓰기, Tick 중 `Find` |
| Factory | `Factory/Runtime/InGame/View/InGameViewSystemFactory.cs` | `new StageMapViewSystem().ExportServices(builder)` 한 줄 | — |

`StageMapViewSystem` 생명주기:

| 단계 | 하는 일 |
|---|---|
| `InitData` (`ISystemInitData<IDataBaseQueryContext>`) | `ViewQuery.StageMapViewTableQuery.Config`를 보관한다. 외형 조회 함수(`StageNodeVisualTableQuery.TryGetNodeVisual` → `VisualPrefab` 또는 null)를 여기서 한 번만 만든다 |
| `PostBindService` | `LodingRegisterService`를 받는다. `BufferSystemKey.Buffer`의 `CurrentProgressReadSink`·`StageProgressReadSink`로 `StageMapViewReader`를 만든다 |
| `Initialize` | `SceneIndices.Stage`에 Enter(100)·Exit(-100)를 등록한다. 우선순위는 이름 있는 상수로 둔다 |
| Attach (씬 Enter) | `FindFirstObjectByType<StageMapSceneRoot>()`를 한 번 호출하고 내부 `Attach(StageMapSceneRoot)`에 넘긴다. 없으면 `InvalidOperationException`. 상태를 대기로 둔다 |
| `Tick` | 연결돼 있고 대기 상태일 때만 `TryRead`. 성공하면 `Display.Show` 한 번 호출 후 표시 상태로 바꾼다. 할당 없음 |
| Detach (씬 Exit) | `Display.Clear()`, Root 참조 해제, 상태 초기화 |
| `Shutdown` | Detach (멱등) |

`Show`가 예외를 던지면 Display가 만든 것을 지운 뒤 예외를 그대로 전파한다. 3번에서 따로 복구하지 않는다.

## 4. Task

- [x] **T1** `StageMapSceneRoot` 작성 (파일 이름 = 클래스 이름, `[DisallowMultipleComponent]`, `.meta` 포함)
- [x] **T2** `StageMapViewSystem` 작성 (§3 생명주기, 씬에서 찾는 부분과 내부 `Attach(root)` 분리)
- [x] **T3** `InGameViewSystemFactory.Create`에 등록 (Build 전, FW-003)
- [x] **T4** EditMode 테스트 `StageMapViewSystemTests`
  - 연결 전 Tick은 아무것도 하지 않음
  - Attach 후 Buffer가 비어 있으면 대기, 데이터가 들어온 다음 Tick에 한 번 표시
  - 표시 뒤 Tick을 반복해도 다시 만들지 않음
  - Detach 시 생성 오브젝트 정리, 다시 Attach하면 다시 표시
  - Shutdown 두 번 호출해도 안전
- [x] **T5** `StageSelect.unity`에 `StageMapSceneRoot`와 `mapRoot` 배치
- [x] **T6** 검증: Unity 컴파일, EditMode 전체(`StageMapViewDisplayTests`·`StageMapViewSystemTests`·`DataBaseAssetTests` 포함), Play From Boot로 StageSelect 진입 시 예외 없음 확인
- [x] **T7** 맵 준비 연결(D1 변경): `StageLifeCycle`이 `OnBindServices`에서 `JobRegister`를 받고 `OnEnter`에서 `Register(JobRootType.Stage, JobType.Start)`를 한 번 호출한다. Flow는 만들지 않는다(6번에서 `StageMapFlow`로 옮긴다).
  - 검증: Play From Boot(시작 구간 Stage)로 StageSelect 진입 시 Chapter 1 맵이 표시되고 간격·외형·연결선이 맞는지 확인. `Register(Stage, Start)` → 맵 준비 경로는 기존 `ChapterGraphPreparationTests`가 검증한다

## 5. 5·6번 전제 조건 (2026-10-03 확인)

Core 작업자가 Job·Plan 구현을 View 쪽에서 해도 된다고 했다(아티팩트 Framework 인계 문서). 현재 코드 상태는 다음과 같다.

| 구성 | 스테이지 선택 | 전투 |
|---|---|---|
| Plan | `StagePlan`: `NewRun`·`Start`(맵 준비)만 | `CombatPlan`: `Start`·`ProcessCommand`·`ProcessPreview`·`AdvanceCombat` |
| Job | `StageJob`(`EnterChapter`), `ResetRunProgressJob` | 네 Job 모두 Core 호출 |
| Flow | 없음 | `CombatFlow`가 `Register(JobRootType.Combat, …)` 호출 |
| LifeCycle | `StageLifeCycle.OnEnter`/`OnExit` TODO | `CombatLifeCycle`이 Flow 시작·Tick·정지 |
| 실행 여부 | `Register(JobRootType.Stage, …)` 호출자 없음 → 실행 안 됨 (T7에서 `StageLifeCycle.OnEnter`가 `Start` 등록) | 실행됨 |
| 노드 진입 | 진입 Job·`JobType` 없음. `StageViewBuffer` 요청을 읽는 쪽 없음 | `CombatPrepareJob`이 호환용 `PrepareBattle()` 사용. `EnterBattle(chapterKey, nodeId)`로 바꾸는 V1~V5 미완료 |

6번에서 정할 것:

1. `StageMapFlow`를 만들어 `StageLifeCycle`에 연결하고 `Register(JobRootType.Stage, JobType.Start)`로 기존 `StageJob`을 실행한다(`CombatFlow`·`CombatLifeCycle`을 참고). **2026-10-06:** 노드 진입 요청 전달은 이름이 다른 `ViewSystem/StageMap/StageMapEntryFlow`(`StageMapViewSystem`이 Tick)로 먼저 구현했다(§7). `StageLifeCycle`의 Flow 연결은 아직 TODO이고, 맵 준비는 T7 경로(`OnEnter`에서 `Register(Stage, Start)`)를 유지한다.
2. 노드 진입 요청을 Job까지 전달하는 방식. `Register(JobRootType, JobType)`에는 인자 전달 계약이 없다.
   - 후보 A: Job이 실행 시점에 `StageViewBuffer.TryGetNextCommand`로 요청을 읽는다. Framework 계약은 바꾸지 않는다.
   - 후보 B: `Register`에 인자를 받는 형태를 추가한다. Framework 담당자 검토가 필요하다.
3. 전투 노드 진입(`EnterBattle`)은 전투 쪽 V1~V5와 맞닿는다. 담당을 Core 작업자와 정한다. **2026-10-05 확인:** 전투 진입은 이미 `ICombatEntryService.RequestEntry(chapterKey, nodeId)`(`CombatLifeCycle`)가 있고, 진입 Job 완료·승인 뒤 `CombatLifeCycle`이 `LifeCycleManager.Change(Combat)`을 호출한다. 6번은 이 경로를 재사용하고, 남은 것은 전투 종료 뒤 StageSelect 복귀·이탈 정리다.
4. 상점·이벤트 노드 진입 경로(`PrepareStageEntry`·`ConfirmStageEntry`)는 Core 문서 범위 밖이므로 따로 확인한다.
5. **오류 상태 입력 거부(2026-10-05 추가):** 입력은 Unity 콜백으로 Tick과 무관하게 불리므로, 런타임 오류 상태면 맵 입력·요청 기록도 거부한다(아티팩트 계획 D11과 같은 규칙). 6번 착수 때 `StageMapViewSystem`에 함께 넣는다.

## 6. 범위 밖

- `StageMapFlow`와 노드 진입 연결(6번), 입력·강조(4번), 요청 기록(5번). 맵 준비 Job 등록만 T7에서 앞당겼다
- 씬 전환 호출(F2 합의 뒤)
- 현재 위치 마커(`CurrentMarkerPrefab`) 표시: 필요한 단계에서 따로 정한다

## 7. 진행 기록

| 날짜 | 내용 |
|---|---|
| 2026-10-03 | 1·2번 코드 완료. 외형 조회를 `StageNodeVisualTable`로 일원화, `ClearedIndicator` 클리어 표시, 프리팹 자식 이름 상수화. 남은 확인: Unity에서 `StageMapViewDisplayTests`·`DataBaseAssetTests` 실행, `StageMapNode.prefab` 재저장, 프리팹에서 빠진 Missing Script 컴포넌트가 의도한 것인지 확인 |
| 2026-10-03 | 3번 계획 작성(b안). 구현 전 |
| 2026-10-03 | T1·T2·T4 구현: `StageMapSceneRoot`, `StageMapViewSystem`, `StageMapViewSystemTests`(9개). MSBuild로 Systems·Tests 컴파일 확인(오류·새 경고 없음). Unity 테스트 미실행. **T3 보류:** Factory 등록 시 `ChapterGraphPreparationTests`의 `withJobRuntime` 경로(InGame 전체 Factory + `ViewQuery = null`, Global 없음)가 InitData·PostBindService에서 실패한다. 처리 방식 결정 대기 |
| 2026-10-03 | T3 완료(사용자 결정: 테스트 조립 수정). `InGameViewSystemFactory`에 등록하고, `ChapterGraphPreparationTests`의 `withJobRuntime` 경로를 Core Factory + `JobInGameService`로 바꿨다. Core 전달 문서 §5.5에 기록. MSBuild 컴파일 확인, Unity 테스트 미실행. 남은 것: T5(씬 배치)·T6(Unity 검증) |
| 2026-10-03 | T5 씬 파일 설정 완료: 새 `StageMapSceneRoot`에 기존 `MapRoot`를 자식으로 연결하고 직렬화 참조 지정. Boot 시작 구간을 Stage로 설정. Unity Roslyn으로 Systems·Framework 테스트 코드 컴파일 통과(기존 Audio 중복 using 경고 4개), 씬 로컬 ID·참조 검사 통과. T6 실제 EditMode 전체 실행·Boot 재생 검증은 미실행: 열린 Unity에 연결할 CLI/Pipeline이 없으며, 배치 실행을 위해 사용자에게 에디터 저장·종료 요청 중. 검증용 Boot smoke 테스트 소스는 `Temp/StageMapVerification/StageMapBootSmokeTests.cs`에 준비했으며 Assets에는 남기지 않음. T6는 완료로 표시하지 않는다. |
| 2026-10-04 | T7 코드 완료(사용자 결정, D1 변경): `StageLifeCycle`이 Global `JobRegister`를 받아 `OnEnter`에서 `Register(JobRootType.Stage, JobType.Start)`를 한 번 등록한다. 새 Flow·Job·Plan은 만들지 않았다. MSBuild로 Systems·Tests 컴파일 통과(오류 0). 새 단위 테스트는 없다(등록 뒤 맵 준비 경로는 기존 `ChapterGraphPreparationTests`가 검증, LifeCycle 진입은 Play로 확인) |
| 2026-10-04 | T6 실제 Unity 6000.0.73f1 배치 검증: EditMode 1430개 중 1409 통과·21 실패. StageMapViewSystem 9/9, StageMapViewDisplay 3/3, ChapterGraphPreparation 40/40, DataBaseAsset 5/5, FrameworkRuntimeComposition 10/10 통과. 실패: CombatFlowIntegration 15(배치 깊이), RuntimeFaultBoundary 4(테스트 로그 누적), ArtifactDataTable 1(빈 Artifact View 가정), CharacterRosterService 1(IStageViewBufferReadSink 서비스 누락). Boot Play smoke 0/1: Artifact.RotateLeft·RotateRight 물리 바인딩 누락으로 UnityInputBridge.InitData에서 런타임 정지, StageSelect 진입·맵 표시 미검증. 결과·로그·검증 소스는 Logs/StageMapVerification에 보존; 임시 Assets 테스트는 제거. T6 및 3번 완료로 표시하지 않는다. |
| 2026-10-04 | Boot 재생 차단 해소(사용자 결정): `PlayerInputActions.inputactions`의 `Artifact` 맵에 `RotateLeft` = `<Keyboard>/q`, `RotateRight` = `<Keyboard>/e` 바인딩 추가. JSON 파싱·모든 맵 액션 바인딩 존재·ID 50개 중복 없음 확인. 나머지 실패 21개는 3번 무관(CombatFlowIntegration 15 기존, RuntimeFaultBoundary 4는 `491cae4`에서 수정됨, ArtifactDataTable 1·CharacterRosterService 1은 이번에 처리하지 않음). 남은 것: Boot→StageSelect 재생으로 맵 표시 확인(T6·T7) |
| 2026-10-04 | Boot 재검증 통과: Unity 종료 코드 0, smoke 1/1. Boot에서 StageSelect 진입, MapRoot 자식 29개·Renderer 41개 생성, 120프레임 동안 RuntimeFaultBoundary 오류 없음, 예상 외 로그 없음. 이전 Artifact 회전 입력 바인딩 오류는 이번 실행에서 발생하지 않음. 첫 재시도는 검증 코드 자체 Debug.Log를 NoUnexpectedReceived가 잡아 실패했으므로 LogAssert.Expect 추가 후 재실행해 통과 확인. 제품 코드는 수정하지 않음. 화면 픽셀·카메라에서의 가시성은 미검증이며 전체 EditMode 21개 실패도 이번에는 재검증하지 않음. 결과: Logs/StageMapVerification/boot-confirmed-results.xml 및 boot-confirmed-unity.log. 임시 Assets 테스트 제거. |
| 2026-10-04 | **3번 완료.** 사용자가 Play에서 맵 전체가 화면에 보이는 것을 확인했다(카메라는 2026-09-06 `bced7e6f` 설정 그대로, 코드·씬에서 변경 없음). T6·T7 완료. 3번 완료 조건에서 제외한 것: 전체 EditMode 실패 21개(3번 이전부터 있던 문제, 원인은 위 기록). 다음: 4번은 카메라 제공 방식·UI 클릭 처리에 대한 다른 작업자 협의 뒤 계획 작성 |
| 2026-10-04 | 4·5번 U1~U5 코드 완료(§8.3 사용자 결정: 클릭 즉시 진입 요청, 에디터 전용 로그, 이펙트는 연결 자리만, 강조 색 없음, 카메라 `Camera.main`). 새 파일: `IStageNodePicker`, `StageNodePicker`, `IStageMapInteractionFeedback`, `StageMapInteractionLog`, `StageMapInteraction`, `StageMapInteractionTests`(테스트 11개). 수정: `StageMapViewSystem`(입력 슬롯·Layer·StageView Buffer, `Attach(root, picker)`), `StageNodeView`·`StageMapViewDisplay`(`TryGetNodeId`), `StageMapViewDataSO`(`HitTargetName`, Validate), 기존 시스템 테스트(새 Attach 서명, 테스트 4개 추가), Display 테스트 1개 추가, 설계 §6.3. MSBuild로 Systems·Tests 컴파일 통과(오류 0). Unity 테스트·Play(U6) 미실행 |
| 2026-10-04 | **4·5번 완료.** 사용자가 Unity에서 EditMode 테스트와 Play(U6)를 확인했고 문제가 없었다. 노드 클릭 시 진입 요청 로그 1회, 중복 클릭 차단, 다른 노드·빈 곳·취소 시 기록 없음. 다음: Core `87348b86` 수신 뒤 6번 계획 작성(카메라·UI 브랜치는 6번 진행 상황에 맞춰 별도 수신) |
| 2026-10-04 | Core 머지 후 View 조립 복원: CombatView 등록 유지 + StageMapView 등록, StageView/ArtifactView 네임스페이스 복원(Factory·BufferGroup·서비스 키). 테스트 머지 중복 및 진행·로스터 조립 수정, Artifact View 실제 에셋 ID 검증으로 기대값 갱신. Unity 컴파일 통과. 전체 EditMode 2284 중 2283 Passed·0 Failed·1 Inconclusive(종료 코드 2; 전체 통과로 주장하지 않음). 별도 Boot smoke 통과: StageSelect, 자식 24·Renderer 34, 120프레임 오류 없음(merge-verified-results.xml). 실제 클릭은 Interaction 테스트로 검증, 수동 클릭·화면 픽셀 검증 미실행. 커밋·push 미실행. |
| 2026-10-04 | 사용자 지시로 b5d14d7 카메라 머지 충돌 해결(커밋·push 없음): 최신 Core InitData 계약·DB GUID·Boot/StageSelect/Combat BuildSettings 유지, 카메라 서비스 참조 및 Core/URP/Cinemachine 어셈블리 참조 병합. 렌더 색상 API 이동(Sub→Core, Scope→Handle)에 전투 View/테스트 참조 대응. 현재 Material 테이블에 새 렌더가 요구하는 _SelectOutline·_AreaOutline 실제 에셋 참조 추가(기존 _Outline 유지). Unity 컴파일 통과, 전체 테스트 2314 중 2312 통과·1 실패(AreaOutlineHandle Material 정리)·1 Inconclusive. Boot smoke 1/1 통과, MapRoot 자식 30·Renderer 43, 120프레임 오류 없음. 결과 Logs/StageMapVerification/camera-merge-results.xml 및 camera-boot-final-results.xml. e311804는 병합하지 않음. |
| 2026-10-04 | 1fe9cce 병합 충돌 해결(커밋·push 없음). Framework 테스트 asmdef는 Cinemachine·UI·FMOD·Core.Application 모두 보존. 개발 가이드는 새 UI History·Audio 설명과 현재 Mesh Handle/Core 경로 유지, 카메라 서비스·Cinemachine 의존성 문단 추가. 카메라 테스트 GlobalContext에 Audio 계약 대응. Unity 컴파일 통과, EditMode 2326 중 2321 통과·4 실패(기존 AreaOutline 정리, Audio Global Context, MainCharacter UI 2)·1 Inconclusive. Boot smoke 0/1: AudioSystem.BindScene에서 AudioManager를 찾지 못해 초기화 중단. Git 미해결 충돌 없음이나 게임 실행 완료 상태가 아님. 결과 audio-ui-merge-results.xml 및 audio-ui-boot-results.xml. |
| 2026-10-06 | **6번 일부: 전투 진입 요청 연결.** 사용자 승인으로 StageMapEntryFlow를 추가했다. StageViewBuffer의 클릭 기록을 현재 맵의 Combat 노드에 한해 LifeCycle 그룹의 공개 CombatEntry.RequestEntry에 전달한다. 도달 가능 여부·클리어 여부·전투 준비·활성화는 기존 Core 및 CombatLifeCycle이 담당한다. 요청이 끝날 때까지 Buffer 기록을 유지해 중복 클릭을 막고, 거절 결과는 현재 피드백 방식인 로그로 표시한 뒤 기록을 제거한다. 씬 이탈 시 실행 전 요청의 취소를 시도하고 View 기록을 정리한다. 상점·이벤트·시작 노드는 전투 경로에 보내지 않는다. StageMapViewSystem이 Flow를 Tick하고 Factory가 같은 RuntimeFaultBoundary를 읽기용으로 전달한다. 공용 오류 콜백 및 Core·전투 실행 코드는 수정하지 않았다. Unity Roslyn Systems 컴파일 통과. 실제 Play/마우스 진입 및 Unity 테스트 재실행은 미검증. 운영 진입 제한: GameBootstrap은 StageSelect 시작 시 개발 전투 source/로스터 준비를 사용하지 않는다. DatabaseBattleInitializationSource.Read는 현재 T1/G7 미구현 예외를 낸다. 따라서 요청 연결 완료를 실제 전투 진입 완료로 보고하지 않는다. 운영 전투 정의 조회와 로스터 준비 경로가 필요하며, 개발 fixture로 우회하지 않았다. |
| 2026-10-06 | 문서 정리(코드 변경 없음): §8 뒤에 떨어져 있던 2026-10-04 기록 3행과 별도 섹션이던 2026-10-06 전투 진입 요청 연결 기록을 이 표로 옮겼다(내용 유지). §1 상태표 6번과 §5-1의 Flow 이름을 현재 구현에 맞췄다. |
| 2026-10-06 | **전투 노드 클릭 → Combat 진입 실제 검증 통과(코드 변경 없음).** Core `5f2d0381` 병합(`f68408b1`)으로 T1/G7 해소·StageSelect 첫 진입 `NewRun` 로스터 준비가 들어와, 기존 `StageMapEntryFlow` → `CombatEntry.RequestEntry` → 진입 Job(`EnterBattle`) → `CombatLifeCycle` 전환 경로가 그대로 동작함을 확인. 새 `StageMapCombatEntryBootSmokeTests`: 실제 Boot·DB·Core, 시작 노드의 다음 전투 노드를 StageSelection 입력(포인터 누름·뗌)으로 클릭 → Combat 씬, 선택 노드 일치, 전투 준비, 60프레임 오류 없음(첫 이동에 전투 노드가 없으면 Inconclusive). 실행 결과: 노드 2 진입 통과. **발견:** 커밋된 `Boot.unity` 시작 구간이 Combat(0)이다(10/04 Stage(1)로 바꿨으나 현강 `efb04eea` 병합 때 0으로 돌아감). 그래서 Boot smoke들은 테스트 안에서만 이 PC의 Play From Boot 덮어쓰기를 Stage로 켜고 끝나면 원래대로 복원한다(`BootStageStartOverride`, EditorPrefs 백업). `ArtifactPopupBootSmokeTests` 2개도 같은 방식으로 바꿔 통과. 검증(Unity 배치): 진입·아티팩트 smoke·StageMapViewSystem·StageMapInteraction 27개 중 26 통과·1 Inconclusive(기존 `EnterNode_WithoutMainCamera_Throws`, 환경). 사용자 Play 확인과 Boot 기본 시작 구간 결정은 남음 |

## 8. 4·5번 계획 (2026-10-04 작성, 구현 전)

### 8.1 범위와 전제

- **4번:** 노드 오브젝트 클릭(피킹), 호버·선택 강조, 취소. 스테이지 진입은 3D 노드 클릭이다.
- **5번:** 확정한 노드 ID를 `StageViewWriteSink`에 기록하고 중복 확정을 막는다. 기록만으로 진입 성공으로 처리하지 않는다.
- **카메라(D5, 사용자 결정 A안):** 카메라 시스템은 다른 작업자 작업을 기다린다. 그동안 SceneRoot에 참조를 추가하지 않고 피커가 `Camera.main`(StageSelect의 Main Camera는 `MainCamera` 태그)을 쓴다. 카메라는 Attach 때 한 번 잡고(없으면 오류), 레이는 클릭할 때마다 그 카메라의 현재 위치로 만든다. 카메라 시스템이 오면 피커 생성 한 곳만 바꾼다. 그래서 Play 중 카메라를 직접 움직여도 클릭이 맞는다. 코드는 카메라를 움직이지 않는다.
- **UI 클릭(D6):** 맵 위 UI는 팝업(아티팩트, 앞으로 상점·이벤트 가능)이다. 팝업 중 맵 클릭 차단은 **입력 Layer 전환**으로 처리한다. 팝업이 자기 Layer로 바꾸면 `StageSelection` 입력이 들어오지 않는다. 4번 코드에는 UI 위 판정을 넣지 않는다. 팝업이 Layer를 바꾸고 되돌리는 위치는 팝업 작업에서 정한다.
- **진입 가능 표시(D7):** "갈 수 있는 노드" 판단은 Core 규칙이다. 4번은 강조를 표시하지 않는다(Q3). 강조 색을 넣을 때도 호버·현재 위치(`CurrentProgress.CurrentStage`)까지만 View가 표시하고, `UnavailableColor`·진입 가능 표시는 6번(Core 결과)에서 다룬다.

### 8.2 구조 (2026-10-04 결정 반영)

```text
StageMapViewSystem (기존, 연결·순서만)
  ├─ Attach: Camera.main 확보 → 피커·Interaction 생성 → 슬롯 Bind → ChangeLayer(StageSelection)
  └─ Detach: 슬롯 Unbind → 우리 Layer일 때만 ClearLayer → 표시 정리
StageMapInteraction (순수 C#, IStageSelectionInputReceiver)  호버·누름 상태와 클릭 즉시 요청 규칙
  ├─ IStageNodePicker              화면 좌표 → 노드 ID (구현: StageNodePicker, 테스트: 가짜)
  ├─ IStageMapInteractionFeedback  호버 변경·요청 기록 알림 (지금은 에디터 로그만, 이펙트 연결 자리)
  └─ StageViewBuffer Read/Write    처리 전 요청 확인 + 요청 기록 (5번)
```

| 구성 | 책임 | 하지 않는 일 |
|---|---|---|
| `StageMapInteraction` | 호버 노드·누른 노드, 같은 노드에서 뗄 때 요청 기록, 처리 전 요청이 있으면 무시 | 레이캐스트, 색·이펙트 적용, 진입 가능 판단 |
| `IStageNodePicker` / `StageNodePicker` | 카메라 레이 → `NodeMask`·`PickDistance` → 맞은 Collider → 노드 ID | 상태 보관, 카메라 이동 |
| `IStageMapInteractionFeedback` / `StageMapInteractionLog` | 요청 기록 시 에디터 전용 로그. 호버 알림은 받기만 한다(이펙트 연결 자리) | 강조 색, 이펙트 재생 |
| `StageNodeView` (기존) | 생성 때 `HitTarget` Collider → 노드 ID 표를 함께 만든다 | — |

- 노드 프리팹 자식 계약에 `HitTarget`(Collider)을 추가한다. 이름은 `StageMapViewData` 상수, `Validate`에서 검사한다. `Highlight`는 강조 색을 넣을 때 추가한다.
- 중복 요청 차단은 별도 플래그 없이 `StageViewReadSink.Commands`에 처리 전 요청이 있는지로 판단한다(한 사실은 Buffer 한 곳). 처리된 요청 정리(`RemoveCommand`)는 6번이므로, 6번 전까지는 한 방문에 한 번만 기록된다.

### 8.3 결정 (사용자, 2026-10-04)

- **Q1 클릭 = 진입 요청:** 노드를 눌렀다가 같은 노드에서 떼면 바로 진입 요청을 기록한다. 별도 선택 단계·Enter 확정은 없다. 다른 노드에서 떼거나, 누르는 중 우클릭(`Cancel`)·강제 종료가 오면 기록하지 않는다. `Confirm`(Enter) 입력은 지금 쓰지 않는다.
- **Q2 에디터 전용 로그:** 요청을 기록하면 `[StageMap] 진입 요청 기록: 노드 N, 요청 번호 K`를 에디터에서만 남긴다(`UNITY_EDITOR`).
- **Q3 이펙트·강조 색:** 이펙트는 연결 자리(`IStageMapInteractionFeedback`의 호버·요청 알림)만 열어 두고 재생하지 않는다. 강조 색은 넣지 않는다.

### 8.4 Task

- [x] **U1** `StageMapViewData`에 `HitTargetName` 상수와 `Validate` 검사(HitTarget에 Collider, `NodeMask`에 그 Layer 포함) 추가, 설계 §6.3 갱신
- [x] **U2** `StageNodeView`에 Collider → 노드 ID 표, `StageMapViewDisplay.TryGetNodeId`, `StageNodePicker` 작성
- [x] **U3** `StageMapInteraction`(순수 C#)과 `IStageNodePicker`·`IStageMapInteractionFeedback` 계약, `StageMapInteractionLog`
- [x] **U4** `StageMapViewSystem` 연결: 입력 슬롯·Layer·StageView Buffer 받기, Attach/Detach에서 Bind·Layer 전환·정리
- [x] **U5** EditMode 테스트: 같은 노드 누름·뗌 1회 기록, 다른 노드·빈 곳·취소·강제 종료는 기록 없음, 처리 전 요청이 있으면 무시, 호버 변경 알림, Attach 시 Bind·Layer, Detach 시 Unbind·ClearLayer(우리 Layer일 때만)
- [x] **U6** Play 확인: 카메라를 직접 움직이며 노드 클릭 → 에디터 로그 1회, 다시 클릭해도 추가 기록 없음

### 8.5 주의 (병합 시)

- `RuntimeFaultBoundary`의 입력 정리 콜백은 하나만 연결할 수 있고, 전투 브랜치(`491cae4`)에서 `CombatViewSystem.StopInput`이 이미 연결한다. 맵 입력의 오류 시 정리를 경계에 붙여야 하면 병합 때 방식(여러 콜백 허용 또는 공용 정리)을 용근님과 정한다. 4번에서는 Detach·Shutdown 정리만 구현한다.
