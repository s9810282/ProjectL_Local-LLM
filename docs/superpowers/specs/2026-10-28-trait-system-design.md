# Configurable Trait System Design

Date: 2026-10-28

Status: Review draft. Gameplay decisions recorded below are confirmed by the user; the technical design and implementation stages require document review. No gameplay implementation or runtime verification is claimed.

**2026-11-02 review follow-up** ([follow-up design](2026-11-02-trait-review-followup-design.md)): lines marked "2026-11-02" were changed to match the user's review decisions and the implemented code (trails on passed cells only, Sacrifice owner receives Disarm/Ankle Breaker, untaunted fallback, Kill Chain extra attacks count as actions and recount Focused Bombardment). Confirmed as intended and left unchanged in code: Cannon damage factors also apply to allies hit by friendly fire; Boss Roar counts as an attack action (enemies get no Vanguard/Clean Sweep); Leap still triggers non-mine entry effects on intermediate cells; Ankle Breaker and Rockfall bind share one status key; zero-percent Armor Breaker and Recon exposure entries accumulate.

## 1. Scope and source precedence

Design all 24 traits from the supplied **Project. LIMDUSK — Trait Design Reference**, while scheduling **22 traits for the current implementation scope**. Position Swap and Recon Squad remain documented but deferred until completed-movement undo is restored (historical: both are now implemented with movement undo, `2bd95ad34`). Undo means restoring an already executed move, its action cost, and its effects; cancelling an unsubmitted path is a different operation.

Sources:

- Gameplay reference: [projectl_trait_design_en.md](C:/Users/권천명/Downloads/projectl_trait_design_en.md), initial draft dated 2026-10-28, covering slides 1–29.
- Deliverable requirements: [projectl-trait-design-codex-prompt.md](C:/Users/권천명/Downloads/projectl-trait-design-codex-prompt.md).
- The user's subsequent conversation decisions override the reference and the authoring prompt wherever they resolve an ambiguity.
- Repository instructions: [AGENTS.md](../../../AGENTS.md), [Assets/AGENTS.md](../../../Assets/AGENTS.md), [Framework architecture](../../../Assets/ProjectL/Script/Systems/Documentation/framework-architecture.md), [Framework programmer guide](../../../Assets/ProjectL/Script/Systems/Documentation/framework-programmer-guide.md), and [feature independence criteria, §4.6](../handoffs/2026-09-30-combat-view-design-session-handoff.md#46-기능-변경의-독립성--조립과-기능-구현-분리).
- Inspected checkout: branch feature/Cheon/Core/NewCombatSytem, HEAD e15729850. Code establishes the current implementation; historical design documents do not establish approval of a gameplay rule.

The work product is this design document. Product code, tests, assets, scenes, prefabs, commits, and pushes are outside this documentation task. Future implementation is divided into reviewable stages in §9.

Every trait effect and every stat increase caused by a trait is bounded by the current Run. **No trait contribution carries into a new Run.** Exceptions for particular artifacts belong to the existing artifact policy and do not create trait exceptions. This restriction also applies if a future trait grants permanent-looking stats.

## 2. Confirmed gameplay contract

### 2.1 Shared behavior

- Category membership is editable through SO assets. The source categories are initial assignments, not hard-coded eligibility checks. Common traits initially belong to the Heavy, Rider, and Cannon pools; category-specific traits initially belong to their respective pools. Preserve existing native traits. Main content is a separate configuration decision.
- “All allies” for Vanguard and Morale Boost means living units currently deployed on the field. Exclude hand, waiting, excluded, and dead units. Morale Boost includes its owner; Vanguard excludes its owner.
- Voluntary movement counts as movement. Knockback and Cowboy pull do not count.
- An attack action counts once for Vanguard/Clean Sweep, independently of its target count or number of hits. Separately generated additional attacks retain an explicit attack-action boundary: each Kill Chain extra attack counts as one more attack action, so Clean Sweep can trigger repeatedly while Vanguard still triggers only on the side's first action (2026-11-02). Damage conditions and on-hit effects are evaluated for each sequential strike; see §7.1.
- Additional-attack and reflection kills can trigger Call of Blood, Morale Boost, and Hunter when the existing attribution rules credit the owner.
- Call of Blood and Morale Boost calculate each defeated enemy's contribution separately, round each contribution to the nearest integer with 0.5 rounded up, then sum. Healing is capped by the owner's maximum HP.
- All numeric balance values, including approved concrete values, are editable through SO authoring. Missing n values remain TBD and cannot silently become active zero-value definitions.

### 2.2 Common and Heavy decisions

| Trait | Confirmed decision |
|---|---|
| Morale Boost | Each credited enemy kill grants all living deployed allies ATK equal to 20% of the defeated enemy's **base ATK**, excluding temporary buffs/debuffs and damage multipliers. Gains add together and expire at the end of the current controller-side turn. |
| Disarm / Ankle Breaker | Duration is one affected-side turn occurrence, refreshed rather than added on reapplication. If applied during that side's active turn, it expires at the end of that active turn. Disarm prevents attacking; Ankle Breaker prevents voluntary movement. Neither is consumed by one attempted action. Apply only to the individually designated enemy, including for AoE; no individually designated enemy means no application. When Sacrifice redirects that hit, designation is still tested on the original target but the sacrificing owner (the actual recipient) receives the restriction (2026-11-02). The same holds for every effect attached to the damage — knockback/Impact Bolt push, Intimidation, Hunter's Mark exposure, and the attacker's on-attack item — while Pull stays on the original target. Heavy units carry Steadfast Will as their category default trait, so in practice nobody is pushed. |
| Solo Action | Default shield 10. Any unit in the surrounding eight cells breaks isolation. Leaving isolation disables the shield immediately; returning during the same turn restores its remaining amount, not a fresh 10. Preserve both increases and decreases to that source's budget within the turn. Reset the budget at every allied and enemy turn end. |
| Cohesion | Default footprint is the surrounding eight cells. Leaving range removes the active shield from that source. Within the same turn, re-entry restores only the source's unspent budget. Reset at every allied and enemy turn end. Shields from different sources add together. |
| Sacrifice | Triggers only for damage caused by enemy attacks, only during an enemy turn, at most once per enemy turn per owner. Never relay redirected damage through another Sacrifice. Finishing Shot's instant defeat does not trigger Sacrifice; the original target dies. Selection priority is distance, then Main, then the case leaving the sacrificing Heavy with more HP. Enemy assignment is not planned content; this is not a hard-coded ban in the pure rule. |
| Taunt | If the enemy cannot attack a taunting Heavy, prioritize movement toward a position from which it can attack that Heavy next turn. Preserve ordinary movement, range, wall, and action-cost constraints. If it can neither attack nor reach any taunter (already moved, blocked, or no path), the taunt cannot be honored: choose the ordinary best attack, or the ordinary pursuit target, as if untaunted, and publish no taunt activation (2026-11-02). |
| Entrenchment | Default surrounding eight cells, center excluded, 50% reduction. Applies to all damage. Overlapping reductions multiply. |
| Steadfast Will | Immunity to knockback only. The trait contributes no immunity to Cowboy pull. Existing unrelated behavior-based forced-movement immunity remains its own rule. |

### 2.3 Rider and Cannon decisions

| Trait | Confirmed decision |
|---|---|
| Ground Compaction / Dust Screen | **2026-11-02: only passed cells** — cells the mover entered and then left during the voluntary move. The start cell and the cell where the move stops (destination, or an interrupted stop) get no trail, so a one-step move leaves none; wall/cliff cells are still skipped. (Earlier text included start and destination; a Dust Screen trail on the destination reduced the owner's own range.) No numerical stacking of identical trail effects. Expiry as implemented: Dust Screen at the next opposing-side turn end; Ground Compaction at the creator's next own turn end (changed in code, `dc4614886`). Dust Screen reduces attack damage only. |
| Leap | May pass over units, walls, and cliffs; may finish on any otherwise valid cell except a unit, wall, or cliff. A destination mine can be stepped on and uses its normal arrival behavior. Intermediate mines are ignored. |
| Acceleration | Add ATK for voluntary cells moved; unused gains survive turn boundaries until the owner attacks. Strengthen the entire multi-hit attack and consume the captured bonus after that attack finishes. |
| Stationary Fire | Check voluntary movement in the current own-side turn. Forced displacement does not break the condition. Default additional damage is 100%. |
| Armor Breaker | DEF reductions multiply and last until stage end. Apply a strike's new reduction after that strike's damage. Each hit in a multi-hit behavior applies its own reduction, so the next strike sees the previous strike's reduction. |
| Finishing Shot | Test target current HP **before damage**, strictly below owner ATK × 20%. Ignore the target's remaining shield when the condition is met. Sacrifice does not activate or redirect this instant defeat; the original target dies. HP 30 becoming 15 from normal damage with ATK 100 does not execute in that damage application. A later strike starting at HP 15 tests again and can execute. |
| Tracking Shot | Test current HP before each strike; current HP / maximum HP must be at least 50%, inclusive. |
| Hunter | Select the living field enemy with the highest **current HP**, randomly among ties. Lock the selected ID until death regardless of HP changes. An owner-attributed target kill grants ATK for the current Run and consumes one of the default three successful rewards per stage. Someone else's kill causes reselection without ATK or cap consumption. After the third reward, stop marking for that stage. Owners select independently. Retarget after the current attack action finishes; one AoE action cannot successively reward newly assigned targets. Night end and redeployment do not reset the stage count. |
| Focused Bombardment | Exactly one living enemy unit inside the AoE area at attack start grants a multiplicative 50% additional damage factor. A separately generated attack recounts its own targets: a Kill Chain extra attack always has one target, so it qualifies even when the parent attack hit several enemies (2026-11-02). Structures are excluded from the count. ATK changes occur before damage factors; Stationary Fire, Tracking Shot, and Focused Bombardment factors multiply. |

Hunter is distinct from the Sniper's Focus and the Shotgun's HuntersMark. The source's inconsistent “Focus” label does not rename or replace either existing trait.

## 3. Repository findings and integration points

Paths abbreviated below:

- S = Assets/ProjectL/Script/Systems/
- C = S/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/
- B = S/RuntimeSystem/BufferSystems/

The linked files were inspected. “Existing” describes source behavior, not proof that the corresponding scene or runtime path has been tested.

| Ref | Existing owner and source | Finding and design consequence |
|---|---|---|
| E1 | [UnitTraitAuthoring](../../../Assets/ProjectL/Script/Systems/DataBaseSystem/Data/InGame/Table/Unit/Trait/UnitTraitAuthoring.cs), [TraitSpecAdapter](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CharacterSystem/TraitSpecAdapter.cs) | Serializable authoring produces pure trait Data, then Contracts TraitSpec. Reuse this conversion; preserve SerializeReference type names and serialized identities. |
| E2 | [TraitGrowthDefinitionDataSO](../../../Assets/ProjectL/Script/Systems/DataBaseSystem/Data/InGame/Table/TraitGrowth/Item/TraitGrowthDefinitionDataSO.cs), [CategoryTraitProgressionDataSO](../../../Assets/ProjectL/Script/Systems/DataBaseSystem/Data/InGame/Table/TraitGrowth/Item/CategoryTraitProgressionDataSO.cs), [TraitGrowthDefinitionAdapter](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CharacterSystem/TraitGrowthDefinitionAdapter.cs) | Normal/enhanced definitions and SO pool references already exist. Category membership is edited by moving references between category pools; no second category field is required on the trait definition. |
| E3 | [TraitGrowthRules.EligibleTraits / NextStep](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Roster/TraitGrowthRules.cs), [TraitGrowthCatalogValidator](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Roster/TraitGrowthCatalogValidator.cs) | Eligibility combines native/owned/enhanced traits with configured category pools and attack-profile compatibility. Retain the same eligibility rule for authoring validation and live candidates. Preserve XP/stat-growth and offer/receipt contracts. |
| E4 | [TraitRegistry](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/Traits/TraitRegistry.cs), [CombatCommandEffects.Capture / Apply](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/CombatCommandEffects.cs) | Explicit spec/definition/state/rule registrations and snapshot conversion exist. Add supported types at these boundaries; do not add a separate trait execution service. Apply currently supports only selected mutable trait states and needs deliberate extension. |
| E5 | [CombatEffectCapabilities](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Domain/CombatEffects/Rules/CombatEffectCapabilities.cs), [CombatEffectEngine](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Domain/CombatEffects/CombatEffectEngine.cs) | Pure evaluation, follow-up, movement, and turn capabilities exist. Existing incoming-damage contribution is flat reduction; percentage/aura context needs an explicit extension. Capability existence does not prove controller-turn dispatch. |
| E6 | [CombatTurnCalculator](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/CombatTurnCalculator.cs), [CombatActionAdmission](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/CombatActionAdmission.cs) | Controller-side turn facts, turn-end events, budgets, and action admission are authoritative here. New durations must use these side boundaries, rather than the retired per-unit lifecycle. |
| E7 | [CombatAttackCalculator](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/CombatAttackCalculator.cs), [AttackDirectDamageCalculator](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/AttackDirectDamageCalculator.cs), [FollowUpAttackExecutor](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/FollowUpAttackExecutor.cs) | Admission/cost, delivered hits, and extra attacks have different owners. Direct damage can batch pending health, so sequential strikes must flush state before the next strike's HP/DEF checks. Extra attacks must use the same new hit policies. |
| E8 | [TargetDamagePipeline](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Domain/Combat/TargetDamagePipeline.cs), [CombatDamageCalculator](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/CombatDamageCalculator.cs), [CombatDamageApplication](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/CombatDamageApplication.cs) | Existing target matchup/DEF, received reduction, item hooks, HP application, recovery, and damage-origin events must remain the common damage route. Add policies without duplicating HP/death settlement. |
| E9 | [CombatDamageCredit](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/Rewards/CombatDamageCredit.cs) | Rewards/statistics already attribute direct, additional, and reflection damage. Reuse attribution; do not infer killer from the command actor alone. |
| E10 | [CharacterCombatFacts](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Combat/CharacterCombatFacts.cs), [UnitStatsSnapshot](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Snapshots/UnitStatsSnapshot.cs), [CharacterDebuffState](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Combat/CharacterDebuffState.cs) | Immutable expected/final facts and value comparisons are part of publication. New state requires equality, copy/rebuild, projection, and contract-shape coverage. No numeric shield ledger was found in these inspected contracts; the badge is a different protection mechanic. |
| E11 | [DefenseReductionDebuffState](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Combat/DefenseReductionDebuffState.cs), [CombatDebuffCalculator](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Domain/Combat/CombatDebuffCalculator.cs), [CombatTemporaryStateCleanup](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/CombatTemporaryStateCleanup.cs) | Existing DEF reduction is non-stacking and owns a limited own-turn expiry. Armor Breaker cannot overwrite it. Extend the received-state composition and common death/battle cleanup while preserving that existing effect. |
| E12 | [CombatMovementCalculator](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/CombatMovementCalculator.cs), [MovementPathNormalizer](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/MovementPathNormalizer.cs), [CombatCommandKind](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Combat/CombatCommandInput.cs) | The command set has Move and placement ReturnUnit but no completed-move undo. Normalizer rollback only removes a failed draft branch. Repeated cells are currently rejected; preserve that route rule. |
| E13 | [MovementEnvironmentFacts](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Combat/MovementEnvironmentFacts.cs), [CommandEnvironmentState](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/CommandEnvironmentState.cs), [EnvironmentTerrainRules](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Domain/Movement/EnvironmentTerrainRules.cs) | Typed immutable environment sources, source allocation, visibility, and existing sandstorm/high-ground percentage reductions exist. Add typed transient trail sources under the same environment owner. Do not replace base terrain with a trail. |
| E14 | [EnemyAiTurnService](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/EnemyAi/EnemyAiTurnService.cs), [PullAttackCalculator](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/PullAttackCalculator.cs), [KnockbackImmunityPolicy](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Domain/Combat/KnockbackImmunityPolicy.cs) | Taunt belongs in attack/pursuit policy; movement and spending remain existing owners. Knockback and pull already have separate decision paths. Steadfast must extend only knockback's decision. |
| E15 | [OwnedCharacterRecord](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Roster/OwnedCharacterRecord.cs), [OwnedCharacterChange](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Roster/OwnedCharacterChange.cs), [OwnedCharacterStatsCalculator](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Roster/OwnedCharacterStatsCalculator.cs) | Run-owned identity/level/XP/growth exists but no Hunter ATK field. Add its Run contribution here, preserve it in record rebuilds, and include it in owned/battle stat composition without changing Main's baseline-growth ownership. |
| E16 | [BattleTransactions.Apply](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/Core/BattleTransactions.cs), [CharacterBuffer](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/Character/CharacterBuffer.cs), [RunStartService](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/RunSystem/RunStartService.cs), [ProgressTransactions.PublishNewRun](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/Core/ProgressTransactions.cs) | Prepare/validate all stores before installation; command publication can commit owned changes. New Run publication replaces the roster. Hunter award and stage count must commit atomically; a fresh roster starts with zero trait carryover. |
| E17 | [FocusTraitRule](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Domain/Traits/Rules/FocusTraitRule.cs), [DeferredCombatFeatures](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/DeferredCombatFeatures.cs) | Focus's old rule schedules damage stacks after an unmoved attack and resets on movement. It is currently deferred by the controller-turn migration. Preserve its identity and availability; Hunter does not reactivate or migrate it. |

The source's “existing damage-reduction area” was not identified as a currently reusable named aura in the inspected code. The verified reusable pieces are E5/E8/E13. Entrenchment therefore adds an aura contribution to those owners; it must not claim an unverified legacy area implementation as an existing contract.

## 4. Approach and boundaries

Use the existing trait registry, pure rules/effect engine, command projection, and Buffer publication. Group new pure rules by their real change boundary: attack/kill grants, shields/auras, voluntary movement/trails, and target damage. A trait may have its own typed rule/state where that reduces the amount of unrelated code needed to change it. Uniform class counts are not a goal.

A separate global trait manager or universal effect language would duplicate state and execution ownership. Adding all 22 mechanics as inline conditions in attack/movement/turn coordinators would make unrelated rules inseparable. Both alternatives are rejected in favor of explicit local rule calls and existing composition points.

| Responsibility | Owner and contract |
|---|---|
| Designer definitions / pool membership | Existing InGame SO/Data tables and category pool references. Pure data after GetData. |
| Rule decisions | Core Domain and existing effect capabilities; no Unity/DB/Buffer access. |
| Cross-unit or cross-step orchestration | Existing Core Application attack, damage, movement, and turn owners. Pass the facts needed by the local rule. |
| Authoritative runtime state | Existing Character/Combat/Run-owned Buffer records, through expected/final transitions. A command projection is disposable proposed state. |
| Admission and AI | Shared action admission and targeting/pursuit policy; preview and execution use the same restrictions. |
| Costs, jobs, scene flow | Existing action-budget, Job, and LifeCycle owners. Traits do not run their own frame loop or spend a second action cost. |
| Presentation | Buffer snapshots/events consumed by View. View does not read InGame definitions to calculate trait rules. |

FW-001/002/003 apply when a real service/module is registered; internal trait helpers are not automatically Framework services. Preserve FW-004 state boundaries and FW-016 initialization. Any new UI remains a View handoff through existing registered UI/presentation contracts.

## 5. Trait coverage matrix

Defaults in this table are initial SO values. Detailed tunable names and validation appear in §6. “Battle state” below is stored in existing Buffer facts, never in SO assets.

| ID / trait / slide | Trigger and target | Effect and tuning | Duration / state | Integration / remaining item |
|---|---|---|---|---|
| C1 Vanguard / 선봉장 / 5 | Owner performs the first allied attack action; other living deployed allies | Flat ATK grant, amount TBD | Current turn; source-tagged recipient grants and side attack ordinal | Attack start + turn expiry; E4/E6/E7 |
| C2 Clean Sweep / 깔끔한 뒷정리 / 6 | Another living field ally performs an attack action (each Kill Chain extra attack included, 2026-11-02); owner | Add flat ATK, amount TBD per action | Additive until current turn end | Attack notification + stat composition; E7/E10 |
| C3 Solo Action / 단독 행동 / 7 | Occupancy changes around owner; any unit breaks isolation | Shield 10; square neighbors radius 1, center excluded | Source budget survives leaving/re-entering during turn; reset every side turn end | Presence reconciliation + common shield absorption; E8/E10 |
| C4 Call of Blood / 피의 부름 / 8 | Credited enemy-unit kill; owner | Heal 10% of defeated enemy maximum HP; per-kill half-up | Immediate; clamp to maximum HP, no resurrection | Common kill facts + CharacterSettlementCalculator; E8/E9 |
| C5 Morale Boost / 사기 증진 / 9 | Credited enemy-unit kill; all living deployed allies | Add rounded enemy base ATK × 20% | Additive, current turn end expiry | Kill settlement + source-tagged grants; E9/E10 |
| C6 Disarm / 무장 해제 / 10 | Successful attack contact with individually designated enemy; a Sacrifice owner who takes the hit receives it instead (2026-11-02) | Block attack for 1 affected-side turn | Refresh; active affected turn counts; received status | Designated-target resolution + shared admission; E6/E7 |
| C7 Ankle Breaker / 발목 부수기 / 11 | Same targeting restriction as C6 | Block voluntary move for 1 affected-side turn | Refresh; forced displacement unaffected | Move admission/range/path/AI; E6/E12/E14 |
| H1 Position Swap / 위치 교대 / 12 | Move into allied occupied destination in range | Swap both units; movement allowance remains normal | Must undo both positions and costs/effects coherently | Implemented together with completed-movement undo (`2bd95ad34`); the "deferred" wording elsewhere in this document is historical. |
| H2 Cohesion / 결속 / 13 | Living deployed allies in square neighbors radius 1 | Owner DEF per neighbor TBD; each neighbor shield TBD | DEF derived from occupancy; per-source shield budget/reset; leaving removes active shield | Presence + stat/absorption; E8/E10 |
| H3 Sacrifice / 희생 / 14 | Enemy-turn enemy attack would lethally damage ally within square radius 2 | Redirect one selected hit; shields included; attacker vs sacrificing owner matchup | Default 1 activation per enemy turn; no relay | Incoming attack planning before irreversible HP/shield writes; E7/E8 |
| H4 Taunt / 도발 / 15 | Enemy within square radius 2 | Attack taunter if possible; otherwise prioritize pursuit to next-turn attack position; if neither is possible, act as if untaunted (2026-11-02) | Derived while source is alive, deployed, and in range | Existing AI attack/pursuit policy; E14 |
| H5 Entrenchment / 진지 구축 / 16 | Ally in square neighbors radius 1, center excluded | 50% reduction to all damage; multiply overlaps | Derived aura, ends immediately on lost presence/source | Received-damage stage; E5/E8 |
| H6 Steadfast Will / 굳건한 의지 / 17 | Knockback targets owner | Ignore knockback; no pull immunity | While trait is equipped/available | Knockback-only policy; E14 |
| R1 Ground Compaction / 땅 다지기 / 18 | Passed cells of the voluntary path, excluding start and stop cells (2026-11-02) | Trail grants +1 movement range when movement begins there | Through next opposing turn end; identical effects do not numerically stack | Movement allowance/context + typed environment sources; E12/E13 |
| R2 Dust Screen / 먼지 연막 / 19 | Same trail footprint | Occupant attack range −1; attack damage received −20% | Same expiry as R1; applies to allies and enemies; no identical numerical stacking | Attack queries + damage classification + environment; E8/E13 |
| R3 Leap / 뛰어넘기 / 20 | Owner voluntarily traverses route | Pass units/walls/cliffs; skip intermediate mines; normal destination arrival | Per move; no extra mutable terrain owner | Route admission + traversal/arrival policy; E12/E13 |
| R4 Acceleration / 가속 / 21 | Successful voluntary path steps | Add moved steps × ATK per cell TBD | Keep until next attack; all hits share captured bonus; then consume; stage end clears | Movement success + root attack capture/finish; E7/E12 |
| R5 Recon Squad / 정찰대 / 22 | Cross-shaped neighborhood along voluntary path | Enemy damage received +10% until death; source rollback rules | Needs undo removal and post-damage undo lock | Implemented together with completed-movement undo (`2bd95ad34`); the "deferred" wording elsewhere in this document is historical. |
| A1 Stationary Fire / 고정 사격 / 23 | Attack without voluntary movement this own-side turn | Additional damage 100%, factor 2.0 | Own-turn movement fact; forced movement excluded | Attack factor + movement lifecycle; E7/E12 |
| A2 Armor Breaker / 방어구 부수기 / 24 | Each attack strike contacts enemy unit | DEF reduction rate TBD, multiplicative stacks, applied after current strike | Until stage end; preserve across night/redeployment | New received stack state + effective DEF; E8/E11 |
| A3 Finishing Shot / 마무리 사격 / 25 | Before each strike, current enemy HP < current owner ATK × 20% | Defeat eligible enemy ignoring DEF and remaining shield | Immediate, existing death/attribution route | Pre-strike decision + common death settlement; E7/E8 |
| A4 Tracking Shot / 추적 사격 / 26 | Before each strike, target HP / max HP ≥ 50% | Additional damage rate TBD | Re-evaluate each strike | Target-specific factor; E7/E8 |
| A5 Hunter / 사냥꾼 / 27 | Locked highest-current-HP target dies | Owner kill: Run ATK + TBD, default 3 rewards/stage; other killer: free reselection | Target/count: stage. Earned ATK: Run only | Kill attribution + owned change + command RNG; E9/E15/E16 |
| A6 Focused Bombardment / 일점 포격 / 28 | AoE contains exactly 1 living enemy unit at attack start (a Kill Chain extra attack recounts: 1 target, 2026-11-02) | Additional damage 50%, factor 1.5 | Attack-context fact | Resolved selection/area + multiplier; E7/E8 |

## 6. SO/data schema and designer workflow

### 6.1 Authoring shape

Reuse TraitGrowthDefinitionDataSO.normal/enhanced and the UnitTraitAuthoring family. Add a typed authoring/Data/Spec/Definition for each supported new mechanic, matching existing registration conventions. The following names are **proposed additions**, not APIs already present:

- VanguardTraitAuthoring → VanguardTraitData → VanguardTraitSpec, and corresponding types for the other supported traits.
- New TraitKey members with unused stable serialized numbers. Never reuse an old trait number or rename existing Focus/HuntersMark identities.
- A small pure area value: footprint shape (SquareNeighbors or OrthogonalCross), radiusCells, and includeCenter. Shape and center semantics are prescribed by each trait; only supported numeric area tuning is exposed. There is no arbitrary area scripting language.
- Received effect/state values described in §6.3. They are runtime contracts, not SO fields.

Percentages follow the existing **integer percent convention**: 20 means 20%, not 0.20. Half-up calculations use integer/rational or decimal arithmetic consistently; avoid float comparisons at HP thresholds.

Use one required authoring field per independent magnitude. TBD fields start with an explicit authoring-only unset sentinel, such as −1, and GetData/validation rejects an active definition containing that sentinel with asset/trait/field context. Do not export the sentinel into Core or silently substitute zero. A deliberately configured zero is allowed only where the documented field validation permits it.

No enhancement values have been approved for these new traits. Leave enhanced absent until content supplies a real enhanced definition; acquisition can still be configured. Existing eligibility excludes unavailable/missing enhanced choices.

### 6.2 Numerical inventory

Field names below are proposed lower-camel-case authoring fields. All amounts/counts/radii are int unless stated otherwise. Consumers are the local rules and boundary owners listed in §5.

| Trait | Editable fields and initial values | Units / validation |
|---|---|---|
| Vanguard | attackBonus = TBD; durationControllerTurnEnds = 1 | Flat ATK ≥ 0; required duration ≥ 1. First allied attack is a structural trigger. |
| Clean Sweep | attackBonusPerAllyAttack = TBD; durationControllerTurnEnds = 1 | Flat ATK ≥ 0; duration ≥ 1 |
| Solo Action | shieldAmount = 10; radiusCells = 1; requiredOccupiedNeighborCount = 0; resetEveryControllerTurnEnds = 1 | Shield ≥ 0; radius ≥ 1; count within footprint; reset interval ≥ 1; square/center exclusion retained |
| Call of Blood | enemyMaxHealthHealPercent = 10 | Percent 0–100; use defeated enemy max HP, never owner HP |
| Morale Boost | defeatedEnemyBaseAttackPercent = 20; durationControllerTurnEnds = 1 | Percent ≥ 0; duration ≥ 1; base-ATK input fixed |
| Disarm | durationAffectedSideTurnEnds = 1 | Count ≥ 1; affected-side timing fixed |
| Ankle Breaker | durationAffectedSideTurnEnds = 1 | Count ≥ 1; voluntary-only restriction fixed |
| Position Swap | No independent magnitude; uses the owner's configured movement range/cost | Deferred; do not invent a second range/cost source |
| Cohesion | defensePerAdjacentAlly = TBD; shieldAmountPerAlly = TBD; radiusCells = 1; resetEveryControllerTurnEnds = 1 | Independent DEF/shield magnitudes ≥ 0; radius/reset ≥ 1 |
| Sacrifice | radiusCells = 2; maxTriggersPerEnemyTurn = 1 | Radius/count ≥ 1; square neighbors, enemy-turn basis fixed |
| Taunt | radiusCells = 2 | Radius ≥ 1; square neighbors, center excluded |
| Entrenchment | radiusCells = 1; damageReductionPercent = 50 | Radius ≥ 1; reduction 0–100; square neighbors, center excluded |
| Steadfast Will | No numerical magnitude | Knockback-only capability; no designer switch that silently blocks pull |
| Ground Compaction | movementRangeBonus = 1; opposingTurnEndsToExpire = 1 | Bonus ≥ 0; duration ≥ 1 |
| Dust Screen | attackRangeReduction = 1; attackDamageReductionPercent = 20; opposingTurnEndsToExpire = 1 | Range reduction ≥ 0, effective range floor 0; percent 0–100; duration ≥ 1 |
| Leap | No independent magnitude | Traversal/landing rules; existing movement budget remains authoritative |
| Acceleration | attackPerMovedCell = TBD | Flat ATK per completed voluntary step ≥ 0; no automatic turn expiry |
| Recon Squad | damageTakenIncreasePercent = 10; crossRadiusCells = 1 | Percent ≥ 0; radius ≥ 1; deferred |
| Stationary Fire | extraDamagePercent = 100 | Additional percent ≥ 0; factor = (100 + value) / 100 |
| Armor Breaker | defenseReductionPercentPerHit = TBD | Percent 0–100; multiplicative retained DEF; stage lifetime fixed |
| Finishing Shot | attackThresholdPercent = 20 | Percent ≥ 0; strict less-than and pre-damage timing fixed |
| Tracking Shot | targetHealthThresholdPercent = 50; extraDamagePercent = TBD | HP threshold 0–100 inclusive; extra percent ≥ 0 |
| Hunter | attackPerTargetKill = TBD; maxRewardsPerStage = 3 | Flat ATK ≥ 0; reward count ≥ 1; Run lifetime fixed |
| Focused Bombardment | requiredEnemyCount = 1; extraDamagePercent = 50 | Count ≥ 1 with exact equality; additional percent ≥ 0 |

Changing a number does not change its basis, comparison operator, source ownership, duration basis, or stacking mode. For example, changing Disarm to 2 means two affected-side turn ends; it does not turn into two blocked attack attempts.

### 6.3 Definitions versus runtime state

| Proposed state | Authoritative placement | Content and lifetime |
|---|---|---|
| Typed owner trait status | Existing CharacterCombatFacts.Stats.TraitStatuses path, extended Capture/Apply codecs | Hunter target/reward count; Acceleration held amount; movement-condition facts where not derivable. Per unit and stage. |
| Source-tagged received ATK grants | New immutable received-trait state under CharacterCombatFacts | Source origin, amount, expiry boundary. Vanguard/Clean Sweep/Morale contributions stay distinct from item/soul/position contributions. |
| Shield grants | Same received-trait state | Source origin, budget remaining, eligibility, last reset boundary, expiry/reset policy. Active total is derived. Inactive budget is not a second active shield. |
| Disarm / movement lock | Extend CharacterDebuffState with typed values | Remaining affected-side turn ends, source information needed for refresh/cleanup |
| Armor Breaker stacks | Separate typed member of CharacterDebuffState | Multiplicative reductions until stage end; do not overwrite DefenseReductionDebuffState |
| First-attack turn fact | CombatCommandState/turn-owned immutable extension | Allied attack-action ordinal per side/turn, not one mutable counter in each trait |
| Trail source | Typed MovementTileFact subtype in MovementEnvironmentFacts | Source origin, cell, magnitude, creating side, expiry counter/boundary, allocated source ID |
| Hunter Run ATK | New nonnegative HunterAttackBonus on OwnedCharacterRecord | Keyed by CharacterInstanceId; persists through stages, clears with new roster on new Run |

These are coherent state groups, not a generic key/value effect framework. Add only fields required by the selected implementation stage.

The proposed UnitStatsSnapshot trait contribution is a **derived projection** of received ATK, owner held ATK, and the owned Hunter value. Extend its effective-ATK invariant explicitly instead of disguising trait bonuses as ArtifactAttackBonus or rewriting Stats.AttackPower. MainCharacterPersistentState retains its existing baseline-growth ownership. Every rebuild and CharacterBuffer validation must enforce the same projection; the projection cannot be independently awarded.

Base ATK for Morale Boost means the existing pre-contribution baseline: level-derived base for Sub/enemy, or persistent baseline for Main. Exclude Hunter, soul/artifact, item, position, temporary trait, and debuff contributions. Existing Stats.AttackPower and CharacterBaselineStatsCalculator are the starting integration points; keep base and effective ATK distinguishable.

### 6.4 Designer workflow and validation

1. Create a TraitGrowthDefinitionDataSO using the existing asset menu. Select the supported typed normal authoring payload and fill every required magnitude. Supply a genuine enhanced payload only when designed.
2. Register the definition in the existing trait-growth table/catalog. Add its reference to the desired CategoryTraitProgressionDataSO.pool lists. Common traits initially reference the same definition from all three Sub pools; moving a reference changes membership without copying effect values.
3. Configure acquisition/enhancement steps through existing category SO steps. Pool assignment does not grant a trait immediately and does not replace native traits.
4. GetData creates fresh immutable Data; TraitSpecAdapter and TraitGrowthDefinitionAdapter produce pure contracts. Validate category coverage, duplicate keys, definition/spec pairing, unsupported combinations, missing table references, and availability with the existing shared candidate rules.
5. Start a fresh runtime/battle through the existing initialization path to apply changed definitions. Live SO editing during a battle is outside this design.

Use existing Inspector/SerializeReference tooling first. Add only the minimal typed selector support if the current Inspector cannot instantiate a new payload. No new generic editor or node system is justified.

Validate supported footprints, positive durations, bounds, and missing required values before publication. For deferred Position Swap/Recon, explicit availability excludes them from active acquisition pools; merely registering a descriptive definition must not make them executable. Preserve existing unavailable Focus and other deferred traits. SO/MonoBehaviour moves require preserved .meta GUIDs and MovedFrom where applicable.

## 7. Runtime execution and cleanup

### 7.1 Attack action and strike boundaries

Keep three identities distinct:

- The submitted/root action owns admission, action use, and AP spending.
- A sequential strike reads the current projected HP/DEF/status and resolves its own on-hit/kill effects.
- The existing damage-origin attack group owns existing badge/reflection/attribution semantics. Do not redefine its meaning to implement trait counters.

Vanguard and Clean Sweep count the attack action once. AoE recipients and repeated hit packets do not multiply that notification. Independently scheduled additional attacks notify their own attack boundary. Acceleration captures its current bonus at the root multi-hit action and exposes it to every strike in that action; consuming it after the first strike would violate the confirmed rule. Focused Bombardment captures the living-enemy count once from that action's resolved AoE before the first strike. Repeated strikes reuse the captured qualification; a separately scheduled attack captures its own count.

For each sequential strike:

1. Read current projected facts after previous strikes have settled.
2. Resolve current valid recipients for the action's aim, preserving real line-of-sight, wall, range, and hostility policy. Keep the action-start Focused Bombardment qualification captured above; do not recount after a prior strike kills an AoE recipient.
3. Capture effective owner ATK, target current/max HP, current effective DEF, and applicable damage factors. Evaluate Tracking Shot and Finishing Shot against these pre-damage facts. Use exact comparisons: HP × 100 < ATK × thresholdPercent; HP × 100 ≥ MaxHP × thresholdPercent.
4. Resolve the selected normal-damage or instant-defeat branch through common damage/death orchestration. Normal damage retains existing splash, matchup, physical defense, received reduction, item, shield, HP, and attribution owners. Insert new multiplicative reductions at the declared received-damage stage; do not apply matchup or DEF twice.
5. Apply Armor Breaker's new reduction after this strike's damage, before the next strike starts. If two 50% reductions are applied to DEF 20, subsequent strikes see 10 and then 5. Do not round each stored rate into an absolute DEF penalty.
6. Settle newly attributed kills once, in delivered order, using immutable pre-death facts. Round each Call of Blood/Morale contribution separately. Preserve ordinary death, soul, reward, item, and stage-record settlement.
7. At the attack-action finish boundary, consume captured Acceleration and perform Hunter reselection. Do not retarget between AoE victims.

An unaccepted action changes none of these facts. Previews use the same pure rules and disposable projected state, including RNG state; previews must not consume the committed Hunter tie-break stream or grant ATK.

For Disarm/Ankle Breaker, individual designation maps to **ResolvedAttackSelection.SelectedTargetId**, populated from a Primary recipient by [CombatAttackQueries.Selection](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/CombatAttackQueries.cs). [AttackSelectionResolver](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Domain/Combat/AttackSelectionResolver.cs) gives Single/Cell aiming a PrimaryCell; Direction/All aiming has no individual primary. Apply only when that nonempty ID identifies a living hostile character that receives successful contact. An empty primary cell, a structure primary, or splash-only victim gets no restriction. The role named Designated is not sufficient: Direction/All uses that role for its area recipients without creating an individual target.

### 7.2 Incoming damage, shields, and Sacrifice

Compute active shield total from per-source remaining budgets. Absorb each eligible normal-damage application once at the common application boundary; keep HP loss separate from shield absorption for existing statistics. Different source shields add. Source expiry/removal deletes only that source's active contribution.

Implementation policy proposed for deterministic absorption: earliest expiry first, then stable source-origin order. This is a technical tie-break, not a separately confirmed gameplay preference. For aura shield re-entry, retaining an inactive unspent budget does not mean the unit retains shield protection outside range.

Sacrifice must be planned before irreversible shield consumption/HP writes. Simulate the original recipient's complete incoming-damage result, including its shields and existing defenses; only a lethal result qualifies. Recalculate the redirected attack using the sacrificing owner's matchup, DEF, received reductions, and shields. Do not transfer an already-mitigated scalar and mitigate it again.

For an AoE damage batch, consider qualifying owner/recipient pairs and apply the confirmed priority: distance ascending, recipient Main first, projected surviving HP of the sacrificing owner descending. Proposed distance policy is minimum occupied-cell Manhattan distance, following the existing orthogonal pursuit distance in EnemyAiPolicy; the square radius-2 footprint still decides eligibility independently. Stable recipient/owner IDs settle exact residual ties. Reserve the chosen owner's enemy-turn activation and the recipient so the same hit is redirected only once. A redirected hit cannot enter Sacrifice again.

Finishing Shot's instant defeat does not enter Sacrifice selection or consume a Sacrifice activation. Defeat the original target through the common death/attribution settlement, ignoring its shield as confirmed. The normal-damage branch still uses the ordinary lethal-damage and Sacrifice rules. Keep an instant-defeat cause distinct from a numeric damage packet rather than representing execution as an inflated damage amount.

### 7.3 Presence, terrain, and voluntary movement

Reconcile Solo Action, Cohesion, Entrenchment, and Taunt whenever relevant occupancy changes: placement, voluntary movement, forced movement, recall, arrival, death, or source removal. Ordinary attack grants keep their own expiry even if their original provider later dies; continuous auras require a living deployed source.

Presence queries derive square-neighbor membership from current facts. Do not store a second authoritative map of aura membership. Store only irreducible history such as a shield's spent budget.

Ground Compaction and Dust Screen use new typed transient sources under the existing MovementEnvironmentFacts/CommandEnvironmentState allocation and visibility contract. Create them only on passed cells — when the mover leaves a cell other than the start cell (`TraitTrailCalculator.EmitOnLeave`, 2026-11-02) — exclude walls/cliffs, and retain underlying terrain. Identical trails contribute one magnitude at a cell; multiple source lifetimes remain distinguishable so expiring one does not delete another still-active trail. Proposed arbitration for differently tuned sources of the same trail is the largest active value per field: maximum packed-ground allowance, maximum dust range reduction, and maximum dust attack-damage reduction. Recompute when a source expires, allowing a weaker still-active source to resume. This is a design proposal rather than an additional confirmed balance rule. Different damage reductions multiply, including overlapping Entrenchment sources; identical Dust Screen trails do not numerically stack.

Capture the Ground Compaction allowance at voluntary movement start. Passing over later packed cells does not repeatedly increase the budget. The range query, path context, path validation, preview, and execution must use the same allowance and Leap traversal/landing policy. Leap skips intermediate mines and processes destination arrival normally. Existing repeated-cell rejection remains in force.

Acceleration accumulates actual completed voluntary steps only, including additional voluntary moves in later turns. Interrupted routes contribute only completed steps. Forced displacement contributes zero. Trail creation and movement rewards stay inside the existing accepted movement transition.

Movement undo, refunds, rollback locking, Position Swap, and Recon execution are deferred. Do not introduce an ersatz refund or separate trait rollback store now. The future undo design must capture all accepted movement side effects at the existing command/state boundary before those two traits are enabled.

### 7.4 Turn, stage, and Run boundaries

| Boundary | Required operations |
|---|---|
| Own-side turn start | Initialize the current-side first-attack/movement facts; preserve unrelated side state and Acceleration. Enemy-turn start opens the next Sacrifice allowance. |
| Each side turn end | Expire grants due at that controller boundary; tick affected-side restrictions only for that side; reset Solo Action/Cohesion budgets and re-evaluate eligibility; expire trails due at this opposing-side end. |
| End-of-turn environment actions | Resolve existing tornado/train/damage actions in their documented owner order. Proposed scheduling: trait end expiry/reset follows that side's end actions, so those actions observe the ending turn's protection. Validate this integration in the stage plan. |
| Night end / recall | Remove continuous aura eligibility from recalled units and reconcile field neighbors. Preserve stage-owned Armor Breaker, Hunter count/target history where the target survives, and unused Acceleration of units that stay on the field. A recalled sub unit's unused Acceleration is cleared (`CombatReserveReturnCleanup`; confirmed as intended 2026-11-02). Do not confuse preparation/night sequence with a new stage. |
| Death | Remove active aura/shield contributions as appropriate; retain attribution facts until kill consumers finish; use common cleanup for received effects. No healing resurrects a dead owner. |
| Stage end | Clear stage trait state, received grants/shields/restrictions/Armor Breaker, transient trails, Hunter target/count, and Acceleration. Preserve only accumulated Hunter Run ATK in the Run-owned record. |
| New Run | Replace Run-owned records through existing new-Run publication, setting Hunter ATK and every trait-derived contribution to zero. No static cache, SO field, retained snapshot, or artifact exception may restore a previous Run's trait effect. |

### 7.5 Hunter persistence and atomicity

Add HunterAttackBonus to OwnedCharacterRecord rather than a service-local dictionary or save system. An award is an expected/final OwnedCharacterChange composed with the same command's stage counter/status change and final result. Extend record equality/rebuild paths and change invariants so XP growth preserves Hunter ATK, Hunter awards preserve XP/level/growth, and trait selection preserves earned Run ATK.

Battle/owned-stat projection reads the same owned source. During a command, later strikes read the proposed owned value and derived projection; after publication both become visible atomically. Rejected publication, duplicate command, stale roster, and repeated result consumption cannot award twice. Do not wait until victory to persist a legitimate award from earlier combat.

RunStartService and ProgressTransactions.PublishNewRun already own replacement of the roster. Initialize new records to zero; battle cleanup must not copy a previous record's Hunter value into a new identity. Apply the zero-carryover invariant to future trait fields as well.

## 8. Feature independence and necessary regression scope

| Real change | Expected edit scope | Required understanding | Verification consequence |
|---|---|---|---|
| Morale Boost 20% → another value | One trait definition SO after authoring is implemented | Defeated-enemy base ATK, per-kill rounding, source grant/expiry contract | Conversion/value test and one attributed multi-kill/turn-expiry scenario; no need to study Leap or Hunter internals |
| Disarm duration 1 → 2 | Its SO duration only | Affected-side counter and refresh contract | Active-enemy-turn application, reapplication, both relevant turn ends, AoE designated-only rule; no unrelated AI damage-formula rewrite |
| Change Disarm targeting semantics | Local Disarm rule plus shared targeting contract if necessary | Actual selected enemy identity and received-status admission | Targeting/AI/preview integration must be rechecked because the shared boundary changed |
| Keep Hunter ATK across stages | Owned record/change, projection, battle preparation and new-Run reset contracts | CharacterInstanceId, stage/Run lifetime, publication atomicity | Command/Buffer integration, second battle preparation, new Run, stale/duplicate publication, XP/trait-choice preservation |
| Change a trail's numerical effect | Trail definition and its local movement/range/damage consumer | Source lifetime, identical-effect deduplication, shared movement context | Related environment query/preview/execution integration; no review of Morale's kill formula |
| Add a dependency/registration | Trait registry/adapter/codec and existing composition points | The relevant registration and state conversion contracts | Catalog/contract-shape and affected integration tests; ordinary assembly wiring changes are permitted |

These are expected scopes, not measured implementation savings. Shared damage, turn, targeting, or Buffer contracts legitimately require cross-feature regressions. This design does not waive Core/CoreIntegration or authoring verification and does not claim that fewer files imply better independence.

## 9. Future implementation stages — outline only

This is a dependency outline, not execution authorization or a replacement for a task plan. Stop for review after each authorized stage.

| Stage | Expected file/type changes | Depends on / exit evidence |
|---|---|---|
| A — Definitions and contracts | New typed authoring/Data/Spec/Definition and stable keys; TraitRegistry/trait adapters/codecs; required numerical validation; category-pool availability; received-state/owned-Hunter contract shapes | Review §10 technical policies, including D2, before affected gameplay. No active TBD definitions. Authoring/catalog/shape tests and existing growth contracts remain valid. |
| B — Stat grants, restrictions, turn lifecycle | Local Vanguard/Clean Sweep/Call of Blood/Morale/Disarm/Ankle rules; CombatActionAdmission; explicit controller-turn lifecycle calls; CombatCommandEffects; effective stat projection | A. Confirm target scope, attribution, rounding, source cleanup, duration refresh, and action-count semantics. |
| C — Shields, auras, AI, knockback | Typed shield state and common absorption; presence reconciliation; Cohesion/Entrenchment/Sacrifice/Taunt/Steadfast rules; existing attack/pursuit and knockback-only policy integration | A/B. Integrate damage planning, source overlap, forced-movement presence changes, AI movement, unchanged Cowboy pull behavior, and no Sacrifice for Finishing instant defeat. |
| D — Movement traits | Typed trail facts and environment validation/allocation extensions; allowance/range/context/execution; Leap traversal/landing; Acceleration capture/consume; Stationary Fire movement fact | A/B. Movement preview/execution parity, both endpoints, opposing-turn expiry, arrival hazards, no forced-movement gains. |
| E — Cannon hit policies and Hunter | Armor stack state/effective DEF; shared pre/post-strike rules for all attack deliveries and extra attacks; Hunter owned changes/RNG/projection; Finishing/Tracking/Focused Bombardment | B/C/D. Sequential-strike and instant-defeat tests, atomic persistence across stages, new-Run zero carryover. |
| F — Content and View handoff | Content owners fill TBD SO values/pools; Core publishes status/activation/preview facts; View owners add icons/text/markers and scene/prefab links through existing contracts | Prior stages. Record Core, authoring, View, and manual play evidence separately. |
| Deferred — Completed-move undo, then Swap/Recon | Separate undo design/plan first; subsequently H1/R5 integration and rollback acceptance | User's explicit ordering. This document does not implement or partially enable them. |

Do not reactivate deferred Focus, remove legacy assets, alter unrelated FieldCamera/Architecture, or implement stand-in UI as a side effect. Core implementation belongs to its owner; View/Flow delivery is handed off with concrete public contracts.

## 10. Decision register and remaining review points

The initial 21-question discussion and subsequent corrections are incorporated in §2. This table distinguishes resolved choices, draft technical policies, and genuine remaining cross-mechanic decisions.

| ID | Topic / evidence | Status and recommendation | Impact |
|---|---|---|---|
| R1 | Highest HP, target lock, third-party kills, Run lifetime, stage cap | **Confirmed:** current HP; lock until death; free third-party reselection; Run ATK; 3 successful owner rewards/stage by default | Resolved |
| R2 | Morale formula/lifetime; Armor expiry; aura/trail/status details | **Confirmed:** see §2, including base ATK and all new-Run resets | Resolved |
| R3 | Multi-hit timing versus action count | **Recorded interpretation:** one attack-action notification for Vanguard/Clean Sweep; each sequential strike re-evaluates damage and applies post-hit effects; Acceleration covers the whole action. This preserves both the explicit one-count answer and the later sequential-hit clarification. | Review this distinction in the document; do not collapse action and strike IDs |
| R4 | Unknown n/default values | **Balance pending:** Vanguard ATK; Clean Sweep ATK; Cohesion DEF and shield separately; Acceleration ATK/cell; Armor reduction; Tracking extra damage; Hunter ATK | Does not block schema/rules design; blocks enabling those definitions as usable content |
| R5 | Main pools and enhanced variants | **Content pending:** use existing configurable pool/step and enhancement paths; invent no Main candidates or enhanced values | Blocks only that content's activation |
| R6 | Swap/Recon and rollback edge cases | **Deferred by user:** restore completed-movement undo first; then resolve detailed source rollback/Recon starting-area and damage-lock semantics | Outside current 22-trait implementation scope |
| D1 | Finishing instant defeat versus Sacrifice | **Confirmed by the user's final clarification:** Sacrifice does not activate; the original target dies. Preserve the instant-defeat cause, shield bypass, and normal death attribution. Do not consume a Sacrifice activation. | Resolved; cover in incoming-damage and Cannon integration acceptance |
| D2 | Sacrifice priority when several sources and recipients tie | **Draft technical policy:** rank owner/recipient pairs by the confirmed distance → recipient Main → sacrificing owner's remaining HP order, then stable IDs; reserve one source use and one recipient hit. | Requires document review; avoids adding an unapproved new distance or random priority |
| T1 | Same-turn shield re-entry and source removal | **Confirmed budgets; technical tie-break proposed:** earliest expiry then stable origin for absorption; no shared mutable SO budget | Acceptance must check that source removal cannot delete another source's remaining shield |
| T2 | Existing damage-reduction-area implementation | **Source limitation:** only flat incoming reduction and terrain percentage pipeline verified. Reuse these extension boundaries rather than claiming a named legacy aura was found. | Technical investigation complete enough for proposed boundary; no legacy behavior rewrite authorized |
| T3 | End-of-turn scheduling and exact residual ties | **Draft technical policy:** retain existing environment action order, then perform due trait cleanup/reset; use stable IDs for otherwise exact ties | Document review and integration checks, not a new scheduler |
| T4 | Allied summons / multi-cell support | **Implementation policy:** same owner + alive + Field defines an ally, including a summon. Preserve existing unsupported multi-cell boundaries until a footprint contact rule is designed; do not silently accept ambiguous partial-cell interactions. | Content/compatibility validation; no new multi-cell system in this task |
| T5 | Differently tuned copies of the same trail | **Draft technical policy:** use the largest active value per numerical field, retain each source's lifetime, and reveal weaker values when stronger sources expire. Ordinary identical-source stacking remains disabled as confirmed. | Review this arbitration before enhanced/differently tuned trail content is enabled |

The gameplay questions discussed in this session, including D1, are resolved. The draft still requires technical-design review, an authorized task plan, and the required balance/content values before operational activation. Pending balance values do not block writing pure rules or schemas, but they must not become active fallback values. None of these items authorizes reopening an already confirmed gameplay choice.

## 11. Acceptance and verification plan

This section specifies future outcomes. No tests were implemented or executed for this documentation task.

| Area | Required scenarios and expected outcome |
|---|---|
| Authoring and eligibility | Edit a magnitude and category pool reference only in SO assets; new initialization uses the changed value/membership. Common pools share one definition. TBD/missing references fail with asset/trait/field context. Normal/enhanced identity and candidate validation use existing shared rules. |
| State isolation / publication | Two units share one definition but have independent targets, shield budgets, triggers, and durations. Rejected/stale/duplicate commands do not consume or grant state. Preview does not mutate Buffer, SO, RNG, or Run ATK. New fields participate in equality, copying, and contract-shape tests. |
| Common attack grants | First allied action triggers Vanguard once and excludes owner; each other ally action triggers Clean Sweep once, and each Kill Chain extra attack is one more action. AoE and repeated hits do not multiply action notifications. Morale stacks from each credited kill and expires only its own grants at current turn end. Hand/dead units receive no grant. |
| Rounding / attribution | Multi-kill contributions round separately before summing. Morale ignores temporary enemy ATK changes and uses base ATK. Additional/reflection kills retain actual credit. Call of Blood clamps healing and does not resurrect a dead owner. |
| Restriction timing / targeting | Application in allied turn covers the coming affected-side turn; application during enemy turn expires at that active enemy turn end. Refresh does not add durations. Only the designated enemy is restricted in AoE; Cannon without such a target applies neither effect. Attack/move admission, availability, AI, preview, and execution agree; forced displacement remains allowed. |
| Shield lifecycle | Solo Action 10 → damage 4 → lose isolation → regain isolation gives 6, not 10. At the next reset boundary its budget returns to 10 only if active eligibility allows protection. Cohesion range exit removes only that source's protection, same-turn re-entry does not refill, multiple sources add, source death and turn reset remain distinct. |
| Sacrifice / Taunt | Enemy attack lethal test includes original shields; transfer recalculates attacker-versus-sacrificer matchup/defenses once. Per-enemy-turn cap, distance/Main/surviving-HP priority, no relay, source competition, AoE, and source death are exercised. Finishing instant defeat kills the original target despite a nearby available Sacrifice owner, leaves that owner unchanged, and does not consume its activation. A non-executing strike still follows normal Sacrifice rules. Taunted enemies attack a valid taunter or prioritize legal approach to a next-turn attack position; with neither possible they act as if untaunted. Disarm/Ankle Breaker on a redirected hit land on the sacrificing owner. |
| Reduction and knockback | Entrenchment covers eight neighbors and excludes center; apply to attack/environment/reflection numeric damage. Dust Screen reduces attack damage only. Overlaps multiply; identical trail effects do not numerically stack. Steadfast blocks knockback while the same Cowboy pull remains unaffected by this trait. |
| Movement / geometry | Square radius 1 yields 8 neighbors, radius 2 yields 24; center exclusion is explicit. Trails cover passed cells only (no start or stop cell), preserve base terrain, exclude walls/cliffs, last through the next opposing turn end, and affect both sides. Range/context/preview/execution agree. Leap crosses blocked intermediates, cannot land on unit/wall/cliff, ignores intermediate mines, and triggers normal destination arrival. |
| Acceleration / Stationary Fire | Voluntary completed steps add Acceleration; force movement adds zero; unused bonus survives turn boundaries within stage (a recalled sub unit loses it at night end), strengthens every strike of the next action, then clears. Stationary condition resets each own turn and is not broken by force movement. |
| Cannon hit ordering | DEF 20 with 50% Armor Breaker is used as 20 on first strike and 10 on the next, then becomes 5 after the second application. Reduction survives turns/night and ends with stage. Finishing ATK 100, pre-hit HP 30, post-hit HP 15 does not execute on that strike; a later strike at HP 15 can. Exactly HP 20 fails strict threshold. Shields cannot save a valid Finishing target. Tracking at exactly 50% qualifies, and later strikes recheck current HP. |
| Damage factors / AoE count | Stationary +100%, Tracking configured rate, and Focused +50% multiply after ATK composition. Focused counts living enemy units at attack start, not structures; exactly the configured count qualifies. |
| Hunter persistence | Highest-current-HP ties use committed deterministic RNG. HP changes do not retarget. Another killer gives no ATK/cap use. Three owner rewards stop marking, while no AoE retarget chain awards extra. Same stage night/redeployment preserves cap. A new stage resets cap and preserves Run ATK. A new Run resets every trait effect/stat contribution, including Hunter, regardless of previous stage outcome. XP/trait selection/record rebuilding cannot erase or duplicate an award. |
| Regression and handoff | Run appropriate Core and CoreIntegration suites plus meaningful authoring/contract tests for each stage. Existing candidates include TraitGrowthRulesTests, CombatEffectEngineTests, CombatDamageBoundaryTests, PlayerTurnMovementTests, PullBoundaryRegressionTests, KnockbackImmunityPolicyTests, OwnedCharacterStatsCalculatorTests, and corresponding Buffer transaction integration tests. Record actual results, View/Flow integration, and manual play separately. |

When Unity execution is required in a future implementation stage, use the approved execution route with AppData access per repository instructions. The known sandbox CurlRequestCache.db failure is not a reason to upgrade Unity or delete caches.
