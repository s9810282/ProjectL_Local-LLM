# Phase 3 — Source validity and frame preparation

Status: **Phase 3 implementation and Core verification complete; review stop, user Unity acceptance Pending.** The user explicitly requested Phase 3 execution in this chat on 2026-11-07. Tasks 3.1–3.3 were implemented. Phase 4 and gameplay activation are not included. Default game composition remains on immediate Advance; the prepared route requires explicit diagnostic/test composition.

The existing uncommitted feature checkout and Phase 2 oracles are preserved. No commit or push is planned. Framework and Job execution foundations remain unchanged. New tests belong to CoreIntegration; no View/Flow tests or MSBuild are added.

## Source dependency audit

`CombatInputAssembler` remains the source-location owner. `EnemyDecisionSourceStamp` retains immutable sources and scalar values; it does not own another world or add revisions to Buffer stores. Capture occurs before assembly, validation occurs before and after assembly, and inconsistent capture returns no input/stamp. Configuration/data errors still throw.

| Assembled dependency | Existing source / validation | Reason for invalidation |
|---|---|---|
| Battle lifetime | HasBattle, BattleInstanceId, Completion | An ended or replaced battle cannot use prior work |
| Command state, phase, controller sequence, RNG, geometry, environment, gimmicks, structures | Immutable CombatStateData identity plus QueryRevision | CommandState and its nested immutable facts belong to this state; no board hash or traversal is needed |
| Result sequence | LatestResultSequence | A processed command, including rejection, separates execution attempts |
| Membership, draw order/cursor, player zone, rail/spec/context/bindings/obstacle contents | Immutable CombatStateData identity | These are input context even when no character changed |
| Characters | Ordered CharacterData identity/count; each holds immutable CharacterCombatFacts | Detect changes that do not replace the command state or roster |
| Owned records / profile composition | RosterRevision and participant OwnedCharacterRecord identities | Native/rank/category composition affects compiled profiles |
| Main persistent state | Source-owned MainCharacterPersistentState identity | Main stats/health feed input and settlement |
| Soul counts | Actual participant soul count values | These values are not assumed to be covered by QueryRevision |
| Soul field | Source-owned SoulFieldSnapshot identity | Field pickup/expiry facts are independent of owned souls |
| Installed battle definitions | BattleLifetime.Require identity | Installation pins categories, progression, rewards, profiles, growth and item definitions |
| Artifact state | ArtifactBufferSnapshot identity | Pool/placed instance changes affect effects and settlement |
| Artifact definitions/configuration | DB context identity and each placed artifact's immutable definition identity | Unlike battle definitions, the existing reader queries these at assembly time; replacement must be detected |
| Inventory | Source-owned immutable ItemInventorySnapshot identity | An equivalent freshly assembled loadout is not a valid identity source |
| Gold / fame | Actual Amount values | Currency changes need not advance combat QueryRevision |
| Decision settings / movement rules | Immutable Default settings and fixed validated rules retained by one service | Recomposition creates a new service; no mutable runtime settings surface is introduced |

Preview answer number and presentation acknowledgement are deliberately excluded: neither is a calculator input. Command request constants and expected battle/turn identity are captured by the service. Run identity/outcome and Automatic phase are checked by that service in addition to the assembler stamp.

## Verification ledger

- `phase-3-source-red`: initial missing API compile run also exposed a missing test namespace import; not behavioral RED.
- `phase-3-source-behavior-red`: 11 selected, 3 passed, 8 failed. All seven changed-source cases and inconsistent capture failed against the placeholder implementation.
- `phase-3-source-green1`: 11 selected/passed, 0 failed/skipped. This is initial source coverage, not the Phase exit or a performance gate.
- `phase-3-service-api-red`: missing service APIs, not behavioral RED.
- `phase-3-service-behavior-red2`: 12 selected, 3 passed, 9 failed for missing frame/lifetime/preparation behavior. Its preceding run had a missing test namespace import and is not behavioral RED.
- `phase-3-service-green1`: 22/23 passed. The remaining test compared independent fixture objects by identity. Subsequent comparison refinements use existing value contracts and ordered result facts; they normalize only independently generated Run IDs, with within-Run identity checked separately. `CombatCommandState.SameValues` deliberately expects the same installed structure-definition objects, so cross-fixture state comparison reads their public values instead.
- `phase-3-service-expanded-red`: explicit factory composition failed because its new argument did not yet reach the service. The argument is now passed through the existing Core/Combat factory chain. Other failures in this run concerned test-owned terrain bindings and independent-Run comparison, not product behavior.
- `phase-3-fresh-run-red`: the new run-end-during-fresh-assembly case demonstrated one unnecessary calculation after Run termination. An additional eligibility check before calculation fixes it; publication had already been suppressed.
- `phase-3-combined-green3`: all 47 initial/expanded cases passed. Extra reaction, clock and cleanup coverage follows in the final focused run.

## Implemented boundaries

- The assembler captures and validates the full source; currentness reads reuse source-owned immutable references and participant arrays captured once. Artifact definition identity is checked within a stable DB context, not only on context replacement.
- The existing advance service owns one decision session, one retained source, the monotonic clock and the last-spent frame. Capture plus initial overhead is one measured unit; later units call one session Step. The frame stamp survives cancellation and source replacement. Readiness reads do not assemble or select actions.
- Prepared execution reassembles once, checks the captured source before calculation and publication, and publishes one accepted transition through the existing publisher. Rejection retains the session's rejection/candidate counters and does not publish. Terminal changes and faults release retained work. Cleanup Tick only releases; it does not prepare or publish.
- Factory creation -> local `ICombatAdvanceService` plus module registration -> CoreGroup export -> the existing CombatAdvance property -> ProgressReader/StepContext is unchanged as a service route. The Core factory's explicit opt-in defaults false.
- Plan adds preparation routing only after its existing completion, abandon, input and presentation priorities. Two new one-shot Jobs complete on normal no-publication dispositions and enforce sequence invariants. BuildStep cancels preparation on superseding exit facts. CombatNextJob and the shared JobRunner are unchanged.
- No existing test assertions were edited. The only existing test configuration change is a reference to installed `UnityEngine.TestRunner` for the new yielded Core-service measurement. No View/Flow test or MSBuild was added.

## Final regression and source verification

| Run | Selected | Passed | Failed | Skipped |
|---|---:|---:|---:|---:|
| `phase-3-final-core` | 5,115 | 5,085 | 25 | 5 |
| `phase-3-final-integration` | 1,011 | 962 | 47 | 2 |
| `phase-3-yielded-service` (explicit measurement) | 1 | 1 | 0 | 0 |

All 53 new source/preparation behavioral cases passed in the final CoreIntegration run. The explicit measurement is skipped in that full run and passed separately. The earlier `phase-3-focused-final` had 52/53 passing with one invalid test-owned Boss definition; `phase-3-roar-green` passed after marking that fixture's voluntary movement disabled, and the final full integration run confirms the corrected case. No production Boss rule changed.

All 72 existing failure fullnames, labels and messages match the Phase 0 baseline exactly. `Logs/EnemyAi/phase-3-work/regression-comparison.json` records zero differences for each assembly, using `compare-regression.ps1`. The full suites are not entirely green. Existing failures and explicit diagnostic skips are not waived or converted into passes.

The two frozen Preview/movement checks and all 11 legacy decision fixture cases passed in final Core. Their reference files are unchanged: 86-vector SHA-256 `021FBD5C922D7307D5194E2FD548F040B12E1A77A394D902450311EC038C1A49`; four-vector SHA-256 `B8C37AB809A42EF40F49A13DB97A179EF89C611F3EFCA03EBC839DB934050752`; frozen side rows SHA-256 `F71DF6D6DAED5F5EC98FF6CE7960F9A2F23A1DB0995D184C7B50F8516F097006`.

Both final regression manifests match all 76 current changed C# fingerprints, including inherited Phase 2 work. The service measurement manifest differs only in CombatPlan.cs, where a duplicate using was removed afterward; the measurement does not execute Plan/Flow. Seven new C# files have their matching unique metadata GUIDs. No Framework, shared JobRunner or ViewSystem implementation changed. No existing test assertions changed. `git diff --check` passed.

Starting and ending HEAD is `9d8c9ce456db95e8e0aff766240af47353f18f46`. No commit or push occurred. Unity-generated FMOD/font changes were backed up under `Logs/EnemyAi/phase-3-work/generated-assets/` and restored to the exact verified entry bytes after all Editors exited. The initial restore guard correctly stopped at the Git LF/working-tree CRLF hash difference; restoration proceeded only after the CRLF baseline hashes matched all three files. Their Git status is clean; all source and document work is preserved.

### Remaining acceptance

User Unity compilation and F1–F8 remain Pending. G-atom remains unmet; exact overrun attribution, real gameplay frame/latency and allocation acceptance remain open. The necessary source dependencies are covered; these remaining measurement/manual gates do not authorize gameplay activation. No Phase 4 work was started.

## Yielded Core-service measurement

`Logs/EnemyAi/phase-3-yielded-service/` passed its explicit UnityTest (1/1). It reuses the existing N5/N6/N8 service fixtures and seeds 1/7/19/41/97. One warmup side per fixture/seed is retained separately; four measured sides per seed produce 60 measured sides, 360 accepted actions and 1,701 yielded Editor updates. Every ordered action result and final world was compared with a fresh immediate-Advance run using the same fixture. CSV writes and comparison assertions occur outside the timed work.

The harness yields between `EditorApplication.update` callbacks and advances an injected frame stamp. The clock is the real Stopwatch (10,000,000 ticks/s). Unity 6000.0.73f1, Windows 11, AMD Ryzen 7 6800H; batchmode/nographics. This proves yielding and service behavior, **not gameplay rendering, presentation, a 60 Hz frame cadence, or F1–F8**. Live frame/latency acceptance cannot be inferred from these unthrottled Editor update latencies.

Preparation time includes cleanup Tick, initial status, the slice or execution-call preparation overhead, and two subsequent status reads. Actual selected-action calculation and publication are measured separately. Source capture/session construction is the first timed work unit. A capture-containing update may then run more units; its full time is not an assembly-only duration.

| Fixture | Sides / actions | Preparation per update p95 | p99 | Maximum | Largest warmed atom | Action calculation + publication p95 |
|---|---:|---:|---:|---:|---:|---:|
| N5 | 20 / 120 | 1.0489 ms | 1.1339 ms | 1.3566 ms | 0.4761 ms | 3.0951 ms |
| N6 | 20 / 120 | 1.0538 ms | 1.1487 ms | 1.3839 ms | 0.5265 ms | 3.0161 ms |
| N8 | 20 / 120 | 1.0269 ms | 1.0596 ms | 3.5491 ms | 3.0911 ms | 1.8901 ms |
| All | 60 / 360 | 1.0467 ms | 1.1276 ms | 3.5491 ms | 3.0911 ms | 2.9546 ms |

Preparation-only call updates (1,341 samples) had p95 1.0525 ms and p99 1.1438 ms. Across the 360 executed actions, calculation plus publication maximum was 4.4032 ms; this cost is not hidden in preparation statistics. Editor-update decision latency p95 was 11.643 ms, with maximum 22.3807 ms. Full-side preparation sum p95 was 37.5285 ms. These are fixture-specific batch observations, not gameplay acceptance.

### Overrun disposition and limits

- No measured warmed preparation update exceeded 4 ms. One warmed atom exceeded 1 ms: N8/seed41/repeat3/decision5/frame16, 3.0911 ms, zero recorded collections. That update spans WorkVersion 3020–3759. The current aggregate probe does not identify which individual Step in that interval took the maximum; exact attribution remains open. The sample is retained as an activation blocker rather than attributed to GC or a specific rule without evidence.
- Warmup/first-observation costs are retained. The first N5 capture/session-creation unit was 3.4167 ms (the whole update was 4.2222 ms), zero collections. The highest first-side update was N5/seed1/decision1/frame28: 27.0726 ms, including a 27.0352 ms unit advancing WorkVersion 1317 to 1318, zero collections. The narrower owner/runtime cause has not been established by this probe. The earlier Phase 2 first-observation issue remains open; similar duration alone is not proof of the same cause.
- Repeated source validation on the focused five-character fixture took 3.7006 ms for 10,000 reads in `phase-3-focused-final`. It retained the existing source identities. This is a small-fixture observation, not a maximum bound for every source shape.
- The 4,096-byte allocation calibration returned zero again. Byte measurements are **unavailable**, not zero; CSV uses -1. No G-allocation pass is claimed.
- Warmed open-fixture frame percentiles meet the numerical targets in this harness. G-atom remains unmet; first-use, full stress-matrix, live frame/latency and user Flow acceptance remain outstanding. Normal gameplay stays on the immediate route.

Raw files: `prepared-frames.csv`, `prepared-decisions.csv`, `prepared-sides.csv`, `prepared-runtime.txt`, `prepared-summary.json`, NUnit XML, Unity log and source hashes. The reproducible summarizer is `Logs/EnemyAi/phase-3-work/summarize.cjs`, using nearest-rank percentiles.

## Independent review and locality

The final fresh read-only review found no Critical/Important issue in the source, timing/lifetime, publication or Flow contracts. It found one duplicated using in CombatPlan, removed before the full regression runs. This was a no-behavior compiler-warning cleanup; no extra test was invented for that one-line edit. No other-author test assertions changed.

The reviewer did not claim final RC/RI results, gameplay compilation/playback, F1–F8, numerical performance acceptance or Phase 4+ semantics. Those remain separately evidenced or Pending below. The old G-atom failure was not reclassified as a new Phase 3 code defect or silently waived.

| Representative future change | Actual modification / understanding boundary | Required verification |
|---|---|---|
| Change preparation budget or timing | CombatAdvanceService; one monotonic clock and frame stamp. No attack/Trait implementation knowledge is needed to change the budget. | Clock/frame tests, yielded measurement, real Flow frame check |
| Change an assembled input source | CombatInputAssembler/EnemyDecisionSourceStamp and the source owner. Rules/AI preferences continue to consume immutable input. | Dependency invalidation, no-copy validation, publication staleness, RC/RI |
| Change scheduling precedence or prepared disposition | CombatProgressFacts/Reader, CombatPlan and its two one-shot Jobs. Existing Runner/Job foundations remain unchanged. | User F1–F8 and permitted integration regressions; no new View test |

## User Unity acceptance (Pending)

**컴파일 미확인.** Core batch evidence does not sign off View/Flow compilation and gameplay playback under the user's verification rule. F1–F8 from the implementation plan remain Pending: continued UI/rendering while thinking; once-per-frame scheduling; stale/battle-end cleanup; hidden rejection fallback; abandon precedence; roar/terminal priority; last-night gimmick order; finite all-Hold/no-action completion. Use an explicit diagnostic composition with `InGameCoreSystemFactory.Create(..., usePreparedEnemyAdvance: true)` for these checks. Default callers retain false. Do not enable the normal game path until the existing atom, frame, correctness/isolation and Flow gates are satisfied.
