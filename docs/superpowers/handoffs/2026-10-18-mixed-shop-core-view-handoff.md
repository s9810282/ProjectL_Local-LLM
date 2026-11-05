# Mixed Shop — Core/View Handoff

Date: 2026-10-18  
Scope: mixed units/items, one purchase per visit, automatic completion, and safe completion-only retry.  
Design: Mixed shop design (Git history: `2026-10-18-mixed-shop-single-purchase-design.md`)\
Plan: Implementation plan (Git history: `2026-10-18-mixed-shop-single-purchase-plan.md`)\
Executed evidence and remaining acceptance: [Verification](2026-10-18-mixed-shop-verification.md).

## Public entry and input contracts

Root: `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/`.

`ShopSystem/ShopService.cs` exposes `IShopService` through the existing Core group `Shop` property:

```csharp
ChapterProgressionResult EnterShop(int chapterKey, StageNodeId targetNodeId, int seed);
ShopPurchaseResult Purchase(ShopVisitContext context, ShopOfferId offerId);
ChapterProgressionResult CompleteShop(ShopVisitContext context);
```

`CoreCaculator/Contracts/Shop/ShopVisitContext.cs` is the immutable identity:

```csharp
ShopVisitContext(int chapterKey, StageNodeId nodeId, StageKey stageKey, StageEntryId entryId);
```

Use the current Buffer visit's `EntryId`, not a locally generated identity. A later visit or new Run must not receive an earlier visit's input, even if chapter/node/slot numbers are identical. The identity is preserved by the popup model, queued command, and receipt.

View submits through `BufferSystems/ShopView/ShopViewCommand.cs` and the existing ShopView write sink:

```csharp
ShopViewCommandData.Purchase(ShopVisitContext context, ShopOfferId offerId);
ShopViewCommandData.Complete(ShopVisitContext context);
```

The stage scheduling entry remains `StageCommands.ProcessNextShopCommand(int expectedRequestNumber)`. `ShopFlow`, `ShopPlan`, and `ShopCommandJob` retain scheduling responsibility. View never calls an inventory grant after payment and never writes currency or inventory to recover completion.

## Products, calculations, and state ownership

| Fact or operation | Source owner |
|---|---|
| Candidate unit/item keys, recruit price, category weights | `DataBaseSystem/Data/InGame/Table/Stage/Shop/ShopStageData.cs` and `ShopStageDetail.cs` |
| Item price, name, description, effect | Existing consumable InGame definitions |
| Candidate adaptation and validation | `CoreSystem/ShopSystem/ShopDefinitionAdapter.cs` |
| Distinct weighted assortment and frozen recruit level | `CoreCaculator/Application/Shop/ShopOfferPlanner.cs` |
| Purchase decision | `CoreCaculator/Application/Shop/ShopPurchaseRules.cs` |
| Atomic payment, acquisition, purchase mark | `BufferSystems/Core/ShopTransactions.cs` |
| Current visit, offers, purchase mask | `BufferSystems/Current/CurrentShopData.cs` |
| Run gold and item inventory | Existing Currency and Run ItemInventory buffers |
| Receipt sequencing | `CoreSystem/ShopSystem/ShopCommandExecutor.cs` |
| Projection and popup lifecycle | `ViewSystem/StageMap/Shop/ShopPopupModelBuilder.cs` and `ShopPopupController.cs` |

Capacity is `ShopOfferLimits.Maximum == 3`; an actual visit may contain 0-3 products. The remaining category is sampled with item:unit weights 2:1, then uniformly within that category. Products do not repeat. Owned unit keys are excluded; owned item stock does not exclude an item. Each item purchase grants one.

`ShopPurchaseSnapshot` exposes `Kind`, `OfferId`, `CharacterKey`, `ItemKey`, `Cost`, `InitialLevel`, `ItemName`, and `ItemDescription`. Unit and item payloads remain distinct. Offers are frozen at entry; restoring, rejecting, or hiding does not reroll them.

Recruit level is the floored average of all owned Sub levels, minimum one, excluding Main. For 2/3/6 it is 3, with the fixed 600G price. Item prices are Whiskey 100G, Roket 150G, Rope 200G, Lantern 200G.

`ShopPurchaseResult` exposes `Succeeded`, `FailureReason`, `Kind`, `UnitAcquisition`, and `ItemKey`. An item result has no fake unit acquisition. `CurrentShopData.HasPurchase` derives from its mask and permanently locks this visit's other products. There is no separate mutable permission or completion-pending flag.

## Ordering and recovery

1. The UI locks all controls before submitting its first intent.
2. The executor validates the request and performs the atomic shop purchase. The transaction rechecks expected roster, inventory, gold, and visit facts before installing prepared values.
3. A correlated receipt containing the committed purchase is published before fallible progression completion.
4. The executor calls `CompleteShop` synchronously and updates that same receipt with completion only if it still owns the result slot and visit.
5. View matches request number, visit context, command kind, and offer. On confirmed completion it acknowledges/removes its input, closes through the UI service/scope, and refreshes the map.

`BufferSystems/Current/ShopCommandResult.cs` distinguishes `PurchaseResult` and `CompletionResult`. A successful `PurchaseResult` means acquisition/payment committed; it does not establish successful progression completion. Exceptions propagate while the first receipt and purchase mark remain intact. Runtime faults follow the existing shared fault boundary; exceptions are not disguised as ordinary purchase rejections.

This mixed Buffer receipt differs from the legacy `CoreCaculator.Contracts.Results.ShopCommandResult`. Qualify or alias the Buffer type when importing both namespaces; the legacy result is not the active UI receipt.

The single result slot is retained until View acknowledges/removes the handled request. Both executor and ShopFlow wait on that oldest unacknowledged command. A queued second command cannot overwrite the receipt. Once acknowledged, another purchase is rejected by the used/completed visit.

For a recoverable completion failure, release the matching UI request, render all cards disabled from Buffer facts, and allow only `Complete(context)` through the existing exit control. Its label becomes `완료 재시도`. Do not submit another Purchase. A restored purchased visit follows the same rule without a pending request. A completed visit closes/reconciles; hiding preserves ownership of pending results. Stale visit input never targets a new visit.

## View data and authoring

`DataBaseSystem/Data/View/Table/ConsumableItem/Item/ConsumableItemVisualDataSO.cs` contains the View-only data and SO. Its fields are `ItemKey` and nullable `Icon`. The matching table and query are `ConsumableItemVisualDataTableSO` and `ConsumableItemVisualTable`:

```csharp
ConsumableItemVisualData Get(int itemKey);
bool TryGet(int itemKey, out ConsumableItemVisualData data);
```

`IViewQuery.ConsumableItemVisualTableQuery` and `ViewTableKey.ConsumableItemVisual` provide composition. `StageMapViewSystem` injects both unit and consumable visual queries into `ShopPopupViewFactory`. The popup reads no InGame query.

Registered assets:

- `Assets/ProjectL/DataBaseSO/ViewGroup/ConsumableItemVisual/ConsumableItemVisualDataTable.asset`.
- Four rows under its `Item/` directory: Whiskey, Roket, Rope, Lantern.
- `ViewGroup.asset` includes the table; `Shop_Default.asset` references the four existing consumable definitions and preserves ShotGun/Sniper.

All four final icons are currently **null**. Assign approved Sprite art to those View rows when supplied. A missing icon hides the image while retaining name, price, description, quantity, identity, and input. No copied item display-name/price source belongs in this View table. Item owned count is not displayed.

Unit cards show the frozen level and current owned-unit count; item cards show quantity one and frozen description. Existing `UIInputBinding`/`IUIInputReceiver` handles PointerClick and Submit. Stale pooled slots are disabled when rendering fewer cards. `ShopPopupModel` carries copied display values; ShopUI has no transaction rules.

No missing serialized reference required a scene/prefab rewrite. Real assets remain `Assets/ProjectL/Prefabs/UI/Shop/ShopUI.prefab`, `StageOfferCard.prefab` in that directory, and `Assets/Scenes/StageSelect.unity`. The existing heading still says `캐릭터 상점`; manual caption/readability acceptance remains recorded separately.

## Content and locality limits

The default starting roster already owns both production recruit candidates, so default production shopping is item-only. The 700G/unowned ShotGun acceptance fixture proves mixed-contract recruitment without changing production eligibility or candidates. It is not evidence that the live default assortment contains a recruit. Natural starting funds may permit zero purchases; the explicit natural-map smoke now permits skipping an unaffordable visit and labels its separate zero-gold boundary.

L1: shop transactions no longer depend on recovery input/result internals. Future stock changes read shop facts and roster/currency/inventory contracts; shared guards and Buffer composition remain necessary boundaries.

L2: sequencing changes are local to the shop executor and its View consumer. The one-slot result acknowledgment and shared stage dispatcher remain integration contracts; they must be preserved when editing command scheduling.

L3: active capacity and permission come from the focused shop contract/Buffer facts. Future card-count changes need the active contracts, projection, and pooled-input tests; inactive legacy wallet/deck models are not part of the runtime feature.

L4: production candidate selection, final art, and human readability are content/acceptance inputs. No unlock framework, automatic candidate expansion, or unrelated art reuse was introduced. No measured speed/performance improvement is claimed.

The design keeps responsibilities separated: pure calculation, expected-state Buffer installation, service fact assembly, command sequencing, composition, and display. Existing acquisition, inventory arithmetic, progression, Framework registration, and popup scope contracts are reused. Full-suite failures and unconfirmed manual checks are listed in verification; do not infer repository-wide green or human acceptance from scoped tests.
