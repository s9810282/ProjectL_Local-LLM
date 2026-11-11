# Phase 7 item 2 performance investigation

The [N8 allocation follow-up](phase-7-n8-allocation.md) records the later call-stack attribution and the drop from 8.86 MiB to 6.18–6.30 MiB in matched preparation captures. The measurements below are the earlier item 2 record.

2026-11-08, Unity 6000.0.73f1 EditMode on the same Windows 11 and Ryzen 7 6800H environment as the [post-scope baseline](phase-7-post-scope-timing.md). The checkout is `feature/Cheon/Core/NewCombatSytem` at `9d8c9ce456db95e8e0aff766240af47353f18f46` with the prior work in the working tree. The final C# manifest differs from the 118-file item 1 manifest only by `MovementPathNormalizer.cs` and `PlayerTurnMovementTests.cs`. Four temporary instrumentation files were restored to their verified entry hashes before final validation.

## N5 first attack

Work version 1644 displayed `Attack/SacrificePlan` after the Step. The previous displayed phase was `Attack/SacrificeSource`. The `AttackSacrificePlanning` owner marker did not run inside this Step. Direct damage ran, with `AttackCommonDamage` and `TraitPresenceReconcile` among the recorded scopes. The separate Unity CPU trace on the item 1 source captured 34.6675 ms at this Step. Its 183 direct `Mono.JIT` samples totalled 29.9697 ms without nested double counting. `TraitPresenceReconcile` accounted for 0.5902 ms inclusive. The uninstrumented final run still measured 30.2972 ms at that Step with no recorded collection. First-use JIT remains included in the gate and depends on the target runtime. No startup warmup was added.

## U8 route normalization

The repeated warmed Step at work version 17043 reaches `MovementPathNormalizer.TryNormalize`. The previous loop called `VisibleTo` for every valid draft segment and again for the branch search. U8 has 32 wall tiles. Each pass copies and validates the same observer-visible environment. Normalization now initializes that local snapshot on the first valid segment and shares it between step budgets and corner search. Visibility, failure order, candidate limits, route validation and movement settlement follow the existing contracts. The deterministic regression observed three visibility copies for two segments before the change and one afterwards. All 18 player movement cases passed in the focused instrumented run.

| U8 work version 17043, seed 1 | Warm samples | Step median | Step maximum | Normalizer median | Normalizer maximum |
|---|---:|---:|---:|---:|---:|
| Instrumented before | 29 | 0.3740 ms | 1.0389 ms | 0.2993 ms | 0.7782 ms |
| Instrumented after | 29 | 0.1696 ms | 0.3868 ms | 0.0948 ms | 0.2144 ms |
| Final uninstrumented | 12 | 0.1566 ms | 0.2965 ms | Unmeasured | Unmeasured |

The two uninstrumented item 1 captures measured the same boundary at up to 4.9721 and 4.9877 ms. Those larger tails did not reproduce under owner instrumentation. The instrumented before/after comparison identifies reduced work at the repeated boundary. It cannot assign the entire historical 4.99 ms to the normalizer.

## Final uninstrumented timing

- **G-atom unmet.** The same eleven fixtures, seed 1, one first observation, three warmups and twelve measured repeats produced 1,259,436 warmed Steps. Their p95/p99 were 0.0080/0.0213 ms, with 26 above 0.5 ms and 12 above 1 ms. Five of the 12 overlapped a collection. The 18.5855 ms maximum was W8 at version 9225, with a recorded collection. The largest warmed overrun without a recorded collection was N8 at version 2325, 3.1933 ms. The N5 first construction was 1.1965 ms, and its first attack reached 30.2972 ms. The final atom series matched all 143 item 1 decision kinds, work counts, Preview calls and expanded-node counts.
- **G-frame unmet in the yielded Editor service.** Across N5, N6, N8 and G16, five seeds and 80 warmed sides, the 3,405 preparation-update samples reached p95 1.0667 ms and p99 1.1798 ms. Six exceeded 4 ms and each overlapped a collection. The maximum was 17.3523 ms on G16 seed 19, repeat 1, decision 1, update 10, with a 16.8266 ms preparation atom. Eleven prepare-call atoms exceeded 1 ms. Collection overlap and the separate allocation capture do not establish the exact internal work for each timing tail. Action calculation and publication had p95 3.0604 ms and a 21.2024 ms maximum. All 600 service publications, including 480 measured, matched the synchronous Utility oracle and the item 1 actor/kind sequence.

## Native allocation diagnostic

The managed current-thread counter still returned zero for the 4 KiB probe. A separate Unity `GC.Alloc` trace reported zero bytes for an empty marker and 4,128 bytes in one allocation for a 4,096-byte array. All 97 measured service updates matched their native markers, and each execution update had an `EnemyActionExecution` subtree. The preparation column includes the surrounding service Tick and source/readiness checks. Trace overhead can change the number of slices, so the allocation run is separate from timing. One seed-1 side per fixture was measured after one warmup side.

| Fixture | Preparation per side | Execution per side | Largest decision preparation |
|---|---:|---:|---:|
| N5 | 8,529,080 B | 2,750,540 B | 2,186,272 B |
| N8 | 9,288,364 B | 3,196,400 B | 2,432,780 B |

**G-allocation unmet.** All twelve measured decisions exceed 256 KiB of preparation. The N8 side exceeds 2 MiB of preparation alone. Other fixture/seed allocations and individual allocation owners remain unmeasured.

## Correctness and disposition

The final corpus passed. All 55 frozen inputs and 480 Utility trace rows match the item 1 files byte for byte. The run included 1,100 warmed decisions and 220 sides. The new visibility-count regression, 11 legacy decision trace cases, 86 frozen attack Preview vectors, four frozen movement Preview vectors, seven frozen route vectors and the 15 affected A8/X8/H8 decisions passed in Core.

Full Core selected 5,239 cases with 5,205 passed, 26 failed and eight skipped. CoreIntegration selected 1,016 with 967 passed, 47 failed and two skipped. All 72 Phase 0 failure full names, labels and messages matched exactly. The additional Core failure was `AttackPreviewResumptionTests.AllPartitionsMatchFrozenPreview(1)` at its 180-second limit. The partition-2 and partition-31 cases passed in the full run. The same-source isolated rerun selected exactly one case and also timed out at the same limit (184.7673 s elapsed). The partition-1 case remains failed. The completed parity cases establish their own checks.

A standalone rerun of the historical Hold-only diagnostic failed at A8 seed 1 because it still expects `EndEnemyPhase` and receives `Move` after the item 1 change. That run stopped there. Its incomplete output is retained separately.

The next performance work is to measure allocation by owner within source capture, public projection and precise candidate preparation. First-use cost and recurring warmed overruns still need their own acceptance evidence. Rendered gameplay, F1–F8 and activation are outside this item 2 run. The default game path uses immediate `Advance`.

## Evidence locators

Under `Logs/EnemyAi/`, the focused evidence is in `phase-7-item2-boundary-before-v2` and `phase-7-item2-boundary-after`. The native N5 trace is `phase-7-item2-boundary-native/boundary.raw`, with the successful export in `phase-7-item2-boundary-export-v3/boundary-native-samples.tsv`. The temporary source snapshots are under `phase-7-item2-boundary-native/diagnostic-source/`. Early failed or skipped measurement attempts retain their own logs and any emitted XML and are excluded from gate results.

The final timing summaries are `phase-7-item2-atoms/item2-atom-summary.json` and `phase-7-item2-service/prepared-summary.json`. The allocation trace and matched summary are `phase-7-item2-allocation-native/allocation.raw` and `phase-7-item2-allocation-export-v2/allocation-summary.json`. Source hashes, raw CSV, XML, Unity logs and exits are stored with each run. The failure comparison is `phase-7-item2-integration/regression-comparison.json`. The isolated timeout is in `phase-7-item2-preview-retry/tests.xml`. Frozen corpus and historical diagnostic logs are in `phase-7-item2-corpus` and `phase-7-item2-behavior`.
