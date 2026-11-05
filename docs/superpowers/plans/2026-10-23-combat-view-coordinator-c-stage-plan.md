# 전투 View 조정자 C 단계 Task 계획 (2026-10-23)

- 설계: [c-stage-design](../specs/2026-10-23-combat-view-coordinator-c-stage-design.md) 2판. **결정: Q-C1~Q-C8·Q-T1~Q-T2 모두 A(2026-10-23 사용자), 설계에서 정한 것 P1~P8 그대로.**
- 기준 코드: HEAD `4c328d07`. 작업 트리는 문서·`CLAUDE.md`·FMOD 캐시만 바뀐 상태라 코드 겹침이 없다.
- **진행 규칙**
  - Phase 하나가 끝나면 멈추고 보고한다. 사용자가 Unity에서 컴파일·EditMode 시험·재생을 확인한 뒤 다음 Phase로 간다(CLAUDE.md).
  - **Phase 코드가 끝나면 보고 전에 새 에이전트(읽기 전용)로 그 Phase 변경을 검토하고, 지적을 반영한 뒤 보고한다**(2026-10-23 사용자 지시). 검토 결과와 반영 내용은 실행 기록에 적는다.
  - 전투 View 작업이라 **MSBuild를 돌리지 않고 새 `[Test]`를 만들지 않는다.** 보고에 "컴파일 미확인"을 적는다.
  - 시험 처리(Q-T1·Q-T2):
    - 걷은 기능이나 바뀐 동작을 보던 우리 시험(사례)은 지운다. 남는 내용을 보는 우리 시험은 호출·입력만 맞춘다.
    - 다른 사람 시험은 설계에 적은 줄만 고친다(현강님 1줄, 천명님 3줄).
    - 고친 시험은 모두 실행 기록과 공유 메모에 적는다.
  - 새 파일은 `.meta`와 함께 만든다. 옮기는 파일은 `.meta`도 같이 옮겨 GUID를 유지한다. 위치는 설계 Q-C2 A다(진행 클래스는 `Input/` 바로 아래, 포기 컨트롤러는 `Input/UiEntry/`).
  - 다른 사람 줄을 고치면 Phase C-e 공유 메모에 적는다. 연결 줄(GlobalSystem·Factory·ServiceKeys)은 바꾸지 않는다.
- 경로 약어:
  - `V/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/`
  - `C/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Combat/`
  - `T/` = `Assets/Tests/EditMode/Framework/`
  - 줄 번호는 HEAD 기준이다. 앞 Phase가 줄을 바꾸므로 각 Phase에서 다시 찾는다.

## 0. 점수 흐름(설계 §6 감점표, 누적)

| Phase 끝 | 7.5에서 | 바뀌는 것 |
|---|---|---|
| C-a (C-1·C-2) | **7.6** | 닿지 않는 포기 경로(M11) 해소, 진행 계약 분리(+), 진행 ↔ 조정자 양방향(−) |
| C-b (C-3·C-4) | **7.8** | "재생 중" 두 식(M1)·제출 정지 주인 둘(M2) 해소 |
| C-c (C-5·C-6) | **8.0** | 조용한 분리(M9) 해소 |
| C-d (C-7·C-8) | **8.1** | 넓은 서비스 계약(M6) 해소, `CaptureLost`(+), 시험 입구 `RequestReturn` 남음(−) |

설계 §6의 합 "약 8.0(7.9~8.1)"을 Phase별로 나눈 계산값이다. H1(최종 승리 화면)은 미룬다.

## Phase C-a — C-1·C-2: 죽은 포기 경로 걷기 + 진행 사실 클래스

**목표:**
- 골격이 읽는 View 쪽 진행 사실(제출 정지·완료 표시·포기 요청)과 그 전이 규칙을 새 클래스 `CombatViewProgress` 한 곳에 둔다.
- 조정자는 좁은 호스트 계약 넷으로 화면·입력 정리만 한다.
- 운영에서 닿지 않는 포기 버튼·확인창 길을 걷는다.
- 동작 동일.

| Task | 파일 | 내용 |
|---|---|---|
| Ta.1 | `V/Input/ActionButtons/ICombatAbandonDisplay.cs` (+ `.meta`) | 파일 삭제 |
| Ta.2 | `V/Display/ICombatBattleDisplay.cs` | 상속 목록에서 `ICombatAbandonDisplay`를 빼고, 머리 주석의 포기 문장(`:17-18`)을 지운다. 행동 버튼·UI 가림 문장은 남긴다 |
| Ta.3 | `V/Display/CombatSceneDisplay.cs` (현강님 줄) | `#region 포기 표시 계약 호환`(`:291-300`)을 통째로 지운다. 행동 버튼 주석(`:306`)의 "(포기 버튼과 같은 규칙)"도 지운다 |
| Ta.4 | 새 `V/Input/CombatViewProgress.cs` (+ `.meta`) | NS `…ViewSystem.Combat.Input`. 내부 계약 `ICombatProgressHost`(멤버 넷)와 `CombatViewProgress : ICombatProgressView`를 설계 C-2 스케치대로 둔다. 상태: 제출 정지·완료 표시·보인 완료·포기 요청. 규칙: `CanMarkAbandon`, `MarkAbandonRequested`(위반 시 예외), `IsShowingCompletionOf`, `BeginConnection`/`EndConnection`(끝에서는 제출 정지를 남김, P2) |
| Ta.5 | `V/Input/ActionButtons/CombatAbandonController.cs` → `V/Input/UiEntry/CombatAbandonController.cs` (+ `.meta` 함께 이동) | NS를 `…Input.UiEntry`로 바꾼다. 버튼·확인창 길(`Attach`·`Sync`·`Close`·`Stop`·`Detach`·`Open`·`Confirm`·`Cancel`·`IsConfirmOpen`·`IsRequested`·표시 참조·대리자 셋)을 지운다. 생성자는 `(faults, readFacts, canAct, cancelInputPresentation, CombatViewProgress progress)`이고, 남는 것은 `TryRequestAbandon` 하나다(정책 + `CanMarkAbandon` 확인 → 입력 표시 취소 → `MarkAbandonRequested`, 예외는 오류 경계). 머리 주석을 고친다. `TryRequestAbandon` 본문은 현강님 줄이다 |
| Ta.6 | `V/CombatViewSystem.cs` | 아래 "Ta.6 조정자" 목록 |
| Ta.7 | `C/ICombatProgressView.cs` | remarks에 "연결 사이(해제 뒤 다음 연결 전)에는 제출 정지의 마지막 값을 유지하고, 다음 연결 시작이 지운다"를 더한다(P2) |
| Ta.8 | 시험 | 아래 "Ta.8 시험" 목록 |

**Ta.6 조정자(`V/CombatViewSystem.cs`)**
- **형식·필드**
  - 구현 목록 `ICombatProgressView` → `ICombatProgressHost`. 호스트 멤버는 명시적 구현으로 둔다.
  - `IsAttached`는 공개 속성으로 남기고, 호스트 멤버가 그것을 돌려준다.
  - 필드 `_submissionsSuspended`(`:91`)·`_shownCompletion`(`:95`)을 지우고 `private readonly CombatViewProgress _progress`를 더한다.
- **생성자:** `_progress = new CombatViewProgress(this)`를 `_abandon`보다 먼저 만든다(골격 사실 읽기가 PostBind 때 `Progress`를 잡아 둠). `_abandon` 생성 인자 끝을 `_progress`로 바꾼다.
- **연결 수명**
  - `TakeDisplay`(`:495-498`)의 제출 정지·보인 완료·완료 표시 세 줄 → `_progress.BeginConnection()`. 같은 자리에 둔다(C-c에서 묶음 함수로).
  - `ContinueAttach`(`:512`)의 `_abandon.Attach(display)`를 지운다.
  - `Detach`(`:616-618`)의 완료 둘 + `_abandon.Detach()` → `_progress.EndConnection()`.
  - `StopInput`(`:663`)의 `_abandon.Stop`을 지운다.
- **진행 계약 → 호스트**
  - `SuspendSubmissions`(`:893-907`) → `ICombatProgressHost.OnSubmissionsSuspended`. 플래그 줄과 `_abandon.Sync()`는 빼고, 배치 확인창 닫기·입력 표시 취소·입력 해제·행동 버튼 숨김은 순서대로 둔다.
  - `PresentCompletion`(`:914-931`) → `CanShowCompletion`(`CanContinue ∧ !HasPendingPresentation ∧ 같은 완료`)과 `ShowCompletion`(표시를 열고, 확인 때 `CanContinue ∧ 같은 표시 ∧ confirm()`이면 배경음).
  - `Progress => _progress`로 바꾸고, `IsSubmissionSuspended`·`CompletionDisplay`·`IsAbandonRequested`(`:1099-1103`)를 지운다. `TryRequestAbandon` 위임(`:1106`)은 그대로 둔다.
- **판정**
  - `IsHoldingResult`(`:1013-1018`) → `… && _progress.IsShowingCompletionOf(_state.Completion)`.
  - 제출 정지를 읽던 곳 → `_progress.IsSubmissionSuspended`: `SyncInputLayer :866`, `CanReceiveDeckInput :1055`, `CanEnterFromDeployment :1196`, `SyncActionButtons :1188-1191`.
  - `SyncSubmissionState`(`:1070-1082`)는 `_abandon.Sync()`와 그 앞 `CanContinue` 확인을 지우고, `SuspendSubmissions()` → `_progress.SuspendSubmissions()`로 바꾼다. 함수 자체는 C-b에서 걷는다.
- **모달:** `private bool IsModalOpen => _deploymentConfirmation?.IsOpen ?? false`를 더한다. 쓰는 곳은 수신 창구 생성(`:259`), `CanReceiveDeckInput`, `SyncActionButtons`(`:1188`)다. `CanEnterFromDeployment`(`:1196`)는 `_abandon.IsConfirmOpen`만 지우고 `IsModalOpen`은 넣지 않는다. 배치 확인창이 자기를 열어 둘지 이 판단으로 묻기 때문에(`CombatDeploymentConfirmation.Sync`), 넣으면 열자마자 닫힌다(실행 중 바로잡음, 실행 기록 C-a).
- **주석:** 클래스 remarks `:48`(진행 계약·포기 버튼 상태 맞춤)과 `:53`(포기 확인창)을 고친다. `using …Input.ActionButtons`는 행동 버튼·배치 확인창 형식 때문에 남는다.

**Ta.8 시험**
- **삭제(우리):**
  - `T/CombatPanelControllerTests.cs`(+ `.meta`) 파일
  - `T/CombatViewSystemTests.cs`의 `Abandon_LiveIdle_…`(`:920`)·`Abandon_Cancel_…`(`:942`)·`Abandon_ExitStarted_…`(`:984`, 사용자 결정으로 C-b에서 호출만 맞춰 되살림)
  - 가짜 표시의 포기 멤버(`:1679-1702`)
- **일부 삭제(우리):** `ActionButtons_HiddenBySuspendAndFault_AndClickIgnoredWhileAbandonConfirmOpen`(`:1073`)에서 `:1098-1101`(확인창 열기·버튼 누름·단언)만 지우고, 이름을 `ActionButtons_HiddenBySuspendAndFault`로 바꾼다. 연결·Tick(`:1096-1097`)은 남긴다.
- **호출 맞춤(우리):**
  - `Abandon_RequestIsClearedByDetachAndNextAttach`(`:969`): `AbandonOpen()`·`AbandonConfirm()` → `_rig.View.TryRequestAbandon()`.
  - `ActionButtons_…ClearedAtTickAfterAbandonOpen`(`:1056`): `AbandonOpen()` → `TryRequestAbandon()`, `AbandonConfirmShown` 단언(`:1067`) 삭제.
- **`.Progress` 맞춤:** `_rig.View.`(또는 `rig.View.`)의 `SuspendSubmissions`·`PresentCompletion`·`CompletionDisplay`·`IsSubmissionSuspended`·`IsAbandonRequested` → `….View.Progress.…`. 줄은 454·455·477·478·483·493·494·495·501·502·504·505·506·509·514·517·518·648·665·794·963·975·978·980·1084, 모두 25줄이다. 963은 현강님 줄(Q-T2)이고, 나머지는 우리 줄이다.
- **조정자를 진행 계약 자리에 넘기는 우리 줄:** `T/CombatMovementViewTests.cs:46·186·248`(천명님 파일 안). `CombatProgressTestDriver(…, r.View)` → `r.View.Progress`.
- **그대로:** 가짜 진행 계약(`TestProgressView`·`FakeView`·`PresentationConsumer`·호진님 `CombatViewStub`), `CombatDisplayReuseTests`(`AbandonPanel_…` 포함), 조정자 `IsAttached`를 읽는 23곳.

**Phase C-a 재생 확인(사용자)**
- Option → 포기 → 결과 화면 → 확인 → 로비
- 일반 승리 → 결과 → 확인 → StageSelect, 패배 → 로비. 확인 뒤 배경음이 바뀌는지
- 배치 단계에서 미배치 유닛이 있을 때 전투 시작 확인창 열기·취소, 그 동안 손패·가방 입력 막힘
- 손패·가방 드래그, 행동 버튼(공격·턴 종료·복귀)

## Phase C-b — C-3·C-4: 판정 하나로

**목표:**
- "확인하지 않은 결과(P < S)"를 규칙 함수 하나로 모은다. View도 골격과 같은 식을 쓴다.
- 제출 정지 깃발은 골격만 세우고, View는 종료 결과를 보면 입력만 바로 놓는다.
- 끝 = 완료 하나.
- 화면·입력 동작은 동일하고, 깃발이 서는 시점만 바뀐다.

| Task | 파일 | 내용 |
|---|---|---|
| Tb.1 | `C/CombatProgressFacts.cs` | `internal static bool IsUnpresented(int presented, int latest)`를 더하고, `HasUnpresentedResult`(`:99`)가 그것을 부른다 |
| Tb.2 | `C/Plans/CombatPlan.cs` | `IsAwaitingPlayerCommand`(`:152-154`)가 `!CombatProgressFacts.IsUnpresented(…)`를 쓴다. `Decide` 1행 주석(`:94-95`)을 "제출 정지 깃발의 유일한 주인. View는 종료 결과를 보면 입력만 놓는다(C 단계 C-4)"로 고친다 |
| Tb.3 | `V/Input/CombatInputPolicy.cs` | `IsTerminal` = `facts.HasCompletion`. 주석(`:21-26`)에 근거를 적는다: Finished와 완료는 한 `Apply` 거래(`CombatCommandTransition.cs:88`, `BattleTransactions.cs:219·228·230`), 포기는 완료만 남김 |
| Tb.4 | `V/CombatViewSystem.cs` | 아래 "Tb.4 조정자" 목록 |
| Tb.5 | (기록) | P5 대조: 끝을 정책으로 막는 입구(`PassesCommon`·`GetDesiredLayer`·`CanEditPath`·`CanRequestAbandon`·`CanInspectUnit`·배치 Drag `CanPreview`)와, 정책 밖에서 표시만 하는 입구(손패 호버·유닛 정보)를 코드로 다시 확인해 실행 기록에 남긴다 |
| Tb.6 | 시험 | 아래 "Tb.6 시험" 목록 |

**Tb.4 조정자(`V/CombatViewSystem.cs`)**
- **`IsPresenting(bool hasBattle)`(`:1021-1024`)** → 속성 `IsPresenting => CombatProgressFacts.IsUnpresented(P, S)`. 부르는 셋을 맞춘다: `ReadFacts`의 `isPresenting:`(`:988`), `RouteInput`(`:1151`), `HasPendingPresentation`(`:1095`).
- **`TickCore`의 새 결과 블록(`:335-341`)** → `signal.UnpresentedResult != null && !_playback.IsBusy && CombatInputPolicy.IsTerminal(ReadFacts())`이면 `SyncInputLayer()` 뒤 `CanContinue` 확인. 주석은 설계 C-4 diff대로 쓴다.
- **`ShowBattle`(`:531-533`):** `SyncSubmissionState()`와 뒤의 확인을 지운다. 끝난 전투면 바로 아래 `SyncInputLayer`가 목표 Layer 없음으로 입력을 잇지 않는다.
- **`SyncSubmissionState`(`:1066-1082`):** 주석까지 함수째 지운다.
- **주석:** `Tick` 요약(`:274-279`, "새 결과면 제출 상태 맞춤" → "종료 결과면 입력 해제"), 클래스 remarks `:48`, `SyncInputLayer` 요약의 "종료"가 완료를 뜻하는지 확인한다.

**Tb.6 시험(모두 우리 줄)**
- `T/CombatInputPolicyTests.cs`:
  - `IsTerminal_UsesFinishedOrCompletionOnly`의 `[TestCase(CombatFlowPhase.Finished, false, true)]`(`:16`)를 지우고, 이름을 `IsTerminal_UsesCompletionOnly`로 바꾼다.
  - `IsTerminal_IgnoresHasBattle_…`(`:30`): `Facts(hasBattle: true, phase: CombatFlowPhase.Finished)` → `Facts(hasBattle: true, hasCompletion: true)`.
  - `GetDesiredLayer_TerminalWinsOverStagePhase`의 `[TestCase(CombatFlowPhase.Finished, false)]`(`:59`)를 지운다.
  - `CanRequestAbandon_…`의 `[TestCase("finished", true, false)]`(`:233`)를 지운다.
- `T/CombatViewSystemTests.cs`:
  - `FirstDisplay_OfFinishedBattle_DoesNotBindInput`(`:278`): 준비에 완료를 넣는다(`_rig.State.Completion = …`, 같은 파일 `:471-474`의 완료 꼴). `Phase = Finished`는 그대로 둔다.
  - `Finished_ReleasesInputOnly_AndStillAcknowledgesTheLastDelayedResultOnce`(`:615`): 결과 게시 전에 같은 방식으로 완료를 넣는다. 골격 역할 호출은 넣지 않는다.
- **그대로:** 641·659·902, Policy 82·145, `CombatInteractionTests.Terminal_BlocksEverything_…`(`:289`)(국면 검사로 막힘).

**Phase C-b 재생 확인(사용자)**
- 일반 승리·패배·보스 승리·포기 각각에서, 마지막 연출이 도는 동안 입력이 막히고 행동 버튼·전투 전환 버튼·가방이 지금과 같은 때에 숨는지
- 마지막 연출이 끝나면 결과 화면 → 확인 → 이동
- 튜토리얼 전투가 있으면 그 결과까지

## Phase C-c — C-5·C-6: 조용한 분리 F1 + 수명 정리

**목표:** 결과 유지가 아닌데 전투가 사라지거나 바뀌면 F1로 드러낸다. 씬 수명·연결 수명의 코드를 이름 있는 묶음으로 모은다. 동작은 계약 위반 때만 바뀐다.

| Task | 파일 | 내용 |
|---|---|---|
| Tc.1 | `V/CombatViewSystem.cs` `TickCore`(`:303-310`) | 설계 C-5 diff대로: 원인을 `_faults.Report`로 먼저 보고(다른 전투 / 완료 전 정리 두 문구)한 뒤, 지금처럼 비우기 → `Detach`. 주석 `:303`은 호진님 줄이고, 다시 쓴다 |
| Tc.2 | 〃 | `#region 씬 수명(−195 ~ ReleaseScene)`을 만들고 `OnCombatSceneLoaded`(`:706`)·`OnCombatSceneEntered`(`:726`)·`ReleaseScene`(`:634`)·`ReleaseBackdrop`(`:642`)을 모은다. 본문은 바꾸지 않고 옮기기만 한다(diff가 커 보임) |
| Tc.3 | 〃 | `#region 연결 수명` 안에 `BeginConnectionState()`·`EndConnectionState()`를 둔다(설계 C-6 순서). `TakeDisplay`는 `_inputWrite.Clear()` → `_presentation.Attach` → `_playback.Attach()` → `BeginConnectionState()` → `_display = display`. `Detach`는 `_display = null` → `_interaction`·`_gate`·`_router` 비움 → `EndConnectionState()` → 연출 연결 끊기 → 표시 반환(실행 중 바꾸고 사용자가 유지 결정, 실행 기록 C-c). `EndConnectionState` 주석에 남기는 둘(전투 번호, 제출 정지 P2)을 적는다 |
| Tc.4 | 〃 | `_sceneRoot` → `_subscribedRoot`: `:86`(선언·주석)·`:440`·`:593-599`·`:674-675`·`:1188-1189`. `:87-88` 주석의 "_sceneRoot(구독)"도 고친다. 대부분 현강님 줄이다 |
| Tc.5 | 시험 | 바꿀 곳 없음. 실행 확인 대상: 우리 `BattleRemoved_DetachesAndReleasesInput`(`:291`)·`BattleRemoved_ClearsDisplay_AndDetachReleasesItOnce`(`:1308`), 천명님 `CombatMovementViewTests.BattleEndBeforePointerCallback_…`(`:365`). 모두 F1 뒤에도 단언이 같다 |

**Phase C-c 재생 확인(사용자)**
- Stage ↔ Combat 진입·이탈을 두 번 이상: 배경 맵·맵 카메라, 다시 진입한 전투의 입력·버튼
- 결과 화면을 띄운 채 기다렸다가 확인(결과 유지)
- 재생 중 Play 종료(앱 종료 정리), 콘솔에 F1이 없는지

## Phase C-d — C-7·C-8: 서비스 계약 좁히기 + 환경 호버 정책

**목표:** `ICombatViewService`에서 바깥 호출이 없는 멤버 다섯을 뺀다. 환경 호버의 사실 조건을 정책으로 옮긴다. 동작 동일.

| Task | 파일 | 내용 |
|---|---|---|
| Td.1 | `V/ICombatViewService.cs` | `Attach`·`Detach`·`FocusLost`·`CaptureLost`·`RequestReturn`(`:23-27`, 천명님 줄)을 지운다. 남는 `using`(`Input.UiEntry`·`LifeCycles.Combat`)은 남은 멤버가 그대로 쓴다 |
| Td.2 | `V/CombatViewSystem.cs` | `Attach`(`:379`)·`Detach`(`:587`)·`FocusLost`·`RequestReturn`(`:1119·1121`)을 `public` → `internal`로 바꾼다. `RequestReturn`에는 "시험 입구, 운영 복귀는 행동 버튼 → 수신 창구" 주석을 단다. `CaptureLost`(`:1120`, 천명님 줄)를 지우고, 주석 `:1117-1118`을 고친다 |
| Td.3 | `V/Input/CombatInputPolicy.cs` | `CanShowEnvironmentHover(in CombatInputFacts)`를 더한다(사실 조건만, remarks에 "수신 가능·포인터 대상은 호출자가 확인") |
| Td.4 | `V/CombatViewSystem.cs` `SyncEnvironmentHover`(`:1169-1171`, 현강님 줄) | `CanReceiveDeckInput() && CombatInputPolicy.CanShowEnvironmentHover(facts) && ResolvePointerTarget(…) != None` |
| Td.5 | 시험 | 아래 "Td.5 시험" 목록 |

**Td.5 시험**
- **우리:** `CombatViewSystemTests.FocusOrCaptureCleanupException_UsesSharedBoundary_AndStillUnbinds`(`:718-735`). `[TestCase(true)]`·매개변수·`capture` 갈래를 지우고 `[Test] FocusCleanupException_UsesSharedBoundary_AndStillUnbinds()`로 한다. 본문 단언은 그대로다.
- **천명님(Q-T2):**
  - `CombatMovementViewTests.cs:394` `group.Combat.Attach(null)` → `owner.Attach(null)`.
  - `CombatMovementViewTests.FocusOrCaptureLoss_CancelsFrozenDraft`(`:347-356`): `[TestCase(false)]`·매개변수·`CaptureLost` 갈래를 지우고, `[Test]`·`r.View.FocusLost()`로 한다. 이름은 그대로 둔다(다른 사람 시험이라 최소로).
  - `CoreStageCombatCycleTests.cs:355` `view.Combat.RequestReturn()` → `((CombatViewSystem)view.Combat).RequestReturn()`.
- **그대로:** 가짜 진행 계약·`CombatViewStub`(빠진 멤버가 공개 메서드로 남아도 컴파일됨), 천명님 `CombatMovementViewTests` 152·227·484(`r.View.RequestReturn()`, 구체 형식이라 내부 입구로 그대로 됨).

**Phase C-d 재생 확인(사용자)**
- 이동 경로 Drag 중 창 전환(포커스 잃음) → 경로 취소
- 복귀 버튼 → 복귀 견적·복귀
- 환경 칸 호버 문구가 지금처럼 나오고, 연출 중·결과 뒤에는 나오지 않는지

## Phase C-e — 문서·공유 (모두 미전달로 작성, 코드 변경 없음)

| 받는 사람·문서 | 위치 | 내용 |
|---|---|---|
| 현강님 | `docs/superpowers/handoffs/2026-10-22-combat-view-structure-hyeongang-share.md`에 §6 새 절 | 설계 §7 현강님 항목: 포기 표시 계약·빈 메서드 삭제, `TryRequestAbandon` 본문·컨트롤러 이동, `CombatViewSystem` 현강님 줄 목록, `PackUI` 조건 알림, 시험 `:963` |
| 천명님 | `docs/superpowers/handoffs/2026-09-24-view-core-data-boundary-handoff.md`에 §31 새 절(요청 없음) | 설계 §7 천명님 항목: 계약 멤버 다섯·`CaptureLost`, 필드 이름 `_sceneRoot` → `_subscribedRoot`(HEAD `:86`), 시험 줄 셋, 기대는 Core 동작(Finished와 완료는 한 거래), `BattleEndBeforePointerCallback_…` 장면 |
| 호진님 | `docs/superpowers/handoffs/2026-10-22-combat-view-structure-hojin-share.md`에 §8 새 절 | `CombatViewSystem.cs:303` 주석을 다시 씀(결과 유지 블록 동작은 그대로), `:912` `PresentCompletion` 문서 주석을 진행 클래스·호스트로 옮김 |
| 민형님 | — | 없음(연결 줄 변경 없음, P7) |
| 배치 안내 | `docs/superpowers/viewDocs/combat-view-folder-guide.md` | `Input/`(바로 아래) 예에 `CombatViewProgress`를 넣고, `UiEntry/` 예에 `CombatAbandonController`를 넣고, `ActionButtons/` 예에서 `CombatAbandonController`를 뺀다 |
| 정리 문서 | `docs/superpowers/specs/2026-10-23-combat-view-coordinator-inventory.md` 머리말 | "C 단계 뒤 진행 사실 원본은 `CombatViewProgress`" 한 줄 |
| 설계·CLAUDE.md | 설계 상태 줄, CLAUDE.md 현재 상태 | C 단계 완료·남은 것 |

## 계획에서 정한 것 (사용자 확인 대상)

- **TP1** 포기 컨트롤러 이동(Q-C2 A)은 내용 변경과 같은 Phase C-a에서 한다. 파일 이동과 내용 변경이 한 diff에 섞인다.
- **TP2** C-b의 두 시험(278·615)에 완료를 넣을 때, 같은 파일 `:471-474`의 완료 생성 꼴을 그 시험 안에 그대로 쓴다. 공용 도우미는 새로 만들지 않는다.
- **TP3** 우리 시험 `:718`은 매개변수를 없애고 이름을 고친다. 천명님 시험 `:347`은 사례만 줄이고 이름은 그대로 둔다.
- **TP4** C-c의 씬 수명 region은 메서드 본문을 바꾸지 않고 옮기기만 한다. diff가 커 보이지만 동작은 바뀌지 않는다.
- **TP5** `V/Display/Hud/CombatAbandonPanel.cs:13` 주석에 나오는 `CombatAbandonController` 이름(평문, cref 아님)은 고치지 않는다. 보고만 하는 파일이다(설계 §8).
- **TP6** C-a에서 `SyncSubmissionState`는 남긴다(진행 클래스의 정지를 부름). C-b에서 함수째 걷는다. 그래서 C-a가 끝난 시점의 동작은 지금과 같다.

## 커밋 묶음(사용자가 함)

- Phase마다 한 묶음을 권장한다.
- **새 파일**(`.meta`와 함께): `V/Input/CombatViewProgress.cs`(C-a).
- **옮긴 파일**(`.meta` 함께, GUID 유지): `V/Input/ActionButtons/CombatAbandonController.cs` → `V/Input/UiEntry/`(C-a).
- **지운 파일**(`.meta`와 함께): `V/Input/ActionButtons/ICombatAbandonDisplay.cs`, `T/CombatPanelControllerTests.cs`(C-a).
- `Assets/Plugins/FMOD/Cache/Editor/FMODStudioCache.asset` 변경은 이번 범위가 아니다.

## 실행 기록

(Phase마다 여기에 적는다: 바꾼 파일, 계획과 다른 점, 고친·지운 시험, 다른 사람 줄, 검토 결과, 컴파일 미확인.)

### Phase C-a (C-1·C-2) — 2026-10-23, 코드 완료, **컴파일 미확인**(MSBuild 안 돌림, 새 `[Test]` 없음)

**바꾼 파일(코드)**
- 삭제: `V/Input/ActionButtons/ICombatAbandonDisplay.cs`(+ `.meta`).
- 새 파일: `V/Input/CombatViewProgress.cs`(+ `.meta`, GUID `1669593f…`). 안에 `ICombatProgressHost`와 `CombatViewProgress`가 있다.
- 옮김: `V/Input/ActionButtons/CombatAbandonController.cs` → `V/Input/UiEntry/`(+ `.meta`, GUID `3097b7d6…` 유지). 상태 없는 포기 입구로 다시 썼다.
- `V/CombatViewSystem.cs`:
  - `ICombatProgressView` 대신 `ICombatProgressHost`를 명시적으로 구현한다(`IsAttached`·`OnSubmissionsSuspended`·`CanShowCompletion`·`ShowCompletion`).
  - 진행 사실 필드 둘을 지우고 `_progress`를 둔다(생성자에서 `_abandon`보다 먼저 만듦).
  - 연결 시작·끝은 `BeginConnection`/`EndConnection`으로 바꿨다.
  - `IsModalOpen`을 더하고, `IsHoldingResult`를 진행 클래스에 묻게 했다.
  - 클래스·메서드 주석을 고쳤다.
- `V/Display/ICombatBattleDisplay.cs`: 상속 목록·주석을 고쳤다.
- `V/Display/CombatSceneDisplay.cs`: 포기 region과 주석 두 곳을 고쳤다.
- `C/ICombatProgressView.cs`: remarks에 P2를 더했다.
- 주석만 바꿈(검토 L1): `V/Input/CombatInputReceiver.cs`, `V/Input/CombatInputPolicy.cs`, `V/Input/Interaction/CombatInteraction.cs`.

**바꾼 파일(시험)**
- `T/CombatPanelControllerTests.cs`(+ `.meta`): 파일 삭제.
- `T/CombatViewSystemTests.cs`:
  - 삭제: 920·942·984(984는 C-b에서 사용자 결정으로 되살림). 1073은 확인창 부분만 지우고 이름을 `ActionButtons_HiddenBySuspendAndFault`로 바꿨다. 가짜 표시의 포기 멤버도 지웠다.
  - 호출 맞춤: 969·1056(이름 `…ClearedAtTickAfterAbandonRequest`).
  - `.Progress` 25줄.
- `T/CombatMovementViewTests.cs`: 우리 줄 3곳(46·186·248)을 `.Progress`로 맞췄다.

**계획과 다른 점**
1. **`CanEnterFromDeployment`에 `IsModalOpen`을 넣지 않았다.** 배치 확인창 `Sync`가 이 판단으로 자기를 닫기 때문이다. 설계 C-1과는 같고, 틀린 것은 계획 Ta.6 줄이다(고침).
2. **시험 1056의 이름을 바꿨다**(`AbandonOpen` → `AbandonRequest`). 걷은 멤버 이름이 들어 있었다.
3. **검토 뒤 고침:** `ICombatProgressHost.IsAttached`도 명시적으로 구현해 계획·주석과 맞췄다. 걷은 확인창을 말하던 주석(조정자 4곳, 수신 창구 2, 정책 2, 입력 기능 1, 씬 표시 1)을 고쳤다.
4. **사이 문제:** Git Bash `sed -i`가 `CombatViewSystemTests.cs`의 줄 끝을 LF로 바꿨다. CRLF로 되돌렸다(diff는 변경 줄만).

**다른 사람 줄**(blame, 중괄호 제외)
- 현강님:
  - `CombatSceneDisplay` 291·293-298
  - `CombatAbandonController` 152-179 중 현강님 줄(중괄호 빼고 13줄: 지운 `Confirm()` 몸통과 `TryRequestAbandon` 본문)
  - `CombatViewSystem` HEAD 259·898·929·1054-1055·1188-1189·1196
  - 시험 `CombatViewSystemTests` HEAD 963(지금 931) 한 줄(`.Progress`)
- 호진님: `CombatViewSystem` HEAD 912 `PresentCompletion` 문서 주석을 진행 클래스·호스트로 옮겼다.
- 설계 §7·계획 C-e에 반영했다.

**검토(새 에이전트, 읽기 전용) — 높음·중간 없음**
- **확인됨**
  - 남은 참조 0, 시험 가짜 구현, 네임스페이스, GUID 유일·유지, 삭제 GUID 참조 0
  - HEAD 대비 동작 동일: 제출 정지 순서, 완료 표시 조건 넷과 배경음, 결과 유지 식, 포기 요청 초기화 시점, `StopInput`, `TryRequestAbandon` 순서
  - 계획 Ta.8 목록대로 시험 처리
- **반영:** 명시적 `IsAttached`, 주석(L1), 설계 §7 줄 주인 셋(`CombatSceneDisplay:306`은 우리 줄, 컨트롤러 줄 설명, 호진님 `:912`), 계획 모달 줄.
- **반영 안 함(보고)**
  - **L2(사용자 결정):** "이탈 시작 뒤 포기 요청 거절"(`CanMarkAbandon`의 제출 정지 조건)을 View 단계에서 보는 시험이 이제 없다. 지운 984가 버튼 경로로 보던 규칙이다. 정책 단계 사례(`CombatInputPolicyTests`의 `exitIdle` 거짓)는 남아 있다. → **사용자 결정(10-23): 984를 호출만 맞춰 되살림**(C-b 기록).
  - **L3:** 969·1056의 `TryRequestAbandon()` 반환값은 단언하지 않는다(뒤 단언이 같은 실패를 잡음, 호출만 맞춤 원칙).
  - **L4(기존):** `ActionButtons_HiddenBySuspendAndFault` 끝의 "오류 정지는 숨긴다" 단언은 배치 Layer라 처음부터 숨어 있어 늘 통과한다. HEAD도 같다.
  - `CombatInputReceiverTests.AbandonConfirmOpen_PassesOnlyPresentationCleanup`의 이름에 "Abandon"이 남는다. 보는 내용(모달이면 정리 신호만 통과)은 배치 확인창에도 맞아 그대로 둔다.

**사용자 확인(10-23): Unity 확인 문제 없음.**

### Phase C-b (C-3·C-4) — 2026-10-23, 코드 완료, **컴파일 미확인**(MSBuild 안 돌림, 새 `[Test]` 없음)

**바꾼 파일(코드)**
- `C/CombatProgressFacts.cs`: 규칙 원본 `internal static bool IsUnpresented(int, int)`를 더했다. `HasUnpresentedResult`가 그것을 부른다.
- `C/Plans/CombatPlan.cs`:
  - `IsAwaitingPlayerCommand`가 `!IsUnpresented(…)`를 쓴다(`>=`와 같음).
  - `Decide` 1행 주석: "제출 정지는 Plan만 세운다(이 줄과 포기 블록), View는 입력만 놓는다".
- `V/Input/CombatInputPolicy.cs`: `IsTerminal` = `facts.HasCompletion`. remarks에 Core 근거를 적었다(Finished ⇔ 끝남, 끝난 명령은 완료와 한 거래, 포기는 완료만).
- `V/CombatViewSystem.cs`:
  - `IsPresenting(bool)` → 속성 `IsPresenting => CombatProgressFacts.IsUnpresented(P, S)`. 부르는 셋(`ReadFacts`·`RouteInput`·`HasPendingPresentation`)을 맞췄다.
  - `TickCore`: 새 결과가 종료면 `SyncInputLayer()`(입력 해제만).
  - `ShowBattle`의 `SyncSubmissionState()` 호출과 그 함수를 지웠다.
  - 주석: `Tick` 요약, 클래스 remarks, 새 결과 블록.

**바꾼 파일(시험, 모두 우리 줄)**
- `T/CombatInputPolicyTests.cs`:
  - `IsTerminal`의 Finished 사례를 지우고 이름을 `IsTerminal_UsesCompletionOnly`로 바꿨다.
  - `IsTerminal_IgnoresHasBattle_…`는 입력을 맞췄다(`hasCompletion: true`).
  - `GetDesiredLayer_TerminalWinsOverStagePhase`와 `CanRequestAbandon_…`의 Finished 사례를 지웠다. 그 사례를 지워 쓰지 않게 된 `scenario == "finished"` 갈래(`:238`)도 지웠다(검토 L4).
- `T/CombatViewSystemTests.cs`:
  - `FirstDisplay_OfFinishedBattle_DoesNotBindInput`·`Finished_ReleasesInputOnly_…`의 준비에 완료를 넣었다(같은 파일 완료 생성 꼴, TP2).
  - **사용자 결정으로 되살림:** `Abandon_ExitStarted_RejectsRequest`. 옛 984를 호출만 맞췄다: 제출 정지 → `TryRequestAbandon()` false, 요청 없음.

**Tb.5 P5 대조(코드 확인)**
- 끝(완료)을 정책으로 막는 입구:
  - 명령·미리보기 공통 문 `PassesCommon`(`CanSubmit`·`CanPreview`), `GetDesiredLayer`, `CanEditPath`(`CanSubmit` 경유), `CanRequestAbandon`
  - 유닛 범위 호버 `CanInspectUnit`(`CanPreview` 경유)
  - 배치·공격·아이템 Drag(`CanPreview`·`CanSubmit`)
  - 행동 버튼 `CombatActionButtons.Read`(입력 없음이면 기본값, 아니면 `CanSubmit`)
  - `BattleChangerUI:53`·`PackUI:91`(`!HasCompletion`)
- 정책 밖에서 표시만 하는 입구: 손패 호버(`CombatDeckHoverInput.Hover`, 오류·해제만 봄), 유닛 정보(`RouteInput`, `!IsPresenting`만 봄). 둘 다 지금과 같다.

**다른 사람 줄**
- 현강님: `CombatViewSystem` HEAD 1151(`RouteInput` 유닛 정보 조건 `!IsPresenting(_state.HasBattle)` → `!IsPresenting`, `80eb7481`). 검토에서 찾아 설계 §7에 더했다.
- 천명님 줄은 지운 함수의 `}`뿐이다.

**검토(새 에이전트, 읽기 전용) — 높음 0·중간 1·낮음 5**
- **확인됨**
  - 컴파일: 지운 멤버 참조 0, `IsUnpresented` 접근성과 `using`, 시험 형식 이름
  - 동작 (a)~(g):
    - 불변식 "재생 중이면 P < S"
    - 종료 결과를 본 같은 View Tick의 입력 해제, Tick 끝 버튼 숨김·배치 확인창 닫기
    - 깃발이 늦는 동안 읽는 곳 모두 같은 결과, 이미 풀린 입력이 다시 이어지는 길 없음
    - 끝난 전투의 첫 그리기 입력 없음
    - 종료 아닌 결과의 Layer 그대로
    - 정리 뒤 P = S, 튜토리얼 판정 같음
  - 시험: Tb.6 목록대로, 실행에서 새로 깨질 시험 없음(Boot smoke 시험은 WaitFor라 영향 없음)
- **반영**
  - M1: 현강님 줄을 설계 §7과 이 기록에 넣었다.
  - L1: Plan 1행 주석을 "Plan만"(포기 블록 포함)으로 고쳤다.
  - L2: 조정자 주석 두 곳을 고쳤다(종료를 보는 자리 셋, 결과 없이 생기는 포기 완료).
  - L4: 죽은 갈래를 지웠다.
  - L5: 984 되살림을 설계 C-1 표·C-a 기록에 반영했다.
- **반영 안 함(보고)**
  - **L3:** `Finished_DirectInputBeforeNextTick_RecordsNothing`·`CombatInteractionTests.Terminal_BlocksEverything_…`·정책 시험의 "종료" 메시지·경로 "finished" 사례는 통과한다. 다만 지금 막는 것은 끝 판정이 아니라 PlayerInput 국면 검사다. 설계 표에서 "그대로"로 정한 우리 시험이다.
  - **L4 나머지:** `IsTerminal_IgnoresHasBattle`에 "전투 없음 + 완료" 사례가 없다(HEAD도 같은 꼴, 새 사례는 만들지 않음).
  - **관찰:**
    - 입력 해제 중 슬롯 `SelectCanceled` 경로의 버튼 맞춤은 깃발 전이라 잠깐 "보임·누름 불가"로 써지고, 같은 Tick 끝에서 숨는다.
    - 정상 종료에서도 `OnSubmissionsSuspended`가 마지막 재생 중에 한 번 돈다(HEAD 포기 경로와 같은 길).
    - 그 자리에서 끝나는 결과면 Settle이 Job 한 주기 늦어진다(깃발 시점 차이, 설계 허용).

**사용자 확인(10-23): Unity 확인 문제 없음.**

### Phase C-c (C-5·C-6) — 2026-10-23, 코드 완료, **컴파일 미확인**(MSBuild 안 돌림, 새 `[Test]` 없음)

**바꾼 파일:** `V/CombatViewSystem.cs`만. 시험은 바꾸지 않았다.
- **Tc.1(C-5):** `TickCore`에서 결과 유지가 아닌데 전투가 사라지거나 다른 전투로 바뀌면, 먼저 `_faults.Report`로 원인을 보고한다(문구 둘: 다른 전투 설치 / 완료 전 정리). 그 뒤 지금처럼 비우고 `Detach`. 주석을 다시 썼다(호진님 `:303`).
- **Tc.2:** 새 `#region 씬 수명(씬 로드 -195 ~ 씬 이탈 ReleaseScene)`에 `OnCombatSceneLoaded`·`OnCombatSceneEntered`·`ReleaseScene`·`ReleaseBackdrop`을 옮겼다. 본문과 주석은 글자 그대로다(검토가 HEAD와 diff로 확인). 옛 `#region 씬 연결`은 `#region 연결 수명(표시 연결 TakeDisplay ~ 해제 Detach)`이 됐다.
- **Tc.3:** `BeginConnectionState()`·`EndConnectionState()`를 더했다. `TakeDisplay`는 HEAD 순서 그대로이고 `_display = display`가 여전히 마지막 문장이다.
- **Tc.4:** `_sceneRoot` → `_subscribedRoot`(선언 위 주석 추가, 현강님·천명님 줄 포함).
- **검토 뒤 주석:** `_subscribedRoot` 주석(Pack은 입력이 아니라 표시만), `Detach`의 순서 이유 주석, `ReleaseScene` 요약 "씬 연결 수명의 끝" → "씬 수명의 끝". 예외 문구 "Combat 씬 연결 수명 정리"는 본문이라 TP4대로 두었다.

**계획과 다른 점(사용자 확인 필요)**
- **`Detach`의 순서.** 계획 Tc.3·설계 C-6은 `_display = null → EndConnectionState() → _interaction·_gate·_router = null`이라고 적었다. 실제로는 `_display = null → 참조 셋 null → EndConnectionState()`로 했다.
  - 까닭: `EndConnectionState`의 입력 Buffer 비우기(외부 쓰기)가 지금(HEAD)처럼 단순 대입 뒤 마지막에 오게 하려는 것이다. 계획 순서대로면 그것이 대입 셋 앞으로 옮겨진다.
  - 검토 확인: 순서가 바뀐 것은 콜백 없는 필드 대입뿐이고, 실패할 수 있는 호출의 앞뒤는 그대로라 동작이 같다.
  - 구현 전에 물었어야 했다. → **사용자 결정(10-23): 이 순서 그대로 유지.** 설계 C-6·계획 Tc.3의 글을 실제 순서로 고쳤다.

**사용자 확인(10-23): Unity 확인 문제 없음.**

**다른 사람 줄**
- 호진님: `:303` 주석(`cb76bd2c`).
- 현강님: `_sceneRoot` 이름이 바뀐 줄 `:595-596`·`:674-675`·`:1188-1189`.
- 천명님: 필드 선언 `:86`(`e0b02cc7`). 검토 M2에서 찾아 설계 §7·C-e 목록에 더했다.

**검토(새 에이전트, 읽기 전용) — 높음 0·중간 2·낮음 4**
- **확인됨**
  - 컴파일: `_sceneRoot` 남은 참조 0, 반사 이름 그대로, region 짝
  - 옮긴 넷은 HEAD와 글자 그대로
  - `TakeDisplay` 불변식 유지, 재진입 없음
- **F1 오탐 없음:** 정상 흐름에서 이 분기에 닿는 경로를 찾지 못했다.
  - Settle 연쇄는 한 Runner Tick에 끝난다(같은 우선순위 FIFO, Runner가 View보다 먼저).
  - 포기는 `HasBattle`을 지우지 않는다.
  - 챕터 완료는 전투·완료를 바꾸지 않는다.
  - 새 Run은 Stage·Lobby·Bootstrap에서만 만든다.
  - 튜토리얼은 전투를 끝내거나 다시 준비하지 않는다.
  - Exit −100은 먼저 표시를 놓는다.
  - 결과 확인 뒤에는 결과 유지가 먼저 돈다.
  - 추측으로 남는 것: Runner 예산 32가 연쇄 사이에서 끊기는 경우.
- **보고 → 정리 순서:** 보고 → 정지 → 비우기 → `Detach`. 정리를 두 번 하는 것은 기존 F1 경로와 같다.
- **시험:** 실행에서 깨질 시험이 없다.
  - 우리 둘·천명님 하나는 단언이 F1 뒤에도 성립한다(로그는 목록이나 빈 함수).
  - 표시를 연결한 채 전투를 끝내거나 바꾸는 다른 시험은 없다.
  - 로그가 Unity 실패로 이어지는 rig는 이 분기에 닿지 않는다.
- **반영:** M2 천명님 줄, L2·L3·L4 주석.
- **반영 안 함(보고):** M1 순서 차이는 위 "사용자 확인 필요"로 뒀다(→ 사용자가 유지 결정).
- **L1:** F1 보고 뒤 비우기나 `Detach`가 던지면, 그 실패는 이미 오류 상태라 기록되지 않는다(HEAD는 첫 오류로 남겼음). Phase 6 `OnCombatSceneDisabled`와 같은 꼴이라 받아들인다.

### Phase C-d (C-7·C-8) — 2026-10-23, 코드 완료, **컴파일 미확인**(MSBuild 안 돌림, 새 `[Test]` 없음)

**바꾼 파일(코드)**
- `V/ICombatViewService.cs`: `Attach`·`Detach`·`FocusLost`·`CaptureLost`·`RequestReturn`을 계약에서 지웠다(천명님 줄 `:23-27`). 계약 요약 주석을 더했다: 바깥에서 쓰는 것만 둔다.
- `V/CombatViewSystem.cs`:
  - `Attach`·`Detach`(천명님 줄 `:379`·`:587`)와 `FocusLost`·`RequestReturn`을 `public` → `internal`로 바꿨다.
  - `CaptureLost`(천명님 줄 `:1120`)를 지웠다.
  - `RequestReturn`에 "시험 입구" 주석을 달았다. 운영 복귀는 복귀 키(슬롯)·복귀 버튼이 수신 창구로 바로 들어간다.
  - `SyncEnvironmentHover`(현강님 줄 `:1169-1171`)는 `CanReceiveDeckInput() && CombatInputPolicy.CanShowEnvironmentHover(facts) && ResolvePointerTarget(…) != None`이다.
- `V/Input/CombatInputPolicy.cs`: `CanShowEnvironmentHover`를 더했다. 사실 조건만 두고 Router 의존은 넣지 않았다. remarks에 "수신 가능·포인터 대상은 호출자가 확인"을 적었다.

**바꾼 파일(시험)**
- 우리 `T/CombatViewSystemTests.cs`: `FocusOrCaptureCleanupException_…`(매개변수) → `[Test] FocusCleanupException_UsesSharedBoundary_AndStillUnbinds()`. 단언은 그대로다(TP3).
- 천명님 `T/CombatMovementViewTests.cs`(Q-T2):
  - `FocusOrCaptureLoss_CancelsFrozenDraft`(`:347-349·356`): capture 사례·매개변수 → `[Test]`, `r.View.FocusLost()`. 이름은 그대로다(TP3).
  - `:394` → `owner.Attach(null)`.
- 천명님 `T/CoreStageCombatCycleTests.cs:355` → `((CombatViewSystem)view.Combat).RequestReturn()`.

**계획과 다른 점:** 없음.

**검토(새 에이전트, 읽기 전용) — 높음 0·중간 0·낮음 3**
- **확인됨**
  - 컴파일:
    - 지운 다섯을 계약 형식으로 부르는 곳 0
    - 가짜 넷은 명시적 구현이 없음
    - `Systems/AssemblyInfo.cs:3`의 `InternalsVisibleTo("ProjectL.Tests.Framework")`로 시험이 내부 멤버를 봄
    - 다른 어셈블리는 조정자를 참조하지 않음
    - 조정자가 구현하는 인터페이스 중 이 넷을 요구하는 것 없음
  - 동작: 환경 호버 식이 HEAD와 같다(단락 순서 포함). 조정자는 MonoBehaviour가 아니라 Unity 이벤트·`SendMessage` 대상이 아니다. 이름 문자열 반사도 없다.
  - 시험: HEAD `CaptureLost`는 `FocusLost`만 불러, 지운 사례가 다른 경로를 보지 않았다. 빠진 시험 조정은 없다.
- **반영**
  - L1: 복귀 경로 주석 두 곳(조정자·계약 요약)과 설계 C-7 글
  - L2: 설계 §7 천명님 목록에 `:379`·`:587`(`public` → `internal`)과 시험 `:347-349` 범위를 더했다.
  - L3: 현강님 줄 범위를 `1169-1173` → `1169-1171`로 바로잡았다.

**사용자 확인(10-23): Unity 확인 문제 없음.**

### Phase C-e (문서·공유) — 2026-10-23, 문서 완료, 코드 변경 없음, **모두 미전달**

| 문서 | 바꾼 것 |
|---|---|
| 현강님 `handoffs/2026-10-22-combat-view-structure-hyeongang-share.md` | §6 새 절: 포기 표시 계약 걷음·컨트롤러 이동(6.1), 바꾼 `CombatViewSystem` 현강님 줄 표(6.2), 계약·판정(`IsTerminal`, `PackUI` 같은 조건 둘, 깃발은 Plan만, 6.3), 고친 시험 한 줄(6.4). UI 코드 고칠 곳 없음 |
| 호진님 `handoffs/2026-10-22-combat-view-structure-hojin-share.md` | §8 새 절: `:303`(C-5)·`:912`(C-2) 주석, F1 분기가 정상 흐름에서 닿지 않는 근거, 호진님 시험 영향 없음 |
| 천명님 `handoffs/2026-09-24-view-core-data-boundary-handoff.md` | §31 새 절(요청 없음): 기대는 Core 동작 넷(31.1), 고친 천명님 줄(31.2: 계약 멤버 다섯, `Attach`·`Detach` 접근성, `CaptureLost`, 필드 이름), 고친 시험(31.3), 시험 장면 변화(31.4) |
| 민형님 | 없음(연결 줄 변경 없음, P7) |
| 배치 안내 `viewDocs/combat-view-folder-guide.md` | 갱신 줄, §0 트리 `UiEntry/` 설명에 Option, §1.1 표(`Input/` 바로 아래에 `CombatViewProgress`, `UiEntry/`에 `CombatAbandonController`, `ActionButtons/`에서 뺌), §2 "헷갈리는 경우"에 두 줄(파일 안 여러 형식 예 `ICombatProgressHost`, 골격이 읽는 View 쪽 상태는 루트가 아님) |
| 정리 문서 `specs/2026-10-23-combat-view-coordinator-inventory.md` | 머리말에 "C 단계 전 상태" 줄과 바뀐 요점 |
| 설계 상태 줄, `CLAUDE.md` | 구현 완료와 실행 중 바뀐 것 |

**검토(새 에이전트, 읽기 전용, 코드·blame과 대조) — 높음 0·중간 5·낮음 7, 빠진 다른 사람 줄 없음**
- **확인됨:**
  - 다른 사람 줄을 blame + diff로 다시 셌고, 메모에서 빠진 줄은 없다.
  - Core 근거 줄이 모두 맞다.
  - 호진님 시험·가짜에 관한 주장이 맞다.
  - 배치 안내 표의 위치·네임스페이스·GUID가 실제와 맞다.
  - 세 메모 모두 미전달로 표시돼 있다.
- **반영(중간)**
  1. 현강님 메모·설계 §7에서 바뀌지 않은 줄 1182를 뺐다.
  2. 배치 안내 §0 트리의 `ActionButtons/` 설명에서 "포기"를 빼고, 갱신 줄을 "§0·§1.1·§2"로 고쳤다.
  3. "정상 흐름에서 닿지 않음"에 Runner 예산 단서를 달았다(호진님 §8, Core §31.1). 튜토리얼 근거("전투를 끝내거나 다시 준비하지 않음")는 C-c 기록과 호진님 메모에 적었다.
  4. 컨트롤러 줄 수를 "현강님 20줄"에서 "중괄호 빼고 13줄: 지운 `Confirm()` 몸통과 `TryRequestAbandon` 본문"으로 고쳤다(메모·설계·이 계획).
  5. Core §31.1 1행의 "View가 입력을 막지 못한다"를 나눠 고쳤다. 명령·미리보기는 국면 검사로 여전히 막히고, 입력 Layer 연결·포기 요청·환경 호버·행동 버튼 표시가 열린다.
- **반영(낮음)**
  - 커밋 표기: `ICombatViewService` 다섯 줄은 `e0b02cc7`로 적었다(Core §31.2, 설계).
  - 시험 줄: `BattleEndBeforePointerCallback_…`는 `:364`다.
  - 실제 바뀐 줄로 맞춤: 674-675, 1196, 1169-1170, 291·293-298, `PackUI.cs:91`
  - CLAUDE.md 호진님 줄 표기를 `:303`·`:912`로 고쳤다.
  - 현강님 메모 머리말의 확인 범위를 "Unity 확인 문제 없음(사용자)"으로 맞췄다.
  - 이 계획의 낡은 줄 번호를 고쳤다(963 → 지금 931, `BattleChangerUI:53`·`PackUI:91`).
