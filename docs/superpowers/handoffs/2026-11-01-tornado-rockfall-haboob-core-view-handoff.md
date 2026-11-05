# Tornado, Rockfall and Haboob — Core to View Handoff

- **Date:** 2026-11-01
- **Audience:** The View worker connecting stage gimmick visuals, playback, hover information and presentation data.
- **Implementation:** C# / Unity 6000.0.73f1.
- **Status:** Core is implemented and tested locally. View integration described below is still required.

This is the combined handoff for all three features. It explains what the player should see, which existing public facts provide that information, where the current View needs changes, and how to verify the result. You do not need to learn the other gimmicks' internal calculations to implement one feature.

The source of truth is the current code. The [T3 execution report](2026-11-01-tornado-stage-gimmick-t3-report.md) contains implementation history and verification artifacts; this document is the practical View entry point. The [Tornado design](../specs/2026-11-01-tornado-stage-gimmick-design.md) provides additional rationale.

**Delivery note:** The user authorized committing and pushing this implementation and handoff on `feature/Cheon/Core/NewCombatSytem`. The pre-change baseline is `c3daac1fcef74457cb92b2460e1628ddac9cc267`; checking out that baseline alone does not provide the new Tornado contract. Use the delivery commit containing this document and its accompanying Core files. The T1/T2/T3 reports retain their historical pre-delivery verification records.

## Contents

1. [Start here](#1-start-here)
2. [Shared Buffer, timing and playback contract](#2-shared-buffer-timing-and-playback-contract)
3. [Current View integration map](#3-current-view-integration-map)
4. [Tornado](#4-tornado)
5. [Rockfall](#5-rockfall)
6. [Haboob](#6-haboob)
7. [Recommended implementation order](#7-recommended-implementation-order)
8. [Authoring and scene preparation](#8-authoring-and-scene-preparation)
9. [Verification and acceptance checklist](#9-verification-and-acceptance-checklist)
10. [Troubleshooting and remaining decisions](#10-troubleshooting-and-remaining-decisions)
11. [Source map](#11-source-map)

## 1. Start here

### 1.1 What is ready and what is missing

| Feature | Core already provides | View still needs |
|---|---|---|
| Tornado | Prepared square areas; permitted area reader; scheduled warnings and actual diagonal center paths; immediate combat entry contact; attributed throws; landing settlement; preview/stale-input protection | Read grouped areas instead of legacy terrain; show direction; play actual center steps; adapt existing lift/landing playback to the new identity and persistent storm; connect new event types |
| Rockfall | Prepared cooldown; warning cells; impact recipients; actual HP changes and Bind status; cooldown/terminal state | Persistent warning markers, impact presentation, event routing, and verification of existing HP/status presentation |
| Haboob | Prepared cloud cells; relocation warning and new area; attack-range/aim filtering; explicit designation failure | Cloud mask and warning presentation, relocation presentation, event routing, and clear attack feedback |

**Important current gap:** `CombatEventTypeTable` registers the old `CombatTornadoEvent`, but none of the six new event classes in §2.4. Unregistered types route to `CombatEventRoute.Skip`. Final unit facts may update while gimmick warnings and movement are silently absent. Adding a prefab alone will not connect these features.

A useful starting point already exists: `CombatDisplayValues.State` is a `CombatStateData`. `CombatSceneDisplay.RefreshCore` receives it today. You do not need a new Core service or a direct InGame DB read to obtain these facts.

### 1.2 First practical pass

1. Confirm the new types exist, especially `TornadoAreaFact` and `TornadoGimmickEvents.cs`.
2. Add initial/final rendering from `CombatDisplayValues.State` for all three features. Start with simple visible shapes, then apply art.
3. Register the six event classes and route them to feature-local presentation handling. Verify they are no longer skipped.
4. Preserve chronological playback. Add Tornado center/throw handling before enabling concurrent presentation of these events.
5. Connect View-only assets, hover information and sound. Run §9 before calling the integration finished.

## 2. Shared Buffer, timing and playback contract

### 2.1 Where View reads

```text
IBufferQuery.CombatStateReadSink
  ├─ HasBattle / BattleInstanceId / QueryRevision
  └─ Current : CombatStateData
       ├─ CommandState.Turn
       ├─ Gimmicks.Entries
       ├─ StageGimmicks
       └─ ReadTornadoAreas(Owner.Player)

IBufferQuery.CombatResultReadSink.TryGetLatest(out CombatResult)
  └─ Events in authoritative playback order

CombatDisplayValueReader.Read()
  → CombatDisplayValues.State
  → CombatSceneDisplay.Attach / RefreshCore
```

Use the existing Buffer context and value-passing boundary. Do not query the root resolver for a new gimmick service, read InGameGroup from View, call a Core calculator during animation, or write gimmick state back to Buffer.

The following is a **read fragment for the existing display context**, not a new API or complete presenter:

```csharp
CombatStateData state = values.State;
long completedSides = state.CommandState.Turn.CompletedControllerTurnCount;
var tornadoAreas = state.ReadTornadoAreas(Owner.Player);
var rockfalls = state.Gimmicks.Entries.OfType<RockfallGimmickState>().ToArray();
var haboobs = state.Gimmicks.Entries.OfType<HaboobGimmickState>().ToArray();
```

It needs `System.Linq`, `Contracts.Combat`, and `Contracts.Common`; `CombatStateData` is in `ProjectL.Systems.RuntimeSystem.BufferSystems.Combat`. Here, `Contracts` means `ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts`.

`StageGimmicks` is a read-only prepared-definition list published through Buffer, suitable for descriptions and immutable presentation inputs. It is not a direct InGame SO read. For a state/area with `Id`/`GimmickId`, its definition is at `StageGimmicks[id.Value - 1]`; check/cast its expected type. Identify a Tornado member by `SourceId`, never by visual list position.

### 2.2 Identity and ownership

| Value | Meaning | View usage |
|---|---|---|
| `BattleInstanceId` | One prepared battle lifetime | Reset shown facts/resources on a new battle; include it in retained presentation identity |
| `StageGimmickKey` | Kind: Railway 10, Haboob 20, Rockfall 30, Tornado 40 | Select behavior/settings; not an instance ID |
| `StageGimmickId` | Definition index + 1 within this battle | Match state/definition/event; do not hardcode an ID for a kind |
| Tornado `SourceId` | Authored member identity | Match body, center path and throw origin within its group |
| `EntityId` | Character/structure identity | Match actual unit movement, HP and status events |

A storm source ID is not an `EntityId`. Several Tornado members occupy one grouped state. Haboob and Rockfall each have a state entry when authored. A stage may omit any feature; empty results are normal.

Core owns state, deadlines, RNG, victims and outcomes. View may retain presentation-only shown facts while replaying an old-to-new transition, as `CombatShownFacts` already does for units. Seed them from a snapshot, advance from ordered events, and reconcile to final Buffer after playback. They must not become another gameplay state owner.

### 2.3 The clock: completed sides, not frames or individual units

All three scheduled features use `CompletedControllerTurnCount`:

- Starts at 0 in a new battle.
- Accepted player end adds 1; accepted enemy end adds 1.
- Unit actions, frames and elapsed seconds do not advance it.
- Same-battle deployment returns preserve count and prepared gimmick facts.
- The accepted side-end event and increment precede scheduled effects.
- A lethal scheduled effect retains that accepted end and increment.

`ControllerTurnSequence` is input freshness/turn identity, not this schedule clock. `CompletedPlayerTurnCount`, `IsNight` and a locally counted number of unit actions are also unsuitable substitutes.

| Feature / phase | `NextBoundaryCount` means |
|---|---|
| Tornado `Cooldown` | Issue the next direction warning |
| Tornado `Warning` | Resolve the actual sweep |
| Rockfall `Cooldown` | Select/publish warning cells |
| Rockfall `Warning` | Impact the stored cells |
| Haboob `Active` | Warn that the current area will relocate |
| Haboob `RelocationWarning` | Resolve relocation |

Read phase and deadline together. At a synchronized snapshot, remaining sides are `max(0, NextBoundaryCount - CompletedControllerTurnCount)`. Do not label a warning deadline as a movement deadline. During playback, do not reinterpret old event snapshots using a newer final turn count.

Registered order is **Tornado → Railway → Haboob → Rockfall**, independently of definition order. Common composition passes each result to the next rule and stops on terminal outcome. Do not reorder features to group similar VFX.

### 2.4 Event types that need View routing

Numeric values are diagnostic references; prefer typed routing.

| Kind | Type | Information / required handling |
|---|---|---|
| 42 `RockfallAdvanced` | `CombatRockfallEvent` | `Step`, `Expected`, nullable `Final`, `RecipientIds` |
| 43 `HaboobAdvanced` | `CombatHaboobEvent` | `Step`, `Expected`, non-null `Final` |
| 44 `TornadoAdvanced` | `CombatTornadoGimmickEvent` | `Expected`, `Final`, `Boundary`, ordered `Advances`; one group summary per boundary |
| 45 `TornadoCenterReached` | `CombatTornadoCenterEvent` | `Origin`, derived `Center`; actual reached center/step |
| 46 `TornadoEntryStarted` | `CombatTornadoEntryEvent` | `GimmickId`, `EntrantId`, `Cell`; independent actual-entry chain marker |
| 47 `TornadoMemberAdvanced` | `CombatTornadoMemberEvent` | `GimmickId`, `Advance`; source completion after its actual contacts |

Existing character, trait-state, object, soul, structure, death, fall and outcome events still carry their existing facts. Gimmick events do not replace subsequent actual character events.

### 2.5 Initial state, playback and final refresh are different jobs

1. **Attach / reattach:** Render the complete current snapshot, including an already existing area/warning. Prior event replay is unnecessary.
2. **Playback:** Use event-time `Expected`/`Final` and ordered center/character facts. Buffer already contains the calculated final world; reading it for every animation can jump ahead.
3. **Final refresh:** After required pieces finish, reconcile to synchronized Buffer. Remove disappeared entries and reset resources on battle replacement/detach.

Continue using existing `CombatResultPlayback`, result acknowledgement and input gating. Include long-running gimmick pieces in presentation-complete/cancel/release lifecycle. Do not acknowledge a result while a required sweep, impact or landing is pending. `CombatSceneDisplay.RefreshCore` already requires presentation completion before drawing final facts.

## 3. Current View integration map

Paths below are relative to `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/`.

| Existing owner | Present now | Required connection |
|---|---|---|
| `CombatDisplayValues.cs`, `Display/CombatDisplayValueReader.cs` | Carry `CombatStateData` | Reuse it; no extra Core read service |
| `Display/CombatSceneDisplay.cs` | `RefreshCore` draws terrain/objects/trails/thorns; no three-gimmick snapshot rendering | Initial/final gimmick rendering through normal construction/binding |
| `Playback/Ordering/CombatEventTypeTable.cs` | Old Tornado registered; unknown types skip | Register all six new types, handlers and ordering/barriers |
| `Playback/Handlers/CombatFactEventHandler.cs` | Old event hides old Tornado body and shows a new cell | Migrate Tornado and route Haboob/Rockfall, possibly through dedicated feature handlers |
| `Playback/Handlers/CombatCharacterEventHandler.cs` | Lift/land uses `TornadoSourceId`, legacy terrain body, and a hide callback | Use `TornadoContact`; adapt lookup/lifetime for persistent storms and hidden sources |
| `Playback/Support/CombatShownFacts.cs` | Event-time character/structure facts, including pull | Preserve existing owner; add bounded shown-gimmick facts where appropriate |
| `Display/Map/FieldTerrainPresenter.cs` | Old terrain storm instances; anchor-corner 2×2 positioning/height | Centered published footprints; preserve unrelated terrain behavior |
| `Display/Map/EnvironmentVisualKinds.cs` | Maps `TornadoSourceFact` to terrain art | Reuse art through an area-to-View adapter; do not fabricate terrain facts |
| `Input/Assist/CombatEnvironmentText.cs` | Repeat text reads old `TornadoSourceFact` | Permitted area + published tuning; feature-specific descriptions |
| `Input/Interaction/CombatHazardLookup.cs`, `CombatUnitRangeHover.cs` | Mines/barrels through environment/structures | Separate consumption of gimmick cells if used for hover |
| `Playback/Support/CombatEventSounds.cs` | Old Tornado event sound; lift/land helpers | New warning/sweep/impact/relocation triggers without duplicate summary/center/member sounds |

No `CombatHaboobEvent` or `CombatRockfallEvent` references were found in the inspected Combat View. No Haboob/Rockfall settings were found in the inspected View data code. These are integration tasks, not supplied View APIs.

**View choices:** Durations, colors, opacity, sounds and warning art remain presentation decisions. Mandatory contracts are correct cells, visibility, identity, chronology, movement and final facts. The tasks below do not imply that proposed presenter/settings classes already exist.

## 4. Tornado

### 4.1 What the player should understand

A storm occupies a centered odd-sized square, normally 3×3. It remains at its current center between scheduled sweeps. During ongoing combat, entering any covered cell can immediately throw a unit even in `Cooldown`. That phase means waiting for the next scheduled warning; it does not disable contact.

A new battle prepares the starting area once. Deployment placement/repositioning and combat activation do not throw an overlapping unit. After activation, a subsequent actual step into coverage can throw immediately; a unit still inside at the scheduled sweep is also eligible. A same-battle deployment return keeps center/deadline but pauses contact eligibility.

Default schedule from fresh count 0:

| Count | Core state/action | View |
|---|---|---|
| 0 | Prepared `Cooldown`, next 1 | Permitted stationary footprint; deployment has `IsActive = false` |
| 1 | Warning, next 2, direction locked | Body stays at current center; show published direction when active |
| 2 | Sweep; next warning deadline 3 | Actual centers/contacts, then reconcile member state |
| 3 | Warning, next 4 | Next published direction |
| 4 | Next sweep | Continue using the new actual path |

Custom profiles change intervals, size and distance. A short repeat interval can leave the sweep's final state already `Warning`; display that final warning without waiting for another warning event.

### 4.2 Use the permitted area reader

Call `state.ReadTornadoAreas(Owner.Player)` for player-facing rendering. It returns immutable `TornadoAreaFact` entries:

| Member | Meaning |
|---|---|
| `GimmickId` | Group identity |
| `SourceId` | Storm member identity |
| `Center` | Actual center cell, not lower-left corner |
| `SideLength` | Odd square side length |
| `Cells` | Actual coverage derived by Core |
| `Phase` | `Cooldown` or `Warning` |
| `NextBoundaryCount` | This member's next warning/sweep deadline |
| `WarningDirection` | Locked direction when active; null when unavailable/inactive or stationary |
| `IsActive` | Ongoing-combat contact eligibility for this snapshot; false during deployment/finished state |

The raw `TornadoGimmickState : StageGimmickState` contains `Members`. Each `TornadoMemberState` has `SourceId`, `Center`, `HorizontalSign`, `Phase`, `NextBoundaryCount`, `LockedDirection`. The group deadline is the earliest member deadline, not every member's deadline.

For descriptions, match `TornadoGimmickSpec` in Buffer `StageGimmicks`, then `TornadoMemberSpec` by `SourceId`. Tuning includes `KnownTo`, `SideLength`, `DiagonalDistance`, `FirstActivationAfterControllerTurns`, `RepeatIntervalControllerTurns`, `WarningLeadControllerTurns`. Establish observer permission through the area reader first.

**Visibility:** Raw grouped states/events can include unknown members. Filter source-body, footprint and warning rendering by observer-permitted source IDs. Do not expose every raw `Members`/`Advances` entry. A hidden source can still cause authoritative character relocation: preserve unit movement while keeping the storm body hidden. This needs a presentation path without an existing visible body; the old lift lookup currently assumes one.

`IsActive` is a synchronized snapshot value. A result can already have a final `Finished` state while its final contacts are still being replayed. Do not use that newer final flag to skip earlier valid contact playback; reconcile inactive/finished presentation at the proper final boundary.

### 4.3 Geometry and art placement

- Use `Center` and `Cells` with `FieldGridMapper`. Remove the old half-cell corner shift and old 2×2 covered-height calculation for new storms.
- Default coverage is center ±1 in both cell axes. Custom side lengths require the published footprint, not a fixed 3×3 decal.
- The full square fits the board. Walls, cliffs, structures and units may be covered; their presence does not remove footprint cells.
- Core chooses a feasible vertical direction at warning, locks it, shortens travel at the boundary, and chooses the later horizontal reflection. View does not recompute that policy.
- `HorizontalSign` is not a complete direction. Use `WarningDirection` for arrows and actual paths/center events for motion.
- A narrow valid board can produce no diagonal motion. The path still contains its starting center and contacts still resolve. No arrow may be available. Do not invent off-board motion.

### 4.4 Events and actual playback order

`CombatTornadoGimmickEvent` is a **summary**, inserted before actual center/contact events. It includes final facts for the whole calculation. Do not teleport every storm to that final state, or animate every summarized path and then also play center events.

Each `TornadoMemberAdvance` has:

- `Expected` / `Final`: old/resulting member facts.
- `SideLength`: this action's footprint.
- `Path`: actual centers including origin; last entry equals `Final.Center`.
- `IsTravel`: true when advancing from `Warning`; false for a warning action.

A conceptual stream is:

```text
PlayerTurnEnded or EnemyPhaseEnded (count already incremented)
CombatTornadoGimmickEvent (one group summary)
  each executed due member in ascending SourceId:
    warning action:
      CombatTornadoMemberEvent
    sweep action:
      CombatTornadoCenterEvent, StepIndex 0 (origin)
      actual relocations and landing/health/object/status/death facts
      CombatTornadoCenterEvent, StepIndex 1
      actual relocations and settlement facts
      ...only centers actually reached...
      CombatTornadoMemberEvent (source complete)
then later registered gimmicks, unless battle ended
```

This is an ownership guide, not permission to reorder `Events`. Settlement can produce additional facts omitted above.

`CombatTornadoCenterEvent.Origin` is `TornadoContactOrigin`: `GimmickId`, `SourceId`, actual `Center`, `StepIndex`. Move the matching storm to that center, complete the necessary segment, then show following contacts from there. Index 0 establishes the origin; it is not an extra movement step.

A standalone entry starts with `CombatTornadoEntryEvent(GimmickId, EntrantId, Cell)` and then actual relocation/landing facts. It leaves scheduling unchanged and need not contain a grouped advance. Immunity or no legal destination can produce no throw: an entry marker alone is not a movement command.

### 4.5 Unit throw attribution and lift/land reuse

The actual throw is an existing `CombatCharacterEvent` with `Kind == Relocated`:

| Member | Usage |
|---|---|
| `Expected` | Unit immediately before this relocation |
| `Final` | This relocation's destination, before later settlement |
| `TornadoSourceId` | Same stable member ID, retained for compatibility |
| `TornadoContact` | New attribution: group/source/current center/step |

`TornadoContactOrigin.StepIndex == -1` denotes stationary-entry contact; sweep attribution uses the actual source step from 0. Use the provided attribution rather than proximity, the storm's final center or the final result's unit anchor.

`CombatCharacterEventHandler.TryBeginTornado` already has reusable lift/landing composition, pose leasing and cues. Its current lookup/hide callback assumes disappearing/reappearing legacy terrain. Adapt these assumptions:

- Resolve center/body/settings from the matching member at the contact's event-time center.
- A throw does not destroy or respawn the storm. Keep its source available for other victims and later steps. Cosmetic fading must not remove identity or break later target lookup.
- Several units can use the same source in one sweep. Another source can throw a unit during nested landing. Preserve attribution and event order.
- Core guards successful `(SourceId, EntityId)` throws within one resolution. Replay each published relocation once; do not deduplicate by source alone or by final result position.
- A → B → A chains terminate in Core. A final coordinate equal to an earlier coordinate does not erase the real intervening relocations.
- Use this `Relocated.Final` for its landing pose. Later push, mine, soul, status or death events may change the unit again. Do not overwrite those later facts using the first throw's snapshot.

### 4.6 Interruption, terminal paths, preview and undo

Core interrupts the old voluntary/forced path after a real throw, including nested throws returning to the same reached cell. Pull/knockback summaries contain actual reached prefixes. Do not animate the rest of the originally requested displacement. Pull still uses `CombatPullEvent`; its same steps are not also emitted as ordinary `Moved` events.

Fatal contact truncates the sweep. Path/center events contain only reached centers. Later centers/source actions can be absent; the fatal member keeps its terminal deadline instead of reserving a normal cycle. Do not synthesize completion motion, later warnings or later gimmicks.

Movement/attack preview uses permitted sources and a projected RNG cursor. `MovementPreviewContext.VisibleTornadoAreas` participates in freshness checks. Geometry, relevant landing occupancy/terrain or RNG changes can invalidate an old command. Use existing preview/submission/rejection paths; do not choose a landing or reroll randomness in View.

Actual relocation prevents an undo checkpoint under existing policy. Refresh availability from Core. Restoration is not another forward entry and must not replay a throw.

### 4.7 Concrete Tornado migration checklist

- [ ] Initial/final permitted areas, keyed by battle/group/source.
- [ ] Centered published footprints; old 2×2 pivot/height assumptions audited.
- [ ] Four new Tornado types routed with center/contact/member barriers.
- [ ] Lift/land lookup, persistent-body lifetime and hidden-source behavior adapted.
- [ ] Old event sound/respawn assumptions replaced with deliberate warning/sweep/throw triggers.
- [ ] Hover/description reads permitted area and published tuning, not old terrain.
- [ ] Custom size/interval, zero travel, multiple sources, interrupted pull/knockback and fatal-prefix cases verified.

## 5. Rockfall

### 5.1 What the player should see

Rockfall alternates between waiting and a visible warning on exact published cells, followed by impact on those cells. Impact affects living field characters occupying warned cells. Structures are not recipients. Core chooses the cells/recipients; View shows them.

Defaults: cooldown 2 completed sides, warning duration 2, 3–5 candidate footprints, maximum-HP loss 20%, Bind for 1 affected-side turn end. With the default one-cell footprint:

| Count | State/action | View |
|---|---|---|
| 0–1 | `Cooldown`, next 2, no warned cells | No active warning markers |
| 2 | `Warning`, next 4, cells selected | Exact warning markers |
| 3 | Same warning | Keep markers |
| 4 | `Impact`; cooldown next 6 if non-terminal | Impact at stored cells; actual HP/status/death events; clear warnings |
| 6 / 8 | Next warning / impact | Continue from published state |

Custom profiles change these values. Do not hardcode this timeline into View.

### 5.2 Snapshot and event contract

Read `RockfallGimmickState` from `state.Gimmicks.Entries`: inherited `Id`, `Key`, `NextBoundaryCount`, plus:

| Member | Meaning |
|---|---|
| `Phase` | `RockfallGimmickPhase.Cooldown` or `Warning` |
| `WarnedCells` | Exact unique warned cells; empty in cooldown, possibly empty in warning when no legal candidate exists |

Corresponding `RockfallGimmickSpec` tuning: `CooldownControllerTurns`, `WarningControllerTurns`, `MinimumRockCount`, `MaximumRockCount`, `MaximumHealthLossPercent`, `BindAffectedSideTurns`, `FootprintOffsets`.

`CombatRockfallEvent`:

| Member | Warning | Impact |
|---|---|---|
| `Step` | `RockfallGimmickStep.Warning` | `RockfallGimmickStep.Impact` |
| `Expected` | Old cooldown | Warning state containing actual impact cells |
| `Final` | Non-null warning state | Next cooldown, or null if impact ended battle |
| `RecipientIds` | Empty | Unique actual living character recipients in entity-ID order |

On **Warning**, use `Final.WarnedCells`. On **Impact**, use `Expected.WarnedCells`, not `Final.WarnedCells`: non-terminal final state is cooldown and has no warnings. Empty recipients are valid; rocks can impact empty cells. Nullable `Final` is a normal terminal contract.

`WarnedCells` is a union. Candidate count is limited by availability and multi-cell masks can overlap. There is no separate rock-object list with per-rock anchors/IDs. Showing affected cells is supported; reconstructing an exact hidden candidate partition is not.

### 5.3 HP loss and Bind: use existing unit facts

Core computes `floor(MaxHealth * MaximumHealthLossPercent / 100)`. This is direct maximum-health-based reduction, not an ordinary numerical attack. Armor, shield and ordinary damage-reduction logic do not require another View formula. There is no attacking character to invent for direction or credit.

Use actual character HP/death events and their HP delta for numbers, including legitimate zero delta. Do not subtract the percentage again or emit a second attack/hit result in the Rockfall handler.

Survivors receive `StatusEffectKey.Bind`. **Bind restricts movement; it does not itself prohibit attacks.** It differs from Disarm. Refresh action availability from existing Core queries, rather than disabling everything with a Rockfall-specific View flag.

The status is published through `CombatTraitStateEvent` and normal character `StatusEffects`. At synchronized Buffer reads, `CharacterData.StatusEffectSnapshots` exposes `ActionRestrictionEffectSnapshot` for UI. Raw entries use `ActionRestrictionStatus.RemainingAffectedSideTurnEnds`.

Core adjusts the granted timer when impact occurs at the affected side's own end, then performs shared cleanup. Event-time granted and final displayed counters can differ. Show received snapshots and expiration events; do not decrement another timer in View.

`CombatFactEventHandler` already routes `CombatTraitStateEvent` into shown unit facts. Preserve that path; verify a readable Bind visual/formatter exists. Persistent status and expiration belong to shared status UI.

### 5.4 Impact ordering and terminal behavior

```text
accepted side-end event (incremented count)
earlier due gimmicks, if any
CombatRockfallEvent(Impact, Expected warning, Final cooldown-or-null, RecipientIds)
actual character HP / Bind / death and common settlement facts
shared side-end cleanup / regeneration if battle continues
final synchronized refresh
```

The impact event identifies timing, area and recipients; subsequent unit/status events carry results. Use existing pieces/handlers so impact is visible before associated HP/death changes. Prevent concurrent batching from moving a status/HP update before its impact cue.

Terminal impact can remove the state (`Final == null`). Finish impact and actual unit effects, then remove warning/presenter state in order. Final snapshot rendering must tolerate the missing entry.

### 5.5 Concrete Rockfall integration checklist

- [ ] Initial/reattached warnings from published cells; no cooldown warning.
- [ ] `CombatRockfallEvent` routing preserves warning-to-impact identity.
- [ ] View-only warning/impact art/sounds through existing View DB patterns.
- [ ] Impact uses `Expected.WarnedCells` and actual `RecipientIds`.
- [ ] Existing HP/death and Bind application/expiration paths retained.
- [ ] Zero candidates, empty recipients, zero-percent loss, custom masks and terminal null handled.
- [ ] Markers clear on impact/new battle/detach and persist through same-battle deployment when still published.

## 6. Haboob

### 6.1 What the player should see

Haboob is a relocating cloud that affects **direct designation across its boundary**. It is not the old static `Sandstorm` terrain kind. Do not implement it as static sandstorm tiles or a generic range penalty.

The initial cloud is prepared before any event. The default mask has 12 cells in a 4×4 box:

```text
.##.
####
####
.##.
```

Published `Cells` are authoritative, including custom masks. The cloud may cover walls, cliffs, structures or units. It does not become solid terrain or apply an entry-damage/movement-blocking effect.

Defaults: relocation interval 2 completed sides, warning lead 1:

| Count | State/action | View |
|---|---|---|
| 0 | Initial `Active` area, next 1 | Cloud already visible |
| 1 | `RelocationWarning`, next 2; same cells | Warn about relocation; keep current cloud visible/effective |
| 2 | `Relocation`; new area, next 3 | Old-to-new transition, then final cells |
| 3 / 4 | Next warning / relocation | Repeat from published state |

Warning does not reveal a destination. Core samples the new area at relocation. With only one legal area, relocation may keep the same cells. When warning lead equals interval, initial/final phase may already be `RelocationWarning`; do not require a separate Warning event every cycle.

### 6.2 Snapshot and event contract

Read `HaboobGimmickState` from `state.Gimmicks.Entries`: inherited `Id`, `Key`, `NextBoundaryCount`, plus:

| Member | Meaning |
|---|---|
| `Phase` | `HaboobGimmickPhase.Active` or `RelocationWarning` |
| `Cells` | Current actual footprint, still the current area during warning |

`HaboobGimmickSpec` exposes `RelocationIntervalControllerTurns`, `WarningLeadControllerTurns`, `FootprintOffsets`. Haboob/Rockfall currently have no per-owner `KnownTo` mask in their state contracts; do not invent Tornado-style visibility fields.

`CombatHaboobEvent` has `Step`, `Expected`, `Final`; both states are non-null:

| Step | Expected / final | Presentation |
|---|---|---|
| `HaboobGimmickStep.Warning` | Active → RelocationWarning, same cells | Warning treatment without moving/removing cloud |
| `HaboobGimmickStep.Relocation` | RelocationWarning → next published phase/area | Transition from `Expected.Cells` to `Final.Cells`; allow unchanged cells |

No continuous cell-by-cell path is published. Fade, translate or dissolve is a View choice. Do not generate gameplay cells, contacts, collisions or temporary aim permissions during that cosmetic transition.

### 6.3 Targeting rule, with examples

For direct primary designation, Core requires attacker and primary cell to have the **same membership in the current cloud**:

| Attacker | Primary cell | Haboob permission |
|---|---|---|
| Outside | Outside | Allowed by Haboob, even if the line crosses cloud cells |
| Inside | Inside | Allowed by Haboob |
| Outside | Inside | Rejected |
| Inside | Outside | Rejected |

Other targeting rules can still reject a Haboob-permitted aim. Empty primary cells are checked too. A legal primary can cause splash/indirect hits across the boundary. Direction/All attacks without a primary cell are not converted into forbidden Single designation because an affected unit is in the cloud.

Core query/preview results remain authoritative:

- Single range/`AttackAims` already exclude forbidden primary cells.
- Execution/preview can report `AttackFailureReason.HaboobDesignationBlocked` (51).
- Use returned aims/range/failure, not a View ray/occlusion rule.
- Explain that attacker and directly designated cell must both be inside or both outside. Do not claim projectiles cannot pass through the cloud.
- Refresh hover/range/preview when synchronized relocation changes the area; unchanged unit position does not guarantee an old aim remains legal.

Standing inside does not create a per-unit Haboob debuff or HP event. Its rule reads current area membership. Cloud geometry and an area explanation are the primary presentation responsibility.

### 6.4 Concrete Haboob integration checklist

- [ ] Prepared cloud rendered before first end, with exact/custom mask.
- [ ] Event routing and warning treatment without guessed future destination.
- [ ] Cosmetic `Expected.Cells` → `Final.Cells` transition, including unchanged area and immediately warned final state.
- [ ] Existing Core aim/range/preview results used; explicit failure explained.
- [ ] View-only cloud art/settings distinct from static Sandstorm gameplay.
- [ ] Field readability, picking and existing highlight layers remain usable.
- [ ] Detach/new-battle resource reset and final snapshot reconciliation verified.

## 7. Recommended implementation order

This is a suggested View breakdown, not another Core phase plan.

| Step | Work | Evidence before continuing |
|---|---|---|
| V1 — read/display | Use `CombatDisplayValues.State`; draw initial/final areas/warnings | Prepared battle renders before event consumption; reattach restores it |
| V2 — routing | Register six types, handlers and completion lifecycle | No accidental skip; all-feature result consumed once and refresh completes |
| V3 — Tornado | Center/pivot, arrows, center barriers, lift/land lookup | Multiple sources/victims, stationary/nested contact work without duplicate motion or missing-body errors |
| V4 — Rockfall | Warning persistence, impact cue, shared HP/status | Stored cells survive cooldown transition; HP/Bind/death appears once |
| V5 — Haboob | Mask/warning/relocation, aim explanation | Membership examples match preview and execution |
| V6 — assets/acceptance | View settings, scene/prefab wiring, cancellation, PlayMode | §9 cases signed off with stage/profile/playback evidence |

Keep feature behavior local. A Tornado presentation change should primarily touch its presenter/handler/settings/tests, not Rockfall/Haboob internals. Normal composition/binding edits are expected. A thin shared owner may dispatch events and manage lifetime; it should not reimplement gameplay or duplicate authoritative state.

Use normal construction/module/scene-binding paths. Feature-local helpers do not automatically need independent Framework services. Required binding failures should identify the feature/resource instead of silently dropping supported events.

## 8. Authoring and scene preparation

### 8.1 Core content inputs for test setup

A stage must actually author each feature. A presentation prefab alone does not enable gameplay.

| Feature | Authoring route | Fields / caveats |
|---|---|---|
| Tornado placement | `CombatStageData` partial environment input → `terrainPlacements` using a Tornado `TerrainDefinitionDataSO` | `sourceId`, `cell` (center), `knownTo`, `randomizeInitialPosition`, `tornadoInitialHorizontalSign`; a fixed interior center needs explicit -1/+1 sign |
| Tornado tuning | `TerrainDefinitionDataSO` Tornado fields | Old serialized names `firstActivationAfterUnitTurns` / `repeatIntervalUnitTurns` now count completed sides. New fields: `tornadoSideLength`, `tornadoDiagonalDistance`, `tornadoWarningLeadControllerTurns` |
| Rockfall | `CombatStageData.rockfall : RockfallGimmickAuthoring` | `enabled`, `cooldownControllerTurns`, `warningControllerTurns`, `minimumRockCount`, `maximumRockCount`, `maximumHealthLossPercent`, `bindAffectedSideTurns`, `footprintOffsets` |
| Haboob | `CombatStageData.haboob : HaboobGimmickAuthoring` | `enabled`, `relocationIntervalControllerTurns`, `warningLeadControllerTurns`, `footprintOffsets` |

Rockfall/Haboob authoring are disabled by default. Tornado is authored through terrain input but becomes a grouped gimmick; new runtime storms are not `TornadoSourceFact` entries in `Environment.Tiles`.

The default `Assets/ProjectL/DataBaseSO/InGameGroup/Terrain/Item/Terrain_Tornado.asset` changed first/repeat 3/3 → 2/2. Custom values stay custom. T1 inventory found no production stage reference to that named asset: do not assume changing it provides a playable stage. Verify selected-stage authoring and prepared Buffer before debugging rendering.

Use a board that fits the full Tornado square and Haboob mask. A fixed center on the board's outermost edge is invalid if its square extends outside. Content owners should correct invalid input; clipping gameplay coverage in View would conceal the wrong setup.

### 8.2 View assets and scene wiring

Existing Tornado art/settings use Terrain View data: `TerrainVisualDataSO`, `TornadoPresentationSettingsSO/Data`. Reuse suitable art, lift timings and cues, while migrating old source creation, pivot and disappearing-body assumptions. General Tornado sound listens for the old event; lift/land helpers also contain pending AudioSystem slots. Verify audio availability before claiming those cues play.

Haboob/Rockfall need presentation data and art wiring through normal View Group patterns: cloud mask material, relocation warning, Rockfall warning decal, impact effect and sounds. Exact new class/table names are a View decision; none are claimed as already existing here.

Use `CombatSceneRoot` / normal scene binding and existing display construction. Consider both cell-prefab and grid-outline modes when supported by the target scene. Use `FieldGridMapper` for positions, sizes and surface height, without hardcoded world axes/unit cell sizes/flat-board assumptions.

Do not delete old Missing Script assets, change FieldCamera/Architecture ownership, or repair unrelated audio/package/camera work as part of this handoff. Coordinate shared scene/prefab changes with their owner.

## 9. Verification and acceptance checklist

### 9.1 Core evidence already available

These are completed Core checks, not proof of finished View presentation:

| Evidence | Actual result | Artifacts |
|---|---|---|
| Final T3 | Core 4,521/4,521; CoreIntegration 628/629 | `Library/TornadoT3/final-acceptance.xml`, `.log` |
| Later registry/entry correction | Related 96/96; full Core 4,524/4,524, CoreIntegration 628/629 | `Library/TornadoEntryContract/green.xml`, `full.xml`, logs |
| Cross-gimmick audit | 265/265, no skips; Core 202/202, CoreIntegration 63/63 | `Library/StageGimmickConsistencyAudit/results.xml`, `editor.log` |
| DB asset checks | 9/9 | Included in full/audit XML |

The one full-suite failure is pre-existing `CombatCameraZoomPoseTests.PostProcessingExtensionIsAllowedButPoseWriterIsRejected("Cinemachine.CinemachineCameraOffset", false)` namespace/type lookup (`TypeLoadException`). Camera/package code is unchanged. The 265-test audit excludes that test. `Library` artifacts are local and Git-ignored; another machine must rerun or receive exported results.

Useful executable specifications:

- Core: `StageGimmickProgressionTests`, `RailwayGimmickTests`, `HaboobGimmickTests`, `HaboobTargetingTests`, `RockfallGimmickTests`, `RockfallEffectTests`, `TornadoGeometryTests`, `TornadoGimmickPreparationTests`, `TornadoGimmickResolutionTests`, `TornadoGimmickEvidenceTests`, `TornadoRuntimePathTests`.
- CoreIntegration: `StageGimmickAuthoringTests`, `StageGimmickStatePublicationTests`, `StageGimmickCompositionTests`, `HaboobPublicationTests`, `RockfallPublicationTests`, `TornadoGimmickAuthoringTests`, `TornadoGimmickPublicationTests`, `TornadoRuntimeIntegrationTests`, `DataBaseAssetTests`.

Use Unity 6000.0.73f1 through the AppData-authorized path on this PC. A sandbox `CurlRequestCache.db` startup error is an environment permission issue, not evidence that this work needs a Unity upgrade or cache deletion.

### 9.2 Shared View regressions

- [ ] All six new types route to supported handlers; tests detect accidental `Skip`.
- [ ] Attach/reattach renders current facts without old events.
- [ ] Final state does not appear before intermediate playback finishes.
- [ ] Later centers/sources do not overtake prior landing/HP/death pieces.
- [ ] Repeated polling/acknowledgement does not replay effects.
- [ ] Cancel/detach releases poses/effects/pooled visuals; late callbacks cannot recreate a prior battle's source.
- [ ] Terrain/object, pull, swap, status and Railway playback regressions remain valid.
- [ ] New visuals preserve pointer/hover/highlight behavior.

### 9.3 Tornado PlayMode cases

- [ ] Deployment square matches published cells; placing into it does not throw.
- [ ] Combat activation alone is inert; later actual covered-cell entry throws.
- [ ] Default 1/2 and 3/4 warning/sweep cycles use the published directions.
- [ ] Shortened/zero-motion sweep adds no invented centers.
- [ ] Origin/intermediate/endpoint contacts use the correct event-time center.
- [ ] Multiple victims retain one persistent source; multiple sources remain distinct during nesting.
- [ ] Hidden bodies/arrows remain hidden; actual unit relocation still plays without a missing-body exception.
- [ ] Pull/knockback/tile push/both swap arrivals stop at contact; no obsolete endpoint or duplicate settlement animation.
- [ ] Fatal path is a real prefix with one outcome and no later source/gimmick motion.
- [ ] Same-battle deployment preserves area/deadline; new battle resets identity.
- [ ] Undo follows Core and does not replay another forward throw.

### 9.4 Rockfall PlayMode cases

- [ ] Exact warning cells persist and restore on reattach.
- [ ] Impact uses old warning cells even when final state is cooldown.
- [ ] Empty recipients and zero legal warnings complete normally.
- [ ] HP loss, zero-loss impact, surviving Bind and deaths show once.
- [ ] Bind blocks movement while legal attacks remain available; expiry matches shared status facts.
- [ ] Terminal `Final == null` removes warning safely after playback.
- [ ] Custom timing/masks reach the renderer without View constants replacing them.

### 9.5 Haboob PlayMode cases

- [ ] Initial cloud appears before first end, with exact mask.
- [ ] Warning retains current cloud and reveals no unsampled destination.
- [ ] Relocation reaches final cells; unchanged area and immediately warned final phase work.
- [ ] Inside→inside/outside→outside direct aims work when otherwise legal; crossing primary membership is rejected, including empty cells.
- [ ] Outside endpoints remain legal when the line crosses the cloud.
- [ ] Legal-primary splash and Direction/All follow Core responses across boundaries.
- [ ] Relocation refreshes hover/aim feedback and explicit failure text.
- [ ] Cloud opacity, picking and cell highlights stay usable.

Record tested stage/profile, result/event sequence, screenshot or playback observation, and unresolved art/audio dependencies. Compilation and Core tests alone do not close these items.

## 10. Troubleshooting and remaining decisions

| Symptom | First check |
|---|---|
| Units update, gimmicks absent | Type table still skips new types, or display only reads terrain tiles |
| New Tornado has no terrain source | Expected: use grouped state and `ReadTornadoAreas` |
| Lift fails with missing body/settings | Legacy terrain lookup still connected, or hidden-source path assumes a visible body |
| Storm shifted by half a cell / 2×2 | Legacy corner pivot and covered-height code |
| Storm disappears after first victim | Old hide/respawn callback controls a persistent source |
| Storm jumps ahead of contact | Group `Final`/latest Buffer applied before center events |
| Rockfall impacts nowhere | Renderer used empty cooldown `Final.WarnedCells`, not `Expected.WarnedCells` |
| Rockfall crashes at stage end | `Final` assumed non-null |
| Bind disables attacks | Movement Bind confused with Disarm |
| Haboob warning predicts new area | View sampled/reconstructed an unsupplied destination |
| Cloud acts like projectile wall/range penalty | Static Sandstorm or View ray rule used instead of Core aims/preview |
| Timers drift across unit actions | Local action/frame count used instead of completed sides |
| Feature absent in valid stage | Check enabled/placements, selected definition and prepared Buffer first |

Remaining View decisions include art/timing/sound, bounded shown-gimmick facts, and hidden-source throw presentation without a visible source. These fit existing View boundaries without gameplay changes.

A Core request is appropriate if a concrete requirement needs unpublished facts: for example exact per-rock anchors/partition, a future Haboob destination during warning, or runtime-changing Tornado visibility. Do not infer or silently create those facts in View. Current contracts support the acceptance behavior above.

## 11. Source map

All links are repository-relative. Existing contracts are distinguished from View integration points; calculation internals are optional reading when the contracts above suffice.

### Core and Buffer contracts

- [CombatStateData](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/Combat/CombatStateData.cs): state, definitions and permitted Tornado reader.
- [Buffer read interfaces](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/Combat/CombatStateBuffer.cs): state/result sinks and atomic validation.
- [StageGimmickState](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Combat/StageGimmickState.cs): Haboob/Rockfall state and shared identity/deadline.
- [StageGimmickEvents](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Combat/StageGimmickEvents.cs): Haboob/Rockfall event fields and nullability.
- [TornadoAreaFact](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Combat/TornadoAreaFact.cs), [TornadoGimmickState](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Combat/TornadoGimmickState.cs), [TornadoGimmickEvents](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Combat/TornadoGimmickEvents.cs).
- [TornadoGimmickSpec](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Commands/StageGimmicks/TornadoGimmickSpec.cs), [RockfallGimmickSpec](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Commands/StageGimmicks/RockfallGimmickSpec.cs), [HaboobGimmickSpec](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Commands/StageGimmicks/HaboobGimmickSpec.cs): immutable published tuning.
- [CombatTurnFacts](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Combat/CombatTurnFacts.cs): clock and phase facts.
- [CharacterData](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/Character/CharacterData.cs): synchronized unit/status snapshots.

### Existing View connection points

- [CombatDisplayValues](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/CombatDisplayValues.cs), [CombatDisplayValueReader](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Display/CombatDisplayValueReader.cs), [CombatSceneDisplay](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Display/CombatSceneDisplay.cs).
- [CombatEventTypeTable](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Playback/Ordering/CombatEventTypeTable.cs), [CombatFactEventHandler](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Playback/Handlers/CombatFactEventHandler.cs), [CombatCharacterEventHandler](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Playback/Handlers/CombatCharacterEventHandler.cs), [CombatShownFacts](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Playback/Support/CombatShownFacts.cs).
- [FieldTerrainPresenter](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Display/Map/FieldTerrainPresenter.cs), [EnvironmentVisualKinds](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Display/Map/EnvironmentVisualKinds.cs), [CombatEnvironmentText](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Input/Assist/CombatEnvironmentText.cs), [CombatEventSounds](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Playback/Support/CombatEventSounds.cs).
- [CombatSceneRoot](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/CombatSceneRoot.cs): existing scene-bound references.

### Authoring and presentation data

- [CombatStageData](../../../Assets/ProjectL/Script/Systems/DataBaseSystem/Data/InGame/Table/Stage/Combat/CombatStageData.cs), [CombatStageEnvironmentInput](../../../Assets/ProjectL/Script/Systems/DataBaseSystem/Data/InGame/Table/Stage/Combat/CombatStageEnvironmentInput.cs), [TerrainDefinitionDataSO](../../../Assets/ProjectL/Script/Systems/DataBaseSystem/Data/InGame/Table/Terrain/Item/TerrainDefinitionDataSO.cs).
- [RockfallGimmickAuthoring](../../../Assets/ProjectL/Script/Systems/DataBaseSystem/Data/InGame/Table/Stage/Combat/RockfallGimmickAuthoring.cs), [HaboobGimmickAuthoring](../../../Assets/ProjectL/Script/Systems/DataBaseSystem/Data/InGame/Table/Stage/Combat/HaboobGimmickAuthoring.cs).
- [TerrainVisualDataSO](../../../Assets/ProjectL/Script/Systems/DataBaseSystem/Data/View/Table/Terrain/Item/TerrainVisualDataSO.cs), [TornadoPresentationSettingsSO](../../../Assets/ProjectL/Script/Systems/DataBaseSystem/Data/View/Table/Terrain/Item/TornadoPresentationSettingsSO.cs): existing Tornado art/settings route, requiring the migration described above.

### Optional calculation references

- [StageGimmickComposition](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/StageGimmicks/StageGimmickComposition.cs): one registry, initialization, execution order.
- [TornadoGimmickRule](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/StageGimmicks/TornadoGimmickRule.cs), [TornadoCalculator](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/TornadoCalculator.cs): actual sweep/contact integration.
- [RockfallGimmickRule](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/StageGimmicks/RockfallGimmickRule.cs), [HaboobGimmickRule](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/StageGimmicks/HaboobGimmickRule.cs), [HaboobTargetingRules](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/StageGimmicks/HaboobTargetingRules.cs).

This is documentation only. It does not implement the View work or establish scene/PlayMode acceptance. Codex has not externally sent it to another worker.
