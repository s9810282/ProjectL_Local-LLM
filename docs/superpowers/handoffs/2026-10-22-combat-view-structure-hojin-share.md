# 전투 View 구조 개선 — 호진님 공유 (2026-10-22, 미전달)

작성: 용근 작업(Claude). 근거: [개선 설계 2판](../specs/2026-10-22-combat-view-structure-improvement-design.md), [Task 계획·실행 기록](../plans/2026-10-22-combat-view-structure-improvement-plan.md)(Phase 1~6 기록이 원본). 코드는 작업 트리에 있고 아직 커밋 전입니다(커밋은 사용자). Phase 1~6 모두 Unity 컴파일·EditMode 시험·재생 문제 없음(2026-10-22 사용자 확인).

범위는 Combat 씬의 전투 View 구조입니다(작성자와 무관, CLAUDE.md). 그 안에 있는 호진님 줄은 개선 대상이라 고쳤고, 범위 밖(Stage·Lobby·StageMap)에서는 우리 변경에 묶인 Stage 보류 장치만 사용자 지시로 고쳤습니다. 범위 밖의 다른 구조 항목은 고치지 않고 §5에 제안으로 적었습니다.

경로 약어(`Assets/ProjectL/Script/Systems/` 아래): `C/` = `RuntimeSystem/LifeCycleSystem/LifeCycles/Combat/`, `S/` = `…/LifeCycles/Stage/`, `L/` = `…/LifeCycles/Lobby/`, `V/` = `RuntimeSystem/InGameSystem/ViewSystem/`. 시험은 `T/` = `Assets/Tests/EditMode/Framework/`.

## 1. 먼저 알아 둘 흐름 변화

- **이탈 순서가 모든 결과에서 같습니다.** 승패 기록 → 제출 정지 → 남은 연출 → [정리(EndBattle) → (보스 승리면) 챕터 완료 → 결과 표시] → 사용자 확인 → 떠나기. 보스만 "정리 먼저, 표시 나중"이던 특수 경로가 없어졌습니다. 정리·챕터 완료·표시는 한 걸음이라 같은 Runner Tick에 이어 돌고, 결과 화면은 어떤 결과든 Core가 Run 결과를 확정한 뒤 열립니다.
- **목적지는 `CombatPlan.DestinationOf(Run 결과)` 한 곳입니다.** Run이 이어지면 StageSelect, 끝났으면(패배·포기·최종 승리) Lobby. 전투 쪽에서 새 Run을 만들지 않습니다(새 Run은 로비 출발만).
- **정상 이탈은 Combat을 나가는 순간 진입 요청을 닫습니다**(`CombatLifeCycle.OnExit` → `TryCloseExited`). 그래서 Stage·Lobby의 `OnEnter` 때 요청은 이미 `Closed`입니다. 전투가 남은 채 나간 경우만 `ExitCheck` Job이 F1로 잡습니다.
- **판단은 실행 순간입니다.** `Register(Combat, Next)`는 할 일이 있는지만 보고 `CombatNextJob` 하나를 넣고, 그 Job이 실행될 때 판단해 단계 Job을 Runner에 넘깁니다(같은 Tick에 이어 실행). **(2026-10-23 바뀜: 단계 Job은 그 Job 안에서 차례로 실행, §9.1)**
- **착지 대기가 Runner를 붙들지 않습니다.** 진입 전환 Job은 착지 전이면 요청에 "착지 대기"를 남기고 끝납니다. 착지 게시(`CombatEntryService.TryCompleteMapPresentation`)가 `JobType.EntryTransition`으로 전환을 다시 겁니다. 착지 대기 중에 맵 표시 소유권(Lease)이 풀리면 그 자리에서 F1입니다(입력 정리·서비스 종료 중은 제외).
- **진입 요청에 이름이 생겼습니다.** 진입 종류 `CombatEntryKind { Direct, Map }`(맵 명령 번호 0/양수 해석 대신), 단계 전이 표 하나(`Close`는 활성만, `Fail`은 끝나지 않은 모든 단계에서 받고 예외 없음), 질문 `IsInProgress`·`IsAwaitingLanding`·`HasExited`, 같은 대상 판정 `IsTargetOf`. **(2026-10-23~24 바뀜: 질문이 늘었고 `IsInProgress`는 쓰는 곳이 없어 지웠음, §9.1)**
- **View 계약:** `ICombatViewService`는 진행 계약을 상속하지 않고 `Progress`로 냅니다. `IsPlaybackBusy`는 지웠습니다(재생은 P < S일 때만이라 `IsPresenting = P < S`).

## 2. 바꾼 호진님 줄

| 파일 | 원래 줄 | 바뀐 것 |
|---|---|---|
| `C/Plans/CombatPlan.cs` | 보스 경로(`cb76bd2c`): `Decide` 0행, `PresentCompletion` 보스 갈래, `ExitBattle` 두 갈래 | 삭제. 모든 결과가 `Settle`(정리 → 보스 승리면 챕터 완료 → 표시) → `Leave`(전환 하나). 보스 승리 판정은 `RequiresChapterCompletion` 한 곳 |
| `C/Jobs/CombatExitJobs.cs` | `CombatCompletionCheckJob`·`CombatRunOutcomeJob`(`cb76bd2c`) | 삭제. 챕터 완료는 `CombatChapterCompletionJob`(보스 승리일 때만 등록, `CompleteChapter` 한 번, 거절은 예외) |
| `V/Combat/CombatViewSystem.cs` | 결과 유지 분기(`cb76bd2c`) | 이름 있는 판정 `IsHoldingResult()` 하나(보스 주석 제거). 같은 완료 비교는 `CombatCompletionIdentity.IsSame` |
| `C/ICombatProgressView.cs` | 완료 표시 설명 "보스는 정리·챕터 완료 뒤"(`cb76bd2c`) | "모든 결과는 정리 뒤"로 |
| `C/CombatEntryRequest.cs` | 맵 표시 상태·착지 게시·표시 연결(`edcda135`) | 위 §1의 진입 종류·전이 표·질문·`IsTargetOf`·`MarkAwaitingLanding`·`UsesPresentationOwner` |
| `C/CombatEntryService.cs` | 착지 게시·소유권 해제(`edcda135`), 맵 입력 대조(`Leehojin`) | 착지 게시가 착지 대기 요청이면 `EntryTransition` 등록. 소유권 해제 때 착지 대기 요청이 그 소유권을 쓰면 `Fail` + F1. 대상 비교는 `IsTargetOf`, 맵 진입 판단은 `Kind` |
| `C/MapCombatPresentationTicket.cs` | Ticket(`edcda135`) | 소유권 확인 `IsOwnedBy(lease)` |
| `C/Jobs/CombatEntryTransitionJob.cs` | 착지 대기 갈래(`edcda135`) | `Pending`이면 `MarkAwaitingLanding` 뒤 완료(Runner를 붙들지 않음) |
| `C/Jobs/CombatPrepareJob.cs` | 맵 명령 갈래(`Leehojin`) | 직행 판단을 `Kind`로 |
| `LifeCycles/Tutorial/Plans/TutorialPlan.cs` | 설명 시작 조건의 판정 사본(`89cbc44e`) | `CombatPlan.IsAwaitingPlayerCommand`를 부르고 사본 삭제(뜻은 같음, 조건 추가 없음). `NoJobs`에 "받는 쪽은 읽기만" 약속 주석 |
| `S/StageLifeCycle.cs` | 보류 장치(`dce453db`) — 사용자 지시 | `_pendingCombatReturn`·`TryDeferCompletedCombatReturn`·`OnEnter`/`OnTick`의 보류 갈래·`StopAndCancelEntry`의 비우기·`HasPendingCombatReturn` 사실·보류에만 쓰던 `_combatProgress` 삭제. 보류와 함께 생긴 `StartStageEntry`는 `OnEnter` 안으로 되돌림. 이유: 정상 이탈에서 `ExitCheck`가 없어져, 대비하던 경우(이탈 확인 Job이 Runner 32개 예산 뒤에 남은 채 Stage 진입)가 생기지 않음 |
| `V/Boot/GameBootstrap.cs` | 씬별 카메라 수집·Lobby UI 연결·Stage UI 열기 노드(`Leehojin`) | 현강님 줄과 함께 씬 진입 노드 표 하나 + 이름 있는 상수(`BindUIPriority` −200·`BindCamerasPriority` −190·`PrepareCombatDeckPriority` 90·`OpenSceneUIPriority` 200)로. 순서·동작 그대로. 캐스트 둘은 표를 만드는 자리 한 곳 |
| `Factory/Runtime/InGame/InGameSystemFactory.cs` | `Create`·`CreateComposition`·`CreateWithSources` 줄(`55aa9a3a`·`4d6d739d`) | 연결 줄(민형님 공유 문서에 적음): 운영 조립 자리 `Compose`·`CreateComposition`은 오류 경계를 반드시 받고, 기본 경계 대체 생성은 시험용 입구 `Create`·`CreateWithSources`만(`TestBoundary()`). 부르는 곳은 그대로 |

참고(우리 줄): `IStageDeckEditFacts.HasPendingCombatReturn`과 덱 편집 창 조건(`38869ec1`)도 함께 지웠습니다. 창 조건은 `!IsRunnerIdle`만 남았습니다.

## 3. 고친 호진님 시험

모두 Unity에서 통과 확인(사용자). 걷은 동작을 확인하던 단언은 지우고, 새 순서로도 성립하는 것은 호출만 맞췄습니다(설계 Q1·Q2).

| 시험 | 바뀐 것 |
|---|---|
| `T/CombatEntryIntegrationTests.Stage.cs` | 보류 시험 넷 삭제: `Shutdown_DuringPendingCombatReturn_…`, `NormalCombatReturn_FastLoadAfterRunnerBudget_…`, `DeferredCombatReturn_SharedFaultDoesNotRegisterStagePreparation`, `DeferredCombatReturn_ShutdownClearsPendingPreparationWithoutRegisteringJobs`, 공용 준비 `BeginCombatReturnAtBudget` |
| `T/CombatEntryIntegrationTests.cs` | `…StartsNewRunThenReturnsToLobby` → `Defeat_EndsRun_ReturnsToLobby`. 공용 도우미 `AssertNewRunThenReturn` → `AssertRunEndThenReturn`(천명님과 함께 쓴 줄): 새 Run 상태 단언 일곱 삭제, 목적지 로비·오류 없음·Runner 빔·전투 없음·Run 요약 넷은 그대로 |
| `T/MapCombatPresentationTests.cs` | `Pending_DoesNotActivate` → `Pending_EndsAwaitingLanding_WithoutActivating`(Job 완료 참·착지 대기 참, 전환 없음은 그대로) |
| `T/CombatEntryIntegrationTests.MapMovement.cs` | `DetachDuringAcceptedMovement_AbandonsGateWithoutChangingCoreReceipt`: "다음 프레임이 예외" → "그 자리에서 오류 상태". `CurrentLandingReadyPublishFailure_FaultsThroughExistingBoundary` → `CurrentLandingLeaseLoss_FaultsThroughExistingBoundary`: 오류 상태·첫 오류 문구("소유권")를 봄, 표시 폐기 기대는 소유권 해제 앞으로. Core 수락 결과·현재 노드·로드 0은 그대로 |
| `T/LobbyDepartureIntegrationTests.cs` | 가짜 `CombatViewStub`: 진행 계약을 `Progress => this`로 냄, 재생 중 멤버 삭제 |
| `T/StageMapNodeMovementBootSmokeTests.cs`·`.Victory.cs` | 진행 계약을 View 서비스로 읽던 `CompletionDisplay` 여섯 곳을 `.Progress`로 |
| `T/TutorialPlanTests.cs` | `StartConditions_MatchCombatPlanWaitingForPlayer`: 판정 호출을 `CombatPlan.IsAwaitingPlayerCommand`로, 재생 중 반복·인자·설명 줄 삭제 |

대조만 하고 고치지 않은 것: `StageMapNodeMovementBootSmokeTests.Victory.cs`(확인 전 Combat 유지·확인 뒤 Stage 복귀는 새 순서에서도 같음), `StageMapNodeMovementTests`, `StageFlowTests`.

**보고만:** `StageMapNodeMovementBootSmokeTests.OperationalBoot_RejectShopRecoveryCombatAndAbandonReturn_RespectLanding`(`70de0066`)은 포기 뒤 StageSelect 복귀를 기다립니다. 10-20 정책(포기 → 로비, `b6c8513e`)부터 이미 맞지 않던 것이라 이번 변경과 무관합니다.

## 4. 호진님 코드에 닿는 보이는 차이

- **결과 화면 동안 전투가 "없음"으로 읽힙니다**(정리가 표시보다 먼저라서). 화면 뒤 전투 장면은 결과 유지 판정으로 그대로 남습니다. `HasBattle`을 직접 보는 튜토리얼 세 곳이 해당합니다 — 결과 화면에서 튜토리얼이 시작되거나 전투 대상을 찾을 때만입니다. 지금까지 보스 승리에서는 이미 이렇게 동작했습니다.
  - `V/Tutorial/TutorialViewTarget.cs:49`: 대상 찾기 실패
  - `LifeCycles/Tutorial/Jobs/TutorialRequestJob.cs:74`: 그때 시작하는 튜토리얼 세션은 전투 인스턴스 0으로 기록
  - `V/Tutorial/TutorialViewFocusLocator.cs:304·336`: 현재 행위자·적 본부 찾기 실패
- **로비 출발 전까지 Core는 끝난 Run 상태입니다.** 보유 로스터를 읽는 Global UI가 새 Run의 시작 로스터 대신 끝난 Run의 로스터를 봅니다(로비에 보이는 것은 주인공 초상뿐이라 그림은 같음).
- **착지 대기 중 Runner가 비어 있습니다.** Phase 3 시작 때 대조했습니다 — Stage 흐름은 전투 요청 확인에서 멈춰 할 일이 없고, 덱 편집 창은 맵 요청 확인 대기로 열리지 않고, 튜토리얼은 판단이 할 일을 정합니다(끼어들어도 착지 게시가 거는 전환은 그 뒤에 줄을 섬).

## 5. 고치지 않은 제안 (범위 밖, 원하시면)

| 번호 | 지금 | 할 수 있는 것 |
|---|---|---|
| P10 | 요청 단계·플래그 조합을 직접 해석: `S/StageNodeEntryFlow.cs:56-77`, `S/StageLifeCycle.cs:169-192`, `V/StageMap/StageMapEntryObserver.cs:42-80`, `V/StageMap/StageMapNodeMovement.cs:57-59`. `Prepared`면 `IsJobCompleted`·`Result.Accepted`는 늘 참이고, "Active까지 기다림" 분기는 운영에서 닿지 않음 | 요청의 이름 있는 질문(`IsInProgress`·`IsAwaitingLanding`·`HasExited`)으로 바꾸기. 필요한 질문이 더 있으면 요청에 더해 드림 **(2026-10-23 바뀜: 줄별 대응은 §9.4)** |
| P7 (④) | `S/StageLifeCycle.cs:183-185`가 "요청 `Activating` + Job 완료 + Manager 현재 키 = Combat"으로 전환을 추론. 전환 Job의 두 줄 순서와 Manager가 `Enter()` 전에 키를 바꾸는 순서에 기댐 | 골격이 "수락된 전환 중" 사실을 낼 수 있음. 쓰는 곳이 Stage뿐이라 보류했음 — 원하시면 냄 |
| P8 | Stage·Lobby Flow가 "번호 기억 후 확인" 모델이라 요청이 실행 단계(`Queued`·`Running`·`IsJobCompleted`)를 밖에 공개 | 길게 보면 Flow가 요청 질문만 보고 기다리게 |
| P27 | `StageLifeCycle`이 전투 진입·복귀 규약의 절반과 덱 편집 사실 11개를 함께 맡음(보류 장치를 걷어 260줄) | 덱 편집 사실을 따로 빼기 |
| P29 | 운영 생성자의 `?? new RuntimeFaultBoundary(…)`: `S/StageLifeCycle.cs:51`, `L/LobbyLifeCycle.cs:24`, `V/Lobby/LobbyViewSystem.cs:135`. 조립 경로 하나가 인자를 빠뜨리면 별도 경계가 조용히 생김(지금은 잠복) | Factory 운영 조립 자리(Phase 5)처럼 경계를 필수로 받고 대체 생성은 시험 입구로 |
| Lobby 진입 조건 | `L/LobbyLifeCycle.cs:56`이 "Runner가 비어야 함"을 요구 | 정상 이탈은 이제 나가는 순간 요청을 닫아 확인 Job이 남지 않으므로 그대로 둬도 됨. 원하면 "전투 이탈 완료"(`HasExited`)를 직접 읽어 이유를 분명히 |
| StageMap 신호 | 착지 신호가 `StageMapNodeMovement.Tick` → `Lease.TryComplete` → 진입 서비스 → Ticket을 거침. StageMap 쪽 API(`IMapCombatPresentationOwnerSource`, `Lease.TryComplete`)는 그대로 둠 | 받기 조건(`StageMapNodeMovement.cs:57-59`)을 P10 질문으로 줄이기 |

## 6. 확인 부탁

- §5 중 원하는 것이 있으면 알려 주세요. 요청 쪽 질문을 더하는 것은 우리 쪽에서 할 수 있습니다.
- 결과 화면에서 튜토리얼이 시작되는 경우가 있는지(§4 첫 항목의 차이가 실제로 보이는지).

## 7. 최종 승리 화면이 연결돼 있지 않음 (2026-10-23 발견 → 2026-10-24 해결, §10.3)

**다른 작업자 작업과 겹칠 수 있어 사용자 결정으로 미뤘고, 코드·씬은 바꾸지 않았습니다.**

- 엔딩 패널(`EndingPanel.prefab`)은 `9965237c`까지 Combat 씬에 있었는데, 병합 `9e0c0f2b`(core-system → StageSelect)에서 Combat 씬이 반대쪽 판으로 정리되며 빠졌습니다. 그래서 최종 승리(지금은 보스 승리) 때 `CombatEndUI.ending`이 비어 F1로 멈춥니다.
- 예전 자리(`CombatEndCanvas` 아래)는 그 캔버스가 꺼진 채 저장돼 있어, 결과 화면을 열지 않는 최종 승리에서는 엔딩의 부모 활성 조건(`EndingRunSummaryUI.Open`)에 걸립니다. 되돌릴 때는 항상 켜진 다른 캔버스에 두는 것을 권합니다(현강님 메모 §5).
- `Lobby.unity`에 남은 임시 엔딩 요약(`83e53dac`)은 그대로입니다.

## 8. 조정자 C 단계 (2026-10-23, 미전달)

근거: [C 단계 설계](../specs/2026-10-23-combat-view-coordinator-c-stage-design.md) §7, [Task 계획·실행 기록](../plans/2026-10-23-combat-view-coordinator-c-stage-plan.md). 코드는 작업 트리에 있고 커밋 전입니다(커밋은 사용자). Phase C-a~C-d 모두 Unity 확인 문제 없음(2026-10-23 사용자). 줄 번호는 HEAD `4c328d07` 기준입니다.

**호진님 코드에서 고칠 곳은 없습니다.** 바꾼 호진님 줄은 `CombatViewSystem.cs`의 주석 둘입니다.

| HEAD 줄 | 무엇(`cb76bd2c`) | 바뀐 것 |
|---|---|---|
| 303 | `TickCore`의 "그 밖에 전투가 사라지거나 다른 전투로 바뀌면 … 끊는다" 주석 | C-5에서 이 분기가 조용히 끊지 않고 원인을 F1로 먼저 보고하게 바꾸며 주석을 다시 썼습니다. 바로 위 결과 유지 블록(299-301)과 그 동작은 그대로입니다 |
| 912 | `PresentCompletion` 요약의 "지난 수명의 확인은 무시합니다" | C-2에서 완료 표시가 새 진행 사실 클래스 `Input/CombatViewProgress`(전이 규칙)와 조정자의 호스트 `ShowCompletion`(표시·배경음)으로 나뉘며 두 요약으로 옮겼습니다 |

- **F1이 되는 분기:** "결과 유지가 아닌데 전투가 사라지거나 다른 전투로 바뀜"은 정상 흐름에서 닿지 않습니다.
  - 정리 → (보스면) 챕터 완료 → 완료 표시가 같은 Runner Tick에 이어 돌아, 다음 View Tick은 결과 유지에서 돌아갑니다.
  - 그 밖에는 같은 씬에 다음 전투를 붙이지 않습니다.
  - 검토 에이전트가 일반 승패·보스·포기·씬 이탈·앱 종료·튜토리얼(전투를 끝내거나 다시 준비하지 않음)·새 Run 경로를 대조했습니다.
  - 단서: 연쇄 중간에 Runner의 한 Tick 예산(32개)이 끊기는 경우는 닿는 경로를 찾지 못했지만, 확인하지 못한 추측으로 남깁니다.
- **호진님 시험은 영향이 없습니다.**
  - `LobbyDepartureIntegrationTests.CombatViewStub`은 계약에서 빠진 멤버를 공개 메서드로 남겨도 컴파일됩니다.
  - `HojinCombatAttackDragIntegrationTests`가 반사로 읽는 조정자 필드(`_interaction`·`_display`)는 이름이 그대로입니다.

## 9. 전투 골격 점검 (2026-10-23~24, 미전달)

근거: [골격 점검 설계](../specs/2026-10-23-combat-skeleton-check-design.md), [Task 계획·실행 기록](../plans/2026-10-23-combat-skeleton-check-plan.md). 코드는 작업 트리에 있고 커밋 전입니다(커밋은 사용자). Phase K-a~K-e 모두 Unity 컴파일·EditMode 시험 문제 없음(사용자 확인). 줄 번호는 HEAD `4c328d07` 기준입니다.

### 9.1 먼저 알아 둘 흐름 변화

- **단계 Job은 `CombatNextJob` 안에서 차례로 실행합니다.** 판단한 단계 Job을 Runner에 다시 넣지 않습니다. 한 번 실행으로 끝나지 않는 단계 Job이 있으면 F1입니다. 단계는 지금도 한 Runner Tick 안(`CombatNextJob` 하나 안)에서 돕니다. 없어진 것은 Runner 큐·예산에 기대던 점입니다. 그래서 §1 넷째 줄의 "Runner에 넘김"은 바뀌었고, §8의 "Runner 한 Tick 예산(32개)이 끊기는 경우" 단서는 해당이 없습니다.
- **진입 등록은 공용 입구 하나입니다.** 진입 서비스가 `Register(JobRootType.Combat, JobType.Entry)`를 부르고, Plan이 진입 서비스의 `Current`를 읽어 [준비 → 전환]을 만듭니다. `IJobRegisterSystem.RegisterCombatEntry`는 지웠습니다. 등록됨 표시(`MarkQueued`)와 등록 실패 표시(`Fail`)는 진입 서비스가 합니다.
- **진입 요청 질문:** `IsAwaitingRegistration`·`IsQueued`·`IsActivating`·`IsActive`·`IsCancelled`·`IsRejected`·`IsReplaceable`이 생겼습니다(`IsAwaitingLanding`·`HasExited`·`IsTargetOf`는 그대로). `IsInProgress`는 쓰는 곳이 없어 지웠습니다. `Status`는 Stage·StageMap 소비자가 있어 public으로 남겼습니다.
- **씬 Node 순서 원본:** 새 표 `RuntimeSystem/Common/SceneNodeOrder.cs`입니다(Bootstrap 노드와 Combat 씬 시스템 노드, 기준 값은 숫자·붙는 값은 식). §2 Bootstrap 행이 알린 이름 있는 상수 넷(`BindUIPriority`·`BindCamerasPriority`·`PrepareCombatDeckPriority`·`OpenSceneUIPriority`)은 이 표의 `BindSceneUI`·`BindCameras`·`PrepareCombatDeck`·`OpenSceneUI`로 옮겼습니다. 값은 그대로입니다.

### 9.2 바꾼 호진님 줄

| 파일 HEAD 줄 | 무엇 | 바뀐 것 |
|---|---|---|
| `C/CombatEntryService.cs` 121-122 | `Shutdown`에서 끝난 의미를 보존하는 조건(`dce453db`) | 같은 식을 질문으로: `!_current.HasExited && !_current.IsCancelled && !_current.IsRejected`. 바로 위 주석(120)은 그대로 |
| `V/Tutorial/TutorialViewSystem.cs` 51-53 | 상수 `SceneEnterPriority`·`SceneExitPriority`와 주석(`89cbc44e`) | 지웠습니다. 값과 이유는 표의 `SceneNodeOrder.TutorialAttach`(110)·`TutorialDetach`(−110)로 옮겼습니다 |
| 같은 파일 203-204 | Loading 등록 두 줄 | 표를 읽습니다. 그 위에 "순서와 그 이유의 원본은 SceneNodeOrder다." 한 줄 |
| `V/Combat/CombatViewSystem.cs` 303 | `TickCore`의 F1 분기 주석(`cb76bd2c`, §8에서 C-5가 다시 씀) | "정리와 완료 표시는 같은 Runner Tick에 이어지고(Settle)" → "한 Job 안에서 이어지고(`CombatNextJob`이 `CombatPlan.Settle`이 만든 목록을 차례로 실행)". 주석만 |
| `C/Jobs/CombatPrepareJob.cs` 45-53 | 맵 명령 갈래(`dce453db`) | 그룹(`_service.CoreGroup.StageCommands`)에서 꺼내지 않고, 생성자로 받은 `IStageCommandProcessingService`를 부릅니다. 맵 진입인데 이 서비스가 없으면 생성자에서 `ArgumentNullException`입니다. Core 호출·실패 처리는 그대로 |

### 9.3 고친 호진님 시험 (사용자 허락)

- `CombatEntryIntegrationTests.cs:962`: 감싼 등록기 `StagePreparationObserver`의 `RegisterCombatEntry` 전달 한 줄을 지웠습니다(계약에서 빠져 컴파일되지 않음). 이 시험은 실제 런타임 조립이라 진입이 그대로 돕니다.
- `MapCombatPresentationTests.cs`:
  - 가짜에 필드 `internal ICombatEntryService Entry;`, SetUp에 `_register.Entry = _service;` 한 줄
  - 가짜 `Registration.Register`가 `(Combat, Entry)`일 때 `Calls++`와 `OnRegister?.Invoke(Entry.Current)`를 하고, 그 밖에는 지금처럼 `NotSupportedException`
  - 가짜의 `RegisterCombatEntry`와 `MarkQueued`를 지웠습니다(등록됨 표시는 이제 서비스가 함). 설명 주석 한 줄
  - `OnRegister`·`Calls`를 쓰는 단언은 고치지 않았고 그대로 성립합니다.
- `TutorialViewSystemTests.cs:34-35`: `CombatViewSystem.SceneEnterPriority` → `SceneNodeOrder.CombatViewAttach`, `SceneExitPriority` → `SceneNodeOrder.CombatViewRelease`. 메시지·단언은 그대로입니다. 같은 파일 `:9`의 `using …ViewSystem.Combat;`가 쓰이지 않게 됐지만(숨김 진단) 허락 밖이라 두었습니다.
- 손대지 않은 것(지워도 됨): `RegisterCombatEntry`를 공개 메서드로 가진 가짜 11개, `ActiveCharacterCount`를 공개 속성으로 가진 가짜 셋(`CombatEntryIntegrationTests.MapMovement.cs:514`·`MapCombatPresentationTests.cs:297`·`StageMapNodeMovementTests.cs:421`). 계약에서 빠진 멤버라 남아도 컴파일됩니다.

### 9.4 제안 — Stage·StageMap의 요청 단계 해석을 질문으로 (고치지 않음, §5 P10의 줄별 대응)

필요한 질문은 우리 쪽 요청 파일에 더해 드립니다(지금은 쓰는 곳이 없어 더하지 않았습니다). 모두 옮기면 `Status`를 internal로 닫을 수 있습니다.

| 줄 | 지금 해석 | 쓸 질문 |
|---|---|---|
| `S/StageLifeCycle.cs:169-170` | `Created ∨ Cancelled ∨ Rejected` | `IsAwaitingRegistration ∨ IsCancelled ∨ IsRejected` |
| `S/StageLifeCycle.cs:183` | `Activating ∧ 배출 ∧ 수락` | `IsActivating`(활성화는 수락·배출 뒤에만 시작) |
| `S/StageLifeCycle.cs:192` | `Created ∨ Queued` | 새 `CanCancelBeforeRun` |
| `S/StageNodeEntryFlow.cs:56` | `≠ Created` | `!IsAwaitingRegistration` |
| `S/StageNodeEntryFlow.cs:65` | `Cancelled` | `IsCancelled` |
| `S/StageNodeEntryFlow.cs:71` | `Closed ∨ Faulted` | 새 `HasEndedUnexpectedly` |
| `S/StageNodeEntryFlow.cs:73-74` | `Created ∨ Queued ∨ Running ∨ 미배출` | 새 `IsWaitingForJob` |
| `S/StageNodeEntryFlow.cs:77` | `Active ∨ Rejected` | 새 `IsEntryDecided` |
| `V/StageMap/StageMapEntryObserver.cs:42` | `Prepared ∧ 배출` | 새 `IsPrepared` |
| `V/StageMap/StageMapEntryObserver.cs:49` | `Cancelled` | `IsCancelled` |
| `V/StageMap/StageMapEntryObserver.cs:80` | `Active ∨ Rejected` | 새 `IsEntryDecided` |
| `V/StageMap/StageMapNodeMovement.cs:59` | `Prepared ∧ 배출 ∧ 수락` | 새 `IsPrepared`(준비 단계는 수락 뒤에만 옴) |

새로 더할 질문 다섯의 식:
- `IsWaitingForJob` = `Created ∨ Queued ∨ Running ∨ !IsJobCompleted`
- `CanCancelBeforeRun` = `Created ∨ Queued`
- `IsPrepared` = `Prepared ∧ IsJobCompleted`
- `IsEntryDecided` = `Active ∨ Rejected`
- `HasEndedUnexpectedly` = `Closed ∨ Faulted`

### 9.5 낡게 된 호진님 문서 (고치지 않음)

- `docs/superpowers/viewDocs/stage-selection-input-guide.md:23`: `JobRegisterSystem.RegisterCombatEntry` → 이제 `Register(JobRootType.Combat, JobType.Entry)`입니다(진입 서비스가 부르고 등록됨 표시도 서비스가 함).
- `docs/superpowers/guides/stage-artifact-shop-sequence-uml.md:159`: 같은 입구 이름이 남아 있습니다.
- `docs/superpowers/specs/2026-10-18-tutorial-buffer-job-public-contract-design.md:62`: "요청 객체를 받는 것은 전투 진입 전용 `RegisterCombatEntry`뿐"은 이제 틀립니다. 전투 진입도 범용 `Register(Combat, Entry)`이고, 요청은 Plan이 진입 서비스의 `Current`로 읽습니다.
- `docs/superpowers/handoffs/2026-10-10-stageselect-framework-share.md:132`: `RegisterCombatEntry`·`CombatPlan.RegisterEntry` 경로 설명이 낡았습니다(위와 같은 뜻).

### 9.6 이 메모 앞 절에서 바뀐 사실

- §1 넷째 줄(판단 뒤 Runner에 넘김), 다섯째 줄(질문 `IsInProgress`): §9.1로 바뀌었습니다(그 줄에 표시).
- §2 `CombatPrepareJob.cs` 행: 맵 명령 갈래가 받은 서비스를 부릅니다(§9.2).
- §2 `GameBootstrap.cs` 행의 이름 있는 상수 넷: `SceneNodeOrder`로 옮겼습니다(§9.1).
- §5 P10의 "`IsInProgress`": 지웠습니다. 줄별 대응은 §9.4입니다(그 줄에 표시).
- §8의 "같은 Runner Tick에 이어 돎": 지금도 한 Tick 안이지만 이제 `CombatNextJob` 하나 안에서 돕니다. "Runner 예산이 끊기는 경우" 단서는 해당이 없습니다.
- §8 303 행(F1 분기 주석): K-a에서 한 번 더 고쳤습니다(§9.2).

## 10. 전투 View 단계 1라운드 (2026-10-24, 미전달)

근거: [View 단계 설계](../specs/2026-10-24-combat-view-stage-design.md), [1라운드 Task 계획·실행 기록](../plans/2026-10-24-combat-view-stage-round1-plan.md). 코드는 작업 트리에 있고 커밋 전입니다(커밋은 사용자). Phase V-a~V-d 모두 Unity 확인 문제 없음(사용자). 다만 V-d의 마지막 검사(엔딩 부모 캔버스 켜짐) 한 줄은 그 확인 뒤에 더해 컴파일 미확인입니다. 줄 번호는 기준 커밋 `8ed3f8cb` 기준입니다.

### 10.1 바꾼 호진님 줄

- **결과 화면 `CombatEndUI.cs`(현강님 파일)의 호진님 줄(`cb76bd2c`):** `Present`의 `ending != null &&`(`:90`)와 엔딩 null 예외(`:96-97`)를 지웠습니다. 참조 검사가 진입 때 엔딩을 확인하므로 이 자리에서는 늘 있습니다(§10.3). `Dismiss`의 `if (ending != null)`(`:183`)는 정리 경로라 그대로입니다.
- **행동 버튼 판정 `CombatActionButtons.cs`의 호진님 줄(`34ee0de4`):**
  - `Visible` 요약(`:28`)을 고쳤습니다.
  - 숨김 조건(`:49`)은 `isSubmissionSuspended || !facts.IsInputAvailable`에서 `CombatInputPolicy.IsInputBlocked(facts) || !facts.IsInputAvailable`로 바뀌었습니다. 이제 포기 요청과 확인창 열림에도 숨깁니다.
  - 확인창은 배치 국면에서만 열리고 그때 버튼은 이미 숨겨져 있어, 보이는 차이는 포기 순간뿐입니다.
  - `Read`의 정지 인자는 지웠습니다(우리 줄 `:47`).
- `FieldUnitPresenter.cs:138-164` `TryGetRelocationBody`를 지웠습니다. 운영·시험 어디서도 부르지 않았습니다(재배치 TR2용으로 보임). 필요하면 기준 커밋에서 되살릴 수 있습니다.
- 입력 사실 `CombatInputFacts`, 정책 `CombatInputPolicy`, 조정자 `ReadFacts`의 호진님 줄(튜토리얼 제한 사실·판정)은 그대로입니다.
  - 그 뒤에 사실 셋(제출 정지·포기 요청·확인창)과 판정 둘(`IsInputClosed`·`IsInputBlocked`)을 더했습니다.
  - 공통 판정(`PassesCommon`)이 "입력 닫힘"을 보므로, 포기를 확인하면 그 순간부터 명령·미리보기를 받지 않습니다. 튜토리얼 제한 판정과는 서로 독립입니다.

### 10.2 고친 호진님 시험(허락)

- `StageMapNodeMovementBootSmokeTests.cs:523`·`StageMapNodeMovementBootSmokeTests.Victory.cs:207`: `Hud`의 `completionButton` 대신 `Completion`의 `confirmButton`을 읽습니다. 씬에서 같은 버튼(`2100000302`)이라 확인하는 내용은 같습니다.
- 기록 문구는 그대로 두었습니다. 바꾸셔도 됩니다.
  - `:534`의 "actual HUD completionButton"
  - Victory `:198·214`의 "실제 HUD"·"actual HUD"
- 손대지 않은 것:
  - `HojinCombatAttackDragTests:454-455` 가짜의 손패 멤버는 그대로입니다. 계약에서 빠졌지만 공개 메서드라 컴파일됩니다.
  - `TutorialViewFocusLocatorTests:92`의 `HandCardHoverView` 형식도 그대로입니다(파일을 남김).

### 10.3 최종 승리 엔딩(§7 해결)

- 사용자가 Combat 씬에 새 루트 `BossEndingCanvas`(Screen Space - Overlay, 정렬 7, 켜짐)를 만들고, 그 아래에 `EndingPanel.prefab`을 두어 `CombatEndUI.ending`에 연결했습니다. 패널은 꺼진 채 저장돼 있다가 열릴 때 켜집니다.
- `CombatEndUI.ValidateReferences`가 엔딩의 null·결과 화면 밖·부모 켜짐을 검사합니다(부모 켜짐은 컴파일 미확인). 빠지거나 꺼진 캔버스로 옮겨지면 Combat 진입 때 멈춥니다.
- 엔딩 작업과 겹치는 데가 있으면 알려 주세요. `Lobby.unity`의 임시 엔딩 요약(`83e53dac`)은 그대로입니다.

### 10.4 옛 손패 경로

- 옛 손패(`HandPresenter`) 경로를 운영 코드·계약·씬 루트 참조에서 걷었습니다(손패 집기, 손패 영역에 놓아 회수). 손패 선택은 유지 덱 카드, 되돌리기는 우클릭입니다.
- 튜토리얼 "손패" 초점(`TutorialViewFocusLocator`)은 덱 카드를 보므로 영향이 없습니다.
- 씬의 `Hand` 객체에 `HandPresenter` 컴포넌트가 붙어 있어, Missing Script를 만들지 않으려고 스크립트 두 파일은 남겼습니다. 컴포넌트를 뗀 뒤 지울 예정입니다.

## 11. 전투 View 단계 2라운드 (2026-10-25, 미전달)

근거: [2라운드 설계](../specs/2026-10-24-combat-view-stage-round2-design.md), [2라운드 Task 계획·실행 기록](../plans/2026-10-24-combat-view-stage-round2-plan.md). 코드는 작업 트리에 있고 커밋 전입니다(커밋은 사용자). Phase V2-a~V2-g 모두 Unity 확인 문제 없음(사용자). 줄 번호는 기준 커밋 `23436b00` 기준입니다.

### 11.1 공격 Drag 넘겨받기를 소유자 하나로 (V2-b·V2-c)

- 묶음 `CombatInteraction`의 넘겨받기 본문(`TryHandleAttackDragPointer` `:152-206`)과 관련 줄(`:51` 필드·`:69` 대입·`:356-358`)을 새 소유자 `Input/Interaction/CombatAttackDragHandover.cs`로 옮겼습니다. 생성자 서명 `:63`은 그대로입니다.
  - 받는 쪽은 좁은 계약 `Input/Interaction/ICombatAttackDragTarget.cs`입니다. 경로 입력 `CombatPathInput`이 명시적 구현으로 답합니다.
  - 몸짓 `CombatAttackDragGesture`와 경로 쪽 출발점 메서드 셋(`CombatPathInput.cs:109-150`)은 그대로입니다.
  - 묶음의 `TryHandleAttackDragPointer`는 이름을 남기고 소유자에 위임합니다. 묶음 생성자 서명도 그대로입니다.
- 바뀐 것:
  - 경로 null·정지 검사 여섯 곳(`path != null`·`path == null`·`ReferenceEquals(path, _path)`)은 `IsTargetAttached`(받는 쪽이 있고 끊기지 않음) 하나가 됐습니다.
  - 낡은 후보 검사는 `InvalidateStaleCandidate` 하나입니다. 포인터 처리와 매 Tick 감시가 같이 씁니다.
  - 수명 단계의 무효화 다섯(`SelectCanceled` `:265`·`BeforeResult` `:393`·`CancelPresentation` `:422`·`CancelInputs` `:436`·`Stop` `:449`)과 `Poll`의 후보 블록은 지웠습니다. 묶음이 소유자 목록을 돌 때 넘겨받기 소유자의 수명 단계가 맡습니다. 넘겨받기가 목록 맨 앞입니다.
  - 의도 입구 넷(`RequestReturn` `:252`·`CancelDragOrAttack` `:294`·`LeaveMoveMode` `:309`·`AttackModeRequested` `:326`)은 `_attackDrag?.Invalidate()`가 `_handover?.Invalidate()`로 바뀌어 한 줄씩 남았습니다. 공격 진입은 지금처럼 진입 판단(`CanEnter`) 앞에서 무효화합니다.
- 동작 차이(보이는 차이는 없습니다):
  - `CancelInputs`·`Stop`의 몸짓 무효화가 두 번에서 맨 앞 한 번으로 줄었습니다. 옛 코드는 경로 취소 뒤에 한 번 더 했습니다. 결과가 달라지는 것은 경로 취소 콜백 안에서 새로 누른 경우뿐입니다.
  - 덱 카드 집기·아이템 집기·공격 진입 때 전경 전환 규칙(`BeginForeground`)이 몸짓을 무효화합니다. 그때 몸짓은 Idle이거나 이미 Suppress라 세대 번호만 오릅니다.
  - 경로를 묶음 밖에서 직접 `Detach`한 뒤 UI 위에서 떼면, 옛 코드는 끊긴 경로에 `CancelInputs`를 불렀고 지금은 부르지 않습니다. 운영에는 그런 호출이 없습니다.
- 시험 `HojinCombatAttackDragTests`는 바꾸지 않았습니다. 사례를 하나씩 대조했고 결과가 같습니다.

### 11.2 조정자에서 옮기거나 바꾼 호진님 줄 (V2-e·V2-f·V2-g)

| 옛 자리(`CombatViewSystem.cs`) | 지금 |
|---|---|
| Router 생성 `:648-649` | 입력 Factory `CombatSceneInputFactory.CreateSceneInput`(V2-e) |
| `:1017` `isAttackActive` | 연결 입력 묶음의 입력 기능 묶음을 읽습니다(V2-e) |
| `:99-100` 튜토리얼 필드, `:249-250` 대입 | 지움(V2-f) |
| `:189` `_combatQueries = inGame.CoreGroup.CombatTimeline` | 필드에 두지 않고 `BindServices`의 타임라인 인자로 넘깁니다(V2-f, 계획 TP-10). 묻는 자리는 표시 값 읽기 하나입니다 |
| 클래스 설명 `:58`("CombatViewBuffer와 Tutorial 요청 Buffer에만 씁니다") | Tutorial 요청 Buffer는 막힘 보고 객체가 쓴다고 고쳤습니다(V2-f) |
| `:236-238` 튜토리얼 짝 검사 | 튜토리얼 막힘 보고 `Input/Assist/CombatBlockedInputReport.cs`의 생성자로 옮김(V2-f). V2-g에서 두 Sink를 각각 필수로 검사하게 바뀌었습니다(`ArgumentNullException`) |
| `:649` 막힘 알림 인자 | `_blockedInput.Report`(V2-f) |
| `:1018` 입력 제한 사실 | `_blockedInput.Restriction`(V2-f) |
| `:1025-1037` `ReportBlockedInput` | `CombatBlockedInputReport.Report()`(V2-f) |
| `BindServices` `:232-234` 선택 인자(재고·튜토리얼 둘) | 필수(V2-g). 시험 조립은 그 조립의 실제 Buffer 창구를 넘깁니다(기본 제한 None) |

- **연결 줄 `InGameViewSystemFactory.cs:60`(V2-g):** 조정자 생성자가 생성 계약 둘(`ICombatSceneFactory`·`ICombatSceneInputFactory`)을 받게 되어, `CombatSceneFactory.Instance`를 두 번 넘깁니다. 민형님 Factory 공유 문서 §24에도 적었습니다.
- 그대로: `PostBindService`의 `:188`·`:213-214`, Router의 튜토리얼 막힘 줄.
- **제안(고치지 않음):** `:213-214`의 `?? throw new InvalidOperationException(…)`은 이제 보고 객체 생성자의 null 검사와 겹칩니다. 예외 형식과 문구만 다르고 해는 없습니다. 지울지는 정해 주세요.

### 11.3 고친 호진님 시험(허락)

- `HojinCombatAttackDragIntegrationTests.cs:128`(Q-T6 a): 리플렉션 경로를 조정자 `_interaction` → 연결 입력 묶음 `_input`의 `Interaction`으로 맞추고 `using` 한 줄을 더했습니다. 확인 내용은 그대로입니다.

## 12. 전투 View 단계 3라운드 (2026-10-25, 미전달)

근거: [3라운드 설계](../specs/2026-10-25-combat-view-stage-round3-design.md), [3라운드 Task 계획·실행 기록](../plans/2026-10-25-combat-view-stage-round3-plan.md). 코드는 작업 트리에 있고 커밋 전입니다(커밋은 사용자). Phase V3-a~V3-i 모두 **컴파일 미확인**입니다. 줄 번호는 기준 커밋 `35562e96` 기준입니다.

### 12.1 연결 객체와 고친 호진님 시험 (V3-c, Q-T7 a)

- 조정자의 연결 수명 필드 여덟(표시·입력 표면·연결 입력 묶음·정보창·덱 호버·루트 구독·첫 그리기·전투 번호)이 연결 객체 `CombatViewConnection`(Combat 루트 폴더) 하나가 됐습니다.
- `HojinCombatAttackDragIntegrationTests.cs:130`: 리플렉션 경로를 조정자 `_input`·`_display` → `_connection`의 `Input.Interaction`·`Display`로 맞췄습니다(`:129`는 우리 줄). 확인 내용은 그대로입니다.

### 12.2 사건 Presenter의 호진님 줄 (V3-i)

- 생성자가 필수 인자 여덟이 됐습니다: `(units, values, audio, sounds, camera, effects, projectileRoot, ports)`.
- 토네이도 숨김·켜기(`:78` 선택 인자 둘)는 필드 창구 `Playback/CombatFieldEventPorts.cs`의 필수 대리자 둘(`HideTornado`·`ShowTornado`)이 됐습니다.
  - `:122-124`의 null 갈래(`showTornado != null ? … : null`)를 걷었습니다. 늘 `(id, cell) => ports.ShowTornado(id, cell, _mapper)`입니다.
  - `:132` 사실 처리기 생성은 창구에서 읽습니다(인자 순서 그대로).
- 표시 Factory의 이름 있는 인자 `hideTornado: terrain.Hide`·`showTornado: terrain.ShowAt`(호진님 줄)는 필드 창구 생성 자리로 옮겼습니다. 넘기는 메서드는 그대로입니다.
- 운영 동작은 같습니다. 시험 조립은 아무것도 하지 않는 창구를 넘깁니다(우리 도우미 `CombatViewSystemTests.FieldPorts`).

### 12.3 소리 프리셋 시험 (V3-h)

- `StageMapCombatEntryBootSmokeTests.cs`(호진님 파일)의 프리셋 읽기 줄 `:129-138`을 실제 DB 프리셋(`CombatViewSystemTests.SoundPresets`)으로 맞췄습니다. 바뀐 줄은 우리 `:129-130`·현강님 `:131`·천명님 `:137-138`이고, 호진님 줄은 그대로입니다. 확인 내용(슬롯 프리셋 목록)은 같습니다.
- 소리 프리셋은 이제 유닛 외형 표 칸 둘과 전투 연출 설정 칸 넷이 원본입니다(현강님 메모 §10.5).

### 12.4 알려 드리는 것

- 보스 스테이지(2101) 배경은 맵 정의(`Map_Map` → `Map.prefab`)이고, 그 안에 전환 카메라(`Day Camera`·`Night Camera`)가 없습니다. W9부터 표시를 만들 때 카메라 이름을 검사해 이 스테이지 진입이 실패합니다(기준 커밋에서도 첫 밤 전환 때 카메라 조회에서 실패했습니다). 사용자 결정으로 지금 세팅은 임시라 그대로 둡니다.

## 13. 개발 진입 지정 스테이지 배치 (2026-10-27, 미전달)

Play From Boot에 새 개발 모드(`DevBattleStage.PlacedStage`)를 더하면서 Core 조정자 계약에 오버로드 `EnterChapter(int chapterKey, ChapterStagePin pin)`이 생겼습니다(사용자 허락, Core 전달 문서 §5.53). 설계는 [dev-placed-stage-entry-design](../specs/2026-10-26-dev-placed-stage-entry-design.md)입니다.

- `LobbyDepartureIntegrationTests.cs`의 감싸는 가짜 `CountingChapterProgression`(호진님 줄)에 새 오버로드를 더했습니다. 컴파일을 맞추기 위한 것입니다.
  - 기존 `EnterChapter(int)`와 같은 꼴로, 챕터를 `EnteredChapters`에 적고 안쪽으로 넘깁니다.
  - using은 이미 있었습니다. 시험 본문과 단언은 그대로입니다.
- 운영 `EnterChapter(int)`의 동작은 같습니다. 로비 출발(`LobbyRunStartJob`)·Stage 진입은 고칠 곳이 없습니다.

## 14. 전투 카메라 조작(이동·줌) — 입력 에셋·시험 가짜 (2026-10-27, 미전달)

설계 [카메라 조작](../specs/2026-10-26-combat-camera-control-design.md), 계획 [카메라 조작 계획](../plans/2026-10-27-combat-camera-control-plan.md) §6.

- 액션 에셋 `PlayerInputActions.inputactions`에 새 맵 `CombatCamera`를 Combat 맵 바로 뒤에 더했습니다(Pan = WASD·화살표 2D 묶음, Zoom = 휠 PassThrough). 다른 화면의 맵은 그대로입니다.
- 스테이지 입력 수신 계약 `IStageInputReceiver`에 메서드 둘이 생겨 `ViewInputCompositionTests.cs`의 기록 가짜 `RecordingReceiver`에 빈 메서드 둘을 더했습니다. 컴파일을 맞추기 위한 것이고 시험 본문·단언은 그대로입니다. 이 시험은 실제 에셋을 읽어 새 맵을 찾으므로 실행 영향은 없을 것입니다.
- 새 스테이지 프랍(View DB `ViewStageDataSO`)을 만들 때는 `stageViewPropData`의 `cameraSettings`에 `CombatCameraSettings_Default`(또는 스테이지 전용 값)를 넣어야 합니다. 비면 부트 DB 생성에서 실패합니다.

## 15. 전투 넉백 미리보기 — View DB `StageFieldViewData` 칸 둘 (2026-10-27, 미전달)

설계 [넉백 미리보기 View](../specs/2026-10-27-combat-knockback-preview-view-design.md), 계획 [넉백 미리보기 계획](../plans/2026-10-27-combat-knockback-preview-view-plan.md) §7. 공격 호버 때 밀려날 대상에 화살표와 도착 칸을 그립니다.

- 필드 표시 설정 `StageFieldViewData`에 머리말 "Knockback Preview"와 칸 둘을 더했습니다.
  - `knockbackArrowPrefab`: 필수이고 Renderer가 하나 이상 있어야 합니다.
  - `knockbackArrowLift`: 0 이상, 기본 0.05(화살표를 칸 표면 위로 띄우는 높이).
  - 검사 `ValidateKnockbackPreview()`는 `Validate` 끝에서 표시 모드와 상관없이 돕니다. 비면 부트 DB 생성에서 실패합니다.
- `StageFieldView_Default.asset`에 임시 화살표 `Prefabs/Combat/Arrow.prefab`을 연결했습니다. 사용자가 만든 임시 아트입니다.
  - 약속: 화살표는 로컬 +Z를 가리키고 바닥 평면에 눕혀 두며, 피벗은 가운데, 충돌체는 없습니다.
  - 모든 Renderer의 재질에 `_BaseColor`가 있어야 합니다(밀림·막힘·면역 색을 덮어씀, 필드 구성 때 검사).
- 새 필드 설정 에셋을 만들 때는 이 칸을 채워야 합니다. 시험이 `StageFieldViewData`를 직접 만들어 프리팹이 비면 필드는 화살표 부품을 만들지 않습니다(궤적 재질과 같은 꼴).
- 우리 `ViewDataTableTests`의 설정 도우미 둘에 화살표 한 줄씩을 넣었습니다. 호진님 시험은 바뀌지 않았습니다.

## 16. 2026-11-04 전투 View 평가 후속 3·4차 — 기믹 줄·층 표·하붑 거절 (미전달)

[후속 수정 계획](../plans/2026-11-04-combat-view-evaluation-fixes-plan.md) §8(3·4차)에서 호진님 줄을 고쳤습니다. 줄 번호는 HEAD `032608bf` 기준입니다. 컴파일 미확인, 새 시험 없음.

- `Factory/Runtime/InGame/View/CombatSceneDisplayFactory.cs` 150·160·164-178(C4-02): 필드 창구 조립(지형·토네이도 외형 람다 넷, 토네이도·낙석·하붑 인자)을 같은 파일의 새 함수 `CreateFieldPorts`로 옮겼습니다. 넘기는 값과 순서는 그대로입니다. 새 기믹 창구는 이 함수 한 곳에 더하면 운영과 미리보기에 함께 들어갑니다.
- `Factory/Runtime/InGame/View/CombatTraitPreviewFactory.cs` 102(C4-02): 미리보기도 `CreateFieldPorts`를 부릅니다. 그래서 미리보기에도 토네이도 외형 연결이 들어갑니다(사용자 결정 Q-N2 A). 토네이도 계획 TP-9의 "미리보기 동작은 바꾸지 않는다"와 TN-b의 "외형 연결은 그대로다"가 바뀌었습니다(계획에 표시함). 지금 미리보기 시나리오에 토네이도가 없어 화면은 같습니다.
- `Display/CombatSceneDisplay.cs` 264·397·414(C2-01): 정리 목록 셋(설치 실패·`Clear`·`Release`)에 손으로 적던 기믹 넷(흔적·토네이도·낙석·하붑)을, 생성자에서 한 번 만든 배열 `_gimmickClears`·`_gimmickReleases`로 바꿨습니다. 정리 순서는 같습니다. 새 기믹 표시는 이 배열 둘에 한 번씩 넣으면 됩니다.
- `Display/Field/FieldHighlightLayers.cs` 140(C1-21): 층 표에 "원점 열기" 열이 생겼습니다. 기본값이 없어 모든 줄에 적습니다. 낙석 경고 줄은 `SourceOpening.None`입니다. 원점 쪽 변을 여는 층은 `Origin`(공격·이동 범위 등), 호버 범위는 `HoverOrigin`, 나머지는 `None`입니다. 새 층에서 이 열을 빠뜨리면 컴파일에서 걸립니다.
- `Input/Interaction/AttackTargetingInteraction.cs` 477(C5-02): 거절 안내 창구가 필수 인자가 되어, 하붑 거절의 `_rejections?.`를 `.`으로 바꿨습니다. 동작은 같습니다.
- 경계 커밋(`^83fb2767`, 호진님으로 표시)에 걸린 줄도 고쳤습니다. 실제 작성자를 알 수 없어, 사용자 확인 뒤 넘깁니다(계획 §5.3).

## 17. 2026-11-05 전투 View 평가 후속 5차 — 토네이도 이동 취소·질문 셋 (미전달)

[후속 수정 계획](../plans/2026-11-04-combat-view-evaluation-fixes-plan.md) §9(5차)에서 호진님 줄을 고쳤습니다. 줄 번호는 HEAD `fd8b0d2f` 기준입니다. 컴파일 미확인, 새 시험 없음.

- `Presentation/Pieces/CombatTornadoMovePiece.cs` 48(`4a38c7c9`, G1-02): 이동 조각이 시작한 뒤 취소되면 멈추기만 하던 것을, 하붑 이동 조각처럼 목표 자리로 옮기고 끝내게 바꿨습니다(시작 전 취소는 그대로 아무것도 안 함). 취소는 오류·정리 길에서만 오고, 오류 정지 화면에 폭풍 몸이 칸 사이에 남던 것을 막습니다. 정상 재생은 그대로입니다. 클래스 요약도 고쳤습니다.
- 6차 주석 정리(평가 C12)에서 호진님 줄의 주석만 고쳤습니다(동작 무변경): `CombatFieldEventPorts` 15·`CombatPresentationPlayers` 10(`1ab12e55`, 지운 시험·실행 목록 순서 설명), `CombatSceneEventPresenter` 159(`4a38c7c9`, 특성 주석을 끌어오기 인자 줄에서 다음 줄로), `CombatFactEventHandler` 13(`4a38c7c9`)·`CombatEventHandlerTable` 23(`87642ff5`, '구조물' 설명을 지금 처리 대상에 맞춤).
- 경계 커밋(`^83fb2767`, 호진님으로 표시)에 걸린 줄도 고쳤습니다. 코드는 `FieldRailwayPresenter` Scope 재사용(C9-03), `EntityViewSet` 종류 교체(C10-16), `FieldUnitPresenter.EnsureUnitHud`(C10-17), `CombatDeploymentConfirmation.Request`(C10-09)이고 나머지는 주석입니다. 실제 작성자를 알 수 없어 사용자 확인 뒤 넘깁니다(계획 §9.6).

여쭐 것(평가 중간 문제, 답에 따라 다음 단계를 정합니다)

- **C5-01 아침 자동 전환:** `CombatPhaseTransitionPresenter.TryBegin`을 주석 처리한 커밋이 호진님 `b9c4f3b4`("밤 전환 안되게 테스트로 수정", 10-29)입니다. 되살릴 계획이 있으신지요? 되살리지 않으면 재생 조정·Factory에 남은 경로(약 45줄, 늘 false인 판정 셋)를 걷겠습니다. 파일을 만든 민형님께도 같이 여쭙니다.
- **C1-42 곡사 공통 수명:** 곡사 애니메이션(`CombatMortarAnimation`, 천명님 파일)만 공통 기반 `CombatAttackMotion`(호진님 파일) 밖에서 수명을 따로 돌립니다. 기반에 훅 둘(`BeginResources` 가상 함수, `CancelUnavailable`)을 더하고 곡사를 그 위로 올려도 될지 합의를 부탁드립니다(약 130줄, 구조 단계에서 함).
- **C2-04 클립 이벤트 표:** `CombatClipEventCursor`·`CombatClipPlayback`·`CombatClipPiece`의 Event2·Event3 하드코딩을 이벤트 종류 표 하나로 모으려 합니다(약 50줄, 구조 단계). 줄 대부분이 호진님 줄이라 미리 알립니다. 바꾼 뒤 변경 정리를 넘기겠습니다. **(11-05 정정: 이 질문은 필요 없게 됐습니다.** 얕은 clone의 blame이 경계 커밋을 호진님으로 보였을 뿐이고, GitHub 파일 이력으로 보니 세 파일은 용근 `e0ee626d`(10-19 클립 시스템)가 만든 줄이었습니다. [작은 묶음 셋](../plans/2026-11-05-combat-view-small-bundles-plan.md)(SB2)에서 번호 표 둘로 바꿨고 호진님 줄은 바뀌지 않았습니다.)

## 18. 2026-11-05 전투 View 조정자 나누기 — 행동 버튼 표면·배치 확인창 (미전달)

[조정자 나누기 계획](../plans/2026-11-05-combat-view-coordinator-split-plan.md)(설계 [coordinator-split-design](../specs/2026-11-05-combat-view-coordinator-split-design.md) CS1~CS6)에서 호진님 줄을 고쳤습니다. 줄 번호는 HEAD `dfb49b34` 기준입니다. 새 시험 없음, 시험 변경 없음. Unity 컴파일·재생 문제 없음(사용자, Ua~Ud).

- `CombatViewSystem.cs` 1182-1183(`8339e308`, 행동 버튼 표면 맞춤): `connection.Surfaces.Sync(…)`가 새 입력 분배 `Input/CombatSceneInputDriver.SyncActionButtons`로 옮겼습니다(CS-b). 넘기는 입력 쪽 사실 넷은 이제 연결 입력 묶음의 `ReadActionButtons(facts)`가 새 값 `Input/ActionButtons/CombatActionButtonState`(공격 진입 가능 → 공격 중 → 되돌리기 가능 → 가용성, 읽는 순서 그대로)로 줍니다(CS-a). 표면 쪽 계약 `CombatInputSurfaces.Sync`는 그대로입니다.
- `Input/ActionButtons/CombatDeploymentConfirmation.cs` 42(경계 커밋 `^83fb2767`, Leehojin): `HasUnplacedUnits`가 자기 클래스 안에서만 쓰여 internal → private으로 내렸습니다(CS-a). 실제 작성자를 알 수 없어 사용자 확인 뒤 넘깁니다(평가 후속 계획 Q-N11).
- 경계 커밋(`^83fb2767`, Leehojin으로 표시)에 걸린 줄 둘도 고쳤습니다. 조정자 17줄 using(`InputSystem.Data`)은 쓰는 곳이 없어 지웠고, `Input/CombatInputReceiver.cs` 13 주석은 Layer를 바꾸는 쪽을 "조정자"에서 입력 분배·입력 연결로 고쳤습니다. 실제 작성자를 알 수 없어 사용자 확인 뒤 넘깁니다(Q-N11).

## 19. 2026-11-06 전투 View 선택 인자 나머지 — 입력 표면·공격 Drag·경로 입력·행동 버튼 (미전달)

[선택 인자 나머지 계획](../plans/2026-11-06-combat-view-optional-args-plan.md)(설계 [optional-args-design](../specs/2026-11-06-combat-view-optional-args-design.md) OA1·OA2·OA4·OA5, 평가 C5-02 2단계)에서 호진님 줄을 고쳤습니다. 작성자는 전체 이력으로 셌습니다(설계 §1.7). 줄 번호는 고친 뒤(기준 판 `7279a9d5` 위) 기준입니다. 컴파일 미확인(전투 View 작업 규칙상 MSBuild 없음), 새 시험 없음, 시험 변경은 우리 시험 호출 한 곳뿐입니다.

운영 조립이 늘 넘기는 협력자·값을 필수로 받고, 쓰는 곳의 `?.`·null 갈래를 지웠습니다. 운영에서 보이는 동작은 같습니다.

- `Input/ActionButtons/CombatInputSurfaces.cs`(`8339e308`)
  - 32-38 `BindRoot`: 루트가 null이면 돌아가던 갈래를 `ArgumentNullException`으로 바꿨습니다(설계 P12). 부르는 곳(`CombatViewConnection.BindRoot`)이 먼저 막으므로 동작은 같습니다. 문서 한 줄도 "루트는 필수"로 고쳤습니다.
  - 38·46-48·62-64·74-76: 루트 안의 `BattleChanger?.`·`Pack?.`·`UnitInfo?.`를 `.`로 바꿨습니다. 세 칸은 씬 루트 `ValidateReferences`가 표시 조립 때 막습니다. 루트를 묶기 전·푼 뒤를 건너뛰는 `_root?.`는 그대로입니다.
  - 57-58 `Sync`: `canUseReturn = false`·`availability = null` 기본값을 지웠습니다. 부르는 곳(입력 분배)이 늘 넘깁니다. `availability`의 null은 "Core 응답 전"이라는 뜻이라 그대로 받습니다(56 인자 문서).
- `Input/Interaction/CombatInteraction.cs` 74-75 `attackDrag`(`32654528`): 기본값 `= null`을 지우고 생성자에서 막습니다. 넘겨받기 소유자를 늘 만들므로 `_handover` null 갈래 넷(`TryHandleAttackDragPointer`·`CancelDragOrAttack` 등)을 지웠습니다. 소유자 순서(넘겨받기 → 경로 → 아이템 → 배치 → 공격 → 호버)는 그대로입니다.
- `Input/MovePath/CombatPathInput.cs` 83(`34ee0de4`): 이동 범위 사실을 읽을 때의 `actor.Anchor.HasValue` 재확인을 지웠습니다. 필드 유닛의 칸·방향은 Core 사실(`CharacterCombatFacts` 위치 검사)이 보장하고, `FindActor`는 필드에 있는 살아 있는 아군만 돌려줍니다. 같은 근거로 400의 `actor.Rotation ?? Rotation4.R0`도 `actor.Rotation.Value`가 되었습니다(용근 줄).
- `Input/CombatInputFacts.cs` 38-39 `combatInputRestriction`·`hasCurrentAcceptedEmptyMovementRange`(`34ee0de4`): 기본값을 지웠습니다. 입력 사실 인자 28개가 모두 필수입니다(결정 Q-OA1 A). 사실을 더할 때 읽는 곳(`CombatInputFactsReader`)이 넘기기를 빠뜨리면 컴파일이 막힙니다. 운영 읽기는 이미 28개를 모두 넘깁니다.
- `Input/ActionButtons/CombatActionButtons.cs` 19(생성자)·50(`Read`)의 `canUseReturn` 인자(`34ee0de4`): 기본값 `= false`를 지웠습니다(기본값 글자는 천명님 `d4a6d6fd`). 부르는 곳이 늘 넘깁니다.

## 20. 2026-11-07 전투 View 씬 표시 나누기 — 토네이도·낙석·하붑 자리와 선택 표시 (미전달)

[씬 표시 나누기 계획](../plans/2026-11-07-combat-view-scene-display-split-plan.md)(설계 [scene-display-split-design](../specs/2026-11-07-combat-view-scene-display-split-design.md) DS3·DS5·DS7, 평가 C2-03 2·3단계)에서 호진님 줄을 옮겼습니다. 작성자는 기준 판 `eafd42f5`의 전체 이력으로 셌습니다(계획 T0.2). 줄 번호는 고친 뒤 기준입니다. 컴파일 미확인, 새 시험 없음, 시험 변경 없음. 화면에 보이는 동작은 같습니다.

- 토네이도·낙석·하붑 표시(`9d364b8d`·`678dbafb`·`87642ff5`의 필드·생성 인자·대입)
  - `CombatSceneDisplay`에서 빠지고 새 `Display/Map/CombatFieldDisplay.cs` 생성자(46-49)가 받습니다. 그리기는 지금처럼 진입 보류(`CombatEntryDeferredDisplay`)가 하고, 필드 표시는 정리·해제 묶음 `GimmickClears`·`GimmickReleases`(74-75)만 만듭니다. 차례(흔적 → 토네이도 → 낙석 → 하붑)는 그대로입니다.
  - `CombatSceneDisplayFactory.cs` 191-193: 옛 표시 생성 줄의 `tornado, rockfall, haboob` 낱말은 `new CombatFieldDisplay(…, tornado: tornado, rockfall: rockfall, haboob: haboob, …)` 이름 있는 인자로 옮겼습니다. 표시 생성자는 인자 38개 → 19개(이름 있는 인자)입니다.
  - 기믹 공통 계약(평가 C2-01 2단계)은 다음 묶음 ③에서 이 자리를 바꿉니다(설계 DS5).
- 선택 표시(`88377dec`의 `RefreshInspectionSelection` 주석, `ClearSelection`의 `SetSelectedEntity(EntityId.Empty)` 되살림)
  - 새 `Selection/CombatSelectionDisplay.cs`의 `RefreshInspectionSelection`(85)·`Clear`(95)로 옮겼습니다. 식과 차례는 같습니다. 정보창 Presenter의 선택 바뀜 알림은 이제 `selectionDisplay.RefreshInspectionSelection`에 묶습니다(`CombatSceneDisplayFactory.cs` 218).
- 그대로인 줄: `root.UnitInfo.Configure(…, tables.UnitStatuses)`(`8339e308`), `AttackTargetingInteraction`의 `_modeGeneration`(`32654528`)·`_rejectionRequestNumber`(`f40962f1`).
