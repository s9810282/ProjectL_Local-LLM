# Position Swap, Recon Squad, and Effect-Aware Movement Undo Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans for authorized implementation. Phases 0-4 were separately authorized and are complete (sections 5.1-5.5). Stop at the completed Phase 4 boundary. No commit, push, external delivery or View implementation is authorized.

**Goal:** Add Position Swap and Recon Squad, reverse only movement-owned effects before their use, and clear temporary unit effects on reserve return while preserving permanent growth and anti-abuse history.

**Architecture:** Extend the existing immutable MovementUndoState/checkpoint and command transaction. Effect owners supply bounded inverse material and actual execution usage; existing character/environment owners retain live values, Buffer prepares and installs, and View consumes immutable facts. No second rollback engine, status manager, or Framework service.

**Tech Stack:** Unity 6000.0.73f1, C#, Core Contracts/Domain/Application, existing Systems/Buffer/InGame SO authoring, NUnit EditMode. No new dependency.

**Spec:** [Approved design](../specs/2026-10-31-position-swap-recon-undo-design.md), including U01-U12 and approved P01-P07.

**Status:** Phases 0-4 complete. Final Core: 4,359/4,359. CoreIntegration: 441/442, non-green solely for the exact baseline StageKey 2101 failure; zero skips. Final evidence and locality review are in section 5.5. Work remains in feature/Cheon/Core/NewCombatSytem at unchanged HEAD a0ec23c0e3b3a96ebe6ac4c709a09f355d4c00a5. Core-to-View handoff is prepared locally; View/content/PlayMode/manual acceptance and external delivery remain pending. No commit or push.

## Global Constraints

- User performs commits and pushes. Skill/template commit steps do not apply.
- Execute only the authorized Phase or explicit continuous Phase range. At each other Phase boundary, report evidence and stop. Design changes require discussion before implementation.
- Core/InGame/Buffer changes and a prepared View handoff are in scope. View, UI, scenes, prefabs, FieldCamera, Architecture and external delivery are not. Do not edit production acquisition pools or invent enhanced entries.
- Preserve unrelated FMOD catalog/cache and legacy specialization design changes. Reinventory concurrent work before execution; do not revert or absorb it.
- One authoritative live state per fact. Checkpoints contain inverse history and guards, never an installable old world or another live damage total. Preserve existing prepare/install order and Job authority.
- Same-trait received-damage increases use the maximum; different traits add. All tied maxima count as used; suppressed lower sources do not. Numeric damage incorporating an effect commits it even when shields absorb all damage.
- Queries/previews never commit. Successful subsequent participant actions and actual source use commit permanently; canceling a consuming action never reopens the older move. Preserve existing own-attack, action, battle and turn restrictions.
- Only the initiator spends movement and records distance. Swap passenger displacement and undo produce no voluntary movement traits. Existing unsupported/irreversible effects remain barriers.
- Hand/EnemyWaiting entry clears temporary recipient effects. Hunter permanent growth and reward/action/trigger/reset history survive. Undo is field-to-field and never calls reserve reset.
- Manual Hand return retains existing equipment refund: move the equipped item back to inventory and clear the slot exactly once. This is not gold reimbursement. Night/lifecycle equipment policy remains with its current owner.
- Core/InGame runtime contracts remain Unity-free. English implementation documents; Korean C# comments/XML docs; existing namespaces/spelling, Allman braces, four spaces, UTF-8/LF. New C# files require unique `.meta`; moved assets retain GUIDs.
- No Unity upgrade or cache deletion. Future Unity runs use approved AppData access, sequentially, with the user's editor left under their control.
- Retired traits remain retired; Necromancy/ThornField/old Focus and other deferred work are not activated. StageKey 2101 and native growth serialization are separate existing investigations.

## Review Focus

1. A source can tie for the effective maximum yet produce no HP difference after rounding/shields: B1/C1/D2 must still identify and commit all used sources.
2. Speculative Sacrifice/preview evaluation can look like real damage: A2/C1/D2 must install no lock from discarded projections or failed preparation.
3. A swap can fail after accepted intermediate steps: E1 must preserve the accepted prefix and leave the passenger untouched, or reject without mutation when invalidity was known initially.
4. Return/redeploy can accidentally replenish consumed shields, trigger limits or Hunter rewards: F1 must clear visible temporary effects while retaining anti-abuse history.
5. An unrelated later action can change a Recon recipient or occupy a restoration cell: D2/E2 must preserve unrelated facts and distinguish temporary blocking from irreversible commitment.

## 1. Phase sequence and exits

| Phase | Retained tasks / former scope | Deliverable | Exit / stop condition |
| --- | --- | --- | --- |
| 0 | 0.1; unchanged ongoing baseline | Current baseline, exact owner/caller map and verification route | Evidence recorded; no product change. Stop before Phase 1. |
| 1 | A1 -> A2 -> B1; former A + B | Common cancellation/publication contract plus exposure grouping, lifetime and selected-source evaluation | Ordinary undo and atomic publication preserved; same-trait max / cross-trait sum and HuntersMark timing verified; read contract documented. New effect cancellation remains disabled. Full Core/CoreIntegration gates, report, stop. |
| 2 | C1 -> C2 -> D1 -> D2; former C + D | Reversible existing movement effects and complete Recon Squad | Acceleration/trails/passive inverse guards and Recon path/lifetime/source-specific undo work with actual-use locks. No unguarded cancellation. Full Core/CoreIntegration gates, report, stop. |
| 3 | E1 -> E2; former E | Position Swap and paired undo | Preview/execute agree; participant actions lock; no half-swap or passenger action/distance refund. Full Core/CoreIntegration gates, report, stop. |
| 4 | F1 -> G1 -> G2; former F + G | Reserve-return reset, combined regression/locality review and View handoff | Temporary effects clear without refreshing persistent/anti-abuse history; equipment policy preserved; final full Core/CoreIntegration gates and handoff complete. Report Core evidence and external View acceptance separately, stop. |

Order: `0 -> 1 -> 2 -> 3 -> 4`. Phase 0's work, evidence and execution scope are unchanged; only its next-phase reference now points to Phase 1. Task IDs A1-G2 remain stable so existing cross-references and test labels still resolve; these letters identify tasks, not separate current Phase approval boundaries. Within an authorized consolidated Phase, continue through its listed tasks without an approval stop at the former A/B, C/D or F/G boundary. Design deviations still require discussion before implementation.

This remains one integration plan because the traits share cancellation, status and publication contracts. Keep every task's targeted regression and invariant checks; run full Core then CoreIntegration at the consolidated Phase exit. An earlier full run is warranted by a new failure or unresolved integration concern, not merely a former Phase boundary. Do not parallel-edit shared facts, damage, movement or transaction owners. A partial Phase must not be presented as a releasable feature.

## 2. Paths and change boundaries

Prefixes below expand to exact repository-relative directories. In file lists, a bare filename inherits the directory of the preceding fully prefixed file. New production files are explicitly labeled; existing files are modified only where their task requires it.

| Prefix | Directory |
| --- | --- |
| C | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator` |
| CS | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem` |
| B | `Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems` |
| TA | `Assets/ProjectL/Script/Systems/DataBaseSystem/Data/InGame/Table/Unit/Trait` |
| T | `Assets/Tests/EditMode` |
| TI | `Assets/Tests/EditMode/CoreIntegration` |

Planned new production units:

- `C/Contracts/Combat/MovementEffectReceipt.cs`: typed movement identity, participant restoration and bounded inverse/usage values together; no generic dictionary, callback registry or mutable history service.
- `C/Application/Combat/MovementEffectUndo.cs`: dispatch only the supported inverse families against current facts, returning a candidate transition through the existing calculator/policy. Effect arithmetic remains with its existing rule owner.
- `C/Application/Combat/ReconSquadApplication.cs`: movement-path recipient selection and application of the shared exposure policy.
- `C/Application/Combat/CombatReserveReturnCleanup.cs`: temporary recipient cleanup that deliberately does not own equipment inventory or permanent progression.
- For each new trait, follow the current Leap/GroundCompaction pipeline: `TA/{Name}TraitAuthoring.cs`, `TA/{Name}TraitData.cs`, `C/Contracts/Commands/Traits/{Name}TraitSpec.cs`, `C/Domain/Traits/Definitions/{Name}TraitDefinition.cs`, `C/Domain/Traits/States/{Name}RuntimeState.cs`, `C/Domain/Traits/Rules/{Name}TraitRule.cs`, `C/Domain/Traits/Effects/{Name}EffectRule.cs`, `C/Contracts/Snapshots/Traits/{Name}TraitStatusSnapshot.cs`, and `C/Application/Combat/Traits/{Name}TraitFactories.cs`. `{Name}` is exactly `ReconSquad` or `PositionSwap`. Existing registry-required runtime adapters contain no second applied-status/receipt store; movement stays in its existing calculator, not in a second movement engine.

Existing owners:

| Responsibility | Files |
| --- | --- |
| Cancellation/freshness | `C/Contracts/Combat/MovementUndoCheckpoint.cs`, `MovementUndoState.cs`, `MovementUndoOriginFacts.cs`, `CombatChangeEvidence.cs`, `CombatChangeEvidenceReader.cs`; `C/Contracts/Results/MovementUndoResult.cs`, `MovementUndoAvailability.cs`; `C/Application/Combat/MovementUndoPolicy.cs`, `MovementUndoCalculator.cs` |
| Movement/occupancy | `C/Domain/Movement/MovementRouteValidator.cs`; `C/Application/Combat/CombatMovementCalculator.cs`, `MovementProjection.cs`, `MovementContextCalculator.cs`; `C/Contracts/Combat/MovementPreviewContext.cs` |
| Canonical effects/read facts | `C/Contracts/CombatEffects/AppliedStatus.cs`, `StatusEffectSet.cs`, `CharacterStatusCompatibility.cs`; `C/Domain/CombatEffects/Rules/WeaknessExposureRules.cs`, `AppliedStatusRules.cs`, `ShieldStatusRules.cs`; `C/Application/Combat/StatusEffectReadModel.cs`; `C/Contracts/Snapshots/StatusEffectSnapshot.cs` |
| Actual use/terrain/passive | `C/Application/Combat/CombatDamageApplication.cs`, `CombatDamageCalculator.cs`, `AttackTraitScope.cs`, `TraitTrailCalculator.cs`, `CommandEnvironmentState.cs`, `TraitPresenceReconciler.cs`, `EnvironmentPresenceReconciler.cs`; `C/Contracts/Combat/TraitTrailFact.cs`; existing Acceleration/StationaryFire/GroundCompaction/DustScreen rules |
| Return/lifecycle | `C/Application/Combat/CombatPlacementCalculator.cs`, `CombatTurnCalculator.cs`, `TraitTurnLifecycle.cs`, `CombatTemporaryStateCleanup.cs`, `RegenerationTurnApplication.cs`, `HunterApplication.cs`; `C/Application/Combat/Items/ItemEquipmentCalculator.cs`, `ItemLifetimeCalculator.cs` |
| Publication | `CS/CombatSystem/CombatTransitionPublisher.cs`; `B/Core/BattleTransactions.cs`; `B/Character/CharacterBuffer.cs`, `CharacterData.cs`; `B/Combat/CombatStateBuffer.cs`, `CombatStateData.cs` |
| Registration | `C/Contracts/Common/TraitKey.cs`; `CS/CharacterSystem/TraitSpecAdapter.cs`; `C/Application/Combat/Traits/TraitRegistry.cs` for definition/effect factories and descriptions; inspect its runtime/owned description consumers in Phase 0 without adding a second catalog |

No broad file splitting is planned. A new producer should require its trigger/settings/registration and shared receipt calls; it should not need another trait's private implementation. New inverse families legitimately require cancellation validation and integration tests.

## 3. Shared implementation seams

These are planned names and contracts, not claims that the APIs already exist. Preserve current public entry points; add overloads/immutable fields at these seams rather than parallel services.

- `MovementEffectId(long battleInstanceId, long controllerTurnSequence, EntityId actorId, int moveCommandNumber)` is a value identity. Positive IDs are required for player cancellation. Enemy/automatic effects have no fabricated player identity. `EffectOriginKey` continues to identify the producer.
- `MovementUndoCheckpoint` gains immutable `Participants` and `Effects`, each defensively copied. The initiator's existing identity/token remains the query/submission key. Participant entries contain original and expected placements/facing; only the initiator carries action/distance restoration. Effect receipts carry exact contribution IDs, added amounts or bounded prior/expected guarded values, never whole-world snapshots.
- `MovementEffectUse` carries the existing source identity, optional originating `MovementEffectId`, consumer ID, typed use kind and existing action/damage origin. The command transition carries ordered execution-only usage. `MovementUndoPolicy.Reconcile(CombatCommandTransition resolved)` remains the sole command-level checkpoint update; `WithoutAffected(CombatChangeEvidence evidence)` remains the direct-write invalidation entry.
- Preserve `MovementUndoCalculator.Evaluate(CombatCommandInput input)`, `Calculate(CombatCommandInput input)` and `MovementUndoPolicy.ValidatePublication(CombatCommandTransition transition, IReadOnlyList<CharacterCombatFacts> published)`. Add `MovementEffectUndo.TryApply(CombatCommandInput input, MovementUndoCheckpoint checkpoint, out CombatCommandTransition candidate)`; false returns no candidate installation. Evaluate and Calculate use the same guard/candidate rules.
- Extend `WeaknessExposureStatus` with explicit lifetime kind (`AffectedSideTurnEnds`, `UntilReserveReturnOrDeath`), nullable remaining-turn value for the timed kind, and optional movement identity. Keep the existing timed constructor as an overload. Same-producer timed replacement stays unchanged; movement grants are distinct by producer plus move identity and recipient, with duplicate same-move contact ignored.
- Add `WeaknessExposureRules.Evaluate(StatusEffectSet current)` returning `WeaknessExposureEvaluation { int EffectivePercent; IReadOnlyList<WeaknessExposureStatus> UsedSources; }`. `Amplify(int damage, StatusEffectSet current)` delegates to that policy; the actual damage path retains the same evaluation's sources. Group by trait identity qualified by owner kind; callback RuleKey/entity ID never creates a second trait group. No Item/Artifact producers are introduced.
- `WeaknessExposureRules.RemoveMovement(StatusEffectSet current, MovementEffectId movement)` removes only live entries from that move. Read facts use `EffectiveIncreasePercent` and explicit source lifetime. Keep a zero-storage old-name alias only if a current downstream compile dependency requires it; never leave two numerical owners.
- `CombatReserveReturnCleanup.Clear(CharacterCombatFacts character, List<CombatEvent> events)` returns immutable cleared facts and ordered existing state/removal evidence. It is called only on actual entry into Hand/EnemyWaiting inside the existing projection, before publication; inventory and equipment remain the surrounding return flow's responsibility.
- Allocate `TraitKey.PositionSwap = 440` and `TraitKey.ReconSquad = 450`, currently unused above existing 430. Recheck collisions in Phase 0. PositionSwap spec has no extra range/cost settings. ReconSquad authoring/spec/definition take `increasePercent = 10` and `radius = 1`, using orthogonal cross geometry. Normal/enhanced settings copy independently; no enhanced asset is invented.

Extend existing full-fact snapshots, copies, equality and typed events whenever a field is added. Value and selected-source detail must come from one calculation. Unknown effects remain barriers. Do not enable an inverse before its execution usage and direct-write guards are present.

## 4. Verification procedure for authorized execution

Phase 0 copies the inspected `.superpowers/buff-debuff-system/run-tests.ps1` to `.superpowers/position-swap-recon-undo/run-tests.ps1`, changing its evidence directory only. Preserve Core/CoreIntegration allowlist, hidden launch, unique run names and positive XML-count checks. Do not alter or overwrite older evidence. No runner is created or executed by this planning turn.

Later commands from the project root, with approved AppData access:

```powershell
pwsh -NoProfile -File .superpowers/position-swap-recon-undo/run-tests.ps1 -Run A1-red-01 -Assembly ProjectL.Tests.Core -Filter 'ProjectL.Tests.Core.MovementEffectReceiptTests'
pwsh -NoProfile -File .superpowers/position-swap-recon-undo/run-tests.ps1 -Run A1-green-01 -Assembly ProjectL.Tests.Core -Filter 'ProjectL.Tests.Core.MovementEffectReceiptTests'
pwsh -NoProfile -File .superpowers/position-swap-recon-undo/run-tests.ps1 -Run Phase1-core-01 -Assembly ProjectL.Tests.Core
pwsh -NoProfile -File .superpowers/position-swap-recon-undo/run-tests.ps1 -Run Phase1-integration-01 -Assembly ProjectL.Tests.CoreIntegration
```

For each task, replace run label/filter with its named fixture and unique attempt. New T fixtures use namespace `ProjectL.Tests.Core`; TI fixtures use `ProjectL.Tests.CoreIntegration`. RED means the named behavioral assertion fails; setup, compilation, empty selection and missing XML are not behavioral evidence. GREEN means positive selected count, no unexpected failure/skip, successful exit and consistent XML/log evidence. An initial missing-type compile failure must be followed by a real behavioral RED before a behavior is claimed fixed.

At each consolidated implementation Phase 1-4 exit, run full Core then CoreIntegration sequentially. Retain targeted behavioral/publication checks within every task; former Phase boundaries do not independently require another full run or approval. Phase 0 retains its original baseline runs. If assets change, explicitly identify the executed `ProjectL.Tests.CoreIntegration.DataBaseAssetTests` cases. Do not restore the retired Framework test assembly. Reuse exactly unchanged-source evidence for documentation-only completion instead of rerunning suites without cause.

Prior plan section 5.8 records Core 4269/4269 and CoreIntegration 404/405; these are historical evidence, not this plan's baseline. Phase 0 must compare exact failed fullnames/messages, especially `ProjectL.Tests.CoreIntegration.BossStagePreparationTests.ProductionBossStage_PreparesAndPlacesBoss` / StageKey 2101. Preserve native growth `Refid -2` diagnostics in reports. Never suppress a failure, skip a fixture or repair unrelated production data to obtain a green claim. If a baseline exception persists, label the suite non-green with the exact exception and separate new regressions.

## Phase 0 — Baseline and caller inventory

### Task 0.1: Establish current execution evidence

**Files:** read AGENTS instructions, approved design, section 2 owners and existing runner; create only local runner/baseline evidence and append this plan's ledger.

**Interfaces:** consumes current source/dirty manifest; produces baseline XML/log/exit data, exact caller paths and an ownership matrix for all later tasks.

- [x] Record HEAD/branch and hashes of unrelated dirty files; verify the planned trait IDs remain unused and inspect actual assembly definitions.
- [x] Trace Move/Undo publication, actual attack start, numeric damage including interception/reactions, direct writes and all Hand/EnemyWaiting transitions. Record exact registration/context/description files and existing shield reset/reward bookkeeping ownership. Do not turn the inventory into a refactor.
- [x] Prepare the local runner; verify Unity 6000.0.73f1 path and no conflicting editor. Run baseline Core and then CoreIntegration using section 4.
- [x] Record exact failures/diagnostics and current changed files. Report baseline and stop before Phase 1; no gameplay code changes.

## Phase 1 — Common cancellation, atomic publication and exposure policy

Execute A1, A2 and B1 in order. A2's targeted gate is an internal checkpoint; B1 completes the Phase's full regression and user review gate.

### Task A1: Extend the receipt without changing ordinary undo

**Files:** new `C/Contracts/Combat/MovementEffectReceipt.cs`; modify checkpoint/state/origin/result/evidence contracts from section 2 and reserve the two new keys in `C/Contracts/Common/TraitKey.cs` for B's policy tests; new `T/MovementEffectReceiptTests.cs`; extend `T/MovementUndoContractTests.cs` and `MovementUndoRulesTests.cs`. Reserving keys does not register an executable producer.

**Interfaces:** produces section 3 identity, participant/effect receipt, immutable equality/copy and result details. Existing actor/token submission remains accepted.

- [x] Add RED cases `Copies_DoNotAliasReceiptCollections`, `OrdinaryUndo_RestoresExactDistanceOnce`, `ForeignBattleTurnOrMove_Rejects`, and `ParticipantReceipt_DoesNotRefundPassengerActionOrDistance`; assert defensive copying, unchanged old snapshots and no restoration of unrelated facts.
- [x] Run targeted RED, implement the minimum typed contract extension and compatibility overloads, then run targeted GREEN plus existing movement-undo contracts/rules.
- [x] Keep receipt types limited to the approved families. Reject malformed ownership, duplicate participants, foreign source IDs and incompatible action/distance restoration before installation. Unsupported inverse kinds cannot become valid through serialization or public construction.

### Task A2: Publish shared use locks and bounded inverses atomically

**Files:** new `C/Application/Combat/MovementEffectUndo.cs`; modify `MovementUndoPolicy.cs`, `MovementUndoCalculator.cs`, command transition/evidence carriers and section 2 publication owners; new `TI/MovementEffectPublicationTests.cs`; extend `TI/MovementUndoPublicationTests.cs` and `T/MovementUndoLifetimeTests.cs`.

**Interfaces:** consumes A1; produces execution-use transport, common participant/source invalidation and multi-change validation. Only already-supported ordinary undo is enabled at this exit.

- [x] Add RED cases `StaleParticipantOrReceipt_InstallsNothing`, `LatePrepareFailure_PreservesAllStoresResultAndRevision`, `HandledCommandReplay_DoesNothing`, `UsageOnlyChange_AdvancesQueryRevision`, and `RejectedOrSpeculativeUse_DoesNotCommit`. Compare characters, environment, inventory/resources, cancellation state, result and revisions before/after.
- [x] Run RED; extend prepare/install validation for the complete bounded set. Bind each declared use to captured authoritative action/damage inputs; reject forged unrelated-use evidence. Ordinary distance remains monotonic outside the validated initiator undo exception.
- [x] Route command and direct-write invalidation through the existing owner. Separate source revision, query revision, battle/turn and command identity; preserve lifecycle clearing and single initiator query ownership.
- [x] Run targeted GREEN, including existing ordinary-undo and native publication regressions. Record ordinary behavior preservation and remaining disabled families, then continue to B1 within the authorized Phase 1.

### Task B1: Evaluate effective value and contributing sources together

**Files:** modify `AppliedStatus.cs`, `WeaknessExposureRules.cs`, `AppliedStatusRules.cs`, compatibility/copy and status snapshot/read-model files; `MovementContextCalculator.cs`; add `T/ExposureContributionTests.cs`; extend existing WeaknessExposure policy/lifecycle/damage and StatusEffectReadModel tests.

**Interfaces:** consumes A1 identity; produces section 3 exposure evaluation/removal/lifetime contract for D1-D2. Existing HuntersMark uses its timed overload.

- [x] Add RED `SameTraitMax_DifferentTraitsAdd`: Recon 10 + Recon 10 = 10; Recon 10 + Recon 20 = 20; Recon 10 + HuntersMark 30 = 40; damage 100 becomes 140 once. `TiedMaxima_ReturnAllSources` returns both 10 sources, but a suppressed 5 source is absent.
- [x] Add `TimedReapply_ReplacesOwnMagnitudeAndDuration`, `MovementGrants_RemoveOnlyMatchingMove`, `UntimedRecon_DoesNotTick`, `CheckedSumAndAmplificationOverflow_RejectBeforePublication`; include same producer/different move, duplicate contact, expiry fallback and old snapshot immutability.
- [x] Run RED; implement max-per-trait then checked sum, explicit lifetime and source selection in one policy. Preserve logical producer matching for timed HuntersMark. Remove on recipient death/return/battle, not producer return/death; preserve no-source legacy data unrelated to exposure.
- [x] Migrate current effective-total consumers and movement-context comparisons; duration-only changes still do not stale movement damage context, while full publication equality checks complete facts. Prepare the read-contract section of the G2 handoff, including any required zero-storage compatibility alias.
- [x] Run targeted GREEN and full Phase 1 Core/CoreIntegration gates covering A1, A2 and B1. No live Recon producer or production acquisition entry yet. Report and stop before Phase 2.

## Phase 2 — Reversible movement effects and Recon Squad

Execute C1, C2, D1 and D2 in order. Complete each effect's inverse and usage guards together; D2 owns the Phase's full regression and user review gate.

### Task C1: Acceleration and trail inverses with actual-use barriers

**Files:** section 2 actual-use/terrain owners; `MovementEffectUndo.cs`, `MovementUndoPolicy.cs`, trail/status receipt fields; existing Acceleration/GroundCompaction/DustScreen rule files; new `T/MovementEffectUsageTests.cs`; extend `T/TraitTrailTests.cs`, `TraitMovementAttackTests.cs`, `MovementUndoBarrierTests.cs`; extend `TI/MovementEffectPublicationTests.cs` and `TraitTrailPublicationTests.cs`.

**Interfaces:** consumes A1-A2 use/receipt publication; produces exact held-gain/trail-source inverse plus permanently committed use. No second held-attack total or persisted lookup owner.

- [x] Add RED `AccelerationUndo_SubtractsOnlyMoveGain` (prior 7, gain 3, final 10, undo 7), `CapturedGain_NumericHitCommitsEvenWithFullShield`, and `ChildAttack_DoesNotBorrowParentReservation`. Keep existing own-attack barrier even when no contribution is delivered.
- [x] Add RED `ShortDeparture_UsesPreexistingPackedGround`, `NewlyCreatedGround_DoesNotSelfCommit`, `DustAttackStart_CommitsRestrictedRangeSource`, `DustDamage_CommitsActualRecipientSources`, `TiedDustMaxima_AllCommit`, and `PreviewRejectedOrCanceledBeforeStart_DoesNotCommit`. Include supported damage kinds, zero delivered damage, intercepted recipient and no-step moves.
- [x] Run RED; capture selected-source detail at authoritative attack-start/departure/damage boundaries. Forward speculative evaluations may carry discarded detail but never install use. Preserve numerical values and existing damage-kind eligibility.
- [x] Implement exact gain subtraction and created-trail deactivation through existing rules/successors. Never rewind allocation IDs. Enable each receipt only after its usage and direct-write guards are wired. Already committed move removal leaves surviving forward effects intact.
- [x] Run GREEN including overlap preservation and `CancelConsumer_DoesNotReopenProducer`; reject stale sources and late failure with no partial change. Run source-specific publication tests before C2.

### Task C2: Bound passive consequences and retain unknown-effect barriers

**Files:** `MovementEffectUndo.cs`, `MovementUndoPolicy.cs`, `TraitPresenceReconciler.cs`, `EnvironmentPresenceReconciler.cs`, `ShieldStatusRules.cs`, existing StationaryFire rule; new `T/MovementPassiveUndoTests.cs`; extend `TI/MovementEffectPublicationTests.cs`.

**Interfaces:** consumes A1-A2 and C1; produces guarded StationaryFire and Cohesion/SoloAction inverses, reconciled against candidate positions through current owners.

- [x] Add RED `StationaryFlag_RestoresOnlyUnconsumedVersion`, `PresenceUndo_PreservesUnrelatedShieldBudget`, `ConsumedOrReplacedContribution_Commits`, `CandidateReconciliation_UnsupportedHpChangeRejects`, and `DamagePickupOrUnknownMutation_RemainsBarrier`.
- [x] Run RED; record only movement-caused source budgets/eligibility and affected recipient IDs. Restore guarded contributions then reconcile candidate presence; do not restore whole recipient facts or spent shields. Classify every forward changed fact; observational activation alone is not a barrier or proof of reversibility.
- [x] Run targeted GREEN, including passive-effect publication regressions, with later unrelated HP/status changes preserved where the receipt permits them. Preserve conservative position-participant invalidation, then continue to D1 within the authorized Phase 2.

### Task D1: Author and execute deterministic path grants

**Files:** new ReconSquad pipeline files listed in section 2 and `ReconSquadApplication.cs`; modify `TraitKey.cs`, adapter/registration/description owners from Phase 0, `CombatMovementCalculator.cs`, `MovementProjection.cs`; new `T/ReconSquadMovementTests.cs`, `TI/ReconSquadAuthoringTests.cs`.

**Interfaces:** produces the configured Recon producer using B1's shared receiving policy and A1's move identity. Defaults 10 percent/cross radius 1; category Rider.

- [x] Add RED `DefaultAndConfiguredValues_RoundTripIndependently`, `StartAndReachedPath_GrantOncePerEnemy`, `InterruptedPrefix_DoesNotGrantOnSuffix`, `ArrivalDeathOrBattleEnd_StopsScan`, `ForcedAndUndoMovement_DoNotGrant`, and `EnemyVoluntaryMove_GrantsWithoutPlayerUndoToken`. Include legal Leap intermediate cells and hostile living field eligibility.
- [x] Run RED; wire normal authoring/data/spec/definition/factory/runtime description. Scan start after admission immediately before first step, then actually reached cells after arrival effects while actor/battle remain valid. Do not change production pools or invent enhanced definitions.
- [x] Run GREEN and authoring integration checks. Forward irreversible moves still grant but remain non-cancelable. Keep new Recon-producing moves non-cancelable until D2 completes; do not report Phase 2 complete here.

### Task D2: Cancel only Recon grants owned by the move

**Files:** `ReconSquadApplication.cs`, `MovementEffectUndo.cs`, actual numeric receiving boundary and A2 use transport; new `T/ReconSquadUndoTests.cs`, `TI/ReconSquadPublicationTests.cs`.

**Interfaces:** consumes B1 evaluation and A1-A2/C1-C2 validated receipt handling; produces reversible Recon contribution removal and actual-recipient locks.

- [x] Add RED `Undo_PreservesEarlierAndOtherSourceGrants`, `UnrelatedRecipientChange_SurvivesUndo`, `UsedTiedSources_CommitButSuppressedSourceDoesNot`, `ShieldOnlyHit_Commits`, `PreviewAndSacrificeProjection_DoNotCommit`, `ClearedOrExpiredGrant_IsNotResurrected`, and `ProducerReturnOrDeath_DoesNotClearRecipient`.
- [x] Run RED; attach live grant identities to the checkpoint and bind selected sources from real damage to the use transport. Cover interception, multi-hit, reactions, numeric mine/reflection paths and damage-before-grant ordering. Instant defeat/fall do not fabricate multiplier use; existing participant/barrier rules remain.
- [x] Enable Recon inverse only after both creation and consuming paths are complete. Publish source removal/full facts once with no forward activation replay. Run targeted GREEN and full Phase 2 Core/CoreIntegration gates covering C1, C2, D1 and D2. Report and stop before Phase 3.

## Phase 3 — Position Swap

### Task E1: Share allied-endpoint admission and simultaneous exchange

**Files:** new PositionSwap pipeline files from section 2; modify `TraitKey.cs`, adapter/registration/description owners, `MovementRouteValidator.cs`, `MovementContextCalculator.cs`, `CombatMovementCalculator.cs`, `MovementProjection.cs`; new `T/PositionSwapMovementTests.cs`, `TI/PositionSwapAuthoringTests.cs`.

**Interfaces:** consumes existing Move intent/path/range and A1 participant receipts. Produces initiator/passenger final placements and captured passenger identity in movement freshness data; category Heavy.

- [x] Add RED `AllyEndpoint_PreviewMatchesExecute`, `EnemyOrUnsupportedFootprint_Rejects`, `ReplacementPassenger_RejectsStaleContext`, `BlockedReversePlacement_RejectsBeforeSpend`, and `IntermediateStop_LeavesPassengerUntouched`.
- [x] Add `PassengerKeepsFacingActionAndDistance`, `OnlyInitiatorTriggersVoluntaryTraits`, and `EndpointEffects_RunOnceInEntityOrder`; test hazard/object outcomes and terminal short-circuit after simultaneous placement. Previously acted allies remain eligible; normal faction/AI/action constraints remain.
- [x] Run RED; share endpoint validation between preview/execution while leaving intermediate traversal/Leap rules intact. In the projection settle both positions, then run arrivals/presence without transient overlap. Passenger moves to initiator origin, not the last path step; passenger has endpoints, not reverse route traversal.
- [x] Run GREEN and authoring checks; retain a cancellation barrier for new swap moves until E2. No production pool edits.

### Task E2: Restore both participants and lock subsequent actions

**Files:** `MovementUndoCalculator.cs`, `MovementUndoPolicy.cs`, `MovementEffectUndo.cs`, actual movement/attack-start use boundaries and result publication; new `T/PositionSwapUndoTests.cs`, `TI/PositionSwapPublicationTests.cs`.

**Interfaces:** consumes E1 and completed Phase 1-2 contracts; produces paired restoration through the original initiator's UndoMove actor/token. Passenger distance/action are never refunded.

- [x] Add RED `Undo_RestoresBothWithoutEntryEffects`, `EitherParticipantLaterMoveOrAttack_Commits`, `ReactionAttack_Commits`, `PriorPassengerAction_DoesNotCommitNewSwap`, `OldPassengerCheckpoint_IsNeverResurrected`, and `LateFailure_DoesNotHalfRestore`.
- [x] Add `TemporaryRestorationBlock_RequeryCanRecover` and `UnrelatedAction_PreservesExchange`; queries/rejection cannot lock. Validate both destinations excluding the pair's current footprints; source mutation and actual use remain permanent invalidations.
- [x] Run RED; add both participant guards and atomic restoration. No hazards, trails, Recon grants, Acceleration gains or reserve reset on undo. Reject unsupported candidate presence effects, and account for all supported inverse changes from C1-C2/D1-D2.
- [x] Run GREEN and full Phase 3 Core/CoreIntegration gates; report Core paired movement/output support separately from external View playback. Stop before Phase 4.

## Phase 4 — Reserve-return cleanup, combined acceptance and handoff

Execute F1, G1 and G2 in order. F1's targeted return/publication checks precede G1's final full regression; G2 finishes documentation without repeating unchanged-source tests.

### Task F1: Reset temporary effects without refreshing progression or equipment

**Files:** new `CombatReserveReturnCleanup.cs`; modify existing return/lifecycle owners from section 2 and compatible status/item/health subrules only; new `T/ReserveReturnEffectResetTests.cs`, `TI/ReserveReturnPublicationTests.cs`; extend `IntimidationStatePreservationTests.cs`, `RegenerationCadenceTests.cs`, `CombatItemEquipmentTests.cs`, `HunterTraitTests.cs`, `TI/HunterPersistenceTests.cs`.

**Interfaces:** consumes canonical statuses and receipt invalidation; produces section 3 common recipient-reset policy inside existing return transitions. Existing `ItemEquipmentCalculator.RefundReturn` still owns manual item return.

- [x] Add RED `HandAndEnemyWaiting_ClearTemporaryEffects`, `Return_ClearsAccelerationAndCadence`, `Return_PreservesHunterGrowthAndRewardCap`, `SameTurnRedeploy_DoesNotRefreshActionsTriggersOrShields`, and `FieldMainAtNightEnd_DoesNotReset`. Replace old Intimidation/Acceleration persistence expectations only where the approved rule changed.
- [x] Add `ManualReturn_RefundsEquippedItemOnce`, `ConsumedItem_IsNotRefunded`, `InventoryPrepareFailure_ClearsNothing`, `NightReturn_KeepsExistingEquipmentPolicy`, `MaxHpNormalization_CreatesNoHealOrDamageCredit`, and `SwapUndo_DoesNotRunReserveReset`. Test temp item modifiers separately from equipment identity/inventory.
- [x] Run RED; clear temporary recipient status/stat/cadence/producer progress and recompute through existing owners. Preserve permanent source growth, action/trigger allowances, shield reset/consumption history, Hunter rewards, RNG/allocation and Run state. If visible shields are removed, retain their consumed/reset eligibility history within the existing owner; do not reconstruct a fresh budget on redeploy.
- [x] Route all actual Hand/EnemyWaiting entries through the same cleanup, reconcile Hunter target/reaction/field presence, and keep effects already installed on other recipients governed by their lifetimes. Do not reuse death/battle-end Clear wholesale or call Hunter stage reset here.
- [x] Run targeted GREEN and native no-partial-return publication checks. Record exact obsolete assertions migrated and retained equipment behavior, then continue to G1 within the authorized Phase 4.

### Task G1: Exercise mixed dependencies and audit change locality

**Files:** new `T/MovementEffectCombinationTests.cs`, `TI/MovementEffectCombinationPublicationTests.cs`; touched files only for scoped corrections; update this plan's evidence ledger.

**Interfaces:** consumes completed Phase 1-3 work and F1; produces combined regression evidence, not another gameplay mechanism.

- [x] Add mixed cases: swap + Recon + Acceleration + Ground/Dust + passive shield; source A's ground used by B then B undone; tied Recon sources with an unrelated later action; recipient return after a grant; stale second participant plus late environment/inventory preparation failure. Assert full facts, exact distance, permanent locks, immutable old snapshots and once-only ordered evidence.
- [x] Run targeted RED/GREEN for new combination cases and any regression found, then final full Phase 4 Core/CoreIntegration gates against the completed F1/G1 source and tests. If an earlier full run already covers that exact unchanged state, reuse its evidence. Verify no new Unity references in pure contracts, unique `.meta` GUIDs and no unintended production asset changes; run DataBaseAssetTests if assets did change.
- [x] Review actual change/understanding/verification scope against section 2: changing Recon magnitude must not require Ground internals; adding one supported inverse family may change typed receipt validation but must not add another state owner. Check SSOT, SOLID, FW dependency direction and prepare/install order. Record concrete violations or explain why retained coupling is necessary; file counts are not success metrics.
- [x] Record exact suite totals/failures, residual baseline diagnostics and remaining View acceptance. Do not silently fix unrelated findings. Stop before G2 only if a material design decision is required; otherwise complete the authorized Phase.

### Task G2: Complete the Core-to-View handoff

**Files:** create/finalize `docs/superpowers/handoffs/2026-10-31-position-swap-recon-undo-core-view-handoff.md`; update this plan and design status with actual completion evidence only.

**Interfaces:** produces consumer-facing immutable result/query contracts and reproduction scenarios; no external message or View implementation is authorized.

- [x] Document paired exchange/restoration facts, initiator actor/token, refreshed availability after another actor uses an effect, effective exposure total, individual source/lifetime detail, Recon removal, Acceleration refresh and trail deactivation. Include actual implemented type/member names rather than leaving section 3's planned signatures as delivery claims.
- [x] Document common TraitActivationSnapshot source/targets/cells/outcome/origin and how full state plus summaries are consumed exactly once. Undo is not a new forward activation; cancel/rebind/detach does not execute gameplay or tick lifetimes.
- [x] List concrete receiving-owner checks for two-unit playback, status refresh, stale token rejection and lifecycle detach. State that Core tests do not establish scenes, HUD, presentation or whole-game acceptance.
- [x] Report completed Phases, verification evidence, existing exceptions and prepared-but-undelivered handoff. Stop; user retains commit/push and external delivery control.

## 5. Coverage and execution ledger

| Approved requirement | Implementation / acceptance owner |
| --- | --- |
| U01/U09, P04-P05: paired exchange and participant locks | E1-E2, G1 |
| U02/U03, P03/P07: Recon path, configuration, reversible grant and damage use | B1, D1-D2 |
| U04: exact Acceleration gain and use | C1 |
| U05-U07: Ground departure and Dust attack range/defense | C1 |
| U08, P01: real numeric use, ties, shield-only outcomes | B1, C1, D2 |
| U10: same-trait maximum / cross-trait sum | B1 |
| U11: reserve reset and persistent exceptions | F1 |
| U12, P02/P06: single checkpoint owner, bounded inverses and barriers | A1-A2, C2, G1 |
| Design sections 10-13: freshness, atomicity, authoring, output and locality | A2, D1-D2/E1-E2 publication tests, G1-G2 |

Planning self-review checked approved rule coverage, shared names, task dependencies, missing-use hazards and Phase exits. The subsequent user-requested consolidation preserves Phase 0 and every A1-G2 task, acceptance requirement and targeted test. It reduces the remaining approval/full-suite boundaries from seven to four; it does not reduce behavioral or integration coverage. No product implementation or Unity/test execution was performed by this document-editing turn. Ongoing Phase 0 evidence belongs to its executor. Proposed test names are acceptance targets, not existing passing tests.

| Phase | Status | Evidence / authorized scope |
| --- | --- | --- |
| Planning | Complete; consolidated | User approved design/P01-P07, then requested consolidation excluding ongoing Phase 0. Scope and Task IDs preserved. |
| 0 | Complete | User authorized Phase 0 on 2026-10-31; current baseline and owner inventory recorded in section 5.1. Stop before Phase 1. |
| 1 | Complete | A1/A2/B1 completed before Phase 2; implementation and evidence recorded in section 5.2. Historical authorization boundary retained there. |
| 2 | Complete | C1/C2/D1/D2 and review corrections verified; Core 4,335/4,335; CoreIntegration 428/429 with exact retained StageKey 2101 failure. See section 5.3. Stop before Phase 3. |
| 3 | Complete | User authorized E1/E2 on 2026-10-31; paired exchange/undo, publication and final evidence in section 5.4. Stop before Phase 4. |
| 4 | Complete | User authorized F1/G1/G2 on 2026-10-31; reserve reset, combined checks, final gates and local handoff recorded in section 5.5. View acceptance remains separately owned. |

### 5.1 Phase 0 execution — 2026-10-31

**Authorization and boundary:** the user requested Phase 0 only. Continued in the existing feature checkout at the exact HEAD above. The concurrent document consolidation into Phases 1-4 was preserved; its Phase 0 scope did not change. No product implementation, test changes, asset changes, alternate checkout, commit, push or external delivery. Task 0.1 establishes the baseline and owners; it enables no new behavior.

**Workspace:** initial tracked modifications were the following three unrelated files; the approved design and this plan were already untracked. Initial status and hashes are retained in `.superpowers/position-swap-recon-undo/phase0-initial-status.txt` and `phase0-initial-dirty-hashes.json`. Final checks match all three hashes exactly.

| Preserved pre-existing path | Initial and final SHA256 |
| --- | --- |
| `Assets/Audio/FMODplus/New Fmod Event Catalog.asset` | `93F50B0C819B90E52261A89B6B5A7F4006DB64AC6327740303105D4750C365CC` |
| `Assets/Plugins/FMOD/Cache/Editor/FMODStudioCache.asset` | `CA3A7D51F69846C5DEDB44F367F4C9A19B27AE5B06EA800D3B8AE5D61089E335` |
| `docs/superpowers/specs/2026-10-29-legacy-enhancement-retirement-main-specialization-design.md` | `9FA4A7B2826DD85E2A22F6DF7F5E5B1FF8A6AF83149F87A92C190C2305E16649` |

**Keys and assemblies:** inspected `C/Contracts/Common/TraitKey.cs`: active sequence ends at `FocusedBombardment = 430`; 440 and 450 remain unused and were not reserved in Phase 0. Retired IDs are unchanged. Contracts has no project references; Domain references Contracts; Application references Contracts and Domain. All three asmdefs set `noEngineReferences: true`. `T/ProjectL.Tests.Core.asmdef` resolves those three exact GUIDs (`05931ba9661d9e745859bbdf70325edb`, `9b019b81075a3af4d9f545e338271abd`, `42f479a350493ca4abd078910e78a9ad`), is Editor/NUnit/`UNITY_INCLUDE_TESTS`, and forbids Engine references. `TI/ProjectL.Tests.CoreIntegration.asmdef` references Framework, Systems, Contracts and Application and allows Engine references. No retired Framework test assembly was recreated or selected.

**Runner and execution route:** copied `.superpowers/buff-debuff-system/run-tests.ps1` to `.superpowers/position-swap-recon-undo/run-tests.ps1`. Exact text comparison confirms only the evidence directory differs; allowlist, hidden launch, unique names, positive XML count and zero-skip checks remain. Verified `ProjectSettings/ProjectVersion.txt` and `D:/Program Files/Unity/Hub/Editor/6000.0.73f1/Editor/Unity.exe` (`6000.0.73f1_a166abc3bf0e`). Initial process 35364 was `D:/UnityHub/Unity Hub/resources/unity.exe serve`, not an Editor, but still blocked the existing name guard. The user explicitly chose to preserve that guard and close Hub themselves. Tests started only after the process disappeared; the agent terminated no process. Both Unity runs used approved AppData access, sequentially. No cache deletion, upgrade or manipulation of the user's Editor.

| Run / assembly | XML root / actual case count | Passed / failed / skipped | Native Unity exit |
| --- | --- | --- | --- |
| `phase0-core-01` / `ProjectL.Tests.Core` | 4,269 / 4,269 | 4,269 / 0 / 0 | 0 |
| `phase0-integration-01` / `ProjectL.Tests.CoreIntegration` | 405 / 405 | 404 / 1 / 0 | 2 |

Executed from the repository root:

```powershell
pwsh -NoProfile -File .superpowers/position-swap-recon-undo/run-tests.ps1 -Run phase0-core-01 -Assembly ProjectL.Tests.Core
pwsh -NoProfile -File .superpowers/position-swap-recon-undo/run-tests.ps1 -Run phase0-integration-01 -Assembly ProjectL.Tests.CoreIntegration
```

**Exact retained failure:** `ProjectL.Tests.CoreIntegration.BossStagePreparationTests.ProductionBossStage_PreparesAndPlacesBoss` — `System.Collections.Generic.KeyNotFoundException : Stage data not found. StageKey: 2101.` Stack: `StageTable.GetStage` (`StageTable.cs:66`) → `BattleDefinitionAdapter.Read` (`:32`) → `DatabaseBattleInitializationSource.Read` (`:43`) → the test (`BossStagePreparationTests.cs:52`). The failed fullname and message compare exactly equal to `.superpowers/buff-debuff-system/F-integration-01.xml`; totals also match the prior final baseline. CoreIntegration is **non-green**, with no additional failed test relative to that baseline. Nothing was skipped, suppressed or repaired to change the result.

**Native diagnostics and artifacts:** integration log lines 1442-1463 contain 22 instances of `The serialized array of [SerializeReference] objects is missing entry for Refid -2`; Core has zero. This remains the separate native growth serialization investigation. All three `DataBaseAssetTests` cases ran and passed in the full integration suite; no assets were edited. XML/log/native-exit files and full failure stack remain beside the runner; `phase0-baseline-summary.json` records exact failure comparison, counts, runner comparison and dirty-file preservation. The execution harness returned 1 for the failed PowerShell session; saved native Unity exit is 2.

#### Current caller and ownership matrix

Paths use section 2 prefixes; a bare filename in a cell inherits that cell's preceding directory. These are current inspected owners, not planned APIs. Local `phase0-command-damage-callers.txt` and `phase0-reserve-callers.txt` retain command/damage and whole-Systems reserve reference searches.

| Boundary / consuming tasks | Current exact owners and execution contract | Required later boundary; no implementation in Phase 0 |
| --- | --- | --- |
| Composition and Job authority / all | `Assets/ProjectL/Script/Systems/Factory/Runtime/InGame/Core/CombatSystemFactory.cs` creates assembler/publisher/services; `CS/InGameCoreSystemGroup.cs` exposes CombatCommands; `Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Combat/Jobs/CombatCommandJob.cs` calls ProcessNextCommand. `CS/CombatSystem/CombatCommandService.cs:51-86` assembles, checks context, calculates and publishes, including UndoMove mapping. | Preserve Factory → internal service → Core group → Job. No root/per-effect service registration is needed. |
| Final command reconciliation / A2 | `C/Application/Combat/CombatCommandCalculator.cs:29,52,72` resolves pending `BossRoarCalculator` reactions before `MovementUndoPolicy.Reconcile`, and dispatches Undo to `MovementUndoCalculator`. | Keep one post-reaction checkpoint owner; only accepted final execution may install usage. |
| Move/preview/freshness / D1,E1 | `C/Application/Combat/CombatMovementCalculator.Range/UnitMoveCells/ProjectRoute`; `C/Domain/Movement/MovementRouteValidator.cs`; `C/Application/Combat/MovementContextCalculator.Capture/Matches/SameValues`; `C/Contracts/Combat/MovementPreviewContext.cs`. ProjectRoute validates, begins ordinary move, enters/reconciles reached steps, then applies move-success effects. | Share allied endpoint admission across range/preview/execute; retain Leap traversal and accepted-prefix stops. Capture passenger identity in freshness data. |
| Ordinary undo / A1-A2,E2 | `C/Contracts/Combat/MovementUndoCheckpoint.cs`, `MovementUndoState.cs`, `MovementUndoOriginFacts.cs`; `C/Application/Combat/MovementUndoCalculator.Evaluate/Calculate`; `MovementUndoPolicy.CreateCheckpoint/Reconcile/ValidatePublication`; `C/Contracts/Results/MovementUndoResult.cs`. Guards include exact actor, battle/turn/token, pending reactions, origin occupancy/facts and candidate position/presence normalization. | Current creation requires ordinary-movement-only evidence and otherwise unchanged facts. Publication permits one actor and one Returned event. Broaden bounded validators before enabling new inverses. Undo restores field placement directly; it does not call reserve Location or arrival processing. |
| Direct writes / A2 | `B/Core/BattleTransactions.Apply(CharacterChanges)`, Apply(SoulActionChanges), CompleteBattle → PrepareUndoInvalidation → `B/Combat/CombatStateBuffer.PrepareMovementUndoInvalidation/InstallMovementUndoState`; `C/Contracts/Combat/CombatChangeEvidenceReader.cs`; MovementUndoState.WithoutAffected. | Current dependencies are actor/origin cells plus lifecycle clear. Extend this owner for participants/source guards; preserve distinct query/source/battle/turn/command identities. |
| Atomic publication / A2,E2,F1 | `CS/CombatSystem/CombatTransitionPublisher.Publish` → `B/Core/BattleTransactions.Apply(CombatCommandTransition)`: prepare inventory, characters, soul field, command/environment/result, rewards/completion; validate query revision; install. `B/Character/CharacterBuffer.PrepareCommandChanges` validates distance; `B/Combat/CombatStateBuffer.PrepareCommand` calls MovementUndoPolicy.ValidatePublication. | Keep prepare/install order and handled-command replay guard. Only the initiator has a validated distance-decrease exception. `B/Character/CharacterData.cs` exposes canonical facts; no second live effect state. |
| Attack start/scopes / C1,E2 | `C/Application/Combat/CombatAttackCalculator.Calculate/Reaction/Execute`: admission/selection → CommandAttackProjection → AttackTraitScope → TraitAttackNotifications.Apply → delivery/Pull → AttackDeliveryExecutor.Execute. `HunterApplication.FinishAction` finishes the scope; `KillChainAttackCalculator.cs` uses independent child scopes. | Previews execute discarded projections too, so Execute entry alone is not committed use. AttackTraitScope.ForDamage/Finish owns captured Acceleration and ancestor reservation. Preserve actual reactions, which bypass normal admission/action settlement. |
| Numeric recipient/interception / C1,D2 | `C/Application/Combat/AttackDirectDamageCalculator.Apply/FlushHealth` uses SacrificeDamagePlanner.Plan/TryConsume, `CombatDamageCalculator.Attack` for actual recipient, then CombatDamageApplication.Apply. `WeaknessExposureApplication.CompleteHit` follows damage. | Shared Apply amplifies exposure before items/shields/HP; zero HP loss does not prove no use. Capture value and selected sources together. SacrificeDamagePlanner also simulates discarded damage, so transport must remain isolated until accepted publication. Grants after a hit cannot retroactively amplify it. |
| Other numeric paths / C1,D2 | `C/Application/Combat/BossRoarCalculator.cs` → CombatAttackCalculator.Reaction; KillChainAttackCalculator applies ExtraAttack; `C/Application/Artifact/ThornHeartAttackScope.cs` / ThornHeartReflection.cs use shared damage for reflection; `C/Application/Combat/MovementProjection.ApplyEffects` and AttackForcedMovement.cs route environment/mine/thorn damage to it. CombatDamageApplication.Defeat is separate. | Cover actual recipients, reactions, reflections and numeric environment paths. Instant defeat/falling must not fabricate multiplier use; preserve existing attribution/recovery boundaries. |
| Ground/Dust sources / C1 | `C/Application/Combat/MovementProjection.BeginOrdinaryMove` reports Ground RangeChanged only for paths exceeding base range, then emits origin trails; Enter emits reached-cell trails. TraitTrailCalculator.EmitAt/EndControllerTurn/Clear and CommandEnvironmentState.AddTraitTrail own source creation/lifetime/allocation. CombatActionAdmission.MovementAllowance and `C/Domain/Traits/Rules/GroundCompactionTraitRule.MovementBonusAt` read starting ground. DustScreenTraitRule.RangeReductionAt owns range reduction; CombatCommandEffects.CaptureIncoming picks one maximum defense trail with an ID tie-break. | Explicit departure evidence is needed for short moves before new ground is created. Capture all tied IDs without multiplying values; keep allocation monotonic and preserve overlaps. Dust defense currently supports OrdinaryAttack/ExtraAttack/ReactionAttack only. |
| Status/value/read facts / B1,D2,F1 | `C/Contracts/CombatEffects/AppliedStatus.cs`, StatusEffectSet.cs, CharacterStatusCompatibility.cs; `C/Domain/CombatEffects/Rules/WeaknessExposureRules.Apply/MaximumPercent/Amplify`, AppliedStatusRules.EndControllerTurn/OnAreaChanged; `C/Application/Combat/StatusEffectReadModel.Capture`; `C/Contracts/Snapshots/StatusEffectSnapshot.cs`; CharacterCombatFacts.SameValues/Rebuild/With*. | Current exposure is timed global max with logical same-source replacement. WeaknessExposureEffectSnapshot.MaximumIncreasePercent and movement damage-context comparisons require total/lifetime migration. Full publication compares complete facts; duration-only freshness must remain non-staling. |
| Passive shields / C2,F1 | `C/Application/Combat/TraitPresenceReconciler.Calculate/AfterTransition/ApplyAndRecord/EndAndRecord`, EnvironmentPresenceReconciler.cs; `C/Domain/Traits/Effects/SoloActionEffectRule.cs`, CohesionEffectRule.cs; `C/Domain/CombatEffects/Rules/ShieldStatusRules.Reconcile/Absorb`; `C/Domain/Traits/Rules/ShieldGrantRules.cs` is compatibility only. Canonical ShieldStatus stores RemainingBudget, Eligible, LastResetControllerTurnCount. | Missing prior eligible source creates full capacity. Preserve consumed/reset history through this existing owner; bounded passive inverse must not restore full recipients or unrelated spent budgets. |
| Reserve entries / F1 | `C/Application/Combat/CombatPlacementCalculator.Return` → Location(Hand) → Items/ItemEquipmentCalculator.RefundReturn → presence. `CombatTurnCalculator.EndNight` clears round items before RecallForNight, which routes living non-Main Field units through Location to player Hand/enemy EnemyWaiting. BattleCharacterCalculator initially creates Hand facts; RedeployEnemies leaves waiting via Location(Field). Whole-Systems search found no other gameplay reserve-entry constructor path. | Location currently removes exposure via OnAreaChanged, resets regeneration on field/reserve boundaries and sets Deferred action; it does not implement full new return cleanup. Main staying Field is not a return; initial battle construction is distinct. Preserve same-turn allowances deliberately. |
| Equipment and lifecycle distinction / F1 | `C/Application/Combat/Items/ItemEquipmentCalculator.RefundReturn` grants one equipped key to inventory and clears the slot; no key means no consumed-item refund. `CombatTurnCalculator.EndNight` first calls ItemLifetimeCalculator.ClearRoundItems(RoundEnded). CombatTemporaryStateCleanup.Clear clears death/battle effects, held Acceleration, StationaryFire and Hunter stage state. | Manual refund concerns item inventory, not gold. Retain night equipment policy. Do not use whole death/battle cleanup or Hunter stage reset for reserve return; failed inventory preparation must install nothing. |
| Persistent rewards/allowances / F1 | `C/Application/Combat/HunterApplication.TargetDied` proposes OwnedCharacterRecord.HunterAttackBonus and increments HunterTraitStatusSnapshot.SuccessfulRewards; Reconcile checks MaxRewards and retains count; ClearStage resets stage bookkeeping. TraitAttackNotifications owns side attack ordinals in TraitTurnFacts; SacrificeDamagePlanner.TryConsume owns enemy-turn activation limits; turn/action facts and regeneration retain current owners. | Preserve owned growth, caps, action/trigger/reset history, RNG/allocation. Reset temporary progress only; target/reaction reconciliation cannot refresh successful rewards/allowances. |

#### Registration, context and description checklist

- Existing authoring chain: `TA/LeapTraitAuthoring.cs` / LeapTraitData.cs (or GroundCompaction equivalents) → `CS/CharacterSystem/TraitSpecAdapter.cs` → `C/Contracts/Commands/Traits/*TraitSpec.cs` → definition factory in `C/Application/Combat/Traits/*TraitFactories.cs` → TraitRegistry.BuildDefinition/GetRequiredRegistration/CreateDefault. `C/Application/BootstrapModels/UnitCatalog.cs` consumes definitions; BattlePreparationCalculator/BattleCharacterCalculator build runtime states. New producer registration belongs to D1/E1, not Phase 0.
- Runtime descriptions are **not all in TraitRegistry**. `C/Application/Combat/Traits/TraitEffectProvider.CreateStatuses` maps definition/state to public trait status; `C/Application/Combat/CombatCommandEffects.Capture` maps status to effect state and applies returned changes. New stateless producers need existing mapping support and snapshot copy/equality coverage in `C/Contracts/Snapshots/Traits/*TraitStatusSnapshot.cs`.
- Owned descriptions: `C/Application/Roster/OwnedCharacterStatsCalculator.cs` → `C/Contracts/Snapshots/OwnedCharacterStatsSnapshot.cs` currently carries typed ThornField/HuntersMark/Pull/ImpactBolt settings plus key/availability facts; Leap/Ground have no separate owned settings description. Recon configuration must be considered at this actual consumer boundary, not assumed to appear from registry registration. `CS/CharacterSystem/TraitGrowthDefinitionAdapter.cs` carries normal/enhanced specs and authored names/descriptions; CharacterRosterService publishes corresponding offer text. No second catalog.
- Freshness/read consumers: MovementContextCalculator/MovementPreviewContext, StatusEffectReadModel/StatusEffectSnapshot, and `B/Character/CharacterData.cs` canonical projections. New exposure total/lifetime must propagate through them. View playback/UI remain later handoff consumers.

#### Locality assessment and exit

Observed current constraints match the approved design: ordinary undo intentionally rejects effectful movement; global-max exposure and single-winner Dust lose the source detail needed for later locks. Minimum later work is the planned bounded receipt/use transport, shared grouping/lifetime and existing transaction validators. No new state owner, generic rollback service, broad file split or Framework change was introduced.

| Concrete change | Current observed modification / understanding scope | Planned scope and verification; not yet proven |
| --- | --- | --- |
| Recon magnitude/radius | Producer absent; existing authoring/spec/factory/runtime-description chain must be understood to add it. | D1 configuration/trigger/description plus shared exposure/receipt contract; no Ground arithmetic change. Authoring/path/grant tests and full Phase exit gates remain required. |
| One new inverse family | Current checkpoint/publication rejects effects; direct-write/Buffer guards are shared. | Typed receipt, effect-owned inverse/use selection, common validation. Stale/late-failure/combination and native publication tests are necessary shared-contract verification. |
| Reserve temporary reset | Location, refund, night item cleanup, canonical shields and Hunter caps intersect; ordering is a real dependency. | Existing transition hooks plus one cleanup using existing owners. Return/redeploy/night/Main/equipment/growth tests remain necessary; locality does not justify dropping them. |

SSOT/SOLID/FW assessment is an inventory result, not an implemented improvement: retain one canonical status collection, one checkpoint state, effect-owned arithmetic and current prepare/install/Job authority. Explicit shared contracts and normal registration/validator changes are justified; duplicate state, per-effect services and speculative abstractions are not.

**Changed-file boundary:** this plan's status/checklist/evidence and new local `.superpowers/position-swap-recon-undo/` runner/evidence only. The plan/design were already untracked; the new evidence directory is also untracked and unstaged. All three unrelated tracked modifications remain byte-identical. Phase 0 changed no gameplay/test C#, metadata, asmdef, asset, scene, prefab, package, Framework or View file.

**Task 0.1 complete. Stop before Phase 1.** Integration failure/native diagnostics remain baseline exceptions. No Phase 1 implementation or View/PlayMode/manual acceptance is claimed.

### 5.2 Phase 1 — A1/A2/B1 execution evidence

**Authorization and boundary:** the user authorized Phase 1 in the existing checkout/branch, confirmed Phase 0 was already complete, and requested proportionate verification without duplicate tests. Phase 0 was not repeated. A1, A2 and B1 are complete; stop before Phase 2. No commit, push, production asset/pool, View, UI, scene, prefab, FieldCamera, Architecture, Framework service or external delivery change was made.

**Implemented:**

- A1: `MovementEffectId`, immutable participant/restoration details, and five bounded receipt families. Checkpoint/result lists are copied; existing scalar actor/token/placement/distance properties project the initiator. Ownership, duplicate participants/contributions/source IDs and passenger refund constraints reject malformed values. Keys 440/450 use the existing `ImplementationPending` policy, without executable producer registration.
- A2: `MovementEffectUndo` shares the candidate/guard path used by ordinary undo queries and execution. Paired/effect-bearing undo remains disabled. Final participant actions carry `MovementEffectUses` tied to actual event references in that transition; copies/composition retain those references. `MovementUndoPolicy.Reconcile` remains the command checkpoint owner and rejects unrelated or unsupported declarations. Direct-write evidence includes participant/source dependencies. Existing Buffer preparation, installation, query revision, replay protection and initiator-only distance exception were reused without another state store or formula.
- B1: exposure uses max per `(EffectOwnerKind, DefinitionKey)`, then checked sum. One immutable evaluation holds the effective percentage and all positive tied maxima, excluding suppressed/zero sources. Numeric damage retains that same evaluation when items/shields erase HP loss; zero input and instant defeat do not fabricate use. This prepares Phase 2 source capture without enabling effect locks.
- Exposure has explicit timed/untimed lifetime, a nullable timed clock and optional movement identity. HuntersMark's existing timed constructor, weaker/shorter replacement and affected-side clock remain. Different moves from one producer stay distinct; duplicate same-move contact is ignored; removal affects only that move's live entries. Untimed sources neither tick nor emit false expiry events. Existing recipient return/death/battle cleanup and legacy compatibility owners remain.
- Public read total is `WeaknessExposureEffectSnapshot.EffectiveIncreasePercent`; the misleading maximum name was removed. No current View dependency required an alias. Movement damage freshness compares this effective total but ignores duration-only differences; publication equality checks complete source/lifetime/identity facts. The [Phase 1 read contract](../handoffs/2026-10-31-position-swap-recon-undo-core-view-handoff.md) documents actual APIs and outstanding G2 work.

**Targeted tests:** reused existing ordinary undo, stale identity, distance, publication, replay and lifecycle cases instead of duplicating the plan's example names. RED exposed absent participant/usage contracts, cross-trait addition, checked sum and effective-total freshness. Final targeted results:

| Run | Passed / total | Boundary |
| --- | --- | --- |
| `phase1-a1-green-01` | 66 / 66 | Receipt copy/refund constraints and existing undo contracts/rules. |
| `phase1-a2-core-green-03` | 109 / 109 | Undo barriers/lifetime, rejected/speculative uses, copy/composition, pending keys. |
| `phase1-a2-integration-green-02` | 40 / 40 | Publication, shield-only action use, forged consumer/move/unsupported use, stale participant/receipt, late preparation failure and replay. |
| `phase1-b1-green-02` | 105 / 105 | Grouping/ties/removal, clocks/read copies, overflow, shields, freshness, HuntersMark, actual death/fall cleanup. |

Intermediate test-only corrections were namespace/value-type comparisons, an invalid empty-cell attack fixture changed to an actual protected enemy, EnemyWaiting's null reserve slot, and nullable tuple expectations. Death cleanup was tested at the existing command-finalization owner instead of incorrectly expected inside the low-level `Defeat` projection. The existing shape guard was updated after equality/copy/publication support. No behavior assertion was suppressed and no fixture was skipped.

**Final native gates:** Unity 6000.0.73f1, existing runner with approved AppData access, sequentially. XML totals equal actual test-case counts.

| Run / assembly | Passed / failed / skipped | Actual cases | Native exit |
| --- | --- | --- | --- |
| `phase1-final-core-01` / `ProjectL.Tests.Core` | 4,298 / 0 / 0 | 4,298 | 0 |
| `phase1-final-integration-01` / `ProjectL.Tests.CoreIntegration` | 412 / 1 / 0 | 413 | 2 |

Integration remains **non-green**. Its sole failure matches Phase 0's fullname and message exactly: `ProjectL.Tests.CoreIntegration.BossStagePreparationTests.ProductionBossStage_PreparesAndPlacesBoss` — `System.Collections.Generic.KeyNotFoundException : Stage data not found. StageKey: 2101.` There are no new failed tests. The existing 22 `SerializeReference` / `Refid -2` diagnostics remain; Core has none. All three `DataBaseAssetTests` passed inside the full integration suite, so no redundant asset-only run was added. Exact XML/log/native-exit evidence and comparison are recorded in `.superpowers/position-swap-recon-undo/phase1-final-summary.json` and the named run artifacts.

**Review and preservation:** one independent read-only review found no blocking correctness, architecture or scope issue. Its one stale maximum-versus-total comment was corrected as documentation only after the executable-state gates; no redundant test rerun was needed. Five new C# files have five unique .meta GUIDs, each present once in Assets. No missing metadata, new pure-Core Unity references, touched-C# CR/replacement characters or scoped whitespace errors were found. The three initial unrelated FMOD/specialization files retain identical SHA256 hashes. A separate concurrent .gitignore entry appeared during execution and was left untouched.

**Observed change locality:**

| Change | Modification / understanding scope | Necessary validation |
| --- | --- | --- |
| Exposure grouping/magnitude | The existing effect rule owns grouping and amplification; read/freshness consumers explicitly use the total. Other traits' movement/trail arithmetic is unchanged. | Group/tie/overflow/damage/read/lifetime tests; no View/content tests. |
| Cancellation extensions | Shared immutable receipt/participant contracts feed the existing checkpoint/publication owners. Each future family must supply its actual-use and bounded inverse guards before enabling cancellation. | Ordinary undo, malformed ownership/refund, speculative evidence and native stale/late-failure/replay tests. |
| Framework/Buffer integration | Factory -> internal Core service -> Core group -> Job and prepare/install ordering are unchanged. No new service, status owner or cancellation store. | Full Core/CoreIntegration gates, with the existing integration exception explicit. |

The review correctly left later execution unclaimed: Phase 2 effect locks/inverses, Phase 3 swap/paired gameplay, Phase 4 broader reserve-reset anti-abuse policy, and View/content/PlayMode/manual acceptance. The reviewer launched no duplicate tests; final native evidence is recorded above.

**Phase 1 complete. Stop before Phase 2.**


### 5.3 Phase 2 — C1/C2/D1/D2 execution evidence

**Authorization and boundary:** the user authorized Phase 2 after completed Phase 1, in the existing checkout/branch, retaining the request for proportionate verification and no commits/pushes. No baseline rerun was needed. No Phase 3 swap execution, Phase 4 broader reserve reset, View/UI/scene/prefab, production pool/asset, Framework service or external delivery work was performed.

**Implemented:**

- C1: Acceleration undo subtracts the exact added amount from the guarded held value. Created Ground/Dust trails deactivate by exact source ID without rewinding allocation or RNG. Preexisting Ground departure, actual Dust-restricted attack start, selected Dust defense and captured Acceleration damage uses commit originating moves. All positive tied sources participate; a consuming move's undo never reopens the older source checkpoint.
- C2: StationaryFire restores only the guarded moved flag. Cohesion/SoloAction restoration is bounded to recorded source status/eligibility and derived defense values, followed by existing candidate reconciliation. Unrelated recipient HP and independent shield budgets survive. Consumed/replaced contributions and unsupported candidate changes remain barriers.
- D1: ReconSquad key 450 is available through the existing authoring/data/spec/definition/runtime/effect registration chain. Defaults are 10 percent and orthogonal cross radius 1. Admitted start/reached cells, including legal Leap intermediates, grant once per hostile living field recipient; scan follows arrival effects and stops on death/battle termination. Forced/undo movement does not grant. Enemy voluntary movement uses ordinary source identity without a fabricated player token. Runtime and owned description projections expose resolved settings. PositionSwap 440 remains pending; no acquisition or enhanced assets were created.
- D2: undo removes only the originating move's live Recon grants. Earlier/other contributions and independent recipient changes remain; cleared entries are not resurrected. The actual numeric receiving evaluation retains all selected sources before shields, including held badges. Preview and Sacrifice projections do not install usage. Numeric execution evidence is carried in the existing immutable command transition; no new presentation event kind, global state owner or Buffer calculation was added.

**Review corrections:** one independent read-only reviewer found two Important defects, both reproduced by native phase2-review-red-01 (0/3): badge-protected positive hits lost Recon use, and removing a whole numeric-use group could reopen the source checkpoint. The correction pass added immutable MovementDamageExecution evidence (created only by Core.Application via Contracts friend access), required receiving-event coverage and derived-use validation, and preserved zero-input badge consumption without creating use. Direct attacks retain the existing early badge-consumption event order while passing pre-absorption numeric input and the already resolved protection result to the receiving owner. Multiple forced-thorn origins retain per-origin protection and one aggregate incoming/item calculation; only blocked input/origin evidence is passed additionally, with no proportional redistribution or second live damage owner.

**Targeted evidence:**

| Run | Passed / total | Boundary |
| --- | --- | --- |
| phase2-c1-green-02 | 92 / 92 | Existing movement effects, use barriers and ordinary undo. |
| phase2-c1-integration-02 | 46 / 46 | Native movement/trail publication and atomicity. |
| phase2-c2-green-02 | 132 / 132 | Stationary/passive inverse guards, unrelated state, occupancy and barriers. |
| phase2-c2-integration-01 | 17 / 17 | Native passive and movement publication. |
| phase2-d2-green-03 | 49 / 49 | Recon path, grouping, exact removal, six numeric kinds, tied/suppressed sources, interception/preview and forced/arrival ordering. |
| phase2-d-authoring-03 | 5 / 5 | Default/configured/invalid authoring, runtime and owned projections. |
| phase2-review-core-green-03 | 87 / 87 | Badge positive/zero receiving inputs, source uses, existing held behavior and corrected contract expectations. |
| phase2-review-integration-green-01 | 18 / 18 | Badge commitment, omitted entire use groups through copy/fresh constructor, native undo/stale/replay/late-failure publication. |

All listed runs have zero skips and native exit 0. Initial full Core results required migration of three exact registry counts after Recon registration and the trail evidence fixture from broad cell changes to observational summaries plus exact companion source IDs. A later full run exposed two unchanged event-order assertions; the direct attack order was restored in product code without relaxing those assertions. Compilation/setup attempts are not behavioral RED/GREEN evidence.

**Final source-contract correction:** a final audit of Pull found that the use validator expected full movement events, while Pull publishes its typed path before subsequent damage. Both arrival-hit and intermediate-thorn cases failed in phase2-pull-red-01 (0/2). The validator now uses the actual receiving position captured by Core-only numeric execution evidence, preserving source/event identity, live-field/owner and selected-source checks without reconstructing another movement feature. Both cases pass in the full Core gate below.

**Final native gates:** Unity 6000.0.73f1, existing approved-AppData runner, sequentially. XML totals equal actual test-case counts.

| Run / assembly | Passed / failed / skipped | Actual cases | Native exit |
| --- | --- | --- | --- |
| phase2-final-core-05 / ProjectL.Tests.Core | 4,335 / 0 / 0 | 4,335 | 0 |
| phase2-final-integration-02 / ProjectL.Tests.CoreIntegration | 428 / 1 / 0 | 429 | 2 |

Integration remains **non-green** solely for the baseline exception: `ProjectL.Tests.CoreIntegration.BossStagePreparationTests.ProductionBossStage_PreparesAndPlacesBoss` — `System.Collections.Generic.KeyNotFoundException : Stage data not found. StageKey: 2101.` Its fullname/message match Phase 0 exactly. There are no new failed tests or skips. The existing 22 SerializeReference / Refid -2 diagnostics remain. All three DataBaseAssetTests passed inside the full integration run; no redundant asset-only run was added. Exact XML/log/native-exit comparison is in `.superpowers/position-swap-recon-undo/phase2-final-summary.json` and the named run artifacts.

**Preservation and static checks:** all 17 new C# files have unique metadata GUIDs, each appearing exactly once across Assets metadata. No missing metadata, new pure-Core Unity reference, replacement character, CR line ending in changed C#, or scoped whitespace error was found. The four initial unrelated dirty files (.gitignore, two FMOD files and the separate specialization design) retain identical SHA256 hashes. Executable source hashes were unchanged during the final Core/Integration gates. Documentation-only completion reused those exact-state results; no further test run was needed.

**Change locality and ownership:** the Recon producer owns configuration/geometry/trigger timing and uses the shared exposure policy; changing its magnitude/radius requires no Ground/Dust arithmetic knowledge. A new inverse family legitimately changes typed receipts, use capture and shared validation, so dependency/source/atomicity regression remains necessary. Live values remain in canonical character/environment facts; inverse receipts and numeric execution witnesses are history/evidence. Existing Framework registration, Job authority, prepare/install order and initiator-only action/distance restoration are unchanged. Position participants remain conservatively guarded; Recon-only recipients use contribution-specific guards.

The [updated local handoff](../handoffs/2026-10-31-position-swap-recon-undo-core-view-handoff.md) documents effect removal, source use, query refresh, Recon settings and current single-participant output. G2's paired playback examples and final combined acceptance remain Phase 4. No View, content, PlayMode or manual acceptance is claimed.

**Phase 2 complete. Stop before Phase 3.** The user retains commit/push and external delivery control.

### 5.4 Phase 3 — E1/E2 execution evidence

**Authorization and boundary:** the user authorized Phase 3 after confirming Phase 1/2 completion, then explicitly requested proportionate direct verification. Continued in the same feature checkout and preserved the dirty Phase 1/2 implementation. Their baseline suites were not rerun separately. No Phase 4 return cleanup, final G2 handoff, View/UI, scenes/prefabs, production acquisition/enhanced assets, Framework service, commit, push or external delivery is included.

**E1 implemented:**

- PositionSwap key 440 is available through the existing authoring/data/spec/definition/factory/runtime/effect/status chain. It has no independent movement range or cost setting. Existing generic owned availability uses the registered key; there is no additional configuration description to duplicate. No production growth pool or unit asset was populated.
- Shared landing geometry permits an eligible living same-side single-cell endpoint occupant and validates the reverse origin while excluding only the two participants. Application admission retains current profiles, footprint support and forced-movement immunity. Range, preview and execution share these checks; intermediate traversal and Leap behavior remain unchanged. Occupied allied endpoints are destinations, not ordinary transit cells.
- `MovementPreviewContext.PositionSwapPassenger` captures immutable passenger facts; comparison rejects replacement identity before recalculating the preview and compares complete captured passenger facts. Runtime rechecks the captured passenger placement and reverse landing before the final step. Intermediate interruption keeps the accepted prefix and leaves the passenger untouched by the exchange.
- `MovementProjection.ExchangeEndpoint` settles both positions before arrival evaluation. The passenger moves forcibly to the initiator's original cell, preserving facing, action-use and stage distance. Only the initiator records voluntary path cells and creates voluntary movement traits/trails. Both final arrivals use existing terrain/hazard/object/soul/presence owners in EntityId order and stop at terminal outcomes; no overlap is exposed to those owners.
- Output reuses two typed `CombatCharacterEvent` movement facts with `CombatMovementCause` (Voluntary/Forced, TraitKey.PositionSwap), the initiator's ordinary path result, and the common PositionSwap/MovementChanged activation containing both targets/cells. The shared evidence reader validates companion movement facts before treating the activation as observational evidence. Actual movement facts remain authoritative.

**E2 implemented:**

- `MovementUndoPolicy` captures two existing `MovementUndoParticipant` receipts only for validated exchange movements, with action/distance restoration reserved to the initiator. The common inverse restores current bounded effects and both placements, then performs existing candidate-presence normalization. All participant guards/restoration cells are validated before publication; ordinary invalid-terrain and occupied-destination failure semantics are retained.
- Forward receipt validation replays both movement event chains while counting only initiator path steps. It checks the passenger's old endpoint, origin destination, unchanged rotation/action/distance, and final character restoration. Existing Acceleration/trail/StationaryFire/passive/Recon receipt dispatch remains the same owner; unsupported effects still block cancellation.
- Existing participant-action capture commits either participant's subsequent successful move/attack/reaction. Forced passenger relocation invalidates its older checkpoint; undo never recreates it. Queries/rejected actions do not lock and unrelated actions remain intact. Current `BattleTransactions`, CharacterBuffer and CombatStateBuffer validate/prepare/install both character changes through their existing transaction; no second state owner or Buffer gameplay formula was introduced.
- Undo emits two Returned facts and immutable `MovementUndoResult.Participants`, ordered with initiator first. It does not rerun arrivals, movement traits or reserve reset. Passenger action/distance remain unchanged; exact initiator distance is reverted once. Native late preparation failure, stale passenger and replay checks prove no half restoration or duplicate publication.

**Direct verification and corrections:** `phase3-behavior-red-02` selected 16 cases, with 13 behavioral failures for absent endpoint exchange/paired restoration. The three existing rejection cases already passed. Earlier missing-type/namespace/API fixture attempts are not behavioral evidence. After implementation, `phase3-core-green-03` passed 16/16. One additional direct arrival case reproduced a real bug: an earlier endpoint effect could force the passenger away, after which the second endpoint iteration evaluated generic arrival effects at the new location again. `phase3-arrival-red-01` failed 0/1; retaining the captured endpoint cell and skipping relocated participants fixed it. `phase3-core-green-04` passed 17/17. The final Core gate also includes a production MineMovementTileRule case proving passenger-origin mine activation occurs once against final pair positions and makes the move irreversible.

`PositionSwapAuthoringTests` and `PositionSwapPublicationTests` passed 5/5 in `phase3-integration-green-01`, covering actual authoring → adapter → runtime → exchange, immutable old snapshots, paired undo/replay, late preparation failure/stale second participant, and participant movement/query revision. Existing `MovementUndoLifetimeTests.OccupiedOriginRejectsThenUnblockingPreservesToken` supplies the shared temporary-block/requery assertion; no duplicate or impossible overlapping live-world swap fixture was invented. New paired restoration excludes the pair's current footprints; actual participant relocation/actions remain permanent locks.

Only obsolete registration expectations changed: distinct registrations 36 → 37 in Intimidation/Regeneration tests; active registrations 33 → 34 and keys >= 220 from 23 → 24 in retirement tests. Recon's old pending-swap assertion now expects availability. The now-obsolete single reserved-producer-unavailable case was removed; direct authoring/movement tests cover its newly executable behavior. Other retirement checks were retained.

**Final native gates:** Unity 6000.0.73f1, approved AppData runner, Core then CoreIntegration sequentially. No duplicate baseline or asset-only suite run. XML root totals match actual case counts.

| Run / assembly | Passed / failed / skipped | Actual cases | Native exit |
| --- | --- | --- | --- |
| `phase3-final-core-01` / `ProjectL.Tests.Core` | 4,352 / 0 / 0 | 4,352 | 0 |
| `phase3-final-integration-01` / `ProjectL.Tests.CoreIntegration` | 433 / 1 / 0 | 434 | 2 |

Integration remains **non-green** solely for the exact baseline failure: `ProjectL.Tests.CoreIntegration.BossStagePreparationTests.ProductionBossStage_PreparesAndPlacesBoss` — `System.Collections.Generic.KeyNotFoundException : Stage data not found. StageKey: 2101.` Fullname/message match Phase 2 exactly. No new failure or skip. The same 22 native SerializeReference `Refid -2` diagnostics remain; Core has zero. All three DataBaseAssetTests passed within the integration suite. Exact counts/failure comparison/log/XML/native exits are in `.superpowers/position-swap-recon-undo/phase3-final-summary.json` and the named artifacts.

**Review, locality and preservation:** one independent read-only review found no Critical/Important Phase 3 issue and ran no tests. The minor unused PositionSwapTraitSpec allocation in PositionSwapTraitDefinition, inherited from the Leap pattern, is recorded as a deferred cleanup only; it has no gameplay effect. View playback/content, Phase 4 reserve cleanup and general prior-phase correctness were deliberately outside that review; their acceptance remains pending with their owners.

The concrete change boundary is the PositionSwap producer/registration chain, shared landing/freshness/projection code, existing participant/evidence/inverse owners and directly related tests. A new movement trait does not require editing Ground/Dust/Recon arithmetic. Paired movement necessarily requires both participant guards and shared publication tests; this coupling is explicit in immutable receipts, not hidden in another manager. SSOT remains canonical character/environment facts and one checkpoint collection. SRP/OCP keep trait eligibility/settings with the producer and transaction authority with existing owners; ISP/DIP preserve the established assembly/Framework/Job direction without per-effect service interfaces.

There are 13 new C# files with 13 unique matching metadata GUIDs, each found exactly once across Assets metadata. No new pure-Core Unity references, replacement characters, scoped C# CR line endings or unexpected changes to pre-existing files were found. `.gitignore`, unrelated FMOD catalog/cache and legacy specialization design remain byte-identical to the Phase 3 start. Source/test hashes are unchanged across the final gates. Inventory compares both initial dirty paths and final paths because removing the old pending reservation makes DeferredCombatFeatures identical to HEAD while still being an intentional Phase 2 → Phase 3 change. Local evidence stays under the already-ignored directory; no evidence files appear in Git changes.

**Phase 3 complete. Stop before Phase 4.** Core paired exchange/restoration output is complete with the named baseline integration exception. Two-unit View playback/HUD/scene/manual acceptance and the final combined handoff remain unclaimed; the user owns commits, pushes and external delivery.

### 5.5 Phase 4 — F1/G1/G2 final execution evidence

**Authorization and boundary:** the user authorized Phase 4 after Phase 3 and retained the request to avoid unnecessary or indirect verification. Continued in the existing checkout/branch without rerunning earlier-phase baselines. Phases 0–4 are now implemented; prior sections retain their historical stop boundaries. No View/UI/scene/prefab, production acquisition/enhanced asset, Framework service, commit, push or external delivery work is included.

**F1 — common reserve return:**

- Added `CombatReserveReturnCleanup` at actual `CombatPlacementCalculator.Location` entry into Hand/EnemyWaiting. It clears canonical temporary recipient effects, held Acceleration, Regeneration cadence, StationaryFire/Roar progress, item effects and held environment contributions. Existing position/presence owners remove field-derived values. Night recall uses this same path; a Main remaining Field and field-to-field movement/swap/undo do not run it.
- Temporary attack/max-HP contributions are removed through the existing facts/rule owners, preserving residual permanent Hunter/soul contributions and stage records. Explicit optional temporary-bonus overrides in `CharacterCombatFacts.With`/rebuild retain copy/equality ownership. HP normalization emits complete state without numeric damage/healing/recovery/kill credit. Prior immutable snapshots remain unchanged.
- `AppliedStatusRules.ClearForReserve` retains each shield origin with zero budget, false eligibility and its existing reset count. Returning forfeits remaining visible budget without giving redeploy a fresh one; normal periodic reset remains the only refill boundary. No second shield-history store was introduced.
- Existing Deferred placement action policy, Sacrifice/same-turn trigger allowances, Hunter successful reward count/owned bonus, RNG/allocation and Run state remain. Hunter target clears on return and existing reconciliation rejects targets outside Field. Other recipients retain their own status lifetimes when a producer departs.
- Cleanup preserves equipment identity while removing temporary item effects. The existing manual `RefundReturn` refunds only an equipped key once and clears its slot; no consumed item or gold is refunded. Existing night `ClearRoundItems` policy remains. Failed inventory preparation publishes no cleanup, inventory, equipment, result or revision change.
- Ordered return facts describe relocation first, then full cleanup `TraitStateChanged`/summaries, followed by existing equipment handling when applicable. An intermediate event is not necessarily the final whole-character state. Field-to-field undo Returned facts do not enter reserve cleanup.

**G1 — combined behavior and publication:** new mixed fixtures cover Position Swap + Recon + Acceleration + Ground/Dust + StationaryFire + passive shielding, exact participant/distance restoration, unrelated contribution preservation, immutable old snapshots and monotonic trail allocation. Recipient return followed by undo cannot resurrect a removed grant. Swap undo preserves unrelated held/item/debuff state rather than calling reserve reset. Native combined cases reject stale second participants and late inventory/environment preparation changes without half inverse; successful publication and replay preserve once-only behavior.

Unchanged shared scenarios were reused in the final full Core gate: `MovementEffectUsageTests.ShortDeparture_UsesPreexistingPackedGround_AndConsumerUndoDoesNotReopenProducer`, `TiedDustMaxima_AllCommit_ButSuppressedTrailIsNotUsed`, and `ReconSquadUndoTests.UsedTiedSources_CommitButSuppressedSourceDoesNot`. Prior numeric-receiving/omission/late-publication fixtures run in the final full suites as well. These shared rules were not duplicated as another implementation or rerun as extra isolated gates.

**Direct evidence and migrated expectations:**

| Run | Passed / total | Meaning |
| --- | --- | --- |
| `phase4-f1-red-01` | 1 / 5 | Four behavioral failures reproduce missing reserve cleanup, temporary max-HP normalization and shield anti-refill behavior. |
| `phase4-f1-core-green-01` | 5 / 5 | Return reset, persistent history, shield redeploy and no numeric credit. |
| `phase4-combination-core-01` | 3 / 3 | Mixed inverse, recipient-return lifetime and field-to-field undo isolation. |
| `phase4-publication-01` | 7 / 9 | All mixed preparation cases pass; old Hunter target persistence and an intermediate-event equipment expectation fail. |
| `phase4-publication-green-02` | 9 / 9 | Native return/refund/preparation, mixed atomicity and Hunter reward-cap/growth preservation after migration. |

All direct runs have zero skips; green runs have native exit 0. The final Core gate includes a strengthened max-HP case with a position modifier before return. The new equipment test initially compared a pre-refund TraitState with final equipment; it now compares status at that event while separately checking final slot/inventory/replay.

The first full Core run (`phase4-final-core-01`, 4,351/4,359) exposed eight obsolete reserve-preservation assertions: Roar counter, combined debuffs, held badge source, recalled-night badge, HuntersMark copy/night, and Hand/EnemyWaiting exposure-policy cases. Those expectations were migrated without changing product code, retaining ordinary copy/turn behavior, original snapshots, Field Main and no-recharge checks. Earlier scoped migrations in `IntimidationStatePreservationTests`, `WeaknessExposureLifecycleTests` and `RegenerationCadenceTests` cover the same approved boundary.

The first full integration run (`phase4-final-integration-01`, 438/442) had the retained StageKey 2101 exception plus three obsolete assertions. `ArmorBreakerPublicationTests` and `TraitMovementAttackPublicationTests` now retain effects on ordinary turns and clear them on actual night reserve entry. `RegenerationPublicationTests` still verifies Field Main progress and returned-unit zero cadence, explicitly consuming relocation followed by its cleanup TraitState. Hunter night target expectations were migrated while preserving all eight cycles of permanent growth and stage reward-cap checks. No test is skipped/suppressed and no unsupported product behavior was introduced to satisfy old expectations.

**Final native gates:** Unity 6000.0.73f1, approved AppData runner, Core then CoreIntegration sequentially. After the final Core pass, only three CoreIntegration test assertions changed; runtime/Core test hashes remained identical, so the passing Core gate was reused and only integration reran. No extra baseline or asset-only run was added. XML root totals equal actual test-case counts.

| Run / assembly | Passed / failed / skipped | Actual cases | Native exit |
| --- | --- | --- | --- |
| `phase4-final-core-02` / `ProjectL.Tests.Core` | 4,359 / 0 / 0 | 4,359 | 0 |
| `phase4-final-integration-02` / `ProjectL.Tests.CoreIntegration` | 441 / 1 / 0 | 442 | 2 |

Integration remains **non-green** solely for `ProjectL.Tests.CoreIntegration.BossStagePreparationTests.ProductionBossStage_PreparesAndPlacesBoss` — `System.Collections.Generic.KeyNotFoundException : Stage data not found. StageKey: 2101.` Fullname/message exactly match Phase 3 and the original baseline. No new failure or skip remains. Integration retains the same 22 native SerializeReference `Refid -2` diagnostics; Core has zero. All three DataBaseAssetTests pass inside the full integration suite. Evidence/exit/failure comparison is retained in `.superpowers/position-swap-recon-undo/phase4-final-summary.json` and the named run artifacts, under the existing ignored directory.

**Review and change locality:** one independent read-only reviewer found no Critical, Important or actionable minor defect in F1/G1 and examined shared combinations; the reviewer ran no duplicate tests. General re-review of all prior phases, View playback and production content was outside that bounded review. The previously recorded Phase 3 unused spec allocation remains a deferred minor cleanup; it was not expanded into Phase 4.

| Change | Before / after modification and understanding scope | Required verification |
| --- | --- | --- |
| Change reserve temporary-reset policy | Existing manual/night placement owners had partial lifecycle behavior. Both now enter one cleanup using existing status/trait/item/position subrules; the changer reads those explicit preservation contracts instead of copying death/battle disposal or inventory internals. Hunter target validity remains with Hunter. | Actual Hand/EnemyWaiting entry, Field Main, shield/allowance preservation and existing return publication; no camera/render implementation knowledge. |
| Change Recon magnitude/radius | Remains in Recon authoring/spec/producer and shared exposure semantics; no Phase 4 edit to Ground/Dust/Acceleration formulas. | Producer geometry/group/lifetime checks; native publication when its contract changes. |
| Add a supported inverse family | Explicit typed receipt, use capture/validation and candidate reconciliation may change together. Character/environment facts remain the live owners, checkpoints and execution witnesses remain history/evidence. | Actual use, bounded removal, unrelated state preservation and atomic preparation are real shared dependencies. |
| Consume the final output | Existing Buffer query/result interfaces and immutable participant/source facts; no new per-effect service, View rollback or duplicate damage/status totals. | Receiving-owner presentation acceptance remains separate from Core gates. |

SSOT, SRP/OCP and dependency direction remain consistent with the approved Framework architecture. No new subclass obligation or broad service interface changes were added (LSP/ISP); the existing Factory → Core group → Job and Buffer prepare/install authority remain (DIP). Shared movement/cancellation validation legitimately knows participant/source contracts; it does not own another trait's formulas. File splitting was not used as an improvement metric.

**Static/preservation checks:** five new C# files have matching unique metadata GUIDs, each appearing once across Assets metadata. No missing metadata, Unity reference in changed pure Core code, replacement character, CR in scoped C#, scoped whitespace issue or unintended production asset/pool edit was found. Initial unrelated `.gitignore`, FMOD catalog/cache and legacy specialization design hashes are unchanged. Verified runtime/Core hashes match the passing Core gate; final runtime/integration hashes match the final integration gate. Local XML/log/exit evidence stays ignored. Branch and HEAD are unchanged.

**G2 delivered locally:** the [final Core-to-View handoff](../handoffs/2026-10-31-position-swap-recon-undo-core-view-handoff.md) replaces the interim Phase 1–2 contract. It documents actual query/signature/member names, paired Moved/Returned facts, initiator-only token/action/distance, source/lifetime/effective exposure reads, bounded removal, return event order, common activation/origin and once-only consumption. Concrete current consumer seams and reproduction cases identify pending View routing, paired playback, status/availability refresh and detach acceptance. No external message was sent; no View/content/PlayMode/manual acceptance is claimed.

**Phases 0–4 complete. Stop.** The named pre-existing integration/configuration exception remains open. The user retains commit/push, receiving-owner scheduling and external delivery control.
