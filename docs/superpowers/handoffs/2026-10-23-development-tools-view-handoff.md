# Development Tools — View Handoff

Date: 2026-10-23
Audience: View/UI implementer
Status: Local handoff prepared; not sent. A/B/C implementation is available in the current working tree. Final Phase C scene/manual acceptance remains open.
Scope of this update: documentation. A development panel, button layout, hotkey and scene wiring remain a separately scoped View task.

## 1. What View consumes

Use the public `IDevelopmentTools` facade exposed by `IInGameViewSystemGroupService.DevelopmentTools`. View owns the development controls, input binding and presentation of returned results. Core owns eligibility, grants and navigation preparation; Buffers own the installed gameplay facts.

~~~text
Development UI input
    -> existing View consumer
    -> ViewGroup.DevelopmentTools
    -> internal Core commands
    -> existing/narrow Buffer transactions
~~~

The four operations are synchronous. Call them from the existing Unity input/lifecycle context. A normal runtime assembled by `FrameworkRuntimeFactory` already includes the feature in Editor and Development builds. No extra bootstrap call, runtime enable flag or second facade instance is needed.

References:

- [Public facade](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Dev/IDevelopmentTools.cs)
- [Context](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Development/DevelopmentContext.cs) and [results/reasons](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Development/DevelopmentResults.cs)
- [Approved design](../specs/2026-10-23-development-tools-design.md) and [implementation/evidence record](../plans/2026-10-23-development-tools-plan.md#15-phase-c-delivery-and-outstanding-acceptance--2026-10-23)
- [Core/View ownership record, section 30](2026-09-24-view-core-data-boundary-handoff.md#30-optional-development-tools-proposal-and-abc-implementation-2026-10-23-local-not-sent)

## 2. Obtain the facade once through the existing hierarchy

In an existing View module's `PostBindService`, obtain the existing InGame parent and retain only its facade reference. Merge these fragments into the module's existing lifecycle methods; keep its ordinary binding and cleanup intact.

~~~csharp
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Dev;
#endif
~~~

~~~csharp
#if UNITY_EDITOR || DEVELOPMENT_BUILD
private IDevelopmentTools _developmentTools;
#endif

public void PostBindService(ISystemServiceStoreReader resolver)
{
    // 기존 서비스 바인딩은 그대로 유지한다.
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    _developmentTools = resolver.GetService(SystemGroupsService.InGame)
                                .ViewGroup.DevelopmentTools;
#endif
}
~~~

The existing method uses `ProjectL.Framework.Composition.ISystemServiceStoreReader` and `ProjectL.Systems.ServiceKeys.Runtime.SystemGroupsService`.

Pass the retained reference to the appropriate development input owner through normal View composition. A scene screen should not retain the root resolver. The View factory/group owns the facade's lifecycle; a panel releases its own reference/input bindings on teardown and does not call `DevelopmentTools.Shutdown` itself. Bind a new runtime's facade when that runtime is created.

`IDevelopmentTools` is registered once inside the View builder. Retrieve it through `InGame -> ViewGroup -> DevelopmentTools`; it is not a runtime-root service. No new ServiceKey, root export or Framework lookup change is required.

### Availability of the API itself

- The interface, property and implementation exist only under `UNITY_EDITOR || DEVELOPMENT_BUILD`. Guard development-specific using directives, fields, methods and call sites. A runtime `if` alone cannot make a Release reference compile.
- Lower View and paired InGame factories default `includeDevelopmentTools` to `false`. A null facade is an intentional, supported omission in those compositions; hide or disable the optional development controls.
- The full runtime opts in automatically under the build guard. If an explicitly included facade has no matching Core provider/commands, binding throws. Missing required Buffer capabilities also throw. Treat these as configuration errors rather than substituting a new service or silently falling back to direct writes.
- The API is not an Editor menu or an out-of-play-mode asset editor. It requires a bound runtime and a ready StageSelect view.

## 3. Read a fresh context for each user action

`ReadContext()` returns an immutable observation:

| Member | Meaning |
|---|---|
| `RunId` | Observed Run identity |
| `ChapterKey` | Current generated chapter/map key |
| `HasSelection` | Whether a real stage node is currently selected |
| `NodeId` / `StageKey` | Current selected map node / its bound stage definition, not the requested destination |
| `AvailabilityReason` | Current refusal reason, or `None` |
| `IsAvailable` | Derived from `AvailabilityReason == None` |

Use the returned instance as the command's `expected` argument. Do not construct a context from UI labels, cache it across Run/chapter transitions or treat it as a lock, revision counter or single-use token. The implementation rechecks current state on submission.

A gold-input handler inside the existing consumer can follow this pattern:

~~~csharp
#if UNITY_EDITOR || DEVELOPMENT_BUILD
private void GrantRequestedGold(int amount)
{
    // 하위 조립에서 개발 기능을 의도적으로 생략할 수 있다.
    var tools = _developmentTools;
    if (tools == null)
    {
        return;
    }

    // 사용자 입력마다 현재 수명과 선택을 다시 읽는다.
    var context = tools.ReadContext();
    if (!context.IsAvailable)
    {
        UnityEngine.Debug.Log($"개발 명령 사용 불가: {context.AvailabilityReason}");
        return;
    }

    var result = tools.GrantGold(context, amount);
    if (!result.Applied)
    {
        UnityEngine.Debug.Log($"골드 지급 거절: {result.Reason}");
        return;
    }

    UnityEngine.Debug.Log($"Run 골드: {result.Before} → {result.After}");
}
#endif
~~~

The log calls illustrate result handling; the eventual UI should present equivalent feedback. Each actual input submits one operation. There is no request queue, completion callback or idempotency key: two successful grant calls are two grants. Do not issue mutations from Tick, redraw callbacks or automatic retry loops.

### Runtime admission

Version 1 operates only while StageSelect is idle. It requires:

- An attached, live, fully displayed map scope.
- No marker presentation in progress and no open Shop, recovery or deck-edit popup.
- No pending StageView input, including a command that Core has handled but the existing View observer has not yet consumed.
- The existing LifeCycle admission window to permit the operation: no loading, pending job/request or conflicting interaction.
- An active, in-progress Run and no retained battle. A completed battle still blocks calls while `HasBattle` remains true.

Before binding, after shutdown, during redraw or after a runtime fault, calls are unavailable. The facade may return generic `Unavailable` before Core can provide a more specific reason; do not infer a particular blocker from that value. Let existing Flow/observers complete their work. Development UI must not clear their buffers, close ordinary popups or reset Flow to force admission.

## 4. Operation and result contracts

| Method | Effect | Useful result fields |
|---|---|---|
| `GrantGold(context, amount)` | Adds a strictly positive amount to the single Run balance, `CurrencyReadSink.Amount`; preserves `RunSpent`. Zero/negative input and overflow are refused. | `Applied`, `Reason`, nullable `Before` / `After`. Balances are null on refusal. |
| `GrantUnit(context, key)` | Calls ordinary `Characters.Acquire(key)` at default initial level 1. Normal ownership and category limits apply. Adds owned roster data; selected deck and combat hand remain unchanged. | `Applied`, `Reason`, `Acquisition`. On success inspect `Acquisition.Acquired`; each record carries `Id` (`CharacterInstanceId`), `CharacterKey`, `Role` and `Level`. |
| `SkipNextNode(context, nodeId)` | Clears and selects the supplied eligible next regular Combat node, without battle completion or rewards. The node must have a successor. | Navigation fields below; the bypass list contains the skipped node. |
| `PrepareJumpToNode(context, targetNodeId)` | Prepares forward access within the current chapter. Clears supported intermediate nodes and selects the last predecessor. Leaves the final target uncleared and unselected. | Navigation fields below; an already eligible adjacent target is a successful no-op. |

Run gold is the existing shared Run balance, not a new developer or combat-only currency. Preserve ordinary balance/roster display reads. Use the existing deck editor to select a granted unit afterward; a successful grant does not promise that every acquired unit fits the current deck.

A `RosterRejected` result retains the native `Acquisition.FailureReason`. Earlier admission refusal leaves `Acquisition == null`. There is no top-level acquired-character-ID property: preserve identities from `Acquisition.Acquired` instead of locating an owned instance by `EntityKey` alone.

### Node identity and supported navigation

Both navigation arguments are `StageNodeId` values from the currently generated map. `StageKey` identifies a DB stage definition and is not interchangeable with a node ID. Obtain visible node identities from existing map read data. View should not query InGame DB or duplicate path/eligibility calculations.

`SkipNextNode` means the requested next node, not "finish the current battle." Ordinary navigation admission still applies, including current-stage completion where required.

The bypass rule applies to the skipped node and Jump intermediates: only regular Combat nodes with successors are supported. Boss, Shop, Event/recovery and terminal nodes cannot be bypassed. Those nodes **may be the final Jump target** when the normal navigation/content rules permit entry. No cross-chapter teleport or active-battle skipping is provided. Path choice belongs to Core; a View consumer supplies the destination, not its own calculated path.

For a chain `start -> 2 -> 3 -> 4` where 2/3 are supported Combat nodes:

- `SkipNextNode(context, new StageNodeId(2))` marks 2 clear and selects 2.
- `PrepareJumpToNode(context, new StageNodeId(4))` marks 2/3 clear and selects 3.
- Node 4 is still unentered. Its battle, shop visit or recovery context is created only by normal entry.
- An adjacent target needs no bypass: `Applied == true`, `Changed == false`, empty `BypassedNodes` and unchanged selection.

These operations do not publish victory/reward receipts, grant XP/fame/rewards, reset the Run, replace the selected deck or fabricate a noncombat visit.

## 5. Redraw completion and ordinary target entry

A navigation result exposes:

`Applied`, `Changed`, `Reason`, nullable `NavigationFailureReason`, `TargetNodeId`, `TargetStageKey`, `BypassedNodes` and `Current`.

`Changed` means an accepted result with at least one bypassed node. The facade then requests the existing map redraw exactly once. While that redraw is pending, fresh facade contexts are unavailable; the existing map display work restores readiness.

**After changed navigation succeeds, `result.Current` describes the committed Core state before the facade requests redraw. Its `IsAvailable` does not confirm that View has redrawn.** A no-op or refusal performs no development progress publication. For subsequent actions, use a fresh `tools.ReadContext()` and require its `IsAvailable`. Do not assume that one arbitrary frame is sufficient or request a second redraw from the caller.

An adjacent Jump no-op requests no redraw and does not itself close readiness. Grant operations also request no map redraw; their values are available through the existing read contracts.

After Jump, let the user enter the final target through the ordinary map interaction:

~~~text
StageMapInteraction
    -> StageViewCommandData.SelectStageNode(displayedChapter, targetNodeId)
    -> existing StageView input / Flow / Jobs
    -> ordinary Core entry
    -> existing result / entry observer / presentation
~~~

Do not call a battle entry API, set selection directly or manufacture completion/visit data from the development control. [StageMapInteraction](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/StageMap/StageMapInteraction.cs) already owns normal submission.

Path preparation and target entry are separate results. Normal entry may later reject an invalid deck or content. Previously bypassed progress remains committed; show the actual entry failure and keep normal result consumption. Never describe a successful Jump preparation as a successful battle entry.

## 6. Refusals and unexpected failures

| Reason | Consumer handling |
|---|---|
| `Unavailable` / `Busy` | Show current unavailability; wait for the existing lifecycle/presentation owner. Faulted/stopped instances will not recover in that same lifetime. |
| `NoActiveRun` / `RunEnded` / `BattleRetained` | Explain the current Run/battle restriction. Do not create/reset/end a Run or battle from the development panel. |
| `StaleContext` | Discard the old observation, refresh displayed state and require a new user action. |
| `InvalidAmount` / `Overflow` | Correct the amount; no grant was applied. |
| `RosterRejected` | Display the retained `Acquisition.FailureReason`. Do not weaken roster rules or auto-edit the deck. |
| `InvalidTarget` / `AlreadyPassed` / `UnsupportedNode` / `NoSupportedPath` | Display the returned reason and `NavigationFailureReason` when present. Do not recalculate a substitute route in View. |
| `None` | The operation was accepted. For navigation, separately inspect `Changed` and wait for actual redraw readiness when needed. |

Expected refusals have `Applied == false` and apply no partial development mutation. They are distinct from exceptions.

Null required arguments, invalid configured data and unexpected readiness/provider/redraw errors can throw. The facade reports unexpected call failures through the existing shared `RuntimeFaultBoundary` and rethrows. Subsequent retained calls are unavailable. Preserve that fault behavior; do not turn it into an ordinary "not applied" result.

A redraw exception can occur **after** Core has committed progress. It does not roll back that progress. Do not retry automatically, infer that nothing happened or connect a second StopInput callback.

## 7. View implementation boundary

Follow the repository's [UI/input and registration rules](../../../Assets/AGENTS.md):

- A new independent development screen/popup follows the existing `UIBase` / UISystem registration and lifetime model. Specify its UIId, parent, policy, creation/binding and teardown in the separate View task.
- Button input uses `UIInputBinding` / `IUIInputReceiver` and existing pointer/Submit dispatch. Avoid `Button.onClick.AddListener` and Inspector UnityEvent wiring.
- InputSystem owns hotkeys, action maps and input layers.
- Reuse existing View resources for names/icons and existing Buffer reads for displayed facts.
- On scene detach or runtime shutdown, unbind the screen's inputs and release its references. The facade's own availability checks also protect retained callers.

The handoff introduces no new UI layout or registration policy. It does not authorize changes to Framework, ServiceKeys, assembly definitions, ordinary Flow/gameplay rules, Core provider visibility or the existing map restoration behavior.

Ownership review: SSOT remains with Buffer facts and the existing map scope; SRP separates UI, facade admission, Core policy and transactions; OCP/LSP are supported by optional composition and release exclusion; ISP/DIP keep consumers on a focused contract. FW-001/002/003 retain child registration and real lifecycle forwarding, FW-004 keeps Core internals private, and FW-016 retains runtime ownership. A UI change should require the facade/input/display contracts, not reward settlement, path algorithms or store internals.

## 8. Acceptance checklist for the View task

Record these separately from the automated evidence already delivered:

- [ ] In Editor and a Development Player, the full runtime exposes the facade; an intentionally omitted lower composition hides/disables controls.
- [ ] Release builds have no references to development types, fields or calls.
- [ ] Controls reject actions before map readiness, during marker movement/redraw, with Shop/recovery/deck UI open, with pending input, and with an active or completed retained battle.
- [ ] A successful positive gold grant updates the displayed existing Run balance. Historical spending remains intact.
- [ ] A granted unit appears in the owned roster at level 1 and can be selected through the existing deck editor when normal deck rules permit; the grant alone leaves selected slots unchanged.
- [ ] A skipped node stays cleared after actual scene detach/rebind, with the marker at the stored current node. The current marker may hide that node's cleared indicator under existing presentation rules.
- [ ] Jump keeps the final target unentered; normal user interaction then enters it through the existing request/result path.
- [ ] A rejected target entry preserves the prepared predecessor/cleared flags and displays its original reason.
- [ ] A successful actual-scene startup -> StageSelect -> stage -> return path works, including existing audio assertions.
- [ ] Closing/reopening the development UI and changing runtime lifetime does not duplicate grants, retain an old context or leak input bindings.

## 9. Delivered evidence and open gates

The following are prior implementation results, not tests newly run for this documentation update:

| Evidence | Recorded result |
|---|---|
| Phase C focused tests | 57/57 passed: facade 30, composition 8, map/entry integration 19 |
| A+B+C feature cases | 223/223 passed |
| Combined required regression evidence | 318/350 passed; all 32 failures reproduced on pre-C code by exact name and complete message |
| Actual Windows Development/Release Players | Both succeeded, 0 errors and 3 warnings each |
| Packaged Release audit | None of the 19 development types or related guarded members/references remain; neither Player contains test assemblies |

The 32 baseline failures include 27 old map-fixture missing-marker errors, two roster failures, two runtime-composition failures and one Boot audio-preset failure. The actual Boot smoke reached Combat and prepared the battle before the audio assertion failed; the same failure was reproduced without C.

The paired-factory ordinary entry/return tests use real Flow/Job/Core with test loading/combat presentation. The paired View is inspected and disposed, but not initialized in that fixture. This is not proof of a full actual-scene round-trip or manual UI acceptance.

Final Phase C acceptance remains open for the actual-scene round-trip and manual View checks above. Full failure identities, filters, Player build/audit details and source-restoration evidence remain in [plan section 15](../plans/2026-10-23-development-tools-plan.md#15-phase-c-delivery-and-outstanding-acceptance--2026-10-23); keep that record as the verification source of truth.

This handoff changes documentation only. No UI/code/assets were implemented, no Unity tests were rerun, and no commit, push or external message was made.
