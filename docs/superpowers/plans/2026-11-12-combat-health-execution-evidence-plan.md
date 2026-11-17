# Combat Health Execution Evidence Implementation Plan

> **For agentic workers:** Use `superpowers:executing-plans` for inline implementation after phase authorization. Use `superpowers:requesting-code-review` for the completed validation cutover. Repository instructions override skill defaults: no commits/push, no new tests or unauthorized test edits, no Unity/MSBuild execution, and stop/report after each Phase.

**Goal:** Validate health claims against the actual execution that produced them, without reconstructing trait behavior from final HP or accepting unrelated changes on the same recipient.

**Architecture:** Evolve the existing `MovementDamageExecution` carrier into immutable health executions, retain numeric movement-use evidence, and associate existing claim occurrences with exact execution steps. Keep the View event stream and all gameplay calculations unchanged; Buffer remains the state authority.

**Tech Stack:** Unity 6000.0.73f1, existing C# Core Contracts/Application and Systems assemblies; no new dependency or Framework service.

**Spec:** [Combat Health Execution Evidence Design](../specs/2026-11-12-combat-health-execution-evidence-design.md).

**Status:** H1–H3 implemented on 2026-11-12. H4 Unity compilation passed and the 20 authorized fixture regressions now pass. Full Core/CoreIntegration EditMode reported 6,592 passed, 36 failed, and 11 ignored. Health-specific and player-build acceptance remain open.

## Global constraints and execution order

- Preserve existing Core paths, Framework contracts, Buffer/Job responsibilities, public View events, result/preview fields, and event ordering. Do not change combat rules to make recording easier.
- Implementation documents are English; code comments/XML documentation are Korean. New `.cs` files require `.meta`; preserve GUIDs when moving an existing asset.
- Do not rerun formulas, add trait-specific validation exceptions, introduce another mutable health-state store, or create a global execution-ID generator.
- Implementation must be explicitly authorized Phase by Phase. Default proposed execution is inline work in this chat; dependencies make parallel implementation of these Tasks unsuitable.
- No automated-test authoring, Unity runs, MSBuild runs, commits, or pushes are authorized. Pure inspection and scoped `git diff --check` are allowed. Compilation remains unverified until the user reports it.
- A necessary change to somebody else's existing test beyond the permitted compile-only adjustment requires asking first. Do not infer agent authorship from the Git committer's name. Do not weaken assertions or remove scenarios.
- H1 -> H2 -> H3 -> H4. H1/H2 retain the current shared-interpreter validation; H3 performs one coherent production cutover only after producer/claim coverage is complete. No permanent missing-receipt fallback.

## Paths and file responsibilities

Use these exact prefixes throughout this plan:

- `C/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/`
- `S/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/`
- `T/` = `Assets/Tests/EditMode/`

| File/group | Responsibility |
|---|---|
| New `C/Contracts/Combat/CombatHealthExecution.cs` + `.meta` | Immutable source/step/execution types. The existing execution type remains as a compatibility base through H2. Other movement receipt types stay in their current file. |
| New `C/Contracts/Combat/CombatHealthClaim.cs` + `.meta` | Immutable reference association from a concrete claim occurrence to canonical steps. |
| `C/Contracts/Combat/CombatCommandTransition.cs` | One canonical execution collection and claim collection; preserve them through copies. |
| `C/Application/Combat/CombatDamageApplication.cs` and existing projection/operation owners | Construct facts from actual operation variables; retain the established command-local collection ownership. |
| `C/Application/Combat/CombatCommandComposition.cs`, `MovementProjection.cs`, `CombatCommandCalculator.cs`, `CombatTurnCalculator.cs` | Preserve and combine operation evidence from every phase, including finalization. |
| New `C/Contracts/Combat/CombatHealthEvidenceValidator.cs` + `.meta` | Pure structural, conservation, coverage, and claim-scope validation. No gameplay formulas or service registration. |
| `C/Contracts/Combat/CombatChangeEvidenceReader.cs`, `C/Application/Combat/MovementUndoPolicy.cs` | Consume validated health facts; retain existing non-health evidence and undo rules. |
| `S/BufferSystems/Combat/CombatStateBuffer.cs`, `S/BufferSystems/Core/BattleTransactions.cs` | Existing publication boundary; call the shared Core validation before any installation. No copied health rules. |

`C/Contracts/Combat/CombatEvent.cs`, `CombatResult.cs`, and View files are compatibility checks, not planned modification targets. If implementation needs to change their public presentation meaning, stop and revise the design.

## Contract decisions used by every Task

### Canonical facts

- `CombatHealthExecution`: a sealed immutable class with `Steps`, `Evidence`, and `MovementUses`. Its identity is the in-memory instance. Migrate `MovementDamageExecution.Collect` behavior to `CombatHealthExecution.CollectMovementUses`; retain current use deduplication semantics.
- `CombatHealthStep`: a sealed immutable class with `RecipientId`, `Before`/`After` health snapshots, `ActualLoss`, `ActualRecovery`, `NormalizationLoss`, `Source`, `EvidenceEvent`, and `EvidenceOrdinal`. The last two identify the existing event carrying that health change and the ordered step within it (for example, Reconstruction loss then restoration). Arrays/lists are copied read-only; step instance identity is preserved through composition.
- `CombatHealthSource`: a small immutable value in the execution file, using existing `EffectOriginKey`, a read-only collection of existing `CombatDamageOrigin` values, and an existing context-event reference where needed. Its operation category distinguishes damage, effect recovery, environment, lifecycle, growth, and normalization; it is not an enum of traits. Numeric/direct-defeat cause retains `CombatDamageCause` when applicable. Damage must have its real origins; an effect-owned step must identify that effect; non-attack operations must supply their actual owner/context rather than an invented attacker or `Unknown` source. Preserve aggregate origins for already-combined damage; do not invent a per-origin split of actual HP loss.
- Construction is internal to the existing Core Application friend assembly. No public mutable builder or setter is added.
- Use `long` for conservation arithmetic, even though snapshot values are `int`. Max-HP-only changes are valid zero-current-delta steps. Reconstruction records actual loss then restoration, with separate causal sources; do not derive either from the net HP difference.
- A genuine zero-health-effect execution may have no positive steps, but must retain its real shield/item/movement evidence where present. No positive health claim can use it as positive damage/recovery.

### Claim locations and membership

- `CombatHealthClaim` contains `Kind`, `Event`, `EntryIndex`, `HitIndex`, `RecipientId`, and read-only step references. `EntryIndex`/`HitIndex` are `-1` when unused; validate selectors, never silently clamp them.
- Supported `Kind` values: `AttackHit`, `ChildAttackHit`, `ArtifactDamage`, `ArtifactRecovery`, `ItemRecovery`, `TraitRecovery`.
- `AttackHit`: a `CombatAttackEvent` or an attack-bearing `CombatRoarEvent`, selected hit index. `ChildAttackHit`: a `CombatEffectsEvent`, selected trait entry's `ChildAttack`, selected hit index. The existing child outcome forbids nested child outcomes in its reactions; do not invent recursive selector infrastructure.
- Artifact claims select an activation entry in `CombatArtifactEvent`; item claims select their `CombatItemEvent`; trait recovery selects a `CombatEffectsEvent` entry and one declared recipient. All selections must match the event type and existing recipient/source fields.
- Primary/child attack summaries may share steps with a specific effect claim. A single claim cannot duplicate a step. Associated steps must come from executions in the same transition, and declaration events must be in its event stream.
- Producers bind directly to returned execution/step references. Do not discover matches by scanning for equal recipient/amount/key. Repeated invocations with identical values remain distinct objects.
- For a trait occurrence also represented in an attack summary, retain the canonical `CombatEffectsEvent` occurrence and its snapshot association. Do not treat a summary copy as a new health execution or guess the occurrence by magnitude.
- Exact actual-amount claims compare exact values, not `>=`: artifact `AppliedAmount`, healing item's `AppliedMagnitude`, and attack damage/recovery/normalization totals. Generic trait-healing declarations preserve their current positive-recovery guarantee; do not assume every trait `Magnitude` means actual HP restored.

### Transition transport

- Final names: `CombatCommandTransition.HealthExecutions`, `.HealthClaims`, and internal `WithHealthEvidence(executions, claims)`.
- H1 retains `DamageExecutions` as a read-only alias of `HealthExecutions` and preserves the existing public constructor signature, including `damageExecutions`. The old execution type supplies its original event and movement-use storage to the new sealed carrier. Each proposal stores one canonical execution list. Remove the compatibility type, constructor and aliases in H3 after the permitted caller/fixture migration.
- `WithCompletion`, `WithFinalState`, `WithMovementEffectUses`, `CombatCommandComposition.WithEvents/Merge`, and `MovementProjection.TransitionFromCompleted` preserve both collections and the same instances.
- `Merge` concatenates actual child executions in operation order. Reusing the same execution as two independently applied operations is invalid; referencing it from two summaries is valid. Neither event insertion indexes nor command numbers are execution identity.
- Nested effects can finish recording before their parent invocation is sealed. Validate health-step chronology by the final event stream's exact `EvidenceEvent` references and `EvidenceOrdinal`, not by record-construction/completion order. This orders already-recorded facts; it does not infer their amounts from event kinds. Preserve the same references when events are inserted for presentation. Every actual health mutation must have a concrete existing evidence event, including the soul event's character changes where applicable.
- Do not attach the projection's evidence by overwriting evidence produced during `BeginFinish`. Finalization, experience, healing and completion contributions must be combined once.

## H1 — Contract and propagation

### H1-T1. Establish the baseline and immutable contracts

**Files:** new execution/claim files above; `C/Contracts/Combat/MovementEffectReceipt.cs`, `CombatCommandTransition.cs`; this plan's execution record.

**Consumes:** existing `MovementDamageExecution`, event classes, health/source types, and `Contracts/AssemblyInfo.cs` friend access.
**Produces:** the exact types and names in the contract decisions; an inventory with no unowned producer/claim row.

- [x] Refresh branch/status and preserve the two prior code fixes and unrelated assets. Inspect any new `AGENTS.md` in the affected paths.
- [x] Inventory `new MovementDamageExecution`, `DamageExecutions`, all transition construction/copy sites, health mutation owners, and health-claim producers. Expand the H2 matrix if a legitimate owner is missing; do not expand gameplay scope.
- [x] Add the immutable contracts and `.meta` files. Migrate execution production to the new carrier and retain the existing type for constructor and reader compatibility. Enforce amount/snapshot validity, source shape, membership-selector shape, and collection immutability at construction.
- [x] Retain legacy execution evidence with empty steps/claims during H1 migration, without claiming it is complete health evidence. Keep current gameplay validation active until H3.
- [x] Inspect negative cases: invalid/max-only snapshots, overflow-safe conservation, foreign recipient, invalid claim selectors, duplicate step reference, and same-value/different-instance invocations. Record runtime checks as pending, not passed.

**Acceptance:** one execution fact owner; no Unity/Buffer/View dependency introduced; existing movement receipts and formulas unchanged.

### H1-T2. Propagate through every copy and composition boundary

**Files:** `C/Contracts/Combat/CombatCommandTransition.cs`; `C/Application/Combat/{CombatCommandComposition,CombatDamageApplication,MovementProjection,CombatCommandCalculator,CombatTurnCalculator,MovementUndoPolicy}.cs`.

**Consumes:** H1-T1 types. **Produces:** `WithHealthEvidence` and preservation through all methods listed in Transition transport.

- [x] Change the existing command-local execution list to the new carrier; retain the existing evidence/movement-use data and collection algorithm.
- [x] Add the claim collection and pass both through constructors, copy methods, `Merge`, `WithEvents`, result finalization, and completion paths. Ensure late-stage evidence is appended/combined rather than replaced.
- [x] Inspect direct calculation, prepared AI, taunt event insertion, pending roar, turn/gimmick composition, rejection, preview/speculative work and stale publication. No evaluation receipt may be reused as evidence for a different committed execution.
- [x] Record fixture impact. Known direct execution-copy/read sites: `T/ReconSquadUndoTests.cs`, `T/TornadoGimmickEvidenceTests.cs`, `T/CoreIntegration/ReserveReturnPublicationTests.cs`. Search all additional constructor callers; keep aliases until the authorized migration can occur.
- [x] Run scoped `git diff --check`, review the identity-preservation diff, update the handoff, and stop/report H1. Do not enable strict receipt validation or start H2 automatically.

**Acceptance:** previous behavior remains the baseline; no lost events, executions, claims, or movement uses; no second list storing the same facts independently.

## H2 — Producers and claim binding

### H2-T1. Capture actual damage, reconstruction and nested item outcomes

**Files:** `C/Application/Combat/{CombatDamageApplication,AttackDirectDamageCalculator,AttackForcedMovement,SacrificeDamagePlanner}.cs`; `C/Application/Artifact/ThornHeartAttackScope.cs`; `C/Application/Combat/Items/ItemCharacterChanges.cs` if its event-return plumbing is required.

**Consumes:** H1 carrier and claim selectors. **Produces:** actual steps from `ApplySteps` / `DefeatSteps`, plus exact returned references for reflection and item-heal claims.

- [x] Capture numeric/direct-defeat loss from the variables already applied by the owner. Preserve `CombatDamageOutcome`'s existing meanings; its `Recovery` is not automatically the total reconstruction restoration.
- [x] Record reconstruction restoration separately from incoming loss and post-damage item recovery. Use actual clamped amounts and current snapshots at each sub-operation, with the correct source for each step.
- [x] Preserve shield/held-defense/zero-damage evidence and movement-use selection. Do not require an HP-decrease event for a valid fully absorbed execution.
- [x] Preserve aggregate thorn/event origins and `resolvedHeldAbsorptions`: record the actual HP loss once, with all actual contributing origins, while retaining each absorbed source's zero-loss/movement evidence. Do not copy the same positive loss into each per-origin record.
- [x] Bind reflection/item claims to the returned steps before the parent result is sealed. Keep presentation event insertion/order unchanged. Speculative clones must own discarded facts, not share committed execution instances.
- [x] Inspect low/full-HP reconstruction, reconstruction shields, lethal/nonlethal reflection, overkill, full absorption, simultaneous item recovery, repeated hits and redirection. Confirm stage/recovery accounting uses the same actual values without double recording.

**Acceptance:** the reflected-loss claim can use its loss step, not the reconstruction/item restoration; no formula or balance change.

### H2-T2. Cover non-damage health owners and cap changes

**Files:** `C/Application/Combat/{MovementProjection,RegenerationTurnApplication,TraitKillSettlement,PositionTileModifierCalculator,CombatTurnCalculator,CombatPlacementCalculator,CombatReserveReturnCleanup}.cs`; `C/Application/Combat/Items/ItemEquipmentCalculator.cs` for return transport if needed; `C/Application/Artifact/{SoulEffectCalculator,ArtifactActionProjection}.cs`; `C/Application/Combat/Experience/CombatExperienceSettlement.cs`.

**Produces:** execution steps for the paths in this matrix, using the same H1 carrier.

| Path | Required source and outcome |
|---|---|
| Regeneration / kill recovery | Existing effect origin and actual recovered HP; bind the specific trait occurrence. |
| Environment/object recovery | Actual object/activation context and clamped recovery; do not label it as an attack. |
| Soul arrival / growth | Existing soul/growth context, actual health/max-health change; retain `CombatSoulEvent` presentation rather than adding fake health events. |
| Position/cap normalization | Before/after health including max HP; normalization loss is not damage and max gain alone is not recovery. |
| Deployment return cap cleanup | `CombatPlacementCalculator.Location` clears the position modifier before relocation. `CombatReserveReturnCleanup.Clear` then removes temporary max HP. Carry both results through the return transition owned by `ItemEquipmentCalculator.RefundReturn`. Trace the existing return/cleanup events as anchors for the ordered steps, including a cap-only change. |
| Experience growth | Capture the actual owner-produced health transition at the existing reward/experience timing. |

- [x] Route each producer's already-computed result into the common recording surface; keep calculations at their existing owners. A small recording helper may collect values, but must not apply the rules again.
- [x] Preserve collectors created during turn cleanup. `CleanupControllerTurn` currently creates a damage application for regeneration; pass its new evidence into the changed transition instead of discarding it.
- [x] Connect any necessary health-source arguments through existing helpers, preserving normal callers with narrowly scoped overloads if needed. Never use a null/unknown origin to bypass a missing source decision.
- [x] Inspect overheal, zero actual recovery, max-only changes, soul collection plus growth in one action, and several independent heals of one unit. Document any additional owner discovered in H1.

**Acceptance:** health outside ordinary attacks is covered; no fake View event or duplicated mutable state is needed to carry the evidence.

### H2-T3. Cover structures, hazards and completion finalization

**Files:** `C/Application/Combat/{MovementProjection,ObstacleReactionCalculator,BattleCompletionCalculator,CombatCommandCalculator}.cs`; `C/Application/Combat/StageGimmicks/{RailwayGimmickRule,RockfallGimmickRule,StageGimmickComposition}.cs`; `S/InGameSystem/CoreSystem/CombatSystem/CombatTransitionPublisher.cs` for compatibility inspection.

- [x] Record actual structure damage separately from movement-only structure events. Preserve obstacle reaction and chained-operation order.
- [x] Record existing fall, railway and rockfall outcomes at their current owners, including the current Reconstruction/defeat policy. Do not funnel them through a different damage policy just to reuse a recorder.
- [x] Trace damage initiated by `DeathTraitApplication` and tornado-driven movement through the existing damage, structure and fall owners. Check the creation/arrival split for placement, summons and nexus reinforcements. Use the created snapshot as the baseline and record subsequent arrival effects through their existing owners.
- [x] Carry battle-completion healing, temporary cap cleanup, experience and finalization evidence into the same final transition. Ensure `MovementProjection.CompletionWork` does not overwrite the evidence added by `CombatCommandCalculator.BeginFinish`.
- [x] Keep `CombatTransitionPublisher` a publisher: `WithCompletion` must preserve evidence; it must not calculate health outcomes a second time.
- [x] Inspect stage-ending commands, compound hazards, structure death, completion healing followed by cap cleanup, and created/removed recipients. Record initialization/removal baselines explicitly; do not treat roster creation as healing an existing unit.

**Acceptance:** every actual health change proposed for publication has evidence, including late phases; gameplay outcome and View event sequence remain unchanged.

### H2-T4. Bind attack scopes and close the producer/claim matrix

**Files:** `C/Application/Combat/{CommandAttackLedger,CombatAttackCalculator,KillChainAttackCalculator,AttackDirectDamageCalculator,MovementProjection,CombatCommandEvents}.cs` and the H2 producer files that emit artifact/item/healing declarations.

**Produces:** claims for every declared health effect and attack hit, with existing capture-point semantics.

- [x] Add execution-reference capture to `CommandAttackLedger.BeginScope` / `CaptureHitHealth` and the existing child-attack scope. Store references/cut points, not a second copy of health amounts or all combat state.
- [x] Bind primary/roar hit claims after the corresponding event is constructed; bind child hits to the existing trait entry's `ChildAttack`. Retain delivery/redirection recipient distinctions and the current before/after capture points.
- [x] Bind child hits after `AttackResultFactory.AttachChild` replaces the provisional effects event. The replacement event is the claim occurrence for the current Core kill-chain path.
- [x] Bind healing declarations to their actual local operation results. Preserve generic trait-heal semantics rather than treating all `Magnitude` fields as applied HP.
- [x] Verify canonical trait occurrence versus attack-summary representation: an alias of an occurrence must not trigger a second execution or borrow a lookalike occurrence's steps.
- [x] Review all inventory rows and outstanding fixtures. Mark each producer, transport and claim adapter connected or explicitly inapplicable with a concrete reason; no unowned/unknown row may pass the H2 gate.
- [x] Run scoped whitespace checks, update handoff and stop/report H2. Keep the old interpreter authoritative until H3; the additional data is not yet a completed strict-validation system.

**Acceptance:** every required claim has its precise step association, including multiple sources on one recipient; no recipient-wide matching or trait-combination exception.

## H3 — Validation cutover

**2026-11-12 implementation status:** H2-T4 scope review fixes and H3 production cutover are implemented; independent review and syntax-only verification are recorded below. Runtime acceptance is pending. Checkboxes requiring runtime evidence remain pending; H4 has not started.

### H3-T1. Implement one pure health-evidence validator

**Files:** new `C/Contracts/Combat/CombatHealthEvidenceValidator.cs` + `.meta`; `C/Contracts/Combat/{CombatHealthExecution,CombatHealthClaim,CombatChangeEvidenceReader}.cs`.

**Interface:** `CombatHealthEvidenceValidator.Validate(CombatCommandTransition transition)` performs the complete check without mutation. `CombatChangeEvidenceReader.ReadAll(CombatCommandTransition transition)` becomes the authoritative reader entry point; its health branches resolve the transition's exact claims.

- [ ] Validate canonical membership, source/context shape, selector validity, recipient agreement, uniqueness within each claim, and event/execution reference preservation. Permit legitimate shared summaries without counting an execution twice.
- [ ] Validate ordered health conservation and complete before/after coverage against current/proposed character and structure health. Order steps using their exact event anchors and per-event ordinals; reject duplicate/conflicting coverage, but retain zero-current-delta max-HP changes. Handle created entities at their actual creation baseline and returned/removed entities according to existing state transitions. Preserve legitimate zero effects.
- [ ] Validate exact artifact/item actual amounts and attack damage/recovery/normalization totals within their recorded scopes. Keep trait recovery's declared semantics. A foreign equal-valued step, unrelated heal, or reused sibling invocation must fail.
- [ ] Check completeness independently of claim totals: missing execution data cannot pass merely because final HP matches, especially full-HP reconstruction and damage-then-recovery with zero net delta.
- [ ] Produce actionable exceptions naming the claim/operation, recipient and source context. Keep all checks before Buffer installation; do not add AI bypasses or catch-and-continue behavior.

**Acceptance:** validation reads executed facts and their associations; it does not rerun trait rules or infer hidden operations from final HP.

### H3-T2. Switch production callers and retire fallback authority

**Files:** `C/Application/Combat/MovementUndoPolicy.cs`; `C/Contracts/Combat/{CombatChangeEvidenceReader,CombatCommandTransition,MovementEffectReceipt}.cs`; composition/projection callers from H1; `S/BufferSystems/Combat/CombatStateBuffer.cs` and `S/BufferSystems/Core/BattleTransactions.cs` only where integration requires it.

- [ ] Complete the fixture-ownership/authorization gate below before changing tests beyond existing permission. If required evidence cannot be checked, leave H3 unaccepted; do not label the migration finished.
- [ ] Route final reconciliation and publication through transition-based validation. Revalidation must leave the same records/undo result unchanged. Preserve numeric movement-source checks; move health completeness into the common health boundary rather than duplicating it in undo.
- [ ] Remove production use of recipient-wide health sums and the event-delta interpreter as authoritative evidence. Event-only classification may remain distinctly named for non-health shape checks; it cannot be a publication fallback.
- [ ] Remove the old execution class and temporary names/aliases once authorized callers are migrated. Audit every transition clone and late finalization path again after cutover.
- [ ] Obtain independent read-only review of the whole H1-H3 change, address actionable findings within scope, run scoped `git diff --check`, update handoff, and stop/report H3.

**Acceptance:** one authoritative health evidence path; missing/foreign data rejects atomically; no new View contract or second health engine. Runtime acceptance remains pending if the user has not run it.

### H3 implementation record — 2026-11-12

- The user authorized H2-T4 review/fixes and H3 in this session. The existing H1/H2 working tree was preserved, with a before-change copy retained for review. No branch, index, asset or View changes were made by this work.
- H2-T4 now preserves immutable attack membership at `CaptureHitHealth` and binds it to the final event/entry using `CombatHealthScope`. Child attacks share the same contract. Reflection preserves its actual `CombatDamageOrigin` reference, not only equal field values.
- Implemented `CombatHealthEvidenceValidator`, switched `MovementUndoPolicy` to transition-based `ReadAll`, and removed the old execution type/aliases and the old undo-owned completeness routine. Existing publication calls already validate before installing state; no Buffer rules were duplicated.
- Standalone sample checking is explicitly named `ReadEventShapes`; production publication never calls it. Existing fixture call-site changes only adapt removed API names and retain their inputs/assertions. The earlier session's reserve normalization test changes were not modified.
- Independent reviews found and resolved the scope and reflection linkage gaps; final source review found no further critical/important issue. The final summary-alias check was separately reviewed against actual event/snapshot sharing.
- Syntax parsing and whitespace checks are static evidence only. No tests were created or executed, no MSBuild/Unity compilation was run, and no commits/push were made. H3 code implementation is complete but runtime acceptance remains pending; H4 is not started. The unchecked acceptance/verification steps above must not be read as a runtime pass.

## H4 — Verification and integration handoff

### H4-T1. Collect user-run verification and regressions

**Files:** this plan's execution record; boundary handoff. Existing tests below are verification targets, not permission to author or change them.

- [x] Compile in Unity and run the full Core/CoreIntegration EditMode regression through the approved AppData-capable batch path. Record the exact working-tree basis, scope, counts, and failures below.
- [ ] User reproduces Ground Compaction departure and lethal reflection/Reconstruction in direct/prepared AI, with the reported artifact loadout and restart sequence in a player build.
- [ ] Verify displayed damage, Reconstruction timing, event ordering and final refresh with the existing View. No planned View code change is a compatibility goal, not a visual acceptance result.
- [ ] Execute the Review Focus cases through authorized tests or explicit user manual checks. Missing automated coverage must be recorded; a manual scenario does not prove the atomic-forgery cases.
- [ ] Investigate any failure under systematic debugging. A scope/design change requires a user decision before implementation; do not patch unrelated features or loosen validation to obtain a pass.

### H4-T2. Finish handoff and extension guidance

**Files:** `docs/superpowers/handoffs/2026-09-24-view-core-data-boundary-handoff.md`, this plan, and the design's implementation-status note.

- [x] Document final producer/claim ownership, before/after modification and understanding scopes, compatibility removal, and measured verification limits.
- [x] Explain how a damage or healing trait uses the existing operation owner, returned steps, declaration link, and shared validator. Document contract review for a new primitive.
- [x] Update the handoff title and date for H4. Retain the two original defect fixes and historical evidence.
- [ ] Stop/report H4. Do not commit/push. Mark complete only when all required implementation and verification are actually complete.

### H4 execution record — 2026-11-12

Unity 6000.0.73f1 compiled the worktree on `feature/Cheon/Core/NewCombatSytem` at `424242ab8836368e6aabc8ec35e014129be04626`. The run selected both EditMode assemblies in full. The pre-run manifest captures SHA-256 for 60 changed source/test files and the worktree status. All 60 hashes matched after the run. Local evidence is under `.superpowers/sdd/2026-11-12-combat-health-execution-evidence/h4-20261112-core-regression/` in `baseline.txt`, `editor.log`, and `results.xml`. Unity returned exit code 2 after writing the test report.

| Assembly | Total | Passed | Failed | Ignored |
|---|---:|---:|---:|---:|
| `ProjectL.Tests.Core` | 5,567 | 5,551 | 8 | 8 |
| `ProjectL.Tests.CoreIntegration` | 1,072 | 1,021 | 48 | 3 |
| **Total** | **6,639** | **6,572** | **56** | **11** |

| Failure group | Count | Observed cause or boundary |
|---|---:|---|
| `CombatCommandPublicationTests` | 4 | The `Replace` helper copies an attack transition without its canonical health executions and claims. |
| `CombatRewardPublicationTests` | 4 | The `WithReward` helper loses health evidence. Strict validation fails before the intended reward guard. The same four case names passed in the H2 reserve-fix run. |
| `TraitStatePublicationTests` | 4 | The `Replace` helper drops health evidence from an otherwise valid attack. The control publication in each case now rejects the incomplete transition. |
| `NexusReinforcementPublicationTests` | 2 | Two synthetic proposals append a death event without an executed loss for the created unit. |
| Frozen snapshots | 6 | `AttackPreviewParityTests` (2) reports unregistered health contract types. `EnemyActionAssessmentTests` (1) compares a frozen transition containing `DamageExecutions`. `AttackPreviewResumptionTests` (3) first diverges at `TargetReconstructs`, which predates the cutover. The full snapshot differences require review before updating references. |
| Other failures | 36 | 23 UI/camera cases, 11 database/Run Gold/AI tuning cases, and 2 trait contract/activation cases. The run identifies their failing assertions or setup errors. A same-revision pre-change full-suite comparison is unavailable. |

The initial run preceded the authorized fixture changes. The copy helpers in `CombatCommandPublicationTests`, `CombatRewardPublicationTests`, and `TraitStatePublicationTests` now retain the transition's undo and health evidence. Two `NexusReinforcementPublicationTests` scenarios use `CombatDamageApplication` to obtain the death event and evidence for the created unit. Existing publication assertions still verify the installed state and creation behavior.

The snapshot migration compares 86 attack, 4 movement continuation, and 7 route vectors with their original frozen outputs. Every existing gameplay value and ordered event matches after accounting for the health evidence fields and the two Reconstruction flags introduced after the original capture. All 97 legacy execution receipts preserve their evidence and movement uses in order. The [new reference directory](../reports/2026-11-12-combat-health-execution-evidence/) freezes the complete current output, including the 111 additional health executions across projections and executions. The original November 7 reference files remain available. `AttackPreviewResumptionTests` consumes the revised references through `AttackPreviewParityTests` and retains its existing checks for partitions 1, 2, and 31.

### H4 fixture regression — 2026-11-12

Unity 6000.0.73f1 compiled the repaired fixtures and reran both full EditMode assemblies. Local evidence is under `.superpowers/sdd/2026-11-12-combat-health-execution-evidence/h4-fixture-regression/` in `editor.log` and `results.xml`. Unity reported exit code 2 after the test report was written.

| Assembly | Total | Passed | Failed | Ignored |
|---|---:|---:|---:|---:|
| `ProjectL.Tests.Core` | 5,567 | 5,557 | 2 | 8 |
| `ProjectL.Tests.CoreIntegration` | 1,072 | 1,035 | 34 | 3 |
| **Total** | **6,639** | **6,592** | **36** | **11** |

All 69 cases in the four repaired publication suites and six frozen checks passed. All 20 targeted failures became passes. Comparing the full case list to the initial H4 result found no other outcome changes. The remaining 36 failures match their initial H4 full names and messages. This comparison uses two H4 runs after the H3 cutover.

| Review focus | Executed evidence and remaining gap |
|---|---|
| Same target and unrelated recovery | Reflection and item publication suites passed. Existing standalone `ReadEventShapes` tests do not exercise a forged claim borrowing a different recovery step on the same target. Direct health-claim forgery and identical-source sibling cases remain open. |
| Zero net HP change | Reconstruction, Thorn Heart reflection, and movement effect usage suites passed. The exact lethal-reflection plus full-HP Reconstruction combination, the original loadout/restart, and omitted zero-net health facts still need targeted verification. |
| Late and non-attack paths | Reserve return 4/4, experience settlement 20/20, Rockfall publication 10/10, Tornado publication 5/5, and completion publication 3/3 passed. Both reinforcement death cases passed with canonical execution evidence. Ordered multi-source finalization with a forged or missing step remains unverified. |
| Scope and identity | Kill-chain, Sacrifice, and prepared enemy advancement suites passed. All six frozen parity checks passed. The original Ground Compaction and Thorn Heart/Reconstruction reproductions in direct and prepared AI still require the player build. |
| Atomic failure and revalidation | Movement-effect publication 10/10 and movement undo publication 33/33 passed, including rejection and repeat-publication coverage for movement facts. The missing-health-evidence fixture rejections establish the thrown guard. Direct omitted, duplicate, foreign, and wrong-source health-step tests that assert every Buffer store stays unchanged remain open. |

The [boundary handoff §5.92](../handoffs/2026-09-24-view-core-data-boundary-handoff.md#592-health-evidence-integration-verification-h4-2026-11-12) records extension guidance and the player-build checklist. Runtime acceptance requires resolving the remaining full-regression failures and collecting the health-specific and original player-build/View evidence.

## Existing verification targets and fixture gate

At H1, inspect direct constructors and copy fixtures with `rg`, and record the exact files, necessary change, and known/unknown authorship before touching them. Current targets include:

| Concern | Existing files/suites |
|---|---|
| Health evidence shape and rejection | `T/CombatChangeEvidenceTests.cs`, `CombatChangeEvidenceCoverageTests.cs`, `CombatChangeEvidenceVariantTests.cs` |
| Actual damage / reflection / items | `T/CombatDamageBoundaryTests.cs`, `CombatItemDamageSourceTests.cs`, `ArtifactThornHeartRuleTests.cs`, `CombatCommandEffectTests.cs` |
| Reconstruction and its lifecycle | Existing `T/Reconstruction*Tests.cs` and `T/CoreIntegration/EnemyDeathTraitPublicationTests.cs` |
| Movement and numeric uses | `T/MovementEffectUsageTests.cs`, `ReconSquadUndoTests.cs`, `MovementUndoLifetimeTests.cs`, `T/CoreIntegration/MovementEffectPublicationTests.cs`, `MovementUndoPublicationTests.cs` |
| Publication / completion | `T/CoreIntegration/CombatCommandPublicationTests.cs`, `CombatCompletionPublicationTests.cs`, `CombatItemPublicationTests.cs`, `CombatRewardPublicationTests.cs` |
| Direct/prepared AI and speculation | `T/EnemyDecisionSessionTests.cs`, `EnemyPreviewPreparationTests.cs`, `EnemyMoveThenAttackTests.cs`, existing CoreIntegration enemy publication suites |

The scan already found three direct execution-copy/read fixtures named in H1-T2. It does not establish that these are the only affected fixtures, nor who authored them. Runtime fixture repairs requiring new evidence are not automatically compile-only edits. Present that concrete list and obtain authorization when required. Do not create new automated tests under the current instructions; if adequate negative/combination coverage needs them, request a specific exception before H3 acceptance.

## Review Focus

These five checks are required at their owning Tasks and H4. The H4 execution record identifies the exercised cases and remaining coverage.

1. **Same target, unrelated recovery:** an item/artifact claim cannot borrow a reconstruction or another heal; identical repeated sources remain distinct. Owners: H2-T1/T4, H3-T1.
2. **Zero net HP change:** full-HP reconstruction, damage followed by recovery, and full absorption retain their actual operations without inventing loss or losing source evidence. Owners: H2-T1, H3-T1.
3. **Late/non-attack health paths:** soul growth, cap changes, structures, hazards and completion evidence survive finalization rather than being overwritten; nested execution seal order must not reorder actual health changes. Owners: H2-T2/T3, H1-T2, H3-T1.
4. **Scope and identity:** child attacks, redirection, roar, taunt insertion, AI wrapper changes, previews and stale prepared work preserve only the correct committed references. Owners: H1-T2, H2-T4, H3-T2.
5. **Atomic failure and revalidation:** omitted, duplicated, foreign or wrong-source facts cannot partly update Buffer; a valid transition passes repeated reconciliation/publication validation without changing undo metadata. Owners: H3-T1/T2, H4-T1.

## Plan self-review and execution record

- Spec coverage: contracts/identity -> H1; every observed health producer and claim -> H2; validation/completeness/atomicity -> H3; View and runtime acceptance -> H4. Future extension and SSOT/FW boundaries are carried through all phases and the final handoff.
- Task order keeps dependent contract/producer/validator changes sequential. No phase is an authorization for the next one.
- Type/property names and claim selectors above are the shared Task interfaces.
- Planning checked the actual source paths, existing copy methods, effect snapshot fields and current working-tree changes. Further producer/fixture inventory is explicitly H1 work, not an excuse to omit a discovered path.

### H1 execution record — 2026-11-12

- Baseline inspected on `feature/Cheon/Core/NewCombatSytem` at `424242ab8`. The two prior Core fixes and unrelated FMOD, font, render and settings changes were carried forward. Git status used a command-local LFS filter override after the default filter could not write its temporary object. The flagged PNG matched the indexed LFS pointer by SHA-256.
- Automatic review rejected immediate removal of `MovementDamageExecution` and replacing its implementation with an abstract facade. The retained concrete base, existing public constructor, and read-only aliases keep H1 callers compatible. Existing movement evidence is stored once on the same carrier instance. The old constructor can adapt a legacy-only execution at intake. Normal Core production creates only `CombatHealthExecution`. Remove the compatibility path with the authorized H3 migration.
- Inspected the direct movement and attack paths, presence reconciliation, direct and prepared AI, taunt insertion, pending roar, turn/gimmick composition, preview and speculative copies, rejection, and the stale/cancelled publication guards. The prepared decision supplies an action choice to fresh execution. The publication completion wrapper carries the resulting references. Projection finalization appends the evidence returned by `BeginFinish`; the late health producers themselves belong to H2.
- The H2 producer inventory covers numeric damage and defeat, nested items, Reconstruction, trait recovery, environment and souls, growth and caps, structures and hazards, and terminal healing. Deployment return has two cap-normalization owners, now listed in H2-T2. Undo rejects position normalization that would alter restored facts. Battle preparation and creation supply baselines. The explicit abandonment service publishes separate `BattleCompletionChanges`; H1 changes the command-transition path.
- The claim inventory covers primary and roar hits (`CombatAttackCalculator` and `CommandAttackLedger`), kill-chain child hits (`KillChainAttackCalculator` and `AttackResultFactory`), reflection (`ThornHeartAttackScope`), terminal artifact recovery (`BattleCompletionCalculator`), item recovery (`CombatDamageApplication` and `ItemCharacterChanges`), and trait recovery (`RegenerationTurnApplication` and `TraitKillSettlement`). The `FollowUpAttack` contract has no Core production emitter. Artifact attack-power contributions, damage mitigation and temporary item effects are observation or non-health state declarations.
- The fixture scan found 23 files with direct transition constructors and 25 files including execution reads and movement-use copies. All existing constructor calls retain the public signature. The exact execution read/copy sites and H3 impact are below. Authorship is unconfirmed for all three.

| Fixture | H1 result | H3 migration to assess |
|---|---|---|
| `T/ReconSquadUndoTests.cs` (195, 227) | The execution assertion and the damage fixture's copy use the compatibility properties. | Migrate to canonical executions and carry claims through the fixture copy where required. |
| `T/TornadoGimmickEvidenceTests.cs` (178–179) | The reconstructed transition preserves legacy execution references through the existing constructor. | Carry the canonical execution/claim references and preserve the intended event mutations and failure assertions. |
| `T/CoreIntegration/ReserveReturnPublicationTests.cs` (73) | The assertion uses the retained legacy property signature. | Reassess the empty-execution expectation against return cap normalization in H2. An expectation change requires the existing fixture authorization gate. |

- Static review checked default and max-only snapshots, wide arithmetic, source shape, selector bounds, foreign recipients, duplicate references, and the distinction between equal-valued invocations and the same invocation. Scoped Git whitespace validation covered tracked changes and new files. Compilation, Unity, MSBuild, and runtime tests remain unverified. H1 ends at this review point.

### H2 execution record — 2026-11-12

| Producer or claim | Connected evidence and scope |
|---|---|
| Numeric damage, direct defeat, absorbed hits | `CombatDamageApplication` seals the applied loss, retains the existing movement-use selection, and retains shield or item events for zero-loss executions. Aggregate thorn loss is recorded once. Absorbed sources can retain their separate movement evidence. Finishing Shot, Crash Impact, periodic status and Self Destruct pass their existing effect origins. |
| Reconstruction, reflection and item recovery | Reconstruction has a depletion step and a restoration step with separate sources and event ordinals. Reflection binds only the returned loss step. A healing item binds its own clamped recovery step. A zero-amount declaration carries an empty step list. |
| Regeneration and Call of Blood | Each producer links the exact effects occurrence to its own clamped healing step. Turn cleanup carries regeneration evidence through the existing turn merge. |
| Environment and structures | Object recovery carries its activation. Generic tile rules also carry the evaluated source fact, including persistent tiles with no activation event. Terrain, obstacle and structure damage retain their existing origins. Barrel chains carry the reaction occurrence, and movement-only structure events yield no health step. |
| Soul arrival, caps and experience | Soul collection records each applied snapshot in consumed-soul order on the existing soul event. Position changes and reserve cleanup carry normalization with the exact existing events. Experience passes the growth event references into finalization, which seals evidence against the final applied event after any owned-record rewrite. |
| Fall, railway, rockfall and tornado | Existing fall and railway rules record direct loss. Rockfall preserves its own clamping and Reconstruction behavior. Tornado movement reuses arrival, normalization and hazard producers. |
| Completion and baselines | The existing artifact allocator supplies applied amounts in activation order. The terminal health record carries those steps and the later cap correction. `BeginFinish` adds experience and terminal evidence, and movement completion combines them with the projection's executions. Creation uses the initial character facts as the baseline. Placement and later arrival use the normal operation owners. Abandonment uses the separate completion service described in H1. |
| Primary attack, roar and kill-chain hit claims | Existing before and hit-health capture points delimit the canonical steps for each actual recipient. The child binds after the provisional activation is replaced. Attack summaries share the original trait snapshots. Local speculation uses separate projection collections, and the publisher receives the actual command execution. |

- Source inspection covered lethal, overkill and absorbed damage, Reconstruction at low and full HP, zero/clamped healing, max-only changes, nested operations, actual versus intended attack recipients, and late finalization. The existing shared interpreter remains the publication authority through H2.
- `ReserveReturnPublicationTests.MaxHpNormalization_HasNoNumericDamageOrRecoveryCredit` currently asserts that the legacy execution list is empty. H2 adds a normalization step to that same list. Static inspection predicts the assertion will fail. The H3 fixture migration must assert zero numeric loss/recovery and the expected normalization while preserving the remaining checks. The existing test was left untouched.
- The H1 `ReconSquadUndoTests` and `TornadoGimmickEvidenceTests` copy fixtures still use the compatibility constructor. H3 must carry the canonical claim references when those fixtures reconstruct transitions. No fixture authorship was inferred.
- Scoped Git whitespace checks and a C# 9 syntax scan cover the changed source. Compilation and runtime verification remain pending. H2 ends at this review point.
