# 현강님 공유 — 로비 아티팩트 팝업의 UI 연결

- 작성자: 호진(이호진)
- 최초 작성: 2026-10-06, 호진(View). **P6 현재 구조로 다시 작성: 2026-10-15.**
- 대상: 현강님(UI 담당). 상태: **작성 완료·미전달**, 사용자가 직접 전달한다. 외부 회신·합의 기록은 없다.
- 기준: [View 구조 설계](../specs/2026-10-13-view-structure-design-Hojin.md)(옛 비전투 View 설계 §3~§6), [View 구조 계획 실행 기록](../plans/2026-10-13-view-structure-plan-Hojin.md). 최종 UI 접점은 [P6 UI 공유 메모](2026-10-15-noncombat-view-scope-ui-share-hyeongang-Hojin.md)와 함께 본다.
- 이전 StageSelect 열기 버튼·독립 Session 설명과 전달 보류 배너를 이 본문으로 대체한다. 초기 UI 이전·SA/P7 검증은 §6·§7·§9의 과거 이력으로 남긴다.

## 0. 한눈에

아티팩트는 **로비 시설에서 여는 팝업**이다. `LobbyViewSystem`이 씬 루트를 찾고 `LobbyViewScope`를 생성·게시한다. Scope가 `ArtifactPopupViewFactory`로 만든 `ArtifactPopupController`를 소유한다. Controller는 표시·드래그·수신기·결과 소비를 맡고, 그 안의 `PopupViewScope`가 UI 열기/닫기·열림 알림·입력 Layer를 맡는다. 독립 `ArtifactViewSystem`·`ArtifactPopupSession`은 제거했다.

2D 화면은 `UIBase` 기반 `ArtifactPopupUI`, 3D 보드는 `ArtifactPopupView`다. UI는 `IArtifactPopupDisplay`로 받은 표시값을 그리며 Buffer·DB·Core를 직접 읽거나 호출하지 않는다. 버튼/목록 입력은 `UIInputBinding`·`IUIInputReceiver`를 사용하고 Inspector On Click은 비워 둔다.

## 1. 사용한 현강님 구성 요소

| 구성 요소 | 현재 사용 방식 |
|---|---|
| `UIBase`·`UIId.Artifact` | 화면 ID·Open·Hide·표시. 화면 타입·네임스페이스는 기존 `Screens/Stage`에 유지 |
| `IArtifactPopupDisplay` | UI 소유 표시 계약. `IsOpen`·동기 `OpenStateChanged`·`SetCanOpen`·`BindClose`·목록/상세 표시·좌표 판정 |
| `UIInputBinding`·`IUIInputReceiver` | 닫기 Button·목록 슬롯의 Click/Hover를 UI가 받고 닫기는 View 수신기로 전달. 열기 슬롯 -1은 사용하지 않음 |
| `IUISystemService` | `PopupViewScope`가 `OpenUI(UIId.Artifact)`·`CloseUI(UIId.Artifact)` 호출. UI의 `GetUIData`·`BindUIService`는 인자 검사만 수행 |
| `ArtifactPopupViewScreen` | UI 표시 계약을 View 내부 `IPopupViewScreen`으로 감싸 Scope의 이벤트 구독/해제를 전달 |
| `IPopupViewHost` | 현재 부모 연결 확인·호스트 차단용 View 내부 계약. UI는 구현하거나 참조하지 않음 |

`UIBase`·`UISystem`·`UIInputBinding`·UIId의 기반 계약과 UI 소유 파일은 P1~P5에서 바꾸지 않았다. View 동작 객체에는 `XxxPopupController`, UI 표시 데이터에는 기존 `XxxContent` 이름을 쓴다.

## 2. 씬 구성 (Lobby)

`Lobby.unity`의 `ArtifactPopupCanvas`에 기존 `ArtifactSceneRoot`·`ArtifactPopupView`·`ArtifactPopupUI`가 있다. 자식 `Panel`이 화면을 담고 `PoolArea`에 목록 슬롯을 만든다. 닫기 Button은 화면 안에 있으며 **`ArtifactOpenButton`은 없다.** 로비의 아티팩트 시설 상호작용이 열기를 요청한다.

`ArtifactPopupUI.Hide`는 Panel·상세 표시를 정리하고 실제 열림 상태가 바뀔 때 `OpenStateChanged(false)`를 동기로 한 번 보낸다. Stage 폴더에 남은 화면 파일명이 StageSelect 진입을 뜻하지 않는다. 저작 필드 설명은 [UI 화면 문서](../viewDocs/artifact-guide-Hojin.md#2-artifactpopupui-연결)를 참고한다. P6에서는 씬·프리팹·UI 코드를 편집하지 않는다.

## 3. 동작 흐름

```text
로비 시설 → LobbyFacilityActions → ArtifactPopupController.Open
  → PopupViewScope: 호스트 차단 → Artifact Layer → UISystem.OpenUI
  → ArtifactPopupUI.Open → 동기 OpenStateChanged(true)
  → Controller.Show: 3D·슬롯·닫기 수신기·카메라 렌더 구독 연결

뒤로가기 Button → UIInputBinding → ArtifactPopupUI.Click(-2)
  → BindClose 수신기 → Controller.Close → PopupViewScope.Close
  → UISystem.CloseUI → Hide 알림에서 표시/수신 정리
  → CloseUI 반환 뒤 자기 Layer 반납·로비 입력 복귀

외부 Hide/Back/다른 화면 교체
  → 같은 호출에서 표시·드래그·슬롯·카메라 렌더 구독 정리
  → 다음 Tick에 자기 Layer 반납·호스트 복귀 시도(다른 Layer는 보존)

씬 이탈/공용 오류 → LobbyViewSystem.Detach/StopInput
  → LobbyViewScope.Dispose → Controller.Dispose → PopupViewScope 정리
```

열림 알림의 수명 구독은 `PopupViewScope` 한 곳이며 UI 입력 수신과 별개다. 외부 열림은 다음 Tick, 연결 때 이미 열린 화면은 Start에서 넘겨받는다. 일반 Hide는 제출한 편집을 취소하지 않는다. 숨김 중에도 일치 결과를 소비하되 UI를 다시 열지 않는다.

목록·상세·상태 문구는 Refresher·상태 표시 객체가 Buffer와 View DB로 구성해 UI에 전달한다. 드래그·Q/E 회전·회수는 InputSystem의 Artifact Layer 입력이다.

## 4. 현재 프로젝트 결정과 UI 담당 확인 항목

팝업 수명·다음 Tick 복귀의 정책 원본은 [View 구조 설계 §4.2](../specs/2026-10-13-view-structure-design-Hojin.md#42-popupviewscope)(옛 비전투 View 설계 §4.2 `PopupViewScope`)다. UI 알림 안에서는 Layer를 바꾸거나 UISystem을 재귀 호출하지 않는다. 다른 UI가 Layer를 가져갔다면 그 Layer와 로비 시설 차단을 유지한다. 그 화면이 닫힌 뒤의 자동 복귀는 현재 지원 범위가 아니다.

`OpenStateChanged(bool)`만으로는 숨김 사유·UI 전체 dispatch 경계를 모두 알 수 없다는 한계가 남는다. 숨김 사유 API를 인수하기 전에는 모든 외부 전환의 복귀를 보장하지 않는다. `SetCanOpen`은 현재 화면의 닫기/목록 입력 허용이며 옛 열기 버튼을 뜻하지 않는다. 부모 Scope가 교체됐으면 이전 팝업이 새 연결의 같은 ID 화면·수신기·Layer를 정리하지 않는다(D18).

## 5. 현강님이 코드를 바꾸실 때 아티팩트에 영향이 있는 경우

| 변경 지점 | 영향 |
|---|---|
| UIBase 추상 멤버·바인딩 순서·UIId | 화면 구현·등록·어댑터 ID |
| UIInputBinding·IUIInputReceiver 서명/허용 조건 | 닫기·목록 Click/Hover |
| IArtifactPopupDisplay·알림 동기성·BindClose | 공용 Scope 수명·수신기 연결 |
| OpenUI/CloseUI 이력·HideAllUI/Back·화면 교체 | 외부 숨김·다음 Tick 복귀 |
| 씬 재바인딩·이미 열린 화면의 Hide 보장 | Start에서 기존 화면을 넘겨받는 경로 |

## 6. 테스트·검증

P6는 문서 검사이며 Unity·테스트·빌드·Play·Profiler를 다시 실행하지 않는다. 10/06 UI 이전, SA/P7, 병합 보완의 검증은 각각 [팝업 계획](../plans/2026-10-05-artifact-popup-plan-Hojin.md), [스테이지 맵 계획 §3.2](../plans/2026-10-10-stage-map-plan-Hojin.md#32-sa-구조-정렬)·[§4.2](../plans/2026-10-10-stage-map-plan-Hojin.md#42-병합-후-연결-보완)(옛 SA 계획·병합 검토 §7)의 과거 기록이다. 당시 Unity 6000.0.73f1 UI 이전 관련 테스트 통과와 기존 UI 배치 실패 관찰을 현행 전체 결과로 대체하지 않는다.

현재 구조의 P3 아티팩트·사용자 Play 4항목과 P5 최종 회귀·기존 실패·Profiler 한계는 [View 구조 계획의 실행 기록](../plans/2026-10-13-view-structure-plan-Hojin.md)(옛 비전투 View 계획 실행 기록)을 따른다. 전체 통과·외부 인수 완료를 뜻하지 않는다. `ArtifactPopupUITests`는 실제 UISystem·UIInputBinding을, Boot smoke는 로비 연결을 검사한다.

## 7. 변경 이력

- 2026-10-06: UI 이전. 사용자 결정으로 Artifact 전용 ESC 닫기를 제거하고 뒤로가기 Button만 사용. Common/OpenSettings는 별개다.
- 2026-10-11: SA/P7의 입력 Buffer·Flow/Plan/Job·번호 있는 Core 결과로 전환.
- 2026-10-13: LV5에서 StageSelect 열기 버튼을 제거하고 로비 시설로 이전.
- 2026-10-15: P3에서 독립 System/Session을 로비 Scope에 흡수. P6에서 이 공유 문서를 다시 작성. 외부 전달·회신은 미확인.

## 8. 확인·회신

| 번호 | 확인할 현재 접점 | 현강님 회신 |
|---|---|---|
| Q1 | 기존 UIId·화면 위치·표시 인터페이스를 유지하는 View 어댑터 연결 | 미회신 |
| Q2 | 닫기/목록은 UIInputBinding, 열기는 로비 시설인 현재 경로 | 미회신 |
| Q3 | 동기 열림 알림과 외부 숨김/열림의 다음 Tick 입력 처리 | 미회신 |
| Q4 | 향후 숨김 사유·UI 전체 dispatch 경계 계약 제공 여부 | 미회신 |

옛 StageSelect 열기 버튼 위치·하위 클래스 resolver 요청은 현행 확인 항목에서 제외했다. 실제 회신 없이 합의 완료로 표시하지 않는다.

## 9. P7 입력·결과 경로 교체 (2026-10-11, 사용자 승인, 미전달)

P7은 입력 Buffer → Flow/Plan/Job → 번호가 있는 Core 결과로 바꾼 과거 단계다. UI는 현재도 표시·동기 알림·입력 전달만 맡는다. API·처리 번호·교체 경계의 원본은 [P7 Core 인계](2026-10-11-stageselect-core-replacement-handoff-Hojin.md#2부-stageselect-p7-아티팩트-core-구현교체-인계)이며 P6 문서 갱신을 Core 변경·재검증·외부 전달로 해석하지 않는다.
