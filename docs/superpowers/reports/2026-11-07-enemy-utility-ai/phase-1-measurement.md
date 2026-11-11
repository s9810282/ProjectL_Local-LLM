# Enemy Utility AI Phase 1 measurement and handoff

**Review stop.** The deterministic Core corpus, legacy action traces and CPU measurements are available. G-atom failed on the current calculator. Exact standardized allocation and future scheduling gates remain open. Phase 2 requires a separate decision.

## Scope and source identity

- Branch `feature/Cheon/Core/NewCombatSytem`, base HEAD `9d8c9ce456db95e8e0aff766240af47353f18f46`. The working tree began with zero tracked changes and the existing untracked plan, spec and Phase 0 report.
- The normal reference is N5, with five enemy field units. N6 and N8 vary only enemy count within the same open Core setup. All original ten workload IDs remain in the corpus as comparison or stress cases.
- The fixtures own their coordinates, installed stats, profiles, terrain and hazard definitions. [Fixture manifest](fixture-manifest.json) records the 55 canonical input hashes and the measured source-file SHA-256 values. W8 and U8 use the same actors and board, with the wall's three-cell opening closed in U8. U8 exhausts the mandatory search and ends the side. G16 starts from an already populated 16-enemy snapshot. Live growth to 16 is unverified. The current Nexus reinforcement rule caps its field population at five.
- CoreIntegration uses a separate test-owned database and normal battle entry, command processing and Advance publication for N5, N6 and N8. The tests compare board dimensions, seed, deployed coordinates, enemy HP and enemy attack range. The assembly installs its own player definitions, identities, headquarters and persistent data. Use its service timings as a separate series. CoreIntegration parity for W8 through G16 and additional hidden-information variants remain open.
- The existing synchronous calculator exposes optional counters through an internal overload. The public Advance call uses no diagnostics object. Legacy WorkUnits count expanded frontier labels and preview attempts. CapHits has no active cap in this phase. The optional four-entry buffer holds the first inspected candidates as typed records. The Unity marker adapter already enumerates CoreProfileSection and registers each appended value, preserving existing names and ordinals.
- The approved AppData-capable Editor ran Unity `6000.0.73f1 (a166abc3bf0e)`, Windows 11 x64, EditMode Mono, `-batchmode -nographics`. Host hardware and the rendered DX11 reference are in [Phase 0](phase-0-baseline.md). The batch target uses the null graphics device. Resolution, VSync and target frame rate are inapplicable. Unity Deep Profile was off. An in-process sampler captured existing nested Core scopes with preallocated storage. Stopwatch recorded monotonic elapsed time around synchronous work, including GC or scheduling delays. A marker's inclusive time can contain other marker rows.

## Core measurement

Each of the 55 fixture and seed pairs contributed one first-use observation, ten warmups and twenty measured calls from the same immutable input. The five fixed seeds are 1, 7, 19, 41 and 97. Setup and output occurred outside the decision timer. The original matrix contributes 1,000 warmed decisions and N5 adds 100. Side runs reset to the same source and project each accepted transition into the next calculation. There are twenty full sides per fixture. Their totals below sum Advance calls and exclude the separately timed projection used to drive the harness.

All times are ms. Percentiles use nearest rank over individual samples. The batch results describe the current synchronous calculator.

| Fixture | Decision n | Mean | p95 | p99 | Max | Full side p95 | Full side max |
|---|---:|---:|---:|---:|---:|---:|---:|
| N5 | 100 | 14.267 | 26.498 | 45.594 | 49.166 | 76.339 | 215.209 |
| N6 | 100 | 12.992 | 18.145 | 44.444 | 46.429 | 143.698 | 255.384 |
| N8 | 100 | 11.829 | 15.380 | 39.280 | 42.234 | 134.614 | 138.638 |
| W8 | 100 | 68.685 | 106.860 | 198.412 | 275.871 | 623.530 | 953.420 |
| U8 | 100 | 132.870 | 166.920 | 396.372 | 503.884 | 161.651 | 338.924 |
| A8 | 100 | 9.410 | 15.019 | 38.863 | 48.042 | 77.358 | 77.859 |
| T8 | 100 | 41.422 | 71.722 | 141.829 | 144.962 | 355.831 | 367.948 |
| R8 | 100 | 11.927 | 17.902 | 44.907 | 45.195 | 140.744 | 153.639 |
| X8 | 100 | 12.570 | 18.665 | 34.161 | 41.041 | 184.486 | 458.171 |
| H8 | 100 | 3.858 | 5.469 | 9.266 | 10.303 | 153.268 | 154.161 |
| G16 | 100 | 12.303 | 16.707 | 40.626 | 46.570 | 248.314 | 652.124 |
| Original ten | 1,000 | 31.787 | 117.325 | 162.539 | 503.884 | — | — |
| All | 1,100 | 30.194 | 115.989 | 161.313 | 503.884 | 575.534 | 953.420 |

N5's first decision in the dedicated process took **364.657 ms** with a single preview invocation of **208.778 ms**. Its later per-fixture first rows include an already running process and are kept separate from warmed measurements. This sampler cannot attribute that first spike to JIT or GC. The rendered Phase 0 capture independently identified JIT under one first-use preview.

### Warmed scope maxima

| Scope | Calls | Mean | p95 | p99 | Max | Calls >1 ms | Worst fixture / seed / decision |
|---|---:|---:|---:|---:|---:|---:|---|
| Full attack range | 1,800 | 3.907 | 8.764 | 16.093 | 43.461 | 1,800 | N5 / 7 / 15 |
| Full aim preview | 1,400 | 2.809 | 12.684 | 18.146 | 55.000 | 1,400 | T8 / 41 / 2 |
| Aim selection with preview children | 1,800 | 2.533 | 13.104 | 18.922 | 60.142 | 820 | T8 / 41 / 2 |
| Position attack check | 270,700 | 0.033 | 0.053 | 0.183 | 18.469 | 45 | U8 / 1 / 8 |
| Entire pursuit search | 1,000 | 18.027 | 57.955 | 85.426 | 258.525 | 820 | W8 / 7 / 13 |
| Pursuit target selection | 1,000 | 0.019 | 0.029 | 0.059 | 0.198 | 0 | U8 / 19 / 16 |
| Selected attack or move calculation | 1,000 | 3.280 | 13.887 | 19.161 | 69.635 | 929 | T8 / 41 / 3 |
| Movement undo reconciliation | 2,100 | 0.115 | 0.318 | 0.550 | 16.862 | 8 | A8 / 97 / 15 |

N5 alone peaked at **26.034 ms per warmed preview**. U8's single decision can traverse 1,744 labels and end the side after scanning eight actors. W8 expanded 960 labels for its first decision. Inclusive pursuit, position-check and aim-selection rows overlap. The selected-action scope covers the accepted or rejected attack/move calculation. Merge, controller-end settlement, post-action reconciliation and Buffer publishing have separate or outer boundaries.

The overhead probe interleaved twenty N5 calls per mode with the timing sampler off. Mean/p95/max were 12.584/16.158/16.171 ms for no diagnostics, 12.863/17.753/18.138 ms for counters, and 13.582/17.931/43.594 ms with alternatives. Object creation was outside the timing. These noisy timings cannot establish a precise marginal cost. The disabled explanation path uses a null storage reference and constructs no per-candidate strings.

### Allocation and service evidence

`GC.GetAllocatedBytesForCurrentThread` returned 0 bytes in this Editor after a forced live 4 KiB allocation. The first run rejected the probe. The CPU rerun recorded empty allocation cells for all 1,155 decisions and 295,995 scope invocations. Empty means unavailable. The rendered Phase 0 Profiler is the correlated fallback evidence for the current path. In stage 1124, frame 97 contained one 516.13 KiB preview. Frame 995 grouped nine previews at 3.15 MiB and four full range calls at 1.38 MiB. The five-enemy side totaled about 29.05 MiB in Advance. The fixture-specific per-call and preparation/execution byte splits remain unmeasured. [Phase 0 Profiler evidence](phase-0-baseline.md#bottleneck-attribution)

The dedicated CoreIntegration run measured 60 full sides and 360 published Advances. Each side resets the battle, enters the enemy controller through the normal service and verifies the result sequence increments once per call. The timer includes assembly, calculation and publication. These samples include first use of the service path. Preparation does not exist yet. Per-assembly, per-publish and byte samples are unavailable for this corpus.

| Service fixture | Advance n | Mean | p95 | p99 | Max | Side p95 | Side max |
|---|---:|---:|---:|---:|---:|---:|---:|
| N5 | 120 | 23.025 | 52.801 | 59.826 | 278.187 | 217.764 | 446.591 |
| N6 | 120 | 18.055 | 31.626 | 60.568 | 60.903 | 145.451 | 148.277 |
| N8 | 120 | 23.263 | 56.469 | 90.541 | 142.302 | 279.844 | 357.525 |
| All | 360 | 21.448 | 49.683 | 87.522 | 278.187 | 217.764 | 446.591 |

## Legacy action traces and checks

The frozen [baseline traces](baseline-traces.tsv) contain 500 accepted transitions, one full sequence for each fixture and seed. The file keys each row to its initial input hash and includes actor, selected aim, requested path, taunt source, before/after action use and event kind order. Representative seed-1 rows were inspected.

- N5 and N6 first select actor 11 and attack cell (2,0), with the attacker changing from Available to Attacked.
- W8 first moves actor 11 along (2,2) → (1,2) → (1,3) → (1,4). U8 emits one EnemyPhaseEnded after finding no route.
- T8 first selects taunt source 4 and attacks (4,0). EffectsActivated precedes Attacked. R8 ends with EnemyPhaseEnded → GimmickActivated → three Died events → RockfallAdvanced.
- X8 emits tornado settlement and relocation ahead of railway and rockfall settlement. H8 ends with HaboobAdvanced. The fixed baseline holds visibility constant across seeds.

| Verification | Result |
|---|---|
| Initial fixture-only run | 22 passed, 0 failed. The first fixture checks ran before instrumentation. |
| Final RC, all Core EditMode | 5,017 passed, 25 failed, 1 expected opt-in skip, total 5,043. The 34 fixture and diagnostic checks passed. |
| Final RI, all CoreIntegration EditMode | 909 passed, 47 failed, 1 expected opt-in skip, total 957. The four service baseline checks passed. |
| Dedicated performance runs | Core `CaptureCorpus` 1/1, service `CaptureOpenEncounters` 1/1 after the metadata fix. These are capture-run results. Gate status is evaluated separately below. |
| Compared to Phase 0 | Zero new or lost failure identities in either assembly. Failure labels and messages matched all 72 baseline failures, including the existing Cinemachine TypeLoadException and night-boundary assertions. |
| RD | Source hashes matched the manifest. All new scripts have valid unique Unity metadata. Restored the three generated FMOD/font file changes to the initial tree after the Editor exited. No production asset diff. |
| UF | NotRun. No manual Flow, rendered playback or View compilation verification was performed for this phase. |

The service baseline cases exercise one accepted Advance with pending command preservation for N5/N6/N8 and no publication during player input or after Run termination. The Core tests compare selected-action evidence, projected actor state, RNG state and event order with diagnostics enabled.

The first compile attempts failed on a missing AttackAim import and an unavailable NUnit attribute. The first runtime measurement then failed the allocation probe. Unity also selected the opt-in measurement during an early assembly-wide run and overwrote that run's raw folder. The final dedicated capture uses an explicit environment gate and a separate folder. The first dedicated CoreIntegration performance attempt selected zero tests because its new Unity metadata GUID was malformed. Its XML is retained separately. A zero-test run provides no performance evidence.

## Gate disposition and next-phase handoff

| Gate | Phase 1 status |
|---|---|
| G-baseline | Current synchronous CPU baseline captured and source fingerprints verified. Fixture-specific byte measurements and direct replication of the historical incident remain open. |
| G-atom | Failed on current implementation. All 1,400 warmed full previews exceeded 1 ms. Full range, whole pursuit, individual position checks and selected-action execution also crossed 1 ms. |
| G-frame | NotRun. No prepared slices exist. Batch samples describe elapsed work in the synchronous entry points. |
| G-latency | The warmed synchronous N5 p95 is 26.498 ms and the worst fixture p95 is U8 at 166.920 ms. U8 p99 is 396.372 ms. Prepared decision latency is NotRun. |
| G-allocation | Phase 0 exceeded the provisional scale. Phase 1 standardized decision, atom and side allocation are unavailable. |
| G-correctness | Baseline-relative Core and CoreIntegration comparison recorded above. Full suites retain known failures. |
| G-isolation | Diagnostics parity verified at the calculator and one-result publication/pending-input behavior at the service. Slicing equivalence, source-stamp invalidation and hypothetical hazard projection isolation are later checks. |

Review the following ownership boundaries before treating any existing call as a bounded preparation unit.

| Owner and measured input | Minimum change to evaluate | Modification, understanding and verification scope |
|---|---|---|
| `CombatAttackQueries.Range` and `Selection`, N5/7/15 | Incrementally enumerate legal AI aims with the existing targeting rules and retain immutable attack geometry for the scan. The current range path computes both normal/basic masks and materializes display facts. | Core attack queries and the AI consumer. Preserve empty-cell, direction, splash, pierce, wall, elevation and haboob legality with aim-set parity. The existing public range result remains a required regression. |
| `EnemyAiPolicy.TrySelectAttackAim` and the preview projection in `CombatAttackCalculator`, T8/41/2 | Separate cheap legal-candidate work and effect-owned precise assessment. Reduce projection setup/copying with measured evidence before allowing a full preview to occupy a work unit. | AI consumer and shared attack/effect query owners. Verify taunt, collateral, reaction and ordered damage facts. Optional caps limit frequency and still leave a costly individual preview to resolve. |
| `EnemyMovementPathfinder.TryFind` and `AttackReach.From`, W8/7/13 and U8/1/8 | Retain queue, labels and the current distance-layer cutoff across frontier work. If an individual position check still overruns, retain the inner geometry/aim cursor or reduce its inputs with parity evidence. | AI pathfinder and existing route/delivery rules. Verify exact route tie-breaks, budget prefixes, reachability and finite mandatory scans against W8, U8, taunt and the existing equivalence suite. |
| `CombatCommandCalculator.Calculate`, T8/41/3, and `MovementUndoPolicy.Reconcile`, A8/97/15 | Measure selected execution and reconciliation separately from preparation, with a profiler capture for large outliers. | Actual action and post-action owners, plus the publisher for end-to-end evidence. Check action-use settlement, reaction order, terminal suppression, pending input and one accepted publication. |

`EnemyPursuitTarget` warmed maximum was 0.198 ms in this run. Keep its semantics in parity coverage and use the pursuit-search and position-check samples to prioritize the measured cost. Review the existing StageGimmick registry as the owner of ordered hazards when later adding forecast facts.

## Reproduction and raw evidence

- Primary raw folder `Logs/EnemyAi/phase-1-9d8c9ce45-final/`. It contains `manifest.json`, `inputs.tsv`, `decisions.csv`, `atoms.csv`, `sides.csv`, `overhead.csv`, `runtime.txt`, the traces, `service-decisions.csv`, `service-sides.csv`, Unity logs and XML. Phase 0 contains the native rendered capture and exporter. New prepared-slice records and a Phase 1 rendered capture are NotRun.
- Set `PROJECTL_ENEMY_AI_RAW` to a fresh absolute folder and `PROJECTL_ENEMY_AI_CAPTURE` to `core` or `service`. Use the approved Editor path with `-batchmode -nographics -runTests -testPlatform EditMode`. Core selects assembly `ProjectL.Tests.Core` and method `ProjectL.Tests.Core.EnemyAiPerformanceMeasurements.CaptureCorpus`. CoreIntegration selects assembly `ProjectL.Tests.CoreIntegration` and method `ProjectL.Tests.CoreIntegration.EnemyAiServicePerformanceMeasurements.CaptureOpenEncounters`. Keep performance capture disabled for assembly-wide verification.
- Use `inputs.tsv` SHA-256 and the fixture source hashes before comparing later numbers. Keep the dedicated CPU, rendered CPU/GC and service series separate. Preserve the original failure identities and do not promote an unavailable or zero-test check to a pass.
