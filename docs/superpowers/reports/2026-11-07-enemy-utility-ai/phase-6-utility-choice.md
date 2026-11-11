# Phase 6 bounded Utility choice

The user authorized Phase 6 on 2026-11-08. Work continued in the existing uncommitted `feature/Cheon/Core/NewCombatSytem` checkout at `9d8c9ce456db95e8e0aff766240af47353f18f46`. The Phase 5 compatibility surface and all existing changes were preserved. Phase 6 covers Tasks 6.1 and 6.2. Phase 7 is a separate review checkpoint.

## Choice, execution and ownership

- `EnemyUtilityScorer` applies the locked checked integer weights. Health loss is capped and normalized per victim. Trait activation adds no separate damage credit. Engagement uses an admitted pursuit prefix, with the score contribution capped at 100 points in either direction. A projected follow-up contributes half its positive immediate benefit.
- One Utility session builds the shared hazard forecast and threat map per source, visits actors in ascending EntityId and compares each legal candidate with that actor's Hold. Equal action scores use Attack before Move, followed by stable aim or endpoint and full-route order. A zero-gain tie Holds. Hold has no public action or state mutation. Mandatory Taunt filters out untagged candidates before ranking.
- The shortlist stays within four attacks and four moves per actor. At most two move routes can start precise evaluation. A deterministic surviving move can nominate one follow-up aim for at most one precise evaluation. Rejected and abandoned children remain charged. The same allowance is bounded by eight per actor and sixteen per decision. When precise admission is unavailable, the candidate retains supported coarse facts and coverage.
- Supported precise results use projected health, position and restriction facts. Coarse attacks use shared delivery and capped pre-protection loss. Observable unresolved effects retain and score the reported possible allied-loss upper bound. Budget exhaustion by itself retains coarse delivery. Movement contact uses visible arrival sources and forecast bounds, and published thorn damage is a pre-protection estimate. Exact movement continues to use the existing route owner. A rejected attack closes that actor's attack family. A rejected move advances to the next actor. The next accepted action starts a new decision from the actual source.
- The default runtime still calls `CombatCommandCalculator.Advance` through the immediate compatibility route while activation is gated. Explicit diagnostic `AdvanceUtility` drains the same `EnemyDecisionSession` that existing prepared composition advances incrementally. Both use the existing execution, reaction and publication owners. Existing Phase 2 session regression tests explicitly select immediate compatibility so their frozen expectations retain the original scope. The compatibility path can be removed after the gameplay gate approves migration. No public gameplay toggle, duplicate publisher, View change or Framework/Job foundation change was added.

The scorer consumes facts through the assessment, route and forecast owners. Changes to combat consequences still require the existing movement, Preview and publication regressions. Changes to preference weights touch the scorer and fixed score vectors, with no changes to the movement, damage or Buffer contracts.

## Coverage decisions

- The Phase 4 public-information contract always retains HiddenEnvironment. An observed terminal outcome cannot claim a guaranteed terminal tier or erase later possible hazard and threat. The V4 plan vector assumes complete terminal coverage. That assumption cannot be constructed through the current production source. A score-level case verifies that certified player Defeat outranks safe Hold even with sacrifice, and the Core scenarios verify both conservative rail escape under incomplete terminal projection and actual accepted main death before a later gimmick.
- The movement-assessment projection preserves both public wall owners when constructing a deterministic moved input. The existing Preview and route behavior remains the default for previous callers. Direct threat uses the greatest exposure across the actor's footprint. Future player movement and unmodelled chains remain partial.
- The independent read-only review found three issues. The final implementation retains future exposure under incomplete terminal results, consumes the partial allied-harm upper bound and skips the actor after an actual move rejection. The partial-loss assertion reproduced a score of +500 before the correction and passed with -2,500 afterward.

## Verification records

| Run | Selected | Passed | Failed | Evidence |
|---|---:|---:|---:|---|
| `phase-6-score-red` | 1 | 0 | 1 | Missing Utility scorer entry |
| `phase-6-rail-red-score-green` | 10 | 7 | 3 | Score tests pass. Old policy chooses Attack in the due-rail vector at each partition |
| `phase-6-utility-second` | 82 | 77 | 5 | Found public wall propagation, a fixture ownership gap and a legacy expectation running the new policy |
| `phase-6-utility-focused` | 125 | 125 | 0 | Core run before final review fixes |
| `phase-6-utility-publication` | 62 | 62 | 0 | Integration run before final review fixes |
| `phase-6-review-red` | 4 | 3 | 1 | Retained possible allied damage was missing from the score |
| `phase-6-verified-focused` | 134 | 134 | 0 | Final scorer, decision, budget, assessment, candidates, threat and forecast cases |
| `phase-6-verified-publication` | 62 | 62 | 0 | Final synchronous/prepared publication, source stamp, cleanup and default-route coverage |

The compiler-only run `phase-6-utility-first` exposed a missing namespace import and contributed no behavior evidence. The final focused runs include actual rail escape, Hold without mutation, finite rejection fallback, shared follow-up charging, seed/cursor and partition stability, mandatory Taunt after budget exhaustion, and actual replan from a moved position. Integration compares one result and before/after Buffer contents under three fake frame budgets, preserves the pending View command and checks cancellation or staleness after selection.

## Full regression and performance

| Assembly | Selected | Passed | Failed | Skipped |
|---|---:|---:|---:|---:|
| ProjectL.Tests.Core | 5,228 | 5,196 | 25 | 7 |
| ProjectL.Tests.CoreIntegration | 1,016 | 967 | 47 | 2 |

All 72 failed full names, labels and messages match Phase 0 exactly. Phase 6 adds 28 passing Core cases and five passing CoreIntegration cases over Phase 5. The frozen 86 attack and four movement Preview vectors, seven route vectors and 500 baseline decision rows under 55 hashes passed. No frozen oracle was changed. The comparison is in `Logs/EnemyAi/phase-6-verified-integration/regression-comparison.json`.

### Step capture

`phase-6-verified-atoms` passed and retained all eleven fixtures at seed 1 with one first observation, three warmups and twelve timed repetitions. The capture includes construction, each Step, and disposal. There are 1,357,260 warmed Steps. The warmed p95 is 0.0072 ms and p99 is 0.0158 ms. Thirty-seven warmed Steps exceed 0.5 ms and eleven exceed 1 ms. The largest is W8 sample 9, work version 5020, UtilityCandidates, at 15.6029 ms with one recorded collection. Three of the eleven warmed overruns coincide with a collection. The first observations have 117 Steps above 1 ms. The largest is N5's Attack/SacrificePlan at 27.7617 ms. The harness does not establish the cause of these events.

**G-atom remains unmet.** This is an isolated Core capture. Real-frame/User Flow F1–F8 and exact allocation remain open. The 4 KiB Mono calibration returned zero, so even the score-only sample reports unavailable bytes.

### Whole-decision corpus

The first corpus attempt `phase-6-verified-corpus` exhausted the diagnostic marker buffer at U8/1. It is a failed capture. The first retry `phase-6-corpus-complete` failed to compile due to the capture switch also reaching a separate measurement helper. The retry applies the switch only to the whole-decision Utility corpus. That path disables nested marker recording and retains independent per-Step evidence above.

`phase-6-corpus-retry` passed the explicit capture. All 55 frozen input hashes matched. It recorded 55 first observations, 1,100 warmed fresh decisions, and 220 independently reset full enemy sides. Each fixture uses five seeds, ten warmups and twenty timed decisions per seed, with twenty timed sides per fixture across seeds. Decision and side columns below measure accumulated Core time, including actual calculation for the selected action. They are separate from per-Step timings and do not measure a rendered frame. The first-observation decision maximum was 366.2193 ms.

| Fixture | Warm decision p95 | Warm decision max | Enemy side p95 | Max precise calls | First decision |
|---|---:|---:|---:|---:|---|
| N5 | 19.6168 ms | 65.9145 ms | 57.0729 ms | 6 | Attack |
| N6 | 13.6239 ms | 21.5270 ms | 82.9659 ms | 6 | Attack |
| N8 | 29.7369 ms | 44.2848 ms | 49.1569 ms | 6 | Attack |
| W8 | 51.2490 ms | 56.4197 ms | 239.1944 ms | 2 | Move |
| U8 | 97.6948 ms | 100.5385 ms | 98.1522 ms | 14 | End |
| A8 | 28.9365 ms | 45.0146 ms | 21.2139 ms | 0 | End |
| T8 | 19.8371 ms | 33.6426 ms | 54.3049 ms | 1 | Attack |
| R8 | 10.7852 ms | 37.9888 ms | 91.2768 ms | 6 | Move |
| X8 | 44.7390 ms | 73.6598 ms | 62.5711 ms | 0 | End |
| H8 | 9.9592 ms | 32.9622 ms | 7.3074 ms | 0 | End |
| G16 | 13.7379 ms | 37.8718 ms | 142.4706 ms | 6 | Attack |
| All eleven | 61.4819 ms | 100.5385 ms | 227.4243 ms | 14 | 500 Attack, 200 Move, 400 End across 1,100 samples |

The original ten excluding N5 had a decision p95 of 61.7939 ms, p99 of 95.0509 ms and side p95 of 229.2164 ms. N5 is the five-enemy reference. U8 spent fourteen precise evaluations and finished one End, so optional Preview exhaustion does not explain that result. No cap hit was recorded in the corpus. The separate lowered-budget and hard-cap cases exercised exhaustion.

Twenty of 55 first intents changed. R8 switched from Attack to a legal escape Move under all five seeds, which is the intended preference. A8 and X8 switched from Attack to End, and H8 switched from Move to End, each under all five seeds. These are observed consequences of conservative partial coverage. They remain behavioral acceptance concerns because unrelated visible unsupported effects can keep the conservative bound broad enough to prefer Hold. The evidence neither establishes no legal action in these fixtures nor approves their gameplay behavior. The other 35 first intents match. Exact trace keys and raw data are retained in `Logs/EnemyAi/phase-6-corpus-retry/`, including `phase-6-summary.json`. No old regression assertions were rewritten for the new preferences.

The final corpus source manifest matches all 115 changed C# files. Compared with Phase 5, Phase 6 touched 19 C# files, four of them new with matching Unity metadata. The corpus retry differs from the verified Core, Integration and Step runs only in `EnemyAiPerformanceMeasurements.cs`, which disables the nested marker buffer for this capture. All production source hashes are identical. The three Unity-generated FMOD/font files were verified, backed up and restored after the Editor exited. Backup hashes are under `Logs/EnemyAi/phase-6-verified-integration/generated-assets/`.

Phase 6 development stops here. Default gameplay remains immediate Advance. G-atom, exact allocation, real-scene profiling and user Unity compilation/playback F1–F8 remain open. Phase 7 requires the user's next instruction.

