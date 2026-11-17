# Lobby Main Selection — Composition Boundaries and View Draft Ownership Handoff (2026-11-16)

Updated: 2026-11-17

Status: Core Phases A through C are complete. Focused EditMode evidence covers both mains through a real skill attack and both same-main and different-main Run resets. On 2026-11-17 the lobby popup, draft owner, departure payload, and Job forwarding were implemented at the user's request (§8). Unity compilation, lobby PlayMode, and visual acceptance remain unverified.

> 2026-11-16 호진: 위 상태 줄은 Core 인계 당시 기록이다. View/LifeCycle 쪽(팝업·선택 원본·출발 키·Job 전달)은 구현을 마쳤다 — §8.

Design: [Run Start Main Selection](../specs/2026-11-15-run-start-main-selection-core-design.md)

Core plan: [Phase-based implementation plan](../plans/2026-11-16-run-start-main-selection-core-implementation-plan.md). View/LifeCycle work remains owned by the receiving worker and is excluded from every Core Phase.

## 1. Intent

Use the existing lobby deck popup to choose the next Run's main (currently Akimbo or Sniper). Keep the base loadout's sub characters, starting Run gold, hand size and initial deck selection unchanged. This does not edit the current Run's deck or swap its main.

## 2. Core surface

Through the existing `InGame.CoreGroup.RunStart` service:

```csharp
RunStartMainOptions ReadMainOptions(int loadoutKey);
RosterAcquireResult StartNewRun(int loadoutKey);
RosterAcquireResult StartNewRun(int loadoutKey, EntityKey mainCharacterKey);
```

The query and both start overloads are available after Core DB initialization and service binding. Consumers can read the options before a Run exists. The Core plan records the query, start, rejection, and battle evidence.

`RunStartMainOptions` contains `LoadoutKey`, `DefaultMainCharacterKey`, and immutable `CandidateKeys` only. View resolves names and icons from ViewGroup by key. Do not read InGameGroup, modify Unit/Loadout SOs, or derive player-main eligibility in the screen.

Candidate lookup has no Run mutation. It is definition eligibility, not a departure preflight. The default comes from the loadout; never assume the first card is the default. Invalid read configuration throws and should reach the existing fault boundary.

Explicit start never falls back to the default for an invalid key. Normal start failures use existing `RosterAcquireResult.FailureReason` and `InitialDeckFailure`; success identifies the main in `Acquired`. The one-argument start remains the compatibility/default path.

The options query reports a missing loadout via `KeyNotFoundException`. Malformed registered definitions, an invalid authored default, and premature calls surface through the existing fault boundary. The start command returns `InvalidDefinition` for invalid explicit selection or a missing loadout, `InvalidRequest` if captured state changes, and `InvalidInitialDeck` with its specific deck reason when applicable. A rejected start preserves the existing Run. A successful start creates a fresh Run even when the submitted key matches the current main.

## 3. Existing integration points

Paths below are under `Assets/ProjectL/Script/Systems/RuntimeSystem/` unless stated otherwise.

| Source | Required future change owned by View/LifeCycle worker |
|---|---|
| `InGameSystem/ViewSystem/Lobby/Deck/LobbyDeckPopupController.cs` | Replace its empty model with Core-provided keys mapped to existing View visuals. Inject the read dependency through normal lobby composition. |
| `InGameSystem/ViewSystem/Lobby/Deck/LobbyDeckPresenter.cs` and the screen | Distinguish detail pinning from the main selected for departure. The existing click is only detail pinning. Exact layout/confirm interaction remains the View worker's design responsibility. |
| Lobby session owner and departure producer (resolve concrete ownership in §3.1) | Own one draft selection shared by popup display and departure. Initialize from Core's default once per lobby session; retain it across popup close/reopen. Do not put the sole draft in a presenter that clears on hide. |
| `BufferSystems/LobbyView/LobbyViewBuffer.cs`, `LobbyDepartureRequest.cs`, `ILobbyDepartureState.cs` | Extend the existing request transport to carry an immutable selected-main key. Preserve the existing no-selection/default submission path and request state machine. |
| `LifeCycleSystem/LifeCycles/Lobby/Plans/LobbyPlan.cs` | Pass the submitted key into the existing RunStartJob. Do not reread the current UI draft when the Job executes. |
| `LifeCycleSystem/LifeCycles/RunStart/RunStartJob.cs` | Supply the explicit key to Core when present; keep the existing default path for other callers, battle admission, outcome publication and installed-roster check. |

The Core delivery does not edit these files. Do not create a parallel start Job or bypass the departure state machine by calling Core from a card click.

### 3.1 Resolve Concrete Ownership in the View Implementation Plan

Before implementing the View connection, the receiving worker must replace the abstract phrase "lobby session owner" with an explicit ownership and dependency map. This is a View planning deliverable; the Core API does not require a particular new class or interface.

Current source anchors, not a prescribed future design:

- `InGameSystem/ViewSystem/Lobby/LobbyViewScope.cs` owns one scene attachment's lifecycle and constructs its `LobbyDepartureFlow`.
- `InGameSystem/ViewSystem/Lobby/LobbyDepartureFlow.cs` submits through `ILobbyViewBufferWriteSink` and owns a reference to its submitted request. Request execution status remains in the Buffer.
- `Assets/ProjectL/Script/Systems/Factory/Runtime/InGame/View/LobbySceneFactory.cs` creates the popup factory list and the scene scope; `Deck/LobbyDeckPopupFactory.cs` constructs the deck popup collaborators.

The receiving plan must name the actual classes/fields/methods for each item below:

| Decision | Required contract |
|---|---|
| Draft owner | Exactly one owner of the unsubmitted main key. State which concrete object owns it and why its lifetime outlasts popup close/reopen. Other components may display it or call its explicit operations; they must not maintain independent authoritative copies. |
| Initialization and loadout source | Name who reads `ReadMainOptions`, where the shared loadout key enters composition, and when the authored default is installed. Do not reset the draft on each popup show. |
| Injection path | Trace creation → popup reader/writer → departure reader using actual constructor parameters or explicit methods. Keep names/icons in ViewGroup and eligibility in Core. Do not use new global state or an event chain to conceal dependencies. |
| Submission boundary | Name the method that snapshots the draft into the existing numbered departure request. Pending requests retain that key; execution reads the request, not the live draft. |
| Lifetime and cleanup | Map popup close/reopen, scene-attachment replacement, leaving/reentering the lobby, cancellation/rejection/retry, fault and disposal to draft/request behavior. Distinguish attachment lifetime from lobby-session lifetime; if reattachment can happen within the same session, specify how the session draft survives without two owners. |

Prefer the existing composition and lifetime owners when they fit these contracts. Normal factory/service-connection changes in the receiving worker's authorized scope are acceptable; moving code to a new factory is not itself evidence of improved independence. Introduce a separate collaborator only if it gives a concrete ownership or change-boundary benefit. Preserve existing admission, fault, request-state and UI registration contracts; a change to those semantics needs design review.

Use three concrete checks in the View plan: changing popup presentation should require no roster/reset internals; changing selection delivery should require only draft/request/Job contracts; changing a shared lifecycle or request contract must include the genuinely affected integration checks. Record modification, understanding and verification scope for each. This handoff leaves the object choice to the View worker, but does not leave ownership implicit at implementation time.

## 4. Lifetime and execution contract

1. Read options after services are ready. Display the authored default unless this lobby session already has a still-valid draft.
2. Choosing a candidate changes only the lobby draft. Closing/reopening the popup retains it; a newly created lobby session starts with the authored default. No persistence across Run returns or application restarts is promised.
3. Departure snapshots the selected key in the existing numbered request. Pending requests keep their original payload even if a later caller supplies a different choice; preserve duplicate submission behavior and inspect the returned request.
4. Selection controls obey the existing pending-departure admission. After successful submission, display the submitted choice as the pending choice. Changing local presentation must not alter the request.
5. Cancellation is allowed only before execution under the current contract. Rejection/cancellation permits correction and a fresh request; keep the draft so the user can retry. Faults use the current fault boundary and recovery policy.
6. The existing Job checks retained battle, starts once, publishes the result, and the existing transition proceeds only on success. The draft is not authority for the installed main: use roster/deck read contracts after success.
7. Preserve current loadout-key ownership in the flow (currently `RunStartJob.DefaultLoadoutKey = 1`). Supply the same key to the candidate read and start. Do not introduce a second hardcoded default in the popup; pass it through composition as necessary.

The exact transport overload/constructor names belong to the View/LifeCycle implementation plan. Required semantics are explicit-key versus omitted-key distinction, immutable pending payload, and unchanged default callers. Explicit `EntityKey.None` must not become an implicit default.

## 5. Ownership and completion boundary

| Core worker | View/LifeCycle worker |
|---|---|
| Options DTO/read API, explicit start overload, shared planner changes, Core verification | Popup rendering/input, draft lifetime, request payload, Job/Plan forwarding and scene wiring |
| Eligibility, roster/deck validation and existing publication path | Presentation, submission admission, cancellation/duplicate/transition handling |
| Same-loadout behavior and rejection evidence | User-visible choice and correct submitted/installed main |

No new authoritative selection Buffer, Core selection setter, active-Run deck-edit extension, or SO mutation is required. Existing UIBase/UISystem registration, Framework input conventions, service-group lookup paths and fault handling continue to apply.

## 6. Acceptance checklist for the receiving worker

- Complete the concrete ownership/injection/lifetime map in §3.1 before View implementation; confirm a single draft owner and explicit request snapshot boundary, including attachment replacement within a session.
- Display both production candidates from Core; missing visual configuration reaches the existing fault boundary.
- Display/retain the selected main independently of detail-card hover/pinning.
- Closing/reopening preserves the session draft; leaving/recreating the lobby resets it to the default under this proposal.
- The candidate read and departure use the same loadout key.
- Pending/duplicate submissions cannot replace the captured key; pre-execution cancel and rejection/retry retain current semantics.
- Akimbo and Sniper departures each install the intended main; both keep identical loadout subs/gold/hand settings.
- Stage/development/default callers still use authored default behavior unless explicitly migrated.
- Selected-main skill, traits and visuals match after battle entry.
- Compilation and PlayMode are user/Unity verification. Do not add View tests or run combat-View MSBuild under this handoff.

## 7. Core delivery evidence

Unity 6000.0.73f1 compiled the scripts and passed all 150 selected EditMode cases. Test-owned inputs entered through the database battle source. Akimbo used Powder Blossom and Sniper used Magic Bullet. Both attacks published a hit and consumed one use. The fresh-Run cases confirmed that Akimbo's prior upgrade and persistent stat changes reset on a second Akimbo start and on a Sniper start. They also confirmed fresh IDs and installation of the authored reordered deck subset.

The focused Test Runner XML and Editor log are at `C:/CodexHome/visualizations/2026/11/15/01a1dfe2-e73c-7022-9645-43288d7bf483/projectl-main-selection-phase-c/phase-c-focused.xml` and `C:/CodexHome/visualizations/2026/11/15/01a1dfe2-e73c-7022-9645-43288d7bf483/projectl-main-selection-phase-c/phase-c-focused.log`. The linked Core plan identifies the executed fixtures and the Phase 0 full-suite baseline.

The receiving worker owns popup rendering and input, the single lobby draft and its injection/lifetime map in §3.1, the immutable departure payload, and forwarding the captured key through the existing Job. View compilation, lobby PlayMode, and visual acceptance are pending.

Locality-review clarification on 2026-11-16: the receiving worker resolves concrete draft ownership using the planning criteria in §3.1. Normal composition changes follow the approved ownership and lifetime contracts.

## 8. View/LifeCycle 대응 (2026-11-16, 호진, 미전달)

받는 쪽 구현이 끝났다. 설계는 [로비 메인 선택 View 연결 설계](../specs/2026-11-16-lobby-main-selection-view-design-Hojin.md), 실행 기록은 [Task 계획 §8](../plans/2026-11-16-lobby-main-selection-view-plan-Hojin.md). 사용자 Unity 확인(Ua·Ub·Uc) 문제 없음, 컴파일·재생은 사용자 확인이며 새 시험은 없다. **Core 파일은 바꾸지 않았고 Core 요청도 없다.**

### 8.1 §3.1 담당 지정 맵

| 결정 | 구체 |
|---|---|
| 선택 원본 | 새 `InGameSystem/ViewSystem/Lobby/LobbyMainSelection` 하나. 로비 씬 연결(= 로비 세션, Lobby 씬 Enter~Exit 콜백)마다 `LobbyViewSystem.AttachFromScene`이 만든다. 팝업을 닫았다 열어도 연결이 들고 있어 남고, 다음 연결은 새 객체라 기본 메인부터다. 같은 씬 번호 재로딩(Exit → Enter)도 새 세션으로 본다 |
| 초기화·로드아웃 출처 | `LobbyViewSystem.PostBindService`가 `CoreGroup.RunStart`의 `ReadMainOptions`만 대리자로 넘긴다(시작 명령은 넘기지 않음). 부르는 시점은 씬 연결(이전 연결을 떼기 전)이고, 로드아웃 키는 `RunStartJob.DefaultLoadoutKey` 하나를 조회·시작이 같이 쓴다. 처음 선택 = `DefaultMainCharacterKey`(후보 첫째를 기본으로 보지 않음) |
| 주입 경로 | `AttachFromScene` → `ILobbySceneFactory.FindPopups(…, selection, …)` → `LobbyDeckPopupFactory`(생성자에서 후보 키 → 이름·아이콘 카드, View DB `UnitVisual`) → `LobbyDeckPresenter`(쓰기 계약 `ILobbyMainSelectionWriter`) / `Create(…, selection, …)` → `LobbyViewScope` → `LobbyDepartureFlow`(읽기 계약 `ILobbyMainSelectionReader`). 잠김은 출발 흐름이 좁은 계약 `ILobbyDepartureHold`로 선택에 한 번 연결. 전역 상태·이벤트 사슬 없음 |
| 제출 경계 | `LobbyDepartureFlow.Submit`이 `ILobbyViewBufferWriteSink.Submit(EntityKey)`로 그 순간의 선택을 담는다. 요청 `LobbyDepartureRequest.MainCharacterKey`(`EntityKey?`, 생성자에서만 설정). 진행 중 요청이 있으면 Buffer가 그 요청을 키째 돌려준다. `LobbyPlan`은 요청의 키를 `RunStartJob`에 넘기고 실행 순간 화면 선택은 읽지 않는다 |
| 수명·정리 | 설계 §5 표. 실행 전 취소·거절 뒤 재출발·오류·연결 교체 모두 기존 요청 단계 표와 오류 경계 그대로 |

### 8.2 바뀐 계약

- `ILobbyViewBufferWriteSink.Submit(EntityKey mainCharacterKey)` 추가. 기존 `Submit()`(키 없음 = 로드아웃 기본)은 그대로. `EntityKey.None`도 값 있음으로 그대로 싣는다(기본값으로 바꾸지 않음).
- `LobbyDepartureRequest.MainCharacterKey`(`EntityKey?`). 읽기 계약 `ILobbyDepartureState`에는 더하지 않았다.
- `RunStartJob` 생성자 셋째 인자 `EntityKey? mainCharacterKey`. 값이 있으면 `StartNewRun(loadout, key)`, 없으면 `StartNewRun(loadout)`. 성공 뒤 설치 확인에서 키가 있을 때만 설치된 메인 = 지정 키를 확인한다. Stage 첫 진입(`StagePlan`)은 `null`, `GameBootstrap`·`TemporaryCoreRuntimeFactory`는 그대로.

### 8.3 §6 체크리스트

| 항목 | 상태 |
|---|---|
| 담당 지정 맵 | 8.1 |
| Core 후보 둘 표시, 외형 누락은 오류 경계 | 표시 확인(사용자). 외형 누락은 연결 시작 때 예외(코드), 재생은 미확인 |
| 선택이 상세 고정·호버와 따로 | 확인(사용자). 카드 클릭은 상세 고정, 선택은 "다음 Run 메인으로 선택" 버튼 |
| 팝업 재개방 유지·로비 재생성 시 기본값 | 확인(사용자) |
| 조회와 출발이 같은 로드아웃 키 | 정적 확인(`RunStartJob.DefaultLoadoutKey` 하나, 받은 응답의 `LoadoutKey`가 다르면 예외) |
| 중복 제출이 키를 못 바꿈·실행 전 취소·거절 뒤 재시도 | 정적 확인(설계 §12.2). 거절 재생은 정상 데이터로 일으킬 조작이 없어 미확인 |
| 아킴보·스나이퍼 각각 의도한 메인 설치, 서브·골드·손패 같음 | 확인(사용자, 로즈로 출발해 전투에 로즈) |
| Stage·개발·기본 호출은 기본 메인 | 확인(사용자 Ua 3)·정적 확인 |
| 고른 메인의 스킬·특성·외형 | 확인(사용자). 단 스나이퍼 외형이 옛 프리팹(`Colored/Playerable/Sniper.prefab`, 모델 배율 1.5)이라 크게 보인다 — 사용자 결정으로 그대로 둠 |
| 컴파일·PlayMode는 사용자 | 그대로. View 시험 추가·전투 View MSBuild 없음 |

### 8.4 Superseded parallel connection (2026-11-17, Core session)

The Core worker's session implemented the same lobby connection in parallel on `feature/Cheon/Core/NewCombatSytem` (`f4e6a352e`). When `develop` was merged into that branch on 2026-11-17, the 19 files both sides changed were resolved to the version above (`1b9d76162`), so that version is the only implementation. The Core session's record in [Core review follow-up design §2](../specs/2026-11-17-core-review-followup-design.md#2-run-start-main-selection--lobby-connection-implemented) describes the replaced version (`LobbyDepartureRequest.SelectedMainCharacterKey`, an optional last `RunStartJob` argument) and is historical only.
