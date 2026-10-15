# Task 7 — Buffer-backed combat command contract

Date: 2026-10-01. Scope: the common Task 7 calculations and the remaining Task 7E automatic enemy turn. This supplements the [character migration plan](2026-09-29-character-buffer-effect-migration-plan.md#task-7-effect-and-combat-calculations-consume-facts-and-return-changes) and [targeting implementation plan](2026-10-01-attack-targeting-methods-implementation-plan.md). Production activation remains Task 8.

## Governing ownership

The user's confirmed Buffer ownership and gameplay rules govern this implementation. Historical documents describe prior behavior and dependencies; they do not authorize adding a runtime owner, changing shared Framework behavior, or activating unresolved attack profiles.

The [Framework guidelines](../framework/Framework%20Development%20Guidelines.md), [architecture](../../../Assets/ProjectL/Script/Systems/Documentation/framework-architecture.md), [programmer guide](../../../Assets/ProjectL/Script/Systems/Documentation/framework-programmer-guide.md), [Buffer contract correction](../handoffs/2026-09-28-buffer-contract-correction.md), and current source were inspected. Systems resolves the existing `IBufferQuery` in `CombatProcessingService.PostBindService`. Application and Domain consume Contracts values and perform no Buffer, DB, View, or Framework operations.

The service caches immutable initialization definitions and explicit attack profiles. It retains no HP, wallet, phase, turn, draw cursor, effect consumption, or map progress. Command-local projected dictionaries compose related calculations and are discarded after the call. No StageSession, UnitState, StageSnapshot, or replacement mutable battle owner is constructed on this route.

## Per-command ownership and execution ledger

All rows read current Character/Combat Buffer facts; definition and Artifact inputs are adapted by Systems. Writes pass through the existing `IBattleBufferWriteSink.Apply(CombatCommandTransition)` and its Buffer-owned validation/install boundary. Core never writes a Buffer directly.

| Operation | Additional reads / admission | Calculated changes and ordered facts | Prior calculation responsibilities replaced on this route |
| --- | --- | --- | --- |
| Place main / place unit | Deployment phase, main lock, hand/field location, deployment depths, occupancy, one-cell profile | Free relocation and freed reserve slot; `Placed` fact | StageDeploymentService / main placement and deployment policy |
| Return unit | Deployment phase, living player sub on field, first free reserve slot | Hand location and released occupancy; `Returned` fact; main cannot return | StageDeploymentService return |
| Enter combat | Living placed main, exactly one living enemy HQ, enemy origin records, living actors' priority/delay, gimmick definitions | Redeploy waiting enemies; reserve every available origin before assigning nearest alternatives; lock main; initialize real participants and night/gimmick facts; placement and combat-entered facts | StageFlowService phase transition, timeline/night initialization and enemy redeployment |
| Move | Active actor and matching player/automatic phase, checkpoint, pending cost, committed range, AP, occupancy | Destination, remaining range, pending cost difference and effect state; move and typed Trait/Category activation facts | UnitMovementService checkpoint movement and movement effects |
| Attack | Existing approved profile/aim/admission; original field/roster facts; pending movement, souls, Artifact definitions | Existing Task 7D settlement order; attack/health/death/forced-move/effect/soul/terminal facts; one projected outcome and completion | UnitAttackService and object-bound attack/effect/Artifact settlement plans |
| End unit turn | Active actor, actual participant, night length, pending traversal, souls and fields | Settle traversal; living arrival collection; apply end-turn effect; restore AP; reschedule; increment completed counts/night by delay; decrement fields/souls; return living non-main units at night end; resolve deaths/outcome | StageCombatFacade turn settlement, CombatTimelineService and StagePhaseTransitionService |
| Draw character | Deployment after first preparation, fixed draw order/cursor, canonical Currency amount, configured draw cost, living sub limit, free slot, current entity allocator, owned card and definition | Create one owned hand link; advance cursor/entity ID; deduct gold; retain draw receipt and `Drawn` fact | Character draw rules and reserve/deck/currency settlement |
| Automatic: due railway | Actual completed unit turns, gimmick schedule and rail cells | Eliminate living field units intersecting rails; preserve structures; advance that gimmick's count/schedule; resolve souls/outcome; retain activation and casualty facts | StageGimmickTimelineService and railway settlement |
| Automatic: begin next turn | Real participants, current time, deterministic seed/tie rule and current actor stats | Select next participant, reset turn movement/AP, apply start-turn effects, publish active actor and matching phase; retain start fact | CombatTimelineService begin-turn and effect initialization |
| Automatic: enemy turn | Current living active enemy; shared range/preview scoring and movement candidates | Attack if possible; otherwise one improving pursuit move and recomputed attack; end turn when still active/in progress; merge all steps and facts into one publication | EnemyAiTurnService gateway/StageSnapshot execution |

Player commands cannot control an enemy even if a malformed phase says PlayerInput. Automatic actor calculations cannot control a player. Friendly units and structures remain eligible attack victims under the confirmed targeting rules; caster exclusion is independent of command ownership.

## Canonical data and lifetime

`CharacterBuffer` owns character HP/stats/location, reserve links, checkpoints, temporary bonuses and Trait/Category state. Occupancy is derived from canonical locations; structures remain immutable definition/location values with changed health in the existing Combat Buffer. Hand slots are unique and bounded by reserve capacity before installation.

`CombatStateBuffer.Current.CommandState.Turn` stores immutable **actual** turn values: stage phase, preparation index, main lock, current time/night time, completed unit turns, participant schedules/opening flags/completed counts, enemy origins, and gimmick schedules/counts. A forecast is calculated from these values and never used to reconstruct canonical timeline state. Its unit ordering reuses the existing Domain action interval and tie-key functions. `Delay` advances night progress; the existing action interval remains `ceil(100 / Priority)`.

The existing Combat Buffer also owns fixed draw order/cursor/entity allocation, night/configuration facts, soul/thorn fields and completion/result receipts. Manual draw cost comes from explicit `BattleInitialization.Economy`; absent configuration is an exception, never free drawing. Currency remains in `CurrencyBuffer`; preparation does not reset or copy a legacy wallet.

No new Buffer or actual resource handle was needed. There is no fabricated lifetime scope. `EndBattle` uses the existing Buffer cleanup: active combat/character links and preview are cleared; the latest result and processing counters survive both cleanup and the next preparation, while completion survives cleanup and is cleared at the next preparation. A later publication replaces the latest result. Owned cards, soul counts and persistent main data follow the existing Character Buffer policy. The processing service clears its definition/profile cache on its cleanup call. Chapter entry and boss completion continue to use existing progress Buffer APIs and retention rules.

The old object-based services remain reachable by their historical callers pending Task 8 migration/cleanup. Their presence does not establish a dependency from the new calculation path. Do not delete them before the production consumer is switched and verified.

Remaining legacy consumer paths to trace during Task 8 are Application/StageRun `StageCombatFacade`, `StageFlowService`, `StageDeploymentService` and `StageDrawService`; Application/Combat `UnitMovementService`, object-bound `UnitAttackService`, `CombatTimelineService`, `EnemyAiTurnService.Run` and `IEnemyAiCombatGateway`; Application/Combat/Effects owner/coordinator/orchestrator paths; Application/Artifact object-bound lifecycle/projection/resolution paths; and Application/StageGimmicks `StageGimmickTimelineService`. The new route reuses only inspected pure static math/definition compilation from shared files. Actual Factory/Job callers and standalone progression cleanup must be reviewed before removing old consumers.

## Processing and read contracts

Use the already registered `IInGameCoreSystemGroupService.Combat` / `ICombatProcessingService`; no second coordinator or service key was introduced.

| Member | Current implemented behavior |
| --- | --- |
| `bool ProcessNextCommand()` | Reads the next existing numbered intent and current Buffer facts, calculates, validates and publishes one result. `true` includes defined rule rejection. `false` means no eligible command/publication. |
| `bool ProcessLatestPreview()` | Answers the latest unanswered existing preview number for main/unit placement, move/range or attack/range. Publishes only latest preview/answered number. |
| `bool AdvanceCombat()` | Publishes one due gimmick, start-next-turn or complete enemy turn when phase is Automatic. Returns false outside that phase or without a battle. Automatic number is 0 and does not change handled command number. |
| `CombatDrawAvailability GetDrawAvailability()` | Pure current-fact eligibility/reason/remaining count/cost/free-slot query; shares command admission. Requires an active battle and explicit draw-cost definition. |
| `CombatTimelineEntrySnapshot[] GetTimeline(int maxEntries = 10)` | Pure deterministic forecast. Zero returns empty; negative count faults. Stops at night end, appends a night-end marker if room remains and interleaves due gimmicks. Returns fewer entries when appropriate. |

The last two members are Systems-side rule queries from canonical Buffer facts, with no retained runtime state. They are not production View wiring. Task 8 must bind the agreed query/presentation timing while preserving the user's rule that View displays Buffer state and never assembles DB/Core inputs.

`CombatResult` retains sequence, processing kind, original command number/kind/actor, acceptance/reason, attack/outcome, movement destination/cost, draw settlement and `IReadOnlyList<CombatEvent> Events`. Events contain immutable IDs/values and typed existing attack/effect/gimmick receipts; they have no callbacks or execution ownership. AI composition concatenates step events in calculation order. Rejection contains no gameplay changes or events. Preview carries exactly one placement/move/range/attack payload and its request number, separately from command results.

## Publication boundary

The Buffer group first prepares character replacements/creation and derived indexes, field changes, Combat state/result metadata, optional currency amount and optional progress/completion changes. Any unexpected invalid proposal throws before gameplay installation; Core does not disguise it as a rule rejection. Installation is synchronous, does not invoke calculations or callbacks, and publishes result/counters last. This is one concrete command boundary, with no lock, version, transaction abstraction, new allocator service or event bus.

Accepted/rejected command numbers can contain input gaps. An already handled number does not publish twice. Automatic steps use number 0 with an increasing result sequence. View input acknowledgment does not clear the latest result; later Flow processing can still read it. Task 8 verifies actual consumer ordering and fault handling.

## Scope and remaining integration

Movement keeps the current destination/checkpoint cost and deterministic traversal. User-selected paths require a separate confirmed design and are deferred by the user; no future path API was introduced.

Multi-cell unit profiles remain explicitly rejected by `CombatAttackProfile` before activation. Structures retain their existing footprints. ThornHeart attack reflection, undecided empty-All and new terrain policy remain explicit gates. Ordinary splash, Ballista falloff and the approved one-cell mappings are unchanged.

Task 8 still owns real authored DB source/starting loadout/economy binding, View gestures and Plan/Job/Flow execution/acknowledgment, old production-path cleanup and PlayMode acceptance. EditMode evidence does not establish a playable production cutover.

## Verification

Focused common command/publication/effect/preview tests: 62/62 passed (`task7/common-final-green.xml`). Boundary RED→GREEN evidence includes forecast/night phase, ordered AI events, atomic draw slot rejection, player/enemy admission and cleanup-query failures. Fresh review found no Critical/Important ownership defect; the entered night flag was fixed after a witnessed failing assertion.

Final Unity 6000.0.73f1 EditMode (`task7/final-verified.xml` / `.log`): Core 1383/1383, Framework 265/270 and CombatViewBoundary 13/13. Total 1666, passed 1661, failed 5, skipped 0; test exit 2. Common command and publication fixtures passed 25/25. The exact failed-name set matches Task 7C's five historical authored CombatStageDetail Economy omissions. These remain outside this code slice; no asset validation is relaxed and no full-suite green is claimed.
