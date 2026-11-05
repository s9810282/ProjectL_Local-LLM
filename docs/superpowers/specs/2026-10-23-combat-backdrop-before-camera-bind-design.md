# 전투 배경 맵을 카메라 연결 전에 세우기 — 설계 (A안)

- 날짜: 2026-10-23
- 상태: **결정 완료(2026-10-23 사용자): Q-B1~Q-B3 A, Q-B4 Stage 먼저, Q-B5 이것 먼저(§6).** BK-1(코드)·BK-2(문서) Stage에서 완료, 컴파일·Unity 미확인. BK-3은 Refector-Stage 병합 뒤. Task 계획 [backdrop-before-camera-bind-plan](../plans/2026-10-23-combat-backdrop-before-camera-bind-plan.md).
- **대신함(2026-10-23, [골격 점검 설계](2026-10-23-combat-skeleton-check-design.md) K-4):** 이 문서의 `CombatViewSystem.BackdropEnterPriority`·`GameBootstrap.BindCamerasPriority`는 순서 표 `RuntimeSystem/Common/SceneNodeOrder.cs`의 `CombatBackdrop`(= `BindCameras` − 5)·`BindCameras`가 됐다. "순서 원본은 안내 §1.1 표"는 "숫자 원본은 `SceneNodeOrder`, 안내 §1.1은 그것을 가리키는 전체 표"로 읽는다.
- 요청: 민형님 → 사용자. "맵 생성 단계를 BindScene 이전으로 끌어올 수 있나." 사용자 설명으로는 배경에 카메라·셰이더 효과가 적용되지 않는 버그를 피하려는 것이다.
- 범위: Combat 씬의 전투 View(작성자 무관, CLAUDE.md 10-22 범위 정정). Framework(로딩·카메라 카탈로그·카메라 효과), ViewStage 표, `MapAssembler`, Core는 바꾸지 않는다.

## 0. 기준 코드 — 두 브랜치에 나뉘어 있다

| 파일 | 최신판이 있는 곳 |
|---|---|
| `StageViewPropPresenter`(맵 SO면 `MapAssembler`로 생성) | `feature/Yonggeun/Stage` `e706b65`(민형님) |
| `CombatViewSystem`, `GameBootstrap`(씬 Node 표), [씬 연결 안내](../viewDocs/runtime-scene-binding-guide.md) §1.1 | `feature/Yonggeun/Refector-Stage` `44c4cdc2`(10-22 개선) |
| `CombatSceneDisplay`, `CombatSceneDisplayFactory`, `CombatCameraWindow` | 두 브랜치가 같다 |

둘을 모두 가진 브랜치가 아직 없다. 아래 줄 번호는 표의 해당 판 기준이다.

**구현 기준(Q-B4 결정): Stage에서 먼저 구현하고, 사용자가 Refector-Stage에 Stage를 병합한 뒤 그쪽에서 조정한다.** Stage 판과 Refector-Stage 판의 차이 중 이 설계에 닿는 것은 아래뿐이다(2026-10-23 대조).

| 곳 | Stage 판 | Refector-Stage 판 | 이 설계에 주는 영향 |
|---|---|---|---|
| `CombatViewSystem` Node 등록·`OnCombatSceneEntered`·`Attach`·`AttachCreated`·`Detach` | 같음 | 같음 | 없음. Stage에서 그대로 구현 |
| `CombatViewSystem.OnCombatSceneDisabled` | `Detach`만 | F1 보고 뒤 `Detach` | 없음(둘 다 `Detach`가 배경을 치움) |
| `GameBootstrap`의 -190 | 숫자 그대로(`new LodingNode(-190, …)`) | 이름 있는 상수 `BindCamerasPriority`·Node 표 | Q-B3 B는 Stage에 상수가 없어 해당 없음 |
| 씬 연결 안내 | §1 블록의 priority 목록만 | §1.1 전체 Node 표(원본) | Stage에서는 §1 블록에 한 줄, 병합 뒤 §1.1 표에 한 줄 |

## 1. 지금 순서와 문제

### 1.1 Combat 씬 진입 순서

`LodingSystem.EnterLoadedScene`(`LodingSystem.cs:153`)은 씬 Enter Node를 priority 오름차순으로 돌린 뒤, `finally`에서 AfterLoad Node를 돈다. 모두 같은 Tick 안이다.

| 단계 | priority | 하는 일 | 맵 |
|---|---|---|---|
| (로드 전) | — | Core 전투 준비(`CombatPrepareJob` → `EnterBattle`)가 현재 선택(스테이지 키)을 Buffer에 게시 → 전환 Job → `Load` | 없음 |
| Enter | -200 | UI `BindScene`(`GameBootstrap`) | 없음 |
| Enter | **-190** | **CameraGroup `BindScene`**: 가상 카메라 카탈로그 수집, 카메라 효과 이벤트 다시 등록(`GameBootstrap`) | **없음** |
| Enter | 0 | Combat `OnEnter`: 수락된 진입 활성화 | 없음 |
| Enter | 90 | 덱 UI 부모 준비 | 없음 |
| Enter | **100** | `CombatViewSystem.OnCombatSceneEntered` → 씬 루트 찾기 → Factory가 표시 조립 → `Install` → `_props.Show(stageKey)` → **맵 생성**, 맵 카메라를 카메라 창구에 넘김 | **생김** |
| Enter | 110 | 튜토리얼 표시 연결 | 있음 |
| Enter | 200 | 전투 UI 열기 | 있음 |
| AfterLoad | 10 | `CameraCatalogSystem.BindCamera` 다시 수집(`CameraCatalogSystem.cs:49`, Framework 자체 등록) | 있음 |

### 1.2 맵 카메라에 기대는 효과

| 효과 | 맵에서 쓰는 것 | 찾는 방법 |
|---|---|---|
| 낮/밤 화면 전환(`CombatPhaseTransitionPresenter`, 스텐실 셰이더) | `Night Camera`·`Day Camera`, 레이어 `BefalRendering`·`AfterRendering` | 시작 때 카탈로그 `GetHandle(이름)` |
| 카메라 줌(`CombatCameraWindow` → `CameraZoomEvent`) | 같은 두 카메라 | 보낼 때 카탈로그 `GetHandle(이름)` |
| 카메라 흔들림 | 맵 카메라의 Impulse Listener 채널 | 카탈로그와 무관 |

### 1.3 문제

- 맵이 -190 카메라 수집보다 늦게 생긴다. 맵 카메라가 카탈로그에 들어가는 것은 Framework가 따로 등록한 AfterLoad 10 재수집 덕분이다. 이 의존은 코드·문서 어디에도 적혀 있지 않다.
- 그래서 다음 경우 "배경에만 효과가 안 먹는" 모양으로 깨진다.
  - 100~AfterLoad 사이에 맵 카메라를 쓰는 효과가 생길 때
  - 재수집 Node의 순서나 유무가 바뀔 때
  - 재수집은 빌려 간 핸들을 모두 반납시킨다(`ReleaseAll`, `CameraCatalogSystem.cs:115`). 그 사이에 핸들을 빌리는 기능이 생겨도 깨진다.
- 배경은 게임 상태와 무관한 씬 내용물인데, 수명은 전투 표시(Display)에 묶여 있다. 그래서 씬 바인딩 단계에 들어갈 수 없다.

### 1.4 목표

1. 맵이 Combat Enter **-190 전**에 씬에 있다. -190의 카메라 수집과 효과 등록이 맵을 포함한다.
2. 맵 카메라 사용이 AfterLoad 재수집에 기대지 않는다(재수집은 Framework 것이라 그대로 남는다).
3. 맵 선택 원본(Buffer 현재 선택의 스테이지 키)과 맵 생성 방식(`StageViewPropPresenter`, ViewStage 표, `MapAssembler`)은 그대로다.
4. 화면에 보이는 변화는 없다(§5 Q-B2 예외 하나).

## 2. 확인한 사실

| # | 사실 | 근거 |
|---|---|---|
| F1 | 스테이지 키는 씬 로드 전에 Buffer에 있다. 진입 연쇄가 준비 Job(`EnterBattle`) 뒤에만 전환한다. 맵 진입도 준비 → 착지 대기 → 전환이다 | `CombatPlan.cs:36-42·81`(44c4cdc2), `CombatEntryService.ActivateAccepted`가 선택과 요청 대상이 같은지 확인 |
| F2 | 배경 부모 `FieldView.cellsRoot`는 씬에 직렬화된 Transform이다. `FieldView.Build`는 이 Transform을 옮기지 않는다. 그래서 필드를 만들기 전에 붙여도 위치가 같다 | `FieldView.cs:97`·`Build` |
| F3 | View DB 표(`CombatDisplayTables.StageProps`)는 Boot의 `InitData` 뒤부터 있다 | `CombatViewSystem` |
| F4 | -195 시점의 진입은 `Activating`이다(활성화는 0). 읽기 계약 `ICombatEntryRead`는 `IsActive` 하나뿐이다 | `CombatEntryRequest.cs:9`, `CombatEntryService.cs:202`(44c4cdc2) |
| F5 | 씬 루트(`CombatSceneRoot`)를 찾는 곳은 100번 한 곳이다("씬 오브젝트를 찾는 유일한 자리") | `CombatViewSystem.cs:670-685`(44c4cdc2) |
| F6 | 배경 Presenter와 카메라 창구는 100번에서 Factory가 표시와 함께 만든다. Presenter가 맵을 세운 직후 카메라를 읽어 `AttachMap`하고, 없애기 직전 `DetachMap`한다(카메라 조각 설계 K1) | `CombatSceneDisplayFactory.cs:86-89`, `StageViewPropPresenter.Show`·`Release` |
| F7 | 표시의 `Clear`·`Release`가 배경도 없앤다. `Clear`는 결과를 붙잡지 않은 채 전투가 사라질 때만 불리고 곧 `Detach`가 따른다 | `CombatSceneDisplay.cs:250-271`, `CombatViewSystem.cs:296-301`(44c4cdc2) |
| F8 | 카탈로그 수집은 비활성 객체까지 찾는다(`FindObjectsInactive.Include`). 이름이 겹치면 예외다 | `CameraCatalogSystem.cs:67-81` |
| F9 | Stage·Lobby View는 카메라 핸들을 AfterLoad 100에서 빌린다(재수집 뒤). 전투 View는 핸들을 직접 빌리지 않고, 효과가 보낼 때 빌린다 | 씬 연결 안내 §1.1(44c4cdc2) |
| F10 | 씬 로드는 `LoadSceneMode.Single`이다. 배경은 씬 객체의 자식이라 씬과 함께 사라진다. `MapBakedLighting`은 `OnDisable`에서 전역 라이트맵을 되돌린다 | `LodingSystem.cs:72`, `MapBakedLighting.cs:83` |
| F11 | Combat의 이탈 목적지는 Stage·Lobby뿐이다. 같은 Combat 씬을 다시 여는 경로(`isCurrentScene`)는 지금 도달하지 않는다 | `CombatPlan.DestinationOf`(`CombatPlan.cs:232`, 44c4cdc2) |
| F12 | 표시를 조립하는 시험 6개 파일이 `CombatSceneDisplayFactory.Create(root, tables, …)`를 직접 부른다. 배경 자식 수를 세는 단언은 없다 | `CombatMortarAssetTests:637`, `CombatPointerMappingTests:284`, `CombatSceneEventPresenterTests:83·172·377·1448`, `TerrainOperationalDisplayTests:147·261`, `WallAttackInteractionTests:214` |

## 3. 설계

### BD1 새 Enter Node: Combat -195 "배경 세우기"

- `CombatViewSystem.Initialize`가 Combat Enter에 Node를 하나 더 등록한다. 상수 `BackdropEnterPriority = -195`.
  - 카메라 다시 연결(-190, `GameBootstrap.BindCamerasPriority`)보다 앞이고 UI 다시 연결(-200)보다 뒤다. 두 숫자의 순서 원본은 씬 연결 안내 §1.1 표다(Q-B3).
- 하는 일(오류 상태면 아무것도 안 함):
  1. 씬 루트를 찾는다(BD2).
  2. Factory로 배경 Presenter를 만든다(BD3).
  3. Buffer 현재 선택의 스테이지 키로 `Show`한다. 선택이 없거나 키에 맞는 항목이 없으면 지금처럼 아무것도 두지 않는다.
- 진입 활성 여부는 여기서 검사하지 않는다(F4, Q-B1 A). 진입의 정당성은 지금처럼 0(활성화)과 100(`IsActive` 요구)이 검사한다. 0이 실패하면 F1로 런타임이 멈추고, 배경은 씬과 함께 사라진다(F10).
- 예외는 지금 100번 예외와 같은 길이다: 로딩 Node → Bootstrap → F1.

### BD2 씬 루트 찾기를 -195로 옮김 (찾는 곳은 계속 하나)

- 루트 찾기(`FindObjectsByType<CombatSceneRoot>`, 정확히 하나)를 -195로 옮기고, 찾은 루트를 `_sceneRoot`에 보관한다.
- 100번 `OnCombatSceneEntered`는 찾지 않고 보관한 루트로 `Attach`한다. 보관한 루트가 없으면(-195가 안 돈 경우) 예외다.
- 표시 연결 뒤에만 하던 씬 루트 이벤트 구독(`Disabled`·`FocusLost`)은 지금처럼 `AttachCreated`에서 한다. 루트 "보관"과 "구독"은 다른 사실이라, 보관은 -195, 구독은 100에 그대로 둔다.
- `Detach`(-100)는 구독 해제 뒤 루트 보관도 비운다. 지금 코드에서는 보관 = 구독이라 한 줄로 되어 있는데, 이 둘을 나눈다.

### BD3 배경의 주인을 표시에서 씬 연결로

- 배경 Presenter(`StageViewPropPresenter`)는 그대로 쓴다. 바뀌는 것은 누가 언제 만들고 없애느냐다.
  - 만들기: Factory 새 입구 `CombatSceneDisplayFactory.CreateBackdrop(root, tables)`. 조립은 Factory에 둔다(§4.6).
  - 보관: `CombatViewSystem`의 `_backdrop`. 씬 연결 수명(−195 ~ −100)이다.
  - 표시(`CombatSceneDisplay`)는 배경을 갖지 않는다. `Install`의 `Show`, `Clear`·`Release`의 배경 해제 줄을 뺀다. 생성자의 배경 인자는 카메라 창구 인자로 바꾼다(표시 `Release`가 창구 `DetachMap`을 부르려고. 지금 표시는 창구를 직접 들지 않고 배경을 통해서만 닿는다).
- **맵 카메라 사실의 흐름 (카메라 조각 설계 K1 개정)**
  - 지금: Presenter가 세운 직후 읽어 창구에 `AttachMap`, 없애기 직전 `DetachMap`. 프랍 수명과 카메라 사실 수명이 Presenter 하나에 묶여 있다.
  - 바뀜: Presenter는 세운 직후 카메라를 **읽기만** 해서 `MapCameras`(읽기 전용 목록)로 낸다. 같은 이름 둘이면 세운 맵을 치우고 던지는 규칙은 그대로다.
  - 표시를 만들 때 Factory가 창구에 `AttachMap(backdrop.MapCameras)`를 넘긴다. 입구는 `Create(root, tables, services, backdrop = null)`처럼 끝 선택 인자로 더한다. 그래서 표시를 조립하는 기존 시험(F12)은 그대로 컴파일되고, 그 시험에서는 맵 카메라가 없는 창구가 된다. 표시 `Release`가 창구 `DetachMap`을 부른다(빌린 줌 취소 포함, 지금 `_props.Release`가 하던 일).
  - 창구가 맵 카메라를 갖는 기간은 "표시 연결 기간"이다. 배경은 -195에 생기고 -100에서 표시보다 나중에 사라진다. 그래서 이 기간은 항상 배경 수명 안에 있다(창구가 사라진 카메라를 쥐는 때가 없다).
- Presenter 생성자는 `(표, 부모)` 두 인자가 된다. 창구 인자를 뺀다. 민형님 시험 `MapSoDatabaseAssemblyTests`의 두 인자 생성은 그대로 컴파일된다.

### BD4 해제 순서

**2026-10-23 BK-1 구현 때 고침:** `Detach`는 씬 이탈만이 아니라 전투 중간에도 불린다. 전투가 사라지면 `Clear` 뒤 `Detach`, 씬 루트가 꺼지면 `Detach`다. 그래서 배경 해제를 `Detach`에 넣으면 Q-B2 A(씬을 나갈 때까지 남김)가 깨진다. 배경 해제는 새 `ReleaseScene`(= `Detach` → 배경 해제·루트 보관 비움)에만 두고, Exit -100 Node와 `Shutdown`이 이것을 부른다. 처음 쓴 표의 "`Detach`에 배경 해제를 더한다"는 이렇게 바뀌었다.

| 언제 | 순서 |
|---|---|
| 정상 이탈 Exit -100 | `ReleaseScene`: 표시 정리(`Detach`, 표시 `Release` 끝에 창구 `DetachMap`) → **배경 `Release`** → 루트 보관 비움 |
| 표시 연결 전에 멈춤(0 실패 등) | Exit -100 `ReleaseScene`이 표시 없이도 배경을 치운다(`Detach`는 구독만 풀고 돌아옴) |
| 전투가 사라짐(`Clear` → `Detach`) | 표시만 놓고 배경은 남긴다. 씬 이탈이 치운다(Q-B2 A) |
| 씬 루트가 꺼짐(`OnCombatSceneDisabled`) | `Detach`만. 배경은 씬 이탈이 치운다 |
| 런타임 종료(`Shutdown`) | `ReleaseScene` |
| -195 안에서 실패 | 세운 배경은 `Show`가 치운다(지금 규칙). Presenter와 루트 보관은 Exit -100이 비운다 |

`StageViewPropPresenter.Release`와 `ReleaseScene`은 여러 번 불려도 안전하다.

### BD5 바뀌지 않는 것

- 맵 선택: Buffer 현재 선택의 스테이지 키(`ReadDisplayValues`와 같은 원본). View는 InGame DB를 읽지 않는다(P6).
- 맵 생성: `StageViewPropPresenter.Show`의 레거시 프리팹 / `MapAssembler` 분기, 보정 값, 같은 키면 그대로 두기.
- Framework: CameraGroup `BindScene`, AfterLoad 재수집, 카메라 효과, 로딩 Node 큐. Bootstrap의 -200·-190 Node.
- 낮/밤 전환·줌·흔들림의 동작.

### BD6 문서

- 씬 연결 안내 §1.1 표에 줄 하나: `Enter | -195 | Combat | CombatViewSystem | 씬 루트 찾기·배경 맵 세우기(카메라 다시 연결보다 먼저) | CombatViewSystem.BackdropEnterPriority`. 100번 줄 설명 "Attach(SceneRoot를 한 번 찾음)"을 Combat만 "Attach(-195에서 찾은 루트)"로 고친다.
- 카메라 조각 설계 K1에 개정 표시(BD3).
- `CombatCameraWindow`·`StageViewPropPresenter` 문서 주석의 K1 설명.
- 민형님 공유 메모: 맵 생성 시점이 -195로 바뀜, -190 수집에 맵 카메라가 들어감, ViewStage·`MapAssembler`는 손대지 않음.

### 검토한 다른 방법

| 방법 | 기각 이유 |
|---|---|
| 배경만 맡는 별도 View 시스템(튜토리얼처럼 자기 Node) | 맵 카메라 사실이 `CombatViewSystem`의 표시 안 창구로 가야 해서 시스템 사이 연결이 생긴다. 씬 루트 찾는 곳도 둘이 된다(F5) |
| 카메라 다시 연결을 100 뒤(예: 150)로 옮김 | "View가 핸들을 빌리기 전에 수집"이라는 Bootstrap 규칙과 어긋난다. 100~150 사이 제약이 그대로 남는다(B안) |
| 표시 전체(`Attach`)를 -195로 당김 | 표시 연결은 진입 활성(0)을 요구한다(F4). 입력·재생 수명까지 바뀐다 |

## 4. 원칙 점검

**SSOT**
- 맵 선택 원본(Buffer 선택)은 바뀌지 않는다.
- 씬 루트를 찾는 곳은 여전히 하나다(100 → -195).
- 맵 카메라 사실을 읽는 곳도 하나다(Presenter). 창구는 지금처럼 받은 사본을 표시 수명 동안 든다.
- Node 순서의 원본은 씬 연결 안내 §1.1 표와 각 소유자 상수다.

**SOLID**
- S: 표시는 "전투를 그리는 일"만 하고, 씬 내용물인 배경은 씬 연결 수명이 맡는다.
- D: 생성은 Factory 입구가 한다. 시스템은 Factory가 준 객체만 들고, 씬 찾기는 한 자리다.
- 과한 추상화는 없다. 새 인터페이스·클래스 없이 기존 Presenter의 주인만 옮긴다.

**FW 계약**
- Framework 파일은 바꾸지 않는다.
- Node 등록은 기존 `ILodingRegisterService` 입구로 `Initialize`에서 한다(Build 뒤 구조 변경 없음, 지침 §11).
- FW-004(내부 상태 우회)는 해당 없음.
- 지침 §25(Find 남용)는 지금과 같다. 한 번, 씬 로드 Node에서만 찾는다.

**변경 독립성(§4.6) — 대표 시나리오**

| 시나리오 | 지금 고칠 곳 | 바뀐 뒤 고칠 곳 |
|---|---|---|
| 배경 생성 방식 변경(맵 SO·베이크 등) | Presenter. 다만 카메라 창구 연결이 같이 있어 창구 계약도 읽어야 함 | Presenter만(카메라는 읽기만) |
| 카메라 효과 규칙 변경(줌·흔들림) | 창구 + 연결 주인인 Presenter 확인 | 창구 + Factory 한 줄 |
| 씬 진입 순서 변경 | 표시 `Install` 안에 묻힌 `Show`를 찾아야 함 | `CombatViewSystem` Node 하나 + 안내 §1.1 |
| 전투 표시 설치·정리 변경 | 표시 정리 목록에 배경이 섞여 있음 | 배경 무관 |

**남는 공통 경계**
- 배경 → 창구 카메라 사실 전달(Factory 한 줄).
- `CombatViewSystem.Detach`의 해제 순서(표시 → 배경).
- `CombatViewSystem`이 Node 하나와 필드 하나만큼 커진다. 조정자 클래스 분리(C1)가 이 Node를 "씬 연결" 묶음에 넣으면 된다(Q-B5).

## 5. 영향

**코드(구현 때, 컴파일 미확인으로 보고)**
- `CombatViewSystem`: Node 등록, -195 처리, 루트 보관·구독 분리, `Detach` 배경 해제.
- `StageViewPropPresenter`: 창구 인자 빼기, `MapCameras` 내기.
- `CombatSceneDisplay`: `_props` 제거, 창구 `DetachMap`을 `Release`에.
- `CombatSceneDisplayFactory`: `CreateBackdrop` 입구, `Create`가 배경의 카메라 사실을 받아 창구에 넘김.
- 새 파일은 없다.

**시험(새 시험 없음, 깨지는 곳만)**
- 우리 `CombatMapCameraTests`의 Presenter 시험 둘이 창구 인자 생성자를 쓴다. 동작이 옮겨 가므로 "Presenter가 카메라를 읽어 내고 같은 키면 그대로, 해제하면 비움 / 같은 이름 둘이면 치우고 던짐"으로 단언을 바꾸거나 지운다. 10-22 Q1 선례는 지우기다.
- 표시를 조립하는 시험 6개 파일(F12)은 선택 인자라 고칠 곳이 없다. 그 시험들의 `Install`은 이제 배경을 세우지 않지만, 배경을 단언하는 곳이 없다.
- `MapSoDatabaseAssemblyTests`(민형님): 두 인자 생성자라 그대로다.

**화면**
- 차이 없음. 단 결과를 붙잡지 않은 채 전투가 사라지는 비정상 경로(F7)에서는, 지금은 배경도 사라지지만 바뀐 뒤에는 씬을 나갈 때까지 남는다(Q-B2).

**확인(사용자 Unity)**
1. 컴파일, EditMode.
2. 전투 진입 직후 맵 카메라가 -190 카탈로그에 있는지. 카탈로그에는 조회 로그가 없어서, 진입 첫 프레임에 낮/밤 전환이나 줌이 배경에 먹는지로 본다.
3. 낮/밤 전환, 포효 줌, 피격·기차 흔들림이 배경에 적용되는지.
4. 패배·포기·승리 이탈 뒤 배경과 라이트맵이 남지 않는지.
5. 민형님이 본 증상이 사라졌는지(증상은 아직 모름).

## 6. 정할 것

| # | 질문 | A(권장) | B |
|---|---|---|---|
| Q-B1 | -195에서 진입 상태를 검사하나 | **결정 A(2026-10-23 사용자)**: 검사 안 함. 선택된 스테이지 키만 본다(F4). 정당성은 0·100이 지금처럼 검사 | 진입 읽기 계약에 "활성화 중" 질문을 더해 -195에서도 요구(LifeCycle 계약 변경) |
| Q-B2 | 전투가 사라지는 `Clear` 경로에서 배경 | **결정 A(2026-10-23 사용자)**: 남긴다(씬 내용물, 곧 씬 이탈이 치움) | 지금처럼 같이 없앤다(배경 해제를 `Clear` 호출 자리에 따로 둠) |
| Q-B3 | -195 상수 위치 | **결정 A(2026-10-23 사용자)**: `CombatViewSystem.BackdropEnterPriority`. 순서 원본은 안내 §1.1 표 | Bootstrap의 `BindCamerasPriority`를 공개하고 `-5`로 참조(전투 View가 Bootstrap을 알게 됨) |
| Q-B4 | 어느 브랜치에서 구현하나 | **결정(2026-10-23 사용자): Stage에서 먼저 구현 → 사용자가 Refector-Stage에 Stage를 병합 → 거기서 조정**(§0) | — |
| Q-B5 | 조정자 분리(C1)와 순서 | **결정(2026-10-23 사용자): 이것 먼저.** 병합·조정 뒤 조정자 수정(C1)에 들어가며, C1이 이 Node를 씬 연결 묶음에 넣는다 | — |

## 7. Phase 초안

Task는 [계획](../plans/2026-10-23-combat-backdrop-before-camera-bind-plan.md)이 원본이다.

- **BK-1 코드(Stage)**: BD1~BD4, 깨지는 시험만 맞춤. MSBuild·새 시험 없음(상시 지시). 멈추고 보고.
- **BK-2 문서(Stage)**: BD6 중 Stage에 있는 문서(안내 §1 블록, K1 개정, 민형님 메모).
- **BK-3 병합 뒤 조정(Refector-Stage)**: 사용자 병합 뒤 충돌 확인, 안내 §1.1 표 한 줄, CLAUDE.md 한 줄.
