# Piercing and Dead Hand Implementation Plan

> **For agentic workers:** Use `superpowers:executing-plans` for inline execution, or `superpowers:subagent-driven-development` if the user selects that method. Execute the approved phase only, then stop and report. Repository instructions override generic skill suggestions to commit, push, or continue automatically.

**Goal:** Deliver the revised Piercing trait and reusable Dead Hand basic-attack effect, SoulEater InGame data, and View-facing request/result APIs without adding acquisition content or implementing View.

**Architecture:** Keep basic geometry in targeting definitions, resolve trait activation through the existing effect capability/context pattern, and execute Dead Hand before the normal damage stage on the existing command projection. Share compatibility validation, side normalization, and calculation between preview and execution. Reuse existing forced movement, damage/Sacrifice settlement, trait-growth resolution, and Buffer publication.

**Tech Stack:** Unity 6000.0.73f1, existing C# Contracts/Domain/Application/Systems assemblies, ScriptableObject authoring, NUnit EditMode Core/CoreIntegration tests. No new packages.

**Spec:** [Piercing Revision and Dead Hand — SoulEater Design](../specs/2026-11-14-piercing-and-dead-hand-design.md)

**Status:** Phase 0 and Phases A–E are complete. The final Core and CoreIntegration EditMode run selected 6,835 cases and reproduced the 36 Phase 0 failures with zero new failures. View integration remains open.

**Locality review:** The planning review on 2026-11-14 incorporated the user-supplied `feature-change-locality-guidelines.md`. The execution records document the resulting implementation boundaries and verification.

## Global constraints

- Implementation documents are English; new code comments and XML documentation are Korean. Preserve existing Core paths and spelling, including `CoreCaculator`.
- No commit or push. Do not change unrelated user work, production loadouts, shop/reward/level-up pools, FieldCamera, Architecture, or retired assets.
- Implement Core, InGame SOs, and Buffer/request/result contracts only. View controls, formatters, animations, prefabs, scenes, and visual acceptance are outside scope. Do not author View tests or run combat View MSBuild.
- New Core tests use test-owned inputs; production SO verification is confined to authoring/integration cases. Existing third-party tests are not rewritten to hide a changed behavior; report any test-ownership conflict before altering them.
- Run required Core/CoreIntegration checks through Unity with AppData access allowed. Do not delete `CurlRequestCache.db`, upgrade Unity, or infer passing tests from an Editor process exit alone.
- New or moved C# files require `.meta` handling; preserve existing GUIDs. Pure InGame/Contracts/Domain types do not reference UnityEngine.
- Preserve existing Buffer ownership, Job processing, command identities, stale-request checks, event order, result acknowledgement, and Framework registration contracts.
- Every phase ends at a review point. New scope or a contradiction with confirmed gameplay requires a report, not an unannounced change.

## Review focus

1. Equal rank values: 100/100/100 must be valid only under the new explicit definition policy; old definitions retain their validation. Covered by B2.
2. Extra-attack leakage: Kill Chain calls common damage again; it must neither sweep nor inherit/reapply Dead Hand's coefficient. Covered by D3.
3. Target identity after movement: moved targets remain hit candidates, dead targets do not receive another hit, and Sacrifice uses final positions without moving the protector. Covered by D2/D3.
4. Intent/default propagation: explicit Left must survive copies and service assembly; omitted side must equal relative Right for both preview and execution. Covered by C2/E1.
5. Geometry and hidden state: attack walls must not become movement-passable, row endpoints cannot depend on enemy positions, and preview must not reveal hidden environment facts or mutate live state. Covered by C1/D2/E1.

## Paths and current evidence

The repository root is `D:/My_ProjectWork/Github_desktop/Project_L`. The aliases below expand to exact repository-relative prefixes in every task file list:

| Alias | Prefix |
|---|---|
| `C/` | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/` |
| `S/` | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/` |
| `B/` | `Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/` |
| `D/` | `Assets/ProjectL/Script/Systems/DataBaseSystem/Data/InGame/` |
| `A/` | `Assets/ProjectL/DataBaseSO/InGameGroup/` |
| `T/` | `Assets/Tests/EditMode/` |

Source inspection found:

- Piercing already has ordered straight travel, falloff, budget, rounding, and result evidence. Its current delivery uses selection eligibility, which excludes allies under the ordinary policy.
- Sniper currently references `TargetingMethod_1` (RadialSquare/Single), has range 4, and has no native traits in its asset. Verify effective defaults before inserting Piercing.
- `CombatAttackProfile` validates trait combinations; `TraitGrowthRules.EligibleTraits` already constructs hypothetical profiles. Keep one compatibility authority.
- `TraitGrowthDefinition` rejects adjacent equal specs. Default 100/100/100 needs an opt-in policy, not an identity-specific bypass.
- `AttackDamageEffectContext` and `CommonAttackDamage` currently lack attack kind; Kill Chain reuses common damage.
- `AttackRotation` returns a `Rotation4` for aimed attacks and R0 for All; All sweeps must instead use the actor's existing field rotation.
- `AttackKnockbackSnapshot` and result validation assume ordinary post-hit knockback. Use a distinct sweep aggregate; do not weaken ordinary hit/knockback correspondence to fit movement-before-damage.
- Verified Editor executable during planning: `D:/Program Files/Unity/Hub/Editor/6000.0.73f1/Editor/Unity.exe`. Recheck before execution. No Editor was running during the process inspection. No Unity tests were run for this plan.

## Technical decisions shared by tasks

### Change-locality acceptance criteria

Evaluate actual edit scope, required understanding, and verification together. File/interface counts are not acceptance criteria. The following source-based cases make the boundary goals concrete; expected improvements remain unverified until implementation.

| Real change and source evidence | Cause / judgment | Minimal planned change and edit scope | Understanding before -> after | Verification that remains necessary |
|---|---|---|---|---|
| Basic-only Dead Hand activation and rank damage: `CommonAttackDamage` is called by both normal attacks and `KillChainAttackCalculator`, while the effect evaluation currently lacks attack kind | Shared damage calculation is legitimate. Repeating the new activation test in every caller would be avoidable coupling; it is a risk of this change, not a proven existing Dead Hand defect | D1/D3 pass existing `CombatDamageKind` through the evaluation boundary; one rule decides eligibility. Rank resolution stays in B2 and existing owned-spec composition | Initial implementer must inspect both entry paths. Later activation edits require the effect contract and its owner, without reading Kill Chain target selection/death-loop internals; caller classification remains a required contract | Normal/extra/reaction entry classification, coefficient application once, other active effects, preview parity. These regressions remain because the calculation is shared |
| Pack selected enemies toward a boundary: `AttackForcedMovement.ExecuteUnitSteps` owns entry legality/terrain; `PullAttackCalculator.ExecuteSteps` performs a separate forced-arrival call; `AttackDeliveryExecutor` owns post-hit knockback | Ordered occupancy and movement-triggered effects are necessary coupling. Last-cell object timing and the separate arrival call would be a hidden dependency if described only as "reuse movement" | D2 owns packing and calls the explicit pre-hit movement contract below. D3 owns the one transition from all movement to damage; existing Pull/Knockback bodies stay intact unless their shared helper contract actually needs a change | Initial implementer traces movement internals. Later packing edits require entry/arrival/terminal postconditions, not each terrain effect or Sacrifice implementation; post-movement damage remains an explicit boundary | Walls, hazards, interruption, one arrival per completed move, Sacrifice position changes, and existing Pull/Knockback regressions are necessary because state and stage order are shared |
| Add another approved incompatible displacement trait: `CombatAttackProfile` has pair checks, `TraitGrowthRules.EligibleTraits` builds profiles, and `OwnedUnitSpecResolver.Compose` combines native/default/owned definitions | Same compatibility rule at multiple entry points is required; separate pair lists in each caller would be avoidable coupling | B1 declares the group/pair once in a pure contract and invokes it at effective-profile and composed-spec boundaries. Keep unrelated method restrictions where they are | A membership edit needs the exclusion declaration and effective-trait input contract, not each shop/level-up candidate loop. Registration and actual trait behavior still require their normal owners | Candidate filtering, effective defaults, restored records, and profile rejection remain necessary; unrelated shop pricing/UI behavior does not become part of the focused check |

The current full Core/CoreIntegration baseline and final runs remain mandatory. Focused tests explain *why* a boundary is affected; locality does not justify omitting full required checks.

### Behavioral contracts at feature boundaries

The signatures in tasks are only part of their contracts. Preserve the following entry conditions, state ownership, ordering, lifetime, and failure behavior.

| Boundary | Entry / ownership | Output and ordering guarantee | Failure / lifetime |
|---|---|---|---|
| `TraitCompatibilityRules.Validate` | Receives the effective trait set, including defaults and owned/native entries; pure Contracts code, no catalog/global lookup | No mutation; one group declaration and explicit pair rule. Invoked before publishing a changed owned spec or accepting an invalid profile | Invalid composition throws the established argument/configuration failure. Existing candidate filtering may translate that rejection into "not offered"; do not turn malformed persisted state into silent trait removal |
| Rank resolution | Definition SO/data owns rank values and equal-value policy; OwnedCharacterRecord owns actual rank history | Returns an immutable effective spec; no mutation of source SO or a second runtime percent field | Unknown/missing/inconsistent rank remains an error. Do not silently substitute 100% when input is missing; 100% is authored initial data |
| `DeadHandAttackCalculator.Prepare` | Uses one command's immutable input, accepted selection, active contribution, and resolved basic geometry | Captures only derived area/target/side/facing facts. Does not move units, charge actions, append events, or publish Buffers | Invalid intent follows existing rejection semantics; invalid configuration fails explicitly. Preparation cannot be reused for another battle/query revision or treated as permission to commit preview destinations |
| `ExecuteSteps` | The attack orchestrator owns one `CommandAttackProjection`, ledger, and arrival projection created by `world.CreateArrivalProjection(plan)`. All share the same command world | Mutates that transient world only; processes targets in spatial order; emits bounded work steps and one completed sweep result on normal iterator completion. Per-target terrain/arrival occurs before the next target; all movement ends before attack damage | Blocked/immune are successful outcomes. Terminal state records Interrupted/NotProcessed and prevents later attack damage. Exceptions/disposal do not publish a partial world or invent a successful result; callers use existing completion/publication ownership |
| Damage settlement | Uses captured original target IDs and the current post-movement world; common damage receives explicit attack kind | Dead Hand coefficient applies once on the basic attack; normal defenses/Sacrifice/deaths remain their owners. A protector's damage receipt does not schedule another sweep | Existing normal death/terminal behavior remains. Do not reuse pre-movement victim positions or let copied extra-attack inputs imply basic-attack eligibility |
| Result/event publication | Command service publishes a completed transition through `CombatTransitionPublisher` and existing Buffer transactions | Sweep aggregate is immutable derived evidence. The result and header use the same finalized sweep data; ordinary Moved/terrain/health events remain the state transitions. The header never re-applies movement | No new publisher/queue. Preserve failure atomicity and request identities; stale preview completion is discarded by the current preview service |

For the pre-hit movement contract, D2 uses `AttackForcedMovement.ExecuteUnitSteps` with `deferLastObjects: false`, `publishMovedEvents: true`, and Forced/DeadHand movement cause. The helper settles visited-cell terrain and last-cell objects; it does **not** replace the separate forced-arrival callback. After a non-interrupted positive-length path, if the target is alive at the recorded final path cell and combat is not terminal, call the existing `arrival.ResolveForcedArrivalSteps` once before processing the next target, following the existing Pull contract. Zero-step, immune, dead, externally relocated, and interrupted outcomes do not synthesize an arrival. Do not enqueue those same sweep moves for `AttackKnockbackCalculator.ResolveArrivalsSteps` after damage. Add tests that fail if last-cell objects or forced arrival are executed twice or skipped.

### Assembly, orchestration, and abstraction limits

- `TraitRegistry` / trait factories construct and bind rule objects; SO/spec adapters translate configuration. Adding their registration entry is expected. Packing, damage math, and target eligibility do not move into them.
- `CombatAttackCalculator` / `AttackDeliveryExecutor` legitimately coordinate execution stages. Keep the sequence visible there; do not create one Bootstrap/Factory/service per stage simply to shrink the coordinator file.
- The new trait definition/state/factory types are required by the existing registration contract. The sweep capability extends the existing effect dispatch model; no new plugin system, automatic reflection registration, or generalized trigger engine is justified.
- Pure facing extraction is warranted because aimed attacks and sweep calculation need one existing convention, and Domain cannot call Application. Keep the old Application method as the compatibility entry; do not add several delegation layers.
- An immutable preparation object exists to freeze derived command facts across preview/execution steps. A sweep result exists because pre-damage movement/death cannot satisfy the existing post-hit knockback contract. Neither becomes authoritative mutable state or a parallel occupancy model.
- Where an existing type/helper already meets the documented semantic contract, reuse it rather than preserving a proposed new file for its own sake. A local code-layout adjustment that preserves these boundaries is routine; a changed ownership/public/gameplay contract requires the existing phase review process.

### Phase exit evidence

Include a short locality record in each phase report: actual changed files/functions; the feature rule and connection contracts a later editor must understand; which unrelated internals no longer need inspection; remaining shared-state/order dependencies; tests run and their dependency-based reason. Mark source observations, expected improvement, and runtime evidence separately. Do not claim improvement merely because code moved to another file.

If a change reaches another shared file, determine whether it follows an already planned dependency/contract change or exposes avoidable coupling. Normal registration/copy/adapter work is allowed within scope. Record out-of-scope cleanup as a finding; do not bundle it into this implementation. No additional approval is needed solely because a normal assembly file changes.

### Identifiers and geometry

- Add `TraitKey.DeadHand = 530`, `EntityKey.Unit_SoulEater = 27`, and `TargetingShape.ForwardRectangle = 6`; recheck for concurrent allocations before editing. Never renumber existing values.
- Reserve targeting method keys 10 for Sniper cardinal Single and 11 for SoulEater rectangle Direction, subject to the same collision check. Do not edit shared method 1.
- Append `int width = 0` / `Width` to targeting SO/data/spec transport. Existing shapes keep width 0 and their current behavior. ForwardRectangle requires positive width; depth remains `UnitSpec.AttackRange` and its normal range-resolution path.
- Rectangle offsets are `origin + depth * forward + lateral * right`, depth 1..range; lateral runs from `-(width / 2)` through `-(width / 2) + width - 1`. Width 3 is centered. Even widths have a deterministic local-axis anchor; this is basic geometry authoring, never a Dead Hand restriction or recentering rule.
- Default selection uses the union of legal directional masks. SoulEater has six cells in one direction and 20 in the four-direction union. Deduplicate intersections and exclude the actor tile.
- Sweep endpoints use the outermost attack-mask tile in the chosen direction on each target's sweep row, plus one tile. Use the complete resolved basic geometry before occupancy/occlusion clipping, not the occupied-target bounding box. For Single, use its basic legal range mask for the endpoint and its actual selected enemy for membership; for Direction use the chosen directional mask; for All use its selected full mask. Existing splash cells, if supported by the base profile, are part of its resolved attack geometry. A hole inside a mask is not the outer boundary.
- Movement still tests each intervening cell; nominal endpoints beyond the board do not shift inward or permit leaving the board.

### Intent and public evidence

- Add public `AttackSweepSide { Left = 1, Right = 2 }` in `C/Contracts/Common/AttackSweepSide.cs`. Null means omitted; explicit 0 or any other value is invalid.
- Append `AttackSweepSide? sweepSide = null` to the command/preview attack factories and transport constructors. Keep `AttackAim` unchanged.
- Use `AttackSweepGeometry.ResolveSide(AttackSweepSide? side)` as the only default resolver. Extract the existing pure `AttackRotation` mapping into `C/Domain/Combat/AttackFacingCalculator.cs` as `Resolve(AttackAim aim, CellCoord origin)` and keep the existing Application method as a delegating wrapper. `AttackSweepGeometry.ResolveFacing(AttackAim aim, CellCoord origin, Rotation4 currentFacing)` uses that shared Domain helper for Cell/Direction and currentFacing for All. Domain must not call Application, and the mapping must not be duplicated. Publish that resolved facing; do not mutate other attacks' facing or add diagonal facing states.
- Add immutable `AttackSweepSnapshot` and `AttackSweepTargetSnapshot` in `C/Contracts/Results/`. Aggregate fields: effect origin, actor ID, facing, resolved side, original area cells, captured target IDs, ordered target outcomes, and interrupted status. Target fields: target ID, start, intended endpoint, entered sweep cells, nullable actual final anchor, and `AttackSweepStop` (`Completed`, `Blocked`, `Immune`, `Interrupted`, `NotProcessed`). Include zero-step and unprocessed outcomes. Reuse existing forced-path results to build them; no second movement state.
- `AttackResult.Sweep` and `AttackPreview.Sweep` are optional and distinct from `Knockbacks`/`Pull`/`PiercingShot`. Sweep target identities may outlive a hit entry because a target can die during movement. Validate each field's actual contract instead of requiring every sweep target to have a later damage hit.
- Add `CombatSweepEvent` with `CombatEventKind.Swept = 50` after checking numeric collisions. Publish a typed sweep group header before its existing movement/terrain/damage events, analogous to the current Pull header pattern. Retain individual state-change events as the authoritative state transitions; header paths are presentation evidence, not an instruction to commit positions again.

### Policy and effect ownership

- Add pure `TraitCompatibilityRules.Validate(IReadOnlyList<TraitSpec> traits)` in `C/Contracts/Commands/Traits/`. One declaration owns the displacement group `{Knockback, Pull, ImpactBolt, CrashImpact, DeadHand}` and the additional pair `{PiercingFalloff, DeadHand}`. Existing unrelated method/combination checks remain. Do not expand Piercing into the whole displacement group.
- Invoke that rule from the effective profile boundary. Make effective owned-trait composition validate too when it can be returned without profile construction; reuse the same rule and existing exceptions. Existing candidate filtering then benefits from the same check without new acquisition entries.
- Add one capability within the existing effect model: `IEffectAttackSweepRule.EvaluateAttackSweep(in EffectRuleInput input, in AttackSweepEffectContext context)` returning `EffectAttackSweepContribution?`. Context contains the existing `CombatDamageKind`; contribution contains its `EffectOriginKey` and resolved rank damage percent. The engine aggregates at most one active contribution, retains existing origin/state validation, and exposes `EvaluateAttackSweep(bindings, context)` through `ICombatEffectEngine`.
- Dead Hand's rule returns a contribution only for `OrdinaryAttack`. Absence of this contribution leaves ordinary targeting/damage stages unchanged. Preview/range requests for a basic attack use that same kind; reaction callers pass ReactionAttack explicitly. No blanket removal of other effect bindings.
- `CommonAttackDamage(input, actor, profile, CombatDamageKind damageKind, events = null, traitScope = null)` receives kind explicitly at every call. Compute the existing effective common attack power (including its existing bonuses/debuff order), then scale it once by the active sweep contribution with `CategoryMatchupDamageCalculator.Apply(power, percent, 100)`, before per-target damage settlement. 100% preserves ordinary damage. Extra/Reaction calls have no active contribution, including Kill Chain recomputation and fallback paths.
- The same active sweep evaluation supplies the Dead Hand selection envelope (enemy targets, ignore line-of-sight walls) and pre-damage stage. Keep these decisions in the preparation/effect boundary, not independent trait-key checks in View, AI, and several calculators. Wall-occupied tiles and movement collision retain current rules.

## Verification commands

During implementation, use the approved AppData-access launch route (`require_escalated` where required by this host). Never run a second Editor against an open project. If an Editor owns it, coordinate the equivalent EditMode run in that Editor; do not terminate it. The project does not currently list `com.unity.pipeline`; do not install it as a side effect of this work.

One focused batch invocation, with a fresh output directory and the task's explicit filter:

```powershell
$unityEditor = 'D:/Program Files/Unity/Hub/Editor/6000.0.73f1/Editor/Unity.exe'
$projectPath = 'D:/My_ProjectWork/Github_desktop/Project_L'
$runDirectory = Join-Path $projectPath ('Library/CodexTests/PiercingDeadHand/' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Path $runDirectory -Force | Out-Null
$resultPath = Join-Path $runDirectory 'results.xml'
$logPath = Join-Path $runDirectory 'unity.log'
$filter = 'ProjectL.Tests.Core.PiercingRevisionTests'
& $unityEditor -batchmode -nographics -projectPath $projectPath -runTests -testPlatform EditMode -testFilter $filter -testResults $resultPath -logFile $logPath
```

Do not add `-quit`. Launch/poll asynchronously through the agent tool as necessary to keep progress updates possible. For final broad verification replace `-testFilter $filter` with `-assemblyNames 'ProjectL.Tests.Core;ProjectL.Tests.CoreIntegration'`. Require fresh XML, executed expected cases, zero unexpected failures, and a log without compiler/test-runner errors. Record fixture counts, results and log paths. A zero exit without XML is not a pass. Compare failures with Phase 0; never mask an unrelated baseline failure.

## Phase 0 — Baseline and execution readiness

**Files:** read-only repository/Editor inspection. Test evidence is stored under `Library/CodexTests/PiercingDeadHand/`.

- [x] Read the spec, root/Assets AGENTS, framework guide, and this plan. Confirm the approved execution phase and native/worktree context. A worktree, if needed, is an execution concern, not a prerequisite to this document.
- [x] Capture branch, status, and the existing FMOD/font/settings modifications. Check the target files for user edits; never restore or stage unrelated files.
- [x] Recheck IDs, the Editor executable, process ownership, and the approved AppData access route.
- [x] Run the full Core and CoreIntegration baseline once using the command above with assembly selection. Record any existing compilation/fixture failures separately; do not claim a green baseline if blocked.
- [x] Report evidence and stop. No product change is made in this phase.

**Execution record (2026-11-14)**

- Checkout was `feature/Cheon/Core/NewCombatSytem` at `3cfd325d5480673fbade0eea301a9fcc7e90b79f`. Root and Assets instructions, the linked design, and the Framework contracts were checked. There was no earlier execution record for this plan or product diff on its target files. The existing FMOD, font, rendering, and project-settings edits were captured separately.
- The occupied IDs were checked in code and registered assets. Method keys 10/11, entity key 27, trait key 530, shape 6, and event 50 were free. Unity 6000.0.73f1 compiled and ran through the AppData-enabled batch route.
- Core selected 5,633, passed 5,620, failed 2, and skipped 11. CoreIntegration selected 1,072, passed 1,035, failed 34, and skipped 3. All 36 failures predate Phase A. The XML lists the exact cases, including camera type load, DB and Deck placement/UI cases, and two existing trait-activation failures.
- Evidence resides in `Library/CodexTests/PiercingDeadHand/phase-0-baseline/{results.xml,unity.log}`. Unity deleted the requested Temp XML on exit. The fresh XML was recovered from the Editor's AppData results file before another test run.
- Changed files at this checkpoint include this plan's execution record only. The baseline exercises both shared assemblies because subsequent phases change selection, effect registration, and data transport. Its failures are comparison evidence for the later regression run.

## Phase A — Narrow Piercing revision

### A1. Separate hostile aim validation from projectile victims

**Modify:** `C/Application/Combat/CombatAttackQueries.cs`, `CombatAttackDelivery.cs`; `C/Domain/Combat/PierceTargetCollector.cs` only if an explicit victim-input contract is needed; `D/Table/Unit/Trait/UnitTraitAuthoring.cs` defaults.

**Create tests:** `T/PiercingRevisionTests.cs` (+ meta).

**Interfaces:** preserve `PiercingShotDelivery`, encounter indices, and current straight trace signature. Selection retains hostile occupied aims; delivery obtains living allied/enemy character candidates excluding the actor. A local shared delivery-candidate helper belongs with the current delivery calculator, not a new service. Preserve current structure/neutral rules and straight-line alignment validation.

- [x] Add failing tests: enemy aim with an ally before it hits ally first; friendly/empty aim rejected; targets beyond authored range hit; four combined ally/enemy encounters exhaust the shot even before the aim; damage 100/75/50/25; a different compatible carrier can still aim diagonally; low-power rounded-zero stop and wall/structure stops unchanged.
- [x] Run `ProjectL.Tests.Core.PiercingRevisionTests`; confirm a behavioral failure against the old eligibility path.
- [x] Implement the aim/delivery separation in the shared calculation, preserving ordinary targeting permissions for other traits and all primary/encounter correspondence checks. Do not set a global friendly-aim policy for Piercing.
- [x] Set new Piercing authoring initializers to 25 and 4; keep persisted definition migration explicit in A2.
- [x] Run the new fixture and existing `PiercingShotTraceTests`, `PiercingShotCommandTests`, `PiercingShotContractTests`, `PiercingFalloffTraitTests`, `PiercingShotAiTests`, and attack-target-policy fixtures. Preserve preview/AI delivery parity through shared code.

### A2. Configure Sniper without changing shared targeting

**Create:** `A/Targeting/Item/TargetingMethod_SniperCross.asset` (+ meta).

**Modify:** `A/Targeting/TargetingMethodDataTable.asset`, `A/Unit/Item/Playerable/Unit_Sniper.asset`.

**Create tests:** `T/CoreIntegration/PiercingAuthoringTests.cs` (+ meta).

**Interfaces:** method key 10 = CardinalLine/Single/no splash/Unrestricted; Sniper retains range 4 and unrelated stats. Effective Piercing definition = 25% reduction/cap four exactly once.

- [x] Add authoring assertions for the dedicated method, unchanged method 1, Sniper range, and one effective Piercing trait. Compare category/default/native composition before deciding which existing source supplies the trait.
- [x] Create/register the dedicated targeting SO and assign it to Sniper. If no effective Piercing source exists, author the native trait; otherwise update its existing source without duplicating it or changing other units unintentionally.
- [x] Run `ProjectL.Tests.CoreIntegration.PiercingAuthoringTests` and `DataBaseAssetTests`. Check the asset diff contains no acquisition/loadout or View edits.
- [x] Report Phase A's code/asset changes and validation, then stop.

**Execution record (2026-11-14)**

- Source changes: `CombatAttackQueries.ValidatePrimary` checks the accepted primary's hostility for Piercing. `CombatAttackDelivery.ResolveSteps` expands character eligibility at projectile delivery. `AttackDirectDamageCalculator` now resolves delivered victims against the current projection so an allied projectile victim uses the ordinary character damage pipeline. Structure and neutral delivery permissions remain governed by the existing policy.
- Authoring changes: `UnitTraitAuthoring.cs`, `TargetingMethod_SniperCross.asset` and its meta, `TargetingMethodDataTable.asset`, and `Unit_Sniper.asset`. Sniper had no native Piercing and its Main category has no default assignment. Sniper alone receives a native 25%/4-target trait and method key 10. The shared method 1 retains its original data.
- Test changes: new `PiercingRevisionTests` and `PiercingAuthoringTests` plus metas. Existing `PiercingShotCommandTests`, `AttackTargetAdmissionTests`, and `ObstacleReactionTests` were aligned with occupied hostile aims and allied projectile victims. Cannon's empty-Single allowance remains on regular attacks. The agreed Piercing rule takes precedence for a Piercing profile.
- RED selected 7 cases with 4 expected failures on the old delivery and authored method. The final focused run selected 475, passed 467 and reproduced 8 Phase 0 `DataBaseAssetTests` failures. Both new fixtures passed, along with the selected targeting, wall, obstacle, AI, rounding and delivery-contract fixtures. Results and logs are in `Library/CodexTests/PiercingDeadHand/phase-a-{red,verified}/`.
- Locality: future aim changes enter at primary validation. Victim rules enter at projectile delivery. The terrain/result/damage contracts still require joint verification because they consume the ordered encounter list. The full regression belongs to the final A–C checkpoint.

## Phase B — Dead Hand definition, rank policy, and compatibility

### B1. Add the trait through existing registration boundaries

**Create:**
- `C/Contracts/Commands/Traits/DeadHandTraitSpec.cs`
- `C/Domain/Traits/Definitions/DeadHandTraitDefinition.cs`
- `C/Domain/Traits/Rules/DeadHandTraitRule.cs`
- `C/Domain/Traits/States/DeadHandRuntimeState.cs`
- `C/Domain/Traits/Effects/DeadHandEffectRule.cs`, `DeadHandEffectStateSnapshot.cs`
- `C/Application/Combat/Traits/DeadHandTraitFactories.cs`
- `C/Contracts/Commands/Traits/TraitCompatibilityRules.cs`
- `T/DeadHandDefinitionTests.cs`, `T/AttackDisplacementCompatibilityTests.cs`

**Modify:** `C/Contracts/Common/TraitKey.cs`; `C/Application/Combat/Traits/TraitRegistry.cs`; `C/Application/Combat/DeferredCombatFeatures.cs`; `C/Contracts/Combat/CombatAttackProfile.cs`; `C/Application/Enhancements/OwnedUnitSpecResolver.cs`; `D/Table/Unit/Trait/UnitTraitAuthoring.cs`, `UnitTraitData.cs`; `S/CharacterSystem/TraitSpecAdapter.cs`, `MainSpecializationDefinitionSnapshot.cs`; `Assets/ProjectL/Script/Systems/DataBaseSystem/Editor/UnitTraitAuthoringDrawer.cs` only as required by its current explicit authoring registration.

**Interfaces:** `DeadHandTraitSpec(int damagePercent)` and matching pure data/definition expose `DamagePercent` (nonnegative integer, above 100 allowed). Factories follow existing stateless trait conventions. A runtime state/snapshot satisfies the registry contract without storing duplicate positions, attack range, or per-command direction. Compatibility uses the pure shared method defined above.

- [x] Add tests for spec validation, immutable equality/copy semantics, data-to-spec registration, all approved exclusion pairs in both orderings, and allowed SteadfastWill/DeadHand. Test category-default plus owned/native compositions rather than only a hand-built list.
- [x] Implement definition/adapters/registry and the one compatibility declaration. Profile/candidate/owned resolution consumes that declaration; do not duplicate it in the editor drawer or View. Keep existing unrelated profile restrictions.
- [x] Trace native/default/owned composition -> validation -> candidate rejection or persisted profile publication, and verify the check receives the effective set at each entry point. This trace is initialization/validation work, not a reason to refactor shop pricing or acquisition flows.
- [x] Mark Dead Hand `ImplementationPending` using the existing availability mechanism until Phase D is complete; do not publish a production trait that silently does nothing. Foundation tests may instantiate its pure rules directly. Preserve registration completeness conventions.
- [x] Run the two new fixtures, registry fixtures, `OwnedTraitGrowthTests`, `TraitGrowthRulesTests`, and relevant specialization/copy coverage. Inspect any existing conflicting authored loadout and report it rather than deleting a trait silently.

### B2. Support the approved equal initial rank values

**Modify:** `C/Contracts/Enhancements/TraitRankValues.cs`, `TraitGrowthDefinition.cs`; `D/Table/TraitGrowth/Item/TraitGrowthDefinitionDataSO.cs`, `TraitGrowthDefinitionData.cs`, `TraitRankAuthoring.cs`; `S/CharacterSystem/TraitGrowthDefinitionAdapter.cs`, `MainSpecializationDefinitionSnapshot.cs`.

**Create tests:** `T/DeadHandRankTests.cs`, `T/CoreIntegration/DeadHandRankAuthoringTests.cs`.

**Interfaces:** append optional `bool allowUnchangedRankValues = false` to growth definition/data constructors; expose `AllowUnchangedRankValues`, author `allowUnchangedRankValues` in the SO, and carry it through adapters/snapshot identity. Add `DeadHandRankAuthoring` returning `TraitRankValues(TraitKey.DeadHand, damagePercent)`. Extend `FromNormal` and `Apply` for `DeadHandTraitSpec` using the existing one-amount path.

- [x] Add failing tests: 100/100/100 accepted with the policy and rejected without it; existing default rejection unchanged; missing Rank2 with Rank3 rejected; distinct fixture values 100/125/150 resolve by current rank; rank purchases/restores advance rank even when initial numbers match.
- [x] Gate only the adjacent-SameValues validation with the explicit definition policy. Keep all type/key/rank-slot and number checks. Add the typed rank mapping and authoring default 100.
- [x] Verify immutable definition identity detects a changed policy/percentage and that battle trait settings come from the same effective spec. No extra runtime percentage override or View rule lookup.
- [x] Run both new fixtures, `TraitGrowthCatalogTests`, `NewTraitRankTests`, `TraitTerminalRankTests`, and CoreIntegration growth-authoring/stat-settlement fixtures. Stop with Phase B report; no production Dead Hand asset is activated yet.

**Execution record (2026-11-14)**

- B1 added the Dead Hand spec, definition, stateless rule/state/snapshot, effect rule, and factories with metas. `TraitKey`, `TraitRegistry`, and `DeferredCombatFeatures` register the type and reject runtime use with `ImplementationPending`. `UnitTraitAuthoring`, `UnitTraitData`, `TraitSpecAdapter`, and `MainSpecializationDefinitionSnapshot` carry the authored percent. The existing type-discovery drawer already detects the new authoring type.
- `TraitCompatibilityRules` owns the displacement group and the Piercing/Dead Hand pair. `CombatAttackProfile` and `OwnedUnitSpecResolver.Compose` call it. Candidate enumeration already builds the hypothetical profile and catches the argument failure. Restored choices are validated after native/default/acquired composition. Authored sources had no conflicting production assignments. The existing Knockback/Crash Impact combination test was updated to verify the approved rejection.
- B2 changed `TraitGrowthDefinition`, `TraitRankValues`, `TraitGrowthDefinitionDataSO`, `TraitGrowthDefinitionData`, `TraitRankAuthoring`, `TraitGrowthDefinitionAdapter`, and specialization definition identity. The opt-in bypasses the adjacent equal-value comparison only; rank-slot and value validation still run. Restored purchases and effective settings use the same rank resolver.
- Added `DeadHandDefinitionTests`, `AttackDisplacementCompatibilityTests`, `DeadHandRankTests`, and `DeadHandRankAuthoringTests` with metas. Compatibility RED reproduced five permitted conflicts. Rank RED reproduced missing Dead Hand rank resolution in both cases. The focused run passed 421/421. Evidence resides in `Library/CodexTests/PiercingDeadHand/phase-b-{red,rank-red,verified}/`.
- Locality: editing exclusivity now requires the effective input contract and one compatibility declaration. Rank tuning depends on definition data, immutable snapshot identity, and the owned history path. Focused registration, composition, authorship, and specialization tests cover those dependencies. Runtime activation and production Dead Hand assets are pending.

## Phase C — Basic rectangle geometry and sweep API contracts

### C1. Add forward rectangle as basic attack geometry

**Modify:** `C/Contracts/Common/TargetingShape.cs`; `C/Contracts/Commands/TargetingMethodSpec.cs`; `D/Table/Targeting/Item/TargetingMethodDataSO.cs`, `TargetingMethodData.cs`; `S/CharacterSystem/UnitSpecAdapter.cs`, `MainSpecializationDefinitionSnapshot.cs`; `C/Domain/Combat/AppointShapeCalculator.cs`, `AppointRangeCalculator.cs`, `AttackSelectionResolver.cs`; `C/Application/Combat/CombatAttackQueries.cs`.

**Create tests:** `T/ForwardRectangleTargetingTests.cs`, `T/CoreIntegration/ForwardRectangleAuthoringTests.cs`.

**Interfaces:** `TargetingMethodSpec(..., TargetingChoiceAngle choiceAngle = Unrestricted, int width = 0)`; corresponding data/SO `Width`. Extend the existing mask operations with an optional width or a method-spec overload, keeping old signatures behavior-compatible. All production call sites pass the method's width; no hard-coded width in Dead Hand.

- [x] Add tests for width 3/depth 2 = six selected cells and 20 unique total cells, all four rotations, 3x3, 2x2 deterministic anchoring, range changes, boundary clipping, and unchanged type-5 range-2 direction mask of ten cells.
- [x] Implement width transport and mask generation/containment consistently for range, selection, and candidate enumeration. Include Width in method-value identity such as MainSpecializationDefinitionSnapshot so a changed width invalidates stale prepared definitions. Existing shapes with width 0 keep their original semantics. Count/overflow guards must match allocated masks.
- [x] Run the new fixtures plus `TargetingMethodGeometryTests`, `TargetingMethodSelectionTests`, `AppointRangeCalculatorTests`, `TargetingPreviewParityTests`, and angle fixtures. Confirm no View geometry implementation was added.

### C2. Carry optional side and immutable sweep results

**Create:** `C/Contracts/Common/AttackSweepSide.cs`; `C/Contracts/Results/AttackSweepSnapshot.cs`, `AttackSweepTargetSnapshot.cs`; `C/Contracts/Combat/CombatSweepEvent.cs`; `C/Domain/Combat/AttackSweepGeometry.cs`, `AttackFacingCalculator.cs`; `T/AttackSweepContractTests.cs`.

**Modify:** `B/CombatView/CombatViewCommand.cs`, `CombatViewPreview.cs`; `S/CombatSystem/CombatActionRequest.cs`, `CombatCommandService.cs`, `CombatPreviewService.cs`, `CombatInputAssembler.cs`; `C/Contracts/Combat/CombatCommandInput.cs`, `CombatEvent.cs`; `C/Application/Combat/CombatCommandComposition.cs`, `CombatCommandCalculator.cs`; `C/Contracts/Results/AttackResult.cs`, `AttackPreview.cs`.

**Interfaces:**
- `CombatViewCommandData.Attack(EntityId attacker, AttackAim aim, long expectedBattleInstanceId, long expectedControllerTurnSequence, AttackSweepSide? sweepSide = null)`.
- `CombatViewPreviewData.Attack(EntityId attacker, AttackAim aim, AttackSweepSide? sweepSide = null)`.
- `SweepSide` travels unchanged through request/input. Common copy construction, WithWorld/WithActor, and existing same-intent WithIntent preserve it. Add an explicit-side WithIntent overload when composing a genuinely new intent; passing null explicitly clears it. Update automatic/reaction composition deliberately rather than accidentally inheriting a player's choice.
- Sweep DTOs use the fields and enums in Technical Decisions. Add optional `sweep = null` at the end of result factories/constructors and propagate it in `AttackPreview.FromFacts`; ordinary callers remain unchanged.

- [x] Add tests for omitted versus explicit Right, explicit Left, invalid enum, all four facing mappings, All retaining current facing, existing aimed facing conventions, copy preservation/explicit clearing, immutable collections, and ordinary result construction with no sweep.
- [x] Add the transport and DTOs. Keep command numbering and expected battle/turn checks intact. Do not add a direct View-to-Core execution route or new command queue.
- [x] Document the constructor/WithIntent preserve-versus-clear contract beside the shared copy boundary and test it there; transport adapters forward intent and must not invent their own default-side rule.
- [x] Validate sweep aggregate membership/order without requiring movement-only deaths to have hit results. Do not relax existing ordinary Knockback or Piercing validation.
- [x] Run `AttackSweepContractTests`, existing request/copy/result contract tests, and CoreIntegration command/preview transport checks. Stop with Phase C report; runtime trait remains pending.

**Execution record (2026-11-14)**

- C1 passes `Width` through `TargetingMethodSpec`, `TargetingMethodDataSO`, `TargetingMethodData`, `UnitSpecAdapter`, and prepared specialization identity. `AppointShapeCalculator`, `AppointRangeCalculator`, `AttackSelectionResolver`, and `CombatAttackQueries` share forward rectangle masks and containment. Rectangle steps maintain bounded work and ordered unique output. Both old and new masks remain subject to board checks at the existing selection/range boundary.
- C2 carries optional `SweepSide` through `CombatViewCommand`, `CombatViewPreview`, `CombatActionRequest`, the command/preview services, `CombatInputAssembler`, and `CombatCommandInput`. The common copy preserves intent and new automatic, assessment, pursuit, and reaction intents clear it. The calculation and transport files are `CombatCommandComposition`, `CombatActionAssessment`, `EnemyAttackReach`, `CombatAttackCalculator`, `CombatCommandCalculator`, `AttackFacingCalculator`, and `AttackSweepGeometry`.
- `AttackSweepSide`, `AttackSweepSnapshot`, `AttackSweepTargetSnapshot`, and `CombatSweepEvent` are new contracts with metas. `AttackResult` and `AttackPreview` carry optional sweep evidence independent of hit membership. `CombatEvent` reserves value 50. `CombatChangeEvidenceReader` records the group header as an observation so state-change evidence continues to come from individual movement, terrain, and health events.
- The independent review found four existing boundaries that needed width support. `CombatStageSoAssets` and `CombatStageSoRange` forward the existing authoring tool's serialized width. `EnemyEngagementField` and `TraitGrowthRules` read the same maximum base distance before filtering possible origins or distance-damage growth. The limited-direction union checks its allocation bound before enumerating. The sweep aggregate verifies that each nominal endpoint is one cell past the corresponding complete-mask row.
- Added `ForwardRectangleTargetingTests`, `ForwardRectangleAuthoringTests`, `AttackSweepContractTests`, and `AttackSweepTransportTests` with metas. Updated `CombatCommandInputCopyTests`, `EnemyEngagementFieldTests`, `CombatChangeEvidenceCoverageTests`, and `TargetingMethodContractTests`. `AttackPreviewParityTests` preserves frozen fixture format by omitting only the newly added optional defaults and still serializes explicit values. The `RegenerationTraitTests`, `IntimidationTraitTests`, and `LegacyEnhancementRetirementTests` registry checks now account for the registered pending definition. Follow-up rank tests verify prepared identity and existing training behavior with equal values, including Dead Hand's pending gate.
- The revised geometry RED reproduced nine unsupported-shape failures. The first complete focused run passed 321/321. The post-review focused run passed 348/348. An interim full run found 13 additional expectations to update for the new optional fields, event kind, shape number, and pending registry entry. The stable preview/route fixture schemas are preserved by omitting only the new optional default values.
- The next full regression reproduced the 36 baseline failures and found two A8 reference differences plus one preview-resumption timeout. An isolated rerun passed the preview partitions 1, 2, and 31 in 120.3, 61.7, and 7.2 seconds. A side-by-side capture showed only five A8 records per tuning differed. The same selected paths, shots, hits, and state hashes were preserved. The change is in the follow-up candidate ranking and score for two hypothetical movement paths. Updated only those five rows per file in `docs/superpowers/reports/2026-11-13-enemy-ai-reuse-evidence/baseline-{default,current-tuning}.json`. All other rows, including the A8 stale-actor rejection case, matched byte for byte.
- Final baseline comparison selected 6,758 and passed 6,708, with 36 failures and 14 skips. Core passed 5,667 of 5,680 selected, with 2 failures and 11 skips. CoreIntegration passed 1,041 of 1,078 selected, with 34 failures and 3 skips. Failure names and messages match Phase 0 exactly. All 53 added cases passed. There were no C# compiler errors. Evidence is in `Library/CodexTests/PiercingDeadHand/phase-c-baseline-comparison/{results.xml,unity.log}`. The previous full XML and isolated rerun are in `phase-c-final/` and `phase-c-ai-capture/` under the same folder.
- Locality: geometry owns the width, anchor, union, and maximum distance. AI and growth depend on the distance query. The authoring tool depends on the serialized width and existing mask APIs. Side intent depends on the common copy boundary and existing service adapters. Sweep results share immutable evidence with the typed header. Runtime preparation, pre-damage movement, publication ordering, SoulEater assets, and visual verification remain for D/E. The interactive Editor overlay was not played or inspected.

## Phase D — Shared sweep evaluation and pre-damage execution

### D1. One activation/preparation rule

**Create:** `C/Domain/CombatEffects/Contexts/AttackSweepEffectContext.cs`, `Evaluations/EffectAttackSweepContribution.cs`, `Evaluations/EffectAttackSweepEvaluation.cs`; `C/Application/Combat/DeadHandAttackEvaluation.cs`, `DeadHandAttackCalculator.cs` (preparation method); `T/DeadHandActivationTests.cs`.

**Modify:** `C/Domain/CombatEffects/Rules/CombatEffectCapabilities.cs`, `ICombatEffectEngine.cs`, `CombatEffectEngine.cs`; `C/Domain/Traits/Effects/DeadHandEffectRule.cs`; `C/Application/Combat/CombatAttackQueries.cs`, `AttackSelectionEvaluation.cs`, `CommandAttackPlan.cs`, `CombatAttackCalculator.cs`.

**Interfaces:** the capability/context/contribution signatures are defined in Technical Decisions. `DeadHandAttackEvaluation` holds only captured command facts: active contribution, resolved facing/side, complete basic mask, and actual selected enemy IDs. `DeadHandAttackCalculator.Prepare(input, actor, profile, selection, contribution)` returns that immutable evaluation. This is per-command derived data, not persistent state.

- [x] Add tests that the rule contributes for OrdinaryAttack only, keeps origin/state expectations, ignores extra/reaction kinds, and leaves other effect contributions untouched. Verify enemy-only Single/Direction/All target membership and ignore-line-of-sight selection without making walls traversable.
- [x] Evaluate sweep eligibility before selection occlusion. Thread actual attack kind through preparation for basic preview/execution and reaction callers. Use active contribution presence as the shared preparation condition, with no repeated trait-name exceptions.
- [x] Build the complete range mask using current attack-range/angle/category restrictions and the C1 shape rules; keep that geometry separate from the resolved target identities. Current enemy occupancy determines targets, not endpoints.
- [x] Verify preparation has no mutation/publication side effects and belongs to one command input. The later executor consumes this contract rather than repeating target selection after movement.
- [x] Run `DeadHandActivationTests`, targeting/admission regression, and existing Roar/extra-attack tests. No new native-game activation flag is removed until D3 passes.

### D2. Ordered movement using the current forced-movement engine

**Create:** `T/DeadHandMovementTests.cs`, `DeadHandTerrainTests.cs`, `DeadHandTestData.cs`.

**Modify:** `C/Domain/Combat/AttackSweepGeometry.cs`; `C/Application/Combat/DeadHandAttackCalculator.cs` (execution), `CombatAttackCalculator.cs`, `CommandAttackPlan.cs`; reuse `AttackForcedMovement.cs`, `KnockbackImmunityCheck.cs`, `MovementProjection` and environment calculators. Change a shared helper only where needed to expose its existing contract, not to fork legality.

**Interfaces:**
- `AttackSweepGeometry.Destination(IReadOnlyList<CellCoord> areaCells, CellCoord start, CellCoord step)` returns the row's outer boundary plus one cell with checked coordinates.
- `DeadHandAttackCalculator.ExecuteSteps(DeadHandAttackEvaluation evaluation, CommandAttackPlan plan, CommandAttackProjection world, CommandAttackLedger ledger, MovementProjection arrival, Action<AttackSweepSnapshot> completed)` returns bounded `IEnumerable<string>` work steps.

- [x] Add tests with attacker (3,1), forward Up, mask x=2..4/y=2..3: a single enemy reaches x=5 on right sweep; enemies x=2,3,4 finish x=3,4,5; left sweep mirrors this. Rotate the same cases. Include Single with a non-target unit inside range blocking the one moving target.
- [x] Add tests for walls, board edge, ally/outside target blockers, immune target remaining damage-eligible, enemies on both sides of a blocker, vacated cells, zero-step outcomes, and endpoints independent of target placement.
- [x] Add direct calculation/projection tests for terrain: a moving target dies and stops; subsequent targets use the freed cell; prior completed moves do not rerun; confirmed termination stops remaining work. D3 adds the no-later-attack-hit assertion through the full attack path. These focused tests instantiate the calculation directly while public trait activation remains pending. Reuse existing terrain/object/forced-arrival timing, including interruptions that move a target away from the sweep row.
- [x] Implement near-edge-first processing per row on the existing command projection. Apply the explicit pre-hit movement/arrival contract above, including `deferLastObjects: false`, `publishMovedEvents: true`, Forced/DeadHand attribution, and exactly one eligible arrival before the next target. Never repeat sweep arrival in the post-damage knockback stage or defer all terrain merely to animate simultaneously.
- [x] Add boundary tests for last-cell object settlement exactly once, forced arrival exactly once, zero-step/immune outcomes without synthesized arrival, and interruption/disposal leaving the original input facts unchanged. Tests observe paths/events/facts rather than mirroring private helper calls. E1 owns the actual Buffer-publication failure check, so these pure Core tests do not acquire a Systems/Buffer dependency.
- [x] Preserve paths and actual post-environment destinations separately in sweep evidence. Record NotProcessed outcomes when termination prevents later targets from moving. Build and insert the group header at the recorded pre-movement event boundary after the calculation is complete, following existing immutable header publication conventions.
- [x] Run the new movement/terrain fixtures plus `KnockbackImmunityPolicyTests`, `KnockbackPathTests`, `PullTerrainTests`, and movement-undo/environment regressions. Keep normal damage-first Knockback order unchanged.

### D3. Damage once, after movement, with normal Sacrifice

**Modify:** `C/Application/Combat/CombatAttackCalculator.cs`, `CombatAttackDelivery.cs`, `AttackDeliveryExecutor.cs`, `AttackDirectDamageCalculator.cs`, `CommandAttackPlan.cs`, `KillChainAttackCalculator.cs`; `C/Domain/CombatEffects/Contexts/AttackDamageEffectContext.cs` only if needed by the chosen explicit-kind path. Reuse `SacrificeDamagePlanner.cs` and normal settlement; do not duplicate their logic.

**Create tests:** `T/DeadHandDamageTests.cs`, `DeadHandSacrificeTests.cs`.

**Interfaces:** use the explicit-kind `CommonAttackDamage` signature from Technical Decisions. The original target set survives movement. Build damage hit inputs against post-movement facts without re-running range/occlusion membership. The new sweep stage completes before `AttackDirectDamageCalculator.ApplySteps`; normal attacks preserve their current pipeline.

- [x] Add tests: attack 6 at 100% = nominal 6; 15 at 50% = 8 and 15 at 150% = 23 before defenses; no duplicate ordinary-plus-trait hit; moved-outside-mask and immune/blocked surviving targets still hit; dying targets omitted.
- [x] Add a basic attack at 200% plus Kill Chain fixture proving basic damage 12 from attack power 6, while the extra attack uses its ordinary base 6 and does not sweep. Include the common-damage fallback path and a second unrelated damage effect to prove no blanket binding removal.
- [x] Add Sacrifice tests: original victim moves, post-movement eligibility can appear/disappear, protector takes redirected damage without extra movement, protector originally captured moves only once, and existing consumption/capacity rules still apply.
- [x] Wire pre-hit movement into the current command sequence, then normal damage/Sacrifice/death/attack-completion settlement. Keep stage coordination in its current owner and packing out of it. Preserve causality, action use once, event ordering, original actor identity, and terminal interruption behavior. Do not recompute a new target set after packing or damage kills a blocker.
- [x] Resolve the Dead Hand coefficient from the current rank-aware spec. Pass Ordinary/Extra/Reaction kinds explicitly at every common-damage call and preview/assessment entry. Remove the temporary ImplementationPending status only when D1-D3 behavior passes.
- [x] Run D fixtures, `AttackPreviewParityTests`, `AttackPreviewResumptionTests`, existing damage/Sacrifice/KillChain/Pull/Knockback families, and relevant trait-state tests. Report and stop at Phase D.

**Execution record (2026-11-14)**

- D1 adds an attack-sweep capability/context/contribution to the existing effect engine. The Dead Hand rule owns OrdinaryAttack eligibility and the rank-resolved percentage; ExtraAttack and ReactionAttack receive no contribution. Selection preparation uses that contribution for enemy-character membership and through-wall line-of-sight delivery while preserving wall-occupied-cell rejection, category restrictions, and movement collision. The immutable preparation retains the complete basic mask, captured target IDs, facing/side, and input identity; it cannot execute against another command input.
- D2 adds the focused sweep calculator and checked row-endpoint geometry. It processes each row from the chosen edge inward on the command's existing projection. It reuses AttackForcedMovement with immediate last-cell objects, Forced/DeadHand moved-event attribution, and exactly one eligible separate forced arrival before the next target. It retains entered paths separately from actual final anchors, records immune/blocked/interrupted/not-processed outcomes, and inserts one immutable sweep header at the pre-movement boundary. Normal post-hit Knockback is unchanged.
- D3 connects the sweep before normal direct damage. Delivery retains the accepted target identities, reads surviving targets from the post-movement world, and leaves normal defenses, Sacrifice, deaths, action-use settlement, and completion in their existing owners. Original captured enemy IDs survive plan rebuilding. CommonAttackDamage now requires the actual CombatDamageKind; basic calls apply the coefficient once after the existing bonuses/debuffs, and Kill Chain recomputes ExtraAttack damage even without a parent plan. Reaction execution and preview use ReactionAttack. AttackResultFactory shares the finalized sweep with the result/preview and preserves movement-only target evidence.
- Runtime activation required the standard DeadHandTraitStatusSnapshot, TraitEffectProvider, CombatCommandEffects, and TraitEffectStateCodec adapters in addition to the planned rule. The snapshot adds no duplicate percentage field. ImplementationPending was removed with the complete runtime path and validated by the definition/rank tests. Existing tests of the earlier pending state were updated; unrelated existing test changes are limited to the required common-damage kind argument. Five AI files only classify their common-damage calls as OrdinaryAttack; no strategy or side-search change was made.
- New test-owned fixtures are DeadHandActivationTests, DeadHandMovementTests, DeadHandTerrainTests, DeadHandDamageTests, DeadHandSacrificeTests, and DeadHandTestData. D1 RED reproduced missing contribution/origin checks; D2 RED reproduced stationary targets; D3 RED reproduced the pending activation gate. The first integrated focused run passed 177/177. Expanded focused verification selected 705 (702 passed, two new fixture/assertion failures, one skip); the new wall-geometry and actor-blocker inputs were corrected without changing movement rules. A final boundary RED also identified unvalidated default attack kind; the effect boundary now rejects it. Final boundary verification passed 68/68, including all 54 new Phase D cases, rank/registration cases, separate arrival, last-cell mine, Tornado interruption, no-parent Kill Chain, Sacrifice eligibility/capacity, and resumable previews.
- Final full regression selected 6,812 cases: 6,762 passed, 36 failed, and 14 skipped. Core selected 5,734 (5,721 passed, two failed, 11 skipped); CoreIntegration selected 1,078 (1,041 passed, 34 failed, three skipped). All 54 new Phase D cases passed. Every failure name and message matches Phase 0 exactly; there are no new failures or C# compiler errors. Fresh evidence: `Library/CodexTests/PiercingDeadHand/phase-d-full/{results.xml,unity.log,baseline-comparison.json}`. Boundary evidence is in `phase-d-boundary-green/`; earlier RED/focused attempts remain under the same evidence root.
- Independent read-only review found no confirmed production defect. Its requested arrival/relocation/fallback evidence is present in the final boundary run. Multi-cell ordinary attacks remain rejected by the existing admission contract. Buffer failure atomicity, stale/hidden-state integration, assessment-specific publication, registered SoulEater content, and View handoff remain Phase E work.
- Locality: activation/percentage edits now require the sweep effect rule and the explicit attack-kind contract; packing edits require the sweep calculator plus the existing forced-entry/arrival/termination contracts. The coordinator keeps stage order visible, and damage/Sacrifice owners keep their current state ownership. Initial implementation still required tracing those shared contracts; future reduction in unrelated implementation knowledge is an expected maintenance benefit, not a measured result. Shared signatures legitimately touched the five AI callers and compile-only test call sites; no new service, persistent occupancy state, or publisher was added. Verification retained Pull, Knockback, damage, Sacrifice, trait-state, preview/resumption, movement-undo/environment, and both complete test assemblies.
- Static checks found unique metas/GUIDs for all 12 new C# files, no replacement characters, and no UnityEngine references in changed pure Core files. Pre-existing non-code changes were preserved. No commit, push, View implementation, or acquisition/production content was added. View compilation and playback remain unverified. Stop at Phase D.

## Phase E — Publication, registered content, and handoff

### E1. End-to-end API and assessment verification

**Modify only if propagation requires it:** `S/CombatSystem/CombatTransitionPublisher.cs`, `CombatPreviewService.cs`, `CombatInputAssembler.cs`; `B/Core/BattleTransactions.cs`, `BufferGuards.cs`; `C/Contracts/Combat/CombatResult.cs`; `C/Application/Combat/MovementUndoPolicy.cs`; existing EnemyAi assessment consumers of attack facts. No View implementation.

**Create tests:** `T/CoreIntegration/DeadHandPublicationTests.cs`, `DeadHandRequestTests.cs`; `T/DeadHandPreviewTests.cs`.

**Interfaces:** expose `AttackResult.Sweep` / `AttackPreview.Sweep` through existing result/preview read contracts and publish `CombatSweepEvent` with its current attack group/origin. `BattleTraitSettings` continues exposing the resolved spec for numeric descriptions; no new View DB rule reader.

- [x] Add tests for Buffer request -> action request -> input -> calculation -> published result; explicit Left survives and omission matches Right. Prove stale preview responses are discarded and live state is unchanged by preview.
- [x] Test hidden obstacles using observer-appropriate preview inputs; preserve existing uncertainty reporting. Test stage interruption, result immutability, damage ordering, and forced-movement undo invalidation without adding a second undo rule.
- [x] Inject a calculation failure after transient movement and verify that the existing command/publication boundary never publishes a partial sweep world. Verify preview abandonment cannot mutate live Buffers. Keep these integration assertions here rather than coupling pure movement tests to Buffer internals.
- [x] Preserve assessment honesty: adapt existing outcome readers to sweep facts where needed; declare supported capabilities/assessment gaps conservatively through the current effect contracts. Do not rewrite AI strategy or add side-search optimization; callers without an explicit side use the documented default.
- [x] Run the new fixtures plus existing CoreIntegration command/preview/result publication, `BattleTraitSettingsPublicationTests`, and affected EnemyAi boundary/parity fixtures. Any current View consumer that lacks the new event is recorded as handoff work, not silently changed or claimed playable.

### E2. Author and register SoulEater and Dead Hand growth

**Create:** `A/Unit/Item/Playerable/Unit_SoulEater.asset`, `A/Targeting/Item/TargetingMethod_SoulEater.asset`, `A/TraitGrowth/Item/TraitGrowth_DeadHand.asset` (+ metas).

**Modify:** `C/Contracts/Ids/EntityKey.cs`; `A/Unit/UnitDataTable.asset`, `A/Targeting/TargetingMethodDataTable.asset`, `A/TraitGrowth/TraitGrowthDefinitionTable.asset`.

**Create tests:** `T/CoreIntegration/SoulEaterAuthoringTests.cs`.

**Interfaces/data:** Unit key 27, PlayerOnly Regular Heavy, footprint 1x1, movement/basic attack enabled, health 20, attack 6, defense 5, priority 30, range/depth 2, method key 11 ForwardRectangle width 3/Direction/Unrestricted/no splash. Dead Hand native identity resolves through the current catalog/rank source; default ranks are 100/100/100 with `allowUnchangedRankValues = true`, no Enhanced definition. Use Common growth grouping as technical metadata for this reusable trait; do not add it to category pools. Existing Heavy default SteadfastWill remains effective, without a duplicate native entry.

- [x] Add tests for the exact data above, independently editable rank values, table registration, fresh immutable GetData results, retained enemy Gravekeeper definition, and unchanged acquisition/loadout membership.
- [x] Create SOs with the existing authoring serialization conventions. Reuse existing native/rank assembly so the growth definition owns effective percentages. Keep player kill-reward behavior consistent with existing player units (explicitly excluded); do not copy enemy reward flags.
- [x] Register only the three InGame definitions. Do not add assets to shop/reward/level-up pools, UnitRoster/PlayerLoadout, or production inventory. Do not fabricate UnitVisual/TraitVisual/hand animations or bypass missing View validation.
- [x] Run `SoulEaterAuthoringTests`, the rank-authoring fixture, `DataBaseAssetTests`, and production InGame construction checks. If current catalog validation demands a View asset, report that dependency and retain the agreed boundary; do not fake a visual reference or weaken the validation.

### E3. Final regression and View handoff

**Create:** `docs/superpowers/handoffs/2026-11-14-piercing-dead-hand-core-view-handoff.md`.

**Update:** this plan's checkboxes/evidence and the spec's implementation status only to match observed outcomes.

- [x] Run complete `ProjectL.Tests.Core` and `ProjectL.Tests.CoreIntegration` with fresh XML/logs after focused checks pass. Include authoring checks for changed production SOs. Record baseline-only failures honestly; do not mask unrelated Cinemachine/FMOD/environment issues.
- [x] Review the final diff against the spec, compatibility/activation ownership, actual edited-file boundaries, and required regressions. Check meta/GUID integrity, source encoding, no Unity references in pure types, and no unintended acquisition/View edits.
- [x] Compare the three locality cases above against actual implementation. Identify both the reduced understanding burden and the remaining legitimate shared contracts; record any added indirection cost and whether each new boundary earned its place. Keep unproven improvements labeled as expectations.
- [x] Write the handoff with exact command/preview factory signatures, omission/Left/Right examples, facing mapping, mask-versus-target distinction, sweep event ordering, source of rank percentage, snapshot fields, interrupted/dead/immune cases, and the current lack of View playback/selection/visual implementation.
- [x] Include a user Unity checklist for directional overlays, real destinations, relative-side arrows, hand entering from the opposite side, concurrent movement before damage, cancel/scene release, and unit/trait visual registration. Mark View compilation/playback unverified; Core automated checks do not prove View acceptance.
- [x] Report completed phases, test counts/artifact paths, remaining external handoff work, and unchanged user modifications. Stop. Do not commit, push, implement View, or add acquisition content.

### Phase E execution record

- Scope and starting evidence. Phase D was already complete on the local `feature/Cheon/Core/NewCombatSytem` checkout at `3cfd325d5`. Its full run selected 6,812 cases with 36 Phase 0 failures and zero new failures. Prior phase work and unrelated working-tree changes were carried forward.
- Production files. Updated `C/Contracts/Combat/CombatSweepEvent.cs`, `C/Contracts/Results/AttackSweepSnapshot.cs`, `C/Contracts/Ids/EntityKey.cs`, and `C/Application/Combat/{CombatActionAssessment,CombatAttackQueries,DeadHandAttackCalculator,EnemyAi/EnemyThreatField}.cs`. Registered `Unit_SoulEater`, `TargetingMethod_SoulEater`, and `TraitGrowth_DeadHand` with their three metas and owning InGame tables. The player kill reward is explicitly excluded. Display naming remains with View. The growth asset uses the agreed trait name and leaves description fields empty for that handoff.
- Verification files. Added `T/DeadHandPreviewTests.cs` and `T/CoreIntegration/{DeadHandPublicationTests,DeadHandRequestTests,SoulEaterAuthoringTests}.cs` with metas. Updated `T/{AttackSweepContractTests,CombatChangeEvidenceCoverageTests}.cs` for the grouped header and `T/CoreIntegration/DataBaseAssetTests.cs` for the added definition. Created the [Core to View handoff](../handoffs/2026-11-14-piercing-dead-hand-core-view-handoff.md). The plan and design status record the delivery.
- Code and plan discrepancies. Direct candidate assessment initially omitted sweep entry reach. The focused RED run reproduced an unbound movement-rule exception when a visible mine lay on the sweep path. Candidate coverage now checks possible entry cells through the existing forced-entry and obstacle assessment. The threat field shares the ordinary sweep selection policy and wall envelope and retains `UnsupportedEffect` for approximated displacement, terrain, and post-move damage. It keeps the existing future-choice/hidden-environment gaps. It does not search alternate sides. Independent review also found that a board-clipped mask shortened the published nominal endpoint. Selection now supplies pre-clipping geometry to the sweep calculation while keeping ordinary selection and overlay cells clipped. The same snapshot is published as the header and result, with the current attack group. Existing publication and undo code handle timing, discarded preview work, atomic failure, and affected undo records.
- Locality, activation and rank. The attack kind and resolved effect contribution decide activation, while the growth adapter and owned profile resolver supply rank values and BattleTraitSettings. Retuning the coefficient does not require changing Kill Chain targeting. Normal, extra, reaction, preview parity and rank-setting checks remain coupled through the shared damage boundary.
- Locality, packing. Packing uses the authored mask, the one projected board, and the existing forced-entry, arrival and termination contracts. A change to row packing still requires verifying occupancy/terrain and the movement-to-damage handoff, including Sacrifice. The new mask capture adds a callback and a temporary copy only for sweep selection. It gives nominal endpoints and displayed cells their distinct geometry without a second shape calculation or persistent world. The AI reach calculation shares the geometry and conservatively bounds possible entry cells. The annotated threat estimate retains its model limits.
- Locality, compatible composition. An approved displacement exclusion changes the shared compatibility declaration and validates through both effective profile and owned composition. Candidate filtering, restored growth, category defaults and normal profile rejection still require regression coverage. No separate shop or level-up rule table was introduced. These reduced edit and reading scopes are source observations and maintenance expectations. No maintenance-time measurements were taken.
- Static verification. Seven new asset/script meta pairs have unique GUIDs, each InGame registration appears once, and the seven changed pure Core files passed UTF-8 and Unity dependency checks. `git diff --check` passed. No change to View, acquisition pools, UnitRoster, PlayerLoadout, or Gravekeeper is part of Phase E. Commits and pushes were left to the user.
- Focused verification selected 317 cases. It passed 309 with no skips. All 23 new Phase E cases passed. The eight failed cases match Phase 0 `DataBaseAssetTests` names and messages. SoulEater authoring, independent rank tuning, InGame table construction, compound DB construction for the growth catalog, and category/loadout membership checks passed. The runtime rank test observed 140% in BattleTraitSettings and base damage 7 from the test-owned attack power 5. Evidence is in `Library/CodexTests/PiercingDeadHand/phase-e-focused-final/{results.xml,unity.log}`.
- Full verification selected 6,835 cases. It passed 6,785, failed 36, and skipped 14. Core selected 5,745 (5,732 passed, two failed, 11 skipped). CoreIntegration selected 1,090 (1,053 passed, 34 failed, three skipped). All 23 new Phase E cases passed again. The 36 failure names and messages match Phase 0 exactly, including the existing Cinemachine camera type-load failure. Unity returned code 2 with these baseline failures and logged zero C# compiler errors. Evidence is in `Library/CodexTests/PiercingDeadHand/phase-e-full/{results.xml,unity.log,baseline-comparison.json}`.
- Remaining work. Current View routes the sweep header to Skip and requires a UnitVisual for SoulEater placement and field rendering. The handoff contains interaction, animation and resource-release acceptance steps. View compilation and playback acceptance were not verified.

### Pre-push review

- The live combat job calls the existing Core command or preview service. A rejected action publishes the existing rejection result. Unexpected calculation errors propagate to `GameBootstrap.Update` and its shared `RuntimeFaultBoundary`. The calculation-failure integration test verified that a fault after forced movement leaves the published world, results and command cursor intact.
- Knockback and Dead Hand both use `AttackForcedMovement.ExecuteUnitSteps` and the existing forced-arrival settlement. Knockback defers its destination objects to the existing arrival phase. Dead Hand applies entry objects during movement and calls forced-arrival normalization once for a surviving target at its settled destination. The final review added two regression cases that move a live enemy into Haboob and issue a fresh Single attack from outside. Preview and execution reject with `HaboobDesignationBlocked`; execution emits no combat events. The existing Direction/All targeting policy still allows indirect hits across the boundary. The focused Core and CoreIntegration run passed all 352 cases, including Knockback, Haboob, Rockfall, Tornado, cliff, Pull, and command publication coverage. Evidence: `Library/CodexTests/PiercingDeadHand/phase-e-final-review/{results.xml,unity.log}`.
- Review finding: `TraitGrowthRules.HasDistanceDamageCoverage` validates the initial mask distance. `AttackDirectDamageCalculator` uses the target's post-movement location for `DistanceDamageScale`, whose rule throws beyond the configured table. A reused radial range-2 profile can move a target to distance 3 and then exceed a table of length 2. The newly authored SoulEater profile has no distance table. The interaction needs a coverage contract for post-movement distances before authoring that combination.
- Review finding: `CombatAttackCalculator.PrepareDeliverySteps` reads the post-sweep roster when producing attack power evidence. `CombatAttackDelivery.CurrentDamage` recomputes from the original input roster. If a sweep hazard changes the living unit counts used by a roster-dependent artifact such as Last Stand, its reported bonus can differ from the damage calculation. The review also identified an independent repeated computation in `CombatAttackQueries.Range`: it prepares the selection and then prepares it again for each aim. The focused run verifies the current authored content and terrain paths. These source findings remain open.

### Follow-up to the pre-push review

- DistanceDamageScale uses the actual post-sweep distance. If sweeping pushes a target past the last configured distance, apply the last coefficient. An absent coefficient at the original target distance is still a configuration error. Direct hits and Sacrifice planning share this calculation and use the original victim for the distance. Other attack types continue to use their existing lookup rules.
- Dead Hand captures the living roster and soul counts at delivery preparation, after the sweep and its entry effects. Every hit reads the same captured values used for the Last Stand event. The damage pipeline still reads the current attacking unit for effects that can change between hits. Ordinary attacks, Pull and independent follow-up attacks retain their previous damage context.
- The attack-range query shares one selection preparation across its aim candidates and reuses its range and base range. Aim validation still applies the same targeting policies and Pull path checks. Actual execution and attack preview prepare the sweep using the accepted aim.
- The red run at `Library/CodexTests/PiercingDeadHand/follow-up-red-final/` reproduced the distance-3 exception, two Last Stand damage mismatches of five points, and nine effect-binding evaluations for one range query. The tests cover the last coefficient, an explicit post-sweep coefficient, invalid original configuration, matching Last Stand evidence and multi-target damage, and one shared binding evaluation with executable aims. A Sacrifice case also verifies that its planner preserves the redirected target when the sweep moves it beyond the coefficient table.
- The final focused EditMode run selected 484 cases. It passed 483 with zero failures and one existing explicit capture test skipped. All eight new regression cases passed, as did both forced-entry Haboob cases. Evidence: `Library/CodexTests/PiercingDeadHand/follow-up-green-final/{results.xml,unity.log}`.
- The full EditMode run selected 6,845 cases. It passed 6,793, failed 38, and skipped 14. All eight new cases passed. The 36 baseline failure names and messages matched Phase 0. `AllPartitionsMatchFrozenPreview(1)` and `CacheOnOffIdenticalTraces` exceeded their existing 180-second time limits at 233.7 and 202.7 seconds. Evidence: `Library/CodexTests/PiercingDeadHand/follow-up-full-final/{results.xml,unity.log}`.
- The targeted rerun passed preview partitions 2 and 31. Partition 1 and the Core AI cache comparison timed out again at 224.0 and 180.1 seconds. The production-stage CoreIntegration cache comparison passed in the preceding rerun. The cause of the repeated timeouts was not established. Test time limits remain in effect. Evidence: `Library/CodexTests/PiercingDeadHand/follow-up-timeout-exact/{results.xml,unity.log}` and `follow-up-timeout-rerun/results.xml` under the same evidence root.

## Plan self-review and coverage

| Spec requirement | Owning task |
|---|---|
| Basic-range-based Piercing, hostile aim, friendly delivery, 25%/four, preserved trace/rounding | A1/A2 |
| SoulEater 3x2 and reusable basic geometry | C1/E2 |
| Rank-aware 100/100/100, equal-rank policy, copies and settings | B2/E2 |
| Displacement exclusivity and exact Piercing/DeadHand ban | B1 |
| Optional relative side, Single/Direction/All, stale/current targets | C2/D1/E1 |
| Row boundary, ordering, blockers, immunity, existing terrain/death rules | D2 |
| Movement before damage, Sacrifice, half-up, basic-only activation | D1/D3 |
| Buffer facts, immutable publication, assessment honesty, undo | E1 |
| Core/SO/API-only scope; no new acquisition availability | E2/E3 |
| Narrow structural improvement with one rule owner per concern | B1/C2/D1/D3 |

All five Review Focus items have an owning test task. The plan contains no gameplay question requiring a further answer before review; it deliberately leaves concrete method bodies, formatting, and ordinary fixture construction to implementation. Discovered source drift, unsupported combinations not covered by the confirmed rules, or an unresolvable scope conflict must still be reported at the affected phase rather than guessed away.

The 2026-11-14 locality-guideline review found the original plan already preserved SSOT, Core/View ownership, registry boundaries, and full regression requirements. It needed more explicit behavioral contracts and a source-grounded distinction between unavoidable shared state/order and preventable repeated policy. The added locality cases, pre-hit arrival contract, abstraction limits, task checks, and phase-exit evidence address those gaps at the plan level. The execution records above contain the subsequent code and verification evidence.
