# 로비 설정의 공용 UI 이력 재진입 문제 — UI 작업자 전달

- 작성자: 호진(이호진)
- 작성일: 2026-10-12.
- 상태: **미전달·미수정**. 사용자가 이번 LV3에서 공용 `UISystem`을 수정하지 않고 전달 내용만 정리하도록 요청했다. 이후 설정 UI와 관련 연결·테스트만 되돌리도록 요청했으며, 해당 설정 타입과 재현 테스트 소스는 철회됐다. 공용 UISystem 자체의 문제를 해결한 것은 아니다. (→ 바뀜: 지금은 현강님 `OptionUI`(`UIId.Setting`)가 있다 — [로비 안내 §9](../viewDocs/lobby-guide-Hojin.md#9-설정-ui). 이 문서가 요청한 이력 재진입 재검증의 결과는 이 문서에 기록돼 있지 않다.)
- 범위: LV3 설정 화면의 일반 UI 이력 사용 중 확인한 기존 공용 서비스의 재진입 경계다. Core·Camera 변경 요청은 없다.
- 당시 실행 기록 원본: [로비 계획 §3.4 LV3](../plans/2026-10-11-lobby-plan-Hojin.md#34-lv3-공통-팝업-입력-수명과-설정-ui-철회)(옛 §14.3). 현재 철회 범위·검증·UI 작업자 소유권은 [§3.4](../plans/2026-10-11-lobby-plan-Hojin.md#34-lv3-공통-팝업-입력-수명과-설정-ui-철회)(옛 §14.4)와 [로비 안내 §9 설정 UI](../viewDocs/lobby-guide-Hojin.md#9-설정-ui)(옛 2부 설정 UI 인수 조건).

## 1. 확인된 현상

**아래는 철회 전 실제 서비스 재현 기록이다.** 현재 설정 UI/입력·로비 연결은 미연결이다. UI 작업자가 실제 화면/fixture를 제공하면 같은 순서를 다시 검증해야 한다.

일반 설정 열기·닫기·Back과 ESC 열기는 별도 검증한다. 이 문제의 조건은 **`SettingsUI.Hide()`의 닫힘 알림 중 같은 설정창을 다시 여는 경우**다. 예를 들어 이전 연결을 닫는 콜백이 로비 수신자를 재연결하고 설정을 다시 열 수 있다.

실제 `UISystem.CloseUI`의 순서는 `CloseChildrenInternal(id)` → `targetUI.Hide()` → `_openRoots.Remove(id)`(지금 코드 이름은 `_openUIs`, 순서는 그대로) → `RemoveFromHistory(id)`다. `Hide()`의 동기 알림에서 새 `OpenUI(Setting)`가 실행되면 새 표시와 입력 Layer가 생기지만, 바깥 CloseUI의 마지막 두 줄이 새 열림 기록과 Back 이력까지 지운다. **창은 보이는데 `BackCloseUI()`가 닫지 못한다.** 일반 창 전환의 이력과도 불일치할 수 있다.

근거 파일:

- [UISystem.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/UISystem/UISystem.cs): `CloseUI`, `OpenUI`, `BackCloseUI`.
- 당시 `Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/UISystem/Screens/Global/SettingsUI.cs`: Hide에서 표시를 먼저 닫고 `OpenStateChanged(false)`를 보냈다. 이번 철회로 파일/관련 `.meta`를 제거해 현재 소스 링크는 두지 않는다.
- 당시 `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Lobby/LobbyPopupCoordinator.cs`: 오래된 false 알림 뒤 실제 IsOpen=true라면 새 입력 수명을 지켰지만 공용 UI 내부 이력에는 접근하지 않았다. 이번 철회로 파일/관련 `.meta`를 제거했다.

## 2. 실제 서비스 재현과 검증 증거

당시 `Assets/Tests/EditMode/Framework/UIHistoryReproductionTests.cs`의 `CloseServiceObserver_ReopensSettings_AndKeepsBackHistory`는 SettingsUITests의 실제 UISystem·SettingsUI fixture를 재사용했다. 닫힘 관찰자가 한 번만 다시 열도록 하고 다음 순서로 검사했다. 두 테스트 소스와 관련 `.meta`는 이번 설정 철회에 포함됐으며 현재 같은 이름으로 실행할 수 없다.

1. `OpenUI(Setting)`.
2. `CloseUI(Setting)`; Hide의 닫힘 알림에서 `OpenUI(Setting)` 1회.
3. 새 설정의 `IsOpen=true`를 확인.
4. `BackCloseUI()`.
5. 기대 `IsOpen=false`, 실제 **true**.

Unity 컴파일 성공 뒤 해당 행동 실패를 확인했다. 증거는 `.superpowers/sdd/2026-10-11-lobby-plan/LV3/review-red.xml`과 `.log`다(git에서 제외되는 폴더라 작성한 PC에만 있다. 전달 받는 쪽은 위 1~5번 순서가 재현 방법이며, 원본이 필요하면 따로 첨부한다). 당시에는 SettingsUITests에 같은 검사가 있었으며 사용자 보류 결정 뒤 별도 명시 재현 클래스로 옮겼다. 이 실행은 24건 중 21통과·3실패였고, 다른 두 실패는 새 설정 글자의 한글 폰트와 2560×1080 닫기 위치였다. 두 표시 문제는 LV3 범위에서 보완하고 따로 검증한다.

별도 클래스로 옮긴 뒤에도 위 메서드를 이름으로 직접 실행했다. 최종 재현 결과는 **1건 중 0통과·1실패, Unity exit 2**이며 `Expected: False / But was: True`로 같은 이력 손실을 확인했다. 증거는 같은 디렉터리의 `ui-history-deferred.xml`, `.log`, `.exit`다. 설정 패널의 화면 배치 보완과 임시 캡처 코드 제거 후 실행한 결과이며, 컴파일 오류로 실패한 것이 아니다.

이 공용 이력 재현 1건은 당시 사용자 보류 결정을 반영해 **Explicit 별도 재현 검사**로 실행했다. 현재는 설정 관련 테스트와 함께 제거됐으며 아래 전체 이름은 역사상의 실행 식별자다. 현재 `-testFilter`로 재실행할 수 없고 파일 제거를 문제 해결이나 통과로 표시하지 않는다. UI 작업자가 실제 화면/fixture로 아래 행동을 다시 작성한 뒤 명시 필터와 정상/재진입 회귀를 실행해야 한다.

```text
ProjectL.Tests.Framework.UIHistoryReproductionTests.CloseServiceObserver_ReopensSettings_AndKeepsBackHistory
```

당시 Coordinator/View의 fake UI 검사는 모달 입력 소유와 교체 순서를 검사했다. fake는 열린 목록을 Hide 전에 제거하므로 공용 UISystem의 Hide 후처리 이력 문제를 대신 검증하지 못했다. 현재 설정 관련 fake 검사도 제거돼 있으며 위 실제 서비스 실패 XML/log와 재현 순서를 문서 증거로 유지한다.

## 3. 검토 요청과 최소 수정 제안

공용 서비스 소유자가 `CloseUI`에서 **이전 창의 열림·이력 기록을 Hide 전에 종료하는 순서**를 검토해 달라. 공개 API는 유지하며, View가 서비스의 내부 이력을 조작하는 우회는 추가하지 않는다.

최소 제안은 기존 `_openRoots.Remove(id)`(지금은 `_openUIs.Remove(id)`)와 `RemoveFromHistory(id)`를 `targetUI.Hide()` 바로 앞에 옮기는 것이다. 이 문서에는 제안만 기록했으며 공용 UISystem 파일은 수정하지 않았다. 자식 UI 닫기·파괴·Hide 예외·동기 재열기·씬 재연결에서도 해당 순서가 FW UI 계약과 맞는지 공용 소유자가 확인해야 한다.

검증할 범위:

- 위 실제 서비스 재현의 새 설정 Back 닫기.
- 일반 Root/Child UI 이력, 고정/글로벌 UI, Hide 예외와 중복 닫기.
- 로비 설정 재연결·이동 취소·입력 복귀·fault/Shutdown.
- 기존 상점·아티팩트·카드·글로벌 UI 회귀.

당시 LV3에는 기존 8번 Option/OptionPannel을 재사용한 설정 열기·닫기를 연결했으나, 이후 사용자 요청으로 설정 UI와 입력/로비 연결·관련 테스트를 철회했다. 설정 화면·버튼·일반 UI 등록/닫기·표시 계약은 UI 작업자가 제공하고 View는 실제 계약 인수 및 사용자 후속 요청 뒤 연결한다. 상세 설정 항목, 다른 씬의 설정 호스트, 실제 로비 얼굴/덱/아티팩트 운영 연결은 후속 Phase/계약 범위다.

## 4. 철회 뒤 현재 상태와 인수 요청

GlobalUICanvas·UIId·Factory의 이번 설정 추가분은 LV3 이전 상태로 복원하고 설정 타입/연결·테스트를 제거한다. 공통 PopupInputSession·Stage 어댑터·기존 상점 정책과 설정에 독립된 Deck 표시·기존 로비/LVM3/아티팩트/카메라는 유지한다. 공용 UISystem 소스는 이 철회에서도 수정하지 않았다.

UI 작업자에게 이 문서의 동기 Hide→다시 Open→Back 순서와 실제 실패 XML/log를 전달하고 새 설정 UI/fixture에서 다시 검증하도록 요청한다. 화면 제공·이력 해결·View 입력 재연결을 각각 구분해 인수하며, 삭제된 테스트 이름이 현재 실행되지 않는다는 이유로 공용 문제를 종료하지 않는다. 문서와 증거는 아직 외부로 전달하지 않았다.
