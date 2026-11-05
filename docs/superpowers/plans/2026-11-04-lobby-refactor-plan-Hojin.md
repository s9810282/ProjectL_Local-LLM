# 로비 리팩토링 계획

- 작성자: 호진(이호진)
- 설계: [로비 리팩토링 설계](../specs/2026-11-04-lobby-refactor-design-Hojin.md)(LR1~LR5, 결정 Q-LR1~Q-LR4 완료)
- 상태: LB-a 코드 완료(2026-11-05, 사용자 "LB-a 실행해", 컴파일 미확인, 새 시험 없음, 시험 변경 없음, Unity 확인 대기). 실행 기록은 §3.
- Phase는 하나(LB-a)다. 끝나면 멈추고 보고한다. 보고 전에 Codex 검수를 요청한다.

## 1 진행 규칙

- 새 시험 코드를 만들지 않는다. MSBuild를 돌리지 않는다. 보고에는 "컴파일 미확인"을 적는다.
- 각 Task 전에 바꾸는 줄의 작성자를 `git blame`으로 확인한다. 남의 줄이면 그 줄은 바꾸지 않고 §4 메모 목록에 적는다.
- 기존 시험이 이 변경으로 컴파일·실행에서 깨지면 그 부분만 맞춘다. 착수 전에 `Assets/Tests`에서 바뀌는 형식을 검색한다.
- 설계와 다른 결정이 필요하면 구현하지 않고 먼저 묻는다.

## 2 Task (Phase LB-a)

| Task | 내용 | 파일 | 설계 |
|---|---|---|---|
| T1 | `ChessPieceClone.Prepare` 새 파일. 로비 말·현재 위치 말이 부르고 사본을 걷음 | `Common/ChessPieceClone.cs`(새), `Lobby/LobbyCharacterVisual.cs`, `StageMap/StageCurrentMarkerView.cs` | LR1 |
| T2 | `LobbyDeckUI`의 `private OnDisable` → `OnUIHidden` 재정의, `ArtifactPopupUI`에 같은 재정의 | `Screens/Lobby/LobbyDeckUI.cs`, `Screens/Stage/ArtifactPopupUI.cs` | LR2 |
| T3 | `_lobbyTexts` 주석 한 줄(Q-G5 B 이유) | `Lobby/LobbyViewScope.cs` | LR3 |
| T4 | `LobbyViewInput.IsLobbyLayer`, 다섯 곳이 읽음 | `Lobby/LobbyViewInput.cs`, `Lobby/LobbyViewScope.cs` | LR4 |
| T5 | 아티팩트 씬 루트를 비활성 포함·정확히 하나로 찾음 | `Lobby/LobbyViewSystem.cs` | LR5-1 |
| T6 | 아티팩트 컨트롤러 의존 넷 필수, 씬 갈래·`?.` 걷음 | `ArtifactPopup/ArtifactPopupController.cs`(+ 그 안의 부품이 받는 null 갈래) | LR5-2 |
| T7 | DB와 같은 데이터 객체 재검사 걷음(`ValidateCharacterPrefab` 입구 검사는 R-b 결정대로 남김) | `Lobby/LobbyViewServices.cs`, `Lobby/LobbyBoard.cs`, `Lobby/LobbyCharacterVisual.cs` | LR5-3 |
| T8 | 시설 Collider 한 번만 찾음 | `DataBaseSystem/Data/View/Table/LobbyVisual/Item/LobbyFacilityView.cs` | LR5-4 |
| T9 | 배경음 주석 고침, System의 `_audio` 필드 사본을 걷고 묶음의 `Audio`를 바로 읽음 | `Lobby/LobbyViewServices.cs`, `Lobby/LobbyViewSystem.cs` | LR5-5 |
| T10 | base와 같은 재정의 걷음 | `LobbyDeckUI.cs`, `ArtifactPopupUI.cs` | LR5-6 |
| T11 | `SetCanOpen` 이름 바꿈, 영어 예외 문구를 한국어로 | `ArtifactPopupUI.cs`와 부르는 곳(`ArtifactPopupController`·`ArtifactPopupViewFactory`) | LR5-7 |
| T12 | 닫기 슬롯 상수 한 곳(`LobbyDeckUI.CloseSlot` 공개, Presenter가 읽음) | `LobbyDeckUI.cs`, `Lobby/Deck/LobbyDeckPresenter.cs` | LR5-8 |
| T13 | 끌기 Tick의 `UpdateCandidate` 이유 확인 → 이유가 없으면 Tick에서 뺌, 있으면 주석 | `ArtifactPopup/ArtifactDragController.cs` | LR5-9 |
| T14 | 실행 기록(§3)·CLAUDE.md 현재 상태 한 줄 | 이 문서, `CLAUDE.md` | — |

경로 머리: `ViewSystem` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/`, `Screens` = `…/RuntimeSystem/GlobalSystem/UISystem/Screens/`.

## 3 실행 기록

### 3.1 LB-a (2026-11-05)

- 기준 HEAD `22fd399ee`. 바꾼 줄은 모두 `git blame`으로 호진 줄임을 확인했다. 남의 줄은 §4로 돌렸다.
- `Assets/Tests`에 바뀐 형식(`ArtifactPopupController`·`IArtifactPopupDisplay`·`SetCanOpen`·`LobbyDeckUI`·`ArtifactPointerMapper` 등)을 쓰는 시험은 없었다. 그래서 시험 변경은 없다.

| Task | 한 것 |
|---|---|
| T1 | 새 `ViewSystem/Common/ChessPieceClone.cs`(`Prepare`, 상수 `DefaultLayer` = 0). `LobbyCharacterVisual`·`StageCurrentMarkerView`가 부르고 사본 12줄씩을 걷음. `.meta`는 Unity가 만든다 |
| T2 | `LobbyDeckUI`의 `private OnDisable` 삭제 → `protected override void OnUIHidden() => Hide();`. `ArtifactPopupUI`에도 같은 재정의 |
| T3 | `LobbyViewScope._lobbyTexts` 위 주석 두 줄(Q-G5 B 이유) |
| T4 | `LobbyViewInput.IsLobbyLayer`. 입력 셋(`Create` 람다·`Connect`·`Dispose`)과 Scope 둘(`SetPopupBlocked`·`CanDepart`)이 읽음 |
| T5 | `LobbyViewSystem.AttachFromScene`이 `FindObjectsByType<ArtifactSceneRoot>(Include)`로 찾고 정확히 하나가 아니면 예외 |
| T6 | `ArtifactPopupController` 생성자의 `root`·`output`·`popupOutput` 기본값 삭제·필수 검사, `inputSlot` null 검사, `root != null` 갈래·`_popupOutput?.`·`_inputSlot != null`·`AllowsInput`의 `Root != null` 삭제. 같은 이유로 `ArtifactPointerMapper`의 `camera`·`mapper`도 필수로 바꾸고 `Resolve`의 null 검사를 걷음(계획 표 "부품이 받는 null 갈래"). Factory 호출 줄은 그대로 |
| T7 | `LobbyViewServices` 생성자의 `Validate()` 다섯, `LobbyBoard`의 `layout.Validate()`, `LobbyCharacterVisual`의 `presentation.Validate()` 삭제. `ValidateCharacterPrefab`는 R-b대로 남김. 클래스 주석을 지금 사실대로 고침 |
| T8 | `LobbyFacilityView`에 `_selectionCollider` 필드, `SelectionCollider`가 처음 읽을 때 한 번 찾음. 직렬화 칸 없음. 주석의 "누름·뗌만 읽음"을 "호버·누름·뗌"으로 고침 |
| T9 | `LobbyViewServices` 클래스 주석만 고침(배경음 서비스는 묶음이 원본, 트는 시점은 System). `LobbyViewSystem.cs:90`의 `_audio = _services.Audio;`와 필드 선언(`:35`)은 현강님 줄(`c5329fcc9`·`48d1b2750`)이라 바꾸지 않고 §4에 적음 |
| T10 | `LobbyDeckUI`·`ArtifactPopupUI`의 `BindUIService`·`GetUIData` 재정의 삭제(base와 같음), 안 쓰게 된 `using ProjectL.Framework.SystemBase;` 삭제 |
| T11 | `SetCanOpen` → `SetCanClose`(`IArtifactPopupDisplay`·`ArtifactPopupUI`·컨트롤러 `OwnedDisplay`·부르는 곳 넷), 필드 `_canOpen` → `_canClose`. 호진 줄의 영어 예외 문구 넷을 한국어로. 현강님 줄(`RenderPool`의 예외 `:225`)은 이미 한국어라 그대로 |
| T12 | `LobbyDeckUI.CloseSlot`을 `internal`로, `LobbyDeckPresenter`가 자기 상수를 걷고 이것을 읽음 |
| T13 | 확인 결과 **이유가 있어 남김**(P-LR3): 출력 카메라는 Boot Main이고 Cinemachine이 로비 가상 카메라를 따라가므로, 포인터가 멈춰도 카메라가 섞이는 동안 포인터 아래 칸이 바뀐다. 칸이 같으면 `ArtifactInteraction.SetCandidate`가 바로 돌아가 프리뷰 요청을 다시 내지 않는다. 주석 두 줄만 더함 |
| T14 | 이 기록과 CLAUDE.md 현재 상태 한 줄 |

- 줄 끝 문자는 파일마다 원래대로 맞췄다. 한 파일 안에 LF와 CRLF가 섞이지 않게 했다.
- 계획과 다른 점:
  - T6에서 `ArtifactPointerMapper`까지 필수로 바꿨다. 계획 표의 "부품이 받는 null 갈래" 범위다.
  - T9는 남의 줄 규칙 때문에 필드 사본을 걷지 않았다.
- Codex 검수: §3.2.

### 3.2 Codex 검수

2026-11-05, 읽기 전용 정적 검수. 결과는 **지적 없음**이다. 첫 실행은 샌드박스가 저장소 밖 개인 규칙 파일을 읽지 못해 검수 전에 멈췄다. 그래서 규칙 내용을 프롬프트에 붙여 다시 돌렸다.

| 확인 항목 | 결과 |
|---|---|
| UI 숨김 수명 | 첫 `OnDisable`, 부모 비활성화, 씬 해제에서 새 예외나 이중 알림이 없다. 아티팩트 출력 마스크는 닫힘 알림 때 돌아온다 |
| 동작 변경 | LR2 말고는 동작 변경이 없다. T5·T6의 예외 강화는 LR5에 적힌 것이다 |
| 호출자·이름 | `Assets/Tests`까지 보았다. 필수 인자를 넘기지 않는 곳과 `SetCanOpen`을 부르는 곳이 남아 있지 않고, `CloseSlot`은 같은 어셈블리라 `internal`로 읽힌다 |
| T13 유지 | 타당하다. `Boot.unity`의 Brain 블렌드가 2초라 포인터가 멈춰 있어도 포인터 아래 칸이 바뀐다 |
| 원칙·소유권 | 새 위반이 없다. 지우거나 바꾼 109줄은 모두 호진 줄이다. 현강님 배경음 줄은 그대로다 |
| 줄 끝 | 한 파일 안에서 LF와 CRLF가 섞인 곳이 없다 |

남은 실행 확인은 Codex도 짚은 두 가지다. 부모를 끈 뒤 다시 켜는 경우와, 팝업을 연 채 씬을 떠났다 돌아오는 경우다. 둘 다 사용자 Unity 확인 목록(설계 §7)에 들어 있다. 검수 통과는 Phase 인수가 아니다.

## 4 다른 작업자에게 넘길 것

- **현강님**: `LobbyViewSystem.cs:35`(`_audio` 필드)와 `:90`(`_audio = _services.Audio;`)은 서비스 묶음 `LobbyViewServices.Audio`의 사본이다. 배경음을 트는 `:174-175`(호진 줄)가 묶음 값을 바로 읽으면 이 두 줄은 지울 수 있다. 바꿀지는 현강님께 정리해 넘긴다(미전달).

## 5 사용자 Unity 확인

설계 §7 목록 그대로.
