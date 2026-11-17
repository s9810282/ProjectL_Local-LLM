# Pillar and moving platform Core-to-View handoff — obstacle boarding restriction — 2026-11-16

2026-11-16

Core publishes the authoritative state and ordered results through the existing Combat Buffers. View activation and acceptance are pending. See the [implementation and verification record](../plans/2026-11-16-pillar-platform-implementation-plan.md#phase-6-execution-record--2026-11-16) and the [obstacle asset inventory](2026-11-16-obstacle-hit-count-migration.md).

## Delivery baseline and ownership

Integrate both commits from `feature/Cheon/Core/NewCombatSytem` before connecting this feature:

| Commit | Delivered behavior |
| --- | --- |
| [0d92812d0](https://github.com/ANTI-LIKE/Project_L/commit/0d92812d09f035e3e23dde2d3522f75206d3e26f) | Phases 1–6: hit-count obstacles, pillars, platform support/motion, previews, SO conversion and publication validation; includes the defensive platform stop. |
| [8d66c7788](https://github.com/ANTI-LIKE/Project_L/commit/8d66c7788e6a5e1f32368d06adeb9db8e9b4c426) | Reject obstacle knockback onto platforms; retain the defensive stop for exceptional occupancy. This is the current delivery baseline. |

Core owns hit counts, impacts, deaths, terrain, motion order, passenger membership and RNG. View reads Combat state/results, submits existing View-buffer requests, and owns appearance, interpolation, arrows and presentation acknowledgment. Keep existing Framework/Buffer/Job ownership: no View writes to authoritative facts, independent physics-based outcome calculation, duplicate turn gate or independent shuffle. `DetonatePowderBarrel` is a Core calculation entry, not a newly added View command/button.

The latest verification was 6,013 Core passes, zero failures and 11 existing diagnostic skips; 365 passes across 34 selected CoreIntegration fixtures; and two final exceptional-occupancy checks. These results establish Core behavior, not scene or presentation acceptance. View compilation and PlayMode remain unverified. Runtime stage content and legacy obstacle hit-count values still require content-owner activation.

## Gameplay that presentation must preserve

- Obstacles use hit counts. A landed zero-damage hit consumes one count; multi-hit attacks consume one per delivered hit. Use ordinary structure HP only for non-obstacles.
- A destroyed pillar falls away from its destroying hit's physical source in one of eight directions. An indirect source can be a barrel/pillar rather than the credited attacker. Use the recorded direction and source even if that source has disappeared.
- Pillar impact guarantees death, including Reconstruction holders and platform passengers. Child obstacles are destroyed and their destructive consequences continue; crush-origin supply rewards are suppressed. Do not display a defensive survival or supply reward absent from the result.
- The standing pillar disappears without adding debris to its original cell. Walls do not truncate impact. Ordinary impact floor becomes a wall; an impacted cliff becomes walkable `FilledCliff`. Existing wall/platform cells create no debris, although their occupants can still be crushed. Read terrain changes separately from the full impact footprint.
- Platforms move longest first, with Core-resolved random ties. Budget equals length. Blockage can reverse the remaining travel; exact endpoints store inward direction. The first player turn preserves authored initial placement. Ordinary unit boarding/disembarking resumes after automatic movement/playback.

### Latest obstacle-entry policy: required View behavior

| Situation | Core result | View behavior |
| --- | --- | --- |
| A hit would knock an obstacle onto any platform cell | Hit/durability resolves; that knockback step is blocked. The full obstacle and platform footprints count, not only their anchors. | Show the hit and actual stopped path. Do not animate the obstacle boarding or attach it as a passenger. |
| Obstacle starts before a platform and remains outside it after the blocked hit | Next platform prelude can move normally if its destination is clear. | Do not stop the platform merely because it blocked the earlier obstacle knockback. |
| Exceptional current state already has a living obstacle on a platform | `NontransportableOccupant`; no motion steps, anchor/sign/support preserved. | Keep both in place. Acknowledge the result after any other events complete so the following controller turn can start. |
| That obstacle is destroyed or leaves | A later platform calculation reevaluates current occupancy. | Follow the new recorded path; do not retain a local disabled flag. |

Example: a barrel at `(4,3)` is hit toward a platform at `(5,3)`. The barrel stays at `(4,3)` and can change from three remaining hits to two. The platform may subsequently move to `(6,3)`. The defensive stationary case applies only if an exceptional state already places the living barrel on `(5,3)`.

## Authoring and live reads

- The obstacle asset accepts `kind`, positive `requiredHits`, a rectangular footprint, and kind-specific settings. A pillar uses positive `fallLength` with default 3. The existing asset creation menu initializes new definitions to one hit. Older assets require an explicit approved value.
- `CombatStageData.environment.movingPlatforms` holds the shared environment source ID, length 1–3, shape axis, route endpoints, initial anchor and sign. Shape extends from the anchor along the positive shape axis. Travel axis derives from the endpoints. Sign +1 increases the travel-axis coordinate. Shared routes are valid with disjoint initial footprints. Core validates the entire initial footprint on cliffs and rejects initial bosses and structures on that footprint.
- `CombatStateData.ReadPlatforms()` reads the current immutable `MovingPlatformFact` values. Use `Id` as visual identity, `Cell` and `OccupiedCells` for position/shape, and `Spec.TravelAxis` with `TravelSign` for the persistent direction arrow. Ordinary board/disembark is an existing move, and passenger membership follows occupancy.
- Obstacle knockback cannot enter any currently supported platform cell: every cell in the proposed obstacle footprint is checked on each step. The hit still consumes durability and the obstacle stops before the platform. Unit boarding and ordinary obstacle knockback/falling retain their existing rules. If an exceptional state already contains a living obstacle on a platform, the prelude preserves its anchor, direction and support and reports `NontransportableOccupant` without motion steps. Destroyed/departed obstacles do not retain a lock. The existing initial-authoring restriction on structure/platform overlap remains unchanged.
- Read `StructureCombatFacts.ObstacleDurability.RemainingHits` and `RequiredHits` for obstacles. `Health` exposes only their liveness and `AttackHitResult.IsObstacle` identifies those entries. Ordinary structure HP stays available through its existing contracts. `ObstacleCommandCalculator.DetonatePowderBarrel` is the Core calculation entry for explicit detonation.
- Map `MovingPlatformFact` and `StaticTerrainKind.FilledCliff` in `EnvironmentVisualKinds` and `TerrainViewSet`. Raw cliff remains underneath a platform. A filled cliff retains its source ID and a new debris wall receives an allocated ID.

## Ordered result playback

| Event | Meaning |
| --- | --- |
| 52 `ObstacleDurabilityChanged` / `CombatObstacleDurabilityEvent` | Before/after hit counters, cause, origin and reaction context. |
| 53 `PillarFell` / `CombatPillarFallEvent` | Pillar ID, origin, fall direction and ordered impact cells, crushed units and destroyed obstacles. |
| 54 `PillarTerrainChanged` / `CombatPillarTerrainEvent` | Expected/final terrain and the parent fall. Apply at the event's position in the global sequence. |
| 55 `PlatformPrelude` / `CombatPlatformBoundaryEvent` | Boundary count and resolved platform order. |
| 56 `PlatformMotionResolved` / `CombatPlatformMotionEvent` | One platform's expected/final state, ordered steps and stop reason. |
| 57 `PlatformStepped` / `CombatPlatformStepEvent` | Translation or in-place turnaround; platform state and simultaneous passenger translations. |
| 58 `EnvironmentRandomSelected` / `CombatEnvironmentRandomEvent` | Evidence for Core's RNG and order validation. |

For each translation, move the platform and listed passengers together. `SourceChange` identifies the linked platform state event and `CompletionEvent` identifies when step effects finish. Play associated character move evidence through the same frame once. Play deaths, ejections and terrain changes in global result order. A passenger with `TransportEnded` stays out of later steps even if the platform makes a return trip.

Present the preceding result, then present every step of the platform result, then call `CombatViewWriteSink.AcknowledgePresented(result.Sequence)`. The existing `CombatPlan` checks `HasUnpresentedResult` before scheduling the next automatic advance. The next result starts the controller turn and refreshes actions. Enable input/AI only at the corresponding turn boundary. The initial player turn can start directly. A terminal platform result follows the existing stage-end flow.

```text
Previous result presentation completes
  -> Core publishes platform result (Automatic + StartingPlayer/StartingEnemy)
  -> View presents ordered platform/passenger frames and all linked effects
  -> View acknowledges that result sequence
  -> existing LifeCycle/Job gate admits the next advance
  -> Core publishes ControllerTurnStarted and turn-start effects
  -> existing presentation/input lifecycle continues
```

Turn-start action refresh, Reconstruction and periodic damage belong to that subsequent result. View must not trigger them or schedule AI itself. Even a stationary prelude has a result sequence to acknowledge. `PlatformMotionResolved.Steps`, the standalone step events and linked character/source events describe the same execution: do not animate each representation as an additional movement. Final Buffer positions can already be ahead of the currently animated frame; use event expected/final values during playback, then reconcile the display to the latest state.

### Playback examples

- A length-three platform traveling right from x=3 to an endpoint at x=4 can travel **3 → 4 → 3 → 2**. Play the turnaround at x=4 and refresh the negative direction arrow after it.
- A length-two platform can travel **3 → 4 → 3**. Its anchor begins and ends at x=3. The two translations and intervening turnaround still appear in the result.
- A length-two/three occupied platform moves each of its two/three riders in one frame. If a tornado ejects one passenger, play that displacement at its step and carry only the remaining riders afterward.
- A pillar can crush a passenger on a supported cliff. The fall lists that cell as an impact cell. The platform and underlying cliff stay visible and that cell emits no debris event.
- When a pillar fills a raw cliff in a later platform's route, display the new filled floor from event 54. The next prelude can reverse the platform away from that route or stop it if both directions are blocked.

## Forecast requests

Use `CombatViewPreviewData.PlatformForecast()` (kind 15) with the existing `CombatViewWriteSink.SetPreview`. Read the matching `CombatPreviewResult.PlatformForecast` by request number. Use `ClearPreview(number)` for the request you own. `PlatformForecastSource.Matches(battleInstanceId, queryRevision, commandState)` checks battle, revision, controller turn and boundary. Discard a stale reply when any source value changes. The read sink also hides a stale platform reply.

The existing Flow schedules previews during player input. The verification at an already-due starting boundary calls the Core preview service directly. Service tests also cover requests cleared or replaced during calculation. The synchronous preview Job expects an answer for its captured request. If Flow later yields between prediction and publication, the Job needs to accept a superseded request completing without a response.

`ProjectedBoundaryCount` names the simulated boundary. `Paths` exposes each platform in order with initial/final anchors and signs, steps and passengers. Example for the first playback scenario:

```text
RequestNumber=83, Source=(battle 3, revision 24, turn 4, completed 4, processed 3)
ProjectedBoundaryCount=4, Coverage=KnownInformationConditional
Paths[0]: PlatformSourceId=100, InitialAnchor=(3,0), InitialSign=+1
  Translation (3,0) → (4,0), sign +1, Passengers=[...]
  Turnaround  (4,0) → (4,0), sign -1
  Translation (4,0) → (3,0), sign -1, Passengers=[...]
  Translation (3,0) → (2,0), sign -1, Passengers=[...]
FinalAnchor=(2,0), FinalSign=-1, StopReason=BudgetConsumed
```

The path uses the installed environment RNG and the real prelude calculator on projected state. A path response is conditional on public information, including when requested after turn-end, so its coverage cannot reveal whether a hidden source exists. During an active turn, current terrain and occupancy are the assumptions. Actions, end-turn effects, or prior RNG use may change the next forecast. Empty exact answers cover the first turn, a completed starting boundary, and battles without platforms.

`CombatPreviewResult.Attack.Value.PillarFalls` holds ordered fall projections from the actual hypothetical attack. Use `ImpactCells` for the hit footprint and each `TerrainChanges` entry's `Cell` and `Kind` for debris. `CrushedUnitIds`, `DestroyedObstacleIds`, and `ChainReactions` describe the impact and chained consequences. This is hover data. Authoritative playback reads terrain IDs and source facts from the published combat result. The attack preview observes its existing public information policy.

## View-owned follow-up

The following are existing integration points to inspect, not a requirement to edit every file. `V` means `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat`.

| Existing area | Required outcome |
| --- | --- |
| `V/Display/Map/EnvironmentVisualKinds.cs`, `TerrainViewSet.cs` | Platform identity/footprint, raw cliff underneath, filled floor and persistent debris walls are represented correctly. |
| `V/Display/Unit/FieldUnitPresenter.cs`, `CombatStructureHitFeedback.cs` | Pillar appearance and obstacle hit-count feedback use typed durability, while ordinary HP remains correct. |
| `V/Playback/Support/CombatVolleyDamageNumbers.cs` | Avoid interpreting obstacle liveness HP as damage or remaining-hit count. |
| `V/CombatResultPlayback.cs` and its existing playback collaborators | Consume events 52–58, play simultaneous passenger/platform frames once, and finish linked effects before acknowledgment. |
| Existing hover/preview consumer and direction display | Show `PillarFalls` and the numbered platform forecast; respect conditional coverage/currentness and retain current direction outside playback. |

Use `CombatStateReadSink.Current.Structures`, `.Environment` and `.ReadPlatforms()` for current facts. Preserve the existing result-consumption path and `CombatViewWriteSink` acknowledgment/preview protocol. This handoff does not add a separate scene bootstrap or a new service registration.

- Migrate `FieldUnitPresenter`, `CombatStructureHitFeedback`, and `CombatVolleyDamageNumbers` from the obstacle HP compatibility value to hit counts and durability events. Confirm ordinary structure HP and damage feedback.
- In Unity, check placement and arrows from initial state, refresh after a reversal and after reopening display, ordinary boarding/disembarking, no input during transport, multi-passenger movement, ejection, death, net-zero replay, debris walls and filled-cliff visuals.
- Check that playback acknowledgement precedes the controller-turn start and that terminal results bypass turn start. Exercise stale and replaced preview requests while the highlighted context changes.
- Core results provide no View compile, scene or playback acceptance. Production activation also requires approved stage and obstacle content.

### Manual acceptance checklist for the View owner

- [ ] Show hit counts for barrels, supplies and pillars; check landed zero-damage and multi-hit feedback, plus ordinary structure HP.
- [ ] Show eight-way pillar falls, including a fall beyond an existing wall; distinguish impact from debris/fill and omit debris at the original pillar cell.
- [ ] Show crush death without Reconstruction, a chained barrel/pillar destruction, suppressed supply rewards, and a crushed platform passenger while retaining the platform.
- [ ] Play `3 -> 4 -> 3 -> 2`, a net-zero round trip, and two/three riders without duplicate movement; update direction arrows on each turnaround and after display reattachment.
- [ ] Show tornado ejection at the correct step and omit the ejected/dead passenger from later carrying steps.
- [ ] For barrels, pillars and a multi-cell obstacle, show knockback stopping before any platform overlap; confirm durability still changes and the next unoccupied platform can move.
- [ ] In an exceptional occupied-platform fixture, show a stationary result with unchanged direction and allow the next turn after acknowledgment; remove the obstacle and verify later movement resumes.
- [ ] Verify no input during motion, no turn-start effects before acknowledgment, initial-player skip, completely blocked preludes and terminal-result exit.
- [ ] Show conditional forecast paths without implying certainty; clear stale/replaced requests and distinguish hover terrain cells from authoritative terrain source IDs.

After approved production SO changes, the content/Core owner should run `DataBaseAssetTests` and battle-entry validation. View's acceptance report should separately record compilation, scene wiring and the manual cases above. This document update does not itself implement or verify View behavior.
