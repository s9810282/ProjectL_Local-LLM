# Mixed Shop — Phase A Verification

Date: 2026-10-18  
Status: A1-A2 complete; stopped before Phase B.  
Plan: Mixed Shop and Single-Purchase Implementation Plan (Git history: `2026-10-18-mixed-shop-single-purchase-plan.md`)

## Delivered scope

- Added six pure contracts under `CoreCaculator/Contracts/Shop`: product kind, shared offer capacity, visit identity, copied purchase input, typed purchase decision, and typed purchase result.
- Extended candidate/offer factories for units and consumables. Item keys do not masquerade as EntityKey values; successful results have one acquisition branch. Existing unit constructors retain their costs and explicit levels as compatibility bridges for later migration.
- Appended failure reasons without renumbering existing enum members; made legacy capacity a constant alias.
- Added `ShopOfferPlanner`: owned-unit exclusion, duplicate configuration rejection, category weights followed by uniform selection without replacement, canonical seeded ordering, 0..3 offers, and floor(mean Sub level) with a wide accumulator.
- Added `ShopPurchaseRules`: visit-wide purchase rejection, already-owned unit rejection, frozen price/level use, and quantity-one item grant calculations through the existing ItemInventoryCalculator. Inputs and runtime stores are not mutated.
- Added 38 tests in ShopProductContractTests (8), ShopOfferPlannerTests (15), and ShopPurchaseRuleTests (15), with source/folder metadata.

Eleven new C# files and four existing contract files belong to this change. No runtime service/Buffer transaction composition, SO data, View/UI, scene, or shop activation was changed. Existing production shopping still uses its old calculation path until the later authorized integration Phases.

## Red/Green evidence

The actual repository Core sources were compiled with the locally installed NUnit framework in an ignored .NET scratch harness while concurrent reward work temporarily blocked Unity compilation. No external package download was used. These runs are managed evidence, separately identified from Unity evidence.

| Run | Passed | Failed | Explanation |
|---|---:|---:|---|
| A1 managed red | 66 | 8 | New factory/context/input/result tests failed against unimplemented API shells. |
| A1 managed green | 74 | 0 | Contract behavior implemented; legacy roster checks retained. |
| A2 managed red | 74 | 28 | New planner/purchase tests failed against unimplemented rule shells. |
| A2 managed green | 102 | 0 | Planner/purchase behavior implemented. |

Two explicit additional boundary tests (99G Whiskey and missing unit definition) were then added and executed in the final Unity run below. No NotImplementedException shell remains in product source.

## Final Unity verification

Unity: `D:/Program Files/Unity/Hub/Editor/6000.0.73f1/Editor/Unity.exe`, approved AppData-access execution, batch mode, no graphics. Runs were serialized with the other active Unity validation.

| Run | Passed / total | Failed | Skipped | Exit |
|---|---|---:|---:|---:|
| Complete ProjectL.Tests.Core | 3323/3323 | 0 | 0 | 0 |
| Focused ProjectL.Tests.Framework | 243/243 | 0 | 0 | 0 |

Core includes all 38 new shop cases and existing CharacterRosterContractTests 45/45 plus CharacterRosterRuleTests 21/21. Focused Framework filter: `OwnedLevelShopDataTests;ShopStageServiceTests;ShopEntryPublicationTests;ShopPopupModelBuilderTests`. Its fixtures passed 6/6, 204/204, 26/26, and 7/7 respectively. The focused run also recompiled the LF-normalized sources and the two additional tests.

Evidence directory: `Library/CodexShopValidation/` (ignored local artifacts):

- `phase-a-core-final.xml` / `.log` — final complete Core run.
- `phase-a-framework-focused.xml` / `.log` — complete focused Framework run.
- `a1-red-managed.xml`, `a1-green-managed.xml`, `a2-red-managed.xml`, `a2-green-managed.xml` and matching logs.
- `source-hashes.txt` — reviewed Phase A source hashes.
- `phase-a-progress.md` — execution rulings and evidence ledger.

## Exceptions and verification limits

1. The initial Unity baseline could not finish while another combat-reward implementation changed its sources/tests: first a missing StageOutcome import, then six Settle-call signature compilation errors. Those files were not edited by this shop task. Later complete Unity runs compiled successfully; no clean pre-change full baseline is claimed.
2. A broader Framework `Shop` filter encountered a native `GfxDevice::DrawSharedGeometryJobs` crash (exit -1073741819), with no complete result XML. Its log is `phase-a-shop-framework-unity.log`. It is **not** counted as passing coverage. The focused non-rendering fixtures above subsequently completed successfully.
3. This is not a claim of full Framework, graphical/manual acceptance, production mixed-item UI, or end-to-end one-buy-and-close behavior. Those later integration responsibilities remain in B-E.
4. The initial Temp evidence directory was cleared during concurrent Unity execution; durable evidence was moved to the ignored Library directory. The incomplete initial baseline is documented from observed command output, not linked to a nonexistent XML.

## Scope and architecture review

Source review confirms no UnityEngine/BufferSystems dependency in the new Core code, defensive collection copies, immutable output offers, a single capacity source, and no runtime ownership/inventory copy. Sampling and purchasing reuse existing acquisition/grant rules instead of implementing combat item behavior. Targeted whitespace/encoding checks passed and all new source files have .meta files. The four existing source GUIDs were preserved.

The review was a local author review plus executed tests; no external or separate-model review was initiated. Concurrent combat-reward changes, reward documents, FMOD catalog changes, and TMP asset changes remain outside this task.

## Next boundary

Phase B is not started. Its transaction, visit transport, and production single-purchase locking changes still require explicit authorization. No stage/commit/push was performed.
