# Configurable Category Trait Progression — Core to View/Flow Handoff

Prepared: 2026-10-28. Checkout: `feature/Cheon/Core/NewCombatSytem`.

**Status:** Core Phases A–C are implemented and reviewed. Phase D's consumer handoff is prepared and independently contract-reviewed, including two corrected wording issues about registration access and stale-Run failure receipts. External delivery/owner acceptance and Phase E integration are pending. Production activation is not approved by this document.

Sources: [implementation plan](../plans/2026-10-27-category-trait-progression-implementation-plan.md), [design](../specs/2026-10-27-category-trait-progression-design.md). This contract supersedes the fixed-slot API in [the older Sub trait handoff](2026-10-27-sub-trait-growth-core-handoff.md). Its `GetTraitGrowthChoices`, `ChooseTraitGrowth(ownedId, EnhancementKey)`, Sub-only restriction and one-Acquire/one-Enhance model are historical, not supported consumer APIs.

## 1. Ownership and access

| Owner | Responsibility |
| --- | --- |
| InGame authoring/query | Immutable ordered category steps, candidate pools, normal/enhanced trait payloads and display text. |
| Core | Shared eligibility/composition, target/step ordering, frozen-offer preparation, exact-command processing and fresh progress projection. |
| Character Buffer | Authoritative owned selection histories, one active offer and one retained processed result. |
| TraitGrowthView Buffer | One pending input, lifetime request-number allocation and exact result acknowledgment. |
| Existing LifeCycle Flow/Plan/Jobs | Safe-boundary ordering, explicit Core calls, command scheduling, continuation and scene transitions. |
| View/UI | Display published values, submit a selected published key, consume the exact result, acknowledge it, and manage presentation/input lifetime. |

The current consumers live in `ProjectL.Systems`. In their existing binding/composition boundary, use the established owning contexts:

```csharp
var core = resolver.GetService(SystemGroupsService.InGame).CoreGroup;
var buffers = resolver.GetService(BufferSystemKey.Buffer);

var growthRead = buffers.TraitGrowthStateReadSink;
var growthResults = buffers.TraitGrowthResultReadSink;
var growthInputRead = buffers.TraitGrowthViewReadSink;
var growthInputWrite = buffers.TraitGrowthViewWriteSink;
```

`resolver` is the existing `ISystemServiceStoreReader`. `SystemGroupsService` and `BufferSystemKey` are in `ProjectL.Systems.ServiceKeys.Runtime`. `SystemGroupsService` is public; `IBufferQuery` and `BufferSystemKey` are assembly-internal. The interfaces/DTOs exposed through these contexts are the consumer contracts. This is the existing Systems binding path, not a promise that an unrelated assembly can resolve internal keys.

Flow uses `core.Characters.ReadTraitGrowthProgress()`, `core.Characters.PrepareNextTraitGrowthOffer()` and `core.TraitGrowthCommands.ProcessNextTraitGrowthCommand(number)`. Inject only the necessary contracts into collaborators. View uses the Buffer read/input contracts; it must not call the roster service, read InGame tables, or install authoritative state. Do not re-export child services at the runtime root or depend on `CharacterRosterService`, the validator, composer, or transaction preparation internals.

Registration is already implemented: Buffer Factory creates the Character-owned state and input Buffer; their local exports are retrieved by `BufferSystemGroup`. Core Factory constructs Characters before the coordinator, injects `ITraitGrowthAvailabilityRead`, and registers the processor once. `InGameCoreSystemGroup` exposes Characters and TraitGrowthCommands through the existing InGame context. View/Flow consumer registration is still external work.

Contract sources:

- [Core Group](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/InGameCoreSystemGroup.cs), [Buffer Group](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/Core/BufferSystemGroup.cs).
- [Roster service contract](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CharacterSystem/CharacterRosterService.cs), [progress read contract](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CharacterSystem/ITraitGrowthAvailabilityRead.cs), [processor contract](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CharacterSystem/TraitGrowthCommandProcessingService.cs).
- [Offer/result reads](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/Character/TraitGrowthOfferState.cs), [input/acknowledgment contract](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/TraitGrowthView/TraitGrowthViewBuffer.cs).

## 2. Exact operations and side effects

| Surface | Signature / members | Contract |
| --- | --- | --- |
| `ICharacterRosterService` (inherits `ITraitGrowthAvailabilityRead`) | `TraitGrowthProgressSnapshot ReadTraitGrowthProgress()` | Side-effect-free fresh projection; no offer allocation, sampling, history mutation or acknowledgment. May read immutable definitions and diagnose invalid state. |
| `ICharacterRosterService` | `TraitGrowthProgressSnapshot PrepareNextTraitGrowthOffer()` | Explicit mutation only when `ChoiceRequired` has no offer. Otherwise returns the current status/offer. Rechecks source and receipt barrier before publishing. Source changes may return a fresh snapshot without an offer: inspect the returned status again. |
| `ITraitGrowthStateReadSink` | `TraitGrowthOffer ActiveOffer { get; }` | Side-effect-free, nullable immutable published offer. Reading/reopening never rerolls it. |
| `ITraitGrowthResultReadSink` | `int HandledCommandNumber { get; }`, `TraitGrowthProcessedResult LatestResult { get; }` | Side-effect-free. Initially/reset: zero/null. The latest result remains readable after acknowledgment until replaced or context-cleared. Non-null alone does not mean it is unconsumed. |
| `ITraitGrowthViewWriteSink` | `int Submit(TraitGrowthChoiceRequest request)` | Stores one pending input and returns its positive number. Does not apply growth. Null throws; a second pending submission throws before writes; allocation overflow fails before storing. |
| `ITraitGrowthViewReadSink` | `bool TryGetCommand(int number, out TraitGrowthViewCommand command)` | Exact-number lookup without consuming. No public queue enumeration or next-number inference API. Use the output only when true. |
| `ITraitGrowthViewReadSink` | `int AcknowledgedResultNumber { get; }` | Read-only delivery correlation. View may distinguish retained already-consumed results; Flow must use the progress contract for growth readiness. |
| `ITraitGrowthCommandProcessingService` | `bool ProcessNextTraitGrowthCommand(int expectedNumber)` | Core-side processing attempt. Positive number required. True means a success **or typed failure** receipt was published. False means no publication, e.g. missing/handled input, retained unacknowledged result, or input canceled during reads. Neither return value is permission to continue to another node. |
| `ITraitGrowthViewWriteSink` | `bool AcknowledgeResult(RunId runId, int number)` | Acknowledges the current retained result's exact publication Run and number, and removes its matching actual input. Wrong Run, future/obsolete number or mismatched input returns false without writes. Repeating the latest accepted same-context acknowledgment returns true without writes. No trait grant or next-offer creation. |

Binding/DB initialization must be complete before Core calls. Supported operations are synchronous on the existing runtime execution boundary. Invalid authoring, inconsistent state/counters, repeated source mutation and allocation overflow are diagnostic exceptions, not ordinary choice rejection. Let the existing fault boundary handle them; do not suppress them as successful completion.

There is no View `Clear`, cancellation/removal method, direct choice service or roster growth-write bypass. `ITraitGrowthViewBufferStore.Clear()` and transaction installation are internal lifecycle operations, not consumer APIs.

## 3. Payloads

Roster DTOs live in `ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster`; `TraitGrowthKind` and selection history are under `Contracts.Enhancements`.

| DTO | Read-only fields / constructor |
| --- | --- |
| `TraitGrowthProgressSnapshot` | `RunId RunId`, `TraitGrowthProgressStatus Status`, nullable `TraitGrowthOffer ActiveOffer`. |
| `TraitGrowthOffer` | `RunId RunId`, `long OfferId`, `CharacterInstanceId OwnedId`, `int StepId`, `TraitGrowthKind Kind`, `IReadOnlyList<TraitGrowthChoice> Choices`. |
| `TraitGrowthChoice` | `TraitKey TraitKey`, `TraitGrowthKind Kind`, `string Name`, `string Description`. Kind is the offered normal acquisition or enhanced form; there is no separate `Form` member. |
| `TraitGrowthChoiceRequest` | `TraitGrowthChoiceRequest(RunId runId, long offerId, CharacterInstanceId ownedId, int stepId, TraitKey traitKey)`. Copy identities from the displayed offer and a key from its choices. |
| `TraitGrowthViewCommand` | `int Number`, `TraitGrowthChoiceRequest Request`. Submit owns number allocation. |
| `TraitGrowthProcessedResult` | `RunId RunId`, `int Number`, `TraitGrowthChoiceRequest Request`, `TraitGrowthResult Result`. `RunId` is the publication owner's Run; `Request.RunId` is the unchanged submitted Run. |
| `TraitGrowthResult` | `bool Succeeded`, `TraitGrowthFailureReason FailureReason`, nullable `OwnedCharacterRecord Final`. Final exists only for success. |
| `OwnedTraitGrowth` / `TraitGrowthSelection` | Read-only `Selections`, each with `int StepId` and `TraitKey TraitKey`. This is the stored history; View does not append it. |

Acquire offers always contain three distinct choices. Enhance offers contain one, two or three (`min(3, eligible count)`). Display the published order and count; never pad, independently filter, regenerate or pick a fallback key. Native traits remain; Enhance replaces one owned trait's payload without adding another trait. Each trait can be enhanced once.

Offer IDs and input numbers are monotonic over their respective Buffer lifetimes, including after context reset; do not assume consecutive values or reset to one. Use the complete Run/offer/owned/step/key identity. There is no money/cost field: level-up choices do not purchase anything.

Use `OwnedId` with the Character Buffer's published owned records when target identity/role is needed, and the existing View unit/trait visual tables for presentation. `ViewQuery.TraitVisualTableQuery.TryGetTraitVisual(TraitKey, out TraitVisualData)` is the existing trait icon lookup. Names/descriptions/forms come from the offer; View must not reconstruct them from InGame data or use obsolete enhancement recipe keys.

`TraitGrowthFailureReason` currently declares `None`, `InvalidRequest`, `NotUnlocked`, `NotCandidate`, `UnknownDefinition`, `NotOwned`, `InBattle`, `RunEnded`, `SourceChanged`, `StaleOffer`, and `AwaitingAcknowledgement`. Do not assume every declared reason is currently emitted by every path. Typical handling:

- `NotCandidate`: a valid trait key was not in this frozen offer or is no longer eligible. Present the failure, acknowledge its receipt, then read fresh progress; do not consume the offer locally.
- `StaleOffer`, `NotOwned`, `NotUnlocked`, `SourceChanged`: discard stale presentation assumptions after consuming/acknowledging the exact receipt; reload current progress/offer. Do not resubmit the old request automatically.
- `InBattle` / `RunEnded`: follow fresh progress and the existing battle/Run policy. Never turn a rejection into a grant or transition.
- The processor normally stops before publication when a prior receipt is outstanding. `AwaitingAcknowledgement` is primarily a progress state, not a signal to acknowledge unseen results.

For a late old-Run request, `receipt.RunId` may differ from `receipt.Request.RunId` on failure. Acknowledge using **receipt.RunId**. A successful result requires matching identities. Old-Run acknowledgments cannot consume a new Run's receipt.

## 4. Four-state progress contract

Evaluate in the listed priority order; consumers do not store a parallel pending/complete flag.

| Status | Condition | Consumer action |
| --- | --- | --- |
| `Unavailable` | Run ended or battle still active; the implementation also returns this before a Run identity exists. | Do not prepare/select growth. Continue the existing battle/Run-end policy. This is not ordinary growth completion. |
| `AwaitingAcknowledgement` | Continuing non-combat Run with handled result number greater than acknowledged result number. | Deliver/recover the retained result. Do not create another offer, process another choice or permit new stage entry. A failure result may coexist with the same offer. |
| `ChoiceRequired` | No unacknowledged result, and a frozen offer or reached unresolved step exists. | Reuse the offer or explicitly prepare one, then display it. |
| `ReadyToContinue` | Continuing non-combat Run with no unacknowledged result, no offer and no reached unresolved step. | Resume normal admission. Other chapter, node, roster, battle and shop checks still apply. |

Success atomically appends one selection, clears the consumed offer and publishes its numbered result. Even the last selection leaves `AwaitingAcknowledgement`. Failure preserves offer/history and publishes a failure receipt. Acknowledgment changes delivery state only. Read progress again afterward; it may be `ChoiceRequired`, `ReadyToContinue`, or `Unavailable` if the Run context changed.

Core entry checks reject both waiting states with `ChapterProgressionFailureReason.PendingTraitGrowth`, including before an offer has been opened. The public Prepare/Confirm path, buffered node commands, direct battle entry, Shop and Recovery consume fresh admission. Final chapter bookkeeping is deliberately not gated. Hiding/dismissing the UI cannot bypass Core admission.

## 5. Consumer sequence and lifetime

1. Flow completes combat playback and `EndBattle`, then required chapter completion. If the Run ended, follow its existing exit/new-Run destination without growth prompts.
2. For a continuing Run, finish result presentation/confirmation before preparing growth. Existing XP, Sub category stats and Main persistent grants have already been committed; do not replay settlement or level-up grant receipts.
3. Flow reads fresh progress. On `ChoiceRequired`, call `PrepareNextTraitGrowthOffer()` only if needed and inspect its returned status. On `AwaitingAcknowledgement`, recover the retained result instead.
4. View reads the published offer and renders exactly its choices. A single pointer/EventSystem Submit sends:

   ```csharp
   var request = new TraitGrowthChoiceRequest(offer.RunId, offer.OfferId,
       offer.OwnedId, offer.StepId, selectedChoice.TraitKey);
   int number = growthInputWrite.Submit(request);
   ```

5. The existing Flow/Job request-lifetime owner retains `number` and schedules `core.TraitGrowthCommands.ProcessNextTraitGrowthCommand(number)`. It does not infer `handled + 1`: resets can leave number gaps. There is no public peek for an unknown unprocessed input number. Retain this correlation across presentation unbind, rather than destroying its only owner before processing completes.
6. View reads `LatestResult`, correlates its number and exact request when available, and presents success/failure. A rebind can recover an unconsumed receipt directly from the Buffer even when its old UI instance is gone. `HandledCommandNumber > AcknowledgedResultNumber` distinguishes retained unconsumed delivery; it does not replace the Core progress projection.
7. After consuming that result, call `growthInputWrite.AcknowledgeResult(receipt.RunId, receipt.Number)`. If false, reload the current context rather than clearing state or assuming success. The still-readable, already-acknowledged `LatestResult` is not another result to play.
8. Flow reads fresh progress and repeats or continues. A false processor return, a closed popup, a cleared offer after success, or reaching the last step is never sufficient continuation evidence.

**Unbind/reopen:** unsubscribe/release only presentation-owned callbacks, selection visuals and input scopes. Do not auto-acknowledge, clear pending input, discard an offer or submit again. The request-lifetime/Plan owner must survive presentation replacement while an unprocessed request exists. Restore interaction from current Buffer state. If the external architecture destroys that owner too, it must resolve the missing-number recovery contract explicitly before integration; do not guess numbers or add an undocumented queue.

**Run end/reset/shutdown:** existing Core/Buffer lifecycle owners clear offer, receipt and pending input together. Reset clears handled/acknowledged context but retains allocation high-water marks. Old requests cannot grant traits in a new Run, and old-Run acknowledgments cannot consume a new Run's receipt. A late old request may still produce a failure receipt owned by the current Run and wait for its acknowledgment. Ended Runs return `Unavailable`; do not force discarded selections. Presentation teardown alone performs none of those lifecycle operations.

**Non-combat acquisition:** after a purchase/acquisition transaction and its own result consumption are finished, Flow reads the same growth progress before permitting another node. Higher-level acquisition makes reached steps pending; it does not auto-grant traits. Another legal acquisition while an offer exists must not reroll or replace that offer; subsequent targets follow owned-ID order, then configured step order. Ordinary per-definition ownership limits remain unchanged.

## 6. Worked fixture sequences

These are illustrative DTO values built from test-fixture content, not production balance, actual generated GUIDs, or a claim that a particular seed returns these exact subsets. Offer/request IDs are example labels. Source payloads: [TraitGrowthTestRuntime.GrowthTables](../../../Assets/Tests/EditMode/CoreIntegration/TraitGrowthTestRuntime.cs). Tests provide normal/enhanced Knockback 1/3, DoubleShot 1/3, HuntersMark 20/40 and ContemptForWeak (2,1)/(4,2), plus non-enhanceable PrecisionShot. Their published names are `knock`, `double`, `mark`, `weak`, `precision`, and enhanced names with `+`; descriptions are `normal` / `enhanced`.

### 6.1 Two Acquire steps and one Enhance step

Use the existing multi-level fixture: native Knockback, level 5; ordered rows `10 = Acquire at 2`, `20 = Enhance at 2`, `30 = Acquire at 4`. The sequence deliberately follows configured order, not grouping by kind. Assume this is the only pending owned instance, `OwnedId=5`, in Run R.

| Published offer | Display-ready choices in TraitKey order | Selection and following state |
| --- | --- | --- |
| R / OfferId 1 / OwnedId 5 / StepId 10 / Acquire | `(ContemptForWeak, Acquire, "weak", "normal")`, `(PrecisionShot, Acquire, "precision", "normal")`, `(DoubleShot, Acquire, "double", "normal")` | Submit DoubleShot, number 41. Success stores `(10, DoubleShot)`, clears offer and waits for acknowledgment 41. After acknowledgment: `ChoiceRequired`. |
| R / OfferId 2 / OwnedId 5 / StepId 20 / Enhance | `(Knockback, Enhance, "knock+", "enhanced")`, `(DoubleShot, Enhance, "double+", "enhanced")` | Submit Knockback, number 42. Success appends `(20, Knockback)`; total trait count stays two. Acknowledge 42 before the next offer. |
| R / OfferId 3 / OwnedId 5 / StepId 30 / Acquire | `(ContemptForWeak, Acquire, "weak", "normal")`, `(PrecisionShot, Acquire, "precision", "normal")`, `(HuntersMark, Acquire, "mark", "normal")` | Submit HuntersMark, number 43. Three total traits: enhanced native Knockback, normal DoubleShot, normal HuntersMark. Still `AwaitingAcknowledgement`; only acknowledgment 43 permits `ReadyToContinue` when no other target is pending. |

If another owned instance has reached rows, the final acknowledgment above yields `ChoiceRequired` for it instead. Neither XP nor levels change during this sequence. Below the next row's unlock level, that future row does not block continuation.

### 6.2 One-option Enhance and Main

The `lastOnly` fixture has native Knockback and only row 10 (`Enhance at 2`). At level 2 its offer has one choice: `(Knockback, Enhance, "knock+", "enhanced")`. Display one selectable option, not two placeholders. Processing its selection still requires a numbered result acknowledgment before continuing.

Configure `CategoryKey.Main` in that same fixture and target the owned Main ID: the operation, receipt, acknowledgment and composition path are identical. Do not hide the UI because `Role == Main`. Production Main steps/pool remain empty/pending; this fixture approves no Main content or unlock schedule. Empty Main authoring yields no opportunity and does not independently block valid Sub progression.

### 6.3 Failed choice and receipt rebind

For offer 1 above, HuntersMark is valid category content but was not offered. Submitting it produces `NotCandidate`: history, currency, stage and active offer remain unchanged; only the failure receipt is published. While it waits, progress is `AwaitingAcknowledgement` with that same offer. Rebind reads the retained failure, consumes it and acknowledges its publication Run/number. The next prepare returns the original offer. A valid retry gets a new request number and may succeed; it is not a reroll.

If the last success receipt is unconsumed during teardown, `ActiveOffer` is already null. Rebind must still present/acknowledge that receipt; interpreting a null offer as completion would bypass the barrier. Repeating the latest accepted acknowledgment is harmless, but an older number after a newer receipt exists is obsolete.

### 6.4 Higher-level acquisition, termination and stale Run input

A new level-5 unit may already have reached all three rows. With no offer opened, fresh progress is `ChoiceRequired`, and node entry returns `PendingTraitGrowth`. Resolve the steps only after the acquisition's own transaction/result finishes. Keep any earlier frozen target first.

If the Run ends, Core clears its growth context and reports `Unavailable`; no mandatory popup should delay defeat, abandon or final-victory exit. After NewRun, a late request from R may produce a failure receipt owned by new Run R2 while preserving `Request.RunId = R`. Acknowledgment under R is rejected; a current consumer handles the R2 receipt using its actual publication identity. Never reinterpret the old request as a new selection.

## 7. External View/Flow acceptance checklist

These are requirements for the external owners, not implementation delivered by this handoff.

**View owner**

- Register the independent selection panel as `UIBase` with a chosen `UIId`, parent and `UIOpenPolicy` through the existing UISystem. Open/close through `IUISystemService`; no standalone popup service or direct external `SetActive` substitute.
- Bind an explicit `IUIInputReceiver` / `UIInputBinding` once; support pointer and EventSystem keyboard/controller Submit. Do not use Inspector UnityEvents or `Button.onClick` closures. Prevent duplicate Submit while the one actual input/result waits.
- Render exact published one/two/three options, names, descriptions and kind. Use existing TraitKey visual lookup and report required missing wiring/assets clearly. Main must share the display path.
- Back/dismiss cannot skip a required choice; reopening cannot reroll. Settings and teardown release/restore only the popup's owned input scopes, preserving pending request/receipt ownership.
- Test success/failure/rebind, last-success-with-null-offer, repeated acknowledgment, pending unprocessed-number lifetime, and multiple input modalities using actual public contracts/EventSystem dispatch.

**Flow owner**

- Continuing victory: playback → EndBattle → required chapter completion → result confirmation → repeated offer/input/result/acknowledgment cycle → normal transition exactly once.
- `AwaitingAcknowledgement` never schedules another offer or resumes normal continuation. Read fresh progress at each action boundary; do not cache `ReadyToContinue` across acquisition/result mutations.
- Final victory/defeat/abandon follow existing exit/new-Run policy without discarded growth prompts. Do not gate chapter-completion bookkeeping with the new-node guard.
- Non-combat readiness runs the same sequence after completed purchases/acquisitions and before node entry. Retain normal entry failure handling; `ReadyToContinue` removes only the growth block.
- Keep the existing Plan/Job runner as sequencing owner. Add only necessary actions/facts; no second general Flow engine or popup-controlled scene transitions. Any changed contract must return to the design/plan, not weaken Core admission.

## 8. Automated Core evidence and its limits

No Unity run was repeated for this documentation-only phase. These are retained A–C execution results checked against their XML and current signatures, not new View/Flow acceptance results.

| Evidence | Coverage |
| --- | --- |
| [TraitGrowthCatalogTests](../../../Assets/Tests/EditMode/TraitGrowthCatalogTests.cs), [TraitGrowthAuthoringTests](../../../Assets/Tests/EditMode/CoreIntegration/TraitGrowthAuthoringTests.cs) | Shared eligibility/capability and all-path validation; immutable SO/query conversion; disabled/empty Main authoring. |
| [TraitGrowthOfferTests](../../../Assets/Tests/EditMode/TraitGrowthOfferTests.cs), [TraitGrowthRulesTests](../../../Assets/Tests/EditMode/TraitGrowthRulesTests.cs), [TraitGrowthChangeTests](../../../Assets/Tests/EditMode/TraitGrowthChangeTests.cs) | Arbitrary ordered history, native preservation, exact append, sampling counts and valid-but-unoffered rejection. |
| [TraitGrowthProgressContractTests](../../../Assets/Tests/EditMode/CoreIntegration/TraitGrowthProgressContractTests.cs), [TraitGrowthTransactionTests](../../../Assets/Tests/EditMode/CoreIntegration/TraitGrowthTransactionTests.cs) | Last-result barrier, failure receipt/rebind, no reroll, stale source, reset, acknowledgment and shutdown. |
| [TraitGrowthChoiceServiceTests](../../../Assets/Tests/EditMode/CoreIntegration/TraitGrowthChoiceServiceTests.cs), [TraitGrowthBoundaryIntegrationTests](../../../Assets/Tests/EditMode/CoreIntegration/TraitGrowthBoundaryIntegrationTests.cs) | Shared Main/Sub path, source-change checks, target/step ordering, per-instance composition and lifecycle boundaries. |
| [TraitGrowthStatSettlementTests](../../../Assets/Tests/EditMode/CoreIntegration/TraitGrowthStatSettlementTests.cs), [TraitGrowthStatCoexistenceTests](../../../Assets/Tests/EditMode/TraitGrowthStatCoexistenceTests.cs) | Actual 950-XP victory settlement, enabled/disabled trait configuration, Main/Sub grants and failure/retry/ack preservation. |
| [TraitGrowthCommandTests](../../../Assets/Tests/EditMode/CoreIntegration/TraitGrowthCommandTests.cs), [TraitGrowthStageGateTests](../../../Assets/Tests/EditMode/CoreIntegration/TraitGrowthStageGateTests.cs) | Real typed Group processor, stale/duplicate/reentrant input, legal acquisition during a frozen offer, pre-offer/final-receipt entry guards, all entry consumers and ungated final-chapter bookkeeping. |
| [SubTraitShopRetirementTests](../../../Assets/Tests/EditMode/CoreIntegration/SubTraitShopRetirementTests.cs) | Stale Sub sale/transaction rejection without money/purchase/roster mutation; unavailable legacy enhancement entry. |

Final A–C regression: [C-T5-full-final/results.xml](../../../Logs/TraitGrowthPlan/C-T5-full-final/results.xml), Core 3773/3777 and CoreIntegration 228/233; total 4001/4010, zero skips. The nine failures exactly match the accepted baseline listed in the implementation plan. All twenty T5 cases passed. [C-T5-green-final/results.xml](../../../Logs/TraitGrowthPlan/C-T5-green-final/results.xml) passed 111/111 focused cases. Independent task and combined B–C reviews reported no actionable findings.

Core tests prove producer/storage/admission behavior. They do not prove that actual View consumes a receipt before acknowledging, that a popup restores input, that scene links exist, or that Flow runs the required sequence.

## 9. Phase E inspection and acceptance ledger

Inspection date: 2026-10-28, current checkout only. No external implementation artifact or owner acceptance was supplied in this request. Repository inspection found:

- No `TraitGrowth` / `PendingTraitGrowth` consumer in `ViewSystem`, `LifeCycleSystem`, `GlobalSystem/UISystem`, or the View Factory tree. The actual public command/offer consumers found are Core definitions/guards/registration and test consumers.
- `UIIDs.cs` has no dedicated growth panel ID. No growth-named UI prefab/scene file or `TraitGrowthPresentationTests` / `TraitGrowthFlowTests` was found. Name searches alone do not prove absence of every possible implementation; combined with the missing contract consumers, this is insufficient evidence for acceptance.
- Existing [CombatPlan](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Combat/Plans/CombatPlan.cs), [CombatProgressReader](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Combat/CombatProgressReader.cs), [StagePlan](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Stage/Plans/StagePlan.cs) and [StageLifeCycle](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Stage/StageLifeCycle.cs) provide existing sequencing/binding locations, but contain no growth consumption cycle. These are owner handoff locations, not authorization to rewrite them in this Core phase.

| Acceptance item | Current evidence / status | Owner and evidence required |
| --- | --- | --- |
| Core implementation | A–C complete; evidence in §8. | Core; rerun affected contracts only if integration changes them or exposes a defect. |
| Handoff prepared | This document and checked source/signatures/examples. | Core. |
| Handoff delivered/accepted | Not externally sent; owner acknowledgment not received. | View/Flow owners must acknowledge the artifact and outstanding responsibilities. No message was sent on the user's behalf. |
| External View delivery | Pending; no current growth consumer/panel evidence found. | View: implementation/ref, UI registration and bindings, variable options, EventSystem Submit, rebind and settings/teardown evidence. |
| External Flow delivery | Pending; no current growth sequence consumer found. | Flow: actual continuing/ending Run sequences, result/ack ordering, high-level acquisition readiness and exactly-once transition evidence. |
| Joint integration acceptance | Pending external integration. | View + Flow + Core: run the concrete sequences in §§5–7 through actual consumers; attach test/manual evidence. |
| Production activation | Pending, outside D/E inspection. | Content/Core + external owners: approved pools/effects, intentional empty Main content, external integration, and Main-shop delivery or approved obsolete-node disabling under F. |

The operational map still references obsolete enhancement nodes; Core rejects their entry and Sub purchases. Do not call an unavailable node a completed Main shop. Do not enable production step lists merely because Core tests pass. Existing empty/pending Main level-up content does not prevent separately approved Sub content, but Main-shop availability is a different release dependency.

**Phase E result: pending external integration.** The inspection and gap/owner ledger are prepared; external View/Flow and joint acceptance checkboxes remain open. Core/handoff completion must not be relabeled as end-to-end acceptance. No surrogate UI/Flow, scene edit, production activation, commit or push is included.
