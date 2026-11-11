# Phase 2 prerequisite: Preview optimization and remaining atom gate

**Status: measured optimization implemented; G-atom still failed. Tasks 2.1–2.3 have not started.** The user authorized Phase 2 and explicitly included the preceding Preview optimization after the [entry review](phase-2-entry-review.md). This report does not claim Phase 2 completion or runtime frame protection.

## Source and scope

Start/end HEAD: `9d8c9ce456db95e8e0aff766240af47353f18f46`, branch `feature/Cheon/Core/NewCombatSytem`. Existing uncommitted Phase 1 work was preserved. No commit/push or Flow/View/scene change was made.

Paths below use `A` and `C` from the implementation plan.

| Change | Files |
|---|---|
| Reuse fixed registration metadata | `A/CombatCommandEffects.cs` |
| Append profiling sections without changing existing values | `C/Application/Diagnostics/CoreProfiling.cs` |
| Add owner-level scopes only | `A/CombatAttackQueries.cs`, `CombatAttackCalculator.cs`, `CommandAttackProjection.cs`, `AttackDeliveryExecutor.cs`, `AttackResultFactory.cs`, `MovementProjection.cs`, `TraitPresenceReconciler.cs`, `Traits/TraitRegistry.cs` |
| Add four focused Core checks | `Assets/Tests/EditMode/EnemyPreviewPreparationTests.cs` and `.meta` |
| Record actual continuation status | Implementation plan, entry review, this report |

The initial source fingerprints matched all 13 Phase 1 manifest entries. Subsequent raw folders contain source hashes where noted below. HEAD alone does not identify these uncommitted changes.

## Root cause and minimal correction

`CombatCommandEffects.Capture` constructed and validated the entire default `TraitRegistry` for every effect query, including characters with no traits. The corrected red run measured 32 registry constructions in 32 captures:

| Input | Capture time | Registry construction inside it |
|---|---:|---:|
| No traits | 3.545 ms | 3.525 ms |
| StationaryFire | 3.310 ms | 3.132 ms |

The owner now retains one `static readonly` registry. Its registrations contain stateless rule/factory metadata and expose no mutation API. Definitions, effect-rule bindings and current state snapshots are still created from each call's current profile and character. No projected world, RNG, decision or cross-turn gameplay snapshot is cached. The existing registration table remains the single source.

The green run recorded zero registry constructions across 32 warmed captures; corresponding capture times were 0.121 ms and 0.320 ms. These small probes demonstrate the removed repeated operation, not a whole-preview latency guarantee.

## Matched workload measurements

Unity 6000.0.73f1, approved AppData-capable batch EditMode path, Mono, null graphics device. The original 55 fixture/seed inputs, warmups and samples were preserved: 1,100 warmed decisions and the associated 1,400 full-preview invocations. Input TSV SHA-256 remains `3a779e1b0228bd855f8d1df1f309a400e10fbd599ea5fc5793762caf64152372`.

Times below are ms; percentiles are nearest rank over individual warmed full-preview calls.

| Capture | Mean | p95 | p99 | Max | Calls above 1 ms |
|---|---:|---:|---:|---:|---:|
| Before optimization, owner scopes | 3.2896 | 15.9701 | 20.2214 | 54.6725 | 1,400/1,400 |
| After registry reuse | 1.1106 | 2.5040 | 3.9557 | 23.3835 | 447/1,400 |
| Additional projection/presence scopes | 1.0931 | 2.5499 | 3.9855 | 24.8348 | 471/1,400 |

The improvement repeats in the second capture, but the gate does not pass. Instrumentation was extended between captures: the before run has eight added owner scopes, the after run adds binding/registry scopes, and the final detail run adds projection/presence scopes. These are same-workload observations, not identical-instrumentation confidence intervals or a precise causal percentage estimate. There was no optimization to fixture construction or a reduced candidate workload.

Cold N5 first-decision times were 360.295/341.247/351.592 ms respectively. They are kept separate; this work does not solve first-use latency. Current-thread allocated-byte probing remains unsupported. Outliers have not been attributed to GC, JIT or scheduling without evidence.

Final detail scopes include both preview and actual-action children:

| Scope | Calls | Mean | p95 | Max |
|---|---:|---:|---:|---:|
| Attack delivery | 2,200 | 0.5098 | 1.3349 | 20.5007 |
| Command completion | 2,500 | 0.3398 | 0.9026 | 17.6075 |
| Current projected input | 8,200 | 0.0789 | 0.2515 | 3.2802 |
| Presence reconciliation | 15,200 | 0.0667 | 0.2371 | 13.8754 |

These are inclusive nested scopes and must not be added together. They identify remaining owners, not the exact exclusive cost or cause of every long preview.

## Verification

| Check | Result |
|---|---|
| Red, corrected fixture | 2 passed, 2 expected failures: each warmed capture rebuilt the registry |
| Focused green | 4 passed: no repeated registry build, independent current state/definition, repeated-preview isolation and actual settlement parity |
| Full Core | 5,047 total: 5,021 passed, 25 failed, 1 expected opt-in skip |
| Full CoreIntegration | 957 total: 909 passed, 47 failed, 1 expected opt-in skip |
| Failure comparison | All 72 failed-case identities, labels and messages match Phase 1; zero added, removed or changed failure records |
| Pure corpus captures | Each capture passed its one measurement test; passing a capture test is not passing G-atom |
| Core-service capture | Passed 1/1; 60 full sides and 360 published Advances, with exactly one result-sequence increment per Advance |
| Legacy action traces | All 500 rows match the frozen Phase 1 trace in every capture, including order; comparison normalizes line endings only |
| Independent read-only code review | No actionable Critical/Important/Minor findings in this prerequisite diff |
| UF / rendered frame checks | NotRun; no prepared session or frame scheduling exists |

The exact retained failure records are in `Logs/EnemyAi/phase-2-work/regression-failures.json`, with original NUnit XML in the Core and integration folders. The known night-length, Taunt activation, Cinemachine and other baseline failures were not edited away. The first new state test used intentionally deferred Focus; it was corrected to active StationaryFire before the meaningful red/green cycle. No production availability rule was changed.

Core compilation and batch tests executed. Manual View compilation/playback verification was not performed (컴파일 미확인); user-owned View/scene acceptance is not cleared by these results.

The separate service series includes first use and assembly/calculation/publication, not preparation slices. N5/N6/N8 each have 120 Advances: mean 7.671/6.330/5.328 ms, p95 8.952/13.236/9.094 ms. Their full-side p95 values are 84.804/65.206/69.125 ms over 20 sides each. N5's first-use Advance still took 220.313 ms. These values cannot establish rendered-frame responsiveness.

Final source verification matched all 22 C# hashes captured for the full Core run. The new test metadata GUID is unique; `git diff --check` reported no whitespace errors. After all Editors exited, the three FMOD/font assets that were clean at entry were backed up under raw evidence and restored; no production asset change remains from these runs.

## Locality and next entry condition

**Actual modification scope:** one effect-binding owner for behavior-neutral metadata reuse, existing profiling hooks, one Core test file. AI selection, damage formulas, registration definitions, Buffer ownership and Framework service wiring remain unchanged. No new service, layer or state mirror was introduced.

**Understanding scope:** the fix requires the registration immutability contract and the separation between rule metadata and per-unit state. AI consumers need no new knowledge of individual traits. The independent review verified current registry/rule/factory state safety; new mutable rule fields would require reconsidering this reuse contract.

**Verification scope:** focused freshness checks and action traces plus full Core/Integration regression were necessary because the shared capture path serves real attacks and other effects. The smaller code change does not justify skipping those dependencies.

**Remaining gate:** G-atom is failed; G-frame and prepared latency are NotRun; exact allocation is unavailable. Pure pursuit and full attack scans remain synchronous. Utility, optional caps, decision sessions and prepared execution are not active.

Further local optimizations may exist; this evidence does not prove a larger redesign is unavoidable. However, current-world construction and presence settlement are not fixed metadata: they depend on projected characters, deaths, shields, taunt, turn counts and owned rewards. Reusing them across mutations, skipping them, yielding inside settlement or changing assessment coverage needs an explicit lifecycle/invalidation or coverage contract. The concrete next review should use the final detail capture to decide whether to continue measured owner-local reductions or revise the planned full-preview-as-one-Step boundary. No such behavior/design change or performance-gate waiver was assumed here.

The original Phase 2 contract and its entry gate remain unchanged. Stop at this evidence checkpoint; do not label Tasks 2.1–2.3 complete or proceed to Phase 3.

## Evidence locations

All paths are under `Logs/EnemyAi/` (ignored local evidence):

- `phase-2-preview-before/`: first owner-scope CPU corpus, traces and capture XML; no separate source-hash file was captured for this initial run.
- `phase-2-preview-red3/`, `phase-2-preview-green/`: meaningful failing/passing tests and registry cost output; source hashes, logs, XML and exit codes.
- `phase-2-preview-after/`, `phase-2-preview-detail/`: fixed-input/side corpus, per-scope CSV, nearest-rank summaries, traces, source hashes, logs and XML.
- `phase-2-preview-core/`, `phase-2-preview-integration/`: full regression XML/logs/source hashes/exit codes.
- `phase-2-preview-service/`: service decision/side CSV, summaries, source hashes, XML and log.
- `phase-2-work/`: initial dirty status/diff, progress ledger, reproducible runner/summarizer, trace comparison and exact failure records.

No raw file overwrote Phase 0/1 evidence. The unusable initial Focus test runs are retained as `phase-2-preview-red/` and `phase-2-preview-red2/`, and are not counted as successful verification.
