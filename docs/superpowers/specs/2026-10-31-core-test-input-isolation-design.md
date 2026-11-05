# Core Test Input Isolation

## Status and scope

Approved design. Phases A and B are implemented, verified, and independently reviewed. The user requested that Core-owned tests verify Core behavior without failing when production ScriptableObject content changes. Execution evidence is recorded in `docs/superpowers/plans/2026-10-31-core-test-input-isolation-plan.md`.

The user explicitly deferred changes to `CombatCameraZoomPoseTests.cs`. Preserve that file, its assembly membership, and its known failing case. Do not change FBX editor tests or View implementation. The prior merge resolution added no tests.

When authorizing Phase B, the user allowed deleting the camera fixture if it was unnecessary. It contains useful zoom, rotation, pan, repeated-motion, immutable-setting, and invalid-configuration coverage, so it was retained unchanged as the explicit exception.

## Current evidence

The merge verification report is `Logs/merge-20261031-editmode.xml`: 4,888 passed, 9 failed, and 1 skipped. Core passed 4,359/4,359; CoreIntegration passed 470/477. The remaining failures belong to the separate FBX editor test assembly.

Five CoreIntegration failures share a production-content assumption. `Unit_ShotGun.asset` currently contains Sacrifice, SoloAction, and CallOfBlood. Tests expect either exactly one trait or a HuntersMark trait at index zero:

- `DataBaseAssetTests.ProductionShotGun_HasExplicitDurationOneAndUnchangedMagnitudeAndGuid`: no HuntersMark element for `Single()`.
- `HuntersMarkAuthoringTests.OwnedProjection_PreservesActualAuthoredPercentageWithoutChangingAsset(10)` and `(25)`: three traits violate `Single()` before the configured-value assertion runs.
- `RetiredTraitAuthoringTests.ProductionShotgun_UsesMarkTrait`: no HuntersMark element for `Single()`.
- `TraitTurnConfigurationTests.UnitCopy_CarriesDurationAndFreezesAlreadyBuiltDefinitions`: the first trait is Sacrifice, so `bonusDamagePercent` is not a serialized property.

`BossStagePreparationTests` asks the active production database for stage 2101. `Stage_Boss.asset` exists, but its GUID is absent from the registered `StageRootDataTable`; lookup fails before preparation is exercised.

The deferred camera failure uses `Cinemachine.CinemachineCameraOffset`, while the installed Cinemachine 2.10.7 source declares `CinemachineCameraOffset` in the global namespace. The reflection lookup fails before camera validation.

The FBX bake test selects ProgressiveGPU during a `-nographics` run and reports `RenderTexture.Create failed`. The FBX catalog test throws inside `EditorWindow.Close()` from its `finally` block; it constructs a window without showing it. The cleanup failure is confirmed, but its underlying Unity window-state cause has not been independently reproduced.

## Ownership boundary

- `ProjectL.Tests.Core`: pure Domain/Application/Contracts behavior. Preserve `noEngineReferences: true`.
- `ProjectL.Tests.CoreIntegration`: Core service composition, Core database input conversion, command processing, and Buffer publication/transaction contracts. Unity editor APIs are permitted for explicitly created test SOs and serialization round trips.
- CoreIntegration tests must own their input values. They must not load the production catalog or discover production assets to obtain a fixture.
- A value such as a percentage, movement distance, trait set, or stage key may be asserted exactly when the test explicitly configures that value.
- Production asset GUIDs, balancing values, current unit-to-trait assignments, content pool membership, View presentation, and rendering are not Core correctness assertions.
- Core-owned presentation snapshots and result events remain covered: these are Core publication contracts, even when their consumers are View code.
- The camera test remains an explicit, temporary exception at the user's request.

## Approach

Use test-owned input fixtures and the existing production conversion and composition paths. This is the approach consistent with the user's stated boundary.

Updating expected values to match today's SOs would preserve the same coupling. Skipping failing assertions or replacing real factories with duplicated validation would weaken Core coverage. Creating a new content-validation suite is outside this task; no production-content assertions will be presented as retained coverage after they are replaced.

Reuse `CoreIntegrationTestData` and `ConfiguredMovementBattle` for Core service and battle tests. For authoring tests, create SOs in memory and explicitly configure all relevant fields before calling `GetData()`, adapters, or runtime services. Preserve separate produced values and mutation checks.

Where a test exercises registration itself, construct the required InGame SO tables and invoke the existing InGame factory. Do not import View/Render catalog groups or replace the factory's validation with a second implementation. A small test-only fixture may own object creation and disposal if several tests need the same registration setup. Keep feature-specific payloads in the feature test.

## Implementation phases

### Phase A: Remove the dependencies responsible for current Core failures

Review checkpoint after this phase.

- Replace every production-content dependency in `DataBaseAssetTests`, `HuntersMarkAuthoringTests`, `RetiredTraitAuthoringTests`, and `TraitTurnConfigurationTests` with explicit test-owned SO/data inputs. This includes ShotGun, regeneration, the Shielder attack-reduction input, the Cowboy/retired-trait input, the Vanguard growth-definition clone, and the scan of production growth definitions. The growth checks must use explicit normal/enhanced payloads and preserve key/clock validation rather than scanning today's asset list.
- Preserve configured percentages, duration conversion, immutable copies, owned-stat projection, retired-type rejection, and live Core behavior checks. Rename production-specific tests to describe the actual Core contract under test.
- Replace the production catalog in `BossStagePreparationTests` with an explicit valid boss stage/unit fixture, preserving the real database-source/preparation route and boss placement/objective assertions.
- If the existing test database is reused for a non-default stage key, make its lookup honor the configured stage's key; do not accept an unrelated hard-coded key or bypass the real battle-definition source.
- Preserve existing `.meta` GUIDs if a test file is renamed; do not alter production SOs.
- Record any coverage whose only purpose was a production-content policy and explain its replacement or removal; do not silently delete meaningful behavior checks.

### Phase B: Remove the remaining production fixture dependencies

Start only after the Phase A review checkpoint.

The remaining affected files are `CombatActionUsePublicationTests`, `ChapterStageDistributionAuthoringTests`, `LegacyEnhancementRetirementAuthoringTests`, `TraitGrowthAuthoringTests`, `ImpactBoltAuthoringTests`, and `PullAuthoringTests`.

- Test configured category distances instead of the current production defaults.
- Test explicit map candidates and retirement contracts instead of enumerating current content.
- Give trait-growth registration tests a test-owned InGame database; preserve missing-table, duplicate-key, invalid-reference, candidate-sufficiency, and serialization coverage.
- Establish a successful valid baseline for registration fixtures before exercising a malformed input. Check the relevant error cause so an unrelated missing required table cannot satisfy an expected `InvalidOperationException`.
- Give ImpactBolt and Pull tests explicit unit/targeting inputs; preserve query, preview, execution, per-instance projection, and immutability coverage.
- Keep temporary serialization assets confined to test-owned folders with existing cleanup.

## Change, understanding, and verification scope

Before: a balance or content edit can require understanding unrelated tests' assumptions about the production catalog, unit trait ordering, GUIDs, or unrelated View/Render groups.

After: feature tests own the values they assert; their required understanding is the feature's authoring/adapter/Core/publication contract and shared fixture contracts. Production content changes do not change these inputs. Registration tests still exercise actual required table composition, because that dependency is part of the contract.

The test suite must not duplicate Core rules, state ownership, Framework service registration, or Buffer transactions. No new runtime abstraction or runtime-state copy is proposed. Production behavior and public APIs remain unchanged.

## Validation

- Run the affected fixtures after each phase using Unity 6000.0.73f1 through the approved AppData-capable execution path.
- Run `ProjectL.Tests.Core` and `ProjectL.Tests.CoreIntegration` as the regression boundary. Report the unchanged camera failure separately; do not claim the complete CoreIntegration assembly is green while that exception remains.
- Verify configured values across more than one valid input where the contract is configurable. Confirm previously returned data/snapshots remain unchanged after editing a test-owned SO.
- Audit production paths, catalog loads, asset discovery, and View references after Phase B. Test-created serialization assets and the deferred camera file must be distinguished from production dependencies.
- Keep the user's unrelated changes intact. Do not run the full View/FBX suite for this Core-only follow-up.
- Do not modify production data or weaken assertions solely to make counts pass. Native test results, static review, and unexecuted manual checks must remain distinct.
