# 로비 View 평가 — 문제 목록 (2026-11-05)

기준 판은 커밋 `fc02777aa`이고, 범위는 124개 파일·8,778줄이다. 점수와 기준별 해석은 [평가 보고서](2026-11-05-lobby-view-evaluation-Hojin.md)에 있다. 이 문서는 검증을 거친 문제만 적는다. 중간 6, 낮음 88, 버림 4다. 중간 후보는 증거 검증과 반론 검증을 따로 거쳤고, 둘이 갈리면 판정자가 하나 더 봤다. 낮음 후보는 묶음 검증을 거쳤다. 경로 앞의 `S/`는 `Assets/ProjectL/Script/Systems/`다.

| 기준 | 중간 | 낮음 |
|---|---|---|
| C1 SSOT | 1 | 17 |
| C2 SOLID | 0 | 8 |
| C3 FW 계약 | 0 | 0 |
| C4 기능 변경 독립성 | 0 | 2 |
| C5 불필요한 반복·검사 | 0 | 20 |
| C6 C#·Unity 권장 구조 | 0 | 10 |
| C7 매 프레임 반복 | 0 | 3 |
| C8 캡슐화 | 0 | 5 |
| C9 수명·자원 | 0 | 3 |
| C10 정확성·오류 처리 | 4 | 10 |
| C11 핫패스 할당 | 0 | 2 |
| C12 주석 정합성 | 0 | 5 |
| C13 UI 규약 | 1 | 3 |
| 합 | 6 | 88 |

## 1 중간 (6)

### M-01 로비 시설·아티팩트 드래그·덱 닫기/카드 입력 콜백의 예외가 RuntimeFaultBoundary에 닿지 않고 Unity 로그로만 남음

- 기준: C10
- 위치: `S/RuntimeSystem/InGameSystem/ViewSystem/Lobby/LobbyInteraction.cs:63-94`, `S/RuntimeSystem/InGameSystem/ViewSystem/LobbyInput/LobbyInputController.cs:41-78`, `S/RuntimeSystem/InGameSystem/ViewSystem/LobbyInput/LobbyReceiverSlot.cs:78-82`, `S/RuntimeSystem/InGameSystem/ViewSystem/Lobby/Deck/LobbyDeckPresenter.cs:69-92`, `S/RuntimeSystem/InGameSystem/ViewSystem/Common/PopupViewScope.cs:84-107`, `S/RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactPopupController.cs:245-282`(InputReceiver)·`:292-304`(CloseReceiver)
- 문제: 시설 입력(브리지 → Router → LobbyInputController → 수신 슬롯 → `LobbyInteraction`)과 아티팩트 드래그 입력은 InputSystem 콜백에서 돈다. 덱 닫기·카드 클릭(`UIInputBinding` → Presenter → `PopupViewScope.Close`)은 EventSystem 콜백에서 돈다. 둘 다 런타임 Tick 밖이고, 브리지·Router·`UIInputBinding`에 catch가 없다. 경로 어디에도 `RuntimeFaultBoundary.Report`가 없다. `OnSelectReleased`의 catch는 `Cancel()` 뒤 다시 던질 뿐이다. 같은 컨트롤러의 `CloseReceiver.Click`만 잡아서 Report한다. 범위 안에 규칙이 이미 있는데 다른 입구가 따르지 않는 것이라 기반 한계가 아니다.
- 실패 시나리오: 이동 소리 슬롯이 없으면 `AudioSystem`이 `InvalidOperationException`을 던진다. `ChessPieceMotion.Begin`도 경로·시간 검사로 던질 수 있다. 그러면 시설을 누를 때마다 이동이 취소되고 예외는 콘솔에만 남는다. 런타임은 F1로 멈추지 않고 어느 시설에도 갈 수 없는 채 계속 돈다. 덱 닫기에서 `CloseUI`가 실패하면 `AbortOpen`이 Layer를 지우고, `SetPopupBlocked(false)`는 로비 Layer가 아니라 무시된다(N-04). 입력은 막히고 오류 정지도 없다. 지금 운영 데이터에서 던지는 경로는 확인되지 않았다.
- 고칠 방향: 로비 입력 수신 입구(LobbyInteraction 공개 메서드 또는 수신 슬롯, 아티팩트 InputReceiver, 덱 Presenter Click)를 `CloseReceiver`처럼 감싸 `RuntimeFaultBoundary.Report`로 보낸다.
- 검증: 증거 중간 / 반론 중간

### M-02 도착 실행이 덱·아티팩트 열기에서만 입력 Layer를 보지 않아, 설정창 독점 중 열기가 예외로 F1 정지할 수 있음

- 기준: C10
- 위치: `S/RuntimeSystem/InGameSystem/ViewSystem/Lobby/LobbyFacilityActions.cs:25-39`, `S/RuntimeSystem/InGameSystem/ViewSystem/Lobby/LobbyViewScope.cs:131-134`, `S/RuntimeSystem/InGameSystem/ViewSystem/Lobby/LobbyInteraction.cs:98-121`, `S/RuntimeSystem/InGameSystem/ViewSystem/Common/PopupInputSession.cs:27-58`
- 문제: `CanExecute`는 StageEntry만 `_canDepart()`(막힘 아님 && `IsLobbyLayer`)를 본다. Deck·Artifact는 `CanOpen`만 보고 Layer를 보지 않는다. 도착 실행(`LobbyInteraction.Tick`)이 다시 보는 `_isBlocked()`도 Layer를 보지 않는다(Layer는 호버에서만 본다). "지금 실행해도 되나"가 종류마다 다르다.
- 실패 시나리오: 공용 설정창이 `OverrideLayer(Settings)`로 열리면 timeScale이 0이 된다. 그러나 그 프레임의 `deltaTime`은 이미 정해져 있어, 남은 시간이 짧은 이동은 이번 Tick에 끝날 수 있다. 덱 시설 도착이 소비되면 `PopupInputSession.Begin`의 `ChangeLayer(DeckPopup)`은 독점 때문에 요청만 저장된다. `OwnsLayer`가 거짓이 되고, 요청 Layer까지 비워져 설정창을 닫아도 로비 Layer로 돌아오지 못한다. 이어서 "모달 입력 Layer가 확립되지 않았습니다" 예외 → `AbortOpen` → Tick 밖 → F1. 창은 한 프레임이라 재현은 미확인이지만 경로는 코드상 성립한다.
- 고칠 방향: 시설 실행 가능 판정(막힘 아님 && 로비 Layer)을 하나로 두고, 세 종류의 `CanExecute`·`Execute`가 모두 그것을 거친다.
- 검증: 증거 중간 / 반론 중간

### M-03 View DB 외형이 없는 아티팩트 ID를 조용히 null 외형으로 그려 이름 없는·모델 없는 항목이 됨

- 기준: C10
- 위치: `S/RuntimeSystem/InGameSystem/ViewSystem/Lobby/LobbyViewServices.cs:43-44`, `S/RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactBoardRefresher.cs:55-57,66-82,90`, `S/RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactPopupView.cs:107`, `S/RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactDragDisplay.cs:64-68`, `S/DataBaseSystem/Queries/ViewDataBase/ViewTable/ArtifactViewTable.cs:30-75`
- 문제: `FindArtifactVisual`은 `TryGetArtifactView`가 실패하면 null을 돌려준다. 예외를 내는 `GetArtifactView`가 있는데 쓰지 않는다. Refresher는 null을 캐시하고 `?.DisplayName`·`?.Description`·`?.Prefab`으로 넘긴다. View는 `ModelPrefab == null`이면 건너뛰고, 드래그 표시는 `Begin(null)`에서 그냥 돌아온다. 대체 동작이라는 주석도 없다. 표 생성은 항목 자체만 검사하고 InGame 아티팩트 정의와 짝이 맞는지는 보지 않는다.
- 실패 시나리오: 지금 데이터는 InGame·View 모두 1001~1003으로 짝이 맞는다. Core 표에 새 아티팩트를 넣고 View 표 등록을 빠뜨리면, 보유 목록에 이름 없는 칸이 생긴다. 놓으면 Core 배치는 성공하는데 보드에 모델이 없다. 로그·F1이 없다. UI 규약이 금지한 "필수 구성 누락을 성공처럼 넘김"에 해당한다.
- 고칠 방향: 조회 실패를 원인 ID를 담은 예외(`GetArtifactView`)로 바꾸거나, 표 생성 때 정의마다 외형 항목이 있는지 검사한다.
- 검증: 증거 중간 / 반론 중간

### M-05 UI 가림 판정이 방 프리팹의 월드 공간 시설 안내 글자까지 UI로 보아 그 자리의 시설 호버·클릭을 막음

- 기준: C10
- 위치: `S/RuntimeSystem/InGameSystem/ViewSystem/Common/ScreenUIPointerBlocker.cs:15-46`, `S/RuntimeSystem/InGameSystem/ViewSystem/Lobby/LobbyInteraction.cs:66,75,173`, `S/DataBaseSystem/Data/View/Table/LobbyVisual/Item/LobbyVisualPrefabRules.cs:59-62,73-85`, 에셋 `Assets/ProjectL/Prefabs/Lobby/Visuals/Lobby.prefab`
- 문제: `ScreenUIPointerBlocker.IsBlocked`는 레이캐스트 결과의 module이 `GraphicRaycaster`면 막힘으로 본다. Canvas 렌더 모드는 가리지 않는다. 방 프리팹에 월드 공간 Canvas 셋(`m_RenderMode: 2`, `m_Camera` 비어 있음)과 GraphicRaycaster, `m_RaycastTarget: 1`인 안내 TMP 셋이 있다. 이벤트 카메라가 비면 uGUI는 `Camera.main`을 쓰는데, 로비 출력·시설 피커 카메라도 `Camera.main`이라 같은 화면 좌표에서 실제로 걸린다. DB 검사는 Canvas·GraphicRaycaster·TMP를 허용만 하고 raycastTarget은 보지 않는다. 배경 이미지만 Raycast Target 꺼짐을 검사한다.
- 실패 시나리오: 포인터가 안내 글자 사각형 위에 있으면 누름·뗌·호버가 막힘으로 끝나 시설이 빛나지 않고 말도 움직이지 않는다. 프리팹 좌표로 보면 아티팩트 안내 글자(약 1단위 정사각형)는 아티팩트 BoxCollider 윗부분과 월드 공간에서 겹친다. 덱·출발 안내는 Collider보다 위에 있다. 카메라 투영에서 실제로 겹치는 넓이는 미확인이다. 시설 대부분은 여전히 고를 수 있어 높음은 아니다.
- 고칠 방향: 가림 판정에서 월드 공간 Canvas를 빼거나, 방 프리팹 검사에 "안내 글자 raycastTarget 꺼짐"을 더한다.
- 검증: 증거 중간 / 반론 중간

### M-09 아티팩트 모델 자세(칸 무게중심·높이·회전·배율) 식이 배치 표시와 드래그 표시에 두 벌

- 기준: C1
- 위치: `S/RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactPopupView.cs:104-116,128-144`, `S/RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactDragDisplay.cs:84-105`, `S/RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactBoardMapper.cs:32-36`
- 문제: 칸 평균 중심 → `CellPosition(.., ModelHeight)` → `Euler(0, rot*90, 0) * prefab.localRotation` → `prefab.localScale * ModelScale`의 네 조각이 `ArtifactPopupView.Render/PlaceModel`과 `ArtifactDragDisplay.Show`에 따로 적혀 있다. 공유하는 것은 매퍼 함수뿐이고, 서로를 가리키는 주석도 없다. 모델 풀도 두 클래스가 각자 갖는다.
- 실패 시나리오: 지금은 두 사본이 같아 결함은 아니다. 기준점·높이·회전·배율 규칙을 한쪽만 고치면, 끄는 동안 보인 자리와 놓은 뒤 다시 그린 자리가 달라진다. 드래그 중 배치 모델을 숨겼다가 놓은 뒤 켜는 구조라 놓는 순간 모델이 튄다.
- 고칠 방향: "칸 목록 + 회전 → 로컬 자세" 함수 하나(예: `ArtifactBoardMapper.ModelPose`)를 두 표시가 부른다. 회전 식 부분은 N-40과 겹치므로 점수에서는 한 번만 센다.
- 검증: 증거 중간 / 반론 중간

### M-12 아티팩트 3D 표시가 로비 씬 루트를 직접 끄고 켜며, 참조(lobbyRoot)가 빠져도 예외 없이 건너뛰고 같은 객체를 두 저작 참조가 따로 가리킴

- 기준: C13
- 위치: `S/RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactPopupView.cs:18-20,62-68,69-91`, `S/RuntimeSystem/InGameSystem/ViewSystem/Lobby/LobbySceneRoot.cs:17,21`, 씬 `Assets/Scenes/Lobby.unity`
- 문제: `ArtifactPopupView.Show`는 `lobbyRoot != null && activeSelf`일 때만 로비 공간을 끄고, `Hide`는 null이면 건너뛴다. `Validate`는 `worldRoot`·`cellPrefab`만 필수로 보고 `lobbyRoot`는 보지 않는다. 선택 칸이라는 근거도 없고 `FormerlySerializedAs("stageMapRoot")` 흔적만 있다. 이 컴포넌트는 Lobby 씬 하나에서만 쓰여 칸이 비는 정상 사용처가 없다. 지금 씬에서 이 칸은 `LobbySceneRoot.worldRoot`와 같은 객체라, "로비 공간 루트"를 두 컴포넌트가 따로 저작 참조로 갖는다.
- 실패 시나리오: 씬 편집에서 lobbyRoot가 비거나 다른 객체로 바뀌어도 연결 검사는 통과한다. 팝업을 열면 로비 방·캐릭터가 숨지 않고 오류·로그도 없다(팝업 화면에 실제로 보이는지는 카메라 구도를 확인하지 않아 미확인). 로비가 그 루트를 직접 끄는 코드는 지금 없어, "닫을 때 다시 켜짐" 경로는 성립하지 않는다.
- 고칠 방향: lobbyRoot를 `Validate`에서 필수로 던지거나, 저작 참조를 없애고 로비 연결의 `WorldRoot`를 `Configure` 때 넘겨 원본을 하나로 둔다.
- 검증: 증거 중간 / 반론 중간

## 2 낮음 (88)

### C1 SSOT (17)

### M-07 호출 Job 바로 뒤에 확인/전환 Job이 도는 순서가 파일마다 따로 적은 같은 우선순위 숫자에 기댐

- 기준: C1
- 위치: `S/RuntimeSystem/LifeCycleSystem/LifeCycles/Lobby/Artifact/Jobs/ArtifactCommandJob.cs:13`, `S/RuntimeSystem/LifeCycleSystem/LifeCycles/Lobby/Artifact/Jobs/ArtifactPreviewJob.cs:13`, `S/RuntimeSystem/LifeCycleSystem/LifeCycles/Lobby/Artifact/Jobs/LobbyArtifactCheckJob.cs:15-16,22`, `S/RuntimeSystem/LifeCycleSystem/LifeCycles/Lobby/Jobs/LobbyTransitionJob.cs:16-17`, `S/RuntimeSystem/LifeCycleSystem/LifeCycles/Lobby/Plans/LobbyPlan.cs:59-84`
- 문제: 아티팩트 호출 Job 둘은 `Priority => 3` 리터럴, 확인 Job은 자기 상수 `CheckPriority = 3`이다. 출발은 `RunStartJob(..., 0)` 인자와 `LobbyTransitionJob.Priority => 0`이다. 넷은 서로를 참조하지 않는다. Runner는 작은 값 먼저, 같으면 등록 순이라 같은 숫자가 순서 계약의 유일한 근거다. 확인 Job과 Plan 출발 목록에는 "같은 우선순위라 바로 뒤" 주석이 있지만, 호출 Job 둘의 숫자에는 그 관계가 적혀 있지 않다.
- 유지 비용: 프리뷰 Job만 4가 되면 확인 Job이 먼저 돌아 도메인이 정지된다. 새 Run 값만 1이 되면 전환 Job이 준비 안 된 요청을 보고 예외 → F1. 다만 둘 다 첫 프리뷰·첫 출발에서 크게 드러나고, 원인을 문구로 남긴다. 조용히 틀린 채 도는 길은 없다.
- 고칠 방향: 묶음별 우선순위 상수 하나를 호출·확인·새 Run·전환 Job이 함께 읽거나, 확인을 호출 Job의 후속 단계로 묶는다.
- 검증: 증거 낮음 / 반론 중간 / 판정자 낮음

### M-08 로비 씬 노드 순서 관계(이탈 검사 → View 해제, 카탈로그 재수집 → 핸들 대여)를 서로 다른 클래스의 독립 숫자가 지킴

- 기준: C1
- 위치: `S/RuntimeSystem/LifeCycleSystem/LifeCycles/Lobby/LobbyLifeCycle.cs:26`, `S/RuntimeSystem/InGameSystem/ViewSystem/Lobby/LobbyViewSystem.cs:25-28`, (범위 밖) `S/RuntimeSystem/ErrorHandling/SceneNodeOrder.cs`
- 문제: `ExitPreparationPriority = -200`(이탈 검사)과 `DetachPriority = -100`(View 해제: 출발 취소·아티팩트 입력 비우기)이 다른 클래스의 독립 숫자다. 대소 관계가 의미를 갖는데 어디에도 적혀 있지 않다. `CameraHandlePriority = 100`은 카탈로그의 리터럴 10과의 관계를 주석으로 적었다. 카탈로그가 상수를 내지 않는 것은 기반 한계라 그 몫은 작다. 공용 표 `SceneNodeOrder`는 로비 노드를 "각 시스템의 상수"로 두기로 적어 두었다.
- 유지 비용: View 해제 값을 -200보다 작게 바꾸면 해제가 먼저 돌아 남은 편집·출발이 검사 전에 지워진다. 씬 노드 값은 거의 바뀌지 않고, 관계 하나·값 둘의 국소 비용이다. 지금 순서는 맞다.
- 고칠 방향: 로비 노드 값을 식으로 묶는다("View 해제 = 이탈 검사 + n"). 카메라 핸들 대여는 N-15대로 걷으면 그 순서 부분이 사라진다.
- 검증: 증거 낮음 / 반론 낮음

### M-10 누름·뗌·강제 취소 해석과 선택 소유자 보존 규칙이 로비·아티팩트 입력 Controller·Slot에 두 벌

- 기준: C1
- 위치: `S/RuntimeSystem/InGameSystem/ViewSystem/LobbyInput/LobbyInputController.cs:28-78`, `S/RuntimeSystem/InGameSystem/ViewSystem/ArtifactInput/ArtifactInputController.cs:31-67`, `S/RuntimeSystem/InGameSystem/ViewSystem/LobbyInput/LobbyReceiverSlot.cs:21-105`, `S/RuntimeSystem/InGameSystem/ViewSystem/ArtifactInput/ArtifactInputSlot.cs:39-112`
- 문제: Performed 아닌 포인터 버림, ReleaseOnly Canceled는 눌린 동안만 취소, 재진입 전 `_isPressed` 갱신, Performed 뗌만 확정, Bind 중복 거부, Unbind 때 상태 먼저 비우고 선택 소유자에게만 취소(`TakeSelectOwner`)가 두 Controller·두 Slot에 줄 단위로 같다. 로비는 누름 전에 `OnPointer`를 다시 보내고 아티팩트는 좌표를 인자로 넘기는데, 이것은 수신 계약 모양 차이일 뿐 해석 의미는 같다. 두 쪽 모두 지금 올바르다. 같은 꼴이 범위 밖 Stage·StageSelection 입력에도 있다.
- 유지 비용: 늦은 Canceled 같은 수명 결함이나 Phase 의미 변화를 한쪽만 고치면 다른 화면에 남는다. 사본은 각 50~80줄이라 대조는 쉽다.
- 고칠 방향: 선택 수명 해석과 소유자 보존 슬롯을 Common 부품 하나로 두고, 화면마다 수신 계약만 연결한다.
- 검증: 증거 낮음 / 반론 낮음

### N-40 Rotation4 → 수평 각도(인덱스×90°) 변환이 세 곳에 리터럴로

- 기준: C1
- 위치: `S/RuntimeSystem/InGameSystem/ViewSystem/Lobby/LobbyBoard.cs:88`, `S/RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactDragDisplay.cs:103`, `S/RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactPopupView.cs:115`
- 문제: `(int)rotation * 90f` 리터럴이 세 곳에 있고, Core 회전 방향의 View 쪽 변환 원본이 없다. 아티팩트 두 곳은 M-09에 들어가므로, 이 문제의 독자 몫은 로비 판 한 곳과 공용 변환이 없다는 점이다.
- 유지 비용: 회전 규칙을 바꿀 때 세 곳을 찾아야 한다.
- 고칠 방향: View 공용 도우미 하나로 모은다.
- 검증: 묶음 검증 낮음

### N-41 로비 배치판 안쪽 판정이 DB 검사와 View에 두 벌

- 기준: C1
- 위치: `S/DataBaseSystem/Data/View/Table/LobbyVisual/Item/LobbyBoardData.cs:59-62`, `S/RuntimeSystem/InGameSystem/ViewSystem/Lobby/LobbyBoard.cs:82-83`
- 문제: `LobbyBoardData.ValidateArrival`과 `LobbyBoard.Contains`가 같은 경계식을 따로 갖는다.
- 유지 비용: 경계 규칙을 한쪽만 바꾸면 DB는 통과하고 View가 예외를 낸다.
- 고칠 방향: `LobbyBoardData.Contains` 하나를 둘이 부른다.
- 검증: 묶음 검증 낮음

### N-42 팝업 UIId를 UIBase와 ViewScreen 어댑터가 따로 적음

- 기준: C1
- 위치: `S/RuntimeSystem/GlobalSystem/UISystem/Screens/Lobby/LobbyDeckUI.cs:36`, `S/RuntimeSystem/InGameSystem/ViewSystem/Lobby/Deck/LobbyDeckPopupViewScreen.cs:16`, `S/RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/ArtifactPopupUI.cs:43`, `S/RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactPopupViewScreen.cs:18`
- 문제: `UIBase.ID`와 `IPopupViewScreen.Id`가 같은 상수를 각자 반환한다. 덱 어댑터는 `LobbyDeckUI`를 쥐고 있어 `_view.ID`를 쓸 수 있다. 아티팩트 어댑터는 `IArtifactPopupDisplay`만 쥐어 지금은 ID를 읽을 수 없다.
- 유지 비용: 한쪽만 바꾸면 "팝업 화면이 UI 서비스에서 열리지 않았습니다" 예외가 나고, 원인이 멀다.
- 고칠 방향: 덱 어댑터는 `_view.ID`를 돌려준다. 아티팩트 쪽은 표시 계약에 ID를 더할 때 같이 맞춘다.
- 검증: 묶음 검증 낮음

### N-43 아티팩트 "입력을 받을 수 있나" 판정을 컨트롤러·수신자·드래그에 여러 사본으로 조합

- 기준: C1
- 위치: `S/RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactPopupController.cs:92-93,113,171,210-211,252-253,294-295`, `S/RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactDragController.cs:47,58,103`
- 문제: `CanWork && IsOpen && _displayActive`가 세 곳(210·252·294), `_allowsInput() && _board.Current.IsInitialized`가 두 곳(58·103)에 있다. 컨트롤러 `CanOpen`(`_started && CanWork`)은 Scope `CanOpen`과 식이 다르고, 닫기 허용(113·171)도 그 값으로 정한다.
- 유지 비용: 입력 조건(예: 확인창 중 막기)을 한 곳만 더하면 드래그·닫기·호버 중 일부만 막힌다.
- 고칠 방향: 표시 연결 유효·입력 허용·닫기 허용·호버 가능을 이름 있는 판정 하나씩으로 둔다.
- 검증: 묶음 검증 낮음

### N-44 씬 연결 Scope 게시·교체·출력 카메라 정책이 StageMap과 평행 사본이고 이미 어긋남

- 기준: C1
- 위치: `S/RuntimeSystem/InGameSystem/ViewSystem/Lobby/LobbyViewSystem.cs:143-185,215-223`, `S/RuntimeSystem/InGameSystem/ViewSystem/Lobby/LobbyViewScope.cs:52-55`, (범위 밖) `S/RuntimeSystem/InGameSystem/ViewSystem/StageMap/StageMapViewSystem.cs:166-200`
- 문제: `_lastPublishedScope` 재게시 거름·`previous?.Supersede()`·`Camera.main` 출력 정책이 StageMap과 줄 단위로 같은 꼴이다. 이미 다르다. 로비는 늦은 로딩 콜백을 `if (_stopped || IsFaulted) return;`(:146)으로 조용히 건너뛰고 StageMap은 던진다. CinemachineBrain 검사(:220)는 로비에만 있다. 범위 밖 사본은 감점하지 않고 로비 몫만 센다.
- 유지 비용: 규칙·결함 수정을 한 씬에만 반영하게 된다.
- 고칠 방향: Common 부품 하나로 모은다.
- 검증: 묶음 검증 낮음

### N-45 덱 팝업의 입력 가능 여부·카드 칸 판정과 목록 크기를 화면과 Presenter가 따로 가짐

- 기준: C1
- 위치: `S/RuntimeSystem/GlobalSystem/UISystem/Screens/Lobby/LobbyDeckUI.cs:29,32-33,40,109,224,228-229`, `S/RuntimeSystem/InGameSystem/ViewSystem/Lobby/Deck/LobbyDeckPresenter.cs:16,26-27,48,112`
- 문제: 화면 `CanReceive`(`_acceptInput`·`_isOpen`·`_receiver`)와 Presenter `CanInput`(`_inputEnabled`·`_view.IsOpen`·`isActiveAndEnabled`)이 같은 질문에 답한다. `_visibleCount`와 `_model.Cards.Count`가 같은 개수를 각자 든다. 두 겹 방어라 지금 동작은 맞다.
- 유지 비용: 입력 차단 조건을 바꿀 때 두 곳을 고친다.
- 고칠 방향: 한쪽을 원본으로 두고 다른 쪽은 그것을 읽는다.
- 검증: 묶음 검증 낮음

### N-46 출발 요청의 "끝나지 않은 단계" 정의가 IsPending 나열과 CanMove 전이 표에 두 벌

- 기준: C1
- 위치: `S/RuntimeSystem/BufferSystems/LobbyView/LobbyDepartureRequest.cs:23,32-33,73,104-121`
- 문제: `IsPending`은 Created·Running·Prepared·Transitioning을 나열하고, `CanMove` 표는 같은 넷만 나가는 전이를 갖는 것으로 따로 정의한다. 머리 주석은 "CanMove 표 한 곳"이라고 적는다. `TryStart`의 `Cancelled` 직접 비교(:73)도 표 밖에 있다.
- 유지 비용: 단계를 더하고 한쪽만 고치면 Submit이 끝난 요청을 대기로 보거나, Decide·이탈 판정이 어긋난다.
- 고칠 방향: `IsPending`을 전이 표에서 파생한다.
- 검증: 묶음 검증 낮음

### N-47 아티팩트 입력 Buffer의 번호 발급이 Submit·SetPreview 두 벌

- 기준: C1
- 위치: `S/RuntimeSystem/BufferSystems/ArtifactView/ArtifactViewBuffer.cs:63-73,93-103`
- 문제: 같은 번호 공간인데 넘침 검사·`+1`·`_lastIssuedNumber` 기록 세 줄이 각자 있다.
- 유지 비용: 번호 규칙을 한쪽만 바꾸면 명령·프리뷰 번호가 겹칠 수 있다.
- 고칠 방향: `IssueNumber()` 하나로 모은다.
- 검증: 묶음 검증 낮음

### N-48 ArtifactCommandJob과 ArtifactPreviewJob이 Core 호출 한 줄만 다른 같은 코드

- 기준: C1
- 위치: `S/RuntimeSystem/LifeCycleSystem/LifeCycles/Lobby/Artifact/Jobs/ArtifactCommandJob.cs:8-33`, `S/RuntimeSystem/LifeCycleSystem/LifeCycles/Lobby/Artifact/Jobs/ArtifactPreviewJob.cs:8-33`
- 문제: 필드·생성자·가드·주석이 같고 `ProcessNextCommand`/`ProcessLatestPreview` 한 줄만 다르다.
- 유지 비용: 가드·완료 규칙을 바꿀 때 두 파일을 고친다.
- 고칠 방향: 호출 대리자나 종류 값을 받는 Job 하나로 둔다.
- 검증: 묶음 검증 낮음

### N-49 float·Vector3·Quaternion 유한 값 판정 도우미가 float.IsFinite·ViewPrefabRules를 두고 파일마다 다시 있음

- 기준: C1
- 위치: `S/DataBaseSystem/Data/View/Table/LobbyVisual/Item/LobbyFacilityHoverData.cs:87`, `S/DataBaseSystem/Data/View/Table/LobbyVisual/Item/LobbyMotionData.cs:41`, `S/RuntimeSystem/InGameSystem/ViewSystem/Lobby/LobbyFacilityPicker.cs:45`, `S/RuntimeSystem/InGameSystem/ViewSystem/Lobby/LobbySceneLayout.cs:25-32`, `S/RuntimeSystem/InGameSystem/ViewSystem/Common/ChessPieceMotion.cs:306-314`, `S/RuntimeSystem/InGameSystem/ViewSystem/Lobby/LobbyBoard.cs:89-91`, `S/DataBaseSystem/Data/View/Table/LobbyVisual/Item/LobbyVisualPrefabRules.cs:138-140`
- 문제: float 사본 넷(`!IsNaN && !IsInfinity`), Vector3 사본 둘, Quaternion 성분 나열 셋이 있다. 같은 범위의 다른 파일은 `float.IsFinite`·`ViewPrefabRules.IsFinite(Vector3)`를 쓴다.
- 유지 비용: 동작 차이는 없고, 같은 판정을 여러 곳에서 읽어야 한다.
- 고칠 방향: 공용 판정으로 모은다.
- 검증: 묶음 검증 낮음

### N-50 로비 걸음 회전 기준 각도가 전투 걸음 리터럴을 베낌

- 기준: C1
- 위치: `S/RuntimeSystem/InGameSystem/ViewSystem/Lobby/LobbyViewHopPath.cs:18-19`
- 문제: `SameFacingDegrees = .1f`의 주석이 "전투 걸음 회전과 같은 기준"이라 하지만, 전투 쪽(범위 밖)은 리터럴 `> .1f`다. 클래스 요약은 일부러 전투 코드를 참조하지 않는다고 적는다.
- 유지 비용: 한쪽만 바꾸면 "같은 기준"이 조용히 깨진다. 아주 국소적이다.
- 고칠 방향: 주석에서 "같은 기준" 약속을 걷는다(공용 상수를 두는 것보다 이쪽이 맞다).
- 검증: 묶음 검증 낮음

### N-51 로비 씬 루트 검사가 세 곳에 나뉨

- 기준: C1
- 위치: `S/RuntimeSystem/InGameSystem/ViewSystem/Lobby/LobbySceneRoot.cs:23-33`, `S/RuntimeSystem/InGameSystem/ViewSystem/Lobby/LobbySceneLayout.cs:6-20`, `S/RuntimeSystem/InGameSystem/ViewSystem/Lobby/LobbyViewSystem.cs:203-209`
- 문제: 가상 카메라는 `LobbySceneRoot.Validate`, worldRoot는 그 안에서 부르는 `LobbySceneLayout.Validate`, ui·캔버스는 System `ValidateRoot`가 본다. `LobbySceneLayout` 요약은 "참조와 pose"라 하지만 worldRoot 하나만 본다. 셋 중 둘은 이미 이어져 있다.
- 유지 비용: 저작 칸을 더할 때 세 곳을 봐야 한다.
- 고칠 방향: `LobbySceneRoot.Validate` 하나로 모은다.
- 검증: 묶음 검증 낮음

### N-52 "현재 연결만 입력 Layer를 바꾼다" 규칙이 창구 클래스와 로비 입력 부품에 따로 있음

- 기준: C1
- 위치: `S/RuntimeSystem/InGameSystem/ViewSystem/Common/ViewAttachmentInputLayers.cs:24-32`, `S/RuntimeSystem/InGameSystem/ViewSystem/Lobby/LobbyViewInput.cs:47,50-51,63-67`
- 문제: 팝업 세션만 `ViewAttachmentInputLayers`로 감싼다. 로비 입력은 원본 `_services.Layers`에 `_isCurrent()` 조건을 손으로 붙이고(Connect·Dispose), `ChangeLayer`도 원본을 직접 부른다. 로비 쪽은 `IsLobbyLayer` 조건이 더 있어 완전히 같은 규칙은 아니다.
- 유지 비용: 규칙을 한 곳만 고치면 팝업 복귀와 로비 연결이 다른 규칙으로 Layer를 바꾼다.
- 고칠 방향: 로비 입력도 같은 창구를 받는다.
- 검증: 묶음 검증 낮음

### N-53 패널형 팝업 개폐·열림 알림 패턴이 두 화면에 사본으로 있고 재진입·중단 규칙이 서로 다름

- 기준: C1
- 위치: `S/RuntimeSystem/GlobalSystem/UISystem/Screens/Lobby/LobbyDeckUI.cs:36-38,54,56-99,262-283`, `S/RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/ArtifactPopupUI.cs:43-47,69,74-125`, `S/RuntimeSystem/InGameSystem/ViewSystem/Lobby/Deck/LobbyDeckPopupViewScreen.cs:16-19`, `S/RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactPopupViewScreen.cs:18-29`
- 문제: 루트는 켜 두고 자식 panel만 개폐, `OnUIHidden => Hide()`, `OpenStateChanged` 관찰자 순회·AggregateException, panel 위치 검사를 두 UI가 각자 구현한다. `IPopupViewScreen` 어댑터도 두 벌이다. 규칙도 다르다. LobbyDeckUI는 `_hideInProgress`로 재진입을 막고 panel을 끈 뒤 알리며, 상태가 바뀌면 순회를 멈춘다(:270). ArtifactPopupUI는 알린 뒤 finally에서 panel을 끄고 순회를 멈추지 않는다(:98-104).
- 유지 비용: 같은 `PopupViewScope` 계약 아래 관찰자가 화면마다 다른 순서를 본다. 닫힘 알림 안에서 다시 열면 ArtifactPopupUI는 `_isOpen=true`인데 panel이 꺼진다(그런 관찰자는 지금 없음). 세 번째 팝업 때 어느 쪽을 따를지 갈린다.
- 고칠 방향: 패널형 팝업 공통 기반(OnUIOpened/OnUIHidden에서 알림) 하나로 모은다.
- 검증: 묶음 검증 낮음

### C2 SOLID (8)

### M-13 시설 종류 지식이 열거 밖 여섯 곳(수 상수·이름 칸·SO 칸·짝 비교·판 필드·실행 switch)에 고정됨

- 기준: C2
- 위치: `S/DataBaseSystem/Data/View/Table/LobbyVisual/Item/LobbyFacilityKind.cs:4-9`, `S/DataBaseSystem/Data/View/Table/LobbyVisual/Item/LobbyBoardData.cs:27-66`, `S/DataBaseSystem/Data/View/Table/LobbyVisual/Item/LobbyBoardDataSO.cs:20-30`, `S/DataBaseSystem/Data/View/Table/LobbyVisual/Item/LobbyVisualPrefabRules.cs:15,33-35`, `S/RuntimeSystem/InGameSystem/ViewSystem/Lobby/LobbyBoard.cs:21-48`, `S/RuntimeSystem/InGameSystem/ViewSystem/Lobby/LobbyFacilityActions.cs:25-39`
- 문제: 원본 `LobbyFacilityKind`(셋)가 있는데 `FacilityCount = 3`과 세 이름 문구, 도착 칸 세 속성·SO 세 칸, "서로 달라야 함" 짝 비교 셋, `LobbyBoard`의 Pose 필드 셋·`GetArrival` switch, `LobbyFacilityActions`의 `CanExecute`·`Execute` switch가 각자 셋을 고정한다.
- 유지 비용: 시설은 지금 기획상 고정 셋이고 넷째 시설 계획은 코드에 없다. 넷째를 더하면 빠짐 대부분은 크게 드러난다(DB 생성 때 방 거절, `GetArrival` 예외, Data 생성자 서명 변경). 조용한 것은 짝 비교 누락(두 시설이 같은 도착 칸) 하나이고 결과는 표시 겹침이다. 넷째 시설은 실행 동작을 새로 짜야 해 `LobbyFacilityActions` 수정은 어차피 필요하다.
- 고칠 방향: 도착 칸을 (종류 → 칸·방향) 목록 하나로 받고, 판·실행은 종류로 찾는 표, 시설 수는 열거 값 수에서 구한다.
- 검증: 증거 낮음 / 반론 낮음

### N-01 22개 인자 로비 서비스 묶음을 씬·입력·팝업 부품에 통째로 넘김(넓은 계약)

- 기준: C2
- 위치: `S/RuntimeSystem/InGameSystem/ViewSystem/Lobby/LobbyViewServices.cs:25-86`, `S/RuntimeSystem/InGameSystem/ViewSystem/Lobby/LobbyViewSystem.cs:67-89`, `S/RuntimeSystem/InGameSystem/ViewSystem/Lobby/LobbyViewScope.cs:49,69`, `S/RuntimeSystem/InGameSystem/ViewSystem/Lobby/LobbyViewScene.cs:14-22`, `S/RuntimeSystem/InGameSystem/ViewSystem/Lobby/LobbyViewInput.cs:12-19`, `S/Factory/Runtime/InGame/View/LobbySceneFactory.cs:21-33`
- 문제: `LobbyViewServices`가 DB 값 7, Buffer 6, 입력 슬롯 2, UI·Layer·Delay·카메라 둘·오디오 등 7을 한 생성자로 받아 Scope·Scene·Input·Factory에 모두 넘긴다. Scene은 Models·Board·CharacterPresentation·Cameras만, Input은 Layers·Delay·Motion·FacilityHover·Audio·Slot만 쓴다. 아티팩트 11칸은 `LobbySceneFactory`가 다시 풀어 넘긴다. 생성 때 null을 한 번만 검사하려는 internal 매개변수 객체이고, 서비스 조회 경로는 규약대로다.
- 유지 비용: 아티팩트 팝업에 서비스 하나를 더하면 묶음 생성자·속성 → Factory 둘 → 컨트롤러까지 5파일을 고친다. 모두 컴파일로 잡히고 조용한 실패는 없다.
- 고칠 방향: 쓰는 쪽별(씬 표시·로비 입력·출발·덱·아티팩트)로 묶음을 나누고, 아티팩트 묶음은 아티팩트 Factory가 직접 받는다.
- 검증: 증거 낮음 / 반론 낮음

### N-54 시설 실행이 공통 계약 없이 구체 컨트롤러 둘(LobbyDeckPopupController·ArtifactPopupController)에 직접 기댐

- 기준: C2
- 위치: `S/RuntimeSystem/InGameSystem/ViewSystem/Lobby/LobbyFacilityActions.cs:11-39`, `S/RuntimeSystem/InGameSystem/ViewSystem/Lobby/LobbyViewPopups.cs:78-79`
- 문제: 두 컨트롤러는 같은 모양의 `CanOpen`·`Open`을 갖지만 공통 계약이 없다. 실행 클래스가 구체 타입 둘을 생성자로 받고 종류 switch로 부른다. M-13과 같은 근본 원인이라 점수에서는 상한을 함께 적용한다.
- 유지 비용: 교체·추가 때 생성자·switch를 고치고, 가짜로 대체할 수 없다.
- 고칠 방향: `IFacilityPopup { CanOpen; Open(); }`을 종류 → 항목 표로 받는다.
- 검증: 묶음 검증 낮음

### N-55 로비 입력 클래스가 고르기·호버·이동 시작·도착 실행·이동 소리까지 맡음(인자 9개)

- 기준: C2
- 위치: `S/RuntimeSystem/InGameSystem/ViewSystem/Lobby/LobbyInteraction.cs:39-52,71-94,98-121`
- 문제: `LobbyInteraction`이 picker·blocker·motion·actions·board·isBlocked·audio·highlight·isLobbyLayer를 받아 고르기 수명·호버·도착 조회·이동 시작·이동 소리(`UnitMovementPreset.Base` 고정, :87)·도착 실행을 함께 한다.
- 유지 비용: 이동 소리를 바꾸려 해도 입력 수명 클래스를 연다.
- 고칠 방향: 이동 시작·소리를 말 표시 쪽으로 옮긴다.
- 검증: 묶음 검증 낮음

### N-56 덱 팝업은 구체 MonoBehaviour LobbyDeckUI에, 아티팩트 팝업은 계약에 기대어 두 팝업 경계가 다름

- 기준: C2
- 위치: `S/RuntimeSystem/InGameSystem/ViewSystem/Lobby/Deck/LobbyDeckPopupController.cs:14,20-33`, `S/RuntimeSystem/InGameSystem/ViewSystem/Lobby/Deck/LobbyDeckPresenter.cs:10,18-24`, `S/RuntimeSystem/InGameSystem/ViewSystem/Lobby/Deck/LobbyDeckPopupViewScreen.cs:11-14`, `S/RuntimeSystem/InGameSystem/ViewSystem/Lobby/LobbyViewPopups.cs:60,67-68`
- 문제: 덱 컨트롤러·Presenter·어댑터·팝업 묶음이 모두 `LobbyDeckUI` 구체 타입을 받는다. 아티팩트 어댑터는 `IArtifactPopupDisplay` 계약을 받는다.
- 유지 비용: Presenter를 단독으로 확인하려면 MonoBehaviour가 필요하다.
- 고칠 방향: `ILobbyDeckDisplay` 계약을 둔다.
- 검증: 묶음 검증 낮음

### N-57 아티팩트 팝업 표시 계약이 넓고 닫기 수신에 IUIInputReceiver 전체를 씀

- 기준: C2
- 위치: `S/RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/IArtifactPopupDisplay.cs:9-21`, `S/RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/ArtifactPopupUI.cs:20,72,137-142`, `S/RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactPopupController.cs:386-409`
- 문제: 열림 상태·이벤트·닫기 연결·문구 셋·목록·좌표 판정 셋이 한 계약에 있고 소비자는 일부만 쓴다. `BindClose(IUIInputReceiver)`는 `Click`만 쓰면서 슬롯 -2를 넘긴다. 감싸개 `OwnedDisplay`가 모든 멤버를 다시 구현한다.
- 유지 비용: 판정 방식만 바꿔도 모든 소비자·가짜·감싸개가 영향을 받는다.
- 고칠 방향: 표시·판정·수명 계약을 나누고, 닫기는 좁은 계약으로 받는다.
- 검증: 묶음 검증 낮음

### N-58 로비 표시 표가 역할 SO마다 switch·속성·누락 검사를 손으로 더하고, 방 프리팹 검사가 프리팹 규칙과 호버 데이터에 나뉘어 순서에 기댐

- 기준: C2
- 위치: `S/DataBaseSystem/Queries/ViewDataBase/ViewTable/LobbyVisualTable.cs:40-78`, `S/DataBaseSystem/Data/View/Table/LobbyVisual/Item/LobbyFacilityHoverData.cs:63-77`, `S/DataBaseSystem/Data/View/Table/LobbyVisual/Item/LobbyModelData.cs:24-29`
- 문제: 새 역할은 `case`·속성·`Missing`·인터페이스·묶음·System을 함께 고쳐야 한다. `ValidateLabels`(:72 `facility.Label.fontSharedMaterial`)는 `ValidateRoom`이 Label 연결을 먼저 봤다는 데 기댄다. 그 순서는 루프 안 검사 → 루프 뒤 :78 호출이라는 줄 배치와 주석(:77)으로만 보장된다. 지금 순서는 맞다.
- 유지 비용: `ValidateLabels`를 먼저 부르면 Label null에서 원인 문구 없는 NRE로 실패한다.
- 고칠 방향: 방 프리팹 검사를 `LobbyVisualPrefabRules` 한 곳으로 모은다.
- 검증: 묶음 검증 낮음

### N-59 런타임 View가 매 포인터 이동마다 읽는 MonoBehaviour LobbyFacilityView가 DB 입력 계층에 있음

- 기준: C2
- 위치: `S/DataBaseSystem/Data/View/Table/LobbyVisual/Item/LobbyFacilityView.cs:7-36`
- 문제: DB 검사가 읽는 저작 입력이면서 `LobbyFacilityPicker`·`LobbyFacilityHighlight`가 읽는 씬 컴포넌트이고, 런타임 캐시 `_selectionCollider`를 갖는다. 요약 주석이 겸하는 역할을 이미 적어 두었으므로, 남는 것은 런타임 캐시의 위치다.
- 유지 비용: View 동작 변경이 DB Data 폴더 형식을 고치게 한다.
- 고칠 방향: 런타임 캐시를 View 쪽으로 옮긴다.
- 검증: 묶음 검증 낮음

### C3 FW 계약 (0)

검증을 통과한 문제가 없다. 서비스 조회 경로(Global Context)는 규약대로였다.

### C4 기능 변경 독립성 (2)

### M-14 로비 팝업 집합이 System·생성 계약·Factory·팝업 부품에 덱·아티팩트 둘로 고정됨

- 기준: C4
- 위치: `S/RuntimeSystem/InGameSystem/ViewSystem/Lobby/LobbyViewPopups.cs:15-88`, `S/RuntimeSystem/InGameSystem/ViewSystem/Lobby/LobbyViewSystem.cs:136-160`, `S/RuntimeSystem/InGameSystem/ViewSystem/Lobby/ILobbySceneFactory.cs:15-16`, `S/Factory/Runtime/InGame/View/LobbySceneFactory.cs:21-34`, `S/RuntimeSystem/InGameSystem/ViewSystem/Lobby/LobbyViewScope.cs:117-125`
- 문제: `LobbyViewPopups`는 `Deck`·`Artifact` 필드와 `Create`·`Start`·`Tick`·`RestoreOutput`·`Dispose`에 두 팝업 순서를 손으로 적었다. `AttachFromScene`이 `ArtifactSceneRoot`를 직접 찾아 `ILobbySceneFactory.Create` 서명에 넣고, `StopInput`이 `ArtifactWriter.Clear()`를, Scope.Dispose가 출력 복원을 직접 안다. 다만 두 팝업은 재료가 다르다. 아티팩트만 씬 루트·출력 카메라 대여·출력 복원이 필요해, 공통 목록으로 바꿔도 생성 단계는 팝업별로 남는다.
- 유지 비용: 셋째 팝업은 셋째 시설과 함께 오는 가상 변경이다(M-13과 묶임). Tick 하나를 빠뜨리면 그 팝업의 외부 열림 채택이 돌지 않는데, 첫 확인에서 바로 보인다.
- 고칠 방향: 팝업을 Start/Tick/Dispose 공통 항목의 순서 있는 목록으로 두고, 씬 루트 찾기·출력 복원은 각 팝업 Factory가 맡는다.
- 검증: 증거 낮음 / 반론 낮음

### N-61 출력 카메라 마스크 층 이름 "UI"가 코드에 고정되고 로비 팝업 부품이 그 정책을 직접 생성

- 기준: C4
- 위치: `S/RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactPopupOutputMask.cs:21-31`, `S/RuntimeSystem/InGameSystem/ViewSystem/Lobby/LobbyViewPopups.cs:49`
- 문제: `LayerMask.NameToLayer("UI")`가 고정이고, 생성은 아티팩트 Factory가 아니라 `LobbyViewPopups.Create`가 한다. "UI"는 Unity 기본 층이라 위험은 작다.
- 유지 비용: 층·방식을 바꿀 때 로비 부품까지 연다.
- 고칠 방향: 층 이름을 설정으로 빼고, 생성은 아티팩트 Factory로 옮긴다.
- 검증: 묶음 검증 낮음

### C5 불필요한 반복·검사 (20)

### N-15 로비 카메라 Catalog 핸들을 빌리지만 쓰는 곳이 없음

- 기준: C5
- 위치: `S/RuntimeSystem/InGameSystem/ViewSystem/Lobby/LobbyViewScene.cs:15,66-69,88`, `S/RuntimeSystem/InGameSystem/ViewSystem/Lobby/LobbyViewSystem.cs:27-28,103,188-201`, `S/RuntimeSystem/InGameSystem/ViewSystem/Common/ViewCameraHandleLease.cs:25-28`
- 문제: AfterLoad 노드를 등록해 핸들을 빌리지만 `Handle`·`HasHandle` 외에 읽는 곳이 없고, Dispose만 한다. 효과는 이름 존재 검사와 풀 대여뿐이다.
- 유지 비용: 노드·상수·실패 갈래가 쓰이지 않는 대여를 위해 존재한다.
- 고칠 방향: 대여·노드를 걷거나, 이름 검사 의도를 명시한다.
- 검증: 묶음 검증 낮음

### N-16 입력 슬롯 둘의 서비스 등록(IServiceModule·ExportServices)과 키가 한 번도 쓰이지 않는 죽은 경로이고 ArtifactInputKey 주석은 "루트에 등록됩니다"로 틀림

- 기준: C5
- 위치: `S/RuntimeSystem/InGameSystem/ViewSystem/ArtifactInput/ArtifactInputSlot.cs:10,31-35,98-104`, `S/RuntimeSystem/InGameSystem/ViewSystem/LobbyInput/LobbyReceiverSlot.cs:8,16,90-98`, `S/ServiceKeys/Runtime/ArtifactInputKey.cs:8-10`, `S/ServiceKeys/Runtime/LobbyInputKey.cs:8-12`, `S/Factory/Runtime/InGame/View/InGameViewSystemFactory.cs:63`
- 문제: 슬롯 계약은 `IServiceModule`을 상속하고 구현은 `ExportServices`로 자기를 등록한다. 그러나 슬롯은 `InGameViewInputContext` 생성자로만 전달되고, `ExportServices`를 부르는 Builder도 두 키를 쓰는 곳도 없다. LobbyInputKey 주석은 미사용을 적었지만, ArtifactInputKey 주석은 "루트에 등록됩니다"라고 틀리게 적는다. 주석 틀림은 같은 사실의 한 면이라 여기서 한 번만 센다.
- 유지 비용: 새 소비자가 키로 루트 조회하면 KeyNotFound가 나거나, 루트 재등록 우회를 만들 유인이 된다.
- 고칠 방향: 슬롯의 서비스 등록 구현·상속과 키를 걷거나, 남기면 주석을 실제 전달 경로로 고친다.
- 검증: 묶음 검증 낮음

### N-17 PopupViewScope가 오류 경계 null 갈래를 두지만 모든 호출자가 넘김

- 기준: C5
- 위치: `S/RuntimeSystem/InGameSystem/ViewSystem/Common/PopupViewScope.cs:36,44,247`, `S/RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactPopupController.cs:82-83`, `S/RuntimeSystem/InGameSystem/ViewSystem/Lobby/Deck/LobbyDeckPopupController.cs:29-30`
- 문제: 생성자는 `faults`를 검사 없이 저장하고, `CanContinue`(`_faults == null ||`)와 `if (_faults == null) throw;`가 null 갈래를 처리한다. 로비 호출자는 모두 null이 아닌 경계를 넘긴다.
- 유지 비용: null을 넘겨도 생성이 통과해, 경계 없이 예외가 UI 콜백 밖으로 나가는 다른 동작이 된다.
- 고칠 방향: 생성자에서 ArgumentNullException으로 막고 null 갈래를 지운다.
- 검증: 묶음 검증 낮음

### N-18 ChessPieceMotion이 시간 getter 읽기를 재진입 경계처럼 감싸고 같은 소유·대상 검사를 앞뒤로 반복

- 기준: C5
- 위치: `S/RuntimeSystem/InGameSystem/ViewSystem/Common/ChessPieceMotion.cs:124-139,237-256`
- 문제: `BeginCore`는 `delay.CurrentTime`을 try/catch·토큰 재확인으로 감싼다. `Tick`은 getter 앞뒤로 예약 일치와 대상 null을 두 번 본다.
- 유지 비용: 동작은 같고, 실제 보호 지점을 읽기 어렵다.
- 고칠 방향: getter 비재진입 계약을 두고, 재확인은 콜백 지점에만 남긴다.
- 검증: 묶음 검증 낮음

### N-19 DB 생성 때 검사한 캐릭터 프리팹·배치판 자세를 View 생성마다 다시 검사

- 기준: C5
- 위치: `S/RuntimeSystem/InGameSystem/ViewSystem/Lobby/LobbyCharacterVisual.cs:21-23`, `S/DataBaseSystem/Data/View/Table/LobbyVisual/Item/LobbyModelData.cs:20-37`, `S/RuntimeSystem/InGameSystem/ViewSystem/Lobby/LobbyBoard.cs:31,85-94`
- 문제: `LobbyCharacterVisual.Create`가 DB가 이미 부른 `ValidateCharacterPrefab`을 다시 부른다(이를 위해 검사 함수를 internal static으로 노출). `LobbyBoard.Calculate`는 DB가 검사한 값으로 만든 자세의 유한성을 다시 본다. 같은 Data의 다른 칸은 "DB 생성 때 한 번"이다.
- 유지 비용: 검사 규칙을 바꿀 때 두 시점을 맞춰야 한다.
- 고칠 방향: 입구 검사를 DB 한 곳에 두고 View 쪽 검사를 걷는다.
- 검증: 묶음 검증 낮음

### N-20 아티팩트 편집·프리뷰 데이터를 팩토리·Submit·생성자에서 세 번, 확인 번호를 두 번 검증하고 늘 참인 조건을 둠

- 기준: C5
- 위치: `S/RuntimeSystem/BufferSystems/ArtifactView/ArtifactViewCommand.cs:29-67,95-103`, `S/RuntimeSystem/BufferSystems/ArtifactView/ArtifactViewBuffer.cs`(Submit·SetPreview 첫 줄), `S/RuntimeSystem/BufferSystems/ArtifactView/ArtifactViewPreview.cs:40-45`, `S/RuntimeSystem/LifeCycleSystem/LifeCycles/Lobby/Artifact/LobbyArtifactDomain.cs:100-107`, `S/RuntimeSystem/LifeCycleSystem/LifeCycles/Lobby/Artifact/Jobs/LobbyArtifactCheckJob.cs:25-29`, `S/RuntimeSystem/BufferSystems/LobbyView/LobbyDepartureRequest.cs:51-52`
- 문제: 편집 데이터는 `Place`/`Move`/`Return` → `Submit`의 `Validate` → `ArtifactViewCommand` 생성자 `Validate`로 세 번 검사된다(프리뷰도 같은 꼴). Submit 검사는 default 구조체를 거르는 역할이 있어, 완전한 중복은 생성자 쪽이다. `CreateCheck`와 확인 Job 생성자가 같은 번호를 두 번 검사한다. `IsTransitionSucceeded`의 `Result != null && Result.Succeeded`는 Transitioning이 성공 Prepared에서만 와서 늘 참이다.
- 유지 비용: 원본 검사 자리가 흐려진다.
- 고칠 방향: 생성 지점 한 곳만 검증한다.
- 검증: 묶음 검증 낮음

### N-21 Runner가 빌 때마다(로비 동안 사실상 매 프레임) 읽는 사실 경로가 Bind에서 보장된 값을 매번 다시 검사

- 기준: C5
- 위치: `S/RuntimeSystem/LifeCycleSystem/LifeCycles/Lobby/LobbyProgress.cs:49-70`, `S/RuntimeSystem/LifeCycleSystem/LifeCycles/Lobby/Artifact/LobbyArtifactDomain.cs:52-79`, `S/RuntimeSystem/LifeCycleSystem/LifeCycles/Lobby/Artifact/ArtifactEditFacts.cs:14-33`
- 문제: `LobbyProgress.Read` 한 번에 `_loading == null` 1회, `RequireBound()` 3회, `ArtifactEditFacts` 인자 null 검사 4회가 돈다. 모두 `PostBindService`의 `Bind`에서 막은 값이다.
- 유지 비용: 결과는 같다. 프레임마다 분기 8개(비용 미미)이고, 같은 보장을 세 겹으로 적었다.
- 고칠 방향: 보장은 Bind 한 곳에 두고 Read 경로의 중복 검사를 지운다.
- 검증: 묶음 검증 낮음

### N-22 씬 연결 때 검사한 UI 구성을 덱 열기·SetModel·카드 Render·배경 연결·보유 목록 그리기마다 다시 검사

- 기준: C5
- 위치: `S/RuntimeSystem/GlobalSystem/UISystem/Screens/Lobby/LobbyDeckUI.cs:60,106,182-201,209-213,228-229`, `S/RuntimeSystem/GlobalSystem/UISystem/Screens/Lobby/LobbyDeckListItemView.cs:48`, `S/RuntimeSystem/GlobalSystem/UISystem/Screens/Lobby/LobbyBackgroundView.cs`(ConnectOutput 첫 줄), `S/RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/ArtifactPopupUI.cs:141,216-217`, `S/RuntimeSystem/GlobalSystem/UISystem/Screens/Lobby/LobbyCanvasView.cs:34-35`
- 문제: 씬 연결의 `ValidateComposition` 뒤에도 `LobbyDeckUI.Open`·`SetModel`이 매번 `Validate()`(`GetComponents<UIBase>()` 배열 할당 포함)를 다시 한다. 카드 `Render`, 배경 `ConnectOutput`, `RenderPool`도 다시 검사한다. 닫기·카드 클릭은 `UIInputBinding.CanInteract`와 같은 조건을, `_canClose`는 `closeButton.interactable`과 같은 사실을 따로 둔다.
- 유지 비용: 열기 빈도라 비용은 작고, 검사 원본이 흐려진다.
- 고칠 방향: 구성 검사는 씬 연결 한 번으로, 입력 가능 판단은 바인딩 한 곳에 맡긴다.
- 검증: 묶음 검증 낮음

### N-23 Group Context가 이미 보장한 서비스에 다시 null 검사

- 기준: C5
- 위치: `S/RuntimeSystem/InGameSystem/ViewSystem/Lobby/LobbyViewSystem.cs:65-66,74,91-92`, `S/RuntimeSystem/LifeCycleSystem/LifeCycles/Lobby/Plans/LobbyPlan.cs:51-52,75-76,94-95`
- 문제: Context 속성은 생성자에서 `ServiceStoreReader.GetService`(없으면 KeyNotFoundException)로 채워진다. 그런데 소비자가 `?? throw`·`?.Config`로 다시 본다. `?.Config`의 null 갈래는 예외 종류만 바꾼다.
- 유지 비용: 누가 non-null을 보장하는지 흐려진다.
- 고칠 방향: Context가 보장한 속성은 바로 쓴다.
- 검증: 묶음 검증 낮음

### N-24 로비 쪽에서 같은 판정·정리를 연달아 두 번 하는 갈래들

- 기준: C5
- 위치: `S/RuntimeSystem/InGameSystem/ViewSystem/Lobby/Deck/LobbyDeckPopupController.cs:52-57`, `S/RuntimeSystem/InGameSystem/ViewSystem/Lobby/Deck/LobbyDeckPresenter.cs:45-67`, `S/RuntimeSystem/InGameSystem/ViewSystem/Lobby/LobbyInteraction.cs:104,118-120`, `S/RuntimeSystem/InGameSystem/ViewSystem/Lobby/LobbyFacilityActions.cs:25-38`, `S/RuntimeSystem/InGameSystem/ViewSystem/Lobby/LobbyViewSystem.cs:108`, `S/RuntimeSystem/InGameSystem/ViewSystem/Lobby/LobbyViewScope.cs:85`
- 문제: 덱 `Hide`는 `SetInputEnabled(false)` 뒤 그 동작을 포함하는 `Clear()`를 또 부른다(Clear는 `_model`만 더 지운다). `LobbyInteraction.Tick`은 104행과 119행에서 `_isBlocked()`를 두 번 보고, `Execute`는 StageEntry에서 `CanExecute`를 다시 본다. System과 Scope가 faulted를 둘 다 본다.
- 유지 비용: 실제 보호 검사를 구분하기 어렵다.
- 고칠 방향: 재진입 없는 구간의 중복 검사·정리를 하나로 줄인다.
- 검증: 묶음 검증 낮음

### N-25 아티팩트 팝업 쪽의 이미 보장된 null·상태 재검사, 늘 넘기는 선택 인자, 같은 갈래

- 기준: C5
- 위치: `S/RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactPopupController.cs:52-70,86-90,119,147-148,155`, `S/RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactPopupViewFactory.cs:57-61`, `S/RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactBoardRefresher.cs:39`, `S/RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactDragController.cs:49,121-126`, `S/RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactPopupStatus.cs:63,65`
- 문제: 같은 인자를 Factory와 컨트롤러가 다시 검사한다. `Tick`의 147-148행은 같은 조건을 연달아 본다(매 프레임). `Open`(119)과 `Show`(155)가 연달아 `ClearPreview`를 부른다. 호출자가 늘 넘기는 `beforeBoardChange?.`, 호출자가 늘 채우는 `isVisible = true` 기본값, `SelectPressed` 두 갈래의 같은 `SelectCanceled`, `ConsumeEditResult` 한 번 전달과 겹치는 Status `_reportedEditNumber` 가드가 있다.
- 유지 비용: 어떤 검사가 계약인지 구분하기 어렵다.
- 고칠 방향: 검사는 경계 한 곳에 두고, 늘 있는 의존은 필수 인자로 받는다.
- 검증: 묶음 검증 낮음

### N-26 아티팩트 팝업 View·컨트롤러·UI의 쓰이지 않는 멤버와 필요 이상 넓은 노출

- 기준: C5
- 위치: `S/RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactPopupView.cs:31`, `S/RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactPopupController.cs:71,86-90,189`, `S/RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/ArtifactPopupUI.cs:49-59`, `S/RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactInteraction.cs:43,51,54`
- 문제: `ArtifactPopupView.BorrowedCount`, `ArtifactPopupController.Root`(대입·null 대입만), `ArtifactPopupUI.CloseButton`·`VisibleSlotCount`(부를 때마다 슬롯을 셈)를 읽는 곳이 없다. `ArtifactInteraction.InvalidateCandidate`는 internal이지만 클래스 안에서만 쓴다.
- 유지 비용: 읽을 멤버가 늘고, 닫기 버튼 노출은 바깥에서 직접 조작할 길이 된다.
- 고칠 방향: 지우거나 private으로 좁힌다.
- 검증: 묶음 검증 낮음

### N-27 쓰는 곳이 없는 조회 GetAllArtifactViews

- 기준: C5
- 위치: `S/DataBaseSystem/Queries/ViewDataBase/ViewTable/ArtifactViewTable.cs:18,78-82`
- 문제: `IArtifactViewTableQuery.GetAllArtifactViews`는 선언과 구현만 있고 부르는 곳이 없다. `_dataById.Values`를 그대로 내놓는다.
- 유지 비용: 대체 구현·가짜가 쓰이지 않는 멤버를 맞춰야 한다.
- 고칠 방향: 멤버를 걷는다.
- 검증: 묶음 검증 낮음

### N-28 덱 팝업이 운영에서 늘 빈 목록이라 카드 생성·고정·호버·상세 경로 전체가 닿지 않음

- 기준: C5
- 위치: `S/RuntimeSystem/InGameSystem/ViewSystem/Lobby/Deck/LobbyDeckPopupController.cs:43-50`, `S/RuntimeSystem/InGameSystem/ViewSystem/Lobby/Deck/LobbyDeckPresenter.cs:69-110`, `S/RuntimeSystem/GlobalSystem/UISystem/Screens/Lobby/LobbyDeckUI.cs:113-129,147-163`, `S/RuntimeSystem/GlobalSystem/UISystem/Screens/Lobby/LobbyDeckListItemView.cs`, `S/RuntimeSystem/GlobalSystem/UISystem/Screens/Lobby/LobbyDeckCardContent.cs:7`
- 문제: `Show`는 늘 `Array.Empty<LobbyDeckCardContent>()`로 모델을 만들고, `new LobbyDeckCardContent(`는 운영 코드에 0건이다. 카드 생성·고정 핀·호버·상세 경로가 운영에서 돌지 않는다. 주석상 의도된 임시다.
- 유지 비용: 덱 시설을 열면 빈 창만 보이고, 핀·호버 구현은 실제 데이터로 검증되지 않는다.
- 고칠 방향: 후보 계약 전까지 시설을 막거나 미구현을 명시하고, 공급 입구 하나를 둔다.
- 검증: 묶음 검증 낮음

### N-29 아티팩트 아이콘을 필수로 검사하지만 읽는 코드가 없음

- 기준: C5
- 위치: `S/DataBaseSystem/Data/View/Table/Artifact/Item/ArtifactViewData.cs:22,39-43`, `S/DataBaseSystem/Data/View/Table/Artifact/Item/ArtifactViewDataSO.cs:33-34`
- 문제: `Icon == null`이면 View DB 생성이 멈춘다. 그런데 표시 경로는 이름·설명·Prefab만 읽고 `Icon`을 읽는 곳이 없다. 에셋 셋은 내장 스프라이트를 자리 값으로 넣었다.
- 유지 비용: 아이콘 없이 새 에셋을 넣으면 쓰이지 않는 칸 때문에 부트 DB 생성이 실패한다.
- 고칠 방향: 쓰는 화면이 생길 때까지 선택 칸으로 둔다.
- 검증: 묶음 검증 낮음

### N-30 방 프리팹 검사의 도달하지 않거나 겹치는 조건

- 기준: C5
- 위치: `S/DataBaseSystem/Data/View/Table/LobbyVisual/Item/LobbyVisualPrefabRules.cs:49-61,92,101-105`
- 문제: `[DisallowMultipleComponent]`와 같은 객체 BoxCollider 하나 조건 때문에 `colliders.Add` 실패 갈래에 닿지 않는다. `IsActiveWithin`은 root에 닿지 않으면 false라 60행 `IsChildOf`와 겹친다. 루트 Transform은 92행과 103-105행 루프에서 두 번 검사된다.
- 유지 비용: 읽어야 할 조건이 늘어난다.
- 고칠 방향: 겹치는 조건을 걷는다.
- 검증: 묶음 검증 낮음

### N-31 배치판 SO의 죽은 null 검사와 같은 뜻의 서로 다른 입력 모양

- 기준: C5
- 위치: `S/DataBaseSystem/Data/View/Table/LobbyVisual/Item/LobbyBoardDataSO.cs:18-37`
- 문제: Unity가 늘 채우는 `[Serializable]` 칸 `ArrivalInput`의 null 검사(35행)는 닿지 않는다. 시작 칸만 `startCell`+`startFacing` 모양이고 시설 도착은 `ArrivalInput` 묶음이다.
- 유지 비용: 같은 뜻을 두 모양으로 읽어야 한다.
- 고칠 방향: 시작 칸도 `ArrivalInput`(FormerlySerializedAs)으로 두고 null 검사를 걷는다.
- 검증: 묶음 검증 낮음

### N-32 쓰지 않는 사실 칸 HasBattle과 그 때문의 전투 싱크 의존, 늘 넘기는 기본값 인자, 닿지 않는 갈래

- 기준: C5
- 위치: `S/RuntimeSystem/LifeCycleSystem/LifeCycles/Lobby/LobbyProgressFacts.cs:10,15-22,51-52`, `S/RuntimeSystem/LifeCycleSystem/LifeCycles/Lobby/LobbyProgress.cs:30,45,54,69`, `S/RuntimeSystem/LifeCycleSystem/LifeCycles/Lobby/Plans/LobbyPlan.cs:32-41`
- 문제: `HasBattle`은 `Decide`·`CanLeave` 어디서도 읽지 않는데, 이 칸 때문에 `ICombatStateBufferReadSink`를 바인딩하고 매번 읽는다(remarks는 쓰이는 것처럼 설명한다). 생성자 일곱 인자가 모두 기본값인데 유일한 호출자가 전부 넘긴다. Lobby 등록은 `JobType.Next`뿐이라 `JobType.None` 갈래에 닿지 않는다.
- 유지 비용: 새 칸을 더하고 호출을 빠뜨려도 컴파일된다.
- 고칠 방향: HasBattle과 그 의존을 걷거나 판단에 넣고, 인자를 필수로 바꾼다.
- 검증: 묶음 검증 낮음

### N-33 끌고 있지 않아도 막힘 갈래에서 SelectCanceled가 표시 정리 전체를 포인터 이벤트마다 다시 함

- 기준: C5
- 위치: `S/RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactDragController.cs:98-126,161-189,210-217,244-278`, `S/RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactPopupView.cs:51-55`, `S/RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactDragDisplay.cs:128-139`
- 문제: 상호작용이 막혀 있으면(보드 미준비, 명령 대기) `PointerMoved`·`Rotate`·`Cancel`·`SelectPressed`가 드래그 여부와 무관하게 `SelectCanceled`를 부른다. 그때마다 Buffer `ClearPreview`, 상태 문구, `EndDrag`(배치 모델 전부 `SetActive(true)`), 상세 비우기가 돈다. 정상 누름(126행) 전에도 한 번 돈다.
- 유지 비용: 대기 중 마우스 이동마다 불필요한 Buffer 쓰기·오브젝트 순회가 생긴다(대기 구간이 짧아 작다).
- 고칠 방향: 실제로 정리할 드래그·프리뷰가 있을 때만 정리한다.
- 검증: 묶음 검증 낮음

### N-34 누름·뗌 한 번에 같은 포인터 판정을 두 번 함

- 기준: C5
- 위치: `S/RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactDragController.cs:127,138,151-152,224-232`, `S/RuntimeSystem/InGameSystem/ViewSystem/Lobby/LobbyInteraction.cs:63-76,165-174`
- 문제: 아티팩트 `SelectPressed`·`SelectReleased`가 `Resolve` 뒤 `UpdateCandidate` 안에서 같은 좌표로 또 `Resolve`한다. 로비 누름·뗌도 직전 호버가 이미 한 UI 가림 판정과 Collider Pick을 다시 한다.
- 유지 비용: 클릭마다 판정이 한 번 더 돈다(클릭 빈도라 작다).
- 고칠 방향: 한 입력 처리 안에서 판정 결과를 한 번 구해 넘긴다.
- 검증: 묶음 검증 낮음

### C6 C#·Unity 권장 구조 (10)

### N-63 가변 struct Job이 IJobService를 구현하고 완료 표지가 두 개

- 기준: C6
- 위치: `S/RuntimeSystem/LifeCycleSystem/LifeCycles/Lobby/Artifact/Jobs/ArtifactCommandJob.cs:8-32`, `S/RuntimeSystem/LifeCycleSystem/LifeCycles/Lobby/Artifact/Jobs/ArtifactPreviewJob.cs:8-32`
- 문제: 두 Job은 `Execute`에서 `_started`·`IsCompleted`를 바꾸는 struct이고, 같은 폴더의 다른 Job은 sealed class다. `_started`·`IsCompleted`가 같은 사실을 둘로 들고, Core 예외 때 `_started`만 참으로 남는다. Plan이 곧바로 `IJobService`로 박싱해 Runner가 박싱 객체를 실행하므로 지금 복사 경로는 없다. 프로젝트의 다른 씬 Job도 struct라, 감점 몫은 로비 안에서 class와 섞인 것이다.
- 유지 비용: 값으로 복사해 실행하는 코드가 생기면 원본 완료가 바뀌지 않아 Runner가 멈출 수 있다.
- 고칠 방향: sealed class로 바꾸고 완료 표지를 하나로 둔다.
- 검증: 묶음 검증 낮음

### N-64 이름이 검사·조회인 멤버에 상태 변경이 섞임(Validate의 BindOwner, Storage getter의 GameObject 생성)

- 기준: C6
- 위치: `S/RuntimeSystem/GlobalSystem/UISystem/Screens/Lobby/LobbyDeckUI.cs:60,106,182-201`(:197), `S/RuntimeSystem/GlobalSystem/UISystem/Screens/Lobby/LobbyCanvasView.cs:35`, `S/RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactPopupView.cs:41,133,164-177`
- 문제: `LobbyDeckUI.Validate()`가 `details.BindOwner(this)`로 소유를 기록하고, 연결 검사·Open·SetModel마다 돈다. private `Storage` getter가 처음 읽힐 때 GameObject를 만든다(지연 생성 자체는 흔한 관례).
- 유지 비용: 검사만 하려는 호출자가 소유를 바꾸게 된다. 다른 소유자로 묶인 상태면 "검사"가 예외를 던진다(지금은 같은 소유자라 멱등).
- 고칠 방향: 소유 연결·저장소 생성을 Awake 등 준비 메서드로 옮긴다.
- 검증: 묶음 검증 낮음

### N-65 보유 목록 슬롯 위젯을 실행 중 AddComponent·반복 GetComponent로 얻고 모양 셀·색을 코드로 만듦

- 기준: C6
- 위치: `S/RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/ArtifactPopupUI.cs:21,233-249,260-266`, `S/RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/ArtifactPoolItemView.cs:34-43`
- 문제: 슬롯 프리팹에 `ArtifactPoolItemView`가 없다(그 스크립트 GUID를 쓰는 프리팹·씬이 없다). 그래서 `SlotShape`가 처음에 AddComponent하고, RenderPool 한 번에 슬롯마다 GetComponent·BindOwner가 최대 세 번 돈다(:239·:244·:249). 모양 셀은 `new GameObject`, 색은 코드 상수다(:21, :40). 3D 미리보기 색은 View DB에 있다.
- 유지 비용: 아트가 슬롯·셀·색을 바꾸려면 코드를 고쳐야 하고, 원본 구성 오류가 드러나지 않는다.
- 고칠 방향: 프리팹에 직렬화해 두고 생성 때 한 번 보관한다. 원본에 없으면 Validate에서 예외, 색은 설정으로 뺀다.
- 검증: 묶음 검증 낮음

### N-66 로비 전용 아티팩트 UI가 Screens.Stage 폴더·네임스페이스에 있음

- 기준: C6
- 위치: `S/RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/ArtifactPopupUI.cs:8,11`, `S/RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/ArtifactPoolItemView.cs:9,15`, `S/RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/ArtifactPoolItemContent.cs:5`, `S/RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/IArtifactPopupDisplay.cs:6`
- 문제: 요약이 "로비의 아티팩트 팝업 UI"이고 씬 사용처도 Lobby뿐인데 네 파일이 `Screens.Stage`에 있다. `lobbyRoot`의 `FormerlySerializedAs("stageMapRoot")`도 옛 자리 흔적이다.
- 유지 비용: 잘못 분류되어 찾기 어렵다.
- 고칠 방향: `Screens/Lobby`로 옮기고 `MovedFrom`을 붙인다.
- 검증: 묶음 검증 낮음

### N-67 직렬화 칸 cellPrefab이 실제로는 보드 전체 바닥 모델이고, 파일 이름이 담긴 형식과 다름

- 기준: C6
- 위치: `S/RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactPopupView.cs:21,64-65,100-103`, `S/RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactPopupDisplay.cs:9,28`
- 문제: `cellPrefab`은 보드 전체 바닥 하나로 쓰이고, 검사 문구는 "floor prefab"이다. `ArtifactPopupDisplay.cs`에는 `ArtifactPopupItem`·`ArtifactPopupBoard`만 있고, 비슷한 이름의 `IArtifactPopupDisplay`는 UI 쪽 다른 계약이다.
- 유지 비용: 기획자가 칸 하나짜리 프리팹을 넣거나, 검색에서 잘못된 대상을 고른다.
- 고칠 방향: `FormerlySerializedAs`로 `floorPrefab`으로 바꾸고, 파일 이름을 형식에 맞춘다.
- 검증: 묶음 검증 낮음

### N-68 같은 성격의 형식끼리 접근 수준·계약 구현 방식이 제각각

- 기준: C6
- 위치: `S/DataBaseSystem/Data/View/Table/LobbyVisual/Item/LobbyModelData.cs:7-11`, `S/RuntimeSystem/InGameSystem/ViewSystem/Lobby/LobbySceneRoot.cs:19-23`, `S/RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactPopupView.cs:43-61`, `S/RuntimeSystem/GlobalSystem/UISystem/Screens/Lobby/LobbyDeckListItemView.cs:44`
- 문제: internal 클래스 `LobbyModelData`만 속성이 public이다(같은 폴더 `LobbyBoardData`는 internal). `LobbySceneRoot`는 `LobbyCamera`·`Validate`만 public이다. `ArtifactPopupView`는 internal 메서드 + 명시 구현 래퍼 네 쌍(`BeginDrag`·`ClearPreview`·`ShowPreview`·`EndDrag`)을 섞고 `Rotation4`를 완전 이름으로 쓴다. `LobbyDeckListItemView.Render`만 public이다.
- 유지 비용: 노출 범위를 읽을 때마다 따로 확인해야 한다.
- 고칠 방향: 내부 형식 멤버는 internal, 계약 구현은 한 방식으로 맞춘다.
- 검증: 묶음 검증 낮음

### N-69 예외 문구의 언어와 형식 이름 머리말이 범위 안에서 섞임

- 기준: C6
- 위치: `S/DataBaseSystem/Data/View/Table/Artifact/Item/ArtifactPopupConfigData.cs:36`, `S/DataBaseSystem/Queries/ViewDataBase/ViewTable/ArtifactPopupConfigTable.cs:24,28`, `S/RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactPopupView.cs:65,67,96,106,162`, `S/RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactSceneRoot.cs:30,35`, `S/RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactDragController.cs:277`, `S/RuntimeSystem/LifeCycleSystem/LifeCycles/Lobby/Plans/LobbyPlan.cs:40`, `S/RuntimeSystem/GlobalSystem/UISystem/Screens/Lobby/LobbyBackgroundCameraBinding.cs:33,36`
- 문제: 대부분 한국어 + `[형식 이름]` 머리말인데, 위 줄들은 영어이거나 머리말이 없다(`"Placed item requires absolute cells."`, `"Artifact drag cleanup failed."` 등). `LobbyBackgroundCameraBinding`은 다른 형식 이름 `[LobbyBackgroundView]`를 머리로 쓴다.
- 유지 비용: 로그에서 출처를 찾기 어렵다.
- 고칠 방향: 한국어 + 던지는 형식 이름 머리말로 맞춘다.
- 검증: 묶음 검증 낮음

### N-70 ArtifactDragDisplay 생성자만 인자 검사 없이 GameObject를 만듦

- 기준: C6
- 위치: `S/RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactDragDisplay.cs:27-48`, `S/RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactPopupView.cs:39-42`
- 문제: `root`·`storage`·`config`를 검사하지 않고 매퍼를 만들고, 월드 Canvas GameObject를 root 아래 만든다. 지우는 곳은 없고 씬과 함께 파괴된다. 유일한 호출처는 값을 넘기지만, `Configure` 전이면 `config` null이 매퍼 쪽 예외로 나온다.
- 유지 비용: null이면 다른 형식 이름의 예외가 나 원인을 찾기 어렵다.
- 고칠 방향: 인자를 먼저 검사한다.
- 검증: 묶음 검증 낮음

### N-71 시험용 생성 함수 주입 입구가 운영 코드에 섞여 Instantiate 뒤 Transform을 다시 설정

- 기준: C6
- 위치: `S/RuntimeSystem/InGameSystem/ViewSystem/Lobby/LobbyVisualSet.cs:17-21,42-53`
- 문제: 운영 입구는 `Instantiate(prefab, parent, false)`로 이미 자세를 맞추는데, `CreateOne`이 다시 SetParent·로컬 자세·배율을 넣고 `instance == prefab` 검사를 붙인다. 3인자 판의 호출처는 2인자 판 하나뿐이고 시험 폴더에도 호출이 없다.
- 유지 비용: 운영 경로에 쓸모없는 설정이 있고, 생성 정책이 두 입구로 갈린다.
- 고칠 방향: 3인자 판을 걷거나, 시험 입구가 자세를 보장하게 한다.
- 검증: 묶음 검증 낮음

### N-72 로비 배경음 프리셋 번호를 숫자 0으로 고정

- 기준: C6
- 위치: `S/RuntimeSystem/InGameSystem/ViewSystem/Lobby/LobbyViewSystem.cs:176`
- 문제: `_audio.SetPreset(AmbgmEvent.BGM, 0)`의 0에 이름이 없다. 이동 소리는 `(int)UnitMovementPreset.Base`로 이름을 쓴다.
- 유지 비용: 값의 뜻을 코드에서 알 수 없다.
- 고칠 방향: 이름 있는 상수나 DB 값으로 바꾼다.
- 검증: 묶음 검증 낮음

### C7 매 프레임 반복 (3)

### N-35 드래그 동안 포인터 판정 전체(Resolve)를 매 프레임, 포인터가 움직이면 한 프레임에 두 번 돔

- 기준: C7
- 위치: `S/RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactDragController.cs:72-74,113,224-232`, `S/RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactPointerMapper.cs:26-49`, `S/RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/ArtifactPopupUI.cs:177-202`
- 문제: 드래그 중 `Tick`이 포인터·카메라가 그대로여도 `UpdateCandidate`를 불러 `Resolve` 전체(가림 사각형 넷·보유 슬롯 N개·반환 영역·광선·배치물 셀 훑기)를 돈다. 쓰는 값은 `Cell`·`HasCell`뿐이다. 포인터가 움직이면 `PointerMoved`가 같은 프레임에 또 돈다. 주석의 "출력 카메라 섞임" 이유와 달리 섞임 여부를 보지 않는다.
- 유지 비용: 드래그 동안 매 프레임 버려지는 검사 4+N회와 셀 훑기가 생긴다(작다).
- 고칠 방향: 칸만 구하는 좁은 판정을 두고, 포인터·카메라 자세가 바뀐 프레임에만 다시 판정한다.
- 검증: 묶음 검증 낮음

### N-36 아티팩트 보드 호버가 같은 배치물·빈 칸 위에서도 포인터 이동마다 판정과 상세 글자 쓰기를 반복

- 기준: C7
- 위치: `S/RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactDragController.cs:103-106,234-242`, `S/RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactBoardRefresher.cs:66-72`, `S/RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/ArtifactPopupUI.cs:162-168`
- 문제: 끌지 않는 동안 `PointerMoved`마다 `Resolve` 전체와 `ShowDetail`(사전 조회 + TMP 두 칸 대입) 또는 `ClearDetail`을 다시 한다. 직전 대상을 기억하지 않는다. 로비 시설 호버는 `_pointerMoved`로 거른다.
- 유지 비용: 결과는 맞고, 이동마다 판정과 글자 대입이 돈다.
- 고칠 방향: 마지막 상세 대상을 기억해 바뀔 때만 갱신한다.
- 검증: 묶음 검증 낮음

### N-37 미리보기 갱신마다 월드 캔버스를 껐다 켜고 셀 이미지를 전부 반환·재대여

- 기준: C7
- 위치: `S/RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactDragDisplay.cs:76-100,128-139`
- 문제: `Show` 첫 줄 `ClearCells`가 셀을 전부 비활성·반환하고 캔버스를 끈다(138). 바로 88행에서 다시 켜고 같은 수를 다시 빌리며, `sizeDelta`도 매번 넣는다.
- 유지 비용: 후보 칸이 바뀔 때마다 캔버스 재구성과 SetActive 왕복이 생긴다(칸 이동 빈도라 작다).
- 고칠 방향: 기존 이미지를 재사용해 위치·색만 바꾸고, 남는 셀만 끈다.
- 검증: 묶음 검증 낮음

### C8 캡슐화 (5)

### N-02 아티팩트 팝업 컨트롤러가 좁은 계약과 함께 구체 ArtifactSceneRoot를 받아 내부값으로 보드 좌표 변환기를 따로 만들고, 변환기가 세 곳에서 만들어짐

- 기준: C8
- 위치: `S/RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactPopupController.cs:43-84`, `S/RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactPopupViewFactory.cs:63-73`, `S/RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactPopupView.cs:33-42`, `S/RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactDragDisplay.cs:27-32`
- 문제: 생성자는 좁은 계약 셋(`root.PopupView, root.PopupView, root.PopupUI`)과 함께 구체 `root`도 받아 `root.PopupView.WorldRoot`·`.Configuration`으로 `ArtifactBoardMapper`를 만든다(:78-79). 변환기는 View `Configure`(:36), 드래그 표시 생성자(:31), 컨트롤러 세 곳에서 따로 만들어진다. 변환기는 상태가 없고 세 곳 모두 같은 `worldRoot`·같은 DB 설정 객체를 받으므로, 지금 피킹 칸과 그리는 칸이 어긋나는 길은 없다. 드래그 표시의 `_drag ??=`도 설정이 실행 중 하나라 차이가 없다.
- 유지 비용: 보드 그리기를 다른 구현·가짜로 바꿔도 컨트롤러가 `ArtifactSceneRoot`를 요구한다. 루트·설정 출처를 한 곳만 바꾸면 칸이 어긋날 수 있다.
- 고칠 방향: View가 만든 변환기 하나(또는 포인터 → 칸 좁은 계약)를 드래그 표시·컨트롤러에 넘기고 `root` 인자를 걷는다.
- 검증: 증거 낮음 / 반론 낮음

### N-03 ArtifactPopupView의 Configure → PrepareDragDisplay 준비를 바깥 Factory가 순서대로 불러야 하고, 빠지면 드래그 표시가 조용히 사라짐

- 기준: C8
- 위치: `S/RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactPopupView.cs:33-55,96,116,120`, `S/RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactPopupViewFactory.cs:63-73`
- 문제: `_drag`가 없으면 `BeginDrag`·`ShowPreview`·`ClearPreview`·`EndDrag`·`Render`의 `Prepare`가 `_drag?.`로 넘어간다. `_config == null`이면 `Render`는 예외를 던진다. 같은 준비 누락에 한쪽은 예외, 한쪽은 무음이다. 유일한 호출자인 Factory는 `Configure` 뒤 `host.IsCurrent`이면 늘 `PrepareDragDisplay`를 부르고, 호스트가 현재가 아니면 드래그 호출을 `OwnedPresenter`가 막는다. 그래서 운영에서 `_drag?.`의 null 갈래는 닿지 않는다.
- 유지 비용: 다른 호출자가 `Configure`만 하고 준비를 빠뜨리면 보드는 그려지지만, 끌기 중 모델·프리뷰 칸이 오류 없이 나오지 않는다.
- 고칠 방향: `Configure` 한 입구에서 드래그 부품까지 만들거나, `_drag`가 없으면 예외로 바꾼다.
- 검증: 증거 낮음(기준 C5로 봄) / 반론 낮음(기준 C8)

### N-73 LobbyViewScene·LobbyViewInput이 내부 부품을 그대로 내놓아 Scope·입력이 단계를 조합

- 기준: C8
- 위치: `S/RuntimeSystem/InGameSystem/ViewSystem/Lobby/LobbyViewScene.cs:25-29`, `S/RuntimeSystem/InGameSystem/ViewSystem/Lobby/LobbyViewInput.cs:21,28-39`, `S/RuntimeSystem/InGameSystem/ViewSystem/Lobby/LobbyViewScope.cs:61-81,88`
- 문제: Scene이 `Root`·`Output`·`Visuals`·`Board`·`Character`를 내놓고, `LobbyViewInput.Create`가 `scene.Character.MovementRoot`·`scene.Visuals.Facilities`·`scene.Output`·`scene.Board`를 꺼낸다. Input은 `Interaction`을 내놓아 Scope가 `_input.Interaction.Tick()`을 직접 부르고, 시작 순서를 Scope 한 메서드가 조합한다.
- 유지 비용: Scene 부품 구성을 바꾸면 Input도 고쳐야 한다.
- 고칠 방향: `LobbyViewInput.Tick()`을 두고, Scene은 입력에 필요한 값만 좁게 넘긴다.
- 검증: 묶음 검증 낮음

### N-74 ArtifactViewData만 public이고 검사 함수도 public

- 기준: C8
- 위치: `S/DataBaseSystem/Data/View/Table/Artifact/Item/ArtifactViewData.cs:8,19-25`
- 문제: 같은 폴더의 다른 Data는 internal인데, 이 클래스·속성·`Validate`가 public이다. 사용처는 모두 같은 어셈블리다.
- 유지 비용: 한 번 부르는 검사가 공개 계약처럼 보인다.
- 고칠 방향: internal로 맞춘다.
- 검증: 묶음 검증 낮음

### N-75 "이것만 부른다"는 출발 단계 변경·도메인 정지 손잡이가 어셈블리 전체에 internal로 열려 있음

- 기준: C8
- 위치: `S/RuntimeSystem/BufferSystems/LobbyView/LobbyDepartureRequest.cs:19-28,62-101`, `S/RuntimeSystem/BufferSystems/LobbyView/LobbyViewBuffer.cs:7-10`, `S/RuntimeSystem/LifeCycleSystem/LifeCycles/Lobby/Artifact/LobbyArtifactDomain.cs:32-35,50,119-132`
- 문제: `TryStart`·`PublishResult`·`BeginTransition`·`CompleteTransition`·`Fail`이 internal이다. 읽기 싱크 `Current`가 단계 변경자를 가진 가변 객체를 그대로 내고, `Status`는 public이다. 도메인 `Stop`·`Reset`도 "이것만 부른다"는 주석 약속뿐이다. 한 어셈블리라 internal이 유일한 C# 수단이지만, 읽기 계약을 따로 두는 길은 있다.
- 유지 비용: View가 단계 변경자를 불러도 컴파일이 막지 않는다(지금 오용은 없다).
- 고칠 방향: View에는 질문만 가진 읽기 계약을, 단계 변경은 좁은 쓰기 계약으로 준다.
- 검증: 묶음 검증 낮음

### C9 수명·자원 (3)

### M-11 같은 의존을 원본과 "현재 호스트만" 감싼 판으로 함께 쥐고, 호스트 검사를 두 방식으로 나눠 둠

- 기준: C9
- 위치: `S/RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactPopupController.cs:25-29,74-81,93,96-135,151-172,193-235,313-430`
- 문제: 컨트롤러는 `_writer`·`_presenter`·`_screen` 원본을 필드로 갖고, 같은 객체를 `OwnedWriter`·`OwnedPresenter`·`OwnedDisplay`로 감싸 하위 부품에만 넘긴다. 파일의 `_host.IsCurrent` 26개 중 18개는 감싸개 안에 있다. 본체 8개 중 하나는 `CanWork` 정의, 여섯은 정리 경로 도우미(`CancelDrag`·`ResetPointer`·`ClearInput`·`HidePresenter`·`UnbindClose`·`BlockScreen`), 하나는 Close 예외 판정이다. 원본을 직접 쓰는 줄은 모두 바로 앞에서 `CanWork`를 확인하거나 도우미를 거쳐, 지금 이전 연결이 새 연결의 공유 장면을 건드리는 길은 없다.
- 유지 비용: 감싸개와 `CanWork`·도우미 두 방식이 공존해, 새 정리 경로를 넣는 사람이 규칙을 알아야 한다. 검사 없는 원본 호출을 하나 더 넣으면 교체된 연결의 정리가 새 로비 연결의 3D 보드·닫기 버튼·입력 Buffer를 지운다.
- 고칠 방향: 컨트롤러도 감싼 판만 쓰게 원본 필드를 없애고, 호스트 검사는 감싸개 한 곳에서만 한다.
- 검증: 증거 낮음 / 반론 낮음

### N-13 씬 표시 시작의 부분 생성 정리 갈래가 불일치해 방·바닥 또는 캐릭터 복제본이 정리되지 않을 수 있음

- 기준: C9
- 위치: `S/RuntimeSystem/InGameSystem/ViewSystem/Lobby/LobbyViewScene.cs:35-63,83-90`, `S/RuntimeSystem/InGameSystem/ViewSystem/Lobby/LobbyViewSystem.cs:136-141,225-229`
- 문제: `finally`는 `!canWork()`일 때만 `visuals.Dispose()`하는데, `requireRunning()`(System `_stopped || IsFaulted`)과 조건이 다르다. 다만 `_stopped`는 `StopInput`만 세우고 `StopInput`은 같은 Scope를 Dispose하므로, 이 갈래는 사실상 닿지 않는다. 캐릭터는 먼저 속성에 넣고 `Show()`한 뒤 끝에서 `canWork()`만 돌려줘, Dispose가 먼저 돌았으면 정리 주체가 없다. 56-60행 사이에 콜백이 없어 이것도 잠복이다.
- 유지 비용: 생성 중 재진입 경로가 생기면 인스턴스가 씬 언로드까지 남는다.
- 고칠 방향: 만든 즉시 소유 정리 대상에 맡기거나, 실패 때 무조건 정리하는 한 꼴로 맞춘다.
- 검증: 묶음 검증 낮음

### N-14 Detach 뒤에도 _lastPublishedScope가 폐기된 로비 연결 전체를 다음 진입까지 붙잡음

- 기준: C9
- 위치: `S/RuntimeSystem/InGameSystem/ViewSystem/Lobby/LobbyViewSystem.cs:38,122-134,164-172`, `S/RuntimeSystem/InGameSystem/ViewSystem/Lobby/LobbyViewScope.cs:52-55,99`
- 문제: `Detach`는 `_scope`만 비우고 `_lastPublishedScope`는 다음 `Attach`의 `previous?.Supersede()`까지 남긴다. 그동안 폐기된 Scope(씬 객체 래퍼·팝업 컨트롤러·풀 등)가 GC되지 않고, 그 Scope의 `IsCurrent`(`!IsSuperseded`)는 참이다.
- 유지 비용: Run 동안 파괴된 로비 객체 그래프가 남는다. `IsCurrent`만 보는 늦은 콜백이 생기면 끝난 연결이 현재처럼 행동할 여지가 있다.
- 고칠 방향: Detach에서 바로 Supersede하거나 세대 번호만 남긴다.
- 검증: 묶음 검증 낮음

### C10 정확성·오류 처리 (10)

### M-04 배치판 바닥 모델이 BoardCenter와 보드 크기를 따르지 않음

- 기준: C10
- 위치: `S/RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactPopupView.cs:100-103,128-144`, `S/RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactBoardMapper.cs:20-36`, `S/RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactDragDisplay.cs:92,97`, `S/DataBaseSystem/Data/View/Table/Artifact/Item/ArtifactPopupConfigData.cs:10-37`, `S/DataBaseSystem/Data/View/Table/Artifact/Item/ArtifactPopupConfigDataSO.cs:19-28`
- 문제: 바닥은 `PlaceModel`로 `BoardCenter` 자리에 놓인 바로 다음 줄에서 `localPosition = Vector3.zero`로 덮인다. 칸 판정·배치 모델·드래그 프리뷰는 `BoardCenter` 기준이다. 바닥 배율은 `CellScale`만 써서 보드 너비·높이·`CellSpacing`과 무관하다. 프리뷰 칸 크기 `.9f`·높이 `.03f`도 코드 고정이다. 바닥이 보드 크기를 전혀 따르지 않으므로, `BoardCenter`가 "고정 바닥 아트 위에 논리 격자를 맞추는 오프셋"일 가능성도 있다. 코드만으로 의도를 정할 수 없고 SO 칸에 툴팁도 없다.
- 유지 비용: 지금 값은 `boardCenter (0,0,0)`, 보드 4×4이고, 보드 크기를 바꾸는 `ExpandBoard`(범위 밖 Core)는 호출처가 없어 드러나지 않는다. 값을 바꾸면 에디터 재생 첫 화면에서 바로 어긋남이 보인다. 계산하고 버리는 위치, 숨은 "바닥은 고정 크기" 제약, 모호한 "중앙에 하나만 표시" 주석이 읽는 사람을 헷갈리게 한다.
- 고칠 방향: `BoardCenter`의 뜻을 정하고, 고정 바닥이 맞는 보드 크기를 설정 검사·툴팁에 둔다. 따라야 한다면 덮어쓰는 줄을 지우고 바닥 배율을 칸 수 × 간격에서 계산한다.
- 검증: 증거 중간 / 반론 낮음 / 판정자 낮음

### M-06 아티팩트 도메인 정지(기록 누락)가 로그 한 줄로 끝나고 팝업은 이유 없이 굳음

- 기준: C10
- 위치: `S/RuntimeSystem/LifeCycleSystem/LifeCycles/Lobby/Artifact/LobbyArtifactDomain.cs:43-50,116-132`, `S/RuntimeSystem/LifeCycleSystem/LifeCycles/Lobby/Artifact/Jobs/LobbyArtifactCheckJob.cs:34-45`, `S/RuntimeSystem/LifeCycleSystem/LifeCycles/Lobby/Plans/LobbyPlan.cs:101-145`, `S/Factory/Runtime/LifeCycle/LifeCycleSystemFactory.cs:62`, `S/RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactDragController.cs:47`
- 문제: 확인 Job이 Core 처리 기록 누락을 발견하면 `LobbyArtifactDomain.Stop`이 `Debug.LogError` 콜백을 부르고 `IsStopped`를 켠다. View는 이 사실을 모른다. 정지 뒤 `Decide`는 아티팩트 줄을 건너뛰고, `CanLeave`는 남은 편집이 있어도 이탈을 허용한다. View `AllowsInteraction`(`Commands.Count == 0`)은 남은 입력 때문에 계속 거짓이다. 이 축소 동작은 생성자 주석("공용 오류 경계가 아닙니다")과 클래스 문서에 밝힌 의도된 설계이고, 원인 번호를 Error 로그로 남긴다.
- 유지 비용: 발동 조건은 Core가 처리 기록 계약을 어길 때뿐이다. 그때 팝업은 안내 없이 드래그를 받지 않는다(다음 로비 진입 때 Reset). Core가 처리하고 기록만 빠뜨린 경우라면 편집은 Core에 반영됐을 수 있어, "편집이 사라진다"는 확정이 아니다.
- 고칠 방향: 정지 사실을 View가 읽는 좁은 계약으로 내 안내·입력 차단을 한다.
- 검증: 증거 낮음 / 반론 낮음

### N-04 팝업 닫힘 때 로비 Layer가 아니면 막힘을 영구히 남기고 다시 풀 경로가 없음

- 기준: C10
- 위치: `S/RuntimeSystem/InGameSystem/ViewSystem/Lobby/LobbyViewScope.cs:101-109,131`, `S/RuntimeSystem/InGameSystem/ViewSystem/Common/PopupViewScope.cs:205-213,305-308`
- 문제: `SetPopupBlocked(false)`는 `!_input.IsLobbyLayer`면 return해 `_popupBlocked`와 글자 숨김이 남는다. 다시 false로 만드는 호출은 `FinishClose`·`UnblockCurrentHost`뿐이라, 이후 Layer가 돌아와도 풀리지 않는다.
- 유지 비용: 닫힘 순간 다른 소유자가 Layer를 쥐면 그 연결 동안 시설·출발이 막히고 제목 글자도 꺼진다. 지금 씬에서 그 조작 경로는 확인되지 않았다(잠복). M-01·N-05의 실패 시나리오가 이 동작에 기댄다.
- 고칠 방향: 팝업 닫힘은 늘 막힘을 풀고, Layer 조건은 쓰는 순간의 판정에 둔다.
- 검증: 묶음 검증 낮음

### N-05 PopupInputSession.Begin이 스스로 끝낸 수명의 토큰을 성공처럼 돌려줘 입력 없는 팝업이 열림

- 기준: C10
- 위치: `S/RuntimeSystem/InGameSystem/ViewSystem/Common/PopupInputSession.cs:49-69`, `S/RuntimeSystem/InGameSystem/ViewSystem/Common/PopupViewScope.cs:151-164,205-213`, `S/RuntimeSystem/InGameSystem/ViewSystem/Lobby/LobbyViewScope.cs:69-70`
- 문제: `Begin`은 Layer를 바꾼 뒤 `_canRestore()`(`CanWork && !_departure.IsHolding`)가 거짓이면 `End(scope, false)`로 소유를 지우고도 같은 토큰을 돌려준다. 49행 주석 "미확립 수명은 반환하지 않습니다"와 어긋난다. `BeginInput`은 `CanContinue`만 보고 열린 상태로 두며, 닫을 때 `End(token, true)`는 아무것도 하지 않는다.
- 유지 비용: 출발 처리 중 외부 열림 채택이 일어나면 팝업이 Layer 없이 열리고, 닫힌 뒤 로비가 막힌 채 남는다(N-04). 외부 열기 순서에서만 생긴다.
- 고칠 방향: 수명을 확립하지 못하면 실패 결과를 돌려주고 `BeginInput`이 열기를 멈춘다.
- 검증: 묶음 검증 낮음

### N-06 팝업 열기·닫기 도중 연결이 바뀌면 원인 예외를 보고 없이 버림

- 기준: C10
- 위치: `S/RuntimeSystem/InGameSystem/ViewSystem/Common/PopupViewScope.cs:71-77,183-188`, `S/RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactPopupController.cs:123-135`
- 문제: `PopupViewScope.Open`·`AdoptOpenScreen`의 catch는 `if (!IsCurrent) return;`으로 전파·Report 없이 끝난다. `ArtifactPopupController.Close()`도 호스트가 바뀌었으면 예외를 `return`으로 삼킨다.
- 유지 비용: 열기 호출 안 동기 콜백에서 Supersede가 일어나고 같은 호출이 진짜 구성 오류(UIId 미등록 등)로 실패하면, 첫 원인 로그가 사라진다. 드문 순서에서만 생긴다.
- 고칠 방향: 버리는 갈래에서도 원인 예외는 경계에 보고한다.
- 검증: 묶음 검증 낮음

### N-08 실패 사유 문구가 열거 값 일부를 빠뜨려 기본 문구로 묻힘

- 기준: C10
- 위치: `S/RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactPopupText.cs:14-29`
- 문제: `InstanceAlreadyPlaced`(4)·`RevisionOverflow`(12) 갈래가 없어 default "요청을 처리하지 못했습니다."가 나온다. 새 사유도 같은 문구로 넘어간다.
- 유지 비용: 이미 놓인 인스턴스 재배치 거절이 오면 원인을 알 수 없는 문구가 보인다.
- 고칠 방향: 빠진 값을 채우고, 모르는 값은 개발 빌드에서 경고나 예외로 알린다.
- 검증: 묶음 검증 낮음

### N-09 우클릭 회수가 포인터 유효 여부를 보지 않고 마지막 좌표를 씀

- 기준: C10
- 위치: `S/RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactDragController.cs:161-181,203-208`, `S/RuntimeSystem/InGameSystem/ViewSystem/ArtifactInput/ArtifactInputController.cs:31-40`
- 문제: `ResetPointer`는 `_hasPointer`만 끄고 `_pointer`는 남긴다. `Cancel`의 비드래그 갈래는 `_hasPointer`를 보지 않고 `Resolve(_pointer)`로 회수를 요청한다. 포인터 이동은 Performed일 때만 전달되고, 이 컨트롤러 객체는 연결 동안 재사용된다.
- 유지 비용: 최신 좌표 통지보다 우클릭(Cancel)이 먼저 도달하면 지난 좌표 아래 배치물이 회수될 수 있다. 포인터 위치 액션은 `Value`·`initialStateCheck: true`라 다시 켜진 뒤 첫 입력 갱신에서 움직임 없이도 현재 좌표를 알리므로, "다시 열고 움직이지 않음"만으로는 생기지 않는다. 그 순서가 실제로 오는지와 회수 여부는 미확인이다(Codex 검수 반영).
- 고칠 방향: 회수 갈래에 `_hasPointer` 조건을 두거나 좌표도 무효화한다.
- 검증: 묶음 검증 낮음

### N-10 화면 좌표 판정이 Overlay Canvas를 전제하지만 검사하지 않음

- 기준: C10
- 위치: `S/RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/ArtifactPopupUI.cs:128-135,183,268-272`
- 문제: `TryPickPool`·`IsReturnArea`·`IsBlockingArea`가 `RectangleContainsScreenPoint(rect, screen, null)`로 판정해 Overlay를 전제한다. `ArtifactPopupUI.Validate()`는 render mode를 보지 않는다. Overlay 검사는 로비 Canvas(`LobbyCanvasView.cs:24-28`)에만 있다. 지금 씬은 Overlay라 맞다.
- 유지 비용: 팝업을 Screen Space - Camera Canvas 아래로 옮기면 집기·반환·가림 판정이 예외 없이 틀린다.
- 고칠 방향: Validate에서 Overlay를 검사하거나 worldCamera를 넘긴다.
- 검증: 묶음 검증 낮음

### N-11 아티팩트 보드 설정 표가 서로 다른 실패 셋을 한 문구로 던짐

- 기준: C10
- 위치: `S/DataBaseSystem/Queries/ViewDataBase/ViewTable/ArtifactPopupConfigTable.cs:21-28`
- 문제: 중복 등록·다른 형식이나 빈 칸·GetData 형식 불일치를 23행 한 조건으로 묶어 "Exactly one configuration item is required."만 던진다.
- 유지 비용: Missing 칸 하나에도 "정확히 하나" 문구가 나와 중복으로 오인한다.
- 고칠 방향: 갈래를 나눠 원인별 문구를 둔다. 영어 문구 문제는 N-69에서 센다.
- 검증: 묶음 검증 낮음

### N-12 연결 시작이 오류·폐기로 중간에 끝나도 Attach가 성공처럼 이어가 배경음을 틈

- 기준: C10
- 위치: `S/RuntimeSystem/InGameSystem/ViewSystem/Lobby/LobbyViewSystem.cs:173-178`, `S/RuntimeSystem/InGameSystem/ViewSystem/Lobby/LobbyViewScope.cs:61-81`
- 문제: `LobbyViewScope.Start`는 도중에 `CanWork`가 거짓이 되면 예외 없이 return한다(64·68·71·75·76·78행). `Attach`는 곧바로 `SetPreset`·`Play(BGM)`을 부른다.
- 유지 비용: Start 안 동기 콜백이 Report해 `StopInput`이 Scope를 정리해도 BGM 요청이 나간다. Audio 쪽에서 무엇이 나는지는 미확인이다.
- 고칠 방향: `Start`가 준비 완료 여부를 돌려주고, Attach는 성공일 때만 배경음을 처리한다.
- 검증: 묶음 검증 낮음

### C11 핫패스 할당 (2)

### N-38 미리보기 응답·보드 변경 경로에서 인터페이스 foreach·ToArray·문자열 보간으로 할당

- 기준: C11
- 위치: `S/RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactDragDisplay.cs:84,89`, `S/RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactPopupView.cs:104,109,119,147`, `S/RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/ArtifactPoolItemView.cs:22,27,44`, `S/RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/ArtifactPopupUI.cs:232`
- 문제: `IReadOnlyList` foreach가 박싱 열거자를 만든다(드래그 중 후보 칸마다). `ClearBoard`의 `_models.ToArray()`, 슬롯마다 `$"ArtifactSlot_…"` 이름 문자열도 있다. 매 프레임 할당은 없다.
- 유지 비용: 칸 이동·편집 빈도의 작은 GC 압력이다.
- 고칠 방향: 인덱스 for, 역순 제거로 바꾸고, 이름은 바뀔 때만 만든다.
- 검증: 묶음 검증 낮음

### N-39 실패 프리뷰가 바뀔 때마다 지울 수 없는 지연 예약과 클로저를 새로 만듦

- 기준: C11
- 위치: `S/RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactPopupStatus.cs:74-80,88-94,110-132`
- 문제: `SetPreview` → `ResetPreview`로 실패 칸을 지날 때마다 `new DelayDataHandler(() => Expire(preview, version), …)`가 쌓이고, 3초간 남아 버전 비교로만 무효화된다. 예약을 지우는 API가 없는 것은 기반 한계지만, 칸마다 새로 할당하는 몫은 범위 안이다.
- 유지 비용: 실패 칸을 훑을 때 칸마다 할당과 대기 예약이 늘어난다(작다).
- 고칠 방향: 예약 하나에 만료 시각만 갱신하거나 Tick에서 비교한다.
- 검증: 묶음 검증 낮음

### C12 주석 정합성 (5)

### N-76 없는 형식·옛 이름을 가리키는 아티팩트 쪽 주석(ArtifactViewSystem cref, ArtifactEditFlow, 구현 목록, "중앙에 하나만 표시")

- 기준: C12
- 위치: `S/RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactSceneRoot.cs:8`, `S/RuntimeSystem/LifeCycleSystem/LifeCycles/Lobby/Artifact/Jobs/LobbyArtifactCheckJob.cs:11`, `S/RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/IArtifactPopupPresenter.cs:7`, `S/RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactPopupView.cs:100-103`
- 문제: `ArtifactSceneRoot` 요약의 `<see cref="ArtifactViewSystem"/>`은 없는 형식이다(실제는 `LobbyViewSystem.AttachFromScene`). `LobbyArtifactCheckJob`의 "옛 ArtifactEditFlow"도 없는 형식이다. `IArtifactPopupPresenter`의 "구현: ArtifactPopupView, 테스트 가짜"는 `OwnedPresenter`(`ArtifactPopupController.cs:340`)를 빠뜨리고 없는 시험을 가리킨다. Render의 "중앙에 하나만 표시" 바로 아래 줄이 원점으로 덮어쓴다(M-04).
- 유지 비용: 읽는 사람이 없는 시스템을 찾고, XML 문서를 만들면 CS1574 경고가 난다.
- 고칠 방향: cref를 `LobbyViewSystem`으로 고치고, 옛 이름을 지우고, 구현 목록을 지금 코드에 맞춘다.
- 검증: 묶음 검증 낮음

### N-77 아티팩트 입력 계약·Factory·체스 말 경로의 쓰임 설명이 지금과 다름

- 기준: C12
- 위치: `S/RuntimeSystem/InGameSystem/ViewSystem/ArtifactInput/IArtifactInputReceiver.cs:3`, `S/RuntimeSystem/InGameSystem/ViewSystem/ArtifactInput/ArtifactInputFactory.cs:7`, `S/RuntimeSystem/InGameSystem/ViewSystem/Common/ChessPieceLiftPath.cs:8`
- 문제: `IArtifactInputReceiver`는 "맵 View가 받는 입력"이라 적지만, 실제 수신자는 로비 아티팩트 팝업 둘(드래그 컨트롤러·컨트롤러 내부 수신자)이다. `ArtifactInputFactory`는 "아티팩트 팝업들이 공유"라 적지만 팝업은 하나다. `ChessPieceLiftPath`는 "지금은 StageSelect 노드 말 이동"이라 적지만 `StageCombatDeparture`도 쓴다.
- 유지 비용: 수신자를 맵 View로 오해한다.
- 고칠 방향: 지금 쓰임에 맞게 고친다.
- 검증: 묶음 검증 낮음

### N-78 배경음 서비스 보관 위치 주석이 코드·묶음 설명과 반대

- 기준: C12
- 위치: `S/RuntimeSystem/InGameSystem/ViewSystem/Lobby/LobbyViewSystem.cs:34-35,89-90`, `S/RuntimeSystem/InGameSystem/ViewSystem/Lobby/LobbyViewServices.cs:23,60`, `S/RuntimeSystem/InGameSystem/ViewSystem/Lobby/LobbyViewInput.cs:37`
- 문제: System 주석은 "배경음과 씬 노드 등록은 System만 쓰므로 묶음에 넣지 않고 직접 갖는다"이다. 그러나 오디오는 묶음 `Audio`에 있고(묶음 설명 "이 묶음이 원본"), System은 `_audio = _services.Audio`로 사본 참조를 든다. 입력도 같은 서비스로 이동 소리를 낸다.
- 유지 비용: 원본이 어디인지 헷갈린다.
- 고칠 방향: System 주석을 "씬 노드 등록만 직접"으로 고치고, `_audio`는 묶음에서 읽는다.
- 검증: 묶음 검증 낮음

### N-79 로비 전환 시점·OnEnter·명령 중복 방지 책임을 잘못 설명하는 주석

- 기준: C12
- 위치: `S/RuntimeSystem/LifeCycleSystem/LifeCycles/Lobby/Jobs/LobbyTransitionJob.cs:41-44`, `S/RuntimeSystem/BufferSystems/LobbyView/LobbyDepartureRequest.cs:50-52`, `S/RuntimeSystem/LifeCycleSystem/LifeCycles/Lobby/LobbyLifeCycle.cs:61-68`, `S/RuntimeSystem/BufferSystems/ArtifactView/ArtifactViewBuffer.cs:18-22`
- 문제: ① `LobbyTransitionJob`의 "동기 이탈 검사는 이 시점의 Transitioning 요청을 확인"과 `IsTransitionSucceeded` 설명은 로딩 표현이 없는 경로에만 맞다. 로딩 표현이 있으면 `Load`가 덮기만 시작하고, SceneExitLoaded는 덮기가 끝난 뒤 Tick에서 돈다. 그때 요청은 이미 Completed다. ② `OnEnter` 요약 "Runner가 바빠도 예외가 아닙니다"는 OnTick 설명이다. ③ `TryGetNextCommand`의 "중복 방지도 Core 처리 계층이 관리"와 달리, 실제 중복 방지는 `LobbyLifeCycle.OnTick`의 Runner 빔(`IsIdle`) 조건이다.
- 유지 비용: ①을 믿고 이탈 검사를 Transitioning 기준으로 고치면 운영 경로에서 쓰이지 않는다. 덮기 중 편집으로 F1이 나는 길은 없다(`LobbyDepartureFlow.IsHolding`이 Completed 요청도 막아 시설·팝업 입력이 열리지 않는다).
- 고칠 방향: ①은 두 시점을 함께 적거나 표현과 무관한 설명으로, ②③은 지금 코드 기준으로 고친다.
- 검증: 묶음 검증 낮음

### N-80 "새 참조를 먼저 확인해 기존 연결을 불완전한 값으로 교체하지 않는다" 주석과 달리 아티팩트 루트 검사는 기존 연결을 뗀 뒤에 함

- 기준: C12
- 위치: `S/RuntimeSystem/InGameSystem/ViewSystem/Lobby/LobbyViewSystem.cs:157-169`, `S/RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactPopupViewFactory.cs:63`, `S/RuntimeSystem/InGameSystem/ViewSystem/Lobby/LobbyViewPopups.cs:50`, `S/RuntimeSystem/InGameSystem/ViewSystem/Lobby/LobbyViewScope.cs:68-71`
- 문제: `AttachFromScene`은 Detach 전에 `ValidateRoot`(LobbySceneRoot·UI)만 본다. `ArtifactSceneRoot.Validate`는 Detach → Create → `scope.Start` → 방·캐릭터 생성(:68) → `_popups.Create` → `ArtifactPopupViewFactory.Create:63`에서 돈다. 157행 주석과 다르다.
- 유지 비용: 결과는 오류 정지라 차이는 작다. 주석을 믿고 오류 없는 재연결에 기대면 틀린다.
- 고칠 방향: 아티팩트 루트 검사를 Detach 전으로 옮기거나, 주석 범위를 고친다.
- 검증: 묶음 검증 낮음

### C13 UI 규약 (3)

### N-81 로비 화면 글자 묶음을 UISystem 밖에서 LobbyViewScope가 raw GameObject로 받아 SetActive로 여닫음

- 기준: C13
- 위치: `S/RuntimeSystem/InGameSystem/ViewSystem/Lobby/LobbyViewScope.cs:22-24,45,66-67,101-109`, `S/RuntimeSystem/GlobalSystem/UISystem/Screens/Lobby/LobbyCanvasView.cs:13-18`
- 문제: `LobbyCanvasView.LobbyTexts`(GameObject)를 Scope가 받아 연결 시작·팝업 알림에 맞춰 직접 켜고 끈다. 미등록 사유("팝업 동안만 숨김")는 주석(:22-23)에 있다. 그러나 UI 규약이 요구하는 "기존 Policy(Exclusive + RestoreWhenHistoryEmpty 등)로 표현하지 못하는 구체 제약"은 적혀 있지 않다. 사유 주석이 있어 낮음이다.
- 유지 비용: `PopupViewScope`를 거치지 않는 새 화면을 더하면 숨김을 따로 연결해야 하고, 잊으면 글자가 남는다. 숨김 방식을 바꾸면 InGame Scope를 고쳐야 한다. "팝업 동안 로비에서 무엇을 숨기나"가 M-12와 두 곳에 나뉜다.
- 고칠 방향: RestoreWhenHistoryEmpty 루트 UIBase로 등록하거나, 최소한 `LobbyCanvasView.SetLobbyTextsVisible(bool)`로 GameObject 노출을 걷는다.
- 검증: 묶음 검증 낮음

### N-82 덱 UI가 상태 플래그를 먼저 내려 호버 해제를 수신자에게 넘기지 않음

- 기준: C13
- 위치: `S/RuntimeSystem/GlobalSystem/UISystem/Screens/Lobby/LobbyDeckUI.cs:40,80-88,107-111,168-171,222-226,237-248`, `S/RuntimeSystem/GlobalSystem/UISystem/Screens/Lobby/LobbyDeckListItemView.cs:36-42`
- 문제: Hide·SetModel·BindInput이 `_isOpen`/`_acceptInput`/`_receiver`를 먼저 바꾼 뒤 `ClearControlInput()` → `SetInputEnabled(false)` → `OnHoverExit()`를 부른다. 그래서 HoverExit가 `CanReceive` 거짓으로 수신자에게 가지 않는다. UI 규약의 "호버는 나감·비활성·재바인딩 때 한 번 해제"를 화면 계층에서 지키지 않고, Presenter 자체 초기화에 기댄다.
- 유지 비용: 수신자를 바꾸거나 Presenter 초기화 줄을 빼면 호버가 남는다. 열린 채 SetModel이 다시 오면 미리보기가 다시 나오지 않는다. 지금은 컨트롤러 Hide가 Presenter를 직접 초기화하고 모델이 늘 빈 목록(N-28)이라 드러나지 않는다.
- 고칠 방향: 플래그를 내리기 전에 `ClearControlInput()`을 불러 HoverExit를 한 번 보낸다.
- 검증: 묶음 검증 낮음

### N-83 아티팩트 보유 목록 슬롯의 같은 클릭이 UIInputBinding과 InputSystem Select 두 경로로 처리되고 포인터 이동마다 슬롯별 수동 영역 판정

- 기준: C13
- 위치: `S/RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/ArtifactPopupUI.cs:137-158,177-202,204-210,227`, `S/RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactPointerMapper.cs:26-30`, `S/RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactDragController.cs:98-157,183-189,234-242`
- 문제: 슬롯 Button은 `UIInputBinding`으로 Click·Hover를 받아 상세를 바꾼다(`ShowSlotDetail`). 같은 누름·뗌은 InputSystem Select가 드래그 컨트롤러로 보내 `IsBlockingArea`·`TryPickPool`(슬롯 순회)·`IsReturnArea`로 직접 판정하고 상세(`ShowDetail`)를 처리한다. 포인터 이동마다 `UpdateHover`가 같은 수동 판정을 돈다. 상세 표시 소유가 두 경로로 나뉜다는 점은 주석(:238)이 인정한다. UI 규약은 컨트롤별 판정을 피하고 EventSystem 디스패치를 쓰라고 한다.
- 유지 비용: 슬롯 클릭 한 번에 드래그 시작(ShowDetail) → 뗌(ClearDetail) → Click(ShowSlotDetail)이 섞여, 두 경로 순서에 따라 최종 상세가 비거나 남을 수 있다(순서 미확인).
- 고칠 방향: 슬롯 누름·끌기 시작을 포인터 인터페이스로 받거나, 상세 소유를 한 경로로 모은다.
- 검증: 묶음 검증 낮음

## 3 버림 (4)

- M-15 출발 규칙이 View 입력 판정·Plan 판단·이탈 판정·Buffer 단계 표·Core 거절 문구로 나뉨 — 넷은 같은 규칙의 사본이 아니라 서로 다른 질문(입력 제출 가능, Job 실행 시점, 로비 이탈 가능, 단계 전이)에 하나씩 답한다. 출발 거절 근거는 게임 규칙이라 Core 결과가 원본인 것이 데이터 경계에 맞다. 남는 것은 아직 없는 기능(로비 전용 거절 단계)이다. (증거 낮음 / 반론 버림 / 판정자 버림)
- N-07 꺼짐 훅(OnUIHidden → Hide) 경로의 관찰자 알림 예외가 오류 경계를 거치지 않음 — 사실이 틀렸다. 두 화면의 `OpenStateChanged` 유일한 구독자 `PopupViewScope.OnOpenStateChanged`가 본문 전체를 try/catch로 감싸 `_faults.Report`를 부른다.
- N-60 체스 말 복제 정책·이동 수명이 StageMap과 공유됨 — 같은 요구를 한 곳에 둔 정상적인 공유다. 실패 시나리오가 아직 없는 요구에 기대고, 갈라 두면 오히려 사본이 된다.
- N-62 로비 표 역할 목록이 표 switch와 편집기에 따로 있음 — 빠진 역할(`LobbyFacilityHoverDataSO`)은 범위 밖 편집기의 사본·결함이다. 범위 안 표 switch는 원본 하나로 문제가 없다.
