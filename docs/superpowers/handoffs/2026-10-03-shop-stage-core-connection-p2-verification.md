# Shop Stage Core Connection — P2 Verification

Date: 2026-10-03  
Base: `e8977adc` plus the existing uncommitted P1 and temporary Core runtime work.  
Status: Historical P2 checkpoint, implemented, verified and independently reviewed. P3 is now complete; see the [consumer handoff](2026-10-03-shop-stage-core-connection-handoff.md). Statements below describe the original P2 delivery boundary. No commit or push.

## Delivered contract

The existing `ICharacterShopService` now provides `Purchase(int chapterKey, StageNodeId nodeId, ShopOfferId offerId)` and `CompleteShop(int chapterKey, StageNodeId nodeId)` alongside P1 entry. New consumers must use:

```csharp
core.Shop.EnterShop(chapterKey, nodeId, seed);
// Optional: purchase zero, one, two or three installed offers.
core.Shop.Purchase(chapterKey, nodeId, offerId);
// Call when the consumer requests exit, even without any purchase.
core.Shop.CompleteShop(chapterKey, nodeId);
```

Each call's result must be checked before proceeding. The caller owns legitimate battle settlement and `Combat.EndBattle()` cleanup before entry. Neither purchase nor completion forces cleanup or resets state.

Purchase uses the installed frozen offer and price, validates explicit visit identity and current graph/root metadata, and rechecks live state after the final unit-definition lookup. It reads current main-character and balance facts after that guard, then reuses existing `PlanPurchase` and `PublishPurchase` once. Success commits paid gold, a distinct owned Sub-character identity and the consumed slot together. No new purchase store or acquisition/payment implementation was introduced.

Completion accepts zero through three purchases, including after an unaffordable purchase attempt. Buying the third offer does not auto-complete. The internal coordinator uses the pure `CompleteShopStage` command and makes one accepted selected-node clear write. Completion preserves the selection and purchased mask, roster, currency, HP, combat results/counters and chapter/map state. It produces no fake combat victory, settlement, map-end attempt, chapter clear or reset. Recovery retains its public semantics; only the identical private completion-rule body is shared.

## Failure and retry handoff

| Guarded purchase condition | Existing result |
| --- | --- |
| Requested chapter/node or captured stage key differs | `EntryMismatch` |
| Matching node/map is clear, or the captured shop has been replaced | `StaleEntry` |
| No prepared shop/selection, retained battle, or incompatible stage metadata | `WrongPhase` |
| Unknown offer, consumed offer or insufficient balance | `UnknownOffer`, `SoldOut`, `InsufficientFunds` |
| Invalid character or owned-identity exhaustion | Existing `InvalidCharacter`, `IdentityOverflow` behavior |

Invalid arguments and missing/invalid definitions retain the existing exception contract. Phase/identity/progress guards precede offer rules. Query-fixture mutations are not rolled back; a rejected operation adds no acquisition, payment, slot or clear write of its own. Different nodes sharing a StageKey remain distinct visits. No concurrency or identity-reuse guarantee across Run resets was added.

Completion uses existing progression rejection reasons: `ChapterMismatch`, `StageNotFound`, `StageMismatch`, `ChapterCompleted` and `StageAlreadyCleared`. Incompatible phase/type or invalid definitions fail without the stage-clear write. Duplicate completion rejects; subsequent guarded purchase returns `StaleEntry` for the cleared visit.

Purchase and completion remain separate committed operations. If purchase succeeds but completion fails before its clear write, retain the paid currency, acquired owned identity and purchased mask, with the node still uncleared. Correct the cause and retry **only `CompleteShop(chapterKey, nodeId)`**. The retry clears once without another acquisition/payment. Do not restart entry, reset offers, refund or roll back the purchase. Retrying a consumed purchase remains `SoldOut`.

The compatibility methods `PrepareShop(int)` and `Purchase(ShopOfferId)` retain their existing behavior and intentionally do not provide the explicit path's stale-node request guarantee. New callers must adopt the sequence above.

## Verification

Unity 6000.0.73f1 ran through the approved AppData-capable path. Raw evidence, launcher, exact before-P2 snapshots and ledger are outside the repository at `C:/Temp/ProjectL-shop-core-p2-20261003`.

| Run | Result | Meaning |
| --- | --- | --- |
| `baseline.xml` | 1594/1599, exit 2 | Fresh pre-P2 baseline; five existing catalog/Economy failures. |
| `red.xml` | 55/122, exit 2 | Existing cases pass; 67 new cases fail assertions for missing P2 contracts. Compilation succeeds. |
| `green.xml` | 267/267, exit 0 | Final typed purchase/completion tests and existing progression, roster, battle-cycle, recovery, composition and temporary-runtime regressions pass. |
| `regression-final.xml` | 1661/1666, exit 2 | All 67 new cases pass; no missing tests or changed existing outcomes. |

No cases were skipped. `comparison.json` compares baseline/final names, outcomes, failure messages and stacks. All five failures remain identical and report `Combat stage requires Economy`:

- `ArtifactDataTableTests.Catalog_RegistersOnlyMigratedRulesAndExplicitlyPendingViews`
- `DataBaseAssetTests.Catalog_CreatesAllDataBaseGroups`
- `DataBaseAssetTests.CombatStages_ReferenceExistingInGameData`
- `DataBaseAssetTests.EveryStage_HasNodeVisual`
- `DataBaseAssetTests.EveryUnitAndStructure_HasUnitVisual`

The whole project suite therefore remains failing on the pre-existing authored-data issue.

New coverage uses registered services and real Buffers: frozen offers and distinct owned IDs, stale same-key nodes, pre/post-query phase and graph mutations, no own writes on rejected operations, completion after zero through three purchases, unaffordable exit, duplicate completion, invalid arguments and committed-purchase/completion-fault recovery. The focused case buys a cost-5 offer from gold 50, observes gold 45 and its acquired identity/consumed bit after a completion query fault, then retries only completion. Pure command cases verify accepted node-only completion and literal existing rejection reasons. Existing offer RNG, overflow, publication, deck/roster and recovery coverage is reused.

A fresh independent read-only reviewer found no Critical, Important or Minor findings. The reviewer compared P2 against the exact before-P2 snapshots and independently inspected focused/broad saved evidence, including all 1,599 unchanged baseline outcomes and 67 new passing cases. The reviewer did not run Unity or judge production scenes/authored-data readiness.

## Delivery and remaining scope

The implementation modifies the existing shop service, coordinator and pure chapter command/rules, adds the partial `ShopStageOperationsTests` fixture and extends existing chapter command tests. It adds no Buffer, session, revision, generic transaction, service key or dependency. The new Unity metadata GUID is unique. Existing uncommitted P1/temporary work is preserved. Raw XML/logs/launchers are outside the worktree, and the Unity-generated FMOD scan timestamp was restored.

P2 delivers the complete Core consumer API. P3 still needs the named temporary shop scenario and normal battle settlement → caller-owned EndBattle → shop purchase/skip/completion → next battle integration evidence. No P3 factory/data or cycle tests were implemented here. Actual Job/Flow/View/scene adoption, authored production data, rewards and final Run policy remain separate acceptance.

References: [design](../specs/2026-10-03-shop-stage-core-connection-design.md), [implementation plan](../plans/2026-10-03-shop-stage-core-connection-implementation-plan.md), [P1 verification](2026-10-03-shop-stage-core-connection-p1-verification.md).
