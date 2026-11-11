# Phase 7 acceptance and locality review

**Item 1 follow-up.** The [candidate scope remediation report](phase-7-allied-exposure-remediation.md) contains the subsequent A8/X8/H8 correction and regression. The measurements and Hold diagnosis below remain the original Phase 7 evidence.

The user authorized the Phase 7 acceptance review on 2026-11-08. Work continued on `feature/Cheon/Core/NewCombatSytem` at `9d8c9ce456db95e8e0aff766240af47353f18f46`. The scope covers measurement, behavior evidence, locality and handoff. The production source remains at the Phase 6 hashes and the runtime uses immediate Advance. The only source additions and changes in this review are test diagnostics.

## Method and retained evidence

Unity 6000.0.73f1, Windows, EditMode batch, fixed input corpus and sequential assemblies. `phase-7-corpus` records 55 first observations, 1,100 warmed decisions and 220 reset sides under the same 55 frozen input hashes. Each fixture used five seeds, ten warmups and twenty timed decisions per seed. Each fixture has twenty full sides across seeds. Decision timings include actual calculation for the selected action. Full side timing is summed Advance work. Both are elapsed Stopwatch time and the service diagnostic below measures a different boundary.

| Fixture | Warm decision p95 | p99 | Maximum | Warm side p95 | Maximum precise calls |
|---|---:|---:|---:|---:|---:|
| N5 | 19.7264 ms | 31.2255 ms | 48.7410 ms | 64.5936 ms | 6 |
| N6 | 9.0366 ms | 9.8594 ms | 15.2112 ms | 71.5388 ms | 6 |
| N8 | 7.5684 ms | 8.3219 ms | 8.7217 ms | 92.9375 ms | 6 |
| W8 | 65.5882 ms | 113.5416 ms | 124.6652 ms | 243.1047 ms | 2 |
| U8 | 101.3138 ms | 178.5265 ms | 301.2020 ms | 102.6987 ms | 14 |
| A8 | 9.6424 ms | 12.3639 ms | 39.8471 ms | 8.7788 ms | 0 |
| T8 | 24.7747 ms | 27.7857 ms | 28.5401 ms | 93.7268 ms | 1 |
| R8 | 22.3464 ms | 26.1189 ms | 27.8391 ms | 101.7749 ms | 6 |
| X8 | 23.1423 ms | 45.0925 ms | 50.2766 ms | 22.9816 ms | 0 |
| H8 | 14.1884 ms | 29.6451 ms | 30.0690 ms | 30.5152 ms | 0 |
| G16 | 10.5064 ms | 21.7719 ms | 29.9965 ms | 143.5569 ms | 6 |
| All eleven | 61.7181 ms | 101.2250 ms | 301.2020 ms | 221.0358 ms | 14 |

The original ten excluding N5 had decision p95 62.0707 ms, p99 101.3138 ms and side p95 226.8742 ms. The five-enemy N5 is the standard reference. N6/N8 and G16 are stress sizes. Across the warmed matrix the first intent distribution was 500 attacks, 200 moves and 400 side ends, with zero cap hits. U8 finished the side and spent fourteen precise evaluations, below the decision cap. `phase-7-corpus/phase-7-summary.json` contains the complete per-fixture and first-intent comparison. The score-only allocation attempt sampled 20,000 calls. Its 4 KiB calibration returned zero, so bytes are unavailable.

Compared with Phase 6, the frozen input export and the Utility outcome trace are byte-identical. There are no rejections in the 1,100 warmed first decisions. The largest work totals are U8 at 45,564 Steps and 1,835 expanded nodes, and W8 at 16,127 Steps and 974 nodes. N5 reaches 4,348 Steps with eleven nodes. Per-decision candidate, work, rejection and cap counters are in `phase-7-corpus/decisions.csv`.

## Step timing and attribution

`phase-7-atoms` sampled eleven fixtures at seed 1 with one first observation, three warmups and twelve measured repetitions. Among 1,357,260 warmed Steps, p95 was 0.0069 ms, p99 0.0160 ms and twelve exceeded 1 ms. The largest was 20.6024 ms on W8, EnvironmentCoverage, work version 12121, with one recorded collection. Three warmed overruns coincided with a recorded collection. Warmed constructor and disposal maxima were 0.0160 ms and 0.0014 ms. The first observations included 116 Steps above 1 ms, with a 46.8446 ms maximum at N5, Attack/SacrificePlan, version 1652, with zero recorded collections. The same first-observation boundary reached 27.7617 ms in Phase 6. These runs reproduce the expensive boundary and leave the exact runtime cause unconfirmed.

Matched warm repetitions distinguish the two peaks. W8 5020 reached 15.6029 ms in Phase 6 and 0.0010 ms at most in Phase 7. W8 12121 reached 0.1370 ms at most in Phase 6 and 20.6024 ms in Phase 7. Their median times were 0.0004 ms in both runs and 0.0738/0.0781 ms respectively. U8 MoveRoute 28888 reached 3.4330 ms without a recorded collection and had matching medians around 0.33 ms. The recurring N5 first-use point at version 1652 had warmed medians 0.1017/0.1528 ms. The recorded phase label comes from nested cursors and identifies the nearby Step boundary. It does not attribute all elapsed time to the named inner method. All Phase 6 and Phase 7 rows above 1 ms and their matched repetitions are in `phase-7-atoms/matched-overruns.json`.

## Behavior diagnosis

Across all fifteen A8/X8/H8 fixture-seed inputs, the first intent was EndEnemyPhase and zero precise previews started. The diagnostic evaluated every retained, admitted candidate against the actual scorer, then calculated each intent in isolation with `AdvancePrepared` against the frozen source. No candidate was published.

| Fixture | Admitted and accepted by calculation | Attack / move | Would beat Hold without the broad upper bound | Of those, no actual immediate allied HP loss |
|---|---:|---:|---:|---:|
| A8 | 195 | 35 / 160 | 105 | 105 |
| X8 | 220 | 60 / 160 | 180 | 180 |
| H8 | 175 | 15 / 160 | 60 | 60 |

The score-only comparison removes just the broad allied-loss upper bound and retains the other recorded terms, including hazard and threat. It identifies the deciding term. The actual calculations describe the fixed test state and do not certify future or hidden outcomes in gameplay. Among those 345 candidates, 110 were attacks with positive opponent damage and zero immediate allied HP loss.

Every admitted candidate received an allied-loss upper bound of 8,000 permille, equivalent to the full current HP of eight allied units. `CombatActionAssessment.Work.Run` builds this bound over all living allied units, then merges world-level effect and environment gaps before admitting optional precise work. `EnemyUtilityScorer.AttackBenefit` charges -3 per permille against candidates that carry the unresolved upper bound. Hold has no corresponding whole-side term. In A8 seed 1, actor 11's attack on (4,0) was accepted, dealt 10 opposing HP and lost 0 allied HP. Its observed Hold score was -100, attack score -24,000, and attack score 0 with only the broad upper bound removed.

- A8 carries UnsupportedEffect from effect coverage. Its PiercingFalloff owner is unclassified for assessment.
- X8 carries RandomOutcome and UnknownPosition from the public Tornado environment coverage. Its phase and actual hazard uncertainty still belong in the positional forecast.
- H8 carries UnsupportedEffect from the visible MineSourceFact. The environment coverage visits the whole visible source, including tiles outside an admitted path.

These observations confirm over-conservative choice under the current scope. They do not justify discarding the upper bound for genuinely unresolved allied effects. Each all-Hold decision ends the side once in the measured traces. It does not loop within the side.

The final explicit diagnostic passed 55 inputs across slice widths 1, 2 and 31, with 355 accepted action or boundary settlements per schedule. Ordered intent, outcome traces and final observed facts agree. The 55 public-identical inputs with changed item and environment RNG cursors retained the same first intent. Earlier attempts failed separately, once at compilation on an internal visibility API and once at diagnostic formatting after a dead actor had no anchor. Neither attempt is counted as a pass. Raw score terms and trace hashes are under `phase-7-behavior-verified/`.

## Service and regression

The `phase-7-service` yielded EditMode service capture ran on Windows 11 with an AMD Ryzen 7 6800H and a 1 ms service budget. It ran 20 warmup sides and 80 measured sides across N5/N6/N8/G16. All 600 published results, including 480 measured results, matched the synchronous Utility oracle and final Buffer values. No measured stale, cancelled or retry disposition occurred in these open fixtures. Those paths are covered by separate integration tests. The measured rows recorded 1,413,384 work units, a maximum of six charged precise evaluations and no cap hits.

| Fixture | Measured updates | Preparation work per update p95 / p99 / max | Largest atom | Decision latency p95 | Side wall time p95 |
|---|---:|---|---:|---:|---:|
| N5 | 871 | 1.0715 / 1.2870 / 13.5174 ms | 13.2407 ms | 20.8292 ms | 99.6057 ms |
| N6 | 715 | 1.0505 / 1.1077 / 13.7812 ms | 12.8979 ms | 10.7065 ms | 50.1013 ms |
| N8 | 783 | 1.0670 / 1.1473 / 3.7636 ms | 3.1186 ms | 13.8065 ms | 50.9293 ms |
| G16 | 905 | 1.0601 / 1.1950 / 12.1183 ms | 11.5198 ms | 18.4908 ms | 93.5705 ms |
| All | 3,274 | 1.0635 / 1.1526 / 13.7812 ms | 13.2407 ms | 18.1789 ms | 97.6905 ms |

Five measured service updates exceeded 4 ms and sixteen preparation atoms exceeded 1 ms. All five updates above 4 ms overlap a collection. The data does not establish sole cause. The slowest update is N6 seed 41, repeat 1, update 52, decision 5 at 13.7812 ms, with a 12.8979 ms atom. Timed service work includes Tick, validation, capture, preparation, repeated status reads, cleanup and pre-publication assembly. Actual action calculation and publication are separate, with p95 2.8199 ms and max 18.9495 ms. This diagnostic does not include rendered updates, actual Flow Job scheduling, animation or View presentation. Raw rows and `prepared-summary.json` are under `phase-7-service/`.

| Assembly | Selected | Passed | Failed | Skipped |
|---|---:|---:|---:|---:|
| ProjectL.Tests.Core | 5,229 | 5,196 | 25 | 8 |
| ProjectL.Tests.CoreIntegration | 1,016 | 967 | 47 | 2 |

All 72 failed full names, labels and messages match Phase 0. The additional Core skip is the explicit Phase 7 diagnostic, which passed separately. Frozen attack and movement Preview, routes and immediate policy baseline checks passed. The exact comparison is in `phase-7-integration/regression-comparison.json`. Compared with the 115-file Phase 6 changed-source manifest, this review adds one C# diagnostic and edits two CoreIntegration measurement files. No production C# hash changed. Earlier Phase 7 run manifests differ from the final one only in the diagnostic's output formatting. The final behavior manifest matches all 116 changed C# files. The metadata audit checked 45 untracked scripts in nine containing folders, including the one new Phase 7 script. Existing tracked metadata is unchanged. Verified Unity-generated FMOD/font changes were backed up and restored under the integration and final behavior run folders.

## Locality and handoff

| Representative change | Concrete modification and understanding scope | Required regression |
|---|---|---|
| Survival or aggression weights | `EnemyUtilityScorer.Score`, `Rank` and its fixed numeric and behavior cases. These consume capped facts, one Hold baseline and hard-rule admission. The confirmed Hold issue is upstream in `CombatActionAssessment`, not a weight selection. | Numeric vector, legal escape and Taunt priority, cap accounting and a matched timing sample. |
| Railway timing or rockfall loss/Bind | `RailwayGimmickRule.IsDue` is shared by forecast and live settlement. `RockfallGimmickRule.HealthLoss` and `BindEndCount` are used by live settlement and by `StageHazardForecast`. `StageGimmickComposition` owns Tornado, Railway, Haboob, Rockfall order. Read the affected spec/state and shared boundary count. | Forecast/live parity, compound order, terminal cut-off, and actual publication. |
| Slice budget or freshness dependency | Budget is owned by `CombatAdvanceService.PrepareAdvanceSlice` and `BudgetSpent`. Freshness follows `CombatInputAssembler.AssembleDecision` and `IsCurrent` with `EnemyDecisionSourceStamp`. `CombatProgressReader` and the existing Plan own scheduling, including post-publication progression. | Slice determinism, unchanged Buffer during preparation, staleness/cancellation and one publication. Rendered Flow is still required. |

The concrete registration chain is `InGameCoreSystemFactory.Create` → `CombatSystemFactory.Register` → the local `CombatAdvanceService.ExportServices` registration → `InGameCoreSystemGroup.CombatAdvance` → `CombatProgressReader` and `CombatStepContext` → `CombatPlan` → `CombatPrepareAdvanceJob` and `CombatPreparedAdvanceJob`. Both Jobs keep one-shot completion. The publisher and existing transaction own authoritative changes, and preview state remains local. The three pure Core assemblies use `noEngineReferences` and a source scan found no UnityEngine or UnityEditor references in their C# code. The Systems service uses the Unity frame clock at its existing platform boundary. The factory's default composition remains immediate Advance.

## Acceptance disposition

| Gate | Disposition |
|---|---|
| G-baseline | Current-source Core traces and timings are complete. The historical stage 1124/1122 Profiler captures remain before evidence. A matched prepared-Utility real-scene after capture is NotRun. |
| G-atom | Failed. Twelve warmed Core Steps and sixteen service preparation atoms exceeded 1 ms. First-observation and collection-associated tails remain open. |
| G-frame | Diagnostic failed the spike criterion. Aggregate preparation percentiles are within target in the yielded service, with five unexplained updates over 4 ms. Rendered gameplay and Flow Job totals are NotRun. |
| G-latency | Core and yielded service diagnostic percentiles are within the numeric targets for the measured fixture groups. Real-scene latency and presentation are NotRun. |
| G-allocation | Unavailable. Both score and service allocation calibration returned zero for a 4 KiB probe. |
| G-correctness | Core regressions retain baseline failures and support legal rail escape. The confirmed overly broad Hold behavior and user Flow checks keep gameplay acceptance open. |
| G-isolation | Core and service diagnostic checks passed for the covered inputs, including slice parity, pending command isolation, staleness and single publication. Manual cancellation and presentation remain NotRun. |

**Manual acceptance F1–F8 is Pending.** User Unity compilation and gameplay playback are unverified. F1's standard case uses five enemies, and larger encounters are stress cases. The existing Phase 0 stage captures cannot stand in for a current matched before/after run.

**Phase 7 acceptance remains unmet.** Leave normal gameplay on immediate Advance. Two bounded remediation scopes follow from the evidence.

1. In `CombatActionAssessment`, preserve visible-world gaps and make the possible allied harm follow the relevant candidate chain. Use coverage from the existing effect and environment owners. A8's deterministic PiercingFalloff can be evaluated for explicit coverage with Preview/live parity. For X8/H8, keep Tornado uncertainty in the forecast and distinguish entry contact along a route from a visible source elsewhere. Use the same 15 fixture-seed rows to confirm useful legal actions can beat Hold while genuine allied risk remains penalized. This does not require changing score weights, taunt or the publisher.
2. Attribute the repeatable N5 first-use boundary inside `AttackDirectDamageCalculator` and `SacrificeDamagePlanner` with nested timing before changing computation. For W8/U8, match the surrounding `CombatActionAssessment` Visibility/EnvironmentCoverage and route work with a calibrated allocation profiler, then measure the actual service and rendered Flow totals on the same workload. The broad outer phase names and collection overlaps are insufficient to choose a code optimization.

These proposals are review items. Phase 7 authorizes acceptance evidence and handoff. Any production remediation needs a separate instruction.

