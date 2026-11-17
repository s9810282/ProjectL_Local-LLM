# Enemy AI computation reuse — Phase 1

Date 2026-11-13. Base revision `360ab06e9e626cd3aaffcf10fe5d279fbde66135`. Phase 1 is retained for review.

## Changes and contracts

The existing engagement field creates one `EnemyAttackReach.Relocations` per actor and world. The scope now checks input, actor, observed geometry and the separately supplied environment by reference before any lookup. An aim-producing position can retain one common-damage value and one selection preparation, including the exact projected source references. Prerequisite misses compute damage and selection in separate iterator steps. The ordinary attack query and fallback pursuit use the shared unprepared path. Target membership, delivery, preview charging, Buffer ownership and the service budget retain their existing contracts.

| Change scenario | Before | Phase 1 | Required verification |
|---|---|---|---|
| Change a damage or range rule | Edit the shared calculator and exercise reach and attack queries | Edit the same calculator. If the rule reads new facts, check that the bound world/actor/profile/environment covers them | Shared query parity, destination/disarm reach tests and frozen decisions |
| Change reuse lifetime or position identity | The engagement field shared moved facts and projected inputs across targets | Edit the existing scope and its reach consumer. Use the same field-owned lifetime | Scope mismatch, repeated target queries and pursuit resumption |

The shared calculators still own combat rules. The additional dictionary contains derived values from the existing per-position projection. It has the same 289-position upper bound as the field. The observed peak was 112 entries in A8. W8 fallback held zero entries.

## Verification

The final Unity 6000.0.73f1 Core EditMode run passed 212 tests after the diagnostic patch was removed. It includes the prepared/unprepared query comparisons, the location scope and no-aim build checks, pursuit equivalence/resumption, engagement, beam and budget regressions, attack-preview parity, all three harm-scope suites and the Phase 0 harm counterexamples.

Both tuning profiles matched the Phase 0 baseline across partitions 1, 2 and 31, for 336 complete-side comparisons. The baseline includes the actual rejected transition and session continuation. The reference hashes were checked after the run. Across the timed and instrumented runs, all 40 fixture/seed/profile first intents matched, and the timed preview charges and cap hits matched.

## Computation counts

Counts cover five first decisions per fixture and tuning profile. Both profiles produced the same counts. U8 did not enter a reach scope during these decisions.

| Fixture | Common-damage builds | Selection-parameter builds | Delivery calls | Repeat delivery occurrences | Peak prerequisite entries per scope |
|---|---:|---:|---:|---:|---:|
| N8 | 545 → 195 | 545 → 195 | 545 | 0 | 39 |
| A8 | 840 → 560 | 2490 → 560 | 2490 | 570 | 112 |
| W8 | 5 → 5 | 5 → 5 | 5 | 0 | 0 |
| piercing-cap-1 | 2 → 1 | 8 → 1 | 8 | 2 | 1 |

The focused cap-1 piercing case still produced four null deliveries and three non-null target misses. The A8 delivery rate remains 570 repeated occurrences in 2,490 calls (22.9%), with 2,190 null deliveries and up to 384 unique keys in one scope. A8 clears the duplicate-key gate for evaluating P2. The actual hit rate under the proposed 256-entry cap and the benefit after P1 remain open.

[Per-scope data, source hashes and test results](p1-opportunities.json) · [Archived diagnostic patch](p1-diagnostic.patch)

## Matched warm timings

Each row uses 60 measured samples per run, with five matched seeds, three warmups per seed and 12 timed repetitions per seed. Both phases used the same hardware and runtime. Counting ran in a separate launch. Each cell shows median / nearest-rank P95 in milliseconds. Median shift compares the median of the three run medians.

### Decision preparation

| Tuning | Fixture | P0 run 1 | P0 run 2 | P0 run 3 | P1 run 1 | P1 run 2 | P1 run 3 | Median shift |
|---|---|---:|---:|---:|---:|---:|---:|---:|
| 50/50/50 | N8 | 13.35 / 23.47 | 13.32 / 23.77 | 12.86 / 22.02 | 12.57 / 30.21 | 12.91 / 24.39 | 12.78 / 24.82 | -4.1% |
| 50/50/50 | A8 | 33.17 / 74.29 | 33.08 / 60.75 | 32.16 / 61.31 | 27.85 / 55.56 | 29.43 / 60.56 | 28.92 / 70.70 | -12.6% |
| 50/50/50 | W8 | 18.23 / 78.34 | 18.35 / 28.06 | 18.12 / 32.73 | 17.80 / 30.15 | 18.18 / 72.69 | 18.19 / 59.49 | -0.3% |
| 50/50/50 | U8 | 31.29 / 63.51 | 31.31 / 110.36 | 31.68 / 72.71 | 30.93 / 65.94 | 31.41 / 59.28 | 31.30 / 55.23 | -0.1% |
| 20/20/200 | N8 | 12.34 / 17.92 | 12.44 / 17.06 | 12.38 / 13.96 | 11.73 / 16.35 | 11.73 / 15.15 | 11.67 / 15.82 | -5.3% |
| 20/20/200 | A8 | 31.48 / 57.12 | 31.53 / 68.32 | 31.06 / 65.86 | 26.84 / 38.97 | 27.51 / 35.04 | 27.38 / 54.50 | -13.0% |
| 20/20/200 | W8 | 18.56 / 74.83 | 17.94 / 33.48 | 17.88 / 26.10 | 17.65 / 26.68 | 17.85 / 27.86 | 17.77 / 36.04 | -0.9% |
| 20/20/200 | U8 | 30.92 / 66.86 | 31.02 / 83.96 | 31.44 / 62.10 | 31.20 / 59.54 | 31.62 / 71.73 | 31.00 / 66.75 | +0.6% |

### Decision and execution

| Tuning | Fixture | P0 run 1 | P0 run 2 | P0 run 3 | P1 run 1 | P1 run 2 | P1 run 3 | Median shift |
|---|---|---:|---:|---:|---:|---:|---:|---:|
| 50/50/50 | N8 | 14.24 / 24.93 | 14.21 / 24.62 | 13.64 / 24.43 | 13.52 / 32.10 | 13.83 / 25.83 | 13.70 / 26.35 | -3.6% |
| 50/50/50 | A8 | 33.85 / 74.84 | 33.69 / 61.30 | 32.75 / 63.30 | 28.40 / 56.12 | 30.00 / 62.65 | 29.46 / 71.23 | -12.6% |
| 50/50/50 | W8 | 19.40 / 82.08 | 19.60 / 29.24 | 19.39 / 36.83 | 19.04 / 35.48 | 19.62 / 78.00 | 19.40 / 60.89 | -0.0% |
| 50/50/50 | U8 | 31.62 / 63.84 | 31.69 / 111.52 | 32.03 / 73.04 | 31.27 / 66.28 | 31.76 / 59.61 | 31.68 / 55.58 | -0.0% |
| 20/20/200 | N8 | 13.12 / 18.92 | 13.19 / 18.09 | 13.15 / 14.72 | 12.50 / 17.28 | 12.47 / 16.01 | 12.44 / 16.82 | -5.2% |
| 20/20/200 | A8 | 32.03 / 59.07 | 32.07 / 68.90 | 31.56 / 66.36 | 27.38 / 39.56 | 28.02 / 35.64 | 27.89 / 55.03 | -12.9% |
| 20/20/200 | W8 | 20.07 / 79.20 | 19.14 / 37.22 | 19.12 / 27.68 | 18.83 / 28.51 | 19.02 / 29.50 | 19.02 / 46.42 | -0.6% |
| 20/20/200 | U8 | 31.31 / 67.19 | 31.35 / 84.30 | 31.80 / 62.44 | 31.55 / 59.89 | 31.95 / 72.78 | 31.33 / 67.10 | +0.6% |

A8 preparation medians improved in every run under both profiles. The median of run medians changed from 33.08 ms to 28.92 ms at 50/50/50 and from 31.48 ms to 27.38 ms at 20/20/200. W8 and U8 median shifts were below 1%. N8 at 50/50/50 had a lower median and a higher P95 in all three runs (22.02–23.77 ms in P0, 24.39–30.21 ms in P1). The slow samples span different seeds and include samples with and without a completed collection. The measurements leave the tail increase unexplained. A8’s repeatable median gain supports retaining Phase 1 with that tradeoff recorded.

Allocation calibration returned zero. Allocation bytes are unavailable. First samples are excluded from the tables and archived in the measurement data. The three matched comparisons cover the pure Core harness. The prepared-service, structural-atom and PlayMode measurements are outside this Phase 1 result.

Raw NUnit XML and CSV are under `Logs/EnemyAi/20261113-reuse-p1-timing-{1,2,3}`, `...-counts` and `...-verify-restored`. The four instrumented files matched their pre-instrumentation byte hashes. Preflight byte snapshots verified the unrelated tracked changes.

Phase 1 ends at the P2 review point.
