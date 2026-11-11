# Enemy AI — Verification and Next Session Handoff

Updated 2026-11-08

**Current checkpoint.** Phase 7 diagnostic review is complete and gameplay acceptance remains unmet. Read the [Phase 7 acceptance handoff](2026-11-07-enemy-utility-ai-hazard-forecast-handoff.md) before resuming. This supersedes the historical checkpoints below.

## Historical Phase 6 checkpoint

**Phase 6 checkpoint.** Phase 6 development and Core verification are complete. Start from the [Phase 6 report](../reports/2026-11-07-enemy-utility-ai/phase-6-utility-choice.md). Phase 7 requires the user's next instruction.

- Preserve the uncommitted checkout at `9d8c9ce456db95e8e0aff766240af47353f18f46` and all earlier changes. Commits and pushes belong to the user. View and Framework/shared Job foundations remain outside this delivery.
- Default gameplay still uses immediate `Advance`. `AdvanceUtility` and prepared composition drive the same Utility session and actual settlement. Keep the immediate compatibility selector until gameplay activation is explicitly accepted. No normal-game flag was changed.
- Focused Core 134/134 and publication/validity CoreIntegration 62/62 passed. Full Core was 5,228/5,196/25/7 and CoreIntegration was 1,016/967/47/2 (selected/passed/failed/skipped). All 72 failed names, labels and messages match Phase 0. The frozen 86 attack Preview vectors, four movement Preview vectors, seven routes, 500 decision rows and 55 input hashes passed.
- Keep mandatory Taunt, private Hold, shared 8/16 precise budgets, one optional follow-up aim per refined move, deterministic ties and finite rejection progression. Attack rejection closes that actor's attack family. Move rejection skips the actor. Preserve the public HiddenEnvironment rule. A projected end cannot clear future possible exposure or claim the terminal tier while coverage is incomplete. V4's complete terminal assumption was verified at score level.
- The corpus retained 1,100 warmed decisions and 220 full sides. R8 changed to a legal escape. A8/X8/H8 instead Hold through End under conservative partial coverage across all five seeds. Carry this as a behavior acceptance concern. Do not infer unreachable actions or accept the observed tendency as a gameplay improvement.
- G-atom remains unmet. Eleven of 1,357,260 warmed Steps exceeded 1 ms, maximum 15.6029 ms at W8 UtilityCandidates. The first-observation Step maximum was 27.7617 ms. Exact allocation is unavailable after a zero-byte calibration. Real-scene and user Flow F1–F8 remain Pending. Read the named run folders in the report, including the failed initial corpus and successful retry.
- All three generated FMOD/font files were backed up and restored after the Editor exited. The corpus retry manifest matches all 115 changed C# files. Its only difference from the full regression and Step runs is the marker switch in the test-only measurement harness. Use the AppData-capable Unity runner and sequential assemblies.

## Historical Phase 5 checkpoint

**Phase 5 checkpoint.** Phase 5 implementation and Core verification are complete. Read the [Phase 5 report](../reports/2026-11-07-enemy-utility-ai/phase-5-candidates-and-hazards.md). The user authorized Phase 5 in this session. Phase 6 requires the user's next instruction. This checkpoint supersedes the historical notes below.

- Preserve the uncommitted checkout at `9d8c9ce456db95e8e0aff766240af47353f18f46`, including all earlier Phase work. This Phase changed 15 Core/test C# files relative to the Phase 4 compatibility manifest. Keep View and the Framework/shared Job foundations outside the work. Commits and pushes belong to the user.
- Final focused verification passed 128/128. Final Core was 5,200/5,168/25/7 and CoreIntegration was 1,011/962/47/2 (selected/passed/failed/skipped). All 72 failures retain the exact Phase 0 full name, label and message. All three final manifests match all 111 changed C# files. Keep the frozen 86 attack and four movement Preview vectors, seven original routes, 500 decision rows and 55 input hashes.
- Candidate generation returns at most four attacks and four moves. Use its path and aim identities as supplied. Enforce `RequiredTauntSource` in the scorer even when a non-taunt escape or Hold is available. Treat an optional escape refinement truncated at its cap as partial coverage. Mandatory pursuit remains a separate existing legality search.
- Keep threat as an estimate of the greatest current-position single-attack loss per cell. Future player choice, movement, hidden information and unsupported effects remain partial. Consume Tornado displacement as uncertain occupancy when random, including later Railway/Rockfall risks. Haboob controls targeting and publishes relocation uncertainty. Actual settlement and RNG remain with their existing owners.
- G-atom is unmet. The final warmed capture observed a 12.9292 ms U8 Threat Step and a 3.1230 ms W8 Candidate Step. Two first X8 Forecast Steps also exceeded 1 ms. Exact allocated bytes remain unavailable after failed Mono calibration. Preserve raw rows in `Logs/EnemyAi/phase-5-verified-focused/` and keep Task 2.0d open.
- Normal gameplay stays on immediate Advance. Phase 3 user Unity compilation/playback and F1–F8 remain Pending. Keep development integration separate from game activation. Run Unity 6000.0.73f1 through the existing AppData-capable runner with sequential assemblies.
- The three Unity-generated FMOD/font files were verified and restored after the Editor exited. Backups and hashes are under `Logs/EnemyAi/phase-5-verified-integration/generated-assets/`. Preserve all source, test and documentation changes.

## Historical Phase 4 checkpoint

**Phase 4 checkpoint.** Phase 4 assessment/forecast implementation, fresh review fixes and final Core verification are complete. Read the [Phase 4 report](../reports/2026-11-07-enemy-utility-ai/phase-4-assessment.md). Stopped for Phase 4 review. Do not begin Phase 5 or enable gameplay without a new instruction. This header supersedes the historical Phase 3 checkpoint and Phase 2 pause below.

- Preserve the existing uncommitted checkout and all Phase 0–3 changes. Necessary Core owners are authorized; View and Framework/shared Job foundations remain outside the work. Commits/pushes belong to the user.
- The user approved public-information evaluation with conservative actual-outcome guarantees. HiddenEnvironment is always present because the source cannot prove hidden hazards absent. IsPrecise describes an observed-world projection; random/actual-only/unsupported observable chains do not create precise children. Dead/terminal movers have no follow-up input.
- Forecasts retain the existing registry order and shared railway/rockfall/night arithmetic. Main/objective death elsewhere cuts off later groups through the shared outcome contract; simultaneous direct victims still count. Predicted Bind plus attack restriction marks unmodelled longer exposure.
- Final focused run passed 50/50 (49 normal + explicit measurement). Final Core was 5,165 selected / 5,134 passed / 25 failed / 6 skipped; CoreIntegration was 1,011 / 962 / 47 / 2. All 72 failure identities match Phase 0. Both final regression manifests match all 103 current changed C# files. The capture predates only the three-line TransitionFromCompleted rename required to preserve existing name-only reflection; the five affected existing tests passed unchanged.
- Keep the frozen 86 Preview vectors, four movement vectors, seven original routes, 500 decision rows and 55 input hashes. No oracle was rewritten. Utility preference/shortlist/activation are not implemented; legacy selection and inactive optional caps remain.
- G-atom remains unmet: five of 254,240 warmed assessment Steps exceeded 1 ms (maximum 12.0903 ms); one warmed R8 lookup was 11.5026 ms. Raw rows, matched repetitions and collection deltas are retained. Precise assessment covers 40/67 admitted candidates from 92 fixed candidates. Allocation calibration remains unsupported; these are Core microbenchmarks, not live frame acceptance.
- Phase 3 user Unity compilation/playback and F1–F8 remain Pending. Normal gameplay remains immediate Advance. Use the existing AppData-capable Unity runner and sequential assemblies.
- All Unity processes exited. The three generated FMOD/font assets were verified, backed up under `Logs/EnemyAi/phase-4-work/generated-assets/`, and restored; their Git status is clean. Do not reset or clean any source/test/document work.

## Historical Phase 3 checkpoint

The user explicitly resumed Phase 3 in this chat. Tasks 3.1–3.3 implementation and Core verification completed with user Unity compilation/F1–F8 Pending. G-atom remained unmet and normal gameplay stayed immediate. See the [Phase 3 report](../reports/2026-11-07-enemy-utility-ai/phase-3-preparation.md). The later Phase 4 authorization above supersedes that review stop.

- Resume the existing uncommitted checkout at `9d8c9ce456db95e8e0aff766240af47353f18f46`. Preserve the working tree and the frozen 86 Preview vectors, 4 movement vectors, 500 decision rows and 55 input hashes. The user has authorized necessary Core owner work and delegated routine decisions.
- Carry forward the implemented decision/session, pursuit, source stamp and prepared service/Flow APIs. Final Core selected/passed/failed/skipped was 5,115/5,085/25/5; CoreIntegration was 1,011/962/47/2. All 53 new Phase 3 cases passed; all 72 failure names, labels and messages match Phase 0. The full frozen Preview and decision checks passed. Both final manifests match all 76 current changed C# files.
- Keep G-atom unmet until the remaining overruns have an evidenced disposition. The follow-up passed the 326 focused cases and matched all 90 frozen Preview results. It recorded 17 warmed Preview Steps and 3 Preview constructors above 1 ms. The final decision capture has 596,160 warmed Steps with 13 above 1 ms, 5 with a recorded collection. The highest was 26.9928 ms with a collection. The first N5 DamagePresence boundary reproduced about 28 ms across isolated runs with zero recorded collections. The report links raw rows and matched Step comparisons.
- Keep normal gameplay on existing immediate Advance. Explicit `InGameCoreSystemFactory.Create(..., usePreparedEnemyAdvance: true)` selects preparation for test/diagnostic composition through the same registered service. The yielded service capture passed 60 measured sides and 360 exact action comparisons: preparation/update p95 1.0467 ms, p99 1.1276 ms, maximum 3.5491 ms. One warmed atom was 3.0911 ms; first-observation updates reached 27.0726 ms. Allocation calibration remains unsupported. This Editor-update harness is not gameplay frame/Flow acceptance.
- Carry forward the verified wall-part cursor, geometry index reuse and pursuit placement optimization. Continue from the exact selection, completion and first-observation boundaries listed in the report. Keep shape/aim legality, immutable world semantics and ordered reactions. Caps remain inactive for the Phase 2 legacy scan.
- Use the existing AppData-capable Unity runner and sequential assemblies. Record compiler/setup failures separately from NUnit behavioral results. Preserve exact failure identities. Keep View and Framework/Job foundations outside the change. The user handles commits and pushes.

## Historical notes from the earlier pause

The following sections record the earlier state before the latest continuation. Follow the current checkpoint above for next work.

## 1. Latest instructions take precedence

- Resume only when the user asks the next session to continue. The current session stopped implementation and prepared this handoff.
- The user is the Core programmer. Continue the already approved Phase 2, then stop and report; do not advance into Phase 3.
- **Omitted necessary Core files are authorized.** Update the plan and proceed; do not repeatedly ask permission because an owner was absent from an earlier filename list.
- Do not modify View code.
- Preserve **Framework/Job foundations**: execution structure, lifecycle, responsibilities, fundamental contracts and state ownership. The user clarified that this is about the foundations, not a blanket prohibition based on filenames. This Phase 2 currently requires no Framework/Job implementation changes. Record any truly necessary external work in the Phase report.
- No commits or pushes. Preserve all inherited uncommitted work. Do not reset/clean/recreate the checkout.
- Documentation is English; code comments/XML docs are Korean. Preserve namespaces, existing spelling, public behavior, SSOT and metadata GUIDs.
- Keep the existing inline execution workflow. No automatic per-task implementer/reviewer delegation. Use a fresh read-only final review when appropriate.

Earlier report statements asking approval for Sacrifice, ForcedDisplacementPathCalculator or WeakGroundCalculator are **historical**. The latest user instruction removes those filename permission blockers.

## 2. Checkout and source state

- Repository: `D:\My_ProjectWork\Github_desktop\Project_L`
- Branch: `feature/Cheon/Core/NewCombatSytem`
- HEAD: `9d8c9ce456db95e8e0aff766240af47353f18f46`
- Changes are uncommitted. The working tree contains inherited Phase 0/1 instrumentation, prerequisite registry reuse, and current partial Phase 2 work.
- No Unity Editor or batch runner remains active at handoff.
- Final capture regenerated three FMOD/font assets; they were backed up under `Logs/EnemyAi/phase-2-work/generated-assets-handoff/` and restored. Their Git status is clean. Do not restore any source/test/doc changes.
- No Framework/View/Job implementation was changed by this task.

Read in order, without restarting completed work:

1. This handoff.
2. [Implementation plan](../plans/2026-11-07-enemy-utility-ai-hazard-forecast-implementation-plan.md), §2.5 and Tasks 2.0a–2.3.
3. [Design](../specs/2026-11-07-enemy-utility-ai-hazard-forecast-design.md), §12.
4. [Latest completed checkpoint](../reports/2026-11-07-enemy-utility-ai/phase-2-sacrifice-resumption.md).
5. `Logs/EnemyAi/phase-2-work/progress.md`, especially its final authorization/pause entries.

## 3. Implemented and verified

Application base:
`Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/`

- `CombatAttackCalculation` and internal `CombatCommandCalculator.BeginPreview`: reference-only creation, Step/WorkVersion, completion-only Preview/Transition, fault/dispose rules.
- Existing ordinary attack, Preview and Reaction drain the same shared machinery; no second preview-only damage algorithm.
- Owner-local work for `CommandAttackProjection`, `TraitPresenceReconciler`, `MovementProjection.BeginCurrentInput/BeginComplete`, `CombatCommandCalculator.BeginFinish`, and `AttackResultFactory.BeginCreate`.
- `AttackDeliveryExecutor` and `AttackDirectDamageCalculator` have shared iterator progression, retaining direct-hit batches and ordering.
- `CombatDamageApplication.ApplySteps/DefeatSteps` share the synchronous damage/instant-death implementation and drive pre/post presence reconciliation incrementally.
- `SacrificeDamagePlanner.PlanSteps/ChooseSteps/SimulateSteps` retain baseline/candidate/probe state, remove unnecessary reservations and reevaluate in original order. Plan/ResolveSingle drain the same work. Candidate ordering is unchanged.
- Independent review found no unresolved semantic/lifecycle issue in implemented Sacrifice work. The earlier Reaction validation regression was fixed and verified.

Important limitations: many movement/arrival/completion and geometry leaves still drain nested work; this is **not complete Preview resumability**. EnemyDecisionSession, prepared execution adapter and resumable pursuit are not implemented. No live frame scheduling or Utility activation is present.

## 4. Exact verification and frozen references

Latest full production-source regression:

| Run under `Logs/EnemyAi/` | Total | Passed | Failed | Skipped |
|---|---:|---:|---:|---:|
| `phase-2-sacrifice-core-final` | 5,074 | 5,045 | 25 | 4 |
| `phase-2-sacrifice-integration-final` | 957 | 909 | 47 | 1 |

All 72 failure fullnames/labels/messages match the preceding checkpoint exactly. Do not call the full suites entirely green or change expected values to erase these failures. The four Core skips are explicit diagnostic captures. All 13 root resumption cases passed.

All 30 C# fingerprints matched those final runs at that checkpoint. **The only C# file changed afterward is `Assets/Tests/EditMode/AttackPreviewParityTests.cs`**, to factor the existing reference checker and capture four additional movement vectors. No production C# changed after the full regression. The full suites were not rerun after this test-only addition.

Frozen oracles; never overwrite to fit new output:

- `docs/superpowers/reports/2026-11-07-enemy-utility-ai/phase-2-preview-reference.json`
  - 86 complete input/Preview/projected-transition/actual-transition vectors.
  - SHA-256 `021FBD5C922D7307D5194E2FD548F040B12E1A77A394D902450311EC038C1A49`.
- `docs/superpowers/reports/2026-11-07-enemy-utility-ai/phase-2-movement-reference.json`
  - Four newly captured pre-migration cases: weak-knockback, weak-two, weak-main, weak-pull.
  - SHA-256 `B8C37AB809A42EF40F49A13DB97A179EF89C611F3EFCA03EBC839DB934050752`.
  - `phase-2-movement-reference2`: 1/1 passed, output explicitly says Captured vectors=4.
  - Initial `phase-2-movement-reference` had a missing test namespace import; fixed before capture. Not behavioral RED.
  - Separate oracle leaves the original 86-vector reference intact.

Current test entry points:
- `AttackPreviewParityTests.FrozenPreviewAndActualSemanticsMatch`
- `AttackPreviewParityTests.FrozenMovementContinuationSemanticsMatch`
- `AttackPreviewResumptionTests`
- `AttackProjectionResumptionTests`

The new movement reference has **not yet been added** to the partition/cancellation loops in AttackPreviewResumptionTests. Do this next. Existing old-path baseline capture is already complete; do not recapture it after production changes.

## 5. Measurements and remaining work

Final exact Sacrifice fixture:
- `phase-2-sacrifice-steps-final`: 100 equivalent evaluations, 478 work calls each, 47,800 samples.
- Step p95 0.0112 ms, maximum 0.2938 ms; zero above 0.5 ms.
- Summed planning median 1.0423 ms; uninstrumented whole Preview median 2.3078 ms.
- This proves a smaller interruption unit for the measured case, not total CPU/GC or all-pipeline acceptance.

Remaining exact forced-path measurement:
- `phase-2-forced-path-probe`, frozen pull-tornado, 100 evaluations.
- Median 0.7055 ms, p95 4.4956 ms, maximum 6.6023 ms; all 100 above 0.5 ms.
- No GC collection recorded during those measured evaluations.
- `AttackForcedMovement` contains only a call-site timing scope here; the actual generic traversal is still unchanged.

Both measurements retain CSV/runtime/source hashes and summary JSON. Allocation probe is unsupported (zero returned for a 4,096-byte allocation); unavailable is not zero. Batch loops do not establish rendering-frame performance.

### Next implementation sequence

1. Add behavioral RED checks that nested movement/weak-ground work does not drain multiple presence units in one root Step. Reuse the existing presence counter and frozen vector builders.
2. Resume `ForcedDisplacementPathCalculator` **in that shared owner**. Its unit/obstacle/tile callers must keep one traversal implementation, preserve RequestedDistance/actual steps/Blocked/Interrupted/requested-last-cell semantics, and allow can-enter/entry child work to suspend. Do not duplicate traversal in callers.
3. Resume `WeakGroundCalculator.CollapsePending`: change all pending terrain first, complete the observation barrier, process original ID-ordered occupants, then settle deaths. Its CurrentInput drain is still present.
4. Connect the already existing owning callers:
   - `AttackForcedMovement`, `AttackKnockbackCalculator`, `PullAttackCalculator`;
   - `MovementEnvironmentCalculator`, `TornadoCalculator`;
   - `MovementProjection` health, presence, forced arrival and death boundaries;
   - nested obstacle/death/Hunter/result/geometry work as required by measured call paths.
   These necessary Core owners no longer require filename-by-filename approval.
5. Preserve frozen results and all current event/RNG/observation ordering. Parent calls advance one child unit; no synchronous drain hidden in a bounded Step. Profiling scopes must end inside each Step, never remain open across yields. Dispose must not run pending settlement.
6. Finish Task 2.0c and Task 2.0d's complete measurements/regressions before integrating decisions. Investigate every >1 ms atom and include setup/seal/dispose/whole Preview costs. Do not silently waive a failed gate.
7. Then implement Tasks 2.1–2.3: decision/settings/session and execution adapter, resumable pursuit/aim enumeration, legacy selection/rejection parity. Legacy scan caps remain inactive until Phase 6. Preserve the original 1,100-decision corpus and 500 side trace rows.
8. Stop after Phase 2 and report. Phase 3 runtime/Flow scheduling is not authorized by this handoff.

Do not expand actual-only reflection merely for Preview boundedness: ThornHeart reflection is disabled by `!plan.PublicPreview`. Current no-op contact/flag/soul-death helpers need no artificial work classes. VisibleTo outliers need exact investigation before assuming an owner redesign.

## 6. Unity execution and final checks

Use the existing `Logs/EnemyAi/phase-2-work/run-unity.ps1` with a fresh RunName each time. Example focused run:

```powershell
& 'Logs/EnemyAi/phase-2-work/run-unity.ps1' -RunName 'phase-2-movement-focused-1' -Filter 'ProjectL.Tests.Core.AttackPreviewParityTests'
```

- Execute Unity through the approved AppData-capable escalation route. The sandbox CurlRequestCache.db failure is environmental; do not delete caches or change Unity versions.
- Verified editor: `D:/Program Files/Unity/Hub/Editor/6000.0.73f1/Editor/Unity.exe`.
- Run assemblies sequentially, never two Editors on this project.
- Inspect NUnit XML and compiler logs; nonzero selected tests and exact failure identity comparison matter.
- Keep new production code/comments/docs consistent with repository rules; preserve existing author tests.
- Back up and restore only verified Unity-generated asset changes after Editors exit. Preserve all other dirty work.
- No new Unity/test process should start merely to prepare this handoff. Implementation and testing are paused now.

