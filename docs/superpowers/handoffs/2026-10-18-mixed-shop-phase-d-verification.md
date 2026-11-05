# Mixed Shop — Phase D Verification

Date: 2026-10-18  
Authorization: user requested Phase D only. D1-D2 complete; stop before Phase E.  
Plan: Mixed shop implementation plan (Git history: `2026-10-18-mixed-shop-single-purchase-plan.md`)\
Design: Mixed shop design (Git history: `2026-10-18-mixed-shop-single-purchase-design.md`)

## Delivered behavior

- Added the View-only consumable icon data, table, key, query property, factory registration, and popup injection. The data contains only positive `ItemKey` and nullable `Sprite Icon`. Prices, names, descriptions, effects, and inventory are not duplicated there.
- Item cards use frozen Buffer text/price and the item-key icon lookup; unit cards retain their View unit lookup. Cards show frozen unit level, or quantity one and item description. Missing icons leave readable, purchasable cards; no unrelated art was borrowed.
- Registered four item visuals and activated Whiskey/Roket/Rope/Lantern in `Shop_Default`. Prices remain 100/150/200/200G; recruits remain 600G and weights 2/1. The ShotGun/Sniper recruit references remain unchanged. A real-asset test confirms the default starting roster owns both recruit candidates and the sampler produces three distinct item offers. No production recruit expansion was made.
- All product buttons derive their lock from `CurrentShopData.HasPurchase`. A matching committed receipt without completion releases its request but leaves every product locked and exposes `Complete` on the existing exit button, labeled `완료 재시도`. Completion rejection can be retried without payment/acquisition.
- Confirmed purchase-plus-completion and skip close through `PopupViewScope`/UI service and refresh the map. Hidden requests are consumed without reopening. Restored purchased/completed visits reconcile without needing a pending request; a changed visit closes the old popup before restoring a new identity.
- Ordinary ownership/quantity refusals stay visible. Purchase-used refusals remain locked by Buffer facts; stale contexts reconcile; unexpected item failures and close exceptions use `RuntimeFaultBoundary`.
- Removed the shop success-status timer and its unused Delay dependency. Existing failure status, input guards, request numbers, UI history, pooled-card input behavior, and layer ownership remain in use. Shared DelaySystem and popup scope internals were not changed.

## Verification

Unity 6000.0.73f1 ran with approved AppData access. The project had no running Unity Editor at preflight. No caches were deleted or Editor versions changed. Each selected run completed with XML; process exits were checked. Evidence is under ignored `Library/CodexShopValidation`.

### Final scoped gate

`d-gate-complete.xml` / `.log`: Unity exit **0**, **411 passed, 0 failed, 0 skipped**. This is the final production-code state.

`d-owned-item-projection.xml` / `.log`: Unity exit **0**, **1 passed, 0 failed, 0 skipped**. This additional case was added after the gate and verifies the actual Buffer already contains two copies while the displayed item retains its frozen 100G price, quantity one, offer identity, and enabled purchase action. Production code did not change after the 411-case gate; the new case was run separately. The combined selection covers 412 distinct cases.

| Actual XML fixture | Passed |
|---|---:|
| ArtifactPopupConfigTests | 7 |
| DataBaseAssetTests | 2 |
| GlobalInputCompositionTests | 5 |
| LobbyInputTests | 18 |
| LobbyViewSystemTests | 49 |
| OwnedLevelShopDataTests | 11 |
| RecoveryPopupControllerTests | 13 |
| RecoveryUITests | 21 |
| ShopConsumableVisualTests | 8 |
| ShopPopupModelBuilderTests | 7 |
| ShopStageServiceTests | 207 |
| ShopUITests | 25 |
| StageShopUIAssetTests | 4 |
| UnityStageInputTests | 16 |
| ViewInputCompositionTests | 8 |
| ViewPopupCompositionTests | 10 |

`ShopPopupControllerTests.cs` is part of the partial `ShopStageServiceTests` fixture; it is not a separately named XML fixture. The two selected DataBaseAsset cases verify real catalog construction/prices/visual keys and the default roster's item-only fallback. Existing EventSystem-based `ShopUITests` already cover 3 -> 1 -> 0 pooled slots, stale receiver slots, PointerClick/Submit, immediate all-control locking, and submission reentrancy; those tests were reused rather than duplicated.

The final selection includes recovery popup/UI and the direct `IViewQuery` test consumers. Recovery UI internals, `StageOfferCardContent/View`, and shared popup/display interfaces were not modified. `ViewPopupCompositionTests` additionally verifies a non-null item icon through the default factory in both initialization orders, while its InGame query throws if accessed.

### Behavioral red and intermediate runs

| XML/log stem | Result | Meaning |
|---|---|---|
| `d1-behavior-red` | 11 passed, 2 failed | Item name routed through the unit lookup; other offers remained enabled after a purchase. |
| `d1-green` | 68 passed, 3 failed | D1 projection/catalog/UI assets passed; the three unrelated asset failures below remained. |
| `d2-behavior-red` | 220 passed, 9 failed | Missing automatic close, completion-only presentation, no-pending restoration, normal rejection mapping, and close-fault behavior. |
| `d2-green` | 244 passed | Initial lifecycle implementation and D1 regressions. |
| `d-gate-final` | 410 passed | Shared composition and popup regressions before the final level/quantity display check. |
| `d-card-details-red` | 12 passed, 3 failed | Unit level and item quantity were absent from the actual card text. |
| `d-gate-complete` | 411 passed | Final scoped gate after level/quantity display and production fallback coverage. |

`d2-red.log` was an intermediate test compile failure (ambiguous legacy/mixed result type and incorrect Complete API spelling), corrected in tests before the behavioral red. It produced no XML and is not red behavior evidence. The first `d1-red.xml` contained only the seven existing model tests because the new test file had not yet been generated after a sandboxed Python launch failed; only `d1-behavior-red` is used as D1 red evidence.

`d1-author.log`: Unity exit **0**. A temporary Editor API authoring script created the five View assets, appended one table reference, and assigned four item references. It saved only those targets, was copied to `phase-d/CodexMixedShopD.cs` for evidence, and was removed from Assets with its temporary folder/meta.

### Full DB asset fixture, separate from the scoped gate

`d-assets-final.xml` / `.log`: Unity exit **1**, **39 passed, 3 failed, 0 skipped** out of 42. These are the same identities and observed values recorded in Phase C, also reproduced by `d1-green`:

1. `DataBaseAssetTests.EveryCombatStage_EnvironmentAndViewRotationsReachOperationalDefinition`: Stage 1101 expects 12 entries, receives 13 (extra 213).
2. `DataBaseAssetTests.EveryCombatStage_UsesRequestedNineByNineCenterRail_AndBoundTrainPrefab`: Stage 2101 expects 8, receives 0.
3. `DataBaseAssetTests.LineMaterial_IsRegisteredInCatalog_AndSupportsColorOverride`: `Assets/ProjectL/Script/Render/Shader/Line/LineShader.mat` is absent.

These were observed before D in C's report, but are not claimed to have a pre-C baseline. Their assets were not changed by D. The full asset run preceded the final View text-only change; its two mixed-shop asset cases were rerun successfully in `d-gate-complete`. No full Core/Framework run or manual Play acceptance was performed in D.

## Change inventory and boundary review

`phase-d/status-before.txt`, `head.txt`, `before-hashes.json`, and `sources.json` preserve preflight status/HEAD and per-file hashes. The Phase D comparison contains **14 production C# files, 14 test/support C# files, and 7 production assets**, plus required Unity metadata. The existing dirty A-C and concurrent changes are not attributed to D.

Production code:

- `DataBaseSystem/Data/View/Table/ConsumableItem/{Item/ConsumableItemVisualDataSO,ConsumableItemVisualDataTableSO}.cs` and `Queries/ViewDataBase/ViewTable/ConsumableItemVisualTable.cs` (new).
- `Queries/ViewDataBase/ViewDataBase.cs`, `Factory/DBFactory/Group/ViewDataBaseFactory.cs`, `ServiceKeys/DBFactory/Group/ViewTableKey.cs` (one-table composition).
- `ViewSystem/StageMap/Shop/{ShopPopupModelBuilder,ShopPopupViewFactory,ShopPopupController,ShopPopupText,ShopResultPolicy}.cs` and `StageMapViewSystem.cs` (typed projection, narrow dependency connection, lifecycle).
- `UISystem/Screens/Stage/{ShopPopupModel,ShopUI}.cs` (copied exit text and existing exit-label rendering).

Tests/support:

- `ShopConsumableVisualTests` (new), `ShopPopupModelBuilderTests`, `ShopPopupControllerTests`, `ShopUITestFixture`, `DataBaseAssetTests`, `OwnedLevelShopDataTests`, `ViewPopupCompositionTests`.
- `ArtifactPopupConfigTests`, `GlobalInputCompositionTests`, `LobbyInputTests`, `LobbyViewSystemTests`, `UnityStageInputTests`, `ViewInputCompositionTests`: the new required `IViewQuery` property on their existing stubs only.
- `CombatEntryIntegrationTests.MapMovement`: remove the retired shop-factory Delay argument only. Its broader movement acceptance remains Phase E work; this file compiled with the full test assembly.

Assets: `ViewGroup.asset`, `Shop_Default.asset`, and the new icon table plus four nullable-icon rows under `ViewGroup/ConsumableItemVisual`. No scene/prefab reference was missing in `StageShopUIAssetTests`, so no scene or prefab was rewritten. The existing ShopUI heading still says `캐릭터 상점`; caption/layout inspection belongs to manual acceptance, and the plan allows prefab edits only for proven missing references.

- SSOT: Buffer offers own frozen products; purchase permission derives from the visit-wide mark and progression/context. The View icon mapping contains no duplicate gameplay fields, and UI stores only rendered/request lifecycle values.
- SOLID/locality: the item query is narrow; projection, display, receipt policy, and scope retain their own responsibilities. StageMap passes both visual queries at composition. The projection model has no dependency on the feature text provider. Recovery and Core transaction/command implementations were not edited.
- Framework contracts: table service export follows existing registration; no new runtime modules, subscriptions, generic merchant services, or assembly references were added. Input still uses UIInputBinding/IUIInputReceiver.
- `git -c core.autocrlf=false diff --check` passed; changed C# files were verified as UTF-8/LF with `.meta` files. No stage/commit/push or external review/delivery occurred.

## Remaining acceptance and stop point

- Four final item icons are **unassigned**. Null icons are intentionally supported; final art assignment is pending. Item owned quantity is not displayed, so no inventory-count projection was added.
- Default production content is item-only with the current starting roster. Mixed unit/item icon projection is tested with explicit fixture offers; this does not claim production mixed content.
- Phase E owns updated smoke assertions, full affected/Core/Framework regression, manual runtime readability/closure/retry acceptance, and final combined handoff. Existing E-owned smoke assertions still include the old success-status/card-text assumptions and were not used as D evidence.
- D1-D2 are complete. Stop before E. No design/gameplay deviations or new Core changes were required.
