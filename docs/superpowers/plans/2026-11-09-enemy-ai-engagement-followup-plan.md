# Enemy AI Engagement Progress and Follow-up Implementation Plan

> **For agentic workers:** After design and phase authorization, use `superpowers:executing-plans` for inline execution, or `superpowers:subagent-driven-development` if delegated execution is selected. Steps use checkbox syntax. Stop at each phase boundary; this document does not authorize implementation.

**Goal:** Correct missing approach and move-then-attack value while preserving current combat, safety, and publication contracts.

**Architecture:** Reuse pursuit attack geometry for a bounded actor-local distance map. Reuse the existing movement projector under explicit effect eligibility, then the existing coarse attack estimator when precise follow-up facts are unavailable. Keep all state temporary and all actual commands on the existing pipeline.

**Tech Stack:** Unity 6000.0.73f1; pure C# Core; existing NUnit Core and CoreIntegration EditMode assemblies.

**Spec:** [Engagement and follow-up design](../specs/2026-11-09-enemy-ai-engagement-followup-design.md)

**Status:** Phases A and B completed on 2026-11-09 after separate explicit authorizations. Phase C functional verification (new integration fixture, full-suite comparison) and measurements completed on 2026-11-09. Measurements found a large-map Hold regression after engagement-map truncation; the user chose the fallback, which restores the prior pursuit progress on truncation (fixed and re-measured). Decision work remains about 1.6× the pre-feature corpus. Manual Unity playback not run (see Phase C record).

## Global constraints

- Preserve public APIs, Buffer/result/event schemas, actual combat, ascending actor order, Taunt, strict comparison with Hold, deterministic ties, and rejection continuation.
- Preserve four attacks, four moves, two refined moves, one follow-up aim, eight Preview children per actor, sixteen per decision, and 256 alternate-route expansions; honor lower settings.
- Additional engagement map: at most 289 retained traversable anchors (raised from 256 on 2026-11-09; truncation falls back to representative pursuit progress), at most four edge inspections per anchor, at most one goal evaluation and one reverse-distance dequeue per anchor. Overflow invalidates the entire map; it is not partial distance evidence.
- Preserve all existing coverage/harm fallbacks. `HiddenEnvironment` remains. No new terminal guarantees from coarse estimates.
- No View/Flow/Plan/Job/Framework/DB/assets changes, new runtime registrations, dependencies, global caches, or second combat simulator.
- Test-owned inputs only. Do not silently change other workers' assertions or golden outputs. New code comments are Korean; implementation documents are English; C# additions include `.meta`.
- No commits/pushes by Codex. Recheck concurrent edits and stop/report at every phase boundary.

## Review focus

1. Safe rail prefixes may be lost before scoring: A1 checks both shortlist retention and final selection.
2. Wall detours and alternate firing positions must use a common target/metric, not reward cycles: A1 checks signed progress and equal-distance ties.
3. Regeneration mixed with another unsupported source must not unlock projection: B1 tests callback OR semantics and route provenance.
4. A child context with stale actor/geometry can fabricate follow-up damage: B1 tests moved modifiers, action-use, and source immutability.
5. New map work and coarse attacks can bypass budgets or block cancellation: A1/B1 test caps and yields; C1 measures preparation and publication.

## Exact file map

Prefixes name existing repository directories:

- `C` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/`
- `A` = `C/Application/Combat/`
- `E` = `A/EnemyAi/`
- `T` = `Assets/Tests/EditMode/`

| Task | Files |
|---|---|
| A1 | Modify `E/EnemyPursuitSearch.cs`, `E/EnemyActionCandidates.cs`, `E/EnemyDecisionSession.cs`; create `E/EnemyAttackReach.cs`, `E/EnemyEngagementMap.cs` and both `.meta`; create `T/EnemyEngagementProgressTests.cs` and `.meta` |
| B1 | Modify `C/Domain/CombatEffects/Rules/CombatEffectCapabilities.cs`, `C/Domain/Traits/Effects/RegenerationEffectRule.cs`, `A/CombatCommandEffects.cs`, `A/CombatActionAssessment.cs`, `E/EnemyDecisionSession.cs`; create `T/EnemyFollowupEstimateTests.cs` and `.meta` |
| C1 | Create `T/CoreIntegration/EnemyEngagementFollowupPublicationTests.cs` and `.meta`; update these two documents with evidence. No planned production edits |

No scorer formula change is scheduled. `EnemyActionCandidates.EstimateAttackSteps` is consumed unchanged. If a defect in that existing heuristic requires a new damage model, report it outside this plan before modifying it.

## Phase A — Engagement progress

### Task A1: Share reach geometry and score every retained endpoint

**Interfaces consumed:** `MovementRouteValidator.PursuitTraversal`, current pursuit target selection, resumable attack reach, `EnemyActionCandidates.Progress(EnemyDecision)`, and `EnemyScoreFacts.EngagementProgress`.

**Interfaces produced:**

- Extract internal sealed `EnemyAttackReach(CombatCommandInput input, CharacterCombatFacts actor, EntityId targetId, MovementEnvironmentFacts environment)` with existing `TargetSteps() : IEnumerable<int>`, `FromSteps(CellCoord cell, Action<bool> completed) : IEnumerable<string>`, and `From(CellCoord cell) : bool` behavior preserved.
- Internal sealed `EnemyEngagementMap`, with `internal const int CellLimit = 289` (originally 256), get-only `bool IsTruncated`, `int ExpandedNodes`, `bool TryGetDistance(CellCoord cell, out int distance)`, and `long Progress(CellCoord endpoint)`.
- `EnemyEngagementMap.BuildSteps(CombatCommandInput input, CharacterCombatFacts actor, EntityId targetId, Action<EnemyEngagementMap> completed) : IEnumerable<int>` builds one immutable map under design A1–A3. `ExpandedNodes` counts component and reverse-distance dequeues, not attack-geometry substeps.
- `EnemyActionCandidates.Progress` keeps its signature and returns map progress, default zero. Expose internal `EngagementExpandedNodes` and `EngagementTruncated` for aggregation into existing session diagnostics; no public diagnostics schema additions.

- [x] Confirm approval for Phase A and the new 256-anchor bound. Capture HEAD, status, relevant hashes, and pre-existing A–D changes. Read current AGENTS instructions and Framework/state-boundary contracts.
- [x] Capture a fresh focused baseline for the existing pursuit/candidate/Utility suites listed below, then full Core and CoreIntegration entry baselines sequentially before Phase A production edits. Retain full failure names/messages, XML, logs, and source manifest; Phase C compares against this pre-feature baseline, not only an A/B-applied tree.
- [x] Add the following test-owned cases before changing behavior. Seed the controlled rail case from the earlier diagnostic; assert selected intents, not merely helper arithmetic.

| Case | Required assertions |
|---|---|
| `SafeRailPrefixReceivesProgressAndBeatsHold` | Representative endpoint remains lethal; one/two-step safe prefixes receive +20/+40; a safe progress move is retained and executes; never select the lethal rail endpoint |
| `WallDetourReducesRouteDistance` | A necessary first detour increases Manhattan distance but reduces graph distance; positive progress and legal move |
| `EquivalentRoutesShareOnePotential` | Alternate shortest routes receive equal progress; same-distance sidestep is zero; retreat is negative; no positive reward for a distance-preserving loop |
| `AttackGeometryDefinesGoals` | Melee, ranged, directional, splash, occlusion, and destination-range fixtures agree with existing attack reach; already attack-capable origin/endpoint has zero progress |
| `AttackCandidateDoesNotSuppressProgressMap` | Presence of an attack candidate does not force every move's progress to zero; attack candidate ordering is unchanged |
| `UnknownDistancesDoNotInventProgress` | Unreachable goal, no target, exchange-only endpoint, and graph overflow produce zero; no Manhattan fallback or partial-map bias |
| `TraversalAndTargetPolicyRemainOwned` | Ordinary mine blocked; Leap may transit but never use mine/collapse as goal; living obstacles retained; Taunt and selected target unchanged |
| `MapCapsAndPartitionsAgree` | 256-anchor complete component succeeds; discovery of a 257th truncates; goal evaluation/dequeue counts respect bounds; Step partitions 1/2/31 give equal candidates/progress; no retained cross-decision cache |

- [x] Run the new behavioral regression before edits and retain the expected safe-prefix failure. Helper-only cases may be added alongside the new API, but are not a substitute for red behavioral evidence.
- [x] Extract `EnemyAttackReach` without changing its body/semantics; keep existing pursuit wrapper and search order. Run existing pursuit equivalence/resumption tests before integrating the metric.
- [x] Implement the bounded component, shared goal evaluation, and reverse distance BFS. Use the design's unknown/cutoff rules and yield at each edge, delegated reach step, and distance expansion.
- [x] Build the map in candidate generation after the target/Taunt choice and before destination ranking; use finite distance at the existing distance rank position, preserve prior fields/reserved slots, and replace representative-endpoint scoring with signed map progress. Aggregate new counters in the session.
- [x] Run new tests and existing `EnemyPursuitEquivalenceTests`, `EnemyPursuitResumptionTests`, `EnemyActionCandidateTests`, `EnemyUtilityDecisionTests`, `EnemyUtilityScoreTests`, `EnemyHazardForecastTests`, and `EnemyLeapAssessmentTests`. Compare fixed-intent execution and input snapshots.
- [x] Inspect SOLID/SSOT boundaries and work slicing. Record added work, cap behavior, exact intended selection changes, baseline failures, and unresolved compilation/playback status.
- [x] **Stop and report Phase A. Do not implement Phase B.**

## Phase B — Follow-up estimate

### Task B1: Conditional movement projection and coarse one-aim benefit

**Interfaces consumed:** `CombatActionAssessment.Begin`, `Work.MovedFacts/Facts`, `CombatMovementCalculator.BeginProjectRoute`, `CombatCommandComposition.ProjectSteps`, `EnemyActionCandidates.EstimateAttackSteps`, existing Preview budget, and `EnemyUtilityScorer.AttackBenefit`.

**Interfaces produced/changed:**

- `IEffectMoveProjectionRule : ICombatEffectRule` with `bool SupportsMoveProjection { get; }`; only Regeneration opts in initially.
- `AssessmentCoverageSteps(CombatCommandInput input, Action onUnboundedAlliedHarm = null, Action onMoveProjectionBlocked = null)`; existing yielded gaps and harm callbacks unchanged.
- `Work.MovedFacts` can be a conditional input with `Facts.IsPrecise == false`; existing callers must not infer precision from non-null.
- `FollowupSteps(CombatCommandInput moved, EntityId actorId, StageHazardForecast hazards, IReadOnlyList<IMovementTileRule> rules, EnemyDecisionContext budget, bool preciseMove, Action<long> completed) : IEnumerable<int>`; the session supplies the originating facts' precision.

- [x] Confirm separate Phase B authorization; recheck the three shared files changed in Phase A/current earlier work. Capture fresh baseline and fixed-intent move/attack results.
- [x] Add `EnemyFollowupEstimateTests` with the following cases; reproduce the nonlethal Regeneration selection failure before production edits.

| Case | Required assertions |
|---|---|
| `RegenerationKeepsNonlethalMoveAttackValue` | Paired plain/Regeneration fixtures: +60 approach, +200 follow-up contribution, -150 threat => +110 versus Hold 0; both select legal move; no regeneration side-end healing during projection |
| `RemoteRegenerationUsesTheSameContract` | Actor and remote-owner variants preserve gaps and recover the same controlled estimate; no concrete type switch in the session |
| `MixedSourcesKeepProjectionBlocked` | Add unknown binding, held/equipped item, artifact, obstacle, and undeclared effect in separate controls; Regeneration never resets another blocker or its harm callback |
| `RouteUncertaintyCannotCreateFollowupWorld` | Tornado/random arrival, mine risk, unknown arrival rule, and unsupported passenger route keep no added coarse moved input; source values unchanged |
| `MovedWorldOwnsAttackInputs` | Destination attack/range modifiers and move-success state are read from projected actor/geometry; dead, non-field, projected-terminal, attack-used, or Disarmed actor gains no illegal follow-up |
| `CoarseAttackKeepsHarmAndUncertainty` | Direct allied splash plus possible reaction/forced movement bound feed existing `AttackBenefit`; negative net benefit contributes zero; no coarse kills, restrictions, or terminal tier; inherited movement uncertainty stays nonprecise |
| `PreciseMovementCanUseCoarseAttack` | Valid precise moved input plus an unsupported attack effect receives existing delivery estimate with its harm bound, rather than zero merely for nonprecision |
| `NoDoubleChargeForCoarseMove` | Known supported movement damage is charged once through coarse entry scoring; optional projected input does not add another loss, turn, or publication |
| `PreviewExhaustionAllowsOnlyBoundedEstimate` | Existing precise moved input at exhausted budget still gets one coarse aim; zero extra Preview; no budget for added move projection => no moved input/bonus; only first two refined moves can qualify |
| `PartitionAndFixedIntentParity` | Step partitions 1/2/31 yield equal decisions; original inputs unchanged; explicit move/attack commands yield byte-equivalent supported state/event digests before/after |

- [x] Add the optional effect capability and Regeneration declaration; extend the shared coverage traversal with independent OR-style blocking. Do not add a second trait traversal/registry or change `AssessmentGaps` to None.
- [x] Separate effect and route coverage in `Work`. Use the existing charge/project/complete path once when either existing precise eligibility or the design's conditional eligibility succeeds. Preserve coarse facts on the new branch; emit only the temporary follow-up input when eligible.
- [x] Audit all `MovedFacts` consumers. Pass `preciseMove` in the session, remove only the early follow-up budget return, and reuse coarse attack estimation after successful admission with the child context and parent budget.
- [x] Run new cases and existing `EnemyActionAssessmentTests`, `EnemyAlliedExposureTests`, `EnemyEffectHarmScopeTests`, `EnemyUtilityDecisionTests`, `EnemyUtilityScoreTests`, `EnemyPreviewPreparationTests`, `EnemyCombinedHazardAssessmentTests`, `EnemyForecastBindDecisionTests`, and Phase A's new fixture. Include available Regeneration and movement/status lifecycle suites identified by current source search.
- [x] Compare existing gap flags, harm bounds, fixed-intent transitions, Preview counts, and side-end regeneration behavior. Report remaining unsupported traits as limitations, not solved coverage.
- [x] **Stop and report Phase B. Do not begin Phase C.**

## Phase C — Combined contracts and delivery evidence

### Task C1: Verify coupled behavior and publication

**Interfaces:** Existing `ICombatAdvanceService`, synchronous Utility advancement, prepared decision Step/Ready/publish, and existing test-owned CoreIntegration fixtures. No new runtime interfaces.

- [ ] Confirm Phase C authorization and record current source manifests. Locate the pre-Phase-A full-suite baseline from this task. If intervening unrelated changes invalidate that comparison, isolate their contribution or establish a matching pre-feature baseline; do not call failures introduced during A/B pre-existing merely because they occur at Phase C entry.
- [ ] Create `EnemyEngagementFollowupPublicationTests` with rail-prefix, wall-detour, and nonlethal Regeneration move-then-attack scenarios, each at Step partitions 1/2/31. Compare synchronous Utility against prepared execution on identical input, including the next actual attack where applicable.
- [ ] Assert preparation does not mutate stores, one result is published, ordered events and state match, and queued/handled View command numbers remain unchanged. Use existing cancellation/stale-input suites to cover before/at Ready; add a corrected-scenario case only where coverage is missing.
- [ ] Verify mandatory Taunt, deterministic actor/intent order, rejected action continuation, final-night recall, and terminal exit. Include lethal railway and no-benefit controls where Hold remains correct. Do not alter hazard weights to force movement.
- [ ] Run the new fixture and `EnemyUtilityPublicationTests`, `EnemyAdvancePreparationTests`, `EnemyDecisionSourceStampTests`, `EnemyAssessmentBoundaryPublicationTests`, `LeapPublicationTests`, and `PositionSwapPublicationTests`.
- [ ] Run full `ProjectL.Tests.Core` then `ProjectL.Tests.CoreIntegration` sequentially. Compare every baseline/final failure by full name, label, and message; keep existing external failures visible.
- [ ] Run existing corpus, acceptance measurements, and yielded preparation harness on fixed seeds plus all new cases. Record decision differences attributable to A/B, input manifests, Preview/node/cap counters, warmed Step p95/max, preparation updates, and calibrated allocation evidence. Include 256/257-anchor and expensive attack-geometry cases. Preserve historical reference files.
- [ ] Check that no Step synchronously drains the map, projection, or full attack enumeration. If added work creates a material regression, stop and report the evidence and proposed design revision; do not raise limits silently or claim the prior performance backlog is fixed.
- [ ] Update the completion record below with actual changes, failures, evidence locations, expected versus measured independence improvements, and manual verification checklist.
- [ ] **Stop after Phase C. The user owns commits and pushes.**

## Validation commands and acceptance

During authorized implementation, verify the installed Unity path and whether the project is already open. Use the existing Editor Test Runner if it owns the project. For batch execution use Unity 6000.0.73f1 with AppData access through the approved path; do not stop the user's Editor or use cache deletion/version changes as workarounds.

Argument template (runner supplies verified executable and unique absolute output paths):

```text
-batchmode -nographics -projectPath D:\My_ProjectWork\Github_desktop\Project_L
-runTests -testPlatform EditMode -assemblyNames ProjectL.Tests.Core
-testFilter ProjectL.Tests.Core.EnemyEngagementProgressTests
-testResults <absolute-results.xml> -logFile <absolute-editor.log>
```

Use `EnemyFollowupEstimateTests` in Phase B. In Phase C use assembly `ProjectL.Tests.CoreIntegration` and filter `ProjectL.Tests.CoreIntegration.EnemyEngagementFollowupPublicationTests`. Run existing fixtures through their actual current full names. For full assembly runs omit `-testFilter`. Do not add an unconditional early `-quit`.

Acceptance requires completed XML, selected-test counts, log inspection for compiler errors/timeouts, zero new unexplained failures, and the intended decision/contract assertions. Failed-before/passed-after evidence and current source identity are required. Tests belonging to View remain outside scope; no combat View MSBuild.

Manual Unity checklist:

- [ ] Enemy advances to a safe cell before an active railway instead of waiting solely because the representative endpoint is unsafe.
- [ ] Enemy takes a useful wall detour; equal-distance movements do not create obvious back-and-forth loops in a static setup.
- [ ] Regeneration melee enemy can move and then attack when the controlled net benefit is positive.
- [ ] Lethal railway endpoints and negative-net-benefit attacks are still rejected when safe Hold is available.
- [ ] Taunt, damage/Bind presentation, cancellation/abandon, and final-night/terminal transitions remain correct.
- [ ] Evaluate representative melee/ranged party layouts for fun and remaining waits; record scope-external policy issues separately.

## Completion record

### Phase A — 2026-11-09

- The user authorized Phase A in this chat, including the documented 256-anchor map limit. Work continued in the existing feature checkout at `c995dd4555f3b657f7605eeddf8d422650e9d7db`, preserving the prerequisite assessment-boundaries A–D changes. No commits or pushes were made.
- Before implementation, Unity 6000.0.73f1 ran the complete assemblies sequentially: Core **5,326 passed / 25 failed / 8 skipped**, CoreIntegration **1,005 passed / 39 failed / 2 skipped**. All **188** Phase A focused regressions passed inside that Core baseline. These pre-feature XML files and full failure names/messages are retained for Phase C comparison; this is not a claim that the full assemblies passed.
- The new rail-prefix decision regression failed before production edits at partitions 1, 2, and 31: expected Move, actual EndEnemyPhase. Both fixed-intent movement controls passed. The initial zero-test runner and a test-only missing Artifact namespace import were corrected before accepting RED evidence.
- Extracting `EnemyAttackReach` alone passed **112/112** existing pursuit equivalence/resumption cases. The movement/attack geometry implementation is shared, not duplicated.
- Final focused validation passed **213/213** cases: **188** existing regressions and **25** new cases. Safe one/two-step rail prefixes receive one/two progress units, the selected move is safe and legal, wall detours use route distance, and equal-distance/retreat behavior follows one shared potential. Geometry, cutoff, missing targets, mine/Leap/collapse eligibility, Taunt overrides, and full candidate/decision partition equality are covered.
- A new origin-mine case exposed a reverse-edge error during implementation: leaving a blocked origin did not permit reentry. Its RED failure was retained, and reverse propagation now checks the destination's existing traversal eligibility. No movement rule was changed.
- Test setup corrections explicitly made mines enemy-visible and retained the required main fact in the no-field-target case. No production change was made to accommodate those setup mistakes.
- The user explicitly approved changing `EnemyActionCandidateTests.WeakGroundRiderDecisionAvoidsLethalStopAndCanCross`: equally valuable attack-capable cells now use the existing intent tie order, changing the Utility endpoint from (3,1) to (3,0). The test now asserts actual ground traversal and post-move attack reach, retaining survival, no fall, non-ground landing, and cliff conversion checks.
- Both fixed-intent execution digests match RED and final validation. The rail fixture records identical counters across partitions: **88 map node expansions, 1,863 decision work units, 2 Preview calls**. The 256-anchor corridor records 512 combined component/distance expansions; a 257th anchor invalidates the map. All new loops yield through the existing iterator. These are work/cap observations, not latency/allocation acceptance.
- Independent read-only review found no actionable production correctness or architecture defects. Its final-test condition is satisfied. Explicit Taunt-map and full partition identity assertions were added to finish the planned verification details. Full post-change corpus/timing/allocation, preparation/publication regression, and manual Unity playback remain Phase C or user verification work.
- Actual responsibility boundaries match the design: map owns distance; shared reach owns attack geometry; candidate builder owns selection inputs; session only aggregates diagnostics. Scorer weights, combat execution, state ownership, and Framework registration remain unchanged. Pre-existing C# source/test hashes outside Phase A are unchanged.
- Evidence: `C:\\CodexHome\\visualizations\\2026\\11\\09\\01a1bf48-920a-75d3-94ee-86e1b65ad95d\\engagement-phase-a`. See `baseline-core.xml`, `baseline-integration.xml`, `red-final.xml`, `extraction.xml`, `origin-red.xml`, `verified.xml`, paired logs, entry backups/manifests, comparison records, and `progress.md`.
- Unity-generated font changes are restored from byte-for-byte entry backups after the final run. Pre-existing FMOD edits are preserved. Core EditMode compilation/test execution is verified; Play Mode and View acceptance are not. Stop at the Phase A boundary; B/C need separate authorization.

### Phase B — 2026-11-09

- The user separately authorized Phase B. Work continued at the same HEAD with Phase A and prerequisite assessment-boundaries changes present. The five production targets were captured as entry snapshots. The newly present `docs/enemy-utility-ai-designer-guide.md` and all unrelated entry files were preserved.
- Fresh focused baseline passed **202/202**. The additional Regeneration cadence/turn baseline passed **21/23**; the two failures already existed before production edits: `RegenerationCadenceTests.LastEnemyEnd_HealsBeforeRecallAndKeepsPersistentMainProgress` (EnemyWaiting versus Field) and `RegenerationTurnTests.EighthEnemyCompletion_DoesNotHealPersistentMain` (Deployment versus Combat).
- All six owner/remote Regeneration decision cases failed before implementation at partitions 1, 2, and 31: plain input selected Move, while Regeneration selected EndEnemyPhase. All three fixed-intent move/attack controls passed before the fix.
- Effect owners now separately declare movement projection support. Only Regeneration opts in. The shared traversal preserves existing coverage/harm callbacks and accumulates independent projection blockers. Movement assessment retains coarse facts and uncertainty, charges one existing Preview slot, and uses the existing route/completion/projector to supply an optional conditional follow-up input.
- Follow-up assessment uses the moved child context with the parent Preview budget. It retains a bounded one-aim coarse estimate when precision or remaining Preview budget is unavailable. Nonprecise movement cannot acquire precise attack/kill/terminal credit. The existing score weights and actual combat processing are unchanged.
- Final Unity 6000.0.73f1 focused result: **258/260**, with **35/35 new tests passing**. Both remaining failures match the pre-edit names, labels, and messages exactly. No new regression failure remains. This is not an all-green claim for the whole project.
- The controlled nonlethal case now retains **+60 approach +200 follow-up -150 threat = +110**, versus Hold 0, with Regeneration on either owner. Additional cases cover mixed effect/item/artifact/obstacle blockers, route uncertainty and passenger arrival, damage/range modifiers, Disarm/action use, actor death/collapse, one-time movement loss, direct allied splash under the actual Cannon policy, conservative unbounded harm, and coarse kill-reward exclusion.
- All three fixed-intent move/attack digests match RED/final. Partitions 1/2/31 select identical intents with **2 Preview calls, 2 refined moves, and 1,996 work units**. These are bounded-work observations, not latency/allocation acceptance.
- The earlier assistant-authored `EnemyEffectHarmScopeTests` helper now explicitly requests coarse-only assessment through `Begin(..., allowPrecise: false)`. Its assertions and coverage/harm purpose are unchanged; the default assessment may now legitimately produce a conditional move input for Regeneration. No other existing test was modified.
- Retained intermediate logs distinguish test setup errors from behavior defects: the generic trait fixture lacked Regeneration's constructor arguments; the custom arrival tile was outside the frozen serializer; normal attacks do not target friendly characters; HighGround is not a range bonus, so the range control uses Sandstorm. A duplicate capability declaration from patch application was removed before behavioral verification. None required changing unrelated gameplay rules or golden references.
- Independent read-only review compared the five production targets with their entry snapshots and found no actionable correctness or architecture defects. It directly confirmed final XML and digest comparisons. Projection eligibility belongs to effect owners, movement remains in the existing calculator, and the session wires scoring/budget facts without new state ownership, service registration, or a simulator.
- Evidence: `C:\CodexHome\visualizations\2026\11\09\01a1bf48-920a-75d3-94ee-86e1b65ad95d\engagement-phase-b`. See `baseline.xml`, `red-final.xml`, `passed.xml`, paired logs, `comparison.json`, entry and passed-source manifests, `review.txt`, and `progress.md`.
- Unity-generated font changes were restored byte-for-byte; original FMOD changes and all unrelated entry files remain intact. Core EditMode compilation/test execution is verified. Full post-change Core/CoreIntegration regression, prepare/publish/cancellation integration, performance acceptance, and Play Mode remain Phase C or user checks. No commits/pushes; execution stops at Phase B.

### Phase C — 2026-11-09

- The user authorized Phase C in chat after confirming A/B were done. A/B (together with the assessment-boundaries A–D work) are in commit `04c9e3a87` on `feature/Cheon/Core/NewCombatSytem`; its parent `c995dd455` is the pre-feature source. Work ran on another PC (`D:\Github_LocalFile\Project_L`), where the Phase A evidence folder is unavailable, so a new pre-feature baseline was taken by temporarily checking out `c995dd455` (clean tree, no edits/commits there) and returning to the branch. Unity side-effect files (two fonts, FMOD catalog, sequences settings) were restored after every batch run.
- Pre-feature baseline (Unity 6000.0.73f1 batch, both assemblies): Core **5,280 passed / 25 failed / 8 skipped**, CoreIntegration **996 passed / 39 failed / 2 skipped**. The failure counts equal the Phase A baseline; totals differ because this source lacks the A–D/A/B tests.
- Created `Assets/Tests/EditMode/CoreIntegration/EnemyEngagementFollowupPublicationTests.cs` (+ `.meta`), test-owned inputs only, partitions 1/2/31, each comparing yielded prepared execution against synchronous `AdvanceUtility` with store immutability during preparation, one input capture, one published result, unchanged queued/handled command numbers, and equal battle values/results (same harness as `EnemyAssessmentBoundaryPublicationTests`). Player units use attack 0 so player threat does not decide the case.
  - `SafeRailPrefixMovesAndSurvivesTheTrain`: rail row y=2, enemy at (2,5); the enemy stops before the rail, then the train fires at the end of the enemy turn without killing it.
  - `WallDetourMovesAwayInManhattanButCloserInRoute`: wall row y=3 with a right-side gap; the move increases Manhattan distance and goes toward the gap.
  - `RegenerationEnemyMovesThenAttacksInTheSameEnemyPhase`: Regeneration melee enemy moves, then the next published step is its nonlethal attack.
  - `LethalRailEndpointsKeepSafeHold` (control): every reachable endpoint is a rail cell; the enemy holds and survives the train.
- Final run at HEAD + new fixture: Core **5,386 / 25 / 8**, CoreIntegration **1,017 / 39 / 2**. Every failure matches the baseline by full name and message; zero new failures, zero fixed. New fixture **12/12**. Named suites all pass: `EnemyUtilityPublicationTests` 5, `EnemyAdvancePreparationTests` 27, `EnemyDecisionSourceStampTests` 26, `EnemyAssessmentBoundaryPublicationTests` 9, `LeapPublicationTests` 4, `PositionSwapPublicationTests` 6, `TauntPublicationTests` 3, plus `EnemyEngagementProgressTests` 25 and `EnemyFollowupEstimateTests` 35. Existing cancellation/stale-input cases in `EnemyUtilityPublicationTests`/`EnemyAdvancePreparationTests` cover before/at Ready; no extra case was added.
- Failed-before evidence: the new fixture run on `c995dd455` fails only `SafeRailPrefixMovesAndSurvivesTheTrain` at 1/2/31 (old code holds, `PathMovement` null). Wall detour, Regeneration, and Hold control also pass on the old code. So in integration only the rail case discriminates A; the wall case confirms the pursuit route already detoured, and the Regeneration case verifies the move→attack publication coupling, not that B's follow-up value was decisive (in this layout approach progress alone wins). B's decisive-value evidence remains the Phase B unit RED/GREEN cases.
- Not done: the explicit measurement harnesses (`EnemyUtilityAcceptanceMeasurements`, `EnemyAdvancePreparationMeasurements`, `EnemyAiPerformanceMeasurements`, etc.) were not run, so there is no Step p95/max, allocation, or 256/257-anchor timing evidence for Phase C. The no-synchronous-drain check relies on the Phase A/B yield tests and the `PreparationWorkVersion <= partition` assertions in the new fixture. Final-night recall/terminal exit are covered only by existing suites (several night-recall tests fail identically in baseline and final). Manual Unity checklist is unverified.
- Evidence (this PC, scratchpad, not retained in repo): `baseline.xml`/`.log`, `final.xml`/`.log`, `red.xml`/`.log`, `baseline-failures.tsv`, `final-failures.tsv`. No production code changed in Phase C. No commits/pushes.

#### Phase C measurements — 2026-11-09 (user: "측정도 돌려")

- Same harness files and fixtures at both sources (no diff between `c995dd455` and HEAD), Unity 6000.0.73f1 EditMode batch, Intel i7-14700, Windows. Captures: `phase-6-corpus` (`EnemyAiPerformanceMeasurements.CaptureCorpus`), `phase-6-atoms` (`EnemyDecisionStepMeasurements`), `phase-7-behavior` (`EnemyUtilityAcceptanceMeasurements`), `phase-7-service` (`EnemyAdvancePreparationMeasurements`, 1 ms budget, N5/N6/N8/G16). Frozen corpus inputs are byte-identical between runs. Allocation is unavailable on this runtime (4 KiB probe returned 0), so no byte evidence.
- **Attribution limit:** the pre-feature source `c995dd455` lacks both the assessment-boundaries A–D work and this plan's A/B (all landed in `04c9e3a87`). Differences below are A–D + A/B combined. The `expandedNodes` jump comes from engagement-map nodes, which the session now adds to that counter; timing is not isolated per change.
- Corpus (warm decision timings, ms; nearest-rank):

| Fixture | p95 base → head | max base → head | warm side p95 base → head | first-decision work (5 seeds) base → head | cap hits head |
|---|---|---|---|---|---:|
| N5 | 4.76 → 10.03 | 7.16 → 24.20 | 23.0 → 57.7 | 21,435 → 46,830 | 0 |
| N6 | 5.66 → 7.54 | 14.14 → 9.55 | 45.3 → 133.9 | 22,495 → 47,830 | 0 |
| N8 | 4.73 → 8.78 | 5.02 → 26.44 | 53.2 → 74.8 | 24,615 → 49,800 | 0 |
| W8 | 22.13 → 111.79 | 39.61 → 280.09 | 184.0 → 111.9 | 80,440 → 526,300 | 735 |
| U8 | 59.76 → 70.71 | 60.90 → 168.92 | 126.9 → 171.8 | 226,430 → 306,390 | 0 |
| A8 | 5.49 → 14.01 | 21.96 → 27.25 | 60.0 → 155.4 | 23,630 → 56,930 | 0 |
| T8 | 13.99 → 15.24 | 23.54 → 17.96 | 57.5 → 63.2 | 10,030 → 10,030 | 0 |
| R8 | 4.78 → 10.08 | 27.07 → 26.05 | 59.5 → 79.7 | 24,745 → 49,930 | 0 |
| X8 | 9.28 → 8.82 | 19.17 → 25.54 | 60.9 → 85.5 | 25,780 → 50,965 | 0 |
| H8 | 9.97 → 35.65 | 28.94 → 57.36 | 80.0 → 128.5 | 45,605 → 116,215 | 0 |
| G16 | 6.64 → 5.94 | 18.37 → 15.48 | 162.9 → 84.9 | 33,095 → 39,085 | 105 |
| All | 35.90 → 83.85 | 60.90 → 280.09 | 126.9 → 130.9 | 538,300 → 1,300,305 | 840 |

- **Decision change (material behavior regression):** W8 (32×32, wall column with a far gap) first intent changed from Move to EndEnemyPhase on all five seeds. Its reachable component exceeds 256 anchors, so the engagement map truncates (the 735 cap hits), every move gets zero progress, and the enemy holds instead of approaching. Before A, the representative pursuit endpoint gave the move positive progress. G16 also truncates (105 cap hits) but its first intents are attacks and unchanged. All other fixtures' first intents are unchanged (Attack 30 in both; Move 20 → 15, Boundary 5 → 10, all from W8). Production fields are at most 15×15 (225 cells), so truncation cannot occur on current production stages.
- Step timing (`phase-6-atoms`, warmed Steps): 1,259,412 → 3,019,476 Steps; p95 0.0035 → 0.0035 ms; p99 0.0076 → 0.0065 ms; max 8.60 → 10.26 ms; Steps over 1 ms 5 → 19 (12 of them ~3.1 ms with no collection, 7 at 7–10 ms coinciding with a gen0 collection; most labelled `UtilityCandidates`, plus `UtilityFollowup`). First observations over 1 ms 72 → 78; first-observation max 19.22 → 18.69 ms (`Attack/SacrificePlan` both). No single Step drains the map or projection, but more work means more collection-coincident overruns.
- Yielded service (`phase-7-service`, measured repeats): updates 1,945 → 2,954; preparation per update p95 1.032 → 1.027 ms, p99 1.121 → 1.104 ms, max 7.89 → 8.41 ms; updates over 4 ms 2 → 7; atoms over 1 ms 5 → 17 (max 6.91 → 7.60 ms); decision latency p95 6.06 → 8.74 ms; side wall p95 42.8 → 59.8 ms. The 1 ms slicing still holds at p95/p99; spikes increased.
- `phase-7-behavior` fails at **both** sources on its first A8 Hold assertion (`Expected EndEnemyPhase, was Move`, `EnemyUtilityAcceptanceMeasurements.cs:122`). That diagnostic encodes the original Phase 7 over-conservative Hold, which later work already removed before `c995dd455`; it is pre-existing and was not edited. Its schedule-parity rows before the failure (N5–U8, 25 rows) passed slice 1/2/31 and RNG-cursor identity at both sources; only the W8 trace hashes/first intent differ, as above.
- Per the plan's stop rule, no limits or code were changed. Proposed revisions for a decision: (1) when the map truncates, fall back to the prior representative-endpoint progress instead of zero, or size the anchor cap to the board (≤1,024 for the 32×32 fixtures); (2) reduce the roughly 2× per-decision work, e.g. skip map construction for actors whose candidates already include an attack and whose moves cannot change attack value, or build goals lazily. 256/257-anchor and expensive-geometry timing were not separately captured; their work counts remain the Phase A unit evidence.
- **Truncation fallback (user: "대체 점수로 고쳐", 2026-11-09).** `EnemyActionCandidates.Progress` now returns map progress only when the map is complete; when the map is truncated it returns the prior rule (representative pursuit route `tauntRoute ?? fallback` endpoint gets its `StepCount`, other moves 0). `_pursuitPath` is restored for this purpose and cleared on dispose. Destination ranking stays neutral on truncation as designed. Tests: `EnemyEngagementProgressTests.MapCapsAndPartitionsAgree` now asserts at most one rewarded move whose value equals its step count (was: all zero, the regressed behavior); new `TruncatedMapFallsBackToRepresentativePursuitProgress` (20×14 = 280 anchors, partitions 1/2/31) asserts truncation, one rewarded pursuit move, a Move decision that reduces distance, and accepted execution.
  - Full suites after the fix: Core **5,389 / 25 / 8**, CoreIntegration **1,017 / 39 / 2**; all failures match the baseline by name and message; no compiler errors. Engagement unit + publication tests 40/40.
  - Measurements after the fix: corpus first intents are **identical to the pre-feature baseline** (Attack 30, Move 20, Boundary 5); W8 whole-side trace hashes are identical to the baseline. W8 first-decision work 526,300 → 85,815 (baseline 80,440); W8 cap hits 735 → 105 (one truncation per decision, then fallback). Corpus first-decision work All 538,300 (base) → 1,300,305 (before fix) → 859,820 (after fix); the remaining increase on normal fixtures is the engagement map itself. Warm Steps over 1 ms 19 → 9 (baseline 5), Step max 8.65 ms. Service: preparation p95 1.039 ms, p99 1.130 ms, updates 3,271, over 4 ms 8, decision latency p95 12.68 ms.
  - Wall-clock note: identical code with identical work units moved N5 warm p95 between 10.0 and 16.6 ms across runs on this PC, so corpus wall-clock deltas of this size are within run-to-run noise; work units and decision/trace identity are the reliable comparison. `phase-7-behavior` still stops at the pre-existing A8 Hold assertion; the A8 trace hash differs from baseline already at HEAD before the fix (A–D/A/B), with an unchanged Move first intent.
  - Production fields are at most 15×15 (225 anchors); the user plans at most 17×17 (289 anchors), which exceeds the 256 cap on an open board and would use the fallback rather than map progress.
- **Cap raised to 289 (user: "289칸으로 올려", 2026-11-09).** `EnemyEngagementMap.CellLimit` 256 → 289 so a 17×17 empty board completes; the fallback remains for anything larger. Other limits (e.g. the 256 alternate-route expansions in `EnemyActionCandidates`) are unchanged. Cost on fields of 225 anchors or fewer is unchanged because the map only grows with the reachable component. Tests: `MapCapsAndPartitionsAgree` now derives sizes from `CellLimit` (full component of `CellLimit` anchors = `2 × CellLimit` expansions; one more truncates); the fallback test uses 20×15 (299 anchors); new `LargestPlannedFieldBuildsTheCompleteMap` (17×17, partitions 1/2/31) asserts a complete map covering the board corner, origin distance 13, candidate progress equal to map progress with more than one rewarded move, and an accepted positive-progress Move. The corpus fixtures (16×16 and 32×32) are unaffected, so measurements were not repeated.
- Evidence (scratchpad, not retained): `measure/base/*`, `measure/head/*`, `measure/fix/*`, `fix.xml`/`fix.log` (raw CSV/XML/logs, `summary.json` per side), `analyze.ps1`, `measure.ps1`. Unity side-effect files were restored after each source switch; the two edited docs were copied aside and restored around the baseline checkout.

