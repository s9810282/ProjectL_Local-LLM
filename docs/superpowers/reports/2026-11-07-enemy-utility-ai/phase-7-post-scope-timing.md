# Phase 7 timing remeasurement after candidate scoping

The [item 2 performance investigation](phase-7-item2-performance.md) records the later owner attribution, route normalization change and calibrated Unity allocation measurements. This report retains the pre-change timing baseline.

Measured on 2026-11-08 using Unity 6000.0.73f1 on Windows 11, AMD Ryzen 7 6800H. All three explicit captures passed and exited with code 0. Their manifests match the 118 changed C# hashes in `phase-7-scope-integration` and the current checkout at `9d8c9ce456db95e8e0aff766240af47353f18f46`. This follow-up changes documentation and retained measurement evidence. The code measured already contains item 1.

## Core Step timing

The existing `EnemyDecisionStepMeasurements.CaptureDecisionSteps` fixture runs eleven frozen encounters at seed 1. Each fresh Editor samples the first observation, drains three warmups, then samples twelve repetitions per fixture. Stopwatch frequency is 10,000,000 Hz. Session construction and disposal are timed separately. Internal work, including result sealing, is measured within its owning Step.

| Editor run | Warmed Steps | p95 | p99 | Maximum | Above 0.5 ms | Above 1 ms | First-observation Step maximum |
|---|---:|---:|---:|---:|---:|---:|---:|
| Phase 7 before scope | 1,357,260 | 0.0069 ms | 0.0160 ms | 20.6024 ms | 34 | 12 | 46.8446 ms |
| After scope | 1,259,436 | 0.0081 ms | 0.0198 ms | 20.8947 ms | 71 | 19 | 27.9104 ms |
| After scope, fresh Editor repeat | 1,259,436 | 0.0077 ms | 0.0183 ms | 19.7252 ms | 62 | 13 | 27.7476 ms |

Work counts can change after candidate scoping because coverage and selection changed. The frozen encounter inputs and harness parameters are matched. Neither new run recorded a constructor or disposal over 1 ms. Five warmed Step overruns in each new run coincide with a recorded collection.

- **N5 first use.** The first observation at work version 1644, labelled `Attack/SacrificePlan`, took 27.9104 ms and 27.7476 ms in separate fresh Editors. Neither row recorded a collection. The earlier source's corresponding first phase label took 46.8446 ms at version 1652. Labels are published by nested cursors and locate a boundary. They do not isolate one method or establish a JIT cause.
- **U8 warmed repeat.** Work version 17043, `MoveRoute`, exceeded 1 ms in both current-source runs. The first run recorded 4.9721 ms. The repeat recorded 4.9877 and 1.1533 ms. All three had zero recorded collections. The warmed medians at this boundary were 0.3778 and 0.3245 ms.
- **Largest warmed Steps.** U8 `MoveRoute` reached 20.8947 ms at version 12993 and 19.7252 ms at version 17262 in the two new runs. Both rows overlap a collection and occurred at different work versions. Across 29 warmed overrun keys in the two runs, 17043 was the only identical fixture, operation, version and label that exceeded 1 ms in both.

Raw rows and the comparison are in `Logs/EnemyAi/phase-7-post-scope-atoms/` and `Logs/EnemyAi/phase-7-post-scope-atoms-repeat/`. Read `post-scope-atoms-summary.json` for matched work versions, per-fixture distributions, collection deltas and thread cycle observations. **G-atom remains unmet.**

## Yielded preparation service

`EnemyAdvancePreparationMeasurements.CaptureYieldedOpenEncounters` ran N5, N6, N8 and G16 with five seeds, 20 warmup sides and 80 measured sides. All 600 service publications, including 480 measured publications, matched the synchronous Utility oracle. The recorded actor and command-kind rows also match the earlier Phase 7 service capture. The measured clock includes service Tick, source validation, assembly, repeated readiness checks and preparation. Actual execution and publication markers are accounted separately. This is an EditMode yielded update diagnostic, with rendered gameplay and actual Flow orchestration outside the sampled boundary.

| Run | Measured updates | Preparation p95 | p99 | Maximum | Updates above 4 ms | Preparation atoms above 1 ms |
|---|---:|---:|---:|---:|---:|---:|
| Phase 7 before scope | 3,274 | 1.0635 ms | 1.1526 ms | 13.7812 ms | 5 | 16 |
| After scope | 3,415 | 1.0739 ms | 1.1888 ms | 16.0081 ms | 6 | 13 |

Preparation-only call updates were p95 1.0827 ms and p99 1.2013 ms. Actual action calculation and publication had p95 3.4459 ms and a maximum of 9.8982 ms. The largest service preparation atom was 15.7609 ms on N5 seed 41, repeat 1, decision 3, update 65. That update took 16.0081 ms and recorded a collection. Five of six updates above 4 ms overlapped a collection. N6 seed 7, repeat 3, decision 1, update 3 took 4.0857 ms with no recorded collection. One N8 execution update also exceeded 4 ms in the residual preparation accounting, with 15.2232 ms after removing the execution markers. The prepare-call atom timer does not report a Step for that update.

The aggregate preparation percentiles meet their numeric targets. **The preparation spike criterion remains unmet.** Raw timings are under `Logs/EnemyAi/phase-7-post-scope-service/`, including `prepared-summary.json`. All three captures returned zero for the 4 KiB allocation probe. Exact bytes are unavailable. The FMOD and font files generated during the Editor runs were hash-checked, backed up and restored.

For the remaining item 2 investigation, the reproduced boundaries are N5 first-use work version 1644 near `AttackDirectDamageCalculator` and `SacrificeDamagePlanner`, and U8 work version 17043 within movement RouteWork. The next attribution sample needs the owning leaf timing inside those boundaries. Warm spikes that move between Steps also need a calibrated allocation and collection capture. These timing runs do not certify actual gameplay frames or user Flow F1–F8.
