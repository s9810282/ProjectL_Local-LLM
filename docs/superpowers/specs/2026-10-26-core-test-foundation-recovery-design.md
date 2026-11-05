# Core Test Separation, Coverage Consolidation and Foundation Recovery

Date: 2026-10-26  
Status: Coverage-first approach and Core/View test separation requested by the user. This revision changes documents only; original Task 1 work from another session is preserved, not continued.  
Inspected HEAD: `1ba7aec4728e1dfa5199a5f6043cce10dde636b5`.

Follow-up: the [bounded case disposition](../plans/2026-10-26-core-test-foundation-disposition.md) and [implementation plan](../plans/2026-10-26-core-test-foundation-recovery-plan.md) select exact cases for one Phase. Revised Task 1 separates CoreIntegration before further recovery. Stop the original Task 1 session, preserve its results, and resume against the revised boundary only when authorized. Unlisted legacy cases remain deferred. This documentation request does not authorize execution or automatically stop another session.

## 1. Purpose and approach

Use the new combat tests as the starting point for trustworthy category/trait development. Compare guarantees, move selected Core–Buffer checks into an independent test assembly, delete retired/replaced tests, and repair only remaining gaps. The goal is not to preserve old classes or run the slow mixed Framework/View suite during ordinary Core work.

A short design is warranted because `PrepareFixtureBattle` / `CaptureFixtureBattleMembership` have references in 48 test files. That is potential dependency reach, not an edit list. The previous draft's requirement to restore retained tests in ten old integration classes is withdrawn. Coverage is defined by observable contracts and relevant execution boundaries, not historical file names.

| Approach | Consequence | Decision |
|---|---|---|
| Map guarantees, separate CoreIntegration, consolidate and recover remaining gaps | Reuses new tests, isolates execution from View tests, limits fixture work | Selected by the user |
| Restore all old classes before feature work | Rebuilds duplicate coverage and expands shared-helper changes | Reject |
| Delete every failing test | Removes valid stale-source, reward and atomic-publication checks along with obsolete expectations | Reject |
| Migrate the entire Framework/View suite first | Pulls in unrelated content and presentation work before category/trait work | Defer |

## 2. Evidence and scope boundary

The original review of `d4a6d6f` executed 127 selected integration tests: 88 failed at old preparation fixtures, 9 retained obsolete action/night scenarios, and 2 had a placement omission also present in parent source. Core passed 3,497 tests then. These historical numbers describe failure causes, not how many tests need repair.

The later movement SO delivery adds a useful normal-entry example: `MovementPublicationTests.ConfiguredMovementBattle` uses existing runtime services, explicit placement, `EnterCombat` and automatic controller start before publishing moves. Its stored `.superpowers/category-movement-so/task3-check.xml` was read for this design: Core 3,540 passed; selected Framework tests 186 passed / 89 failed. That is a different selection from the original 127; do not subtract or combine these failure totals. No tests were rerun while writing this document.

Production contracts stay unchanged: preparation has empty AP, controller sequence zero, no active controller and Deferred characters; actual owner start resets eligibility and refills only player AP. Accepted ordinary actions carry matching identity, action-use and cost settlement. Buffer owns validation, installation, publication and revisions.

### Test assembly boundary and execution cost

- Preserve `ProjectL.Tests.Core`: its pure Contracts/Domain/Application references and `noEngineReferences: true` stay unchanged.
- Add `Assets/Tests/EditMode/CoreIntegration/ProjectL.Tests.CoreIntegration.asmdef`: Editor-only NUnit tests referencing production Framework, Systems, Core.Contracts and Core.Application. In-memory SO/UnityEditor use is allowed; references to existing test assemblies, scene/project-asset loading, View/render initialization and Play mode are excluded.
- Add only `[assembly: InternalsVisibleTo("ProjectL.Tests.CoreIntegration")]` to `Assets/ProjectL/Script/Systems/AssemblyInfo.cs` for internal services/Buffers/factories. Preserve existing friends. No speculative Framework/Core friend declarations or public test setters.
- Track old/new case identities. Whole-file moves retain GUIDs; splits get new metas. Do not import entire mixed classes or their test helpers into the new assembly. Use minimal local in-memory fixture data and read-only snapshots.
- Extracting `ConfiguredMovementBattle` alone is insufficient: its old DefinitionDatabase, roster/deck helper and probe factory dependencies must be removed. Reuse the useful extraction; do not finish the old Framework-folder task first.
- Normal verification runs Core and CoreIntegration, not Run All/full Framework. Record per-test and total duration and diagnose slow cases without silently skipping them. Existing Systems transitive dependencies and Unity-wide compilation remain; this separates executed tests, not all compile/import cost. Unrelated View/Framework cleanup stays with its owner.

### Initial source comparison

`T = Assets/Tests/EditMode/`; `F = T/Framework/`. Method names below are exact. This is a representative first mapping pass, not a completed per-case deletion manifest. Existing old files can already contain migrated/new tests.

| Old guarantee or candidate | Current replacement evidence | Decision and residual scope |
|---|---|---|
| Shared spending / one action; old per-unit refill and return assumptions | `T/PlayerTurnAttackTests.TwoDifferentCannonsSpendFivePlusFive`, `AttackThenAttackOrMoveRejectsAndOtherUnitStaysAvailable`; `T/PlayerTurnMovementTests.MoveCompletesWithoutAttackAndCannotActAgain` | New-rule calculation coverage exists. Delete solely retired balance/return checks if any remain; assess any publication assertions separately |
| `F/CombatCommandServiceTests.EndUnitTurnWithoutActor_ResolvesCurrentActiveActor` | `T/EnemyControllerPhaseTests.ManualEnd_IncrementsOnceWithoutRefill_AndDuplicateRejects`, `StaleEndCannotEndLaterPlayerTurn` | Actor inference is retired. Calculation semantics are covered; only an unmapped actor-free published result/handled-number guarantee could require a service check |
| `F/CoreStageCombatCycleTests.HeldEffects_NightEndExpiresSuppliesWhileMainStaysFieldAndKeepsBadge` | `T/EightTurnNightTests.EighthResponseExpiresItemsAndSuppliesOnMain_BeforeDay`, `EightPlayerEnds_WaitForEachEnemyResponse_ThenEnterDay` | Partial: expiry and retained Main are calculated. Badge preservation and the live Buffer/event result still need mapping; do not restore the one-turn-night fixture |
| Basic movement publication / preview purity | `F/MovementPublicationTests.ConfiguredMovement_SoToOwnedStatsBattlePreviewAndPublication_CapturesLifetime` | Actual SO -> stats -> battle -> preview -> command -> Buffer path checks AP, action use, position and sequence. Reuse this coverage instead of recreating basic integration cases |
| `F/CombatCommandServiceTests.ProcessesNextCommandOnce_AndDoesNotReprocessHandledInput` | `F/MovementPublicationTests.ConfiguredMovement_RejectedRequestPreservesWorldAndPublishesOneRejection` (`budget`, `allowance`, `cost`) | Partial candidate: new tests check rejected movement, unchanged stores, one result and no second processing. Old test submits Attack without asserting acceptance. Decide the required accepted-action contract and strengthen a current case only if missing |
| `F/MovementPublicationTests.HighGround_PublishesFixedActionCostAndUsedAction`, `HighGround_OriginChangeRejectsStalePreviewWithoutChargingMovement`, `HighGround_HiddenChangeKeepsPreviewValidAndChargesAuthoritativeCost` | Configured-movement cases above | Generic cost/publication overlaps; new cases do not introduce HighGround or hidden environment changes. Map those environment-specific guarantees before deletion |
| `F/CombatCommandPublicationTests.Publication_DifferentOwnedLevelRejectsWithoutWrites`, `Publication_OwnedChangeInstallsWithCommandAtomically`; `F/CombatRewardPublicationTests.InvalidPublication_LeavesEveryStoreUnchanged` | New movement/turn tests inspected above | No equivalent cross-store owned/reward failure check found in this inspected set. Search other current publication tests before declaring a gap; recover only the unique guarantee |
| `F/CombatTransitionPublisherTests.RunEndedDuringResolution_DoesNotPublish`; `F/CoreStageCombatCycleTests.FinalCompletionLookupEndsRun_ReturnsFalseWithoutPublication` | `T/EightTurnNightTests.TerminalEighthResponseCannotPublishDayOrStart` | Different boundaries: terminal calculation is covered; Run termination during resolution/publication is not established by it. Compare the two integration paths for overlap before selecting one |
| Already migrated tests in old files, e.g. `T/CharacterActionCostRulesTests.UnitContracts_DoNotExposeABalanceOrTimelineClock` | Current API-removal assertions in that method | Keep unless independently shown redundant; an old filename does not make its contents obsolete |

No entire old class has been established as fully replaceable by this first pass. Nor does this table establish that every unlisted publication case lacks a replacement. Complete exact case mappings before deleting or repairing a candidate group; a pure calculator result is not evidence of live Buffer installation.

## 3. Conditional fixture recovery

Apply this section only after mapping identifies a necessary uncovered guarantee. Old sources are under `Assets/Tests/EditMode/Framework/`; selected checks/helpers end in `CoreIntegration/`. Do not repair a helper solely to keep obsolete/replaced tests alive. Consolidate residual assertions into independent current fixtures.

### 3.1 Keep preparation and progression explicit

- Reuse the working configured-movement fixture and production calculator/transaction/service contracts. Old fixture code supplies evidence for necessary inputs/assertions, not a cross-test-assembly dependency. Local test data replaces old DefinitionDatabase/roster/deck setup; narrow local bindings/snapshots replace CombatRequestTestParts and mixed test classes. Do not copy a general framework or unrelated DB/UI/scene graph.
- Make preparation builders supply legal initial facts and establish the Run deck, captured Main identity, membership and definitions before initial installation. Keep `PrepareFixtureBattle` a strict preparation operation: it must not silently reset AP, replace invalid caller facts, start a controller turn, or turn a deliberately invalid preparation into a valid one.
- Make an active-player/active-enemy fixture explicitly perform placement where required, `EnterCombat`, and the existing controller advancement. Service/integration tests use real Core services. Focused Buffer tests may use explicit legal deployment facts and existing pure calculators plus Buffer transactions; they must not emulate the turn rules themselves.
- Do not globally replace every `Available` with `Deferred`: preparation facts and active-action facts have different meanings. Migrate callers according to the state they request. Retain malformed inputs in tests intentionally exercising preparation rejection.
- `CaptureFixtureBattleMembership` must not tear down and recreate a progressed battle to repair its membership. Construct membership before preparation; preserve intentional membership-error tests.
- Test helpers may submit, assert and read facts; they must not directly set live AP, action use or controller identity to simulate a turn. Do not add production setters, bypass switches, reflection-based state installation or compatibility APIs.

### 3.2 Preserve identity and publication evidence

Capture battle/turn identity at request creation. Capture command/result/preview numbers **after setup**, and assert the operation's deltas. Real setup now publishes results: never reset counters/history to make an old `== 1` assertion pass, and never accidentally submit an already-handled command number. Duplicate-command tests deliberately reuse their captured number.

For synthetic publication tests in `CombatCommandPublicationTests`, first build a valid accepted transition with the real calculator and profiles, identity, action use and settlement. Then corrupt only the source/final fact being tested. A valid control must succeed; the negative case must fail at its intended validation boundary and preserve all affected stores. Merely observing any `InvalidOperationException` is insufficient.

If a test exclusively checks a lower-level health/soul/owned-state transaction, use its existing narrow transaction contract. Do not label arbitrary state edits as accepted attacks or change command kinds just to evade action validation. Command-publication tests must still prove command-publication guarantees.

## 4. Classify coverage before retaining, rewriting or deleting

Before implementation, record exact parameterized test name, contract, input conditions, execution boundary, assertions, replacement test(s), residual guarantee, disposition and source/execution evidence. A class name containing “turn” is not evidence that its entire contents are obsolete. Validation of a replacement must include its relevant failure branches and successful execution before the duplicate is deleted.

| Classification | Required action |
|---|---|
| Retired contract | Delete; no replacement of the obsolete behavior is required |
| Fully covered by current tests | Keep validated replacements and delete the duplicate; do not restore its fixture |
| Partially covered | Consolidate only missing assertions/scenarios into a suitable current test, or retain a narrowly focused check; remove superseded remainder after verification |
| Uncovered current guarantee | Search the current inventory, then implement/repair the smallest check and only its necessary setup |
| Not yet mapped | Inspect before deciding; neither deletion nor restoration is automatically required |

For any surviving lifecycle gap, use real controller progress or a different eligible actor instead of move-then-attack by one unit. Remove ineffective ShortNight or disabled lethal-railway assumptions only when their old scenario is still needed to prove a unique guarantee. If a composition check survives mapping, supply actual Main placement. Deferred content's obsolete cadence tests are not a reason to reactivate features; preserve current disabled-admission and unaffected-effect coverage.

Next-turn helpers must have a bounded advancement guard and assert the expected controller/stage. They must stop on battle completion, rather than advancing until a preferred state appears regardless of the result. Do not encode a fixed number of enemy actions.

## 5. Change boundary and locality

**Candidate source inventory, not a restoration list:** old service/publication/lifecycle files named in the disposition. Its destination table selects CoreIntegration classes and exact transferred cases. Mixed sources retain unrelated methods and old shared helpers remain unchanged. Pure Framework contracts and View tests are not moved wholesale. Audit references before removing source methods/helpers.

Pure Core test files are eligible for mapped retired/fully covered deletions, partial-coverage consolidation, or a demonstrated missing foundation assertion. Removing an entire class also removes its paired `.meta`; mixed classes retain unique valid checks. A proposed new helper file needs a concrete reuse benefit and a matching `.meta`, not a new assembly.

**No production behavior changes:** Core calculators, Buffers, Framework, View, factories, jobs, scenes, SO assets and existing asmdefs stay unchanged. The new test asmdef/metas and named Systems friend declaration are the explicit boundary exception. A production defect or further access change needs a separate decision with evidence; never weaken expectations or widen public APIs to hide it.

First remove/consolidate duplicated checks, then determine whether any shared helper still needs recovery. Before consolidation, repeated fixtures make one contract change require several independent migrations. After consolidation, one suitable current fixture owns each guarantee; only surviving callers need to understand preparation, progression and operation-relative publication. Shared-helper changes still require testing their actual consumers. No new runtime state source, scheduler or transaction implementation is introduced.

## 6. Guarantee-based acceptance gate

Name existing executed tests for the following guarantees in the final coverage map; add only demonstrated gaps, with calculation and live-publication boundaries distinguished:

1. Legal preparation and real owner start: empty AP/Deferred before start, player AP 10 and eligible units available afterward; wrong/stale requests cannot mutate state.
2. Two distinct units share the installed category-priced budget; one unit cannot move and attack in the same owner turn. Enemy actions consume eligibility without spending player AP. Preserve the completed movement SO migration's configured-value evidence.
3. Player end is actor-free and does not refill AP. Owner start alone resets eligibility. Day/cleanup occurs after the eighth enemy response, including a no-action enemy phase; terminal completion prevents further starts/refills. Map successive-night identity/publication separately: `EightTurnNightTests` baseline-0/8 cases construct inputs separately and do not alone prove two live nights. Reuse any equivalent current integration coverage before adding a bounded scenario.
4. Valid commands publish matching world, AP, action use, result and handled number once. Duplicate/stale or invalid source proposals cannot partially install character, soul, inventory, reward or completion state. Test both a valid control and the intended failing mutation.
5. Supported attack/movement effects retain their current boundaries; no disabled unit-turn effect is re-enabled to make a fixture pass.

The gate is pure Core plus independent CoreIntegration proving selected guarantees, not ten historical classes or a fixed count. CoreIntegration discovery must contain no View/scene/render tests or old test-assembly dependencies. An old class may disappear when its contracts are retired/covered/transferred. Required checks must pass; unrelated old failures are not automatic repair tasks.

## 7. Verification and stop conditions

Before code edits, reconcile the stopped original Task 1 baseline with the checkout; refresh focused evidence only when source drift requires it. After separation run Core and CoreIntegration with separate XML/logs/timings. Keep old shared-helper behavior unchanged; use targeted source/discovery checks for removals and any demonstrated affected retained consumer. No full Framework/View run or repeat of the recorded rendering crash is required. Report any dependency that would require broadly changing old shared helpers before extending scope.

Use Unity 6000.0.73f1 through the approved AppData-capable path. Retain XML/logs and the disposition ledger; no cache deletion, upgrade, skips or assembly exclusion. Reconcile count reductions against exact retired/replaced cases and retained guarantees. Replacement coverage is source-inspected until its execution succeeds; names alone do not establish equivalence. Existing unrelated failures must match baseline by identity and cause; do not claim full Framework success while they remain.

The implementation plan must start from exact coverage decisions and contain only surviving gaps and necessary deletions/consolidation. One bounded Phase completes when selected replacement/gap checks pass, full Core succeeds, no unexplained collateral failures remain, production is unchanged, and deferred failures are reported. If a required guarantee remains blocked, report incomplete at that blocker. Category/trait implementation follows separately. Commit and push remain user-owned.

The initial revision performed the source-mapping pass only. The linked follow-up adds bounded case decisions and an implementation plan at the user's request. No test repair/deletion, test execution or production change has been performed by these documentation deliveries.
