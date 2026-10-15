# Shop Stage Core Connection — Consumer Handoff

Date: 2026-10-03  
Base: `e8977adc` plus existing uncommitted P1/P2 and temporary-runtime work.  
Status: P1–P3 implemented, verified and independently reviewed; uncommitted. No commit or push.

## Consumer sequence

The complete Core contract is `EnterShop` → optional `Purchase(chapterKey, nodeId, offerId)` → `CompleteShop`. Entry prepares selection and three frozen offers together. Purchase commits currency, one new owned Sub identity and its consumed slot. Completion clears only the selected shop node, including after zero purchases or an unaffordable purchase attempt. Buying the third offer never auto-completes.

Use the same registered runtime/services throughout the cycle. First settle the previous battle through normal combat commands and present/acknowledge its results under the existing consumer contract. A retained terminal battle still blocks shop entry. The caller must perform legitimate `Combat.EndBattle()` cleanup; Shop never calls EndBattle, resets state or repeats settlement.

The following sequence starts after legitimate cleanup, on an already initialized runtime. Check every result before moving to the next operation:

```csharp
var core = runtime.GetService(SystemGroupsService.InGame).CoreGroup;
var shopNode = new StageNodeId(5); // Development shop scenario only.
var entered = core.Shop.EnterShop(1, shopNode, seed);
if (!entered.Accepted) return;

// Optional purchases, using offer IDs from the prepared current-shop read model.
var purchased = core.Shop.Purchase(1, shopNode, selectedOfferId);
// Handle purchased.FailureReason when !purchased.Succeeded.
// The consumer may request exit without purchasing any offer.
var completed = core.Shop.CompleteShop(1, shopNode);
if (!completed.Accepted) return;

// The View/Flow owner clears previous-battle input at its legitimate boundary.
// Preserve the existing command/result/acknowledgement lifecycle contracts.
var next = core.Combat.EnterBattle(1, new StageNodeId(4));
```

Required namespaces for the service lookup and IDs are `ProjectL.Framework.Composition`, `ProjectL.Systems.ServiceKeys` and `ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids`. The sample's node/chapter values apply only to the explicit development shop graph; production callers pass their authored/current intent. Offer IDs come from prepared offers; this handoff adds no View read model or production Job/Flow change. Consumers should keep the requested node identity with their input rather than substitute whichever node is current when handling stale input.

`EndBattle` clears active characters, the active battle, soul field and latest Core preview, while retaining the completion, latest result and sequence/handled/answered counters. Shop operations retain those facts plus View input/preview/acknowledgement state. Next battle installation clears the old completion/Core preview; it does not implicitly clear pending View inputs. The View/Flow owner performs the existing `CombatViewWriteSink.Clear()` boundary operation in its own wiring. Its command numbering and presented-result acknowledgement remain retained. In Core integration tests this is explicit fixture-driven consumer input cleanup, not a production Flow implementation.

## Failure and retry contract

New consumers must use identity-aware `Purchase(chapterKey, nodeId, offerId)`. Compatibility `PrepareShop(int)` and `Purchase(ShopOfferId)` retain their behavior and intentionally lack the new stale-node request guarantee.

| Operation / condition | Existing result |
| --- | --- |
| Entry navigation/progress mismatch | Existing `ChapterProgressionFailureReason` rejection |
| Purchase chapter/node/captured key mismatch | `EntryMismatch` |
| Purchase matching cleared node/map or replaced shop snapshot | `StaleEntry` |
| Purchase missing prepared selection, retained battle or incompatible metadata | `WrongPhase` |
| Offer missing, sold or unaffordable | `UnknownOffer`, `SoldOut`, `InsufficientFunds` |
| Invalid purchase definition / identity exhaustion | Existing `InvalidCharacter`, `IdentityOverflow` rules; invalid DB/arguments retain exception contracts |
| Completion identity/progress mismatch | `ChapterMismatch`, `StageNotFound`, `StageMismatch`, `ChapterCompleted`, `StageAlreadyCleared` |
| Entry/completion incompatible phase/type or invalid definitions | Existing exception contract, no own publication/clear write |

Purchase and completion are separate committed operations. If purchase succeeds and completion fails before its clear write, retain the purchased owned identity, paid currency and consumed mask; the node remains uncleared. Correct the cause and retry **only `CompleteShop(chapterKey, nodeId)`**. Do not restart entry, reset offers, repurchase, refund or roll back. A consumed purchase is still `SoldOut`; duplicate completion is `StageAlreadyCleared`. P2 already verifies this query-fault/retry case, so P3 reuses that coverage rather than introducing production fault hooks.

## Explicit disposable scenarios

Both entry points compile only under `UNITY_EDITOR || DEVELOPMENT_BUILD`, initialize their own real Buffers/services, enter chapter 1 and return an initialized `FrameworkRuntime`. Do not initialize again. Use `runtime.Shutdown()` on disposal. The normal production factory remains unchanged and never falls back to these definitions.

| Entry point | Fixed development path |
| --- | --- |
| `TemporaryCoreRuntimeFactory.Create()` | Start 1 → battle 2 / stage 101 → recovery 3 / stage 20 → boss 4 / stage 102 |
| `TemporaryCoreRuntimeFactory.CreateWithShop()` | Start 1 → battle 2 / stage 101 → shop 5 / stage 30 → boss 4 / stage 102 |

The shop definition is regular, with one Akimbo candidate costing 5. Existing offer rules create three independently purchasable slots; duplicate kinds do not share owned identity. Both scenarios retain initial main plus three Akimbo, wallet 50, empty artifact board, existing temporary fields and explicit targeting profiles. Buying `n` slots leaves gold `50 - 5*n` and roster count `4 + n`. Purchased identities start with zero souls and occur exactly once in the next battle's full Sub draw order; they need not all fit in its initial hand.

This data is disposable execution data, not balance approval, a DB schema or permanent content. When the Excel-imported DB and production targeting linkage are ready:

1. Validate real stages, candidate unit definitions, loadouts, fields, categories, Economy and recovery tables through existing query contracts. Finish production Method_Key querying/linkage and `DatabaseBattleInitializationSource` conversion.
2. Switch development callers back to normal runtime composition and authored graph generation, retaining the Core shop API sequence. Re-run the service cycle against real definitions and perform separate scene/Job/Flow acceptance.
3. Remove `Factory/Runtime/Development` and temporary runtime scenario tests when the replacement acceptance is complete. Remove temporary factory call sites before removing the fixtures. Keep the general Core shop/recovery APIs and their service/domain tests; they are permanent code. Retain named battle-source injection only if still used for supported composition/testing.

No production data fallback or asset/importer change was introduced. Boss victory still uses existing chapter-clear behavior; next-chapter/final-Run victory and rewards are not added.

## Verification evidence and limits

Raw test XML/logs, arguments/exit records, before-P3 snapshots, comparison and ledger are outside the repository at `C:/Temp/ProjectL-shop-core-p3-20261003`. Unity 6000.0.73f1 uses the approved AppData-capable execution path.

Fresh pre-P3 baseline: 1661/1666, exit 2, zero skipped, with the five known `Combat stage requires Economy` catalog failures. RED: 7/12, exit 2; all five new cases fail the absent named scenario assertion while the original seven temporary cases pass. Final tests use typed factory calls.

The new four purchase-count cycle cases resolve both victories through real commands, confirm retained terminal battle blocks entry without writes, clean up through caller-owned EndBattle, enter/shop/complete on the same services and prepare the boss. They verify HP, existing souls, wallet, progress, individual new owned IDs, receipts and full draw-order membership, plus result/counter and View retention at each existing lifecycle boundary. A fifth case verifies the separately initialized shop graph. Default recovery and existing deck/roster, chapter-graph, composition and preparation coverage are reused.

An intermediate cycle run exposed a fixture mistake at the second battle: a deliberately retained old View draw command was processed before the new EnterCombat command. The fixture now verifies input retention, then explicitly performs caller-owned View Clear before beginning the second battle; no production behavior changed.

| Run | Passed / total | Exit / skipped |
| --- | --- | --- |
| `baseline.xml` | 1661/1666 | 2 / 0 |
| `red.xml` | 7/12 | 2 / 0 |
| `focused-final.xml` | 326/326 | 0 / 0 |
| `regression-final.xml` | 1666/1671 | 2 / 0 |

`comparison.json` confirms five added passing cases, no missing tests, no changed baseline outcomes and identical baseline/final failure names, messages and stacks. The same five failures report `Combat stage requires Economy`:

- `ArtifactDataTableTests.Catalog_RegistersOnlyMigratedRulesAndExplicitlyPendingViews`
- `DataBaseAssetTests.Catalog_CreatesAllDataBaseGroups`
- `DataBaseAssetTests.CombatStages_ReferenceExistingInGameData`
- `DataBaseAssetTests.EveryStage_HasNodeVisual`
- `DataBaseAssetTests.EveryUnitAndStructure_HasUnitVisual`

The whole suite remains failing on that pre-existing authored-data issue. Baseline and final broad runs select `ProjectL.Tests.Core;ProjectL.Tests.Framework;ProjectL.Tests.CombatViewBoundary` in EditMode. Exact arguments and exit records are in `baseline.json`, `focused-final.json` and `regression-final.json`; logs/XML share their run names. The saved launcher was invoked as:

```powershell
& 'C:/Temp/ProjectL-shop-core-p3-20261003/run-tests.ps1' -RunName baseline
& 'C:/Temp/ProjectL-shop-core-p3-20261003/run-tests.ps1' -RunName focused-final -Filter 'ProjectL.Tests.Framework.TemporaryCoreShopRuntimeTests;ProjectL.Tests.Framework.TemporaryCoreRuntimeTests;ProjectL.Tests.Framework.CoreStageCombatCycleTests;ProjectL.Tests.Framework.ShopStageServiceTests;ProjectL.Tests.Framework.ShopEntryPublicationTests;ProjectL.Tests.Framework.CharacterRosterBattleCycleTests;ProjectL.Tests.Framework.FrameworkRuntimeCompositionTests;ProjectL.Tests.Framework.ChapterGraphPreparationTests;ProjectL.Tests.Framework.BattleDefinitionAdapterTests;ProjectL.Tests.Framework.BattleEntryPublicationTests;ProjectL.Tests.Framework.CharacterBattlePreparationTests;ProjectL.Tests.Core.ChapterProgressionCommandsTests;ProjectL.Tests.Core.CharacterRosterRuleTests'
& 'C:/Temp/ProjectL-shop-core-p3-20261003/run-tests.ps1' -RunName regression-final
```

These are completed-run records, not instructions to overwrite the saved evidence. Use a fresh evidence directory/run name for subsequent verification.

A fresh independent read-only reviewer approved scoped P3 delivery and P1–P3 contract coherency with no Critical, Important or Minor findings. The reviewer inspected exact before-P3 diffs, integration boundaries, updated handoffs and saved focused/broad evidence without running Unity. Metadata GUID uniqueness, documentation links, source whitespace, unchanged assembly dependencies and development guards passed delivery checks. Unity's generated FMOD timestamp was restored; no raw XML/log/launcher was added to the worktree.

Core service acceptance is separate from authored production-data acceptance and actual Job/Flow/View/scene integration. These tests use an HQ-only temporary opponent; they do not establish enemy AI, authored difficulty, scene playability, total framework migration or final Run completion.

References: [design](../specs/2026-10-03-shop-stage-core-connection-design.md), [implementation plan](../plans/2026-10-03-shop-stage-core-connection-implementation-plan.md), [P1 verification](2026-10-03-shop-stage-core-connection-p1-verification.md), [P2 verification](2026-10-03-shop-stage-core-connection-p2-verification.md), [temporary runtime](2026-10-03-temporary-core-runtime.md).
