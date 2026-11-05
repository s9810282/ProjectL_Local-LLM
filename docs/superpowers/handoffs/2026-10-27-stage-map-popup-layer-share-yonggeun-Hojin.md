# 스테이지 맵 팝업 입력 Layer 이름·자리 변경 — 용근님 공유

- 작성자: 호진(이호진)
- 날짜: 2026-10-27
- 작성: 호진(Claude 작업), 미전달
- 출처: 10-26 Lobby·StageSelect 원칙 검사 6번 C-2. [검사 후속 정리 설계](../specs/2026-10-26-lobby-stage-review-cleanup-design-Hojin.md)(옛 작은 정리 설계 §3), [계획](../plans/2026-10-26-lobby-stage-review-cleanup-plan-Hojin.md) Phase C-2.
- 상태: 코드 반영, 외부 C# 빌드 오류 0, Unity 미확인, 커밋됨(`b1dd4962`, 10-27)

## 바뀐 것

- 상점·회복·덱 편집이 함께 쓰는 팝업 입력 Layer의 **이름**이 `"ShopPopup"` → `"StageMapPopup"`으로 바뀌었습니다. Framework `InputLayer`는 이름으로 같음을 판정하므로, 이름이 쓰임(상점만이 아니라 세 팝업 공통)에 맞게 바뀐 것입니다.
- Layer **자리**가 `ViewSystem/StageMap/StageMapPopupInputLayers.Popup` → `ViewSystem/StageSelectionInput/StageSelectionInputLayers.Popup`으로 옮겨졌습니다(로비 `LobbyInputLayers`에 `Lobby`·`DeckPopup`을 함께 두는 것과 같은 꼴). `StageMapPopupInputLayers.cs`(+ `.meta`)는 지웠습니다.
- 동작은 같습니다. 세 팝업은 여전히 같은 Layer 하나를 씁니다.

## 용근님 줄

- `ViewSystem/StageMap/Deck/DeckEditPopupController.cs:42`(덱 편집 UI, `38869ec1`): `StageMapPopupInputLayers.Popup` → `StageSelectionInputLayers.Popup`, using 한 줄(`…ViewSystem.StageSelectionInput`) 추가.

## 이름을 문자열로 비교하는 곳

- `GlobalInputCompositionTests:90`(Layer 이름 목록)의 기대값을 `"StageMapPopup"`으로 맞췄습니다. 다른 곳에서 Layer 이름 문자열을 직접 쓰고 있다면 같이 바꿔야 합니다(씬·에셋에는 없음).

## 2026-10-28 덱 편집 컨트롤러가 공통 수명 부품을 씀 (R-a, 미전달)

- 출처: [검사 후속 정리 계획 §6.4](../plans/2026-10-26-lobby-stage-review-cleanup-plan-Hojin.md#64-phase-r-a--묶음-2-스테이지-맵-조립-q-r1r2r4r12b-b1b3b5b7) R-a. 외부 C# 빌드 오류 0, Unity 미확인, 커밋 전.
- 상점·회복·덱 편집 컨트롤러의 같은 뼈대(공통 `PopupViewScope` 만들기, 열림 여부, 열기·닫기, 시작 실패 정리, 정리 순서, 오류 보고)를 새 부품 `ViewSystem/StageMap/StagePopupViewLifetime.cs` 하나로 모았습니다. 세 컨트롤러는 그것을 갖고 자기 내용만 다룹니다.

### 용근님 파일에서 바뀐 줄

- `Deck/DeckEditPopupController.cs`: `_host`·`_faults`·`_scope`·`_disposed` 필드 대신 `_lifetime` 하나. `Start`·`Tick`·`IsOpen`·`Close`·`RequestOpen`·`Act`·`Show`·`Hide`·`Dispose`·`Guard`가 그 부품을 부릅니다. `UnbindScreen` 메서드는 `Dispose` 안으로 들어갔습니다.
- `Deck/DeckEditPopupViewFactory.cs`: 오류 경계 `faults`가 필수가 되었습니다(null이면 생성 때 예외). 운영 조립은 이미 늘 넘기고 있었습니다.

### 동작 차이 (모두 오류·정리 경로)

- 런타임이 오류로 멈춘 뒤에는 열기·보이기·편집 동작을 하지 않습니다(전에는 연결이 현재인지만 봄).
- 시작은 한 번만 하고, 시작 중 실패하면 컨트롤러를 정리한 뒤 모아서 던집니다(상점·회복과 같음).
- 시작 때 열기 요청 연결(`BindOpenRequest`)이 공통 Scope 시작보다 먼저 됩니다. 정리 때는 화면 연결 끊기가 공통 Scope 정리보다 먼저 됩니다. 화면에 보이는 차이는 없습니다.

### 판단 부탁 — 덱 편집 열기 조건 (설계 Q-R3)

- 덱 편집을 겹쳐 열지 않는 맵 쪽 조건은 덱 편집 쪽 몫이라 판정 내용은 그대로 두고, 판정 자리만 맵 연결 `ViewSystem/StageMap/StageMapViewScope.cs`의 `CanOpenDeckEdit`(private 판정 함수)로 옮겼습니다. 덱 편집 컨트롤러는 이 함수를 받아 부릅니다.
- 다른 두 판정(선택 화면 다시 보이기·개발 도구)과 달리 이 조건은 **처리 전 맵 요청·맵 표시 여부·입력 Layer·상점/회복 결과 대기**를 보지 않습니다. 예: 노드 진입 요청이 처리 전인 순간이나 상점 구매 결과를 기다리는 동안에도 덱 편집이 열릴 수 있습니다. 의도인지 정해 주세요.
