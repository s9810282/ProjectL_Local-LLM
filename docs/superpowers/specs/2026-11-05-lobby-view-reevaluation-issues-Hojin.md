# 로비 View 재평가 — 문제 목록 (2026-11-05)

- 작성자: 호진(이호진)
- 기준 판: `cc3ccdf96`
- 보고서: [재평가 보고서](2026-11-05-lobby-view-reevaluation-Hojin.md)
- 개수: 중간 1 · 낮음 92 · 버림 6
  - 낮음 92 = 낮음 후보 유지 87 + 중간 후보에서 내린 5
- 경로 머리 약어
  - `S/` = `Assets/ProjectL/Script/Systems/`
  - `V/` = `S/RuntimeSystem/InGameSystem/ViewSystem/`
- 검증 표기
  - 중간 후보: "증거 / 반론 [/ 판정자]"
  - 낮음 후보: "묶음 검증 A"(R-N-01~46) / "묶음 검증 B"(R-N-47~91)

## 1 중간

### R-M-03 아티팩트 외형 프리팹이 선택 칸이라 빠진 모델이 조용히 안 보임
- 위치: `S/DataBaseSystem/Data/View/Table/Artifact/Item/ArtifactViewData.cs:41-44`, `ArtifactViewDataSO.cs:37`, `V/ArtifactPopup/ArtifactPopupView.cs:120-123`, `V/ArtifactPopup/ArtifactDragDisplay.cs:57,75,109`, `V/ArtifactPopup/ArtifactBoardRefresher.cs:77-78`
- 내용: 프리팹이 null이면 DB 검사를 건너뛴다. 툴팁은 "필요 없으면 비워 둠"이다. 유일한 소비처인 배치판 View 세 곳은 null을 `continue`/return으로 넘긴다. 비워도 되는 소비처는 코드에 없다.
- 실패 시나리오: 새 외형 에셋에서 프리팹을 빠뜨리면 그 아티팩트는 칸만 차지하고 보이지 않는다. 끌 때도 모델이 없다. 예외·경고가 없다. 지금 에셋 셋은 모두 프리팹이 있다.
- 고칠 방향: 프리팹을 필수로 바꿔 `RequirePrefabAsset`를 늘 부른다. View의 null 갈래를 걷는다.
- 검증: 증거 중간 / 반론 중간. 조용히 묻히는 조건부 구성 오류.
- **고침(2026-11-05, 사용자 "R-M-03만 고쳐", Unity 확인 문제 없음 — 사용자, 새 시험 없음, 2차 계획 §3.13):** `ArtifactViewData.Validate`가 늘 `RequirePrefabAsset`를 부르고, 툴팁은 필수로, `ArtifactPopupItem`은 프리팹 null이면 예외, `ArtifactPopupView`의 `continue`와 `ArtifactDragDisplay`의 null 갈래(Prepare·Begin)를 걷었다.

## 2 낮음

### C1 SSOT (16)

#### R-M-01 입력 콜백 오류 경계 감싸개가 세 벌이고 공용 수신 Slot은 감싸지 않음
- 위치: `V/Common/FaultReportingUIInputReceiver.cs:22-41`, `V/Lobby/FaultReportingLobbyReceiver.cs:22-48`, `V/ArtifactPopup/ArtifactPopupController.cs:225-293`, `V/ArtifactInput/ArtifactInputSlot.cs:46-58`, `V/LobbyInput/LobbyReceiverSlot.cs:31-37`
- 내용: "정지면 안 부름 → try → catch Report" 규칙이 UI 수신자 감싸개·로비 시설 감싸개·아티팩트 컨트롤러 안 수신자에 각각 있다. Slot 둘은 수신자를 그대로 부른다.
- 실패 시나리오: 새 수신자가 Slot에 Bind하며 감싸기를 잊으면 예외가 F1을 우회한다. 정책을 바꾸려면 세 곳을 함께 고친다. 지금 Bind 지점은 하나씩이고 모두 감싼다.
- 고칠 방향: Slot 전달 지점 한 곳에서 정지 확인 + Report. 또는 Common 정적 도우미 하나로.
- 검증: 증거 낮음 / 반론 낮음. 출처 중간 → 낮음. 수신 계약이 셋이라 생긴 3~4줄 관용구 사본이고 지금 우회 경로가 없다. "아티팩트 수신자에 정지 확인 없음" 주장은 틀림(`IsDisplayCurrent` ⊃ `CanWork`).

#### R-N-04 입력 Layer·오디오 서비스가 묶음 안 두 경로로 노출
- 위치: `V/Lobby/LobbyViewServices.cs:44-45,49,67-68`, `V/Lobby/LobbyInputServices.cs:16,20`, `V/Lobby/LobbyViewScope.cs:70`, `V/Lobby/LobbyViewInput.cs:24,28`, `V/Lobby/LobbyViewSystem.cs:177`
- 내용: 같은 `IInputLayerService`·`IAudioSystemService`가 `LobbyViewServices`와 `.Input` 둘로 노출된다. 쓰는 쪽마다 경로가 다르다.
- 실패 시나리오: 두 경로를 오가며 읽어 원본을 찾기 어렵다. 같은 생성자 인자라 지금 한쪽만 바뀌지는 않는다.
- 고칠 방향: 한 경로만 남긴다.
- 검증: 묶음 검증 A 유지. 두 경로 노출만 인정.

#### R-N-16 실패 사유 → 문구와 → 임시 여부가 두 파일에 나뉨
- 위치: `V/ArtifactPopup/ArtifactPopupText.cs:15-33`, `V/ArtifactPopup/ArtifactPopupStatus.cs:172-173`
- 내용: 문구 switch와 임시 여부 판정이 따로 있다.
- 실패 시나리오: 새 실패 사유에서 한쪽을 빠뜨리면 영구 문구로 남는다.
- 고칠 방향: (문구, 임시 여부)를 한 표에서.
- 검증: 묶음 검증 A 유지.

#### R-N-27 유한값·회전 정의 검사가 곳마다 다른 식
- 위치: `V/Common/ChessPieceMotion.cs:83,101,103,129,147,239`, `S/DataBaseSystem/Data/View/Table/Artifact/Item/ArtifactPopupConfigData.cs:35-38`, `S/RuntimeSystem/GlobalSystem/UISystem/Screens/Lobby/LobbyDeckUI.cs:234`, `S/DataBaseSystem/Data/View/Table/LobbyVisual/Item/LobbyBoardData.cs:69,93`, `S/RuntimeSystem/BufferSystems/ArtifactView/ArtifactViewCommand.cs:79-82`
- 내용: `ViewPrefabRules.IsFinite`·`float.IsFinite`·`IsNaN || IsInfinity`가 섞이고 `Rotation4` 정의 검사가 Buffer·DB에 각각 있다.
- 실패 시나리오: 기준을 바꾸면 여러 곳을 맞춘다.
- 고칠 방향: 판정 하나로.
- 검증: 묶음 검증 A 유지.

#### R-N-33 열림 상태 관찰자 알림 함수가 두 화면에 사본, 건너뜀·순서 규칙이 다름
- 위치: `S/RuntimeSystem/GlobalSystem/UISystem/Screens/Lobby/LobbyDeckUI.cs:104-107,307-328`, `S/RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/ArtifactPopupUI.cs:86-96,100-118`
- 내용: 덱은 상태가 바뀌면 남은 관찰자를 건너뛰고 패널을 끈 뒤 알린다. 아티팩트는 끝까지 부르고 끄기 전에 알린다.
- 실패 시나리오: 같은 계약 소비자가 화면마다 다른 규칙을 받는다.
- 고칠 방향: 공용 도우미 하나로.
- 검증: 묶음 검증 A 유지.

#### R-N-34 덱 화면 `_acceptInput`이 두 사실에서 늘 파생되는 사본 상태
- 위치: `S/RuntimeSystem/GlobalSystem/UISystem/Screens/Lobby/LobbyDeckUI.cs:33,43,76,99,131,167,205,215,297`
- 내용: 세울 때마다 `_isOpen && _receiver != null`이고 읽는 쪽도 다시 본다.
- 실패 시나리오: 갱신 경로 하나를 빠뜨리면 입력이 꺼진 채 남는다.
- 고칠 방향: 판정 하나로 계산.
- 검증: 묶음 검증 A 유지.

#### R-N-47 값 하한이 Inspector 속성과 Validate에 두 벌, 수도 다름
- 위치: `S/DataBaseSystem/Data/View/Table/Artifact/Item/ArtifactPopupConfigDataSO.cs:18-20` ↔ `ArtifactPopupConfigData.cs:35-37`
- 내용: 편집기는 `[Min(0.01f)]`, 검사는 `<= 0` 거절로 기준이 갈린다.
- 실패 시나리오: 한쪽만 바꾸면 편집기와 검사가 다른 값을 허용한다.
- 고칠 방향: 조건 원본은 Validate 하나로.
- 검증: 묶음 검증 B 유지. 범위 정정 — `ArtifactViewDataSO.cs:23` ↔ `ArtifactViewData.cs:29` 짝은 같은 기준이라 뺌.

#### R-N-48 SO 필드 초기값이 기본 에셋 값의 사본(마법 수)
- 위치: `S/DataBaseSystem/Data/View/Table/LobbyVisual/Item/LobbyCharacterPresentationDataSO.cs:10,12`, `LobbyBoardDataSO.cs:14-28`
- 내용: 에셋 보정 값을 코드 초기값으로 복사했다.
- 실패 시나리오: 에셋 값을 바꿔도 새 에셋에는 옛 보정이 들어간다.
- 고칠 방향: 중립값만 둔다.
- 검증: 묶음 검증 B 유지.

#### R-N-49 세 조회 표의 구성 검사 방식이 제각각, "역할 하나" 검사 두 벌
- 위치: `S/DataBaseSystem/Queries/ViewDataBase/ViewTable/ArtifactPopupConfigTable.cs:21-32`, `LobbyVisualTable.cs:33-88`, `ArtifactViewTable.cs:37-53`
- 내용: null·Missing 처리와 문구가 셋 다 다르다. `ArtifactViewTable` 실패 문구는 어느 에셋인지 알 수 없다.
- 실패 시나리오: 구성 오류 때 원인 에셋을 찾기 어렵고 규칙이 갈린다.
- 고칠 방향: 도우미 하나로, 문구에 SO 이름.
- 검증: 묶음 검증 B 유지.

#### R-N-62 판 칸 ↔ 위치 식·범위 판정이 로비 판과 아티팩트 판에 따로
- 위치: `V/Lobby/LobbyBoard.cs:47-68`, `V/ArtifactPopup/ArtifactBoardMapper.cs:22-37,62-64`, `S/DataBaseSystem/Data/View/Table/LobbyVisual/Item/LobbyBoardData.cs:62-63`
- 내용: 중심 기준 격자 식과 역변환(반올림/내림)을 각자 구현한다.
- 실패 시나리오: 판 기준을 한쪽만 바꾸면 규칙이 갈린다.
- 고칠 방향: 공용 격자 값 하나로.
- 검증: 묶음 검증 B 유지.

#### R-N-63 "시설 종류마다 정확히 하나" 규칙이 세 곳에서 세 방식
- 위치: `S/DataBaseSystem/Data/View/Table/LobbyVisual/Item/LobbyBoardData.cs:46-50,73-85`, `LobbyVisualPrefabRules.cs:32-45`, `V/Lobby/LobbyFacilityActions.cs:22-35`
- 내용: TryAdd·비트마스크(0~31 가정)·Count 비교로 각자 검사한다.
- 실패 시나리오: 선택 시설 도입 등에서 셋을 따로 고친다.
- 고칠 방향: 검사 하나로.
- 검증: 묶음 검증 B 유지.

#### R-N-64 "팝업을 열 수 있나" 판정이 아티팩트와 덱에서 다른 원본
- 위치: `V/ArtifactPopup/ArtifactPopupController.cs:79,102,107,169`, `V/Lobby/LobbyArtifactPopupFactory.cs:41`, `V/Lobby/Deck/LobbyDeckPopupController.cs:40`, `V/Common/PopupViewScope.cs:40`
- 내용: 덱은 `PopupViewScope.CanOpen`, 아티팩트는 `_started && CanWork`다. 같은 값이 닫기 허용에도 쓰인다.
- 실패 시나리오: 이미 열린 아티팩트에 `TryExecute`가 true를 돌려준다. 반환값에 기대는 변경 때 드러난다.
- 고칠 방향: Scope 판정을 원본으로.
- 검증: 묶음 검증 B 유지.

#### R-N-65 "연결이 살아 있나" 식이 부품마다 따로 조합
- 위치: `V/Lobby/LobbyViewScope.cs:56,70-71,127`, `V/ArtifactPopup/ArtifactPopupController.cs:80`, `V/Common/PopupViewScope.cs:44-45`, `V/Lobby/LobbyViewSystem.cs:148,206`
- 내용: `!disposed && current && !IsFaulted`가 셋, 로비 Scope 안 두 조합도 따로다.
- 실패 시나리오: 조건 하나를 더할 때 하나를 놓치면 부품 판정이 어긋난다.
- 고칠 방향: 호스트 계약 하나에서 파생.
- 검증: 묶음 검증 B 유지.

#### R-N-66 "지금 Layer가 내 것인가 / 내 것일 때만 비우기" 규칙 사본
- 위치: `V/Lobby/LobbyViewInput.cs:28,56,74`, `V/Common/PopupInputSession.cs:152-158`, `V/ArtifactPopup/ArtifactPopupController.cs:75,207-208`
- 내용: 아티팩트 Layer 상수를 생성 인자와 입력 허용 판정에서 따로 쓴다.
- 실패 시나리오: 하나만 바꾸면 열리지만 입력이 막힌다.
- 고칠 방향: Scope 질의·`ClearIf` 하나로.
- 검증: 묶음 검증 B 유지.

#### R-N-67 Play/에디터 갈래 파괴 식 사본 셋
- 위치: `V/Lobby/LobbyCharacterVisual.cs:63-64`, `V/Lobby/LobbyVisualSet.cs:56-57`, `V/Lobby/LobbyFacilityHighlight.cs:76-77`
- 내용: `isPlaying ? Destroy : DestroyImmediate`가 세 곳에 있다.
- 실패 시나리오: 파괴 정책을 바꿀 때 세 곳을 고친다.
- 고칠 방향: 공용 도우미 하나.
- 검증: 묶음 검증 B 유지.

#### R-N-70 덱 목록 슬롯 유효성 판정이 UI와 Presenter에 따로
- 위치: `S/RuntimeSystem/GlobalSystem/UISystem/Screens/Lobby/LobbyDeckUI.cs:135,260,264-265`, `V/Lobby/Deck/LobbyDeckPresenter.cs:107`
- 내용: UI는 `_visibleCount`, Presenter는 모델 카드 수다. UI 안에서도 HoverExit과 `IsInteractiveSlot` 조건이 다르다.
- 실패 시나리오: 부분 갱신 등에서 어긋난다.
- 고칠 방향: 원본을 Presenter 모델로.
- 검증: 묶음 검증 B 유지.

### C2 SOLID (8)

#### R-N-05 팝업 생성 계약이 모든 팝업에 출력 카메라·로비 루트를 강요
- 위치: `V/Lobby/ILobbyPopupFactory.cs:14`, `V/Lobby/Deck/LobbyDeckPopupFactory.cs:24`
- 내용: `Create(host, input, output, lobbyRoot)` 중 덱은 뒤 둘을 쓰지 않는다(ISP).
- 실패 시나리오: 새 팝업도 쓰지 않는 인자를 받아야 한다.
- 고칠 방향: 공통 재료만 계약에, 나머지는 아티팩트 Factory가 생성 때 받는다.
- 검증: 묶음 검증 A 유지.

#### R-N-10 덱 Factory가 null 캡처 클로저로 Presenter ↔ 컨트롤러 순환을 만듦
- 위치: `V/Lobby/Deck/LobbyDeckPopupFactory.cs:27-29`
- 내용: `() => controller?.Close()`를 먼저 넘긴다.
- 실패 시나리오: 닫기가 생성 순서와 null 허용에 기댄다.
- 고칠 방향: 컨트롤러가 Presenter를 만들며 Close를 넘기거나 좁은 계약으로 알린다.
- 검증: 묶음 검증 A 유지.

#### R-N-17 드래그 컨트롤러가 호버 상세·편집 결과 소비·끌기를 함께 갖고 깃발이 얽힘
- 위치: `V/ArtifactPopup/ArtifactDragController.cs:24-37,60-113,201-233,267-285`
- 내용: 세 책임과 깃발 다섯이 한 클래스에 있다.
- 실패 시나리오: 호버 규칙만 바꿔도 취소·정지 경로의 깃발 초기화를 함께 맞춘다.
- 고칠 방향: 호버 상세를 부품으로 떼고 편집 결과 소비는 컨트롤러로.
- 검증: 묶음 검증 A 유지.

#### R-N-28 로비·아티팩트 입력 수신 계약의 좌표 전달 방식이 다름
- 위치: `V/LobbyInput/ILobbyInputReceiver.cs:6-12`, `V/LobbyInput/LobbyInputController.cs:39-46`, `V/ArtifactInput/IArtifactInputReceiver.cs:6-8`, `V/ArtifactInput/ArtifactInputController.cs:42-47`
- 내용: 같은 해석기 위에서 아티팩트는 좌표를 인자로, 로비는 직전 `OnPointer`로 넘긴다.
- 실패 시나리오: 로비는 선택과 좌표가 갈릴 수 있는 구조다.
- 고칠 방향: 로비도 좌표 인자로.
- 검증: 묶음 검증 A 유지.

#### R-N-38 덱 Presenter가 계약 대신 구체 `LobbyDeckUI.CloseSlot` 상수에 기댐
- 위치: `V/Lobby/Deck/LobbyDeckPresenter.cs:68`, `S/RuntimeSystem/GlobalSystem/UISystem/Screens/Lobby/LobbyDeckUI.cs:14-15`, `S/RuntimeSystem/GlobalSystem/UISystem/Screens/Lobby/ILobbyDeckDisplay.cs:7-8`
- 내용: 계약 주석은 "구체 화면을 알지 않는다"인데 닫기 슬롯 번호를 구체 UI의 internal 상수로 읽는다.
- 실패 시나리오: 다른 구현은 닫기를 카드로 읽을 수 있다.
- 고칠 방향: 계약으로 올리거나 닫기 메서드를 나눈다.
- 검증: 묶음 검증 A 유지.

#### R-N-50 `ILobbyVisualTableQuery`가 다섯 역할을 한 계약·한 switch로 묶음
- 위치: `S/DataBaseSystem/Queries/ViewDataBase/ViewTable/LobbyVisualTable.cs:9-16,40-76`
- 내용: 역할 추가에 계약·속성·case·Missing 네 곳을 고친다. 소비자는 전체 계약에 기댄다.
- 실패 시나리오: 역할 하나 추가가 네 곳 수정과 넓은 재검증을 부른다.
- 고칠 방향: 역할별 좁은 계약이나 등록 표 하나로.
- 검증: 묶음 검증 B 유지.

#### R-N-73 공통 팝업 계약에 아티팩트 전용 "출력 복원" 단계
- 위치: `V/Lobby/ILobbyPopup.cs:20-21`, `V/Lobby/Deck/LobbyDeckPopupController.cs:53-56`, `V/Lobby/LobbyViewPopups.cs:72-82`, `V/Lobby/LobbyViewScope.cs:113-116`, `V/ArtifactPopup/ArtifactPopupController.cs:146,180,195`
- 내용: 덱은 빈 구현이다. 목록·Scope가 아티팩트 요구 순서를 공통 단계로 조합한다. 아티팩트는 같은 출력 해제를 세 입구에서 부른다.
- 실패 시나리오: 팝업마다 쓰지 않는 단계를 구현하고 순서를 맞춘다.
- 고칠 방향: 좁은 계약으로 나누거나 필요 여부를 다시 본다.
- 검증: 묶음 검증 B 유지.

#### R-N-76 ArtifactPopupController가 수명·표시 순서·Tick·수신자·호스트 감싸개 넷을 모두 쥠
- 위치: `V/ArtifactPopup/ArtifactPopupController.cs:20-419`
- 내용: 약 420줄에 수명·표시 순서(:148-170)·Tick·수신자 둘·감싸개 넷(:65-69)이 함께 있다.
- 실패 시나리오: 감싸개 규칙이나 입력 수신 방식 하나를 바꿔도 표시·수명 코드가 섞인 파일을 연다.
- 고칠 방향: 감싸개를 별도 파일로, 표시 순서를 부품으로.
- 검증: 묶음 검증 B 유지.

### C3 FW 계약 (1)

#### R-N-41 등록·조회하는 곳이 없는 서비스 키 둘과 슬롯의 Export 입구
- 위치: `S/ServiceKeys/Runtime/ArtifactInputKey.cs:6-13`, `S/ServiceKeys/Runtime/LobbyInputKey.cs:6-12`, `V/LobbyInput/LobbyReceiverSlot.cs:9,39-47`, `V/ArtifactInput/ArtifactInputSlot.cs:11,60-66`
- 내용: 키·`ExportServices`가 있지만 어떤 Builder에도 Export되지 않고 조회도 없다. 전달은 `InGameViewInputContext`다.
- 실패 시나리오: 새 작업자가 키로 조회하거나 루트에 재Export해 두 번째 경로·두 번째 슬롯을 만들 수 있다.
- 고칠 방향: 걷거나 시험 전용임을 명시.
- 검증: 묶음 검증 A 유지. C5(죽은 경로)로 볼 수도 있음.

### C4 기능 변경의 독립성 (7)

#### R-M-06 팝업 하나·아티팩트 의존 하나 추가에 로비 System·서비스 묶음을 함께 고침
- 위치: `V/Lobby/LobbyViewSystem.cs:61-97`, `V/Lobby/LobbyViewServices.cs:29-55`, `S/Factory/Runtime/InGame/View/LobbySceneFactory.cs:22-33`, `S/RuntimeSystem/GlobalSystem/UISystem/Screens/Lobby/LobbyCanvasView.cs:10-35`, `V/LobbyInput/LobbyInputLayers.cs:13`, `V/LobbyInput/LobbyInputFactory.cs:13-15`
- 내용: `LobbyViewServices` 생성자가 21개 인자를 받고 그중 9개는 아티팩트 전용이다. `ArtifactPopupServices`(12인자)도 그 안에서 조립한다.
- 실패 시나리오: 아티팩트 의존 하나를 더하면 `ArtifactPopupServices`·`LobbyViewServices`·`LobbyViewSystem.PostBindService` 세 곳을 고친다. 빠뜨리면 컴파일 오류나 진입 즉시 예외로 드러난다.
- 고칠 방향: 아티팩트 Factory가 Global Context·Buffer에서 직접 받는다.
- 검증: 증거 중간 / 반론 낮음 / 판정자 낮음 → 가운데 값 낮음. 출처 중간 → 낮음. 기계적 생성자 전달이고 조용히 틀어지지 않는다. "새 팝업 하나에 약 9곳"은 과장(팝업 등록은 목록 한 줄, 아티팩트 Layer는 아티팩트 소유). 줄 번호 정정: `LobbyCanvasView.cs`는 38줄(:10-35), `LobbyInputFactory.cs`는 31줄(:13-15).

#### R-N-32 팝업 수명 두 클래스의 재진입 방어가 겹쳐 변경 비용이 큼
- 위치: `V/Common/PopupViewScope.cs:19-24,57-139,287-333`, `V/Common/PopupInputSession.cs:13-14,31-150`
- 내용: 같은 "동기 콜백 중 교체·폐기" 사실을 두 층이 각각 판정한다.
- 실패 시나리오: 열기 규칙 하나를 바꿔도 두 클래스 상태 조합과 세 팝업을 다시 검증한다.
- 고칠 방향: 현재성 판정을 Scope 한 곳으로.
- 검증: 묶음 검증 A 유지.

#### R-N-55 아티팩트 요청 종류(편집·프리뷰)가 여러 열거·분기에 흩어짐
- 위치: `S/RuntimeSystem/LifeCycleSystem/LifeCycles/Lobby/Plans/LobbyStep.cs:4-16`, `…/Lobby/Artifact/LobbyArtifactDomain.cs:13-17,108-111`, `…/Lobby/Artifact/Plans/ArtifactPlan.cs:15-20`, `…/Lobby/Artifact/Jobs/ArtifactCoreCallJob.cs:38-39`, `…/Lobby/Artifact/Jobs/LobbyArtifactCheckJob.cs:38`, `…/Lobby/Plans/LobbyPlan.cs:56-67,103-108` (`…` = `S/RuntimeSystem/LifeCycleSystem/LifeCycles`)
- 내용: 종류 분기가 열거 둘·Plan 두 곳·Job 둘·도메인 하나에 있다.
- 실패 시나리오: 새 종류 하나에 여섯 곳을 고친다.
- 고칠 방향: 종류별 정보를 한 표로.
- 검증: 묶음 검증 B 유지.

#### R-N-69 팝업 동안 로비 숨김이 Scope(글자)와 아티팩트 표시(월드 루트) 두 곳
- 위치: `V/Lobby/LobbyViewScope.cs:68,97-105`, `V/ArtifactPopup/ArtifactPopupView.cs:20-21,35-43,79-101`, `V/Lobby/LobbyArtifactPopupFactory.cs:25-29`
- 내용: 무엇을 숨길지가 Scope와 아티팩트 3D 표시로 갈린다. 아티팩트 표시가 로비 루트 개념과 `IsChildOf` 검사를 갖는다.
- 실패 시나리오: 다른 팝업도 월드를 가려야 하면 숨김 코드를 복사하고 복원 순서가 어긋날 수 있다.
- 고칠 방향: 호스트 책임으로 모으고 팝업은 필요 여부만 선언.
- 검증: 묶음 검증 B 유지.

#### R-N-71 팝업이 아닌 시설의 실행 항목·"출발할 수 있나" 판정이 Scope 안 람다
- 위치: `V/Lobby/LobbyViewScope.cs:139-145`
- 내용: `new LobbyFacilityAction(() => true, _departure.Submit)`로 출발 조건 주인이 수명 객체의 상수 람다다.
- 실패 시나리오: 출발 조건 추가·비팝업 시설 추가가 Scope를 고치게 한다.
- 고칠 방향: `LobbyDepartureFlow`가 항목을 직접 구현.
- 검증: 묶음 검증 B 유지.

#### R-N-74 출력 마스크는 마스크만 바꾸고 되돌릴 때 렌더 상태 전체를 덮음
- 위치: `V/ArtifactPopup/ArtifactPopupOutputMask.cs:52-54,78`
- 내용: `Capture`로 상태 전체를 잡고 마스크만 바꾼 뒤 `Restore(before)`로 Enabled·RenderOrder·TargetTexture까지 되돌린다.
- 실패 시나리오: 열린 동안 다른 기능이 바꾼 값이 조용히 사라진다(다른 대여자 미확인).
- 고칠 방향: 마스크만 되돌린다.
- 검증: 묶음 검증 B 유지.

#### R-N-75 아티팩트 판 크기와 고정 바닥 아트의 짝을 확인하지 않음
- 위치: `V/ArtifactPopup/ArtifactPopupView.cs:111-117`, `S/DataBaseSystem/Data/View/Table/Artifact/Item/ArtifactPopupConfigDataSO.cs:23`
- 내용: 바닥은 4×4 고정 아트, 칸은 `board.Width/Height`를 따른다. 짝 검사가 없다.
- 실패 시나리오: 판이 5×5가 되면 바닥이 4×4로 남아 경고 없이 어긋난다(확장 경로 미확인).
- 고칠 방향: 맞는 크기를 설정에 두고 다르면 예외.
- 검증: 묶음 검증 B 유지.

### C5 불필요·우회 (13)

#### R-N-07 시설 Picker가 "검사 끝남" 주석과 달리 null을 다시 보고 조용히 건너뜀
- 위치: `V/Lobby/LobbyFacilityPicker.cs:22-25,30,39,42`
- 내용: 생성자·Tick에서 컬라이더 null을 다시 본다.
- 실패 시나리오: 누락 시설을 조용히 못 고른다.
- 고칠 방향: 생성자에서 누락이면 예외, 반복 검사는 파괴 감지만.
- 검증: 묶음 검증 A 유지.

#### R-N-15 같은 프리팹 풀 두 벌, 첫 그리기 때 모든 드래그 모델을 미리 만듦, 칸 이미지 풀은 반환 없음
- 위치: `V/ArtifactPopup/ArtifactDragDisplay.cs:19,21,43-69,135-145`, `V/ArtifactPopup/ArtifactPopupView.cs:25,130-134,143-155`
- 내용: 배치·드래그 모델이 같은 프리팹에 풀을 따로 둔다. Render가 모든 종류 `Prepare`로 드래그 모델을 미리 만든다. `_cellPool`은 Get만 한다.
- 실패 시나리오: 끌지 않아도 종류마다 모델이 생긴다.
- 고칠 방향: 드래그 모델은 끌기 때 빌리거나 풀을 공유, 칸 이미지는 리스트로.
- 검증: 묶음 검증 A 유지. 줄 정정: View 쪽 풀은 :143-155.

#### R-N-18 정리·기록 호출이 같은 경로에서 겹침(Dispose/HideDisplay, RecordCamera 두 번)
- 위치: `V/ArtifactPopup/ArtifactPopupController.cs:180-199`, `V/ArtifactPopup/ArtifactPointerMapper.cs:62-68,82-88`
- 내용: `Dispose`가 출력·상태·차단을 한 뒤 `HideDisplay`가 같은 셋을 한다. `TryMapCandidate`와 `MapCell`이 카메라 기록을 두 번 한다.
- 실패 시나리오: 멱등 중복이라 정리 순서를 바꿀 때 어느 쪽이 원본인지 헷갈린다.
- 고칠 방향: 한 곳씩만.
- 검증: 묶음 검증 A 유지.

#### R-N-19 Factory 생성 실패 정리 갈래가 사소한 한 줄만 감쌈
- 위치: `V/ArtifactPopup/ArtifactPopupViewFactory.cs:53-63`
- 내용: try/catch가 `SetCanClose(false)`만 감싼다.
- 실패 시나리오: 실제 생성 실패는 덮지 않아 정리 갈래가 의미 없다.
- 고칠 방향: 걷거나 실제 생성 구간을 감싼다.
- 검증: 묶음 검증 A 유지.

#### R-N-20 출력 마스크 층 이름을 DB와 View가 두 번 검사
- 위치: `S/DataBaseSystem/Data/View/Table/Artifact/Item/ArtifactPopupConfigData.cs:40-41`, `V/ArtifactPopup/ArtifactPopupOutputMask.cs:31-35`
- 내용: "DB 생성 때 검사가 끝났다" 주석 바로 아래에서 `NameToLayer < 0`을 다시 본다.
- 실패 시나리오: 규칙을 바꾸면 두 판정이 어긋날 수 있다.
- 고칠 방향: DB가 층 번호를 구해 내고 마스크는 그 값을 쓴다.
- 검증: 묶음 검증 A 유지.

#### R-N-21 쓰지 않는 멤버·오버로드·재진입 토큰·늘 넘기는 선택 인자
- 위치: `S/RuntimeSystem/BufferSystems/LobbyView/LobbyDepartureRequest.cs:21,44-45`, `V/Common/ChessPieceMotion.cs:63,95,111-112,143-151`, `V/ArtifactPopup/ArtifactPopupView.cs:33`, `V/ArtifactPopup/ArtifactPopupBoard.cs:17-18`
- 내용: `IsFaulted`·`Begin(Vector3)`·`WorldRoot`는 사용처가 없다. `_beginToken`은 콜백 없는 계산 실패에서만 본다. `ArtifactPopupItem`의 `modelPrefab`·`rotation`은 유일한 호출이 늘 넘기는데 선택 인자다.
- 실패 시나리오: 새 호출이 인자를 빠뜨리면 R0·모델 없음으로 조용히 그린다.
- 고칠 방향: 걷고 필수로.
- 검증: 묶음 검증 A 유지.

#### R-N-26 팝업 입력 Layer의 null 갈래가 죽은 경로
- 위치: `V/Common/PopupInputSession.cs:31,44-51,152`, `V/Common/PopupViewScope.cs:15,27`
- 내용: 호출 셋 모두 유효한 Layer를 넘기는데 `InputLayer?` + `ClearLayer` 갈래·`?? default`가 남았다.
- 실패 시나리오: 수명 조합만 늘어 검증 범위가 커진다.
- 고칠 방향: 필수 `InputLayer`로.
- 검증: 묶음 검증 A 유지.

#### R-N-35 덱 화면 재진입 방어가 과하고 Presenter와 겹침
- 위치: `S/RuntimeSystem/GlobalSystem/UISystem/Screens/Lobby/LobbyDeckUI.cs:30-34,62-80,116-169,191-217,277-293`, `V/Lobby/Deck/LobbyDeckPresenter.cs:26-39`
- 내용: 단계마다 버전 재검사, 닫기 연결 반복 호출, 상세 비우기 두 번.
- 실패 시나리오: 실제 재진입 지점이 묻혀 고칠 때 범위가 넓다.
- 고칠 방향: 실제 재진입 지점만 막고 닫기 연결은 Awake 한 곳에.
- 검증: 묶음 검증 A 유지.

#### R-N-36 UIInputBinding이 본 상호작용 가능 여부를 화면이 다시 검사(화면마다 다름)
- 위치: `S/RuntimeSystem/GlobalSystem/UISystem/Screens/Lobby/LobbyDeckListItemView.cs:20`, `…/Screens/Lobby/LobbyDeckUI.cs:248,254,264-265`, `…/Screens/Stage/ArtifactPopupUI.cs:139` (`…` = `S/RuntimeSystem/GlobalSystem/UISystem`)
- 내용: 같은 원본을 두 번 읽고 어느 슬롯에서 다시 볼지가 화면마다 다르다.
- 실패 시나리오: 판정 규칙이 화면마다 갈린다.
- 고칠 방향: 바인딩 판정을 원본으로.
- 검증: 묶음 검증 A 유지.

#### R-N-37 씬 연결 검사로 보장된 필수 참조를 실행 경로마다 null로 다시 보고 건너뜀
- 위치: `S/RuntimeSystem/GlobalSystem/UISystem/Screens/Lobby/LobbyDeckUI.cs:174-175,269,279,298`, `…/Screens/Lobby/LobbyDeckListItemView.cs:20,34,38,46-47`, `…/Screens/Stage/ArtifactPopupUI.cs:56,69,96,188,212-215,296`, `V/ArtifactPopup/ArtifactPopupView.cs:98` (`…` = `S/RuntimeSystem/GlobalSystem/UISystem`)
- 내용: `Validate`가 필수로 막는 참조를 `!= null`로 다시 거른다. `ArtifactPopupUI.Awake`는 `closeButton`을 검사 없이 쓰고 다음 줄은 검사한다.
- 실패 시나리오: 검사 순서가 바뀌면 누락이 "표시 안 됨"으로 묻힌다.
- 고칠 방향: 검사 뒤 경로의 null 갈래를 걷는다.
- 검증: 묶음 검증 A 유지.

#### R-N-46 읽는 곳 없는 아티팩트 외형 `Icon`과 `TryGetArtifactView`
- 위치: `S/DataBaseSystem/Data/View/Table/Artifact/Item/ArtifactViewData.cs:16,23-24`, `ArtifactViewDataSO.cs:33-34`, `S/DataBaseSystem/Queries/ViewDataBase/ViewTable/ArtifactViewTable.cs:14-16,69-74`
- 내용: 아이콘을 넣어도 화면에 안 나오고 검사도 없다. 조회 입구 하나는 호출이 없다.
- 실패 시나리오: 기획자가 아이콘을 넣고 반영을 기대한다.
- 고칠 방향: 칸을 걷거나 "미사용" 툴팁, `TryGet`은 걷는다.
- 검증: 묶음 검증 A 유지.

#### R-N-54 같은 입력 검사가 생성 함수·Buffer 입구·Job·도메인에 겹치고 요청 종류 검사가 두 식
- 위치: `S/RuntimeSystem/BufferSystems/ArtifactView/ArtifactViewCommand.cs:29-67`, `ArtifactViewPreview.cs:20-30`, `ArtifactViewBuffer.cs:65,91`, `S/RuntimeSystem/LifeCycleSystem/LifeCycles/Lobby/Artifact/Jobs/ArtifactCoreCallJob.cs:24-26`, `S/RuntimeSystem/LifeCycleSystem/LifeCycles/Lobby/Artifact/LobbyArtifactDomain.cs:99-102`
- 내용: ID·회전 검사가 최대 세 번이다. 요청 종류는 Job이 `Enum.IsDefined`·`<= 0`, 도메인이 두 값 비교·`< 1`로 본다.
- 실패 시나리오: 종류에 값을 더하면 입구마다 판정이 갈린다.
- 고칠 방향: Buffer 입구 Validate 하나와 Job 생성 입구 하나로.
- 검증: 묶음 검증 B 유지.

#### R-N-87 보드가 바뀔 때마다 외형 캐시를 버리고 모델·보유 목록 전체를 다시 만듦
- 위치: `V/ArtifactPopup/ArtifactBoardRefresher.cs:43,51-62`, `V/ArtifactPopup/ArtifactPopupView.cs:104-141`, `S/RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/ArtifactPopupUI.cs:227-269`
- 내용: 바뀌지 않는 View DB 외형 캐시를 `Clear()`하고 즉시 다시 채운다.
- 실패 시나리오: 편집마다 불필요한 재구성 스파이크.
- 고칠 방향: 캐시 비우기를 걷는다.
- 검증: 묶음 검증 B 유지.

### C6 관례·이름 (7)

#### R-N-23 드래그 미리보기 칸 높이·크기 비율이 코드 상수
- 위치: `V/ArtifactPopup/ArtifactDragDisplay.cs:37-51,99,141`
- 내용: 다른 판 표현 값은 View DB에 있는데 `.03`·`.9`와 월드 Canvas·Image 구성은 코드에 있다.
- 실패 시나리오: 바닥 아트를 바꾸면 코드를 고친다.
- 고칠 방향: 설정 칸으로.
- 검증: 묶음 검증 A 유지.

#### R-N-30 덱 팝업 Layer 이름만 이름 상수 밖 문자열
- 위치: `V/LobbyInput/LobbyInputLayers.cs:13`
- 내용: `"LobbyDeckPopup"` 리터럴.
- 실패 시나리오: 이름을 바꿀 때 상수 목록에서 찾지 못한다.
- 고칠 방향: `LobbyInputNames` 상수로.
- 검증: 묶음 검증 A 유지.

#### R-N-42 로비 전용 아티팩트 UI가 `Screens.Stage` 네임스페이스, 생성 함수 이름이 `FindPopups`
- 위치: `S/RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/ArtifactPopupUI.cs:8-11`(같은 폴더 ArtifactPool*·IArtifactPopup*), `S/Factory/Runtime/InGame/View/LobbySceneFactory.cs:22`
- 내용: 덱 UI는 `Screens.Lobby`인데 아티팩트는 `Screens.Stage`다. `FindPopups`는 실제로 새로 만든다.
- 실패 시나리오: 위치·이름이 사용처와 맞지 않아 찾기 어렵다.
- 고칠 방향: 옮기고(`MovedFrom`, GUID 유지) `CreatePopups`로.
- 검증: 묶음 검증 A 유지.

#### R-N-58 진행 중 단계에 "Succeeded" 이름, 전환 Job의 이중 부정
- 위치: `S/RuntimeSystem/BufferSystems/LobbyView/LobbyDepartureRequest.cs:53-58`, `S/RuntimeSystem/LifeCycleSystem/LifeCycles/Lobby/Jobs/LobbyTransitionJob.cs:33-46`
- 내용: `IsTransitionSucceeded`는 Change 전·중 단계다. Job은 `!CanResubmit` 안 `!IsPrepared` 이중 부정이다.
- 실패 시나리오: 이름을 믿고 완료로 판단하는 실수.
- 고칠 방향: 이름·분기 순서를 고친다.
- 검증: 묶음 검증 B 유지.

#### R-N-78 로비 Plan이 조립 의존 인자를 갈래마다 늦게 검사
- 위치: `S/RuntimeSystem/LifeCycleSystem/LifeCycles/Lobby/Plans/LobbyPlan.cs:26-40,46-47,70-71`
- 내용: `lifeCycles`·`buffers` null이 첫 출발 때에야 드러난다.
- 실패 시나리오: 조립 실수가 진입이 아니라 출발 순간에 터진다.
- 고칠 방향: `Register` 첫머리에서 함께 검사.
- 검증: 묶음 검증 B 유지(출처 C3 → C6, 인자 검사 시점 관례).

#### R-N-90 예외 형식·문구 관례 불일치
- 위치: `S/RuntimeSystem/BufferSystems/ArtifactView/ArtifactViewCommand.cs:65,73`, `ArtifactViewPreview.cs:28`, `S/RuntimeSystem/BufferSystems/LobbyView/LobbyDepartureRequest.cs:87-88,132`, `LobbyViewBuffer.cs:41`, `V/Common/PopupInputSession.cs:22`, `S/RuntimeSystem/GlobalSystem/UISystem/Screens/Lobby/LobbyDeckUI.cs:327`, `LobbyDeckDisplayModel.cs:17`
- 내용: 인자 null에 `InvalidOperationException`, 값 검사에 매개변수 없는 `ArgumentException`, `[타입]` 머리 없는 문구.
- 실패 시나리오: 오류 원인을 찾기 어렵고 관례가 퍼진다.
- 고칠 방향: 규칙대로 맞춘다.
- 검증: 묶음 검증 B 유지.

#### R-N-91 `IDisposable`이 아닌 `Dispose` 메서드
- 위치: `V/Lobby/LobbyViewInput.cs:64`, `V/Lobby/LobbyViewScene.cs:96`, `V/Lobby/LobbyViewPopups.cs:85`
- 내용: 같은 폴더 다른 클래스는 구현하는데 셋은 이름만 `Dispose`다.
- 실패 시나리오: `using`·일괄 해제 목록에 넣을 수 없어 해제 규칙이 갈린다.
- 고칠 방향: 구현하거나 이름을 바꾼다.
- 검증: 묶음 검증 B 유지.

### C7 Tick·호출 빈도 (4)

#### R-N-11 아티팩트 호버가 막힌 동안 포인터마다 취소·상세 지우기를 되풀이
- 위치: `V/ArtifactPopup/ArtifactDragController.cs:69-80,120-128,201-212`
- 내용: 끌지 않는데 `CanHover`가 거짓이면 `PointerMoved`가 `CanHover`를 두 번 계산하고 `SelectCanceled`로 빈 상세를 지운다. `_hoverSnapshot = null`이라 다음 Tick에 한 번 더 지운다.
- 실패 시나리오: 같은 빈 값 쓰기 반복(비용 작음).
- 고칠 방향: 이미 비었는지 기억해 한 번만, 끌기 취소는 끌 때만.
- 검증: 묶음 검증 A 유지.

#### R-N-53 아티팩트 정지 한 값을 위해 로비 사실 전체를 매 프레임·포인터마다 읽음
- 위치: `V/Lobby/LobbyViewServices.cs:52-54`, `V/ArtifactPopup/ArtifactPopupController.cs:140,207-208`, `V/ArtifactPopup/ArtifactDragController.cs:57-58,84,120,125`, `S/RuntimeSystem/LifeCycleSystem/LifeCycles/Lobby/LobbyProgress.cs:15-19,53-66`
- 내용: `() => progress.Read().IsArtifactStopped`가 열린 동안 매 Tick·포인터마다 2~4회 불려 사실 전체를 읽는다(할당 없음, 비용 미측정).
- 실패 시나리오: 사실 항목이 늘면 팝업 Tick도 영향을 받는다.
- 고칠 방향: 정지만 답하는 좁은 읽기 계약, 또는 Tick에서 한 번 읽어 넘긴다.
- 검증: 묶음 검증 B 유지.

#### R-N-86 로비 호버가 시설 판정 전에 EventSystem 전체 Raycast를 먼저 돌림
- 위치: `V/Lobby/LobbyInteraction.cs:62-63,71-72,163-164`, `V/Common/ScreenUIPointerBlocker.cs:21-53`
- 내용: 포인터가 움직인 프레임마다 시설이 없어도 `RaycastAll`을 한다(비용 미측정).
- 실패 시나리오: 불필요한 Raycast 비용.
- 고칠 방향: 싼 `TryPick`을 먼저, 잡혔을 때만 가림 확인.
- 검증: 묶음 검증 B 유지.

#### R-N-88 끄는 동안 후보 칸이 바뀔 때마다 미리보기 Canvas·모델을 껐다 켬
- 위치: `V/ArtifactPopup/ArtifactDragController.cs:258-265,287-305`, `V/ArtifactPopup/ArtifactDragDisplay.cs:83-119,147-161`
- 내용: 칸 경계마다 월드 Canvas 활성 토글.
- 실패 시나리오: 짧은 깜빡임과 Canvas 재구성 비용.
- 고칠 방향: Canvas는 켜 둔 채 이미지만 숨긴다.
- 검증: 묶음 검증 B 유지.

### C8 캡슐화 (5)

#### R-M-04 출발 요청 구체 타입이 로비 사실 묶음을 통해 View까지 노출됨
- 위치: `S/RuntimeSystem/LifeCycleSystem/LifeCycles/Lobby/LobbyProgressFacts.cs:13,21,47`, `…/Lobby/LobbyProgress.cs:15-19`, `S/RuntimeSystem/BufferSystems/LobbyView/LobbyViewBuffer.cs:8-16`, `…/LobbyView/LobbyDepartureRequest.cs:25,68-107`, `…/LobbyView/ILobbyDepartureState.cs:6-7`, `V/Lobby/LobbyViewServices.cs:39,54`
- 내용: View 출발 창구는 읽기 계약으로 좁혔지만 View 묶음이 `ILobbyProgressReader` 전체를 받고 `Read().Departure`는 구체 `LobbyDepartureRequest`다. 변경자는 internal이고 같은 어셈블리다.
- 실패 시나리오: View 쪽에 단계 변경·`Status` 의존이 들어와도 컴파일이 막지 않는다. 지금 View는 람다 하나로 정지 여부만 읽는다.
- 고칠 방향: View 묶음에는 `Func<bool>` 정지 여부만 넘기고 사실 묶음은 읽기 계약만 담는다.
- 검증: 증거 낮음 / 반론 낮음. 출처 중간 → 낮음. 지금 오용 없음, 일부러 internal 변경자를 불러야 닿는 경계 표현 차이다. 읽기 계약도 캐스트하면 같은 노출이라 고유 위험이 작다.

#### R-N-06 쓰는 곳보다 넓은 접근 수준과 두 벌 표면(덱 컨트롤러 internal + 명시 구현 등)
- 위치: `V/Lobby/Deck/LobbyDeckPopupController.cs:40-52`, `V/Lobby/LobbyViewScope.cs:53-54`, `V/ArtifactPopup/ArtifactInteraction.cs:13,16`
- 내용: 덱 컨트롤러가 `internal CanOpen/Start/Open/Tick`과 명시 인터페이스 구현을 함께 둔다. `IsDisposed/IsSuperseded`·`Anchor/HasCandidate`는 클래스 밖 사용이 없는데 internal이다.
- 실패 시나리오: 입구가 두 길이 되어 정식 입구를 따로 확인한다.
- 고칠 방향: private·인터페이스 한 벌로.
- 검증: 묶음 검증 A 유지.

#### R-N-14 공개 MonoBehaviour가 Presenter 메서드를 public으로 내놓아 호스트 감싸개 우회 가능
- 위치: `V/ArtifactPopup/ArtifactPopupView.cs:17,79,89,156` (대비 :57-65,103)
- 내용: `Show/Hide/ClearBoard`가 public이다.
- 실패 시나리오: 씬 참조로 직접 부르면 "현재 호스트만" 보호를 거치지 않는다.
- 고칠 방향: 명시 구현으로 맞춘다.
- 검증: 묶음 검증 A 유지.

#### R-N-51 `LobbyFacilityKinds.All`이 바꿀 수 있는 공유 배열을 그대로 내놓음
- 위치: `S/DataBaseSystem/Data/View/Table/LobbyVisual/Item/LobbyFacilityKinds.cs:9`
- 내용: `internal static readonly LobbyFacilityKind[] All`.
- 실패 시나리오: 원소를 덮어쓰면 DB 검사·배치판·실행 표가 다른 목록을 본다(지금 쓰기 없음).
- 고칠 방향: `IReadOnlyList`로.
- 검증: 묶음 검증 B 유지.

#### R-N-89 바깥이 부품 내부를 꺼내 단계를 조합(보드 변환기·씬 루트)
- 위치: `V/ArtifactPopup/ArtifactPopupViewFactory.cs:48-51`, `V/ArtifactPopup/ArtifactPopupView.cs:35-53`, `V/Lobby/LobbyViewScope.cs:46,72`, `V/Lobby/LobbyViewScene.cs:27-28`
- 내용: Factory가 "Configure 뒤에만" 되는 `BoardMapper`를 꺼내고, Scope는 씬 MonoBehaviour 루트를 통째로 꺼내 넘긴다.
- 실패 시나리오: 순서를 어기면 실행 때만 드러난다.
- 고칠 방향: Configure가 결과 객체를 돌려주고 씬은 필요한 값만 좁게.
- 검증: 묶음 검증 B 유지.

### C9 수명·자원 (3)

#### R-N-09 끝난 연결을 다음 로비 연결 전까지 "현재"로 붙잡아 둠
- 위치: `V/Lobby/LobbyViewSystem.cs:41-43,124-136,163-171`, `V/Lobby/LobbyViewScope.cs:53-56`
- 내용: `_lastPublishedScope`가 Detach 뒤에도 남고 `IsCurrent`는 `!IsSuperseded`뿐이다.
- 실패 시나리오: 폐기된 Scope의 씬 객체·팝업 그래프가 스테이지·전투 내내 남고 공유 Layer·Buffer 쓰기 권한을 유지한다(늦은 쓰기 경로는 못 찾음, 누적 없음).
- 고칠 방향: 미룬 반환이 끝난 뒤 비우고 쓰기 권한은 `!IsDisposed`로 나눈다.
- 검증: 묶음 검증 A 유지.

#### R-N-61 Core 호출 Job이 예외 뒤 다시 실행되면 조용히 아무것도 안 함
- 위치: `S/RuntimeSystem/LifeCycleSystem/LifeCycles/Lobby/Artifact/Jobs/ArtifactCoreCallJob.cs:32-41`
- 내용: `_started`를 Core 호출 전에 세운다.
- 실패 시나리오: Core가 던지면 Runner가 같은 Job을 다음 Tick에 다시 불러 영원히 미완료로 점유할 수 있다(F1으로 멈추는지 미확인).
- 고칠 방향: 도메인을 멈추거나 F1 위임을 계약에 적는다.
- 검증: 묶음 검증 B 유지.

#### R-N-81 열림 알림 안에서 폐기되면 팝업 입력 수명(Layer)을 돌려주지 않음
- 위치: `V/Common/PopupViewScope.cs:141-155,232-270,326-333`
- 내용: `OnOpenStateChanged` 끝에 미룬 반환이 없다. 알림 안 Dispose는 `IsUiBusy`라 `EndDisposedInput`을 건너뛴다.
- 실패 시나리오: 알림 안 F1 → Detach → Dispose 경로에서 팝업 Layer가 남는다(오류 정지 뒤).
- 고칠 방향: finally에서 `EndDisposedInput()`.
- 검증: 묶음 검증 B 유지.

### C10 정확성 (12)

#### R-M-02 꺼짐 훅(OnDisable) 경로의 Hide가 관찰자 예외를 AggregateException으로 던짐
- 위치: `S/RuntimeSystem/GlobalSystem/UISystem/Screens/Lobby/LobbyDeckUI.cs:60,106-107,307-327`, `…/Screens/Stage/ArtifactPopupUI.cs:62,86-89,100-118` (`…` = `S/RuntimeSystem/GlobalSystem/UISystem`)
- 내용: `OnUIHidden() => Hide()`가 `UIBase.OnDisable → NotifyHidden` 안에서 돌고 `Hide`는 관찰자 실패를 모아 던진다.
- 실패 시나리오: Unity 메시지 안이라 예외가 콘솔 로그로만 끝난다. 지금 운영 구독자는 예외를 Report로 끝내 닿지 않는다.
- 고칠 방향: 꺼짐 경로의 관찰자 예외를 오류 경계로 넘기거나, 정리를 소유 Scope의 해제 경로에서 한다.
- 검증: 증거 낮음 / 반론 낮음. 출처 중간 → 낮음. 운영 구독자는 `PopupViewScope.OnOpenStateChanged` 하나이고 catch → `_faults.Report`로 끝나 다시 던지지 않는 잠재 경로.

#### R-N-01 로비 덱 팝업이 늘 빈 모델을 보여 카드 호버·고정 갈래가 운영에서 닿지 않음
- 위치: `V/Lobby/Deck/LobbyDeckPopupController.cs:58-65`, `V/Lobby/Deck/LobbyDeckPresenter.cs:64-107`
- 내용: `Show`가 늘 빈 카드 배열 모델을 넘긴다.
- 실패 시나리오: 덱 시설을 열면 빈 화면이고 카드 갈래는 검증되지 않은 채 남는다.
- 고칠 방향: 덱 읽기 계약을 연결하거나 연결 전까지 시설을 막고 안내.
- 검증: 묶음 검증 A 유지(주석으로 밝힌 미완성).

#### R-N-13 ArtifactPopupView가 검증 전에 상태를 바꿈
- 위치: `V/ArtifactPopup/ArtifactPopupView.cs:40-43,81-82`
- 내용: `Show`는 SetActive 뒤 Configure 여부를, `Configure`는 대입 뒤 null·`IsChildOf`를 검사한다.
- 실패 시나리오: 예외 뒤에도 바뀐 상태가 남는다(잘못된 구성일 때만).
- 고칠 방향: 검사를 앞으로.
- 검증: 묶음 검증 A 유지.

#### R-N-22 닫기 실패를 호스트 교체 중이면 로그만 남기고 삼킴
- 위치: `V/ArtifactPopup/ArtifactPopupController.cs:119-127`
- 내용: `!_host.IsCurrent`면 `LogException` 후 return한다.
- 실패 시나리오: `_scope.Close()` 예외가 교체 타이밍과 겹치면 F1로 가지 않는다(좁은 타이밍).
- 고칠 방향: 이 연결 소유 자원 실패는 Report하거나 무시할 예외 종류를 좁힌다.
- 검증: 묶음 검증 A 유지.

#### R-N-31 새 이동 시작이 소비되지 않은 도착 완료를 조용히 버림
- 위치: `V/Common/ChessPieceMotion.cs:110,211-216`, `V/Lobby/LobbyPieceMover.cs:28,40`
- 내용: `BeginCore`가 늘 `Cancel()`로 완료 토큰을 지운다.
- 실패 시나리오: 도착과 새 선택이 겹치면 첫 시설이 실행되지 않을 수 있다(겹침 발생 미확인).
- 고칠 방향: 미소비 완료가 있으면 거부하거나 계약을 문서화.
- 검증: 묶음 검증 A 유지(미확인).

#### R-N-43 닫는 도중의 Open을 조용히 버려 UISystem 상태와 어긋날 수 있음
- 위치: `S/RuntimeSystem/GlobalSystem/UISystem/Screens/Lobby/LobbyDeckUI.cs:62-65,82-112`
- 내용: `_hideInProgress`이면 알림 없이 반환한다.
- 실패 시나리오: UISystem은 열었다고 기록하는데 패널은 닫힌 채다(그런 호출자 미확인).
- 고칠 방향: 예외로 막거나 미룬다.
- 검증: 묶음 검증 A 유지(미확인).

#### R-N-52 시작 칸과 시설 도착 칸이 같아도 검사하지 않음
- 위치: `S/DataBaseSystem/Data/View/Table/LobbyVisual/Item/LobbyBoardData.cs:72-86`, `V/Lobby/LobbyViewScene.cs:75`, `V/Common/ChessPieceMotion.cs:238-239,265-275`, `V/Lobby/LobbyViewHopPath.cs:63-64,128-131`
- 내용: 도착 칸끼리만 막는다.
- 실패 시나리오: 같으면 시작 정착 위치와 도착 위치가 정확히 같지 않을 때 `LobbyViewHopPath.Plan`이 "같은 칸인데 위치가 다름" 예외로 F1(왕복 오차 미확인, 기본 에셋은 겹치지 않음).
- 고칠 방향: DB 검사에서 막거나 계산한 위치를 쓴다.
- 검증: 묶음 검증 B 유지. 줄 정정: 예외 자리 `LobbyViewHopPath.cs:63-64`.

#### R-N-80 덱·아티팩트 팝업이 입력 세션 하나와 막힘 bool 하나를 공유해 두 번째 팝업 실패가 첫 팝업 막힘을 풂
- 위치: `V/Lobby/LobbyViewScope.cs:70-72,97-105`, `V/Common/PopupInputSession.cs:33-36`, `V/Common/PopupViewScope.cs:157-205,287-298`
- 내용: 외부에서 두 번째 팝업이 열리면 `Begin` 예외 → `AbortOpen`이 막힘을 풀고 글자를 켠다.
- 실패 시나리오: 첫 팝업은 열린 상태로 F1(외부 열림 경로 미확인).
- 고칠 방향: 소유 중이면 Begin 전에 거절하거나 막힘을 세는 수로.
- 검증: 묶음 검증 B 유지.

#### R-N-82 로비 출발이 아닌 씬 이탈이 한두 프레임 창에 걸리면 예외로 멈춤
- 위치: `S/RuntimeSystem/LifeCycleSystem/LifeCycles/Lobby/LobbyLifeCycle.cs:94-103`, `…/Lobby/Plans/LobbyPlan.cs:129-138`
- 내용: 처리 안 된 편집·진행 중 출발이 있으면 이탈 노드가 예외를 던진다.
- 실패 시나리오: 다른 이탈 경로가 그 창에 걸리면 F1(다른 이탈 경로 존재 미확인).
- 고칠 방향: 미루거나 이탈 쪽이 `CanLeave`를 먼저 묻게.
- 검증: 묶음 검증 B 유지.

#### R-N-83 아티팩트 보유 슬롯 입력은 오류 경계 감싸개를 거치지 않음
- 위치: `S/RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/ArtifactPopupUI.cs:54-57,134-156,242`
- 내용: 슬롯 클릭·호버 수신자가 화면 자신이다.
- 실패 시나리오: 예외가 Unity 로그로만 간다(지금 본문은 단순).
- 고칠 방향: 덱처럼 감싼 수신자로.
- 검증: 묶음 검증 B 유지.

#### R-N-84 숨긴 동안 도착한 배치 실패 문구가 다음 열기 때 낡은 채 보임
- 위치: `V/ArtifactPopup/ArtifactPopupStatus.cs:45-64,80-88`, `V/ArtifactPopup/ArtifactPopupController.cs:131-139,148-170`
- 내용: `Suspend` 뒤에도 `Tick(false)`가 임시 실패 문구를 다시 저장하고 `ResetDisplay`가 지우지 않는다.
- 실패 시나리오: 다시 열면 낡은 실패 문구가 보인다(표시만, 창 짧음).
- 고칠 방향: 숨김 중 저장하지 않거나 열 때 비운다.
- 검증: 묶음 검증 B 유지.

#### R-N-85 팝업이 숨기는 로비 루트 아래에 팝업 UI가 있어도 막지 않음
- 위치: `V/ArtifactPopup/ArtifactPopupView.cs:35-50,79-88`, `V/ArtifactPopup/ArtifactSceneRoot.cs:27-36`, `V/Lobby/LobbySceneRoot.cs:23-37`
- 내용: 3D 표시만 루트 밖인지 보고 UI·씬 루트는 보지 않는다.
- 실패 시나리오: 배치 실수 때 원인 불명 예외나 즉시 닫힘(씬 배치 미확인).
- 고칠 방향: 연결 검사에 더한다.
- 검증: 묶음 검증 B 유지.

### C11 할당 (1)

#### R-N-60 프리뷰 번호마다 목록·Job 두 개를 새로 할당
- 위치: `S/RuntimeSystem/LifeCycleSystem/LifeCycles/Lobby/Plans/LobbyPlan.cs:62-67`, `…/Lobby/Artifact/LobbyArtifactDomain.cs:104`, `…/Lobby/Artifact/Plans/ArtifactPlan.cs:19-20` (`…` = `S/RuntimeSystem/LifeCycleSystem/LifeCycles`)
- 내용: 끌기 중 후보 칸이 바뀔 때마다 객체 셋을 할당한다.
- 실패 시나리오: 끌기 동안 GC 압력.
- 고칠 방향: 재사용하거나 빈도로 충분함을 기록.
- 검증: 묶음 검증 B 유지.

### C12 주석·문서 (9)

#### R-N-08 가상 카메라 검사 문구가 검사하지 않는 "직교"를 적음
- 위치: `V/Lobby/LobbySceneRoot.cs:29-30`
- 내용: 조건은 Follow/LookAt만 보는데 문구는 직교 카메라여야 한다고 한다.
- 실패 시나리오: 문구를 믿고 원인을 잘못 찾는다.
- 고칠 방향: 직교 검사를 더하거나 문구를 고친다.
- 검증: 묶음 검증 A 유지.

#### R-N-12 ArtifactSceneRoot 주석이 실제 찾기 옵션과 반대
- 위치: `V/ArtifactPopup/ArtifactSceneRoot.cs:11`, `V/ArtifactPopup/ArtifactPopupViewFactory.cs:29-30`
- 내용: "비활성은 찾지 못한다"지만 `FindObjectsInactive.Include`로 찾는다.
- 실패 시나리오: 주석을 믿고 씬 배치를 바꾼다.
- 고칠 방향: 주석을 고친다.
- 검증: 묶음 검증 A 유지.

#### R-N-24 FaultReportingUIInputReceiver 주석이 쓰지 않는 사용처(아티팩트 닫기)를 적음
- 위치: `V/Common/FaultReportingUIInputReceiver.cs:9`
- 내용: 사용처는 덱 하나이고 아티팩트 닫기는 자체 try/catch다.
- 실패 시나리오: 감싸개를 고칠 때 아티팩트도 바뀐다고 오해한다.
- 고칠 방향: 주석을 맞추거나 아티팩트 닫기도 이 감싸개를 쓴다.
- 검증: 묶음 검증 A 유지.

#### R-N-25 ViewCameraHandleLease가 로비도 쓴다고 적혀 있으나 스테이지 맵 전용
- 위치: `V/Common/ViewCameraHandleLease.cs:6-7`
- 내용: 사용처는 `StageMapCameraDrag` 하나다.
- 실패 시나리오: 로비 변경 때 이 부품을 함께 확인하게 만든다.
- 고칠 방향: 주석에서 로비를 빼고 배치를 검토.
- 검증: 묶음 검증 A 유지.

#### R-N-29 IPopupViewHost.SetPopupBlocked 주석이 해제만 설명
- 위치: `V/Common/IPopupViewHost.cs:8-9`
- 내용: `true`로 차단하는 데도 쓰인다.
- 실패 시나리오: 매개변수 뜻을 잘못 읽는다.
- 고칠 방향: 매개변수 뜻을 적는다.
- 검증: 묶음 검증 A 유지.

#### R-N-44 `cellScale`이 실제로는 바닥 모델 배율에만 쓰임
- 위치: `S/DataBaseSystem/Data/View/Table/Artifact/Item/ArtifactPopupConfigDataSO.cs:20`, `ArtifactPopupConfigData.cs:14,24`, `V/ArtifactPopup/ArtifactPopupView.cs:116`
- 내용: 이름과 쓰임이 다르다.
- 실패 시나리오: 칸 크기를 키우려고 바꾸면 바닥만 커진다.
- 고칠 방향: `floorScale`로 이름 바꾸고(`FormerlySerializedAs`) 툴팁.
- 검증: 묶음 검증 A 유지.

#### R-N-45 캐릭터 프리팹을 View에서 다시 검사한다는 주석이 코드와 다름
- 위치: `S/DataBaseSystem/Data/View/Table/LobbyVisual/Item/LobbyModelData.cs:20-23,32`, `V/Lobby/LobbyCharacterVisual.cs:22-23`
- 내용: 검사 함수는 private이고 View는 다시 부르지 않는다.
- 실패 시나리오: 주석을 믿고 DB 검사를 줄이면 검사 없이 복제된다.
- 고칠 방향: 주석 둘째 문장을 지운다.
- 검증: 묶음 검증 A 유지.

#### R-N-57 Buffer 읽기 계약·사실 묶음 주석이 실제 소비자·의미와 다름
- 위치: `S/RuntimeSystem/BufferSystems/ArtifactView/ArtifactViewBuffer.cs:11`, `S/RuntimeSystem/LifeCycleSystem/LifeCycles/Lobby/LobbyProgressFacts.cs:9-12`, `S/RuntimeSystem/BufferSystems/LobbyView/LobbyViewBuffer.cs:13-16`
- 내용: 읽기 싱크를 "Core 처리 계층용"이라 하나 LifeCycle·View도 받는다. Facts를 "일시적 사본"이라 하나 `Departure`는 살아 있는 참조다. "View가 단계 변경자를 부를 수 없다"는 R-M-04 경로로 맞지 않는다.
- 실패 시나리오: 주석을 믿고 경계를 잘못 판단한다.
- 고칠 방향: 주석을 맞춘다.
- 검증: 묶음 검증 B 유지.

#### R-N-72 `LobbyFacilityAction` 주석의 사용처 설명이 실제와 다름
- 위치: `V/Lobby/LobbyFacilityAction.cs:5-8`
- 내용: "팝업 컨트롤러·출발 의도처럼 감싼다"지만 팝업은 자신을 항목으로 넣고 이 클래스는 출발 하나에만 쓰인다.
- 실패 시나리오: 새 시설을 잘못된 꼴로 추가한다.
- 고칠 방향: 주석을 맞춘다.
- 검증: 묶음 검증 B 유지.

### C13 UI 규약 (6)

#### R-M-05 아티팩트 팝업 화면 UIId를 UI 밖에서 상수로 다시 적음
- 위치: `V/ArtifactPopup/ArtifactPopupViewScreen.cs:18`, `S/RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/ArtifactPopupUI.cs:47` (대비 `V/Lobby/Deck/LobbyDeckPopupViewScreen.cs:17`)
- 내용: 덱은 `Id => _view.ID`로 UIBase를 원본으로 읽는데, 아티팩트는 `IArtifactPopupLifetime`에 ID가 없어 `UIId.Artifact`를 따로 적는다.
- 실패 시나리오: UI의 ID를 다른 값으로 바꾸거나 다른 UI를 이 수명에 붙이면 `PopupViewScope`가 옛 ID로 `OpenUI`를 불러 "열리지 않았습니다" 예외가 난다.
- 고칠 방향: 계약에 `UIId ID`를 두고 덱과 같은 꼴로 읽는다.
- 검증: 증거 낮음 / 반론 낮음. 출처 중간 → 낮음. 고정 enum 상수 하나이고 어긋나면 첫 열기에서 바로 예외로 드러난다. 드문 변경.

#### R-N-02 고정 해제 직후 포인터가 그 카드 위여도 호버 미리보기가 돌아오지 않음
- 위치: `V/Lobby/Deck/LobbyDeckPresenter.cs:76-81`
- 내용: 고정 카드를 다시 누르면 `_hoveredSlot`까지 지운다.
- 실패 시나리오: 다음 HoverEnter 전까지 상세가 빈다(AGENTS "pin 해제 뒤 호버 재개"와 어긋날 수 있음, 지금 카드가 없어 안 드러남).
- 고칠 방향: 해제 때 호버 슬롯을 유지해 상세를 보이거나 해석을 주석에.
- 검증: 묶음 검증 A 유지.

#### R-N-03 로비 글자 묶음을 GameObject로 내놓고 Scope가 직접 SetActive
- 위치: `V/Lobby/LobbyViewScope.cs:25,46,66-68,104`, `S/RuntimeSystem/GlobalSystem/UISystem/Screens/Lobby/LobbyCanvasView.cs:14,18`
- 내용: 팝업 동안 숨기는 `LobbyTexts`의 활성 변경을 UI가 아니라 View Scope가 한다(UIBase 미등록 근거는 주석에 있음).
- 실패 시나리오: 숨김 방식을 바꾸면 Scope를 고치고, 연결마다 다시 켜는 보정이 따로 필요하다.
- 고칠 방향: 캔버스에 `SetLobbyTextsVisible` 같은 메서드를 두고 GameObject 노출을 걷는다.
- 검증: 묶음 검증 A 유지.

#### R-N-39 아티팩트 UI 가림·보유 슬롯 판정이 손 규칙(하드코딩 영역·사각형 재기·두 방식)
- 위치: `S/RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/ArtifactPopupUI.cs:171,191-216`, `V/ArtifactPopup/ArtifactPointerMapper.cs:34-39,62-67` (대비 `V/Common/ScreenUIPointerBlocker.cs:21-53`)
- 내용: `IsBlockingArea`가 패널 요소 넷만 하드코딩한다. 슬롯 집기는 사각형 기하로, 호버는 EventSystem으로 판정한다. 로비는 `ScreenUIPointerBlocker`를 쓴다.
- 실패 시나리오: 패널에 요소를 더하면 드래그 판정 코드를 같이 고친다. 팝업 밖 화면 UI가 보드 위에 있으면 가림으로 안 볼 수 있다(겹침 미확인).
- 고칠 방향: 공용 Blocker + 호버 슬롯 원본으로.
- 검증: 묶음 검증 A 유지(미확인).

#### R-N-40 보유 슬롯 위젯을 런타임 AddComponent로 메우고 표현 값이 코드에 고정
- 위치: `S/RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/ArtifactPopupUI.cs:22,37-38,121-132,280-286`, `…/Screens/Stage/ArtifactPoolItemView.cs:15,34,37-43,54` (`…` = `S/RuntimeSystem/GlobalSystem/UISystem`)
- 내용: 위젯이 프리팹에 없으면 `AddComponent`로 붙이고 `Validate`는 위젯을 요구하지 않는다. 슬롯·셀 색과 비율이 코드 상수다.
- 실패 시나리오: 구성 누락이 조용히 기본값으로 덮인다(AGENTS ④). 아트 조정에 코드를 고친다.
- 고칠 방향: 위젯을 직렬화·필수로 검사하고 값을 직렬화 칸으로.
- 검증: 묶음 검증 A 유지.

#### R-N-77 OnUIHidden 로컬 숨김이 UISystem 열림·이력과 어긋날 수 있음
- 위치: `S/RuntimeSystem/GlobalSystem/UISystem/Screens/Lobby/LobbyDeckUI.cs:60`, `…/Screens/Stage/ArtifactPopupUI.cs:62` (`…` = `S/RuntimeSystem/GlobalSystem/UISystem`)
- 내용: 꺼짐 훅 경로는 UISystem을 거치지 않아 열림 목록·이력에 ID가 남는다.
- 실패 시나리오: 부모 Canvas만 꺼졌다 켜지면 다음 뒤로가기가 헛소비될 수 있다(현재 구성 미발생 추정).
- 고칠 방향: 외부 숨김 처리 때 `CloseUI`로 맞추거나 근거를 적는다.
- 검증: 묶음 검증 B 유지. 기준 C3 → C13(UI 화면 개폐 규약).

## 3 근본 원인 묶음

- B1 입력 콜백 오류 경계: R-M-01, R-N-24, R-N-83
- B2 UIBase 꺼짐 훅·열림 알림: R-M-02, R-N-33, R-N-77
- B3 View가 로비 사실 리더 전체를 받음: R-M-04, R-N-53, R-N-57
- B4 로비 공통 계약·서비스 묶음이 아티팩트 요구를 품음: R-M-06, R-N-05, R-N-73
- B5 팝업 동안 로비 숨김의 소유: R-N-03, R-N-69
- B6 아티팩트 팝업 부품 책임 집중: R-N-17, R-N-76

묶음마다 문제가 셋 이하이고 모두 낮음이라 기준 안 감점 상한은 이번에 걸리지 않는다.

## 4 버림

- R-N-56 "처리 안 된 편집" 판정 두 벌 — 비문제. View의 `Commands.Count == 0`은 "결과 소비 전엔 새 끌기를 막는다"는 다른 질문이고 실행 계층 규칙의 사본이 아니다.
- R-N-59 로비 활성 동안 Runner가 빌 때마다 판단 — 기반 한계·비문제. Job·Plan 등록 계약의 폴링 방식이고 빈 목록 재사용으로 할당이 없다.
- R-N-68 실패 안내 3초 두 상수 — 비문제. 이름으로 구분된 다른 문구의 값이고 같은 사실이라는 근거가 없다.
- R-N-79 View 표 Query `ExportServices`에 sink null 검사 없음 — 주장과 다름. ViewTable Query 26개 중 검사하는 곳은 둘뿐이라 관례가 아니다.
- D-01 아티팩트 외형 표와 InGame ID 짝 미대조 — 범위 밖·기반 한계. View는 InGame 표를 읽을 수 없고, 누락은 `KeyNotFoundException`으로 바로 드러난다.
- D-02 체스 말 복제본 정책을 로비와 스테이지 맵이 공유 — 비문제. 같은 정책의 원본 하나이고 주석이 공유 범위를 밝힌다.

## 5 1차 평가와 대조

평가가 끝난 뒤 대조 담당 한 명이 1차 [문제 목록](2026-11-05-lobby-view-evaluation-issues-Hojin.md)·[재평가 인계](../handoffs/2026-11-05-lobby-refactor-2-reevaluation-handoff-Hojin.md) §5와 맞대 보았다. 검토자에게는 이 대조를 주지 않았다.

기준: 1차 문제 목록(`fc02777aa`, 94개), 이번 최종 93개(중간 R-M-03 1, 낮음 92 = R-M-01·02·04·05·06 + 유지된 R-N 87, 버림 R-N-56·59·68·79·D-01·D-02), 지금 코드 HEAD `cc3ccdf96`. "고쳐짐"과 "안 잡힘"은 모두 해당 코드를 열어 확인했다. 경로 앞 `V/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/`.

상태 판정 규칙: 1차 문제의 **주된 진술**이 지금 코드에 없으면 "고쳐짐"이다. 주변 일부만 남아 이번에 다시 잡혔으면 "고쳐짐(남은 일부 R-x)"로 적고 고쳐짐으로 센다. 주된 진술이 아직 맞으면(일부 고쳐졌어도) "남음"이다.

### 요약

1차 중간 6개:

| 1차 | 상태 | 근거 |
|---|---|---|
| M-01 입력 콜백 예외가 F1에 안 닿음 | 고쳐짐 | 로비 시설 `FaultReportingLobbyReceiver`(`LobbyViewInput.cs:44`), 덱 `FaultReportingUIInputReceiver`(`LobbyDeckPopupController.cs:35`), 아티팩트 `InputReceiver` 메서드마다 `_faults.Report`(`ArtifactPopupController.cs:238-274`). 후속으로 감싸개 세 벌 사본(R-M-01, 2차 새 코드)과 보유 슬롯 UI 미포장(R-N-83)을 새로 찾음 |
| M-02 덱·아티팩트 열기가 Layer를 안 봄 | 고쳐짐 | 판정 하나 `CanRunFacility() => !IsInteractionBlocked() && _input.IsLobbyLayer`(`LobbyViewScope.cs:133`)를 `LobbyFacilityActions.TryExecute`가 모든 종류에 씀 |
| M-03 외형 없는 아티팩트 ID를 null로 그림 | 고쳐짐(남은 일부 R-M-03) | `LobbyViewServices.cs:53`이 `GetArtifactView`(없으면 예외)를 넘김. 다만 외형 안 `Prefab`은 선택 칸이라(`ArtifactViewData.cs:41`) View의 null 건너뜀 갈래가 남아 R-M-03(이번 유일한 중간)으로 다시 잡힘 |
| M-05 월드 공간 안내 글자가 가림 판정에 걸림 | 고쳐짐 | `ScreenUIPointerBlocker.IsScreenSpace`가 루트 Canvas `renderMode != WorldSpace`만 가림으로 봄(`:44,55-59`) |
| M-09 아티팩트 모델 자세 식 두 벌 | 고쳐짐(남은 일부 R-N-15) | `ArtifactBoardMapper.ApplyModelPose` 하나를 `ArtifactPopupView.cs:126`·`ArtifactDragDisplay.cs:111`이 부름. 같은 프리팹 풀 두 벌은 남아 R-N-15 |
| M-12 lobbyRoot 누락 무시·저작 참조 두 벌 | 고쳐짐 | 직렬화 칸 없앰, `Configure(config, lobbyRoot)`가 null이면 `ArgumentNullException`(`ArtifactPopupView.cs:35-43`), 표시 전 Configure 안 됐으면 예외(`:82`) |

1차 94개 상태별 개수:

| 상태 | 개수 |
|---|---|
| 고쳐짐 | 67 (그중 남은 일부를 이번에 다시 찾은 것 7: M-03·M-09·M-13·M-14·N-18·N-56·N-83) |
| 남음·이번에 다시 찾음 | 25 |
| 남음·이번에 안 잡힘 | 2 (N-31, N-44) |
| 판단 불가 | 0 |

이번 93개:

| 구분 | 개수 |
|---|---|
| 1차 대응 있음 | 36 (그중 R-M-02는 1차에서 버린 N-07과 같은 주장) |
| 새로 찾음 | 57 |
| 그중 2차 리팩토링 새 파일·코드에서 나온 것 | 11 (R-M-01, R-N-04, R-N-05, R-N-10, R-N-24, R-N-31, R-N-51, R-N-55, R-N-69, R-N-71, R-N-72) |

새로 찾은 57개 가운데 2차 수정의 부산물(새 파일 이름 목록 밖)도 몇 있다: R-N-20(N-61로 층 이름을 DB에 옮긴 뒤 이중 검사), R-N-25(N-15 대여 삭제 뒤 낡은 주석), R-N-45(N-19 재검사 삭제 뒤 낡은 주석), R-N-53(M-06 정지 안내가 사실 전체를 읽음).

인계 §5 16줄: 다시 찾음 11, 안 잡힘 5(N-44, N-25 일부, N-31, N-34 로비 부분, N-19 판 유한성).

### 표 1 — 1차 94개

| 1차 | 기준 | 상태 | 이번 대응 | 근거(지금 코드) |
|---|---|---|---|---|
| M-01 | C10 | 고쳐짐 | (후속 R-M-01·R-N-83 새로) | 위 요약 |
| M-02 | C10 | 고쳐짐 | — | 위 요약 |
| M-03 | C10 | 고쳐짐(남은 일부) | R-M-03 | 위 요약 |
| M-05 | C10 | 고쳐짐 | — | 위 요약 |
| M-09 | C1 | 고쳐짐(남은 일부) | R-N-15 | 위 요약 |
| M-12 | C13 | 고쳐짐 | — | 위 요약 |
| M-07 | C1 | 고쳐짐 | — | `LobbyJobPriority.Artifact/Departure`를 `ArtifactCoreCallJob:17`·`LobbyArtifactCheckJob:20`·`LobbyTransitionJob:17`·`LobbyPlan:78`이 함께 읽음 |
| M-08 | C1 | 고쳐짐 | — | `DetachPriority = LobbyLifeCycle.ExitPreparationPriority + 100`(`LobbyViewSystem.cs:30`) |
| M-10 | C1 | 고쳐짐 | (R-N-28은 1차가 문제 아님으로 본 부분) | `Common/PointerSelectInterpreter.cs`·`SelectOwnerSlot.cs`를 로비·아티팩트 Controller·Slot이 씀 |
| N-40 | C1 | 고쳐짐 | — | `Common/BoardRotation.Yaw` 하나(`LobbyBoard.cs:81`, `ArtifactBoardMapper.cs:58`) |
| N-41 | C1 | 고쳐짐 | — | `LobbyBoard`가 `_layout.Contains`(`:49,66`) = `LobbyBoardData.Contains`(`:62`) |
| N-42 | C1 | 남음·다시 찾음 | R-M-05 | 덱은 `_view.ID`로 고쳐짐, 아티팩트 `ArtifactPopupViewScreen.Id => UIId.Artifact` 남음 |
| N-43 | C1 | 남음·다시 찾음 | R-N-64 | `CanOpen => _started && CanWork`(`ArtifactPopupController.cs:79`)가 Scope `CanOpen`과 다르고 닫기 허용에도 씀(`:102,169`), `CanWork && _displayActive` 조합 반복(`:142-169`) |
| N-44 | C1 | 남음·이번에 안 잡힘 | — | `_lastPublishedScope`·`previous?.Supersede()`(`LobbyViewSystem.cs:43,163-171`), 늦은 콜백 `if (_stopped || _faults.IsFaulted) return;`(`:148`), Brain 검사 로비만(`:199`) 그대로. 인계 §5 미룬 낮음 |
| N-45 | C1 | 남음·다시 찾음 | R-N-70, R-N-34 | 입력 판정은 하나로, 목록 크기 두 곳은 남음(인계 §5) |
| N-46 | C1 | 고쳐짐 | — | `IsPending => CanMove(Status, Faulted)`(`LobbyDepartureRequest.cs:36`) |
| N-47 | C1 | 고쳐짐 | — | `IssueNumber()` 하나(`ArtifactViewBuffer.cs:66,92,99-104`) |
| N-48 | C1 | 고쳐짐 | — | Job 둘이 `ArtifactCoreCallJob` 하나로 |
| N-49 | C1 | 남음·다시 찾음 | R-N-27 | 범위 안 대부분 `float.IsFinite`로, `LobbyDeckUI.cs:234` `IsNaN || IsInfinity` 남음, 판정 식 갈림은 R-N-27 |
| N-50 | C1 | 고쳐짐 | — | 주석이 "전투 코드와 묶지 않습니다(N-50)"로 바뀜(`LobbyViewHopPath.cs:18`) |
| N-51 | C1 | 고쳐짐 | — | `LobbySceneRoot.Validate`가 레이아웃·`ui.ValidateComposition()`까지(`:23-36`), System은 그것만 부름(`:156`) |
| N-52 | C1 | 고쳐짐 | (R-N-66 새로) | `LobbyViewInput`이 `ViewAttachmentInputLayers`를 받음(`:17,24,53,56,74`) |
| N-53 | C1 | 남음·다시 찾음 | R-N-33 (R-M-02 관련) | 인계 §5 미룬 낮음 |
| M-13 | C2 | 고쳐짐(남은 일부) | R-N-63 | `LobbyFacilityKinds.All/Count`, 종류→실행 표(`LobbyFacilityActions.cs:20-38`), `FacilityCount`·종류 switch 없음. "종류마다 하나" 검사 세 방식은 R-N-63 |
| N-01 | C2 | 남음·다시 찾음 | R-M-06 | 쓰는 쪽별 묶음은 생겼지만 `LobbyViewServices` 생성자 21인자(아티팩트 전용 다수) 그대로(`:29-39`) |
| N-54 | C2 | 고쳐짐 | — | `ILobbyFacilityAction` 표로 받음(`LobbyFacilityActions.cs:20-43`) |
| N-55 | C2 | 고쳐짐 | — | 이동 시작·소리를 `LobbyPieceMover`로(`LobbyInteraction.cs:10,79`) |
| N-56 | C2 | 고쳐짐(남은 일부) | R-N-38 | `ILobbyDeckDisplay` 계약, `V/Lobby` 안 `LobbyDeckUI` 참조는 `LobbyDeckPresenter.cs:68` `LobbyDeckUI.CloseSlot` 하나 |
| N-57 | C2 | 고쳐짐 | — | 계약을 `IArtifactPopupLifetime`·`IArtifactPopupScreenAreas`·`IArtifactPopupCloseReceiver`로 나눔, `BindClose(IArtifactPopupCloseReceiver)` |
| N-58 | C2 | 남음·다시 찾음 | R-N-50 | 방 검사만 한 곳(`ValidateLabels` → `LobbyVisualPrefabRules`), 역할 switch(`LobbyVisualTable.cs:42-62`) 그대로(인계 §5 부분 해결) |
| N-59 | C2 | 고쳐짐 | — | `LobbyFacilityView`에 `_selectionCollider` 캐시 없음(코드 전체 검색 0) |
| M-14 | C4 | 고쳐짐(남은 일부) | R-N-73 | 팝업 목록 `ILobbyPopup`·`ILobbyPopupFactory`·`FindPopups`. 공통 계약의 출력 복원 단계는 R-N-73 |
| N-61 | C4 | 고쳐짐 | (R-N-20 새로) | 층 이름 `_services.Config.OutputLayerName`, 생성은 `ArtifactPopupViewFactory.cs:50` |
| N-15 | C5 | 고쳐짐 | (R-N-25 새로) | `V/Lobby`에 `ViewCameraHandleLease`·`CameraHandle` 0건 |
| N-16 | C5 | 남음·다시 찾음 | R-N-41 | `IArtifactInputSlot : IServiceModule`·`ExportServices`(`ArtifactInputSlot.cs:11,60`) 그대로, 키 주석만 고침(인계 §5) |
| N-17 | C5 | 고쳐짐 | — | `_faults = faults ?? throw new ArgumentNullException`(`PopupViewScope.cs:37`) |
| N-18 | C5 | 고쳐짐(남은 일부) | R-N-21 | `CurrentTime` try/catch·Tick 재검사 걷음(`ChessPieceMotion.cs:125-128,236`). `_beginToken`은 경로 준비 catch에만 남아 R-N-21 |
| N-19 | C5 | 고쳐짐 | (R-N-45 새로) | `LobbyCharacterVisual`에 `ValidateCharacterPrefab` 호출 없음. 판 자세 유한성(`LobbyBoard.cs:63,83`)은 근거 있게 유지(인계 §5) |
| N-20 | C5 | 남음·다시 찾음 | R-N-54 | Buffer 입구 `command.Validate()`(`ArtifactViewBuffer.cs:65,91`)와 생성·Job·도메인 검사 겹침 |
| N-21 | C5 | 고쳐짐 | — | `LobbyProgress.cs:56` 한 줄 검사만, `RequireBound`·`ArtifactEditFacts` throw 없음 |
| N-22 | C5 | 남음·다시 찾음 | R-N-36, R-N-37 | Open·SetModel 재검사는 걷음(`Validate()` 정의만 `:219`). 바인딩 판정 재검사·필수 참조 null 재검사는 남음 |
| N-23 | C5 | 고쳐짐 | — | Context 속성을 그대로 씀, 주석 "(N-23)"(`LobbyViewSystem.cs:71-92`), `LobbyPlan` `?? throw` 0건 |
| N-24 | C5 | 고쳐짐 | — | 덱 Hide는 `Clear()` 하나(`LobbyDeckPopupController.cs:67-71`), `Tick`의 `_isBlocked()` 한 번(`LobbyInteraction.cs:96`), `TryExecute` 한 번 판정 |
| N-25 | C5 | 고쳐짐 | — | `ClearPreview` 한 번(`ArtifactPopupController.cs:152`), `beforeBoardChange` 필수(`ArtifactBoardRefresher.cs:32`). `_reportedEditNumber`는 근거 있게 유지(인계 §5) |
| N-26 | C5 | 고쳐짐 | — | `BorrowedCount`·`Root`·`CloseButton`·`VisibleSlotCount` 0건, `InvalidateCandidate` private(`ArtifactInteraction.cs:54`) |
| N-27 | C5 | 고쳐짐 | (R-N-46의 TryGet은 새로) | `GetAllArtifactViews` 0건 |
| N-28 | C5 | 남음·다시 찾음 | R-N-01 | `Array.Empty<LobbyDeckCardContent>()`(`LobbyDeckPopupController.cs:62`) |
| N-29 | C5 | 남음·다시 찾음 | R-N-46 | 아이콘은 선택 칸이 됐지만 읽는 곳 없음 그대로 |
| N-30 | C5 | 고쳐짐 | — | 닿지 않는 Add 실패 갈래 없음, `IsChildOf` 겹침 걷고 주석 "(N-30)"(`LobbyVisualPrefabRules.cs:54-61`) |
| N-31 | C5 | 남음·이번에 안 잡힘 | — | `startCell`+`startFacing` 모양과 `FacilityArrivalInput[]` 두 모양 그대로(`LobbyBoardDataSO.cs:19-23`). 인계 §5 근거 있게 유지 |
| N-32 | C5 | 고쳐짐 | — | `HasBattle`·`JobType.None` 0건 |
| N-33 | C5 | 남음·다시 찾음 | R-N-11 | `SelectPressed`가 막혀도 먼저 `SelectCanceled()`(`ArtifactDragController.cs:139`) |
| N-34 | C5 | 고쳐짐 | — | 아티팩트 누름은 같은 `hit`를 `UpdateCandidate(hit)`로(`:141,155`). 로비 다시 고르기는 근거 있게 유지(인계 §5) |
| N-63 | C6 | 남음·다시 찾음 | R-N-61 | struct → `sealed class ArtifactCoreCallJob`, 그러나 `_started`·`IsCompleted` 두 표지와 예외 때 `_started`만 참(`:18-40`) 그대로 |
| N-64 | C6 | 고쳐짐 | — | `BindOwner`는 `Awake`(`LobbyDeckUI.cs:44-49`), `Storage` getter는 예외만(`ArtifactPopupView.cs:176`) |
| N-65 | C6 | 남음·다시 찾음 | R-N-40 | `GetComponent` 뒤 `AddComponent`(`ArtifactPopupUI.cs:282-283`). 인계 §5 사용자 결정으로 미룸 |
| N-66 | C6 | 남음·다시 찾음 | R-N-42 | 아티팩트 UI 일곱 파일 `Screens/Stage`에 그대로 |
| N-67 | C6 | 고쳐짐 | (R-N-44 cellScale은 M-04 대응) | `floorPrefab` + `FormerlySerializedAs("cellPrefab")`(`ArtifactPopupView.cs:23-24`), 파일 `ArtifactPopupBoard.cs` |
| N-68 | C6 | 고쳐짐 | — | `LobbyModelData` 속성 internal, `LobbySceneRoot` 멤버 internal, `Render` internal(`LobbyDeckListItemView.cs:44`), 드래그 메서드는 명시 구현만 |
| N-69 | C6 | 고쳐짐 | (R-N-90 다른 위치 새로) | 1차 위치 문구 모두 한국어 + 자기 형식 머리(`ArtifactPopupConfigData:39-41`, `ArtifactPopupConfigTable:24-32`, `ArtifactSceneRoot:30,35`), 영어 문구 검색 0 |
| N-70 | C6 | 고쳐짐 | — | 생성자 인자 넷 검사(`ArtifactDragDisplay.cs:31-36`) |
| N-71 | C6 | 고쳐짐 | — | 생성 함수 주입 판 없음, `Create(worldRoot, data)` 하나(`LobbyVisualSet.cs:17,38`) |
| N-72 | C6 | 고쳐짐 | — | `LobbyBgmPreset` 이름 상수(`LobbyViewSystem.cs:36,178`) |
| N-35 | C7 | 고쳐짐 | — | Tick은 `HasCameraMoved`일 때만(`ArtifactDragController.cs:90-91`) |
| N-36 | C7 | 고쳐짐 | — | `_hoverTarget` 같으면 건너뜀(`:280-281`) |
| N-37 | C7 | 남음·다시 찾음 | R-N-88 | 인계 §5 부분 해결(칸 이미지 재사용), Canvas·모델 토글 남음 |
| N-02 | C8 | 고쳐짐 | — | 매퍼는 `ArtifactPopupView.Configure`(`:46`) 한 곳에서 만들고 드래그 표시에 넘김, 컨트롤러는 `ArtifactSceneRoot`를 받지 않음(Factory만) |
| N-03 | C8 | 남음·다시 찾음 | R-N-89 | `_drag?.Hide()`(`ArtifactPopupView.cs:69`) 남음, Factory가 Configure 뒤에만 되는 `BoardMapper`를 꺼냄(`ArtifactPopupViewFactory.cs:49`) |
| N-73 | C8 | 남음·다시 찾음 | R-N-89 | `Interaction` 노출은 걷음, `LobbyViewScene.Root`·`Output` 노출과 `_scene.Root.WorldRoot`(`LobbyViewScope.cs:72`) 남음 |
| N-74 | C8 | 고쳐짐 | — | `internal sealed class ArtifactViewData`, 속성·`Validate` internal |
| N-75 | C8 | 남음·다시 찾음 | R-M-04 | 단계 변경자 internal + `Status` public(`LobbyDepartureRequest.cs:28,68`) 그대로 |
| M-11 | C9 | 남음·다시 찾음 | R-N-76 | 원본 `_writer`·`_presenter` 필드와 감싸개 넷 공존(`ArtifactPopupController.cs:25-27,298-396`), `_host.IsCurrent` 20곳 |
| N-13 | C9 | 고쳐짐 | — | 단계마다 `canWork()` 실패 때 그 단계 자원을 정리(`LobbyViewScene.cs:47-78`) |
| N-14 | C9 | 남음·다시 찾음 | R-N-09 | 인계 §5 되돌림 |
| M-04 | C10 | 남음·다시 찾음 | R-N-75, R-N-23, R-N-44 | 바닥 `localPosition = Vector3.zero`·`CellScale`만(`ArtifactPopupView.cs:114-116`). 2차 Q-R2-5 C로 의도만 주석 |
| M-06 | C10 | 고쳐짐 | (R-N-53 새로) | View가 `progress.Read().IsArtifactStopped`로 정지 안내·편집 차단(`LobbyViewServices.cs:54`) |
| N-04 | C10 | 고쳐짐 | — | `SetPopupBlocked`가 Layer 조건 없이 늘 반영(`LobbyViewScope.cs:97-105`) |
| N-05 | C10 | 고쳐짐 | — | `End(scope, false)` 뒤 `return null`(`PopupInputSession.cs` Begin) |
| N-06 | C10 | 남음·다시 찾음 | R-N-22 | 교체 중 예외는 `Debug.LogException`만(`PopupViewScope.cs:78,200`, `ArtifactPopupController.cs:124`). 2차 R2-a A 결정 |
| N-08 | C10 | 고쳐짐 | — | `InstanceAlreadyPlaced`·`RevisionOverflow` 갈래 있음(`ArtifactPopupText.cs:23,30`) |
| N-09 | C10 | 고쳐짐 | — | 비드래그 회수는 `else if (_hasPointer)`(`ArtifactDragController.cs:191`) |
| N-10 | C10 | 고쳐짐 | — | 루트 Canvas `ScreenSpaceOverlay` 검사(`ArtifactPopupUI.cs:130`) |
| N-11 | C10 | 고쳐짐 | (R-N-49 새로) | 실패 셋을 각자 문구로(`ArtifactPopupConfigTable.cs:24-32`) |
| N-12 | C10 | 고쳐짐 | — | `if (scope.Start(RequireRunning))`일 때만 배경음(`LobbyViewSystem.cs:175-180`) |
| N-38 | C11 | 고쳐짐 | — | `ToArray` 검색 0, 슬롯 이름은 바뀔 때만(`ArtifactPopupUI.cs:251`) |
| N-39 | C11 | 고쳐짐 | — | 만료 콜백 `_expirePreview`·`_expireEdit` 채널마다 하나(`ArtifactPopupStatus.cs:33-42,144,159`) |
| N-76 | C12 | 고쳐짐 | (R-N-12 다른 주석 새로) | `ArtifactViewSystem`·`ArtifactEditFlow` 검색 0, 구현 목록에 `OwnedPresenter`(`IArtifactPopupPresenter.cs:7`), "중앙에 하나만" 0 |
| N-77 | C12 | 고쳐짐 | — | 수신자 "로비 아티팩트 팝업", Factory "로비 아티팩트 팝업이 쓰는", LiftPath에 `StageCombatDeparture` |
| N-78 | C12 | 고쳐짐 | (R-N-04 새로) | `_audio` 사본 없음, 주석 "묶음(Audio)이 원본"(`LobbyViewSystem.cs:38`) |
| N-79 | C12 | 고쳐짐 | — | `OnEnter` 요약 고침(`LobbyLifeCycle.cs:62`), 전환 Job 주석이 로딩 유무 두 경우를 적음(`LobbyTransitionJob.cs:42-43`) |
| N-80 | C12 | 고쳐짐 | — | 팝업 루트 찾기를 Detach 전 `FindPopups`로(`LobbyViewSystem.cs:154-158`) |
| N-81 | C13 | 남음·다시 찾음 | R-N-03 | `_lobbyTexts.SetActive(!blocked)`(`LobbyViewScope.cs:104`). 인계 §5 1차 결정 유지 |
| N-82 | C13 | 고쳐짐 | — | `ClearControlInput`(→ `OnHoverExit`)을 `_isOpen = false` 전에(`LobbyDeckUI.cs:94,98`) |
| N-83 | C13 | 고쳐짐(남은 일부) | R-N-39 | 보유 슬롯 상세는 UI 한 경로, 슬롯 위 여부는 `IsPoolHovered`(`ArtifactDragController.cs:270-276`). 하드코딩 가림 영역·사각형 집기는 R-N-39 |

### 표 2 — 이번 93개

"2차 새" = 위치에 2차 리팩토링 새 파일(지시 목록)이 들어 있고 1차 대응이 없는 것.

| 이번 | 심각도 | 1차 대응 | 2차 새 |
|---|---|---|---|
| R-M-01 | 낮음 | 새로 찾음(M-01 고친 결과의 감싸개 사본) | ○ FaultReportingUIInputReceiver·FaultReportingLobbyReceiver |
| R-M-02 | 낮음 | 1차 버림 N-07과 같은 주장(+N-53 갈래) | |
| R-M-03 | 중간 | M-03(남은 일부) | |
| R-M-04 | 낮음 | N-75 | |
| R-M-05 | 낮음 | N-42 | |
| R-M-06 | 낮음 | N-01(+M-14) | |
| R-N-01 | 낮음 | N-28 | |
| R-N-02 | 낮음 | 새로 찾음 | |
| R-N-03 | 낮음 | N-81 | |
| R-N-04 | 낮음 | 새로 찾음 | ○ LobbyInputServices |
| R-N-05 | 낮음 | 새로 찾음 | ○ ILobbyPopupFactory·LobbyDeckPopupFactory |
| R-N-06 | 낮음 | 새로 찾음(N-68과 같은 종류, 다른 위치) | |
| R-N-07 | 낮음 | 새로 찾음 | |
| R-N-08 | 낮음 | 새로 찾음 | |
| R-N-09 | 낮음 | N-14 | |
| R-N-10 | 낮음 | 새로 찾음 | ○ LobbyDeckPopupFactory |
| R-N-11 | 낮음 | N-33 | |
| R-N-12 | 낮음 | 새로 찾음 | |
| R-N-13 | 낮음 | 새로 찾음 | |
| R-N-14 | 낮음 | 새로 찾음 | |
| R-N-15 | 낮음 | M-09(남은 일부: 모델 풀) | |
| R-N-16 | 낮음 | 새로 찾음(N-08 관련) | |
| R-N-17 | 낮음 | 새로 찾음 | |
| R-N-18 | 낮음 | 새로 찾음 | |
| R-N-19 | 낮음 | 새로 찾음 | |
| R-N-20 | 낮음 | 새로 찾음(N-61 고친 결과) | |
| R-N-21 | 낮음 | N-18(남은 일부, `_beginToken`) | |
| R-N-22 | 낮음 | N-06 | |
| R-N-23 | 낮음 | M-04 | |
| R-N-24 | 낮음 | 새로 찾음 | ○ FaultReportingUIInputReceiver |
| R-N-25 | 낮음 | 새로 찾음(N-15 고친 뒤 낡은 주석) | |
| R-N-26 | 낮음 | 새로 찾음 | |
| R-N-27 | 낮음 | N-49 | |
| R-N-28 | 낮음 | 새로 찾음(1차 M-10이 "모양 차이"로 넘긴 부분) | |
| R-N-29 | 낮음 | 새로 찾음 | |
| R-N-30 | 낮음 | 새로 찾음 | |
| R-N-31 | 낮음 | 새로 찾음 | ○ LobbyPieceMover |
| R-N-32 | 낮음 | 새로 찾음 | |
| R-N-33 | 낮음 | N-53 | |
| R-N-34 | 낮음 | N-45 | |
| R-N-35 | 낮음 | 새로 찾음 | |
| R-N-36 | 낮음 | N-22 | |
| R-N-37 | 낮음 | N-22 | |
| R-N-38 | 낮음 | N-56(남은 일부) | |
| R-N-39 | 낮음 | N-83(남은 일부) | |
| R-N-40 | 낮음 | N-65 | |
| R-N-41 | 낮음 | N-16 | |
| R-N-42 | 낮음 | N-66(+`FindPopups` 이름은 새로) | |
| R-N-43 | 낮음 | 새로 찾음 | |
| R-N-44 | 낮음 | M-04(N-67 관련) | |
| R-N-45 | 낮음 | 새로 찾음(N-19 고친 뒤 낡은 주석) | |
| R-N-46 | 낮음 | N-29(+`TryGet` 미사용은 M-03 고친 결과) | |
| R-N-47 | 낮음 | 새로 찾음 | |
| R-N-48 | 낮음 | 새로 찾음 | |
| R-N-49 | 낮음 | 새로 찾음(N-11 관련) | |
| R-N-50 | 낮음 | N-58 | |
| R-N-51 | 낮음 | 새로 찾음 | ○ LobbyFacilityKinds |
| R-N-52 | 낮음 | 새로 찾음 | |
| R-N-53 | 낮음 | 새로 찾음(M-06 고친 결과) | |
| R-N-54 | 낮음 | N-20 | |
| R-N-55 | 낮음 | 새로 찾음 | ○ ArtifactCoreCallJob |
| R-N-57 | 낮음 | 새로 찾음 | |
| R-N-58 | 낮음 | 새로 찾음(N-20·N-79의 `IsTransitionSucceeded` 관련) | |
| R-N-60 | 낮음 | 새로 찾음 | |
| R-N-61 | 낮음 | N-63 | |
| R-N-62 | 낮음 | 새로 찾음 | |
| R-N-63 | 낮음 | M-13(남은 일부) | |
| R-N-64 | 낮음 | N-43 | |
| R-N-65 | 낮음 | 새로 찾음 | |
| R-N-66 | 낮음 | 새로 찾음(N-52 주제, 다른 사본) | |
| R-N-67 | 낮음 | 새로 찾음 | |
| R-N-69 | 낮음 | 새로 찾음(M-12 관련) | ○ LobbyArtifactPopupFactory |
| R-N-70 | 낮음 | N-45 | |
| R-N-71 | 낮음 | 새로 찾음 | ○ LobbyFacilityAction |
| R-N-72 | 낮음 | 새로 찾음 | ○ LobbyFacilityAction |
| R-N-73 | 낮음 | M-14(남은 일부) | |
| R-N-74 | 낮음 | 새로 찾음 | |
| R-N-75 | 낮음 | M-04 | |
| R-N-76 | 낮음 | M-11 | |
| R-N-77 | 낮음 | 새로 찾음 | |
| R-N-78 | 낮음 | 새로 찾음 | |
| R-N-80 | 낮음 | 새로 찾음 | |
| R-N-81 | 낮음 | 새로 찾음 | |
| R-N-82 | 낮음 | 새로 찾음 | |
| R-N-83 | 낮음 | 새로 찾음(M-01 갈래, 1차에 없던 위치) | |
| R-N-84 | 낮음 | 새로 찾음 | |
| R-N-85 | 낮음 | 새로 찾음 | |
| R-N-86 | 낮음 | 새로 찾음 | |
| R-N-87 | 낮음 | 새로 찾음 | |
| R-N-88 | 낮음 | N-37 | |
| R-N-89 | 낮음 | N-03, N-73 | |
| R-N-90 | 낮음 | 새로 찾음(N-69 규칙, 다른 위치) | |
| R-N-91 | 낮음 | 새로 찾음 | |

합: 1차 대응 36, 새로 찾음 57, 그중 2차 새 11.

### 표 3 — 인계 §5 알려진 남은 문제

| 구분 | 문제 | 이번 |
|---|---|---|
| 미룬 낮음 | N-53 패널형 팝업 공통 기반 | 다시 찾음 R-N-33 (R-M-02 관련) |
| | N-66 아티팩트 UI 폴더·네임스페이스 | 다시 찾음 R-N-42 |
| | N-81 로비 글자 묶음 SetActive | 다시 찾음 R-N-03 |
| | N-28 덱 시설 늘 빈 목록 | 다시 찾음 R-N-01 |
| | N-44 씬 연결 골격 두 벌 | 안 잡힘 |
| 사용자 결정으로 미룸 | N-65 보유 슬롯 위젯 직렬화 | 다시 찾음 R-N-40 |
| 되돌림 | N-14 뗀 연결 붙잡음 | 다시 찾음 R-N-09 |
| 주석만 | N-16 입력 슬롯 서비스 등록·키 | 다시 찾음 R-N-41 |
| 부분 해결 | N-58 역할 SO switch | 다시 찾음 R-N-50 |
| | N-37 미리보기 켜고 끄기 왕복 | 다시 찾음 R-N-88 |
| | N-45 덱 목록 크기 두 곳 | 다시 찾음 R-N-70 (R-N-34) |
| 그대로 둔 것 | N-22 덱 카드 클릭 `IsInteractiveSlot` | 다시 찾음 R-N-36·R-N-70 |
| | N-25 `_reportedEditNumber` 가드 | 안 잡힘 |
| | N-31 시작 칸 입력 모양 | 안 잡힘 |
| | N-34 로비 누름·뗌 다시 고르기 | 안 잡힘 (R-N-86은 Raycast 순서라 다른 문제) |
| | N-19 판 자세 유한성 검사 | 안 잡힘 |

다시 찾음 11, 안 잡힘 5.
