# 전투 View 구조 개선 — 현강님 공유 (2026-10-22, 미전달)

> **2026-10-26 현재 5라운드 최종 인계는 §16을 따른다(미전달).** 4라운드 V4-n과 5라운드 a/b/c 각각 사용자 컴파일·기존 시험·Unity 정상 보고를 인수했다. 아래 10-22의 UI 변경 없음과 §11~15의 진행/미확인은 당시 이력이다. 현재 허락된 UI/시험 변경과 확인 한계는 §16 및 5라운드 계획 §10에 둔다.

작성: 용근 작업(Claude). 근거: [개선 설계 2판](../specs/2026-10-22-combat-view-structure-improvement-design.md), [Task 계획·실행 기록](../plans/2026-10-22-combat-view-structure-improvement-plan.md). 코드는 작업 트리에 있고 아직 커밋 전입니다(커밋은 사용자). Phase 1~6 모두 Unity 컴파일·EditMode 시험·재생 문제 없음(2026-10-22 사용자 확인).

**현강님 UI 코드에서 고칠 곳은 없습니다.** 바꾼 현강님 줄은 아래 §1·§2의 두 곳이고, §3·§4는 화면에서 달라 보일 수 있는 점입니다.

## 1. `ICombatViewService` 계약 (Phase 4, 설계 §7.1 D1)

선언 줄은 현강님 줄입니다(`9354c522`).

```diff
- public interface ICombatViewService : IServiceModule, ICombatProgressView, ICombatAbandonRequestService
+ public interface ICombatViewService : IServiceModule, ICombatAbandonRequestService
  {
+     /// 전투 진행 골격(전투 Plan·사실 읽기)이 보는 작은 계약입니다. 골격은 이것만 받습니다.
+     ICombatProgressView Progress { get; }
      ICombatDeckPlacementInput DeckPlacement { get; }
      …
```

- 진행 계약 멤버(완료 표시 상태·확인 등)는 이제 `Combat.Progress`로 읽습니다. 진행 계약의 `IsPlaybackBusy`도 지웠습니다(재생은 P < S일 때만이라 골격은 `P < S`로 연출 대기를 봄).
- UI 쪽 사용처(`DeckCardList`·`PackUI`)는 `DeckHover`·`DeckPlacement`·`ItemInput`만 써서 영향이 없습니다.
- 가짜 `ICombatViewService`를 만드는 시험은 `Progress`를 내야 합니다(우리·호진님 시험 넷은 맞춰 둠).

## 2. Bootstrap 씬 진입 노드 (Phase 5, 설계 §7.2 E4·E5)

`GameBootstrap.BindUILifeCycle`의 현강님 줄(`efb04eea`·`387d2665`·`015c66be`, Stage·Combat UI 다시 연결·덱 카드 준비·Combat/Lobby 화면 열기)과 호진님 줄(Lobby UI 다시 연결·카메라 수집·Stage 열기)을 **이름 있는 표 하나**로 모았습니다. 순서와 동작은 그대로입니다.

```csharp
private const int BindUIPriority = -200;            // LifeCycle 진입(0)·View 연결(100) 전
private const int BindCamerasPriority = -190;       // View가 카메라 핸들을 빌리기 전
private const int PrepareCombatDeckPriority = 90;   // 전투 View(100)의 첫 Render 전
private const int OpenSceneUIPriority = 200;        // 진입·연결이 끝난 뒤

var nodes = new (int Scene, int Priority, Action Run)[]
{
    (SceneIndices.Stage, BindUIPriority, bindUI.BindScene),
    (SceneIndices.Combat, BindUIPriority, bindUI.BindScene),
    (SceneIndices.Lobby, BindUIPriority, bindUI.BindScene),
    … 카메라 셋, 덱 준비, 화면 열기 셋
};
```

- `BindLoadedUI`(부를 때마다 `_ui`를 캐스트)는 지우고, 표를 만들 때 한 번 캐스트합니다. 캐스트가 실패하면 첫 씬 진입이 아니라 부트 때 멈춥니다(같은 F1, 더 이르게).
- 새 씬 노드를 더할 때는 이 표에 한 줄을 넣습니다. 전체 씬 Node 순서(LifeCycle 0·View ±100·튜토리얼 ±110·Bootstrap 노드)의 원본은 [씬 연결 안내 §1.1](../viewDocs/runtime-scene-binding-guide.md)입니다. **(2026-10-23 바뀜: 위 상수 넷은 `SceneNodeOrder`로 옮겨졌고 숫자 원본은 그 표, §7)**

## 3. 결과 화면 동안 보이는 차이 (Phase 1, 설계 §4.7, 사용자 결정 Q3 A)

이제 **모든 결과**에서 전투 정리(EndBattle)가 결과 화면보다 먼저입니다(전에는 보스 승리만). 화면 뒤 전투 장면은 그대로 남지만, Core의 `HasBattle`을 직접 보는 UI는 "전투 없음"으로 동작합니다.

| UI | 결과 화면 동안 |
|---|---|
| `NightCheckerUI.cs:57` | 현재 전투 정보가 null → 밤 진행 `0/0` |
| `MainCharactorUI` | 주인공 체력이 보유 캐릭터 값으로 |
| `BattleChangerUI.cs:52`·`PackUI.cs:90` | 이미 "제출 정지면 숨김"이라 차이 없음 |

- 지금까지 보스 승리에서는 이미 이렇게 보였습니다. 사용자가 이 차이를 받아들이기로 했습니다.
- 결과 화면(`CombatEndUI`)은 코드 변경이 없습니다. 이제 어떤 결과든 Core가 Run 결과를 확정한 뒤 열리고, 읽는 값(완료·보상 영수증·Run 요약·맵 좌표)은 정리 뒤에도 남습니다.
- **로비 출발 전까지:** 전투 쪽 새 Run을 걷어서, 로비에 있는 동안 Core는 끝난 Run 상태입니다. 보유 로스터를 읽는 `MainCharactorUI`·`UnitCardUI`가 끝난 Run의 로스터를 봅니다. 로비에 보이는 것은 주인공 초상뿐이라 그림은 같습니다. 새 Run은 로비 출발이 만듭니다.
- 결과 화면 동안 밤 진행·체력을 전투 마지막 값으로 보이고 싶으면 알려 주세요(어떤 사실을 읽을지 같이 정해야 함).

## 4. 씬 루트가 이탈 밖에서 꺼지면 F1 (Phase 6, 설계 §7.3 F1)

Combat이 활성인 동안 `CombatSceneRoot`가 꺼지면(오브젝트 비활성화·파괴) 이제 조용히 분리하지 않고 F1로 보고합니다. 같은 씬에 다시 붙지 않아 전투가 오류 없이 멈추던 것을 막으려는 것입니다.

- 정상 이탈(씬 Exit Node)과 앱·재생 종료는 그 전에 구독을 풀어 해당하지 않습니다.
- Combat 씬 UI를 작업하면서 재생 중에 `CombatSceneRoot`가 붙은 오브젝트를 끄면 오류로 기록됩니다.

## 5. 최종 승리 화면이 연결돼 있지 않음 (2026-10-23 발견 → 2026-10-24 해결, §8.1)

독립 재평가에서 찾은 결함입니다. **다른 작업자 작업과 겹칠 수 있어 사용자 결정으로 미뤘고, 코드·씬은 바꾸지 않았습니다.**

- **증상:** 최종 승리 때 `CombatEndUI.ending`이 비어 있어 `CombatEndUI.cs:96`에서 예외 → F1. 지금 맵은 챕터 하나라(`StageMap_Chapter1`에 다음 챕터 키 없음) 보스 승리가 곧 최종 승리입니다. `ValidateReferences`는 `ending`을 검사하지 않아 미리 잡히지 않습니다.
- **원인:** 엔딩 패널은 호진님 브랜치의 Combat 씬에 있었는데 병합 `9e0c0f2b`에서 빠졌습니다. 그 뒤 결과 화면을 새로 짜면서(`01f5c787`) `CombatEndUI`가 결과 캔버스 루트(`CombatEndCanvas`, 평소 꺼짐)에 직접 붙었습니다.
- **고칠 때 주의:** 최종 승리는 결과 화면을 열지 않고 엔딩(`UIId.EndingRunSummary`)만 엽니다. 엔딩은 부모가 켜져 있어야 열리므로(`EndingRunSummaryUI.Open`) `CombatEndCanvas` 아래에 두면 다시 막힙니다. 항상 켜진 다른 캔버스(예: 새 루트 `EndingCanvas`, Screen Space - Overlay, 정렬 7)에 두는 것을 권합니다.
- **같이 하면 좋은 것:** `CombatEndUI.ValidateReferences`에 `ending` 필수와 "종료 화면 밖" 검사를 넣으면 씬 연결 때 바로 잡힙니다. 이때 `CombatEndUITests` 준비 단계에 꺼 둔 엔딩 가짜가 필요합니다(지금은 `ending` 없이 만듦).

## 6. 조정자 C 단계 (2026-10-23, 미전달)

근거: [C 단계 설계](../specs/2026-10-23-combat-view-coordinator-c-stage-design.md) §7, [Task 계획·실행 기록](../plans/2026-10-23-combat-view-coordinator-c-stage-plan.md)(Phase C-a~C-d). 코드는 작업 트리에 있고 커밋 전입니다(커밋은 사용자). C-a~C-d 모두 Unity 확인 문제 없음(2026-10-23 사용자). 줄 번호는 HEAD `4c328d07` 기준입니다.

**현강님 UI 코드(GlobalSystem)에서 고칠 곳은 없습니다.** 화면에 보이는 차이도 없습니다. 아래는 전투 View 안에서 바꾼 현강님 줄과 알려 드릴 점입니다.

### 6.1 포기 표시 계약을 걷었습니다 (C-1)

- Option 이관(`9354c522`) 뒤 운영에서 닿지 않던 포기 버튼·확인창 길을 지웠습니다.
  - `ICombatAbandonDisplay`(+ `.meta`)
  - `CombatSceneDisplay.cs:291-300`의 `#region 포기 표시 계약 호환`(빈 메서드 넷, 현강님 줄 291·293-298)
  - 컨트롤러의 `Open`·`Confirm`·`Cancel`·`Sync`·`Close`·`Stop`·`IsConfirmOpen`
- 살아 있는 입구 OptionUI → `ICombatAbandonRequestService.TryRequestAbandon`(`ICombatViewService.cs:6-10`)은 그대로입니다.
- 포기 컨트롤러는 상태 없는 입구가 되어 `Input/ActionButtons/` → `Input/UiEntry/`로 옮겼습니다(`.meta` GUID 유지).
  - `CombatAbandonController.cs:152-179`의 현강님 줄(중괄호 빼고 13줄: 지운 `Confirm()` 몸통과 `TryRequestAbandon` 본문)을 고쳤습니다. `TryRequestAbandon`에서는 확인창 닫기·버튼 맞춤 호출(빈 구현만 불렀음)을 뺐습니다.
  - 요청 깃발은 새 진행 사실 클래스 `Input/CombatViewProgress`에 둡니다. 검사 순서(받을 수 있나 → 입력 표시 취소 → 요청)와 예외를 오류 경계로 보내는 것은 같습니다.
- `Display/Hud/CombatAbandonPanel.cs`는 씬·프리팹 참조가 0이지만 삭제 금지 규칙이라 그대로 둡니다.

### 6.2 바꾼 `CombatViewSystem.cs`의 현강님 줄

| HEAD 줄 | 무엇 | 바뀐 것 |
|---|---|---|
| 259 | 수신 창구의 모달 판정 | `_abandon.IsConfirmOpen \|\| 배치 확인창` → `IsModalOpen`(배치 확인창 하나) |
| 898 | 제출 정지 때 배치 확인창 닫기 | 진행 사실이 알리는 호스트 `OnSubmissionsSuspended`로 옮김(순서 그대로) |
| 929 | 결과 확인 뒤 배경음 재생 | 호스트 `ShowCompletion`의 확인 콜백으로 옮김. 확인이 받아들여졌을 때만 바꾸는 조건은 같음 |
| 1054-1055 | 손패·가방 수신 조건 | 포기 확인창 조건을 빼고 `IsModalOpen`·진행 사실의 제출 정지를 읽음 |
| 1151 | `RouteInput`의 유닛 정보 열기 조건 | `!IsPresenting(_state.HasBattle)` → `!IsPresenting`(골격과 같은 "확인 안 한 결과" 식, C-3) |
| 1169-1170 | 환경 호버 허용 식 | 사실 조건을 `CombatInputPolicy.CanShowEnvironmentHover`로. 수신 가능·포인터 대상 판정은 그 자리에 남음(C-8) |
| 1188-1189 | `BattleChanger`·`Pack` 맞춤 | 제출 정지 값을 지역 변수로, 모달은 `IsModalOpen`, 필드 이름 `_subscribedRoot` |
| 1196 | 배치 진입 가능(`CanEnterFromDeployment`) | 포기 확인창 조건만 뺌. 모달 판정은 넣지 않음(배치 확인창이 이 판단으로 자기를 열어 둘지 묻기 때문) |
| 595-596·674-675 | 해제·버튼 숨김의 씬 루트 | 필드 이름 `_sceneRoot` → `_subscribedRoot`(구독 수명이라는 뜻) |

`TryRequestAbandon` 위임(1105-1106)은 그대로입니다.

### 6.3 계약과 판정

- `ICombatViewService`에서 바깥 호출이 없던 `Attach`·`Detach`·`FocusLost`·`CaptureLost`·`RequestReturn`을 뺐습니다(천명님 줄, C-7). UI가 쓰는 `DeckHover`·`DeckPlacement`·`ItemInput`·`TryRequestAbandon`은 그대로입니다.
- `CombatInputPolicy.IsTerminal`은 이제 "완료 있음" 하나입니다(C-4). 근거: Core가 `Finished`와 완료를 한 거래로 게시합니다.
  - 그래서 `PackUI.cs:91`의 `!facts.HasCompletion && !CombatInputPolicy.IsTerminal(facts)`는 같은 조건 둘이 됐습니다. 동작은 같아 고칠 필요는 없습니다.
- 제출 정지 깃발은 이제 전투 Plan만 세웁니다. View는 승패가 기록된 결과를 처음 보면 입력 연결만 바로 놓습니다. 행동 버튼·전투 전환·가방이 숨는 때는 지금과 같습니다.

### 6.4 고친 현강님 시험

- `CombatViewSystemTests.Abandon_ExternalConfirmedRequest_UsesExistingStateAndRejectsDuplicates`의 한 줄(HEAD `:963`): `_rig.View.IsAbandonRequested` → `_rig.View.Progress.IsAbandonRequested`. 조정자가 진행 계약을 직접 구현하지 않게 됐기 때문입니다. 확인 내용은 그대로입니다.

## 7. 전투 골격 점검 (2026-10-23~24, 미전달)

근거: [골격 점검 설계](../specs/2026-10-23-combat-skeleton-check-design.md), [Task 계획·실행 기록](../plans/2026-10-23-combat-skeleton-check-plan.md). 코드는 작업 트리에 있고 커밋 전입니다(커밋은 사용자). Phase K-a~K-e 모두 Unity 컴파일·EditMode 시험 문제 없음(사용자 확인).

**현강님 코드와 시험에서 바뀐 줄은 없습니다.** 알려 드릴 것은 둘입니다.

- **§2의 Bootstrap 노드 상수가 표로 옮겨졌습니다.** 이름 있는 상수 넷(`BindUIPriority`·`BindCamerasPriority`·`PrepareCombatDeckPriority`·`OpenSceneUIPriority`)은 지웠습니다. 노드 표가 새 순서 표 `RuntimeSystem/Common/SceneNodeOrder.cs`의 `BindSceneUI`·`BindCameras`·`PrepareCombatDeck`·`OpenSceneUI`를 읽습니다.
  - 값과 표의 순서는 그대로입니다(−200·−190·90·200).
  - 덱 카드 준비는 이제 식 `CombatViewAttach - 10`으로 적혀 있습니다. 전투 View 연결 순서를 옮기면 덱 준비도 따라 옮겨집니다.
  - §2 끝의 "전체 씬 Node 순서의 원본은 씬 연결 안내 §1.1"은 이제 "숫자의 원본은 `SceneNodeOrder`(Bootstrap·Combat 씬 시스템), 안내 §1.1은 그것을 가리키는 전체 표"입니다.
- **결과 화면 `CombatEndUI.cs:150`의 포기 판정은 그대로 두었습니다.** 골격의 포기 판정 두 곳은 새 함수 `CombatPlan.IsAbandonment`(패배 ∧ 자발적 포기) 하나로 모았습니다. 결과 화면의 사유 판정(`Reason == VoluntaryAbandonment`)은 사본으로 남아 있습니다.
  - 고치면 결과 화면(UISystem)이 처음으로 골격 판단 클래스를 참조하게 됩니다.
  - 다음 View 단계에서 "결과 화면은 판정된 화면 값만 받기"로 다룰 예정이라 그때 같이 정리합니다. 표시 문구·동작은 그대로입니다.

## 8. 전투 View 단계 1라운드 (2026-10-24, 미전달)

근거: [View 단계 설계](../specs/2026-10-24-combat-view-stage-design.md), [1라운드 Task 계획·실행 기록](../plans/2026-10-24-combat-view-stage-round1-plan.md). 코드는 작업 트리에 있고 커밋 전입니다(커밋은 사용자). Phase V-a~V-d 모두 Unity 확인 문제 없음(사용자). 다만 V-d의 마지막 검사(엔딩 부모 캔버스 켜짐) 한 줄은 그 확인 뒤에 더해 컴파일 미확인입니다. 줄 번호는 기준 커밋 `8ed3f8cb` 기준입니다.

### 8.1 바꾼 현강님 줄

- **결과 화면 `CombatEndUI.cs`(V-d, §5 해결, 현강님 파일):**
  - `ValidateReferences` 맨 앞에 엔딩 검사 셋을 더했습니다(우리 줄).
    - `ending`이 비었는가
    - 결과 화면 아래에 있는가
    - 부모 캔버스가 꺼져 있는가(`EndingRunSummaryUI.Open`의 부모 계층 조건과 같음, 컴파일 미확인)
  - 어느 하나라도 걸리면 예외입니다. 이 검사는 Combat 진입 때 씬 루트 검사에서 돌므로, 엔딩이 다시 빠지면 보스를 이기기 전에 멈춥니다.
  - 기존 도우미 `Require`는 "결과 화면 안" 조건을 보므로 엔딩에는 쓰지 않았습니다.
  - `Present`의 `ending != null &&`(`:90`)와 엔딩 null 예외(`:96-97`)는 지웠습니다. 앞의 검사가 막습니다. `Dismiss`의 null 검사(`:183`)는 그대로입니다. 씬이 내려갈 때 정리에서 파괴된 객체를 거르는 자리입니다.
  - 이 셋(`:90·96-97·183`)은 호진님 줄입니다(`cb76bd2c`). 호진님 메모 §10.1에도 적었습니다.
- **표시 `CombatSceneDisplay.cs`(V-c):**
  - 생성자 선택 인자 `unitInfo`·`pack`·`completion`(`:117·119-120`, 현강님 줄)과 `unitHudLayer`(`:118`, 우리 줄)를 필수로 바꿨습니다. 이 표시를 만드는 곳은 Factory 하나이고, 늘 넘깁니다.
  - 대입 줄 `:128·133·134`(현강님 줄)는 null이면 `ArgumentNullException`을 내는 꼴로 바뀌었습니다.
  - 옛 HUD 확인 갈래와 그 주석(`:131-132`, `:279-280`)과 `HideCompletion`의 `_hud.HideCompletion()`(`:285`)을 지웠습니다. 완료는 결과 화면만 보여 줍니다. `HideCompletion` 메서드는 남아 있고, 다른 정리처럼 파괴된 객체 검사를 합니다.
  - 옛 손패 그리기(`:219-220`)를 지웠습니다. 손패 선택은 유지 덱 카드가 합니다.
  - `_unitInfo?.Render`(`:224`)와 `InspectUnit`의 `_unitInfo == null`(`:317`)은 필수가 되어 지웠습니다.
- **`CombatHud.cs:58`("확인" 문구):** HUD 완료 확인을 모두 걷으면서 함께 사라졌습니다. 씬의 그 버튼은 결과 화면의 확인 버튼(`CombatEndUI.confirmButton`)과 같은 객체라 그대로 남습니다.
- **조정자 `CombatViewSystem.cs:1151`(현강님 줄, `:1150`의 형식 검사는 우리 줄):** `_display is ICombatUnitInspection` 형식 검사로 꺼낸 창구 호출을 바꿨습니다. 대신 운영 연결이 표시와 함께 정보창 창구를 따로 넘기고(`AttachCreated`의 셋째 인자), 선택 누름이 `_inspection?.InspectUnit`을 부릅니다. 동작은 같습니다.
- **Factory `CombatSceneDisplayFactory.cs:79·165`:** `root.UnitInfo?.Configure` → `root.UnitInfo.Configure`. 표시 생성 인자에서 `hand`를 뺐습니다.
- **씬 루트 `CombatSceneRoot.cs`:** `pack`과 `completion` 검사 사이에 `unitInfo` 필수 검사를 더했습니다(씬에는 이미 연결됨).
- **유닛 표시 `FieldUnitView.cs:267`(`SyncHudActive();`, V-a):** 정리 목록을 하나로 묶으며 새 `OnDisable`의 첫 줄로 그대로 옮겼습니다. 그 뒤의 클립 멈춤과 예상 체력 지우기의 순서가 서로 바뀌었는데, 대상이 달라 결과는 같습니다.
- 그대로인 줄:
  - 호스트의 `_deploymentConfirmation?.Close()`는 그대로입니다(호스트 이름만 `OnInputClosed`로 바뀜).
  - `PlacementDragInteraction.cs:107-108`(덱 선택 대상 판정)도 그대로입니다.

### 8.2 고친 현강님 시험

- `CombatEndUITests` SetUp·TearDown(허락): `_root` 밖에 꺼 둔 엔딩 가짜를 만들어 `ending`에 넣고 함께 지웁니다. 단언은 그대로입니다.
- `CombatInteractionTests`의 `PickAndCancel_ArePlayedOnce_…`(`:202`)·`PlacementCompletionAndPresentationCleanup_…`(`:214`)(허락): 손패 누름을 덱 카드 선택(`SelectHandSub`)으로 바꿨습니다. 단언은 그대로입니다.
- `CombatDisplayReuseTests`의 `HudCompletion_DefeatByAbandonment_…`·`HudCompletion_Victory_…`는 지웠습니다(사용자 사후 확정).
  - 본문은 우리 줄이지만, `[TestCase(…, "확인")]` 네 줄(`:722-723·739-740`)이 현강님 줄이었습니다.
  - 걷은 HUD 완료 경로를 보던 시험이라 남길 수 없었습니다. 먼저 여쭙지 못해 죄송합니다.

### 8.3 화면·동작 차이

- **포기를 확인하는 순간 새 입력이 닫힙니다.** 행동 버튼·BattleChanger·가방이 그 자리에서 숨습니다. 전에는 다음 Tick에 골격의 제출 정지가 돌 때였습니다.
- **배치 확인창이 열린 동안 가방(`PackUI`)도 숨습니다.** BattleChanger와 같은 판정입니다.
  - 이때 `PackUI.Hide`가 펼침을 접고 설명을 숨기므로, "아니오" 뒤 가방은 접힌 채 다시 보입니다.
  - 펼침을 유지해야 하면 알려 주세요.
- `PackUI.Refresh`·`BattleChangerUI.Refresh`의 둘째 인자(`suspended`)로, 이제 "입력 막힘"(제출 정지·포기 요청·확인창 중 하나)을 넘깁니다. 두 UI의 서명과 코드는 바꾸지 않았습니다.
- **최종 승리(지금은 보스 승리)에서 F1 대신 엔딩 요약이 열립니다.** 사용자가 Combat 씬에 새 루트 `BossEndingCanvas`(Overlay, 정렬 7, 켜짐)를 만들고 그 아래에 `EndingPanel`을 두어 연결했습니다.

### 8.4 알려 드리는 것(고치지 않음)

- `GlobalSystem/UISystem/Screens/Stage/DeckButtonUI.md:29`의 "CombatSceneDisplay가 빈 목록·숨김을 전달"은 이제 사실이 아닙니다(옛 손패 그리기를 걷음).
- `BossEndingCanvas`의 크기 맞춤은 Constant Pixel Size(배율 1)입니다. 결과 캔버스와 같은 설정이고, 이 모드에서는 기준 해상도 값(800×600)은 쓰이지 않습니다. 기본 캔버스는 Scale With Screen Size라서, 해상도가 바뀌면 엔딩·결과 화면만 상대 크기가 달라 보일 수 있습니다. 한 번 봐 주세요.

## 9. 전투 View 단계 2라운드 (2026-10-25, 미전달)

근거: [2라운드 설계](../specs/2026-10-24-combat-view-stage-round2-design.md), [2라운드 Task 계획·실행 기록](../plans/2026-10-24-combat-view-stage-round2-plan.md). 코드는 작업 트리에 있고 커밋 전입니다(커밋은 사용자). Phase V2-a~V2-g 모두 Unity 확인 문제 없음(사용자). 줄 번호는 기준 커밋 `23436b00` 기준입니다.

이번 라운드는 조정자(`CombatViewSystem`)를 나누고, 입력 기능에 공통 수명 계약을 둔 작업입니다. 현강님 줄은 대부분 같은 문장을 새 클래스로 옮긴 것이고, 운영 동작은 바뀌지 않았습니다.

### 9.1 조정자에서 옮긴 현강님 줄 (V2-e·V2-f)

새 클래스 셋으로 나눴습니다.
- 연결 입력 묶음 `Input/CombatSceneInput.cs`: 대기 관문·입력 기능 묶음·Router·덱 배치 입구·아이템 입구·환경 호버·거절 안내, 일곱을 갖습니다.
- 입력 표면 `Input/ActionButtons/CombatInputSurfaces.cs`: BattleChanger·가방·행동 버튼의 보임과 막힘을 맞춥니다.
- 표시 값 읽기 `Display/CombatDisplayValueReader.cs`: HUD 값·스테이지 키·가방 재고를 읽습니다.

| 옛 자리(`CombatViewSystem.cs`) | 지금 자리 |
|---|---|
| 필드 `:121`(`_deckPlacement`)·`:124`(`DeckPlacement`)·`:131`(`_environmentHover`)·`:134`(`_actionNotices`) | `CombatSceneInput` 안. 서비스 멤버 `DeckPlacement`는 서명 그대로 묶음의 값을 돌려줍니다 |
| `:526-527` BattleChanger 입력 연결 | `CombatInputSurfaces.BindRoot` |
| `CreateInput` `:645-646·650-651·656-658` | 입력 Factory `CombatSceneInputFactory.CreateSceneInput`(같은 순서) |
| `HideActions` `:744-746·749` | `CombatInputSurfaces.Hide`(`HideActions`는 지움) |
| `StopInteraction` `:754-756·761-762` | `CombatSceneInput.Stop` |
| `:847` 거절 안내 `OnResult` | `CombatSceneInput.OnResult` |
| `:1141` 환경 호버 지우기 | `CombatSceneInput.CancelPresentation` |
| `RouteInput` `:1166-1169`(환경 호버 지우기·포인터, 바로 앞 Router 넘기기 `:1165`는 우리 줄) | `CombatSceneInput.Route`. 배치 확인 갈래·정보창 선택은 조정자에 남습니다 |
| `SyncEnvironmentHover` `:1178·1182` | `CombatSceneInput.HasEnvironmentHover`·`SyncEnvironmentHover` |
| 배치 확인창 콜백 `:1217`·`:1220` 뒤·`:1222`·`:1226`·`:1228`(`:1227` 예외 줄은 그대로) | `CombatSceneInput.RouteCommand`(Router만)·`Notices` |
| `:4` `using …AudioSystem`, `:103` `_audio` 필드, `:192` `_audio =` | 지움. 조정자는 오디오를 갖지 않습니다 |
| `:646` 입력 Factory의 오디오 인자 | `_displayServices.Audio` |
| `:885` 타임라인 줄 | `CombatDisplayValueReader.Read` |
| `:956` 완료 뒤 배경음 | `CombatSceneDisplay.ShowCompletion(completion, confirm)`. 확인이 참이면 표시가 냅니다. 검사 순서(연결 → 같은 표시 → 확인 → 배경음)는 같습니다 |

- 입력 Factory의 `CreateEnvironmentHover` 본문은 그대로입니다.

### 9.2 바꾼 현강님 줄 (V2-a·V2-b·V2-g)

- **V2-a:** `CombatViewSystem.cs:1181`의 `ResolvePointerTarget`에 셋째 인자로 사실의 `IsItemDragActive`를 넘깁니다(판정식의 기본값을 지움). 이 줄은 조정자의 `SyncEnvironmentHover`에 그대로 남고, 그 메서드의 `:1178`·`:1182`만 연결 입력 묶음을 부릅니다(§9.1). 환경 호버는 결과가 None인지만 보므로 결과는 같습니다.
- **V2-b:** `PlacementDragInteraction.cs`(현강님 파일)에 공통 수명 계약의 어댑터(`OwnerAdapter`)와 창구(`InputOwner`)를 더했습니다. 현강님 줄인 메서드는 그대로입니다.
  - 같은 파일에서 V2-a가 미리보기 비우기를 Buffer의 번호 있는 비우기(`ClearPreview(int)`)로 바꾸며 `_requests` 필드와 비우기 본문을 고쳤습니다. 둘 다 우리 줄입니다.
  - 묶음 `CombatInteraction`의 현강님 줄도 그대로입니다: `CanSelectDeckUnit`, `SelectDeckUnit`의 `_placement.SelectDeckUnit`, `CancelDeckSelection`, `Cancel`, `CancelDragOrAttack`의 `_attack.Cancel`.
  - `SelectDeckUnit`이 전경 전환 규칙 `BeginForeground`를 부르게 바꾼 두 줄은 우리 줄입니다.
- **V2-g(운영 입구 필수):**
  - `CombatViewSystem.cs:256`: `BindServices`의 텍스트 표시가 필수가 되어, null이면 `ArgumentNullException`입니다.
  - 환경 호버(V2-e에서 Factory로 옮긴 HEAD `:656-657`의 갈래)와 거절 안내(`CombatSceneInputFactory.cs:63-64`의 생략 갈래·주석)를 늘 만듭니다.
  - `CombatSceneDisplay.cs:204`(국면 배경음)·`:274`(승리 효과음): `_audio?.` → `_audio.`. 표시 생성자가 오디오를 필수로 받습니다.
  - `CombatDisplayServices.cs:49`: UI 서비스가 필수입니다.

### 9.3 고친 현강님 시험(허락)

- `FrameworkRuntimeCompositionTests.cs:228-229`(Q-T6 b): 리플렉션 경로를 조정자 `_combatQueries` → 표시 값 읽기(`_displayValues`)의 `_timeline`으로 맞췄습니다. 같은 객체 비교, 같은 문구입니다.
- `CombatSceneEventPresenterTests.cs:1449`(Q-T5): `new CombatDisplayServices(…)` → 시험 도우미 `CombatViewSystemTests.DisplayServices(…)`. 넘기지 않은 서비스(오디오·화면 전환·UI·카메라 효과)는 아무것도 하지 않는 가짜로 채웁니다.

### 9.4 화면·동작 차이

- 운영 화면·동작은 같습니다.
- 시험 조립에서는 예전에 꺼져 있던 거절 안내·환경 호버가 늘 만들어집니다. 시험의 텍스트 표시는 아무것도 그리지 않는 가짜입니다. 환경 호버를 만들려면 View DB 표가 있어야 해서, 시험 조립도 실제 DB를 한 번 읽어 받습니다.
- 표시 계약 `ICombatBattleDisplay.ShowCompletion`이 확인 함수(`Func<bool>`)를 받습니다. 이 계약을 구현하는 클래스를 새로 만들면 같은 꼴로 구현해 주세요.

### 9.5 알려 드리는 것

- 조정자의 여러 줄을 새 클래스로 옮겨서, blame에서 현강님 이름이 우리 이름으로 바뀐 줄이 많습니다. 원래 기록은 기준 커밋 `23436b00`에서 볼 수 있습니다.

## 10. 전투 View 단계 3라운드 (2026-10-25, 미전달)

근거: [3라운드 설계](../specs/2026-10-25-combat-view-stage-round3-design.md), [3라운드 Task 계획·실행 기록](../plans/2026-10-25-combat-view-stage-round3-plan.md). 코드는 작업 트리에 있고 커밋 전입니다(커밋은 사용자). Phase V3-a~V3-i 모두 **컴파일 미확인**입니다(Unity 확인은 사용자). 줄 번호는 기준 커밋 `35562e96` 기준입니다.

이번 라운드는 조정자를 수명별 객체로 더 나누고(W11), 공격 종류 지식을 전략으로 모으고(W8), 화면 전환·소리 값을 View DB로 옮긴 작업(W9)입니다. 운영 화면·동작은 같습니다(아래 §10.5의 소리 차이 하나만 이론상 다름).

### 10.1 배치 확인창과 진입 콜백 (V3-b)

- 새 배치 진입 `Input/ActionButtons/CombatDeploymentEntry.cs`가 현강님 `CombatDeploymentConfirmation`을 만들어 갖습니다. 확인창 클래스는 그대로입니다.
- 조정자(`CombatViewSystem.cs`)에서 옮긴 현강님 줄
  - 필드 `:129`, 확인창 생성 `:264-265`
  - 진입 갈래 `:1092`, 행동 버튼 맞춤 `:1122`
  - 콜백 넷 `:1131-1157`(진입 가능 판단·진입 제출·입력 정리·메인 미배치 안내) → 배치 진입 안으로
- 닫기 `:679-680`·`:722-723`·`:881`은 지금 자리(분리·입력 정지·입력 닫힘의 맨 앞)에 남았고, `_input?.Deployment.Close()`를 부릅니다.
- 확인창 캐스트(`text as IUIConfirmationPresentation`)는 입력 Factory로 옮겼습니다.
- 확인창의 수명이 서비스에서 씬 연결로 바뀌었습니다. 확인창은 첫 그리기 뒤에만 열리고 분리 때 닫혀서 보이는 차이는 없습니다.

### 10.2 덱 호버 (V3-c)

- 조정자 `:119-120`(필드·서비스 멤버), `:520-521`(생성), `:739-740`(해제)이 연결 객체 `CombatViewConnection`으로 옮겨졌습니다(생성은 `BeginUse`, 해제는 `StopInput`).
- 서비스 멤버 `DeckHover`의 서명은 그대로입니다.

### 10.3 기차 소리 판별 (V3-d)

- `CombatEventSounds.cs:113`: 기차인지는 사건 형식 표의 `CombatEventTypeTable.IsRailway`가 답합니다(재생 조정과 같은 함수). `:6` using도 바꿨습니다.

### 10.4 입력 소리와 배치 Drag 서명 (V3-d·V3-h)

- `PlacementDragInteraction` 생성자: `(input, picker, feedback, gate, CombatInputSounds sounds = null)`.
  - V3-d: 쓰지 않던 `requests` 인자를 지웠습니다(우리 줄).
  - V3-h: 오디오 인자 자리에 새 소리 창구 `Input/Interaction/CombatInputSounds.cs`(오디오 + 프리셋)가 왔습니다.
  - 현강님 줄 `:34`(오디오 필드 → 소리 창구), `:35`(`-1` → `CombatSoundPresets.None`), `:57`(서명), `:204`·`:283`(소리 내기)
- `AttackTargetingInteraction`: `:43`(필드), `:60-61`(서명, 병합 `d0ac086a`), `:133-135·138`(취소 소리)를 같은 꼴로 바꿨습니다.
- "프리셋이 0 이상일 때만 낸다"는 판정은 `CombatSoundPresets.IsSound` 하나입니다. 소리 창구와 사건 소리 단서(`CombatSoundCue.Effect`)가 함께 씁니다.
- 입력 Factory: `CreateSceneInteraction`·`CreateInteraction`이 오디오 대신 소리 창구를 받습니다(같은 자리).

### 10.5 소리 프리셋을 View DB로 (V3-h)

- 지운 것: `CombatEventAudio.UnitPreset`·`AttackPreset`(현강님 switch 항목 `:76·78·81-85` 포함), `CombatEventSounds.BgmPreset`.
- 값은 이제 View DB에 있습니다.
  - 유닛 외형(`UnitVisualData`) 칸 둘: `unitSoundPreset`(아군일 때 고르기·이동·취소·사망)·`attackSoundPreset`(공격·포효). 비우면 −1 = 소리 없음입니다.
  - 전투 연출 설정 칸 넷: 적 공용 유닛 프리셋 5, 배경음 밤 3·낮 2·완료 뒤 1
  - 외형 에셋 열넷의 값은 옛 switch·분류 규칙과 같습니다.
- 새 값 객체 `Playback/Support/CombatSoundPresets.cs`가 번호를 답합니다(`Unit(key, owner)`·`Attack(key)`·`Bgm(phase)`). 표시 표가 한 번 만들고, 입력·표시·사건 소리가 같은 객체를 씁니다.
- **새 유닛을 더할 때는 외형 에셋의 두 칸만 채우면 됩니다.** 코드 switch는 없습니다.
- 바꾼 현강님 줄: `CombatEventSounds.cs:104-105`(포효), `:110`(밤 배경음 호출), `CombatSceneDisplayFactory.cs:162`(표시 생성 인자).
- 차이: 포효 소리는 이제 **포효한 유닛의 공격 프리셋**입니다(옛: 늘 Stage1Boss 공격 프리셋 15). 지금 데이터에서는 보스만 포효해 같습니다.

### 10.6 사건 Presenter (V3-i)

- 생성자가 필수 인자 여덟이 됐습니다: `(units, values, audio, sounds, camera, effects, projectileRoot, ports)`.
  - 값 묶음 `Playback/Support/CombatEventPresentationValues.cs`(설정 표 `From`)와 필드 창구 `Playback/CombatFieldEventPorts.cs`(필수 대리자 여덟)를 받습니다.
- `CombatSceneEventPresenter.cs:127`(현강님 줄, 포효 처리기 생성): 카메라·카메라 연출 값을 늘 넘깁니다. 포효 처리기도 둘을 필수로 받습니다.

### 10.7 고친 현강님 시험(허락)

| 시험 | 바꾼 것 |
|---|---|
| `CombatEventSoundsTests` `:68·105·114·122·163` | 소리 표·사건 소리 호출에 실제 DB 프리셋(`CombatViewSystemTests.SoundPresets`)을 넘김(Q-T7 d) |
| `CombatEventSoundsTests.Roar_WithoutTargets_PlaysBossAttackSound` | 포효한 유닛 사실을 Stage1Boss 키로 두는 준비 줄을 더함. 기대값 15 그대로(사용자 결정) |
| `GameAudioTests.ClassArguments_SelectBootPresetsWithoutPlayback` | FMOD 반복이 분류 인자를 `분류/10`으로 바로 넘김(분류 규칙이 코드에서 없어짐, 바뀐 줄 `:36`은 우리 줄) |
| `GameAudioTests` 분류 시험 셋(`MainCharacter_UsesPresetZero`·`ClassPreset_MapsCoreValues`·`Enemy_OverridesMainAndClass`) | 분류 사례를 그 분류의 실제 유닛으로: 메인 Akimbo 0, Sniper 1·Cowboy 2·ShotGun 3·CactusCannon 4·본부 −1, 적 5(사용자 결정). 바뀐 현강님 줄 `:51·53·57-61`, 천명님 줄 `:52·56`(우리 줄 `:54·62·66`) |
| `GameAudioTests.AttackPreset_UsesAuthoredOrder` | `AttackPreset(key)` → DB 프리셋(바뀐 줄 `:81`은 우리 줄, 현강님 `:80`·사례 줄은 그대로) |
| `StageMapCombatEntryBootSmokeTests:131` | DB 프리셋으로 읽기(Q-T7 d) |
| `CombatSceneEventPresenterTests:270·515-516` | 사건 Presenter를 우리 시험 도우미 묶음으로 만듦(Q-T7 e) |
| `CombatSceneEventPresenterTests:529` | 포효 소리 기대값 15 → 포효 유닛(`_initial`)의 DB 공격 프리셋(사용자 결정, 시점 확인은 그대로) |
| `CombatInteractionTests:76` | 소리 창구 인자 하나(`sounds: new CombatInputSounds(_audio, CombatViewSystemTests.SoundPresets)`, 사용자 허락). 고르는 Cowboy는 프리셋 2라 확인 내용 그대로 |

### 10.8 알려 드리는 것

- `GameAudioTests.AttackPreset_UsesAuthoredOrder`의 사례 둘(`Unit_MainCharacter` 0·`Unit_Akimbo` 1)은 기준 커밋에서도 실패하던 것이고 지금도 같은 까닭으로 실패합니다(MainCharacter는 외형 표에 없어 −1, Akimbo 공격 프리셋은 0). 기대값을 고칠지는 정해 주세요.
- `MainCharacter_UsesPresetZero`는 이제 Akimbo(지금 메인)를 봅니다. 메서드 이름은 그대로 두었습니다.
- 조정자·Presenter의 여러 줄을 새 클래스로 옮겨서 blame에서 현강님 이름이 우리 이름으로 바뀐 줄이 있습니다. 원래 기록은 기준 커밋 `35562e96`에서 볼 수 있습니다.

## 11. 전투 View 4라운드 V4-f 기존 상태/특성 표시 (2026-10-26, 미전달)

- 사용자 Q-V30·31·Q-T8 b 및 **"V4-f 진행해"** 지시로 기존 UI 표시 접점만 수정했다. [4라운드 계획 §6](../plans/2026-10-25-combat-view-stage-round4-plan.md#6-문서-변경실행-기록)에 코드/검증 경계를 기록했다. **컴파일·기존 시험·Unity 동작 미확인**, W15 독립 검토는 V4-g 끝이다. 이 메모를 외부로 발송하지 않았다.
- `CombatUnitInfo`: 기존 이동/공격 비용에 완료 Buffer의 행동 사용 상태와 같은 키 Core 응답의 짧은 불가 사유를 함께 표시한다. A→B 전환·같은 값 Refresh·가려졌다 재표시될 때 현재 개체의 값으로 복원한다. 숫자만 있던 TMP 영역은 같은 참조로 50×50·숫자 18px/상태 12px로 맞췄다. 실제 해상도에서 읽기/겹침 확인이 필요하다. 비용 강조 Root·아이콘·배경·UIBase/Deck 등록·Scene/Prefab은 유지했다.
- `NightCheckerUI`: 기존 live Buffer 읽기 시점에서 완료된 아군 턴 수와 배치/아군 준비/아군 턴/적 준비/적 응답을 표시한다. StagePhase·ControllerPhase도 비교해 같은 8/8의 진영 변화를 놓치지 않는다. 적 응답 8/8을 밤 종료로 표시하지 않는다.
- `UnitCardTraitContent`: Snapshot 경로가 공개한 전체/부분 불가 사유만 이름/설명에 덧붙인다. 정적 TraitKey 경로에는 현재 활성 여부를 붙이지 않는다. 가시의 개체별 수치와 기존 아이콘·특성 순서·원래 설명은 보존했다. 가시 등 전체 보류와 우선도 등 부분 보류의 실제 표시·긴 문구 맞춤은 수동 확인이 필요하다.
- 기존 `CombatUnitInfoTests`는 승인된 비용 기대 2개만 맞췄으며 기존 사례/단언 수는 유지했다. `UnitCardTraitContentTests`의 기존 독립 Snapshot/정적 키 기대는 영향이 없어 수정하지 않았다. 새 시험·사례·단언은 추가하지 않았다. 우리 공통 FakeDisplay의 새 표시 메서드 빈 구현은 계약 보정만이다.

## 2026-10-26 V4-h — 폐지된 복귀 버튼/입력 접점 (미전달)

- 새 Core에서 폐지된 체크포인트 복귀의 사용자 접점만 정리했다. `CombatActionPanel.returnButton`은 선택 참조이며 null이어도 Bind/입력 Attach/표시가 가능하다. 연결된 옛 버튼은 숨기고 누를 수 없게 유지한다. 공격·턴 종료의 필수 참조와 기존 UIInputBinding/수신 슬롯 계약은 그대로다. 씬/프리팹 객체는 삭제하거나 수정하지 않았다.
- `CombatInputController`는 Combat.Return을 소비하지 않는다. 예약 액션·기본 R 바인딩·KeyBindingAction.Return/저장 호환·세 번째 BindActions 인자와 RequestReturn 경로는 보존한다. 배치 유닛 회수와 우클릭 취소는 유지한다. 설정 화면의 복귀 항목 노출과 호환 사슬 정리는 후속 작업이다.
- Q-T8 d 허락 범위에서 기존 StageInputTests의 R 이벤트 기대와 CombatDisplayReuseTests의 복귀 기대/선택 참조 준비만 맞췄다. 새 시험 사례는 없고 실제 이동 경로·재생/결과 확인과 나머지 UI 입력 검증은 유지한다. 현재 사용 안내는 `viewDocs/stage-input-guide.md` §2.1~2.2다.
- **컴파일 미확인**, Unity/EditMode 미실시. 미연결 상태 진입·기존 버튼 숨김·R 무동작과 공격/턴 종료·배치 회수 확인이 필요하다. 구현/검토 원본은 [4라운드 계획 §6](../plans/2026-10-25-combat-view-stage-round4-plan.md#6-문서-변경실행-기록)이며 이 메모는 외부로 발송하지 않았다.

## 12. 2026-10-26 V4-m — 4라운드 UI/입력/기존 시험 인계 (미전달)

이번 라운드는 구조 개선보다 새 Core 규칙 대응을 우선했다. 실제 상태와 문서 지도는 [4라운드 계획 §7](../plans/2026-10-25-combat-view-stage-round4-plan.md), 사용 방법은 [스테이지 입력 안내 §2.1~2.4](../viewDocs/stage-input-guide.md)다. §11·V4-h 절의 구현에 a/d/e 변경까지 모아 알린다. **코드·독립 정적 검토 완료와 Unity 인수 완료는 구분한다.**

| 변경 파일/접점 | 현재 동작과 유지 계약 |
|---|---|
| `CombatUnitInfo.cs`·`CombatSceneDisplay.cs`·`CombatViewSystem.cs` | 같은 필드 유닛 재누름은 선택을 유지하며 강조를 즉시 맞춘다. 다른 개체/빈 칸 전환은 현재 선택 정책을 따른다. 덱 핀 토글과 입력 차단·공격/경로/재생 중 보호는 유지한다 |
| `CombatActionButtons`·공격/이동 입구·`CombatActionNotice` | 버튼과 키가 같은 현재 가용성을 읽는다. 공격 확인 중 새 진입/늦은 자동 실행은 없고 모드 끄기는 유지한다. 비용 부족은 “공용 AP가 부족합니다!”다. 실제 시도/최종 거절만 안내한다 |
| `CombatUnitInfo`·`CombatActionStateText`·`ICombatBattleDisplay.ShowActionAvailability` | 기존 비용 칸에 행동 상태/불가 사유를 붙이고 슬라이드/재표시 뒤 복원한다. 표시 변환 새 .cs와 .meta를 함께 인수한다. 기존 아이콘/강조 Root/등록/개폐는 유지한다 |
| `NightCheckerUI.cs`·`UnitCardTraitContent.cs` | 완료 아군 턴 수+현재 진영/국면, Snapshot 전체/부분 사용 불가를 표시한다. 정적 TraitKey에 현재 활성 상태를 추측해 붙이지 않는다 |
| `CombatActionPanel.cs`·`CombatInputController.cs` | 폐지된 복귀는 숨김/무동작, returnButton은 선택 참조다. R은 소비하지 않지만 예약 액션/키 저장값·호환 슬롯/세 번째 인자는 보존한다. 공격·턴 종료의 필수 참조, 배치 회수/우클릭 취소/Pointer/Submit은 유지한다 |

승인된 기존 시험 변경:

- Q-T8 a/b: `CombatUnitInfoTests`의 같은 필드 선택 기대와 이름, 비용 텍스트 기대 2개만 맞췄다. 덱/슬라이드/아이콘 등의 기존 단언은 유지한다. `UnitCardTraitContentTests`는 기존 기대에 영향이 없어 수정하지 않았다.
- 별도 Q-T8 f/g 허락: `CombatActionNoticeTests` 2개, `CombatInteractionTests` 2개, `CombatUnitRangeHoverTests` 1개, `MovementDraftTests` 1개의 비용 부족 기대 문자열만 공용 AP로 변경했다.
- Q-T8 d: `StageInputTests`는 R을 보내되 Return 이벤트를 기대하지 않는다. `CombatDisplayReuseTests`는 폐지 복귀 기대와 선택 참조 준비만 맞췄고 실제 이동 경로/견적/재생/결과 확인 및 버튼 입력 검증은 유지한다. `UnityStageInputTests`의 예약 액션 고정물/설정 검증은 그대로다.
- 공통 `CombatViewSystemTests` FakeDisplay는 새 표시 메서드 빈 구현을 추가했다. 새 시험 사례는 없다. 호진님 공격 Drag의 Ready 고정물/대기와 관련 Policy 기대는 호진님 메모 및 계획 §5를 따른다.

W13~W16 정적 검토에서 남은 구체적 결함은 없지만 **변경 후 컴파일·기존 EditMode·Unity는 미확인**이다. 정보창 50×50의 읽기/줄 맞춤·A→B/슬라이드·8번째 적 응답 표시·긴 특성 사유·복귀 미연결/연결 상태와 기존 입력은 수동 확인이 필요하다. 씬/Prefab/DB·등록 정책은 바꾸지 않았다. 보류 효과의 지원 재개와 설정 화면의 복귀 항목 정리는 후속이며 외부 발송·커밋/push는 하지 않았다.

## 13. 2026-10-26 V5-a — 선택 원본 분리/정보창 시험 준비 (미전달)

- 정보창은 Display 소유 ICombatSelectionReader로 선택을 읽는다. SelectCell/선택 원본 쓰기는 SelectionState로 옮기고 기존 카드/슬라이드/행동 비용 표시와 UI 등록·부모/Policy·아이콘/GUID를 유지했다. Hide/정보창 Clear가 빌린 선택을 변경하지 않는다.
- Display가 완료 표시 목록을 한 번 고정해 정보창/선택과 공유한다. InspectUnit에서 상태 선택→정보/강조만 갱신하고 전체 Refresh나 Core 확인을 호출하지 않는다. StopInput은 마지막 표시 선택을 먼저 지우지 않으며 Display가 정리한다.
- 기존 CombatUnitInfoTests의 초기 자동 메인 선택/빈 칸 뒤 메인 HP 복귀 기대가 현행 무선택/해제와 달라 추가 허락을 받았다: 사용자 "위 기존 기대·준비 정정 허용". 초기 표시 사례는 첫 유닛을 명시 선택하고, 기존 선택해제 사례는 무선택/숨김, 원래 유닛 복귀 사례는 실제 원래 칸 클릭으로 맞췄다. Clear 고정물은 표시 소유 상태를 함께 정리한다. 새 사례/특성 선언은 추가하지 않았다.
- Q-T9 a/d의 나머지 기존 4파일은 명시적 Attach/Inspection 및 Fake Reader 준비/옛 네임스페이스만 맞췄다. FakeDisplay의 기본 선택 1은 유지한다. 새 기대 허락을 다른 타인 시험에 확대하지 않는다.
- W17 새 읽기 전용 검토에서 보고할 P1/P2 결함 없음. 변경 후 컴파일·기존 EditMode·Unity는 미확인이다. 기존 V4-n은 사용자 정상 보고로 인수했으나 이 변경 후 결과와 구분한다. 외부 발송/커밋/push는 하지 않았다. 원본은 5라운드 계획 §5·§8이다.

## 14. 2026-10-26 V5-b — 입력 사실 명칭/시험 참조 (미전달)

- View Facts의 ActiveActorId/activeActorId만 SelectedActorId/selectedActorId로 바꾸고 SceneInput/공격 조준/가용성/Path 공급자를 맞췄다. Core ActorId·진영 턴·FieldHighlightLayer.ActiveActor와 UI 등록/표시/선택 소유권은 유지한다.
- Q-T9 b의 기존 10파일은 멤버/이름 붙인 생성자 인자만 기계적 변경했다. 역치환/줄바꿈 정규화 후 시작 전체 내용과 해시가 일치한다. 기대값/검증 의도/새 사례/특성 선언은 바꾸지 않았다. a의 별도 초기 선택/해제 기대 허락을 b의 기대 변경에 확대하지 않았다.
- a 변경 후 사용자 컴파일·기존 시험·Unity 정상 보고를 받아 인수했다. b는 W18 새 읽기 전용 검토에서 보고할 P1/P2 결함이 없었으며 변경 후 컴파일/기존 시험/Unity 미확인이다. 기존 정보창/선택 시험 5파일에 새 의미 변경을 추가하지 않았다. 상세 원본은 5라운드 계획 §5·§8, 외부 발송/커밋/push 없음.

## 15. 2026-10-26 V5-c — 기존 부분 조립 도우미/명시적 확인창 (미전달)

- Q-T9 c 승인 범위로 기존 CreateInteraction 부분 조립을 시험 어셈블리 CombatInputTestFactory.cs/meta로 옮겼다. 기존 6파일은 호출 위치와 Path의 단일 Facts 선택 준비만 맞췄다. 새 Test/TestCase/사례는 없다.
- Q-T9 d 범위로 공통 SilentText는 IUIConfirmationPresentation을 명시 구현한다. receiver의 열림/닫힘을 보관하고 자동 응답/명령 제출은 하지 않는다. 운영의 잘못된 누락을 Empty/무동작으로 덮지 않는다.
- 기존 시험 7파일의 특성 선언/Assert 포함 줄이 시작 내용과 같고 기대값·검증 의도는 유지했다. 확인창 누락 진단의 기대를 바꿀 실제 사례는 현재 조사에서 발견하지 못했다. 이후 발견되면 Q-T9 e의 정확한 사례/줄 별도 허락을 따른다.
- b 수정 후 사용자 전체 정상 보고로 인수했고 c는 W19 새 읽기 전용 검토에서 보고할 P1/P2 결함 없음, 변경 후 컴파일/기존 EditMode/Unity 미확인이다. 정보창/선택·공용 UI 등록/개폐와 Core/Framework·씬/Prefab/DB는 수정하지 않았다. 외부 발송·커밋/push는 하지 않았다. 원본은 5라운드 계획 §5·§8이다.

## 16. 2026-10-26 V5-d — 선택·정보창·기존 시험 최종 인계 (미전달)

- 선택 원본은 UnitInfo에서 Display 소유 CombatSelectionState로 옮겼다. UnitInfo는 ICombatSelectionReader와 Display가 고정한 동일 완료 표시 목록을 읽고, `_shown`/슬라이드/비용 상태는 표현 캐시로 유지한다. UI Hide/Clear가 빌린 선택을 초기화하지 않는다. 같은 필드 유닛 재클릭은 유지, 빈 칸은 해제, 적은 정보만 표시한다. 덱 핀 토글·UIBase/UISystem·입력/개폐 계약은 유지한다.
- 현재 Facts 명칭은 SelectedActorId이며 Reader가 현재 연결에서 읽는다. 일반 Path도 같은 Facts를 사용한다. 빈 범위 관측은 선택 ID 값만 받으므로 전체 Facts를 역호출하지 않는다. Core의 비용/가용성 판정이나 정보창 레이아웃을 Reader로 옮기지 않았다.
- 시험은 Q-T9의 기존 조립/기계적 변경·부분 조립 도우미와 a에서 사용자 **“위 기존 기대·준비 정정 허용”**한 초기 선택/빈 칸 해제/실제 재선택 준비만 바꿨다. 기존 6파일의 부분 Factory 호출/Path 준비는 시험 CombatInputTestFactory로 연결, 공통 SilentText는 확인창 계약을 명시하되 자동 승인하지 않는다. c에서 예외 시점 단언을 바꿀 대상은 발견하지 않았고 추가 허락을 임의로 확대하지 않았다. 새 Test/TestCase/사례 없음.
- a/b/c 각각 사용자 컴파일·기존 시험·Unity 정상 보고와 W17~19 정적 검토 뒤 d 통합 인수를 마쳤다. b의 StagePhase using 누락은 수정 후 정상 확인받았다. d에는 코드/시험 수정 없음. 실행 기록/확인 공백·후속 구조 작업은 [5라운드 계획 §10](../plans/2026-10-26-combat-view-stage-round5-plan.md#10-v5-d-최종-인수와-잔여-2026-10-26)이 원본이다. 외부 전달은 아직 하지 않았다.

## 17. 2026-10-27 전투 카메라 조작 — 시험 가짜 한 곳 (미전달)

- 스테이지 입력 수신 계약 `IStageInputReceiver`에 카메라 메서드 둘(`CameraPanChanged(x, y)`·`CameraZoomScrolled(delta)`)이 생겼습니다(카메라 조작 계획 P9).
- 그래서 `BattleChangerUITests.cs`의 기록 가짜 `Receiver`에 빈 메서드 둘을 더했습니다. 컴파일을 맞추기 위한 것이고 시험 본문·단언은 그대로입니다.
- `BattleChangerUI`는 계약을 쓰기만 하므로 고칠 곳이 없습니다. 카메라 가장자리 이동은 포인터가 UI 위면 하지 않습니다(손패·HUD 위에서 멈춤).

## 18. 2026-10-30 특성 시스템 — 거절 안내 두 줄 (미전달)

- `Input/Assist/CombatActionNotice.cs`의 사유 표(현강님 줄)에 특성이 건 행동 제한 둘을 더했습니다([특성 시스템 View 계획](../plans/2026-10-30-combat-trait-system-view-plan.md) TP-9·§7.5).
  - `AttackFailureReason.Disarmed` → "무장 해제 상태라 공격할 수 없습니다."
  - `MovementFailureReason.AnkleBroken` → "발목이 꺾여 이동할 수 없습니다."
- 문구는 초안입니다. 고치실 때는 이 두 줄만 바꾸면 됩니다. 결과 거절과 미리보기 거절이 같은 함수를 지나므로 둘 다 같은 문구가 나옵니다. 이동 거절의 `Disarmed`(같은 이름)도 같은 줄이 받습니다.
- 같이 더한 짧은 글자(행동 패널의 상태 글자 `Display/Hud/CombatActionStateText.cs`, 우리 파일): "무장해제"·"발목꺾임".
- 다른 UI 코드·프리팹은 바뀌지 않았습니다.

## 19. 2026-11-02 낙석 — 이동 불가 문구 둘·환경 설명 입력 (미전달)

- `Input/Assist/CombatActionNotice.cs`(현강님 줄): `MovementFailureReason.AnkleBroken` → "이동 불가 상태라 이동할 수 없습니다." 낙석 속박도 같은 사유라 발목 문구가 틀렸습니다.
- `Display/Hud/CombatActionStateText.cs`: 같은 사유의 짧은 글자 "발목꺾임" → "이동불가".
- `Input/Assist/CombatEnvironmentText.cs`(현강님 줄): 경고 칸 호버 설명을 더했습니다. `Build`는 `(CombatStateData state, CellCoord cell)`로 줄었고, 생성자 끝 인자에 낙석 설정을 받습니다. 표시되는 다른 설명은 그대로입니다.
- 문구는 초안입니다. 근거는 [낙석 계획](../plans/2026-11-01-combat-rockfall-view-plan-Hojin.md) RP-10·RP-11입니다. 컴파일 미확인.

## 20. 2026-11-02 하붑 — 거절 안내 한 줄·환경 설명 입력 (미전달)

- `Input/Assist/CombatActionNotice.cs`(현강님 줄): `AttackFailureReason.HaboobDesignationBlocked` → "공격자와 지정한 칸이 모두 먼지구름 안이거나 모두 밖이어야 합니다." 규칙을 설명하는 긴 문장이라 이 안내만 2초입니다(`RuleDuration`, 임시 값). 다른 안내는 0.5초 그대로입니다.
- `Input/Assist/CombatEnvironmentText.cs`(현강님 줄): 구름 칸 호버 설명을 더했고, 생성자 끝 인자에 하붑 설정을 받습니다. 표시되는 다른 설명은 그대로입니다.
- 문구는 초안입니다. 근거는 [하붑 계획](../plans/2026-11-01-combat-haboob-view-plan-Hojin.md) HP-11·Q-HP1입니다. 컴파일 미확인.

## 21. 2026-11-03 전투 GC 끊김 — HUD 그리기·HUD 층 (미전달)

적 진행 중 프레임 끊김을 줄이는 View 묶음([GC 끊김 계획](../plans/2026-11-03-combat-view-gc-hitch-plan.md) §3.3·§3.4)에서 현강님 줄(`1013a4df` HUD 레이아웃) 둘레를 고쳤습니다. 보이는 HUD 내용·동작은 같게 두었습니다. 컴파일·재생은 사용자 확인(Uc·Ud) 문제 없음.

- `Display/Unit/FieldUnitPresenter.cs` `RenderUnitHud`(GC-c): 사건마다 모든 유닛을 다시 그릴 때, 그 유닛 HUD에 **같은 사실 객체(불변)**를 이미 다 썼으면 보이기(`ShowUnitHud(true)`)만 맞추고 돌아갑니다. 처음이거나 사실이 바뀌면 지금처럼 `ValidateReferences` → `SetUnit` → `SetInfo` → `SetItem` → `SetStatuses`를 모두 쓰고, 다 쓴 뒤에만 기억합니다(`FieldUnitView.RememberHudFacts`).
  - 기억을 비우는 때: HUD 붙이기·떼기, 다른 개체로 묶기, HUD가 숨겨질 때(`UnitHUD.OnUIHidden`이 체력 상태를 지우므로), HUD 층이 숨겨질 때(아래 `OnUIHidden`), `Configure`·`ConfigureHUDCategories`(표가 바뀔 수 있음). 목록 원본은 `FieldUnitView._hudFacts` 주석입니다.
  - HUD 내용에 새 값(사실·표 밖의 것)을 더하실 때는 이 건너뛰기를 알아 두셔야 합니다. 사실 객체 밖에서 바뀌는 값이면 그 값이 바뀔 때 `ForgetHudFacts()`를 부르거나 건너뛰기 조건에 넣어야 다시 그려집니다.
- `Display/Unit/UnitHUDLayer.cs`(GC-d):
  - `RefreshPositions`: HUD 위치를 지금 값과 비교해 바뀔 때만 씁니다(배율과 같은 `!=` 규칙). 카메라·유닛이 멈추면 HUD `RectTransform`에 쓰지 않아 캔버스를 다시 묶지 않습니다.
  - `RefreshSelectedInfo`: 선택 HUD `ValidateReferences`를 잡힌 선택 HUD가 바뀔 때만 합니다(새 칸 `_selectedHud`, `Release`가 비움). 찾기 반복·맨 앞 올리기 확인은 매 갱신 그대로입니다 — HUD의 개체가 `UnitHUD.SetUnit`·`Clear`로 층 모르게 바뀔 수 있어서입니다.
  - `OnUIHidden`(묶음 끝 검토 반영, 계획 §3.8 L1): `HideAll` 뒤 살아 있는 기준점마다 새 계약 메서드 `IUnitHUDAnchor.OnHudLayerHidden()`을 부릅니다. 층과 함께 꺼진 HUD가 체력 상태를 지우므로, 유닛 표시가 HUD에 쓴 사실 기억을 버려 다시 켜진 뒤 첫 그리기가 HUD를 모두 다시 씁니다. 필드만 비우는 호출이라 UISystem `HideAll` 순회 중에도 안전합니다. 이것으로 GC-c 때 적었던 "층이 꺼졌다 켜지면 다음 체력 감소가 애니메이션 없이 줄어듦"이 없어졌습니다.
  - 기준점 생존 판정(파괴된 Unity 오브젝트가 아닌지)을 `IsAlive` 하나로 꺼내 `Register`·`RefreshPositions`·`OnUIHidden`이 부릅니다(식은 그대로).
- 남는 매 프레임 쓰기(UISystem 화면, 고치지 않음 — 나중 요청 목록): 선택 정보 호버 창 `UnitHUDHoverPanel.RefreshPosition`이 층의 매 갱신마다 피벗·위치를 비교 없이 씁니다. 설명 창 `TextNoticeUI.LateUpdate`도 떠 있는 동안 매 프레임 위치를 다시 씁니다. 같은 값이면 쓰지 않게 하시면 같은 효과가 납니다.

## 22. 2026-11-04 전투 View 평가 후속 3·4차 — 배치 소리 호출 두 줄 (미전달)

- `Input/Interaction/PlacementDragInteraction.cs` 202·281(HEAD `032608bf`, `df422cbc` 줄, C5-02): 소리 창구가 필수 생성 인자가 되어 `_sounds?.Play`를 `_sounds.Play`로 바꿨습니다. 소리는 그대로 납니다. 근거는 [후속 수정 계획](../plans/2026-11-04-combat-view-evaluation-fixes-plan.md) §8입니다.
- 같은 파일의 1차 G3-01(재생 중 놓기 → 손에 남김) 변경은 사용자 쪽 줄입니다.
- 경계 커밋(`^ad7e6b28`, 현강님으로 표시)인 `CombatRefreshSignal`·`CombatViewRefresh` remarks, 그리고 10-20 공유 문서상 현강님 코드인 `CombatEnvironmentText` 53줄(보임 조건 → `CombatEnvironmentFacts.IsShown`)도 고쳤습니다. 작성자가 확실하지 않아 사용자 확인 뒤 넘깁니다(계획 §5.3).
- 컴파일 미확인, 새 시험 없음.

## 23. 2026-11-05 전투 View 평가 후속 — 질문 둘 (미전달)

5차([후속 수정 계획](../plans/2026-11-04-combat-view-evaluation-fixes-plan.md) §9)에서 현강님 줄은 바꾸지 않았습니다. 평가 중간 문제 가운데 현강님과 정해야 하는 것 둘을 여쭙니다.

- **C1-38 약한 지반 소리 프리셋 — (2026-11-12 정함: 설정 표 원본, §30)** `Playback/Support/CombatEventSounds.cs` 158·178-186줄은 11-02 `8fe4a688b`("환경요소 사운드 연결")에서 DB 값을 코드 숫자 2로 바꾼 줄입니다. 그래서 설정 표의 `weakGroundCollapseSoundPreset`(지금 -1)을 바꿔도 아무 일이 없습니다. 둘 중 어느 쪽으로 할까요?
  - (권장) DB 값 하나로: 표 기본값을 2로 맞추고 코드는 `presets.WeakGroundCollapse()`를 읽습니다. 진입 1·2회 프리셋(0·1)도 표 칸으로 올릴지 함께 정합니다.
  - 코드 숫자를 원본으로: 설정 칸과 `WeakGroundCollapse()`를 지웁니다.
  - 어느 쪽이든 붕괴 소리(2번)는 지금처럼 납니다. 표 값과 코드는 한 커밋에 넣어야 합니다.
- **C1-22 HP 막대 규칙:** 전투 정보창(`CombatUnitInfoView`)과 유닛 HUD(`UnitHUD`)가 HP·보호막 막대의 분모·스냅 규칙을 글자 그대로 따로 갖고 있습니다. 지금은 같지만 한쪽만 바뀌면 어긋납니다. 공통 함수를 `UISystem/Base/HealthFillAnimation.cs` 옆(예: `HealthBarRule`)에 두고 두 곳이 부르게 해도 될까요? 바꿀 줄이 모두 현강님 줄(GlobalSystem 포함)이라 위치·이름을 정해 주시면 그대로 맞추겠습니다.

## 24. 2026-11-05 전투 View 조정자 나누기 — 줄 둘 (미전달)

[조정자 나누기 계획](../plans/2026-11-05-combat-view-coordinator-split-plan.md)(설계 [coordinator-split-design](../specs/2026-11-05-combat-view-coordinator-split-design.md) CS1~CS6)에서 현강님 줄 둘을 고쳤습니다. 줄 번호는 HEAD `dfb49b34` 기준입니다. 새 시험 없음, 시험 변경 없음. Unity 컴파일·재생 문제 없음(사용자, Ua~Ud). UI 코드와 프리팹에서 고칠 곳은 없습니다.

- `Input/CombatSceneInput.cs` 116(`e2cb9912`, CS-a): 연결 입력 묶음의 부품 속성을 private 필드로 숨기면서 `ClearActionAvailability`가 `ActionAvailability.Clear()` 대신 `_actionAvailability.Clear()`를 부릅니다. 이름만 바뀌었습니다. 5줄 using과 115줄은 그대로입니다.
- `CombatViewSystem.cs` 251(`0a6fe9c4`, CS-d): 조정자가 서비스를 하나씩 받지 않고 묶음 `CombatViewBindings` 하나로 받게 되어, 표시 값 읽기 생성이 `new CombatDisplayValueReader(bindings.State, bindings.Characters, bindings.Currency, bindings.Current, bindings.Items)`가 되었습니다. 넘기는 값과 순서는 같습니다.

## 25. 2026-11-05 전투 View 작은 묶음 셋 — 환경 설명 갈래 넷 (미전달)

[작은 묶음 계획](../plans/2026-11-05-combat-view-small-bundles-plan.md)(설계 [small-bundles-design](../specs/2026-11-05-combat-view-small-bundles-design.md) SB3, 평가 C1-05)에서 `Input/Assist/CombatEnvironmentText.cs`의 현강님 줄(`63a1c23e`)을 고쳤습니다. 줄 번호는 HEAD `fedd5627` 기준입니다. 컴파일 미확인(전투 View 작업 규칙상 MSBuild 없음), 새 시험 없음, 시험 변경 없음. UI 코드와 프리팹에서 고칠 곳은 없습니다.

환경 원본을 어느 분류로 볼지의 판단을 `Display/Map/CombatEnvironmentFacts` 한 곳으로 모았습니다. 위험 범위 찾기·지형/오브젝트 표시·회전 검사가 같은 함수를 부르므로, 분류 정책이 바뀌어도 한 곳만 고치면 됩니다.

- 62 지형 갈래: `Contains(bindings.TerrainSourceIds, source.Id)` → `CombatEnvironmentFacts.IsTerrain(bindings, source.Id)`.
- 68-71 오브젝트 갈래: `Contains(bindings.ObjectSourceIds, …)` + 형식 검사 예외 → `CombatEnvironmentFacts.TryGetObject(bindings, source, out ObjectSourceFact item)`. 분류는 Object인데 형식이 다를 때의 예외 문구가 `[CombatEnvironmentFacts] Object 분류와 Buffer 원본 형식이 다릅니다: {id}.`로 바뀝니다(분류 계약을 어긴 Buffer에서만 남, 결정 Q-SB3 A).
- 80-81 장애물 갈래: 생존·점유 칸 줄(80, 현강님)과 장애물 찾기 줄(81, 용근 `5de448e3`) → `CombatEnvironmentFacts.TryGetObstacleAt(bindings, obstacles, structure, cell, out obstacle)` 한 줄. 판단 순서(생존 → 점유 칸 → 분류·프로필)는 같습니다.
- 123-127 쓰지 않게 된 `Contains(IReadOnlyList<long>, long)`를 지웠습니다. `ContainsCell`은 토네이도·기믹 반복이 써서 남습니다.

화면에 보이는 설명·순서(흔적 → 지형 → 오브젝트, 그다음 장애물·토네이도·기믹)는 그대로입니다. 54 먼지 연막 줄(용근 `950a52c0`)도 같은 판단 `IsDustScreen`으로 바꿨고, 호진님 줄(토네이도·기믹)은 바뀌지 않았습니다.

## 26. 2026-11-06 전투 View 선택 인자 나머지 — 확인창·거절 창구·덱 찾기·취소 (미전달)

[선택 인자 나머지 계획](../plans/2026-11-06-combat-view-optional-args-plan.md)(설계 [optional-args-design](../specs/2026-11-06-combat-view-optional-args-design.md) OA1~OA3, 평가 C5-02 2단계)에서 현강님 줄을 고쳤습니다. 작성자는 전체 이력으로 셌습니다(설계 §1.7). 줄 번호는 고친 뒤(기준 판 `7279a9d5` 위) 기준입니다. 컴파일 미확인(전투 View 작업 규칙상 MSBuild 없음), 새 시험 없음, 시험 변경은 우리 시험 호출 한 곳뿐입니다. UI 코드와 프리팹에서 고칠 곳은 없습니다.

운영 조립이 늘 넘기는 협력자를 생성자에서 필수로 받고, 쓰는 곳의 `?.`·null 갈래를 지웠습니다. 운영에서 보이는 동작은 같습니다. 바뀌는 것은 협력자를 빠뜨린 새 조립이 이제 생성 순간에 `ArgumentNullException`으로 멈춘다는 것입니다(전에는 그 기능만 조용히 빠졌습니다).

- `Input/ActionButtons/CombatDeploymentConfirmation.cs`(`fc133bf7`)
  - 32 생성자: `presentation ?? throw new ArgumentNullException(nameof(presentation))`. 31 주석도 같은 뜻으로 고쳤습니다.
  - 40 `IsOpen`·116 `Close`: `_presentation?.`·`?? false`를 지웠습니다.
  - 확인창을 여는 곳(옛 97-98)의 `InvalidOperationException` 갈래를 지웠습니다. 같은 사실을 생성 때 막으므로 예외 종류가 `InvalidOperationException` → `ArgumentNullException`으로 바뀝니다(계획 TP-5). 입력 재료 묶음이 null을 이미 거절해 운영에서는 둘 다 나지 않습니다.
- 거절 창구 `rejections`(`80eb7481`)
  - `Input/MovePath/CombatMovementInteraction.cs` 77 기본값 `= null`을 지우고 86에서 `?? throw`, 250·283 `_rejections?.MovementRejected` → `.`.
  - `Input/MovePath/CombatPathInput.cs` 90 기본값 `= null`을 지우고 95에서 `?? throw`. 경로 입력이 이동 상호작용에 그대로 넘깁니다.
- `Input/Interaction/PlacementDragInteraction.cs` 105 `FindDeckUnit`(`4dc3ba8a`): `characters == null ||`를 지웠습니다. 부르는 곳이 모두 Buffer 캐릭터 목록(null이 아님)을 넘깁니다. `ownedId.IsEmpty` 검사는 그대로입니다.
- `Input/Interaction/CombatInteraction.cs` 259 `Cancel(IReadOnlyList<CharacterData> characters)`(`1b155e02`): 기본값 `= null`을 지웠습니다. 부르는 곳(Router)이 늘 목록을 넘깁니다. 안에서 부르는 `AttackTargetingInteraction.Cancel`도 기본값과 안쪽 null 검사를 지웠습니다(용근 줄).

## 27. 2026-11-07 전투 View 씬 표시 나누기 — HUD·턴 표시·결과 화면·정보창·선택 HUD 줄 자리 (미전달)

[씬 표시 나누기 계획](../plans/2026-11-07-combat-view-scene-display-split-plan.md)(설계 [scene-display-split-design](../specs/2026-11-07-combat-view-scene-display-split-design.md) DS1~DS7, 평가 C2-03 2·3단계)에서 현강님 줄을 옮기거나 고쳤습니다. 작성자는 기준 판 `eafd42f5`의 전체 이력으로 셌습니다(계획 T0.2). 줄 번호는 고친 뒤 기준입니다. 컴파일 미확인(전투 View 작업 규칙상 MSBuild 없음), 새 시험 없음, 시험 변경 없음. **UI 코드와 프리팹에서 고친 곳은 없습니다**(`CombatEndUI`·`CombatHud`·`TurnDisplayUI`·`CombatUnitInfo`·`UnitHUDLayer`·`PackUI` 그대로).

`CombatSceneDisplay`(876줄)가 화면 표면과 선택 표시를 객체로 넘기고 수명·순서만 남겼습니다(347줄). 화면에 보이는 동작은 같습니다. 부르는 차례는 설계 §3.8 대응표대로 옛 차례와 같습니다.

- 턴 표시·정보창·HUD(`cc8be90e` `_turnDisplay`, `fb30cbd1` `_unitInfo`·`ClearUnitInfo`, `ad7e6b28` `_hud.Present()`)
  - 새 `Display/Hud/CombatHudSurfaces.cs`가 HUD·턴 표시·가방·정보창을 함께 받습니다. `Present`(38)·`RenderStatus(runGold, turn, phase)`(41, 옛 `_hud.Render` + `_turnDisplay.Render`)·`ClearTurn`(56)·`ClearUnitInfo`(59, 본문 그대로)입니다.
  - 표시는 `_hud.Present()`(170)·`_hud.RenderStatus(…)`(223)·정리 목록의 `_hud.ClearTurn`·`_hud.ClearUnitInfo`로 부릅니다. 옛 `RefreshCore`의 `_turnDisplay.Render` 자리와 같습니다.
- 결과 화면(`01f5c787` `_completion`·`HideCompletion`, 생성 인자 `completion`)
  - 새 `Display/Hud/CombatResultScreenDisplay.cs`가 `CombatEndUI`와 배경음 정책을 받습니다. `Hide`(44)는 옛 `HideCompletion` 본문(Unity null 검사 포함)이고, `Show`(31)는 승리 배경음 → 결과 화면 → 확인 뒤 배경음 순서입니다(옛 `ShowCompletion`과 같음).
  - `confirm` 대리자는 이제 null이면 `ArgumentNullException`입니다(계획 TP-7). 부르는 곳(결과 소비)이 늘 넘깁니다.
- 선택 HUD(`1013a4df` `SetSelectedEntity` 줄 넷, `fb30cbd1` `InspectUnit` 껍데기)
  - 새 `Selection/CombatSelectionDisplay.cs`가 선택 원본·정보창 Presenter·`UnitHUDLayer`를 받습니다. `RefreshInspectionSelection`(85)·`Clear`(95)·`InspectUnit`의 `SetSelectedEntity` 줄은 옛 자리와 같은 식입니다.
  - `InspectUnit`의 해제 뒤 막기는 공유 수명(`Display/CombatDisplayLifetime`)으로 봅니다(설계 DS1).
- `ReleaseUnitHudLayer`(`8b61db0f`)는 표시에 그대로 남았습니다(313, 본문 그대로). 해제 목록 맨 끝 차례도 같습니다.
- `Display/Feedback/CombatMovementPlanningPresenter.cs` 17 클래스 요약(`2fb4d2bc`): "이동 계약의 주인"을 덧붙였습니다. 표시가 하던 이동 초안 위임을 이 클래스가 직접 구현합니다(`ICombatMovementFeedback`).
- Factory 연결 줄(민형님 Factory 공유 §52에도 적음)
  - `CombatSceneDisplayFactory.cs` 111 `new CombatMovementPlanningPresenter(…)`(`2fb4d2bc`): 끝 인자 `pointer`를 더했습니다(커서 고스트가 설 자리를 포인터 매퍼가 구함, 설계 P9).
  - `CombatSceneInputFactory.cs` 49-56 `CreateEnvironmentHover`(`63a1c23e`): 첫 인자 `ICombatBattleDisplay display` → `CombatInputFeedback feedback`, 집기·UI 가림·기믹·본부 범위를 묶음에서 이름으로 꺼냅니다. 형식 검사 둘(`is not ICombatGimmickRangeFeedback` 등, 용근 줄)을 지웠습니다.
  - `CombatSceneInputFactory.cs` 153 `new CombatPathInput(…, notices,`(`80eb7481`): 앞 인자 둘 `display, display` → `feedback.Mapping, feedback.Movement`.
- 옮기기만 한 줄: `ICombatInteractionFeedback`의 `ClearAttackHealthPreview` 문서(`2fb4d2bc`)는 `git mv`한 `Input/Contracts/ICombatAttackRangeFeedback.cs`에 글자 그대로 있습니다.

## 28. 2026-11-11 포효 예고 — HUD 행동 가능 여부 조회 옆에 배경 조회 하나 (미전달)

기록: [포효 예고 계획](../plans/2026-11-10-combat-roar-forecast-plan.md) RF-b(TP-11). 보스 포효 카운터가 1 남으면 바닥에 포효 범위와 밀릴 유닛의 넉백 화살표를 보이는 기능이다. 현강님이 넣은 HUD 행동 가능 여부 조회(`CombatHudActionAvailability`, `f659532`)와 같은 자리·같은 꼴로 배경 조회 `CombatRoarForecastTracker`를 더했다. HUD 조회 코드 자체는 고치지 않았다.

- `Input/CombatSceneInput.cs`: 생성자 서명 끝에 `CombatRoarForecastTracker roarForecast` 인자 하나(현강님 줄 `:67`), 새 입구 `SyncRoarForecast`, 표현 정리(`CancelPresentation`)·정지(`Stop`)에 지우기 한 줄씩.
- `Input/CombatSceneInputDriver.cs` `TickEnd`: HUD 조회 바로 앞에서 포효 예고를 맞춘다(결과마다 한 번만 묻는 조회라, 유닛 수만큼 묻는 HUD 조회보다 먼저 슬롯을 씀). HUD 조회 호출 줄은 그대로.
- `Factory/Runtime/InGame/View/CombatSceneInputFactory.cs`: 생성 순서 주석(현강님 줄 `:61`)에 "포효 예고 조회" 한 단계, 반환 줄(현강님 줄 `:104`) 끝에 인자 하나.
- 화면 차이: HUD 조회가 슬롯을 쓰는 차례가 포효 예고 요청 하나만큼 늦어질 수 있다(보스 카운터가 1 남은 동안, 결과마다 한 번).
- 컴파일·Unity 확인 문제 없음(사용자), 새 시험 없음.

## 29. 2026-11-12 HUD 행동 가능 여부 조회 — Core 묶음 조회로 (미전달)

[새 SSOT 계획](../plans/2026-11-11-combat-view-new-ssot-plan.md) SN-c(설계 [new-ssot-design](../specs/2026-11-11-combat-view-new-ssot-design.md) SN2, 재평가 C1-N02)에서 현강님 파일 `Input/Interaction/CombatHudActionAvailability.cs`(`f6595326`)를 다시 썼습니다. 컴파일 미확인, 새 시험 없음, 시험 변경 없음.

- **왜:** 지금까지는 판이 바뀌면 "그 유닛 사실이 바뀌었나 + 필드에 여러 칸 유닛이 있나"로 다시 물을 유닛을 골라 하나씩 물었습니다. 둘째 식이 Core `DefersMultiCellAttack`의 사본이었고, Core 답이 다른 유닛·환경에 기대는 조건을 더하면 HUD가 옛 값으로 남을 수 있었습니다.
- **지금:** 판이 바뀌면 새 미리보기 종류 `ActionAvailabilities`로 Core에 한 번 묻고, 자격 아군 모두의 답(입력 유닛 순)을 한꺼번에 받습니다. 누구를 물을지는 Core 자격 규칙 `CombatActionAdmission.EligibilityFailure`가 정합니다.
  - 답이 올 때까지 이전 아이콘을 그대로 둡니다(설계 Q-SN4 A — 지금은 다시 물을 유닛 아이콘을 먼저 비움). 턴이 바뀌거나 국면·입력이 닫히면 지금처럼 모두 지웁니다.
  - 받은 목록에 없는 유닛(죽음·손패 복귀 등)은 숨깁니다.
  - 선택 유닛은 지금처럼 선택 추적기 답도 함께 보입니다. 마지막으로 보인 답과 같은 객체면 다시 그리지 않아 매 Tick HUD 쓰기가 없습니다.
  - 걷은 것: `_dirty`·`_observed`·`_next`·`_hasNonSingleCellUnit`·`ObserveCharacters`·`_requestedActor`·선택 유닛 건너뛰기.
- **서명:** `Sync`와 `CombatSceneInput.SyncHudActionAvailability`에서 `characters` 인자를 뺐습니다(입력 분배 `CombatSceneInputDriver` 호출 맞춤). 생성자와 Factory 줄은 그대로입니다.
- `CombatActionAvailabilityTracker.IsEligibleActor`(현강님 줄)는 이제 선택 추적기만 써서 `private`로 바꾸고, Core 원본을 가리키는 주석을 달았습니다.
- **화면 차이:** 이동·공격 뒤 답이 올 때까지(보통 1~2 Tick) 옛 아이콘이 남습니다. 결과 직후 공격 모드·끌기에 들어가면 그 입력이 끝날 때까지 남습니다. 대신 행동마다 다른 아이콘이 깜빡이지 않습니다(Unity 확인 U-9~U-11).

## 30. 2026-11-12 약한 지반·추락 소리 번호를 설정 표로 (미전달)

기록: [소리 프리셋 원본·곡사 공통 수명 계획](../plans/2026-11-12-combat-view-sound-preset-mortar-lifecycle-plan.md) SM-a(§10.2), 설계 [sound-preset-mortar-lifecycle-design](../specs/2026-11-12-combat-view-sound-preset-mortar-lifecycle-design.md) SP1~SP3. 재평가 C1-38(중간)·C1-N19(낮음)입니다. §23의 첫 질문(C1-38)은 사용자가 "설정 표를 원본으로"(권장안)로 정했습니다. 줄 번호는 HEAD `03d3555aa` 기준입니다.

- **왜:** 약한 지반 진입·붕괴 소리 번호가 코드 숫자(`Math.Min(2, 남은 횟수)`·`0`)여서 설정 표 칸 `weakGroundCollapseSoundPreset`을 바꿔도 아무 일이 없었고, 툴팁("-1이면 소리 없음")과 실제가 반대였습니다. 추락 칸 `fallSoundPreset`도 읽는 곳이 없었습니다.
- **바뀐 현강님 줄(`Playback/Support/CombatEventSounds.cs`):**
  - 추락 갈래 141-144(`8fe4a688b`·`c0f92a5d8`): `EffectWithoutPreset(UnitEvent.Fall)` → `EffectKeepingPreset(UnitEvent.Fall, presets.Fall())`. 칸이 -1(지금)이면 지금과 같은 소리입니다. 보인 캐릭터만 내는 조건은 그대로입니다.
  - 진입 갈래 145-146·도우미 189-197: 도우미는 "진입인지와 남은 횟수"만 답하고(`TryGetWeakGroundEntry`, 조건 그대로), 번호는 `presets.WeakGroundEntry(remaining)`이 설정 표 새 목록 `weakGroundEntrySoundPresets`(기본 `[1, 2]`, 남은 횟수가 더 많으면 마지막 원소)에서 고릅니다. 지금 소리와 같습니다.
  - 붕괴 줄 169: 숫자 `0` → `presets.WeakGroundCollapse()`. 칸 기본값을 -1 → 0으로 바꿔 지금 소리와 같습니다.
  - 장애물 도우미 `Obstacle`(서명 201 `df422cbcf`): 지웠습니다. 순간 표 두 줄이 같은 규칙 함수 `CombatSoundCue.EffectKeepingPreset(UnitEvent.ObsDamaged / ObsBroken, presets.Obstacle(kind))`를 바로 부릅니다. 추락과 장애물이 이 함수 하나를 씁니다("-1 = 프리셋을 바꾸지 않고 냄").
  - `CombatSoundCue.Attack` 91-92(`686900826`): `slotIndex >= 0` → `CombatSoundPresets.IsSound(slotIndex)`(같은 판정을 공통 규칙으로, 재평가 C1-N19).
- **바뀐 현강님 줄(`Playback/Support/CombatSoundPresets.cs`):** 클래스 요약 16(`686900826`) — 설정 표 원본 목록에 진입·붕괴를 더했습니다. 새 답 `WeakGroundEntry(remaining)`·`Fall()`.
- **확인 부탁:**
  - 추락 소리를 유닛만 내는 것(11-10 변경 그대로, 구조물 추락은 소리 없음)이 의도인지(설계 Q-SP2, 툴팁도 "유닛"으로 고쳤습니다).
  - 추락 칸 -1은 이제 "프리셋을 바꾸지 않고 냄"입니다(Boot `Fall` 칸에 프리셋이 없어서). 0 이상을 쓰려면 Boot `Fall` 칸에 그 프리셋을 먼저 만들어야 하고, 없으면 재생 때 AudioSystem 예외입니다.
- 사운드 작업자 안내 §3·§4.2·§6과 이펙트·소리 창구 안내 §1의 추락·약한 지반 줄도 고쳤습니다(옛 "슬롯 대기" → 나옴).
- 컴파일 미확인, 새 시험 없음. 설정 표 코드와 에셋은 한 커밋입니다(코드만 들어가면 에셋 붕괴 칸 -1이 남아 붕괴 소리가 사라짐).

## 31. 2026-11-17 플레이어 액티브 스킬 — 키 설정 열거·행동 안내 문구·행동 패널 스킬 칸 (미전달)

기록: [플레이어 스킬 View 계획](../plans/2026-11-16-combat-player-skill-view-plan.md) §6.1 표(줄 작성자)·§6.2~§6.6, 설계 [player-skill-view-design](../specs/2026-11-16-combat-player-skill-view-design.md) SK1·SK4·SK6. 천명님 Core 액티브 스킬(화약꽃·마탄)을 전투에서 쓰게 했습니다. 컴파일 미확인, 새 시험 없음, 시험 변경 없음. 사용자 Unity 확인(Ua~Ue) 문제 없음(11-17) — 조건부 문구 없앰(§6.4)도 확인됨, 그 뒤 메인을 골랐을 때만 누름(§6.6)·검토 반영(§6.7)은 Unity 확인 대기.

- **키(결정 Q-SK1):** 전투 이동 되돌리기 키가 R → Q가 되고, R은 새 액션 `Combat/Skill`입니다.
  - `GlobalSystem/InputSystem/View/IKeyBindingService.cs:14` 열거 `KeyBindingAction` 맨 끝에 `Skill`(직렬화된 정수가 바뀌지 않게 맨 끝).
  - **`GlobalSystem/UISystem/Screens/Global/KeySettingUI.cs:66` 고정 수 6은 아직 그대로입니다**(→ 11-17 프리팹 줄과 함께 7로 바꿈, 이 절 끝 덧붙임). 7로 바꾸면 Boot 씬 `GlobalUICanvas.prefab` 키 설정 화면이 6줄이라 부트의 `BindButtons`에서 예외로 모든 Play가 멈춥니다. 프리팹에 일곱째 줄(Action = Skill, Button, Text)을 넣는 것과 6 → 7을 한 커밋으로 하려고 미뤘습니다(계획 §6.2 TP-16). 그 전까지 스킬 키는 키 설정 화면에 안 보이지만, 정의 줄(호진님 `StageInputKeyBindings`)이 있어 겹침 검사는 됩니다.
  - 예전에 저장한 키 설정은 전투 그룹 줄 수가 달라져 한 번 기본값으로 돌아갑니다(경고 한 번, 멈추지 않음). 고치려면 GlobalSystem 키 설정 읽기 규칙을 바꿔야 해 그대로 두었습니다 — 바꿀지 판단 부탁드립니다.
- **행동 안내(`Combat/Input/Assist/CombatActionNotice.cs`):**
  - 계약 `ICombatActionRejectionSink`(10-14)에 `SkillRejected(PlayerSkillFailureReason)` 한 줄(:16), 구현 :43.
  - `OnResult`에 스킬 명령 갈래(:55) — 스킬 실패 이유 이름(무장 해제·기절)이 공격 문구로 가지 않게 공격 갈래보다 먼저 봅니다.
  - 스킬 문구 `ShowSkillFailure`(:116, 초안 — 지금은 스킬을 쓸 수 없음·메인이 필드에 있어야 함·스킬 없음·무장 해제·기절·횟수 다 씀·이번 턴 사용·상황이 바뀜). 점 고르기 오류는 후보로 걸러 오므로 문구가 없습니다. 문구 손질 부탁드립니다.
  - 한때 예보 동안 띄우던 조건부 문구("알려진 정보 기준 예상입니다") 입구 둘은 사용자 결정으로 걷었습니다(공용 안내 창이라 호버마다 팝업이 깜빡임, 계획 §6.4).
- **행동 패널(`Combat/Display/Hud/CombatActionPanel.cs`):**
  - 넷째 칸 `SkillSlot = 3`(:42), 선택 참조 `skillButton`(:66)·`skillIcon`·`skillUsesText`·`skillActiveIndicator`.
  - `Bind(attack, endTurn, returnToCheckpoint, skill)`(:116) — 세 입구 함께 검사를 네 입구로.
  - `BindUIService` 재정의(:136)로 공용 Hover 패널(`UITextPresentation`)을 받아, 스킬 버튼 호버 때 이름·설명 + "남은 횟수 r/l"을 보입니다(UnitInfo 특성 칸과 같은 방식, 밖에서 닫혔으면 다른 사용처 것이라 안 닫음).
  - 스킬 칸 적용 `ApplySkill`(:258) — 보임 = 패널이 보이고 메인에 스킬이 있음, 누름 = 메인을 골랐고 Core 가용성 답이 쓸 수 있음(또는 스킬 모드 끄기), 아이콘·남은/한도 글자·스킬 모드 켜짐 표시. `OnUIHidden`(:100)에서 호버를 닫습니다.
  - `skillButton`은 아직 선택 참조입니다(비어 있으면 스킬이 보일 때 경고 한 번, R 키로만). `Combat.unity` 연결이 끝나 필수 참조로 바꿀 차례입니다(계획 e.3).
- **옆에 더하기만 한 줄(현강님 줄 무변경):** 병종 순환 줄 옆의 `StageInput/StageInputNames.cs`·`CombatInputController.cs`·`IStageInputReceiver.cs`(`SkillRequested`)·`StageInputReceiverSlot.cs`·`Combat/Input/CombatInputReceiver.cs`·`CombatInputRouter.cs`(`Request.Skill`·우클릭 `SkillBack`), `Combat/Input/Interaction/CombatInteraction.cs`·`CombatSceneInput.cs`(스킬 모드·버튼 배경 조회 생성 인자·목록).
- **덧붙임(11-17, 계획 §6.8) — 키 설정 화면 스킬 줄을 넣었습니다(사용자 지시, 컴파일 미확인, Unity 확인 대기):**
  - `Prefabs/UI/GlobalUICanvas.prefab` `KeySetting` 아래에 되돌리기 줄을 복사한 `Skill` 줄(라벨 "스킬")을 `Return` 뒤에 넣고 `keyButtons` 끝에 `action: 6`(Skill)을 더했습니다.
  - 줄이 손으로 놓은 자리라 "전투 조작" 묶음을 위로 올리고 간격을 100 → 90으로 좁혔습니다(머리글 425, 전투 시작 310, 턴 종료 220, 공격 모드 130, 되돌리기 40, 스킬 -50). "아티팩트 조작" 묶음은 그대로입니다. 임시 배치이니 보기 좋게 옮겨 주세요.
  - `KeySettingUI.cs:66` 고정 수 6 → 7(+ 예외 문구). 프리팹과 한 커밋이어야 합니다. 제안: 고정 수 대신 `KeyBindingAction` 값 수와 비교하면 다음 키를 더할 때 이 줄을 안 고쳐도 됩니다(중복 검사가 이미 있어 길이가 같으면 모든 액션이 한 번씩 연결됨) — 바꿀지 판단 부탁드립니다.
