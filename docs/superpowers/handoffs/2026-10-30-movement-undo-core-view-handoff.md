# Movement Undo Core-to-View Handoff

**Status:** U3 Core/API/Buffer delivery and local handoff are complete. Final native gates retain only the reproduced StageKey2101 baseline failure; one independent final review found no Critical/Important/Minor issue. This document is prepared locally, not externally delivered or accepted by View. View/UI/scene/presentation implementation and manual acceptance remain external.

**Source:** U3 verified the working snapshot on `feature/Cheon/Core/NewCombatSytem` above base `f5fe69cac0c3548e1c7f60f83460f0c0f2839736`, including U1/U2 implementation and U3 acceptance tests. Exact verification/source hashes belong to [the implementation plan](../plans/2026-10-30-movement-undo-restoration-plan.md), sections 7.4 and 7.5. The user later authorized commit/push under `[천명] 작업한 내용` (section 7.6). Use the revision containing this handoff and those implementation files; the verification base alone does not contain this API. Repository publication is separate from direct View handoff and acceptance.

## 1. Entry points and immutable facts

All paths below are relative to `Assets/ProjectL/Script/Systems/`. View submits through `RuntimeSystem/BufferSystems/CombatView/CombatViewBuffer.cs` and reads `IBufferQuery.CombatStateReadSink`, `CombatResultReadSink`, `CombatResultHistoryReadSink` and `CombatViewReadSink`. Do not call the pure calculator, store mutators or command-processing service from a UI handler. Existing Flow/Job processing remains the owner of command execution. The submission example assumes the existing query/selection/freshness gates have accepted answer before submitting its token.

| Surface | Contract |
|---|---|
| Query submission | `CombatViewPreviewData.ActionAvailability(EntityId)` through the existing preview slot. |
| Query result | `CombatPreviewResult.ActionAvailability`: enclosing `BattleInstanceId`, `ControllerTurnSequence`, `ActorId`, `SourceRevision`, normal action fields, and `MovementUndo`. Pure calculator output has unbound revision 0; the live service stamps the captured revision. |
| Undo availability | `MovementUndoAvailability.CanUndo`, `Failure`, `MoveCommandNumber`, nullable `Destination`. Token and destination exist only for an available answer. An unavailable answer has token 0 and no destination; it does not prove that the saved checkpoint was removed. |
| Freshness | `ICombatStateBufferReadSink.QueryRevision`. Starts at 1 per battle and becomes 0 after EndBattle. Accepted commands and actual direct character/soul or completion changes advance it. Rejected/duplicate commands, preview publication, no-op direct changes and inventory quantity grants do not. |
| Explicit command | `CombatViewCommandData.UndoMove(EntityId unit, long expectedBattleInstanceId, long expectedControllerTurnSequence, int expectedMoveCommandNumber)`. `CombatViewCommandKind.UndoMove = 15`; Core command value is also 15. Identities/token must be positive and the actor nonempty. No client destination, distance, MovementPreviewContext or state is accepted. |
| Core request | `ExpectedMoveCommandNumber` passes through the existing service request and input assembler. Undo uses its dedicated calculator and current publication transaction. |
| Published result | `CombatResult.UndoMovement`: `ActorId`, `MoveCommandNumber`, `From`, `To`, `Rotation`, `DistanceReverted`. Its action settlement is Moved to Available. Exactly one ordered `CombatCharacterEvent(Returned)` carries before/after facts. |
| Rejection | Normal rejected command result, with reason from `MovementUndoFailureReason`: NoCheckpoint, WrongPhase, WrongController, StaleBattle, StaleTurn, StaleToken, ActorUnavailable, ActorChanged, DestinationOccupied, DestinationInvalid, ReturnWouldCauseEffect, PendingReaction. No distance or gameplay mutation. |

The types live in `CoreCaculator/Contracts/Combat/` and `Contracts/Results/` under `RuntimeSystem/InGameSystem/CoreSystem/`. `CombatViewCommandData` is in BufferSystems/CombatView. The existing public submissions are:

```csharp
int requestNumber = buffers.CombatViewWriteSink.SetPreview(
    CombatViewPreviewData.ActionAvailability(selectedActor));

int commandNumber = buffers.CombatViewWriteSink.Submit(
    CombatViewCommandData.UndoMove(answer.ActorId, answer.BattleInstanceId,
        answer.ControllerTurnSequence, answer.MovementUndo.MoveCommandNumber));
```

These are API call examples, not a new View controller or permission to bypass its command gate. Factory validation rejects an empty actor or nonpositive battle/turn/move identities before submission. Undo has no destination, path, distance or movement-context parameter. Do not substitute the latest result sequence, current preview request number or latest issued input number for MoveCommandNumber. The input buffer shares one increasing number source between commands and previews, so gaps are normal.

### Query and constructor defaults

`CombatActionAvailability` retains BattleInstanceId, ControllerTurnSequence, ActorId, ActionUse, MovementAllowance, MoveFailure, AttackFailure, CanMove and CanAttack. The added trailing constructor arguments are `MovementUndoAvailability movementUndo = null` and `long sourceRevision = 0`; old constructor calls remain unavailable for undo and unbound to a live query revision. `WithSourceRevision(long)` returns an immutable copy and preserves the other fields; negative revisions are rejected. A pure revision-0 response must not be treated as a current live answer.

`MovementUndoAvailability.Failure` is the actual property name; `CanUndo` is derived from Failure == None. The default `Unavailable` value is NoCheckpoint, token 0 and Destination null. A successful value has a positive int MoveCommandNumber and a nullable CellCoord Destination containing the saved anchor. Failed answers deliberately expose neither token nor destination, even when the checkpoint still exists. The enclosing answer supplies the long battle/turn identity and EntityId actor; do not duplicate that identity in View-owned gameplay state.

Existing CombatResult/transition construction defaults UndoMovement to null, and command-state construction defaults MovementUndo to Empty. The pure input's optional ExpectedMoveCommandNumber defaults to 0 for non-undo/query assembly; successful execution still requires the exact positive live token supplied by the public UndoMove factory. `ReturnUnit = 3` remains deployment field-to-hand return; `UndoMove = 15` is field-to-original-placement cancellation. Retired command values 8/9 are not reused.

| Failure value | Meaning for the consumer |
|---|---|
| None = 0 | Ready only when the enclosing live answer and selection/request keys also match. |
| NoCheckpoint = 1 | No record for the requested actor, including a consumed or committed move. |
| WrongPhase = 2; WrongController = 3 | Not the active player combat command context. |
| StaleBattle = 4; StaleTurn = 5; StaleToken = 6 | Request identity no longer matches. Refresh; never guess a replacement token. |
| ActorUnavailable = 7; ActorChanged = 8 | Actor cannot return or its current facts no longer match the saved expectation. |
| DestinationOccupied = 9; DestinationInvalid = 10 | The saved placement is blocked/invalid or its dependencies differ. An occupied origin may become usable later with the same token. |
| ReturnWouldCauseEffect = 11 | Hypothetical position/passive normalization would change additional gameplay facts. No hypothetical effects are applied. |
| PendingReaction = 12 | An unresolved reaction prevents undo; do not use return to trigger/bypass it. |

Use generic public explanations; do not disclose hidden origin dependencies or infer record removal from every failure. An actively processed rejection uses `CombatResult.Accepted == false` and Reason; no-battle/ended-run guards can instead make ProcessNextCommand return false without a new result. Failure describes the applicable validation, not an instruction to force the requested action.

## 2. Freshness, exactly-once processing and acknowledgment

The source revision is query freshness, not undo permission and not a replacement token. A preserved token may become temporarily unavailable when another unit occupies the origin, then available after that unit leaves. Request a fresh availability response and validate the selected actor, request lifetime, battle/turn and SourceRevision against the current read sink before offering submission. Execution still rechecks the current world.

The command queue and existing result sequence/acknowledgment remain authoritative. Replaying the same handled command number is a no-op; submitting a new command with a consumed token produces a rejection. Rejected results can advance result/handled counters without advancing QueryRevision.

QueryRevision starts at 1 when a battle is installed, before deployment/turn commands may increment it. It is paired with BattleInstanceId and resets to 0 at EndBattle. SourceRevision must equal the current QueryRevision, with the same battle/turn/actor and still-owned request number. The service captures before assembly, then rechecks battle/request/revision before publishing. Native publication rejects unbound/stale/foreign responses without advancing AnsweredPreviewNumber. Other preview kinds keep their existing contracts.

The query key and original-move token have different lifetimes: B moving onto A's origin changes QueryRevision and CanUndo while preserving A's token. Likewise, query selection/slot replacement does not commit A's move. When a direct change creates no new command result, QueryRevision still requires a fresh answer. No-op direct changes, preview replacement/publication and inventory-only grants do not advance it. Accepted commands advance it even when they preserve another actor's checkpoint.

Calling Submit again with the same DTO allocates a new command number; it is not a retry of the old number. After a successful undo, such a command rejects without subtracting distance again. The native processor returns false when no unhandled command remains. Consume results in sequence through the existing playback owner, including rejections; after presentation settles call `CombatViewWriteSink.AcknowledgePresented(sequence)`. Remove handled inputs through `RemoveCommandsThrough(CombatStateReadSink.HandledCommandNumber)` under the existing owner. Acknowledgment and queue cleanup do not execute an undo. Do not acknowledge a result before its presentation has completed just to reopen input.

## 3. Result, playback and authoritative ownership

For a successful undo, CombatResult has `Accepted == true`, `CommandKind == UndoMove`, the actor/battle/controller-turn identity and CommandNumber/Sequence of that request. UndoMovement carries ActorId, the original MoveCommandNumber, From, To, original Rotation and nonnegative DistanceReverted. ActionUseSettlement is `(actor, Moved, Available)`. Events contains exactly one CombatCharacterEvent of Kind Returned: Expected is the current pre-undo actor and Final carries the returned placement/action/corrected distance. Movement, PathMovement and Attack have no undo payload. No reverse path, pickup, trait activation, reaction or forward-distance event is produced.

The live character/world Buffers remain the gameplay authority. The checkpoint in combat state is the only saved cancellation record; ExpectedActorAfterMove is a validation guard, not a snapshot to reinstall. Undo restores only recorded placement, Moved-to-Available and its exact recorded distance increment, preserving HP/stats/traits/XP, other record fields, resources, turn counters and unrelated actors. Category allowance is unchanged and fully available for a subsequent ordinary move. No distance-based XP rule is introduced.

Ordered Returned events are the presentation authority; result metadata must not cause a second state application. Showing the actor at the original placement without reverse walking is the accepted direction; animation/timing remain View decisions. From/To value-shape support is not proof of an executable same-cell loop. Current repeated-cell and second-ordinary-move rejection remain in place; only explicit undo restores the opportunity. A rejected command has no undo result or gameplay/statistic mutation.

## 4. View-owned integration and optional controls

The following current files are integration touchpoints, not completed View delivery:

| View path under `RuntimeSystem/InGameSystem/ViewSystem/Combat/` | Current condition / consumer responsibility |
|---|---|
| `Input/Interaction/CombatActionAvailabilityTracker.cs` | Existing selected-actor query owner; extend its key/refresh behavior for QueryRevision/SourceRevision and MovementUndo. Its current result/turn/selection key alone cannot detect direct-world changes. |
| `Input/ActionButtons/CombatActionButtons.cs`; `Display/Hud/CombatActionPanel.cs` | CanUseReturn is still forced false; the panel still hides return and retains ReturnSlot 2. Enable only under the View owner's chosen UI design and existing UIInputBinding/UISystem contract. |
| `Input/CombatInputRouter.cs` | Existing Return input remains ignored. The View owner routes an adopted control through its current single command gate, never through a new queue or direct Core mutation. |
| `Playback/Ordering/CombatEventTypeTable.cs`, `CombatMovementPlayback.cs`, `OrderedCombatPresentation.cs` | Existing field-to-field Returned route is distinct from ReturnUnit. View must accept corrected action/distance facts once and honor completion/cancel/detach resource lifetimes. Existing routing/compilation alone is not manual acceptance of this new command. |

View must obtain gameplay facts from Buffer and retain its existing pending-command, playback and selection lifetime ownership. It must not read InGame SOs for undo eligibility, call Core mutators, create a new Job/transaction or maintain another authoritative checkpoint collection. RecordInputOnly input without an executed authoritative move cannot fabricate a checkpoint.

A button/shortcut is the initial UI recommendation. Optional origin-click input uses exactly the same API and is Planning/View-owned. Ordinary Move/Attack/item targeting never converts to undo because a coordinate equals any saved origin. Changing selection does not commit the old actor's move, but old callbacks must not submit for the new selection. Core validates the requested actor/token, not View selection.

If origin-click is adopted, only the currently selected actor's valid answer in an explicitly allowed mode may submit UndoMove. Attack/item targeting on that same tile retains its own intent; adding a tile-targeted item must not require a new Core undo exception. Selection change A→B retires A's active query/click callback, not A's gameplay checkpoint. Core rejects mismatched actor/token pairs but cannot know that View accidentally sent a fully valid A undo while displaying B selected. This is a consumer lifetime requirement, not a Core-owned selection state.

## 5. Expected examples and evidence limits

- A safe one-cell move records one distance unit. Undo restores the original position and Available action state and subtracts that one unit. The next move uses the unchanged full category allowance.
- A movement with effects cannot be undone; attack commits that actor's move. An unrelated B checkpoint is preserved unless its actor/origin dependencies actually change.
- A remote direct soul change refreshes QueryRevision but preserves an unaffected checkpoint. A soul change at its origin invalidates that checkpoint. Main-only published changes are compared through reconstructed actor facts.
- Inventory quantity grants preserve both checkpoint and QueryRevision; equipping/activating effects use their existing character/command boundaries.

Native U3 publication additionally exercises a Rider with allowance 4: previous accepted turns record 4 + 4 + 2 = 10, a new move of 3 produces 13, undo returns to 10, and a subsequent move of 2 produces 12. Repeated cancellations return to 10 each time; an actual subsequent full 4-step movement reaches 14. Each undo emits one Returned/typed result and leaves other statistics, turn and resources unchanged. No return animation distance is counted.

For A/B interleaving, the native public route moves both, then lets A attack or kill a remote enemy. A becomes Attacked and loses its checkpoint; B keeps its original token across the newer query revision and can undo without reverting A's action or victim's death. A direct soul change at A's origin removes only A's record; a remote change preserves independent records while refreshing queries. Death-generated souls remain a deferred no-op in the current producer; the source-change tests publish typed SoulActionChanges through the real transaction rather than claim that dormant producer was activated.

An occupied origin reports DestinationOccupied with no public token/destination, while the internal checkpoint may survive. When the blocker leaves, a fresh query can expose the same original token again. A consumed token, foreign battle/turn or mismatched actor rejects without placement/statistic changes; reselecting a unit must not guess the missing token from result numbers.

Partial movement evidence uses the native MovementProjection/typed NextStepInvalid proposal and publication checks. It proves correct entered-prefix/final placement provenance and return for that consistent proposal; it does not introduce or prove a new ordinary partial-path gameplay mode. Native loops are not supported. Repeated-cell and second-drag regressions remain mandatory. Effects such as Acceleration, StationaryFire, trails, health/collection, weak ground, forced movement/death, passive changes and pending reactions retain their U1 effect barriers; undo never rolls them back.

## 6. Acceptance and delivery record

U3 uses Unity 6000.0.73f1 against this actual checkout through the AppData-enabled route, sequentially. It does not delete caches, upgrade Unity, exclude source assemblies or use a detached harness. Evidence under `.superpowers/trait-system/` is local/ignored and must be preserved separately when sharing the implementation.

| Gate | Result / scope |
|---|---|
| `undo-u3-public-02.xml` | 33/33 passed, zero skipped: U2 publication suite plus five U3 cases for public queries/cycles/event+ack facts, actor interleaving and selective origin changes. |
| `undo-final-core.xml` | 4,197/4,197 passed, zero skipped; actual project/consumer compilation included. |
| `undo-final-integration.xml` | 383/384 passed, zero skipped; only BossStagePreparationTests.ProductionBossStage_PreparesAndPlacesBoss fails with the existing missing StageKey2101 lookup. This is not an all-green suite claim. |
| Framework/View/PlayMode/manual | Not run in U3. No changed Framework implementation or View component requires a new fixture in this Core phase. These behavioral acceptance results remain external. |

`undo-u3-public-01` was a test compilation error caused by using AttackRange instead of the existing CombatPreviewResult.Range property. It launched no behavioral test run and is not RED evidence or a gameplay regression. Only the test reference was corrected. U3 changes the existing integration fixture and this handoff/plan; runtime implementation remains the user-completed U1/U2 source.

One independent final read-only review confirmed the API/document/evidence boundary with no actionable finding. Post-gate verification matched all 10,941 files in the frozen source manifest; the only U3 source change is the existing integration fixture, with no runtime/asset/meta additions or removals. Final Core XML SHA256 is `7E3311A481AB2ADFD6A763EAE68AACB8D5D2546060C9038482163A0EA1A3FC10`; integration XML SHA256 is `A94C6B3963453BA6F7AEA76E069F8BDBD8D88292B69E0161BBAAFBBE0DEA7686`. The final source-manifest hash and retained local evidence paths are in plan section 7.5. No source changed after those gates.

External View acceptance remains: adopted controls share one pending-command gate; Unknown/Pending/stale availability cannot submit; selection/request/preview ownership and optional attack/item mode priority hold; tutorial/input-stop behavior and voluntary end-turn remain; Returned is applied once and refreshed action/range/statistics agree; cancel/detach/exit releases callbacks/pose leases; AP UI stays absent. Buttons/scenes are not enabled or accepted by these Core gates. The document is prepared locally, not sent to another worker or externally acknowledged.
