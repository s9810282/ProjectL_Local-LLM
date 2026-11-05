# 아티팩트 팝업 설계

- 작성자: 호진(이호진)
- 상태: 구현 완료, 로비 4번 시설에서 운영 중. 남은 것은 사람의 마우스 Play, Player 성능 측정, 외부 공유 전달이다([계획 §2](../plans/2026-10-05-artifact-popup-plan-Hojin.md#2-남은-일)).
- 범위: 로비 아티팩트 팝업의 표시, 배치·이동·반환·회전 조작, UI 연결, 배경·카메라, 수명·오류 정리. Core의 배치 규칙·효과 발동, 보상 보드 확장은 다루지 않는다.
- 계획: [아티팩트 팝업 계획](../plans/2026-10-05-artifact-popup-plan-Hojin.md)
- 안내: [아티팩트 안내](../viewDocs/artifact-guide-Hojin.md)(UI Hierarchy·Inspector, Buffer API, 조작 표)
- 정리: 2026-10-27 이 문서의 1부(배치·회전·반환 설계)와 2부(UI 이전 결정 기록, 10-24에 합친 판)를 지금 구조 중심으로 다시 썼다. 계획에 있던 결정 D1~D19·Q1도 §3으로 옮겼다. 옛 판은 git `10914570`.

## 1 목적과 범위

로비 시설에 도착하면 팝업을 연다. 하단 보유 목록에서 배치판으로 끌어 배치한다. 배치한 항목은 옮기고 돌리고 하단 목록으로 되돌릴 수 있다. 배경은 로비 화면을 가리고 모델 뒤에 그린다.

지킬 조건은 넷이다.

1. 기존 Framework 구조를 먼저 지킨다.
2. Core 코드를 임의로 고치지 않는다. SA P7의 Core·Buffer 번호 계약과 직접 조회 보완은 사용자가 승인해 로컬에서 구현했다(외부 합의와는 별개다).
3. 전투 공용 경계를 임의로 바꾸지 않는다. P3.9 공용 오류 연결은 사용자 승인으로 구현했다(전투 작업자 전달은 별개다).
4. GTX 1060에서 250~300 FPS는 목표다. 검증 결과가 아니다. CPU·해상도·렌더 조건을 적은 Player 빌드에서 확인해야 한다.

바뀐 것: 옛 StageSelect 화면의 열기 버튼 → 지금 로비 4번 시설(LV5, 2026-10-13). StageSelect에는 아티팩트 진입점·팝업·배치판·전용 카메라가 없다.

## 2 지금 구조

### 2.1 수명 소유

| 객체 | 맡는 일 |
|---|---|
| `LobbyViewSystem` | Framework 수명, 서비스·DB·슬롯 참조 받기, `ArtifactPopupViewFactory` 조립 |
| `LobbyViewScope` | 씬 연결 하나의 생성·Tick·폐기. 연결 때 Boot Main Camera를 팝업 Factory에 넘긴다 |
| `ArtifactPopupViewFactory` | 씬 참조로 Presenter·UI·Controller를 만든다 |
| `ArtifactPopupController` | 표시·입력 수신·결과 소비, SRP 카메라 렌더 구독. `PopupViewScope`를 소유한다 |
| `PopupViewScope` + `ArtifactPopupViewScreen` | UI 열기·닫기, 열림 알림 단독 구독, 입력 Layer 수명 |
| `PopupInputSession` | 로비 연결에서 받아 덱 팝업과 공유하는 모달 입력. 열 때 `Begin(Artifact)` |
| `ArtifactSceneRoot` | Lobby 씬의 참조 묶음(`popupView`·`popupUI`). 동작은 없다. 항상 켜 둔다 |

구조·수명의 상위 원본은 [View 구조 설계](2026-10-13-view-structure-design-Hojin.md)와 [로비 설계](2026-10-11-lobby-design-Hojin.md)다. 옛 `ArtifactViewSystem`·`ArtifactPopupSession`·`StagePopupSession`은 없다.

### 2.2 기능 객체

| 객체 | 종류 | 맡는 일 | 하지 않는 일 |
|---|---|---|---|
| `ArtifactBoardMapper` | 일반 C# | 출력 Camera Ray와 WorldRoot 로컬 보드 평면의 교차 → `CellCoord`. 점유 칸으로 배치 인스턴스를 찾는다 | Collider로 모델 판별 |
| `ArtifactInteraction` | 일반 C# | 선택 대상·포인터·임시 회전·잡은 칸 오프셋만 든다 | 확정 상태·점유맵 복제, Core 계산기 호출 |
| `ArtifactDragController` | 일반 C# | 슬롯의 입력 수신자. 의도를 `ArtifactViewBuffer`에 제출하고 번호·문맥이 맞는 결과를 소비한다 | 규칙 판단 |
| `ArtifactPointerMapper` | 일반 C# | UI 영역·3D 보드·점유 칸의 대상 판정 한 곳 | — |
| `ArtifactBoardRefresher` | 일반 C# | 스냅샷이 바뀌면 View DB로 표시값을 만들어 보드·목록을 갱신 | — |
| `ArtifactPopupStatus` | 일반 C# | 문구 우선순위(대기 > 프리뷰 실패 > 마지막 편집 결과)와 3초 수명 | 새 타이머 서비스 |
| `ArtifactPopupView` | MonoBehaviour | Floor 하나·배치 모델·드래그 모델·프리뷰 칸을 그린다. `IArtifactPopupPresenter`·`IArtifactDragPresenter`를 구현한다. 프리팹별 CustomPool | Buffer·DB 읽기 |
| `ArtifactPopupUI` | `UIBase` | 보유 목록·이름·설명·상태 문구·닫기 버튼·클릭 차단. `IArtifactPopupDisplay` 구현 | Buffer·DB 읽기, 3D, Layer, Core 요청 |
| `ArtifactPopupOutputMask` | 일반 C# | 열린 동안 출력 Camera Culling Mask에 UI 층을 더하고 닫을 때 되돌린다 | Brain·Boot 카메라 직접 조작 |
| View DB `ArtifactPopupConfig` | 설정 테이블 | 보드 중심·칸 간격·칸/모델 배율·높이, 배경 색·거리·여유 배율, 프리뷰 색 | Core 논리 크기·모양·배치 규칙 |

### 2.3 입력·실행 흐름

```text
보드·보유 목록 드래그
 → InputSystem → ArtifactInputController → ArtifactInputSlot → ArtifactDragController
 → ArtifactViewBuffer (편집 명령 목록 / 최신 프리뷰 하나)
 → LobbyLifeCycle: Runner가 비면 Register(Lobby, Next)
 → LobbyPlan.Decide: 미처리 편집 → 최신 미응답 프리뷰 → 출발
 → ArtifactPlan.CreateCommand / CreatePreview + LobbyArtifactCheckJob
 → CoreGroup.ArtifactCommands: 기대 번호의 입력을 직접 다시 읽어 계산·게시
 → ArtifactBuffer → ArtifactDragController가 번호·데이터를 맞춰 표시하고 입력을 지운다
```

- Stage·Shop·Artifact·Combat은 같은 `JobRegisterSystem`·`JobRunnerSystem`을 쓴다. 아티팩트 전용 실행기는 없다.
- 쓰기 소유: `ArtifactViewBuffer`는 View, `ArtifactBuffer.Current`·`ArtifactCommandResults`는 Core다. 번호 규칙과 API는 [안내 §4](../viewDocs/artifact-guide-Hojin.md#4-입력결과-buffer)에 있다.
- 칸 좌표: `PlacedItems.Cells`는 절대 칸, `PoolItems.Cells`는 정의 로컬 칸이다. 프리뷰 `Shape.Cells`는 이미 회전된 로컬 칸이라 Anchor만 더한다. 프리뷰 성공은 재고·편집 권한을 보장하지 않는다.
- 보드 준비: Boot의 `ArtifactBoardPreparation`이 한 번 초기화한다. 처음 크기는 InGame `ArtifactBoardConfig`(4×4), 지금 크기는 늘 Buffer에서 읽는다.
- 편집 가능 여부는 Core가 정한다(`CanEdit`는 전투 보존 중인지 본다). 잠금은 정상 `LayoutLocked` 결과다.

## 3 결정 기록

D1~D19·Q1은 처음 계획(2026-10-05)에서, U0~U5는 UI 이전(2026-10-06)에서 정했다. 다른 문서가 이 ID를 부른다.

| ID | 지금 결정 | 날짜·근거, 바뀐 것 |
|---|---|---|
| D1 | UI(목록·버튼·문구·클릭 차단)는 `UIBase` 화면 `ArtifactPopupUI`, 3D 보드·모델·배경은 View | 2026-10-06 사용자. 바뀐 것: 옛 "UISystem에 등록하지 않음" → UIBase 이전(BindScene 연결 확인 뒤) |
| D2 | 열 때 모달 입력(Artifact + Common Layer)을 먼저 확보한다. 정상 닫기만 같은 로비 연결일 때 Lobby 입력을 복구한다. 외부 Hide·교체·Detach는 복구하지 않는다 | 2026-10-05. 바뀐 것: 복귀 대상 StageSelection → Lobby(LV5) |
| D3 | Job은 공용 `JobRegisterSystem`이 로비 Plan으로 만든다. Job은 기대 번호와 Core 서비스만 받는다 | 바뀐 것: 옛 "공용 등록기를 고치지 않고 요청용 Job 생성 메서드 추가" → SA P7 번호 계약, 로비 Plan(10-23) |
| D4 | 공용 EventBuse를 쓰지 않는다 | 구독 해제·예외 격리가 없다(DR-008) |
| D5 | 아이템·칸 View는 CustomPool로 재사용하고 빌려준 목록은 소유자가 관리한다 | FW-204, FW-014, DR-011 |
| D6 | 처음 보드 크기는 InGame `ArtifactBoardConfig`, 지금 크기는 Buffer. 코드에 크기 상수를 두지 않는다 | 보상으로 늘어날 수 있다 |
| D7 | 보드 초기화는 Boot `ArtifactBoardPreparation`이 한 번 한다. 이미 준비된 보드는 다시 초기화하지 않는다 | 바뀐 것: 옛 "StageLifeCycle이 진입마다 초기화 Job 등록, `IHasArtifactBoardConfig`로 설정 조회" → Boot 준비(LV5) |
| D7a | 보드 준비 전 편집은 등록하지 않고 기다린다 | 지금은 `LobbyPlan.Decide`가 맡는다 |
| D7b | 초기화 Job은 `InitializeBoard`가 false면 `IsInitialized`로 정상 완료/예외를 가른다 | `ArtifactBoardInitializeJob` |
| D8 | 프리뷰는 최신 하나만 쓴다. 현재 번호·데이터에 맞는 응답만 표시한다 | 바뀐 것: 옛 "대기 Job 최대 1개 + 실행 시점 최신 대상" → `ArtifactViewBuffer` 최신 슬롯(P7) |
| D8a | 취소된 요청은 Core가 기대 번호 입력을 다시 읽어 건너뛴다. `JobRunner.Clear()`는 쓰지 않는다 | 바뀐 것: 옛 요청 객체 취소 표시 → 번호 계약(P7) |
| D8b | 제출한 입력(`ArtifactViewCommandData`)은 바꾸지 않는다 | 바뀐 것: 옛 불변 요청 객체 → 불변 입력 데이터 |
| D9 | 편집 가능 여부(전투 보존 중 잠금 등)는 Core가 판단하고 View는 사유만 표시한다 | 유지 |
| D10 | 편집은 Buffer 목록에 쌓이고 번호 순으로 하나씩 처리된다 | 바뀐 것: 옛 "처리 중 편집 입력 무시" → 번호 순차 처리(P7) |
| D11 | 오류 경계가 오류 상태면 입력 수신·제출을 무시한다 | 입력 콜백은 Tick과 무관하게 불린다 |
| D12 | 투명 차단 Image가 로비 입력을 막되 보드 드래그 판정은 막지 않는다. 버튼·상세 위에서 뗀 드래그는 요청을 만들지 않는다. 좌표로 대상을 판정한다 | 2026-10-06 회수 실패 조사로 가려진 열기 버튼을 차단 판정에서 뺐다(지금은 열기 버튼 자체가 없다) |
| D13 | 팝업 Canvas·`ArtifactSceneRoot`는 늘 켜 두고 안쪽 Panel만 켜고 끈다 | 비활성 오브젝트는 씬 진입 때 찾지 못한다 |
| D14 | 보드·목록은 스냅샷 참조가 바뀔 때만 다시 그린다. 결과 문구는 아직 표시하지 않은 끝난 편집일 때 갱신한다 | 지금은 Refresher와 Controller가 나눠 맡는다 |
| D15 | 일반 Hide는 제출한 편집을 지우지 않고 프리뷰·임시 표현만 정리한다. Detach·StopInput은 연결을 정리한다 | 바뀐 것: 옛 `InvalidatePreview`/`CancelPending` → Controller·Scope 정리 |
| D16 | 풀은 `ArtifactPopupView`가 소유하고, 빌릴 때·돌려줄 때 아이콘·부모·활성을 초기화한다. 그리는 중 예외가 나도 빌린 것을 모두 돌려준다 | `CustomPool.Return`은 검사를 하지 않는다 |
| D17 | View DB 조회는 View 쪽(Refresher)이 하고 Presenter·UI는 받은 값만 그린다 | 바뀐 것: 옛 `ArtifactViewSystem` 조회 → Refresher |
| D18 | 보드 준비 전에는 칸을 만들지 않고 "준비 중" 문구만 보인다 | D7a |
| D19 | 칸·회전·대상이 직전과 같으면 프리뷰를 다시 요청하지 않는다. 드래그 중에는 움직이는 모델과 프리뷰만 갱신한다 | 2026-10-06, 성능 |
| Q1 | 입력·결과 경로는 `ArtifactViewBuffer` 번호 계약이다 | 바뀐 것: 2026-10-05 A(요청 객체) → SA P7 Buffer 경로 |
| U0 | 모든 UI는 현강님 UI 구조·문서를 따른다 | 2026-10-06 사용자 |
| U1 | 팝업의 UI 전체를 `UIBase` 화면 하나로 옮긴다. 3D·드래그·배경은 View에 둔다 | 2026-10-06 사용자 |
| U2 | `UIId.Artifact = 13`을 현강님 `UIIDs.cs`에 직접 한 줄 추가하고 공유한다 | 2026-10-06. 당시 12는 미병합 `Timeline`이 쓰고 있어 피했다 |
| U3 | 열고 닫기는 UISystem(`OpenUI`/`CloseUI`, 뒤로가기 이력)이 맡는다. 호출은 `PopupViewScope`가 한다 | 2026-10-06 |
| U4 | ESC로 팝업을 닫지 않는다. ESC는 `Common/OpenSettings` 전용이다 | 바뀐 것: 2026-10-06 처음 넣은 `Artifact/Close`(ESC)를 같은 날 사용자 요청으로 뺐다 |
| U5 | 열기 버튼은 없다. 로비 시설 도착이 연다 | 바뀐 것: 2026-10-06 "열기 버튼은 `ArtifactPopupUI`가 소유" → LV5에서 제거 |
| — | 우클릭은 회수다. 액션 이름 `Cancel`은 그대로 둔다 | 2026-10-06 사용자(옛: 우클릭 = 취소) |
| — | 배치면 정렬 안 B: `cellSpacing` 1.79, `modelScale` 0.8136 | 2026-10-06 사용자. 측정은 [계획 §3.4](../plans/2026-10-05-artifact-popup-plan-Hojin.md#34-배치면-정렬-2026-10-06) |
| — | 배치판 외형은 `ArtifactSpace_final.fbx` 공간 모델, Dummy 5개 제거 | 2026-10-17 사용자 요청. [계획 §3.7](../plans/2026-10-05-artifact-popup-plan-Hojin.md#37-공간-모델-교체-2026-10-17) |

## 4 조작 계약

| 조작 | 결과 |
|---|---|
| 보유 슬롯 누름 | 종류 ID를 고른다. Core `Place`는 특정 보유 인스턴스를 고르는 API가 아니므로 View가 인스턴스를 정하지 않는다 |
| 배치물 누름 | Buffer 절대 칸과 맞는 `InstanceId`와 원래 Anchor·Rotation을 받고 잡은 칸 오프셋을 지킨다 |
| 드래그 중 Q/E | 임시 `Rotation4`만 바꾸고 새 프리뷰를 요청한다. 원본 배치는 그대로다. 회전해도 Anchor 기준을 같게 둔다 |
| 보드에서 뗌 | 보유 선택이면 `Place`, 배치 선택이면 `Move` 하나. 가능 여부는 실행 결과로 정한다 |
| 배치물을 하단 반환 영역에 뗌 | `Return` 하나. 버튼·상세 UI·화면 밖 해제는 취소다(실수 반환 방지) |
| 우클릭 | 배치물을 끄는 중이면 그 인스턴스, 아니면 포인터 아래 배치물을 회수한다. 보유 항목을 끄는 중이면 임시 선택만 정리한다 |
| SelectCanceled·Layer 변경·닫기·Detach | 임시 표현·프리뷰만 정리하고 반환 요청을 만들지 않는다 |
| 호버 | 보유 슬롯(UIInputBinding)·배치물(PointerPosition)에 이름·효과 설명. 드래그 중에는 고른 항목 정보를 유지한다. Buffer 쓰기·Core 호출이 없다 |

## 5 배경·카메라·보드 정렬

- 출력 Camera는 씬 참조가 아니다. `LobbyViewScope`가 연결 때 Boot Main Camera를 넘기고, 보드 피킹과 배경 배치가 이 Camera를 쓴다. 로비 화면은 고정 가상 카메라 `LobbyCamera`가 비추고 아티팩트 보드·배경은 그 구도 안에 있다. 팝업 열기마다 카메라를 찾지 않는다.
- 배경은 출력 Camera 시야를 모두 덮는 World Space 표현이다. Orthographic·Perspective를 나누고, 열 때와 열린 동안 pose·lens·aspect가 바뀌면 다시 맞춘다. 배경은 보드 모델보다 뒤에 있어야 한다(Near/Far Clip과 모델 깊이 확인).
- 카메라 최종 갱신과 Framework Tick 순서가 달라 배경이 한 프레임 늦을 수 있다. 그래서 Controller가 SRP `RenderPipelineManager.beginCameraRendering`을 Show에서 구독하고 Hide·Dispose에서 푼다. 지금 출력 카메라·열린 팝업만 갱신한다. 바뀐 것: 옛 Built-in `Camera.onPreCull` → SRP 이벤트. Framework LateTick·전역 갱신 API는 더하지 않았다.
- 열 때 WorldRoot·배경을 켜고 로비 표시(`lobbyRoot`)를 숨긴다. 닫을 때 되돌린다. 열린 동안만 `ArtifactPopupOutputMask`가 Culling Mask에 UI 층을 더한다. 마스크는 Framework 카메라 카탈로그의 공개 손잡이로만 바꾼다.
- 배치 면은 모델 bounds가 아니라 설정의 보드 중심·높이·간격이다. 클릭·모델·프리뷰가 같은 Mapper 좌표를 쓴다. 지금 값은 `cellSpacing` 1.79, `modelScale` 0.8136, `modelHeight` 0이다(`ArtifactPopupConfig_Default`). 공간 모델은 중앙 4×4 배치면의 한 변 7.16·중심 (0,0,0)·높이 0을 지키도록 맞췄다.

## 6 UI 연결 계약

`ArtifactPopupUI`는 `UIBase`·`IUIInputReceiver`·UI 소유 계약 `IArtifactPopupDisplay`를 구현한다. Hierarchy·Inspector 표는 [안내 §2](../viewDocs/artifact-guide-Hojin.md#2-artifactpopupui-연결)에 있다.

| 계약 멤버 | 뜻 |
|---|---|
| `OpenStateChanged(bool)` | 실제 Open/Hide를 같은 호출 안에서 한 번 알리는 수명 알림. 버튼 입력 이벤트가 아니다. `PopupViewScope`만 구독한다 |
| `BindClose(IUIInputReceiver)` | 닫기 버튼을 지금 Controller의 닫기 수신자에 잇는다 |
| `SetCanOpen(bool)` | 이름은 옛것을 유지한 입력 허용. 닫기 버튼 interactable과 Click/Submit 허용을 정한다 |
| `RenderPool`·`ShowDetail`·`ShowStatus` | 받은 값만 그린다 |
| `TryPickPool`·`IsReturnArea`·`IsBlockingArea` | UI 영역 화면 좌표 판정만 한다 |

- 루트는 늘 켜 두고 Open/Hide는 자식 Panel만 켜고 끈다. UISystem은 `BindScene`·`Initialize` 때 열리지 않은 UI를 `Hide()`하므로, 루트를 끄면 안 되고 `Hide`는 여러 번 불려도 안전해야 한다.
- 알림 안에서는 표시만 바꾸고 UISystem·입력 Layer를 다시 부르지 않는다. 외부 Hide/Back의 Layer 반납과 외부 Open 인수는 다음 Tick에 한다. Start 때 이미 열린 화면은 알림 밖에서 인수한다.
- 버튼은 `UIInputBinding`·`IUIInputReceiver`로만 받는다. `Button.onClick.AddListener`·Inspector UnityEvent를 쓰지 않는다(`Assets/AGENTS.md`).
- `GetUIData`·`BindUIService`는 인자 null 검사만 한다. UI는 Buffer·DB를 읽거나 매 프레임 폴링하지 않는다.
- 바뀐 것: 옛 `IArtifactPopupScreen`(View → UI)·`IArtifactPopupHost`(UI → View)와 Connect/Disconnect 경로, UI의 Buffer·DB 직접 조회는 SA P3.10에서 `IArtifactPopupDisplay`와 Refresher 표시값으로 바뀌었다. 다시 만들지 않는다.
- 네임스페이스는 폴더 경로 `ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage`다(`StageUI`와 같다).

## 7 수명·정리·오류

- 일반 Hide: 프리뷰·임시 표현·카메라 렌더 구독을 정리한다. 제출한 편집은 지우지 않는다. 닫힌 동안에도 Controller Tick이 맞는 결과를 소비해 문구를 보관하고 입력을 정리한다.
- 명시 닫기: `CloseUI`가 돌아온 뒤 같은 로비 연결·정상 상태일 때만 Lobby Layer와 시설 입력을 복구한다. 다른 UI가 Layer를 가져갔으면 그 Layer를 지킨다.
- Detach·StopInput·Shutdown: 로비 Scope와 Controller가 허용·수명 구독·수신자·슬롯·입력·렌더 구독을 푼다. 씬 이탈 중에는 로비 Layer를 다시 켜지 않는다. 이전 연결의 늦은 콜백은 수명 표식으로 막는다.
- 공용 오류: `RuntimeFaultBoundary`는 정리 콜백 하나만 받는다. `InGameViewSystemFactory`가 `InGameViewFaultCleanup` 하나로 전투 → 맵(상점·회복 표시) → 로비(아티팩트 포함) → 회복 요청 수명 → 튜토리얼 순서로 `StopInput`을 잇고, `InGameViewSystemGroup`이 소유·해제한다. 한 정리가 실패해도 나머지를 시도하고 첫 오류를 지킨다. Controller·Scope·UI는 따로 오류 콜백을 잇지 않는다.
- 정상 반환 뒤 처리 기록이 남지 않으면 `LobbyArtifactCheckJob`이 아티팩트 도메인만 멈춘다(`LobbyArtifactDomain.Stop`). 로비 출발·이탈은 막지 않는다. 출발 보드는 Core 지금 상태를 따르며 View가 다시 제출하지 않는다. Core/Job 예외는 공용 오류로 전체를 멈춘다.
- 정상 로비 이탈은 `LobbyPlan.CanLeave`가 처리 안 된 편집이 없음을 확인한 뒤 진행한다. 답하지 않은 프리뷰는 막지 않는다.

## 8 성능 기준

- 닫힌 팝업은 조작 입력·표시를 갱신하지 않는다. 맞는 결과 소비와 입력 정리는 계속한다.
- 매 프레임 Find·Instantiate/Destroy·LINQ·목록 전체 Render·EventSystem 결과 배열 생성을 하지 않는다. Raycast 목록·PointerEventData·풀은 연결 때 준비한다. UI Raycast는 클릭·해제와 필요한 포인터 변화 때만 한다.
- 프리뷰 요청과 Core 결과에는 실제 할당이 있다. 드래그 전체가 무할당이라고 주장하지 않는다. Core 계산 할당이 병목이면 담당자와 협의한다.
- 목표 검증은 Player 빌드에서 해상도·CPU·GPU·VSync·프레임 제한을 적고 닫힘·열림·드래그·회전·연속 배치/반환의 CPU/GPU frame time·GC.Alloc·GC spike를 비교한다. Editor FPS·컴파일 통과를 목표 달성으로 적지 않는다.

## 9 보류·확인 필요

| 항목 | 상태 |
|---|---|
| Core 규칙 에셋 불일치 | 운영 아티팩트 규칙 에셋이 canonical effect compiler에 거절된다(`ArtifactDataTableTests.CurrentSoulDropAuthoring_IsRejectedByCanonicalCompiler`). Core 확인 사항이며 View에서 고치지 않는다 |
| 보상 보드 확장 | `ExpandBoard` 호출 경로는 보상 작업 범위다. 표시는 Buffer의 지금 크기를 따른다 |
| 영혼 드랍 받침 | 세로 받침이 0.24 짧다(2026-10-06 측정). Blender 수정이 필요하다 |
| 공간 모델 동기화 | Dummy를 지우려고 프리팹 안 FBX 중첩 인스턴스를 풀었다. 원본 FBX 계층·로컬 변환이 바뀌어도 자동 반영되지 않는다 |
| 전투 진입·Run 재시작·편집 잠금 해제 정책 | 이 작업에서 바꾸지 않았다 |

## 10 다른 작업자 영향

| 작업자 | 고친 곳 | 공유 |
|---|---|---|
| 현강님(UI) | `UIIDs.cs`에 `Artifact = 13` 한 줄 | [현강님 공유](../handoffs/2026-10-06-artifact-popup-ui-hyeongang-share-Hojin.md) — 미전달 |
| 전투 공용 경계 | `InGameViewFaultCleanup` 하나로 StopInput 연결(SA P3.9, 사용자 승인) | [공용 오류 변경 기록](../handoffs/2026-10-06-artifact-popup-fault-cleanup-coordination-Hojin.md) — 전투 작업자 미전달 |
| 천명님(Core·DB) | InGame `ArtifactBoardConfig` 테이블 추가로 `ChapterGraphPreparationTests`·`InGameDataTableTests`에 테이블 한 줄씩 | [Core 전달 문서](../handoffs/2026-09-24-view-core-data-boundary-handoff.md) §5.7 |
| 천명님 인계 | E3-1~E3-8 대응 | [아티팩트 Framework·View 인계](../handoffs/2026-10-03-artifact-framework-view-handoff-ko.md), 대응표는 [계획 §3.8](../plans/2026-10-05-artifact-popup-plan-Hojin.md#38-인계-e3-대응) |

참고 기준: [Framework 프로그래머 가이드](../../../Assets/ProjectL/Script/Systems/Documentation/framework-programmer-guide.md) FW-001~003·008·009·014·016·101·204, DR-008·010·011, [런타임과 씬 연결 안내](../viewDocs/runtime-scene-binding-guide.md).
