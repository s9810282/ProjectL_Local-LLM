# 스테이지 맵(StageSelect) 설계

- 작성자: 호진(이호진)
- 상태: 구조 정렬(SA)·맵 표시·노드 이동(NM)·챕터 배경·Canvas/UI 소유권·캐릭터 표시(SC-1)는 구현됐다(확인 상태는 계획 §1). Stage 실행 계층 Job·Plan 재구성은 **보류**(2026-10-26, §11). 스테이지 맵의 여정 포기는 **Q-A1~Q-A7 결정 대기**(§12). 건물 호버 때 제목·설명 표시는 3판(모든 글을 StageOfferText로)까지 구현 완료, 사용자 확인 문제 없음(§5.7, 계획 §3.18).
- 범위: StageSelect 씬의 View(맵 표시·입력·노드 이동·카메라·배경·Canvas/UI 소유권·덱 버튼·캐릭터·팝업 연결)와 그 실행 계층(Stage LifeCycle·Flow·Plan·Job)의 계약. 상점·회복·덱 편집 화면의 내용, 공통 View 구조, 로비, 검사 후속 정리는 다루지 않는다(§1.2의 문서).
- 계획: [스테이지 맵 계획](../plans/2026-10-10-stage-map-plan-Hojin.md)
- 정리: 2026-10-27 StageSelect 구조 정렬 설계, 스테이지 맵 표시 설계(1~4부), 노드 이동 설계, 챕터 배경 설계, Canvas 통합 설계, UI 참조 소유권 설계, 캐릭터 표시 정리, Stage Job·Plan 재구성 설계(보류 절), 여정 포기 Stage 설계(결정 대기 절)를 이 문서로 합쳤다. 옛 판은 git `10914570`. 2026-11-02 그래픽 작업용 카메라 구도 도구(10-29·10-30 계획 둘), 연결선 메시 전환(10-30), 덱 확인 뒤 출전(10-29)을 §8.1·§5.6·§6.7로 합쳤다. 그 옛 판은 git `3ad97e734`.

## 1 목적과 범위

### 1.1 목적

StageSelect는 한 챕터의 스테이지 맵을 보여 주고, 플레이어가 고른 노드로 들어가게 한다. 게임 규칙(진입 가능 여부·현재 위치·완료·결과)은 Core가 정하고 Buffer에 게시한다. View는 Buffer를 읽어 그리고, 의도는 입력 Buffer에 쓴다. 이 문서는 그 사이의 객체·계약·수명을 정한다.

### 1.2 관련 설계

| 주제 | 문서 |
|---|---|
| 공통 View 구조(System → Scope → Factory → `PopupViewScope`, 화면 입력 조립) | [View 구조 설계](2026-10-13-view-structure-design-Hojin.md) |
| 상점 화면·실패 분류·복구 | [상점·회복 UI 설계](2026-10-06-stage-shop-recovery-ui-design-Hojin.md) |
| 회복 이벤트·요청 소유자 | [회복 설계](2026-10-11-noncombat-event-recovery-design-Hojin.md) |
| 강화 노드(StageKey 1202 전용 외형) | [강화 설계](2026-10-19-enhancement-view-integration-design-Hojin.md) |
| 덱 편집 팝업(E1) | [런 덱 View 연결 설계](2026-10-20-run-deck-view-integration-design.md) |
| 로비(아티팩트 팝업 포함, 출발) | [로비 설계](2026-10-11-lobby-design-Hojin.md) |
| 10-26 검사 후속: 노드 부품 묶음(N-1)·null 허용 걷기(R-b)·작은 정리(C-1~C-3)·상점/회복 "제출 중" | [로비·스테이지 검사 후속 정리 설계](2026-10-26-lobby-stage-review-cleanup-design-Hojin.md) |
| 새 Core 규칙 대응 | [새 Core 대응 설계](2026-10-26-new-core-lobby-stage-response-design-Hojin.md) |

### 1.3 범위 밖

- Core 규칙·계산(진입 판정·현재 위치·완료·Run 종료). 필요하면 Core 전달 문서에 요청한다.
- FieldCamera·Architecture(다른 View 작업자), Framework·GlobalSystem 내부.
- 상점·회복·덱 편집 화면의 카드 구성·문구·규칙.

## 2 지금 구조

### 2.1 흐름

```text
[입력]  PlayerInputActions/StageSelection → UnityInputBridge → InputSystem.Submit
        → StageSelectionInputController → StageSelectionReceiverSlot → StageMapInteraction
[의도]  StageMapInteraction ─Submit─► StageViewBuffer (챕터·노드, 번호 발급)
        ShopUI 버튼 ─Submit─► ShopViewBuffer / 회복 팝업 → RecoveryRequestLifetime
[실행]  StageLifeCycle.OnTick → StageFlow.Tick
        → 전투 노드: CombatEntryService.RequestEntry → Register(Combat, Entry) → CombatPrepareJob → CombatEntryTransitionJob
        → 그 외: JobRegisterSystem.Register(Stage|Shop|Recovery, ProcessCommand) → static Plan → Job
[Core]  기대 번호로 입력 Buffer를 직접 조회 → 규칙 처리 → 처리 번호·결과·진행 Buffer 게시
[표시]  StageMapViewScope.Tick → StageMapEntryObserver(결과 대조) → StageMapNodeMovement(이동·착지)
        → 입력 정리 → 상점·회복 팝업 복원 또는 전투 전환 허용 → 맵 다시 그리기
```

Flow는 등록·확인 순서를, Plan은 Job 조립을, Job은 Core 호출을, Core는 규칙과 결과를, View는 표시와 입력 정리를 맡는다(D1·D2). 판단이 Flow 넷과 Plan에 흩어진 문제는 §11에서 보류 중이다.

### 2.2 객체와 책임

| 객체 | 위치 | 맡는 일 | 맡지 않는 일 |
|---|---|---|---|
| `StageMapViewSystem` | `ViewSystem/StageMap/` | Framework 수명·서비스 바인딩, 씬 연결(Attach 100·Detach -100)마다 Scope 하나 생성·게시·폐기, Root·UI 구성 검증 | 맵 표시·입력·팝업 동작 |
| `StageMapViewScope` | 같음 | 연결 한 번의 표시·입력·카메라·이동·비전투 팝업·선택 화면 수명 조립과 Tick·정리 | 게임 판단, 보간 수식 |
| `StageMapViewReader` | 같음 | `CurrentMapKey`·노드 목록·현재 노드 읽기(선택 전은 시작 노드 `IsStart`) | 진행 값 변경 |
| `StageMapViewDisplay` | 같음 | 노드 → 연결선 → 마커 순서로 그리기, 표시 재사용(`NodeIdentity`) 판단, `SetCurrentNode`, `Clear` | 외형 규칙 |
| `StageNodeView` | 같음 | 노드 트리 생성·`ApplyAppearance`(현재·목적지·완료 숨김 합성)·정리 | 좌표 계산 |
| `StageMapNodeLayout` | 같음 | Depth별 가로 중앙 좌표 계산(§5.1) | 객체 생성 |
| `StageLinkView` | 같음 | 노드 Transform 사이 연결선 | |
| `StageCurrentMarkerView` | 같음 | 캐릭터 이동 Root·표시 보정 Root·모델 복제본 생성·재부착·해제(§7) | 수락 판단 |
| `StageMapInteraction` | 같음 | 포인터 해석, 같은 노드 누름·뗌이면 요청 한 번, 빈 배경 드래그 시작 | 진입 가능 판단 |
| `StageNodePicker` | 같음 | 출력 카메라 레이로 HitTarget Collider 집기(`TryPickNode`) | |
| `StageMapPointerBlocker` | 같음 | EventSystem·GraphicRaycaster로 UI 가림 판정 | |
| `StageMapCameraDrag` | 같음 | CameraCatalog 핸들 대여·반환, 시작~보스 범위의 종방향 드래그 | 렌즈·Priority 변경 |
| `StageMapEntryObserver` | 같음 | 같은 번호·챕터·노드의 결과와 전투 요청 수명 대조, 입력 제거·팝업 복원 | Core 호출 |
| `StageMapNodeMovement` | 같음 | 수락된 한 명령의 이동·착지 소비, Buffer 위치 동기화 조정(§6) | 게임 현재 위치 |
| `StageMapBackgroundDisplay` | `StageMap/Background/` | 챕터 키로 배경 프리팹 조회·생성·교체·정리(§9) | 노드·입력 |
| `StageMapSelectionPresentation` | `StageMap/` | 선택 패널(`UIId.StageSelect`) 열기·닫기, 스테이지 화면 부모 수명, 복귀 예약(§10.2) | UI 이력 원본 |
| `StageMapScreenPresentation` | 같음 | 팝업 하나의 `IsOpen`·알림 관찰, 선택 화면 숨김·복귀 예약 | |
| `ShopPopupController`·`RecoveryPopupController`·`DeckEditPopupController` | `StageMap/Shop`·`Recovery`·`Deck` | 각 팝업 기능. 팝업별 Factory가 만들고 `PopupViewScope`로 수명 연결 | 다른 팝업 |
| `StageLifeCycle` | `LifeCycles/Stage/` | 진입 때 맵 준비 Job 등록·Flow Reset, Tick에서 Flow, -200 이탈 검사, `IStageEntryAttemptRead` 제공, 덱 편집 창 연결 | View 표시 |
| `StageFlow`·`StageNodeEntryFlow`·`ShopFlow`·`RecoveryFlow` | 같음 | 도메인별 등록·확인 대기·정지(§4.4) | Buffer 쓰기 |

### 2.3 씬과 프리팹 구성

`StageMapSceneRoot`는 씬 참조만 가진다: `mapRoot`·`backgroundRoot`(형제)·`mapCamera`(CinemachineVirtualCamera)·`ui`(`StageMapCanvasView`). 개별 화면·정렬 값은 갖지 않는다(§10.3).

```text
StageNode_<id> (코드가 만드는 빈 자리, BoxCollider = 건물을 감싼 클릭 상자)
└─ 건물 인스턴스(런타임, 켜진 메시를 재서 밑면을 buildingHeight에 맞춤)
마커: 노드 루트 → 이동 Root → 표시 보정 Root → Akimbo 복제본(런타임, §7)
```

노드 프리팹(`StageMapNode`)·바닥(`Tile`)·종류 아이콘·완료 봉·부품 묶음 `StageNodeViewParts`는 없다. 바뀐 것: 노드 부품 묶음(2026-10-26 N-1, [검사 후속 정리 설계](2026-10-26-lobby-stage-review-cleanup-design-Hojin.md)) → 건물만(2026-11-01 사용자). 클릭 상자는 켜진 건물 메시를 재서 노드 자리에 붙이고 건물이 보이는 동안만 켠다. 건물이 숨은 현재·완료 노드는 Core가 늘 거절해 잃는 동작이 없다.

Canvas 계층은 §10.1이다.

### 2.4 씬 수명 순서

| 시점 | 순서 | 하는 일 |
|---|---|---|
| 진입 | -200 `BindSceneUI` | `UISystem.BindScene`: 그룹 멤버 확정 → 독립 UIBase 등록 → 바인딩 → 부모 검증 |
| | -190 `BindCameras` | CameraGroup 씬 연결 |
| | 0 LifeCycle Enter | `StageLifeCycle.OnEnter`: 맵 준비 Job 등록(로스터가 없으면 `Register(Stage, NewRun)`, 있으면 `Start`) → `StageFlow.Reset()` |
| | 100 View Attach | `StageMapViewSystem`: Root·UI 검증 → 이전 Scope 폐기 → 새 Scope 게시·Start |
| | 200 `OpenSceneUI` | `GameBootstrap.OpenStageUI` → 글로벌 UI의 `ShowStageSelect`만. 선택 패널은 Scope가 연다(§10.2) |
| 이탈 | -200 Stage 이탈 준비 | `ValidateSceneExit`(§4.5) |
| | -100 View Detach | 맵·상점 입력 Clear, 표시·카메라 핸들·선택 화면 정리 |
| | 0 LifeCycle Exit | 정리 마무리(멱등) |

순서 숫자의 원본은 `RuntimeSystem/ErrorHandling/SceneNodeOrder.cs`와 각 시스템의 상수다.

### 2.5 데이터 원본

| 사실 | 원본(한 곳) | View는 |
|---|---|---|
| 노드 그래프·종류·역할·완료·현재 위치 | Core → Stage 진행·현재 진행 Buffer | 읽기만 |
| 맵 입력·상점 입력 | `StageViewBuffer`·`ShopViewBuffer`(View 소유) | 제출·확인 뒤 삭제 |
| 처리 번호·진입 결과 | Core 쪽 Buffer(`StageCommandResults` 등) | 대조만(D2) |
| 전투 진입 요청 수명 | `CombatEntryService.Current`(전투 실행 계층) | `IStageEntryAttemptRead`로 같은 요청만 읽음 |
| 노드 간격·크기·건물 높이·마커 보정·이동 시간 | View DB `StageMapView_Default`(`StageMapViewData`) | 읽기만 |
| 종류별 건물 | View DB `StageNodeVisual` 표 | 한 번 조회 |
| 챕터별 배경 | View DB `StageMapBackground` 표 | 한 번 조회 |
| UI 등록·열린 이력 | `UISystem` | 서비스로 열고 닫음 |
| Canvas 정렬 | 각 Canvas의 sortingLayerID/sortingOrder | 복제하지 않음 |

View는 InGame DB를 읽지 않고 Core Buffer에 쓰지 않는다.

## 3 결정 기록

### 3.1 구조 정렬 (SA, 사용자 2026-10-10)

| ID | 결정 | 근거·바뀐 것 |
|---|---|---|
| D1 | View → 입력 Buffer → Flow(LifeCycle) → `JobRegisterSystem` → static Plan → Job → Core → Buffer → View로 정렬한다 | Framework 구조 문서 §7의 확장 방향을 프로젝트 표준으로 채택. FW MUST 자체는 아니다 |
| D2 | Core가 실행한 요청의 처리 번호·결과는 Core 쪽 Buffer가 원본이다. View·Job이 대신 기록하지 않는다 | 설계 원칙 §4.1. 2026-10-11 사용자가 H3·H4(P5/P6), H1·H2(P7) Core 선행 구현을 승인했다(외부 합의 아님) |
| D3 | 도메인별 루트: `Stage`(맵 준비·노드 진입)·`Shop`·`Artifact`·`Combat`·`Recovery` | `Recovery`는 회복 작업에서 추가됐다. §11 재구성이 진행되면 `Stage` 하나로 바뀐다(보류) |
| D4 | 도메인마다 입력 Buffer·Flow·Plan·Job을 나눈다. `StageFlow`는 조정만, 판단은 도메인 Flow. 공통 인터페이스는 만들지 않는다 | §11 재구성이 진행되면 바뀐다(보류) |
| D5 | 구조 책임은 비전투 View 구조 개선으로 대체했다. `StageMapViewSystem`이 씬 연결 한 번당 `StageMapViewScope` 하나를 게시하고, 맵 표시·입력·이동·팝업은 Scope가 소유한다 | 2026-10-16 대조. [View 구조 설계](2026-10-13-view-structure-design-Hojin.md). 옛 `ArtifactViewSystem`·`ArtifactPopupSession`은 이력 |
| D6 | UI 연결 U1: UI 버튼이 View 입력 Buffer에 쓰고, 기존 Host/Screen을 없앤다. UI는 View 타입을 참조하지 않는다. `Contracts` 폴더는 만들지 않는다 | 프로젝트 결정. 모든 UI에 대한 MUST가 아니다 |
| D7 | `IViewQuery`에 `ArtifactPopupConfigTableQuery`를 넣고 캐스트를 없앤다 | 아티팩트는 지금 로비 |
| D8 | UI는 Buffer·DB를 매 프레임 폴링하지 않는다. View가 바뀐 때만 넘긴다 | |
| D9 | ESC는 설정 열기 전용. 상점·회복 팝업은 화면 버튼으로만 닫는다 | 2026-10-06 아티팩트 ESC 제거 결정 유지 |
| D10 | 동작은 바꾸지 않는다. 프리뷰 규칙(최신 하나, 번호 다른 응답 무시 등) 유지 | |
| D11 | W1은 전환 전 이력이다. P5·P6에서 맵/상점, P7에서 아티팩트를 새 경로로 전환했다 | 현재 운영 ShopUI와 Flow는 연결돼 있다 |
| D12 | H4는 제출된 모든 맵 진입 시도의 Core 성공/정상 거절을 기록한다. 이벤트 미지원·없는 노드도 정상 거절을 게시한다 | |
| D13 | 상점은 첫 유효 입력에서 Submit 전에 UI가 구매·나가기를 함께 잠근다. 일치 결과를 View가 반영한 뒤에만 해제한다 | 2026-10-27 결정된 [상점·회복 "제출 중" 변경](2026-10-26-lobby-stage-review-cleanup-design-Hojin.md)(제출·잠금을 컨트롤러로)이 이 결정을 바꾼다. 미구현이라 지금 코드는 D13이다 |
| D14 | UI 열림/닫힘은 UI가 선언한 동기 `OpenStateChanged(bool)`를 View가 구독한다. `IsOpen` 폴링을 수명 전환의 원본으로 쓰지 않는다. 버튼은 `UIInputBinding` | |

바뀐 것: 2026-10-03 초안 "StageSelect 선택에서 Combat 진입까지"(`StageMapFlow`·노드 ID만 담은 요청) → `StageNodeEntryFlow`·챕터 키를 담은 `StageViewCommandData`·번호 기반 Job(2026-10-13 대체, T1/G7은 Core `5f2d0381`로 해소).

### 3.2 맵 표시

**노드 종류 아이콘(사용자 2026-10-13)**

2026-11-01 사용자 지시로 폐기(§2.3·§5.3). 아래는 이력이다.

| ID | 결정 |
|---|---|
| U1 | 일반 전투 = 일반 해골, 보스 = 보스 해골, 이벤트 = 물음표, 상점 = 텐트 |
| U2 | 엘리트 구분·엘리트 아이콘은 보류한다 |
| U3 | 아이콘은 건물과 함께 숨긴다. 현재 위치에는 캐릭터만, 떠난 클리어 노드에는 완료 봉만 |
| U4 | 이벤트용 임시 건물과 View 외형 테이블 연결을 포함한다 |
| U5 | 사용자가 Event를 `StageRootDataTable`에 추가했다. 잘못 들어간 상세 SO 참조를 `Stage_Event` 루트 SO(StageKey 1301)로 정정한다 |
| U6 | 구현은 Phase별 승인·리뷰를 거친다 |

바뀐 것: 2D Sprite 아이콘(`IconRoot` SpriteRenderer) → 3D 프리팹 아이콘(2026-10-17, 아래). 임시 건물(`StageVisual_*Placeholder`) → 지금 다섯 SO 모두 `Resources/Map/Environment/BackgroundSceneTest/Bar_ver1_final.prefab`(2026-10-20 `550fc0d9` 에셋).

**3D 종류 아이콘(사용자 2026-10-17)**

2026-11-01 사용자 지시로 폐기(§2.3·§5.3). 아래는 이력이다.

| ID | 결정 |
|---|---|
| U1 | 2D 종류 아이콘을 3D 오브젝트로 바꾼다 |
| U2 | 종류별 프리팹을 넣어 쓸 수 있게 한다 |
| U3 | 쓸 수 있는 RenderSystem은 쓰고 Framework·UI·Camera 규칙을 따른다 |
| U4 | 카메라 쪽을 향하게 표시한다. **바뀐 것: 2026-10-19 사용자 정정 → 모든 아이콘에 공통 고정 Y 회전(`IconYaw`)을 적용한다.** 개별 아이콘이 캐릭터·카메라를 향하는 계산은 폐기했다 |
| U5 | 설계 → Task 계획 → Phase별 구현 순서 |
| D1 | View DB의 `icon`(Sprite)을 선택적 `iconPrefab`으로 교체한다. 2D/3D 혼용 모드는 만들지 않는다 |
| D2 | 첫 지원은 정적 MeshRenderer 프리팹이다. billboard·둥실거림·스킨 애니메이션은 후속 요구 |

**Depth별 가로 중앙 정렬(사용자 2026-10-13):** 같은 Depth의 최소·최대 `WidthIndex` 가운데를 가로 중심으로 쓴다. 빈 번호 간격은 보존하고, DB에 저장하지 않는다(§5.1).

**범례(사용자 2026-10-17):** 오른쪽 위 설정 버튼 아래에 항상 펼친 다섯 줄 범례 + `UI_2_textbox` 나인슬라이스 + 범례 영역의 맵 입력 가림 + 기존 UISystem 수명을 승인했다(§5.5).

### 3.3 노드 이동 (NM, 사용자 2026-10-16)

사용자 결정: **기존 Core 수락 확인 → 목적지 박스 숨김 → 로비처럼 이동 → 도착 후 진입.** 로비 이동 코드 공용화를 포함한다.

| ID | 결정 |
|---|---|
| NM-D1 | 진입 판정은 기존 Core가 한다. 거절이면 이동·목적지 숨김 없이 기존 거절 보고·입력 정리로 끝낸다 |
| NM-D2 | 클릭 시 기존 입력 요청을 제출한다. 새 이동 요청 Buffer·번호·Core 선확정 경로를 만들지 않는다 |
| NM-D3 | 전투 준비 수락과 Job 완료 뒤, 맵에서 시작한 전투 전환만 표시 완료를 기다린다. `MapCommandNumber == 0`인 Bootstrap 직접 진입은 기다리지 않는다 |
| NM-D4 | 실행 계층의 `CombatEntryService`가 표시 소유 lease와 요청별 ticket을 소유한다. 새 서비스 등록·Module·ServiceKey·Group property는 추가하지 않는다. Job은 View나 씬을 검색하지 않는다 |
| NM-D5 | View는 착지 완료를 현재 Scope Tick에서 한 번 소비한다. UI·Delay 종료 콜백에서 요청 제거·팝업 열기·LifeCycle 전환을 실행하지 않는다 |
| NM-D6 | 로비의 이동·설정 두 클래스를 `ViewSystem/Common`의 `ChessPieceMotion`·`ChessPieceMotionSettings`로 공용화한다. `.meta` GUID 보존 |
| NM-D7 | StageMap의 시간·높이 원본은 기존 `MarkerMoveDuration`·`MarkerJumpHeight`다. 로비 값을 복제하지 않는다 |
| NM-D8 | 임시 박스는 기존 외형 인스턴스만 비활성화해 재사용한다. 노드 Root·HitTarget은 끄지 않는다 |
| NM-D9 | Core 수락 이후의 표시 취소는 게임 선택 취소가 아니다. 사용자 이동 취소는 추가하지 않는다. 수락 후 소유자 상실·표시 실패는 공용 오류 경계로 정지한다 |
| NM-D10 | Phase마다 구현·검증·기록·리뷰 뒤 중단한다. 커밋·push는 사용자 |

비교한 대안: ① 이동 후 Core 요청(거절될 노드까지 먼저 이동), ② 수락 관찰만 추가(같은 Tick의 전투 전환을 못 막음), ③ 이동 코드 복제(취소 계약 두 벌).

### 3.4 배경·Canvas·UI 소유권·캐릭터

| ID | 결정 | 날짜·근거 |
|---|---|---|
| — | 배경은 전용 View DB 테이블로 두고, StageSelect 씬의 `Probs`를 1챕터 배경으로 쓴다 | 사용자 2026-10-25. 공통 설정에 목록을 넣는 안은 노드 설정과 챕터 배경이 함께 바뀌어 제외 |
| — | StageSelect 전용 UI의 개별 Canvas를 공통 Canvas로 합친다. `DeckEditUI` 내부 OpenButton·Panel 구조는 유지한다 | 사용자 2026-10-26 |
| Q1 | 덱 편집 중에도 선택 패널·범례를 유지한다 | 사용자 2026-10-26 |
| Q2 | (당시) 공통 Canvas를 평소 글로벌 UI 뒤(order 4), 덱 편집 중 앞(100)으로 바꾼다. **바뀐 것: 덱 열기 버튼이 열기 전에 글로벌 이미지에 가려져 → 덱만 독립 Canvas(order 100)로 복원하고 상태별 정렬 전환을 없앴다**(2026-10-26, §10) | 사용자 2026-10-26 |
| — | 비전투 화면(상점·회복)만 실제 부모 `StageScreenRoot`로 묶고, 선택 패널은 고정 루트로 둔다(형제 부모안) | 사용자 2026-10-26. 공용 UISystem 변경 금지 |
| — | UI 참조는 씬 Root → CanvasView → 화면으로 나누고, 정렬은 Canvas 정적 값이 원본이다 | 사용자 2026-10-26 승인 |
| — | 캐릭터는 로비처럼 기존 `Akimbo.prefab`을 재사용한다. 새 프리팹·전투 규칙 데이터는 쓰지 않는다 | 사용자 2026-10-26(SC-1) |

배경·Canvas·UI 소유권 작업은 사용자 지시로 Codex가 직접 진행했다(역할 예외, 해당 작업 한정).

## 4 실행 계층 — 요청·결과 계약

### 4.1 도메인별 구성

| 도메인 | 입력 Buffer(View 소유) | Flow | 등록 | Plan → Job | 결과(Core 쪽) |
|---|---|---|---|---|---|
| 맵 노드 진입 | `StageViewBuffer`(필수 ChapterKey·NodeId, 미정리 입력 하나) | `StageNodeEntryFlow` | 전투: `CombatEntry.RequestEntry(chapter, node, N)` / 그 외·없는 노드: `Register(Stage, ProcessCommand)` | `CombatPrepareJob` 또는 `StageNodeEntryJob` | H4 번호·결과 + 전투 실행 수명(§4.6). 수락 번호와 Active는 별개 |
| 상점 | `ShopViewBuffer`(구매·나가기) | `ShopFlow` | `Register(Shop, ProcessCommand)` | `ShopPlan` → `ShopCommandJob` | H3 처리 번호·결과, 진행·재화·캐릭터 Buffer |
| 회복 | 회복 요청 소유자 `RecoveryRequestLifetime`(View 그룹) | `RecoveryFlow` | `Register(Recovery, ProcessCommand)` | `RecoveryPlan` → `RecoveryCommandJob` | [회복 설계](2026-10-11-noncombat-event-recovery-design-Hojin.md) |

- 아티팩트 편집(H1·H2, `ArtifactEditFlow`)은 LV5로 로비로 옮겼다. 번호 계약은 그대로다([로비 설계](2026-10-11-lobby-design-Hojin.md)).
- 처리 Job은 기대 번호와 도메인 Core 인터페이스만 받는다(FW-101). 맵의 임시 seed 공급자(`Func<int>`)만 예외다. Run seed 요청 R4를 인수하면 지운다([Core 요청](../handoffs/2026-10-06-stageselect-core-requests-Hojin.md)).
- 공용 파일 변경: `JobRootType.Shop`(루트 = 처리 도메인으로 관례 확장), `JobRegisterSystem`에 Shop Plan 등록(§13).

### 4.2 Core 직접 조회 계약 (H3·H4)

| 계약 | Core의 입력 조회와 실행 |
|---|---|
| H3 `bool ProcessNextShopCommand(int expectedRequestNumber)` | `ShopViewReadSink`의 가장 오래된 미처리 명령을 읽어 구매/완료를 분기 |
| H4 일반 `bool ProcessNextNodeCommand(int expectedRequestNumber, Func<int> nextSeed)` | `StageViewReadSink`에서 챕터·노드를 읽는다. 유효 입력 확인·재진입 잠금 뒤 seed 공급자를 한 번 부르고 현재 번호를 다시 확인 |
| H4 전투 `bool TryProcessNextBattleCommand(int expectedRequestNumber, out ChapterProgressionResult result)` | 같은 맵 입력으로 기존 전투 서비스를 실행. `true`는 준비 수락 또는 정상 거절 게시이며 Active가 아니다 |

- 입력 없음·기대 번호 불일치·이미 처리됨은 `false`이고 상태·번호를 바꾸지 않는다. 같은 도메인의 재진입은 예외다. 정상 true/false 모두 Job 완료, 예외는 공용 오류 경계다.
- `StageViewCommandData.SelectStageNode(int chapterKey, StageNodeId nodeId)`의 ChapterKey는 필수다. View는 실제 표시한 맵의 챕터를 넣고, 표시 전 입력은 거부한다. 다른 챕터의 결과는 그 입력만 정리하고 화면에 적용하지 않는다.
- 번호 있는 `RequestEntry`는 등록 전에 입력 번호·챕터·노드와 요청이 맞는지 검사한다. Core가 실행하지 않고 false를 돌려준 준비 Job은 내부 `Running → Cancelled`로 끝낸다. 번호 0 Bootstrap 진입은 직접 전투 API다.
- 동기 Shop/Combat 서비스 실행을 시작한 뒤에는 수락/정상 거절 결과를 반드시 게시한다. 입력 Clear는 실행 중 거래 롤백이 아니다.

### 4.3 Buffer 계약

| Buffer | 쓰는 쪽 | 읽는 쪽 | 내용 |
|---|---|---|---|
| View 입력 Buffer(`StageView`·`ShopView`) | View·UI(Write Sink) | Flow·Plan·View·Core 처리 서비스 | 명령 목록(번호 발급), `RemoveCommandsThrough`·`Clear` |
| Core 쪽 결과 Buffer | Core만 | View·Flow | 처리 번호·결과 |

- 입력 Buffer에는 "처리됨"을 두지 않는다.
- `Clear`는 대기 입력만 비운다. 발급·처리 번호는 Clear·새 Run·씬 왕복에도 유지하고, Buffer 인스턴스를 다시 만들 때만 함께 초기화한다. 번호를 임의로 0으로 돌리지 않는다.

### 4.4 StageFlow와 확인 규칙

`StageFlow.Tick`: 정지·오류·로딩(`LodingService.IsLoading`)·Runner 바쁨이면 반환 → 확인(노드 진입 → 상점 → 회복) → 등록(노드 진입 → 상점 → 회복, 처음 등록한 하나에서 멈춤).

| 종류 | 기다림을 끝내는 조건 | 기록 누락 |
|---|---|---|
| 비전투 맵·상점 명령 | Core 처리 번호 ≥ N, 또는 입력 목록에 N이 더 이상 없음 | 같은 N이 남은 실제 누락만 원인 한 번 기록 후 그 도메인 Flow 정지. 사라진 입력은 취소로 대기 해제(결과를 위조하지 않음) |
| 전투 | §4.6의 전투 수명 | |

- 유효한 명령의 기록 누락과 입력 수명에 따른 취소를 같은 실패로 처리하지 않는다. Flow는 Buffer에 쓰지 않는다.
- 정지한 도메인은 건너뛰므로 다른 도메인은 계속 등록된다. Job·서비스 예외나 공용 fault는 전체를 멈춘다.
- 맵 클릭은 맵 표시 뒤·맵 선택 Layer일 때만 제출한다.

### 4.5 진입·이탈·오류 수명

| 시점 | 동작 |
|---|---|
| `OnEnter` | 공용 오류·Shutdown이면 거부. Runner idle이면 맵 준비 Job 등록 → `StageFlow.Reset()`(기대 번호·대기·정지·보고 상태 초기화). 준비 Job 중에는 명령 등록을 막는다 |
| -200 이탈 준비 | `ValidateSceneExit`: 수락된 전투 전환(같은 Flow 요청·양수 N·`Current` 참조·수락·준비 Job 완료·Activating·관리자 목적 Combat)이면 Runner가 바빠도 허용. 그 밖 정상 이탈은 Runner idle과 요청이 Created·Cancelled·Rejected인지 확인한다. 거절 전에 Flow를 멈추거나 입력을 지우지 않는다. 통과하면 Flow 정지와 Created·Queued 요청 취소(`StopAndCancelEntry`) |
| -100 View Detach → `OnExit` | 새 제출·등록 차단 → View가 입력·임시 상태 정리 → OnExit 마무리(멱등). 수락돼 전환 중인 전투는 취소하지 않는다 |
| `OnShutdown` | 같은 정리를 반복해도 안전. 다른 구간도 쓰는 Global Runner는 비우지 않는다 |
| 공용 fault | Tick 재호출을 기다리지 않고 공용 정리 콜백(`InGameViewFaultCleanup.StopInput`)에서 새 입력 차단·각 View 정리. `Reset`으로 fault를 해제하지 않는다 |

- 현재 호출 순서: `CombatEntryTransitionJob`의 `BeginActivation` → `Manager.Change(Combat)` → Load 안의 동기 Stage 이탈 준비(-200) → View Detach(-100) → LifeCycle Exit(0).
- `InGameViewFaultCleanup` 하나가 StopInput 콜백의 연결·해제를 맡고(P3.9, 소유자 `InGameViewSystemGroup`), 각 View는 자기 입력·구독·표시를 정리한다. 한쪽 정리가 실패해도 나머지를 실행하고 예외를 모은다. 최초 fault는 바꾸지 않는다.
- `LifeCycleSystemFactory`는 `StageLifeCycle(faults)`로 같은 오류 경계를 넘긴다. Manager는 거절된 Exit의 active를 복구하고, 자식부터 Shutdown한 수명에 다시 Exit하지 않는다.
- 바뀐 것: 빠른 전투 복귀 보류 갈래(`_pendingCombatReturn`) → 전투 이탈 때 요청을 바로 닫으므로 삭제(용근님 브랜치, 2026-10-25 병합).

### 4.6 맵 요청의 끝과 입력 정리

한 번에 미정리 맵 입력은 하나다(`StageMapInteraction`이 제출 즉시 목록을 보고 연타를 거부). 입력 발급은 Buffer, 판단·번호·결과는 Core, 전투 준비·로딩 수명은 `CombatEntryService`, 입력 삭제는 View다.

| 상황 | 대기 종료의 원본 | View 정리 |
|---|---|---|
| 상점·회복 수락, 도달 불가 등 정상 거절 | H4의 N과 결과 | 결과 대조 → N 이하 입력 제거 → 현재 맵 확인 → 거절 보고 또는 착지 뒤 팝업 복원 |
| 미지원·없는 노드·진입 대상 아님 | `StageNodeEntryJob` → Core 정상 거절 | 일치 입력 제거 뒤 같은 챕터면 사유 보고 |
| 전투 수락 | `CombatPrepareJob` → `TryProcessNextBattleCommand` 뒤 `Prepared` → (착지, §6.2) → Activating → Active | 수락만으로 입력을 지우지 않는다. Active 또는 그 요청이 소유한 이탈에서 정리 |
| 전투 정상 거절 | H4 거절 + 요청 Rejected | 입력 제거 뒤 같은 챕터면 한 번 보고 |
| 전투 실행 전 취소 | Created/Queued 취소 또는 Core 미실행 false의 내부 취소. H4 기록 없음 | 같은 요청 참조로 관찰하고 N 폐기. Flow는 Job 배출과 입력 제거를 모두 기다림 |
| Runner 바쁨·로딩·맵 준비 전 | 아직 실행 대상 아님 | 입력 보존, 재제출 없음 |
| 예외·공용 fault | 공용 오류 경계 | 다음 Tick을 기다리지 않고 차단·정리. 가짜 기록·자동 재시도 없음 |

- 취소 관찰 계약: `IStageEntryAttemptRead : IServiceModule`의 `bool TryGetCombatEntry(out int mapCommandNumber, out CombatEntryRequest request)`. `StageLifeCycle`이 제공하고, Flow가 가진 N과 같은 요청 참조만 읽게 한다. View에 Flow·취소 권한을 주지 않는다.
- `StageMapEntryObserver`는 요청의 이름 있는 질문(`IsPrepared`·`IsCancelled`·`IsActive`·`IsRejected`)을 읽는다. 바뀐 것: 단계 값 직접 해석 → 이름 있는 질문(2026-10-27 검사 후속 3번, [용근님 공유](../handoffs/2026-10-27-combat-entry-request-question-share-yonggeun-Hojin.md)).

### 4.7 UI 연결 (U1: D6·D13·D14)

- 상점 UI API: `Render(ShopPopupModel)`·`ShowStatus(string)`·`SetCanSubmit(bool)`·`CompleteSubmission(int requestNumber, bool canSubmit)`·`ResetSubmissionState()`, 제출 오류 알림 `SubmissionFailed(Exception)`. 계약은 UI 소유 `IShopPopupDisplay`다.
- 잠금 규칙 요지: 첫 유효 입력에서 모든 구매·나가기를 즉시 잠그고 한 번 Submit한다. `Render`·`Open`·`SetCanSubmit(true)`는 잠금을 풀지 않는다. View가 현재 연결·대기 번호·문맥에 맞는 결과를 반영한 뒤 `CompleteSubmission(N, …)`을 부른다. 숨김만으로 명령을 취소하지 않는다. `SubmissionFailed`는 uGUI EventSystem이 수신자 예외를 로그로 삼키므로 Controller가 같은 오류 경계에 보고하기 위해 있다. 상세 화면 규격은 [상점 설계](2026-10-06-stage-shop-recovery-ui-design-Hojin.md).
- 동기 수명 알림: 각 화면이 `IsOpen`과 `event Action<bool> OpenStateChanged`를 낸다. 열림 상태가 실제로 바뀔 때 한 번 동기 호출한다. 구독자는 그 안에서 Layer·슬롯을 맞추고 다시 `OpenUI`/`CloseUI`를 부르지 않는다. 버튼 입력용 이벤트가 아니다.
- 외부 전환은 `UISystem.OpenUI/CloseUI/HideAllUI`만 쓴다(FW-008·009). `UIBase`·`UISystem`·`UIInputBinding` 규격은 바꾸지 않는다. Inspector OnClick·AddListener 입력 연결은 쓰지 않는다.

## 5 맵 표시

### 5.1 노드 배치 — Depth별 가로 중앙

```text
center(depth) = (minWidthIndex(depth) + maxWidthIndex(depth)) / 2.0
localX = (WidthIndex - center(Depth)) * WidthSpacing
localY = 0
localZ = Depth * DepthSpacing        (MapRoot 기준 로컬)
```

| 같은 Depth의 WidthIndex | center | localX(간격 4) |
|---|---|---|
| 5 | 5 | 0 |
| 1, 2 | 1.5 | -2, +2 |
| 0, 1, 4 | 2 | -8, -4, +8 |

- `StageMapNodeLayout.CalculateLocalPositions(IReadOnlyList<StageProgressData> nodes, float widthSpacing, float depthSpacing)` → 입력과 같은 길이의 `Vector3[]`. 두 번 순회(O(N)), min/max를 double로 더한다. null은 `ArgumentNullException`, 비유한·0 이하 간격은 `InvalidOperationException`.
- 중심 계산 대상은 시작·보스·현재·완료를 포함한 전체 노드다. 입력 순서로 폭을 정하지 않는다. 정렬·재번호·빈 칸 압축을 하지 않는다.
- 산술 평균(양 끝이 대칭이 안 됨)과 개수 기준 재배치(빈 번호가 사라짐)는 요구와 달라 채택하지 않았다.
- 연결선은 실제 노드 Transform으로 그리고, 카메라 범위는 실제 시작·보스 Transform을 읽는다. 상태 변경만으로 좌표를 다시 계산하지 않는다.

### 5.2 노드 외형 데이터

- `StageNodeVisualData`: `stageKey`(0 = 종류·역할 공통, 양수 = 그 StageKey 전용)·`stageType`·`mapRole`·`visualPrefab`(건물)·`displayName`. 건물 호버 글은 이 표가 아니라 공통 화면 글 표다(§5.7 3판). 전용 외형이 있으면 먼저 쓰고, 없으면 `(StageType, StageMapRole)` 공통 외형을 쓴다. 강화 1202 전용 외형은 [강화 설계](2026-10-19-enhancement-view-integration-design-Hojin.md) Q-E3이다.
- Buffer의 종류·역할로 Query를 한 번 조회해 건물을 넘긴다. 종류 switch·파일명 로딩은 만들지 않는다. 시작 노드(`IsStart`)는 종류 조회를 하지 않는다.
- 공통 설정(`StageMapViewData`): `depthSpacing`·`widthSpacing`·`nodeScale`·`buildingHeight`·연결선·연결/시작/마커 프리팹·마커 보정(§7)·`markerMoveDuration`·`markerJumpHeight`·`pickDistance`. 운영 값은 `StageMapView_Default.asset`이 원본이다.

### 5.3 3D 종류 아이콘 (2026-11-01 폐기)

사용자 지시로 종류 아이콘과 그 표·배치 코드를 지웠다. 옛 계약은 git `678dbafb7`의 이 절에 있다.

### 5.4 표시 상태

`StageNodeView.ApplyAppearance`가 Buffer의 최신 사실과 현재·목적지 문맥을 합성하는 한 곳이다.

| 상태 | 건물 | 클릭 상자 |
|---|---|---|
| 일반 미완료 노드(이동 예정 목적지 포함) | 표시 | 켬 |
| 현재 노드 | 숨김(캐릭터 표시) | 끔 |
| 떠난 완료 노드 | 숨김 | 끔 |
| 시작 노드 | `startVisualPrefab`이 있으면 위 규칙, 없으면 아무것도 없음 | 건물이 있을 때만 |
| 시작 노드가 아닌데 건물 없음 | 표시할 때 `InvalidOperationException`(갈 수 없는 노드가 됨) | — |

- 숨김은 건물 인스턴스의 `SetActive`로 하고 인스턴스는 보관한다. 같은 표시의 재갱신은 새 모델을 만들지 않는다. 건물 프리팹 참조가 바뀌면 `NodeIdentity`가 구조 교체를 알아챈다.
- Clear·교체·Detach·Shutdown·부분 실패는 소유 참조와 세대를 먼저 무효화하고 노드 트리를 비활성화한 뒤 파괴한다. 원본 프리팹·공유 자원은 파괴하지 않는다.

### 5.5 범례

- `StageLegendUI : UIBase`(ID `UIId.StageSelect`, Parallel)는 표시만 한다. Core·Buffer·DB를 읽지 않는다. 지금은 선택 그룹 `StageSelectPanelUI`의 멤버로 열리고 닫힌다(§10.1).
- 내용: 명패 "범례", 아이콘+문구 다섯 줄(일반·엘리트·보스·이벤트·상점 스테이지). 배경 `UI_2_textbox` Sliced(Border 120/100/120/150, Importer 무변경). 행은 프리팹에 저작하고 세로 LayoutGroup으로 정렬한다. 런타임 생성기·Pool·DB 테이블은 없다.
- 엘리트는 설명에만 있다. 새 Core 종류·엘리트 노드를 구현한다는 뜻이 아니다.
- 입력: 배경 Image만 Raycast Target. `ScreenUIPointerBlocker` 경로로 범례 영역에서 시작한 맵 선택·드래그를 막는다. 새 InputAction·Layer·Button.onClick은 없다. 이 가림은 게임별 정책이다.
- 접기·툴팁·필터·애니메이션은 범위 밖이다.
- **11-03 숨김(사용자 "범례 나오지 않게"):** 노드 아이콘을 걷은 뒤라 범례가 맞지 않는다. 프리팹 `StageLegendUI.prefab`의 `Frame`(배경·명패·다섯 행을 모두 품은 자식)만 꺼 둔다. 루트·`StageLegendUI`·그룹 멤버 등록·`StageSelectPanelUI` 검사는 그대로라 그룹이 루트를 켜도 아무것도 보이지 않고, 배경 Raycast도 함께 꺼져 그 자리에서 맵을 누를 수 있다. 다시 보이려면 `Frame`을 켠다. 범례를 아주 없애려면 씬 인스턴스·`initialInstances`·범례 한 개 검사를 같이 걷어야 한다(남은 범례 오브젝트는 UISystem이 따로 등록해 같은 UIId가 겹친다).

### 5.6 연결선 (10-30)

- 연결선 프리팹(`StageMapConnection.prefab`, GUID 유지)은 LineRenderer 대신 원점 중심 1×1 XY Quad의 MeshFilter·MeshRenderer다. `StageLinkView`가 맵 로컬 좌표의 중점·방향·길이·두께를 Transform으로 놓는다. 공유 메시라 MeshSystem 자원을 빌리지 않는다.
- 색 원본은 `StageMapViewData`의 `Line Color`(기본 청록, 유한 값만)다. URP Unlit `_BaseColor`를 ColorSystem 핸들로 넣고, Renderer를 지우기 전에 핸들을 반환한다. 재질 복제·공유 재질 수정은 없다. 불투명이고 뒷면도 보이게 재질 Cull Off, 바닥과 겹치면 `Line Height`로 맞춘다.
- 조립: ColorSystem은 기존 Render 등록 → `StageMapViewSystem` → Services·Scene·Display로 넘긴다(루트 재등록 없음). 표시 동일성 비교에 `LineColor`를 넣었다.
- 색은 DB 생성 때 읽는다. 재생 중 SO를 바꾸면 씬 재진입이 아니라 재생을 다시 시작해야 보인다.

### 5.7 노드 호버 정보 (11-02 설계, 11-03 구현)

요청(11-02 사용자): 건물에 마우스를 올리면 그 스테이지의 제목·설명을 보인다. DontDestroyOnLoad의 `HoverTarget` 아래 `HoverPannel`을 다시 쓴다. 옛 계획 Q3("이펙트는 연결 자리만, 강조 색 없음", 10-04)을 이 절이 바꾼다. 지금 동작은 맨 아래 구현 메모를 따른다.

**설계 때 있던 것(11-02)**

- 호버 판정: `StageMapInteraction.PointerMoved` → 클릭 상자 레이(`StageNodeView.TryPickNode`) → `HoverChanged(이전, 지금)`. UI 가림·배경 끌기·숨은 건물이면 0이 된다. 받는 `StageMapInteractionLog.HoverChanged`는 비어 있다.
- 재사용할 패널: `GlobalUICanvas`(정렬 5)의 `HoverTarget/HoverPannel` = `TextNoticeUI`(`UIId.Hover` 21, GlobalUI 등록, 현강님). 입구는 `IUITextPresentation`의 `ShowHover(이름, 설명)`·`MoveHover(화면 좌표)`·`UnHover()`·`HoverHideVersion`이고, Global Context(`global.UITextPresentation`)로 받는다. 전투 환경 호버(`CombatEnvironmentHover`)가 같은 방식으로 쓴다. 씬 재바인딩 때 UISystem이 Hover를 닫는다. 쓰는 법·수명은 [TextNoticeUI 안내](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/UISystem/Screens/Global/TextNoticeUI.md).
- 문구 원본 후보 둘:
  - View DB 노드 외형 표 `StageNodeVisualData.displayName`(에셋 `DataBaseSO/ViewGroup/StageNodeVisual/Item/NodeVisual_*.asset` 여덟). 지금 값은 영어 임시값('Normal Stage A' 등)이고 화면 어디에도 쓰지 않으며, 비면 프리팹 이름이 된다. 설명 칸은 없다.
  - View DB 공통 화면 글 표 `StageOfferTextData`의 `eventName`·`eventDescription`. **StageKey별**이고 이미 한국어다: 1201 용병 고용소, 1202 훈련소, 1203 상점, 1301~1303 회복. 상점·훈련소·회복 팝업 머리줄이 이 글을 쓴다. 전투 스테이지 칸은 없다. 조회는 `GetForStage(stageKey)`(없으면 `KeyNotFoundException`), StageMap Services에 이미 `OfferTexts`로 들어와 있다.
- **외형 표는 스테이지를 구분하지 못한다(11-02 사용자 지적, 코드로 확인):** 외형 항목은 (StageKey 전용 → 종류·역할 공통)으로 고르는데, 전용 항목의 키가 1·2·3·4라 실제 스테이지 키(1201~1303)와 하나도 맞지 않는다. 그래서 Shop 종류인 용병 고용소(1201)·훈련소(1202)·상점(1203)은 모두 공통 Shop 항목 `NodeVisual_MainCharacterUpgrade`(`_MainCharacterUpgradeStageNode`)로, 회복 셋은 모두 `NodeVisual_RandomEvent`로 그려진다. 외형 항목에 글을 두면 이 셋이 같은 글을 보인다. 전용 항목 키 불일치는 보고만 한다(건물을 시설마다 바꿀지는 따로 정할 일).
  - **11-03 고침(사용자 "건물이 안 맞는다" → "회복을 공통으로", 에셋만):** `NodeVisual_MercenaryRecruitment` 키 1201, `NodeVisual_SupportingCharacterGrowth` 키 1202. 회복 스테이지가 셋(1301~1303)이고 항목 하나는 키를 하나만 가지므로 `NodeVisual_Recovery`를 Event 공통(키 0)으로 두고, 같은 공통 자리를 쓰던 `NodeVisual_RandomEvent`는 표 등록에서 뺐다(에셋은 남김, 참조 없음). 지금 Event 종류 스테이지는 모두 회복이다. 랜덤 이벤트 스테이지가 생기면 다시 정한다. 공통 Shop은 그대로 `NodeVisual_MainCharacterUpgrade`라 전용 항목이 없는 상점(1203, 지금 미등록)은 그 건물로 그려진다.
- 정렬: 호버 패널(5)은 StageSelect 공통 Canvas(4) 위, 증강(98)·덱 창(100) 아래다. 상점·회복은 공통 Canvas 안이라 호버가 남으면 **팝업 위에** 그려진다. 그래서 팝업이 입력을 가져갈 때 꼭 지운다.

**설계**

- **NH1 문구 원본(2판, Q-NH1 A — 11-03 3판이 대신함, 아래 "3판"):** 글은 StageKey 기준이고, 스테이지 종류마다 원본이 하나다.
  - 비전투(Event·Shop): `StageOfferTextData`의 `eventName`·`eventDescription` 그대로. 팝업 머리줄과 호버가 같은 글이라 한 곳만 고치면 둘 다 바뀐다.
  - 전투(Combat): 공통 화면 글 표에 전투 칸이 없으므로 노드 외형 표의 전투 항목(일반·보스 공통)에 쓴다. 이름 = 기존 `displayName`, 설명 = 새 `description`(여러 줄) 한 칸. 특정 전투 스테이지만 다르게 하려면 그 StageKey 전용 항목을 둔다(같은 프리팹을 써도 된다).
  - 고르는 규칙은 `StageType` 하나(Combat → 외형 표, 그 밖 → 공통 화면 글 표)다. 없을 때 다른 쪽으로 넘어가는 대체 규칙은 두지 않는다. 비전투 스테이지 글이 없으면 팝업을 열 때와 같은 구성 오류다.
- **NH2 표시 부품:** 새 `StageNodeHoverInfo : IStageMapInteractionFeedback`가 `StageMapInteractionLog` 자리에 들어간다(진입 요청 로그도 옮김). 노드 ID → Buffer 노드 목록의 `StageProgressData`(StageKey·종류·역할) → NH1 규칙으로 글 조회 → `ShowHover`. 진입 가능 여부는 판단하지 않는다(Core 몫). 시작 노드는 띄우지 않는다(Q-NH3).
- **NH3 좌표:** 피드백 계약에 `HoverMoved(float screenX, float screenY)` 하나를 더한다. 호버 중 포인터가 움직일 때마다 `MoveHover`로 넘긴다. 패널은 입력을 읽지 않는다는 기존 계약 그대로이고, 맵 입력은 순수 C# 그대로다.
- **NH4 지우는 때:** 호버 대상이 0이 될 때, 노드를 누를 때(다른 대상으로 옮겨야 다시 뜸 — 클릭으로 열리는 상점·회복·증강·출전 덱 창이 모두 여기 걸림), 팝업이 입력을 가져갈 때(`StageMapViewScope.SetPopupBlocked(true)`, 지금 빈 함수), 입력 정지·해제(`StopInput`). 씬 전환은 UISystem이 이미 닫는다. 밖에서 닫혔으면(`HoverHideVersion`이 바뀜) 다음 포인터 이동 때 다시 띄운다. 매 프레임 검사는 두지 않는다.
- **NH5 조립:** `StageMapViewSystem.PostBindService`가 `global.UITextPresentation`을 Services로 넘긴다. 루트 재등록·새 UIId·새 프리팹·새 서비스 없음.

**점검:** SSOT — 모든 스테이지 이름·설명은 공통 화면 글 표 한 곳(3판, 찾는 규칙도 표의 `GetNodeText` 한 곳), 열림 상태는 패널(`HoverHideVersion`) 한 곳, 노드 사실은 Buffer, "맵 선택 Layer인가"는 `StageMapViewInput.IsSelectionLayer` 한 곳. SOLID — 판정은 `StageMapInteraction`, 문구 조회·표시는 `StageNodeHoverInfo`, 위치·레이아웃은 `TextNoticeUI`. FW — 기존 GlobalUI 등록·개폐 경로(`IUITextPresentation` → `IUISystemService`)만 쓴다. 독립성 — 문구 변경은 에셋 한 곳. 숨김 규칙은 갈래마다 한 곳이다: 누름·시작 노드·표시 전은 `StageNodeHoverInfo`, UI 가림·배경 끌기·취소는 `StageMapInteraction`, 팝업·설정 창(Layer)은 `StageMapViewInput.ReleaseHoverOutsideSelection`(11-03 묶음 끝 검토 L1로 고침). 바뀌는 파일은 모두 호진 영역이고 현강님 패널·서비스는 고치지 않는다.

**정할 것**

| ID | 질문 | 선택지 | 권장 |
|---|---|---|---|
| Q-NH1 | 글 원본(2판, 1판 "외형 항목마다"는 사용자 지적으로 버림) | A: 비전투는 공통 화면 글 표(이미 있음), 전투는 외형 표 전투 항목(설명 칸 하나 더함) / B: StageKey별 새 표 "스테이지 이름·설명" 하나에 모든 스테이지를 적고 팝업 머리줄도 이 표로 옮긴다(글 한 곳, 공통 화면 글 SO 다섯·팝업 코드도 고침) / C: B인데 팝업 글은 그대로(같은 이름이 두 곳) | A |
| Q-NH2 | 이름 칸 | 답(11-02): 1판의 `displayName`은 노드 외형 표 `NodeVisual_*.asset`의 칸이다. A안이면 이 칸은 전투 항목 둘(`NodeVisual_Combat_Regular` 'Normal Stage A', `NodeVisual_Combat_Boss` 'Boss Stage')만 호버에 쓰이고 이 둘만 한국어로 고친다. 비전투는 이미 한국어인 `eventName`을 쓴다 | — |

결정(11-02 사용자): Q-NH1 A(비전투 = 공통 화면 글 표, 전투 = 외형 표 전투 항목), Q-NH3 A(시작 노드는 띄우지 않음), Q-NH4 A(이번엔 제목·설명만), Q-NH5 A(누르면 숨기고 다른 대상으로 옮겨야 다시 띄움). Task는 [계획 §3.18](../plans/2026-10-10-stage-map-plan-Hojin.md).

**구현 메모(11-03, 묶음 끝 검토 반영까지 Ua·Ub·Uc 문제 없음 — 사용자)**

- 부품: `StageMap/StageNodeHoverInfo`(글 고르기·패널 열고 닫기, 진입 요청 에디터 로그). 옛 `StageMapInteractionLog`는 지웠다.
- 계약 `IStageMapInteractionFeedback` 넷: `HoverChanged(chapterKey, nodeId)`(0 = 없음, 챕터는 표시를 마친 맵 — 진입 요청과 같은 원본)·`HoverMoved(x, y)`·`NodePressed(nodeId)`·`EntryRequested`. 맵 입력 `StageMapInteraction`은 사실만 알리고, 누름·끌기·호버를 내려놓는 `ClearPointer()`를 낸다.
- 글은 대상이 바뀔 때 한 번 찾는다(노드 목록 `GetStages`가 읽기 전용 감싸개를 하나 만들 뿐, 글은 표가 만들어 둔 Data를 읽는다). 이동마다는 `MoveHover`만이고 할당이 없다. 밖에서 닫히면(`HoverHideVersion` 바뀜) 다음 이동 때 다시 열고, 이미 밖에서 닫힌 패널은 `UnHover`하지 않는다(그 사이 다른 사용처가 연 패널일 수 있어서).
- 띄우지 않는 때: 시작 노드, 표시를 마치기 전(챕터 0), 맵을 다시 그리기 직전이라 목록에 없는 노드, 누른 노드(대상이 바뀔 때까지).
- 설계와 다른 점 둘(계획 P-NH1·P-NH3, 사용자 승인):
  - NH4: 팝업·설정 창 때 지우기는 `SetPopupBlocked`가 아니라 `StageMapViewScope.Tick` 끝에서 부르는 `StageMapViewInput.ReleaseHoverOutsideSelection` 한 줄이다. 호버 중이고 입력 Layer가 `Selection`이 아니면 `ClearPointer`. 설정 창은 `OverrideLayer(Settings)`로 Layer만 바꿔 팝업 알림이 오지 않고, 호버 패널(Overlay 5)이 그 위에 남기 때문이다. 호버가 없으면 Layer를 읽지 않는다. `SetPopupBlocked`는 빈 함수 그대로.
  - NH5: 패널 서비스는 Services가 아니라 `StageNodeHoverInfo` 생성자로 바로 넘긴다(`StageMapViewSystem.PostBindService`의 `feedback:` 줄). Services 생성자 무변경.
- 띄우지 않는 때에 하나 더: 누른 채 밖에 나갔다 돌아와 떼도 고르는 순간 다시 숨긴다(맵 입력이 뗄 때도 `NodePressed`를 알림, 검토 L5). `NodePressed`는 그 노드가 지금 호버 대상일 때만 억제한다.
- 오류: 글이 없으면(3판부터는 키 줄도 공통 줄도 없으면) 표가 `KeyNotFoundException`을 던진다. 호버는 입력 콜백에서 불리므로 부품이 잡아 오류 경계(`RuntimeFaultBoundary.Report`, 생성 인자)에 보고하고 그 대상을 버린다(맵 Scope의 다른 입력 경로와 같은 규칙, 검토 M1).
- (2판 때) 전투 `Description`이 비어도 제목만으로 띄웠다. 3판에서는 모든 줄의 설명이 필수다(표 검사).
- 글 넣는 곳은 [스테이지 맵 안내](../viewDocs/stage-map-guide-Hojin.md) §2.1.

**3판 — 모든 스테이지 글을 StageOfferText 한 곳에(11-03 사용자, Ud 문제 없음 — 사용자)**

사용자: "일반 전투·보스 전투의 제목·설명도 StageOfferText로" → 키 목록만으로는 일반 전투 스테이지를 만들 때마다 키를 더해야 해서 → "공통 줄 + 키 전용". NH1(2판)의 "전투 = 노드 외형 표"를 대신한다.

- **NH1′ 글 원본:** 모든 스테이지의 호버 제목·설명은 공통 화면 글 표 `StageOfferText` 한 곳이다.
  - 새 줄 종류 `StageOfferTextTarget.Info`(화면 없는 이름·설명 줄). 필수는 이름·설명뿐이고 팝업 칸(버튼·안내 글)은 보지 않는다.
  - `Info` 줄에 `Stage Keys`가 있으면 그 키 전용, 비어 있으면 그 줄의 종류·역할(`stageType`·`mapRole`) 공통 줄이다. 종류·역할마다 공통 줄은 하나.
  - 찾는 순서(호버, 새 조회 `GetNodeText`): 그 키의 줄(팝업 줄 `Stage` 또는 `Info`) → 종류·역할 공통 `Info` 줄 → 없으면 `KeyNotFoundException`. 노드 외형 표와 같은 "전용 → 공통" 규칙이고, 표 하나 안의 규칙이라 다른 표로 넘어가지 않는다.
  - 팝업(상점·훈련소·회복)은 지금처럼 키의 `Stage` 줄만 읽는다(`GetForStage`). 그 키가 `Info` 줄이면 구성 오류다.
  - 에셋: 공통 줄 둘 `StageOfferText_Combat`(전투·일반 "일반 전투")·`StageOfferText_Boss`(전투·보스 "보스 전투"). 새 일반 전투 스테이지는 등록하지 않아도 공통 줄 글이 나온다. 설명은 임시 문구다.
- **NH2′:** 호버 부품은 스테이지 종류로 나누지 않고 `GetNodeText(StageKey, 종류, 역할)` 하나만 부른다. 노드 외형 표를 받지 않는다.
- **되돌림:** 2판에서 더한 노드 외형 표의 `description` 칸·툴팁·전투 외형 이름 변경, 검토 L3의 `StageMapViewServices.VisualOf`(외형 조회를 호버가 더 쓰지 않음)를 걷어 원래대로. `displayName`은 다시 화면에 쓰이지 않는다.

## 6 노드 이동

### 6.1 책임

| 객체 | 맡는 일 | 맡지 않는 일 |
|---|---|---|
| `StageMapViewScope` | 한 연결의 lease·`StageMapNodeMovement` 생성·Tick·종료 순서 | 보간 구현 |
| `StageMapNodeMovement` | 수락된 한 명령의 표시 진행, 착지 완료 소비, Buffer 기반 즉시 표시와 이동의 조정 | 게임 현재 위치·진입 가능 여부 |
| `ChessPieceMotion` | 예약 문맥, 취소·완료 토큰, 정착 자세. 위치·회전 3구간 보간은 안에서 만드는 `ChessPieceLiftPath`가 낸다(2026-10-29 로비 설계 §15) | 노드·팝업·진입 요청 |
| `StageCurrentMarkerView` | 이동 Root·인스턴스·재부착·자원 해제 | 수락 결과 |
| `StageNodeView` | 현재·완료·목적지 숨김 합성 | 이동 수식 |
| `StageMapEntryObserver` | 결과·전투 수명 대조, 완료 뒤 입력 제거·팝업 복원 | Transform 보간 |
| `CombatEntryService`·request·ticket | 맵 표시 소유 lease와 요청별 표시 대기 | Transform·UI·Core 규칙 |
| `CombatEntryTransitionJob` | 준비 완료와 표시 완료 확인 뒤 전환 한 번 | 표시 생성·완료 게시 |

별도 `StageMapMovementSystem`·범용 Factory는 만들지 않는다. Scope가 내부 협력 객체를 조립한다.

### 6.2 전투 표시 대기 계약

위치는 `RuntimeSystem/LifeCycleSystem/LifeCycles/Combat/`다.

```csharp
public interface IMapCombatPresentationOwnerSource
{
    MapCombatPresentationLease BindMapPresentationOwner();
}
// ICombatEntryService가 위 역할을 함께 제공한다. View에는 이 역할로 좁혀 넘긴다.
public sealed class MapCombatPresentationLease : IDisposable
{
    public bool TryComplete(CombatEntryRequest request);
    public void Dispose();
}
```

- lease는 서비스·자기 identity만 보관한다. 내부 `MapCombatPresentationTicket`은 `Pending / Ready / Abandoned`다. 게임 결과나 단계 값을 ticket에 복제하지 않는다.
- **시작:** Scope는 lease를 입력 슬롯 Bind 전에 받아 게시한다. 양수 맵 번호의 `RequestEntry`는 활성 lease가 있어야 하며, 요청에 lease를 묶은 뒤 `Current`로 두고 `Register(Combat, Entry)`한다. lease가 없으면 등록 전에 상태 오류다. 번호 0(Bootstrap)은 ticket 없이 기존 경로를 쓴다.
- **이동 시작:** observer가 같은 요청의 수락과 `IsPrepared`(수락·준비 Job 완료)를 확인하면 이동을 시작한다. Active를 기다린 뒤 움직이는 순환 대기는 만들지 않는다.
- **착지 전 전환 시도:** 전환 Job은 ticket이 Pending이면 요청에 착지 대기(`IsAwaitingLanding`)를 남기고 완료한다. Runner를 붙들지 않는다.
- **착지:** 마커 재부착·외형 확정까지 끝낸 현재 Scope Tick만 `lease.TryComplete(request)`를 부른다. 서비스는 lease identity·현재 요청 동일성·`IsPrepared`·수락 결과를 다시 확인하고, 착지 대기면 `Register(Combat, EntryTransition)`으로 전환을 다시 건다. 아직 시도 전이면 그 시도가 Ready를 보고 바로 전환한다. 옛 완료는 false로 끝나 새 요청을 풀지 않는다. false 뒤에도 같은 owner·요청·`CanWork`가 유지되면 프로토콜 오류로 정지한다.
- **소유자 상실:** lease 교체·Dispose는 자기 ticket만 Abandoned로 만든다. 착지 대기 중인 요청의 소유권이 사라지면 서비스가 그 자리에서 요청을 `Fail`하고 오류 경계(F1)에 보고한다. 입력 정리(오류 정지·정상 종료 전)와 서비스 종료 중의 해제는 그 정리가 맡는다. 영구 Pending·자동 Ready·가짜 거절·자동 재요청은 금지한다.
- **정상 이탈:** `BeginActivation` 이후의 정상 Stage 이탈은 통과한 gate를 다시 실패시키지 않는다(`Pass`가 owner 참조를 끊음).
- `IsPrepared`이면 대응 입력·Core receipt·처리 번호 게시가 이미 끝났어야 한다. observer는 이때 입력 없음·번호/챕터/노드/Kind 불일치·receipt 부재를 기다리지 않고 프로토콜 오류로 정지한다. 준비 완료 전 미도착은 정상 대기다.
- `CancelEntry`의 Created/Queued 취소 범위는 바꾸지 않는다. Prepared 전투를 View가 취소하거나 되돌리지 않는다. Shutdown은 lease/ticket을 무효화하고 늦은 완료를 막는다.

이 gate는 옛 J5의 "맵 진입 수락 시 같은 Tick 즉시 전환"만 대체한다. 바뀐 것: 전환 Job이 Pending으로 Runner를 점유하고 Abandoned를 만나면 Faulted → 착지 대기를 남기고 끝나며 착지 게시가 `EntryTransition`을 다시 걸고, 소유권 상실은 해제 순간 F1(2026-10-22 용근님 전투 골격 C 단계, `RegisterCombatEntry` 삭제는 10-23 K-b).

### 6.3 맵 이동과 비전투 진입

```csharp
internal bool PresentAccepted(StageViewCommand command, CombatEntryRequest combatRequest);
internal void Tick();
internal void SynchronizeCurrentNode(int currentNodeId);
public void Dispose();
```

식별은 명령의 `(ChapterKey, NodeId, Number)`와 선택적 전투 요청 참조다. `PresentAccepted`는 진행 중이면 false, 같은 문맥의 착지가 확정됐으면 true다. 반복 관찰은 이동을 다시 시작하지 않는다. 비전투는 `combatRequest = null`이다.

| 구간 | 입력·결과 수명 | 표시와 다음 실행 |
|---|---|---|
| 클릭~처리 | 미정리 입력 보존 | 현재 캐릭터·외형 유지 |
| 거절·실행 전 취소 | 결과 대조·입력 제거 | 이동·목적지 숨김 없음 |
| 수락~착지 전 | 같은 입력을 보존해 연타 차단 | 목적지 임시 숨김, 같은 캐릭터 이동. Buffer가 먼저 목적지가 돼도 순간 재배치 금지 |
| 비전투 착지 확정 | observer가 번호 한 번 제거 | `RestoreNoncombat`으로 상점·회복 복원 |
| 전투 착지 확정 | lease Ready 게시, 입력은 Active/Rejected/씬 정리까지 유지 | 전환 Job이 전환 |

- Scope Tick 순서: owner 유효성 → observer 수락 관찰 → 이동 완료 소비 → 표시 동기화 → 팝업 Tick·복원. 외부 호출 뒤마다 `CanWork`와 같은 문맥인지 다시 확인한다. 이동 중 `RestoreNoncombat`·초기 복원·`RequestMapRedraw`가 팝업을 먼저 열거나 캐릭터를 다시 만들지 않는다.
- `SynchronizeCurrentNode`는 이동이 없으면 `Display.SetCurrentNode`에 위임한다. 이동 중에는 수락된 목적지와 Core 현재 노드가 대응하는지 확인하고, 다른 챕터·다른 위치가 나타나면 오류 경계로 보낸다. 정상 재진입은 새 Scope가 Buffer 위치에 바로 그린다.

### 6.4 공용 이동과 좌표

`ChessPieceMotion(IDelayService delay, Transform target, ChessPieceMotionSettings settings, bool useLocalSpace = false)`. `Begin(destination[, facingPosition])`·`IsMoving`·`TryConsumeCompletion`·`Cancel`·`Dispose`. 로비는 world 좌표, StageMap은 이동 Root를 MapRoot 밑으로 옮긴 local 좌표를 쓴다. StageMap은 이 설정 생성자를 그대로 쓴다. 로비는 2026-10-29부터 경로 모양 생성자(`ChessPiecePath`)에 칸마다 뛰는 `LobbyViewHopPath`를 넘긴다([로비 설계 §15](2026-10-11-lobby-design-Hojin.md#15-칸마다-뛰는-이동-hm1hm6-2026-10-29-초안)).

| 소비자 | 높이·시간 원본 | 값 |
|---|---|---|
| 로비 | 로비 View DB `LobbyMotionData`(뛰기 높이·한 칸 시간·회전 시간) | [로비 설계 §15](2026-10-11-lobby-design-Hojin.md) |
| StageMap | `MarkerJumpHeight`·`MarkerMoveDuration` | 0.8, 총 0.6초를 12:35:12(`12/59`·`35/59`·`12/59`)로 나눔. 구간 시간은 저장하지 않음 |

- 검증 원본은 `ChessPieceMotionRules`(유한값·높이 0 이상·양수 시간)다. 로비는 2026-10-29부터 이 규칙을 쓰지 않는다(`LobbyMotionData.Validate`). 두 화면의 설정 원본은 공유하지 않는다.
- 상승 중 이동 방향(MapRoot local XZ)으로 회전하고 마지막 유효 방향을 유지한다. 같은 XZ에서 0 벡터로 LookRotation을 만들지 않는다. 착지 재부착 전후 worldRotation은 연속이어야 한다.
- 캐릭터 Root: 정지 때 노드 루트의 자식(local 위치 0·scale 1). 시작 때 MapRoot 밑으로 재부착하고 표시 위치·회전·크기를 보존한다. 음수·0·비유한 scale이나 shear는 시작 전에 진단한다. 착지 때 도착 노드로 재부착한다. 이동 중 Root는 노드 밖에 있으므로 마커 소유자가 직접 해제한다.

### 6.5 목적지 숨김과 비용

- `StageNodeView.SetPendingDestination(int nodeId)`(0 = 없음). `ApplyAppearance`에서 `isCurrent || isPendingDestination`이면 건물·아이콘·완료 봉을 숨긴다. 노드 자체는 끄지 않는다. `Refresh`도 숨김을 보존한다.
- 착지 확정 때 최신 Buffer의 완료 사실로 이전 노드를 복원하고, 목적지는 현재 숨김으로 이어 간다.
- 비활성화 재사용을 고른 이유는 노드 외형이 이미 보관·복원 수명이기 때문이다. SetActive와 Destroy는 GC와 같은 뜻이 아니므로 GC 0·절감량을 주장하지 않는다.

### 6.6 입력·취소·정리

- 처리 중 요청을 보존하므로 이동·착지 소비 대기 중 새 진입이 제출되지 않는다. 별도 모달 Layer는 만들지 않는다.
- `SelectCanceled`·`CancelRequested`는 누름·드래그 정리만 한다. Core 수락 뒤 이동 취소로 넓히지 않는다. `StopInput`은 영구 정지이므로 일시 차단에 쓰지 않는다.
- Scope Dispose는 owner·이동을 먼저 무효화하고 입력·팝업·카메라·표시를 정리한다(`CleanupErrors`로 나머지 계속). 늦은 Delay Tick은 아무것도 바꾸지 못한다.
- 비전투 이동 중 Detach·교체는 System 정지와 Scope 폐기 상태를 먼저 고정한 뒤 공용 오류 경계에 보고한다. Shutdown·StopInput의 정리 경로는 소유권 상실 오류를 보고하지 않는다(`suppressMovementLoss`).

### 6.7 전투 노드 출전 — 덱 확인 뒤 진입 (10-29)

- 사용자 결정: 전투 노드를 누르면 **화면에서만** 캐릭터가 이동하고, 착지 뒤 덱 창이 열린다. 덱 창의 "출전"을 눌러야 기존 StageView 명령을 한 번 제출한다. 덱 창을 닫으면 실제 진행 위치로 돌아간다. Core·LifeCycle은 바꾸지 않았다(Core는 전투 준비 뒤 덱 편집을 금지해서 제출을 늦췄다).
- 원본: 미제출 목적지·표시 이동은 새 부품 `StageCombatDeparture`(`ChessPieceMotion` 소유), 덱 후보·버전은 `DeckEditDraft`, 덱 서비스 호출은 `DeckEditPopupController`, 실제 위치·진입 가능 여부는 Core·Buffer다. Core 결과가 최종 권위다.
- `StageMapInteraction`의 명령 제출 앞에 선택적 진입 처리 경계를 두고, `StageMapViewEntry`는 같은 명령 번호·챕터·노드의 수락 결과일 때만 기존 착지 lease를 한 번 끝낸다(이동을 두 번 하지 않음, 가짜 번호를 만들지 않음).
- 화면: 전투용 덱 창은 확정 버튼 대신 `startCombatButton`("출전") 하나. 확인 창 `DeckCombatConfirmUI`(`UIId.DeckCombatConfirm`, 부모 `DeckEdit`, Exclusive)는 **후보 수가 덱 칸 수보다 적을 때만** 연다(10-29 사용자, 처음 판의 "변경이 있을 때"를 대신함). 가득 찬 변경 덱은 확인 없이 Confirm 뒤 출전, 변경 없는 부족 덱은 확인 수락 뒤 Confirm 없이 출전한다. 버튼은 `UIInputBinding`.
- 막는 것: 표시 이동 중 다른 노드 요청·일반 덱 열기, 출전 연타(덱 거래·전투 명령 각각 한 번), 다른 Run 덱으로 재평가되면 이전 출전 예약 취소. Core가 진입을 거절하면 마커를 실제 위치로 되돌린다.

## 7 캐릭터 표시 (SC-1)

- `StageMapView_Default.currentMarkerPrefab` = `Prefabs/Characters/Colored/Playerable/Akimbo.prefab`(로비와 같은 원본). UnitVisual·InGame/Core 조회는 하지 않는다.
- 표시 보정은 같은 SO의 `markerOffset`·`markerYaw`·`markerScale`이다. 운영 기본값: offset `(0, 0.12890838, 0)`, yaw 0, scale 1.073429. 옛 래퍼의 모델 배율·발끝 보정(0.00890838)에 노드 바닥 윗면 0.12를 더한 값이다. 로비 배율 2.146858은 복사하지 않는다. 유한 값만, 배율은 양수만 받는다.
- `StageCurrentMarkerView`가 이동 Root → 표시 보정 Root → 모델 복제본을 만들고 재사용·해제한다. 모델 루트는 identity로 정규화한다. 복제본에만 Default Layer·Collider 끔·Animator root motion·animation events 끔을 적용하고 Idle은 유지한다. 원본 프리팹과 로비·전투 사용처는 고치지 않는다.
- 표시 동등성에 보정 값을 넣어, 설정이 바뀌면 오래된 생성물을 재사용하지 않는다. 생성 실패 시 자기가 만든 계층을 정리한다.
- 바뀐 것: 캐릭터 자리 `CurrentMarkerAnchor`(노드 루트와 같은 위치) → 노드 루트(N-1). 화면은 같다.

## 8 카메라와 입력

- 입력 조립: `StageSelectionInputFactory`가 Controller·수신 슬롯·Layer(`StageSelectionInputLayers.Selection`·`Popup`)를 만든다. 같은 슬롯을 Controller와 `ViewGroup.Input.StageSelection`이 참조한다. 조립 원칙은 [View 구조 설계](2026-10-13-view-structure-design-Hojin.md)의 화면 입력 조립이다.
- 팝업 Layer `Popup`은 상점·회복·덱 편집 공통이며 이름은 `"StageMapPopup"`이다(맵 선택 액션 맵을 끄고 공용 맵만 켬). 바뀐 것: `"ShopPopup"` → `"StageMapPopup"`(2026-10-27 C-2, [용근님 공유](../handoffs/2026-10-27-stage-map-popup-layer-share-yonggeun-Hojin.md)).
- 확정 정책: 같은 노드에서 누르고 정상적으로 떼면 요청 한 번. `ConfirmRequested`(Enter)는 아무 동작도 하지 않는다. `Cancel`(오른쪽 버튼)은 누름·드래그·호버 해제다. 호버 중인 건물은 전역 Hover 패널에 제목·설명을 보인다(§5.7).
- 노드 집기는 건물을 감싼 클릭 상자만, Layer 필터 없이 `pickDistance`로 한다. 건물 프리팹의 Collider는 보지 않고 캐릭터에는 Collider가 없다. UI 가림은 `StageMapPointerBlocker`가 판정하고, UI·노드에서 시작한 누름은 배경 드래그로 바뀌지 않는다.
- 카메라: `StageMapCameraDrag`가 CameraCatalog 핸들을 한 번 빌리고(`BindHandle`), 실제 시작·보스 위치와 MapRoot 축으로 범위를 정해(`SetBounds`) 빈 배경 드래그만 위치를 바꾼다. 렌즈·회전·Priority는 바꾸지 않고, Dispose에서 핸들을 반환한다. 조립·검증 기록은 [카메라 Framework 공유](../handoffs/2026-10-11-stage-map-camera-framework-share-Hojin.md).
- 사람이 따라 하는 사용법은 [스테이지 맵 안내](../viewDocs/stage-map-guide-Hojin.md)다.

### 8.1 그래픽 작업용 카메라 구도 도구 (에디터 전용, 10-29·10-30)

- **지움(2026-11-09, 사용자 "이제 필요 없으니 빼도 괜찮아"):** [스테이지 선택 리팩토링](2026-11-09-stage-select-refactor-design-Hojin.md) SR-a에서 도구와 런타임 갈래를 모두 지웠다. 이미 씬에 저장한 맵 카메라 구도는 그대로 쓴다. 아래는 지우기 전 기록이다.
- 쓰는 곳: StageSelect 씬 `StageMapSceneRoot` 인스펙터 "그래픽 작업용 카메라 구도". 잠금을 풀면 월드 Position·Rotation XYZ와 렌즈 Orthographic Size를 고친다. Game 뷰에서는 Alt+왼쪽 끌기 = 화면 평면 이동, Alt+오른쪽 = X·Y 회전, Alt+가운데 = 시선 방향 전진·후진, Alt+휠 = Size 0.9배씩(최소 0.01). Z 회전은 인스펙터에서만.
- 원본: 확정 값은 씬의 가상 카메라 Transform과 `m_Lens.OrthographicSize` 하나다. 작업 값은 `StageMapCameraFramingSettings`가 SessionState에 둔다(Unity를 끄면 사라짐). 재생 중에는 기존 Catalog 핸들로 미리 보고, 확정은 재생 중에도 된다. 재생 중 확정 값은 에디터 전용 `StageMapCameraFramingPending`이 들고 있다가 재생이 끝나면 편집 씬 카메라에 Undo·Dirty로 넣는다(대상 씬이 없으면 Additive로 엶). **씬 저장은 사용자가 한다.** 대상이 없거나 둘이면 오류를 내고 값은 세션에 남긴다.
- 입력: `StageSelection` 액션 맵의 `CameraModifier`(Alt, `initialStateCheck`)·`CameraDolly`(가운데)·`CameraZoom`(휠)과 기존 `Cancel`·포인터를 기존 Controller → 슬롯 → 에디터 전용 수신 장식자로 받는다. 비에디터 빌드는 기존 수신자 그대로다. Alt 조정 입력은 노드 클릭·진입으로 넘기지 않고, Alt 해제·취소·슬롯 해제·Game 뷰 포커스 상실에 끌기를 끝낸다.
- 경계: 조정 중에는 일반 맵 끌기도 작업 Position으로 흡수하고 경계로 되돌리지 않는다. 잠그면 경계를 되살리고, 확정 구도가 재생 직후 경계에 밀리지 않게 일반 이동 경계에 확정 구도(offset 0)를 함께 넣는다. 높이·기울기가 바뀌면 시작·보스 경계를 다시 계산한다. 맵 평면을 향하지 않는 구도는 적용 전에 거절한다.
- 범위 밖: 배경·Core·FieldCamera·Architecture·공용 Catalog. 새 서비스 등록 없음.

## 9 챕터 배경

- 타입: `StageMapBackgroundData`·`StageMapBackgroundDataSO`(`chapterKey`·`backgroundPrefab`만)·`StageMapBackgroundDataTableSO`·`IStageMapBackgroundTableQuery`(`Get(int chapterKey)`·`TryGet`)·`StageMapBackgroundTable`. Data는 읽기 전용이고 `GetData()`마다 새로 만든다.
- 규칙: 챕터 키는 양수·유일. 프리팹은 null/Missing·씬 인스턴스 불가, 루트 활성·위치 0·회전 identity·배율 1. 배치 좌표는 프리팹이 소유하고 DB에 중복 기록하지 않는다. 없는 키의 `Get`은 `KeyNotFoundException`, 중복·잘못된 구성은 `InvalidOperationException`.
- 등록: `ViewDataBaseFactory` → `ViewTableKey`·`ViewDataBase` Query 프로퍼티 → `StageMapViewSystem.InitData`의 `data.ViewQuery`. 루트에 재등록하지 않는다.
- 에셋: `DataBaseSO/ViewGroup/StageMapBackground/`(테이블 + `Item/StageMapBackground_Chapter1`, `ViewGroup.asset` 등록), 프리팹 `Prefabs/StageSelect/Backgrounds/StageMapBackground_Chapter1.prefab`(옛 씬 `Probs`를 구조·Transform·렌더러 설정 그대로 옮김). 씬에는 빈 `BackgroundRoot`만 남는다.
- 표시(`StageMapBackgroundDisplay`, Scope 소유): 맵 데이터 준비 전에는 만들지 않는다. 맵 표시 때 같은 `mapKey`로 조회하고, 키·프리팹·부모가 같으면 인스턴스를 유지한다. 교체 전 검증하고, 실패·재진입이면 새 인스턴스를 정리한다. 노드 재그리기는 배경을 다시 만들지 않는다. Dispose·오류 정지·이탈에서 정리한다(반복 허용, 파괴 전 비활성화).
- 2챕터 이후 배경은 아직 없다. 1챕터 배경으로 대신 등록하지 않고, 없는 챕터는 구성 오류로 경계에 보낸다.

## 10 Canvas와 UI 소유권

### 10.1 계층·등록

```text
StageSelectCanvas      (Canvas/Scaler/Raycaster, Overlay, layer 0 / order 4)
├─ StageLegendUI       (StageSelectPanelUI.initialInstances 멤버)
└─ StageScreenRoot     (StageScreenRootUI, ID StageScreenRoot, Parallel 고정 루트)
   ├─ ShopUI           (ParentID StageScreenRoot)
   └─ RecoveryUI       (ParentID StageScreenRoot)

StageSelectPanel       (StageSelectPanelUI : UIGroup, ID StageSelect, Parallel 고정 루트, Canvas 없음)
└─ DeckEditUI          (독립 Canvas/Scaler/Raycaster, Overlay, layer 0 / order 100)
   ├─ OpenButton       (초기 활성)
   └─ Panel            (초기 비활성)
```

| 대상 | 등록 ID | ParentID | Policy·개폐 |
|---|---|---|---|
| `StageSelectPanelUI` | `StageSelect` | None | Parallel. 선택 화면에 있을 때 `OpenUI(StageSelect, true)`, 떠날 때 `CloseUI` |
| `StageLegendUI` | 개별 등록 없음 | 그룹 소유 | 그룹 멤버. 따로 `OpenUI`하지 않는다 |
| `DeckEditUI` | `DeckEdit` | `StageSelect` | Exclusive. 부모가 열린 동안만 열린다 |
| `StageScreenRootUI` | `StageScreenRoot`(신규 40) | None | Parallel 고정. 비전투 화면의 부모 수명 |
| `ShopUI`·`RecoveryUI` | `Shop`·`Recovery` | `StageScreenRoot` | Exclusive, `PopupViewScope` 개폐 |

- 공통 Canvas와 `StageSelectPanel`은 같은 부모의 형제다. 범례는 다른 가지에 있지만 `initialInstances`의 단일 소유 멤버다(`UIGroup.AddInstance`에 Transform 자식 제약이 없음). `DeckEditUI`는 Transform 자식이지만 멤버가 아니라 독립 등록된다.
- 선택 그룹·범례는 초기 비활성으로 저작해 BindScene 전에 보이지 않게 한다.
- `StageSelect`를 닫으면 UISystem이 `DeckEdit` 자식을 먼저 닫고 그룹을 숨긴다. `DeckEditUI.Hide`가 버튼을 다시 켜도 비활성 부모 아래라 보이지 않는다. 외부에서 버튼을 끄는 임시 처리는 필요 없다.
- 정렬 원본은 각 Canvas의 sortingLayerID/sortingOrder다. 공통·덱 Canvas는 같은 Sorting Layer이고 덱 Order > 공통 Order다. 글로벌 Overlay 루트(5)와의 실제 겹침, World Space `OptionCanvas`(99)와의 관계는 숫자로 통과 처리하지 않고 재생으로 확인한다.
- 같은 Canvas 안의 앞뒤는 형제 순서다. Canvas를 UI 전체 입력 차단기로 쓰지 않는다. 기존 EventSystem을 재사용한다.
- 바뀐 것: 단일 Canvas + 덱 열림 때 공통 Canvas를 4 → 100으로 올리던 `StageMapCanvasOrder` → 독립 덱 Canvas(2026-10-26). `StageMapCanvasOrder.cs`는 사용 종료 뒤 참조가 0이라 2026-10-27 삭제했다(작은 정리 C-3, [공유](../handoffs/2026-10-27-unused-lobby-stage-files-removed-share-Hojin.md)).

### 10.2 표시 정책과 복귀 수명

| 상황 | 선택 패널·범례 | OpenButton | 덱 편집 Panel | 스테이지 화면 |
|---|---|---|---|---|
| 등록·맵 준비 전 | 숨김 | 숨김(부모) | 숨김 | 초기 숨김 |
| 선택 화면 준비 완료 | 표시 | 표시 | 숨김 | 숨김 |
| 덱 편집 열기 | 유지(Q1) | 숨김 | 표시 | 숨김 |
| 덱 편집 닫기/Back | 표시 | 표시 | 숨김 | 숨김 |
| 상점·회복 진입 | 숨김 | 숨김(부모) | 숨김 | 그 화면 표시 |
| 선택 화면으로 정상 복귀 | 표시 | 표시 | 숨김 | 끝난 화면 숨김 |
| 전투 전환·씬 해제·오류 정지 | 숨김 | 숨김 | 숨김 | 기존 정리 |

- `StageMapSelectionPresentation`이 Scope 안에서 스테이지 화면 부모 수명·선택 UI 명령·최초 표시·복귀 예약을 소유한다. 새 서비스로 등록하지 않는다. `StageScreenRootUI`의 Hidden 알림은 취소만 전한다.
- 부모 수명은 안정된 `OpenUI(StageScreenRoot, true)`가 돌아오고 같은 Scope·부모가 유지될 때만 연다. 부모 Hide·씬 해제·오류·Scope 교체는 그 수명을 무효로 하고, 같은 부모를 다시 열어도 옛 결과·예약은 살아나지 않는다.
- 팝업별 `StageMapScreenPresentation`은 `IPopupViewScreen`의 현재 `IsOpen`과 알림을 관찰한다. 열림 알림에서 상태를 다시 읽어 선택 UI를 숨기고 옛 예약을 취소한다. bool 인자를 상태 원본으로 보관하지 않는다.
- 정상 완료의 복귀는 Controller가 명시적으로 허용한다. 내부 불일치·오류 닫힘은 복귀를 요청하지 않는다. 외부 Close/Back·다른 Exclusive 교체는 살아 있는 부모 수명 안의 외부 닫힘이다. `mapChanged`·맵 재그리기는 선택 화면 열기 신호가 아니다.
- 순서: ① UI 등록·바인딩 → 이전 Scope 폐기 → 새 부모 Open → 맵 준비 → `RestoreNoncombat` → 복원 팝업이 없을 때 최초 선택 표시 ② 팝업 열림 알림에서 선택 패널을 같은 호출 안에 숨김 ③ 내부 정상 종료는 표시를 캡처한 뒤 `Scope.Close` 후 복귀 예약, 외부 종료는 숨김 알림에서 예약만 ④ 모든 팝업 Tick의 입력 반환 뒤 조건(Scope·부모 수명·맵 준비·Selection Layer·다른 팝업·새 요청·이동)을 다시 보고 연다 ⑤ 전투는 observer가 `IsPrepared`를 확인한 경계에서 선택 UI와 예약을 정리 ⑥ 해제는 Scope 종료 표시 → 팝업 관찰 해제 → 선택 UI·부모 정리, 서비스 호출마다 이전 Scope인지 다시 확인.
- 채택 근거(2026-10-26 재검토): 단순 false 알림·입력 Layer·완료 결과로는 HideAll 뒤 복귀를 막지 못한다. `RestoreWhenHistoryEmpty`·Exclusive 대체는 글로벌 덱 UI 교체와 고정 표시 정책을 바꾼다. StageSelect 전체를 새 부모 아래에 두면 선택 패널이 Back 이력에 들어간다.

### 10.3 참조 책임

`UI` = `GlobalSystem/UISystem/Screens`, `View` = `InGameSystem/ViewSystem`.

| 소유자 | 책임과 필드 | 포함하지 않는 책임 |
|---|---|---|
| `StageMapSceneRoot` | `mapRoot`·`backgroundRoot`·`mapCamera`·`ui: StageMapCanvasView` | 개별 화면·정렬 값 |
| `UI/Stage/StageMapCanvasView` | `canvas`·`selection: StageSelectPanelUI`·`screens: StageScreenRootUI`·`traitGrowth: TraitGrowthUI`, 공통 Canvas 계층 검사(`ValidateComposition`) | 구매·회복·덱 기능·복귀 판단 |
| `StageSelectPanelUI` | `initialInstances` 범례 소유, `deckEditUI` 참조·독립 자식 검증(같은 씬·OwnerGroup·지정 Canvas) | 덱 내부 카드·규칙 |
| `StageScreenRootUI` | `shopUI`·`recoveryUI` 참조(필수)·독립 자식 검증, Hidden 알림 | Core·Buffer 읽기, 완료 판단 |
| 각 화면 UI | 내부 위젯 참조·표시·입력·내부 구성 검증 | 다른 화면·씬 전체 검증 |
| View System·Scope·Factory | 같은 씬·서비스·카메라 연결 검증, 좁은 의존성 조립·수명 | 화면 내부 배치 |

- CanvasView는 UIId·UI 이력이 없는 구성 컴포넌트(MonoBehaviour)다. Root의 UI 진입점은 조립 때 한 번 읽고, Scope는 화면별 명시적 인자를 받는다. 각 Factory는 좁은 화면 계약(`IShopPopupDisplay`·`IRecoveryPopupDisplay`·`IDeckEditDisplay`)을 받는다.
- 서로 다른 컴포넌트로의 참조 이동은 `FormerlySerializedAs`로 처리하지 않고 fileID 대응표로 명시적으로 연결했다.
- 바뀐 것: 상점·회복 화면의 "선택적 null 계약 유지" → 운영 필수(2026-10-26 R-b, [검사 후속 정리 설계](2026-10-26-lobby-stage-review-cleanup-design-Hojin.md)).

### 10.4 같은 설계의 로비 쪽

같은 날 같은 원칙으로 로비도 나눴다. `LobbySceneRoot`(`lobbyCamera`·`worldRoot`·`ui: LobbyCanvasView`), `UI/Lobby/LobbyCanvasView`(`canvas`·`background: LobbyBackgroundView`·`deckUI: LobbyDeckUI`), `UI/Lobby/LobbyBackgroundView`(`canvas`·`image`, raycastTarget false). 배경 출력 연결은 `LobbyBackgroundView.ConnectOutput(Camera output, float nearClip, float farClip, Func<bool> isCurrent)`가 `IDisposable`(내부 `LobbyBackgroundCameraBinding`)을 돌려주고 `LobbyViewScope`만 소유한다. 상세와 결정은 [로비 설계](2026-10-11-lobby-design-Hojin.md)를 따른다.

<a id="2026-10-30-stageselect-단일-canvas-연결-보정"></a>

### 10.5 단일 Canvas 연결 보정 (10-30)

사용자가 모든 화면을 StageSelectCanvas 아래로 모은 현재 씬을 기준으로 한다. 앞선 독립 덱 Canvas(100)와 selection 형제 배치 설명은 과거 이력이다.

- StageMapCanvasView는 공통 Canvas 하나와 selection·screens·traitGrowth를 필수로 받는다. 삭제된 deckCanvas 참조와 두 Canvas 사이 Order 검사는 제거했다.
- StageSelectPanel과 StageScreenRoot는 공통 Canvas 바로 아래, TraitGrowthUI는 마지막 자식이다. DeckEditUI는 StageSelectPanel의 실제 자식이며 공통 Canvas를 사용한다. 범례의 initialInstances 소유권과 각 UIId·ParentID·Policy는 유지한다.
- StageSelectPanel을 전체 Stretch·오프셋 0으로 복구했다. 덱 루트는 전체 Stretch·배율 1이며 삭제된 Canvas/Scaler를 가리키는 씬 참조·override를 제거했다. 기존 버튼·카드 디자인은 유지한다.
- StageDeckEditUISetup도 공통 Canvas 아래에 Canvas 없는 덱 프리팹을 생성·연결한다. 구성의 원본은 씬 계층이며 별도 정렬 서비스나 UI 등록 경로를 추가하지 않는다(SSOT·SOLID·FW 계약 유지).
- 정적 확인: 씬 로컬 참조 전체와 UI 프리팹 5개의 override/stripped 참조가 유효하고 중첩 Canvas가 없다. Unity 컴파일·재생은 미확인이다.

### 10.6 덱 상세 패널 정렬용 Canvas (11-09)

- `StageSelectCanvas`는 공통 루트 Overlay Canvas이며 같은 오브젝트에 CanvasScaler와 GraphicRaycaster가 필요하다.
- `DeckEditUI`는 공통 Canvas를 사용한다. 별도 정렬이 필요한 씬에서는 `Panel` 안에서 기존 `slotInfoPanel` 하나만 직접 감싸는 자식 Canvas를 한 개 둘 수 있다. 이 Canvas에는 Override Sorting과 GraphicRaycaster가 필요하다. 현재 StageSelect 씬의 상세 패널 Canvas Order는 100이며 값의 원본은 씬이다.
- `DeckEditUI.ValidateChildCanvases()`는 덱 상세 패널의 소유 관계와 설정을 확인한다. 씬 연결과 덱 배치 메뉴가 같은 검사를 호출하며, `StageMapCanvasView`는 덱 영역 밖의 중첩 Canvas도 검사한다.
- `UIId.DeckSlotInfo`는 기존대로 `UIId.DeckEdit` 아래에 Parallel 정책으로 등록한다. 상세 패널의 열기·닫기와 바인딩은 기존 UISystem 경로를 사용하며, 정렬용 Canvas의 수명은 덱 Panel 계층을 따른다.

## 11 보류 — Stage 실행 계층 Job·Plan 재구성

### 11.1 보류와 재개

- **보류(2026-10-26 사용자): 코드 착수 전에 멈췄다. 코드는 바꾸지 않았다.**
- 이유: 지금 동작에 오동작이 없다. 고치면 다른 작업자 세 분(민형님·천명님·용근님)의 줄과 천명님 시험에 닿아 비용이 크다. 이득은 Stage 실행 계층을 다시 고칠 때 난다.
- **다시 시작할 조건:** Stage 실행 계층(`StageLifeCycle`·Flow·Plan·Job)을 건드리는 다음 기능이 올 때, 그 기능보다 먼저 한다. 예: 새 노드 종류, 상점·회복 규칙 변경, 맵 진입 흐름 변경.
- **다시 시작할 때 할 일:**
  - 결정(Q-S1~Q-S6, P1~P10, 계획 SP13)은 그대로 쓴다.
  - 고칠 파일·줄·주인을 `git blame`으로 다시 대조한다(옛 계획 §0.4·§2.1, 계획 문서의 보류 절).
  - 용근님 전투 진입 계약(`RequestEntry`는 부르는 쪽이 Runner 빔·로딩 아님을 확인, 이름 있는 질문)이 그대로인지 확인한다.
  - 계획 SP1~SP12, 천명님 시험 둘 삭제·`ChapterGraphPreparationTests` 도우미 수정은 사용자 확인이 남아 있다.
- **보류 동안 남는 위험:** 상점 다음 입력을 고르는 식이 Flow(`ShopFlow`)·Plan(`ShopPlan`) 두 곳에 있다(S-2). 회복은 Plan·Flow·Job이 서로 단계를 확인한다(S-4). 이 부분을 고칠 일이 생기면 두 곳을 함께 고친다.
- 착수 조건이던 용근님 브랜치 병합은 2026-10-25 `130b33085`로 풀렸다. 그 뒤 사실 대조에서 바뀐 것은 없었다.
- 이 재구성이 진행되면 §3.1 D3·D4와 §4.5의 -200 이탈 검사를 대신한다. 그때 그 줄에 "대신함"을 단다.

### 11.2 구조 요지

- Flow 넷(`StageFlow`·`StageNodeEntryFlow`·`ShopFlow`·`RecoveryFlow`)을 없앤다. 판단은 `StagePlan.Decide(사실)` 하나, 이탈 조건은 `StagePlan.CanLeave(사실)` 하나, Core 호출 결과 확인은 호출한 Job 안으로 옮긴다.
- `StageLifeCycle`은 트리거만 한다: 진입 때 방문 시작, Runner가 비면 `StagePlan.TryGetCombatEntry`의 답으로 `CombatEntry.RequestEntry` 또는 `Register(Stage, Next)`(Q-S1 E), -200에서 `CanLeave` 검사와 방문 끝냄 표시. 요청 단계를 바꾸거나 취소하지 않는다(P8).
- 새 LifeCycle 그룹 서비스 `StageProgress`(`IStageProgressReader`)가 사실 묶음 `StageProgressFacts`(readonly struct)를 읽는다. `IStageDeckEditFacts`·`IStageEntryAttemptRead` 구현도 낸다(View·덱 창 무변경).
- 새 `StageVisit`가 이번 방문의 번호·살아 있음·맵 준비 완료·맵 정지·상점 정지를 갖는다. 그룹에는 Stage Job을 만드는 입구 `IStageVisitJobs`만 낸다. Stage Job은 실행하자마자 방문이 살아 있는지 확인하고 아니면 Core를 부르지 않고 끝난다(Q-S3 C).
- 입구는 `Register(Stage, Next)` 하나다. `Shop`·`Recovery` 루트와 Stage `Start`·`NewRun`·`ProcessCommand` 직접 등록은 없앤다(Q-S2). `JobType.NewRun`은 남긴다.
- 판단 표 `Decide`(위에서 처음 맞는 줄): 0 로딩 → 없음 / 1a 착지 대기 → 없음 / 1b 전투 요청 미종료 → 어긋남(예외) / 2 맵 준비 안 됨 → 맵 준비(로스터 없으면 새 Run 뒤 첫 챕터) / 3a~3d 미처리 맵 입력 N(취소면 대기, 끝난 단계면 어긋남, 전투 노드면 `TryGetCombatEntry`, 그 밖 맵 노드 N) / 4 맨 앞 상점 입력 N / 5 회복 예약 가능 / 6 없음.
- 지킬 약속 S1~S7: 착지 뒤 전환, 방문당 맵 준비 한 번, 기록 누락은 그 도메인만 정지, 회복 기록 보존, 덱 창 거절 규칙 그대로, `IStageEntryAttemptRead` 그대로, 처리 순서 맵 → 상점 → 회복.
- 바뀌는 동작 B1~B12(맵 준비가 한 프레임 늦을 수 있음 등)와 시험 영향·재생 확인 목록 1~11은 옛 설계(git `10914570`) §4·§7.2·§9에 있다. 예상 점수 6.5 → 8.9.
- Phase 안: SJ-1(새 객체·순수 함수) → SJ-2(Job 넷) → SJ-3(바깥 계약 출처) → SJ-4(LifeCycle 전환·Flow 삭제) → SJ-5(문서·공유).

### 11.3 결정 (2026-10-25 사용자)

| ID | 결정 | 경위 |
|---|---|---|
| Q-S1 | **E**: LifeCycle이 Plan의 순수 함수 `TryGetCombatEntry`에 답을 받아, 있으면 `RequestEntry`, 없으면 `Register(Stage, Next)`. 용근님 코드 무변경 | A 결정 → 규격 재점검에서 D(Plan이 상태를 바꾸는 위반을 피함) → 용근님 브랜치(`RequestEntry` = 부르는 쪽이 확인하는 트리거) 대조 뒤 E |
| Q-S2 | **A**: 직접 등록 입구를 없애고 `Next`만 받는다. 깨지는 다른 사람 시험은 최소로 고친다(민형님 `JobSystemTests` 세 줄, 천명님 `ChapterGraphPreparationTests`) | |
| Q-S3 | **C**: 직접 이탈의 Runner 검사를 하지 않고, 방문 번호로 Job이 스스로 끝낸다 | 권장 B(전역 Runner 사용, R5 예외) → 재점검 뒤 C |
| Q-S4 | **D**: 덱 편집 창 `HasPendingFlowCheck` 멤버를 남기고 `StageProgress`가 "맵 전투 진입 진행 중(`Kind == Map`이고 `!IsReplaceable`)"으로 값을 낸다. 용근님 파일 무변경 | A → D(착지 대기 동안 Runner가 비어 A로는 못 막음) |
| Q-S5 | **A**: 우리 호출의 기록 누락은 그 도메인만 멈추고 오류 로그 | |
| Q-S6 | **A**: 회복 예약 → 시작 → 호출 → 통지를 Job 안에서 차례로. 소유자 코드 무변경 | |
| P1 | 방문 상태를 바꾸는 손잡이는 인터페이스에 내지 않는다 | |
| P2 | 챕터 키는 Plan이 정한다(새 Run이면 `StagePlan.FirstChapterKey`, 그 밖은 현재 챕터, 없으면 같은 상수). 로드아웃 키는 `StagePlan.DefaultLoadoutKey` 한 곳 | |
| P3 | 새 Run 결과 대조를 로비 `LobbyRunStartJob`에서 공유 함수로 빼고 Stage `NewRunJob`도 쓴다 | |
| P4 | (삭제) Stage Job을 class로 바꾸는 안. 구조 문서 §4의 struct 형태를 유지한다 | §12.3 수정안 4 |
| P5 | 할 일이 없으면 사실 읽기에 할당이 없다. 노드 목록은 미처리 맵 입력이 있을 때만 읽는다 | |
| P6 | `IStageEntryAttemptRead` 서명은 그대로, 구현만 옮긴다 | |
| P7 | `IStageDeckEditFacts` 구현은 `StageProgress`, `Bind`/`Unbind`는 `StageLifeCycle` | |
| P8 | (교체) LifeCycle은 이탈 때 요청을 취소하지 않는다. 맵·상점 입력은 View Detach가 비우고, 회복 대기 요청은 같은 자리에서 소유자 `CancelPending`을 부른다(`IRecoveryRequestSubmitter`에 취소 추가, `IRecoveryRequestExecution`에서 뺌). 전투 요청은 준비 Job이 입력 소멸을 보고 스스로 취소 | §12.3 수정안 3, 로비 B7 |
| P9 | 다른 시스템의 계약 위반은 throw, 우리 호출의 기록 누락은 도메인 정지 | |
| P10 | 시드 공급은 `JobRegisterSystem`이 한 번 만들어 Plan을 거쳐 Job에 넘긴다(민형님 파일이라 이름·위치 유지) | |
| SP13 | 다른 작업자 줄은 "연결만" 안을 따져 본 뒤 원래 설계대로 고친다 | 계획 결정 |

범위 밖으로 둔 것: 덱 확정·개발 도구의 동기 Core 호출(천명님 계약), View 구조(Scope 비대 등), 회복 소유자의 예약·시작 합치기(Q-S6 B). 보고만: `StageUI.cs`(`UIId.Stage`)는 씬·프리팹·에셋 참조가 0이다(지우지 않음).

## 12 결정 대기 — 여정 포기(스테이지 맵·비전투 스테이지)

### 12.1 상태와 막힌 곳

- 설계 1판(2026-10-26), 코드 변경 없음. **Q-A1~Q-A7 사용자 결정 대기.**
- 요청: 로비를 지나 스테이지 맵부터 설정(Option)에 "여정 포기"가 있다. 전투에서 누르면 Run이 끝나고 로비로 간다. 스테이지 맵·상점·회복에서도 Run을 끝내고 로비로 가게 한다.
- **막힌 곳:** 전투 밖에서 Run을 끝내는 Core 공개 입구가 없다. Run 종료 영수증을 설치하는 길은 전투 완료와 최종 챕터 완료뿐이다. 요청은 [Core 전달 문서 §27](../handoffs/2026-09-24-view-core-data-boundary-handoff.md#27-stageselect의-여정-포기용-run-종료-api-요청-2026-10-19-view--core-미전달)(미전달)이다.
- 이어받는 문서: 현강님 [옵션 이동·여정 포기 계획](../plans/2026-10-19-option-navigation-and-abandon-plan.md) Phase 3, §11(보류)의 재개 조건(Q-A2).
- 지금: `OptionUI`(현강님)는 Y에서 전투 View의 `ICombatAbandonRequestService.TryRequestAbandon()`을 부른다(`OptionUI.cs:116`). 그래서 스테이지 맵·로비에서는 거절되고 아무 일도 없다. 확인창 문구는 "현재 전투를 포기하시겠습니까?"다(`ConfirmUI.cs:95`).

### 12.2 설계 요지

| 지금 구간 | 여정 포기 Y를 누르면 |
|---|---|
| 전투 | 지금 그대로. 남은 연출 → 전투 포기 → 결과 화면 확인 → 로비 |
| 스테이지 맵 | Run 종료 → 로비 |
| 상점·회복 팝업이 열린 중 | Run 종료 → 로비(방문 정리는 Core) |
| 로비·전환 중·LifeCycle 없음 | 거절(Option 유지) |

아래 D1~D10은 여정 포기 설계 1판의 번호다(§3.1의 SA D1~D14와 다르다).

- **D1** 입구 하나: LifeCycle 그룹 `IRunAbandonRequestService.TryRequestAbandon()`. 라우터 `RunAbandonRequests`가 관리자 `CurrentKey`로 대상을 고른다(Combat → `CombatRunAbandon` → 전투 View 입구 / Stage → `StageLifeCycle` 명시적 구현 → `StageRunAbandon` / 그 밖 false). 대상 표는 Factory가 한 번 넘긴다. 새 폴더 `RuntimeSystem/LifeCycleSystem/Run/`. Option은 `LifeCycle.RunAbandon`만 받아 전투 View 형식 의존이 빠진다.
- **D2** 전투 대상 `LifeCycles/Combat/CombatRunAbandon.cs`(새, 연결만)는 전투 View 입구를 그대로 부른다.
- **D3** Stage 대상 `LifeCycles/Stage/StageRunAbandon.cs`: 받는 조건은 함수 하나(오류·종료·로딩 아님, Stage 활성·맵 준비, Run 진행 중, 살아 있는 전투 진입 요청 없음, 아직 요청 안 함). Runner가 바빠도 요청만 세운다.
- **D4** `StageFlow.Tick`의 확인 뒤, 등록 맨 앞에 포기를 넣는다: 포기 요청이 있으면 `Register(Stage, JobType.RunAbandon)` 한 번 → `StageRunAbandonJob`(Core Run 포기 + 종료 확인, 아니면 F1)·`StageRunEndTransitionJob`(`Change(RunDestination.Of(결과))`)을 같은 Runner Tick에 실행.
- **D5** 이탈 검사에 "Run이 끝났고 목적지가 `RunDestination.Of(그 결과)`이면 Runner가 바빠도 허용" 한 갈래.
- **D6** 목적지 원본 `LifeCycleSystem/Run/RunDestination.Of(RunOutcome)`. 지금 `CombatPlan.DestinationOf` 한 곳을 공용 규칙으로 옮긴다.
- **D7** Core 요청(§27 고쳐 쓰기): 전투 없음·Run 진행 중·챕터 진입 상태에서 Run 종료 영수증 설치, `LastSummary` 생성, 열린 비전투 방문 정리. 거절이면 아무것도 바꾸지 않는다. 계약 스냅샷 불변식과의 관계는 Core가 정한다. View·LifeCycle은 가짜 결과로 Run 종료를 대신하지 않는다.
- **D8** 확인창 문구를 구간 무관 문구로(Q-A4). **D9** 덱 편집 창 사실에 "포기 요청됨"과 막는 이유 하나. **D10** 비전투 이동 연출 중 포기: 지금은 착지 전 이탈이 F1이다(Option이 열린 동안 `Time.timeScale = 0`). Run이 끝났으면 맵 View 해제가 착지 전 이탈을 정상으로 본다(`suppressMovementLoss`에 "Run 끝남" 추가, Q-A7).
- Phase 안: RA-0 §27 고쳐 쓰기 → RA-1 입구 하나(+Q-A4·Q-A6) → RA-2 목적지 원본 → RA-3 Stage 포기(Core API 뒤) → RA-4 공유·기록.

### 12.3 정할 것

| ID | 질문 | 선택지 | 권장 |
|---|---|---|---|
| Q-A1 | Core Run 포기 API를 누가 만드나 | A: §27을 D7대로 고쳐 천명님께 전달하고 기다린다. RA-1·RA-2는 먼저, RA-3은 API 뒤 / B: 사용자 지시로 이 세션이 Core 입구를 만든다(영수증 키·불변식을 우리가 정하게 됨) | A |
| Q-A2 | Stage 재구성 보류의 재개 조건 | A: 재구성을 먼저 하고 포기를 `Decide`·`CanLeave` 한 줄씩으로 / B: 지금 구조에 붙이되 재구성 모양(조건 함수 하나·등록 자리 하나·이탈 한 갈래)에 맞추고, 재구성 설계에 "여정 포기를 흡수할 것"을 적는다 | B(재구성이 겨냥한 S-2·S-4 위험에 닿지 않고 Flow에 한 갈래만 더함) |
| Q-A3 | "Run 끝 → 로비" 원본 | A: `RunDestination.Of`로 옮기고 전투 Plan이 부른다(용근님 줄 두 줄, 동작 같음) / B: Stage가 로비 상수를 쓴다(규칙 두 곳) | A |
| Q-A4 | 확인창 문구 | A: "여정을 포기하시겠습니까?" 하나(현강님 줄 한 줄) / B: 그대로 | A |
| Q-A5 | Stage 포기 뒤 결과 화면 | A: 바로 로비(확인은 Option 확인창이 이미 함) / B: 전투처럼 결과 화면(새 UI, 현강님) | A |
| Q-A6 | 현강님 `OptionUITests` 가짜 `AbandonRequest`의 인터페이스 이름 한 줄을 고쳐도 되나 | 단언·필드 이름 `_abandon` 그대로 | — |
| Q-A7 | 비전투 노드 이동 중의 포기 | A: 받는다. 맵 View 해제가 Run이 끝났으면 착지 전 이탈을 오류로 보지 않는다 / B: 착지까지 포기 등록을 미룬다(새 좁은 계약, 팝업이 잠깐 열림) / C: 이동 중 거절(Option이 열려 연출이 멈춰 늘 거절됨) | A |

설계에서 정한 것(확인 대상): P1 로비에서는 거절, P2 전투 노드로 가는 중에는 거절, P3 포기 요청 뒤 View 입력은 막지 않고 등록만 안 함, P4 Core 포기와 로비 전환은 같은 Runner Tick의 두 Job(끝난 Run을 View가 그리는 순간이 없음), P5 Core 포기 결과가 예상 밖이면 예외(F1), P6 `StagePlan.Register`의 LifeCycle 그룹 인자는 끝 선택 인자(`RunAbandon` 갈래에서만 필수), P7 라우터는 대상 표에 없는 구간이면 false.

바뀔 파일과 주인: 새 `LifeCycleSystem/Run/*` 셋·`StageRunAbandon`·Stage Job 둘·`CombatRunAbandon`, 호진 `StageLifeCycle`·`StageFlow`·`StagePlan`·`StageDeckEditWindow`·`IStageDeckEditFacts`·`StageMapViewSystem`, 용근 `CombatPlan.cs:257-258`, 민형 `LifeCycleSystemFactory`·`LifeCycleSystemGroup`·`LifeCycleSystemServiceKey`·`JobRootType`(`JobType.RunAbandon`)·`JobRegisterSystem`, 현강 `OptionUI.cs`·`ConfirmUI.cs:95`·`OptionUITests.cs:383`, 천명 Core Run 포기 서비스.

## 13 다른 작업자 영향

| 파일·영역 | 소유 | 바꾼 것 | 기록·공유(전달 여부) |
|---|---|---|---|
| `JobRootType.cs`(`JobRootType.Shop`)·`JobRegisterSystem.cs`(Shop Plan 등록) | 민형 | SA P4 | [StageSelect Framework 공유](../handoffs/2026-10-10-stageselect-framework-share-Hojin.md)(§10 원본 문서 갱신 요청 포함), 미전달 |
| `BufferSystemGroup`·`BufferSystemFactory`·`DataBaseGroupServiceKey` | Buffer 공용 | `ShopViewBuffer` 노출 | [Core 전달 문서](../handoffs/2026-09-24-view-core-data-boundary-handoff.md) §5.15, 미전달 |
| `StagePlan.cs` | Core가 D2로 고친 파일 | `ProcessCommand` 분기, `CreateShopJob` 제거 | Core 전달 문서 §5.14 |
| Core 상점·맵/전투 진입 처리, 결과 Buffer | 천명 | 사용자 승인으로 H3·H4 facade·결과 Store 구현(규칙 본문 유지) | [Core 요청](../handoffs/2026-10-06-stageselect-core-requests-Hojin.md), [Core 교체 인계](../handoffs/2026-10-11-stageselect-core-replacement-handoff-Hojin.md), 외부 미합의 |
| `InGameViewSystemFactory`·`InGameViewSystemGroup`·`InGameViewFaultCleanup`·`CombatViewSystem` | View 공용·전투 View | P3.9 단일 공용 콜백 소유를 그룹으로 | [공용 오류 정리 기록](../handoffs/2026-10-06-artifact-popup-fault-cleanup-coordination-Hojin.md), 미전달 |
| `CombatEntryService`·`CombatEntryRequest`·전환 Job·LifeCycle 그룹·ServiceKeys | 용근(전투 실행 계층) | 맵 번호 전달, `IStageEntryAttemptRead` 등록, NM lease/ticket·착지 gate | [노드 이동 Framework·전투 공유](../handoffs/2026-10-16-stage-map-node-movement-framework-combat-share-Hojin.md), 미전달 |
| `CombatEntryRequest.IsPrepared` | 용근 | 이름 있는 질문 하나 추가(2026-10-27) | [용근님 공유](../handoffs/2026-10-27-combat-entry-request-question-share-yonggeun-Hojin.md), 미전달 |
| `StageMapViewDataSO.cs` 노드 강조 색·`nodeMask` | 용근(`39b634fe`) | N-1에서 제거 | [노드 설정 공유](../handoffs/2026-10-26-stage-node-settings-share-yonggeun-Hojin.md), 미전달 |
| `StageSelectionInputLayers.Popup` 이름 | 용근 영향 | `"ShopPopup"` → `"StageMapPopup"`(C-2) | [팝업 Layer 공유](../handoffs/2026-10-27-stage-map-popup-layer-share-yonggeun-Hojin.md), 미전달 |
| `StageProgressData.IsStart` | 민형·용근 | 질문 추가(C-1) | [IsStart 공유](../handoffs/2026-10-27-stage-progress-is-start-share-Hojin.md), 미전달 |
| `StageMapCanvasOrder.cs` 등 참조 없는 파일 셋 | 호진·용근 2줄 | 삭제(C-3) | [파일 삭제 공유](../handoffs/2026-10-27-unused-lobby-stage-files-removed-share-Hojin.md), 미전달 |
| `UISystem/Screens/Stage/`(ShopUI·카드 부품·모델·`StageLegendUI`·`StageSelectPanelUI`·`StageScreenRootUI`·`StageMapCanvasView`), `UIIDs.cs`(`StageScreenRoot = 40`), `DeckEditUI.ParentID` | 현강 화면 폴더 | 상점 화면(U1), 범례, Canvas·UI 소유권 | [상점 UI 공유](../handoffs/2026-10-10-shop-ui-hyeongang-share-Hojin.md), [범례 인계](../handoffs/2026-10-17-stage-legend-ui-handoff-Hojin.md), [Canvas 통합 인계](../handoffs/2026-10-26-stage-select-canvas-integration-handoff-Hojin.md), 미전달 |
| `ViewDataBaseFactory`·`ViewTableKey`·`ViewDataBase`(배경 Query 등록) | 민형 | 필요한 등록만 추가 | [배경 Framework 공유](../handoffs/2026-10-25-stage-map-background-framework-share-Hojin.md), 미전달 |
| CameraCatalog 연결(맵 카메라 드래그) | 민형 | 핸들 대여·반환 사용(계약 무변경) | [카메라 Framework 공유](../handoffs/2026-10-11-stage-map-camera-framework-share-Hojin.md), 미전달 |
