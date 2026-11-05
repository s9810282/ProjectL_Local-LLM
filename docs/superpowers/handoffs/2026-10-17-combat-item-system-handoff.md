# Combat Item System — Implementation Handoff

Date: 2026-10-17. Scope: accepted implementation plan A1-F2. The user owns commits and pushes. Astra review is explicitly deferred to the user; no Phase F Astra review was performed.

Source of gameplay policy: design (Git history: `2026-10-17-combat-item-system-design.md`). Task status, source ledgers and test provenance: implementation plan (Git history: `2026-10-17-combat-item-system-implementation-plan.md`), especially sections 10.1, 10.4 and 10.8. This document describes the implemented consumer and maintenance boundaries; it does not replace either source of truth.

Updated 2026-10-17 by the locality follow-up (Git history: `2026-10-17-combat-item-locality-followup-design.md`) (F1-F5, execution record (Git history: `2026-10-17-combat-item-locality-followup-plan.md`)). Gameplay, event order and public results are unchanged. Names below reflect the follow-up; the implementation plan's sections 10.x keep the original names as history.

Updated again 2026-10-17 by the composition authority fix (Git history: `2026-10-17-item-composition-authority-design.md`): inventory writes now go only through Buffer transactions, and the item Inspector moved into Systems. Owner notice: [DB/Tool.Editor share](2026-10-17-item-composition-db-tool-share.md).

## 1. Production composition and authored content

The existing `DatabaseCatalogSO.asset` selects `InGameGroup.asset`. That group now includes `Assets/ProjectL/DataBaseSO/InGameGroup/ConsumableItem/ConsumableItemDataTable.asset`. The table is mandatory in real `InGameDataBaseFactory` composition, registered once as `IConsumableItemTableQuery` and exposed through the narrow `IHasConsumableItemTable` facet. Legacy imported item rows are not runtime behavior or price authorities.

| Key | Asset / effect | Cost | Approved parameters |
| --- | --- | --- | --- |
| 1 | Whiskey | 100G | After positive actual damage, surviving holder at or below 50% maximum HP; recover 10% maximum HP |
| 2 | Roket | 150G | Hostile ordinary attack; pre-item incoming damage at least 20% maximum HP; keep 50% damage, rounding down |
| 3 | Rope | 200G | Ordinary attack completion; nearest surviving directly hit enemy; priority penalty 10, minimum priority 1 |
| 4 | Lantern | 200G | Surviving holder receives hostile ordinary attack, including zero damage; attacker's defense penalty 5, minimum defense 0 |

Each SO exports fresh immutable data. `ConsumableItemDataSO.GetData()` rejects only a missing payload; it keeps no list of supported behaviors. `ItemRuleRegistry` is the single authority for supported behaviors: `ItemDefinitionAdapter` validates all exported specs through `ItemRuleRegistry.Validate` and rejects duplicate keys before returning a definition batch. `DatabaseBattleInitializationSource` captures selected definitions into the battle definition; subsequent calculations use that immutable selection. The grant service also validates its selected definition batch during initialization. Invalid/missing authoring data does not become a partial mutable battle installation.

`InGameCoreSystemFactory` registers `ItemInventoryService` once. The service participates in existing InitData/PostBind lifecycle and is exposed as `IInGameCoreSystemGroupService.Items`. In `PostBindService` it takes only `ItemInventoryReadSink`, `RunProgressReadSink` and `ItemInventoryWriteSink`; it does not keep `IBufferQuery`.

Composition points outside the Core factory are limited to the conventions every feature follows:
- `BufferSystemFactory` registers `ItemInventoryBuffer` (every Buffer is registered there).
- `ItemInventoryBufferKey` holds the Read and Store keys. The Store key is resolved only inside `BufferSystemGroup`.
- The DB group registers the table. These DB registrations were announced to their owners and stay.

The item Inspector lives in `Systems/DataBaseSystem/Editor/ConsumableItemDataSOEditor.cs` (`#if UNITY_EDITOR`). `ProjectL.Tool.Editor` does not reference Systems, and Systems grants it no internal access. No new service container, combat loop, event bus or package is introduced. Item-free test/development query contexts without the optional facet remain usable; attempting a grant through such a context throws before a stock write. Real database composition does not silently omit the mandatory table.

Production stock starts empty. No production starting grants, reward odds, shop purchasing rules or enemy assignments were invented. Existing stage assets retain empty item assignments. An explicitly authored enemy placement can supply `startingItem`; export retains only its key, and preparation validates that reference against the selected catalog. Runtime redeployment does not grant that item again.

## 2. Consumer entry points

| Intent | Existing route / new contract |
| --- | --- |
| Grant acquired items | `core.Items.Grant(itemKey, quantity)` returns `ItemGrantResult`; use this for a future authorized reward/shop integration after its acquisition policy succeeds |
| Read shared Run stock | `buffers.ItemInventoryReadSink.Current`, an immutable `ItemInventorySnapshot`; use `GetCount(key)` or `Counts` |
| Equip / replace / return | `CombatViewCommandData.EquipItem(entityId, key)`, `ReplaceItem(entityId, key)`, `UnequipItem(entityId)` through the existing combat input submission/acknowledgement route |
| Inspect character equipment/effects | Published `CharacterData.Items` and corresponding `CharacterCombatFacts.Items` |
| Display defense | `CharacterData.EffectivePhysicalDefense`, calculated in Core; base `Stats.PhysicalDefense` remains unchanged |
| Consume item outcomes | Existing command result event sequence, filtering `CombatItemEvent` by stage/reason rather than evaluating gameplay again |

Grant does not equip. Equipping transfers one shared stock entry into a living field character's slot. Preparation-only player commands support main and subcharacters; enemy configuration uses the initialization boundary. Return and replacement refund the old item. Same-key replacement is an accepted no-op and needs no spare copy. Preparation recall returns equipped player stock before removing the field placement. Unsupported state, missing stock, stale facts and replayed command identity cannot partially install transfers.

The mutable inventory store is reachable only by Buffer transaction classes, and all three use the same store instance:
- `ItemInventoryTransactions` (`IItemInventoryBufferWriteSink.PublishGrant`) installs grants. It runs Run-in-progress guard, then Prepare against the expected snapshot, then Install.
- `BattleTransactions` installs equip, unequip and return refunds with command publication.
- `ProgressTransactions` resets stock for a new Run.

`IBufferQuery` exposes the read sink and the grant write sink, never the store. The store methods on `ItemInventoryBuffer` are explicit interface implementations. Grant eligibility and quantities stay in Core (`ItemInventoryService` / `ItemInventoryCalculator`). View reads snapshots and submits commands; it must not edit inventory, write character facts, call item rules or read InGame rules to reproduce calculations. UI widgets, buttons, VFX and purchase/reward callers are outside this implementation.

## 3. Calculation, publication and event order

`CombatItemResolver` dispatches three explicit capabilities: before damage, after damage and attack completed, and resolves the equipped definition (`EquippedDefinition`). Individual rules are stateless decisions. `CombatDamageApplication.Apply(recipientId, CombatDamageKind kind, CombatDamageOrigin origin, damage)` is the shared application boundary after all existing non-item damage calculations. It applies Roket before actual HP loss, then eligible post-damage item decisions against the latest surviving holder. Healing is capped by `CharacterSettlementCalculator.Recover`; it cannot revive a dead holder or trigger a second damage hook.

- **Damage kind** is a required argument of `Apply` (`Contracts.Combat.CombatDamageKind`, values 1-6, 0 undefined so `default` is rejected). The damage path that calls `Apply` decides it; `CombatDamageOrigin` carries only position, credited character and attack group. The attacker for item rules is `origin.CreditedCharacterId`.
- **One instance per command context.** `CommandAttackProjection.Damage` is shared by the arrival `MovementProjection`, obstacle reactions, kill chain, frenzy, thorn fields, knockback and thorn-heart reflection. Independent projections (movement, night progression, `ForArrival`) create their own. Shared `MovementProjection` / `ObstacleReactionCalculator` constructors take the instance and read its dictionary and event list from it; `MovementProjection` rejects an instance whose preparation index differs from its state.
- **Apply writes results itself.** The holder and any effect target (e.g. Lantern's attacker) are written into the shared dictionary; callers read `CombatDamageOutcome.After` and do not merge changes.
- **Recovery** actually applied is recorded per target. `CommandAttackLedger.CaptureHitHealth` freezes `CaptureRecovery()` for `AttackHitResult.HealthRecovery`; it no longer re-derives recovery from events. Known latent gap (record only, unreachable today): a healing object passed on a non-final knockback step would not be recorded; see follow-up design §5.

`ItemCharacterChanges.ApplyDecision` owns slot consumption and temporary-effect merging. `ItemCharacterChanges.AppendDecisionEvent` owns conversion to item events. Activation consumes the equipped slot once; it does not spend another inventory copy. Non-stacking is keyed by the same item effect: a duplicate consumes with zero applied magnitude and retains the original marker. Different item effects can coexist.

`CombatItemEvent` supplies item/holder/optional target, stage, reason, applied magnitude, old/new equipment and the `CombatDamageKind` for damage stages. `Activated` includes duplicate-effect consumption; lifecycle discards are not activations. Applied stat magnitude is the actual clamped difference, so a floor or duplicate can legitimately report zero. Damage and recovery are separate `CombatCharacterEvent` entries at their causal positions. `AttackHitResult.HealthRecovery` preserves actual damage accounting when recovery makes final HP greater than pre-hit HP.

Rope records only ordinary direct-hit anchors, takes the attacker's pre-consequence anchor, then selects among final survivors after forced arrivals and non-item attack consequences. Extra-attack victims are excluded. Ties use stable identity ordering and the dedicated deterministic item RNG stream; one candidate does not advance it. If everyone directly hit has died, the slot remains. A duplicate selected target consumes without searching for a different target.

Effective priority/defense apply item penalties last. Priority changes refresh the existing scheduler and timeline immediately while preserving active/opening-turn policies. No defense-to-damage formula was supplied or added. Source-specific scheduling and classification remain in the real damage callers recorded in plan section 10.4.

Calculation and preview operate on immutable input/projections. The publication transaction validates stock even when a slot activation produces no stock delta, and validates character item facts, command state/RNG and result identity before any install. It publishes calculated changes without reevaluating rules. Repeated publication cannot consume or emit twice. `WithCompletion` retains inventory changes. Public preview retains existing hidden-information filtering and excludes execution-only extra attacks; no hidden equipment field was added to its payload.

## 4. Lifetime ownership

| Boundary | Result |
| --- | --- |
| Outgoing combat round | Final scheduled environment damage resolves first; clear player/enemy slots and item effects before preparation publication |
| Death, including forced fall/removal | Clear that character's item state; do not activate Whiskey on removal; retain effects already placed on surviving targets |
| Victory / defeat | Command completion clears remaining battle item state before final publication; persistent main HP includes completed recovery |
| Abandonment | Existing completion service merges cleanup atomically; duplicate completion remains guarded |
| Rule owner for death / battle end | `CombatTemporaryStateCleanup.Clear` (Core) is the only definition of what is cleared: debuffs, item slot, item effects; it returns the same instance when nothing changes. `CombatCommandCalculator.Finish`, `BattleCompletionCalculator` (main and settled characters) and `BattleCompletionService` (other participants read from the Buffer) call it; the service decides only whom to clear |
| EndBattle / next stage in the same Run | Battle records disappear; unequipped shared stock remains |
| Successful new Run | Existing prepared new-Run transaction clears shared stock; failed preparation preserves current state |
| Enemy recall / next round | No automatic replacement of unused or spent starting equipment |

No cleanup path refunds an unused item merely because the battle round ended. Preparation unequip/replace/recall are the explicit refund paths. Cleanup is idempotent and avoids duplicate discard events.

## 5. Extension and maintenance locality

For another item using an existing behavior, create another `ConsumableItemDataSO` with a unique key and the existing typed payload, then add it to the one manual table. Validate its numbers and asset references. No item-key branch, dispatcher, source-specific damage adapter or extra runtime service is required.

For a genuinely different behavior within an existing stage, add its immutable spec, typed authoring payload, one `EffectChoice.Of<T>("Label")` row in `Systems/DataBaseSystem/Editor/ConsumableItemDataSOEditor.cs`, stateless rule implementing the appropriate capability, and one registry entry. The SO needs no edit. A new damage source calls `Damage.Apply` with its kind; omitting the kind does not compile. Verify the stage contract, application output and representative integration path. A new stage/capability requires a design revision; do not add a universal hook interface or parallel lifetime owner speculatively.

| Probe | Modification / understanding scope | Evidence and required verification |
| --- | --- | --- |
| LC1: Whiskey recovery 10% -> 20% | Existing SO payload and Whiskey rule semantics; shared recovery, consumption and event contracts remain dependencies. Rope, Lantern, Roket and source-specific damage implementations need no edits. | **Executed:** fixture SO clones through the real adapter, calculation and Buffer publication: HP 60/100 - 15 -> 45 -> 55 or 65, one consumption, separate recovery 10 or 20. Production SO remains 10%. Numeric change still needs its rule and representative application tests. |
| LC2: Rope selection behavior | Rope rule, captured direct-hit candidate/anchor contract and dedicated RNG; scheduler/effect publication are consumed contracts. The other three item rules and their formulas need no edits. | **Executed in D1/D2:** shuffled ties, dead-nearest fallback, all-dead preservation, duplicate no-retarget, source anchors and actual-next-actor/timeline. **Inspected in F:** registration/application retain this separation. A new selection policy still needs candidate/RNG tests and affected attack integration. |
| LC3: shared recovery/consumption/event fix | `CombatDamageApplication.Apply`, `ItemCharacterChanges.ApplyDecision`, `CharacterSettlementCalculator.Recover`, `ItemCharacterChanges.AppendDecisionEvent`; change the relevant shared owner. Item-stage order lives only in `CombatDamageApplication` and `Application/Combat/Items/`, not in `CombatCommandCalculator`. Source adapters retain cause, installation and original timing only. | **Executed in C2/C3:** real ordinary/reaction/extra/reflected/environment callers, multi-hit current-slot state and causal events. **Inspected in F:** all callers still use those owners; plan section 10.4 is the source-to-application ledger. A shared-policy change still requires relevant cross-source regression; centralization does not remove timing contracts. |

The first integration necessarily touched every actual damage writer and character copy/publication boundary. That initial diff is not the expected maintenance scope of a balance edit. SSOT remains one catalog, one stock authority and one item state per character. SRP separates authoring/rules/application/publication; explicit registration supports OCP; capability timing/purity contracts support LSP; the three narrow interfaces support ISP; Core consumes immutable contracts rather than SOs or concrete stores (DIP). Existing Framework service registration, lifecycle, input and transaction contracts remain the composition authority. No allocation, frame-time or device-scale performance improvement is claimed.

## 6. Executed acceptance and remaining checks

Final evidence: `C:/Temp/ProjectL-CombatItems/F-final-all-20261017-052456-776.xml` and `.log`, **7,699/7,801 passed, 102 failed, 0 skipped**, Unity exit 2. Core passes **3,084/3,084**, Framework **4,586/4,688**, CombatViewBoundary **9/9**, and Tool.Editor **20/20**. Included subsets: all item fixtures **194/194**, new F fixtures **19/19**, and `DataBaseAssetTests` **31/34**. The 102 failing identities all appeared in the earlier Phase B run; 101 failure messages match, and the remaining boot failure retains the same missing `Unit_CactusCannon` cause with shifted preparation stack lines. There is no pre-feature full Framework baseline, so this is not proof that every failure predates item implementation. No new failing identity was found. Detailed provenance is in plan section 10.8.

These artifacts are local evidence, not committed repository fixtures. The four-assembly final run uses one graphics-enabled Unity process, so it is not four independently isolated Editor executions. Static metadata/Unicode/LF/pure-dependency checks and `git diff --check` passed; the generated Unity process exited.

The test-owned end-to-end acceptance uses real runtime composition and command publication: create Run -> prepare -> grant three Whiskeys -> equip (stock two) -> move/attack -> return to preparation and discard unused equipment (stock two) -> next combat and victory -> EndBattle -> prepare next stage (stock two) -> real `StartNewRun(1)` (stock empty). It supplies valid Main range-one/cardinal movement fixtures; those fixture corrections did not change gameplay contracts.

Other executed causal examples belong to their named fixtures, rather than being falsely attributed to that Whiskey-only flow: Roket direct-damage applied reduction 15 (`CombatItemDirectDamageTests`); Rope priority 20 -> 10, interval 5 -> 10 and remaining wait 3 -> 6 (`CombatItemTimelineTests`); Lantern defense 8 -> 3 or 3 -> 0 and duplicate applied magnitude zero (`CombatItemLanternTests`). F verifies actual Inspector selector callbacks for all four typed payloads in a graphics-enabled Editor. This is automated Editor interaction, not a manual visual/play-session signoff.

Remaining external work: actual UI/input affordances and item presentation, acquisition transactions and reward probabilities, explicit production enemy assignments or starting grants if approved, save persistence if required, and the user's later Astra review. The 102 earlier-observed broad-suite failures remain explicit exceptions rather than passing acceptance. No production gameplay Play-mode session or device performance benchmark was performed.
