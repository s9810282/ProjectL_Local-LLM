# Core Test Foundation: Bounded Case Disposition

**Current execution status (2026-10-26): Tasks 1–4 complete.** The initial disposition and per-task stop notices below retain their original historical meaning. The final Task 4 section provides the reconciled guarantee map and fresh completion evidence.

Date: 2026-10-26. Source inspected at `1ba7aec4728e1dfa5199a5f6043cce10dde636b5`. Companion to the [implementation plan](2026-10-26-core-test-foundation-recovery-plan.md).

These decisions are fixed for the proposed Phase; they are not executed deletions or claims of replacement-test success. Only listed methods/parameter cases are selected. All unlisted legacy cases are deferred unchanged. The revised destination is the independent CoreIntegration assembly, not repaired tests left under Framework. This avoids restoring ten complete classes or all 48 shared-helper consumers.

## Revised ownership and destination map

Stop the original Framework-folder Task 1 first; preserve its extracted helper and evidence. The historical execution section below remains unchanged and does not complete the new assembly gate. No migration has been performed by this document revision.

`F = Assets/Tests/EditMode/Framework/`; `I = Assets/Tests/EditMode/CoreIntegration/`. Source identities in D/R rows are the old `ProjectL.Tests.Framework` IDs. All destination classes use `ProjectL.Tests.CoreIntegration`; K1/K2 stay in `ProjectL.Tests.Core`.

| IDs | Destination / action | Source preservation |
|---|---|---|
| K3/K4 | `I/MovementPublicationTests.cs` | Split only the four configured cases from F; keep distinct environment tests in F |
| Existing `ConfiguredMovementBattle` and meaningful Task 1 probe | `I/ConfiguredMovementBattle.cs` | Move existing file/meta with GUID preserved; remove references to old test data/providers |
| C1/C2, D7 replacements | `I/CombatCommandServiceTests.cs` | Whole-file move/meta preservation if no deferred methods remain; D1 is retired, not transferred |
| C3/C4, D8 | `I/CombatAdvanceServiceTests.cs` | Same whole-file rule; use I-local setup |
| R1 | `I/CombatCommandPublicationTests.cs` | Split selected methods; retain deferred old cases/helpers in F |
| R2 | `I/CombatRewardPublicationTests.cs` | Split selected methods; preserve deferred reflection/roar cases in F |
| R3 | `I/CombatTransitionPublisherTests.cs` | Whole-file move may preserve GUID once external references are checked |
| R4 | `I/CombatCompletionPublicationTests.cs` | Split selected lookup-race cases from the mixed CoreStage fixture; do not import/copy its View tests or share CycleSource |
| R5 | `I/CombatItemPublicationTests.cs` | Split selected expected-item cases only |
| D1–D6 retirement/duplicate removals | Remove exact old cases in F under their stated conditions | Never delete neighboring unmapped cases or classes wholesale |

I owns minimal `CoreIntegrationTestData.cs` and `CoreIntegrationSnapshot.cs`. It has no references to other test assemblies. Existing Framework fixture data/graph classes are not imported to satisfy missing symbols. Only the new test asmdef/metas and the named Systems `InternalsVisibleTo` line are allowed boundary changes; existing pure Core and Framework test asmdefs remain unchanged.

## Existing coverage to keep

| ID | Executable coverage owner | Boundary and guarantees |
|---|---|---|
| K1 | `PlayerTurnAttackTests` / `PlayerTurnMovementTests` | Pure calculation: configured/shared cost, one ordinary action, invalid/zero-effect actions, weighted/diagonal paths and source immutability |
| K2 | `EnemyControllerPhaseTests` / `EightTurnNightTests` | Pure controller reset, end identity and eighth enemy-response rules. Constructed baselines do not establish consecutive live nights |
| K3 | `MovementPublicationTests.ConfiguredMovement_SoToOwnedStatsBattlePreviewAndPublication_CapturesLifetime` | Actual configured SO/stat/definition path, preview purity, installed move/cost/action, result sequence and captured lifetime |
| K4 | `MovementPublicationTests.ConfiguredMovement_RejectedRequestPreservesWorldAndPublishesOneRejection("budget"/"allowance"/"cost")` | Real stale movement rejection, store preservation, handled/result numbers and service replay suppression. Extend only with pending-input/ack assertions required by D5 |

K1/K2 remain `ProjectL.Tests.Core`. Existing methods below are written by their source class name; the destination map supplies their final CoreIntegration class/namespace (R4 changes class). Keep the whole pure Core suite; this table is not permission to remove unlisted pure tests.

## Delete or consolidate: exact cases

| ID | Existing case | Decision / replacement condition |
|---|---|---|
| D1 | `CombatCommandServiceTests.EndUnitTurnWithoutActor_ResolvesCurrentActiveActor` | Retired active-unit inference. Delete; the separate current player-end publication gap is C2, not a revival of this expectation |
| D2 | `CombatCommandPublicationTests.InvalidFieldOrTimeline_PublishesNeitherHealthNorResult(true)` | Retired timeline branch: current code no longer constructs an invalid timeline. Remove only `[TestCase(true)]`; retain the invalid-field false case deferred |
| D3 | `CombatCommandPublicationTests.InvalidInitialFieldOrTimeline_DoesNotInstallBattle(true)` | Same retired timeline branch at preparation. Remove only true; false remains a separate invalid-field guarantee, deferred |
| D4 | `CombatCommandPublicationTests.ProcessingService_NoInputPublishesNothing` | Duplicate no-command service guarantee. Delete only after C1's no-command branch checks complete store/counter preservation and passes |
| D5 | `CombatCommandPublicationTests.ProcessingService_HandlesRejectionOnceAndNeverAcknowledgesOrRemovesViewInput` | Partial overlap with K4. K4 already checks rejection, one publication and no reprocessing; add queued-input and ack invariants, then delete this duplicate. Preserve direct Buffer replay separately in R1 |
| D6 | `CoreStageCombatCycleTests.HeldEffects_NightEndExpiresSuppliesWhileMainStaysFieldAndKeepsBadge` | Pure expiry is covered by K2. Consolidate badge/live-publication/successive-night residuals into C4, then delete this method and exclusively used ShortNight/SeedHeldMain setup |
| D7 | `CombatCommandServiceTests.ProcessesNextCommandOnce_AndDoesNotReprocessHandledInput` | Replace in this class with explicit accepted-attack settlement/replay assertions (C2). Old method does not assert acceptance; a rejected-Move test alone is not its complete replacement |
| D8 | `CombatAdvanceServiceTests.Automatic_AdvancesActiveTurnWithoutReadingViewCommands` | Rename/adapt as C3's controller-start integration; preserve pending View command and handled-number assertions. This is current coverage, not an obsolete test to discard |

No legacy class is scheduled for wholesale disposal. A whole-file relocation preserves its `.meta`/GUID; a split gets a new meta and removes only transferred/retired source methods. D2/D3's sibling cases are not included. Replacement checks must pass in CoreIntegration before D4–D7 removals; a baseline setup failure is not deletion evidence.

## Current service/controller gaps to execute

| ID | Cases / final owner | Exact retained or added guarantees |
|---|---|---|
| C1 | `CombatCommandServiceTests.NoCommandOrNoBattle_ReturnsFalseWithoutPublication`; `RunEnded_ReturnsFalseWithoutConsumingCommand` | Retain on valid current context; snapshot all stores/counters, pending input and result history. No command, no battle and stopped Run are distinct branches |
| C2 | `CombatCommandServiceTests.AcceptedAttack_PublishesAndSpendsExactlyOnce`; `EndPlayerTurn_PublishesActorFreeBoundaryOnce` (new names replacing D7/D1) | Accepted attack installs cost/action/world once; player end has Empty actor, one increment, no refill and StartingEnemy. Stale end cannot terminate a later turn |
| C3 | `CombatAdvanceServiceTests.NotAutomatic_ReturnsFalseWithoutPublication`; `RunEnded_ReturnsFalseWithoutPublication`; `Automatic_StartsControllerWithoutConsumingViewCommands` (D8); `EnemyAction_UsesNoPlayerBudgetAndPreservesPendingInput` (new) | Real automatic start/action through services, pending input isolation, enemy actor/action publication and unchanged player funds |
| C4 | `CombatAdvanceServiceTests.TwoNights_ExpireSuppliesAfterEnemyResponseAndPreserveBadge`; `TerminalAction_PreventsFurtherControllerPublication` (new) | Actual two-night lifecycle, no-action enemy completion, precise before/after response-8 cleanup, Main/badge preservation, monotonic controller identity and reset night-local progress. Separate terminal case proves no later start/refill/result |

## Selected unique transaction/publication checks

The following have distinct store/transaction guarantees not supplied by K1–K4. Retain the listed method names/parameters while moving them to the mapped CoreIntegration class; repair only their setup/proposals. Use I-local production-service bindings and snapshot assertions, not old Framework test helpers. This is a source-level boundary comparison, not an exhaustive non-overlap claim about unrelated suites.

| ID | Exact existing methods (all listed parameter cases) | Why kept / minimal adaptation |
|---|---|---|
| R1 | `CombatCommandPublicationTests.Publication_DifferentOwnedLevelRejectsWithoutWrites(false/true)`; `Publication_DifferentOwnedExperienceRejectsWithoutWrites`; `Publication_OwnedChangeInstallsWithCommandAtomically(false/true)`; `Publication_AfterOwnedChange_NextInputMustReadInstalledRecord`; `Rejection_PublishesOnceWithoutGameplayChanges` | Expected owned state, combined owned/soul installation, following reads and direct `BattleWriteSink.Apply` rejection replay. New service replay stops earlier and cannot replace that direct Buffer check. Use valid captured identity/settlement, a passing control and a single targeted corruption |
| R2 | `CombatRewardPublicationTests.TwoAcceptedKills_PayImmediatelyOnce_ThenVictoryAndCleanupDoNotRepay`; `InvalidPublication_LeavesEveryStoreUnchanged("staleGold"/"missingChange"/"wrongDelta"/"wrongExpected"/"fameGrant"/"missingCompletion"/"wrongNode")`; `NonrewardCommandWithStaleBalance_IsAlsoRejected` | Immediate/final reward continuity, duplicate payout protection and cross-store rejection. Actual eligible actions replace consecutive same-unit attacks; snapshot every affected store and require the correct guard |
| R3 | `CombatTransitionPublisherTests.TerminalTransition_AttachesCompletionWithoutResettling`; `RunEndedDuringResolution_DoesNotPublish`; `RejectedProgression_DoesNotPublish`; `NonterminalAndRejectedCommand_PreservePublicationContract(true/false)` | Publisher/planner call ordering, no resettlement and terminal publication refusal. Pure terminal facts and movement publication do not execute this collaboration. Replace invalid synthetic accepted attacks with valid calculator results |
| R4 | `CoreStageCombatCycleTests.FinalCompletionLookupEndsRun_ReturnsFalseWithoutPublication("Command"/"Advance"/"Abandon")` | Actual DB lookup changes Run state during terminal resolution. R3 uses a coordinator response stub and does not substitute for this race. Keep all three variants; replace illegal move-then-attack/disabled railway triggers, not the no-publication assertions |
| R5 | `CombatItemPublicationTests.ChangedExpectedItemFacts_RejectBeforeInstallingAnyProposal("Inventory"/"Slot"/"Effect"/"Random")`, plus new `"Unchanged"` control | Movement/turn/reward tests do not replace validation of expected item inventory, slot, effects or RNG. Current setup uses deferred Rope and stale identity. Use a supported starting item and real enemy phase, preserving each corruption's guard and all-or-none character/item publication |

## Explicit deferrals and non-equivalence decisions

- `MovementPublicationTests` HighGround, hidden-mine, environment progress/bindings, occupant, health, battle-reuse, position-modifier and ordered-event cases: defer unchanged. K3/K4 do not cover all their distinct environmental conditions; do not delete them as generic movement duplicates. The current task establishes the common action/transaction foundation, not full environment acceptance.
- Other `CombatCommandPublicationTests`, including overflow, identity/structure/soul/obstacle corruption, field-only D2/D3 siblings, chalice receipts, composed effects and result-history cleanup: defer unchanged. Do not classify an arbitrary exception as proof of their intended invariant.
- Reward reflection/roar and `PreviewOverflowAndAbsentBalance_DoNotMutateStores`: defer unchanged; not replaced by ordinary reward R2.
- Other item publication/equipment/lifetime cases: defer unchanged. R5 owns only the selected expected-item-source boundary; it is not a full item-feature migration.
- Other `CoreStageCombatCycleTests` / `RecoveryStageCycleTests`, including WinCurrentBattle callers, recovery/next-stage flows and View-input integration: defer unchanged. The current rule tests do not prove those complete flows. No repair of WinCurrentBattle solely to clear unrelated failures.
- `CombatStateBufferTests`, `CombatPreviewPublicationTests`, `CombatServiceCompositionTests` and their existing failures: retain/defer as a group outside the selected edit gate. K3 proves a real successful preparation/start and preview path, but does not prove every malformed-draw/health or registration-order case. No whole-class replacement claim is made.
- `CharacterActionCostRulesTests.UnitContracts_DoNotExposeABalanceOrTimelineClock` and other migrated pure tests: retain. Old filenames do not identify old semantics.

## Execution ledger requirements

For every D/C/R row record old and new fully qualified NUnit names, baseline result, final owner, final result/log and relocated/removed/added counts. Relocation is not lost coverage. Retired D1–D3 are distinguished from replacement-dependent removals. Record Core and CoreIntegration execution durations and test discovery; neither selection may execute View/scene tests. The implementer must report source drift that invalidates this mapping; unlisted cleanup or a full Framework rerun is not automatic.

No test has been deleted, rewritten or run while preparing these decisions. Replacement equivalence is conditional on executing the stated assertions; successful current XML does not prove tests that have not yet been written.

> Historical evidence below belongs to the original Task 1, before the assembly-separation revision. Preserve it as-is. Its authorization/results do not establish completion or permission to continue under the revised Task 1. The documented full Framework crash is outside the new normal execution gate.

## Task 1 execution evidence (2026-10-26)

Authorization: the user requested Task 1 only. Tasks 2–4, all deletions and all replacement-test changes remain unexecuted. The earlier planning-only statements describe document preparation, not this authorized execution.

HEAD: `1ba7aec4728e1dfa5199a5f6043cce10dde636b5`; branch: `feature/Cheon/Core/NewCombatSytem`. Initial status contained only the three supplied untracked design/plan/disposition documents. The user selected the current checkout.

| Fresh baseline | Discovered | Passed | Failed | Skipped | Evidence |
|---|---:|---:|---:|---:|---|
| Core | 3540 | 3540 | 0 | 0 | `.superpowers/core-test-foundation/core-baseline.xml` / `core-baseline.log` |
| Selected filter + full Movement fixture | 65 | 5 | 60 | 0 | `.superpowers/core-test-foundation/selected-baseline.xml` / `selected-baseline.log` |
| Selected existing cases below | 44 | 5 | 39 | 0 | `selected-baseline-cases.tsv` |

The optional full Framework run crashed before XML in Unity graphics/URP (`GfxDevice::DrawSharedGeometryJobs`, `DrawUtil::DrawLineOrTrailMultipleFromNodeQueue`, `RenderSingleCamera`); `.superpowers/core-test-foundation/framework-baseline.log` is preserved. No full Framework counts or green status are claimed. This happened before code edits; the font atlas modified by that run is restored to its initial state. The planned focused method filter was then executed successfully to completion (including expected test failures). No Unity upgrade/cache deletion/renderer changes were made.

Full parameter-level discovery/results of the completed focused run are retained in `.superpowers/core-test-foundation/selected-run-baseline-cases.tsv`; every failure name and exact message is retained in `selected-run-baseline-failures.tsv` and the XML (including stack traces). These are new executed baselines, not the historical 99/89 totals. Deferred failures do not authorize extra repairs.

### Selected existing case ledger

Names below are prefixed with `ProjectL.Tests.Framework.`. The failure column preserves the exact message (line breaks shown as `<br>`); XML remains the source for complete diagnostics. D2/D3 include only `True`; their `False` siblings remain outside the selected set. K1/K2 are retained within the full Core run. New C2/C3/C4/R5-control names do not exist yet and have no execution claim.

| ID | Expanded case | Baseline | Exact failure message |
|---|---|---|---|
| K3 | `MovementPublicationTests.ConfiguredMovement_SoToOwnedStatsBattlePreviewAndPublication_CapturesLifetime` | Passed | — |
| K4 | `MovementPublicationTests.ConfiguredMovement_RejectedRequestPreservesWorldAndPublishesOneRejection("budget")` | Passed | — |
| K4 | `MovementPublicationTests.ConfiguredMovement_RejectedRequestPreservesWorldAndPublishesOneRejection("allowance")` | Passed | — |
| K4 | `MovementPublicationTests.ConfiguredMovement_RejectedRequestPreservesWorldAndPublishesOneRejection("cost")` | Passed | — |
| D1 | `CombatCommandServiceTests.EndUnitTurnWithoutActor_ResolvesCurrentActiveActor` | Failed | System.InvalidOperationException : 새 전투는 빈 공유 예산과 대기 행동 상태로 준비되어야 합니다. |
| D2 | `CombatCommandPublicationTests.InvalidFieldOrTimeline_PublishesNeitherHealthNorResult(True)` | Failed | System.InvalidOperationException : 새 전투는 빈 공유 예산과 대기 행동 상태로 준비되어야 합니다. |
| D3 | `CombatCommandPublicationTests.InvalidInitialFieldOrTimeline_DoesNotInstallBattle(True)` | Failed | System.InvalidOperationException : 새 전투는 빈 공유 예산과 대기 행동 상태로 준비되어야 합니다. |
| D4 | `CombatCommandPublicationTests.ProcessingService_NoInputPublishesNothing` | Failed | System.InvalidOperationException : 새 전투는 빈 공유 예산과 대기 행동 상태로 준비되어야 합니다. |
| D5 | `CombatCommandPublicationTests.ProcessingService_HandlesRejectionOnceAndNeverAcknowledgesOrRemovesViewInput` | Failed | System.InvalidOperationException : 새 전투는 빈 공유 예산과 대기 행동 상태로 준비되어야 합니다. |
| D6 | `CoreStageCombatCycleTests.HeldEffects_NightEndExpiresSuppliesWhileMainStaysFieldAndKeepsBadge` | Failed | Expected: 0<br>  But was:  7 |
| D7 | `CombatCommandServiceTests.ProcessesNextCommandOnce_AndDoesNotReprocessHandledInput` | Failed | System.InvalidOperationException : 새 전투는 빈 공유 예산과 대기 행동 상태로 준비되어야 합니다. |
| D8/C3 | `CombatAdvanceServiceTests.Automatic_AdvancesActiveTurnWithoutReadingViewCommands` | Failed | System.InvalidOperationException : 새 전투는 빈 공유 예산과 대기 행동 상태로 준비되어야 합니다. |
| C1 | `CombatCommandServiceTests.NoCommandOrNoBattle_ReturnsFalseWithoutPublication` | Failed | System.InvalidOperationException : 새 전투는 빈 공유 예산과 대기 행동 상태로 준비되어야 합니다. |
| C1 | `CombatCommandServiceTests.RunEnded_ReturnsFalseWithoutConsumingCommand` | Failed | System.InvalidOperationException : 새 전투는 빈 공유 예산과 대기 행동 상태로 준비되어야 합니다. |
| C3 | `CombatAdvanceServiceTests.NotAutomatic_ReturnsFalseWithoutPublication` | Failed | System.InvalidOperationException : 새 전투는 빈 공유 예산과 대기 행동 상태로 준비되어야 합니다. |
| C3 | `CombatAdvanceServiceTests.RunEnded_ReturnsFalseWithoutPublication` | Failed | System.InvalidOperationException : 새 전투는 빈 공유 예산과 대기 행동 상태로 준비되어야 합니다. |
| R1 | `CombatCommandPublicationTests.Publication_DifferentOwnedLevelRejectsWithoutWrites(False)` | Failed | System.InvalidOperationException : 새 전투는 빈 공유 예산과 대기 행동 상태로 준비되어야 합니다. |
| R1 | `CombatCommandPublicationTests.Publication_DifferentOwnedLevelRejectsWithoutWrites(True)` | Failed | System.InvalidOperationException : 새 전투는 빈 공유 예산과 대기 행동 상태로 준비되어야 합니다. |
| R1 | `CombatCommandPublicationTests.Publication_DifferentOwnedExperienceRejectsWithoutWrites` | Failed | System.InvalidOperationException : 새 전투는 빈 공유 예산과 대기 행동 상태로 준비되어야 합니다. |
| R1 | `CombatCommandPublicationTests.Publication_OwnedChangeInstallsWithCommandAtomically(False)` | Failed | System.InvalidOperationException : 새 전투는 빈 공유 예산과 대기 행동 상태로 준비되어야 합니다. |
| R1 | `CombatCommandPublicationTests.Publication_OwnedChangeInstallsWithCommandAtomically(True)` | Failed | System.InvalidOperationException : 새 전투는 빈 공유 예산과 대기 행동 상태로 준비되어야 합니다. |
| R1 | `CombatCommandPublicationTests.Publication_AfterOwnedChange_NextInputMustReadInstalledRecord` | Failed | System.InvalidOperationException : 새 전투는 빈 공유 예산과 대기 행동 상태로 준비되어야 합니다. |
| R1 | `CombatCommandPublicationTests.Rejection_PublishesOnceWithoutGameplayChanges` | Failed | System.InvalidOperationException : 새 전투는 빈 공유 예산과 대기 행동 상태로 준비되어야 합니다. |
| R2 | `CombatRewardPublicationTests.TwoAcceptedKills_PayImmediatelyOnce_ThenVictoryAndCleanupDoNotRepay` | Failed | System.InvalidOperationException : 새 전투는 빈 공유 예산과 대기 행동 상태로 준비되어야 합니다. |
| R2 | `CombatRewardPublicationTests.InvalidPublication_LeavesEveryStoreUnchanged("staleGold")` | Failed | System.InvalidOperationException : 새 전투는 빈 공유 예산과 대기 행동 상태로 준비되어야 합니다. |
| R2 | `CombatRewardPublicationTests.InvalidPublication_LeavesEveryStoreUnchanged("missingChange")` | Failed | System.InvalidOperationException : 새 전투는 빈 공유 예산과 대기 행동 상태로 준비되어야 합니다. |
| R2 | `CombatRewardPublicationTests.InvalidPublication_LeavesEveryStoreUnchanged("wrongDelta")` | Failed | System.InvalidOperationException : 새 전투는 빈 공유 예산과 대기 행동 상태로 준비되어야 합니다. |
| R2 | `CombatRewardPublicationTests.InvalidPublication_LeavesEveryStoreUnchanged("wrongExpected")` | Failed | System.InvalidOperationException : 새 전투는 빈 공유 예산과 대기 행동 상태로 준비되어야 합니다. |
| R2 | `CombatRewardPublicationTests.InvalidPublication_LeavesEveryStoreUnchanged("fameGrant")` | Failed | System.InvalidOperationException : 새 전투는 빈 공유 예산과 대기 행동 상태로 준비되어야 합니다. |
| R2 | `CombatRewardPublicationTests.InvalidPublication_LeavesEveryStoreUnchanged("missingCompletion")` | Failed | System.InvalidOperationException : 새 전투는 빈 공유 예산과 대기 행동 상태로 준비되어야 합니다. |
| R2 | `CombatRewardPublicationTests.InvalidPublication_LeavesEveryStoreUnchanged("wrongNode")` | Failed | System.InvalidOperationException : 새 전투는 빈 공유 예산과 대기 행동 상태로 준비되어야 합니다. |
| R2 | `CombatRewardPublicationTests.NonrewardCommandWithStaleBalance_IsAlsoRejected` | Failed | System.InvalidOperationException : 새 전투는 빈 공유 예산과 대기 행동 상태로 준비되어야 합니다. |
| R3 | `CombatTransitionPublisherTests.TerminalTransition_AttachesCompletionWithoutResettling` | Failed | System.InvalidOperationException : 새 전투는 빈 공유 예산과 대기 행동 상태로 준비되어야 합니다. |
| R3 | `CombatTransitionPublisherTests.RunEndedDuringResolution_DoesNotPublish` | Failed | System.InvalidOperationException : 새 전투는 빈 공유 예산과 대기 행동 상태로 준비되어야 합니다. |
| R3 | `CombatTransitionPublisherTests.RejectedProgression_DoesNotPublish` | Failed | System.InvalidOperationException : 새 전투는 빈 공유 예산과 대기 행동 상태로 준비되어야 합니다. |
| R3 | `CombatTransitionPublisherTests.NonterminalAndRejectedCommand_PreservePublicationContract(True)` | Failed | System.InvalidOperationException : 새 전투는 빈 공유 예산과 대기 행동 상태로 준비되어야 합니다. |
| R3 | `CombatTransitionPublisherTests.NonterminalAndRejectedCommand_PreservePublicationContract(False)` | Failed | System.InvalidOperationException : 새 전투는 빈 공유 예산과 대기 행동 상태로 준비되어야 합니다. |
| R4 | `CoreStageCombatCycleTests.FinalCompletionLookupEndsRun_ReturnsFalseWithoutPublication("Command")` | Failed | Expected: False<br>  But was:  True |
| R4 | `CoreStageCombatCycleTests.FinalCompletionLookupEndsRun_ReturnsFalseWithoutPublication("Advance")` | Failed | Expected: False<br>  But was:  True |
| R4 | `CoreStageCombatCycleTests.FinalCompletionLookupEndsRun_ReturnsFalseWithoutPublication("Abandon")` | Passed | — |
| R5 | `CombatItemPublicationTests.ChangedExpectedItemFacts_RejectBeforeInstallingAnyProposal("Inventory")` | Failed | System.InvalidOperationException : 새 전투는 빈 공유 예산과 대기 행동 상태로 준비되어야 합니다. |
| R5 | `CombatItemPublicationTests.ChangedExpectedItemFacts_RejectBeforeInstallingAnyProposal("Slot")` | Failed | System.InvalidOperationException : 새 전투는 빈 공유 예산과 대기 행동 상태로 준비되어야 합니다. |
| R5 | `CombatItemPublicationTests.ChangedExpectedItemFacts_RejectBeforeInstallingAnyProposal("Effect")` | Failed | System.InvalidOperationException : 새 전투는 빈 공유 예산과 대기 행동 상태로 준비되어야 합니다. |
| R5 | `CombatItemPublicationTests.ChangedExpectedItemFacts_RejectBeforeInstallingAnyProposal("Random")` | Failed | System.InvalidOperationException : 새 전투는 빈 공유 예산과 대기 행동 상태로 준비되어야 합니다. |

## Revised Task 1 execution (2026-10-26)

The user explicitly authorized revised Task 1 after inspecting the changed plan. Work reused the stopped original task in the user-selected checkout at HEAD `1ba7aec4728e1dfa5199a5f6043cce10dde636b5`. Original XML/logs and the failed/crashed broad-run evidence are preserved. No Framework/View tests were rerun for this revised task. Tasks 2–4 and D1–D8 replacement/retirement work remain pending.

### Implemented boundary

- Created `Assets/Tests/EditMode/CoreIntegration/ProjectL.Tests.CoreIntegration.asmdef` with exactly the four planned production references, Editor-only NUnit configuration and root namespace `ProjectL.Tests.CoreIntegration`. Existing Core/Framework asmdefs are unchanged.
- Moved `ConfiguredMovementBattle.cs` and its existing lifecycle probe with meta GUID `afb9915ca62b4f678652c7e55256d9ea` preserved. Split K3/K4 and `SetRiderMovement` into the new movement file with a new meta. Retained the old mixed movement file/meta and every deferred method body.
- Replaced the old test data/provider links with `CoreIntegrationTestData`: five required unit DTOs, one loadout, the same 8x6 board/deployment values, authored enemy/HQ placements, category configuration, targeting, economy and map facts. It delegates definition conversion, roster acquisition, deck validation, entry, preview, commands and advancement to existing production contracts. Unsupported View/render/recovery/random-loadout queries throw; no such systems are initialized.
- Retained the optional initialization source, StartingPlayer stop and bounded real automatic advancement interface. Preserved the useful probe assertions for source forwarding, empty AP/Deferred preparation, watchdog diagnostics/no publication, exact one-step player start, no-op while Player and actual enemy-to-player progress.
- Production source change is exactly `[assembly: InternalsVisibleTo("ProjectL.Tests.CoreIntegration")]` in Systems AssemblyInfo. Existing friend declarations remain. No Framework/Core friend widening, production rules, state stores, scenes, assets, factories, jobs or service keys changed.

### Executed results and timing

| Assembly | Discovered / passed | Failed / skipped | NUnit run seconds | Process wall seconds | Script compile seconds | Asset refresh seconds | Project load seconds |
|---|---:|---:|---:|---:|---:|---:|---:|
| CoreIntegration | 5 / 5 | 0 / 0 | 0.5744511 | 31.091 | 11.104 | 20.452 | 24.409 |
| Core | 3540 / 3540 | 0 / 0 | 9.6513834 | 29.095 | 0.931 | 8.250 | 12.244 |

Each assembly ran separately with `-assemblyNames`, Unity 6000.0.73f1, `-batchmode -nographics -runTests -testPlatform EditMode`, using the AppData-capable approved execution path. Both processes exited 0. Evidence: `.superpowers/core-test-foundation/revised-task1-integration.xml` / `.log` / `-process.json`, and corresponding `revised-task1-core` files. Compile/refresh/load values are Editor log measurements and overlap; they must not be added together. Process wall time includes startup/import/compilation/test/exit overhead; NUnit execution time is reported separately. This does not claim project compilation is isolated from existing Systems transitive dependencies.

### Parameter-level relocation ledger

Old names below are prefixed with `ProjectL.Tests.Framework.`; new names with `ProjectL.Tests.CoreIntegration.`. `.superpowers/core-test-foundation/revised-task1-relocations.tsv` contains full old/new identities. All five old cases passed in `task1-lifecycle-final.xml` before the revision; source HEAD did not drift, so no baseline rerun was required.

| Old/new class and expanded method (namespace change only) | New result | Seconds |
|---|---|---:|
| `ConfiguredMovementBattleTask1Probe.SourceStartWatchdogAndRealProgress` | Passed | 0.432700 |
| `MovementPublicationTests.ConfiguredMovement_RejectedRequestPreservesWorldAndPublishesOneRejection("budget")` | Passed | 0.049320 |
| `MovementPublicationTests.ConfiguredMovement_RejectedRequestPreservesWorldAndPublishesOneRejection("allowance")` | Passed | 0.003710 |
| `MovementPublicationTests.ConfiguredMovement_RejectedRequestPreservesWorldAndPublishesOneRejection("cost")` | Passed | 0.003175 |
| `MovementPublicationTests.ConfiguredMovement_SoToOwnedStatsBattlePreviewAndPublication_CapturesLifetime` | Passed | 0.013968 |

Count reconciliation: four configured-movement cases relocated; one existing probe relocated; zero semantic cases added, deleted or skipped. The mixed Framework movement source retains its 21 other parameter-level cases unchanged. No replacement-dependent deletion is claimed. Full Core names/counts match the earlier 3540-case baseline exactly.

### Scope and dependency audit

`verify-revised-task1.ps1` compares expanded old/new case identities, pure Core discovery, unchanged migrated movement bodies, unchanged deferred movement source, the helper/probe transformation, the moved GUID, exact assembly references, C#/asmdef meta pairs and LF encoding. It passed. Searches in CoreIntegration found no old test namespaces/classes, cross-test assembly references, scene/asset loading, Play-mode tests or View/render initialization. The required production `IDataBaseQueryContext` View/render property types are declarations only and throw if invoked.

Before this change, configured movement depended on private/mixed Framework test fixtures and ordinary verification could enter View rendering. After this change, selected consumers need the local data/fixture contracts and existing production APIs; actual assembly-filtered discovery contained only the five CoreIntegration cases. Setup remains one fixture and one local input provider. Future command/advance reuse is planned, not yet implemented. Existing shared helper behavior was not changed, so this task does not claim to repair or reverify their deferred consumers.

`git diff --check` passed. Production diff inspection contains only the declared Systems friend line; existing test asmdefs/shared fixtures are unchanged. The earlier automatic font-atlas content was restored to its HEAD bytes. No commit or push was performed. Revised Task 1 is complete; stop before Task 2.

## Task 2 execution (2026-10-26)

Authorization: the user confirmed Task 1 complete and requested Task 2 only. HEAD remains `1ba7aec4728e1dfa5199a5f6043cce10dde636b5` on `feature/Cheon/Core/NewCombatSytem`. Work continued in the existing checkout, preserving Task 1 changes and the font asset content present at Task 2 start. Tasks 3–4 remain pending. No commit or push was performed.

### Implemented guarantees and removals

All new owners below are in `ProjectL.Tests.CoreIntegration`. The exact old/new fully qualified names, baseline/final results and per-case durations are recorded in `.superpowers/core-test-foundation/task2/case-ledger.tsv`; D5 has three replacement-owner rows. The earlier baseline table remains the source of exact failure messages. Source HEAD and the selected original methods matched that baseline, so no Framework baseline rerun was needed.

| IDs | Executed final owner | Evidence and disposition |
|---|---|---|
| C1 / D4 | `CombatCommandServiceTests.NoCommandOrNoBattle_ReturnsFalseWithoutPublication` | Passed. No input and no battle are exercised separately; the no-battle branch has pending input and an in-progress Run. Snapshots retain gameplay stores, selected stage/Run/deck identity, results/history, previews, queue and acknowledgement. Removed D4 and its exclusively used service helper after replacement passed. |
| C1 | `CombatCommandServiceTests.RunEnded_ReturnsFalseWithoutConsumingCommand` | Passed. Retained pending input and all stores after installing a Run completion through its existing store contract. |
| C2 / D7 | `CombatCommandServiceTests.AcceptedAttack_PublishesAndSpendsExactlyOnce` | Passed. Real nonlethal attack reduces target health, charges the actor's attack price, installs Attacked and publishes once. Repeated processing changes no stores/counters; input and presentation acknowledgement stay separate. Replaces D7. |
| C2 | `CombatCommandServiceTests.EndPlayerTurn_PublishesActorFreeBoundaryOnce` | Passed. Empty actor, one completed-player increment, StartingEnemy and no AP refill. Replaying the handled number does nothing; a newly submitted old-turn request in a later actual player turn is rejected as StaleTurn with unchanged gameplay facts. D1 is separately retired. |
| C3 / D8 | `CombatAdvanceServiceTests.Automatic_StartsControllerWithoutConsumingViewCommands` | Passed. StartingPlayer becomes Player through the real service, AP becomes 10 and eligibility becomes Available; pending View input, handled number and acknowledgement remain unchanged. D8 relocated and renamed. |
| C3 | `CombatAdvanceServiceTests.NotAutomatic_ReturnsFalseWithoutPublication`; `RunEnded_ReturnsFalseWithoutPublication` | Both passed. Existing guards relocated with full no-op snapshots. |
| C3 | `CombatAdvanceServiceTests.EnemyAction_UsesNoPlayerBudgetAndPreservesPendingInput` | Passed. Ordinary enemy attack publishes the enemy actor and attack, damages a player and installs Attacked while spending zero player AP. Pending input and handled/ack numbers are unchanged. |
| C4 / D6 | `CombatAdvanceServiceTests.TwoNights_ExpireSuppliesAfterEnemyResponseAndPreserveBadge` | Passed. Two actual nights, sixteen player ends and sixteen no-action enemy responses. No early cleanup at player end 8 or enemy start; each eighth response publishes one NightEnded and the Main's supplies-expiry event. Badge/source and Main field presence survive. Actual second EnterCombat resets night-local count, then owner start advances identity and refills AP. Removed D6 plus its exclusive SeedHeldMain/ShortNight field and branch. |
| C4 | `CombatAdvanceServiceTests.TerminalAction_PreventsFurtherControllerPublication` | Passed. Legal lethal headquarters attack reaches Finished with a completion receipt and charged AP; repeated automatic calls publish nothing and refill nothing. |
| K4 / D5 | Three existing `MovementPublicationTests.ConfiguredMovement_RejectedRequestPreservesWorldAndPublishesOneRejection` cases | All passed with queued input and PresentedResultSequence assertions added. Removed D5 only after these passed. Direct Buffer replay R1 remains deferred to Task 3. |
| D1–D3 | No obsolete replacement owner | Retired actor-inference method and only the two timeline=true attributes. Both false invalid-field siblings retain their original bodies and remain deferred. |

Both service class files were relocated with their original `.meta` bytes/GUIDs. The command file has four final tests and the advance file six. K3 and the Task 1 lifecycle probe remain unchanged. Unlisted mixed Framework cases are unchanged; only the now-unused local `Service`, `SeedHeldMain` and `ShortNight` setup was removed. The shared CycleSource's remaining behavior was not rewritten.

The fixture adds only command conveniences, immutable no-op snapshots, Run receipt setup and an encounter source using production definition adapters. Encounter input retains the 8x6 board and player placements; enemy deployment depth 3 makes y=3 targets legal and in the Rider's range, with an inert headquarters for the two-night scenario and a low-health headquarters for the terminal scenario. Real entry/placement/controller/action services install all live turn/AP/action state. No fake accepted transition, live AP setter, counter reset, disabled effect, new test-assembly reference or runtime state owner was introduced.

### Executed verification

| Assembly / gate | Discovered / passed | Failed / skipped | NUnit seconds | Process seconds | Compile seconds | Initial refresh seconds | Project load seconds |
|---|---:|---:|---:|---:|---:|---:|---:|
| Replacement gate, before deletions | 15 / 15 | 0 / 0 | 0.7070048 | 27.773 | — | — | — |
| Final CoreIntegration | 15 / 15 | 0 / 0 | 0.9111285 | 29.690 | 9.224 | 18.181 | 22.445 |
| Final Core | 3540 / 3540 | 0 / 0 | 9.950567 | 35.952 | 1.322 | 12.974 | 17.917 |

Evidence directory: `.superpowers/core-test-foundation/task2/`. Passing runs are `replacement-final`, `final-integration` and `final-core`, each with XML/log/process JSON. Assembly-filtered execution contains no Framework/View cases. Compiler/import/load measurements overlap and must not be summed. The two-night case completed in under one second; it does not load scenes or assets. Existing project-level compilation dependencies remain.

Earlier attempts are retained: one missing test-only namespace import, invalid fixture deployment depths, the no-battle setup using the completion-only service rather than the existing Buffer cleanup boundary, the wrong expected stale-turn reason, and a reference-identity assertion on wrappers rebuilt during rejection publication. Each was traced to setup/assertion code; gameplay value comparisons use existing SameValues contracts. No production defect was hidden or repaired. The first Start-Process -Wait wrapper remained waiting after Unity logged exit 1 and no Unity process remained; only that identified wrapper was stopped, and subsequent runs waited on the Unity process handle. No cache or Unity version changes were made.

### Count, boundary and preservation audit

- Five cases relocated/renamed, one accepted-command case replaced, four current cases added, three retired cases removed, and three duplicate cases consolidated. Parameter-level net change: **-2**. Framework loses 12 selected cases; CoreIntegration grows from 5 to 15. Unity's root suite discovery changes from 10,394 to 10,392, matching exactly. This is discovery evidence, not a full-suite execution claim.
- All five Task 1 CoreIntegration cases remain; pure Core's 3,540 fully qualified identities match Task 1 exactly. No skipped cases.
- `verify-task2.ps1` passed the case/discovery reconciliation, unchanged production/asset hashes relative to Task 2 start, original service GUIDs, source/meta pairing, LF check, allowed asmdef references, dependency search, retained false siblings and exact removed-method inventory. The font asset was preserved byte-for-byte from Task 2 start. The FMOD catalog produced an EOL warning during inspection but no content diff; it was not edited.
- `git diff --check` passed. Production code, runtime contracts/state ownership, existing asmdefs, pure tests, scenes and SOs were not changed by Task 2. The existing duplicate-using warning in LobbyDepartureIntegrationTests is outside this task; project compilation is not claimed warning-free.
- The separate read-only Task 2 review found no actionable findings. It explicitly excluded Task 3's direct Buffer replay/R1–R5 guarantees, Task 4's full disposition reconciliation, and Framework/View-wide success. Those remain pending rather than implicitly complete.

For a future command/controller test change, the modification and understanding scope is now the local encounter/fixture contract and the owning production service/Buffer contract instead of old mixed Framework/View fixtures. Setup remains in one existing fixture and uses the existing Factory lifecycle. No runtime responsibilities or interfaces changed, preserving SSOT and the existing SOLID/FW boundaries. Verification for this task is Core plus CoreIntegration and narrow source-preservation checks; it does not establish unrelated View/scene behavior. Task 2 is complete; stop before Task 3.

## Task 3 execution (2026-10-26)

Authorization: after Task 2, the user requested Task 3. Work continued at HEAD `1ba7aec4728e1dfa5199a5f6043cce10dde636b5` in the same checkout. Task 4 remains pending. No commit or push was performed.

### Recovered owners and observed guarantees

All final classes are in `ProjectL.Tests.CoreIntegration`. `.superpowers/core-test-foundation/task3/case-ledger.tsv` records all **29 final fully qualified identities**, their old names where applicable, baseline/final results and per-case duration. The old selected cases comprised 27 failures and the passing R4 Abandon case; their existing baseline XML/messages remain preserved. Selected source did not drift in a way that invalidated that baseline.

| ID | Final owner / cases | Executed coverage |
|---|---|---|
| R1 | `CombatCommandPublicationTests` — 7 | All six owned-state parameter cases plus direct rejection replay passed. Real accepted attacks precede controlled changes to expected level/experience, owned proposals or soul-field source. Negative cases assert the owned/soul validation guard, preserve every captured store and then successfully publish the valid proposal. The next-input case progresses through a real subsequent player turn before comparing stale/current owned records. Direct rejection replay calls `BattleWriteSink.Apply` twice, rather than relying on the service's handled-command shortcut. |
| R2 | `CombatRewardPublicationTests` — 9 | Two real kills by the same actor on distinct actual owner turns grant 10 and 20 gold immediately, retain pending XP until victory, then install 30 XP. Replay, repeated completion and cleanup do not pay again; Run kills rise twice and RunSpent stays unchanged. All seven invalid variants reach their specific reward/completion/selection guard, preserve all stores, and have passing corrected controls. A real nonreward attack also rejects a stale balance and accepts a fresh balance. |
| R3 | `CombatTransitionPublisherTests` — 5 | Valid nonterminal/terminal calculator results preserve action settlement. The progression spy confirms call counts and outcomes. Terminal publication attaches the original character/soul settlement and events without recalculation; RunEnded refuses publication, other progression rejection throws without installation, and nonterminal/rejected results bypass progression planning while retaining result/handled-number contracts. |
| R4 | `CombatCompletionPublicationTests` — 3 | Command uses a legal lethal headquarters attack; Advance reaches Enemy and calculates a normal lethal enemy attack; Abandon retains the real service path. A minimal stage-read callback is invoked exactly once, verifies unchanged pre-lookup state, ends the Run through its existing receipt store, then captures that intentional change. Each service returns failure and leaves all subsequent combat/store/publication state unchanged. |
| R5 | `CombatItemPublicationTests` — 5 | Four original Inventory/Slot/Effect/Random cases plus the new Unchanged control. A normal enemy starts with supported Whiskey and reaches the real Enemy phase. A supported player Lantern is seeded through the existing character transaction so the calculated ordinary enemy attack proposes actual health, item-slot consumption and attacker-effect changes. Each single source mismatch rejects before any proposal is installed; Unchanged installs the same candidate facts with one result and no player AP spending. No Rope/Frenzy fixture is used. |

The `ConfiguredMovementBattle` helper captures the exact definition returned by an explicitly supplied initialization source; its default DB path remains unchanged. Its small publication binding constructs the existing `CombatInputAssembler`, `BattleProgressionPlanner` and `CombatTransitionPublisher` using one built local reader and the same live Buffer. The fixture gains an optional local database input only for R4's stage-read callback. No production factory, service registration or runtime API was changed.

`CoreIntegrationSnapshot` consolidates the existing Task 2 no-op capture instead of duplicating a second implementation. It captures immutable game values and copied collections: actual membership and Main ID/state, roster/revision/souls, combat and completion, inventory, artifacts, deck, currency/fame, selected stage/map/Run identity and progress, result history, preview counters, queued commands and presentation acknowledgement. Failed transactions use the same snapshot contract as no-op services. Published rejection results are separately checked by gameplay values because their public wrappers are legitimately rebuilt.

### Execution evidence

All evidence is under `.superpowers/core-test-foundation/task3/`. Each completed Unity run has an XML, Editor log and process JSON. NUnit durations are distinct from process wall time; compile, refresh and project load overlap and must not be summed.

| Run | Discovered / passed | Failed / skipped | NUnit seconds | Process seconds | Compile seconds | Initial refresh seconds | Project load seconds |
|---|---:|---:|---:|---:|---:|---:|---:|
| R1 focused (`r1-run`) | 7 / 7 | 0 / 0 | 0.708374 | 26.036 | — | — | — |
| R2 focused (`r2-first`) | 9 / 9 | 0 / 0 | 0.6361974 | 28.710 | — | — | — |
| R3 focused (`r3-first`) | 5 / 5 | 0 / 0 | 0.5989122 | 28.242 | — | — | — |
| All replacements before source removal (`replacement-all`) | 44 / 44 | 0 / 0 | 1.1774592 | 25.748 | — | — | — |
| Final CoreIntegration (`final-integration`) | 44 / 44 | 0 / 0 | 1.3241567 | 31.826 | 10.579 | 20.309 | 24.167 |
| Final Core (`final-core`) | 3540 / 3540 | 0 / 0 | 10.9216249 | 30.939 | 0.951 | 9.011 | 12.897 |

Both final runs exited 0, with no failed or skipped case. Assembly-filtered execution contains only the selected Core or CoreIntegration assembly; no Framework/View test was executed. Existing project-wide compilation dependencies and the prior duplicate-using warning remain outside this task. No new slow case or renderer setup was introduced.

Earlier failures are preserved rather than overwritten: the snapshot extraction initially lacked its test-only Roster import, and R4 Advance initially inspected the actor-free merged controller input instead of `Attack.AttackerId`. Source inspection confirmed the existing composition contract and the assertion was corrected; the actual lethal attack, Enemy phase and no-publication expectations were retained. No failure required or authorized a production change.

### Relocation, preservation and review

- **28 old cases relocated/recovered; 1 unchanged-item control added; 0 semantic coverage deletions.** CoreIntegration grows from 15 to 44; Framework loses only those 28 selected cases. Project root discovery changes from **10,392 to 10,393**, exactly the one added control. This does not claim execution of all discovered project tests.
- The entire R3 file retains its original `.meta` bytes/GUID. Mixed R1/R2/R4/R5 sources remove only selected methods and parameters; split destination files have new metas. All unlisted methods and old shared Input/Create helpers are text-identical after excluding the specified removals. R4's old shared fixture cleanup remains deferred.
- All prior 15 CoreIntegration identities remain, with the existing command/advance/movement test files byte-identical to Task 3 start. Pure Core's 3,540 fully qualified identities are unchanged.
- `verify-task3.ps1` passed exact source preservation, case reconciliation, production/asset hash comparison, meta pairing/GUID, LF and asmdef/dependency audits. There is no CoreIntegration dependency on old test assemblies, old mixed fixtures, scene/asset loading or View/render initialization. Production and asset hashes, including the pre-existing font content and Task 1 friend declaration, match Task 3 start. `git diff --check` passed.
- The separate read-only Task 3 review found no actionable findings. It excluded Task 4's whole-Phase reconciliation, deferred Framework/View/reflection/roar/other item behavior and the appropriateness of pre-existing font edits; none is claimed resolved here.

For a selected transaction test change, the required understanding is now its actual source facts, ordinary calculated proposal, owning validation boundary and local snapshot/setup contract. Old shared Framework/View fixtures are no longer needed by these cases. The existing Framework binding sequence, production responsibility separation and state ownership remain intact; no new runtime authority or speculative layer was introduced. Shared fixture/snapshot changes were validated by all CoreIntegration cases, and pure Core regression remained green. Task 3 is complete; stop before Task 4.

## Task 4: Final guarantee map and Phase acceptance (2026-10-26)

The user authorized the final verification task after Task 3. No test or production source was edited during Task 4. All claims below refer to the fresh Task 4 runs and current source audit, rather than inferred success from earlier checkpoints.

### Executed guarantee owners

`C = ProjectL.Tests.Core`; `I = ProjectL.Tests.CoreIntegration`. Every listed case/prefix has passing expanded NUnit identities in `task4/guarantee-owners.tsv`; that file records the fully qualified identity, calculation/live boundary, result and duration. `task4/phase-case-ledger.tsv` independently connects every retired, consolidated, replaced, relocated and added case to its final owner or explicit retirement.

| Required guarantee | Pure calculation / contract owner | Live service / Buffer owner | Final finding |
|---|---|---|---|
| Legal preparation and real owner start | `C.EnemyControllerPhaseTests.Start_ResetsOnlyLivingFieldUnitsOfOwner`; `DeferredUnit_BecomesAvailableOnlyAtOwnerStart` | `I.ConfiguredMovementBattleTask1Probe.SourceStartWatchdogAndRealProgress`; `I.CombatAdvanceServiceTests.Automatic_StartsControllerWithoutConsumingViewCommands` | Passed. Empty AP/Deferred preparation, real StartingPlayer advance, AP 10/Available, bounded automatic progression and no-op while Player have executed owners. |
| Configured movement and shared budget | `C.PlayerTurnAttackTests.TwoDifferentCannonsSpendFivePlusFive`; `C.PlayerTurnMovementTests.InstalledMovement_*`; `C.CharacterActionCostRulesTests.*` | Both `I.MovementPublicationTests.ConfiguredMovement_*` methods, including budget/allowance/cost parameters | Passed. Pure tests establish shared category-priced attack funding; live movement validates SO → owned stats → captured definition → preview/publication, shared spending across distinct units and stale context rejection. These boundaries are complementary, not interchangeable. |
| One ordinary action and invalid admission | `C.PlayerTurnAttackTests.AttackThenAttackOrMoveRejectsAndOtherUnitStaysAvailable`, `CannonBudgetFourRejectsWithoutChanges`, `MissingMandatoryTargetDoesNotSpendOrConsume`; `C.PlayerTurnMovementTests.MoveCompletesWithoutAttackAndCannotActAgain`, `ZeroCost_ConsumesActionWithoutSpendingBudget` | `I.CombatCommandServiceTests.AcceptedAttack_PublishesAndSpendsExactlyOnce`; configured movement publication | Passed. Action-use/funding rules are calculated without source mutation; live installation and duplicate handling are checked separately. |
| Actor-free end, no early refill, eight responses and two successive nights | `C.EnemyControllerPhaseTests.ManualEnd_IncrementsOnceWithoutRefill_AndDuplicateRejects`, `StaleEndCannotEndLaterPlayerTurn`; `C.EightTurnNightTests.*` | `I.CombatCommandServiceTests.EndPlayerTurn_PublishesActorFreeBoundaryOnce`; `I.CombatAdvanceServiceTests.TwoNights_ExpireSuppliesAfterEnemyResponseAndPreserveBadge` | Passed. The live case performs sixteen ends/responses and actual second EnterCombat/owner start; constructed pure baselines alone are not used to claim two live nights. Cleanup and its held-state event occur after each eighth enemy response, preserving badge/Main. |
| Enemy funding and terminal stop | `C.PlayerTurnAttackTests.EnemyZeroDamageAttackIsUnbudgetedButConsumesAction`; `C.EightTurnNightTests.TerminalEighthResponseCannotPublishDayOrStart` | `I.CombatAdvanceServiceTests.EnemyAction_UsesNoPlayerBudgetAndPreservesPendingInput`; `TerminalAction_PreventsFurtherControllerPublication` | Passed. Real enemy actor/action publication does not spend player AP or consume View input. Terminal completion prevents further publication/refill. |
| Accepted/rejected handling and exact-once publication | Pure action/admission owners above | `I.CombatCommandServiceTests.*`; configured rejected movement; `I.CombatCommandPublicationTests.Rejection_PublishesOnceWithoutGameplayChanges` | Passed. Accepted attack replay, rejected-service replay and direct `BattleWriteSink.Apply` replay remain distinct. Counters are relative to actual setup and queued input/acknowledgement is preserved. |
| Cross-store atomicity, rewards and terminal races | Pure calculation supplies valid proposals; it does not alone prove installation | R1 `I.CombatCommandPublicationTests.Publication_*`; R2 `I.CombatRewardPublicationTests.*`; R3 `I.CombatTransitionPublisherTests.*`; R4 `I.CombatCompletionPublicationTests.*`; R5 `I.CombatItemPublicationTests.*` | All 29 cases passed. Each selected source mismatch reaches its intended guard with a passing control. Snapshots include actual membership/Main, character/soul/item/reward/Run/completion and publication state. Both coordinator rejection and actual DB-lookup Run termination are represented. |
| Supported effects and explicit deferrals | `C.DeferredCombatFeatureTests.*`; `C.PlayerTurnAttackTests.LegalZeroDamageStillSpendsAndConsumesOnce`; `C.PlayerTurnMovementTests.HiddenMiddleMineDoesNotChangePreviewButResolvesOnCommit`, `MiddleMineStopsBeforeDiagonalEndpoint` | Existing configured movement and R5 supported-item control | Passed within the selected boundary. No disabled unit-clock trait, Rope, railway/tornado clock or other deferred feature was restored. This is not blanket acceptance of all environmental or item features. |

### Exact count reconciliation

Counts are expanded parameter cases, not source method or class counts. D5's three replacement-owner rows count as one removed duplicate. The original Task 1 lifecycle probe was introduced after the first baseline, then relocated during revised Task 1; it is therefore a new case relative to HEAD, although the revised Task 1 ledger correctly records its namespace relocation.

| Classification relative to the original baseline | Cases | Final accounting |
|---|---:|---|
| Relocated/recovered current guarantees | 37 | K3/K4: 4; C1/C3: 5; R1–R5: 28 |
| Accepted-command replacement | 1 | D7 → explicit accepted attack settlement/replay |
| Retired obsolete contracts | 3 | D1 plus only D2/D3 timeline=true parameters |
| Consolidated duplicate cases | 3 | D4 → C1; D5 → K4; D6 → two live nights |
| Total old Framework cases removed from their original owners | **44** | 38 retain transferred/replaced owners; six are explicitly retired/consolidated |
| Added current coverage | **6** | Original Task 1 lifecycle probe; Task 2 actor-free end, enemy action, two nights and terminal stop; Task 3 unchanged-item control |
| Final CoreIntegration cases | **44** | 37 + 1 + 6; every identity has a mapped owner |
| Pure Core identities | **3540 → 3540** | Exact identity set unchanged |
| Project root discovery | **10393 → 10393** | -44 old source cases +44 CoreIntegration cases; no unexplained disappearance |

All replacement-dependent removals have passing evidence from before deletion and from the fresh final run. D1–D3 are kept separate as retired contracts; relocation is not reported as coverage loss. No skipped cases or assembly exclusions were introduced.

### Fresh final execution and timing

Evidence directory: `.superpowers/core-test-foundation/task4/`.

| Assembly | Passed / discovered | Failed / skipped | NUnit seconds | Process wall seconds | Compile seconds | Initial refresh seconds | Project load seconds |
|---|---:|---:|---:|---:|---:|---:|---:|
| CoreIntegration | 44 / 44 | 0 / 0 | 0.9120024 | 78.224 | 5.515 | 44.800 | 67.699 |
| Core | 3540 / 3540 | 0 / 0 | 10.4443098 | 32.259 | 1.309 | 9.726 | 13.853 |

Successful evidence is `final-integration.xml/.log/-process.json` and `final-core-sequential.xml/.log/-process.json`; both processes exited 0. The initial Core launch was started before the integration Editor released the project. It stopped before tests at `HandleProjectAlreadyOpenInAnotherInstance` (exit 1073741845); `final-core.log` and its process JSON are retained as failed infrastructure evidence, with no XML/pass claim. Only after integration exit 0 was Core rerun sequentially. No cache deletion or Unity upgrade occurred.

The longer integration process was dominated by Editor startup: project load 67.699 s, including initial refresh 44.800 s, logged domain reload 24.015 s and licensing connection 11.17 s. These nested/overlapping measurements are not additive. The actual 44-test run took 0.912 s; the slowest integration case was under 0.32 s, and the slowest pure Core case was 0.187042 s. No individual slow test was skipped, weakened or replaced with a mock. The logs support startup/import overhead; they do not establish a hardware-level cause for that overhead.

Only Core and CoreIntegration tests executed. Unity still loads/compiles the project and its existing dependencies; this does not claim complete project compilation isolation or full Framework/View success. The known duplicate-using warning remains outside this task.

### Final audit and review disposition

- `verify-task4.ps1` and `reconcile-phase.ps1` passed. Outputs include `verification.json`, `phase-verification.json`, `phase-case-ledger.tsv`, `guarantee-owners.tsv`, full final Core identities and integration per-case timing.
- Original mixed movement methods, unlisted R1/R2/R4/R5 methods and their shared helpers are preserved. Pure `PlayerTurn*`, `EnemyControllerPhaseTests`, `EightTurnNightTests`, `DeferredCombatFeatureTests` and `CharacterActionCostRulesTests` are unchanged. Existing test asmdefs are unchanged.
- The CoreIntegration asmdef has only the four approved production references. Source audit found no old test assembly/fixture dependency, scene/asset loading, Play-mode tests or View/render setup. All C#/asmdef assets have metas; split GUIDs are unique, three relocated service/publisher metas match HEAD, and the extracted helper retains its GUID.
- The only production source diff is `[assembly: InternalsVisibleTo("ProjectL.Tests.CoreIntegration")]` in Systems AssemblyInfo. Production/asset hashes match the preserved Task 3 starting state, including prior font content. Task 4's integration source/meta hashes are unchanged from its start. Concurrent category/trait documents were not modified.
- `git diff --check` passed. The fresh whole-Phase read-only review independently checked integration source, the five acceptance requirements, exact old/new identities, deletion boundaries, pure owners and successful XML/process results, and found **no actionable findings**.
- Review exclusions remain explicit: full Framework/View success, completeness of deferred effects/items/environments, and the content of concurrent font/category/trait work. These are outside this Phase, not unreported failures of the required gate.

The required guarantees now have distinct calculation and live-publication owners. A selected feature test can be changed using its local data/fixture and production contracts without importing unrelated View or old mixed test implementations. Shared fixture/snapshot changes still require the full CoreIntegration gate; pure Core remains the rules regression gate. Existing SOLID/FW responsibilities and SSOT ownership were preserved, and no additional runtime layer or state source was created.

**Tasks 1–4 and this bounded Phase are complete.** Historical broad-run failures/crash evidence and all later focused evidence remain available. The branch and uncommitted work are preserved for the user; no merge, commit, push or category/trait implementation was performed. Stop here.
