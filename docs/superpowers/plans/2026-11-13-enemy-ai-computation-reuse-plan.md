# Enemy AI Computation Reuse Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans for inline execution, or superpowers:subagent-driven-development if the user chooses delegated execution. Steps use checkbox (`- [ ]`) syntax for tracking. Stop after every phase for user review; do not execute this entire document from a single phase approval.

**Goal:** Reduce enemy decision latency by eliminating repeated pure computations while preserving current Utility/beam behavior.

**Architecture:** Extend existing actor-local reach scopes with exact derived-value reuse, and add a separate single-entry assessment visibility cache to the existing decision context. Shared combat queries remain the rule owners. No new service, scheduler, BT, persistent world cache or public gameplay API is introduced.

**Tech Stack:** Unity 6000.0.73f1; existing C# Core assemblies; NUnit/EditMode and existing CoreIntegration measurement tools.

**Spec:** [Enemy AI Computation Reuse Design](../specs/2026-11-13-enemy-ai-computation-reuse-design.md). Read the spec and [measurement report](../reports/2026-11-12-enemy-ai-latency-analysis.md) before execution.

**Status (2026-11-13).** P0 established the frozen baseline. P1–P3 are implemented and retained. P4 recorded integrated verification, inherited failures and the remaining structural step threshold. See the [integrated result](../reports/2026-11-13-enemy-ai-reuse-result.md).

## Global Constraints

- Preserve action availability, generated candidate order, beam reservations, candidate limits, refined-move selection, preview budget charging, score terms, safety filters, actor ordering, tie breaks and rejection fallback.
- Preserve the full selected intent: actor, kind, aim, path cells, and taunt source. Preserve published transitions, event order, final battle facts and RNG state for identical inputs.
- Preserve results across incremental execution partitions, cancellation and retry. WorkVersion, yield count, cache counters and elapsed time may change; they are not gameplay results.
- Preserve the public Core/Buffer/Job contracts and state ownership. No View, FieldCamera, Architecture, SO values, scene, Factory registration or combat scheduler changes.
- Keep the existing 1 ms preparation slice and all search/precision limits. Do not combine iterator work into larger blocking atoms as a shortcut.
- Reuse only pure derived values from immutable input. Do not cache executions, trait state transitions, emitted events, RNG consumption, preview-budget decisions, or mutable command worlds.
- A cache miss, capacity limit or absent cache follows the existing shared calculation. It never means that an action is invalid or that a target can be omitted.
- Retain existing Core paths. Implementation documents are English; code/XML comments are Korean. New or moved `.cs` files require `.meta`; retain GUIDs for moved files.
- Codex must not commit or push. Stop and report after every phase. A changed design or scope requires user review before implementation.
- Use the authorized AppData-access path for Unity execution. Do not upgrade Unity or delete its cache to address the known sandbox issue. Preserve unrelated local changes.
- This plan is Core-only. Do not create View tests or run combat-View MSBuild. Do not rewrite another author's pre-existing failing runtime test without user approval.
- Before accepting a frozen baseline, apply spec §8.0: detect false all-allies harm/holding. Do not preserve an identified defect as a correct oracle, expand the effect-assessment model, or silently repair scoring as part of cache optimization.

## Review Focus

1. Same entity/cell with a different world, actor/profile, or separately supplied environment: P1 scope-mismatch tests must prevent stale values before any cache access.
2. Negative delivery, piercing caps, splash, friendly targeting and pull: P2 differential tests must preserve every target result and selected-aim order, including cache capacity exhaustion.
3. Followup assessment uses the parent's budget context but a different projected environment: P3 must key the actual environment passed to the assessment.
4. Cancellation between callback and iterator completion, then retry: P2/P3 must never expose partially completed entries; P4 checks session disposal and retry integration.
5. Faster computation changes slice boundaries: P0/P4 must compare full behavior across partitions 1/2/31, not equate WorkVersion or require the old number of frames.

Recurring-user-defect gate: `EnemyEffectHarmScopeTests`, `EnemyAlliedExposureTests` and `EnemyForcedMovementAssessmentTests` run before baseline approval and after each implementation phase. Large negative scores require source/scope diagnosis, not a blanket threshold assertion.

## File map

Paths beginning with `Core/` below expand to `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/`. This is a documentation abbreviation, not a new directory or a move.

| File | Planned responsibility/change |
|---|---|
| `Core/Application/Combat/EnemyAi/EnemyAttackReach.cs` | Bound existing `Relocations`; per-position damage/preparation; optional bounded delivery reuse |
| `Core/Application/Combat/EnemyAi/EnemyEngagementField.cs` | Pass existing input/actor/geometry to the scope constructor; no search-policy changes |
| `Core/Application/Combat/CombatAttackQueries.cs` | Shared selection parameter preparation and target-membership helper; retain unprepared path |
| `Core/Application/Combat/EnemyAi/EnemyDecisionContext.cs` | Single-entry assessment visibility reuse |
| `Core/Application/Combat/CombatActionAssessment.cs` | Consume visibility through context using the actual input environment |
| `Assets/Tests/EditMode/EnemyDecisionReuseParityTests.cs` + `.meta` | New current-behavior baseline capture and comparison |
| `Assets/Tests/EditMode/EnemyAiReuseMeasurements.cs` + `.meta` | New focused measurement adapter using existing fixtures and recorder patterns |
| `Assets/Tests/EditMode/CombatAttackPreparationTests.cs` + `.meta` | New prepared/unprepared shared-query equivalence tests |
| `Assets/Tests/EditMode/EnemyAttackReachReuseTests.cs` + `.meta` | New scope, position and delivery-reuse tests |
| `Assets/Tests/EditMode/EnemyAssessmentVisibilityReuseTests.cs` + `.meta` | New visibility identity/cancellation tests |
| `docs/superpowers/reports/2026-11-13-enemy-ai-reuse-evidence/` | Baseline manifests, immutable semantic references and phase summaries |

Read-only dependencies include `EnemyDecisionSession`, `EnemyDecisionSettings`, `EnemyDecisionSideCache`, `CombatEffectEngine`, `CombatAttackCalculator`, `MovementEnvironmentFacts`, the advance service and Plan. Changing them beyond measurement-only diagnostics is not implicitly authorized by this plan.

## Execution and measurement commands

The current machine's existing helper is `Logs/EnemyAi/phase-2-work/run-unity.ps1`. It rejects an already-running Editor and duplicate evidence-directory names. It is a local tool, not a portable repository dependency. Use it when present; otherwise run the equivalent Unity command below with a fresh output directory. Launch through approved AppData access and do not close an interactive Editor without coordination.

```powershell
& './Logs/EnemyAi/phase-2-work/run-unity.ps1' `
    -RunName '<fresh-run-name>' -Assembly 'ProjectL.Tests.Core' `
    -Filter '<full-test-class-or-method>' -Capture '<capture-mode>'
```

Equivalent Editor arguments: `-batchmode -nographics -projectPath <absolute-project> -runTests -testPlatform EditMode -assemblyNames <assembly> -testFilter <filter> -testResults <absolute-new-results.xml> -logFile <absolute-new-unity.log>`. Set `PROJECTL_ENEMY_AI_RAW` to the fresh output directory and `PROJECTL_ENEMY_AI_CAPTURE` to the named capture mode. Start background helpers with `-WindowStyle Hidden`. Omit the filter for a full assembly run. Do not add `-quit` to interrupt the test runner.

For every command: require a fresh NUnit result with the expected executed tests, `failed=0`, and no ignored capture due to a missing mode. A process exit without results is not a pass. Record timings separately from assertions. Use the exact test names below as filters once those tests exist.

---

## Phase P0 — Freeze behavior and qualify reuse opportunities

### Task 0.1: Establish an immutable current-behavior oracle

**Files:** Create `Assets/Tests/EditMode/EnemyDecisionReuseParityTests.cs` and `.meta`; create the evidence directory's `baseline-default.json`, `baseline-current-tuning.json` and `baseline-manifest.json`. Read existing `EnemyUtilityAcceptanceMeasurements`, `EnemyAiBenchmarkFixtures`, `AttackPreviewParityTests` and `EnemyDecisionBudgetTests`.

**Interfaces:** Use existing `EnemyDecisionSession`, `CombatCommandCalculator.AdvancePrepared`, `CombatCommandComposition.Project` and canonical serialization helpers. Add test entry points `CaptureCurrentBehavior`, `FrozenDecisionsAndResultsMatch` and `BaselineRejectsOverwriteAndChangedIntent`. Explicit capture mode: `reuse-baseline`; verify mode never writes references.

- [ ] Record current HEAD, source hashes, dirty-file snapshots, Editor/runtime and test-owned input hashes. If relevant source changed since the latency report, regenerate timings rather than treating old numbers as current.
- [ ] Before capture, run the three harm-scope suites above. Add `IrrelevantRemoteTraitDoesNotIncreaseCandidateHarm`, `UnrelatedAllyDoesNotInflateHarmBound`, and `OutsideReachSourceDoesNotCreateGlobalHarm` to the new parity test file using bounded test-owned scenes; assert the same candidate's harm bound/allied-loss term and retain real local-blast exposure as a positive control. An identified false hold blocks affected baseline approval and requires a separately reviewed correction; never normalize it by recording the buggy outcome as correct.
- [ ] Implement a test-only trace helper that records full `EnemyDecision` intents, candidate score/tier/lethal/precision facts exposed by diagnostics, actual accepted/rejected transitions, preview charges, event order and final state/RNG. Exclude times, counters tied to iterator scheduling and object identity from semantic equality.
- [ ] Capture both explicit settings `50/50/50` and `20/20/200`, the 11 fixture IDs and five seeds defined in the spec. Exercise full sides and rejected-action continuation. Write references only if absent; record their SHA-256 hashes.
- [ ] Add `FrozenDecisionsAndResultsMatch` with partitions 1/2/31 and cache-independent input immutability assertions. `BaselineRejectsOverwriteAndChangedIntent` must prove an existing reference cannot be overwritten and a deliberately altered intent is detected using temporary test data, without editing the real baseline.
- [ ] Run `CaptureCurrentBehavior` with `-Capture reuse-baseline`, then the two verification methods with capture disabled. Expected: references are created once; all settings/fixture/seed/partition cases match the unchanged code; overwrite/change guards pass.

### Task 0.2: Qualify duplicate work and performance controls

**Files:** Create `Assets/Tests/EditMode/EnemyAiReuseMeasurements.cs` and `.meta`; write evidence `p0-opportunities.json` and `p0-performance.md`. Temporary instrumentation may be applied at existing query/candidate call sites, archived as a diagnostic patch, then removed; it is not a new production telemetry API.

**Interfaces:** New explicit measurement `CaptureReuseWork`, mode `reuse-work`; reuse existing `EnemyAiBenchmarkFixtures` and the prepared-service recorder. Honor both explicit tuning profiles. Count builds at actual computation sites, not just wrapper calls.

- [ ] Record per-scope position/aim attempts and unique keys, common-damage builds, selection-parameter builds, delivery builds, repeated delivery keys, visibility source/observer repeats, and peak entries. Verify effect query evaluation is pure and all inputs are bound by the proposed scope.
- [ ] Include failed deliveries and cap-heavy attack cases in the opportunity sample. Report the duplicate-key rate before selecting P2; do not infer it from the broad `UtilityCandidates` timing alone.
- [ ] Capture three warmed baseline runs of targeted N8/A8/W8/U8 cases and the unchanged prepared-service path. Keep seed/settings and measurement mode matched. Keep detailed step instrumentation separate from whole-operation timing; archive initial-run costs independently.
- [ ] Record allocation support honestly. If calibration returns zero, keep allocation bytes unavailable rather than reporting zero allocation.
- [ ] Remove transient instrumentation and verify source hashes. Run the baseline comparator once more. Report the P1 opportunity, P2 eligible/skippable status and the baseline variability to the user.

**P0 review stop:** No optimization is implemented. Wait for approval of P1; do not treat captured baselines as proof of future behavior preservation.

## Phase P1 — Reuse per-position attack prerequisites

### Task 1.1: Add prepared selection without changing shared rules

**Files:** Modify `Core/Application/Combat/CombatAttackQueries.cs`; create `Assets/Tests/EditMode/CombatAttackPreparationTests.cs` and `.meta`.

**Interfaces:** Introduce nested `CombatAttackQueries.SelectionPreparation`; `PrepareSelection(input, actor, profile, environment)` and `ReadSelectionParameters(input, actor, profile, environment)` with the exact return types in spec §5.3. Append optional `SelectionPreparation preparation = null` to existing `SelectionSteps`, `DeliverySteps` and `DeliveryCheckSteps`. Existing callers remain valid.

- [ ] Add `PreparedAndUnpreparedQueriesHaveIdenticalResults` cases for Single/Direction/All, splash, piercing caps, pull, friendly/neutral targeting, Cannon exclusion, terrain and destination modifiers. Assert failure reasons, selected/affected cells, ordered hits and blocked contacts, not merely success booleans.
- [ ] Add `PreparationRejectsDifferentSourceReferences` for changed input, actor, profile and normalized environment. Add `UnpreparedIteratorRemainsLazy` to prevent moving validation/evaluation to iterator construction.
- [ ] Run the new tests and verify the intended missing API/behavior fails before implementation.
- [ ] Extract the current policy/range expressions into one shared helper. A supplied preparation validates its source references on iteration; the default path uses the helper without allocating a preparation object. Keep all target-specific/aim-specific and effect-execution work where it is.
- [ ] Run `CombatAttackPreparationTests` and `AttackPreviewParityTests`. Expected: prepared/unprepared values are identical; existing frozen preview and actual semantics remain unchanged.

### Task 1.2: Integrate bounded lifetime with existing reach reuse

**Files:** Modify `Core/Application/Combat/EnemyAi/EnemyAttackReach.cs` and `EnemyEngagementField.cs`; create `Assets/Tests/EditMode/EnemyAttackReachReuseTests.cs` and `.meta`.

**Interfaces:** `Relocations(CombatCommandInput input, CharacterCombatFacts actor, EnemyDecisionContext.ObservedGeometry geometry)` and `RequireScope(CombatCommandInput input, CharacterCombatFacts actor, MovementEnvironmentFacts environment, EnemyDecisionContext.ObservedGeometry geometry)`. `EnemyAttackReach` keeps its existing external constructor shape; the engagement field supplies the bound scope.

- [ ] Add `RepeatedPositionTargetsMatchUncachedReach`, `DifferentPositionKeepsDamageAndRangeIndependent`, `SameGeometryDifferentEnvironmentIsRejected`, and `NewWorldCannotJoinExistingScope`. Include disarm-cleared hypothetical actors and identical IDs with different facts.
- [ ] Add `NoAimDoesNotBuildDamageOrSelection` using measurement counters, and verify a repeat target query at a populated position performs fewer prerequisite builds. The tests must also compare actual reach results, not only dictionary sizes.
- [ ] Run the new cases to observe the intended missing reuse/guard failures.
- [ ] Bind and validate the existing scope before any lookup. Store lazy damage and preparation per cell, using existing shared calculators. Keep transient behavior when no scope is supplied; do not grow a new fallback-pursuit dictionary.
- [ ] Run `EnemyAttackReachReuseTests`, `EnemyPursuitEquivalenceTests`, `EnemyPursuitResumptionTests`, `EnemyEngagementFieldTests`, `EnemyCandidateBeamTests`, `EnemyDecisionBudgetTests` and `EnemyDecisionReuseParityTests`. Expected: no behavior/charging mismatch; fewer real prerequisite builds where repeated positions occur.
- [ ] Rerun the three harm-scope suites and P0 false-global-harm counterexamples before retaining the phase; unchanged final behavior alone is insufficient.
- [ ] Run three matched `CaptureReuseWork` comparisons with both tuning profiles. Report medians/tails, counts, entry bounds and variability; archive the semantic comparison results.

**P1 review stop:** Report exact changes and evidence. If reuse has no demonstrated benefit, revert it and report. Ask for the next phase only after presenting the concrete result. P2 needs the opportunity gate below, not an assumption that more caching helps.

## Phase P2 — Optional completed-delivery reuse

### Task 2.1: Gate and implement target-independent delivery reuse

**Files:** Modify `Core/Application/Combat/EnemyAi/EnemyAttackReach.cs`, `Core/Application/Combat/CombatAttackQueries.cs`, and the new `EnemyAttackReachReuseTests.cs` from P1.

**Interfaces:** Add `DeliveryHitsTargetSteps(ResolvedAttackDelivery delivery, EntityId targetId, Action<bool> completed)` to `CombatAttackQueries`. Extend `Relocations` with optional internal `int deliveryCapacity = 256`; use 0 for uncached differential verification. No SO/settings/public API change.

- [x] At the P1 review point, record whether repeated `(cell, aim)` delivery keys remain in a useful measured case. If not, mark P2 skipped, retain P1 and proceed only after the next phase approval. Do not implement an unused cache.
- [x] Add `CachedDeliveryMatchesEveryRequestedTarget`, `NullDeliveryIsACompletedCachedFailure`, `CapacityZeroAndFullPreserveIntents`, and `DifferentEnvironmentCannotHitCachedDelivery`. Include full ordered piercing/splash results and failures.
- [x] Add `CancelledOrFaultedMissDoesNotPublishEntry`: dispose the delivery enumerator before completion, including after its result callback but before the iterator finishes, then repeat the query and compare with a fresh uncached calculation. Exercise a throwing query and ensure it leaves no completed entry.
- [x] Run the tests red, then extract the existing target-membership loop into the shared helper. Add the bounded insertion-only cache to the existing scope; publish only completed entries and distinguish cached null from absence. Capacity exhaustion runs the same uncached delivery path.
- [x] Run the P1 focused suite plus `EnemyUtilityDecisionTests`, `EnemyFollowupEstimateTests`, `EnemyActionAssessmentTests` and the frozen current-behavior comparator. Expected: exact intents, precise/coarse behavior and preview charging remain unchanged at capacities 0, 1 and 256.
- [x] Rerun the three harm-scope suites and P0 counterexamples; a cached failure or unknown result must not become an all-allies-loss signal.
- [x] Run three matched timing/count captures. Retain P2 only with repeatable benefit beyond noise and acceptable entry growth. If memory allocation cannot be measured, report that limitation and do not claim zero overhead.

**P2 review stop:** Report retained or skipped/reverted, with the reason. Changing the 256-entry policy, adding eviction, extending lifetime or using a cached miss to prune candidates requires a design review.

## Phase P3 — Reuse assessment visibility projection

### Task 3.1: Add one completed visibility entry per context

**Files:** Modify `Core/Application/Combat/EnemyAi/EnemyDecisionContext.cs` and `Core/Application/Combat/CombatActionAssessment.cs`; create `Assets/Tests/EditMode/EnemyAssessmentVisibilityReuseTests.cs` and `.meta`.

**Interfaces:** `EnemyDecisionContext.AssessmentVisibilitySteps(MovementEnvironmentFacts source, Owner observer, Action<MovementEnvironmentFacts> completed)` returns `IEnumerable<int>`. The method always uses `preservePublic: true`; it does not replace or share `GeometrySteps`' ordinary visibility cache.

- [ ] Add `SameSourceAndOwnerMatchesFreshPublicProjection`, `DifferentSourceOrOwnerRebuilds`, and `PublicAndObserverOnlyProjectionsStayDistinct`. Assert tile values, known-to membership, ordering and unchanged source facts.
- [ ] Add `FollowupUsesItsActualEnvironmentWithParentBudgetContext` for a moved/projected input whose environment differs from `context.Input`. Add `IncompleteVisibilityEnumerationIsNotPublished`, including disposal after the underlying callback but before final iterator completion.
- [ ] Run the tests red, then implement one completed `(source reference, observer, result)` entry. On a miss, run the existing iterator completely before replacing the entry; on a hit, return resumably. Do not cache public command inputs or coverage results.
- [ ] Route only assessment visibility through the new method, passing `input.State.Environment` from that assessment and the actual actor owner. Keep candidate intent, world copy, coverage and route checks unchanged.
- [ ] Run the new tests, `EnemyActionAssessmentTests`, `EnemyFollowupEstimateTests`, `EnemyForcedMovementAssessmentTests`, `EnemyCombinedHazardAssessmentTests`, `EnemyDecisionSideCacheTests`, `AttackPreviewParityTests` and `EnemyDecisionReuseParityTests`. Expected: exact hidden-information, movement, final-state and budget parity.
- [ ] Rerun `EnemyEffectHarmScopeTests` and `EnemyAlliedExposureTests`; visibility reuse must not broaden the set of exposed allies or reinterpret a gap as global harm.
- [ ] Repeat matched U8 and environmental-case measurements. Report the actual eliminated projection builds and timing; do not claim the entire previous Visibility phase time was removed.

**P3 review stop:** Report measured benefit and shared-contract regression results before integration. Effect-coverage caching remains outside scope even if it becomes the next bottleneck.

## Phase P4 — Integrated verification and handoff

### Task 4.1: Establish final behavioral and latency evidence

**Files:** Update only the phase evidence and final report unless a defect within the approved implementation requires a targeted fix. No new feature work.

- [ ] Run the frozen current-behavior comparator for both tuning profiles, all fixtures/seeds and partitions 1/2/31. Exercise cancelled/rejected/stale preparation and compare complete intents, transitions, event order and final RNG/state. Do not regenerate the P0 baseline to make a mismatch pass.
- [ ] Recheck spec §8.0 and list any suspicious holds with source, gap/exposure scope, possible allied loss and score terms. Report unresolved assessment defects separately from performance gains.
- [ ] Run full `ProjectL.Tests.Core` and `ProjectL.Tests.CoreIntegration` assemblies. Diagnose any failure against the pre-change baseline. Do not mask known integration issues or treat Core success as View/PlayMode success.
- [ ] Run the existing prepared capture: assembly `ProjectL.Tests.CoreIntegration`, filter `ProjectL.Tests.CoreIntegration.EnemyAdvancePreparationMeasurements.CaptureYieldedOpenEncounters`, capture `phase-7-service`. Require published-result/final-state parity with its synchronous reference. Use the same fixture construction and settings for before/after; do not conflate these fixtures with the pure Core corpus.
- [ ] Re-run matched pure Core workload measurements through `CaptureReuseWork`; record both tuning profiles, first-use versus warm results, query counts, cap hits/entry limits, median/tail time and preparation slots. Keep the 1 ms budget unchanged.
- [ ] Check that no new long atomic operation was introduced by cache population. Use existing per-step/structural-atom measurement where needed; a shorter total time cannot excuse a new frame stall.
- [ ] Produce `docs/superpowers/reports/2026-11-13-enemy-ai-reuse-result.md` with retained/skipped phases, source revision/hashes, semantic parity, measured improvements/regressions, memory-calibration limits and remaining workloads.
- [ ] Hand off a representative PlayMode check: measure previous playback completion → preparation ready → execution/publication → next presentation start. Repeat on a supported lower-spec CPU when available. Keep these items explicitly unverified until evidence exists; do not label synthetic Editor update time as gameplay latency.
- [ ] Verify unrelated work is preserved, all temporary diagnostics are removed or explicitly retained as measurement tooling, `.meta` files accompany new scripts, and no commit/push was performed.

**P4 review stop:** Deliver the result and remaining external verification. Core optimization completion and real-game latency acceptance are separate statuses.

## Plan self-review

- Spec §3 invariants are enforced by P0/P4 and the phase-local differential checks; timing/counters are intentionally excluded from semantic equality.
- Scope identity, including the separate environment argument highlighted by the design reviewer, is covered by P1 and rechecked on P2 cache hits.
- Public API, Framework registration, Buffer ownership, Job timing and unrelated View work have no planned modifications.
- Conditional delivery reuse has an explicit evidence/skip gate. Geometric rejection and broader caches are recorded as deferred, not hidden follow-on tasks.
- P3 uses actual followup environment identity and preserves `preservePublic: true`; it does not cache command-sensitive coverage.
- Every phase ends with a concrete report and user review. The plan does not authorize implementation on its own; execution status is recorded above and in the phase evidence.

## Suggested model allocation

This is a task-risk recommendation, not a benchmark result or an instruction to switch the current model automatically. OpenAI's [Work/Codex model guidance](https://learn.chatgpt.com/docs/models?surface=web) positions GPT-6.1 Sol for repeated complex work and Astra for the hardest workflows. The phase mapping below is our project-specific judgment; account availability and client effort labels may differ.

| Phase | Suggested model / reasoning effort | Reason |
|---|---|---|
| P0 | GPT-6.1 Sol / High; Astra / High for a reproduced harm-scope ambiguity | Baseline/measurement implementation is bounded; deciding whether a conservative hold is a real defect needs stronger review |
| P1 | GPT-6.1 Sol / High | Existing shared calculations, explicit per-position scope and differential tests |
| P2 | GPT-6 Astra / High | Cross-target delivery equivalence, negative entries, lifetime and cancellation are the highest-risk boundary |
| P3 | GPT-6.1 Sol / High | Narrow one-entry reuse with explicit environment/observer identity |
| P4 | GPT-6.1 Sol / High for execution/reporting; Astra / High for final semantic and harm-scope review | Separate routine verification work from the hardest correctness judgment |

Using GPT-6.1 Sol / High throughout is a reasonable simpler option, with Astra reserved for unresolved defects/review. Model choice never replaces the semantic oracle or the harm-scope gate. No Max/Ultra requirement is imposed.
