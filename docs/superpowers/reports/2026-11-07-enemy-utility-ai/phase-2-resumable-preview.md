# Phase 2 Resumable Preview: Verified Checkpoint and Scope Blocker

Date: 2026-11-07  
Status: **Phase 2 incomplete. Stop at the Task 2.0c owner boundary; atom acceptance failed.**

Historical checkpoint: the user subsequently approved the Sacrifice owner addition. Its implementation, new verification and remaining shared-owner scope are recorded in [the continuation report](phase-2-sacrifice-resumption.md). Preserve the measurements below as evidence of this earlier source.

## Authorized scope and outcome

The user approved the amended Phase 2, Tasks 2.0a–2.3, as Core work. No Phase 3 implementation, View/Flow/Job/Buffer/scene changes, commits or pushes are included. HEAD remains `9d8c9ce456db95e8e0aff766240af47353f18f46`; inherited Phase 0/1 and prerequisite changes remain uncommitted and preserved.

The frozen semantic reference, projection/observation/completion workers, and shared attack coordinator are implemented. Existing synchronous attack callers drain the same machinery. Direct damage now advances its presence reconciliation as child work. **This is not a complete bounded attack pipeline:** several nested owners still run synchronously, the decision session/pursuit work has not begun, and runtime scheduling remains future Phase 3 work.

A measured dependency requires an owner absent from Task 2.0c's explicit modification list: `SacrificeDamagePlanner`. Its existing reservation algorithm invokes repeated private damage simulations. Leaving it as one call violates the substep gate; moving its rules into the attack coordinator would duplicate ownership. The plan explicitly requires reporting an additional owner before expanding scope.

## Implemented work and evidence

| Task | Current disposition |
|---|---|
| 2.0a | Frozen 86-vector reference captured from the pre-migration synchronous path; later comparisons never rewrote the oracle. Step measurement schema is exercised by diagnostic captures; full decision-corpus acceptance is pending. |
| 2.0b | Incremental projection construction, captured-population presence observation, CurrentInput, finalization and attack result assembly implemented. Intermediate RC/RI had no new failures. Nested leaf costs remain provisional. |
| 2.0c | Shared root lifecycle, ordinary/preview/reaction wrappers, per-hit/batch progression and damage presence child steps implemented. Whole-call nested owners, no-match scans and several required child-fault/cascade cases remain pending. Not complete. |
| 2.0d | Diagnostic measurements identify a failed gate and an unlisted owner. Final full-corpus/attributed child-tree/overhead acceptance has not been run. |
| 2.1–2.3 | Not started. No decision session, prepared execution adapter or resumable pursuit is claimed. |
| Phase 3+ | Not started. No rendering-frame protection or Utility behavior change is claimed. |

New Core type: `Application/Combat/CombatAttackCalculation.cs` with internal `CombatCommandCalculator.BeginPreview`, completion-only Preview/Transition, monotonic WorkVersion, faulted/disposed lifecycle and private iterator ownership.

Existing owners changed: `CommandAttackProjection`, `TraitPresenceReconciler`, `MovementProjection`, `CombatCommandCalculator.Finish`, `AttackResultFactory`, `CombatAttackCalculator`, `AttackDeliveryExecutor`, `AttackDirectDamageCalculator` and `CombatDamageApplication`. The diagnostics enum appends markers without changing existing ordinals. New tests are `AttackPreviewParityTests`, `AttackProjectionResumptionTests` and `AttackPreviewResumptionTests`; `EnemyAiPerformanceMeasurements` gains diagnostic captures. New C# files have paired metadata.

The reference includes complete input, Preview, preview transition and actual transition facts with ordered events and type evidence, including rejected/hidden aims, pierce, reflection, Sacrifice, Pull, forced entry, tornado, shield/presence, Acceleration, deaths and both terminal outcomes. The exact final sealed oracle has SHA-256:

`021FBD5C922D7307D5194E2FD548F040B12E1A77A394D902450311EC038C1A49`

It is stored in [phase-2-preview-reference.json](phase-2-preview-reference.json). Draft captures remain separate raw evidence; none became a revised expected result after product migration.

## Verification record

Raw locations below are relative to the repository. Abbreviated run directories are under `Logs/EnemyAi/`. Each run directory contains NUnit XML, Unity log and source fingerprints where applicable.

| Run | Result and meaning |
|---|---|
| `Logs/EnemyAi/phase-2p0a-reference-sealed` | 1/1 passed; frozen old-path capture verified. |
| `phase-2p0b-red`, `completion-red`, `result-red` | Missing API compilation failures while establishing new owner interfaces; these are not represented as behavioral RED. |
| `phase-2p0b-green1`, `completion-green1`, `result-green1` | 7/7, 9/9, 10/10 passed respectively, including frozen parity. |
| `phase-2p0b-core` | 5,058 total; 5,032 passed, 25 failed, 1 skipped. Failure names/messages match the prerequisite baseline exactly. |
| `phase-2p0b-integration` | 957 total; 909 passed, 47 failed, 1 skipped. Same baseline failure identities/messages. |
| `phase-2p0c-red` | 8/8 behavioral failures against the compiling API stub. |
| `phase-2p0c-green1` | 19/19 passed: all 86 vectors under partitions 1/2/31, root lifecycle, source isolation, cancellation at every observed boundary for representative normal/Pull/terminal vectors, plus projection and golden tests. |
| `phase-2p0c-damage-red` | Behavioral RED: one HealthFlush Step drained 36 presence units. |
| `phase-2p0c-damage-green1` | 2/2 passed: at most one presence unit per root Step on the test vector, and all frozen semantics. |
| `phase-2p0c-reaction-red` | Behavioral RED: empty reaction incorrectly inspected rules before its original early return. |
| `phase-2p0c-leaf-final` | 3/3 passed: reaction admission, direct-damage child stepping, and exact Sacrifice call measurement with 100 frozen-result comparisons. |
| `phase-2p0c-core-final` | 5,070 total; 5,042 passed, 25 failed, 3 skipped. All 21 new non-explicit tests passed. The two additional skips are dedicated explicit measurement tests. All 25 failure names/labels/messages match the prerequisite baseline exactly. |
| `phase-2p0c-integration-final` | 957 total; 909 passed, 47 failed, 1 skipped. All 47 failure names/labels/messages match the prerequisite baseline exactly. |

Core batch execution uses the authorized AppData-capable Unity 6000.0.73f1 route. Baseline failures are preserved, not waived or updated to current outputs. The existing full suites are not described as entirely green.

UF/manual scene acceptance is NotRun. Core compilation/test execution does not establish frame scheduling, View playback or real-scene performance. Those acceptance checks remain **컴파일 미확인 / playback unverified** for View/scene acceptance.

## Measurements and failed acceptance

The intermediate `phase-2p0c-work-probe1` capture ran all 86 vectors under partitions 1/2/31 with one first observation and 20 warmed observations: 5,418 complete comparisons. It records constructor, individual Step, result read and disposal in `preview-work.csv`, plus complete evaluation totals in `preview-totals.csv`. All comparisons passed. This capture predates the subsequent direct-damage refinement and Reaction fix; it is diagnostic evidence, not final-source acceptance. Its phase labels identify parent regions, not every internal leaf.

The exact final-source probe adds a call-site scope around **unchanged** `SacrificeDamagePlanner.Plan`, isolating it from adjacent allocation/setup and downstream damage. `phase-2p0c-leaf-final/sacrifice-leaf.csv`, `sacrifice-runtime.txt` and `summary.json` retain the raw evidence. Fixture: frozen `sacrifice-ordered`; ten warmups, 100 measured evaluations; Stopwatch frequency 10,000,000. Each evaluation produced the same frozen semantic result.

| Exact Sacrifice call | Measured milliseconds |
|---|---:|
| Mean | 1.229934 |
| Median | 1.0783 |
| p95 (sorted index floor(n × .95)) | 1.9215 |
| Maximum | 6.4766 |
| Samples above 0.5 ms | 100 / 100 |
| Samples above 1 ms | 92 / 100 |

No GC collection was reported during any of these 100 complete measured evaluations. This rules out observed collections for these samples; it does not rule out scheduling noise or make the measured maximum a portable guarantee. The repeated median already exceeds the target. The earlier intermediate capture also identified Pull, knockback, completion and other regions requiring further attribution/refinement.

The allocation probe returned zero for an allocated 4,096-byte array on this runtime. Exact allocated bytes remain **unavailable**, not zero. Batch loops are not rendered-frame measurements. The original 1,100-decision corpus, 500 side trace rows, total uninstrumented overhead comparison and complete callback/constructor/cleanup acceptance must still be rerun after the remaining pipeline work.

## Required scope decision and smallest remedy

Task 2.0c says: “Any newly required owner outside the listed call graph is reported with its measured dependency before expanding scope.”

Measured call chain:

`AttackDirectDamageCalculator.ApplySteps → SacrificeDamagePlanner.Plan → Choose/Evaluate → Simulate → CombatDamageApplication.Apply/ReconcilePresence`

`Choose` repeatedly evaluates baseline and candidate redirect plans, removes unnecessary reservations and reevaluates. Its synchronous calls drain damage/presence work even though those lower owners now expose resumable paths. That loop is a feature-owned rule, not an AI preference.

**Proposed addition, not implemented:** add `Application/Combat/SacrificeDamagePlanner.cs` to Task 2.0c's allowed owners. Keep Plan/ResolveSingle compatibility entry points, and make both drain the same owner-local continuation used by the attack parent. Retain candidate enumeration, selected reservations, simulation/batch boundaries, defenses, reflection continuation and unnecessary-redirect removal across yields. Drive damage children one unit at a time. Preserve the same ordering, reservation choice and full result; do not approximate lethal damage or remove candidate plans.

Required verification after approval: frozen ordered Sacrifice vectors and existing Sacrifice/reflection/pierce suites; partition 1/2/31 parity; cancellation during baseline/candidate/revalidation; at most one child work item per parent step; exact leaf and end-to-end measurements; complete RC/RI. Then finish the remaining already-authorized Task 2.0c leaves and Task 2.0d before any decision integration. Additional unlisted owners, if measured as necessary, must be reported before modification.

## Locality and review

| Change | Modification / understanding scope | Necessary verification |
|---|---|---|
| Root yield/lifetime change | Root coordinator plus owning child-result contract; no trait formula copy or new Buffer authority | Frozen results, fault/dispose, interleaving, cancelled-source isolation |
| Damage observation change | CombatDamageApplication and its presence child; retain pre/post barrier and batch order | Ordered damage/reflection/status cases, golden actual + Preview, Core/Integration |
| Proposed Sacrifice split | Existing reservation/simulation owner and shared damage work contract; no enemy policy or View internals | Sacrifice ordering and candidates, shared damage regressions, atom measurements |

The change adds retained execution state and synchronous-wrapper overhead. Reduced per-Step work alone is not proof of a total CPU or allocation improvement. The parent still contains unbounded nested leaves, so locality and performance acceptance are explicitly incomplete.

A fresh read-only reviewer found one Important Reaction admission regression; its test was RED, the original input/attacker/rule-check ordering was restored, and the focused final run passed. The reviewer rechecked both constructor and Run guards and reported no remaining Critical/Important/Minor finding in the implemented checkpoint. This review does not certify unfinished nested work or the failed atom gate.

Final checks: all 28 C# fingerprints in the final Core run match the delivered source; all four new C# files have unique paired metadata GUIDs; no disabled stub or new Core Unity reference remains. Whitespace checking passed. After all Editors exited, the three Unity-generated FMOD/font files that were clean at entry were copied to `Logs/EnemyAi/phase-2-work/generated-assets-resumption/` with hashes/diff, then restored; their Git status is clean. Inherited source changes remain. HEAD is unchanged; no commit or push was performed.

