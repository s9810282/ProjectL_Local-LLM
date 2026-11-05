# Mixed Shop — Phase B Verification

Date: 2026-10-18

Status: B1-B2 implemented and verified; stopped before Phase C.

Plan: Mixed Shop and Single-Purchase Implementation Plan (Git history: `2026-10-18-mixed-shop-single-purchase-plan.md`)

## Delivered behavior

- `ShopTransactions` now owns shop entry and purchase publication. Recovery methods remain in `StageServiceTransactions`; their existing open-target predicate moved to `BufferGuards` with explicit readers.
- `ShopPurchasePreparation` freezes the expected visit, exact shop object, balance, copied owned records, main state, selected offer, and approved decision. Publication rejects changed source facts before installing anything.
- Unit and item purchases prepare their result and all store changes first, then install acquisition, currency, and the purchase mark synchronously. Item purchases add exactly one of the selected item and preserve unrelated stock. They do not invoke the item service or equip items after payment.
- `CharacterBuffer.PrepareAcquire` reserves list/dictionary capacity before installation. This small shared-store change supports the planned allocation boundary; it adds no global duplicate-unit rule.
- `CurrentShopData` accepts 0..3 offers, defensively copies them, permits at most one purchased bit, and derives `HasPurchase` from that bit. A second purchase is rejected across the entire visit.
- Buffer preparation creates a new `StageEntryId` for each shop visit. Purchase copies preserve it. Assortment seeding is unaffected. A reused node or new Run cannot accept an earlier visit's intent.
- Entry preparation also copies expected owned records, detecting roster replacement during DB callbacks.
- Service, input, result, popup model, UI submission, and popup result matching now carry the same `ShopVisitContext`. Empty popup contexts cannot submit. Request numbers remain monotonic after cleanup/new Run.

Seventeen production source files and twenty-seven test/support source files belong to Phase B. Two production files and three test/support files are new, each with metadata. No production asset, scene, prefab, item effect, or unrelated combat algorithm was edited for this Phase. Concurrent combat work and existing FMOD/TMP modifications were preserved.

## Active contracts

```csharp
IShopPurchaseBufferWriteSink.PublishPurchase(ShopPurchasePreparation preparation)
    : ShopPurchaseResult
IBufferQuery.ShopPurchaseWriteSink

ICharacterShopService.EnterShop(int chapterKey, StageNodeId targetNodeId, int seed)
    : ChapterProgressionResult
ICharacterShopService.Purchase(ShopVisitContext context, ShopOfferId offerId)
    : ShopPurchaseResult
ICharacterShopService.CompleteShop(ShopVisitContext context)
    : ChapterProgressionResult

ShopViewCommandData.Purchase(ShopVisitContext context, ShopOfferId offerId)
ShopViewCommandData.Complete(ShopVisitContext context)
ShopPopupModel.Context
BufferSystems.Current.ShopCommandResult.Context
```

The context-free service `Purchase`/`PrepareShop` methods and `ICharacterPurchaseBufferWriteSink` were removed. B1's optional temporary unit sink bridge was unnecessary because B1/B2 consumers migrated together before the Phase gate. Initialization/test-only `SetShopOffers` and the internal current-store preparation seam remain; there is no active gameplay caller using them as a substitute for the context-aware service.

`ICharacterShopService` and the unit-only `CharacterShopRules` adapter remain intentionally until C1. The service still uses its existing unit assortment and authored levels. Phase A's owned-unit filtering, mixed DB assortment, and average-level policy are not activated by this Phase. The new item transaction is verified directly with approved decisions; production item purchases require C1. Automatic completion, receipt sequencing, and final popup lock/closure behavior remain C2/D2.

## Executed verification

Unity 6000.0.73f1 ran through the approved AppData-access path, with `-batchmode -nographics -runTests -testPlatform EditMode -assemblyNames ProjectL.Tests.Framework` and explicit fixture filters. Both final runs exited 0 and reported zero failures/skips.

Evidence directory: `Library/CodexShopValidation/` (ignored, retained locally; not committed).

| Final evidence | Passed / total | Scope |
|---|---:|---|
| `b-required-final.xml` / `.log` | 535 / 535 | Phase B gate, shop/roster/input and targeted recovery/item regressions |
| `b-transport-retry.xml` / `.log` | 138 / 138 | Shared callers whose input/context constructors or query forwarding changed |
| Total, distinct selected fixtures | 673 / 673 | Scoped verification, not full Framework coverage |

Required-run fixture identities and counts:

| Fixture in `ProjectL.Tests.Framework` | Passed |
|---|---:|
| ShopPurchaseTransactionTests | 17 |
| ShopVisitIdentityTests | 9 |
| ShopStageServiceTests | 205 |
| ShopEntryPublicationTests | 26 |
| ShopUITests | 25 |
| ShopPopupModelBuilderTests | 7 |
| CharacterRosterServiceTests | 14 |
| CombatItemInventoryTests | 12 |
| RecoveryEntryPublicationTests | 28 |
| RecoveryStageServiceTests | 80 |
| RecoveryTypedOperationTests | 48 |
| RunGoldLifecycleTests | 2 |
| RunTerminationAdmissionTests | 50 |
| StageSelectInputBufferTests | 12 |

`ShopPopupControllerTests.cs`, `ShopStageOperationsTests.cs`, and the shop command test files contribute to the partial **ShopStageServiceTests** fixture. Their filenames are not independent NUnit fixture filters.

Additional transport-run fixtures: ArtifactEditingServiceTests 53, InGameViewFaultCleanupTests 14, JobSystemTests 11, RecoveryRequestLifetimeIntegrationTests 20, RecoveryStageMapViewTests 11, and StageFlowTests 29.

Thirty-two new cases cover transaction/identity behavior and related service/UI boundaries: 17 transaction, 9 identity, 4 service, and 2 UI cases. Checks include both purchase branches, second-slot rejection, frozen level/price, unrelated stock preservation, stale balance/roster/main/stock/visit, invalid deltas, identity overflow, same-node/new-Run replacement, copied offers, empty identity, correlated result transport, and reused popup models. Existing combat inventory tests separately cover equipped-item transfer and lifecycle behavior.

### Test migration and execution history

- `b1-red.xml`: 0/2 passed before implementation. The old store allowed a second slot and rejected an empty assortment. Both behavioral failures are green in the final runs.
- The coordinated B2 API migration was first behaviorally exercised after implementation. A separate pre-implementation behavioral RED was not captured for B2. Compile errors during consumer migration and fixture setup failures are not represented as behavioral RED evidence.
- Obsolete multi-buy assertions now expect one purchase. The exception-after-an-earlier-receipt test uses a distinct visit so it still reaches the injected lookup failure without bypassing the lock.
- Legacy popup status-timer tests inject correlated immutable receipts to test timer lifetime independently of repeated payment. Actual purchase/lock tests still traverse the real service and stores. These timer paths remain subject to D2's planned lifecycle replacement.
- A recovery entry preservation test now seeds historical shop facts directly instead of calling the removed preparation API on an already cleared node. Its recovery algorithm and assertions remain unchanged.
- The roster identity-overflow fixture lacked the currently required Fame buffer; its local composition was repaired. Test helpers were corrected to avoid duplicate map/stage registration. These are test setup changes, not new game policies.

## Verification limits and retained failures

An expanded run (`b-framework-core.xml` / `.log`) reported seven `InvalidMainCharacter` failures before reaching shop entry:

- TemporaryCoreChapterContinuationTests: `DefeatInSecondChapter_TerminatesWithoutClearingChapter(false/true)`, `NewScenario_DoesNotMutateAnotherRuntime(false/true)`, and `TwoChapters_KeepOwnedFacts_ThenFinishRun`.
- TemporaryCoreShopRuntimeTests: `BattleShopNextBattle_PreservesOwnedFacts(0/1)`.

Their existing `WinBattle` helpers submit `EnterCombat` immediately; the failing stack reaches that command before the shop assertions. Their shop signatures were migrated, but their combat setup was not rewritten in Phase B. No clean pre-B execution baseline for those seven cases was captured, so this report does not claim an execution-proven pre-existing baseline or passing end-to-end coverage. Phase E must resolve and rerun these smoke tests.

The first additional transport launch (`b-transport-final.log`) stopped at Unity's `HandleProjectAlreadyOpenInAnotherInstance`, without complete test XML. After confirming no Unity process remained, the retry passed 138/138. No editor instance was terminated and no cache/version workaround was applied.

No complete Core/Framework suite or manual playable-shop acceptance was run for Phase B. No final mixed item UI, production assortment, completion exception sequence, or automatic popup closure is claimed.

## Review and handoff

- Scoped source review confirms that shop transactions have no recovery input/result dependency. Factory/group changes only construct and expose the new collaborator.
- Buffer remains authoritative. The popup transports identity and reads facts; it does not calculate payment, mutate inventory, or create a second visit counter.
- Result construction, offer copying, inventory preparation, and roster capacity reservation precede installation. The currency store rechecks an already validated nonnegative value; it performs no external lookup or callback.
- UTF-8/LF, replacement-character, metadata, and trailing-whitespace checks passed for all 44 Phase B source/test files. Scoped `git diff --check` passed. `phase-b-sources.json` records their verified hashes.
- Review was a local author review. No external/Astra review, staging, commit, push, or external delivery occurred.

Phase C prerequisites are now available: the typed paid-purchase sink, stored visit identity, context-safe active APIs, and global purchase lock. C1 must replace the remaining unit-only planning adapter; C2 must implement automatic completion and receipt/backpressure ordering. **Phase C has not started.**
