# Run Deck — C1 Stage Availability Request (Flow owner + Framework/InGame composition owner)

Date: 2026-10-20 (Asia/Seoul). Issued after the Core refactor Phase 1 gate; **finalized at the Phase 4 gate** (no contract change since Phase 1; decision record §15). Local handoff, not sent externally. Decision source: decision record (Git history: `2026-10-19-run-deck-core-refactor-plan.md`) §3.5 (D-4 option a).

**Status: end-to-end C1 is blocked on steps 1–2 below.**
- Core's consumer contract and binding seam are done (Phase 1).
- The operating root passes no provider. So `CoreGroup.Deck.Read()` is Ready with `Availability.Reason == AuthorityUnbound`, and every `Confirm` returns `WrongPhase`.
- This is the explicit unavailable state, not a fallback.
- Under the decision record's merge gate (L3), C1 is **required before merging into a shared branch**. No C1-unbound merge exception is approved.

## 1. Core-owned consumer contract (delivered)

```csharp
// Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CharacterSystem/IDeckEditAvailabilityRead.cs
namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem
{
    public interface IDeckEditAvailabilityRead : IServiceModule
    {
        DeckEditAvailability ReadDeckEditAvailability();
    }
}
```

- `DeckEditAvailability(DeckUnavailableReason reason)`, with `CanEdit == (Reason == None)`. Namespace: `…CoreCaculator.Contracts.Deck`.
- `DeckUnavailableReason` values: `None, NotReady, InactiveStage, Stopped, Faulted, Loading, JobPending, RequestPending, RunEnded, BattleRetained, InteractionOpen, AuthorityUnbound`.
  - `AuthorityUnbound` is Core-only; the provider never returns it.
  - Core checks `BattleRetained` (active battle) and `RunEnded` from Buffer facts **before** asking the provider. The provider owns the rest of the Stage window.
- Returning `null` is a configuration error (`InvalidOperationException`).
- The contract replaces the unimplemented `LifeCycleSystem.LifeCycles.Stage.IStageInteractionRead`. The file moved to Core with its GUID (`5a6ebb775d984af2a6d42810d2a4eec6`). Core has no LifeCycle reference.

## 2. Provider behavior (Flow owner)

Implement the provider at `StageLifeCycle`, or as a narrow reader it owns. It is one composed read of the existing Stage flow facts. Add no independently writable phase booleans. The acceptance contract is plan P4/C1 and design §7.1.

The window is open only when **all** of these hold:
- the Stage lifetime is active and bound;
- the map is prepared, and the Run is initialized and in progress;
- nothing is loading, faulted or stopped;
- the runner has no work, and there is no submitted-but-unregistered map/shop/recovery request (an idle runner alone is not enough);
- no entry, result drain or combat-return drain is pending;
- the current node has no unfinished interaction. Use `CurrentProgressBuffer` and `StageProgressBuffer` for this:
  - a retained, completed Shop/Recovery record is not a lock;
  - a hidden popup is not a completion.

The deck popup's own modal input block is not an interaction lock. Core calls the read once per snapshot/evaluation, and once more immediately before its callback-free publication.

Required tests (`ProjectL.Tests.Framework`):
- `IdleRunner_WithSubmittedUnregisteredMapInput_IsUnavailable`
- `CompletedShopRecord_DoesNotKeepWindowLocked`
- `HiddenUnfinishedInteraction_RemainsUnavailable`
- `RecoveryPendingConsumption_RemainsUnavailable`
- `PendingCombatReturnDrain_RemainsUnavailable`
- `DeckOwnPopupBlock_IsNotInteractionLock`

## 3. Construction, registration, binding and readiness (exact sequence)

Verified from code: `LifeCycleSystemFactory.Create(faults)` only constructs these objects:
- `LifeCycleManager`
- `CombatProgressReader`
- `CombatEntryService(faults, progress)`
- `ArtifactBoardPreparation(faults)`
- `LobbyLifeCycle(faults)`, `StageLifeCycle(faults)`, `ArtifactLifeCycle()`
- `CombatLifeCycle(faults, entry)`

None of them takes an InGame object; every runtime dependency is bound later, in `PostBindService`. **So the LifeCycle composition can be created before InGame.**

| Step | Owner | File / call site | Change |
|---|---|---|---|
| 1 | Flow | `Factory/Runtime/LifeCycle/LifeCycleSystemFactory.cs` | Add `CreateComposition(RuntimeFaultBoundary faults)` returning `{ IExportSystemServices Group, IDeckEditAvailabilityRead DeckEditAvailability }` (mirrors `InGameSystemComposition`). Register the provider **only** in the LifeCycle builder: `AddService` under a new `LifeCycleSystemServiceKey.DeckEditAvailability` (`ServiceKeys/Runtime/LifeCycle/LifeCycleSystemServiceKey.cs`), plus `AddModule` only if it has a lifecycle (FW-001/002). An optional `LifeCycleSystemGroup` property is for other consumers. |
| 2 | Framework (민형) / InGame composition | `Factory/Runtime/RunTimeSystemFactory.cs` `Create`; `Factory/Runtime/InGame/InGameSystemFactory.cs` `CreateComposition`/`Compose` | Call `LifeCycleSystemFactory.CreateComposition(faults)` **before** `InGameSystemFactory.CreateComposition(battleSource, faults, lifeCycle.DeckEditAvailability)`. `Compose` forwards it to `InGameCoreSystemFactory.Create(battleSource, continuationSource, deckEditAvailability)`. Keep the export order Buffer → Global(`inGame.Registration`) → InGame → LifeCycle, and the public `FrameworkRuntimeFactory.Create(battleSource, faults)` signature (`GameBootstrap`, `TestRuner`). |
| 3 | Core (done) | `Factory/Runtime/InGame/Core/InGameCoreSystemFactory.cs` | `Create(IBattleInitializationSource battleSource = null, IChapterContinuationSource continuationSource = null, IDeckEditAvailabilityRead deckEditAvailability = null)` passes the value to `new RunDeckService(deckEditAvailability)`. InGame keeps a **reference, not a registration**, like the shared `RuntimeFaultBoundary`. The parameter stays optional only because other owners' tests call this factory without it. |

Readiness and teardown:
- The runtime runs BindScene/InitData/PostBind/Initialize over every root group before Tick, and Core calls the provider only inside `Read`/`Evaluate`/`Confirm`.
- Until its Stage lifetime is bound and the map is prepared, the provider reports `NotReady`/`InactiveStage`.
- Root shutdown runs LifeCycle → InGame → Global; after LifeCycle shuts down, the provider reports `Stopped`.

## 4. Completion test and merge impact

Done when:
1. The six provider tests pass.
2. A composition test through `FrameworkRuntimeFactory.Create` shows `Availability.Reason != AuthorityUnbound`, and that it opens and closes with the Stage window.
3. The Core suites pass against the real binding: `RunDeckServiceTests`, `RunDeckRuntimeCompositionTests`, `RunDeckIntegrationTests`.
4. Manual: StageSelect → open the deck editor → confirm → leave → enter a shop node → the editor is unavailable while the interaction is open.

Merge impact: required for L3 (shared-branch merge). The View deck editor (E1) cannot be accepted without it.
