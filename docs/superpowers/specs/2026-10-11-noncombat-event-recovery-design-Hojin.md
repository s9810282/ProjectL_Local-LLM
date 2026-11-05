# 비전투 이벤트·회복 설계

- 작성자: 호진(이호진)
- 상태: 구현 완료(ER6, 2026-10-15, 사용자 커밋 `51f5a892`). 자동 시험·Core fixture·출력 검증까지 했고, 사람 직접 조작 확인은 미실행이다. 글로벌 HUD 전체 입력 차단(U0)은 후속이다.
- **2026-11-02 바뀜:** 아래 부품 표의 `RecoveryUI`·`RecoveryPopupViewScreen`·`RecoveryPopupText`는 지웠다. 회복 화면은 공통 `StageOfferUI`(`screenId` Recovery)·`StageOfferPopupViewScreen`·SO 글 표를 쓴다([공통 레이아웃 설계](2026-11-02-stage-offer-common-ui-design-Hojin.md)). 요청 수명·Plan·Core 경계는 그대로다.
- 범위: StageSelect의 Event 노드 → 회복 팝업 → 선택·나가기 → 맵 복귀의 View·LifeCycle 연결과, 후속 비전투 이벤트가 같은 표시 부품·팝업 수명을 쓰는 경계. 회복 계산·결제·노드 완료(Core, 천명님), 상점 화면 자체([상점 설계](2026-10-06-stage-shop-recovery-ui-design-Hojin.md)), 성장·교환·랜덤 이벤트 규칙은 다루지 않는다.
- 계획: [비전투 이벤트·회복 계획](../plans/2026-10-11-noncombat-event-recovery-plan-Hojin.md)
- Core 요청·인계: [회복 Core 요청·인계](../handoffs/2026-10-09-recovery-core-handoff-Hojin.md)(RC0~RC4 요구, V4 인계, Event 진입 요청, ER1 인계).
- 정리: 2026-10-27 `specs/2026-10-13-stage-event-recovery-entry-design.md`(Event → 회복 연결 설계)와 이 문서의 옛 1부(이벤트 UI 구조)·2부(Event 연결)를 한 틀로 합쳤다. 옛 판은 git `10914570`.

## 1 목적과 범위

회복 이벤트는 새 씬 없이 StageSelect 안의 팝업 UI로 처리한다. 후속 비전투 이벤트를 붙일 때 같은 화면 코드를 다시 만들거나 거대한 이벤트 클래스를 키우지 않도록, **표시 부품과 팝업 수명은 공유하고 입력 명령과 게임 규칙은 이벤트별로 나눈다.** UI로 처리한다는 것은 표현 방식이다. 회복 계산·골드 차감·선택 가능 판정·노드 완료는 Core 책임이다.

기획 근거는 통합 기획서(팀 Dropbox `Project L/기획 문서/통합 기획서.pptx`, 저장소 밖) 12번, 17~22번 슬라이드다.

| 기획 내용 | 구조에 반영한 것 |
|---|---|
| 비전투 종류는 메인 회복·캐릭터 획득·메인 성장 갈림길·캐릭터 교환·랜덤 이벤트 | 회복을 모든 이벤트의 데이터 형식·처리 규칙으로 쓰지 않는다 |
| 회복은 카드 선택지 3개, 고정값·비율 방식 | 기존 옵션 1·2·3을 연결한다(D0). 카드는 Core가 준 목록을 표시한다 |
| 선택·비선택 버튼 외 기능은 작동하지 않음 | 지금은 상점과 같은 입력 정책이다(ER5). 글로벌 HUD 전체 차단은 U0 후속 |
| 선택하지 않으면 스테이지 화면으로 복귀 | Complete 수락을 확인한 뒤 팝업을 닫고 맵을 갱신한다 |
| 텐트 노드 모델과 회복 카드 이미지 | 노드 외형과 팝업 에셋을 나눈다. 카드 UI 때문에 카메라를 만들지 않는다 |

[상점 설계](2026-10-06-stage-shop-recovery-ui-design-Hojin.md)의 S1·S2·S4·S7·S9·S10을 이어받는다. 회복 결과를 보인 뒤 **명시적 나가기로 완료**한다(자동 닫기 없음).

## 2 지금 구조

### 2.1 흐름

```text
Boot → StageSelect Picker / IInputSystemService.Submit → StageViewBuffer
  → StageNodeEntryFlow / StageNodeEntryJob → Core 노드 결과 + RecoveryEntryContext
  → StageMapEntryObserver / StageMapViewScope → RecoveryPopupController / RecoveryUI

RecoveryUI → RecoveryRequestLifetime(ER3) → RecoveryViewBuffer(V4)
  → RecoveryFlow / RecoveryPlan / RecoveryCommandJob → Core 처리 결과
  → RecoveryRequestLifetime: Number + 전체 Command 대조 / ack / 정확한 입력 제거
  → 현재 표시 문맥의 Controller → Complete 수락 때 닫기 / 맵 다시 그리기
```

1. 노드 선택은 기존 Stage 입력·Flow·Job·Core 경로를 쓴다. Core가 Event를 회복으로 판정하고 선택·Context·옵션과 번호 있는 맵 결과를 함께 게시한다(§6.1).
2. Observer가 같은 요청의 수락 결과를 소비한 뒤 `StageMapViewScope`가 현재 query로 회복 팝업을 복구한다(§9.1). 복구 때 `EnterRecovery`를 다시 부르지 않는다.
3. UI는 제출 전에 선택·나가기 버튼을 함께 잠근다. 제출은 runtime 요청 소유자가 받아 V4 입력 Buffer에 넣고 Number와 불변 Command를 보관한다.
4. Recovery Flow가 공용 Runner를 확인해 등록하고, Plan이 그 요청을 한 번 예약하고, Job이 Core 처리 서비스를 한 번 부른다.
5. Core가 계산·결제·적용과 결과 게시를 맡는다. 요청 소유자가 결과를 대조·ack하고 입력을 지운다. Controller는 자기 표시 문맥과 맞는 결과만 화면에 반영한다.
6. 회복 적용(Select)과 노드 완료(Complete)는 별도 명령이다. Complete 수락 뒤에만 팝업을 닫고 입력 토큰을 돌려주고 맵을 갱신한다. 미선택 Complete도 같은 경로다.

### 2.2 구성과 책임

| 구성 | 맡는 일 | 하지 않는 일 |
|---|---|---|
| `StageMapViewSystem` | `PostBindService`에서 회복 Factory를 한 번 만든다(Core `RecoveryState`·View `RecoveryStatus`·UI·View 외형) | 이벤트 규칙, 카드 내용 |
| `StageMapViewScope` | 씬 연결마다 Controller를 만들어 Start/Tick/Dispose. 상점과 같은 부모 `PopupInputSession`을 쓴다 | 요청 기록 소유, Core 직접 호출 |
| `StageMapEntryObserver` | 같은 Number/chapter/node의 Node 결과를 소비한 뒤 `RestoreNoncombat` 호출 | 팝업 종류 판단 |
| `RecoveryPopupViewFactory` | 고정 의존(Query·Status·UI·외형·오류 경계)과 연결별 화면·입력·호스트·맵 갱신 알림을 조합 | 씬·Controller 목록 보관 |
| `RecoveryPopupController` | 모델, 자기 표시 토큰·Context, 그 표시의 제출 번호·명령. 자기 `PopupViewScope` 소유 | Core 쓰기, UI 이력 직접 조작, ack |
| `RecoveryPopupViewScreen` | UI의 `IsOpen`·`OpenStateChanged`를 공용 화면 계약으로 감싼다 | 수명 판단 |
| `PopupViewScope`·`PopupInputSession` | 팝업 UI·입력 토큰 수명(호스트 차단 → Layer 확보 → UI 열기, 닫은 뒤 자기 토큰 반환) | 결과 소비, 거래 판단 |
| `RecoveryUI` | `UIBase` 표시, `UIInputBinding` 수신, 렌더한 ID·Context 복사, 제출 잠금 | ack, 거래·노드 완료 판단 |
| `RecoveryPopupModelBuilder`·`RecoveryPopupText` | Core 상태와 View 외형(이름·아이콘·설명)을 모델·문구로 옮긴다 | InGame DB 조회, 회복량 계산 |
| 공용 카드 `StageOfferCardView` | 아이콘·문구·버튼·완료 표시와 slot | 게임 ID·규칙(§4) |
| `RecoveryViewBuffer`(V4) | 입력 한 건, 요청 번호 발급, ack 번호 보존(§5) | Core 결과·처리 번호 |
| `RecoveryRequestLifetime`(ER3) | 씬 밖에서도 요청 Number·Command·예약·실행·소비 수명(§8) | 화면 보관, HP·골드·영수증 복제 |
| `RecoveryFlow`·`RecoveryPlan`·`RecoveryCommandJob` | 정확한 소유 요청의 등록·예약·실행과 종료 통지 | UI가 준 비용·회복량 적용, 결과 쓰기 |
| Core 회복·명령 처리(천명님) | 입력 재조회, 검증, 규칙 적용, 상태·결과 게시 | UI·Canvas·카메라 참조 |

화면 선택용 범용 Router·Controller 기반 클래스, `EventManager`, 여러 단계 `BaseEvent` 상속은 두지 않는다. 상점과 회복 Controller는 Scope가 명시적으로 조합한다. 둘이 동시에 활성이라고 주장하면 구성·상태 불변식 오류로 공용 오류 경계에 보고한다.

### 2.3 사실의 원본

| 사실 | 원본 |
|---|---|
| HP·골드·클리어·영수증·Context·선택 가능·예상 회복량 | Core / Buffer(`RecoveryState`) |
| 비용·회복 규칙 | InGame 회복 옵션 테이블 → Core |
| 입력 요청 번호·ack 번호 | `RecoveryViewBuffer` |
| 처리 번호·처리 결과 | Core 결과 Buffer(`RecoveryResultReadSink`) |
| 아직 소비하지 않은 요청 | `RecoveryRequestLifetime` 한 곳 |
| 카드 이름·그림·설명 | View DB `RecoveryOptionVisual` |
| 화면의 잠금·표시값 | UI의 표시 사본(게임 사실 아님) |

### 2.4 파일 위치

| 별칭 | 경로 |
|---|---|
| UI | `Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/UISystem/Screens/Stage` |
| MAP | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/StageMap` |
| VIEW | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem` |
| LIFE | `Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Stage` |
| BUF | `Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems` |

| 파일 | 내용 |
|---|---|
| `BUF/RecoveryView/RecoveryViewCommand.cs`, `RecoveryViewBuffer.cs` | V4 입력 |
| `VIEW/Recovery/RecoveryRequestLifetime.cs`, `IRecoveryRequestSubmitter.cs`, `IRecoveryRequestExecution.cs`, `IRecoveryRequestStatus.cs` | ER3 요청 수명 |
| `LIFE/RecoveryFlow.cs`, `LIFE/Plans/RecoveryPlan.cs`, `LIFE/Jobs/RecoveryCommandJob.cs` | ER3 실행 경로 |
| `MAP/Recovery/RecoveryPopupController.cs`, `RecoveryPopupViewFactory.cs`, `RecoveryPopupViewScreen.cs`, `RecoveryPopupModelBuilder.cs`, `RecoveryPopupText.cs` | ER4 팝업 |
| `UI/RecoveryUI.cs`, `IRecoveryPopupDisplay.cs`, `RecoveryPopupModel.cs`, `RecoveryOfferCardEntry.cs` | ER4 화면 |
| `Assets/ProjectL/Prefabs/UI/Recovery/RecoveryUI.prefab` | 운영 화면(ER5). `RecoveryPanel.prefab`은 V3 표시 원본 |
| `Assets/ProjectL/Script/Systems/DataBaseSystem/Data/View/Table/Recovery/Item/RecoveryOptionVisualDataSO.cs` | V3 외형 칸 `icon`·`description` |

### 2.5 2026-10-16 뒤 다른 작업이 바꾼 것

아래 변경은 각 작업 문서가 원본이다. 회복 동작 계약은 바뀌지 않았다.

- 팝업 화면 참조: 옛 `StageMapSceneRoot.recoveryUI` → 지금 `StageScreenRootUI.recoveryUI`(`RecoveryScreen`). StageSelect UI가 공통 Canvas 하나 아래로 모였고 `RecoveryUI.prefab`에는 자기 Canvas가 없다(2026-10-26, [스테이지 맵 설계](2026-10-10-stage-map-design-Hojin.md)).
- 입력 Layer 이름: 옛 `ShopPopupInputLayers.Popup`(이름 `"ShopPopup"`) → 지금 `StageSelectionInputLayers.Popup`(이름 `"StageMapPopup"`, 상점·회복·덱 편집 공통). 2026-10-27 작은 정리 C-2([검사 후속 정리 설계](2026-10-26-lobby-stage-review-cleanup-design-Hojin.md)).
- 카드 프리팹: 회복 카드는 상점·회복·강화 공통 설명 카드 `StageEventOfferDescriptionCard.prefab`을 쓴다([상점 계획](../plans/2026-10-09-stage-shop-plan-Hojin.md), [강화 설계](2026-10-19-enhancement-view-integration-design-Hojin.md) Q-E1). 현강님 `315cb578`(10-27)이 회복·상점 카드 배치를 맞췄다(직렬화 칸 이름 그대로).

## 3 결정 기록

| ID | 결정 | 날짜·근거 |
|---|---|---|
| — | 공통 표시 부품·팝업 수명 + 이벤트별 처리기. 이벤트마다 복제(입력 잠금·취소 수정 반복)와 범용 이벤트 엔진(없는 규칙 추정·만능 분기)은 채택 안 함 | 2026-10-11, Framework 개발 지침 §26~28 |
| — | Core 계약 우선 경로. R-A(`RecoveryTemporaryGuard`로 골드·최대 체력을 View가 검사하는 임시안)는 구현하지 않음 | 2026-10-11, R-A의 최대 체력 계산 복제는 SSOT 예외라서 |
| — | 회복 결과 표시 후 명시적 나가기로 완료. 성공 문구 3초 숨김은 넣지 않음(필요하면 상점의 `IDelayService`·`DelayDataHandler` 패턴) | 2026-10-11, 상점 설계 유지 |
| D0 | 운영 선택지 1·2·3(저작 순서 고정). 비용·회복 규칙은 등록 테이블 값. 그림 null·설명 빈값 + 모델 fallback, 임시 Event 건물·물음표·현재 위치 캡슐 유지. Position·Weight 없음 | 선택지 3개 2026-10-11 사용자("기획대로"), 임시 표시 범위 2026-10-15 사용자 |
| A1 | `AcknowledgeResult(number)`는 `0 <= number <= lastIssued`이고 기존 ack 이상. 0·같은 값은 no-op, 음수·역행·상한 초과는 상태 변경 전 거절. Buffer는 결과 Reader를 받지 않는다 | 2026-10-12 View 결정(§5) |
| — | V4 입력은 최대 한 건·정확한 번호 조회/제거. 바뀐 것: 옛 상점식 다중 큐 제안 → 한 건(2026-10-12, RC0 인수) | 2026-10-12 |
| A2 | runtime 요청 소유자는 씬·팝업 밖의 `RecoveryRequestLifetime` 하나. 팝업 Scope·`StageFlow.Reset`에 유일한 요청 기록을 두지 않음 | V5~V6 전에 확정하기로 한 것(10-12), ER3에서 확정(2026-10-15) |
| — | 진입 전 Stage origin은 기존 번호·취소 계약으로 보장. 전역 RunId·origin 토큰은 추가하지 않음 | 2026-10-13 제안, ER1(10-14) 구현, Core §17 인수 |
| — | Event 클릭은 기존 번호 입력·Job·Core endpoint를 지난다. View 클릭에서 `EnterRecovery` 직접 호출, Event를 Shop으로 바꿔 상점 팝업 재사용은 제외 | 2026-10-13 |
| — | 완료된 현재 회복은 query가 성공해도 다시 열지 않는다(`!IsCompleted` 조건) | 2026-10-13 독립 리뷰 보완 |
| — | 회복 입력은 상점과 같은 팝업 Layer(맵 선택·드래그 차단, CommonMap 설정 허용). 바뀐 것: 옛 엄격 모달(`Begin(null)` = ClearLayer로 게임 액션 전부 끔, 글로벌 HUD 전체 차단) → 상점 정책(2026-10-15 사용자 "회복도 상점과 같은 입력 정책으로 먼저 연결") | 2026-10-15 |
| U0 | 글로벌 HUD 전체의 입력·Navigation·초점 차단은 후속으로 분리. UI 담당 공개 계약을 받은 뒤 한다 | 2026-10-15 사용자("글로벌 UI 전체 차단은 후속으로 분리") |
| — | `UIId.Recovery = 19`. 바뀐 것: ER4 당시 16 → 병합 뒤 19(Confirm 16·KeySetting 17·SoundSetting 18과 겹치지 않게) | 2026-10-15 |
| — | 회복 배경은 프리팹 Image에서 직접 정한다. View DB 배경 설정은 두지 않는다 | 2026-10-15 사용자, [배경 안내](../viewDocs/recovery-popup-background-guide-Hojin.md) |
| — | 새 씬·실제 Camera·Core 계산·이벤트 종류 enum·무작위 옵션·3초 자동 닫기를 만들지 않는다 | 2026-10-13 |

## 4 공용 카드와 도메인 ID (V1)

- 공용 카드 `StageOfferCardView`에는 표시값과 `UIInputBinding` slot만 둔다. 유지 계약: `Render(StageOfferCardContent)`, `Bind(IUIInputReceiver, int slot)`, `SetCanSubmit(bool)`.
- `StageOfferCardContent(Sprite icon, string title, string primaryText, string secondaryText, string buttonText, bool isButtonEnabled, bool isCompleted)`에는 도메인 ID가 없다.
- 상점은 `ShopOfferCardEntry(ShopOfferId, Content)`·`ShopPopupModel.Cards : IReadOnlyList<ShopOfferCardEntry>`로, 회복은 `RecoveryOfferCardEntry`(`RecoveryOptionId` + Content)로 slot과 ID를 잇는다.
- UI는 Render 때 ID를 자기 목록에 복사한다. 재사용 모델을 보관하지 않으므로, 모델을 다시 채운 뒤 이미 그린 카드를 눌러도 표시 당시 ID로 제출된다.
- 회복 ID를 `ShopOfferId`로 바꾸거나 이름·문구로 이벤트 종류를 판정하지 않는다. 다른 입력 구조가 필요한 이벤트는 별도 `UIBase` 화면으로 만든다. 카드 형태를 모든 이벤트에 강제하지 않는다.

## 5 회복 입력 Buffer (V4)

네임스페이스 `ProjectL.Systems.RuntimeSystem.BufferSystems.RecoveryView`. `RecoveryViewCommand`는 공개 readonly struct(생성자 internal), Buffer는 internal sealed다.

```text
RecoveryViewCommand.Number : int
RecoveryViewCommand.Data : RecoveryCommandData
IRecoveryViewBufferReadSink.AcknowledgedResultNumber : int
bool IRecoveryViewBufferReadSink.TryGetCommand(int expectedNumber, out RecoveryViewCommand command)
int  IRecoveryViewBufferWriteSink.Submit(RecoveryCommandData command)
bool IRecoveryViewBufferWriteSink.RemoveCommand(int number)
void IRecoveryViewBufferWriteSink.AcknowledgeResult(int number)
void IRecoveryViewBufferWriteSink.Clear()
```

- 명령은 Core `RecoveryCommandData.Select(state.Context, optionId)`·`Complete(state.Context)`를 그대로 쓴다. Context(EntryId·ChapterKey·NodeId·StageKey)를 View가 따로 저장하거나 새 식별자를 발급하지 않는다. 비용·회복량·선택 가능 상태는 입력에 넣지 않는다.
- 입력은 최대 한 건이다. 입력이 남아 있으면 새 Submit은 거절하고 번호를 발급하지 않는다. 번호는 runtime 인스턴스에서 단조 증가하며 취소로 생긴 공백을 허용한다.
- 정확한 번호 읽기는 소비하지 않고, 다른 번호를 대신 돌려주지 않는다. `RemoveCommand`는 그 번호만, `Clear`는 입력만 지운다.
- 발급 번호와 ack는 제거·Clear·노드 전환·새 Run에도 보존한다. `int.MaxValue` 뒤 제출은 상태 변경 전에 거절한다. Full runtime 교체만 새 번호 수명을 시작한다.
- ack는 View가 결과를 읽고 대조한 사실이다. Clear·Hide·입력 제거를 ack로 보지 않는다. 숫자 검사(Buffer, A1) / 실제 읽기·전체 대조(요청 소유자) / 게시 이력·덮어쓰기 차단(Core)을 따로 검증한다.
- 등록: `IBufferQuery`·Group의 `RecoveryViewReadSink`·`RecoveryViewWriteSink`, 키 `RecoveryViewBufferKey.Read/Write`. 기존 Buffer Factory가 한 인스턴스를 Build 전에 등록한다. 수명(InitData·Tick·씬)이 없어 `AddModule`·전역 System은 없다.

## 6 Core 계약

### 6.1 Event 노드 진입 (ER2, Core 천명님)

- 공개 `ProcessNextNodeCommand(int expectedRequestNumber, Func<int> nextSeed)`와 `StageEntryCommandResult`를 유지한다.
- 지원 Event를 회복 내부 준비·설치 경로로 분기한다. `ConfirmStageEntry`로 선택만 먼저 확정하지 않는다.
- 새 Context·옵션·현재 선택과 Node 결과(Number·ChapterKey·NodeId·Kind=Node)를 함께 설치한다. 취소·정상 거절·정의 실패에 부분 선택을 남기지 않는다.
- Event는 Seed 0이고 `nextSeed`를 부르지 않는다. 상점 seed 동작은 그대로다.
- 정의 조회 뒤 origin·번호를 다시 확인한다(§7). Factory가 Recovery 의존성을 명시적으로 주입한다.
- 운영 Event는 `Stage_Event` 1301 → `Event_Recovery` → 옵션 1·2·3이다. 문자열·키로 분기하지 않는다.

### 6.2 회복 명령 처리와 결과

- Job은 `ProcessNextRecoveryCommand(expectedNumber)`만 부른다. true는 거절을 포함한 **새 결과 게시**이고 거래 성공이 아니다. false여도 Job은 완료한다.
- false는 세 경우다: 입력 없음(실행 전 취소), 이미 처리됨, 입력이 남고 이전 결과가 미확인(backpressure). 마지막 경우 요청 소유자가 과거 결과를 소비한 뒤 같은 번호를 다시 예약한다.
- 정상 거절(골드 부족·효과 없음·이미 선택함 등)은 팝업을 유지하고 사유를 한국어 문구로 보인다. 공용 fault가 아니다.
- 상태 불일치(Run 종료·노드 변경·회복 상태 소실)는 거래 거절과 구분한다. View는 소유 요청을 정리하고 현재 상태로 복구한다.
- 결과 대조 필드는 Number·Kind·Context 네 필드(EntryId·ChapterKey·NodeId·StageKey)·OptionId 전체다.

### 6.3 Core에 요구한 보장

| 계약 | 보장 |
|---|---|
| 노드 진입 | Stage의 번호 있는 노드 명령으로 회복에 진입하고 상태·진입 결과를 게시 |
| 선택·완료 처리 | Core가 기대 번호의 입력을 직접 조회·검증. UI·Job이 결과를 대신 쓰지 않음 |
| 정상 거절 / 상태 불일치 | 사유를 구분해 게시 |
| 불변·재실행 | 실패 시 불변, 중복 결제 방지, 실행 전 취소와 이미 적용한 거래 구분 |
| 화면 상태 | `RecoveryStateSnapshot`·`RecoveryOptionState`(`CanSelect`·`UnavailableReason`·`ProjectedHealAmount`), Gold·Receipt·CanComplete |
| 결과 문맥 | 처리 번호·명령 종류·대상·선택. 새 Run·재진입에서 이전 결과를 거를 수 있음 |
| 향후 이벤트 | 종류·상태를 Core가 명시. View가 문자열·에셋명·ID 범위로 추측하지 않음 |

요구 이력·회신·인수 증거는 [회복 Core 요청·인계](../handoffs/2026-10-09-recovery-core-handoff-Hojin.md) 2부~5부가 원본이다. RC2~RC4 Core 설계·계획은 천명님 문서(git 기록, `2026-10-12-recovery-rc2-rc4-runtime-integration-*.md`)와 [RC2~RC4 Core 인계](../handoffs/2026-10-12-recovery-rc2-rc4-core-handoff.md)다.

## 7 진입 전 Stage origin (ER1)

`RecoveryEntryContext`는 진입 **뒤** 발급되므로 진입 전 Run origin이 될 수 없다. `StageFlow.Reset`은 자기 추적 초기화일 뿐 옛 Job 취소 증거가 아니다. 그래서 기존 번호·취소 계약을 완성한다.

- 실제 Run·챕터 교체 직전에 Stage 등록을 멈추고 옛 Stage 입력을 Clear한다. 발급·처리 번호는 초기화하지 않는다.
- 큐에 남은 옛 번호 Job은 정확한 번호 조회 실패로 거래 없이 정상 완료한다. Runner 큐를 View가 고치지 않는다.
- Core는 Number와 전체 불변 맵 명령·진입 전 사실을 보관하고, 정의 조회 뒤 같은 입력이 살아 있는지 다시 확인한다.
- 교체 호출자: Stage `NewRunJob`·`StageJob`, Lobby `LobbyRunStartJob`(구현함), 개발 `PrepareDevProgress`(새 runtime 최초 초기화로만 확인), Combat의 새 Run·다음 챕터 설치(용근님 영역, 정상 이탈·Detach 전제).
- 운영 호출자는 정상 이탈 전제를 유지한다. 실행 중 임의 Run 교체까지 보장하지 않는다(Core §17 정정).

## 8 runtime 요청 수명 (ER3, A2)

`RecoveryRequestLifetime`은 일반 C# runtime 모듈이다. `InGameViewSystemFactory`가 한 번 만들고 서비스 셋과 PostBind/Tick/Shutdown 모듈로 등록한다. 씬과 무관하게 Tick하므로 씬이 없어도 결과를 소비한다. `GameBootstrap`은 fault 뒤 Tick을 멈추므로 다음 프레임 소비에 기대지 않는다.

| 접근자 | 역할 |
|---|---|
| `ViewGroup.RecoveryRequests` | `IRecoveryRequestSubmitter.Submit(RecoveryCommandData) -> int`. 호출 중부터 잠그고 한 요청만 허용. UI·Controller용 |
| `ViewGroup.RecoveryExecution` | `CanReserveExecution`·`HasReservedExecution`, `TryReserveExecution`, `TryBeginExecution`, `NotifyExecutionFinished`, `CancelPending`. LifeCycle·Plan·Job용 |
| `ViewGroup.RecoveryStatus` | `HasPending`, `TryGetResult(number, fullCommand, out result)`. 소비된 Buffer 결과를 읽기만 함 |

- `RecoveryPlan.Register(service, type)`가 한 번만 예약하고, `RecoveryCommandJob(number, core, owner)`가 실행 시작과 finally 종료를 알린다. 대조·ack·입력 제거는 소유자만 한다.
- `JobRootType.Recovery = 5`(기존 0~4 유지). `StageFlow`는 모든 도메인의 대기 확인을 새 등록보다 먼저 하고, 맵 → 상점 → 회복 순으로 등록한다. inactive·loading·busy·fault에서는 새 예약을 만들지 않는다.
- 소유 기록 없는 결과(foreign)는 ack하지 않고 오류 경계에 보고한다. true인데 자기 게시 결과가 없으면 기록 누락 오류로 멈춘다.
- 일반 Hide·Detach·`Flow.Reset`은 예약 기록을 지우지 않는다. 정상 Stop·씬 이탈은 정확한 입력만 취소한다. 처리되지 않은 취소 번호에 ack를 만들지 않는다.
- `StopInput`은 새 제출·예약을 막고 이미 게시된 자기 결과를 소비한다. 실행 중이던 Job은 종료 통지에서 한 번 더 소비한다.
- `JobRunner.Clear/Shutdown`은 queued Job을 통지 없이 버린다. 그래서 full Shutdown에서만, 자기 queued 예약이 있고 실행 중이 아닐 때 같은 Runner의 공개 Clear를 먼저 마친 뒤 기록을 놓는다. 회복 예약이 없으면 다른 큐를 비우지 않는다. 실행 중이면 Clear하지 않고 finally까지 보존한다.
- 공용 fault 정리는 combat → map → lobby 뒤에 recovery를 둔다. 앞 정리가 실패해도 뒤를 시도한다. 예외가 겹치면 최초 오류를 먼저 고정하고 추가 오류는 원래 예외의 `Data`에 남긴다.
- 한계: Job 안에서 전체 `FrameworkRuntime.Shutdown`을 재진입시키는 기존 Runner 경계는 지원하지 않는다.

## 9 팝업 표시·복구·입력 (ER4·ER5)

### 9.1 복구 조건

- Scope가 `Current.Shop`과 `RecoveryState.TryGetCurrent`로 명시적으로 고른다.
- 회복은 현재 Selection과 Context의 chapter/node/stage가 같고 `!state.IsCompleted`일 때만 연다. `TryGetCurrent`는 완료된 회복에도 true라서 조회 성공만으로 열지 않는다.
- 아직 결과를 대조하지 않은 맵 입력이 있으면 열지 않는다.
- Shop·Recovery 동시 활성 주장, 선택된 Event의 회복 상태 누락은 오류로 보고한다. 가짜 팝업·재진입으로 복구하지 않는다.

### 9.2 Controller·UI 계약

- 표시 계약 `IRecoveryPopupDisplay`: `IsOpen`, `PendingNumber`, `PendingCommand`, `OpenStateChanged`, `SubmissionFailed`, `Render(RecoveryPopupModel)`, `ShowStatus(string)`, `SetCanSubmit(bool)`, `CompleteSubmission(int, bool)`, `ResetSubmissionState()`.
- `PendingNumber`·`PendingCommand`는 현재 표시가 제출한 불변 의도의 읽기 전용 조회다. Controller가 이것과 `RecoveryStatus.TryGetResult`, 현재 표시 토큰·Context를 함께 확인한다. `ResetSubmissionState`는 UI 표시 기록만 초기화하고 runtime 요청을 취소하지 않는다.
- Controller는 Core의 `CanSelect`·`UnavailableReason`·`ProjectedHealAmount`·Gold·Receipt·`CanComplete`를 모델로 옮긴다. 옵션 수를 3으로 고정하지 않는다.
- UI는 `UIBase`·`UIInputBinding`·`IUIInputReceiver`를 쓴다. `Button.onClick`·Inspector UnityEvent·입력용 delegate 구독은 없다. Submit 호출 전에 선택·나가기를 잠근다. Hide·Reset 중 재진입으로 바뀐 표시에는 옛 Submit 번호를 기록하지 않는다.
- 진행 전 변경: "제출 중" 사실을 컨트롤러 영수증 하나로 모으고 두 팝업 UI의 공통 기반을 두는 작업이 결정 완료·진행 전이다. 끝나면 위 표시 계약의 제출 수명 멤버가 바뀐다([검사 후속 정리 설계](2026-10-26-lobby-stage-review-cleanup-design-Hojin.md)).

### 9.3 화면 수명

| 상황 | 처리 |
|---|---|
| 열기 | `PopupViewScope`·`PopupInputSession`이 팝업 Layer를 소유. 맵 선택·진행 중 누름/드래그 취소·차단, CommonMap 설정 허용, 전면 배경 raycast 유지 |
| 제출 대기 | 선택·나가기 함께 잠금. 같은 프레임 클릭·Submit 중복 금지 |
| 정상 거절 | 팝업 유지, 사유 문구, 최신 상태로 입력 허용 다시 결정 |
| 상태 불일치 | 자기 표시만 닫고 노드를 완료하지 않음. 같은 맵 갱신에서 즉시 다시 열리지 않음 |
| Select 수락 | Core 적용 기록 표시, 명시적 나가기 유지 |
| Complete 수락 | `CloseUI` 반환 뒤 자기 토큰 반환, 맵·노드 표시 갱신. 현재 캡슐은 그 노드에 남고, 다음 노드로 떠난 뒤 완료 봉 표시 |
| 외부 Hide | 노드 완료·롤백이 아님. 알림 안에서는 표시만 정리, 입력 반환·맵 복귀는 다음 Tick. 남은 요청 결과는 소유자가 소비하되 화면을 다시 열지 않음. 알림 안에서 다시 열린 새 표시의 Panel·문구는 옛 정리가 끄지 않음 |
| 씬 이탈·Detach·fault·호스트 교체 | 새 입력 차단, 자기 Scope만 정리. 결과 소비는 요청 소유자에 남음. 정지한 StageSelection을 되살리지 않고, 교체된 옛 호스트는 새 UI·Layer를 바꾸지 않음 |

공용 수명 규칙(호스트 차단 → Layer 확보 → UI 열기, 외부 Hide 다음 Tick 복귀, 현재 호스트 검사)은 [View 구조 설계](2026-10-13-view-structure-design-Hojin.md)의 D5·D10·D18·D19를 따른다.

### 9.4 입력 정책

- 회복은 상점·덱 편집과 같은 `StageSelectionInputLayers.Popup`을 쓴다. 맵 선택 ActionMap을 끄고 CommonMap(설정 열기)만 켠다. 닫기 키는 없고 나가기 버튼만 있다.
- 카드·버튼은 기존 EventSystem 경로로 받는다. 회복 전용 Layer·범용 Router는 없다.
- 전면 배경의 raycast 차단은 뒤 글로벌 HUD 전체의 Submit·Navigation·초점 차단을 뜻하지 않는다(U0, §12.1).

### 9.5 카메라

- 기존 StageSelect 출력 카메라·Canvas·맵 구도를 유지한다. 새 Camera·`RecoveryCamera`·직접 Transform 이동은 없다.
- 맵 드래그 위치는 그대로 두고 입력만 취소·차단한다. 닫은 뒤 새 누름으로만 드래그를 다시 시작한다.
- 아티팩트 3D 보드 카메라(`LobbyArtifactCamera`)는 로비에 있고 회복과 무관하다.

### 9.6 화면 에셋

- 운영 `RecoveryUI.prefab`: 활성 루트 아래 초기 비활성 `Panel`, 그 안에 `PointerBlocker`(전체 화면 배경·클릭 차단)·`Content`·`Templates`. `CardsArea`는 비우고 `Templates`의 비활성 카드 인스턴스를 `cardPrefab`으로 쓴다. 공용 카드 원본은 고치지 않고 회복 인스턴스의 설명 높이·버튼 위치만 조정한다.
- 한국어 제목·HP·두 줄 불가 사유가 잘리지 않음을 실제 TMP 메시·3열 렌더로 확인했다(ER5).
- 배경 색·그림을 바꾸는 법은 [회복 팝업 배경 안내](../viewDocs/recovery-popup-background-guide-Hojin.md)다.

## 10 데이터 (D0)

| 항목 | 지금 값 |
|---|---|
| 운영 선택지 | 11-02 사용자 결정: 체력 회복 2개 + 최대 체력 1개. 스테이지 셋을 회복 후보로 등록한다: 1301 `Event_Recovery`(1·2·4), 1302 `Event_Recovery_B`(1·3·4), 1303 `Event_Recovery_C`(2·3·4). 모두 `InGameGroup/Recovery/Item`을 참조 |
| 조합이 정해지는 때 | 맵 생성 때 Core가 회복 노드마다 후보 중 하나를 균등하게 뽑는다(`ChapterGraphGenerator.AssignStageKeys`). 같은 노드는 조합이 그대로다. 새 Core 규칙 없음 |
| 규칙 | 1 = 2G / Flat 10, 2 = 5G / PercentOfMax 30, 3 = 10G / Full, 4 = 10G / 최대 체력 +10(현재 체력 그대로). 등록 SO와 Core가 원본 |
| 운영 초기 상태 | 체력이 가득 차면 회복 선택지는 NoEffect(선택 불가)이고 옵션 4만 살 수 있다 |
| 외형 | View DB `RecoveryOptionVisual` 1·2·3·4. 옵션 4는 11-02 추가(이름·설명은 임시, 아이콘 `UI_icon_HP`) |
| 노드 외형 | 임시 Event 건물·물음표·현재 위치 캡슐. 텐트 에셋 연결 없음 |

- Event SO는 참조 SO에서 OptionId만 뽑는다. Core `RecoveryDefinitionAdapter`가 등록 테이블을 조회한다.
- `Stage/Event/Recovery/Item` 아래 같은 ID의 중복 SO는 운영 원본이 아니다. 지우거나 고치지 않는다.
- View DB에는 외형만 둔다. 비용·회복량·선택 가능 상태를 복제하지 않는다.
- 11-02 결정은 [골드 시설 Core 설계](2026-11-01-run-gold-facilities-and-trait-ranks-core-design.md)의 "회복 스테이지에서 고정·전부 회복을 뺀다"를 되돌린다. 천명님께 알려야 한다(미전달). 이 설계를 따르는 `DataBaseAssetTests`의 회복 선택지 단언(2·4 두 개)은 이 변경으로 실패한다.
- 남은 표시 수정: `RecoveryPopupText`가 옵션 4에 "예상 회복량 : 0"과 "체력 +0" 영수증을 낸다.

## 11 새 이벤트를 붙일 때

| 추가할 것 | 필요한 변경 |
|---|---|
| 기존 회복 규칙 안의 새 선택지 | InGame 회복 옵션·이벤트 선택지 목록, 같은 ID의 View 외형 |
| 새 회복 그림·문구 | View DB와 화면 에셋. 비용·회복 규칙은 그대로 |
| 메인 성장 갈림길 | 성장 전용 Core 계약·상태·입력 처리와 Controller·UI. 같은 카드 형식이면 표시 부품 재사용 |
| 캐릭터 교환 | 교환 대상 선택·검증·결과 Core 계약과 화면. 회복 카드 형식으로 강제하지 않음 |
| 랜덤 이벤트 | Core가 종류·후보·난수·확정 결과 소유. UI가 다시 추첨하지 않음 |
| 3D 연출 이벤트 | 그 연출과 카메라 수명 추가. 규칙 처리와 표시 완료는 분리 |

- "새 규칙까지 데이터만 추가하면 끝"은 범위가 아니다. 기존 구매·회복 코드를 고치지 않고 새 처리기를 붙일 수 있다는 뜻이다. 공용 enum·Plan 등록 같은 조립 변경은 담당자 공유 대상이다.
- 회복만 연결할 때 아직 규칙이 없는 이벤트의 명령·결과를 한 DTO에 합치지 않는다.
- 3D 카메라가 실제로 필요해지면: 가상 카메라 이름 유일·`CameraGroup.BindScene` 수명, 제어 기간에 Catalog 핸들을 한 번 얻고 `Dispose`(위치·Priority·렌즈 원복은 따로 소유), 줌·쉐이크는 `CameraEffectSystem.Dispatch`(호출·효과 만료를 UI 완료로 보지 않음), `StageMapCameraDrag`와 제어권·기준값 복구를 먼저 설계한다.

## 12 보류·결정 대기

### 12.1 U0 글로벌 HUD 전체 입력 차단 (후속)

UI 담당(현강님)이 공개 입력 차단 lease·Navigation/초점 제한·정확한 복원 계약을 주면 별도 설계·Task로 연결한다.

- 지킬 것: HUD 표시·고정 카드·열린 패널을 유지한 채 입력만 막는다. 원래 disabled·interactable·초점·Navigation과 다른 후속 모달 소유자를 보존한다. fault로 원래 꺼진 버튼을 다시 켜지 않는다.
- 쓰지 않을 것: `DeckButtonUI.Hide`(고정 카드·선택까지 지움), `HideAllUI` 뒤 추측 복원, private 상태·UISystem 이력 수정, `ArtifactPopupSession`의 `SetCanOpen` 덮어쓰기, 없는 `IUISystemService.IsOpen` 가정.
- 2026-10-15 기준 공개 경계: `IUISystemService`는 Open/Close/HideAll 계열뿐이다. `OptionUI`의 OverrideLayer·초점 복원은 자기 수명용이다. `UIInputBinding`은 게임 ClearLayer와 무관하게 EventSystem Submit을 전달한다. `InputLayer`는 ActionMap이 하나 이상 있어야 해서 빈 Layer를 만들 수 없다.
- 막아야 할 유지 HUD(Boot `GlobalUICanvas.prefab`, DontDestroyOnLoad): Deck(UIId 9, 목록 Button Navigation Automatic), 고정 UnitCard(11), Option(1)과 자식 KeySetting(17)·Sound(18)·Confirm(16), Portrait `MainCharactorUI`(8, 입력 없음).
- 주의: StageSelect EventSystem은 FirstSelected 없이 Navigation이 켜져 있고 별도 `DefaultInputActions`를 쓴다. `GlobalUICanvas` 정렬 순서가 999라 회복 배경만으로는 HUD 위 포인터 차단을 보장하지 않는다(당시 상점·회복 화면은 110, 지금은 StageSelect 공통 Canvas 아래, 확인 필요).

### 12.2 그 밖

| 항목 | 상태 |
|---|---|
| 정식 카드 그림·설명, 회복 텐트 노드 외형 | D0 후속. 임의로 만들지 않음 |
| Position·Weight(무작위 출현) | Core 계약에 없음. 기획·Core 확인 필요 |
| 상점·회복 "제출 중" 하나·공통 UI 기반 | 결정 완료·진행 전([검사 후속 정리 설계](2026-10-26-lobby-stage-review-cleanup-design-Hojin.md)) |
| 사람 직접 조작 확인 | 미실행. 목록은 [계획 §2](../plans/2026-10-11-noncombat-event-recovery-plan-Hojin.md#2-남은-일) |

## 13 다른 작업자 영향

| 누구 영역 | 파일 | 바꾼 것 | 공유 |
|---|---|---|---|
| 민형님(Framework·Factory·ServiceKeys) | `BUF/Core/BufferSystemGroup.cs`, `Factory/Runtime/Buffer/BufferSystemFactory.cs`, `ServiceKeys/DataBaseGroupServiceKey.cs` | V4 입력 키·서비스·조회 줄만 | [Core 요청·인계 4부](../handoffs/2026-10-09-recovery-core-handoff-Hojin.md#4부-v4-회복-입력-buffer--core-인계) |
| 민형님(Framework) | `JobRootType.cs`(`Recovery = 5`), `JobRegisterSystem.cs` | 회복 Plan 등록 줄(ER3) | 공유 메모에 적힌 곳을 찾지 못했다(확인 필요) |
| 민형님(View 조립) | `InGameViewSystemFactory`·`InGameViewSystemGroup`·`InGameViewFaultCleanup` | 요청 소유자 생성·역할 노출, 정리 순서 끝에 recovery(ER3) | [비전투 View 구조 민형님 공유](../handoffs/2026-10-15-noncombat-view-scope-framework-share-minhyeong-Hojin.md)(미전달) |
| 현강님·민형님 | `UIIDs.cs`(`Recovery = 19`), `StageMapViewSystem`의 회복 Factory | UI 번호, 팝업 조립(ER4) | [ER4 UI·Framework 공유](../handoffs/2026-10-15-recovery-er4-ui-framework-share-Hojin.md)(미전달) |
| 천명님(시험) | `ArtifactEditingServiceTests`의 `EditFaultQuery`, `ChapterResetSpy`의 `ResetSpy`, `CharacterRosterServiceTests` 수동 Group 조립 | V4 Read/Write 전달 프로퍼티·입력 등록만(기대값 그대로) | Core 요청·인계 4부 |
| 천명님(Core) | 없음 | View는 Core 파일을 고치지 않았다. RC1~RC4는 천명님이 구현 | Core 요청·인계 2부~6부 |
| 현강님(UI) | 공용 카드 책임 | V1로 카드에서 ID를 뺐다 | [상점 화면 작업 안내](../handoffs/2026-10-10-shop-ui-hyeongang-share-Hojin.md) |
| 용근님(전투) | 없음 | ER1에서 Combat 새 Run·다음 챕터 경로의 정상 이탈 전제를 확인 대상으로만 남김 | ER1 인계(Core 요청·인계 6부) |

## 14 규칙 점검

- SSOT: §2.3대로 한 사실은 한 곳에만 있다. Controller의 대기 표시가 요청 소유자의 pending을 중복 소유하지 않는다.
- SOLID: 입력·번호(Buffer), 요청 수명(소유자), 등록·실행(Flow·Plan·Job), 표시 문맥(Controller), 조립(Factory), 팝업 수명(Scope)을 나눴다. 읽기·쓰기 Sink와 Submit·Execution·Status 역할을 나눴다(ISP).
- FW: FW-001~003(인터페이스 등록·모듈·Build 1회), FW-004(내부 Store·UI 이력 우회 없음), FW-005~007(입력 설정·Submit·Layer 서비스), FW-008~009(UIBase·고유 UIId·UISystem 전환), FW-011·016(핸들·구독 반환, 기존 runtime 조립)을 지켰다. 새 Mesh·Effect·Pool·Queue는 없다.
- JMH 개발 지침 §23·25~28: Query와 runtime 사실 분리, 씬 검색은 연결 때 한 번, 실제로 상점·회복이 함께 쓰는 카드·팝업 수명만 공통화했다.
