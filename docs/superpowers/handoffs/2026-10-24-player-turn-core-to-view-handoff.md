# Player turn Core contract and View adaptation handoff

Date: 2026-10-24  
From: Core implementation work in the isolated Phase B worktree  
To: View owner, with Core/Buffer and Flow test responsibilities retained by their respective owners

## 1. Delivery status and checkout

This is a review and continuation handoff. Core contracts have changed, and a limited View production adaptation is present. It is **not a declaration that Phase B, integrated gameplay, or View acceptance has passed**. The View changes below are provisional implementation choices for the View owner to review, retain, or replace within the approved contracts.

- Current review checkout: `D:/My_ProjectWork/Github_desktop/Project_L`. The prepared delivery was transferred here as uncommitted changes at the user's subsequent request.
- Preserved source checkout: `C:/CodexHome/worktrees/player-turn-phase-b/Project_L`.
- Delivery base: `d73fb5c6192cb2ea7e0bb9bd15b0b9509f84658c`; the delivery is in uncommitted files, not in that commit alone. Include new files and paired `.meta` files when reviewing or eventually transferring it.
- User checkout: `D:/My_ProjectWork/Github_desktop/Project_L`, branch `feature/Cheon/Core/NewCombatSytem`, transfer baseline HEAD `42d3263acc7164c8da06c930df154f3ea7741ebb`.
- The target was clean before transfer and already contained committed Phase A files and newer Lobby/Flow integration. The transfer preserves those newer changes and the current design's D7 systems-only clarification; it does not copy the entire old checkout over the target.
- The initial request created this handoff only; the user subsequently authorized transferring the prepared code and documents. No commit/push or message to the View owner was made. The preserved source and target transfer are recorded in implementation plan section 8.5.

The subsequent user-authorized test migration in the target checkout restored full native Unity project compilation. The latest Core run passed **3,497 / 3,497 tests, 0 failed, 0 skipped**, with Unity exit code 0 (`core-9.xml` / `core-9.log`, 2026-10-24 10:54:50-10:55:00 UTC). Framework/View test sources compile, but those suites and manual scene/input/playback checks have **not** been executed. These results establish the compile and Core test gates, not integrated Phase B or View acceptance. Section 4 records the test disposition and section 5 separates remaining verification ownership.

The [design](../specs/2026-10-23-player-turn-shared-actioncost-design.md) defines the approved rules. The [implementation plan](../plans/2026-10-24-player-turn-shared-actioncost-implementation-plan.md), particularly sections 8.5 and 8.6, records execution history. Section 8.3 supersedes earlier statements that no View files were changed. This handoff is the current recipient-facing inventory; it does not replace rule ownership or historical verification records.

## 2. Core contracts the View must consume

Path abbreviations in this document are relative to either checkout unless a location is explicitly stated:

| Prefix | Repository path |
|---|---|
| `S/` | `Assets/ProjectL/Script/Systems/` |
| `C/` | `S/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/` |
| `B/` | `S/RuntimeSystem/BufferSystems/` |
| `V/` | `S/RuntimeSystem/InGameSystem/ViewSystem/` |
| `U/` | `S/RuntimeSystem/GlobalSystem/UISystem/` |
| `T/` | `Assets/Tests/EditMode/Framework/` |

### 2.1 Authority and rules

Core calculates immutable proposals and rule facts. Buffer owns the live state, source validation, atomic installation, publication, and revisions. View selects an actor and displays published facts; selection does not spend/refill AP or grant another action. Continue using the existing command, preview, result acknowledgement, Flow, and Job routes.

| Previous concept | Delivered contract and behavior |
|---|---|
| Per-unit AP and active unit turn | Shared `CombatStateData.CommandState.PlayerBudget.Current/Maximum`; each character exposes `ActionUse` from `CharacterCombatFacts` through `CharacterData`. The values are `Deferred`, `Available`, `Moved`, and `Attacked`. |
| Initiative scheduler | `CommandState.Turn.ControllerPhase` and `ControllerTurnSequence`. Phases are `None`, `StartingPlayer`, `Player`, `StartingEnemy`, and `Enemy`. There is no authoritative active player unit to select on behalf of the View. |
| Unit movement followed by attack | Each unit can perform one move **or** one attack during its owner's turn. Reactions and hit chains remain part of their originating action; they do not grant a second ordinary action. |
| Individual AP refill | A real player controller start resets the shared budget to 10. Selection and accepted actions do not refill it. AP reaching zero does not automatically end the player turn. |
| Authored movement and action prices | Current category rules: Main/Heavy allowance 3, Rider 4, Cannon 2; player move cost 2; player Cannon attack cost 5, other attacks 3. Boss movement allowance is unresolved (`null`), so its movement is unavailable; this does not prohibit a legal attack. These are current Core rules, not newly editable SO settings. View reads published values instead of reproducing this table as logic. |
| Enemy AP | Enemies are unbudgeted and act at most once per enemy phase. Core chooses the next actionable enemy, attacks a legal current target even if damage is zero, otherwise attempts movement, then completes the phase when no actionable enemy remains. View does not choose enemy actors. |
| Night clock and unit delay | `Turn.CompletedPlayerTurnsThisNight` with eight player turns per night. The eighth player end is followed by the eighth enemy response before night cleanup/recall/day. Count 8 during the enemy phase is not proof that the night has ended. |

Source entry points: `C/Contracts/Combat/{CombatCommandState,CombatTurnFacts,CombatActionAvailability}.cs`, `C/Contracts/Common/PlayerTurnRules.cs`, `C/Domain/Categories/CategoryActionRules.cs`, and `B/Combat/CombatStateData.cs`.

### 2.2 Commands, previews, and stale input

The request types are defined in `B/CombatView/CombatViewCommand.cs` and `B/CombatView/CombatViewPreview.cs`.

| Entry point | Consumer requirement |
|---|---|
| `CombatViewCommandData.EndPlayerTurn(long expectedBattleInstanceId, long expectedControllerTurnSequence)` | Actor-free. Capture the actual battle/controller identity when preparing the request. Keep the existing pending-command and result-presentation gates. |
| `CombatViewCommandData.Attack(EntityId attacker, AttackAim aim, long expectedBattleInstanceId, long expectedControllerTurnSequence)` | Preserve the selected actor and captured identity throughout targeting and submission. Do not silently retarget an old request to the current turn. |
| `CombatViewCommandData.Move(EntityId unit, MovementPath path, MovementPreviewContext context)` | Display and submit the complete normalized preview route with its corresponding context. `response.Move.Value.PathMovement.RequestedPath` and `response.MovementContext.Path` contain the chosen route. Shared funds, eligibility, turn identity, and relevant world facts participate in validation. |
| `CombatViewPreviewData.ActionAvailability(EntityId unit)` | Consume `CombatPreviewResult.ActionAvailability`: actor/battle/turn identity, `ActionUse`, nullable `PlayerBudget`/`MovementAllowance`, fixed prices, and typed movement/attack failures. `CanMove` and `CanAttack` derive from those failures. |
| `UnitAttackRange` | Informational range remains useful for hovering; it is not authorization to perform an action. |

`WrongController`, `ActionAlreadyUsed`, `ActionDeferred`, and `StaleTurn` distinguish admission failures. Insufficient shared funds remain `ActionCostInsufficient`; stale movement source/context rejects as `StaleContext`. Consume rejection through the existing acknowledgement route and refresh the relevant preview. Do not rewrite the identity of an already captured request to make it appear fresh.

`CombatResult` publishes `BattleInstanceId`, `ControllerTurnSequence`, the initiating actor, and optional `ActionCost`. `ActionCostSettlementSnapshot` contains `ActorId`, `FundingKind`, `Spent`, nullable `Before`/`Final`, and `BeforeActionUse`/`FinalActionUse`. Enemy action settlements have no player balance and spend zero.

Example: at player start, budget is 10 and A/B are Available. A's accepted move spends 2 and publishes A=Moved, budget=8. Selecting B changes no Core state. B's accepted ordinary non-Cannon attack spends 3 and publishes B=Attacked, budget=5. A cannot then attack during the same owner turn. A movement preview saved before another relevant state change must be refreshed if Core rejects its context.

### 2.3 Removed APIs and deferred behavior

- `EndUnitTurn` is replaced by `EndPlayerTurn`. Unit `TurnStarted`/`TurnEnded` scheduling is replaced by `ControllerTurnStarted` (36), `PlayerTurnEnded` (37), and `EnemyPhaseEnded` (38). `CombatEntered` and `NightEnded` remain. Use the actual result/event actor for attack presentation.
- `ICombatTimelineQuery`, timeline forecast snapshots, unit delay/night-clock records, and per-unit AP/refund/checkpoint authority are removed. There is no fake empty timeline replacement. `StageSnapshot.CombatTurn` now carries `CombatTurnFacts`; there is no separate `StageNightSnapshot`.
- `ReturnToCheckpoint`, `CheckpointReturnPreview/Result`, and movement `CanReturn` are removed. Draft cancellation and deployment `ReturnUnit` still exist and must not be removed with checkpoint return.
- Unit and stage constructors no longer accept the removed unit AP, authored movement price/range, delay, or night-length fields. `TimelineSeed` is retained for random streams; its name does not make it a scheduling API. Priority data may remain without an active initiative order.
- Rope equipment/shop admission and unit-turn-dependent runtime features are deferred: Focus, Regeneration, Necromancy, MoveAttackDiscount, timed thorn/soul behavior, and the retired railway/tornado/weak-ground/flag-priority paths. Rider refund state is absent. Retained pure definitions or authored assets do not imply that these runtime features are enabled.

  2026-10-27 railway-only follow-up: the user approved reactivation at both controller ends, retaining 5/5 and the count across nights, before recall. Core calculation and turn-end wiring now follow the [railway design](../specs/2026-10-27-railway-controller-turn-reactivation-design.md); latest compilation/runtime verification is deferred by user request. This does not reactivate the other deferred features in this historical list.
- Consume published trait availability/reasons and partial-unavailability information for retained attack portions such as Intimidation/Frenzy. Existing thorn contact damage is retained without starting the deferred defense timer. Preserve unrelated immediate damage/reactions and the new night cleanup rules.

Do not restore retired Core APIs, duplicate a budget/controller in View, or read InGame recipes to calculate replacements. Report a missing required fact to the Core owner.

## 3. View code already changed by this worker

These are production adaptations, not an approved final View design. The inventory covers 32 changed paths: 26 modified C# files plus three deleted C# files and their three deleted metas. No scene/prefab, asmdef, Framework implementation, or LifeCycle implementation was changed by this continuation. The table includes the one View factory assembly-site change.

| Changed files | Actual change and review responsibility |
|---|---|
| `V/Combat/CombatViewSystem.cs`; `V/Combat/CombatDisplayValues.cs` | Removed timeline query/values. `ReadFacts` now publishes battle/controller identity and reads View selection. Confirm result refresh and detach lifetimes. |
| `V/Combat/Display/CombatSceneDisplay.cs`; `Display/Hud/CombatUnitInfo.cs` under `V/Combat/` | Reused `_selected` for the acting allied unit; exposed `ICombatUnitInspection.SelectedActorId`; field highlight follows selection. Removed fallback to Core active actor and per-unit-turn selection clearing. Review selection persistence, invalid/dead/removed actors, and scene lifetime. |
| `V/Combat/Input/CombatInputFacts.cs`; `Input/CombatInputPolicy.cs`; `Input/PendingInputCommand.cs` under `V/Combat/` | Added identity fields and migrated end-turn handling. Internal `ActiveActorId` now means the View-selected actor, not a Core active turn owner. End-player-turn no longer requires a selected actor. Review stale comments/names and tutorial restrictions. |
| `V/Combat/Input/CombatInputRouter.cs`; `Input/ActionButtons/CombatActionButtons.cs`; `Input/ActionButtons/ICombatActionDisplay.cs` under `V/Combat/` | Router captures identity for actor-free end. Retired Return input slot is inert. Button/display contract uses player end. Validate pending input and cancellation order. |
| `V/Combat/Input/Interaction/AttackTargetingInteraction.cs` | Captures battle/controller identity when targeting starts and validates it before submission. Review selection changes and delayed preview responses. |
| `V/Combat/Input/MovePath/CombatPathInput.cs`; `Input/Contracts/ICombatMovementFeedback.cs` under `V/Combat/`; `S/Factory/Runtime/InGame/View/CombatSceneInputFactory.cs` | Removed checkpoint-return dependencies/calls; factory supplies a selected-actor reader to movement input. Ordinary preview, draft cancellation, and deployment return remain. Review normalized route/context pairing and cleanup. |
| Deleted `V/Combat/Input/MovePath/CombatReturnRequest.cs`, `CombatReturnEstimateTracker.cs` in the same folder, and `V/Combat/Input/Contracts/ICombatReturnFeedback.cs`, including each `.meta` | Independent checkpoint-return code removed. Tests referencing these files still require owner disposition. |
| `V/Combat/Display/Hud/CombatActionPanel.cs`; `V/Combat/Display/Unit/FieldUnitPresenter.cs` | Return button hidden/noninteractive; timeline/order badge connection removed and `SetTurnOrder(false, null)` used. Shared HUDs and serialized input IDs were preserved. |
| `U/Screens/Stage/UnitHUD.cs`; `U/Screens/Stage/NightCheckerUI.cs`; `U/Screens/Global/UnitCardView.cs` | Hid per-unit AP visuals, displayed completed-player-turn night progress, and replaced MAX COST text with derived MOVE COST. Shared-budget HUD design was not implemented. Review the eighth-response display and misleading retained priority text. |
| `V/Combat/Playback/Handlers/CombatEventHandler.cs`; `CombatTurnEventHandler.cs` in the same folder; `V/Combat/Playback/Support/CombatEnemyPresentationPacing.cs`; `CombatEventSounds.cs` in the same folder | Migrated side-boundary pacing and removed old unit-turn pickup/reload connections. Timing/audio acceptance is pending. |
| `V/Combat/Presentation/Attack/CombatAttackPlayback.cs` | `EndSource` starts Ballista reload on actual attack animation completion before ending the animation gate; direct completion uses that route. Review repeated callbacks, generations, cancel/failure/release, and final result acknowledgement. This is a behavioral integration change, not only a compile fix. |
| `V/Dev/DevBattleInitializationSource.cs`; `V/Tutorial/TutorialViewFocusLocator.cs` | Adapted fixture constructors and changed the old active-actor tutorial lookup to the field Main. Review Live/RecordInputOnly entry and tutorial selection behavior. |

### Work still needed in View

1. Review the provisional selection and playback choices above before treating them as final. No manual evidence currently proves them.
2. Wire the delivered `ActionAvailability` response into action enablement and explanatory UI. The current View production adaptation has **no `ActionAvailability` consumer**. Core rejects invalid commands, but that alone does not complete the UI.
3. Present the shared balance, unit action-use status, current controller, and manual player end using published facts. Distinguish completed player turn 8 from completed enemy response 8.
4. Review the still-shared StageInput route (`MovementDraft`, `CombatInputController`, receiver/slot, key binding names/preferences). Retired Return is currently inert; preserve unrelated bindings, draft cancellation, and deployment return. Review cards, tooltips, equipment/shop, and tutorials for deferred-feature claims.
5. Review and execute the migrated View-owned tests against accepted behavior. Sources now compile; this worker did not change View production behavior during test migration or execute the View suite. In particular, selection reset tied to the removed active-unit turn was retired; define the new selection lifetime with the View owner. Preserve input/pending-result gates, resource cleanup, display reuse, and cancellation coverage.

## 4. Test migration and resolved compile barrier

The previous 29 declaration diagnostics were from the preserved isolated source. They are historical and **no longer current blockers**. Fixing declarations exposed additional old constructor and behavioral assumptions; the user authorized deleting retired-feature-only tests and minimally adapting retained tests in the target checkout, without implementing new View behavior.

This continuation changes 217 test paths relative to its pre-migration snapshot (44 Core paths and 173 Framework paths, including deleted metas; line-ending-only differences excluded). Nine dedicated test classes and their metas were removed:

| Retired test class | Reason |
|---|---|
| `CombatItemRopeTests` (Core) | Active Rope equipment/effect is deferred. Deferred admission remains covered. |
| `CharacterActionCostAuthoringTests`, `CharacterActionCostPublicationTests` | Removed authored/per-unit AP authority. Shared-budget and action-use contracts have dedicated Core coverage. |
| `CombatTimelineQueryTests`, `FrenzyTimelineQueryTests` | Removed timeline query and initiative forecast. |
| `CombatReturnRequestTests`, `CombatReturnEstimateTrackerTests` | Removed checkpoint-return components. Ordinary movement, draft cancellation, and deployment return remain. |
| `BossCombatIntegrationTests`, `BossNightPublicationTests` | These classes exclusively depended on the deferred automatic Necromancy cadence. |

Mixed test classes retain damage, wall/target geometry, movement/contact effects, rewards, state immutability, input cleanup, and other applicable checks. Only retired forecast/checkpoint/refund/timed-producer cases were removed. Ordinary repeat actions start a real new owner turn; reaction tests use their actual event/result contract. Enemy pursuit tests allow multiple turns at the category movement allowance. Deferred priority/defense timers and thorn/soul producers are no longer expected to activate. No test was newly ignored/explicit, and no assembly or dependency was excluded.

Three Core production files required narrow regression corrections exposed by retained tests: `CombatAttackQueries.cs` preserves geometric range results when funds are insufficient; `CombatAttackCalculator.cs` preserves spatial blocked-contact evidence before returning insufficient funds; `AttackFailureReason.cs` preserves existing numeric values after removed enum entries. Action availability and execution still enforce funding. These are additional Core corrections, not View implementation changes.

The local evidence directory is `C:/CodexHome/visualizations/2026/10/23/01a16992-5a3f-7892-a093-dfca1a3cc331/test-contract-migration/`. It contains result XML/logs, the pre-migration snapshot, change reasons, `retired-core-methods.json`, and `final-audit.json` (including removed/renamed method inventory and exact additional production paths). It is outside the repository; the status and class disposition above are sufficient for recipient review without that directory.

`ProjectL.Tests.Core` references only Core Contracts/Domain/Application and NUnit (`noEngineReferences: true`). Its successful execution did not require running View tests; all project sources had to compile first.

## 5. Verification and defect ownership

| Scope | Owner and required evidence | Current status |
|---|---|---|
| Full project compilation | Each owner repairs their contract consumers/fixtures; integration coordinator records a native Unity compile without errors. Do not hide assemblies or add compatibility stubs. | Passed in the target checkout during the final native Core test run; no compiler errors. |
| Core rules and migrated Core tests | **Core owner retains responsibility** for running all `ProjectL.Tests.Core` tests after compilation is restored and repairing Core failures. Include shared spending, eligibility, stale identity/context, enemy decisions, night boundaries, damage/rewards, and deferred admission. | Passed: 3,497 / 3,497; 0 failed, 0 skipped (`core-9.xml`). |
| Buffer/service publication | **Core/Buffer owner retains responsibility**, even for files in `T/`. Verify atomic source/spend/start installation, rejected commands, handled-command/result sequence, preview publication, and no partial installation. | Required integration execution pending. |
| View behavior and tests | View owner reviews the provisional changes and verifies selection, action availability, manual end, movement/attack previews, rejection display, pending gates, HUD, and reused displays. | Production and test sources compiled; Framework/View suite execution and manual checks pending. |
| Playback completion | View owner verifies actual attacker identity, Ballista reload, generation/cancellation/failure cleanup, acknowledgement, and terminal-result presentation. Core owner supports any inconsistent result/event payloads. | Not exercised. |
| Flow and full combat lifecycle | Flow owner and Core/Buffer owner jointly verify two real consecutive nights, eighth enemy response before cleanup, victory/defeat/abandon, rewards, and the existing Stage/Run exit policy. View owner verifies presentation completion does not block these paths. | Pending. Fresh per-count fixtures are not evidence of two consecutive live nights. |
| Authored data | Core/data owner runs `DataBaseAssetTests` and relevant content checks after compilation. Preserve seeds, GUIDs, and references. | Prior source audit checked field-removal scope; Unity asset tests not run. |

The latest run used Unity **6000.0.73f1** through approved AppData access with `-batchmode -nographics -projectPath D:/My_ProjectWork/Github_desktop/Project_L -runTests -testPlatform EditMode -assemblyNames ProjectL.Tests.Core -testResults <evidence>/core-9.xml -logFile <evidence>/core-9.log`. Unity exited 0. The initial executable Core run was 3,541 total / 117 failures; retiring obsolete cases reduced the final count by 44, while retained failures were repaired under current contracts. The initial run is migration evidence, not a green pre-change regression baseline.

No Framework/View tests were executed. Buffer/service atomic publication, Flow, two consecutive live nights, deferred-content UI claims, reload/cancellation, and asset cross-validation remain required owner checks. The removed immediate night-clock command-service recall case does not establish the new eighth-enemy-response publication boundary; add/execute that integration case under the current lifecycle. Do not infer these results from the Core pass. No Unity upgrade, cache deletion, compatibility stub, standalone harness, or asmdef exclusion was used.

## 6. Review order and integration boundary

1. View owner reviews section 3 and identifies which provisional selection, input, and playback choices to retain. Core owner answers contract questions without adding another source of state.
2. Review the section 4 test disposition with the actual owners. Agree on shared fixture ownership before concurrent editing; preserve still-relevant checks.
3. Combined compilation and Core tests now pass. Run the separately authorized View/Buffer/Flow integration suites and asset tests. Return failures with input identity, relevant before/after facts, actual/expected behavior, and log/XML evidence to the responsible owner.
4. Verify manual input/HUD/playback and two consecutive nights. Record what passed separately from what remains untested.
5. The prepared changes have now been transferred under separate user authorization. Review the target diff and complete the outstanding verification before claiming integration acceptance. Preserve concurrent work; commits and push remain user-owned.

The intended locality improvement is that View action enablement consumes one availability contract, Core category rules own prices, and Buffer owns state installation. The current implementation has not yet completed the View availability consumer, so reduced UI coupling is an intended result rather than demonstrated acceptance. Changes still require the relevant shared-world, result, reward, and playback integration checks. No new Framework service, duplicate state store, UI hierarchy redesign, deferred-feature reactivation, or Phase C work is part of this handoff.

The target design's D7 clarification is preserved: this migration changes systems and safely disconnects incompatible capabilities. The planner may delete, replace, or redesign traits/items/gimmicks later. No legacy content redesign or speculative lifetime mapping is required for system delivery; required system and integration verification remains mandatory.
