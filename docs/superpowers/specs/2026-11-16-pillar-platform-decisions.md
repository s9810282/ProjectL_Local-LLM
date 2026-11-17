# Pillar and Moving Platform Requirements — Confirmed Decisions

Updated: 2026-11-16

## Status and Scope

This is the requirements decision record from the brainstorming session. The user requested a written design and phase implementation plan after confirming passenger timing. No gameplay implementation has been performed. Technical proposals are distinguished from confirmed gameplay in the [design](2026-11-16-pillar-platform-design.md).

The user is the Core programmer. The working scope is Core rules, SO authoring data, and Buffer information required by View. View implementation is a later handoff to the View worker, not work assigned to this session. Existing Framework, Buffer, and Job ownership must be preserved; gameplay rules and authoritative state must not be duplicated in presentation code. Core verification and subsequent View acceptance must be reported independently.

## Confirmed: Obstacle Durability

- Obstacles use remaining hit count rather than numeric damage to determine destruction.
- An individual hit consumes one count. Multiple hits from one attack consume multiple counts; an Akimbo attack that hits twice consumes two counts.
- Required hit count is authored per obstacle in SO data. This applies to existing powder barrels and supplies as well as new pillars.
- A successfully landed hit consumes one count even when its numeric damage is zero.
- A powder barrel explosion consumes one count from affected obstacles, except that another powder barrel explodes immediately regardless of its remaining count.
- Provide an explicit powder-barrel-specific immediate detonation function/API. This is an intentional requested operation for barrel chains and future barrel-related callers; its exact public exposure and signature remain design work.
- Do not generalize this durability change to unrelated structures without a separate requirement.

## Confirmed: Pillar Destruction and Falling

- The hit that exhausts a pillar's remaining count determines its fall direction.
- For direct attacks, use the position of the attacker responsible for that hit. The pillar falls away from that position, along the attacker-to-pillar vector.
- For indirect attacks, use the position of the effect that directly hits the pillar, such as the exploding barrel's position, rather than the original attacker's position.
- Quantize the direction into eight directions using the same angular rule as the supplied `pillar-simulator.html`.
- Fall length is authored per pillar definition in SO data and defaults to three cells, excluding the pillar's original cell.
- The standing pillar disappears. Its original cell retains its underlying terrain and receives no falling debris.
- Existing walls do not truncate the fall footprint. Cells beyond walls still receive the original footprint's effects.

### Units

- Units within the fall footprint suffer guaranteed death, including units standing on a platform.
- Reconstruction must not activate against pillar crushing. This is a pillar-crushing rule, not a global change to all instant-defeat behavior.
- Do not implement pillar knockback or add a speculative pillar knockback API in this scope.

### Other Obstacles

- Other pillars and obstacles within the fall footprint are destroyed immediately, regardless of remaining hit count.
- Their destructive on-destruction effects execute, including pillar falls and barrel explosions.
- Buff-granting effects do not execute for this pillar-induced destruction; the affected buff-providing obstacle is removed without granting that buff.
- Precise chain ordering and source propagation must be made explicit in the implementation design; they are not implicitly determined by collection iteration.

### Terrain and Platforms

| Cell before impact | Confirmed result |
| --- | --- |
| Ordinary floor | Persistent debris wall |
| Cliff | Persistent filled floor that units can walk on |
| Existing wall | No debris generated; the fall continues beyond it |
| Cell occupied by a platform | Platform remains; no debris generated; units in the footprint still die |

- Falling impact cells and debris creation cells are distinct concepts. Omitting debris does not exempt units from crushing.
- Debris walls and filled floors last for the remainder of the battle and cannot be attacked to destroy them again.
- Debris walls use the existing wall movement and attack-blocking rules.
- Filled floors block platform movement even though units can walk on them.

## Confirmed: Moving Platform Definition

- Platforms are straight shapes of one, two, or three cells.
- Shape orientation (horizontal or vertical) is independent of movement axis (horizontal or vertical).
- A horizontal three-cell platform can therefore move vertically.
- Each route is a straight segment between two endpoints, traversed back and forth.
- SO authoring provides route start, route end, initial position, and initial travel direction, along with platform length and shape orientation.
- Different platforms may share route cells or have intersecting routes, but they block one another and cannot occupy overlapping cells.
- Platforms move only over cliff-like terrain, not ordinary floor. The exact supported cliff-like terrain categories still need definition.

## Confirmed: Platform Movement

- Platforms move before player-side and enemy-side turn start, except the first player turn of the battle. Keep the authored initial positions through that first player turn; movement first runs before the first enemy turn.
- The required lifecycle order is platform movement resolution, View playback of that movement, then controller-turn start. Turn-start effects and unit input/AI actions must not run before that playback completes. Reuse the existing train-style result-presentation gate rather than letting View determine gameplay movement.
- Platforms resolve sequentially, in descending length order: three-cell platforms, then two-cell platforms, then one-cell platforms. Equal-length platforms use a random order.
- Each platform's collision checks use current occupancy after preceding platform movements and their effects. Sequential processing does not replace overlap checks.
- The movement budget per trigger equals the platform's length: one, two, or three cells.
- If blocked before consuming the budget, the platform reverses and uses the remaining budget in the opposite direction during the same movement.
- Example: a three-cell platform travels one cell right, is blocked, then travels two cells left.
- Walls, pillar debris walls, pillar-filled floors, and other platforms block movement.
- A non-passenger unit or obstacle in the destination also blocks movement, even if such occupancy is normally absent over cliff-like terrain. Use the same reversal and remaining-budget rule; do not push, crush, or automatically board that occupant.
- The user's correction supersedes the initially suggested stop-on-occupant behavior: encountering that occupant reverses travel, and movement ends only when the budget is consumed or neither direction is available.
- If both directions are blocked, the platform stops for that movement rather than repeatedly reversing without progress.
- Reaching a route endpoint reverses travel direction immediately, including when the remaining movement budget is exactly zero.

## Confirmed: Platform Passengers and Forced Movement

- A platform can carry multiple units when they occupy available platform cells. Boarding must use vacant space; this does not permit unit overlap. Two-cell platforms can carry two single-cell units, and three-cell platforms can carry three.
- Capacity is interpreted as one unit per platform cell: a one-cell platform already carrying one unit cannot accept an additional passenger. The user has not specified a separate prohibition on boarding an empty one-cell platform.
- Boarding and disembarking at intermediate route positions occur through ordinary unit actions after automatic platform movement has completed. There are no player-input pauses during platform travel and no automatic pickup from adjacent floor.
- A unit coming from adjacent ordinary floor can use ordinary movement to enter an available stationary platform cell. An already occupied platform cell cannot receive that unit.
- A passenger can voluntarily disembark when there is a valid, occupiable supporting cell to step onto. Forced displacement remains governed by its own rules and is not restricted to safe landing cells by the platform.
- For each platform's automatic move, identify its current passengers at that move's start. Carry them through the ordered path; remove passengers that die or are displaced off the platform. Ordinary boarding/disembarking resumes during the following unit-action phase.
- Platforms are supporting surfaces, not protective containers. Boarding does not lock a unit to the platform or grant immunity to knockback or other forced displacement.
- The platform itself does not prevent a passenger from being knocked or forcibly moved off it. Existing terrain and occupancy restrictions still apply to that displacement.
- Carried units undergo applicable cell-entry effects along their movement, including thorns, objects, and tornadoes, following the existing forced-movement rules.
- A supporting platform prevents falling into its underlying cliff while the unit remains supported.
- If a unit dies or an effect displaces it off the platform, carrying that unit ends.
- Transportable units occupy a single cell. Bosses are not transported by platforms. Multi-cell passenger support and a passenger spanning multiple platforms are outside this feature's scope; this does not change unrelated boss movement rules.

## Confirmed: View Information and Preview

- Core provides the authoritative movement outcome; View determines animation speed, interpolation, and turning presentation.
- Movement output must preserve the ordered traveled path, including reversals, rather than only the final position. Returning to the starting position must not erase the journey.
- View must be able to identify the platform, its movement, the affected passengers and their movement, and the resulting travel direction.
- The platform's current travel direction must be available for presentation outside an individual animation as well.
- Provide a pillar preview of the fall, crushing, and terrain-change footprint if the current attack would destroy the pillar.
- Provide a platform preview of the next movement path and passenger movement outcome based on current state. View may render the path with arrows.
- Preview and committed movement results are distinct, while their gameplay calculations must share the same rule source.
- Random ordering of equal-length platforms must be accounted for when designing preview and execution consistency. When the random order is selected and retained remains a design detail; do not silently assume independent preview and execution rolls produce the same path.

## Planner Question Status

The earlier numbered gameplay questions are resolved. Passenger timing is explicitly confirmed: automatic platform movement and its playback finish before the turn begins; ordinary unit actions then permit boarding and disembarking. View never owns authoritative passenger membership.

## Design Proposals and Implementation Review

The [design](2026-11-16-pillar-platform-design.md) now specifies the initial supported cliff category, delivered environmental-contact semantics, chain ordering/origins, geometry/bounds, forecast limits, and the pre-turn effect window. It labels unrequested edge defaults and compatibility/migration choices as author proposals rather than confirmed planner answers.

Review those proposals with the design before implementation. Production obstacle hit-count values still need a concrete asset migration inventory and content-owner review; they must not be silently inferred from old numeric HP. Current owning code and contracts determine regression coverage. The [phase plan](../plans/2026-11-16-pillar-platform-implementation-plan.md) separates Core verification from later View acceptance.

## Verification Status

Documentation only. No product code, assets, tests, builds, Unity execution, or browser execution were changed or performed for this decision record.
