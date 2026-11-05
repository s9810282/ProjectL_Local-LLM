# Configurable Category Trait Progression

Date: 2026-10-27
Status: Revision 3 incorporates the requested locality/ownership corrections, Main eligibility, and preservation of existing stat level-ups. The original draft received a builder review; this revision remains a design document, not implementation authorization.

Execution note (2026-10-27): the user subsequently authorized Phase A of the companion plan, approved `CombatAttackProfile` eligibility/validator inputs and accepted the recorded pre-existing test failures. Phase A is implemented and verified; later Phases and operational activation remain pending. The original design-only statements below describe the drafting request, not the later Phase A authorization.

## 1. Intent and confirmed decisions

Owned Main and Sub characters can gain player-selected traits as they reach configured levels, in addition to their existing stat level-ups. Heavy, Rider, and Cannon have Sub category candidate pools; Main uses its existing `CategoryKey.Main` configuration. Main's actual candidates and unlock schedule are not decided yet. Assigning a category does not grant those traits at creation.

Confirmed by the user in this design session:

- Acquisition presents three category candidates and the player chooses one.
- Acquisition count, enhancement count, and their unlock levels are configurable through SO authoring.
- Configuration uses an ordered list of steps, each with an unlock level and an Acquire/Enhance kind. Counts derive from the list.
- Enhancement presents at most three eligible owned traits; when fewer exist, it presents all of them.
- The enhancement shop will sell Main-character growth only. Sub-unit traits cannot be purchased.
- Main characters are eligible for level-up trait selection; do not encode a Main-role exclusion. Their actual trait content and unlock steps remain undecided and must not be invented.
- Existing Sub category stat growth and existing Main stat growth continue unchanged. Trait choices are an additional level-up opportunity, not a replacement for XP, level gains, or stat growth.
- The user is the Core programmer. Core implements the rules, InGame authoring contracts, Buffer transactions, and public integration contracts; UI, presentation, scenes, and LifeCycle Flow/Job wiring are handed off to their owners.
- SO validation and live candidate generation must use the same eligibility function. Choice application and result acknowledgment must be distinct in the public progress contract.
- This task is design only. Do not implement, commit, or push.

The remaining policies below are explicit design proposals, not previously approved user decisions. Final design approval includes those policies.

### 1.1 Delivery ownership and completion

| Delivery | Owner and completion evidence |
|---|---|
| Category/trait definitions, pure rules, selection storage/composition, offer/receipt transactions, buffered command processing, Core stage-entry guard | Core. Complete when the relevant Core/CoreIntegration and authoring tests pass and the public contracts are documented. |
| UI layout/components, UI/input registration, visual mappings, prefab/scene connections, settings and presentation lifetime | View owner. Core supplies examples and consumer contract tests; it does not edit these files as part of its implementation. |
| Combat completion and Stage readiness sequencing, LifeCycle Plans/Jobs, transition/input handoff | Flow/View owner. Core supplies freshly derived progress facts and failure semantics; sequencing ownership stays with the existing Flow. |
| Operational InGame pools/steps and legacy enhancement-node availability | Core validates; content owners approve the gameplay data and map changes. Main-shop product implementation remains a separate feature. |
| End-to-end acceptance | Joint. Record Core evidence, View/Flow evidence, and operational activation separately. Missing View delivery must not be described as Core failure or as a fully playable feature. |

Core completion does not require implementing a stand-in production UI or modifying another owner's Flow. It still requires integration tests using consumers/test doubles at the public boundary. Those tests do not replace later real UI/Flow acceptance.

### 1.2 Existing level-ups remain independent

Sub baseline attack/max health continue to use `UnitLevelGrowthRules`: level-one value plus `(level - 1) * category per-level growth`. Main continues to use the existing `MainGrowthRules` settlement and persistent stat state. These are separate existing rules, not interchangeable implementations to merge.

Victory XP settlement commits levels and existing stat growth exactly as before. Trait eligibility derives from that resulting level and configured steps. No trait step, an empty Main configuration, an outstanding choice/receipt, or a rejected choice may suppress, delay, repeat, or recalculate that XP/stat settlement. Choosing a trait changes trait history/composition only; it preserves level/XP and Main persistent growth values. Trait effects may influence gameplay through their existing effect contracts, not by reapplying baseline level-up grants.

## 2. Current source evidence

Paths below are relative to `Assets/ProjectL/Script/Systems/` unless stated otherwise. `C` means `RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/`.

| Owning source | Observed behavior | Consequence |
|---|---|---|
| `C/Application/Roster/TraitGrowthRules.cs` | `MaximumTraits = 2`; one Acquire and one Enhance slot; candidates match `CharacterKey`; candidates are returned without three-option sampling. | Both rules and data contracts must change; changing a constant is insufficient. |
| `C/Contracts/Enhancements/OwnedTraitGrowth.cs`, `TraitGrowthChange.cs` | Two optional selections; a change fills exactly one empty slot and preserves identity, level, and XP. | Replace fixed slots with immutable step selections while retaining the narrow mutation contract. |
| `C/Contracts/Roster/OwnedCharacterRecord.cs` | Rejects Main records with nonempty growth today. | Remove this specific restriction in the planned contract cutover; preserve all identity/role/value invariants. |
| `RuntimeSystem/InGameSystem/CoreSystem/CharacterSystem/CharacterRosterService.cs` | Candidate query and selection exist; selection rejects active battle and ended Run, then rechecks Run/revision before applying. | Reuse this service boundary and its source validation. Do not broaden roster acquisition prerequisites. |
| `C/Application/Enhancements/OwnedUnitSpecResolver.cs` | Composes growth with base traits for combat and owned-stat consumers. | Keep one composition path. Do not introduce a second trait calculation in View or Shop. |
| `DataBaseSystem/Data/InGame/Table/Category/Item/CategoryConfigDataSO.cs` | Does not supply the two Core trait unlock levels, which remain zero. | Current production SO cannot activate level choices. New authoring must be connected end to end. |
| `C/Application/Shop/EnhancementOfferPlanner.cs`, `EnhancementPurchaseRules.cs` | Sales use the same trait growth slots, ignore level unlocks, and target owned Sub instances. | Existing enhancement sales are incompatible with the new Main-only policy. |
| `RuntimeSystem/BufferSystems/Core/ShopTransactions.cs` | Prepares roster/currency/purchase changes before installing them; validates frozen offers and original state. | Preserve this transaction boundary for future Main growth sales. |
| `C/Application/Combat/Experience/CombatExperienceSettlement.cs` | Victory settlement applies XP/levels; one settlement can cross several levels. | Eligibility can be derived from settled level; no pending-level counter is needed in XP settlement. |
| `RuntimeSystem/LifeCycleSystem/LifeCycles/Combat/Plans/CombatPlan.cs`, `Jobs/CombatExitJobs.cs` | EndBattle and boss chapter completion precede result presentation; confirmation currently proceeds to Leave. | Insert growth presentation after confirmation, before leaving a continuing Run. Preserve settlement order. |
| `C/Application/Roster/MainGrowthRules.cs`, `C/Contracts/Shop/ShopProductKind.cs` | Main automatic level growth exists; shop product kinds do not implement Main growth purchases. | Main-only shop support is a separate feature, not a role-filter change. |
| `C/Application/Roster/UnitLevelGrowthRules.cs`, `CharacterBaselineStatsCalculator.cs` | Sub baseline attack/max health derive from level and category growth; Main baseline stats use persistent growth state. | Preserve both sources and test them with enabled and disabled trait progression. |

The earlier `2026-10-27-sub-trait-growth-design.md` records the fixed two-trait model. It is historical context, not authority for the new requirements. This proposal replaces its fixed limits, unit-specific candidate selection, and shared Sub shop path only after approval. Existing implementation remains unchanged until a later authorized phase.

## 3. Approaches

| Approach | Tradeoff | Decision |
|---|---|---|
| Ordered steps: `{ stepId, unlockLevel, kind }` | Represents counts and ordering together, including multiple steps at one level. | Selected by the user. |
| Separate acquisition and enhancement level lists | Slightly simpler input per kind, but needs a separate rule for interleaving steps at the same level. | Not selected. |

Do not add separate `MaximumTraits`, `AcquisitionCount`, or `EnhancementCount` authoring fields. Those would duplicate information already present in the steps. Inspector read-only summaries are permitted.

## 4. Authoring and immutable definitions

### 4.1 Category progression configuration

Propose a dedicated category trait progression table in InGame DB, with one configuration SO for each supported category. Keep movement, stat growth, and XP configuration in their existing owners. Trait content should not require editing the movement configuration implementation.

Each configuration contains:

- Category key: Main, Heavy, Rider, or Cannon, unique in the table. Main and Sub use the same step/offer contracts without sharing candidate pools implicitly.
- An ordered `steps` list: stable positive `stepId`, `unlockLevel >= 2`, and Acquire or Enhance.
- References to reusable trait-growth definition assets forming that category's pool.

Example only; these are not approved balance values:

| Step ID | Unlock level | Kind |
|---|---:|---|
| 10 | 3 | Acquire |
| 20 | 5 | Acquire |
| 30 | 7 | Enhance |

This grants two additional traits and one enhancement opportunity. Adding an Acquire row adds another opportunity; deleting a row removes one. An empty step list disables category trait growth. Unlock levels must be nondecreasing; list order resolves equal-level steps.

Provide an explicit Main configuration with empty steps/pool until its content is approved. Empty authoring means no Main opportunities currently configured, not that Main is prohibited. A valid nonempty Main configuration must work through the same rules without a new role-specific service or a later architectural rewrite. Do not infer Main candidates, counts, levels, or links to shop purchases from the Sub example.

Existing unit-native traits remain authored on the unit. They are not silently deleted or moved into the new pool. Final total trait count is native distinct traits plus completed Acquire steps; Enhance does not increase it. For a unit with one native trait, a target total of two requires one Acquire step. For a unit with no native trait, it requires two. This distinction must appear in authoring guidance and validation.

### 4.2 Trait definitions

Propose a reusable character trait-growth definition SO (`TraitGrowthDefinition`) containing:

- A stable `TraitKey` identity using the existing trait vocabulary.
- One normal `UnitTraitAuthoring` payload and an optional enhanced payload of the same `TraitKey`.
- Normal/enhanced display names and descriptions, converted into pure data for Core publication.

Use one registered definition per TraitKey; categories reference it instead of copying effect parameters. Use the existing trait authoring-to-Data-to-Spec adapters. Icons remain in View DB and are looked up by the published trait identity/form.

Do not repurpose `EnhancementKey`, a price, a sale weight, or a target unit key as the identity of a new category trait. The existing `EnhancementDataSO` bundles a Sub recipe with unit targeting and commercial data. Its runtime contracts can be retired from Sub growth as part of migration; do not build a generic target-selection framework around them.

Adding a definition for an existing effect is an authoring change. A genuinely new effect still requires its own Core rule/spec/adapter and effect tests; this design does not make arbitrary behaviors data-only.

### 4.3 Validation

At DB construction and in asset tests, validate unique category/step/trait identities, references, levels, payload identity, and supported effects. Main/Heavy/Rider/Cannon configurations are supported; Boss and enemy-only progression are outside this owned-character feature.

Validate each enabled category against its supported owned-player unit definitions, including Main, native-trait overlap, and effect capability requirements. A shared category is not proof that every attack-specific effect is legal for every unit. Preserve the existing Main definition identity/category/ownership policy; allowing trait history does not relax those invariants.

Acquisition must have three distinct eligible options at every reachable Acquire step, after excluding already owned traits and unavailable effects. Do not silently show one/two options, duplicate an option, auto-select, or consume an empty opportunity. With `N` acquisitions and a fully eligible common pool, at least `N + 2` initially unowned candidates are needed. Compatibility filters can require more.

Enhancement steps must remain satisfiable after any allowed earlier choice path. Each chosen trait can be enhanced once in this design; enhancement count is not a repeatable enhancement tier. Reject configurations with reachable zero-candidate enhancement steps. In particular, if later enhancement opportunities are promised, acquisition sampling must not expose choices that make them impossible. Prefer validating/authoring a valid pool rather than hiding this dependency with ad hoc runtime rerolls.

Definitions are immutable for a running runtime. SO edits take effect after rebuilding the runtime; hot-editing an active Run and disk-save migration are outside this scope.

### 4.4 One eligibility implementation

Introduce `TraitGrowthRules.EligibleTraits` as the shared pure predicate/selection function. It accepts the existing `CombatAttackProfile` basis, current owned TraitKeys, enhanced TraitKeys, step kind, category configuration, and immutable trait catalog. It owns category membership, trait availability/capability, duplicate exclusion, enhancement availability, and once-only enhancement checks. It does not inspect Run state, input receipts, random state, or Unity assets.

During Phase A execution on 2026-10-27, the user approved using `CombatAttackProfile` rather than `UnitSpec` alone for eligibility and validator inputs: targeting-method facts are required to enforce existing Pull, ImpactBolt and piercing compatibility. Reuse that existing compatibility contract rather than copying its predicates.

Phase A capability check also verifies distance-damage payload length for normal/enhanced candidates against authored attack range and existing splash offsets. Distance scaling with piercing-to-board-edge, map-wide kill-chain attacks or post-displacement chain explosions is not supported by this growth path until board/environment distance bounds are available. This conservative candidate restriction does not modify the existing combat rules or activate production content.

The SO catalog validator explores hypothetical key sets and calls this function at every node. Runtime `TraitGrowthRules.Candidates` obtains actual keys through the existing composition/history path and calls the same function. The sampler only samples the returned keys; it does not filter them again. Validation owns path traversal and sufficient candidate counts; runtime owns level/step/Run/offer admission. These different responsibilities must not contain copied eligibility predicates.

Introduce the shared function in the definition Phase, before writing validation traversal. Later runtime integration reuses it. Tests must compare validator and runtime eligibility for equivalent states, including native-trait overlap, unavailable effects, and already-enhanced traits. A rule change should require reading the eligibility contract and its consumers' admission contracts, not reconciling two separate predicate implementations.

## 5. Eligibility, offers, and choice rules

Owned Main and Sub instances participate when their category has configured steps. Category is read from the existing unit definition; Main uses `CategoryKey.Main`, and there is no fallback into Heavy/Rider/Cannon pools. Do not duplicate mutable category state on the ownership record or infer eligibility from shop product targeting.

For each owned instance, the next unresolved step is the first configuration row without a completed selection. It is eligible when the current level reaches its unlock level and the Run is continuing. Later steps cannot bypass it. Counts and readiness derive from configuration, level, and selections rather than stored counters.

Candidates:

- Acquire: the category's eligible normal definitions whose TraitKey is not currently owned. Uniformly sample three distinct keys without replacement.
- Enhance: currently owned traits represented in the category pool with an enhanced payload, excluding already enhanced traits. Uniformly sample up to three distinct keys. A native trait is eligible only if it has an explicitly registered compatible definition in that pool.
- Enhancing replaces the selected trait's payload with its enhanced payload; it does not append a second TraitKey.

Uniform sampling is a proposal; no separate balance-weight system is required by the request.

An offer is created once for an eligible step and frozen in Buffer until resolved. It contains Run ID, owned instance ID, step ID, a unique offer ID, and ordered candidate identities. Core samples; View never rolls. Reopening or refreshing the UI returns the same offer. Creating an offer is an explicit prepare/publish operation; ordinary reads remain side-effect-free.

Use an explicit seed for offer creation, including a stable owned-instance/step identity in deterministic sampling. Store the sampled offer, so a read does not depend on random-call order elsewhere. Exact hash/RNG implementation is an implementation choice with repeatability tests.

Selection carries the current Run/offer/owned/step identity and candidate key. Core revalidates ownership, category, current step, level, continuing Run, absence of active battle, frozen candidate membership, and current eligibility. Stale and duplicate requests do not change growth/offer state; handled failures may publish their typed receipt. Never accept a valid-but-not-offered candidate through the old direct-key API.

After a successful choice, its result is acknowledged before the next reached step can produce an offer. Multiple level jumps and multiple eligible units are handled sequentially, ordered by owned instance ID and then configured step order. Acquisition/starting at a higher level does not grant traits automatically: it makes reached unresolved steps available at the next safe presentation boundary.

## 6. State ownership and composition

Replace `OwnedTraitGrowth.Acquired/Enhanced` with an immutable ordered selection collection on the same owned record. Each entry identifies the configuration step and selected trait. Completed selections are the only persistent source of acquired/enhanced status; do not also store final traits or used-count counters.

Keep the frozen active offer in a character-growth Buffer state owned by the existing Character Buffer boundary. It is a different fact from eligibility: it records which random choices were actually offered. At most one active offer is presented for the Run at a time. Clear it with its successful selection or when the Run is reset/ended.

Generalize `TraitGrowthChange` to append exactly one validated step selection while preserving identity, role, level, XP, and the existing selection prefix. Buffer prepares the final record, offer completion, revision, and command receipt before installation. Failure cannot partially consume an offer or add a trait.

Keep `OwnedUnitSpecResolver` as the composition owner. Compose native traits and selected normal/enhanced payloads in configured selection order. Stats, deck presentation, attack profiles, and battle initialization use this same result. Do not update live combat state with post-battle selections.

Run-end handling discards pending offers and does not force players to choose traits for a finished Run. New Run initialization clears growth input/results/offer state through existing Buffer lifecycle boundaries, with Run identity preventing stale commands from applying.

### 6.1 Derived progress and acknowledgment barrier

`TraitGrowthProgressSnapshot` is a read-only result of the Core service, not a stored state machine. `ReadTraitGrowthProgress()` derives it from current Run/battle state, records, active offer, handled result number, and acknowledged result number. Core entry checks and Flow consumers use this same contract. View continues to read offers/results from Buffer and submit/acknowledge through its input contract; it does not call Core services directly or reconstruct progress from counters.

Evaluate statuses in this order:

| Status | Condition | Consumer action |
|---|---|---|
| `Unavailable` | Run ended or a battle is still active | Do not prepare/select growth. Flow continues its existing battle/Run-end policy; this is not ordinary growth completion. |
| `AwaitingAcknowledgement` | Continuing non-combat Run and handled result number is greater than acknowledged result number | Deliver/recover the retained result. Do not create another offer, process another choice, or permit new stage entry. |
| `ChoiceRequired` | No unacknowledged result, and a frozen offer or a reached unresolved step exists | Reuse the active offer or explicitly prepare one, then display it. |
| `ReadyToContinue` | Continuing non-combat Run, no unacknowledged result, no active offer, and no reached unresolved step | Growth no longer blocks normal Flow/entry admission. This does not bypass other progression checks. |

The snapshot contains `RunId`, `Status`, and `ActiveOffer` (nullable). A retained failure result may coexist with an offer; `AwaitingAcknowledgement` still takes precedence. Invalid acknowledgment numbers are rejected, not interpreted as completion. Reads are side-effect-free. No independent `pending`, `complete`, or `canContinue` flags are persisted.

Selection success atomically appends history, clears the consumed offer, and publishes its numbered receipt. Even for the last step, the next read is `AwaitingAcknowledgement`, not `ReadyToContinue`. View acknowledges exactly the result it consumed through the input Buffer. Acknowledgment removes/marks the processed input according to that input contract; it does not change history, clear another offer, or create the next offer. After acknowledgment, a fresh read returns `ChoiceRequired` or `ReadyToContinue`.

For a failed choice, keep the offer/history unchanged and publish the failure receipt; after acknowledgment the same offer can be shown again. Unbind/reopen does not auto-acknowledge a result. A rebound consumer can consume the retained result and acknowledge its exact number. NewRun/reset clears the old context; commands and acknowledgments carry Run identity so a stale caller cannot consume a new Run's receipt.

`PrepareNextTraitGrowthOffer()` returns the progress snapshot, including its status. It returns `AwaitingAcknowledgement` without sampling/writing when a result is outstanding, returns the same offer when one exists, and publishes only from `ChoiceRequired` with no active offer. It rechecks source state and the acknowledgment barrier before publication. Choice publication likewise checks the barrier before writing. Core stage entry rejects both `ChoiceRequired` and `AwaitingAcknowledgement`; `Unavailable` is handled by existing battle/Run-end admission rules. Flow must read fresh progress at the action boundary rather than caching `ReadyToContinue` across mutations.

## 7. Service, input, UI, and progression integration

Retain `CharacterRosterService` as the application boundary for preparing and applying character trait growth. Use the same rules, offer/receipt state and input contract for Main and Sub, selecting the appropriate category configuration. Factor calculation into the existing pure rules rather than adding a separate Main trait service or state store. Keep ordinary roster acquisition independent of growth-table readiness.

The current growth API signatures will require explicit revision or replacement for step/offer identities. This is a planned contract change: enumerate and migrate callers/tests in the implementation plan. Do not retain an unrestricted `ChooseTraitGrowth(ownedId, key)` compatibility path that bypasses an offer.

View follows the existing input pattern: submit a growth request to a narrow input Buffer; a Core-side processor reads it; results and offer snapshots are published for View to read. A dedicated growth input route is justified because it must operate across Combat completion and non-combat Stage presentation; the current Stage input DTO only represents node selection. Reuse existing request-number/receipt/lifecycle conventions, without changing Framework's generic queue or event systems.

Publication includes display-ready names, descriptions, trait identities/forms, target owned ID, step kind, and candidate count. View reads no InGame tables and writes no authoritative roster state.

The following is the integration contract handed to the View/Flow owners, not a Core implementation task:

1. Existing victory settlement commits XP and levels.
2. Existing flow completes playback, EndBattle, and any chapter completion, then displays results.
3. After result confirmation, a continuing Run resolves all reached growth steps before leaving Combat.
4. Each choice is committed, its result consumed, and the next offer presented.
5. Only `ReadyToContinue` allows the normal continuing-Run transition to resume. An ended Run follows its existing exit policy instead.

Keep the existing Plan as the sequencing owner. The Flow owner reads the §6.1 progress contract and adds actions at this boundary; it does not inspect eligibility/composition internals or duplicate receipt completion logic. Do not make a popup own scene transitions or duplicate the Job runner. Run-ending victory, defeat, and abandonment follow existing end destinations without growth prompts.

For higher-level acquisitions/development entry, the non-combat Stage readiness boundary invokes the same growth processing before another node can start. Core stage-entry checks must reject entry while growth remains unresolved, so hiding the UI cannot bypass the choice. This is a new gameplay gate proposed for approval.

View handoff requirement: create a UISystem-registered `UIBase` selection panel, with explicit UIId/parent/policy and one bound input receiver. Open/close goes through the existing Global UI context. Use `UIInputBinding`/`IUIInputReceiver`, preserve keyboard/controller submit, and restore input layers on completion/unbind. Do not use Inspector UnityEvents or `Button.onClick` closures. Actual widget decomposition, prefab layout, and scene wiring belong to the View implementation plan.

While a required choice is active, dismiss/back must not silently consume or reroll it. Proposed behavior: keep the panel mandatory until selection; application shutdown still works. Scene teardown unbinds presentation without inventing a completed selection. Rebinding reads the same frozen offer. Art/layout work is a later View task.

## 8. Enhancement shop boundary and migration

The new separation is intentional:

- Level-up trait growth for Main and Sub: configured steps, category pools, player choice, no money.
- Existing stat level-ups: preserve Sub category-derived stats and Main persistent stat growth independently of trait choices.
- Enhancement shop: Main growth purchases only, with separate product eligibility and effects.
- Ordinary unit/item shops: existing behavior retained.

Remove the Sub recipe offer/purchase route from the enhancement shop, including its calls to `TraitGrowthRules` and its production use of `TraitGrowthChange` in `ShopTransactions`. Reject stale Sub enhancement purchase commands before money, purchase-use, or roster state changes. Hiding offers in UI alone is insufficient.

Do not permit the old Sub `Enhancement` product to target Main by weakening its role checks. Main automatic `MainGrowthRules` is evidence of a growth calculation, not a complete purchase contract. Main products, amounts, prices, healing interaction, repeat-purchase limits, and stage economy require a separate Main-shop design. This document authorizes no guessed Main products.

Preserve frozen price/offer checks and the existing prepare-before-install currency/purchase transaction when Main sales are later added. Main shop eligibility does not determine level-up trait eligibility. Do not assume purchases consume level-up steps or that level-up selections consume a shop purchase; any future interaction requires its own explicit design.

Migration must include an explicit availability gate: until Main products exist, the old enhancement node cannot remain a reachable shop offering obsolete Sub products. The implementation plan must either coordinate Main-shop delivery before enabling such nodes or disable their authoring availability and validate no reachable reference remains. Do not silently generate empty shops or substitute free Main stats.

Inventory the four current Sub enhancement assets and all table/stage references. Translate retained Sub content into the category definitions only where its effect is valid for the whole configured pool; do not automatically spread unit-specific effects to an entire class. Preserve old asset GUIDs/files as historical assets when appropriate; never delete the protected Missing Script assets. Do not keep both old and new definitions active as competing sources for Sub growth.

There is no assumed in-place conversion of an already running old Run. Activate the new model on a fresh Run after runtime rebuild. If disk persistence of these records is found during implementation, stop that migration step and design an explicit versioned conversion rather than guessing.

## 9. Change locality and architecture review

| Real change | Current edit/understanding scope | Proposed scope | Necessary verification |
|---|---|---|---|
| Change Sub acquisition/enhancement counts | Fixed rules, two-slot record/change contracts, composer, service, shared shop consumers. | Edit category step SO rows; understand step ordering and candidate sufficiency. | Asset validation, boundaries/multi-level jumps; no shop economic retest for an isolated balance edit. |
| Add an existing supported trait to Heavy | Unit-specific recipes and their shop/growth filters. | Definition + Heavy pool + View visual mapping when needed; understand effect compatibility. | Candidate uniqueness/capability, acquisition/enhancement composition, affected combat effect. |
| Implement a new trait effect | Core specs/rules/adapters and effect consumers. | Same necessary effect scope plus pool registration. | Effect tests and combat regression remain required. Configuration cannot remove this dependency. |
| Change selection panel presentation | No current consumer of growth selection APIs. | Panel and published snapshot/input contracts. | Input, lifecycle, stale response, variable enhancement option count. |
| Change trait eligibility | A separately implemented validator/runtime filter would require reconciling both internals. | One `EligibleTraits` function, called by path validation and runtime candidates. | Shared predicate tests plus validation/runtime parity; retain both consumer integrations. |
| Change the result-confirmation sequence | A single pending-step boolean would force Flow to interpret Core receipt counters and internal ordering. | Existing Core service derives §6.1 progress; Flow follows status and public command/acknowledgment contracts. | Last-step acknowledgment, failure/retry, rebind, source change, and stage-entry rejection. |
| Change Main shop price/product | Current enhancement path shares Sub growth state. | Main product authoring/rules and existing shop transaction. | Main/currency/purchase atomicity and shop display, without Sub internals. |

SSOT: SO definitions own authored facts; Character Buffer owns selections and the actual offer; Core owns eligibility and composition; View owns only presentation. Derived totals/readiness are not independent mutable facts.

SOLID: growth rules, offer sampling, composition, purchase economics, and presentation have distinct reasons to change. Existing-effect additions use data registration; new-effect additions retain explicit rule integration. Invalid/stale requests never weaken existing mutation guarantees. Read/write contracts expose the minimum needed per consumer; pure Core receives immutable data rather than Unity SOs or Buffer internals.

Framework: create/register adapters and processors in the existing InGame Core Factory, expose them through the owning Group, and access Buffer through existing contracts. DB creation uses Catalog/Group/Table/Query registration; runtime Query remains immutable. UI/Input/Job consumers use existing Global context paths. AddModule only for actual lifecycle participation; Builder still builds once. Do not re-export child services at root for convenience.

These locality benefits are design expectations. Implementation review must verify actual call graphs, registrations, and test scope; file/class count is not evidence of improvement.

## 10. Acceptance and validation scope

No tests were executed for this design-only task. Existing source tests document the old behavior and must be migrated deliberately, not treated as proof of this proposal.

Required future coverage:

- SO conversion and real asset validation: zero/many steps, same-level order, invalid IDs/levels, missing definitions, unavailable effects, and insufficient candidate paths.
- Core: category isolation; three distinct Acquire options; frozen/repeatable offers; Enhance with one/two/three/more options; acquired and eligible native traits; no duplicate TraitKey or repeated enhancement; many acquisition/enhancement steps without a fixed cap.
- Progression: below/at/above thresholds, multiple crossed levels, interleaved steps, multiple owned instances of the same unit, high-level acquisition, and no automatic creation grants.
- Main eligibility: empty Main authoring yields no opportunities but is not an error; a valid configured Main fixture uses the same selection, enhancement, receipt, reset and composition path. Preserve Main definition invariants and keep its pool separate from Sub pools. These fixtures do not approve production Main content.
- Additive level-up behavior: with/without trait steps, Sub attack/max-health formulas and Main stat grants remain unchanged; waiting, retrying, acknowledging or selecting traits cannot grant XP/levels/stats again or undo existing growth.
- Transactions: stale Run/offer/record, replayed commands, ended Run, active battle, one revision/change per successful selection, and no partial offer/record mutation.
- Contract parity: SO traversal and runtime use the shared eligibility function; candidate counts, current-trait keys, and enhancement exclusion agree for equivalent states.
- Progress contract: last-step success remains `AwaitingAcknowledgement`; next-offer creation and stage entry stay blocked until exact result acknowledgment; failure retains its offer; rebind can consume a retained receipt; old-Run acknowledgments cannot affect a new Run.
- Integration: resolver results reach owned stats, deck, attack profiles, and next battle; XP changes preserve selections; Run reset clears growth state; required choice gates new stage entry.
- Shop: Sub offers absent, stale Sub purchases rejected without payment, ordinary shops unchanged, no reachable obsolete enhancement node; later Main purchases validated in their separate scope.
- View/Flow: reward confirmation ordering, continuing/final victory, defeat/abandonment, repeated bind/unbind, scene teardown, input restoration, back/submit behavior, variable enhancement option count, and no reroll on reopen.
- Run existing required regression/integration suites affected by shared record, Buffer, composition, and flow changes. Do not claim a Core pass proves View/Framework acceptance. Unity execution must use the authorized AppData-access path; no cache deletion or Unity upgrade workaround.

## 11. Approval boundary and next work

The companion implementation plan separates Phases A–C (Core implementation), Phase D (Core handoff), Phase E (external View/Flow integration and joint contract acceptance), and Phase F (separate Core/content and View/Flow activation evidence). None is authorized for implementation by this document-edit request. A Core delivery can be complete with the external work explicitly pending; the feature cannot be called production-ready until joint acceptance is complete.

Main level-up trait content/unlock balance, Main-shop products, and new trait effects remain separate content/design decisions. Main is supported by the common architecture now, with operational Main steps empty until decided; this does not block validated Sub content from being delivered. The plan must not label Main content as activated merely because its contract tests pass.

Policies to review explicitly: native traits remain; enhanced form is once per owned trait; acquisition always needs three valid options; candidate sampling is uniform; required choices block further stage entry; existing offers survive UI reopen; new configuration starts on a fresh runtime/Run.

Verification history: the original draft's independent builder review reported no important unanswered planning questions. Revision 2 addressed locality/ownership/acknowledgment; revision 3 incorporates Main eligibility and explicitly additive stat/trait growth. That earlier review is not claimed as a review of this revision. This update changes only the design and companion plan; no runtime code, SO assets, or tests are modified or executed.
