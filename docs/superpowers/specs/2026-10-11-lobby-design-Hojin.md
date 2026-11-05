# 로비 설계

- 작성자: 호진(이호진)
- 상태: 로비 View 구현 완료(LV1~LV5·LV8A, 모델·배치 View DB, 카메라 이전, Job·Plan 재구성, 역할별 SO, Lobby.prefab 시설 연결 LB-1). 사용자 Unity 확인은 역할별 SO 후속(2026-10-25)까지 받았고, LB-1(2026-10-26 커밋 `8ee9984e`)은 Unity 확인 기록이 없다. LV6·LV7·LV8B·LV9는 미착수, Core 계약 LC0~LC3은 미합의, 설정 UI의 로비 인수는 미착수다. 칸마다 뛰는 이동(§15)은 구현 완료(2026-10-29)다. 시설 호버 글자 빛남(§16)은 코드·에셋 완료(2026-11-02, 컴파일·재생 미확인)다.
- 범위: `Lobby.unity`의 로비 View(System·Scope·카메라·입력·시설·덱 팝업·아티팩트 팝업 연결·출발·Run 종료 뒤 복귀), 로비 실행 계층(LifeCycle·Plan·Job), 로비 View DB(SO 다섯). 아티팩트 팝업 내부, 글로벌 UI 구현, Core 규칙은 다루지 않는다(§1.3).
- 계획: [로비 계획](../plans/2026-10-11-lobby-plan-Hojin.md) — 진행 결과·확인 상태·남은 일.
- 안내: [로비 안내](../viewDocs/lobby-guide-Hojin.md) — 기획자·아티스트용 저작·연결 방법.
- 정리: 2026-10-27 로비 설계(1~6부), 출력 카메라 이전 설계(3판), 로비 Job·Plan 재구성 설계(4판), 배치 DB 이전 설계, 역할별 SO 설계, Lobby.prefab 시설 자동 연결 설계를 이 문서로 합쳤다. 옛 판은 git `10914570`.

## 1 목적과 범위

### 1.1 기획 요구

로비는 아티팩트·스테이지 입장·덱 빌드를 고르는 3D 공간이다. 기획 원본은 통합 기획서(팀 Dropbox `Project L/기획 문서/통합 기획서.pptx`, 저장소 밖) 8~11번 슬라이드다. 11번의 상세 기능 본문은 `ㅁ`만 있어 근거로 쓰지 않는다.

| 번호 | 요소 | 표현 | 지금 |
|---|---|---|---|
| 1 | 배경 | 화면 UI | `LobbyBackgroundCanvas`. 입력을 가로채지 않는다 |
| 2 | 메인 캐릭터 | 3D 체스 말 | 모델 SO의 캐릭터 프리팹(Akimbo)을 복제한다. 실제 선택 캐릭터 연결은 미착수(LV6) |
| 3 | 바닥 | 3D | 모델 SO의 바닥 프리팹. 바닥 클릭 기능은 없다 |
| 4 | 아티팩트 | 3D 시설(금고) | 도착 뒤 아티팩트 편집 팝업. 편집 진입점은 로비 하나다 |
| 5 | 스테이지 입장 | 3D 시설(문) | 도착 뒤 출발 요청 → 새 Run → StageSelect |
| 6 | 덱 빌드 | 3D 시설(탁자) | 도착 뒤 덱 팝업. 지금은 빈 목록과 준비 안내만 보인다(LV7 미착수) |
| 7 | 메인 캐릭터 얼굴 | 글로벌 UI | 기존 `MainCharactorUI`(현강님). 첫 Run 전 메인 키가 없어 얼굴 연결은 LC1 대기 |
| 8 | 설정 | 글로벌 UI | 현강님 `OptionUI`(`UIId.Setting`)와 ESC 열기가 있다. 로비 View 쪽 인수(이동 취소·모달 소유)는 하지 않았다(§7.4) |

### 1.2 사용자 확정

- 시설을 누르면 바로 실행하지 않는다. 캐릭터가 **시설 앞 지정 칸까지 한 칸씩 뛰어** 도착한 뒤 실행한다(2026-10-29 바뀜, §15). 걷기 애니메이션이나 전투 이동 규칙(Core 경로)이 아니다. 바뀌기 전(2026-10-11)은 "살짝 들림 → 이동 → 내려놓기"였다.
- 아티팩트 편집은 로비 4번 시설에서만 연다. StageSelect의 편집 진입점은 LV5에서 지웠다. 아티팩트 보유·배치와 전투 효과는 그대로다(2026-10-11, LV5 2026-10-13).
- 카메라는 고정 구도다. 카메라 드래그·시설별 실내 씬·강화·업적·기록 보관소는 넣지 않는다.
- 아직 정하지 않은 것: 2번 모델 클릭 기능, 덱 편집의 세부 규칙, 설정 항목. 임의로 더하지 않는다.

### 1.3 다른 문서가 맡는 것

| 주제 | 원본 |
|---|---|
| 비전투 View 공통 구조(System·Scope·`PopupViewScope`·`PopupInputSession`) | [View 구조 설계](2026-10-13-view-structure-design-Hojin.md) |
| 씬 UI 참조 소유권(`LobbyCanvasView`) | [스테이지 맵 설계](2026-10-10-stage-map-design-Hojin.md) |
| 로비 검사 후속(카메라 이름 상수 걷기 N-2, 로비 null 허용 걷기 R-a, 출발 안내 파일 삭제) | [로비·스테이지 검사 후속 설계](2026-10-26-lobby-stage-review-cleanup-design-Hojin.md) |
| 새 Core 규칙(진영 턴·직군·특성)이 로비에 닿는 곳 | [새 Core 대응 설계](2026-10-26-new-core-lobby-stage-response-design-Hojin.md) |
| 아티팩트 팝업 내부(보드·드래그·호버·문구) | [아티팩트 팝업 설계](2026-10-06-artifact-popup-design-Hojin.md) |
| Run 종료 목적지 정책 | [전투 Job·Plan 설계 J7](2026-10-10-combat-job-plan-restructure-design.md#j7-이탈-연쇄-k21-유지)(용근님) |
| 글로벌 UI의 씬별 표시(`GlobalUIPresentation`)·`MainCharactorUI`·명성치·`Clicker` | 현강님 UI 코드 |

적용 기준은 [Framework 프로그래머 가이드](../../../Assets/ProjectL/Script/Systems/Documentation/framework-programmer-guide.md) FW-001~016, [UI 입력 규칙](../../../Assets/AGENTS.md), [카메라 카탈로그](../../render-camera-catalog.md), [SSOT·SOLID 기준 §4](../handoffs/2026-09-30-combat-view-design-session-handoff.md#4-반드시-지킬-설계-원칙)다.

## 2 지금 구조

### 2.1 씬과 연결 순서

씬 번호는 `SceneIndices`의 Boot 0 · Lobby 1 · Stage 2 · Combat 3이다(Artifact 4는 예약). `Title.unity`는 삭제했고 시작 화면 역할은 Boot가 맡는다(2026-10-13 사용자 결정). Boot가 런타임을 한 번 조립하고, 각 시스템은 씬 번호에 진입·이탈 노드를 등록한다. 순서 값의 원본은 `RuntimeSystem/ErrorHandling/SceneNodeOrder.cs`다.

| 순서 | 노드 | 하는 일 |
|---|---|---|
| -200 | UI `BindScene` | 로비 UI(`UIBase`) 등록 |
| -190 | Camera `BindScene` | Catalog가 `LobbyCamera`를 수집 |
| 0 | `LobbyLifeCycle.OnEnter` | 아티팩트 도메인 정지를 푼다 |
| 100 | `LobbyViewSystem.AttachFromScene` | `LobbySceneRoot`를 한 번 찾고 `Camera.main`을 받아 Scope를 만들고 시작 |
| AfterLoad 100 | `LobbyViewSystem.BindCameraHandle` | `LobbyCamera` 핸들 대여(Catalog AfterLoad 재수집 뒤) |
| 200 | `GameBootstrap.ShowLobbyUI` | `GlobalUIPresentation.ShowLobby` + `Clicker` 열기 |

이탈은 -200 `LobbyPlan.CanLeave` 검사 → -100 View Detach(Scope 폐기) → 0 `LobbyLifeCycle.OnExit` 순서다. 첫 구간 진입 전에 `GameBootstrap`이 아티팩트 보드 준비를 끝낸다(§9.2).

### 2.2 객체와 책임

| 객체 | 자리 | 책임 | 하지 않는 일 |
|---|---|---|---|
| `LobbySceneRoot` | 씬 | 저작 참조 셋(`lobbyCamera`·`ui`·`worldRoot`)과 그 검증 | 서비스 조회, 게임 상태 계산 |
| `LobbyViewSystem` | View 모듈 | InitData(View Query), 서비스 연결, Enter 100·Exit -100 등록, SceneRoot 검색 1회, 출력 Camera 받기, Scope 생성·게시·폐기, Tick·StopInput·Shutdown | 씬 객체의 기능 |
| `LobbyViewScope` | 씬 연결 한 번 | 하위 객체 조립·소유·Tick·폐기, 현재 호스트 판정(`IsCurrent`·`IsSuperseded`·`CanWork`), 시설 차단 | 서비스·씬 검색 |
| `LobbyVisualSet` | Scope | 방·바닥 복제와 정리, 방 시설 목록 제공 | 캐릭터, 씬의 다른 자식 |
| `LobbyRoomBinding` | Scope | 방 원본 구성 검증, 생성한 방에서 시설 셋 수집 | 입력 연결 |
| `LobbyFacilityView` | 방 프리팹 | 시설 종류(`kind`)와 같은 객체의 BoxCollider | 입력·UI·이동 실행 |
| `LobbyBoard` | Scope | 배치판 SO로 시작 자세 하나·시설별 도착 자세 셋 계산. 종류 → 칸 대응의 유일한 자리 | SO·씬에 되쓰기 |
| `LobbyCharacterVisual` | Scope | `WorldRoot` 아래 이동 루트·보정 루트·캐릭터 복제본 생성과 정리 | 이동 |
| `LobbyInputController`·수신 슬롯 | 입력 | 장치 값을 포인터·선택·취소 호출로 바꿈 | 시설 판단 |
| `LobbyFacilityPicker` | Scope | 출력 Camera Ray와 시설 Collider 셋 대응 | 규칙 판정 |
| `LobbyInteraction` | Scope | 시설 클릭 의도, 이동·팝업·출발 중 입력 허용, 시설 실행 한 번 | Core 호출 |
| `ChessPieceMotion` | View 공용(로비·StageSelect) | 예약·완료/취소 수명과 정착 자세. 곡선은 경로 모양에 묻는다 | 시설 실행 |
| `LobbyViewHopPath` | Scope(경로 모양) | 도착 칸까지 칸별 경로, 걸음 전·도착 뒤 회전, 뛰기 높이(§15) | 수명·시설 실행 |
| `LobbyFacilityActions` | Scope | 시설 종류 → 덱 열기·아티팩트 열기·출발 제출 | 이동 |
| `LobbyCameraController` | View | `LobbyCamera` Catalog 핸들 대여·반환 | 카메라 값 변경 |
| `LobbyDeckPopupController`(+ `LobbyDeckPresenter`·`LobbyDeckPopupViewScreen`) | Scope | 덱 팝업 표시·입력·수명 | 덱 확정 |
| `ArtifactPopupController`(`ArtifactPopupViewFactory`가 만듦) | Scope | 아티팩트 팝업 표시·입력·결과 소비 | 보드 규칙 |
| `ArtifactPopupOutputMask` | Scope | 팝업이 열린 동안 출력 Camera 마스크에 UI 층 추가·복원 | 다른 출력 설정 |
| `LobbyDeparturePresenter` | View | 출발 제출, 요청 질문 읽기로 입력 막기·풀기 | 실행 단계 해석 |
| `LobbyLifeCycle` | LifeCycle | 트리거와 이탈 검사 호출 | 판단·Core 호출 |
| `LobbyProgress`·`LobbyArtifactDomain` | LifeCycle 서비스 | 판단 사실 읽기 / 아티팩트 도메인 정지 한 칸 | 판단·등록 |
| `LobbyPlan`·단계 Job | Plan·Job | 판단 표 / Core 호출과 그 호출의 확인 | 상태 보관 |
| Core·Core Buffer | Core | 아티팩트 보드, 새 Run, 로스터 | 표시 |

외부 서비스가 필요 없는 협력 객체에는 서비스 키나 System을 새로 만들지 않는다. 시설은 `LobbyFacilityKind`(Artifact·StageEntry·Deck)로 식별하고 GameObject 이름·순서·버튼 문구로 분기하지 않는다. 시설 세 개를 위한 범용 시설 엔진이나 상속 트리는 만들지 않는다.

### 2.3 씬 계층과 런타임 생성물

```text
Lobby.unity
├─ LobbySceneRoot                 lobbyCamera · ui · worldRoot
│  └─ LobbyRoot                   WorldRoot. 칸 좌표 기준, 생성물 부모, 아티팩트 팝업이 숨기는 공간
│     ├─ Lobby(Clone)             [런타임] 모델 SO roomPrefab — 방 + 시설 셋(Artifact·StartStage·Deck)
│     ├─ LobbyFloor(Clone)        [런타임] 모델 SO floorPrefab
│     └─ LobbyCharacterRoot       [런타임] 시작 자세·이동
│        └─ LobbyCharacterVisual  [런타임] 표시 보정
│           └─ Akimbo(Clone)      [런타임] 모델 SO characterPrefab
├─ LobbyCamera                    고정 가상 카메라 하나. 씬에 Unity Camera·Brain 없음
├─ LobbyBackgroundCanvas          Screen Space - Camera. 출력 Camera는 연결 때 붙임
├─ ArtifactWorldRoot   아티팩트 3D 보드(평소 꺼짐, UI 층)
└─ LobbyCanvas                    Overlay 하나(Sort Order 2), LobbyCanvasView
   ├─ LobbyPanel                  로비 안내·Clicker
   ├─ LobbyDeckPanel              LobbyDeckUI
   └─ ArtifactPopupPanel          ArtifactPopupUI
```

정확한 계층은 `Lobby.unity`가 원본이다. `WorldRoot`의 씬 이름은 2026-10-26 `8ee9984e`부터 `LobbyRoot`다(그 전 이름은 `LobbyWorld`). 편집 상태의 씬에는 방·바닥·캐릭터가 보이지 않는다. Play의 Attach 때 생성된다.

### 2.4 시설 클릭에서 Core까지

```text
시설에서 누름·같은 시설에서 놓음 → LobbyBoard의 도착 자세 → 들기 → 이동 → 내려놓기
  → 정상 도착을 다음 Tick에 한 번 소비 → LobbyFacilityActions
      ├─ 덱·아티팩트: 팝업 수명 + IUISystemService (Core 경로 없음)
      ├─ 아티팩트 편집: ArtifactViewBuffer 입력 → LobbyPlan → ArtifactCommandJob → Core
      └─ 출발: LobbyViewBuffer.Submit → LobbyLifeCycle → Register(Lobby, Next)
               → LobbyPlan.Decide → RunStartJob(StartNewRun, 결과는 LobbyRunStartOutcome) → LobbyTransitionJob(Change(Stage))
```

호버·이동 연출·팝업 열기는 Core로 보내지 않는다. 게임 상태를 바꾸는 일만 Buffer → Plan → Job → Core 공개 서비스로 간다.

## 3 결정 기록

같은 ID가 여러 작업에서 쓰여(예: 배치 P1~P7과 Job·Plan P1~P10) 표마다 출처를 적는다. "바뀐 것" 줄의 화살표 앞 값은 지금 값이 아니다.

### 3.1 구조·범위·출발

| ID | 결정 | 날짜·근거 |
|---|---|---|
| (구조) | 로비 씬 하나 + 기존 글로벌 UI + 시설별 팝업. 시설마다 씬, LobbyManager 하나는 채택하지 않음 | 2026-10-11 설계 |
| (씬 번호) | Boot 0·Lobby 1·Stage 2·Combat 3, Title 씬 삭제 | 2026-10-13 사용자 |
| LV3 | 설정 UI는 UI 작업자 담당. View가 만든 설정 화면·입력·시험을 철회하고 공통 입력 수명(`PopupInputSession`)과 Deck 숨김 정책만 남김 | 2026-10-12 사용자 |
| LV4 | 덱 팝업은 설정과 독립. 고유 `UIId.LobbyDeck`(15), Common만 허용하는 `LobbyDeckPopup` Layer, 입력 Factory 등록 한 줄 | 2026-10-12 사용자 승인 |
| LV5 | 아티팩트 운영 편집을 로비로 이전. LC4 기존 계약 재사용, Core 외부 회신은 필수 조건이 아님(§10.2) | 2026-10-13 사용자 |
| LV8A | 기본 로드아웃 1로 **항상 새 Run**을 만든 뒤 StageSelect. 이어하기 아님. LV6·LV7은 선행 조건이 아님 | 2026-10-13 사용자 |
| RS-C1 | 최종 승리 Run 결과(EndingPanel)는 전투 씬에서 보이고 닫으면 로비. 로비 자동 표시(RS-L1)는 걷음 | 2026-10-20 사용자 확정 |
| (Run 종료) | Run이 끝나면(패배·포기·최종 승리) 로비. 새 Run은 로비 출발 한 곳 | 2026-10-20·10-22, 전투 J7 |

바뀐 것: Run 종료 목적지는 "일반 패배만 로비"(2026-10-16) → "모든 Run 종료는 로비, 전투 쪽 새 Run 걷음"(2026-10-20·10-22, 전투 J7). 로비 LV10의 "새 Run 생성 위치 통합"은 이 결정으로 풀렸다.

### 3.2 모델·배치·시설 연결

| ID | 결정 | 날짜·근거 |
|---|---|---|
| LVM | 바닥·시설 외형을 View DB 설정 + 프리팹으로. 정적 Mesh만(Animator·Skinned·Particle 제외). GameObject 풀 없음 | 2026-10-12 |
| (장식) | 시설 `Platform`·`FacilityTopFixture`, 플레이어 `ChessPieceBaseFixture` 등 7개 제거 | 2026-10-12 사용자 |
| D1 | 덱 UI 버튼 수신자를 한 번만 연결(`EnsureCloseBinding`, 항목은 생성 때 `Bind` 한 번, `RebindControls` 삭제) | 2026-10-24 사용자 기능 확인 |
| D2 | 모델 Data의 프리팹 구조 검사는 인스턴스당 첫 성공 1회, 이후 참조 생존만 확인(안 A) | 2026-10-24 승인, Unity 확인 대기 |
| D3 | `LobbySceneRoot.Validate`·배치 검사의 호출 경계 유지 | 2026-10-24 |
| C1·C2 | Overlay Canvas 셋을 `LobbyCanvas` 하나로(Sort Order 2), 배경 Canvas 둘은 유지 | 2026-10-24 사용자 확인 |
| Q-L2 | 방향은 공용 `Rotation4` | 2026-10-25 |
| Q-L3 | 씬 시작·도착 Anchor 넷 제거 | 2026-10-25 |
| Q-L4 | "서는 곳 = DB 칸, 누르는 곳 = 방 프리팹". 클릭 영역·시설 종류는 방 프리팹, 생성 때 한 번 수집 | 2026-10-25 사용자 |
| Q-L5 | 캐릭터 표시 보정은 로비 SO | 2026-10-25 |
| Q-L6 | 진입·재연결 때 위치와 방향 모두 DB 시작 값으로 복원 | 2026-10-25 사용자 |
| (역할별 SO) | 모델·배치판·이동·표시 보정 SO 넷을 기존 테이블에 직접 등록. 묶음 SO 없음, Inspector 네 칸 | 2026-10-25 사용자 |
| (후속) | 방·바닥 빈 부모 제거, `WorldRoot` 직속 생성 | 2026-10-25 사용자 |
| (후속) | 캐릭터 이동·보정 부모 런타임 소유, 모델 SO에 Akimbo 프리팹 직접 연결(UnitVisual 조회 제거) | 2026-10-25 사용자 |
| LB-1 | 방 = `Lobby.prefab` 직접. 시설 자식에 `kind`만, 같은 객체 BoxCollider 수집. 시설 모델 슬롯 셋 제거, 방 저작 변환 보존 | 2026-10-26 사용자 "진행해." |

배치 DB 설계에서 정한 규칙(배치 P1~P7): P1 칸 → 위치 식(§5.3), P2 방향 규칙, P3 새 칸의 초기값은 그때 화면 값으로 저장, P4 이동 값 조건은 `ChessPieceMotionRules` 하나, P5 칸 검사 범위(점유·인접·타일 대조 없음), P6 캐릭터 루트 크기 1·표시 루트 identity 검사(캐릭터 부모 런타임 생성으로 대상 없음), P7 표시 보정은 균일 크기만.

바뀐 것:
- 격자 크기: 8×6(LVM1G) → 7×5(2026-10-12) → 11×9(2026-10-13) → 4×4, 간격 4.25/3.85(2026-10-20 방 모델) — 지금 값은 §5.3.
- 위치 원본: 씬 `LobbySceneRoot`·`LobbyFacilityView`의 칸 값(LVM1G) → 씬 Anchor Transform(6부 A1, 2026-10-24) → 배치판 SO의 칸(Q-L, 2026-10-25). 시설 칸·점유·인접 검사는 A1에서 없앴고 되살리지 않았다.
- 도착 정면: 시설 칸 중심을 바라봄(2026-10-13) → `ArrivalAnchor.forward`(2026-10-20) → 배치판 SO의 칸별 방향(2026-10-25).
- 코드 기본값 맞춤 D4(2026-10-24) → A1에서 격자 필드 자체를 지워 대상 없음.
- 모델 SO: 바닥 하나 + 시설 넷(LVM) → 시설 슬롯 선택(2026-10-20) → `roomPrefab` 필수·`LobbyRoom.prefab` 래퍼(5부 R1~R3, 2026-10-24) → 단일 SO에 배치·이동·보정 묶음(Q-L1, 2026-10-25) → 역할별 SO 넷(2026-10-25) → `Lobby.prefab` 직접·시설 슬롯 제거(LB-1).
- 클릭 영역: 씬 시설 루트 Collider → 시설 아래 `SelectionArea`(6부 A2) → 방 프리팹(Q-L4) → 방 프리팹 시설 자식의 같은 객체 Collider(LB-1).
- 캐릭터 외형: 씬 캡슐 → 고정 아킴보 인스턴스(2026-10-16) → `UnitVisual_MainCharacter` 공유(6부 B, 2026-10-24) → 모델 SO `characterPrefab` 직접(2026-10-25).

### 3.3 카메라와 출력

| ID | 결정 | 날짜·근거 |
|---|---|---|
| Q1 A | 출력 Camera는 연결 때 `Camera.main`으로 한 번 받는다(StageMap·전투와 같은 출처). 없거나 Brain이 없으면 예외. SceneRoot의 `outputCamera` 칸 삭제 | 2026-10-24 사용자 |
| Q2 A′ | 아티팩트 팝업이 열린 동안만 출력 마스크에 UI 층을 더하고 닫을 때 복원. 주인은 아티팩트 팝업 | 2026-10-24 사용자 |
| (3판) | `LobbyCamera`를 고정하고 아티팩트 보드·배경 배치를 그 구도에 맞춘다. `LobbyOutputCamera`·`LobbyArtifactCamera` 삭제 | 2026-10-24 사용자 선택 |
| Q4 A | P1·P2 코드는 작업자, P3 씬 편집은 사용자(보드·배경 루트 배치는 위임), Phase마다 멈춤 | 2026-10-24 사용자 |
| (Volume) | `LobbyCamera`에 `CinemachineVolumeSettings` — `StageMapCamera`와 같은 `DefaultVolumeProfile.asset` | 2026-10-24 사용자 |

2판 상세 L1~L6 중 L1(출력 Camera 출처)·L2(배경 Canvas 런타임 연결)·L3(마스크)은 §6.1~§6.3에 남는다. 바뀐 것: Q3 블렌드는 A(Boot Brain Custom Blends 요청) → C(가상 카메라 하나를 `Customize`로 옮기기, 2판) → 고정 카메라 + 보드 재배치(3판, 2026-10-24). 2판의 L4(이동·구도 보관), L5·L6의 두 가상 카메라 전제, 카메라 계획의 LC5·LC10(Core 계약 LC와 다른 번호)은 3판으로 대체됐다.

### 3.4 실행 계층(Job·Plan 재구성)

결정 2026-10-23 사용자: "Q-LJ2는 A, 나머지는 권장대로".

| ID | 결정 |
|---|---|
| Q-LJ1 A | Stage 전환은 출발 연쇄의 마지막 Job(`LobbyTransitionJob`) 안에서 |
| Q-LJ2 A | 아티팩트 기록 누락은 D11·D20 유지: 오류 로그 + 아티팩트 도메인만 정지, 출발·이탈 허용(약속 5를 약속 6보다 앞에 둠) |
| Q-LJ3 A | 이탈 검사는 사실 기반 사전 검사(-200 노드, `LobbyPlan.CanLeave`) |
| Q-LJ4 A | Artifact 루트의 `ProcessCommand`·`ProcessPreview` 등록은 남기고 같은 규칙 함수를 쓴다(민형님 `JobSystemTests`). **2026-10-29 검사 후속 정리 R-f로 바뀜:** 근거였던 시험이 10-27 시험 묶음 삭제로 없어져 Artifact 루트·`ArtifactPlan.Register`·`ArtifactJob`과 등록 값 둘을 걷었다 |
| Q-LJ5 A | Lobby 루트의 `JobType.NewRun` 삭제, `Next`만 받음(천명님 `StageRunOriginRegressionTests` 수정) |
| Q-LJ6 B | 새 시험 없음. 깨지는 기존 시험만 맞추고 대상이 없어진 시험은 지움. 옛 Flow 시험의 규칙은 재생 확인 목록으로 |
| Q-LJ7 B | 판단은 등록 때. 실행 순간 판단 Job(`LobbyNextJob`)은 두지 않음(4판에서 A → B) |
| Q-LJ8 A | 출발 요청은 이름 있는 질문 넷 + `Queued`·`IsJobCompleted` 삭제 |
| Q-LJ9 B | 새 Run 성공 뒤 로스터 대조를 호출 직후 같은 Job 안에서(3판에서 A → B) |

Job·Plan P1~P10(2026-10-23 확정, P1·P3은 사용자 지시로 고침):
- P1 사실 읽기(`LobbyProgress`)와 아티팩트 정지(`LobbyArtifactDomain`)를 나눈다. 정지는 확인 Job만 한다.
- P2 아티팩트 호출 Job은 그대로, 확인은 새 `LobbyArtifactCheckJob`.
- P3 아티팩트 편집 실행 코드는 `LifeCycles/Lobby/Artifact/`로. `ArtifactLifeCycle`(예약 씬)은 `LifeCycles/Artifact/`에 남기고, 보드 초기화는 공용 준비로 뺀다. DB는 옮기지 않는다(§11.6).
- P4 View·UI는 요청 질문만 읽는다(`Status`는 public이라 규칙과 리뷰로 지킨다).
- P5 시작(`Created` → `Running`)은 `LobbyRunStartOutcome.TryStart` 한 곳(공용 `RunStartJob`이 부름, 2026-10-29 R-d1). 취소된 요청을 받은 Job은 아무것도 하지 않는다.
- P6 로스터 확인은 새 Run Job 안, 호출 직후.
- P7 `LobbyJob`·`JobType.Start` 로비 줄은 Framework 표본이라 그대로. **2026-10-29 R-f로 바뀜:** 운영에서 닿지 않고 실제 Job 예가 생겨 `LobbyJob`과 로비 Plan의 `Start` 갈래를 걷었다(사용자 결정).
- P8 전환 Job은 로비가 활성 구간인지 확인하고 아니면 throw.
- P9 Job은 자기 서비스만 생성자로 받는다(`LobbyPlan`이 꺼내 넘김).
- P10 시험 도우미(취소, 새 시험 없음).

## 4 시설 선택과 체스 이동

### 4.1 입력 경로와 포인터 규칙

1. 로비 ActionMap·Layer·Controller는 루트 Factory의 Configuration 생성 때 한 번 등록한다. Common 액션을 다른 Controller에서 다시 등록하지 않는다.
2. 3D 선택은 `UnityInputBridge → InputSystem.Submit → LobbyInputController → 수신 슬롯 → LobbyInteraction`으로 들어온다. `OnMouseDown`이나 오브젝트별 장치 폴링을 쓰지 않는다.
3. UI 판별은 공용 `ScreenUIPointerBlocker`(GraphicRaycaster) 한 곳이다. UI에서 누르기 시작한 입력은 끝까지 시설 클릭이 되지 않는다. 누름과 놓음이 같은 시설일 때만 이동한다.
4. 배경 UI와 로비 안내 글자는 Raycast를 받지 않는다.
5. 팝업은 화면 전체 uGUI 차단 영역과 InputSystem Layer 제한을 함께 쓴다. `ClearLayer`만으로 글로벌 버튼까지 막힌다고 보지 않는다.
6. `Picker`는 출력 Camera Ray로 수집한 시설 Collider 셋만 직접 Raycast한다. 일반 `Physics.Raycast`·모델 이름 검색·UI 버튼으로 바꾸지 않는다.

### 4.2 이동과 실행

- 캐릭터는 지금 칸에서 도착 칸까지 가로·세로로 한 칸씩 뛴다. 열과 행이 모두 다르면 도착 방향 축을 마지막에 걷는다. 경로·높이 식·구조는 §15가 원본이다. 보행·NavMesh·전투 칸 이동(Core 경로)을 쓰지 않는다.
- 뛰기 높이·한 칸 시간·회전 시간은 이동 SO 값이다(§5.1).
- 시간은 `IDelayService`의 `CurrentTime`·`AddDelayEvent(DelayDataHandler)`를 쓴다. TickAction에서 구간별로 보간하고 EndAction에서 최종 위치·완료를 확정한다. 따로 Update 타이머·Coroutine·Tween 패키지를 두지 않는다.
- 회전: 방향이 바뀌는 걸음 앞에서 그 걸음 방향으로 돌고, 마지막 착지 뒤 도착 방향과 다르면 한 번 더 돈다. 바라볼 점은 `도착 위치 + 방향 벡터`다. 캐릭터 정면은 이동 루트의 +Z다.
- 같은 도착 칸을 다시 고르면 이동 없이 완료 경계를 한 번 통과해 실행한다(방향은 맞춘다).
- 이동 중 다른 시설 클릭은 받지 않고 쌓지 않는다. 호버 강조(§16)도 끈다. 팝업이 열려 있거나 출발 대기 중이면 새 실행을 받지 않는다.

### 4.3 완료와 취소

- 취소·Dispose는 완료가 아니다. 시설을 실행하지 않고 마지막 정상 정착 위치와 회전을 복원한다.
- 표시 수명 토큰을 먼저 무효화한다. Delay에는 예약 하나를 지우는 API가 없어 취소된 예약은 원래 시간까지 남는다. 늦은 TickAction·EndAction은 토큰·연결·대상을 확인하고 아무것도 하지 않는다. 전역 Delay 큐는 비우지 않는다.
- EndAction은 완료만 기록한다. Scope의 정상 Tick이 완료를 한 번 소비해 `LobbyFacilityActions`를 부른다. 그때도 오류 정지·씬 이탈·모달 소유를 다시 확인한다. 이동 토큰은 게임 요청 번호가 아니다.
- 창을 닫은 뒤 캐릭터는 그 시설 앞에 남는다. 로비에 다시 들어오면 배치판 SO의 시작 위치·방향에서 시작한다(Q-L6). 이 위치는 표시 상태이며 Core 진행 상태로 저장하지 않는다.

## 5 배치·모델 View DB

### 5.1 역할별 SO 다섯

경로: 코드 `Systems/DataBaseSystem/Data/View/Table/LobbyVisual/Item/`, 에셋 `Assets/ProjectL/DataBaseSO/ViewGroup/LobbyVisual/Item/`. 각 SO는 `DatabaseItem`을 상속하고 `GetData()`가 매번 새 읽기 전용 Data를 만든다(DB 규칙 P2). View Data라 Unity 타입·프리팹 참조를 허용한다(P5).

| 역할 | SO → Data | 필드 | 소비자 | 검사 |
|---|---|---|---|---|
| 모델 | `LobbyModelDataSO` → `LobbyModelData(roomPrefab, floorPrefab, characterPrefab)` | `roomPrefab`·`floorPrefab`·`characterPrefab` | `LobbyVisualSet`·`LobbyCharacterVisual` | 방·바닥 필수, 프리팹 구조(§5.4·§5.5), 캐릭터는 활성·비씬 원본(`ValidateCharacterPrefab`) |
| 배치판·정착 칸 | `LobbyBoardDataSO` → `LobbyBoardData(columns, rows, cellSize, center, start, artifact, stageEntry, deck)` | `boardColumns`·`boardRows`·`cellSize`·`boardCenter`·`startCell`/`startFacing`·`artifactArrival`·`stageEntryArrival`·`deckArrival` | `LobbyBoard` | 행·열 1 이상, 칸 크기 유한 양수, 중심 유한, 모든 칸 범위 안, `Rotation4` 정의 값, 시설 도착 칸 셋 서로 다름(시작 칸과는 같아도 됨) |
| 이동 연출 | `LobbyMotionDataSO` → `LobbyMotionData` | `hopHeight`·`hopSeconds`·`turnSeconds` | `LobbyViewHopPath` | `LobbyMotionData.Validate`(높이 유한·0 이상, 한 칸 시간 유한 양수, 회전 시간 유한·0 이상) |
| 캐릭터 표시 보정 | `LobbyCharacterPresentationDataSO` → `LobbyCharacterPresentationData` | `characterOffset`·`characterYaw`·`characterScale` | `LobbyCharacterVisual` | 위치·Y 회전 유한, 균일 크기 유한 양수 |
| 시설 호버 빛남(§16) | `LobbyFacilityHoverDataSO` → `LobbyFacilityHoverData` | `textColor`·`glowColor`·`glowSize`·`glowSoftness`·`glowMaterial` | `LobbyFacilityHighlight` | `LobbyFacilityHoverData.Validate`(색 0~1, 크기·부드러움 0~1, 머티리얼 Underlay 켬), `ValidateLabels`(방 글자와 같은 셰이더) |

- 도착 칸은 시설마다 이름 있는 칸이다. 배열 순서로 대응하지 않는다. 칸은 Inspector에서 `Vector2Int`로 받고 `GetData()`에서 `CellCoord`로 바꾼다(`Arrival(CellCoord cell, Rotation4 facing)`).
- 각 역할은 자기 검사만 한다. Board 생성은 Board만, CharacterVisual 생성은 보정만 검사한다. DB 구성 오류는 역할·필드가 드러나는 `InvalidOperationException`이다.
- 로비 이동 값 조건의 원본은 `LobbyMotionData.Validate` 하나다(2026-10-29 §15). `ChessPieceMotionRules`(`Data/View/Movement/`)는 이제 StageSelect 들기 값(`ChessPieceMotionSettings`)만 검사한다.
- 누락을 `Default` Data나 옛 SO로 대체하지 않는다. 새 값은 다음 DB 초기화(새 Play)에 반영된다. 실행 중 SO 수정 감지·Query 재생성·프레임별 조회는 없다.
- 개명 이력: `LobbyVisual_Default.asset` → `LobbyModel_Default.asset`(GUID `64b4c66bbe2e17f449e843256009488f`), `LobbyVisualDataSO` → `LobbyModelDataSO`(스크립트 GUID `beb1e2c40b184eeebfe625fbd1fc2c66`, `MovedFrom`), `LobbyVisualData` → `LobbyModelData`, `LobbyLayoutData` → `LobbyBoardData`(GUID `784cf7f3678a4c56b8e59cf68c24c80d`). 옛 타입 alias·`Config`·Layout 묶음·자동 대체는 남기지 않았다.

### 5.2 테이블·Query·Inspector

```text
DatabaseCatalogSO → ViewGroup → LobbyVisualDataTable
                                ├─ LobbyModel_Default
                                ├─ LobbyBoard_Default
                                ├─ LobbyMotion_Default
                                └─ LobbyCharacterPresentation_Default
```

- 네 SO는 별도 `.asset` 파일이다. sub-asset이나 묶음 SO가 아니다.
- `ILobbyVisualTableQuery`는 `Models`·`Board`·`Motion`·`CharacterPresentation` 넷을 낸다. `LobbyVisualTable(IDataProvider<IDatabaseItem>)`가 목록을 한 번 돌며 SO 타입으로 정확히 하나씩 분류한다. null 행·중복·다른 타입·누락은 실패다. 이름·경로·인덱스·문자열로 역할을 찾지 않는다.
- 경로: `ViewDataBaseFactory → LobbyVisualTable.ExportServices → ViewDataBase.LobbyVisualTableQuery → LobbyViewSystem.InitData → BindData(models, board, motion, characterPresentation) → LobbyViewScope`. 서비스는 `ILobbyVisualTableQuery` 하나다.
- 테이블 원본은 Framework `DatabaseTable`의 `databaseItems` 목록 하나다. Framework는 바꾸지 않았다.
- Inspector `Systems/DataBaseSystem/Editor/LobbyVisualDataTableSOEditor.cs`는 역할 칸 넷(해당 SO 타입만, 씬 객체 불가)으로 같은 목록을 편집한다. 칸을 채우거나 비우면 그 역할 행만 더하거나 지운다. 중복·Missing/null·다른 타입이 있으면 역할 칸을 잠그고 원본 목록 편집을 보인다. 잘못된 행을 자동으로 지우거나 하나를 임의로 고르지 않는다. 다중 선택 편집은 없다.

### 5.3 배치판 칸과 방향

- 위치: `local = boardCenter + ((col - (boardColumns - 1) / 2f) × cellSize.x, 0, (row - (boardRows - 1) / 2f) × cellSize.y)`, `world = WorldRoot.TransformPoint(local)`. 열 → X, 행 → Z이고 바닥 타일 이름 `FloorTile_<열>_<행>`과 같은 순서다. 가운데 계산은 실수 나눗셈이다.
- 정착 높이는 `boardCenter.y` 하나다(타일 윗면 0.05와 다른 0.1).
- 방향: `Rotation4` × 90°를 배치판 로컬 Y축 회전으로 쓴다. R0 = +Z, R90 = +X, R180 = -Z, R270 = -X(위에서 시계 방향). `WorldRoot` 회전도 적용한다.
- `WorldRoot`는 같은 씬, 월드 축척 1, Y축 회전만 허용한다(`LobbySceneLayout` 검사).
- `LobbyBoard`는 연결 때 자세 넷(시작 1, 도착 3)을 계산해 보관한다. 클릭마다 계산·할당이 없다.

지금 값(원본은 `LobbyBoard_Default.asset`, 2026-10-27 대조):

| 항목 | 값 |
|---|---|
| 배치판 | 4×4, `cellSize` (4.25, 3.85), `boardCenter` (0, 0.1, -0.98) |
| 시작 | (1,2) R0 |
| 아티팩트(금고) 도착 | (0,2) R270 |
| 스테이지 입장(문) 도착 | (1,3) R0 |
| 덱(탁자) 도착 | (2,2) R90 |

바닥 모델(모델 SO)과 칸 규격(배치판 SO)은 다른 SO다. 타일과 규격을 자동 대조하지 않는다(이름·Mesh에 기댄 검사는 깨지기 쉽다). 바닥을 바꾸면 사람이 함께 맞춘다. 칸 검사는 화면 저작 오류 검사이며 Core의 배치·이동 판정을 대신하지 않는다.

### 5.4 방과 시설

```text
Lobby.prefab (저작 위치·회전·축척 유지, 지금 Y 270° · 축척 3)
├─ Artifact   LobbyFacilityView kind=Artifact   + BoxCollider
├─ StartStage LobbyFacilityView kind=StageEntry + BoxCollider
└─ Deck       LobbyFacilityView kind=Deck       + BoxCollider
```

- 운영 방은 `Assets/ProjectL/Prefabs/Lobby/Visuals/Lobby.prefab`(GUID `70cbd2cc01d789e4e98518a1e3241ef3`)이다. 옛 `LobbyRoom.prefab` 래퍼는 운영에서 참조하지 않으며 지우지 않았다.
- `LobbyFacilityView`의 직렬화 필드는 `kind`와 `label`(그 시설의 안내 글자, 2026-11-02 §16) 둘이다. 클릭 영역 `SelectionCollider`는 같은 객체의 BoxCollider이고, 시설을 고를 때(누름·뗌·호버)만 읽는다. 이름으로 찾지 않는다. `ValidateRoom`은 글자가 방 안의 활성 `TextMeshProUGUI`이고 두 시설이 같은 글자를 쓰지 않는지도 본다.
- **(2026-10-29 검사 후속 정리 R-e, TP-R13)** 시설 표지 `LobbyFacilityView`·`LobbyFacilityKind`는 DB 폴더 `DataBaseSystem/Data/View/Table/LobbyVisual/Item/`에 있다(GUID 그대로, `MovedFrom`). 방 원본 검사는 DB 생성 때 `LobbyVisualPrefabRules.ValidateRoom`이 한 번 한다. 비활성 자식까지 보고, 종류마다 하나·같은 객체의 활성 BoxCollider 하나·유한한 양의 크기·종류 중복·누락·시설끼리 중첩을 거부한다. View(`LobbyRoomBinding`)는 생성한 **그 인스턴스**에서 시설을 모으기만 하고 다시 검사하지 않는다. 원본의 Collider를 런타임 Picker에 넘기지 않는다. 기록은 [검사 후속 정리 계획 §6.8.1](../plans/2026-10-26-lobby-stage-review-cleanup-plan-Hojin.md#681-r-e-실행-기록-10-29-외부-빌드-오류-0경고는-바꾸지-않은-파일-셋뿐-unity-미확인-새-시험-없음)이다.
- 방은 저작한 루트 위치·회전·축척을 그대로 복제한다(유한한 변환, 모든 축 양수). 허용 컴포넌트는 정적 모델 넷(§5.5) + `LobbyFacilityView`와 그 객체의 BoxCollider다. 2026-11-02 사용자 지시로 방의 시설 안내 UI 구성(`RectTransform`·`Canvas`·`CanvasRenderer`·`CanvasScaler`·`GraphicRaycaster`·`TextMeshProUGUI`)도 허용한다. 허용 목록 밖의 MonoBehaviour·Rigidbody·카메라는 거부한다. 이 추가 허용은 로비 방에만 적용하며 바닥·다른 View 표에는 적용하지 않는다.
- 클릭 위치·크기는 시설 자식의 Transform과 BoxCollider Center/Size가 원본이다. DB나 씬에 사본을 두지 않는다. 클릭 영역을 옮겨도 도착 칸은 바뀌지 않는다.

### 5.5 바닥·정적 모델 규칙

로비 검사 원본은 `LobbyVisualPrefabRules`(`Validate`·`ValidateRoom`) 하나다. 다른 View 표와 같은 뜻의 검사(프리팹 에셋·활성 루트·identity 루트·정적 메시)는 공용 `ViewPrefabRules`를 부른다(2026-10-29 R-e DM2).

1. null·씬 인스턴스는 거부한다.
2. 바닥 루트는 활성·위치 0·회전 identity·축척 1이다(방만 저작 변환 허용). 자식 값은 유한하고 축척 축은 양수다. 축·크기 보정은 래퍼 프리팹의 자식에서 한다.
3. 기본 허용 컴포넌트는 `Transform`·`MeshFilter`·`MeshRenderer`·`LODGroup`이다. Missing Script·Collider·Rigidbody·Camera·Canvas·Audio·Light·임의 MonoBehaviour는 거부한다. 방에만 §5.4의 시설·안내 UI 추가 허용을 적용한다. 활성 MeshRenderer와 유효한 Mesh·Material이 하나 이상 필요하다.
4. Layer는 검사하지 않는다. 프리팹에 저작한 Layer를 그대로 쓰고, View가 Layer를 바꾸거나 더하지 않는다(2026-11-02 사용자 "Default로만 되게 하지 말고 다른 Layer들도", 예: 시설 안내 글자 `UIRender`(29)).
5. 빈 모델을 성공 처리하거나 임시 큐브를 자동 생성하지 않는다. Framework가 모든 프리팹에 이 제한을 요구하는 것은 아니다(로비 View 정책).

D2(2026-10-29 R-e로 바뀜): 모델 구조 검사는 DB 생성 때 한 번이다(옛 캐시 `_structureValidated`는 걷었다). View는 방·바닥 프리팹을 다시 순회하지 않는다. 그래서 같은 Play 안에서 Prefab Mode로 고친 구조는 다시 검사하지 않는다 — 프리팹이나 SO를 고친 뒤에는 새 Play로 확인한다.

### 5.6 캐릭터

```text
WorldRoot
└─ LobbyCharacterRoot        크기 1, 배치판 시작 위치·방향, ChessPieceMotion의 대상
   └─ LobbyCharacterVisual   표시 보정(위치·Y 회전·균일 크기)
      └─ <characterPrefab>(Clone)
```

- 원본은 모델 SO의 `characterPrefab`(지금 `Prefabs/Characters/Colored/Playerable/Akimbo.prefab`, GUID `836049dcf6ea66245ae56c7a5419936f`)이다. 전투용 UnitVisual과 로비 모델 선택은 서로 독립이다.
- 복제본 루트는 로컬 0/identity/1로 맞춘다. 로비 복제본만 Collider를 끄고 Layer를 Default로 바꾸며 Animator의 root motion·animation events를 끈다. Idle은 유지한다. 공유 원본·Mesh·Material은 고치지 않는다.
- `LobbyCharacterVisual`은 비활성 루트 아래 복제·정책 적용을 끝낸 뒤 Scope가 시작 자세를 적용하고 보인다. 연결 종료 때 먼저 숨기고, 입력·팝업·이동을 정리한 뒤 자기가 만든 이동 루트만 파괴한다. 재연결은 이동 루트를 공유하지 않는다.
- 지금 보정 값은 `characterOffset` (0, -0.08218316, 0), `characterYaw` 0, `characterScale` 2.146858이다(옛 루트 크기 2 × 표시 루트 보정을 합친 값).
- 실제 선택 캐릭터 자동 반영은 별도 계약이다(§13).

### 5.7 생성 수명

- `LobbyVisualSet.Create(WorldRoot, LobbyModelData)`가 방 → 바닥 순서로 `WorldRoot` 직속에 복제하고 이 연결이 만든 복제본만 소유한다. 부분 실패면 이미 만든 것만 비활성화·정리하고 예외를 넘긴다. 씬 자식을 일괄 삭제하거나 이름으로 생성물을 찾지 않는다.
- `Dispose`는 소유 목록을 먼저 끊어 재호출에 안전하고, 비활성화 뒤 파괴한다(Play는 `Destroy`, EditMode는 `DestroyImmediate`). 같은 프레임 재연결에서도 이전 소유자는 자기 복제본만 정리한다.
- 공유 프리팹·Mesh·Material을 고치거나 복제·파괴하지 않는다.
- 진입당 루트 몇 개라 GameObject 풀은 두지 않는다(Framework 지침 §16~19). Tick·포인터·이동 중 DB 조회·Instantiate·Material 복제가 없다. 성능 실측은 하지 않았다.

### 5.8 원본 표

| 사실 | 단일 원본 |
|---|---|
| 방·바닥·캐릭터 프리팹 선택 | 모델 SO |
| 방 외형·시설 종류·클릭 영역 | `Lobby.prefab` |
| 바닥 외형 | `LobbyFloor.prefab` |
| 배치판 규격·시작/도착 칸·방향 | 배치판 SO |
| 뛰기 높이·한 칸 시간·회전 시간 | 이동 SO |
| 칸 ↔ 위치 식 | `LobbyBoard` |
| 캐릭터 표시 보정 | 표시 보정 SO |
| 칸 좌표 기준·생성물 부모 | 씬 `WorldRoot`(`LobbyRoot`) |
| 카메라 구도 | 씬 `LobbyCamera` |
| 선택 캐릭터·덱·진행 | Core·Buffer(범위 밖) |

계산한 자세와 수집한 Collider 목록은 연결 수명의 파생 값이며 저작 원본이 아니다.

### 5.9 Core와의 경계

로비 배치는 고정 시설의 화면 배치다. 건물 설치·회전·저장이나 전투의 이동 판정이 아니다. 공용 `CellCoord`만 재사용하고, `TileFootprint`(Core.Domain)·`CombatPlacementCalculator`(전투 전용)·`ArtifactEditingService`(아티팩트 보드)를 로비 건물 배치에 쓰지 않는다. 화면 위치만을 위해 가짜 Core 명령·결과를 만들지 않는다.

시설 설치·이동·저장이나 로비 이동 가능 칸·해금·점유가 게임 규칙이 되면 SO 저작값만으로 넓히지 않는다. Core 작업자에게 보드·시설 키·확정 칸·플레이어 칸의 읽기 계약과 초기화·저장 범위를 요청하고, 확정 칸 원본을 Core Buffer로 옮긴다. 이 확장은 **조건부이며 미요청·미합의·미구현**이다.

## 6 카메라와 출력

### 6.1 구성

- 화면을 그리는 출력 Camera는 Boot의 `Main Camera`(CinemachineBrain + FMOD StudioListener) 하나다. `UnityCameraCatalogSystem`이 최초 바인딩 때 DontDestroyOnLoad로 유지한다. Lobby 씬에는 Unity Camera·Brain·MainCamera 태그가 없다(StageSelect·Combat과 같다).
- Lobby 씬의 가상 카메라는 고정 `LobbyCamera` 하나다(직교 크기 9, 타깃 없음). 팝업을 열고 닫아도 위치·회전·렌즈·우선순위를 바꾸지 않는다. `LobbySceneRoot.Validate`가 같은 씬·직교·Follow/LookAt 없음을 검사한다.
- `LobbyViewSystem`이 연결 때 `Camera.main`을 한 번 받아 Brain을 확인하고 Scope 생성 인자로 넘긴다(Q1 A). Scope는 피커·배경·아티팩트 팝업에 같은 Camera를 준다. Boot의 `RenderTexture Camera`도 MainCamera 태그를 갖지만 평소 Camera 컴포넌트가 꺼져 있어 로비에서는 `Main Camera`로 정해진다(민형님 `9b39cb02`). 태그 정리는 전투 화면 전환 중의 모호함용 선택 요청이다.
- `LobbyCameraController`가 `LobbyCamera`의 GameObject 이름으로 Catalog 핸들을 빌린다. 대여 시점은 AfterLoad 100이다(Catalog의 AfterLoad 재수집 `b308c88d` 뒤, [카메라 인계 §3](../handoffs/2026-10-11-stage-map-camera-framework-share-Hojin.md)). 매 프레임 `GetHandle`을 부르지 않는다. 핸들 `Dispose`는 반환이지 값 원복이 아니다. 씬 재바인딩 뒤에는 다시 빌린다.
- `LobbyCamera`에 `CinemachineVolumeSettings`를 붙여 `Assets/Settings/DefaultVolumeProfile.asset`을 참조한다(Focus Tracking None). 프로필 값은 복제하거나 고치지 않는다. 이것만으로 화면 흐림 해결을 증명한 것은 아니다.

### 6.2 배경 Canvas

- `LobbyBackgroundCanvas`(Screen Space - Camera)의 카메라 칸은 씬에서 비운다. 다른 씬의 Camera를 직렬화할 수 없기 때문이다.
- Scope 시작 때 `LobbyBackgroundView.ConnectOutput(Output, LobbyCamera near, far, IsCurrent)`가 `worldCamera = output`을 붙이고 폐기 때 원래 값으로 되돌린다.
- 참조·계층·Raycast는 연결 전에, 렌더 모드·표시 깊이(`planeDistance`가 `LobbyCamera` 렌즈 near/far 안)는 출력 Camera를 붙인 뒤 검사한다. 연결 뒤에도 Camera 모드가 아니면 오류이며 실패 시 원래 카메라로 되돌린다(사용자 재생에서 본 Overlay 상태에 따른 순서).

### 6.3 아티팩트 팝업 동안의 출력 마스크

Boot 출력 마스크에는 UI 층이 없고, 아티팩트 3D 보드는 UI 층이다. 그래서 팝업이 열린 동안만 UI 비트를 더한다(Q2 A′).

- `ArtifactPopupOutputMask`가 출력 핸들(`UnityCameraCatalogSystem`, 출력 이름은 받은 Camera의 `gameObject.name`)을 빌려 `Capture()` → `SetCullingMask(지금 마스크 | UI)` → 닫을 때 `Restore()` 후 반환한다. `"Main Camera"` 문자열을 로비에 두지 않는다.
- 열 때의 마스크를 저장하므로 전투 화면 전환이 남긴 마스크도 그대로 인수한다(`ScreenTransitionOutput`은 전환을 끝까지 마치면 마스크를 복원하지 않는다).
- 팝업 표시 뒤 Enter, 공통 숨김 정리의 맨 앞에서 Exit한다. Scope·Controller `Dispose` 입구에서도 같은 멱등 Exit를 먼저 부른다. 정상 닫기·열린 채 이탈·오류 정리·반복 해제가 같은 길이다. 실패한 Capture는 복원하지 않고 핸들만 반환한다. 현재 호스트만 Restore하고 교체된 호스트는 반환만 한다(`IsCurrent`·`IsSuperseded`).
- 모델·바닥이 Default 층이라 마스크를 UI 전용으로 바꾸지 않는다.
- 보류 A″: 팝업 동안 로비를 아예 그리지 않으려면 아티팩트 모델을 전용 층으로 옮겨야 한다. 정확성이 아니라 비용 문제라 Profiler로 본 뒤 정한다.

### 6.4 보드와 배경 배치

- 보드 배치의 원본은 씬 `ArtifactWorldRoot` Transform 하나다. 고정 `LobbyCamera`에 맞춰 위치·회전·균일 배율을 저작했다. `ArtifactBoardMapper`가 이 루트의 역변환으로 입력을 칸에 대응하고 모델·드래그 프리뷰도 같은 루트를 쓰므로 루트를 옮기면 표시와 입력이 함께 맞는다.
- 2026-10-30 사용자 결정: 아티팩트 화면도 기존 `LobbyBackgroundCanvas`를 사용한다. `ArtifactWorldBackground`와 전용 배경 배치 코드·설정값은 제거한다. 배경 SSOT는 로비 배경이며 보드 표시·입력 수명은 유지한다.
- `ArtifactPopupView`는 팝업 동안 `lobbyRoot`(= `WorldRoot`)만 숨긴다. 카메라·조명·UI·SceneRoot는 숨김 밖이다.

## 7 Canvas·글로벌 UI·설정

### 7.1 LobbyCanvas (C1·C2)

- 배경 Canvas는 `LobbyBackgroundCanvas`(Screen Space - Camera)를 유지한다. 2026-10-30 결정으로 아티팩트 전용 배경은 제거했다. 배경을 Overlay로 바꾸지 않는다.
- Overlay는 `LobbyCanvas` 하나, Sort Order 2, CanvasScaler Scale With Screen Size 1920×1080 · Match 0.5, GraphicRaycaster 하나다. 안내 TMP는 Raycast Target 끔.
- `LobbyCanvas → LobbyPanel / LobbyDeckPanel / ArtifactPopupPanel`. Panel 오브젝트에는 Canvas를 두지 않는다. 덱·아티팩트 프리팹 루트의 Canvas·Scaler·Raycaster는 지웠고 등록 루트는 전체 stretch 자식 RectTransform이다.
- 공용 Canvas는 입력·기능을 조립하지 않고 Core·Buffer를 조회하지 않는다. UIId·Policy·열기/닫기·`PopupViewScope` 수명은 각 UI가 그대로 갖는다.
- `LobbySceneRoot.ui`는 `LobbyCanvasView`(Canvas·배경·덱 UI 참조와 `ValidateComposition`)다. 참조 소유권은 [스테이지 맵 설계](2026-10-10-stage-map-design-Hojin.md)를 따른다.

### 7.2 글로벌 UI

- `GlobalUICanvas`는 Boot가 최초 `BindScene` 전에 한 번 만들고 DontDestroyOnLoad로 유지한다. `GlobalUIBase`는 최초 바인딩에서만 수집된다. **Lobby 씬에 GlobalUICanvas나 7·8번 UI를 다시 두지 않는다.**
- 씬별 글로벌 표시는 현강님 `GlobalUIPresentation`이 맡고 Bootstrap의 `OpenSceneUI`(200) 노드가 부른다. 로비는 `ShowLobby`(Setting·MainCharactor·AfterTitle 외 글로벌 닫기, 체력·골드 숨김, 명성치 표시)와 `Clicker`(시설 안내) 열기, Stage는 `ShowStageSelect`(글로벌 Deck을 다시 엶)다.
- 로비 Scope는 시작 때 글로벌 `UIId.Deck`(9, 보유 캐릭터 목록)을 닫는다. 로비의 덱 진입점은 6번 시설이라 글로벌 Deck을 같이 보이지 않는다. fault·Shutdown·실패한 이탈에서 무조건 다시 열지 않는다.

### 7.3 7번 얼굴

`MainCharactorUI`(`UIId.MainCharactor = 8`)가 HP·골드·Category를 이미 표시한다. 첫 Run 전 로비에는 선택한 메인 캐릭터 키를 읽을 계약이 없다(LC1). 얼굴을 얻으려고 새 Run을 미리 만들지 않고, 로비 얼굴만 Inspector에 따로 복사해 원본을 둘로 만들지 않는다. 연결 방식은 §13의 결정 대기다.

### 7.4 8번 설정

설정 UI는 UI 작업자(현강님) 담당이다(LV3 철회, 2026-10-12 사용자). 2026-10-27 코드 기준으로 현강님이 `OptionUI`(`UIId.Setting`, 일반·사운드·키 설정 하위 화면과 Run 포기·종료 버튼, 입력 Layer 덮어쓰기·오디오·시간 정지 포함)와 ESC 열기·닫기(`OptionInputRouter`, `Common/OpenSettings`)를 만들었다. **로비 View는 이 계약을 아직 인수하지 않았다.** 로비 쪽에는 설정 관련 코드가 없고, 설정을 열 때 로비 이동·미소비 완료·팝업 소유가 어떻게 되는지는 확인하지 않았다(확인 필요).

인수할 때 View가 사용자 후속 요청으로 맞출 요구는 다음과 같다(2026-10-11 설계, UI 쪽 실제 API로 다시 대조할 것).
1. 글로벌 설정 버튼(`GlobalUIBase`)과 일반 설정 팝업(`UIBase`)을 나눈다. 새 UIId는 기존 값을 바꾸지 않고 더한다.
2. 입력은 `UIInputBinding + IUIInputReceiver`. ESC는 Common Controller를 Factory에서 한 번 등록하고 같은 열기 경로로 보낸다. `OpenSettings`는 열기만 한다.
3. 설정을 열면 이동을 취소하고(마지막 정상 정착점 복원, 시설 실행 없음), 열린 시설 창은 Replace 사유를 먼저 알리고 `restore=false`로 끝낸 뒤 설정이 소유권을 갖는다. 설정을 닫아도 이전 시설 창을 자동 복원하지 않는다. 교체 중 Lobby 입력을 잠깐 켜지 않는다.
4. 첫 연결 범위는 로비의 유효한 입력 소유자가 있는 동안이다. 이탈 때 수신 슬롯을 끊는다. 상세 설정 항목·저장은 UI 작업자의 별도 설계다.

철회한 옛 타입·메서드 이름을 UI 작업자의 공개 계약으로 가정하지 않는다. 공용 UISystem 이력 재진입 문제(Hide 알림 안 동기 재열기 뒤 Back 이력 소실)는 View에서 우회하지 않는다 — [UI 작업자 인계](../handoffs/2026-10-12-lobby-settings-ui-history-handoff-Hojin.md)(미전달·미수정).

## 8 덱 팝업

LV4(2026-10-12)에 설정과 독립으로 만들었다. 실제 덱 후보·확정은 LV7(LC0·LC2)이다.

1. **연결:** 6번 정상 도착 → `ILobbyFacilityActions` → `LobbyDeckPopupController` → 공용 `PopupViewScope` → `PopupInputSession`·UI 서비스. 화면 열기에는 Core·Buffer·Plan·Job을 연결하지 않는다.
2. **입력·화면:** 모달 상태를 먼저 게시해 시설 입력과 미소비 이동 완료를 막은 뒤 Common만 허용하는 `LobbyDeckPopup` Layer를 얻는다. 전체 화면 uGUI 차단과 로비 Interaction 차단을 함께 쓴다. 창은 `UIId.LobbyDeck`(15) 일반 `UIBase`(`LobbyDeckUI`)이며 글로벌 Deck(9)과 따로다. 카메라는 고정 그대로다.
3. **표시:** 제목 `덱`·목록·비등록 `UnitCardView` 상세·빈 상태·닫기. 운영은 빈 목록과 `덱 정보를 준비 중입니다.`를 보인다. 후보를 View가 만들지 않는다. 고정(핀)은 Presenter 한 곳이 갖고, 고정 중 다른 항목 Hover는 선택을 바꾸지 않으며 다른 항목 Click은 전환, 같은 항목 재클릭은 해제한다. 목록의 고정은 덱 확정이 아니다. 확정·저장 버튼은 두지 않는다.
4. **정상 닫기:** 닫기 버튼(`UIInputBinding → IUIInputReceiver`) → `Controller.Close` → `PopupViewScope.Close`가 닫기 상태를 기록하고 `CloseUI`. 동기 Hide 알림에서는 표시·고정·입력만 정리한다. 서비스 호출이 돌아온 뒤 Scope가 같은 연결·건강 상태를 확인하고 자기 토큰으로 `End(token, true)`. 실제 Lobby Layer 복귀를 확인한 뒤 시설 차단을 푼다. 캐릭터는 도착 칸에 남는다.
5. **외부 숨김:** BackCloseUI·HideAll·다른 일반 UI 교체의 Hide는 알림 안에서 표시만 정리하고 다음 Tick에 `End(token, true)`. 다른 UI가 Layer를 가져갔으면 그 Layer와 시설 차단을 유지한다. 씬 이탈·fault·Shutdown은 `End(token, false)`로 복귀 없이 끝내고 새 소유자의 입력을 바꾸지 않는다. 사용자용 Back·ESC 경로는 등록하지 않았다.
6. **재진입:** 재바인딩·`OnDisable`의 동기 HoverExit 전에 이전 수신 상태를 무효화한다. Hide 알림 안에서 UI를 전환하거나 다시 열지 않는다. 이탈 때 이전 패널·토큰·구독을 정리해 다음 씬의 같은 ID를 옛 Controller가 닫지 않게 한다.
7. **버튼 바인딩(D1):** 닫기 버튼은 `this`로 한 번만 연결하고 목록 항목은 생성 때 한 번 `Bind`한다. 숨김은 `interactable = false`와 호버 정리만 한다. 전달 여부는 `CanReceive`가 막는다. 공용 `UIInputBinding`(현강님)은 고치지 않았다.

## 9 아티팩트 편집

### 9.1 로비 전용 편집 수명 (LV5)

- 4번 정상 도착이 UI 서비스로 아티팩트 팝업(`UIId.Artifact`, `ArtifactPopupUI`)을 연다. 팝업 열기 전용 버튼은 쓰지 않는다. 닫기·목록·호버 등 내부 입력은 그대로다.
- `ArtifactPopupViewFactory`가 Scope의 `PopupInputSession`·호스트·출력 Camera·`ArtifactPopupOutputMask`로 `ArtifactPopupController`를 만든다. 입력 Layer는 `ArtifactInputLayers.Artifact`(Artifact + Common). 닫기 복귀는 유효한 Lobby 입력 소유자에게 돌아간다. 종료·fault는 입력을 해제만 한다.
- 편집 소비자는 로비 하나다. StageSelect의 아티팩트 버튼·팝업·3D 보드·편집 실행 연결은 지웠다. Stage·Combat이 아티팩트 상태를 읽고 Core가 전투 효과를 적용하는 길은 그대로다. 공용 프리팹·에셋은 로비가 쓰므로 남긴다.
- 숨김 중 이미 제출한 편집을 보존하고 숨김 Tick에도 소유 결과를 정리하는 계약, 호버·프리뷰·오류 문구 3초 숨김은 그대로다([아티팩트 팝업 설계](2026-10-06-artifact-popup-design-Hojin.md)).
- 로비 Buffer 복사본(`LobbyArtifactBuffer`)이나 로비 전용 계산을 만들지 않는다. `ArtifactViewBuffer.Clear`는 입력·프리뷰를 비울 뿐 요청 번호를 되돌리거나 처리 완료를 만들지 않는다. 처리 완료는 Core 결과 번호로 확인한다.

### 9.2 보드 준비

- 게임 데이터 준비와 편집 UI 진입을 나눈다. `GameBootstrap`이 첫 구간(Lobby·Stage·Combat 직행 모두) 진입 전에 `IArtifactBoardPreparation.Request()`로 보드 초기화 Job을 걸고 `IsReady`를 확인한다. 코드는 `LifeCycleSystem/ArtifactPreparation/`(`ArtifactBoardPreparation`·`ArtifactBoardInitializeJob`)에 있다.
- 미초기화일 때만 한 번 준비하고 이미 준비된 보드·배치는 건드리지 않는다. Core `InitializeBoard`의 `false`는 "이미 초기화됨"일 수 있으므로 `Current.IsInitialized`와 함께 판단한다.
- 전투가 빈 `PlacedItems`도 읽으므로 "예외가 없다"로 준비 완료를 판정하지 않는다.

### 9.3 편집 실행

편집·프리뷰는 로비 실행 계층의 판단 표가 출발보다 먼저 처리한다(§11.3). 이탈은 처리 안 된 편집이 남으면 막는다(§11.3 `CanLeave`). 기록 누락은 아티팩트 도메인만 멈춘다(Q-LJ2 A, D11·D20).

## 10 Core 계약 (LC0~LC4)

원본은 이 절이다. [Core 경계 인계 §13.12](../handoffs/2026-09-24-view-core-data-boundary-handoff.md#1312-로비-core-계약-요청-2026-10-11-미전달)는 여기로 연결한다. **미전달·미합의**이며 이 작업으로 Core 파일은 고치지 않았다.

| ID | 요청·확인할 최소 계약 | 이미 있는 것과 인수 조건 | 상태 |
|---|---|---|---|
| LC0 | 로비 선택 덱의 의미와 수명: 고정 DB 로드아웃 선택인지 자유 편집인지, 첫 진입 기본값, 기존 Run에서 돌아온 경우, 확정값 유지 범위 | 디스크 저장·해금·영구 재화는 기획에 없어 넣지 않는다 | 미합의 |
| LC1 | 첫 Run 전에도 읽을 수 있는 메인 캐릭터 키·현재 선택 로드아웃/덱·준비 여부 | 현재 Run의 Owned/Main은 제공됨. 얼굴·3D에 필요한 키만 우선. 최대 HP·스탯 계산은 요청하지 않음 | 미합의 |
| LC2 | 덱 후보·편집/확정 입력·Core 검증·거절 사유·확정 상태 읽기 | DB `PlayerLoadout`과 옛 결과 타입만으로 운영 편집이 제공됐다고 보지 않음. 번호·문맥·취소·중복 방지·결과 보존을 합의한 뒤 Buffer와 담당을 나눔 | 미합의 |
| LC3 | 확정 덱을 새 Run 시작이 소비하는 방법, 성공·거절·실패 판별, 중복 실행 방지 | `StartNewRun(loadoutKey)`는 제공됨. 고정 DB 키 출발은 재사용(LV8A). 자유 편집 덱일 때만 추가 요청. UI가 로드아웃을 조립해 Core에 넣지 않음 | 미합의 |
| LC4 | 편집 진입점을 로비로 한정할 때 Run 전 초기화·편집 조건, 출발 뒤 배치·번호·전투 효과 보존, 직행의 같은 보드 준비 | 기존 계약 재사용으로 확정(§10.2) | 확정(2026-10-13) |

Core 회신에는 실제 파일·멤버·등록/조회 경로, 상태가 처음 유효해지는 시점, 명령·결과 문맥, 정상 거절과 예외의 구분, 초기화·취소·재진입 검증이 있어야 한다. 타입만 있고 실제 게시가 없는 상태는 계약 완료가 아니다. 회복 RC0·RC1과는 별개다.

### 10.1 LC4 준비 상태와 Core 확인 요청 (2026-10-13)

**판정:** 기존 Core 기능·공개 계약이 이미 있고, 새 Core 서비스가 꼭 필요한 공백은 없다. "Core 기능 미구현"으로 분류하지 않는다. 조사 기준은 `c772d9b8`과 당시 작업 트리다. 최신 번호 경로의 근거는 [P7 Core 교체 인계 §2~3](../handoffs/2026-10-11-stageselect-core-replacement-handoff-Hojin.md#2-실제-h1h2-계약)이다(View 쪽 로컬 구현, 외부 합의 기록 없음).

| LC4 항목 | 코드의 계약 | 근거 |
|---|---|---|
| 첫 Run 전 초기화·편집 | DB·Buffer 바인딩 뒤 보드 미초기화·`!Combat.HasBattle`이면 최초 초기화 가능. 편집은 초기화된 보드와 `!HasBattle`만 요구하고 로스터·선택 덱·Run·씬 이름을 보지 않는다 | `ArtifactEditingService`의 `InitializeBoard`·`ReadEditState`·`CanEdit` |
| 한 번 초기화 | 이미 초기화됐으면 정의를 다시 읽기 전에 `false`, 재고·배치 보존 | 같은 서비스, `ArtifactBoardInitializeJob` |
| 등록과 호출 | Factory가 편집 서비스 하나를 조립해 `IArtifactEditingService`·`IArtifactCommandProcessingService`로 등록. `CoreGroup.Artifacts`·`CoreGroup.ArtifactCommands` | `InGameCoreSystemFactory`, `InGameCoreSystemGroup` |
| 번호 입력·결과 | `ProcessNextCommand(N)`·`ProcessLatestPreview(N)`가 입력을 직접 읽고 게시 직전에 기대 번호를 다시 본다. `true` = 결과 게시(정상 거절·프리뷰 생략 포함), `false` = 없음·취소·교체·이미 처리, DB·계산·게시 오류는 예외 | `ArtifactCommandProcessing`, `ArtifactCommandResults` |
| 새 Run·번호 보존 | `StartNewRun`은 아티팩트 보유·배치와 입력·결과 번호를 초기화하지 않는다. 입력 `Clear`도 번호를 재사용하지 않는다. 번호 없는 `LastEditResult`로 완료를 판정하지 않는다 | `RunStartService`, `ProgressTransactions`, `ArtifactViewBuffer` |
| 전투 잠금·효과 | 보존 전투가 있으면 편집은 `LayoutLocked` 정상 거절. 전투 효과는 `PlacedItems`와 Core 정의를 읽는다 | 편집 서비스, `ArtifactLoadoutReader.Read` |

관련 기존 시험 소스: `ArtifactEditingServiceTests`, `ArtifactNumberedCoreTests`, `ChapterGraphPreparationTests`, `ArtifactBoardInitializeJobTests`(이 점검에서 재실행한 결과는 아니다).

Core 작업자에게 준비한 요청 요지(미전달): "현행 `InitializeBoard`·`ArtifactCommands`·결과 Buffer·`StartNewRun` 보존 경로를 로비에서 재사용할 수 있는지 확인 부탁드립니다. 같다면 '추가 구현 없음'으로 회신해 주시면 됩니다."
1. 첫 Run 전 초기화·번호 편집 가능, 같은 Buffer 수명에서 초기 지급 한 번, 로비 재진입·새 Run·챕터/전투 전환이 보유·배치·번호를 지우지 않는지. 앱 재시작·저장 파일은 범위 밖.
2. P7 인계의 기대 번호·직접 조회·게시 전 취소 재확인과 `true/false/예외` 의미를 유지하는지. 화면 닫기는 프리뷰만 취소하고 제출한 편집은 유지한다.
3. 로비에서 편집한 배치가 `StartNewRun` 뒤에도 `PlacedItems` 기반 전투 효과에 쓰이는지와 그 근거 위치.

`LobbyArtifactBuffer`, 새 편집·효과 계산 서비스, Core 내부 Store 접근은 요청하지 않는다.

### 10.2 LC4 기존 계약 재사용 확정 (2026-10-13)

사용자가 Core가 StageSelect에서만 편집하도록 제한하지 않음을 확인하고 **기존 계약 재사용으로 LV5를 진행**하도록 했다. `CanEdit`는 보존 전투 없음만 보고, 초기화·편집은 로스터·Run·씬 이름을 요구하지 않는다. §10.1의 초기화 1회·번호 처리·새 Run 보존·전투 효과 코드를 LV5의 사용 계약으로 인수한다. **Core 작업자의 외부 회신은 LV5 착수 조건이 아니다.** 실제 Core API·의미 변경이 필요해질 때만 그 부분을 요청한다.

LV5는 Buffer·Core 계산을 그대로 두고 준비 시점, 단일 편집 실행, 시설 도착 뒤 팝업 열기, 씬·카메라 소유만 옮겼다. LV5 결과는 [로비 계획](../plans/2026-10-11-lobby-plan-Hojin.md)을 따른다. LC0~LC3은 각 후속 기능의 인수 조건으로 남는다.

## 11 실행 계층 (LifeCycle·Plan·Job)

2026-10-23 재구성(4판, 구현 완료). 옛 `LobbyFlow`·`ArtifactEditFlow`·`LobbyDepartureFlow`를 지웠다. 기준 형태는 전투(`CombatPlan.Decide`)·튜토리얼(`TutorialPlan.Decide`)·용근님 전투 View 구조 개선안 2판(`9575001`)이다. 사용자 목표는 "문제가 난 스크립트를 바르게 고쳐도 위·아래가 오류를 내는 것을 줄이고, 책임을 분명히 하는 것"이다. Core는 바꾸지 않았다.

### 11.1 자리와 약속

Framework 골격은 `LifeCycle → (Flow) → JobRegister(루트, 종류) → Plan → Job`이다. 자리마다 답하는 질문은 하나다.

| 자리 | 답하는 질문 | 로비에서 |
|---|---|---|
| LifeCycle | 이 구간이 살아 있나, Runner가 비었나, 판단을 걸까 | 트리거와 이탈 검사 호출만 |
| Plan | 지금 사실로 무엇을 할까(상태 없음) | `Decide`·`CanLeave`, 단계 Job 목록 |
| Job | 부르고, 그 호출의 약속을 확인 | 호출 직후 같은 Job 안에서 확인 |
| Buffer | 무슨 일이 있었나, 무엇을 원하나 | 바깥에는 입력과 결과만, 실행 단계는 질문으로 감춤 |
| View | 보여 주고 입력을 Buffer에 씀 | 질문만 읽음 |

| # | 약속 | 주인(한 곳) |
|---|---|---|
| 1 | Framework 계약: 훅은 구간 수명용, Job `IsCompleted` 의미, 서비스는 제공 인터페이스로, 이탈 노드 순서 -200 → -100 → 0 | `LobbyLifeCycle`, 각 Job |
| 2 | 로비 출발은 항상 새 Run, 로드아웃 1 | `RunStartJob`(키 `RunStartJob.DefaultLoadoutKey`, 2026-10-29 R-d1) |
| 3 | 출발 뒤 새 Run 결과대로 로스터가 게시돼 있다(Stage는 `Owned.Count == 0`이면 새 Run을 또 만든다) | `RunStartJob`의 호출 직후 확인 |
| 4 | 출발 중엔 입력이 막히고, 거절·취소면 다시 고를 수 있다 | 요청 질문 `IsPending`·`CanResubmit` |
| 5 | 남은 편집을 버리고 떠나지 않는다. 아티팩트가 멈춰도 떠날 수 있다(D11·D20) | `LobbyPlan.CanLeave`, `LobbyArtifactCheckJob` |
| 6 | 오류는 원인 자리에서 throw한다 | 확인은 호출 직후 같은 Job 안(R4) |
| 7 | 지금 출발·아티팩트 동작 규칙(옛 Flow 시험이 지키던 것) | `Decide`·`CanLeave`·Job, 재생 확인 |
| 8 | `LobbyJob`·`JobType.Start`는 Framework 표본 | 그대로 둠 → 2026-10-29 R-f에서 걷음 |

약속 5와 6은 아티팩트 기록 누락 한 곳에서 부딪힌다. Q-LJ2 A로 5를 택했다. 로비에서 확인 실패를 throw 대신 오류 로그로 끝내는 곳은 이 한 곳뿐이다.

### 11.2 파급을 끊는 규칙

| # | 규칙 | 로비에서 |
|---|---|---|
| R1 | 판단 한 곳 | `Decide`·`CanLeave`, 아티팩트 선택 규칙 `ArtifactEditFacts` |
| R2 | 상태 하나에 쓰는 주인 하나. 요청 단계는 실행 Job만 바꾸고 전이는 요청 안 표 하나 | `MarkQueued` 악수 삭제, 시작은 `TryStart` 한 곳 |
| R3 | 경계는 이름 있는 질문 | 출발 요청 질문(Q-LJ8) |
| R4 | 확인은 호출 직후, 그 호출의 계약만 | 로스터 대조를 새 Run Job 안으로, 아티팩트 기록 확인을 호출 바로 뒤 Job으로 |
| R5 | 일시적 조건은 기다리고 계약 위반만 예외. 다른 루트의 Runner 사용은 예외 사유가 아님 | 진입 때 기다림(B5), 이탈 검사는 로비 자기 일만(B4) |
| R6 | 실패의 범위는 그 도메인 | `LobbyArtifactDomain` |
| R7 | 새 시험 없음. 남는 시험은 결정·관찰 가능한 결과만 | Q-LJ6 B |
| R8 | 조립 한 곳, Job은 자기 서비스만 | Job·Plan P9 |

끊은 파급 고리(옛 Flow 구조): C1 출발 등록 순서를 세 파일이 서로 확인, C2 View가 원시 단계를 해석, C3 같은 편집 선택 조건이 Flow·Plan 두 곳, C4 Flow가 처리 번호 사본을 Tick 너머로 보관, C5 호출 확인이 다른 클래스·다음 Tick, C6 전역 `IsIdle`을 "내 일 끝남"으로 씀, C7 시험이 구현 내부를 단언, C8 의존 하나가 Flow·LifeCycle·시험 고정물까지 번짐. 대표 변경 시나리오 S1~S7의 비교는 git `10914570`의 옛 재구성 설계 §9.2에 있다.

### 11.3 판단과 이탈 조건

`LobbyLifeCycle`이 Runner가 빈 Tick에 `Register(Lobby, Next)`를 건다. `LobbyPlan.Next`가 `LobbyProgress.Read()`로 사실을 읽고 `Decide`로 단계 하나를 골라 단계 Job 목록을 바로 건다. 목록은 다음 Runner Tick에 등록 순서대로 이어 실행된다.

`Decide` — 위에서 처음 맞는 줄 하나(아티팩트가 출발보다 먼저):

| 순서 | 조건 | 단계 |
|---|---|---|
| 0 | 로딩 중 | 없음 |
| 1 | 아티팩트 정지 아님 · 보드 초기화됨 · 처리 안 된 편집 N 있음 | 편집(N) → `[ArtifactCommandJob(N), LobbyArtifactCheckJob(편집, N)]` |
| 2 | 아티팩트 정지 아님 · 보드 초기화됨 · 답 안 된 프리뷰 N 있음 | 프리뷰(N) → `[ArtifactPreviewJob(N), LobbyArtifactCheckJob(프리뷰, N)]` |
| 3 | 아티팩트 정지 아님 · 처리 안 된 편집 있음(보드 미초기화) | 없음(기다림) |
| 4 | 출발 요청이 없거나 대기 중이 아님 | 없음 |
| 5 | 출발 요청 `Created` | 출발 → `[RunStartJob(LobbyRunStartOutcome), LobbyTransitionJob]` |
| 6 | 그 밖의 대기 단계(`Running`·`Prepared`·`Transitioning`) | 어긋남 → 예외(공용 오류 경계) |

등록과 실행 사이 한 프레임에 Delay·입력·UI가 돌 수 있지만 로비에서는 결과가 같다. 아티팩트 입력이 바뀌면 Core가 기대 번호로 다시 확인해 게시하지 않고 확인 Job이 정착시킨다. 출발이 취소되면 `TryStart`가 `Cancelled`를 보고 Core를 부르지 않으며 전환 Job도 그냥 끝난다(P5). 출발 대기 중에는 시설·팝업 입력이 막혀 새 편집이 없다. 그래서 실행 순간 판단 Job은 두지 않는다(Q-LJ7 B).

`CanLeave` — 이탈 조건의 유일한 정의. -200 노드에서 거짓이면 예외로 이탈을 막는다.
- 아티팩트: 정지했거나 처리 안 된 편집이 없다. 프리뷰는 막지 않는다.
- 출발: 요청이 없거나, 대기 중이 아니거나, `Transitioning`이고 새 Run이 성공했다.
- Runner가 비었는지는 보지 않는다(R5). 다른 루트의 Job은 그 루트가 책임진다.

### 11.4 Job

| Job | 호출 | 같은 실행 안의 확인 | 실패 |
|---|---|---|---|
| `ArtifactCommandJob` | `ProcessNextCommand(N)` | — | Core 예외 → 공용 오류 |
| `ArtifactPreviewJob` | `ProcessLatestPreview(N)` | — | Core 예외 → 공용 오류 |
| `LobbyArtifactCheckJob` | 없음 | 호출 뒤에도 같은 N이 "처리 안 된 다음 편집/프리뷰"로 남으면 기록 누락 → 도메인 정지 | 오류 로그 + 아티팩트 도메인만 정지, 이탈·출발 허용 |
| `RunStartJob`(로비 결과 `LobbyRunStartOutcome`) | `StartNewRun(RunStartJob.DefaultLoadoutKey)` | 호출 전: 전투 없음. 성공이면 호출 직후: 결과와 게시된 로스터·메인 상태 대조(Q-LJ9 B) | 예외 → 요청 `Faulted` → 공용 오류 |
| `LobbyTransitionJob` | `LifeCycleManager.Change(Stage)` | `Prepared`·성공이고 로비가 활성 구간일 때만(P8) | `Rejected`·`Cancelled`는 전환 없이 완료. 예외 → `Faulted` |

기록 누락 판정은 "호출 뒤에도 같은 N이 처리 안 된 다음 요청으로 남아 있다" 한 줄이다(옛 두 조건과 같은 뜻).

### 11.5 사실 묶음 `LobbyProgressFacts`

`readonly struct`라 매 Tick 읽어도 할당이 없다. 칸을 더해도 위·아래를 깨지 않는다.

| 사실 | 원본 |
|---|---|
| `IsLoading` | `ILodingSystemService.IsLoading` |
| `IsBoardInitialized` | `ArtifactReadSink.Current.IsInitialized` |
| `IsArtifactStopped` | `LobbyArtifactDomain`(진입 때 false) |
| `ArtifactCommandNumber` | 처리 번호 다음 첫 편집 입력의 번호, 없으면 0 |
| `ArtifactPreviewNumber` | 답 안 된 프리뷰 번호, 없으면 0 |
| `Departure` | `LobbyViewReadSink.Current` |
| `HasBattle` | `CombatStateReadSink.HasBattle`(전투 골격의 판단을 거치지 않음) |

두 아티팩트 번호는 공유 규칙 `ArtifactEditFacts` 하나로 계산하고 로비 아티팩트 도메인이 그 함수를 쓴다(R1). 같은 함수를 쓰던 `ArtifactPlan.Register`는 2026-10-29 R-f에서 걷었다.

### 11.6 수명과 조립

| 시점 | 하는 일 |
|---|---|
| `OnEnter` | 종료·오류면 예외. `LobbyArtifactDomain` 정지 해제. Runner가 바빠도 예외가 아니다(B5) |
| `OnTick` | 오류면 등록하지 않음. Runner가 비면 `Register(Lobby, Next)` |
| `OnExit` | 할 일 없음 |
| `OnShutdown` | 종료 표시만 |
| 이탈 노드(-200) | `CanLeave`가 아니면 예외 |

- `LifeCycleSystemFactory`가 `LobbyArtifactDomain`(오류 로그 함수를 받음)과 `LobbyProgress`를 만들어 서비스로 등록하고 `LobbyLifeCycle` 생성자로 넘긴다. 키는 `ILobbyProgressReader`·`ILobbyArtifactChecks` 두 줄, 그룹 속성 두 줄이다.
- `ILobbyArtifactChecks`는 확인 Job을 만드는 `CreateCheck(종류, 번호)`만 낸다. 정지·해제·정지 여부는 어떤 인터페이스에도 없다. 해제는 구체 객체를 받은 `LobbyLifeCycle`, 정지 여부 읽기는 `LobbyProgress`만 한다(P1).
- `JobRegisterSystem`의 로비 줄은 `LobbyPlan.Register(service, jobType, buffers, lifeCycles)`다. Runner는 넘기지 않는다. Artifact 루트는 2026-10-29 R-f에서 걷었다(Q-LJ4 바뀜).
- 파일 위치: `LifeCycles/Lobby/`(`LobbyLifeCycle`·`LobbyProgress`·`LobbyProgressFacts`, `Plans/LobbyPlan`·`LobbyStep`, `Jobs/LobbyRunStartOutcome`·`LobbyTransitionJob`), `LifeCycles/Lobby/Artifact/`(`ArtifactEditFacts`·`LobbyArtifactDomain`, `Jobs/ArtifactCommandJob`·`ArtifactPreviewJob`·`LobbyArtifactCheckJob`, `Plans/ArtifactPlan`).
- 아티팩트 DB는 옮기지 않았다. DB 그룹은 InGame·View·Render 셋뿐이고, 아티팩트 정의(`ArtifactDefinitionDataTable`)는 전투도 읽는 규칙 값이다. 보드 처음 크기(`ArtifactBoardConfigDataTable`)는 `IHasArtifactBoardConfig`로 따로 노출된다(2026-10-06 결정). 외형·팝업 표는 로비만 읽는 View 표다.

### 11.7 전투 개선안과의 관계

흐름은 전투와 같고 디테일은 로비에 맞게 다르다(4판, 사용자 지시 "전투와 흐름은 같게, 디테일까지 같을 필요는 없다").

| 전투 개선안 | 로비 | 이유 |
|---|---|---|
| 실행 순간 판단 `CombatNextJob`, JobRegister가 Runner를 넘김 | 쓰지 않음. 등록 때 판단(Q-LJ7 B) | 틈이 결과를 바꾸지 않는다(§11.3) |
| 걸음 재료 `CombatStepContext` | 쓰지 않음. Job마다 자기 서비스만(P9) | 로비 Job은 넷뿐 |
| 사후 확인 Job `ExitCheck` | 사전 검사 `CanLeave`(Q-LJ3 A) | 남은 편집을 버리는 이탈을 먼저 막는다 |
| 사후 확인은 다음 판단이 기대는 사실만(P15) | 같은 방향: 호출 직후 그 호출의 계약만(R4) | 원인 자리에서 throw |
| 전투 이탈에서 새 Run 빼기 | 로비 출발이 조건 없이 새 Run을 만든다는 것이 Run 종료 흐름의 전제(약속 2) | — |
| 진입 요청 이름 있는 질문 | 출발 요청도 질문(Q-LJ8) | — |

"Run 진행 중이면 새 Run 생략" 같은 조건을 로비 출발에 더하면 전투 쪽이 깨진다. 이 약속은 이 문서와 전투 설계 J7 두 곳에 적고 서로 가리킨다.

### 11.8 바뀐 동작 (재구성 전 → 뒤)

| # | 항목 | 지금 |
|---|---|---|
| B1 | Stage 전환 때 Runner | 출발 연쇄의 마지막 Job 실행 중(옛: 비어 있음). Stage `OnEnter`는 비동기 로드 뒤라 그때는 비어 있다 |
| B2 | 새 Run → 전환 간격 | 같은 Runner Tick(옛: 다음 Tick). 화면 차이는 이것 하나(로딩 시작이 한 프레임 빨라짐) |
| B3 | 아티팩트 기록 확인 | 호출 직후 확인 Job(옛: 다음 Tick Flow) |
| B4 | 로비 이탈 검사 | 로비 자기 일만(옛: Runner 전체가 비어야 함) |
| B5 | 진입 때 Runner가 바쁨 | 기다림(옛: 예외) |
| B6 | 등록 뒤 지워진 편집 | 확인 Job이 정착시키고 출발을 막지 않음 |
| B7 | 오류·비정상 이탈 때 출발 요청 | LifeCycle은 요청을 바꾸지 않음. 시작 전 요청은 View Detach가 취소, 실행 중 예외는 Job이 `Faulted` |
| B8 | 남은 전투 | 새 Run Job의 호출 전 예외(옛: 등록 전 Flow 예외) |
| B10 | 취소된 출발의 입력 해제 | 취소하면 바로 `CanResubmit`(옛: Job 배출까지 대기). 운영에서 취소는 연결 해제·교체 때뿐 |
| B11 | 새 Run 뒤 로스터 확인 | 같은 대조를 호출 직후 같은 Job 안에서 |

(B9는 4판에서 삭제했다.)

## 12 출발·Run 종료·Run 결과

### 12.1 출발 요청 계약

`LobbyViewBuffer` 한 객체를 `ILobbyViewBufferReadSink`·`ILobbyViewBufferWriteSink`로 등록하고 `LobbyViewBufferKey.Read/Write`로 조회한다(`IBufferQuery` 접근자 `LobbyViewReadSink/WriteSink`). `LobbyDepartureRequest`는 번호가 있는 실행 의도와 이번 호출 결과만 보관한다. 게임 상태·아티팩트 효과는 Core·Buffer가 계속 소유한다.

바깥(View)이 읽는 것은 질문 넷이다.

| 질문 | 뜻 |
|---|---|
| `IsPending` | 처리 중이라 입력을 막아야 함 |
| `CanResubmit` | 거절·취소로 끝나 다시 제출할 수 있음 |
| `IsRejected` + `Result.FailureReason` | 정상 거절과 그 사유 |
| `IsFaulted` | 오류로 끝남 |

- 단계: `Created`(제출) → `Running`(새 Run 호출 중) → `Prepared`(성공) / `Rejected` → `Transitioning` → `Completed`, 그리고 `Cancelled`·`Faulted`. `Queued`·`IsJobCompleted`·`MarkQueued`·`CompleteJob`은 지웠다(P5).
- 단계를 바꾸는 곳: `TryCancel`(View, `Created`만) / `TryStart`·`PublishResult`·`Fail`(새 Run Job) / `BeginTransition`·`CompleteTransition`·`Fail`(전환 Job). 전이는 요청 안 표 하나가 정한다. `Fail`은 끝나지 않은 모든 단계에서 받는다.
- View·UI는 `Status` 단계 이름을 직접 비교하지 않는다(P4).
- pending 중 제출은 같은 요청을 돌려주고, 완료·거절·취소 뒤에는 새 번호를 발급한다.

### 12.2 출발 정책 (LV8A)

- 스테이지 입장 시설 정상 도착 → 출발 제출 한 번 → 처리 중 시설 입력 차단 → 남은 아티팩트 편집·프리뷰 먼저 → `StartNewRun(1)` → 로스터 대조 → `Change(Stage)` → Stage의 챕터 준비 → StageSelect.
- 기존 Run에서 돌아왔어도 다음 출발은 새 Run이다. 이어하기가 아니다. 키 원본은 `StagePlan.DefaultLoadoutKey`(= 1) 한 곳이다.
- Stage는 로스터가 있으면 새 Run을 다시 만들지 않아 새 Run과 챕터 진입이 각각 한 번이다.
- 정상 거절은 로비 상태와 입력으로 돌아간다(자동 재시도 없음). 진행 중·보존 전투가 남으면 새 Run Job이 Core 호출 전에 예외를 던져 공용 오류로 멈춘다. 실행 전 취소는 Core 호출을 막지만 이미 시작한 새 Run을 취소했다고 표시하거나 되돌리지 않는다.
- 아티팩트 보유·배치와 전투 효과는 새 Run에도 남는다. 로스터·진행·Run 재화는 Core 새 Run 계약으로 초기화된다.
- 출발 진행·거절 안내 문구는 지금 화면에 없다(2026-10-20 `3bc33b20` 임시 조치부터). 참조가 없던 `LobbyDepartureText`·`LobbyDepartureStatusUI`는 2026-10-27 사용자 결정으로 지웠다(`b1dd4962`). 다시 만들 때의 규칙은 [검사 후속 설계](2026-10-26-lobby-stage-review-cleanup-design-Hojin.md)를 따른다.

### 12.3 Run 종료와 로비 복귀

- Run이 끝나면(일반 패배·자발적 포기·최종 승리) 전투가 로비로 보낸다. 목적지 코드는 `CombatPlan.DestinationOf` 한 곳이고 정책 원본은 [전투 J7](2026-10-10-combat-job-plan-restructure-design.md#j7-이탈-연쇄-k21-유지)이다. Run이 이어지는 일반 승리만 StageSelect다.
- 새 Run은 로비 출발 한 곳이 만든다(약속 2). 전투 쪽 새 Run은 2026-10-22 전투 구조 개선에서 걷었다.
- 로비 재진입 때 캐릭터는 배치판 시작 자세에서 시작하고, 아티팩트 도메인 정지는 풀린다.

### 12.4 Run 결과 화면

- 최종 승리의 지난 Run 결과(사용 골드·명성·처치 수)는 전투 씬의 `EndingPanel`(`EndingRunSummaryUI`, `UIId.EndingRunSummary = 33`)이 보이고, 닫으면 로비로 간다(RS-C1, 2026-10-20 사용자 확정). 최종 여부와 통계는 Core가 게시한 `LastSummary`만 쓴다.
- 로비 도착 때 자동으로 보이던 RS-L1(로비 모듈·`EndingRunSummaryPopup` Layer·호스트 노출)은 걷었다. 로비 씬에 남아 있던 `EndingPanel` 인스턴스는 2026-10-26 사용자 요청으로 지웠다. 원본 프리팹 `Prefabs/UI/EndingPanel.prefab`과 GUID는 그대로다.
- 실제 최종 보스 처치 → 엔딩 → 로비 복귀 전체 경로는 미확인이다(계획).

## 13 보류·결정 대기

| 항목 | 지금 | 다음 조건 |
|---|---|---|
| LV6 얼굴·실제 선택 캐릭터 | 로비 모델은 모델 SO의 Akimbo 고정. 옛 계획은 얼굴·3D를 같은 선택 키로 연결하는 안이었다 | **얼굴만 연동할지, 모델도 선택에 따라 바꿀지 먼저 정한다**(2026-10-25 정리). 그 뒤 LC0·LC1 |
| LV7 덱 후보·선택·확정 | 덱 팝업은 빈 목록·준비 안내 | LC0 정책 + LC2 계약 확인 뒤. 새 Core API가 실제로 필요할 때만 요청 |
| LV8B 선택 덱 출발 | 기본 로드아웃 출발만 | LV7 + LC3. LV8A 수명 재사용 |
| LV9 통합 인수·문서 | 미착수 | LV5~LV8 뒤 |
| 설정 UI 인수 | UI 쪽 `OptionUI`·ESC는 있음, 로비 View 연결 없음 | 실제 API 대조 + 사용자 후속 요청(§7.4) |
| 출발 진행·거절 안내 | 화면에 없음 | 필요하면 요청 질문으로 문구를 고르는 별도 연결 |
| 로비 상태가 Core 규칙이 되는 확장 | 해당 없음 | §5.9의 Core 요청(미요청) |
| A″ 팝업 동안 로비 안 그리기 | 로비를 가린 채 그림 | Profiler로 비용을 본 뒤 |
| 정적 Mesh 밖 모델(애니메이션 시설·파티클), 여러 테마·런타임 스킨·Addressables | 지원 안 함 | 실제 요구 때 별도 설계 |
| 씬 뷰 배치판 미리보기, 바닥 타일 ↔ 배치판 자동 대조 | 없음 | 범위 밖 |
| `LobbyDeckUI.OnDisable → Hide` 직접 호출, 시설 선택 이펙트 | 그대로(호버 글자 빛남은 §16) | 별도 결정(3부 범위 밖으로 남긴 것) |
| 2번 모델 클릭 기능, 덱 편집 상세, 설정 항목 | 기획 미확정 | 기획 확정 뒤 |

## 14 다른 작업자 영향

| 작업자 | 무엇을 바꿨나 | 공유 |
|---|---|---|
| 민형님(Framework) | Job·Plan 재구성: `JobRegisterSystem`의 로비 줄·Artifact `using`, `LifeCycleSystemServiceKey` 두 줄, LifeCycle 그룹 속성 두 줄. LV4: 입력 Factory에 `LobbyDeckPopup` 등록 한 줄(2026-10-12 승인). 카메라는 공개 핸들만 쓰고 Boot·Framework 카메라 구현은 바꾸지 않음 | [Job·Plan 공유](../handoffs/2026-10-23-lobby-job-plan-framework-share-minhyeong-Hojin.md)(미전달), [카메라 인계 §9](../handoffs/2026-10-11-stage-map-camera-framework-share-Hojin.md)(미전달) |
| 용근님(전투) | 겹치는 파일(`LifeCycleSystemFactory.CreateComposition`, `JobRegisterSystem.Initialize`, `LobbyDepartureIntegrationTests`), 로비가 진입 때 Runner를 요구하지 않게 됨(B5). 출발 안내 파일 삭제 때 용근님 2줄(시작 덱 거절 문구) 함께 지움 | [Job·Plan 공유](../handoffs/2026-10-23-lobby-job-plan-combat-share-yonggeun-Hojin.md)(미전달), [참조 없는 파일 삭제 공유](../handoffs/2026-10-27-unused-lobby-stage-files-removed-share-Hojin.md)(미전달) |
| 천명님(Core) | Core 파일 무변경. `StageRunOriginRegressionTests` 두 사례를 Q-LJ5 A·Q-LJ8 A에 맞춤. LC0~LC4 요청은 준비만 | [Core 전달 문서](../handoffs/2026-09-24-view-core-data-boundary-handoff.md) §5.51·§13.12(미전달) |
| 현강님(UI) | 공용 `UIInputBinding`·`GlobalUICanvas`·UISystem 무변경. 설정 UI는 현강님 담당. 글로벌 로비 표시는 현강님 `GlobalUIPresentation`이 원본 | [UI 이력 인계](../handoffs/2026-10-12-lobby-settings-ui-history-handoff-Hojin.md)(미전달) |

## 15 칸마다 뛰는 이동 (HM1~HM6, 2026-10-29 초안)

상태: 구현 완료(2026-10-29, HM-a·HM-b Unity 확인 문제 없음 — 사용자). 결정은 Q-HM1~Q-HM3 모두 A(사용자 "모두 A")다. 진행 결과는 [로비 계획 §3.23](../plans/2026-10-11-lobby-plan-Hojin.md#323-hm-칸마다-뛰는-이동), 구현 메모는 §15.9다.

### 15.1 요청과 지금

- 사용자(2026-10-29): 지금 로비 이동은 칸을 무시하고 도착 칸까지 바로 간다. 이를 전투처럼 **한 칸 한 칸 통통 튀는 느낌**으로 바꾼다. 뛰기 높이 식은 Common으로 공유하지 않고 **로비 쪽에 한 줄**로 둔다(사용자 결정).
- 이 요청은 §1.2의 2026-10-11 확정("들림 → 이동 → 내려놓기")에서 이동 모양만 바꾼다. "전투 이동 규칙이 아니다"는 그대로다. 로비 경로는 화면 표시이고 Core에 묻지 않는다(§5.9). 칸 점유·막힌 칸·이동 가능 칸 같은 규칙도 만들지 않는다.
- 지금 동작
  - `LobbyInteraction`이 `ChessPieceMotion.Begin(도착 위치, 바라볼 점)`을 한 번 부른다.
  - `ChessPieceMotion`은 들기 → 든 채 직선 이동 → 내리기를 한다. 값은 `LobbyMotion_Default`의 `liftHeight` 0.35, `liftSeconds` 0.12, `travelSeconds` 0.35, `lowerSeconds` 0.12다.
  - 이 클래스는 StageMap 노드 말 이동과 함께 쓴다(스테이지 맵 설계 NM-D6). 두 곳 모두 호진 작성이다.
- 전투 참고(코드는 다시 쓰지 않는다)
  - Core가 칸마다 걸음 사건을 하나씩 낸다.
  - 걸음마다 먼저 그 방향으로 돌고(기본 0.1초), 가로는 직선으로 잇고, 높이는 포물선 `4p(1−p)`로 올린다. 기본값은 높이 0.3, 한 칸 0.25초다.
  - 원본은 `Combat/Presentation/Movement/CombatHopStepStyle`·`CombatStepHeight`(용근님 영역)다.

### 15.2 지금 배치판에서 생기는 이동

시작 (1,2) R0, 금고 (0,2) R270, 문 (1,3) R0, 탁자 (2,2) R90이다(§5.3). 캐릭터는 마지막 시설 앞에 남으므로 시설끼리 옮겨 다니는 경로도 생긴다.

| 출발 → 도착 | 칸 차이 | Q-HM1 A 경로 | 마지막 걸음 / 도착 방향 |
|---|---|---|---|
| 시작 → 금고·탁자 | 열 −1·+1 | 한 걸음 | −X·+X / 같음 |
| 시작 → 문 | 행 +1 | 한 걸음 | +Z / R0 같음 |
| 금고 ↔ 탁자 | 열 ±2 | 직선 두 걸음 | 같음 |
| 금고·탁자 → 문 | 열 ±1, 행 +1 | 시작 칸 (1,2)를 거쳐 두 걸음 | +Z / R0 같음 |
| 문 → 금고·탁자 | 열 −1·+1, 행 −1 | 시작 칸 (1,2)를 거쳐 두 걸음 | −X·+X / 같음 |

열과 행이 모두 다른 이동이 넷 있어서 경로 규칙을 정해야 한다(Q-HM1).

### 15.3 설계

**HM1 경로 — 칸 하나에 걸음 하나, 가로·세로만**

- 경로는 출발 칸에서 도착 칸까지 한 칸씩이다. 대각선 걸음은 없다.
- 열과 행이 모두 다르면 **도착 방향 축을 마지막에** 걷는다(Q-HM1 A).
  - 도착 방향이 Z축(R0·R180)이면 열(X)을 먼저 맞추고 행을 나중에 걷는다.
  - 도착 방향이 X축(R90·R270)이면 행을 먼저 맞추고 열을 나중에 걷는다.
  - 이 축은 `Begin`이 받는 바라볼 점에서 나온다. 새 DB 칸은 없다.
- 지금 값으로는 두 축 이동이 모두 시작 칸 (1,2)를 지난다. 시작 칸은 늘 서 있는 칸이라 방 모델을 뚫지 않는다(로비 안내 확인 항목 5). 그리고 마지막 걸음이 이미 시설을 보고 있어 도착 뒤 돌 일이 없다.
- 경로는 이동을 시작할 때 한 번 만든다. 칸 → 위치 식은 `LobbyBoard` 한 곳이다(§5.3 식, 지금 `Calculate`).

**HM2 걸음 모양**

- 걸음마다
  1. 걸음 방향이 지금 방향과 다르면 `turnSeconds` 동안 돈다.
  2. 그다음 `hopSeconds` 동안 가로는 두 칸 위치를 직선으로 잇는다.
  3. 높이는 `정착 높이 + hopHeight × 4p(1−p)`(p = 걸음 진행률)다.
- 로비 바닥은 정착 높이가 `boardCenter.y` 하나라서 전투 식의 높이 차 항이 없다. 이 한 줄은 로비 뛰기 모양 클래스 안에 둔다. 전투 `CombatStepHeight`를 참조하지 않고 Common에도 두지 않는다(사용자 결정). 두 식은 같은 사실이 아니다. 전투는 높이가 다른 칸과 접촉을 다루고, 로비는 평평한 표시용 바닥이다.
- 마지막 착지 뒤 도착 방향과 다르면 `turnSeconds` 동안 돌고 끝난다. 지금 값에서는 늘 같아서 돌지 않는다(§15.2).
- 같은 칸을 다시 고르면 지금처럼 이동 없이 방향만 맞추고 완료를 한 번 통과한다(§4.2).

**HM3 구조 — `ChessPieceMotion`을 수명과 경로 모양으로 나눈다(Q-HM2 A)**

| 부분 | 맡는 것 | 자리 |
|---|---|---|
| 수명 `ChessPieceMotion`(지금 클래스) | Delay 예약 하나, 토큰·Detach, 취소 = 마지막 정착 자세 복원, 완료 기록과 한 번 소비, 정착 위치·회전(유일한 원본) | Common, 지금 코드 그대로. 예약 길이만 설정의 고정 합계 대신 경로 모양이 낸 시간 |
| 경로 모양 계약(가칭 `ChessPiecePath`) | `Begin` 때 (출발 자세, 도착 위치, 바라볼 점)으로 이번 경로를 만들고 시간을 낸다. 경과 시간 → 위치·회전 | Common, 새 추상 클래스 |
| 들기 모양(가칭 `ChessPieceLiftPath`) | 지금 Tick의 들기 → 이동 → 내리기와 들기 중 회전을 그대로 옮긴다 | Common, StageMap이 쓴다. 동작은 같다 |
| 로비 뛰기 모양(가칭 `LobbyViewHopPath`) | HM1 경로와 HM2 걸음. `LobbyBoard`와 이동 값을 받는다 | `ViewSystem/Lobby/` |

- **StageSelect는 그대로 둔다(2026-10-29 사용자 "1로").** `ChessPieceMotion`의 지금 생성자(`ChessPieceMotionSettings`를 받음)는 서명을 바꾸지 않고, 안에서 들기 모양을 만들어 쓴다. 경로 모양을 받는 생성자는 하나 더한다. 그래서 `StageMapNodeMovement.cs`는 고치지 않는다.
- 조립 줄은 하나다. `LobbyViewInput.Create`가 새 생성자에 뛰기 모양을 넘긴다. `LobbyInteraction`의 `Begin(도착 위치, 바라볼 점)` 호출은 그대로다.
- 들기 모양은 HM-b 뒤에는 StageSelect만 쓰게 되지만, `ChessPieceMotionSettings`와 같은 자리인 Common에 둔다(2026-10-29 사용자 A).
- 전투 이동과는 코드·값을 공유하지 않는다. 전투 걸음을 바꿔도 로비는 바뀌지 않는다. 공용 `ChessPieceMotion`은 로비와 StageSelect만 쓴다.
- 새 System·Service·서비스 키·Factory 등록은 없다. 경로 모양은 화면 연결 수명의 협력 객체다(§2.2 규칙).

**HM4 현재 칸은 따로 저장하지 않는다**

- 출발 칸은 `ChessPieceMotion`의 정착 위치를 `LobbyBoard`가 칸으로 바꿔 얻는다. 정착 위치가 칸 위가 아니면 예외다. 정착 위치는 늘 시작·도착 자세나 그 복원이라 칸 위에 있다.
- 로비 쪽에 "현재 칸"을 따로 두지 않는 이유가 있다. 도착(EndAction) 뒤, Tick이 완료를 소비하기 전에 취소되면 정착 위치는 이미 도착 칸인데 따로 둔 칸 기록은 옛 칸으로 남아 다음 경로가 어긋난다.

**HM5 값 — 이동 SO 칸**

| 칸 | 뜻 | 초기값 | 조건 |
|---|---|---|---|
| `hopHeight`(`FormerlySerializedAs("liftHeight")`) | 걸음 꼭대기 높이 | 0.35(지금 들기 높이) | 유한, 0 이상 |
| `hopSeconds`(`FormerlySerializedAs("travelSeconds")`) | 한 칸 걸음 시간 | 0.35(지금 이동 시간) | 유한 양수 |
| `turnSeconds`(새) | 걸음 전·도착 뒤 회전 시간. 0이면 바로 돈다 | 0.1(전투와 같음) | 유한, 0 이상 |

- `liftSeconds`·`lowerSeconds`는 지운다. 에셋 YAML에 남은 옛 키는 다음 저장 때 사라진다.
- 검사 원본은 `LobbyMotionData.Validate` 한 곳이다. DB 생성 때 한 번 하고, 뛰기 모양은 다시 검사하지 않는다. `ChessPieceMotionRules`는 StageMap 들기 값(`ChessPieceMotionSettings`)만 검사하게 되므로 그 클래스 주석의 "DB와 이동 연출에서 함께"를 고친다.
- 초기값은 지금 화면 값에서 시작한다(배치 P3). 칸이 4.25 × 3.85로 넓어서 0.35는 낮게 보일 수 있다. 실제 값은 사용자가 Unity에서 맞춘다.

**HM6 바뀌지 않는 것**

- 시설 고르기와 입력 경로(§4.1)
- 이동 중 새 클릭을 받지 않음
- 완료를 Tick이 한 번 소비한 뒤 시설 실행(§4.3)
- 취소는 시설을 실행하지 않음
- 재진입 때 시작 칸에서 시작
- Delay 서비스 사용(Update 타이머·Coroutine 없음)
- 캐릭터 Animator(Idle)·표시 보정

### 15.4 정할 것 — 결정 완료(2026-10-29 사용자 "모두 A")

| ID | 질문 | 선택지 | 권장(결정) |
|---|---|---|---|
| Q-HM1 | 열·행이 모두 다를 때 경로 | **A** 도착 방향 축을 마지막에 / B 늘 열(X) 먼저 / C 대각선 한 번에 뛰기 | **A** — 지금 값에서 모두 시작 칸을 지나고 마지막 걸음이 시설을 본다. B는 문 → 금고·탁자가 (0,3)·(2,3)을 지나는데, 그 칸이 방 모델과 겹치는지 확인한 적이 없다. C는 칸이 정사각형이 아니라 대각선 걸음만 길어지고 "칸마다"에서 벗어난다 |
| Q-HM2 | 구조 | **A** `ChessPieceMotion`을 수명과 경로 모양으로 나눔 / B 로비 전용 이동 클래스를 새로 만듦 / C 지금 클래스를 칸마다 `Begin`으로 이어 부름 | **A** — 취소·완료·재진입을 막는 수명 코드(약 200줄)가 한 곳에 남고, StageMap 동작은 같다. B는 그 수명 코드가 두 벌이 된다. C는 새 클래스가 없지만 모양이 들기·직선·내리기라 포물선이 아니고, 걸음마다 완료 소비 때문에 칸 사이에 한 프레임씩 멈춘다 |
| Q-HM3 | 걸음 전 회전 | **A** 방향이 바뀌는 걸음 전에 짧게 돌고 뛴다(값 `turnSeconds`) / B 걸음 시작에 바로 돈다(값 없음) | **A** — 전투와 같은 느낌이다. 방향이 같은 걸음은 돌지 않아서, 멈칫하는 곳은 모퉁이 하나뿐이다. 값 칸을 줄이고 싶으면 B |

### 15.5 설계에서 정한 것 (확인 대상)

| P | 결정 |
|---|---|
| P-HM1 | 취소는 지금 규칙대로 경로 시작 칸으로 되돌린다(§4.3). 중간 착지 칸에 남기지 않는다. 이동 중에는 시설 팝업·출발이 열리지 않아, 실제로는 연결 종료(Dispose) 때만 일어난다 |
| P-HM2 | 값 칸 이름·초기값·조건은 HM5 표대로 |
| P-HM3 | 경로 목록은 `Begin`(클릭)마다 한 번 만든다. 매 프레임 Tick은 할당 없이 경과 시간으로 걸음 번호를 구한다 |
| P-HM4 | 뛰기 클립·착지 소리·이펙트는 범위 밖이다 |
| P-HM5 | 클래스 이름은 가칭이고 계획 때 확정한다 |

### 15.6 변경 범위(예상)

| 파일 | 변경 | 작성자 |
|---|---|---|
| `ViewSystem/Common/ChessPieceMotion.cs` | Tick 곡선을 경로 모양 호출로 바꾼다. 지금 생성자는 그대로 두고, 경로 모양을 받는 생성자를 더한다 | 호진 |
| 새 `ViewSystem/Common/` 경로 모양 계약·들기 모양(+ `.meta`) | HM3 | — |
| 새 `ViewSystem/Lobby/` 뛰기 모양(+ `.meta`) | HM1·HM2 | — |
| `ViewSystem/Lobby/LobbyBoard.cs` | 아무 칸 → 위치, 위치 → 칸 | 호진 |
| `ViewSystem/Lobby/LobbyViewInput.cs` | 조립 한 줄 | 호진 |
| `LobbyMotionData.cs`·`LobbyMotionDataSO.cs`·`LobbyMotion_Default.asset` | HM5. 코드와 에셋은 한 커밋 | 호진 |
| `ChessPieceMotionRules.cs` | 주석만 | 호진 |

- 다른 작업자 파일은 없다. `Assets/Tests`에서 로비 이동·`ChessPieceMotion`을 참조하는 시험은 없다(2026-10-29 grep). 새 시험은 만들지 않는다.
- 구현 때 함께 고칠 문서
  - 이 설계 §1.2·§2.2·§4.2·§5.1·§5.8
  - [로비 안내](../viewDocs/lobby-guide-Hojin.md) §4.2와 확인 목록
  - [스테이지 맵 설계](2026-10-10-stage-map-design-Hojin.md)의 `ChessPieceMotion` 줄(수명 + 들기 모양)

### 15.7 원칙 점검

- **SSOT**
  - 칸 ↔ 위치는 `LobbyBoard`, 정착 자세는 `ChessPieceMotion` 하나다. 현재 칸은 파생 값이다.
  - 취소·완료 규칙은 `ChessPieceMotion` 하나를 두 화면이 함께 쓴다.
  - 뛰기 높이는 로비 뛰기 모양 하나, 값은 이동 SO 하나다.
- **SOLID**
  - S: 수명과 경로 모양이 갈린다.
  - O: 새 이동 느낌은 새 모양 클래스로 더한다.
  - L: 모든 모양은 시간이 양수이고, 끝 위치가 도착이며, 같은 입력에 같은 자세를 낸다.
  - D: 로비·StageMap은 경로 모양 계약에 넘길 뿐이다.
- **FW**
  - 새 System·Service·Factory 등록이 없다.
  - Delay 서비스 사용과 예약 수명 규칙(§4.3)은 그대로다.
  - Tick에서 할당·검색을 하지 않는다(지침 §15·§19).
- **기능 독립성**

| 바꾸는 것 | 고칠 곳 | 확인할 것 |
|---|---|---|
| 로비 뛰기 높이·시간 | 이동 SO 값 | 로비 이동 |
| 로비 경로 규칙(대각선 허용 등) | 로비 뛰기 모양 | 로비 이동 |
| StageMap 말 이동 느낌 | 들기 모양 | StageMap 이동 |
| 취소·완료 수명 | `ChessPieceMotion` | 두 화면 모두(지금도 같다) |

- 남는 위험: 수명 클래스를 고치는 첫 구현에서 StageMap 동작이 같은지 Unity로 확인해야 한다.

### 15.8 구현 뒤 확인(사용자 Unity 재생, 새 시험 없음)

1. 시작에서 금고·문·탁자를 각각 누르면 한 걸음 뛰고, 도착 방향을 보고, 시설이 열린다.
2. 금고 ↔ 탁자는 두 걸음 직선으로 간다.
3. 금고·탁자 → 문, 문 → 금고·탁자는 시작 칸을 거쳐 두 걸음으로 가고, 모퉁이에서 돈다.
4. 걸음 사이에 멈추지 않는다. 착지 때 발이 바닥에 닿고, 방 모델을 뚫지 않는다.
5. 같은 시설을 다시 누르면 이동 없이 열린다. 이동 중 다른 시설 클릭은 무시된다.
6. StageMap 노드 이동은 전과 같다.

### 15.9 구현 메모 (2026-10-29)

- 파일
  - `ViewSystem/Common/`: `ChessPiecePath`(계약, `Plan`·`Evaluate`·`FacingRotation`)와 `ChessPieceLiftPath`(옛 들기 곡선을 그대로 옮김)를 새로 만들었다.
  - `ChessPieceMotion`: 설정 생성자는 들기 모양으로 넘기고, 경로 모양 생성자를 더했다. 매 프레임과 도착 때 자세는 모양에 묻는다.
  - `ViewSystem/Lobby/LobbyViewHopPath`(새로), `LobbyBoard`(`PositionOf`·`CellAt`·`IsAlongBoardZ`, 자세 넷도 같은 식), `LobbyViewInput` 생성 한 줄을 고쳤다.
  - 이동 SO 셋(코드·에셋)을 바꿨다.
- StageSelect `StageMapNodeMovement.cs`는 바꾸지 않았다.
- 설계와 다른 점
  - 예약 문맥의 바라볼 점 칸을 걷었다(모양이 보관).
  - `Plan` 예외는 그 시작을 취소한 뒤 다시 던진다.
  - 출발·도착이 같은 칸인데 위치가 다르면 예외다.
  - 마지막 걸음 목표는 `Begin`이 받은 도착 위치 그대로다.
- 에셋 값: 사용자가 Unity 확인 중 `hopHeight`를 직접 맞추고 있다(0.35 → 0.5 → 1). 지금 값의 원본은 `LobbyMotion_Default.asset`이고, 문서에는 값을 따로 적지 않는다.
- 묶음 끝 검토(새 에이전트 하나): 높음·중간 0, 낮음 3이다. StageSelect 들기 이동은 매 프레임·도착 모두 옛 코드와 같다(코드 대조).
  - 낮음 1(문서와 에셋 값 차이)은 반영했다.
  - 낮음 2는 그대로 둔다. 도착 뒤 회전 구간이 마지막이면 부동소수 오차로 약 1e-7만큼 덜 돌 수 있다. 지금 데이터에서는 그 구간이 생기지 않는다.
  - 낮음 3은 보고만 한다. `WorldRoot` 고정은 주석 전제이고, 옛 코드와 같은 성질이다.

## 16 시설 호버 글자 빛남 (2026-11-02)

상태: 코드·에셋 완료(2026-11-02). 컴파일 미확인, Unity 재생 미확인, 새 시험 없음. 진행 기록은 [로비 계획](../plans/2026-10-11-lobby-plan-Hojin.md)의 2026-11-02 절이다.

### 16.1 요청과 결정

- 사용자: 아티팩트·덱 편집·여정 시작 시설을 호버하면 그 안내 글자가 반짝이고, 반짝임 값은 SO에서 고친다.
- 결정(사용자, 2026-11-02)
  - 모양: 글자 색이 밝아지고 글자 뒤로 빛이 번진다(TMP Underlay).
  - 시간: 호버하는 동안 번쩍이지 않고 조금 세게 빛나는 채로 있고, 벗어나면 바로 원래 글자로 돌아간다. 같은 날 "한 번 번쩍 후 유지" → "계속 번쩍" → 지금 안으로 바뀌었다(사용자 "반복 번쩍임 말고 조금 쎄게 빛나는 것"). 번쩍임 칸(오름·내림·사이·세기)은 걷었다.
  - UIRender 레이어(29): 글자를 씬 조명에서 빼는 용도로 그대로 둔다. Light로 비추지 않는다. 글자 셰이더 `TMP_SDF-Mobile`은 빛을 받지 않는다.

### 16.2 구조

| 무엇 | 어디 | 내용 |
|---|---|---|
| 시설 ↔ 글자 | `LobbyFacilityView.label` | 같은 방 안의 `TextMeshProUGUI`. 검사는 `LobbyVisualPrefabRules.ValidateRoom`(필수·방 안·활성·겹침 없음) |
| 값 | 새 역할 SO `LobbyFacilityHoverDataSO` → `LobbyFacilityHoverData` | 표의 다섯째 역할(§5.1). 값 조건 원본은 `Validate`, 글자 셰이더 대조는 `ValidateLabels`(테이블이 모델 검사 뒤 부름) |
| 언제 켜나 | `LobbyInteraction.UpdateHover` | 지금 누르면 고를 시설만(`CanSelect` + 로비 입력 Layer + UI 가림 없음). 이동 중·시설 팝업·출발 처리·다른 Layer(설정 화면 등)면 끈다(§4.2) |
| 어떻게 그리나 | `LobbyFacilityHighlight`(새) | 아래 |

- 다시 고르는 것은 포인터가 움직였거나 "고를 수 있나"가 바뀐 Tick뿐이다. 그 밖의 Tick에는 아무것도 하지 않는다(빛남은 켜고 끌 때만 바뀜).
- 그리기
  - 처음 호버한 글자에 그 글자 머티리얼의 복사본을 하나 만든다. 복사본은 Underlay를 켜고 SO의 빛 색·크기·부드러움을 넣는다.
  - 호버하는 동안 글자에 복사본을 끼우고 글자 색을 SO 글자 색으로 바꾼다. 벗어나면 원래 머티리얼·색으로 돌린다. 크기·부드러움을 넣은 뒤 끼워야 TMP가 빛 번짐까지 담는 여백을 계산한다.
  - 시간에 따라 바뀌는 값이 없어 시계(`IDelayService`)를 쓰지 않는다.
  - 정리: 로비 입력을 정리할 때 글자를 되돌리고 복사본을 지운다. 방 복제본을 지우기 전이다.
- 셰이더 변형: Mobile SDF의 Underlay는 `shader_feature`라, 그것을 켠 머티리얼이 빌드에 없으면 변형이 빠져 빌드에서 빛 번짐이 안 보인다. SO의 `glowMaterial`(Underlay를 켠 `Prefabs/Lobby/Visuals/LobbyFacilityGlow.mat`)이 변형을 빌드에 남긴다. 그 머티리얼의 값은 쓰지 않고 SO 값이 정한다.

### 16.3 정한 것 (확인 대상)

- P1 시설과 글자는 시설 표지의 칸으로 잇는다(이름·위치로 찾지 않음). 필수다.
- P2 값은 새 역할 SO 하나에 둔다. 모델 SO에 칸을 더하지 않는다. 필수 역할이다.
- P3 빛 번짐 머티리얼 칸을 둔다(빌드 셰이더 변형 때문, §16.2).
- P4 벗어나면 바로 원래대로 돌아간다(서서히 꺼지지 않음).
- P5 빛남은 켜고 끌 때 바로 바뀐다(서서히 켜지지 않음). 세기는 글자 색과 빛 색 알파·크기·부드러움으로만 정한다.
- P6 입력 Layer가 로비가 아니면(설정 화면 등) 끈다.

### 16.4 확인할 것 (사용자 Unity 재생)

1. 새 Play에서 DB 생성·로비 진입에 오류가 없다.
2. 금고·탁자·문을 각각 호버하면 그 글자가 밝아지고 빛이 번진 채로 있다(번쩍이지 않음). 벗어나면 바로 원래 글자로 돌아온다. 더 세게 하려면 SO의 글자 색을 흰색 쪽으로, 빛 색 알파·크기를 올린다.
3. 시설을 눌러 이동하는 동안, 팝업이 열린 동안, 출발 처리 중에는 빛나지 않는다. 팝업을 닫으면 다시 빛난다.
4. ESC로 설정을 열면 빛남이 꺼진다.
5. SO 값을 바꾸면 다음 Play에 반영된다.
6. 글자 위에 커서를 올렸을 때: 방 글자의 Raycast Target이 켜져 있어 UI 가림으로 잡히면 호버·클릭이 둘 다 안 된다(기존 성질). 로비 안내 §7은 "안내 글자는 Raycast Target을 끈다"이다.
7. 가능하면 빌드에서도 빛 번짐이 보인다.

### 16.5 파일

- 새로: `DataBaseSystem/Data/View/Table/LobbyVisual/Item/LobbyFacilityHoverData.cs`·`LobbyFacilityHoverDataSO.cs`, `ViewSystem/Lobby/LobbyFacilityHighlight.cs`(+ `.meta`), 에셋 `DataBaseSO/ViewGroup/LobbyVisual/Item/LobbyFacilityHover_Default.asset`·`Prefabs/Lobby/Visuals/LobbyFacilityGlow.mat`(+ `.meta`).
- 바꿈: `LobbyFacilityView`(`label`), `LobbyVisualPrefabRules`(글자 검사), `LobbyVisualTable`(역할), `LobbyVisualDataTableSO`(주석), `LobbyInteraction`(호버), `LobbyViewInput`·`LobbyViewServices`·`LobbyViewSystem`(연결), `LobbyVisualDataTable.asset`(한 줄), `Lobby.prefab`(시설 셋의 `label`).
- 코드와 에셋은 한 커밋이다. SO 에셋이 없으면 DB 생성이 "FacilityHover 역할 SO가 정확히 하나 필요"로 멈추고, `label`이 비면 방 검사가 멈춘다.
- 옛 `LobbyRoom.prefab`(운영에서 안 씀)의 시설에는 `label`이 없다. 모델 SO가 고르지 않아 검사되지 않는다(지우지 않음).
