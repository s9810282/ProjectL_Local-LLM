# 로비 리팩토링 설계

- 작성자: 호진(이호진)
- 상태: 결정 완료(2026-11-04 사용자 "모두 권장안대로" = Q-LR1 A·Q-LR2 A·Q-LR3 A·Q-LR4 B, P-LR1~P-LR3 그대로). 코드는 아직 바꾸지 않았다. Task 계획은 [로비 리팩토링 계획](../plans/2026-11-04-lobby-refactor-plan-Hojin.md).
- 범위: 2026-11-04 Lobby·StageSelect 검사(HEAD `22fd399ee`, 결과는 채팅)에서 로비에 남은 지적. 로비에는 동작 위험(멈춤·갇힘)이 없어 구조 정리만 다룬다. 스테이지 선택 동작 위험은 [스테이지 맵 동작 위험 설계](2026-11-04-stage-map-behavior-risk-design-Hojin.md)가 맡는다.
- 관련: [검사 후속 정리 설계](2026-10-26-lobby-stage-review-cleanup-design-Hojin.md)(로비 구조와 Q-S6·Q-G5 결정의 원본, 지적 ID LM·LL·UL은 그 문서 §1.2).

## 1 확인 사실

지금 코드를 직접 읽어 확인했다. 줄 번호는 HEAD `22fd399ee` 기준이다. 바뀌는 줄은 특별히 적은 것 말고는 모두 호진 줄이다.

### 1.1 말 복제본 표시 정책이 두 벌이다 (LL9·DL6)

- `Lobby/LobbyCharacterVisual.cs:34-46`과 `StageMap/StageCurrentMarkerView.cs:28-40`이 같은 일을 줄 단위로 똑같이 한다.
  - 복제본 루트 자세를 0으로 둔다.
  - 모든 자식을 `layer = 0`으로 바꾼다(숫자).
  - Collider를 끈다.
  - Animator의 루트 모션과 이벤트를 끈다.
- 고르는 모델은 두 SO가 따로 갖는다(`LobbyModel_Default`, `StageMapView_Default`, 둘 다 `Akimbo.prefab`). 로비 배율 2.146858은 마커 배율 1.073429의 정확히 2배다.
- 한쪽만 정책을 바꾸면(예: 새 컴포넌트 끄기) 두 화면의 말이 다르게 동작한다.

### 1.2 패널형 팝업이 UIBase 수명 훅을 우회한다 (UM3·U4)

- `Screens/Lobby/LobbyDeckUI.cs`와 `Screens/Stage/ArtifactPopupUI.cs`는 등록 루트를 켜 둔 채 자식 `panel`만 토글한다. `IsOpen`과 `OpenStateChanged`도 각자 구현한다.
- `UIBase`(`UISystem/Base/UIBase.cs:82-99`)는 루트가 꺼질 때(`OnDisable`) `NotifyHidden` → `OnUIHidden`을 부르는 수명 훅을 준다. 그런데 두 화면 모두 이 훅을 쓰지 않는다.
  - `LobbyDeckUI`는 `private void OnDisable() => Hide();`(53)로 같은 이름 메서드를 다시 선언한다. Unity는 파생 클래스의 메시지만 부르므로 `UIBase`의 `OnDisable`이 가려진다. 지금 `LobbyDeckUI`는 `OnUIHidden`을 쓰지 않아 동작 영향은 없다.
  - `ArtifactPopupUI`에는 꺼질 때 정리가 없다. 부모 Canvas가 꺼지면 `_isOpen`이 참으로 남는다(10-27 검토 U4가 관찰한 것).
- "루트 켜 둠 + Panel 토글" 꼴 자체는 10-27 Q-S6 A로 이번엔 유지하고, 후속은 현강님 판단을 기다리기로 했다. 공용 제안 팝업(`StageOfferScreenUI`)과 덱 편집 UI(담당 아님)도 같은 꼴이다.

### 1.3 로비 글자 묶음을 View가 SetActive로 여닫는다 (U-1)

- `LobbyViewScope`가 `root.UI.LobbyTexts`(`GameObject`)를 연결 시작 때 켜고(65), 시설 팝업 동안 끈다(106).
- 10-28 Q-G5 B("아티팩트 화면에선 숨김")로 정한 것이다. AGENTS의 기본 규칙(독립적으로 열고 닫는 화면은 UIBase로 등록)과는 다르다.
- 옛 등록 화면 `Clicker`(`UIId.Clicker`, 현강님)는 씬·프리팹 참조 없이 남아 있다(지우지 않음).

### 1.4 "로비 Layer인가" 판정이 다섯 곳에 있다 (LM2 일부 재발)

`_services.Layers.CurrentLayer.Equals(LobbyInputLayers.Lobby)` 식이 다섯 곳에 똑같이 있다.

- `LobbyViewInput.Create`의 람다(35, 10-31 호버 빛 `a1b1e2490`에서 더해짐)
- `LobbyViewInput.Connect`(49)
- `LobbyViewInput.Dispose`(63)
- `LobbyViewScope.SetPopupBlocked`(103)
- `LobbyViewScope.CanDepart`(133)

스테이지 맵은 같은 판정을 `StageMapViewInput.IsSelectionLayer` 하나로 둔다.

### 1.5 그 밖의 낮음

- **아티팩트 씬 루트 찾기(LL5):** `LobbyViewSystem.AttachFromScene`(151)은 `FindFirstObjectByType<ArtifactSceneRoot>()`로 찾아, 비활성 오브젝트와 여러 개인 경우를 보지 않는다. 바로 위의 `LobbySceneRoot`는 비활성 포함·정확히 하나로 검사한다.
- **아티팩트 컨트롤러 선택 인자(LM3 일부):**
  - `ArtifactPopupController` 생성자(43-50)는 `root`·`output`·`popupOutput`을 `= null`로 받고, `inputSlot`은 null 검사 없이 받는다. null이면 씬 갈래와 드래그가 조용히 꺼진다.
  - 이 컨트롤러를 만드는 곳은 `ArtifactPopupViewFactory.Create` 하나이고, 늘 넷을 모두 넘긴다(65-66, 그 앞에서 `root`·`output`·`popupOutput`을 필수로 검사).
- **같은 DB 값 재검사(LL1):** 판·이동·캐릭터 보정·호버·아티팩트 설정은 DB 생성 때 이미 검사한다(`LobbyVisualTable.cs:45-65`, `ArtifactBoardConfigTable.cs:31`). 그런데 런타임에서 다시 검사한다.
  - `LobbyViewServices` 생성자(61-65)
  - `LobbyBoard` 생성자(31)
  - `LobbyCharacterVisual.Create`(21-22)
- **시설 선택 Collider:** `LobbyFacilityView.SelectionCollider`(`DataBaseSystem/Data/View/Table/LobbyVisual/Item/LobbyFacilityView.cs:26`)가 `GetComponent<BoxCollider>()`라서, 포인터가 움직일 때마다 시설 수만큼 불린다(`LobbyFacilityPicker.cs:33`).
- **배경음 주석:** `LobbyViewServices.cs:22`의 주석은 "배경음은 묶음에 넣지 않음"인데, 11-02(현강님)부터 `Audio`가 묶음에 있다. System의 `_audio`와 묶음의 `Audio` 두 자리가 생겼다.
- **base와 같은 재정의:** `LobbyDeckUI.BindUIService`·`GetUIData`(55-65)와 `ArtifactPopupUI`의 같은 두 메서드는 base와 똑같다.
- **`ArtifactPopupUI` 이름·문구(UL5·UL6):**
  - 닫기 허용을 정하는 메서드 이름이 `SetCanOpen`이다.
  - 예외 문구가 영어다.
  - 로비 UI인데 자리와 네임스페이스가 `Screens.Stage`다. 같은 폴더의 `IArtifactPopupDisplay`·`ArtifactPoolItemContent`·`ArtifactPoolItemView`가 함께 움직여야 하고, 이 넷 중에는 현강님 줄이 있다.
- **닫기 슬롯 −1 두 벌(LL2):** `LobbyDeckUI.cs:15`와 `LobbyDeckPresenter.cs:10`에 `CloseSlot = -1`이 따로 있다.
- **아티팩트 끌기 중 매 Tick 후보 다시 계산:** `ArtifactDragController.Tick`(72)이 끄는 동안 포인터가 움직이지 않아도 매 Tick `UpdateCandidate`를 부른다. 같은 계산을 `PointerMoved`도 한다. 매 Tick 불러야 하는 이유(판·카메라가 끄는 도중 바뀌는 경우 등)가 있는지는 확인하지 못했다.

## 2 설계

### 2.1 LR1 말 복제본 정책을 한 곳에 둔다

- 새 `ViewSystem/Common/ChessPieceClone.cs`(internal static)에 `Prepare(GameObject instance)` 하나를 둔다. 기존 체스 말 이동 `ChessPieceMotion`과 같은 이름 줄이다.
  - 루트 자세 0·배율 1, 자식 레이어를 이름 있는 상수 `DefaultLayer`(= 0, Unity Default)로, Collider 끄기, Animator 루트 모션·이벤트 끄기.
- `LobbyCharacterVisual.Create`와 `StageCurrentMarkerView`가 이 함수를 부르고 자기 사본을 걷는다.
- 모델을 고르는 SO 둘과 배율 값은 그대로 둔다(Q-LR1).

### 2.2 LR2 패널형 팝업은 UIBase 꺼짐 훅으로 정리한다

- 공통 기반은 이번에 만들지 않는다(Q-LR2). Q-S6에서 현강님 판단을 기다리는 중이다.
- 두 로비 화면만 같은 규칙으로 맞춘다.
  - `LobbyDeckUI`: `private void OnDisable()`을 지우고, `protected override void OnUIHidden() => Hide();`로 바꾼다. 그러면 `UIBase`의 꺼짐 훅이 다시 돌고, 부모가 꺼질 때의 정리는 지금과 같다.
  - `ArtifactPopupUI`: 같은 `OnUIHidden` 재정의를 더한다. 부모 Canvas가 꺼지면 패널을 닫고 `OpenStateChanged(false)`를 알린다. 이로써 `_isOpen`이 참으로 남는 문제가 없어진다.
  - 두 `Hide`는 이미 재진입을 막는다(`_hideInProgress` 또는 `_isOpen` 확인). 그래서 훅이 `Hide` 뒤에 한 번 더 불려도 아무 일도 하지 않는다.
- `StageOfferScreenUI`에도 같은 문제가 있지만 스테이지 선택 정리에서 다룬다.

### 2.3 LR3 로비 글자 묶음은 그대로 둔다 (Q-LR3 권장안)

- `lobbyTexts`는 팝업이 아니라 로비 캔버스의 일부이고, 시설 팝업 동안 숨기는 것만 한다. 그래서 따로 등록할 화면이 아니라고 보고, Q-G5 B 결정을 그대로 둔다.
- 이 이유를 `LobbyViewScope`의 `_lobbyTexts` 주석 한 줄로 남긴다.

### 2.4 LR4 로비 Layer 판정을 하나로

- `LobbyViewInput`에 `internal bool IsLobbyLayer`를 둔다. 스테이지 맵의 `IsSelectionLayer`와 같은 꼴이다.
- 다섯 곳이 이것을 읽는다. `LobbyViewInput.Create`의 람다는 생성 때 한 번 만든 `() => IsLobbyLayer`를 넘긴다.

### 2.5 LR5 작은 정리

1. **아티팩트 씬 루트:** `FindObjectsByType<ArtifactSceneRoot>(FindObjectsInactive.Include, …)`로 찾고, 정확히 하나가 아니면 예외를 던진다. `LobbySceneRoot`와 같은 규칙이다.
2. **아티팩트 컨트롤러:** `root`·`output`·`popupOutput`·`inputSlot`을 기본값 없는 필수 인자로 받는다. 그 뒤 `root != null` 갈래와 `?.`를 걷는다. Factory가 이미 넷을 모두 넘기므로 호출 줄은 그대로다.
3. **재검사 걷기:** `LobbyViewServices`·`LobbyBoard`·`LobbyCharacterVisual`의 `Validate()` 호출(데이터 객체 검사)을 걷는다. DB 생성 검사(`LobbyVisualTable`)가 원본이다. `LobbyCharacterVisual`의 `LobbyModelData.ValidateCharacterPrefab(prefab)`은 남긴다. 10-28 R-b에서 "View 생성 때도 한 번 더 부른다"로 정했고(`LobbyModelData.cs:21-22` 주석), 인자로 받은 프리팹을 보는 입구 검사이기 때문이다(§8).
4. **시설 Collider:** `LobbyFacilityView`가 처음 읽을 때 `BoxCollider`를 한 번 찾아 필드에 둔다. 직렬화 칸을 새로 만들지 않으므로 프리팹 작업이 없다.
5. **배경음 주석:** System의 `_audio`는 이미 묶음의 `Audio`를 복사한 값이라(`LobbyViewSystem.cs:90`) 원본은 하나다. 주석만 지금 사실대로 고치고, 필드 사본을 걷어 묶음 값을 바로 읽는다. 배경음 프리셋 숫자 `0`은 10-28 결정(배경음은 System)대로 그대로 둔다.
6. **같은 재정의:** `LobbyDeckUI`·`ArtifactPopupUI`의 base와 같은 `BindUIService`·`GetUIData`를 걷는다.
7. **`ArtifactPopupUI` 이름·문구:** `SetCanOpen`을 하는 일에 맞는 이름으로 바꾸고, 예외 문구를 한국어로 바꾼다. 부르는 곳은 `ArtifactPopupController`·`ArtifactPopupViewFactory`다. 자리·네임스페이스 이동은 Q-LR4.
8. **닫기 슬롯:** `LobbyDeckPresenter`가 자기 상수를 두지 않고 UI의 공개 상수(`LobbyDeckUI.CloseSlot`)를 읽는다.
9. **아티팩트 끌기 Tick:** 구현 때 `UpdateCandidate`를 매 Tick 불러야 하는 이유를 확인한다. 이유가 없으면 Tick에서는 Buffer 응답만 소비하고, 후보 계산은 `PointerMoved`에만 둔다. 이유가 있으면 그대로 두고 주석으로 남긴다.

## 3 결정

2026-11-04 사용자 "모두 권장안대로": 아래 표의 권장(굵은 글자)을 그대로 정했다.

| ID | 질문 | 권장 | 다른 안 |
|---|---|---|---|
| Q-LR1 | 말 모델·배율을 고르는 SO를 하나로 합칠지 | **A 그대로 둠.** 코드 정책만 하나로(LR1). 두 화면의 크기는 따로 바뀔 수 있다 | B 공용 SO 하나에 모델·기준 배율, 화면마다 배수만(View DB 코드·에셋 변경, 에셋 값은 Codex) |
| Q-LR2 | 패널형 팝업 공통 기반 | **A 이번엔 훅만 바로잡기**(LR2). 기반은 Q-S6 현강님 판단 뒤 | B 지금 `UISystem` 쪽에 패널형 팝업 기반을 만들어 로비 둘·공용 제안 팝업에 씀 |
| Q-LR3 | 로비 글자 묶음을 UIBase로 등록할지 | **A 그대로**(Q-G5 B 유지, 이유만 주석) | B 로비 기본 화면 UIBase 하나로 등록(`Clicker`를 다시 쓰거나 새로) |
| Q-LR4 | 아티팩트 UI 넷을 `Screens/Lobby`로 옮길지 | **B 이번엔 그대로**, 이름·문구만. 현강님 줄이 든 파일을 함께 옮겨야 함 | A 넷을 옮기고 네임스페이스를 바꿈(`.meta` GUID 유지, MonoBehaviour 둘에 `MovedFrom`, 현강님께 변경 정리 전달) |

설계에서 정한 것(확인 대상):

- P-LR1: 복제본 정책 함수 이름은 `ChessPieceClone.Prepare`, 자리는 `ViewSystem/Common/`이다(로비·스테이지 둘이 쓰므로).
- P-LR2: `StageCurrentMarkerView`는 스테이지 맵 파일이지만 같은 정책의 사본이라 이 묶음에서 함께 고친다(호진 줄).
- P-LR3: LR5-9는 확인 결과에 따라 바뀔 수 있어, 구현 보고에 어느 쪽으로 했는지 적는다.

## 4 바뀌는 곳

| 파일 | 바꾸는 것 |
|---|---|
| `ViewSystem/Common/ChessPieceClone.cs`(새, `.meta`는 Unity가 만듦) | LR1 |
| `ViewSystem/Lobby/LobbyCharacterVisual.cs` | LR1, LR5-3 |
| `ViewSystem/StageMap/StageCurrentMarkerView.cs` | LR1 |
| `GlobalSystem/UISystem/Screens/Lobby/LobbyDeckUI.cs` | LR2, LR5-6, LR5-8 |
| `GlobalSystem/UISystem/Screens/Stage/ArtifactPopupUI.cs` | LR2, LR5-6, LR5-7 |
| `ViewSystem/Lobby/LobbyViewInput.cs`·`LobbyViewScope.cs` | LR3 주석, LR4 |
| `ViewSystem/Lobby/LobbyViewSystem.cs` | LR5-1, LR5-5 |
| `ViewSystem/ArtifactPopup/ArtifactPopupController.cs`·`ArtifactPopupViewFactory.cs` | LR5-2, LR5-7 |
| `ViewSystem/Lobby/LobbyViewServices.cs`·`LobbyBoard.cs` | LR5-3, LR5-5 |
| `DataBaseSystem/Data/View/Table/LobbyVisual/Item/LobbyFacilityView.cs` | LR5-4 |
| `ViewSystem/Lobby/Deck/LobbyDeckPresenter.cs` | LR5-8 |
| `ViewSystem/ArtifactPopup/ArtifactDragController.cs` | LR5-9(확인 뒤) |

- 구현 때 각 파일의 바꾸는 줄 작성자를 `git blame`으로 다시 확인한다. 남의 줄이면 그 줄은 빼고 변경 정리 메모로 돌린다.
- UISystem `Base`, Factory·ServiceKeys·Framework·Core, 씬·프리팹·SO 값은 바꾸지 않는다.
- 시험: 이 파일들을 만드는 기존 시험이 있으면 컴파일이 깨지는 곳만 맞춘다(구현 때 검색). 새 시험은 만들지 않는다.

## 5 원칙 점검

- SSOT:
  - 복제본 정책 한 곳(LR1).
  - 로비 Layer 판정 한 곳(LR4).
  - DB 값 검사는 DB 생성 한 곳(LR5-3).
  - 닫기 슬롯 번호 한 곳(LR5-8).
  - 배경음 서비스 자리 한 곳(LR5-5).
- SOLID: 아티팩트 컨트롤러가 필요한 의존을 생성 때 모두 받는다. 표시 화면은 수명 정리를 `UIBase` 훅으로 받는다(규약대로).
- FW·AGENTS: UIBase 수명 훅을 우회하지 않는다. 씬 루트 찾기 규칙을 하나로 둔다(필수 구성 누락은 예외).
- 기능 변경 독립성:
  - LR1은 두 말 표시만 건드린다.
  - LR2는 두 로비 UI만 건드린다. 공용 제안 팝업·덱 편집은 바뀌지 않는다.
  - 나머지는 파일 안 정리다.
- 성능:
  - 포인터 이동마다의 `GetComponent`를 없앤다(LR5-4).
  - 끌기 Tick의 계산을 줄일 수 있다(LR5-9).
  - 새로 더하는 매 프레임 비용은 없다.

## 6 범위 밖

- 덱 시설이 늘 빈 목록인 것(LL3): Core 후보 계약이 정해진 뒤.
- 시설 종류 switch 셋(LM4): 시설이 늘 때 시설 표 하나로.
- 아티팩트 판이 바뀔 때 모델을 모두 반환하고 다시 놓는 것과 `ToArray` 할당: 바뀔 때만 드는 비용이다.
- 아티팩트 미리보기의 칸 높이·크기·회전 상수, 작은 사본(`float.IsFinite` 사본 여섯, `Rotation4 * 90`, 판 중심 격자 식).
- 안 쓰는 입력 키·슬롯 등록(`LobbyInputKey`, `ArtifactInputKey` 주석, 슬롯 `ExportServices`): `ServiceKeys`라 민형님 메모 후보.
- 로비 덱 컨트롤러를 Factory 없이 만드는 것: 의존이 화면·Presenter뿐이라 지금은 그대로.
- 로비·스테이지 맵 System의 연결 골격 두 벌과 늦은 씬 콜백 처리 차이: 스테이지 선택 정리 때 함께.
- 공용 팝업 수명(`PopupViewScope`)이 바깥 닫힘 뒤 UISystem 열림 기록(`_openUIs`·이력)을 맞추지 않는 것(§8): 로비·스테이지 팝업 공통이라 따로.

## 8 FW·렌더링 대조 (2026-11-04)

사용자 요청 "FW이나 렌더링 쪽에서 맞는지 확인"으로 Framework UISystem·공용 팝업 수명·출력 카메라 코드와 대조했다. 고칠 것은 LR5-3·LR5-5의 문구 둘이었고(위에 반영), 나머지는 설계대로 맞다.

| 항목 | 대조한 코드 | 결과 |
|---|---|---|
| LR2 `OnUIHidden` 재정의 | `UIBase.cs:82-99`(꺼짐·씬 해제를 훅으로 잇고 중복을 막음), `UI-lifecycle.md` "실제 활성 상태 변경과 정리는 UIBase.Open/Hide 및 OnUIOpened/OnUIHidden에서" | 규약에 맞다. `DeckEditUI`·`UnitCardUI`·`DeckSlotInfoUI` 등 패널형 화면이 이미 같은 훅을 쓴다. `LobbyDeckUI`의 `private OnDisable`은 이 규약을 가린다 |
| LR2 훅이 부르는 `Hide` | `ArtifactPopupUI.Hide`(94-113, `wasOpen`일 때만 알림), `LobbyDeckUI.Hide`(`_hideInProgress`) | 두 번 불려도 안전하다 |
| LR2 알림을 받는 쪽 | `PopupViewScope.OnOpenStateChanged`(215-253) → 바깥 닫힘(`HiddenExternally`) → 컨트롤러 `Hide` → 다음 Tick `FinishClose`(입력 세션 끝, 로비 막힘 풀기) | 로비 덱과 같은 길이다. 아티팩트는 컨트롤러 `HideDisplay`(195-210)가 출력 대여 `Exit`를 불러, 부모가 꺼졌을 때도 출력 카메라 마스크가 되돌아간다. 지금은 `_isOpen`이 참으로 남아 마스크가 UI만 그리는 채로 남을 수 있다(렌더링 쪽 개선) |
| LR2 UISystem 열림 기록 | `UISystem.OpenUI`(300-341), `PopupViewScope.FinishClose`(205-213) | 바깥 닫힘 뒤에도 UISystem의 `_openUIs`에는 남는다(`CloseUI`를 부르지 않음). 다음 열기는 `OpenUI`가 "열림 + 이력"이면 `Open()`을 다시 불러 정상으로 열린다. 이 어긋남은 공용 팝업 수명의 기존 성질이고 로비 덱·덱 편집에도 이미 있다. 이번 범위 밖(§6) |
| LR1 레이어 0 | 지금 두 사본 모두 0 | 값이 같아 화면 변화가 없다. 이름 있는 상수로만 바꾼다. 렌더링 레이어(URP Rendering Layer)는 건드리지 않는다 |
| LR5-1 씬 루트 찾기 | `ArtifactSceneRoot`·`LobbySceneRoot`가 든 씬·프리팹 | 둘 다 `Scenes/Lobby.unity` 하나에만 있다. 비활성 포함 검색으로 바꿔도 하나다 |
| LR5-3 재검사 | `LobbyVisualTable.cs:45-65`(DB 생성 때 다섯 검사), `LobbyModelData.cs:21-22` | 데이터 객체 검사는 DB 한 곳이면 된다. 캐릭터 프리팹 입구 검사는 R-b 결정이라 남긴다 |
| LR5-5 배경음 | `LobbyViewSystem.cs:89-90` | 서비스 원본은 이미 하나(Global Context → 묶음 → 필드 사본) |
| 서비스 조립 | 새 파일 `ChessPieceClone`은 정적 함수라 등록이 없다. 다른 변경은 Factory·Group·Export 경로를 바꾸지 않는다 | FW-001~003 무관 |

§6에 하나를 더한다: 공용 팝업 수명이 바깥 닫힘 뒤 UISystem 열림 기록을 맞추지 않는 것(`PopupViewScope`, 로비·스테이지 팝업 공통).

## 7 사용자 Unity 확인 목록(구현 뒤)

1. 로비 진입 → 캐릭터 말이 지금과 같은 자리·크기로 보이고, 시설로 뛰어 이동한다.
2. 스테이지 맵의 현재 위치 말이 지금과 같다.
3. 덱 시설 → 덱 팝업 열기·카드 호버·고정·닫기가 지금과 같다.
4. 아티팩트 시설 → 팝업 열기·끌어 놓기·회전·닫기가 지금과 같다. 끄는 도중 포인터를 멈춰도 미리보기가 그대로다(LR5-9를 바꿨다면).
5. 아티팩트 팝업이 열린 채 씬을 떠났다 다시 오면 팝업이 닫힌 상태로 시작한다(LR2).
6. 시설 호버 빛·안내 글자 숨김·출발이 지금과 같다.
7. 콘솔에 새 예외가 없다.
