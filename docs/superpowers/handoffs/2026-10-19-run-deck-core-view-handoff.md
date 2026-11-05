# Run Deck — Core → View Handoff

Date: 2026-10-20 (Asia/Seoul). Rewritten after Phase 3 of the Core refactor (decision record (Git history: `2026-10-19-run-deck-core-refactor-plan.md`) §3, §13–§14). This replaces the 2026-10-19 draft. It is a local document and has not been sent. The code is on `feature/Cheon/refactor/core-system`, uncommitted, on top of `f6a18f3f`. **Finalized at the Phase 4 gate** (decision record §15): full Core and full Framework show no new failure from the refactor.

Separate owner documents:
- Flow/LifeCycle (C1 availability provider and composition): [2026-10-19-run-deck-stage-availability-request.md](2026-10-19-run-deck-stage-availability-request.md)
- DB/content (E2 operating assets, Boot gate, `PlayerLoadoutData.Validate`): [2026-10-19-run-deck-content-e2-handoff.md](2026-10-19-run-deck-content-e2-handoff.md)

## 0. Readiness

| Capability | State |
|---|---|
| Read the committed deck and owned cards, with stats (`CoreGroup.Deck.Read`) | Usable now (fixture DB). On the operating DB it is blocked by the E2 Boot gate. |
| Evaluate a draft (`Evaluate`) | Usable now: canonical order, typed failures. |
| Confirm a draft (`Confirm`) | **Depends on C1.** Until the Flow provider is bound, `Availability.Reason == AuthorityUnbound` and `Confirm` returns `WrongPhase`. |
| Battle participant list | Usable now: `CombatStateReadSink.Current.Membership`. |
| New-Run initial deck `[Akimbo, Cactus Cannon, Shotgun]` | **Depends on E2** assets. Core installs it atomically once the data exists. |
| Editor UI, popup and input | View E1, not implemented. |
| Manual acceptance | E3, not done. |

## 1. Rules View must not re-implement

- **Deck legality and order** belong to `Evaluate`/`Confirm`: exactly one Main, at most four cards, Main in slot 0, Sub relative order kept. Don't count cards, choose a Main or reorder in View.
- **Ownership caps** belong to Core acquisition (shop/roster): one per Main definition, two per Sub category. View shows only the failure reason it receives.
- **Finding "the" Main:** never search `Owned` for `Role == Main`. Several Mains can be owned; only the committed deck, the battle membership or a captured interaction says which one is meant (§5).
- **Writes:** View never writes Buffer state. `IBufferQuery.RunDeckWriteSink` is visible by convention only; do not call it.

## 2. Public API

Namespaces:
- `Ds` = `ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Deck`
- `Cs` = `…CoreSystem.CharacterSystem`

Access: `resolver.GetService(SystemGroupsService.InGame).CoreGroup.Deck` (`Cs.IRunDeckService`). Buffer reads come from `resolver.GetService(BufferSystemKey.Buffer)`.

```csharp
public interface IRunDeckService : IServiceModule        // Cs
{
    DeckReadResult Read();                                // 화면 표시 계약(카드 능력치 포함)
    DeckEvaluationResult Evaluate(DeckEditRequest request); // 규칙 판정만, 능력치 계산 없음
    DeckEditResult Confirm(DeckEditRequest request);       // 확정, 능력치 계산 없음
}
```

| Type (`Ds`) | Fields |
|---|---|
| `DeckReadResult` | `DeckReadStatus Status` (`Ready`, `NotReady`, `StateChanged`)<br>`DeckEditorSnapshot Snapshot` (non-null only when `Ready`) |
| `DeckEditorSnapshot` | `RunId RunId`, `long RosterRevision`, `long DeckRevision`, `RunDeckSnapshot Deck`<br>`IReadOnlyList<OwnedCharacterRecord> Owned`<br>`IReadOnlyDictionary<EntityKey, CharacterDefinitionFacts> Definitions`<br>`IReadOnlyList<OwnedCharacterStatsSnapshot> Stats`<br>`DeckEditAvailability Availability`<br>All from one capture. **Stats are kept** (unlike the 10-19 draft). |
| `RunDeckSnapshot` | `RunId RunId`, `long DeckRevision`<br>`IReadOnlyList<CharacterInstanceId> OrderedIds` (canonical)<br>`CharacterInstanceId MainId` (= `OrderedIds[0]`) |
| `DeckEditRequest(RunId expectedRunId, long expectedRosterRevision, long expectedDeckRevision, IReadOnlyList<CharacterInstanceId> candidateIds)` | Immutable copy; any candidate order is allowed. |
| `DeckRuleState` **(new)** | `DeckReadStatus Status`, `long RosterRevision`, `RunDeckSnapshot Deck`, `DeckEditAvailability Availability`<br>`Deck` and `Availability` are present only when `Ready`. No owned list, no definitions, no stats. |
| `DeckEvaluationResult` | `Request`, `bool Accepted`, `DeckFailureReason Reason`<br>`DeckRuleState Current` — **type changed** from `DeckReadResult`<br>`IReadOnlyList<CharacterInstanceId> CanonicalIds` (only when accepted) |
| `DeckEditResult` | `Request`, `bool Accepted`, `DeckFailureReason Reason`<br>`DeckRuleState Current` — **type changed**: after a commit, the installed deck; on rejection, the fresh state |
| `DeckEditAvailability` | `DeckUnavailableReason Reason`, `bool CanEdit` |

**Result values**

| Result | Value of `Current` |
|---|---|
| Accepted `Evaluate` | The evaluated source: the same `RunId`, `RosterRevision` and `DeckRevision` as the request. |
| Accepted `Confirm` | `Current.Deck` is the **installed** deck. A no-op keeps the same instance and `DeckRevision`; a changed deck has a new instance with a higher `DeckRevision`. `RosterRevision` equals the request. Do not compare the whole `Current` with the old request. |
| Rejection | The current source. `CanonicalIds` is null. |

**`DeckFailureReason`**

| Group | Values | Meaning |
|---|---|---|
| Rules | `EmptyDeck`, `UnknownOwnedId`, `DuplicateOwnedId`, `UnsupportedUnit`, `MissingMain`, `MultipleMains`, `TooManyCards` | The draft breaks a deck rule. |
| Old draft | `StaleRun`, `StaleRoster`, `StaleDeck` | The draft was made against an older state. |
| Phase | `WrongPhase` | Editing is not allowed now; see `Availability.Reason`. |
| Moved source | `StateChanged` | The source changed during the call; read again. |
| Not ready | `NotReady` | No Run or deck yet. |

**`DeckUnavailableReason`:** `None`, `NotReady`, `InactiveStage`, `Stopped`, `Faulted`, `Loading`, `JobPending`, `RequestPending`, `RunEnded`, `BattleRetained`, `InteractionOpen`, `AuthorityUnbound`. The last one is new: the C1 provider is not connected.

**ID meanings**
- `CharacterInstanceId` is one owned card instance; two Subs of the same definition have different instance IDs.
- `EntityKey` is the unit definition.
- `EntityId` is a unit on the battle field.

## 3. Sequence

1. **Open:** call `var read = deck.Read();`.
   - If `Status != Ready`, show the unavailable state: `NotReady` means no Run yet; `StateChanged` means call `Read()` again once.
   - `Read()` throws when a stats table is broken. That is a configuration error, not a reason to retry.
2. **Draft:** keep the draft IDs **and** the snapshot's `RunId`, `RosterRevision` and `DeckRevision` together. When `!CanEdit`, show `Availability.Reason`.
3. **Evaluate on each draft change:** call `deck.Evaluate(new DeckEditRequest(s.RunId, s.RosterRevision, s.DeckRevision, draftIds))`. Show `Reason`; when accepted, `CanonicalIds` is the order Core will commit.
4. **Confirm:** call `deck.Confirm(sameRequest)`.
   - When `Accepted`, `result.Current.Deck` is the committed deck. **Never retry `Confirm`** because a later `Read()` fails or returns `StateChanged`; the commit already happened. To refresh the display, call `Read()` and compare `Read().Snapshot.DeckRevision` with `result.Current.Deck.DeckRevision`.
   - When rejected with `StaleRun`/`StaleRoster`/`StaleDeck`, the draft is old. Show the current state and ask the user to re-evaluate; **do not** silently copy the new versions onto the old draft.
5. **Refresh:** there is no change event.
   - Call `Read()` again when the screen opens or regains focus, and after any shop, recovery or acquisition you observe.
   - Compare `RunId`/`RosterRevision`/`DeckRevision` to decide whether the draft is stale.
   - Hiding the popup never undoes an accepted `Confirm`. A result that arrives for a closed or reopened screen is ignored by the display, not rolled back.

```csharp
var core = resolver.GetService(SystemGroupsService.InGame).CoreGroup;
DeckReadResult read = core.Deck.Read();
if (read.Status != DeckReadStatus.Ready) { /* 준비 안 됨 표시 */ return; }
DeckEditorSnapshot s = read.Snapshot;                     // s.Stats: 카드 능력치(OwnedId로 연결)
var request = new DeckEditRequest(s.RunId, s.RosterRevision, s.DeckRevision, draftIds);
DeckEvaluationResult evaluation = core.Deck.Evaluate(request);
if (evaluation.Accepted && s.Availability.CanEdit)
{
    DeckEditResult result = core.Deck.Confirm(request);
    // result.Accepted → result.Current.Deck.OrderedIds를 표시. 아니면 result.Reason과 result.Current로 갱신.
}
```

## 4. Which Main is meant (R10)

| Need | Source |
|---|---|
| Currently selected Main (StageSelect, HUD outside battle) | `IRunDeckReadSink.TryGetCurrent(out RunDeckSnapshot d)` → `d.MainId`. When it returns `false` (no Run or deck yet), there is **no** selected Main: show nothing. Do not fall back to a role search. |
| Main and participants of the active battle | `CombatStateReadSink.Current.Membership` (`RunDeckSnapshot`; `Current` is null when there is no battle) → `.MainId`, `.OrderedIds`.<br>Captured at entry; it does not follow later deck edits.<br>`CombatStateReadSink.BattleInstanceId` identifies the battle lifetime.<br>`CombatStateReadSink.Current.DrawOrder` is always the participant Subs (each exactly once, no Main). |
| Main targeted by an open Recovery/Shop | `CurrentProgressReadSink.Recovery.MainId` / `.Shop.MainId`, captured when that interaction was entered. |
| Persistent state of a specific Main | `ICharacterBufferReadSink.TryGetMainState(CharacterInstanceId, out MainCharacterPersistentState)` |

**Removed:** `CombatStateData.EntryRosterRevision` had no readers. `CombatStateData.Membership` **stays** and is the only stored copy of the participant list. The 10-19 draft's `CharacterReadSink.BattleMembership` was never shipped.

## 5. Behavior changes that affect View code and tests

| Change | Effect |
|---|---|
| Battle install requires membership (D-3) | `BattlePreparation` without `membership:` (plus the current `rosterRevision`, deck and Run) is rejected. A stale roster, deck or Run is rejected with every store unchanged. |
| Exact battle ID (D-3) | A `CombatCommandInput` whose `battleInstanceId` is not the current battle (including 0) is rejected. |
| Raw character writes in battle (D-1b) | `CharacterWriteSink.Apply` throws while a battle is installed. In battle, changes go through `BattleWriteSink`. Outside a battle it works as before. |
| Main duplicate check moved (R3) | The Buffer no longer rejects a second Main of the same definition. Ownership rules reject it with `OwnershipLimitReached`. `RosterAcquireFailureReason.MainAlreadyOwned` (3) is **no longer produced**; the value stays so existing code compiles. |
| Shop Main offers (D-5) | A Main candidate is offered at level 1 (`CharacterRosterRules.MainAcquisitionLevel`), even when the Sub average is higher. Today no operating shop lists a Main. |

## 6. View call sites to migrate

| File | Today | Required |
|---|---|---|
| `GlobalSystem/UISystem/Screens/Global/DeckCardList.cs` (~L148–162) | Picks the first owned `Role == Main` and lists every owned Sub. | Outside battle, show `RunDeckReadSink` `OrderedIds` (slot 0 is the Main; empty slots stay empty). In battle, show `CombatStateReadSink.Current.Membership`. |
| `GlobalSystem/UISystem/Screens/Global/OwnedCharacterStatsReader.cs` | The `IRunDeckReadSink deck = null` parameter is optional; a missing deck silently makes `MainCharacter` null. | Make `deck` required (R6, staged here because the callers are View code). Update `OwnedStatsUITestServices.cs` (the only caller without it). With no committed deck, `MainCharacter` stays null by design. |
| `InGameSystem/ViewSystem/Lobby/LobbyDepartureText.cs` | Message per `RosterAcquireFailureReason`; it has `MainAlreadyOwned`. | Add `InvalidInitialDeck` (8: initial-deck data invalid; the detail is in `RosterAcquireResult.InitialDeckFailure`) and `OwnershipLimitReached` (7). The `MainAlreadyOwned` branch can go. After that, Core removes the enum value. |
| `InGameSystem/ViewSystem/Dev/DevBattleInitializationSource.cs` | Authors the retired Main key 14. | Switch to Akimbo with the E2 data. |
| `InGameSystem/ViewSystem/Combat/CombatEventAudio.cs` | Maps the retired key 14 to preset 0. | Map Akimbo when E2 lands. |
| Single-argument `BattleWriteSink.PrepareBattle(BattlePreparation)` callers | Kept for compile safety, under the same membership requirement. | Pass a preparation that carries membership. Core removes the overload after the callers move. |
| Tests using `ICharacterRosterService.InitializeRoster` | `CombatEntryIntegrationTests`, `CombatProgressIntegrationTests`, `CombatSceneEventPresenterTests`, `TerrainOperationalDisplayTests` (용근); `ArtifactHealingChaliceIntegrationTests`, `HojinCombatTornadoTerrainAssetTests` (호진). | `InitializeRoster` is obsolete: it creates a roster without a Run ID or deck. Migrate to `CoreGroup.RunStart.StartNewRun(loadoutKey)`. Core removes `InitializeRoster` once these callers are gone. |

**Already done by Core under the D-3 exception** (fixture lines only, for your review):

| File | Author | Lines |
|---|---|---|
| `CombatLogUITests.cs` | 현강 | 2 |
| `MainCharactorUITests.cs` | 현강 | 2 |
| `OwnedStatsUITestServices.cs` | 현강 | 1 |
| `CombatViewSystemTests.cs` | 용근 | 2 |
| `LobbyDepartureIntegrationTests.cs` | 호진 | 1 |

- Each line is the same swap: `buffers.BattleWriteSink.PrepareBattle(prep)` → `CharacterBufferTestData.PrepareFixtureBattle(buffers, prep)`.
- That helper installs a test deck built from the preparation and passes it as membership.
- Assertions and inputs are unchanged.

## 7. Integration checklist (E1)

- [ ] The editor reads `Deck.Read()` only (stats included); it does not compose versions from separate Buffer reads.
- [ ] A draft keeps its original versions; stale results ask for re-evaluation.
- [ ] An accepted `Confirm` is never retried because of a later read failure.
- [ ] `AuthorityUnbound`/`WrongPhase` is shown as unavailable, never as an error-dialog loop.
- [ ] A three-card deck leaves the fourth slot empty.
- [ ] The battle display uses `CombatStateReadSink.Current.Membership`; no role search remains in `DeckCardList`.
- [ ] Old evaluation/confirm results are ignored after hide/reopen; accepted commits remain.
