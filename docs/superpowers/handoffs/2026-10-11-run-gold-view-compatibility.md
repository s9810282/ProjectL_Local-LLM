# Run Gold: View Compatibility Phase

## Publication update (2026-10-12)

The user reports that the scene entered Play Mode without errors; this is a user-observed smoke result, not verification of every gameplay path. After being informed of the remaining test failures, the user explicitly requested a local commit titled `[천명] 골드 통합 작업`. That request supersedes the earlier no-commit status below for this scoped change only. Include the Core/Buffer migration, View compatibility changes, affected tests, and these handoff/plan updates together. Exclude FMOD asset changes and separate recovery proposal/plan documents. Push and external delivery are not part of this request. The full test suite has not been rerun for the commit; the dated results below remain the verification record.

## Scope and authorization

On 2026-10-11 the user authorized the small View-side changes required by the previously approved Core/Buffer migration. This phase integrates the existing Core candidate into the current checkout, updates its View consumers, and verifies the resulting Unity project. It does not authorize SO/schema/catalog cleanup, scene/prefab changes, FieldCamera or Architecture work, Renown, items, event rerolls, rewards, or new growth rules. No commit or push is performed.

Current checkout: `D:/My_ProjectWork/Github_desktop/Project_L`, branch `feature/Cheon/refactor/core-system`, starting commit `2d0509df`. The old candidate started at `cf6192d1`; only its patch was transferred. The current terrain and piercing work is preserved, including the additional structure-position validation in `CombatStateBuffer`.

## Changes

- The previous Core/Buffer candidate removes manual drawing and stage Gold. `CurrencyBuffer.Amount` remains the single Run Gold balance. Starting values, shop/recovery spending, initial hand, ownership, levels, and souls retain their behavior.
- `CombatViewSystem` obtains `ICurrencyBufferReadSink` through its existing binding path and passes the current amount to `CombatDisplayValues.RunGold`. The display value is a transient read snapshot, not a second authoritative balance. The existing install/result-refresh lifecycle is retained.
- `CombatHud` renders Run Gold. `FormerlySerializedAs` retains both `currencyText` and `stageGoldText` references. The legacy `remainingDrawText` reference is optional and is used only to hide already-authored draw text at runtime. No scene is rewritten.
- `DevBattleInitializationSource` retains database/stage-key/combat-stage validation, but stops querying economy or supplying the retired constructor argument.
- The View command value `8`, draw factory, receiver method, slot forwarding, deployment binding, and router/policy branches are removed. `ReturnToCheckpoint = 9` is unchanged. The input asset itself is untouched: its old draw action is no longer bound to a receiver.
- Generic queue/activation/publication tests use retained commands. Obsolete draw-only tests are retired; constructor consumers, including newly added terrain fixtures, are migrated. The pre-existing `CombatProgressJobTests.Core` fake lacked the current `Results` interface property; only that missing test member was added, following its existing unused-service convention.

## Contract review

SSOT: all Gold comes from the existing currency sink; the HUD caches only rendered values. Initial-hand metadata is retained and is not displayed as remaining manual draws.

SOLID: service binding retains a narrow currency read interface. The display formats provided data; input controllers report intent; Core owns rules. No new service or general economy abstraction is introduced.

Framework: the existing registration/composition and one-build flow are unchanged (FW-001/002/003). No private store mutation, alternate input submission route, or lifetime owner is introduced (FW-004/005/006/007). Existing UI input receiver bindings remain intact.

## Verification

Evidence is stored locally under `.superpowers/sdd/run-gold-view-integration/`.

- Before the change, the actual Unity HUD regression failed as intended: configured Run Gold `75`, displayed stage Gold `30` (`red.xml`). The initial attempt exposed the pre-existing missing `Results` member in a test fake; that compile-only correction preceded the successful failing-test run.
- Additional passing regressions check the remaining balance on reattachment, optional/hidden legacy draw text, invalid retired command value, and the inert legacy input action.
- Actual Unity 6000.0.73f1 compilation succeeded in the current checkout. Full EditMode execution (`final.xml`): **3,877 total, 3,837 passed, 39 failed, 1 inconclusive**. Core: **1,961/1,961 passed**. CombatViewBoundary: **9/9 passed**. Framework: 1,846 passed, 39 failed, 1 inconclusive. Editor tests: 20/20; Addressables documentation example: 1/1.
- Relevant Framework fixtures passed: `CombatViewSystemTests` (64), `CombatInputPolicyTests` (48), `CombatInputReceiverTests` (6), `CombatInputRouterTests` (19), `CombatViewBufferTests` (21), `StageInputTests` (19), `UnityStageInputTests` (15), `BattleDefinitionAdapterTests` (43), `RunGoldLifecycleTests` (2), `ChapterContinuationServiceTests` (37), `ShopStageServiceTests` (93), and `RecoveryStageServiceTests` (80). The new HUD regression also passed; this does not imply every unrelated display fixture passed.
- Of the 39 failures, 30 names also occur in the earlier `cf6192d1` baseline. Nine were absent from that older baseline: camera lifecycle (1), chapter graph lifecycle registration (4), DB table registration (3), and authored wall-feedback casting (1). These are outside this phase's migration behavior, but a clean test baseline at the newer `2d0509df` was not obtained, so they are **not all proven pre-existing**. The full repository suite remains failing; exact names and errors are retained in `final.xml`.
- Independent review found no scoped correctness issues and checked the serialized HUD aliases, legacy text's leaf-object reference, input removal, and preservation of newer Core structure validation. Known Unity-generated asset changes were restored after testing. No SO, scene, prefab, or input-action asset changes remain.
- No Play Mode smoke check was performed. No commit or push was made.

## Remaining owner work

SO/DB economy keys, validation, tables and assets still exist. Their cleanup is a separate owner task; it is not necessary to restore removed Core types. The old input action and authored draw objects can be removed by the asset owner later. Actual scene appearance and play-mode behavior require a smoke check; EditMode success alone does not establish those results.

## View owner handoff: completed work and next actions

**Delivery status:** Prepared for the user to share; not sent externally. Changes are in the current project checkout and are not committed. Another checkout must receive the Core/Buffer changes and View compatibility changes together; copying only the removed Core contracts will break its old consumers.

**Already completed — do not repeat:** Run Gold is read from `ICurrencyBufferReadSink.Amount` and passed as `CombatDisplayValues.RunGold` to the HUD. The development source no longer requires `StageEconomySpec`. Manual-draw command/input code and the related callbacks are removed. Starting-hand selection remains available. The HUD preserves the old Gold field bindings and hides an optional legacy draw label at runtime. Code consumers and related regression tests have been migrated.

**Remaining View actions:**

- [ ] Open the integrated project in Unity 6000.0.73f1 and perform a Play Mode smoke check through the existing Boot/Combat entry path.
- [ ] Confirm the Gold label retains its serialized reference and displays the current Run balance on initial entry and re-entry; entering another battle must not restore the old stage starting Gold.
- [ ] Confirm the legacy remaining-draw label is hidden and the retired draw action cannot submit a command. Check initial hand, placement/return, combat entry, and turn progression still work.
- [ ] Remove obsolete authored draw UI objects, button references, and the unused draw action from the relevant scene/prefab/input assets when taking ownership of asset cleanup. The current runtime code already disables the old draw route; these asset edits are not a prerequisite for script compilation.
- [ ] After all authored references have been removed, remove the temporary optional `remainingDrawText` field and its hide helper together with the associated compatibility test, if no longer needed.

**Separate owner/deferred work:** SO/DB economy tables, schema keys, catalog entries, and asset validation belong to the SO/DB migration, not to this View follow-up. Renown, item purchases/use, event rerolls, artifact enhancement, and new reward rules remain deferred until planning is confirmed. The 39 remaining EditMode failures require separate triage; they are not 39 outstanding script compilation errors.

**Editor-open / Safe Mode expectation:** The integrated source compiled successfully in the actual Unity editor batch run and proceeded to execute all 3,877 tests. No C# compilation errors were found in `final.log`. Therefore the retired Gold/draw API references no longer present a known compilation blocker to opening this checkout. Interactive editor reopening and Play Mode were not tested, so this does not promise an empty Console or rule out separate import, configuration, or runtime errors.
