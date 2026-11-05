# Category and Unit Trait Rework

Date: 2026-10-26
Status: Phases A and B were implemented at local checkpoints; G-M is confirmed in section 6.2. The user confirmed G-P with obstacles excluded from Pull; Phase C is implemented and verified at its local checkpoint. Phase D code integration is implemented and reviewed, with review findings fixed and the two authorized test assemblies passing. Final manual scene/art acceptance remains open. This document alone does not authorize further implementation or test deletion.
Original planning source baseline: working tree on `feature/Cheon/Core/NewCombatSytem`, HEAD `1ba7aec4`.

## 1. Intent and decision labels

Remove obsolete Rider/Heavy effects, replace Heavy Shotgun and Cowboy base traits, and retire tests that only specify removed behavior. Preserve the shared player-turn system, category identity, existing Framework ownership, and unrelated combat rules.

- **Confirmed** means explicitly requested or accepted in this conversation.
- **Proposed** means a concrete design choice for review, not a user-confirmed gameplay rule.
- **Implementation choice** means a bounded technical detail that may be settled during planning without changing the behavior described here.

The original request produced this design without implementation. The user subsequently authorized Phases A and B of the separate phased task plan and explicitly confirmed G-M. Actual changes, verification and remaining acceptance are recorded in that plan section 9. Existing ownership boundaries remain in force for work outside those authorized phases; no commits or pushes were performed.

## 2. Confirmed scope

| Area | Required change |
|---|---|
| Rider | Remove the category's movement-then-attack refund effect. |
| Heavy | Remove the category's protection of allies in authored areas behind the provider. |
| Heavy Shotgun / Heavy | Replace its base distance-dependent damage trait with Hunter's Mark. Attacked enemies receive a mark; another allied unit's attack deals bonus damage and consumes that mark. No turn-based expiry before that attack. |
| Hunter's Mark configuration | Designer-authored bonus percentage, default **10%**. Marks **do not stack**. |
| Cowboy / Rider | Replace Frenzy with Pull, automatically included in a basic attack. Friendly units take no attack damage. Only units can be pulled; obstacles retain ordinary attacks within basic range. |
| Cowboy targeting | `Method_Type = 1`, `Method_ChoiceRange = 1`, `Method_ChoiceAngle = 0`: cardinal-line shape, single-cell selection, unrestricted facing. Default base attack range is **1**, not 2. |
| Pull configuration | Designer-authored additional range. Default Cowboy extension is **1**. Effective selection reach is current base attack range plus the extension. |
| Frenzy | Delete the trait itself, including its remaining additional-attack behavior and obsolete priority-debuff components. |
| Tests | Remove obsolete behavior tests; preserve distinct guarantees of surviving behavior. |
| Ballista | Deferred until new gameplay requirements arrive. Keep its current behavior and content. |

Category keys, matchup rules, movement allowance/cost, growth, physical defense, Cannon behavior, and Heavy's existing **knockback** immunity are not removed by the protection change. Do not interpret this task as deleting every Heavy defense-related mechanic or every AP-related trait.

## 3. Current source findings

Paths below are relative to the repository. `C` denotes `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/`; `S` denotes `Assets/ProjectL/Script/Systems/`; `T` denotes `Assets/Tests/EditMode/`.

| Evidence | Current behavior and implication |
|---|---|
| `S/RuntimeSystem/InGameSystem/CoreSystem/CharacterSystem/UnitSpecAdapter.cs`, `C/Application/BootstrapModels/CategoryRegistry.cs`, View `DevBattleInitializationSource.cs` | Production, default, and development category assembly still construct ManeuverRefund and HeavyProtection rules. Update all actual definition sources, not just one SO. |
| `C/Application/Combat/CombatAttackCalculator.cs`, `T/FrenzyAttackTests.cs` | Current shared-turn attack settlement does not invoke the Rider refund evaluator; `RiderMoveAndWaves_RespectOneActionAndNoRefund` already asserts no refund. Removal includes obsolete rule/registration/state cleanup; do not claim the current ordinary attack still refunds AP. |
| `C/Application/Combat/Categories/CategorySnapshotBuilder.cs` | Heavy snapshot construction requires a HeavyProtection definition. Removing only its registration would leave a failure. |
| `C/Application/Combat/CommandAttackPlan.cs`, `CombatDamageCalculator.cs`, `HeavyProtectionIncomingEffects.cs` | Protection is captured and passed through damage, follow-up, and reaction paths. Remove these dependencies while retaining other incoming-damage effects and defense ordering. |
| `Assets/ProjectL/DataBaseSO/InGameGroup/Unit/Item/Playerable/Unit_ShotGun.asset` | Key 11, Heavy, base attack range 2, `DistanceDamageScaleTraitAuthoring` with distance percentages. Change the base trait; preserve other stats/targeting unless explicitly amended. |
| `.../Unit_Cowboy.asset`, `.../Targeting/Item/TargetingMethod_1.asset` | Key 19, Rider, base range 1, Frenzy. Its current referenced method is radial-square/single. The requested cardinal shape therefore requires actual authoring migration. Do not edit a shared method asset without checking other consumers. |
| `C/Application/Combat/FrenzyAttackLedger.cs`, `FrenzyAttackCalculator.cs` | Hit-time priority modification is already inert, but direct kills still earn attack waves. This is not merely removal of a live AP penalty. |
| `C/Application/Combat/AttackDeliveryExecutor.cs`, `AttackKnockbackCalculator.cs` | Existing knockback is after direct damage and resolves terrain along the path. Pull needs movement before direct damage; reversing knockback direction alone is insufficient. |
| `C/Application/Combat/CombatAttackQueries.cs`, `C/Domain/Combat/AttackTargetingPolicy.cs` | Query and execution already share targeting logic and owner eligibility. Friendly-target capability exists. Preserve the existing deferred multi-cell-unit admission rule. |
| `C/Contracts/Combat/CharacterDebuffState.cs`, `C/Application/Combat/CombatStatusLifecycle.cs` | Target status is part of immutable character facts; lifecycle dispatch currently includes defense reduction. Add mark ownership here rather than creating a parallel View state store. |
| `S/RuntimeSystem/GlobalSystem/UISystem/Screens/Global/UnitCardTraitContent.cs` | Shared card descriptions consume trait snapshots and View visual data. New configurable values need snapshot-driven descriptions, not a hard-coded 10% text. |

These are source observations, not fresh runtime verification. Existing uncommitted CoreIntegration test migration and other working-tree changes belong to separate work. Recheck their current locations before planning test edits.

## 4. Approach

**Recommended:** extend the existing trait registration, immutable combat projections, target-status facts, and result publication. Keep Hunter's Mark and Pull rules separate; share only the existing movement, damage, and command contracts that they actually need.

Alternatives considered:

1. Special-case Cowboy/Shotgun entity keys inside damage and View input. Small initial patch, but duplicates targeting/preview decisions and makes unrelated units harder to change. Rejected.
2. Build a new universal skill framework and separate status/movement service. Unnecessary for two traits and duplicates existing ownership. Rejected.
3. Extend current trait boundaries and extract only forced-movement mechanics that are demonstrably shared. Recommended; ordinary registry/factory changes are expected.

The user requested the design now. This recommendation is documented for review; no separate implementation-architecture approval is implied.

## 5. Cowboy: range and action contract

### 5.1 Confirmed geometry

Let `A` be the current effective basic attack range, including existing range modifiers, and `P` the configured Pull extension. `R = checked(A + P)` is derived, never a separately authored or persistent stat. The production Cowboy starts with `A = 1`, `P = 1`.

For a target on a cardinal ray at distance `d`:

| Condition | Behavior |
|---|---|
| `1 <= d <= A` | No pull. Apply the ordinary attack to a hostile target or attackable Obstacle. A friendly unit selection is rejected without cost/action use. |
| `A < d <= A + P` | Pull a unit inward along that ray to distance **exactly A**, then apply the attack if the target successfully arrives alive. Friendly units receive movement only. Obstacles/structures are not eligible through this extension. |
| Outside a cardinal ray, or `d > A + P` | Not a valid selection for this trait. |

For `A = 3`, `P = 2`: distances 1–3 are ordinary attacks; distances 4–5 pull to distance 3; distance 6 is out of reach. Do not pull to an adjacent cell when `A > 1`, or pull every target by a fixed `P` cells. Actual travel is `d - A`.

No splash is proposed for Cowboy. `P = 0` is a valid configuration that disables the extension. Pull and PiercingFalloff cannot share one attack profile in this phase: the existing profile validation rejects that unsupported composition before friendly selection can enter piercing damage. No hybrid delivery semantics are introduced. Negative extension, unsupported shape/mode/splash combinations, and range overflow are configuration errors, not silently ignored data. New parameter names may be finalized during planning.

### 5.2 Approved edge rules (G-P, 2026-10-26)

- Pull uses the normal basic-attack command and its one attack cost/action-use settlement. It does not issue a voluntary movement command, spend the victim's AP, or consume the victim's voluntary action.
- Friendly units are eligible only for this trait's movement, not for damage/ordinary hostile-hit effects. A friendly target already inside `A` has no effect: reject without cost or action use. Self-targeting remains invalid.
- Check the full straight path, intervening occupancy, wall crossings, board bounds, and destination occupancy before geometric displacement. An obstructed path is rejected with no movement, no attack damage, and no cost/action use. This extends the confirmed rule that failure to pull into attack range causes no attack damage; the rejection/cost policy was approved by the user on 2026-10-26.
- Do not move to an alternative side or stop at a merely convenient cell outside `A`. Do not teleport through intervening units or Obstacles.
- Respect explicit forced-movement immunity. Heavy category membership alone currently grants **knockback-specific** immunity; it does not prevent Pull by itself, as approved by the user.
- Reuse existing per-cell forced terrain/arrival processing. The friendly-damage exemption concerns the Cowboy attack; existing terrain hazards may still hurt a moved ally. If hazards kill the target or end combat during movement, keep resolved movement/hazard effects and do not deliver the attack. Such an accepted action spends its normal attack cost once.
- Obstacles and other structures are excluded from Pull. Preserve their ordinary attack behavior within the effective basic range `A`; the extension `P` does not make an outer obstacle/structure selectable or attackable. Never move their footprint or publish Pull movement for them. Existing after-hit knockback remains unchanged. Multi-cell unit combat remains out of scope.
- Recompute damage and positional defenses from the arrival position. Keep target identity through the pull; do not attack the vacated selection cell or retarget a different entity.

### 5.3 Calculation and publication boundary

Use a shared, immutable range/target evaluation for range queries, preview, AI evaluation, and execution. It must distinguish `A` from `A + P`, qualify the selected target, and describe destination/failure. Do not overwrite `AttackRange` with total selection reach, since that would make distant targets receive damage without pulling.

The execution sequence is: admission and cost validation; resolve Pull in the existing command-local projection; resolve terrain/arrival; if still valid and alive, calculate direct damage at the new position; apply ordinary after-hit effects; settle the attack once; publish through the existing transaction. Ordinary attacks without Pull retain their existing order.

`AttackDeliveryExecutor`/attack admission currently assumes direct delivery before forced movement. Planning must explicitly update this order and rebuild/re-evaluate target delivery against the projected position; adding a late hook is insufficient. Reuse `ForcedDisplacementPathCalculator`, movement projection, footprint checks, and environment machinery where their contracts fit. Do not copy the knockback calculator wholesale or rewrite knockback's damage-before-movement rule.

Result contracts must expose the original target identity, path/original and final position, no-damage friendly outcome, and failure reason. Existing result/event contracts may be extended where needed. Publication must include unit occupancy changes and HP/status changes atomically. Obstacles retain their existing ordinary attack/publication contracts. Preview operates on copies and honors existing visible-environment rules; it must not reveal hidden hazards.

## 6. Heavy Shotgun: Hunter's Mark

### 6.1 Confirmed behavior and configuration

Heavy Shotgun remains Heavy. Its attack applies Hunter's Mark to attacked enemy units. Another allied unit attacking that enemy gains the configured bonus and consumes the mark. The mark has no turn-count expiry and cannot stack.

Author a `bonusDamagePercent`-style integer in the trait's SO authoring entry, default **10**. Convert it through the existing pure Data -> TraitSpec -> definition/registry path. Do not place the gameplay percentage in View data. Propose nonnegative percentages, allowing 0 and values above 100, with checked arithmetic. The new trait/status IDs must not reuse retired serialized IDs.

### 6.2 Proposed precise mark semantics

**G-M confirmed (2026-10-26):** After authorizing Phase B, the user explicitly accepted the design proposals in sections 6.2–6.3. The following trigger, non-stacking, consume-before-reapply, shield/zero-damage, lifetime and percentage-basis rules are now approved for Phase B. Nonnegative configuration includes zero and percentages above 100; checked arithmetic rejects an unrepresentable damage result. G-P was unresolved at that Phase B decision; section5.2 now records the later approved Phase C rules.

- One immutable mark slot per enemy character, carrying source entity ID, source owner, and the bonus percentage captured at application. No list of stacks or duplicate runtime store.
- Apply on a successfully delivered direct attack hit to a surviving enemy character, including a hit absorbed by defense. Do not mark friendly units, Obstacles, structures, corpses, or targets merely inside a preview area. Existing Shotgun attack shape can mark every qualifying hit enemy.
- The source's own later attacks do not consume its mark. Any different unit on the recorded source team can consume it; eligibility is by entity identity, not unit type.
- Non-stacking refresh proposal: if a mark already exists and the hit does not consume it, keep the existing source and percentage unchanged. Repeated hits do not accumulate percentages or change attribution.
- Evaluate and consume an existing eligible mark before applying any new mark from the current attacker. Thus a second Shotgun can consume the first Shotgun's mark, then place its own mark on a surviving enemy. At every point there is at most one mark.
- A successful qualifying direct hit consumes the mark even if shields or rounding make the bonus contribute zero final HP damage. Rejected/blocked attacks and non-damaging friendly Pull do not consume it.
- For multi-hit attacks, only the first qualifying delivered hit consumes that mark. Ordinary trait-generated follow-up attacks may qualify; environment damage, damage-over-time, reflections, and reaction attacks do not. No extra attack group or recursive attack is created solely by the bonus.
- Mark state survives turn changes, movement, and source death/removal; victim death or battle disposal removes it. Its owner/percentage snapshot avoids needing to dereference a dead source.

### 6.3 Proposed percentage basis

The user confirmed percentage configurability and default 10%, but not the damage stage. Recommended: multiply the consuming attack's target-specific damage **after the existing attack damage calculations and before held-defense/shield absorption**. This keeps defense, category matchup, terrain, and other target modifiers in their existing order, with no separate damage event.

Let `D` be the nonnegative result that `CombatDamageCalculator.Attack` would otherwise return, before `ApplyHeldDefense`, and `p` the mark percentage. The marked hit is `RoundHalfUp(D * (100 + p) / 100)`, using the existing percentage-rounding helper with checked intermediates. The bonus is the difference from `D`; it does not use the Shotgun's attack power or the victim's maximum health. Example: `D = 20`, `p = 10` yields 22 before shield absorption. No separate minimum-one-damage rule is added to the bonus.

This basis, low-damage rounding, reapplication, and trigger rules above are reviewable proposals. They must be resolved before implementing mark damage and status tests; they are not gaps an implementer should silently guess.

### 6.4 State ownership

Add the immutable mark component to the existing character combat status/facts boundary. Preserve it through fact copies, movement projections, snapshots, equality/expected-state checks, and publication. Add the new trait to existing registration/codec/status mapping and authoring adapters. Keep application/consumption rules feature-local; normal factory/registry entries are legitimate integration changes.

Preview and AI evaluate the same candidate damage and state changes on projections without consuming the live mark. A mark is combat-instance state; it must not persist on run-deck/owned-unit records or become a second mutable state in the View.

## 7. Removal and content migration

### 7.1 Rider and Heavy

Remove ManeuverRefund and HeavyProtection definitions, rule/state construction, category-specific capture and snapshot requirements, authoring values, obsolete activations, and dedicated tests once reference disposition is complete. Rider/Heavy snapshots become stateless with respect to these effects. Preserve unrelated category facts.

Known integration points include `UnitSpecAdapter`, `CategoryRegistry`, `CategoryRuleFactories`, `CategorySnapshotBuilder`, `UnitDefinition`/`UnitSpec`, `UnitData`/`UnitDataSO`, `CategoryConfigData`/SO, `OwnedUnitSpecResolver`, development fixtures, and attack plan/damage/follow-up callers. Remove `heavyProtectionOffsets` and `heavyDamageReductionPercent` from active authoring and validation; do not require dummy protection data for Heavy units.

Trace removal of the protection query through `CombatSystemFactory` -> `HeavyProtectionQueryService` -> Core group export/access -> `IHeavyProtectionReadSink` consumers. Remove retired APIs and all in-repository consumers together, record the intentional contract removal for View owners, and do not register a replacement root service that always returns an empty result. A required external consumer migration is a phase dependency, not permission to silently break a consumer.

Do not delete the generic incoming-damage engine or refund-related APIs solely because these category rules disappear. First establish whether another surviving rule uses each contract; remove only exclusively obsolete pieces.

### 7.2 Frenzy

Remove trait authoring/Data/Spec/definition/runtime marker, registry/factories/codec/status, priority debuff component/calculator, attack wave ledger/calculator/events, special delivery arguments, visual registration/content, and dedicated fixtures after identifying all references. Preserve independent Intimidation, defense-reduction, KillChain, DoubleShot, cost settlement, and generic playback behavior.

Do not rename a serialized Frenzy managed-reference class into Pull and reuse its old payload. Migrate `Unit_Cowboy.asset` to the new trait explicitly, remove the retired visual-table entry before any asset deletion, and reserve rather than recycle old enum numeric IDs. Avoid unrelated enum renumbering.

### 7.3 Heavy Shotgun and authoring safety

Replace `DistanceDamageScaleTraitAuthoring` on `Unit_ShotGun.asset`. This task does **not** yet authorize deleting the reusable DistanceDamageScale trait globally: only Frenzy was explicitly requested for global deletion. Keep its generic implementation/tests unless a later approved scope decision removes it. Rewrite assertions that require Shotgun specifically to own that trait.

The inspected asset search found DistanceDamageScale on Shotgun and Frenzy on Cowboy. This is not proof that all code-driven definitions, import sources, or external content are unused. Check those consumers during migration. Do not modify shared targeting assets without a reverse-reference check; use a compatible existing cardinal method or a dedicated authored entry.

Preserve remaining asset GUIDs, unit keys, and unrelated stats. Pair approved source/asset deletion with `.meta` handling, preserve GUIDs on moves, update catalogs and serialized references, and do not delete the protected legacy Missing Script assets. New code comments/XML docs remain Korean; implementation documents remain English.

## 8. View and Framework integration

Core owns range, Pull success, damage, status, and target identity. The existing Buffer/result publication remains authoritative. View consumes range/preview/status/results, never computes `A + P` from InGame DB or writes combat facts.

Required View contracts: distinguish attack cells from extension cells where helpful, show the resolved destination and friendly zero damage, animate Pull before hit damage, publish/show one mark per target, remove the consumed mark, and display current trait values on cards. Reuse existing input, playback, and UI registration. No new skill button or action mode is needed. Rule percentages/range values come from exposed snapshots; visual icons/text/templates belong to View DB.

Framework checks: FW-001/002 service/module registration; FW-003 one-time Build; FW-004 no state-owner bypass; FW-005–007 existing input submission; FW-008/009 existing UI ownership; FW-010–012 existing rendering/resource ownership; FW-013–016 event, queue, pool, and lifecycle guarantees. No Framework, Buffer, or Job responsibilities are reimplemented. Query-service removal and any result-contract additions require a complete factory/group/consumer trace.

## 9. Test disposition

Deletion is based on obsolete behavior, not failure counts or filenames. The concurrent [Core test foundation disposition](../plans/2026-10-26-core-test-foundation-disposition.md) owns its separate migration; reconcile source locations and do not restore or delete its work incidentally. Never erase required coverage merely because a legacy fixture no longer boots.

| Existing tests / cases | Proposed disposition |
|---|---|
| `T/RiderManeuverRefundTests.cs`: refund activation, used flag, reset, allocation, obsolete settlement | Retire removed-rule cases. `SettlementPlan_AppliesSpendAndRefundAsOneFinalValue` uses a private test-only `RefundSettlement`; delete it with that helper, rather than treating it as production transaction coverage. Keep generic validation tests only if the validated API survives. |
| `T/HeavyProtectionAreaTests`, `HeavyProtectionCaptureTests`, `HeavyProtectionDefinitionTests`, `HeavyProtectionQueryParityTests` | Retire cases specifying removed protection geometry/state/query. Search helper consumers before deleting whole files. |
| `T/HeavyProtectionIncomingEffectsTests.Engine_CapsCombinedContributionsAtIncomingDamage` | Preserve the generic incoming-effect cap guarantee with surviving or minimal synthetic effect contributions if that engine contract remains; remove Heavy-specific setup. |
| `T/HeavyProtectionAttackIntegrationTests` | Remove protection expectations. Preserve independently useful held-defense, reflection, terrain, reaction/follow-up, and preview-purity assertions in the existing owning suites before retiring shared fixtures. Avoid duplicate replacements when equivalent passing coverage already exists. |
| `T/FrenzyAttackTests`, `FrenzyPriorityTests`, `FrenzyScheduleTests`, `FrenzyHitAccountingTests`, `FrenzyPreviewTests` | Retire wave/priority/retired marker behavior. Preserve the ordinary single-action/no-refund guarantee from `RiderMoveAndWaves_RespectOneActionAndNoRefund` in a surviving player-turn test without Frenzy. |
| `T/FrenzyStatePreservationTests` | Remove Frenzy state/codec cases. Preserve independent status copy/equality/immutability guarantees; add mark-specific coverage where the new state actually crosses those boundaries. |
| `T/Framework/FrenzyAuthoringTests`, `FrenzyPublicationTests`, `HeavyProtectionAuthoringTests`, `HeavyProtectionQueryServiceTests` | Retire feature-specific cases. Keep generic database/publication invariants in their proper owners and remove orphan helpers only after a reference search. |
| `T/Framework/FrenzyViewContractTests.UnsupportedWaveMarker_DoesNotSuppressLaterHealthEventsOrFinalization` | This includes a generic playback-continuation guarantee. Retain it in playback tests with a surviving unsupported event or appropriate test event; remove only the Frenzy dependency. |
| `T/DistanceDamageScaleTraitTests` | Retain the generic trait suite under current scope, including non-trait damage behavior. Update Shotgun-specific authoring/content expectations elsewhere. |
| Category/bootstrap/trait-contract/DB-asset/shared fixture tests | Remove obsolete required fields/counts/registration expectations, preserve remaining contracts, and add exact new values. No wholesale deletion by keyword. |

Whole-file removal requires every test and helper in that file to have a recorded disposition. Move files with their `.meta` GUIDs; split tests into existing owners or the appropriate current test assembly without linking test assemblies together. Plan execution must record method-level decisions where a suite mixes retired and retained behavior.

### Required replacement and regression coverage

- **Removal:** production/default/dev category construction accepts Heavy without protection offsets; Rider movement plus attack gives no refund; removing protection leaves physical defense, held defense, matchup, terrain, and knockback immunity unchanged; Cowboy no longer emits Frenzy waves/priority state.
- **Pull:** default `A=1,P=1`; `A=3,P=2` distances 1–6 in all four rays; live range modifiers; `P=0`; diagonals rejected; exact outer-edge destination; friendly zero damage; unchanged ordinary obstacle attacks/occupancy and rejected extension-only obstacle attacks; blocked path/destination; configured immunity; hazard death/terminal handling; single action/cost; no victim AP/action change; no stale-cell attack.
- **Mark:** default 10% and edited authoring values survive to runtime/display snapshots; no stack; same-source hit; different ally; multiple Shotguns under reviewed reapplication rule; turn persistence; source death; victim death; percentage rounding/shields; multi-hit once; rejected/nonqualifying hits; no live-state mutation by preview/AI.
- **Transactions:** Pull moves and mark/HP changes publish exactly once; stale expected-state rejection preserves all stores; query/preview/execution agree on shared visible information; Obstacle occupancy and unit status remain consistent after publication.
- **Content/View:** real `DataBaseAssetTests`, trait visual-table references, card descriptions, range/destination display, Pull-before-hit playback, mark apply/consume display, scene exit/cancel lifecycle.

Run meaningful focused tests, the required Core and integration suites, and affected Framework/View suites after their owning changes. Run Unity compile/EditMode and manual acceptance for the integrated phase. Unity must use the approved AppData-access execution path on this machine; do not upgrade Unity or delete caches to address the known sandbox issue. Record baseline failures separately, and do not claim Core-only success establishes full View acceptance.

## 10. Change independence and SOLID/SSOT review

| Real change | Current modification/understanding scope | Intended scope after this design | Required verification |
|---|---|---|---|
| Tune mark bonus | Feature absent | Trait authoring value -> existing immutable conversion -> mark rule/snapshot. No Cowboy movement internals. | Data validation, damage rounding, snapshot/card output. |
| Tune Cowboy extension | One range currently drives selection | Pull extension authoring plus shared range contract; `A+P` stays derived. No Shotgun status internals. | Query/preview/attack range and destination parity. |
| Change Pull path behavior | Knockback owns damage-after-delivery displacement details | Pull owns eligibility/destination/order; shared movement primitives own collision/terrain. | Pull and the actually shared knockback/environment boundary. |
| Remove protection | Assembly, snapshot requirements, per-attack capture, query, authoring | One bounded removal across proven consumers; no inert compatibility provider or duplicate defense path. | Category construction, surviving damage rules, consumer compilation. |

SRP: each trait owns its rules; orchestration owns stage ordering; status facts own combat state; publication owns installation. OCP: add effects through current registration while allowing necessary integration changes. LSP: preserve command failure/preview/immutability and service lifecycle guarantees. ISP: expose only needed targeting/status/result data. DIP: pure Core consumes definitions/contracts, View consumes Buffer/query contracts, and factories assemble dependencies.

SSOT: one authored percentage/extension, one status instance per target, one derived range calculation, one attack cost owner, and one publication transaction. Fewer files or more interfaces are not success criteria. The narrower change scope above is an expected design benefit, not a measured implementation result; evaluate it against the final diff and regression results.

## 11. Review points and later phase boundaries

Confirmed gameplay is captured in sections 2, 5.1, and 6.1. G-M was explicitly confirmed for Phase B as recorded in section 6.2, including sections 6.2–6.3's precise semantics and percentage basis. G-P is confirmed: section 5.2 records the approved cost, immunity and terrain rules and the explicit exclusion of obstacles from Pull.

Suggested planning boundaries, not an executable task plan:

1. Remove Rider refund/Heavy protection/Frenzy with coordinated contract/content migration and case-level test retirement. Stop and report.
2. Add Hunter's Mark after its proposed semantics are approved. Stop and report.
3. Add Pull with range/admission, movement-before-damage, publication, and regression coverage. Stop and report.
4. Complete owner-coordinated View/content integration and manual acceptance. Stop and report.

Every intermediate phase must state its playable content policy: removing Frenzy before adding Pull temporarily leaves Cowboy without a base trait, so do not ship that state as the completed feature. If intermediate playability is required, combine the relevant migration at the cutover instead. Coordinate shared contract consumers within each compiling phase rather than leaving owners with unexplained broken builds.

Ballista, a general skill framework, multi-cell-unit combat enablement, blanket removal of DistanceDamageScale, and unrelated architectural/test repair remain outside scope.

## 12. Design verification record

This document was grounded in current source and representative tests, including current shared-turn settlement and existing attack/forced-movement order. No Unity run, test execution, gameplay implementation, or deletion has been performed for this design task. Existing working-tree modifications are external to this document.

A separate builder review found no additional essential planning questions or material contradictions beyond the explicitly listed gameplay proposals. It checked Pull ordering, single-slot mark consumption/reapplication, mixed-test preservation, and compiling phase boundaries. The implementation plan must make each phase's content state and coordinated consumer migration explicit. Document checks found no replacement characters or trailing whitespace and confirmed the default 10%, non-stacking rule, and `A = 3, P = 2` example.


## 13. Phase C approved scope correction — 2026-10-26

The user approved G-P rules 1–3 and rejected obstacle Pull. Pull targets units only. Obstacles/structures keep ordinary attacks within A, are not eligible through P, never move because of Pull, and produce no Pull event. This supersedes earlier obstacle-Pull proposals throughout this document, including whole-footprint movement and publication requirements. Required obstacle regressions now prove unchanged inner-range attacks, rejected extension-only attacks and unchanged occupancy. Core and CoreIntegration are the only authorized test assemblies for Phase C.
