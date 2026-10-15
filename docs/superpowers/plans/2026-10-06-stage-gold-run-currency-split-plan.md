# Stage gold / Run currency split — design and plan

**Date and status:** 2026-10-06. Design approved; D1–D3 decided by user (all recommendations). Branch `feature/Cheon/refactor/core-system`, no commits by Claude. Stop and report after every phase.

**Rule source:** `docs/superpowers/handoffs/2026-10-05-combat-core-requests-phase6b.md` §1.1 (user-confirmed game rules). Stage gold pays manual draws only; Run currency pays shop and recovery only; neither affects the other. Stage gold starts from the stage's economy `InitialGold` on each new stage, persists across phase changes and scene re-entry in the same stage, and ends with the stage. Run currency starts at an explicit new Run and persists across stages.

## 1 Current state (problem)

- One balance, `CurrencyBuffer.Amount`, is shared by draw (`CombatProcessingService` L446 → `CombatDrawCalculator`; `BattleTransactions.Apply(transition)` installs `Draw.ExpectedGold/FinalGold` into the currency store), shop (`CharacterShopService` L160/L284), recovery (`CombatProcessingService` L410), the combat HUD (`CombatViewSystem` L590) and the global UI (`MainCharactorUI` L88).
- `StageEconomyData.InitialGold` is read into `StageEconomySpec.InitialGold`, but `BattlePreparationCalculator` (L147) passes only `ManualDrawGoldCost`. Nothing installs the stage's starting gold in production; only the dev `TemporaryCoreRuntimeFactory` (L57) writes it into the shared balance with `SetAmount`.
- There is no Run starting currency in any DB.

## 2 Design

**Stage gold lives in the battle state.** Its lifetime is exactly the battle's: created by `PrepareBattle`, kept across phases and scene re-entry because the battle state is kept, removed by `EndBattle`. Storing it in `CombatStateData` gives one owner and reuses the existing draw settlement path, which already applies `ExpectedCursor → FinalCursor` and `ExpectedNextEntityId → FinalNextEntityId` to the battle state in `CombatStateBuffer.PrepareCommand`.

| Change | Where |
|---|---|
| `CombatPreparation.InitialStageGold` (int, 0 when the stage has no economy; non-negative) | Contracts `BattlePreparation.cs` |
| `BattlePreparationCalculator` passes `initialization.Economy?.InitialGold ?? 0` | Application |
| `CombatStateData.StageGold`: set from the preparation; after a draw, `draw.FinalGold`; otherwise unchanged | Buffer `CombatStateData.cs` |
| `PrepareCommand` checks `draw.ExpectedGold == _current.StageGold` together with the cursor and ID checks (stale-input check) | Buffer `CombatStateBuffer.cs` |
| `BattleTransactions.Apply(transition)` no longer touches the currency store; `BattleTransactions` drops `ICurrencyBufferStore` | Buffer `Core/` |
| Combat facts read `battle.StageGold` instead of `CurrencyReadSink.Amount` | `CombatProcessingService` L446 |
| `CombatDrawSettlement.ExpectedGold/FinalGold` keep their names; doc comment says "stage gold" | Contracts |

**Run currency stays in `CurrencyBuffer`.** Shop and recovery already use it; nothing changes there except a doc comment that it is the Run currency. The interface name is kept to avoid churn in UI and View code (D3).

**Run starting currency:** not in this plan. It needs a DB value (Excel DB pending, see `handoffs/2026-10-06-excel-db-reply-to-minhyeong.md` §3). Until then a new Run starts at 0, as today.

## 3 Decisions needed

| ID | Question | Recommendation |
|---|---|---|
| D1 ✅ | Combat HUD gold (`CombatViewSystem` L590, View code) must show stage gold. Change it here, or hand off to the View worker? | Change it here (remove the `ICurrencyBufferReadSink` field/argument, read `_state.Current.StageGold`), and record it in a View share note. Otherwise the HUD shows the Run currency during battle |
| D2 ✅ | Dev `TemporaryCoreRuntimeFactory` writes stage `InitialGold` into the Run currency. Remove it? | Remove. Stage gold now arrives through battle preparation. Dev Run currency then starts at 0 like production (shop/recovery in the dev runtime need cost 0 or a later Run starting value) |
| D3 ✅ | Rename `CurrencyBuffer`/`ICurrencyBuffer*` to `RunCurrency…`? | Keep the names, add doc comments. A rename touches service keys, UI and View for no behavior gain |

## 4 Phases

- **S1 Core/Buffer split.** Section 2 changes; update tests that funded draws through `CurrencyWriteSink.SetAmount` to use the stage economy `InitialGold` instead; new tests: stage gold starts from `InitialGold`, a draw lowers only stage gold, shop/recovery change only Run currency, a stale `ExpectedGold` is rejected, `EndBattle` removes stage gold and the next battle starts from its own `InitialGold`. Full EditMode.
- **S2 View HUD and dev factory** (per D1/D2). Full EditMode.
- **S3 Documents.** Mark request 3 in the Core request document as implemented for stage gold (Run starting value still pending); View share note for the HUD change.

## 5 Execution notes

**S1 — 2026-10-06.** As designed:
- `CombatPreparation.InitialStageGold` is a new last optional parameter (default 0, negative rejected), so existing call sites compile unchanged.
- `BattlePreparationCalculator` passes `Economy?.InitialGold ?? 0`.
- `CombatStateData.StageGold` comes from the preparation, becomes `draw.FinalGold` after a draw, and is otherwise copied.
- `CombatStateBuffer.PrepareCommand` rejects a draw whose `ExpectedGold` differs from `StageGold`, checked together with the cursor and ID; the message names stage gold.
- `CombatProcessingService` builds combat facts from `battle.StageGold`.
- `BattleTransactions` no longer prepares or installs the currency store for draws and no longer receives `ICurrencyBufferStore`. `StageServiceTransactions` keeps the store for shop and recovery.
- Doc comments: `ICurrencyBufferReadSink` is the Run currency, `ICurrencyBufferWriteSink` lists purchase/recovery only, and `CombatDrawSettlement` gold is stage gold.

Tests: `CombatCommonPublicationTests` now funds draws with the stage economy, `Create(..., stageGold = 10)`. Run currency is set to a distinct 50 and asserted unchanged through draws, rejections, previews, repeated nights and battle end. The insufficient-gold case now proves that a large Run currency does not enable a draw. Two new cases:
- `StaleStageGold_DrawIsRejected_AndNothingInstalls`
- `StageGold_EndsWithBattle_AndNextBattleStartsFromItsInitialGold`

Full EditMode (`.utmp/S1-review/full.xml`): **2900 total, 2871 passed, 28 failed, 0 skipped**. The failed-name set is identical to the post-merge G1 run (`.utmp/Graph-review/full.xml`): 16 inherited from E10 plus 12 merged UI/render/audio/boot failures shown to be independent of Core changes. FMOD catalog restored to its pre-run state (SHA verified). No other test depended on the shared balance for draws.

**S2 — 2026-10-06.**
- **D1, combat HUD.** `CombatViewSystem.ReadDisplayValues` passes `_state.Current.StageGold` as the HUD gold. The `ICurrencyBufferReadSink` field, the `BindServices` parameter and the `using` were removed rather than kept as a compatibility path, following the user's preference. Callers updated: the factory binding in the same file, plus the test entry points in `CombatViewSystemTests`, `CombatDisplayReuseTests` and `CombatMovementViewTests`. `Install_PassesBufferValuesOnce_ThenBindsInput` now prepares a battle with `initialStageGold: 30` while Run currency is 75, and asserts that the HUD shows 30. The global `MainCharactorUI` still shows the Run currency (unchanged, correct).
- **D2, dev factory.** `TemporaryCoreRuntimeFactory` no longer writes the stage `InitialGold` into the Run currency, so the dev Run currency starts at 0 like production. The three dev-runtime fixtures fund it in their own setup with `TemporaryCoreRuntimeTests.StartingRunCurrency = 50` (a test-preparation direct write, B9b). The same applies to the second runtime in the isolation tests.

Full EditMode (`.utmp/S2-review/full.xml`): **2900 total, 2871 passed, 28 failed, 0 skipped**, with the failed-name set identical to S1. The one `CombatDisplayReuseTests` failure (`HudFeedback_ClearAndNewVisibleLifetime_InvalidateCachedValues`) is already in the S1/G1 sets. FMOD catalog restored (SHA verified).
