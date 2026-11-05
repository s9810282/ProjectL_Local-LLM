# Configurable Trait System Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox syntax for tracking. Execute only the Phase and owner scope explicitly authorized by the user, then stop for review. The user handles all commits and pushes.

**Goal:** Implement the 22 currently scoped traits through editable SO definitions and existing combat/Buffer contracts, with correct action/strike timing and zero trait carryover into a new Run.

**Architecture:** Extend the current trait registry, pure effect rules, command projections, immutable facts, and atomic Buffer publication. Keep trait formulas in their own rules and shared execution order in the existing attack, damage, movement, and controller-turn owners. Category SO pools remain the sole source of category membership.

**Tech Stack:** Unity 6000.0.73f1, C#, existing Core.Contracts/Core.Domain/Core.Application and ProjectL.Systems assemblies, ScriptableObjects, NUnit EditMode tests. No new package dependency.

**Spec:** [Configurable Trait System Design](../specs/2026-10-28-trait-system-design.md), including the final confirmed Finishing Shot/Sacrifice decision.

**Additional reference:** [Feature change locality guidelines](D:/My_ProjectWork/학교%20관련%20문서/졸업작품_프로젝트L/2학기%20내용/AI%20관련%20문서/feature-change-locality-guidelines.md).

**Post-E review input:** [Combat trait activation Core request, 2026-10-29](C:/Users/권천명/Downloads/2026-10-29-combat-trait-activation-core-request.md). This is a View-authored draft based on a different checkout; its R1–R7 requirements and Q1–Q8 recommendations are review input, not evidence of current behavior or user approval of every proposed API. The user's current request authorizes this plan revision only.

**Status:** A–E/E+ and subsequent remediation remain implemented. F1 is prepared, now amended for verified R0–R3 legacy retirement: **31 executable producers (22 new + nine surviving legacy), five independently deferred registrations, six retired Trait reservations**. Fresh retirement gates: Core4089/4089 and CoreIntegration344/345, zero skips, only the unchanged StageKey2101 catalog failure. See [current retirement handoff](../handoffs/2026-10-29-legacy-enhancement-retirement-handoff.md), [F1](../handoffs/2026-10-28-trait-system-core-view-handoff.md) and §11.8. External delivery, View/Flow implementation, new production SO/pools and F2 joint/manual acceptance remain pending; Heavy steps/pool are temporarily empty by user direction. Historical E+37-producer evidence in §11.6 and original F1 evidence in §11.7 remain unchanged. R4 and Main specialization are separate unimplemented work. No commit/push.

**Execution order:** E1–E3 completion and Phase E report → separately authorize E+ → E+1–E+4 corrections/activation facts and report → F1 Core-to-View handoff → F2 approved content and external acceptance. E+ is neither an extra task inside the running E nor authorization to start View work. Historical mentions of E1–E4 or F1–F3 in §11.4 are stale labels: the actual tasks are E1–E3 and F1–F2, with the new E+ tasks defined in §7A.

## Global Constraints

- Implement 22 traits. Defer Position Swap and Recon Squad until completed-movement undo is restored through a separate design/plan.
- Every trait effect and every stat increase caused by a trait is bounded by the current Run. No trait contribution carries into a new Run. Existing artifact exceptions do not apply to traits.
- Category membership is editable through CategoryTraitProgressionDataSO.pool. Reuse one definition across pools; do not add a second authoritative category field.
- All numeric values in design §6.2 are SO fields, including concrete defaults, duration counts, limits, thresholds, and radii. Required TBD values fail configuration validation; fixture numbers are not approved production defaults.
- Preserve native traits, existing growth offers/receipts, XP/stat growth, Main baseline ownership, and Main's separately configured candidate pool.
- Preserve Focus and HuntersMark identity and current availability. The new Hunter has its own key. Do not reactivate deferred legacy traits.
- Count Vanguard/Clean Sweep once per attack action. Re-evaluate damage conditions and apply Armor Breaker separately for sequential strikes. Acceleration strengthens the whole multi-hit action.
- Morale uses defeated-enemy base ATK × 20%; per-kill half-up rounding, then sum; field/alive recipients only; current-turn expiry.
- Trails include start/end, have no identical-effect numeric stacking, and expire at the next opposing-side turn end. This follows the user's explicit allied creation → enemy turn end example.
- Finishing tests pre-strike HP with strict less-than, ignores shields, and does not trigger or consume Sacrifice. HP 30 → 15 with ATK 100 does not execute on that strike; a later strike starting at 15 may.
- Core rules and Buffer changes belong to the Core owner. View, Flow/Job, scenes, prefabs, FieldCamera, and Architecture remain with their existing owners. Handoff is a Core deliverable; external delivery/acceptance is separate.
- Implementation documents are English; code/XML comments are Korean. Follow project namespaces, Allman formatting, UTF-8/LF, .meta/GUID, serialized-type, and exception rules.
- Preserve Framework/Buffer/Job ownership and existing registration/lifecycle contracts. Add neither a second state store nor a universal effect framework.
- E+ adds activation facts to existing ordered command results. Persistent trait state remains in existing character/environment/owned facts. Do not add a trait Buffer, polling loop, parallel event channel, new gameplay hook without a concrete scoped mechanic, or View-side rule calculation.
- Stop after each authorized Phase. A plan departure affecting gameplay, public contracts, or ownership requires a design update and user decision. Technical choices explicitly listed below belong to this plan's review.
- Codex does not commit or push. Do not add automatic commit steps.

## Review Focus

1. A failed/stale publication or preview must not consume shields, advance Hunter RNG/cap, grant Run ATK, or publish speculative activations. Owned-record, combat-state, and result changes commit together. Tests: A2, E3, E+4.
2. A repeated hit must observe earlier HP/DEF changes without repeating root action costs, Vanguard/Clean Sweep notifications, or Acceleration consumption. Tests: B1, D3, E1/E2.
3. Losing one aura/trail source must not remove another source, refill a spent shield, or delete a weaker unexpired trail. Tests: C1, D1.
4. Recall/night end is not stage end; stage end is not new Run. Preserve only the state whose declared lifetime allows it. Tests: B3, D3, E1/E3.
5. Direction/All recipients named Designated are not individual targets. Disarm/Ankle must use the actual Primary-derived SelectedTargetId. Finishing must not be reclassified as normal lethal damage. Tests: B3, C3, E2.
6. A trait owner can differ from the damage creditor; grouping by the owner or attack-group number alone must not attach defensive activations to the wrong hit. One multi-target activation must not become repeated owner actions. Tests: E+1/E+2.
7. Start-cell trails, completed steps, interrupted movement, and expiry need ordered source facts; final environment snapshots alone cannot establish their playback moment. Tests: E+3/E+4.

---

## 1. Execution and verification contract

Recheck branch, status, applicable AGENTS instructions, and concurrent edits when a Phase is authorized. Planning inspected feature/Cheon/Core/NewCombatSytem. Preserve the untracked design and this plan. Isolation decisions belong to execution; do not create a worktree merely to write this document.

Before Phase A, capture a fresh Core/CoreIntegration baseline. Historical counts are not acceptance evidence. A baseline failure must be recorded with its test name and relation to the current change; do not silently accept or repair unrelated failures.

### 1.1 Test runner recipe

A1 creates the local evidence runner at .superpowers/trait-system/run-tests.ps1 by adapting the inspected .superpowers/sdd/2026-10-15-heavy-knockback-immunity/run-tests.ps1. Keep its hidden Start-Process launch and unique XML/log names. Add an Assembly parameter forwarded to -assemblyNames; this argument is documented by the project's installed Unity Test Framework in Library/PackageCache/com.unity.test-framework@082c24152fd4/Documentation~/reference-command-line.md. The parameter takes one exact assembly name for the runs below.

Runner contract:

~~~
param(
    [Parameter(Mandatory = $true)][string]$Run,
    [Parameter(Mandatory = $true)][string]$Assembly,
    [string]$Filter
)
~~~

Use the project's verified editor path D:/Program Files/Unity/Hub/Editor/6000.0.73f1/Editor/Unity.exe, rechecking it at execution. Use -batchmode -nographics -projectPath, -runTests, -testPlatform EditMode, -assemblyNames, optional -testFilter, -testResults, and -logFile. Do not add -quit to a test run. Store evidence under .superpowers/trait-system/<Run>.{xml,log,exit.txt}; reject an existing name.

Run the editor through the approved route with AppData access. If the sandbox blocks AppData, use approved elevated execution; do not upgrade Unity or delete CurlRequestCache.db. Do not launch another editor for the same project while one is already using it.

For task-focused checks, substitute the Task's exact test class and assembly:

~~~
pwsh -File .superpowers/trait-system/run-tests.ps1 -Run A1-red -Assembly ProjectL.Tests.Core -Filter ProjectL.Tests.Core.TraitDefinitionContractTests
pwsh -File .superpowers/trait-system/run-tests.ps1 -Run A1-green -Assembly ProjectL.Tests.Core -Filter ProjectL.Tests.Core.TraitDefinitionContractTests
~~~

Task red runs fail for the newly specified behavior. A missing API may first require the minimum compiling contract skeleton; infrastructure/compilation failure alone is not a meaningful red result. Each green run must produce XML with all selected cases present, total > 0, result Passed, failed = 0, and no unexpected skips. Process exit alone is insufficient.

At every Phase end, run these two commands sequentially with that Phase letter and fresh evidence names:

~~~
pwsh -File .superpowers/trait-system/run-tests.ps1 -Run A-core -Assembly ProjectL.Tests.Core
pwsh -File .superpowers/trait-system/run-tests.ps1 -Run A-integration -Assembly ProjectL.Tests.CoreIntegration
~~~

Core is the existing pure test assembly at Assets/Tests/EditMode/ProjectL.Tests.Core.asmdef; CoreIntegration is the existing Systems/Unity-capable assembly at Assets/Tests/EditMode/CoreIntegration/ProjectL.Tests.CoreIntegration.asmdef. No legacy Framework test assembly is assumed. Authoring/asset tests for this work go into CoreIntegration. This plan creates no extra friend assembly or production test dependency.

### 1.2 Common Task cycle

Every implementation Task below has checkboxes for the same sequence: add its named failing behavioral tests, run the focused red, implement the specified contract, run focused green plus the named related regressions, and update evidence. Do not mechanically retest unrelated features, but complete the Phase-wide Core/CoreIntegration gates.

A Task is done only when its deliverable is independently testable. Record changed files, contract changes, actual assertions/results, baseline differences, and remaining external acceptance. Test fixtures may use explicit values such as Armor Breaker 50% or Hunter +2 to expose behavior; label them fixture-only.

## 2. File map and cross-Task contracts

All paths below are repository-relative. Expand these exact prefixes:

| Prefix | Directory |
|---|---|
| C/ | Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/ |
| S/ | Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/ |
| B/ | Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/ |
| D/ | Assets/ProjectL/Script/Systems/DataBaseSystem/ |
| T/ | Assets/Tests/EditMode/ |
| TI/ | Assets/Tests/EditMode/CoreIntegration/ |

### 2.1 Stable trait identities and definition files

A1 reserves these currently unused values after rechecking TraitKey. Each listed stem X expands to the exact new files C/Contracts/Commands/Traits/XTraitSpec.cs, C/Domain/Traits/Definitions/XTraitDefinition.cs, D/Data/InGame/Table/Unit/Trait/XTraitData.cs, and D/Data/InGame/Table/Unit/Trait/XTraitAuthoring.cs. Add .meta files. Do not split or relocate unrelated existing authoring classes.

| Stem | Key | Implemented by |
|---|---:|---|
| Vanguard | 220 | B1 |
| CleanSweep | 230 | B1 |
| SoloAction | 240 | C1 |
| CallOfBlood | 250 | B2 |
| MoraleBoost | 260 | B2 |
| Disarm | 270 | B3 |
| AnkleBreaker | 280 | B3 |
| Cohesion | 290 | C1 |
| Sacrifice | 300 | C3 |
| Taunt | 310 | C4 |
| Entrenchment | 320 | C2 |
| SteadfastWill | 330 | C5 |
| GroundCompaction | 340 | D1 |
| DustScreen | 350 | D1 |
| Leap | 360 | D2 |
| Acceleration | 370 | D3 |
| StationaryFire | 380 | D3 |
| ArmorBreaker | 390 | E1 |
| FinishingShot | 400 | E2 |
| TrackingShot | 410 | E2 |
| Hunter | 420 | E3 |
| FocusedBombardment | 430 | E2 |

Registry/factory/status boilerplate follows the existing typed trait registration pattern. The mechanics Task owns C/Application/Combat/Traits/XTraitFactories.cs, the corresponding C/Domain/Traits/Rules/XTraitRule.cs and effect adapter, plus only the runtime state/status it actually needs. Reuse existing stateless state representations where supported; no artificial mutable state per trait. TraitRegistration requires real runtime-state factories, a rule, and an effect factory, so A1 does not install fake execution registrations. Register the complete executable tuple and enable its availability together in the owning mechanics Task. Until then, an authored/native/pool reference requiring execution is rejected as implementation-pending before runtime construction; Data/Spec/Definition conversion remains independently testable.

### 2.2 Shared file responsibilities

| Boundary | Modify existing | New cohesive files / responsibility |
|---|---|---|
| Identity/authoring | C/Contracts/Common/TraitKey.cs; C/Application/Combat/Traits/TraitRegistry.cs; S/CharacterSystem/TraitSpecAdapter.cs; D/Data/InGame/Table/TraitGrowth/Item/{TraitGrowthDefinitionDataSO,CategoryTraitProgressionDataSO}.cs only if validation requires it | A1 files above; C/Contracts/Commands/Traits/TraitAreaSpec.cs; C/Domain/Traits/Rules/TraitAreaRules.cs |
| Character state | C/Contracts/Combat/{CharacterCombatFacts,CharacterDebuffState,CharacterChanges}.cs; C/Contracts/Snapshots/UnitStatsSnapshot.cs; C/Application/Combat/{CombatCommandEffects,CombatEffectiveStatsCalculator,CombatTemporaryStateCleanup}.cs | C/Contracts/Combat/CharacterTraitEffects.cs; TraitAttackGrant.cs; later TraitShieldGrant.cs and typed debuff/status values |
| Owned state | C/Contracts/Roster/{OwnedCharacterRecord,OwnedCharacterChange}.cs; C/Application/Roster/{CharacterBaselineStatsCalculator,OwnedCharacterStatsCalculator}.cs; C/Application/Combat/BattleCharacterCalculator.cs; relevant roster/growth/XP record rebuilds | HunterAttackBonus is a field on the existing owned record, not a new service or store |
| Command state/turn | C/Contracts/Combat/CombatCommandState.cs; C/Application/Combat/{CombatTurnCalculator,CombatCommandComposition,CombatActionAdmission}.cs | C/Contracts/Combat/TraitTurnFacts.cs stores irreducible first-attack counts; C/Application/Combat/TraitTurnLifecycle.cs orchestrates only declared turn cleanup |
| Attack scope | C/Application/Combat/{CombatAttackCalculator,AttackDeliveryExecutor,AttackDirectDamageCalculator,FollowUpAttackExecutor,CommandAttackProjection,CommandDeathSettlement}.cs | C/Application/Combat/AttackTraitScope.cs captures root-action facts; TraitAttackNotifications.cs applies action grants; TraitKillSettlement.cs consumes attributed kills |
| Incoming damage/presence | C/Application/Combat/{CombatDamageCalculator,CombatDamageApplication,EnvironmentPresenceReconciler}.cs; C/Domain/Combat/TargetDamagePipeline.cs; existing incoming-damage capability/contribution/context types | C/Application/Combat/TraitPresenceReconciler.cs; C/Domain/Traits/Rules/ShieldGrantRules.cs; C/Application/Combat/SacrificeDamagePlanner.cs |
| Movement/environment | C/Application/Combat/{CombatMovementCalculator,MovementContextCalculator,MovementProjection,CommandEnvironmentState}.cs; C/Domain/Movement/{MovementRouteValidator,EnvironmentTerrainRules}.cs; C/Contracts/Combat/MovementEnvironmentFacts.cs | C/Contracts/Combat/TraitTrailFact.cs; C/Application/Combat/TraitTrailCalculator.cs |
| AI/knockback | C/Application/Combat/EnemyAi/{EnemyAiPolicy,EnemyMovementPathfinder,EnemyAiTurnService}.cs; C/Application/Combat/AttackKnockbackCalculator.cs; C/Domain/Combat/KnockbackImmunityPolicy.cs | TauntTraitRule and SteadfastWillTraitRule own their policies; no new AI loop or pull immunity |
| Buffer publication | B/Character/CharacterBuffer.cs; B/Combat/CombatStateBuffer.cs; B/Core/{BattleTransactions,ProgressTransactions}.cs; existing preparation/completion validators | Extend existing expected/final preparation and install; no separate trait transaction or authoritative cache |

The shared files above legitimately connect rules. They must not acquire the local formula of every trait. Adapters/registry contain mapping, shape checks, and composition; local rules own formulas and source-specific state transitions.

### 2.3 Concrete interfaces delivered across Tasks

These are proposed signatures/fields for this plan; existing signatures remain unless explicitly extended here. Namespaces follow each file's real folder. Immutable collection constructors copy their input; all state types implement the project's value equality/copy conventions.

| Delivered by | Contract for later Tasks |
|---|---|
| A1 | TraitAreaSpec(footprint, int radiusCells, bool includeCenter); bool TraitAreaRules.Contains(CellCoord origin, CellCoord target, TraitAreaSpec area). Supported footprints are square neighbors and orthogonal cross. |
| A2 | CharacterCombatFacts.TraitEffects : CharacterTraitEffects; CharacterTraitEffects.AttackGrants : IReadOnlyList<TraitAttackGrant>; TraitAttackGrant has EffectOriginKey Origin, int Amount, int RemainingControllerTurnEnds. Add optional traitEffects to With/Rebuild and preserve it through all copies. |
| A2 | UnitStatsSnapshot.TraitAttackBonus : int, included explicitly in EffectiveAttackPower's invariant. Derived solely from the authoritative received/owner/owned contributions. OwnedCharacterRecord.HunterAttackBonus : int defaults to 0; SameValues and all owned rebuilds preserve it. |
| A2/B1 | CombatCommandState.TraitTurn : TraitTurnFacts with player/enemy attack-action counts, reset at each new controller turn. No second turn sequence or budget owner. |
| B1 | AttackTraitScope captures the actor ID, root-action boundary, and later Acceleration/Focused-area facts. It is local proposed execution context; never a persistent store. void TraitAttackNotifications.Apply(CombatCommandInput input, CommandAttackProjection world, AttackTraitScope scope). Apply once per accepted action boundary. The world's proposed TraitTurn facts must flow into the final CombatCommandState; local scope lifetime is not counter persistence. |
| B2 | void TraitKillSettlement.Apply(CombatCommandInput input, CommandAttackProjection world, int eventStart). Inspect newly settled attributed deaths, deduplicate by victim/death occurrence within the command, and emit normal state changes/events. E3 extends this same consumer rather than rescanning deaths separately. |
| B3 | void TraitTurnLifecycle.EndControllerTurn(CombatCommandInput input, IDictionary<EntityId, CharacterCombatFacts> projected, Owner endedSide). Existing CombatTurnCalculator owns calling order/state/result creation. Each feature's local cleanup owns its meaning. |
| C1 | CharacterTraitEffects.Shields : IReadOnlyList<TraitShieldGrant>. A grant holds source origin, remaining budget, eligibility, and reset-boundary history. Active total is derived. CharacterChanges TraitPresenceReconciler.Calculate(CombatCommandInput input, IReadOnlyList<CharacterCombatFacts> projected) returns only changed facts. |
| C2 | Extend IncomingDamageEffectContext with damage kind, recipient/current source facts needed for presence. CaptureIncoming on CombatCommandEffects gathers eligible source bindings. Existing engine output carries distinct flat and multiplicative contributions; do not encode percentages as flat reductions. |
| C3 | SacrificeDamagePlanner.Plan consumes a batch of original attack-hit facts plus current projected characters/definitions and returns immutable recipient/redirection decisions. It performs no HP/shield writes. Root damage orchestration applies the decisions through existing common owners. |
| D1 | TraitTrailFact extends MovementTileFact with trait/source origin, creating side, configured magnitude fields, and opposing-side expiry. int TraitTrailCalculator.MovementBonusAt(CellCoord cell, MovementEnvironmentFacts environment); int TraitTrailCalculator.RangeReductionAt(CellCoord cell, MovementEnvironmentFacts environment). |
| E1 | ArmorBreakerTraitRule.ApplyAfterHit(CharacterDebuffState current, int reductionPercent) returns a new received state; EvaluateDefense(int existingEffectiveDefense, ArmorBreakState stacks) returns the post-Armor value. Existing unrelated DEF reductions/item penalties are not reapplied. |
| E3 | Hunter status carries target ID and successful rewards this stage; accumulated ATK lives only on OwnedCharacterRecord. Commands compose OwnedCharacterChange with status/projection changes in the existing transition. |

The implementation may add narrow private helpers within a Task. A new public service, Framework registration, state owner, or cross-Task replacement signature is not a private helper; review that change against the plan first.

## 3. Phase A — definitions, immutable state, and configuration safety

### A1. Register editable definitions, geometry, and availability

**Files:** create the finite definition families in §2.1, TraitAreaSpec.cs/TraitAreaRules.cs, T/TraitDefinitionContractTests.cs, T/TraitAreaRulesTests.cs, TI/TraitDefinitionAuthoringTests.cs, and the local runner. Modify identity/registry/authoring adapters and DeferredCombatFeatures.cs. Preserve existing table/factory registration.

**Interfaces:** consumes existing UnitTraitAuthoring → Data → TraitSpec → Definition and category pools; produces all §2.1 identities/validated payloads plus the A1 geometry API.

- [x] Add named tests SquareNeighbors_DefaultsExcludeCenter, InvalidOrUnsetMagnitude_RejectsWithFieldContext, Definitions_RoundTripEveryTunable, and CategoryPoolMembership_IsEditableWithoutCopyingDefinition. Assert radius 1 = 8 cells, radius 2 = 24, cross radius 1 = 4; 20 authoring percent remains 20 in the pure spec.
- [x] Run Core and CoreIntegration focused red checks, then capture untouched assembly baselines with separate run IDs.
- [x] Implement fields from design §6.2 exactly. Required TBD payloads use an explicit authoring unset sentinel and throw before conversion. Make Inspector payload types selectable through the existing mechanism; no custom editor framework.
- [x] Add the new keys and definition conversion as explicitly implementation-pending until the owning mechanics Task supplies a complete execution registration. Reject unimplemented native/pool activation with trait/asset context rather than constructing a fake no-op rule. Preserve legacy unavailability and its reasons. Use existing implemented traits in the category-membership integration fixture until the new mechanic is delivered.
- [x] Run focused green and TraitGrowthCatalogTests / TraitGrowthCatalogValidator coverage. Verify a definition can be referenced by several category SO pools and no runtime category switch overrides that membership.
- [x] Record changed identities, serializer names, exact test evidence, and the unavailable-trait list.

### A2. Publish source-tagged facts and preserve Run-owned contributions

**Files:** state/owned/Buffer paths in §2.2; C/Contracts/Combat/{CharacterTraitEffects,TraitAttackGrant,TraitTurnFacts}.cs; T/TraitStateContractTests.cs; extend T/ContractShapeGuardTests.cs and T/OwnedCharacterStatsCalculatorTests.cs; TI/TraitStatePublicationTests.cs.

**Interfaces:** consumes A1 validated definitions and existing CharacterChanges/OwnedCharacterChange; produces A2 state/projection fields in §2.3. Shield storage is added by C1, not prebuilt with fake defaults.

- [x] Write CopiesAndEquality_PreserveTraitFacts, SharedDefinition_HasIndependentReceivedState, TraitProjection_DoesNotRewriteBaseAttackOrArtifactBonus, OwnedRebuilds_PreserveHunterBonus, and StalePublication_ChangesNothing. With base ATK 10, received +3, and fixture Hunter +2, assert base remains 10 and effective ATK becomes 15 exactly once.
- [x] Run focused red, including shape guards. Exercise rejected expected/final publication with unchanged counters, results, character facts, and owned records.
- [x] Extend immutable constructors, With/Rebuild, equality, composition, BattleCharacterCalculator, and owned-stat queries as one contract change. Include CombatCommandState.TraitTurn in state reconstruction, command composition, all projection completion paths, and Buffer comparison/installation. Preserve Main's PersistentAttack invariant; a derived trait contribution is not baseline growth.
- [x] Preserve HunterAttackBonus across every XP/growth/roster record reconstruction. Initialize it to zero on acquisition/new Run. Extend OwnedCharacterChange's invariants to allow later Hunter-only increases while preserving level/XP/growth and identity.
- [x] Extend CharacterBuffer/BattleTransactions prepare-before-install validation so an owned contribution and its battle projection agree before any store is installed. Do not add an independent save/cache or a post-publication repair.
- [x] Run focused green, ContractShapeGuardTests, OwnedCharacterStatsCalculatorTests, TraitGrowthStatCoexistenceTests, TraitGrowthStatSettlementTests, and CombatCommandPublicationTests. Run both full assemblies sequentially; **stop and report Phase A**.

## 4. Phase B — common grants, kill attribution, and restrictions

### B1. Attack-action notifications for Vanguard and Clean Sweep

**Files:** C/Application/Combat/{AttackTraitScope,TraitAttackNotifications}.cs and attack-scope paths in §2.2; Vanguard/CleanSweep local rules/effect adapters; T/TraitAttackNotificationTests.cs; TI/TraitAttackGrantPublicationTests.cs.

**Interfaces:** consumes A2 received grants/turn facts and existing accepted action boundaries; produces one notification per action and source-tagged ATK grants. Successful action admission remains with CombatAttackCalculator.

- [x] Add RepeatedHits_CountOneAction, AoERecipients_CountOneAction, IndependentAdditionalAttack_HasItsOwnBoundary, FirstAttack_VanguardExcludesOwner, and CleanSweep_OnlyOtherLivingFieldAllies. Use fixture +3; assert three hit packets do not create +9 from one action.
- [x] Run focused red.
- [x] Add the root scope after admission succeeds. Increment the appropriate side's count once, evaluate Vanguard before damage, and notify Clean Sweep once. Independent extra attacks get their own notification; repeated strikes inside one behavior share its scope.
- [x] Append/expire grants by source rather than overwriting TemporaryAttackBonus or item/soul state. Hand/dead/waiting units receive none. Capture/Apply/status mappings keep their existing exception behavior on mismatched types.
- [x] Run focused green and CombatCommandEffectTests, TargetingPreviewParityTests, plus A2 publication tests. A rejected/preview action changes no committed notification count.
- [x] Record the exact scope call sites so later multi-hit work can use the boundary without reading Vanguard internals.

### B2. Per-kill Call of Blood and Morale Boost

**Files:** C/Application/Combat/TraitKillSettlement.cs; CallOfBlood/MoraleBoost local rules; CommandDeathSettlement.cs, CombatDamageApplication.cs and normal attribution/settlement callers; T/TraitKillRewardTests.cs; TI/TraitKillRewardPublicationTests.cs.

**Interfaces:** consumes existing CombatDamageCredit and pre-death facts; produces the B2 kill consumer, ordinary healing changes, and received ATK grants. Later Hunter hooks join this consumer.

- [x] Add AdditionalAndReflectionKills_AreCredited, DuplicateDeath_IsNotAwardedTwice, Morale_UsesBaseAttack, PerVictimRounding_PrecedesSum, and DeadOrHandAllies_ReceiveNoGrant. For two enemies with base ATK 13 each, 20% rounds to 3 + 3 = 6. Temporary enemy ATK +100 must not alter that result.
- [x] Run focused red in both assemblies.
- [x] Consume settled death occurrences in delivered order, preserving actual attributed killer and immutable pre-death base/max-HP values. Use the existing half-up arithmetic where suitable; healing is clamped through CharacterSettlementCalculator.
- [x] Emit Morale grants for the current living field allies, including its owner; gains add and retain separate source ownership. Do not resurrect a dead owner or change reward/soul attribution to the command actor.
- [x] Run focused green with CombatDamageApplicationTests, CombatDamageBoundaryTests, CombatCompletionPublicationTests, and B1/B2 publication tests.
- [x] Record that the kill consumer is the sole new dispatch point; it is not a second death detector or reward ledger.

### B3. Affected-side duration, individual designation, and turn cleanup

**Files:** Disarm/AnkleBreaker rules, C/Contracts/Combat/{DisarmState,AnkleBreakerState}.cs, CharacterDebuffState.cs, TraitTurnLifecycle.cs; CombatActionAdmission.cs, CombatAttackQueries.cs, CombatMovementCalculator.cs, CombatTurnCalculator.cs, CombatTemporaryStateCleanup.cs; T/TraitRestrictionTests.cs; TI/TraitTurnLifecyclePublicationTests.cs.

**Interfaces:** consumes A2 state and existing ResolvedAttackSelection.SelectedTargetId; produces shared attack/move admission restrictions and the lifecycle method in §2.3. Preserve unrelated received statuses in every constructor.

- [x] Add AppliedDuringEnemyTurn_ExpiresAtThatEnd, AppliedDuringAlliedTurn_CoversNextEnemyTurn, Reapply_RefreshesWithoutAdding, DirectionAndAll_DoNotApply, StructureOrSplashPrimary_DoesNotApply, and ForcedMovement_IgnoresVoluntaryMoveLock. Test duration 1 and an SO-only change to 2.
- [x] Run focused red. Assert remaining duration is not consumed by an attempted attack.
- [x] Apply only to a nonempty Primary-derived SelectedTargetId that is a living hostile character receiving contact. Never infer an individual target from the role named Designated; Direction/All uses that role for area hits.
- [x] Make admission, availability, range/path previews, execution, and AI use the same restriction checks. Wire cleanup to existing controller-turn ends: current affected side counts immediately; reapplication refreshes.
- [x] Wire current-turn grant expiry and first-action reset without reviving legacy per-unit turn callbacks. Preserve the existing end-of-turn environment order; apply due cleanup/reset after those end actions, before next-side input.
- [x] Run focused green, PlayerTurnMovementTests, CharacterActionCostLifecycleTests, existing pull boundary tests, and B1/B2 lifecycle regressions. Run both full assemblies; **stop and report Phase B**.

## 5. Phase C — source shields, reductions, Sacrifice, AI, and knockback

### C1. Solo Action and Cohesion source budgets

**Files:** TraitShieldGrant.cs and CharacterTraitEffects.cs; ShieldGrantRules.cs, TraitPresenceReconciler.cs; SoloAction/Cohesion rules/status mappings; common presence and damage-absorption call sites; T/TraitShieldTests.cs; TI/TraitShieldPublicationTests.cs.

**Interfaces:** consumes A1 areas, A2 state, B3 controller boundaries; produces source shields and the C1 presence API. Reconcile on placement/move/forced move/recall/death/source removal, using existing command projections.

- [x] Add SoloAction_ExitAndReenterRetainsSpentBudget, Cohesion_ExitRemovesOnlyItsSource, MultipleSources_AddButDoNotRefill, SharedDefinition_IndependentBudgets, and BothSideEnds_ResetBudgets.
- [x] Run focused red. The required example is shield 10, damage 4, exit, re-enter → 6; next reset → 10 when eligible. Preserve in-turn budget increases as well as decreases.
- [x] Store irreducible remaining/reset history once; derive current eligibility and active total from present facts. Cohesion DEF uses actual living neighbors, not a cached second occupancy map.
- [x] Absorb normal damage once at the common application boundary; retain actual HP loss separately. Use the reviewed technical tie-break of earliest expiry then stable origin. Leaving/death/expiry removes only the relevant active contribution.
- [x] Run focused green and all movement/pull/knockback presence regressions plus CombatDamageApplicationTests. Publication/preview rejection must preserve spent budgets exactly.
- [x] Record the source reset/removal contract for C2/C3 and View; keep shield budget out of authoring assets.

### C2. Entrenchment and a shared received-percentage stage

**Files:** Entrenchment rule/effect; incoming-damage context/contribution/evaluation/engine types; CombatCommandEffects.cs, CombatDamageCalculator.cs, CombatDamageApplication.cs, TargetDamagePipeline.cs; T/TraitDamageReductionTests.cs; TI/TraitDamageReductionPublicationTests.cs.

**Interfaces:** consumes C1 presence and existing damage-kind/origin facts; produces the C2 source binding/context extension. Existing flat reduction and percentage reduction remain distinct.

- [x] Add Entrenchment_ExcludesCenter, Entrenchment_ReducesAttackEnvironmentAndReflection, MultipleAuras_Multiply, and ExistingItemAndTerrainHooks_ExecuteOnce. At fixture damage 100, two 50% auras yield 25; a future 20% Dust Screen factor with one 50% aura yields 40 for an attack.
- [x] Run focused red with attack, environment, and reflection paths, not only the root direct-hit path.
- [x] Gather eligible source bindings from current field facts. Local area rules own applicability; the common engine composes contributions. Preserve existing matchup, defense, item hook, minimum-damage, badge, shield, and HP owners.
- [x] Route all numeric damage categories through the declared received-percentage stage once. Direct defeat causes stay distinct; do not turn Finishing into a large mitigatable damage number.
- [x] Run focused green with CategoryDamagePipelineTests, EnvironmentDamageTests, CombatItemDamageSourceTests, CombatDamageBoundaryTests, and C1 shield tests.
- [x] Record the finalized common damage order with exact call sites; later traits consume that contract without copying its internals.

### C3. Plan Sacrifice before defensive mutation

**Files:** SacrificeTraitRule.cs, SacrificeDamagePlanner.cs, Sacrifice status; AttackDirectDamageCalculator.cs, FollowUpAttackExecutor.cs, CombatDamageApplication.cs; T/SacrificeTraitTests.cs; TI/SacrificePublicationTests.cs.

**Interfaces:** consumes C1/C2 nonmutating defense evaluation and existing attack batches; produces one immutable redirection plan. Only common orchestration spends budgets/shields and writes HP.

- [x] Add OriginalShield_PreventsUnnecessarySacrifice, Redirect_RecalculatesSacrificerMatchupAndDefense, EnemyTurnOnly_OncePerTurn, Redirect_DoesNotRelay, Priority_DistanceThenMainThenSurvivingHeavyHp, and InstantDefeat_DoesNotTriggerOrSpend.
- [x] Run focused red, including competing Heavy/recipient pairs and an AoE batch. Assert no HP, shield, cap, or result is changed by planning alone.
- [x] Rank qualifying pairs by reviewed design D2: minimum occupied-cell Manhattan distance, recipient Main, projected sacrificing-owner HP descending, then stable IDs. Square radius 2 determines eligibility independently of the distance tie-break.
- [x] Reserve one activation per sacrificing owner/enemy turn and one redirection per original hit. Simulate the original full defenses including shields; recalculate the redirected attack against the chosen owner. Apply once; no mitigation or on-hit event is duplicated.
- [x] Keep direct instant defeat outside this planner; E2 must supply that cause without spending Sacrifice. Ordinary nonexecuting Finishing hits remain eligible normal damage.
- [x] Run focused green with C1/C2, multi-target attack, reflection-origin, and command publication regressions. Record the plan/apply ordering and source competition evidence.

### C4. Taunt through existing attack and pursuit policy

**Files:** TauntTraitRule.cs; existing EnemyAiPolicy.cs, EnemyMovementPathfinder.cs, EnemyAiTurnService.cs and attack queries; T/TauntTraitTests.cs; TI/TauntPublicationTests.cs.

**Interfaces:** consumes current source presence and legal attack/movement queries; produces a constrained target/pursuit choice, not an AI executor or an extra move.

- [x] Add AttackableTaunter_IsRequiredTarget, UnreachableTaunter_PrioritizesNextTurnAttackPosition, MultipleTaunters_UseStableNearestChoice, and DeadOrOutOfAreaSource_ReleasesConstraint.
- [x] Run focused red with a wall, depleted movement budget, and an occupied candidate destination.
- [x] Use current legal attacks first. Otherwise prefer a reachable position enabling an attack on a valid taunter next turn; preserve ordinary path validity, action-use, AP, and hazard processing. Use distance then stable IDs for equivalent taunters.
- [x] Run focused green and EnemyMovementPathTests plus existing enemy-turn/attack-targeting tests. Invalid approaches must not teleport, spend twice, or loop the automatic turn.
- [x] Record which shared targeting/pursuit contract changes and the actual required AI regression scope.

### C5. Steadfast Will at the knockback-only boundary

**Files:** SteadfastWillTraitRule.cs and registration/status mapping; AttackKnockbackCalculator.cs, KnockbackImmunityPolicy.cs; T/SteadfastWillTraitTests.cs; extend TI/PullPublicationTests.cs only with the new paired scenario.

**Interfaces:** consumes the existing separate knockback and pull decisions; contributes trait-based knockback immunity without setting general UnitBehavior forced-movement immunity.

- [x] Add Trait_BlocksKnockback and SameTarget_CowboyPullStillWorks. Keep independent behavior immunity fixtures separate.
- [x] Run focused red.
- [x] Add the trait contribution only to knockback's policy. Preserve Heavy category immunity's current disabled policy and unrelated behavior immunity.
- [x] Run focused green, KnockbackImmunityPolicyTests, KnockbackPathTests, PullBoundaryRegressionTests, and the actual Buffer pull-publication scenario.
- [x] Run both full assemblies; **stop and report Phase C**, distinguishing Core evidence from unperformed View/manual checks.

## 6. Phase D — voluntary movement and retained attack preparation

### D1. Ground Compaction and Dust Screen typed trails

**Files:** TraitTrailFact.cs, TraitTrailCalculator.cs; GroundCompaction/DustScreen rules; MovementEnvironmentFacts.cs, CommandEnvironmentState.cs, CombatStateBuffer.cs environment validation, MovementContextCalculator.cs, CombatMovementCalculator.cs, EnvironmentTerrainRules.cs, attack-range and C2 received-damage consumers; T/TraitTrailTests.cs; TI/TraitTrailPublicationTests.cs.

**Interfaces:** consumes A1 definitions/areas, B3 opposing-side expiry, C2 damage classification; produces D1 APIs in §2.3 and typed sources allocated by the existing environment progression.

- [x] Add IncludesStartAndDestination, PreservesUnderlyingTerrain, ExpiresAtNextOpposingTurnEnd, SameTrailDoesNotStack, StrongerSourceExpiry_RevealsWeaker, and Dust_OnlyReducesAttackDamage. The fixture +1 and +2 Ground overlap grants +2, then +1 after +2 expires.
- [x] Run focused red and assert preview leaves source IDs/RNG/lifetimes unchanged.
- [x] Allocate typed source IDs through the existing environment allocator and extend successor/visibility/classification validation. Preserve base terrain and per-source lifetime; no cell-keyed replacement cache.
- [x] Use the reviewed T5 arbitration: maximum active magnitude per field for the same trail kind. Different reduction kinds multiply; identical Dust sources do not. Both sides can use/receive the effects.
- [x] Capture Ground's allowance once at movement start. Make range/context/path validation/preview/execution share it. Dust range is floored at zero and its damage factor uses attack kind rather than actor ownership.
- [x] Run focused green, MovementContractTests, MovementEnvironmentTests, EnvironmentTerrainRuleTests, CellEnvironmentQueryTests, and movement publication tests. Preserve current repeated-cell rejection.
- [x] Record the source-lifetime/removal contract for future undo; do not implement rollback/refunds.

### D2. Leap traversal differs from landing/arrival

**Files:** LeapTraitRule.cs; MovementRouteValidator.cs, MovementPathNormalizer.cs, CombatMovementCalculator.cs, MovementProjection.cs and normal arrival-rule selection; T/LeapTraitTests.cs; TI/LeapPublicationTests.cs.

**Interfaces:** consumes the existing validated path and D1 terrain/trail rules; produces distinct intermediate-traversal and final-landing checks.

- [x] Add CrossesWallCliffAndUnit, CannotLandOnWallCliffOrUnit, IntermediateMine_Ignored, DestinationMine_Triggers, and Trail_SkipsWallCliffCells.
- [x] Run focused red. Keep a normal non-Leap unit in the same fixtures to prove old route behavior is preserved.
- [x] Permit the confirmed intermediate traversal while preserving board bounds, action range/cost, and final occupancy rules. Skip only intermediate mines; execute normal destination arrival.
- [x] Make range/path/context previews use the same distinction. Do not globally disable hazards or turn forced displacement into Leap movement.
- [x] Run focused green, MineEnvironmentTests, WallMovementTests, WeakGroundMovementTests, MovementRouteTests, and movement publication tests.
- [x] Record interrupted-route behavior and its actual completed path.

### D3. Acceleration and Stationary Fire share voluntary movement facts

**Files:** Acceleration/StationaryFire rules and typed statuses; MovementProjection.cs, CombatMovementCalculator.cs, AttackTraitScope.cs, CombatAttackCalculator.cs, FollowUpAttackExecutor.cs, TraitTurnLifecycle.cs, CombatTemporaryStateCleanup.cs; T/TraitMovementAttackTests.cs; TI/TraitMovementAttackPublicationTests.cs.

**Interfaces:** consumes B1 root-action scope and actual completed voluntary steps; produces held Acceleration and current-own-turn movement facts. Neither fact is reconstructed from animation or a second path store.

- [x] Add Acceleration_SurvivesTurnsAndNightUntilAttack, InterruptedMove_CountsOnlyCompletedSteps, WholeMultiHitAction_UsesCapturedBonus, ForceMovement_AddsZeroAndDoesNotBreakStationary, and StageEnd_ClearsHeldBonus.
- [x] Run focused red. With fixture +2/cell and 3 completed steps, assert held +6, same +6 contribution on every strike of the next action, then zero afterward.
- [x] Add voluntary-step gain to accepted movement transitions only. Store a movement flag where UnitActionUse.Attacked no longer retains earlier movement history; reset it at the next own-side turn.
- [x] Capture/consume Acceleration once at the root action finish; sequential strikes reuse it while observing current target facts. Default Stationary factor is 2.0; it is a damage multiplier, not a permanent ATK award.
- [x] Run focused green, B1 notifications, force-movement/pull, player-turn, preview, and publication regressions. Run both full assemblies; **stop and report Phase D**.

## 7. Phase E — per-strike Cannon rules and Hunter persistence

### E1. Armor Breaker has separate stage-owned multiplicative stacks

**Files:** ArmorBreakerTraitRule.cs, C/Contracts/Combat/ArmorBreakState.cs, CharacterDebuffState.cs, CombatEffectiveStatsCalculator.cs; direct/follow-up post-hit application and cleanup; T/ArmorBreakerTraitTests.cs; TI/ArmorBreakerPublicationTests.cs.

**Interfaces:** consumes existing effective DEF as its input and produces §2.3 post-hit state/evaluation; preserves the separate non-stacking DefenseReductionDebuffState.

- [x] Add CurrentStrike_UsesOldDefense, NextStrike_UsesNewDefense, MultiHit_AppliesEachReduction, NightPreservesStageEndClears, and ExistingDefenseDebuff_IsNotOverwritten.
- [x] Run focused red. Fixture base effective DEF 20 and 50% reduction gives first-strike DEF 20, next-strike DEF 10, and 5 after the second application.
- [x] Compose retained percentages without rounding each stack into a permanent flat penalty; clamp/round the final derived DEF consistently and apply the ordinary target pipeline once.
- [x] Flush previous strike state before evaluating the next strike. Both direct and follow-up paths apply post-hit effects through the same local rule and preserve source attribution.
- [x] Run focused green with existing defense/terrain/item damage regressions, B3 status preservation, and C3 Sacrifice simulation/application parity.
- [x] Record exact post-hit timing and stage cleanup evidence.

### E2. Finishing, Tracking, and Focused Bombardment hit decisions

**Files:** FinishingShot/TrackingShot/FocusedBombardment rules; AttackTraitScope.cs, AttackDirectDamageCalculator.cs, FollowUpAttackExecutor.cs, CombatDamageCalculator.cs, common death/attribution/preview owners; T/CannonThresholdTraitTests.cs; TI/CannonThresholdPublicationTests.cs.

**Interfaces:** consumes B1 action-start scope, C2/C3 damage/defeat distinction, E1 current target projection; produces pre-strike threshold/factor decisions. Preserve existing root AP and hit-origin groups.

- [x] Add Finishing_UsesPreStrikeHpAndStrictThreshold, LaterStrike_RechecksHp, Finishing_BypassesShieldWithoutSacrifice, Tracking_RechecksEachStrike, Focused_CountFrozenAtActionStart, and DamageFactors_Multiply.
- [x] Run focused red. ATK 100, HP 30 with normal loss 15: first strike is normal, final HP 15; later strike may execute. HP exactly 20 does not qualify. HP 15 plus shield 100 still dies; nearby Sacrifice remains unused.
- [x] Use integer/rational threshold comparisons to avoid division/float rounding. Tracking at HP exactly 50/100 qualifies, then does not if the next strike begins below 50%.
- [x] Capture Focused's living enemy-unit AoE count once per attack action, excluding structures; repeated strikes reuse that qualification. Independently scheduled additional attacks capture a new count.
- [x] Keep instant defeat in common death/attribution settlement with its real cause, without a synthetic huge damage amount, shield spend, Sacrifice spend, or duplicated item/kill event.
- [x] Run focused green across direct, multi-target, sequential, extra-attack, preview, damage-boundary, and command-publication cases. A multi-target normal damage hit can still trigger Sacrifice normally.
- [x] Record action-versus-strike boundaries using public outcomes, without a test tied only to helper call counts.

### E3. Hunter award, RNG, target lock, and Run reset in one publication

**Files:** Hunter rule/status; TraitKillSettlement.cs; owned/projection/Buffer paths from A2; CommandAttackProjection.cs, command completion and battle preparation; T/HunterTraitTests.cs; TI/HunterPersistenceTests.cs; extend existing XP/growth settlement and new-Run integration fixtures.

**Interfaces:** consumes B2 attributed kill facts, A2 HunterAttackBonus, existing environment command RNG, and B1 action finish. Produces a composed existing OwnedCharacterChange plus stage status/projection changes; no separate Hunter store.

- [x] Add HighestCurrentHp_TieUsesCommandRng, HpChange_DoesNotRetarget, OtherKiller_RetargetsWithoutRewardOrCap, ThreeRewards_StopMarking, AoE_DoesNotRetargetBetweenVictims, StagePreservesRunBonus_NewRunClearsAllTraits, and FailedOrDuplicatePublication_DoesNotAwardTwice.
- [x] Run focused red in Core/CoreIntegration. Use fixture +2 per reward: three owner target kills yield Run +6 and cap 3; an unrelated killer yields +0 and no cap increment.
- [x] Sort tied candidates by stable entity ID before using the existing captured command RNG. Preview works on a disposable copy and cannot commit the choice/RNG advancement.
- [x] Select living field targets at participation or when an initially empty enemy field gains a target. Keep the ID until death. Settle owner rewards immediately in the command; defer reselection to action finish.
- [x] Compose the owned award, stage cap, target state, projection, result, and any same-command XP/growth changes before Buffer installation. Preserve identity/level/XP/growth and update the derived trait contribution exactly once.
- [x] Verify a second stage resets target/cap while using the prior Run bonus; night/recall does not reset the cap. StartNewRun installs fresh zero-contribution records even after defeat/abandon/final victory. No artifact carryover hook may restore trait state.
- [x] Run focused green plus OwnedCharacterStatsCalculatorTests, TraitGrowthStatCoexistenceTests, TraitGrowthStatSettlementTests, CombatCommandPublicationTests, CombatCompletionPublicationTests, and preview/snapshot regressions.
- [x] Run both full assemblies; **stop and report Phase E** with separate Core and external integration statuses.

## 7A. Phase E+ — post-E corrections and trait activation facts

**Entry and boundary:** Start only after E1–E3 has finished, its tested source/evidence checkpoint has been reported, and the user authorizes E+. Re-read that checkpoint rather than diagnosing the running E from an incomplete file. This phase closes reproduced deviations from the approved A–E contracts and supplies the missing Core presentation facts before F1. It does not implement View playback, new trait mechanics, completed-move undo, production content, or unrelated baseline repairs. Current E+ implementation and verification are recorded in §11.6.

### 7A.1 Review findings and disposition — 2026-10-29

Evidence is read-only inspection of the uncommitted working tree at HEAD `e1572985064ab3cf98f9a420916e4d092e8e6f06`, the design, and this plan. The attachment inspected HEAD `efa5e7d7`; its historical inventory is not the current baseline. These are source/plan findings, not newly executed Unity failures. E+1 must recheck them against the completed E.

| ID | Observed gap and owning source | Minimum post-E action / verification |
|---|---|---|
| PR-1 | The former F1 delegated unspecified snapshot/event extensions to the handoff task. `C/Contracts/Snapshots/Traits/TraitActivationSnapshot.cs` still contains only source, one optional target, key, kind, and magnitude; there is no result origin, target-cell collection, or additional-attack aim. Current `TraitActivationKind.cs` already includes A–E additions such as AttackGranted, HealthRecovered, DamageRedirected, and ArmorBroken; the request's eight-kind inventory is stale. | E+1 defines the additive contract and complete producer matrix; E+2/E+3 implement it; E+4 verifies public consumption. F1 documents the verified result instead of hiding implementation work in handoff. |
| PR-2 | `ViewSystem/Combat/Playback/Ordering/CombatEventSubjects.cs` groups by **AttackGroup and CreditedCharacterId**, within the action segment. `CombatConcurrentGroups.cs` also needs a subject; `CombatEventTypeTable.cs` currently supplies neither origin nor subject for CombatEffectsEvent. A shield/aura/Sacrifice owner can differ from the attacking creditor. | Reuse the actual `CombatDamageOrigin` for correlation, retaining trait owner separately. Test owner != creditor and null-credit origins. View grouping/subject adaptation remains an explicit F1 handoff; a new Core field alone does not prove concurrent playback compatibility. |
| PR-3 | `C/Application/Combat/TraitKillSettlement.cs` appends Call of Blood activation **after** its health event. It and `TraitAttackNotifications.cs` emit one grant activation per recipient. `TraitPresenceReconciler.cs` and `TraitTurnLifecycle.cs` update persistent facts without a complete activation lifecycle. These paths do not yet satisfy the proposed ordering/grouping contract. | E+2 preserves the gameplay calculation while recording one logical occurrence with its recipients before its own results, plus meaningful apply/consume/expiry/removal transitions. Test capped healing, zero/no-op grants, multi-target grants, and independent repeated occurrences. |
| PR-4 | `TraitTrailCalculator.EmitAt` / `MovementProjection.AddTraitTrail` update the existing environment, and `TraitTrailCalculator.EndControllerTurn` changes lifetimes, without corresponding trail result events. `MovementProjection.Enter` currently calls EmitAt before appending Moved. Simply adding events inside EmitAt would put entered-cell effects before the step. | E+3 adds typed source creation/change/deactivation facts with explicit start-cell and entered-cell ordering, preserving allocation, terrain, actual completed path and expiry rules. Test an interrupted path and stronger-source expiry revealing a weaker source. |
| PR-5 | `ViewSystem/Combat/Playback/Handlers/CombatFactEventHandler.cs:49` rebuilds CharacterDebuffState with only Intimidation, DefenseReduction and HuntersMark; the constructor now also holds Disarm, AnkleBreaker and ArmorBreak. Its MovementSourceChanged handler only renders a StaticTerrainFact kind change. | F1 must hand off preservation of all unaffected debuffs and typed trail creation/expiry rendering. Do not edit this View file in E+. Do not claim that a generic source event will already render a trail or that additional activation events leave playback unchanged. |
| PR-6 | F1 requires readable effective DEF, but `UnitStatsSnapshot.PhysicalDefense` is the baseline field; Cohesion/item/Armor calculations live in `CombatEffectiveStatsCalculator.PhysicalDefense`. Persistent state visibility also does not prove event-time state visibility. | E+4 verifies the actual public read path and supplies any missing immutable Core-derived presentation value/change through existing contracts. Never relabel baseline DEF as effective DEF, require View to read definitions, or copy the gameplay calculation into a UI consumer. Define which context-dependent defenses belong only to the attack preview. |
| PR-7 | §11.4 refers to nonexistent E4 and F3, while the actual plan defines E1–E3 and F1–F2. No dedicated step rechecks the finished A–E implementation against confirmed rules before handoff. | The current execution order above corrects the stale labels without rewriting historical evidence. E+1 owns the frozen-source correctness review and a bounded correction ledger. Incomplete E work, missing approved content, and unrelated baseline failures are not automatically defects. |

**Review conclusion:** Keep the existing registry, rule/effect, command projection, environment allocator and expected/final Buffer boundaries. The verified gaps concern public facts, ordering and acceptance scope; they do not justify a universal trait dispatcher, mirrored aura state, a second transaction, or changes to approved gameplay. Additional gameplay defects require a reproducible case after E, not a speculative fix to active work.

### 7A.2 Proposed contract to finalize at E+1

This is the plan's technical proposal for review, not a claim that the attached Q1–Q8 answers were confirmed. Q1/Q2/Q4/Q6/Q7 favor meaningful outcomes, the existing event, grouped recipients, preserved dedicated results, and explicit additional-attack aim. Q3 reuses existing kind meanings where valid and adds stable values only for missing meanings. Q5 must include the existing creditor as well as the group. Q8 is adapted to the user's sequence: finish E first, then cover new and executable legacy traits before F1. Any departure affecting gameplay or ownership still follows the Global Constraints.

- **Occurrence:** Emit through `CombatEffectsEvent.Traits` only when a trait actually changes an applied result, decision, or persistent state. A successfully delivered extra attack, prevented knockback, selected Taunt action, or real status refresh can be meaningful without positive HP loss. Mere eligibility, an unchanged refresh, zero configured contribution, capped zero recovery, preview queries and discarded Sacrifice simulations are not committed activations. Pure Evaluate methods remain pure.
- **Payload:** Extend `TraitActivationSnapshot` additively with `IReadOnlyList<EntityId> TargetIds`, `IReadOnlyList<CellCoord> TargetCells`, optional `CombatDamageOrigin ResultOrigin`, optional `Rotation4? AttackDirection`, and optional `CellCoord? TargetCell`. The last two describe the resolved additional-attack aim; TargetCells describes affected cells, not the selected center. Preserve existing five-argument construction and existing enum numbers. Normalize old TargetId into the canonical target list; expose the legacy scalar only for a single target, and reject conflicting scalar/list inputs rather than keeping two independent target sources.
- **Copy/validation:** Copy collection inputs, reject empty/duplicate unit IDs and invalid payload combinations, preserve deterministic order and all existing validation, and update result copies/comparison/shape checks where applicable. Null ResultOrigin means no damage correlation; group 0 retains its existing ungrouped meaning. Do not mint a group merely for animation.
- **Attribution and grouping:** ResultOrigin is the same physical/credited/group fact used by the related result, not an origin reconstructed from SourceUnitId. Keep group semantics unchanged for badge, reflection and rewards. One event may combine recipients of the same occurrence; distinct strikes, kills, sources, origins or phases remain distinct occurrences. Do not mix incompatible origin keys in one grouped activation. Define each kind's Magnitude unit and zero semantics; differing per-recipient outcomes remain in the paired result facts rather than an ambiguous summed scalar.
- **Order:** Record an activation after its cause and before the first result it produces, preserving the real causal chain. Examples: root attack event → pre-hit activation → defense/HP result; HP result → Armor Breaker activation → armor-state result; attributed death → Call of Blood activation → recovery; step → trail activation → source-created result. A start-cell trail has no preceding step and is recorded at accepted movement start before the first step. Status-only activations precede their state change. Do not move a post-hit or on-kill effect ahead of the hit/death that caused it.
- **State versus event:** CharacterCombatFacts, typed debuffs/statuses, TraitEffects, the environment and OwnedCharacterRecord remain authoritative. Activation records do not become a replayable state store. Where no existing result carries the event-time state change, extend the appropriate immutable result contract so View need not inspect the final command snapshot early or recompute an aura, stack, restriction, effective stat or lifetime.
- **Existing results:** Keep CombatPullEvent, CombatHuntersMarkEvent, CombatRoarEvent and existing damage/movement/source results; an activation identifies the cause, while the dedicated event carries its result. Keep the action/cost meaning of CombatAttackEvent. Define CombatResult.Events as the ordered playback source; any AttackResult.TraitActivations summary must derive from the same occurrences with a documented scope and must not trigger a second playback.
- **Trails:** Reuse CombatMovementSourceEvent's MovementTileFact contract: creation has absent Expected and a final typed TraitTrailFact; subsequent lifetime/deactivation changes have matching source IDs and expected/final values. Validate these forms at the boundary, including the existing static-terrain form. Preserve source IDs, creating side, origin, magnitudes, underlying terrain and source lifetimes. Typed overlays are not static-terrain replacement. Do not fabricate a pre-existing source to model creation.
- **Future forms:** The payload can describe affected units/cells and ordered effects, but the request's hypothetical per-step attacks or new hit-response hooks are not new gameplay scope. Supply producers for concrete implemented mechanics only. Leave Position Swap, Recon Squad, Focus, MoveAttackDiscount, ThornField, Necromancy, Regeneration and deferred Intimidation priority behavior unavailable as before.

### E+1. Freeze E, review correctness, and pin the activation contract

**Files:** this plan and the design only for an explicitly accepted contract addendum; `C/Contracts/Snapshots/Traits/TraitActivationSnapshot.cs`, `C/Contracts/Common/TraitActivationKind.cs`, `C/Contracts/Combat/CombatEvent.cs`, existing result/copy/shape owners; create `T/TraitActivationContractTests.cs`; extend `T/ContractShapeGuardTests.cs`. Review A–E source/test paths already listed above; do not start unrelated refactors.

**Interfaces:** consumes the reported E snapshot, approved design §§2/6/7, and existing immutable result/publication contracts; produces the finalized §7A.2 contract, an exhaustive producer matrix and an evidence-backed correction ledger.

- [x] Capture the completed E source hashes, actual full-suite failure names and remaining owner boundaries. Compare A–E with the confirmed behavior: action/strike timing; original versus redirected defenses; source/reset lifetimes; movement/trail/Leap rules; Hunter attribution/RNG/owned projection; stage/new-Run cleanup. Reuse the existing named tests and inspect their assertions; add only missing behavioral reproducers.
- [x] For each finding, record approved requirement, actual path, input/expected/observed outcome, root cause, owning task, minimal fix and regression scope. Separate confirmed deviations, additional activation requirements, unverified risks and View handoffs. Apply reproduced A–E corrections in the relevant E+2/E+3/E+4 task even if a historical Phase was previously marked complete. A new balance/behavior/ownership decision returns to design review.
- [x] Build the producer matrix from the final registry/availability table: all 22 new traits and every currently executable legacy trait, including damage modifiers absent from the attachment's examples. For each key record kind/meaning, source, actual recipient(s)/cells, Magnitude unit, emission boundary, origin/aim, persistent read path and positive/no-op/removal tests. A conditional trait needs a positive fixture; lack of production assets is not proof that its activation contract is complete.
- [x] Add `LegacyConstructor_PreservesSingleTarget`, `Collections_AreDefensiveCopies`, `ConflictingTargets_AreRejected`, `OwnerAndCreditor_AreDistinct`, `TargetlessFollowUp_RetainsDirectionAndSelectedCell`, and `ExistingEnumValues_AreUnchanged`. Include null creditor, group 0, multiple targets/cells and nullable aim.
- [x] Run focused RED under §1.1, then implement only the finalized additive immutable contract. Retain valid old calls without introducing ambiguous overloads for null targets. Pin per-kind semantics and the producer matrix in this section's execution record before producer implementation.
- [x] Run focused GREEN and contract-shape/result-copy regressions. Record any consumer compatibility changes as F1 owner work; changing Core contracts is not authorization to rewrite View.

### E+2. Record applied attack, defense, status and lifecycle outcomes

**Files:** existing `C/Application/Combat/{CombatCommandEffects,CombatCommandEvents,CombatAttackCalculator,AttackDirectDamageCalculator,FollowUpAttackExecutor,KillChainAttackCalculator,TraitAttackNotifications,TraitKillSettlement,TraitPresenceReconciler,TraitTurnLifecycle,CombatTemporaryStateCleanup,AttackKnockbackCalculator,PullAttackCalculator,HuntersMarkApplication,CombatDebuffApplication}.cs`; the relevant A–E local rules/application owners and existing effect-result types only where a source decision is missing; create `T/TraitActivationOutcomeTests.cs`; extend the matching existing mechanic tests. Formula ownership remains local.

**Interfaces:** consumes E+1 payload/producer semantics and the already applied branch of existing rule decisions; produces ordered immutable occurrences and required event-time character/status results without changing gameplay calculations or action costs.

- [x] Add `CallOfBlood_ActivationPrecedesActualRecovery`, `MultiRecipientGrant_IsOneOccurrence`, `NoEffect_DoesNotActivate`, `RepeatedStrike_RemainsDistinctFromActionGrant`, `DefensiveOwner_UsesIncomingResultOrigin`, and `PureEvaluationAndSacrificePlanning_DoNotPublish`. Cover zero/capped contributions, fully absorbed damage, blocked delivery, independent extra attacks, reflection, and two same-key sources.
- [x] Run focused RED for PR-2/PR-3 and every reproduced correctness defect assigned here. Use native expected outcomes, not helper-call counts or a fabricated success event.
- [x] Record metadata from the same decision used by the applied branch. If pure evaluation currently returns only a scalar, carry the necessary immutable contribution evidence through its existing result; emit only at its application boundary. Do not rerun whole attacks with a trait disabled, infer the cause from final totals, or emit from every evaluator/simulation call.
- [x] Complete producer coverage for the new grants/healing/restrictions/shields/auras/Sacrifice/Taunt/Steadfast/Acceleration/Stationary/Cannon/Hunter outcomes and active legacy modifiers, follow-ups, knockback/Impact Bolt, Pull, HuntersMark, Intimidation and Roar. Preserve unavailable traits and actual legacy eligibility. Add aim and origin to existing follow-up/KillChain occurrences instead of emitting duplicates.
- [x] Correct PR-3's recovery ordering and group only recipients from the same logical grant. Record real application/refresh/consumption/removal/expiry/reset boundaries with retained source attribution, including source death, recall, both side ends and stage cleanup. Do not replay an unchanged passive aura or a query as a new activation. Keep Hunter distinct from HuntersMark.
- [x] Add `DedicatedResult_HasOnePrecedingActivation`, `StatusChange_PreservesUnrelatedFields`, `SourceRemovalAndReset_HaveDistinctOutcomes`, and `AttackSummary_DoesNotDuplicateOrderedOccurrences`. Verify the exact event subsequences and their state values, including post-hit Armor and post-death rewards.
- [x] Run focused GREEN plus the affected B–E/native damage, shield, restriction, pull/knockback, reflection and lifecycle suites. Record any corrected gameplay deviation separately from added observability; defer unrelated findings.

### E+3. Publish movement trail creation and expiry at their real boundaries

**Files:** `C/Application/Combat/{TraitTrailCalculator,MovementProjection,CombatMovementCalculator,CombatTurnCalculator,CombatCommandCalculator,CombatTemporaryStateCleanup,CommandEnvironmentState}.cs`, the existing completion/publication owner where required, and the E+1 movement-source contract; extend `T/TraitTrailTests.cs` and `TI/TraitTrailPublicationTests.cs`; create `T/TraitActivationMovementTests.cs`.

**Interfaces:** consumes the existing accepted voluntary path, trail source allocator/lifecycle and E+1 event payload; produces ordered activation/source changes for creation, lifetime change and deactivation, with no new trail storage or movement executor.

- [x] Add `StartCellTrail_PrecedesFirstStep`, `EnteredCellTrail_FollowsCompletedStep`, `InterruptedRoute_HasNoUnenteredTrailEvents`, `OpposingEnd_EmitsOnlyDueSourceChanges`, and `StrongerExpiry_PreservesWeakerSource`. Assert source IDs and expected/final typed values as well as final environment state.
- [x] Run focused RED. Include rejected/preview routes, wall/cliff transit, forced displacement, start/destination cells, both creating sides and stage-end cleanup.
- [x] Preserve D's allocation and gameplay order while separating event placement from mutation where necessary: record the accepted Moved fact before that cell's trail activation/result; retain the start-cell exception. Never move a gameplay effect across hazard/arrival resolution merely to arrange presentation.
- [x] Publish creation and later changes through the existing movement-source event contract. Expiry uses the existing opposing-side end and end-of-turn environment order; distinguish countdown from active removal. Preserve separate overlapping sources so a consumer can remove the expired source and show the still-active one.
- [x] Complete the non-trail movement producers from the same accepted decisions: Leap records actual exceptional traversal/arrival behavior, not an ordinary route where it changed nothing; Acceleration and Stationary Fire record their real move-success state changes once. Add `Leap_OrdinaryRouteDoesNotActivate`, `Leap_ExceptionalTraversalHasOrderedFact`, and `CompletedPath_StateActivationIsNotRepeatedPerStep`. Preserve existing source/target visibility in preview payloads and do not reveal hidden hazards merely to describe an activation.
- [x] Verify state-result agreement for normal completion and all existing battle-end routes. An EndBattle disposal with no playable command result is documented as disposal, not a fabricated activation command. Keep destruction/reset ownership with existing completion/Buffer owners.
- [x] Run focused GREEN, D1–D3 movement/environment/Leap tests and actual trail publication checks. No new path-attack feature, undo, refund, terrain rewrite or View renderer is included.

### E+4. Verify atomic publication and public presentation readiness

**Files:** existing expected/final publication, snapshot/query and completion owners in §2.2; `TI/TraitPresentationContractTests.cs` (moved here from former F1); create `TI/TraitActivationPublicationTests.cs`; extend existing stale/duplicate/new-Run integration fixtures. Any reproduced A–E defect is fixed at its real owner, with the narrowest justified regression.

**Interfaces:** consumes the final E+ producer matrix, ordered events and authoritative facts; produces verified public contracts/evidence for F1. This is not View execution or manual acceptance.

- [x] Add `AcceptedResult_ContainsOrderedActivationsAndFinalFacts`, `StalePublication_PublishesNeitherFactsNorActivations`, `DuplicateCommand_DoesNotRepeatOccurrence`, `Preview_DoesNotCommitEventsRngOrCounters`, and `ResultRead_DoesNotReapplyEffects`. Assert character/owned/environment/result state and source/RNG progress together through actual Buffer submission; do not test only a detached calculator.
- [x] Run focused RED where the public boundary lacks the required contract. Verify effective ATK/DEF, shield sources/budgets, restrictions, Hunter target/cap/Run bonus and trail geometry/lifetime through public Buffer results/queries without invoking Core calculations or reading InGame definitions as the consumer.
- [x] Supply only demonstrated missing immutable projections or event-time state changes through existing publication. Keep baseline stats and context-specific attack-preview defenses distinct; compute derived presentation values at the Core owner from the same authoritative inputs. Extend all relevant copy/equality/expected-final checks together; add no post-publication repair or parallel store.
- [x] Complete the E+1 correction ledger: each confirmed issue has a failing reproducer, minimal owner fix and passing evidence; each unverified/out-of-scope issue has its owner and acceptance limit. Recheck finished A–E behavior after integration, including repeated-hit shields/Sacrifice/Finishing, Hunter with XP/growth, and night/stage/new-Run boundaries. Missing E work is assessed only from the final E checkpoint.
- [x] Run focused GREEN with contract-shape, all affected activation/movement/damage and native publication regressions. Then run both full assemblies sequentially using fresh names `Eplus-core` and `Eplus-integration` under §1.1. Compare exact failed-fullname sets with completed E; investigate any changed failure, including a baseline case whose path is affected. Preserve the editor's approved AppData route and avoid the running E editor.
- [x] Verify Unity compilation of existing consumers after the additive contract changes. Core tests do not establish View grouping/rendering correctness; record those pending owner checks explicitly. Source-review SSOT, all five SOLID principles, FW registration/state/lifecycle boundaries, and the concrete locality scenarios in §9; retain required integration tests even when a local helper is small.
- [x] **Stop and report Phase E+.** F1 is ready only when the producer/correction matrices, ordered examples, public contract evidence and all required Core checks are complete with explained baseline differences. Report Core implemented, handoff pending, View/Flow pending and production/manual acceptance pending separately.

## 8. Phase F — Core handoff, production content, and joint acceptance

### F1. Publish a concrete Core-to-View contract and integration evidence

**Files:** create docs/superpowers/handoffs/2026-10-28-trait-system-core-view-handoff.md; update this plan's execution record. This is a documentation checkpoint. Runtime contract extensions and TraitPresentationContractTests now belong to E+, not F1.

**Interfaces:** consumes completed A–E/E+ plus the later remediation A–D/X verified contracts; produces the original planned Core-to-View handoff with ownership, meanings, ordering, lifetime, availability and remaining acceptance work. X's R8–R10 ownership exceptions supersede older blanket Events-only language; historical execution records remain unchanged.

- [x] Trace E+4 through the subsequent remediation A–D/X source manifests, confirm the latest tested source matches the handed-off contract, and link exact results/baseline differences. Reopen the owning E+ item if a required public fact is missing; do not silently implement a new contract during F1 or claim correctness from old counts.
- [x] Document exact field/constructor/kind names, units, null/group-0 semantics, source versus creditor, target lists/aim, producer locations and source lifetimes. Include ordered examples for multi-hit and targetless extra attacks, Sacrifice/Finishing, shield reset/removal, post-kill rewards, Hunter transitions, start/step trails and opposing-turn expiry. Define the single playback source and AttackResult summary compatibility.
- [x] Hand off View routing by (TraitKey, TraitActivationKind), same-origin concurrency/subject handling in CombatEventTypeTable/CombatConcurrentGroups, and preservation of causality barriers. Reuse existing result consumption/acknowledgment so reading a summary and an event cannot play one activation twice. Activation rendering and its dedicated result rendering have distinct responsibilities.
- [x] Hand off event-time state application and the PR-5 correction in CombatFactEventHandler: preserve Disarm, AnkleBreaker, ArmorBreak and all unrelated debuffs when applying HuntersMark. Include typed trail creation/change/deactivation handling instead of assuming the existing StaticTerrainFact renderer covers it. View regression tests and manual playback belong to the View owner.
- [x] Leave trait appearance SOs, icons/text/status HUD, Hunter markers, aura/trail visuals, VFX/audio/animation, scene/prefab links and Flow integration with their owners. View consumes public facts through existing presentation/UI registration contracts; it neither reads InGame rules nor creates a trait Buffer/polling loop. The future path-attack example and deferred traits remain outside delivery scope.
- [x] Record handoff as prepared and communication as not delivered until actual authorized delivery occurs. Track Core evidence, View integration, Flow integration and joint acceptance independently; no external message is authorized by preparing this document.
- [x] **Stop and report the Core handoff checkpoint.** Core completion does not imply working trait visuals, preserved concurrency, or production acceptance.

### F2. Validate approved SO content and record external acceptance

**Files:** only explicitly approved trait definition/category pool assets under Assets/ProjectL/DataBaseSO/InGameGroup/; TI/TraitProductionContentTests.cs; update this plan's execution log and the handoff. External owners edit their own View/Flow files.

**Interfaces:** consumes the designer-approved numeric/content list; produces valid active pools with only implemented/configured candidates and joint acceptance evidence.

- [ ] List all still-TBD numbers, Main content, and enhanced variants before changing assets. Do not invent values, substitute fixture defaults, or treat empty enhanced/Main content as a missing runtime implementation.
- [ ] Add asset cross-validation cases proving all active pool references are implemented, configured, unique/compatible, and use the intended SO category membership.
- [ ] Run focused red for any known invalid approved activation, then apply only the approved SO values/references. Keep Position Swap/Recon and existing deferred traits unavailable.
- [ ] Run focused green and both full assemblies. Check actual Inspector serialization/definition selection and pure Data conversion. Record Core/asset evidence separately.
- [ ] Obtain external delivery evidence for F1's View corrections, icons, Hunter marker, aura/trail visuals, status UI, preview parity, multi-hit playback, scene transitions, and a real new-Run reset. Jointly verify activation/result order, same-origin simultaneous hits, targetless follow-up aim, exactly-once playback, status preservation, overlapping trail expiry and representative attack/kill, movement, enemy-turn, stage-change and new-Run scenarios.
- [ ] Record statuses independently: Core implemented; content valid; handoff prepared; external integration delivered; joint acceptance; production activated. An unfinished external check remains pending.
- [ ] **Stop and report Phase F.** Do not mark production accepted from Core tests alone.

## 9. Locality assessment required at Phase review

The following are planned effects, not measured implementation results. The executing owner records actual files/call paths and evidence at each checkpoint.

| Real change | Planning-time implementation burden / cause | Minimum boundary and expected later change | Required understanding / verification |
|---|---|---|---|
| Implement or tune Morale's percentage | The original implementation required authoring, pure formula, kill attribution, grants, projection, and publication. These are legitimate connections; delivered B2 evidence is in §11.2. | B2 keeps its formula in MoraleBoostTraitRule and uses one attributed-kill consumer. After implementation, ordinary tuning changes only its SO payload. | Enemy base ATK, per-victim rounding, recipient/expiry contract. Formula changes need kill/publication checks; tuning does not require Leap, Taunt, or Hunter internals. |
| Change Disarm from 1 to 2 turns | Admission/AI/preview and controller-turn lifecycle must agree; separate copied counters would create unnecessary coupling. | B3 stores one typed affected-side duration and uses shared admission/lifecycle. Numerical tuning changes its SO field. | Individual-target mapping, refresh, affected-side ends. Keep AoE, AI, preview/execution, and forced-movement regressions because they share the actual restriction. |
| Preserve Hunter ATK across stages | Owned-record reconstruction, battle projection, command merge, and Buffer validation currently share level/XP/growth facts. Adding an owned field necessarily touches those contracts. | A2 makes preservation an explicit value contract; E3 composes awards in the existing transaction. Other feature authors preserve the field without understanding Hunter selection. | CharacterInstanceId, equality/rebuild, expected/final publication, stage/Run boundary. Recheck XP/growth/new-Run integration; no separate save/cache or delayed repair. |
| Adjust a shield source or remove an aura | Multiple sources and range changes share protection; a single flat shield scalar would hide who owns the remainder. | C1 holds per-source remaining budgets and derives active total; source removal acts on that origin only. | Grant/reset/removal/absorption contract. Forced-movement presence and common damage tests remain necessary; trail/kill formulas do not. |
| Add a trait dependency/registration | Definition types, adapters, snapshot codecs, and registry must stay consistent. This is normal assembly wiring. | Keep registration explicit; do not eliminate it with global lookup, hidden event subscriptions, auto-registration, or an extra Bootstrap per trait. | New trait's contract and relevant mapping only. Run catalog/shape tests and its real integration paths. |
| Change one trait's activation moment or add a result field | Current producers emit different subsets and View would need to infer missing causes/order from mechanic internals. | E+ keeps the decision/formula at its owner and exposes a common immutable occurrence/result contract; a local trigger change touches that producer and its tests, while a payload change legitimately touches shared copies and consumers. | Understand the trait's cause/apply boundary, kind semantics and origin contract. Verify affected ordering/publication plus required full gates; no need to understand unrelated trait formulas or View VFX implementation. |
| Add trail lifecycle presentation | Final environment facts preserve gameplay but do not identify the event-time creation/removal moment; the existing View handler covers static terrain only. | E+3 publishes typed source transitions from the existing allocator/lifecycle. F1 hands off their rendering and grouping without duplicating terrain or lifetime state. | Understand accepted steps, source identity and opposing-side expiry. Movement/hazard/overlap/publication regressions are necessary; animation asset internals are not Core prerequisites. |

For E+, explicitly check all five SOLID principles at these concrete boundaries: **SRP**, local rule decisions versus shared publication/order; **OCP**, extend existing effect/result capabilities without a central switch containing every formula; **LSP**, preserve valid legacy snapshot/result construction and gameplay contracts; **ISP**, pass only required outcome/context facts to each producer; **DIP**, pure rules depend on contracts, not DB/View/Buffer implementations. Preserve SSOT for state and existing FW-001/002/003/004/016 scope; no new service or lifecycle is needed for an activation value. These are expected improvements to verify, not measured claims or a mandate to add interfaces.

Do not report success using file/class count reductions. If implementation still requires knowledge of unrelated formulas, record the concrete hidden state/order dependency and fix only the relevant boundary within the authorized Task.

## 10. Coverage, policy review, and completion checklist

| Design coverage | Owning Tasks |
|---|---|
| All 24 definition/coverage entries | §2.1 lists 22 active identities; Position Swap and Recon remain explicitly deferred |
| Editable magnitudes/category membership and required-value rejection | A1, F2 |
| SSOT, copy/equality, atomic state and zero new-Run carryover | A2, E3, E+1/E+4 |
| Vanguard, Clean Sweep | B1 |
| Call of Blood, Morale Boost | B2 |
| Disarm, Ankle Breaker | B3 |
| Solo Action, Cohesion | C1 |
| Entrenchment | C2 |
| Sacrifice | C3, E2 |
| Taunt | C4 |
| Steadfast Will | C5 |
| Ground Compaction, Dust Screen | D1 |
| Leap | D2 |
| Acceleration, Stationary Fire | D3 |
| Armor Breaker | E1 |
| Finishing Shot, Tracking Shot, Focused Bombardment | E2 |
| Hunter | E3 |
| Post-E approved-behavior review and reproduced corrections | E+1 ledger; fixes/verification at E+2–E+4 |
| Activation request R1/R2/R5/R6: meaningful occurrences, order, lifecycle and dedicated results | E+1/E+2; movement at E+3; atomic publication at E+4 |
| Activation request R3/R4/R7: targets/cells, result origin, additional-attack aim | E+1/E+2, E+4; View grouping remains F1 handoff |
| Public presentation contracts / owner handoff / production evidence | E+4 / F1 / F2 |

Technical policies carried forward for this plan's review: D2 Sacrifice pair ranking with Manhattan distance/stable residual ties; T1 shield absorption by earliest expiry/stable origin; T3 cleanup after existing end-of-turn environment actions; T5 strongest same-trail magnitude per field. They remain explicit design proposals, not newly invented user confirmations. Change them by updating the design/plan before affected implementation. Main content, enhanced variants, and numerical TBD values remain content gates.

For every Phase report:

- [ ] Authorized scope, actual file changes, and contract changes are listed.
- [ ] New behavior has meaningful focused red/green evidence and the Phase Core/CoreIntegration gates.
- [ ] Baseline failures and unperformed View/manual checks are identified without stale test-count claims.
- [ ] Source ownership, exception/failure behavior, and lifecycle order match the design.
- [ ] The relevant locality scenario records expected versus actual edit/understanding/verification scope.
- [ ] Runtime state never mutated SO assets or acquired an independent mirrored owner.
- [ ] Deferred traits, unrelated workers' areas, and manual commit/push boundaries are preserved.
- [ ] From E+ onward, each scoped trait has producer/no-op/lifecycle coverage, each correction has its reproducer and evidence, and pending View work is an explicit handoff rather than a Core completion claim.
- [ ] Update the execution log and stop for the user's Phase review.

## 11. Execution log

| Date | Phase / Task | Authorization | Implementation / evidence | Remaining |
|---|---|---|---|---|
| 2026-10-28 | Planning | User requested an implementation document | Plan only; source/contract and documentation checks. No code, asset, or test implementation; no Unity run. | Superseded by explicit Phase A authorization below |
| 2026-10-29 | Phase A / A1–A2 | User requested “Phase A 진행해” | Implemented definitions, immutable state, owned contribution preservation and atomic projection validation; focused red/green and both full gates; one review finding fixed. See §11.1. | Stop for Phase A review; B–F and external/manual acceptance pending; no commit/push |
| 2026-10-29 | Phase B / B1–B3 | User subsequently requested “Phase B 진행해” | Six trait families execute through attack notifications, attributed kills, restrictions and controller-turn cleanup. Full gates preserve the same nine baseline failures; the final reflection finding is fixed and verified. See §11.2. | C–F, production content and external/manual acceptance pending; no commit/push |
| 2026-10-29 | Phase C / C1–C5 | User requested “Phase B 끝나면 Phase C 진행해.” | Shield histories/presence, Entrenchment, planned Sacrifice, Taunt targeting/pursuit and knockback-only Steadfast implemented. Full gates preserve exactly the same nine baseline failures; four review findings fixed and reverified. See §11.3. | C complete; D–F, production content and external/manual acceptance pending; no commit/push |
| 2026-10-29 | Post-E plan review / E+ scheduling | User requested plan review and an activation-fact supplement after the running E and before F1, leaving View work as handoff | Documentation only. Read the supplied draft, current plan/design and owning Core/View/FW contracts; recorded PR-1–PR-7 and E+1–E+4, narrowed F1 to handoff, and corrected current task references. No code/asset/test changes or Unity execution by this review. | Finish/report E first; E+ execution awaits separate authorization. View corrections/integration remain F1 handoff and F2 external acceptance; no commit/push |

### 11.1 Phase A implementation checkpoint — 2026-10-28–29

**Authorization and stop:** The user authorized Phase A only. A1/A2 are implemented; stop here for review. No commit or push was performed. The existing feature/Cheon/Core/NewCombatSytem checkout was used at base e15729850; the untracked design and plan were preserved.

**A1 delivered:** All 22 identities in §2.1 use the reserved values 220–430. Each has the four typed files specified there (88 new files plus their .meta files). Numeric constraints are owned by each pure Spec; Data validation and Definition construction reuse those constraints. Percentages remain integer percentages (20 stays 20). The eight required TBD fields start at explicit -1 sentinels, without Inspector Min/Range attributes that would silently clamp them to zero. Invalid conversion reports the definition asset, trait and field. GetData returns fresh immutable payloads.

The serialized managed-reference names are ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait.<Stem>TraitAuthoring in ProjectL.Systems, where each exact Stem is listed in §2.1. Existing serialized types and GUIDs were not moved or renamed. The small UnitTraitAuthoringDrawer uses Unity's existing SerializeReference/TypeCache mechanisms for selection and numeric editing; it does not register runtime effects. Category pool membership remains solely in CategoryTraitProgressionDataSO.pool; the editable-membership fixture uses a shared implemented Knockback definition.

**Availability:** Every new §2.1 key returns ImplementationPending and has no execution registration. Valid definition conversion can be tested independently; native Unit SO, active category-pool, pure catalog-validation and runtime registry/catalog ingress reject implementation-pending references before runtime construction. Existing Focus, MoveAttackDiscount, ThornField, Necromancy and Regeneration retain DeferredUnitTurnTiming. HuntersMark remains available at key 190 and differs from Hunter at 420. Position Swap/Recon were not added or enabled. No production pool, Main candidate, enhanced value or trait asset was activated.

**A2 delivered:** CharacterCombatFacts owns immutable CharacterTraitEffects.AttackGrants. Each grant preserves EffectOriginKey, amount and remaining controller-turn ends. Collection construction copies inputs; equality includes source and lifetime. With/WithPlacement/Rebuild and command composition preserve facts. CombatCommandState.TraitTurn stores only player/enemy attack-action counts; existing controller-turn start resets the starting side while preserving the other side. No parallel turn sequence or budget was added. Shield storage and executable notification/expiry mechanics remain with their later Tasks.

UnitStatsSnapshot.TraitAttackBonus is a derived contribution. EffectiveAttackPower equals base + Artifact + position + trait contribution. CombatEffectiveStatsCalculator projects received grants plus the owned Hunter contribution; the same calculation validates the prepared Buffer records. Base ATK, ArtifactAttackBonus, Main PersistentAttack and XP/stat-growth ownership remain distinct. Initial battle/owned queries, ordinary attack damage, soul collection, position changes, Main settlement and cleanup preserve this distinction. Cleanup removes received grants while retaining the owned contribution.

OwnedCharacterRecord.HunterAttackBonus is nonnegative, defaults to zero, participates in equality, and survives XP/growth rebuilding. OwnedCharacterChange permits nondecreasing Hunter awards with identity/growth invariants; TraitGrowthChange cannot alter Hunter. CharacterBuffer compares final owned records and all final battle projections before installation through the existing BattleTransactions preparation. No extra transaction, store, cache, repair pass, Framework service or Job was introduced. StartNewRun replaces the roster with fresh zero-contribution records through the existing ProgressTransactions path.

**Review fix:** One independent read-only review found that arithmetic agreement alone allowed a reused fresh-battle payload with received grants. A focused test failed because preparation did not throw. The single fix pass extended RequireFreshBattleProgress to reject nonempty received effects while allowing a valid nonzero owned Hunter projection. The positive preparation, rejection and unchanged-store assertions are covered together. No further actionable findings or deferred minor findings remained in that review. B–F mechanics and Inspector/View/Flow/PlayMode/manual acceptance were explicitly outside this checkpoint.

#### Verification evidence

Evidence is retained locally under .superpowers/trait-system/. Unity 6000.0.73f1 ran sequentially through the approved AppData-access route, hidden, with the selected exact assembly and fresh XML/log/exit filenames. No Unity upgrade or cache deletion was needed. XML case counts, failure names and zero skips were checked; exit code alone was not used as acceptance.

| Run | Assembly / purpose | Actual result |
|---|---|---|
| baseline-core | Core before product/test changes | 3773/3777; 4 existing failures; 0 skipped |
| baseline-integration | CoreIntegration before product/test changes | 228/233; 5 existing failures; 0 skipped |
| A1-red | Identity/geometry/availability | 26 expected failures / 27 cases |
| A1-authoring-red | Typed authoring/required values/activation | 31 expected failures / 32 cases |
| A1-green | New contracts and existing catalog regression | 44/44 passed |
| A1-authoring-green | New SO conversion and existing authoring regression | 42/42 passed |
| A2-red | State contracts and shape guards | 6 expected failures / 12 cases |
| A2-publication-red-2 | Atomic state/owned publication | 4 expected failures / 4 cases |
| A2-green-2 | Definition conversion, state/shape, owned queries and growth coexistence | 100/100 passed |
| A2-publication-green-2 | State publication, XP/growth settlement and command publication | 18/18 passed |
| A-review-freshness-red | Fresh-stage received-state guard | 1 expected failure / 1 case |
| A-review-green | State publication including the freshness regression | 6/6 passed |
| A-core-verified | Final full Core gate after review fix | 3832/3836; same 4 baseline failures; 0 skipped |
| A-integration-verified | Final full CoreIntegration gate after review fix | 266/271; same 5 baseline failures; 0 skipped |

The first full Core gate exposed two old registry tests that assumed every enum key had execution registration. IntimidationTraitTests and RegenerationTraitTests now require registration for every non-ImplementationPending key, preserving coverage of legacy deferred registrations. Separate assertions cover all 22 pending keys and reject runtime registration. No no-op rule was added to satisfy an obsolete expectation.

Final failed-test-name sets were compared directly against both untouched baseline XML files: no additional or removed failures. All 97 added cases passed. Existing failures are:

- ProjectL.Tests.Core.ExperienceAttributionPathTests.DeferredGroundAtPlayerEnd_DoesNotCreateKillsOrExperience.
- ProjectL.Tests.Core.WeakGroundTurnTests.ControllerCompletion_DoesNotCollapseOrCountGround(Player,0).
- ProjectL.Tests.Core.WeakGroundTurnTests.ControllerCompletion_DoesNotCollapseOrCountGround(Enemy,1).
- ProjectL.Tests.Core.WeakGroundTurnTests.ControllerCompletion_DoesNotCollapseOrCountGround(Enemy,8).
- ProjectL.Tests.CoreIntegration.BossStagePreparationTests.ProductionBossStage_PreparesAndPlacesBoss (StageKey 2101 missing).
- ProjectL.Tests.CoreIntegration.PullAndMarkPresentationTests.Pull_AttackMotionRunsOnlyForDirectHit(False).
- ProjectL.Tests.CoreIntegration.PullAndMarkPresentationTests.Pull_AttackMotionRunsOnlyForDirectHit(True).
- ProjectL.Tests.CoreIntegration.PullAndMarkPresentationTests.Pull_PreviewAndAttackTargetUseArrival(False).
- ProjectL.Tests.CoreIntegration.PullAndMarkPresentationTests.Pull_PreviewAndAttackTargetUseArrival(True).

The Pull failures are pre-existing NullReferenceExceptions. These nine cases were not repaired in this Phase. No Framework-wide, PlayMode, scene, prefab, manual Inspector interaction or real-gameplay acceptance was performed. This checkpoint does not claim B–F mechanics, content, presentation or production acceptance.

#### Actual change locality and ownership

| Concrete change | Actual edit / understanding scope | Verification and result |
|---|---|---|
| Add or tune one trait's definition | Its typed authoring/Data/Spec/Definition plus explicit adapter/definition mapping and availability. Numeric validation is reused rather than copied into shared execution owners. Category membership remains one pool reference. | Every tunable is round-tripped through authoring, pure Data, Spec and Definition. Invalid values/required sentinels and editable membership were checked. No unrelated trait formula was needed. |
| Preserve Hunter ATK across XP/growth and stages | Owned record/change, both XP and trait-choice rebuilds, baseline/battle projection and final-owned Buffer agreement. This is a necessary shared lifetime/publication contract, not an extra Hunter store. | Base 10 + received 3 + fixture Hunter 2 gives effective 15 once; Main baseline is unchanged. XP/trait selection, stage end, new-Run replacement, stale/duplicate publication and mismatched projections were verified. Other Hunter targeting/kill/RNG rules remain outside A2. |
| Add received state or turn facts | Existing immutable copy/equality/With, movement/controller state reconstruction, command composition and Buffer comparisons/install paths. Shared owners carry facts without learning each trait's local formula. | Defensive copies, source/lifetime equality, starting-side reset, state installation, stale expectations, fresh-stage rejection and unchanged stores were checked. Shared Core/CoreIntegration regressions remain necessary. |

No FieldCamera, Architecture, View/Flow/Job, scene/prefab or legacy Missing Script asset was changed. Unity-generated FMOD catalog/audio .meta changes were restored after testing. Runtime state did not mutate trait SO assets.

#### Source manifest

The 88 new definition-family files are exactly the four expansions per Stem in §2.1. Additional new source files (each with a new .meta) are:

- D/Editor/UnitTraitAuthoringDrawer.cs
- C/Contracts/Combat/CharacterTraitEffects.cs
- C/Contracts/Combat/TraitAttackGrant.cs
- C/Contracts/Combat/TraitTurnFacts.cs
- C/Contracts/Commands/Traits/TraitAreaSpec.cs
- C/Domain/Traits/Rules/TraitAreaRules.cs
- TI/TraitDefinitionAuthoringTests.cs
- TI/TraitStatePublicationTests.cs
- T/TraitDefinitionContractTests.cs
- T/TraitStateContractTests.cs

Existing source files changed (34, using the §2 prefixes):

- D/Data/InGame/Table/TraitGrowth/Item/CategoryTraitProgressionDataSO.cs
- D/Data/InGame/Table/TraitGrowth/Item/TraitGrowthDefinitionDataSO.cs
- D/Data/InGame/Table/Unit/Item/UnitDataSO.cs
- B/Character/CharacterBuffer.cs
- B/Combat/CombatStateBuffer.cs
- S/CharacterSystem/TraitSpecAdapter.cs
- C/Application/Artifact/ArtifactAttackPowerEvaluator.cs
- C/Application/Artifact/SoulEffectCalculator.cs
- C/Application/BootstrapModels/UnitCatalog.cs
- C/Application/Combat/BattleCharacterCalculator.cs
- C/Application/Combat/CharacterSettlementCalculator.cs
- C/Application/Combat/CombatEffectiveStatsCalculator.cs
- C/Application/Combat/CombatTemporaryStateCleanup.cs
- C/Application/Combat/CombatTurnCalculator.cs
- C/Application/Combat/DeferredCombatFeatures.cs
- C/Application/Combat/MovementContextCalculator.cs
- C/Application/Combat/MovementProjection.cs
- C/Application/Combat/PositionTileModifierCalculator.cs
- C/Application/Combat/Traits/TraitRegistry.cs
- C/Application/Roster/CharacterBaselineStats.cs
- C/Application/Roster/CharacterBaselineStatsCalculator.cs
- C/Application/Roster/ExperienceRules.cs
- C/Application/Roster/TraitGrowthCatalogValidator.cs
- C/Application/Roster/TraitGrowthRules.cs
- C/Contracts/Combat/CharacterCombatFacts.cs
- C/Contracts/Combat/CombatCommandState.cs
- C/Contracts/Common/TraitKey.cs
- C/Contracts/Enhancements/TraitGrowthChange.cs
- C/Contracts/Roster/OwnedCharacterChange.cs
- C/Contracts/Roster/OwnedCharacterRecord.cs
- C/Contracts/Snapshots/UnitStatsSnapshot.cs
- T/ContractShapeGuardTests.cs
- T/IntimidationTraitTests.cs
- T/RegenerationTraitTests.cs

**Remaining:** User Phase A review; then separately authorized Phase B. Required TBD balance values, enhanced/Main content, executable B–E mechanics and Phase F handoff/activation/acceptance remain pending. The work is left uncommitted for the user.

### 11.2 Phase B implementation checkpoint — 2026-10-29

**Authorization:** The user subsequently authorized B1–B3. The existing checkout and uncommitted Phase A work were preserved. The Phase B starting source snapshot is `.superpowers/trait-system/phase-b-base/manifest.json`; the exact review delta and hashes are `.superpowers/trait-system/phase-b.diff` and `phase-b-source-manifest.json`. HEAD remains e15729850. No commit, push, worktree change or C–F implementation was performed.

**B1 delivered:** Vanguard and Clean Sweep use real typed registrations/status mappings and local rules. `CombatAttackCalculator.Execute` notifies once after successful admission, before common damage. Valid `FollowUpAttackExecutor.ExecuteTarget` and SameDirection deliveries each notify their independent action once. `KillChainAttackCalculator` repeats strikes within its parent scope and does not notify again. AoE targets do not multiply the count. Living field recipients receive separate source-tagged grants; Vanguard excludes its owner and requires that owner to perform the first allied attack, while Clean Sweep excludes its own attack. Proposed `TraitTurn` transfers into the existing arrival projection. Final cost settlement reads the current projected actor, preserving grants earned during the action.

**B2 delivered:** `TraitKillSettlement` is the only new attributed-death consumer. `CombatDamageApplication` invokes it after existing damage/health events, and movement/death completion flushes the same consumer. Its transient consumed-death set belongs to that disposable command projection, not a persistent reward ledger. Existing `CombatDamageCredit.Recipient` retains the owned-player reward filter; its shared character attribution supplies either side's real credited trait owner. Call of Blood uses each defeated enemy's pre-death max HP, half-up rounding and ordinary clamped recovery. Morale uses pre-death base `Stats.AttackPower`, rounds each victim separately (13 × 20% gives 3; two victims give 6), and grants current living field allies including the owner. Dead owners do not resurrect; dead/hand recipients gain nothing. KillChain later strikes recalculate common damage when a newly gained trait contribution changes ATK, preserving their existing action/AP boundary.

The internal consumer signature takes the existing `CombatDamageApplication` rather than introducing another world/death owner. This is an implementation connection within the planned common damage boundary; gameplay, reward attribution and public Buffer ownership remain unchanged. The existing `CharacterTraitEffects` and owned Hunter field remain the sources of the derived attack projection.

**B3 delivered:** `DisarmState` and `AnkleBreakerState` store one source and remaining affected-side turn ends. Reapplication replaces the configured duration and source instead of adding duration. `TraitRestrictionApplication.AfterContact` runs from the direct-damage contact boundary, requires the actual nonempty Primary-derived `SelectedTargetId`, and rejects dead/nonhostile/noncharacter recipients. Direction/All, structure/empty primary and splash-only victims do not acquire restrictions. Shared admission drives attack/move availability, queries, previews, execution and AI; attack restriction also gates reaction, follow-up and pending Roar. An attempted attack consumes no duration. Forced displacement remains outside voluntary movement admission. Disarm-only AI still pursues: only its disposable future-destination attack geometry ignores the current attack lock; committed state retains the lock.

`TraitTurnLifecycle.EndControllerTurn` runs after existing tornado and train/gimmick end actions, before next-side input. It expires due attack grants for the current controller end and decrements received restrictions only on the affected side's end. Existing controller-start handling resets that side's attack ordinal. Duration 1 and configured duration 2 are both tested, including application during the affected active turn. Intimidation, DefenseReduction and HuntersMark reconstruction now preserves both restrictions; restriction expiry preserves those independent statuses. Existing death/stage cleanup already clears the full immutable debuff/received-grant state. Attack failure `Disarmed = 50` and movement reasons `Disarmed = 50` / `AnkleBroken = 51` are appended without renumbering existing values.

**Verification:** All evidence is under `.superpowers/trait-system/`, with fresh XML/log/exit files and AppData-authorized Unity 6000.0.73f1 launches. Compilation/infrastructure failures are not counted as meaningful RED evidence.

| Task / check | Meaningful RED | Final focused GREEN |
|---|---|---|
| B1 action notifications / publication | B1-red-2: 6 expected failures; B1-publication-red: 2 | B1-green-2: 73/73; B1-publication-green: 8/8 |
| B2 kill attribution / publication | B2-red: 7; B2-publication-red: 2 | B2-green-2: 33/33; B2-publication-green-2: 5/5 |
| B2 later-strike projection | B2-chain-red: 8/9, stale ATK on the later KillChain strike | Included in B2-green-2 and the full Core gate |
| B3 restriction / lifecycle | B3-red-2: 13; B3-publication-red: 2 | B3-publication-green-2: 9/9 across B1/B2/B3/completion |
| Shared enum / movement / status regression | Existing movement enum guard and B3-enum-red caught implicit renumbering; independent-status tests caught a constructor-argument editing error | B3-regressions-verified: 186/186, covering player movement, action costs, Pull boundaries, enemy paths, debuffs, shape guards, effects and targeting parity |

Final phase-wide `B-core-verified.xml`: **3865/3869**, four baseline failures. `B-integration-verified.xml`: **272/277**, five baseline failures. Both have zero skipped cases. Exact failed-fullname-set comparisons against `A-core-verified.xml` and `A-integration-verified.xml` have no added or removed failures. All **39** newly added B cases pass. The existing nine failures are listed in §11.1; no unrelated baseline fix was included. Framework/View test execution, PlayMode and manual scene/Inspector acceptance were not performed.

**Final review:** One fresh read-only reviewer found one Important defect and no Critical/Minor findings: Thorn Heart reflection deaths reached the new consumer before the existing reflection attribution context was installed. Actual enemy-Advance tests reproduced both the surviving-reflector recovery loss and missing ally Morale (B-review-reflection-red: 0/2). ThornHeartAttackScope now defers only trait-death consumption during common damage, installs its unchanged activation/context, then flushes the same consumer before later attack stages. The existing null credited-origin policy remains unchanged. B-review-reflection-green passed 101/101, including both new cases, exactly-once recipients, no resurrection and all reflection/common damage regressions. Both final full gates above were rerun after this fix; exact failure sets remain unchanged. No second reviewer or untested fix was used. Later mechanics/View activation and the nine baseline failures were outside the B review, with those boundaries recorded in the ledger.

**Actual locality and ownership:** Initial B integration necessarily touched attack admission/dispatch, common damage credit, stat projection, immutable status reconstruction and controller ends. Future Morale numeric tuning uses its existing SO field; formula changes stay in `MoraleBoostTraitRule` and consume base-ATK/rounding/recipient contracts through the common kill adapter. No Leap, Taunt or Hunter selection implementation is needed. Changing Disarm duration from 1 to 2 uses only its existing SO field; the actual tests demonstrate refresh and affected-side end behavior. Targeting-semantic changes would also require the shared selected-target contract. AI, preview/execution, forced movement and turn-end regressions remain necessary because they share those boundaries. Registry/status wiring is normal assembly work, not another state source. No service, subscription bus, mirrored Buffer store, SO mutation or universal effect framework was added. No Phase B change was needed in Buffer because Phase A's immutable equality/projection validation already enforces atomic publication.

**Source manifest:** 44 new C# files with matching .meta files, 29 existing C# files modified. Relative prefixes C/T/TI are defined in §2.

- For each exact stem `Vanguard`, `CleanSweep`, `CallOfBlood`, `MoraleBoost`, `Disarm`, `AnkleBreaker`: `C/Domain/Traits/States/<Stem>RuntimeState.cs`, `C/Contracts/Snapshots/Traits/<Stem>TraitStatusSnapshot.cs`, `C/Domain/Traits/Rules/<Stem>TraitRule.cs`, `C/Domain/Traits/Effects/<Stem>EffectRule.cs`, `C/Application/Combat/Traits/<Stem>TraitFactories.cs` (30 files).
- New shared files: `C/Application/Combat/{AttackTraitScope,TraitAttackNotifications,TraitKillSettlement,TraitRestrictionApplication,TraitTurnLifecycle}.cs` and `C/Contracts/Combat/{DisarmState,AnkleBreakerState}.cs` (7 files).
- New tests/fixtures: `T/{TraitCombatFixtures,TraitAttackNotificationTests,TraitKillRewardTests,TraitRestrictionTests}.cs` and `TI/{TraitAttackGrantPublicationTests,TraitKillRewardPublicationTests,TraitTurnLifecyclePublicationTests}.cs` (7 files).
- Modified existing combat files: `C/Application/Combat/{AttackDirectDamageCalculator,BossRoarCalculator,CombatActionAdmission,CombatAttackCalculator,CombatAttackQueries,CombatCommandEffects,CombatDamageApplication,CombatDebuffApplication,CombatEffectiveStatsCalculator,CombatTurnCalculator,CommandAttackProjection,DeferredCombatFeatures,FollowUpAttackExecutor,HuntersMarkApplication,KillChainAttackCalculator,MovementProjection}.cs`; `C/Application/Combat/EnemyAi/EnemyMovementPathfinder.cs`; `C/Application/Combat/Rewards/CombatDamageCredit.cs`; `C/Application/Combat/Traits/{TraitEffectProvider,TraitRegistry}.cs` (20 files).
- Other modified files: `C/Contracts/Combat/CharacterDebuffState.cs`, `C/Contracts/Common/TraitActivationKind.cs`, `C/Contracts/Results/{AttackFailureReason,MovementFailureReason}.cs`, `C/Domain/Combat/CombatDebuffCalculator.cs`, `C/Domain/CombatEffects/Rules/DefenseReductionStatusRule.cs`, `T/{ContractShapeGuardTests,TraitDefinitionContractTests}.cs` (8 files).

- Review fix also modifies `C/Application/Artifact/ThornHeartAttackScope.cs` (1 file). The final B manifest has 73 C# files. Source/meta pairs, LF/UTF-8, protected ownership scope and diff checks passed. Unity-generated audio metadata was removed and its catalog restored.

**Remaining / external boundary:** Phase B is complete; the user subsequently authorized Phase C. The other 16 new identities remain ImplementationPending; legacy deferred traits are unchanged. No production pool or unit asset was activated, and Vanguard/Clean Sweep fixture +3 remains a test value rather than an approved balance default. Phase C is the next authorized work; D–F, required balance/Main/enhanced content, presentation and joint acceptance remain later gates. For F1/View ownership: `ViewSystem/Combat/Playback/Handlers/CombatFactEventHandler.cs:49` reconstructs HuntersMark using the old three-field debuff constructor; that event consumer must preserve the two new restrictions when View integration is authorized. It was identified by source inspection and was not edited or manually accepted in Phase B.

### 11.3 Phase C implementation checkpoint — 2026-10-29

**Authorization and review state:** The user explicitly authorized C1–C5 after B. The existing checkout, uncommitted A/B source, branch and HEAD e15729850 were preserved. Implementation, both final full gates and the single fresh-context read-only C review/fix pass are complete. Stop at this C checkpoint. D–F remain unauthorized. No commit or push was performed.

**C1 delivered:** Solo Action and Cohesion use real typed rule/effect/status/factory registrations. TraitShieldGrant stores source, remaining budget, current eligibility and last controller-end reset boundary in the existing immutable CharacterTraitEffects. Presence queries never refill a source. Leaving/re-entry preserves spent or increased budget within the turn; source death/removal removes only that source, and explicit controller-end cleanup resets due budgets after existing end actions. These two continuous sources have no fixed expiry, so stable origin orders their equal expiry rank. CombatDamageApplication absorbs active budgets once after pre-damage items and before HP, keeping actual HP loss separate. Cohesion DEF derives from current living field neighbors without changing base stats. Existing placement, voluntary/forced movement, return, death and turn transitions reconcile current presence. The existing shared temporary-state cleanup now also clears shield-only histories and preserves owned Hunter ATK.

**C2 delivered / damage order:** Entrenchment contributes a typed remaining-damage percentage through the existing incoming engine. Current living field sources determine square-neighbor eligibility; center is excluded. Flat reduction allocations remain separate. CombatDamageCalculator.Attack retains splash/matchup/DEF, then evaluates flat/percentage reductions, existing terrain and minimum/badge handling. Existing mark and held-defense stages follow, then CombatDamageApplication handles item-before-damage, shield absorption, HP, item-after-damage, common death consumption and presence. Environment/reflection retain their existing caller-owned terrain calculation and enter the same incoming percentage engine at CombatDamageApplication before items/shields/HP; they do not gain attack-only matchup/DEF/minimum stages. ItemDamageContext now receives the already-reduced amount. All numeric kinds pass their percentage stage once; instant defeat remains a distinct cause. For100 damage, two50% auras yield25 and50% with20% yields40. No item, terrain, health or death owner was duplicated.

**C3 delivered / plan-apply boundary:** SacrificeDamagePlanner runs before canonical defensive mutation and returns immutable delivered-hit-index mappings. It simulates original full defenses including shields, then recomputes each proposed source's matchup/DEF/aura/held/item/shield/HP with the existing calculators in disposable character/group copies. It ranks minimum occupied-cell Manhattan distance, recipient Main, projected surviving source HP descending, then stable IDs; square radius2 independently controls eligibility. Reservations prevent a source from exceeding its configured enemy-turn cap or protecting a hit after its earlier death. Actual delivered hits consume the typed source status through existing codecs and apply only their chosen numeric recipient. Redirected damage never re-enters selection. Original contact, mark and displacement targeting stay with the originally delivered target; numeric allocation and death/credit follow the actual recipient. Direct, fixed/random additional and KillChain paths share this planner. Current-dependent defense sources require prior HP flushes; attacks without these sources preserve their prior batching/event order. InstantDefeat is excluded without consuming HP/shields/caps during planning; Finishing's actual execution is still unavailable E2 work.

**C4 delivered:** Taunt derives valid sources from current facts through its local rule/adapter. EnemyAiPolicy selects an attackable taunter by distance then stable ID, or prioritizes the existing pathfinder's legal approach to one. CombatAttackQueries.DeliversTo is shared with pursuit so walls, targeting and actual delivery restrictions remain owned by the same query. Pathfinding, movement prefixes, hazard handling, AP and action-use stay with their existing owners. Dead/out-of-area sources release the constraint. No legal approach or movement budget ends the automatic step normally; it never teleports, repeats movement or creates an AI loop. No persistent aura-membership map or new AI executor was added.

**C5 delivered:** Steadfast Will contributes only at AttackKnockbackCalculator through KnockbackImmunityPolicy.IsImmuneWithTrait. It does not set UnitBehavior forced-movement immunity, affect Cowboy Pull, or reactivate Heavy category immunity. A paired native Buffer scenario confirms the same equipped target blocks knockback and still publishes a pull.

**Verification:** All evidence below uses the local hidden Unity6000.0.73f1 runner with AppData-authorized execution, unique XML/log/exit files under .superpowers/trait-system/. Compiler and invalid-fixture failures are recorded separately from meaningful RED.

| Task / boundary | Meaningful RED | Focused GREEN |
|---|---|---|
| C1 shield history / publication | C1-red0/8; C1-publication-red0/2 | C1-regressions119/119; C1-publication-green-2 12/12 |
| Shared shield-only cleanup | C1-cleanup-red0/1 | C1-cleanup-green34/34 |
| C2 percentage composition / all numeric kinds | C2-red-2 0/9; C2-publication-red0/2 | C2-green-2 79/79; C2-publication-green6/6 |
| C3 complete defenses / cap / ranking / AoE / purity | C3-red0/10; C3-publication-red0/2 | C3-green-4 72/72; C3-publication-green8/8; additional fixed/random and reflection-kind boundary cases pass in final full Core |
| C4 attack / pursuit / invalid approaches | C4-red-2 0/8; C4-publication-red0/2 | Included in C5-green129/129; C4-publication-green4/4 |
| C5 knockback / unchanged Pull | C5-red-2 0/2; C5-publication-red0/2 | C5-green129/129; C5-publication-green16/16 across all C publication and existing Pull fixtures |

Pre-review full C-core.xml:3908/3912 and C-integration.xml:282/287. After the review fixes, final C-core-verified.xml: **3915/3919**, the same four Core failures; C-integration-verified.xml: **284/289**, the same five integration failures. Exact failed-fullname-set comparisons against B-core-verified.xml and B-integration-verified.xml show no additions/removals; zero skipped cases. All **62** added C cases (50 Core,12 integration) pass and no prior test was removed. The nine baseline failures remain listed in §11.1. Framework/View test execution, PlayMode, Inspector and manual scene acceptance were not performed.

**Final review and one fix pass:** One fresh read-only reviewer found four Important defects, no Critical/Minor findings. All four were confirmed against the real owners and reproduced before production fixes:

1. Direct redirection passed the original target distance while planning used the source distance, producing opposite Door Breaching DEF decisions. Both near/far cases failed; direct execution now uses the actual numeric recipient's distance, matching simulation and fixed/random extras.
2. Turn-start Changed received active-side Field updates only and reconciled that partial world, incorrectly publishing Solo protection beside an opposing unit. It now overlays those changes on all input characters before presence reconciliation. Both pure starts and both actual Buffer starts were reproduced and now pass.
3. Saving an earlier Entrenchment provider made a later reserved Main hit nonlethal, but the reservation still spent source HP/cap. Each proposal now rechecks original complete-defense lethality for existing mappings and removes unnecessary reservations before accepting/ranking the resulting batch.
4. Simulation omitted lethal Thorn Heart cancellation and could reserve a later undelivered Main hit instead of protecting an earlier Sub. Each simulation clones the existing per-attack ThornHeartAttackScope and uses its ordinary reflection/stop behavior in the disposable damage projection; the actual scope is untouched. Undelivered mappings are removed. The existing piercing-after-reflection continuation remains active and has a positive paired regression.

C-review-red: **0/6** (both distances, both pure starts, preserved-aura AoE, interrupted AoE). Native C-review-publication-red-2: **0/2** (both actual starts). The first native player fixture still contained an allied neighbor and was corrected before meaningful RED. C-review-green: **151/151**, including the six failures, piercing continuation, all reflection/common damage/shield/percent/turn regressions. C-review-publication-green: **18/18**, including every C publication fixture and existing Pull scenarios. Both final full gates above were rerun after this single fix pass. No second review dispatch or untested fix was used.

The reviewer set aside later D/E execution (including actual Finishing/Hunter), production/View/manual acceptance, and the nine baseline failures. Each boundary is accepted in the ledger because it follows the authorized C scope; its remaining acceptance cost is explicit rather than silently dropped.

**Expected versus actual locality and ownership:** The planned first integration necessarily adds common presence/absorption and complete-defense planning connections. Actual changes stayed at those seams; no Framework service, Buffer store or source asset was added. The transient simulation is proposed state only; authoritative budget/cap/HP ownership remains CharacterTraitEffects, existing trait statuses and existing expected/final Buffer publication.

| Change scenario | Planned edit / understanding scope | Actual scope and required verification |
|---|---|---|
| Solo/Cohesion shield or reset rule | Local definitions/rules plus the explicit presence/end boundary; understand source budget and reset contract | Same. Radius/amount tuning is existing authoring data; eligibility/reset formula changes stay in local rules and shared presence contract. Shield/presence and real publication checks ran; movement/return/force/death/turn regressions remain required because those alter eligibility. No AI, View or Job internals needed. |
| Entrenchment percentage | Local applicability plus the typed common incoming stage; understand damage kind/order | Same. It reuses existing engine/callers rather than introducing an alternative HP pipeline. Attack/environment/reflection, overlap, terrain/items and publication checks ran because all feed that boundary. No trail, Leap or UI implementation needed. |
| Sacrifice priority/cap | Local planner/rule/status plus existing batch/defense contracts | Same. Direct, additional and KillChain dispatch connections are needed to avoid divergent lethal tests; the planner calls common defenses rather than duplicating their internals. Full original shield, recalculation, AoE/source death, cap/nonrelay and publication tests ran. Shared damage/attribution regressions remain necessary. |
| Taunt targeting/pursuit | Local source eligibility, existing AI policy and legal attack/path contract | Same. The pathfinder reuses the shared delivery query; movement/AP/hazard owners stayed intact. Attackable/unattackable/multiple/dead/out-of-area, walls, occupancy, budget and already-moved cases ran. No extra turn loop, View or scene knowledge is required. |
| Steadfast knockback immunity | Local trait and knockback-only policy; understand separation from Pull/behavior immunity | Same. No Pull code changed. Existing knockback policy/path/Pull boundary and paired native publication checks ran to substantiate the promised separation. Heavy category policy stayed disabled. |

**Source manifest:** The frozen pre-fix review is .superpowers/trait-system/phase-c-reviewed.diff and phase-c-reviewed-source-manifest.json (78 C# files:46 new,32 modified). Final C-only delta phase-c.diff and phase-c-source-manifest.json contains79 C# files (46 new,33 modified), compared with phase-c-base/manifest.json. Each new C# file has a matching .meta. Prefixes C/T/TI are defined in §2.

- For each exact stem SoloAction, Cohesion, Entrenchment, Sacrifice, Taunt, SteadfastWill: five runtime/status/rule/effect/factory files (30).
- Other new production files: C/Contracts/Combat/{TraitShieldGrant,CombatDamageCause}.cs; C/Domain/Traits/{Rules/ShieldGrantRules,Effects/SacrificeEffectStateSnapshot}.cs; C/Application/Combat/{TraitPresenceReconciler,SacrificeDamagePlanner,EnemyAi/TauntTargeting}.cs (7).
- New tests: T/{TraitShieldTests,TraitDamageReductionTests,SacrificeTraitTests,TauntTraitTests,SteadfastWillTraitTests}.cs and TI/{TraitShieldPublicationTests,TraitDamageReductionPublicationTests,SacrificePublicationTests,TauntPublicationTests}.cs (9). The paired Pull scenario extends its existing test file.
- Modified source/contract paths are enumerated exactly in the hash manifest. They cover common attack/damage/placement/turn orchestration, current-fact projection, incoming context/engine/evaluation, codecs/availability and knockback/pursuit policy; no View/Flow/Job or production content change is present. The final review also modifies C/Application/Artifact/ThornHeartAttackScope.cs only to clone its existing per-attack conditions/opportunity. Source/meta pairing, UTF-8/LF, protected-scope and diff checks passed; the touched placement file was normalized from inherited CRLF. Unity-generated audio metadata was removed and its catalog restored after the final run.

**Remaining:** Phase C is complete; stop here for user review. D–F mechanics, ten remaining implementation-pending identities, required TBD/enhanced/Main content, final public presentation handoff, production activation and manual/joint acceptance remain pending. The existing View debuff reconstruction follow-up in §11.2 remains for F1. Leave all work uncommitted for the user.

### 11.4 Phase D execution — movement and retained attack preparation

**Authorization and baseline:** After the verified C checkpoint, the user explicitly requested D after C. D1–D3 are the authorized implementation scope; E–F remain pending. The existing feature/Cheon/Core/NewCombatSytem checkout and HEAD e1572985064ab3cf98f9a420916e4d092e8e6f06 were retained with all A–C changes. The D baseline is the exact 1,709-file source snapshot at .superpowers/trait-system/phase-d-base/manifest.json. Starting verification was C-core-verified3915/3919 and C-integration-verified284/289, with the same nine baseline failures and no skips. No commit or push was performed.

**Implemented behavior:**

- Ground Compaction and Dust Screen emit immutable TraitTrailFact sources through the existing CommandEnvironmentState source allocator. Start and completed voluntary path cells are included; wall/cliff cells are excluded. Terrain remains separate. Active sources retain their own origin, creating side, configured magnitudes, visibility and opposing-end lifetime. Per-field maximum arbitration preserves weaker unexpired sources. Both owners use/receive effects. Dust reduces only ordinary/extra/reaction attack damage and floors attack range at zero.
- Object and trail creation share one contiguous source-ID span; only Objects increment the existing Object entry ordinal and Object classification. The existing Buffer expected/final validator checks successor identity, lifetime, visibility and allocator facts before installation. Battle definitions reject runtime trail history. Opposing-side expiry follows existing end actions. Normal command completion deactivates trails at stage end; existing EndBattle disposes the entire environment for every completion route. Undo/refunds remain deferred.
- Ground allowance is captured from the start cell once, shared by availability, range, normalization, context, route validation and AI. Trails encountered later do not enlarge the current route. Units without a field anchor retain the existing availability failure.
- Leap distinguishes intermediate traversal from legal landing. Intermediate wall/cliff/unit cells are traversable within the same board/range/cost contract; final occupancy and terrain constraints remain. Only intermediate mines are skipped. Destination mines and other hazards retain existing behavior. An alive interruption on an unlandable transit cell rejects the disposable proposed move as DestinationOccupied; it introduces no relocation or refund. Accepted interrupted routes publish their actual completed cells. Forced movement is unchanged.
- AccelerationTraitStatusSnapshot owns held ATK, and StationaryFireTraitStatusSnapshot owns voluntary movement this own turn. The existing MoveSucceeded boundary supplies completed voluntary steps once; force displacement contributes none. The +2/cell fixture produces held6 after three completed steps. Held ATK survives controller ends, night and recall; own-side start resets only the movement flag, including inactive records. Shared death/stage cleanup and fresh-battle validation clear/reject prior history while preserving Hunter and unrelated attack owners.
- Existing AttackTraitScope holds an ephemeral capture and parent link. Repeated KillChain strikes share the capture while reading current Morale/target facts; independently scheduled attacks exclude active ancestor claims. Common damage, target conditions, Pull rebuilt delivery and Sacrifice planning use the same scoped calculation facts. These disposable actor copies never enter the authoritative world. Finish consumes the captured contribution once. Stationary Fire contributes a configured damage multiplier (fixture/default2.0), without awarding ATK or losing its flag when UnitActionUse becomes Attacked.

**Behavioral verification:**

| Boundary | RED evidence | GREEN evidence before final review |
|---|---|---|
| D1 trails, terrain, overlap, expiry, attack kinds | D1-red-2 Core0/11; D1-publication-red0/2 | D1-green171/171; D1-regressions173/173; D1-publication-green-2 16/16 |
| D1 hand availability | D1-boundary-red-2 1/2, absent-anchor failure | Included in173/173, paired enemy voluntary trail test |
| D2 transit, landing, mines, partial path | D2-red-2 Core0/12; D2-publication-red0/2 | D2-green182/182; D2-final-core182/182; D2-final-publication10/10 |
| D2 alive interrupted illegal landing | D2-landing-red0/3, wall/cliff/unit | Included in D2-final-core182/182 |
| Fresh definition versus runtime trails | D2-boundaries-publication-red5/6 | D2-final-publication10/10; mixed Object/trail allocator positive case also passed |
| D3 held gain, action capture, Stationary condition | D3-red Core0/12; D3-publication-red-2 Integration0/3 | All17 D3 Core cases pass in the full gate, including nested Lucky Bullet/current Morale/relative-ATK and enemy movement |
| D3 native state/publication/lifecycle | Pending execution failures above | D3-publication-verified-2 35/35 across D and earlier related publication fixtures, including all eight turns/night/recall, stale rejection, duplicate no-op, fresh-history rejection, projection validation and atomic Hunter coexistence |

Fixture corrections are recorded in the ledger: initial compile-only runs are not RED evidence; native requests must use actual newly submitted numbers; bool reflection construction must select the parameterized constructor; a line attack can hit both units; AttackHitResult records actual HP loss; death cleanup is checked at existing command completion; ThornFieldCellSnapshot distinguishes duration from damage; owned changes use the existing combat-command atomic boundary. None of those fixture errors justified a new production ownership or lifetime policy.

**Full gates before final review:** D-core.xml **3960/3964** and D-integration.xml **297/302**, zero skips. Exact failed-fullname sets match final C: the same four Core and five integration failures listed in §11.1. All **58** added Phase D cases (45 Core,13 integration) pass; no prior case was removed. Core failures remain ExperienceAttributionPathTests.DeferredGroundAtPlayerEnd_DoesNotCreateKillsOrExperience and WeakGroundTurnTests.ControllerCompletion_DoesNotCollapseOrCountGround(Player,0)/(Enemy,1)/(Enemy,8). Integration failures remain BossStagePreparationTests.ProductionBossStage_PreparesAndPlacesBoss (missing StageKey2101), PullAndMarkPresentationTests.Pull_AttackMotionRunsOnlyForDirectHit(False)/(True) and Pull_PreviewAndAttackTargetUseArrival(False)/(True). Framework/View test execution, PlayMode and manual scene acceptance were not performed.

**Locality and ownership — expected versus observed:**

| Change scenario | Required edit / understanding scope | Observed verification and boundary |
|---|---|---|
| Trail magnitude or expiry | Local definitions/rules and typed source lifecycle; allocator/publication contract for a source-shape change | Same. Amount/range arbitration stays local; shared movement/range consumers read the local query. Terrain, max overlap/weaker resumption, actual opposing-end and allocator publication checks ran. No View, Job, or other environment rule internals are required for tuning. |
| Leap traversal or landing | Local eligibility plus the existing normalizer/route/range/AI distinction and arrival policy | Same. These consumers necessarily share traversability; final legality and destination arrival remain existing contracts. Wall/cliff/unit, mines, non-mine interruption, preview, AI, repeated cells and native publication checks ran. Forced displacement did not gain a new policy. |
| Acceleration gain/consume | Local typed owner state and existing move-success/action-finish contracts; projection invariant | Same. Registration/provider/codecs are normal assembly changes. The existing action scope and all common delivery paths share one capture; no persistent reservation store or second path history was introduced. Current-stat repeats/independent extras, force/Pull, turns/night/recall, death, Hunter coexistence and native atomic publication ran because they touch these lifetimes. |
| Stationary condition | Local movement fact and target-damage factor; own-side reset | Same. It does not inspect UnitActionUse to reconstruct lost movement history. No attack-ATK grant or extra turn owner was introduced; paired moved/unmoved, current/opposing/inactive starts, force movement and actual publication checks ran. |

**Source and review checkpoint:** The D-only manifest and diff are .superpowers/trait-system/phase-d-source-manifest.json and phase-d.diff:69 C# files (35 added,34 modified). Frozen review copies are under phase-d-reviewed/; hashes match the current sources. Every new source has a .meta; UTF-8/LF, protected-scope and diff checks pass. No View/Flow/Job, scene, prefab, Framework, FieldCamera, Architecture or production asset change is included. The single fresh read-only phase-wide reviewer verified the69 source hashes and both full XML failure sets, and reported no Critical, Important or Minor findings. No fix pass or second review was needed. Review boundaries accepted: actual E rules/RNG/rewards remain pending; production/View/Flow/manual acceptance remains with F and the owning workers; the same nine baseline failures remain outside this delta; A–C were examined only for D integration impact. These boundaries limit the completion claim and do not waive required D regression checks. Source hashes still match the tested/reviewed snapshot. Unity-generated audio metadata was removed and the catalog restored after the last run.

**Remaining:** E1–E4 execution (Armor Breaker, Finishing Shot, Tracking Shot, Hunter and Focused Bombardment), F1–F3 presentation/content/handoff, required TBD production balance, enhanced/Main pool activation and manual/joint acceptance remain pending. The View debuff reconstruction follow-up in §11.2 still belongs to F1. Leave all work uncommitted for the user.

### 11.5 Phase E execution — per-strike rules and Hunter persistence

**Authorization and baseline:** The user authorized Phase E after the completed D checkpoint. This plan has E1–E3; there is no E4. F remains outside the authorization. The same feature/Cheon/Core/NewCombatSytem checkout and HEAD e1572985064ab3cf98f9a420916e4d092e8e6f06 were preserved with all A–D changes. The exact pre-E source snapshot contains 1,744 files under .superpowers/trait-system/phase-e-base/. D baseline gates were Core 3960/3964 and CoreIntegration 297/302, the same nine existing failures and no skips. No commit or push was performed.

**Implemented:**

- Armor Breaker stores immutable origin-tagged percentage stacks in CharacterDebuffState.ArmorBreak, separately from the existing timed DefenseReduction. Effective DEF takes the existing item/presence input, multiplies exact retained rates using BigInteger, and floors only the final value. The current hit uses old DEF; post-hit actual numeric recipients receive the next stack before another strike. Direct, fixed/random extras, KillChain and Sacrifice's disposable simulation call the same local rule. Core debuff reconstruction and equality preserve the state. Actual night/recall retains it; shared death/stage cleanup clears it; fresh preparation rejects old history.
- Finishing compares pre-strike HP×100 strictly below current effective owner ATK×threshold. Tracking compares current HP×100 inclusively against maxHP×threshold on every strike. Focused captures living hostile unit count from the resolved action selection, excluding structures; repeated strikes retain the qualification, while independent additional actions recapture it. Factors use the existing rational target-damage stage.
- Instant defeat is a distinct CombatDamageCause on character/structure death events. The common death owner sets actual HP to zero and retains attribution, real HP-loss accounting and one equipment death discard. It does not synthesize a numeric damage request or enter damage-item, badge, shield or Sacrifice consumption. Existing reflection/contact/delivery and common kill settlement remain connected. A mixed AoE can execute one victim while normal lethal damage to another still uses Sacrifice.
- HunterTraitStatusSnapshot holds target ID and successful rewards this stage; the existing OwnedCharacterRecord.HunterAttackBonus remains the sole Run ATK source. Selection uses highest current HP, stable entity IDs and the existing captured environment RNG for ties. Participation and initially empty enemy-field transitions select a target, and HP changes do not retarget a living lock.
- The existing attributed-death consumer proposes an owner reward immediately, incrementing cap and projecting the new Run contribution before later strikes/grants. Other killers cause no award/cap increment. Action finish performs reselection; an active ancestor scope prevents nested follow-ups from retargeting the parent's AoE/repeated action. A maximum of 3 fixture rewards of 2 produces Run +6 and then no mark. Death/stage resets stage facts, night/recall preserves them, and a real second-node battle starts with cap 0/target absent while retaining Run ATK. Abandon followed by StartNewRun replaces all contributions with zero.
- The existing shared command damage projection carries command-local OwnedCharacterChange proposals, and CurrentInput exposes their final proposed values. Finish composes them with XP/growth using original expected records and the same native transaction. ExperienceApplied events point to that same combined change, preserving the existing CombatRewardConsistency contract. No persistent Hunter cache, new RNG stream, Framework service, Buffer store, or alternative HP/transaction owner was added.

**Behavioral evidence:**

| Boundary | RED | Focused GREEN |
|---|---|---|
| E1 DEF timing, multiplicative rates, preservation | E1-red-2 Core 0/5; E1-publication-red0/2 | E1-regressions136/136; E1-publication-verified13/13, including actual 8-turn night/recall and exact final-only rounding |
| E2 pre-HP thresholds, factors, scopes and defeat | E2-red Core 0/13; E2-publication-red0/2 | E2-green91/91; E2-publication-green22/22; same-action repeated delivery, actual Whiskey/death/Blood and mixed normal-Sacrifice additional cases pass |
| E3 target/cap/Run ownership | E3-red29/36: all 7 Hunter cases pending, 29 earlier cases pass; E3-publication-red0/7 | E3-regressions275/275; E3-publication-green-2 44/44, including current Hunter/Morale/Acceleration/KillChain and credited knockback fall |
| Hunter plus XP in one victory | E3-publication-green42/43: native receipt validator rejected XP's post-Hunter expected record | E3-publication-green-2 44/44 after aligning the existing XP event to the composed original-to-final record; validator unchanged |
| Completed Hunter authoring availability | Initial full E-integration309/315 retained an obsolete A1 pending-Hunter expectation | E-authoring-verified40/40 after replacing that expectation with configured native/pool payload acceptance; unset/TBD rejection remains |

Compile-only fixture runs are not RED evidence. Other fixture corrections are explicit in the ledger: existing DEF is percentage mitigation rather than flat subtraction; the first night fixture allowed default enemy ATK 5 to kill Main HP 10 and correctly end the stage, so the lifetime fixture uses zero enemy ATK and asserts the battle stays active; one ItemEventStage namespace was corrected. None justified a new damage, recall, ownership or validation policy. The explicit shape guard was updated only after ArmorBreak copy/cleanup/native installation checks were in place.

**Acceptance migration:** A1's ImplementationPending_NativeAndPoolActivationRejectWithAssetContext used Hunter as the last still-pending identity. E3 implements it, so that one case is explicitly replaced/renamed to ImplementedHunter_NativeAndPoolPayloadsAreAccepted. No prior acceptance obligation is silently deleted: required unset Hunter magnitude rejection remains covered separately, all new native cases exercise the real rule, and no production asset is enabled. Test-name comparison must account for this one migration.

**Full gates before final review:** E-core-verified.xml passed **3991/3995** and E-integration-verified.xml passed **310/315**, both with zero skipped tests. Exact failed-fullname comparisons against D-core.xml and D-integration.xml preserve only the same four Core and five CoreIntegration baseline failures listed in §11.1. All **44 new cases** pass (31 Core, 13 CoreIntegration), as does the explicitly replaced Hunter authoring case. There are no removed Core cases; Integration has only that named acceptance migration. Unity exit code 2 reflects the known baseline failures. Framework/View tests, PlayMode and manual scene/Inspector acceptance were not executed.

**Final review and bounded fix:** One fresh read-only reviewer found one Important gameplay defect, no Critical or Minor findings. Native direct AoE/piercing delivery refreshed the actor but reused action-start base damage, and Hunter alone did not flush prior death rewards before a later hit. Sacrifice batch planning retained the same frozen raw input. The real-command full-splash Hunter and disposable Sacrifice planning/application cases reproduced the defect before product edits. Additional partial-splash/piercing fixtures initially assumed the wrong DEF rounding; their inputs were corrected to expose the existing floor boundaries, without changing gameplay rounding. E-review-current-atk-red was 0/4 (two valid reproducers and two fixture errors); E-review-current-atk-green-3 passed **296/296**, including all four final regressions and related item, presence, reflection, Pierce, action and E rules. The fix settles Hunter/Morale prior HP/death effects and shares current common-damage evaluation between direct delivery and disposable planning, preserving captured Acceleration, resolved encounter/falloff/splash, root costs and notifications. Fixed single-follow-up raw inputs retain their existing calculation. No second reviewer or second fix pass was used.

**Final full gates after the fix:** E-core-final.xml passed **3995/3999** and E-integration-final.xml passed **310/315**, zero skips. Failed-fullname sets exactly match D: four Core and five CoreIntegration cases. All **48 new E cases** pass (35 Core, 13 CoreIntegration), plus the explicitly replaced Hunter authoring acceptance case. No other tests were removed. The earlier pre-review gate counts above remain historical evidence, not the final checkpoint.

**Expected versus actual locality:**

| Real change | Required edit / understanding scope | Observed verification and ownership |
|---|---|---|
| Armor percentage or stacking | Local definition/rule, received state and post-hit/effective-DEF contract | Same. Registration/copy changes are normal composition; later tuning uses the SO value. DEF/items/terrain, ordered strikes, existing statuses, C3 planning/application and native night/publication checks are necessary because they share these facts. No Hunter selection or View internals are required. |
| Threshold or damage factor | Local exact comparison/factor and action-versus-strike context | Same. Instant death necessarily connects the existing character/structure death and attribution paths. Current HP, strict/inclusive boundaries, repeated/independent attacks, item/shield/Sacrifice bypass, common kill effects and native preview/publication checks ran. No alternate damage framework was introduced. |
| Hunter selection, reward or cap | Local rule/status plus existing attributed deaths, captured command RNG and action finish | Same. Owned deltas stay in the disposable command; the existing atomic publication owns installation. Highest HP/ties, lock, unrelated killer, AoE, cap, immediate later-strike ATK, stale owned/status/RNG and duplicate checks ran. No second mark map or authoritative target cache exists. |
| Hunter across XP, stages and Runs | Existing owned-record/projection, reward-event consistency and preparation/reset contracts | Same. Native same-victory XP, next-node second battle, night/recall, abandon/new Run and existing growth/completion checks ran. Main baseline and growth records remain their existing owners. Scene/Flow/View acceptance remains external. |

**Source checkpoint:** The final E-only manifest/diff are .superpowers/trait-system/phase-e-source-manifest.json and phase-e.diff: **72 C# files (36 added, 36 modified)**. The original 70-file fresh-review diff/manifest and source copies remain under phase-e-reviewed*. The five post-review source/test edits belong to the single reproduced current-ATK correction. Final tested hashes are saved separately in phase-e-tested-source-manifest.json and match the working sources. All new .cs/.meta pairs, unique GUIDs, UTF-8/LF, protected-scope and diff checks pass. No View/Flow/Job, Framework, scenes/prefabs, FieldCamera, Architecture, production balance or activation change is included. Unity-generated audio metadata was removed and the catalog restored after the final run. Source did not change after the final full gates.

**Remaining:** The separately authorized E+1–E+4 checkpoint/correction and activation-fact work in §7A must precede F1–F2 handoff/presentation/content and joint/manual acceptance. Neither E+ nor F execution is authorized by this Phase E request. The existing View debuff reconstruction follow-up in §11.2 must additionally preserve ArmorBreak, and new DamageCause/Hunter target/cap facts require the planned consumer handoff. Required TBD numbers, enhanced/Main pools and production activation are not inferred from fixture values. Leave the work uncommitted for the user and stop after E.


### 11.6 Phase E+ execution — ordered activation and presentation facts

#### 11.6.1 E+1 checkpoint and finalized technical contract

The user explicitly authorized E+ after the E report. Frozen source phase-eplus-base contains 1,780 files and the 72 tested E hashes match. E-core-final passed 3995/3999 and E-integration-final passed 310/315: the same four/five baseline failures, no skips. F and all View/Flow/Job/production/manual work remain pending. No commit/push or checkout change.

The authorized technical proposal in §7A.2 is adopted: one optional-tail constructor adds canonical defensive-copy TargetIds/TargetCells, nullable actual ResultOrigin, AttackDirection and TargetCell. Legacy scalar TargetId is the projection of exactly one canonical ID; conflicting scalar/list input, duplicate/empty IDs, duplicate cells and invalid rotation fail before assignment. Owner identity and creditor identity are deliberately independent; null credit/group 0 remain valid. Existing immutable result copies retain the same immutable snapshots. Ordered CombatResult.Events is the playback source; AttackResult.TraitActivations is a summary only and must derive from those occurrences rather than producing another event. Dedicated Pull/Mark/Roar results stay.

State-only changes will use an immutable expected/final character fact result in the existing ordered CombatResult.Events channel, with identity validation and no independent state store. E+3 extends the existing movement-source result to creation (absent Expected) and typed changes with the same source ID. These are listed technical extensions, not new gameplay/ownership decisions. Consumer handlers/grouping remain F1 work.

Existing values 0/10/11/12/20/21/22/70/80/90/100/110/120/130 remain unchanged. Add only missing meanings: DamageModified=140 (absolute damage-point difference at its applied numeric stage, not capped HP loss), DefenseBypassed=150 (DEF points ignored), StatusApplied=160 (trait-specific turns/charges/flag/percent), StatusConsumed=170 (trait-specific ATK/bonus/charges), StatusRemoved=180, StatusExpired=190 and StatusReset=200 (0; paired result carries before/after), ShieldGranted=210, ShieldAbsorbed=220, ShieldRemoved=230, ShieldReset=240 (budget change; zero is meaningful only for an actual source/reset transition), MovementChanged=250 (exceptional completed steps), TrailCreated=260 (one typed source), TrailChanged=270 (remaining opposing ends), TrailRemoved=280 (0), KnockbackApplied=290 (completed cells, including a real blocked attempt), KnockbackPrevented=300 (attempted cells), PullApplied=310 (completed cells), TauntSelected=320 (0), RangeChanged=330 (selected allowance contribution in cells when an extension is required), DefenseChanged=340 (absolute DEF points), InstantDefeat=350 (actual HP), RoarTriggered=360 (affected cells, including empty reset). Each (key,kind) retains the units in the matrix; no sum across unequal recipient outcomes.

Eplus1-red passed 1/14: the stable enum case passes and thirteen missing-payload assertions fail. Eplus1-green passed 29/29 with shape/result/snapshot regressions; no skips. No gameplay formula or production value changed.

**Correction inventory:** PR-1/PR-2 missing payload/correlation are confirmed contract gaps (E+1/E+2); PR-3 recovery order, per-recipient grant events and missing state lifecycle results are confirmed source gaps (E+2); PR-4 no ordered trail changes is confirmed (E+3); PR-5 View debuff/trail handling is an explicit F1 handoff; PR-6 effective DEF/public event-time projection needs a native public-boundary reproducer in E+4. A–E timing, exact Armor ratios, original-versus-redirected defenses, source competition, Leap/path rules, Hunter lock/RNG/owned XP composition and Run/stage/night resets retain the tested E behavior and named tests. The previously fixed direct current-ATK gap remains covered by its four final regressions. Potential nested notification/C3 use of old owned input is an unverified risk: reproduce before changing, assigned to E+2/E+4. Unsupported duplicate profiled obstacles and unrelated baseline failures remain findings only.

**Producer matrix:** All 37 executable registered traits are included below; all 22 new traits and 15 active legacy traits. The five deferred registrations remain unavailable: Focus, MoveAttackDiscount, ThornField, Necromancy, Regeneration. Position Swap/Recon Squad remain absent. Magnitude, source owner, actual recipient/cells and application boundary are pinned here. Incoming effects use the real damage origin, not an origin rebuilt from their owner; follow-ups use their resolved aim. Acceptance groups below are coverage obligations for E+2/E+3/E+4, not claims that pending producer tests already passed. Every group requires positive, no-op and real removal/reset/expiry where applicable; pure queries/simulation/preview cannot commit occurrences.

| Key | Kind / Magnitude | Applied boundary | Persistent / dedicated read path | Acceptance group |
|---|---|---|---|---|
| Vanguard | AttackGranted: ATK per recipient; Expired/Removed: 0 | TraitAttackNotifications / turn and stage lifecycle | TraitEffects.AttackGrants | Grant |
| CleanSweep | AttackGranted: ATK; Expired/Removed: 0 | TraitAttackNotifications / lifecycle | TraitEffects.AttackGrants | Grant |
| CallOfBlood | HealthRecovered: actual capped HP | TraitKillSettlement before recovery | Health | Kill |
| MoraleBoost | AttackGranted: per-recipient ATK from one kill; Expired: 0 | TraitKillSettlement / turn lifecycle | TraitEffects.AttackGrants | Kill |
| Disarm | StatusApplied: remaining side ends; Expired/Removed: 0 | TraitRestrictionApplication / lifecycle | Debuffs.Disarm | Restriction |
| AnkleBreaker | StatusApplied: remaining side ends; Expired/Removed: 0 | TraitRestrictionApplication / lifecycle | Debuffs.AnkleBreaker | Restriction |
| SoloAction | ShieldGranted/Reset/Absorbed/Removed: budget delta | TraitPresenceReconciler / common damage | TraitEffects.Shields by origin | Shield |
| Cohesion | Shield lifecycle: budget delta; DefenseChanged: DEF points | Presence / common damage / public derived projection | TraitEffects.Shields; effective DEF projection | Shield |
| Entrenchment | DamageModified: applied reduction HP | Incoming contribution / applied numeric damage | Attack preview and damage breakdown; no mirrored aura | Defense |
| Sacrifice | DamageRedirected: actual redirected HP loss | Applied damage boundary after chosen reservation | Sacrifice status; real result origin, owner distinct | Defense |
| Taunt | TauntSelected: 0 for selected forced decision | Existing enemy AI taunt branch | Taunt owner status; actual move/attack decision | Decision |
| SteadfastWill | KnockbackPrevented: attempted cells | Existing immunity decision | Own status; knockback outcome | Decision |
| GroundCompaction | TrailCreated: 1; Changed: remaining ends; Removed: 0; RangeChanged: cells when used | Accepted start/step / opposing expiry / applied range decision | Typed TraitTrailFact; existing allocator | Movement |
| DustScreen | Trail lifecycle as above; DamageModified: reduction HP; range remains query context | Accepted path / expiry / applied incoming/outgoing branch | Typed TraitTrailFact; public/filtered range query | Movement |
| Leap | MovementChanged: exceptional completed steps | Actual accepted traversal/arrival decision | Ordered movement facts; no hidden-source revelation | Movement |
| Acceleration | AttackGranted: held ATK gain; StatusConsumed: captured ATK | MoveSucceeded / once at attack finish | Acceleration status; TraitAttackBonus | Movement |
| StationaryFire | StatusApplied/Reset: moved flag; DamageModified: HP difference | MoveSucceeded / own start / target-damage contribution | StationaryFire status; final hit breakdown | Movement |
| ArmorBreaker | ArmorBroken: configured percent; Removed: 0 | After actual hit before armor state result | Debuffs.ArmorBreak exact source stacks | Cannon |
| FinishingShot | InstantDefeat: actual target HP | Pre-strike qualifying branch before common typed defeat | Death DamageCause; no numeric shield/Sacrifice spend | Cannon |
| TrackingShot | DamageModified: applied HP difference | Current-HP target contribution before hit result | Current health / hit breakdown | Cannon |
| FocusedBombardment | DamageModified: applied HP difference | Frozen action qualification / applied strike | Existing scope only; hit breakdown | Cannon |
| Hunter | HunterTargetSelected: 0; AttackGranted: Run ATK; Reset/Removed: 0 | Participation/action finish / same attributed death / stage reset | Hunter status; owned HunterAttackBonus | Hunter |
| PiercingFalloff | DamageModified: actual encounter reduction HP | Actual native encounter before hit result | PiercingShot delivery record; preserved encounters | LegacyModifier |
| Knockback | KnockbackApplied: completed cells (0 for a real blocked attempt) | Actual knockback result before forced movement | AttackResult.Knockbacks and movement | LegacyDisplacement |
| DistanceDamageScale | DamageModified: applied distance-stage HP difference | Applied distance contribution | Hit damage breakdown; no persistent mirror | LegacyModifier |
| KillChainAttack | KillChainAttack: requested damage after held defense, before trait shields/HP cap | Existing repeat occurrence, add same result origin and aim | Ordered occurrences; existing parent action scope | LegacyFollowUp |
| Roar | RoarTriggered: affected-cell count, including empty counter reset | Existing accepted reaction before CombatRoarEvent | Roar status / dedicated result | LegacyDisplacement |
| Intimidation | StatusApplied: retained attack-reduction percent; Consumed/Removed: 0 | Existing applied debuff / attack-consume / lifecycle | Debuffs.Intimidation; priority component remains deferred | Restriction |
| ImpactBolt | KnockbackApplied: completed cells | Actual Impact Bolt result before force movement | Existing knockback result; distinct trait key | LegacyDisplacement |
| Pull | PullApplied: completed cells | Before existing CombatPullEvent | Dedicated pull result and movement | LegacyDisplacement |
| HuntersMark | StatusApplied: charges; StatusConsumed: actual bonus HP; Removed: 0 | Before existing apply/consume/remove mark result | Debuffs.HuntersMark; dedicated result | Restriction |
| ChainExplosion | FollowUpAttack: requested damage after held defense, before trait shields/HP cap; actual origin/aim | Existing accepted child occurrence only | Ordered child result; no duplicate occurrence | LegacyFollowUp |
| LuckyBullet | FollowUpAttack: requested damage after held defense, before trait shields/HP cap; actual origin/aim | Existing accepted child occurrence only | Ordered child result; no duplicate occurrence | LegacyFollowUp |
| DoubleShot | FollowUpAttack: 0 for delivered directional action; resolved aim | Existing accepted independent action before its results | Actual direction/selected cell, even no recipient | LegacyFollowUp |
| PrecisionShot | DefenseBypassed: first-recipient actual DEF points | Applied recipient-ordinal contribution | Actual delivered ordinal / hit breakdown | LegacyModifier |
| ContemptForWeak | DamageModified: applied HP difference | Applied current target comparison contribution | Current actor/target ATK / hit breakdown | LegacyModifier |
| DoorBreaching | DefenseBypassed: actual ignored DEF points | Applied distance-qualified bypass, positive effective DEF | Existing bypass contribution / hit breakdown | LegacyModifier |

Source/recipient cells use actual applied geometry. One logical grant groups only recipients from that occurrence; independent kills/strikes/owners/origins stay separate. Activation follows its cause and precedes its own result. No-op/capped zero recovery or unchanged aura refresh emits nothing. A zero-HP effect may still represent a real forced decision, successful extra attack, source transition or prevented displacement. Stage disposal without a playable result is disposal, not a fabricated command.


#### 11.6.2 Applied producers, ordered results and public correction evidence

E+2 and E+3 are implemented. E+4 implementation, focused checks, both final full gates and the single fresh phase review/correction pass are complete. No View, Flow, Job, Framework, scene, prefab or production-content implementation is included.

- Every executable identity in the 37-row matrix has a positive ordered-event assertion. The new Core suites are TraitActivationContractTests, TraitActivationOutcomeTests, TraitActivationMovementTests and TraitEffectiveStatsProjectionTests; native suites are TraitActivationPublicationTests and TraitPresentationContractTests. Existing trait, damage, source and owned-persistence regressions also run. Identity-to-test mapping is retained in the phase evidence workspace (phase-eplus-producer-evidence.json). Deferred registrations stay unavailable.
- Damage modifiers carry immutable contribution evidence through the existing damage result, then emit at actual application. Prefix products assign stage deltas without changing aggregate rounding or recalculating an attack with a trait disabled. DamageModified means damage points at that stage; shields and HP caps can make actual HP loss different. Sacrifice and recovery use actual HP loss/gain. Follow-up/KillChain Magnitude retains the old requested packet after held defense and before trait shields/HP caps.
- CombatTraitStateEvent carries the complete immutable expected/final CharacterCombatFacts plus ExpectedStats/FinalStats (CombatEffectiveStatsSnapshot.AttackPower and PhysicalDefense). Those numbers come from the same Core calculator as CharacterData. Baseline Stats.PhysicalDefense remains baseline. Damage-specific bypass, received damage percentages, target matchup and range-query context stay in their existing preview/results.
- The native public DEF reproducer confirmed that CharacterData already exposed effective ATK/DEF but context-free DEF missed Cohesion. CharacterTraitEffects.CohesionDefenseBonus now carries only that derived current-neighbor contribution; gameplay target defense still calculates from the live world through the same local helper. Reconciliation refreshes it, copies retain it, cleanup clears it, and event-time stats observe it. No independent stat store, second occupancy map, View formula or new service was added.
- CombatMovementSourceEvent validates typed creation (absent Expected) and same-source successors. Buffer validates the recorded trail chain against the final environment; it preserves the existing weak-ground/static-terrain form and no longer dereferences absent Expected for trail creation. A missing source event rejects all stores before installation. IDs, object entry ordinals and RNG ownership are unchanged.

**Causal examples:** one Vanguard grant with multiple recipients precedes their state results; an actual death precedes Blood recovery; Sacrifice names its actual source and incoming creditor separately and precedes consumption/HP; post-hit Armor follows HP and precedes armor state; shield source removal/reset retains each source; Hunter selection/reward/reset retains owned attribution and actual RNG decisions; pre-hit Pull remains before the physical CombatAttackEvent. Start-cell TrailCreated/source-created precedes the first step; entered-cell Moved precedes TrailCreated/source-created; opposing end changes only due sources, preserving weaker active overlays. Hidden mines are not exposed in Leap metadata. EndBattle without a playable result remains disposal and adds no result.

**Summary compatibility ruling:** CombatResult.Events is the only playback stream. AttackResult.TraitActivations is a summary of the executed root interval before command-completion lifecycle: correlated occurrences must match that attack's group and creditor, while uncorrelated action-state occurrences retain null origin. Child attack groups remain in Events and do not enter the parent's direct-hit summary. Existing KillChain direct Hits, summary exclusion, physical attack/Pull/artifact order and action costs remain covered by their unchanged native assertions. Historical manually constructed Intimidation state without a received origin stays compatible; Core reports its state but does not invent an owner.

| Confirmed gap / reproducer | Owning correction | Focused evidence |
|---|---|---|
| Payload, canonical targets, independent creditor/owner | TraitActivationSnapshot plus stable additive kinds | Eplus1-red1/14 to Eplus1-green29/29 |
| Recovery/grouped grants, shield origin, post-hit state, lifecycle | Existing grant/kill/presence/damage/lifecycle owners | Eplus2-lifecycle-green186/186 |
| Applied modifier/Finishing evidence, repeated strikes | Existing damage pipeline result and application boundary | Eplus2-modifiers-green-2 425/425 |
| Mark/Intimidation/displacement/Roar/Taunt/additional aim | Existing dedicated owners and accepted AI decision | Final affected Core591/591; producer78/78 |
| Trail/source ordering, exact expiry and public chains | Existing movement/trail/source publication owners | Movement/source cases in Core591/591; native33/33 |
| Public Cohesion DEF and event-time stats | Existing trait projection and Core calculator | Eplus4-public-green20/20; native33/33 |
| Null Expected in weak-ground validator | Existing typed environment validation | Native source/Leap/stale checks33/33 |
| Redundant Steadfast occurrence under explicit immunity | Existing applied immunity boundary only | Producer RED then GREEN78/78 |
| Child group leaking into direct AttackResult summary | Actual group/credit filter of ordered occurrences | Original KillChain full-gate RED then summary GREEN86/86 |
| 0-percent Armor activation/removal | Suppress zero-contribution metadata, retain existing state history | Eplus-zero-red0/1 to Eplus-core-verified (case passed) |

No speculative A-E gameplay repair was applied. Notification calls use the root input before rewards, and independent child calls use arrival.CurrentInput with proposed owned facts; current Hunter/Morale/Acceleration/repeat and native same-victory XP/growth tests retain this contract. Missing production assets and F1's View handler/grouping changes remain separate owner work.

**Change locality and architecture:** modifying a trait rule still requires its local definition/rule and its existing application contract; shared contribution/result formatting replaces repeated inference of a scalar cause. Trail changes require the local lifecycle plus existing movement/source contracts and Buffer invariants, without unrelated environment implementations. Cohesion DEF requires the local world-derived contribution, existing character projection and Core calculator, without a UI/definition dependency. Registration/provider wiring remains normal assembly work. Required cross-feature shield/Sacrifice/repeated-hit/HP/owned/source/turn regressions were retained. SRP keeps rules, application and publication separate; OCP reuses effect bindings and explicit local producers; LSP preserves old constructors and existing dedicated result behavior, including the exact reflected two-argument CharacterTraitEffects constructor; ISP/DIP add no service interface or alternate dependency route. SSOT remains character/debuff/owned/environment state; ordered events and effective numbers are immutable results/projections, never effect replay storage.

**Final gates before independent review:** Eplus-core-verified.xml passed4090/4094; Eplus-integration-verified.xml passed317/322, both with zero skipped tests. All102 added cases pass (95 Core,7 Integration), with no removed cases and the exact same four/five failed fullnames as completed E. The full Core gate initially found the existing KillChain summary compatibility assertion; it was fixed at summary scope, confirmed86/86 and then both assemblies rerun. Existing consumer assemblies compiled. The audit covers52 C# files (7 new/45 modified), each new source has a unique .meta GUID, all changed source is UTF-8/LF and has no source-hash drift. Protected paths have no new changes from the post-E status snapshot. Unity-generated audio metadata is removed and its catalog restored; no cache deletion, version change or concurrent editor was used. The single fresh read-only phase review and its bounded corrections are recorded below; no commit/push.


#### 11.6.3 Single final review and bounded correction pass

The one fresh read-only reviewer (gpt-6-astra) verified all52 frozen source hashes, every phase delta and exact gate failure sets. There were no Critical findings. Two Important ordering findings were accepted. Two advisory Minors were regraded Important because they change the truth/units of the public playback contract, rather than polish: zero grants emitted effects that never activated, and Intimidation's documented duration unit could mislead a consumer of its actual percent value. No deferred Minor remains; no second review is dispatched.

| Finding | Minimal correction at existing owner | Verification |
|---|---|---|
| Sacrifice selected a later same-origin Armor/shield state for insertion | Match the actual Sacrifice status transition, including a new enemy-turn sequence with the same activation count; preserve all HP/defense ordering | Direct Armor, shield, combined and next-turn cases RED then GREEN |
| KillChain identity followed redirected consumption | Capture its existing occurrence insertion point before ResolveSingle; preserve current damage, group, target and action/cost calculations | Four actual redirected child variants RED then GREEN |
| Zero Vanguard/rounded-zero Morale produced countdown/expiry/removal activations | Filter zero Amount only in lifecycle metadata; retain historical grants, lifetime changes and exact state facts | Both traits with1/2 end durations RED then GREEN |
| Intimidation StatusApplied unit was documented as duration | Document retained attack-reduction percent in plan/matrix/evidence; pin20 in actual producer test; consumption/removal stay0 | Documentation contract check exits1 then0; producer assertion passes |

Eplus-review-red-2 failed all12 new supported-combination cases. The initial review run included four duplicate fixture IDs; those were corrected before meaningful child-order RED, with no product inference. Eplus-review-green passed266/266, zero skips, including all12, preserved original KillChain/physical characterization tests, Sacrifice, source shields, received grants, Intimidation and event-time projection. Only four existing application owners plus the outcome test file changed after review; no state owner, gameplay formula, target/RNG choice, cost or Buffer atomicity was moved. Both final full gates passed the accepted baseline comparison from those exact sources.

**Review boundaries ruled:** the same nine baseline failures remain outside this delta; View playback/concurrency/debuff preservation/trail rendering and Flow/scenes/prefabs/production balance/manual/Framework/PlayMode/device-performance acceptance require their owners/F. Deferred/unavailable mechanics and unsupported duplicate obstacle delivery remain outside supported acceptance. Physical Pull/artifact/fire ordering and the documented parent summary retain existing compatibility; ordered Events is the sole playback source. These boundaries limit the completion claim and do not waive Core regression requirements.

**Final verified delivery:** Eplus-core-final.xml passed **4102/4106** and Eplus-integration-final.xml passed **317/322**, no skips. All **114 new E+ cases** pass (107 Core/7 CoreIntegration); no original case was removed. Exact failed-fullname sets remain the same four Core/five CoreIntegration cases listed in §11.1 and frozen completed E. Final review fixes are covered by RED12 to GREEN266, the Intimidation unit contract check and both full reruns. No Critical, unresolved Important or deferred Minor remains in E+; there was one fresh review and no re-review.

Final source is52 files (7 new/45 modified), frozen and hashed in phase-eplus-tested/. All52 tested hashes match the working tree; only the four accepted application owners and outcome tests changed after review. All new metas/GUIDs, UTF-8/LF, diff/scope checks pass; there are no source deletions or unexpected new protected-path changes. Branch/HEAD and all earlier uncommitted work are preserved. Final Unity-generated audio metadata was removed and the catalog restored after the last sequential approved run. Evidence remains in the ignored phase workspace because the user will handle commits.

**Stop at Phase E+.** Core implementation/verification is complete. F1 handoff preparation/delivery is pending; View/Flow integration and production/manual acceptance remain pending. Existing dedicated event handlers, subject/concurrency routing, preservation of every debuff field and typed trail rendering require the owning View work. No production asset, scene, prefab, Job, Flow or unrelated baseline repair was performed. No commit or push was performed.


### 11.7 Original F1 — Core-to-View handoff preparation, 2026-10-29

**Authorization and identity:** The user authorized F1 documentation and explicitly confirmed this means the original handoff planned after E+, not a new remediation handoff. The planned path is retained: [Trait System — Core-to-View Handoff](../handoffs/2026-10-28-trait-system-core-view-handoff.md). Its preparation date is 2026-10-29. The document incorporates E+ and subsequent verified remediation contracts, particularly typed movement causes and root/Roar/child interception ownership. This is documentation only; no runtime contract is added in F1.

**Evidence:** All 1,792 current Core/Buffer/DB/EditMode source hashes match `TR-X3-source-manifest.json` (SHA256 `afe74fa47a1c3020a1b145d59215aeae1f99f64cab7aaf9e755fbb2c063d27d0`). Reused final XML: Core4154/4154 and CoreIntegration337/338, zero skips. The sole remaining failure is `BossStagePreparationTests.ProductionBossStage_PreparesAndPlacesBoss`, missing StageKey2101. Remediation D records all phase additions, prior failure changes and independent baseline-test repairs; neither older E+ counts nor those external fixes are represented as new F1 evidence. No Unity run occurred.

**Contents:** The handoff identifies source/creditor/group/null semantics, exact constructors/member names and compatibility, 37 executable producer identities and magnitude units, canonical playback carriers, actual-recipient Contacts/HP/recovery, event-time effective stats, all six debuff fields, source-specific shields, Hunter stage/Run lifetimes, typed trail creation/opposing expiry, and ordered examples. It distinguishes ordinary summary entries from canonical interceptions and child outcomes. It includes concrete View routing/grouping/acknowledgment work, manual/regression acceptance and separate external status tracking.

**Verified consumer gaps:** F1 inspected the current View sources rather than assuming they already consume E+ facts. CombatTraitStateEvent lacks a type-table row; Effects remains Facts-only; HuntersMark-only reconstruction drops Disarm/AnkleBreaker/ArmorBreak; the movement-source rendering branch handles StaticTerrainFact kind changes only; movement pacing still infers agency. These are documented View-owner tasks, not implemented in F1. Existing result token/acknowledgment, dedicated Pull/Roar handling, Framework/UI registration and state ownership remain the integration boundaries.

**Locality and ownership:** Repository-visible edits are this original implementation plan and the new handoff. Required understanding was public producer/result semantics and actual View consumption boundaries; no unrelated gameplay formula, service wiring or scene was changed. SSOT preserves one canonical occurrence and existing state owners; SRP separates cue rendering from applied state/result playback; OCP uses existing typed handlers/keys; LSP explicitly distinguishes source-compatible constructors from requested semantic migration; ISP/DIP add no interface/service/Buffer. Shared consumer regressions are necessary and remain pending with their owner.

**Review:** One independent read-only documentation review checked current constructors, producer units, root/Roar/child ownership, actual contacts, movement/Pull, state/trails, consumer gaps and acknowledgment. Its two documentation corrections are applied: HuntersMark consumption is the bonus damage-stage delta before held defense/shields/HP caps, and heterogeneous grouped shield deltas use Magnitude=0 with per-recipient budgets in paired state. The acceptance checklist now explicitly covers both. No runtime correction or second review was required.

**Requested future-work context:** The user additionally asked to consider the legacy-enhancement retirement plan for View. Handoff §11 now separates the current 37 executable identities from six planned retirements (future verified total 31), names the two View-owned appearance assets/table rows, preserves the common KillChain/ChildAttack/interception/movement contracts and existing shop/Flow boundaries, and links the separate future Main specialization work. Retirement R3 must refresh current coverage after actual verification; historic evidence is retained. No retirement or specialization Phase is authorized or implemented by this addition.

**Preservation / exit:** `F1-start.json` and `F1-verification.json` in the existing ignored evidence workspace capture source/consumer hashes and document-only scope. Core source and the nine inspected View readers are unchanged, X3 XML hashes match, and all unrelated pre-existing dirty files are preserved. This plan and the new handoff are the only repository-visible edits. All original F1 preparation items are complete. **Handoff prepared; external communication not delivered.** Stop at F1. F2, production activation, View/Flow implementation and joint PlayMode/manual acceptance remain pending; no external messages, commit or push.

## 11.8 Current coverage amendment after retirement R3 (2026-10-29)

The [R0–R3 retirement handoff](../handoffs/2026-10-29-legacy-enhancement-retirement-handoff.md) is the current retirement source/evidence basis, superseding only current producer/status claims from the older F1 checkpoint. Current producers are all22 new scoped traits plus PiercingFalloff, Knockback, DistanceDamageScale, KillChainAttack, Roar, Intimidation, ImpactBolt, Pull and HuntersMark (31 total). DoorBreaching, ContemptForWeak, PrecisionShot, DoubleShot, LuckyBullet and ChainExplosion are Retired with serialized numbers reserved; five independent deferred registrations remain unchanged. Native KillChain ChildAttack/interception is retained; FollowUpAttack80 has no current producer.

Fresh sequential gates are Core4089/4089 and CoreIntegration344/345 with zero skips. The sole missing StageKey2101 failure has exactly the same fullname/message and catalog cause as retirement R0. The final obligation ledger resolves removed/renamed cases against current passing evidence. Source manifests, exact XML hashes, current31 producer table, dormant shop reservation and prepared View cleanup request are linked in that handoff.

Read-only coordination found the owning trait/F1 chat idle with its last turn completed, and no other active Project_L writer; the owner documents were frozen/hashed before this narrow amendment. No external message, owner approval or recipient acceptance was inferred. Sections11.6–11.7, including the original37-producer matrix and actual historical results, are preserved verbatim. View-owned two-row cleanup and valid new SO/pool content remain pending. The five new Heavy Core implementations are present; temporary empty Heavy steps/pool do not remove ordinary stat growth. [Main specialization](2026-10-29-main-specialization-plan.md) and retirement R4 costs are later separate work. No commit/push.
