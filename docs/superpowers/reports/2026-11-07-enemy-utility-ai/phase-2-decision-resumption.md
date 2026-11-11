# Phase 2 enemy decision resumption and verification

## Scope and disposition

Phase 2 Core implementation covers the prepared intent contract, cancellable decision and pursuit cursors, and the existing attack-first policy. G-atom remains open after direct Unity measurement. The later explicit Phase 3 resumption and its implementation/Core verification are recorded in [the Phase 3 report](phase-3-preparation.md). Its user Unity acceptance remains Pending. The earlier pause references below are historical; the Phase 2 performance criterion still gates gameplay activation.

The session began and ended on `feature/Cheon/Core/NewCombatSytem` at `9d8c9ce456db95e8e0aff766240af47353f18f46`. The starting worktree contained uncommitted Phase 0–2 work. The final evidence runs record SHA-256 fingerprints of the complete changed C# source set. The user authorized continuing Phase 2 and delegated ordinary implementation decisions. Tasks 2.1–2.3 proceeded alongside the open Task 2.0d investigation because their correctness oracles were already frozen. The exit criterion and budgets stayed in force.

| Task | Current state | Evidence |
|---|---|---|
| 2.0a | Frozen reference retained | 86 Preview vectors, 4 movement vectors, 500 independent side trace rows, 55 input hashes |
| 2.0b and 2.0c | Shared resumable implementation and semantic checks | Prior [movement and geometry checkpoint](phase-2-movement-geometry-resumption.md) and final Preview corpus below. Aggregate owner leaves remain in the atom review |
| 2.0d | **G-atom unmet** | Stopwatch, GC window and thread-cycle data below. Exact managed allocation unavailable |
| 2.1 | Implemented and focused checks passed | Immutable intent and settings, lifecycle, supplied attack/move/end, actual rejected move, pending roar precedence |
| 2.2 | Implemented and focused checks passed | Reference route and attack-position equivalence, partitions 1/2/31, known mine and Leap paths, per-item route validation and aim generation |
| 2.3 | Implemented and focused checks passed | Frozen traces, partition and logical Preview count parity, cancellation, hidden attack rejection, movement rejection cursor, continued synchronous Advance |
| 3 | NotRun | Runtime scheduling and full source invalidation are Phase 3 work |

## Core ownership and review

- `EnemyDecision.cs` and `EnemyDecisionSettings.cs` define the immutable payload and defaults 4/4/2/1/8/16/256. Complete route and taunt source participate in equality. The Phase 2 legacy scan uses its original full candidate set.
- `EnemyDecisionSession.cs`, `EnemyDecisionContext.cs`, `EnemyAiPolicy.cs`, `EnemyDecisionDiagnostics.cs` and `TauntTargeting.cs` retain indexes and cursors within one decision. One attack Preview is active at a time. Creation charges one logical Preview. Score extraction visits events and target IDs separately. A rejected attack resumes pursuit for that actor. A rejected move resumes at the next actor. Diagnostics retain the last exact rejected intent and capture its reason only in opt-in development evidence.
- `CombatCommandCalculator.cs` and `EnemyAiTurnService.cs` route prepared and synchronous callers through the same ordinary command settlement. Pending required roars retain precedence. Attack, movement and phase end keep existing ordered settlement and publication ownership.
- `EnemyPursuitSearch.cs` and the narrow synchronous `EnemyMovementPathfinder.cs` adapter share one search. `CombatAttackQueries.cs` and `AttackSelectionResolver.cs` share aim legality and delivery. `MovementRouteValidator.cs` supplies a pursuit-local occupancy index over the immutable visible inputs and validates the final route one cell at a time. The neighbor order, first goal layer and cost/distance/Y/X tie break are preserved.
- Changes are confined to Core, EditMode evidence and Phase 2 documentation. The final read-only review checked selection, rejection and reactions. Its findings led to intrinsic route validation, exact rejection evidence, reuse of the first pursuit projection and separation of world projection from common damage. The move cursor test simulates rejection, while a separate adapter test verifies an actual rejected supplied move without a fallback or events.

Changing a ranking tie now requires the policy and trace tests. The author needs the public facts, Preview transition and selection order contracts. Changing route traversal requires the shared movement owner, reference equivalence and mine/Leap regressions. Changing settlement or reaction timing still requires Core and CoreIntegration because both prepared and synchronous actions use the same rule owner and publication boundary.

## Decision Step measurements

Approved Windows Unity 6000.0.73f1 EditMode Mono with the null graphics device. The decision probe used all 11 fixed fixtures at seed 1, 3 warmups and 12 repeated samples. Raw records include construction, each Step including final seal, disposal, Stopwatch elapsed time, GC collection deltas and the Windows execution-thread CPU cycle delta. Timing and cycles have different probe boundaries. Neither attributes the full duration of a collector pause or a specific nested call. The first row for each fixture comes from the focused test process. Fresh-process cold latency is unmeasured.

| Warmed operation | Samples | Median | p95 | p99 | Maximum |
|---|---:|---:|---:|---:|---:|
| Decision construction | 132 | 0.0014 ms | 0.0028 ms | 0.0030 ms | 0.0040 ms |
| Decision Step | 560,232 | 0.0004 ms | 0.0211 ms | 0.0275 ms | 14.8775 ms |
| Decision disposal | 132 | 0.0002 ms | 0.0005 ms | 0.0015 ms | 0.0129 ms |

11 Steps exceeded 0.5 ms, of which 9 exceeded 1 ms. All 9 have 11 matched same-fixture/operation/version/phase peer repetitions in `phase-2-decision-verified-steps/decision-step-summary.json`. The highest peer p95 is 0.1423 ms. The first observations contained 46,708 operation rows, with maximum 0.4435 ms in this test process.

| Warmed sample | Elapsed | GC in window | Attribution status |
|---|---:|---|---|
| U8 Step 16792 | 14.8775 ms | Yes | Pursuit attack reach |
| U8 Step 6940 | 14.5083 ms | Yes | Pursuit attack reach |
| W8 Step 2706 | 14.4372 ms | Yes | Pursuit attack reach |
| T8 Step 1714 | 14.0808 ms | Yes | Preview completion entry |
| W8 Step 3866 | 3.1570 ms | No | Pursuit attack reach |
| U8 Step 5127 | 3.1543 ms | No | Pursuit attack reach |
| U8 Step 5055 | 1.3844 ms | No | Pursuit attack reach |
| U8 Step 14627 | 1.1616 ms | No | Pursuit attack reach |
| T8 Step 1662 | 1.1042 ms | No | Preview observation |

The five samples without a collection all recorded more thread cycles than their same-work peers. Thread timing also spans the probe calls. This evidence leaves their specific cause open and rules out classifying them as known collector pauses. The remaining pursuit label includes aim generation, temporary immutable world projection, profile and geometry/delivery child work. No repeatable slow leaf was proved by that label alone.

The pre-index run recorded 13 warmed Steps above 1 ms, 8 at the pursuit neighbor boundary. The current run recorded zero above 1 ms at that boundary. In the two runs the median sum of measured operations was 49.0661 to 26.3038 ms for W8 and 98.4280 to 50.4758 ms for U8. Expanded nodes remained 960 and 1,744 respectively. W8 and U8 use 32 by 32 stress boards. The N5 fixture uses five enemy units and four player units. The probe reuses its measurement buffer in the current run, so both the code and harness changed and these elapsed comparisons describe the observed runs.

## Phase 2 acceptance and next checkpoint

- **G-correctness and G-isolation for Core.** Use the frozen comparisons and exact XML differential below. The independent Phase 1 trace records action, actor, aim, route, taunt and ordered events. Phase 2 focused tests also check private cancellation, partition identity and identical logical Preview counts.
- **G-atom unmet.** In addition to the measured decision and Preview tails, individual Steps retain aggregate operations in world snapshot copies, wall clipping/collection sealing and parts of observation/settlement. The timing limits have not been verified for these owners. Start a follow-up at exact pursuit attack-reach children and the measured Preview owner boundaries. Separate child setup, immutable input copies and wall part work before deciding which leaves need more slicing or reviewed input limits.
- **G-allocation unavailable.** The 4,096-byte validation allocation returned zero through the Mono thread allocation counter. Missing bytes are unavailable measurements. The known GC-window samples remain part of the timing distribution.
- **Rendered frame/UF checks NotRun.** The batch path produces no rendered frames. The previous Phase 0 live evidence remains historical. Frame scheduling and live validity are separate acceptance evidence.

## Final regression and measurement evidence

The final focused decision run passed 168 of 168 tests, including the step capture. The intermediate full Core run before the occupancy and route-contract refinement selected 5,113, passed 5,083, failed 25 and skipped 5. Earlier compiler-only runs produced no NUnit result. One uncovered the lost optional diagnostics parameter on the internal turn wrapper. Later compiler-only attempts caught a Contracts-to-Domain access mistake in the route shape helper. The final source retains the private payload shape check and the existing domain admission boundary.


| Unity run | Selected | Passed | Failed | Skipped | Disposition |
|---|---:|---:|---:|---:|---|
| Core | 5,114 | 5,084 | 25 | 5 | All 25 failures match Phase 0 |
| CoreIntegration | 957 | 909 | 47 | 1 | All 47 failures match Phase 0 |
| Focused decision, pursuit and atom capture | 168 | 168 | 0 | 0 | Passed |
| Full legacy decision capture | 1 | 1 | 0 | 0 | Passed |

The differential compares exact NUnit full names, failure labels and messages. The six skips are dedicated explicit performance captures. The raw comparison is `Logs/EnemyAi/phase-2-work/decision-regression-comparison.json`.

The full legacy capture reproduced **500 of 500** frozen side rows and **55 of 55** complete input manifest entries. The normalized trace SHA-256 is `0c4b2a269d9e1813fd5bc511911ae51dab1095fe3d9c9b66e5a8874079675791`. Only BOM, line endings and trailing file newline are normalized. The stored frozen source files retain these SHA-256 hashes.

| Frozen file | SHA-256 |
|---|---|
| `phase-2-preview-reference.json` | `021FBD5C922D7307D5194E2FD548F040B12E1A77A394D902450311EC038C1A49` |
| `phase-2-movement-reference.json` | `B8C37AB809A42EF40F49A13DB97A179EF89C611F3EFCA03EBC839DB934050752` |
| `baseline-traces.tsv` | `F71DF6D6DAED5F5EC98FF6CE7960F9A2F23A1DB0995D184C7B50F8516F097006` |

## Whole-decision and full-side elapsed time

These fixed-input decisions use the same fixture and seed corpus as the previous checkpoint with 10 warmups and 20 repeats. The decision timer includes synchronous Advance and its diagnostic marker recorder. Marker distributions can change as preparation is split. Full-side Advance sums were recorded with the recorder removed and exclude world projection performed by the test harness. The prior comparison is the Phase 2 movement and geometry checkpoint.

| Fixture | Warm decisions | Prior decision p95 | Current mean | Current p95 | Current p99 | Current maximum |
|---|---:|---:|---:|---:|---:|---:|
| N5 | 100 | 5.1536 ms | 3.3099 ms | 4.5289 ms | 5.2712 ms | 5.5640 ms |
| W8 | 100 | 89.2070 ms | 30.1773 ms | 53.0656 ms | 59.7950 ms | 61.6797 ms |
| U8 | 100 | 132.6426 ms | 53.3123 ms | 80.9388 ms | 84.1283 ms | 85.9166 ms |
| Original ten | 1,000 | 94.0456 ms | 11.8134 ms | 48.7179 ms | 74.7886 ms | 85.9166 ms |
| All | 1,100 | 92.5649 ms | 11.0404 ms | 48.5663 ms | 73.3786 ms | 85.9166 ms |

| Full-side Advance sum with profiler removed | Sides | Prior p95 | Current p95 | Current maximum |
|---|---:|---:|---:|---:|
| N5 | 20 | 43.1238 ms | 34.1096 ms | 43.1539 ms |
| W8 | 20 | 509.4962 ms | 286.1635 ms | 485.1756 ms |
| U8 | 20 | 126.0779 ms | 76.5151 ms | 79.5135 ms |
| All | 220 | 448.0640 ms | 254.6937 ms | 485.1756 ms |

Per-fixture mean, p95, p99, maximum and same-input comparisons are retained in `Logs/EnemyAi/phase-2-decision-verified-legacy/decision-corpus-summary.json`. These are batch latency observations. The live scheduling and allocation gates remain separate.


## Final Preview corpus and atom follow-through

The combined Preview run passed 37 of 37 tests. The timed corpus rechecked all 90 frozen vectors across partitions 1/2/31, with 10 warmups and 20 warmed repeats per vector and partition. Its 90 per-vector semantic hashes match the earlier checkpoint. The final SHA-256 manifests cover all 57 changed C# files and agree across the five final runs.

| Warmed Preview measurement | Samples | Median | p95 | p99 | Maximum |
|---|---:|---:|---:|---:|---:|
| Step | 1,992,060 | 0.0005 ms | 0.0128 ms | 0.0354 ms | 11.6205 ms |
| Constructor | 5,400 | 0.0022 ms | 0.0038 ms | 0.0064 ms | 3.0863 ms |
| Result read | 5,400 | 0.0001 ms | 0.0002 ms | 0.0003 ms | 0.0006 ms |
| Dispose | 5,400 | 0.0001 ms | 0.0001 ms | 0.0003 ms | 0.0004 ms |
| Full Preview with Step timing | 5,400 | 1.0366 ms | 2.4772 ms | 4.0135 ms | 17.1748 ms |
| Full synchronous Preview without Step timing | 1,800 | 0.6715 ms | 2.0486 ms | 4.1484 ms | 16.4910 ms |

Warmed calls took 11–886 Steps. Twenty-six Steps exceeded 1 ms, with a collection recorded in 9 and no collection recorded in 17. Two constructors also exceeded 1 ms and the constructor probe had no per-operation collection field. The largest Step without a collection was 9.20 ms at N8-19 `WallGeometryComplete`. The highest p95 among all matched same-work overrun groups was 0.1322 ms. One first-observation Step exceeded 1 ms at `knockback-tornado/Rules` and recorded no collection. Fresh-process cold-start bounds are unmeasured. The exact owner, fixture, phase, version, time, available collection data and matching repetitions are in `Logs/EnemyAi/phase-2-decision-verified-preview/summary.json`.

`PursuitAttackReach` should be narrowed at the immutable world copy, profile/damage setup and delivery boundary. `WallGeometryComplete` is the builder's final geometry seal. The decision `ScopeObservation` label maps to one `AttackDeliveryExecutor` observation child Step, and `CompletionBegin` maps to `MovementProjection` completion entry. These locations identify where to instrument next. Attribution within those owners remains open.

## Raw evidence and workspace state

- `Logs/EnemyAi/phase-2-decision-verified-{steps,core,integration,legacy,preview}/` contains Unity logs, NUnit XML, exit codes and source manifests. Unity exited 0 for focused, legacy and Preview runs. It exited 2 for the full assemblies with the exact existing failures.
- Decision Step rows are in `decision-steps.csv.gz` with `decision-step-summary.json` and individual tail rows. Whole-decision and full-side records are in the legacy capture. Full Preview records retain the uninstrumented totals separately from Step timings.
- Summary scripts are in `Logs/EnemyAi/phase-2-work/` as `summarize-decision-steps.cjs`, `summarize-decision-corpus.cjs` and `summarize-preview-final.cjs`. Test code and the frozen references are the reproducible repository inputs.
- Unity regenerated three FMOD/font assets. Their generated copies were backed up under `generated-assets-decision-verified` and all three paths returned to their clean entry state. The checkout retains uncommitted source and evidence documents. Final `git diff --check` passed. No Unity process remained.


## G-atom follow-up

The user continued the Phase 2 atom investigation. The change stays within Core and EditMode verification. The Phase 2 time criteria and original decision oracles still govern acceptance.

The wall clipping probe first failed with zero resumable parts against three bent-wall parts. After the change, the focused run passed 326 of 326 cases, including connected-wall ordering, piercing, the new clipping probe, and pursuit and decision parity. Selection and piercing now advance one convex wall part per Step. The builder transfers its completed component list and cell index to the immutable wall geometry, avoiding a second board-wide collection rebuild. The closest exact segment entry and component-ID tie break are preserved. The independent read-only review confirmed the ownership transfer and ordering.

The prior N8-19 9.20 ms wall seal observation occurred on a fixture with zero walls. Its elapsed time cannot be explained by wall population traversal. The clipping change removes a separate source-proven whole-component operation. The largest new Preview Step without an observed collection was 8.9998 ms at X8-1 `Profile`; the narrower cause remains open.

| Warmed measurement | Previous checkpoint | Wall refinement |
|---|---:|---:|
| Preview Steps sampled | 1,992,060 | 1,992,060 |
| Preview Step p95 | 0.0128 ms | 0.0132 ms |
| Preview Steps above 1 ms | 26 | 17 |
| Preview maximum Step | 11.6205 ms | 12.8743 ms |
| Preview constructors above 1 ms | 2 | 3 |
| Full synchronous Preview p95 | 2.0486 ms | 2.0453 ms |

The final wall-code Preview capture rechecked all 90 frozen results. Seven of the 17 warmed Step overruns had a recorded collection. Ten Steps and the three construction overruns had zero recorded collections. The harness now records collection counts for setup, read and release as well as Steps. The frozen Preview scenarios happened to execute the same total number of Steps as before. The focused bent-wall test separately verifies per-part suspension for an exercised clipping path.

After pursuit child labels were propagated, the decision recapture had 11 warmed Steps above 1 ms over 592,728 Steps. Eight were in `PursuitActorPlacement`. Five of those eight had zero recorded collections. `PursuitWorldCopy` had no warmed >1 ms sample in that run. The placement change that followed reuses the moved facts when Disarm is absent and checks for any active visible destination modifier once per search. The same status and destination calculations still apply when needed.


### Final placement retest

The placement change preserves the original path for a present Disarm and for a visible active position modifier. Independent review confirmed that the existing resolver still detects overlapping visible modifiers at a visited cell and validates the actor's position attack contribution.

| Warmed placement work | Samples per run | p95 before | p95 after | Above 1 ms before | Above 1 ms after |
|---|---:|---:|---:|---:|---:|
| W8 | 11,520 | 0.0309 ms | 0.0150 ms | 4 | 2 |
| U8 | 20,928 | 0.0397 ms | 0.0204 ms | 4 | 1 |

The fixtures still expanded 960 and 1,744 nodes per decision. Their median sums of timed operations fell from 26.8066 to 17.4548 ms and from 53.1646 to 37.8849 ms. The final warmed decision probe had 596,160 Steps, p95 0.0082 ms and p99 0.0170 ms. Thirteen Steps exceeded 1 ms, including five with a recorded collection. The largest Step was 26.9928 ms at N8 `DecisionAttackCandidate` with a collection. The largest without a collection was 5.67 ms at N8 `SelectionTargetsComplete`. The placement label had three >1 ms rows, two with a collection. Every >1 ms row has matching same-work repetitions in `phase-2-atom-final-decision/decision-step-summary.json`.

The first N5 `DamagePresence` Step at version 1262 took 28.1046 ms in the isolated recapture and 27.9678 ms in the final isolated run, both with zero recorded collections. This repeatable first-observation boundary warrants separate runtime attribution. It is included in the raw first-observation series.

**G-atom remains unmet.** Warmed wall/Preview and final decision captures still include >1 ms work with incomplete attribution. The remaining reviewed aggregate leaves include per-component `WallRun` and `AttackBlockedContact` evidence copies, occupant scans within piercing cells and the pursuit world copy. The latter had zero warmed >1 ms rows in the intermediate recapture. The next diagnostic targets are `SelectionPolicy`, `SelectionTargetsComplete`, `CompletionFinish` and the recurring first `DamagePresence` boundary. Mono still returned zero for the 4 KiB allocation probe. Phase 3 status is NotRun.

Final Core selected/passed/failed/skipped was 5,115/5,085/25/5. CoreIntegration was 957/909/47/1. The full names, failure labels and messages of all 72 failures match Phase 0. Raw differences are in `phase-2-work/atom-regression-comparison.json`. The focused wall and route run preceded the final placement change. The full regression runs and final decision captures include it. The Preview recapture predates the isolated `EnemyPursuitSearch.cs` change and exercises the same final Preview and wall calculation sources.


The final 1,100-decision corpus matched all 500 frozen action rows and all 55 input manifests. Decision p95 was 4.0328 ms for N5, 45.8949 ms for W8, 72.8978 ms for U8 and 33.0861 ms overall. The overall maximum was 168.6270 ms. The previous Phase 2 checkpoint had overall p95 48.5663 ms and maximum 85.9166 ms.

The two full regression manifests and the final decision/legacy capture manifests match all 59 current changed C# files. The Preview capture manifest differs only in `EnemyPursuitSearch.cs`, which is outside that benchmark's Preview and attack execution call graph. Raw runs are under `Logs/EnemyAi/phase-2-atom-*/`. The three regenerated Unity assets matched the previous generated bytes and were backed up and restored. The checkout passed `git diff --check` and the Unity processes exited.


### Phase 3 entry and gameplay activation

The previous plan treated G-atom as a Phase 2 exit gate. The measured wall and pursuit improvements preserve the functional oracles, but the gate remains unmet. One example is N8 `SelectionTargetsComplete` at 5.6706 ms with no recorded collection. Its same-work peer p95 was 0.0012 ms. The source at this yield publishes an already constructed target array to its private caller. The sample identifies a timing window. Its internal cause remains open. The repeatable first N5 `DamagePresence` observation is separate. Phase 0 recorded native `Mono.JIT` in a historical first-use attack Preview. The current timing harness has no matching JIT marker for this sample.

The user approved this entry change on 2026-11-07. Phase 3 implementation and integration testing are eligible with G-atom recorded as unmet. The user then requested a documentation-only stop. Implementation and tests are NotRun. On an explicit start request, use the following sequence. Start with Task 3.1 source capture and staleness tests. In Tasks 3.2 and 3.3 make preparation selectable in the test or diagnostic composition while the existing automatic Advance remains the default runtime route. Reuse the existing service registration, Buffer publisher and one-shot Job contracts. Require the existing G-atom disposition and Phase 3 frame and Flow evidence before selecting the prepared route in normal gameplay. The numerical performance criteria remain in force. This documentation change introduces zero product or test source changes.

