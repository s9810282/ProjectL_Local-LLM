# Wall Attack Blocking Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. Execution method is selected by the user; this document does not start implementation.

**Goal:** Add exact tile-center wall obstruction to the existing attack system, preserve category eligibility and no-wall combat behavior, and remove obsolete aim/Cannon contracts without expanding coordinator responsibilities.

**Architecture:** Immutable stage terrain supplies wall facts. Focused Domain calculations build straight wall regions, clip exact segments, and collect piercing encounters; Application composes these results with existing targeting permissions and settlement. Existing Buffer transactions publish immutable results, and View renders Core diagnostics without calculating attack legality.

**Tech Stack:** Unity 6000.0.73f1, C#, NUnit EditMode, existing Core/Framework assemblies, standard-library `System.Numerics.BigInteger` for exact rational comparison.

**Spec:** [Wall Attack Blocking and Attack Contract Cleanup](../specs/2026-10-08-wall-attack-blocking-design.md). Read both documents. Source baseline: `680f309f`; recheck the working tree before execution. Planning date: 2026-10-08.

## Global Constraints

- Implementation documentation is English; new or changed code comments are Korean.
- Commits and pushes remain user-owned, following the existing repository implementation-plan convention. Leave each reviewed task uncommitted unless explicitly instructed otherwise.
- Core must remain independent of Unity; keep `noEngineReferences: true` and Contracts -> Domain -> Application dependency direction.
- `CornerCutTicks = 128`, `TicksPerTile = 1024`, valid corner ticks `0 <= ticks < 512`. Wall APIs receive exact endpoint cells, never quantized directions.
- Only static, public, non-destructible, one-cell-wide horizontal/vertical wall runs are supported. Reject unsupported topology instead of approximating it.
- Preserve empty/friendly eligibility, self exclusion, Cannon near exclusion, AP/refunds, physical structure ray stopping, and per-ray encounter semantics.
- Preserve the planning defaults in spec Section 2: direct RequireClear, splash FromImpact, immediate Thorn propagation clipped, existing KillChain/environmental effects wall-independent, per-ray piercing cap migrated exactly.
- Preserve `AttackAimKind.Cell=2`, `Direction=3`, `All=4`; do not recycle the retired Entity value. Keep gameplay EntityIds.
- No transaction bodies in `BufferSystemGroup`, no wall math in Buffer/AI/View/settlement, no per-unit/category wall branches in consumers, and no parallel wall store or generic rule registry.
- New pure implementations stay internal. Existing Domain `InternalsVisibleTo` already includes Application and Core tests; do not make helpers public merely to reach them.
- Do not modify the existing untracked DB handoff as collateral work. Its later update is an explicit Task 9 deliverable after the runtime schema is settled.

## Review Focus

1. An arbitrary slope such as `(5,3)` must never become a cardinal/diagonal segment, even when representative facing is R0: Tasks 3 and 6.
2. A blocked target absent from `AttackAims` still needs hover diagnostics, without enabling a click or depending on an unrequested preview: Task 8.
3. A multi-cell structure with one reachable occupied contact must be hit once; its anchor is not automatically the contact: Task 6.
4. A wall before a physical structure must not make victims beyond that structure become diagnostic targets or consume falloff: Task 5.
5. Configuration and blocker evidence must survive state/result copies and rejected publication without changing gameplay state: Tasks 4 and 7.

---

## 1. Planning clarifications and exclusions

The user authorized implementation planning, not production edits. The following clarifications were added during the source review:

- **Aim direction, representative facing, and the obstruction segment differ.** Existing targeting masks can be directional. A wall segment is always `A + t(B-A)` with exact endpoint cells. Cell-aim examples use arbitrary slopes; this feature does not change what cells a mask permits selecting.
- **Existing facing limitation remains explicit.** `CombatCommandCalculator.AttackRotation` maps non-cardinal direction input to R0, and All has no unique direction. This feature must not feed that value into wall geometry or silently change facing-sensitive effects. Pin current facing compatibility and test actual diagonal geometry independently. A new eight-way/free-angle facing mechanic is outside this plan.
- **Blocked evidence is not a hit list.** Piercing can retain otherwise eligible hidden contacts for diagnostics after hit collection stops, but stops all traversal at the first physical structure. No hidden contact increments encounter indices or damage.
- **Failure data cannot be lost with null selection.** Introduce the internal evaluation value in Task 6, rather than attaching diagnostics to a success-only selection or encoding wall IDs into the reason string.
- **No positive-damage admission redesign.** Range/admission remain spatial. Current zero-budget piercing behavior is preserved; execution/AI apply the budget after spatial collection.
- **W07/W08 need correct board coordinates.** `(8,9)` is legal for a pure geometric test, not a zero-based 9x9 integrated board. Integration fixtures use 10x10 or translate every coordinate consistently.

The splash/Thorn/KillChain choices are visible planning defaults from spec Section 2. They are not claims that the original wall document already specified these mechanics.

## 2. Paths, file ownership, and verification

All path aliases below refer to exact repository-relative directories:

| Alias | Directory |
| --- | --- |
| `CORE` | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator` |
| `ADAPTER` | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CombatSystem` |
| `DB` | `Assets/ProjectL/Script/Systems/DataBaseSystem/Data/InGame/Table` |
| `BUFFER` | `Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems` |
| `VIEW` | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat` |
| `TESTS` | `Assets/Tests/EditMode` |

Behavior owners created by this plan are `WallGeometryBuilder`, `WallSegmentClipper`, `AttackOcclusionResolver`, and `PierceTargetCollector`. `WallGeometry`, `PierceTrace`, and result/selection types are immutable data, not services. Existing targeting geometry owns ordered ray definitions. Existing environment spatial checks own occupied-cell restrictions. Each task lists its files; newly created scripts require matching Unity metadata through the normal project workflow.

The Unity executable and project version were verified during planning. Use this command template with each task's assembly/filter and a unique label; substitute the active worktree path if execution uses one:

```powershell
$wallProject = 'D:/My_ProjectWork/Github_desktop/Project_L'
$wallUnity = 'D:/Program Files/Unity/Hub/Editor/6000.0.73f1/Editor/Unity.exe'
$wallAssembly = 'ProjectL.Tests.Core'
$wallFilter = 'ProjectL.Tests.Core.WallGeometryTests'
$wallRun = 'wall-task3-green'
New-Item -ItemType Directory -Force -Path "$wallProject/TestResults" | Out-Null
& $wallUnity -batchmode -nographics -projectPath $wallProject -runTests -testPlatform EditMode -assemblyNames $wallAssembly -testFilter $wallFilter -testResults "$wallProject/TestResults/$wallRun.xml" -logFile "$wallProject/TestResults/$wallRun.log"
```

Run separate invocations for separate named fixtures, or use Unity's supported filter separator. Omit `-testFilter` for a full assembly run. Do not launch a second Editor against an already open project; use that Editor's EditMode runner. Omit `-quit` for test execution. Read XML totals and failures and inspect compilation logs; process exit alone is insufficient. For green, require nonzero executed tests and zero failures/errors. A .NET geometry harness is supplemental, not a substitute for Unity assembly/Framework validation.

Before starting, record `git status --short`, current HEAD, and existing modified/untracked files. Preserve unrelated work. Each task follows red test -> focused implementation -> green test -> responsibility review. For new APIs, a missing-symbol red compile is only initial evidence; once declarations exist, verify an intended assertion fails before implementing the behavior. Never count broken fixtures as a useful red result.

## Task 1: Retire entity aim without removing target identity

**Modify:**

- `CORE/Contracts/Common/AttackAimKind.cs`
- `CORE/Contracts/Commands/AttackAim.cs`
- `CORE/Domain/Combat/AttackSelectionResolver.cs`
- `CORE/Contracts/Results/AttackPreview.cs`
- `CORE/Contracts/Results/AttackResult.cs` (matching stale aim comment)
- `CORE/Application/Combat/EnemyAi/EnemyAiPolicy.cs` (remove the always-empty aim identity tie-breaker)
- `BUFFER/CombatView/CombatViewCommand.cs`
- `TESTS/TargetingMethodContractTests.cs`, `TESTS/TargetingMethodSelectionTests.cs`
- `TESTS/Framework/CombatViewBufferTests.cs`

**Interfaces:** Retain `AttackAim.ForCell(CellCoord cell)`, `ForDirection(AttackDirection direction)`, and `ForAll()`. Remove `ForEntity`, aim's EntityId, the candidate-direction resolver overload, and the obsolete `ForCell(CellCoord, AttackDirection)` overload after checking callers. Other EntityId contracts are unchanged.

- [x] Add `SupportedAimKinds_KeepNumericValues`, `CellAim_HasNoHeading`, and Buffer invalid/default-kind rejection tests. Assert Cell=2, Direction=3, All=4 and None is rejected. Replace the obsolete Entity-positive test with these contract assertions.
- [x] Run `TargetingMethodContractTests`, `TargetingMethodSelectionTests`, and Framework `CombatViewBufferTests`; record the intended failures.
- [x] Remove obsolete members and migrate all compiler-identified callers. Remove stale entity-aim preview comments; retain Core validation of aim kind, direction, bounds, and method compatibility. Buffer allows exactly the supported input kinds.
- [x] Run the same fixtures plus `AttackTargetAdmissionTests`; ordinary empty/friendly rejection and Cannon exceptions must remain unchanged.
- [x] Search production/tests for `ForEntity`, `AttackAimKind.Entity`, and aim-specific EntityId access. Classify unrelated target IDs instead of deleting them. Review and retain this task uncommitted.

**Task 1 verification (2026-10-08):** Unity EditMode red: 84 tests, 77 passed, 7 intended assertion failures (three obsolete-contract checks and four unsupported Buffer kinds). Green: 166/166 passed across `TargetingMethodContractTests` (44), `TargetingMethodSelectionTests` (20), `CombatViewBufferTests` (20), `AttackTargetAdmissionTests` (47), `StageEnemyAiTurnTests` (12), and `TargetingPreviewParityTests` (23). Unity recompiled the affected assemblies without compiler errors. Source search found no remaining obsolete production calls or aim identity access; the only `ForEntity` string is its absence assertion. Test artifacts: `.superpowers/sdd/2026-10-08-wall-attack-blocking/test-results/wall-task1-{red,green}.{xml,log}`. Task 2 remains unstarted.

**Deliverable:** A smaller aim contract with identical current gameplay selection behavior.

## Task 2: Migrate the live piercing cap and retire legacy impact authoring

**Modify:**

- `CORE/Contracts/Commands/Traits/PiercingFalloffTraitSpec.cs`
- `CORE/Domain/Traits/Definitions/PiercingFalloffTraitDefinition.cs`
- `CORE/Domain/Traits/Rules/PiercingFalloffTraitRule.cs`
- `CORE/Application/Combat/Traits/TraitFactories.cs`
- `CORE/Application/Combat/CombatCommandCalculator.cs`
- `CORE/Contracts/Commands/UnitSpec.cs`, `CORE/Domain/Entities/UnitDefinition.cs`
- `CORE/Application/BootstrapModels/UnitCatalog.cs`
- `DB/Unit/Trait/UnitTraitAuthoring.cs`, `DB/Unit/Trait/UnitTraitData.cs`, `DB/Unit/Item/UnitData.cs`, `DB/Unit/Item/UnitDataSO.cs`
- `ADAPTER/BattleDefinitionAdapter.cs`
- `Assets/ProjectL/Script/Systems/Factory/Runtime/Development/TemporaryCoreDatabase.cs`
- `TESTS/UnitSpecTestFactory.cs`, `TESTS/CombatCommandTestData.cs`, `TESTS/PiercingFalloffTraitTests.cs`, `TESTS/CombatEffectPierceFalloffTests.cs`, `TESTS/CharacterCategoryContractTests.cs`, `TESTS/Framework/DataBaseAssetTests.cs`

**Caller closure performed in Task 2:** `CORE/Contracts/Commands/AttackProfileCompatibility.cs` (retain the live aim mapping), `CORE/Contracts/Commands/Categories/CategorySpec.cs`, and `CORE/Domain/Combat/ImpactPatternCalculator.cs` (retain live direction helpers, correct documentation); `VIEW/../Dev/DevBattleInitializationSource.cs`; `TESTS/UnitStateTestFactory.cs`, `UnitCombatStaticStatsTests.cs`, `ThornFieldTraitTests.cs`, `EnemyMovementPathTests.cs`, `EnemySpawnCalculationTests.cs`, `BattlePreparationRuleTests.cs`, `BattleEnvironmentPreparationTests.cs`, `AttackProfileRuleTests.cs`, `AttackTargetAdmissionTests.cs`, `CombatCommandEffectTests.cs`, `DistanceDamageScaleTraitTests.cs`, `ObstacleReactionTests.cs`, `StaticTerrainIntegrationTests.cs`, `TargetingPreviewParityTests.cs`; Framework `BattleDefinitionAdapterTests.cs`, `CharacterBattlePreparationTests.cs`, `CharacterRosterDatabase.cs`, `CombatEntryIntegrationTests.cs`, `CombatFlowIntegrationTests.cs`, `CombatInteractionTests.cs`, `TargetingMethodDataTests.cs`; and the five existing unit assets. These are constructor/field migrations and obsolete-contract test retirement, not new behavior owners.

**Retire after caller closure:** `CORE/Contracts/Commands/ImpactPatternSpec.cs`, its obsolete impact enums/compatibility helper, and matching metadata. Discover exact consumers with `rg -n -g '*.cs' 'ImpactPattern|CannonArea|PierceRayCount' Assets`; compile and inspect each reported definition/adapter/test before deletion. `ImpactPatternCalculator`'s live direction helpers are not deletion-ready until Task 5.

**Interfaces:** `PiercingFalloffTraitSpec(int reductionPercentPerTarget, int maxTargetsPerRay)` and the matching definition expose immutable `MaxTargetsPerRay >= 0`. The trait's existing target-budget evaluation returns the minimum of its migrated cap and its existing damage-falloff budget. Consumers no longer read `Definition.ImpactPattern.Range`.

- [x] Record every old piercing definition's `(reduction, impactRange)` and expected per-ray hits. Add `MigratedCap_PreservesZeroOneAndMultipleTargets`, with caps 0, 1, 3, and a cap above the falloff limit; assert exact target order/damage equality to the baseline fixtures.
- [x] Run the two piercing fixtures red. Add a DB round-trip assertion that explicit cap 0 stays 0 and negative cap fails validation.
- [x] Carry the new value through trait authoring/factories/evaluation; migrate constructor fixtures and serialized data before removing the legacy field. Do not give an omitted cap an arbitrary positive default.
- [x] Remove old shape/ray-count inputs and obsolete category-impact validation after caller migration. Preserve `CombatAttackProfile`'s actual targeting/trait compatibility checks and category eligibility rules.
- [x] Inspect actual `Assets/ProjectL/DataBaseSO/InGameGroup/Unit/Item/*.asset` fields. Planning found legacy serialized fields even on non-Cannon assets; remove only obsolete fields and preserve unrelated GUIDs/content. If a piercing asset exists at execution time, copy its old cap before deletion. No generic migration framework is needed for this bounded asset set.
- [x] Run the affected Core fixtures, Framework `DataBaseAssetTests`, and full compiler checks. Compare before/after no-wall piercing results, review, and retain uncommitted.

**Task 2 verification (2026-10-08):** Accepted red run: 88 tests, 9 intended ignored-cap failures. Focused green: 194/194 passed. Full Core: 1497/1497 passed. Affected Framework fixtures: 183/183 passed. Full Framework has 1605 passed, 30 failed, and 1 inconclusive out of 1636. An isolated clean `680f309f` worktree reproduced all 30 failures and the same inconclusive test (its full Framework run had 31 failures, including one baseline-only artifact-board mapping failure). There are no current-only failing test names. The existing UI/audio/rendering/bootstrap failures are outside Task 2 and remain unresolved; do not describe the full Framework suite as green. Inventory and XML/log evidence are under `.superpowers/sdd/2026-10-08-wall-attack-blocking/`, including `task2-migration-inventory.md` and `test-results/wall-task2-baseline-comparison.txt`. Task 3 remains unstarted; changes remain uncommitted.

**Deliverable:** Piercing owns its cap explicitly; unused Cannon shape inputs no longer misrepresent behavior.

## Task 3: Implement exact wall construction and clipping

**Create:**

- `CORE/Contracts/Commands/WallBlockingSpec.cs`
- `CORE/Domain/Combat/WallGeometry.cs`
- `CORE/Domain/Combat/WallGeometryBuilder.cs`
- `CORE/Domain/Combat/WallSegmentClipper.cs`
- `TESTS/WallGeometryTests.cs`, `TESTS/WallGeometryReference.cs`

**Modify:** `CORE/Contracts/Combat/StaticTerrainFact.cs` (append Wall=3), `CORE/Domain/ProjectL.Core.Domain.asmdef` only if standard-library resolution requires it; retain noEngineReferences and no new package dependency.

**Interfaces:**

- `WallBlockingSpec(int cornerCutTicks)` exposes CornerCutTicks, const TicksPerTile=1024, and immutable Default(128).
- `WallGeometryBuilder.Build(int width, int height, MovementEnvironmentFacts environment, EnvironmentSemanticBindings bindings, WallBlockingSpec settings) : WallGeometry` validates supported content and returns ordered runs/occupied cells.
- Internal `WallGeometry` exposes `IReadOnlyList<WallRun> Runs` and `bool Occupies(CellCoord cell)`. `WallRun` contains long tick bounds, corner ticks, sorted source IDs, and minimum-ID component key. No actors or repositories.
- `WallSegmentClipper.IntersectsInterior(CellCoord start, CellCoord end, WallRun run, out SegmentParameter entry) : bool`. Internal `SegmentParameter` has long numerator/positive denominator and exact CompareTo using BigInteger products; keep it in `WallGeometry.cs`, not a public math API.

- [x] Add the spec's W01-W09 as literal endpoint cases, plus W07 at ticks 127/128/129 expecting blocked/clear/clear. Add arbitrary `(5,3)` and reversed segments, internal seams, boundary-only contact, same-cell clear propagation, and a wall behind B.
- [x] Add malformed topology/duplicate-cell/bounds/visibility tests and horizontal/vertical/single-cell runs. Tick -1 and 512 reject; 0 and 511 accept. Test extreme int endpoint coordinates against an ordinary valid run to exercise exact arithmetic without a huge board allocation.
- [x] Run `WallGeometryTests` red, then implement four-neighbor component construction and the spec's eight strict half-planes. Reject diagonal contact between separate components. Cast before arithmetic; scale min/max centers before adding/subtracting half-cell ticks.
- [x] Implement the independent test oracle with explicit clipped-polygon vertices and rational segment-edge crossings. Sort crossing parameters and test interval midpoints with polygon orientation, instead of reproducing production plane clipping. Compare all distinct non-wall pairs on a small board, reversed endpoints, rotations, translations, source reorder, and monotonic corner cuts.
- [x] Run the fixture green and confirm Core compiles against BigInteger. Record executed case counts. Review APIs for absence of Rotation4/AttackDirection, engine references, unit facts, and state mutation.

**Task 3 verification (2026-10-08):** Red: 92 tests, 17 passed and 75 intended assertion failures with empty-builder/clear-clipper stubs. Green: 92/92; full Core: 1589/1589, no failures or skips. After the full run, the oracle assertions were strengthened to compare every transformed result against an independently specified original polygon and explicitly check nondefault cut propagation; the final focused run again passed 92/92. The 40 exhaustive cases cover 18,480 ordered non-wall endpoint pairs (including reversals), four rotations, two translations, five cuts, and source reorder. A separate check covers 3,234 segment/cut combinations for monotonicity. `BigInteger` resolves without asmdef or package changes; `noEngineReferences` stays true. Only mathematical/content-validation branches were added; no actor, direction-enum, repository, Buffer, or gameplay-admission responsibilities entered these helpers. XML/logs and detailed rulings are in `.superpowers/sdd/2026-10-08-wall-attack-blocking/`. Task 4 remains unstarted; changes remain uncommitted.

**Deliverable:** Engine-independent exact geometry, not yet connected to gameplay admission.

## Task 4: Carry wall facts/settings through stage preparation and occupancy

**Create:** `TESTS/WallStateLifecycleTests.cs`, `TESTS/WallMovementTests.cs`.

**Modify:**

- `CORE/Contracts/Combat/BattleInitialization.cs`, `CORE/Contracts/Combat/BattlePreparation.cs`, `CORE/Contracts/Combat/CombatCommandState.cs`
- `CORE/Application/Combat/BattlePreparationCalculator.cs`, `CORE/Application/Combat/BattleEnvironmentPreparation.cs`, `CORE/Application/Combat/EnvironmentContentSupport.cs`, `CORE/Application/Combat/CellEnvironmentQuery.cs`
- `CORE/Application/Combat/MovementProjection.cs`, `CORE/Application/Combat/CombatCommandCalculator.cs`, `CORE/Application/Combat/CombatCommandComposition.cs`
- `CORE/Domain/Movement/EnvironmentTerrainRules.cs`, `CORE/Domain/Movement/MovementRouteValidator.cs`
- `CORE/Application/Combat/CombatPlacementCalculator.cs`, `CORE/Application/Combat/CombatGimmickCalculator.cs`
- `BUFFER/Combat/CombatStateBuffer.cs`, `BUFFER/Combat/BattlePreparationValidator.cs`
- `DB/Stage/Combat/CombatStageData.cs`, `DB/Stage/Combat/CombatStageDetail.cs`, `ADAPTER/BattleDefinitionAdapter.cs`
- `TESTS/EnvironmentPlacementRuleTests.cs`, `TESTS/Framework/BattleDefinitionAdapterTests.cs`

**Interfaces:** Add read-only `WallBlockingSpec WallBlocking` to initialization, CombatPreparation, and command state; append optional constructor parameter `WallBlockingSpec wallBlocking = null`, resolving null to Default only at creation boundaries. All derived copies explicitly pass the original value. `EnvironmentTerrainRules.IsWallOccupied(MovementEnvironmentFacts environment, EnvironmentSemanticBindings bindings, CellCoord cell) : bool` is the shared non-geometric occupancy predicate.

For bounded SO intake, add `initialWallCells: CellInput[]` and `cornerCutTicks: int = 128` to CombatStageData; expose copied `WallCells` and WallBlocking in CombatStageDetail. The adapter sorts cells by Y then X, rejects duplicates, and creates public active Wall StaticTerrainFacts with deterministic IDs allocated after the maximum existing environment source ID. Merge them into the existing Environment and Terrain bindings once. No serialized wall-manager object or separate runtime store. A replacement DB provider can supply equivalent terrain facts directly.

- [x] Add nondefault-tick round-trip tests (257) through adapter, initialization.WithAttackProfiles, preparation, simulated movement, rejected command, accepted command, stage exit, and fresh stage entry. Assert SameValues detects a tick change and original snapshots remain immutable.
- [x] Add wall overlap/visibility/topology failures during preparation. Initial characters, structures, objects, and rails overlapping walls reject before publication. Existing no-wall content still loads.
- [x] Run lifecycle/adapter fixtures red; implement state/settings propagation and call the Task 3 builder during preparation. Audit every `new CombatCommandState`, target-typed state construction, and copy method, not just the files listed above.
- [x] Implement shared full-tile occupancy checks for placement, redeployment, voluntary movement, AI pathfinding, forced displacement, and relevant gimmick/rail placement. Do not use clipped-corner geometry for movement. Add a Korean responsibility comment at the occupancy helper.
- [x] Add `WallCornerClearForAttack_StillOccupiedForMovement`, `KnockbackIntoWall_DoesNotMove`, and Player/Enemy deployment rejection. Supply actual existing movement/placement failure conventions rather than inventing a second transaction policy. The attack-ignore policy integration test belongs to Task 6, where that contract exists.
- [x] Run `WallStateLifecycleTests`, `WallMovementTests`, `EnvironmentPlacementRuleTests`, `StaticTerrainIntegrationTests`, and Framework `BattleDefinitionAdapterTests`. Verify Buffer source comparison enforces static wall/settings lifetime and all content validation remains in Core/adapters, not Group.

**Task 4 verification (2026-10-08):** Initial behavior red: 55 tests, 20 intended failures; publication/authoring red: 10/10 intended failures. First focused green: 107/107. Additional path-audit red runs exposed weighted enemy spawn (two failures), Tornado relocation (one), and flag-drop candidates (one); these now use the shared full-cell occupancy predicate. Final regression: full Core **1621/1621**, affected Framework **228/228**, **1849 total**, zero failures/skips/inconclusive. The named fixtures include lifecycle (15), wall movement (12), placement rules (22), static terrain (25), adapter (38), and wall publication (9). Evidence is in `.superpowers/sdd/2026-10-08-wall-attack-blocking/test-results/wall-task4-*.xml` and matching logs.

**Implementation rulings:** `StageEnvironmentAdapter` owns deterministic wall-ID allocation. `CellEnvironmentSnapshot.IsWallOccupied` and appended `WallOverlap=16` let existing placement rules distinguish walls from passable terrain. `CellEnvironmentQuery.ValidateInitialPlacement` composes geometry/content validation and existing coexistence rules; Buffer delegates preparation checks and only enforces stored wall/settings immutability. The audit additionally covered `CombatTurnCalculator`, `MovementEnvironmentCalculator`, `EnemySpawnCalculator`, `TornadoCalculator`, `FlagDropCalculator`, and the development initialization copy. `CombatCommandComposition` and `CombatStateData` already forward complete state objects. Rail activation needs no wall branch because overlapping rail/wall content is rejected at preparation. No-wall random spawning retains its original candidate order and RNG sequence. No new gameplay state store, service interface, engine dependency, or Group transaction body was added.

Task 5 has not started. These changes connect wall authoring, lifetime, validation, and occupancy; attack obstruction/admission remains scheduled for Tasks 5-6. Changes remain uncommitted. The unrelated full-Framework baseline failures recorded under Task 2 were not rerun or changed.

**Deliverable:** Authored walls survive the combat lifecycle and block occupancy; attacks still need Tasks 5-6.

## Task 5: Share ordered ray geometry and extract piercing traversal

**Create:** `CORE/Domain/Combat/PierceTargetCollector.cs`, `CORE/Domain/Combat/PierceTrace.cs`, `CORE/Domain/Combat/AttackOcclusionResolver.cs`, `CORE/Contracts/Results/AttackBlockedContact.cs`, `TESTS/WallPierceTests.cs`.

**Modify:** `CORE/Domain/Combat/AppointShapeCalculator.cs`, `CORE/Application/Combat/CombatCommandCalculator.cs`, `CORE/Application/Combat/CombatAttackQueries.cs`, `CORE/Application/Combat/ResolvedAttackSelection.cs`, `CORE/Application/Combat/EnemyAi/EnemyMovementPathfinder.cs`.

**Interfaces:**

- `AppointShapeCalculator.CreateOrderedRaySteps(TargetingShape shape, AttackDirection direction) : CellCoord[]`; supported line shapes only, explicit unsupported-shape failure. EightDirectionLine returns forward, left diagonal, right diagonal. CreateDirectionMask uses this same definition but retains sorted mask output.
- `PierceTargetCollector.Collect(CellCoord origin, IReadOnlyList<CellCoord> actorCells, IReadOnlyList<CellCoord> raySteps, int width, int height, int range, IReadOnlyList<CharacterCombatFacts> eligibleCharacters, IReadOnlyList<StructureCombatFacts> eligibleStructures, IReadOnlyList<StructureCombatFacts> physicalStructures, AttackTargetingPolicy policy, WallGeometry walls, bool ignoreWalls) : PierceTrace`.
- `AttackBlockedContact(CellCoord origin, CellCoord contact, AttackOcclusionPhase phase, IReadOnlyList<long> wallSourceIds, IReadOnlyList<CellCoord> wallCells)` with Phase {Direct=1, Propagation=2}. Copy sorted unique positive IDs and unique occupied cells ordered by Y/X; require matching nonzero counts. Expose Origin, Contact, Phase, WallSourceIds, and WallCells. No engine objects or mutable arrays. Construct evidence from Task 3 run bounds/source facts in Domain, never in View.
- `AttackOcclusionResolver.FindBlock(CellCoord origin, CellCoord contact, WallGeometry walls, AttackOcclusionPhase phase, bool ignoreWalls) : AttackBlockedContact` centralizes iteration over runs, calls WallSegmentClipper, and returns null or nearest-entry blocker evidence, with component-key tie breaking. The collector delegates to it; Task 6 adds authored-policy entry points to this same owner.
- `PierceTrace` exposes immutable `Encounters` (EntityId Id, int Encounter, int Ray), hidden `BlockedContacts` (AttackBlockedContact), and `HasWallBlockedEligibleTargets`. `PierceTargetCollector.ApplyBudget(PierceTrace trace, int maxTargetsPerRay) : (EntityId Id, int Encounter, int Ray)[]` returns encounter facts with Encounter less than the supplied budget; no traversal occurs here.

- [x] Add no-wall characterization for directions Up/Right/Down/Left, ray indices, caps 0/1/3, excluded friends, physical allied structures, and multi-cell structures. Assert encounter order and target damage match pre-extraction behavior.
- [x] Add walls blocking only one ray and walls in empty cells. Add a wall before an allied physical structure and a hostile victim beyond that structure: no hit, no hidden-victim diagnostic, no falloff consumption for that victim.
- [x] Run `WallPierceTests` red; extract ray steps and collector. Hit collection stops at the first wall crossing, while the same loop may inspect hidden eligible contacts only up to the first physical structure/range boundary. Never restart a ray for diagnostics.
- [x] Store the spatial trace in resolved selection. Execution and AI apply the shared budget to that trace instead of re-collecting positions. Preserve current eligibility and near exclusion. Use explicit ignoreWalls=false until Task 6 connects the authored policy.
- [x] Remove the calculator's traversal body and unused ImpactPatternCalculator helpers after all callers migrate. Run `TargetingMethodGeometryTests`, `WallPierceTests`, `CombatEffectPierceFalloffTests`, and `EnemyMovementPathTests`; review no-wall behavior and retain uncommitted.

**Task 5 implementation rulings:** Preserve the legacy exclusion of adjacent cells before inspecting physical structures in those cells; beyond that exclusion, physical structures terminate both hit and hidden-contact traversal regardless of eligibility. Wall checks precede this exclusion. A change to the pre-existing adjacent-structure rule is a separate gameplay decision. Build wall geometry from complete public environment facts, not observer-filtered `VisibleTo` projections. Range builds geometry once for its candidate evaluations. Resolved selection derives `IsPiercing` from the optional immutable trace, avoiding contradictory boolean/trace state. Additional caller closure touched `AttackTargetAdmissionTests` and removed only the unused right-step helper from `ImpactPatternCalculator`; its forward helper remains required by forced movement.

**Task 5 verification (2026-10-08):** Red: 41 tests, 14 passed and 27 intended assertion failures; all 12 pre-extraction direction/cap damage cases passed. The first implementation compile identified a missing Domain namespace import in the AI caller; it was corrected before the accepted green run. Focused green: **257/257** across `WallPierceTests` (41), `TargetingMethodGeometryTests` (64), `CombatEffectPierceFalloffTests` (11), `EnemyMovementPathTests` (26), `AttackTargetAdmissionTests` (47), `CombatCommandEffectTests` (45), and `TargetingPreviewParityTests` (23). Full Core: **1662/1662**, zero failures/skips/inconclusive. Evidence: `.superpowers/sdd/2026-10-08-wall-attack-blocking/test-results/wall-task5-{red,green2,core}.{xml,log}`. The new tests also cover nearest-blocker ordering, tie breaking, tangent clearance, immutable evidence, ignored-wall physical stopping, and extreme board coordinates. No engine dependency, service registry, persistent geometry cache, or BufferSystemGroup change was introduced. Framework was not rerun for this internal Core extraction; its prior baseline failures remain documented under Task 2.

**Scope boundary:** Task 5 enforces walls on piercing traversal and retains successful-selection evidence internally. General attack admission, authored wall policies, and evidence on rejected selections remain Task 6; public diagnostic publication remains Task 7. Task 6 is unstarted. Changes remain uncommitted.

**Deliverable:** One definition of ray steps and one spatial collection, reusable by actual attack policy.

## Task 6: Resolve exact obstruction and shared attack admission

**Create:**

- `CORE/Contracts/Commands/AttackOcclusionSpec.cs`
- `CORE/Application/Combat/AttackSelectionEvaluation.cs`
- `TESTS/WallAttackAdmissionTests.cs`

**Modify:** `CORE/Contracts/Commands/UnitSpec.cs`, `CORE/Contracts/Combat/CombatAttackProfile.cs`, `CORE/Domain/Entities/UnitDefinition.cs`, `CORE/Application/BootstrapModels/UnitCatalog.cs`, `DB/Unit/Item/UnitData.cs`, `DB/Unit/Item/UnitDataSO.cs`, `ADAPTER/BattleDefinitionAdapter.cs`, `CORE/Domain/Combat/AttackOcclusionResolver.cs`, `CORE/Application/Combat/CombatAttackQueries.cs`, `CORE/Application/Combat/ResolvedAttackSelection.cs`, `CORE/Application/Combat/CombatCommandCalculator.cs`, `CORE/Application/Combat/EnemyAi/EnemyMovementPathfinder.cs`, `CORE/Contracts/Results/AttackFailureReason.cs`.

**Interfaces:**

- `AttackOcclusionSpec(AttackWallDelivery delivery, AttackWallPropagation propagation)` with Delivery {RequireClear=1, IgnoreWalls=2}, Propagation {FromImpact=1, IgnoreWalls=2}, Default RequireClear/FromImpact. Carry immutable Occlusion on UnitSpec/UnitDefinition; CombatAttackProfile delegates to its definition. UnitDataSO authors these two enums, never faction permissions.
- Reuse Task 5's `AttackBlockedContact` without introducing another diagnostic type.
- Extend Task 5's resolver with `CheckDirect(CellCoord attacker, CellCoord contact, WallGeometry walls, AttackOcclusionSpec policy) : AttackBlockedContact` and `CheckPropagation(CellCoord impact, CellCoord contact, WallGeometry walls, AttackOcclusionSpec policy) : AttackBlockedContact`. They map the respective policy once and call FindBlock. Callers provide exact origins; no per-consumer policy dispatch.
- Internal `AttackSelectionEvaluation` exposes `AttackFailureReason FailureReason`, success-only `ResolvedAttackSelection Selection`, and `IReadOnlyList<AttackBlockedContact> BlockedContacts` on either success or failure. The new entry signature is `CombatAttackQueries.Selection(CombatCommandInput input, CharacterCombatFacts actor, CombatAttackProfile profile, AttackAim? aim = null, MovementEnvironmentFacts environment = null, WallGeometry walls = null) : AttackSelectionEvaluation`. Update every caller atomically.
- Resolved selection retains raw Cells, ReachableCells, final Targets, eligible facts/policy, and optional PierceTrace. Range builds geometry once and passes it through its candidate evaluations; no global cache.

- [x] Add W07/W08-based real Single attacks on a 10x10 board using a square selection mask. Assert identical selected target and differing block result at ticks 128 versus 0. Add reverse and `(5,3)` cases proving representative facing is not an input.
- [x] Add ordinary empty/friendly rejection before wall filtering, clear/blocked Cannon empty centers, wall-occupied center -> AppointCellRejected, partially blocked Direction/All, genuinely empty groups, and all otherwise eligible contacts blocked -> BlockedByWall.
- [x] Add a multi-cell structure with clear and blocked contacts: one ordinary hit if any included contact is clear; Single checks its selected occupied cell; a hidden anchor is irrelevant. Assert no duplicate ordinary hits and stable role precedence.
- [x] Run `WallAttackAdmissionTests` red; implement exact resolver and typed evaluation. Append BlockedByWall without changing existing enum values. Separate occupancy, eligibility, and segment obstruction; do not attach wall logic to category rules.
- [x] Apply direct/propagation policy to normal and piercing paths. IgnoreWalls skips only segment crossing; wall occupancy and physical structure stopping remain. Preserve raw mask versus reachable/effect cell distinction. Add `IgnoreAttackWalls_DoesNotGrantMovement` against the Task 4 occupancy path.
- [x] Reject before movement sealing or resource/effect settlement. Add snapshots asserting AP, HP, pending movement, refunds, RNG, effects, and timeline are unchanged on rejection. Include a hostile target with zero predicted damage to preserve admission semantics.
- [x] Run `WallAttackAdmissionTests`, `AttackTargetAdmissionTests`, `WallPierceTests`, and `TargetingPreviewParityTests`. Compile all query/AI callers; review the coordinator for delegation rather than embedded geometry.

**Task 6 implementation rulings:**

- `AttackOcclusionResolver.ResolveCells` owns ordinary direct/propagation cell filtering; the query coordinator retains existing eligibility and acceptance ordering. Delivery/propagation enum dispatch occurs only in `CheckDirect` / `CheckPropagation`, including for piercing. No category or unit-key wall exception was added.
- Replace the collector's temporary boolean with `AttackOcclusionSpec` and retain immutable reachable cells in the same `PierceTrace` traversal. This avoids a second wall scan to reconstruct piercing cells. Additional caller closure includes `PierceTargetCollector`, `PierceTrace`, `AttackTargetAdmissionTests`, and `WallPierceTests`.
- Require raw and reachable cell selections explicitly in `ResolvedAttackSelection`. Existing result/preview designated/additional cells keep the raw selection; `AffectedCells` use the reachable selection. Failed evaluation retains evidence even when its success-only selection is null.
- UnitDataSO authors `wallDelivery` / `wallPropagation` with RequireClear / FromImpact defaults. The immutable policy passes through UnitData, adapter, UnitSpec, UnitDefinition/catalog, and CombatAttackProfile. Four Framework authoring/adapter cases cover both independent settings and reject undefined serialized values. Existing asset validation confirms omitted new fields use the intended defaults.
- The mixed-contact structure fixture was corrected after its first green attempt: `(1,3)->(4,5)` actually crosses the wall at `(2,3)`. Extending the structure to `(4,6)` provides a tangent clear contact; the test now explicitly verifies both segments and primary-role precedence.

**Task 6 verification (2026-10-08):** Initial red: 36 tests, 17 passed / 19 intended failures. Expanded Core + authoring red: 46 tests, 19 passed / 27 intended failures. Initial implementation: 211/212 passed, with the fixture-coordinate failure explained above. Boundary red: 44/45 passed; the new assertion exposed raw selection overwritten in result cells, and the result construction was corrected. Regression: **1832/1832 passed**, zero failures/skips/inconclusive — full `ProjectL.Tests.Core` **1707/1707**, selected Framework **125/125**. Named fixtures include WallAttackAdmissionTests 45, AttackTargetAdmissionTests 47, WallPierceTests 41, TargetingPreviewParityTests 23, BattleDefinitionAdapterTests 42, DataBaseAssetTests 17, TargetingMethodDataTests 26, CombatEntryIntegrationTests 25, and CombatFlowIntegrationTests 15. Evidence is under `.superpowers/sdd/2026-10-08-wall-attack-blocking/test-results/wall-task6-*.{xml,log}`. Full Framework was not rerun; Task 2 records its pre-existing failures.

**Final focused verification:** `wall-task6-final.xml`, **45/45 passed**, after strengthening the rejection fixture with nonzero refund-eligible movement, armed discount, unchanged AP/HP/checkpoint, and an explicit RNG state. No production behavior changed after the full regression run.

**Scope boundary:** Public blocker publication, generated Thorn propagation, and explicit secondary-effect compatibility tests remain Task 7. View presentation remains Task 8. No BufferSystemGroup, assembly definition, engine dependency, or global cache was added. Changes remain uncommitted.

**Deliverable:** Existing public attack calls enforce exact wall legality and return internal diagnostic evidence.

## Task 7: Close secondary-effect and publication paths

**Create:** `TESTS/WallAttackEffectsTests.cs`, `TESTS/Framework/WallAttackPublicationTests.cs`.

**Modify:**

- `CORE/Application/Combat/CombatCommandCalculator.cs`, `CORE/Application/Combat/KillChainAttackCalculator.cs`
- `CORE/Contracts/Results/AttackRangeResult.cs`, `CORE/Contracts/Results/AttackPreview.cs`, `CORE/Contracts/Results/AttackResult.cs`
- `CORE/Contracts/Combat/CombatCommandTransition.cs`, `CORE/Contracts/Combat/CombatResult.cs`
- `CORE/Application/Combat/CombatAttackQueries.cs`, `BUFFER/Combat/CombatStateBuffer.cs`, `BUFFER/Core/BattleTransactions.cs`
- `TESTS/KillChainAttackTraitTests.cs`, `TESTS/Framework/CombatPreviewPublicationTests.cs`

**Interfaces:** Add immutable `IReadOnlyList<AttackBlockedContact> BlockedContacts` to range, attack result/preview, transition, and published CombatResult. Constructor/factory additions default to an empty defensive copy for unrelated callers. Preserve evidence through Preview failure conversion, FromFacts, WithCompletion, and Buffer publication. Diagnostics are facts, not CombatEvents, and are allowed on rejected transitions with no gameplay changes.

- [x] Add a blocked splash neighbor test with a valid primary and a blocked Thorn application cell outside the targeting splash mask. Assert blocked cells receive no immediate damage/new field, existing shadowed fields are not removed, and clear cells retain existing effects.
- [x] Add explicit KillChain wall-independent hostile-only coverage and environmental explosion/persistent-field compatibility cases. Test that a blocked initial attack cannot trigger any of those effects.
- [x] Run `WallAttackEffectsTests` red; route generated Thorn cells through the resolver's propagation helper before placement/damage. Keep its footprint in the trait rule; keep math out of the settlement loop. Add a Korean comment naming KillChain's deliberate compatibility policy.
- [x] Add `RejectedWallAttack_PublishesEvidenceWithoutStateChange` and `CopiesAndCompletion_PreserveDiagnostics`. Assert identical source IDs/origin/contact across preview/result/Buffer, empty events and unchanged state for rejection, and no loss through copies.
- [x] Carry evidence through existing transactions and read models. Preserve reason-string compatibility; do not serialize wall IDs into Reason or invent a second wall transaction. Range keeps raw RangeCells and raw direct InRangeTargetIds, legal AttackAims, and direct blocked contacts; per-aim preview includes propagation contacts.
- [x] Run Core `WallAttackEffectsTests`, `KillChainAttackTraitTests`, and Framework `WallAttackPublicationTests`/`CombatPreviewPublicationTests`. Review Buffer changes for data forwarding/validation only.

**Task 7 implementation rulings:**

- Reuse `AttackOcclusionResolver.ResolvePropagationCells` for targeting splash and generated Thorn cells. Generate Thorn footprints in the existing trait rule, filter before any occupant damage or field mutation, and leave existing fields on excluded cells intact. Build wall geometry once per attack and share it with admission and Thorn settlement.
- Current splash/Thorn footprints contain immediate neighbors only. Between legal adjacent non-wall cell centers, a straight wall cannot obstruct the segment through its open interior. Integrated tests therefore use wall-occupied neighbors; a longer propagation segment independently verifies genuine shadow filtering at the shared resolver boundary. No victim is placed illegally on a wall, and no authored footprint is enlarged for a test.
- Occupied cells also publish the occupying component's evidence, including when crossing is ignored. This reports occupancy exclusion without weakening it. Delivery and propagation enum interpretation remains in the resolver.
- Keep the established wall-independent hostile-only KillChain policy, environmental barrel damage, and persistent Thorn entry damage/consumption. Rejected initial attacks produce no effects or gameplay events.
- Add optional, defensively copied diagnostic collections to existing contracts. Preserve them through success/failure factories, preview conversion, completion attachment, automatic-command composition, and publication. Preserve source order when combining steps; diagnostics are separate from gameplay events and reason strings.
- Range diagnostics cover direct geometric cells only; per-aim preview/result include propagation. Raw range cells/direct target IDs and legal aims remain distinct, including when AP is insufficient.
- Additional copy-path closure includes `CombatCommandComposition`, `MovementProjection`, and `AttackBlockedContact.CopyCollection`. `BattleTransactions.Apply` already forwards the complete transition to `CombatStateBuffer.PrepareCommand`, so it needs no edit. No transaction body was added to `BufferSystemGroup`.

**Task 7 verification:** The accepted red run executed 33 tests: 23 passed, 9 intended failures, and 1 fixture-range failure. After implementation, 32/33 passed; the remaining out-of-range fixture was corrected. Expanded coverage passed 38/39; the final assertion was corrected to match the existing consume-on-entry Thorn policy, whose damage assertion already passed. Regression `wall-task7-regression.xml`: 1950/1950 passed, zero failures/skips/inconclusive (full Core 1722; selected Framework 228). Full Framework was not rerun; previously reproduced baseline failures remain documented under Task 2.

**Final focused verification:** `wall-task7-final.xml`: 39/39 passed after strengthening KillChain coverage to assert that its extra victim is actually behind a blocking segment. No production code changed after the full regression run. Unity exited, generated FMOD scan metadata was restored, and whitespace checks passed.

**Scope boundary:** Task 7 implementation and publication are complete. Changes remain uncommitted. View presentation and the Task 8 parity work are unstarted; no manual View smoke check is claimed.

**Deliverable:** Alternate effects and published failures obey explicit policies and expose useful diagnostics.

## Task 8: Connect View diagnostics and verify AI parity

**Create:** `TESTS/Framework/WallAttackInteractionTests.cs`, `TESTS/WallAttackAiTests.cs`.

**Modify:** `VIEW/AttackTargetingInteraction.cs`, `VIEW/ICombatInteractionFeedback.cs`, `VIEW/CombatSceneDisplay.cs`, `VIEW/Display/FieldHighlightLayers.cs`, `VIEW/Display/FieldView.cs`, `CORE/Application/Combat/EnemyAi/EnemyMovementPathfinder.cs`, `TESTS/Framework/CombatInteractionTests.cs`, `TESTS/EnemyMovementPathTests.cs`.

**Interfaces:** Add `ShowAttackBlockers(IReadOnlyList<AttackBlockedContact> contacts)` and `ClearAttackBlockers()` to the existing feedback boundary. The scene display unions the supplied WallCells and delegates to the existing field highlight facility; no ID lookup service or geometry reconstruction is needed. Add `FieldHighlightLayer.BlockedWall = 1 << 4`, preserve existing numeric flags, and resolve it to the existing HoverInvalid color with precedence above Affected and below pointer feedback. Keep this priority decision in FieldHighlightLayers, not the interaction coordinator.

- [x] Add a real Core range -> hover case for blocked Single omitted from AttackAims: hover is invalid, corresponding wall sources are highlighted from range diagnostics, no detailed preview is required, and clicking submits no attack.
- [x] Add partially blocked Direction/All and splash cases, invalid-to-valid hover, hover exit, canceled mode, stale preview answer, and battle teardown. Assert old blocker highlights clear and actor/cost preview behavior remains intact. Do not allow a blocked click to obtain diagnostics.
- [x] Run Framework `WallAttackInteractionTests` red; retain blocked-contact data from range/preview and forward it to feedback. Use existing request numbers to ignore stale answers. Do not calculate angles or ray intersections to derive wall legality in View.
- [x] Add Player/Enemy AI tests: a target across a wall causes pursuit toward a genuinely clear attack position; finite-budget piercing feasibility matches preview; ignoring walls affects attack legality only and still avoids wall-occupied movement cells. Reuse the Task 6 evaluation and Task 5 trace/budget.
- [x] Run Core `WallAttackAiTests`/`EnemyMovementPathTests` and Framework `WallAttackInteractionTests`/`CombatInteractionTests`. Perform one scene smoke check if the scene is available; otherwise report that presentation check as unperformed, not passed.

**Task 8 implementation rulings:**

- `AttackTargetingInteraction` indexes immutable range contacts by contact cell for mode-local hover lookup. Blocked Single cells remain unselectable, show their supplied blockers, and do not issue a detailed preview or an attack. Matching attack previews replace these diagnostics, including valid partial Direction/All attacks and impact-based splash evidence.
- Invalidate the owned hover request on every new hovered cell and pointer exit, including cells without a legal aim. Also require the answer to match the latest live request. Reset blocker/cache/effect/cost presentation on result refresh and mode exit; an old answer cannot restore it.
- `CombatSceneDisplay` unions supplied WallCells using reusable scratch collections and forwards them to the existing field layer API. `FieldHighlightLayers` owns BlockedWall = 1 << 4 and priority: pointer feedback > blocked wall > affected cells. Reuse HoverInvalid color. No geometry, wall ID lookup, new renderer, color authoring field, or per-pointer filtered contact allocation was introduced.
- `FieldView` already supports arbitrary layer bits through SetLayerCells/ClearLayer and resets them on release, so it needs no change. `EnemyMovementPathfinder.CanAttackFrom` already consumes common selection, the collected pierce trace, and ApplyBudget. The new nine AI cases and existing 26 pursuit cases pass without an additional AI branch; retain that implementation.
- Update the existing interaction feedback fake and CombatViewSystem fake for the expanded feedback interface. These are compatibility changes, not new production services.
- The existing CombatPointerMapping fixture fails during EventSystem setup in EditMode, as reproduced on the clean baseline. Keep its unrelated setup unchanged. The new authored-scene smoke test independently installs the real display without manipulating EventSystem.current; it tests supplied wall-cell union, actual field color precedence/restoration, empty replacement, and clear/reinstall. It skips if the user's Combat scene is already loaded rather than closing it.

**Task 8 verification:** Initial red `wall-task8-red.xml`: 83 executed, 72 passed, 10 intended View failures and 1 existing scene-fixture setup failure. After separating the scene smoke from that setup, `wall-task8-scene-red.xml`: 12 executed, 1 passed, 11 intended failures, including the real scene's missing blocker layer. First green `wall-task8-green.xml`: 83/83 passed. Full EditMode `wall-task8-full.xml`: 3411 executed, 3380 passed, 30 failed, 1 inconclusive; Core 1731/1731 and CombatViewBoundary 9/9 passed, Framework 1639 passed of 1670, Addressables example 1/1 passed. All 30 failing names and the inconclusive case occur in the clean baseline from Task 2; no new failing name. Full Framework remains non-green.

**Final focused verification:** `wall-task8-final.xml`: 85/85 passed, zero failures/skips/inconclusive. This includes result-refresh stale-answer cleanup, empty blocker replacement, the authored Combat scene smoke, and the existing highlight priority regression. No production code changed after the full suite run. Manual interactive visual inspection was not performed.

**Scope boundary:** Task 8 implementation is present and uncommitted. View belongs to another developer; the completed four presentation edits and related tests are documented as implemented work for that developer's integration context. Task 9 makes no further production View changes. See the [implementation handoff](../handoffs/2026-10-08-wall-attack-view-handoff.md).

**Deliverable:** Players can understand blocked attacks, and AI does not select unreachable firing positions.

## Task 9: Regression, architecture audit, and content handoff

**Modify:** This plan's evidence section and `docs/superpowers/handoffs/2026-10-08-db-runtime-data-requirements.md` only after inspecting and preserving any intervening user changes. Add wall placement/settings, attack occlusion enums, and migrated trait cap; retire old Cannon shape requirements. Keep language English.

- [x] Run full `ProjectL.Tests.Core`, affected `ProjectL.Tests.Framework`, and `ProjectL.Tests.CombatViewBoundary` using separate XML/logs. For Framework, include state publication, adapters, input/preview, scene display, lifecycle, and all new wall fixtures. Rerun only after changes or unresolved failures justify it.
- [x] Search for `CollectPierce`, `ImpactPattern.Range`, `ForEntity`, old Cannon fields, and every new wall policy consumer. Confirm remaining references are intentional documentation/history or migration evidence; no alternate gameplay collector or dead input remains.
- [x] Inspect Core asmdefs/references, helper signatures, BufferSystemGroup diff, and View diff against the spec's architecture checklist. Verify internal visibility and immutable arrays; do not create public helpers just to bypass assembly boundaries.
- [x] Audit authored assets, new metadata, no-wall behavior, all constructor/copy paths, raw-range versus actionable-aim contracts, and block diagnostics ordering. Distinguish intentional field migration from unrelated Unity/FMOD-generated file changes; do not discard user edits.
- [x] Update the DB handoff with final schema and defaults. Record unsupported wall topology and explicit special-effect exceptions so future content authors do not infer behavior from category names.
- [x] Record actual commands, test totals, failures/skips, final fixes, and manual coverage limits below. Run `git diff --check`, inspect status including untracked files, and retain reviewed changes uncommitted.

### Task 9 execution evidence

Task 9 changes documentation only. Production and test changes from Tasks 1–8 remain byte-equivalent in the tracked diff after restoring only Unity-generated FMOD scan metadata. No commit, push, scene, prefab, assembly-definition, or BufferSystemGroup change was made.

| Separate fresh EditMode run | Total | Passed | Failed | Inconclusive | Skipped |
| --- | ---: | ---: | ---: | ---: | ---: |
| `wall-task9-core.xml` / ProjectL.Tests.Core | 1731 | 1731 | 0 | 0 | 0 |
| `wall-task9-framework.xml` / ProjectL.Tests.Framework (full assembly) | 1671 | 1640 | 30 | 1 | 0 |
| `wall-task9-boundary.xml` / ProjectL.Tests.CombatViewBoundary | 9 | 9 | 0 | 0 | 0 |

Each run used Unity `6000.0.73f1`, `-batchmode -nographics -projectPath D:/My_ProjectWork/Github_desktop/Project_L -runTests -testPlatform EditMode -assemblyNames <assembly>` with separate `-testResults` and `-logFile` under `.superpowers/sdd/2026-10-08-wall-attack-blocking/test-results/`. No `-quit` was supplied; the Editor exited before each subsequent invocation. The complete commands and failure list are retained in `task9-verification.md` in that workspace.

All 30 current Framework failing names **and failure messages** match the clean Task 2 baseline; zero new/changed failures. The same StageMapViewSystemTests.EnterNode_WithoutMainCamera_Throws remains inconclusive. The baseline had one additional ArtifactBoardMapper failure that passed in this run; Task 9 does not claim to fix it. Full Framework is explicitly non-green. The additional passing test relative to Task 8's full run is the result-refresh case already verified in Task 8's final focused run. WallAttackInteraction 13/13, WallAttackPublication 3/3, WallStatePublication 9/9, adapters, related publication/preview/buffer/entry fixtures, and the authored scene wall smoke passed. Manual interactive visual review was not performed.

### Architecture and migration audit

| Gate | Evidence and conclusion |
| --- | --- |
| Responsibility and dependencies | Internal WallGeometryBuilder owns topology; WallSegmentClipper exact math; AttackOcclusionResolver obstruction policy; PierceTargetCollector ordered spatial encounters and pure budget truncation. Application composes admission/settlement; Buffer validates/publishes; View renders supplied evidence. All three Core asmdefs retain noEngineReferences; no Unity imports were introduced. |
| Shared rules | Existing target eligibility retained, including ordinary empty/friendly rejection. One ordered ray definition and one spatial collector serve preview, execution, and AI. Only the resolver interprets delivery/propagation ignore policy; no unit-key wall branch or alternative store/registry exists. |
| Immutable contracts/copies | Wall settings checked through initialization, preparation, movement/turn/command state construction and Buffer validation. Evidence copied through selection, range, preview, attack, rejection, composition, movement projection, WithCompletion and public result. Contact ID/cell arrays are independently sorted sets for a run, not zipped pairs. |
| No-wall and exact-geometry behavior | Full Core includes legacy cap outcomes, no-wall lifecycle/targeting regressions, arbitrary slopes, corner/seam/tangent cases, independent polygon-oracle comparison, partial structure contacts, physical stops, generated Thorn, and explicit KillChain/environmental exceptions. |
| Retired inputs | No gameplay/asset reference to CollectPierce, ImpactPattern.Range, ForEntity, or retired Cannon fields remains. The sole C# search hit is a reflection assertion that ForEntity is absent. Historical docs/migration evidence remain intentional. Runtime EntityIds remain intact. |
| Asset hygiene | Five unit assets remove only three obsolete Cannon fields each; none has an authored piercing trait requiring cap migration. New default wall/policy fields were exercised through real asset tests. All 21 new C# scripts have metadata and GUIDs unique across Assets. Whitespace checks pass. |
| Coordinator limits | BufferSystemGroup, Core assembly definitions, scenes and prefabs have no feature diff. Existing DevBattleInitializationSource has a one-line Task 2 constructor compatibility migration, documented separately from the Task 8 presentation changes. |

Fresh read-only review by an Astra reviewer found no actionable findings and no deferred minor findings. It reviewed the working-tree diff plus new files because all work is intentionally uncommitted. Reviewer exclusions were explicitly resolved: existing Framework failures stay outside this feature; manual appearance remains unverified; broader AI geometry caching is deferred without measured regression; the completed View changes remain implemented and are documented for the View developer; final handoff wording and test evidence were checked by the implementing agent.

### Task 9 rulings and handoff limits

- Retain the completed View implementation and provide an [English implementation handoff](../handoffs/2026-10-08-wall-attack-view-handoff.md) listing changed files, behavior, public contracts and verification. This resolves the earlier pending-decision framing. Integration impact: the View developer needs awareness of the new feedback methods and highlight layer when merging concurrent work.
- Complete the audit with exact baseline-matched Framework failures documented, rather than treating the entire Framework as passing. Cost: those pre-existing UI/lifecycle failures remain unresolved and require separate ownership.
- Keep per-query derived geometry and existing query boundaries. No measured regression justifies a new cache/service. Cost: large maps may later require profiling and scoped reuse work.
- Automated authored-scene coverage verifies rendering state but does not certify visual quality. Limit: interactive appearance/usability has not been manually checked.
- Use a working-tree review package and retained local evidence instead of empty commit-range automation. Cost: review evidence stays local until the user commits the implementation/documents. No worktree or original evidence was deleted.

The [DB handoff](../handoffs/2026-10-08-db-runtime-data-requirements.md) now specifies WallCells/CornerCutTicks, explicit delivery/propagation enums, required MaxTargetsPerRay (including zero), retired Cannon inputs, unsupported topology, lifetime/occupancy restrictions, and effect exceptions. Its unrelated inventory sections were preserved. Current piercing compatibility remains EightDirectionLine + Direction + no splash, with three rays and cardinal selection; arbitrary-slope obstruction does not change targeting availability.

**Deliverable:** Verified implementation with an auditable boundary review and matching data contract documentation.

## Coverage and handoff

| Spec requirement | Owning tasks |
| --- | --- |
| Exact arbitrary-slope clipping, strict boundaries, source topology | 3, 6 |
| Static wall intake, settings propagation, occupancy, lifecycle | 4 |
| Eligibility preservation, aim cleanup, per-ray cap migration | 1, 2, 6 |
| Shared ray definition, trace/budget separation, AI feasibility | 5, 8 |
| Splash/Thorn and explicit special-effect exceptions | 6, 7 |
| Structured failure data, immutability, atomic publication | 6, 7 |
| Blocked hover without valid aim, stale response cleanup | 8 |
| Responsibility boundaries, no engine dependencies, DB handoff | All tasks; final gate 9 |

Dependency order is 1 -> 2 -> 3 -> 4 -> 5 -> 6 -> 7 -> 8 -> 9. Each task has its own focused review gate; do not ship the intermediate state with walls loaded but not enforced by attacks. Direct implementation in the same session is a reasonable default because the tasks share evolving contracts; independent review can be requested at task boundaries or at the final diff. Do not dispatch agents until the execution method is selected.

### Execution evidence

The initial planning pass performed no implementation or gameplay tests. Execution results are recorded under each completed task above; unchecked tasks remain unimplemented. Do not copy historical passing counts from another feature as evidence for walls.
