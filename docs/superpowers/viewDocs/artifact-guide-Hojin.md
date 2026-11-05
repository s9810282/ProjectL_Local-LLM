# 아티팩트 안내

- 작성자: 호진(이호진)
- 대상: 로비 아티팩트 팝업의 UI·3D View·입력·Core 처리·LifeCycle 조립을 고치거나 확인하는 사람.
- 설계(구조·결정의 원본): [아티팩트 팝업 설계](../specs/2026-10-06-artifact-popup-design-Hojin.md). 진행·남은 일: [아티팩트 팝업 계획](../plans/2026-10-05-artifact-popup-plan-Hojin.md).
- 정리: 2026-10-27 1부(입력·요청 Buffer 사용 안내)와 2부(ArtifactPopupUI 연결)를 따라 하기 좋은 순서로 다시 묶고 지금 코드와 맞지 않는 줄을 고쳤다. 날짜별 검증 수치는 계획으로 옮겼다. 옛 판은 git `10914570`.

## 1 한눈에

아티팩트는 로비 4번 시설에서 여는 팝업이다. StageSelect에는 진입점이 없다.

```text
[열기]  로비 아티팩트 시설 클릭 → LobbyInteraction → 체스 이동 완료 → LobbyFacilityActions
        → ArtifactPopupController.Open → PopupViewScope → UISystem.OpenUI(UIId.Artifact)
        → ArtifactPopupUI.Open → OpenStateChanged(동기) → 3D·입력·Layer 준비

[닫기]  닫기 Button → EventSystem → UIInputBinding → BindClose로 받은 수신자
        → UISystem.CloseUI → ArtifactPopupUI.Hide → OpenStateChanged(동기) → 정리

[편집]  보드·보유 목록 드래그 → UnityInputBridge → InputSystem.Submit → ArtifactInputController
        → ArtifactInputSlot → ArtifactDragController → ArtifactViewBuffer
        → LobbyLifeCycle: Runner가 비면 JobRegisterSystem.Register(Lobby, Next)
        → LobbyPlan: 사실 한 번 읽기 → Decide → ArtifactPlan.CreateCommand / CreatePreview + LobbyArtifactCheckJob
        → ArtifactEditingService: 기대 번호의 입력을 직접 읽어 계산·결과 게시
        → ArtifactDragController: 번호·데이터를 맞춰 표시하고 입력을 지운다
```

- 아티팩트 전용 실행기는 없다. Stage·Shop·Artifact·Combat이 같은 `JobRegisterSystem`·`JobRunnerSystem`을 쓴다.
- `ArtifactCommandProcessing.cs`는 따로 된 Core 서비스가 아니다. `ArtifactEditingService`의 partial이며 같은 객체가 `IArtifactEditingService`·`IArtifactCommandProcessingService`를 준다.
- UI는 받은 목록·문구·허용 상태만 그린다. 보드 상태·View DB 조회와 표시 모델 준비는 View(`ArtifactBoardRefresher`)가 한다.

## 2 ArtifactPopupUI 연결

### 2.1 Hierarchy와 Inspector

```text
ArtifactPopupCanvas (Canvas, CanvasScaler, GraphicRaycaster, ArtifactPopupUI)
└─ Panel (기본 비활성)
   ├─ InventoryBackdrop (Image)
   ├─ PoolArea (RectTransform, GridLayoutGroup)
   ├─ StatusText (TMP_Text)
   ├─ CloseButton (Button)
   │  └─ Label (TMP_Text)
   ├─ Title (TMP_Text)
   ├─ ArtifactName (TMP_Text)
   └─ EffectDescription (TMP_Text)
```

| Inspector 필드 | 연결 대상 |
|---|---|
| Panel | 별도 자식 Panel. `ArtifactPopupUI` 오브젝트 자체를 연결하면 안 된다 |
| Close Button | Panel 안의 CloseButton. Inspector On Click 목록은 비워 둔다 |
| Pool Area | 보유 슬롯 생성·반환 판정용 RectTransform |
| Slot Prefab | `Prefabs/UI/Artifact/ArtifactItem.prefab`의 Button |
| Status Text / Artifact Name / Effect Description | 상태·결과 문구, 이름, 효과 설명 TMP_Text |

- 열기 버튼은 없다. 로비 시설 도착이 연다.
- `ArtifactSceneRoot`에는 `popupView`·`popupUI`를, `ArtifactPopupView`에는 `worldRoot`·`backgroundRoot`·`cellPrefab`을 잇는다.
- 팝업 동안 숨기는 로비 공간 루트는 저작 칸이 아니다. 로비 연결이 `LobbySceneRoot.WorldRoot`를 `ArtifactPopupView.Configure`에 넘긴다(2026-11-05 로비 리팩토링 2차 M-12, 옛 `lobbyRoot` 칸은 걷음). `ArtifactSceneRoot`와 팝업 Canvas는 그 밖에 둔다(안에 두면 `Configure`·`Validate`가 예외를 낸다).
- 씬에 출력 Camera나 아티팩트 전용 가상 카메라를 두지 않는다. 출력 Camera는 `LobbyViewScope`가 연결 때 Boot Main Camera를 넘긴다.

### 2.2 열기·닫기와 입력 소유

- 루트는 늘 켜 두고 Open/Hide는 Panel만 켜고 끈다. UISystem의 바인딩 단계에서 Hide가 불려도 루트 참조가 남는다.
- 로비 시설에서 누르고 같은 시설에서 뗀 뒤 이동이 정상 완료되면 `LobbyFacilityActions → ArtifactPopupController.Open → PopupViewScope → IUISystemService.OpenUI(UIId.Artifact)`가 불린다.
- `ArtifactPopupController`는 로비 연결에서 받은 `PopupInputSession`을 덱 팝업과 함께 쓴다. 열릴 때 이동·시설 입력을 먼저 막고 Artifact + Common Layer를 잡는다.
- 닫기 버튼은 `UIInputBinding → ArtifactPopupUI → BindClose`로 이은 수신자를 부른다. Controller가 정상 닫기 표식을 남기고 UI 서비스로 닫는다. Hide 정리가 끝나고 호출이 돌아온 뒤, 같은 로비 연결·정상 상태일 때만 Lobby Layer와 시설 입력을 되돌린다. 캐릭터는 도착 칸에 남는다.
- 직접 `CloseUI`·`BackCloseUI`·`HideAllUI`·다른 UI로 바뀌는 Hide에는 정상 닫기 표식이 없다. 표시·프리뷰·입력 슬롯·자기 모달 토큰만 정리하고 로비 입력을 되돌리지 않는다.
- Detach·Shutdown·fault는 정상 복귀를 하지 않는다. 수신자와 동기 알림은 연결 수명을 확인해 이전 호출이 새 연결을 닫거나 프리뷰를 지우지 못하게 한다.
- `SetCanOpen(bool)`은 옛 이름을 유지한 UI 입력 허용이다. 닫기 버튼 interactable과 Click/Submit 허용을 정한다. 여는 권한은 Controller와 로비 시설 실행 포트가 갖는다.
- `OpenStateChanged(bool)`는 실제 Open/Hide를 같은 호출 안에 한 번 알린다. 알림 안에서 UI 서비스를 다시 부르지 않는다. 알림 처리 오류는 `RuntimeFaultBoundary`로 보고한다.

### 2.3 보유 목록·호버·문구

- 목록 슬롯은 `UIInputBinding`이 직접 전달한다. `Button.onClick` 리스너나 Inspector UnityEvent를 쓰지 않는다.
- 호버하면 이름·효과 설명을 보이고 이탈·비활성화·목록 교체 때 비운다. 이전 슬롯의 늦은 HoverExit는 새 슬롯이나 보드의 상세를 지우지 않는다.
- `RenderPool`은 받은 항목 값을 복사한다. 슬롯 Button과 모양 부품은 재사용하고 목록이 줄면 남은 슬롯을 숨긴다.
- `ShowDetail`·`ShowStatus`는 받은 문구만 표시한다. 타이머는 UI가 아니라 `ArtifactPopupStatus`가 갖는다(§5.2).
- 화면을 숨겨도 이미 제출한 편집은 취소되지 않는다. Controller가 숨김 Tick에서도 맞는 결과와 입력을 소비한다.
- `TryPickPool`·`IsReturnArea`·`IsBlockingArea`는 UI 영역 판정만 한다. 드래그·회전·회수는 InputSystem Artifact 경로로 View가 처리한다.

### 2.4 수명 알림

UI 소유 계약은 넷이다: 표시 `IArtifactPopupDisplay`(목록·상태 문구·상세), 수명 `IArtifactPopupLifetime`(열림 상태·알림·닫기 허용·닫기 수신자 연결), 영역 판정 `IArtifactPopupScreenAreas`, 닫기 수신 `IArtifactPopupCloseReceiver.CloseClicked()`. UI는 View 타입이나 Core 서비스를 찾지 않는다. 관찰자 하나가 예외를 내도 다른 관찰자를 부르고 `AggregateException`으로 원인을 지킨다. Hide의 finally에서 Panel과 문구를 정리한다. 옛 `IArtifactPopupHost`·`IArtifactPopupScreen` 연결 예제는 다시 쓰지 않는다.

## 3 조작

설정 원본은 [PlayerInputActions.inputactions](../../../Assets/ProjectL/Input/PlayerInputActions.inputactions)다.

| 입력 | 동작 |
|---|---|
| 아티팩트 시설 클릭 | 이동 완료 뒤 UISystem으로 연다 |
| 닫기 Button | `UIInputBinding`의 Click/Submit으로 지금 팝업 소유자에게 정상 닫기 |
| PointerPosition | 배치물 호버 정보 또는 드래그 후보 갱신 |
| Select: Pointer press, ReleaseOnly | 보유 항목·배치물 누름·드래그·정상 뗌 |
| Q / E: RotateLeft / RotateRight, PressOnly | 선택 중 회전 값과 최신 프리뷰 갱신 |
| 우클릭: Cancel | 배치물 회수. 액션 이름은 Cancel 그대로 |

1. 보유 항목을 보드로 끌어 놓으면 Place, 배치물의 점유 칸을 눌러 다른 칸에 놓으면 Move를 제출한다.
2. 배치물을 하단 보유 목록 영역에 놓으면 Return이다. 버튼·상세 UI·반환 영역이 아닌 보드 밖에서 놓으면 드래그만 취소한다.
3. 우클릭은 배치물을 끄는 중이면 그 인스턴스를, 아니면 포인터 아래 배치물을 회수한다. 보유 항목을 끄는 중이면 임시 선택만 정리한다.
4. 회전·후보 이동은 최신 프리뷰만 갱신한다. Q/E마다 편집 명령을 쌓지 않는다. 최종 칸·회전으로 Place/Move를 제출한다.
5. SelectCanceled·일반 닫기는 임시 드래그·프리뷰 정리이며 Return을 만들지 않는다.
6. 보유 슬롯은 UIInputBinding, 배치물은 PointerPosition·`ArtifactPointerMapper`로 호버를 받는다. 호버는 Buffer 쓰기나 Core 호출을 하지 않는다.

`Artifact/Close`와 ESC 닫기는 없다. ESC는 `Common/OpenSettings`다. 편집 승인과 프리뷰 판정은 Core 결과를 따른다.

## 4 입력·결과 Buffer

### 4.1 소유권

| 계약 | 쓰기·처리 소유자 | 뜻 |
|---|---|---|
| `ArtifactViewBuffer` | View | Place/Move/Return 입력 목록과 최신 프리뷰 하나 |
| `ArtifactBuffer.Current` | Core | 초기화 여부·보드 크기·보유/배치 상태를 담은 `ArtifactBufferSnapshot` |
| `ArtifactCommandResults` | Core 게시, View·로비 Plan 읽기 | `HandledCommandNumber`, `LastCommand`, `AnsweredPreviewNumber`, `LastAnsweredPreview` |

View는 `IArtifactBufferReadSink.Current`로 화면을 준비한다. 게임 상태 쓰기는 Core가 계산 결과를 게시하는 경로에만 있다.

| 입력 | 생성 | 식별자 |
|---|---|---|
| Place | `ArtifactViewCommandData.Place(artifactId, cell, rotation)` | 종류 ID |
| Move | `ArtifactViewCommandData.Move(instanceId, cell, rotation)` | 배치 인스턴스 ID |
| Return | `ArtifactViewCommandData.Return(instanceId)` | 배치 인스턴스 ID |
| Place/Move 프리뷰 | `ArtifactViewPreviewData.Place/Move(...)` | 실제 편집과 같은 대상·칸·회전 |

`CellCoord`·`Rotation4`는 Core 계약 형식이다. 요청에 GameObject·화면 좌표·SO·배치 가능 여부를 넣지 않는다.

### 4.2 API와 번호

| API | 동작 |
|---|---|
| `Commands`, `TryGetNextCommand(handledThrough, out command)` | 지우지 않은 편집 목록과 순차 조회. 조회는 지우지 않는다 |
| `Submit(data)` | 편집 입력을 더하고 번호를 돌려준다 |
| `HasPreview`, `Preview` | 지금 최신 프리뷰 |
| `SetPreview(data)` | 최신 프리뷰 교체, 새 번호 |
| `ClearPreview()` | 프리뷰만 지운다 |
| `RemoveCommandsThrough(handledNumber)` | View가 확인한 번호까지 편집 입력을 지운다 |
| `Clear()` | 편집 입력과 프리뷰를 함께 지운다 |

- 편집과 프리뷰는 이 Buffer의 같은 증가 번호를 쓴다. 편집 번호 사이에 프리뷰 번호가 낄 수 있다. 지운 뒤에도 번호를 다시 쓰지 않고 다른 Buffer 번호와 섞지 않는다.
- 로비가 활성인 동안 Runner가 비면 `LobbyLifeCycle`이 다음 판단을 한 번 건다. `LobbyPlan.Decide`가 보드가 준비됐을 때 미처리 편집 → 최신 미응답 프리뷰 → 출발 순서로 하나를 고른다. 기록 누락으로 멈춘 아티팩트는 건너뛴다. 보드 준비 전 편집은 기다린다.
- Job은 `ProcessNextCommand(expectedRequestNumber)`·`ProcessLatestPreview(expectedRequestNumber)`를 부른다. Core가 입력을 다시 읽어 이미 처리됐거나 취소·교체된 요청을 건너뛰고, 게시 직전에도 같은 번호가 남았는지 본다. 바로 뒤 `LobbyArtifactCheckJob`이 처리 기록이 남았는지 확인하고, 남지 않았으면 아티팩트 도메인만 멈춘다.
- View는 결과 번호와 입력 데이터를 맞춘 뒤 문구를 반영하고 입력을 지운다. 프리뷰도 지금 요청 번호·데이터에 맞는 최신 응답만 표시한다. 같은 후보로는 프리뷰를 다시 내지 않는다. 이동 프리뷰 대상이 더는 배치돼 있지 않으면 Core가 번호가 있는 생략 응답을 낼 수 있다.

## 5 Controller·Scope 수명

### 5.1 조립과 열고 닫기

[LobbyViewSystem](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Lobby/LobbyViewSystem.cs)이 Framework 단계와 연결별 `LobbyViewScope`를 갖는다. PostBindService에서 슬롯·Global 서비스 참조를 받고 Initialize에서 `ArtifactPopupViewFactory`를 조립한다. Scope가 Factory로 만든 [ArtifactPopupController](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactPopupController.cs)를 보관·폐기한다. UI·Layer 열림 수명은 [PopupViewScope](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Common/PopupViewScope.cs)가 갖는다.

```csharp
var input = resolver.GetService(SystemGroupsService.InGame).ViewGroup.Input;
IArtifactInputSlot slot = input.Artifact;
```

- `ArtifactInputFactory`가 Controller·슬롯·Layer·키 설정 대상을 준비하고, 같은 슬롯을 `ViewGroup.Input.Artifact`로 준다. 팝업마다 슬롯을 다시 만들거나 루트 서비스로 찾지 않는다. Configure는 `Bridge.InitData`에서 한 번만 한다.
- Controller 생성자는 서비스 묶음 `ArtifactPopupServices`와 `IArtifactPopupPresenter`(보드)·`IArtifactDragPresenter`(드래그)·`IArtifactPopupDisplay`·`IArtifactPopupLifetime`(UI)·포인터 판정을 받는다. 운영 `ArtifactPopupView`가 두 Presenter를, `ArtifactPopupUI`가 UI 계약 셋을 구현한다. 공유 장면은 "현재 호스트만" 감싼 판으로만 쥔다.
- `ArtifactPopupView.Configure`가 보드 좌표 변환기 하나와 드래그 표시를 함께 준비한다. 드래그 표시와 포인터 판정이 같은 변환기를 쓴다.
- UI 허용을 false로 둔 뒤 연결별 표식을 만들고 `OpenStateChanged`를 한 번 구독한다. 이미 열린 UI면 지금 상태를 바로 맞춘다. 이전 연결의 알림은 표식 비교로 무시한다.
- Open은 `PopupInputSession.Begin(Artifact)`로 모달 입력을 먼저 잡은 뒤 UI 서비스를 부른다. Panel이 켜진 뒤의 동기 Open 알림에서 보드·목록을 그리므로 실제 레이아웃 크기로 모양을 계산한다.
- 일반 Close는 같은 호출 안에서 드래그·프리뷰를 취소하고 슬롯을 풀고 3D를 숨긴다.
- Detach·StopInput은 Scope를 정리하고 `Controller.Dispose`로 허용·수명 구독·드래그 수신자·슬롯·입력·렌더 구독을 푼다. 씬 이탈 중에는 로비 Layer를 다시 켜지 않는다.
- 공용 오류 콜백은 View 그룹이 소유한 `InGameViewFaultCleanup` 하나다. Controller·Scope는 자기 입력·임시 표현만 정리하고 따로 오류 구독을 더하지 않는다.

### 5.2 상태 문구 수명

`ArtifactPopupStatus`가 문구 우선순위와 시간 수명을 갖는다. `LobbyViewSystem.PostBindService`가 Global Utility의 `IDelayService`를 받아 Factory·Controller에 넘긴다. 새 타이머 서비스나 Update는 없다.

- `OutOfBounds`(보드 밖)·`CellOccupied`(겹침)는 프리뷰·편집 결과 모두 처음 표시할 때 3초 만료를 예약한다. Bootstrap이 넘기는 런타임 `deltaTime` 기준이라 시간이 멈추면 만료도 멈춘다.
- 새 결과는 같은 문구라도 3초를 다시 센다. 같은 요청 번호를 다시 소비하거나 고정된 프리뷰가 반복돼도 예약을 늘리지 않는다. 우선순위에 가려 한 번도 표시되지 않은 오류는 실제로 표시될 때 시간을 센다.
- Delay 서비스에는 취소가 없어 채널별 버전을 비교한다. Close·Detach·StopInput은 드래그 정리보다 먼저 상태를 멈춰 옛 예약이 새 문구나 해제된 화면을 바꾸지 못하게 한다. 만료는 문구만 지운다.
- 닫을 때 보관한 임시 오류는 버린다. 숨긴 동안 새로 온 편집 오류는 보관했다가 다시 열 때 처음 표시하며 3초를 준다.

## 6 씬·카메라·이탈

- Boot는 `ArtifactBoardPreparation.Request → 초기화 Job → 결과 게시 + Runner Idle → IsReady` 뒤에 첫 Lobby/Stage/Combat 진입을 요청한다. 이미 준비된 보드는 다시 초기화하지 않는다.
- 운영 Canvas·WorldRoot는 `Lobby.unity`에 하나씩 있다. 공용 `ArtifactItem`·`ArtifactCell` 프리팹을 쓴다.
- 로비 화면은 고정 가상 카메라 `LobbyCamera`가 비추고 출력은 Boot Main Camera가 한다. 아티팩트 보드·배경은 그 구도 안에 있다. 팝업이 열리면 WorldRoot·배경을 켜고 로비 공간 루트(`LobbySceneRoot.WorldRoot`)를 숨기며, 닫을 때 되돌린다.
- 열린 동안에만 `ArtifactPopupOutputMask`가 출력 Camera Culling Mask에 표시 설정 `outputLayerName`(기본 UI) 층을 더하고, 닫기·해제 때 대여 전 값으로 되돌린다. Framework 카메라 카탈로그의 공개 손잡이로만 바꾸고 Brain·Boot 카메라를 직접 만지지 않는다.
- 보드 피킹과 배경 배치는 이 출력 Camera를 쓴다. WorldRoot 로컬 XZ의 Board Center·Cell Spacing을 매핑·모델·프리뷰가 함께 쓰고, 논리 보드 크기는 Buffer에서 받는다.
- 정상 로비 이탈은 `LobbyLifeCycle`의 씬 이탈 노드(우선순위 −200)에서 `LobbyPlan.CanLeave`로 확인한다. 처리 안 된 편집이나 진행 중인 출발이 남으면 예외로 멈춘다. 답하지 않은 프리뷰는 막지 않는다. 입력 Clear를 처리 성공으로 세지 않는다. 출발은 [로비 안내](lobby-guide-Hojin.md)를 따른다.

## 7 소스 위치

| 볼 것 | 소스 |
|---|---|
| UI 전환·동기 수명 계약 | [ArtifactPopupUI](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/ArtifactPopupUI.cs), [IArtifactPopupDisplay](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/IArtifactPopupDisplay.cs), [IArtifactPopupLifetime](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/IArtifactPopupLifetime.cs) |
| 입력 해석·제출·결과 소비 | [ArtifactDragController](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactDragController.cs) |
| 입력·프리뷰 보관 | [ArtifactViewBuffer](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/ArtifactView/ArtifactViewBuffer.cs) |
| 실행 조정·Job 구성 | [LobbyLifeCycle](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Lobby/LobbyLifeCycle.cs), [LobbyPlan](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Lobby/Plans/LobbyPlan.cs), [ArtifactPlan](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Lobby/Artifact/Plans/ArtifactPlan.cs) |
| Core 직접 조회·계산·게시 | [ArtifactEditingService](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/ArtifactSystem/ArtifactEditingService.cs), [ArtifactCommandProcessing](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/ArtifactSystem/ArtifactCommandProcessing.cs) |
| 상태·번호 결과 | [ArtifactBuffer](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/Artifact/ArtifactBuffer.cs) |
| 보드 준비 | [ArtifactBoardPreparation](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/ArtifactPreparation/ArtifactBoardPreparation.cs) |

## 8 고칠 때 지킬 것

- [Framework 개발 가이드](../../../Assets/ProjectL/Script/Systems/Documentation/framework-programmer-guide.md) FW-006/007은 InputSystem 설정·Submit 경로, FW-008/009는 등록된 UI의 준비·전환 이력에 적용되는 MUST다.
- [Assets/AGENTS.md](../../../Assets/AGENTS.md)의 버튼 규칙은 `UIInputBinding + IUIInputReceiver`다. `Button.onClick.AddListener`·Inspector UnityEvent로 입력을 잇지 않는다. 모든 동기 수명 알림을 금지한다는 뜻은 아니다.
- Buffer 소유권, 번호가 있는 Plan/Job, UI가 상태·DB를 읽지 않는 역할 분리, 우클릭 회수, ESC 닫기 제거는 아티팩트의 프로젝트 결정이다. 공용 UI/Framework가 모든 기능에 이 꼴을 강제한다고 넓히지 않는다.
- 일반 조립·서비스·수명 원칙은 [Framework 구조 지침](../framework/Framework%20Development%20Guidelines.md)을 따른다.
- 배치판 프리팹 `ArtifactFloor.prefab`은 FBX 중첩 인스턴스를 풀어 둔 상태다. 원본 `ArtifactSpace_final.fbx`를 바꾸면 프리팹을 손으로 맞춰야 한다. 시험은 배치면을 이름 `ArtifactSpace_floor_001`로 찾는다.
