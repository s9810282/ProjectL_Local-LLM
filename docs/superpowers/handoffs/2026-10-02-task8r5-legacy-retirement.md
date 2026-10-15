# Task 8R5 — Legacy combat retirement

Date: 2026-10-02. Scope: the authorized 8R5 continuation in the existing Core feature checkout. This report records physical retirement after 8R2–8R4 behavioral mapping. It does not complete Task 8, production integration, or 8R6.

## Source closure

The original [335-row source manifest](2026-10-02-core-combat-retirement-source-manifest.csv) is preserved as historical inventory, with `8r5_disposition` and `8r5_member_closure` recording the current outcome for every row.

- 90 candidate source files and their matching `.meta` files were removed as a coherent old-host dependency group: StageSession/StageServices and their factory/facade/services, mutable unit/structure/map/reserve/phase/economy/timeline/gimmick/deck owners, object-bound prepared plans and effect owners.
- Two original candidate files are actually mixed. `UnitMovementService.cs` retains only the existing public immutable `MovementExecutionResult`; `UnitAttackService.cs` retains only `AttackExecutionResult`. Their service classes are removed. Existing public result scalar/null/copy contracts and their tests remain; no compatibility service or new API was introduced. File paths and GUIDs are preserved.
- Fifteen original mixed files lose their owner-based members while retaining current fact-based calculation, AI selection/Calculate, trait status conversion, category snapshot conversion, and timeline interval/tie math. `SoulGrowthState.cs` remains immutable growth values/math. `SoulFieldState` now contains only static fact-based countdown; `StageSoulService` contains only static death-to-field calculation.
- All 221 shared dependencies and six separately owned Artifact feature files remain. Standalone Artifact editing/layout/pool/resource behavior is outside combat-host retirement.

Current registered processing services, DB adapters, Buffer classes, Framework classes, production View/Flow code, assembly definitions, and DB authoring are unchanged from the incoming 8R5 snapshot. An incidental unused DB import edit was restored to bytes matching the incoming SHA-256, rather than replacing it from HEAD. Unity importer/cache/settings changes are restored to the incoming backups.

## Test closure

The original [606-row method matrix](2026-10-02-core-combat-retirement-test-matrix.csv) retains its earlier classifications and 8R2–8R4 reviews. Every row now has a scoped 8R5 disposition. Current witness names are resolved against successfully executed methods using NUnit `classname`/`methodname`, including custom test-case names.

The [retired-case ledger](2026-10-02-task8r5-retired-test-cases.csv) maps all 527 removed cases from the actual 8R4 run to their original method IDs and dispositions. Eight new cases split retained geometry/current-contract assertions from old owners. This count change is retirement evidence, not increased production acceptance.

Important seams:

- M011 keeps the same single-cell square-distance boundary `(1,1) -> (4,3)`: excluded at range 2 and included at range 3, through the existing pure range API. The old entity-pair overload retires.
- M273 keeps the **one occupied cell** of a padded width-2 footprint: anchor `(-1,4)` produces occupied cell `(0,4)`, inside the board and enemy deployment zone. It does not activate padded unit profiles or certify current redeployment candidate selection. Actual multicell unit integration stays deferred.
- M204 uses the current `BattleCharacterCalculator` to prove a trait-free definition creates facts without trait statuses; the obsolete UnitFactory/UnitState seam retires.
- M217–M219 retain public pierce Preview copying, individually rounded records, empty defaults, invalid/duplicate rays, aggregate mismatch, index/value guards and overflow. AttackTargetPlan/PierceRayHitPlan owner-only assertions retire.
- M249 directly exercises the surviving pure provider's rejection of a registered state without a status mapping. Independent review identified this missing assertion; its private test state/factory scaffolding was restored without the old resolver or UnitState.
- Knockback, ThornField and PiercingFalloff registry/state/codec/status assertions use existing registered state factories and pure effect bindings. No visibility expansion or replacement runtime owner is needed.
- The CombatViewBoundary assembly keeps its original references. Old StageSession-based execution fixtures are replaced with current public result/display-contract assertions; actual battle execution behavior is covered by current Core and Framework witnesses. This proves public-contract accessibility and immutable output behavior, not View-owned Job wiring.
- Legacy-only StageSessionTestFactory, LegacyArtifactStageInput, CellAimScenario, KnockbackTestScenario and StageSessionViewTestFactory are removed after their callers. UnitStateTestFactory retains only pure UnitDefinition construction and its category registry; it constructs no UnitState.

## Verification

Fresh actual Unity 6000.0.73f1 EditMode run, with approved AppData access:

| Assembly | Passed | Total | Failed |
| --- | ---: | ---: | ---: |
| ProjectL.Tests.Core | 860 | 860 | 0 |
| ProjectL.Tests.Framework | 319 | 324 | 5 |
| ProjectL.Tests.CombatViewBoundary | 6 | 6 | 0 |
| Total | 1185 | 1190 | 5 |

No skipped or inconclusive cases. Unity records exit code 2; the launch shell's 0 is not the test result. The exact five failure names and full messages match `8r4-complete.xml`; all report `System.InvalidOperationException : [CombatStageDetail] Combat stage requires Economy.` These remain authored-data failures, not a green full suite:

- `ArtifactDataTableTests.Catalog_RegistersOnlyMigratedRulesAndExplicitlyPendingViews`
- `DataBaseAssetTests.Catalog_CreatesAllDataBaseGroups`
- `DataBaseAssetTests.CombatStages_ReferenceExistingInGameData`
- `DataBaseAssetTests.EveryStage_HasNodeVisual`
- `DataBaseAssetTests.EveryUnitAndStructure_HasUnitVisual`

```powershell
& 'D:/Program Files/Unity/Hub/Editor/6000.0.73f1/Editor/Unity.exe' -batchmode -nographics -projectPath 'D:/My_ProjectWork/Github_desktop/Project_L' -runTests -testPlatform EditMode -assemblyNames 'ProjectL.Tests.Core;ProjectL.Tests.Framework;ProjectL.Tests.CombatViewBoundary' -testResults 'D:/My_ProjectWork/Github_desktop/Project_L/.superpowers/sdd/2026-10-02-core-combat-legacy-retirement-plan/8r5-complete.xml' -logFile 'D:/My_ProjectWork/Github_desktop/Project_L/.superpowers/sdd/2026-10-02-core-combat-legacy-retirement-plan/8r5-complete.log'
```

Static source/caller scans find no remaining UnitState, CombatEntityState, StageMapState, StageServices, UnitFactory or old AI gateway/helper consumers. Fresh serialized-GUID scans cover Assets, Packages and ProjectSettings. Removed paths were checked inside the checkout and removed individually; no recursive folder purge was used. Compile and EditMode are actual Unity evidence; no managed harness, RED replay, PlayMode or live game claim substitutes for them.

## Independent review

One fresh read-only reviewer checked the incoming hashes, surviving member bodies, all method dispositions/witnesses, the case ledger and removed GUID/caller searches. It found no Critical issue and one Important coverage defect: M249's still-live pure status-mapping rejection. This was corrected in one test-only pass, and the final full Unity run above includes the restored test passing. No production behavior changed, so no fabricated RED replay was used. There was no second review or deferred minor finding. The reviewer explicitly declined production DB, View/F1/F2/live-play, deferred profiles, baseline data repair and unauthorized 8R6 acceptance.

## Boundaries and remaining work

No replacement mutable combat host, scheduler, dispatcher, runner, combat Plan/Job path or whole-Buffer restore adapter was added. Canonical runtime state and joint publication remain in existing Buffers and registered Systems services; View/Flow still owns lifecycle, combat Jobs and execution wiring. Current pure AI command composition is retained calculation, not an independently executing service.

8R6 remains the next authorized-plan task: final closure/handoff and remaining acceptance gates. Production Method_Key DB authoring/query integration, actual DB-loaded Artifact acceptance, View/Flow wiring, Framework F1/F2 acceptance and live play remain external/dependent work. Multicell units, custom movement paths, ThornHeart ordering, Rider profile, temporary-max-HP authoring and empty-All planning remain explicit deferrals. No commit or push is part of this task.
