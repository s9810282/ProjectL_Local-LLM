# Enemy AI computation reuse — Phase 3

Date 2026-11-13. Base revision `360ab06e9e626cd3aaffcf10fe5d279fbde66135` with the verified Phase 2 changes.

## Assessment visibility

`EnemyDecisionContext` retains one completed assessment projection per decision context. The key uses the actual environment reference and observer passed by `CombatActionAssessment`. `MovementEnvironmentFacts.VisibleSteps` owns the projection rules and assessment uses `preservePublic: true`. The ordinary geometry projection has its own entry and observer-only semantics.

The assessment entry is replaced after the underlying iterator finishes. An interrupted or faulted miss leaves the last completed entry available. Candidate input construction and harm/route evaluation run for each assessment.

| Change scenario | Before Phase 3 | Phase 3 | Verification boundary |
|---|---|---|---|
| Change visibility rules | Projection rules live in `VisibleSteps`; assessment invokes it directly | The rule stays in `VisibleSteps`; assessment and geometry pass their distinct visibility modes | Tile values, order, known-to membership, hidden information and frozen decisions |
| Change followup input or assessment lifetime | The assessment reads the projected input and uses the parent context for preview charges | Keep the actual source reference at the assessment call site and the one-entry lifetime in the parent context | Followup with different environment, cancellation and budget tests |

## Verification

Unity 6000.0.73f1 Core EditMode passed 160 focused regression tests after the count probes were removed. Coverage includes assessment and followup, forced movement, combined hazards, side cache, attack preview, the P0 harm counterexamples and the three existing harm suites, decision budget, and Phase 2 delivery reuse.

The seven new tests compare full tile values and memberships, source and observer replacement, distinct geometry projection, the actual followup environment with its parent budget, cancellation before and after the inner callback, and a faulted projection. Both tuning profiles matched the Phase 0 baseline across partitions 1, 2 and 31 for 336 complete-side comparisons. The before/after timing and counting launches matched selected intent, preview charges and cap hits for all 30 fixture/seed/profile combinations.

## Completed projection builds

Counts aggregate five first decisions per fixture and tuning profile. Requests and completed underlying projections are counted separately. The two diagnostic patches are archived in `Logs/EnemyAi/20261113-reuse-p3-audit`.

| Tuning | Fixture | Assessment requests | Before builds | P3 builds | Eliminated |
|---|---|---:|---:|---:|---:|
| 50/50/50 | U8 | 280 | 280 | 5 | 275 |
| 50/50/50 | H8 | 70 | 70 | 20 | 50 |
| 50/50/50 | X8 | 90 | 90 | 20 | 70 |
| 20/20/200 | U8 | 280 | 280 | 5 | 275 |
| 20/20/200 | H8 | 70 | 70 | 20 | 50 |
| 20/20/200 | X8 | 90 | 90 | 20 | 70 |

## Timing

Three fresh before and three after Editor launches ran the same pure Core harness. Each fixture/profile/run used five seeds, three warmups per seed and 12 timed repetitions per seed. Each cell is median / nearest-rank P95 in milliseconds across 60 warm samples. Median shift compares the median of the three run medians. First samples are recorded in the structured evidence.

### Decision preparation

| Tuning | Fixture | Before 1 | Before 2 | Before 3 | P3 1 | P3 2 | P3 3 | Median shift |
|---|---|---:|---:|---:|---:|---:|---:|---:|
| 50/50/50 | U8 | 32.58 / 78.35 | 32.06 / 72.02 | 32.56 / 68.15 | 31.91 / 64.79 | 31.49 / 68.56 | 30.95 / 68.88 | -3.3% |
| 50/50/50 | H8 | 11.53 / 16.16 | 11.73 / 17.32 | 11.96 / 16.51 | 12.04 / 18.24 | 11.80 / 15.05 | 11.56 / 15.46 | +0.6% |
| 50/50/50 | X8 | 12.60 / 17.27 | 12.82 / 16.01 | 12.65 / 16.29 | 12.45 / 19.56 | 12.63 / 17.36 | 12.71 / 18.82 | -0.2% |
| 20/20/200 | U8 | 31.06 / 62.05 | 30.90 / 62.78 | 31.11 / 67.46 | 30.05 / 80.91 | 29.61 / 103.45 | 33.17 / 84.24 | -3.2% |
| 20/20/200 | H8 | 11.23 / 16.59 | 11.15 / 16.05 | 10.98 / 14.43 | 10.75 / 12.20 | 11.05 / 15.33 | 12.82 / 22.44 | -0.9% |
| 20/20/200 | X8 | 12.59 / 18.95 | 12.50 / 16.58 | 12.42 / 19.22 | 12.23 / 21.37 | 12.49 / 17.26 | 13.34 / 17.75 | -0.1% |

### Decision and actual execution

| Tuning | Fixture | Before 1 | Before 2 | Before 3 | P3 1 | P3 2 | P3 3 | Median shift |
|---|---|---:|---:|---:|---:|---:|---:|---:|
| 50/50/50 | U8 | 32.97 / 78.73 | 32.40 / 72.52 | 32.91 / 68.52 | 32.25 / 65.17 | 31.84 / 68.92 | 31.32 / 69.24 | -3.2% |
| 50/50/50 | H8 | 12.37 / 16.97 | 12.55 / 21.35 | 12.93 / 17.86 | 12.92 / 20.20 | 12.64 / 16.07 | 12.36 / 16.49 | +0.7% |
| 50/50/50 | X8 | 13.37 / 18.24 | 13.58 / 17.01 | 13.43 / 17.26 | 13.17 / 20.54 | 13.38 / 18.21 | 13.46 / 19.66 | -0.4% |
| 20/20/200 | U8 | 31.38 / 62.39 | 31.24 / 63.10 | 31.44 / 67.79 | 30.39 / 82.25 | 29.95 / 103.78 | 33.55 / 84.57 | -3.2% |
| 20/20/200 | H8 | 11.98 / 17.56 | 11.94 / 16.99 | 11.78 / 15.99 | 11.48 / 13.00 | 11.85 / 16.35 | 13.75 / 23.95 | -0.8% |
| 20/20/200 | X8 | 13.37 / 20.93 | 13.25 / 17.61 | 13.13 / 20.18 | 12.94 / 25.49 | 13.16 / 18.14 | 14.13 / 18.65 | -0.7% |

U8 preparation changed 32.56 → 31.49 ms at 50/50/50 and 31.06 → 30.05 ms at 20/20/200. Environmental fixture median shifts were H8 at 50/50/50 +0.6%, X8 at 50/50/50 -0.2%, H8 at 20/20/200 -0.9%, X8 at 20/20/200 -0.1%. U8 P95 ranges were 50/50/50 68.15–78.35 → 64.79–68.88 ms, 20/20/200 62.05–67.46 → 80.91–103.45 ms. The preparation timer includes the entire decision. The count change quantifies completed visibility projections, while world construction and coverage still contribute to preparation time.

U8 preparation medians improved in all three default-tuning runs and two of three current-tuning runs. The third current-tuning launch had higher medians in U8, H8 and X8 than its matched baseline. Current-tuning U8 and default-tuning X8 preparation P95 increased in all three runs. The captures do not isolate the cause. Phase 3 is retained on the measured reduction in repeated projections and the U8 median benefit, with the tail increases recorded for review.

Allocation calibration returned zero. Allocation bytes are unavailable. These samples are pure Core Editor timings. Prepared-service slots and actual PlayMode latency remain Phase 4 measurements.

[Structured results, source and baseline hashes, and NUnit run evidence](p3-opportunities.json). Raw CSV and XML are under `Logs/EnemyAi/20261113-reuse-p3-*`. The two temporarily instrumented files and all retained Phase 2 sources were hash-verified.

Phase 3 ends at the Phase 4 review point.
