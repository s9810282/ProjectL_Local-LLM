# Chapter Stage Repeat Reduction Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Reduce repeated facility kinds along connected chapter paths while preserving topology, kind counts, fixed placements, and depth eligibility.

**Architecture:** Keep the existing distribution and stage-key RNG streams. Between those operations, run a bounded deterministic search over legal kind swaps, guided by the approved repetition score. Carry one opt-in boolean through the existing immutable distribution rule and SO authoring input.

**Tech Stack:** Unity 6000.0.73f1, pure C# Core, NUnit EditMode tests.

**Spec:** `docs/superpowers/specs/2026-11-06-chapter-stage-repeat-reduction-design.md` (approved in chat on 2026-11-06).

## Global Constraints

- Consecutive Combat nodes remain allowed. This is a soft preference, not a generation constraint: unavoidable repetitions remain valid.
- Different facility kinds may follow each other.
- No complete route enumeration is needed.
- With all flags false, skip reduction entirely and preserve the existing golden outputs and RNG call sequence.
- The reducer consumes no random numbers. Identical inputs remain deterministic.
- Commit/push require an explicit user instruction. After Phase 2, the user authorized committing and pushing this completed change on the current feature branch.
- Implementation documents are English; new code comments and XML documentation are Korean.
- Preserve the user's existing FMOD/font asset changes. Work in the supplied feature checkout; do not move the user's Unity project or change branches.
- Phase 1 stopped at its review point. The user explicitly authorized Phase 2 on 2026-11-06; stop after its final report.
- This is Core work: no View test additions or combat View MSBuild runs. Unity runs use the approved AppData-access path.

## Review Focus

- Fixed nodes influence adjacent repeat scoring even though they cannot move (Task 2).
- A zero-weight kind placed to satisfy a minimum may move but must not gain extra nodes (Task 2).
- Merging paths and branching paths must count actual three-node chains without treating disconnected same-depth nodes as consecutive (Task 2).
- A legal input with no improving swap must terminate with a valid graph, not fail generation (Task 2).
- Existing serialized authoring that omits the new field remains disabled, while production Chapter 1/2 activation is explicit (Task 3).

## Phase 1 — Pure Core reduction and compatibility (implemented; baseline failures documented)

### Task 1: Extend the immutable distribution rule without changing old callers

**Files:**
- Modify: `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/StageMap/ChapterStageDistributionRule.cs`
- Create: `Assets/Tests/EditMode/ChapterStageRepeatReductionTests.cs` and matching `.meta`

**Interfaces:**
- Produces: `public bool ReduceConsecutive { get; }` and trailing constructor argument `bool reduceConsecutive = false`.
- Consumes: existing `ChapterStageKind`, `Weight`, `MinimumCount`, `MinDepth`, and `ChapterStagePool` contracts.

- [x] Add a failing contract/default check to the new test file, then add the opt-in flag. Keep existing constructor calls valid and leave `IsActive` and placement validation unchanged.
- [x] Keep the existing golden tests and all their expectations unchanged.

### Task 2: Reduce repeats through bounded legal swaps

**Files:**
- Modify: `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/StageMap/ChapterGraphGenerator.cs`
- Extend: the Task 1 test file only; do not rewrite other authors' tests.

**Interfaces:**
- Consumes: `ChapterStagePool.Rule.ReduceConsecutive` from Task 1.
- Produces: unchanged `Generate(ChapterGraphSpec)` signature and graph format.
- Private helpers: `ReduceConsecutiveKinds(List<Node> middle, List<Node> fixedNodes)` and `RepeatScore(...)`, with generation-local adjacency/count scratch storage reused across candidate evaluations.

- [x] Add failing behavior tests against public `Generate`: a known repeat-heavy case must improve; a legal two-node swap must preserve counts and graph connections. Derive score assertions by explicitly enumerating edges and three-node chains in test code, not by calling production scoring helpers.
- [x] Add coverage for clean graphs, combat-only graphs, mixed facility kinds, fixed first-stage and developer-pin nodes, late `MinDepth`, zero-weight minima, unavoidable repetition, all three connection modes, and width six. Use a fixed seed sweep for determinism and non-increasing score.
- [x] Run the new fixture before implementing the reducer; retain the expected failing output as evidence.
- [x] Insert reduction after `AssignKinds` and before `AssignStageKeys`. Exclude fixed nodes from candidate pairs, but include them in scoring. Use real graph edges only; same-kind edge count plus same-kind three-node-chain count is the score.
- [x] Search node-ID-ordered pairs of different kinds, requiring both destination depths to be eligible. Restore every tentative swap; apply only the best strict improvement, preserving stable tie-breaking. Stop at score zero, no improvement, or M accepted swaps for M movable middle nodes. Accumulate scores in `long`.
- [x] Re-run the new fixture and existing chapter graph/distribution/navigation tests. Expected: all pass, including unchanged golden outputs.
- [x] Run the full `ProjectL.Tests.Core` assembly and relevant chapter `ProjectL.Tests.CoreIntegration` fixtures. Expected: no new failures; explicitly report any existing compile, environment, or test failures rather than masking them.
- [x] Record width-six elapsed generation measurements separately from assertions; report both scale and observed timing without claiming unrestricted scalability.
- [x] Obtain a fresh review of the Phase 1 diff, resolve actionable findings, and record results before stopping at the review point.

**Execution commands:** Use a task-owned copy of the repository's existing native Unity runner at `.superpowers/sdd/2026-11-06-chapter-stage-repeat-reduction-plan/run-tests.ps1`, with unique `-Run` labels, `-Assembly ProjectL.Tests.Core` or `ProjectL.Tests.CoreIntegration`, and optional `-Filter`. The runner invokes Unity 6000.0.73f1 in EditMode batch mode and writes XML/log evidence under that same directory. Run outside the restricted sandbox for AppData access. Do not run parallel Editors against this checkout.

## Phase 2 — Authoring, production activation, and integration (implemented; baseline failures documented)

### Task 3: Carry the setting through SO input and activate the two chapters

**Files:**
- Modify: `Assets/ProjectL/Script/Systems/DataBaseSystem/Data/InGame/Table/Map/Item/StageMapDataSO.cs`
- Modify: `Assets/ProjectL/DataBaseSO/InGameGroup/StageMap/Item/StageMap_Chapter1.asset`
- Modify: `Assets/ProjectL/DataBaseSO/InGameGroup/StageMap/Item/StageMap_Chapter2.asset`
- Create: `Assets/Tests/EditMode/CoreIntegration/ChapterStageRepeatReductionAuthoringTests.cs` and matching `.meta`.

**Interfaces:**
- Consumes: Task 1's trailing flag and Task 2's unchanged generation API.
- Produces: serialized `DistributionInput.reduceConsecutive`, passed into the immutable rule by `Rule(...)`. No duplicated field on `StageMapData`, `ChapterGraphSpec`, or Buffer.

- [x] Add authoring tests with test-owned SO inputs: missing nested field stays false, explicit true reaches generated graph behavior, and new SO defaults disable Combat and enable Shop/Recovery/Enhancement.
- [x] Add the field, Korean tooltip, accessor, constructor parameter, and existing rule-construction forwarding. Set default inputs explicitly; retain existing weights, minima, and depths.
- [x] Set the two chapter assets' four flags explicitly (Combat false, other three true); preserve all other values and GUIDs.
- [x] Run authoring/integration tests plus Core regressions and production asset validation. Hand off user-side `DataBaseAssetTests` and Unity map presentation checks, distinguishing compilation from PlayMode and visual acceptance.
- [x] Review the final change and update the design/plan status. Stop and report without committing or pushing.

## Execution Record

- 2026-11-06: User approved the design and authorized proceeding to planning and implementation. Native execution in the current checkout selected to keep the existing Unity project and unrelated edits in place. The approved small change is divided at the Core/authoring boundary; Phase 1 is current, Phase 2 remains pending.
- Plan self-review: approved scoring, fixed-node inclusion, all-disabled compatibility, termination, propagation, topology/count preservation, and verification requirements each have a task above. No production behavior outside the approved design is introduced.
- Contract RED: `phase1-contract-red` ran two new tests against the original code; both failed on the missing opt-in contract as expected. After adding the flag, `phase1-algorithm-red` passed 8 and failed 5: all failures concerned absent reduction (including fixed-node triple repetition). Production reduction was implemented only after these failures were observed.

### Phase 1 review result (2026-11-06)

At the Phase 1 review point, Core implementation was complete and SO authoring/Chapter 1/2 activation were still pending. Phase 2 results below supersede that activation status.

| Check | Result |
| --- | --- |
| Contract RED | 2 failures on the missing flag before implementation |
| Algorithm RED | 8 passed / 5 expected failures before the reducer |
| Initial focused chapter run | 187 passed / 0 failed |
| Fresh review | One P2 test-coverage finding, addressed below; no production correctness finding |
| Review sensitivity check | Deliberately substituting Shop-only scoring caused the new Recovery, Enhancement, and disabled-Shop tests to fail (3 failures / 1 passed); the mutation was removed |
| Final new fixture | 17 passed / 0 failed |
| Final chapter subset (within full Core run) | 191 passed / 0 failed |
| Relevant chapter CoreIntegration | 32 passed / 0 failed |
| Full Core after change | 4,927 total; 4,902 passed / 25 failed |
| Full Core with the two production files restored to HEAD, new fixture excluded | 4,910 total; 4,885 passed / 25 failed |
| Baseline comparison | Identical 25 failing test names; no newly failing existing tests |
| Source restoration | Final source equals the verified implementation after LF normalization; new fixture and its original meta restored |
| Static checks | Focused git diff --check passed; changed C# is UTF-8/LF; new meta GUID unique |

Width-six measurements in the final Core run used 31 middle nodes, 16 generated maps per connection mode: Nearest 9.395 ms total, Random 12.747 ms total, WeightedRandom 10.849 ms total. These are local Unity EditMode measurements, not guarantees for arbitrary map size or visual acceptance.

Review resolution: the original fixture placed only one Recovery and Enhancement, so it could not expose their repetition. Three per-kind repeat-heavy cases and a selectively disabled-facility case now cover that gap. Their sensitivity was demonstrated using the temporary Shop-only mutation, then all 17 tests passed on the restored implementation. No other author's tests were edited.

Review scope rulings: Phase 2 authoring, PlayMode/presentation, and unrelated FMOD/font changes remain outside Phase 1. Global optimality and unrestricted large-map performance are explicitly not promised by the approved bounded search. The review's optional suggestion for a dedicated triple-versus-edge-only choice fixture is deferred; the independent edge/three-node path scorer, fixed-node triple case, and branching/merging seed sweeps passed. No product-scope deviation was made.

Full Core is NOT green. Most existing failures concern the night boundary; for example, PlayerTurnRules.PlayerTurnsPerNight is currently 99 while NightLengthIsEight expects 8. The Taunt activation expectation also fails before and after this change. These unrelated rules and expectations were preserved. The complete baseline-matching failure list is below.
- ProjectL.Tests.Core.BossNightLifecycleTests.DeadSummon_NeverReturns_AndFreesItsSlot
- ProjectL.Tests.Core.BossNightLifecycleTests.NightDayNight_PreservesIdentityAndStats_ReturnsAroundBossWithOpening
- ProjectL.Tests.Core.BossNightLifecycleTests.NightDayNight_ReserveReturnResetsRoarCounter
- ProjectL.Tests.Core.BossNightLifecycleTests.NoRoomOnReturn_StaysWaiting_AndCombatEntryContinues
- ProjectL.Tests.Core.BossNightLifecycleTests.PendingRoarAcrossDay_RoarsAfterReturnAtNextNight
- ProjectL.Tests.Core.BossNightLifecycleTests.SummonThenNightEnds_RecallsSameEntityWithRecord
- ProjectL.Tests.Core.CombatCommonCommandTests.NightRecallCapacityFailure_LeavesFactsUnchanged
- ProjectL.Tests.Core.CombatItemLifetimeTests.OutgoingEnvironmentalDamage_ActivatesWhiskeyBeforeRoundExpiry
- ProjectL.Tests.Core.CombatItemLifetimeTests.RoundEnd_ClearsPlayerAndEnemyItems_AndRedeploymentDoesNotRegrant
- ProjectL.Tests.Core.EightTurnNightTests.EighthResponseExpiresItemsAndSuppliesOnMain_BeforeDay
- ProjectL.Tests.Core.EightTurnNightTests.EightPlayerEnds_WaitForEachEnemyResponse_ThenEnterDay(0)
- ProjectL.Tests.Core.EightTurnNightTests.EightPlayerEnds_WaitForEachEnemyResponse_ThenEnterDay(8)
- ProjectL.Tests.Core.HeldEnvironmentEffectTests.NightEnd_ExpiresSuppliesBeforeRecallIncludingMain(8,True)
- ProjectL.Tests.Core.HuntersMarkStateTests.TurnPreservesMark_NightRecallClearsOnlyReturnedRecipients(8,True)
- ProjectL.Tests.Core.IntimidationStatePreservationTests.EndTurnPreservesDebuff_NightRecallClearsOnlyReturnedRecipients(8,True)
- ProjectL.Tests.Core.MainDeckInitialPlacementTests.FirstNightEnd_KeepsManuallyPlacedMain_AndRecallsSameSub
- ProjectL.Tests.Core.PlayerTurnRuleTests.NightLengthIsEight
- ProjectL.Tests.Core.PositionTileModifierTests.NightRecallAndPlacement_ReconcileOnlyCurrentPositionContribution
- ProjectL.Tests.Core.RailwayGimmickTests.LastEnemyEndChecksRailwayBeforeRecall_AndTerminalStopsNightCleanup(False)
- ProjectL.Tests.Core.RegenerationCadenceTests.LastEnemyEnd_HealsBeforeRecallAndKeepsPersistentMainProgress
- ProjectL.Tests.Core.RegenerationTurnTests.EighthEnemyCompletion_DoesNotHealPersistentMain
- ProjectL.Tests.Core.TargetingAngleLifecycleTests.RecallRedeploy_PreservesFactionForward
- ProjectL.Tests.Core.TraitActivationOutcomeTests.Taunt_OnlyAnAppliedForcedDecisionActivates(True)
- ProjectL.Tests.Core.WeakGroundTurnTests.ControllerCompletion_DoesNotCollapseOrCountGround(Enemy,8)
- ProjectL.Tests.Core.WeaknessExposureLifecycleTests.EnemyEndAndNightRecall_UseAffectedClockThenClearBothReserves(8,True)

### Phase 2 result (2026-11-06)

The user explicitly authorized Phase 2 and asked whether View code beyond SO input needed changes. It does not: StageMapViewReader still consumes the same StageProgressData records from Buffer, and neither the graph format nor presentation/navigation contracts changed. The only Phase 2 production files are StageMapDataSO.cs and the two chapter map assets.

Chapter 1/2 now explicitly enable reduction for Shop, Recovery, and Enhancement, with Combat disabled. The Inspector exposes Reduce Consecutive in each existing distribution input. The setting affects newly generated maps, not graphs already installed in Buffer. Removing just the four new YAML flag lines from either chapter asset reproduces its HEAD content exactly; weights, minima, depths, widths, seeds, fixed stages, and GUIDs were preserved.

| Check | Result |
| --- | --- |
| Authoring/activation RED | 8 tests: 1 passed, 7 expected failures before implementation |
| First integration run | 108/116 passed: legacy missing-field regression plus 7 DataBaseAssetTests failures |
| Legacy compatibility fix | Moved new-asset facility defaults to Reset; kept nested field initializers false |
| New tests after fix | 8/8 passed, including real legacy YAML import and three facility flags through StageGeneratorSystem |
| Final Core chapter regressions | 191/191 passed |
| Final chapter CoreIntegration | 40/40 passed, including the new 8 tests |
| Final chapter + DataBaseAssetTests selection | 109/116 passed; only 7 production asset tests fail |
| DataBaseAssetTests with Phase 2 SO/asset files restored to HEAD | 69/76 passed; identical 7 failures |
| Failure-name comparison | Zero differences between baseline and final DataBaseAssetTests failures |
| Restoration/static checks | Final SO and chapter activation restored; no View diff; unique new meta GUID; no leftover temporary legacy asset |

Fresh review identified the same legacy missing-field failure: true nested initializers survived deserialization. The fix uses the normal Unity authoring Reset callback and keeps the one-boolean schema and approved behavior intact. The previously failing real-import test and all other new tests pass after the fix. No other author's tests were modified.

The final Core chapter run covers the unchanged Phase 1 generator after SO activation. The full Core result from Phase 1 (25 baseline-matching failures) remains the broader regression record; the pure Core production code did not change in Phase 2. DataBaseAssetTests was actually executed here, but its 7 baseline failures below remain unresolved and outside scope. Do not describe the entire project as green.

User-side handoff: in Unity PlayMode, start a new Run and inspect Chapter 1/2 node appearances, connections, and facility sequences. Repetition is reduced, not prohibited, and the graph should not change mid-Run. Re-run DataBaseAssetTests after the unrelated production asset/expectation issues are resolved. Compilation and EditMode tests were exercised; PlayMode and visual acceptance were not. Commit/push were subsequently authorized by the user; unrelated FMOD/font changes are excluded from delivery.

Baseline-matching DataBaseAssetTests failures:
- ProjectL.Tests.CoreIntegration.DataBaseAssetTests.CategoryDefaultBattle_ProductionAssignmentsAndGrowthPools
- ProjectL.Tests.CoreIntegration.DataBaseAssetTests.CategoryDefaultBattle_ProductionMainAndStagePrepareSpecializedTraits(Rider,Leap)
- ProjectL.Tests.CoreIntegration.DataBaseAssetTests.CategoryDefaultBattle_ProductionMainAndStagePrepareSpecializedTraits(Heavy,SteadfastWill)
- ProjectL.Tests.CoreIntegration.DataBaseAssetTests.GrowthProduction_AkimboKeepsPrePresentationTestNativeTrait
- ProjectL.Tests.CoreIntegration.DataBaseAssetTests.GrowthProduction_CannonKeepsPreviouslyLegalTraitWithoutReopeningIt(Taunt)
- ProjectL.Tests.CoreIntegration.DataBaseAssetTests.GrowthProduction_CannonKeepsPreviouslyLegalTraitWithoutReopeningIt(Acceleration)
- ProjectL.Tests.CoreIntegration.DataBaseAssetTests.RunGoldFacilities_ActiveRestReferencesAndOrdinaryShopMatchPurchases
