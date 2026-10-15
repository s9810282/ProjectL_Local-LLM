# Task 8R2 — Current Boundary Regression Coverage

## Outcome and scope

Task 8R2 closes preparation, movement, deployment, draw and turn regression mapping for the currently approved one-cell-unit rules. It extends three existing test fixtures; it does not change runtime behavior, retire production files or complete the full Task 8 migration.

Execution baseline: branch `feature/Cheon/refactor/core-system`, HEAD `83f485d1`, after the authorized Stage branch merge. Incoming working-tree documentation and importer/cache changes were preserved. The earlier 8R1 baseline is used only to compare existing failures and case counts; the verification below is a fresh execution.

Changed tests:

- `Assets/Tests/EditMode/BattlePreparationRuleTests.cs`
- `Assets/Tests/EditMode/CombatCommonCommandTests.cs`
- `Assets/Tests/EditMode/Framework/CombatCommonPublicationTests.cs`

No registered service, calculator, Buffer, factory, Framework, assembly definition, public contract or View/Flow implementation was changed. Existing processing service calls and Buffer reads remain the integration contract for the View owner. Direct service calls in these tests establish Core/Buffer behavior, not production Flow/Job wiring.

## Assertion mapping

The [method matrix](2026-10-02-core-combat-retirement-test-matrix.csv) retains all 606 original rows and classification fields. It adds `8r2_disposition`, `8r2_assertion_review`, `8r2_verified_witnesses` and `8r2_remaining_scope` to 160 rows assigned to 8R2 plus the three `StageServicesBoundaryTests` rows explicitly named by this task. Each listed witness method executed successfully in the final run: 59 distinct methods across 163 reviewed rows.

This is scope-aware mapping, not a claim that every historical API assertion has an identical replacement. Rows distinguish approved gameplay, withdrawn mutable-owner APIs, surviving shared guards, deferred unit footprints and mixed assertions assigned to later tasks. Every old fixture, assertion and helper remains intact; physical removal is an 8R5 decision after the remaining consumers move.

| Behavior | Current evidence |
| --- | --- |
| Persistent main, initial `min(3, subs)`, stable owned identities | Existing preparation/roster/service tests plus `BattlePreparationRuleTests.DuplicateCardKinds_HaveIndependentRuntimeValuesAndStableOwnedIds`; duplicate keys retain separate owned IDs, souls and health values. |
| Invalid preparation before allocation | `CharacterBattlePreparationTests.InvalidPlacementOrCapacity_ChangesNoBufferOrAllocator`; `BattlePreparationRuleTests.InvalidInitialDefinitions_RejectBeforeAllocatingOrLinking` includes a distinct invalid fourth, undrawn definition while the first three remain valid. Unexpected trait configuration faults propagate. |
| Enemy/structure/hand allocation and DB facts | `BattlePreparationRuleTests.EnemiesAndHeadquarters_PrecedeHandAllocation_AndMayExceedReserveCapacity`; existing `BattleDefinitionAdapterTests` verify authored/weighted placement and headquarters-first ordering. Test definitions do not satisfy production DB acceptance. |
| Checkpoint movement, refund and costs | Existing `(1,3) -> (3,3)` AP `5 -> 3`, pending `2`, checkpoint `(1,3)` Preview test; added equal-radius/origin retarget tests and `Move_OwnerCost_UsesWholeCheckpointCostBeforeSpending`. At AP5, cost3 permits one cell and rejects two; cost2 permits two cells leaving AP1. |
| Move/attack settlement and main movement | `MovementSettlement_AttackAndTurnCommitOnlyAcceptedSegment`, `MainMovement_AttackCommitsManhattanDistanceAndChargesBaseCostOnce`, `Main_AttackFirstPreservesMovementBudget_AndFollowingMoveChargesOnlyDistance`; invalid attacks preserve pending movement, accepted attacks charge the base cost once. |
| Deployment and return | Relocation preserves identity, health and stats without payment; invalid bounds/zone/occupancy reject unchanged. Main return and post-lock placement reject. `Return_FullHandRejectsWithoutChanges_AndFreedSlotAllowsReturn` verifies current first-empty-slot semantics. |
| Draw and economy | Free/exact-balance draw, exhaustion, living-sub count, failure priority and allocator overflow; `CombatCommonPublicationTests.DrawPublication_InstallsCharacterCurrencyCursorAndResultOnce` verifies one joint install. Later CurrencyBuffer updates refresh affordability; separate groups remain independent. |
| Joint publication before first write | Existing invalid-slot/identity tests plus `CombatCommonPublicationTests.InvalidDrawTimeline_InstallsNeitherCharacterCurrencyCursorNorResult`; invalid combined proposals install neither gameplay nor success metadata. |
| Turn, night and order | Successful settlement restores spent AP; lethal traversal does not restore the dead actor. Priority30 yields interval4 independently of Delay35/200; Delay controls night time. Checked time overflow and recall failure preserve input. Forecast default10/explicit4, deterministic ties and forecast/advance order are covered. |
| Night recall/redeployment/gimmicks | Repeated real service cycles preserve main health/position/lock, structures and enemy origins without income. Original enemy cells are reserved before deterministic fallback; a full zone rejects. Preparation indices1/2 restart railway activation at turn1/count0 without mutating prior schedule facts. |
| Death and progress | Railway eliminates main/regular units with the correct terminal outcome, survivor facts and timeline removal. Existing terminal publication tests verify stage and boss-map progress together. |

The three changed fixtures contribute 56 additional executed cases relative to the pre-8R2 checkout: 52 Core and 4 Framework. The intervening Stage merge separately added 9 Framework cases relative to 8R1. No old cases were deleted.

## Ownership and review

The test helpers copy immutable command facts and use existing Buffer factories/publication interfaces. They do not introduce a mutable test StageSession, production adapter, scheduler or compatibility service. Core continues to read View intent; View/Flow continues to own combat execution and intent removal. Buffer storage validation remains distinct from pure gameplay calculations.

A separate reviewer checked the changed tests against current source and the task. Two weak assertions were corrected: the invalid-undrawn fixture now isolates the fourth definition, and railway survivor assertions inspect the resulting overlay rather than only the unchanged input. A final assertion audit also made full-hand return, cost3 movement, attack-first movement and per-night gimmick initialization explicit. The final run below includes all corrections. No production defect requiring a behavior change was demonstrated.

## Fresh Unity verification

Unity 6000.0.73f1 compiled the project and ran actual EditMode tests through the approved AppData-access execution path:

| Assembly | Passed / total | Failed |
| --- | ---: | ---: |
| ProjectL.Tests.Core | 1334 / 1334 | 0 |
| ProjectL.Tests.Framework | 315 / 320 | 5 |
| ProjectL.Tests.CombatViewBoundary | 13 / 13 | 0 |
| Total | **1662 / 1667** | **5** |

Skipped and inconclusive: zero. Unity records exit code **2**, not a green suite. The five failed tests and their failure messages are unchanged from the 8R1 baseline; each reports `System.InvalidOperationException : [CombatStageDetail] Combat stage requires Economy.`:

- `ArtifactDataTableTests.Catalog_RegistersOnlyMigratedRulesAndExplicitlyPendingViews`
- `DataBaseAssetTests.Catalog_CreatesAllDataBaseGroups`
- `DataBaseAssetTests.CombatStages_ReferenceExistingInGameData`
- `DataBaseAssetTests.EveryStage_HasNodeVisual`
- `DataBaseAssetTests.EveryUnitAndStructure_HasUnitVisual`

```powershell
& 'D:/Program Files/Unity/Hub/Editor/6000.0.73f1/Editor/Unity.exe' -batchmode -nographics -projectPath 'D:/My_ProjectWork/Github_desktop/Project_L' -runTests -testPlatform EditMode -assemblyNames 'ProjectL.Tests.Core;ProjectL.Tests.Framework;ProjectL.Tests.CombatViewBoundary' -testResults 'D:/My_ProjectWork/Github_desktop/Project_L/.superpowers/sdd/2026-10-02-core-combat-legacy-retirement-plan/8r2-final.xml' -logFile 'D:/My_ProjectWork/Github_desktop/Project_L/.superpowers/sdd/2026-10-02-core-combat-legacy-retirement-plan/8r2-final.log'
```

Final result XML and the Unity log are local execution evidence under the ignored plan workspace. Every named matrix witness was checked against the final XML for a Passed result. Pre-execution SHA-256 comparisons of tracked script/test/document files showed only the three intentional test edits before report updates; production and View/Flow sources were unchanged. Importer-managed catalog/settings bytes were restored to the captured 8R2 incoming bytes, preserving pre-existing changes; the incoming FMOD cache diff was retained.

No independent managed harness, fabricated RED replay, PlayMode or actual gameplay run is claimed. The test-only migration preserves existing behavior; no production fix required a new RED cycle.

## Remaining work

- **8R3:** Attack/AI/traits/categories and forced-displacement assertion migration. Mixed main/movement fixtures remain until these assertions are classified.
- **8R4:** Artifact, souls, settlement and lifetime assertion migration.
- **8R5:** Remove obsolete owners/wrappers/test helpers only after consumer closure. Preserve or split surviving shared constructor/contract tests, rather than deleting them with a legacy fixture.
- **8R6:** Final ownership/source audit and handoff status. Task8R2 does not close master Task8.
- **Explicit deferrals:** Multicell-unit gameplay, user-selected movement routes and unconfirmed profiles remain unchanged. Structure footprint support remains independently tested.
- **Production dependencies:** Actual targeting DB schema/query/mapping, authored Artifact acceptance, agreed startup timing, View-owned Flow/Job wiring and joint production verification remain open. The existing missing-DB preparation guard is preserved.

The next plan task is 8R3. No commit, push or next-task implementation was performed as part of 8R2.
