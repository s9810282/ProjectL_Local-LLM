# Nexus Reinforcement Spawn Interval Core-to-View Handoff

Date 2026-11-16

## Stage setup and examples

In a test-owned copy of a headquarters-objective stage, expand **Nexus Reinforcements** on the combat stage SO. Enable it and author a pool of eligible single-cell enemy units from the stage roster. The stage needs one living enemy headquarters when prepared. Pool, range, and roster requirements are documented in the original handoff. [Authoring fields](../../../Assets/ProjectL/Script/Systems/DataBaseSystem/Data/InGame/Table/Stage/Combat/NexusReinforcementAuthoring.cs) · [Base reinforcement contract](2026-11-05-nexus-enemy-reinforcement-core-view-handoff.md)

| SO field | Default | Meaning |
|---|---:|---|
| `spawnIntervalTurns` | 1 | Enemy-turn interval. The first opportunity is enemy turn N for a value of N. |
| `spawnCountPerTurn` | 2 | Maximum enemies created in one successful batch. |
| `fieldEnemyLimit` | 5 | Cap on living enemy characters on the field, sampled before enemy-start effects. |
| `rangePadding` | 2 | Padding around the source headquarters footprint for the reinforcement area. |
| `pool[].unitDefinition`, `pool[].count` | Empty pool | Finite supply grouped by unit definition. |

Use interval 1 and batch 1 for one enemy on each eligible enemy start. Use interval 2 and batch 1 for one on turns 2, 4, and 6 when capacity, cells, and supply permit. Use interval 2 and batch 2 for up to two on each eligible start. The first enemy turn is the phase after the first completed player turn.

A due batch that cannot create an enemy retries at each following enemy start. If turn 2 is full and a player attack frees space before turn 3, turn 3 can create. A success on turn 3 makes turn 5 the next opportunity. If a two-enemy batch can place just one, that creation restarts the interval and consumes one pool entry. A batch that creates zero keeps the previous success turn, remaining pool, and reinforcement RNG. New actors start with `UnitActionUse.Deferred` and become eligible at their next enemy start, subject to ordinary action and status rules. [Calculator](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/NexusReinforcementCalculator.cs)

Old stage YAML with the field omitted loads interval 1. An explicit 0 or negative value is rejected when enabled. The prepared spec captures the SO value for that battle. Edits apply on the next preparation. [Immutable definition](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Combat/NexusReinforcementSpec.cs)

## Published state and playback

`CombatStateData.NexusReinforcements` carries the nullable, prepared definition. `CommandState.Reinforcements.LastSpawnEnemyTurn` records the last enemy turn that created one or more actors in an accepted batch. It starts at 0 each battle. The current enemy turn comes from `CombatTurnFacts.CompletedPlayerTurnCount` at enemy start. A batch that creates and immediately loses an actor still counts as a success. The Buffer publishes the stamp, creation history, actor facts, and result together. [Snapshot](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/Combat/CombatStateData.cs) · [Reinforcement state](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Combat/NexusReinforcementState.cs) · [Publication validation](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/Combat/CombatStateBuffer.cs)

Use the existing ordered `CombatEventKind.Reinforced` (48) playback. `CombatReinforcementEvent.Created.EntityId` identifies the new actor and `NexusId` identifies its source. The registered handler installs the creation facts and plays the existing appearance animation. Subsequent events apply effects or death in order, then View refreshes from the final snapshot at the usual result boundary. Core owns the interval. View drives appearance from published events and reads `ReadNexusReinforcementArea()` for the geometric overlay. [Event registration](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Playback/Ordering/CombatEventTypeTable.cs) · [Appearance handler](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Playback/Handlers/CombatReinforcementEventHandler.cs)

## Verification and remaining scene checks

Unity 6000.0.73f1 compiled the Core and CoreIntegration EditMode suites. Final regression results were 6,026 Core and 1,167 CoreIntegration passes, with zero failures and 19 existing opt-in skips. A separate empty-scene Play Mode run drove test-owned combat stage SOs through the real Core entry, command, advance, and publication path. The run confirmed the 2-blocked/3-success/5-success sequence, a one-cell partial batch, and next-turn action eligibility. The existing `Combat_LD_P20.asset` loaded interval 1 after unloading and reloading, and the serialized property provided its Korean Inspector tooltip. Scene playback and Inspector hover are the remaining visual checks.

Use a test-owned stage connected to the Combat scene for visual acceptance:

1. Open the SO Inspector and expand Nexus Reinforcements. Confirm the interval appears alongside the batch settings and hover it to read the Korean tooltip.
2. Try interval 1 with batch 1, then interval 2 with batch 2. Confirm each published enemy appears once, the full batch shows each actor, and the range overlay remains geometric while the interval is waiting or the field is full.
3. Fill the field through turn 2, clear a slot before turn 3, and reopen space before turn 5. Observe appearance on turns 3 and 5 and ordinary enemy actions on turn 4.
4. Leave a single valid cell for a two-unit batch. Open another before the following enemy start. Verify the one actor that appeared can act from its next enemy start and the next appearance waits until the interval elapses. When a spawn is followed by a trait effect or death, check the ordered playback resolves the created actor.

[Implementation plan and execution evidence](../plans/2026-11-16-nexus-reinforcement-spawn-interval-plan.md#phase-3-execution-evidence)
