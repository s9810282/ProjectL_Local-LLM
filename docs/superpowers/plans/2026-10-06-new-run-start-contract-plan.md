# New Run start contract — design and plan

**Date and status:** 2026-10-06. N1+N2 complete (see section 5). Design approved. D1 = 0, D2 = change here (recommendations), D3 = **typed rejection** (user chose against the recommendation). Branch `feature/Cheon/refactor/core-system`, no commits by Claude. Stop and report after every phase.

**Ownership (agreed direction):** the skeleton (LifeCycle Flow/Plan/Job) owns *when* a new Run starts and the screen flow. Core reads, validates and computes the start data. The Buffer owns the reset scope and installs everything atomically. One public Core call must not take over the Flow's responsibilities.

## 1 Current state

| Piece | What it does | Problem |
|---|---|---|
| `IRunProgressBufferWriteSink.ResetRunProgress` (`ProgressTransactions` L126) | Ends the battle and clears the current/stage progress, map clears, roster, Run currency and Run outcome | Leaves an empty roster: an inconsistent "new Run" |
| `ICharacterRosterService.InitializeRoster(loadoutKey)` | Reads `PlayerLoadout`, plans main + 3 starting subs, publishes them | Requires an empty roster and rejects when the Run has ended. Called before a reset, it is wiped by the reset |
| `StagePlan` `JobType.NewRun` | Runs `ResetRunProgressJob`, then `StageJob` (enter chapter) | Nothing registers `NewRun`; it would produce an empty roster and 0 Run currency |
| `StageLifeCycle` | Registers `JobType.Start`, which only enters the chapter | The operational path never initializes a roster, so battle preparation later fails on the missing main state (`CombatProcessingService` L204) |
| Run starting currency | Not in any DB | Run currency is 0 after reset |

Callers of `InitializeRoster` today: the dev `GameBootstrap` path, `TemporaryCoreRuntimeFactory` and tests.

## 2 Design

### 2.1 Data

`PlayerLoadoutData.StartingRunCurrency` (int, ≥ 0) is a new field. `PlayerLoadoutDataSO.startingRunCurrency` provides it, and `PlayerLoadout_1.asset` is set to the D1 value. This is temporary until the Excel DT provides the value; Core reads it only through the loadout query, so the source can be replaced later.

### 2.2 Core

New service `IRunStartService.StartNewRun(int loadoutKey) : RosterAcquireResult`, in `CoreSystem/RunSystem/`, exposed on the Core group:

1. Read and validate the loadout. A missing key returns `InvalidDefinition`, and so does a negative starting currency.
2. Plan the initial roster with the existing `CharacterRosterRules.PlanInitialRoster`. It is planned against an empty roster, because the new Run discards the old one.
3. Publish one `NewRunPreparation(roster requests, initial main, starting Run currency)` to the Buffer.

Allowed when the previous Run has ended, since that is the purpose. An active battle is discarded by the reset, which is today's `ResetRunProgress` behavior. Calling it twice gives the same fresh state twice: there is no double grant, because every call resets before installing.

Shared code: the loadout → roster plan moves into one internal helper used by both `InitializeRoster` and `StartNewRun`, so the loadout rules stay in one place. `InitializeRoster` keeps its current contract for dev and test callers.

### 2.3 Buffer

`IRunProgressBufferWriteSink.PublishNewRun(NewRunPreparation) : RosterAcquireResult` runs in two stages.

**Prepare (nothing changes yet):**
- `CharacterBuffer` gets a new store method, `PrepareInitialRoster`. It is the same as `PrepareAcquire` but evaluated as if the roster were empty. This means no "main already owned" check against the old Run.
- If preparation fails, the method returns the failure and changes nothing.

**Install (cannot fail):**
1. The existing reset scope (end battle, clear progress, map clears, roster, currency, Run outcome).
2. Install the prepared roster.
3. Set the Run currency.

Owned IDs keep increasing across Runs, as today, because `ClearRoster` does not reset the counter.

`ResetRunProgress` stays for existing callers and tests. Its comment will say that a playable new Run uses `PublishNewRun`.

### 2.4 Chapter entry

This stays a separate Flow step, `EnterChapter(startChapter)`, after `StartNewRun`. The reset clears the map selection, so the order must be: start Run → enter chapter.

## 3 Decisions needed

| ID | Question | Recommendation |
|---|---|---|
| D1 ✅ 0 | Temporary Run starting currency in `PlayerLoadout_1` | A planning value. If none is decided, use 0 (today's behavior) and let the planner change the asset |
| D2 ✅ here | The `StagePlan` `NewRun` job is skeleton code. Switch it from `ResetRunProgressJob` to a job that calls `StartNewRun`, then `StageJob`? | Do it here, since the plan is otherwise wrong: it would produce an empty roster. It needs a loadout key; use 1 as a named constant until a Run-setting source exists. Who registers `NewRun` (the title "new game" button) stays with the skeleton/View |
| D3 ✅ rejection | Battle entry with no roster currently throws (`CombatProcessingService` L204) | Keep the throw for now. The flow is fixed by D2; turning it into a typed rejection is a separate contract change |

## 4 Phases

- **N1 Data + Buffer + Core:**
  - Add the `StartingRunCurrency` field and set the asset value.
  - Add `NewRunPreparation`, `PublishNewRun` and `PrepareInitialRoster`.
  - Add `RunStartService` and its group exposure, and extract the shared loadout planner.
  - Tests:
    - roster and currency are installed;
    - an old roster, currency, battle and ended outcome are all replaced;
    - a repeated call does not double grant;
    - an invalid loadout changes nothing;
    - a preparation failure changes nothing;
    - `DataBaseAssetTests` checks `PlayerLoadout_1` validity.
  - Full EditMode.
- **N2 Flow (per D2):** the `NewRun` plan uses the Core start, then enters the chapter; plan tests. Full EditMode.

## 5 Execution notes

**N1 + N2 — 2026-10-06 (done together; one verification run).**

Data:
- `PlayerLoadoutData.StartingRunCurrency` is a new optional last constructor parameter (default 0, negative rejected in `Validate`).
- `PlayerLoadoutDataSO.startingRunCurrency` feeds it, with a tooltip marking it as temporary until the Excel DT provides it.
- `PlayerLoadout_1.asset` is set to `startingRunCurrency: 0` (D1).

Buffer:
- New `BufferSystems/Run/NewRunPreparation` (roster requests, initial main, starting Run currency). It is format-checked: exactly one main request, no nulls, and the currency must not be negative.
- `ICharacterBufferStore.PrepareInitialRoster` is a new store method. It is `PrepareAcquire` evaluated as if the roster were empty; the shared body has an `asEmptyRoster` flag.
- New `IRunProgressBufferWriteSink.PublishNewRun`, implemented in `ProgressTransactions`. It prepares the roster first and returns a failure without changing anything. Otherwise it runs the existing reset scope, installs the roster and sets the Run currency.
- The `ResetRunProgress` doc now says it alone is not a playable Run.

Core:
- `CharacterSystem/PlayerLoadoutRosterPlanner` holds the loadout → `CharacterRosterRules.PlanInitialRoster` logic. It is shared by `InitializeRoster` (unchanged contract) and the new `RunSystem/RunStartService.StartNewRun(loadoutKey)`. `StartNewRun` plans against an empty roster, is allowed after the Run has ended, and publishes `NewRunPreparation`.
- Exposed as `IInGameCoreSystemGroupService.RunStart` (service key `InGameSystemServiceKey.RunStart`), and registered in `InGameCoreSystemFactory`.

D3:
- New `ChapterProgressionFailureReason.RosterNotInitialized`, appended so the existing numeric values are kept.
- `CombatProcessingService.EnterBattle` returns it before reading definitions or changing selection when no main state exists.
- The compatibility `PrepareBattle()` and the internal `CalculatePreparation` guard still throw.

D2 / N2:
- `StagePlan` `NewRun` now runs `NewRunJob(service, StagePlan.DefaultLoadoutKey = 1, priority 0)` and then `StageJob`.
- `ResetRunProgressJob` was deleted; it had no other caller.
- Who registers `NewRun` (title or new-game UI) is still skeleton/View work.

Test doubles updated:
- `CombatPlanTests.FakeCoreService` gained `RunStart`.
- `ChapterResetSpy` implements `PublishNewRun` and counts it as a reset.
- `ChapterGraphPreparationTests.Prepare` now authors a test loadout: main ×1 + Akimbo ×3, starting Run currency 30.

Existing tests adjusted:
- The `NewRun` job tests expect currency 30.
- The defeat → new Run test uses the roster installed by the new Run instead of acquiring a main, and checks that the next new Run installs a fresh roster with new IDs.

New tests:
- `StartNewRun_InstallsRosterAndRunCurrency_AndRepeatDoesNotDoubleGrant`
- `StartNewRun_UnknownLoadout_ChangesNothing`
- `PublishNewRun_FailedRosterPreparation_ChangesNothing`
- `EnterBattle_WithoutRoster_IsRejectedBeforeAnyChange`
- `DataBaseAssetTests.StartingLoadout_PlansNewRunRosterWithValidRunCurrency` (real `PlayerLoadout_1`)

Full EditMode (`.utmp/NewRun-review/full.xml`): **2906 total, 2877 passed, 28 failed**, with the same failed-name set as the T1/G7 run. FMOD catalog restored.

## 6 Voluntary abandonment public API (user request 3) — 2026-10-06

**Problem:** `ICombatProcessingService.AbandonBattle(ArtifactLoadoutSnapshot)` made the caller assemble the settlement input. The skeleton and View cannot build the loadout from the Artifact DB, and a `bool` result did not tell "already completed" apart from "Run ended" or "no battle".

**Change (public contract):** `BattleAbandonResult AbandonBattle()`.

- New `Contracts/Results/BattleAbandonResult`: `Abandoned`, `AlreadyCompleted`, `RunEnded`, `NoActiveBattle`.
- Check order:
  1. An existing completion record returns `AlreadyCompleted`. This includes after `EndBattle`, because the record is kept for result display.
  2. A Run that has ended returns `RunEnded`.
  3. No battle returns `NoActiveBattle`.
- Otherwise Core reads the current Artifact placement through the internal `ReadArtifactLoadout()` and completes the battle as Defeat/VoluntaryAbandonment.
- If the Run ends during the progression check, the result is `RunEnded`.
- A DB definition error (a placed artifact without a definition) is an exception, not a result, and nothing changes.
- Only the completion record is written. No general `CombatResult` is published (sequence unchanged).
- The Flow keeps the order: stop input → present results → abandon → present completion → `EndBattle` → transition. The destination is still the existing on-hold decision.

**Callers:**
- No production caller existed.
- Test callers were updated mechanically: `true` → `Abandoned`, `false` → `Is.Not.EqualTo(Abandoned)`, and the `CombatPlanTests` fakes.
- `ChapterGraphPreparationTests.ArtifactSnapshot` now holds the test artifact in the pool (not placed). The test DB has no artifact definitions, and the snapshot only checks preservation across a Run reset.

**New or strengthened tests:**
- `CoreStageCombatCycleTests.RegisteredCycle_AbandonmentSettlesOnceBeforeCleanup` now checks:
  - `Abandoned`;
  - an unchanged result sequence;
  - `AlreadyCompleted` before and after `EndBattle`.
- `CombatCommonPublicationTests.AbandonBattle_WithoutActiveBattle_ReportsNoActiveBattle`.
- `CombatCommonPublicationTests.AbandonBattle_UnknownPlacedArtifactDefinition_ThrowsAndChangesNothing`.
- The existing Run-ended race test (`CoreStageCombatCycleTests`, operation switch) still returns non-`Abandoned`.

**Not changed:** `ResolveBattle(ArtifactLoadoutSnapshot)` and `FinalizeTurn(..., ArtifactLoadoutSnapshot)` still take a caller-supplied loadout. They are candidates for the same change.

**Verification:** Full EditMode (`.utmp/Abandon-review/full-3.xml`): **2908 total, 2879 passed, 28 failed**, the same failed-name set as the new-Run run. FMOD catalog restored.

## 7 Review fixes — 2026-10-06 (external review, verified before fixing)

Three findings were checked against the code. All three were real, and the first one was introduced by the T1/G7 change.

1. **[P2] A non-attacking unit fails later, at placement.** Core looks up unit definitions through `RequiredProfile` in placement, knockback, damage, turn and AI pathfinding, so every battle unit needs a profile. `ReadAttackProfiles` skipped units with targeting key 0. It now throws at battle preparation instead: "a non-attacking unit cannot enter battle yet".
   - Follow-up: separating definition lookup from attack capability in Core.
   - Test: `BattleDefinitionAdapterTests.ReadAttackProfiles_NonAttackingUnit_IsRejectedBeforeBattleInstall`. `CharacterRosterDatabase` gained a `nonAttacker` option.
2. **[P2] The trait/targeting combination is not checked before battle.** The `PiercingFalloff` combination rule moved from the attack query (`CombatAttackQueries`, which threw at attack time) into the `CombatAttackProfile` constructor. A new public static `SupportsPierce(method)` keeps the rule in one place. The runtime throw was removed because the constructor now guarantees the rule.
   - Test: `PiercingFalloffTraitTests.AttackProfile_RejectsPierceWithIncompatibleTargeting_AtConstruction`.
3. **[P3] Ineffective loadout settings.** `PlayerLoadoutData.StartingHandSize`/`RandomSeed` and the SO fields `startingHandSize`/`randomSeed` were removed. The current rule is kept: the initial draw is `min(3, subs)` and the shuffle seed is the stage `TimelineSeed`. `PlayerLoadout_1.asset` was cleaned. The callers were updated:
   - `TemporaryCoreDatabase`
   - `CharacterRosterDatabase`
   - `InGameDataTableTests`, which now round-trips `startingRunCurrency` instead.

**Results.** Full EditMode (`.utmp/Review3-review/full.xml`): **2911 total, 2882 passed, 28 failed, 1 inconclusive**, with the same failed-name set as the StageLifeCycle run.
- The inconclusive test is `StageMapViewSystemTests.EnterNode_WithoutMainCamera_Throws`. Its precondition assumption ("the test scene must not have a MainCamera") is not met in this batch environment. It is a View test unrelated to these changes.
- The first attempt stopped at CS1729 because `TemporaryCoreDatabase` used a target-typed `new(...)` that a text search missed. It was fixed and rerun.

**Follow-up to finding 3 — 2026-10-06 (user decision).** `startingHandSize` is planner-tunable, so it was restored and connected instead of removed. `randomSeed` stays removed: the shuffle uses the stage `TimelineSeed` so each stage shuffles differently.

**Game rule (user):** only the first hand of every stage is main 1 + sub 3. The loadout value counts the main, so subs drawn = `startingHandSize - 1`. Asset value 4 keeps the current behaviour.

Data path:
1. `PlayerLoadoutData.StartingHandSize` (required constructor argument, ≥ 1) and SO `startingHandSize` (default 4, tooltip explains the rule) feed `NewRunPreparation.StartingHandSize`.
2. `PublishNewRun` calls the new `IRunProgressBufferStore.InstallStartingHandSize`. It is exposed as `IRunProgressBufferReadSink.StartingHandSize` (`int?`), and `Reset` clears it.
3. `CharacterRosterService.PlanInitialDraw` reads that value. When it is null (a roster made without a new Run, as in dev or tests), it uses `InitialDrawPlan.DefaultStartingHandSize = 4`.
4. `InitialDrawCalculator.Plan(candidates, seed, startingHandSize)` passes `startingHandSize - 1` to `InitialDrawPlan(order, initialSubDraws)`, which still caps the value at the deck size.

`InitialDrawPlan` no longer hard-codes 3; the default parameter is derived from `DefaultStartingHandSize`. The asset gets `startingHandSize: 4` back, and the dev/test loadouts pass 4.

New tests:
- `CharacterRosterRuleTests.InitialDrawPlan_DrawsStartingHandSizeMinusMain`: 4 → 3, 3 → 2, 1 → 0, limited by the deck, and 0 rejected.
- `ChapterGraphPreparationTests.StartNewRun_RecordsStartingHandSize_AndFirstDrawUsesIt`: loadout 4 → 3 and 3 → 2 through the real new-Run path.
- `RunProgressBufferTests.StartingHandSize_IsUnsetByDefault_InstalledPerRun_AndClearedOnReset`.

`InGameDataTableTests` round-trips both `startingHandSize` and `startingRunCurrency`.

Full EditMode (`.utmp/HandSize-review/full.xml`): **2918 total, 2889 passed, 28 failed, 1 inconclusive**. The failed-name set is the same as the review-fix run. FMOD restored.
