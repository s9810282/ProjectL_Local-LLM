# Enemy AI computation reuse — integrated result

Date 2026-11-13. Base revision `360ab06e9e626cd3aaffcf10fe5d279fbde66135` plus the retained P1–P3 working-tree changes. Unity 6000.0.73f1 on AMD Ryzen 7 6800H. Source and reference hashes are in the structured evidence.

| Phase | Disposition |
|---|---|
| P0 | Frozen behavioral oracle and baseline measurements |
| P1 | Retained per-position damage and targeting prerequisites |
| P2 | Retained completed delivery reuse with capacity 256 |
| P3 | Retained one completed assessment visibility projection per decision context |
| P4 | Integrated verification and handoff |

## Semantic and regression results

Both frozen tuning profiles matched all 56 scenarios at partitions 1, 2 and 31, for 336 complete-side comparisons. The reference includes full selected intents, candidate and hold terms, preview charges, transition hashes, ordered events and final state/RNG. Phase 4 used capacity 256. The Phase 2 archive contains the separate full-side runs at capacities 0 and 1. [Phase 2 evidence](2026-11-13-enemy-ai-reuse-evidence/p2-performance.md)

The prepared-service fixture also exercised cancellation during a decision, stale input after preparation and a hidden-terrain rejection. The recovery run compared each accepted publication and resulting stores with synchronous Utility through the end of the enemy side. Its rejected or stale attempts left the stores at their pinned values. Full-suite checks additionally covered frame spending and stale identity disposal.

| Full assembly | Passed | Failed | Skipped | Runtime | Same failure / behavior failures checked |
|---|---:|---:|---:|---:|---:|
| Core | 5608 | 5 | 8 | 705.45 s | 2/2 |
| CoreIntegration | 1035 | 34 | 3 | 216.31 s | 34/34 |

Three of the five Core failures were dedicated capture assertions because Unity selected explicit methods during assembly discovery. The capture entry points now skip outside their requested modes. A focused rerun reported all three as skipped and passed the baseline overwrite/change guard. The table preserves the actual full-run totals from before this test-only correction. The immutable baseline files were hash-verified.

Every remaining failing case reproduced with the same failure message on the original pre-reuse production source and the same current workspace assets. Full-suite status remains red where those inherited cases fail.

Core’s behavioral failures concern interception payload validation and a Taunt activation count of two where the test expects one. CoreIntegration failures include the legacy `Cinemachine.CinemachineCameraOffset` type lookup, a missing parent Canvas, deck UI setup and audio binding, production DB values and run facility expectations. The tuning assertion expects `50/50/150` while the current asset supplies `20/20/200`. These failures are tracked separately from the optimization.

- `Core.TraitActivationContractTests` reproduced 1 of 1 final failures.
- `Core.TraitActivationOutcomeTests` reproduced 1 of 1 final failures.
- `CoreIntegration.CombatCameraZoomPoseTests` reproduced 1 of 1 final failures.
- `CoreIntegration.CombatStructureInspectionTests` reproduced 1 of 1 final failures.
- `CoreIntegration.DataBaseAssetTests` reproduced 8 of 8 final failures.
- `CoreIntegration.DeckMainChangePolicyTests` reproduced 8 of 8 final failures.
- `CoreIntegration.DeckSlotInfoPanelTests` reproduced 13 of 13 final failures.
- `CoreIntegration.EnemyAiTuningAssetTests` reproduced 1 of 1 final failures.
- `CoreIntegration.RunGoldFacilityTests` reproduced 2 of 2 final failures.

The failure records retain test names, reasons, stacks and original-source results. The three harm-scope suites and the P0 harm counterexamples ran in the full Core assembly. [Structured verification evidence](2026-11-13-enemy-ai-reuse-evidence/p4-opportunities.json)

### Harm and holding

Each profile recorded 225 no-reachable-target holds and one uncertainty hold. The uncertainty came from the synthetic A8/1 stale-actor exercise. Removing actor 11 deliberately caused `EntityNotFound` at settlement. Its attack option contributed +100 and the move options had zero attack contribution. The hold follows rejection and does not carry a global-harm score. No natural uncertainty hold or negative attack term among held candidates appeared in the frozen fixtures.

The focused checks distinguish a remote Hunter random-outcome gap from allied exposure. A remote ally outside a local mine blast leaves its 1,000-permille local bound unchanged. A mine outside forced-movement reach contributes zero possible allied loss. These cases and the frozen corpus delimit the harm finding.

## Computations and pure Core timing

The final counts are from five first decisions per fixture/profile. Both profiles produced the same build counts. Per-position prerequisites are bounded by the field’s 289 positions. Delivery entries are bounded at 256. Assessment visibility retains one completed result per context.

| Fixture | Damage builds P0 → P4 | Targeting builds P0 → P4 | Delivery requests / builds / hits | Full-cache misses | Peak prerequisites / deliveries | Visibility builds P0 or pre-P3 → P4 |
|---|---:|---:|---:|---:|---:|---:|
| N8 | 545 → 195 | 545 → 195 | 545 / 545 / 0 | 0 | 39 / 109 | 90 → 20 |
| A8 | 840 → 560 | 2490 → 560 | 2490 / 1920 / 570 | 640 | 112 / 256 | 85 → 20 |
| W8 | 5 → 5 | 5 → 5 | 5 / 5 / 0 | 0 | 0 / 0 | 40 → 5 |
| U8 | 0 → 0 | 0 → 0 | 0 / 0 / 0 | 0 | 0 / 0 | 280 → 5 |
| H8 | — / 195 | — / 195 | 540 / 540 / 0 | 0 | 39 / 108 | 70 → 20 |
| X8 | — / 195 | — / 195 | 545 / 545 / 0 | 0 | 39 / 109 | 90 → 20 |

The A8 delivery hits include 500 completed null results. W8 recorded five search cap hits per tuning profile, one per seed. Timed and counted first intents, preview charges and cap hits matched their references. The JSON archives the per-seed measurements.

The pure Core timer compares the three P0 launches with three new integrated launches. Each cell uses 60 warm samples per launch, five seeds and three warmups per seed. Reported medians are medians of run medians. P95 ranges are across launches. Preparation ends when the decision is ready. Total adds actual settlement.

| Tuning | Fixture | Preparation median P0 → P4 (ms) | Shift | Preparation P95 P0 → P4 (ms) | Total median P0 → P4 (ms) |
|---|---|---:|---:|---:|---:|
| 50/50/50 | N8 | 13.32 → 13.69 | +2.8% | 22.02–23.77 → 18.81–25.36 | 14.21 → 14.57 |
| 50/50/50 | A8 | 33.08 → 27.20 | -17.8% | 60.75–74.29 → 32.17–67.76 | 33.69 → 27.76 |
| 50/50/50 | W8 | 18.23 → 18.12 | -0.6% | 28.06–78.34 → 26.11–49.16 | 19.40 → 19.28 |
| 50/50/50 | U8 | 31.31 → 29.84 | -4.7% | 63.51–110.36 → 53.88–114.49 | 31.69 → 30.17 |
| 20/20/200 | N8 | 12.38 → 11.96 | -3.4% | 13.96–17.92 → 13.85–28.74 | 13.15 → 12.65 |
| 20/20/200 | A8 | 31.48 → 26.55 | -15.7% | 57.12–68.32 → 41.59–58.61 | 32.03 → 27.09 |
| 20/20/200 | W8 | 17.94 → 18.02 | +0.5% | 26.10–74.83 → 25.45–32.32 | 19.14 → 19.23 |
| 20/20/200 | U8 | 31.02 → 29.91 | -3.6% | 62.10–83.96 → 63.50–95.29 | 31.35 → 30.25 |

A8 provides the largest preparation gain in both settings. N8 at default tuning has a 2.8% higher preparation median and stores up to 109 delivery entries with zero delivery hits in this sample. W8 fallback remains near its original median. U8 improves at the median with variable tails. The reach fallback, candidate coverage work and rendered presentation remain separate contributors to investigate if the pause persists.

The process-first N8 preparation sample was 406.56–463.39 ms in P0 and 382.28–480.16 ms in P4. First samples are archived separately from warm measurements. Allocation calibration returned zero, so byte counts and retained-object GC cost remain unavailable.

## Prepared-service control and calculation steps

The unmodified service capture used service-owned N5/N6/N8/G16 fixtures and the 50/50/50 control with the existing 1 ms budget. The original-source and integrated runs each executed 80 warm sides plus 20 warmup sides. Both compared 700 ordered publications and final stores with synchronous Utility. The publication sequence of actor and kind also matched across the captures.

| Fixture | First decision preparation ms median / P95, original → integrated | First decision update slots median / P95, original → integrated | Full side slots median / P95, original → integrated |
|---|---:|---:|---:|
| N5 | 16.87 / 22.62 → 13.83 / 20.76 | 18 / 21 → 15 / 19 | 69 / 104 → 57 / 89 |
| N6 | 15.65 / 24.31 → 12.62 / 13.46 | 16.5 / 25 → 14 / 15 | 64 / 79 → 53 / 61 |
| N8 | 16.25 / 59.67 → 12.87 / 17.39 | 17.5 / 41 → 14 / 17 | 63.5 / 89 → 54 / 80 |
| G16 | 18.44 / 28.54 → 14.68 / 19.75 | 19 / 29 → 16 / 21 | 80 / 129 → 61 / 68 |

First process-use N5 preparation was 379.17 → 306.03 ms. Update slots are synthetic Editor updates. The highest observed prepared atom was 17.60 → 20.11 ms. These maxima include runtime interference and the structural check compares minimum repeats separately.

The fresh original-source N8 median of 16.25 ms was higher than the three Phase 0 service controls, which ranged from 13.36 to 14.57 ms. The integrated N8 capture was 12.87 ms. Interpret the fresh before/after pair alongside that original-source variation. [Phase 0 service evidence](2026-11-13-enemy-ai-reuse-evidence/p0-performance.md)

The service structural capture uses the minimum of four warm repeats at each step and asserts a 0.10 ms maximum. The first integrated capture exceeded that limit at `Attack/SacrificeSource → Attack/SacrificePlan`. The transition executes existing attack preview work. Here are both original and both integrated captures.

| Service structural run | Max repeated step (ms) | Phase transition | Result |
|---|---:|---|---|
| baseline-atoms | 0.0935 | Attack/SacrificeSource → Attack/SacrificePlan | Passed |
| baseline-atoms-repeat | 0.1007 | Attack/SacrificeSource → Attack/SacrificePlan | Failed(Child) |
| atoms | 0.1089 | Attack/SacrificeSource → Attack/SacrificePlan | Failed(Child) |
| atoms-repeat | 0.1382 | UtilityFollowup/Attack/SacrificeSource → UtilityFollowup/Attack/SacrificePlan | Failed(Child) |

One original-source launch and both integrated launches exceeded 0.10 ms in the existing Sacrifice preview path. The structural threshold therefore remains open. These captures locate the overruns and leave their cause unresolved.

The separate default-tuning pure Core step capture uses the minimum of 12 warm repeats at each ordinal. It includes cases with delivery hits and wide environment sources. The table shows the longest repeatable step and the first decision’s step count.

| Fixture | P0 steps | P0 longest step (ms) | P4 steps | P4 longest step (ms) |
|---|---:|---:|---:|---:|
| N8 | 15743 | 0.0751 (Attack/SacrificePlan) | 15712 | 0.0811 (Attack/SacrificePlan) |
| A8 | 32950 | 0.0902 (Attack/SacrificePlan) | 30078 | 0.0936 (Attack/SacrificePlan) |
| W8 | 17232 | 0.1353 (MoveRoute) | 16826 | 0.1405 (MoveRoute) |
| U8 | 51944 | 0.1439 (MoveRoute) | 48424 | 0.1531 (MoveRoute) |
| T8 | 2415 | 0.0819 (Attack/SacrificePlan) | 2415 | 0.1151 (Attack/SacrificePlan) |
| H8 | 13975 | 0.0489 (Attack/SacrificePlan) | 13915 | 0.0784 (Attack/SacrificePlan) |
| X8 | 15984 | 0.0719 (Attack/SacrificePlan) | 15953 | 0.0772 (Attack/SacrificePlan) |

The longest repeated step in each pure fixture stayed in the existing Sacrifice preview or MoveRoute phase. W8 and U8 MoveRoute exceeded 0.10 ms before and after. T8 Sacrifice preview crossed 0.10 ms in the integrated sample with the same 2,415-step trace length. The 0.10 ms service assertion is reported separately above.

An individual cache miss retains the existing damage, selection, delivery or visibility iterator work. Damage and selection preparation yield separately. Delivery and visibility store their result after the underlying iterator completes. The static review found no new unbounded synchronous traversal. These measurements cover the captured Editor fixtures.

## PlayMode handoff

Use a production encounter that reproduces the visible pause and record the stage/node, seed or reproducible start, roster, tuning, graphics preset, target frame rate and hardware. Compare the same encounter before and after, including initial use and warmed enemy actions. Record timestamps, frame numbers and combat result sequence at each boundary.

1. Previous playback completion in `CombatResultPlayback.OnFinished`. Record the subsequent `AcknowledgePresented` as a separate point to capture View finalization time.
2. Preparation first returns `Ready` from `CombatAdvanceService.PrepareAdvanceSlice`. Record when the next frame observes readiness.
3. Start of `CombatPreparedAdvanceJob.Execute` and publication of the next sequence. Record retries and stale attempts that produce no publication.
4. Next `CombatResultPlayback.Begin` as presentation starts. Compute completion-to-ready, ready-to-publish, publish-to-start and the full gap, with median, P95 and maximum frame time.

Repeat on a supported lower-spec CPU with the same encounter and graphics settings when available. Use temporary profiler markers or a trace build for the boundary timestamps. Matched before/after PlayMode acceptance and lower-spec measurements are pending.

### Rendered Editor follow-up

The integrated worktree ran authored stages 1120 and 1124 in PlayMode with Main and all three subunits deployed. Four measured enemy turns ran for 14.14–21.70 s from End Turn to player input. Result playback occupied 81.6% of the combined time, and AI preparation occupied 17.1%. First enemy action playback began after 1.37–2.37 s. For those first actions, the Ready-to-publication interval was 15.46–19.82 ms and the publication-to-playback entry interval was 0.06–0.56 ms. The specific five-second report remains unmatched. Local traces and per-result measurements are in `Logs/EnemyAi/playmode-boundaries/`.

## Review and preservation

The independent static review found no concrete correctness issue in the retained P1–P3 production diff. The audit verified the unrelated worktree bytes, retained production sources, the frozen reference hashes, and matching script metadata. Phase 4 changes to test source are limited to the capture mode guards. The temporary count and recovery instrumentation was restored before the final focused smoke run. [Phase 4 evidence](2026-11-13-enemy-ai-reuse-evidence/p4-opportunities.json)
