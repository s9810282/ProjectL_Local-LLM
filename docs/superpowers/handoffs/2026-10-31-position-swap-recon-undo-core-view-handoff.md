# Position Swap / Recon Squad / Effect-Aware Movement Undo — Core-to-View Handoff

Date: 2026-10-31. Status: Core implementation Phases 0–4 complete; prepared locally, not delivered externally. The [implementation plan](../plans/2026-10-31-position-swap-recon-undo-implementation-plan.md), sections 5.1–5.5, owns execution evidence. The [approved design](../specs/2026-10-31-position-swap-recon-undo-design.md) owns gameplay rules.

## 1. Delivery and acceptance boundary

PositionSwap (440) and ReconSquad (450) are executable through the existing authoring/data/spec/definition/runtime/effect registration path. Position Swap, paired undo, supported movement-effect cancellation, and common reserve-return cleanup are implemented. No production acquisition pool, enhanced definition, View/UI, scene, prefab or presentation asset was added.

| Final native gate | Passed / total | Remaining failure |
| --- | --- | --- |
| `phase4-final-core-02`, `ProjectL.Tests.Core` | 4,359 / 4,359 | None; exit 0, zero skips. |
| `phase4-final-integration-02`, `ProjectL.Tests.CoreIntegration` | 441 / 442 | Retained StageKey 2101 case; exit 2, zero skips. |

The retained integration exception is `ProjectL.Tests.CoreIntegration.BossStagePreparationTests.ProductionBossStage_PreparesAndPlacesBoss`: `System.Collections.Generic.KeyNotFoundException : Stage data not found. StageKey: 2101.` Integration is non-green while this production-configuration failure remains. Existing native growth `SerializeReference` / `Refid -2` diagnostics are recorded separately in plan section 5.5. Core evidence does not establish View playback, scenes, HUD, PlayMode, production-content acquisition or whole-game acceptance.

## 2. Query, token and submission

Use the existing `CombatViewPreviewData.ActionAvailability(EntityId unit)` query. Its `CombatActionAvailability` answer contains `SourceRevision`, `BattleInstanceId`, `ControllerTurnSequence`, `ActorId`, `ActionUse`, and `MovementUndo`. `MovementUndoAvailability` contains `CanUndo`, `Failure`, `MoveCommandNumber`, and nullable `Destination` (the initiator's restoration cell).

Submit the answer's initiator and token through the existing command:

```csharp
CombatViewCommandData.UndoMove(
    availability.ActorId,
    availability.BattleInstanceId,
    availability.ControllerTurnSequence,
    availability.MovementUndo.MoveCommandNumber);
```

`CombatStateData.QueryRevision` / answer `SourceRevision`, battle instance, controller-turn sequence, move command number, and result sequence have different meanings. A source revision is a freshness check, not a move token. There is no new passenger undo command or View rollback authority. Keep the established Flow/Job submission and result-consumption boundary.

Requery the original mover after another unit uses a dependent effect, a participant acts, a source changes, or relevant direct writes occur, even if the original mover's HP/position did not change. Queries, previews and rejected commands never commit usage. A successfully executed participant move/attack/reaction or actual effect use permanently locks the older checkpoint; undoing the consuming move cannot reopen it. Temporary third-party occupancy can make undo unavailable and later recover with the same token after the blocker leaves. Genuine participant/source invalidations do not recover.

## 3. Position Swap and paired restoration

Position Swap uses the normal Move command and configured movement range. It adds no independent range or cost setting. Preview and execution share allied endpoint, footprint, reverse-landing and forced-movement-immunity checks. `MovementPreviewContext.PositionSwapPassenger` captures immutable passenger identity and facts. An interrupted intermediate prefix does not exchange the passenger.

The endpoint exchange emits `CombatCharacterEvent` movement facts for both participants. `Expected` and `Final` are complete character facts. `MovementCause.Agency` identifies the initiator as `Voluntary` and passenger as `Forced`, with `TraitKey.PositionSwap`. The ordinary `PathMovement` describes only the initiator. The passenger does not walk that path, spend movement, record distance or trigger voluntary movement traits.

The common `PositionSwap` / `MovementChanged` activation has the initiator as source, both `TargetIds`, final `TargetCells` and `Magnitude = 1`. Use each character event's identity/cell to associate placements; do not infer a second path or gameplay instruction from this summary. Both positions settle internally before endpoint effects run in EntityId order. Arrival damage, collection or another unsupported irreversible change prevents undo; terminal outcomes stop later arrival work.

Example with safe empty terrain: A moves from (0,0) through (1,0) to allied B at (2,0). Final A is at (2,0), B at (0,0). Only A gains two distance units and spends its normal move; B retains its facing, action use and distance. Intermediate A steps still have ordinary movement facts; the endpoint pair is additional typed movement output.

`MovementUndoResult.Participants` is an immutable list of `MovementParticipantRestoration`:

| Member | Consumer meaning |
| --- | --- |
| `EntityId`, `From`, `To`, `Rotation` | Actual field-to-field restoration for that participant. |
| `RestoresActionUse` | True only for the initiator. |
| `DistanceReverted` | Exact initiator distance; always zero for the passenger. |

The initiator is first. Existing scalar `ActorId`, `From`, `To`, `Rotation`, and `DistanceReverted` project that entry; they are not separate live state. In the example, undo produces A (2,0) → (0,0), B (0,0) → (2,0), with only A's two distance units reverted. Ordinary undo has one participant; swap undo has two.

Undo emits one or two `Returned` character facts with both areas still `Field`. This is restoration, not reserve return. Do not remove the views, reverse-walk the initiator's path, trigger arrival gameplay, or apply reserve cleanup. Restore/display the published placements and full state once. A stale passenger, invalid restoration cell or late preparation failure publishes no partial pair. Replayed publication is a no-op.

## 4. Received-damage increase and Recon Squad

Canonical state remains `CharacterCombatFacts.StatusEffects`; Buffer exposes it through `CharacterData.StatusEffects` and immutable `CharacterData.StatusEffectSnapshots`.

| Actual read member | Meaning |
| --- | --- |
| `WeaknessExposureEffectSnapshot.EffectiveIncreasePercent` | Sum of each `(EffectOwnerKind, DefinitionKey)` group's maximum. Recon 10 + Recon 20 + HuntersMark 30 yields 50, applied once. |
| `StatusEffectSnapshot.Sources` | All source facts, including suppressed sources; no second state store. |
| `StatusEffectSourceSnapshot.Status` as `WeaknessExposureStatus` | `Origin`, `IncreasePercent`, `LifetimeKind`, nullable `RemainingAffectedSideTurnEnds`, optional `Movement`. |
| `StatusEffectSourceSnapshot.LifetimeKind` | Read-model `StatusEffectLifetimeKind.AffectedSideTurnEnds` or `UntilReserveReturnOrDeath`. |
| `StatusEffectSourceSnapshot.RemainingAffectedSideTurnEnds` | Positive timed clock, or null for untimed exposure. Null is not zero turns. |

`MaximumIncreasePercent` was renamed to `EffectiveIncreasePercent`; no compatibility alias remains. Callback rule strings and holder IDs do not split the same trait group. Timed application by the same logical producer replaces magnitude and duration, including weaker/shorter values. Distinct movement grants remain separate contributions; repeated contact from the same producer/move is deduplicated.

`WeaknessExposureRules.Evaluate` calculates the total together with every positive tied maximum in `UsedSources`. Suppressed and zero sources are not used. That evaluation is retained at the actual positive numeric receiving boundary before shields/items/held protection, even if final HP damage is zero. Zero numeric input and instant defeat fabricate no multiplier use. All tied contributing moves commit; a suppressed lower source stays cancelable when its other guards permit. Preview-only and discarded projections install no usage.

Recon settings are `increasePercent = 10`, `radius = 1` by default, using an orthogonal cross. `ReconSquadTraitStatusSnapshot.IncreasePercent` / `Radius` and `OwnedCharacterStatsSnapshot.ReconSquadDescription` expose the resolved configuration. Normal/enhanced authoring remains independent; no enhanced asset was invented.

An admitted voluntary move scans its starting cell just before the first step and each actually reached cell after arrival effects, once per living hostile field recipient per move, including legal Leap intermediate cells. Unexecuted suffixes, forced movement and undo grant nothing. Enemy voluntary moves use ordinary source identity without a fabricated player undo token. Irreversible moves can still grant forward effects. Grants emit existing `ReconSquad` / `StatusApplied` activations and full `TraitStateChanged` facts; earlier arrival damage does not retroactively use a later grant.

Both exposure lifetimes clear on recipient reserve entry, recipient death or battle cleanup. Untimed entries do not decrement at side end. Returning/killing the producer does not remove contributions on other recipients. Undo removes only that move's still-live grants; previously cleared entries never reappear.

## 5. Supported inverses and actual-use evidence

`MovementUndoCheckpoint.Participants` / `Effects` are the sole checkpoint collection. `MovementEffectId` contains `BattleInstanceId`, `ControllerTurnSequence`, `ActorId`, `MoveCommandNumber`. Guards and receipts are history, not a saved world to install.

| Receipt | Bounded inverse and commitment boundary |
| --- | --- |
| `MovementExposureReceipt` | Remove the originating move's still-live grant. Positive selected numeric use commits it, including shield-only outcomes. Independent recipient HP/status changes survive. |
| `MovementAccelerationReceipt` | Subtract exact `AddedAmount` from guarded held gain through the existing rule. Captured attack use or incompatible later held-value change prevents undo. |
| `MovementTrailReceipt` | Deactivate only created source IDs, retaining allocation. Successful departure using preexisting Ground, actual Dust-restricted attack start or selected Dust defense commits the source move. |
| `MovementStationaryReceipt` | Restore only the guarded moved flag. Later participant/flag changes invalidate it. |
| `MovementShieldReceipt` | Restore bounded source budget/eligibility and optional Cohesion defense, then reconcile candidate positions. Consumed/replaced source history invalidates it; unrelated source budgets and HP survive. |

`MovementUndoResult.RemovedEffects` lists only contributions actually reversed. Refresh Recon sources, Acceleration attack display and trail visibility from complete facts and exact source deactivation. Keep inactive trail allocation history; do not delete/reallocate source IDs. Undo produces no new forward Recon/Acceleration/PositionSwap activation.

`CombatCommandTransition.MovementEffectUses` carries ordered immutable evidence bound to actual event objects in that transition. Supported kinds are `ParticipantAction`, `ExposureDamage`, `AccelerationDamage`, `GroundDeparture`, `DustAttackRange`, `DustDefense`. `CombatCommandTransition.DamageExecutions` retains immutable `MovementDamageExecution` witnesses from the actual receiving owner, including empty use lists for zero input. Their source/consumer/receiving-location checks prevent fabricated uses and omitted whole groups, including Pull/terrain receiving positions. These are Core validation data, not additional playback streams.

Calculation, usage locks, character/environment inverse, result and revision pass through the existing prepare/install transaction. A failed preparation installs none of them. Unsupported effects remain barriers; no generic rollback manager, new service or Buffer gameplay calculation was added.

## 6. Actual reserve entry and item handling

Actual entry into `Hand` or `EnemyWaiting` runs common `CombatReserveReturnCleanup` through `CombatPlacementCalculator.Location`. It removes temporary recipient buffs/debuffs/restrictions/exposure, held Acceleration, Regeneration cadence, temporary producer progress, item effects and held environment modifiers. Position/presence contributions leave through existing owners. A Field Main at night end, ordinary field movement, Position Swap and `UndoMove` do not run reserve cleanup.

Temporary attack/max-HP normalization preserves permanent Hunter/soul contributions and stage records. It creates no damage, healing, kill or recovery credit. Hunter target clears/reconciles when no longer valid while `SuccessfulRewards` and owned growth survive. Existing action-use, same-turn trigger allowances, shield reset/consumption history, RNG/allocation and Run state do not refresh.

Visible shields are removed by retaining canonical `ShieldStatus` history with zero budget, `Eligible = false`, and unchanged reset count. Redeployment does not reconstruct a new budget. A normal later reset may refill through the existing rule. `ShieldEffectSnapshot.RemainingEligibleBudget` and source eligibility govern active display; the presence of a historical source alone is not an active shield chip.

Cleanup retains `Items.EquippedItemKey` while clearing `Items.Effects`. The existing surrounding manual `RefundReturn` moves one equipped item to inventory and clears the slot exactly once. An already consumed item is not refunded; this is not gold reimbursement. Night return preserves the existing `ClearRoundItems` discard policy. Inventory preparation failure leaves effects, equipment and inventory unchanged.

Reserve event order is relocation `Returned` followed by cleanup `TraitStateChanged` facts/activation summaries when needed; manual equipment handling can follow that cleanup. Consequently a relocation event or an intermediate TraitState event is not necessarily the command's final full equipment/status state. Apply the ordered facts once and finish with the published Buffer state. The Regeneration native test explicitly checks its zero-cadence TraitState after relocation. Field-to-field undo `Returned` events carry restoration and do not enter this cleanup flow.

## 7. Common activation and once-only consumption

Select presentation by `(TraitKey, TraitActivationKind)`. Actual `TraitActivationSnapshot` members are `SourceUnitId`, `TargetIds`, `TargetCells`, `Magnitude`, `ResultOrigin`, optional `AttackDirection`, `TargetCell`, `Interception`, and `ChildAttack`. Convenience `TargetId` is null unless there is exactly one target. There is no generic `AppliedOutcome` field: applied character/source facts and typed interception/child attack data carry the result.

`ResultOrigin` preserves `CombatDamageOrigin.PhysicalCell`, `CreditedCharacterId`, and `AttackGroup`. Honor typed origins for children/interception instead of inventing a new source or damage action. Summaries describe the corresponding facts; do not replay their magnitudes as another state mutation. Undo/result summaries, numeric witnesses and full character events must not execute the same gameplay twice. Presentation completion, cancel, rebind and detach must not submit hidden gameplay, tick lifetimes or refresh allowances.

## 8. Receiving-owner checklist and reproduction

These are pending View-owner checks, based on current consumers; they are not instructions to change View in this Core phase.

| Receiving seam | Concrete check |
| --- | --- |
| `CombatEventTypeTable` and `CombatFactEventHandler` | Current table has ordinary character/source events but no `CombatTraitStateEvent` or common trait activation row; the fact handler has no TraitState case. Verify routing/complete shown-fact refresh for Recon, return cleanup, Acceleration and shield history before claiming playback acceptance. |
| `OrderedCombatPresentation`, character movement handler / `FieldUnitPresenter` | Exchange two views and restore both from typed facts. Existing Return routing distinguishes Field → Field; verify paired timing, selection/hover and absence of pooling/removal for undo. Passenger has no initiator path. |
| `CombatActionAvailabilityTracker` and command input | The tracker currently keys battle, turn, latest result and selected actor. Check response `SourceRevision` against current `QueryRevision` and invalidate cached undo after another actor's effect use or a direct-write revision with no selected-actor movement. Requery rather than forge a new token. |
| Status/HUD/environment display | Show effective total and all source/lifetime detail; remove exact Recon grants and created trails on undo/return, refresh Acceleration attack and normalized max HP, hide inactive zero-budget shield history. |
| `CombatResultPlayback`, `CombatViewRefresh`, scene attach/detach | Consume each result once, preserve event order, finish with published facts, cancel stale callbacks on detach/rebind and perform no Core mutation/lifetime ticks during cleanup. |

Minimum receiving scenarios:

1. Safe swap A (0,0) → B (2,0), then undo A: both cells/facing/action displays restore and only A's distance is refunded. A passenger action before undo disables the original affordance; a stale token changes neither view nor gameplay.
2. Recon 10 plus Recon 20 plus HuntersMark 30: show 50 percent with separate sources. A positive attack fully absorbed by a shield still disables all selected source moves. A suppressed Recon 10 move can remain cancelable.
3. Move A creates Ground/Dust, B actually uses the source and then undoes B: A stays locked. An unrelated actor's action does not remove another checkpoint or unrelated status.
4. Undo an unused mixed-effect swap: remove only live Recon/movement contributions and exact trails; retain unrelated debuffs, items and later recipient state. Return a recipient before undo: its grant never resurrects.
5. Manual equipped return and night recall: temporary state clears, manual item refund occurs once, no consumed-item refund, Field Main retains field-eligible state, and redeploy does not replenish consumed allowances/shields. Cancel playback/detach during the result and reattach from published state without another gameplay execution.

Core fixtures supporting these scenarios include `MovementEffectCombinationTests`, `MovementEffectUsageTests`, `ReconSquadUndoTests`, `ReserveReturnEffectResetTests`, `PositionSwapPublicationTests`, `MovementEffectCombinationPublicationTests`, `ReserveReturnPublicationTests`, and the migrated night lifecycle publication tests. Scene/presentation acceptance, production acquisition configuration and external delivery remain with the receiving owner/user. The user retains commit and push control.
