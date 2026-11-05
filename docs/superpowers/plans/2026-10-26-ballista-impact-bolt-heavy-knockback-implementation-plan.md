# Ballista Impact Bolt and Heavy Knockback Immunity Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. Preserve the user's phase review stops and prohibition on commits/pushes. This document authorizes no implementation by itself.

**Goal:** Replace Ballista's piercing base trait with SO-configurable, impact-centered knockback, disable Heavy category knockback immunity reversibly, and hand presentation integration to the View owner.

**Architecture:** Extend the existing trait registry and effect contribution contract; reuse the current forced-movement executor, damage pipeline, publication, and Buffer ownership. Targeting and numeric balance remain in InGame SO/data, with immutable Core snapshots exposing configured values. No Ballista entity-key branch, duplicated movement engine, or new Framework service.

**Tech Stack:** Unity 6000.0.73f1, C#, ScriptableObject authoring, NUnit EditMode tests in ProjectL.Tests.Core and ProjectL.Tests.CoreIntegration only.

**Spec:** [Approved direction and revised balancing contract](../specs/2026-10-26-ballista-impact-bolt-heavy-knockback-design.md).

**Status:** Phases A–C completed on 2026-10-26 for Core/InGame, the two allowed test assemblies, and the prepared local View handoff. View implementation and manual acceptance remain pending. The original planning pass did not modify code or execute tests.

## Global Constraints

- Scope: Core logic, InGame authoring/assets, Core-facing publication, and handoff documentation. The user is the Core programmer.
- Modify and execute tests only in `ProjectL.Tests.Core` and `ProjectL.Tests.CoreIntegration`; do not migrate, modify, or execute Framework/View/CombatViewBoundary tests.
- Do not edit ViewSystem, ViewGroup assets, UI presentation, FieldCamera, or Architecture. Core result compatibility is handed off, not simulated by fake piercing records.
- Initial Method values are **5 / 1 / 0 / 3 / 70** (shape / choice mode / angle / splash / percentage). Initial attack distance is **4**, Impact Bolt distance **1**. They are SO defaults, never hard-coded runtime requirements.
- SO-editable shape, angle, splash, percentage, attack distance, and positive knockback distance must flow into actual queries, preview, execution, and relevant snapshots.
- Only Single choice mode supplies the required center. Reject centerless modes and incompatible displacement-origin compositions, not alternate valid balance settings.
- Preserve existing category rules except Heavy's automatic attack-knockback immunity. Explicit unit forced-movement immunity remains effective.
- English documents; Korean comments/XML documentation; Allman braces, four spaces, UTF-8/LF, explicit accessibility, existing namespaces. New C# files require unique `.meta` files; existing asset GUIDs remain unchanged.
- No commit or push. No cache deletion or Unity upgrade. Run Unity through an approved AppData-capable execution path.
- Stop after each Phase A/B/C and report its actual evidence before proceeding to the next phase.

## Review Focus

1. Alternate SO settings must change behavior, not just serialize correctly: Task 3 tests distance 2, range 6, 40% Cross splash, non-radial shape, facing changes, and None splash.
2. Zero final damage, empty impact cells, and center occupancy must not corrupt direction/eligibility: Task 2 tests no-damage displacement, empty-center neighbors, and center exclusion before zero-vector validation.
3. Nearby allies, structures, and targets not directly delivered must not inherit Impact Bolt from ledger side effects: Task 2 tests the delivered-hit boundary and unaffected ordinary Knockback.
4. Heavy must traverse terrain after immunity is disabled while explicit immunity still wins: Tasks 1/2 cover thorns, mines, blocking, and preview agreement.
5. Repeated preview and stale publication must not mutate or partially apply battle state: Task 4 covers preview purity, stale target position rejection, and idempotent publication.

## Paths and responsibility map

All paths are repository-relative. Prefixes below are literal path abbreviations, not new folders:

| Prefix | Exact directory |
| --- | --- |
| C | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator` |
| CS | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CharacterSystem` |
| D | `Assets/ProjectL/Script/Systems/DataBaseSystem/Data/InGame/Table` |
| A | `Assets/ProjectL/DataBaseSO/InGameGroup` |
| T | `Assets/Tests/EditMode` |
| I | `Assets/Tests/EditMode/CoreIntegration` |

- Trait identity/configuration: new ImpactBolt contract, definition, authoring, and snapshot types. No persistent position, center, or distance counter.
- Assembly: existing `TraitRegistry`, `TraitSpecAdapter`, effect codec/provider, and command snapshot conversion. Follow the current Pull/Knockback patterns; do not restructure unrelated traits.
- Displacement: one explicit mode added to the existing internal contribution, consumed by `AttackKnockbackCalculator`. Impact Bolt's target/direction rule lives in the already-planned `ImpactBoltTraitRule`; all movement application stays in existing movement infrastructure.
- Owned-card data: append an optional Impact Bolt description snapshot to the existing Core owned-character contract; View consumption is external.
- Heavy: one enable flag and preserved category rule in `KnockbackImmunityPolicy`.
- Production defaults: Ballista's unit asset and dedicated method asset only; keep key 15/category Cannon and method key 7/GUIDs.

## Implementation boundaries and prohibited shortcuts

Review basis: the user-supplied `feature-change-locality-guidelines.md` at `D:/My_ProjectWork/학교 관련 문서/졸업작품_프로젝트L/2학기 내용/AI 관련 문서/`, checked against the owning sources below. The document supplies review criteria, not additional code-edit authority. The following are concrete implementation requirements, not requests to refactor the whole subsystem.

### Required paths and ownership

| Boundary | Required path / owner | Conditions and failure behavior |
| --- | --- | --- |
| Authored configuration | `UnitDataSO.GetData()` / `TargetingMethodDataSO.GetData()` → immutable Data → `TraitSpecAdapter.Read` / `BattleDefinitionAdapter.ReadAttackProfiles` | Validate at the existing boundaries. Keep distance on the trait and targeting parameters on the method. No code-side tuple override, clamp, fallback default, or alternate profile factory. |
| Trait installation and command capture | `TraitRegistry.CreateDefault` → existing factories/provider → `CombatCommandEffects.Capture` → `CombatEffectEngine.EvaluateKnockback` | Add the new registration and codec cases. Invalid origin/state/mode is an error; do not catch it and silently omit Impact Bolt or substitute ordinary Knockback. |
| Target and direction rule | `ImpactBoltTraitRule.TryResolveStep` using `HostilityPolicy` and `ForcedDisplacementGeometry` | Pure decision over supplied facts; no DB lookup, Buffer writes, movement, damage, or global state. Returns false for ineligible targets/center; invalid required arguments throw. |
| Displacement and order | `AttackDeliveryExecutor` → `AttackKnockbackCalculator` → `AttackForcedMovement.ExecuteUnit` / `ResolveArrivals` | Existing shared mechanics own occupancy, terrain, normalization, event recording, and arrival. Add only mode dispatch and feature-rule consumption to the coordinator. |
| Publication | Existing result factories, command/preview services, and `BattleWriteSink` | Calculators change only command-local projections. No direct live-store writes, second result application, service lookup, event subscription, or fake piercing result to make presentation pass. |
| Display facts | Resolved trait definition → combat status / `OwnedCharacterStatsCalculator` → immutable description | These are derived reads, not configuration stores. No View/InGame cross-read, UI-specific distance calculation, or default distance when a required snapshot is absent. |

Configuration lifetime: the SO is read during the existing definition/setup path. Test each edited configuration in a freshly initialized runtime. This work does not add live asset polling, hot reload, or a mutable global balance cache. An installed battle continues to use its installed immutable definition according to existing lifecycle rules.

Execution order is part of the shared contract: direct damage/reflection/deaths → attack knockback with per-step terrain → existing ThornField application → action settlement → hit-health capture → deferred final objects/arrival → existing follow-up stages. `AttackDeliveryExecutor` already suppresses displacement after reflected attacker death or when direct delivery is disabled. Do not reorder these stages, revive deferred traits, or duplicate the sequence inside Impact Bolt. Earlier targets' completed movements affect later occupancy through the existing projection; preserve entity-ID ordering.

Impact Bolt filters displacement only. Do not mutate `plan.Delivery.Hits`, redefine Cannon damage eligibility, or suppress center damage when its movement rule returns false. Returning no movement is a normal effect outcome; a missing required configuration/state is an error, not grounds to substitute distance 1 or return a fake success.

### Defaults, invariants, and validation are different

- **Editable defaults:** distance 1, attack range 4, method 5/1/0/3/70. Default literals belong in authoring/initial assets and explicit fixture data; production execution reads the installed values. Changing balance must not require editing a factory, registry, command handler, or description formatter.
- **Semantic invariants:** one impact center, outward direction, center exclusion, hostile-unit eligibility, preserved explicit immunity, positive configured distance. Single is required by the current center contract. These are not reasons to require RadialSquare, Radial splash, 70%, range 4, or distance 1.
- **Existing DB validation:** `D/Targeting/Item/TargetingMethodData.cs` requires positive percent for a non-None splash and zero for None. Do not loosen this validation. `TargetingMethodSpec` separately permits a zero-percent non-None splash; keep that case as a Core-only synthetic-profile regression. An SO with Cross/Radial + 0 must still fail at the authoring/adapter boundary.
- **Identifiers:** trait key 210 and asset/method keys identify definitions; they are not tuning knobs. A legitimate `TraitKey.ImpactBolt` registration/codec case is allowed. `EntityKey.Unit_Ballista`, unit names, method key 7, or serialized numeric tuple checks must not select special runtime behavior.
- **Heavy flag:** the named disabled policy flag is the specifically requested reversible feature disable. It is not a per-unit distance or other balance default, and does not justify introducing a general runtime flag framework.

### Locality requirements for reviewers

| Real change scenario | Edit and understanding boundary after implementation | Required regression and reason |
| --- | --- | --- |
| Tune distance, shape, splash, percentage, or range | SO values and their documented validity; understand the Data/spec handoff, not Pull, terrain, mines, or playback internals | Configuration-to-execution tests prove propagation. Runtime code/registry must remain untouched for balance-only edits. |
| Change Impact Bolt's eligible targets or direction | `ImpactBoltTraitRule` and its behavior tests; shared executor consumes its output | Core behavior plus ordinary Knockback regression because the mode dispatch shares an executor. Movement mechanics must not be copied into this rule. |
| Install the new trait | Registration/factory/codec/provider/adapter cases | This is necessary assembly work, not a locality violation. No registry redesign, reflection auto-discovery, per-trait service/Bootstrap, or new DI layer. |
| Change shared occupancy/terrain rules | Outside this feature's requested changes; current owners are `AttackForcedMovement` and movement calculators | Existing movement/terrain regressions remain necessary. A failing edge case does not authorize changing shared semantics without documenting and reviewing that deviation. |

Do not make unrelated extraction, renaming, formatting, public-contract replacement, service registration, or test-suite migration part of this feature. A file count target is not a design constraint: retain required existing extension files, and do not add forwarding-only layers to look modular. If a listed contract cannot support the requirement, report the exact mismatch and minimum proposed change at the phase gate; do not create a bypass. Any new production edit outside the file map needs a recorded dependency reason and scope review, not silent expansion.

At each phase gate, inspect the production diff for hard-coded unit/method selection, repeated distance/percentage literals, swallowed validation errors, runtime DB reads, direct Buffer writes, duplicated path/damage loops, and test-only exceptions. Do not reject legitimate enum cases or data defaults merely because a text search matches. Report actual call-path evidence and unverified limitations; passing tests alone do not establish these structural properties.

## Verification commands

Before implementation, recheck `git status`, applicable instructions, the current trait keys, and the test fixtures. The observed checkout was `feature/Cheon/Core/NewCombatSytem`; do not switch branches or overwrite concurrent changes merely to match this observation. Establish fresh baselines for the two allowed assemblies before code changes, then use focused filters during tasks and full allowed-assembly runs at the final gate.

Use this PowerShell template through the approved execution path; launch one Unity process at a time. Read the `unity:unity-cli` skill before executing Unity. If the project is already open, use its supported Editor test path or an isolated checkout rather than launching a second Editor on the same project.

```powershell
$boltProject = 'D:/My_ProjectWork/Github_desktop/Project_L' # use the actual implementation checkout
$boltEvidence = Join-Path $boltProject '.superpowers/ballista-impact-bolt'
New-Item -ItemType Directory -Force -Path $boltEvidence | Out-Null
$boltAssembly = 'ProjectL.Tests.Core'
$boltLabel = 'baseline-core' # unique per run; integration uses its own label
$boltArgs = @(
    '-batchmode', '-nographics', '-projectPath', ('"' + $boltProject + '"'),
    '-runTests', '-testPlatform', 'EditMode', '-assemblyNames', $boltAssembly,
    '-testResults', ('"' + (Join-Path $boltEvidence ($boltLabel + '.xml')) + '"'),
    '-logFile', ('"' + (Join-Path $boltEvidence ($boltLabel + '.log')) + '"')
)
# For focused runs append: $boltArgs += @('-testFilter', 'ProjectL.Tests.Core.ImpactBoltTests')
$boltProcess = Start-Process 'D:/Program Files/Unity/Hub/Editor/6000.0.73f1/Editor/Unity.exe' -WindowStyle Hidden -PassThru -ArgumentList $boltArgs
$boltProcess.Id
```

Monitor the returned process and logs with bounded checks, keeping progress visible. Run the same template separately with `$boltAssembly = 'ProjectL.Tests.CoreIntegration'`. **Keep `-assemblyNames` even when adding a test filter.** For a full assembly run omit only the filter. Do not use an unrestricted EditMode run. Require fresh XML with the selected tests executed, compilation success, and the expected test results; process exit alone is insufficient. For TDD red runs, record the intended assertion failure, not a missing tool or unrelated compilation failure.

## Phase A — Core rules and displacement

### Task 1: Disable Heavy's automatic knockback immunity reversibly

**Files:** Modify `C/Domain/Combat/KnockbackImmunityPolicy.cs`, `T/KnockbackImmunityPolicyTests.cs`, and the Heavy-specific cases in `T/KnockbackPathTests.cs`.

**Interfaces:** Keep `internal static bool IsImmune(CategoryKey targetCategory, bool isForcedMovementImmune)`. Add an internal three-argument overload with `bool heavyCategoryImmunityEnabled`; the production two-argument entry supplies one named, disabled policy flag. This makes the preserved rule testable without mutating global test state. No caller gains a runtime settings dependency.

- [x] Add/update behavior assertions: Heavy without explicit immunity returns false in production; explicit immunity returns true for every category. Verify the preserved enabled branch using `IsImmune(CategoryKey.Heavy, false, true) == true` and the disabled branch with the last argument false.
- [x] Run focused `KnockbackImmunityPolicyTests` and `KnockbackPathTests` in Core; record expected red failures for the new production behavior.
- [x] Preserve the original Heavy comparison behind the flag and keep explicit immunity independent. Update Heavy movement tests to assert actual movement, thorn traversal, mine activation, and preview/commit agreement; retain lethal-hit and tile-forced-movement coverage.
- [x] Rerun those fixtures; require all updated expectations and unaffected cases to pass. Inspect the diff to ensure no Heavy stats/protection/category assembly changed.

### Task 2: Register configurable Impact Bolt and execute center-based displacement

**Create:**

- `C/Contracts/Commands/Traits/ImpactBoltTraitSpec.cs`
- `C/Contracts/Snapshots/Traits/ImpactBoltTraitStatusSnapshot.cs`
- `C/Domain/Traits/Definitions/ImpactBoltTraitDefinition.cs`
- `C/Domain/Traits/States/ImpactBoltRuntimeState.cs`
- `C/Domain/Traits/Rules/ImpactBoltTraitRule.cs`
- `C/Domain/Traits/Effects/ImpactBoltEffectStateSnapshot.cs`
- `C/Domain/Traits/Effects/ImpactBoltEffectRule.cs`
- `C/Application/Combat/Traits/ImpactBoltTraitFactories.cs`
- `T/ImpactBoltTests.cs` (include its small fixture helpers locally; reuse existing command/movement fixture APIs).

**Modify:**

- `C/Contracts/Common/TraitKey.cs`, `C/Contracts/Combat/CombatAttackProfile.cs`
- `C/Application/Combat/Traits/TraitRegistry.cs`, `TraitEffectProvider.cs`
- `C/Application/Combat/CombatCommandEffects.cs`, `AttackKnockbackCalculator.cs`
- `C/Domain/Traits/Effects/TraitEffectStateCodec.cs`
- `C/Domain/CombatEffects/Evaluations/EffectKnockbackContribution.cs`, `EffectKnockbackEvaluation.cs`
- `C/Domain/CombatEffects/CombatEffectEngine.cs` only for validation of the new contribution mode.
- `T/EnhancementPrecisionShotTests.cs` for the existing enhancement's interaction with the new profile.

**Interfaces:**

- Add `TraitKey.ImpactBolt = 210` after rechecking that the serialized ID is unused; never recycle retired ID 120. Collision is a reason to amend the plan before choosing another key.
- Public spec, definition, and status types each take `int distanceCells` and expose get-only `int DistanceCells`; reject values below 1. Snapshot equality includes distance. Runtime/effect states carry identity only, following existing stateless traits.
- Factories implement the existing `BuildDefinition(TraitSpec)` and `BuildRuntimeState(TraitDefinition)` contracts; `ImpactBoltTraitRule.Key` returns ImpactBolt.
- In the same `ImpactBoltTraitRule`, add `internal static bool TryResolveStep(Owner attackerOwner, CharacterCombatFacts target, CellCoord center, out CellCoord step)`. It checks living field-unit eligibility through existing facts and `HostilityPolicy`, excludes center-containing footprints, and delegates the step to `ForcedDisplacementGeometry.ResolveBranchStep`. It does not check terrain/immunity, choose distance, apply movement, or receive `CommandAttackPlan`, a world projection, or services. This places one feature's decisions in its existing rule file without a new abstraction layer.
- Add internal `EffectKnockbackMode { AttackerOrigin = 0, ImpactCenter = 1 }` alongside the contribution. Extend its constructor to `EffectKnockbackContribution(EffectOriginKey origin, int distanceCells, EffectKnockbackMode mode = EffectKnockbackMode.AttackerOrigin)` and expose `Mode`. Existing callers preserve their behavior.
- `ImpactBoltEffectRule.EvaluateKnockback(in EffectRuleInput input)` emits the configured distance with ImpactCenter mode and validates matching origin/state. Do not change `IEffectKnockbackRule` or add a parallel engine.
- Preserve `AttackKnockbackCalculator.Resolve(...)` and `ResolveArrivals(...)` signatures. Keep target eligibility/direction selection local to this displacement feature; existing executor owns path application.

- [x] Add parameterized `ImpactBoltTests` with center `(4,4)` and each offset `(dx,dy)` from the eight neighbors. At distance 1, assert final anchor `(4+2*dx, 4+2*dy)` on an unobstructed board; center target stays `(4,4)`. Place the shooter elsewhere and repeat to prove shooter independence. Use high-health targets and a nonterminal stage fixture.
- [x] Add distance-2 expectations `(4+3*dx, 4+3*dy)`; center remains unchanged. Add empty-center, lethal-target, explicit-immunity, Heavy, blocked-destination, board-edge, and diagonal cases. Add a wall-blocked direct-delivery variant: no delivered hits means no movement and no missing-center exception. With unmodified damage and defenses disabled, assert base damage 100 gives 100 center damage and 70 surrounding damage; use a separate normal-defense case for the existing pipeline.
- [x] Add zero-splash-percentage **Core synthetic-profile** and fully defended cases: eligible living neighbors still move. This does not authorize zero-percent non-None SO authoring. Assert allied units/structures retain position while their existing damage eligibility remains unchanged, undelivered targets do not move, and ordinary Knockback still moves its supported obstacles. Reuse mine/thorn fixtures for path effects and a terminal-state case for early stopping. Verify reflected attacker death suppresses displacement through the existing executor path rather than a feature-specific override.
- [x] Add profile tests allowing Single with Cross, Diagonal, Radial, or None splash, alternate shapes/angles/percentages, and range independent of the trait. Reject Direction/All and Impact Bolt combined with ordinary Knockback or PiercingFalloff. Validate distance 0/-1 and unknown contribution modes.
- [x] Run the focused Core fixtures, recording intended failures. Implement the trait registration, codec, provider, and immutable command capture paths together so authoring-independent profiles can execute.
- [x] Add a feature-identity regression: the same installed Impact Bolt trait on another valid unit key behaves identically, while Ballista without that trait has no impact knockback. Do not grant the effect based on its name, category, or method ID. Assert invalid binding origin/state is rejected rather than bypassing registry/capture.
- [x] Extend the existing displacement executor by mode. For ImpactCenter, return no displacement when `plan.Delivery.Hits` is empty (including a wall-blocked center). Otherwise take the current `plan.Selection.ReachableCells.PrimaryCell` and require it explicitly; delivered hits without a center indicate an inconsistent plan. Do not invent a `PrimaryCell` property on `ResolvedAttackDelivery`, which has only hits/affected cells. Candidate IDs come from `plan.Delivery.Hits`, distinct and entity-ID ordered. Skip structures, call `ImpactBoltTraitRule.TryResolveStep` with current unit facts, then use the existing shared immunity policy before movement. Do not repeat hostility/center/sign rules in the executor, filter on positive damage, or recreate splash geometry. Preserve ordinary mode's existing candidate/obstacle behavior and distance aggregation. Conflicting modes must fail clearly, never collapse via distance maximum alone.
- [x] Apply movement with `AttackForcedMovement.ExecuteUnit`, retaining `deferLastObjects: true`, `publishMovedEvents: true`, terminal checks, and the existing arrival stage. Do not alter damage/arrival/cost ordering or add persistent center state.
- [x] Run focused `ImpactBoltTests`, `KnockbackPathTests`, `KnockbackTraitTests`, `TraitEffectRuleTests`, `PiercingShotCommandTests`, `PullAttackTests`, `PullPreviewTests`, and `ThornFieldTraitTests` within Core. Extend and run `T/EnhancementPrecisionShotTests.cs` for the new profile: preserve its first-recipient behavior while additional damage/displacement follows the authored splash. Run the Core assembly once for the Phase A gate.

**Phase A stop:** Report changed Core contracts, proof of configurable distance, Heavy reversal behavior, focused/full Core results, and remaining authoring work. Include the actual rule → contribution → executor path and any changed shared contract, distinguishing necessary regression from accidental coupling. Production Ballista assets are still unchanged. Do not begin Phase B without the phase review.

## Phase B — InGame defaults, configuration propagation, and publication

### Task 3: Connect SO parameters and expose configured values to consumers

**Create:** `D/Unit/Trait/ImpactBoltTraitAuthoring.cs`, `D/Unit/Trait/ImpactBoltTraitData.cs`, `I/ImpactBoltAuthoringTests.cs`, `I/ImpactBoltBattleSource.cs`.

**Modify:** `CS/TraitSpecAdapter.cs`; `C/Contracts/Snapshots/OwnedCharacterStatsSnapshot.cs`; `C/Application/Roster/OwnedCharacterStatsCalculator.cs`; `A/Unit/Item/Playerable/Unit_Ballista.asset`; `A/Targeting/Item/TargetingMethod_BallistaAngle.asset`; `I/CoreIntegrationTestData.cs` only for the input configuration seam below.

**Interfaces:**

- `[Serializable] internal sealed ImpactBoltTraitAuthoring : UnitTraitAuthoring` owns `[SerializeField, Min(1)] private int distanceCells = 1`; `ToData()` creates a new `ImpactBoltTraitData(distanceCells)`.
- `public sealed ImpactBoltTraitData : UnitTraitData` exposes get-only `DistanceCells`, returns ImpactBolt as its key, and rejects values below 1 in `Validate()`. No UnityEngine dependency in this pure data file. `TraitSpecAdapter.Read` forwards the configured value into Task 2's spec.
- Append optional `ImpactBoltTraitStatusSnapshot impactBoltDescription = null` to `OwnedCharacterStatsSnapshot`'s existing constructor and expose `ImpactBoltDescription`. Follow existing key/description presence checks. `OwnedCharacterStatsCalculator` derives it from the resolved definition, including per-owned-instance enhancement resolution; do not add another editable source.
- `ImpactBoltBattleSource : IBattleInitializationSource` implements `Read(IDataBaseQueryContext database, int chapterKey, StageKey stageKey)` and accepts constructor inputs `TraitSpec trait, TargetingMethodSpec method, int range = 4`. Build only the fixture adaptations needed for this feature using `ConfiguredMovementBattle.EncounterSource` and existing `ConfiguredMovementBattle`; do not build a second service assembly fixture.
- `CoreIntegrationTestData` gets `internal void SetUnit(UnitData data)` and `internal void SetTargetingMethod(TargetingMethodData data)`, each null-checking/validating the supplied immutable input. Keep the original method key 1/default row and use a dictionary for additional method rows; all Get/TryGet/GetAll methods must observe the same collection and retain missing-key behavior. Configure before `ConfiguredMovementBattle` constructs its runtime. This is a test DB input seam, not a production balance store or a way to edit live Buffer facts.

- [x] Add authoring-to-spec/installed-status round-trip cases for distances 1 and 2 and invalid 0/-1. Assert combat status and owned-character description both expose the configured distance. Mutating a temporary SO after `GetData()` must not mutate the already-produced data or the installed battle's snapshot; a new runtime must receive the edited values.
- [x] Add SO-edit tests using temporary in-memory `UnitDataSO`/`TargetingMethodDataSO` instances or cloned assets, disposed in `finally`. Feed their exact `GetData()` results into the fixture DB before runtime construction; use `ConfiguredMovementBattle(..., database: configuredData)` with its default production battle source. Its Actor is currently the Cowboy fixture key: adapting only the clone's identity to that fixture is allowed and proves the feature is trait-based; separately verify the real Ballista asset. Do not reconstruct a matching trait/method by hand after reading the SO, or inject final facts to make propagation pass.
- [x] Verify the owned-character snapshot through that configured DB and Core service as well. `ConfiguredMovementBattle.OwnedStats` is captured before battle entry; a custom `IBattleInitializationSource` alone cannot configure it. Do not patch the captured snapshot, add a View lookup, or weaken key/description checks. `ImpactBoltBattleSource` is reserved for deliberately authored combat/publication scenarios, not proof of SO/roster propagation.
- [x] Test alternate distance 2 + 40% Cross splash: with base damage 100 and zero defense/modifiers, cardinal neighbors lose 40 health and move 2 steps, diagonal neighbors are unaffected. Pin the full multi-target geometry in Core; through the production-source CoreIntegration fixture, prove the same authored distance/percentage on an in-bounds victim and query/preview/commit. Also test range 6 vs 4, a non-radial Single shape, Front vs Unrestricted selection, and None splash with percentage 0. Assert non-None + 0 is rejected by the existing DB validation. Assert query membership/acceptance, preview, and final state—not just copied fields. Keep failure causes isolated so one invalid cell does not mask another parameter.
- [x] Add initial-production assertions separately: unit key 15, Cannon, unchanged stats/range 4, Impact Bolt distance 1, no PiercingFalloff, method key 7 with tuple 5/1/0/3/70, arcing wall delivery, unchanged propagation. Prove shared targeting assets and other units' piercing configuration are not mutated; do not assert View trait visuals exist yet.
- [x] Run focused CoreIntegration authoring tests to establish red. Implement authoring/adapter/owned-data propagation, then update only the two specified InGame assets. Preserve GUIDs, unrelated stats, other trait implementations, and shared methods. Wall delivery uses the existing IgnoreWalls enum value, not a new hard-coded exception.
- [x] Rerun the focused tests and verify both edited values and initial assets. Record that production Ballista is not playable through its old View presentation until the handoff is implemented.

### Task 4: Verify real Core service publication and prepare the View contract

**Create:** `I/ImpactBoltPublicationTests.cs`.

**Read/reuse, do not redesign:** `I/ConfiguredMovementBattle.cs`, `CoreIntegrationSnapshot.cs`, `CoreIntegrationTestData.cs`; existing command/preview services and `BattleWriteSink` publication. Correct a feature-specific missing Task 2/3 mapping at its owning source, not in test adapters or View code.

**Interfaces:** Consume Task 3's battle source through existing Core services. Expected output is the existing attack result with `PiercingShot == null`, configured trait snapshots, current selection/delivery facts, and existing movement events/final anchors. Do not add a public ImpactBolt event solely for animation.

- [x] Through CombatPreview, repeat the same preview three times; compare `CoreIntegrationSnapshot`/gameplay capture before and after. Assert surrounding damage/displacement matches one committed attack and center position is unchanged.
- [x] Submit once through CombatCommands; verify existing AP/action settlement happens once, all moved targets publish correct before/after facts, no center movement event appears, and no piercing payload is fabricated. Repeat command processing to assert no second charge/movement/publication.
- [x] Calculate then change a victim position before applying the result; expect stale-publication rejection and unchanged stores. Restore expected state, apply once, and reapply to exercise existing idempotency. Preserve established all-or-nothing publication.
- [x] Run Heavy and explicit-immunity variants through these service paths. Assert Heavy reaches its destination while an explicitly immune target keeps its position with normal damage behavior.
- [x] Run the new publication fixture and Task 3's authoring fixture in CoreIntegration. Investigate failures at the actual owning contract; do not relax assertions or modify excluded test assemblies.

**Phase B stop:** Report Core/CoreIntegration results, modified InGame assets, exact published configuration/positions, and the known View incompatibility. Identify which tests traversed SO → Data → production adapters/source → Core services, versus synthetic profile fixtures. Show the battle and owned-description distance sources agree. Stop before Phase C review/closure work.

## Phase C — Regression evidence and View handoff

### Task 5: Complete allowed regression and write the handoff

**Create:** `docs/superpowers/handoffs/2026-10-26-ballista-impact-bolt-core-to-view-handoff.md`.

**Update:** This plan's execution record and the design only where actual implementation clarifies an approved decision. Do not silently revise semantics to match code.

**Interfaces:** Handoff contains the actual `ImpactBoltTraitStatusSnapshot.DistanceCells`, `OwnedCharacterStatsSnapshot.ImpactBoltDescription`, trait key, and existing attack/movement publication fields verified in Tasks 2–4. No View implementation is included.

- [x] Run the complete `ProjectL.Tests.Core` and `ProjectL.Tests.CoreIntegration` assemblies separately using the verification template. Capture fresh XML/logs/counts/exit status and reconcile against the pre-change baseline. Required new/changed cases must pass; disclose remaining unrelated baseline failures without broadening test scope.
- [x] Inspect `git diff --check`, touched paths, `.meta` pairing, pure-data Unity dependencies, and initial asset values. Confirm no View/Framework tests, View code/assets, retired assets, unrecorded other-unit changes, or unrelated docs were edited. The documented Phase B Hammer-reference and Precision Shot recipe corrections are the explicit exceptions to the original two-asset map. Apply the implementation-boundary review above; provide one short edit/understanding/verification comparison for balance tuning and one for trait-rule changes. Classify newly touched shared code as required registration, required shared behavior, or an unresolved coupling issue. Do not call a shorter file list proof of locality.
- [x] Write the handoff: initial settings versus configurable settings; exact outward-center diagram; configured distance available for both combat and roster cards; no piercing record; where the existing Ballista presentation throws; arcing trajectory/playback and impact release work owned by View; explicit immunity retained. For the new base profile, the result/preview `PrimaryCell` identifies the intended impact; existing `Pull.FinalAnchor` remains the delivery override for any supported Pull combination. Empty delivery after occlusion causes no displacement. Include the real result example and verification evidence from this implementation.
- [x] Identify excluded Framework tests with stale Ballista-piercing expectations (`DataBaseAssetTests`, `TargetingAngleAssetTests`) and presentation tests for owner follow-up, explicitly marking them unchanged/unexecuted. Record View/manual acceptance as pending. Do not send the document externally.
- [x] Update the execution record below and report Phase C completion limited to Core, InGame settings, the two allowed test assemblies, and the prepared handoff. Do not claim visual or whole-project completion. Do not commit or push.

## Execution record

| Phase | Status | Evidence |
| --- | --- | --- |
| A | Complete; stopped for phase review | Core 3,599/3,599; CoreIntegration 105/105; focused 161/161; Heavy-dependent regression 33/33; independent review clean |
| B | Complete; stopped for phase review | Focused 30/30; Core 3,599/3,599; CoreIntegration 135/135; independent review clean |
| C | Complete within approved scope | Fresh Core 3,599/3,599 and CoreIntegration 135/135, exit 0; local View handoff prepared; excluded suites unchanged/unexecuted |

Plan self-review (2026-10-26 locality pass): confirmed requirements map to Tasks 1–5; alternate SO behavior and all five review-focus failures have explicit tests; default and configured values use one authoring source; Task 2/3 snapshot and distance signatures match; View remains a handoff. Source inspection corrected the non-None zero-percent SO test and the battle-source-only roster proof, and fixed the feature-rule/executor ownership boundary. Structural benefit is an implementation expectation, not a measured result. No runtime verification is implied by this review.

### Phase A execution — 2026-10-26

**Scope completed:** Tasks 1–2 only. Production Ballista SO assets, InGame authoring, owned-character descriptions, View code/assets, and Framework/View tests are unchanged. No commit or push. Phase B has not started.

**Implemented contracts and ownership:**

- `TraitKey.ImpactBolt = 210` was unused and is now registered; retired key 120 remains untouched. `ImpactBoltTraitSpec`, `ImpactBoltTraitDefinition`, and `ImpactBoltTraitStatusSnapshot` expose immutable positive `DistanceCells`; snapshot equality includes distance. Runtime/effect state carries identity only.
- Existing registry factories, effect codec/provider, and immutable command capture now handle the trait. Distance comes from the installed definition. Tests round-trip distances 1 and 2 through these paths; this is Core configuration evidence, not proof of SO propagation (Phase B).
- `ImpactBoltTraitRule.TryResolveStep` owns living hostile field-unit eligibility, center-footprint exclusion, and direction through the existing `HostilityPolicy` / `ForcedDisplacementGeometry` contracts.
- The actual path is `CombatCommandEffects.Capture` → registered `ImpactBoltEffectRule.EvaluateKnockback` → `EffectKnockbackContribution(ImpactCenter, DistanceCells)` → `CombatEffectEngine` / `EffectKnockbackEvaluation` → `AttackKnockbackCalculator` → `AttackForcedMovement.ExecuteUnit` → existing `ResolveArrivals`.
- The shared internal contribution gains `Mode`, defaulting to `AttackerOrigin` for existing callers. Unknown modes and mixed origins are rejected. Impact candidates are distinct, entity-ID-ordered delivered hits; ledger-only victims and structures are excluded. Damage/reflection/death, terrain, action settlement, hit-health capture, and deferred final-object ordering remain owned by the existing executor.
- Profiles require Single for Impact Bolt and reject ordinary Knockback/PiercingFalloff combinations. Alternate valid shapes, angles, splash patterns/percentages, ranges, and positive distances remain accepted.
- `KnockbackImmunityPolicy` retains the Heavy rule behind `HeavyCategoryImmunityEnabled = false`; its existing two-argument entry remains intact. A three-argument overload verifies restoration without mutable global state. Explicit forced-movement immunity still wins.

**Verification:** All runs used Unity 6000.0.73f1 with approved AppData access and an explicit allowed assembly filter. XML/log/exit evidence is under `.superpowers/ballista-impact-bolt/`.

| Run | Result | Evidence basename |
| --- | --- | --- |
| Baseline Core | 3,534/3,534; exit 0 | `baseline-core` |
| Baseline CoreIntegration | 105/105; exit 0 | `baseline-integration` |
| Heavy policy RED | One expected policy failure | `heavy-red` |
| Heavy paths + registration RED | Seven expected failures | `heavy-path-and-impact-registration-red` |
| Heavy focused GREEN | 33/33; exit 0 | `heavy-green` |
| Impact behavior / mode RED | Wrong-origin, center, and profile failures observed; fixture defects corrected separately | `impact-behavior-red`, `impact-contract-red` |
| Planned focused fixtures | 161/161; exit 0 | `phase-a-focused` |
| Extra Heavy consumer regression | 33/33; exit 0 | `heavy-dependent-regression` |
| Final full Core | 3,599/3,599; exit 0 | `phase-a-core-gate` |
| Final existing CoreIntegration regression | 105/105; exit 0 | `phase-a-integration-gate` |

The first full-gate launch stopped on a test-only enum typo (`UnitRole.Sub`, corrected to `Regular`); no test success was claimed for it. The next full run exposed three stale Heavy-immunity expectations, addressed below. The final full Core count includes 65 additional cases relative to baseline, with zero failures.

Behavior coverage includes all eight directions at distances 1/2 and two shooter positions; simultaneous full-ring movement and ID order; 100 center / 70 surrounding damage; empty center; zero-percent synthetic Core splash; complete held defense; normal defense; lethal targets; Heavy/explicit immunity; board/occupancy/diagonal limits; lobbed and blocked delivery; allies/structures; undelivered and duplicate hits; thorn/mine timing; terminal stopping; reflected attacker death; repeated preview purity; trait identity on another unit; invalid binding/configuration; center-containing footprint and off-field targets; ordinary obstacle knockback; Precision Shot with 70/40/0-percent splash.

**Additional decisions and actual dependency findings:**

1. The public wall-blocked command already rejects with `BlockedByWall`. Preserve that contract; separately test an internal empty delivery without a center as a normal no-movement result, and delivered hits without a center as an inconsistency. No occlusion semantics changed.
2. Full Core regression found two `BossRoarTests` and one `EnhancementChainExplosionTests` expectation that depended on automatic Heavy immunity. These two additional Core test files were updated with both explicitly immune and non-immune cases. They are necessary shared-policy regression, not a new feature or production-code expansion. Non-immune Heavy now moves under roar and can trigger the existing post-displacement chain explosion.
3. The current feature checkout was retained with the user's pre-existing untracked spec/plan preserved. No source conflicts or unrelated source changes were observed. The user authorized default/recommended decisions without further questions during implementation.

**Boundary review — actual result:** Registration/factory/codec/provider/capture changes are required assembly. Contribution-mode validation and mode dispatch are required shared behavior. Target/direction rules remain in the feature rule; occupancy, terrain, damage, event publication, and state ownership are not duplicated. No runtime unit-key or method-key branch, DB read, Buffer write, distance fallback, or fake piercing payload was introduced.

| Change scenario | Before → after edit / understanding / verification scope |
| --- | --- |
| Tune Impact Bolt distance | Previously no impact-distance contract → one positive trait configuration passed through immutable definitions/status. Core distance tests exercise 1/2; authoring-only tuning and SO-to-runtime proof remain Phase B. No executor or registry balance literal is needed. |
| Change eligible targets or outward direction | Existing executor used attacker origin for ordinary knockback → Impact Bolt rule owns its eligibility/direction while the coordinator consumes the result. Understand shared facts, hostility, geometry, and contribution mode; ordinary knockback/terrain regressions remain necessary because execution is shared. |
| Disable Heavy category immunity | Previously all attack paths used Heavy-or-explicit immunity → one named disabled flag preserves the Heavy branch and independent explicit immunity. Understand the shared policy; ordinary knockback, roar, and chain-explosion consumers all require regression. |

A fresh independent Phase A review found no Critical/Important/Minor issues. Nine new C# files have paired unique `.meta` GUIDs; new Core files have no UnityEngine dependency. `git diff --check` passed. Review exclusions are the planned Phase B authoring/service-publication work and Phase C handoff/View/manual acceptance; they are not completion claims.
### Phase B execution — 2026-10-26

**Scope completed:** Tasks 3–4. Phase C handoff/closure has not started. No commit, push, View implementation, Framework/View test execution, or manual acceptance.

**Implemented configuration and display contract:**

- Serializable `ImpactBoltTraitAuthoring.distanceCells` defaults to 1 and exposes a minimum of 1. `ToData()` creates a separate immutable `ImpactBoltTraitData`; validation rejects 0/negative distance. `TraitSpecAdapter` forwards the exact value to `ImpactBoltTraitSpec`.
- `OwnedCharacterStatsSnapshot.ImpactBoltDescription` is an optional appended constructor parameter/property whose presence must match `TraitKey.ImpactBolt`. `OwnedCharacterStatsCalculator` derives it from the already resolved per-owned-instance definition, including enhancements. There is no second editable distance source.
- Actual propagation is SO `GetData()` → immutable Data → existing unit/trait/targeting adapters → production `DatabaseBattleInitializationSource` → Core service preparation/query/preview/command → combat status and result publication. Owned description uses the same DB values through the existing roster service before battle entry.
- `CoreIntegrationTestData.SetUnit` and `SetTargetingMethod` validate immutable inputs; all Get/TryGet/GetAll methods share their corresponding collections and preserve missing-key behavior and method 1 defaults. Configuration occurs before runtime construction.

**Initial production assets:**

| Asset | Actual change |
| --- | --- |
| `Unit_Ballista.asset` | PiercingFalloff replaced by ImpactBolt distance 1; wall delivery becomes existing IgnoreWalls value 2. Key 15, Cannon, HP 15, defense 5, power 13, priority 30, range 4, footprint, behavior, rewards, and FromImpact propagation remain unchanged. |
| `TargetingMethod_BallistaAngle.asset` | Method key 7 retains its GUID; shape/choice/angle/splash/percentage are now 5/1/0/3/70. |
| `Unit_DynamiteHammer.asset` | Required dependency correction: reference moved from method 7 to existing method 9 with identical old EightDirectionLine/Single/Front/None/0 semantics. All other Hammer fields and Knockback distance 1 remain unchanged. |
| `Enhancement_PrecisionShot.asset` | Required dependency correction: remove obsolete PiercingFalloff replacement and its managed-reference entry; retain added PrecisionShot and key 8, level 1, cost 700, weight 1, target, and GUID. Description now states the remaining first-recipient defense bypass. |

The first two assets were planned; the other two are recorded scope adjustments under the user's authorization to use the recommended solution without asking. Method 7 was not actually Ballista-only, and the real enhancement recipe would reject a missing base PiercingFalloff in `OwnedUnitSpecResolver`. Preserving those consumers required input changes; generic targeting and enhancement implementations were not changed. No other targeting asset was edited and no new targeting asset was created. Some legacy assets (Hammer and Unit_MainCharacter) are not present in the active Unit table; preservation tests read those existing assets directly without changing registration.

**Behavior and publication evidence:**

- Production-source tests feed the exact `GetData()` outputs of temporary Unit/Targeting SO clones into the fixture DB. The actor identity is adapted to the existing Cowboy fixture; power 100 and high-health targets isolate damage percentages. Tests do not manually reconstruct the cloned trait/method or patch installed gameplay facts.
- Distance 2 + Cross 40%: actor `(0,1)`, selected center `(3,5)`, enemy `(2,5)` moves to `(0,5)` and health 1,000 → 960. The center headquarters takes 100 damage and remains in place. Distance 1 ends at `(1,5)`; 70% ends at health 930. Battle status and owned description expose the same authored distance.
- Authoring tests also prove range 4/6, RadialSquare/CardinalLine, Unrestricted/Front, and None/0 changes through actual range membership, preview admission, committed state, and action cost. Non-None/0 remains rejected at the existing DB boundary. An already produced Data value and installed runtime retain old distance/percentage after editing the clone; a newly constructed runtime receives the edit.
- Real Precision Shot recipe tests verify plain/enhanced owned descriptions and combat profiles retain Impact Bolt at distances 1/2, add PrecisionShot, and contain no PiercingFalloff. A separate per-instance replacement fixture proves owned descriptions can differ (1 versus 2) without mutating the shared basis.
- Synthetic publication fixtures deliberately use the existing Rider actor (power 5, attack cost 3) and three high-health enemies around `(3,3)`. They prove distance 1/2 publication: west `(2,3)` → `(1,3)`/`(0,3)`, east `(4,3)` → `(5,3)`/`(6,3)`, center unchanged. Ordinary target health is 95 at center and 97 around it; Heavy follows the existing damage matchup and moves unless explicitly immune.
- Three real service previews preserve gameplay and command/result sequencing. A submitted command applies cost, damage, and movement once; repeated processing is a no-op. Moving a victim after calculation rejects the stale result with all stores unchanged; restoring the expected fact permits one application, and reapplication is idempotent. Only these deliberately stale-publication tests alter a live fact through the established BattleWriteSink test seam.
- All result/preview piercing payloads remain null. Existing `CombatCharacterEvent` movement records publish each step's expected/final placement, with no center movement event; no new animation event or fake piercing record was introduced.

**Existing preview boundary:** Public `AttackPreview.TargetOccupiedCells` is original occupancy and the contract has no final displacement anchors. The internal preview-transition overload is available to Core tests, not CoreIntegration. Phase A proves internal preview displacement agreement. Phase B proves public service-preview facts/purity, then compares public `Calculate` transitions and existing movement events to real service publication. No reflection bypass, new public payload, or friend-assembly expansion was added merely for tests.

**Additional test-fixture decision:** The production fixture's default headquarters health 20 would die to the controlled 100-damage center hit, ending the battle before the adjacent enemy can be displaced. `CoreIntegrationTestData` therefore accepts optional `headquartersHealth = 20`; these cases pass 1,000 before initialization. The default and all previous fixtures remain unchanged. This is immutable input setup, not a balance store or live-state override.

**Verification:** Unity 6000.0.73f1, approved AppData access, one Editor process at a time, explicit allowed assembly filters. Evidence remains in `.superpowers/ballista-impact-bolt/`.

| Run | Result | Evidence basename |
| --- | --- | --- |
| Initial authoring/defaults RED | 6 expected failures | `phase-b-authoring-red2` |
| Configuration and dependency RED | New authored paths exercised; old Ballista/recipe defaults and fixture expectation defects identified | `phase-b-configuration-red2` |
| Focused authoring + publication | 30/30; exit 0 (22 authoring, 8 publication) | `phase-b-focused2` |
| Full CoreIntegration | 135/135; exit 0, versus Phase A baseline 105 | `phase-b-integration-gate` |
| Full Core | 3,599/3,599; exit 0, unchanged count from Phase A | `phase-b-core-gate` |

Test-only import mistakes and an attempted use of Core-only internal preview APIs caused earlier compilation failures; none were counted as behavioral RED or passing evidence. Fixture assumptions were corrected at their source: the authored Cannon attack spends 5 AP, while the synthetic Rider publication fixture spends 3; legacy asset existence is not active-catalog membership.

**Boundary and review result:** Five new Phase B C# files have unique paired `.meta` files; the pure Data type has no UnityEngine dependency. Authoring/adapter/owned-snapshot additions are required configuration mapping; service assembly, state ownership, Buffer transactions, generic enhancement resolution, and forced movement remain unchanged. Balance edits now touch the SO values and their validity rules; target/direction implementation still lives in the Phase A trait rule. Configuration-to-execution tests and shared publication regression are necessary validation, rather than evidence of unrelated implementation coupling. Independent Phase B review found no Critical/Important/Minor issues; `git diff --check` passed.

**Remaining:** Current Ballista presentation still routes to piercing playback and requires a non-null `PiercingShot` (`CombatAttackPlayback.FirePiercingShot` / Ballista animation path). Production Ballista is not presentation-compatible until the View owner performs the arcing-shot/impact transition and trait visual work. Phase C will prepare the explicit handoff and document stale excluded Framework asset/presentation expectations. View/manual acceptance remains pending; no whole-project completion is claimed.
### Phase C execution — 2026-10-26

**Completed scope:** Task 5 verification and the local [Core-to-View handoff](../handoffs/2026-10-26-ballista-impact-bolt-core-to-view-handoff.md). No runtime/test/asset implementation was added in Phase C. No commit, push, or external delivery.

| Fresh run | Selected assembly confirmed in XML | Result / process exit | XML start time (UTC) |
| --- | --- | --- | --- |
| `phase-c-core-gate` | `ProjectL.Tests.Core.dll` only | 3,599/3,599, 0 failed; exit 0 | 2026-10-26 05:45:42Z |
| `phase-c-integration-gate` | `ProjectL.Tests.CoreIntegration.dll` only | 135/135, 0 failed; exit 0 | 2026-10-26 05:47:14Z |

Fresh `.xml`, `.log`, and `.exit` evidence is under `.superpowers/ballista-impact-bolt/`. Compared with the pre-change baselines (Core 3,534; CoreIntegration 105), there are 65 additional Core cases and 30 additional integration cases, with no remaining failure in either allowed assembly. Counts agree with the Phase B gate. Unity 6000.0.73f1 used approved AppData access, one process at a time, with explicit assembly selection.

**Static and ownership review:** `git diff --check` passed. All 14 new C# files across A/B have `.meta` pairs and unique GUID declarations in Assets; existing asset metadata/GUIDs are unchanged. New Core/pure Data files do not import UnityEngine. Touched paths match the recorded scope. View code/assets, Framework tests, FieldCamera, Architecture, retired assets, and unrelated documents remain unchanged. Other-unit preservation is stated precisely: the previously recorded Hammer reference correction is present, with identical effective targeting; no unrecorded other-unit edit was found. The Precision Shot recipe correction is also retained and verified.

**Handoff contents verified against source:** positive configured distance in combat and owned descriptions; eight outward grid directions and center-footprint exclusion; actual delivered-hit membership; zero-damage displacement; unchanged damage/arrival ordering; `PiercingShot == null`; existing per-step `CombatCharacterEvent` placement facts; `PrimaryCell` for the base profile and existing `Pull.FinalAnchor` delivery override; normal empty delivery; explicit immunity despite Heavy category disable. The examples distinguish exact SO outputs through the production source from deliberately synthetic publication profiles.

The handoff identifies the current `CombatAttackPlayback.TryBeginBallista` / `FirePiercingShot` exceptions, the existing shared delivery-cell resolver and arcing reuse candidates, and owner responsibilities for trajectory, impact release, weapon identity, trait art, and cancellation/lifecycle. It does not claim an enum-only conversion is complete. Public preview's original-occupancy contract and lack of final movement anchors are explicit.

**Required locality comparison:**

- Balance tuning previously depended on the old piercing profile and attacker-origin-only movement; the implemented Impact Bolt configuration now flows from existing SO inputs through immutable definitions to combat/owned reads. An ordinary balance edit needs the authored validity and adapter contract, not movement/damage internals. Configuration-to-query/preview/commit regression remains necessary to prove propagation.
- Target/direction changes now belong to `ImpactBoltTraitRule` with hostility/geometry contracts; the executor consumes the contribution mode and rule result. Ordinary Knockback/Pull/terrain regression remains necessary because shared execution applies the path. Required assembly mappings and required shared-mode/Heavy behavior are distinguished from feature rules; no unresolved implementation coupling was found by the prior independent A/B reviews or this source check.

**Excluded owner follow-up:** `DataBaseAssetTests` and `TargetingAngleAssetTests` retain source-observed stale Ballista/Precision Shot expectations. `CombatBallistaAssetTests`, `CombatBallistaAttackTests`, `CombatBallistaLifecycleTests`, and existing shot-path/Mortar tests are listed for their owners to adapt or retain appropriately. They were not edited, migrated, or executed, and no fresh failure count is claimed for them.

**Remaining acceptance:** View trait visuals, production arcing trajectory/playback and impact timing, Framework/View acceptance, and manual/PlayMode verification. Core/InGame and allowed-assembly completion does not imply production Ballista is playable through the current piercing View path. The handoff is prepared locally; it has not been sent externally.
Final Phase C handoff review: an independent read-only reviewer checked actual public contracts, delivery-cell interpretation, movement-event timing, current presentation exceptions, fixture examples, excluded test expectations, and scope statements. No Critical/Important/Minor findings. Plan/spec status and Task 5 checkboxes are complete. All remaining acceptance is explicitly external View/manual work.
