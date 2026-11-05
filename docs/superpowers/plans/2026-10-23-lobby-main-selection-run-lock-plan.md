# Lobby Main Selection and Run Identity Lock Implementation Plan

> **For agentic workers:** Use `superpowers:executing-plans` or `superpowers:subagent-driven-development` when implementation is explicitly authorized. Execute the approved Phase only, record evidence, and stop at its boundary. Do not commit or push.

**Goal:** Select the starting Main in Lobby, install that choice in a fresh Run, and preserve its instance identity throughout that Run while allowing StageSelect Sub edits.

**Architecture:** Extend the existing Run-start service and loadout authoring. Capture a definition key in the existing departure request; use the existing atomic new-Run publication to create the selected instance. Enforce same-Run identity in Core evaluation and Buffer publication, and derive Lobby/Stage controls from those contracts.

**Tech Stack:** Unity 6000.0.73f1, C#, existing Framework/Buffer/Job contracts, uGUI, NUnit EditMode tests.

**Spec:** [Lobby Main Selection and Run Identity Lock](../specs/2026-10-23-lobby-main-selection-run-lock-design.md).

**Status:** Draft implementation plan, 2026-10-23. The user requested this document; product implementation has not been authorized by that request. All task boxes below are pending. Existing enhancement-display changes belong to concurrent work.

## Global constraints

- Implementation documents are English; code/XML comments and player-facing copy are Korean.
- Preserve existing Framework registration, Group access, UIBase/PopupViewScope lifecycle, and request/result ownership. Framework infrastructure, FieldCamera and unrelated Architecture work are outside scope.
- Core calculates/validates proposals; Buffers own runtime storage, checked installation, publication and revisions. View consumes facts and submits input through existing channels.
- Reuse `RunDeckSnapshot.MainId`; no second locked-Main store, authoritative View deck, or hardcoded Akimbo rule.
- Preserve `StartNewRun(int)` for authored-default callers. Keep both existing Run creation boundaries: post-combat preparation and Lobby departure.
- Preserve starting Subs, initial deck indices, currency, hand size, enhancement eligibility, battle/recovery identity, completed Run summary and destinations.
- No new production characters, unlocks, cross-session preference, arbitrary starting Sub selection, multi-cell support or purchase-history work.
- New C# files require corresponding .meta files; preserve existing GUIDs. Use block namespaces, Allman braces, four spaces, UTF-8 and LF.
- Do not commit, push or stage. Do not overwrite concurrent Core/View/handoff changes.
- Unity must run through the approved AppData-capable execution path. Never start a second Editor for the same open project; do not delete caches or upgrade Unity for the known sandbox failure.
- Phase A changes Core and Buffer contracts. It requires explicit implementation authorization covering that scope; a View-only instruction does not authorize it.

## Review focus

1. A different valid Main must be rejected through Evaluate, Confirm and direct Buffer publication; disabling a button is insufficient (A2).
2. Candidate reads and popup selection must work before the first Run without manufacturing owned IDs or mutating an installed Run (A1, B1).
3. A queued request must retain its selected key despite later UI input, duplicate submission or popup disposal (B2).
4. A failed preparation must preserve the previous Run; transition trouble after successful preparation must not install it again (A1, B2).
5. Single-candidate production content cannot demonstrate two-Main switching. Use valid synthetic alternatives for automated identity tests and keep operating-content/manual claims separate (A1, C2).

## 1. Planning decisions and Phase boundaries

The design already proposes an explicit candidate list and the authored default on every new Lobby session. This plan uses those proposals; writing the plan does not mark them approved. At implementation review, confirm or revise these two policies before A1/B1. Popup close/reopen within one Lobby session preserves its draft.

The first selection screen reuses the existing name/portrait cards. Its Core projection needs validated definition keys, not fabricated owned-character stats. Numeric preview expansion and replacing the Lobby actor's 3D presentation are not prerequisites for selection/locking and are not added here.

| Phase | Deliverable | Dependency | Stop boundary |
|---|---|---|---|
| A | Candidate/selected-start contracts and same-Run identity enforcement | Accepted design policies and explicit Core/Buffer authorization | Core/Buffer checks pass; publish exact contracts for View and stop |
| B | Lobby draft, explicit selection, captured departure and once-only installation | Accepted Phase A contracts | Lobby automated/asset checks complete; report manual status and stop |
| C | Fixed Main controls in StageSelect and end-to-end acceptance | Accepted A and B | Stage regressions and integration/manual evidence recorded; stop |

A alone is not a finished Lobby feature. B does not complete Stage usability. C must not silently activate unapproved Main content.

The older [Lobby plan](2026-10-11-lobby-plan-Hojin.md) contains LV6/LV7/LV8B task placeholders. Use this plan for the specific Main-only departure and Run-lock change; do not expand it into the older arbitrary deck-selection work. Preserve the completed LV8A request/runner/transition behavior.

## 2. File map and ownership

All aliases below are repository-relative paths, not new folders:

- `S` = `Assets/ProjectL/Script/Systems`
- `R` = `S/RuntimeSystem`
- `C` = `R/InGameSystem/CoreSystem`
- `K` = `C/CoreCaculator`
- `V` = `R/InGameSystem/ViewSystem`
- `U` = `R/GlobalSystem/UISystem/Screens`
- `T` = `Assets/Tests/EditMode`

| Task | Modify existing files | New files, each with .meta |
|---|---|---|
| A1 | `S/DataBaseSystem/Data/InGame/Table/Loadout/Item/PlayerLoadoutData.cs`, `PlayerLoadoutDataSO.cs` in the same directory; `C/CharacterSystem/PlayerLoadoutRosterPlanner.cs`; `C/RunSystem/RunStartService.cs`; `Assets/ProjectL/DataBaseSO/InGameGroup/Loadout/Item/PlayerLoadout_1.asset` | `K/Contracts/Roster/MainStartOptions.cs`; `T/Framework/MainStartSelectionTests.cs` |
| A2 | `K/Application/Roster/DeckRules.cs`; `K/Contracts/Deck/DeckFailureReason.cs`; `C/CharacterSystem/RunDeckService.cs`; `R/BufferSystems/Core/RunDeckTransactions.cs` | None expected |
| B1 | `V/Lobby/Deck/LobbyDeckPopupController.cs`, `LobbyDeckPresenter.cs`; `V/Lobby/LobbyViewScope.cs`, `LobbyViewSystem.cs`; `U/Lobby/LobbyDeckDisplayModel.cs`, `LobbyDeckUI.cs`, `LobbyDeckListItemView.cs`; `Assets/ProjectL/Prefabs/UI/Lobby/LobbyDeckPanel.prefab` | `V/Lobby/Deck/LobbyMainSelectionDraft.cs`; `T/Framework/LobbyMainSelectionDraftTests.cs` |
| B2 | `R/BufferSystems/LobbyView/LobbyDepartureRequest.cs`, `LobbyViewBuffer.cs`; `V/Lobby/LobbyDeparturePresenter.cs`; `R/LifeCycleSystem/LifeCycles/Lobby/Jobs/LobbyRunStartJob.cs`; `R/LifeCycleSystem/LifeCycles/Lobby/LobbyDepartureFlow.cs` | None expected |
| C1 | `V/StageMap/Deck/DeckEditDraft.cs`, `DeckEditModelBuilder.cs`, `DeckEditDisplayModel.cs`, `DeckEditText.cs`, `DeckEditPopupController.cs`; `U/Stage/DeckEditUI.cs` | `T/Framework/StageMainLockViewTests.cs` |
| C2 | Affected existing integration fixtures listed below; this plan's evidence log and existing Core/View handoff after checking concurrent changes | No new runtime infrastructure |

Only change assembly/Factory/Group/ServiceKeys files if actual dependency wiring requires it, and record each change. No new service key, SystemGroup or Framework implementation is expected. `LobbyViewSystem` and `LobbyViewScope` remain the relevant composition points.

## 3. Phase A — Core contracts and Run identity

### A1. Publish candidates and prepare the chosen Main

**Interfaces to add:**

- `MainStartOptions(int loadoutKey, EntityKey defaultMainKey, IReadOnlyList<EntityKey> candidateKeys)`: immutable, defensive-copy contract with get-only `LoadoutKey`, `DefaultMainKey`, `CandidateKeys`.
- `IRunStartService.ReadMainStartOptions(int loadoutKey) -> MainStartOptions`: no owned IDs, Run creation, mutable Buffer writes or RNG consumption. Invalid authored configuration fails explicitly.
- `IRunStartService.StartNewRun(int loadoutKey, EntityKey selectedMainKey) -> RosterAcquireResult`: explicit selection; invalid choice returns failure before installation.
- Extend `PlayerLoadoutData` with copied `AllowedMainKeys` and a trailing `EntityKey[] allowedMainKeys = null` constructor argument. Null on legacy C# callers means the existing default only; an explicit empty list, duplicate, None or missing default is invalid.
- Add SO `UnitDataSO[] allowedMainCharacters`, convert references to keys in `GetData()`, and explicitly author Akimbo in the operating loadout. An empty/null authored list is an authoring error, not silent production fallback.
- Add the selected-key overload of `PlayerLoadoutRosterPlanner.Plan` alongside its existing overload with both `out PlayerLoadoutData` and `out IReadOnlyDictionary<EntityKey, CharacterDefinitionFacts>`; route default and selected preparation through one implementation.

**Tests:** new `T/Framework/MainStartSelectionTests.cs`; existing `T/Framework/RunDeckInitializationTests.cs`, `InGameDataTableTests.cs`, `DataBaseAssetTests.cs`.

- [ ] Add behavior tests using two valid synthetic Main definitions: candidate read before any Run returns the configured default/list with unchanged Buffers; mutating the source list cannot change the result; explicit selection installs the requested definition with a fresh RunId/new Main ID and unchanged authored Sub/economy/hand configuration.
- [ ] Add rejection tests for missing registration, retired key, non-Main role/category, player-ineligible candidate, duplicate/default-missing list, incompatible effective roster, and a source change during preparation. Assert previous Run/roster/deck/currency/enhancements/revisions remain identical on rejection. Run and record behavioral failures.
- [ ] Implement shared candidate validation and effective initial-roster preparation. Replace only the Main grant before existing acquisition/initial-deck rules; never start the default Run and swap Main afterward. Preserve existing source revalidation and `PublishNewRun(NewRunPreparation)`.
- [ ] Update all `IRunStartService` implementations/test doubles for the new contract. Verify existing one-argument callers still use the authored default. Run the A1 classes and real asset checks; require zero new failures.

### A2. Enforce same-Run Main identity at both boundaries

**Interfaces:**

- Add `DeckRules.EvaluateEdit(IReadOnlyList<CharacterInstanceId> requested, IReadOnlyList<OwnedCharacterRecord> owned, IReadOnlyDictionary<EntityKey, CharacterDefinitionFacts> definitions, CharacterInstanceId currentMainId) -> DeckSelectionDecision`. The service still wraps this pure decision in its existing `DeckEvaluationResult`.
- Append `DeckFailureReason.MainLockedForRun` without renumbering existing values.
- Keep `IRunDeckService.Evaluate(DeckEditRequest)`, `Confirm(DeckEditRequest)` and `RunDeckTransactions.TryPublish(DeckEditRequest, IReadOnlyList<CharacterInstanceId>)` signatures.

**Tests:** `T/RunDeckRuleTests.cs`, `RunDeckContractTests.cs`; `T/Framework/RunDeckServiceTests.cs`, `RunDeckBufferTests.cs`, `RunDeckIntegrationTests.cs`, `RunDeckBattleMembershipTests.cs`.

- [ ] Add tests: replace selected Main A with a valid owned Main B in the same Run; Evaluate, Confirm and direct transaction publication all reject with no state/revision change. Equal stats do not substitute for the same instance ID. Confirm ordinary invalid/stale reasons retain precedence.
- [ ] Add tests that the same Main permits valid Sub add/remove/order changes, same-deck confirmation remains a revision-preserving no-op, and a successful new Run may establish a different Main. Run and record failures.
- [ ] Use generic `DeckRules.Evaluate` first, then compare the accepted canonical Main against the captured current Main. Confirm reuses that evaluation. Add the final identity check to `TryPublish` before preparation/install, retaining version/reentrancy/battle/outcome checks. Do not duplicate eligibility calculation inside Buffer.
- [ ] Migrate fixtures that used same-Run Main replacement: create the intended Main through A1's selected new-Run path; use Sub edits when a test needs a deck revision change. Preserve downstream battle-membership assertions instead of deleting them.
- [ ] Run all A2 classes and the complete Core EditMode group. Record exact failures against a baseline, update the existing [Core/View handoff](../handoffs/2026-09-24-view-core-data-boundary-handoff.md) with actual interfaces without overwriting concurrent work, and stop for Phase A review.

## 4. Phase B — Lobby selection and departure

### B1. Own one Lobby draft and display real candidates

**Interfaces:**

- `LobbyMainSelectionDraft(MainStartOptions options, Func<bool> isDeparturePending)`, with get-only `BaseLoadoutKey`, `SelectedMainKey` and immutable `CandidateKeys`; `bool TrySelect(EntityKey key)` rejects unavailable keys or a pending departure.
- `LobbyViewScope` owns one draft for its attached Lobby session. Popup and departure presenters reference it; popup Hide/Clear must not dispose/reset it.
- Bind a narrow candidate-read delegate from the existing Run-start service during composition. View receives Core-validated keys and resolves names/icons through View DB; it cannot call the mutation API through this delegate.
- Extend `LobbyDeckDisplayModel` with `SelectedMainKey`. Keep information-card pinning and hover independent from an explicit selection action through existing `IUIInputReceiver`/UIInputBinding conventions.

**Tests:** new `T/Framework/LobbyMainSelectionDraftTests.cs`; existing `LobbyDeckPresenterTests.cs`, `LobbyDeckPopupControllerTests.cs`, `LobbyDeckUITests.cs`, `LobbyViewSystemTests.cs`, `LobbySceneAssetTests.cs`, `LobbyDeckBootSmokeTests.cs` under `T/Framework`.

- [ ] Add tests: authored default is selected before any Run; popup close/reopen preserves the choice; pin/hover does not change it; a new Lobby scope resets it to the authored default; old/disposed input cannot alter the current scope. Assert no Core writes/Run creation.
- [ ] Add tests for a single candidate, unavailable keys, pending departure, and a missing required View row. Missing presentation data must surface as a configuration problem, not fabricate another candidate. Run and record failures.
- [ ] Replace the empty preparation model with projected candidate cards. Add a selected marker and explicit selection control to the existing popup/prefab; preserve details access, close/cancel behavior, input layers and popup lifetime. Keep existing trait/stat panels empty when the candidate contract does not provide them.
- [ ] Connect the shared draft in `LobbyViewSystem`/`LobbyViewScope`; keep setup/cleanup with the current owner and supply no new service hierarchy. Verify actual prefab references, targeted tests and Boot smoke. Do not edit FieldCamera or unrelated scene layout.

### B2. Freeze selection in the existing departure request

**Interfaces:**

- Add immutable `BaseLoadoutKey` and `SelectedMainKey` to `LobbyDepartureRequest`; constructor becomes `LobbyDepartureRequest(int number, int baseLoadoutKey, EntityKey selectedMainKey)`.
- Change `ILobbyViewBufferWriteSink.Submit` to `Submit(int baseLoadoutKey, EntityKey selectedMainKey)`. Update all callers/fakes explicitly; Buffer must not resolve a default key from DB.
- Pending submissions return the same request and never replace its payload. Preserve existing request-number, cancellation, result and Job-completion semantics.

**Tests:** `T/Framework/LobbyDepartureTests.cs`, `LobbyDepartureViewTests.cs`, `LobbyDepartureIntegrationTests.cs`, `LobbyDepartureBootSmokeTests.cs`, `LobbyArtifactDepartureTests.cs`.

- [ ] Add tests: submit A then attempt B/submit twice -> original key/request/one install; cancel before execution -> zero installs; change after cancellation/drain -> a new request; retained battle -> no Core start; normal rejection -> no transition and previous Run preserved. Run and record failures.
- [ ] Make `LobbyDeparturePresenter` capture the shared draft at Submit. Disable choice changes while the actual request is pending. `LobbyRunStartJob` calls A1's selected overload from the immutable request, after existing battle/runner/artifact prerequisites.
- [ ] Extend `LobbyDepartureFlow.RequireInstalledRoster` to check that the installed deck Main ID belongs to the acquired result and its CharacterKey equals the captured choice, in addition to all current receipt/state checks. Read the existing RunDeck sink through the current composition boundary.
- [ ] Test failure/delay after Core success: existing accepted request/result is retained; processing/transition attempts do not invoke StartNewRun twice. Preserve the existing fault/transition policy rather than introducing automatic recovery.
- [ ] Run B1/B2 tests and Lobby/artifact/fault regressions. Record selected contract, prefab/input wiring and outstanding manual checks, then stop for Phase B review.

## 5. Phase C — Stage controls and integration

### C1. Preserve Main while editing Subs

**Interfaces:** retain `DeckEditDraft.Toggle(CharacterInstanceId)`, `RemoveAt(int)`, `CreateRequest()` and existing Core request shape. Use `Basis`'s current Main ID; any fixed/selectable fields in the display model are derived presentation facts.

**Tests:** new `T/Framework/StageMainLockViewTests.cs`, plus existing tests that instantiate `DeckEditDraft`, `DeckEditPopupController` or `DeckEditUI` (locate current callers before execution).

- [ ] Test fixed slot 0: information display remains available; remove/toggle/replacement attempts cannot change Main; a reserve Main is not offered as a replacement. Existing valid Sub add/remove/order/confirm operations remain usable.
- [ ] Test direct/stale rejection with `MainLockedForRun`: show the reason and refresh authoritative facts without discarding Core's stale reasons. Run and record failures.
- [ ] Guard Main edits in the draft and derive unavailable replacement controls in the shared model builder. Add the Korean locked-Main message in `DeckEditText`. Do not close the entire `StageDeckEditWindow`, remove owned records or calculate eligibility in UI.
- [ ] Verify the new cases and existing Stage deck/popup/input regressions. Core A2 remains the authority even if UI protection is bypassed.

### C2. Prove the installed identity survives the Run

**Tests:** `T/Framework/RunDeckBattleMembershipTests.cs`, `RunDeckInitializationTests.cs`, `LobbyDepartureIntegrationTests.cs`, `EnhancementShopServiceTests.cs`, `ShopVisitIdentityTests.cs`, `RecoveryContextLifecycleTests.cs`, `RunGoldLifecycleTests.cs`, `RunTerminationAdmissionTests.cs`.

- [ ] Add/extend an integration fixture: select synthetic Main B in Lobby -> submit -> fresh Run with B -> edit Subs -> shop/recovery/battle use B's exact ID -> enhance the selected Main without changing that ID -> Run ends -> next accepted Lobby departure may select A. Keep reserve-unit enhancement eligibility unchanged.
- [ ] Cover rejected preparation preserving the prior Run and successful replacement retaining the existing `LastSummary` policy. Test missing/stale captured facts and no repeated reward or installation.
- [ ] Run the complete Core and Framework EditMode suites after targeted cases pass; compare any failures by exact test name/message with a recorded baseline. Also run DataBaseAssetTests and affected scene/prefab checks.
- [ ] Perform manual operating-content acceptance: only approved Akimbo candidate appears; choice/selected marker/details work; popup reopen retains selection; departure runs once; Main stays fixed in Stage while Subs remain editable; return-to-Lobby/new departure follows current policy. Do not claim production multi-Main acceptance from synthetic tests.
- [ ] Record evidence and the Framework/Factory/ServiceKeys/SystemGroup/asmdef audit. Stop after reporting Phase C; unresolved manual cases remain explicitly pending.

## 6. Verification procedure and evidence

At each authorized implementation Phase, inspect checkout/diff and current contracts first. Capture a relevant baseline before changing behavior. Re-scan constructors, service implementations and direct Submit/fixture callers because concurrent work can add them.

Use the open Editor's Test Runner when it owns the project. Otherwise, run the installed Editor through the approved AppData-capable path; an example Phase A filter follows. This is an execution instruction, not a test run performed while writing this plan.

```powershell
$mainPlanEvidence = Join-Path $env:TEMP 'ProjectL-MainSelection-A'
New-Item -ItemType Directory -Force -Path $mainPlanEvidence | Out-Null
& 'D:/Program Files/Unity/Hub/Editor/6000.0.73f1/Editor/Unity.exe' -batchmode -nographics -projectPath 'D:/My_ProjectWork/Github_desktop/Project_L' -runTests -testPlatform EditMode -testFilter 'ProjectL.Tests.Core;ProjectL.Tests.Framework.MainStartSelectionTests;ProjectL.Tests.Framework.RunDeckServiceTests;ProjectL.Tests.Framework.RunDeckBufferTests;ProjectL.Tests.Framework.RunDeckInitializationTests;ProjectL.Tests.Framework.RunDeckIntegrationTests;ProjectL.Tests.Framework.RunDeckBattleMembershipTests;ProjectL.Tests.Framework.InGameDataTableTests;ProjectL.Tests.Framework.DataBaseAssetTests' -testResults "$mainPlanEvidence/results.xml" -logFile "$mainPlanEvidence/editor.log"
```

For B/C, select the classes listed in their tasks; C2's full filter is `ProjectL.Tests.Core;ProjectL.Tests.Framework`. Successful process launch is insufficient: require complete NUnit XML, expected tests actually selected, no new failures, and no relevant compilation errors. A crash without complete XML is no verdict. Headless automation does not replace UI/manual acceptance.

## 7. Change, understanding and validation scope

| Change case | Before | Planned after | Required regression |
|---|---|---|---|
| Add an approved starting Main later | No active Lobby candidate path | Existing allowed list + definition/View content; no new departure algorithm | Candidate validity, default membership, selected initialization and asset coverage |
| Change a Sub deck during a Run | General deck edit can also replace Main | Same editor and contracts, fixed Main identity | Sub legality/order, no-op/stale revisions and selected battle membership |
| Change Main choice before departure | Placeholder popup; fixed default start | One Lobby draft -> immutable request -> existing Run-start transaction | Popup lifetime, duplicate/cancel, failed prepare and transition behavior |
| Enhance the selected Main | Per-owned-instance enhancement | Same ownership and identity, unchanged eligibility policy | Main ID preserved; shop/recovery/battle see the same owned instance |

These are expected locality improvements, not measured implementation results. The feature implementer must understand candidate validation, departure lifetime and Run identity; damage formulas, unrelated trait executors and camera internals should not need modification. Necessary shop/recovery/battle integration checks remain because they consume the captured Main identity.

SOLID/SSOT review: separate candidate calculation, draft interaction, request lifecycle and storage installation by existing owners; extend their contracts without creating duplicate state. Preserve interface guarantees when adding overloads and updating fakes. Inject a narrow candidate-read dependency into View and let normal composition changes wire it. Do not split files merely to increase class/interface count.

## 8. Execution record

| Phase | State | Evidence |
|---|---|---|
| Document preparation | Complete | Existing design, Core/Lobby APIs, file paths and test classes checked; no product implementation or Unity execution |
| A | Not started | Awaiting scoped implementation authorization and design-policy acceptance |
| B | Not started | Depends on A |
| C | Not started | Depends on A and B |

Record actual changed files, API signatures, XML/log locations, exact failures, manual status and structural audit at each Phase boundary. Do not carry older passing counts forward as verification of this change.

