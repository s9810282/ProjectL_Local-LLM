# Category Movement Settings in the Existing SO

Date: 2026-10-25  
Status: Design approved and Tasks 1–3 subsequently authorized and completed on 2026-10-25. See the implementation plan for verification evidence and 89 remaining legacy Framework failures.  
Source inspected: `d4a6d6fd83cb79e862e582d183a77a93241efe50`.

## 1. Intent and selected approach

The user requested editable **movement action cost and category movement allowance**, using the existing `CategoryConfigDataSO`. This approved scope replaces the fixed movement values in the [player-turn design](2026-10-23-player-turn-shared-actioncost-design.md); its other gameplay rules remain in force. Approval of this design does not authorize implementation.

| Approach | Tradeoff | Decision |
|---|---|---|
| Extend `CategoryConfigDataSO` and its existing pure-data/category pipeline | Reuses registration and both battle/stat-query consumers; requires updating the current fixed-rule readers | Selected by the user |
| Add a separate movement SO/table | Separately groups movement settings but adds a lookup and registration without a current need | Not selected |
| Restore movement fields on individual Unit SOs | Allows per-unit overrides but duplicates category facts and changes the requested ownership | Not selected |

## 2. Current code and proposed flow

All source paths below are relative to `Assets/ProjectL/Script/Systems/`:

- `DataBaseSystem/Data/InGame/Table/Category/Item/CategoryConfigDataSO.cs` exports a fresh `CategoryConfigData`. It currently contains Heavy protection and growth/progression settings.
- `RuntimeSystem/InGameSystem/CoreSystem/CharacterSystem/UnitSpecAdapter.ReadCategories` supplies category specs to both `CombatSystem/BattleDefinitionAdapter` and `CharacterSystem/CharacterStatsQueryService`.
- Under `RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/`, `Domain/Entities/UnitDefinition` currently reads movement values from `Domain/Categories/CategoryActionRules`. `Application/Combat/CombatActionAdmission` and `Domain/Movement/MovementRouteValidator` independently read that same fixed table during actions.
- `Contracts/Snapshots/UnitStatsSnapshot` already carries `MoveRange` and `MoveCost`; baseline calculation, character creation, comparison/copy, and movement-context comparison already carry these facts.

Use this existing route:

```text
CategoryConfigDataSO -> CategoryConfigData -> UnitSpecAdapter.ReadCategories
    -> CategorySpec -> CategoryRegistry -> CategoryDefinition
    -> UnitDefinition -> CharacterBaselineStatsCalculator
    -> battle UnitStatsSnapshot / owned-character stats snapshot
    -> movement availability, range, preview, execution and enemy pathfinding
```

Extend the existing category contract/definition with immutable movement values. `UnitDefinition` derives movement values from its category definition. At runtime, admission and route validation read the actor's existing `Stats.MoveRange` / `Stats.MoveCost`, rather than looking up fixed movement constants again. Keep `CombatActionAdmission` as the existing action boundary; do not add a movement-settings service or a second runtime configuration store.

## 3. Authoring, validation and defaults

**Proposed representation:** add one serialized movement pair (`movementAllowance`, `moveCost`) for each existing category in the current SO. A small nested serializable input struct may group the pair; it is not a new asset type. Export validated, immutable values through the existing data/spec types. Unit SOs continue to identify their category only.

| Category | Initial movement allowance | Initial player move cost |
|---|---:|---:|
| Main | 3 | 2 |
| Rider | 4 | 2 |
| Heavy | 3 | 2 |
| Cannon | 2 | 2 |
| Boss | 0 (voluntary movement disabled) | 2 |

The following edge semantics were proposed in this document and are included in the user's design approval; the existing asset defaults remain unchanged in value:

- Both values are nonnegative integers. Allowance `0` means no voluntary movement; it represents the existing Boss non-movement behavior without inventing a Boss range. It does not disable knockback or other forced movement.
- Cost `0` is a valid free movement action and still consumes that unit's action opportunity, consistent with the existing nonnegative snapshot contract. Costs above the shared maximum are valid but unaffordable for the player; never clamp them or silently replace them with `2`.
- Movement allowance counts traversed orthogonal edges. Diagonals retain their selected L-shaped path and count two edges, including the middle cell's effects. Cost is charged once for an accepted movement action, not per cell.
- Positive allowance does not override `UnitBehaviorSpec.CanMoveVoluntarily`, supported movement method, footprint, terrain, controller or action-use restrictions. Boss content is not re-enabled by this migration.
- Validate at SO export/pure-data and category-contract boundaries; Inspector attributes alone are insufficient. Invalid/missing configuration fails before installation. Preserve the category registry's existing required/optional-key rules.

Explicitly migrate `Assets/ProjectL/DataBaseSO/InGameGroup/Category/Item/CategoryConfig_Default.asset` to the table above and inspect any other assets of the same type. Preserve GUIDs and all unrelated values. Missing serialized fields must not accidentally turn existing movable categories into zero-range categories. New-asset defaults are authoring convenience, not a runtime fallback for missing category data.

## 4. Ownership and scope

The SO is the authored source; exported category definitions are immutable configuration. Installed character snapshots are derived facts for their battle, not independently editable category settings. Capture settings through existing battle-definition initialization; changing an SO does not mutate an active battle. New initialization/stat-query lifetimes use the configuration available through the existing DB lifecycle. Live reload is out of scope.

Buffer retains ownership of installed character facts, shared AP, action use, stale-source validation, revisions and atomic publication. Core calculates proposals. View consumes published facts and existing stats queries; it does not read the InGame category SO. No Framework, Factory registration, ServiceKey, Job/Plan or assembly-boundary redesign is needed. This preserves FW-001–004/016 and the existing prepare-before-install transaction.

Attack prices, shared maximum AP, eight-turn nights, one move OR attack, enemy unbudgeted actions, growth, enhancement behavior and deferred traits/items/gimmicks remain unchanged. Enemy movement uses the authored allowance but neither checks nor spends the player move price. Remove the movement constants/readers from `CategoryActionRules`; keep its attack-price responsibility. Do not restore per-unit authored overrides or retired unit-turn/checkpoint APIs.

## 5. Expected edit and understanding scope

| Area | Intended work |
|---|---|
| Category input | Extend `CategoryConfigDataSO`, `CategoryConfigData` and the existing category asset |
| Immutable translation | Extend `UnitSpecAdapter.ReadCategories`, `Contracts/Commands/Categories/CategorySpec`, `Application/BootstrapModels/CategoryRegistry`, `Domain/Entities/CategoryDefinition` and `UnitDefinition` |
| Action readers | Adapt `CombatActionAdmission` and `MovementRouteValidator`; remove fixed movement members from `CategoryActionRules` |
| Existing fact propagation | Audit `CharacterBaselineStatsCalculator`, `BattleCharacterCalculator`, `OwnedCharacterStatsCalculator`, snapshot copies and `MovementContextCalculator`; modify only a demonstrated missing propagation/validation edge |
| Initialization callers and tests | Migrate category constructor callers and fixture defaults explicitly; inspect `CategoryRegistry.CreateDefault` / the two-argument `UnitCatalog` path used by `StageStartCatalog`. Non-DB validation/test defaults must not substitute for configured categories in live battle or owned-stat calculation |

Before: tuning requires a code-table change and understanding why runtime checks bypass the published movement fields. After: tuning an existing category changes its SO pair; definitions and runtime facts share one derivation route. The initial migration still requires understanding category construction, snapshot lifetime and action settlement. Cross-feature damage/reward internals need no redesign. If a new writable source, public View change or Framework change becomes necessary, report the concrete dependency before extending scope.

## 6. Required verification for later implementation

1. Change one category to non-default values, for example Rider allowance `5` / move cost `4`. Verify SO export, `ReadCategories`, owned-stat query and battle snapshot agree; another category keeps its original values. Verify malformed data rejection and explicit asset migration with `DataBaseAssetTests`.
2. Verify availability, range, preview and committed movement agree on allowance and cost: exact/insufficient AP, one-cell versus longer routes, diagonal middle-cell effects, detours, zero allowance/cost, and stale movement contexts. Accepted actions consume one opportunity; rejected actions change neither AP nor world state. Preserve terminal/interrupted movement settlement.
3. Verify enemies use the new allowance at player AP `0` and never spend the configured price. Verify forced movement and unit behavior restrictions remain independent. Attack prices and controller/night progression stay unchanged.
4. Run the full Core suite and the affected category authoring/adapters/stats, movement publication, command/advance and battle lifecycle integration tests. Existing copy/level-up/redeployment paths must preserve the installed movement facts. Audit for remaining runtime fixed movement lookups and new Unity references in pure Core/Data.

The preceding review at this HEAD ran Core **3,497/3,497 passed** and selected Core–Buffer tests **28 passed / 99 failed** (127 total). Of those failures, 88 stopped at old preparation fixtures, 9 retained obsolete action/night scenarios, and 2 had a parent-code placement omission. These are historical review results, not verification of this proposed change. Capture failure identities before implementation; report existing and new failures separately. Repair relevant blocking fixtures only within the later authorized scope; do not suppress guards/tests or label integration green while blocked.

## 7. Delivery boundary

This delivery changes this English design document only. No code, SO asset, test, scene, prefab or project setting is changed, and no Unity tests are run for documentation. The proposed implementation is one coherent phase after design approval and a short task plan. Report its verification and stop at that phase boundary. Commit and push remain user-owned.
