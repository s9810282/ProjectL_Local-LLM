# Run Deck — DB/Content (E2) Handoff

Date: 2026-10-20 (Asia/Seoul), issued after the Core refactor Phase 1 gate; **finalized at the Phase 4 gate** (decision record §15). Decision source: decision record (Git history: `2026-10-19-run-deck-core-refactor-plan.md`). Owners: DB/content programmer (Unit/loadout/roster/shop assets, `PlayerLoadoutData` validation) and the View visual owner (UnitVisual). Local handoff, not sent externally.

**Merge gate: the operating DB does not boot on `feature/Cheon/refactor/core-system`.** Under the decision record's L3 gate, this branch is not merged into a shared branch until these asset corrections land and an operating Boot is verified by playing. No exception is approved.

## 1. Verified cause (code and assets inspected; the baseline run at HEAD already fails `DataBaseAssetTests.Catalog_CreatesAllDataBaseGroups`; an actual Play From Boot was not run)

- `UnitTable`'s constructor (`DataBaseSystem/Queries/InGameDataBase/InGameTable/UnitTable.cs`) calls `UnitData.Validate()` for every registered unit.
- `Unit_MainCharacter.asset` (key 14, role MainCharacter, `ownerEligibility: 0` = PlayerOnly, category 10) is still registered in `UnitDataTable.asset` and is the `mainCharacter` of `PlayerLoadout_1.asset`.
- `UnitData.ValidateMainCharacter` calls `PlayerMainDefinitionPolicy.Validate`. The policy rejects the retired key 14 as a player definition, so `DataBaseFactory.Create` fails and Boot stops.
- `PlayerLoadout_1.asset` has no `initialDeckGrantIndices`. Even with valid units, `IRunStartService.StartNewRun(1)` would now return `FailureReason = InvalidInitialDeck` with `InitialDeckFailure = EmptyDeck`. Before this refactor the same case was reported only as `InvalidRequest`.

**This is a content dependency, not a Core defect.** The policy is the approved rule (design D7), so it is not weakened, skipped or caught. Core does not fabricate a default selection.

## 2. Required asset changes (one coordinated package)

| Asset | Change |
|---|---|
| `DataBaseSO/InGameGroup/Unit/Item/Unit_Akimbo.asset` (+ meta, new) | Approved Akimbo stats, targeting, wall policy and traits; key 10, role MainCharacter, PlayerOnly, category Main (10), 1x1 footprint. Keep the KillChainAttack-only composition and existing enhancement semantics. Do not copy the retired Main's balance. |
| `DataBaseSO/ViewGroup/UnitVisual/Item/UnitVisual_Akimbo.asset` (+ meta, new) | Approved visual references. View visual owner. |
| `UnitDataTable.asset` / `UnitVisualDataTable.asset` | Register Akimbo; remove `Unit_MainCharacter` (key 14) and its visual from operating registration. Keep the retired asset files and GUIDs; do not reuse value 14. |
| `PlayerLoadout_1.asset` | `mainCharacter` = Akimbo; entries Cactus Cannon ×1 and Shotgun ×1 (remove the Sniper entry); `initialDeckGrantIndices: [0, 1, 2]`; keep `startingRunCurrency: 20` and `startingHandSize: 4`. Expanded grant order is Main first, then entries in authored order and count. |
| UnitRoster / acquisition / `Shop_Default` references | Remove Sniper's active routes; retain the Sniper Unit/Visual/trait assets and GUIDs unchanged. Do not give Sniper a Main or Cannon role. |
| `Unit_Demon.asset` | No change: category Main (10), role Regular, EnemyOnly. |

## 3. `PlayerLoadoutData.Validate` requirement (DB owner)

`PlayerLoadoutData` (`DataBaseSystem/Data/InGame/Table/Loadout/Item/PlayerLoadoutData.cs`) belongs to the DB owner. The Core programmer added only the `InitialDeckGrantIndices` field. Add this structural check to `Validate()` so authoring errors fail at DB creation:

- `InitialDeckGrantIndices` is non-empty.
- Every index is distinct and inside `[0, 1 + Σ entry.Count)`; the expanded grant count is Main first plus each entry repeated `Count` times.

Do not replicate deck rules (exactly one Main, at most four cards, Main moves to index 0) in DB code. Those stay in Core's `DeckRules.EvaluateInitial`, which `StartNewRun` runs and reports with the specific `DeckFailureReason`.

Recommended asset test in `DataBaseAssetTests`, run against the real catalog:
- `DataBaseFactory.Create` succeeds.
- Run `CharacterDefinitionAdapter`/`PlayerLoadoutRosterPlanner` plus `DeckRules.EvaluateInitial` for loadout 1 and expect `DeckFailureReason.None`, with canonical indices `[0, 1, 2]`.
- Akimbo is the only `PlayerOnly` MainCharacter.

## 4. Completion test

1. `ProjectL.Tests.Framework.DataBaseAssetTests` and `EnhancementAuthoringTests` pass against the real assets (not fixtures).
2. Play From Boot reaches StageSelect, and a new Run shows exactly Akimbo, Cactus Cannon and Shotgun as the committed deck.
3. Boot/Play integration failures that the Phase D audit attributed to the retired Main catalog no longer appear in a full Framework run.

A passing Core rejection test for invalid loadout data does **not** prove the operating DB boots; only steps 1–2 do.

## 5. Phase 4 evidence (2026-10-20, full Framework run on the refactored tree)

- `DataBaseAssetTests`: 30 of 43 fail, every one with "퇴역한 메인 키는 플레이어 정의로 사용할 수 없습니다." — the operating DB build stops on the retired key 14 (§1). The same failures and messages are in the HEAD baseline; the refactor adds no new cause.
- No other Framework failure changed message against the baseline. The remaining baseline failures that need a booting operating DB stay blocked until §2 lands.

## 6. Test decision deferred until the data exists (X-2)

`DataBaseAssetTests.MixedShopProduction_StartingRosterLeavesThreeDistinctItemOffers` assumes "owning every listed shop unit leaves only item offers". The approved category-cap policy no longer excludes a unit because it is owned: two per Cannon/Rider/Heavy category, one per Main definition.
- Core made only the compile fix: the planner now requires a definition table, read from the real unit table as `ShopService` does.
- The test still fails at the DB build (§5), so the new call path has not run yet.
- After §2 lands, decide together whether to adapt its premise to the cap policy (with the new `Shop_Default` unit list) or retire it. Do not rewrite it only to make it pass.
