# Run Start Main Selection — Core API Design

Date: 2026-11-15

Status: Approved on 2026-11-16; Core Phases A through C are implemented and independently reviewed. The View/LifeCycle connection was implemented on 2026-11-17 ([record](2026-11-17-core-review-followup-design.md#2-run-start-main-selection--lobby-connection-implemented)); Unity compilation and lobby PlayMode remain unverified.

Implementation plan: [Phase-based Core plan](../plans/2026-11-16-run-start-main-selection-core-implementation-plan.md).

Scope: Core API and a separate View/LifeCycle handoff.

## 1. Confirmed requirements

- The lobby's existing deck popup will become a main-character selection entry point, implemented by another View programmer.
- Choose Akimbo or Sniper for a **new Run**. Only the main changes.
- Keep the selected loadout's starting sub-character definitions, quantities, starting Run gold, initial deck indices, and starting hand size unchanged.
- Deliver the Core design first. Do not implement View, LifeCycle, scenes, or production assets in this task.

The following are design recommendations, not additional user requirements: pass the selection as an explicit start argument; expose all registered valid player-main definitions; keep an unsubmitted choice local to the lobby presentation session; do not persist that choice across application restarts.

## 2. Pre-implementation code evidence

Paths in this document are relative to the repository root. This table records the inspected starting point before implementation. Current Core delivery and verification status are recorded in §10 and the linked implementation plan.

| Owning source | Observed behavior |
|---|---|
| `Assets/ProjectL/DataBaseSO/InGameGroup/Loadout/Item/PlayerLoadout_1.asset` | Loadout 1 references Akimbo as `mainCharacter`, three sub entries, indices 0–3, hand size 4, and starting Run gold 200. Those are current content values, not new constants. |
| `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/RunSystem/RunStartService.cs` | `IRunStartService.StartNewRun(int loadoutKey)` resolves a loadout, plans a roster, evaluates the initial deck, rechecks captured state, then publishes one `NewRunPreparation`. |
| `.../CoreSystem/CharacterSystem/PlayerLoadoutRosterPlanner.cs` | Reads `loadout.MainCharacterKey`, puts its grant first, then expands the existing sub entries. Calls `CharacterRosterRules.PlanInitialRoster`. |
| `.../CoreSystem/CharacterSystem/CharacterDefinitionAdapter.cs` | Validates UnitData and derives fresh full-health main persistent state from that definition. |
| `.../CoreSystem/CoreCaculator/Contracts/Common/PlayerMainDefinitionPolicy.cs` | Player-main invariants are centralized here; the retired `Unit_MainCharacter` key is not a playable main. |
| `Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/Core/ProgressTransactions.cs` | `PublishNewRun` prepares roster/deck before reset, then installs the roster, Run ID, deck, currency, and hand size. |
| `.../CoreSystem/CombatSystem/DatabaseBattleInitializationSource.cs` | Reads the installed deck's MainId and owned CharacterKey to resolve the player's skill. It is not hardcoded to Akimbo. |
| `Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/RunStart/RunStartJob.cs` | Calls `StartNewRun(DefaultLoadoutKey)` with key 1; rejects a retained battle before the call. Shared by lobby departure and Stage first entry. |
| `Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/LobbyView/LobbyViewBuffer.cs` | `Submit()` currently carries no character selection. A pending request is returned unchanged on duplicate submission. |
| `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Lobby/Deck/LobbyDeckPopupController.cs` | Supplies an empty card list until a production candidate contract exists. |
| `.../ViewSystem/Lobby/Deck/LobbyDeckPresenter.cs` | Card clicks pin details; they do not commit a game selection. |
| `.../CoreSystem/CharacterSystem/RunDeckService.cs` | Edits the deck of an existing Run, with Stage-owned availability. This is a different responsibility from choosing a future Run's main. |

These are source inspections, not Unity execution evidence.

## 3. Approaches and recommendation

| Approach | Tradeoff |
|---|---|
| **Explicit main argument on the existing start service (recommended)** | Reuses one loadout and one creation transaction. The lobby must carry the choice into its departure request. No persistent selection store or extra service is needed. |
| Separate loadout per main | Reuses the existing one-argument API, but duplicates the sub/gold/hand/deck settings that the user explicitly wants to remain shared. Future content edits could make those settings diverge. |
| Core-owned pending selection plus a separate confirm API | Makes selection survive popup closure automatically, but introduces a second state lifetime, reset rules, and implicit dependence of start on prior commands. There is no current requirement for this state. |

The user approved this design on 2026-11-16. Core Phases A through C implemented the first approach and the signatures below; the View/LifeCycle connection remains a separate handoff.

## 4. Implemented public contract

The existing `IRunStartService` exposes the following methods in its existing namespace:

```csharp
RunStartMainOptions ReadMainOptions(int loadoutKey);
RosterAcquireResult StartNewRun(int loadoutKey);
RosterAcquireResult StartNewRun(int loadoutKey, EntityKey mainCharacterKey);
```

The immutable pure-C# DTO is implemented at `CoreCaculator/Contracts/Roster/RunStartMainOptions.cs`:

```csharp
public sealed class RunStartMainOptions
{
    public int LoadoutKey { get; }
    public EntityKey DefaultMainCharacterKey { get; }
    public IReadOnlyList<EntityKey> CandidateKeys { get; }
}
```

The DTO constructor copies its input collection and exposes a read-only collection. It is not registered as a Framework service. Code comments and XML documentation will be Korean; this design and the implementation plan remain English.

### 4.1 Candidate read

`ReadMainOptions(loadoutKey)`:

1. Requires normal DB initialization and service binding, like the start API. Calling too early throws `InvalidOperationException`.
2. Resolves and validates the existing loadout. A missing loadout is a configuration error on this read path (`KeyNotFoundException`, using the existing table contract).
3. Enumerates registered UnitData through `IUnitTableQuery.GetAllUnits()`. Uses validated definitions and the existing `PlayerMainDefinitionPolicy` rather than maintaining a second allowlist or reimplementing its invariants.
4. Returns keys of valid `UnitRole.MainCharacter` definitions, ordered by numeric EntityKey for deterministic presentation. Current production candidates are expected to include Akimbo and Sniper; do not hardcode those two keys.
5. Returns `loadout.MainCharacterKey` as the default. It must be among the candidates; otherwise throw a configuration exception identifying the loadout and key. Do not silently select the first candidate.

This query reads definition data only: it does not acquire characters, install a roster, change the current deck, grant gold, or start/reset a Run. It works before any Run exists. Ordinary non-main definitions are excluded; malformed definitions are configuration errors, not silently hidden candidates.

The list describes main-definition eligibility, **not** a guarantee that the complete loadout or current departure will succeed. Start still validates sub entries, initial deck selection, and captured state. No unlock, ownership, save, or profile restrictions are introduced.

Only keys and the default are needed by the existing lobby card model. Names and icons come from ViewGroup. This version does not introduce a combat-stat preview API or return raw UnitData/SO objects to View. Such a preview would require the appropriate existing Core projection and a separately agreed display contract.

### 4.2 Start semantics and compatibility

- `StartNewRun(loadoutKey)` keeps its current behavior and uses the authored `MainCharacterKey`.
- `StartNewRun(loadoutKey, mainCharacterKey)` uses the explicit main key for this call only.
- Explicit `EntityKey.None`, an undefined/missing key, a regular unit, or a valid enemy-only definition returns `RosterAcquireFailureReason.InvalidDefinition`. Explicit invalid input never falls back to the default.
- A malformed registered definition continues to throw its existing validation exception before publication. Do not convert infrastructure/configuration exceptions into normal invalid-selection results.
- Both overloads reach the same internal planning/publication path. Existing callers and `InitializeRoster(loadoutKey)` retain their current semantics.
- The result type remains `RosterAcquireResult`. Its acquired records identify the main with `CharacterOwnershipRole.Main` and its chosen `CharacterKey`; no redundant selected-main field is required in the result or RunProgressBuffer.
- This is a new-Run command, not an active-Run main-swap command. Successful calls reset the previous Run according to the existing contract, including when the same key is selected again. It is not idempotent; duplicate suppression remains the departure owner's responsibility.
- Retained-battle and scene/phase admission remain the existing Job/LifeCycle responsibility. This change does not add a scene dependency to Core or change the existing low-level reset contract. The View must not invoke start directly from a card click.

### 4.3 Data that stays unchanged

| Source | Treatment |
|---|---|
| `PlayerLoadoutData.MainCharacterKey` | Default only; explicit call overrides its use, without mutating the loadout. |
| `Entries` | Preserve definitions, counts, and grant order. |
| `StartingRunCurrency` | Preserve authored value. This is Run gold; stage combat gold remains stage-owned. |
| `StartingHandSize` | Preserve authored value. |
| `InitialDeckGrantIndices` | Preserve indices and run existing `DeckRules.EvaluateInitial`. Grant 0 now refers to the selected main. |
| Selected UnitData | Supplies the new main's own base health, attack, defense, category and later combat definitions/traits/skill. No stats or progression are copied from Akimbo or the previous Run. |

## 5. Internal execution and state ownership

Extend `PlayerLoadoutRosterPlanner` with an internal overload that accepts an explicit effective main key. Keep current overloads delegating to the common implementation with no override. An internal nullable key is acceptable to distinguish omission from an explicitly invalid `None`; the public API uses the two distinct overloads above.

The implementation must preserve this sequence:

1. Capture the existing `RosterSourceVersion`, currency, and Run outcome as the current service does.
2. Resolve the loadout once and choose the effective main key: explicit argument or authored default.
3. Read the main through `CharacterDefinitionAdapter`, expand unchanged sub entries, then call the existing initial-roster rules. Put the main grant at index 0 and use `rosterIsEmpty: true` for the replacement Run.
4. Evaluate unchanged initial deck indices against the actual planned grants and definitions using `DeckRules.EvaluateInitial`.
5. Recheck the same captured state immediately before publication. Return `InvalidRequest` on the existing stale-source condition.
6. Publish one `NewRunPreparation` using unchanged `RunProgressWriteSink.PublishNewRun`. Preserve its prepare-before-reset behavior.

Do not clone an SO, temporarily modify its main field, copy a loadout into a mutable store, introduce a second roster builder, or reset buffers before successful validation. `CharacterRosterRules`, `DeckRules`, `CharacterDefinitionAdapter`, and the existing Buffer transaction remain the owners of their current rules and state.

| Fact | Owner |
|---|---|
| Main eligibility and initial main state | Validated UnitData + existing Core policy/adapter |
| Shared starting composition and economy | Existing loadout |
| Unsubmitted lobby choice | One View-owned lobby-session draft; not gameplay state |
| Submitted choice | Immutable payload of the existing departure request (future View/LifeCycle integration) |
| Installed current Run's main | Existing CharacterBuffer records and RunDeckBuffer MainId |
| Creation and failure result | Existing start service and Buffer publication transaction |

There is no new Core `SelectedMain`, pending-selection Buffer, selection event bus, or selection save file.

## 6. Framework, SOLID, and composition review

- **SRP:** RunStart owns preparing a new Run; RunDeck retains active-Run deck editing; the popup owns presentation. Choice lookup is definition input for the same start use case.
- **OCP:** Valid registered player-main content appears without a new switch on Akimbo/Sniper. Existing definition policies still constrain it.
- **LSP:** Preserve the old overload's behavior and results. Update any test doubles implementing `IRunStartService` when the interface expands; do not quietly change fallback semantics.
- **ISP:** The existing small RunStart contract gains one read and one explicit command overload. Do not add unrelated deck editing, unlocks, or stat-preview methods.
- **DIP:** Consumers continue to depend on `IRunStartService`; Core depends on existing DB queries and Buffer contracts, not MonoBehaviours or lobby classes.
- **SSOT:** The loadout remains the sole source of shared starting settings. A submitted request is intent; the installed main is derived from the existing roster/deck, not duplicated in a new state store.
- **FW-001/002/003:** Preserve `RunStartService.ExportServices` service/module registration and the existing InitData/PostBind lifecycle. The DTO adds no module.
- **Composition trace:** `InGameCoreSystemFactory.Create` constructs the service → its `ExportServices` registers it in the Core builder → `InGameCoreSystemGroup` obtains the service using `InGameCoreSystemServiceKey.RunStart` → callers use `InGame.CoreGroup.RunStart`. Do not re-export it as a new root/global service. No Core factory/key/group changes are expected for the interface extension itself.

## 7. View and LifeCycle boundary

Detailed integration instructions are in [the handoff](../handoffs/2026-11-15-run-start-main-selection-core-view-handoff.md). That document describes future work; it is not evidence that the screen or transport is connected.

The intended call sequence is:

```text
Lobby opens selection → Core.ReadMainOptions(loadoutKey)
View displays candidate keys through ViewGroup and keeps one draft choice
Departure → existing input Buffer snapshots the chosen key
LobbyPlan → existing RunStartJob → Core.StartNewRun(loadoutKey, chosenKey)
Existing result/transition path → installed roster and deck
```

The API can be implemented and verified independently. Production behavior remains the authored default until the View/LifeCycle owner changes the current parameterless departure transport and fixed-argument Job call. Core API delivery must not be reported as end-to-end lobby selection completion.

## 8. Change, understanding, and verification scope

| Scenario | Before | After this design is implemented |
|---|---|---|
| Choose Sniper for one departure | Change shared loadout content; start still has no per-request key. Must understand content impact on every caller. | UI submits a key; Core resolves it for that call. Understand options, command/result and departure lifetime contracts. Verify selected grant, unchanged sub/economy settings, and rejection behavior. |
| Adjust shared starting gold/subs | Edit one loadout today. Separate-loadout workaround would create multiple copies to keep aligned. | Still edit one loadout. Verify both main selections against those same settings. |
| Restyle the selection screen | Popup currently has empty content and detail-pin behavior. | Change the View presenter/display and normal scene composition only. Core internals do not need edits; verify input, selection and departure payload at the boundary. |
| Add another valid player main | Definition/content and combat integration are required; no lobby options contract. | Same content/skill/visual obligations plus options/read/start checks. No new selection switch or duplicated loadout. |

Expected Core edits: `RunStartService.cs`, `PlayerLoadoutRosterPlanner.cs`, one new options DTO with `.meta`, and focused Core/CoreIntegration verification where permitted. Existing Core registration should remain sufficient. No changes to `RunDeckService`, Buffer publication, loadout SO schema/assets, battle calculations, or Framework are expected.

Necessary shared regression boundaries remain: initial roster rules, initial deck indices, new-Run reset/publication, main-specific battle definition/skill resolution, and compatibility of all one-argument callers. The new boundary does not justify skipping these checks. Unrelated shop/recovery pricing, UI layout, camera, and rendering behavior are not implementation targets.

## 9. Acceptance and verification plan

The original design task performed no tests or Unity runs. Core implementation and focused verification have since completed; §10 and the implementation plan distinguish those results from the remaining View work. Further Core test additions must respect repository ownership and the user's View-test restrictions; no View tests or combat-View MSBuild runs are authorized. Existing tests owned by someone else must not have their assertions/behavior repaired without asking.

Use test-owned tables/fixtures rather than mutable production SOs for automated rule/integration evidence. Acceptance criteria:

1. Read options before a Run exists: valid player mains only, deterministic order, authored default, immutable returned collection, no Buffer changes.
2. Missing loadout, invalid default, uninitialized service and malformed definition: documented read errors, no silent fallback or partial state.
3. Default one-argument start remains unchanged; explicit Akimbo and explicit Sniper each install exactly the chosen main with fresh initial state.
4. For both selections, preserve sub keys/count/order, starting gold, hand size and deck grant indices; installed deck MainId points to the selected owned record.
5. Explicit None, unknown key, regular unit and enemy definition fail without changing prior Run identity, roster, deck, currency, progress, retained battle or item state.
6. Existing malformed sub/initial-deck failures and stale-state rejection still occur before reset; preserve `InvalidInitialDeck` and its detailed reason.
7. A second successful start creates a fresh Run and resets prior main progression using the selected definition. It does not replace a character inside an ongoing Run.
8. Both mains reach real database battle initialization with the selected main's corresponding skill and traits. Verify no former main stats/skill/progression leak into the new one. Phase C provides focused runtime evidence for these paths, including a real skill command for each main.
9. Verify roster/deck rules, Run reset/economy, main definition policy and player skill lifetime. The user authorized focused regressions for Phase C and final review, and requested only related tests for review follow-ups. Retain the Phase 0 full-suite baseline separately; focused passes do not imply that all existing suite failures are resolved.

View owner acceptance separately covers candidate display, popup close/reopen draft retention, immutable pending choice, duplicate departure, cancellation/rejection/retry, retained-battle admission, successful transition, and old default callers.

If Unity is later run, use the approved execution path with AppData access. Do not treat the known sandbox CurlRequestCache failure as a Unity-version or corrupt-cache issue.

## 10. Implementation, review and remaining work

- Confirmed: only the main changes; shared starting subs/gold remain authored once; View integration belongs to another worker.
- Approved on 2026-11-16: the API and draft lifetime above. Core Phases A through C are implemented; production changes are confined to the options DTO, start service and roster planner. The existing state owners, initialization rules and registration remain in use.
- Independent builder review on 2026-11-15 found no important unanswered questions preventing an implementation plan. This is a design review, not implementation or runtime verification.
- Final independent source review found no blocking code defects. The fresh focused Unity run passed all 150 selected cases with zero failures, skips or compiler errors. Evidence: `C:/CodexHome/visualizations/2026/11/15/01a1df8f-77f9-7783-a68b-713b9f9e1cd9/main-selection-final-review/final-focused.xml` and `final-focused.log` in the same directory.
- The Phase 0 full-suite baseline had 36 failures and 14 diagnostic skips; that suite was not rerun for final review. Its detailed record remains in the implementation plan.
- Review follow-up completed on 2026-11-16: `StartNewRun_NonDefaultHandSize_PreservesAuthoredValue` verifies starting hand size 2, distinguished from the default 4, for both mains and both start overloads while preserving the four-card deck and starting gold. All 30 cases in `RunStartMainSelectionTests`, including the four new cases, passed in Unity 6000.0.73f1 with zero failures, skips or compiler errors (Editor exit 0). Only this fixture was rerun for the follow-up; no production code changed. Evidence: `C:/CodexHome/visualizations/2026/11/15/01a1df8f-77f9-7783-a68b-713b9f9e1cd9/main-selection-review-followup/hand-size-focused.xml` and `hand-size-focused.log` in the same directory.
- Remaining implementation belongs to View/LifeCycle: popup display/input, a concrete session draft owner and injection/lifetime map, immutable departure payload, and Job forwarding. PlayMode and visual acceptance remain unverified. Core API completion does not mean that lobby main selection is connected.
