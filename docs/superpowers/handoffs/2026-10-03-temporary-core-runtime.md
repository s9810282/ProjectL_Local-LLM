# Temporary Core Runtime

Date: 2026-10-03  
Base: `e8977adc`  
Purpose: unblock Core battle preparation and the combat/recovery service cycle while Excel-imported definitions are being prepared.

Status: implemented, verified and independently reviewed. Changes are uncommitted.

## Entry point

`TemporaryCoreRuntimeFactory.Create()` is an explicit, development-only entry point. It is compiled only for `UNITY_EDITOR || DEVELOPMENT_BUILD`; the normal `FrameworkRuntimeFactory.Create()` still uses the original DB-backed source. There is no automatic fallback when production data is invalid.

```csharp
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using ProjectL.Systems.Factory.Runtime.Development;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.ServiceKeys;

var runtime = TemporaryCoreRuntimeFactory.Create();
var core = runtime.GetService(SystemGroupsService.InGame).CoreGroup;
var entry = core.Combat.EnterBattle(
    TemporaryCoreRuntimeFactory.ChapterKey,
    new StageNodeId(TemporaryCoreRuntimeFactory.FirstBattleNodeId));
#endif
```

Creation already completes InitData, PostBindService, Initialize, roster/currency/artifact-board setup and chapter entry. Do not initialize it again. Retain this runtime while using its services and call `runtime.Shutdown()` when finished. Each call to Create produces independent Buffers/services.

Only real Buffer and InGame/Core/View group composition is included. This is a callable Core development environment, not a full game bootstrap: Global, LifeCycle, Jobs, Unity input, scene rendering and View definitions are not supplied. A normal game runtime must not be replaced with this runtime while expecting those services. Existing Core command/result Buffer ports and service APIs drive the scenario; no alternate combat calculator or test assembly dependency is introduced.

## Temporary scenario

| Fact | Development value |
| --- | --- |
| Chapter | 1; fixed start node 1 → combat node 2 → recovery node 3 → boss node 4 |
| Stage keys | Combat 101, recovery 20, boss 102 |
| Starting roster | Main character plus three Akimbo instances, matching existing initial-roster policy |
| Unit rules | HP 10, attack 5; Base category; explicit radial-square/single targeting, no splash |
| Fields | First battle 4×5; boss battle 5×5; reserve capacity 8 |
| Placements | Main at (1,1); enemy headquarters at (1,3), HP 5 |
| Enemies | Headquarters target only; no enemy-unit loadout |
| Economy | Initial wallet 50; manual draw cost 2; normal first-preparation draw restriction remains |
| Recovery | Option 1: heal 3 for 5; option 2: full heal for 10; no-purchase completion supported |
| Artifact board | Empty 4×4 board; no artifact definitions |

These values are execution fixtures, not approved balance. `TemporaryCoreDatabase` uses existing DB data/query contracts for fields, stages, units, roster, categories, economy and recovery. The existing `BattleDefinitionAdapter` reads and validates those definitions. `TemporaryBattleInitializationSource` supplies explicit targeting profiles for the two known temporary unit types; it does not infer or define the pending Method_Key authoring schema. Unknown keys or a different DB context fail rather than selecting a default definition.

Use normal commands to enter combat and resolve victory. After settlement the caller must call `Combat.EndBattle()` before entering recovery. Then call `Recovery.EnterRecovery(1, node 3)`, optionally `Recovery.Recover(1, node 3, option 1 or 2)`, and `Recovery.CompleteRecovery(1, node 3)`. Enter the boss with `Combat.EnterBattle(1, node 4)`. The boss clears the chapter through existing progression; no final-Run victory, reward or next-chapter policy is added.

## Additional disposable shop scenario (P3)

`TemporaryCoreRuntimeFactory.CreateWithShop()` creates a separate initialized runtime using the same composition/initial roster, wallet and battle definitions. Its graph is start 1 → battle 2 (stage 101) → shop 5 (stage 30) → boss 4 (stage 102). `ShopNodeId` is 5. The shop is regular, with one Akimbo candidate costing 5; existing rules produce three independent slots. Buying n slots yields wallet `50 - 5*n` and owned count `4 + n`. `Create()` retains its original recovery graph and remains a unique public method name.

After actual battle settlement, call legitimate `Combat.EndBattle()`, then `Shop.EnterShop(1, node 5, seed)`, optional `Shop.Purchase(1, node 5, offerId)`, and explicit `Shop.CompleteShop(1, node 5)`. Purchase is optional; a third purchase never auto-completes. Enter the boss with `Combat.EnterBattle(1, node 4)`. Old compatibility purchase methods do not guarantee explicit stale-node protection. Completion failure after successful purchase retains payment/acquisition/slot state; correct the cause and retry only CompleteShop, never re-enter/reset/repurchase.

Shop retains battle results/counters and View inputs according to the existing lifecycle. Caller-owned EndBattle clears active combat facts while retaining completion/result/counters; next battle installation clears the old completion/Core preview. The View/Flow owner must handle its own input cleanup before new battle commands; Core does not clear pending View commands implicitly. See the [shop consumer handoff](2026-10-03-shop-stage-core-connection-handoff.md) for exact acceptance, retry and replacement conditions.

Both scenarios are disposable development data. When the real Excel DB and production targeting linkage pass replacement acceptance, switch callers to normal composition and remove the Development definitions/factories and temporary scenario tests. Permanent Core APIs/service tests remain. There is no automatic production fallback.

P3 focused verification: 326/326, Unity exit 0, zero skipped. Five new cases verify the shop graph and purchase counts 0–3 through two real victories, including HP/souls, owned IDs and full next-battle draw order, receipts, progress and result/counter retention. The original recovery path is included in this run. P3 broad verification is 1666/1671, exit 2: five new cases pass, all 1,666 baseline outcomes and five failure messages/stacks are unchanged. A fresh independent read-only reviewer approved P3 and final contract coherency with no findings; its evidence stays separately at `C:/Temp/ProjectL-shop-core-p3-20261003`. Original evidence below remains historical.

## Additional disposable chapter-continuation scenario

`TemporaryCoreRuntimeFactory.CreateWithChapterContinuation()` retains chapter 1's shop path and supplies a second chapter plus explicit Next(1,2)/Final(2) definitions. Constants SecondChapterKey/SecondChapterFirstBattleNodeId/SecondChapterBossNodeId are 2/2/3. Chapter 2 is start 1 → battle 2 (201) → boss 3 (202), with valid map-2 roots, depth 3, width 1 and seed 42. Both maps register through ordinary DB InitData; the second graph is absent until CompleteChapter prepares/publishes it.

After genuine chapter-1 boss settlement, caller EndBattle precedes `ChapterProgression.CompleteChapter(1, node 4)`. Accepted NextChapterSelected selects map 2 with no selected node. Enter its two battles through normal EnterBattle. After its genuine boss victory and caller EndBattle, `CompleteChapter(2, node 3)` yields RunCompleted and one Victory receipt (chapter 2/node 3/key 202), retaining final selection and owned facts. Query/definition failure retains settlement: correct the dependency and retry only CompleteChapter. Do not reprepare the cleared boss or reset/replay settlement. View input cleanup remains caller-owned.

Create()/CreateWithShop() preserve their one-map defaults and unavailable continuation source; they do not implicitly become two-chapter/final routes. The constructor's second-map option defaults off. The existing temporary battle targeting adapter is reused unchanged. Normal composition never falls back to this scenario.

Chapter-continuation P3 implementation, verification and independent review completed on 2026-10-04 with no outstanding findings. Unity verification: new cases 5/5; focused 314/314; broad 1840/1845, with the same five pre-existing Economy failures and no changed existing outcomes/messages/stacks or removed tests. Actual commands resolve four victories, shop payment/deck carry-over and final Victory; separate cases cover second regular/boss defeat and isolation from both default runtimes. Existing next/final fault-retry tests are reused. Artifact retention uses a populated pool fixture through an existing trusted test seam; defaults remain empty. See the [current continuation consumer handoff](2026-10-03-chapter-continuation-run-completion-handoff.md) for APIs, ownership, evidence and production replacement. Raw evidence is external at `C:/Temp/ProjectL-chapter-run-p3-20261003`. Prior P3 counts above refer to the historical shop phase.

## Replacement when Excel data is ready

1. Supply the real catalog/query definitions, including valid Economy references, recovery items/stages, shop candidates and Chapter_NextChapter/explicit-final representation and continuation adapter.
2. Finish Method_Key querying/unit linkage and the production `DatabaseBattleInitializationSource` conversion. Temporary targeting profiles are not a production mapping.
3. Use normal runtime composition and authored initialization/graph generation instead of the temporary factory. Keep combat/recovery/shop consumer calls unchanged, using the explicit shop identity APIs.
4. Run equivalent authored-data continuation/final/defeat/retry acceptance and switch consumers, then remove the Development folder, its continuation source and temporary scenario tests when no longer useful. Permanent Core continuation/Run APIs and generic service/rule tests remain. Small source-injection composition methods may remain if needed; parameterless Create preserves its unique lookup/call contract.

The implementation is deliberately a bounded extension of existing injection/composition. It does not complete all framework migration, authored-data acceptance or scene playability. No importer, production catalog asset, View/Flow or gameplay rule was changed.

## Evidence

The new factory's absence was observed as six failing setup assertions before implementation. The final tests use typed API calls. Temporary data was corrected to the existing main-plus-three roster policy; a test expecting manual draw during first preparation was corrected to assert the existing rejection with no payment. These corrections did not alter gameplay rules.

Final focused Unity EditMode: **206/206 passed**, covering the seven new cases and existing adapter, preparation, composition, combat-cycle, recovery and chapter-graph fixtures. New cases verify initialization, both applied/skipped recovery through two real command-driven victories, chapter clear, HP/currency carry-over, first-preparation draw rejection, invalid navigation, source isolation and independent runtimes. The cycle supplies damage through the existing health API; no fake victory or direct clear write is used in the tests.

The first broad run exposed four existing chapter-graph failures because an overloaded `Create` made reflective method lookup ambiguous. The new injection method was renamed to `CreateWithBattleSource`, retaining the unique parameterless `Create`. The existing failures supplied regression evidence; all 40 chapter-graph cases then passed in the final focused run.

Fresh broad baseline: **1525/1530**, Unity exit 2. Final broad: **1532/1537**, exit 2. Seven cases added, none missing and no changed existing outcomes. The following five pre-existing failures retain exactly the same names/messages/stacks, all reporting `Combat stage requires Economy`:

- `ArtifactDataTableTests.Catalog_RegistersOnlyMigratedRulesAndExplicitlyPendingViews`
- `DataBaseAssetTests.Catalog_CreatesAllDataBaseGroups`
- `DataBaseAssetTests.CombatStages_ReferenceExistingInGameData`
- `DataBaseAssetTests.EveryStage_HasNodeVisual`
- `DataBaseAssetTests.EveryUnitAndStructure_HasUnitVisual`

The whole project suite is therefore still failing on authored catalog readiness. Temporary composition bypasses that catalog only by explicit selection; it does not conceal or fix production data failures. Unity 6000.0.73f1 used the approved AppData-capable execution path. Focused and broad runs had no skipped cases. Raw XML/logs, arguments and exact comparisons are stored outside the repository at `C:/Temp/ProjectL-temporary-core-20261003`, not in commit candidates.

A fresh read-only reviewer found no Critical, Important or Minor issues and independently inspected the final focused results. Review did not execute Unity. Full scene/runtime integration, production Method_Key mapping, enemy AI, authored balance and final Run policy remain outside this delivered scope.

Changed files: the three `Factory/Runtime/Development` C# files and metadata; the named source-injection method in `InGameSystemFactory.cs`; `TemporaryCoreRuntimeTests.cs` and metadata; this handoff. Structural checks cover unique metadata GUIDs, development compilation guards, Korean new comments, English handoff, no test-assembly dependency and a clean whitespace diff. Raw verification files were not added to the working tree.
