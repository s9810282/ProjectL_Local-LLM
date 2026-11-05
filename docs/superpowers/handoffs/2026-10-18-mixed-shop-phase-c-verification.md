# Mixed Shop — Phase C Verification

Date: 2026-10-18  
Scope: C1-C2 only, authorized by the user. Stop before Phase D.  
Plan: Mixed shop implementation plan (Git history: `2026-10-18-mixed-shop-single-purchase-plan.md`)\
Design: Mixed shop design (Git history: `2026-10-18-mixed-shop-single-purchase-design.md`)

## Delivered behavior

- `IShopService` replaces the character-only service name. Its script GUID is preserved. Entry, purchase, and completion retain the Phase B visit context contract.
- `ShopStageDetail` owns copied unit/item key lists, shared recruit price, and category weights. SO conversion exports keys without evaluating item effects. Existing `candidates[].unitDefinition` references remain intact; per-candidate prices and authored recruit levels are removed.
- `ShopDefinitionAdapter` validates the full candidate definition, reuses existing character eligibility and item effect validation, and passes immutable candidates to `ShopOfferPlanner`. Owned units are excluded; typed products never repeat; shortages produce 0-3 offers.
- `ShopService` captures owned facts before DB callbacks, then uses the Phase B guarded entry and purchase sinks. It binds only the required read/write sinks. Unit offers retain their entry-time price and floored mean Sub level. Item purchases use the existing inventory calculator and the atomic shop transaction to grant exactly one, including when stock already exists.
- `ShopCommandExecutor` owns one synchronous guard across purchase, first receipt, completion, and final receipt. A committed purchase receipt is visible before completion begins. Completion rejection or exception leaves payment/acquisition/purchase lock intact; a completion-only retry never purchases again. Exceptions propagate.
- The executor and `ShopFlow` wait while the oldest queued command has an unacknowledged receipt. A later command cannot overwrite that result. After acknowledgment, a second purchase from a completed visit is rejected. Same-request final publication checks context, request, offer, and purchase-result identity.
- Existing factory assembly constructs the executor, and the shared command service delegates only its shop path. Public scheduling signatures, map/recovery execution, and assignment-only result stores remain unchanged.

## Content and View boundary

`Shop_Default.asset` still references ShotGun and Sniper, now with shared 600G pricing and item/unit weights 2/1. Its item references intentionally remain empty until D1. The production starting roster can exhaust this recruit pool, leaving a valid empty shop at this intermediate phase.

Four-item prices 100/150/200/200 and mixed purchasing were verified with test-only definitions. Existing general shop regressions use three distinct unowned fixture units at 5G to preserve their unrelated assertions. Ballista/CactusCannon eligibility in that test database is fixture-only: no production unit eligibility, recruit pool, art, or visual catalog was expanded.

No Phase D runtime UI work was performed. Existing UI controls become disabled when Core clears the node, but immediate popup closure, mixed item rendering, restored purchased-state handling, and completion-only retry presentation remain D2 work. This is not final playable shop acceptance.

The C-specific `OwnedLevelShopDataTests.ProductionShop_PreservesRecruitReferencesAndDefersItemActivation` expectation must be updated when D1 activates the item references. Timer/display-only tests still describe the intermediate View implementation; D2 owns their final replacement.

## Verification

All evidence is under ignored `Library/CodexShopValidation`. Unity 6000.0.73f1 was run through the AppData-authorized batch path. Complete XML results and process exit were checked; no cache deletion or Unity upgrade was used.

### Scoped final gate

`c-gate-final.xml` / `.log`: Unity exit **0**, **499 passed, 0 failed, 0 skipped**.

| Actual XML fixture | Passed |
|---|---:|
| CharacterRosterServiceTests | 14 |
| CombatItemInventoryTests | 12 |
| MixedShopServiceTests | 19 |
| OwnedLevelShopDataTests | 11 |
| RecoveryCommandProcessingTests | 37 |
| RecoveryNodeCommandTests | 20 |
| RunGoldLifecycleTests | 2 |
| RunTerminationAdmissionTests | 50 |
| ShopCommandExecutorTests | 15 |
| ShopEntryPublicationTests | 26 |
| ShopPopupModelBuilderTests | 7 |
| ShopPurchaseTransactionTests | 17 |
| ShopStageServiceTests | 205 |
| ShopUITests | 25 |
| ShopVisitIdentityTests | 9 |
| StageFlowTests | 30 |

`ShopStageServiceTests` includes the partial fixtures implemented in `StageShopCommandExecutionTests.cs`, `StageCommandProcessingTests.cs`, `ShopStageOperationsTests.cs`, and `ShopPopupControllerTests.cs`; file names were not treated as separate fixture filters.

The new tests cover owned-unit filtering, short/empty pools, item prices, frozen recruit price/level, definition rejection, callback mutations, owned-item purchases, actual inventory/payment/completion, exception/rejection recovery, receipt visibility and identity, reentrancy, stale request numbers, cleared input, acknowledgment waiting, and newer-result protection.

### Red and migration evidence

- `c1-red.xml`: 1/1 expected behavioral failure. An all-owned pool incorrectly produced three old-style offers before C1 implementation.
- `c2-behavior-red.xml`: the extracted old executor passed 4 and failed 11 of its 15 sequencing tests; C1 mixed-service 15/15 and data 10/10 passed in the same run. The red failures preceded automatic completion/receipt sequencing implementation.
- The data fixture did not have a separate pre-implementation behavioral red. Compile failures during type/schema migration are not counted as behavioral red.
- Intermediate failures exposed obsolete duplicate-unit/continued-purchase UI assumptions, one repeatedly invoked mutation callback, and a battle fixture missing the newly recruited unit definition. Only the affected test setup/expectations were updated. No unrelated battle implementation was changed.

### Separate asset regression

`c-assets-final.xml` / `.log`: Unity exit **1**, **37 passed, 3 failed, 0 skipped** out of 40. The same three failures appeared in the earlier `c1-regression.xml` asset fixture and remain outside the shop migration:

1. `DataBaseAssetTests.EveryCombatStage_EnvironmentAndViewRotationsReachOperationalDefinition`: Stage 1101 expects 12 entries, receives 13 (extra 213).
2. `DataBaseAssetTests.EveryCombatStage_UsesRequestedNineByNineCenterRail_AndBoundTrainPrefab`: Stage 2101 expects 8, receives 0.
3. `DataBaseAssetTests.LineMaterial_IsRegisteredInCatalog_AndSupportsColorOverride`: `Assets/ProjectL/Script/Render/Shader/Line/LineShader.mat` is absent.

These are recorded as unrelated observed failures, not proven pre-existing baseline failures: no before-C execution baseline of this full asset fixture was captured. Their combat/line asset targets were not changed by Phase C. The shop-specific real-asset reference/price/weight test passed in the scoped gate. Do not infer a green full asset or repository suite from the scoped results. Full Core/Framework suites and manual Play Mode acceptance were not run in Phase C; those remain Phase E work.

## Locality and contracts review

- SSOT: prices originate in shop/item definitions and are frozen in offers; purchase permission derives from the stored visit-wide mark. No View-owned purchase flag, separate acknowledgment state, or duplicate item inventory was introduced.
- Responsibility boundaries: adapter converts/validates definitions, pure rules calculate, Buffer transactions publish, service assembles facts, executor sequences purchase/completion, and factory performs composition only.
- Existing character eligibility, item effect validation, inventory arithmetic, and chapter completion contracts are reused. No Domain assembly reference expansion or universal transaction framework was added.
- Recovery policy and transaction internals remain untouched. The shared dispatcher, queue acknowledgment, service-name consumers, and DB constructor fixtures are the necessary integration changes; the selected recovery command regressions pass.
- Inactive legacy `CharacterShopRules` and unit-only candidate constructors remain because genuine legacy pure-Core consumers still compile against them. No active shop path calls those rules.
- Self-review covered 12 production C# files, 20 test/support files, and the one migrated asset. `phase-c-sources.json` records their hashes; metadata, preserved service GUID, UTF-8/LF, pure-data boundary, and scoped `git diff --check` passed. No external/Astra review was requested or run.

## Stop point

C1-C2 are implemented. D1-D2 and E are not started. No staging, commit, push, external delivery, cache removal, or production candidate expansion was performed. Existing Phase A/B and concurrent unrelated working-tree changes are preserved.
