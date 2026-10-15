# Task 8P: Map ownership retirement

Date: 2026-10-01. Baseline: `6367026e211bf8769b5c0cef1707a07f5be7010a` on `feature/Cheon/refactor/core-system`; the tracked checkout was clean before this slice.

## Scope and ownership

This slice removes the disconnected Core map/progression owner. It does not implement another progression owner or change chapter generation, combat scheduling, DB schema, View code, shared Framework behavior, or Buffer APIs.

The surviving source of truth is `CurrentProgressBuffer` for selection, `StageProgressBuffer` for generated nodes/edges and clears, and `MapProgressBuffer` for chapter completion. Systems `ChapterProgressionCoordinator` binds their existing interfaces through `PostBindService`, assembles command-local `ChapterNodeProgress` facts, invokes pure `ChapterProgressionCommands`/`ChapterNavigationRules`/`ChapterProgressionRules`, and publishes through existing Buffer APIs. The existing Systems `StageGeneratorSystem` generates local nodes once and registers them in StageProgressBuffer.

Combat settlement still uses `BattleCompletionCalculator` and the existing `IBattleBufferWriteSink.CompleteBattle` publication boundary. Stage and boss/map completion are prepared before installation by the existing Buffer group. This slice does not add a transaction, lock, version, reset policy, or result history.

View/Flow continues to own combat LifeCycle, Flow, Plan, Jobs, their service wiring and JobTypes. Core owns processing services and pure calculation. No production View, combat Job, Framework, DB, or Buffer file is changed by Task 8P. The boundary test assembly changes test consumers only; its pure fact tests are not a production View integration claim.

## Caller audit and removed source

Before removal, all references to the old map owners were within the legacy Core graph or EditMode tests. Neither the production Core factory nor the current Systems coordinator invoked that graph. The legacy chain was:

`StageMapSessionFactory -> StageMapGenerator -> StageMapRunState -> StageMapSession/StageMapSnapshotBuilder -> RunProgressionService -> RunRecoveryService/RunShopService/RunWalletService`.

`RunProgressionService` retained its own map selection, Run revision, checkpoint health, wallet, active recovery/shop, and completion receipt. Retaining it or adapting its state back into Buffers would preserve the ownership duplication, so its callers were migrated or classified first.

Removed source and matching `.meta` files:

- Domain/StageMap: `StageMapRunState`, `StageMapSelectionPlan`, legacy `StageMapGenerator`, and its unused immutable `StageMapNode` carrier.
- Application/StageMap: `StageMapSession`, `StageMapSessionFactory`, `StageMapSnapshotBuilder`.
- Application/Run: `RunProgressionService`, its now-unused `RunRecoveryService`, `RunShopService`, `RunWalletService`, and `ActiveStageBinding`.
- Domain/Run: now-unused `RunProgressState`, `RunProgressValues`, and `ShopStageState`.

Preserved: the actual Systems generator and coordinator, all three pure chapter rule/command classes, `RecoveryHealCalculator` (used by the current calculator), standalone legacy combat/artifact services with remaining consumers, old shared Contracts, and unrelated deck/wallet helpers with existing tests. Removing all legacy combat state is still Task 8 Step 6, gated by actual production replacement. The surviving legacy artifact tests receive an immutable test-only Stage input; that helper has no graph, current node, Run revision counter, or progression service.

`ResetRunProgress` remains reachable only through the existing explicit `JobType.NewRun -> ResetRunProgressJob` branch. Ordinary `JobType.Start` does not schedule it. Current chapter entry, settlement, presentation acknowledgement, map return, EndBattle, and next chapter preparation do not call it. No reset implementation was modified.

Full before/after searches and old method inventory are recorded under `.superpowers/sdd/2026-09-29-character-buffer-effect-migration-plan/task8p/`. The following mapping is the durable coverage record; the local logs are supporting execution evidence.

## Test migration and retirement mapping

Retirement means the obsolete state/identity API is removed, not that its gameplay behavior is considered optional. Useful behavior is covered by the current pure and actual Buffer/service suites below. Test counts can decrease because unsupported Run contracts are no longer acceptance requirements.

| Previous tests / behavior | Current coverage or explicit retirement |
| --- | --- |
| StageMapRunState: defensive edge copy | `ChapterGraphPreparationTests.ProgressData_DefensivelyCopiesEdges`; existing pure navigation tests continue to consume immutable command-local facts. |
| StageMapRunState: initial choices, connected path to boss, invalid selection, plan/validate without mutation | Current `ChapterNavigationRulesTests`, `ChapterProgressionCommandsTests`; actual coordinator missing/disconnected, uncleared predecessor, prepare/confirm/recheck tests and boss progression tests in `ChapterGraphPreparationTests`. |
| StageMapRunState: stale/foreign selection-plan owner, execution-kind commit mismatch | Retired owner/revision protocol. Fresh Buffer recheck before confirmation and current cleared-node rejection replace the gameplay protection. No equivalent foreign-owner protocol is introduced. |
| StageMapRunState: invalid depth edge / no incoming edge | Actual generator seed-range assertions check depth+1, every node reachable from start, outgoing paths to the terminal boss, and complete incoming coverage. Buffer storage validation remains in existing StageBuffer tests; graph rules are not moved into Buffer. |
| StageMapRunState: noncombat final-node constructor rejection | Retired old execution-kind constructor. Boss role comes from the current DB stage definition, not that constructor; actual missing-boss preparation and stage/map completion tests remain. |
| StageMapSession: initial View-ready snapshot, query/select/latest snapshot, internal commit boundary | Old Core-built View snapshot projection retired. Current prepare is read-only; explicit confirm records selection after rechecking Buffer. Existing Buffer reading and boundary fact tests replace selection checks. |
| StageMapGenerator: old exact golden signatures | Retired signatures of the unused algorithm. The existing Systems generator has its own Nearest/Random/WeightedRandom policy and is deliberately unchanged. Actual fixed-seed determinism is asserted for all three policies. |
| StageMapGenerator: content validation, two-depth graph, layered DAG, seed variation, nearest/incoming repair and extra-edge branch fixtures | Actual DB missing-boss test, two-depth start-to-boss test, seed-range graph invariants at extra-edge chances 0 and 1, and fixed-seed/reuse tests. No claim that old nearest-neighbor golden layouts equal current layouts; nearest/repair policy validation applies to the current generator. |
| RunRecovery: request read-only, activation preservation, invalid targets | Current prepare/confirm and rule rejection tests preserve selection/history; recovery inputs live in CurrentProgressBuffer, not a separate active Run owner. |
| RunRecovery: bounded healing, Full ignores amount, flat clamping, percentage truncation, int maximum | The five original numeric cases moved to `CharacterRecoveryRuleTests`; no-effect/death/insufficient-funds, percentage-to-zero and soul-adjusted maximum cases also use the current pure calculator. |
| RunRecovery: payment/heal once, result retry, completion handoff | `CharacterBattleSettlementTests.Recovery_IsOneUseAndPaysAndHealsTogether`, stale-currency rejection, three-mode current-max tests; retained Current recovery receipt and main state are the current contract. |
| RunRecovery: unknown option, inactive stage, no-purchase exit | Added actual `ChapterGraphPreparationTests.RecoveryService_UsesCurrentBufferOptions_PaysOnceAndRespectsNextSelection` and `RecoveryStage_NoAvailablePurchaseCanStillExitWithoutChangingHealthOrCurrency`: registered Core Recover service and coordinator, real Buffers, explicit option facts. No production DB option-preparation wiring is claimed. |
| RunRecovery: foreign Run/entry, version overflow, disposed owner, typed old-command windows, sequential Run identity | Retired Run protocol. Current node/stage correspondence, frozen options, one-use receipt, and validation before publication protect current behavior without Run IDs or versions. |
| RunShop: seeded frozen/copy offers and invalid definition pool | Existing `CharacterRosterRuleTests.Shop_UsesLegacySeededOfferSelectionAndValidatesUnselectedCandidates` and `CharacterRosterServiceTests.ShopSelection_PreservesFrozenOffersAndPurchasesUntilSelectionChanges`. |
| RunShop: duplicate slots, free unowned character, once-only payment/grant | Added actual DB-query/service tests for three independent slots at cost 0/2 and a free unowned player-eligible unit; existing repeated-payment test retains soul and instance identity assertions. |
| RunShop: insufficient funds, overflow, unknown slot, valid retry | Existing currency/allocator rejection tests and added unknown-slot test verify no partial state and later valid purchase. |
| RunShop: completion cannot pay/grant again, shop/recovery selection isolation | Settlement does not grant purchases; frozen offer tests preserve receipts until selection changes. Added recovery exit/next-selection tests exercise the same noncombat progression boundary; shop purchases remain separate from completion. |
| RunShop: foreign Run/entry, Run overflow/disposal, sequential typed retry windows | Retired Run owner protocol, not reproduced in current service or Buffer. |
| MainCharacterRunProgression: initial main and frozen draw, deterministic preparation, short/empty decks, catalog eligibility | Existing `CharacterRosterRuleTests`, `CharacterBattlePreparationTests`, `CharacterRosterBattleCycleTests`, `BattlePreparationRuleTests` and definition adapter tests cover current roster identity and preparation. |
| MainCharacterRunProgression: live HP handoff once, previous stage disposal, final boss/death, wallet/deck preservation | Existing command-visible persistent main, stale completion, defeat, boss/map clear, roster persistence and cleanup tests; added multi-chalice settlement/next preparation preserves healed health. No checkpoint copy-back is needed. |
| MainCharacterRunProgression: Run activation provenance, forged/stale entry, mismatch, overflow, disposed old Run/wallet/deck, fresh Run restart | Withdrawn Run owner/admission protocol; current preparation and publication validate actual facts and preserve Buffer on rejection. New-Run-after-death remains explicitly outside this Core slice. Standalone Contracts/deck tests remain where still consumed. |
| ArtifactEffectsIntegration: combined attack/soul bonuses, two chalices, once-only victory receipts | Kept the actual standalone stage attack/preview/victory assertions; replaced Run fixture with immutable stage input. Current persistent HP and temporary-bonus cleanup remain in Buffer settlement tests. |
| ArtifactRunHandoff: wrong Run/entry then once-only healed handoff | Wrong Run/entry protocol retired. Added `MultipleChalices_SettleOnceAndPersistHealedHealthAcrossCleanupAndNextPreparation`; old standalone receipt origin/overheal/copy tests remain unchanged. This does not claim new production artifact receipt projection. |
| ArtifactStageLifetime: preparation failure leaves layout editable, only owning stage releases lease, disposed stage rejects | Kept actual stage lifecycle tests; replaced the Run admission wrapper test with direct inactive-lease rejection, activation and owning stage release. Other legacy stage consumers remain intentionally supported. |
| StageMapViewBoundary: Core-session rendering/selection and stale display | Replaced obsolete session calls with immutable fact/command checks, including fresh cleared-node rejection. Actual selection recheck is separately exercised through the Systems coordinator; production View wiring is not claimed. |

## Verification

The ownership guard first compiled in the proper Core test assembly and failed in four expected cases because the duplicate Domain/Application owners existed. A first focused migration run executed 87 cases: 84 passed, and three new shop fixture cases tried to replace frozen offers. The fixture was corrected to supply its price/unit through the existing test DB query; Buffer behavior was not relaxed.

Focused migration rerun: 87/87 passed. Additional direct recovery-service/no-purchase-exit assertions and a Nearest primary-edge assertion were then added to complete the behavior mapping.

Final Unity 6000.0.73f1 EditMode execution used:

```text
-batchmode -nographics -projectPath D:/My_ProjectWork/Github_desktop/Project_L
-runTests -testPlatform EditMode
-assemblyNames ProjectL.Tests.Core;ProjectL.Tests.Framework;ProjectL.Tests.CombatViewBoundary
-testResults .superpowers/sdd/2026-09-29-character-buffer-effect-migration-plan/task8p/full-final.xml
-logFile .superpowers/sdd/2026-09-29-character-buffer-effect-migration-plan/task8p/full-final.log
```

| Final suite | Passed / total | Result |
| --- | --- | --- |
| Core | 1282 / 1282 | No failures; current source and test assemblies compiled. |
| Framework | 302 / 307 | Same five baseline DB Economy failures below. |
| CombatViewBoundary | 13 / 13 | No failures. |
| Combined | 1597 / 1602 | No newly introduced failures. All 90 Task 8P focused behavior cases in this full run passed. |

The five failures are `ArtifactDataTableTests.Catalog_RegistersOnlyMigratedRulesAndExplicitlyPendingViews` and `DataBaseAssetTests.Catalog_CreatesAllDataBaseGroups`, `CombatStages_ReferenceExistingInGameData`, `EveryStage_HasNodeVisual`, `EveryUnitAndStructure_HasUnitVisual`. Each reports `[CombatStageDetail] Combat stage requires Economy`, matching the pre-Task-8P checkpoint. They were not hidden or fixed through fake DB data. The decreased Core test count reflects removal of explicitly withdrawn Run/session protocols, with useful behavior mapped above.

Static checks: no production source references to the retired map/Run owners; no Framework, Buffer, DB, production View/LifeCycle/Job or assembly-definition diff; `git diff --check` and new-file whitespace checks pass. The only two importer side effects (FMOD catalog and Sequences settings) were restored from exact pre-run byte backups and hash-checked. No production asset/settings changes remain.

A fresh read-only `task8p_final_review` independently inspected the whole Task 8P diff, reference audit, coverage mapping and final XML. It reported no Critical/Important blocking findings. No new map/progression state owner, combat Plan/Job or scheduler was introduced.

## Completion and handoff

Task 8P is complete for its approved Core ownership-retirement scope in the working tree. No commit or push was requested for this slice. View can use the existing service handoff; none of its production call names, Buffer responsibilities or combat execution ownership changed.

This does not mark full Task 8 complete. Actual DB method definitions/schema and production preparation, View-owned combat wiring, remaining legacy combat retirement, deferred profile/multicell decisions, and PlayMode acceptance remain separately gated. Recovery service tests install explicit Current options and generation tests use in-memory DB assets; they do not prove production data authoring or playable integration. No PlayMode or real-player run was performed.
