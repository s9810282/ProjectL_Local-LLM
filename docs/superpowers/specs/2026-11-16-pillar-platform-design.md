# Pillars, Obstacle Hit Durability, and Moving Platforms — Core Design

Date: 2026-11-16
Status: Approved for phased implementation; current execution status is maintained in the linked plan.
Requirements: [Confirmed decisions](2026-11-16-pillar-platform-decisions.md)
Execution plan: [Phase implementation plan](../plans/2026-11-16-pillar-platform-implementation-plan.md)

## 1. Intent, Scope, and Authority

Create destructible pillars that crush units and permanently reshape the board, together with moving platforms that transport units across cliffs. All obstacles use hit-count durability. Platform motion must be presented before the next controller turn begins; boarding and disembarking use ordinary unit actions after that motion finishes.

The gameplay decisions below are confirmed; implementation choices and unrequested edge defaults were identified as design proposals during review. The user subsequently authorized phased implementation. Finish and report each phase separately; do not automatically execute the next phase. The execution plan records the current authorization and completed phase.

The user is the Core programmer. In scope: pure Core calculations, existing command/publication integration, SO-to-Data input, state/evidence validation, preview output, and preparation of a later Core-to-View handoff, subject to the authorized phase. Do not require a separate unidentified Core owner to authorize this user's Core work.

Out of scope: View animation, meshes, arrows, scenes, FieldCamera, Architecture, new UI, a pillar knockback API, a new universal terrain framework, new networking/save systems, or unrelated refactors. View implementation and visual verification belong to the later View worker's handoff. Do not implement a separate Framework service, clock, Job runner, or state store for either feature. Core completion does not require that future View work to be finished; integrated feature activation and visual acceptance do require it.

Documents are English. New code comments/XML documentation are Korean. Preserve existing paths, namespaces, assembly boundaries, GUIDs, and `.meta` files. Codex must not commit or push. View test-writing and combat View MSBuild restrictions remain in force; Core scope does not waive them.

## 2. Current-Code Evidence

All paths in the tables use these explicit repository-relative roots:

- `C = Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator`
- `S = Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CombatSystem`
- `B = Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems`
- `D = Assets/ProjectL/Script/Systems/DataBaseSystem/Data/InGame/Table`
- `L = Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Combat`
- `V = Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat` (read-only impact inspection and later handoff; no edits in this Core task)

These are path abbreviations, not new folders. Findings were read from the working tree at `4730de720`; that tree also contains unrelated in-progress asset/test changes. Historical test results do not establish current correctness.

| Owning source | Observed behavior and design implication |
| --- | --- |
| `C/Application/Combat/ObstacleReactionCalculator.cs`, `AttackDirectDamageCalculator.cs`, `DeliveredAttackHit.cs` | Obstacles use structure HP; zero numeric damage currently returns without reaction. Delivery preserves individual hits, and obstacle reactions run in delivery order. Durability must be driven by actual delivered hits, not damage totals or one command count. |
| `C/Contracts/Combat/StructureCombatFacts.cs`, `ObstacleContentProfile.cs` | Structure identity/location are already authoritative; profiles describe obstacle behavior. Do not create a second obstacle entity registry. |
| `C/Application/Combat/CommandAttackProjection.cs`, `MovementProjection.cs`, `ObstacleReactionCalculator.cs` | Attack and arrival projections share character/structure dictionaries and damage application, but currently construct distinct reaction calculators; `RefreshObstacles` replaces one calculator and each receives an environment snapshot. A mutable pillar chain therefore needs an explicit command-owned environment/queue lifetime, not merely additional calculator files. |
| `D/Obstacle/Item/ObstacleDefinitionDataSO.cs`, `ObstacleDefinitionData.cs` | SO currently authors `maxHealth`, footprint and barrel/supply values. Add explicit hit-count authoring and migrate deliberately; old HP numbers are not automatically sensible hit counts. |
| `C/Domain/Movement/EnvironmentTerrainRules.cs`, `C/Application/Combat/CliffFallCalculator.cs` | Walls/cliffs block ordinary entry; fall resolution also directly reads raw cliff facts. Supporting platforms require one effective-surface rule used by every relevant consumer. |
| `C/Application/Combat/CommandEnvironmentState.cs`, `B/Combat/CombatStateBuffer.cs` | Terrain changes currently support weak-ground progression; existing walls and terrain classification are fixed. Generated wall/fill facts require narrow, evidence-backed extensions, not removal of validation. |
| `C/Application/Combat/CombatDamageApplication.cs` | Existing `DefeatSteps` permits Reconstruction. Crushing needs its own explicit cause and must bypass Reconstruction while retaining normal death settlement. |
| `C/Application/Combat/CombatTurnCalculator.cs`, `CombatCommandCalculator.cs` | End-turn calculation enters `StartingPlayer/StartingEnemy`, resolves end-gimmicks and outgoing-side cleanup, then the next automatic advance starts the controller turn. Platform processing must run after that cleanup and before `BeginControllerTurn`. Both ordinary and prepared advance paths need this routing. |
| `C/Domain/CombatEffects/Rules/ActionRestrictionTiming.cs`, `C/Application/Combat/DeathTraitApplication.cs` | `StartingPlayer/StartingEnemy` also denotes the old pending end-cleanup window for Fear duration. Platform entry effects need an explicit calculation-time window to avoid adding an extra duration after cleanup has already completed. |
| `L/Plans/CombatPlan.cs`, `L/CombatProgressFacts.cs` | The existing presentation gate waits for unpresented results. Use it between a platform-motion result and the next turn-start result. |
| `C/Domain/Combat/EnvironmentRandom.cs`, `C/Contracts/Combat/EnvironmentProgressFacts.cs` | Deterministic environment randomness already exists. Preview copies it; execution publishes its advancement. Do not use `UnityEngine.Random` or wall-clock seeds. |
| `S/CombatPreviewService.cs`, `C/Contracts/Combat/CombatPreviewResult.cs`, `B/CombatView/CombatViewPreviewKind.cs` | Request-numbered previews already have lifecycle and stale-result handling. Extend them; do not calculate independent View paths. |

The governing Framework references are `Assets/ProjectL/Script/Systems/Documentation/framework-architecture.md` and `framework-programmer-guide.md`, especially FW-001–004, FW-012–013, and FW-016. Functional-independence review follows `docs/superpowers/handoffs/2026-09-30-combat-view-design-session-handoff.md` section 4.6. Existing feature documents are historical context, not proof of the current implementation.

## 3. Approaches and Recommended Boundary

| Approach | Trade-off | Decision |
| --- | --- | --- |
| Reuse numeric HP as hit counts and add platform-specific exceptions to each movement consumer | Small initial patch, but HP scoring/evidence become ambiguous and platform/cliff behavior diverges between movement, falling, preview, and AI. | Reject. |
| Extend existing structure/environment facts, add focused pillar/platform calculations, and retain the existing publication and playback gates | Requires coordinated contract/validator changes, but keeps one state owner and a shared surface interpretation. | Recommended in this design. |
| Build a general layered terrain/component/event system and independent moving-object runtime | Broad migration, new ownership and ordering complexity beyond the requested features. | Reject. |

The user confirmed the gameplay and requested the documents without separately selecting an architecture. The middle approach is the design author's proposal, not a previously approved user decision.

## 4. Confirmed Gameplay Contract

### 4.1 Obstacles

- Required hit count is configured per SO. Each landed hit consumes one count, including zero-damage hits; two Akimbo hits consume two counts. Ordinary structures retain their existing HP rules.
- Barrel explosions consume one hit from obstacles. Barrel-to-barrel contact triggers immediate detonation regardless of remaining hits.
- Expose an explicit barrel-only immediate detonation operation for future Core callers. It goes through normal calculation/publication, never direct Buffer mutation.
- A pillar immediately destroys other pillars/obstacles in its footprint and executes their destructive consequences. Supply/buff-granting effects are suppressed for pillar-induced destruction.

### 4.2 Pillars

- A direct destroying hit uses that attacker's position, captured for the delivered hit. Indirect destruction uses the direct effect source position, such as the barrel or falling pillar, separately from kill-credit identity.
- Eight-way angle quantization matches the supplied HTML. Default fall length is three cells, excluding the original cell, configurable per pillar SO.
- Every unit in the fall footprint dies, including platform passengers; Reconstruction does not trigger. No pillar knockback/API is added.
- The pillar's original cell retains its underlying terrain. Existing walls do not shorten the footprint.
- Ordinary floor becomes a debris wall. A cliff becomes walkable filled floor. Existing wall/platform cells receive no debris; the platform remains, but its passengers are still crushed.
- Debris lasts for the battle and cannot be attacked again. Filled floor is walkable by units but blocks platforms. Debris walls use existing wall movement and attack blocking.

### 4.3 Platforms

- Length 1–3, straight horizontal/vertical footprint, independent horizontal/vertical travel axis. SO supplies route endpoints, initial anchor, and initial direction.
- Routes may overlap, but current platform footprints cannot overlap. Platforms block one another.
- Before each controller turn, move platforms sequentially in descending length order; shuffle equal-length groups randomly. A platform consumes its length in successful one-cell translations.
- On a blocker, reverse and use the remaining budget. At a route endpoint reverse immediately, even when the budget just reached zero. If both directions are blocked, stop.
- Only cliff-like underlying cells admit platform movement. Ordinary floor, walls, filled floor, other platforms, non-passenger units, and obstacles block the proposed whole footprint.
- Carry eligible one-cell units from occupied platform cells. Two cells support two units, three support three; every rider occupies a distinct cell. Bosses are not transported.
- Carrying consumes no ordinary move/attack use. Cell-entry effects still apply; a supporting platform prevents its underlying cliff from causing a fall. Displacement/death removes that passenger from subsequent transport steps.
- Ordinary actions can board an empty stationary platform cell or leave for a valid supporting destination. No automatic adjacency pickup or input during automatic platform movement. Forced movement can throw units off a platform and then uses normal landing/fall rules.
- Skip platform movement before the first player turn; first move occurs before the first enemy turn.

## 5. State Ownership and Proposed Contracts

### 5.1 Explicit obstacle durability without duplicate HP

Add immutable `ObstacleDurabilitySnapshot(int remainingHits, int requiredHits)` in `C/Contracts/Combat/ObstacleDurabilitySnapshot.cs`, with `0 <= remainingHits <= requiredHits` and `requiredHits >= 1`.

For classified native obstacles, `StructureCombatFacts.ObstacleDurability` is the sole authoritative durability value. For ordinary structures it is null and existing numeric health remains authoritative. Fact construction enforces this exclusive choice. Identity, anchor, rotation, and occupied cells remain in `StructureCombatFacts`.

Keep the existing `Health` read surface as a **derived liveness compatibility projection** for obstacles: alive is `(1,1)`, destroyed is `(0,1)`. Do not store an independent obstacle HP value. New obstacle code reads `ObstacleDurability`, never interprets the projection as damage or required hits. Add `IsDestroyed` as the shared liveness query. Existing constructor/copy call sites must preserve the discriminator and durability; silent reconstruction from the liveness projection is invalid.

This proposal preserves only liveness compatibility, **not numeric HP semantics for obstacle consumers**. Treat it as an explicit, obstacle-scoped contract migration. Character and ordinary-structure health contracts are unchanged. Before enabling the new obstacle model, inventory each Core numeric health consumer and route it to either real HP or hit durability; no consumer may infer which unit of measure is meant from the value `1`.

Current View consumers also read these values: `V/Display/Unit/FieldUnitPresenter.cs` passes structure current/max HP to display; `V/Display/Unit/CombatStructureHitFeedback.cs` and `V/Playback/Support/CombatVolleyDamageNumbers.cs` derive numeric loss from structure health. These are observed handoff dependencies, not files for this Core task to edit. The later handoff must specify hit-count display, dedicated hit evidence and `IsDestroyed`/typed reads. Keep production activation pending until those consumers have been migrated by their owner; do not claim the old View will display hit durability correctly from the compatibility getter alone.

Obstacle hit/destruction evidence carries expected/final counts and cause in `CombatObstacleDurabilityEvent`. Numeric `CombatHealthEvidenceValidator` excludes classified obstacle liveness projections and delegates their chain conservation to `ObstacleDurabilityEvidenceValidator`; it still fully validates characters and ordinary structures. Obstacle events must not be counted as HP damage in attack receipts, floating damage, AI HP-loss scoring, or statistics. Existing untyped obstacle reaction events remain reaction notifications, not a second durability store.

Non-attack migration defaults are explicit design proposals, preserving the hit-versus-damage distinction:

| Existing operation | Proposed obstacle behavior |
| --- | --- |
| A discrete delivered environmental contact, including a death-trait explosion | One hit per target per actual contact, independent of amount. Carry a valid direct physical origin before processing a possible pillar destruction. |
| Numeric healing request | No restoration of remaining hits and no compatibility-health write; repairing obstacles is outside scope. Ordinary structure healing remains unchanged. |
| Falling into an unsupported cliff | Remove the obstacle with remaining hits set to zero through `RemoveFallenObstacle(EntityId, CombatFallEvent)`. Preserve current no-explosion/no-supply/no-pillar-fall consequence for falling; record fall-backed durability evidence without fabricating a direction origin. |
| Periodic status tick without a delivered contact | No hit-count change. A future periodic obstacle-damage feature must explicitly define contact semantics. |

Audit `MovementProjection.ChangeHealthSteps`, `ApplyFall`, all `DamageStructure` callers, and direct `StructureCombatFacts` copies. A `(1,1)` compatibility projection must never cause a hidden one-hit destruction, restoration, or lost hit state in these paths.

`ObstacleContentProfile` and `InitialObstaclePlacementSpec` gain pillar settings and required-hit configuration. `StructureSpec.MaxHealth=1` is only an obstacle compatibility construction value, not authored durability. Keep general structure definition/HP contracts unchanged.

### 5.2 Platform facts in the existing environment

Add `MovingPlatformSpec` in `C/Contracts/Commands/MovingPlatformSpec.cs`: `SourceId`, `Length`, `ShapeAxis`, `RouteStart`, `RouteEnd`, `InitialAnchor`, `InitialSign`. Use `CellCoord`; shape axis is a two-value enum, travel axis derives from nonidentical aligned endpoints. Initial sign is `-1/+1` along increasing/decreasing coordinates, not relative to array order.

Add `MovingPlatformFact : MovementTileFact` in `C/Contracts/Combat/MovingPlatformFact.cs`. `Cell` is its single authoritative anchor; it holds the immutable spec reference and current travel sign. `OccupiedCells` derives from anchor, shape axis and length, not separately stored mutable cells. `TriggersArrivalRule=false`. Preserve subtype/spec/sign in visibility copying and value equality.

Classify these IDs as terrain in the existing `EnvironmentSemanticBindings.TerrainSourceIds`; validate the supported platform subtype explicitly. No new terrain-ID registry or runtime platform service. The intentional cliff-plus-platform overlap is admitted by preparation validation; unrelated illegal terrain overlaps remain rejected. Platforms are public to both sides in this scope (author proposal).

`CombatCommandState.LastPlatformBoundaryCount` is a nonnegative scalar initialized to zero. It records the last completed platform prelude using `Turn.CompletedControllerTurnCount`, and is preserved by every copy/composition path. It is processing progress, not a duplicated turn counter. No persistent passenger list: resolve riders from current character occupancy for each platform move.

Adding this field is a shared command-state contract change, so constructor/copy/composition audits are necessary once. Future platform route or collision-policy changes must not require editing unrelated command-state producers. Use the existing canonical copy path where it already exists; do not add a global reflection copier or a separate platform state store to avoid the audit.

### 5.3 Effective surface contract

Add `BoardSurfaceRules` in `C/Domain/Movement/BoardSurfaceRules.cs`, using the existing environment/bindings:

```csharp
internal static bool HasPlatformSupport(MovementEnvironmentFacts environment, EnvironmentSemanticBindings bindings, CellCoord cell);
internal static bool IsUnsupportedCliff(MovementEnvironmentFacts environment, EnvironmentSemanticBindings bindings, CellCoord cell);
internal static bool IsPlatformRouteSurface(MovementEnvironmentFacts environment, EnvironmentSemanticBindings bindings, CellCoord cell);
```

Underlying cliff facts remain unchanged beneath platforms. `IsUnsupportedCliff` is false when a platform supports the cell. `IsPlatformRouteSurface` reads underlying terrain and is false for filled floors, ordinary floors, or walls; occupancy collision is checked by the platform calculator separately. Initial supported cliff category is existing `StaticTerrainKind.Cliff`; further cliff-like categories extend this single classifier when their semantics exist, not through speculative enums.

Add `StaticTerrainKind.FilledCliff`. It is walkable, does not cause falling, and blocks platform travel. A filled cliff retains the original source ID and visibility. Generated debris walls are ordinary `StaticTerrainFact(Wall)` with an allocated source ID and explicit pillar-change evidence; provenance belongs to evidence, not a parallel wall map.

Route the effective cliff checks in ordinary movement, forced entry/fall, deployment/spawn/summoning, cell inspection, move preview/context, and AI through this shared rule. Preserve unrelated terrain, leap, wall corner-cut, and placement policies. Unit support is necessary but not sufficient for placement: existing deployment/eligibility rules still apply.

### 5.4 Command-local collaborators and lifetime

This is a proposed correction to the observed attack/arrival snapshot boundary, not a new world framework:

- For an attack command, construct one `CommandEnvironmentState` before the reaction calculator and share that concrete instance with `CommandAttackProjection` and its one arrival `MovementProjection`. The shared-constructor path accepts the existing state explicitly; it does not reconstruct it from the original input. Standalone movement/prelude calculations construct their own command-local state. Separate commands and speculative previews never share mutable projection instances.
- Keep one reaction queue and destroyed-ID guard for that command across direct hits and arrival effects. Share the existing `ObstacleReactionCalculator` instance rather than introducing a generic context/service or a second obstacle runtime. Its environment reads come from the shared current `CommandEnvironmentState`, not a constructor-time `MovementEnvironmentFacts` copy.
- `RefreshObstacles` must become a hit-scope refresh, not a replacement that discards command-wide queued work. It may reset per-hit blast summaries only after the queue is drained and the previous ledger consumed them; it preserves the shared queue owner, destruction guard, environment and damage boundary. Document this precondition beside the implementation and test two hit scopes explicitly.
- Pillar geometry consumes coordinates/length only. Pillar impact reads current cells through the surface contract and writes through existing damage/terrain/reaction operations. It never reads platform ordering/passenger animation internals. Platform movement reads resulting terrain and obstacle liveness, not pillar reaction internals.
- On completion, the existing publisher validates and commits one coherent transition. Calculation exceptions discard speculative state; expected collision or a stale/rejected command is not an exception disguised as a successful mutation. No partial chain state is published.

| Boundary | Inputs and condition | Output / permitted mutation | Lifetime and failure contract |
| --- | --- | --- | --- |
| Delivered obstacle hit | Landed contact, target ID, current direct source, active command | One count transition; destruction queued at zero | Command-local; invalid source/target rejected before mutation; an already destroyed target emits no second effect. |
| Pillar impact | Destroyed pillar with usable source and current environment | Ordered impact/death/reaction/terrain evidence | Queue drained before command completion; invalid geometry cannot partially change terrain. |
| Platform prelude | Starting-side phase, due boundary, current environment/characters and RNG | Motion transition and processed-boundary stamp; no turn-start effects | One command per due boundary; both-sides-blocked is a successful stationary result. |
| Motion step | Whole target footprint admitted, continuing riders identified | One positional frame, then existing cell-entry settlement | All rider/platform positions update before effects; effect-displaced riders leave subsequent steps. |
| Read/forecast | Immutable source stamp and observer-visible state | Typed facts or projected result, no authoritative writes | Existing request cancellation/revision rules; conditional coverage is explicit. |

These contracts are the knowledge future feature editors need. The one-time integration audit can inspect other internals, but it must not make that knowledge a permanent prerequisite for changing fall length or platform bounce rules.

## 6. Reaction and Terrain Execution

### 6.1 Hit delivery and chains

Keep hit delivery as the owner of whether contact happened. Zero resulting damage must not drop a landed obstacle hit. A zero-percent splash placeholder currently retained for reporting is not automatically a hit; add/use an explicit landed-hit flag so numeric amount and contact are independent (design proposal).

`ObstacleReactionCalculator` remains the reaction coordinator. Add `ApplyHit(EntityId, CombatDamageOrigin)` and `DestroyObstacle(EntityId, CombatDamageOrigin, ObstacleDestructionCause)`; preserve numeric `DamageStructure` for non-obstacles only. Causes distinguish direct hit, barrel chain, pillar crush, and explicit detonation so supply rewards cannot leak across crushing chains.

Use one iterative FIFO destruction queue for barrel and pillar consequences. Mark an obstacle destroyed when enqueuing its consequence, before processing children. A destroyed ID can emit its destructive reaction once per command. Root delivered-hit order is preserved; each impact processes cells by distance then stable entity ID, enqueueing children in that order. No recursive stack or traversal-order-dependent dictionaries. Descendants of a pillar-crush chain retain a suppression flag for supply buffs (author proposal).

Expose the pure facade in `C/Application/Combat/ObstacleCommandCalculator.cs`:

```csharp
public static CombatCommandTransition DetonatePowderBarrel(
    CombatCommandInput input, EntityId barrelId, CombatDamageOrigin origin,
    IReadOnlyList<IMovementTileRule> rules);
```

The internal chain calls the same reaction operation. The public facade verifies a live barrel in the current battle, rejects non-barrels, and returns a normal evidence-bearing transition for the existing publisher. An already destroyed barrel produces no second explosion. It does not add a player command/button, separate service, or direct store write. Existing battle/command stamps and publication validation remain mandatory.

### 6.2 Pillar geometry and impact

`PillarFallCalculator` owns geometry and pillar impact; `ObstacleReactionCalculator` only dispatches its queued consequence. Geometry entry:

```csharp
internal static IReadOnlyList<CellCoord> GetCells(CellCoord pillar, CellCoord source,
    int length, int boardWidth, int boardHeight);
```

Use the HTML's angle quantization on the source-to-pillar vector; account explicitly for HTML row-down versus Core coordinate conventions. Compare axes, diagonals and off-axis examples under that coordinate mapping. Clip at the rectangular board edge. Reject nonpositive length and zero-direction sources before mutation; real queued pillar/barrel sources retain their physical origin even after removal. Do not silently fall east for a missing origin.

For each falling pillar, capture its footprint and terrain classifications, crush each distinct unit intersecting it once, immediately mark affected obstacles destroyed/enqueue consequences, then apply terrain changes for that impact. Resolve each resulting chain within the same command before publishing. Preserve multi-cell boss damage semantics for intersecting occupied cells even though bosses are never platform passengers.

Add explicit `CrushSteps(EntityId recipientId, CombatDamageOrigin origin, CombatEvent contextEvent)` to the existing `CombatDamageApplication`, sharing death/item/credit/evidence settlement but bypassing numeric mitigation, protective on-damage substitution, and Reconstruction. Do not call ordinary `DefeatSteps` and do not fake a cliff-fall event. Complete the bounded chain and legal terrain/occupancy state before terminal outcome publication; no next automatic turn is admitted if the battle ended.

Extend `CommandEnvironmentState` with focused `ApplyPillarTerrainChanges(...)` using expected facts, new allocated wall facts, and explicit fill successors. Buffer validation replays that evidence, conserves IDs, checks classifications and forbids arbitrary wall replacement. Extend `StaticTerrainFact` successor validation narrowly for cliff-to-filled, not arbitrary kind changes.

Author-proposed edge defaults requiring design review: repeated impact on filled cliff stays filled (no stacking); existing debris walls stay walls; non-Obstacle structures retain their current rules and occupied cells receive no new debris wall. Do not silently kill unrelated headquarters or entomb a surviving structure. These defaults are not earlier planner answers.

**Approved integration correction — 2026-11-16:** The current wall builder rejected diagonal contact between distinct cardinal wall components. That restriction conflicts with eight-way pillar debris. The user explicitly approved removing this restriction and updating the existing `WallGeometryTests.DiagonalContact` rejection expectation to successful, separate blocking components. Duplicate-cell, bounds, public-visibility and terrain-classification checks remain. The existing segment-clipping/corner-cut rules still determine attack blocking; no second wall geometry system is introduced.

The supplied reference was located at `C:/Users/권천명/Downloads/pillar-simulator.html`, line 1807. Its quantization is `floor((atan2(dy, dx) + PI / 8) / (PI / 4))`, normalized modulo eight. HTML row-down coordinates map to inverted Core Y. Phase 3 tests use literal axis, diagonal and off-axis coordinates under this mapping.

## 7. Platform Prelude and Passenger Execution

### 7.1 Before-turn result boundary

Required sequence:

```text
Previous turn ends / existing end-turn gimmicks resolve
  -> existing result playback completes
  -> Advance publishes platform prelude, still Automatic + StartingPlayer/StartingEnemy
  -> View presents ordered platform/passenger movement and acknowledges the result
  -> next Advance performs BeginControllerTurn
  -> turn-start effects and their existing result playback
  -> player input / enemy decision becomes eligible
```

Do not merge platform motion and `ControllerTurnStarted` into one transition. The turn sequence, action refresh, Reconstruction, periodic status processing, and reinforcements remain untouched until the subsequent `BeginControllerTurn` call. Existing end-turn gimmicks are not moved or reordered.

Add `PlatformMotionCalculator.TryAdvanceBeforeTurn(input, rules)` returning null when no prelude is due, otherwise a complete transition. A prelude is due only in `StartingPlayer/StartingEnemy`, with platforms present, positive `CompletedControllerTurnCount`, and `LastPlatformBoundaryCount` smaller than that count. After processing, stamp that boundary even if every platform was blocked. Preserve `Automatic` and the starting-side phase. Zero-platform and initial-player states proceed directly to normal turn start without an empty extra result.

Route `AdvanceCore` and `AdvancePrepared` through the same helper before normal turn start. Keep `RequiresEnemyDecision` false during these starting phases. `CombatPlan`'s existing `HasUnpresentedResult` gate and result sequence are the only presentation acknowledgment; no timer or new presentation boolean in Core. Inspect direct advance callers, prepared work, first-use rehearsal, cancellation and stale-publication paths against that caller contract. Core produces two ordered results; existing LifeCycle/Job orchestration decides when another advance is admitted, and View acknowledges presentation. Do not move the orchestration gate into a new Core service or alter another owner's flow in this task; record an actual missing gate as a targeted handoff dependency.

The prelude's **effect timing** differs from the old end-gimmick window even though the published controller phase remains `StartingPlayer/StartingEnemy`. Add a feature-local calculation context `CombatResolutionWindow.BeforeControllerTurn` to `MovementProjection` construction (default `Normal` for existing callers). Its `PendingEndCleanupSide` is null, whereas the normal projection derives that value from `ActionRestrictionTiming.PendingEndSide(Turn)`. Pass the explicit value to restriction-duration calculation, including `DeathTraitApplication` Fear. Propagate the window through nested movement/effect projections. This is invocation context, not another persisted turn state or a presentation flag. Do not repeat outgoing-side cleanup. Tests must compare end-gimmick and prelude deaths with the same published starting phase: only the former gets the compensation for an end decrement still pending.

### 7.2 Order, collision, and movement

At each eligible prelude, start from source-ID-sorted platform groups, ordered by descending length. Fisher–Yates shuffle each equal-length group using `EnvironmentRandom` on a local copy of environment RNG. Publish the resulting RNG state once with the successful prelude. A singleton group consumes no random value. Sorting by source ID only seeds the shuffle; it is not the final tie rule.

Process one platform's full budget before the next. For each proposed one-cell translation, verify the whole new footprint is in route/map bounds, over eligible underlying cliff, clear of other platforms/structures and non-passenger characters. Exclude the moving platform and its own riders from self-collision. Do not jump over blocked intermediate cells.

Try current sign, reverse if blocked, then test the reverse. If neither fits, end this platform's move and keep its incoming sign stable (author proposal). Successful translations consume budget; reversals alone do not. Reaching an endpoint stores the inward direction immediately. A route can be dynamically shortened by debris without changing authored endpoints.

For every successful step, relocate the platform and all continuing passengers as one positional frame before evaluating any rider's entry effects. This prevents false self-collision and transient falls on underlying cliffs. Then process rider entry effects in stable entity-ID order using existing movement-effect operations. Stop carrying a rider after death or displacement off its assigned cell. Update departure/arrival presence modifiers, consumed objects, thorns, fall, tornado and death settlement through existing boundaries rather than replaying an attack-knockback command. Platform transport is distinct from hostile knockback and must not be canceled by knockback immunity.

Passenger membership is computed per platform when its move begins. A unit displaced by an effect onto a different platform may be carried when that later platform begins, if eligible; this is an author-proposed consequence of sequential current occupancy. No user input occurs inside the prelude. Terminal effects stop further platform steps after the current frame and its required settlement are coherent.

### 7.3 Boarding, leaving, and boss handling

Ordinary movement onto supported vacant cells boards by position; leaving changes position and therefore membership. No Board/Disembark commands or duplicate seat counters. Existing action cost/path/undo policies apply. Platform transport invalidates previous undo receipts through existing environmental/automatic-change rules; it is not a voluntary move that consumes a move use.

Use existing boss classification/profile, not footprint size alone, to exclude bosses from transport. A nontransportable entity in a required destination blocks motion. Authoring/preparation rejects an initial platform footprint already occupied by a boss; general boss movement remains unchanged. If forced movement or another runtime effect nevertheless places a nontransportable entity on the platform's current footprint, that platform stays in place for this prelude, retaining support and its direction. It neither carries the entity nor moves out from underneath it (author-proposed safety rule). Reevaluate on the next prelude using current occupancy; no permanent disabled flag is stored.

**Approved follow-up — 2026-11-16:** Obstacles cannot board a platform through knockback. Every proposed obstacle footprint cell must be outside active, classified platform support before that step is admitted. Reject the movement step while retaining the landed hit and ordinary blocked-knockback result. Unit transport/boarding and unsupported-cliff falls are unchanged. Keep the nontransportable-occupant stop as defensive handling for an exceptional state that already contains a living obstacle on the platform.

## 8. Published Data and Forecasts

Add immutable contracts under `C/Contracts/Combat` and `C/Contracts/Results`:

| Contract | Required information |
| --- | --- |
| `CombatObstacleDurabilityEvent` | Entity ID; expected/final remaining/required hits; hit/destruction cause; physical origin; credited actor; link to reaction context. |
| `CombatPillarFallEvent` | Pillar ID/origin; source position; eight-way direction; configured length; ordered in-board impact cells; crushed IDs; links to queued reactions and terrain changes. |
| `CombatPillarTerrainEvent` | Pillar impact reference; expected/final source facts or allocated wall; cell and resulting terrain kind. |
| `CombatPlatformMotionEvent` | Boundary count; platform source ID; order index; initial/final anchor and sign; ordered steps/turnarounds including repeated visits; stop reason. |
| `PlatformPassengerStep` | Entity ID; from/to cell for that platform step; whether transport ended due to death/displacement. Existing effect events remain interleaved in the global event order. |
| `PlatformForecastResult` | Source stamp (battle, query revision, turn/boundary); ordered projected platform paths/passenger outcomes; current/final directions; exact/conditional coverage information. |
| `PillarFallPreview` | Destroyed pillar ID; direction; impact cells distinct from debris cells; projected deaths/terrain changes and chain outcomes, attached to existing attack preview. |

Reuse existing `CombatEvent`/result sequence publication. Append event kinds without renumbering existing values. State reads derive current platforms, obstacle counts, and filled/wall cells from `CombatStateData.CommandState`; event history is not a second current-state cache. Provide `CombatStateData.ReadPlatforms()` as an immutable typed read projection if a typed property is needed.

Add `CombatViewPreviewKind.PlatformForecast` and `CombatPreviewResult.ForPlatformForecast(...)` through the existing request/service path. Pillar forecast derives from the same hypothetical attack transition used for existing attack preview; do not create a separately approximated View cone. Cover chain-induced falls/explosions, crushing exceptions, and zero-damage hits.

Forecast consumes only a copied state and RNG. For identical source stamps and visible inputs, it must match execution; it never advances real RNG. When end-turn gimmicks, hidden effects or later actions can intervene, mark the preview conditional and invalidate it on the normal revision change. Never present a partial future path as an exact guaranteed outcome. Hazard/AI coverage marks unsupported future interactions explicitly; movement legality and actual attack resolution must still use the shared exact rules.

View uses ordered steps for animation and current sign for persistent direction indicators/arrows. A net-zero round trip still has nonempty movement evidence. View acknowledges presentation only after its complete platform sequence; no boarding controls are opened during that playback.

## 9. Authoring, Preparation, and Migration

- Extend `ObstacleDefinitionDataSO/Data`: `requiredHits >= 1`, pillar `fallLength >= 1` default 3; preserve barrel damage and supply values with kind-specific validation. Add `Pillar` to obstacle kind/key mappings with append-only IDs.
- New definitions use `requiredHits=1` as an author-proposed editor default. Existing `maxHealth` must not be renamed into hits with `FormerlySerializedAs`; this would silently turn HP balance into hit balance.
- Preserve existing asset GUIDs. Inventory actual referenced obstacle assets and produce an explicit old-HP/new-required-hits migration table in Phase 6. Production values need content-owner review before activation; test fixtures specify their own values. Do not turn unreviewed migration values into a hidden runtime fallback.
- Add `MovingPlatformAuthoring` as a serializable block within the existing combat-stage SO input, not a new DB table/service. It converts to immutable specs, carried by `CombatStageDetail`, `DatabaseBattleInitializationSource`, and battle preparation.
- Anchor convention: shape extends from anchor in positive X or positive Y; use the same convention for route endpoints and initial anchor. Validate aligned distinct endpoints, sign, length, initial anchor on route, entire footprint inside board, unique source IDs, initial cliff support, and nonoverlapping initial platform footprints. Route overlap is allowed; blocked route sections are valid gameplay, not teleportation permissions.
- Generic Inspector input and validation are sufficient. If `CombatStageDataEditor` or the SO layout document serializer hides/drops new fields, minimally round-trip those fields. A graphical route editor is outside scope.
- Unknown future cliff-like categories are not introduced now. The shared classifier is the extension point when a real category is specified.

## 10. Independence, SOLID, and Verification Scope

Reviewed against the user-supplied `feature-change-locality-guidelines.md`, sections 1–6, on 2026-11-16. This review changes these design/plan documents only. The external guideline is a review criterion, not permission to refactor runtime code or redefine gameplay.

The findings below distinguish observed code from expected effects of the proposed boundaries. No reduction in implementation effort or regression scope has been demonstrated by execution.

| Actual change scenario | Current burden and evidence | Classification / minimum action | Expected modification and understanding boundary | Necessary validation retained |
| --- | --- | --- | --- | --- |
| Hit-count obstacles and explicit barrel detonation | `AttackDirectDamageCalculator` sends numeric amounts; `ObstacleReactionCalculator.DamageStructure` owns HP reduction, supply reward and explosion enqueueing. `CombatHealthEvidenceValidator` and current View readers assume HP. | Changing the metric is a necessary shared contract migration. Hiding it behind `(1,1)` would be avoidable semantic coupling. Keep one typed durability source and explicitly inventory numeric consumers. | One-time Core delivery/evidence/copy migration plus a View contract handoff. Later hit-count tuning changes SO values; a barrel trigger calls the barrel operation and does not reconstruct fake damage or know pillar/transport internals. | Zero-damage/multihit, non-attack/fall paths, general HP preservation, evidence and AI/receipt semantics. View reads verified later by its owner. |
| Pillar impact after an earlier reaction changed the board | Attack and arrival calculations share some dictionaries but build distinct reaction calculators/environment snapshots; `RefreshObstacles` replaces an instance. Existing wall publication forbids generated walls. | Terrain publication is a necessary shared boundary change. Multiple stale snapshots/implicit queue replacement are avoidable coupling. Share command-local existing collaborators and make hit-scope lifetime explicit (5.4). | Geometry/impact changes remain in pillar calculation; platform collision consumes effective surface facts. Neither feature editor needs the other's scheduling/reaction implementation. Construction changes remain localized to real command assembly. | Two-hit-scope chain, current terrain read, duplicate destruction, source allocation and atomic publication; movement and wall/LOS behavior after terrain mutation. |
| Platform movement before the next turn | `AdvanceCore`/`AdvancePrepared` start the turn directly. `ActionRestrictionTiming` infers pending end cleanup from the same starting phases; `CombatPlan` already gates unpresented results. | Prelude placement and effect timing are necessary rule connections. A new animation clock, duplicated gate, or platform-specific branches in each status rule would be avoidable coupling. Keep orchestration and explicitly pass the existing effect-timing meaning. | Route/bounce/order edits stay in platform calculation; entry effects remain owned by common movement settlement. Callers know due-boundary and result/acknowledgment contracts, not View animation or individual trait internals. | Sequential footprint/rider collisions, effects and duration, seeded forecasts, one boundary execution, existing turn/end-gimmick regressions. Presentation gate acceptance belongs to View/flow ownership after handoff. |

Composition edits are valid when a real dependency is introduced: passing shared command state/reaction ownership into the existing projection constructors, or adding authoring inputs to preparation. Do not create per-feature Bootstrap/Factory/services merely to make those constructors shorter. Runtime reaction draining and before-turn sequencing are legitimate execution coordination, not misplaced object assembly.

For later local changes, distinguish a value edit (e.g. pillar SO length) from an algorithm change (angle/footprint) and from a shared contract change (durability or effective support). Their regression sets need not be identical. This does not waive mandatory project regression/integration checks.

Planned principle checks: SRP separates obstacle reaction, pillar terrain impact, platform transport and publication by their actual reasons to change. OCP extends explicit existing discriminators/composition without a speculative plug-in framework. LSP requires every `MovementTileFact` subtype to preserve visibility/equality semantics; limited liveness compatibility must be documented and numeric consumers explicitly migrated. ISP gives View immutable facts/results rather than writable world internals. DIP keeps calculations on existing pure contracts/rules, assembled by SO conversion/services. Constructor/factory registration changes are permitted where real dependencies require them. These checks require implementation evidence later; the document does not prove them by naming the principles.

SSOT: one obstacle durability source, one platform anchor/sign source, terrain source IDs from the existing allocator, one RNG stream, no persistent passenger/seat copy, shared forecast/execution rules. FW contracts remain unchanged; existing service interfaces/Group export paths are extended only where required.

Verification is described in the phase plan. Pure Core and CoreIntegration fixtures use test-owned inputs. View code/tests and MSBuild are excluded. Unity execution for authorized Core verification must use an AppData-access-approved path; do not delete caches or upgrade Unity to work around sandbox denial. Report independent compile, test, publication, and View playback status without conflation.

Core delivery is complete when its approved calculations, input conversion, publication/read contracts, required Core verification and handoff are complete. A later View worker owns animation, terrain/debris visuals, hit-count display, arrows, playback acknowledgments and their visual acceptance. Preserve the existing cross-owner result contract and report integration readiness separately; do not mark the whole feature visually accepted from Core tests, or leave all Core phases open merely because View has not implemented it yet.

## 11. Review Status

No code, runtime assets, builds, Unity tests, or browser runs were performed for this design. The builder review raised effect-window timing, non-attack obstacle operations, and runtime nontransportable occupancy; all three are addressed above and mapped to plan tests. The locality-guideline follow-up found unclear shared-projection lifetime, limited HP compatibility, and mixed Core/View completion wording; sections 1, 5.1, 5.4, 7.1 and 10 now address them. These are document corrections and anticipated implementation benefits, not verified runtime improvements. Approval of the written design includes reviewing the explicitly labeled author proposals; confirmed gameplay is not reopened by those technical choices.
