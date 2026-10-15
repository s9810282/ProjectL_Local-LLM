# Complete DB Data Requirements for Replacing SO-Based Input

Date: 2026-10-08

Source baseline: working tree at `680f309f`, including the uncommitted wall attack blocking Tasks 1–8. Task 9 reconciles the affected schema below; unrelated inventory entries are preserved.

Audience: DB/DT developer, Core developer, and runtime/View integration developers

## 1. Purpose and scope

This is the complete data intake list for replacing the current ScriptableObject (SO) input mechanism. **Every existing data dependency remains a delivery requirement, including data that is already connected and working today.** Existing integration is not a reason to omit an item or mark it complete. The source names below identify what must be reproduced by the replacement data provider; they do not require the new provider to retain SOs or the existing query implementation.

The inventory covers combat, stage/chapter generation and progression, new-run setup, buffer initialization, runtime definition creation, shops, recovery, artifacts, presentation, input, rendering, and project-owned supporting SOs. It also lists required inputs whose current source is a constant, a caller argument, or an incomplete authoring contract.

The rows describe **logical datasets and fields**, not a prescribed physical SQL/DT layout. Nested arrays can become child tables, but their parent keys and significant ordering must survive. Unity asset references need a resolvable asset identifier or asset binding; a numeric gameplay table alone cannot replace a prefab, Sprite, Material, animation, or InputActionAsset.

Field names normally follow the current returned data object. Lowercase artifact effect names match its serialized contract. `int` is 32-bit, `long` is 64-bit, and `CellCoord` is an integer `(x, y)` board coordinate. Existing Inspector defaults are examples of current authoring, not permission to omit a required field.

This is a source-based inventory, not a certification that every authored asset is valid or that production gameplay passes. No gameplay implementation is changed by this document.

## 2. Master delivery checklist

All checkboxes refer to delivery through the **replacement mechanism**, regardless of current SO connectivity.

| ID | Dataset to provide | Current source / scope | Detail |
| --- | --- | --- | --- |
| G01 | Player starting loadout and starting resources | `PlayerLoadoutDataSO` | Section 4 |
| G02 | Chapter/map generation configuration | `StageMapDataSO` | Section 5 |
| G03 | Stage identity, chapter membership, role, and type | `StageRootDataSO` | Section 5 |
| G04 | Combat stage definition and initial placements | `CombatStageData` | Section 6 |
| G05 | Field dimensions and deployment limits | `FieldDataSO` | Section 6 |
| G06 | Unit definitions | `UnitDataSO` | Section 7 |
| G07 | Targeting methods | `TargetingMethodDataSO` | Section 7 |
| G08 | Unit traits and all type-specific parameters | `UnitTraitAuthoring` inside `UnitDataSO` | Section 7 |
| G09 | Category rule configuration | `CategoryConfigDataSO` | Section 7 |
| G10 | Structure definitions | `StructureDataSO` | Section 7 |
| G11 | Stage unit catalog/roster | `UnitRosterDataSO` | Section 8 |
| G12 | Weighted enemy loadout | `EnemyLoadoutDataSO` | Section 8 |
| G13 | Combat economy | `StageEconomyDataSO` | Section 8 |
| G14 | Railway gameplay configuration | Nested in `CombatStageData` | Section 8 |
| G15 | Terrain, walls, obstacles, and object placements/content | Runtime contracts; wall SO intake exists, other environment SO authoring remains incomplete | Section 9 |
| G16 | Shop candidates and prices | `ShopStageData` | Section 10 |
| G17 | Recovery stage options | `EventStageData` | Section 10 |
| G18 | Recovery cost and healing definitions | `RecoveryOptionDataSO` | Section 10 |
| G19 | Artifact identity, starting quantity, shape, effects | `ArtifactDefinitionDataSO` | Section 11 |
| G20 | Artifact board initial dimensions | `ArtifactBoardConfigDataSO` | Section 11 |
| V01 | Unit and structure visual bindings | `UnitVisualDataSO` | Section 12 |
| V02 | Trait text and icons | `TraitVisualDataSO` | Section 12 |
| V03 | Combat field and placement visuals | `StageFieldViewDataSO` | Section 12 |
| V04 | Chapter map layout, prefabs, colors, effects, picking | `StageMapViewDataSO` | Section 12 |
| V05 | Stage node visual variants | `StageNodeVisualDataSO` | Section 12 |
| V06 | Stage background props | `StageViewPropDataSO` | Section 12 |
| V07 | Recovery option display names | `RecoveryOptionVisualDataSO` | Section 12 |
| V08 | Railway presentation | `RailwayVisualDataSO` | Section 12 |
| V09 | Artifact text, icons, and models | `ArtifactViewDataSO` | Section 12 |
| V10 | Artifact popup layout and preview visuals | `ArtifactPopupConfigDataSO` | Section 12 |
| V11 | Input action asset binding | `InputSettingsDataSO` | Section 12 |
| R01 | Material bindings | `MaterialDataSO` | Section 13 |
| R02 | Effect prefabs and pool settings | `EffectDataSO` | Section 13 |
| R03 | Light prefabs and pool settings | `LightDataSO` | Section 13 |
| A01 | Additional unit-card presentation data | `UnitCardVisualsSO` outside the DB catalog | Section 14 |
| A02 | Additional deck image bindings | `DeckCharacterImagesSO` outside the DB catalog | Section 14 |
| A03 | Project audio catalog and runtime bank configuration | `FmodEventCatalog` outside the DB catalog | Section 14 |
| N01 | New-run entry configuration | Current loadout/chapter constants | Section 16 |
| N02 | Chapter continuation and final-chapter definition | `IChapterContinuationSource`; production source absent | Section 16 |
| N03 | Explicit decisions on additional tunable policies | Current code-owned values listed individually | Section 16 |

## 3. Identity, enumeration, and reference requirements

The replacement must support full-table enumeration as well as key lookup. Initialization reads all maps and all artifact definitions; reading only the selected stage is insufficient.

| Identity | Type / current meaning | Required references |
| --- | --- | --- |
| `MapKey` | Positive `int`; also used as chapter key | Stage membership, generation, progression, map registration |
| `StageKey` | Positive `int` for authored stages | Stage details, props, graph node definition binding |
| `StageType` | `Combat`, `Shop`, `Event` | `Event` currently means recovery; preserve that interpretation explicitly |
| `MapRole` | `StageMapRole` | Regular versus boss; separate from stage type |
| `FieldId` | Nonempty `string` | Combat stage to field |
| `EntityKey` | Existing enum-based definition identity | Unit, structure, loadout, placement, shop, and unit visual references |
| `MethodKey` | Positive `int` | Unit to targeting method |
| `RosterKey` | Positive `int` | Combat stage to allowed unit catalog |
| `LoadoutKey` | Positive `int` in each loadout dataset | Player start or weighted enemies; these are separate datasets |
| `EconomyKey` | Positive `int` | Combat stage to starting combat gold and draw cost |
| `OptionId` | Positive `int` | Recovery stage to healing definition and display name |
| `ArtifactId` | Nonnegative `int` | Definition, shape, effects, pool quantity, visual binding |
| `TraitKey`, `CategoryKey` | Typed gameplay enum identities | Rule selection and corresponding presentation |
| Effect `entryId` | Nonempty `string`, distinct within an artifact's effects | Stable effect identity during compilation |
| Environment source ID | `long` | Stable stage-local terrain/object identity; unique across those source lists |
| View node key | `(StageType, MapRole)` | Node visuals are not keyed by `StageKey` |
| Render keys | Material `string`, `EffectType`, `LightPresetType` | Runtime material/effect/light resolution |

SO references currently become these keys in `GetData()` or `CreateStageData()`. Reproduce that relationship, rather than exporting a Unity object pointer. Preserve enum meanings explicitly; do not infer meaning from old enum ordinal values or asset list positions. `StageNodeId`, `EntityId`, owned `CharacterInstanceId`, and artifact instance IDs are runtime identities, not definition primary keys.

The current catalog also supplies group/table/item membership through `DatabaseCatalogSO.databaseGroups`, `DatabaseGroup.databaseTables`, and `DatabaseTable.databaseItems`. The replacement needs equivalent dataset discovery/registration. Inventory coverage is 14 InGame table families, 11 View table families, and 3 Render table families, plus nested stage/trait/effect records and the supporting SOs in Section 14. `TextureDataSO.cs` is empty and currently defines no additional record to migrate.

Sources: [catalog structure][catalog], [InGame registration][ingame-factory], [View registration][view-factory].

## 4. New-run and owned-character initialization — G01

Source: `PlayerLoadoutDataSO` -> `PlayerLoadoutData`. Consumers: `RunStartService.StartNewRun`, `PlayerLoadoutRosterPlanner`, `CharacterDefinitionAdapter`, and `CharacterRosterService`.

| Required field | Type / relationship | Used for |
| --- | --- | --- |
| `LoadoutKey` | `int` | Selecting the starting composition |
| `MainCharacterKey` | Unit `EntityKey` | Creating exactly one owned main character |
| `Entries[].UnitKey` | Unit `EntityKey` | Starting sub-character types |
| `Entries[].Count` | Positive `int` | Number of owned instances of each sub-character |
| Entry order | Ordered list | Deterministic acquisition and resulting owned-instance sequence |
| `StartingHandSize` | `int >= 1` | Initial hand size including the main character; requested sub-character count is this value minus one |
| `StartingRunCurrency` | `int >= 0` | Run wallet used by shops and recovery |

Also provide the referenced main unit's `MaxHealth`, `AttackPower`, `MaxActionCost`, `MoveRange`, and `MoveCost`. `CharacterDefinitionAdapter` creates `MainCharacterPersistentState` with current health initially equal to max health and the other initial values from that unit definition.

Each starting sub-character key appears once in the loadout with its quantity. Main/sub role and player eligibility must match the unit definitions. The main character currently requires `Unit_MainCharacter`, `MainCharacter` role, `PlayerOnly`, a 1x1 footprint, and no traits.

Initialization output is the owned roster and main persistent state in `CharacterBuffer`, starting wallet in `CurrencyBuffer`, and initial hand setting in `RunProgressBuffer`. `InitializeRoster` alone is not the complete new-run operation. On later battles, current owned instances, surviving main state, and soul counts come from runtime state rather than being recreated from the initial loadout.

**Keep two currencies distinct:** `StartingRunCurrency` initializes the run wallet. `StageEconomy.InitialGold` initializes only the current battle's `StageGold`.

Sources: [loadout schema][player-loadout], [new-run service][run-start], [character definition adapter][character-adapter], [roster service][roster-service].

## 5. Chapters, map generation, and stage identity — G02–G03

### G02. Chapter/map configuration

Source: `StageMapDataSO` -> `StageMapData`.

| Required field | Type | Used for |
| --- | --- | --- |
| `MapKey` | `int` | Registering every available chapter in `MapProgressBuffer` |
| `DepthCount` | `int >= 2` | Graph depth including start and boss depths |
| `WidthMax` | `int >= 1` | Maximum node width |
| `AdditionalConnectionChance` | `float`, 0–1 | Extra graph edges |
| `ConnectionAlgorithm` | `Nearest`, `Random`, `WeightedRandom` | Graph connection policy |
| `NearestConnectionWeight` | Positive `float` | Weighted nearest choice |
| `OtherConnectionWeight` | Positive `float` | Weighted other choices |
| `Seed` | `int` | Deterministic graph generation |
| `RandomizeSeedEachRun` | `bool` | Whether the caller replaces the configured seed; current generator uses `Environment.TickCount` |

### G03. Stage root

Source: `StageRootDataSO` -> `StageRootData`.

| Required field | Type / relationship | Used for |
| --- | --- | --- |
| `StageKey` | `int` | Selecting a reusable authored stage definition |
| `MapKey` | Chapter reference | Grouping stage candidates for chapter generation |
| `MapRole` | Regular / boss enum | Selecting regular candidates and the boss definition |
| `StageType` | Derived today from the detail SO type | Routing to combat, shop, or recovery |
| `StageData` binding | Combat/shop/recovery detail record | All type-specific data from Sections 6, 8–10 |

A generated chapter needs regular candidates and exactly one boss stage; the continuation flow requires that boss to be a combat stage. Start nodes use generated `StageKey = 0`, not an authored stage row. Graph node IDs, outgoing connections, selection, and cleared flags are generated/runtime data. If the DT implementation uses child detail tables, provide an explicit stage-to-detail relationship that cannot disagree with `StageType`.

Sources: [map schema][map-data], [stage root schema][stage-root], [graph assembly][stage-generator], [map buffer initialization][map-buffer].

## 6. Combat stage and field — G04–G05

### G04. Combat stage definition

Source: `CombatStageData` -> `CombatStageDetail`. These are required replacement inputs even though the current battle adapter already reads them.

| Required field | Type / relationship | Used for |
| --- | --- | --- |
| Parent `StageKey` | Stage root reference | Selecting this combat detail |
| `FieldId` | Field reference | Board dimensions, reserve capacity, deployment zones |
| `UnitRosterKey` | Roster reference | Unit definition catalog for the stage |
| `EconomyKey` | Combat economy reference | Starting stage gold and manual draw cost |
| `TimelineSeed` | `int` | Battle timeline seed and initial sub-character draw shuffle |
| `NightLength` | Positive `long` | Night-cycle timing in the Core timeline's unit; not assumed to be seconds |
| `MainCharacterAnchor` | `CellCoord` | Initial main-character board location/checkpoint basis |
| `MainCharacterRotation` | `Rotation4` | Initial main-character orientation |
| `InitialEnemyPlacements[]` | Ordered `{ Key, Anchor, Rotation }` | Explicit enemy composition and placement |
| `EnemyLoadoutKey` | Enemy loadout reference; 0 means none | Alternative weighted generation of enemies |
| `StructureCatalog[]` | Structure `EntityKey` list | Available structure definitions |
| `InitialEnemyStructurePlacements[]` | Ordered `{ Key, Anchor, Rotation }` | Enemy structures |
| `HeadquartersKey` | Structure reference; `None` means absent | Separate enemy headquarters definition |
| `EnemyHeadquartersAnchor` | `CellCoord` | Enemy headquarters placement |
| `EnemyHeadquartersRotation` | `Rotation4` | Enemy headquarters orientation |
| Railway enabled/presence | SO `railwayEnabled`; nullable returned `Railway` | Whether railway gameplay exists in this battle |
| `Railway` fields | Section 8 | Railway cells and turn intervals |
| All environment placement lists | Section 9 | Terrain, neutral obstacles, and objects |
| `WallCells[]` | Unique `CellCoord` list; SO field `initialWallCells` | Static wall occupancy and derived attack blocking geometry; empty means no walls |
| `WallBlocking.CornerCutTicks` | `int`, 0–511; SO field `cornerCutTicks`, default 128 | Cut size on outer corners of a complete wall run, in 1/1024-tile units |

Explicit enemies and a nonzero `EnemyLoadoutKey` are mutually exclusive, including a zero-spawn loadout. Enemy headquarters is inserted before the other authored enemy structures. Preserve authored placement order because runtime entity allocation depends on it. Do not confuse the old `playerHeadquartersAnchor` serialized alias with the current `MainCharacterAnchor` meaning.

Battle definition assembly must include the union of stage roster units, currently owned character types, and explicit enemy placement types. Owned characters obtained from shops may not appear in the selected stage's original roster.

### G05. Field definition

Source: `FieldDataSO` -> `FieldData`.

| Required field | Type | Used for |
| --- | --- | --- |
| `FieldId` | `string` | Lookup from combat stage |
| `ColumnCount`, `RowCount` | Positive `int` | Board width/height and bounds |
| `ReserveCapacity` | Positive `int` | Available reserve/hand slots |
| `PlayerDeploymentDepth` | Positive `int` | Player deployment region at the bottom |
| `EnemyDeploymentDepth` | Positive `int` | Enemy deployment region at the top |

Deployment regions and all rotated occupied cells must fit the board. Placements cannot overlap. Reserve capacity must accommodate the actual initial sub-character draw. A prefab scale or View spacing value must not substitute for gameplay dimensions.

Sources: [combat SO][combat-so], [combat detail][combat-data], [field definition][field-data], [battle adapter][battle-adapter], [battle preparation][battle-preparation].

## 7. Combat definitions — G06–G10

### G06. Unit definition

Source: `UnitDataSO` -> `UnitData` -> `UnitSpec` / runtime unit definition.

| Required field | Type | Used for |
| --- | --- | --- |
| `Key` | `EntityKey` | Unit definition identity |
| `Role` | `UnitRole` | Main versus regular unit behavior |
| `OwnerEligibility` | `UnitOwnerEligibility` | Player/enemy acquisition and deployment eligibility |
| `FootprintWidth`, `FootprintHeight` | Positive `int` | Occupied shape and collision/deployment checks |
| `MaxHealth` | Positive `int` | Initial health capacity |
| `PhysicalDefense` | Nonnegative `int` | Incoming damage calculation |
| `MoveRange` | Nonnegative `int` | Movement reach |
| `MoveCost` | Positive `int` | Movement action cost |
| `TargetingMethodKey` | `int`, targeting reference | Attack shape, selection, and splash definition |
| `AttackRange` | Nonnegative `int` | Owner-specific attack reach |
| `Category` | `CategoryKey` | Category rules and matchup calculation |
| `Occlusion.Delivery` | `AttackWallDelivery`: `RequireClear = 1`, `IgnoreWalls = 2` | Direct attacker-to-contact wall policy; SO field `wallDelivery`, default RequireClear |
| `Occlusion.Propagation` | `AttackWallPropagation`: `FromImpact = 1`, `IgnoreWalls = 2` | Impact-to-secondary-cell wall policy; SO field `wallPropagation`, default FromImpact |
| `AttackPower` | Nonnegative `int` | Base attack damage |
| `MaxActionCost` | Nonnegative `int` | Action resource capacity |
| `AttackCost` | Positive `int` | Attack action cost |
| `Priority`, `Delay` | Positive `int` | Timeline initialization and scheduling |
| `Traits[]` | Ordered typed definitions, G08 | Rule setup and initial trait state |

Current conversion fills every local cell in the width-by-height rectangle, sets `MoveMethod.LiftAndPlace`, and enables timeline configuration. `IsCannon` is derived from `Category == Cannon`. Targeting method shape/choice/splash own the attack footprint; the PiercingFalloff trait enables piercing. Category alone does not grant wall bypass. These are conversion rules, not additional SO columns.

`CannonAreaType`, `CannonAreaRange`, `PierceRayCount`, `ImpactPatternSpec`, and `AttackImpactMethod` are retired and are not replacement input requirements. For any external piercing content still using the old schema, copy its former effective `ImpactPattern.Range` into the trait's explicit `MaxTargetsPerRay` before removing legacy data. Do not substitute a positive default for zero. The five current unit assets have no piercing trait; only their unused Cannon fields were removed. Piercing ray steps now come from the targeting method's shared ordered geometry, not a separate one-versus-three ray field.

Both occlusion enum values must be valid; zero and unknown values are rejected. `AttackOcclusionSpec.Default` is RequireClear/FromImpact. Null optional runtime policy input uses this default; a DB importer must distinguish absence from an invalid numeric enum. IgnoreWalls changes obstruction only, never target ownership eligibility or full-cell wall occupancy. Current cell-aim segments use exact integer endpoints, including arbitrary slopes; wall policy does not expand the targeting mask.

### G07. Targeting methods

Source: `TargetingMethodDataSO` -> `TargetingMethodData` -> `TargetingMethodSpec` -> `CombatAttackProfile`.

| Required field | Type / values | Used for |
| --- | --- | --- |
| `MethodKey` | Positive `int` | Definition identity |
| `Shape` | `CardinalLine`, `DiagonalLine`, `EightDirectionLine`, `Fan`, `RadialSquare` | Reach geometry |
| `ChoiceMode` | `Single`, `Direction`, `All` | How the attack aim is selected |
| `Splash` | `None`, `Cross`, `Diagonal`, `Radial` | Secondary affected cells |
| `SplashPercent` | Nonnegative `int` percentage | Damage scaling for secondary cells |

`AttackRange` stays on the unit; it is not a method field. Splash is supported only with `Single` choice mode. No arbitrary 100% upper bound should be invented: current validation only requires a nonnegative percentage. Unit traits, category rules, and targeting must also pass combined `CombatAttackProfile` validation. Piercing currently requires exactly EightDirectionLine shape, Direction choice, and no splash.

Despite the EightDirectionLine name, its Direction choice currently accepts four cardinal inputs and emits three ordered rays: forward, left diagonal, right diagonal. A configurable single-ray piercing unit is not supported by this schema. Do not infer selectable directions or ray count from the shape name. This existing targeting limitation is independent of arbitrary-slope wall clipping for legal cell aims.

Although `UnitData` allows method key 0 for a nonattacking unit, the current production battle source rejects method key 0 for every included unit because other Core operations also look up the attack profile. The replacement must preserve this limitation or pair a deliberate contract change with the necessary Core work; merely supplying attack power 0 does not make the current battle pipeline accept it.

### G08. Complete unit trait payloads

Each trait record needs its parent unit key, `TraitKey` discriminator, order, and the matching typed payload. A display name or trait ID without parameters is insufficient. Duplicate trait keys on one unit are rejected.

| Trait | Required parameters | Current constraints / meaning |
| --- | --- | --- |
| `Focus` | `DamagePerStack`, `MaxStacks` | Both positive |
| `MoveAttackDiscount` | `DiscountAmount`, `MinimumAttackCost` | Both positive |
| `PiercingFalloff` | `ReductionPercentPerTarget`, `MaxTargetsPerRay` | Reduction is integer 1–100; cap is integer >= 0, required explicitly. Zero yields no hits; it does not mean unlimited. SO fields: `reductionPercentPerTarget`, `maxTargetsPerRay` |
| `ThornField` | `ThornDamage`, `LifetimeUnitTurns` | Both at least 1; lifetime counts unit turns |
| `Knockback` | `DistanceCells` | At least 1 |
| `DistanceDamageScale` | Ordered `PercentByDistance[]` | Nonempty, nonnegative; index 0 is distance 1; must cover reachable attack distances |
| `KillChainAttack` | Trait identity only | No numeric parameters in the current definition |

The piercing cap is per ray and counts eligible encounters in traversal order; the damage-derived falloff budget can stop a ray earlier. Shared traversal stops at walls unless Delivery ignores them and stops at physical structures even when the structure is not an eligible victim. Blocked diagnostic targets consume neither cap nor falloff. Existing Cannon adjacent-cell exclusion remains unchanged.

Secondary-effect policy is explicit: ordinary splash propagates from the selected impact cell using Propagation; immediate Thorn placement uses that same propagation policy before creating fields. Wall-occupied cells remain unavailable even under IgnoreWalls. Existing KillChain follow-up damage is hostile-only and wall-independent. Powder-barrel/environmental damage and later entry into an already-created Thorn field remain wall-independent. These are code-owned mechanics, not additional trait DB toggles.

### G09. Category configuration

Source: `CategoryConfigDataSO` -> singleton `CategoryConfigData`.

Provide `HeavyDamageReductionPercent` (`int`, 1–100) and `HeavyInitialFacing` (`Rotation4`). The adapter creates Base, Rider, Heavy, and Cannon category definitions; Rider uses maneuver refund, Heavy uses directional armor, and Cannon uses safe bombardment. Those category-to-rule assignments are presently code-owned. The separate matchup matrix and target-policy decisions are listed in Section 16 so they are not mistaken for fields already supplied by this SO.

### G10. Structure definitions

Source: `StructureDataSO` -> `StructureData` -> `StructureSpec`.

Provide `Key` (`EntityKey`), `FootprintWidth`, `FootprintHeight` (positive `int`), `MaxHealth` (positive `int`), `IsAttackable` (`bool`), and `AttackPower` (nonnegative `int`). The existing validator requires `AttackPower >= 1` when `IsAttackable` is true. Local footprint cells are currently derived as a full rectangle. Headquarters and other structures also need V01 visual bindings.

Sources: [unit SO][unit-so], [unit data validation][unit-data], [trait payloads][trait-data], [targeting schema][targeting-data], [category schema][category-data], [structure schema][structure-data], [battle adapter][battle-adapter].

## 8. Stage catalogs, spawning, economy, and railway — G11–G14

| ID | Source | Complete required data | Consumer / purpose |
| --- | --- | --- | --- |
| G11 | `UnitRosterDataSO` | `RosterKey`; ordered `UnitKeys[]` | Resolve stage unit definitions and validate weighted enemy candidates |
| G12 | `EnemyLoadoutDataSO` | `LoadoutKey`; ordered `Entries[]` of `UnitKey`, positive `Weight`; nonnegative `SpawnCount`; `RandomSeed` | Weighted type draw and placement generation |
| G13 | `StageEconomyDataSO` | `EconomyKey`; nonnegative `InitialGold`; nonnegative `ManualDrawGoldCost` | Initialize `CombatStateBuffer.StageGold` and manual draw cost |
| G14 | `CombatStageData` railway payload | Enabled/presence; ordered `RailCells[]` of `(x, y)`; positive `FirstActivationAfterUnitTurns`; positive `RepeatIntervalUnitTurns` | Railway board occupancy/effect timing and runtime gimmick initialization |

Enemy loadout candidates with a positive spawn count must belong to the stage roster and have enemy-eligible regular-unit definitions. Preserve entry ordering and the RNG seed: the calculator draws types with repetition, then shuffles enemy-zone cells using the same RNG. Enemy structures, including the full rotated headquarters footprint, and wall cells exclude spawn cells. Insufficient space is an error rather than permission to drop enemies. Automatic multi-cell unit spawning is not supported by the current allocation policy. A zero-spawn loadout can be empty.

`InitialGold = 0` and `ManualDrawGoldCost = 0` can be explicit valid values. A missing economy reference is still invalid for the production combat detail. Railway turn counts are not presentation seconds; V08 supplies separate visual timing.

Sources: [roster data][roster-data], [enemy loadout][enemy-loadout], [economy data][economy-data], [battle adapter][battle-adapter].

## 9. Environment content and placement — G15

These inputs must be included in the new data intake. `CombatStageData.CreateStageData()` now supplies `WallCells` and `WallBlocking`. The older general terrain, obstacle, and object placement lists on `CombatStageDetail` still have no SO authoring path and remain empty through normal SO conversion. The remaining gap does not remove their runtime requirements.

The exact combat-detail collections are `TerrainPresencePlacements[]`, `StaticTerrainPlacements[]`, `ObstaclePlacements[]`, `ObjectPlacements[]`, and `WallCells[]`. Every placement needs a parent stage binding and stable authored order where specified. Reusable content definitions and per-stage placements may be separate tables.

| Content | Required initial fields | Runtime data created |
| --- | --- | --- |
| Presence/stat-modifier terrain | `SourceId: long`, `Cell`, `KnownTo: Owner[]`, `Definition.AttackBonus: int`, `Definition.MaxHealthBonus: int` | `DestinationTileModifierFact`, terrain semantic binding, and character positional modifiers |
| Static terrain | `SourceId: long`, `Cell`, `KnownTo: Owner[]`, `Kind: StaticTerrainKind` (`HighGround = 1`, `Sandstorm = 2`, `Wall = 3`) | `StaticTerrainFact` and terrain semantic binding; walls must satisfy the rules below |
| Preferred wall stage input | `WallCells[]`, `WallBlocking.CornerCutTicks` | Sorted wall cells become active Wall facts known to Player and Enemy, with allocated stage-local IDs and Terrain bindings |
| Neutral obstacle common data | Structure definition/reference with its footprint and stats; `Kind`, `Anchor`, `Rotation` | Neutral `StructureCombatFacts` and `ObstacleContentProfile` |
| Powder barrel | Common obstacle fields; nonnegative `ExplosionDamage`; both attack reward values zero | Destruction explosion behavior; kind must agree with the structure definition key |
| Supplies | Common obstacle fields; explicit nonnegative `HitAttackBonus`, `DestructionAttackBonus`; `ExplosionDamage = 0` | Attack rewards for hit/destruction; preserve explicit configured rewards |
| Mine | Stable source ID, `Cell`, `Damage`, `KnownTo: Owner[]`, authored placement order | `MineSourceFact` with generated entry ordinal |
| Flag | Stable source ID, `Cell`, `PriorityBonus`, `KnownTo: Owner[]`, authored placement order | `FlagSourceFact` with generated entry ordinal |
| Sheriff badge | Stable source ID, `Cell`, `KnownTo: Owner[]`, authored placement order | `SheriffBadgeSourceFact` with generated entry ordinal |
| Tornado | Initial source ID, `Cell`, `KnownTo: Owner[]`; decision on initial activation timing | `TornadoSourceFact`; see the extra source gap below |

Tornado has a runtime fact and an optional parameter on `StageEnvironmentAdapter.Read`, but no tornado list on `CombatStageDetail`, and the production adapter does not supply one. Its current interval is fixed to 3 completed unit turns; the runtime fact's initial due turn defaults to that interval. If tornado content must be authored, add the stage input relationship as well as deciding whether initial timing/interval should be data-driven.

Terrain and object source IDs must be unique across their combined stage lists. Obstacle entity IDs are allocated during battle preparation and must not be exported as permanent definition IDs. Array order for objects establishes arrival/entry order. Coordinates, occupied cells, visibility owners, and combinations of terrain on one cell need validation. Current exclusive static-terrain combinations cannot be silently stacked by the importer.

### Wall authoring and lifetime contract

- `WallCells` is copied at intake. The adapter sorts by Y then X and rejects duplicate cells. It allocates wall source IDs above the maximum of all earlier terrain/object sources using checked arithmetic. Do not provide the same wall through both WallCells and StaticTerrainPlacements.
- Explicit Wall facts must be active, public to both owners, in bounds, uniquely placed, and classified as Terrain. They are static and non-destructible for the entire battle; removal, relocation, deactivation, visibility changes, and changing CornerCutTicks during command publication are rejected. Battle exit clears them; a new battle installs its own facts/settings.
- Each four-neighbor connected component must be one horizontal or vertical, one-cell-wide contiguous run. Isolated cells are allowed. Reject bends, L/T/cross shapes, thick blocks, and diagonal contact between separate runs. Unsupported topology is a content error, not an approximate polygon.
- CornerCutTicks is 0–511 inclusive, with default 128 and fixed TicksPerTile = 1024. Corners are cut on the outside of the complete run, not each tile seam. A segment touching only the boundary is clear; crossing the open interior blocks. Cell endpoints retain arbitrary slopes and are never rounded to four/eight directions for wall checks.
- Walls occupy full cells for movement and placement regardless of corner cuts or attack IgnoreWalls. Initial character/structure/object/terrain/rail overlap is invalid. Weighted spawning, redeployment, voluntary/forced movement, and generated destinations must respect the same occupancy constraint.
- Without walls, use an empty WallCells list and the default settings. Existing SO assets obtain the new default fields without requiring a bulk reserialization. Replacement DB rows must deliberately supply or default these values; enum value zero is not a valid policy.

The static source initializes environment state once. Consumption, movement, discovered visibility, held flags, remaining activations, and obstacle HP are runtime state; do not recreate consumed content from the static table each command.

Sources: [combat detail contract][combat-data], [SO conversion gap][combat-so], [environment adapter][environment-adapter], [obstacle placement contract][obstacle-spec], [object placement contracts][object-spec], [tornado contract][tornado].

## 10. Shop and recovery stages — G16–G18

### G16. Shop

Source: `ShopStageData` -> `ShopStageDetail`.

Provide the parent stage relationship and the ordered `Candidates[]` list containing `UnitKey` and nonnegative `Cost` for each candidate. Also provide every referenced unit definition, including role and owner eligibility, and its visual data. Candidates must be acquirable player sub-characters. Current generation selects candidates uniformly with replacement; there is no shop weight column today.

The service additionally requires an input seed for offer generation. Offer IDs, selected offers, and sold-out flags are generated per runtime shop entry in `CurrentProgressBuffer.Shop`. Purchase cost is deducted from the **run wallet**. Current offer count is a code constant; seed and offer-count decisions are recorded in Section 16.

### G17. Recovery stage

Source: `EventStageData` -> `EventStageDetail`.

Provide the parent stage relationship and ordered `RecoveryOptionKeys[]`. Current `Event` stage type is recovery, and recovery entry requires a non-boss stage. Preserve option order for the offered choices.

### G18. Recovery option definition

Source: `RecoveryOptionDataSO` -> `RecoveryOptionData`.

Provide positive `OptionId`, nonnegative `Cost`, `HealMode` (`Flat`, `PercentOfMax`, `Full`), and `HealValue`. `HealValue` is nonnegative for `Flat` and an integer from 1 through 100 for `PercentOfMax`; `Full` has no numeric healing amount to apply. The corresponding V07 row supplies the display name.

Recovery builds `RecoveryOptionSpec` / `RecoveryHealSpec` and current recovery entry data; payment uses the run wallet and healing changes the main character's persistent state. Purchased/resolved state and resulting HP are runtime values, not static option columns.

Sources: [shop detail][shop-data], [shop service][shop-service], [recovery detail][recovery-stage], [recovery option][recovery-option], [recovery adapter][recovery-adapter].

## 11. Artifacts and board initialization — G19–G20

### G19. Artifact definition

Source: `ArtifactDefinitionDataSO` -> `ArtifactDefinitionData` / `ArtifactDefinition`.

| Required field | Type | Used for |
| --- | --- | --- |
| `ArtifactId` | Nonnegative `int` | Definition identity |
| `InitialCount` | Nonnegative `int` | Initial available quantity in the artifact pool |
| `Cells[]` | Ordered local `CellCoord` list | Authored shape, placement, rotation, collision, preview |
| `Effects[]` | Ordered `ArtifactEffectData` list | Compile equipped artifact behavior |

Shape cells are explicit authored offsets, unlike the rectangle derived for a unit. Do not replace them with only a width and height.

### Complete effect record

| Required field | Type / meaning |
| --- | --- |
| `entryId` | Stable effect identifier string |
| `trigger` | `ArtifactTriggerType` |
| `source.faction`, `source.category` | Faction filter and `CategoryKey` for the triggering source |
| `condition.type`, `condition.comparison` | Condition discriminator and comparison |
| `target.type`, `target.faction` | Selection mode and faction filter |
| `target.category`, `target.excludeCategory` | Category filter and exclusion flag |
| `target.useHpRatioFilter`, `target.maxHpRatio` | Filter enablement and normalized 0–1 threshold |
| `location.type` | `ArtifactLocationType` |
| `effect.type` | `ArtifactEffectType` |
| `effect.statId` | Stat identity string when applicable |
| `effect.knockbackDistance` | Distance in cells when applicable |
| `effect.soulGrowth.attackBonus` | Soul growth attack amount |
| `effect.soulGrowth.maxHealthBonus` | Soul growth max-health amount |
| `effect.soulGrowth.healAmount` | Soul healing amount |
| `value.type`, `value.basis` | Calculation mode and percentage basis |
| `value.amount` | Finite `float`; percentage 20 means 20%, unlike normalized HP ratio |
| `durationTurns` | `int`; meaning and supported value depend on the effect schema |

The serializable structure is broader than the current compiler's supported combinations. Supplying every enum value does not make every combination executable. Preserve all fields, including required neutral values, and validate against `ArtifactEffectCompiler`:

| Supported schema | Current defining combination |
| --- | --- |
| Healing Chalice | `StageCleared`; allied main character; `Heal`; flat amount exactly 3; duration 0 |
| Last Stand | `StageActive`; allied unit-count disadvantage; `ModifyStat` with `statId = "Attack"`; unit-count difference amount exactly 5; duration 0 |
| Soul Altar | Enemy `UnitDied` at night; allied collector; death position; `SpawnSoul`; attack/max-health growth fixed at +1/+1; authored nonnegative healing; duration exactly 5; unused value amount 0 |
| Thorn Heart | Enemy `UnitDamaged` source at night; enemy last attacker; `Damage`; nonnegative whole flat amount; duration 0 |

The compiler currently rejects category target filtering, HP-ratio filtering, and knockback payload use for these schemas. Those serialized fields still belong in the replacement inventory; their accepted values must match the supported contract. Do not substitute generic buff execution for the actual compiler restrictions.

### G20. Artifact board configuration

Source: singleton `ArtifactBoardConfigDataSO` -> `ArtifactBoardConfigData`.

Provide positive integer `InitialWidth` and `InitialHeight`. Stage lifecycle uses them to request board initialization. `ArtifactEditingService.InitializeBoard` enumerates **all artifact definitions**, creates the initial pool from `InitialCount`, and initializes `ArtifactBuffer`. Placement instance IDs, placed anchors/rotations, pool remaining counts, layout revision, and previews are runtime outputs.

The current `ResetRunProgress` intentionally leaves artifact ownership/state intact. Do not assume that artifact initial quantities are automatically reapplied on each `StartNewRun`; any change to that lifetime requires an explicit behavior decision.

Sources: [artifact SO][artifact-so], [effect payload][artifact-effect], [effect compiler][artifact-compiler], [board config][artifact-board], [artifact initialization service][artifact-service], [run reset behavior][progress-transactions].

## 12. Complete View and input data — V01–V11

These are all replacement requirements, even when a visual feature is already connected or a current consumer uses only part of a record. They need not be placed in the same DT as gameplay values, but the new provider must supply their data or asset bindings.

| ID | Dataset / key | Complete fields to provide | Main use |
| --- | --- | --- | --- |
| V01 | Unit/structure visuals / `EntityKey` | `Key`, `Prefab`, `DisplayName`, `TimelineIcon`, `InfoIcon` | Board units, structures, placement ghosts, hand, deck, cards, timeline |
| V02 | Trait visuals / `TraitKey` | `Key`, `DisplayName`, `Icon`, `Description` | Trait cards and unit information |
| V03 | Field view / singleton | `CellPrefab`, `AlternateCellPrefab`, `CellOffsetSize`, `CellPaddingX`, `CellPaddingY`, `HandUnitLayerMask`, `GhostMaterial`, `ValidGhostTint`, `InvalidGhostTint`, `GhostWorldOffset`, `GhostScale`, `FieldUnitScale` | Board geometry presentation, picking, and placement feedback |
| V04 | Stage map view / singleton | `DepthSpacing`, `WidthSpacing`, `NodeScale`, `LineHeight`, `LineWidth`, `CurrentColor`, `SelectableColor`, `HoveredColor`, `CandidateColor`, `UnavailableColor`, `HoverEffect`, `SelectEffect`, `NodePrefab`, `ConnectionPrefab`, `StartVisualPrefab`, `CurrentMarkerPrefab`, `MarkerMoveDuration`, `MarkerJumpHeight`, `NodeMask`, `PickDistance` | Chapter map layout, selection effects, traversal marker, picking |
| V05 | Stage node visuals / `(StageType, MapRole)` | `StageType`, `MapRole`, `VisualPrefab`, `Icon`, `DisplayName` | Regular combat, boss, shop, and recovery node variants |
| V06 | Stage props / `StageKey` | `StageKey`, `GameObject`, `PositionOffset`, `RotationOffset`, `ScaleOffset` | Stage-specific background prop |
| V07 | Recovery option visuals / `OptionId` | `OptionId`, `DisplayName` | Recovery choice presentation |
| V08 | Railway visuals / singleton | `TrainPrefab`, `RailPrefab`, `RailTilePrefab`, `Controller`, `Clip`, `Effect`, `ClipEndZ`, `Duration`, `TrainWidthInCells`, `OutsideCells`, `TravelPaddingCells`, `TrainModelYaw`, `Reverse` | Railway model, animation, travel layout, and effect binding |
| V09 | Artifact visuals / `ArtifactId` | `ArtifactId`, `DisplayName`, `Description`, `Icon`, `Prefab` | Artifact inventory, board models, information popup |
| V10 | Artifact popup config / singleton | `cellSpacing`, `cellScale`, `modelScale`, `modelHeight`, `boardCenter`, `previewValidColor`, `previewInvalidColor`, `backgroundDistance`, `backgroundMargin` | Board/model layout, drag preview, background sizing |
| V11 | Input settings / singleton | `Actions` (`InputActionAsset` binding) | Runtime action-map and input initialization |

Vectors, colors, masks, prefabs, Sprites, Materials, animation controllers, and clips retain their actual Unity types or an explicitly defined serialized equivalent plus resolver. `InputActionAsset` replacement must retain the action maps, bindings, and action identities that the input bridge expects, not just a human-readable filename.

Singleton lookup semantics must be explicit in the new provider. Empty configuration tables and missing referenced assets must not silently become zero spacing, a zero mask, or null prefabs. Optional icons/props should retain their current optionality; do not make every asset universally mandatory merely because it appears in this inventory.

Recovery visual and railway visual datasets remain on the complete delivery list even if their current query-to-screen consumption is incomplete. The goal is replacing the whole data source, not listing only call sites that currently execute.

Sources: [View item schemas][view-items], [View queries][view-queries], [View registration][view-factory].

## 13. Render and pool initialization — R01–R03

| ID | Current SO / key | Complete fields to provide | Runtime use |
| --- | --- | --- | --- |
| R01 | `MaterialDataSO` / string key | `Key`, `Material` | Material lookup; current consumers include `_SelectOutline` and `_AreaOutline` |
| R02 | `EffectDataSO` / `EffectType` | `EffectType`, `EffectPoolInitializeCount`, `EffectPoolMaxCount`, `Prefab` | Effect pool creation and effect spawning |
| R03 | `LightDataSO` / `LightPresetType` | `PresetType`, `UnityLightType`, `LightPrefab`, `InitialPoolSize` | Light preset selection and initial pool construction |

The current code contains the spellings `MetrialData`, `MetrialTable`, `MetrialDataTableSO`, and `EffecDataTableSO`; these are source identifiers, not additional datasets. Preserve the key-to-asset relationship even if the new schema corrects these names.

Source: [Render item schemas][render-items].

## 14. Project-owned SOs outside the central DB catalog — A01–A03

These are also included so that replacing the SO input approach does not miss data attached directly to components. Their transfer to the central DB versus a separate asset/config provider is an ownership choice; it is not a reason to lose the fields.

| ID | Source | Complete data |
| --- | --- | --- |
| A01 | `UnitCardVisualsSO.characters[]` | `Character` (`EntityKey`), `DisplayName`, `Portrait`, `SynergyIcon`, `SynergyDescription`, `FunctionIcon`, `FunctionDescription` |
| A02 | `DeckCharacterImagesSO.images[]` | `Character` (`EntityKey`), `Image` (`Sprite`) |
| A03 | `FmodEventCatalog` runtime config | `bankFolderOverride`, `includedBanks[]`, `loadSampleData` |
| A03 | Audio bus records | `path`, `name`, `parentPath`, `guidString`, `sourceBankPaths[]` |
| A03 | Audio event records | `path`, `folderPath`, `eventName`, `displayName`, `sourceBankPath`, `guidString`, `isOneShot`, `isSnapshot`, `is3D`, `hasSustainPoint`, `lengthMs`, `minDistance`, `maxDistance`, ordered `parameters[]` |
| A03 | Audio parameter records | `name`, `idString`, `scanIndex`, `kind`, `scope`, `isReadOnly`, `isAutomatic`, `min`, `max`, `defaultValue`, `labels[]` |
| A03 | Audio scan metadata | `lastScanTime`, `lastScannedFolder`, `lastBankFileCount`, `lastEventCount`, `lastBusCount` |

Audio scan metadata is tooling metadata, not combat-balance data; audio runtime caches/refcounts are generated and are not authoring fields. Vendor/plugin settings and Unity project/import settings are not game-content DTs. A01 overlaps V01/V02 in presentation purpose, so the replacement needs an explicit resolution rule if they are consolidated. A02 is included even without an identified active consumer.

Sources: [unit-card supporting SO][unit-card-so], [deck supporting SO][deck-images-so], [audio catalog][audio-catalog].

## 15. Buffer initialization and runtime creation mapping

This table identifies what the DB developer must supply for each runtime boundary. It does not ask the DB to serialize live buffers.

| Boundary / target | Required definition inputs | Values created or installed by runtime |
| --- | --- | --- |
| DB/bootstrap discovery | Catalog/table membership; all required singleton configs and keyed records | Query registries and validated lookup maps |
| `MapProgressBuffer.InitData` | All G02 map keys | Registered maps, initially uncleared |
| `RunStartService.StartNewRun` -> `PublishNewRun` | G01 plus referenced G06 unit definitions | Owned character instances, main persistent state, run wallet, initial hand setting; prior run progress reset |
| Chapter entry / `StageProgressBuffer` | G02 plus all G03 stages for the chapter | `ChapterGraphSpec`, generated nodes/edges, stage type/role snapshots, current chapter selection |
| Artifact board initialization / `ArtifactBuffer` | G20 and every G19 definition | Board bounds, initial pool counts, empty placement state |
| `DatabaseBattleInitializationSource.Read` | G03–G15 plus definitions for all owned units | `BattleInitialization`, `UnitSpec`, `StructureSpec`, category specs, targeting/attack profiles, environment facts |
| `BattlePreparationCalculator` -> `CharacterBuffer` | Unit stats, traits, categories, placements; current owned/main/soul runtime facts | Main/enemy/reserve character facts, health/action values, footprint occupancy, initial category/trait state |
| Battle publication -> `CombatStateBuffer` | Field, timing, placements, economy, railway, environment, initial draw setting | Board size, reserve capacity, phase, timeline basis, draw order/cursor, next entity ID, structures, environment bindings, obstacle profiles, `StageGold`, `ManualDrawGoldCost` |
| Combat effect setup and later command reads | G19 effects for equipped artifact IDs; G08/G09 rule definitions | Compiled effect bindings and evaluations; runtime state remains in its owning buffers |
| Shop entry -> `CurrentProgressBuffer.Shop` | G03 shop root, G16 candidate list, G06 referenced definitions, caller seed | Generated offers, offer identities, initially unpurchased flags |
| Recovery entry -> current recovery state | G03 recovery root, G17 ordered option references, G18 definitions | Recovery specs and per-entry state |
| Chapter completion / run completion | N02 continuation definition plus registered maps | Next chapter entry or final run-completion result |
| Scene/View preparation | V01–V11 and required R01–R03 references | Board/node/character models, UI presentation, input bindings, effect/light pools |

Additional state needed to create a battle comes from runtime: owned card instance IDs, current main HP/growth, soul counts, selected chapter/node/stage, artifact placements, and run outcome. The DT supplies initial/default definitions, not a replacement for these state owners.

Do not add static DT columns solely for current HP, current AP, draw cursor, shuffled deck order, timeline queue entries, effect stacks/cooldowns, consumed objects, current gold, purchased flags, cleared nodes, command/result counters, previews, or input commands. If save/load is required, that is a separate persistence contract. Source constants/defaults for their initial values are distinguished in Section 16.

`CombatViewBuffer`, `StageViewBuffer`, and `ArtifactViewBuffer` start with empty input/preview state; they do not need authored command rows. Likewise, current selection and battle-result state are created by runtime operations rather than read from SO records.

Sources: [map initialization][map-buffer], [run publication][progress-transactions], [combat state fields][combat-state], [battle preparation][battle-preparation], [artifact service][artifact-service].

## 16. Required source gaps and code-owned policy decisions — N01–N03

These items are included alongside all existing SO fields. Proposed column names below are suggestions for the replacement contract, not claims that those columns already exist.

### N01–N02. Inputs needing an explicit production source

| Required input | Current source / gap | What the replacement must settle |
| --- | --- | --- |
| Starting loadout selection | `StagePlan.DefaultLoadoutKey = 1` | Explicit start configuration or caller-provided `StartingLoadoutKey` |
| Starting chapter selection | `StageJob` uses chapter 1 when no map is selected | Explicit `StartingMapKey` or equivalent start context |
| Next/final chapter definition | `IChapterContinuationSource`; default is `UnavailableChapterContinuationSource`; two-chapter source is development-only | Per-chapter `NextChapter` versus `Final` kind and valid `NextChapterKey`; do not infer continuation from numeric key order |
| Environment authoring | Wall cells/settings now have SO intake; other G15 placement lists remain absent from `CombatStageData` authoring; tornado also lacks the combat-detail link | Supply complete typed content and stage placement input through the replacement |
| Shop offer seed | Argument to `EnterShop`/offer preparation, not a `ShopStageData` field | Define caller/run seed ownership and reproducibility; persist generated offers as runtime state rather than redrawing on every read |

### N03. Values presently supplied by code, not by an SO column

The game has working code rules for these values. Include them in the DB/Core agreement so that changing input technology does not silently drop, duplicate, or accidentally re-author them. Moving them into DT requires both a data contract and consuming Core support.

| Policy / tunable | Current behavior to preserve or explicitly replace |
| --- | --- |
| Category matchup matrix | `CategoryMatchupSpec.CreateDefault()` contains all 16 directed Base/Rider/Heavy/Cannon pairs with numerator/denominator values. A future table needs attacker category, defender category, numerator, denominator, and complete pair coverage |
| Category rule assignments | Base has no category rule; Rider maneuver refund; Heavy directional armor; Cannon safe bombardment; only Heavy parameters currently come from G09 |
| Attack target policy | Empty-cell, friendly, neutral, and adjacent-cell constraints are composed by Core rules; they are not existing targeting-method SO columns |
| Unit geometry/movement conversion | Full rectangular footprint, `LiftAndPlace`, timeline enabled; targeting method owns attack footprint |
| Attack obstruction/effect policy | Unit Occlusion fields are authored (G06); wall occupancy and the special-effect exceptions listed in G08 remain code-owned |
| Initial hand fallback | Roster-only paths use `InitialDrawPlan.DefaultStartingHandSize`; the new-run path uses G01 and must retain it |
| Initial draw seed | Uses the combat stage's `TimelineSeed`; there is no separate player-loadout shuffle seed |
| Graph randomization | G02 flag currently replaces seed with `Environment.TickCount`; reproducible run seeds require an explicit generation/persistence decision |
| Shop offer count and sampling | `ShopStageSnapshot.OfferCount = 3`; uniform candidate sampling with replacement |
| Artifact canonical values | Compiler enforces Chalice 3, Last Stand 5, Soul Altar +1/+1 and duration 5; these are not freely tunable just because the payload carries numeric values |
| Artifact reset scope | Artifact state survives current new-run reset; choose an explicit policy before changing this lifetime |
| Terrain rules and tornado interval | HighGround/Sandstorm behavior and tornado interval 3 are implemented rules; the current terrain kind field is not a full configurable effect payload |
| Generated runtime identities and starting state | ID allocation, starting phase, zero initial soul records, empty input buffers, and mutable effect state belong to runtime construction |

No general reward table, random-event definition system, or arbitrary enemy-AI configuration is supplied by the current SO catalog. If the DB developer is also expected to introduce these, their fields need separate gameplay requirements; this inventory does not invent production schemas for unspecified features.

Sources: [stage startup plan][stage-plan], [stage job][stage-job], [continuation contract][continuation-interface], [production continuation fallback][continuation-unavailable], [development continuation][continuation-dev], [matchup matrix][matchups], [target policy][target-policy].

## 17. Replacement acceptance checklist

- [ ] Deliver every G01–G20, V01–V11, R01–R03, and A01–A03 field above through the chosen new provider or an explicitly assigned supporting asset/config provider. Existing SO connectivity does not satisfy this checkbox.
- [ ] Deliver N01/N02 entry and continuation sources, and record ownership decisions for all N03 policies.
- [ ] Support enumeration of maps, stages by chapter, units where validation requires it, and all artifact definitions; support typed key lookup for every reference.
- [ ] Preserve required singleton configuration records, enum semantics, coordinate/rotation conventions, numeric widths, null/absent meaning, and meaningful list order.
- [ ] Resolve all foreign keys, including shop-acquired owned units and their attack profiles, before installing a battle.
- [ ] Preserve separate run currency and per-battle stage gold lifetimes.
- [ ] Validate field bounds, role/owner eligibility, placements, capacity, weighted-spawn limits, and complete unit/method/trait combinations.
- [ ] Validate wall topology/public visibility/Terrain bindings/overlaps; preserve CornerCutTicks and both explicit occlusion enum meanings through preparation and state copies.
- [ ] Migrate every external piercing cap exactly into MaxTargetsPerRay, including zero; remove retired Cannon fields and verify no-wall preview/execution/AI parity.
- [ ] Validate artifact payloads through the real compiler contract, rather than accepting the whole serialized enum space.
- [ ] Provide resolvable Unity asset bindings for all migrated presentation/input/render records and preserve optional asset behavior.
- [ ] Add the environment input route, including the tornado stage relationship if that content is required.
- [ ] Verify new-run initialization, chapter generation, battle preparation, initial draw, shop entry/purchase, recovery entry/healing, artifact initialization/effects, and chapter continuation using replacement data.
- [ ] Keep generated runtime state and save-game persistence separate from static definition tables.

## 18. Source references

Paths point to the current repository implementation. Older handoffs describe earlier migrations and may have outdated statements about which tables exist; the source baseline above is the basis for this inventory.

[catalog]: ../../../Assets/ProjectL/Script/Framework/DataBase/DatabaseCatalogSO.cs
[ingame-factory]: ../../../Assets/ProjectL/Script/Systems/Factory/DBFactory/Group/InGameDataBaseFactory.cs
[view-factory]: ../../../Assets/ProjectL/Script/Systems/Factory/DBFactory/Group/ViewDataBaseFactory.cs
[player-loadout]: ../../../Assets/ProjectL/Script/Systems/DataBaseSystem/Data/InGame/Table/Loadout/Item/PlayerLoadoutData.cs
[run-start]: ../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/RunSystem/RunStartService.cs
[character-adapter]: ../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CharacterSystem/CharacterDefinitionAdapter.cs
[roster-service]: ../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CharacterSystem/CharacterRosterService.cs
[map-data]: ../../../Assets/ProjectL/Script/Systems/DataBaseSystem/Data/InGame/Table/Map/Item/StageMapData.cs
[stage-root]: ../../../Assets/ProjectL/Script/Systems/DataBaseSystem/Data/InGame/Table/Stage/Item/StageRootData.cs
[stage-generator]: ../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/StageSystem/StageGeneratorSystem.cs
[map-buffer]: ../../../Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/Map/MapProgressBuffer.cs
[combat-so]: ../../../Assets/ProjectL/Script/Systems/DataBaseSystem/Data/InGame/Table/Stage/Combat/CombatStageData.cs
[combat-data]: ../../../Assets/ProjectL/Script/Systems/DataBaseSystem/Data/InGame/Table/Stage/Combat/CombatStageDetail.cs
[field-data]: ../../../Assets/ProjectL/Script/Systems/DataBaseSystem/Data/InGame/Table/Field/Item/FieldData.cs
[battle-adapter]: ../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CombatSystem/BattleDefinitionAdapter.cs
[battle-preparation]: ../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/BattlePreparationCalculator.cs
[unit-so]: ../../../Assets/ProjectL/Script/Systems/DataBaseSystem/Data/InGame/Table/Unit/Item/UnitDataSO.cs
[unit-data]: ../../../Assets/ProjectL/Script/Systems/DataBaseSystem/Data/InGame/Table/Unit/Item/UnitData.cs
[trait-data]: ../../../Assets/ProjectL/Script/Systems/DataBaseSystem/Data/InGame/Table/Unit/Trait/UnitTraitData.cs
[targeting-data]: ../../../Assets/ProjectL/Script/Systems/DataBaseSystem/Data/InGame/Table/Targeting/Item/TargetingMethodData.cs
[category-data]: ../../../Assets/ProjectL/Script/Systems/DataBaseSystem/Data/InGame/Table/Category/Item/CategoryConfigData.cs
[structure-data]: ../../../Assets/ProjectL/Script/Systems/DataBaseSystem/Data/InGame/Table/Structure/Item/StructureData.cs
[roster-data]: ../../../Assets/ProjectL/Script/Systems/DataBaseSystem/Data/InGame/Table/Loadout/Item/UnitRosterData.cs
[enemy-loadout]: ../../../Assets/ProjectL/Script/Systems/DataBaseSystem/Data/InGame/Table/Loadout/Item/EnemyLoadoutData.cs
[economy-data]: ../../../Assets/ProjectL/Script/Systems/DataBaseSystem/Data/InGame/Table/Economy/Item/StageEconomyData.cs
[environment-adapter]: ../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CombatSystem/StageEnvironmentAdapter.cs
[obstacle-spec]: ../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Combat/InitialObstaclePlacementSpec.cs
[object-spec]: ../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Combat/InitialObjectPlacementSpec.cs
[tornado]: ../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Combat/TornadoSourceFact.cs
[shop-data]: ../../../Assets/ProjectL/Script/Systems/DataBaseSystem/Data/InGame/Table/Stage/Shop/ShopStageDetail.cs
[shop-service]: ../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/ShopSystem/CharacterShopService.cs
[recovery-stage]: ../../../Assets/ProjectL/Script/Systems/DataBaseSystem/Data/InGame/Table/Stage/Event/EventStageDetail.cs
[recovery-option]: ../../../Assets/ProjectL/Script/Systems/DataBaseSystem/Data/InGame/Table/Recovery/Item/RecoveryOptionData.cs
[recovery-adapter]: ../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/RecoverySystem/RecoveryDefinitionAdapter.cs
[artifact-so]: ../../../Assets/ProjectL/Script/Systems/DataBaseSystem/Data/InGame/Table/Artifact/Item/ArtifactDefinitionDataSO.cs
[artifact-effect]: ../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Common/ArtifactEffectData.cs
[artifact-compiler]: ../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Artifact/ArtifactEffectCompiler.cs
[artifact-board]: ../../../Assets/ProjectL/Script/Systems/DataBaseSystem/Data/InGame/Table/ArtifactBoard/Item/ArtifactBoardConfigData.cs
[artifact-service]: ../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/ArtifactSystem/ArtifactEditingService.cs
[progress-transactions]: ../../../Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/Core/ProgressTransactions.cs
[view-items]: ../../../Assets/ProjectL/Script/Systems/DataBaseSystem/Data/View/Table
[view-queries]: ../../../Assets/ProjectL/Script/Systems/DataBaseSystem/Queries/ViewDataBase/ViewTable
[render-items]: ../../../Assets/ProjectL/Script/Systems/DataBaseSystem/Data/Render/Table/Item
[unit-card-so]: ../../../Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/UISystem/Screens/Global/UnitCardVisualsSO.cs
[deck-images-so]: ../../../Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/UISystem/Screens/Global/DeckCharacterImagesSO.cs
[audio-catalog]: ../../../Assets/Audio/FMODplus/FmodEventCatalog.cs
[combat-state]: ../../../Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/Combat/CombatStateData.cs
[stage-plan]: ../../../Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Stage/Plans/StagePlan.cs
[stage-job]: ../../../Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Stage/Jobs/StageJob.cs
[continuation-interface]: ../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/StageSystem/IChapterContinuationSource.cs
[continuation-unavailable]: ../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/StageSystem/UnavailableChapterContinuationSource.cs
[continuation-dev]: ../../../Assets/ProjectL/Script/Systems/Factory/Runtime/Development/TemporaryChapterContinuationSource.cs
[matchups]: ../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Commands/CategoryMatchupSpec.cs
[target-policy]: ../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Domain/Combat/AttackTargetingPolicy.cs
