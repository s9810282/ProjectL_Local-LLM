# Player Active Skills Core Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans as the single model owner of the authorized Phase. That owner implements, self-reviews, tests, fixes, and reports the Phase. Do not add an automatic second-model review loop. Steps use checkbox syntax for tracking. Stop after each Phase and report; do not continue automatically.

**Goal:** Implement the Main-owned active skills, their upgrades and complete Core forecasts, and a concrete Buffer-based API that View can consume without calculating game rules.

**Architecture:** Extend the existing combat command/preview path and preserve its Job, source identity, transition, Buffer validation, and result-acknowledgment contracts. Powder Blossom owns its burst rules and Magic Bullet owns its segment rules; both reuse existing damage, effects, health evidence, and forced movement. Forecast and execution consume the same complete transient calculation, with public information policy handled explicitly.

**Tech Stack:** Unity 6000.0.73f1; existing C# Contracts/Domain/Application/Systems assemblies; ScriptableObject -> pure Data authoring; NUnit EditMode Core/CoreIntegration tests. No new packages.

**Spec:** [Player Active Skills Core Design](../specs/2026-11-15-player-active-skills-core-design.md).

**Status:** Phase execution is recorded in §6. The user confirmed DV/DH/DK on 2026-11-15. Each Phase requires its own user authorization.

## Global Constraints

- “The selected Main unit determines its one active skill.” Sub composition does not grant skills.
- “Each skill's initial stage limit is 1.” The limit is authored per skill, with one initial cast per player-side turn.
- “Casting neither consumes nor restores ordinary movement/attack permission.”
- “Bind permits casting. Disarm and Stun prohibit casting.”
- “A valid cast with no targets still executes and consumes the use.”
- “Automatic reactivation, including all child consequences of that cast, consumes no additional skill uses.”
- “Upgrade branches are mutually exclusive.” They persist for the Run, are not interchangeable, and may be applied only outside an active prepared battle.
- Preserve confirmed masks: 8, 20, 36 cells excluding center; base/Bloom/Condemnation damage 50%; Magic Bullet 100%; Judgment 150% and two-cell incoming-direction knockback.
- “Encounter index and remaining budget persist across all bends of the same projectile.” Allies and attackable structures each count once per distinct entity.
- “Unlike ordinary piercing attacks, Base and Distortion pass through an attackable structure even when it survives.”
- “Current public attack previews omit hidden environment facts and some consequences.” Do not silently change legacy previews or describe a partial skill simulation as complete.
- Public skill forecasts preserve hidden information and display prediction limits. Common damage/contact/kill/defense/reflection effects apply; ordinary-attack movement/aim transformations and ordinary-action-completion-only effects do not.
- Judgment's incoming direction and requested distance of two cells override competing displacement traits. Existing immunity, blockers, terrain reactions and interruptions remain effective.
- “A projectile path is not character movement.” A terrain response does not change the command identity or the actual movement agency.
- “Do not copy HP, occupancy, traits, random state, or terrain into a new persistent skill store.”
- Preserve Framework/Buffer/Job ownership, existing Core paths, assembly boundaries, and typed result evidence. Do not fix the illustrative dust/AI incident or perform a general AI refactor.
- Documents are English; code/XML comments are Korean. C# uses existing namespace paths, Allman braces, explicit access, UTF-8/LF, and companion .meta files. Keep GUIDs on moves.
- Only Core/CoreIntegration tests are planned. No View tests, View implementation, combat View MSBuild, or scene edits. Do not alter another author's failing test without required agreement.
- Use test-owned fixtures/catalogs. Retain required broad Core/CoreIntegration regression checks; locality does not remove them.
- Run Unity only through the AppData-enabled approved execution route. Do not delete CurlRequestCache.db or upgrade Unity to bypass sandbox restrictions.
- The user commits and pushes. No task contains a commit/push step. Existing unrelated modifications must be preserved.

## Review Focus

1. A growth/specialization/copy operation must not erase an upgrade or refresh a cast's budget: pinned by A1 and F1 tests.
2. A canceled/replaced/stale forecast or repeated submission must neither reroll RNG nor charge twice: pinned by E1 and B2 tests.
3. Several burst kills and later KillChainAttack deaths must not replay old kill seeds or consume extra uses: pinned by C1 tests.
4. A bent projectile hitting a multi-cell ally/structure at a bend must count one encounter and report the actual incoming direction: pinned by D1/D2 tests.
5. A forced displacement through terrain/effects must keep its movement/damage meaning and publish valid health/undo/completion evidence: pinned by B2, D2, and G1 integration tests.

---

## 1. Execution gates and Phase order

### 1.1 Confirmed decision gates

The user explicitly answered all three material questions on 2026-11-15. These are fixed requirements for the dependent tasks, not questions to ask again in another session. Source classification, implementation and tests remain unstarted.

| Gate | Design reference | Dependent task | Confirmed requirement |
|---|---|---|---|
| DV — public visibility | D12 / Q1 / section 7.3 | E1 public forecast projection and its acceptance assertions | Resolved: preserve hidden information and expose prediction limits; public known-information forecasts remain conditional without leaking secret presence. Internal full-information parity is still required. |
| DH — hook eligibility | D13 / Q3 / section 6.4 | B1 skill damage-family hook changes | Resolved: apply common damage/contact/kill/defense/reflection effects with their conditions; exclude ordinary-attack movement/aim transformations and ordinary-action-completion-only effects. |
| DK — Judgment displacement | M15 / Q3 / section 8.3 | D2 Judgment composition | Resolved: one incoming-direction two-cell request overrides competing displacement traits; immunity, blockers and terrain reactions remain common. |

Answers are recorded in the spec and this table. Do not mark dependent tasks complete merely because their decisions are resolved. Upgrade acquisition economy remains intentionally out of scope (Q2). Production Sniper role/configuration is an activation audit (Q4), not permission to weaken Main validation.

### 1.2 Phase summary

| Phase | Deliverable | Dependencies / review boundary |
|---|---|---|
| 0 | Current baseline, source audit, decision ledger, owned-file inventory | No implementation; report and stop. |
| A | Typed contracts, skill definition facts, owned upgrade/battle-use preservation, Stun admission/lifetime | Approved contract/state choices; report and stop. |
| B | Shared complete-simulation/effect seams and skill evidence/publication invariants | A; apply confirmed DH; report and stop. |
| C | Powder Blossom, Bloom, Condemnation through one cast calculation | A/B; report and stop. |
| D | Magic Bullet, Distortion, Judgment with candidate/execution geometry agreement | A/B; apply confirmed DK in D2; report and stop. |
| E | Full skill forecast and View request/response/execute API | C/D; apply confirmed DV; report and stop. |
| F | Upgrade operation, battle/Run wiring, DB catalog and production activation audit | A–E; report and stop. |
| G | Required broad verification, publication scenarios, View handoff | F; report and stop with Core/Unity/View limits separated. |

C and D own separate skill rules, but shared B files must not be edited concurrently without an explicit ownership arrangement. No parallel-agent execution is implied by this plan.

A Phase is complete only when its own tests and mandatory affected checks have evidence, its deliverable is internally coherent, and the Phase report names any baseline failures or unverified conditions. A dependent Phase does not start just because code for the previous one exists.

## 2. File map and responsibility boundaries

Paths in tasks use the following exact repository-root aliases:

| Alias | Root |
|---|---|
| C | Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator |
| S | Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CombatSystem |
| R | Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CharacterSystem |
| B | Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems |
| DB | Assets/ProjectL/Script/Systems/DataBaseSystem |
| F | Assets/ProjectL/Script/Systems/Factory |
| T | Assets/Tests/EditMode |
| P | docs/superpowers |

Every newly created C#/SO/table asset under Assets also receives a .meta file. Do not create extra files merely to make this table longer; the concrete split below groups values that change together.

| New unit | Responsibility |
|---|---|
| C/Contracts/PlayerSkills/PlayerSkillDefinition.cs | Skill/upgrade keys, validated immutable definition, fixed Main binding and stage limit. |
| C/Contracts/PlayerSkills/PlayerSkillState.cs | Battle-bound identity/definition and usage facts; source stamp and one-use settlement. |
| C/Contracts/PlayerSkills/PlayerSkillRequest.cs | Immutable ordered point intent and typed rejection codes. |
| C/Contracts/PlayerSkills/PlayerSkillQueryResults.cs | Availability, selection, forecast, coverage payloads. |
| C/Contracts/PlayerSkills/PlayerSkillResult.cs | Skill attack/cast result using existing hit, trait, status, and movement records. |
| C/Contracts/PlayerSkills/PlayerSkillUpgradeResult.cs | Upgrade query/application request/results for Run-owned Main identity. |
| C/Application/Combat/PlayerSkills/PlayerSkillCalculator.cs | Admission, dispatch, one use settlement, complete cast transition. |
| C/Application/Combat/PlayerSkills/PlayerSkillQueries.cs | Availability/candidates/forecast conversion; calls shared validation and calculation. |
| C/Application/Combat/PlayerSkills/PowderBlossomCalculator.cs | Masks, per-burst direct-kill qualification, child-priority sequence, expansion and Stun delivery. |
| C/Application/Combat/PlayerSkills/MagicBulletCalculator.cs | Point validation/candidates, bent traversal, encounter budget, incoming hit direction and Judgment. |
| C/Application/Roster/PlayerSkillUpgradeRules.cs | Pure None -> one branch transition; no payment/reward policy. |
| R/CharacterRosterService.PlayerSkills.cs | Existing service's DB/Buffer adaptation for upgrade availability/application. |
| B/Core/PlayerSkillUpgradeTransactions.cs | Existing prepare/install ownership and source checks for upgrade application; no separate persistent store. |
| DB/Data/InGame/Table/PlayerSkill/Item/PlayerActiveSkillData.cs and PlayerActiveSkillDataSO.cs | Pure authored row and SO input. |
| DB/Data/InGame/Table/PlayerSkill/PlayerActiveSkillDataTableSO.cs; DB/Queries/InGameDataBase/InGameTable/PlayerActiveSkillTable.cs | Existing typed table/query pattern. |

Existing files to extend are named in each task. Do not invent a replacement shared attack engine, a second generic Context, a universal event bus, or a new skill Job/Runner.

## 3. Shared contract names

All names below are planned additions. Match the design's public surface; if a signature needs a material change, update the spec and plan before implementation.

- PlayerSkillKey: None, PowderBlossom, MagicBullet.
- PlayerSkillUpgradeKey: None, Bloom, Condemnation, Distortion, Judgment.
- PlayerSkillDefinition: Key, MainCharacterKey, StageUseLimit; stage limit positive, default authored value 1; valid branches derived from skill kind.
- PlayerSkillBattleState: owning Main instance/entity, bound definition/upgrade, RemainingUses, LastUsedControllerTurnSequence. Provide WithUsage(int remainingUses, long lastUsedSequence) and SameValues(PlayerSkillBattleState other).
- PlayerSkillSourceStamp: BattleInstanceId, ControllerTurnSequence, QueryRevision, ActorId; bundles existing values, owns no clock.
- PlayerSkillRequest: immutable Points and optional expected source for executable requests. Actor identity remains the enclosing request's actor; reject disagreement rather than trusting two sources.
- PlayerSkillUseSettlement: owning actor, expected/final remaining uses, expected/final last-used sequence, current controller sequence. Never substitutes for UnitActionUseSettlementSnapshot.
- PlayerSkillAvailability / PlayerSkillSelectionResult / PlayerSkillForecast: fields defined in spec section 9.1a, each with Source and typed failure.
- PlayerSkillAttackResult: shared AttackHitResult/damage/knockback/trait facts with skill/burst/segment metadata; no ordinary-action settlement.
- PlayerSkillResult: ordered skill attack/child occurrences and one use settlement. Existing CombatResult remains the outer command/result identity.
- CombatPlayerSkillAttackEvent: actual primary burst/projectile occurrence, recognized by health/evidence validators. Existing extra-attack events remain extra attacks.

Pure Core entry points:

    PlayerSkillAvailability PlayerSkillQueries.Availability(
        CombatCommandInput input, PlayerSkillSourceStamp source)
    PlayerSkillSelectionResult PlayerSkillQueries.Selection(
        CombatCommandInput input, PlayerSkillSourceStamp source)
    PlayerSkillForecast PlayerSkillQueries.Forecast(
        CombatCommandInput input, IReadOnlyList<IMovementTileRule> rules,
        PlayerSkillSourceStamp source)
    CombatCommandTransition PlayerSkillCalculator.Calculate(
        CombatCommandInput input, IReadOnlyList<IMovementTileRule> rules)

CombatCommandInput gains the immutable PlayerSkillRequest and captured skill source facts. Preserve them through every WithActor/WithIntent/WithWorld/WithPersistentFacts/copy route. The assembler captures actual query revision; View supplies only an expected stamp. Core compares them, and Buffer rechecks at publication.

Skill-owned internal entry points:

    CombatCommandTransition PowderBlossomCalculator.Calculate(
        CombatCommandInput input, IReadOnlyList<IMovementTileRule> rules)
    CombatCommandTransition MagicBulletCalculator.Calculate(
        CombatCommandInput input, IReadOnlyList<IMovementTileRule> rules)
    PlayerSkillSelectionResult MagicBulletCalculator.Selection(
        CombatCommandInput input, PlayerSkillSourceStamp source)

The common dispatcher owns top-level admission and use settlement. Skill calculators receive already-validated intent but still use the same geometry/definition rules for runtime continuation. Return one complete transient transition; do not install intermediate children.

Systems input factories and upgrade service signatures are specified in Task E1/F1. Enumerated numeric IDs are append-only and allocated after the Phase 0 collision audit; do not assume an integer is free from a historical report.

## 4. Verification commands and evidence rules

The Editor executable was checked during this planning task at:

    D:/Program Files/Unity/Hub/Editor/6000.0.73f1/Editor/Unity.exe

Recheck before execution. Core test assemblies are ProjectL.Tests.Core and ProjectL.Tests.CoreIntegration. No Unity process was started for this plan.

For each task, run its listed fixture filter using this PowerShell command template, through an approved AppData-enabled execution call. Each run gets a new evidence directory; do not overwrite a previous red/baseline result.

    $unityEditor = 'D:/Program Files/Unity/Hub/Editor/6000.0.73f1/Editor/Unity.exe'
    $projectPath = 'D:/My_ProjectWork/Github_desktop/Project_L'
    $testFilter = 'ProjectL.Tests.Core.PlayerSkillContractTests'
    $evidencePath = Join-Path $projectPath 'Library/CodexTests/PlayerActiveSkills/phase-a-contracts-red'
    New-Item -ItemType Directory -Force -Path $evidencePath | Out-Null
    $resultPath = Join-Path $evidencePath 'results.xml'
    $logPath = Join-Path $evidencePath 'unity.log'
    & $unityEditor -batchmode -nographics -projectPath $projectPath -runTests -testPlatform EditMode -testFilter $testFilter -testResults $resultPath -logFile $logPath

Do not add -quit. Use asynchronous launch/polling as needed for progress updates. A full run replaces -testFilter with:

    -assemblyNames 'ProjectL.Tests.Core;ProjectL.Tests.CoreIntegration'

**RED:** new meaningful assertions fail for the intended missing behavior; a sandbox/AppData/runner failure is not RED evidence. Establish the smallest compilable contract when necessary, then confirm behavioral failures before completing implementation.

**GREEN:** fresh XML contains the expected executed cases, no unexpected failures, and the log has no compilation/runner failure. A zero exit code or zero discovered tests is not a pass.

**Baseline:** record failures by case name and failure reason from the current run. Historic counts in other plans are not this task's baseline. Do not mask or fix unrelated failures under this feature.

**Per Phase:** focused fixtures plus every affected shared-contract suite listed in the task; broaden only for a new concern or the mandatory Phase 0/G broad runs. Test discovery and public API/Buffer fixtures matter as much as pure calculators.

---

## Phase 0 — Revalidate scope, decisions, and baseline

### Task 0.1: Establish an executable starting point

**Files:** Read AGENTS.md, Assets/AGENTS.md, the spec, its source references, current Framework guides, and affected working-tree diffs. Update only this plan's execution ledger and approved spec decisions.

**Interfaces:** Consumes current source and confirmed DV/DH/DK decisions. Produces source/ownership inventory, a mapping of those decisions to affected contracts, and fresh baseline evidence.

- [x] **Step 1 — Capture source:** record branch, HEAD, dirty files, Unity path/version, and active Editor ownership. Preserve in-progress attack/damage/piercing changes; do not reset, clean, or overwrite them.
- [x] **Step 2 — Verify decision mapping:** read the confirmed DH/DK/DV requirements and map them to B1/D2/E1 without asking the user to decide them again. Keep acquisition economy deferred. Preserve the remaining design-review boundaries; a planning request or policy answer is not implementation authorization.
- [x] **Step 3 — Audit actual consumers:** enumerate new enum IDs, command/preview switches, input/state copy constructors, health/evidence validators, damage-kind predicates and their intended behavior, upgrade record copy sites, and battle/Run reset sites. Record exact affected paths rather than adding global catch-all branches.
- [x] **Step 4 — Run baseline:** use the full Core/CoreIntegration assembly command in section 4; record selected/passed/failed/skipped counts and case-level failures. If infrastructure prevents execution, mark that gate blocked instead of claiming baseline parity.
- [x] **Step 5 — Report and stop:** state confirmed scope/decisions, any newly discovered blockers, baseline, shared-file risks, and the proposed Phase A deliverable.

**Exit:** no feature code; baseline is explicit, execution ownership is clear, and later gates cannot be mistaken for answered questions.

## Phase A — Contracts, state preservation, and restrictions

### Task A1: Define authored skill identity and immutable state

**Create:** all C/Contracts/PlayerSkills files in section 2; DB PlayerSkill files in section 2; T/PlayerSkillContractTests.cs; T/CoreIntegration/PlayerSkillAuthoringTests.cs.

**Modify:** C/Contracts/Roster/OwnedCharacterRecord.cs; C/Contracts/Combat/CombatCommandState.cs and CombatCommandInput.cs; C/Contracts/Combat/BattlePreparation.cs; existing owned-record copy sites found in 0.1. Do not enable production table registration yet.

**Interfaces:** Produces section 3 contracts; OwnedCharacterRecord.PlayerSkillUpgrade with a preserving WithPlayerSkillUpgrade method; CombatCommandState.PlayerSkill; immutable battle-bound definition. No cast implementation.

- [x] **Step 1 — Add contract RED cases:** PlayerSkillContractTests asserts request arrays are copied; wrong branch/kind combinations and nonpositive use limits reject; default upgrade is None; current state equality detects remaining-use/last-sequence changes; With/copy paths preserve upgrade and usage.
- [x] **Step 2 — Add authoring RED cases:** PlayerSkillAuthoringTests uses a test-owned table with Akimbo/PowderBlossom/1 and Sniper/MagicBullet/1. Assert duplicate skill keys/Main bindings, unsupported branches, invalid limits, and invalid owner roles are diagnosed; GetData returns fresh pure data.
- [x] **Step 3 — Run RED:** filter ProjectL.Tests.Core.PlayerSkillContractTests;ProjectL.Tests.CoreIntegration.PlayerSkillAuthoringTests.
- [x] **Step 4 — Implement the contracts/table:** stage limits belong to the skill definition, upgrades to the owned Main, usage to battle state. Update every relevant copy/equality/composition path; do not use a default optional field that erases state in another action.
- [x] **Step 5 — Run GREEN and copy regression:** rerun the new fixtures plus CombatCommandInputCopyTests, CharacterRosterContractTests and relevant MainSpecialization growth/contract fixtures. Record any unchanged baseline failures.

### Task A2: Add real Stun and skill admission

**Create:** C/Application/Combat/PlayerSkills/PlayerSkillQueries.cs; T/PlayerSkillAdmissionTests.cs; T/PlayerSkillStunTests.cs.

**Modify:** C/Contracts/CombatEffects/StatusEffectKey.cs, AppliedStatus.cs, CharacterStatusCompatibility.cs; C/Domain/CombatEffects/Rules/AppliedStatusRules.cs; C/Application/Combat/CombatActionAdmission.cs and StatusEffectReadModel.cs. Extend existing lifecycle/read projections where the new restriction otherwise disappears.

**Interfaces:** Produces PlayerSkillQueries.Availability(input, source) and a Stun ActionRestrictionStatus. Reuses affected-side duration semantics and source identity.

- [x] **Step 1 — Add admission RED cases:** Availability returns true after ordinary attack and under Bind; false under Disarm or Stun, enemy/deployment phase, absent/dead/non-field actor, wrong selected Main, no skill, spent stage budget, or current-turn use. Assert zero mutation and distinct failure reasons.
- [x] **Step 2 — Add Stun RED cases:** one affected-side end expires duration 1; the opposite side's end does not; repeated 1 stays 1; existing duration 2 plus incoming 1 stays 2. Movement, ordinary attack and skill admission all prohibit Stun; existing death/reserve cleanup preserves its established semantics.
- [x] **Step 3 — Run RED:** filter ProjectL.Tests.Core.PlayerSkillAdmissionTests;ProjectL.Tests.Core.PlayerSkillStunTests.
- [x] **Step 4 — Implement restriction/admission:** append a Stun key, extend allowed restriction validation and read facts, and calculate skill availability from actual source facts. Do not implement Stun as unrelated Bind/Disarm instances or reuse ordinary action exhaustion.
- [x] **Step 5 — Run GREEN:** new fixtures plus AppliedRestrictionStatusTests and existing action-availability/ordinary move/attack checks selected from the current suite.

**Phase A exit:** immutable contracts and restrictions work in fixtures; all copy paths preserve the new values; no skill is available through production input yet. Report actual blockers, tests, and modified shared contracts; stop.

## Phase B — Complete simulation and valid result evidence

### Task B1: Make shared attack effects usable by a complete skill calculation

**Decision:** DH is confirmed. Complete the source hook matrix under that policy; do not treat an old OrdinaryAttack-only predicate as proof that a common defensive/contact effect must exclude skills.

**Modify:** C/Contracts/Combat/CombatDamageKind.cs; C/Application/Combat/CommandAttackPlan.cs, CommandAttackProjection.cs, AttackDeliveryExecutor.cs, AttackDirectDamageCalculator.cs, CombatDamageCalculator.cs, CombatDamageApplication.cs, KillChainAttackCalculator.cs, TraitAttackNotifications.cs; the exact trait/item predicates approved in DH (spec section 6.4). Add a narrow shared damage-family predicate beside CombatDamageKind only if it expresses actual shared rules.

**Create:** T/PlayerSkillEffectIntegrationTests.cs; T/PlayerSkillSimulationPolicyTests.cs.

**Interfaces:** Produces an explicit complete-consequence calculation option in the existing plan/executor and a skill damage family. Legacy callers retain existing defaults. KillChainAttack gains an optional explicit initial seed set using existing EntityId, e.g. initialKilledEnemyIds, while ordinary callers retain their existing seed derivation.

- [x] **Step 1 — Add effect RED cases from DH:** parameterize each approved hook with ordinary/extra/reaction/skill/environment kinds. Assert the exact approved inclusion/exclusion and unchanged existing kinds; include dust mitigation, contact status, sacrifice/redirection, reflection and ordinary-action-only effects where applicable.
- [x] **Step 2 — Add complete-simulation RED cases:** run the existing shared stages on isolated facts and assert KillChainAttack, relevant death effects, local consumable use and RNG progression are simulated without installation. Assert legacy public-preview calls retain their documented omissions.
- [x] **Step 3 — Run RED:** filter ProjectL.Tests.Core.PlayerSkillEffectIntegrationTests;ProjectL.Tests.Core.PlayerSkillSimulationPolicyTests.
- [x] **Step 4 — Implement only required seams:** separate “calculate complete consequences,” “observer visibility,” and “install result.” Add explicit per-scope kill seeds and preserve old callers. Do not flip PublicPreview globally or introduce a second effect/terrain engine.
- [x] **Step 5 — Run GREEN and shared regressions:** new fixtures; KillChainAttackTraitTests; KillChainSacrificeRecipientRegressionTests; CombatCommandEffectTests; CombatItemPreviewTests; AttackPreviewParityTests; affected DH trait fixtures. Inspect differences rather than recapturing golden results.

### Task B2: Publish skill evidence without impersonating ordinary attacks

**Create:** T/PlayerSkillEvidenceTests.cs; T/CoreIntegration/PlayerSkillPublicationTests.cs.

**Modify:** C/Contracts/Combat/CombatEvent.cs, CombatCommandTransition.cs, CombatResult.cs, CombatHealthClaim.cs, CombatHealthEvidenceValidator.cs, CombatChangeEvidenceReader.cs; C/Application/Combat/AttackResultFactory.cs and MovementUndoPolicy.cs; B/Combat/CombatStateBuffer.cs; B/Core/BattleTransactions.cs. Extend result contracts from A1.

**Interfaces:** Produces CombatPlayerSkillAttackEvent, PlayerSkillAttackResult, PlayerSkillResult and PlayerSkillUseSettlement publication support. The Buffer accepts valid skill transitions, rejects malformed evidence, and retains its existing atomic prepare/install sequence.

- [x] **Step 1 — Add evidence RED cases:** a skill occurrence can own existing health steps/claims and forced-movement facts; an event reference from another occurrence rejects; duplicate HP application/claims reject; a projectile path is never a character move.
- [x] **Step 2 — Add publication RED cases:** a valid synthetic fixture transition reduces remaining uses by exactly one and records current turn use, preserves actor ActionUse, publishes one sequence, and passes health/change/undo validation. Rejected or stale transitions change no gameplay/RNG; repeated command number cannot charge twice.
- [x] **Step 3 — Run RED:** filter ProjectL.Tests.Core.PlayerSkillEvidenceTests;ProjectL.Tests.CoreIntegration.PlayerSkillPublicationTests.
- [x] **Step 4 — Extend exact constructors/readers/validators:** use a dedicated skill attack result because existing AttackResult requires ordinary/reaction settlement. Do not set reaction=true, forge Attacked, drop health claims, suppress validation exceptions, or repair live state after a failed publish.
- [x] **Step 5 — Run GREEN:** new fixtures plus CombatActionUsePublicationTests, CombatCommandPublicationTests, CombatRewardPublicationTests and CombatCompletionPublicationTests in CoreIntegration. Verify normal move/attack invariants remain strict.

**Phase B exit:** shared effect semantics and evidence are usable by skill code, but no incomplete skill is activated. Report DH decisions and the actual shared regression scope; stop.

## Phase C — Powder Blossom and its two upgrades

### Task C1: Implement one complete burst chain

**Create:** C/Application/Combat/PlayerSkills/PlayerSkillCalculator.cs and PowderBlossomCalculator.cs; T/PowderBlossomSkillTests.cs; T/PowderBlossomParityTests.cs. Extend T/CoreIntegration/PlayerSkillPublicationTests.cs created by this task's workstream.

**Modify:** C/Application/Combat/CombatCommandCalculator.cs for explicit UsePlayerSkill dispatch/final reconciliation; the skill-owned and shared contracts produced by A/B. Do not wire View input until E.

**Interfaces:** Produces PlayerSkillCalculator.Calculate and PowderBlossomCalculator.Calculate from section 3. The complete transition includes all bursts/children and one use settlement.

- [x] **Step 1 — Add rule RED cases:** exact masks contain 8/20/36 non-center cells; edge clipping; allies excluded; walls do not shield targets; a multi-cell structure is hit once per burst; damage uses 50% at the approved common-damage boundary; empty valid cast consumes one use.
- [x] **Step 2 — Add chain/upgrade RED cases:** several direct enemy kills schedule one burst; KillChainAttack resolves first and its kills add no Powder Blossom bursts; later bursts do not recount old deaths; Bloom grows at most twice and resets next cast; Condemnation starts at 20 cells, adds Stun, permits approved traits and never repeats.
- [x] **Step 3 — Add parity/publication RED cases:** same immutable input and RNG produce equal complete facts/evidence for forecast computation and execution; current attack changes between bursts take effect at existing boundaries; actor/terminal interruption still settles one use; publish the resulting real transition through B2.
- [x] **Step 4 — Run RED, then implement:** filter ProjectL.Tests.Core.PowderBlossomSkillTests;ProjectL.Tests.Core.PowderBlossomParityTests;ProjectL.Tests.CoreIntegration.PlayerSkillPublicationTests. Use the per-burst direct-cause seed and shared projected world; finalize/publish only at the outer command.
- [x] **Step 5 — Run GREEN:** new fixtures plus KillChainAttackTraitTests, AppliedRestrictionStatusTests, PlayerSkillEvidenceTests and affected damage/death fixtures. Validate named causal order rather than comparing only final HP.

**Phase C exit:** all Powder Blossom variants work with test-owned definitions and complete internal parity. No arbitrary chain cap, duplicate damage pipeline, live preview mutation, or production UI claim. Report and stop.

## Phase D — Magic Bullet selection, traversal, and Judgment

### Task D1: Make candidate selection and execution share geometry

**Create:** C/Application/Combat/PlayerSkills/MagicBulletCalculator.cs; T/MagicBulletSelectionTests.cs; T/MagicBulletTraversalTests.cs.

**Modify:** C/Application/Combat/PlayerSkills/PlayerSkillQueries.cs for selection dispatch. Reuse existing CellCoord, wall geometry and attack-range evaluation; do not globally modify ordinary ray stopping.

**Interfaces:** Produces MagicBulletCalculator.Selection(input, source). The same internal point validator feeds MagicBulletCalculator.Calculate in D2.

- [ ] **Step 1 — Add selection RED cases:** for all four initial cardinal directions, A uses the cross, B is perpendicular through A, C is perpendicular through B; each distance uses current caster attack range. Empty/ally/enemy-occupied points are valid; zero-length, extra/missing points, walls, blocked segments, and board overflow reject.
- [ ] **Step 2 — Add traversal RED cases:** base bends only at A, Distortion at A/B; final point is not a bend/end; count a bend cell once; global entity deduplication across multi-cell contacts; selection prefix never damages or consumes anything.
- [ ] **Step 3 — Run RED:** filter ProjectL.Tests.Core.MagicBulletSelectionTests;ProjectL.Tests.Core.MagicBulletTraversalTests.
- [ ] **Step 4 — Implement skill-owned geometry:** represent ordered segments and per-contact incoming direction, retaining existing wall facts. Do not rotate the caster, simulate movement along the ray, or let an ordinary height exemption bypass the skill's clear-segment rule.
- [ ] **Step 5 — Run GREEN:** new fixtures plus current targeting/wall and PiercingShotTraceTests. Prove existing ordinary structure stopping and straight-shot geometry are unchanged.

### Task D2: Resolve cumulative piercing and Judgment through shared damage/movement

**Decision:** DK is confirmed: Judgment's incoming direction and two-cell request take precedence; common immunity, blockers and terrain reactions still apply.

**Create:** T/MagicBulletSkillTests.cs; T/JudgmentInteractionTests.cs; T/MagicBulletParityTests.cs.

**Modify:** C/Application/Combat/PlayerSkills/MagicBulletCalculator.cs and PlayerSkillCalculator.cs; C/Application/Combat/AttackKnockbackCalculator.cs / AttackForcedMovement.cs only for an explicit caller-supplied incoming direction/displacement policy that preserves ordinary defaults. Use existing falloff and damage calculators.

**Interfaces:** Produces MagicBulletCalculator.Calculate(input, rules) and the final Magic Bullet branch of PlayerSkillCalculator.Calculate.

- [ ] **Step 1 — Add piercing RED cases:** a fixture with attack 100, zero defense and neutral matchup gives 100/75/50/25 across four encountered targets for 25% falloff. Include an ally and a surviving structure, preserve encounter count across A/B, and stop at budget. Verify earlier rounded-zero budget and wall/board stops via existing falloff rules.
- [ ] **Step 2 — Add Judgment RED cases:** first eligible target only, 150% before other mitigation, physical-defense-only bypass and first-hit termination after death/displacement. With a longer-distance or impact-center trait, still request exactly two cells along the incoming segment; do not sum, choose the longer distance, reject incompatible modes, or move twice. Immunity and wall/unit/edge blockers may reduce actual travel; terrain reactions keep their existing interruptions.
- [ ] **Step 3 — Add interaction/parity RED cases:** knockback traverses fixture dust/thorns/weak ground/mine/tornado where applicable; preserve event agency, actual recipient, stop reason, local RNG, terminal state and health claims. Include structure explosion plus subsequent hits to expose stale-world or double-hit errors.
- [ ] **Step 4 — Run RED, then implement:** filter ProjectL.Tests.Core.MagicBulletSkillTests;ProjectL.Tests.Core.JudgmentInteractionTests;ProjectL.Tests.Core.MagicBulletParityTests. Reuse falloff/target-budget computation once per projectile; use one global encountered set; run existing one-cell forced movement rather than endpoint teleportation.
- [ ] **Step 5 — Run GREEN and shared regressions:** new fixtures plus PiercingShotCommandTests, PiercingFalloffTraitTests, ForcedDisplacementTests, ForcedDisplacementPathTests, EnvironmentTerrainRuleTests and affected publication fixtures. Ordinary piercing must still stop at structures.

**Phase D exit:** both Magic Bullet variants and Judgment have exact internal parity and valid evidence; DK is explicit. Report and stop.

## Phase E — Full forecast and View request API

### Task E1: Expose the complete calculation through existing services and Buffers

**Decision:** DV is confirmed: preserve secrecy and publish explicit known-information forecast limits.

**Create:** T/PlayerSkillForecastTests.cs; T/CoreIntegration/PlayerSkillTransportTests.cs; T/CoreIntegration/PlayerSkillPreviewPublicationTests.cs.

**Modify:** C/Application/Combat/PlayerSkills/PlayerSkillQueries.cs; C/Contracts/Combat/CombatPreviewResult.cs; B/CombatView/CombatViewCommand.cs, CombatViewCommandKind.cs, CombatViewPreview.cs, CombatViewPreviewKind.cs; S/CombatActionRequest.cs, CombatInputAssembler.cs, CombatCommandService.cs, CombatPreviewService.cs; B/Core/BattleTransactions.cs. Inspect existing Combat Plan/Job dispatch for enum assumptions; only necessary routing changes belong here.

**Interfaces:** Add these factories to existing DTO types and retain existing Buffer methods:

    CombatViewPreviewData ForPlayerSkillAvailability(EntityId actorId)
    CombatViewPreviewData ForPlayerSkillSelection(
        EntityId actorId, IReadOnlyList<CellCoord> selectedPoints)
    CombatViewPreviewData ForPlayerSkillForecast(
        EntityId actorId, IReadOnlyList<CellCoord> completePoints)
    CombatViewCommandData ForPlayerSkill(
        EntityId actorId, IReadOnlyList<CellCoord> completePoints,
        PlayerSkillSourceStamp expectedSource)

Produces PlayerSkillQueries.Forecast(input, rules, source), exclusive SkillAvailability/SkillSelection/SkillForecast response arms, and PlayerSkillResult on CombatResult. Request/result numbering, ClearPreview(requestNumber), and presentation acknowledgment remain existing APIs.

- [ ] **Step 1 — Add forecast RED cases:** complete known-world/internal computation includes KillChainAttack, reactivation, Stun, knockback and downstream consequences. Repeated requests preserve original facts/RNG/consumables. An incomplete selection returns no accepted cast; empty-target valid cast predicts consumption.
- [ ] **Step 2 — Add disclosure RED cases from DV:** worlds with equal permitted public facts but differing hidden effects produce indistinguishable public forecasts, including the coverage label, apart from already-permitted opaque source bookkeeping. Withhold/project hidden-dependent HP/path/outcome details; do not simply hide an event while exposing its secret-derived result. Separately assert full-information internal simulation/execution parity.
- [ ] **Step 3 — Add transport RED cases:** request mapping round-trips points/source; old response after replacement/battle/turn/revision change is not installed; ClearPreview on another owner's number does nothing; stale execution rejects without use consumption; unknown preview kinds never fall into placement.
- [ ] **Step 4 — Run RED, then implement:** filter ProjectL.Tests.Core.PlayerSkillForecastTests;ProjectL.Tests.CoreIntegration.PlayerSkillTransportTests;ProjectL.Tests.CoreIntegration.PlayerSkillPreviewPublicationTests. Forecast calls the same complete skill computation and projects its result; execution independently revalidates current facts. Do not accept a View-supplied calculated transition.
- [ ] **Step 5 — Run GREEN and publication regression:** include real service -> assembler -> Core -> Buffer execution/preview cases, same-source semantic comparison, CombatTransitionPublisherTests, CombatCommandPublicationTests, AttackPreviewParityTests and AttackPreviewResumptionTests.

**Phase E exit:** a View caller can query, select, forecast, submit, consume and acknowledge through the existing route; no skill UI is implemented. Report exact failure codes, coverage behavior and stale handling; stop.

## Phase F — Upgrade operation, lifetime wiring, and activation

### Task F1: Persist upgrades and reset only the correct state

**Create:** C/Application/Roster/PlayerSkillUpgradeRules.cs; R/CharacterRosterService.PlayerSkills.cs; B/Core/PlayerSkillUpgradeTransactions.cs; T/PlayerSkillUpgradeTests.cs; T/CoreIntegration/PlayerSkillLifetimeTests.cs.

**Modify:** R/CharacterRosterService.cs interface/export as needed; B/Character/CharacterBuffer.cs; B/Core/ProgressTransactions.cs; S/BattleDefinitionAdapter.cs, DatabaseBattleInitializationSource.cs, BattleEntryService.cs, BattleLifetime.cs / IBattleDefinitionRead.cs where required to bind immutable skill definitions; C/Application/Combat/BattlePreparationCalculator.cs; F/Runtime/InGame/Core/CombatSystemFactory.cs only for real new dependencies.

**Interfaces:**

    PlayerSkillUpgradeAvailability ICharacterRosterService.ReadPlayerSkillUpgradeAvailability()
    PlayerSkillUpgradeApplyResult ICharacterRosterService.TryApplyPlayerSkillUpgrade(
        RunId expectedRunId, long expectedRosterRevision,
        CharacterInstanceId mainId, PlayerSkillUpgradeKey selectedBranch)

The pure rule returns a proposed owned-record change; the existing character service prepares and installs through Buffer ownership. Reuse Main-specialization preparation patterns without copying its unrelated locks/offer policies blindly.

- [x] **Step 1 — Add upgrade RED cases:** None -> one compatible branch succeeds outside battle; repeat/conflicting branch, wrong selected Main, stale Run/roster, no skill or active prepared battle rejects without mutation.
- [x] **Step 2 — Add lifetime coverage:** selected branch survives stage transition and growth/specialization copies; true new Run resets branch; true new battle initializes use limit once; repeated entry and preview do not refresh uses; next player turn restores turn opportunity only. Actual scene rebind remains a Unity playback check.
- [x] **Step 3 — Run RED:** filter ProjectL.Tests.Core.PlayerSkillUpgradeTests;ProjectL.Tests.CoreIntegration.PlayerSkillLifetimeTests.
- [x] **Step 4 — Implement existing-owner wiring:** add preserving record mutations, guarded upgrade preparation, immutable battle binding and reset behavior. Do not introduce a second roster, transferable per-skill global upgrade pool, or live DB reads by View.
- [x] **Step 5 — Run GREEN:** new fixtures plus CharacterRosterContractTests, relevant MainSpecialization tests, current battle preparation and Run-start/reset integration fixtures. Verify all returned IDs/revisions correspond to the installed owner.

### Task F2: Register and activate the authored definitions

**Create:** the new skill table and two row assets under Assets/ProjectL/DataBaseSO/InGameGroup/PlayerSkill/, using the repository's existing table/item naming convention; all .meta files.

**Modify:** F/DBFactory/Group/InGameDataBaseFactory.cs; DB/Queries/InGameDataBase/InGameDataBase.cs; active InGame group/catalog references located in 0.1; the intended Sniper unit definition only as required by its approved Main role. Extend T/CoreIntegration/PlayerSkillAuthoringTests.cs.

**Interfaces:** Existing InGame query context exposes the typed PlayerActiveSkillTable query. Definition binding resolves the selected Main's key; it never consults Sub membership to grant a skill.

- [x] **Step 1 — Add activation RED cases:** test-owned catalog resolves Akimbo/PowderBlossom/1 and Sniper/MagicBullet/1; missing mapping reports no skill, malformed registered definitions fail with source identity, and duplicate Main mapping cannot boot silently.
- [x] **Step 2 — Audit production Main data:** found the intended Regular Sniper in the unit table, with the Main category, PlayerOnly eligibility and one PiercingFalloff entry. No production loadout or unit roster referenced the Sniper asset.
- [x] **Step 3 — Run RED, then register:** filter ProjectL.Tests.CoreIntegration.PlayerSkillAuthoringTests. Add the table/query/context/factory/catalog chain in its existing local builders; write the two skill rows; apply only the audited production corrections.
- [x] **Step 4 — Run GREEN and asset validation:** all new authoring cases passed. DataBaseAssetTests retained eight Phase 0 failures with the same case names and messages. The source audit found no incidental asset or View changes.
- [x] **Step 5 — Report activation:** test-owned authoring, the active database/catalog and both operational Main bindings were verified in EditMode. Playback remains outside this Phase.

**Phase F exit:** Run/battle lifetimes and out-of-battle upgrade API work through actual owners; database activation is explicit. No economy acquisition implementation. Report and stop.

## Phase G — Final regression and View handoff

### Task G1: Verify the full boundary and document the actual API

**Create:** P/handoffs/2026-11-15-player-active-skills-core-view-handoff.md; T/CoreIntegration/PlayerSkillEndToEndTests.cs. Extend this plan's evidence ledger and the spec's verification status without rewriting approved rules.

**Interfaces:** Uses every public contract implemented by A–F. Produces actual signatures, complete examples, event schemas, known limitations, and evidence paths. No View code.

- [ ] **Step 1 — Add end-to-end cases:** from test-owned preparation, query -> candidates -> complete forecast -> command -> Buffer publication -> result acknowledgment for all six forms. Compare canonical gameplay facts, ordered semantic evidence, RNG, status, recipients, use settlement, movement and terminal outcome.
- [ ] **Step 2 — Run focused GREEN:** PlayerSkillEndToEndTests and all new fixtures; verify a rejection still completes command bookkeeping while preserving gameplay. Include unrelated pending preview cancellation and an early terminal chain.
- [ ] **Step 3 — Run mandatory broad Core/CoreIntegration:** use assembly selection in section 4. Compare exact failures against Phase 0. Do not normalize away event kind, provenance, random progress, status or child effects to manufacture parity. Record new failures as unresolved work.
- [ ] **Step 4 — Write the concrete handoff:** actual service/Buffer access path; all factories and typed responses; availability/candidate/forecast/execute sequences; sample success/rejection/empty-hit/chain/early-stop results; conditional coverage if applicable; stale source, cancellation and acknowledgment; new skill event handlers; metadata/configuration values; remaining View/Unity checks.
- [ ] **Step 5 — Inspect scope and stop:** verify changed files, .meta/GUID preservation and absence of unrelated edits from this task. Report Phase completion, current baseline failures, test XML/log paths and unverified playback. Do not commit, push, implement View, or start another Phase.

**Phase G exit:** Core support is demonstrated by fresh calculation/publication/regression evidence, and View has implementable contracts. UI/scene/playback acceptance remains a separate statement; never infer it from Core tests.

## 5. Spec coverage and required review evidence

| Spec area | Owning tasks |
|---|---|
| Main ownership, use restrictions, state lifetimes (D01–D10) | A1, A2, B2, F1, F2 |
| Full Core forecast and visibility (D11–D12) | B1, C1, D2, E1, G1 |
| Powder Blossom and variants (F01–F08) | A2, C1 |
| Points, traversal, piercing and Judgment (M01–M15) | D1, D2, E1 |
| Existing request approach and Framework composition | B2, E1, F1, F2 |
| Single state owners and pure authoring | A1, F1, F2 |
| Shared effects and explicit eligibility (D13) | B1; confirmed DH decision |
| Full simulation versus disclosure | B1, E1; DV decision |
| Consistent command/damage/movement/result meaning | B1, B2, D2, E1, G1 |
| View API and implementation handoff | E1, F1, G1 |
| Change/understanding/verification locality | Every task's owned files; Phase reports and G1 |
| Existing required regression, no unrelated bug scope | 0.1, affected Phase checks, G1 |

For each Phase report, describe one actual changed rule and list:

- Changed files/functions/registration and why those changes are necessary.
- Contracts the next implementer needs to understand; unrelated skill internals they do not need.
- Shared dependencies that genuinely require regression versus accidental coupling removed.
- Fresh tests and failures, evidence paths, pending game decisions, and runtime/manual checks not performed.

Do not use number of files, interfaces, tests, or lines as proof of independence or quality.

## 6. Execution ledger

DV/DH/DK are confirmed requirements. Replace a Phase status only with evidence from that Phase.

| Phase | Status | Decisions / source | Focused evidence | Broad/shared evidence | Remaining work |
|---|---|---|---|---|---|
| 0 | Complete — GPT-6.1, Task 0.1 | DH → B1, DK → D2, DV → E1. [Source audit](#61-phase-0-source-audit) | Baseline uses both full assemblies | 6,845 selected, 6,795 passed, 36 existing failures, 14 skipped. [Execution evidence](#62-phase-0-execution-evidence) | Baseline retained for case-level comparison. |
| A | Complete — GPT-6.1, A1/A2 | [Contracts and preservation](#63-phase-a-implementation-and-verification) | 304/304 corrected focused; 112/112 final affected | Earlier broad run matched 36 baseline failures and identified nine affected test regressions. All nine passed on the final affected run. | Consumed by B; production/lifetime wiring remains with F. |
| B | Complete — Astra, B1/B2 | [Shared effects, evidence and publication](#64-phase-b-implementation-and-verification) | 82/82 new Phase B cases passed in the final run | 678 affected cases: 677 passed and one new event-inventory omission; corrected. Final affected recheck: 240/240 passed. | Stop. C1 requires a separate user request; no skill is activated. |
| C | Complete — GPT-6.1, C1 | [Powder Blossom, Bloom and Condemnation](#65-phase-c-implementation-and-verification) | 16/16 new cases | 224/224 focused Core/CoreIntegration cases, including 208 relevant regressions | Phase D requires a separate user request. Public forecast, transport and activation remain pending. |
| D | Complete — Astra, D1/D2 | Shared selection geometry, cumulative projectile delivery and explicit DK request; §6.6 | 33 new cases passed | 297/297 scoped final | Continued to E under the user's combined D/E authorization. |
| E | Complete — Astra, E1 | Public conditional forecast, immutable transport and dual stale guards; §6.7 | 23/23 new E cases | 148/148 corrected scoped final; subsequent cross-segment dedup fixture 7/7 | Stop. F and later phases require a separate request. |
| F | Complete — GPT-6.1, F1/F2 | [Upgrade, lifetime and production activation](#68-phase-f-implementation-and-verification) | 13/13 new cases, including both production Main casts | 262/270 selected; eight existing Phase 0 asset failures, zero new failures | Phase G requires a separate request. Full assemblies and Unity playback remain pending. |
| G | Complete — Astra, G1 | F intake: 9,973 files matched; §6.9 | 21/21 focused; corrected 30/30 | Full: 7,003 passed / 44 failed / 14 skipped of 7,061; eight new failures corrected and rerun, 36 unchanged baseline failures retained | Core→View handoff delivered; Unity playback unverified; stop |

Planning verification: inspected source paths, Editor executable location and test assembly names; checked plan/spec coverage and interface naming. No Unity tests, compilation, product code, test code or production assets were executed/changed while writing this plan.

The user selected two sessions, GPT-6.1 and Astra, and clarified that each Phase has one model owner. The owner performs that Phase's implementation, self-review, tests, corrections, and report. Preserve each user review boundary.

### 6.1 Phase 0 source audit

GPT-6.1 owns Task 0.1. Checked the current checkout at `D:/My_ProjectWork/Github_desktop/Project_L`, branch `feature/Cheon/Core/NewCombatSytem`, HEAD `a95ca5a22cde603e136666f6b68b2ea3ce4c9cbc`. Read both AGENTS files, the design and handoff, the original brief and locality guidelines referenced by the design, and the repository Framework guides and locality criteria. The current user request authorizes the audit, baseline and continuation records. The original brief supplies design evidence.

#### Current source and overlap

Initial porcelain status listed 25 modified tracked paths and three untracked skill documents. Significant overlap is in `C/Application/Combat/{AttackDirectDamageCalculator,CombatAttackCalculator,CombatAttackDelivery,CombatAttackQueries,CombatDamageCalculator,CommandAttackPlan,SacrificeDamagePlanner}.cs`. The ongoing change carries the basic range into sweep resumption, preserves pre-sweep distance evidence, and uses the same distance-scaling path for direct and redirected damage. It also changes the distance-trait spec/context/definition/effect/rule and the Dead Hand damage and sacrifice fixtures. Use `Library/CodexTests/PlayerActiveSkills/phase-0/pre-status.txt`, `pre-diff-stat.txt`, `pre-files/` and `pre-source.csv` for paths, contents and SHA-256 hashes. Seven font/settings paths appeared in initial porcelain and dropped out of the actual diff after Git's content check. Their bytes were also captured in `pre-files/` and `pre-porcelain-supplement.csv`, with file write times predating the starting snapshot. Source audit paths below use the root aliases in §2.

#### Append-only enum audit

| Contract and file | Current tail | Available append value on this snapshot | Owning work |
|---|---:|---|---|
| `StatusEffectKey` in `C/Contracts/CombatEffects/StatusEffectKey.cs` | Reconstructing 19 | Stun 20 | A2 |
| `CombatCommandKind` in `C/Contracts/Combat/CombatCommandInput.cs` and `CombatViewCommandKind` in `B/CombatView/CombatViewCommandKind.cs` | UndoMove 15 in both | UsePlayerSkill 16 in each enum | Core contract and dispatch, then E1 transport |
| `CombatViewPreviewKind` in `B/CombatView/CombatViewPreviewKind.cs` | RoarForecast 10 | 11–13 for SkillAvailability, SkillSelection and SkillForecast in that order | E1 |
| `CombatDamageKind` in `C/Contracts/Combat/CombatDamageKind.cs` | DamageOverTime 6 | PlayerSkillAttack 7 | B1 |
| `CombatEventKind` in `C/Contracts/Combat/CombatEvent.cs` | Swept 50 | 51 for the primary skill-attack occurrence | B2 |

These are audit results, with no reservations installed in source. Recheck against the implementation checkout before assigning. Preserve gaps, including preview 7 and the explicitly retired event values 15 and 30. `CombatPreviewResult` has typed payloads without a separate kind enum. `CombatProcessingKind` identifies the processing stream. `CombatDamageCause` describes numeric damage versus instant defeat. Neither needs a skill-specific value. New standalone keys have no existing definition: the plan's order allows skill keys None/PowderBlossom/MagicBullet as 0/1/2 and upgrade keys None/Bloom/Condemnation/Distortion/Judgment as 0/1/2/3/4. If explicit skill status provenance uses `EffectOriginKey`, `EffectOwnerKind` currently ends at StageGimmick 5; a distinct owner can append at 6. No artificial trait key is needed. Ordinary move and attack failure enums both end at Reconstructing 52. A typed ordinary Stun reason can append at 53 in both where the existing name-based mapping is used.

#### State, action restrictions, and lifetime

| Owner and current path | Preservation or validation point |
|---|---|
| `C/Contracts/Combat/CombatCommandInput.cs` | Initial constructor from `S/CombatInputAssembler.cs`, common source copy, actor copy, both `WithIntent` overloads, `WithWorld`, and `WithPersistentFacts`. Keep captured source distinct from submitted expected source. `C/Application/Combat/CombatCommandComposition.cs` chains the existing copy paths for projection, rewrapping and merge. |
| `C/Contracts/Combat/CombatCommandState.cs` | Constructor, `With`, `SameGameplayValues` and `SameValues`. Direct constructors also appear in `C/Contracts/Combat/BattlePreparation.cs` (default and coherence check), `C/Application/Combat/MovementProjection.cs` (projected world reconstruction), and target-typed `CombatTurnCalculator.State` (phase/turn transition). Preserve usage through the latter two; initialize explicitly only with genuine battle preparation. |
| `C/Contracts/Roster/OwnedCharacterRecord.cs` and `B/Character/CharacterBuffer.cs` | Extend record equality and `WithGrowth`. Existing-record reconstruction occurs in `C/Application/Roster/{ExperienceRules,MainSpecializationRules}.cs` and target-typed `C/Domain/Traits/Rules/HunterTraitRule.cs:Award`. `CharacterBuffer.PrepareAcquire` creates fresh records. Keep stale-record checks through existing `SameValues`; use test-owned growth, specialization, XP and Hunter award coverage for the preserving copies. |
| `C/Contracts/CombatEffects/{AppliedStatus,CharacterStatusCompatibility}.cs`, `C/Domain/CombatEffects/Rules/AppliedStatusRules.cs` | `ActionRestrictionStatus` accepts Bind and Disarm today. Its per-key max-remaining merge and affected-side end already apply generically by type. `CharacterStatusCompatibility` projects the existing legacy debuff fields and must leave Stun in canonical `StatusEffectSet` during legacy replacement. Check `CharacterCombatFacts.With/WithStatusEffects`, `TraitTurnLifecycle.EndAndRecord`, `CombatReserveReturnCleanup` and `CombatTemporaryStateCleanup` for preservation, expiry evidence and cleanup. |
| `C/Application/Combat/{CombatActionAdmission,CombatAttackQueries,CombatMovementCalculator,StatusEffectReadModel}.cs` and `C/Contracts/Snapshots/StatusEffectSnapshot.cs` | Shared ordinary restrictions feed live/range/availability and the intrinsic attack check; the attack queries also map movement failure names and have a separate `RestrictionFailure`. Add Stun to the read-model enumeration and the snapshot constructor's key validation. Skill admission needs its own usage and permission checks because the ordinary admission gate rejects already spent actions. View notice/HUD/tooltip text consumers are handoff points. |
| `S/{DatabaseBattleInitializationSource,BattleDefinitionAdapter,BattleEntryService,BattleLifetime}.cs`, `C/Contracts/Combat/BattleInitialization.cs`, `C/Application/Combat/BattlePreparationCalculator.cs`, `B/Core/BattleTransactions.cs` | Read definitions and selected owned Main, prepare once, validate membership/revision and install both stores, then pin by battle ID in `BattleLifetime.Install`. Preserve immutable definition facts through `BattleInitialization.WithAttackProfiles` and `WithTraitGrowth` if that initializer carries them. `BattleLifetime.EndCompleted` and `CombatStateBuffer.EndBattle` clear the battle; active preparation rejects duplicates. `CombatTurnCalculator.BeginControllerTurn` increments the existing sequence, so next turn availability derives from that sequence. |
| `RunSystem/RunStartService.cs` under the same CoreSystem root as S; `B/Core/ProgressTransactions.cs` | `StartNewRun` calls `PublishNewRun`: prepare initial roster, reset progress and install the new roster. Chapter entry/continuation install chapter progress and do not recreate owned records. Upgrade belongs to the owned Main for this Run. |

#### Transport, publication and evidence

- **E1 / DV.** Follow `B/CombatView/{CombatViewCommand,CombatViewPreview,CombatViewBuffer}.cs` → `S/{CombatCommandService,CombatPreviewService,CombatActionRequest,CombatInputAssembler}.cs` → `C/Contracts/Combat/CombatPreviewResult.cs` and the existing Buffer publication. `ToCommandKind` and both preview switches are explicit extension points. The first preview switch already throws on unknown values. The response switch uses a placement fallback; enumerate both placement cases when adding the skill arms. Request-number and battle checks are shared; the service and `BattleTransactions.PublishPreview` currently check query revision/turn explicitly for ActionAvailability. Skill queries need their own payload/source validation at both boundaries. The confirmed DV policy gives indistinguishable public forecasts under equal permitted facts and tests full-information parity separately. `CoreProfilerMarkers` derives preview markers from enum values. `RuntimeSystem/LifeCycleSystem/LifeCycles/Combat/Plans/CombatPlan.cs` and `Jobs/{CombatCommandJob,CombatPreviewJob}.cs` route by work and request number; source inspection found no skill-kind routing prerequisite there.
- **B2/C1.** `C/Application/Combat/CombatCommandCalculator.cs` is the Core dispatch/finalization seam. `C/Contracts/Combat/{CombatCommandTransition,CombatResult}.cs` carry and copy payloads. Check `CombatCommandComposition.WithEvents/Merge` and publisher `WithCompletion` for payload and occurrence identity. `S/CombatTransitionPublisher.cs` → `B/Core/BattleTransactions.cs:Apply` → `B/Combat/CombatStateBuffer.cs:PrepareCommand/InstallCommand` keeps one prepared transition and one result sequence. `ValidateActionState` checks ordinary settlements. Validate skill use independently in this same publication boundary. Rejected or stale input must preserve gameplay state and RNG.
- **B2 evidence.** Extend `C/Contracts/Combat/{CombatEvent,CombatHealthClaim,CombatHealthEvidenceValidator,CombatChangeEvidenceReader}.cs`. `CombatHealthScope` currently accepts a primary attack/roar occurrence at entry -1; `CombatHealthClaim.MatchesOccurrence` and `ValidateClaims` match physical hit indexes, recipients and event references. The current KillChainAttack child declaration is a `CombatEffectsEvent` entry. Preserve it. `C/Application/Combat/{CommandAttackLedger,AttackResultFactory}.cs` supply shared hit/health evidence. `MovementUndoPolicy` identifies ordinary attack/roar execution, resolves damage origins and filters attack damage kinds for captured receipts. Add explicit skill occurrence support and retain actual movement agency, dependencies and origin matching. Later View consumers include `ViewSystem/Combat/Playback/CombatSceneEventPresenter.cs` and `Playback/Ordering/CombatEventTypeTable.cs`; G1 records their handoff.

#### DH and DK source classification

| Current hook or seam | Phase 0 classification under the confirmed policy |
|---|---|
| `C/Domain/Traits/Rules/{BleedingTraitRule,DustScreenTraitRule}.cs`, `C/Application/Combat/{LavaCannonApplication,SacrificeDamagePlanner,CombatDamageApplication}.cs` | B1 includes primary skill damage alongside the current attack family for contact, incoming mitigation, actual-recipient redirection and the corresponding exclusion from the non-attack incoming branch. Keep all other damage kinds and their predicates, including recipient and environment conditions. |
| `C/Domain/Traits/Rules/HuntersMarkTraitRule.cs` | B1 includes active skill contact alongside ordinary and extra hits. Retain its reaction exclusion and other eligibility. |
| `C/Domain/Items/{RoketItemRule,LanternItemRule,WhiskeyItemRule}.cs`, through `CombatDamageApplication` and `Items/CombatItemResolver` | Roket final incoming mitigation and Lantern surviving-holder retaliation are incoming-hit hooks, so include primary skill hits with existing hostility, holder and threshold conditions. Preserve their existing ordinary versus extra/reaction distinctions. Whiskey already gates on actual damage and its health threshold, so needs no additional kind condition. |
| `AttackDirectDamageCalculator.ItemDirectHits` capture, `CombatAttackCalculator.ApplyAttackCompletedItem`, `C/Domain/Items/RopeItemRule.cs` | This candidate list feeds Rope through `AfterOrdinaryAttack` only. Keep it and the item/RNG completion hook ordinary-only. |
| `CombatAttackQueries` pre-hit processing, `C/Domain/Traits/Effects/DeadHandEffectRule.cs`, `CombatAttackCalculator.ApplyAttackSucceededEffects` | Pull/sweep aiming and movement, the Dead Hand sweep-specific multiplier, and the Focus stack scheduling called on ordinary success stay with that ordinary action. Existing Focus damage from already-active stacks remains a common modifier. B1 should keep actual attack-scope notifications in `TraitAttackNotifications` separate from the completion callback. |
| `C/Application/Combat/AttackDeliveryExecutor.cs` → `C/Application/Artifact/ThornHeartAttackScope.cs` | Include primary skill reflection when its existing combat, equipped and recipient conditions pass. Preserve reflected damage and the current reflection recursion/termination boundary. The current ordinary and preview gates also occur at scope construction; audit both under the complete simulation policy. |
| `C/Application/Combat/{CommandAttackPlan,CommandAttackProjection,AttackDeliveryExecutor,KillChainAttackCalculator}.cs` | B1 separates consequence execution from observer restriction and later publication. Existing `PublicPreview` influences hidden terrain, death traits, reflection and KillChainAttack. Keep legacy calls stable. Skill burst seeds must use fresh direct-caused deaths, because the current chain starts with deaths inferred against its input. |
| `C/Application/Combat/{AttackKnockbackCalculator,AttackForcedMovement}.cs` and `C/Domain/Combat/PierceTargetCollector.cs` | D2 passes one explicit Judgment request of two cells along the incoming segment before ordinary distance/mode composition. Current knockback picks a max trait distance and derives direction from impact center/actor. Preserve shared immunity, blocker and stepped arrival behavior, including independently eligible collision effects. D1/D2 own bent geometry and structure pass-through; ordinary straight-ray stopping remains independently covered. |

#### Production activation and implementation locality

- F2 activation path is `Assets/Scenes/Boot.unity` → `Assets/ProjectL/DataBaseSO/DatabaseCatalogSO.asset` → `InGameGroup/InGameGroup.asset` → `F/DBFactory/Group/InGameDataBaseFactory.cs` → `DB/Queries/InGameDataBase/InGameDataBase.cs`. Register in that group using its local Builder and query context. Existing combat services are created/exported by `F/Runtime/InGame/Core/{CombatSystemFactory,InGameCoreSystemFactory}.cs`, with the group forwarding PostBind to consumer modules. Preserve interface registration, lifecycle and one-build ownership (FW-001–004, FW-012, FW-016). Typed combat evidence stays in the existing transition/Buffer contract. FW-013's EventBus contract has no new registration here.
- `InGameGroup/Unit/Item/Playerable/Unit_Akimbo.asset` is key 10, MainCharacter and PlayerOnly. `Unit_Sniper.asset` is key 12, Regular and PlayerOnly with category Main, attack range 4 and existing piercing configuration. Its direct InGame asset reference is `Unit/UnitDataTable.asset`. `Loadout/Item/PlayerLoadout_1.asset` starts Akimbo plus ShotGun, Ballista and Cowboy. F2 must verify valid Sniper Main configuration and how selection is reached using the existing roster rules. Preserve Main validation (`PlayerMainDefinitionPolicy`, `TraitGrowthCatalogValidator`, and roster/deck rules) and the scope gate for any unrelated composition change. Acquisition economy remains deferred.
- **Before implementation:** attaching a new skill path by ordinary attack reuse would couple skill behavior to the ordinary action settlement and its transformations. The planned minimum seams keep authored definition, Run-owned upgrade and battle-owned use in their respective current owners. The request boundary depends on typed immutable contracts (ISP/DIP), the existing dispatcher has one extension point (OCP), and a skill result must satisfy evidence validation in its own shape (LSP). A Bloom mask change should touch its skill owner and focused tests (SRP); a shared health claim or damage-family change legitimately requires publication and existing attack regressions. These are expected change, understanding and verification boundaries. Phase 0 verified the source connections; feature behavior remains for later phases.

### 6.2 Phase 0 execution evidence

The project version and Editor were verified at Unity 6000.0.73f1 (`a166abc3bf0e`). The shared checkout had no running Unity Editor or project lock immediately before launch. GPT-6.1 ran the single approved AppData-enabled batch invocation with the §4 arguments and confirmed Editor exit and lock release.

    & 'D:/Program Files/Unity/Hub/Editor/6000.0.73f1/Editor/Unity.exe' -batchmode -nographics -projectPath 'D:/My_ProjectWork/Github_desktop/Project_L' -runTests -testPlatform EditMode -assemblyNames 'ProjectL.Tests.Core;ProjectL.Tests.CoreIntegration' -testResults '<run>/results.xml' -logFile '<run>/unity.log'

Run directory is `Library/CodexTests/PlayerActiveSkills/phase-0/baseline-20261115-043655/`. `invocation.txt` records the expanded arguments. Unity compiled and wrote fresh `results.xml` and `unity.log`. The runner reported exit code 2 for test failures. The log shows no C# compiler errors or abort before results.

| EditMode selection | Selected | Executed | Passed | Failed | Skipped |
|---|---:|---:|---:|---:|---:|
| ProjectL.Tests.Core | 5,755 | 5,744 | 5,742 | 2 | 11 |
| ProjectL.Tests.CoreIntegration | 1,090 | 1,087 | 1,053 | 34 | 3 |
| Total | 6,845 | 6,831 | 6,795 | 36 | 14 |

`failures.csv` records all 36 fully qualified case names, sites and messages; the XML also stores their stack traces. Comparison with `Library/CodexTests/PiercingDeadHand/phase-e-full/results.xml` found the same failure names, sites and messages, with zero differences in `baseline-comparison.json`. The fresh baseline includes ten additional passing cases from the existing Dead Hand work. Core failures are the interception-payload defensive identity assertion and the parameterized Taunt forced-decision assertion. CoreIntegration includes the CinemachineCameraOffset type load, UnitInfo Canvas and ScrollPanelPresentation audio setup failures, DB/growth and production Run Gold assertions, Deck policy null references, and existing tuning assertions. These failures are the Phase 0 comparison baseline for later Phases.

Tested source evidence is `pre-source.csv` (SHA-256 `75285F7479A759614DC106FAC59DC5805B98B741D8DDF9BFED3B58CE21A1F57A`), supplemented by `pre-porcelain-supplement.csv` and byte snapshots in `pre-files/`. `post-unity-source.csv` records the post-run comparison; `final-document-hashes.csv` records the two completed document edits. The baseline manifest includes the tracked Systems/Framework, tests, Packages and ProjectSettings trees and existing dirty paths. The post-run hash comparison and status check showed only edits to this plan and the two-session handoff. All snapshotted code, tests and assets matched.

**Self-review and continuation.** Rechecked actual constructor syntax, typed payload consumers and publication names. The audit captured the target-typed turn and Hunter copies, Stun's snapshot constructor, and the distinct incoming versus attack-completion item rules. No new Phase A blocker was found in the baseline. Next authorized work is GPT-6.1 A1/A2 with test-owned contract/copy and Stun fixtures, including ordinary action and affected-side expiry regressions. F2 still owns the production Sniper role and selection audit. PlayMode, actual View playback and production skill activation remain unverified. Task 0.1 delivered audit and evidence; product code and test code changes begin with later Phases.

### 6.3 Phase A implementation and verification

GPT-6.1 owns A1 and A2 in the shared checkout `D:/My_ProjectWork/Github_desktop/Project_L`, branch `feature/Cheon/Core/NewCombatSytem`, HEAD `a95ca5a22cde603e136666f6b68b2ea3ce4c9cbc`. The Phase 0 fingerprint matched at intake. The Phase A evidence directory contains `pre-status.txt`, `pre-source.csv`, verified affected-file preimages in `pre-files/`, and `phase-a-file-changes.csv` with absent/new or before/after SHA-256 values. `tested-source.csv` has SHA-256 `A0B625D07BE5180D8F1035CD7942C97D9233B2F29893FCF2E0C55CD95EB526DD`. The earlier broad run used `final-broad-tested-source.csv` (SHA-256 `456C41437CDE73BF9D56CCB6E2CC5D01B491AE46ECBBC34A41777E61953BC24B`). The final source differs from that broad source only in three affected test files and the progress documents.

**A1 contracts.** `C/Contracts/PlayerSkills/` contains the immutable definition, battle usage, intent/source, availability/selection/forecast, attack/cast result and upgrade contracts. `PlayerSkillAttackResult` carries independent hit, recipient damage breakdown, knockback and projectile contact direction facts. `PlayerSkillUseSettlement` records the existing controller sequence and derives a one-use decrement. `CombatCommandInput` preserves request, caller expectation and captured stamp through existing copy paths. `CombatCommandState.With` preserves the active binding and its two comparisons detect usage changes. The direct turn and movement projection constructors preserve it too. `BattlePreparation` validates the initial budget and the selected Main's identity. No feature clock or state store was added.

The Run-owned upgrade is on `OwnedCharacterRecord`; growth, experience, specialization and Hunter award copies preserve it. The existing `OwnedCharacterChange`, `TraitGrowthChange` and `MainSpecializationChange` contracts reject an upgrade change through those operations. The new item/table/query in `DB/Data/InGame/Table/PlayerSkill/` and `DB/Queries/InGameDataBase/InGameTable/PlayerActiveSkillTable.cs` validate Main eligibility, limits, supported branch pairs and uniqueness. They produce fresh immutable rows, with the typed query prepared for F2 local registration.

**A2 contracts.** `PlayerSkillQueries.Availability(input, source)` uses the active battle membership, actual actor, bound skill use and current captured source. The ordinary-action permission remains independent. Bind and spent ordinary actions permit the skill; Disarm and Stun reject it. Stage/control, actor/selected Main, missing skill, depleted uses, used turn and stale source are typed outcomes. `StatusEffectKey.Stun = 20` is a debuff with skill effect owner provenance, maximum remaining duration per restriction key, and affected-side turn-end expiry. Ordinary movement and attack share the restriction check and report `Stunned = 53`; canonical status snapshots retain the status and its origin through legacy debuff updates. Existing reserve and death cleanup clears it. The read-model list groups the actual restriction keys and preserves the existing enum order.

**Self-review and scope.** Fixed the new test's missing movement path and registered Stun in the shared classification table. Extended the existing status catalog test with ID 20 and debuff classification. Extended the input-copy inventory and the previous ordinary-preview serializer's optional-field convention. It omits absent skill request/source/usage and `None` upgrades for existing frozen vectors. Non-default skill facts still serialize. The frozen data and parity assertions were preserved. Added validation at the owned-record mutation boundaries so the future upgrade operation retains sole authority over that change.

The skill definition and limit now change through the authored row and its validation. Admission code needs only the captured state and status contracts. Turn progression and movement projections keep usage intact through existing state copies, so their regressions matter even with the skill rule isolated. The work adds no Factory/Group registration, assembly reference, View files, production assets or scene wiring. The shared checkout retains the existing Dead Hand changes, including its separate edits in `CombatAttackQueries.cs`. The only edit there from A2 is the attack restriction failure mapping and its comment.

| Run directory under `Library/CodexTests/PlayerActiveSkills/phase-a/` | Result |
|---|---|
| `a1-red-20261115-050006` | 14 selected, 14 failures on the initial compilable contracts. Recipients, state, request copies and authoring rules were still absent in this run. |
| `a1-green-20261115-050546` | 136 selected, 135 passed, one new test fixture error. The projected-world test omitted the required movement path. |
| `a2-red-20261115-050754` | 33 selected, 15 passed and 18 failed. The corrected A1 cases passed; the absent admission/restriction behavior failed. |
| `focused-20261115-051054` | 304 selected, 293 passed and 11 failed, exposing Stun classification and the frozen ordinary-preview format at the shared boundaries. |
| `corrected-focused-20261115-051415` | 304 selected and passed, 0 failed or skipped, including copy, roster/specialization, restrictions, ordinary actions and preview regressions. |
| `broad-20261115-051957` | Canceled after finding the existing fixed status enum inventory assertion. The owned batch process was stopped and its unlocked temporary lock file was moved into the evidence directory. No result was used from this run. |
| `final-broad-20261115-052412` | 6,882 selected, 6,823 passed, 45 failed, 14 skipped. All 37 added cases passed. Of the failures, 36 exactly matched Phase 0; nine were caused by stale property inventories and reflection fixture arguments. |
| `final-affected-20261115-053713` | 112 selected and passed, 0 failed or skipped (99 Core, 13 CoreIntegration). All 37 new skill cases and all nine corrected cases passed. Exit 0 and no C# compiler errors. |

Completed run directories contain the expanded `invocation.txt`, `results.xml`, `unity.log` and `exit-code.txt`. Runs use the approved AppData-enabled Unity 6000.0.73f1 EditMode command in §4.

The broad run finished before the user's instruction to skip unrelated tests. `baseline-comparison.json` in that run directory compares names, sites and messages. Updating `ContractShapeGuardTests` to include both new fields preserves its explicit contract inventory. The existing reflection fixtures in `TraitStateContractTests` and `TraitStatePublicationTests` now pass the original upgrade, preserving the test intent. The user limited subsequent Phase A verification to the affected Core/CoreIntegration fixtures. Final broad results on the test-only correction are outside that requested scope.

**Continuation.** When explicitly authorized, Astra owns B1/B2; take the existing immutable contracts and Phase 0 DH source inventory into shared full-consequence simulation and publication. Extend the primary skill attack occurrence, health/undo claims and Buffer validation in B2. The current `Availability` input requires the captured `PlayerSkillSource`; later command handling must separately compare `PlayerSkillRequest.ExpectedSource` with authoritative facts and publication state. Command dispatch, skill calculations, View transport and production definition/upgrade activation are subsequent deliverables. PlayMode and View playback remain unverified.

### 6.4 Phase B implementation and verification

The authorized Phase B owner performed implementation, self-review, corrections and verification in this session. No other model or subagent reviewed the work. Only B1/B2 were implemented; C and later Phases were not started. The checkout remains `D:/My_ProjectWork/Github_desktop/Project_L`, branch `feature/Cheon/Core/NewCombatSytem`, HEAD `a95ca5a22cde603e136666f6b68b2ea3ce4c9cbc`. No commit or push was made.

**Intake and source preservation.** The 8,805 entries in Phase A's tested-source manifest matched, except for the two completion documents, which exactly matched Phase A's separate final-document hashes. `Library/CodexTests/PlayerActiveSkills/phase-b/` contains the pre-status, HEAD, pre-source manifest, all preimages and the intake comparison. `phase-b-file-changes.csv` identifies 29 product files, two existing Core test files, and four new fixtures with their four `.meta` files. Existing Phase A and piercing/Dead Hand changes were preserved, including the overlapping attack files. No View, Framework, Factory, assembly definition, production SO, scene or package was changed.

**B1 — confirmed DH, with existing eligibility retained.** `CombatDamageKind.PlayerSkillAttack = 7` and `CombatDamageKinds.IsAttack` describe the shared attack family; they do not turn every item trigger into an all-attack trigger.

| Source/hook | Implemented treatment |
|---|---|
| Bleeding, DustScreen, LavaCannon, Sacrifice planning, complementary non-attack incoming mitigation | Include primary skill hits in the existing attack family. Preserve hostility, alive/field, captured-area, controller-side, actual-recipient and other original conditions. Skills do not run incoming mitigation twice. |
| HuntersMark | Ordinary, extra and primary skill contact are eligible; reaction, reflected, environment and periodic damage keep their exclusions. |
| Roket and Lantern | Add primary skill hits beside ordinary hits. Extra/reaction exclusions, holder identity, hostility, survival and thresholds remain unchanged. Whiskey already uses actual loss and its threshold and needed no kind change. |
| ThornHeart | Ordinary/primary-skill scopes can reflect when the existing equipped, combat, enemy-attacker/player-recipient conditions pass. Complete transient simulation includes the reflection; legacy public previews retain their omission. Reflection remains `ReflectedDamage`, runs once per scope, and records that scope's actual triggering attack group. |
| Ordinary-only behavior | Pull/sweep selection and movement, the Dead Hand sweep multiplier, ordinary success/Focus scheduling and Rope's `AfterOrdinaryAttack` path remain in their existing owner. Current deferred Focus/Rope availability was preserved rather than activated. |
| Actual attack notifications | `TraitAttackNotifications.Apply` remains separate from ordinary completion. The primary-scope caller notifies once; the existing KillChainAttack owner notifies its actual extra attacks. Local notification counts do not mutate input state or settle ordinary action use. |

`CommandAttackPlan.CompleteConsequences` is an optional explicit choice, defaulting to `!PublicPreview` for existing callers. The executor's kill chain/reflection and the projection's death effects/corpse displacement use this choice. `PublicPreview` and its observer still control the visible environment/Tornado observation. Movement-use capture remains explicit on the projection, and installation remains exclusively at the existing Buffer transaction. Complete local simulations exercise death effects, incoming item consumption and RNG without installing them; hidden dust is not reread through the non-attack branch.

`CommandAttackLedger.DirectKilledEnemyIds` captures fresh hostile actual-recipient deaths by matching the direct scope's health-step/contact origin references before obstacle and arrival consequences. Reconstruction, old deaths, reflection and collateral damage do not seed another primary occurrence. The skill executor passes this set to `KillChainAttackCalculator.Resolve(initialKilledEnemyIds: ...)`; an explicitly empty set means no initial work, duplicates are deduplicated, and invalid non-dead/non-hostile seeds reject. Ordinary callers retain their original inference when the argument is null.

**B2 — one occurrence stream and one use settlement.** `CombatPlayerSkillAttackEvent` appends event ID 51 (`PlayerSkillAttacked`). `AttackResultFactory.CreatePlayerSkill` reuses the existing hit ledger and adds recipient damage breakdowns, reactions and knockbacks without constructing an ordinary/reaction `AttackResult`. `CommandAttackLedger.BindHealthClaims(damage, occurrence)` binds the same primary occurrence to existing `AttackHit` claims; no new HP application is introduced.

`CombatCommandTransition.PlayerSkill` and `CombatResult.PlayerSkill` retain the immutable result. State/evidence/completion copies and command composition preserve the payload and event instances. Result validation requires ordered, distinct primary ordinals/groups owned by the settlement actor and the same ordered event references in the outer command. Health validation recognizes the new scope, rejects duplicate application/claims and reused attack steps, and checks primary-skill contact origin identity and causal event order. The change reader checks actual forced movement steps against the knockback path. A projectile path creates no character movement. Movement-undo reconciliation recognizes the skill actor and skill dust-defense uses while retaining unrelated checkpoints.

`CombatStateBuffer` checks the captured and expected source against its actual battle, controller sequence, query revision and actor before installation. It validates the unchanged Main/definition/upgrade binding, one-use decrement and current-turn settlement, and rejects skill mutations from other commands. Existing ordinary action validation remains strict and unchanged action use is required for skill casts. `BattleTransactions.Apply` already supplies prepare-before-install, duplicate command-number handling and one result sequence; that implementation was reused without a second transaction/store or live-state repair.

**Self-review findings and corrections.**

1. Adding an optional kill-seed parameter alone left the executor able to recount an earlier occurrence's deaths. Two failing cases reproduced 1/2 extra attacks instead of 0/1. The direct ledger seed capture and skill executor forwarding fixed the actual caller.
2. Numeric HP agreement alone accepted a forged primary skill declaration with another contact origin or after its HP event. Both cases failed to reject in `self-review-red`; exact origin-reference and occurrence-order validation now reject them.
3. ThornHeart used the constant direct group for every triggering occurrence. A scope with group 5 reproduced group 1. The scope and its planning clone now retain the caller's actual group; legacy callers still default to group 1.
4. Added the new event to the existing Core-owned exhaustive evidence sample inventory. Extended the frozen ordinary-preview serializer's established absent-feature convention for null `CombatCommandTransition.PlayerSkill`. Existing golden vectors and assertions were not recaptured or weakened.
5. Corrected new fixture errors: environment ID progression, explicit trait-state construction, deferred Focus expectations, valid initial enemy placement/Main attack range, and value comparison for rebuilt read wrappers. Two compile-only runs exposed missing test imports/nonpublic contract access and a test-only Domain reference; the final fixtures preserve the existing assembly boundaries. Nonpublic health constructors are used through reflection only in corruption/synthetic publication tests, not exposed to consumers.

**Verification.** Unity 6000.0.73f1 ran serially through the approved AppData-enabled batch route. Only new B1/B2 Core/CoreIntegration tests and regressions of the changed shared contracts were selected, as requested. No full assembly run was performed. Each completed run retains `invocation.txt`, `tested-source.csv`, `results.xml`, `unity.log`, `exit-code.txt`, `failures.csv`, `baseline-comparison.json` and `summary.json` under `Library/CodexTests/PlayerActiveSkills/phase-b/`.

| Run directory | Fresh result |
|---|---|
| `b1-red-20261115-055035` | 36 selected; 22 passed, 14 failed, including intended missing behavior and new fixture errors. |
| `b1-focused-20261115-055338` | 370/370 passed. |
| `b2-red-20261115-060132` | Compilation failed in new test code; no test XML produced. Compiler failures/log retained. Not behavioral RED evidence. |
| `b2-red-compiled-20261115-060314` | 29 selected; 15 passed, 14 failed. Missing skill evidence/copy support and repeated kill seeds reproduced. |
| `b2-focused-20261115-061117` | 111 selected; 90 passed, 21 failed (new fixture placement and absent-field frozen serialization). |
| `b2-corrected-20261115-061427` | 112 selected; 110 passed, two remaining new fixture assertions failed. |
| `b2-publication-20261115-061912` | Compilation failed on the new integration fixture's Domain import; no test XML produced. Existing assembly references were retained. |
| `b2-publication-corrected-20261115-062237` | 79/79 passed. Includes synthetic damage/forced movement/completion publication and checkpoint retention. |
| `self-review-red-20261115-062551` | Both provenance/order corruption cases failed because rejection was missing. |
| `final-affected-20261115-062750` | 678 selected; 677 passed, one exhaustive event-inventory omission failed. All new behavior and the ordinary preview/resumption regressions passed. |
| `reflection-scope-red-20261115-063448` | 51 selected; 50 passed (including the corrected event inventory), one triggering-group assertion failed as intended. |
| `final-corrected-20261115-063709` | 240/240 passed: 184 Core and 56 CoreIntegration. All 82 new Phase B cases passed (31 effect, 16 simulation, 11 evidence, 24 publication). Exit 0; no C# compiler errors. |

The final corrected source manifest has SHA-256 `8378FC5A83B35B742185BAD04A642A49177E79E80E254E7FE13D9605FB5F0246`. The final recheck contains every new Phase B fixture plus the event inventory, reflection/sacrifice, frozen-preview and mandatory B2 publication regressions affected by the last corrections. The earlier 678-case run additionally covers kill chains, effect/item/death paths, attack projection/resumption, change evidence and movement-undo/publication contracts; its single failure was corrected and rerun. This is evidence from two scoped runs, not a claimed 678-case all-pass run on the final snapshot. Case-level comparison against Phase 0 is retained for every behavioral failure; no Phase 0 failure remained in these final selected checks. Compile-only failures have compiler lists and an explicit no-XML status. New C# files are UTF-8/LF with companion `.meta` files; `git diff --check` passed. Final document hashes and post-run source preservation are recorded separately in the evidence directory.

**Architecture/locality and Phase C intake.** No persistent HP, occupancy, trait, RNG, terrain or usage copy was added. Character/Combat Buffers retain SSOT; the existing publisher/transaction owns installation. No Factory/Group/root registration, Job or Framework dependency changed. A future mask/burst rule belongs to its skill owner; a shared hook change belongs to the existing rule and the typed attack-family condition; occurrence/health/use changes legitimately require shared publication regressions. The new explicit policy, direct-kill seed and typed result binding reduce the need for C to understand ordinary settlement/preview internals. They do not eliminate the need to inspect the shared damage boundary or run its relevant regressions.

For C1, use one cast projection/arrival lifetime and fresh current projected input/ledger for each primary scope. Notify the actual attack scope before resolving its current damage, allocate distinct groups through the shared `CommandAttackGroups`, pass the existing arrival to subsequent executor calls, and use `DirectKilledEnemyIds` for direct-burst qualification. The executor resolves eligible KillChainAttack before returning; do not invoke it again for the same seed. Construct the primary event with `CreatePlayerSkill`, insert it before its effects, bind its health claims, and keep those instances through the one outer finalization and one `PlayerSkillUseSettlement`.

C still owns Powder Blossom masks, 50% scaling, reactivation/Stun and its current-attack update tests. In particular, `CombatAttackDelivery.CurrentDamage` rebases delivery from raw common damage when projected attack changes; C must integrate its skill factor explicitly at the approved damage boundary rather than pass a pre-scaled `CommonDamage` and assume rebasing preserves the factor. This is a recorded C integration point; no skill damage formula or rule dispatcher was activated in B. D owns projectile geometry/scaling/Judgment, E owns transport and public forecast disclosure, F owns production definitions/lifetime/upgrade activation, and G owns broad verification. PlayMode, actual View playback, production activation and complete skill forecast/execution parity remain unverified. Stop at this Phase boundary.

### 6.5 Phase C implementation and verification

GPT-6.1 completed C1 on 2026-11-15 in `D:/My_ProjectWork/Github_desktop/Project_L`, branch `feature/Cheon/Core/NewCombatSytem`, HEAD `a95ca5a22cde603e136666f6b68b2ea3ce4c9cbc`. The starting manifest matched all 8,813 Phase B source entries after applying B's final document hashes. All 39 entries in B's change manifest matched their tested hashes. Existing Phase A/B and piercing/Dead Hand edits remain in the shared checkout. `phase-c-file-changes.csv` identifies the 15 source/test/meta paths added or changed by C1 relative to that starting point. Two ledger/handoff edits follow verification.

**Calculation and publication.**

- The actual `UsePlayerSkill` command and the transient full-calculation entry use the same admission and calculation path. Admission compares the submitted expected source with the captured source before creating a combat projection. Stale intent and invalid target counts return without consuming a use or changing RNG.
- `PowderBlossomCalculator` owns the exact 8/20/36-cell shapes and their cast-local expansion, board clipping, non-allied targeting, wall-independent eligibility and per-burst entity deduplication. Condemnation begins at 20 cells, applies one affected-side-turn-end of Stun to surviving direct recipients through the common status boundary, and ends after its first burst.
- Each primary burst captures current projected inputs, sends the actual scope notification once, and creates a fresh ledger and attack group. Bursts share the world, arrival, health evidence and group allocator. The obstacle calculator is refreshed between bursts to read the current environment and isolate its per-burst blast accounting. The executor resolves eligible KillChainAttack from that burst's direct death set. Only the primary burst's own direct hostile character deaths can schedule one more burst after the executor returns. A dead/off-field caster or terminal outcome prevents further bursts.
- `CommandAttackPlan.DamagePercent` carries the primary multiplier (50 for Powder Blossom, default 100). `CombatAttackDelivery.CurrentDamage` applies it to the delivered base after resolving any raw common-damage rebase and before target modifiers, with the existing integer half-up rule. KillChainAttack retains its own damage computation and sees the shared projected attack state at its existing point in the sequence.
- Each typed skill occurrence precedes its effects and uses the Phase B factory and exact health-claim binding. The cast finalizes once, attaches one `PlayerSkillUseSettlement`, and flows through the existing command reconciliation and real Buffer publisher. Empty and interrupted accepted casts still settle one use. Ordinary move/attack permission stays independent.

**Self-review and corrections.** Checked preimages of each touched shared file against the C1 diff, including the prior piercing rebase logic and Phase B event/health ordering. A same-scope attack change and a between-burst attack change are covered independently. The supplies fixture verifies KillChainAttack reads the updated attack before the next burst and retains its own damage rule. The initial parity fixture passed a roster GUID to the generic test serializer; the correction compares immutable gameplay facts and evidence. A focused run then exposed the fixture checking item RNG for a KillChainAttack scenario. The final version checks environment RNG and compares deterministic result facts and claim identity across local calculation and command execution. No independent model review was used.

**Focused execution.** Unity 6000.0.73f1 ran EditMode through the approved AppData-enabled route. The local evidence root is `Library/CodexTests/PlayerActiveSkills/phase-c/`. Each run directory contains the command arguments, exit code, XML, log, failure list and case-level Phase 0 comparison. The final manifest was captured before Unity started. The RED and first focused manifests were reconstructed afterward from the intake/final hashes and retained intermediate edit hunks. No failed C1 case matched a Phase 0 failure.

| Run | Result |
|---|---|
| `red-20261115-065226` | 14 selected, 13 missing-dispatch/rejection failures and one test serializer failure. |
| `focused-20261115-065553` | 111/112 passed. The sole failure checked the wrong RNG stream in the parity fixture. |
| `final-20261115-070031` | 224/224 passed, exit 0 and no skipped cases. 199 Core and 25 CoreIntegration cases include all 16 new C1 cases and 208 directly related regressions. |

The final `tested-source.csv` SHA-256 is `DECD0604C7ED5F7FD750327F266EAED25BDF01901DACC5873DC906C9BCC8899D`. The scoped run checks the three C1 fixtures plus relevant shared KillChainAttack, Stun/restriction, obstacle/death, piercing, reflection, damage and evidence contracts. Post-run hashes matched the tested inputs. Full assemblies, public skill forecasting/disclosure, input transport, PlayMode, View, scenes and production activation were outside this verification.

**Phase D intake.** Once D1/D2 are separately authorized, retain the source guard, one cast projection/finalization and typed event/health binding. Add Magic Bullet selection and dispatch through its own calculator and use the approved DK incoming direction and displacement distance with the existing forced movement consequences. Preserve the raw common-damage rebase and put scope-wide or hit-specific scaling at the boundary appropriate to the Magic Bullet rule. Keep projectile path data separate from character movement. E retains public forecasting/disclosure and transport. Stop at C1. No commit or push was performed.

### 6.6 Phase D implementation and verification

The user authorized D1 → D2 → E1 in this same session, with a D ledger checkpoint before continuing to E. No second-model review, commit, push, View work or production activation was performed. Intake compared 8,821 Phase C manifest entries with the actual working tree, substituting the final document hashes: zero differences. Existing A/B/C and piercing/Dead Hand work is preserved; HEAD remains `a95ca5a22cde603e136666f6b68b2ea3ce4c9cbc`.

**Contracts and locality.** `MagicBulletCalculator` owns relative cardinal/perpendicular point validation, ordered bent geometry and global entity deduplication. `PlayerSkillQueries.Selection` and cast admission use this same validator and the current caster range, without moving/rotating the caster. Shared delivery now accepts an optional projectile budget/falloff policy; ordinary delivery defaults are unchanged. A projectile flushes its previous structure reaction before the next live encounter. Dead candidates consume no encounter, and falloff uses the existing effect evaluation/rounding. Actual path, contact-index incoming direction and `ImpactCells` describe delivered geometry separately from the damage origin.

Judgment supplies a single `AttackDisplacementRequest` (incoming direction, distance 2), 150% primary scaling and physical-defense bypass. Common immunity, stepped admission, terrain, interruptions, corpse anchors and independently eligible collision effects stay with their existing owners. Ordinary trait compatibility and displacement rules remain unchanged. Base/Distortion require one existing PiercingFalloff configuration; F must ensure the authored Sniper configuration supplies it. Judgment needs no piercing trait.

**Self-review and fixes.** Test fixture errors (NUnit constraints, namespace imports, captured membership and parameterized trait statuses) were corrected separately from behavioral RED. The final interaction run exposed a real evidence defect: thorn damage in a skill scope had been mistaken for direct skill contact. `CombatHealthSource` now carries the actual damage kind from `CombatDamageApplication`; the validator retains strict direct-contact identity checks for skill/legacy unclassified damage and accepts independently typed environmental steps. A test that combined two already-incompatible authored traits was corrected to test Judgment versus each legal competing trait; existing authoring restrictions were preserved. Actual contact ordinals remain contiguous after an explosion kills a later candidate.

**Evidence.** All artifacts are in `Library/CodexTests/PlayerActiveSkills/phase-d/`, with per-run command, tested-source hashes, Unity log, XML, failures and Phase 0 comparison. Compile-only attempts have no XML. `d1-red-valid-fixture-20261115-072001`: 14 expected failures; `d1-green-20261115-072213`: 49/49. `d2-red-corrected-20261115-072540`: 14 missing-dispatch failures and two fixture-constructor failures. `d2-focused-20261115-072954`: 29/30; the failure was the invalid fixture trait combination. `d-final-20261115-073239`: 295/297; the thorn evidence defect and incompatible two-trait fixture were then fixed. **`d-corrected-20261115-073539`: 297/297, exit 0, no skips, including all 33 new D cases.** No failed case matched Phase 0. Full assemblies were not run.

The final selection includes the new selection/traversal/skill/Judgment/parity fixtures, ordinary piercing/rounding/trace, forced displacement/path, knockback terrain, Powder Blossom, skill evidence and real publication regressions. D verifies local computation and existing publication contracts; public disclosure/transport follows in E, production activation in F. PlayMode, View and scenes remain unverified and out of scope.

### 6.7 Phase E implementation and verification

**E1 implementation and self-review.** The existing command/preview DTOs now carry immutable `PlayerSkillRequest` values. `CombatActionRequest` preserves the payload; `CombatInputAssembler` independently captures battle, turn, query revision and actor from the current Buffer. `CombatCommandService` maps enum 16 to the existing skill calculator. `CombatPreviewService` maps appended preview kinds 11–13 to availability, selection and forecast; placement arms are explicit and unknown kinds throw. `CombatPlan`, `CombatCommandJob` and `CombatPreviewJob` were inspected: they already dispatch pending work/request numbers through these services and need no skill-specific registration. The existing factory/service ownership and Buffer transaction remain the only live state owners.

The API is `CombatViewPreviewData.ForPlayerSkillAvailability(actor)`, `ForPlayerSkillSelection(actor, points)`, `ForPlayerSkillForecast(actor, points)` and `CombatViewCommandData.ForPlayerSkill(actor, points, expectedSource)`. Responses have exactly one `CombatPreviewResult.SkillAvailability`, `.SkillSelection` or `.SkillForecast` arm. Selection and forecast echo immutable `SelectedPoints`. Submit/SetPreview numbers, `ClearPreview(requestNumber)`, result sequence and `AcknowledgePresented` retain their existing lifetimes. Execution returns the existing `CombatResult` with one `PlayerSkillResult` and one use settlement. Rejected requests leave gameplay/RNG unchanged while normal handled/result bookkeeping advances.

**Disclosure contract (DV).** A public selection/forecast first projects the existing Player-visible environment and semantic bindings, preserving the existing requirement that walls are public to both sides. Complete calculation then runs through the same skill/command engine, including known child attacks, death/terrain consequences and pending reactions. Grouped tornado entry uses an internal calculation observer, including nested forced movement. Hidden effects cannot influence public HP, path, outcome events, acceptance or coverage through the authoritative world. The public calculation uses canonical private RNG seeds and derives environment ID/entry counters from visible sources; it does not expose the live private RNG sequence. Public random branches are conditional projections, not reservations or promises of the authoritative random result. All accepted forecasts use `KnownInformationConditional`, regardless of whether any hidden source is actually present. Rejected forecasts use `None` and no prediction. Internal full-information calculation continues to use the actual RNG and matches execution.

The observer and health damage-kind metadata are internal implementation facts. Self-review caught their accidental appearance in frozen ordinary-preview snapshots; they were made internal instead of changing the frozen tests. `RequireAuthoritativeCalculation()` rejects installation of the transient public calculation. Normal execution independently validates its submitted `ExpectedSource`, actual facts and full geometry. The service discards replaced/battle/turn/revision-stale work, and `BattleTransactions.PublishPreview` independently validates response arm, actor, points, battle, turn and revision before installation.

**Failure contract.** Normal query/execution failures use `NoActiveBattle`, `NotPlayerCombatTurn`, `NotSelectedMain`, `ActorUnavailable`, `NoSkillConfigured`, `Disarmed`, `Stunned`, `StageUsesExhausted`, `UsedThisTurn`, `WrongPointCount`, `PointOutsideBoard`, `PointOnWall`, `PointOutOfRange`, `InvalidSegmentDirection`, `SegmentBlockedByWall`, `SourceChanged` or `BattleChanged`; `None` means accepted. An incomplete prefix can be a valid selection but is not an accepted forecast/cast. Valid empty casts consume a use. Malformed DTOs and inconsistent publication payloads throw before mutation; they are not gameplay failure codes.

**Completed verification.** Evidence is under `Library/CodexTests/PlayerActiveSkills/phase-e/`. `e-red-corrected-20261115-074142`: 13 failures and two vacuous initial secrecy passes; those tests were strengthened to require an accepted forecast. One fixture attempted a forbidden hidden wall and was replaced with valid hidden range terrain. `e-focused-20261115-074532`: 7/15; seven integration failures came from the pre-existing Phase B test re-preparation changing the battle ID while its original runtime definition cache remained pinned, and one fixture expected the wrong Sandstorm range reduction. Tests now bind the real command/preview services to the prepared test battle's real assembler/publisher definition. This does not implement F's operational lifetime/activation work. `e-corrected-20261115-074729`: 15/15. `e-disclosure-transport-20261115-074923`: 21/21, including hidden/known tornado and private RNG indistinguishability, Magic Bullet transport and stale publication after a real move. `e-final-20261115-075106`: 143/148; all five failures were the internal-metadata snapshot exposure described above. **`e-final-corrected-20261115-075322`: 148/148, exit 0, no skips (95 Core, 53 CoreIntegration), including all 23 new E cases and all 33 D cases.** No failure matched Phase 0, and no frozen regression test was edited.

An additional acceptance case then made cross-segment deduplication explicit: one sparse multi-cell structure is contacted on the initial and final segments of Distortion but consumes only one encounter, followed by a 75% hit. **`e-dedup-final-20261115-075947`: 7/7**, covering that new case and the six prior Magic Bullet skill cases. No product code changed after the 148-case run. These two runs cover 149 distinct cases on the final product source; they are not a claimed single 149-case run. The 148-case manifest SHA-256 is `6701DD9016E2A7E1E92AA9167C6C221F05A00625EFE245A5D421C7C9D875FAC3`; the final source/test manifest is `e-dedup-final-20261115-075947/tested-source.csv`, SHA-256 `828703293E24C6AF1FBE25FEF19203F432148098C4438551BFBA3E0E3BA11F14`. Final documentation hashes are separate. Compile-only attempts retain compiler failure lists and explicitly have no NUnit XML. Full assemblies were not run.

**Change locality and continuation.** Skill geometry/damage remains in its skill owner plus explicit shared delivery policies. Adding a skill request requires its DTO/dispatch/result arms and shared source guard; no second queue, service, state store or Framework lifecycle was created. Understanding scope is skill rules, common delivery/evidence, observer projection and request/publication contracts; unrelated View, economy, AI and production content are unchanged. The extra verification scope is caused by the actually changed shared contracts, including ordinary frozen previews. F retains upgrade operations, genuine battle/turn reset and production Main/skill configuration; G retains broad regression and final View handoff. No View UI, scenes, PlayMode, production assets, commit or push were included.

### 6.8 Phase F implementation and verification

GPT-6.1 completed F1 then F2 on 2026-11-15 in the shared checkout at `a95ca5a22cde603e136666f6b68b2ea3ce4c9cbc`. All 8,841 Phase E manifest entries matched after substituting its two document hashes. Both 28-entry D/E delta lists matched with seven later E entries taking precedence. The F2 intake added byte hashes for 1,112 tracked database asset and metadata files. Existing A–E, piercing and Dead Hand changes were preserved.

**Upgrade and lifetime.** `ICharacterRosterService.ReadPlayerSkillUpgradeAvailability()` presents the confirmed deck's selected Main, authored definition, branch, Run ID and roster revision. `TryApplyPlayerSkillUpgrade(expectedRunId, expectedRosterRevision, mainId, selectedBranch)` uses the existing roster source and selected deck. The pure rule returns a new owned record for one compatible branch. `PlayerSkillUpgradeTransactions` revalidates the existing source, Run and battle lifetime before CharacterBuffer prepares and installs the record. Installation replaces only the expected Main's record and increments the existing roster revision once. Deployment, combat and completed battles awaiting cleanup all return `InBattle`. Specialization and growth preserve the branch on that owned instance. Genuine new Run acquisition creates fresh records and clears the old branch through the existing Run transaction.

The operational `DatabaseBattleInitializationSource` reads the selected deck Main's authored skill and passes it through `BattleDefinitionAdapter` to immutable `BattleInitialization`. Profile/growth copies keep that definition. `BattleEntryService` guards the captured Run, roster and deck. `BattlePreparationCalculator` pairs the definition with the same Main instance's branch and allocated combat actor. Existing `BattleLifetime` installs the definitions and preparation for the new battle. `CombatStateBuffer` owns stage uses and the last-used player turn. The initial stage limit comes from the authored definition. Rejected re-entry and prediction preserve it. A later player turn changes only the admission opportunity. E's command, forecast, source stamp, publication, acknowledgment and disclosure contracts were reused.

**Registration and production Main audit.** Active `DatabaseCatalogSO` includes `InGameGroup`. The group explicitly references the new table with two rows. `InGameDataBaseFactory` converts rows to pure data, validates the canonical unit bindings and registers `IPlayerActiveSkillTableQuery` in the local InGame builder. The `IInGameQuery` instance exposes it through `IHasPlayerActiveSkillTable`. Consumers reach it from the database context. The existing core group still exports the roster and battle services. No additional root service or Job registration was added.

The shipped mappings are Akimbo → Powder Blossom and Sniper → Magic Bullet, both with one stage use. Duplicate skill/Main keys and malformed SO rows fail with source identifiers. Factory validation rejects absent or ineligible canonical owners and requires a single authored PiercingFalloff entry for Magic Bullet. A missing table in development fixtures or an absent Main mapping returns no skill. The existing Sniper asset already had Main category, PlayerOnly eligibility, a cardinal targeting profile, range 4 and one 25%/four-target PiercingFalloff entry. Its GUID was referenced by the unit table and no loadout, unit roster, scene or prefab. The minimal role change was Regular → MainCharacter. The production loadout retains Akimbo. The existing acquisition and deck selection boundaries can acquire and bind Sniper independently, as tested. The authored range, attack and piercing values stay on the unit definition.

**Self-review and corrections.** Checked the Core/Systems assembly boundary, the pre/post source guards, one-field upgrade preparation, no transfer on Main switch and the product/fixture configuration paths. Published the pure rule to the Systems consumer across the existing assembly boundary. Corrected test data that put an enemy headquarters in a player deployment cell. The F2 fixture now uses a valid test-owned targeting table. Production end-to-end checks select the currently authored first stage and query a valid deployment cell, after fixture assumptions about the retired stage 1101 and an out-of-zone configured anchor failed. The eight unrelated DB expectations were preserved unchanged.

| Run | Result |
|---|---|
| `f1-red-20261115-081042` | Compilation stopped before NUnit. The new rule was absent and one forecast fixture used the wrong type name. |
| `f1-focused-20261115-081504` | Compilation stopped at the Core/Systems access boundary. |
| `f1-focused-20261115-081553` | 136/137 passed. The failure came from the test's invalid deployment setup. |
| `f1-corrected-20261115-081728` | 138/138 passed before F2, including next-player-turn use opportunity. |
| `f2-red-20261115-082014` | 7/12 passed. The failures exposed absent production registration, Regular Sniper and incomplete test-owned targeting setup. |
| `f2-focused-20261115-082334` | 10/12 passed. The two production tests still pointed to retired stage 1101. |
| `f-final-20261115-082602` | 260/270 passed. Eight DB failures matched Phase 0 and two production tests used an out-of-zone deployment anchor. |
| `f-final-corrected-20261115-082834` | 262/270 passed, zero skipped. Both operational Main casts and all 13 new F cases passed. The eight failures matched Phase 0 by qualified name and message. |

**Verification evidence and scope.** All runs used Unity 6000.0.73f1 EditMode through the approved AppData-enabled route. `Library/CodexTests/PlayerActiveSkills/phase-f/` retains per-run commands, prelaunch source hashes, logs, test XML when NUnit executed, and case-level failure comparisons. The final scoped run passed 191/191 outside DataBaseAssetTests. The test-owned Table/Factory checks and the separate two production integration cases passed. DataBaseAssetTests passed 71/79 with eight existing failures. The production cases traverse the actual catalog, Core and Buffer factories, Run/roster, generated Chapter 1 entry, battle preparation, deployment, selection/forecast, command and publication. Each Main publishes one typed result and consumes one use. The stage fixture separately verifies repeat entry, next turn, next stage and a genuine new Run.

The final `tested-source.csv` covers 9,973 paths and has SHA-256 `0BB8F42177FAA6BD91A5B726A3FBD8D2ED68EE9B065B45A96C1F365A58A2B1DE`. `phase-f-file-changes.csv` lists 35 source, test, asset and metadata deltas. `final-document-hashes.csv` captures the two post-test documentation changes. The post-run audit found no other source drift or unrelated FMOD, font, pipeline, graphics, scene or View change. No commit or push was performed.

**Phase G intake.** Retain the installed roster, preparation and service contracts. Use the final source and document hashes and exact baseline comparisons before broad verification. G owns the final Core/View contract handoff and full-assembly regression. Unity PlayMode, scene rebinding, actual UI and playback remain unverified. Upgrade acquisition economy and production loadout changes remain outside F.

### 6.9 Phase G end-to-end verification and Core/View delivery

**Authorization/intake.** Astra owned G1 end-to-end in this session with no delegated model review. The user explicitly authorized new tests, related fixes, mandatory full Core/CoreIntegration verification and the English handoff, and confirmed continuation without further questions. All 9,973 Phase F entries matched the actual tree after substituting its final document hashes. Existing A–F and piercing/Dead Hand work was preserved at HEAD `a95ca5a22cde603e136666f6b68b2ea3ce4c9cbc`.

**New acceptance coverage.** `PlayerSkillEndToEndTests` and its `.meta` add ten cases through the real Buffer/Core factories, roster/upgrade API, battle preparation, availability, candidate prefixes, full forecast, command service, result publication and acknowledgment. Six cases cover all six forms. Two cover valid empty casts, one covers Bloom ending the battle during its expanded chain, and one covers stale rejection/bookkeeping with unrelated preview cancellation. The fixture owns its data and intentionally binds its skill variants to a test Main. Actual production Akimbo/Sniper identity and eligibility remain independently covered by the authoring production cases. Full-information checks use the actual command service's assembler to capture identical authoritative input/RNG, compare ordered events/origins, use settlement/outcome, final gameplay state/RNG and character changes, and validate health evidence and occurrence identity. The existing eleven public-disclosure cases cover hidden effects and private RNG separately; public predictions remain conditional, not reservations.

**Runs and correction.** All runs used Unity 6000.0.73f1 via the approved AppData-enabled route and are under `Library/CodexTests/PlayerActiveSkills/phase-g/`.

| Run | Result |
|---|---|
| `g-focused-20261115-084556` | Compilation stopped before NUnit: missing test namespace for StatusEffectKey. Compiler failure list retained; no behavioral RED claim. |
| `g-focused-corrected-20261115-084632` | 21/21 passed: ten new end-to-end and eleven existing disclosure cases. |
| `g-full-20261115-084734` | Required `-assemblyNames "ProjectL.Tests.Core;ProjectL.Tests.CoreIntegration"`: 7,061 selected; 7,003 passed, 44 failed, 14 skipped; exit 2. Core 5,895/5,908 with two failures and eleven skips; CoreIntegration 1,108/1,153 with 42 failures and three skips. All ten G cases passed. |
| `g-corpus-corrected-20261115-085850` | 30/30 passed, exit 0, no skips: all eight newly failing corpus cases, all ten G cases and twelve authoring/production cases. |

The full run's 36 unchanged failures match Phase 0 by qualified name, failure site, message and exact first source location. Four complete stacks differ in deeper callers, without changing the first failing method/location/reason. The eight new failures were one fixture-integration problem: `EnemyStageBehaviorCorpus` uses a fixed Sub Sniper roster but imported the newly Main production role. Its `ProductionStage.Read` now creates a test-only Regular Sniper definition/profile with the same stats, traits and behavior. The production role/asset, eligibility checks, AI algorithms, assertions and frozen expectations remain unchanged. This small fixture adapter correction preserves what the corpus was testing instead of changing its party or weakening runtime validation. `DatabaseBattleInitializationSource` builds these profiles from the same unit definitions, so the replacement preserves their definition values except the explicitly pinned test role.

There was **no second post-fix full run**. The broad and corrective runs together establish the verified scope; do not report a single 7,061-case all-pass run. No new failure is left unresolved. The retained 36 are: CombatCameraZoomPoseTests 1; CombatStructureInspectionTests 1; DataBaseAssetTests 8; DeckMainChangePolicyTests 8; DeckSlotInfoPanelTests 13; EnemyAiTuningAssetTests 1; RunGoldFacilityTests 2; TraitActivationContractTests 1; TraitActivationOutcomeTests 1. Exact cases and evidence are in `known-baseline-failures.csv`, `corrected-regressions.csv` and each run's `case-comparison.csv`/summary. F's prior 262/270 remains its separate scoped result.

**Self-review/locality and delivery.** G modifies only the new acceptance test, its metadata, the narrow corpus test adapter, and documentation. No product code, public contract, state owner, production asset, Framework registration, Job, View, scene or acquisition economy was changed. Understanding scope was the existing service/assembler/publication contracts and the corpus's fixed-party adapter; the full verification scope was explicitly required by G, not an inferred need to refactor unrelated systems. No independent model review, commit or push was performed.

The [Core→View handoff](../handoffs/2026-11-15-player-active-skills-core-view-handoff.md) records actual access paths, signatures/DTOs, six-form call sequences, source/cancellation/result-number contracts, event 51 and common causal playback, projectile versus character movement, upgrade API, operational settings, conditional forecast limits and remaining Unity checks. Unity EditMode compilation/execution is verified; actual UI, scenes, animation, PlayMode and scene-rebind acceptance remain unverified.

The broad manifest SHA-256 is `C0B91A0D368B1EFA5ADE626D284073C25FA7E1728E9EE833A015B3820C9036D4`. The final tested source is `g-corpus-corrected-20261115-085850/tested-source.csv`, SHA-256 `FE9CA0B9C160CAAE3B15CBA54FD6CC28EAD8BFFB251E0B16549F0600A4C1F556`. Final documentation hashes, G-only file deltas, preimages and final source/asset audit are retained separately in the phase-g root. Stop at G1.

## 7. Recommended models and two-session execution

These recommendations are engineering judgments based on this plan's shared-contract risks, not results of a model benchmark on this repository. Keep the user's selected GPT-6.1 variant; the plan does not silently change it. High/xhigh reasoning settings are recommendations, not changes already made to either session.

| Phase | Recommended sole owner | Reasoning | Owner's implementation/verification focus |
|---|---|---|---|
| 0 | GPT-6.1 | high | Source/baseline inventory, confirmed decision mapping, implementation scope. |
| A | GPT-6.1 | high | Single state ownership, immutable copies/equality, Stun and ordinary/skill permission separation. |
| B | Astra | xhigh | Complete simulation versus public visibility, exact hook matrix, health evidence and publication invariants. |
| C | GPT-6.1 | high | Per-burst kill seeds, KillChainAttack priority, expansion lifetime and one-use settlement. |
| D | Astra | xhigh | Relative segment geometry, cumulative encounter budget, structure piercing, incoming-direction terrain interactions. |
| E | Astra | xhigh | Same-source forecast/execution agreement, disclosure policy, stale-response and command handling. |
| F | GPT-6.1 | high | Run/battle reset, upgrade operation, DB/Main eligibility and real registration path. |
| G | Astra | xhigh | Fresh broad evidence, baseline comparison, necessary corrections, and usable View handoff. |

GPT-6.1 owns 0/A/C/F; Astra owns B/D/E/G under this recommendation. Astra implements its assigned Phases rather than acting as a permanent reviewer. Consecutive Phases with the same owner need no model switch, but still retain their user review boundary. Neither max nor ultra is a default requirement.

**Protocol:** The assigned model reads the previous Phase's handoff -> implements the authorized Phase -> self-reviews, tests and fixes it -> updates the execution ledger and reports -> stops for the user's Phase review. The next assigned model takes over only when that Phase is requested. Do not route every Phase through the other model for review, and do not bounce a defect back to the previous model merely because it originally wrote the code.

The execution ledger in section 6 is the single progress source. On each update, include the Phase owner, completed tasks, changed files/contracts, tested source fingerprint, self-review findings/corrections, fresh test evidence, remaining issues, and next action. A separate other-model review report is not a completion condition. Required tests and acceptance criteria still are.

Only the current Phase owner writes product files and the ledger; the other session waits. Serialize Unity runs against the same checkout. At handoff the previous owner stops editing, and the next owner verifies the actual state before continuing. Existing uncommitted work makes HEAD alone insufficient: record pre-Phase file snapshots and tested content hashes. This is a continuation check, not an extra full code-review Phase.

Use the [two-session handoff and starter prompts](../handoffs/2026-11-15-player-active-skills-two-session-handoff.md) for each new or resumed session. These instructions preserve context across chats; they do not create chats or dispatch messages automatically.

