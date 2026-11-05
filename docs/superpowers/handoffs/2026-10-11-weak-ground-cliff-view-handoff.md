# Weak Ground and Cliff — View Handoff

Date: 2026-10-11  
Status: Prepared locally; not sent. Phases 1-5 Core, authoring and Buffer work are implemented. Actual-source managed verification: Core 1,971/1,971; affected Framework 198/198; Systems/Core compilation 0 errors. Unity-native follow-up remains deferred. View implementation is excluded; the audit in section 8 identifies live integration blockers.

## 1. Purpose and ownership

The user is implementing Core gameplay. The View owner is asked to integrate terrain display and fall presentation using the published contracts verified in Phase 5. Existing playback rejects CombatFallEvent; resolve the dependencies in section 8 before enabling this terrain in live content.

Gameplay SSOT: terrain design (Git history: `2026-10-11-weak-ground-cliff-design.md`), section 2. Core delivery and verification tracking: implementation plan (Git history: `2026-10-11-weak-ground-cliff-implementation-plan.md`), Phase 5. Do not duplicate the rules in View or read InGame DB to calculate progress.

## 2. Required user-facing behavior

- Distinguish intact weak ground, pending-collapse ground, and cliffs.
- Show remaining entries on weak ground. At zero, show an explicit pending-collapse cue; the cell is still ground until Core publishes collapse.
- Readability must not depend on color alone. Labels/icons can accompany the material or outline.
- Support both current CellPrefab and GridOutline field modes and preserve input/highlight priorities.
- Play the batch terrain transition and occupant falls from the recorded result. Final state must also reconstruct correctly on refresh/rebind without replaying entry counts.
- Remove fallen character/Obstacle visuals after their presentation lifecycle permits. Core death/removal is already settled and must not wait for animation callbacks.
- Retain progress/cliffs during night/deployment changes within a battle; a new battle receives a fresh snapshot.

Example: a cell authored with n=2 begins with remaining=2. First entry publishes remaining=1. Second entry publishes remaining=0 and pending=true while Kind is WeakGround. After the active actor's end effects and due tornadoes, the collapse result publishes Kind=Cliff and any occupant falls. Do not change the visual to a cliff at the second entry itself.

## 3. Verified read contracts

Paths are relative to `Assets/ProjectL/Script/Systems/`.

| Contract | Proposed integration |
|---|---|
| `RuntimeSystem/BufferSystems/Combat/CombatStateData.cs` | Existing Environment and EnvironmentBindings expose the authoritative snapshot and classification. Reuse the current Buffer read sink. |
| `RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Combat/StaticTerrainFact.cs` | Phase 1 implemented WeakGround=4 and Cliff=5, plus EntryThreshold, EntryCount, RemainingEntries, and PendingCollapse. Remaining/pending are derived read-only properties; Id, Cell, Active, and KnownTo retain their existing meaning. |
| Existing `CombatMovementSourceEvent` | Expected/Final terrain facts expose entry progress and WeakGround->Cliff at the confirmed action boundary. Phase 5 verifies publication and chronological expected/final matching, including last entry and collapse within one command. |
| `Contracts/Combat/CombatFallEvent.cs` | EntityId, TerrainSourceId, Cell; Kind=Fell. Identifies the fall location even if the dead character's final anchor has been cleared. Fell precedes held cleanup/death facts on forced cliff entry and batch collapse. Phase 5 verifies publication with character/structure removal and final attack health. |
| `Contracts/Results/AttackTargetPreview.cs` | Phase 3 uses finalized AttackHitResult health/death. Read TargetHealthAfter and TargetKilled directly: a hit for 20 followed by falling has ExpectedDamage=20 and TargetHealthAfter=0. Existing preview constructors retain their previous behavior. |
| Existing character/structure state events | Health/death/removal remains observable through the existing event types and final state. A fall event does not replace those events. |

Consume the existing result sequence once. Respect the current visibility contract. For visual association, use source IDs/cell coordinates and entity IDs, not object names or animation-derived positions. Do not persist an independent remaining-count or collapse queue in View.

A removed Obstacle may remain as a dead fact for Core identity/binding validation. Treat it as absent from live occupancy; do not require physical removal from the snapshot array before removing its visual.

## 4. Expected event ordering

Within a collapse batch, terrain source changes precede occupant fall/state events. All pending cells and affected occupants belong to one result before its outcome publication. Within a single fall, the explicit fall cause is available before the corresponding death/removal state event. Phase 4 calculation tests verify source-ID changes before entity-ID ordered falls, no duplicate multi-cell Obstacle falls, and completion of the batch even after main-character death. Phase 5 verifies Buffer publication, including terminal main-character fall and nonterminal Obstacle falls.

A forced cliff entry may occur in a movement/attack result before action end. A tornado destination may cause an immediate fall; a later collapse batch only processes remaining pending weak ground. Replaying visual events must never request additional Core death or terrain commands.

Keep source-change and fall handling separate from normal movement interpolation if the current consumer assumes Moved implies a living destination. Cancel and release animation resources on scene exit, error, or playback interruption using the existing presentation contract.

## 5. Candidate integration locations — owner review required

These are read-only inspection targets for the View owner, not changes authorized in this work:

- `RuntimeSystem/InGameSystem/ViewSystem/Combat/CombatViewRefresh.cs`: consume current terrain snapshot during refresh.
- `RuntimeSystem/InGameSystem/ViewSystem/Combat/CombatResultPlayback.cs`: coordinate source/fall events with existing completion and cancellation.
- `RuntimeSystem/InGameSystem/ViewSystem/Combat/Display/FieldView.cs`: coordinate field-relative terrain presentation in both display modes without owning rules.
- Existing character/structure presenters: associate falling visuals with entity identity and release after playback.
- View DB, prefabs, scene bindings, UI labels, materials, or shaders: choose exact assets/components under View ownership. No new camera or Architecture changes are assumed.

If a new event currently causes an exhaustive switch to reject the result, or final refresh removes an entity before it can fall visually, record the exact consumer and fix it in View work. Do not ask Core to duplicate counters or delay authoritative death to compensate.

## 6. View acceptance checklist

- [ ] n=2 shows 2 -> 1 -> pending, then Cliff only after the collapse result.
- [ ] n=2 and n=5 cells update independently; character identity does not reset progress.
- [ ] A pushes B onto the threshold; the warning and subsequent collapse follow A's action end.
- [ ] A due tornado can move a character away before collapse or create a new pending cell in the same boundary.
- [ ] Several cells collapse together; no duplicate falls or early removal hiding required visuals.
- [ ] Forced cliff entry displays immediate fall; fallen Obstacles do not play a barrel explosion/destruction-reward effect.
- [ ] Both field display modes work, including hover/range overlays and remaining-count readability.
- [ ] Refresh/rebind, skipped presentation, night redeployment, new battle, cancellation, and scene exit leave no stale labels, objects, subscriptions, or render handles.

## 7. Core readiness record

| Item | Current state |
|---|---|
| Gameplay rules | Confirmed with user on 2026-10-11; design is SSOT. |
| Actual compiled API | Terrain/fall contracts compile; WeakGroundStateTests 20/20 and Phase 5 publication tests pass against actual source. Existing CombatStateData and CombatResult APIs are reused. |
| Voluntary movement calculation | Phase 2 entry/return/legality/context tests 27/27; full managed Core regression 1,925/1,925. Preview simulates visible progress locally and preserves its input. This is not Buffer/publication verification. |
| Action-end calculation | Phase 4 WeakGroundTurnTests 16/16: batch/terminal ordering, interrupted action, tornado-before-collapse, recall-after-collapse, and player/enemy redeployment persistence. Core full suite 1,971/1,971; Framework managed cycle 40/40. Unity GameObject drag cycle remains deferred. |
| Buffer/event evidence | WeakGroundPublicationTests 25/25; affected Framework managed suite 198/198; actual-source Core suite 1,971/1,971. See section 9 for exact evidence and limits. |
| View compatibility findings | Read-only audit complete; unsupported fall event and missing terrain presentation are documented in section 8. |
| View changes | Excluded; no View files or assets edited. |
| External delivery | Not sent. The user will deliver this document. |

## 8. Read-only View compatibility audit — 2026-10-11

Paths below are relative to `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/`. No audited file was edited.

| Location | Current behavior / integration consequence | Work for the View owner |
|---|---|---|
| `CombatSceneEventPresenter.cs`, `CanPresent` (line 100) | The whitelist includes CombatMovementSourceEvent and CombatTornadoEvent but omits CombatFallEvent. | Add fall-event support and the chosen presentation lifecycle. This is a playback blocker, not only missing artwork. |
| `OrderedCombatPresentation.cs`, `Play` (line 41) | Preflights every result event; unsupported non-movement events throw NotSupportedException before playback starts. CombatSceneDisplay delegates CanPresent to the scene event presenter. | Ensure the display supports Fell before using terrain results in live content; do not remove the Core event or skip result acknowledgements to bypass this check. |
| `CombatSceneEventPresenter.cs`, `BeginEvent` (line 107) | Terrain source events currently complete immediately without applying a terrain visual. Character/structure state events render their snapshots immediately. | Present entry warnings and source transitions in the existing recorded order. Preserve the fall visual until its completion before consuming the subsequent death/removal state. |
| `CombatSceneDisplay.cs`, `Install`/`Refresh` (lines 104/131) | CombatDisplayValues already carries CombatStateData, but field setup/refresh does not consume Environment or EnvironmentBindings. Refresh updates units, hand, HUD and the active actor. | Read the authoritative visible terrain snapshot on first install and refresh/rebind. No new Core DTO or separate terrain Buffer is required. |
| `Display/FieldView.cs`, `Build`/highlight rendering | Supports CellPrefab and GridOutline, with existing static/highlight layers. There is no weak-ground/cliff or remaining-entry rendering path. | Add the terrain display under View ownership in both modes without replacing current hover/range/highlight priority. |
| `Display/FieldUnitPresenter.cs`, `RenderFacts` (line 67) | Skips dead or non-field characters and dead structures; the entity-view set returns visuals not requested in that render pass. | Complete fall animation using CombatFallEvent identity/cell and retained event snapshots before dead facts are rendered. Stable dead Obstacle records are intentional. |
| `CombatResultPlayback.cs` / `CombatViewRefresh.cs` | Existing lifecycle and sequence handling do not switch on event kind. | Reuse their completion/cancellation and result acknowledgement contracts; no replacement coordinator is indicated. |

The audit is source inspection, not a Unity visual test. Scene/prefab/material/UI choices and any required presenter/display contract changes belong to the View owner. This document requests the integration work; it has not been externally delivered.

## 9. Published example and verification

For an active source 71 at cell (1,2), threshold 2:

| Result | Published Kind | EntryCount | RemainingEntries | PendingCollapse | Events |
|---|---|---|---|---|---|
| Fresh preparation | WeakGround | 0 | 2 | false | Initial snapshot; placement is not an entry. |
| First character entry | WeakGround | 1 | 1 | false | CombatMovementSourceEvent: count 0 -> 1. |
| Second character entry | WeakGround | 2 | 0 | true | CombatMovementSourceEvent: count 1 -> 2. The source is still ground. |
| Acting character ends action | Cliff | 0 | 0 | false | Source Expected retains threshold 2/count 2, Final is Cliff. All batch source events precede occupant Fell/state events. |

Source ID/cell/active state/visibility and TerrainSourceIds survive collapse. Cliff progress fields are zero; this is a kind change, not restored weak ground. The fall event identifies the physical cell even when a dead character's final anchor is absent.

Phase 5 tests use the actual Systems/Core implementation and actual NUnit sources. WeakGroundPublicationTests covers exact count/pending publication, preview purity and stale rejection, immutable source guards, event expected/final matching, same-command entry/collapse, terminal character collapse, Obstacle knockback followed by collapse, immediate Obstacle cliff fall and fresh battle installation. Affected Framework fixtures: WeakGroundPublicationTests 25, DynamicEnvironmentPublicationTests 14, EnvironmentEntryPublicationTests 25, CombatCommandPublicationTests 31, MovementPublicationTests 20, CombatPreviewPublicationTests 3, WallStatePublicationTests 9, StageEnvironmentAdapterTests 24, WeakGroundAuthoringTests 7 and CoreStageCombatCycleTests 40 (198 total). Core regression: 1,971 passed.

Unity EditMode remains user-deferred because the existing CombatProgressJobTests.Core test double lacks IInGameCoreSystemGroupService.Results. Four UnityEditorNative SO cases and CoreStageCombatCycleTests.RegisteredPathCycle_PreviewMoveBarrierReturnAttackEndTurn (Unity GameObject/View drag) are excluded from the managed suite and are not claimed as passed. No DB assets were changed; DataBaseAssetTests was not required or run. View acceptance in section 6 remains unchecked.
