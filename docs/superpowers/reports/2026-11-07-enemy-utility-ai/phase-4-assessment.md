# Phase 4 — Observable assessment and hazard forecast

Status: implementation and Core verification complete on 2026-11-08; stop for Phase 4 review. Authorized scope is Tasks 4.1–4.2 only. No Phase 5 implementation, gameplay activation, commits or pushes. G-atom and user Unity acceptance remain open.

Start/end checkout HEAD: `9d8c9ce456db95e8e0aff766240af47353f18f46`, branch `feature/Cheon/Core/NewCombatSytem`. Phase 0–3 changes are inherited and remain uncommitted. The Phase 3 source manifest is the preceding checkpoint; `Logs/EnemyAi/phase-4-work/source-scope.json` identifies 32 Phase 4 C# files. A null priorHash means that file was not in Phase 3's changed-file manifest, not necessarily that it is a new file.

## Applied decisions and boundaries

The user explicitly selected **public-information evaluation with conservative limits on guaranteed outcomes**. No current public contract proves hidden hazards absent. All publicly identical assessments carry HiddenEnvironment. IsPrecise means a deterministic projection of observed facts; GuaranteedTerminal remains false with any gap. Hidden mine/sandstorm presence is never inspected to improve completeness. Observable random, actual-only and unsupported chains skip precise child creation.

- CombatMovementCalculator owns RouteWork; ProjectRoute drains the same iterator. Visibility, validation, observation, entry, Recon contact and arrival yield in their existing owners.
- CombatCommandEffects owns shared CaptureSteps and effect capability composition. It checks all living field participants, including defenders/aura sources, rather than only the attacker.
- MovementEnvironmentCalculator/TornadoCalculator own environment support classification. Public native static terrain/destination modifiers are supported; unclassified callbacks are partial and not invoked for optional consequence estimation.
- CombatActionAssessment checks public legality through existing attack/route queries, charges the existing decision context once immediately before creating a precise child, and extracts capped health loss, eliminations, restriction deltas and observed endpoint. No trait activation is counted as a second damage benefit.
- EnemyDecisionContext retains actor/decision Preview counters through cancellation or fallback; exhausted optional work stays admitted and partial. No new runtime consumer is connected, so the legacy selector's caps remain inactive.
- CombatCommandComposition.ProjectSteps supplies a completed moved input without copying a movement or settlement algorithm into AI. Dead/terminal actors return no follow-up world.
- StageGimmickComposition remains the sole registration order: Tornado -> Railway -> Haboob -> Rockfall. Railway IsDue, Rockfall HealthLoss/BindEndCount and CombatTurnCalculator.EndsNightAfterEnemyPhase are shared with live execution.
- Forecast holds immutable cell/time facts and indexes. Main/target references and rule-group endpoints support fixed-occupancy terminal cutoff using the existing BattleOutcomeCalculator.Resolve contract. All direct consequences of one terminating gimmick group are retained. No live transition or fake EndEnemyPhase is run for forecasting.
- A predicted Bind plus an existing attack restriction adds UnsupportedEffect | FutureWarning for unmodelled longer exposure. Final-night recall does not invent another action opportunity.

Necessary additional Core owners were included under the user's standing authorization: CombatCommandComposition, MovementProjection, ReconSquadApplication, MovementEnvironmentCalculator, TornadoCalculator, EnemyDecisionContext and RoarEffectRule. This preserves SSOT and avoids a second rule engine. The only existing other-author test adaptation is the required empty forecast implementation on StageGimmickProgressionTests.ObservingRule; its assertions are unchanged. No View, Framework or shared Job foundation change is part of Phase 4.

The current PlayerTurnRules.PlayerTurnsPerNight is 99. New final-night tests follow that source; existing tests expecting the historical eight-turn setting are not rewritten.

## Assessment support table

This is a report of runtime owner capabilities, not another registration table.

| Owner / hook | Observed-world support | Evidence / limit |
|---|---|---|
| Ordinary attack, shared targeting/health/geometry | Precise when its composed chain is supported | Capped damage, zero-damage contact, frozen Preview comparisons |
| ArmorBreakerEffectRule | None for its own gap | Defense/status and health Preview/live parity |
| DistanceDamageScaleEffectRule | None | Distance-scaled health parity |
| TauntEffectRule | None | Defender's incoming reduction/status parity |
| SteadfastWillEffectRule | None | Defender immunity under forced movement parity; an unannotated attacker Knockback still makes that whole chain partial |
| SafeBombardmentCategoryRule | None | Cannon targeting/friendly consequence parity |
| HunterEffectRule | RandomOutcome | No precise child; seed variants retain identical assessment |
| KillChainAttackEffectRule, RoarEffectRule | ActualOnlyReaction | Public Preview omits actual reaction chains |
| Other unannotated effects, including Sacrifice/Pull/Impact/Recon/PositionSwap | UnsupportedEffect | Zero invented optional benefit; mandatory legality remains shared |
| Entrenchment/dust and other aura/trail sources | Partial unless every reachable binding/source is supported | All participants and visible environment sources are scanned |
| Held environment, equipped/temporary items | UnsupportedEffect | No blanket support declaration without operation-specific parity |
| Nonempty artifact effects/items | ActualOnlyReaction + UnsupportedEffect | Includes actual-only reflection; conservative classification |
| Obstacle reactions | UnsupportedEffect | No speculative barrel/drop/reaction gain |
| Native static terrain, destination modifier, ordinary arrival/souls | Deterministic public projection | Shared route/completion; healing yields no damage credit; weak-ground death has no follow-up input |
| Visible arbitrary tile/object callbacks, including known mines | UnsupportedEffect | Optional assessment does not invoke them; existing known-mine selection policy is unchanged |
| Public Tornado source/group contact | RandomOutcome + UnknownPosition | No RNG-backed precise endpoint |
| Hidden environment | HiddenEnvironment always | Public projection may be precise; actual-world guarantee remains partial |
| Railway endpoint at due boundary | Known fixed-occupancy loss | Crossing alone is not a death; count+1/+2 and live parity |
| Rockfall warning | Known fixed-occupancy loss/Bind | Shared floor/max-health and ended-side duration arithmetic |
| Future Rockfall warning | FutureWarning / RandomOutcome | No sampled coordinate |
| Tornado/Haboob scheduled forecast | Explicit partial global facts | Precise masks/combined prediction remain Phase 5 |

A partial result carries conservative possible allied harm. It is not evidence of safety or zero effect. Restrictions and hard admission are not inferred from this lower-detail value estimate.

## Verification ledger

- `phase-4-route-reference`: captured seven old synchronous input/Preview/actual vectors before migration, 1/1 passed. The immutable route oracle hash is `0705B7299F5746CAEDC67AE18334CF801070B53142353FDC1C4B3DAF771B296D`.
- `phase-4-route-behavior-red`: 4/5 passed; open route drained 54 presence units in one Step. `phase-4-route-green2`: 5/5 passed, including all seven frozen routes.
- `phase-4-forecast-behavior-red`: six failures demonstrated missing forecast behavior; a seventh was the obsolete eight-turn fixture. `phase-4-forecast-green3`: 21/21 route/forecast cases passed after the new test's serialization adapter was corrected. The old serializer and frozen output were not changed.
- `phase-4-assessment-behavior-red`: 5/8 passed, three failures against the placeholder Work. `phase-4-assessment-coverage-green`: 33/33 passed.
- `phase-4-distance-evidence`: five supported-owner Preview/live comparisons passed before the support annotations were added. A later defender-Taunt comparison also passed.
- `phase-4-followup-red2`: the accepted weak-ground route returned a follow-up world for a dead actor, 0/1 passed. The fix suppresses dead/terminal follow-up worlds; subsequent focused runs pass.
- `phase-4-review-red`: 0/2 passed. Main death elsewhere incorrectly left 20 known Rockfall loss; newly predicted Bind omitted the longer-horizon gap. `phase-4-review-green`: 48/48 passed after both fixes.
- `phase-4-reviewed-capture`: 50/50 passed: 49 behavioral cases plus one explicit measurement. Includes a regression ensuring Main and actor deaths in the same Railway batch both count before terminal cutoff.
- The first full reviewed CoreIntegration run exposed five new AmbiguousMatchException failures in existing TornadoGimmickPublicationTests. Name-only reflection of MovementProjection.Transition became ambiguous after adding the completed-result overload. The new adapter and its callers were renamed TransitionFromCompleted; the old method and all five existing tests were preserved. `phase-4-reflection-green` passed 5/5. Final full regressions run after this compatibility repair.
- Compiler/setup-only failures (using placement, test namespace and source patch transfer) and invalid test fixture failures are retained in their run folders. They are not behavioral RED evidence.
- Existing tests/assertions were not changed to erase baseline failures. The initial full Core run, before final review repairs, was 5,162 selected / 5,131 passed / 25 failed / 6 skipped. Final reviewed-source RC/RI results will be recorded below.

## Final regression

| Final run | Selected | Passed | Failed | Skipped |
|---|---:|---:|---:|---:|
| `phase-4-compatibility-core` | 5,165 | 5,134 | 25 | 6 |
| `phase-4-compatibility-integration` | 1,011 | 962 | 47 | 2 |
| `phase-4-reviewed-capture` | 50 | 50 | 0 | 0 |

All 49 new normal Phase 4 tests pass in final Core (26 assessment/route, 23 hazard cases). All 72 inherited failure fullnames, labels and messages match Phase 0 exactly; `Logs/EnemyAi/phase-4-work/regression-comparison.json` records zero differences. The complete suites are not entirely green. Explicit diagnostic skips are not failures or acceptance evidence.

Final Core passed both frozen Preview checks (86 original + four movement vectors), the frozen seven route vectors and all 23 decision-session tests, including the 11 fixture cases comparing 500 side rows and 55 input hashes. The oracles are unchanged: original Preview SHA-256 `021FBD5C922D7307D5194E2FD548F040B12E1A77A394D902450311EC038C1A49`; movement `B8C37AB809A42EF40F49A13DB97A179EF89C611F3EFCA03EBC839DB934050752`; route hash above; side rows `F71DF6D6DAED5F5EC98FF6CE7960F9A2F23A1DB0995D184C7B50F8516F097006`.

Both final regression manifests match all 103 currently changed C# files. The 32-file Phase 4 scope includes inherited files that received a Phase 4 addition. New files have matching metadata; no duplicate GUID was found within the scope, no Core UnityEngine import was introduced, and `git diff --check` passed. HEAD and branch are unchanged.

All Editors exited. Three Unity-generated FMOD/font assets were backed up under `Logs/EnemyAi/phase-4-work/generated-assets/` and restored only after matching the original CRLF entry hashes. Their Git status is clean. All source/tests/documents and inherited uncommitted work remain.

## Measurement and limits

Final capture: `Logs/EnemyAi/phase-4-reviewed-capture/`; summary [phase-4-measurements.json](phase-4-measurements.json). Unity 6000.0.73f1 Windows batch/nographics, Stopwatch frequency 10,000,000 Hz. This is a Core microbenchmark, not a rendered-frame, Flow or decision-quality acceptance run.

The capture manifest differs from final source only in CombatActionAssessment.cs and MovementProjection.cs: the three-line completed-adapter/caller rename described above. No measured algorithm or workload changed after capture. The final full-regression manifests are checked against current source separately.

The fixed corpus contains the 86 existing Preview inputs plus six constructible route inputs. The seventh malformed route remains covered by the frozen route test but cannot become an EnemyDecision.Move and is excluded before measurement. Per candidate, sample 0 is a first observation; ten subsequent samples measure Step work. Another ten matched synchronous TryAssess calls are measured without per-Step probes. Comparison assertions and CSV writes are outside measured calls. Construction includes the decision context; read/dispose are separately measured. The summed instrumented work is not a wall-clock decision-latency metric.

### Coverage

92 candidates: 67 admitted, 40 precise, 27 partial and 25 rejected by public legality. Precise coverage is **59.7% of admitted candidates**, or 43.5% of the full fixed corpus. N5/N6/N8/T8/R8/G16 each admit and precisely evaluate their five fixed aims. X8's five are partial. The fixed W8/U8/A8/H8 aims are not admitted; this does not claim those scenarios have no legal candidate. Among 37 supplemental cases, 32 are admitted: 10 precise and 22 partial. These figures are coverage of the fixed corpus, not a distribution of production AI choices.

### Warmed costs

| Operation | Samples | p95 | p99 | Maximum |
|---|---:|---:|---:|---:|
| Assessment construction | 920 | 0.0029 ms | 0.0051 ms | 0.0492 ms |
| Assessment Step | 254,240 | 0.0065 ms | 0.0274 ms | 12.0903 ms |
| Assessment result read | 920 | 0.0003 ms | 0.0005 ms | 0.0012 ms |
| Assessment Dispose | 920 | 0.0001 ms | 0.0001 ms | 0.0001 ms |
| Summed instrumented assessment work | 920 | 2.3744 ms | 2.6297 ms | 16.5342 ms |
| Uninstrumented whole TryAssess | 920 | 2.2127 ms | 2.3715 ms | 4.3459 ms |
| R8 forecast Step | 3,700 | 0.0022 ms | 0.0042 ms | 0.0620 ms |
| R8 complete forecast work sum | 100 | 0.0409 ms | 0.1156 ms | 0.1506 ms |
| R8 endpoint lookup | 12,800 | 0.0034 ms | 0.0320 ms | 11.5026 ms |

R8 uses seeds 1/7/19/41/97, twenty measured forecast builds and 128 endpoint lookups per build. The lookup includes fixed-occupancy Main/objective terminal checks. The byte-allocation calibration again returned zero for a 4,096-byte allocation; bytes are **unavailable**, not zero (raw rows use -1).

### Overrun disposition

G-atom remains **unmet**. Five warmed assessment Steps exceeded 1 ms:

| Fixture / repeat / WorkVersion | Phase label at entry | Time | Collection delta | Same-work median |
|---|---|---:|---:|---:|
| N5-97 / 6 / 11 | Visibility | 4.6036 ms | 0 | 0.0003 ms |
| N5-97 / 6 / 35 | EffectCoverage | 7.4889 ms | 0 | 0.0005 ms |
| R8-1 / 9 / 263 | Attack/ForcedArrivals | 1.2558 ms | 0 | 0.0009 ms |
| R8-1 / 10 / 379 | Attack/CompletionHunterObservation | 12.0903 ms | 3 | 0.0006 ms |
| swap / 4 / 9 | EffectCoverage | 3.1209 ms | 0 | 0.0061 ms |

One warmed forecast lookup exceeded 1 ms: seed 1 / repeat 6 / actor 14 / X=10, 11.5026 ms, collection delta 3; same-work median 0.0020 ms. Lookup X is reconstructed from the harness's fixed increasing-X order. A coincident collection is evidence, not proof of exclusive cause. The four no-collection assessment overruns are not explained by this probe; no scheduler/GC/JIT cause is asserted.

Seven first-observation assessment Steps exceeded 1 ms (maximum 3.9746 ms), and the first forecast had two Step and two lookup overruns (maximum Step 3.5582 ms). All have matched warmed repetitions in the summary. First observation is not guaranteed to be a completely cold process because earlier fixtures run in the same Editor.

These overruns, unsupported allocations and the inherited Phase 2/3 first-use/atom issues keep activation gated. No numerical acceptance is inferred from low medians or from skipping partial chains. Further attribution should begin with the exact source manifests, WorkVersions and raw rows, without recapturing or replacing the frozen semantic oracles.

## Independent review and locality

One fresh read-only review found two Important issues and no minors. Both were reproduced in `phase-4-review-red` and fixed in the same pass. No second reviewer was dispatched.

The review explicitly did not judge Phase 5+ precise Tornado/Haboob and Utility preference quality, normal-game/View acceptance, pending final regression/performance, or inherited Phase 0–3 changes. Those remain separately scoped or gated; none is silently accepted.

| Future change | Actual modification / understanding boundary | Necessary verification |
|---|---|---|
| Declare another effect deterministic | Its owner capability + representative Preview/live consequences; assessment consumes the flag | Transitive coverage, privacy/RNG, matched cost, shared Core/Integration regression |
| Change railway timing or Rockfall loss/Bind | Existing gimmick owner/spec; forecast calls the shared function | Live/forecast parity, terminal grouping, horizon and registry ordering |
| Change optional Preview limits | EnemyDecisionSettings/context and budget tests | No-refund/exhaustion, legacy cap inactivity, Phase 5/6 consumer tests when connected |
| Change movement arrival/composition | Existing route/projection/composition owners, preserving mutation/observation order | Frozen seven routes, frozen 90 Preview vectors, cancellation/boundedness, RC/RI |

Splitting files is not the claimed improvement. Rule implementations remain authoritative, while assessment callers need the capability/result contracts rather than unrelated trait internals. The shared nature of route, projection and effect capture still requires full Core/Integration regressions.

## Remaining acceptance and next entry

Normal gameplay remains immediate Advance. No Utility ranking, shortlist, Phase 5 masks or Phase 6 activation was added. Phase 3 user Unity F1–F8 remain pending; **컴파일 미확인** applies to the user's View/Flow compilation and playback acceptance. Core batch results do not replace those checks.

Phase 4 is stopped for user review. Phase 5 requires a new user instruction. Performance attribution and allocation/live-frame acceptance remain open; they are not waived by this implementation checkpoint.
