# 전투 View 조정자(`CombatViewSystem`) 현재 상태 정리 (2026-10-23, 코드 변경 없음)

- **C 단계 뒤(2026-10-23) — 이 문서는 C 단계 전(HEAD `4c328d07`) 상태다.** 바뀐 것은 [C 단계 설계](2026-10-23-combat-view-coordinator-c-stage-design.md)·[계획 실행 기록](../plans/2026-10-23-combat-view-coordinator-c-stage-plan.md)이 원본이다. 요점:
  - 진행 사실 셋(제출 정지·완료 표시·포기 요청)의 원본은 `Input/CombatViewProgress`이고, 조정자는 좁은 호스트 계약(`ICombatProgressHost`)만 구현한다.
  - "재생 중"은 골격과 같은 규칙 함수(`CombatProgressFacts.IsUnpresented`), "끝"은 완료 하나다.
  - 제출 정지 깃발은 Plan만 세운다.
  - 조용한 분리는 F1이다.
  - 포기 확인창 길은 걷었다.
  - `ICombatViewService` 멤버 다섯을 뺐다.
  - 줄 번호는 아래와 달라졌다.
- **목적:** C1(진행 사실을 조정자에서 떼어 내기) 설계의 입력. 판단·제안은 하지 않고 지금 코드의 사실만 적는다.
- **기준:** HEAD `4c328d07` — 골격 개선 1차(`44c4cdc2`) 위에 Stage 브랜치 병합(배경 맵을 씬 로드 −195에 세우기 `6bc1a5c4`, 천명님 버퍼 리펙토링 `8644c702`, 민형님 툴 `e706b65a`). 작업 트리에 코드 변경 없음. 줄 번호는 이 시점 기준.
- **대상:** `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/CombatViewSystem.cs`(1,217줄). 협력 객체는 상태 주인이 조정자 밖인 경우만 같이 적는다(`CombatResultPlayback`·`CombatAbandonController`·`StageViewPropPresenter`·`CombatViewBuffer`).
- **약어:** `V/` = `…/ViewSystem/Combat/`, `C/` = `…/LifeCycleSystem/LifeCycles/Combat/`.

## 0. 첫 판(`44c4cdc2` 기준)에서 바뀐 것

배경 맵의 주인이 전투 표시에서 조정자의 **씬 수명**으로 옮겨졌다([배경 설계](2026-10-23-combat-backdrop-before-camera-bind-design.md) BD1~BD4, Q-B1~Q-B3 A).

- 새 씬 Node: Combat Enter **−195** `OnCombatSceneLoaded`(`:706-724`) — 씬 루트를 찾고(`_loadedRoot`) 배경을 세움(`_backdrop.Show(CurrentStageKey)`). 카메라 다시 연결(−190)보다 앞이라 맵 카메라가 그 수집에 들어간다.
- Enter 100 `OnCombatSceneEntered`(`:726-737`)는 −195가 찾아 둔 루트로 `Attach`한다. 씬 오브젝트를 찾는 곳은 계속 하나(−195)다.
- Exit −100과 `Shutdown`은 `Detach` 대신 새 `ReleaseScene`(`:634-640`) = `Detach` → 배경 해제·루트 보관 비움을 부른다. 전투 중간의 `Detach`(전투 사라짐·다른 전투·씬 루트 꺼짐)는 배경을 놓지 않는다(Q-B2 A).
- `Attach`는 씬 로드 때 같은 루트에 세운 배경만 표시 Factory에 넘긴다(`:394`). 표시는 배경을 갖지 않고, 카메라 창구가 그 맵 카메라를 받았다가 표시 해제 때 비운다.
- 스테이지 키 읽기를 `CurrentStageKey`(`:858`) 하나로 모아, 배경과 표시 값이 같은 키를 쓴다.
- 배경 설계 Q-B5: **C1이 −195 Node를 "씬 연결" 묶음에 넣기로 정해 두었다.**
- 진행 사실 다섯과 골격(`C/`)·튜토리얼·Bootstrap·오류 경계는 바뀌지 않았다. Core 정리(`EndBattle`)가 남기는 값도 그대로다(`BufferSystems/Core/BattleTransactions.cs:247-251`, `BufferSystems/Combat/CombatStateBuffer.cs:167-172`, 줄 번호만 바뀜).

## 1. 한눈에

조정자는 `ICombatViewService`(View 서비스)와 `ICombatProgressView`(골격이 보는 진행 계약)를 함께 구현하고, Framework 모듈 여섯(`InitData`·`PostBindService`·`Initialize`·`Tick`·`Shutdown`·`ExportServices`)으로 등록된다.

가진 상태는 수명에 따라 다섯으로 나뉜다.

| 묶음 | 무엇 | 수명 |
|---|---|---|
| **A. 진행 사실** | 연결, 제출 정지, 결과 확인 번호 P, 완료 표시, 포기 요청 | 골격이 매 판단 때 읽음. 연결 수명 안에서 바뀜 |
| **B. 씬 수명** | 씬 로드 때 찾은 루트, 배경 맵 | 씬 로드(−195) ~ 씬 이탈·종료(`ReleaseScene`) |
| **C. 연결 수명** | 표시 소유권, 구독한 씬 루트, 전투 번호, 첫 그리기 끝, 재생 세대, 변경 관측 | 표시 연결(`Attach`, 100) ~ 해제(`Detach`) |
| **D. 입력 객체** | 대기 관문, 입력 기능 묶음, Router, 손패·가방 입구, 호버, 안내 | 첫 그리기 뒤 ~ 입력 정지·해제. 일부는 서비스 수명 |
| **E. 의존** | Buffer 싱크 열, Global 서비스, View DB 묶음, 진입 읽기 | 서비스 연결 때 한 번 |

**씬 Node와 수명 입구**

| 입구 | 순서 | 부르는 것 | 하는 일 |
|---|---|---|---|
| Combat Enter | −195 | `OnCombatSceneLoaded`(`:706`) | 오류면 끝. 남은 배경 먼저 놓기 → 루트 하나 찾기 → 배경 세우기 |
| Combat Enter | 100 | `OnCombatSceneEntered`(`:726`) | 진입 활성·찾아 둔 루트 요구 → `Attach` |
| Combat Exit | −100 | `ReleaseScene`(`:634`) | `Detach` → 배경 해제 |
| Framework 종료 | — | `Shutdown`(`:361`) → `ReleaseScene` | 같음 |
| 씬 루트 `Disabled` | — | `OnCombatSceneDisabled`(`:746`) | Combat 활성이면 F1 보고 → `Detach`(배경은 남음) |
| Tick 안 | — | `TickCore`(`:304-310`) | 전투가 사라지거나 다른 전투면 `Detach`(배경은 남음) |

## 2. 진행 사실 다섯 (A, C1 대상)

골격의 `CombatProgressReader.Read`(`C/CombatProgressReader.cs:92-96`)가 아래 다섯을 사실 묶음에 담고, `CombatPlan.Decide`(`C/Plans/CombatPlan.cs:89-147`)가 읽는다.

| 사실 | 원본 | 처음 값 | 바꾸는 곳 → 값 | Decide에서 |
|---|---|---|---|---|
| **연결** `IsAttached` | 파생: `_display != null && _playback.IsAttached`(`:166`) | 거짓 | 참: `TakeDisplay`(`:478-502`, 첫 그리기 **전**). 거짓: `Detach`(`:587-628`) | 승패 블록 "연결 안 됐으면 기다림"(`:98`) |
| **제출 정지** `IsSubmissionSuspended` | 필드 `_submissionsSuspended`(`:91`) | — | 거짓: `TakeDisplay`(`:495`)뿐. 참: `SuspendSubmissions`(`:893-907`) — ① View Tick의 `SyncSubmissionState`(`:1070-1082`, `IsTerminal` = `Finished ∨ 완료`), ② 골격 `CombatSuspendJob`(`C/Jobs/CombatExitJobs.cs:34`, 승패 블록 1행·포기 블록) | 승패 블록 1행(`:96`), 포기 블록(`:120`) |
| **결과 확인 번호 P** | `CombatViewBuffer.PresentedResultSequence` | Buffer가 유지(전투 사이에도 안 내려감) | 올림: `FinalizeResult`(`:831`)만. 내림 없음 | `HasUnpresentedResult`·`IsPresenting` = P < S(`C/CombatProgressFacts.cs:99·104`) |
| **완료 표시** `CompletionDisplay` | 자동 속성(`:1100`) + `_shownCompletion`(`:95`) | None | None: `TakeDisplay`·`Detach`. Showing: `PresentCompletion`(`:914-931`, 골격 `CombatCompletionJob`이 부름). Confirmed: 결과 화면 확인 콜백(`:922-930`) | 승패 블록 switch(`:100-108`) |
| **포기 요청** `IsAbandonRequested` | `CombatAbandonController.IsRequested`(`V/Input/ActionButtons/CombatAbandonController.cs`) | 거짓 | 거짓: 컨트롤러 `Attach`·`Detach`. 참: `TryRequestAbandon`(OptionUI가 `ICombatAbandonRequestService`로 부름, 확인창 `Confirm`도 같은 함수) | 포기 블록(`:118`) |

**전이에서 눈여겨볼 사실**

- 제출 정지는 **세우는 길이 둘**(View 스스로, 골격 Job)이고 **내리는 길은 연결 시작 하나**다. `Detach`는 내리지 않는다.
- `SuspendSubmissions`는 값 하나만 바꾸지 않는다. 배치 확인창 닫기 → 포기 버튼 맞춤 → 입력 표시 취소 → 입력 해제 → 행동 버튼 숨김까지 한다(`:898-906`). 묶음 D 객체를 건드린다.
- 연결은 첫 그리기 **전**에 참이 된다. 조정자 안의 "첫 그리기 끝"(`_battleShown`, `:530`)과 골격이 보는 "연결됨"은 다른 시점이다.
- 완료 표시는 `PresentCompletion`이 "재생·후처리 중이 아님"(`HasPendingPresentation`, `:1095`)과 "같은 완료"(`CombatCompletionIdentity.IsSame`)를 확인한 뒤 세운다. 확인 콜백은 같은 표시·같은 완료일 때만 Confirmed로 바꾸고, **배경음 프리셋 전환까지** 한다(`:928-929`).
- 포기 요청의 가능 조건은 `CombatInputPolicy.CanRequestAbandon`(전투 있음 ∧ 첫 그리기 끝 ∧ 종료 아님) ∧ 아직 요청 안 함 ∧ 제출 정지 아님이다. 확인창 쪽 입구(`Open`·`Confirm`)는 표시 계약이 빈 구현이라 운영에서 닿지 않는다(`V/Display/CombatSceneDisplay.cs:295-298`). 살아 있는 입구는 OptionUI → `TryRequestAbandon` 하나다.

**결과 화면 유지 판정** `IsHoldingResult`(`:1013-1018`)는 진행 사실 셋을 섞어 쓴다: 전투 없음 ∧ 같은 전투 번호 ∧ 제출 정지 ∧ 같은 완료 ∧ 완료 표시 None 아님. 참이면 Tick이 마지막 화면만 유지한다(`:300`).

## 3. "재생 중"·"끝났나" 판정의 식

| 판정 | 식 | 쓰는 곳 |
|---|---|---|
| 조정자 `IsPresenting(hasBattle)`(`:1021-1024`) | `_playback.IsBusy ∨ (hasBattle ∧ S > P)` | 입력 사실(`ReadFacts`), `HasPendingPresentation`(완료 표시 전 확인), `RouteInput`의 유닛 정보 열기(`:1151-1153`) |
| 골격 `IsPresenting`(`C/CombatProgressFacts.cs:104`) | `P < S` | `Decide` |
| 조정자 종료 `CombatInputPolicy.IsTerminal`(`V/Input/CombatInputPolicy.cs:27-30`) | `Finished ∨ 완료 있음` | 제출 자체 정지(`:1075`), 목표 Layer, 포기 가능, 제출 가능 |
| 골격 종료 | `HasCompletion` | `Decide` 승패 블록 |

두 "재생 중" 식이 같다는 근거는 "재생은 P < S일 때만 시작하고, P를 올리는 것과 재생을 끝내는 것이 같은 호출 안"이라는 코드 논증이다(`C/ICombatProgressView.cs` remarks). 두 "종료" 식이 같다는 근거는 Core가 `Finished`와 완료를 한 거래로 게시한다는 약속이다(`:1073-1075` 주석).

## 4. 씬 수명 상태 (B)

| 필드 | 세움 | 비움 | 읽는 곳 |
|---|---|---|---|
| `_loadedRoot`(`:88`) | `OnCombatSceneLoaded`(`:721`) | `ReleaseBackdrop`(`:646`, `ReleaseScene`과 다음 씬 로드 첫머리 `:713`) | `OnCombatSceneEntered`(없으면 예외), `Attach`의 배경 넘기기 판정(`:394`) |
| `_backdrop`(`:89`, `StageViewPropPresenter`) | `OnCombatSceneLoaded`(`:722-723`, 만들고 `Show(CurrentStageKey)`) | `ReleaseBackdrop`(`:645`, 프랍 파괴·맵 카메라 비움) | `Attach`(같은 루트일 때만 표시 Factory에 넘김) |

- 씬 루트 참조가 **둘**이다. `_loadedRoot`는 씬 수명(배경·연결 대상), `_sceneRoot`(아래 C)는 연결 수명(이벤트 구독·`BattleChanger`·`Pack`)이다.
- 배경 맵 카메라 사실은 배경이 읽어 두고(`MapCameras`), 표시 Factory가 카메라 창구에 넘기며(`AttachMap`), 표시 해제가 비운다(`DetachMap`). 배경을 없애는 것은 그 뒤 `ReleaseBackdrop`이다.

## 5. 연결 수명 상태 (C)

| 필드 | 세움 | 비움·되돌림 | 읽는 곳 |
|---|---|---|---|
| `_display`(`:120`) | `TakeDisplay` 마지막 문장(소유권 이전, `:500`) | `Detach`(`:614`) | 거의 모든 판정의 바탕(`IsSceneActive` `:1045`) |
| `_sceneRoot`(`:86`) | `AttachCreated`(`:440`, 표시가 아직 연결돼 있을 때만) | `Detach` 첫머리(`:593-600`) | 행동 버튼·`BattleChanger`·`Pack` 갱신, 이벤트 구독 |
| `_battleInstance`(`:90`) | `TakeDisplay`(`:499`) | 없음 | Tick의 "다른 전투면 해제"(`:304`), `IsHoldingResult` |
| `_battleShown`(`:94`) | `ShowBattle`에서 입력 객체 생성 뒤(`:530`) | `TakeDisplay`(`:496`)·`Detach`(`:615`) | Tick 진행 여부(`:314`), 입력 사실의 "첫 그리기 끝", 행동 버튼 |
| `_playback`(`CombatResultPlayback`, `:84`) | `Attach`(`TakeDisplay`), `Begin`(`ConsumeResult`) | `CompleteFinalization`(`FinalizeResult`), `Abort`(`Detach`·`StopInput`) | `CanContinue`(`:1064`), `IsPresenting`, `IsFinalizing`(입력 사실), 토큰 확인 |
| `_refresh`(`CombatViewRefresh`, `:96`) | `Observe`(Tick 매번) | `Reset`(`TakeDisplay` `:494`·`Detach` `:622`) | Tick의 새 결과·응답 관측 |
| 입력 Buffer 비우기 | — | `_inputWrite.Clear()`(`TakeDisplay` `:491`·`Detach` `:623`) | — |

**연결 시작과 해제의 초기화가 두 곳에 나뉘어 있다.** `TakeDisplay`는 여덟(입력 Buffer·재생·관측·제출 정지·첫 그리기·완료 둘·전투 번호)을, `Detach`는 그중 여섯(제출 정지·전투 번호 제외)을 되돌린다.

**수명 판정(이름 있는 식)**

| 판정 | 식 |
|---|---|
| `IsSceneActive`(`:1045`) | 오류 아님 ∧ 진입 활성 ∧ `_display` 있음 |
| `CanContinue`(`:1064`) | `IsSceneActive` ∧ 재생 연결 |
| `IsInputReady`(`:1052`) | `IsSceneActive` ∧ 입력 슬롯 연결 |
| `IsCurrent(token)`(`:1089`) | `CanContinue` ∧ 재생 토큰 일치 |
| `CanReceiveDeckInput`(`:1054`) | `IsInputReady` ∧ 포기 확인창 닫힘 ∧ 배치 확인창 닫힘 ∧ 제출 정지 아님 |

## 6. 입력 객체 (D)

| 객체 | 만드는 곳 | 정리하는 곳 | 수명 |
|---|---|---|---|
| `_receiver`(수신 창구, `:139`) | `BindServices`(`:257`) | 해제만(`Unbind`), 객체는 유지 | **서비스** |
| `_deploymentConfirmation`(배치 확인창, `:134`) | `BindServices`(`:253`) | `Close`(`Detach`·`StopInput`·`SuspendSubmissions`) | **서비스** |
| `_abandon`(포기 컨트롤러, `:118`) | 생성자(`:157`) | `Attach`/`Detach`/`Stop` | 시스템 |
| `_deckHover`(`:123`) | `AttachCreated`(`:435`, 첫 그리기 전) | `StopInteraction` | 연결 |
| `_gate`·`_interaction`·`_actionNotices`·`_router`·`_deckPlacement`·`_itemInput`·`_environmentHover` | `CreateInput`(`:542-561`, 첫 그리기 성공 뒤) | `StopInteraction`(`:679-696`)·`Detach`(`:619-621`) | 연결 |

입력 사실 `ReadFacts`(`:970-993`)는 묶음 A·C·D를 모두 읽어 18개 값을 만든다(첫 그리기 끝, 단계, 확인 번호, 후처리 중, 대기 관문, 재생 중, 입력 가능, 공격 모드, 튜토리얼 제한, 빈 이동 범위 등). Router·손패·가방·경로 입력·행동 버튼·포기 컨트롤러가 이 함수를 대리자로 받아 쓴다.

## 7. 책임 목록 (메서드 묶음)

| # | 책임 | 메서드(줄) |
|---|---|---|
| 1 | Framework 수명·서비스 받기 | `InitData` 172, `PostBindService` 180, `BindServices` 213-261, `Initialize` 264, `Shutdown` 361, `ExportServices` 363 |
| 2 | **씬 수명·배경 맵** | `OnCombatSceneLoaded` 706, `ReleaseScene` 634, `ReleaseBackdrop` 642, `CurrentStageKey` 858 |
| 3 | 표시 연결·소유권 | `OnCombatSceneEntered` 726, `Attach` 379, `AttachCreated` 410-445, `ReleaseUnowned` 451, `AttachDisplay` 468, `TakeDisplay` 478-502, `RequireActiveBattle` 574 |
| 4 | 첫 그리기·입력 객체 조립 | `ContinueAttach` 505, `ShowBattle` 522, `CreateInput` 542 |
| 5 | 해제·오류 정지 | `Detach` 587-628, `OnCombatSceneDisabled` 746, `StopInput` 654, `StopInteraction` 679, `HideActions` 671, `ReleaseInput*` 933-958, `AbortPlayback` 960 |
| 6 | 결과 소비·후처리 순서 | `Tick` 280, `TickCore` 293-358, `ConsumeResult` 765, `FinalizeResult` 800-836 |
| 7 | 표시 값 읽기 | `ReadDisplayValues` 843(타임라인 요청 길이 상수 80-81) |
| 8 | 입력 Layer 맞춤 | `SyncInputLayer` 864 |
| 9 | 입력 사실 조립 | `ReadFacts` 970, `ReadCurrentPreviewAnswer` 1027, `IsPresenting` 1021 |
| 10 | 진행 계약(골격 쪽) | `SuspendSubmissions` 893, `PresentCompletion` 914, `Progress`·`IsSubmissionSuspended`·`CompletionDisplay`·`IsAbandonRequested` 1098-1103, `IsHoldingResult` 1013, `HasPendingPresentation` 1095 |
| 11 | 제출 자체 정지 | `SyncSubmissionState` 1070 |
| 12 | 입력 입구 앞단 | `RouteInput` 1142-1162(유닛 정보·환경 호버·행동 버튼 맞춤), `FocusLost`·`CaptureLost`·`RequestReturn` 1119-1121, `DeckHover`·`DeckPlacement`·`ItemInput` 124-127 |
| 13 | 화면 파생(행동 버튼·`BattleChanger`·`Pack`·환경 호버) | `SyncActionButtons` 1180, `SyncEnvironmentHover` 1165, `CancelInputPresentation` 1127 |
| 14 | 배치 확인창 연결 | `CanEnterFromDeployment` 1194, `SubmitDeploymentEntry` 1201, `CancelDeploymentInputs` 1207, `ShowMainPlacementNotice` 1210 |
| 15 | 포기·튜토리얼·소리 연결 | `TryRequestAbandon` 1106, `ReportBlockedInput` 996, 완료 확인 때 배경음(`:928-929`) |

## 8. 바깥 사용처

| 사용처 | 무엇을 | 경로 |
|---|---|---|
| 골격 사실 읽기 | 연결·제출 정지·완료 표시·포기 요청 | `C/CombatProgressReader.cs:49`(`ViewGroup.Combat.Progress`), `:93-96` |
| 골격 걸음 재료 | 진행 계약(Job에 넘김) | `C/Plans/CombatStepContext.cs:75` |
| 골격 Job | `SuspendSubmissions`, `PresentCompletion` | `C/Jobs/CombatExitJobs.cs:34·69` |
| 튜토리얼 | P(조정자를 거치지 않고 Buffer에서) | `LifeCycles/Tutorial/TutorialFactsReader.cs:24` |
| Global UI | `DeckHover`·`DeckPlacement`(`DeckCardList`), `ItemInput`(`PackUI`), `TryRequestAbandon`(`OptionUI`) | `GlobalSystem/UISystem/Screens/…` |
| Global UI(밀어 줌) | 입력 사실을 `BattleChangerUI.Refresh`·`PackUI.Refresh`로 넘김 | `SyncActionButtons` `:1188-1189` |
| 로딩 | 씬 Node 셋(−195·100·Exit −100). 전체 순서의 원본은 [씬 연결 안내 §1.1](../viewDocs/runtime-scene-binding-guide.md)(2026-10-23 바뀜: 숫자 원본은 `SceneNodeOrder`) | `Initialize` `:264` |
| Factory | `StopInput`을 오류 정리에 연결, 배경·표시 생성(`CreateBackdrop`·`Create(…, backdrop)`) | `Factory/Runtime/InGame/View/InGameViewSystemFactory.cs:74`, `…/CombatSceneDisplayFactory.cs` |
| 씬 | `CombatSceneRoot.FocusLost`·`Disabled` 구독, `BattleChanger.BindInput` | `AttachCreated` `:442-444` |

- `ICombatViewService`의 `Attach`·`Detach`·`FocusLost`·`CaptureLost`·`RequestReturn`은 **바깥 운영 호출이 없다.** 안에서(로딩 노드·씬 이벤트·Tick) 부르거나 시험이 부른다.
- 진행 계약 멤버(`SuspendSubmissions`·`PresentCompletion`·`CompletionDisplay`·`IsAbandonRequested`·`IsSubmissionSuspended`)를 쓰는 시험은 9파일이다: `CombatViewSystemTests`·`CombatEntryIntegrationTests`·`CombatProgressJobTests`·`CombatPlanDecideTests`·`CombatProgressReaderTests`·`CombatProgressTestDriver`·`LobbyDepartureIntegrationTests`·`StageMapNodeMovementBootSmokeTests`(·`.Victory`). 우리·천명님·호진님·현강님 줄이 섞여 있다.

## 9. C1 설계에 넘길 관찰 (사실만)

1. 진행 사실 다섯 중 셋(제출 정지·완료 표시·포기 요청)은 조정자 상태, 하나(P)는 Buffer, 하나(연결)는 표시·재생에서 파생된다.
2. 제출 정지는 세우는 길이 둘이고 내리는 길이 하나다. 세울 때의 부수효과가 묶음 D(입력·버튼·확인창)를 건드린다.
3. "재생 중"은 조정자 식(재생 중 ∨ S > P)과 골격 식(P < S)이 따로 있다. "종료"도 조정자 식(`Finished ∨ 완료`)과 골격 식(완료)이 따로 있다. 같다는 근거는 각각 코드 논증과 Core 약속이다.
4. 완료 표시는 상태 둘(`CompletionDisplay`·`_shownCompletion`)과 판정 하나(`IsHoldingResult`)로 이뤄지고, 확인 콜백이 배경음까지 바꾼다.
5. 포기 요청의 주인은 이미 따로 있다(`CombatAbandonController`). 그 안의 확인창 경로는 운영에서 닿지 않는다.
6. 연결 시작(`TakeDisplay`)과 해제(`Detach`)의 초기화 목록이 다르다(제출 정지·전투 번호는 시작에서만).
7. 입력 객체 중 수신 창구·배치 확인창은 서비스 수명이고, 나머지는 연결 수명이다.
8. 골격의 "연결됨"(첫 그리기 전 참)과 조정자의 "첫 그리기 끝"은 다른 시점이다.
9. 진행 계약 자체(`ICombatProgressView`)는 이미 따로 있다(Phase 4). C1이 계약 모양을 그대로 두면 시험 9파일의 가짜·호출은 바뀌지 않는다.
10. **수명이 이제 셋이다(씬·연결·서비스).** 해제 입구도 둘이다: `Detach`(전투 중간에도 — 전투 사라짐·다른 전투·씬 루트 꺼짐, 배경은 남김)와 `ReleaseScene`(씬 이탈·종료, 배경까지).
11. 씬 루트 참조가 둘이다(`_loadedRoot` 씬 수명, `_sceneRoot` 연결 수명·구독).
12. 배경 설계 Q-B5로 C1은 −195 Node를 "씬 연결" 묶음에 넣기로 정해져 있다.
