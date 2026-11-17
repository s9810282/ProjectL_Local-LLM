# Enemy AI computation reuse — Phase 0

Date 2026-11-13. Source revision `360ab06e9e626cd3aaffcf10fe5d279fbde66135`. Unity 6000.0.73f1 on AMD Ryzen 7 6800H with Radeon Graphics.

## Behavior reference

Unity captured 56 scenarios at each tuning profile. Each profile covers 11 test-owned fixtures across seeds 1, 7, 19, 41, and 97, plus a complete side where actual settlement rejects the first selection after the test removes its actor. The session then resumes against its original input. The 336 replay cases matched at partitions 1, 2, and 31 after the diagnostic patch was removed. Each attempt includes full intent, candidate and hold scores, preview charges, acceptance and ordered events. Canonical SHA-256 values cover the full input, transition and final state. The item and environment RNG cursors are also recorded directly.

The existing harm-scope suites passed 40 tests before capture. Three new checks compare the same candidate with a remote Hunter, an unrelated ally outside a local mine blast, and a mine outside forced-movement reach. The local blast bounds exactly one full-health ally at 1,000 permille. Hunter adds a random-outcome gap, so the Hunter comparison uses coarse evaluation on both sides to isolate the harm bound and its score contribution.

Each profile recorded 570 selections, 225 no-reachable-target holds and one uncertainty hold. The uncertainty followed the injected A8/1 stale actor (entity 11) rejection. Its attack term was +100 and the move candidates had zero attack contribution. No natural uncertainty hold or negative attack term appeared among held candidates in these fixtures. Existing diagnostics expose precision and score terms. The focused harm tests inspect coverage gaps and the actual possible allied loss.

The restored-source verification passed 54 tests, including the frozen comparator, overwrite/change guards, all harm checks, attack-preview parity and decision-budget tests. [Baseline manifest](baseline-manifest.json)

## Duplicate computations

| Fixture | Damage builds → unique aim-producing positions | Selection builds → unique positions evaluated | Delivery calls | Repeated delivery occurrences | Peak moved / projected entries per scope |
|---|---:|---:|---:|---:|---:|
| N8 | 545 → 195 | 545 → 195 | 545 | 0 | 39 / 39 |
| A8 | 840 → 560 | 2,490 → 560 | 2,490 | 570 (22.9%) | 245 / 112 |
| W8 fallback | 5 → 5 | 5 → 5 | 5 | 0 | 0 / 0 |
| U8 | 0 | 0 | 0 | 0 | 0 / 0 |

Each row summarizes the first decision over five seeds. Both tuning profiles produced the same counts. The timing and instrumented first intents matched for all 40 fixture/seed/profile combinations.

- P1 has a measured prerequisite reuse opportunity. A8 has 280 repeated common-damage builds and 1,930 selection-parameter builds above one per visited aim-producing position. W8 fallback visited 4,800 distinct positions and produced just five aims.
- P2 is eligible for a post-P1 measurement on A8. Of 2,490 A8 delivery calls, 2,190 ended at selection failure. A scope had up to 384 distinct keys. The 22.9% duplicate rate is an upper bound on the hit rate with a 256-entry cache. The cap-1 piercing control produced eight calls, six unique keys, four null results and three non-null deliveries that missed the requested target.
- U8 completed 280 assessment visibility projections, with 275 immediately repeating the source reference and observer in the same decision context. These counts cover the visibility projection. Candidate world construction and coverage retain their separate work.

Source review traced common damage through trait state, destination modifiers, souls, artifact loadout and other character facts. The reach call supplies neither an event sink nor an execution trait scope. Targeting contributions and range read the fixed actor/profile, normalized environment and world bindings. Delivery adds the aim and fixed world geometry. Target membership happens after delivery. These dependencies fit the proposed actor/world/geometry binding and preserve the shared rule owners. A change to targeting or damage rules still requires shared-query parity and the related AI regressions.

The diagnostic capture used `CaptureReuseWork` with mode `reuse-work` and `PROJECTL_ENEMY_AI_REUSE_COUNTS=1` while the archived patch was applied. The warm timing runs used the same test entry point with the patch absent.

[Per-scope counts](p0-opportunities.json) · [Archived diagnostic patch](p0-diagnostic.patch)

## Warm timing

The four pure Core fixtures used seed-matched first decisions, three warmups per input and 12 samples per seed in each of three Editor launches. Preparation ends when the selected intent is available. Total includes one actual settlement. Each cell is median / nearest-rank P95 in milliseconds, with 60 measured samples per fixture/profile/run.

| Tuning | Fixture | Prep 1 | Prep 2 | Prep 3 | Total 1 | Total 2 | Total 3 |
|---|---|---:|---:|---:|---:|---:|---:|
| 50/50/50 | N8 | 13.35 / 23.47 | 13.32 / 23.77 | 12.86 / 22.02 | 14.24 / 24.93 | 14.21 / 24.62 | 13.64 / 24.43 |
| 50/50/50 | A8 | 33.17 / 74.29 | 33.08 / 60.75 | 32.16 / 61.31 | 33.85 / 74.84 | 33.69 / 61.30 | 32.75 / 63.30 |
| 50/50/50 | W8 | 18.23 / 78.34 | 18.35 / 28.06 | 18.12 / 32.73 | 19.40 / 82.08 | 19.60 / 29.24 | 19.39 / 36.83 |
| 50/50/50 | U8 | 31.29 / 63.51 | 31.31 / 110.36 | 31.68 / 72.71 | 31.62 / 63.84 | 31.69 / 111.52 | 32.03 / 73.04 |
| 20/20/200 | N8 | 12.34 / 17.92 | 12.44 / 17.06 | 12.38 / 13.96 | 13.12 / 18.92 | 13.19 / 18.09 | 13.15 / 14.72 |
| 20/20/200 | A8 | 31.48 / 57.12 | 31.53 / 68.32 | 31.06 / 65.86 | 32.03 / 59.07 | 32.07 / 68.90 | 31.56 / 66.36 |
| 20/20/200 | W8 | 18.56 / 74.83 | 17.94 / 33.48 | 17.88 / 26.10 | 20.07 / 79.20 | 19.14 / 37.22 | 19.12 / 27.68 |
| 20/20/200 | U8 | 30.92 / 66.86 | 31.02 / 83.96 | 31.44 / 62.10 | 31.31 / 67.19 | 31.35 / 84.30 | 31.80 / 62.44 |

The widest spread between the three pure Core preparation medians is 1.01 ms in A8 at 50/50/50. The tails vary more, especially W8 and U8. Compare future timings with the same workload, runtime and measurement mode.

The unchanged prepared-service recorder uses its existing 50/50/50 control profile and service-owned fixtures. Each cell uses the first decision of 20 measured sides per fixture/run. The 1 ms service budget is active. All three runs matched ordered publications and final battle state against synchronous Utility. Slots are synthetic Editor updates.

| Fixture | Prep ms 1 | Prep ms 2 | Prep ms 3 | Slots 1 | Slots 2 | Slots 3 |
|---|---:|---:|---:|---:|---:|---:|
| N5 | 13.76 / 21.45 | 13.81 / 17.04 | 14.22 / 23.79 | 15 / 22 | 15 / 18 | 15.5 / 24 |
| N6 | 13.22 / 16.84 | 13.59 / 18.69 | 13.78 / 41.09 | 14 / 18 | 15 / 19 | 15 / 41 |
| N8 | 13.36 / 20.26 | 13.86 / 18.64 | 14.57 / 20.55 | 14.5 / 20 | 15 / 20 | 16 / 19 |
| G16 | 15.25 / 15.65 | 15.20 / 16.11 | 16.40 / 19.05 | 16 / 17 | 16 / 17 | 17.5 / 20 |

Allocation calibration returned zero in all six time captures. Allocation bytes are unavailable. The recorded entry counts characterize scope sizes. PlayMode latency remains unverified.

## First samples, excluded from warm timing

Seed 1 first sample in each fixture/profile/run. Pure Core cells are preparation / total milliseconds. Service cells are preparation milliseconds / update slots. Only the first workload in each Editor launch is process-cold.

| Tuning | Pure Core fixture | Run 1 | Run 2 | Run 3 |
|---|---|---:|---:|---:|
| 50/50/50 | N8 | 444.24 / 503.57 | 463.39 / 522.43 | 406.56 / 474.17 |
| 50/50/50 | A8 | 54.38 / 55.62 | 52.75 / 54.15 | 49.96 / 51.06 |
| 50/50/50 | W8 | 36.09 / 37.44 | 37.77 / 39.11 | 35.37 / 36.63 |
| 50/50/50 | U8 | 32.30 / 42.91 | 32.62 / 43.18 | 32.67 / 43.22 |
| 20/20/200 | N8 | 12.58 / 13.35 | 12.67 / 13.43 | 12.83 / 13.64 |
| 20/20/200 | A8 | 30.98 / 31.57 | 31.97 / 32.60 | 30.46 / 30.97 |
| 20/20/200 | W8 | 18.32 / 19.45 | 17.83 / 19.09 | 24.54 / 31.01 |
| 20/20/200 | U8 | 33.45 / 33.86 | 31.09 / 31.42 | 30.62 / 30.97 |

| Service fixture | Run 1 | Run 2 | Run 3 |
|---|---:|---:|---:|
| N5 | 329.59 / 112 | 336.72 / 116 | 356.32 / 117 |
| N6 | 13.16 / 14 | 14.14 / 15 | 13.52 / 15 |
| N8 | 14.33 / 15 | 13.88 / 15 | 13.60 / 15 |
| G16 | 15.24 / 16 | 15.35 / 17 | 15.97 / 17 |

Raw samples and NUnit XML are in `Logs/EnemyAi/20261113-reuse-p0-timing-{1,2,3}`, `...-service-{1,2,3}` and `...-verify-restored`. Source hashes for all four temporary call sites matched their pre-instrumentation values. Six worktree content changes matched their preflight byte snapshots. Seven additional paths appeared in Git status before and after the runs and had an empty normalized Git diff. Their byte hashes were recorded after the runs.

Phase 0 ends at the P1 review point.
