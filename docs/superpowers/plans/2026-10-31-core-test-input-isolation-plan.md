# Core Test Input Isolation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. Both phases are complete; the camera fixture remains the explicit exception.

**Goal:** Make Core-owned tests independent of production SO content while retaining real conversion, calculation, and publication coverage.

**Architecture:** Tests create their own in-memory SOs only when checking authoring conversion. Pure data and existing Core integration fixtures supply all other inputs. Production adapters, factories, Core services, and Buffer responsibilities remain unchanged.

**Tech Stack:** Unity 6000.0.73f1, C#, NUnit/EditMode, existing Core/CoreIntegration assemblies.

**Spec:** `docs/superpowers/specs/2026-10-31-core-test-input-isolation-design.md` (approved by the user in this chat).

## Global Constraints

- Preserve `CombatCameraZoomPoseTests.cs` exactly, including its known failure and assembly membership. Do not edit View or FBX tests.
- Preserve `ProjectL.Tests.Core` with `noEngineReferences: true`.
- Do not modify production SOs or runtime code. Test-owned SOs use `ScriptableObject.CreateInstance` and are destroyed after the test; Phase A creates no `.asset` files.
- Preserve the user's concurrent main-specialization code, tests, documents, and FMOD changes. Work in the requested current feature checkout; do not stash, reset, or commit unrelated work.
- Use Korean comments and English implementation documents. Add `.meta` files for new C# files; preserve GUIDs for existing files.
- Implementation phases initially excluded commit/push. After both phases were reviewed, the user explicitly authorized committing and pushing only this test cleanup and its documents, excluding FMOD changes.
- Run Unity through the approved AppData-capable path. Do not launch a second editor while another verification process owns the same project.

## Review Focus

- Trait order and trait count: configured-value assertions must not depend on the current ShotGun list; use explicit test inputs and select the tested trait by type/key.
- Copy isolation: editing a copied SO must not mutate its source, previously converted data/specs, or installed runtime snapshots.
- Growth payloads: normal/enhanced forms must retain the same key and independent positive clocks; mismatched keys or invalid clocks must still be rejected.
- Boss preparation: a non-default configured stage key must resolve through the real database battle source, and the prepared objective must identify the placed boss.
- Resource ownership: every in-memory SO is cleaned up even when an assertion throws; no dependency may silently return to the production catalog.

## Phase A — Current Core Failures and Their Fixture Dependencies

### Task 1: Isolate authoring conversion and immutable-copy tests (A1)

**Files:**
- Create: `Assets/Tests/EditMode/CoreIntegration/CoreAuthoringFixture.cs` and `.meta`.
- Modify: `Assets/Tests/EditMode/CoreIntegration/DataBaseAssetTests.cs`.
- Modify: `Assets/Tests/EditMode/CoreIntegration/HuntersMarkAuthoringTests.cs`.
- Modify: `Assets/Tests/EditMode/CoreIntegration/RetiredTraitAuthoringTests.cs`.
- Modify: `Assets/Tests/EditMode/CoreIntegration/TraitTurnConfigurationTests.cs`.

**Interfaces:**
- `CoreAuthoringFixture : IDisposable` owns only objects it creates/clones.
- `T Create<T>() where T : ScriptableObject` and `T Clone<T>(T source) where T : ScriptableObject` track cleanup.
- `UnitDataSO CreateUnit(EntityKey key, CategoryKey category, params UnitTraitAuthoring[] traits)` builds a valid, explicit unit and targeting SO input.
- `static void Set(object target, string field, object value)` sets test-owned private authoring fields and fails if a field does not exist.
- Consumes the existing `GetData()`, `TraitSpecAdapter`, `UnitSpecAdapter`, `OwnedCharacterStatsCalculator`, and `ConfiguredMovementBattle` paths. The fixture does not implement game validation or expected calculations.

- [x] Run the five affected test fixtures before editing; record the expected six production-dependent failures from this chat and any concurrent-work baseline differences.
- [x] Add the owned SO fixture; explicitly configure unit identity, category, valid stats, targeting, rewards, and traits. Destroy objects in reverse creation order.
- [x] Replace production unit/catalog/GUID reads with fixture inputs. Preserve tests for regeneration amount/clock, HuntersMark percentage/duration, owned projection, removed priority-reduction fields, retired types/IDs, Pull projection, and UnitSpec conversion.
- [x] Replace the growth-asset scan and Vanguard clone with explicitly constructed growth definitions, including normal/enhanced key agreement and invalid payload/clock checks. Record that production GUID/content assignment coverage is intentionally no longer part of the Core suite.
- [x] Exercise at least two non-default values where relevant; keep the existing default authoring tests. Confirm copy edits cannot mutate original SO data, produced data/specs, or battle snapshots.
- [x] Run the authoring fixtures; expect all cases to pass without loading production assets. Review the focused diff and production-path scan.

### Task 2: Isolate boss-stage preparation input (A2)

**Files:**
- Modify: `Assets/Tests/EditMode/CoreIntegration/BossStagePreparationTests.cs`.
- Modify only the stage lookup if necessary: `Assets/Tests/EditMode/CoreIntegration/CoreIntegrationTestData.cs`.

**Interfaces:**
- Reuse `CoreIntegrationTestData.SetUnit(UnitData)` and `SetStage(StageRootData)`.
- Preserve `CoreIntegrationTestData.DatabaseSource(buffers).Read(...)`, actual runtime initialization/roster/deck setup, and `BattlePreparationCalculator.Prepare(...)`.

- [x] Configure a boss unit with Roar and disabled voluntary movement/basic attack, explicit enemy placement, and a designated-boss objective at stage key 2101; retain the source/preparation/placement/objective assertions.
- [x] Run the new boss fixture before changing the shared stage lookup. Expect failure if `GetStage` still only recognizes 1101.
- [x] Make `GetStage(int key)` accept the configured `_stage.StageKey`, retaining rejection for missing keys. Do not bypass production source lookup.
- [x] Run the boss and affected authoring fixtures together; expect all cases to pass. Confirm neighboring tests that use the default stage key remain covered by the assembly regression run.

### Task 3: Validate, review, and stop (A3)

- [x] Run the full `ProjectL.Tests.Core` and `ProjectL.Tests.CoreIntegration` assemblies in Unity EditMode. The deferred camera case remains a separately reported failure; investigate any other failures against the current shared-workspace baseline.
- [x] Verify no production asset loading, asset discovery, or production file reads remain in the five Phase A test files. Verify the camera file and runtime/production assets are unchanged by this task.
- [x] Have an independent reviewer inspect only this phase's changes, with special attention to the Review Focus above and lost assertions.
- [x] Record results, any deliberately removed content-only checks, and concurrent-work limitations in this plan. Stop for user review before Phase B.

## Phase B — Remaining Dependencies (Authorized)

The user authorized Phase B and allowed camera-test deletion only if it is unnecessary. Inspection found meaningful zoom/rotation/pan/immutability/configuration coverage, so retain the camera fixture unchanged. Its assembly placement and known type-name failure remain a documented exception.

### Task 4: Isolate category, map, and retirement inputs (B1)

**Files:** `Assets/Tests/EditMode/CoreIntegration/CombatActionUsePublicationTests.cs`, `ChapterStageDistributionAuthoringTests.cs`, and `LegacyEnhancementRetirementAuthoringTests.cs`.

**Interfaces:** Reuse `CoreAuthoringFixture.Create<T>()`, `SerializedObject`, the existing category/map/stage SO conversion, `ChapterStagePool`, and `EnhancementTable`.

- [x] Record the six-fixture baseline before changes. Expected: current Core behavior passes; content independence is a fixture refactor, not a product behavior change.
- [x] Configure category movement inputs explicitly for `(3,4,3,2,0)` and `(5,6,7,8,1)`; verify conversion and absence of removed cost fields.
- [x] Replace production map enumeration with configured SO rules and explicit candidate keys. Verify an active weight or minimum rejects missing candidates through `ChapterStagePool`, while an inactive rule accepts an empty pool. Retain existing map serialization/validation and real generation integration tests.
- [x] Replace the production retirement catalog smoke test with an explicit empty enhancement stage/table. Keep all unknown/retired authoring rejection cases; document removal of current content membership checks.
- [x] Run the three fixtures; expect all cases to pass without loading production SOs.

### Task 5: Isolate attack authoring and preserve execution paths (B2)

**Files:** `Assets/Tests/EditMode/CoreIntegration/CoreAuthoringFixture.cs`, `ImpactBoltAuthoringTests.cs`, and `PullAuthoringTests.cs`.

**Interfaces:** Add `TargetingMethodDataSO CreateTargetingMethod(int key = 1)` and an overload `UnitDataSO CreateUnit(EntityKey key, CategoryKey category, TargetingMethodDataSO method, params UnitTraitAuthoring[] traits)`; preserve the original unit helper signature as a delegating convenience. Reuse `CoreIntegrationTestData.SetUnit/SetTargetingMethod` and actual `BattleDefinitionAdapter.ReadAttackProfiles`.

- [x] Build ImpactBolt inputs explicitly: unit key Cowboy, Cannon category, range 4, attack 100; separate method key 7, RadialSquare/Single/Unrestricted/Cross with splash 40. Tests continue changing distance, range, shape, angle, and splash before real query/preview/command processing.
- [x] Replace production Ballista checks with configured unit/targeting conversion. Replace or remove production-only Hammer/shared-unit assignment freezes, retaining actual shared-method and prior-snapshot isolation checks.
- [x] Build Pull unit/targeting inputs explicitly and verify conversion through the real attack-profile adapter. Replace production shared-method checks with an explicit shared-key scenario and check unrelated registered values remain intact.
- [x] Run ImpactBolt/Pull fixtures, including all execution/projection/immutability cases. Expected: all pass with no production paths and no changed product assertions merely to fit the new fixture.

### Task 6: Isolate real InGame registration (B3)

**Files:** `Assets/Tests/EditMode/CoreIntegration/TraitGrowthAuthoringTests.cs`.

**Interfaces:** Existing local `New<T>()`/`Table<T>()` own table objects; `CoreAuthoringFixture` owns configured unit/targeting inputs. `Group(params DatabaseTable[])` supplies all required InGame tables, one explicit category config and artifact-board config, and a playable Heavy unit for candidate-sufficiency validation. `CreateDatabase(InGameGroup)` returns `IInGameQuery` obtained from the actual factory's exported services and a real builder.

- [x] Construct a valid test-only required table set. Do not load `DatabaseCatalogSO` or any View/Render group, and do not duplicate factory checks.
- [x] Prove the absent growth pair builds and exposes disabled queries, and a valid present pair builds and exposes registered data.
- [x] Keep missing-pair, duplicate-category, missing-required-category, duplicate-trait, unregistered-pool, and candidate-sufficiency tests. Check the expected error cause, not only `InvalidOperationException`, so unrelated invalid fixture setup cannot masquerade as success.
- [x] Keep the existing temporary-asset serialization round trip and cleanup; these files are test-owned and never sourced from production content.
- [x] Run the growth fixture; expect every case to pass through the actual InGame factory and service export path.

### Task 7: Validate Phase B and report the ownership boundary (B4)

- [x] Run all six Phase B fixtures, then Core/CoreIntegration regression. Report any untouched camera or concurrent-feature failures separately.
- [x] Scan the whole Core/CoreIntegration source set for production paths, asset discovery, and content reads. The growth serialization test's own temporary asset loads are allowed; the camera fixture remains the explicit exception.
- [x] Independently review Phase B changes for preserved Core coverage, required registration validation, input ownership, and cleanup.
- [x] Update results and intentionally removed production-content checks. Do not commit/push or modify concurrent work.

## Execution Record

- Delivery authorization after Phase B: commit and push only the test-input-isolation changes and these design/execution documents. Exclude the pre-existing FMOD modification and leave the camera fixture unchanged.

- Design approved; inline Phase A execution authorized. No implementation changes existed before this plan.
- Initial workspace contains concurrent main-specialization work. The currently running Unity process is a separate main-specialization Core validation and must finish before this phase starts its own runner.
- Prior merge baseline: Core 4,359/4,359; CoreIntegration 470/477. These are historical counts and will not substitute for fresh phase verification.

### Phase A result — 2026-10-31

- Before changes, the five targeted fixtures reproduced 30 passed / 6 failed / 0 skipped (`Logs/core-input-phase-a-before.xml`).
- The four isolated authoring fixtures passed 42/42 (`Logs/core-input-phase-a-authoring.xml`). The initial new helper had a missing namespace import, fixed before this successful run.
- Configured boss keys 2101 and 3407 both failed first at the existing fixture's hard-coded 1101 lookup (`Logs/core-input-phase-a-boss-red.xml`). The lookup now compares against `_stage.StageKey`. A first test-input choice of a 2x2 boss was rejected by the existing 1x1-only attack-profile contract; the fixture uses a supported 1x1 boss. No production contract was changed.
- All Phase A target cases passed 44/44 with zero skips (`Logs/core-input-phase-a-targeted.xml`).
- Full regression (`Logs/core-input-phase-a-regression.xml`): Core 4,398/4,410; CoreIntegration 491/492; zero skips. All twelve Core failures were in the concurrently added, untouched `MainSpecializationCombatTests`. The only CoreIntegration failure was the explicitly deferred camera type-name case. These results describe the shared workspace at the time of the run, not acceptance of the concurrent feature.
- Independent review found no actionable Phase A defect. It confirmed actual source/preparation routing, input ownership, cleanup, conversion/projection coverage, and copy isolation.
- The five scoped test files and the helper contain no production asset loads, asset discovery, production file reads, or full production catalog construction. The camera file's SHA-256 matches the phase-start snapshot. The user's pre-existing FMOD change also matches the phase-start snapshot.
- Coverage intentionally replaced: production asset GUIDs, current unit-to-trait assignments, discovery of current growth assets, and production-catalog smoke checks. Their replacements verify configured authoring/adapter contracts, immutable conversion, owned projections, and boss preparation. This does not repair or certify production stage 2101 registration. Real InGame registration isolation remains Phase B work.
- Changed implementation scope: six existing test/fixture files and one new test-only helper plus its `.meta`. No runtime implementation, production SO, camera/View/FBX test, or assembly definition was changed by this task.
- No commit or push was performed for this follow-up. Stop here for Phase A review; the six Phase B files still have production-content dependencies.

### Phase B result — 2026-10-31

- The user reviewed Phase A and authorized Phase B. The conditional camera-deletion option was evaluated: retain useful camera coverage unchanged rather than remove it. This does not certify the currently failing type-name case.
- Baseline six-fixture run: 67/67 passed, zero skips (`Logs/core-input-phase-b-before.xml`). This phase is a test-input refactor; it does not change product rules.
- Focused checks passed: category/map/retirement 29/29 (`Logs/core-input-phase-b-basic.xml`), ImpactBolt/Pull 31/31 (`Logs/core-input-phase-b-attacks.xml`), and growth registration/serialization 10/10 (`Logs/core-input-phase-b-growth.xml`). Initial test input used optional Shop, but the actual map contract requires Shop/Recovery minimum 1; the optional-rule case correctly uses Enhancement. A misplaced using declaration was corrected before the successful growth run.
- Full regression (`Logs/core-input-phase-b-regression.xml`): Core 4,425/4,425; CoreIntegration 577/578; total 5,002 passed, one failed, zero skipped. The only failure is the unchanged camera lookup for `Cinemachine.CinemachineCameraOffset`. All 70 Phase B cases and all 44 Phase A cases passed in that same run.
- Concurrent main-specialization work advanced HEAD to `f6350a289` during this phase. This task did not create that commit, and the earlier phase's concurrent Core failures are absent in the latest run. No unrelated implementation was edited or reverted.
- Independent Phase B review found no actionable defect. It confirmed actual adapter/query/preview/execution coverage, immutable inputs, the factory/export/builder/query registration route, and the playable Heavy profile that makes candidate insufficiency observable. Invalid registration tests verify the intended cause after a successful control configuration.
- Whole Core/CoreIntegration source inspection found no production SO path, production catalog load, asset discovery, or production file read. Remaining `AssetDatabase.LoadAssetAtPath` calls read only the growth serialization test's own temporary files, which its cleanup removes. Real `InGameDataBaseFactory.Create` calls intentionally remain because they are the registration contract under test.
- Intentionally removed content policies: current category balancing defaults, production map membership, legacy-product/current growth-pool catalog membership, and Ballista/Hammer/shared-unit asset assignments. Replacements verify explicitly configured conversion, candidate validity, empty-table behavior, attack profiles, shared-reference isolation, and real registration. Actual production content validity is outside this suite's acceptance claim.
- The camera and pre-existing FMOD file hashes match the Phase B start. No runtime code, production SO, camera/View/FBX test, or assembly definition was changed by this test-cleanup task. No commit or push was performed for these changes.
