# Enemy AI Decision Latency Investigation — 2026-11-12

## Outcome and scope

The measured cost is concentrated in obtaining facts for candidate evaluation, not in the weighted utility sum. Preserve the existing selection policy first. Prioritize exact reuse of attack-query work and earlier rejection of geometrically impossible attack positions. A Behavior Tree or whole-combat dynamic programming rewrite is not supported by this evidence.

This investigation measured the current code and identified improvement candidates. It did **not** implement an optimization or change gameplay behavior, tuning assets, search limits, or the preparation budget. Temporary diagnostic labels and benchmark configuration were restored byte-for-byte afterward.

## Method and evidence

- Source revision: `360ab06e9e626cd3aaffcf10fe5d279fbde66135`.
- Unity: `6000.0.73f1`, Windows 11, AMD Ryzen 7 6800H.
- Unity ran through the authorized AppData-access path. No cache deletion or Editor upgrade was needed.
- Existing Core fixtures: N5/N6/N8 normal encounters; W8 wall detour; U8 disconnected wall; A8 mixed targeting and piercing; T8 taunt/restriction; R8/X8/H8 environmental combinations; G16 larger encounter/board.
- These are test-owned inputs, not production encounters. They do not cover every trait or future combination.
- Whole-operation corpus: 11 fixtures × 5 seeds × 20 warmed measurements = 1,100 samples, plus warmups and complete-side traces. Uses default tuning `50/50/50` and no cross-decision side cache. This times selection **and execution**, not just preparation.
- Per-step runs: 11 fixtures × 12 warmed samples, seed 1; one initial sample and three warmups per fixture. Separate runs used unchanged labels, detailed candidate labels with default tuning, and detailed labels with the current SO values `20/20/200` explicitly supplied to the measurement. Assets were not changed.
- Prepared-service capture: four service-owned fixtures × five seeds × four measured sides, plus one warmup side per seed. Uses the actual preparation service, its 1 ms budget, and its side cache, with default tuning. Its fixture construction differs from the pure Core corpus despite the shared names.
- All five dedicated measurement invocations passed. The prepared-service measurement also compared each published result and final battle values with its synchronous Utility reference.
- Default detailed instrumentation preserved decision kind, step count, preview count, and node count in all 143 per-step records. This is a counter-level instrumentation check, not a complete intent-equivalence test.

Local evidence folders under `Logs/EnemyAi/`:

| Folder | Contents |
|---|---|
| `20261112-latency-audit` | Source manifest, original-file backups, temporary instrumentation patch, summary scripts |
| `20261112-latency-current-atoms` | Unmodified per-step baseline |
| `20261112-latency-current-corpus` | 1,100 warmed whole-operation samples, input hashes and side traces; `corpus-summary.json` |
| `20261112-latency-detail-default` | Detailed candidate phase measurements with default tuning |
| `20261112-latency-detail-current-tuning` | Detailed measurements using `20/20/200`; `phase-summary.json` |
| `20261112-latency-prepared-service` | Preparation slices, published-result parity and `latency-summary.json` |

Allocation calibration failed (`allocationProbeBytes=0`), so allocation measurements are unavailable; zero allocations must not be inferred. Incremental GC was enabled. Time includes scheduling/GC interference. Small-sample tails and initial samples are not portable performance guarantees.

## Findings

### 1. Candidate evaluation has different bottlenecks by encounter

Current SO-equivalent tuning, seed 1, median of 12 warmed per-step runs:

| Fixture | Decision preparation work (ms) | Steps | Previews | Significant measured component |
|---|---:|---:|---:|---|
| N8 | 15.09 | 15,743 | 6 | Attack-position evaluation and several smaller assessment stages |
| A8 | 37.24 | 32,950 | 5 | Selection policy/range: 5.42 ms / 498 yields; aim enumeration: 5.30 ms / 2,212 yields; common damage: 1.82 ms / 168 yields |
| W8 | 21.70 | 17,232 | 2 | Fallback pursuit and its route-safety work: 13.27 ms / 9,246 yields |
| U8 | 38.68 | 51,944 | 14 | Move-route projection: 10.83 ms; visible-world preparation: 5.83 ms; followup preparation: 4.18 ms |
| G16 | 8.87 | 10,365 | 6 | Actor ordering and threat preparation; engagement field reaches its size limit |

These times are sums of instrumented `Step` intervals, not rendered frame latency or full service elapsed time. Yield counts are work indicators, not equal-cost CPU instructions. Phase names identify yielded scopes; for example `SelectionPolicy` includes both policy construction and range lookup.

In A8, engagement distance propagation itself took only **0.38 ms**, versus **5.42 ms** for policy/range preparation and **5.30 ms** for aim enumeration inside attack-position checks. Replacing distance propagation with another DP implementation is therefore a poor first target for this case.

G16 being faster than N8 is not evidence that more enemies are cheaper. The pure Core G16 fixture has a larger board; the engagement field's 289-cell limit triggers a different path. Workload shape and fallback behavior must be recorded alongside unit count.

For an uninstrumented whole-operation cross-check, default-tuning warmed medians were N8 **13.38 ms**, A8 **54.61 ms**, W8 **19.64 ms**, and U8 **53.25 ms**. These include actual execution and have different settings/sample counts, so they must not be subtracted from the preparation-only measurements or treated as optimization before/after results.

### 2. The frame budget turns work into visible waiting

Production composition enables prepared enemy advancement. `CombatAdvanceService.BudgetSpent` targets 1 ms of preparation per frame and blocks a second preparation/execution spend in that frame. Individual atoms can overrun that budget.

The restored-code service capture measured:

| Service fixture | First decision samples | Median preparation time (ms) | Median update slots | P95 update slots |
|---|---:|---:|---:|---:|
| N5 | 20 | 14.34 | 15 | 24 |
| N6 | 20 | 13.54 | 15 | 25 |
| N8 | 20 | 13.41 | 15 | 19 |
| G16 | 20 | 15.08 | 16 | 19 |

At one slot per rendered 60 Hz frame, 15–16 slots correspond approximately to **0.25–0.27 seconds**. That is a conversion model, **not measured PlayMode latency**. The harness advances a synthetic frame counter and yields Editor updates without game rendering, View playback, or the full Plan/Job scheduling path.

`CombatPlan` also waits for unpresented results before scheduling automatic preparation. Actual user-visible gaps therefore require a later in-game trace separating previous playback completion, preparation, execution, and next presentation start. Reducing CPU work should reduce the number of required slices; increasing the per-frame budget alone trades waiting for greater frame pressure.

### 3. Repeated work exists at useful local boundaries

- `EnemyEngagementField.BuildSteps` loops over targets and candidate positions. `EnemyAttackReach.FromSteps` then evaluates possible aims and resolves delivery. Position-dependent actor/input projections are already shared through `Relocations`, but common damage is recomputed for each target-position visit, and `SelectionSteps` recreates targeting policy/range work for each attempted aim.
- `DeliveryCheckSteps` performs `DeliverySteps` before checking a particular target ID. Within one unchanged actor-position-input/aim, the delivery computation is target-independent. This is a candidate reuse boundary; the actual duplicate-key rate has not yet been measured.
- W8 reaches the engagement size limit and invokes `EnemyPursuitSearch`. That search evaluates attack reach at visited cells. `FromSteps` currently constructs the relocated actor before enumerating whether any aim could reach the target. A shared, exact necessary-condition rejection could avoid expensive preparation for distant impossible cells while continuing the same path traversal.
- U8 still creates and assesses movement alternatives before ultimately ending the enemy phase. Several candidates repeat visible-world/coverage preparation. Existing context visibility has different `preservePublic` semantics from assessment visibility, so those results must not simply be interchanged.

## Prioritized improvement candidates — not implemented

| Priority | Minimal direction | Expected change/understanding scope | Required verification and limits |
|---|---|---|---|
| 1 | Reuse exact attack-query prerequisites/results within one actor's unchanged decision world. Start with per-position common damage and targeting preparation; consider per-position/aim delivery reuse only if duplicate-key counts justify it. | `EnemyAttackReach`, its existing `Relocations`, and the shared attack-query entry points. Understand targeting/delivery dependencies, not View/Job internals. | Same aim/path/order/scores and final result; piercing, splash, pull, targeting restrictions, modifiers and terrain. Do not cache across changed worlds or reproduce combat rules in AI. No speedup percentage established. |
| 2 | Reject provably impossible attack positions before constructing relocated facts/input in fallback pursuit; reuse existing observation geometry where equivalent. | `EnemyPursuitSearch`, `EnemyAttackReach`, existing aim/range geometry contract. | Preserve visited-path behavior and tie breaks. Reject only the attack query, not traversal through the cell. Keep piercing/All/pull exceptions correct. Existing reach/pursuit equivalence checks remain necessary. |
| 3 | Reuse visible assessment inputs and invariant coverage work within an identical input/visibility scope. | `CombatActionAssessment` and decision context; greater shared-contract exposure than priorities 1–2. | Respect `preservePublic`, hidden information, reactions, changed positions, moving terrain and forced movement. Public-preview and movement-projection regression coverage is required. Do not skip assessments merely because no target is reachable. |

Keep candidate limits, precision limits, score weights, safety filtering and tie breaks unchanged during these optimizations. Do not assume the seven tuning controls or a BT rewrite address these costs. A proposed cache must store derived facts only, have explicit lifetime/dependencies, and preserve Framework/Buffer ownership. Reuse existing calculation contracts rather than adding a second combat evaluator.

## CPU and future-trait variability

Measurement is necessary but insufficient. Combine it with structural review:

1. Compare identical inputs on the same machine before/after, including selected intents, full traces, final state and work counts. Timing alone cannot establish equivalence.
2. Track geometry/target counts, query calls, preview calls, fallback hits and reused keys. Eliminate duplicated work even when absolute milliseconds vary.
3. Keep separate workloads for normal, disconnected, piercing, area, forced movement, reactive/status-heavy and environmental cases. This run covers only the existing corpus; it does not establish future-trait or maximum-content limits.
4. Re-run on a supported low-spec machine and in Player/PlayMode when optimizing for a user-visible latency target. CPU/cache/runtime/GC differences can change both absolute times and relative bottlenecks.
5. Make new trait dependencies explicit for any reused data. A fixed 1 ms slice is not a bound on total decision latency; preview/cell limits also do not bound every enumeration or effect chain.

User play is not needed for the initial Core investigation performed here. One representative in-game capture will still be needed to validate the reported pause and the final optimization against real frame scheduling. No observed in-game improvement is claimed.
