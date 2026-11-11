# Nexus Enemy Reinforcement Core-to-View Handoff

Date 2026-11-06

Scope covers Core authoring, turn processing, and Buffer contracts. The View owner handles playback and range display. Stage activation uses designer-approved content.

## Stage authoring

Set `CombatStageData.nexusReinforcements.enabled` on a headquarters-objective stage. Set `pool[].unitDefinition` and `pool[].count` for each unit type. Entries must be unique and counts positive. The enabled pool may be empty. Each candidate must be in the stage UnitRoster, have an enemy-eligible regular single-cell definition, and resolve an unowned attack profile. Boss categories and boss objectives are rejected. The stage must have one live enemy headquarters. The initial enemy count is not limited (2026-11-09). [Stage authoring](../../../Assets/ProjectL/Script/Systems/DataBaseSystem/Data/InGame/Table/Stage/Combat/CombatStageData.cs) · [Pool fields](../../../Assets/ProjectL/Script/Systems/DataBaseSystem/Data/InGame/Table/Stage/Combat/NexusReinforcementAuthoring.cs)

- `spawnCountPerTurn` defaults to 2, `rangePadding` defaults to 2, and `fieldEnemyLimit` defaults to 5 (2026-11-09). All accept positive integers. Reinforcements use the stage's existing `enemyLevel`.
- Initial placement comes from either the stage's authored placements or `EnemyLoadout`. The reinforcement pool supplies only new actors created after deployment. For example, a pool of three A and two B begins with five entries even if the stage initially places A.
- The cap is the stage `fieldEnemyLimit` living enemy field characters (`NexusReinforcementSpec.FieldEnemyLimit`, default 5). With the default, four living enemies leave room for one reinforcement and three leave room for two. Starting above the cap is allowed; no reinforcement appears until enough enemies die. The batch is also limited by the remaining supply and valid cells. A blocked opportunity keeps its supply and RNG and is retried on a later enemy start.
- A 2×2 headquarters with padding 2 covers 32 cells away from board edges. Fresh spawns require normal unoccupied ground. Active terrain, objects, persistent effects, and rail cells block spawning, as do orthogonal neighbors of living player field characters. A warning or prospective blast cell can remain available.

## Buffer range and lifetime

Read `ICombatStateBufferReadSink.Current.ReadNexusReinforcementArea()` from the current snapshot while `HasBattle` is true. The cells are ordered by Y then X and clipped to the board. The range excludes the headquarters footprint and is available during deployment and combat. It becomes empty when the source is disabled or destroyed, or when combat is finished. Reaching the cap, exhausting the pool, or moving a blocker leaves the geometric result unchanged. Use it for a range overlay. [CombatStateData](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/Combat/CombatStateData.cs)

Read the nullable pinned definition from `CombatStateData.NexusReinforcements`. Read append-only provenance from `CommandState.Reinforcements.Records` and derive remaining supply through `RemainingTotal(spec)` or `RemainingCount(spec, index)`. A record pairs the actor ID with the stable pool row index. Read the snapshot again after a published result or a change of battle instance. The result sequence orders playback, the query revision identifies published query changes, and the battle instance identifies the lifetime of the facts. A fresh battle starts with a fresh pool. [State and provenance](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Combat/NexusReinforcementState.cs)

## Ordered playback

`CombatReinforcementEvent` uses `CombatEventKind.Reinforced = 48`. The event has `NexusId`, `Record`, and `Created`. The subject for displaying a new actor is `Created.EntityId`. The nexus ID identifies the structure that supplied it. `Created` contains its initial position, facing, level, and `Deferred` action state before surrounding effects. The Character Buffer contains the final fact after the entire batch and its effects. An actor created and killed in the same result can have an alive creation event and a dead final fact. [Typed event](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Combat/NexusReinforcementEvent.cs)

The same accepted `StartControllerTurn` result contains the enemy controller-start event, any ordinary start processing, the ordered reinforcement events, then the effects applied after the batch is present. Create or reveal the actor by ID during ordered playback so dependent effects can resolve their targets. Refresh from the final snapshot at the normal result boundary. The new actor can first act at its next eligible enemy start, subject to the usual status restrictions.

The follow-up publication review verified Taunt and SoloAction shield application to new actors. Freshness is checked on the initial creation event, while the Buffer installs the affected final fact. The publisher also checks the ordered placement chain: final placement may differ only through recorded character changes, including movement or death. The original spawn cell remains the night-redeployment origin.

Survivors become `EnemyWaiting` during night recall. The next combat entry uses the same ID and the original spawn cell or the usual nearest-valid redeployment fallback. This produces `Placed` and preserves the pool record. Death also preserves the record. A credited kill increments kill and run statistics, with zero kill gold and XP for a reinforced victim. Initial enemies of the same unit type and stage-clear rewards use their ordinary rules.

## View integration and acceptance

- Register the sealed event type in `CombatEventTypeTable` and map its handler slot through `CombatEventHandlerTable`. Use the created actor as the event subject, the nexus for any source animation, and the current presentation contracts for sequencing. The existing ordering code warns and skips unregistered types. [Event type table](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Playback/Ordering/CombatEventTypeTable.cs) · [Handler table](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Playback/Handlers/CombatEventHandlerTable.cs) · [Ordering](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Playback/Ordering/OrderedCombatPresentation.cs)
- Exercise deployment and combat range display, one- and two-member batches, a follow-up trait effect targeting a newly created actor, night recall and `Placed`, and the overlay clearing on victory. Use a test-owned stage or an explicitly approved production stage. Keep geometric range display independent of candidate availability.

The companion plan records executable Core and CoreIntegration verification. View playback and PlayMode acceptance are pending. [Implementation and evidence](../plans/2026-11-05-nexus-enemy-reinforcement-core-implementation-plan.md)
