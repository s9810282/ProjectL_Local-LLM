# Phase 2 Sacrifice Resumption and Remaining Shared-Owner Boundary

Date: 2026-11-07  
Status: **Approved Sacrifice continuation implemented and measured; Phase 2 remains incomplete.**

This continues [the preceding checkpoint](phase-2-resumable-preview.md). The user approved adding `SacrificeDamagePlanner.cs` to Task 2.0c. That authorization has been applied to the plan; it is no longer an outstanding request. The new remaining owner boundary is described below.

## Implementation

`SacrificeDamagePlanner.Plan` and `ResolveSingle` now use the same owner-local continuation implementation:

- Enumerate source eligibility, delivered hits, candidate sources and existing reservations without draining the whole search in one parent call.
- Retain each baseline/candidate/probe simulation, its cloned character facts, defense groups and reflection state across suspension.
- Drive the existing damage/presence child work one unit at a time.
- Remove unnecessary reservations and reevaluate the same proposal in the original order.
- Select the same minimum by distance, Main priority, guardian HP descending, target ID, guardian ID and delivery index. Replacing only on strict comparison preserves original stable ties. All candidates are still evaluated; the implementation retains only the current best simulation instead of a list of all candidates.
- Return the readonly reservation map only when planning finishes. Dispose/fault never completes pending simulation or writes authoritative state.

`AttackDirectDamageCalculator` drives this continuation. `CombatDamageApplication.DefeatSteps` supplies the already-owned instant-defeat continuation; existing `Defeat` drains it, retaining death, kill settlement and presence order. A final refinement separates raw damage reconstruction from target damage evaluation.

New runtime changes in this continuation are confined to `SacrificeDamagePlanner`, `AttackDirectDamageCalculator` and `CombatDamageApplication`. `AttackForcedMovement` only gains an exact-call timing scope; its behavior and the shared path owner are unchanged. Diagnostics enum entries append without renumbering. Existing original-author tests are unchanged; additional checks live in our existing `AttackPreviewResumptionTests` and measurement class.

## Verification

All raw run names below are under `Logs/EnemyAi/`; runs contain Unity log/XML/source hashes where applicable. The existing frozen 86-vector oracle was not modified.

| Evidence | Observed result |
|---|---|
| `phase-2-sacrifice-red` | Ordinary case passed; Sacrifice case failed because a single root Step drained 360 presence units. Behavioral RED. |
| `phase-2-sacrifice-green1` | 41/41 passed: existing Sacrifice and direct-reflection cases, frozen complete Preview/actual facts and both child-work-count cases. |
| `phase-2-sacrifice-steps` | 100 frozen-equivalent evaluations; 471 steps each. One 0.559 ms sample motivated splitting raw and target damage evaluation. This intermediate source is not final evidence. |
| `phase-2-sacrifice-work-after` | 5,418 full comparisons passed: 86 cases × partitions 1/2/31 × 21 observations. This precedes only the final additional raw-damage yield and test/diagnostic additions. |
| `phase-2-sacrifice-extra` | Compile failure in newly added tests: missing EntityKey namespace import. Corrected; this is not a behavioral failure or production regression. |
| `phase-2-forced-path-probe` | 3/3 passed: child fault propagation, reservation-removal/revalidation cancellation, exact forced-path diagnostic with 100 frozen comparisons. |
| `phase-2-sacrifice-steps-final` | 1/1 passed: final-source Sacrifice child measurements and 100 frozen comparisons. |
| `phase-2-sacrifice-core-final` | 5,074 total; 5,045 passed, 25 failed, 4 explicit skips. All 13 AttackPreviewResumptionTests cases passed, including all partitions, expanded cancellation and new fault/revalidation cases. All 25 failure names/labels/messages exactly match the preceding checkpoint. |
| `phase-2-sacrifice-integration-final` | 957 total; 909 passed, 47 failed, 1 explicit skip. All 47 failure names/labels/messages exactly match the preceding checkpoint. |

Cancellation coverage now includes every observed boundary of the frozen Sacrifice example, plus explicit baseline/candidate/removal/revalidation boundaries in the richer aura-source case. A profiler-injected exception during a planning presence child must propagate unchanged, fault the root, prevent retry/result access and leave source facts unchanged.

The independent read-only review found no new Critical/Important semantic or lifecycle regression, and no new Minor finding. It verified comparator equivalence, simulation clone ownership, completion-only reservation handoff, cancellation cleanup and instant-defeat settlement order. The review separately identified the unfinished shared-owner scope below.

## Final Sacrifice measurements

Fixture: frozen `sacrifice-ordered`; 10 warmups, 100 measured evaluations. Stopwatch frequency 10,000,000. Exact scope wraps each planning `MoveNext`; source/constructor/whole Preview acceptance remains a separate gate.

| Metric | Final observation |
|---|---:|
| Planning work calls per evaluation | 478, identical in all 100 evaluations |
| Total measured work calls | 47,800 |
| Work-call median | 0.0006 ms |
| Work-call p95 | 0.0112 ms |
| Work-call maximum | 0.2938 ms |
| Work calls above 0.5 ms / 1 ms | 0 / 0 |
| Sum of planning work, median / p95 / maximum | 1.0423 / 1.7385 / 5.7204 ms |
| Uninstrumented complete Preview, median / p95 / maximum | 2.3078 / 3.0708 / 7.4121 ms |

All 100 results match the existing frozen semantic hash; no GC collection was recorded during the instrumented complete evaluations. Exact allocation bytes remain unavailable on this runtime. Raw files: `phase-2-sacrifice-steps-final/sacrifice-leaf.csv`, `sacrifice-totals.csv`, `sacrifice-runtime.txt` and `summary.json`.

The preceding single-call planner had median 1.0783 ms, p95 1.9215 ms and maximum 6.4766 ms. These are **whole planner calls**, not comparable percentile populations to the new child-call table. The improvement demonstrated is the smaller interruption unit, not a demonstrated total CPU/allocation speedup. The final fixture's measured planning calls meet the 0.5 ms target; this is not acceptance of the complete reachable Preview pipeline or real rendering frames.

## Remaining gate and concrete scope proposal

The full diagnostic still exposes unfinished Pull/knockback/arrival work. The parent `pull-tornado/Pull` region has median 0.7408 ms and p95 3.949 ms. These parent-region figures are not attributed to one helper.

A separate exact call-site scope immediately around `ForcedDisplacementPathCalculator.Execute` in `AttackForcedMovement.ExecuteUnit` establishes the narrower dependency:

| Exact forced path, frozen `pull-tornado`, 100 warmed evaluations | Result |
|---|---:|
| Median | 0.7055 ms |
| p95 | 4.4956 ms |
| Maximum | 6.6023 ms |
| Calls above 0.5 ms / 1 ms | 100 / 22 |
| Evaluations with recorded GC collections | 0 |

Raw: `phase-2-forced-path-probe/forced-path-leaf.csv`, `forced-path-totals.csv`, `forced-path-runtime.txt` and `summary.json`. Frozen output remains identical. The scope includes the owned distance loop and its entry callbacks, not just loop bookkeeping; suspending callbacks is necessary to break this unit.

A read-only review of the remaining reachable owners identified the following minimum additional scope together, to avoid another single-file-only proposal:

| Additional owner, not modified | Why it owns necessary work | Minimal change and invariant |
|---|---|---|
| `Application/Combat/ForcedDisplacementPathCalculator.cs` | Shared distance loop used by unit displacement, obstacle knockback and tile forced movement. Existing enter callback cannot suspend during arrival cascades. | Owner-local path cursor and suspendable entry child, with synchronous Execute draining the same loop. Preserve RequestedDistance, actual steps, first blocked cell, Interrupted, and the requested last-cell flag. Do not reproduce traversal in AttackForcedMovement. |
| `Application/Combat/WeakGroundCalculator.cs` | `MovementProjection.Completion` calls `CollapsePending`; with pending ground it drains CurrentInput and the full occupant collapse/death sequence. This is a source-proven observation-barrier gap; no standalone duration claim is made. | Resume the existing owner: change all pending terrain first, finish one observation, process occupants in original ID order, then settle deaths. Do not move collapse policy into MovementProjection. |

These owners are absent from the current explicit Task 2.0c allowlist. Under that task's “report before expanding scope” rule and the user's plan-change boundary, they have been **proposed, not implemented**. The already-approved movement, tornado, observation and death owners then connect to these continuations.

Required verification: original distance/stop tests, unit/obstacle/tile forced movement, Pull/knockback deferred final arrivals, tornado RNG/event order, weak-ground count/collapse/terminal cases, partitions 1/2/31, child cancellation/faults, frozen semantics, complete RC/RI and matched atom/total measurements. Add an explicit pending-weak-ground attack vector before its owner migration; the current 86-vector reference is not claimed to exhaust that branch.

Do not expand into actual-only reflection: `ThornHeartAttackScope` is inactive in public Preview, and §12 excludes bounding actual-only work. Existing no-op contact/flag/soul-death helpers do not justify new continuations. `MovementEnvironmentFacts.VisibleTo` has observed outliers (U8-19 region p95 0.6597 ms), but a costly leaf/GC/scheduling investigation is still needed before claiming its owner must change. Remaining already-listed owner work is also incomplete; approval of these two files is not an atom-gate waiver.

## Locality and final disposition

- A Sacrifice rule change still belongs to its existing planner/rule and shared damage contract. The AI coordinator holds execution progress, not reservation policy.
- Changing a yield point requires knowing that owner's observation/mutation barrier and result contract. It does not introduce another state source or Buffer publication path.
- Shared damage, movement, death and publication regressions remain necessary because actual and Preview callers reuse those owners. Reduced file counts or test counts would not demonstrate independence.

Phase 2 Tasks 2.0c/2.0d remain incomplete; Tasks 2.1–2.3 and Phase 3+ have not started. No View/Flow/scene implementation, commit or push was performed. Core compilation and test evidence do not establish manual scene acceptance; View/scene acceptance remains 컴파일 미확인 / playback unverified.

Final checks: all 30 C# source hashes match both final run manifests; the frozen reference SHA-256 remains `021FBD5C922D7307D5194E2FD548F040B12E1A77A394D902450311EC038C1A49`. No new C# file or metadata pair was required by this continuation. Whitespace checks passed, no new Core Unity reference/stub/disabled test was introduced, and the two proposed owners have no diff. After all Editors exited, the three generated FMOD/font files were backed up with hashes/diff under `Logs/EnemyAi/phase-2-work/generated-assets-sacrifice/` and restored to their clean entry state. Inherited source changes remain. HEAD is still `9d8c9ce456db95e8e0aff766240af47353f18f46`; no commit or push occurred.

