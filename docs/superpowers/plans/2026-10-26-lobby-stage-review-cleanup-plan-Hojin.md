# 로비·스테이지 맵 검사 후속 정리 계획

- 작성자: 호진(이호진)
- 상태: 1~4·6번, 로비 안내 글자, 묶음 2~8의 R-0~R-f 코드가 끝나 모두 커밋됐다(마지막 `24c21b3a3`, 10-29). 남은 것은 R-g 측정, Unity 미확인 몇 개(§2), 공유 메모 전달, 묶음 7 합의다.
- 설계: [검사 후속 정리 설계](../specs/2026-10-26-lobby-stage-review-cleanup-design-Hojin.md)
- 정리: 2026-10-27 `2026-10-26-stage-node-parts-plan-Hojin.md`, `2026-10-26-required-view-dependencies-plan-Hojin.md`, `2026-10-27-stage-small-cleanup-plan-Hojin.md`, `2026-10-27-stage-popup-submission-plan-Hojin.md`를 이 문서로 합쳤다(옛 판 git `10914570`). 2026-10-29 다시 줄임: 끝난 Task·체크박스·완료 조건·Review Focus·실행 기록·검토 상세는 git `a8612321e`.
- 번호 주의: TP 번호는 작업마다 따로 매겼다. 2번의 TP-1~TP-14(§3.4·§3.5), 4번의 TP-1~TP-9(§3.12), 묶음 2~8의 TP-R1~TP-R13(§4.2)은 서로 다른 항목이다. Phase 이름 "R-a·R-b"도 2번(필수 의존, 10-26~27)과 묶음 2~8(10-28)에 겹친다. 이 문서는 2번 쪽을 "R-a(2번)", 묶음 쪽을 "R-a(묶음 2)"로 적는다.

## 1 진행 요약

공통 규칙: Phase가 끝나면 멈추고 보고한다. 커밋·push는 사용자가 한다. 다른 작업자 시험은 묻지 않고 호출만 맞추고 기록한다(10-26 사용자). 10-27 `ProjectL.Tests.Framework` 시험 묶음이 지워진 뒤(용근님 `660a10e7`, 병합 `358063e9`)로는 새 시험을 만들지 않는다. 컴파일은 외부 C# 빌드로 본다(10-27부터 `ProjectL.Systems.csproj` + 지금 파일 목록으로 만든 임시 targets). Unity 컴파일·재생은 사용자가 확인한다.

| 작업 | 내용 | 확인 | 커밋 |
|---|---|---|---|
| N-1 (1번) | 노드 부품 묶음 `StageNodeViewParts`, 필요 없는 것 일곱 걷기 | 사용자(10-27) | `45b20161` |
| N-2 (1번) | 로비 카메라 이름 상수·유일성 검사 걷기 | 미확인 | `8ee9984e` |
| 3번 | `CombatEntryRequest.IsPrepared`, View 단계 직접 비교 넷 → 질문 | 사용자(10-27) | `45b20161` |
| R-a (2번) | 로비 오류 경계·출발 싱크 필수 | 사용자(10-27) | `45b20161` |
| R-b (2번) | 스테이지 맵 오류 경계·결과·진입 시도·상점·회복·덱 편집 필수 | 사용자(10-27) | `b1dd4962` |
| C-2 (6번) | 팝업 Layer `"StageMapPopup"` | 미확인 | `b1dd4962` |
| C-3 (6번) | 참조 없는 파일 셋 삭제 | 미확인 | `b1dd4962` |
| C-1 (6번) | `StageProgressData.IsStart` | 미확인 | `42be34e6` |
| S-a (4번) | 공통 기반 `StageOfferPopupUI`(동작 같음) | 사용자(10-27) | `354bd8d7d` |
| S-b (4번) | 수신자 계약, 컨트롤러가 유일한 제출자 | 재생 일부 미확인(§2) | `354bd8d7d` |
| S-c = R-0 (4번) | 공유·기록 | — | `24c21b3a3` |
| 로비 안내 글자 | Clicker 걷음, 글자를 `LobbyTexts` 묶음으로, 화면마다 제목 | 사용자(10-28) | `24c21b3a3` |
| R-a (묶음 2) | 스테이지 맵 조립: 생성 계약·서비스 묶음·Scope 부품 넷 | 사용자(10-28) | `24c21b3a3` |
| R-b (묶음 3) | 로비 조립·입력 판정·출발 거절 안내 | 사용자(10-29) | `24c21b3a3` |
| R-c (묶음 5) | `StageEntryIdentity`, 비전투 팝업 고르기 한 곳 | 사용자(10-29) | `24c21b3a3` |
| R-d1·R-d2 (묶음 6) | 새 Run Job 하나, Stage Flow → 사실·Plan | 사용자(10-29) | `24c21b3a3` |
| R-e (묶음 4) | DB SO: Data 나누기·프리팹 규칙·시설 검사·회복 참조·이름 | 사용자(10-29) | `24c21b3a3` |
| R-f (묶음 8·7) | 작은 정리, Core·민형·용근·현강 메모 | 미확인 | `24c21b3a3` |
| R-g | 블라인드 A/B 측정 | 미착수 | — |

`24c21b3a3`에는 증강(TraitGrowth) 초안 코드도 같이 들어갔다. 사용자 확인 날짜는 다음 Phase 착수 때 사용자가 "확인함"이라 한 날이다.

## 2 남은 일

- [ ] R-g 측정(§4.10).
- [ ] Unity 확인(사용자). 10-27 시험 묶음 삭제로 EditMode 목록은 없고 컴파일·재생만 남는다.
  - N-2: 로비 진입 → 고정 카메라 화면 → 아티팩트 팝업 카메라 전환·복귀. (로비 카메라 코드는 묶음 R-b에서 다시 바뀌어 그 확인이 대신할 수 있다.)
  - C-2: 스테이지 선택 → 상점·회복·덱 편집 열고 닫기, 로비 덱 팝업.
  - C-3: 로비·스테이지 선택 씬에 Missing Script 경고 없음.
  - C-1: 시작 노드 외형·완료 표시·현재 위치·카메라 시작 위치.
  - S-b: 상점 빠른 연타·키보드 Submit으로 두 번 제출되지 않음, 회복 제출 중 닫았다 다시 열기. (지운 T5 새 시험이 보려던 동작이다.)
  - R-f: 컴파일, 로비 진입·아티팩트 편집(편집·프리뷰), 스테이지 맵 현재 위치 마커·배경, 아티팩트 보드, 맵 범례 열고 닫기.
- [ ] 공유 메모 전달(§5, 모두 미전달).
- [ ] 묶음 7 합의: F2 seed(천명), F3 `SceneNodeOrder`(용근), F8 Plan 입력 좁히기·F10(민형). 요청·제안은 써 두었다(§5).
- 보고만(고치지 않음):
  - [View 개발 안내](../viewDocs/view-development-guide-Hojin.md)의 "현재 덱 팝업이 이 경우다(부모 Scope에서 직접 생성)"는 낡았다. 덱 편집도 Factory로 만든다(2번 R-b 때 발견).
  - 미등록 사본 회복 표 `InGameGroup/Stage/Event/Recovery/`(선택지 1·2, 값 다름)는 참조가 없어졌지만 지우지 않았다(R-e).
  - `LobbyViewServices`가 `artifactConfig.Validate()` 등을 다시 부른다(R-b 결정, R-f 검토 L6). 걷을지는 따로 정한다.
  - `JobRegisterSystem.Register`에 재진입 막기가 없다(민형님 파일, R-d2 검토 L1). 전투 진입 갈래에 Job을 더하면 순서가 꼬일 수 있다.
  - N-1 때 본 범위 밖 기존 실패(`CombatEntryIntegrationTests`·`DevelopmentNavigationIntegrationTests`·`ShopStagePlaySmokeTests`·`StageMapDepthCenteringBootSmokeTests`·`StageMapNodeMovementBootSmokeTests`·`DataBaseAssetTests`)는 10-27 시험 묶음 삭제로 파일이 없어져 할 일이 없다.

<a id="3-작업별-결과"></a>

## 3 작업별 결과 — 1~4·6번

### 3.1 N-1 노드 부품

- 사용자 "머지 했으니 작업 진행"(10-26). SC-1·LB-1 병합을 확인하고 `d8b6bb1f`에서 착수했다(Q-N4).
- 한 것: `StageNodeViewParts`(+ `.meta`), `StageMapViewDataSO`, View 여섯, 프리팹 `StageMapNode.prefab`(GUID·남은 값 보존), `StageMapView_Default.asset`(폐기 칸 여덟만 지움). 다른 작업자 줄은 천명님 시험 `DevelopmentNavigationIntegrationTests:619-621`뿐이었다.
- 계획과 다른 점: 대상 시험을 돌리며 시험 쪽 결함(아이콘 소유 단언, 지운 에셋 경로 고정물, `BindUI` 누락, 지연 Destroy 로그 수)을 함께 고쳤다. 운영 검증은 약하게 하지 않았다.
- 확인: 에이전트가 Unity TestRunnerApi로 대상 시험을 돌려 통과, 이어서 사용자 확인. 커밋 `45b20161`.

### 3.2 N-2 로비 카메라

- 사용자 "LobbyViewScope에 있어야 하는지 확인만 한번더 해주시고 진행해주세요"(10-26). 한 파일이라 Task 계획 없이 했다.
- 한 것: `LobbySceneRoot`의 상수·이름 비교·`ValidateUniqueCameraName` 삭제, 컨트롤러가 카메라 참조를 받음.
- 계획과 다른 점: 설계 "`LobbyViewScope:187` 그대로" → 컨트롤러가 참조를 받음(사용자 지시). 그 컨트롤러는 묶음 R-b에서 공용 `ViewCameraHandleLease`로 바뀌어 지금은 없다.
- 확인: 외부 빌드 오류 0, Unity 미확인. 커밋 `8ee9984e`.

### 3.3 3번 진입 요청 질문

- 사용자 "3번을 진행해줘"(10-27). `CombatEntryRequest.IsPrepared`(용근님 파일)와 View 넷. 외부 빌드 오류 0, 사용자 확인, 커밋 `45b20161`.

### 3.4 R-a (2번) 로비

- 사용자 "TP-1~TP-4가 SOLID, SSOT, FW의 규칙과 규격에 맞고, 우리 구조에 맞게 적성적으로 넣고 Phase R-a 진행해줘"(10-26).

| 번호 | 정한 것 |
|---|---|
| TP-1 | 출발 싱크 검사 자리: `Initialize`·`Attach`의 서비스 목록을 `RequireConnectionServices(step)` 하나로 모으고 출발 싱크를 넣음. `Attach`는 기존 연결을 끊기 전에 검사 |
| TP-2 | 시험 고정물 도우미 `BindRuntimeDeparture(system)`를 따로 둠 |
| TP-3 | `CreateP2Scope`는 싱크를 안 넘기면 새 `LobbyViewBuffer`, 오류 경계는 시험용 |
| TP-4 | 새 계약 시험 둘: `Constructor_RequiresFaultBoundary`, `MissingDepartureSinks_RejectInitializeAndAttachBeforeConnecting` |

- 계획과 다른 점: Scope는 출발 싱크를 따로 검사하지 않고 `LobbyDeparturePresenter` 한 곳만 검사했다. View 개발 안내 한 줄은 2번 R-b로 미뤘다.
- 확인: 외부 빌드 오류 0, 사용자 확인(10-27 "N-1·3번·R-a 확인하고 커밋했어"). 커밋 `45b20161`. 이 구조(`RequireConnectionServices`·`Bind*`)는 묶음 R-b가 다시 걷었다.

### 3.5 R-b (2번) 스테이지 맵

- 사용자 "TP-5~TP-14가 SOLID, SSOT, FW, UI의 규격과 규칙에 맞고, 있는 서비스를 적절하게 사용하고 있으면 Phase R-b 진행해줘"(10-27). 기준 `45b20161`.

| 번호 | 정한 것 |
|---|---|
| TP-5 | 결과·진입 시도는 `BindEntry(results, attempts)` 하나로 |
| TP-6 | 덱 편집 Factory는 `BindDeck(DeckEditPopupViewFactory)`, `PostBindService`는 없을 때만 `inGame.CoreGroup.Deck`으로 만듦(용근님 줄 `38869ec1`) |
| TP-7 | `RequireConnectionServices(string step)` 하나가 모든 연결 서비스를 검사 |
| TP-8 | Scope 생성 서명: `recovery`·`recoveryState`·`deck` 기본값 삭제·선택 인자 앞으로 |
| TP-9 | 준비(`_readyForTick`) 뒤에만 지나는 `?.`는 직접 호출. `Dispose`·`ReleaseMovement`의 `?.`는 남김 |
| TP-10 | 컨트롤러·Factory 안 `_faults == null`과 System `_shopInput?.Clear()`는 4번으로. 덱 편집 Factory·컨트롤러(용근님)는 기록만 |
| TP-11 | 공용 시험 도우미 `StageMapRequiredPartsTestKit.cs` |
| TP-12 | 새 계약 시험 둘: `Constructor_RequiresFaultBoundary`, `MissingRequiredServices_RejectInitializeAndAttachBeforeConnecting` |
| TP-13 | `StageScreenRootUI` 문구 `"[StageScreenRootUI] {필드} 연결이 필요합니다."` |
| TP-14 | R-a(2번)에서 미룬 View 개발 안내 의존성 표 한 줄 |

- 계획과 다른 점: TP-11의 리플렉션 채우기 대신 이름 있는 도우미(`AttachMissingScreens`·`BindAll` 등)를 고정물이 골라 부름.
- 다른 작업자 시험: 천명님 `DevelopmentNavigationIntegrationTests:500-501`·`:510`(`4782921e`)의 리플렉션 셋 → `BindEntry`·`BindDeck`.
- 확인: 외부 빌드 오류 0, 사용자 확인("R-b Unity 확인하고 커밋했어"). 커밋 `b1dd4962`.

### 3.6 C-2 팝업 Layer

- 사용자 "Phase C-2 진행해줘"(10-27). `StageSelectionInputLayers.Popup`, `StageMapPopupInputLayers.cs`(+ `.meta`) 삭제, 컨트롤러 셋·`StageSelectionInputFactory`·시험 참조, 안내 문서 둘.
- 계획과 다른 점: 이름 상수를 이름 파일로 옮기는 단계를 뺐다(10-27 사용자). 용근님 문서 `stage-input-guide.md:247`은 그 줄이 호진님 줄이라 직접 고쳤다.
- 확인: 외부 빌드 오류 0, Unity 미확인. 커밋 `b1dd4962`.

### 3.7 C-3 파일 삭제

- 사용자 "Phase C-3 진행해줘"(10-27). 이름·GUID 참조 0을 확인하고 세 파일과 `.meta`를 지웠다. `lobby-guide-Hojin.md` 한 문장을 고쳤다.
- 확인: 외부 빌드 오류 0, Unity 미확인. 커밋 `b1dd4962`.

### 3.8 C-1 시작 노드

- 사용자 "어 커밋했어 그래서 C-1 시작해"(10-27, 판 `e3f0d29e`). `StageProgressData.IsStart`와 View 일곱 곳. 참·거짓 방향을 줄마다 대조했다.
- 확인: 외부 빌드 오류 0, Unity 미확인. 커밋 `42be34e6`.

### 3.9 S-a 공통 기반 (4번)

- 착수: 10-27 사용자 "4번은 시작해주세요", "SOLID·SSOT·FW·UI 규칙과 규격을 지키면서". 처음 구현은 작업 트리를 비운 뒤 병합(`358063e9`)해 사라졌고, 사용자 "문서 수정을 다시 넣고 S-a를 처음부터 다시 진행해"로 같은 내용으로 다시 했다.
- 한 것: 새 `Screens/Stage/StageOfferPopupUI.cs`(+ `.meta`). `ShopUI` 253 → 101줄, `RecoveryUI` 272 → 123줄. 현강님 카드 줄과 천명님 나가기 문구 줄은 같은 동작으로 기반에 옮겼다.
- 계획과 다른 점: `CardContentAt`은 하위가 `Render` 동안만 모델을 든다(`try/finally`, 모델 참조를 보관하지 않음). 회복 표시 세대 값 증가는 `RecoveryUI.Hide` 재정의 한 줄로 남겼다(S-b에서 걷음). 시험 변경 없음.
- 바뀐 동작은 설계 §7.4(D1·D4·D6·D7).
- 확인: 임시 targets로 `ProjectL.Systems` 빌드 오류 0(경고 8은 바꾸지 않은 파일), 사용자 Unity 확인. 커밋 `354bd8d7d`.

### 3.10 S-b 수신자이자 유일한 제출자 (4번)

- 착수: 10-27 사용자 "S-a Unity 확인 문제 없어. S-b 진행해." 운영 코드만 바꿨다.
- 한 것: 수신자 계약 둘(`IShopPopupInput`·`IRecoveryPopupInput`), 표시 계약 여섯 멤버, `ShopUI` 49줄·`RecoveryUI` 51줄, 두 컨트롤러가 수신자를 구현하고 영수증 하나를 듦, 오류 경계 필수(TP-6), `RecoveryPopupViewFactory`에 제출 입구.
- 계획과 다른 점: 제출 허용 계산을 컨트롤러마다 `CanSubmitNow` 하나로 모음(계획에 없던 이름). TP-5 영수증 읽기는 내지 않음(읽던 시험이 없어짐).
- 바뀐 동작은 설계 §7.4.
- 확인: `ProjectL.Systems` 빌드 오류 0, 검사 grep(UI·표시 계약의 옛 제출 멤버 0곳, `_faults == null` 0곳). Unity: 재생 일부 미확인(§2). 커밋 `354bd8d7d`.

### 3.11 S-c 공유·기록 (4번, 묶음 2~8의 R-0)

- 10-28 사용자 "R-0 진행", 중간에 Q-R9를 "3개 유지"로 바꾼 뒤 "R-0 이어서". 코드 변경 없음.
- 현강님 상점 UI 안내 3부, 천명님 Core 요청 문서 5부(TP-R10, 알림), 설계 §7 구현 메모, View 개발 안내 팝업 규칙 한 줄, 메모리.
- 정정: 천명님 줄은 `ShopUI`의 방문 문맥·나가기 문구·명령 만들기 줄(`a5133b32`)만 옮겼고 `ShopPopupController`의 천명님 47줄은 그대로다(설계 §11의 옛 "47줄 옮김"은 틀렸다).
- 커밋 `24c21b3a3`.

### 3.12 4번 계획에서 정한 것 (TP-1~TP-9)

<a id="5-4번-task-계획"></a>
<a id="52-계획에서-정한-것-사용자-확인-대상"></a>

처음 계획은 사용자 "R-b Unity 확인하고 커밋했어. 4번 Task 계획 써줘."(10-27), TP-1~TP-9는 10-27 사용자 "4번은 시작해주세요"로 그대로 정했다. 10-27 시험 묶음 삭제로 시험 관련 Task(옛 Task 2·6)와 TP-2·TP-8은 할 일이 없어졌고, TP-5는 내지 않았다.

| 번호 | 정한 것 | 지금 |
|---|---|---|
| TP-1 | 기반의 `Awake`는 `protected void Awake()`(하위가 새로 선언하면 숨김 경고) | 그대로 |
| TP-2 | 기반 직렬화 칸은 `private`, 시험 리플렉션 도우미가 기반까지 찾음 | 시험 없어 할 일 없음 |
| TP-3 | 수신자 계약 둘, 표시 계약에 `BindInput(수신자)`, 인자는 UI가 그린 문맥·ID | 그대로 |
| TP-4 | 상점 영수증은 제출 때. 영수증이 없고 Buffer에 같은 방문 명령이 있으면 넘겨받음. "남은 입력" 검사 그대로 | 그대로 |
| TP-5 | 회복 컨트롤러 영수증 읽기 `PendingNumber`·`PendingCommand` | 내지 않음 |
| TP-6 | 상점·회복 컨트롤러·Factory 오류 경계 필수 | 그대로 |
| TP-7 | 제출 예외는 수신자 메서드 안 `try/catch`로 오류 경계에 보고 | 그대로 |
| TP-8 | 시험 도우미 수신자 | 시험 없어 할 일 없음 |
| TP-9 | System `_shopInput?.Clear()`, 상점 컨트롤러 `presentation = null` 기본값은 5번으로 | 묶음 R-a가 받음 |

### 3.13 로비 안내 글자 (Clicker → `LobbyTexts`)

설계 §12.7. 사용자 지시로 묶음 R-a보다 먼저 했다(10-28).

- 사용자가 에디터에서 글자 셋을 `LobbyPanel` 아래로 옮기고 `Clicker` 오브젝트를 뺐더니 Boot `OpenUI(UIId.Clicker)`가 "미등록" 예외를 냈다. 그래서 설계의 "Boot 한 줄은 남김"(옛 Q-G4 A)을 지킬 수 없어 Clicker를 여닫는 코드 줄을 모두 걷었다: `GameBootstrap.cs`의 `_ui.OpenUI(UIId.Clicker)`(전투 작업 영역), `LobbyViewScope`의 숨김·복귀·`Dispose` 초기화·필드 `_clickerSuppressedByFacility`(현강님 `b691d8ae` 줄).
- Q-G5 B 반영(10-28 사용자 "아티팩트에선 숨김"): `LobbyCanvasView` 칸 하나, 시설 팝업 경계 `SetPopupBlocked`에서 묶음을 끄고 켬, `Start`에서 한 번 켬. 씬은 사용자 지시("씬 파일 고쳐줘")로 YAML을 직접 고쳤다. 연결 전에는 `ArgumentNullException(facilityLabels)`로 멈췄다(사용자 보고).
- 화면마다 자기 제목(10-28 사용자 "깔끔하고 유연한 방법이면 A"): 묶음 이름 `FacilityLabels` → `LobbyTexts`, "로비" 제목을 묶음 안으로, 아티팩트 패널에 `ScreenTitle` "아티팩트". 코드는 칸 이름만 `lobbyTexts`로.
- 확인: 외부 빌드는 복원 파일이 없어(`NETSDK1004`) 돌리지 못했다. 사용자 Unity 확인 문제 없음(10-28). 커밋 `24c21b3a3`. 메모 [로비 Clicker 걷음](../handoffs/2026-10-28-lobby-clicker-removed-share-hyeongang-Hojin.md)(미전달).

<a id="6-묶음-28-task-계획-설계-12"></a>

## 4 작업별 결과 — 묶음 2~8 (R-0~R-g)

설계 §12. 10-28 사용자 "모두 A"로 설계 결정이 끝났고, 계획에서 정한 TP-R1~TP-R13은 10-28 사용자 확인(TP-R3·TP-R4·TP-R6·TP-R7은 바꿈, 나머지 그대로).

<a id="61-진행-규칙"></a>

### 4.1 진행 규칙

- 원칙 B1~B7(설계 §12.2)을 각 Phase의 기준으로 쓴다.
- 검토: 묶음 끝마다 검토 에이전트 하나, 전체 끝에 블라인드 A/B 한 번(R-g).
- 다른 작업자 운영 코드를 바꾸면 작성자를 적고 공유 메모에 넣는다. 영역 밖 결정(Core·Framework·UI 작업자 몫)은 메모·요청만.

<a id="62-계획에서-정한-것-사용자-확인-대상"></a>

### 4.2 계획에서 정한 것 (TP-R1~TP-R13)

| # | 정한 것 |
|---|---|
| TP-R1 | B2의 꼴: Factory는 생성 계약 하나(`IStageMapSceneFactory`·`ILobbySceneFactory`, 구현은 `Factory/Runtime/InGame/View/` 한 파일씩)를 System 생성자로 넘기고, System은 `PostBindService`에서 서비스를 묶음 값(`StageMapViewServices`·`LobbyViewServices`)으로 한 번 모아 씬 연결 때 루트와 함께 넘긴다. 생성 입구는 이것 하나, 계약은 Scope를 돌려준다 |
| TP-R2 | 새 파일은 지금 폴더에(`ViewSystem/StageMap/`·`Lobby/`·공용 `Common/`), 새 하위 폴더 없음 |
| TP-R3 | **바뀜(10-28 사용자 "권장대로 단순화"):** 정리 목록 부품은 두지 않고 Scope `Dispose`가 만든 순서의 역순으로 부품마다 `CleanupErrors.Try` 한 줄씩. 옛 안: `ViewCleanupList` |
| TP-R4 | **바뀜(10-28 사용자 "권장대로 단순화", "매 Tick 검사하지 않음"):** 입력 문은 Scope 안 private 판정 함수. 우리가 바꾸는 사실(팝업 열림)은 알림으로 한 곳에, 알림 없는 사실(Buffer·Layer·정지)은 쓰는 순간 읽음. 예외: 출발 거절은 Tick에서 그 값 하나만 봄. 옛 안: 사실 값 struct + Policy 정적 클래스 |
| TP-R5 | 카메라 핸들 공용 부품 `ViewCameraHandleLease`(`ViewSystem/Common/`) |
| TP-R6 | **뺌(10-28 사용자 "씬에 카메라는 하나만 있어도 된다"):** 출력 카메라는 Boot Main Camera 하나라 `Camera.main` 그대로(설계 Q-R4 B) |
| TP-R7 | 출발 거절 안내는 현강님 공용 안내 패널 `IUITextPresentation.ShowNotice(문구, 3초)`를 거절을 처음 본 순간 한 번. 새로 만드는 것은 문구 원본 `LobbyDepartureText` 하나(10-28 사용자 "단순 2") |
| TP-R8 | 새 Run Job 하나 `LifeCycles/RunStart/RunStartJob` + 받는 쪽 계약 `IRunStartOutcome` |
| TP-R9 | Stage 실행 계층: 사실 값·읽기·`StagePlan.Decide`, 처리 확인은 처리 Job 바로 뒤 확인 Job, Flow 넷 지움, 진입 시도는 `StageEntryAttempts` |
| TP-R10 | Core 요청은 호진 Core 요청 문서에 새 절로(새 문서 만들지 않음) |
| TP-R11 | 회복 이름 임시 문구: "체력 회복"·"최대 체력 비율 회복"·"체력 모두 회복" |
| TP-R12 | DM1 SO에 용근님 `ConsumableItemVisualDataSO`도 넣음, 작성자 메모 |
| TP-R13 | `LobbyFacilityView`·`LobbyFacilityKind`를 DB 폴더로(GUID 유지, `MovedFrom`), 방 시설 검사는 DB 생성 때 |

<a id="63-phase-r-0--4번-s-c-56-그대로"></a>

### 4.3 R-0

4번 S-c와 같다(§3.11).

### 4.4 R-a (묶음 2) 스테이지 맵 조립

<a id="64-phase-r-a--묶음-2-스테이지-맵-조립-q-r1r2r4r12b-b1b3b5b7"></a>
<a id="641-r-a-실행-기록-10-28-외부-빌드-오류-0바꾼-파일-새-경고-0-unity-미확인-새-시험-없음"></a>

- 착수: 10-28 사용자 "R-a 진행". 기준 판 `d78dd64a`.
- 한 것: 새 파일 10(공용 `ViewCameraHandleLease`·`ViewAttachmentInputLayers`, `StageMapViewServices`·`IStageMapSceneFactory`·부품 넷·`StagePopupViewLifetime`, Factory 쪽 `StageMapSceneFactory`). `StageMapViewSystem` 441 → 266줄, `StageMapViewScope` 490 → 255줄, 팝업 컨트롤러 셋(용근님 `DeckEditPopupController` 포함)·`DeckEditPopupViewFactory`, `InGameViewSystemFactory.cs` 한 줄. 걷은 것은 설계 §12.3.
- 계획과 다른 점: 배경음은 Scope 부품이 아니라 System에 남겼다(오류 정지로 Scope가 없어져도 정상 이탈 때 AMB를 멈춰야 함). "System 약 150줄" 목표는 266줄.
- 묶음 끝 검토: 높음 0·중간 1(Tick마다 대리자 생성 → 한 번)·낮음 7 반영. 남긴 넷(정리 목록·사실 struct·서비스 묶음 칸·이름에 View)은 이어서 사용자 "권장대로 단순화"로 모두 고쳤다: `ViewCleanupList`·`StageMapInputFacts`·`StageMapInputPolicy` 지움, 묶음에서 `Audio`·`Loading` 빼 27칸, 부품 이름에 View(`StageMapViewScene`·`StageMapViewEntry`·`StageMapViewInput`·`StageMapViewPopups`·`StagePopupViewLifetime`·`ViewAttachmentInputLayers`). 천명님 `itemVisuals` 세 줄 필수화는 Core 요청 문서 5부에 덧붙였다.
- 바뀐 동작(정리·오류 경로뿐): 표·서비스가 바인딩 때 모두 필수, 덱 편집 팝업은 오류 정지 뒤 열지 않음, 회복·덱 편집 정리 때 화면 끊기가 먼저, 순수 Buffer 읽기 뒤 연결 확인 몇 줄 걷음(B7).
- 확인: 외부 빌드 오류 0·새 경고 0, 사용자 Unity 확인(10-28, 묶음 R-b 착수 때). 커밋 `24c21b3a3`.

### 4.5 R-b (묶음 3) 로비

<a id="65-phase-r-b--묶음-3-로비-q-r1r4r5r6r12b"></a>
<a id="651-r-b-실행-기록-10-28-외부-빌드-오류-0경고는-바꾸지-않은-파일-셋뿐-unity-미확인-새-시험-없음"></a>

- 착수: 10-28 사용자 "확인함, R-b 진행을 먼저 해주세요". 묶음 R-a 단순화 규칙을 그대로 따랐다.
- 한 것: 새 파일 7(`LobbyViewServices` 20칸·`ILobbySceneFactory`·`LobbyViewScene`·`LobbyViewInput`·`LobbyViewPopups`·`LobbyDepartureText`, Factory `LobbySceneFactory`). `LobbyDeparturePresenter` → `LobbyDepartureFlow`(`git mv`, GUID 유지), `LobbyCameraController` 지움. `LobbyViewSystem` 344 → 228줄, `LobbyViewScope` 274 → 135줄. 출발 요청 질문, `LobbyPlan`·`LobbyTransitionJob`이 질문만 읽음. 덱 버튼 닫기 우리 줄 걷음(Q-R5 ①).
- 계획과 다른 점: 출발 요청 질문은 둘이 아니라 셋(`IsQueued`·`IsPrepared`·`IsTransitionSucceeded`, "끝남"은 이미 있는 `CanResubmit`). 판정 함수는 셋이 아니라 둘(`IsInteractionBlocked`·`CanDepart`). 필수화를 아티팩트 팝업 Factory 안까지 넓힘. 로비 카메라 핸들은 빌린 직후 정지 확인을 하지 않음(맵과 같음).
- 바뀐 동작(정리·막힘 시점뿐): 가는 도중 막히면 그 이동은 다음 Tick에 버림, 로비 진입 때 덱 UI를 다시 닫지 않음(Boot가 닫음), DB 값 검사는 바인딩 때 한 번.
- 묶음 끝 검토: 높음·중간 0, 낮음 6. L1 `lobbyTexts` 검사를 `LobbyCanvasView.ValidateComposition`으로 옮김. 기록만: L2 방 프리팹·아티팩트 설정 검사가 부트로 옮겨져 로비 데이터가 잘못되면 Combat 직접 시작도 부트에서 멈춤(의도 B1·B2).
- 출발 거절 안내 확인법: `PlayerLoadout_1`의 서브 유닛 하나를 적 전용 유닛으로 잠시 바꾸고 출발 → "출발 데이터가 올바르지 않습니다." 3초.
- 공유 없음(모두 호진 영역). 확인: 외부 빌드 오류 0, 사용자 Unity 확인(10-29, R-d 착수 때). 커밋 `24c21b3a3`.

### 4.6 R-c (묶음 5) 진입 비교·노드 문맥

<a id="66-phase-r-c--묶음-5-진입-비교노드-문맥-q-r8q-r9"></a>
<a id="661-r-c-실행-기록-10-29-외부-빌드-오류-0경고는-바꾸지-않은-파일-셋뿐-unity-미확인-새-시험-없음"></a>

- 착수: 10-29 사용자 "R-c 먼저"(증강 TG-d보다 먼저).
- 한 것: 새 `StageEntryIdentity`(+ `.meta`), 비교 다섯을 `==` 하나로. 비전투 팝업 고르기를 `StageMapViewScope.RestoreNoncombat` 한 곳(Buffer 상태만)으로, `ShopPopupController`의 `StageType.Shop` 읽기 걷음. 이제 StageMap View는 `StageType`을 읽지 않는다.
- 계획과 다른 점: 새 형식 `StageMapNodeContext`를 만들지 않고 Scope 판정에 둠(판정 두 줄). `CurrentShopData.VisitKind`는 읽지 않음(나눠 그리는 소비자가 아직 없음, 메인 강화 상점 때).
- 바뀐 동작: 상점·회복 상태가 모두 없는 Event 노드를 다시 고르면 오류 대신 아무 팝업도 열지 않음(Q-R9 의도).
- 묶음 끝 검토: 높음·중간 0, 낮음 3(기록만). 확인: 외부 빌드 오류 0, 사용자 Unity 확인(10-29). 커밋 `24c21b3a3`.

### 4.7 R-d (묶음 6) Stage 실행 계층

<a id="67-phase-r-d--묶음-6-stage-실행-계층-q-r7-tp-r8tp-r9"></a>
<a id="670-착수-전-상세-안-10-29-코드-변경-없음-결정-대기-q-rd1q-rd3"></a>

착수 전(10-29 사용자 "확인함(R-b·R-c Unity 문제 없음), R-d 진행") 상세 안을 쓰고 셋을 물었다.

| # | 정한 것 | 결정(10-29 사용자) |
|---|---|---|
| Q-Rd1 | 진행 단위 | A 둘로(R-d1 → Unity 확인 → R-d2) |
| Q-Rd2 | 증강 `TraitGrowthFlow` | B 증강도 R-d2에서 사실·Plan으로. 증강 세션이 보낸 계약 둘·지킬 조건 일곱은 설계 §12.3 묶음 6 |
| Q-Rd3 | 상점·회복 Job 등록 자리 | A Stage `Next` 하나로(사용자 "A안으로"). 열쇠는 판단하는 주인(수명)마다 하나. 쓰이지 않게 된 열거 값은 R-d2에서 지움(사용자 "지움, 민형님 공유 메모에 적을 필요는 없음") |

<a id="671-r-d1-실행-기록-10-29-외부-빌드-오류-0경고는-바꾸지-않은-파일-셋뿐-unity-미확인-새-시험-없음"></a>

- **R-d1**(10-29 사용자 "A안으로 해주세요"): 새 `LifeCycles/RunStart/RunStartJob`·`IRunStartOutcome`. 받는 쪽 로비 `LobbyRunStartOutcome`(옛 `LobbyRunStartJob.cs`를 `git mv`)·Stage `StageRunStartOutcome`(`StageJob.cs` 안, 옛 `NewRunJob` 자리). `StageLifeCycle`은 `Register(Stage, Start)` 하나, 로스터가 비면 `StagePlan`이 새 Run Job을 앞에 붙임. `StageLifeCycle`의 원시 비교 셋을 이미 있는 질문으로. 바뀐 동작: Stage 첫 진입의 새 Run도 남은 전투가 있으면 멈추고 로스터 설치를 확인(로비와 같음). 검토 높음·중간 0. 사용자 Unity 확인(10-29).

<a id="672-r-d2-실행-기록-10-29-외부-빌드-오류-0경고는-바꾸지-않은-파일-셋뿐-unity-미확인-새-시험-없음"></a>

- **R-d2**(10-29 사용자 "확인함(R-d1 Unity 문제 없음), R-d2 진행"):
  - 새 파일 7(`LifeCycles/Stage/`): `StageProgressFacts`·`IStageProgress`·`StageProgress`·`StageEntryAttempts`·`StageTraitGrowthProgress`·`Plans/StageStep`·`Jobs/StageCheckJob`.
  - 지움: Flow 다섯(`StageFlow`·`StageNodeEntryFlow`·`ShopFlow`·`RecoveryFlow`·증강 `TraitGrowthFlow`), Plan 셋(`ShopPlan`·`RecoveryPlan`·증강 `TraitGrowthPlan`). 증강 둘은 커밋 전이라 `rm`(증강 세션 허락).
  - 연결 줄: `JobRegisterSystem`의 Shop·Recovery·TraitGrowth 줄 걷음, `JobRootType.Shop`(4)·`Recovery`(5)·`TraitGrowth`(7)·`JobType.NewRun`(2) 지움(번호는 주석), 서비스 키·그룹 속성 `StageProgress` 더함.
  - 계획과 다른 점: 전투 진입 요청은 Job이 아니라 `StagePlan.Next`가 판단 직후 부름(진입 서비스가 Runner가 빈 순간에만 받음). `StageProgressReader`는 따로 두지 않고 `StageProgress` 하나에.
  - 증강 조건 일곱과 계약 둘은 대조해 모두 지켰다.
  - 바뀐 동작: 맵·상점·회복·증강 처리 확인이 같은 Tick으로 당겨짐, 전투 진입 시도 닫기·진입 도메인 멈춤과 그 뒤 등록은 한 Tick 늦어짐.
  - 검토 높음·중간 0. 반영: 진입 시도가 막는 동안 증강 진행을 읽지 않음, 종료 뒤 진입 시도를 답하지 않음. 기록만 L1(§2)·L4~L7.
  - 공유: 증강 세션에 알림. 용근님 덱 편집 사실 줄(`38869ec1`)이 Flow에서 `StageProgress`로 옮겨진 것은 알리지 않음(10-29 사용자 "안 알려도 됨").
  - 사용자 Unity 확인(10-29, R-e 착수 때).
- 커밋: R-d1·R-d2 모두 `24c21b3a3`.

### 4.8 R-e (묶음 4) DB SO

<a id="68-phase-r-e--묶음-4-db-so-q-r10q-r10bq-r11-tp-r11tp-r13"></a>
<a id="681-r-e-실행-기록-10-29-외부-빌드-오류-0경고는-바꾸지-않은-파일-셋뿐-unity-미확인-새-시험-없음"></a>
<a id="680-착수-전-확인-10-29-코드-변경-없음"></a>

- 착수: 10-29 사용자 "확인함(R-d2 Unity 문제 없음), R-e 진행", "한 번에", DM2는 "같은 뜻만 합침". 착수 전 확인: 같은 뜻만 엄격한 쪽으로 합쳐도 걸리는 에셋이 없음을 먼저 확인했다.
- 한 것:
  - DM1: SO 여섯(회복 외형·용근님 소모 아이템 외형·아티팩트 외형·보드 설정·노드 외형·맵 표시 설정)을 읽기 전용 Data + 입력 SO로. 칸 이름이 같아 에셋 값 그대로. 처음 public 안쪽 칸으로 CS0649 경고 21개 → `[SerializeField] private` + `ToData()` 꼴로 없앰.
  - DM2: 새 `ViewPrefabRules`. 새로 엄격해진 것: 노드 아이콘·맵 프리팹에 실제 프리팹 에셋 확인, 로비 메시 정점 0 거절, 마커·로비 캐릭터에 "루트에 부모 없음". 걸리는 에셋 없음.
  - DM3(TP-R13): 시설 표지 둘을 `DataBaseSystem/Data/View/Table/LobbyVisual/Item/`로 `git mv`(GUID 그대로, `MovedFrom`), 방 시설 검사는 `LobbyVisualPrefabRules.ValidateRoom`이 DB 생성 때, `_structureValidated` 걷음.
  - DM4: `Event_Recovery.asset`의 선택지 둘을 등록된 표로. 게임 값은 그대로.
  - DM5: 회복 이름 둘을 TP-R11 임시 문구로.
- 계획과 다른 점: 시설 셋 검사는 `ViewPrefabRules`가 아니라 로비 규칙 `ValidateRoom`에(로비만의 요구). View 쪽 값 다시 검사(R-b 결정)는 그대로 두고 모델 프리팹 검사만 걷음.
- 묶음 끝 검토: 높음·중간 0, 낮음 7. 반영 L1(아티팩트 외형 프리팹도 `RequirePrefabAsset`)·L3·L4·L5·L7(로비 설계 §5.4·§5.5·D2). 기록만: L2 View 다시 검사 넷(→ R-f에서 걷음), L6 미등록 사본 회복 표. 참고: R-e 밖 View SO에도 `GetData()`가 같은 인스턴스를 돌려주는 것이 있다(`CategoryVisual`·`TutorialDefinition`·`StageViewProp`·`TraitVisual`·`InputSettings` 등, 일부는 다른 작업자 영역).
- 공유: 용근님 [소모 아이템 외형 P2](../handoffs/2026-10-29-consumable-item-visual-data-share-yonggeun-Hojin.md)(미전달).
- 확인: 외부 빌드 오류 0, 사용자 Unity 확인(10-29, R-f 착수 때). 커밋 `24c21b3a3`(SO 코드와 에셋 셋 함께).

### 4.9 R-f (묶음 8 작은 정리, 묶음 7 요청)

<a id="69-phase-r-f--묶음-8-작은-정리-묶음-7-요청"></a>
<a id="691-r-f-실행-기록-10-29-외부-빌드-오류-0경고는-바꾸지-않은-파일-셋뿐-unity-미확인-새-시험-없음"></a>

- 착수: 10-29 사용자 "확인함(R-e Unity 문제 없음), R-f 진행". 착수 때 결정(10-29 사용자): R-e 검토 L2의 View 다시 검사 넷을 걷음, J6·J7 아티팩트 죽은 경로와 `LobbyJob` 표본도 걷음(로비 설계 P7·Q-LJ4 바뀜), F6 다른 작업자 파일은 주석만 직접 고치고 키 걷기는 메모로, J8은 "부트 유지, 바로 예외".
- 한 것:
  - L2: `StageCurrentMarkerView`·`StageMapViewDisplay`·`StageMapBackgroundDisplay`·`ArtifactPopupView`의 다시 검사 걷음.
  - J6·J7: `ArtifactPlan.Register`·`ArtifactJob.cs`(민형님 파일)·`LobbyJob.cs`(민형님 파일) `git rm`, `JobRegisterSystem`의 아티팩트 Plan 줄 걷음, `LobbyPlan`의 `JobType.Start` 갈래 걷음. `JobRootType.Artifact`(2)·`JobType.ProcessCommand`(3)·`ProcessPreview`(4) 지움(번호는 주석).
  - F6 주석: `StageSelectionInputKey`·`LobbyInputKey`(민형님), `IRecoveryResultReadSink`·`IRecoveryStateReadSink`(천명님).
  - UL9: `StageLegendUI` 재정의 넷 걷음. `Clicker`(현강님)는 메모로.
  - J8: `ArtifactBoardPreparation.InitData`가 보드 설정이 없으면 바로 예외, Runner 직접 등록 이유를 주석에.
  - 묶음 7·8 문서는 §5(Core 요청 문서 6부, 민형님 씬 Factory 공유 R-f 절, 용근님 씬 노드 순서 제안, 현강님 Clicker 메모 덧붙임). 로비 설계 Q-LJ4·P7·§9 표 고침.
- 계획과 다른 점: `StageLegendUI`는 계획에 현강님 줄로 적었지만 호진 파일(`32654528`)이라 직접 고침. F6 주석은 사용자에게 물어 직접 고침. J7로 쓰이지 않게 된 `ProcessCommand`·`ProcessPreview`(용근님 줄 `d1dcb92f`)도 걷음(메모에는 적지 않음, R-d2 결정과 같게).
- 묶음 끝 검토: 높음 0·중간 1·낮음 6. M1 민형님 메모에 `ArtifactJob`·`ArtifactPlan.Register` 본문 추가, 옛 [10-23 로비 Job·Plan 공유](../handoffs/2026-10-23-lobby-job-plan-framework-share-minhyeong-Hojin.md)에 "바뀜" 표시. L1~L5 반영(L2: 민형님 `framework-architecture.md` 클래스 그림에 지운 Job이 남은 것을 메모에, L3: 로비 설계 `LobbyRunStartJob` 이름 여섯 곳). L6은 §2.
- 확인: 외부 빌드 오류 0, Unity 미확인(§2). 커밋 `24c21b3a3`.

### 4.10 R-g 측정 (미착수)

<a id="610-phase-r-g--측정"></a>

- 블라인드 A/B 한 번: A = `bb29f806` + 4번 커밋 판(`354bd8d7d`), B = 끝난 판. 기준 다섯(설계 §1.1)과 같은 방식·감점. 예상(설계 §12.4)보다 0.2~0.5 낮을 수 있다.

## 5 공유 메모

<a id="4-공유-메모"></a>
<a id="611-공유-메모-모두-미전달-phase마다-갱신"></a>

모두 미전달이다. 전달은 사용자가 한다.

| 메모 | 받는 사람 | 내용 | 비고 |
|---|---|---|---|
| [노드 설정 정리](../handoffs/2026-10-26-stage-node-settings-share-yonggeun-Hojin.md) | 용근 | N-1에서 지운 색·마스크 칸과 검사, R-b(2번) 덱 편집 줄 | |
| [진입 요청 질문](../handoffs/2026-10-27-combat-entry-request-question-share-yonggeun-Hojin.md) | 용근 | `IsPrepared` 추가 | 메모 상태 줄 "커밋 전"은 낡음(`45b20161`) |
| [팝업 Layer 변경](../handoffs/2026-10-27-stage-map-popup-layer-share-yonggeun-Hojin.md) | 용근 | 덱 편집 컨트롤러 한 줄·Layer 이름, 묶음 R-a 절(덱 편집 수명 부품·열기 조건 불일치 Q-R3) | "커밋 전" 낡음(`b1dd4962`) |
| [참조 없는 파일 삭제](../handoffs/2026-10-27-unused-lobby-stage-files-removed-share-Hojin.md) | 용근, 예전 `Lobby.unity` 원격 브랜치 작업자 | 용근님 2줄, Missing Script 위험 | "커밋 전" 낡음(`b1dd4962`) |
| [IsStart 추가](../handoffs/2026-10-27-stage-progress-is-start-share-Hojin.md) | 민형·용근 | `StageProgressData.IsStart` | "커밋 전" 낡음(`42be34e6`) |
| [상점 UI 안내](../handoffs/2026-10-10-shop-ui-hyeongang-share-Hojin.md) 3부 | 현강 | 공통 기반·표시/수신자 계약·U4 판단 부탁 | S-c |
| [Core 요청 문서](../handoffs/2026-10-06-stageselect-core-requests-Hojin.md) 5부 | 천명 | 옮긴 `ShopUI` 줄 알림, 노드 종류 셋 유지, 묶음 R-a `itemVisuals` 세 줄 | S-c·R-a |
| [Core 요청 문서](../handoffs/2026-10-06-stageselect-core-requests-Hojin.md) 6부 | 천명(2번은 용근·민형) | F2 seed, DM4 DB 생성 때 참조 검사, F7 아티팩트 Buffer 읽기 둘, F6 회복 주석 알림, 강화 노드 맵에서 뺌 알림 | R-f |
| [로비 Clicker 걷음](../handoffs/2026-10-28-lobby-clicker-removed-share-hyeongang-Hojin.md) | 현강(Boot 줄은 전투 작업자에게도) | 글자를 `LobbyTexts`로, Clicker 줄 걷음, 남은 Clicker 정리 여부, 덧붙임 `Clicker` 재정의 넷(UL9) | 로비 안내 글자·R-f |
| [씬 Factory 공유](../handoffs/2026-10-28-stage-map-scene-factory-share-minhyeong-Hojin.md) | 민형 | 묶음 R-a·R-b Factory 줄, R-f 절(`LobbyJob`·`ArtifactJob` 삭제·아티팩트 Plan 줄·입력 키 주석), 제안 F6·F8·F10 | R-a·R-b·R-f |
| [소모 아이템 외형 P2](../handoffs/2026-10-29-consumable-item-visual-data-share-yonggeun-Hojin.md) | 용근 | `ConsumableItemVisualDataSO`를 Data + 입력 SO로(TP-R12) | R-e |
| [씬 노드 순서 제안](../handoffs/2026-10-29-scene-node-order-lobby-stage-proposal-yonggeun-Hojin.md) | 용근 | 로비·스테이지 노드 값을 `SceneNodeOrder` 표에(F3), 쓰지 않는 `DeckEditAvailability` 키(F6) | R-f |
