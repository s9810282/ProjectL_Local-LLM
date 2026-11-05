# 로비 View 평가 설계 — 13기준 100점

- 작성자: 호진(이호진)
- 상태: 평가 실행 완료(2026-11-05, 기준 판 `fc02777aa`). 결과는 [보고서](2026-11-05-lobby-view-evaluation-Hojin.md)에 있다. 설계와 다른 점은 셋이다.
  - 실행은 21회였다. 찾기 12, 합치기 1, 검증 7, 문제 목록 정리 1이다.
  - 중간 이상 후보는 판정자를 갈린 셋에만 두었다.
  - 검토자에게 `CLAUDE.md`가 자동으로 들어갔다(보고서 §1 독립성).
- 결정: 2026-11-05 사용자 "로비만, 권장대로". 범위는 로비만으로 하고, 기준은 전투와 같은 12기준에 UI 규약을 더한 13기준으로 한다. 기준 판은 LB-a 커밋이고, 규모는 전투 절차를 줄여서 쓴다.
- 참고: 전투 쪽 평가 `032608bf6`(`origin/feature/Yonggeun/Refector-Stage`)의 `docs/superpowers/specs/2026-11-04-combat-view-evaluation.md`. 기준·감점표·절차를 거기서 가져와 로비 크기에 맞게 줄였다.

## 1 목적

- 로비 코드가 SSOT·SOLID·JMH Framework 지침·UI 규약 등을 지키는지 점수로 잰다. 그 문제 목록으로 다음 리팩토링 설계를 쓴다.
- 같은 기준 판·같은 판정 자료로 다시 계산하면 같은 점수가 나와야 한다. 그래야 리팩토링 전후를 비교할 수 있다.
- **코드는 바꾸지 않는다.** 정적 검토이고, 컴파일·Unity 실행·Profiler 측정은 하지 않는다.

## 2 범위

### 2.1 점수에 들어가는 것 (124개 파일, 8,778줄)

호진이 만들거나 손댄 로비 코드다. 다른 사람이 뒤에 고친 줄도 이 파일 안에 있으면 범위에 넣는다(2026-11-05 사용자 기준). 줄 수는 2026-11-05 작업 트리 기준이고, 기준 판이 정해지면 다시 센다.

| 묶음 | 파일 | 줄 |
|---|---|---|
| 로비 View `ViewSystem/Lobby/**`(덱 포함) | 24 | 1,893 |
| 아티팩트 팝업 `ViewSystem/ArtifactPopup/**` | 18 | 1,856 |
| 공용 팝업 수명·체스 말 `ViewSystem/Common/*.cs`(ScrollPanel 제외) | 13 | 1,173 |
| 로비·아티팩트 입력 `ViewSystem/LobbyInput/**`·`ArtifactInput/**` | 13 | 545 |
| 로비 UI 화면 `UISystem/Screens/Lobby/*`(Clicker 제외)·아티팩트 UI `Screens/Stage/*Artifact*` | 11 | 916 |
| 로비 View DB 입력 `Data/View/Table/LobbyVisual/**`·`Table/Artifact/**`·조회 표 셋 | 23 | 1,052 |
| 로비 LifeCycle `LifeCycles/Lobby/**`(출발·아티팩트 Plan·Job) | 13 | 805 |
| Buffer `BufferSystems/LobbyView/*`·`ArtifactView/*` | 6 | 476 |
| 연결: Factory 한 개, 서비스 키 둘(`LobbyInputKey`·`ArtifactInputKey`) | 3 | 62 |

파일 하나하나는 부록 A에 있다.

### 2.2 범위 밖

- 호진이 손대지 않은 것은 범위에서 뺀다(감점 안 함):
  - 옛 로비 화면 `Screens/Lobby/Clicker.cs`(현강님, 참조 없음, 지우지 않음)
  - `Common/ScrollPanel/*` 넷(민형님)
  - `ServiceKeys/Runtime/Buffer/LobbyViewBufferKey.cs`(민형님)
- 기반이라 감점하지 않는 것:
  - Framework, GlobalSystem 내부(UISystem Base·InputSystem·Render·Audio)
  - Core
  - 공용 Factory 조립, 스테이지 맵·전투 등 다른 씬 코드, 시험
- 기반이 강제한 한계는 감점하지 않는다. 범위 안 코드가 그 한계를 흡수하면서 사본이나 우회를 만들면, 그 몫만 깎는다.

## 3 기준 13개

C1~C12는 전투 평가와 같고, C13을 더했다. C1~C7은 사용자가 요청한 기준이고, C8~C12는 전투 평가가 추천한 기준이다.

| # | 기준 | 무엇을 보나 |
|---|---|---|
| C1 | SSOT | 같은 판정·식·상태·상수가 두 곳 이상에 있는가 |
| C2 | SOLID 5원칙 | 책임이 하나인가, 확장이 수정 없이 되는가, 계약이 좁고 구체 타입에 기대지 않는가 |
| C3 | JMH Framework 지침·FW 계약 | FW-001~016, 서비스 등록·Export·조회 경로, Job·Plan·LifeCycle 계약, 데이터 경계(View는 InGame DB를 읽지 않음 등) |
| C4 | 기능 변경의 독립성 | 한 기능을 바꿀 때 관련 없는 기능을 읽거나 고치거나 검증해야 하는가(변경 시나리오로 확인) |
| C5 | 불필요한 반복 작업·일일이 검사 | 이미 보장된 것을 다시 검사, 늘 넘기는 인자를 선택으로, 죽은 갈래 |
| C6 | C#·Unity 권장 구조 | 직렬화·MonoBehaviour 수명·`GetComponent`·네이밍·예외 형식 관례 |
| C7 | 불필요한 매 프레임 반복 | Tick·Update마다 바뀌지 않은 것을 다시 계산하거나 그림 |
| C8 | 캡슐화·정보 은닉 | 내부 부품을 그대로 내놓아 바깥이 단계를 조합하는가 |
| C9 | 수명·자원 관리 안전성 | 연결·해제·대여·풀·씬 전환 순서, 재진입, 끝난 연결의 늦은 콜백 |
| C10 | 정확성·오류 처리 | 실제로 틀리게 동작하는 경로, F1 오류 경계 우회, 조용히 묻히는 오류 |
| C11 | 핫패스 힙 할당 | 포인터 이동·Tick·호버마다의 할당 |
| C12 | 주석·문서 정합성 | 낡은 설명, 설계 절 참조가 틀린 곳 |
| C13 | **UI 규약** (로비 추가) | `Assets/AGENTS.md`의 UI 규약을 보는 기준이다. 아래 넷을 본다 |

C13이 보는 넷:
- 입력: `UIInputBinding`·`IUIInputReceiver`와 포인터 인터페이스로 직접 호출하는가. `onClick` 리스너·UnityEvent·대리자 연결을 쓰지 않는가. 고정 핀·호버 규약을 지키는가.
- UIBase 등록: 등록 단위·UIId·Policy가 맞는가. 열기·닫기가 UISystem을 거치는가. 정리를 `OnUIOpened`/`OnUIHidden`에서 하는가.
- 서비스 조회: Global Context 경로로 조회하는가.
- 예외: 필수 구성 누락을 예외로 던지는가.

- C13을 따로 둔 까닭: 로비는 UI 화면·팝업 비중이 크다. C3(FW 계약) 하나에 묶으면 UI 규약 문제가 묻힌다.
- 문제 하나는 주된 기준 하나에만 둔다. UI 서비스 등록·Export 경로 문제는 C3, UI 화면 쪽 규약은 C13으로 센다.

## 4 감점표·점수

- 기준마다 100점에서 깎는다. 높음 −20, 중간 −7.5, 낮음 −2.5.
- 낮음은 기준마다 −10까지만 깎는다.
- 같은 근본 원인에서 온 것은 기준 안에서 −15까지 깎는다. 높음 하나는 그대로 −20이다.
- 심각도 정의(전투 평가와 같음):
  - **높음:** 지금 운영에서 틀리게 동작하거나 멈춘다. 또는 구조 위반이 커서 그 종류의 변경마다 여러 영역을 함께 고쳐야 한다.
  - **중간:** 다음 넷 중 하나다.
    - 조건부 결함
    - 실수하기 쉬운 사본·우회·넓은 의존
    - 조용히 묻히는 오류
    - 자주 도는 경로의 의미 있는 낭비
  - **낮음:** 읽기·유지 비용, 국소 위반.
- 평균은 셋을 낸다.
  - 요청 7기준(C1~C7) 평균
  - 12기준(C1~C12) 평균: 전투 평가와 같은 구성이라 참고로 견줄 수 있다. 다만 범위·깊이가 달라 직접 비교하지 않는다.
  - 13기준(C1~C13) 평균: 로비의 대표 점수.

## 5 절차

전투는 찾기 41명·보강 4명·검증 약 200명이었다. 로비는 범위가 약 1/7이라 줄인다. 실행은 약 30~40회로 본다.

1. **찾기 12명**
   - **슬라이스 6명:** 범위 파일을 묶음별로 나눠 **끝까지 읽는다**. 다 읽었는지 파일 목록으로 확인한다.

     | 슬라이스 | 묶음 | 줄 |
     |---|---|---|
     | S1 | 로비 View(덱 포함) | 1,893 |
     | S2 | 아티팩트 팝업 | 1,856 |
     | S3 | 공용 팝업 수명·체스 말 + 로비·아티팩트 입력 | 1,718 |
     | S4 | 로비 UI 화면·아티팩트 UI + 연결(Factory·서비스 키) | 978 |
     | S5 | 로비 View DB 입력·조회 표 | 1,052 |
     | S6 | 로비 LifeCycle + Buffer | 1,281 |

   - **렌즈 6명:** 범위 전체를 관점 하나로 훑는다.
     - L1 SSOT: 규칙·상태·상수 사본
     - L2 SOLID·변경 독립성: 변경 시나리오로 확인한다. 예: 시설 하나 더하기, 팝업 하나 더하기, 출발 규칙 바꾸기, 아티팩트 판 크기 바꾸기, 말 모델 바꾸기
     - L3 FW 계약 + UI 규약
     - L4 정확성·수명·오류 경계(결함 사냥)
     - L5 매 프레임·핫패스 할당·불필요한 반복 검사
     - L6 캡슐화·C#/Unity 관례·주석 정합성·죽은 코드
   - 모두 위치·내용·실패 시나리오·고칠 방향·기준·심각도를 적는다.

2. **합치기 1~2명.** 같은 문제를 묶고, 기준을 넘는 중복은 주된 기준 하나로 정한다.

3. **적대적 검증**
   - 중간 이상 후보는 증거·심각도 검증자와 반론 검증자가 따로 판정한다. 둘이 갈리면 판정자 한 명을 더 두고, 셋의 가운데 값을 쓴다.
   - 낮음 후보는 묶어서 검증자 1~2명이 판정한다. 중간 이상으로 올린 것은 위의 검증을 다시 거친다.

4. **점수.** §4 감점표로 계산한다. 판정 자료(문제 목록과 최종 심각도)를 문서에 남겨 다시 계산할 수 있게 한다.

**독립성**
- 검토자에게 `CLAUDE.md`의 현재 상태 서술, 과거 평가·검토 문서, git 기록을 근거로 쓰지 못하게 한다. 이번 LB-a·10-26 검사 후속 정리 설계 같은 것이다.
- 근거는 지금 코드와 규약 문서다: `framework-programmer-guide.md`, `Assets/AGENTS.md`, `CLAUDE.md`의 코딩 규칙·DB 규칙 절.
- 검토자 프롬프트에 `CLAUDE.md`가 자동으로 들어가는지는 실행 때 확인해 보고서에 적는다.
- 모든 검토자는 읽기 전용이다.

## 6 기준 판

- **LB-a를 커밋한 뒤의 커밋 하나**다. 지금은 LB-a가 커밋 전이라 실행을 기다린다.
- 커밋 해시를 보고서 머리에 적는다. 검토자는 그 판의 파일을 본다. 작업 트리에 다른 변경이 섞이지 않게, 실행 전에 범위 파일이 기준 판과 같은지 확인한다.

## 7 산출물

- 보고서: `docs/superpowers/specs/<실행일>-lobby-view-evaluation-Hojin.md`
  - 결과 표: 기준별 점수와 중간·낮음 수, 깎인 까닭
  - 방법
  - 중간 이상 문제 전체
  - 기반 한계(감점 없음)
- 문제 목록: `docs/superpowers/specs/<실행일>-lobby-view-evaluation-issues-Hojin.md`
  - 검증을 통과한 문제 전부
  - 위치·내용·실패 시나리오·고칠 방향·기준·검증 판정
- 평가 끝에 Phase 끝 Codex 검수를 받는다. 검수 대상은 보고서의 근거와 점수 계산이다.
- 평가 결과로 로비 리팩토링 설계(2차)를 쓴다. 평가 단계에서는 코드를 바꾸지 않는다.

## 8 열린 것

- 지금 아는 문제(로비 리팩토링 설계 §6 범위 밖 목록, 배경음 서비스 사본 등)는 검토자에게 미리 알려 주지 않는다. 평가 뒤 보고서의 문제 목록과 대조해 빠진 것이 있으면 보고서에 적는다.

## 부록 A 범위 파일 (124개)

경로 머리는 `Assets/ProjectL/Script/Systems/`다. 줄 수는 2026-11-05 작업 트리 기준이다.

| 파일 | 줄 |
|---|---|
| `DataBaseSystem/Data/View/Table/Artifact/ArtifactPopupConfigDataTableSO.cs` | 8 |
| `DataBaseSystem/Data/View/Table/Artifact/ArtifactViewDataTableSO.cs` | 14 |
| `DataBaseSystem/Data/View/Table/Artifact/Item/ArtifactPopupConfigData.cs` | 39 |
| `DataBaseSystem/Data/View/Table/Artifact/Item/ArtifactPopupConfigDataSO.cs` | 31 |
| `DataBaseSystem/Data/View/Table/Artifact/Item/ArtifactViewData.cs` | 51 |
| `DataBaseSystem/Data/View/Table/Artifact/Item/ArtifactViewDataSO.cs` | 44 |
| `DataBaseSystem/Data/View/Table/LobbyVisual/Item/LobbyBoardData.cs` | 70 |
| `DataBaseSystem/Data/View/Table/LobbyVisual/Item/LobbyBoardDataSO.cs` | 55 |
| `DataBaseSystem/Data/View/Table/LobbyVisual/Item/LobbyCharacterPresentationData.cs` | 24 |
| `DataBaseSystem/Data/View/Table/LobbyVisual/Item/LobbyCharacterPresentationDataSO.cs` | 16 |
| `DataBaseSystem/Data/View/Table/LobbyVisual/Item/LobbyFacilityHoverData.cs` | 89 |
| `DataBaseSystem/Data/View/Table/LobbyVisual/Item/LobbyFacilityHoverDataSO.cs` | 27 |
| `DataBaseSystem/Data/View/Table/LobbyVisual/Item/LobbyFacilityKind.cs` | 10 |
| `DataBaseSystem/Data/View/Table/LobbyVisual/Item/LobbyFacilityView.cs` | 38 |
| `DataBaseSystem/Data/View/Table/LobbyVisual/Item/LobbyModelData.cs` | 39 |
| `DataBaseSystem/Data/View/Table/LobbyVisual/Item/LobbyModelDataSO.cs` | 23 |
| `DataBaseSystem/Data/View/Table/LobbyVisual/Item/LobbyMotionData.cs` | 43 |
| `DataBaseSystem/Data/View/Table/LobbyVisual/Item/LobbyMotionDataSO.cs` | 17 |
| `DataBaseSystem/Data/View/Table/LobbyVisual/Item/LobbyVisualPrefabRules.cs` | 187 |
| `DataBaseSystem/Data/View/Table/LobbyVisual/LobbyVisualDataTableSO.cs` | 11 |
| `DataBaseSystem/Queries/ViewDataBase/ViewTable/ArtifactPopupConfigTable.cs` | 33 |
| `DataBaseSystem/Queries/ViewDataBase/ViewTable/ArtifactViewTable.cs` | 88 |
| `DataBaseSystem/Queries/ViewDataBase/ViewTable/LobbyVisualTable.cs` | 95 |
| `Factory/Runtime/InGame/View/LobbySceneFactory.cs` | 36 |
| `RuntimeSystem/BufferSystems/ArtifactView/ArtifactViewBuffer.cs` | 134 |
| `RuntimeSystem/BufferSystems/ArtifactView/ArtifactViewCommand.cs` | 105 |
| `RuntimeSystem/BufferSystems/ArtifactView/ArtifactViewCommandKind.cs` | 11 |
| `RuntimeSystem/BufferSystems/ArtifactView/ArtifactViewPreview.cs` | 48 |
| `RuntimeSystem/BufferSystems/LobbyView/LobbyDepartureRequest.cs` | 130 |
| `RuntimeSystem/BufferSystems/LobbyView/LobbyViewBuffer.cs` | 48 |
| `RuntimeSystem/GlobalSystem/UISystem/Screens/Lobby/LobbyBackgroundCameraBinding.cs` | 58 |
| `RuntimeSystem/GlobalSystem/UISystem/Screens/Lobby/LobbyBackgroundView.cs` | 33 |
| `RuntimeSystem/GlobalSystem/UISystem/Screens/Lobby/LobbyCanvasView.cs` | 38 |
| `RuntimeSystem/GlobalSystem/UISystem/Screens/Lobby/LobbyDeckCardContent.cs` | 20 |
| `RuntimeSystem/GlobalSystem/UISystem/Screens/Lobby/LobbyDeckDisplayModel.cs` | 24 |
| `RuntimeSystem/GlobalSystem/UISystem/Screens/Lobby/LobbyDeckListItemView.cs` | 57 |
| `RuntimeSystem/GlobalSystem/UISystem/Screens/Lobby/LobbyDeckUI.cs` | 285 |
| `RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/ArtifactPoolItemContent.cs` | 26 |
| `RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/ArtifactPoolItemView.cs` | 74 |
| `RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/ArtifactPopupUI.cs` | 279 |
| `RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/IArtifactPopupDisplay.cs` | 22 |
| `RuntimeSystem/InGameSystem/ViewSystem/ArtifactInput/ArtifactInputController.cs` | 69 |
| `RuntimeSystem/InGameSystem/ViewSystem/ArtifactInput/ArtifactInputFactory.cs` | 31 |
| `RuntimeSystem/InGameSystem/ViewSystem/ArtifactInput/ArtifactInputKeyBindings.cs` | 17 |
| `RuntimeSystem/InGameSystem/ViewSystem/ArtifactInput/ArtifactInputLayers.cs` | 13 |
| `RuntimeSystem/InGameSystem/ViewSystem/ArtifactInput/ArtifactInputNames.cs` | 16 |
| `RuntimeSystem/InGameSystem/ViewSystem/ArtifactInput/ArtifactInputSlot.cs` | 116 |
| `RuntimeSystem/InGameSystem/ViewSystem/ArtifactInput/IArtifactInputReceiver.cs` | 23 |
| `RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactBoardMapper.cs` | 43 |
| `RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactBoardRefresher.cs` | 95 |
| `RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactDragController.cs` | 280 |
| `RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactDragDisplay.cs` | 141 |
| `RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactInteraction.cs` | 94 |
| `RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactPointerMapper.cs` | 77 |
| `RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactPopupController.cs` | 432 |
| `RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactPopupDisplay.cs` | 46 |
| `RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactPopupOutputMask.cs` | 78 |
| `RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactPopupStatus.cs` | 137 |
| `RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactPopupText.cs` | 32 |
| `RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactPopupView.cs` | 202 |
| `RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactPopupViewFactory.cs` | 84 |
| `RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactPopupViewScreen.cs` | 31 |
| `RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/ArtifactSceneRoot.cs` | 38 |
| `RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/IArtifactDragPresenter.cs` | 15 |
| `RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/IArtifactPopupOutput.cs` | 9 |
| `RuntimeSystem/InGameSystem/ViewSystem/ArtifactPopup/IArtifactPopupPresenter.cs` | 22 |
| `RuntimeSystem/InGameSystem/ViewSystem/Common/ChessPieceClone.cs` | 35 |
| `RuntimeSystem/InGameSystem/ViewSystem/Common/ChessPieceLiftPath.cs` | 57 |
| `RuntimeSystem/InGameSystem/ViewSystem/Common/ChessPieceMotion.cs` | 316 |
| `RuntimeSystem/InGameSystem/ViewSystem/Common/ChessPieceMotionSettings.cs` | 26 |
| `RuntimeSystem/InGameSystem/ViewSystem/Common/ChessPiecePath.cs` | 38 |
| `RuntimeSystem/InGameSystem/ViewSystem/Common/IPopupViewController.cs` | 12 |
| `RuntimeSystem/InGameSystem/ViewSystem/Common/IPopupViewHost.cs` | 11 |
| `RuntimeSystem/InGameSystem/ViewSystem/Common/IPopupViewScreen.cs` | 14 |
| `RuntimeSystem/InGameSystem/ViewSystem/Common/PopupInputSession.cs` | 163 |
| `RuntimeSystem/InGameSystem/ViewSystem/Common/PopupViewScope.cs` | 364 |
| `RuntimeSystem/InGameSystem/ViewSystem/Common/ScreenUIPointerBlocker.cs` | 48 |
| `RuntimeSystem/InGameSystem/ViewSystem/Common/ViewAttachmentInputLayers.cs` | 34 |
| `RuntimeSystem/InGameSystem/ViewSystem/Common/ViewCameraHandleLease.cs` | 55 |
| `RuntimeSystem/InGameSystem/ViewSystem/Lobby/Deck/LobbyDeckPopupController.cs` | 80 |
| `RuntimeSystem/InGameSystem/ViewSystem/Lobby/Deck/LobbyDeckPopupViewScreen.cs` | 21 |
| `RuntimeSystem/InGameSystem/ViewSystem/Lobby/Deck/LobbyDeckPresenter.cs` | 114 |
| `RuntimeSystem/InGameSystem/ViewSystem/Lobby/ILobbyFacilityActions.cs` | 11 |
| `RuntimeSystem/InGameSystem/ViewSystem/Lobby/ILobbySceneFactory.cs` | 18 |
| `RuntimeSystem/InGameSystem/ViewSystem/Lobby/LobbyBoard.cs` | 96 |
| `RuntimeSystem/InGameSystem/ViewSystem/Lobby/LobbyCharacterVisual.cs` | 67 |
| `RuntimeSystem/InGameSystem/ViewSystem/Lobby/LobbyDepartureFlow.cs` | 61 |
| `RuntimeSystem/InGameSystem/ViewSystem/Lobby/LobbyDepartureText.cs` | 25 |
| `RuntimeSystem/InGameSystem/ViewSystem/Lobby/LobbyFacilityActions.cs` | 41 |
| `RuntimeSystem/InGameSystem/ViewSystem/Lobby/LobbyFacilityHighlight.cs` | 110 |
| `RuntimeSystem/InGameSystem/ViewSystem/Lobby/LobbyFacilityPicker.cs` | 47 |
| `RuntimeSystem/InGameSystem/ViewSystem/Lobby/LobbyInteraction.cs` | 180 |
| `RuntimeSystem/InGameSystem/ViewSystem/Lobby/LobbyRoomBinding.cs` | 21 |
| `RuntimeSystem/InGameSystem/ViewSystem/Lobby/LobbySceneLayout.cs` | 34 |
| `RuntimeSystem/InGameSystem/ViewSystem/Lobby/LobbySceneRoot.cs` | 35 |
| `RuntimeSystem/InGameSystem/ViewSystem/Lobby/LobbyViewHopPath.cs` | 154 |
| `RuntimeSystem/InGameSystem/ViewSystem/Lobby/LobbyViewInput.cs` | 71 |
| `RuntimeSystem/InGameSystem/ViewSystem/Lobby/LobbyViewPopups.cs` | 89 |
| `RuntimeSystem/InGameSystem/ViewSystem/Lobby/LobbyViewScene.cs` | 92 |
| `RuntimeSystem/InGameSystem/ViewSystem/Lobby/LobbyViewScope.cs` | 136 |
| `RuntimeSystem/InGameSystem/ViewSystem/Lobby/LobbyViewServices.cs` | 87 |
| `RuntimeSystem/InGameSystem/ViewSystem/Lobby/LobbyViewSystem.cs` | 231 |
| `RuntimeSystem/InGameSystem/ViewSystem/Lobby/LobbyVisualSet.cs` | 72 |
| `RuntimeSystem/InGameSystem/ViewSystem/LobbyInput/ILobbyInputReceiver.cs` | 14 |
| `RuntimeSystem/InGameSystem/ViewSystem/LobbyInput/LobbyInputController.cs` | 80 |
| `RuntimeSystem/InGameSystem/ViewSystem/LobbyInput/LobbyInputFactory.cs` | 31 |
| `RuntimeSystem/InGameSystem/ViewSystem/LobbyInput/LobbyInputLayers.cs` | 15 |
| `RuntimeSystem/InGameSystem/ViewSystem/LobbyInput/LobbyInputNames.cs` | 13 |
| `RuntimeSystem/InGameSystem/ViewSystem/LobbyInput/LobbyReceiverSlot.cs` | 107 |
| `RuntimeSystem/LifeCycleSystem/LifeCycles/Lobby/Artifact/ArtifactEditFacts.cs` | 35 |
| `RuntimeSystem/LifeCycleSystem/LifeCycles/Lobby/Artifact/Jobs/ArtifactCommandJob.cs` | 34 |
| `RuntimeSystem/LifeCycleSystem/LifeCycles/Lobby/Artifact/Jobs/ArtifactPreviewJob.cs` | 34 |
| `RuntimeSystem/LifeCycleSystem/LifeCycles/Lobby/Artifact/Jobs/LobbyArtifactCheckJob.cs` | 47 |
| `RuntimeSystem/LifeCycleSystem/LifeCycles/Lobby/Artifact/LobbyArtifactDomain.cs` | 140 |
| `RuntimeSystem/LifeCycleSystem/LifeCycles/Lobby/Artifact/Plans/ArtifactPlan.cs` | 22 |
| `RuntimeSystem/LifeCycleSystem/LifeCycles/Lobby/Jobs/LobbyRunStartOutcome.cs` | 27 |
| `RuntimeSystem/LifeCycleSystem/LifeCycles/Lobby/Jobs/LobbyTransitionJob.cs` | 55 |
| `RuntimeSystem/LifeCycleSystem/LifeCycles/Lobby/LobbyLifeCycle.cs` | 103 |
| `RuntimeSystem/LifeCycleSystem/LifeCycles/Lobby/LobbyProgress.cs` | 72 |
| `RuntimeSystem/LifeCycleSystem/LifeCycles/Lobby/LobbyProgressFacts.cs` | 54 |
| `RuntimeSystem/LifeCycleSystem/LifeCycles/Lobby/Plans/LobbyPlan.cs` | 147 |
| `RuntimeSystem/LifeCycleSystem/LifeCycles/Lobby/Plans/LobbyStep.cs` | 35 |
| `ServiceKeys/Runtime/ArtifactInputKey.cs` | 12 |
| `ServiceKeys/Runtime/LobbyInputKey.cs` | 14 |
