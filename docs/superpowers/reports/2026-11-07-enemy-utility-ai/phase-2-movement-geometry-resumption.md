# Enemy AI Phase 2 Core resumption checkpoint

**Phase 2 remains open at Task 2.0d.** The implemented attack-preview continuations preserve the frozen combat results. The measured atom gate still has unresolved samples, so decision-session integration in Tasks 2.1–2.3 remains pending.

## Scope

The user resumed the inherited checkout at `9d8c9ce456db95e8e0aff766240af47353f18f46` on `feature/Cheon/Core/NewCombatSytem`. The implementation used the existing Core owners for displacement, weak ground, object arrival, obstacle damage, tornado movement, attack visibility and geometry. All changes are local and uncommitted. The [original handoff](../../handoffs/2026-11-07-enemy-ai-phase2-next-session.md) and [Sacrifice checkpoint](phase-2-sacrifice-resumption.md) retain the prior work and evidence.

| Owner | Implemented continuation | Rule and verification boundary |
|---|---|---|
| Forced displacement, Pull, knockback and tile movement | A shared cursor advances cell admission and entry. Attacks, obstacles and tile movement drive the same traversal. Child terrain and arrival work can suspend inside a cell. | Verify requested versus actual distance, Blocked/Interrupted/Completed, requested-last behavior, intermediate and final objects, plus Pull's post-move delivery. |
| Weak ground | Scans terrain, collapses all pending cells, finishes the observation barrier, processes captured occupants in ID order and settles deaths. | Keep all terrain changes ahead of the first fall, and retain the original per-occupant source choice and terminal order. |
| Movement projection and tornado | Resumes current-input observations, landing-candidate scanning, entry, health and forced-arrival work. | The same command owns local RNG, events and occupancy. The selected landing still uses the original row-major candidate order. Verify nested contact, relocation and cancellation. |
| Obstacle reactions | An explosion advances recipient discovery and shared damage work before starting the next queued explosion. | The existing attack cause, character-then-structure order and deferred blast evidence remain the regression boundary. |
| Visibility, aim geometry and delivery | Visible sources, wall component cells, elevation, area masks, legal target selection and piercing trace advance in their existing Contracts, Domain and Application owners. Synchronous callers drain these implementations. | Preserve public targeting methods and failure evidence, both target masks, wall geometry/order and pierce falloff. A new yield point requires the owning rule and its read/write boundary. |

The implementation and test edits stay inside Core and its tests. Existing tests that call internal synchronous movement and obstacle helpers still use wrappers that drain the shared code. The first post-change compile exposed those wrapper dependencies. The wrappers were restored before the passing runs.

## Verification

The frozen preview file has 86 vectors and SHA-256 `021FBD5C922D7307D5194E2FD548F040B12E1A77A394D902450311EC038C1A49`. The separate movement file has four vectors and SHA-256 `B8C37AB809A42EF40F49A13DB97A179EF89C611F3EFCA03EBC839DB934050752`. The four were added to partition and cancellation checks. The original references were kept verbatim.

The original movement cases reproduced 30–60 presence operations in one root Step. The barrel case reproduced 90. The visibility probe reproduced 24 source copies. Area, wall and piercing cases reproduced absent intermediate progress. Final focused verification passed all 13 selected cases. The full suites and captured performance results below use the final source.

| Verification | Total | Passed | Failed | Skipped |
|---|---:|---:|---:|---:|
| Final Core EditMode | 5,086 | 5,057 | 25 | 4 |
| Final CoreIntegration EditMode | 957 | 909 | 47 | 1 |

All 72 failures match the inherited checkpoint by full test name, label and message. The four Core skips and one integration skip are opt-in captures. Core passed all 24 attack-preview resumption cases, 10 projection cases, two frozen parity cases and nine forced-path cases. Each of the three dedicated final captures passed its selected test. All 47 C# fingerprints match across the five final run manifests and the checkout. Three Unity-generated asset changes were backed up and restored after the Editor exited.

The Phase 1 oracle matched all 500 ordered side-trace rows after line-ending normalization. All 55 input hashes matched and 220 sides ran. The legacy benchmark observed these warmed synchronous decision p95 values on the matched input series. Timings include the inherited Phase 2 prerequisite optimizations, so the difference cannot isolate the effect of this continuation.

| Fixture | Phase 1 p95 | Current p95 | Current max |
|---|---:|---:|---:|
| N5 | 26.498 ms | 5.154 ms | 5.605 ms |
| W8 | 106.860 ms | 89.207 ms | 181.513 ms |
| U8 | 166.920 ms | 132.643 ms | 322.946 ms |
| All | 115.989 ms | 92.565 ms | 322.946 ms |

## Atom measurements

The approved Unity `6000.0.73f1` EditMode batch runner measured partitions 1/2/31, with ten warmups and twenty warmed evaluations per vector and partition. Each recorded evaluation matches its frozen result. Times are monotonic elapsed time. The harness records a GC collection-count delta for the timed Step. The allocated-byte probe returned zero after a known 4,096-byte allocation, so byte cells are empty. Unity batch measurements provide no rendered frame evidence.

| Warmed measurement | Samples | Median | p95 | p99 | Maximum |
|---|---:|---:|---:|---:|---:|
| Step | 1,992,060 | 0.0005 ms | 0.0137 ms | 0.0379 ms | 12.2329 ms |
| constructor | 5,400 | 0.0024 ms | 0.0043 ms | 0.0066 ms | 0.0950 ms |
| read | 5,400 | 0.0001 ms | 0.0003 ms | 0.0004 ms | 0.0126 ms |
| dispose | 5,400 | 0.0001 ms | 0.0001 ms | 0.0002 ms | 0.0005 ms |
| Full Preview with Step timing | 5,400 | 1.0686 ms | 2.5265 ms | 3.9423 ms | 18.1902 ms |
| Full synchronous Preview without Step timing | 1,800 | 0.6813 ms | 2.0831 ms | 3.0811 ms | 20.4826 ms |

Warmed calls took 11–886 Steps. The 270 first observations across fixtures and partitions are separate from the warmed set. They include 8 Steps above 1 ms. The largest first observation was **19.8692 ms** at `R8-1/AttackElevationComplete`, with a GC collection recorded in its Step window. First observations after the process has already run do not establish fresh-process cold latency.

The matched frozen `pull-tornado` owner probe measured 34,600 exact path scopes over 100 evaluations, with 346 scopes per evaluation and zero observed collection events. Individual scopes measured p95 **0.0067 ms**, maximum **0.6856 ms**, and zero above 1 ms. The previous whole path scope measured p95 4.4956 ms and maximum 6.6023 ms. The median of the new summed path scopes is **1.0223 ms**, versus 0.7055 ms for the previous whole path. The new uninstrumented full preview median is **2.0721 ms**. The evidence supports smaller interruption units in this path and preserves visibility of the added summed cost.

**G-atom remains open.** 38 warmed Steps exceeded 0.5 ms and 31 exceeded 1 ms. 14 had a recorded collection during the measurement window and 17 had no recorded collection. A collection delta establishes coincidence with a Step. It does not quantify the amount of the elapsed time spent collecting. The longest Step without a recorded collection was **3.2267 ms** at `impact/SelectionPolicy`.

Each of the 31 affected warmed fixture, phase and work-version combinations had at most one sample above 1 ms in the sixty matched repetitions. The highest same-Step p95 was **0.1388 ms**. The raw outliers and same-work comparisons are retained in `phase-2-resume-final-preview/summary.json`. These repetitions do not establish the cause of the intermittent samples or verify the gate.

| Sample | Elapsed | Collection in Step | Owner boundary |
|---|---:|---|---|
| pull, partition 31, repeat 15, Step 196 | 12.2329 ms | Yes | Movement completion entry |
| X8-19, partition 2, repeat 16, Step 84 | 11.9123 ms | Yes | Damage presence reconciliation |
| impact, partition 2, repeat 11, Step 9 | 3.2267 ms | No | Selection policy and effective range |
| T8-1, partition 31, repeat 1, Step 454 | 3.1354 ms | No | Command finalization child |

## Remaining Phase 2 entry condition

Review found no actionable semantic or lifetime regression in the implemented movement and geometry continuations. It confirmed remaining aggregate calls inside some Steps. Continue with the measured owner and retain the same frozen oracles.

- `AttackOcclusionResolver.FindBlock` and `WallSegmentClipper` still traverse wall runs and convex parts within one contact. `WallGeometry` also seals sorted copies of whole geometry collections. Add the wall-part cursor or measure a validated input bound before relying on that contact as a bounded unit.
- `PierceTargetCollector` and `MovementRouteValidator.CanOccupy` advance per cell but still search eligible occupants or terrain inside a cell. Reuse indexes constructed from the captured world within their existing owners. Rebuild after any dependent projected mutation.
- `ObjectArrivalCalculator.OrderedAt`, `EnvironmentPresenceReconciler.Calculate`, `MovementProjection.ResolveDeathBoundary` and `HunterApplication.Reconcile` still collect or settle multiple source items in one call. Preserve the observation barrier and original mutation order when moving their child loops into the owning continuations.
- `CombatCommandCalculator.Finalization` still invokes aggregate reward, experience and terminal helpers. Measure inside the `CompletionFinish` boundary before attributing its observed overrun to one helper. Measure sealing and allocation separately if the runtime permits it.

Once Task 2.0d has an accepted disposition for the remaining atoms, implement the decision/session and supplied-intent execution contracts, resumable pursuit, and attack-first decision/rejection parity in Tasks 2.1–2.3. Reuse the frozen 500-row decision oracle and test logical Preview counts independently of partitions. Phase 3 scheduling is a separate checkpoint.

The raw runs are under `Logs/EnemyAi/phase-2-resume-final-*/`. Each directory includes NUnit XML, Unity logs, and SHA-256 fingerprints for the working-tree C# files. Prior failed setup or compile attempts and intended red assertions remain in their own run directories. The workspace ledger is `Logs/EnemyAi/phase-2-work/progress.md`.
