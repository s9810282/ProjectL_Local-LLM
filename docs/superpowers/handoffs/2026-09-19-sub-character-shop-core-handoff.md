# Sub-Character Shop — Core Handoff

> 과거 기록 (2026-10-06 정리): 아래 내용은 Framework/Buffer 전환 전 구현이며 현재 구현 지침이 아닙니다. 삭제된 계획·설계 원문은 Git 커밋 `606fdf4c`에서 확인할 수 있습니다. 현재 구조와 API는 [Framework 개발 가이드](../../../Assets/ProjectL/Script/Systems/Documentation/framework-programmer-guide.md)와 실제 코드를 기준으로 확인합니다.

Date: 2026-09-19

Status: Core T1–T5 implemented and verified. A follow-up fixes Host initialization and map/content input plumbing; see the integration status below. Shop commands, routing, profiles, Presenter/View, and production asset wiring remain deferred. Core API readiness does not establish screen or authored-content readiness.

Primary authority: Core implementation plan (Git 이력: `2026-09-19-sub-character-shop-stage-core-implementation-plan.md`).

## Delivered behavior and ownership

RunProgressionService remains the single owner of progression, wallet, deck, active entry, and latest completion receipt. RunShopService prepares offers, provides availability snapshots, and executes purchases against those owners. ShopStageState holds only fixed entry/offer facts and a three-bit purchased mask.

Each shop presents exactly three uniform draws with replacement from an authored ordered pool. Each slot can grant one copy for its exact Run-currency cost; equal characters in different slots are independent purchases. Free purchases consume a slot and advance both revisions. Existing ownership is eligible; initially unowned characters are eligible when included in the full Run catalog.

Completion is a separate public command, valid after zero through three purchases. It preserves HP, wallet, deck contents, DeckRevision, and map position. It returns to StageSelection/InProgress, advances RunRevision once, clears current entry/node/stage/execution/content kind and active Shop, and records the actual prior purchases. There is no reroll, discount, exchange, growth, random event, combat outcome, exit fee, or generic non-combat framework.

## Public APIs

```csharp
public RunProgressionService(
    RunId id, StageMapSession map, UnitSpec mainCharacterSpec,
    RunCharacterDeckSpec deckSpec, IReadOnlyList<UnitSpec> characterCatalog,
    RunNonCombatSpec nonCombatSpec, int initialBalance = 0);

// RunProgressionService
public RunShopService Shop { get; }
public ShopStageEntryRequestResult TryCreateShopEntry(StageNodeId nodeId);
public RunCommandResult TryActivateShopStage(ShopStageEntryRequest request, ShopStageSpec spec);
public RunCommandResult TryCompleteShopStage(RunId runId, StageEntryId entryId);

// RunShopService
public bool TryGetSnapshot(out ShopStageSnapshot snapshot);
public ShopCommandResult TryPurchaseOffer(RunId runId, StageEntryId entryId, ShopOfferId offerId);

// Immutable inputs
public NonCombatStageBinding(StageKey stageKey, NonCombatStageKind kind);
public RunNonCombatSpec(IReadOnlyList<NonCombatStageBinding> bindings, int shopBaseSeed);
public ShopCandidateSpec(EntityKey characterKey, int cost);
public ShopStageSpec(StageKey stageKey, IReadOnlyList<ShopCandidateSpec> candidates);
public ShopStageEntryRequest(RunId runId, StageEntryId entryId, long expectedRunRevision,
    StageNodeId nodeId, StageKey stageKey, HealthStateSnapshot health, int offerSeed);
```

Use Core-created requests; do not reconstruct identifiers, health, or seeds in presentation. Entry requests freeze identities, RunRevision, checkpoint health, and OfferSeed without moving the map. Activation revalidates these inputs, selection, feature binding, matching Spec key, and the entire candidate pool before sampling. Invalid activation leaves map and Run state unchanged. Activation increments RunRevision once and retains the previous completion receipt.

The pool must be nonempty, have unique character keys and nonnegative integer costs, and contain only catalogued player-eligible regular characters. Main, enemy-only, and unknown characters are rejected. Validation uses the same catalog eligibility predicate as public deck additions. All sale characters must be supplied in characterCatalog even if the initial deck does not own them. Candidate order affects deterministic sampling.

## Explicit binding and seed construction

StageExecutionKind remains Combat = 0 / NonCombat = 1. NonCombatStageKind is a separate discriminator: Recovery = 0 / Shop = 1.

RunNonCombatSpec copies its bindings and accepts any signed base seed. Binding entries reject empty keys and undefined kinds; the spec rejects null entries and duplicate keys. Run construction requires a binding for every actual NonCombat node; unused authored bindings are allowed. Combat-only callers must supply an explicit empty binding list and seed. No compatibility overload invents bindings.

Execution classification is checked before binding lookup. A Combat node remains Combat even if its key also appears in the binding table. Recovery and Shop request/activation APIs require their exact feature binding; missing bindings are constructor argument errors and wrong-feature entry commands return StageKindMismatch.

Run derives OfferSeed using the plan's fixed unchecked mix:

```csharp
unchecked
{
    uint hash = 2166136261u;
    hash = (hash ^ (uint)shopBaseSeed) * 16777619u;
    hash = (hash ^ (uint)nodeId.Value) * 16777619u;
    hash = (hash ^ (uint)stageKey.Value) * 16777619u;
    return (int)hash;
}
```

After full pool validation, a private new Random(offerSeed) makes exactly three Next(candidateCount) calls. Slots are numbered 1, 2, 3. Queries and purchases do not consume RNG or regenerate offers. Fixed regression: base seed 42, node 2, stage 100 gives seed 76514027 and two-candidate indices [0, 1, 1]. Failed preparation retains no RNG state. Cross-runtime save compatibility remains outside scope.

## Snapshots, reasons, and transaction boundary

ShopStageSnapshot contains Run/entry/node/stage identities, RunRevision, Wallet, Deck (including DeckRevision), three copied Offers, PurchasedCount, IsSoldOut, and CanComplete. Offers expose Id, CharacterKey, Cost, IsPurchased, CanPurchase, and UnavailableReason. Retained snapshots remain unchanged after commands.

UnavailableReason uses ShopFailureReason; no separate display-reason enum was introduced. Availability priority is SoldOut, InsufficientFunds, DeckCountOverflow (per-key or total), DeckRevisionOverflow, RunRevisionOverflow. A valid active shop can complete regardless of balance or purchase count unless RunRevision is exhausted.

SoldOut is the snapshot/display reason for a purchased slot. An active command retry for that same slot is success with AlreadyApplied = true and the current snapshot, checked before funds/count/revision limits. Identify commands by RunId + StageEntryId + ShopOfferId, never CharacterKey. Callers cannot override the price, key, or quantity.

Purchase plans payment and one deck copy, prepares replacement deck dictionary/list storage, next entries, Run values, purchased mask, and the complete result snapshot. Before writing, it revalidates the active reference, identities, revisions, wallet balance, slot state, and deck plan owner/revision. The commit uses assignments only: wallet balance, prepared deck references/count/revision, Run values, purchased mask. No allocation, validation, or delegate participates after the first write.

Public wallet mutation and deck-add phase guards remain intact during Shop. Unchanged wallet arithmetic is reused internally. Ordinary availability queries reuse deck arithmetic guards without allocating mutation plans.

Run activation/completion reuse existing RunFailureReason.RevisionOverflow. Only InvalidShopSpec was appended to RunFailureReason, preserving existing numeric values. Shop purchase uses the planned ShopFailureReason.RunRevisionOverflow.

## Completion receipts and lifetimes

ShopReceiptSnapshot exposes Purchases, PurchaseCount, and checked TotalSpent. Purchases contains zero to three copied ShopPurchaseSnapshot records in ascending offer-slot order; each record exposes OfferId, CharacterKey, and actual Cost for one granted copy. Duplicate character keys are valid. Empty means no purchases; a free purchase has a record even though its cost is zero. Completion records already committed facts without charging, granting, or re-evaluating content.

CompletedStageSnapshot now exposes nullable NonCombatKind and a typed Shop payload alongside existing Combat and Recovery constructors. Exactly one payload exists. Combat has null NonCombatKind; Recovery and Shop set their respective kinds. Outcome throws for either NonCombat receipt.

RunProgressSnapshot requires an appended nullable currentNonCombatKind argument. It is present exactly during NonCombatStageActive and null during selection, combat, combat results, and RunEnded. All Core producers and discovered Core fixture callers were migrated explicitly.

Purchasing after completion is rejected. The matching latest Shop receipt gives StaleEntry; older commands fail current phase/identity checks. Matching Shop completion retries succeed with current Run state while that receipt remains LastCompletedStage, including during a later active combat, recovery, or shop. They never clear that later entry. Recovery retries similarly recognize only Recovery receipts. A later completion replaces the retry window; no history ledger was added. Legacy TryCompleteStage retained-receipt replay behavior remains unchanged.

Completion overflow preserves the active shop and prior purchases. A dead checkpoint returns InvalidMainCharacter with null CurrentRun, matching recovery's existing behavior. Commands after disposal return Disposed; queries throw. A live inactive shop query returns false/null. Retained immutable snapshots survive disposal.

## Unity/Host/View consumption

The Run services belong to ProjectL.Core.Application.Run. Inputs, IDs, results, and snapshots live under ProjectL.Core.Contracts.Commands, .Ids, .Results, and .Snapshots; feature/phase enums are in .Common. Host forwarding, publication, and screen wiring remain Unity work.

1. Route a selectable NonCombat node by its explicit Shop binding. Through Host, obtain TryCreateShopEntry, freeze the authored ShopStageSpec, and prepare screen dependencies. Retain pending identity and block conflicting input. Do not advance the map through StageMapSession.TrySelectNode or prepare a combat Stage for Shop.
2. Activate the retained request/spec through TryActivateShopStage. On success, publish CurrentRun and query Shop.TryGetSnapshot. Repeating a successful activation returns StaleEntry, not AlreadyApplied. Cancellation before activation leaves Core unchanged; screen failure or recreation after activation must reconnect to the same active entry through queries, without recreating or reactivating it.
3. Bind each slot to its ShopOfferSnapshot.Id and originating RunId/EntryId. Render Cost, IsPurchased, CanPurchase, and UnavailableReason from the latest snapshot. CanPurchase is a display/input guard, not a guarantee that a retained command will succeed. On purchase, forward only those IDs; publish CurrentShop when present and refresh the Run snapshot for shared displays. ShopStageSnapshot has no HP field; use RunProgressSnapshot.MainCharacterHealth if HP is displayed.
4. Offer a reachable completion interaction using CanComplete, including zero balance and zero purchases. Call TryCompleteShopStage through Host. Buying the third slot does not complete the stage automatically. On a first successful completion, return to selection using CurrentRun; do not call TryCompleteStage or TryLeaveStageResult as the Shop exit path.

| Result | Consumer handling |
|---|---|
| ShopStageEntryRequestResult | Read Succeeded and FailureReason (RunFailureReason); Request is null on failure. |
| RunCommandResult from activation/completion | Read Succeeded, FailureReason, AlreadyApplied, and CurrentRun. CurrentRun can be null after disposal, an unreadable active combat binding, or the documented dead-checkpoint completion failure. Do not substitute stale state. |
| ShopCommandResult | Read Succeeded, FailureReason (ShopFailureReason), AlreadyApplied, and CurrentShop. CurrentShop can be present on rejection; it is null when no active Shop exists or after disposal. Refresh matching screens from returned state without assuming failure implies a null snapshot. |

Retain the originating Run instance and entry identity in command ports. Never retarget an old click to the Host's current IDs. A rejected old purchase can return a newer active shop's CurrentShop; check snapshot identity before applying it to a screen. A completion retry can return a later active combat/recovery/shop in CurrentRun: AlreadyApplied must not trigger unconditional navigation, teardown, or release of that later entry. Treat purchase AlreadyApplied as confirmation of a prior purchase, not a new acquisition.

Host must serialize commands under Core's existing synchronous single-thread contract and publish only after they return. Display/subscriber failure does not undo a committed purchase. Preserve the active shop across ordinary screen recreation; invalidate originating ports on actual Run disposal. ResetRun now rejects both StageActive and NonCombatStageActive, and StartNewRun retains the complete initial Core inputs. Shop still has no borrowed combat Stage to release; command-port invalidation and shop screen release remain integration work.

View/Presenter must not own wallet/deck/sold-slot state, regenerate offers or prices, calculate authoritative affordability/overflow rules, spend currency, add deck copies, spawn battle entities, guarantee an opening-hand draw, or reconstruct receipt totals. Core owns those decisions and mutations. View owns labels, portraits, input gating, and presentation of returned snapshots; completion failure keeps prior purchases and requires retrying completion rather than purchasing again or refunding locally.

## Verification actually executed

Verification used the local ignored .utmp/ShopVerification runner adapted from T6Verification, not Unity EditMode. Contracts, Domain, and Application were rebuilt from current source globs as separate assemblies with their real ProjectL.Core.* names and existing internal-visibility boundaries. The executable test assembly is ProjectL.Tests.Core, targeting .NET 9 and referencing the repository's cached NUnit framework. No stale copied DLLs were used.

Execution history:

- T1: Contracts and Core dependents built successfully, zero warnings/errors, after allowing SDK access outside the sandbox.
- T2: RunRecoveryTests + MainCharacterRunProgressionTests + RecoveryStageContractTests: 101 passed, 0 failed, 0 skipped.
- T3: RunCharacterDeckTests: 11 passed, 0 failed, 0 skipped.
- T4: RunShopTests: 13 passed, 0 failed, 0 skipped.
- T5: forced current-source rebuild: zero warnings/errors. Final selected run: 131 passed, 0 failed, 0 skipped.

| Final selection | Passed | Failed | Skipped |
|---|---:|---:|---:|
| RunShopTests | 13 | 0 | 0 |
| RunRecoveryTests | 44 | 0 | 0 |
| MainCharacterRunProgressionTests | 33 | 0 | 0 |
| RunCharacterDeckTests | 11 | 0 | 0 |
| RecoveryStageContractTests | 24 | 0 | 0 |
| Existing GameFlowContractTests methods below | 6 | 0 | 0 |

Exact final commands, run from the repository root:

```powershell
dotnet build .utmp/ShopVerification/Tests.csproj --no-restore --no-incremental --verbosity quiet
$filters = @(
    'ProjectL.Tests.Core.RunShopTests'
    'ProjectL.Tests.Core.RunRecoveryTests'
    'ProjectL.Tests.Core.MainCharacterRunProgressionTests'
    'ProjectL.Tests.Core.RunCharacterDeckTests'
    'ProjectL.Tests.Core.RecoveryStageContractTests'
    'ProjectL.Tests.Core.GameFlowContractTests.RunProgressSnapshot_NonCombatRequiresLiveInProgressStageIdentity'
    'ProjectL.Tests.Core.GameFlowContractTests.RunProgressSnapshot_RetainsRecoveryWithoutReadingCombatOutcome'
    'ProjectL.Tests.Core.GameFlowContractTests.RunProgressSnapshot_PreservesCombatHandoffAndResultInvariants'
    'ProjectL.Tests.Core.GameFlowContractTests.CompletedStageSnapshot_RequiresTerminalStageAndNonemptyIdentifiers'
    'ProjectL.Tests.Core.GameFlowContractTests.CompletedStageSnapshot_RecoveryHasExclusivePayloadAndNoCombatOutcome'
)
dotnet run --no-build --project .utmp/ShopVerification/Tests.csproj -- @filters
```

The runner uses NUnit FullNameFilter selections and fails if the selected count is zero. Local detailed results are in .utmp/ShopVerification/core-results.xml. The runner is an optional ignored tool, not a committed project dependency.

New tests comprise the 13-case RunShopTests fixture, one binding-route regression, and one lower-level prepared-deck-plan ownership/staleness test. Existing deck coverage already proves ordering, existing/new-key additions, retained snapshots, same-key count overflow, and revision rejection. Shop integration adds aggregate overflow before granting an unowned key, plus wallet/Run consistency, duplicate slots, zero cost, zero-purchase exit, completion retries, next combat preparation, and feature/lifetime isolation.

No dedicated DTO, ID, constructor, wrapper, copied-field, or checklist tests were added. No manufactured RED cycle was run. Wallet internals, StageMap algorithms, StageDraw, and StageGold production code were unchanged, so their separate groups and unrelated EditMode/Unity integration groups were not run.

## Reconciliation and deviations

There was no architecture, behavior, or gameplay-scope deviation. Implementation followed T1 through T5. The existing Run overflow reason was sufficient.

The caller audit found additional RunProgressSnapshot constructors and receipt tests in GameFlowContractTests, beyond the plan's named fixture inventory. These callers received the required explicit content-kind argument; only five relevant existing methods (six cases) were selected for final verification. The prepared-deck-plan test was added because existing public-service tests could not exercise a foreign or stale internal plan.

Modified shared production files are RunProgressionService, RunRecoveryService, RunCharacterDeckService, RunProgressState, RunProgressValues, RunCharacterDeckState, RunProgressSnapshot, CompletedStageSnapshot, and RunFailureReason. Added production files are the plan's Shop contracts, NonCombatStageKind/binding/spec, ShopStageState, and RunShopService, with matching metadata. Core fixture changes and this handoff complete the delivery; the recovery handoff was updated only for binding/constructor/typed-receipt integration changes.

## Deferred Unity consumer prerequisites

The Host initialization follow-up implements these inputs without changing Core gameplay or adding Shop command forwarding:

- `RunSessionHost.InitializeProgression(UnitSpec, RunCharacterDeckSpec, IReadOnlyList<UnitSpec>, RunNonCombatSpec, int initialBalance = 0)` replaces the removed one-argument Host initializer. It publishes progression only after successful Core construction, copies the full catalog, and retains all initial inputs for `StartNewRun`. Reset/disposal clears the retained inputs.
- `StageDefinitionSO.ExecutionKind` defaults to Combat for existing combat assets. A NonCombat definition must explicitly select `NonCombatKind` (Recovery or Shop); the unassigned value is rejected. Boss content must be Combat.
- `StageMapCatalog.RegularStages` freezes `StageContentRef` values, preserving duplicate candidate weights. `CreateNonCombatSpec(shopBaseSeed)` freezes one binding per distinct NonCombat key. `RegularStageKeys` remains a display lookup convenience. `RunSessionHost.Initialize` uses `RegularStages` for Core map generation.
- `PlayerLoadoutSO.CharacterCatalog` is a required full `UnitRosterSO` reference for production initialization. `PlayerLoadoutAdapter` copies authored ownership counts, the Main-exclusive starting hand count, draw seed, and all roster definitions. `StageSelectSceneInstaller` passes these inputs and `StageMapDefinitionSO.ShopBaseSeed`; its initial Run balance remains 0. Main is supplied separately.

**Required asset assignment:** connect the initial PlayerLoadout's new Character Catalog field to the complete existing UnitRoster, including unowned sale characters. No production `.asset`, scene, or prefab was changed. NonCombat content also needs explicit execution/feature settings and the deferred profiles/routes below.

This is an initialization compatibility fix, not completion of the combat Draw/Gold migration. `PlayerLoadoutState`, Unity squad selection, and the old Stage factory path still exist. Production combat preparation must migrate to the exact `request.Draw` and an authored economy input before Run combat activation is ready. Do not use the retained legacy loadout as Shop deck ownership or update it after purchases; Core Deck remains authoritative. Paths below are relative to Assets/Scripts/ProjectL unless otherwise stated.

| Future owner/path | Remaining work |
|---|---|
| Unity/Data/ScriptableObjects/Stage/StageDefinitionSO.cs | Assign the implemented execution/feature metadata in assets and add Recovery/Shop profile references. Do not infer feature from names/key ranges. |
| Future Shop SO/adapter | Author UnitDefinitionSO + Run-price pairs, validate them, and copy them into ShopStageSpec. Keep portraits/text outside Core and freeze active inputs. |
| Unity/Data/Runtime/StageMapCatalog.cs and map consumers | Initialization now uses StageContentRef and explicit bindings. Complete feature routing in selection/screen consumers; preserve the frozen catalog and weights. |
| Unity/Runtime/Run/RunSessionHost.cs | Initialization is migrated. Forward shop request/activation/query/purchase/completion and publish snapshots after commands, with originating identity and lifetime guards. |
| Unity/Runtime/StageSelect/StageSelectRuntime.cs and Setup | Route using execution kind plus explicit authored feature binding. Adopt Shop request/activation and block competing navigation during an active entry. |
| Shop Presenter/View | Render exactly three slots. Submit OfferId with originating Run/Entry IDs, display Core availability/reasons, and provide a reachable TryCompleteShopStage path at zero balance and zero purchases through Host. |
| Unity integration tests | Constructor, Draw-request, and snapshot compile consumers are updated. Migrate legacy combat preparation fixtures with Draw/Gold integration and validate SO-to-Core-to-screen behavior in the separate integration task. The test-only empty deck used by legacy fixtures is not a production fallback. |

Follow-up verification: current-source `ProjectL.Unity` and `ProjectL.Tests.StageComposition` assemblies compiled with zero warnings/errors in the isolated build. Unity 6000.0.73f1 batch EditMode execution passed exactly 15 cases: `RunSessionInitializationTests` (3), `StageMapDataLayoutTests` (6), and `StageResultFlowTests` (6). The new integration fixture checks initial deck/full-catalog delivery, restart/disposal, weighted frozen Shop metadata, feature rejection, active-Shop Reset protection, and failure/retry without published partial state. No additional Core or unrelated EditMode/PlayMode groups were run. Production asset wiring, legacy combat migration, and screen acceptance remain unverified. Recovery's one-purchase limit, zero-effect rejection, and HP rules remain unchanged.
