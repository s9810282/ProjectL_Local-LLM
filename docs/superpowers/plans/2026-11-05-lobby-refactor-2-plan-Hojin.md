# 로비 리팩토링 2차 계획

- 작성자: 호진(이호진)
- 설계: [로비 리팩토링 2차 설계](../specs/2026-11-05-lobby-refactor-2-design-Hojin.md). 결정은 Q-R2-1~Q-R2-9 모두 끝났다(Q-R2-5는 C, Q-R2-7은 B, 나머지는 A).
- 근거: [평가 보고서](../specs/2026-11-05-lobby-view-evaluation-Hojin.md)·[문제 목록](../specs/2026-11-05-lobby-view-evaluation-issues-Hojin.md). 기준 판은 `fc02777aa`다.
- 상태: 계획 작성, 착수 전.

## 0 진행 규칙

- Phase 하나가 끝나면 다음 순서로 진행하고 멈춘다. 다음 Phase는 사용자 지시로 시작한다.
  1. 실행 기록(§3)을 쓴다.
  2. Codex 검수를 받는다(세션 저장·추론 요약을 켜고, 개인 규칙을 프롬프트에 붙인다).
  3. 보고한다.
- 새 시험 코드는 만들지 않는다. MSBuild도 돌리지 않는다. 보고에는 "컴파일 미확인"을 쓴다. 기존 시험이 깨지면 그 부분만 맞춘다. 착수 전에 `Assets/Tests`에서 바뀌는 형식을 검색한다.
- 바꾸는 줄마다 `git blame`으로 작성자를 확인한다.
  - 다른 사람 줄도 평가 범위 파일 안이면 고친다.
  - 고친 다른 사람 줄은 §4에 적어 그 사람에게 넘긴다.
- 보호 영역은 연결 줄만 바꾼다. Framework·GlobalSystem 내부, Core, `ServiceKeys` 계약, Factory 공용 조립이 그렇다.
- 프리팹·씬 값은 Codex 몫이다. 필요하면 Unity에서 할 일 목록을 먼저 넘긴다.
- 설계와 다른 결정이 필요하면 구현하지 않고 먼저 묻는다.
- 공용 `ViewSystem/Common` 부품을 바꾸면 스테이지 맵 호출처도 함께 확인한다. Unity 확인 목록에도 스테이지 맵 항목을 넣는다.
- 경로 머리: `V/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/`, `S/` = `Assets/ProjectL/Script/Systems/`.

## 1 Phase 요약

| Phase | 문제 | 예상 13기준 평균(알려진 문제 기준) |
|---|---|---|
| R2-a 정확성 | M-01·M-02·M-03·M-05·M-04·M-06, N-04·N-05·N-06·N-08·N-09·N-10·N-11·N-12 | 88.7 → 약 91.7 |
| R2-b UI 규약·SSOT | M-09·M-12·M-10·M-07·M-08, N-40~N-52(N-44·N-45 제외), N-82 | 약 93.6 |
| R2-c 구조 | M-13·M-14·M-11, N-01·N-02·N-03·N-13·N-14·N-45·N-54~N-59·N-61·N-73·N-75·N-83 | 약 96.2 |
| R2-d 정리 | N-15~N-34(N-28 제외, N-16은 주석만), N-63~N-72(N-66 제외), N-74, N-76~N-80 | 약 97.9 |
| R2-e 성능 | N-35~N-39 | 약 98.8 |
| R2-f 재평가 | 같은 평가 설계로 다시 잼 | 실제 90.8(§3.12) |

- 미룬 낮음 다섯(N-53·N-66·N-81·N-28·N-44)은 남는다(설계 §5).
- N-65(보유 목록 슬롯 위젯 프리팹 직렬화)도 슬롯 UI 디자인이 정해질 때까지 미룬다(2026-11-05 사용자, §3.8). R2-d에서는 코드 쪽 정리만 했다.
- N-16은 주석만 고치므로(Q-R2-7 B) 낮음으로 남는다.

## 2 Task

### 2.1 R2-a 정확성

| Task | 내용 | 파일(주로) | 문제 |
|---|---|---|---|
| a1 | 공용 감싸개 `FaultReportingUIInputReceiver`를 새로 만든다. 예외를 경계에 보고하고, 정지 뒤에는 부르지 않는다 | `V/Common/FaultReportingUIInputReceiver.cs`(새) | M-01 |
| a2 | 로비 입력 감싸개 `FaultReportingLobbyReceiver`를 새로 만든다. `LobbyViewInput.Connect`가 슬롯에 이것을 건다 | `V/Lobby/FaultReportingLobbyReceiver.cs`(새), `LobbyViewInput.cs` | M-01 |
| a3 | 덱 Presenter를 a1로 감싸 화면에 건다. 아티팩트 `InputReceiver`·`CloseReceiver`는 같은 보고 꼴로 맞춘다 | `V/Lobby/Deck/LobbyDeckPopupController.cs`, `V/ArtifactPopup/ArtifactPopupController.cs` | M-01 |
| a4 | `LobbyViewScope.CanRunFacility()`(막힘 아님 + `IsLobbyLayer`)를 둔다. `CanDepart`·`CanExecute`가 이것을 쓴다. 도착 완료는 이 판정이 참일 때만 소비하고, 거짓이면 기다린다(Q-R2-1 A) | `LobbyViewScope.cs`, `LobbyFacilityActions.cs`, `LobbyInteraction.cs`, `LobbyViewInput.cs` | M-02 |
| a5 | 외형 조회를 `GetArtifactView`(ID 담은 예외)로 바꾼다. Refresher·View의 null·`?.`·`continue` 갈래를 걷는다(Q-R2-2 A) | `LobbyViewServices.cs`, `ArtifactBoardRefresher.cs`, `ArtifactPopupView.cs` | M-03 |
| a6 | 가림 판정은 `WorldSpace`가 아닌 Canvas의 `GraphicRaycaster`만 센다. Canvas는 Raycaster마다 처음 한 번 찾아 사전에 둔다(Q-R2-3 A). 스테이지 맵 노드 프리팹에 월드 공간 Canvas가 있는지 확인한다 | `V/Common/ScreenUIPointerBlocker.cs` | M-05 |
| a7 | 바닥 덮어쓰는 줄에 의도 주석을 단다. `boardCenter` 툴팁에 "격자 위치만 옮김, 바닥은 4×4 고정 아트로 원점"을 적는다. "중앙에 하나만" 주석도 고친다(Q-R2-5 C) | `ArtifactPopupView.cs`, `S/DataBaseSystem/Data/View/Table/Artifact/Item/ArtifactPopupConfigDataSO.cs` | M-04(·N-76 일부) |
| a8 | 아티팩트 도메인 정지를 View가 읽는 좁은 계약(`IsStopped` + 이유)으로 낸다. 팝업에 안내 한 줄을 보이고 편집 입력을 막는다. 출발은 막지 않는다(Q-R2-6 A). LifeCycle 쪽 연결 줄을 확인한다 | `LifeCycles/Lobby/Artifact/*`, `ArtifactPopupController.cs`, `ArtifactPopupStatus.cs` | M-06 |
| a9 | 팝업이 닫히면 늘 막힘을 푼다 | `LobbyViewScope.SetPopupBlocked` | N-04 |
| a10 | `PopupInputSession.Begin`이 수명을 확립하지 못하면 실패를 돌려준다. `PopupViewScope`가 열기를 멈춘다. 공용 부품이라 스테이지 맵 팝업도 해당한다 | `V/Common/PopupInputSession.cs`, `PopupViewScope.cs` | N-05 |
| a11 | 연결이 바뀌어 원인 예외를 버리는 갈래도 경계에 보고한다 | `V/Common/PopupViewScope.cs` | N-06 |
| a12 | 실패 사유 문구에 빠진 열거 값을 채운다. 보드 설정 표의 실패를 원인별 문구로 나눈다 | 문구 클래스, `ArtifactBoardConfigTable.cs` | N-08·N-11 |
| a13 | 우클릭 회수는 `_hasPointer`일 때만 한다. 아티팩트 UI `Validate`에서 Overlay Canvas인지 검사한다 | `ArtifactDragController.cs`, `ArtifactPopupUI.cs` | N-09·N-10 |
| a14 | `LobbyViewScope.Start`가 준비 완료 여부를 돌려준다. `Attach`는 성공일 때만 배경음을 튼다 | `LobbyViewScope.cs`, `LobbyViewSystem.cs` | N-12 |

**Unity 확인(R2-a)**
1. 시설 세 곳 이동·열기·출발이 지금과 같다.
2. 말이 덱 시설로 가는 도중 ESC로 설정창을 열고 도착 뒤 닫는다. 닫는 순간 덱 팝업이 열린다.
3. 아티팩트 안내 글자 위에 포인터를 둔다. 그 시설의 호버 빛·클릭이 된다.
4. 아티팩트 팝업 열기·끌어 놓기·우클릭 회수·닫기가 지금과 같다.
5. 스테이지 맵에서 노드 호버·클릭, 상점·회복 팝업 열기·닫기가 지금과 같다(공용 부품).
6. 콘솔에 새 예외가 없다.

### 2.2 R2-b UI 규약·SSOT

| Task | 내용 | 문제 |
|---|---|---|
| b1 | `ArtifactBoardMapper.ModelPose(cells, rotation, prefab)`와 회전 각도 도우미를 둔다. 배치·드래그 표시가 함께 쓴다 | M-09·N-40(아티팩트 둘) |
| b2 | `ArtifactPopupView.lobbyRoot` 직렬화 칸을 걷는다. 로비 연결이 월드 루트를 넘기고, null이면 예외로 막는다(Q-R2-4 A) | M-12 |
| b3 | 누름·뗌·강제 취소 해석과 선택 소유자 보존을 `V/Common` 부품 하나로 둔다. 로비·아티팩트 입력이 함께 쓴다 | M-10 |
| b4 | Job 우선순위는 한 상수에서 읽는다. 씬 노드 순서는 기준 값에서 식으로 구한다 | M-07·M-08 |
| b5 | 작은 사본을 하나씩 원본으로 모은다(설계 §3.2 목록). 로비 판 회전, `LobbyBoardData.Contains`, UIId, 아티팩트 입력 가능 판정, 덱 판정, `IsPending` 파생, Buffer 번호 발급, 명령·프리뷰 Job 하나, 유한 값 판정, 걸음 회전 주석, `LobbySceneRoot.Validate`, Layer 창구가 대상이다 | N-40~N-52(N-44·N-45 제외) |
| b6 | 덱 UI는 플래그를 내리기 전에 호버 해제를 보낸다 | N-82 |

Unity 확인: 아티팩트 끌어 놓기에서 놓는 순간 모델이 튀지 않는다. 팝업이 열리면 로비 공간이 숨는다. 덱 호버·고정. 로비 진입·이탈 순서.

### 2.3 R2-c 구조

| Task | 내용 | 문제 |
|---|---|---|
| c1 | 시설 실행 계약 `ILobbyFacilityAction`과 종류 → 항목 표를 둔다. 판의 도착 칸은 (종류 → 칸·방향) 목록 하나로 둔다. 시설 수는 열거에서 구한다 | M-13·N-54 |
| c2 | 로비 팝업을 Start·Tick·Dispose 공통 항목 목록으로 둔다. 씬 루트 찾기·출력 복원은 각 팝업 Factory가 맡는다 | M-14 |
| c3 | 서비스 묶음을 쓰는 쪽별로 나눈다. 이동 시작·소리는 말 표시로 옮긴다. `ILobbyDeckDisplay`를 둔다. 덱 화면·Presenter의 입력 판정·목록 크기 사본을 그 계약 한 곳으로 모은다(N-45, R2-b에서 옮김). 아티팩트 표시 계약을 나눈다. 방 프리팹 검사는 한 곳에 둔다. `LobbyFacilityView` 캐시는 View로 옮긴다 | N-01·N-45·N-55~N-59 |
| c4 | 출력 마스크 층 이름을 설정으로 뺀다. 마스크 생성은 아티팩트 Factory로 옮긴다 | N-61 |
| c5 | `LobbyViewInput.Tick()`을 둔다. 출발 단계 계약은 읽기·쓰기로 나눈다. 보드 좌표 변환기는 하나만 만든다. 드래그 표시 준비는 `Configure` 한 입구에서 한다 | N-73·N-75·N-02·N-03 |
| c6 | 컨트롤러는 감싼 판만 쥔다. 씬 표시 시작의 부분 정리를 한 꼴로 맞춘다. `Detach`에서 지난 연결을 바로 놓는다 | M-11·N-13·N-14 |
| c7 | 아티팩트 보유 목록 슬롯의 누름·끌기를 포인터 인터페이스 한 경로로 모은다(R2-b에서 옮김) | N-83 |

- N-45·N-83은 R2-b에서 옮겨 왔다(§3.3 계획과 다른 점). 2026-11-05 사용자 "R2-c로 확정". 두 항목은 R2-b 완료로 세지 않는다.
- c1·c2는 Factory 연결 줄(`ArtifactPopupViewFactory`, 로비 생성 계약)을 바꾼다. 민형님 공유 문서에 적을 것이 생기면 §4에 둔다.

### 2.4 R2-d 정리

| Task | 내용 | 문제 |
|---|---|---|
| d1 | 죽은 경로·중복 검사를 걷는다. N-16은 `LobbyInputKey`·`ArtifactInputKey` 주석만 실제 경로로 고친다(Q-R2-7 B) | N-15~N-34(N-28 제외) |
| d2 | 관례를 맞춘다. 대상은 아래와 같다. 슬롯 위젯 프리팹 직렬화(N-65)는 Codex 프리팹 작업 목록을 먼저 넘긴다 | N-63~N-72(N-66 제외) |
| d3 | `ArtifactViewData` 접근 수준을 맞춘다 | N-74 |
| d4 | 낡은 주석을 고친다. N-78과 함께 `LobbyViewSystem._audio` 사본을 걷는다(현강님 줄이라 §4에 적음) | N-76~N-80 |

d2 대상:
- 가변 struct Job → class
- 조회 멤버의 상태 변경을 준비 메서드로
- 슬롯 위젯 프리팹 직렬화(N-65)
- `cellPrefab` → `floorPrefab`(`FormerlySerializedAs`)
- 예외 문구를 한국어 + 형식 이름 머리말로
- 배경음 프리셋 상수

### 2.5 R2-e 성능

| Task | 내용 | 문제 |
|---|---|---|
| e1 | 드래그 판정을 칸만 구하는 좁은 판정으로 바꾼다. 포인터·카메라 자세가 바뀐 프레임에만 다시 판정한다. LB-a T13 판단(카메라가 섞이는 동안 필요)은 카메라 자세 변화 검사로 지킨다 | N-35 |
| e2 | 보드 호버는 대상이 바뀔 때만 갱신한다. 미리보기 셀 이미지는 다시 쓴다 | N-36·N-37 |
| e3 | 미리보기·보드 변경 경로의 할당을 없앤다. 실패 문구 지연 예약은 하나만 둔다 | N-38·N-39 |

### 2.6 R2-f 재평가

- 다른 컴퓨터에서 실행한다(2026-11-05 사용자). 시작점은 [재평가 인계](../handoffs/2026-11-05-lobby-refactor-2-reevaluation-handoff-Hojin.md)다.
- 평가 설계를 그대로 쓴다. 기준 판은 R2-e 커밋이고, 범위 파일 목록을 다시 센다.
- 결과를 1차와 견준다. 고친 문제와 새로 찾은 문제를 나눠 적는다.

## 3 실행 기록

### 3.1 R2-a 정확성 (2026-11-05, 사용자 "R2-a 시작")

- 기준은 `fc02777aa` + 평가 문서 커밋 전 작업 트리다.
- 컴파일 미확인, 새 시험 없음, 시험 변경 없음. `Assets/Tests`에는 바뀐 형식을 쓰는 시험이 없었다.

| Task | 한 것 |
|---|---|
| a1 | 새 `V/Common/FaultReportingUIInputReceiver.cs`를 만들었다. 정지 뒤에는 안쪽을 부르지 않고, 예외를 `RuntimeFaultBoundary.Report`로 보낸다. `.meta`는 Unity가 만든다 |
| a2 | 새 `V/Lobby/FaultReportingLobbyReceiver.cs`를 만들었다. `LobbyViewInput`이 오류 경계를 받아 감싸개를 슬롯에 걸고 푼다. 생성 인자에 `faults`를 더했다 |
| a3 | 덱 Presenter를 a1로 감싸 화면에 건다. 아티팩트 `InputReceiver` 일곱 입구는 직접 try/catch로 보고한다. 포인터 이동마다 불리므로 대리자를 만들지 않았다. 닫기 수신자는 a1로 감싸고, 안쪽의 "호스트가 현재일 때만 보고" try/catch는 걷었다 |
| a4 | `LobbyViewScope.CanRunFacility()`를 두었다(옛 `CanDepart`). `LobbyFacilityActions.CanExecute`는 세 시설 모두 이것을 먼저 보고, `Execute`는 판정이 거짓이면 예외로 막는다. `LobbyInteraction.Tick`은 로비 Layer가 아니면 도착 완료를 소비하지 않고 기다린다(Q-R2-1 A) |
| a5 | 외형 조회를 `ArtifactViewTable.GetArtifactView`로 바꿨다. 없는 ID는 `KeyNotFoundException`(ID 포함)이 난다. Refresher의 `?.` 셋을 걷었다 |
| a6 | `ScreenUIPointerBlocker`가 **루트** Canvas가 월드 공간인 Graphic은 가림으로 보지 않는다. 판정은 Raycaster마다 한 번 해 사전에 둔다 |
| a7 | 바닥 덮어쓰는 줄에 의도 주석을 달았다. `boardCenter` 툴팁과 "중앙에 하나만" 주석도 고쳤다(Q-R2-5 C) |
| a8 | 기존 공개 사실 `ILobbyProgressReader.Read().IsArtifactStopped`를 View가 읽는다. 연결 경로는 `LobbyViewSystem` → `LobbyViewServices.IsArtifactStopped` → `LobbySceneFactory` → `ArtifactPopupViewFactory` → 컨트롤러다. 멈춘 동안 `AllowsInput`이 거짓이 되고, 상태 문구가 가장 앞에 "아티팩트 편집 처리가 멈췄습니다. 로비에 다시 들어오면 다시 받습니다."를 보인다. 닫기와 출발은 막지 않는다 |
| a9 | `SetPopupBlocked(false)`는 늘 막힘을 푼다 |
| a10 | `PopupInputSession.Begin`이 수명을 쥐지 못하면 `null`을 돌려준다(복귀 불가로 바로 끝낸 경우, 다른 소유자로 넘어간 경우). `PopupViewScope.BeginInput`은 `null`이면 `AbortOpen` 뒤 열기를 멈춘다. 스테이지 맵 팝업도 같은 부품을 쓴다 |
| a11 | `PopupViewScope.Open`·`AdoptOpenScreen`과 아티팩트 `Close`가 새 연결로 넘어가 예외를 버리는 갈래에서 `Debug.LogException`으로 원인 로그를 남긴다 |
| a12 | 실패 문구에 `InstanceAlreadyPlaced`·`RevisionOverflow`를 더했다. `ArtifactPopupConfigTable`의 실패 셋을 원인별 한국어 문구로 나눴다 |
| a13 | 우클릭 회수는 `_hasPointer`일 때만 한다. `ArtifactPopupUI.Validate`가 루트 Canvas가 Overlay인지 검사한다 |
| a14 | `LobbyViewScope.Start`가 준비 완료 여부를 돌려준다. `Attach`는 true일 때만 배경음을 튼다 |

**설계·계획과 다른 점(셋)**
1. **a5:** 외형 표의 `Prefab`은 선택 칸이다(`ArtifactViewData.Validate`가 `Prefab != null`일 때만 검사). 그래서 `ArtifactPopupView.Render`의 "모델 프리팹 없으면 건너뜀"은 정상 갈래로 남기고 주석만 달았다. 설계 §2.3의 "프리팹은 DB 생성 때 이미 필수"는 틀린 사실이었다. 없는 **ID**만 예외다.
2. **a6:** 렌더 모드는 Raycaster의 Canvas가 아니라 **루트 Canvas** 기준으로 판정한다. 공용 설정창 `OptionCanvas`와 `SandstormLoading`은 직렬화 값이 월드 공간(`m_RenderMode: 2`)이다. 하지만 화면 공간 루트 아래의 중첩 Canvas라 실제로는 화면 공간이다. 자기 값으로 판정하면 설정창이 클릭을 막지 못한다.
3. **a11:** 버리는 갈래에서 오류 경계로 **보고하지 않고 로그만** 남긴다. 연결이 교체되는 정상 흐름에서도 이 갈래로 예외가 올 수 있어서, 보고하면 정상 교체가 F1로 멈출 수 있다. 문제 목록이 말한 결함은 "첫 원인 로그가 사라진다"이므로 로그로 충분하다고 판단했다.

**바뀐 동작**
- 로비 시설·아티팩트 끌기·덱·아티팩트 닫기 입력에서 난 예외가 이제 F1로 멈춘다. 전에는 로그만 남고 계속됐다.
- 시설에 도착한 순간 설정창이 열려 있으면 설정창을 닫을 때까지 기다렸다가 실행한다.
- 외형 표에 없는 아티팩트 ID가 오면 F1이다.
- 방의 시설 안내 글자 위에서도 호버·클릭이 된다.

### 3.2 R2-a Codex 검수 (2026-11-05)

읽기 전용 정적 검수다. 세션을 저장하고 추론 요약도 남겼다. 결과는 **지적 2건(모두 낮음, 정책 차이)**이고, 새 실행 결함이나 컴파일 파손은 찾지 못했다.

1. **M-02: 팝업·출발 막힘에서는 대기하지 않고 취소함 → 수용(기록 정정).**
   - 막혔을 때(시설 팝업·출발 처리 중) 가던 이동을 버리는 것은 원래부터 의도한 동작이다. `LobbyInteraction.Tick` 주석에 "가는 도중 막히면 그 이동을 버립니다"라고 적혀 있다. 이번에는 이 동작을 그대로 두었다.
   - 기다리는 것은 "입력 Layer만 빼앗긴 경우(설정창 등)"뿐이다.
   - 설계 §2.2의 "판정이 거짓이면 기다린다"를 이 뜻으로 고쳤다. 설계와 다른 점의 넷째로 센다.
2. **a11·N-06: 교체된 이전 연결의 예외를 모두 로그로만 처리 → A로 결정(2026-11-05 사용자 "A 그대로").** 지금 코드를 그대로 둔다.
   - 지금 조건은 "연결이 교체됐는가" 하나다. 그래서 정상 교체 때 나는 Layer 미확립 예외와 진짜 구성 오류를 구분하지 못하고 둘 다 로그로 끝낸다.
   - 고를 수 있는 안은 둘이다.
     - A(지금): 이전 연결의 예외는 로그만 남긴다. 정상 교체가 F1로 멈추지 않는다.
     - B: 예외 종류로 나눈다. Layer 미확립(`PopupInputSession`의 그 예외)만 로그로 끝내고, 나머지는 경계에 보고한다. 예외 형식을 새로 두거나 문구로 구분해야 한다.

나머지 확인 결과다.
- a1~a10·a12~a14가 해당 문제를 고쳤다. a5·a6 판단이 맞다. 스테이지 노드 프리팹 8개에는 Canvas가 없다.
- 시험을 포함한 `Assets/` 전체에 남은 호출자가 없다. 비호진 줄 두 곳은 §4와 맞다.
- 한 파일 안에 줄 끝 문자가 섞인 곳이 없다.

**Unity 확인(R2-a, 사용자)**: §2.1 목록 1~6과 같다.

### 3.3 R2-b UI 규약·SSOT (2026-11-05, 사용자 "확인 후 R2-b")

- R2-a 위에 이어서 했다. R2-a의 Unity 확인은 사용자에게서 아직 결과를 받지 못했다.
- 컴파일 미확인, 새 시험 없음, 시험 변경 없음. `Assets/Tests`에 바뀐 형식을 쓰는 시험은 없다.
- 새 `.cs` 넷의 `.meta`는 Unity가 만든다.

| Task | 한 것 |
|---|---|
| b1 | `ArtifactBoardMapper.ApplyModelPose`(칸 평균 중심·모델 높이·회전·배율) 하나를 배치 표시(`ArtifactPopupView.Render`)와 드래그 표시(`ArtifactDragDisplay.Show`)가 부른다. 판 회전 식은 새 `V/Common/BoardRotation.Yaw`에 두고, 로비 판의 도착 방향(`LobbyBoard`)도 이것을 쓴다(N-40). `PlaceModel`은 자세를 정하지 않는 `RentModel`이 됐다. 바닥 자세 값은 그대로다 |
| b2 | `ArtifactPopupView`의 `lobbyRoot` 직렬화 칸(`FormerlySerializedAs("stageMapRoot")` 포함)을 걷었다. 로비 연결이 `LobbySceneRoot.WorldRoot`를 `LobbyViewPopups.Create` → `ArtifactPopupViewFactory.Create` → `ArtifactPopupView.Configure`로 넘기고, null이거나 팝업 표시가 그 루트 안에 있으면 예외를 던진다. 아티팩트 안내 문서 세 줄도 고쳤다 |
| b3 | 새 `V/Common/PointerSelectInterpreter`(포인터 Performed만 받기, 누름·뗌·강제 취소 → `SelectEdge`)와 `V/Common/SelectOwnerSlot<T>`(연결 대상과 선택 소유자 보존)를 만들었다. 로비·아티팩트 Controller 둘과 Slot 둘이 이것을 쓰고, 전달할 것만 각자 정한다. 연결이 겹칠 때의 예외 문구 머리말만 `[LobbyReceiverSlot]`·`[ArtifactInputSlot]`로 바뀌었다 |
| b4 | 새 `LifeCycles/Lobby/LobbyJobPriority`(Artifact 3·Departure 0)를 둘 다 아티팩트 호출·확인 Job과 출발 새 Run·전환 Job이 읽는다. `LobbyArtifactCheckJob.CheckPriority`는 걷었다. `LobbyViewSystem.DetachPriority`는 `LobbyLifeCycle.ExitPreparationPriority + 100`으로 구한다(값 -100 그대로) |
| b5 | 아래 목록 |
| b6 | `LobbyDeckUI`의 `Hide`·`SetModel`·`BindInput`이 입력 플래그를 내리기 전에 `ClearControlInput()`을 불러, 호버 해제가 수신자에게 한 번 간다(N-82) |

b5에서 고친 것:
- 판 범위 판정은 `LobbyBoardData.Contains` 하나로 두고, DB 검사와 `LobbyBoard`가 함께 쓴다(N-41).
- 덱 화면 어댑터의 UIId는 `_view.ID`에서 읽는다(N-42). 아티팩트 쪽은 표시 계약에 ID가 없어 그대로 뒀다.
- 아티팩트 판정에 이름을 붙였다(N-43). 표시 연결이 유효한지는 `IsDisplayCurrent`, 호버해도 되는지는 `CanHover`다.
- `LobbyDepartureRequest.IsPending`을 전이 표 `CanMove(Status, Faulted)`에서 구한다(N-46).
- `ArtifactViewBuffer`에서 편집·프리뷰 번호를 `IssueNumber()` 하나가 발급한다(N-47).
- 편집·프리뷰 Job을 `ArtifactCoreCallJob`(sealed class) 하나로 합쳤다(N-48). `ArtifactCommandJob.cs`는 `git mv`로 이름을 바꿔 GUID를 그대로 두었고, `ArtifactPreviewJob.cs`와 `.meta`는 `git rm`으로 지웠다. 이 이름 바꾸기·삭제는 git 색인에 올라가 있다.
- 유한 값 판정 사본을 `float.IsFinite`, `ViewPrefabRules.IsFinite(Vector3)`, 새 `ViewPrefabRules.IsFinite(Quaternion)`로 바꿨다(N-49).
- 걸음 회전 주석에서 "전투와 같은 기준"을 걷었다(N-50).
- 로비 씬 루트 검사(가상 카메라·worldRoot·UI)를 `LobbySceneRoot.Validate` 한 곳에 모았다. System의 `ValidateRoot`는 걷었다(N-51).
- 로비 입력의 Layer 변경은 팝업 세션과 같은 `ViewAttachmentInputLayers` 창구를 지난다(N-52).

**계획과 다른 점**
- N-45(덱 화면·Presenter의 입력 판정·목록 크기 사본)와 N-83(아티팩트 보유 슬롯 누름이 두 경로)은 R2-c로 넘겼다.
  - N-45는 R2-c c3의 덱 표시 계약 `ILobbyDeckDisplay`와 함께 정하는 것이 맞다.
  - N-83은 보유 슬롯 입력을 포인터 인터페이스로 바꾸는 구조 작업이다.
- 판 회전 도우미는 아티팩트 판 매퍼가 아니라 공용 `BoardRotation`에 두었다. 로비 판이 아티팩트 판 매퍼에 기대지 않게 하려는 것이다.

**바뀐 동작**
- 없다. 같은 값·같은 순서를 원본 하나에서 읽게만 바꿨다.
- 다른 점 하나: 아티팩트 팝업 표시가 로비 공간 루트 안에 있으면 이제 `Configure`가 예외를 던진다. 안내 문서는 원래 그렇게 두지 말라고 적고 있었다.

**Unity 확인(R2-b)**
1. 아티팩트를 끌어 놓을 때 놓는 순간 모델이 튀지 않는다. 회전한 채 놓아도 같다.
2. 아티팩트 팝업이 열리면 로비 방·캐릭터가 숨고, 닫으면 돌아온다.
3. 로비 말이 시설에 도착할 때의 방향이 지금과 같다.
4. 시설 누름·뗌·끌다 취소, 아티팩트 끌기·우클릭이 지금과 같다(입력 해석 공용화).
5. 덱 호버·고정·닫기, 아티팩트 편집·프리뷰(놓기 가능/불가 색), 출발이 지금과 같다.
6. 콘솔에 새 예외가 없다.

### 3.4 R2-b Codex 검수

검수는 `git diff HEAD`와 새 파일을 대상으로 했고, 결과는 중간 1건·낮음 3건이다(실행으로 재현하지는 않음). 네 건 모두 수용했다(컴파일 미확인).

| 등급 | 지적 | 반영 |
|---|---|---|
| 중간 | `LobbyDeckUI`(N-82): 호버 해제 콜백에서 `BindInput`·`SetModel`이 다시 불리면, 바깥 호출이 버전을 검사하기 전에 새 상태를 덮는다. `Hide`는 콜백 예외가 나면 닫힘·입력 차단을 남기지 못한다 | `ClearControlInput(version)`이 콜백마다 버전을 보고, 낡으면 false를 돌려주고 멈춘다. `BindInput`·`SetModel`은 그 결과가 false면 바로 끝내고, 버전이 지금 것일 때만 `finally`에서 입력을 끈다. `Hide`는 `finally`에서 닫힘·입력 차단·버전 올리기를 반드시 한다 |
| 낮음 | `ArtifactCoreCallJob`(N-48): Core 호출 전에 완료를 올린다 | 시작 가드 `_started`와 완료 표지를 나눴다. 완료는 Core가 정상 반환한 뒤 올린다(옛 두 Job과 같음) |
| 낮음 | `ArtifactPopupView.RentModel`(b1): 자세보다 먼저 켠다 | `RentModel`은 꺼진 채 돌려준다. 바닥·배치 모델은 자세를 정한 뒤 켠다(옛 순서) |
| 낮음 | 계획 §1·§2.2·§2.3에 N-45·N-83 이관이 없다 | §1 표·b5·b6·c3을 고치고 새 c7을 더했다. 이관은 사용자가 확정함(2026-11-05 "R2-c로 확정") |

나머지(씬 참조 b2 GameObject 187812004 동일, b3 입력 순서, b4 우선순위 값, N-46·N-47·N-51·N-52, 삭제한 API의 남은 호출 없음, 줄 끝 섞임 없음)는 문제가 없다는 결과였다.

**Unity 확인 추가(R2-b 검수 반영)**
7. 덱을 열고 닫으며 카드에 호버한 채 닫거나 목록을 바꿔도, 호버가 남거나 입력이 막히지 않는다.

### 3.5 R2-c 구조 (2026-11-05, 사용자 "R2-c진행해")

c1~c6 코드 완료(N-14는 되돌림, §3.6). 컴파일 미확인, 새 시험 없음, 시험 변경 없음(바뀐 API를 쓰는 시험 없음). c7은 사용자 결정(Q-R2c-1 A) 뒤 §3.7에서 했다.

**c1 시설 종류(M-13·N-54)**
- 시설 종류 목록 원본 `LobbyFacilityKinds`(새 파일, 열거 값에서 구함)를 만들었다. 시설 수·방 시설 셋 검사·도착 칸 검사·실행 표가 모두 여기서 센다.
- 판의 도착 칸은 (종류 → 칸·방향) 목록 하나다. `LobbyBoardDataSO.facilityArrivals`, `LobbyBoardData.ArrivalOf`를 쓰고, 빠진 종류·겹친 칸을 막는다. 짝 비교 셋과 Pose 필드 셋, `GetArrival`의 switch는 걷었다.
- `LobbyBoard_Default.asset`은 옛 칸 셋을 목록으로 옮겼다. 값은 그대로다(Artifact (0,2) R270 / StageEntry (1,3) R0 / Deck (2,2) R90).
- 시설 실행은 계약 `ILobbyFacilityAction` + 종류 → 항목 표(`LobbyFacilityActions`)다. 모든 종류에 항목이 정확히 하나 있어야 만들어진다.

**c2 팝업 목록(M-14)**
- 공통 항목 `ILobbyPopup`(Start·Tick·출력 복원·정리 + 시설 종류·열기)과 생성 입구 `ILobbyPopupFactory`를 두었다.
- `LobbyViewPopups`는 Factory 목록(덱 → 아티팩트)을 돈다. 시작은 목록 순서, Tick·출력 복원·정리는 역순이다(옛 순서와 같음).
- 덱은 `LobbyDeckPopupFactory`, 아티팩트는 `LobbyArtifactPopupFactory`가 만든다. 덱 컨트롤러는 항목을 직접 구현하고, 아티팩트 컨트롤러는 로비 쪽 감싸개가 감싼다.
- 아티팩트 씬 루트 찾기는 System에서 `ArtifactPopupViewFactory.FindSceneRoot`로 옮겼다.

**c3 서비스 묶음·표시 계약(N-01·N-45·N-55~N-59)**
- `LobbyViewServices`를 쓰는 쪽별 묶음으로 나눴다: `LobbySceneServices`·`LobbyInputServices`·`ArtifactPopupServices`(새 파일 셋)와 출발 창구. 부품은 자기 묶음만 받는다. 아티팩트 Factory·컨트롤러는 `ArtifactPopupServices` 하나를 받는다(컨트롤러 인자 19 → 10).
- 이동 시작·이동 소리를 새 `LobbyPieceMover`로 옮겼다(N-55, `LobbyInteraction` 인자 9 → 6). 소리 프리셋·시점은 그대로다.
- 덱 표시 계약 `ILobbyDeckDisplay`(새 파일)를 두었다. 덱 컨트롤러·Presenter·팝업 수명·Factory가 이 계약만 받는다(N-56).
- 화면이 입력을 받을 수 있는지는 `ILobbyDeckDisplay.IsShown` 하나로 정하고, 화면의 수신 판정과 Presenter가 함께 쓴다(N-45).
- 아티팩트 표시 계약을 넷으로 나눴다(N-57): 표시 `IArtifactPopupDisplay`, 수명 `IArtifactPopupLifetime`, 영역 판정 `IArtifactPopupScreenAreas`, 닫기 수신 `IArtifactPopupCloseReceiver`. 닫기는 `IUIInputReceiver` 전체와 슬롯 -2 대신 `CloseClicked()` 하나를 받는다.
- 방 안내 글자 셰이더 검사를 `LobbyVisualPrefabRules.ValidateRoomLabelShader` 한 곳으로 옮겼다. 이제 글자 연결 검사가 먼저 돌았는지에 기대지 않는다(N-58).
- 시설 클릭 영역 캐시를 `LobbyFacilityView`(DB 입력 계층)에서 View의 `LobbyFacilityPicker`로 옮겼다. 만들 때 한 번만 찾는다(N-59).

**c4 출력 마스크(N-61)**
- 층 이름을 아티팩트 표시 설정 `outputLayerName`(기본 "UI")으로 뺐다. 그 층이 프로젝트에 있는지는 DB 생성 때 검사한다.
- 기본 에셋에 같은 값 한 줄을 더했다.
- 마스크는 `ArtifactPopupViewFactory.Create`가 만든다. 출력 복원은 컨트롤러의 `RestoreOutput`이 맡는다.

**c5 캡슐화(N-73·N-75·N-02·N-03)**
- 씬 표시는 입력에 필요한 값만 `LobbyInputTargets`(말·판·시설·출력 카메라)로 넘긴다. Scope는 `_input.Tick()`만 부른다.
- 출발 창구:
  - View는 쓰기 싱크 하나만 받는다. 이 싱크의 `Current`·`Submit`·`Cancel`은 읽기 계약 `ILobbyDepartureState`(새 파일)만 주고받으므로, View는 단계 변경자를 부를 수 없다.
  - 실행 계층은 지금처럼 읽기 싱크로 구체 요청을 받는다. View가 쓰던 읽기 싱크 인자는 걷었다.
- 보드 좌표 변환기는 `ArtifactPopupView.Configure`가 하나만 만들고, 드래그 표시와 포인터 판정이 함께 쓴다(N-02). 컨트롤러는 `ArtifactSceneRoot`를 받지 않는다(`Root` 속성 삭제).
- 드래그 표시도 `Configure` 한 입구에서 준비한다. `PrepareDragDisplay`를 걷었고, 준비 전에 부르면 `_drag?.`로 그냥 지나가지 않고 예외로 멈춘다(N-03).

**c6 수명(M-11·N-13·N-14)**
- 아티팩트 컨트롤러는 Buffer 쓰기·Presenter·표시·수명을 감싼 판(`Owned*`)으로만 쥔다. 본문 도우미 여섯의 `_host.IsCurrent` 검사는 걷었다. 남은 호스트 판단은 `CanWork`와 Close 예외 판정 둘뿐이다.
- 씬 표시 시작은 한 꼴로 맞췄다. 만든 것은 연결이 살아 있을 때만 정리 대상에 넣고, 이미 끝났으면 그 자리에서 정리한다(방·배경 출력·캐릭터 모두).
- N-14(`Detach` 뒤에도 뗀 연결을 붙잡음)는 Codex 검수 뒤 되돌려 그대로 둔다(§3.6). 처음에는 `Detach`가 바로 `Supersede`하도록 고쳤다.

**계획과 다른 점**
- c1: 실행 항목은 컨트롤러가 직접 구현하지 않는다. 출발과 아티팩트는 감싸개(`LobbyFacilityAction`, 아티팩트 팝업 항목)를 거친다. 아티팩트 팝업이 로비 계약에 기대지 않게 하려는 것이다.
- c2:
  - 생성 계약 `ILobbySceneFactory`를 `FindPopups`(이전 연결을 떼기 전)와 `Create`로 나눴다. 씬 구성이 틀리면 기존 연결을 건드리지 않고 멈추는 옛 순서를 지키려는 것이다.
  - `StopInput`의 아티팩트 입력 Buffer 비우기는 서비스 수명이라 System에 남겼다.
- c3:
  - N-58은 방 프리팹 검사만 한 곳으로 모았다. 표의 역할 SO 분기(switch)는 손대지 않았다.
  - N-45는 입력 판정만 하나로 모았다. 목록 크기는 그대로 둘이다. 화면의 `_visibleCount`는 그린 칸 수이고, Presenter의 `_model.Cards.Count`는 자기 모델을 읽을 때 쓰는 경계다. 같은 모델에서 나오지만 두 군데가 따로 든다. 아래 "남은 결정"의 Q-R2c-2다.
- c5: `LobbyArtifactDomain`의 `Stop`·`Reset`은 바꾸지 않았다. 클래스가 internal이고 실행 계층만 쥐어, View에서는 이미 닿지 않는다.

**바뀐 동작**
- 씬 표시 시작 중 `requireRunning`이 예외를 던지면 방을 바로 정리한다. 전에는 남을 수 있었다.
- 표시 설정에 없는 층 이름이면 DB 생성에서 멈춘다. 전에는 팝업을 만들 때 멈췄다.
- 그 밖의 값·순서는 같다.

**새 파일**(`.meta`는 Unity가 만듦): `LobbyFacilityKinds`, `ILobbyDepartureState`, `ILobbyDeckDisplay`, `IArtifactPopupLifetime`, `IArtifactPopupScreenAreas`, `IArtifactPopupCloseReceiver`, `ArtifactPopupServices`, `LobbyDeckPopupFactory`, `ILobbyFacilityAction`, `LobbyFacilityAction`, `ILobbyPopup`, `ILobbyPopupFactory`, `LobbyArtifactPopupFactory`, `LobbyInputServices`, `LobbySceneServices`, `LobbyPieceMover`(16개).
- 에셋 둘은 코드와 한 커밋이어야 한다. 하나라도 빠지면 DB 생성이 실패한다.
  - `LobbyBoard_Default.asset`(도착 칸 목록)
  - `ArtifactPopupConfig_Default.asset`(층 이름)

**남은 결정**
- Q-R2c-1(c7, N-83) 보유 목록 슬롯 입력:
  - A 상세 표시는 UI(호버·클릭) 한 경로가 갖고, 드래그는 보유 슬롯에서 시작할 때 상세를 쓰지 않는다.
    - 슬롯에서 끌어 나가면 상세가 비는 것이 화면 차이다.
    - 포인터 이동마다 하던 슬롯 순회도 UI 호버 상태를 읽게 바꿀 수 있다.
  - B 슬롯 누름을 포인터 인터페이스(`IPointerDownHandler`)로 받아 드래그를 시작한다.
    - EventSystem 누름과 InputSystem Select의 순서가 정해져 있지 않다. 둘이 서로 취소하지 않게 하는 장치가 더 든다.
  - 권장은 A다. 계획 문구(포인터 인터페이스 한 경로)는 B에 가깝다.
- Q-R2c-2(N-45 목록 크기): 그대로 둘지(권장, 화면이 그린 칸 수와 모델 경계는 다른 질문), 화면이 그린 수를 계약으로 내고 Presenter가 그것을 읽을지.

**Unity 확인(R2-c)**
1. 로비 진입·시설 셋 도착 방향·실행(덱·아티팩트·출발)이 지금과 같다.
2. 덱 팝업 호버·고정·닫기, 아티팩트 열기·끌어 놓기·회전·우클릭 회수·닫기 버튼이 지금과 같다.
3. 아티팩트 팝업이 열린 동안 보유 목록 UI가 보이고(출력 마스크), 닫으면 마스크가 돌아온다.
4. 로비 → Stage → 로비로 다시 들어와도 팝업·입력·배경음이 정상이다.
5. 말 이동 소리가 지금처럼 이동을 시작할 때 한 번 난다.
6. 콘솔에 새 예외가 없다. DB 생성(Boot)에서 로비 판·아티팩트 설정 오류가 없다.

### 3.6 R2-c Codex 검수 (2026-11-05)

검수는 HEAD `ab061c730` 대비 변경 42개와 새 파일 16개를 대상으로 했고, 결과는 중간 1건·낮음 1건이다(실행으로 재현하지는 않음). 둘 다 수용했다(컴파일 미확인).

| 등급 | 지적 | 반영 |
|---|---|---|
| 중간 | `Detach`가 바로 `Supersede`하면, 팝업 UI 호출 중에 뗀 경우 `PopupViewScope`가 미뤄 둔 입력 Layer 반환(`EndDisposedInput`, `_host.IsCurrent` 필요)이 다시는 돌지 않는다. 오류 경계 → `StopInput` → `Detach`도 이 길이다 | 옛 수명 규칙(다음 연결이 게시될 때 지난 연결을 `Supersede`)으로 되돌렸다. N-14는 해결하지 않은 채 남는다. 고치려면 미뤄 둔 정리가 "현재"가 아니어도 자기 Layer만 돌려주게 하는 공용 부품(`PopupViewScope`, 스테이지 맵도 씀) 변경이 필요하다 |
| 낮음 | `LobbyPieceMover`가 옛 세대 검사 없이 소리를 낸다. 이동 시작 중 취소·새 이동이 다시 들어오면 바깥 호출도 소리를 낼 수 있다(운영 경로 발생은 미확인) | 이동 요청 세대를 부품 안에 두고 취소·정리 때 올린다. 세대가 그대로일 때만 소리를 낸다 |

- 그 밖에 c1~c5, c6의 감싼 판, 서비스 조회 경로(Global Context 경유, 루트 우회 등록 없음), 줄 끝, 남은 호출자에는 문제가 없다는 결과였다.
- 판단 항목: N-58은 방 검사 부분만 풀렸으니 전체 해소로 세지 않는다. Q-R2c-1 권장 A·Q-R2c-2 권장(그대로)은 타당하다는 의견이다.

### 3.7 R2-c c7 보유 슬롯 상세 (2026-11-05, 사용자 "A로 진행" = Q-R2c-1 A)

코드 완료. 컴파일 미확인, 새 시험 없음.

- 보유 목록 슬롯의 상세는 `ArtifactPopupUI`(호버·클릭·이탈·목록 다시 그리기·숨김) 한 경로가 갖는다. 슬롯 상세가 떠 있는 동안 보드·드래그의 `ShowDetail`(지우기 포함)은 바꾸지 않는다.
- 드래그는 놓인 배치물을 집을 때만 상세를 쓴다. 보유 슬롯에서 끌기 시작할 때는 쓰지 않는다.
- 보드 호버는 슬롯 위인지를 UI 호버 상태(`IArtifactPopupScreenAreas.IsPoolHovered`)로 묻는다. 그래서 포인터 이동마다 하던 슬롯 영역 순회(`TryPickPool`)는 이제 누름 때만 돈다(`Resolve(point, pickPool)`).
- 화면 차이(사용자에게 알림): 보유 슬롯에서 끌어 나가면 상세가 빈다.

**Codex 검수**(중간 1, 수용)
- 지적: 슬롯에서 놓인 배치물 위로 옮기며 포인터 이동(InputSystem)이 슬롯 이탈(EventSystem)보다 먼저 오면, 보드 호버가 "아직 슬롯 위"로 보고 끝난다. 뒤이은 이탈이 상세를 비운 뒤로는 다시 고르지 않아, 다음 포인터 입력까지 상세가 빈다.
- 반영: 드래그 컨트롤러가 마지막 판단 때 슬롯 위였는지(`_hoverPoolSnapshot`)를 기억한다. 그 값이 바뀐 Tick에 지금 포인터로 다시 고른다. 매 Tick 읽는 것은 bool 하나다.
- 되돌림 둘(수명 규칙, 소리 세대 검사)은 HEAD 조건과 같다는 확인을 받았다.

**Unity 확인(c7)**
1. 보유 슬롯 호버·클릭 때 그 아티팩트 상세가 보이고, 슬롯을 벗어나면 빈다.
2. 슬롯에서 놓인 배치물 위로 포인터를 옮기면 그 배치물 상세가 보인다(멈춘 채로도).
3. 보유 슬롯에서 끌어 보드에 놓기·목록으로 되돌리기, 놓인 배치물 집기가 지금처럼 동작한다(슬롯 밖으로 끌면 상세가 비는 것만 다름).

### 3.8 R2-d 정리 (2026-11-05, 사용자 "커밋했어 R2-d", 기준 커밋 `9ea63ad80`)

코드 완료. 컴파일 미확인, 새 시험 없음, 시험 변경 없음(바뀐 API를 쓰는 시험 없음).

**d1 죽은 경로·중복 검사**
| 문제 | 한 것 |
|---|---|
| N-15 | 쓰는 곳 없는 로비 카메라 핸들 대여를 걷었다(AfterLoad 노드·`CameraHandlePriority`·Scope·Scene의 대여, 씬 묶음의 카메라 카탈로그) |
| N-16 | `ArtifactInputKey` 주석만 실제 전달 경로(`InGameViewInputContext.Artifact`)로 고쳤다(Q-R2-7 B) |
| N-17 | `PopupViewScope`의 오류 경계를 필수로 했다. null 갈래 둘을 걷었다 |
| N-18 | `ChessPieceMotion`이 시간 getter를 비재진입으로 보고, 그 앞뒤의 토큰·예약·대상 재확인을 걷었다. 예약 등록(콜백 있음)의 재확인은 그대로 둔다 |
| N-19 | 캐릭터 프리팹 재검사를 걷었다(검사 함수는 private으로). 판 자세 유한성 검사는 Codex 검수 뒤 남겼다(§3.9) |
| N-20 | 편집·프리뷰 요청 생성자의 `Validate`, 확인 Job 생성자의 번호 검사를 걷었다(Buffer Submit·`CreateCheck` 한 곳). `IsTransitionSucceeded`는 단계만 본다 |
| N-21 | 사실 읽기의 보장 검사를 `LobbyProgress.Read` 입구 한 번(로딩 서비스·도메인 `IsBound`)으로 줄였다. 도메인 `RequireBound` 셋과 `ArtifactEditFacts` 인자 검사 넷을 걷었다 |
| N-22 | 덱 `Open`·`SetModel`, 카드 `Render`, 배경 `ConnectOutput`, 보유 목록 `RenderPool`의 구성 재검사를 걷었다. 덱 닫기 클릭의 버튼 상태 재검사, 아티팩트 `_canClose` 사본(닫기 버튼 `interactable`이 원본)을 걷었다 |
| N-23 | Group Context가 보장한 속성의 `?? throw`·`?.`를 걷었다(`LobbyViewSystem`·`LobbyPlan`) |
| N-24 | 덱 `Hide`는 `Clear` 하나, 시설 실행은 `TryExecute` 한 입구(판정·실행 한 번), 도착 실행 앞 막힘 재검사 삭제, System Tick의 오류 검사는 Scope `CanWork`에 맡겼다 |
| N-25 | 보드 변경 콜백은 갱신기 생성자 필수 인자, `Tick(bool)`·`SetEdit(…, bool)` 기본값 삭제, `Open`의 프리뷰 지우기 삭제(Show가 함), `SelectPressed` 두 갈래 합침, Factory의 중복 인자 검사 삭제 |
| N-26·N-27 | `BorrowedCount`·`CloseButton`·`VisibleSlotCount`·`GetAllArtifactViews` 삭제, `InvalidateCandidate` private |
| N-29 | 아티팩트 아이콘을 선택 칸으로 바꿨다(필수 검사 삭제) |
| N-30 | 방 프리팹 검사의 닿지 않는 Collider 중복 갈래, 라벨 `IsChildOf` 겹침, 루트 Transform 두 번 검사를 걷었다 |
| N-31 | 배치판 SO의 배열·원소 null 검사를 걷었다 |
| N-32 | 로비 사실의 `HasBattle`과 전투 싱크 의존을 걷었다. 생성자 인자는 필수로 바꿨고, 닿지 않는 `JobType.None` 갈래는 걷었다 |
| N-33 | 끌고 있지 않고 보인 프리뷰도 없으면 `SelectCanceled`가 드래그 표시 정리(Buffer 프리뷰·문구·모델 다시 켜기)를 건너뛴다(`_dragShown`). 상세 지우기는 늘 한다(§3.9) |
| N-34 | 아티팩트 누름·뗌은 판정 결과 하나를 후보 계산에 넘긴다 |

**d2 관례**
| 문제 | 한 것 |
|---|---|
| N-63 | R2-b에서 끝남(합친 Job은 sealed class, 시작 가드와 완료 표지를 나눔) |
| N-64 | 덱 상세 카드 소유 연결은 `Awake`로, 아티팩트 모델 풀 저장소는 `Configure`가 만든다 |
| N-65 | 슬롯 위젯을 만들 때 한 번 찾아 보관한다(반복 GetComponent·BindOwner 삭제). 프리팹 직렬화는 슬롯 UI가 정해질 때까지 미룬다 |
| N-67 | `cellPrefab` → `floorPrefab`(`FormerlySerializedAs`, 씬 값 그대로). `ArtifactPopupDisplay.cs` → `ArtifactPopupBoard.cs`(`git mv`, `.meta` GUID 그대로) |
| N-68 | `LobbyModelData` 속성·`LobbySceneRoot.LobbyCamera`·`Validate`·`LobbyDeckListItemView.Render`를 internal로, 아티팩트 View 드래그 계약은 명시적 구현 한 방식으로, `Rotation4` using |
| N-69 | 영어·머리말 없는 예외 문구를 한국어 + 던지는 형식 이름 머리말로 맞췄다 |
| N-70 | `ArtifactDragDisplay` 생성자가 GameObject를 만들기 전에 인자를 검사한다 |
| N-71 | `LobbyVisualSet`의 시험용 생성 입구와 Instantiate 뒤 자세 다시 넣기를 걷었다 |
| N-72 | 배경음 프리셋 번호를 이름 있는 상수 `LobbyBgmPreset`으로 |

**d3·d4**
- N-74: `ArtifactViewData`를 internal로(속성·`Validate` 포함).
- N-76·N-77·N-79: 낡은 주석을 지금 코드에 맞췄다(없는 형식 cref, 옛 이름, 구현 목록, 입력 수신자, 체스 말 경로, 전환 시점 두 경우, OnEnter/OnTick 요약, 명령 중복 방지 책임).
- N-78: `LobbyViewSystem._audio` 사본을 걷고 묶음 `Audio`를 읽는다(현강님 줄, §4 결정대로 전달하지 않음).
- N-80: 아티팩트 루트 구성 검사를 이전 연결을 떼기 전(`FindSceneRoot`)으로 옮겼다.

**계획과 다른 점(그대로 둔 것)**
- N-22: 덱 카드 클릭의 `IsInteractiveSlot`은 남겼다. 모델 칸 경계 확인을 겸하기 때문이다.
- N-25: 상태 문구의 `_reportedEditNumber` 가드는 남겼다. 번호 순서 보호를 겸하기 때문이다.
- N-31: 시작 칸 입력 모양(`startCell`+`startFacing`)은 그대로 두었다. 시설 도착은 R2-c에서 종류를 가진 목록이 되어 모양이 이미 다르고, 맞추려면 에셋을 다시 옮겨야 한다.
- N-34: 로비 누름·뗌의 가림 판정·고르기는 남겼다. 호버 결과는 다음 Tick까지 낡을 수 있어 누름 순간 다시 고르는 것이 맞다.
- N-65: 프리팹 직렬화·모양 셀 생성·색 상수는 슬롯 UI 디자인(아이콘 이미지·스크롤 배경)이 정해진 뒤에 한다.

**바뀐 동작**
- 로비 가상 카메라가 카메라 카탈로그에 없어도 이제 멈추지 않는다. 전에는 핸들 대여가 이름 존재를 확인했다.
- 아티팩트 아이콘이 비어도 View DB 생성이 멈추지 않는다.
- 아티팩트 팝업 루트 구성이 틀리면 이전 로비 연결을 떼기 전에 멈춘다.

**N-65 프리팹 작업은 미룸(2026-11-05 사용자)**
- 사용자 설명: 보유 목록 슬롯은 아티팩트 아이콘 이미지를 따로 받을 예정이고, 스크롤되는 목록 뒤에 배경도 들어간다. 슬롯 모양이 정해지지 않았으니 지금 프리팹에 고정하지 않는다.
- 대상이 될 프리팹은 `Assets/ProjectL/Prefabs/UI/Artifact/ArtifactItem.prefab`(`ArtifactPopupUI.slotPrefab`)이다.
- 남은 일(슬롯 UI가 정해지면): 위젯을 프리팹에 직렬화, `CreateSlotShape`의 AddComponent 갈래 걷기, `Validate`에서 위젯이 없으면 예외, 모양 셀 생성·색 상수 정리. 아이콘을 보이게 되면 N-29에서 선택 칸으로 바꾼 `ArtifactViewData.Icon`을 다시 필수로 할지도 그때 정한다.
- 이번 R2-d에서 한 코드 정리(슬롯마다 위젯을 한 번만 찾기)는 그대로 둔다.

**Unity 확인(R2-d)**
1. 로비 진입·시설 셋·덱·아티팩트·출발이 지금과 같다. 콘솔에 새 예외가 없다.
2. 아티팩트 팝업의 바닥 모델이 지금처럼 보인다(`floorPrefab` 이름 바꿈, 씬 값 그대로).
3. 아티팩트 끌기·놓기·회전·우클릭 회수·닫기 버튼이 지금과 같다.
4. 로비 말 이동과 소리, 배경음이 지금과 같다.
5. 로비 → Stage → 로비로 다시 들어와도 정상이다.

### 3.9 R2-d Codex 검수 (2026-11-05)

검수는 HEAD `9ea63ad80` 대비 변경 56개를 대상으로 했고, 결과는 중간 1건·낮음 1건이다(실행으로 재현하지는 않음). 둘 다 수용했다(컴파일 미확인).

| 등급 | 지적 | 반영 |
|---|---|---|
| 중간 | `LobbyBoard`의 계산 자세 유한성 검사는 DB 검사와 중복이 아니다. DB는 입력 값만 보고, 칸 크기·월드 변환을 곱한 결과는 넘칠 수 있다 | 계산 결과 검사를 되살렸다 |
| 낮음 | N-33 건너뛰기가 막힌 누름뿐 아니라 드래그 없는 놓음·우클릭 회수·입력 취소의 상세 지우기도 건너뛴다 | 드래그 표시 정리만 건너뛰고 상세 지우기는 늘 한다 |

- 그 밖에 N-18·N-21·N-24·N-22·N-25·N-34·N-67·N-80, 접근 수준·명시적 구현·남은 호출자, 줄 끝에는 문제가 없다는 결과였다.
- "그대로 둔 것" 판단(N-22·N-25·N-31·N-34)은 타당하고, N-65는 후속 작업으로 남아 완료로 세지 않는다는 의견이다.

### 3.10 R2-e 성능 (2026-11-05, 사용자 "커밋 했어 R2-e", 기준 커밋 `b6342c734`)

코드 완료. 컴파일 미확인, 새 시험 없음, 시험 변경 없음. Profiler 확인은 사용자 Unity 확인에 넣었다.

| 문제 | 한 것 |
|---|---|
| N-35 | 끄는 동안의 후보 칸은 칸만 구하는 좁은 판정 `ArtifactPointerMapper.TryMapCandidate`로 구한다(보유 슬롯·배치물 훑기 없음, 가림·반환 영역 규칙은 같음). Tick은 출력 카메라의 뷰·투영 행렬·화면 영역이 마지막 판정 뒤 바뀐 프레임(`HasCameraMoved`)에만 다시 본다. 포인터 이동은 `PointerMoved`가 바로 반영한다. LB-a T13의 판단(카메라가 섞이는 동안 필요)은 이 카메라 변화 검사로 지킨다 |
| N-36 | 보드 호버는 마지막으로 상세에 쓴 대상(배치물 인스턴스 ID, 빈 칸)을 기억해 바뀔 때만 상세를 쓴다. 보드·슬롯 호버가 바뀌거나 다른 경로가 상세를 지우면(`_hoverSnapshot = null` 자리) 기억을 버린다 |
| N-37 | 미리보기 칸 이미지를 반환·재대여하지 않고 위치·색만 바꿔 다시 쓴다. 남는 칸만 끄고, 캔버스는 켜짐이 바뀔 때만 바꾼다. 칸 크기는 만들 때 한 번 넣는다. 부분 해결: 후보가 바뀌면 응답을 기다리는 동안 옛 칸을 숨기므로 칸·캔버스 켜고 끄기 왕복은 남는다(§3.11) |
| N-38 | 미리보기·보드·보유 슬롯 모양의 `IReadOnlyList` foreach를 인덱스 for로, `ClearBoard`의 `ToArray`를 뒤에서부터 꺼내기로 바꿨다. 보유 슬롯 이름은 (아티팩트 ID, 인스턴스 ID)가 바뀔 때만 만든다 |
| N-39 | 실패 문구 만료 예약은 채널(프리뷰·편집)마다 하나만 둔다. 문구가 바뀌면 만료 시각만 옮기고, 예약이 끝났을 때 시각이 남았으면 남은 시간만 한 번 더 기다린다. 콜백은 생성자에서 한 번 만든다. 버전 비교 필드는 걷었다 |

**바뀐 동작**: 없다(같은 값을 덜 자주 계산한다). 문구 3초 규칙·새 요청이면 다시 3초도 그대로다.

**Unity 확인(R2-e)**
1. 아티팩트를 끄는 동안 미리보기 칸(가능·불가 색)과 모델이 지금처럼 포인터를 따라간다. 로비 카메라가 섞이는 동안 포인터를 멈춰도 칸이 맞게 바뀐다.
2. 보드의 배치물 위를 오가면 상세가 바뀌고, 빈 칸에서는 빈다. 끌기·놓기·우클릭 회수 뒤에도 상세가 맞다.
3. 겹침·판 밖 실패 문구가 3초 뒤 사라지고, 실패 칸을 계속 옮겨 다니면 마지막 실패에서 3초를 센다.
4. (선택) Profiler에서 끌기 중 포인터를 멈춘 프레임에 `ArtifactPointerMapper.Resolve` 호출이 없고, 실패 칸을 옮겨 다녀도 GC Alloc이 늘지 않는다.

### 3.11 R2-e Codex 검수 (2026-11-05)

검수는 HEAD `b6342c734` 대비 변경 9개 파일을 대상으로 했고, 결과는 중간 1건·낮음 2건이다(실행으로 재현하지는 않음). 컴파일 미확인.

| 등급 | 지적 | 처리 |
|---|---|---|
| 중간 | `Matrix4x4`의 `!=`는 근사 비교라, 허용오차 안의 작은 카메라 섞임이 포인터 칸을 바꿔도 `HasCameraMoved`가 거짓일 수 있다 | 수용. 뷰·투영 행렬을 `Equals`로 정확히 비교한다 |
| 낮음 | N-37: 후보가 바뀌면 `ClearPreview`가 칸·캔버스를 끄고 다음 응답에서 다시 켜, 켜고 끄기 왕복이 남는다 | 기록. 응답을 기다리는 동안 옛 칸을 숨기는 동작은 지켜야 해서 그대로 두고 N-37을 부분 해결로 셈 |
| 낮음 | `Prepare`의 풀 생성 람다가 풀이 있어도 보드 갱신마다 클로저를 만든다(기존 코드) | 수용. 생성 함수를 `CreatePool`로 빼 풀이 없을 때만 만든다. 보드 갱신의 다른 배열 생성은 남아 전체 무할당은 아니다 |

- 그 밖에 N-35 좁은 판정 값 동일, N-36 기억 무효화, N-37 잔여 칸, N-39 만료 규칙, 컴파일·줄 끝에는 문제가 없다는 결과였다.

### 3.12 R2-f 재평가 (2026-11-05, 사용자 "여기서 시작", 기준 판 `cc3ccdf96`)

- 결과: [재평가 보고서](../specs/2026-11-05-lobby-view-reevaluation-Hojin.md), [문제 목록](../specs/2026-11-05-lobby-view-reevaluation-issues-Hojin.md). **코드 변경 없음.**
- 같은 평가 설계로 쟀다. 범위는 145개·9,559줄이고, 실행은 20회다(찾기 12·합치기 1·검증 5·정리 1·1차 대조 1).
- **13기준 평균 88.7 → 90.8**, 요청 7기준 91.4 → 91.1, 12기준 89.0 → 90.8
  - 높음 0·중간 1·낮음 92(1차 중간 6·낮음 88)
  - 남은 중간은 R-M-03(C10) 하나다: 아티팩트 외형 프리팹이 선택 칸이라, 비면 판에서 조용히 안 보인다.
  - C10은 60.0 → 82.5로 올랐다. 다른 열 기준은 중간 0이지만 낮음이 상한 근처라 90 언저리에 머문다.
- 예상(약 99.0)과 차이: 대부분 새로 찾은 낮음 57개다.
  - 그중 11개는 2차가 새로 만든 파일에서 나왔다.
  - 4개는 2차 수정의 부산물이다.
- 1차 대조:
  - 1차 중간 6개는 모두 고쳐졌다. M-03·M-09는 곁가지가 R-M-03·R-N-15로 남았다.
  - 1차 94개: 고쳐짐 67, 남음·다시 찾음 25, 남음·안 잡힘 2(N-31·N-44)
- Codex 검수: 점수·범위·핵심 근거는 지적 없음. 낮음 3건(점수 하락 까닭 설명, 줄 번호 셋, M-01 고쳐짐 범위)은 수용해 문서에 반영했다(보고서 §5).
- 남은 결정: 3차를 할지, 하면 어디부터인지(R-M-03, 기준별 낮음, 근본 원인 묶음 B1~B6).

### 3.13 R-M-03 고침 (2026-11-05, 사용자 "R-M-03만 고쳐", Unity 확인 문제 없음 — 사용자, 새 시험 없음, 커밋 전)

- 아티팩트 외형 프리팹을 필수로 했다.
  - DB: `ArtifactViewData.Validate`가 늘 `ViewPrefabRules.RequirePrefabAsset`를 부른다(비면 DB를 만들 때 예외). SO 툴팁도 필수로.
  - View: `ArtifactPopupItem` 생성자가 프리팹 null이면 예외(기본값 삭제, 부르는 곳은 `ArtifactBoardRefresher` 하나), `ArtifactPopupView.Render`의 `continue` 삭제, `ArtifactDragDisplay.Prepare`는 null이면 예외·`Begin`의 null return 삭제, `ArtifactBoardRefresher.GetModel` 주석.
- 에셋 변경 없음: 지금 외형 셋(`HealingChalice`·`SoulDrop`·`ThornArmor`) 모두 프리팹이 있다.
- 남은 결정은 3차 여부뿐이다(기준별 낮음, 근본 원인 묶음 B1~B6).

## 4 다른 작업자에게 넘길 것

- **전달하지 않음(2026-11-05 사용자 "이정도면 두명한테 알릴정도는 아닌거 같아")**: 아래 R2-a~R2-c 항목은 기록으로만 남긴다. R2-d에서 `_audio` 사본을 걷을 때 다시 판단한다.
- **현강님**(전달 안 함, 기록만)
  - R2-a에서 현강님이 쓴 줄 두 곳에 인자를 덧붙였다.
    - `LobbyViewServices.cs` 생성자 서명 끝(`IAudioSystemService audio`) 뒤에 `ILobbyProgressReader progress`
    - `LobbyViewSystem.cs`의 `audio: global.AudioServiceCommand` 뒤에 `progress: lifeCycles.LobbyProgress`
  - 배경음 동작은 그대로다. 다만 이제 배경음은 로비 연결 시작이 끝까지 성공했을 때만 튼다(N-12).
  - R2-d에서 `LobbyViewSystem._audio` 사본을 정리하면 함께 알린다.
  - R2-c(N-55)에서 현강님이 쓴 로비 말 이동 소리 줄(`LobbyInteraction`의 `_audio.Play(UnitEvent.UnitMovement, …Base)`, `LobbyViewInput`의 `_services.Audio` 인자)을 새 `LobbyPieceMover`로 옮겼다. 소리 프리셋·시점(실제로 이동을 시작할 때 한 번)은 그대로다.
  - R2-c(N-57)에서 `ArtifactPopupUI`가 계약 넷(표시·수명·영역 판정)을 구현하도록 바꿨다. 닫기 수신자는 `IArtifactPopupCloseReceiver.CloseClicked()`로 바뀌었다. 현강님이 쓴 줄은 고치지 않았다.
- **민형님**(전달 안 함, 기록만)
  - Factory 연결 줄 `Factory/Runtime/InGame/View/LobbySceneFactory.cs`(호진 줄)에서 `ArtifactPopupViewFactory` 생성 인자 끝에 `services.IsArtifactStopped`를 더했다.
  - R2-c에서 같은 파일을 바꿨다(호진 줄). 생성 계약은 `FindPopups`(팝업 Factory 목록: 덱 → 아티팩트)와 `Create`로 나뉜다. `ArtifactPopupViewFactory`는 `(faults, services.Artifact)`만 받는다. 서비스 키·그룹은 바꾸지 않았다.
  - 서비스 키·그룹은 바꾸지 않았다. 기존 `LifeCycleSystemGroup.LobbyProgress`를 읽는다.
- **스테이지 맵 쪽 영향**(공용 부품, 호진 영역)
  - `PopupInputSession.Begin`의 null 반환과 `PopupViewScope`의 열기 중단은 상점·회복·덱 편집 팝업에도 적용된다.
  - 가림 판정은 노드 호버(`StageMapPointerBlocker`)에도 적용된다.
