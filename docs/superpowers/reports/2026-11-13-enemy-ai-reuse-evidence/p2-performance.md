# Enemy AI computation reuse — Phase 2

Date: 2026-11-13. Starting revision: 360ab06e9e626cd3aaffcf10fe5d279fbde66135 with the user's uncommitted Phase 1 changes. Phase 2 is implemented and retained for user review. Phase 3/4 were not started. No commit or push was performed.

## Changes and boundaries

The existing actor/world-bound Relocations scope now retains completed attack delivery by position and aim, including completed null results. The default insertion limit is 256. A full cache retains existing entries and computes missing queries normally; it never prunes a target or candidate. Entries are stored after delivery enumeration finishes, not when the result callback first runs. The shared target-membership helper preserves hit order and AppliesDamage checks.

Only EnemyAttackReach.cs and CombatAttackQueries.cs changed in production during P2. EnemyAttackReachReuseTests.cs gained tests; existing P1 assertions were preserved. No tuning, score, coverage, precision budget, beam, actor-order, Framework/Buffer/Job or View change was made. The preparation budget remains 1 ms.

The modification/understanding boundary is the existing reach scope and shared delivery contract. No separate combat rule implementation, cross-world invalidation system or service was introduced. Shared-query parity and the relevant attack/path/assessment regressions remain necessary despite this small code boundary.

## Verification

- Focused suite: **300/300 passed**, including P1 regressions, full frozen behavior, budgets, preview parity and all three harm-scope suites.
- Temporary default capacity 0 and 1: **51/51** and **51/51 passed**. Each includes both tuning profiles and partitions 1/2/31 against the unchanged P0 full-side references: 336 comparisons per capacity. Default 256 was covered in the focused run. Across these three configurations, 1,008 full-side comparisons preserve selected intents, transitions, final facts and charges.
- After restoring default 256 and removing diagnostic code, final focused checks: **84/84 passed**. The three changed files match the independently reviewed snapshot byte-for-byte.
- New tests cover cross-target reuse, null failure versus target miss, capacities 0/1/256, exact ordered piercing delivery, All/splash, full-capacity fallback, wrong environment, cancellation before/after callback, and a throwing delivery query.
- RED evidence: the duplicate-query test failed for both piercing caps before P2 (2 repeated SelectionPolicy stages instead of 0). Missing API failures were also recorded. Initial new-test import/serialization issues were corrected without changing production rules or pre-existing expectations.
- The initial broad baseline filter accidentally selected the explicit baseline-generation method; its mode guard failed before writing files. Subsequent filters excluded it. A first diagnostic capture executed 0 tests after a sandbox-blocked partial patch; it is not evidence. The completed counts-2 run executed and passed one capture.
- A fresh GPT-6 Astra review found no Critical/Important/Minor code findings. The reviewer deferred runtime/performance judgments to these captures; static review is not presented as test execution.

CoreIntegration full-suite, prepared-service before/after, lower-spec hardware and PlayMode latency remain Phase 4/user verification. This result does not claim measured in-game waiting-time improvement.

## Actual work saved

Counts aggregate five seeds per fixture; both tuning profiles produced the same counts. The limit retained every repeated A8 key in this workload even though 384 unique keys were attempted per scope.

| Fixture | Requests | Actual builds | Reused | Reused null | Misses at capacity | Peak entries |
|---|---:|---:|---:|---:|---:|---:|
| N8 | 545 | 545 | 0 | 0 | 0 | 109 |
| A8 | 2490 | 1920 | 570 | 500 | 640 | 256 |
| W8 | 5 | 5 | 0 | 0 | 0 | 0 |
| U8 | 0 | 0 | 0 | 0 | 0 | 0 |

A8 builds fall from 2,490 to 1,920: **570 builds eliminated (22.9%)**. This is not a 22.9% whole-decision speedup. Cached null means the same attack delivery failed; it is not an uncertainty marker or an all-allies loss estimate. N8 has no reuse in this corpus and stores entries without gaining hits. W8 fallback and U8 hold cases do not use this delivery cache here.

## Matched preparation timings

Three fresh P1 baseline launches and three P2 launches on the same machine, without counting instrumentation. Each row/run contains 60 warmed samples (five seeds × twelve repetitions); first samples are archived separately. Cells show median / nearest-rank P95 milliseconds. Shift compares the median of the three run medians.

| Tuning / fixture | Before 1 | Before 2 | Before 3 | P2 1 | P2 2 | P2 3 | Median shift |
|---|---:|---:|---:|---:|---:|---:|---:|
| 50/50/50/N8 | 12.57 / 22.20 | 13.41 / 23.13 | 13.54 / 24.34 | 13.35 / 30.43 | 13.57 / 25.89 | 13.00 / 25.10 | -0.5% |
| 50/50/50/A8 | 28.05 / 65.18 | 28.85 / 66.67 | 28.73 / 66.82 | 27.77 / 48.20 | 26.46 / 30.50 | 28.01 / 69.47 | -3.4% |
| 50/50/50/W8 | 18.30 / 31.96 | 18.55 / 31.89 | 18.37 / 28.79 | 18.30 / 69.19 | 18.32 / 46.55 | 18.07 / 29.15 | -0.4% |
| 50/50/50/U8 | 30.92 / 62.91 | 31.32 / 60.44 | 31.92 / 67.33 | 31.43 / 60.69 | 31.61 / 78.38 | 31.04 / 67.38 | 0.4% |
| 20/20/200/N8 | 11.39 / 15.16 | 11.72 / 12.95 | 11.92 / 16.57 | 11.93 / 25.98 | 11.60 / 16.57 | 11.90 / 14.95 | 1.5% |
| 20/20/200/A8 | 26.68 / 38.99 | 27.54 / 43.84 | 27.44 / 45.58 | 26.55 / 60.56 | 25.43 / 26.72 | 25.95 / 27.61 | -5.4% |
| 20/20/200/W8 | 17.84 / 26.62 | 18.00 / 26.26 | 17.82 / 25.48 | 17.86 / 24.78 | 18.08 / 40.20 | 17.75 / 32.77 | 0.2% |
| 20/20/200/U8 | 30.85 / 64.20 | 31.33 / 68.69 | 31.85 / 66.18 | 31.06 / 64.94 | 31.44 / 104.23 | 30.76 / 65.67 | -0.9% |

A8 preparation medians improve in all three matched runs for both profiles. The median-of-medians reduction is **3.4% at 50/50/50** and **5.4% at 20/20/200**. All 40 fixture/seed/profile first-intent hashes, preview charges and cap hits matched across timing captures; the counter capture also matched all 40 first-intent hashes.

Tail tradeoff: N8 at 50/50/50 had P95 22.20–24.35 ms before and 25.10–30.43 ms after, higher in every matched run despite a nearly unchanged median. W8 default P95 also rose in these runs (28.79–31.96 to 29.15–69.19 ms) while its median stayed stable and its fallback did not gain entries. Other tails vary with run/GC; these measurements do not isolate causation. **Uniform tail-latency improvement is not established.** Retaining P2 is based on eliminated repeated work and the repeatable targeted A8 median benefit, with this unresolved tail behavior explicitly left for the phase review rather than concealed by a budget/policy change.

Allocation calibration was unavailable, so no byte reduction or zero-allocation claim is made. The tested retained-entry limit is 256; retained-object/GC effects are not quantified. The implementation does not enlarge scope lifetime beyond the existing candidate build.

## Evidence and preservation

[Structured results, hashes, counts and full timing statistics](p2-opportunities.json). Raw runs are under Logs/EnemyAi/20261113-reuse-p2-*: before-1..3, after-1..3, focused, counts-2, capacity-0, capacity-1 and final-restored. The audit directory stores exact pre-P2 files, the isolated review diff and temporary counter source. Raw failed/empty setup runs are retained and excluded from successful evidence.

Unrelated inherited files and Phase 1 files outside the P2 edit set were hash-checked unchanged. The original baseline references were not regenerated. Stage assets and gameplay source outside the two named production files were not changed by P2.

Phase 2 stops here for user review. No Phase 3 implementation is included.
