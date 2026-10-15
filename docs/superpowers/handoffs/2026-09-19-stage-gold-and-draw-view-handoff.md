# Stage Gold and Character Draw — View Handoff

## Status and ownership

Core Tasks 1–7 and the Stage Gold extension are implemented. Unity/SO assets, automatic earning rules, and UI wiring are **not implemented**. Core owns Run deck contents, Stage shuffle/cursor, initial grants, living-Sub limits, current Gold, affordability, and draw/payment commits. Unity authors immutable inputs, manages prepared Stage ownership and command lifetime, and displays Core snapshots.

Follow-up status: `RunSessionHost` and `StageSelectSceneInstaller` now pass the initial deck, full character catalog, and explicit non-combat spec to Core; current Unity/test compile consumers are updated. Assign `PlayerLoadoutSO.CharacterCatalog` to the complete UnitRoster in production assets. The legacy Unity deck and Stage preparation path remain: finish Tasks 8 and 9 before claiming play readiness. Do not restore an implicit empty-deck fallback. See the [Shop handoff integration status](2026-09-19-sub-character-shop-core-handoff.md) for the narrow Host fix and its 15 passing Unity tests.

All paths below are relative to the repository root.

## Actual Core API

Namespaces: `ProjectL.Core.Contracts.Commands`, `.Snapshots`, `.Results`, `ProjectL.Core.Application.Run`, and `.StageRun`.

```csharp
new RunProgressionService(runId, map, mainSpec, deckSpec, characterCatalog, nonCombatSpec, initialBalance);
// initialBalance는 Run 재화이며 Stage 골드가 아니다.

new StageEconomySpec(initialGold, manualDrawGoldCost: 0);

StageServices services = StageSessionFactory.Create(
    config, units, structures, mainEntry, request.Draw, economySpec);

CharacterDrawResult drawResult = services.Draw.TryDraw();
StageGoldResult goldResult = services.Gold.TryCredit(amount);
StageEconomySnapshot economy = services.Gold.GetSnapshot();
StageSnapshot snapshot = services.Query.GetSnapshot();
```

- `StageEntryRequest.Draw` is required and frozen by Core Run progression. Use that exact input during preparation; do not re-read the live deck or shuffle/filter entries in Unity.
- The full Stage factory requires **six arguments**, including non-null `StageEconomySpec`. There is no five-argument compatibility overload. It creates the Main, grants initial Subs, and produces the completed initial snapshot before returning.
- `StageEconomySpec.InitialGold` and `.ManualDrawGoldCost` are immutable nonnegative `int` values. Only the draw cost has a default argument, 0. Starting Gold must be supplied explicitly.
- `StageSnapshot.Economy.Gold` / `.ManualDrawGoldCost` are immutable display values. `StageSnapshot.Draw.CanManualDraw` / `.ManualDrawFailureReason` remain the sole gameplay availability source for the draw button.
- Draw results retain `Succeeded`, `FailureReason`, `DrawnEntityId`, `DrawnKey`, nullable `ReserveSlotIndex`, and `CurrentStage`. Gold results expose `Succeeded`, `FailureReason`, and `CurrentStage`. Publish the returned full snapshot after either success or a normal rejection; crediting Gold may change draw availability.
- `Gold.TryCredit(amount)` is allowed during Deployment or Combat while the Stage is in progress. Negative amounts return `InvalidAmount`; exceeding `int.MaxValue` returns `Overflow`; zero is a successful no-op. Ended stages return `StageEnded` before arithmetic checks. Reads remain available until disposal. Both Gold and Draw APIs throw `ObjectDisposedException` after disposal.
- Credits are not idempotent: two successful positive calls grant twice. A future reward owner must decide when/how often to call. Do not add kill/day/win hooks in this migration. No public standalone spend API is needed for draws.

## SO authoring boundary — to implement in Unity

Create `Assets/Scripts/ProjectL/Unity/Data/ScriptableObjects/Stage/StageEconomyConfigSO.cs` containing only authored `InitialGold` and `ManualDrawGoldCost` values, both nonnegative. Add one profile reference to the existing `StageDefinitionSO`; do not duplicate those scalar fields or add override flags there. Shared settings may share a profile, while deliberately different stages reference different profiles.

During `StageEntryDataBuilder` preparation, copy the profile into `new StageEconomySpec(profile.InitialGold, profile.ManualDrawGoldCost)`. Carry that immutable input in `StageEntryData` to `StageSessionBuilder` and the full Core factory. Validate missing profiles and assign production assets explicitly. Never keep current Gold in an SO, View model, static field, or Run Wallet. Asset edits must not alter an already prepared/active Stage.

A new Stage creates a fresh balance from its profile; phase changes do not reset or add Gold. Starting balances are content tuning values, not a new hardcoded Core default. Currency earning policies remain outside scope.

## Task 8–10 integration checklist

| Area | Required change |
|---|---|
| `Unity/Data/ScriptableObjects/Unit/PlayerLoadoutSO.cs`, `Unity/Adapter/Unit/PlayerLoadoutAdapter.cs` | Core input conversion and the full-roster reference are implemented; assign that reference in assets. The adapter maps Main-inclusive hand 4 to initial Subs 3 and permits short decks. Remove the legacy loadout/selection restrictions as part of combat migration. |
| `Unity/Runtime/Run/RunSessionHost.cs`, `Unity/Setup/StageSelect/StageSelectSceneInstaller.cs` | Actual constructor, one-time initialization, and initial-input recreation are implemented. Migrate consumers to `Deck.GetSnapshot()` and remove mutable Unity deck ownership. |
| `Unity/Setup/Stage/StageEntryPreparation.cs`, `StageEntryDataBuilder.cs`, `Unity/Data/Runtime/StageEntryData.cs`, `Unity/Runtime/Stage/StageSessionBuilder.cs` | Carry `request.Draw`, Main entry, and economy input into one Core factory call. Remove the Unity player selection/shuffle and reserve creation loop. Direct test entry must also supply explicit Draw and economy inputs. |
| `Unity/Data/Runtime/PreparedStageEntry.cs`, `Unity/Runtime/Stage/StageViewModule.cs`, `StageRuntimeFactory.cs` | Transfer the same prepared Services once. The Main already exists: do not recreate it or fall back to the old manual-initial-generation flow. Preserve existing allowed Main repositioning rules. |
| `Unity/Runtime/Stage/StageSessionHost.cs`, `IStageDrawCommands.cs`, `StageUIContracts.cs` | Route a dedicated Draw command through existing activation, input blocking, reentrancy, terminal and command-fault guards. Call Core once, then publish `result.CurrentStage`. Keep combat-command enums separate. |
| Gold credit boundary | If exposing credits through the Host for future game systems, use the same command/lifetime guards and publish the returned snapshot. Do not expose a mutable balance or wire arbitrary UI rewards. No earning trigger is required now. |
| `StageUISnapshotFeed` / existing UI binding | Display Gold and price from `Economy`; use Core draw availability. Gold credits and draws must update the same snapshot feed. |
| Obsolete Unity deck path | After migrating all consumers/tests, remove `PlayerLoadoutState`, `PlayerSquadSelectionBuilder`, `PlayerSquadSelection`, and `StageEntryData.PlayerUnitKeys`; do not retain a second ownership source. |

The planned `IStageDrawCommands`, economy SO, adapter, and Host credit wrapper are **Unity work**, not APIs already provided by this commit. Once consumers migrate, coordinate the **Core-owned Task 9 access changes**: make raw two/three-argument Stage factory overloads and `Setup.CreatePlayerReserveUnit` internal. They remain temporarily public for existing consumers and cannot provide valid Run activation provenance.

## Rules the View must not reimplement

- Initial day: one Main plus up to three Subs, always free; a deck of 0–2 Subs is valid. Later days do not automatically draw.
- Main remains on the field; living Subs are recalled by the existing Core transition. Capacity counts living player Subs across Reserve **and** Combat; Main is excluded. Three Subs allow one more, four surviving Subs block the next day even if reserve slots are empty.
- Draw failure priority: `StageEnded`, `WrongPhase`, `FirstPreparation`, `DeckEmpty`, `LivingSubLimitReached`, `ReserveFull`, `InsufficientGold`. Disposal throws before this list. Definition/trait/invariant failures are exceptions, not ordinary insufficient-funds results.
- Core prepares unit/ID/slot/cursor and payment before committing. Never call a separate spend and then draw. Normal failures preserve Gold, cursor, entities, IDs and slots. Run deck and Run currency are unchanged on either successful or failed draws.
- Snapshot construction or subscriber failure **after commit** does not undo the command. Use the Host's fault path; do not retry the draw/credit, refund automatically, or route an old Stage port to a new Stage.
- A rejected activation does not refund deck copies: preparation never consumed Run ownership. Dispose the prepared Stage through its current Unity owner. Dispose invalidates retained Gold/Draw services; release must also invalidate retained Host ports.

## Minimal integration acceptance

1. Compile the migrated Unity and affected StageComposition/CombatViewBoundary assemblies; do not treat the Core-only build as Unity validation.
2. Through actual SO→Run→Stage wiring, verify Main + Sub3 (and a short deck), reserve capacity 4, authored initial Gold and price. Reusing the same SO in two Stages must not share balances.
3. Configure initial Gold 10 and price 4: the initial grant leaves 10; a permitted manual draw leaves 6 and adds exactly one Sub; no further draw is allowed at four living Subs. A zero-cost profile still works with zero Gold.
4. With Gold 3 and price 4, show `InsufficientGold`; an explicit credit of 1 updates affordability through the same feed. A rejected draw must not change Gold or units.
5. Check stale prepared-deck rejection, cancelled preparation disposal, one-time service adoption, and an old command port after release. UI delivery exceptions must never duplicate a debit or credit.

Core verification: 58 focused/regression cases passed (7 Gold cases plus existing Draw/Run/Deck cases); actual Core/test assemblies built with zero warnings/errors. The combined-source harness has two pre-existing DeploymentZonePolicy warnings. Unity compilation, SO assets and scene/play checks remain the View integration owner's work.
