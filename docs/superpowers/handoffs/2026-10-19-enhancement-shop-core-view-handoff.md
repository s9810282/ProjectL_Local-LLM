# Enhancement Shop — Core, View, and Content Handoff

Date: 2026-10-19. Scope: approved Phases A–D and review-correction Phase R in the current worktree.

Core implementation, fixture integration, and the three reviewed corrections are delivered. See section 8 for fresh correction evidence. Production recipes, stage activation, and enhancement presentation are **not operationally complete**. This document identifies the existing connections as well as missing deliveries. No production node IDs, missing unit statistics, scenes, prefabs, or VFX were invented. No commit, push, or external message was sent.

## 1. Current behavior

Each owned character instance may acquire one enhancement, at enhancement level 1, independently of its ordinary Level/Experience. An enhancement visit freezes up to three distinct owned-instance/branch offers. Fewer candidates produce fewer cards; zero candidates still creates an enhancement visit that can be completed without purchasing. All eight intended rows cost 700 Run gold and have weight 1. A purchase grants a selection, not another character. Selection survives stage changes and resets when a new Run is successfully installed. Failed new-Run preparation preserves the prior roster and selection.

Akimbo's Nansa consists only of `KillChainAttack`. `MoveAttackDiscount` remains a reusable implementation and visual asset, but is excluded from Akimbo's developer definition. The player two-trait cap was removed in Phase C; typed trait validation and registration remain. Change Rider changes only effective base Move/Attack costs to 2/4, preserving category, growth, and Nansa. Moving does not discount its next attack.

Poison Thorn replaces the selected instance's ThornField with damage 7, lifetime 5, defense reduction 30, and `UnitTurnTickPhase.Started`. Ordinary Cactus Cannon remains damage 5/lifetime 5, no defense reduction, and end-ticking. Defense is `floor(defenseAfterFlatItemPenalties * (100 - percent) / 100)`. Re-exposure replaces/refreshes one status, without stacking. Application during the victim's active turn survives its current end and expires at its next end; otherwise it expires at its next turn end. Only the victim's actual ends count. Poison fields separately expire at the fifth subsequent actual unit-turn start across actors. Night cleanup removes fields; existing death and battle-disposal cleanup remove temporary statuses. Night participant reconstruction does not extend the victim's remaining own-turn count.

Voluntary movement, displacement, and immediate attack-created thorn contact use the same status semantics. Contact consumes an existing field. Status applies only to surviving characters, after contact damage. Existing overlap refresh preserves the first field's payload/clock and takes the maximum remaining lifetime, matching the pre-existing overlap policy. Preview and rejected publication do not consume duration.

## 2. Exact source roots and contract boundaries

Prefixes below are repository-relative; concatenate prefix and suffix for the actual file. These are source locations, not additional services.

| Prefix | Directory |
|---|---|
| S | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem` |
| C | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts` |
| A | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application` |
| D | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Domain` |
| B | `Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems` |
| DB | `Assets/ProjectL/Script/Systems/DataBaseSystem` |
| F | `Assets/ProjectL/Script/Systems/Factory` |
| V | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem` |

### P1: identity and authoring

Public immutable declarations:

- `C/Enhancements/OwnedEnhancement.cs`: `OwnedEnhancement(EnhancementKey key, int level)`, properties `Key`, `Level`, and `bool SameValues(OwnedEnhancement other)`.
- `C/Roster/OwnedCharacterRecord.cs`: existing constructor appends `OwnedEnhancement enhancement = null`; get-only `Enhancement`. Ordinary XP changes preserve the selection. `C/Enhancements/EnhancementChange.cs` represents acquisition with `Expected` and `Final` owned records.
- `C/Enhancements/EnhancementDefinition.cs`: `EnhancementDefinition(EnhancementKey key, EntityKey characterKey, int level, int cost, int weight, string name, string description, IReadOnlyList<TraitSpec> addedTraits, IReadOnlyList<TraitSpec> replacedTraits, int? moveCost = null, int? attackCost = null)`. Current executable definitions require level 1; no level-up behavior is implied by the metadata.
- `DB/Data/InGame/Table/Enhancement/Item/EnhancementDataSO.cs`: `public override IData GetData()` creates fresh pure `EnhancementData`. Typed added/replaced trait authoring and optional costs are converted by internal `S/CharacterSystem/TraitSpecAdapter.Read(UnitTraitData) : TraitSpec` and `EnhancementDefinitionAdapter.Build(IReadOnlyList<EnhancementData>) : IReadOnlyList<EnhancementDefinition>`.
- `DB/Data/InGame/Table/Stage/Enhancement/EnhancementStageDetail.cs`: `EnhancementStageDetail(IReadOnlyList<EnhancementKey> candidateKeys)`, `CandidateKeys`, `StageType`, `void Validate()`. Empty candidate lists are supported; duplicate keys are invalid. `EnhancementStageData` is the SO input adapter. Root type is Shop and map role is Regular.

`DB/Queries/InGameDataBase/InGameTable/EnhancementTable.cs` contains the internal `IEnhancementTableQuery : IServiceModule`, with `EnhancementData Get(EnhancementKey)`, `bool TryGet(EnhancementKey, out EnhancementData)`, and `IReadOnlyList<EnhancementData> GetAll()`. Internal `IHasEnhancementTable` on the local InGame database exposes that query. View must not query these interfaces.

### P2: public service/input/result contracts

`S/ShopSystem/ShopService.cs` retains `public interface IShopService : IServiceModule`:

```csharp
ChapterProgressionResult EnterShop(int chapterKey, StageNodeId targetNodeId, int seed);
ShopPurchaseResult Purchase(ShopVisitContext context, ShopOfferId offerId);
ChapterProgressionResult CompleteShop(ShopVisitContext context);
```

`S/StageCommandSystem/StageCommandProcessingService.cs` retains the numbered public processing boundary:

```csharp
bool ProcessNextNodeCommand(int expectedRequestNumber, Func<int> nextSeed);
bool ProcessNextShopCommand(int expectedRequestNumber);
```

`B/ShopView/ShopViewCommand.cs` exposes `ShopViewCommandData.Purchase(ShopVisitContext, ShopOfferId)` and `.Complete(ShopVisitContext)`, both returning `ShopViewCommandData`. `B/ShopView/ShopViewBuffer.cs` exposes `IShopViewBufferWriteSink.Submit(ShopViewCommandData) : int`, returning the request number.

`C/Shop/ShopVisitContext.cs` has `ShopVisitContext(int chapterKey, StageNodeId nodeId, StageKey stageKey, StageEntryId entryId)`; all four values identify the captured visit. `B/Current/CurrentShopData.cs` publishes immutable `Offers`, `EntryId`, `VisitKind`, `PurchasedMask`, and `HasPurchase`. `VisitKind == ShopVisitKind.Enhancement` works even when Offers is empty.

`C/Snapshots/ShopPurchaseSnapshot.cs` exposes:

```csharp
public static ShopPurchaseSnapshot ForEnhancement(
    ShopOfferId id, CharacterInstanceId targetId, EntityKey characterKey,
    OwnedEnhancement enhancement, int cost, string name, string description);
```

Read `Kind`, `OfferId`, `CharacterKey`, `TargetOwnedId`, `Enhancement`, `EnhancementName`, `EnhancementDescription`, and `Cost`. `TargetOwnedId` is a non-nullable value type; ordinary products use its empty value. `InitialLevel` remains recruitment data and is not enhancement level.

`B/Current/IStageCommandResultReadSink.cs` provides `int HandledShopCommandNumber` and `bool TryGetShopResult(out ShopCommandResult result)`. Here `ShopCommandResult` means **`BufferSystems.Current.ShopCommandResult`**, whose `RequestNumber`, `Context`, `Kind`, `OfferId`, `PurchaseResult`, and `CompletionResult` are public read-only facts. `Accepted` on a Purchase result means purchase success, not successful stage completion.

Public pure calculation entry points are actually located in **`A/Shop/`**, correcting the plan's original `A/Enhancements/` location for these two files:

```csharp
IReadOnlyList<ShopPurchaseSnapshot> EnhancementOfferPlanner.Plan(
    IReadOnlyList<EnhancementDefinition> definitions,
    IReadOnlyList<OwnedCharacterRecord> owned, Random random);
ShopPurchaseDecision EnhancementPurchaseRules.Plan(
    ShopPurchaseSnapshot offer, bool hasPurchase, int balance,
    IReadOnlyList<OwnedCharacterRecord> owned,
    MainCharacterPersistentState main, EnhancementDefinition definition);
```

These are Core calculations, not View eligibility APIs. `ICharacterBufferStore` preparation/install methods (declared in `B/Character/CharacterBuffer.cs`) and `B/Character/PreparedEnhancementChange.cs` are internal. `ShopTransactions` prepares roster, gold, visit lock, and result before installing. The public character write sink was not widened into a general roster mutation API.

### P3: per-instance combat resolution

`A/Enhancements/OwnedUnitSpecResolver.cs` is public static:

```csharp
UnitSpec Resolve(UnitSpec basis, OwnedCharacterRecord owned,
    IReadOnlyList<EnhancementDefinition> enhancements);
IReadOnlyList<CombatAttackProfile> BuildProfiles(
    IReadOnlyList<CombatAttackProfile> baseProfiles,
    IReadOnlyList<OwnedCharacterRecord> owned,
    IReadOnlyList<EnhancementDefinition> enhancements);
```

`C/Combat/CombatAttackProfile.cs` publishes nullable `OwnedId`; `(Definition.Key, OwnedId)` is identity. Null identifies an unowned base profile. `BattleInitialization.Enhancements` carries copied definitions. Preparation, owned-stat queries, input assembly, preview, and AI share resolution; selected instances cannot silently fall back to base profiles. Shared definitions remain unchanged. Movement context includes owned-profile identity and all new thorn/status payload values. Initial hand creation is covered; this work does not introduce a later/manual draw command.

### P4: damage and causal follow-ups

Public specs are in `C/Commands/Traits/`: `DoorBreachingTraitSpec(int radiusCells)`, `ContemptForWeakTraitSpec(int numerator, int denominator)`, `PrecisionShotTraitSpec()`, `DoubleShotTraitSpec(int radiusCells)`, `LuckyBulletTraitSpec()`, and `ChainExplosionTraitSpec(int attackPercent)`. Matching definitions/statuses/authoring and registry mappings exist. New trait keys are 130–180; old values were not renumbered.

`D/CombatEffects/Rules/CombatEffectCapabilities.cs` holds internal target-damage, attack-resolved, kill-chain-activated, and displacement-resolved capabilities. Immutable contexts/contributions and `D/CombatEffects/EffectCausalChain.cs` carry command-local facts. `A/Combat/FollowUpAttackExecutor.cs` is internal and applies requests to the same projection, authoritative RNG, damage application, and final transaction; it does not submit another View command. `FollowUpAttack` is appended to activation reporting. View renders published results and must not rerun target selection, random draws, damage formulas, or duration rules.

### P5: thorn/status transport and lifetime

- Public `C/Combat/DefenseReductionDebuffState.cs`: `DefenseReductionDebuffState(int reductionPercent, int remainingOwnTurnEnds)`, properties `ReductionPercent`, `RemainingOwnTurnEnds`, `bool SameValues(DefenseReductionDebuffState)`. Values are 1–100 and 1 or 2 respectively. `CharacterDebuffState` appends optional `defenseReduction = null` and publishes `DefenseReduction`.
- Public `C/Common/UnitTurnTickPhase.cs`: **`Ended = 0`, `Started = 1`**, preserving ordinary behavior for old serialized/default values. These explicit values clarify the plan's unordered enumeration.
- Public `C/Commands/Traits/ThornFieldTraitSpec.cs`: `ThornFieldTraitSpec(int thornDamage, int lifetimeUnitTurns, int defenseReductionPercent = 0, UnitTurnTickPhase tickPhase = UnitTurnTickPhase.Ended)`. Equivalent values cross authoring, pure data, definitions, status snapshots, and engine contributions.
- Public `C/Snapshots/ThornFieldCellSnapshot.cs`: `ThornFieldCellSnapshot(CellCoord cell, int remainingUnitTurns, int thornDamage, int defenseReductionPercent = 0, UnitTurnTickPhase tickPhase = UnitTurnTickPhase.Ended)`.
- Public `C/Combat/MovementDebuffChange.cs`: `MovementDebuffChange(EntityId targetId, CharacterDebuffState expected, CharacterDebuffState final)`, with matching get-only properties. `MovementStepEffects` appends optional copied `IReadOnlyList<MovementDebuffChange> debuffChanges = null`. `MovementStepInput` transports `Debuffs` and `OwnsActiveTurn`; its full constructor appends `EntityId activeActorId = default`.
- Internal `A/Combat/CombatStatusLifecycle.OnTurnEnded(CharacterCombatFacts actor) : CharacterDebuffState` dispatches stable registered `D/CombatEffects/Rules/ICharacterStatusLifecycleRule.OnTurnEnded(CharacterDebuffState) : CharacterDebuffState`. `DefenseReductionStatusRule` alone owns victim-status expiry.
- Internal `A/Combat/ThornFieldLifetimeCalculator.Advance(IReadOnlyList<ThornFieldCellSnapshot>, UnitTurnTickPhase) : IReadOnlyList<ThornFieldCellSnapshot>` owns field clock evaluation. Existing trait callbacks remain in CombatEffectEngine; no unused callbacks or global bus were added.

View reads `B/Character/CharacterData.Debuffs.DefenseReduction` and **`CharacterData.EffectivePhysicalDefense`**. The latter already uses Core's effective-stat calculation; D required no change to CharacterData. Fields are read from `B/Combat/CombatStateData.CommandState.ThornFields`, including remaining turns, damage, percentage, and tick phase. A tooltip/countdown must use these facts, not maintain its own timer.

## 3. View/Flow integration sequence

1. Existing stage-selection owner submits a numbered node intent. Its established Flow/Job calls `ProcessNextNodeCommand(expectedRequestNumber, nextSeed)`. The authored Shop node routes through `EnterShop`; map entry origin and lifetime remain with the existing owner.
2. Read `ICurrentProgressBufferReadSink.CurrentMapKey`, `CurrentStage`, and `Shop`. Capture `ShopVisitContext` with chapter, node, stage, and `shop.EntryId`. Identify enhancement presentation from `shop.VisitKind`, not from the first offer. Read Run gold from `ICurrencyBufferReadSink.Amount`, not combat StageGold. Read owned identity/selection from `ICharacterBufferReadSink`.
3. Render each frozen offer using its ID, target owned ID/type, enhancement key/level, name, description, and cost. Show the actual number of offers, including zero. Match duplicate character types by owned ID. Do not regenerate candidates on reopen or calculate eligibility in View.
4. Purchase submits `ShopViewCommandData.Purchase(context, offer.OfferId)`; skip submits `.Complete(context)`. Save the request number returned by `Submit`. Gate duplicate input using existing command ownership. View writes only these intents.
5. Existing ShopFlow/Plan/Job calls `ProcessNextShopCommand(requestNumber)`. Read the handled number and `TryGetShopResult`; match both request number and captured visit identity. A stale result must not close a newer visit.
6. Check `PurchaseResult.Succeeded` and `CompletionResult.Accepted` separately. A committed purchase followed by failed completion keeps the purchase lock and selection. Disable further purchases and submit **Complete for the same visit** to retry completion; never repeat Purchase to retry exit.
7. On accepted completion, the existing owner closes the popup and refreshes/continues progression. Empty visits support the same Complete path. Render failures from the published reason, without local gold or roster writes.
8. During battle, use CharacterData/effective facts and published CombatResult/events for visuals. Current `V/StageMap/Shop/ShopPopupModelBuilder.cs` distinguishes consumables from unit products and has no enhancement-specific presentation. Implement that missing branch before enabling production enhancement nodes; ordinary unit-card fallback is not acceptance.

No new Flow/Plan/Job, public enhancement service, or global service lookup was added. Use existing runtime Core group exports, Current/Character/Currency read sinks, ShopView write sink, and stage-result read sink. Do not create a second service or independently publish purchase/turn outcomes.

## 4. Complete content and View delivery inventory

Asset prefixes: `IG = Assets/ProjectL/DataBaseSO/InGameGroup`, `VG = Assets/ProjectL/DataBaseSO/ViewGroup`. Presence and table references below were inspected in the current checkout, not inferred from enum names.

| Dependency | Current state | Required delivery/acceptance |
|---|---|---|
| DB catalog and InGame group | Existing `IG/InGameGroup.asset` references Unit, Targeting, Category, Stage, StageMap, Loadout/Roster, Economy and other existing tables | Retain these connections; register the enhancement table in this same group and the existing catalog path |
| Shotgun (11) | `IG/Unit/Item/Unit_ShotGun.asset` is in UnitDataTable; original DistanceDamageScale is 150/50 | Preserve stats, category, targeting, and original trait for both branches |
| Ballista (15) | Unit asset/table entry exists; original PiercingFalloff is 30% with its authored target limit | Preserve ordinary row; selected Precision replaces only falloff to 20% and adds its rule |
| Cactus Cannon (16) | Unit asset/table entry exists; ThornField damage 5/lifetime 5 | Preserve ordinary row and end clock; Poison is per-instance replacement |
| Dynamite Hammer (24) | Unit asset exists with Knockback 1, but **is not in current UnitDataTable** | Register approved unit content, acquisition route, and appearance before operational Chain Explosion acceptance |
| Akimbo (10) | No Unit asset with key 10 in current Unit item folder; developer fixture corrected to KillChainAttack only | Supply approved production stats/category/targeting and Nansa composition, register in table and acquisition route; no MoveAttackDiscount in Akimbo |
| Shielder (23) | No Unit asset with key 23 in current Unit item folder | Supply approved production definition and Intimidation parameters; register table and acquisition route |
| Targeting | Existing TargetingMethodDataTable is connected; current Shotgun, Ballista/Dynamite, and Cactus references resolve to existing methods | Preserve range/selection/delivery relationships; supply approved Akimbo/Shielder references, do not infer them from fixture geometry |
| Category/growth | Existing CategoryConfig_Default and CategoryConfigDataTable are connected | Preserve every base category and growth/matchup rule; Change Rider supplies costs only |
| Loadout/roster/acquisition | Existing Loadout and UnitRoster tables are connected. PlayerLoadout_1 currently references Main, Cactus Cannon, Sniper, Shotgun | Ensure each target can actually become owned through approved production content; this implementation does not change starting roster or add acquisition restrictions |
| Base traits | KillChainAttack, DistanceDamageScale, Intimidation, Knockback, ThornField, PiercingFalloff implementations exist | Keep all originals except the explicit Poison/Precision replacement and user-approved Akimbo correction |
| Typed enhancement authoring | All eight recipes can be represented by existing/new typed authoring and converted to pure Core definitions | Author eight actual rows with the table below; no production row assets currently exist |
| Enhancement table | SO/query/factory support exists; no EnhancementDataTableSO asset registered in InGameGroup | Create table asset, populate all rows, register once; missing required table is an explicit failure |
| Stage detail/root/map | EnhancementStageData/Detail implemented. Existing Stage_Shop points to ordinary Shop_Default | Supply enhancement detail, Shop/Regular root, StageRootDataTable entry, and StageMap node/link with approved keys; ordinary shop is not silently repurposed |
| Unit visuals/portraits | VG UnitVisual table contains keys 11, 15, 16 with prefab/icon/presentation references | Retain and manually verify those references. Keys 10, 23, 24 have no current UnitVisual item; provide prefab, display name, timeline/info icons and attack/reaction presentation and register them |
| Existing trait visuals | TraitVisual table contains 10,20,30,40,50,60,70,80,90; relevant PiercingFalloff, ThornField, Knockback, Shotgun and KillChainField assets are connected | Retain existing keys. MoveAttackDiscount visual remains reusable but is not an Akimbo requirement |
| Missing trait/branch visuals | No current TraitVisual rows for Intimidation 110 or added enhancement traits 130–180 | Supply/register required icons/text/presentation. Poison shares ThornField key 40 and Change Rider adds no trait key; distinguish branch description/selection by EnhancementKey, not by inventing a trait key |
| Enhancement cards | Ordinary ShopPopupModelBuilder exists; enhancement product binding is absent | Bind target owned ID, branch name/description/icon, enhancement level 1, price, selection/purchase lock, and variable offer count |
| Gold/status/skip/result bindings | Public facts and numbered command route exist | Bind Run gold, insufficient-funds/stale errors, empty visit skip, request/visit matching, completion-only retry, and accepted exit |
| Combat status and VFX | Public defense/status/field facts and extra-attack activation results exist | Bind effective defense and expiry text; author follow-up/knockback/poison visuals as required. Use result playback; do not recalculate rules |
| Services/Buffer registration | Existing Core/Buffer factories and group exports are reused; optional enhancement query added locally by InGameDataBaseFactory | Keep existing Current, Character, Currency, ShopView and result services. No new singleton/service registration is required for D |
| Manual acceptance | EditMode fixtures exercise Core/service/Buffer behavior only | After content/View wiring, play entry, short/empty pools, purchase, skip, completion failure/retry, next battle, each branch, normal same-type units, and new Run reset |

Required recipe rows (all level 1 / cost 700 / weight 1; descriptions/icons supplied by content):

| Name | EnhancementKey / target | Added or replaced typed recipe |
|---|---|---|
| 럭키 불릿 | LuckyBullet / Akimbo 10 | Add LuckyBullet; retain KillChainAttack |
| 체인지 라이더 | ChangeRider / Akimbo 10 | Override MoveCost 2, AttackCost 4; no category or trait replacement |
| 2연발 | DoubleShot / ShotGun 11 | Add DoubleShot radius 1; retain DistanceDamageScale |
| 도어 브리칭 | DoorBreaching / ShotGun 11 | Add DoorBreaching radius 1; retain DistanceDamageScale |
| 약자 멸시 | ContemptForWeak / Shielder 23 | Add ContemptForWeak numerator 2 / denominator 1; retain Intimidation |
| 연쇄 폭발 | ChainExplosion / DynamiteHammer 24 | Add ChainExplosion attackPercent 50; retain Knockback |
| 독 가시 | PoisonThorn / CactusCannon 16 | Replace ThornField: damage 7, lifetime 5, defenseReductionPercent 30, Started |
| 정밀 사격 | PrecisionShot / Ballista 15 | Replace PiercingFalloff with 20% and original target limit; add PrecisionShot |

The D3 tests deliberately supply complete fixture definitions, geometry, categories, stage keys and explicit excluded kill rewards. Those values are not approved production replacements. Eight parameterized cases execute real shop purchase/completion, per-instance battle preparation, actual turn start, command processing, and Buffer readback. They also keep another ordinary owned instance unchanged. Separate tests cover successful/failed new-Run preparation. Existing C tests cover nonzero kill gold/XP and once-only reward settlement.

## 5. Placement and independence audit

| Responsibility | Actual owner and shared edge |
|---|---|
| Eligibility, sampling, purchase validation | `A/Shop/EnhancementOfferPlanner`, `EnhancementPurchaseRules`; ShopService routes product/detail kinds only |
| Authoritative acquisition | `B/Core/ShopTransactions`, internal Character prepare/install; one publication of selection/gold/lock/result |
| Per-owned definition | `A/Enhancements/OwnedUnitSpecResolver`; preparation/query/input/profile consumers route identity only |
| Breaching / Contempt / Precision | Individual rules under `D/Traits/Effects`; CombatDamageCalculator consumes generic contributions; direct delivery supplies distance/recipient ordinal |
| Double Shot / Lucky Bullet / Chain Explosion | Individual rules under `D/Traits/Effects`; attack/kill-chain/displacement callers report typed outcomes, shared executor applies deliveries |
| Thorn status application | ThornMovementTileRule and CombatDebuffCalculator; MovementProjection applies expected/final proposals after damage; attack-knockback reuses consumed-field status evaluation after its existing aggregated damage |
| Effective defense | CombatEffectiveStatsCalculator -> CombatDebuffCalculator; item flat penalties then percentage. Existing CharacterData publishes the result |
| Victim lifetime | DefenseReductionStatusRule through CombatStatusLifecycle; coordinator dispatches actual actor end only |
| Field lifetime | ThornFieldLifetimeCalculator uses authored phase; CombatTurnCalculator dispatches actual start/end and preserves night cleanup |
| Snapshot/validation | CharacterDebuffState equality, trait/cell snapshots, movement-context equality, CombatStateBuffer validation; Intimidation/Frenzy copy/clear preserve the new independent payload |
| Composition | TraitSpecAdapter, TraitFactories, TraitEffectProvider and existing registries; InGameDataBaseFactory optionally registers EnhancementTable. D adds no global factory/module |

Changing thorn percentage is an authoring edit; changing explosion multiplier is its typed recipe/rule edit; changing Lucky target policy is a LuckyBulletEffectRule edit. None requires a branch-key switch in shop, damage, turn, movement, Flow, or Job coordinators. Full shared Core regression is justified by formula/transport changes, while individual tuning remains local.

SSOT: Buffer/projection owns facts, rules return immutable decisions, and View consumes facts. SRP separates rules/application/composition; OCP uses narrow demonstrated capabilities; ISP avoids unused lifecycle hooks; LSP preserves ordinary defaults and typed validation; DIP keeps Core free of Unity/View/Framework internals. Existing FW service registration, builder ownership, lifetime, and transaction contracts remain. Review was an author self-review under the plan's explicit no-subagent constraint.

## 6. Verification record

The companion plan's Phase D exit record is the exact final test ledger. Local XML/log evidence is under `.superpowers/sdd/2026-10-19-enhancement-shop-trait-plan/`. Core plus focused integration run `D-core-and-integration`: **3480/3480 passed**, zero failures/skips, Unity exit 0. This includes the complete Core assembly and targeted Framework authoring, publication, purchase and eight-branch integration fixtures.

Fresh whole-Framework baseline `D-pre-framework-graphics`: **5337 passed / 5577 total, 240 failed** before D production edits. Final `D-framework-final-graphics`: **5352/5591 passed, 239 failed**, exit 2. Exact comparison: **0 introduced, 1 previously failing case passed, 239 remaining, 0 changed failure messages**. All 19 historical pre-A affected failures also retain identical messages. The one newly passing railway presenter case was not edited by D and is not claimed as a Phase D repair. The pre-A historical baseline covers an affected subset (19 failures); a pre-A whole-Framework result does not exist because that attempt crashed. Do not relabel the pre-D baseline as pre-A or infer repository-wide green from focused tests.

Unity runs use the approved AppData-access path. The initial pre-D `-nographics` run crashed in native Unity/graphics code without XML; the graphics-enabled retry supplied the usable baseline. No cache deletion or Unity upgrade was performed. No production asset was edited, and no manual View/content acceptance is claimed.

## 7. Remaining work by readiness — 2026-10-19 follow-up

This section separates work that can start from confirmed requirements from work waiting for content decisions. It is a delivery breakdown, not authorization to activate a production stage. This follow-up changed documentation only.

### 7.1 Existing View plumbing to retain

The earlier inventory's gold/input/result entries are dependencies to retain and verify, not all missing implementations. Source inspection confirms:

- `V/StageMap/Shop/ShopPopupModelBuilder.cs` already reads Run gold, iterates the actual offer count, disables purchasing after `HasPurchase`, and sets the completion-retry button text. Its missing branch is enhancement-specific card mapping; currently every non-item offer falls through to unit recruitment presentation.
- `V/StageMap/Shop/ShopPopupController.cs` already matches request numbers and full visit context, distinguishes purchase from completion success, keeps a failed completion open, and refreshes the map on accepted completion.
- `Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/ShopUI.cs` already submits Purchase/Complete intents with displayed OfferIds and gates pending input. `ShopPopupModel` already carries the visit and variable card list.
- `V/StageMap/Shop/ShopResultPolicy.cs` and `ShopPopupText.cs` have ordinary-shop failure handling, but no explicit enhancement failure entries. Extend/review these for `TargetNotOwned`, `AlreadyEnhanced`, `InvalidEnhancement`, and `UnsupportedEnhancementLevel`, preserving the existing stale-context versus data-error policy.

### 7.2 Work that can start without additional gameplay data

| Work | Concrete scope | Completion boundary |
|---|---|---|
| Five recipe SOs with existing target assets | DoubleShot, DoorBreaching, ChainExplosion, PoisonThorn, PrecisionShot; use section 4's confirmed parameters, Korean names, level 1, cost 700, weight 1 | Description text can be drafted from the approved rules. Final editorial/art review is separate. This does not enable a production node |
| Enhancement table asset and DB group connection | Create/register the existing EnhancementDataTableSO type and populate available valid rows | Three remaining rows depend on missing Akimbo/Shielder UnitDataSO references; do not insert null references or fixture stand-ins |
| Dynamite Hammer UnitDataTable registration | Reuse the existing key-24 SO and its authored values | No new balance value is required for the table link. Acquisition and complete UnitVisual readiness remain separate prerequisites for operational use |
| Enhancement card model/text | Map branch name/description/level, target owned instance, price and selected/purchased state; use VisitKind for empty enhancement visits | Existing card layout permits functional binding. Final art/any redesigned layout remains separate. Do not display enhancement as unit recruitment |
| Input/result regression and error presentation | Reuse the existing request/visit matching, purchase lock, Complete-only retry, skip and map refresh; cover enhancement failures and zero/one/two/three offers | No replacement Flow/Job or new service required. View-level fixtures can exercise this without an operational enhancement map |
| Combat read-model integration | Consume effective defense, defense-reduction payload, field lifetime/clock, and published follow-up results | Read/binding logic and tests can start; final placement, icons, and animations require presentation assets or a reuse choice |
| Content validation | Check table references, typed recipe conversion, original trait preservation, same-type owned-instance separation, and existing unit/category/targeting/visual dependencies | Run affected DataBaseAssetTests after asset edits and compare existing failures; manual production play follows complete wiring |

The three recipes that cannot yet become valid production SOs are **LuckyBullet and ChangeRider (Akimbo)** and **ContemptForWeak (Shielder)**. `EnhancementDataSO.character` requires a real UnitDataSO reference; enum keys and developer/test definitions do not satisfy it. The other five recipes can be authored before those units arrive.

### 7.3 Information or assets needed from the user/content owner

| Input | Minimum useful information | What it unblocks |
|---|---|---|
| Akimbo and Shielder production definitions | Approved existing SO/document location, or their missing base stats, category, footprint/behavior, targeting/range/wall policy, action costs and priority/delay; Shielder's Intimidation parameters and any category-specific protection data. Specify reward eligibility if relevant | Two unit SOs and table registrations, then the remaining three enhancement rows. Akimbo's KillChainAttack-only composition is already confirmed and need not be asked again |
| Enhancement shop placement | Target chapter/map and node or placement rule; whether to add an enhancement visit or replace an existing ordinary shop; candidate pool if it is narrower than the eight confirmed branches | Enhancement detail/root and actual map linkage. Existing ordinary Stage_Shop is not automatically replaced |
| Unit acquisition | How missing/unavailable targets become owned: starting loadout, ordinary shop/reward pool, or another existing route, and any required price/weight | Gameplay access to all eight branches. Table presence alone does not grant ownership |
| Unit appearance | Prefab, timeline/info icons, attack/reaction presentation for Akimbo, Shielder and Dynamite Hammer, or explicit approved reuse mappings | Missing UnitVisual keys 10, 23 and 24 and final card/combat appearance |
| Trait/enhancement presentation | Icons/text/animations for Intimidation and new traits, branch-specific presentation for Poison/Change Rider, or an approved reuse/minimal-presentation policy | TraitVisual entries, enhancement card art, status display and follow-up/poison VFX acceptance |

An authoritative content document or asset path is sufficient; the user does not need to retype every field. Read supplied material first and ask only for values genuinely missing from it. Confirmed enhancement prices, levels, weights, damage/percentage rules, and Akimbo's Nansa correction are not open questions. Korean descriptions can be drafted from those confirmed rules; they need not block code or non-activated data preparation.

### 7.4 Recommended delivery order

1. Prepare the five valid recipe rows, table/group registration, and existing Dynamite Hammer unit registration; keep production enhancement stage activation separate.
2. Implement enhancement-specific View card mapping and result/text coverage on the existing shop plumbing, with focused View fixtures. Prepare combat read bindings in parallel with content work where files do not overlap.
3. Obtain the missing unit definitions, appearance mappings, acquisition policy and map placement; finish the remaining three rows and production references.
4. Enable the approved enhancement stage route, then manually verify entry, short/empty pools, duplicate-type owned targets, purchase/skip/retry, all eight subsequent combat behaviors and new-Run reset. Automated Core success alone does not close this gate.

## 8. Review corrections and current readiness — 2026-10-19

The post-D review found duplicate temporary-attack contribution, missing Poison Thorn status on tile-driven forced movement, and duplicated ordinary/same-direction delivery sequencing. These are now corrected under spec section 10 and plan Phase R:

- Current-attack comparisons count the published temporary contribution once.
- Tile-driven forced contact applies and refreshes Poison Thorn using the victim's active-turn context, preserving unrelated debuffs and death cleanup.
- Internal AttackDeliveryExecutor shares execution ordering. Cost settlement, reaction/preview/reflection differences, shared command state, rewards, and final publication retain their existing boundaries. There is no new View command or binding contract.

Fresh verification: full Core **3461/3461 passed**, affected Framework **44/44 passed**, both Unity exit 0. The prior five failing regression cases now pass. Full Framework was not rerun in R; section 6's whole-assembly failures remain historical unresolved evidence. Final review was author self-review; no subagent was used.

The reviewed Core blockers are closed, so View may proceed with the contracts and delivery tasks above. Production content registration and manual View acceptance remain required; this is not a claim that the complete feature is playable in production.
