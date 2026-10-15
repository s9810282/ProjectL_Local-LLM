# Cell Environment Foundation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans for sequential execution, or superpowers:subagent-driven-development if the user selects that method. Steps use checkbox (`- [ ]`) syntax. Execute only authorized tasks. Commit or push only when explicitly requested; otherwise record each task's reviewable diff and evidence.

**Goal:** Build a usable Cell environment foundation: explicit semantic bindings, composed Cell queries, known coexistence checks, fixture-backed stage layout conversion, and correct initial presence contributions through the existing battle preparation boundary.

**Architecture:** Existing Combat and Character Buffers remain the only runtime owners. Immutable semantic bindings identify which existing sources represent Terrain/Object and which explicitly identified structures represent Obstacles; they never duplicate position, HP, or activity. Pure query/rule/calculation helpers compose those facts, and existing stage entry publishes the prepared result.

**Tech Stack:** Unity 6000.0.73f1, C#, NUnit EditMode, existing ProjectL.Core.Contracts/Domain/Application and ProjectL.Systems assemblies, existing Framework factories and Buffer prepare/install operations. No new package, service key, scheduler, or assembly is required.

**Spec:** [Cell Environment and Stage Integration Design](../specs/2026-10-05-cell-environment-terrain-obstacle-object-design.md).

**Date and status:** 2026-10-05. Implementation plan only; no task below has been executed. The user requested concrete planning for the system foundation. Detailed Object/Obstacle gameplay, production activation, and dynamic environment commands are not implied by this plan.

## Global Constraints

The following constraints apply to every task, including fixtures and handoff work.

- "Preserve the completed movement implementation, generic sequential environment-effect processing, route-cell ordering, intermediate state evaluation, damage/death settlement, return/checkpoint behavior, and existing tests."
- "The same-tile multiple-Object policy is unresolved and postponed."
- "This document does not establish a one-Object limit, later-Object overwrite, first-only activation, or Priority/instance-ID gameplay order."
- "A runtime cell is interpreted within the active battle and its existing `BattleInstanceId`."
- "Do not add StageKey or terrain behavior to `CellCoord`, or equate the same coordinate in different battles."
- "View/Flow remains the owner of combat Plan/Jobs; expose only the required calculation/result contract for later caller work."
- "No new generic event bus, scriptable trigger language, dynamic plugin registry, or transaction framework is a prerequisite."
- R04/R05/O07, Object + Object gameplay coexistence, insertion-order storage, stacked resolution, Q11, and stacked Q16 remain deferred historical requirements. No current task or acceptance test implements them.
- Object placement, direct/AoE immunity, approved-arrival activation, removal after activation, Terrain + Object allowance, and Obstacle + Object exclusion remain requirements. Unimplemented behavior must be reported as pending, not replaced with an inert production Object.
- This request writes only this plan. Its future file modifications are a proposed allowlist, not edits made now. Do not change the original analysis, design, completed movement documents, or historical movement plan.
- During foundation implementation, do not edit movement runtime files, movement tests, CharacterBuffer, Framework, production Flow/Jobs, asmdefs, assets, scenes, or generated csproj files. Narrow Combat Buffer metadata/entry-validation changes listed below are the proposed Buffer-owner work; no new Buffer is allowed.
- Documents are English; new code comments are Korean. Add Unity-generated unique `.meta` files for new C# files; preserve existing GUIDs.
- Keep the default DB source's T1/G7 rejection. An injected test source is not a production fallback.
- Preserve incoming changes. At planning time the audio catalog was already modified and the design document was untracked. Neither belongs to this plan's implementation diff.

## Review Focus

1. Valid source IDs with incorrect classification, dangling references, or stale copied metadata must fail entry before installation, while identical numeric values in different ID namespaces remain valid: P1, P4, P5.
2. A consumed/inactive source, a dead structure, or a hand Character must not reappear as an active cell participant; queries must read current facts without mutable caches: P1.
3. A new stage using the same field must load its own layout, and a malformed layout must not select the new stage, erase old shop/recovery state, or consume battle IDs: P3, P5.
4. Initial terrain contributions must preserve persistent/soul/temporary values, avoid free healing, and never silently clamp persistent main HP outside the preparation contract: P4, P5.
5. Preserve empty-environment behavior, generic source data/classification, movement previews, metadata copy paths, and cleanup. Supported unclassified destination modifiers intentionally gain initial presence application under spec section 7.2; P4 distinguishes this change from data compatibility. Fixture success is not production readiness: P2, P4, P5.

## 1 Deliverable and deliberate limits

This is the first implementation slice of spec stages A/B and the calculable part of C. It is a coherent foundation, not an attempt to implement the entire environment feature before its rules exist.

After P1–P5, an injected stage definition can supply an explicitly classified Terrain presence layout; Core validates its spatial facts, prepares appropriate initial Character contributions, and the existing Buffer entry operation installs the matching battle and stage. Cell queries can inspect current classified environment facts and structural/Character occupancy without owning state. Known coexistence rules are independently testable for all three semantic kinds.

The first concrete effect payload is the already-supported `DestinationTileModifierFact` capability: attack and maximum-health position contributions. Test values below are synthetic and are not assigned to high ground, tornado, ash, or any other named content. No production effect values or definition identities are invented.

The following remain separate future plans or owner handoffs:

| Capability | Why it is not an executable task here |
| --- | --- |
| Production Object activation, consumption/removal, cost non-refund | Q03 still needs Object-specific arrival-cause/activation mapping; Q04 still needs the Object non-refund requirement mapped to existing paid-cost, checkpoint, and attack-refund behavior. Q12 and the consumption/canonical-removal contract remain open. Completed movement rules are fixed inputs to this integration, not unresolved design work. |
| Production Obstacle authoring, faction/targeting, hit/destruction, knockback | Owner/targeting mapping and Q05–Q09/Q13/Q14 are unresolved; structure relocation publication is unsupported. |
| Timed Terrain and disappearance | Q01/Q02/Q15 and applicable non-Object Q16 are unresolved. |
| Dynamic environment command publication | Requires an approved trigger plus complete effect/event/health/outcome semantics. The pure reconciliation helper in P4 is preparation for that contract, not a runtime command. |
| Same-cell Terrain composition and multi-cell contact | Q17 remains open; preserve existing unsupported-combination behavior. |
| SO authoring UI, definition tables, visual catalog, assets/scenes | Q18–Q20 and DB/View contracts remain separate. P3 supplies a typed in-memory authoring seam, not a final content catalog. |
| Default DB entry and actual scene activation | Existing T1/G7 and View/Flow activation gates remain open. |

Do not add placeholder production rules returning no effects to make incomplete Objects appear supported. Spatial fixtures may classify existing test facts without registering them as executable content.

Decision basis: [spec sections 8.2–8.3](../specs/2026-10-05-cell-environment-terrain-obstacle-object-design.md#82-object-arrival-and-consumption) require cause-aware Object integration and removal review while retaining generic effect processing. The completed [movement ledger/checkpoint contract](../specs/2026-10-04-drag-path-movement-design.md#7-movement-ledger-and-checkpoint-return) and [route/effect settlement contract](../specs/2026-10-04-drag-path-movement-design.md#8-tile-effects-interruption-and-final-arrival) already determine route order, billing, settlement, checkpoint Return, and return barriers. Q03/Q04 do not reopen those decisions or authorize movement redesign. A demonstrated Object mapping gap needs its own later contract review, outside this foundation.

## 2 Verified baseline and file map

Rechecked during planning: `BattleInitialization.Environment` flows through `BattlePreparationCalculator` into `CombatPreparation` and `BattlePreparation.CommandState`. `CombatPreparation` is declared inside `BattlePreparation.cs`, not in a separate file. `CombatStateData` has both an initial constructor and a command-copy constructor. `BattlePreparationCalculator` currently creates initial Character facts without resolving their destination modifiers. `CharacterBuffer` requires a main Character's current HP to agree with persistent main HP. These facts determine the bounded work below.

Path aliases expand to repository-relative paths; they are not new directories to create indiscriminately:

- `Calc` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator`
- `Combat` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CombatSystem`
- `Buffers` = `Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems`
- `StageData` = `Assets/ProjectL/Script/Systems/DataBaseSystem/Data/InGame/Table/Stage/Combat`
- `CoreTests` = `Assets/Tests/EditMode`
- `SystemTests` = `Assets/Tests/EditMode/Framework`

| Task | Create | Modify existing files |
| --- | --- | --- |
| P1 | `Calc/Contracts/Combat/EnvironmentSemanticBindings.cs`; `Calc/Contracts/Combat/CellEnvironmentSnapshot.cs`; `Calc/Application/Combat/CellEnvironmentQuery.cs`; `CoreTests/CellEnvironmentQueryTests.cs`; `CoreTests/CellEnvironmentTestData.cs` | None. |
| P2 | `Calc/Domain/Placement/EnvironmentPlacementRules.cs`; `CoreTests/EnvironmentPlacementRuleTests.cs` | Add the violation enum in the new P1 snapshot file; extend the new P1 query facade. |
| P3 | `StageData/TerrainPresencePlacementData.cs`; `Combat/StageEnvironmentAdapter.cs`; `SystemTests/StageEnvironmentAdapterTests.cs` | `StageData/CombatStageDetail.cs`; `Combat/BattleDefinitionAdapter.cs`; `Calc/Contracts/Combat/BattleInitialization.cs`; extend the new P1 query with shared initial-environment validation. |
| P4 | `Calc/Application/Combat/EnvironmentPresenceReconciler.cs`; `Calc/Application/Combat/BattleEnvironmentPreparation.cs`; `CoreTests/BattleEnvironmentPreparationTests.cs` | `Calc/Application/Combat/BattlePreparationCalculator.cs`; `Calc/Contracts/Combat/BattlePreparation.cs` |
| P5 | `SystemTests/EnvironmentEntryPublicationTests.cs` | `Buffers/Combat/CombatStateData.cs`; `Buffers/Combat/BattlePreparationValidator.cs` |

Test-local setup belongs in the new test files. Read existing `BattlePreparationRuleTests`, `BattleEntryPublicationTests`, `BattleDefinitionAdapterTests`, and their fixture helpers for conventions; do not edit movement fixtures to accommodate the new tests. `CellEnvironmentTestData` is for the Core test assembly only. The Framework test assembly must not reference that test assembly; put its necessary small setup in its own fixtures.

No changes to `BufferSystemGroup`, `CombatProcessingService`, service registration, or the existing movement rule list are needed for this slice. Reuse those paths and verify them. If execution reveals a real need outside this file map, record the exact incompatible contract and obtain a plan adjustment before expanding the slice.

## 3 P1 Composed Cell query and semantic bindings

**Files:** Create the five P1 files in section 2.

**Consumes:** Existing `MovementEnvironmentFacts`, `CombatCommandState`, `CharacterCombatFacts`, `StructureCombatFacts`, `CellCoord`, and `EntityId`.

**Produces:** The following exact proposed Contracts API. Namespace: existing `...Contracts.Combat`. These are signatures, not implementation bodies.

```csharp
public sealed class EnvironmentSemanticBindings
{
    public static EnvironmentSemanticBindings Empty { get; }
    public IReadOnlyList<long> TerrainSourceIds { get; }
    public IReadOnlyList<long> ObjectSourceIds { get; }
    public IReadOnlyList<EntityId> ObstacleEntityIds { get; }
    public EnvironmentSemanticBindings(IReadOnlyList<long> terrainSourceIds,
        IReadOnlyList<long> objectSourceIds, IReadOnlyList<EntityId> obstacleEntityIds);
    public void ValidateInitialReferences(MovementEnvironmentFacts environment,
        IReadOnlyList<StructureCombatFacts> structures);
}

public sealed class CellEnvironmentSnapshot
{
    public CellCoord Cell { get; }
    public bool IsInside { get; }
    public IReadOnlyList<long> TerrainSourceIds { get; }
    public IReadOnlyList<long> ObjectSourceIds { get; }
    public IReadOnlyList<EntityId> ObstacleIds { get; }
    public IReadOnlyList<EntityId> BlockingStructureIds { get; }
    public IReadOnlyList<EntityId> CharacterIds { get; }
    public CellEnvironmentSnapshot(CellCoord cell, bool isInside,
        IReadOnlyList<long> terrainSourceIds, IReadOnlyList<long> objectSourceIds,
        IReadOnlyList<EntityId> obstacleIds, IReadOnlyList<EntityId> blockingStructureIds,
        IReadOnlyList<EntityId> characterIds);
}
```

Public Application facade, namespace `...Application.Combat`:

```csharp
public static class CellEnvironmentQuery
{
    public static CellEnvironmentSnapshot Read(CombatCommandState state,
        IReadOnlyList<CharacterCombatFacts> characters,
        EnvironmentSemanticBindings bindings, CellCoord cell);
    internal static CellEnvironmentSnapshot ReadSpatial(int boardWidth, int boardHeight,
        MovementEnvironmentFacts environment,
        IReadOnlyList<StructureCombatFacts> structures,
        IReadOnlyList<CharacterCombatFacts> characters,
        EnvironmentSemanticBindings bindings, CellCoord cell);
}
```

`Read` validates its state argument and forwards the state's dimensions, Environment, and Structures together with the supplied characters/bindings/cell to `ReadSpatial`. That internal method owns the single spatial composition calculation and its shared input checks. P4 calls it directly with preparation facts, then passes its snapshot to P2 `CheckPlacement`. It requires no timeline, thorns, deployment phase, turn progress, or synthetic `CombatCommandState`. This follows [spec section 6.2](../specs/2026-10-05-cell-environment-terrain-obstacle-object-design.md#62-a-cell-is-a-composed-read-view): a composed read view from required facts. Keep both entry points in the same new helper; add no service, registry, context hierarchy, or second query algorithm.

Bindings contain only immutable semantic classification, not coordinates, HP, active flags, ordering, or cached query results. Source IDs and EntityIds are distinct namespaces. One source ID cannot be classified as both Terrain and Object; that is an identity error, not a restriction on two distinct sources sharing a cell. Copy constructor inputs and expose read-only collections. Require positive source IDs, nonempty entity IDs, no duplicates within a list, and no null lists/items where applicable.

`ValidateInitialReferences` checks that every classified source exists in the initial environment and every classified obstacle references an existing structure. It does not decide target eligibility, run effects, require classification of legacy sources, or infer roles from types/names/Owner/IsMine. It is called on preparation only: immutable semantic bindings may outlive a consumed source during a battle. Queries resolve current presence from current source facts rather than treating a missing runtime source as present.

`Read` reports active classified source IDs at the requested cell, living field Characters, and living blocking structures. ObstacleIds are the explicitly bound subset of those structures. Unclassified generic sources remain unclassified, and an ordinary HQ is not automatically an Obstacle. Return an outside snapshot with empty lists for an out-of-bounds coordinate. Null top-level arguments and duplicate Character/structure identities in malformed query inputs are errors. Result ordering is unspecified and must never drive gameplay.

The snapshot constructor copies its lists, validates IDs and duplicates, requires ObstacleIds to be a subset of BlockingStructureIds, and requires empty participant lists when IsInside is false. It does not reject prohibited coexistence combinations itself: P2 must be able to inspect and explain a candidate state before it can be published.

- [ ] **P1.1 Add failing tests.** In `CellEnvironmentQueryTests`, add `Query_ComposesCurrentFactsWithoutOwningThem`, `Query_OmitsInactiveDeadAndHandParticipants`, `Bindings_RejectMalformedIdentityAndMissingInitialReferences`, `BindingsAndSnapshots_DefensivelyCopyInputCollections`, `Query_DifferentBattlesDoNotShareCellState`, `Query_OutsideBoardIsEmpty`, and `Query_LegacySourcesAndHeadquartersAreNotReclassified`. Add `Query_StateFacadeMatchesNarrowSpatialComposition` for identical dimensions/environment/structures/characters/bindings, including outside and malformed shared inputs. Use a 4-by-5 board and cell (1,1); terrain source 11, Object source 12, Character EntityId 11, structure EntityId 21. Mutate caller-owned arrays after construction and assert retained IDs are unchanged. Query two independent states at the same coordinate with different source IDs and assert no cross-state result leakage. Separate structurally inconsistent overlap arrangements into raw pure-query fixtures; do not publish them as valid battles.
- [ ] **P1.2 Run filter P1 and record RED.** First missing-symbol compile failure is acceptable; after declarations exist, require a behavioral failure showing stale/inactive participants or incorrect classification.
- [ ] **P1.3 Implement immutable bindings and snapshot.** Input collection mutation after construction must not alter the stored identities. `ValidateInitialReferences` is reference-integrity validation only.
- [ ] **P1.4 Implement `ReadSpatial` and the forwarding `Read` facade.** Scan supplied facts once; do not cache by coordinate or add a global Cell registry. Query the same bindings against two environment snapshots where source 12 is first active and then inactive; the second result must omit 12. Preparation must not fabricate unrelated combat state to use this calculation.
- [ ] **P1.5 Run filter P1 and record GREEN.** Verify structural ID 11 and source ID 11 can coexist in their different namespaces. Verify no input is changed. Save the isolated diff and evidence; commit only if requested.

Representative assertions after arranging those fixtures:

```csharp
Assert.That(first.TerrainSourceIds, Is.EquivalentTo(new long[] { 11 }));
Assert.That(first.ObjectSourceIds, Is.EquivalentTo(new long[] { 12 }));
Assert.That(first.CharacterIds, Is.EquivalentTo(new[] { new EntityId(11) }));
Assert.That(second.ObjectSourceIds, Is.Empty);
Assert.That(outside.IsInside, Is.False);
Assert.That(outside.BlockingStructureIds, Is.Empty);
```

P1 does not approve Object cardinality, define stacking order, or create a View-to-Core polling API. Its facade is for Systems/Application composition; View still consumes Buffer state/results.

## 4 P2 Known coexistence rules with no replacement policies

**Files:** Create `Calc/Domain/Placement/EnvironmentPlacementRules.cs` and `CoreTests/EnvironmentPlacementRuleTests.cs`. Add the enum to `Calc/Contracts/Combat/CellEnvironmentSnapshot.cs` and the facade method below to the new P1 query file.

**Consumes:** P1 snapshot and query contracts.

**Produces:** Contracts flags and internal Domain rule:

```csharp
[Flags]
public enum EnvironmentPlacementViolation
{
    None = 0,
    OutsideBoard = 1,
    TerrainWithObstacle = 2,
    ObstacleWithObject = 4,
    OverlappingObstacles = 8
}
internal static class EnvironmentPlacementRules
{
    internal static EnvironmentPlacementViolation Evaluate(CellEnvironmentSnapshot cell);
}
```

Add `public static EnvironmentPlacementViolation CheckPlacement(CellEnvironmentSnapshot cell)` to `CellEnvironmentQuery`. It delegates to Domain; Systems must not gain a Domain assembly reference.

`None` means no violation of this specified subset. It does not certify movement legality, deployment zones, damage eligibility, executable content, or deferred multiplicity. Do not add a Boolean named `CanPlaceAnything` around it. Character blocking remains in existing occupancy validation. A non-Obstacle structure remains a blocker in the query without gaining PPT obstacle semantics.

- [ ] **P2.1 Add failing table-driven cases.** `KnownCoexistence_ReportsAllApplicableViolations` covers Terrain + Object = None; Terrain + Obstacle = TerrainWithObstacle; Obstacle + Object = ObstacleWithObject; two Obstacle IDs = OverlappingObstacles; all three kinds with two obstacles = flags 2 | 4 | 8. `OrdinaryFloorAndCharacterDoNotBecomeTerrainOrObstacle` covers empty semantic layers and a Character. `OutsideCell_ReportsOutsideBoard` covers the outside snapshot.
- [ ] **P2.2 Run filter P2 and record RED.** No Object + Object gameplay test or source-order test is added.
- [ ] **P2.3 Implement the four predicates and Application facade.** Combine flags without selecting which conflicting element to delete, move, overwrite, or activate. Inputs are the complete candidate state for a cell, so reversing placement order cannot bypass a constraint.
- [ ] **P2.4 Run filter P2 and P1.** Expected GREEN, including symmetry of the same final facts supplied in different collection orders. Do not assert Object activation order. Record the diff and evidence; commit only if requested.

```csharp
Assert.That(CellEnvironmentQuery.CheckPlacement(terrainAndObject),
    Is.EqualTo(EnvironmentPlacementViolation.None));
Assert.That(CellEnvironmentQuery.CheckPlacement(allConflicts),
    Is.EqualTo(EnvironmentPlacementViolation.TerrainWithObstacle |
        EnvironmentPlacementViolation.ObstacleWithObject |
        EnvironmentPlacementViolation.OverlappingObstacles));
```

These predicates are reusable by future approved placement/relocation paths. This task does not claim those callers or their publication contracts are implemented.

## 5 P3 Typed stage layout conversion for the supported presence effect

**Files:** Create `StageData/TerrainPresencePlacementData.cs`, `Combat/StageEnvironmentAdapter.cs`, and `SystemTests/StageEnvironmentAdapterTests.cs`. Modify `StageData/CombatStageDetail.cs`, `Combat/BattleDefinitionAdapter.cs`, and `Calc/Contracts/Combat/BattleInitialization.cs`; extend the new P1 `CellEnvironmentQuery.cs` with the focused validation entry below.

**Consumes:** P1 bindings; existing `DestinationTileModifierFact`, `MovementEnvironmentFacts`, explicit stage/field DB conversion, and `BattleInitialization`.

**Produces:** Two immutable DB value types in the existing StageData namespace. Keeping these small associated values in one file is intentional; they are not a new registry.

```csharp
public sealed class TerrainPresenceDefinitionData
{
    public int AttackBonus { get; }
    public int MaxHealthBonus { get; }
    public TerrainPresenceDefinitionData(int attackBonus, int maxHealthBonus);
}
public sealed class TerrainPresencePlacementData
{
    public long SourceId { get; }
    public CellCoord Cell { get; }
    public TerrainPresenceDefinitionData Definition { get; }
    public IReadOnlyList<Owner> KnownTo { get; }
    public TerrainPresencePlacementData(long sourceId, CellCoord cell,
        TerrainPresenceDefinitionData definition, IReadOnlyList<Owner> knownTo);
}
```

Add get-only `IReadOnlyList<TerrainPresencePlacementData> TerrainPresencePlacements` to `CombatStageDetail`, with an optional final constructor argument `IReadOnlyList<TerrainPresencePlacementData> terrainPresencePlacements = null`. Normalize omitted/null collection to empty for legacy callers; reject null entries and duplicate SourceIds. Copy the input. Negative/outside coordinates are checked against field dimensions by the adapter, not guessed from stage-map node dimensions. Definition is required; KnownTo is explicit, copied, and follows the existing distinct Player/Enemy visibility contract, including an empty list. Bonuses are integers, with representability validated when applied; do not invent a gameplay sign restriction.

Add get-only `EnvironmentSemanticBindings EnvironmentBindings` to `BattleInitialization`, with optional final constructor argument `EnvironmentSemanticBindings environmentBindings = null`, defaulting to Empty. Do not rename/remove `Environment` or change existing positional argument meanings.

The Systems adapter signature is:

```csharp
internal static class StageEnvironmentAdapter
{
    internal static (MovementEnvironmentFacts Environment,
        EnvironmentSemanticBindings Bindings) Read(int boardWidth, int boardHeight,
        IReadOnlyList<TerrainPresencePlacementData> placements);
}
```

For each placement, emit an active `DestinationTileModifierFact` with exactly the explicit SourceId, Cell, KnownTo, bonuses, and existing default Priority 0. Bind that ID as Terrain; ObjectSourceIds and ObstacleEntityIds are empty. Stable authored SourceIds are not generated from array order and carry no gameplay order meaning. Use constructor validation and checked calculations already required by existing contracts.

Add this focused Application entry to the new P1 query helper so the Systems adapter and P4 preparation share the same capability check without a Systems-to-Domain reference:

```csharp
public static void ValidateInitialEnvironment(int boardWidth, int boardHeight,
    MovementEnvironmentFacts environment);
```

Validate positive dimensions, non-null environment, and every source's bounds. For every distinct cell containing an active `DestinationTileModifierFact`, call the existing `PositionTileModifierCalculator.ResolveAt(cell, environment)` to validate supported composition. Its existing rejection of multiple active destination modifiers is reused, not reimplemented as a new Terrain-count predicate. Check occupied and unoccupied cells alike, regardless of semantic binding or visibility; inactive destination modifiers do not contribute. This pure check requires neither Characters nor a full combat state, does not apply effects, and never filters or rewrites sources. `StageEnvironmentAdapter.Read` calls it on the converted environment; `BattleEnvironmentPreparation.Prepare` independently calls it on every supplied initialization, including direct callers that bypass the adapter. Character-dependent representability and health checks remain in P4. This is the current effect-capability boundary under [spec sections 6.3](../specs/2026-10-05-cell-environment-terrain-obstacle-object-design.md#63-spatial-invariants) and [8.1](../specs/2026-10-05-cell-environment-terrain-obstacle-object-design.md#81-terrain-presence-and-timing), not a permanent Terrain cardinality policy or an Object-stacking decision. Buffer does not duplicate this calculation.

Call this adapter in `BattleDefinitionAdapter.Read` once field dimensions are available, and pass both outputs to the returned initialization. No new source fallback, reflection, runtime type discovery, or production rule registration is required: this effect is already handled by the position-modifier calculator.

- [ ] **P3.1 Add failing tests.** `Layout_ConvertsExplicitIdentityValuesAndVisibility` uses a 4-by-5 field, source 11 at (1,0), synthetic bonuses +2 attack/+3 max HP, KnownTo = Player. `Layout_RejectsBoundsDuplicatesAndUnsupportedComposition` covers (-1,0), (4,0), duplicate source IDs, and two destination modifiers on one cell. `Layout_DefinitionReuseDoesNotCouplePlacementState` reuses the same immutable definition at two different cells with distinct IDs. `Adapter_UsesRequestedStagesLayoutOnSharedField` uses stages 1101 and 1102 with different layouts and the same FieldId. `LegacyStage_ProducesEmptyEnvironment` covers omitted data.
- [ ] **P3.2 Run filter P3 and record RED.** Use a new test-local DB fixture modeled on `BattleDefinitionAdapterTests`; do not change its production-source rejection expectations.
- [ ] **P3.3 Implement the DB values, optional stage composition, and bindings parameter.** Do not modify `CombatStageData.cs`, serialized SO schemas, assets, definition-key enums, or query tables in this foundation slice. The typed in-memory path is deliberate; the real authoring path remains gate D.
- [ ] **P3.4 Implement the shared validation and adapter, then connect the adapter to the existing explicit-target conversion.** Preserve every other initialization field and existing exception behavior. Pass no new Object/Obstacle runtime content. P4 must also invoke the shared check; adapter validation alone cannot protect directly constructed initialization.
- [ ] **P3.5 Run filter P3 and existing `BattleDefinitionAdapterTests`.** Expect exact source/visibility preservation, unchanged empty-stage behavior, and the default source still rejecting T1/G7. Record evidence and the diff; commit only if requested.

```csharp
var converted = StageEnvironmentAdapter.Read(4, 5, placements);
var tile = (DestinationTileModifierFact)converted.Environment.Tiles.Single();
Assert.That(tile.Id, Is.EqualTo(11));
Assert.That(tile.Cell, Is.EqualTo(new CellCoord(1, 0)));
Assert.That(tile.AttackBonus, Is.EqualTo(2));
Assert.That(tile.MaxHealthBonus, Is.EqualTo(3));
Assert.That(converted.Bindings.TerrainSourceIds, Is.EquivalentTo(new long[] { 11 }));
Assert.That(converted.Bindings.ObjectSourceIds, Is.Empty);
```

This payload is a bounded authoring seam for an existing effect, not the final Terrain definition catalog. Real content names/IDs, SO assets, and visual definitions are added only under a later content contract. Do not claim that a configured production scene can author this data yet.

## 6 P4 Preparation validation and pure contribution reconciliation

**Files:** Create `Calc/Application/Combat/EnvironmentPresenceReconciler.cs`, `Calc/Application/Combat/BattleEnvironmentPreparation.cs`, and `CoreTests/BattleEnvironmentPreparationTests.cs`. Modify `Calc/Application/Combat/BattlePreparationCalculator.cs` and `Calc/Contracts/Combat/BattlePreparation.cs`.

**Consumes:** P1–P3 contracts; existing `PositionTileModifierCalculator.ResolveAt/Replace`, `CharacterChanges`, `CharacterStateChange`, `MainCharacterPersistentChange`, and `MainCharacterPersistentState`.

**Produces:** Internal Application helpers with these exact signatures:

```csharp
internal static class EnvironmentPresenceReconciler
{
    internal static CharacterChanges Calculate(
        IReadOnlyList<CharacterCombatFacts> characters,
        MovementEnvironmentFacts finalEnvironment,
        IReadOnlyList<CellCoord> affectedCells,
        MainCharacterPersistentState main);
}
internal static class BattleEnvironmentPreparation
{
    internal static IReadOnlyList<CharacterCombatFacts> Prepare(
        BattleInitialization initialization,
        IReadOnlyList<CharacterCombatFacts> characters,
        IReadOnlyList<StructureCombatFacts> structures,
        MainCharacterPersistentState main);
}
```

The reconciler evaluates living field Characters on affected cells, deduplicating by EntityId/cell. Return changes only when position contribution or resulting health/stats actually differ. Use existing ResolveAt/Replace; preserve all unrelated values, identity, occupancy, movement ledger, and soul/temporary contributions. Return empty Souls and Created. If the main's current HP changes, include a `MainCharacterPersistentChange` using the existing `CharacterSettlementCalculator.WithHealth`; otherwise Main is null. No Buffer writes, events, costs, triggers, clock changes, source mutation, or fake move commands occur here.

Input duplicates/null values are errors; duplicated affected cells are harmless. Empty/unaffected inputs return an empty change set. Affected multi-cell Characters with applicable position effects remain unsupported rather than receiving an invented contact rule; legacy multi-cell preparation without such effects is unchanged. Invalid resulting maximum HP and integer overflow propagate before publication.

`BattleEnvironmentPreparation.Prepare` first calls P3 `ValidateInitialEnvironment` on the complete initialization environment and validates binding references. This occurs for adapter-produced and directly constructed initialization alike, before contribution reconciliation or installation. It rejects unsupported active destination-modifier combinations even on empty cells. For known coexistence checks, enumerate the distinct cells represented by environment sources and structure occupancy, use P1 `ReadSpatial` with the actual dimensions/environment/structures/characters/bindings, and pass each snapshot to P2 `CheckPlacement`; reject any reported violation. Never construct a temporary combat state for these checks. Require all newly bound Terrain source IDs in this slice to refer to `DestinationTileModifierFact`; other Terrain capabilities are unsupported. Nonempty initial ObjectSourceIds or ObstacleEntityIds are rejected as **unsupported executable content in this slice**, not as an illegal Object cardinality or a gameplay ban. Their pure spatial representation/tests remain useful for future integration.

Compatibility has three distinct meanings:

- **Semantic classification:** an unbound generic source remains unclassified and does not appear as Terrain or Object in a semantic query. This is already decided by [spec section 6.1](../specs/2026-10-05-cell-environment-terrain-obstacle-object-design.md#61-canonical-facts).
- **Source data:** preserve every supplied generic source's ID, cell, concrete type/rule, activity, visibility, priority, and payload. Do not tag, filter, replace, consume, or reorder sources merely to prepare Character contributions.
- **Initial effect application:** resolve presence from the full supplied environment, including active unclassified `DestinationTileModifierFact` sources on initially occupied cells. Lack of a Terrain binding is not an effect opt-out. [Spec section 7.2](../specs/2026-10-05-cell-environment-terrain-obstacle-object-design.md#72-reconcile-characters-when-terrain-changes-beneath-them) requires initial placement and actual relocation to produce the same contribution for the same approved state; [section 8.1](../specs/2026-10-05-cell-environment-terrain-obstacle-object-design.md#81-terrain-presence-and-timing) selects the existing position-modifier capability. This intentionally changes previous preparation, which retained the environment without applying its initial destination contribution. Other generic entry rules are retained as data and are not executed by presence preparation. No claim of complete previous preparation behavior compatibility is made.

Call the reconciler for initially occupied cells after ordinary Character/structure placement and hand draw calculation. Hand Characters are unchanged. Current battle preparation installs Character facts but cannot alter persistent main HP. Therefore if reconciliation returns a non-null Main change, fail preparation with `NotSupportedException` before publication. This gate applies equally to classified Terrain and unclassified destination modifiers; neither dropping an unbound effect nor excluding it from reconciliation is a valid compatibility workaround. Do not mutate persistent health, pretend clamping did not happen, or weaken CharacterBuffer validation. Max-health increases and decreases that do not change current main HP remain supported. Supporting health-changing entry later needs a separate narrow preparation/settlement contract; this is a technical boundary, not a Terrain gameplay rule.

Add get-only `EnvironmentSemanticBindings EnvironmentBindings` to `CombatPreparation` in `BattlePreparation.cs`, with optional final constructor parameter `EnvironmentSemanticBindings environmentBindings = null` defaulting to Empty. Pass `initialization.EnvironmentBindings` from `BattlePreparationCalculator`. Do not change `CombatCommandState` or movement copy paths. `BattlePreparationCalculator` replaces its calculated Character list with the helper's result before constructing `BattlePreparation`; keep definition, ID allocation, draw order, placement, and turn construction unchanged.

- [ ] **P4.1 Add failing initial-state tests.** `Preparation_AppliesPresenceWithoutHealingOrChangingPersistentFacts`: main base attack 7, persistent max HP 12, current HP 4, souls 3, synthetic tile +2/+3. Expect position attack +2, effective attack 12, max HP 18, current HP 4; persistent max/attack stay 12/7 and soul count stays 3. Parameterize the destination source as Terrain-bound or unclassified: both apply identical contributions, while only the bound case appears in the Terrain query and source `SameValues` remains true in both. `Preparation_PreservesEmptyLegacyAndHandStates` covers empty environment, unchanged hand facts, inactive destination sources, and retained generic entry sources without triggering them. Extend `Preparation_RejectsMalformedOrUnsupportedEnvironmentBeforeReturning` with a directly constructed initialization containing two distinct active destination modifiers at an unoccupied in-bounds cell, with no adapter involved; both Empty bindings and Terrain bindings must reject with the existing unsupported-composition cause. Keep dangling bindings, out-of-bounds sources, unsupported bound kinds, and occupied-cell composition cases. A single active plus an inactive destination source at the empty cell must not fail the active-composition check.
- [ ] **P4.2 Add failing reconciliation tests.** `StationaryCharacter_RemovesOnlyPositionContribution` uses prior max 18/current 17 and position +2/+3 over the same base/souls, then finalEnvironment = Empty at its cell: expect max/current 15/15, position attack 0, effective attack 10, and Main final HP 15. Inputs and soul/temporary values remain unchanged. This is pure calculation evidence, not enabled Terrain disappearance. `Reconcile_IsIdempotentAndDeduplicatesAffectedCells` applies the proposed final Character/main facts and expects an empty second change set.
- [ ] **P4.3 Add the entry-contract edge test.** `Preparation_RejectsMainHealthChangingClamp`: persistent max/current 12/12, zero souls, destination max bonus -3. Existing Replace would produce 9/9; preparation must throw before installing anything. `Preparation_AllowsRepresentableMaxReductionWithoutClamp`: current HP 4 with the same max bonus yields max/current 9/4. Run both cases with Terrain-bound and unclassified destination sources; the gate depends on the resulting persistent HP change, not classification. Add overflow and resulting-max-below-one cases; require their intended exception causes, not any exception.
- [ ] **P4.4 Run filter P4 and record RED.** Keep the pure reconciliation tests separate from published entry tests.
- [ ] **P4.5 Implement the two helpers and preparation wiring.** Invoke P3 shared environment validation before per-Character reconciliation, use P1's narrow spatial calculation for P2 checks, and reuse existing stat/health normalization functions over the complete environment. Do not change `PositionTileModifierCalculator`, movement facts/rules/calculators, or CharacterBuffer. Keep the runtime dynamic-change caller absent until its full contract is approved.
- [ ] **P4.6 Run filter P4 and existing `BattlePreparationRuleTests`.** Expect GREEN and unchanged IDs/draw/deployment behavior. Record the implementation diff and evidence; commit only if requested.

```csharp
Assert.That(preparedMain.Stats.PositionAttackBonus, Is.EqualTo(2));
Assert.That(preparedMain.Stats.EffectiveAttackPower, Is.EqualTo(12));
Assert.That(preparedMain.Health.MaxHealth, Is.EqualTo(18));
Assert.That(preparedMain.Health.CurrentHealth, Is.EqualTo(4));
Assert.That(removal.Main.Final.CurrentHealth, Is.EqualTo(15));
Assert.That(secondPass.Characters, Is.Empty);
Assert.That(secondPass.Main, Is.Null);
```

## 7 P5 Existing Buffer publication and entry integration evidence

**Files:** Create `SystemTests/EnvironmentEntryPublicationTests.cs`. Modify `Buffers/Combat/CombatStateData.cs` and `Buffers/Combat/BattlePreparationValidator.cs` only for the metadata/integrity work below.

**Consumes:** P1 bindings, P3 initialization, P4 prepared values, existing real `IBattleBufferWriteSink.PrepareBattle(preparation, selection)` and registered `ICombatProcessingService.EnterBattle`.

**Produces:** `public EnvironmentSemanticBindings EnvironmentBindings { get; }` on `CombatStateData`. Set it from `battle.Combat.EnvironmentBindings` in the initial constructor and preserve the same immutable value from `source.EnvironmentBindings` in the command-copy constructor. EndBattle removes it with Current; no second field store or separate Clear call exists.

In `BattlePreparationValidator.Validate`, call `preparation.Combat.EnvironmentBindings.ValidateInitialReferences(preparation.CommandState.Environment, preparation.Structures)` before any installation. This protects reference integrity for direct low-level publication. It does not recompute bonuses or run Core placement/effect rules. The real entry path remains responsible for semantic eligibility and calculating consistent facts. No new Buffer interface/service or two-phase writer is added: the existing group already prepares all stores before installing them.

- [ ] **P5.1 Add failing real-store tests.** `Entry_InstallsSelectedLayoutBindingsAndPreparedContributions` installs chapter 1/node 3/stage 1102 using the existing explicit-target entry and an injected definition source. Previous node 2/stage 1101 has a different layout with the same dimensions; record which target the source receives. Assert Current selection, source cell/ID/values, bindings, Character contribution, persistent main values, and souls together.
- [ ] **P5.2 Add preservation tests.** Extend `Entry_InvalidEnvironmentPreservesAllStores` beyond out-of-bounds sources and the P4 main-HP-changing clamp: inject a definition source returning a directly constructed `BattleInitialization` with two distinct active destination modifiers on an unoccupied in-bounds cell. Bypass `StageEnvironmentAdapter` but use the registered real entry/preparation path. Cover both Empty and Terrain bindings; require the unsupported-composition rejection before installation. Exercise the clamp with unclassified as well as classified modifiers. `Publication_DanglingBindingRejectsBeforeAnyInstall` constructs a valid-shaped low-level preparation with a binding to missing source 999 to exercise the late Buffer integrity guard. For each rejection, capture Current selection/shop or recovery, Character facts/owned roster/souls/main, Combat result/preview/counters/completion, Currency, Artifact, and next observable battle/entity IDs. Compare unchanged snapshots after failure, then compare a valid retry's allocation with the expected next IDs; do not use only HasBattle as proof. Keep capability/effect validation in Core preparation, not Buffer.
- [ ] **P5.3 Add lifecycle/compatibility tests.** `Metadata_SurvivesAcceptedCommandWithoutDuplicatingRuntimeState` sends an existing valid non-movement command, verifies bindings survive the command-copy constructor, and queries the latest canonical Environment. `EndBattle_ClearsLocalEnvironmentPreservingRunState` uses the existing legal completion/cleanup fixture sequence. `DefaultSource_StillReportsT1G7` retains the existing DB-readiness limitation. Reuse real factories and service binding, not a new fake Buffer implementation.
- [ ] **P5.3a Add preparation-to-movement integration.** In the new `EnvironmentEntryPublicationTests` fixture, add `InitialPresence_MoveAndReturnReconcilesOnlyPositionContribution` using the real preparation and existing movement processing. Follow the concrete scenario below; do not edit existing movement fixtures or runtime files.
- [ ] **P5.4 Run filter P5 and record RED.** Metadata loss on the copy path and partial-publication assertions must fail for the expected missing behavior.
- [ ] **P5.5 Implement the two narrow Buffer changes.** Do not put placement/gameplay calculations in Buffer. Do not update Framework, CharacterBuffer, execution Jobs, input/result acknowledgements, or any existing movement code/test to make the new fixtures pass.
- [ ] **P5.6 Run P5 plus the affected entry/preparation regressions, then the three test assemblies once.** Compare failures with a fresh baseline by name/message/cause; report unrelated baseline failures explicitly rather than assuming historical counts. Verify default production entry remains gated and all protected file hashes are unchanged. Record final diff/evidence; commit only if requested.

```csharp
Assert.That(buffers.CurrentProgressReadSink.CurrentStage.StageKey, Is.EqualTo(1102));
Assert.That(buffers.CombatStateReadSink.Current.EnvironmentBindings.TerrainSourceIds,
    Is.EquivalentTo(new long[] { 11 }));
Assert.That(buffers.CombatStateReadSink.Current.Environment.Tiles.Single().Cell,
    Is.EqualTo(new CellCoord(1, 0)));
Assert.That(buffers.CharacterReadSink.Characters.Single(c => c.OwnedId == mainId)
    .ToFacts().Stats.PositionAttackBonus, Is.EqualTo(2));
```

P5.3a scenario and expected observations:

1. Through the injected stage definition and registered `EnterBattle`, prepare a living player main at (1,0) on a 4-by-5 board with Terrain source 11, +2 attack/+3 max HP, visible to Player. Use persistent attack 7, max HP 12, current HP 4, and souls 3; no artifact/temporary modifiers or entry hazards. The ordinary neighboring cell (2,0) is unoccupied and safe. Use the existing turn-start/advance path until this Character can act; provide a sufficient AP/range budget and a positive MoveCost through the fixture definition. Do not overwrite the published environment or patch the Character to mimic preparation.
2. Assert initial effective attack 12, position attack +2, max/current HP 18/4, and position source 11. Persistent attack/max/current HP remain 7/12/4 and souls remain 3: initial presence grants no healing. Capture AP/range, the initial checkpoint, committed/pending distance and paid-cost ledgers, attack-refund eligibility, all unrelated contribution fields, and immutable semantic bindings after normal turn start. Require no preexisting pending move or barrier in this setup.
3. Set a normal move preview for the one-edge path from (1,0) to (2,0), call `ProcessLatestPreview`, retrieve its `MovementContext`, submit `CombatViewCommandData.Move`, and call `ProcessNextCommand`. Follow existing result/preview acknowledgement conventions. Require acceptance and no return barrier. Assert effective attack 10, position attack 0, max/current HP 15/4, and no position-source IDs; only the position contribution and normal movement position/resources/ledger change. Source 11 remains active at (1,0), and bindings are preserved.
4. Verify the existing one-edge billing: AP decreases by the recorded MoveCost, pending distance increases by one, pending paid cost and attack-refund eligibility increase by that paid amount, available movement decreases by one, and the checkpoint/committed distance remain unchanged. These are assertions of the completed movement contract, not new billing logic.
5. Use `CombatViewPreviewData.ReturnToCheckpoint`, process the preview, then submit `CombatViewCommandData.ReturnToCheckpoint` with its returned context and process the command. Require allowed, accepted Return to (1,0). Assert position source 11, effective attack 12, position attack +2, max/current HP 18/4; the recorded pending AP cost is refunded, pending distance/cost are cleared, movement allowance is restored, and attack-refund eligibility returns to its captured value. Persistent main values, soul/other unrelated bonuses, environment source facts, and semantic bindings match the captured values across both commands. This recomputes presence without healing or restoring an HP snapshot.

The low current HP avoids clamping on departure and the pure presence source does not trigger an entry effect or return barrier. This scenario exercises the new initial-preparation seam through completed movement; the existing non-movement metadata test and unchanged movement regressions remain separate evidence.

New runtime source creation/removal/relocation and mutable classification are intentionally absent. A future feature must validate its final-state coexistence within its complete command boundary; passing these initialization tests does not prove dynamic publication safety.

## 8 Verification commands and evidence

These commands are for later authorized implementation. They were not executed while writing this plan. Unity 6000.0.73f1 is recorded in `ProjectSettings/ProjectVersion.txt`, and the executable below exists in this environment. Recheck the executable when execution begins.

Before P1, record branch/HEAD/status, incoming diffs, and hashes of protected movement sources/tests/documents plus unrelated modified files. Capture a fresh three-assembly baseline once. Use one Editor/test process per checkout. If the user's Editor already owns the checkout, use its established test route; do not terminate it or open another Editor on the same project. Keep evidence outside Assets.

For a batch run when the checkout is available:

```powershell
$cellRoot = (Get-Location).Path
$cellEvidence = Join-Path $cellRoot '.superpowers/sdd/2026-10-05-cell-environment-foundation'
New-Item -ItemType Directory -Force -Path $cellEvidence | Out-Null
$cellRun = 'p1-red'
$cellFilter = 'ProjectL.Tests.Core.CellEnvironmentQueryTests'
$cellArgs = @('-batchmode', '-nographics', '-projectPath', ('"' + $cellRoot + '"'),
    '-runTests', '-testPlatform', 'EditMode',
    '-assemblyNames', 'ProjectL.Tests.Core', '-testFilter', $cellFilter,
    '-testResults', ('"' + (Join-Path $cellEvidence "$cellRun.xml") + '"'),
    '-logFile', ('"' + (Join-Path $cellEvidence "$cellRun.log") + '"'))
$cellProcess = Start-Process -FilePath 'D:/Program Files/Unity/Hub/Editor/6000.0.73f1/Editor/Unity.exe' `
    -ArgumentList $cellArgs -WindowStyle Hidden -PassThru
```

Check process completion through short status checks while keeping progress visible. After exit, record `$cellProcess.ExitCode`, inspect the log, and parse the NUnit XML. GREEN means the intended fixtures actually ran, zero focused failures, no compilation errors, and a compatible process result. A missing/empty report, no selected tests, or a license/editor-lock failure is not a passing result. RED must show the intended missing contract or behavioral assertion, not an unrelated setup fault.

| Filter | Assembly and fixtures |
| --- | --- |
| P1 | `ProjectL.Tests.Core`: `ProjectL.Tests.Core.CellEnvironmentQueryTests` |
| P2 | Core: `ProjectL.Tests.Core.EnvironmentPlacementRuleTests;ProjectL.Tests.Core.CellEnvironmentQueryTests` |
| P3 | `ProjectL.Tests.Framework`: `ProjectL.Tests.Framework.StageEnvironmentAdapterTests;ProjectL.Tests.Framework.BattleDefinitionAdapterTests` |
| P4 | Core: `ProjectL.Tests.Core.BattleEnvironmentPreparationTests;ProjectL.Tests.Core.BattlePreparationRuleTests` |
| P5 | Framework: `ProjectL.Tests.Framework.EnvironmentEntryPublicationTests;ProjectL.Tests.Framework.BattleEntryPublicationTests;ProjectL.Tests.Framework.CharacterBattlePreparationTests;ProjectL.Tests.Framework.CombatStateBufferTests;ProjectL.Tests.Framework.CombatEntryIntegrationTests` |
| Baseline/final | Omit `-testFilter`; `-assemblyNames ProjectL.Tests.Core;ProjectL.Tests.Framework;ProjectL.Tests.CombatViewBoundary` |

Use distinct run names for RED/GREEN/baseline/final so evidence is never overwritten. The final regression run reads existing movement tests without modifying them. Do not copy old pass/fail counts into the completion claim. Run `git diff --check`, inspect the exact allowlist, and compare protected-file hashes before delivery.

## 9 Completion gates and spec coverage

| Gate | Evidence required | Claim allowed |
| --- | --- | --- |
| F Foundation | P1/P2 Cell queries and known predicates; P3 typed in-memory conversion; P4 preparation/reconciliation; P5 real-store tests and regression comparison | Cell environment foundation implemented and verified with fixture definitions. |
| G Gameplay | Approved Object-specific arrival/consumption/removal and cost mapping onto completed movement rules; applicable reaction, timing, expiry, and dynamic-publication contracts plus their tests | Only the particular completed gameplay slice may be claimed. Not closed by this plan; completed movement is not reopened. |
| D Production data | Real definition IDs, SO/query schema, approved values/visibility, actual default-source entry, T1/G7 resolved by its owner | Authored DB path verified. Not closed by in-memory DTO tests. |
| V View and execution | State/result read mapping, visuals, highlight coexistence, registered caller/lifecycle path | Actual View/Flow integration verified. Not closed by direct service calls. |
| A Activation | Applicable F/G/D/V evidence plus in-scene acceptance | Production environment feature activated. Not closed by this plan alone. |

Spec coverage is intentionally bounded:

- Sections 2/4/5/6/10: enforced by constraints, P1–P3 ownership/spatial data, and P5 entry publication.
- Section 7: P4 provides pure contribution reconciliation and P5 verifies initial publication followed by ordinary movement and checkpoint Return; general dynamic environment transitions/events remain a separate contract.
- Section 8: only the existing presence capability is connected, including supported unclassified destination modifiers during initial preparation. Object-specific arrival/removal/cost mapping and timed Terrain remain blocked as listed in section 1; established generic movement processing is preserved.
- Section 9: P1/P2 represent and check explicit obstacle occupancy; production obstacle data, owner policy, reactions, and relocation remain separate.
- Sections 11/12: query metadata and extension boundaries are prepared, but View implementation and a full definition catalog are not claimed.
- Sections 13–15: local blockers, staged completion, and focused tests are preserved. Stacked-Object work is not pulled into any gate.

At handoff, report the implemented files and APIs, fixture-backed capabilities, fresh verification evidence, untouched movement/Framework scope, remaining gameplay decisions, and separate G/D/V/A status. Stop after the authorized foundation tasks; do not turn an unresolved question into a default implementation. Review this plan before selecting sequential execution or per-task subagents.
