# Pull Range and Attack Continuation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans to implement this plan task-by-task. Steps use checkboxes to track progress. Check the user's current phase authorization before editing. Project restrictions override skill defaults concerning tests, commits, and phase boundaries.

**Goal:** Handle environmentally zero Pull range as ordinary unavailability and complete pre-hit movement without starting an invalid follow-up attack or its additional effects.

**Architecture:** Reuse `AttackSelectionEvaluation` and current projection observations for a shared continuation query. Pull owns movement; `CombatAttackCalculator` owns the attack-start decision and action settlement; the existing executor gates attack-associated effects. No new runtime service or persistent state is introduced.

**Tech Stack:** Unity 6000.0.73f1, existing pure C# Core assemblies, existing Buffer/result contracts.

**Spec:** [Pull Range and Attack Continuation Design](../specs/2026-11-14-pull-range-and-attack-continuation-design.md)

**Status (2026-11-14):** Phase 3 Unity compilation and all 972 selected EditMode cases passed after the frozen preview reference was updated. Custom scenario and Play Mode acceptance remain open. The source and test-data changes are uncommitted at HEAD `e4281a38e`.

**Revision:** 2 — applies the user's feature-change-locality guideline. Replaces the earlier separate basic-range precheck with the existing preparation and single selection body. A session holding an older copy must reread design sections 6.1 and 8.1 before Phase 2.

**Confirmed timing decision (2026-11-14):** Successful Pull evaluates attack-start effects using surviving sources and recipients after movement and arrival effects. This resolves the drafting question; it is not authorization to begin implementation.

**Session handoff:** [Model sessions and transfer checklist](../handoffs/2026-11-14-pull-continuation-model-session-handoff.md). Read it before beginning or resuming a phase. The spec owns behavior, this plan owns phase progress, and the handoff owns the current session/checkout checkpoint.

## Recommended model routing

The user's "6.1 session" is interpreted here as **GPT-6.1 Sol** (`gpt-6.1-sol`), separate from the **GPT-6 Astra** (`gpt-6-astra`) session. These are task-specific recommendations, not enforced app settings or claims about account availability.

| Phase | Recommended model / reasoning effort | Reason and stop point |
|---|---|---|
| 1 — Effective zero range | GPT-6.1 Sol / Medium | Two bounded adapter changes with established rule contracts. Report and release ownership at the phase boundary. |
| 2 — Continuation and attack lifecycle | GPT-6 Astra / High | Coordinates current facts, notifications, effect gating, settlement, and publication across four files. Finish the phase coherently, then report and release ownership. |
| 3 — Integration acceptance | GPT-6.1 Sol / High | Inspect integrated diffs and reconcile owner-supplied validation evidence. Unity execution remains the owner's responsibility. |

If Phase 3 exposes an unresolved lifecycle/contract issue, transfer a precise finding back to the Astra session for analysis; do not silently expand Phase 3 into a redesign. A review-only transfer is not permission to edit. Phase 2 being one model's work does not replace its independent review step.

The routing is an engineering judgment based on this plan's scope. Official guidance describes Sol as balancing complex work with cost/time and Astra as suited to the most demanding reasoning work: [OpenAI model catalog](https://developers.openai.com/api/docs/models), [model selection](https://developers.openai.com/api/docs/guides/model-selection), consulted 2026-11-14. No price or performance guarantee is assumed.

## Global Constraints

- Unity 6000.0.73f1; existing pure C# Core assemblies and paths.
- Implementation documents are English; new code comments and XML documentation are Korean.
- No commits or pushes by Codex. Stop and report at every implementation phase boundary.
- Phase 3 authorization includes running the current plan's Unity EditMode tests. Test authoring, fixture edits, and MSBuild remain outside this phase. Record static, EditMode, and Play Mode evidence separately.
- Preserve public commands, results, Buffer ownership, Job completion, event formats, and normal attack policies.
- No View, scene, production asset, Factory registration, or Framework changes are planned.
- Existing unrelated changes and the earlier local Haboob fix must be preserved until explicitly replaced by the approved implementation.
- Apply design sections 6.1 and 8.1: existing rule owners and execution paths are authoritative. No parallel selector, replacement world/ledger, manual Buffer/Job completion, or per-feature attack-start state is permitted.

## Review Focus

1. Zero effective range must not bypass definition/status consistency validation or revive Pull through extra range. Owned by Phase 1.
2. Continuation must not pull a second time, use a stale target position, or silently choose another target. Owned by Phase 2.
3. Skipping the attack must preserve movement/environment settlement while suppressing all attack-start/completion effects. Owned by Phase 2.
4. A started attack with zero damage or reflection is not the same as a never-started attack. Owned by Phase 2.
5. Preview, AI range consumers, action use, and Buffer publication must retain their existing visibility and settlement contracts. Owned by Phases 1-3.

## File map

Paths below use `C = Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator`.

| File | Planned responsibility/change |
|---|---|
| `C/Application/Combat/PullAttackCalculator.cs` | Runtime zero-range adapter; keep Pull movement; remove post-movement delivery rebuilding after migration. |
| `C/Application/Combat/CombatAttackQueries.cs` | Add a current-fact continuation wrapper; extend existing preparation stage choice and keep a single selection/readiness rule path. |
| `C/Application/Combat/CombatAttackCalculator.cs` | Coordinate pre-hit movement, attack-start gating, shared delivery preparation, and separate action-use settlement from success effects. |
| `C/Application/Combat/AttackDeliveryExecutor.cs` | Apply attack-start gating consistently to delivery, LavaCannon, and attack-scope completion. |
| Existing rule/result/profile files | Read and reuse. No public contract or geometry-helper changes planned. |

No new C# files, assembly references, service registrations, or asset changes are planned. Existing `.meta` files remain unchanged.

## Phase 1 — Effective zero range is normally unavailable

**Deliverable:** Range queries, previews, commands, and AI selection tolerate a valid environmental reduction to zero without executing Pull.

**Modify:** `C/Application/Combat/PullAttackCalculator.cs`.

**Interfaces:** Keep `SelectionRange(CharacterCombatFacts actor, CombatAttackProfile profile, int basicRange)` and `EvaluateSteps(...)` signatures unchanged. Consume `EnvironmentTerrainRules.AppointRange` and the existing `AttackFailureReason.InvalidPrimaryTarget`.

- [x] Confirm the approved spec, current local diff, and applicable project instructions. Preserve unrelated changes and the earlier Haboob fix.
- [x] In `SelectionRange`, run `Configuration` first. Valid zero range returns zero. Positive range uses the existing helper, which still rejects negative Pull range. Inconsistent definitions and status fail during configuration validation.
- [x] In `EvaluateSteps`, return `InvalidPrimaryTarget` for zero effective basic range after configuration and before reading the selection's primary cell or calling `Destination`.
- [x] Trace `GetAttackRange`, `GetUnitAttackRange`, preview, command selection, and AI candidate/selection consumers. Confirm the existing zero-radius CardinalLine mask excludes the origin and produces no executable aim.
- [x] Run `git diff --check -- Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/PullAttackCalculator.cs`. Observed exit 0 with no whitespace errors.
- [x] Hand off the owner verification cases below.
- [ ] Owner verifies that range 1 + extra 2 + DustScreen reduction 1 produces empty range/aims, including AI candidates, and a rejected preview/command without movement, action use, or attack effects. Cover enemy and allied base-rank clouds, allied rank-2 exemption, positive-range Sandstorm, and ordinary positive-range Pull.
- [ ] Owner verifies that this normal rejection raises no exception or runtime fault, does not stop the combat loop, and is followed successfully by another legal command. A result named `Failure` is a normal return value, not a thrown exception.
- [x] Run existing `PullRangeTests`, `PullTargetAdmissionTests`, `PullPreviewTests`, `TraitTrailTests`, `EnemyTraitTrailAssessmentRegressionTests`, and relevant enemy range checks. These fixtures passed in the Phase 3 run. The direct `PullTraitRule.SelectionRange(0, 1)` invalid-argument test still passed.
- [x] Report source changes and actual verification evidence for the phase review. Compile and play remain unverified.
- [x] Update the handoff checkpoint and release edit ownership.

### Phase 1 implementation and verification record

- Source delta against the local baseline: one file, two methods, 8 insertions and 1 deletion. `EnvironmentTerrainRules.AppointRange` supplies the effective range, `Configuration` owns definition and status validation, and the existing geometry helper owns positive-range Pull. The change requires these contracts at the Pull adapter boundary. It adds no state, registration, or public contract.
- Static call tracing: `Range` and `UnitRange` use `RangeCells`, whose CardinalLine mask has no offsets at radius zero. `Range` derives its single-cell aims from those cells. Preview and command use `SelectionSteps`, where a well-formed cell aim fails before execution. `AiRangeCandidates` emits no aims from that mask, and `EnumerateAimsThatCanHit` rejects cell candidates through the same aim validation. Shared consumer and rank/terrain regression checks remain necessary.
- Independent Phase 1 source review found no actionable issue in configuration validation, range handling, caller paths, or contract preservation.
- Owner validation: compile unverified. Unity Play Mode and existing tests were not run. The post-movement continuation and attack lifecycle remain Phase 2 work.

## Phase 2 — Common continuation and attack lifecycle boundary

**Deliverable:** Post-Pull gameplay refusal completes movement only, with no attack-associated effects, using one common result contract. This phase is atomic: do not leave effect gating and the new decision boundary half-migrated.

**Modify:** All four implementation files in the file map.

**Interfaces:**

- Produce `CombatAttackQueries.ContinuationSelectionSteps(CombatCommandInput input, CharacterCombatFacts actor, CombatAttackProfile profile, EntityId expectedTargetId, MovementEnvironmentFacts environment, WallGeometry walls, AttackElevation elevation, Action<AttackSelectionEvaluation> completed)`.
- Extend existing `SelectionPreparation` with get-only `bool IncludesPreHitMovement`. Add final optional `bool includePreHitMovement = true` to its constructor and to `PrepareSelection`, `ReadSelectionParameters`, `CombatAttackQueries.SelectionRange`, and `PullAttackCalculator.SelectionRange`; other parameters and return types remain unchanged. No new preparation/result type is introduced.
- Produce private `CombatAttackCalculator.PrepareDeliverySteps(CommandAttackPlan originalPlan, CommandAttackProjection world, CombatCommandInput currentInput, ResolvedAttackSelection selection, AttackElevation elevation, Action<CommandAttackPlan> completed)`.
- Split private settlement into `ApplyAttackSucceededEffects(CommandAttackPlan plan, CommandAttackProjection world)` returning the existing trait/category activation tuple, and `SettleActionUse(CommandAttackPlan plan, CommandAttackProjection world)` returning void and only marking the ordinary action used.
- Rename `AttackDeliveryExecutor.Execute` and `ExecuteSteps`' final optional bool to `attackStarted = true`; preserve all other parameters and update any named arguments after a caller search.
- Remove `PullAttackCalculator.RebuildDeliverySteps` after its only caller migrates. Keep the movement callback as the existing movement-level eligibility result, distinct from the final attack-start decision.

- [x] Before editing, trace the current initial selection, Pull continuation, delivery, settlement, and result paths. Use the ownership table in design section 8.1 to identify each source implementation and its callers. Record the limited migration in the phase report; do not begin a repository-wide cleanup.
- [x] Extend the existing preparation and range-parameter flow as specified above. Default `true` preserves initial range extension and Pull planning; `false` uses the basic range and excludes pre-hit movement planning. Keep configuration validation in the existing Pull adapter in both modes, and keep `RequireSource` identity checks.
- [x] Implement continuation evaluation in the exact order from design section 6. Reuse `IntrinsicUnitReadiness`, construct a fresh continuation preparation, and call `SelectionSteps` once; preserve failure and blocked-contact evidence. Build the aim only after checking the target's current field/anchor state. Do not add a preliminary geometry pass or copy selection filters into the wrapper.
- [x] In the existing `SelectionSteps` body, use the preparation's existing range/policy and condition its existing final Pull-evaluation call on `IncludesPreHitMovement`; null preparation keeps the original initial-selection behavior. No second mask, Haboob, eligibility, or wall/elevation algorithm is created.
- [x] Keep invariant exceptions explicit: required references, captured entity records, actor/profile identity, and successful-selection target mismatch. Do not catch `Exception` or add a per-gimmick failure allowlist.
- [x] Restructure `CombatAttackCalculator` so pre-hit Pull movement and continuation evaluation happen before attack notifications. Build the existing empty-delivery plan without emitting or consuming attack effects. Keep ordinary non-Pull ordering intact.
- [x] Retain `arrival.Terminal` checks before and after current-input observation and before starting the follow-up. Do not derive projected completion from the observed `State.Phase`, which can retain the original phase, or introduce a duplicate victory/death formula.
- [x] On a valid continuation, begin the attack once, emit `TraitAttackNotifications`, refresh current actor/input facts, and use `PrepareDeliverySteps`. Preserve original input/action expectations, requested cells, original captured enemy count, attack group, public-wall geometry, and observer visibility.
- [x] Move, do not duplicate, delivery preparation: replace both the old ordinary inline calculation and the Pull reconstruction body with the private shared helper. Verify one delivery-preparation step per started attack, none for a skipped attack, and no alternate finalization route. Preserve existing per-hit current-stat recalculation and revalidation after actual world changes; do not remove necessary checks merely because they reuse the same function.
- [x] On a failed continuation, retain the empty delivery and Pull movement snapshot, merge selection blocked-contact evidence, and proceed to finalization with `attackStarted = false`. Do not recompute a target or re-enter Pull movement.
- [x] Separate success callbacks from unconditional ordinary action-use mutation. Gate success effects, `CombatDebuffApplication.CompleteAttack`, and attack-completed item callbacks with `attackStarted`. Settle accepted ordinary actions once; preserve reaction cost behavior.
- [x] In the executor, gate LavaCannon and `HunterApplication.FinishAction` with attack start in addition to the existing damage/knockback/chain gates. Preserve independent movement/death/presence settlement. Keep reflection and zero-percent-splash behavior for attacks that did start.
- [x] Remove the Haboob-specific branch and old delivery-rebuild method from Pull. Search all callers to confirm no stale method or named-parameter references remain.
- [x] Review the migration using actual callers: `RebuildDeliverySteps` has no executable references; ordinary and continued started attacks use the same preparation helper; all selection consumers reach the existing `SelectionSteps` body; existing AI preparations still default to initial selection. New names or successful local checks alone do not satisfy this step.
- [x] Verify by source tracing that movement-only result creation still includes Pull and pre-hit damage, has no direct hits/attack affected cells, and reaches finalization with one action-use settlement. Preserve the existing result/event format and View movement-only interpretation.
- [x] Run a scoped `git diff --check` over the four files. Request a focused review of current-fact ordering, invariants, effect gates, state ownership, and ordinary/reaction regressions. Do not equate static review with runtime validation.
- [x] Hand off owner verification using the acceptance matrix below, then report and stop for the phase review.
- [x] Update the handoff checkpoint and release edit ownership before the user returns to the 6.1 session. Include the exact review scope, known failures, and next permitted action.

### Phase 2 acceptance matrix

Phase 3 records the existing EditMode coverage below. Scenario combinations without a matching runtime assertion remain pending.

| Case | Required observation |
|---|---|
| Both units outside Haboob initially; destination outside | Movement and attack continue under existing rules. |
| Both units inside the same Haboob initially and at arrival | Movement and attack continue under existing rules. |
| Legal initial designation, destination membership mismatch | Accepted movement-only result; no exception, direct hit, or attack-associated effects. |
| Initial membership mismatch | Existing initial rejection; no movement or action usage. |
| Attacker becomes reconstructing during Pull arrival damage | Movement/environment results remain; no attack starts. Target-only Reconstruction remains targetable. |
| Current target is dead/off-field or combat ends | No target-anchor dereference failure or new attack; existing environment/death settlement remains. |
| Current basic range or wall/elevation prevents delivery | Normal skip with existing failure/evidence; no second Pull or silent retarget. |
| Initial AI preparation vs post-arrival continuation preparation | Initial extension/planning remains; continuation uses basic range and performs no Pull planning; stale observations fail the existing preparation identity contract. |
| Skipped attack with Vanguard/CleanSweep/Acceleration/Intimidation/items | No attack ordinal/grants, attack-scope consumption, pending-attack debuff consumption, or attack-completed item activation. Action is still used once. |
| Successful Pull whose Vanguard/CleanSweep source dies during arrival | Notifications use the surviving post-arrival sources/recipients; do not retain a grant from the old pre-movement notification order. The user confirmed this timing on 2026-11-14. |
| Skipped attack with LavaCannon | No fire/slow from the unstarted attack. |
| Started attack with reflection or zero-percent splash | Existing attack-effect semantics remain, including LavaCannon's captured-target behavior. |
| Movement-only Pull kills through environment | Preserve movement/death rewards; do not newly enable the separate unresolved KillChain policy. |
| Corrupt profile/status or successful selection of a different ID | Remains an explicit invariant failure. |

### Phase 2 implementation and verification record

- Authorization and baseline: the user requested Phase 2 in the original planning chat. The four source hashes matched the released Phase 1 handoff before editing. Phase 1's compile/play/tests were unverified and remain so.
- Changed four existing Core files only: `CombatAttackQueries`, `PullAttackCalculator`, `CombatAttackCalculator`, and `AttackDeliveryExecutor`. No new types, files, Framework services, public result fields, or Buffer/Job paths were introduced. The Phase 1 zero-range handling remains in place.
- Selection migration: the existing `SelectionPreparation` now carries the default-true pre-hit movement choice; continuation passes false and reaches the same `SelectionSteps` body. Existing AI calls keep their default. Configuration and observation identity checks remain; the original target is retained. `PullAttackCalculator.RebuildDeliverySteps` and the Haboob-specific continuation branch were removed.
- Delivery/lifecycle migration: ordinary and post-Pull started attacks call the private `PrepareDeliverySteps` helper. Pre-hit movement occurs before Pull attack notifications. `arrival.Terminal` remains authoritative before/after current observation and at the start decision. One `attackStarted` value gates direct delivery, attack-associated effects, success/debuff/item callbacks, and Acceleration consumption. The original executor callback still settles ordinary action use once, including movement-only results.
- Observation detail: ordinary public preview supplies its existing visible environment through an immutable `WithWorld` input snapshot to the shared preparation helper. Original `plan.Input`, requested cells, action expectations, observer, and attack group remain preserved. Post-Pull delivery re-observes current facts after notifications. These snapshots are not independent state owners.
- Independent Phase 2 review (GPT-6 Astra / High) compared the received Phase 1 baseline and integrated diff and found no actionable findings. It checked current target/readiness, initial/AI defaults, no repeat Pull, effect gates, ordinary/reaction settlement, observer visibility, and finalization/publication.
- Static evidence: scoped `git diff --check` exited 0. Caller search found no executable `RebuildDeliverySteps` or stale `deliverDirectDamage:` argument. Source inspection confirmed one selection body, one delivery-preparation helper, and the existing finalization route. Changed C# files use UTF-8 without replacement characters and LF line endings.
- Locality result: Pull no longer reconstructs ordinary delivery or knows Haboob's refusal reason. Query callers still need the preparation/current-observation contract; command and executor still require the shared attack-start/settlement order. Range, AI, effects, and publication regressions remain necessary because those contracts are shared, not because a separate implementation was added.
- Compile unverified. No tests were written or modified; no compiler, MSBuild, Unity, or test suite was run. No commits or pushes. Phase 2 source completion does not establish runtime acceptance or complete Phase 3.
- [ ] Owner verifies the Phase 2 acceptance matrix and provides compile/play/test results. The accepted behavior includes normal refusal without faulting combat and successful subsequent legal commands.

## Phase 3 — Integration acceptance and handoff

**Deliverable:** An evidence-backed acceptance record. No additional feature work is included.

- [x] Run the relevant Core fixtures for Pull, Haboob, trait trails, terrain and forced movement, Reconstruction, attack notifications, Acceleration/Hunter, LavaCannon, KillChain, and combat items. The original four snapshot failures passed after the fixture update.
- [x] Run the existing CoreIntegration publication checks for Pull, Haboob, trait trails, and combat items, along with shared status, settlement, and targeting regressions. The user scoped Phase 3 to this plan's related tests.
- [ ] Exercise the new acceptance combinations in Unity, including the first zero-range rejection followed by a legal command through the live combat loop. See the gaps below.
- [x] Run the existing visible-input parity and hidden-environment Pull cases. The updated frozen parity and all three resumption partitions passed.
- [ ] Verify scene playback for movement-only Pull and attack-only status suppression for the new combinations.
- [x] Review the four-file diff for public contracts, state ownership, and scope. Record the three actual change cases below.
- [x] Record compilation, test results, and unresolved checks. Release the handoff at the Phase 3 review boundary.

### Phase 3 verification record (2026-11-14)

The four source hashes matched the Phase 2 handoff before and after testing. Unity 6000.0.73f1 batch compilation succeeded. Two EditMode runs used `ProjectL.Tests.Core` and `ProjectL.Tests.CoreIntegration` with fixture filters. The XML reports record the filters and individual results.

| Run | Core | CoreIntegration | Unity exit |
|---|---:|---:|---:|
| [Focused Pull, continuation, and publication](../../../Logs/pull-continuation-phase3/focused-editmode.xml) | 634 passed, 4 failed | 66 passed | 2 |
| [Shared range, action, and effects](../../../Logs/pull-continuation-phase3/shared-effects-editmode.xml) | 247 passed | 21 passed | 0 |
| Total | 881 passed, 4 failed | 87 passed | |

- The initial Core failures were `AttackPreviewParityTests.FrozenPreviewAndActualSemanticsMatch` and `AttackPreviewResumptionTests.AllPartitionsMatchFrozenPreview` at partitions 1, 2, and 31. Each first mismatch was the `pull-friendly` vector. The original frozen reference expected `PlayerAttackActionCount = 1`. The observed value was 0, consistent with design sections 7 and 7.1 for movement-only Pull. `PullPublicationTests.FriendlyMove_PublishesWithoutFakeDamageHit` passed and confirmed that one Pull was published, the attacker action was consumed, and the attack had zero direct hits.
- Existing EditMode evidence covers valid and invalid Pull targeting, visible preview/execution, hidden mines, terrain death/terminal controls, ordinary Reconstruction, Haboob designation, DustScreen reduction and rank exemption, and ordinary attack-effect regressions including LavaCannon with reflection or zero-percent splash. The tests did not directly exercise the full Pull-plus-DustScreen zero-range scenario through the live loop, post-arrival Haboob refusal, reconstruction of the attacker during Pull, the post-arrival grant source dying, the combined skip/effect matrix, or a changed continuation target identity. Source tracing confirmed that the View attack handler branches around attack playback for a Pull result with zero hits. Scene playback was unverified.
- The selected runs cover this plan and the shared combat paths it changes. Product source, test methods, and production assets were untouched throughout Phase 3.

### Frozen reference follow-up

The user authorized correcting the failed expectations. The existing Unity capture test generated all 86 preview and execution vectors for comparison. Only `pull-friendly` and `pull-tornado` differed. In both vectors, preview and command final state changed `PlayerAttackActionCount` from 1 to 0. The captured movement and ordinary action settlement matched the original record, and both cases had zero direct hits. Only those two rows of the [preview reference](../reports/2026-11-12-combat-health-execution-evidence/phase-2-preview-reference.json) were updated. The other 84 rows and all type lists match the original.

The combined Unity 6000.0.73f1 EditMode rerun used the same two fixture filters in a single run with capture mode disabled. Core passed 885/885 and CoreIntegration passed 87/87. All four previously failing checks passed. Unity exited 0 with zero failures or skips. [NUnit report](../../../Logs/pull-continuation-phase3/after-snapshot-update.xml)

| Change case | Edited functions and contracts | Shared verification |
|---|---|---|
| Effective range reaches zero | `PullAttackCalculator.SelectionRange` and `EvaluateSteps` handle a nonnegative environmental range after configuration validation. `EnvironmentTerrainRules.AppointRange` owns the modifier, and the geometry helper retains its positive-range precondition. | Range and unit-hover queries, preview, command, AI, DustScreen ranks, and positive-range terrain controls share the effective range. |
| Pulled target loses delivery eligibility | `ContinuationSelectionSteps` rebuilds the aim from the current target and feeds `SelectionSteps` using the existing preparation with pre-hit movement disabled. `CombatAttackCalculator.ExecuteSteps` checks `arrival.Terminal` and the shared helper prepares started delivery. Pull's old delivery rebuild and Haboob-specific check were removed. | Current target identity and readiness, Haboob, wall/elevation, visibility, initial AI defaults, and no second Pull remain shared selection checks. |
| Movement settles without attack effects | `CombatAttackCalculator.ExecuteSteps` and `AttackDeliveryExecutor.ExecuteSteps` use one attack-start decision. `ApplyAttackSucceededEffects` and `SettleActionUse` separate effect application from ordinary action use. Existing projection, ledger, result, Buffer, and Job owners carry the outcome. | Ordinary/reaction attacks, notifications, debuffs, Acceleration, items, LavaCannon, and movement-only publication share this lifecycle. |

## Planning self-review

- Effective-zero handling maps to Phase 1; shared continuation and the full lifecycle matrix map to Phase 2; publication and shared regression acceptance map to Phase 3.
- The two private settlement functions have different responsibilities. The continuation query reuses `AttackSelectionEvaluation`; it does not create a second gameplay-rule registry.
- Revision 2 reuses the existing `SelectionPreparation` stage data and `SelectionSteps` algorithm. The discarded separate geometry precheck must not reappear during implementation.
- All five Review Focus entries have explicit acceptance cases or owner checks. No test code or execution is silently authorized by listing them.
- No phase implements ordinary movement cancellation, changes initial Haboob policy, or redesigns KillChain targeting/attribution.
- The plan must be revised if design review changes the attack-effect lifecycle interpretation. Each phase checklist records its current progress.
- Independent builder review found no additional planning blockers. The user confirmed post-arrival notification timing on 2026-11-14, resolving the remaining drafting question. No implementation begins from this provisional plan without the user's implementation instruction.
- The subsequent locality review found a two-pass selection risk and insufficient migration criteria in the earlier draft. The new Phase 2 checks and design section 8.1 address those design gaps; this does not constitute code or execution validation.
- Independent revision-2 review found no material additional issue; its terminal-state ownership clarification is included in Phase 2. Runtime verification remains outstanding.
