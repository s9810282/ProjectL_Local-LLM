# Configurable Category Trait Progression Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. Execute only the Phase and owner scope explicitly authorized by the user; stop after that Phase. The user is the Core programmer. External View/Flow handoff items are not Core implementation tasks. The user handles all commits and pushes.

**Goal:** Add configurable level-up trait choices alongside existing Main/Sub stat level-ups, deliver category-based Sub progression, support Main through the same contracts with content still undecided, and remove Sub traits from enhancement-shop purchases.

**Architecture:** InGame SO tables provide immutable category steps and shared trait definitions. One pure eligibility function serves validation and runtime; Character Buffer owns selections, offers, and receipts. The existing Core service derives progress including result-acknowledgment waits, while View/Flow owners implement presentation and sequencing against the public handoff contracts.

**Tech Stack:** Unity 6000.0.73f1, C#, existing Framework/Systems/Core assemblies, ScriptableObjects, uGUI, NUnit EditMode tests. No new package dependency.

**Spec:** [Configurable Category Trait Progression](../specs/2026-10-27-category-trait-progression-design.md).

**Status:** Phases A–D completed on 2026-10-28. A–C implementation/reviews and D handoff contract review are complete. The user authorized D and E; E inspection found no current external growth consumer/integration evidence and is `pending external integration`, not accepted end-to-end. Handoff is prepared, not externally delivered/accepted. Phase F remains outside the current scope. The approved CombatAttackProfile inputs and accepted pre-existing failures remain as recorded below. Operational activation remains pending.

## Global Constraints

- Acquisition presents three category candidates and the player chooses one.
- Enhancement presents at most three eligible owned traits; when fewer exist, it presents all of them.
- Configuration uses an ordered list of steps, each with an unlock level and an Acquire/Enhance kind. Counts derive from the list.
- The enhancement shop will sell Main-character growth only. Sub-unit traits cannot be purchased.
- Main is eligible for level-up trait selection. Use its existing `CategoryKey.Main` configuration and the same growth contracts as Sub; do not retain a role-based nonempty-growth prohibition. Production Main candidates/counts/unlock levels are undecided; keep its authoring explicitly empty until approved.
- Trait choices supplement existing level-ups. Preserve Sub `UnitLevelGrowthRules` and category attack/max-health increments, and Main `MainGrowthRules`/persistent stat grants. Empty/disabled trait configuration, an unconsumed result, or a rejected trait request must not suppress or repeat XP/stat progression.
- Existing unit-native traits remain authored on the unit. Enhance does not increase total trait count.
- Each owned trait can be enhanced once; repeated tiers are not part of this plan.
- SO definitions are immutable during a runtime. Activate the new ownership model on a fresh Run; no live legacy-Run conversion.
- Implementation documents are English. Code/XML comments are Korean. Preserve existing misspelled identifiers, namespaces unless explicitly changed, GUIDs when moving files, and protected legacy assets.
- No commits/pushes by the agent. No implementation, package installation, or Unity execution while merely preparing this plan.
- Core implementation covers pure rules/contracts, InGame SO/query code, Core services, Buffer transactions, and their tests/registration. UI/presentation/input presentation wiring, prefabs/scenes, and LifeCycle Flow/Job wiring are external View/Flow work. Their requirements remain in the handoff; do not edit those files under a Core Phase authorization.
- Review and stop after each Phase. Report tests actually run, outstanding content, and whether the Phase is suitable for production play.

## Review Focus

1. A later enhancement step becomes impossible after an earlier legal selection: reject that configuration before play, not after consuming a choice. Covered by T1.
2. A valid candidate key that was not in the frozen offer is submitted: reject without changing the record, active offer, money, or stage state. Covered by T4/T5.
3. A higher-level Sub is acquired outside victory settlement: resolve reached steps at a safe boundary and prevent entry bypass. Covered by T5/T7.
4. Teardown/rebind or duplicate input occurs while a result awaits acknowledgment: Core must expose `AwaitingAcknowledgement`, retain the receipt, and block another offer/entry; external View must consume the receipt and restore input correctly. Core coverage T4/T5; handoff and joint acceptance T6/T7.
5. Old enhancement-node references or purchase commands survive migration: block Sub purchases at Core/transaction boundaries and prevent releasing reachable obsolete nodes. Covered by T3/T8.

---

## 1. Execution boundaries and evidence

The current checkout is `feature/Cheon/Core/NewCombatSytem`. Recheck branch, working tree, concurrent edits, and applicable AGENTS files at execution; do not assume today's clean runtime-code state persists. The preceding design document is already an untracked user-task artifact and must be preserved.

Planning inspection found only `ProjectL.Tests.Core` and `ProjectL.Tests.CoreIntegration` as relevant active EditMode test assemblies. Old documentation references to `ProjectL.Tests.Framework`, `CombatFlowTests`, and `DataBaseAssetTests` do not establish that those files currently exist. Add the new integration/asset/Flow coverage below to CoreIntegration, which references Unity and Systems; keep pure tests in Core.

The spec's proposed policies are carried forward explicitly: uniform sampling, mandatory pending choices before another node, native-trait preservation, once-only enhancement, and frozen offers. Any requested change to these policies updates the design before implementation.

Ownership correction: A–C implement Core; D delivers the Core-to-View handoff. E records the external View/Flow delivery and joint acceptance, not UI code work for the Core author. F separates Core/content validation from external scene/manual acceptance. `Core complete`, `handoff prepared`, `external integration accepted`, and `production activated` are separate statuses. The first two may be complete while the latter two remain pending.

### Scope that remains separate

- Actual new trait effects, category balance values, and production trait names/pools must be supplied through the trait-content work. Test fixtures do not become production content.
- Main trait content and its unlock schedule are pending. Common-contract support and fixture tests are in scope; inventing Main traits or copying Sub pools into Main is not. Empty Main content does not block otherwise approved Sub activation.
- Main-shop products, effect amounts, costs, healing rules, and purchase limits require their own design/plan. Do not reinterpret existing Sub enhancement recipes as Main products.
- Production activation in T8 depends on these content decisions and enhancement-node availability. Earlier Phases can be reviewed and tested with fixtures without claiming the feature is playable in production.

## 2. File map and interfaces

All paths in this plan are repository-relative. Expand these exact prefixes:

| Prefix | Path |
|---|---|
| `C/` | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/` |
| `S/` | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/` |
| `B/` | `Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/` |
| `D/` | `Assets/ProjectL/Script/Systems/DataBaseSystem/` |
| `F/` | `Assets/ProjectL/Script/Systems/Factory/` |
| `V/` | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/` |
| `L/` | `Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/` |
| `U/` | `Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/UISystem/` |
| `K/` | `Assets/ProjectL/Script/Systems/ServiceKeys/` |
| `T/` | `Assets/Tests/EditMode/` |

New `.cs` and folder assets require `.meta` files. Do not create one class/interface per trivial operation. The following files group contracts that change together; use existing namespaces/folder conventions.

| Owner | Create | Modify / responsibility |
|---|---|---|
| Definition contracts | `C/Contracts/Enhancements/TraitGrowthStepSpec.cs`, `CategoryTraitProgressionSpec.cs`, `TraitGrowthDefinition.cs`, `TraitGrowthCatalog.cs` | Immutable authoring output, with existing `TraitGrowthKind`/`TraitKey`. No Unity types. |
| Rules | `C/Application/Roster/TraitGrowthCatalogValidator.cs`, `TraitGrowthOfferSampler.cs` | `TraitGrowthRules.cs`: add shared `EligibleTraits` in T1; replace fixed-slot admission/composition callers during T4 without copying eligibility. |
| DB | `D/Data/InGame/Table/TraitGrowth/{CategoryTraitProgressionDataTableSO,TraitGrowthDefinitionDataTableSO}.cs`; `.../Item/{CategoryTraitProgressionData,CategoryTraitProgressionDataSO,TraitGrowthDefinitionData,TraitGrowthDefinitionDataSO}.cs`; `D/Queries/InGameDataBase/InGameTable/{CategoryTraitProgressionTable,TraitGrowthDefinitionTable}.cs`; `S/CharacterSystem/TraitGrowthDefinitionAdapter.cs` | `F/DBFactory/Group/InGameDataBaseFactory.cs`, `D/Queries/InGameDataBase/InGameDataBase.cs`, `K/DBFactory/Group/InGameTableKey.cs`: table creation, group exposure, cross-reference validation. |
| Selection contracts | `C/Contracts/Enhancements/TraitGrowthSelection.cs`; `C/Contracts/Roster/{TraitGrowthOffer,TraitGrowthChoiceRequest,TraitGrowthProcessedResult,TraitGrowthProgressSnapshot}.cs` | Existing `OwnedTraitGrowth.cs`, `TraitGrowthChange.cs`, `TraitGrowthFailureReason.cs`, `Contracts/Roster/TraitGrowthChoices.cs`, `TraitGrowthResult.cs`, and owned-record contracts. The snapshot file also defines `TraitGrowthProgressStatus`. |
| Authoritative Buffer | `B/Character/TraitGrowthOfferState.cs`, `B/Core/TraitGrowthTransactions.cs` | `B/Character/{CharacterBuffer,PreparedTraitGrowthChange}.cs`, `B/Core/{BufferSystemGroup,ProgressTransactions,BattleCompletionTransaction}.cs`: offer/receipt state, atomic publication, reset/end cleanup. |
| Growth input/service | `B/TraitGrowthView/TraitGrowthViewBuffer.cs` in T4; `S/CharacterSystem/{TraitGrowthCommandProcessingService,TraitGrowthOperationPreparation,ITraitGrowthAvailabilityRead}.cs` across T4/T5 | `CharacterRosterService.cs`, `InGameCoreSystemGroup.cs`, `F/Runtime/{Buffer/BufferSystemFactory,InGame/Core/InGameCoreSystemFactory}.cs`, owning service-key containers. Input/acknowledgment storage exists before T4's offer barrier; T5 adds the processor and entry integration. |
| Composition | No second composer | `C/Application/Enhancements/OwnedUnitSpecResolver.cs`, `C/Contracts/Combat/BattleInitialization.cs`, `C/Application/{Roster/OwnedCharacterStatsCalculator,Combat/BattlePreparationCalculator}.cs`, `S/CharacterSystem/CharacterStatsQueryService.cs`, `S/CombatSystem/{DatabaseBattleInitializationSource,CombatInputAssembler}.cs`. |
| Shop retirement | No replacement Main product in this plan | `S/ShopSystem/{ShopService,EnhancementShopDefinitionAdapter}.cs`, `C/Application/Shop/{EnhancementOfferPlanner,EnhancementPurchaseRules}.cs`, shop decision/failure contracts, `B/Core/ShopTransactions.cs`. |
| External View/UI, handoff only | Suggested locations: `V/TraitGrowth/`, `U/Screens/TraitGrowth/`, `F/Runtime/InGame/View/` | View owner chooses component decomposition and modifies `U/UIIDs.cs`, owning View Group/Factory, and popup input binding as needed. Core does not create these files in T6. |
| External Flow, handoff only | Suggested growth Jobs under `L/Combat/Jobs/` and `L/Stage/Jobs/` | Flow owner modifies Combat progress/Plan/context and Stage readiness/Plan as needed. Core's `S/ChapterProgressionCoordinator.cs` guard remains a T5 implementation item. |
| Split production assets | Core/content: trait tables/configuration assets under `Assets/ProjectL/DataBaseSO/InGameGroup/TraitGrowth/`. View: `Assets/ProjectL/Prefabs/UI/TraitGrowth/TraitGrowthUI.prefab` if selected by that owner. | Core/content validates InGame tables and approved map availability; external View owns `Assets/Scenes/{Combat,StageSelect}.unity`, prefab connections, and visual mappings. |

Where the map lists a family of existing contracts, each Task below names the affected API. Re-run caller searches before editing; a new caller is a migration dependency, not permission to broaden behavior.

## 3. Phases and stop points

| Phase | Tasks / owner | Reviewable outcome | Production status |
|---|---|---|---|
| A | T1–T2 / Core | Shared eligibility, definitions, validity rules, SO/query conversion | Existing runtime behavior retained; new tables not activated. |
| B | T3–T4 / Core | Sub sales retired; history/composition, frozen offers, receipts and acknowledgment barrier | Core fixtures work; old enhancement nodes unavailable, not a finished Main shop. |
| C | T5 / Core | Buffered processor, derived progress contract, authoritative entry guard | Core integration verified; production steps disabled until external delivery. |
| D | T6 / Core handoff | Consumer contracts, worked sequences, tests and owner acceptance checklist | Core handoff prepared; actual UI/Flow implementation remains external. |
| E | T7 / View/Flow implementation + joint review | External presentation/sequencing evidence checked against Core contract | Mark pending until actual external implementation and integration evidence exist. |
| F | T8 / Core-content and View-Flow evidence separately | Core assets/regressions plus external scene/manual acceptance | Release only after both evidence sets and content/node prerequisites are complete. |

Do not leave a Phase with compile errors, ignored regression tests, or a temporary API that bypasses offers. Within Phase B, T4 is a single contract cutover across its consumers; do not split its incompatible contract edits across review stops.

## 4. Verification commands

Use the user's approved AppData-access execution path for Unity. In Codex, an external Unity invocation uses the required escalated tool route; do not first attempt the restricted path and then delete caches. If an Editor owns the project, use its Test Runner instead of starting a second instance.

For a closed Editor, the following PowerShell function is a documentation recipe, not a file to install during planning. Resolve the project path again when executing in an isolated worktree. Place evidence in that checkout's `Logs/TraitGrowthPlan/<phase>/`.

```powershell
function Invoke-TraitGrowthTests([string]$Phase, [string]$Assemblies, [string]$Filter) {
    $growthProject = (Get-Location).Path
    $growthEvidence = Join-Path $growthProject "Logs/TraitGrowthPlan/$Phase"
    New-Item -ItemType Directory -Force -Path $growthEvidence | Out-Null
    if (Get-Process Unity -ErrorAction SilentlyContinue) { throw 'Use the running Editor Test Runner.' }
    $growthArgs = @('-batchmode', '-nographics', '-projectPath', ('"' + $growthProject + '"'),
        '-runTests', '-testPlatform', 'EditMode', '-assemblyNames', $Assemblies,
        '-testResults', ('"' + "$growthEvidence/results.xml" + '"'),
        '-logFile', ('"' + "$growthEvidence/editor.log" + '"'))
    if ($Filter) { $growthArgs += @('-testFilter', ('"' + $Filter + '"')) }
    $growthProcess = Start-Process 'D:/Program Files/Unity/Hub/Editor/6000.0.73f1/Editor/Unity.exe' `
        -WindowStyle Hidden -Wait -PassThru -ArgumentList $growthArgs
    if (!(Test-Path "$growthEvidence/results.xml")) { throw 'No test result XML; inspect editor.log.' }
    [xml]$growthResults = Get-Content "$growthEvidence/results.xml" -Raw
    $growthResults.'test-run' | Select-Object result,total,passed,failed,skipped
    if ($growthProcess.ExitCode -ne 0 -or $growthResults.'test-run'.result -ne 'Passed' -or
        [int]$growthResults.'test-run'.total -lt 1) { throw 'Unity verification failed.' }
}
```

At execution, use a fresh evidence directory for each red/green run, reject a zero-test result, and inspect unexpected skips. The expected red result is the new assertion/contract failure, not an unrelated compile/environment failure. Once contracts compile, verify behavior failures before completing implementation. Do not mark a missing Unity result as a pass.

`Core` below means `ProjectL.Tests.Core`; `Integration` means `ProjectL.Tests.CoreIntegration`. Task test-filter names are class-name fragments. Implementation Phases run focused tests and full Core/CoreIntegration suites after shared-contract changes. Document-only handoff work does not trigger unrelated test reruns. External View/Flow owners supply their automated/manual evidence in E/F; Core still runs consumer-contract and actual integration tests needed by shared state/order changes. Existing unrelated failures must be identified and reported, not suppressed.

## Phase A — Definitions and SO contracts

### T1: Define category steps and prove candidate sufficiency

**Files:** Create the definition-contract/validator files in §2 and `T/TraitGrowthCatalogTests.cs`; add the shared eligibility primitive to existing `C/Application/Roster/TraitGrowthRules.cs`. Do not replace runtime selection storage or legacy public behavior yet.

**Interfaces produced:**

```csharp
TraitGrowthStepSpec(int stepId, int unlockLevel, TraitGrowthKind kind)
CategoryTraitProgressionSpec(CategoryKey category, IReadOnlyList<TraitGrowthStepSpec> steps,
    IReadOnlyList<TraitKey> pool)
TraitGrowthDefinition(TraitSpec normal, TraitSpec enhanced, string name, string description,
    string enhancedName, string enhancedDescription)
TraitGrowthCatalog(IReadOnlyList<CategoryTraitProgressionSpec> categories,
    IReadOnlyList<TraitGrowthDefinition> definitions)
void TraitGrowthCatalogValidator.Validate(TraitGrowthCatalog catalog,
    IReadOnlyList<CombatAttackProfile> units)
IReadOnlyList<TraitKey> TraitGrowthRules.EligibleTraits(CombatAttackProfile basis,
    IReadOnlyCollection<TraitKey> ownedTraits, IReadOnlyCollection<TraitKey> enhancedTraits,
    TraitGrowthKind kind, CategoryTraitProgressionSpec progression, TraitGrowthCatalog catalog)
```

`TraitGrowthDefinition.Key` derives from `normal.TraitKey`; enhanced may be null, otherwise its TraitKey must match. Catalog exposes immutable `Categories`, `Definitions`, `GetCategory(CategoryKey)` and `GetTrait(TraitKey)`. `TraitGrowthCatalog.Disabled` contains Main/Heavy/Rider/Cannon with empty steps/pools. Main is supported, Boss/enemy-only progression is excluded. Use the role-neutral proposed type/table names in §2; these names do not rename any implemented source in this documentation task.

**Approved execution correction (2026-10-27):** The user approved passing existing `CombatAttackProfile` inputs to shared eligibility and catalog validation. `UnitSpec` alone lacks targeting-method facts. Reuse the existing attack-profile compatibility contract for hypothetical current traits plus each candidate; do not duplicate its targeting predicates. The user also approved proceeding with the nine recorded pre-existing baseline test failures, comparing the final failure set against `Logs/TraitGrowthPlan/A-baseline/results.xml`.

- [x] Write `StepOrder_RejectsDuplicateIdsDescendingLevelsAndLevelOne`: duplicate IDs, level 1, and descending levels throw; equal-level rows preserve input order.
- [x] Write `AcquireCount_ComesFromRowsAndIncludesNativeOverlap`: for two Acquire rows and no overlapping native trait, pool size 3 is rejected and size 4 is accepted; one overlapping native trait requires another candidate. No maximum-trait constant is consulted.
- [x] Write `Enhance_AllReachablePathsMustRemainSatisfiable`: `Acquire → Enhance` with three non-enhanceable acquisition options is rejected even if a fourth option can enhance; an eligible native trait can make that same sequence valid.
- [x] Write `MainConfiguration_EmptyAndConfiguredAreBothValid`: empty Main steps/pool pass without opportunities; a valid nonempty Main fixture passes the same eligibility/sufficiency checks. Its candidates remain separate from Sub categories. No production Main candidate/level is inferred from the fixture.
- [x] Run `Invoke-TraitGrowthTests 'A-T1-red' 'ProjectL.Tests.Core' 'TraitGrowthCatalogTests'`; observe the expected new failure.
- [x] Implement `EligibleTraits` first. It alone owns category membership, availability/capability, duplicate exclusion, enhanced-payload presence, and once-only enhancement. Inputs are immutable/transient facts; no stored simulation state or new general-purpose rule framework. The sampler and validator may not copy those predicates.
- [x] Implement the contracts and validator. Validate enabled-category player-eligible owned unit bases (Regular/Sub and MainCharacter/Main), preserving existing Main identity/category/ownership invariants. In validation only, traverse transient acquired/enhanced key sets through the ordered steps and call `EligibleTraits` at every node; memoize equivalent sets. At each Acquire node require at least three returned keys; at each Enhance node require at least one. Explore every possible choice, since any candidate can be sampled. Report category, unit, step, and failing selection path. Never silently truncate validation search or reject an arbitrary numeric acquisition limit.
- [x] Run the green filter and verify defensive copies, cross-category references, empty steps, many steps, and null/wrong-key enhancement payloads. Reachable combinations involving unimplemented effect capabilities are authoring errors; do not enable those effects as part of this Task.

### T2: Connect SO authoring to immutable query data

**Files:** Create DB and adapter files in §2, `T/CoreIntegration/TraitGrowthAuthoringTests.cs`; modify the DB factory/group/key files there. Do not edit the operational category movement/XP SO or production tables yet.

**Interfaces:** `ICategoryTraitProgressionTableQuery.Get(CategoryKey)` / `TryGet(CategoryKey, out CategoryTraitProgressionData)` / `GetAll()`; `ITraitGrowthDefinitionTableQuery.Get(TraitKey)` / `TryGet(TraitKey, out TraitGrowthDefinitionData)` / `GetAll()`. `TraitGrowthDefinitionAdapter.Build(categories, definitions)` returns the T1 catalog through the existing `TraitSpecAdapter` conversion. Add a narrow `IHasTraitGrowthTables` DB capability exposing these queries; do not force unrelated fake `IInGameQuery` implementations to invent content.

- [x] Write `SOConversion_CopiesStepsAndConvertsReferencesToKeys`: two `GetData()` calls return distinct immutable objects; Data contains no SO/Unity object references; step order and both effect forms survive conversion.
- [x] Write `Registration_RejectsOneMissingTableAndDuplicateCategory`: absent pair is the documented disabled feature; present pair must contain Main/Heavy/Rider/Cannon rows, even when empty; a partial/malformed pair throws before runtime creation. Empty Main steps are valid pending content, not a reason to disable other categories.
- [x] Run `Invoke-TraitGrowthTests 'A-T2-red' 'ProjectL.Tests.CoreIntegration' 'TraitGrowthAuthoringTests'`.
- [x] Implement normal DatabaseItem/DataTable/Query patterns and `IExportSystemServices` registration. DB factory builds both tables and calls T1's cross-table validator with unit specs through the existing adapter. Group exposes owning query references; no root re-export, no mutable runtime state in a table.
- [x] Run the green filter, including factory-to-group-to-consumer retrieval and real SO serialization round-trip. Verify unrelated roster initialization still works with both new tables absent.

**Phase A gate:** Full Core/CoreIntegration compile and regression, no operational asset activation, report validity constraints and stop.

### Phase A execution record — 2026-10-27 to 2026-10-28

Implementation, independent review and the review fix pass are complete. No Phase B work is included.

- Contracts: immutable ordered steps, category pools and normal/optional enhanced definitions. All four category rows are required in a registered pair; empty Main remains supported. Step IDs are positive and unique, unlock levels are at least two and nondecreasing, and equal-level rows retain input order. Definition/pool keys must be unique and every pool reference must resolve.
- Eligibility: shared `TraitGrowthRules.EligibleTraits` receives the approved `CombatAttackProfile` input, excludes owned/unavailable traits and already-enhanced keys, and reuses the existing attack-profile constructor for candidate compatibility. The validator traverses every reachable selection state, memoizing equivalent owned/enhanced sets. Each Acquire requires three candidates and each Enhance requires one; failures include category, unit, step and selection path. No acquisition limit is introduced.
- Authoring/registration: two SO tables convert references to keys and independent immutable data. The Factory builds and validates the pair before exporting queries into the InGame DB's owning builder. Consumers access `IHasTraitGrowthTables` through `InGameQuery`; no new root convenience registration is added. An absent pair is disabled; a partial or malformed pair throws before the DB is returned. Operational SO tables remain untouched.
- Focused evidence: `Logs/TraitGrowthPlan/A-T1-red-verified` (10 expected failures), `A-T1-green-final` (23/23), `A-T2-red` (9 expected failures), `A-T2-green` (19/19, including existing growth-service integration). The serialization test saves temporary SO assets, unloads/reloads them, validates both forms and step order, and deletes only its own temporary folder.
- Independent review found two P2 issues: missing distance-damage payload coverage and a mutable nested distance array in query data. Both were reproduced before fixing. Shared eligibility now checks normal/enhanced distance arrays against authored attack range plus existing splash geometry. The reused `DistanceDamageScaleTraitData` now exposes a copied read-only collection, preserving its public property type and validation behavior; a query-output mutation test covers both forms.
- Capability limitation: `DistanceDamageScale` combined with `PiercingFalloff`, `KillChainAttack`, or `ChainExplosion` is excluded from the new growth path because board-edge, map-wide, and post-displacement distances cannot be bounded by the supplied attack profile. Both selection orders are tested. Existing combat behavior remains unchanged. Supporting these combinations later requires explicit board/environment distance bounds; the cost of this conservative decision is excluding combinations that could work on a specifically bounded stage.
- Final full regression: `Logs/TraitGrowthPlan/A-final/results.xml`: Core 3812/3816, CoreIntegration 185/190, zero skips. All 27 new cases pass (17 Core and 10 CoreIntegration). The exact nine failing cases match the pre-implementation `A-baseline/results.xml`; there are no new failures. Review reproducers failed 8/8 in `A-review-red-bounds`, then all passed in `A-final`. The user explicitly authorized proceeding with the existing baseline. The compiler's existing CS8632 warning in `CombatCommandEffects.cs:102` is outside this change.
- Existing failures: `ExperienceAttributionPathTests.DeferredGroundAtPlayerEnd_DoesNotCreateKillsOrExperience`; `WeakGroundTurnTests.ControllerCompletion_DoesNotCollapseOrCountGround` (Player/0, Enemy/1, Enemy/8); `BossStagePreparationTests.ProductionBossStage_PreparesAndPlacesBoss`; `PullAndMarkPresentationTests.Pull_AttackMotionRunsOnlyForDirectHit` (false/true) and `Pull_PreviewAndAttackTargetUseArrival` (false/true). These tests remain active and unmodified.
- Change locality confirmed for this Phase: adding an existing supported trait or changing step counts now has authoring/contract validation owners, without modifying movement/XP SOs, shop internals, or View/Flow. Factory/Group/key edits are the required dependency registration. Runtime candidate admission/history still uses the old contracts until Phase B, so the complete runtime locality improvement remains an expectation for later phases. Validation retains the necessary attack-contract and DB integration dependencies.
- Production status: not activated. No production trait content, Main unlock schedule, Main-shop products, offer/receipt lifecycle, or View/Flow integration is supplied in Phase A. Existing runtime selection, shop, XP and stat progression remain on their previous paths. No commits or pushes.

## Phase B — Retire Sub purchases and cut over growth state

### T3: Close the old Sub enhancement-shop path

**Files:** Modify shop-retirement files in §2 plus `C/Contracts/Results/{ShopFailureReason,ChapterProgressionResult}.cs` and `C/Contracts/Shop/ShopPurchaseDecision.cs`; migrate `T/EnhancementShopRuleTests.cs`; create `T/CoreIntegration/SubTraitShopRetirementTests.cs`.

**Interfaces:** Add typed `EnhancementShopUnavailable` rejection to the appropriate existing stage-entry failure enum and `SubTraitPurchaseUnavailable` to `ShopFailureReason`. Keep existing serialized enum numbers/legacy asset deserialization stable. Ordinary unit/item `ShopService` APIs stay unchanged.

- [x] Write `LegacySubOffer_CannotSpendOrUsePurchase`: submit a captured legacy enhancement offer; assert the typed failure, identical currency, `HasPurchase == false`, and unchanged owned records/revision.
- [x] Write `EnhancementNode_DoesNotPublishAnEmptyShop`: attempting old enhancement-stage entry rejects before stage-selection/shop publication. Ordinary unit/item entry and purchase remain valid.
- [x] Run red filters `SubTraitShopRetirementTests|EnhancementShopRuleTests` in their owning assemblies.
- [x] Remove production Sub offer generation, recipe loading, purchase planning, and `TraitGrowthChange` installation from Shop. Remove dead planner/adapter code with its `.meta` only when no callers remain; preserve SO asset classes/keys required to deserialize legacy assets. Do not delete tests proving ordinary purchase atomicity. Migrate old successful-Sub-purchase tests to the explicit retirement contract.
- [x] Run green filters and ordinary `ShopOfferPlannerTests`, `ShopPurchaseRuleTests`, and `ShopProductContractTests`. Record all reachable old enhancement-node references for T8. This Task makes the old shop unavailable; it does not implement the Main shop or silently remove map nodes.

#### T3 execution record — 2026-10-28

- Removed the Sub recipe loading, offer planner, purchase planner, and Shop growth installation path. `ShopPurchaseDecision` no longer carries growth changes. Typed unavailable enum values are appended to preserve existing numeric values. Legacy SO classes, assets, snapshots and historical result contracts remain.
- Entry is blocked before selection through `ChapterProgressionCoordinator.PrepareStageEntry`, including `ConfirmStageEntry` and `ShopService.EnterShop`. The minimal additional caller migration changes internal `StageServiceManager.ValidateEntry` to return the root it already validated, avoiding a second external DB read. The Buffer entry transaction also rejects empty/nonempty enhancement visits. Captured legacy purchases are rejected independently by service, pure purchase rules and the Buffer purchase transaction before any currency, roster or purchase-marker mutation.
- Red evidence: `Logs/TraitGrowthPlan/B-T3-red-verified/results.xml` has five expected retirement failures and one passing ordinary purchase case. `B-T3-entry-red/results.xml` has two expected direct-entry failures. An earlier ordinary test fixture accidentally offered an already-owned unit and was corrected before the verified red run; an intermediate compile attempt restored the still-needed ordinary item adapter import.
- Final focused verification: `Logs/TraitGrowthPlan/B-T3-final/results.xml`, **48/48 passed, zero skips**: 2 retirement rule, 6 retirement integration, 17 ordinary offer-planner, 15 ordinary purchase-rule, and 8 product-contract cases. Ordinary item coverage retains frozen price, inventory delta/preservation, insufficient funds and overflow; ordinary unit integration covers entry and one successful purchase. Full Phase B regression follows T4. Independent T3 review found no actionable issue.
- Confirmed locality: Shop no longer reads growth eligibility, slots, or composition. Its ordinary economics and atomic purchase contract remain. Entry availability is checked at the existing entry owner; no new state store or general transaction layer is introduced.
- T8 release dependency: `StageMap_Chapter1.asset` still has enhancement weight 4 / minimum 1. `Stage_Enhancement.asset` (stage 1202) remains registered through `StageRootDataTable.asset` and `InGameGroup.asset`, and references `Enhancement_Default.asset` (DoubleShot, DoorBreaching, PoisonThorn). `EnhancementDataTable.asset` also retains ChainExplosion. `NodeVisual_Shop_Enhancement.asset` still maps stage 1202. No production C# hardcoded/pinned 1202 entry was found in the scoped source inventory; authored/random/pinned availability must still be validated in T8. These nodes are now unavailable at runtime, not removed or replaced. Main-shop delivery or approved node disabling remains required before release.

### T4: Replace fixed slots, compose selections, and publish frozen offers atomically

**Files:** Selection contracts including `C/Contracts/Roster/OwnedCharacterRecord.cs`, authoritative Buffer, `B/TraitGrowthView/TraitGrowthViewBuffer.cs`, their Buffer registration, composition files, and `S/CharacterSystem/{CharacterRosterService,TraitGrowthOperationPreparation,ITraitGrowthAvailabilityRead}.cs` from §2; create `T/{TraitGrowthOfferTests,TraitGrowthStatCoexistenceTests}.cs`, `T/CoreIntegration/{TraitGrowthTransactionTests,TraitGrowthProgressContractTests}.cs`; migrate `T/{OwnedTraitGrowthTests,TraitGrowthChangeTests,OwnedEnhancementStateTests,OwnedEnhancementResolutionTests,TraitGrowthRulesTests}.cs` and `T/CoreIntegration/TraitGrowthChoiceServiceTests.cs`. Retain `T/UnitLevelGrowthRulesTests.cs` and `UnitLevelGrowthCombatTests.cs` as required regression coverage. Update every live `Growth.Keys`, `Acquired`, `Enhanced`, `WithEnhancements`, and `BuildProfiles` caller in the same Task.

**Contracts and interfaces:**

```csharp
TraitGrowthSelection(int stepId, TraitKey traitKey)
OwnedTraitGrowth(IReadOnlyList<TraitGrowthSelection> selections)
TraitGrowthStepSpec TraitGrowthRules.NextStep(OwnedCharacterRecord owned,
    CategoryTraitProgressionSpec progression)
IReadOnlyList<TraitKey> TraitGrowthRules.Candidates(OwnedCharacterRecord owned,
    CombatAttackProfile basis, TraitGrowthCatalog catalog)
IReadOnlyList<TraitKey> TraitGrowthOfferSampler.Sample(
    IReadOnlyList<TraitKey> candidates, TraitGrowthKind kind, int seed)
UnitSpec OwnedUnitSpecResolver.Resolve(UnitSpec basis, OwnedCharacterRecord owned,
    TraitGrowthCatalog catalog)
```

`NextStep` returns null when complete or below the next row's level. Runtime `Candidates` obtains actual current/ enhanced keys via composition/history and delegates eligibility to T1's `EligibleTraits`; it adds only step/level admission. Its `CombatAttackProfile` input carries forward the user-approved Phase A attack-capability contract; the previous `UnitSpec`-only signature omitted the targeting facts already required by that shared function. The validator calls that same primitive with equivalent simulated facts. Catalog validation, not runtime filtering of future outcomes, guarantees future steps remain possible. `OwnedTraitGrowth.Selections` is a defensive copy; append order and unique step IDs are validated. No separate consumed-count or final-trait collection.

`TraitGrowthOffer` contains `RunId`, `long OfferId`, `OwnedId`, `StepId`, `Kind`, and immutable display-ready choices (`TraitKey`, form, name, description). Offer ID is monotonically allocated by its Buffer owner; overflow fails before publication. `TraitGrowthChoiceRequest` contains Run/offer/owned/step/key. Revise `TraitGrowthChoices` to the new trait identity, not `EnhancementKey`.

`CharacterRosterService` produces:

- `TraitGrowthProgressSnapshot ReadTraitGrowthProgress()` through `ITraitGrowthAvailabilityRead`: a side-effect-free fresh projection. `ICharacterRosterService` also exposes this read contract through its existing owning Core Group access; do not register a convenience root service. Core entry guards use the same method; View does not call Core directly.
- `TraitGrowthProgressSnapshot PrepareNextTraitGrowthOffer()` on the service contract: return the current progress unchanged if unavailable, awaiting acknowledgment, already holding an offer, or ready to continue. Only `ChoiceRequired` without an active offer may sample/publish; recheck the source and acknowledgment barrier immediately before publication.
- Internal `TraitGrowthOperationPreparation PrepareTraitGrowthChoice(TraitGrowthChoiceRequest request)`: immutable captured Run/record/revision/offer plus `TraitGrowthDecision`; performs no installation. T5's processor consumes this.
- Remove the old `ChooseTraitGrowth(ownedId, EnhancementKey)` public bypass. Migrate service tests to prepare + transaction publication now, and add input-path coverage in T5. Do not leave obsolete tests ignored.

Buffer surfaces: `ITraitGrowthStateReadSink.ActiveOffer`, `ITraitGrowthResultReadSink.HandledCommandNumber`/`LatestResult`, and an internal `ITraitGrowthTransaction` with:

```csharp
TraitGrowthOffer PublishOffer(RunId expectedRun, long expectedRevision,
    CharacterInstanceId ownedId, int stepId, TraitGrowthKind kind,
    IReadOnlyList<TraitGrowthChoice> choices)
PreparedTraitGrowthPublication PrepareChoicePublication(RunId expectedRun,
    long expectedRevision, TraitGrowthOffer expectedOffer, TraitGrowthChange change,
    TraitGrowthProcessedResult result)
void InstallChoicePublication(PreparedTraitGrowthPublication prepared)
void Clear()
```

`PublishOffer` allocates the offer ID and returns the published offer. `change` is null for a typed failure receipt. The prepared type is opaque outside Buffer. `TraitGrowthOperationPreparation` in the service holds these captured inputs plus its decision; Buffer does not depend on the service's internal preparation type. Character Buffer retains state ownership; the transaction composes existing roster revision checks with offer/receipt publication. Do not invent a generic transaction engine.

Create the minimal input/acknowledgment Buffer here, before the barrier is used: `ITraitGrowthViewWriteSink.Submit(TraitGrowthChoiceRequest request) : int` and `bool AcknowledgeResult(RunId runId, int number)`; read side `TryGetCommand(int number, out TraitGrowthViewCommand command)` and `AcknowledgedResultNumber`. Acknowledge only the current retained result's exact number/Run, atomically marking/removing that processed input; invalid/future/wrong-Run acknowledgment returns false without writes, and repeating the latest accepted same-context acknowledgment returns true without writes. Other already-obsolete numbers return false. No acknowledgment grants a trait or opens an offer. `ITraitGrowthViewBufferStore.Clear()` is internal and reserved for runtime reset; it is not exposed through the View write sink.

`TraitGrowthProgressSnapshot` has `RunId`, `Status`, and nullable `ActiveOffer`. `TraitGrowthProgressStatus` is derived in this priority order: `Unavailable` for ended Run/active battle; otherwise `AwaitingAcknowledgement` for handled > acknowledged; otherwise `ChoiceRequired` for an active offer or a reached unresolved step; otherwise `ReadyToContinue`. It is not an additional mutable state machine. Illegal counter relationships fail explicitly. ActiveOffer may coexist with a failure receipt; acknowledgment wait takes precedence. Both offer and choice publication enforce the barrier at their Buffer boundary; a caller cannot bypass it by omitting the Flow check.

- [x] Write `SelectionHistory_AllowsFourAcquisitionsAndTwoEnhancements`: use sufficiently large fixture pools and valid ordered steps; assert six selections, four added distinct traits, two replacements, and preserved ID/XP/level/native traits.
- [x] Write `ConfiguredMain_UsesTheCommonChoiceAndCompositionPath`: a Main fixture can acquire/enhance, retain history across XP growth, use the same frozen offer/receipt barrier, and resolve next-battle traits. Empty Main configuration yields no choice. Update existing Main-rejection tests to the new role policy without weakening ownership/identity invariants.
- [x] Write `StatLevelUps_ArePreservedWithAndWithoutTraitSteps` in `TraitGrowthStatCoexistenceTests`: for identical XP input, Sub level/XP and baseline attack/max health match the existing `base + (level - 1) * category growth` with empty/enabled/pending trait steps. Main's stat settlement is identical with empty/enabled/pending trait steps for the same seed and reached levels. Choose/retry/acknowledge does not grant XP/stats again or change Main persistent growth. Include a multi-level jump and enhanced traits; compare baseline growth separately from deliberate trait-effect modifiers.
- [x] Write `FrozenOffer_RejectsAValidKeyNotPresented`, `SecondChoice_ChangesNothing`, and `Reopen_DoesNotConsumeRandomness`: assert candidate membership, identical active offer on read/reopen, exactly one revision increment on success, and no growth mutation on failure.
- [x] Write `PrepareFailure_LeavesOfferRecordAndReceiptUntouched`: alter Run/record/revision/offer after preparation; installation rejects before any writes. For a normal typed failed request, only its failure receipt may advance; the offer and owned record remain unchanged.
- [x] Write `ValidatorAndRuntime_UseEquivalentEligibility`: for matching simulated/live owned and enhanced keys, shared-function results equal runtime `Candidates` before sampling. Include native overlap, unavailable effects, and repeated enhancement. Validation and runtime both retain their necessary consumer tests.
- [x] Write `LastSelection_WaitsForAcknowledgement`, `PrepareNextOffer_CannotCrossReceiptBarrier`, and `FailureReceipt_RetainsOfferForRebind`: assert the four documented statuses, no next-offer allocation/RNG use while awaiting acknowledgment, and correct same-offer retry or next-step readiness afterward. Acknowledge through the real T4 input Buffer in integration tests; do not mutate its counters directly.
- [x] Run red filters for new tests and the existing growth suites.
- [x] Implement step admission and generalized append-one `TraitGrowthChange`; reuse T1 eligibility rather than recreating it. Compose native traits followed by selections in step order, replacing payloads in place on Enhance. Remove the fixed `MaximumTraits` and single Acquire/Enhance slots. Use sorted returned TraitKeys and a seed derived deterministically from Run GUID bytes + owned ID + step ID; never `string.GetHashCode()` or shared combat RNG. The sampler only samples: Acquire count is exactly three, Enhance count is `min(3, eligibleCount)`.
- [x] Remove `OwnedCharacterRecord`'s current rejection of nonempty Main growth and Main-exclusion checks in growth service/rules. Keep valid ownership roles, Main definition policy, and all other record invariants. Main and Sub select configuration by their definition's category and share storage/composition/transactions; do not add a Main-only trait service or let shop eligibility decide level-up eligibility.
- [x] Implement service source capture/revalidation and Buffer prepare-before-install publication. Keep `InitializeRoster`/`AcquireSub` free of growth-table readiness requirements. Dispose/end/reset clears offers; `ProgressTransactions` and completion publication must retain atomicity. An ended Run returns no offer rather than forcing a discarded choice.
- [x] Replace `BattleInitialization.Enhancements`/`WithEnhancements` with `TraitGrowth`/`WithTraitGrowth(TraitGrowthCatalog)`; preserve catalog through all `With...` copies. Update resolver `BuildProfiles(baseProfiles, owned, catalog)` and every listed stats/battle consumer. Empty histories use native traits even with disabled tables. A nonempty history with missing catalog definitions is an error, not a silent native-only fallback.
- [x] Remove unused `SubTraitAcquireLevel`/`SubTraitEnhanceLevel` fields and constructor arguments from `C/Contracts/Commands/LevelProgressionSpec.cs`, migrating `T/ExperienceRulesTests.cs` and all callers; XP/stat-growth behavior stays unchanged. Update `CategoryConfigDataSO.ToData()` only if the constructor signature requires it; do not add duplicate step settings there.
- [x] Run green growth/transaction/stat-coexistence filters and full Core/CoreIntegration, including existing Sub level-growth and Main settlement tests. Prove resolved stats, deck publication, attack profiles, and next-battle initialization agree for two copies of the same Sub and for a configured Main fixture. Do not change XP settlement, `UnitLevelGrowthRules`, `MainGrowthRules`, or their numeric settings merely to support traits. Preserve experience-change immutability tests. Audit old shop/growth key references; none may remain as a level-up trait runtime path.

**Phase B gate:** All shared-contract consumers compile together, old Sub sales reject, fixture selection/composition and acknowledgment-aware progress work, and no existing production Run is converted implicitly. Report the enhancement-node release dependency and stop. No View/Flow files are changed.

### Phase B completion record — 2026-10-28

- T3 and T4 are complete and independently reviewed. No actionable runtime finding remained. T4 replaced fixed slots with immutable ordered selections, enabled Main through the common contract, removed public direct growth writes, and migrated stats, deck projections, per-instance attack profiles and battle initialization to the shared catalog/composer.
- Character Buffer owns the frozen offer and retained result. The input Buffer retains one pending request, allocates monotonic numbers, and accepts only the current result's exact Run/number acknowledgment. A result carries its current publication Run separately from the exact request Run, allowing stale-request failure receipts without allowing old-Run acknowledgments. The successful DTO requires both Run identities to match. Progress is derived in the specified priority order, and both offer and choice publication enforce the acknowledgment barrier.
- Publication captures and revalidates the Run, roster revision, offer, retained receipt, acknowledgment and actual request before installing any mutation. Run reset, both Run-end publication paths and shutdown clear context without reusing request/offer identifiers. The final-chapter cleanup test exercises the approved completion DTO at the Buffer transaction boundary; it is not a full final-chapter Flow/scene acceptance claim.
- T4 red evidence: `Logs/TraitGrowthPlan/B-T4-red-main` reproduced the old Main restriction; `B-T4-red-behavior` executed 94 cases with 32 intended missing-behavior failures after fixture/compiler corrections. `B-T4-green2` passed 94/94. Final expanded focused evidence is `B-T4-green-final3/results.xml`: **123/123 passed, zero skips**. It includes native/selected enhancement, six-step history, valid-but-unoffered rejection, source changes, receipt barriers, reset, shutdown, and exact/stale acknowledgment cases.
- Actual victory tests award 950 XP and compare disabled versus enabled growth configurations for Main/Sub. Levels, remaining XP and deterministic Main persistent grants agree; subsequent failure, retry, Acquire, Enhance and repeated acknowledgment preserve those values. Existing attack/AP/effect tests retain their assertions with step-based fixtures. The retired recipe adapter was removed only after finding no C# callers; legacy asset types remain.
- Same-definition instance isolation is verified through pure composition and a synthetic low-level roster fixture using a three-member deck (Main plus two distinct owned IDs of the same Sub). Actual Core duplicate acquisition still rejects under the unchanged one-per-definition rule. An initial fixture incorrectly installed all six owned records, exceeding the four-card deck limit; this was corrected without changing policy or acceptance criteria. The actual presentation refresh request is issued before checking published stats. The earlier diagnosis that deck validation prohibited matching definitions was incorrect and was withdrawn after inspecting `DeckRules`.
- Final full evidence: `B-T4-full-core2/results.xml` **Core 3773/3777**; `B-T4-full-integration-final/results.xml` **CoreIntegration 208/213**; **zero skips and no new failures**. The exact nine failing fullnames match Phase A's accepted baseline. The focused final and Integration final also compile the final Systems adapter deletion. No baseline test was ignored.
- Confirmed locality: count/pool authoring and eligibility now stay within growth definitions/rules; Shop no longer consumes them. Shared composition changes still require stats/deck/attack/battle regressions. Registration uses the owning Buffer Factory, keys and Group; no root convenience service, copied final-trait state, duplicate progression state machine or general transaction layer was added.
- Production remains inactive: no operational steps/content, Main-shop products, View/Flow wiring, prefab/scene acceptance or legacy-node availability edit was supplied. The T3 release dependency remains. The user authorized Phase C next; no commits or pushes were performed.

## Phase C — Input receipts and authoritative progression gate

### T5: Process exact buffered choices and expose readiness

**Files:** Growth input/service files and Factory/Group/keys from §2; `S/ChapterProgressionCoordinator.cs`, `C/Contracts/Results/ChapterProgressionResult.cs` (contains `ChapterProgressionFailureReason`); create `T/CoreIntegration/{TraitGrowthCommandTests,TraitGrowthStageGateTests}.cs`.

**Interfaces produced:**

- Consumes T4's input/read/result/transaction contracts, including `bool AcknowledgeResult(RunId runId, int number)`; do not introduce a second acknowledgment counter or queue.
- `ITraitGrowthCommandProcessingService.ProcessNextTraitGrowthCommand(int expectedNumber) : bool`.
- Consumes `ITraitGrowthAvailabilityRead.ReadTraitGrowthProgress() : TraitGrowthProgressSnapshot`, implemented by `CharacterRosterService`. Do not add a boolean completion API that drops acknowledgment state.
- `TraitGrowthProcessedResult` contains request number, exact request context, and `TraitGrowthResult`.

- [x] Write `UnacknowledgedResult_PreventsNextPublication`, `DuplicateNumber_DoesNotReapply`, and `OldRunCommand_CannotAffectNewRun`; assert ordered handling, stable receipt until acknowledged, and no duplicate append.
- [x] Write `HighLevelAcquisition_BlocksAllNodeEntryPaths`: before any offer is opened, `ChoiceRequired` rejects Prepare/Confirm/revalidation with `PendingTraitGrowth`. After the final selection, `AwaitingAcknowledgement` still rejects with that reason; only after acknowledgment and `ReadyToContinue` does ordinary admission proceed. A configured Main participates in the same gate; an empty Main configuration does not block. Completed Run/active battle retain existing rejection policies. Existing stat level-ups remain applied while this gate waits.
- [x] Run red filters `TraitGrowthCommandTests|TraitGrowthStageGateTests` in Integration.
- [x] Implement the processor over T4's Buffer using Recovery's explicit expected-number/acknowledgment pattern. It prepares through the roster service, constructs the result, and installs record/offer/receipt atomically. A typed rejection is handled; no input/already handled input is false. Outstanding receipts prevent another publication; actual receipt acknowledgment remains the input consumer's explicit operation. Core reset is the only blanket input clear.
- [x] Construct the roster service before the progression coordinator and inject only `ITraitGrowthAvailabilityRead` into the latter. No service lookup cycle or optional-null bypass. Use fresh progress snapshots at Prepare/Confirm/revalidation, including direct battle-entry callers; reject both `ChoiceRequired` and `AwaitingAcknowledgement`. Check again after external definition reads and before installing stage progress. `ReadyToContinue` only removes the growth block, not other admission checks. Do not gate chapter-completion bookkeeping that determines a final Run outcome.
- [x] Register buffer/service/module contracts in their owning factories and expose through existing Group contexts. Reset input/receipt/offer together at NewRun; request numbers are not reused within a buffer lifetime. Validate Run identity even if a caller replays an old number after reset.
- [x] Run green filters, source-change integration tests, and full suites. Verify a new roster instance may be acquired while an earlier frozen offer exists without rerolling it; its own steps join the queue after that offer resolves. Under normal supported operations, unit/category/level changes cannot invalidate the active target; unsupported corruption raises a diagnostic error rather than choosing another target silently.

**Phase C gate:** Exact request/receipt handling, public progress projection, and entry guards work with public-contract test consumers, without View code. Keep production step lists disabled until external UI/Flow delivery. Core implementation can be reported complete with handoff/operational dependencies still pending. Stop and report.

### Phase C execution record — 2026-10-28

- Added the typed `ITraitGrowthCommandProcessingService.ProcessNextTraitGrowthCommand(int expectedNumber)` contract at `Core.TraitGrowthCommands`. It consumes the existing single pending Buffer input, delegates preparation to the roster service, and publishes through the existing atomic transaction. No second queue, acknowledgment counter or growth state was introduced. The processor neither acknowledges nor clears input on behalf of a consumer.
- Exact-number processing returns false for missing/already-handled input, an outstanding receipt, or a command canceled/replaced during definition reads. Typed rejection retains one exact request/receipt. Reentrant processing and acknowledgment of the same request cannot append twice. Old-Run requests remain failures in the current publication context and cannot be acknowledged using the old Run.
- `InGameCoreSystemFactory` now creates Characters before the progression coordinator and injects mandatory `ITraitGrowthAvailabilityRead`; the processor is registered once in the owning Core builder and exposed through the existing Core Group/key. No optional-null bypass, service lookup cycle or root convenience registration exists.
- Prepare/Confirm/revalidation reject `ChoiceRequired` and `AwaitingAcknowledgement` with appended `PendingTraitGrowth`. Prepare checks again after external stage validation. Direct battle entry already performs a final Prepare after definition/profile/draw calculation, followed by its original selection and roster-source comparisons; this shared gate supplies the new check without a redundant later DB read. Existing Shop, Recovery and buffered-node paths consume that same coordinator contract. Final chapter completion remains outside the new-node gate, and the actual coordinator test confirms pending growth does not prevent final Run bookkeeping.
- Tests cover higher-level acquisition before any offer exists, Main through the same contract, empty Main content, final-receipt waiting, all relevant node entry paths, source changes during definition reads, legal acquisition while an earlier offer is frozen, and unsupported active-target definition corruption as a diagnostic error. Existing B battle-state tests now enter before unlocking choices; explicitly injected impossible state is labeled as a Buffer-boundary fixture. No test was ignored or weakened to allow normal entry bypass.
- Red evidence: `Logs/TraitGrowthPlan/C-T5-red-valid/results.xml`, 13 cases with 11 intended missing-processor/gate failures and 2 passes, zero skips. Initial fixture errors were corrected before accepting red evidence. Initial focused green `C-T5-green1` passed 46/46. Supplemental tests exposed two fixture mistakes in the first full run (a forbidden level-decrease DTO and an omitted Combat type on the boss node); those fixtures were corrected without changing production policy.
- Final focused evidence: `Logs/TraitGrowthPlan/C-T5-green-final/results.xml`, **111/111 passed, zero skips**. Final full evidence: `C-T5-full-final/results.xml`, **4010 total / 4001 passed / 9 failed / zero skips**: Core **3773/3777**, CoreIntegration **228/233**. All **20 new T5 cases passed**. The exact nine failing fullnames match the accepted Phase A baseline; there are no new failures.
- Confirmed locality: future growth admission rules remain in the roster progress projection; the coordinator consumes that narrow contract. Shop/Recovery implementations did not acquire growth internals, and the existing Buffer transaction/lifecycle owners remain unchanged. Shared entry and result-publication changes retain necessary integration coverage.
- Independent T5 specification/quality and final combined B–C integration review passed with no actionable findings. Phase C is complete and execution stops here. Phase D–F work, external View/Flow/scene acceptance, production content/Main-shop delivery and enhancement-node availability changes remain unperformed. Operational steps stay disabled. No commits or pushes.

## Phase D — Core-to-View/Flow handoff

### T6: Deliver consumer contracts and integration examples

**Owner:** Core. Document delivery only; no UI/prefab/scene/Flow implementation.

**Files:** Create `docs/superpowers/handoffs/2026-10-27-category-trait-progression-handoff.md` in English and update execution evidence in this plan. Reference T4/T5 tests; if a missing boundary case is discovered, add it to the owning CoreIntegration test rather than a production View fixture.

**Inputs:** T4/T5 public offer, progress, request, receipt, and acknowledgment contracts. Handoff consumers must not need `CharacterRosterService`, validator, composer, or transaction internals.

- [x] Document the owning Group/read/write access paths and exact signatures, payload fields, preconditions, failure results, reset/unbind lifetime, and operation ordering. Include the four-state progress table from the design verbatim in meaning. Mark which reads are side-effect-free and which operations mutate Buffer.
- [x] Include concrete sequences for two Acquire steps plus one Enhance step, final-step result waiting, failed-choice retry, high-level acquisition outside combat, rebind with an unconsumed receipt, and Run termination/reset. Give a three-option Acquire payload and one-/two-option Enhance payloads; use fixture content, not invented production balance.
- [x] Explicitly tell View/Flow that Main is eligible through the same payloads, with content still empty/pending; never hide the choice UI solely because the target is Main. Show a fixture-only Main sequence. Explain that both Main and Sub have already received their existing XP/stat growth before the additional trait choice; the popup must not replace or replay level-up settlement/animation receipts.
- [x] State the consumer sequence: Flow reads fresh progress; on `ChoiceRequired` it explicitly prepares an offer; View reads the Buffer offer and submits one request; Core publishes a receipt; View consumes and acknowledges that exact Run/result; Flow reads progress again. `AwaitingAcknowledgement` cannot be treated as completion or used to request another offer. `Unavailable` follows existing battle/end policy rather than normal growth continuation.
- [x] Specify View acceptance requirements: a UISystem-registered `UIBase`, explicit input receiver/bindings, published names/descriptions/forms, existing TraitKey icon lookup, keyboard/controller submit, no dismiss-to-skip, no reroll on reopen, and settings/teardown input restoration. Let the View owner choose concrete components/layout/registration details consistent with the existing UI contract.
- [x] Specify Flow acceptance requirements: EndBattle and any chapter completion before growth; result confirmation before showing offers; normal continuation only at `ReadyToContinue`; no growth prompt for final victory/defeat/abandon; post-purchase growth only after the purchase transaction/result is complete. Preserve the existing Plan/Job sequencing owner.
- [x] Link automated Core evidence for shared eligibility parity, immutable selections, receipt barrier, stale input/acknowledgment, reset, and authoritative stage-entry rejection. Check each example against those contracts. If only documentation changed, perform document/signature checks rather than rerunning unrelated suites.
- [x] Record `handoff prepared` separately from `handoff delivered/accepted`. Do not send messages to another worker without explicit user authorization, and do not claim UI/Flow implementation exists.

**Phase D gate:** Core contracts and examples are reviewable; A–C evidence is linked; external owner work is clearly pending. Core/handoff delivery can stop here without implementing a surrogate UI.

### Phase D execution record — 2026-10-28

- Prepared [the Core-to-View/Flow handoff](../handoffs/2026-10-27-category-trait-progression-handoff.md) in English. It documents the actual owning Group/Buffer access paths, exact operations/payloads, current-Run receipt ownership versus original request identity, side effects and four-state progress priority.
- Worked fixture sequences cover two Acquire rows and one Enhance row in their actual configured order, one/two/three displayed choices, Main through the common contract, final-result waiting, failure/retry, high-level acquisition and Run termination/reset. Examples are illustrative valid snapshots using existing fixture content, not production balance or asserted seeded samples.
- The handoff explicitly preserves settled XP/stat grants and the existing Plan/Job sequencing owner. It separates display teardown from acknowledgment/reset, and records that Flow must retain an unprocessed request's returned number across presentation unbind because the current input read API is exact-number lookup only.
- View requirements cover UISystem/UIBase registration, explicit input receivers, EventSystem Submit, TraitKey visuals, required choices, settings and input restoration. Flow requirements cover result confirmation, receipt barriers, node admission and continued/ended Run ordering. The obsolete fixed-slot handoff is identified as superseded without rewriting its historical contents.
- Validation: read current owning contracts and consumer/registration paths; checked all local links (zero missing targets); checked retained XML/evidence. Independent document/contract review completed. Corrected two factual descriptions: `SystemGroupsService` is public while `IBufferQuery`/`BufferSystemKey` are internal, and late old-Run requests cannot grant traits but may publish a current-Run failure receipt requiring acknowledgment. No runtime code changed. No Unity rerun is required for this documentation-only change, and none was performed.
- Status is `handoff prepared`, not externally delivered/accepted. No message was sent to another worker, and no View/Flow implementation, scene/prefab/content edit, commit or push was performed.

## Phase E — External View/Flow integration and joint contract acceptance

### T7: Check the external implementation against the handoff

**Owner:** View/Flow owners implement. Core reviews boundary behavior and supplies Core-side integration support only when authorized; this Phase does not transfer UI/Flow ownership to Core.

**External locations to hand off:** §2's View/UI/Flow rows, UI registration/input layers, and eventual Combat/StageSelect scene connections. Suggested joint tests: `T/CoreIntegration/TraitGrowthPresentationTests.cs` and `TraitGrowthFlowTests.cs`, owned by the external implementation or a separately agreed integration test task.

**Core contract consumed:** `ReadTraitGrowthProgress()` returns `TraitGrowthProgressSnapshot`; offer preparation returns the same contract. Flow may include that immutable projection in its existing progress facts. It must not replace it with a pending-step boolean or independently interpret internal receipt/history counters. No new general Flow engine or duplicated queue is required.

- [ ] **External View evidence:** one/two/three displayed options match the published offer; pointer and EventSystem submit send one request while waiting; failure/rebind retains the correct offer/result; settings and teardown release only owned input. Core reads evidence and reports any contract mismatch without editing View internals.
- [ ] **External Flow evidence:** continuing victory executes playback → EndBattle → optional chapter completion → result confirmation → growth choices/acknowledgments → transition once; normal completion never skips `AwaitingAcknowledgement`.
- [ ] **Joint boundary evidence:** final victory/defeat/abandon do not wait for discarded growth; one settlement can cross multiple steps/units in the specified order; high-level recruitment resolves before another node starts; the final receipt must be acknowledged before another offer or stage entry. Use real public calls, not direct mutation of internal flags.
- [ ] **Core contribution:** rerun affected T4/T5 consumer-contract tests when the integration changes Core contracts or exposes a failure. Keep actual View/Flow automated/manual verification separate. New contract decisions return to the design/plan; do not silently weaken the gate to accommodate a consumer.
- [x] Record external artifact/evidence references and the owner of each unresolved issue. If external implementation is unavailable, mark E `pending external integration`; A–D completion remains valid and end-to-end acceptance remains unproven.

**Phase E gate:** Joint contract acceptance is complete only when actual external evidence exists. No scene/UI/Flow edits are implied by a request to review this Phase from the Core side.

### Phase E inspection record — 2026-10-28

- **Result: pending external integration.** The authorized inspection and gap/owner ledger are prepared; E's View, Flow and joint acceptance are not complete. See [handoff §9](../handoffs/2026-10-27-category-trait-progression-handoff.md#9-phase-e-inspection-and-acceptance-ledger).
- Current-checkout source search found no `TraitGrowth` / `PendingTraitGrowth` consumption in ViewSystem, LifeCycleSystem, GlobalSystem/UISystem or View Factory code. Reviewed the current Combat Plan/progress reader and Stage Plan/lifecycle locations; no growth cycle is connected there. `UIIDs.cs` has no growth-panel registration, and no growth-named UI prefab/scene file or `TraitGrowthPresentationTests` / `TraitGrowthFlowTests` was found. These are scoped repository findings, not proof about another worker's unprovided branch or external artifacts.
- No external View/Flow artifact, owner acknowledgment, actual scene/input demonstration or integration test evidence was supplied. Therefore Core tests cannot close the external acceptance checkboxes. Required evidence and responsible View/Flow/Core/content owners are listed in the handoff.
- No integration-driven Core contract change or new runtime failure was found during this inspection, so existing T4/T5 execution evidence is referenced rather than rerun. Actual consumer sequencing, teardown, keyboard/controller input and scene acceptance remain unverified.
- Production step lists remain disabled. Main level-up content, Main-shop delivery or approved obsolete-node disabling, and Phase F activation remain separate dependencies. This request does not authorize a substitute UI/Flow implementation or Phase F content edits. No commits/pushes.

## Phase F — Separate Core/content and View/Flow activation evidence

### T8: Validate approved InGame assets and record release readiness

**Core/content files:** `Assets/ProjectL/DataBaseSO/InGameGroup/TraitGrowth/`, `.../StageMap/Item/StageMap_Chapter1.asset`, `.../Stage/Root/Stage_Enhancement.asset`, `.../Stage/Enhancement/Enhancement_Default.asset`, `Assets/ProjectL/DataBaseSO/InGameGroup/InGameGroup.asset` referenced by `Assets/ProjectL/DataBaseSO/DatabaseCatalogSO.asset`; create `T/CoreIntegration/TraitGrowthAssetTests.cs`. Update this plan and the T6 handoff. Map/content edits require that owner's approved availability decision.

**External files:** UI prefabs, trait visual assets, and `Assets/Scenes/{Combat,StageSelect}.unity` remain View/Flow-owned; Core only records their acceptance evidence.

**Inputs before operational activation:** approved pools/effect definitions/step balance for the categories being enabled, external UI/Flow delivery, and a content-owner decision that either the separately implemented Main shop is ready or old enhancement nodes are disabled. Main's empty/pending level-up content is valid and does not prevent approved Sub activation. Main-shop availability is a separate dependency from Main level-up content. Data can be prepared and tested in a fixture catalog without enabling an incomplete production path.

- [ ] **Core/content:** inventory the four legacy enhancement assets and table/stage/map references. Classify compatible retained versus deferred/unsupported effects. Do not spread unit-specific effects to a whole class automatically or delete protected assets.
- [ ] **Core:** write `ProductionCatalog_AllCategoryPathsHaveValidChoices` and `ProductionMaps_HaveNoObsoleteReachableSubEnhancementShop`, with actionable asset/category/unit/step or map/node diagnostics. Run the red/green Integration `TraitGrowthAssetTests` filter against the approved configuration.
- [ ] **Core/content:** create/connect the two trait tables, Main/Heavy/Rider/Cannon configurations, and approved definitions through the InGame group. Keep Main steps/pool empty until its actual content is decided; do not copy Sub candidates there or claim Main content activation. Remove the old active Sub recipe route while preserving serialization/history. Keep categories awaiting external integration disabled; an intentionally empty configuration is not evidence of active choices.
- [ ] **Core/content:** implement the approved shop-availability branch in the owning data scope, retain rejection of legacy Sub commands, and audit authored/random/pinned references. Do not substitute encounters or Main rewards without approval.
- [ ] **Core:** run `Invoke-TraitGrowthTests 'F-core-regression' 'ProjectL.Tests.Core;ProjectL.Tests.CoreIntegration' ''`, require nonzero expected counts, inspect new failures/skips, and retain XML/log paths. Include any restored required assemblies. Verify real InGame SO changes on a fresh runtime/Run through public Core/Buffer contracts: configured counts/levels, final acquisition's three distinct candidates, enhancement options, native-inclusive totals, and next-battle composition.
- [ ] **External View/Flow evidence:** registered scene-local UI instances, required visual/prefab links, real Bootstrap/Group/input connections, actual selection display, settings/back/submit/rebind, and all T7 outcome sequences. Core does not edit these files to obtain a green checklist. Do not add feature-specific GameBootstrap logic.
- [ ] **Joint record:** report `Core implementation`, `Core asset validation`, `handoff`, `external View/Flow integration`, and `production activation` individually with evidence or pending owner/dependency. Enable operational steps only after both sides and content/node prerequisites are ready. No commit/push.

**Phase F gate:** Core validation may be complete while external acceptance is pending. Production readiness requires both. New trait effects and Main-shop product development remain separately authorized work.

## 5. Dependency, scope, and self-review record

| Spec requirement | Task coverage |
|---|---|
| Category pools, arbitrary counts, ordered unlock levels, unchanged native traits | T1/T2/T4/T8 |
| Main eligible through common contracts, operational Main content undecided | T1/T2 definitions; T4/T5 fixtures; T6 handoff; T8 explicit empty Main configuration |
| Existing Main/Sub XP and stat level-ups remain additive and independent | T4 coexistence + existing level-growth regression; T6/T7 consumer ordering |
| Exact three acquisitions, up-to-three enhancements, reachable-path validity | T1/T4/T8 Core; T6 handoff and T7 external display acceptance |
| Shared validator/runtime eligibility | T1 implementation; T4 parity/integration tests |
| Frozen offers, deterministic sampling, stale-request rejection | T4/T5 Core; T6 handoff and T7 external consumption |
| Buffer SSOT, append-only history, atomic state/receipt publication | T4/T5 |
| Derived progress, last-result acknowledgment barrier, no hidden completion ordering | T4/T5 Core; T6 public sequences; T7 joint acceptance |
| Shared composition across stats/deck/profiles/next battle | T4 |
| Multiple level jumps and higher-level acquisition | T4/T5/T7 |
| Non-combat choice, required entry gate, continuing versus ended Run | T5/T7 |
| Registered UI, input layers, settings, teardown/rebind | T6 requirements; T7/T8 external View/Flow evidence |
| Main-only shop boundary and safe legacy-asset transition | T3/T8; Main products are an external plan |
| SOLID/FW contracts and change locality | T2/T4/T5/T6 registration and ownership checks |

Self-review rules for execution: verify create/modify paths against the current checkout; use the same named contracts across tasks; keep no unimplemented compatibility API; trace Factory creation → local Export → owning Group → consumer retrieval; preserve required shared-state regressions. Changes to pure growth balance should not require unrelated Main-shop internals after separation, while changes to shared composition still require combat integration tests.

Revision 3 self-review: explicit ownership, shared eligibility, and acknowledgment-aware progress remain consistent across tasks. Main is supported in category/record/service/composition contracts with operational content pending; both existing Main/Sub stat-level-up paths are preserved and covered by coexistence tests. Core completion, handoff preparation, external acceptance, and release activation remain distinct. No implementation, Unity test, or manual game verification was performed while updating these documents.
