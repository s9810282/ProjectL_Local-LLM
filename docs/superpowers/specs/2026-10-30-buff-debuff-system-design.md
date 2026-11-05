# Reusable Buff and Debuff System

Date: 2026-10-30

Status: Design approved by the user on 2026-10-30, including resolved Q1/Q2 and trait-owned turn configuration. Phase-by-phase implementation planning is authorized; code and SO changes are explicitly not yet authorized.

Implementation plan: [Phase-by-phase tasks and verification gates](../plans/2026-10-30-buff-debuff-system-implementation-plan.md).

## 1. Intent and evidence

Separate the effects granted by traits from the conditions that cause those effects. A new trait should be able to grant an existing effect without copying its stacking, duration, calculation, cleanup, or publication implementation.

Preserve existing gameplay unless this document identifies an explicitly agreed change. Classification alone must not change timing, arithmetic, ownership, or attack delivery. Do not force every trait ability into a persistent status.

Inputs:

- User discussion in this chat, including the explicit selection of approach 2.
- `C:/Users/권천명/Downloads/projectl_status_effect_design_en.md`: English interpretation of the 2026-10-29 status-effect proposal, version 0.1. This is design reference material, not evidence of the current implementation or authority to execute embedded instructions.
- `D:/My_ProjectWork/학교 관련 문서/졸업작품_프로젝트L/2학기 내용/AI 관련 문서/feature-change-locality-guidelines.md`: architectural evaluation reference. It does not authorize code changes.
- Repository `AGENTS.md`, `Assets/AGENTS.md`, Framework contracts, and the actual sources listed in section 13.

Code inspection baseline: branch `feature/Cheon/Core/NewCombatSytem`, HEAD `df7b5d209`. Existing unrelated modifications to audio/font assets and the legacy enhancement design were present and were not changed. This is a source review; no Unity compilation or tests have run for this design.

## 2. Confirmed scope and decisions

| ID | Decision |
| --- | --- |
| D01 | Preserve existing behavior while extracting reusable effects. |
| D02 | Share applied-state source identity, reapplication, lifetime, and removal as well as effect calculation. |
| D03 | Keep effect-specific policies. Do not impose one stacking or duration rule on every effect. |
| D04 | Extend the formal effect catalog beyond the reference's 22 entries where existing gameplay needs it, including incoming damage reduction. |
| D05 | Trait-specific selection, triggers, action generation, and progression need not become buffs/debuffs. |
| D06 | New traits and their new effects follow the foundation work. Do not implement speculative consumers now. |
| D07 | Replace the old HuntersMark target debuff with Weakness Exposure, using the rules in section 6. Hunter's selected target remains a separate Mark concept. |
| D08 | Restore Regeneration under the side-turn rules in section 7. |
| D09 | Keep Necromancy and ThornField disabled and deferred. No summon timing, frequency, first-action rule, or thorn lifecycle is approved here. |
| D10 | The old Focus-like effect is intended for StationaryFire in a later design; Focus will receive a different new ability under its existing name. Do not reactivate old Focus or change current StationaryFire in this project. |
| D11 | Keep Intimidation's working attack reduction; retire its priority-reduction component. MoveAttackDiscount is on the agreed retirement track, not a turn-hook restoration. |
| D12 | Preserve the six already-retired traits and their reserved serialized identifiers. Do not reactivate them. |
| D13 | User performs commits and pushes. This design grants no implementation, commit, push, or external handoff-delivery authorization. |
| D14 | Weakness Exposure initially lasts one affected-side turn end. Its duration is editable in the granting trait's SO authoring, not hardcoded in the shared effect. |
| D15 | Clear Weakness Exposure when its target returns to EnemyWaiting at night end, just as on return to Hand. |
| D16 | Turn-based durations, expiry counts, and periodic intervals are configurable per producer trait. Reuse existing fields where present; preserve the clock meaning and existing defaults. Section 5.5 defines the scope. |
| D17 | The user is the Core programmer. This work implements Core/Buffer/InGame contracts and prepares a View handoff; View/UI/presentation DB implementation belongs to the other worker. Core completion and integrated game acceptance are separate gates. |

### 2.1 Explicit behavior changes

Only these gameplay changes are included:

1. HuntersMark grants the new turn-based Weakness Exposure instead of a first-source, eligible-hit-consumed mark.
2. Regeneration becomes active with the agreed side-turn healing rule and an editable interval initially set to one own-side turn end.
3. Deferred priority reduction and the obsolete attack-cost discount are retired. Removing their inactive runtime paths must not change current active combat.

Other existing trait behavior is preserved, including Hunter rewards, FinishingShot thresholds and instant-defeat behavior, Sacrifice, shield budgets, attack grants, movement trails, and current StationaryFire.

### 2.2 Ownership and exclusions

The user's work is Core calculation/contracts, InGame authoring, and the existing Buffer publication integration. Prepare the required View read contract, migration instructions, reproducible examples, and evidence as a handoff. Do not edit ViewSystem, UI presentation, View DB types/assets/registration, or their consumer tests to complete this Core plan.

Core implementation and its own gates may finish before the View worker implements the handoff. Keep the existing project compiling through minimal inert/read-only contract compatibility when needed; do not fabricate old one-hit-mark behavior from new multi-source statuses. Report Core as complete only on its own evidence and View integration as pending. Whole-game release/acceptance still requires the View worker's correct consumption and presentation. A prepared handoff may be complete while its implementation is pending; external delivery requires separate authorization.

Excluded: new trait gameplay, new Focus, StationaryFire redesign, Necromancy/ThornField restoration, general priority-stat removal, flag pickup redesign, unrelated item/artifact/category migration, new animation assets, and FieldCamera/Architecture changes. Existing Missing Script assets are not deleted.

## 3. Approaches considered

| Approach | Benefits | Costs | Decision |
| --- | --- | --- | --- |
| 1. Shared calculations only | Smallest immediate state migration; reuse existing numerical routines. | Each additional producer still needs bespoke application, expiration, and removal wiring. | Not selected. |
| 2. Shared applied-state contract and lifecycle, with typed effect policies | Existing and future traits can share a complete effect contract, including provenance and expiry. | Requires deliberate migration of immutable facts, authoring, result events, and consumers. | Selected by the user. |

A parallel status runtime, universal scripting language, or event bus is not needed. Extend the existing `CombatEffectEngine` and command-transition architecture; do not duplicate them.

## 4. Classification and migration inventory

Formal classification describes the effect, not necessarily a stored timer. Use four representations:

1. **Applied status:** persistent, recipient-owned effect instances such as attack grants, shields, Weakness Exposure, and restrictions.
2. **Derived contribution:** recomputed from current authoritative facts, such as nearby-ally defense or terrain range modifiers. It may have a buff/debuff identity for reading and presentation without acquiring a second stored source of truth.
3. **Instant result:** healing and forced movement use their existing settlement pipelines.
4. **Trait behavior:** targeting, summons, follow-up attacks, reactions, counters, and persistent progression remain with their existing owners.

### 4.1 Existing effect families in scope

| Effect family | Current producers | Representation and preservation requirements |
| --- | --- | --- |
| Attack increase | Vanguard, CleanSweep, MoraleBoost | Applied grants; retain additive grants and each grant's controller-turn-end duration, including multiple grants from one source. Do not replace these with Weakness Exposure's max policy. |
| Attack increase held for an attack | Acceleration | Trait owns movement accumulation and attack-scope consumption. Reuse the attack-contribution contract without copying the held value into another mutable status. |
| Attack decrease | Intimidation | Applied status; retain first applicable reduction and actual-attacker completion consumption. Ordinary, extra, and reaction completion semantics must be characterized from the existing paths. No priority penalty. |
| Defense increase | Cohesion | Derived from current living neighbors. No independent timer or stored duplicate of the neighbor-derived rule. |
| Defense decrease | ArmorBreaker | Applied multiplicative stacks, preserving stack provenance, ordering, rounding, and existing cleanup. Do not collapse repeated same-source stacks. Deferred thorn defense reduction is not reactivated. |
| Outgoing damage increase | StationaryFire, TrackingShot, FocusedBombardment | Conditions and capture scopes remain trait-owned; reuse typed damage contributions and preserve existing factor arithmetic. A target-specific condition is not a universally active actor buff. |
| Incoming damage decrease | Entrenchment, DustScreen | Derived contributions; preserve eligible recipients, damage-kind coverage, and present rounding. Add this family to the formal catalog even though absent from the supplied list. |
| Shield | SoloAction, Cohesion | Applied, source-specific remaining budgets with conditional eligibility and existing periodic reset. Leaving/reentering eligibility must not refill spent budgets. |
| Movement range increase | GroundCompaction | Terrain-owned, derived contribution. Keep trail creation/expiry in the environment owner. |
| Attack range decrease | DustScreen | Terrain-owned, derived contribution. Do not copy the trail lifetime onto every occupant. |
| Attack prohibition | Disarm | Applied restriction; refresh behavior, target-side turn expiry, application targeting, and reaction suppression remain unchanged. |
| Voluntary movement prohibition | AnkleBreaker | Applied restriction; retain forced movement permission. Formal display name may be Bind without renaming the producer trait. |
| Knockback immunity | SteadfastWill | Derived capability; retain existing knockback-specific coverage. Do not grant generic displacement immunity. |
| Weakness Exposure | HuntersMark initially | New applied debuff with section 6's explicitly changed gameplay. |
| Healing | CallOfBlood, restored Regeneration | Instant result through the common recovery/health settlement. Keep CallOfBlood's defeated-enemy maximum-HP basis. |

Sacrifice and Taunt retain their protection/targeting rules. Their eligibility can be represented for display if needed, but this work does not introduce independent timed Sacrifice, Taunter, or Taunt states. Hunter's Mark is a selected-target relation, and its attack reward remains owned by the roster/Run record. FinishingShot remains an attack-time execution rule. Additional attacks, Roar reactions, Pull, Knockback, ImpactBolt, Leap traversal, and trail production are not forced into recipient status storage.

The old Focus implementation and all deferred/retired features are not counted as active migrated consumers.

### 4.2 Reconciliation with all 22 reference effects

| Reference effect | Resolution for this project |
| --- | --- |
| Attack Power Increase | Existing producer policies retained; do not turn permanent growth or attack-consumed bonuses into generic turn timers. |
| Defense Increase | Existing Cohesion contribution retained. New freely grantable timed variants wait for their consumers. |
| Movement Range Increase | GroundCompaction remains terrain-owned. Generic timed grants are future work. |
| Attack Range Increase | Pull selection extension is not a generic attack-range buff. New effect deferred. |
| Weakness Discovery | Reuse damage-increase contributions where applicable; preserve trait conditions rather than manufacturing persistent timers. |
| Shield | Preserve per-source budget, eligibility, and refresh behavior not specified in the reference. |
| Knockback Immunity | Preserve current immunity scope. |
| Sacrifice | Preserve enemy-side-only protection, range, source selection, and trigger limit. |
| Taunter | Current Taunt trait presence/area remains the source. No new timed buff. |
| Perfect Step | Deferred. Leap skips mines on intermediate movement cells, not the destination; it is not an equivalent effect. |
| Attack Power Decrease | Preserve Intimidation's attack-consumed lifetime rather than imposing reference turn duration. |
| Defense Decrease | Preserve ArmorBreaker stack semantics; ThornField remains deferred. |
| Movement Range Decrease | New timed numerical effect deferred; do not alias it to Bind. |
| Attack Range Decrease | Preserve DustScreen's terrain condition. |
| Weakness Exposure | Adopt the agreed all-damage, turn-based status in section 6. |
| Disarm | Preserve existing affected-side expiry and refresh semantics. |
| Bind | Preserve voluntary movement restriction; forced displacement stays allowed. |
| Taunt | Preserve current AI area and pursuit behavior. No new timed victim debuff. |
| Mark | Belongs to Hunter, not Focus or HuntersMark. Preserve highest-current-HP selection, tie RNG, target retention, credited-kill requirement, reward cap, and Run growth. |
| Execution | Preserve strict `current HP < attacker ATK * threshold / 100` comparison and existing instant-defeat bypasses. Do not change to the reference's inclusive unspecified threshold. |
| Healing | Preserve each producer's magnitude basis; restore Regeneration only as specified below. |
| Forced Movement / position swap | A two-character swap is new gameplay and remains deferred. Existing displacement results remain in the movement pipeline. |

## 5. Shared status architecture

Names below describe proposed contracts, not an instruction to create one file, interface, or service per name. The builder should use the smallest typed implementation that satisfies these boundaries.

### 5.1 One state owner

- Canonical applied statuses are immutable values inside the existing `CharacterCombatFacts` lifecycle and CharacterBuffer state. Command projections produce replacement values; only the existing transaction installs them.
- Introduce one typed applied-status collection at that boundary. Migrate existing canonical attack grants, shield budgets, and active debuffs into it in coherent increments. Never keep old and new independently writable copies of the same fact.
- Trait counters and state such as Hunter's selected target, Acceleration's held bonus, and StationaryFire's moved flag remain with the trait. Derived stat totals and presentation records are explicit projections, not alternative state owners.
- Preserve `CharacterCombatFacts` copying, equality, expected-state validation, preview isolation, and snapshot immutability. A stale status change must reject the whole publication before any store is installed.
- Do not store live source-object references inside an applied status. Source death and source-field departure must not invalidate Weakness Exposure.

### 5.2 Definition, source, and instance are different identities

An effect has a stable effect key independent of `TraitKey`, a classification, typed payload, and a policy defining aggregation/reapplication/lifetime/removal. A trait grants it with a target and provenance.

Reuse `EffectOriginKey` for provenance where it fits. Weakness Exposure's logical reapplication identity is `(recipient, effect key, applying battle character, applying trait)`. Different callbacks or rule keys within the same trait must not accidentally create separate Weakness Exposure sources. Different traits on one character and the same trait on different characters remain independent.

Other policies can retain multiple instances from the same source: additive attack grants and ArmorBreaker stacks require that. Do not make `(source, effect)` globally unique just because Weakness Exposure uses replacement.

Use existing deterministic command/result ordering and transaction-local instance identity where multiple instances need it. No global mutable counter outside the transaction. Stable serialized effect IDs must be assigned once during implementation and tested for uniqueness; never reuse retired trait or effect IDs.

### 5.3 Responsibilities

| Owner | Responsibility | Must not own |
| --- | --- | --- |
| Trait rule/application | Trigger, eligible targets, magnitude basis, requested lifetime, trait counters and capture scope | Duplicate status stacking/expiry code |
| Common status policy | Validate requests; add/replace/aggregate/consume/expire/remove typed states | Trait-specific enemy selection, attacks, movement, rewards |
| Existing combat calculators/engine | Apply effective stat/capability/damage contributions at their established boundaries | A second mutable status store |
| Existing turn/attack/placement orchestration | Supply explicit lifecycle boundaries once | Reimplement each effect's expiry policy |
| Buffer transaction | Validate expected facts, prepare all stores, install atomically, publish the existing result | Gameplay timers or effects |
| View | Read immutable state/events and render by effect identity | Recompute magnitude, select a source, decrement timers, or infer states from trait names |

The same key can support compatible typed policies only when the resulting rules remain unambiguous. Do not reduce incompatible payloads to a dictionary of arbitrary strings or a universal numeric field. A shared recipient-status contract does not require a new Framework service or registration per effect.

### 5.4 Explicit lifecycle inputs

The needed boundaries are already present: controller-side start/end, completed attack scope, successful placement/return, eligibility reconciliation, death, and battle completion. Supply target side, ended side, and current sequence explicitly. Preserve effect-specific clocks:

- Existing attack grants decrement at every controller turn end.
- Disarm/Bind and Weakness Exposure use affected-side turn ends.
- Intimidation is consumed by its established attack-completion boundary.
- Shield resets use the existing completed-controller-turn budget rule.
- Conditional contributions follow current eligibility, not elapsed turns.
- Run growth does not enter the status lifetime mechanism.

Preview, rejected commands, range queries, HUD refresh, and result playback never advance these clocks. No elapsed-time timers, frame polling, background jobs, or event subscriptions are needed.

### 5.5 Trait-owned numerical configuration

The shared effect owns the meaning of its policies; each producer owns the requested magnitude and numerical duration/interval. There is no global duration for Weakness Exposure or other reusable effects. Two different traits may apply the same effect with different durations without changing the effect implementation.

Use the existing `UnitTraitAuthoring` input carried by `UnitDataSO` and by `TraitGrowthDefinitionDataSO.normal/enhanced`. The growth definition is the existing SO entry point for configuring acquired traits. This request does not require another asset type per buff, another balance table, or a per-trait Framework service. Do not move unrelated unit/growth authoring to a new ownership model.

The value flow is SO trait authoring -> fresh immutable `UnitTraitData` -> `TraitSpecAdapter` -> typed spec/definition -> grant or producer cadence. Normal and enhanced definitions can have different numerical values through their existing authoring slots. Every conversion must preserve the configured values. The shared runtime must not consult a live SO or contain a fallback `1` for a missing grant duration.

| Producer | Editable quantity | Clock and current default behavior | Required work |
| --- | --- | --- | --- |
| HuntersMark -> Weakness Exposure | `DurationAffectedSideTurnEnds` | Recipient-side ends; initially 1, including the current affected-side turn | Add authoring/data/spec/definition field and explicitly migrate existing applicable assets to 1. Preserve existing percentages. |
| Vanguard, CleanSweep, MoraleBoost | Existing `DurationControllerTurnEnds` | All controller-side ends, not only the recipient's side | Reuse existing configuration and keep each asset's value. |
| Disarm, AnkleBreaker | Existing `DurationAffectedSideTurnEnds` | Recipient-side ends | Reuse existing configuration and refresh policy. |
| SoloAction, Cohesion | Existing `ResetEveryControllerTurnEnds` | Existing controller-turn budget-reset boundaries | Reuse existing reset interval; do not reinterpret it as shield expiration. |
| GroundCompaction, DustScreen | Existing `OpposingTurnEndsToExpire` | Opposing-side ends for the trail's source side | Reuse existing terrain-owned expiry; do not create recipient timers. |
| Regeneration | `HealEveryOwnSideTurnEnds` plus existing `HealAmount` | Eligible own-side ends; initially 1 | Add per-trait cadence configuration and the narrowly scoped producer state described in section 7. |
| Sacrifice | Existing `MaxTriggersPerEnemyTurn` | Per-enemy-turn allowance | Keep configurable as already authored; it is a trigger limit, not a duration. |
| Deferred ThornField/Necromancy/new Focus | Any future turn counts belong in their own trait authoring | Timing semantics are not approved here | Leave disabled. Do not convert the obsolete unit-turn/FirstActionDelay settings into guessed side-turn behavior. |

Duration and interval values must be positive integers, validated in pure data/spec creation as well as constrained in the Inspector. Zero must not silently mean permanent, disabled, or unlimited. Instant effects, attack-consumed effects, conditional eligibility, and Run growth retain their explicit non-duration semantics. In particular, do not add invented timers to Intimidation, Hunter growth, or StationaryFire's moved-this-turn predicate.

Engineering lifecycle choices: authored configuration is captured when the runtime definition is built; an individual grant captures its magnitude and remaining duration when applied. Changing an asset does not retroactively edit installed statuses or a running battle definition. A newly built definition/new grant uses the new value. If an existing supported gameplay upgrade changes a producer definition, old grants retain their captured values until normal replacement/expiry; a same-source Weakness Exposure reapplication uses the new requested values. No new live-reload mechanism is introduced.

Initialize currently authored HuntersMark definitions to duration 1, including already-existing normal/enhanced entries if present. Preserve their percentage differences and do not create missing enhanced definitions or populate unrelated growth pools. Defaults are authoring/migration choices, not hidden runtime fallbacks. Balance changes should require editing the relevant trait definition only, not another trait's code or common lifecycle implementation.

## 6. Weakness Exposure

### 6.1 Agreed behavior

- Debuff: damage received increases by the configured nonnegative integer percentage for a configured positive number of affected-side turn ends.
- Applies to ordinary, extra, reaction, mine, field/environment, and reflected **numeric damage**, regardless of who caused the damage. Applying a status and benefiting from an existing status are different rules.
- Each applying character + applying trait has its own magnitude and remaining duration on the target.
- Use only the largest percentage among active sources. Lower contributions keep counting down while suppressed and can become effective later.
- Reapplication from the same logical source replaces both magnitude and remaining duration with the incoming values, even if weaker or shorter. It does not add time or retain the old larger number.
- At the affected side's turn end, decrement each instance once; remove at zero. If applied during that side's turn, that ending counts immediately. Expiry of one source never removes unrelated sources.
- Source death or departure does not remove already applied instances.
- Target return to Hand or night-end return to EnemyWaiting removes all Weakness Exposure instances immediately. This covers manual Hand return and automatic night recall. Redeployment does not restore them. A character that stays on Field, including a persistent Main character, retains the normal expiry policy.
- Target death and battle completion clear the effect through existing cleanup ownership.
- HUD representation: a broken-shield icon. This is distinct from Hunter's Mark and the old HuntersMark icon/meaning.

Examples: 20% with two affected-side ends remaining plus 30% with one end remaining gives 30% now, then 20% after the next affected-side end. A new 10%/three-turn grant from the 30% source replaces that source with 10%/three turns; the independent 20% source can then be the maximum.

### 6.2 Damage integration — engineering choices for review

These are explicit engineering choices, not claims that the user individually selected every pipeline detail:

1. Apply the multiplier once to the **actual numeric-damage recipient**, after Sacrifice chooses that recipient. An exposure on the original protected target is not also applied to its protector. Preserve all other Sacrifice rules.
2. Use one common evaluation at the established numeric-damage application boundary, after existing non-item damage calculation/held-defense stages and before item `BeforeDamage`, shield absorption, and HP loss. This preserves other paths' current ordering and prevents applying exposure independently in ordinary, reaction, mine, and reflection producers.
3. An incoming request `d` and maximum percentage `p` produce `floor(d * (100 + p) / 100)` with checked wide arithmetic. Zero remains zero. Preserve existing upstream minimum-damage rules; do not add a new minimum at this stage.
4. Item rules see the amplified request and retain their existing order. Shield absorption then consumes the remaining request before HP. For an otherwise final 100 damage and 30% exposure with a 50-point shield and no other intervention: 130 incoming, 50 shield, 80 HP loss.
5. Instant defeat and falling are existing non-numeric defeat paths, not percentage-amplified HP damage. Preserve FinishingShot threshold/bypasses and fall behavior.
6. Reflection keeps its current trigger, magnitude basis, and causal-chain termination. Each reflected numeric hit checks the reflected recipient's exposure once. Exposure does not introduce any new retaliation trigger.
7. Preview, Sacrifice's simulation, ordered multi-hit projection, damage breakdowns, item contexts, shield changes, HP events, and actual-loss statistics must all use the same evaluation. A request already amplified in a simulation must not be amplified again at commit. Do not fix divergence with path-local copies of the formula.

### 6.3 HuntersMark producer migration

Keep the existing trait key and producer assignment. Replace its old mark application with a Weakness Exposure grant. Preserve the existing eligible-application trigger by default: normal/extra direct hit, living hostile field character, application after the hit. Receiving damage from reactions/environment/reflection uses an existing exposure but does not automatically make those damage sources apply it.

The hit that first applies the effect does not retroactively amplify itself; later hits observe the newly projected state. Preserve current application target selection through interception/contact unless a separate trait change is approved. Explicitly test original contact versus actual damage recipient so the general damage policy is not confused with the trait's status-grant targeting.

Remove old consumption and first-source retention semantics for this producer. Do not retain old `HuntersMarkState` as a second canonical store or manufacture a misleading single-source compatibility value from several new sources.

The current production ShotGun asset contains `bonusDamagePercent: 30`; this design does not rebalance it. Current authoring/trait data/spec contain no duration. Add the duration through authoring -> immutable InGame data -> adapter -> spec/definition -> grant and explicitly author the user-selected initial value of 1. This value remains configurable per trait and per existing normal/enhanced definition; see section 5.5.

## 7. Regeneration restoration

- Initially, at the end of the holder's own side turn, heal each living field holder once by its existing configured flat amount, even if it did not act. `HealEveryOwnSideTurnEnds` defaults to 1, preserving this agreed behavior while allowing a designer to choose a longer period in that trait's authoring.
- Resolve existing end-of-turn environment/gimmick damage and resulting death/outcome first. If combat ended, respect the existing terminal short circuit and do not continue healing or turn progression.
- A dead holder is not revived. Hand/reserve holders do not heal. Actual healing is capped at maximum HP.
- On the last enemy turn of a night, eligible enemy healing occurs after end effects and before night recall if combat is still active. Main and regular characters follow the same ownership/field/alive checks.
- Use the existing health/recovery settlement and recording path. Publish requested versus actual recovery correctly; do not introduce a parallel HP owner or a second turn runner.
- A shared recovery primitive may be reused with CallOfBlood while its trigger and defeated-enemy maximum-HP formula remain unchanged.
- Retire the obsolete per-unit-turn disable condition for Regeneration only after these hooks and publication tests pass. Do not re-enable other deferred traits as a side effect.

For configurable cadence, use one holder-owned producer counter, not a recipient Healing buff or another turn system. The following engineering choices make intervals greater than one deterministic without changing the default-one behavior:

- Initialize progress to zero when the trait becomes active for a living field holder. At each eligible own-side end, after environment/outcome resolution, advance once. At the configured interval, issue one recovery and reset progress to zero. The current own-side ending counts even if the holder acquired the trait during that turn.
- Full HP still consumes a scheduled healing opportunity and resets cadence; it does not bank a future burst. Other-side ends, previews, and non-field time do not advance it.
- Reset cadence on leaving Field, death, battle completion, or removal/replacement of that producer definition. Reentering Field starts a fresh interval; no off-field ticking or catch-up healing. A holder remaining on Field across a night retains progress.
- Store progress in the existing trait runtime/snapshot transition path with one canonical owner, publish it with the turn transition, and keep it out of derived HP totals. Do not duplicate it in the common recipient-status collection.
- With interval 2, heal on the second eligible own-side ending, then the fourth; with interval 1, behavior is exactly the previously agreed once-per-own-side-end rule.

## 8. Retirement and deferred features

Intimidation retains its active attack reduction and existing consumer semantics. Remove its priority reduction from active authoring/data/calculation/presentation paths. Audit the old `PriorityReduction` and `PriorityPenalty` fields, constructor validation, registry conversion, snapshots, descriptions, and tests. Reserved serialized data may be handled by an explicit migration shim if necessary, but it cannot affect runtime behavior. Do not remove the general Priority stat or unrelated environment/item code under this task.

MoveAttackDiscount becomes retired rather than merely timing-deferred. Remove executable registration and authoring availability as appropriate to the repository's existing retirement policy. Preserve its numeric key as reserved; account for old managed-reference assets before removing a type. Do not delete unrelated old assets. No replacement action-cost mechanic is designed here.

Keep disabled: ThornField and Necromancy. Keep old Focus disabled pending the new Focus/StationaryFire specification. Keep the six retired traits retired: DoorBreaching, ContemptForWeak, PrecisionShot, DoubleShot, LuckyBullet, ChainExplosion. Flag priority pickup remains out of scope and disabled.

## 9. Publication, compatibility, and presentation

### 9.1 Preserve one transaction

The path remains command input -> isolated projection -> `CombatCommandTransition` -> `CombatTransitionPublisher` -> `BattleTransactions` prepare/validate/install. Include status expected/final data and lifecycle facts in that same transition. A failed preparation or stale status expectation installs no status, HP, item, reward, RNG, or result changes.

For unchanged effect families, preserve observable contracts and ordering. Old read surfaces may remain projections over canonical state during migration, but no second write path is allowed. Constructor/import compatibility must have one unambiguous normalization boundary. Supplying inconsistent old/new representations must fail before publication.

Weakness Exposure intentionally supersedes a contract that cannot represent multiple sources and timed persistence. Migrate its consumers explicitly rather than pretending the old single-mark semantics still hold. Ordinary effect-state changes should use the existing complete character expected/final facts where possible; add a narrow typed lifecycle fact only for information the established result model cannot express. Do not create a duplicate combat event stream.

### 9.2 Read contract

Expose effect key, buff/debuff classification, typed effective values, source entries and their remaining lifetimes, and lifecycle changes needed for replay. Distinguish effective maximum from suppressed sources. Do not publish a misleading single aggregate expiry when the maximum can fall back to another source.

Stable effect identity must not equal the applying trait's key. Reusing the same effect from another trait should not require another HUD rule. Condition-dependent damage contributions must not be displayed as universally active statuses outside their context. Preserve source application order for presentation if needed by the reference; derive it from accepted command ordering or status instance data with transactional ownership.

The reference's buff-before-debuff, then effect-key ordering can be supported in the read model. Icon/animation authoring belongs to View data; Core types remain Unity-free. Weakness Exposure's broken-shield visual is a required presentation handoff, not evidence of completed artwork.

### 9.3 Known consumers that must be addressed

- `CharacterData` currently exposes `Debuffs` and projected effective stats from `CharacterCombatFacts`.
- `CombatFactEventHandler` handles `CombatHuntersMarkEvent` using a manually constructed `CharacterDebuffState`. Migration must preserve every independent state rather than reconstructing only a subset of fields.
- `CombatEventTypeTable` classifies the old mark event for playback ordering.
- `FieldUnitPresenter` creates a HuntersMark-specific HUD entry and reads its icon through trait visuals.
- Deck/card description models expose `HuntersMarkTraitStatusSnapshot`; update descriptions so they no longer claim one-hit consumption or source-attacker exclusion.

Required compilation compatibility is retained by Core. Consumer behavior migration, View DB registration/assets, HUD/card code, and full production HUD/VFX acceptance belong to the View worker's follow-up. They remain required for integrated game acceptance, but do not block completion of the Core implementation and documented handoff. Core tests do not establish those external results.

## 10. Change locality and architectural checks

| Representative change | Current burden | Expected boundary after migration | Necessary verification retained |
| --- | --- | --- | --- |
| Another trait grants Weakness Exposure | Follow HuntersMark's bespoke hit application, single state, consumption, event, and HUD mapping. | Implement its own trigger/target selection and submit the existing typed grant. Normal registration/data wiring can change. | Producer eligibility plus common grant publication; no copied max/expiry algorithm. |
| Change Weakness Exposure reapplication | Several bespoke producer/state/event assumptions would need coordination. | One effect policy and its tests; producers continue supplying requests. | Source identity, weakened replacement, independent expiry, snapshot/publication integration. |
| Add a new attack-increase producer | Existing common grants are partly reusable but named/stored as trait-specific state and tied to trait lifecycle processing. | Reuse the typed additive-grant policy and explicit clock. | Existing Vanguard/CleanSweep/MoraleBoost behavior and shared stat calculation remain required regression coverage. |
| Change a shield source's eligibility | Must understand presence reconciliation and shield budgets. | Eligibility remains source-specific; shield budget/reset/absorption contract remains common. | Source departure/reentry, multi-source absorption, reset boundaries, Sacrifice and ordered damage. |
| Change one trait's duration or periodic interval | Several traits already expose fields, while HuntersMark lacks duration and Regeneration lacks interval input. New hardcoded constants would force code changes for tuning. | Edit the applicable trait SO authoring; its immutable value travels through the existing data/spec path. Common effect rules and unrelated trait implementations remain untouched. | Authoring round-trip/validation, the affected clock and source lifecycle, normal/enhanced separation, and existing required regression gates. |

These are design expectations, not measured results. File counts are not the acceptance criterion.

- **SRP:** producers decide when/where; effect policies manage applied state; existing combat settlement applies results; Buffer publishes; View renders.
- **OCP:** an additional producer for an existing effect needs no new effect engine branch. A genuinely new effect/capability may require explicit registration and consumer integration.
- **LSP:** every producer supplies the same validated immutable request contract; no trait-only hidden preconditions in a reused effect policy.
- **ISP:** keep stat, damage, restriction, and lifecycle capabilities narrow; do not force every effect to implement every hook.
- **DIP / Framework:** use current ownership and composition paths. Pure calculation helpers do not become root services just to make them accessible. Follow FW registration/module contracts only when an actual service is needed.
- **SSOT:** one canonical status value; derived totals and presentation snapshots have explicit owners. Reuse existing damage, recovery, Buffer, and Job responsibilities.

## 11. Verification and sequencing constraints

Implementation planning follows user approval of this design. Phases must stop for review and keep ownership/migration coherent. The following are design acceptance requirements, not a task-by-task implementation plan:

1. Capture the current runnable baseline and preservation fixtures before migration. Do not reuse historical pass counts as current proof.
2. Introduce the typed common contract with immutable expected/final and policy tests; migrate real existing producers rather than building speculative infrastructure.
3. Demonstrate reuse with the existing attack-grant/shield/restriction families while preserving policy differences and single-state ownership.
4. Add Weakness Exposure and migrate Core HuntersMark authoring/result/preview production. Prepare the View contract and migration handoff before the producer changes, then refresh it with actual implementation evidence. The View worker migrates display consumers/descriptions before integrated release.
5. Restore Regeneration with explicit turn ordering and actual recovery publication.
6. Retire obsolete Core components, finish the View handoff, and run relevant regression/integration gates. Keep external View follow-up explicitly pending rather than expanding Core ownership.

Required checks include:

- Multi-source max, independent timers, same-source weaker/shorter replacement, character+trait identity, lower-source fallback, source death/departure, target Hand/EnemyWaiting return, persistent Field holders, and zero-duration removal.
- Own-side versus opposite-side expiry; application during the affected side's turn expires at that very end for duration one.
- Every numeric damage kind, zero damage, checked arithmetic, rounding, shields, damage items, reflection, Sacrifice, sequential hits, and preview-versus-commit consistency. Defeat/fall stay unchanged.
- Existing attack-grant additive behavior, multiplicative ArmorBreaker, Intimidation completion consumption, restriction refresh, shield budget/reset/eligibility, and conditional source evaluation.
- Regeneration intervals 1 and greater than 1, idle holders, both sides, field-only, full/dead HP, environment death, terminal outcome, final-night ordering, cadence reset/retention, no duplicate healing, and recovery result facts.
- Non-default durations/reset periods/expiry counts survive authoring, normal/enhanced data, adapters, specs, grants, and publication. Zero/negative values fail at input boundaries. Changing one source's configured value does not mutate another source or an already installed grant. No hardcoded production fallback hides missing asset migration.
- No mutation in previews or rejected commands; stale expectation/prepare failure installs nothing; old snapshots remain immutable; replay reaches the published state.
- InGame authoring validation, adapters and production assets, reserved key checks, and consumer compilation. Run `DataBaseAssetTests` when assets change.
- Existing Core and CoreIntegration gates plus directly affected View/Framework/EditMode coverage. Preserve required regression even where the implementation becomes more local.
- Run/report affected consumer suites as evidence where practical, but do not rewrite View-owned tests to make the Core branch green. Identify exact pending consumer failures separately from Core regressions and pre-existing failures; do not suppress them or claim the whole suite/game passed. View-owned migration and its final acceptance remain with the receiving worker.
- Unity must run through the approved AppData-access path described in the repository instructions. Do not upgrade Unity or delete caches to address the known sandbox issue.

Existing relevant test families include `TraitAttackNotificationTests`, `TraitShieldTests`, `TraitRestrictionTests`, `ArmorBreakerTraitTests`, `IntimidationAttackTests`, `IntimidationStatePreservationTests`, `CannonThresholdTraitTests`, `SacrificeTraitTests`, `HuntersMark*Tests`, `Regeneration*Tests`, and the corresponding `CoreIntegration` publication/authoring suites. Tests that intentionally assert disabled Regeneration or the old HuntersMark semantics must be replaced with the approved behavior, not kept as conflicting requirements.

## 12. Resolved questions and review status

| ID | Resolution | Consequence |
| --- | --- | --- |
| Q1 | User selected an initial duration of 1 and requested editable turn counts owned by individual traits. | Add and migrate the duration field; preserve existing percentages. Section 5.5 covers all in-scope duration/interval authoring. |
| Q2 | User confirmed clearing Weakness Exposure when an affected enemy returns to EnemyWaiting at night end. | Clear on both reserve-return paths, while leaving a Main character that remains on Field under normal expiry. |

No previously identified product question remains unanswered. The user approved the complete design, including the engineering choices in sections 5, 6.2, 6.3, 7, and 9 and configurable Regeneration cadence. If a later decision changes existing gameplay beyond the listed exceptions, revise the design before implementation.

Initial and follow-up builder reviews completed on 2026-10-30. The initial review found only Q1/Q2, now resolved. The follow-up review checked trait-owned configuration, existing normal/enhanced authoring, Regeneration cadence, and both reserve-return paths and reported no consequential unresolved questions or contradictions for implementation planning. This is read-only design/code review evidence, not compilation, behavioral verification, or approval to implement.

Subsequent user ownership clarification: the user is the Core programmer and wants View work handed off to avoid concurrent edits. D17 and the implementation plan's C3/F2 now specify document delivery preparation rather than View implementation. Gameplay decisions are unchanged; Core completion and downstream View acceptance are tracked independently.

## 13. Source map

All paths below are repository-relative. These are current owning sources inspected for this design; old plans are not substituted for them.

Common prefix `C`: `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/`.

| Concern | Sources |
| --- | --- |
| Framework and locality | `Assets/ProjectL/Script/Systems/Documentation/framework-programmer-guide.md`; `docs/superpowers/handoffs/2026-09-30-combat-view-design-session-handoff.md` section 4.6 |
| Current availability | `C/Application/Combat/DeferredCombatFeatures.cs`; `C/Contracts/Common/TraitKey.cs` |
| State and contributions | `C/Contracts/Combat/CharacterCombatFacts.cs`; `CharacterDebuffState.cs`; `CharacterTraitEffects.cs`; `TraitAttackGrant.cs`; `ArmorBreakState.cs`; `C/Contracts/CombatEffects/EffectOriginKey.cs` |
| Existing effect engine | `C/Domain/CombatEffects/ICombatEffectEngine.cs`; `CombatEffectEngine.cs`; `C/Application/Combat/CombatCommandEffects.cs` |
| Existing policies | `C/Domain/Traits/Rules/`; `C/Domain/Traits/Effects/`; `C/Application/Combat/TraitPresenceReconciler.cs`; `CombatEffectiveStatsCalculator.cs` |
| Weakness producer | `C/Application/Combat/HuntersMarkApplication.cs`; `C/Domain/Traits/Rules/HuntersMarkTraitRule.cs`; `C/Contracts/Commands/Traits/HuntersMarkTraitSpec.cs` |
| Damage | `C/Application/Combat/CombatDamageApplication.cs`; `CombatDamageCalculator.cs`; `AttackDirectDamageCalculator.cs`; `SacrificeDamagePlanner.cs`; `C/Domain/Combat/TargetDamagePipeline.cs` |
| Turn and cleanup | `C/Application/Combat/CombatTurnCalculator.cs`; `TraitTurnLifecycle.cs`; `CombatPlacementCalculator.cs`; `CombatTemporaryStateCleanup.cs`; `CombatCommandCalculator.cs` |
| Publication | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CombatSystem/CombatTransitionPublisher.cs`; `Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/Core/BattleTransactions.cs`; `Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/Character/CharacterData.cs` |
| Authoring | `Assets/ProjectL/Script/Systems/DataBaseSystem/Data/InGame/Table/Unit/Trait/`; `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CharacterSystem/TraitSpecAdapter.cs`; `Assets/ProjectL/DataBaseSO/InGameGroup/Unit/Item/Playerable/Unit_ShotGun.asset` |
| View consumers | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Playback/Handlers/CombatFactEventHandler.cs`; `Combat/Playback/Ordering/CombatEventTypeTable.cs`; `Combat/Display/Unit/FieldUnitPresenter.cs`; `StageMap/Deck/DeckEditDisplayModel.cs` (last three relative to the same ViewSystem root) |

