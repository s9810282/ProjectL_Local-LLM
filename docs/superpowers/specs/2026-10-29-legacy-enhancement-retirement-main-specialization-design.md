# Legacy Enhancement Retirement and Main Specialization Design

**Date:** 2026-10-29. **Status:** M1-M3 implemented under subsequent explicit phase authorization; M4 pending. This design document itself does not authorize implementation or deletion.

**Companion plans:** [retirement](../plans/2026-10-29-legacy-enhancement-retirement-plan.md) and [Main specialization](../plans/2026-10-29-main-specialization-plan.md).

**2026-10-29 policy update:** The user confirmed removal of combat movement/attack costs, category-limited movement, one ordinary attack per unit per controller turn, and voluntary player-turn completion without exhausting actions. Section 4.5 is the current policy; retirement-plan R4 implements it after R0-R3. This update authorizes documentation only. Shop currency prices are unaffected.

**2026-10-31 specialization scope revision:** The user approved revising the documents to cover **Core capability only, stopping before shop integration**. Main keeps its identity and may hold one additional Rider/Heavy/Cannon category. Deliver owned state, effective combat/progression behavior, a reusable Core preparation/application boundary, verification and a handoff. Level-up selection and enhancement-shop purchases are possible later acquisition routes, not required deliverables. No dedicated specialization screen, shop visit type, stage content or production acquisition route is assumed. That revision authorized document changes only; implementation was unstarted at that point. This revision supersedes the earlier specialization shop/activation scope, not the separate retirement work.

**Current checkpoint (2026-10-31):** M1-M3 are implemented; execution evidence is in the [plan, section 6](../plans/2026-10-29-main-specialization-plan.md#6-execution-record). Owned/battle projections retain Main identity, and effective progression preserves Main steps while combining candidate pools. Internal Core preparation/application now verifies living selected Main, growth readiness and original state/DB sources before the existing Buffer installs the owned change. Focused M3 passed 68/68; Core passed 4,425/4,425; CoreIntegration passed 574/575 with the unchanged camera type-load failure, zero skips. M4 handoff and acquisition/UI/content remain unstarted. Stop after M3; no commit/push.

## 1. Confirmed scope and decisions

The user requested a separate deletion plan, then explicitly retained ChangeRider for a new Main-only specialization feature. The resulting scope is **retire seven legacy enhancement products, remove six dedicated trait implementations, preserve ChangeRider, and separately plan Main specialization**. Do not describe this as deleting all eight keys.

| Decision | Authority / meaning |
|---|---|
| Preserve the 22 new traits and other traits' shared behavior | Explicit user instruction. Position Swap/Recon remain deferred. |
| Retain ChangeRider | Explicit user instruction; the old cost-only behavior is superseded, not revived. |
| Main retains identity, dedicated progression and native traits | User selected “retain Main + selected class combat rules.” Akimbo and Sniper are examples, not a hardcoded allowlist. |
| Select one of Rider, Heavy, Cannon | Main-only additional category; no simultaneous three-class membership. |
| No respecialization within a Run | Explicit user answer. A repeated choice, including the same class, must not spend or grant again. |
| Core capability before acquisition integration | User clarification on 2026-10-31. Implement Main + one additional category independently of how it is earned. A later level-up selection or enhancement-shop product may use the capability; neither route nor dedicated UI/content is included now. |
| Use current class rules only | User answer. Do not resurrect Rider movement refunds, Heavy ally protection, or disabled category immunity. |
| No runtime changes now | This task produces English documents only; code comments in later implementation are Korean. No commit/push. |

**Retained technical design:** an optional Run-owned specialization field; Main's existing step sequence plus a deduplicated union of its pool and the selected class pool; preserve old serialized values as tombstones; fresh Run clears specialization. These are implementation recommendations, not additional quoted user decisions. Configured test definitions establish the Core capability without new production assets. Acquisition rules, prices, levels and production progression values are future content decisions and do not block Core completion.

## 2. Source evidence and existing boundaries

The original retirement/source inventory below was inspected at HEAD `e15729850` on 2026-10-29. Its removal status, counts and section 4 follow-on chronology are historical, not current implementation evidence. Re-read the accepted source and later undo/status-effect contracts before implementation; do not repeat completed work from this inventory.

Paths below are repository-relative. Prefixes: **C** = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator`; **S** = `Assets/ProjectL/Script/Systems`; **B** = `S/RuntimeSystem/BufferSystems`; **D** = `S/DataBaseSystem`; **A** = `Assets/ProjectL/DataBaseSO`; **T** = `Assets/Tests/EditMode`; **TI** = `T/CoreIntegration`.

| Source | Verified fact / design consequence |
|---|---|
| `C/Contracts/Enhancements/EnhancementKey.cs` | Values 1..8 exist; retain ChangeRider=2, reserve retired values 1,3,4,5,6,7,8. |
| `C/Contracts/Common/TraitKey.cs` | Six dedicated traits occupy 130,140,150,160,170,180. ThornField=40 is independent and survives. |
| `A/InGameGroup/Enhancement/Item/` | Four remaining products: DoubleShot, DoorBreaching, ChainExplosion, PoisonThorn. Their GUID users are the enhancement table and, except ChainExplosion, Enhancement_Default candidates. |
| `D/Data/InGame/Table/Unit/Trait/{UnitTraitAuthoring,UnitTraitData}.cs` | Six families share these files with surviving traits. Remove declarations only, never whole files. |
| `C/Application/Combat/FollowUpAttackExecutor.cs` and effect capabilities | Its three request producers are exclusively DoubleShot, LuckyBullet and ChainExplosion. General attack delivery and native KillChain are separate surviving owners. |
| `C/Domain/Traits/Effects/{StationaryFire,TrackingShot,FocusedBombardment}EffectRule.cs` | Surviving consumers of IEffectTargetDamageRule; preserve the contribution engine and rounding stages. |
| `C/Application/Roster/TraitGrowthRules.cs` | ChainExplosion has one distance-coverage exclusion alongside surviving PiercingFalloff/KillChain exclusions. Remove only the retired case. |
| `S/RuntimeSystem/InGameSystem/CoreSystem/ShopSystem/ShopService.cs`, `B/Core/ShopTransactions.cs` | Legacy enhancement entry and purchase are rejected. Main specialization needs its own typed path; deleting products must not unblock old Sub purchase requests. |
| `C/Contracts/Common/PlayerMainDefinitionPolicy.cs` | Player Main definitions require Category.Main and PlayerOnly. Changing the authored category to Rider is invalid and loses identity semantics. |
| `C/Contracts/Roster/OwnedCharacterRecord.cs` | Id/key/role, XP, growth history and Hunter bonus are owned immutable facts. Add specialization here, not in SO, View or a second roster. |
| `C/Application/Enhancements/OwnedUnitSpecResolver.cs` | Resolves owned trait history into immutable specs. Its early return for empty Growth must not bypass a specialized Main. The Enhancements folder contains current growth code and survives. |
| `C/Domain/Entities/UnitDefinition.cs`, `C/Domain/Categories/CategoryActionRules.cs` | Current source derives movement from CategoryDefinition and attack cost from category. R4 removes combat pricing; specialization retains the narrow identity/combat-category distinction for movement range, matchup and category rules. |
| `C/Application/Roster/CharacterBaselineStatsCalculator.cs` | Main ATK/HP/DEF use Main persistent growth; class Sub level growth must not replace them. |
| `C/Domain/Entities/CategoryDefinition.cs` | Cannon currently requires SafeBombardment; Rider/Heavy have no old protection/refund rules. Use the existing registry and configured rule set. |
| `C/Application/Roster/TraitGrowthCatalogValidator.cs` | Checks every reachable selection path; Acquire requires three candidates. Substituting fixtures/content must preserve this guarantee. |
| `B/ShopView/ShopViewCommand.cs` | Existing purchase input is visit context + frozen offer ID. Specialization can reuse this input instead of introducing a new choice Buffer. |

The prior read-only audit found 48 dedicated C# files (eight per family), ten directly referencing common source files and 42 directly referencing test files. Indirect hooks/helper consumers add work; these are inventory observations, not deletion quotas. No tests were executed for this design.

**Focused source check on 2026-10-31:** HEAD `2bd95ad342f5075ecbd31714bde8f2ad756af0ab`. `OwnedCharacterRecord` has no specialization field; `OwnedUnitSpecResolver.Resolve` returns the base spec for empty Growth; `UnitDefinition` uses one category for identity and movement. `TraitGrowthSelection` stores only StepId/TraitKey. `CharacterRosterService.PrepareTraitGrowthChoice` and `B/Core/TraitGrowthTransactions.cs` already separate Core decisions from checked Buffer publication. `ICharacterBufferStore.PrepareGrowth/InstallGrowth` is the existing typed owned-change pattern; `B/Core/BufferSystemGroup.cs` owns transaction composition. These are reusable boundaries, not proof that class acquisition is already supported. No Unity/test execution accompanied this document revision.

## 3. Approaches and selected recommendation

| Approach | Trade-off / disposition |
|---|---|
| Delete legacy products only | Small but leaves unwanted executable abilities and their testing dependencies. Does not fulfill dedicated-code retirement. |
| Remove dedicated families and their wiring; retain surviving owners and shared contracts | Recommended retirement approach; exposes local removal boundaries without rewriting the combat framework. |
| Replace Main's authored category with selected class | Rejected: conflicts with Main identity validation and risks Sub growth/deck/ownership behavior. |
| Add general multi-category collections and compose every class effect | Rejected: unnecessary generalization for one Main + one specialization; ambiguous stacking and duplicated eligibility. |
| Store one Main specialization and derive combat category/pool | Recommended; matches the user's identity/combat choice, single-class limit and existing owned composition. |
| Complete specialization through a new dedicated shop/stage first | Previous scope; superseded on 2026-10-31 because it couples Core completion to an undecided acquisition route. |
| Complete the Core capability and defer acquisition adapters | User-selected scope on 2026-10-31. Reuse the same pure transition and typed store preparation later; do not build a generic rewards framework or speculative UI. |

The user confirmed the behavioral model; the concrete field/type split below is the implementation recommendation. Execution still requires design/plan review and explicit Phase authorization.

## 4. Retirement boundary

### 4.1 Exact identities

Retire Enhancement keys LuckyBullet=1, DoubleShot=3, DoorBreaching=4, ContemptForWeak=5, ChainExplosion=6, PoisonThorn=7, PrecisionShot=8. **Keep ChangeRider=2.** Do not rename its serialized value, use 2 for Heavy/Cannon separately, or invent a TraitKey.ChangeRider. The retained name denotes the legacy entry point; the new feature offers all three classes through typed category payloads.

Retire executable Trait keys DoorBreaching=130, ContemptForWeak=140, PrecisionShot=150, DoubleShot=160, LuckyBullet=170, ChainExplosion=180. Keep their numeric reservations, reject active authored/native/pool/catalog references with a retired reason before runtime construction. Missing registration must not masquerade as supported availability. Preserve the existing reserved Frenzy number and all surviving enum numbers.

For each exact stem `DoorBreaching`, `ContemptForWeak`, `PrecisionShot`, `DoubleShot`, `LuckyBullet`, `ChainExplosion`, dedicated deletion candidates are:

- `C/Contracts/Commands/Traits/<Stem>TraitSpec.cs`
- `C/Contracts/Snapshots/Traits/<Stem>TraitStatusSnapshot.cs`
- `C/Domain/Traits/Definitions/<Stem>TraitDefinition.cs`
- `C/Domain/Traits/States/<Stem>RuntimeState.cs`
- `C/Domain/Traits/Rules/<Stem>TraitRule.cs`
- `C/Domain/Traits/Effects/<Stem>EffectRule.cs` and `<Stem>EffectStateSnapshot.cs`
- `C/Application/Combat/Traits/<Stem>TraitFactories.cs`

Remove paired .meta files only with their dedicated files. Preserve shared authoring/Data files and remove just the six classes from each; edit TraitSpecAdapter, TraitRegistry, TraitEffectProvider, TraitEffectStateCodec and CombatCommandEffects in place. Preserve unrelated concurrent edits.

### 4.2 Preserve gameplay owners

Keep all 22 new rules and states; KillChainAttack on Main/Akimbo; normal/reaction delivery; current origin/credit/RNG and damage-kind contracts; action/strike accounting; shields/Sacrifice; common death/kill rewards; Hunter; displacement, hazards and ordinary explosions; typed trail lifecycle; Buffer validation/publication.

Keep TargetDamagePipeline, CombatDamageCalculator and IEffectTargetDamageRule. Remove only obsolete providers; do not simplify away damage-stage evidence or change rounding. Keep base ThornField authoring/runtime/visibility and existing DefenseReduction state independently of the PoisonThorn product and ArmorBreak stacks. Deferred traits stay deferred.

**Exclusive after the six removals:** the three PlanAttackResolved/PlanKillChainActivated/PlanDisplacementResolved capabilities, their matching contexts, EffectFollowUpRequest/FollowUpDeliveryKind and FollowUpAttackExecutor lose all production providers. A bounded optional cleanup may remove these hooks after a fresh caller inventory, including only their calls in AttackDeliveryExecutor, KillChainAttackCalculator and AttackKnockbackCalculator. Never remove those parent calculators. Cause/attack-group and common events are not automatically exclusive; retain any surviving user and serialized enum values. If exclusivity cannot be proved, retain the generic branch and record it rather than broaden the deletion.

### 4.3 Assets and owner boundary

Remove the four listed product assets + metas and their GUID references from EnhancementDataTable.asset and Enhancement_Default.asset. Retain the empty legacy table/schema/service if required for current DB/public contracts. Retain the enhancement stage root, node identity, visit/result/transaction infrastructure and ChangeRider reservation for the later Main shop; do not reopen legacy products.

View-owned follow-up: remove TraitVisual_DoubleShot.asset and TraitVisual_DoorBreaching.asset plus their table entries. Both currently have null icons. Preserve common TraitVisual handling, base ThornField text, card formatting and playback events. Do not edit scenes/Flow/UI in the Core retirement phase; report the remaining unused display rows as handoff, not delivered cleanup.

### 4.4 Test preservation contract

Before removing fixtures, map each affected test to a surviving guarantee or a genuinely retired behavior. Move reusable setup/assertion helpers out of Enhancement*Tests into an existing or minimal neutral fixture; keep Input/Run/Stats/Activations semantics explicit. Filename prefixes are not deletion criteria.

| Existing dependence | Required outcome |
|---|---|
| TraitGrowthFixtures and TraitGrowthTestRuntime use retired candidates | Replace with valid surviving typed definitions; preserve three-choice sufficiency, Enhance eligibility, copy/serialization, Main/Sub and frozen receipt tests. Fixture enhanced values remain test-only. |
| ArmorBreaker/CannonThreshold/Acceleration use DoubleShot | Retain sequential-hit guarantees using native KillChain where semantically equivalent. Do not turn a two-action test into one-action coverage by relabeling it. Remove only now-impossible DoubleShot child-action guarantees and explain the distinction. |
| Sacrifice uses DoorBreaching; multiplier tests use ContemptForWeak | Replace with independent existing defense/range and surviving multiplicative contributions; retain simulation/application parity and nontrivial expected values. |
| Hunter/Mark/ImpactBolt/Movement use Enhancement test helpers | Preserve the surviving behavior and helper access before deleting dedicated suites. |
| PoisonThorn/Lifecycle tests exercise base field/debuff preservation | Retain those base/deferred-clock/cleanup assertions under neutral names. Only product-specific expectations retire. |
| E+ activation matrix and shape/identity tests | Keep all surviving producer, immutable contract and publication checks. Retire only the six identities; numbers remain reserved. |

Expected post-retirement executable count is 31 (22 new + 9 existing), with the same five independently deferred traits. This is not a production-pool count. ChangeRider specialization is an owned/shop capability, not a 32nd trait.

### 4.5 Combat action-cost retirement — confirmed follow-on scope

Remove movement and attack action costs and both player/enemy shared action budgets. Do not simulate removal with zero prices, unlimited budgets or no-op affordability APIs. Preserve shop prices, draw/deployment currency rules, inventory consumption and unrelated economies.

- Category configuration remains the source of maximum movement distance. Current production values are Main 3, Rider 4, Heavy 3, Cannon 2 and Boss 0; preserve these authored values and existing valid movement modifiers. Zero means voluntary movement disabled, not unlimited movement. Distance counts traversed path steps under existing movement rules, not merely start-to-end displacement.
- Keep `UnitActionUseRules` authoritative: Available permits one ordinary move or attack; Moved permits one ordinary attack; Attacked permits neither. A shorter move still uses the one movement action. Reset action use at the existing owning-side turn-start boundary. Cost removal does not grant extra actions or remove range, target, ownership, death, disarm or movement restrictions.
- One attack means one accepted ordinary attack action, not one damage event or one target. Retained reaction/trait-generated strikes keep their existing rules and cannot reopen ordinary attack admission. Rejected commands do not consume action use; accepted no-hit attacks retain existing acceptance semantics.
- The player may end their turn with all, some or no ordinary actions used. Do not require moving, attacking, exhausting every unit, or a resource threshold. Preserve the existing command gate and processing/playback completion boundary; this does not authorize interrupting an in-flight transaction, skipping final playback, or ending the enemy's turn through player input.
- Preserve expected/final action-use evidence and Buffer source/actor/turn validation when removing cost settlement. Core computes the rule, Buffer validates and publishes it, and View consumes availability. Do not copy action-use authority into View or introduce a second transaction/state store.

Basic movement undo is a subsequent task, not part of R4. It will restore the original position and movement opportunity when cancellation is allowed, and attack success closes cancellation. Whether movement-triggered damage, pickups or trait/status effects are reversible remains undecided; do not silently choose a full rollback or the old ReturnBarrier policy. R4 does not restore the old AP refund ledger or checkpoint APIs.

The subsequent order is retirement R0-R3 -> cost removal R4 -> basic movement undo -> buff/debuff migration -> Main specialization -> undo-related traits. Later tasks need their own accepted boundaries; this ordering does not authorize their implementation or invent status/undo mechanics.

## 5. Main specialization contract

### 5.1 State and category meanings

Add optional trailing `CategoryKey? specializationCategory = null` / get-only `SpecializationCategory` to OwnedCharacterRecord. Only an owned Main may hold Rider, Heavy or Cannon. None/Main/Boss and any Sub assignment are invalid. Fresh Run acquisition uses null. It persists through stages, night, recall and rewards in the same Run; new Run creates a fresh unspecialized record. No mirrored selected class or separate Main ID.

Keep authored UnitSpec.CategoryKey and UnitDefinition.Category/CategoryKey as identity category Main. Add optional effective-spec specialization and a derived CombatCategoryKey. At compilation, UnitDefinition additionally resolves CombatCategory through the existing CategoryRegistry; no copied mutable CategoryConfig or second set of balance constants. Legacy constructors default to unspecialized behavior.

Use CombatCategory for MoveRange, category runtime rules and battle CategorySnapshot. Combat MoveCost/AttackCost are removed by R4 and must not be reintroduced by specialization. `CharacterCombatFacts.OwnershipRole` remains Main; combat `Stats.Category.Key` names the effective combat class. `CombatCommandEffects.Capture` currently compares this snapshot with `profile.Definition.CategoryKey`; change that guard to compare the derived effective `CombatCategoryKey`, while PlayerMainDefinitionPolicy continues validating identity Main. Otherwise every specialized Main attack would be rejected. Expose identity and specialization explicitly in owned/battle read DTOs as derived facts; retain baseline identity CategoryKey semantics rather than silently relabeling a public field. Update snapshot copies/equality/With/shape guards together. No additive Main+class movement or matchup multipliers: choose the effective class once. Specialization does not change ordinary action counts.

Preserve Main persistent ATK/HP/DEF/level/XP, native traits, weapon/aiming/range, footprint, MainId and run-deck identity. The selected class supplies current maximum movement distance, matchup row/column and existing category rule set, including Cannon SafeBombardment. Heavy's currently disabled category knockback immunity stays disabled. New rules do not make Main count toward Sub ownership caps or make it eligible for Sub purchases.

Proposed pure owner: `C/Application/Roster/MainSpecializationRules.cs`, exposing `ResolveCombatCategory(CategoryKey identity, CategoryKey? specialization)` and `Plan(OwnedCharacterRecord expected, CategoryKey selected)` returning a typed MainSpecializationChange/decision. OwnedCharacterChange and TraitGrowthChange explicitly preserve specialization; only the dedicated change can set null -> chosen once. Repeated requests fail normally; source mismatch/stale preparation cannot mutate any store.

### 5.2 Trait access and progression

Recommended bounded policy: keep Main's existing ordered steps and step IDs; use the distinct union of Main.Pool and the chosen class.Pool for subsequent choices, through one pure effective-progression resolver used by eligibility, authoring/path validation and OwnedUnitSpecResolver. A class selection is access, not an automatic grant of every class trait, and does not import Sub level-growth formulas or append/replay its step sequence.

Preserve already acquired Main traits and their history. Selecting a class cannot replay completed steps or refill pending offers. The standalone Core application in section 5.3 requires the existing growth flow to be ReadyToContinue, including no unlocked unresolved step, frozen offer or unacknowledged result. This is an application-boundary precondition, not a prohibition inside the pure specialization transition. A future growth acquisition adapter must validate and consume its own matching offer atomically; it must not bypass the current guard or mutate a frozen offer. No respecialization means no cross-class trait stripping/refund policy is needed. Validate legal Main-native/weapon combinations and every reachable Acquire/Enhance path for the chosen Main/class; common pool union never bypasses effect compatibility, retirement/deferred availability or three distinct Acquire choices.

Main unlock steps, candidates and enhanced payloads remain content-owned. An empty Main sequence stays empty; selecting Rider must not invent growth opportunities. Enable only configurations that pass candidate sufficiency.

### 5.3 Reusable Core preparation and application

Retain EnhancementKey.ChangeRider=2 as a reservation only. Specialization is a typed owned transition, not a TraitKey, product, attack effect or SO category rewrite. The Core capability needs no price, shop visit or growth-choice payload.

Use `MainSpecializationRules.Plan(expected, selected)` for the pure transition. Add typed `ICharacterBufferStore.PrepareMainSpecialization(change)` / `InstallMainSpecialization(prepared)` beside the existing growth preparation. They validate expected/final shape and roster revision and update the existing owned record only. They neither own a second roster nor decide how a class is earned.

Use internal `CharacterRosterService.PrepareMainSpecialization(RunId expectedRun, long expectedRosterRevision, CharacterInstanceId mainId, CategoryKey selected)` returning `MainSpecializationOperationPreparation`, and `ApplyMainSpecialization(MainSpecializationOperationPreparation prepared)` returning `MainSpecializationDecision`. Preparation is read-only. It resolves the actual selected Main, validates the native profile and effective progression, and captures the source facts. Application revalidates those facts and uses a narrow transaction composed in the existing `BufferSystemGroup`; recheck the original Core preparation after Buffer preparation and before installation so the transaction cannot silently capture a different source. A failed Core preparation returns its failure without attempting installation. Keep these methods internal; do not add a player command, public free-grant API, new service registration, Job or input Buffer. Integration fixtures can exercise this real boundary without a screen or acquisition opportunity.

The Buffer transaction exposes internal `Prepare(expectedRun, expectedRosterRevision, change)` / `Install(prepared)` operations through `IBufferQuery.MainSpecializationOperations`. Its prepared token records its owner, original Run/roster/deck/Main-health and growth state, and typed character preparation; it is ephemeral validation evidence, not persistent game state. All fallible checks precede installation. Installation rechecks the current sources, uses the original store and rejects foreign, stale or already-installed tokens. Core owns profile/progression validation; Buffer owns source validation and installation. Normal internal composition wiring is allowed; do not duplicate Framework/Job/transaction infrastructure.

Require an in-progress nonempty Run, the living selected owned Main, no active/unsettled battle and growth ReadyToContinue. Reject invalid classes, Sub/nonselected targets, an already chosen class, stale Run/roster/deck/Main-health, changed growth state or an unsupported native/progression combination. Capture and compare relevant DB input values across Core preparation/application; fresh immutable GetData objects are not source changes solely because their object references differ. A Core command refusal returns a typed failure; invalid direct preparation/install is rejected using existing Buffer exception conventions. Null/unbound dependency errors remain explicit exceptions.

Success changes only SpecializationCategory and the existing roster revision (once); owned/battle read projections derive the effective facts. XP, growth history, Hunter bonus, persistent Main health/stats, deck membership, currency, shop state, offers and result counters are unchanged. Repeated same-class or different-class application fails without a second mutation. No failure may leave a partial record update. M1/M2 fixtures may seed valid specialized records for projection tests; M3 must demonstrate preparation, application and a subsequent real battle using the composed runtime.

Future acquisition adapters reuse the pure decision and typed store preparation **inside their own existing atomic publication**. A shop must combine the class change with its payment and purchase receipt; a growth adapter must combine it with consuming the exact choice and publishing its result. Neither may call the standalone apply operation and then separately spend/consume. Such adapters, and any acquisition eligibility beyond the Core conditions above, are outside this plan.

### 5.4 Deferred acquisition, UI and content

Do not add ShopProductKind/ShopVisitKind members, MainSpecializationStageData/Detail/Option, shop offers/prices, Purchase/Complete changes, stage-root migration or production SO activation in this work. Preserve the existing legacy enhancement rejection and ordinary shops. Do not implement a specialization-only screen, popup, scene, level-up choice kind or acquisition command.

The existing trait-selection window is one possible presentation surface, but current StepId/TraitKey contracts do not encode a class. Whether a future class choice replaces a trait choice, is an extra reward, or is sold in an existing shop remains undecided. These decisions require a later bounded integration plan, not placeholders or permissive flags in the Core foundation. Main growth steps/pools/enhanced values remain configured content; an empty sequence stays empty. Core completion uses explicit test content and does not require those production decisions.

## 6. Delivery, acceptance and locality

Retirement can finish independently of Main specialization. Stop after each authorized phase. Do not edit the active trait implementation plan or its historical logs during this documentation task. After retirement verification, update current coverage/handoff as an explicit task while preserving old evidence. F1 can document current Core before specialization, with this separate future plan linked; future work does not retroactively make A-E+ incomplete.

| Real change | Edit / understanding boundary | Necessary verification |
|---|---|---|
| Retire one old ability | Its family, registry/codec/authoring branches, real asset refs and test users | Availability + relevant integration, no unrelated trait formula rewrite |
| Remove combat costs | Admission/turn/AI, action-use evidence, Buffer guards and actual View consumers | Distance limits, one-action rules, voluntary end-turn, publication and View integration; no shop-economy rewrite |
| Tune Main Rider movement | Existing Rider SO and effective-category composition | Owned read, battle install, context/range/action-use parity; no duplicate Main constants |
| Change specialization trait access | Effective progression resolver; existing steps remain owner | History replay, candidate sufficiency, offer/receipt and XP integration |
| Apply a Main specialization in Core | Pure transition + existing roster service + typed owned preparation/Buffer composition | Selected/living Main, Run/roster/deck/growth freshness, no partial update, exactly one revision and next-battle behavior |
| Later expose a level-up/shop acquisition | Its offer/result or purchase transaction adapter; reuse the transition and typed store preparation | That route's consumption/payment atomicity plus shared Core regressions; deferred from this work |

Check SRP at rule/publication boundaries, OCP without global per-trait dispatch, LSP via legacy unspecialized constructors, ISP for targeted contexts, DIP for pure rules, and SSOT for selection/state/config. Normal registration changes are legitimate. Preserve FW-001/002/003/004/016 and existing Buffer/Job ownership. Do not add a general class-composition framework or merge unrelated refactors.

Run Core and CoreIntegration sequentially using Unity 6000.0.73f1 through the approved AppData route, unique XML/log names, meaningful focused RED/GREEN and final full suites. Compare exact failed fullnames against fresh relevant checkpoints; do not preserve failure counts by deleting tests. Document test removals/replacements and affected baseline causes. View/Framework/manual evidence is separate and must not be claimed from Core gates.

**Scope/locality comparison:** Previously, completing specialization required understanding and changing shop kinds, stage details, offers, prices, purchase receipts and content activation in addition to owned/combat/growth rules. The revised work edits the latter owners and the existing Core/Buffer connection only. A later acquisition change should require its own adapter and the shared transition contract, not changes to category calculations or a second owned-state store. This is the expected benefit of the plan, not a verified implementation result. Core/CoreIntegration regressions remain required because shared owned records and projections change; reducing the delivery scope does not waive them.

**Core completion boundary:** M1-M3 implement and verify state, behavior and safe application using explicit fixtures; M4 records the verified contract and prepares an undelivered handoff. There are no content-activation tasks in this plan. Level-up acquisition policy, any shop price/options/placement, production Main progression and UI/Flow integration are later work, not incomplete Core acceptance. No production player can acquire specialization merely because the foundation is complete.

## 7. Documentation review

2026-10-29 follow-on revision: aligned the retirement and specialization plans with section 4.5. Existing source still contains costs; these are planned removals, not verified runtime changes. The previous builder-check record below describes the earlier specialization review, not an independent review of this revision.

A fresh read-only builder check found no remaining product question needed to plan the bounded feature. Two source details were corrected: use CharacterCombatFacts.OwnershipRole, and explicitly update the CombatCommandEffects category guard to the effective combat category. The exclusive three-provider follow-up inventory was independently confirmed. This is design review evidence, not executable validation or implementation authorization. The recommended pool/step composition remains visible for user review; content decisions remain at the production gate.

2026-10-31 scope revision: replaced specialization shop/production activation with Core preparation/application and a handoff. Kept Main identity, one additional class, once-per-Run behavior, configured class rules and the proposed effective progression. Separated standalone growth-readiness checks from future acquisition consumption. The original retirement section and earlier review entries remain historical. Implementation tasks remain unchecked; no runtime, assets, Unity tests, commit, push or external delivery are part of this revision.

A fresh read-only builder check of the revised design found no material unanswered question or contradiction at the Core/Buffer, source-validation or deferred-acquisition boundaries. This is documentation review only, not an implementation or runtime pass.
