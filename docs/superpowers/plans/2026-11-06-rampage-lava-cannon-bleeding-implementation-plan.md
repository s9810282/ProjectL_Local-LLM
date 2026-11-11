# Rampage, Lava Cannon, and Bleeding Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans for sequential implementation, or superpowers:subagent-driven-development if the user selects delegated implementation. Steps use checkbox (`- [ ]`) syntax for tracking. Implement only the authorized Phase and stop at its review boundary. This document does not authorize commits or pushes.

**Goal:** Add the three approved traits and their authoring support while making the relevant attack, status, movement, and settlement contracts safer to extend.

**Architecture:** Reuse immutable Core facts, the existing effect registry, shared attack/damage/death settlement, and the Systems-to-Buffer publication boundary. Consolidate repeated movement rules, make ordered-hit requirements explicit, and keep contact effects separate from captured-area effects. Add no alternative state store, general event bus, or Framework service hierarchy.

**Tech Stack:** Unity 6000.0.73f1, existing C# Core assemblies and ScriptableObject authoring, NUnit EditMode Core/CoreIntegration suites.

**Spec:** [Approved design](../specs/2026-11-06-rampage-lava-cannon-bleeding-core-design.md). The user approved planning on 2026-11-06 and explicitly reaffirmed Bleeding level 1/2/3 = damage 1/2/3 per stack.

**Structural authority:** User-supplied [feature-change-locality-guidelines.md](<D:/My_ProjectWork/학교 관련 문서/졸업작품_프로젝트L/2학기 내용/AI 관련 문서/feature-change-locality-guidelines.md>), read on 2026-11-06. Its requirements are applied below; the external document is not modified and grants no additional implementation authority.

**Status:** Phases 0–5 are complete with the recorded baseline failures unchanged as of 2026-11-07. Execution baseline: `bca934e08`, branch `feature/Cheon/Core/NewCombatSytem`. Final Phase 5 verification and review are recorded below. Production activation and PlayMode acceptance remain pending.

## Global Constraints

- Preserve existing Framework contracts, Core paths, public behavior of other traits, and Buffer/Job ownership. Do not turn the review into a wholesale trait rewrite.
- Implementation documents are English; new code comments and XML documentation are Korean. Do not commit or push.
- Allman braces, four spaces, explicit accessibility, block namespaces, UTF-8/LF, and companion `.meta` files for new Unity source/assets. Preserve existing GUIDs.
- Core is explicitly in scope for this request. View implementation, scenes, VFX, FieldCamera, Architecture, and unrelated production content remain outside it. No View test code or combat-View MSBuild.
- New Core/CoreIntegration tests use test-owned inputs. Preserve existing test assertions; identify authorship before altering another author's behavioral expectations and obtain the required decision first. Do not mask baseline failures.
- Unity execution must use the approved AppData-access route. Do not delete `CurlRequestCache.db`, clear caches, upgrade the Editor, install tooling, or kill an existing Editor to work around an execution problem.
- Canonical target statuses stay in `CharacterCombatFacts.StatusEffects`; the Rampage kill accumulator stays in its trait state. Snapshots and compiled configuration are immutable projections, not new editable sources.
- Rampage: HP strictly below 40%; movement 1/1/2; attack n/n+5/n+7; killed enemy **base** attack added; reaching 40% clears all accumulated gains. SO owns n once.
- Bleeding: **trait level** 1/2/3 gives **per-stack** damage 1/2/3; maximum five mixed-origin stacks; shared three-target-side-start lifetime refreshed on reapplication, including at cap. Existing stacks retain their application-time values.
- Lava Cannon: fire damage 2 for two target-side starts, no stacking; movement -1 for the next target-side turn only. Capture primary/splash targets at accepted attack resolution and apply to surviving captured enemies even if the Cannon dies before remaining hits.
- Preserve approved damage modifiers, source credit, cleanup, whole-batch turn-start settlement, and reinforcement timing from the design. No gameplay simplification beyond that design.
- Deliver three editable SO assets without inserting them into live catalogs/pools or replacing category defaults. Production activation requires paired View registrations and separate gameplay acceptance.

## Review Focus

1. Reflection kills a Cannon before later splash hits: Lava still applies to surviving captured targets; Bleeding does not apply to untouched targets. Task 2.3.
2. A status is refreshed during an already-running affected-side turn: fire/bleeding wait for future starts, and slow survives through the next affected-side turn. Tasks 2.1–2.2.
3. A credited kill heals a Rampage holder across 40%, or changes attack before another hit: no stale bonus, double reward, or registry-order dependency. Task 3.1.
4. Movement availability/undo constructs a candidate after a trait change: query returns a valid answer without the former Taunt-style validation exception. Tasks 1.1 and 3.2.
5. New states reach Buffer and authoring before View support: canonical data round-trips, prior snapshots remain immutable, and dormant SO assets do not accidentally activate. Tasks 2.2, 4.1, and 5.1.

## 1. Phase map and stop rules

| Phase | Deliverable | Depends on | Exit and review point |
|---|---|---|---|
| 0 | Current baseline, contract/caller inventory, test ownership and execution route | Approved design | Report evidence and any actual blockers; no gameplay edits. |
| 1 | Behavior-preserving movement and attack-boundary refactor | 0 | Existing behavior preserved; report before/after modification, understanding, and verification scope. Stop. |
| 2 | Canonical fire/bleeding/slow, turn-start settlement, Lava Cannon and Bleeding combat behavior | 1 | New behavior and Buffer publication validated with owned fixtures; no live acquisition. Stop. |
| 3 | Rampage condition, kill gains, effective stats, cleanup, and movement-context safety | 2 | HP thresholds and cross-effect ordering validated; undo remains safe. Stop. |
| 4 | SO/data adapters, level/rank resolution, three dormant assets | 3 | Designer-editable n and level values verified through real conversion paths; live tables unchanged. Stop. |
| 5 | Combined regression, locality review, and Core-to-View handoff | 4 | Evidence and residual limitations documented; no claim of View/PlayMode acceptance. Stop. |

Do not start the next Phase merely because the previous one passed. A Phase report lists completed tasks, exact changed files, tests/evidence, unresolved items, and the next Phase. If implementation would require changing approved mechanics, public ownership, or a prohibited area, stop the dependent work and explain the concrete difference.

## 2. File map and ownership

Paths below are exact repository-relative paths using these aliases:

- `C/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/`
- `S/` = `Assets/ProjectL/Script/Systems/`
- `D/` = `Assets/ProjectL/Script/Systems/DataBaseSystem/Data/InGame/Table/`
- `T/` = `Assets/Tests/EditMode/`; `I/` = `Assets/Tests/EditMode/CoreIntegration/`
- `A/` = `Assets/ProjectL/DataBaseSO/InGameGroup/TraitGrowth/Item/`

Brace lists below mean the explicit finite expansion of those filenames, not permission to edit every file in a folder. Every new `.cs`/`.asset` also needs its `.meta`.

| Files | Owner/responsibility |
|---|---|
| New `C/Domain/Movement/VoluntaryMovementRules.cs` | One pure movement allowance formula; no Application/Buffer dependency. |
| Existing `C/Application/Combat/CombatActionAdmission.cs`, `C/Domain/Movement/MovementRouteValidator.cs`, `C/Application/Combat/EnemyAi/EnemyMovementPathfinder.cs` | Consume the allowance contract for availability, validation, and AI. |
| Existing `C/Application/Combat/AttackDeliveryExecutor.cs`, `AttackDirectDamageCalculator.cs`; new `C/Application/Combat/TraitContactApplication.cs` | Attack ordering/completion and explicit contact dispatch. No per-feature geometry recomputation. |
| Existing `C/Domain/CombatEffects/Rules/CombatEffectCapabilities.cs`, `C/Domain/CombatEffects/{ICombatEffectEngine,CombatEffectEngine}.cs` | An ordered-hit capability, preserving current trait eligibility. |
| New `C/Contracts/CombatEffects/{FireStatus,BleedingStatus,MovementRangeDecreaseStatus}.cs` | Immutable canonical recipient status values. `BleedingStatus.cs` also owns the immutable `BleedingStack` value. |
| New `C/Domain/CombatEffects/Rules/PeriodicStatusRules.cs`, `C/Application/Combat/PeriodicStatusApplication.cs` | Status merge/clock policy versus command-level ticking through existing damage/completion ownership. |
| New `C/Application/Combat/LavaCannonApplication.cs` | Captured-area application, deliberately separate from actual-contact dispatch. |
| Existing `C/Contracts/CombatEffects/{AppliedStatus,StatusEffectKey}.cs`, `C/Contracts/Snapshots/StatusEffectSnapshot.cs`, `C/Application/Combat/{TraitTurnLifecycle,StatusEffectReadModel}.cs` | Append lifetime/key contracts and publish actual state transitions and read models. |
| Existing `C/Application/Combat/{CombatTurnCalculator,MovementProjection,CombatDamageApplication,TraitKillSettlement,CombatEffectiveStatsCalculator,TraitPresenceReconciler}.cs` | Existing shared lifecycle/settlement owners; only targeted hook/projection changes. |
| New `C/Application/Combat/RampageApplication.cs` | Reconcile Rampage history and derived stats at existing state-change boundaries. |
| Existing `C/Application/Combat/{MovementContextCalculator,MovementEffectUndo,MovementUndoPolicy}.cs` | Keep dependency detection and existing reversibility policy correct; no permissive result-validator bypass. |
| Existing `S/RuntimeSystem/InGameSystem/CoreSystem/CharacterSystem/TraitSpecAdapter.cs`, `C/Contracts/Enhancements/TraitRankValues.cs`, `D/TraitGrowth/Item/TraitRankAuthoring.cs` | Ordinary authoring/rank integration points; legitimate composition edits. |
| New `A/TraitGrowth_{Rampage,LavaCannon,Bleeding}.asset` | Dormant editable definitions. Existing tables/pools are not modified. |

For **each** of `Rampage`, `LavaCannon`, and `Bleeding`, use the existing trait family layout: create `C/Contracts/Commands/Traits/<Name>TraitSpec.cs`, `C/Contracts/Snapshots/Traits/<Name>TraitStatusSnapshot.cs`, `C/Domain/Traits/Definitions/<Name>TraitDefinition.cs`, `C/Domain/Traits/States/<Name>RuntimeState.cs`, `C/Domain/Traits/Rules/<Name>TraitRule.cs`, `C/Domain/Traits/Effects/<Name>EffectRule.cs`, and `C/Application/Combat/Traits/<Name>TraitFactories.cs`. Add `D/Unit/Trait/<Name>TraitData.cs` and `<Name>TraitAuthoring.cs` in Phase 4. These follow the current registry contract; do not introduce another per-feature bootstrap/module layer.

Shared trait integration files are `C/Contracts/Common/TraitKey.cs`, `C/Application/Combat/Traits/{TraitRegistry,TraitEffectProvider}.cs`, `C/Domain/Traits/Effects/TraitEffectStateCodec.cs`, `C/Application/Combat/CombatCommandEffects.cs`, and `C/Application/Roster/OwnedCharacterStatsCalculator.cs`. Preserve the difference between compilation/registration and runtime rule dispatch.

## 3. Verification commands and evidence contract

Phase 0 resolves the installed **6000.0.73f1** executable and records it as `$traitUnityEditor`; do not assume a default Hub installation path. This planning session found no `unity` command in PATH and no Editor at the default C: Hub path. That is not proof the required Editor is absent. Do not install or update anything during this plan.

Run the following only during authorized execution, through the AppData-permitted route, with the project free of a competing Editor test run. `$traitFilter` is the semicolon-separated fully qualified fixture list named by the task. `$traitEvidenceName` distinguishes baseline/Phase/task artifacts. Omit `-testFilter` for the final full assembly run; do not use `-quit` to terminate an asynchronous test run early.

```powershell
$traitRepo = 'D:\My_ProjectWork\Github_desktop\Project_L'
$traitEvidenceRoot = Join-Path $env:TEMP 'ProjectL-Rampage-Lava-Bleeding'
New-Item -ItemType Directory -Force -Path $traitEvidenceRoot | Out-Null
& $traitUnityEditor -batchmode -nographics -projectPath $traitRepo -runTests -testPlatform EditMode `
    -assemblyNames 'ProjectL.Tests.Core;ProjectL.Tests.CoreIntegration' -testFilter $traitFilter `
    -testResults (Join-Path $traitEvidenceRoot ($traitEvidenceName + '.xml')) `
    -logFile (Join-Path $traitEvidenceRoot ($traitEvidenceName + '.log'))
```

Use `ProjectL.Tests.Core.<FixtureName>` for `T/` and `ProjectL.Tests.CoreIntegration.<FixtureName>` for `I/`. A check passes only when the requested fixtures actually ran, the XML contains their cases with no unexpected failures, and the log has no new compilation error. An absent report, empty selection, timeout, or pre-code Editor failure is not a passing test. Retain baseline failure names/messages and distinguish unchanged failures from introduced ones. If the execution route is unavailable, report tests unrun and do not mark the implementation Phase verified.

Within each task, add the smallest meaningful new Core test, observe the expected red result, implement, then run the focused fixtures. A behavior-preserving extraction can use existing characterization plus one new cross-entry-point assertion; it does not need a test mirroring every moved method. At a Phase boundary run the actual shared-contract regressions listed below. Run the full Core/CoreIntegration set at baseline and after all final edits; do not repeat an unchanged successful final run without new changes or unresolved evidence. For dormant asset changes include `DataBaseAssetTests` in the user's Unity verification handoff, while new Core behavior fixtures remain independent of those assets.

Static checks: `git diff --check`, `.meta` pairing, no Unity reference in new pure InGame/Core code, no retired key reuse, no live pool/catalog edits, and no unrelated/FMOD changes included. Save reports outside tracked production content. Do not claim compilation or PlayMode verification from static checks.

## Phase 0 — Baseline and contract inventory

### Task 0.1: Establish evidence and the permitted change surface

**Files:** Read the file map, `AGENTS.md`, `Assets/AGENTS.md`, `Assets/ProjectL/Script/Systems/Documentation/{framework-architecture,framework-programmer-guide}.md`, the design, and the user-supplied locality guide. Update only the execution record in this plan.

**Interfaces:** Consume current git state and existing test/report tooling; produce an evidence row containing revision, pre-existing changes, Unity path, baseline result paths, affected test ownership, and actual caller/consumer lists. No runtime API is added.

- [x] Capture `git status --short`, branch, and HEAD; preserve unrelated changes. Confirm whether an execution worktree is required before implementation; do not create one during planning.
- [x] Trace selection → direct delivery → contact/area effects → shared damage/death → turn completion → Systems publisher → Buffer snapshots. Trace movement range → validation → AI → undo candidate/result validator. Record the exact owners, mutation boundaries, and ordering used by the tasks below.
- [x] Inspect authorship of any existing tests that would need expectations changed, especially `StatusEffectContractTests`, contract shape/registry inventories, and authoring eligibility tests. Do not rewrite their expectations as an incidental implementation step.
- [x] Resolve the installed Editor and run the full Core/CoreIntegration baseline using Section 3. Record all existing failures without broad repairs. Confirm the resolved Taunt movement/availability/undo cases independently in the focused selection.
- [x] Report the baseline and any concrete implementation blockers. **Stop at the Phase 0 review point.**

## Phase 1 — Preserve behavior while clarifying shared contracts

### Task 1.1: Unify voluntary movement allowance

**Files:** Create `C/Domain/Movement/VoluntaryMovementRules.cs` and `T/VoluntaryMovementAllowanceTests.cs`; modify the three movement consumers in the file map. Read `C/Application/Combat/{CombatMovementCalculator,MovementProjection}.cs` for range/query and evidence consumption.

**Interfaces:** Produce `internal static int? VoluntaryMovementRules.Allowance(CharacterCombatFacts actor, MovementEnvironmentFacts environment = null)`. Consume the existing base movement and `GroundCompactionTraitRule.MovementBonusAt`; null means voluntary movement is disabled, not unlimited. `StepBudget` keeps its signature and delegates to this rule. Later phases extend this one formula with slow and Rampage contributions.

- [x] Add `VoluntaryMovementAllowanceTests.ZeroBase_DisablesEveryEntryPoint` and `GroundCompaction_QueryExecutionAndAiAgree`. For base range 3 and trail bonus 2, assert allowance 5, five steps accepted, six rejected; base 0 must stay disabled even on that trail. Check existing `CombatActionCostRetirementTests.ZeroVoluntaryRange_DisablesMovementWithoutChangingFacts` unchanged.
- [x] Run the new cross-entry-point tests and record baseline behavior before extraction.
- [x] Move the repeated formula into the Domain rule; retain observer-specific environment input, no-anchor behavior, checked arithmetic, forced movement exemptions, and normal failure reasons. Do not let Domain call Application.
- [x] Run `VoluntaryMovementAllowanceTests`, `CombatActionCostRetirementTests`, `MovementRouteTests`, `EnemyMovementPathTests`, `TauntTraitTests`, and `MovementPassiveUndoTests`. Verify no extra movement or stale-context acceptance.

**Locality result to report:** lookup, route validation, and AI no longer require separate knowledge of the movement contribution formula; callers still need voluntary/forced movement and observer-visibility contracts. This is consolidation of one repeated rule, not a new wrapper around each consumer.

### Task 1.2: Make attack ordering and contact ownership explicit

**Files:** Modify `C/Application/Combat/{AttackDeliveryExecutor,AttackDirectDamageCalculator}.cs`; create `C/Application/Combat/TraitContactApplication.cs` and `T/AttackBoundaryLocalityTests.cs`; modify the engine/capabilities files in the map and `C/Domain/Traits/Effects/{ArmorBreaker,FinishingShot,TrackingShot,Hunter,MoraleBoost}EffectRule.cs`.

**Interfaces:** Produce `IEffectOrderedHitRule : ICombatEffectRule` with `bool RequiresOrderedHits { get; }`, and `CombatEffectEngine.RequiresOrderedHits(IReadOnlyList<EffectRuleBinding> bindings)` plus its engine-interface declaration. The current five attacker rules return true, preserving the current whitelist's behavior. Defender presence remains owned by `TraitPresenceReconciler.RequiresOrderedDamage`.

Produce `internal static void TraitContactApplication.Apply(CommandAttackPlan plan, CommandAttackProjection world, EntityId targetId, EntityId recipientId, CombatDamageOrigin origin)`. It is called after a damage-bearing contact has settled and preserves the existing WeaknessExposure → restriction → ArmorBreaker order. Original target, redirected recipient, and origin are separate inputs. It may update the existing character projection/events through the same collaborators; it does not own HP settlement, geometry, or new state.

- [x] Add `AttackBoundaryLocalityTests.CompletionRunsOnceAcrossDeliveryBranches`, `OrderedCapabilityMatchesExistingTraits`, and `ContactPreservesOriginalAndSacrificeRecipient`. Assert one Acceleration consumption/reselection, preserved events, and unchanged selected-target restrictions; include preview, reflection death, and nested extra attacks.
- [x] Run the focused characterization and confirm existing results before changing orchestration.
- [x] Remove the redundant guarded `FinishAction` call, retain the single unconditional completion boundary, replace the attacker-key ordering condition with the explicit capability, and move the existing contact dispatch behind the specified contract without moving trait predicates into the dispatcher.
- [x] Run `AttackBoundaryLocalityTests`, `AttackCommandCharacterizationTests`, `CombatEffectEngineTests`, `KillChainAttackTraitTests`, `HunterTraitTests`, `TargetingSplashDamageTests`, `CombatDamageBoundaryTests`, `SacrificeTraitTests`, `ArmorBreakerTraitTests`, `CannonThresholdTraitTests`, and `I/CannonThresholdPublicationTests`.

**Locality result to report:** the delivery loop knows the contact and ordering contracts, rather than each participating trait's internal eligibility. The dispatcher is a small ordered integration point, not a new generic effect framework. Adding a local rule can still require its normal registration.

**Phase 1 exit:** demonstrate preserved behavior and compare modification/understanding/verification scopes using Section 8. Stop before adding new mechanics.

## Phase 2 — Fire, Bleeding, slow, and safe turn-start settlement

### Task 2.1: Add canonical status values and pure lifetime rules

**Files:** Create the three status files and `PeriodicStatusRules.cs` from the map; modify `C/Contracts/CombatEffects/{StatusEffectKey,AppliedStatus}.cs`, `C/Domain/CombatEffects/Rules/AppliedStatusRules.cs`, and the movement allowance rule. Create `T/PeriodicStatusRulesTests.cs`.

**Interfaces:** Append status keys `Fire = 15`, `Bleeding = 16`, `MovementRangeDecrease = 17` after confirming they remain free at execution. Append lifetime kinds `AffectedSideTurnStarts = 8` and `NextAffectedSideTurnEnd = 9`; preserve existing values.

New immutable values: `BleedingStack(EffectOriginKey origin, int damage)`; `BleedingStatus(IReadOnlyList<BleedingStack> stacks, int remainingAffectedSideStarts)`; `FireStatus(EffectOriginKey origin, int remainingAffectedSideStarts)` with damage 2; `MovementRangeDecreaseStatus(EffectOriginKey origin, long expiresAtCompletedControllerTurnCount)` with magnitude 1. Validate positive damage/ticks, 1–5 stacks, and immutable copies. The aggregate Bleeding status's base `Origin` is derived from its first retained stack for compatibility; actual tick/reward attribution always uses each stack's origin, never this aggregate origin.

`PeriodicStatusRules` produces `ApplyBleeding(StatusEffectSet current, EffectOriginKey origin, int damagePerStack)`, `ApplyFire(StatusEffectSet current, EffectOriginKey origin)`, `ApplySlow(StatusEffectSet current, EffectOriginKey origin, Owner affectedSide, CombatTurnFacts turn)`, `ConsumeStartTick(StatusEffectSet current, StatusEffectKey key)`, and `ExpireSlow(StatusEffectSet current, long completedControllerTurnCount)`, all returning `StatusEffectSet`. Slow expiry references the existing completed-controller-turn counter; it does not introduce another global clock. Application on the opposite side expires after that side and the next affected side finish; application during the affected side waits through the following full affected-side turn.

- [x] Add exact policy assertions: `MixedRanks_KeepOneTwoThree` → stack damage `[1, 2, 3]`, sum 6; `FiveStacks_RefreshWithoutReplacement` → five unchanged magnitudes and remaining starts 3; `Fire_ReapplyRefreshesWithoutStacking` → one status and remaining starts 2; `Slow_OpponentApplied_ExpiresAfterNextAffectedTurn` and `Slow_OwnTurnReaction_DoesNotExpireEarly` → magnitude 1 with the approved boundary.
- [x] Run the new tests for the expected failure, then implement immutable values, merge/consume/expiry rules, and independent slow contribution in `VoluntaryMovementRules.Allowance`. Clamp positive-base finite movement at zero; retain the existing base-zero disabled policy.
- [x] Ensure `ClearForReserve`, death/battle cleanup, `SameValues`, and `WithStatusEffects` preserve or clear the new canonical data correctly. Do not add mirrored new fields to the legacy debuff DTO merely to carry the same status twice.
- [x] Run `PeriodicStatusRulesTests`, `StatusEffectContractTests`, `StatusEffectReadModelTests`, `AppliedAttackStatusTests`, `AppliedRestrictionStatusTests`, `ShieldStatusPolicyTests`, and `VoluntaryMovementAllowanceTests`. Any expectation change in another author's test remains subject to the Phase 0 ownership gate.

### Task 2.2: Resolve periodic damage through existing completion owners

**Files:** Create `C/Application/Combat/PeriodicStatusApplication.cs`, `T/PeriodicStatusTurnTests.cs`, and `I/PeriodicStatusPublicationTests.cs`; modify `C/Application/Combat/{CombatTurnCalculator,TraitTurnLifecycle,MovementProjection,StatusEffectReadModel}.cs` and `C/Contracts/Snapshots/StatusEffectSnapshot.cs`. Change `CombatDamageApplication.cs` only for a demonstrated integration need, retaining it as the sole damage boundary.

**Interfaces:** Produce `internal static void PeriodicStatusApplication.BeginSide(CombatCommandInput input, MovementProjection projection, Owner side)`. It consumes `projection.Damage`, the shared event list, current input/facts, and attack-group allocation. It returns through that same projection, not another damage store. Add `FireEffectSnapshot`, `BleedingEffectSnapshot`, and `MovementRangeDecreaseEffectSnapshot` to the existing read-model contract. Expose remaining future starts, fixed fire damage, Bleeding stack damage/origins, and slow expiry semantics; keep source attribution per stack inspectable without copying the shared timer into every stack.

- [x] Add `PeriodicStatusTurnTests.FireTicksTwo_BleedingTicksThree_OnAffectedStarts`, `ShieldExpiryPrecedesTicks`, `WholeBatchUsesExistingOutcomePriority`, `DeadRecipientsSkipLaterStacks`, `SnapshotBatchDoesNotRetickNewStates`, and `TickDeathsDoNotOpenNexusCapacity` with exact 2/3 tick counts and no ticks on the other side.
- [x] Add `PeriodicStatusPublicationTests.LethalTickPublishesDeathRewardsSoulsAndStatsOnce`, `TerminalBatchDoesNotOpenInputOrReinforce`, and `PublishedSnapshotsRemainImmutable`; assert both canonical facts and published before/after events, not just final HP.
- [x] Run red tests, then integrate start resets/shield expiry → captured periodic batch → existing projection completion → nonterminal reinforcement/input. Reuse `MovementProjection.ForArrival` and `Complete` with current turn state; preserve empty/no-status behavior and the existing pre-tick `livingAtStart` snapshot. Never use the old `Changed` InProgress-only shortcut for a lethal tick.
- [x] Route each fire tick and each Bleeding stack through `CombatDamageApplication.Apply(..., CombatDamageKind.DamageOverTime, ...)`, with a source-specific `CombatDamageOrigin`. Process EntityId, fire-before-Bleeding, then stack insertion order; consume once after the captured tick, and skip dead recipients. Resolve the full captured batch before final outcome priority. Retain dead-source IDs for attribution; if source facts are absent, do not substitute the controller actor or create a source entity. Existing credit eligibility decides whether a reward recipient is available.
- [x] Run the new fixtures plus `ControllerTurnCountTests`, `RegenerationTurnTests`, `BossTurnTests`, `CombatDamageApplicationTests`, `CombatItemDamageSourceTests`, `I/StatusEffectPublicationTests`, `I/TraitTurnLifecyclePublicationTests`, and `I/CombatCompletionPublicationTests`. Validate canonical status round-trip through Buffer without widening unrelated legacy preparation acceptance.

### Task 2.3: Wire Bleeding contact and Lava Cannon area application

**Files:** Create the `Bleeding`/`LavaCannon` trait families from Section 2, `C/Application/Combat/LavaCannonApplication.cs`, `T/{BleedingTraitTests,LavaCannonTraitTests}.cs`, and `I/NewTraitAttackPublicationTests.cs`. Modify trait registry/provider/codec/command capture, `TraitContactApplication.cs`, `CommandAttackPlan.cs`, and `AttackDeliveryExecutor.cs` only as needed for these two integration points.

**Interfaces:** Allocate `TraitKey.LavaCannon = 470`, `TraitKey.Bleeding = 480` after the Phase 0 collision check. Use `LavaCannonTraitSpec()` for the approved fixed values and `BleedingTraitSpec(int damagePerStack)` for the resolved rank value. The latter is copied into each new stack at application, not re-read when the stack ticks.

Produce `LavaCannonApplication.Apply(CommandAttackPlan plan, CommandAttackProjection world, IReadOnlyList<EntityId> capturedEnemyIds)`. Capture distinct hostile character IDs from the existing resolved primary/splash set before delivery; preserve this immutable command-local list. Apply once after direct/forced arrival processing and before child kill-chain attacks, even when reflection killed the source. Each child attack has its own accepted plan and target set. Do not use delivered victims, a new radius query, or the attacker's current alive flag as the area predicate.

- [x] Add Bleeding cases for trait levels 1/2/3 producing stack magnitudes 1/2/3, repeated actual hits, primary/splash, shield-absorbed hits, Sacrifice actual recipient, dead targets, no reflection/DoT recursion, cap refresh, and immutable application-time rank after the source later levels up.
- [x] Add Lava cases for original primary/splash targets, enemies outside the footprint, no stacking across two Cannons, latest fire-source refresh, one-turn slow, no status on corpses/structures, captured targets moved by knockback, and `ReflectionKillsCannon_UndeliveredCapturedEnemyStillGetsBothStatuses`.
- [x] Run red tests; implement the two rules and ordinary registry wiring. Bleeding joins the contact collaboration after the existing three effects; Lava uses its separate area entry point. Rejected attacks apply neither effect. Ordinary/extra/reaction plans use the same respective contracts.
- [x] Add `I/NewTraitAttackPublicationTests.PreviewIsolated_CommitMatchesEvents` and `CanonicalStatusesSurviveCommit`; verify private preview projection, no live mutation/random-state consumption, and preserved old Buffer snapshots.
- [x] Run these fixtures plus Phase 1's attack regressions and `I/StatusEffectPublicationTests`. **Stop at the Phase 2 review point.** All tests use injected definitions; no live pool or View changes.

## Phase 3 — Rampage and history-safe effective stats

### Task 3.1: Integrate Rampage condition and kill reward

**Files:** Create the Rampage trait family, `C/Application/Combat/RampageApplication.cs`, `T/RampageTraitTests.cs`, and `I/RampagePublicationTests.cs`. Modify registry/provider/codec/command capture, `CombatEffectiveStatsCalculator.cs`, `TraitKillSettlement.cs`, `TraitPresenceReconciler.cs`, `VoluntaryMovementRules.cs`, `StatusEffectReadModel.cs`, and `StatusEffectSnapshot.cs`. Inspect `C/Application/Combat/{CharacterSettlementCalculator,RegenerationTurnApplication,PositionTileModifierCalculator}.cs` and `C/Application/Artifact/SoulEffectCalculator.cs` as health/max-health producers; retain their mechanics and integrate normalization at their existing shared caller boundaries.

**Interfaces:** Allocate `TraitKey.Rampage = 460` and derived-display `StatusEffectKey.Rampage = 18`. Use `RampageTraitSpec(int baseAttackBonus, int rankAttackBonus, int movementBonus)` and `RampageTraitStatusSnapshot(int baseAttackBonus, int rankAttackBonus, int movementBonus, int killAttackBonus = 0)`. The configuration values are frozen projections of the resolved definition; only the kill accumulator is runtime history. `RampageRuntimeState(int killAttackBonus = 0)` follows the existing clone/copy contract. No active boolean is stored.

`RampageTraitRule` exposes pure `IsActive(CharacterCombatFacts character)`, `AttackBonus(CharacterCombatFacts character)`, and `MovementBonus(CharacterCombatFacts character)`. Use `100L * currentHP < 40L * maxHP`, living field eligibility, checked sums, and the captured configuration. Produce `RampageApplication.Reconcile(CharacterCombatFacts character, int hunterAttackBonus)` returning the unchanged instance when nothing changes, otherwise clearing invalid history and using the existing trait-attack projection. Publish a derived `RampageEffectSnapshot` when active; do not put the accumulator into a recomputable `AppliedStatus`.

- [x] Add `RampageTraitTests.ThresholdIsStrict` for 39/100 active, 40/100 and 41/100 inactive, and max HP 7 with HP 2 active/3 inactive. With n=10 assert rank bonuses `(attack,move)` = `(10,1)`, `(15,1)`, `(17,2)` before kills.
- [x] Add `KillUsesBaseAttackAndSettlesOnce`, `HealingPastThresholdClearsHistory`, `ReentryStartsAtZero`, and `NextHitReadsPriorKillGain`. Enemy base attack 8 with a temporary +20 still grants 8; two such kills grant 16. Compare registry order with CallOfBlood both ways; healing to 40% leaves no retained reward.
- [x] Run red tests and implement the pure condition, capability-based ordered hits, existing kill settlement integration, and one effective attack/movement contribution. Capture reward eligibility before kill-triggered healing; reconcile after all associated healing. Preserve dead-source/no-longer-active eligibility and checked overflow behavior.
- [x] Add `I/RampagePublicationTests.HealthMaxHealthAndCleanupPublishConsistentStats`: ordinary damage, DoT, item healing, Regeneration, soul/max-health changes, terrain normalization, reserve/death/battle cleanup; assert displayed effective attack equals the attack engine's effective input and no Run roster persistence.
- [x] Run new tests plus `TraitGrowthStatCoexistenceTests`, `CombatDamageApplicationTests`, `RegenerationTurnTests`, `HunterTraitTests`, `I/TraitKillRewardPublicationTests`, and `I/ReserveReturnPublicationTests`.

### Task 3.2: Keep movement context and undo validation strict

**Files:** Create `T/NewTraitMovementContextTests.cs`; modify `C/Application/Combat/{MovementContextCalculator,MovementUndoPolicy,MovementEffectUndo}.cs` only where new dependencies or explicit rejection are required. Preserve `C/Contracts/Results/MovementUndoResult.cs` current-eligibility validation; do not expand its exceptions.

**Interfaces:** Retain `MovementContextCalculator.Capture/Matches`, `MovementEffectUndo.Evaluate/TryApply`, and existing normal failure results. No new generic undo API or full-facts restoration is introduced. Effective movement/HP/history dependencies are captured by their canonical values.

- [x] Add `NewTraitMovementContextTests.SlowOrRampageChangeRejectsStaleContext`, `UnchangedHistoryMoveCanUndo`, `HistoryChangingMoveDoesNotProduceInvalidUndoCandidate`, and `TauntCoexistenceAvailabilityNeverThrows`. Assert valid rejection/availability and unchanged facts on rejection.
- [x] Run red tests; include the new effective-movement dependencies and historical accumulator in comparisons. Preserve the existing reversibility boundary: a move that changes HP or unsupported historical gains does not gain a new undo capability. If the existing forward-effect guard cannot restore a Rampage change, do not record a reversible checkpoint; report the existing normal unavailable/no-checkpoint result instead of accepting an invalid candidate. Do not invent a Rampage receipt unless a separately approved design changes reversibility.
- [x] Run `NewTraitMovementContextTests`, `MovementPassiveUndoTests`, `MovementUndoContractTests`, `MovementUndoBarrierTests`, `MovementUndoRulesTests`, `MovementEffectCombinationTests`, `PositionSwapMovementTests`, and `I/MovementEffectPublicationTests`.
- [x] Report the accepted and rejected paths separately, including the old Taunt case. **Stop at the Phase 3 review point.**

## Phase 4 — Designer authoring and level resolution

### Task 4.1: Complete SO/data/rank conversion and dormant assets

**Files:** Create the three Data/Authoring pairs from Section 2 and `A/TraitGrowth_{Rampage,LavaCannon,Bleeding}.asset`. Modify `TraitSpecAdapter.cs`, `D/TraitGrowth/Item/TraitRankAuthoring.cs`, `C/Contracts/Enhancements/TraitRankValues.cs`, and `C/Application/Roster/OwnedCharacterStatsCalculator.cs`; modify existing trait conversion points only for the new types. Create `T/NewTraitRankTests.cs` and `I/NewTraitAuthoringTests.cs`. Keep `TraitGrowthDefinitionTable.asset`, `CategoryTraitProgression_*.asset`, unit loadouts, and category defaults unchanged.

**Interfaces:** `RampageTraitAuthoring.ToData()` → immutable Rampage data → `RampageTraitSpec`; equivalent existing adapter contracts for Lava Cannon and Bleeding. Add `RampageRankAuthoring` with movement bonus and **attack offset**, and `BleedingRankAuthoring` with damage per stack. Extend `TraitRankValues.FromNormal/Apply` through their existing signatures. Rampage rank 2/3 resolves the normal definition's single n plus offsets 5/7, not copied absolute attacks. Bleeding rank 1/2/3 resolves damage 1/2/3; Lava has no rank rows or enhanced variant.

- [x] Add `NewTraitRankTests.RampageNIsSingleSource`: changing n from 10 to 20 changes rank attacks from `[10,15,17]` to `[20,25,27]` without editing rank rows; movement stays `[1,1,2]`. Add `BleedingLevelValuesAreOneTwoThree` and no retroactive change to already-applied stacks.
- [x] Add `I/NewTraitAuthoringTests.GetDataReturnsFreshPureData`, `AdaptersAndRankResolutionAgree`, `InvalidValuesFailBeforeActivation`, and `DormantAssetsDoNotEnterLivePools`. Use test-owned SOs and fixtures for functional conversion assertions; inspect the three delivered assets separately for their authoring contents and GUID references.
- [x] Run red tests; implement authoring, adapters, and rank mapping. n's editable initial placeholder is 0. Keep data validation before mutation, use `System.Enum` under InGame namespaces, and do not add Unity references to immutable Data.
- [x] Create the three unregistered assets with Korean display names/descriptions matching the approved rules, correct CategorySpecific/Common groups, no invented unlock levels, and preserved surrounding assets. Use the existing `.meta`/SerializeReference conventions; verify imported types in Unity when execution is available.
- [x] Run new fixtures plus `TraitGrowthRulesTests`, `TraitGrowthCatalogTests`, `OwnedTraitGrowthTests`, `TraitGrowthStatCoexistenceTests`, `I/TraitGrowthAuthoringTests`, `I/TraitGrowthBoundaryIntegrationTests`, and `I/TraitDefinitionAuthoringTests`. Include Main specialization category eligibility as supported by the existing progression contracts.
- [x] Report designer fields, resolved level values, asset paths, and unregistered activation state. **Stop at the Phase 4 review point.**

## Phase 5 — Combined verification, review, and handoff

### Task 5.1: Verify the combined change and deliver the integration contract

**Files:** Create `I/NewTraitCombinedIntegrationTests.cs` and `docs/superpowers/handoffs/2026-11-06-rampage-lava-cannon-bleeding-core-view-handoff.md`. Update this plan's evidence and the design's implementation status. Inspect existing `S/Factory/Runtime/InGame/View/CombatUnitStatusFactory.cs`, `S/RuntimeSystem/InGameSystem/ViewSystem/Combat/Display/Unit/{CombatEffectFormatters,CombatStatusDescriptionFormatter,CombatUnitStatuses}.cs`, and status/trait visual table contracts; do not edit those View files in this Phase.

**Interfaces:** Consume the final Core snapshots/events and existing Buffer read APIs. Produce a handoff listing exact keys, snapshot types/fields, rank values, lifetime interpretation, required View formatter/description/visual entries, production activation assets, and outstanding user Unity checks. Keep Core facts authoritative; View does not recalculate stacks, movement, HP thresholds, or timers.

- [x] Add `NewTraitCombinedIntegrationTests.MixedBleedingLavaRampageRoundTrip`: mixed-rank stacks plus fire/slow, a periodic credited kill, Rampage gain/healing reset, correct end-of-turn expiry, objective completion, and immutable prior Buffer publications. Exercise the complete existing Systems commit route with test-owned definitions.
- [x] Run that fixture and the full Core/CoreIntegration assemblies after all final source edits. Compare actual failure names/messages with Phase 0; investigate introduced differences without fixing unrelated baseline problems. Preserve historical results only as history, never current proof.
- [x] Review the three locality scenarios in Section 8 against the actual diff and callers. Confirm concrete inputs/outputs, failure behavior, mutation owner, order, and lifetime. Separate necessary gameplay dependencies from unnecessary implementation coupling; record out-of-scope findings without implementing them.
- [x] Write the handoff, including the runtime failure that missing required View formatter/visual registration would cause. Specify an activation sequence: implement paired View registrations → verify statuses in Unity → choose designer n → register desired growth catalog/pools → run production asset checks. No message is sent to another person by this task.
- [x] Run static checks and record the final source state/evidence. Report Core verification, unexecuted checks, dormant assets, and user PlayMode/DataBaseAssetTests checks separately. **Stop at the Phase 5 review point; do not commit/push or claim end-to-end production activation.**

## 8. Locality acceptance: three real change scenarios

| Scenario | Current burden and cause | Judgment and minimum action | Expected after-change understanding | Verification that remains necessary |
|---|---|---|---|---|
| Adjust Rampage movement or Lava slow | Admission/range and Domain route validation independently calculate allowance; AI also gates movement. | Repeated rule is unnecessary implementation coupling. Consolidate into `VoluntaryMovementRules`; preserve base-zero/forced-move semantics. | Rule inputs, contribution ownership, observer environment, and finite/null result; callers need not inspect another caller's arithmetic. | Query/execution/AI consistency, GroundCompaction, zero allowance, context staleness, undo; these share a real movement contract. |
| Add/change Bleeding application versus Lava splash application | Direct-hit code dispatches individual status collaborators; original target, redirected recipient, and splash selection have different meanings. | Clarify contact order and inputs. Keep captured-area application separate because its gameplay trigger differs; do not unify by superficial code shape. | Bleeding's rule + actual-contact contract; Lava's rule + resolved-area contract. Neither requires reading the other's targeting internals. | Shield/Sacrifice/reflection/extra/reaction/preview combinations are necessary shared attack behavior, not avoidable regression overhead. |
| Change periodic duration or Rampage kill gain | Status reduction, turn orchestration, death/credit, stat projection, and publication cross existing boundaries. | Keep canonical state and existing common damage/completion owners. Expose clock and reward ordering; no private timer, reward store, or duplicated HP settlement. | Pure status/rank rules and the explicit start/end/kill transition contract; no need to reimplement reward/soul/Buffer internals. | Lethal tick outcomes, source attribution, HP/max-HP changes, snapshots, cleanup, and once-only publication remain required. |

For each Phase report use: **change scenario → current burden/evidence → required vs unnecessary coupling → action → expected/actual scope difference → preserved contracts and verification → evidence level**. Improvements are predictions until code exists; static source confirmation is distinct from executable proof. File/class/interface counts are not acceptance metrics. If a proposed helper only adds traversal with no reduction in required knowledge, retain the existing structure and record the reason, without silently changing the selected gameplay or cross-task interfaces.

## 9. Execution record and plan self-review

| Phase | Status | Evidence / remaining work |
|---|---|---|
| Planning | Complete | Design, current owning code, and supplied locality guide reviewed; document-only changes. |
| 0 | Complete with baseline failures | Both assemblies ran. 5,785 of 5,857 passed. Focused Taunt and movement undo passed 23 of 23. See execution evidence below. |
| 1 | Complete with baseline failures unchanged | Focused 310/310. Full 5,799/5,871 with all 72 baseline failure names and messages unchanged. |
| 2 | Complete with baseline failures unchanged | Final full assemblies 5,878/5,950; all 72 prior failures unchanged, no new failures. All newly added Phase 2 cases pass. |
| 3 | Complete with baseline failures unchanged | Final full assemblies and independent read-only review completed. No actionable review findings; Phase 4 is the next review boundary. |
| 4 | Complete | Three dormant assets imported and verified. Phase 4 focused regression passed 106/106. |
| 5 | Complete with baseline failures unchanged | Combined fixture passed. Full regression 5,888/5,960, with the same 72 failure names and messages as Phases 0 and 3. |

Self-review: gameplay requirements map to Tasks 2.1–4.1; actual post-hit versus captured-area semantics remain distinct; shared lifetime/settlement responsibilities are preserved; all five Review Focus cases have owning tests; exact finite file families and signatures align between Phases; Core-only tests and paired View handoff remain separate. Required evidence is specified, not claimed. Task planning does not authorize implementation.

### Phase 0 execution — 2026-11-06

- Baseline checkout: `bca934e089f92f52743121b4b070369c023dca12` on `feature/Cheon/Core/NewCombatSytem`. The plan and spec were already untracked, and no tracked changes were present before Unity ran. The two requested phases use the existing checkout because there were no overlapping source edits and the batch tests use its existing Unity import cache.
- Editor: `D:\Program Files\Unity\Hub\Editor\6000.0.73f1\Editor\Unity.exe`, resolved from the local Hub install root. Batch runs use AppData-permitted execution and write NUnit XML and Unity logs under `C:\Temp\ProjectL-Rampage-Lava-Bleeding-bca934e08`.
- Attack: `CombatAttackCalculator` admits the attack and captures selection and effect bindings. `CombatAttackDelivery` resolves deliveries. `AttackDeliveryExecutor` orders direct damage, forced arrivals, action-use settlement, optional kill-chain work, and scope finish. `AttackDirectDamageCalculator.FlushHealth` applies numeric damage through `CombatDamageApplication` before invoking contact collaborators. `KillChainAttackCalculator` and `SacrificeDamagePlanner` also call the lower-level Weakness Exposure rule in their own existing paths. `TraitKillSettlement` owns the kill reward boundary. `MovementProjection.Complete` creates the final transition. The command and advance services pass it to `CombatTransitionPublisher.Publish` and `BattleTransactions.Apply`; `CharacterData` projects statuses with `StatusEffectReadModel.Capture`. Preview uses an isolated projection and does not publish.
- Movement: `CombatActionAdmission.GetAvailability` and `CombatMovementCalculator.Range/UnitMoveCells/Actor` consume the allowance query. `MovementPathNormalizer` checks the observer-visible budget, and `MovementRouteValidator.Validate/StepBudget` checks the route. `EnemyMovementPathfinder.TryFind` uses the enemy-visible environment and the same route validator. `MovementProjection` records the move; `MovementUndoPolicy` captures and validates evidence. `MovementUndoCalculator.Evaluate` reaches `MovementEffectUndo.Evaluate`, which restores the allowed effects, recalculates presence, and constructs a candidate checked by `MovementUndoResult` and `CombatCommandTransition`. The existing `CurrentEligibility` exception applies to recomputable states only.
- Existing test ownership: `StatusEffectContractTests` (GwonCheonmyeong, `941fe4f83`) asserts the exact key inventory in `Catalog_ReservesStableUniqueKeysAndSeparatesClassification`. `CombatTraitContractTests`, `TraitDefinitionContractTests`, and the authoring fixtures `RetiredTraitAuthoringTests`, `TraitDefinitionAuthoringTests`, `TraitGrowthAuthoringTests`, and `TraitGrowthWeightAuthoringTests` also have GwonCheonmyeong history. Phase 1 does not require any existing expectation changes.
- Full baseline (`baseline-full.xml`, `baseline-full.log`): Core 4,902/4,927, CoreIntegration 883/930. The XML retains all 72 failure names and messages. Failures include night and recall expectations, production asset expectations, View fixtures, and the Cinemachine camera-offset type load. No existing fixture listed for Phase 1 failed. The separate `TraitActivationOutcomeTests.Taunt_OnlyAnAppliedForcedDecisionActivates(True)` baseline expects one activation and receives two.
- Focused baseline (`baseline-taunt.xml`, `baseline-taunt.log`): `TauntTraitTests` 15/15, `MovementPassiveUndoTests` 5/5 (including both availability and undo cases), and `TauntPublicationTests` 3/3. No compile errors were reported. The requested refactor has a clean focused comparison set.
- Ruling: the user authorized both phases in the same request. The Phase 0 result was reported before beginning Phase 1. Existing failures outside the Phase 1 regression fixtures remain the full-suite comparison baseline.

### Phase 1 execution — 2026-11-06

- Characterization before changing delivery and movement behavior: the initial run exposed a test fixture that assumed a parameterless Hunter's Mark snapshot constructor. After installing the explicit snapshot in the fixture, `phase1-ordered-red.xml` recorded six passing behavior cases, three passing negative capability cases, and five expected positive capability failures. The new engine query was available, the five rules did not yet declare the capability, and the old delivery predicate remained active during that run.
- Movement locality: a future allowance contribution now changes the single `VoluntaryMovementRules.Allowance` formula and its owning trait or status rule. Admission retains observer selection, route validation retains its failure reasons, and AI uses movement admission and route validation. Forced displacement has its own path. Query, execution, AI, terrain and visibility, stale context, and undo remain shared-contract verification obligations.
- Attack locality: delivery asks the engine whether the captured attacker bindings require sequential settlement and still asks `TraitPresenceReconciler` about defender ordering. Individual rules declare the five existing attacker requirements. The contact dispatcher makes original target, actual recipient, and damage origin explicit and calls the three existing collaborators in the same order. Geometry, damage, and final publication retain their original owners. Sacrifice, reflection, preview, extra attacks, and damage-dependent events still require regression coverage. Completion runs from one common exit after optional kill-chain handling.
- Changed runtime files under `C/`: `Domain/Movement/VoluntaryMovementRules.cs`, `Domain/Movement/MovementRouteValidator.cs`, `Application/Combat/CombatActionAdmission.cs`, `Application/Combat/EnemyAi/EnemyMovementPathfinder.cs`, `Application/Combat/AttackDeliveryExecutor.cs`, `Application/Combat/AttackDirectDamageCalculator.cs`, `Application/Combat/TraitContactApplication.cs`, `Domain/CombatEffects/CombatEffectEngine.cs`, `Domain/CombatEffects/ICombatEffectEngine.cs`, `Domain/CombatEffects/Rules/CombatEffectCapabilities.cs`, and `Domain/Traits/Effects/{ArmorBreaker,FinishingShot,TrackingShot,Hunter,MoraleBoost}EffectRule.cs`. Added `T/VoluntaryMovementAllowanceTests.cs` and `T/AttackBoundaryLocalityTests.cs`. Each new source has a companion `.meta`. The execution record in this plan is the only document updated.
- Focused regression (`phase1-focused.xml`, `phase1-focused.log`): 310/310, including all named Phase 1 fixtures and 14 new test cases. Full comparison (`phase1-full.xml`, `phase1-full.log`): Core 4,916/4,941 and CoreIntegration 883/930. Comparison by full case name and failure message found zero new, resolved, or changed failures. No C# compilation errors were reported.
- Static checks found no whitespace errors, missing source metadata, new Unity dependency, or production catalog changes. The batch-generated FMOD catalog and two TMP font asset changes were restored to the baseline. The index and HEAD stayed at their original state.
- Independent source review found no actionable findings in movement, ordered delivery, completion, contact, test ownership, or Framework/Buffer boundaries. Phase 1 was the stopping point of the preceding request. Phase 2 owns the new status and contact/area mechanics. PlayMode was not exercised.

### Phase 2 execution — 2026-11-06

- Continued the existing feature checkout and preserved the uncommitted Phase 1 changes. This request authorizes both Phase 2 and Phase 3; the Phase 2 result was reported before beginning Phase 3. No commit or push was made.
- Canonical immutable Fire, Bleeding, and MovementRangeDecrease values own recipient state. The pure rules preserve mixed stack magnitudes/origins, the five-stack cap, future affected-side starts, and independent next-side-end slow expiry. Voluntary movement applies the slow through the existing shared allowance rule.
- Turn start captures the periodic batch after existing resets/shield expiry, then delegates each source-specific tick to CombatDamageApplication and final settlement to MovementProjection.Complete. The original living-at-start reinforcement count is preserved; terminal batches do not reinforce or open input. Read-model snapshots retain immutable canonical values.
- Bleeding uses actual surviving hostile contact recipients, including shield absorption and Sacrifice. Lava Cannon captures the resolved primary/splash enemy IDs and applies after direct/forced-arrival processing even after source reflection death. Neither recalculates geometry or writes Buffer directly.
- The existing KillChainAttackCalculator uses a separate singleton-hit pipeline rather than a child CommandAttackPlan. Its two missing new-effect callbacks were reproduced (0/2) and connected to the same contact/captured-area collaborators with its existing selected singleton and captured source bindings. Its old targeting and prior effect behavior were preserved. This necessary integration adds that file to the Task 2.3 change surface.
- Evidence directory: `C:/Temp/ProjectL-Rampage-Lava-Bleeding-phase23`. Pure-rule RED: `phase2-1-red.xml` (0/7); periodic RED: `phase2-2-red2.xml`, with corrected request fixture in `phase2-2-publication-red.xml` (0/3); contact/area RED: `phase2-3-red.xml`; extra-hit RED: `phase2-3-extra-red.xml` (0/2). Attack/publication regression: `phase2-3-green2.xml` (193/193). Final Phase 2 selection: `phase2-boundary.xml` (300/302).
- The boundary failures were `RegenerationTurnTests.EighthEnemyCompletion_DoesNotHealPersistentMain` (same name/message as Phase 1) and `StatusEffectContractTests.Catalog_ReservesStableUniqueKeysAndSeparatesClassification` (the new keys exceeded its old exact inventory). At that checkpoint existing expectations were untouched; the user subsequently approved the inventory update, which is verified in the final comparison below.
- The current SoulEffectCalculator.PlanDeaths deliberately returns an unchanged field. The new publication test verifies that existing behavior alongside death, source credit, kill gold, trait rewards, and stage statistics; no new soul generation was introduced. A duplicate publication attempt is rejected with stores unchanged.
- Locality result: status magnitudes/caps/clocks are owned by their immutable values and pure rules; orchestration only orders capture, common damage, consumption, and completion. The attack integration distinguishes actual contact from accepted area membership. Existing attack, damage, completion, and Systems/Buffer owners remain authoritative. New sources have metadata; no live catalog, View, scene, or authoring activation was changed. Unity-generated FMOD/TMP asset changes will be restored after the final test run. PlayMode is unverified.

### Phase 3 execution — 2026-11-07

- The user approved necessary existing key/registration inventory expectation updates. StatusEffectContractTests now includes keys 15–18 with the correct classifications; its behavioral assertions remain unchanged and passed in the Phase 3 selection.
- Rampage derives eligibility from living field facts and the strict integer HP comparison. Its runtime history stores only accumulated defeated-enemy base attack. The existing trait-attack projection adds the conditional contribution once, and the shared voluntary-movement rule adds movement once. No active flag, applied-history status, or roster gain was introduced.
- TraitKillSettlement awards Rampage before Hunter/CallOfBlood/MoraleBoost settlement. Shared health/placement changes reconcile the condition immediately, including recovery across 40%. The damage projection and final settlement now reconcile against their already-proposed owned rewards, preventing a fresh Hunter reward from being replaced by the command's earlier owned snapshot.
- Health-change integration uses existing owners: CharacterSettlementCalculator.Copy, CombatCommandCalculator.Copy, PositionTileModifierCalculator.Replace, CombatPlacementCalculator.Location, and ArtifactActionProjection.ProjectCollection. These replace only the previous Rampage contribution while retaining other projected bonuses. Battle cleanup clears kill history; the condition itself remains derived from the final HP/field facts until the battle records are removed. No runtime accumulator is written to the Run roster.
- Rampage RED: `phase3-1-red2.xml` (12 intended new-behavior failures; existing contract/inactive/validation cases pass). Publication RED: `phase3-1-publication-red2.xml` (0/9). Rampage/health/kill/presence selection: `phase3-1-green.xml` (113/114), with only the unchanged `RegenerationTurnTests.EighthEnemyCompletion_DoesNotHealPersistentMain` failure.
- Movement RED: `phase3-2-red2.xml` reproduced the missing slow dependency both in Core Matches and in the actual Systems command route. Rampage stat/config changes were already partly detected; the comparison now explicitly includes its canonical history/configuration as well. The command service continues to own stale-context admission rather than duplicating it in raw movement calculation.
- Movement selection: `phase3-2-green.xml` (143/144), with only the unchanged `PositionTileModifierTests.NightRecallAndPlacement_ReconcileOnlyCurrentPositionContribution` failure. Unchanged-history movement (with/without slow) and Taunt coexistence undo successfully. A movement that normalizes maximum HP and clears Rampage history receives no checkpoint and returns the normal NoCheckpoint result. Existing forward-event/character guards already enforce this boundary, so MovementEffectUndo, MovementUndoPolicy, and MovementUndoResult required no new exception or receipt.
- Final verification adds explicit zero-percent splash versus contact, structure-only Lava, periodic kill eligibility for active/inactive/dead sources, and runtime clone/codec checks. Core and CoreIntegration full-suite comparison plus a fresh read-only review are pending. Phase 4 authoring/rank adapters/assets and Phase 5 handoff remain unstarted.
### Final Phase 2/3 verification and review — 2026-11-07

- Final evidence: `C:/Temp/ProjectL-Rampage-Lava-Bleeding-phase23/phase23-full-final.xml` and `.log`. Core: **4,980/5,005**; CoreIntegration: **898/945**; total **5,878/5,950**. Comparison against Phase 1 found **zero new, resolved, or changed failure names/messages**. All **72** baseline failures are retained. The final log contains no C# compilation errors.
- The first combined run (5,874/5,950) found three exact registry-count expectations and one missing Main-specialization definition-comparison integration. The user-authorized inventory changes update only counts/classification. `S/RuntimeSystem/InGameSystem/CoreSystem/CharacterSystem/MainSpecializationDefinitionSnapshot.cs` now includes the three new spec payloads in its existing source comparison; the existing exhaustive payload test supplied the failing case and passes unchanged after the repair. This is compiled Core definition compatibility, not Phase 4 SO/data adapter implementation.
- A fresh read-only reviewer inspected the complete Phase 2/3 source and the compatibility repair. No actionable Critical, Important, or Minor source findings were reported. The review covered captured-area versus actual-contact semantics, source/clock immutability, whole-batch settlement, Rampage/Hunter/healing order, and strict undo ownership. No new service hierarchy, state store, or unrelated refactor was introduced.
- Verification limit: `SnapshotBatchDoesNotRetickNewStates` verifies deterministic captured processing and immutable prior values. It does not inject a status refresh during processing; the captured-reference guard for that hypothetical path was checked statically. Current periodic damage does not invoke contact applications. No View, scene, PlayMode, or production-activation acceptance is claimed.
- Review scope rulings: Phase 4 authoring/rank adapters/assets and Phase 5 combined fixture/handoff remain deferred by the user's requested boundary. Existing KillChain geometry and earlier Phase 1 behavior were preserved and regression-tested rather than redesigned. Existing baseline failures were recorded without unrelated repair. Unity-generated FMOD catalog and TMP font changes were restored to the pre-session content after testing.
- Static verification: source/metadata pairing, new pure-code Unity-reference checks, UTF-8/LF normalization, and whitespace checks completed. The index remains empty and HEAD remains `bca934e089f92f52743121b4b070369c023dca12`. No commit or push was made. Stop at the Phase 3 review point.

### Exact Phase 2/3 source and test change manifest

Aliases are defined in Section 2. New source files include companion metadata. This list excludes unchanged pre-existing Phase 1-only edits.

- `S/RuntimeSystem/InGameSystem/CoreSystem/CharacterSystem/MainSpecializationDefinitionSnapshot.cs`
- `C/Application/Artifact/ArtifactActionProjection.cs`
- `C/Application/Combat/AttackDeliveryExecutor.cs`
- `C/Application/Combat/CharacterSettlementCalculator.cs`
- `C/Application/Combat/CombatCommandCalculator.cs`
- `C/Application/Combat/CombatCommandEffects.cs`
- `C/Application/Combat/CombatDamageApplication.cs`
- `C/Application/Combat/CombatEffectiveStatsCalculator.cs`
- `C/Application/Combat/CombatPlacementCalculator.cs`
- `C/Application/Combat/CombatTemporaryStateCleanup.cs`
- `C/Application/Combat/CombatTurnCalculator.cs`
- `C/Application/Combat/CommandAttackPlan.cs`
- `C/Application/Combat/KillChainAttackCalculator.cs`
- `C/Application/Combat/LavaCannonApplication.cs`
- `C/Application/Combat/MovementContextCalculator.cs`
- `C/Application/Combat/MovementProjection.cs`
- `C/Application/Combat/PeriodicStatusApplication.cs`
- `C/Application/Combat/PositionTileModifierCalculator.cs`
- `C/Application/Combat/RampageApplication.cs`
- `C/Application/Combat/SacrificeDamagePlanner.cs`
- `C/Application/Combat/StatusEffectReadModel.cs`
- `C/Application/Combat/TraitContactApplication.cs`
- `C/Application/Combat/TraitKillSettlement.cs`
- `C/Application/Combat/TraitPresenceReconciler.cs`
- `C/Application/Combat/Traits/BleedingTraitFactories.cs`
- `C/Application/Combat/Traits/LavaCannonTraitFactories.cs`
- `C/Application/Combat/Traits/RampageTraitFactories.cs`
- `C/Application/Combat/Traits/TraitEffectProvider.cs`
- `C/Application/Combat/Traits/TraitRegistry.cs`
- `C/Application/Combat/TraitTurnLifecycle.cs`
- `C/Contracts/CombatEffects/AppliedStatus.cs`
- `C/Contracts/CombatEffects/BleedingStatus.cs`
- `C/Contracts/CombatEffects/FireStatus.cs`
- `C/Contracts/CombatEffects/MovementRangeDecreaseStatus.cs`
- `C/Contracts/CombatEffects/StatusEffectKey.cs`
- `C/Contracts/Commands/Traits/BleedingTraitSpec.cs`
- `C/Contracts/Commands/Traits/LavaCannonTraitSpec.cs`
- `C/Contracts/Commands/Traits/RampageTraitSpec.cs`
- `C/Contracts/Common/TraitKey.cs`
- `C/Contracts/Snapshots/StatusEffectSnapshot.cs`
- `C/Contracts/Snapshots/Traits/BleedingTraitStatusSnapshot.cs`
- `C/Contracts/Snapshots/Traits/LavaCannonTraitStatusSnapshot.cs`
- `C/Contracts/Snapshots/Traits/RampageTraitStatusSnapshot.cs`
- `C/Domain/CombatEffects/Rules/PeriodicStatusRules.cs`
- `C/Domain/Movement/VoluntaryMovementRules.cs`
- `C/Domain/Traits/Definitions/BleedingTraitDefinition.cs`
- `C/Domain/Traits/Definitions/LavaCannonTraitDefinition.cs`
- `C/Domain/Traits/Definitions/RampageTraitDefinition.cs`
- `C/Domain/Traits/Effects/BleedingEffectRule.cs`
- `C/Domain/Traits/Effects/LavaCannonEffectRule.cs`
- `C/Domain/Traits/Effects/RampageEffectRule.cs`
- `C/Domain/Traits/Effects/TraitEffectStateCodec.cs`
- `C/Domain/Traits/Rules/BleedingTraitRule.cs`
- `C/Domain/Traits/Rules/LavaCannonTraitRule.cs`
- `C/Domain/Traits/Rules/RampageTraitRule.cs`
- `C/Domain/Traits/States/BleedingRuntimeState.cs`
- `C/Domain/Traits/States/LavaCannonRuntimeState.cs`
- `C/Domain/Traits/States/RampageRuntimeState.cs`
- `T/BleedingTraitTests.cs`
- `I/NewTraitAttackPublicationTests.cs`
- `I/PeriodicStatusPublicationTests.cs`
- `I/RampagePublicationTests.cs`
- `T/IntimidationTraitTests.cs`
- `T/LavaCannonTraitTests.cs`
- `T/LegacyEnhancementRetirementTests.cs`
- `T/NewTraitMovementContextTests.cs`
- `T/PeriodicStatusRulesTests.cs`
- `T/PeriodicStatusTurnTests.cs`
- `T/RampageTraitTests.cs`
- `T/RegenerationTraitTests.cs`
- `T/StatusEffectContractTests.cs`

### Phase 4 execution — 2026-11-07

- Continued the existing checkout with the prior Phase 0–3 changes intact. Unity evidence is under `C:/Temp/ProjectL-Rampage-Lava-Bleeding-phase45`. The user requested both Phase 4 and Phase 5, and the Phase 4 result was reported before Phase 5 work.
- `phase4-rank-red2.xml` confirmed three intended rank failures before the mapping was implemented. `phase4-focused-final.xml` and its log record 106/106 passing tests, including real SO import, existing growth/authoring, and Main specialization. The first focused run caught an invalid test fixture unlock level of 1, corrected to the existing minimum level of 2.
- Rampage stores n once in the normal SO payload, with placeholder 0, offsets 0/5/7, and movement 1/1/2. The new rank rows store movement and offset. Bleeding carries one positive per-stack damage value per rank, authored as 1/2/3. Lava Cannon has a fixed payload and no enhancement or rank rows. Rampage rank overflow is checked when the Core growth definition is assembled.
- The authoring and rank code lives in the three `D/Unit/Trait/*TraitData.cs` and `*TraitAuthoring.cs` pairs, `TraitRankAuthoring.cs`, `TraitSpecAdapter.cs`, `TraitRankValues.cs`, and `TraitGrowthDefinition.cs`. `OwnedCharacterStatsCalculator` already projects the resolved `TraitSettingsSet` and rank map via `OwnedUnitSpecResolver`. The new integration test checks those settings against the battle profile for a Rider-specialized Main, so the calculator required no parallel mapping.
- `A/TraitGrowth_Rampage.asset`, `A/TraitGrowth_LavaCannon.asset`, and `A/TraitGrowth_Bleeding.asset` import with CategorySpecific, CategorySpecific, and Common groups respectively. The production growth definition table, progression pools, defaults, and unit definition table have no dependency on the three assets.

### Phase 5 locality review — 2026-11-07

| Change scenario | Modification and understanding scope | Necessary verification |
|---|---|---|
| Adjust Rampage movement or Lava slow | The earlier admission and route formulas collapsed into `VoluntaryMovementRules.Allowance`. Rampage configuration resolves through its own trait and rank rules. Slow lifetime lives in `PeriodicStatusRules`. A contributor change can stay in its rule plus its authoring values. The caller contract still supplies the observer-specific environment and distinguishes disabled from a finite allowance. `MovementContextCalculator` checks canonical slow and Rampage history/configuration. | The movement budget is shared by query, route, and AI. GroundCompaction, zero base, forced movement, stale-context admission, and undo still need coverage. Undo keeps its existing historical-state guard. |
| Change actual-contact Bleeding or captured-area Lava | Direct delivery delegates contact after damage settlement to `TraitContactApplication`; the selected target and actual recipient have separate identities. `LavaCannonApplication` consumes the captured area after direct delivery and keeps the captured rule when reflection kills its owner. Each feature owns its trigger. Neither rebuilds selection geometry or introduces another damage writer. | Sacrifice, reflection, zero HP loss after absorption, extra and reaction delivery, and preview isolation exercise genuinely shared attack order. |
| Change periodic duration or Rampage kill gain | Recipient lifetimes and merges have one policy in `PeriodicStatusRules`. `PeriodicStatusApplication` captures and consumes the affected-side batch. `CombatDamageApplication` and `TraitKillSettlement` retain damage and credited-death ownership. Rampage's history remains in its own snapshot and reconciliation uses the existing stat owner. Status snapshots and events project the facts through the existing Buffer transaction. | Turn ordering, health/max-health threshold crossings, per-stack attribution, cleanup, completion and immutable publication remain integration obligations. There is no alternate clock, kill store, or Buffer mutation path. |

The combined test and full regression exercise those shared boundaries. The independent read-only review found no actionable issues in authoring, rank resolution, asset references, test isolation, or the handoff's Buffer/View contract. The View formatter and visual registrations are an activation dependency recorded in the [Core-to-View handoff](../handoffs/2026-11-06-rampage-lava-cannon-bleeding-core-view-handoff.md).

### Phase 5 execution — 2026-11-07

- Added `I/NewTraitCombinedIntegrationTests.cs` and its metadata. Test fixture iterations corrected enemy deployment coordinates, the Main's existing persistent attack, and the explicit eligible kill-reward definition. The final scenario uses command and automatic services through the existing publisher and asserts death credit, Run kill count and gold, Rampage reward and threshold reset, slow expiry, objective victory, and immutable earlier Buffer snapshots.
- `phase5-combined-final.xml` passed 1/1. `phase5-full-final.xml` and its log record Core 4,983/5,008 and CoreIntegration 905/952, for 5,888/5,960 overall. All ten new Phase 4/5 cases passed in the full run. Comparing both the Phase 0 and Phase 3 XML reports by case name and failure message found zero new, resolved, or changed failures. There were no C# compilation errors.
- `DataBaseAssetTests` ran in the full selection, with 69/76 passing and the same seven pre-existing failures. The three new dormant asset import and reference checks passed separately in `NewTraitAuthoringTests`. Post-activation database and PlayMode checks are listed in the handoff.
- Static checks covered whitespace, metadata pairing and GUID uniqueness, pure data, and live asset references. The three batch-generated FMOD/TMP changes were restored. The checked growth definition was normalized to LF with identical source text. The final index is empty and HEAD remains `bca934e089f92f52743121b4b070369c023dca12`.
