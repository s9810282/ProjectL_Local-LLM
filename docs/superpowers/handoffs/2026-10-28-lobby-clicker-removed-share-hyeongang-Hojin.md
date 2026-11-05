# 로비 Clicker 걷음 — 현강님 공유 (2026-10-28, 호진, 미전달)

- 작성자: 호진(이호진)

설계 원본: [검사 후속 정리 설계 §12.7](../specs/2026-10-26-lobby-stage-review-cleanup-design-Hojin.md)

## 1 무엇을 했나

- 로비 시설 안내 글자 셋("여정 시작하기"·"덱 편집하기"·"아티팩트 관리하기")을 `Clicker` 아래에서 `LobbyPanel` 바로 아래("로비" 글자 옆)로 옮겼습니다. 화면 위치는 같습니다(`Clicker`가 화면 가운데 (0, 0)이라 위치 값 그대로).
- `Lobby.unity`에서 `Clicker` 오브젝트(와 컴포넌트)를 뺐습니다(호진 에디터 작업).
- 그래서 `UIId.Clicker`가 등록되지 않아, Clicker를 열고 닫던 줄을 걷었습니다(두지 않으면 로비 진입 때 "등록되지 않은 UI" 예외).
  - `GameBootstrap.ShowLobbyUI`의 `_ui.OpenUI(UIId.Clicker)` 한 줄(`b691d8ae`).
  - `LobbyViewScope.SetPopupBlocked`의 Clicker 숨김·복귀, `Dispose`의 초기화 줄, 필드 `_clickerSuppressedByFacility`(`b691d8ae`).
- 글자 셋과 "로비" 제목은 `LobbyPanel/LobbyTexts` 묶음에 있고, 시설 팝업(덱·아티팩트) 동안만 `LobbyViewScope.SetPopupBlocked`가 묶음을 끕니다(UISystem을 거치지 않음). 아티팩트 화면은 패널 안에 자기 제목 "아티팩트"를 갖습니다(설계 §12.7.8·§12.7.9).

## 2 남겨 둔 것(현강님 결정)

이제 아무 데서도 쓰지 않습니다. 지울지 정해 주세요.

- `GlobalSystem/UISystem/Screens/Lobby/Clicker.cs`(+ `.meta`), `Clicker.md`(Phase 2 내용은 이제 낡음).
- `UIIDs.cs`의 `Clicker = 34`.
- 자동 복귀 기능: `UIBase.RestoreWhenHistoryEmpty`, `UISystem`의 복귀 대상 기억·복귀·검사(`UISystem.cs:339·416-423·546`). 이 기능을 켠 UI는 Clicker 하나였습니다. 다른 씬(StageSelect·Combat)에는 대응하는 것이 없습니다.

## 3 확인

- 컴파일·재생은 Unity에서 확인 대기입니다(외부 빌드는 이번에 돌리지 않음).
- `Boot` 파일은 전투 작업 영역이라 그쪽에도 같은 내용을 알립니다.

## 2026-10-29 덧붙임 — `Clicker`의 기본과 같은 재정의 (R-f, 제안, 미전달)

- 출처: [검사 후속 정리 계획 §6.9](../plans/2026-10-26-lobby-stage-review-cleanup-plan-Hojin.md#69-phase-r-f--묶음-8-작은-정리-묶음-7-요청) R-f 묶음 8, 지적 UL9.
- `Screens/Lobby/Clicker.cs`의 `BindUIService`·`GetUIData`·`Open`·`Hide` 재정의 넷은 `UIBase` 기본과 같은 일을 합니다. 기본 `Open`·`Hide`는 표시 알림(`NotifyOpened`·`NotifyHidden`)도 부르지만, `OnEnable`·`OnDisable`이 같은 알림을 한 번만 부르므로 결과는 같습니다.
- 제안: 넷을 걷고 `ID`·`RestoreWhenHistoryEmpty`만 남기기. 현강님 파일이라 고치지 않았습니다.
- 같은 모양이던 호진 파일 `Screens/Stage/StageLegendUI.cs`는 이번에 넷을 걷었습니다(`ID`·`Policy`만 남음).
