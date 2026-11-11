# Enemy AI Assessment Boundaries Implementation Plan

> **For agentic workers:** After explicit implementation authorization, use `superpowers:executing-plans` for inline execution, or `superpowers:subagent-driven-development` if the user selects delegated execution. Follow the approved phase boundary, not the entire document automatically. Steps use checkbox syntax; none are completed by this planning task.

**Goal:** Correct review findings 1-3 while preserving existing combat rules, public contracts, ownership, and unrelated AI policy.

**Architecture:** Keep the existing candidate / assessment / forecast / scorer / actual-command pipeline. Introduce effect-owned harm metadata, share the existing Leap mine eligibility, and pass one derived future Bind fact into scoring. Do not add runtime services or a second combat simulator.

**Tech Stack:** Unity 6000.0.73f1; existing pure C# Core assemblies; existing NUnit EditMode Core and CoreIntegration assemblies.

**Spec:** [Enemy AI Assessment Boundaries Design](../specs/2026-11-09-enemy-ai-assessment-boundaries-design.md).

**Status:** Phases A–D recorded on 2026-11-09. Phase D retained 64 entry-baseline failures and passed all nine new integration cases. Unity playback and the performance gates remain open.

## Global Constraints

- Keep public APIs, Buffer schemas, result/event formats, state ownership, save data, and authored assets unchanged.
- Keep ascending actor order, mandatory Taunt, strict candidate-over-Hold comparison, deterministic ties, and rejection progression unchanged.
- Keep shortlist and work limits: four attacks, four moves, two refined moves, one follow-up aim per refined move, eight precise children per actor, sixteen per decision, and 256 alternate-route expansions.
- Keep `HiddenEnvironment` and all existing effect coverage gaps. A harmless classification must not make an unsupported effect precise or certify a terminal result.
- Keep unknown reactions, genuinely unbounded chains, random landing, and unknown effect implementations conservative.
- Do not change View, Flow, Plan, Job, Framework registration, or production composition. Do not introduce another scheduler, simulator, service, or persistent cache.
- Use test-owned inputs. Do not change production SO/catalog values to make tests pass.
- Future code comments and XML documentation must be Korean; implementation documents remain English.
- No commits or pushes by Codex. Stop and report after each phase; continuing requires the next phase to be authorized.
- Preserve unrelated working-tree edits. Never revert or regenerate all assets to obtain a clean tree.
- No View test code or combat View MSBuild. The future tests listed here belong to the explicitly scoped Core work; this plan does not waive restrictions on other workers' existing tests.

## Review Focus

1. A harmless declared effect coexists with Roar, an unknown reaction, an item, or an obstacle: do not clear the other source's conservative harm. Covered by A1.
2. `ImpactBoltEffectRule` implements `IEffectKnockbackRule` without the new declaration: retain the existing owner-attack fallback. Covered by A1.
3. Leap crosses a mine, then enters a tornado or exchanges a passenger: do not exempt uncertain or passenger arrivals. Covered by B1.
4. Existing Bind expires on enemy cleanup, while Rockfall refreshes it before that cleanup: count the new next-opportunity restriction once. Covered by C1.
5. Final-night recall, lethal hazards, and terminal results: do not invent a future action opportunity or suppress HP hazards. Covered by C1 and D1.

## File map

The following are exact repository prefixes, not new directories:

- `C` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/`
- `A` = `C/Application/Combat/`
- `E` = `C/Domain/Traits/Effects/`
- `T` = `Assets/Tests/EditMode/`

| Phase | Production files to modify | New tests, only after authorization |
|---|---|---|
| A | `C/Domain/CombatEffects/Rules/CombatEffectCapabilities.cs`; `A/CombatCommandEffects.cs`; eleven effect files listed in A1 | `T/EnemyEffectHarmScopeTests.cs` and `.meta` |
| B | `C/Domain/Traits/Rules/LeapTraitRule.cs`; `A/CombatMovementCalculator.cs`; `A/MovementEnvironmentCalculator.cs` | `T/EnemyLeapAssessmentTests.cs` and `.meta` |
| C | `A/StageGimmicks/StageHazardForecast.cs`; `A/EnemyAi/EnemyUtilityScorer.cs`; `A/EnemyAi/EnemyDecisionSession.cs`; `A/CombatActionAssessment.cs` | `T/EnemyForecastBindDecisionTests.cs` and `.meta` |
| D | No planned production changes | `T/CoreIntegration/EnemyAssessmentBoundaryPublicationTests.cs` and `.meta` |

No existing test file is scheduled for assertion changes. Reuse accessible existing fixture helpers; construct new test-owned inputs where a helper is private. Do not widen production visibility merely to support tests. If an existing test's asserted behavior conflicts with an intended correction, identify its owner and ask before modifying another worker's test.

## Phase A — Effect harm scope

### Task A0: Establish the current baseline

**Files:** Read the spec, repository/Assets instructions, the A1 sources, and existing AI test fixtures. No production edits in this task.

- [x] Confirm design approval and that Phase A implementation is explicitly authorized. Recheck HEAD and all target file changes, especially concurrent combat and trait work.
- [x] Capture the existing target-file diffs and status without cleaning the tree. Check whether planned test filenames already exist before creating them.
- [x] Run the existing focused Core baseline using the validation procedure below; retain XML, logs, source revision, and current working-tree identity.
- [x] Record each existing failure by full test name and failure message. Do not substitute the historical failure counts from older AI reports.

### Task A1: Make the known effect harm declarations local to their owners

**Files:** `CombatEffectCapabilities.cs`, `CombatCommandEffects.cs`, and these exact files under `E`:

`RegenerationEffectRule.cs`, `LeapEffectRule.cs`, `HuntersMarkEffectRule.cs`, `VanguardEffectRule.cs`, `AccelerationEffectRule.cs`, `StationaryFireEffectRule.cs`, `KnockbackEffectRule.cs`, `PullEffectRule.cs`, `HunterEffectRule.cs`, `BleedingEffectRule.cs`, `DisarmEffectRule.cs`.

**Interfaces:**

- Consumes existing `ICombatEffectRule`, `IEffectAssessmentCoverageRule`, `IEffectKnockbackRule`, binding ownership, and `CombatActionAssessment.Begin`.
- Produces internal `EffectAssessmentHarmScope` with the three values specified in D1, and optional `IEffectAssessmentHarmRule.AssessmentHarmScope`.
- Preserves `AssessmentCoverageSteps(CombatCommandInput input, Action onUnboundedAlliedHarm = null)` and its yielded gaps/callback meaning.

- [x] Add the new test file and `.meta`, using the following named behavioral cases. Start with the existing API so a failing test demonstrates wrong harm, not only a missing interface.

| Test | Input and required assertion |
|---|---|
| `RemoteClassifiedEffectDoesNotExpandMoveOrAttackHarm` | Parameterize Hunter/Bleeding/Disarm on a remote non-acting live unit; use an otherwise safe move and plain non-contacting attack, with four full-HP allies. Require possible allied harm 0 instead of 4000; preserve the exact pre-change coverage flags and source snapshots. |
| `RemovedFalseBoundAllowsUsefulActionBeforeEnd` | Safe approach with no threatened destination or unsupported arrival and only one of those remote traits. Require an accepted useful move rather than immediate phase end; all original inputs remain unchanged. |
| `ExistingSixExemptionsKeepTheirCoverageAndBound` | Each D1 existing exemption keeps its gap and zero contributed bound; do not require precise Preview. |
| `OwnerAttackForcedMovementRetainsConservativeHarm` | Knockback/Pull owning the attack still charges the previous bound; a remote owner or a move does not contribute that particular bound. |
| `UndeclaredKnockbackCapabilityKeepsLegacyFallback` | ImpactBolt retains `ActingAttackOnly` behavior without editing its effect file. |
| `IndependentUnboundedSourcesRemainConservative` | Combine a declared harmless trait with an unknown binding, Roar/kill chain, and existing item/obstacle fallbacks in separate cases. Require each retained source to keep its prior harm behavior. |

- [x] Run the new fixture; save the expected pre-fix failures for the named remote effects. Do not weaken fallback tests if an unrelated effect is actually involved.
- [x] Add the optional capability and declarations exactly as D1. Replace the concrete-type exemption switch with capability interpretation plus the existing semantic knockback fallback. Keep unknown/malformed handling and callback OR semantics explicit.
- [x] Run the new fixture and existing `EnemyAlliedExposureTests`, `EnemyActionAssessmentTests`, `EnemyUtilityScoreTests`, `EnemyUtilityDecisionTests`, and `EnemyThreatFieldTests`. Compare gap flags, Preview counts, and fixed-input actual transitions.
- [x] Review that no scorer/session switch on concrete trait names was introduced and no current source of actual harm was reclassified accidentally.
- [x] **Stop after Phase A.** Report files, red/green evidence, remaining conservative categories, and any baseline failures. Do not begin Phase B until authorized.

## Phase B — Leap mine contact parity

### Task B1: Share eligibility and exclude only skipped mine contacts

**Files:** The three production files and new test file in the file map.

**Interfaces:**

- Consumes existing `LeapEffectRule.AllowsIntermediate`, `CombatProfileReadSet`, `MovementPath`, `SwapPassenger`, and `RouteAssessmentCoverageSteps`.
- Produces internal `CombatMovementCalculator.IsLeapActive(CombatCommandInput input, CharacterCombatFacts actor, CombatProfileReadSet profileReads = null) : bool` and `LeapTraitRule.SkipsMineEntry(bool leapActive, bool isFinalStep) : bool`.
- Existing execution APIs, route validation, and coverage signatures remain unchanged.

- [x] Reconfirm authorization and concurrent changes. Capture fixed-intent actual move results for the reference Leap cases before extracting any helper.
- [x] Add `EnemyLeapAssessmentTests.cs` and `.meta` with these cases:

| Test | Required assertion |
|---|---|
| `IntermediateLeapMineDoesNotAddBlastExposure` | A known mine strictly inside a fixed Leap path contributes no gap/blast exposure by itself and zero possible allied HP loss. Preserve unrelated Leap coverage. Actual execution leaves that mine active and causes no blast damage. |
| `LeapSafeCrossingCanBeatHold` | Make crossing the intermediate mine the useful route; require the Utility decision to choose and execute a useful legal move. Check the decision, not only shortlist membership. |
| `OrdinaryTraversalAndLeapLandingRetainMineRisk` | No Leap on intermediate contact and Leap on final mine contact both retain existing assessment/execution consequences. Known-mine voluntary landing remains excluded from the shortlist. |
| `LeapDoesNotExemptPassengerArrival` | An exchange passenger can activate a mine at the origin even if the mover has Leap; retain that contact's exposed units/structures. |
| `RandomLandingRetainsPossibleMineContacts` | Tornado/unresolved landing keeps its gaps and mine bound; do not use the original route index to prove safety. |
| `NonMineIntermediateEffectsRemainVisible` | Thorn damage, tornado entry, and weak-ground counts retain their existing behavior; the general entry set still contains intermediate cells. |

- [x] Run the new fixture and save the expected pre-fix exposure/selection failure.
- [x] Extract the existing Leap activation expression unchanged into `IsLeapActive`; reuse the small mine-skip predicate in actual execution and assessment.
- [x] Apply the assessment exclusion only to the eligible original-mover contact. Preserve contributions when a passenger or uncertain arrival could still activate the same mine.
- [x] Run the new fixture plus `LeapTraitTests`, `MineEnvironmentTests`, `EnemyActionCandidateTests`, `EnemyActionAssessmentTests`, `EnemyAlliedExposureTests`, `EnemyPursuitEquivalenceTests`, and relevant movement/PositionSwap regressions. Fixed-intent actual results must match the pre-extraction results.
- [x] **Stop after Phase B.** Report contact parity, non-mine checks, unknown landing fallback, and unchanged execution results. Do not begin Phase C until authorized.

## Phase C — Forecast Bind reaches utility scoring

### Task C1: Add one next-opportunity restriction fact and charge

**Files:** The four production files and new test file in the file map.

**Interfaces:**

- Consumes `StageHazardForecast.Evaluate`, existing `HazardExposure` duration/HP/gap fields, `AppliedStatusRules.EndControllerTurn`, and `CombatTurnCalculator.EndsNightAfterEnemyPhase`.
- Adds internal `HazardExposure.AdditionalBindAtNextOpportunity : bool` and `EnemyScoreFacts.AdditionalForecastBind : bool` (default false).
- Extends the internal `EnemyScoreFacts` constructor and `WithPosition` with an optional trailing `bool additionalForecastBind = false`, preserving existing positional callers.
- Produces `EnemyUtilityScorer.RestrictionUnitPermille = 500`; existing immediate restriction calculation uses it without a numeric change. `Score` applies one additional -500 charge; `AttackBenefit` remains unchanged.
- Keeps `PositionFacts`' existing signature: it wires the new exposure fact internally, avoiding reflection/diagnostic signature churn.

- [x] Reconfirm authorization, including the proposed 500-point binary charge in D3. Do not treat approval of Phase A as approval of this scoring decision.
- [x] Add `EnemyForecastBindDecisionTests.cs` and `.meta`. Use existing test-owned Rockfall input patterns and actual boundary processing as the timing oracle.

| Test | Required assertion |
|---|---|
| `ZeroDamageRockfallEscapeStrictlyBeatsHold` | Ordinary non-final night, exposed Hold and a safe legal escape, no other score differences. HP loss 0, Hold additional charge -500, safe move charge 0, selected move executes legally. |
| `ExistingBindThatSurvivesIsNotChargedAgain` | Existing Bind remains after the forecast's owner-side cleanup; additional flag false even with another warning. Assess the flag directly when movement admission disallows a move. |
| `ExpiringBindAndNewImpactChargeOneRestriction` | Existing Bind has one remaining affected-side end; new Rockfall Bind survives actual cleanup. Additional flag true, exactly -500, with no status mutation. |
| `RepeatedKnownOrPossibleBindChargesOnce` | Multiple applicable evidence entries/durations do not sum. Known and possible cases retain their original certainty gaps. |
| `PostActionBindIsTheBaseline` | A precise post-action actor already carries surviving Bind. The immediate restriction delta remains unchanged and the forecast does not charge that same restriction again. |
| `RecallDeathAndTerminalRemoveOnlyFutureBind` | Final-night recall, known actor death, and known terminal cutoff give no additional future Bind charge; existing HP hazard and uncertain-outcome handling remain unchanged. |
| `ScorerPreservesExistingTermsAndFollowupBenefit` | With the new flag false, every existing fixed-point score is unchanged; with it true, final score differs by -500 and `AttackBenefit` is identical. |

- [x] Run the zero-damage end-to-end case before production edits and retain its failure; then add field-level assertions as the internal fields become available.
- [x] Derive `AdditionalBindAtNextOpportunity` in the forecast using D3's baseline lifetime and existing survivor/cutoff information. Reuse status cleanup rules; do not create a parallel status scheduler or change actual Rockfall application.
- [x] Wire the derived fact through `PositionFacts`/`WithPosition` to the scorer, using the same path for Hold, attacks, and moves. Centralize the existing restriction unit; keep `RestrictionDeltaPermille` and follow-up benefit semantics intact.
- [x] Run the new fixture, `EnemyHazardForecastTests`, `EnemyCombinedHazardAssessmentTests`, `EnemyUtilityScoreTests`, `EnemyUtilityDecisionTests`, and existing Rockfall/status lifecycle regressions. Compare predicted lifetime against actual gimmick-before-cleanup ordering.
- [x] **Stop after Phase C.** Report exact intended decision changes and preserved values. Do not begin Phase D until authorized.

## Phase D — Combined contracts and delivery evidence

### Task D1: Verify integration without widening the feature

**Files:** Create `T/CoreIntegration/EnemyAssessmentBoundaryPublicationTests.cs` and `.meta`. No production change is scheduled; if verification finds another defect, report it before expanding the implementation.

**Interfaces:** Consume the existing `ICombatAdvanceService` preparation/publication APIs, `CombatCommandCalculator.AdvanceUtility`, and test-owned CoreIntegration fixtures. No new runtime interface is produced.

- [x] Add three integrated scenarios corresponding to F1-F3; compare synchronous Utility to prepared execution for identical current inputs.
- [x] Assert source stores unchanged during preparation, exactly one published result, unchanged handled View command number/queue, and equal actual result facts/events across the two Utility execution paths.
- [x] Cover cancellation/stale source at preparation and Ready, rejected actual action continuation, mandatory Taunt, deterministic actor/tie order, and Step partitions 1/2/31. Reuse existing suites for shared contracts; avoid duplicating them unless a corrected scenario needs specific integration coverage.
- [x] Run new CoreIntegration tests and existing `EnemyUtilityPublicationTests`, `EnemyAdvancePreparationTests`, `EnemyDecisionSourceStampTests`, `LeapPublicationTests`, `PositionSwapPublicationTests`, and `RockfallPublicationTests`.
- [x] Run the required full `ProjectL.Tests.Core` and `ProjectL.Tests.CoreIntegration` EditMode assemblies sequentially. Compare failures to this task's baseline by full name and message, not counts alone.
- [x] Compare existing corpus/measurement results. Classify every changed decision against F1-F3; require unrelated fixed inputs and fixed-intent execution results to remain unchanged. Do not overwrite golden/reference files merely to obtain passing tests.
- [x] Check no increase in configured shortlist/Preview/node caps, no preparation mutation, and no added synchronous full-drain operation inside a Step. Record timing/allocation regression evidence with the existing harness; do not claim the deferred performance gates are solved.
- [x] Hand the user a Unity playback checklist: useful movement with remote Hunter/Bleeding/Disarm; Leap across a visible mine; zero-damage Rockfall escape; normal damage/Bind presentation; Taunt; cancel/abandon during preparation; final-night and terminal transitions. Automated results do not establish manual View acceptance.
- [x] Update this plan with actual phase outcomes, evidence locations, and unresolved checks in English. Stop after the report. The user owns commits/pushes.

## Validation procedure for future implementation

Use Unity 6000.0.73f1 only, through an approved path that allows AppData. Do not launch Unity during this planning task. Before a future run, verify the installed executable path and whether the Editor already owns this project; use the existing Editor Test Runner when appropriate instead of starting a competing process. Do not terminate the user's Editor.

For an authorized batch run with the project available, use these argument templates (the runner supplies the verified executable and unique absolute result/log paths):

```text
-batchmode -nographics -projectPath D:\My_ProjectWork\Github_desktop\Project_L
-runTests -testPlatform EditMode -assemblyNames ProjectL.Tests.Core
-testFilter ProjectL.Tests.Core.EnemyEffectHarmScopeTests
-testResults <unique-results.xml> -logFile <unique-editor.log>
```

Use the exact new fixture names in B1/C1 for their focused runs. For D1 use assembly `ProjectL.Tests.CoreIntegration` and filter `ProjectL.Tests.CoreIntegration.EnemyAssessmentBoundaryPublicationTests`. For full regressions omit `-testFilter`; run the two assemblies separately. Do not add an unconditional early `-quit` that can prevent test completion.

Success requires completed XML plus log inspection, expected selected-test counts, zero new failures, and no hidden timeout/compile error. Failed-before/passed-after evidence is required for each corrected behavior. A process exit alone is not proof of passing tests. Existing failures and manual playback remain separately reported.

## Completion record

### Phase A

- Phase A was authorized and verified on 2026-11-09 at `c995dd4555f3b657f7605eeddf8d422650e9d7db` in the existing Core feature checkout. The target sources and test path were clean at baseline.
- A0 baseline in Unity 6000.0.73f1 EditMode passed all 72 selected Core AI tests. There were zero baseline failures.
- A1 before the fix passed 14 of 20 new cases. The three `RemoteClassifiedEffectDoesNotExpandMoveOrAttackHarm` cases reported 4000 for both intents. The three `RemovedFalseBoundAllowsUsefulActionBeforeEnd` cases emitted `EnemyPhaseEnded` instead of a move. The test setup used the project's tuple assertion pattern and a supported cardinal method for Pull.
- A1 after the fix passed all 92 selected cases, including all 20 new cases and all 72 baseline cases. All nine fixed-intent records matched for projected results, event and outcome digests, gap flags, and Preview counts. The test fixture also asserts input immutability.
- Independent review found no actionable Phase A defects. Effect owners now declare the scope and the existing aggregator consumes it. Adding a classification uses the effect's own rule and its behavior test. Changing the shared interpretation still requires the focused AI regressions. Coverage, state, and Framework registration retain their existing owners.
- Undeclared effects, Roar, kill chains, equipment, held environment, artifacts, obstacles, and uncertain arrivals retain conservative handling. Knockback, Pull, and undeclared ImpactBolt retain the owner-attack condition.
- Evidence is in `C:\CodexHome\visualizations\2026\11\09\01a1bf70-8658-73e2-baa4-091c5fc336f2\phase-a`. See `baseline.xml`, `red-final.xml`, `green.xml`, the paired Editor logs and source manifests, and `comparison.txt`.
- The Unity batch run rewrote three FMOD and font assets. They were restored to their verified pre-run state. The remaining worktree changes are the Phase A Core files, the new test fixture and metadata, and the planning documents.
- Phases B–D, full Core/CoreIntegration suites, Play Mode, and performance verification are pending.

### Phase B

- Phase B was authorized and verified on 2026-11-09 at `c995dd4555f3b657f7605eeddf8d422650e9d7db` with the existing Phase A changes in the checkout. The three Phase B source files and the new test path had no pre-existing edits.
- The baseline passed all 328 selected Core EditMode cases in Unity 6000.0.73f1. The final pre-fix run of the eight new cases had three expected failures. The intermediate mine added a 1000-point harm bound, Utility ended the enemy phase instead of crossing, and the exchange incorrectly included a structure exposed only by the skipped mine.
- The combined run passed all 336 cases. Safe fixed crossings now contribute zero mine harm, and Utility executes the safe crossing. Ordinary traversal, final contact, passenger arrival, random landing, and unresolved forced arrivals keep their conservative exposure. Thorn damage, tornado entry, and weak-ground entry counts are covered by the fixed-intent controls.
- Seven fixed-intent execution digests match before and after. The arbitrary arrival-rule fixture records the supported movement, event, outcome, structure, and mine facts because the shared snapshot serializer rejects test-only tile types.
- Execution and assessment now share the existing Leap eligibility and intermediate-mine predicate. The assessment retains the general entry set and defers only the known mines skipped by the original mover. A rule that can cause unresolved arrival restores those mine contributions, including structure exposure. The existing movement state, candidate limits, and Framework contracts are unchanged.
- Independent review found no actionable Phase B defects. The Unity batch rewrites to three FMOD and font assets were restored to their verified pre-run state.
- Evidence is in `C:\CodexHome\visualizations\2026\11\09\01a1bf70-8658-73e2-baa4-091c5fc336f2\phase-b`. See `baseline.xml`, `red-final.xml`, `green.xml`, the paired Editor logs and source manifests, and `comparison.txt`.
- Phases C and D, full Core/CoreIntegration suites, Play Mode, and performance measurement are pending.

### Phase C

- The user explicitly requested Phase C on 2026-11-09 after the 500-point binary Bind proposal was explained. Work continued at `c995dd4555f3b657f7605eeddf8d422650e9d7db` with the completed A/B changes present. The four Phase C production files were clean on entry; all pre-existing A/B source and test hashes were preserved.
- Baseline Unity 6000.0.73f1 EditMode validation passed all 96 selected Core cases. Before production edits, all three new zero-damage Rockfall cases (Step partitions 1, 2, and 31) failed with `Expected: Move`, `But was: EndEnemyPhase`, despite a legal safe move.
- The final run passed all 114 cases: 96 baseline regressions and 18 new cases. Coverage includes existing Bind surviving or expiring, impacts on either forecast boundary, precise post-action status as the baseline, one charge for repeated possible evidence and different durations, final-night recall, known death, terminal-after-impact, incomplete terminal projections, and unchanged score/follow-up terms.
- Earlier runs exposed only setup errors in the new repeated-evidence test: a representative index initially coincided with the actor cell, a multi-cell profile was rejected by the existing contract, and warning cells are sorted by coordinates. The final fixture keeps a valid single-cell actor and places an off-cell representative to its left. No production adjustment was made to satisfy those setup errors; the intermediate XML/logs are retained.
- All six fixed-intent execution digest rows (safe move and side end across the three partitions) match before/after. Actual combat settlement is unchanged; the corrected Utility selector now chooses a safe escape for the zero-HP-loss Bind scenario.
- `StageHazardForecast` owns the new derived flag and reuses `AppliedStatusRules.EndControllerTurn`; `EnemyUtilityScorer` owns the shared 500-point restriction unit; `PositionFacts` wires the result for Hold and candidates. No public API, Buffer, Framework registration, authored asset, or state-ownership change was introduced.
- Independent static review found no actionable Phase C production defects. Its passing-test condition was satisfied by the final run. Phase D integration/full suites, performance measurement, and manual View acceptance were explicitly outside that review and this phase.
- Evidence is in `C:\CodexHome\visualizations\2026\11\09\01a1bf48-920a-75d3-94ee-86e1b65ad95d\phase-c`: `baseline.xml`, `red.xml`, `passed.xml`, paired Editor logs, `comparison.json`, source/entry manifests, and `review.txt`. The blocked Git Bash workspace helper was stopped; the execution ledger uses this writable evidence directory.
- Unity-generated changes to the FMOD catalog and two font assets were restored byte-for-byte from verified entry backups. No commits or pushes were made. Phase D remains pending; execution stopped at the Phase C review boundary.

### Phase D

- Phase D ran on 2026-11-09 at `c995dd4555f3b657f7605eeddf8d422650e9d7db` with the completed A–C working changes, including the separately delivered Phase C. Production source and all existing test hashes matched the entry manifests. The permanent Phase D additions are the CoreIntegration fixture, its Unity metadata, and this record.
- `EnemyAssessmentBoundaryPublicationTests` passed all nine cases across preparation partitions 1, 2, and 31. Each case compares the two battle stores before and after publication, verifies preparation leaves the source unchanged, checks one result sequence increment with the pending View command and handled number preserved, and compares the complete result and ordered events. The Rockfall scenario also compares the subsequent impact publication.
- The intended decisions match the earlier red and green evidence. F1 advances despite the remote Hunter, Bleeding, and Disarm bindings. F2 crosses a known intermediate mine and leaves it active without taking damage. F3 leaves a zero-damage Rockfall warning and avoids Bind at the impact boundary. The Phase A, B, and C records preserve 9, 7, and 6 fixed-intent execution comparisons respectively.
- The focused CoreIntegration run passed 87 of 88. The sole failure was the entry-baseline `RockfallPublicationTests.ReturnCleansesBeforeAnOpportunityWithoutHiddenState` with `Sequence contains no elements`. The focused preparation, source stamp, and Utility publication suites covered cancellation and stale input before and at Ready, and rejection continuation. The full Core run also passed the existing Utility tests for mandatory Taunt, ascending actor order, equal-score intent order, and partition parity.
- The initial new fixture run exposed test setup issues with NUnit constraints and Core visibility. The compiled retry showed that the first Leap arena did not give the intended pursuit route and the Rockfall walls violated the connected-wall contract. The final fixture uses existing public result facts, a short mine corridor, and connected walls around the Rockfall escape. The intermediate Editor logs and XML remain available.

| Full EditMode assembly | Entry baseline | Final | Failure comparison |
|---|---|---|---|
| Core | 5,326 passed, 25 failed, 8 skipped | 5,326 passed, 25 failed, 8 skipped | All 25 names, labels, and messages match |
| CoreIntegration | 996 passed, 39 failed, 2 skipped | 1,005 passed, 39 failed, 2 skipped | All 39 names, labels, and messages match |

- The full assemblies ran sequentially in Unity 6000.0.73f1. `regression-comparison.json` preserves the complete names and messages, including the existing Cinemachine type-load failure. The baseline for this comparison is the Phase D entry tree with A–C applied.
- The existing Utility corpus produced byte-identical input manifests for all 55 inputs and byte-identical traces for all 480 rows compared with `Logs/EnemyAi/phase-7-n8-residual-corpus`. The 1,100 warmed decisions preserve actor and action kind. H8 recorded two fewer work units on each of its 100 warmed decisions with the same Preview and node counts. The yielded service harness matched all 600 prepared publications to the synchronous oracle, including the 480 measured publications.
- Configured limits remain four attacks, four moves, two refined moves, one follow-up aim per refined move, eight precise children per actor, sixteen per decision, and 256 alternate-route expansions. Independent review found no actionable correctness defects. Decision, Preview, and route work retain their existing resumable boundaries. Phase B adds a lazy synchronous per-actor effect-binding capture on the first relevant known mine contact. The standard corpus does not characterize its cost with a trait-heavy Leap loadout.
- Historical to current timing observations were 58.7295 to 47.6322 ms for warmed corpus decision p95 and 1.0690 to 1.0426 ms for yielded preparation update p95. The latest warmed Step sample had seven operations above 1 ms and a maximum of 13.2302 ms. These measurements use the existing EditMode harnesses and historical reference captures.
- Managed allocation calibration returned 0 bytes. A temporary copy of the historical native profiler harness provided a separate calibrated N8 seed-1 comparison with one warmup side, one measured side, and a 1 ms preparation target. The empty and 4 KiB probes reported 0 and 4,128 bytes. Preparation measured 5,290,512 bytes over 38 updates against 5,273,576 bytes over 36 historical updates. Execution measured 1,771,408 bytes against 1,770,808 bytes. This sample leaves the existing allocation and Step budgets unmet. The diagnostic source was removed from the checkout after export.
- Evidence is in `C:\CodexHome\visualizations\2026\11\09\01a1bf70-8658-73e2-baa4-091c5fc336f2\phase-d`. See `focused-final.xml`, both `baseline-*.xml` and `final-*.xml` pairs, the matching Editor logs, `regression-comparison.json`, `measurement-comparison.json`, and `native-allocation-comparison.json`. Unity's three FMOD and font asset rewrites were restored byte-for-byte from the entry backups.

#### Unity playback checklist

- [ ] With a remote Hunter, Bleeding, or Disarm unit present, observe a safe enemy approach before the enemy side ends.
- [ ] Watch Leap cross a visible intermediate mine. Confirm the mine remains armed and produces no explosion during the crossing.
- [ ] Use a zero-damage Rockfall warning that covers the enemy's starting cell and leaves an escape cell. Confirm the enemy leaves the warning and avoids Bind at impact.
- [ ] Confirm ordinary damaging Rockfall presents the impact, HP change, and Bind state in event order.
- [ ] Confirm mandatory Taunt selects the taunter while a safer alternative exists.
- [ ] With prepared advancement enabled in the development composition, cancel or abandon while preparation is in flight and confirm no late result is presented.
- [ ] Observe final-night recall and terminal transitions. Confirm pending damage and presentation settle at the correct boundary and no additional enemy opportunity appears after recall or termination.
