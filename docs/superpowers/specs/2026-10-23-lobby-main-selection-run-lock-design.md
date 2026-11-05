# Lobby Main Selection and Run Identity Lock

Date: 2026-10-23  
Status: Proposed design for user review; no product implementation

Implementation plan: [Lobby Main Selection and Run Identity Lock](../plans/2026-10-23-lobby-main-selection-run-lock-plan.md). The plan records task boundaries and verification; it does not mark the proposed policies or implementation as approved.

## 1. Requirement and scope

The user requires the Main character to be selected in the starting Lobby and remain unchanged until the Run changes. StageSelect currently exposes a general deck editor that can replace the selected Main when another eligible owned Main exists.

This design changes **Main selection authority and lifetime**, while retaining existing StageSelect Sub selection and ordering. It does not introduce new characters, unlock progression, save/load, arbitrary starting Sub composition, combat rules, or enhancement-shop eligibility changes. Multi-cell characters remain intentionally unsupported.

The confirmed requirement is the Lobby-only selection and per-Run identity invariant. Candidate authoring and API names below are proposals for approval, not existing APIs or separately confirmed gameplay decisions.

## 2. Current implementation findings

### 2.1 Akimbo is an authored default, not a forced deck-editor branch

| Layer | Current evidence | Meaning |
|---|---|---|
| Operating unit | `Unit_Akimbo.asset`: key 10, MainCharacter role, Main category, PlayerOnly | Akimbo is the current valid Main definition |
| Operating catalog | Akimbo is registered in `UnitDataTable.asset` | It is an actual runtime definition |
| Initial selection | `PlayerLoadout_1.asset.mainCharacter` references Akimbo | The starting Main comes from authored data |
| Core initial roster | `PlayerLoadoutRosterPlanner.Plan` reads `loadout.MainCharacterKey` | The planner does not select `Unit_Akimbo` by a hardcoded gameplay branch |
| Lobby departure | `LobbyRunStartJob` calls `StartNewRun(StagePlan.DefaultLoadoutKey)` | The departure path is currently fixed to loadout 1 |
| Stage deck editing | Generic owned-ID selection and `DeckRules` | Another valid owned Main can replace the selected Main |

Only Akimbo is currently an eligible registered Main. The old `Unit_MainCharacter` key 14 asset still exists, but it is not registered and `PlayerMainDefinitionPolicy` explicitly rejects its use as a player definition. Sniper's current role/category combination does not make it an eligible Main. Do not reactivate either as part of this design.

The inspected default loadout grants Akimbo, ShotGun, Ballista, and Cowboy; its initial deck indices are 0,1,2,3, starting hand size is 4, and starting Run currency is 5000. These are current authored values, not new requirements to hardcode. Preserve them unless separately changed.

Development fixtures also reference Akimbo explicitly. They are separate from operating deck selection and should retain deliberate fixture semantics.

### 2.2 StageSelect authorizes general deck edits

```text
DeckEditUI
  -> DeckEditPopupController.SelectOwned / RemoveSlot
  -> DeckEditDraft.Toggle / RemoveAt
  -> IRunDeckService.Evaluate / Confirm
  -> RunDeckTransactions.TryPublish
  -> RunDeckBuffer
```

`DeckRules` validates ownership/definition compatibility, exactly one Main, the existing card limit, and Main-first canonical ordering. It does not require the new Main to equal the installed Main. `RunDeckTransactions` checks versions, candidate identity, ownership and Main role, but not `canonicalIds[0] == current.MainId`.

`StageDeckEditWindow` owns the idle-StageSelect availability window. Closing this entire window would also disable Sub editing and would exceed the requirement.

### 2.3 Lobby selection is not implemented yet

`LobbyDeckPopupController.Show` currently supplies an empty card model and a preparation message. `LobbyDeckPresenter` pins information cards; its selected card is not an authoritative Run Main choice. There is no operating candidate/confirmation connection to reuse as if it were complete.

### 2.4 Run creation occurs at two existing boundaries

- Ended combat Run: `CombatNewRunJob` prepares a default fresh Run before transitioning to Lobby.
- Lobby departure: `LobbyRunStartJob` always calls `StartNewRun` again, replacing the currently installed Run before StageSelect.

This design preserves that existing lifecycle. It does not silently move Run creation or remove the pre-Lobby preparation. Lobby editing is a draft for the next departure; it never mutates the Main of an already installed Run. Departure installs the newly selected Main into a fresh Run.

Removing the extra pre-Lobby preparation could be considered separately, with its own recovery, summary and lifecycle regression review. It is not a prerequisite for this change.

## 3. Alternatives considered

| Approach | Assessment |
|---|---|
| Hide Main controls only in StageSelect | Insufficient: direct or stale requests can still replace the Main. |
| One complete loadout asset per Main | Reuses the current `StartNewRun(int)` shape, but duplicates otherwise identical Sub/economy settings and makes a Main choice also select a full loadout. Useful only if complete presets become a requirement. |
| Capture a Lobby Main definition choice and apply it during new-Run preparation | **Recommended.** Keeps Main choice independent of the active Run and preserves existing Sub editing and atomic installation. |

## 4. Proposed invariant and sources of truth

For an installed Run R, every successful same-Run deck edit must preserve the selected Main's **CharacterInstanceId**:

```text
before.RunId == after.RunId  =>  before.MainId == after.MainId
```

Use the existing `RunDeckSnapshot.MainId` as the source of truth. Do not add a second `LockedMainId`, a View-owned lock boolean, or a special Akimbo rule. Checking equality at every same-Run publication preserves the initial Main by induction.

Before installation, the Lobby selects a **definition key** (`EntityKey`). After installation, the Run owns a newly allocated **instance ID**. Never carry an old Run's owned ID into a new Run or infer identity from equal character stats.

| Fact | Owner and lifetime |
|---|---|
| Permitted starting Main definitions | Authored starting setup, validated by Core |
| Unsubmitted Main choice | One Lobby-scoped draft owner; persists across opening/closing its popup |
| Submitted departure choice | Immutable payload on the current `LobbyDepartureRequest` |
| Installed Main identity | `RunDeckSnapshot.MainId`, until Run replacement/reset |
| Health, experience, enhancement and other owned state | Existing keyed Character/Run facts; not copied into the Lobby draft |

## 5. Candidate and departure contract

### 5.1 Candidate read

Extend the existing Run-start service with a narrow read of starting Main candidates. Avoid adding a new service hierarchy merely for this screen. Suggested names such as `ReadMainStartOptions(loadoutKey)` and `MainStartCandidate` are provisional.

Proposed candidate source: an explicit allowed-Main list in the existing starting setup, with the existing `mainCharacter` as its default. Initially author only Akimbo. This avoids exposing every registered or development-only Main automatically. The exact set of future candidates and unlock rules remains outside this scope.

Core validates registration, player eligibility, Main role/category, retired-key exclusion, and compatibility with the base loadout. The default must be an allowed candidate. Invalid authored configuration should be diagnosed before departure, not silently replaced with Akimbo.

Return immutable rule/stat facts needed by the Lobby card. View resolves names/icons/models through View DB and does not read InGame DB or reproduce eligibility rules.

### 5.2 Draft behavior

- Opening/closing the popup does not create a Run, allocate owned IDs, or change the installed deck.
- Information-card pinning remains distinct from committing a Main choice; add an explicit selection action/state.
- The owning Lobby controller retains the draft across popup close/reopen during that Lobby session.
- On a new Lobby session, use the authored default. Cross-session remembered choice is not included unless requested.
- With the current single candidate, the UI shows that candidate as selected; it does not invent alternative playable characters.

### 5.3 Immutable departure request

Capture `{ baseLoadoutKey, selectedMainKey }` in `LobbyDepartureRequest` at Submit. Keep the existing request number, state machine, pre-execution cancellation and receipt lifetime.

Once submitted, freeze the payload and disable selection changes. A duplicate submission must reuse the same pending request without a second Run installation. A different choice must not overwrite a queued/running request; cancel before execution and submit a new request if changing it is allowed at that point.

`LobbyRunStartJob` reads the captured selection, not a mutable presenter or a new default lookup. Flow still waits for artifact work and the runner, and the Job still rejects a retained battle before calling Core.

### 5.4 Atomic new-Run installation

Propose an explicit selected-Main entry point, for example:

```text
IRunStartService.StartNewRun(baseLoadoutKey, selectedMainKey)
```

Core must:

1. Read and validate the base loadout and selected candidate.
2. Substitute only the initial Main definition; preserve authored Sub grants, initial deck indices, currency and hand size.
3. Run the existing roster and initial deck rules against the effective definitions, including duplicate/role/ownership constraints.
4. Revalidate the current source state before publication.
5. Publish through the existing `NewRunPreparation` / `PublishNewRun` transaction, allocating new instance IDs and installing Main/deck/Run identity atomically.

Keep `StartNewRun(int)` as an explicit authored-default route for existing reset/development callers; route both APIs through the same preparation logic. The selected overload must not silently fall back when the selected key is invalid.

Extend `LobbyDepartureFlow.RequireInstalledRoster` to verify the installed Main corresponds to the captured choice, in addition to checking the actual acquisition result. After Core succeeds, transition retry must use the successful request; it must not call `StartNewRun` again for that same request.

## 6. Enforce the lock without disabling Sub editing

### Core evaluation

Keep initial deck formation and same-Run deck editing distinct. Preserve generic `DeckRules.Evaluate` where it describes formation; add an edit-specific check using the captured current deck.

After ordinary formation validation/canonicalization, reject a different Main ID with a proposed `DeckFailureReason.MainLockedForRun`. Existing invalid-input reasons, such as missing Main or stale Run, should remain precise. `Confirm` must use the same rule as `Evaluate`.

### Transaction boundary

`RunDeckTransactions.TryPublish` must also require the proposed Main ID to equal the currently installed Main ID for that Run, before its callback-free install tail. This is a storage identity invariant, not a second implementation of eligibility or deck composition policy. Retain all RunId, RosterRevision, DeckRevision, battle and outcome checks.

New-Run publication is a separate operation and may establish a new Main ID. Same-deck no-op behavior must preserve revisions as today.

### Stage View

Keep general `CanEdit`/Stage availability for Sub edits. Show the Main in slot 0 as fixed, permit its information display, and suppress removal/replacement actions. Filter other owned Main records out of selectable replacements or show them as unavailable; do not delete them from ownership.

`DeckEditDraft` should preserve the captured Main while toggling/removing/reordering Subs. This improves interaction but does not replace Core enforcement. Return a clear locked-Main message for a rejected stale/direct request and refresh from Core facts.

## 7. Interaction with enhancement, battle and recovery

- Enhancing the chosen Main changes the owned instance's enhancement, not its ID, so it remains allowed under the lock.
- Acquiring another Main during a Run does not make it selectable for that Run. Acquisition availability itself is not changed by this design.
- Enhancement offers currently consider all eligible owned instances, including reserve units. Do not change that to selected-deck-only as an accidental consequence of the lock.
- Shop/recovery visits and battle entry already capture the selected Main ID. Preserve those captured identities and their stale-context checks.
- New Run success resets Run-owned enhancement state through existing roster replacement. Rejected preparation must preserve the previous Run, inventory, currency, deck and enhancement facts.
- Preserve `LastSummary` and existing end-of-Run destinations. Choosing a Main does not replay rewards or mutate the completed Run summary.

## 8. Ownership and implementation boundaries

| Area | Expected change | Owner |
|---|---|---|
| Starting setup | Explicit allowed candidates and default validation | DB authoring + Core adapter |
| Candidate projection / selected start | Pure validation and preparation using selected definition | Core RunStart / roster planners |
| Same-Run edit invariant | Evaluate/Confirm and final identity-preserving publication | Core + Buffer transaction |
| Draft / captured departure payload | Popup selection, immutable request, pending-input behavior | Lobby View + input Buffer contract |
| Departure orchestration | Captured payload -> existing Job -> verified install -> Stage | Lobby Flow/Plan/Job |
| Fixed Stage Main controls | Preserve info access and Sub editing | Stage View/UI |

Follow existing Framework service registration and Group access. Reuse registered `UIBase` popup lifecycle and existing input receiver conventions. Do not move rule calculation into View, put scene-name checks in pure Core, or create a second authoritative deck.

Implementation should be planned in separate reviewable phases after approval: (1) Core identity invariant and selected-start contract; (2) Lobby candidate/selection/departure integration; (3) Stage fixed-Main presentation and integration acceptance. Phase 1 alone must not be reported as a complete Lobby selection feature.

## 9. Required validation

| Scenario | Required result |
|---|---|
| Same Run, different eligible owned Main | Evaluate, Confirm and direct transaction publication reject; no state/revision changes |
| Same Main, Sub add/remove/reorder | Existing allowed operations succeed |
| Same deck no-op | No unnecessary revision change |
| Old Run/roster/deck request | Existing stale reason and no mutation |
| Lobby draft close/cancel | Installed Run and Main unchanged |
| Departure selection changed after submission | Original immutable payload remains; no second start |
| Invalid/unregistered/retired/non-Main choice | Rejected before reset or installation |
| Successful chosen departure | Fresh RunId, new correct Main instance, preserved authored starting Sub/economy configuration |
| Failed chosen departure | Previous Run and enhancements remain intact; no Stage transition |
| Transition failure after successful preparation | No repeated Run creation for the same request |
| Battle/shop/recovery after departure | All use the new selected Main identity |
| Enhancement of selected Main | Same MainId and updated enhancement; no unlock of Main replacement |
| Run end then Lobby departure | Existing preparation/summary policy remains; next selected Run installs once for its request |

Use two valid Main definitions only in test fixtures until another production Main is approved. Existing selected-Main tests that currently swap Main within a Run, including `RunDeckBattleMembershipTests`, must establish their selected Main at new-Run creation instead; preserve their downstream membership assertions.

## 10. Evidence and source map

Current-source audit and authored asset/GUID checks were performed in this session. Five existing pure Core fixture groups (deck rules/contracts and enhancement rules/state/resolution) passed 48 tests in a .NET 10 managed harness. This validates existing behavior only, not the proposed lock. Unity, Framework integration and manual Lobby/Stage behavior were not executed in this session.

- [PlayerLoadout_1.asset](../../../Assets/ProjectL/DataBaseSO/InGameGroup/Loadout/Item/PlayerLoadout_1.asset), [Unit_Akimbo.asset](../../../Assets/ProjectL/DataBaseSO/InGameGroup/Unit/Item/Playerable/Unit_Akimbo.asset), [UnitDataTable.asset](../../../Assets/ProjectL/DataBaseSO/InGameGroup/Unit/UnitDataTable.asset).
- [PlayerMainDefinitionPolicy.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Common/PlayerMainDefinitionPolicy.cs), [PlayerLoadoutRosterPlanner.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CharacterSystem/PlayerLoadoutRosterPlanner.cs), [RunStartService.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/RunSystem/RunStartService.cs).
- [RunDeckService.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CharacterSystem/RunDeckService.cs), [RunDeckTransactions.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/Core/RunDeckTransactions.cs), [ProgressTransactions.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/Core/ProgressTransactions.cs).
- [LobbyDeckPopupController.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Lobby/Deck/LobbyDeckPopupController.cs), [LobbyDepartureRequest.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/LobbyView/LobbyDepartureRequest.cs), [LobbyRunStartJob.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Lobby/Jobs/LobbyRunStartJob.cs), [LobbyDepartureFlow.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Lobby/LobbyDepartureFlow.cs).
- [StageDeckEditWindow.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Stage/StageDeckEditWindow.cs), [DeckEditDraft.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/StageMap/Deck/DeckEditDraft.cs), [DeckEditUI.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/DeckEditUI.cs).
- [CombatExitJobs.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Combat/Jobs/CombatExitJobs.cs), [RunDeckBattleMembershipTests.cs](../../../Assets/Tests/EditMode/Framework/RunDeckBattleMembershipTests.cs).

## 11. Decisions reserved for approval

The recommended contract is definition-key selection in Lobby, immutable departure input, and instance-ID locking for the installed Run. The user should review the proposed explicit candidate list and default-on-new-Lobby behavior before implementation. No additional production Main, remembered cross-session preference, unlock rules, or reset-lifecycle redesign is assumed.
